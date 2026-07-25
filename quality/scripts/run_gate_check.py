#!/usr/bin/env python3
"""Canonical gate-check runner for Project Nova (D-064 trust bundle component).

This tool executes exactly one criterion check for one gate at one subject
commit and writes a strict ``gate-check-result-v1`` JSON artifact. The
evidence validator (``quality/scripts/validate_gate_evidence.py``) compares
that artifact field by field against the evidence command record, so the
artifact shape and the canonical command string are contractual:

    python3 quality/scripts/run_gate_check.py \
        --gate <G0..G5> --criterion <ID> --subject <commitSha> --result <path>

The canonical command string never carries ``--executor``; a reviewer run
sets ``NOVA_GATE_EXECUTOR=reviewer`` in the environment instead (the flag
exists for local reproduction only). The deterministic ``commandId`` is
``impl-<gate>-<suffix>`` / ``review-<gate>-<suffix>`` (lower-cased, suffix =
criterion without its gate prefix, e.g. ``impl-g0-architecture`` for
``G0-ARCHITECTURE``).

Canonical criterion IDs come from the gate profiles of the scenario contract
``quality/scenarios/mvp-v1.json`` (D-062/D-064). G0-B checks are implemented
for real and report honestly; G1-G5 criteria are registered but fail closed
with "criterion not implemented" until their gate is implemented:

    G0: G0-ENGINE-PIN, G0-SHARED-SOURCES, G0-ARCHITECTURE, G0-BUILD-WINDOWS,
        G0-BUILD-MACOS, G0-TEST-DOTNET, G0-TEST-EDITMODE, G0-NEGATIVE-CONTROL,
        G0-EVIDENCE-VALIDATOR, G0-NO-TRACKED-BINARIES
    G1: G1-NUMERIC, G1-COMMANDS, G1-STATE, G1-HASHES, G1-SNAPSHOT, G1-REPLAY,
        G1-PARSER, G1-CROSS-PLATFORM, G1-COVERAGE, G1-FORMAT-RESET, G1-V1,
        G1-V2, G1-V3, G1-V4, G1-V5A
    G2: G2-SESSION-PATH, G2-AETHERIUM, G2-GLUTRINNE, G2-FOW, G2-MUTATION-GUARD
    G3: G3-FILTERED-AI, G3-CANONICAL-INTENTS, G3-REPLAY, G3-SAVE-CONTINUATION,
        G3-HIDDEN-WORLD, G3-HEADLESS-VALIDITY, G3-V5B
    G4: G4-MANIFEST, G4-GLUTRINNE, G4-CONTENT, G4-VICTORY, G4-UX-PERSISTENCE,
        G4-ACCESSIBILITY, G4-UI-ONLY, G4-PROVENANCE, G4-USABILITY
    G5: G5-HEADLESS-120, G5-DENOMINATOR, G5-QUARANTINE, G5-MVP-FULL-100,
        G5-MAC-M2, G5-UI-ONLY, G5-TASK-TESTERS, G5-PACING, G5-AUTOSAVE,
        G5-DEFECTS, G5-INDEPENDENT-REVIEW

Exit code is 0 only when the check passes; any failure, unknown
gate/criterion or usage error exits non-zero (fail-closed). The runner is
deterministic, performs no network access and never mutates the repository;
all scratch work happens under ``tempfile``.
"""

from __future__ import annotations

import argparse
import ast
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Any, Callable

ROOT = Path(__file__).resolve().parents[2]
RESULT_SCHEMA_VERSION = "gate-check-result-v1"
GATES = tuple(f"G{number}" for number in range(6))

UNITY_VERSION = "6000.5.4f1"
UNITY_REVISION = "d550df8bd089"
DOTNET_SDK_MAJOR = 8

VALIDATOR = ROOT / "quality/scripts/validate_gate_evidence.py"
AUTHORIZE_WORKFLOW = ROOT / ".github/workflows/quality-gate.yml"
SIMRUNNER_CSPROJ = "tools/Nova.SimRunner/Nova.SimRunner.csproj"

COMMIT_SHA_RE = re.compile(r"^[0-9a-f]{40,64}$")
# Architecture layers (D-061): references may only point strictly downwards.
# Nova.Editor is the host layer; test assemblies are handled separately.
ASSEMBLY_RANKS = {
    "Nova.Core": 0,
    "Nova.Simulation": 1,
    "Nova.AI": 2,
    "Nova.AI.Data": 2,
    "Nova.Networking": 2,
    "Nova.Data": 2,
    "Nova.Gameplay": 3,
    "Nova.Presentation": 4,
    "Nova.Presentation.Maps": 4,
    "Nova.Presentation.Shaders": 4,
    "Nova.Presentation.UI": 4,
    "Nova.Editor": 5,
}
NO_ENGINE_REFERENCE_ASSEMBLIES = (
    "Nova.Core",
    "Nova.Simulation",
    "Nova.AI",
    "Nova.Networking",
)
TEST_RUNNER_REFERENCES = {"UnityEngine.TestRunner", "UnityEditor.TestRunner"}

EXECUTOR_PREFIX = {"implementation": "impl", "reviewer": "review"}


def _run(
    arguments: list[str],
    *,
    cwd: Path = ROOT,
    timeout: int = 120,
) -> subprocess.CompletedProcess[str]:
    """Run one local subprocess without shell, bytecode caches or network."""
    environment = os.environ.copy()
    environment["PYTHONDONTWRITEBYTECODE"] = "1"
    return subprocess.run(
        arguments,
        cwd=cwd,
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        timeout=timeout,
        env=environment,
    )


def _load_json(path: Path) -> Any:
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


# --------------------------------------------------------------------------
# Architecture analysis (shared by G0-ARCHITECTURE and G0-NEGATIVE-CONTROL)
# --------------------------------------------------------------------------


def analyze_asmdef_tree(base: Path) -> list[str]:
    """Return architecture-contract violations for every asmdef below *base*.

    Contract (D-061): Nova.Core/Simulation/AI/Networking must set
    ``noEngineReferences``; the reference graph may only contain strictly
    downward edges Core <- Simulation <- {AI, Networking, Data} <- Gameplay
    <- Presentation <- Editor host; Nova.Simulation must never reference
    Nova.AI; test assemblies may reference test runners and non-presentation,
    non-editor Nova assemblies only.
    """
    violations: list[str] = []
    assemblies: dict[str, dict[str, Any]] = {}
    for path in sorted(base.rglob("*.asmdef")):
        try:
            document = _load_json(path)
        except (OSError, json.JSONDecodeError) as error:
            violations.append(f"{path.name}: unparseable asmdef: {error}")
            continue
        name = document.get("name")
        if not isinstance(name, str) or not name:
            violations.append(f"{path.name}: missing assembly name")
            continue
        if name in assemblies:
            violations.append(f"{name}: duplicate assembly definition")
            continue
        assemblies[name] = document

    for name, document in sorted(assemblies.items()):
        references = document.get("references")
        if not isinstance(references, list):
            violations.append(f"{name}: references must be an array")
            continue
        is_test = name.endswith(".Tests")
        if name in NO_ENGINE_REFERENCE_ASSEMBLIES and (
            document.get("noEngineReferences") is not True
        ):
            violations.append(f"{name}: noEngineReferences must be true")
        for reference in references:
            if not isinstance(reference, str):
                violations.append(f"{name}: non-string reference {reference!r}")
                continue
            if is_test and reference in TEST_RUNNER_REFERENCES:
                continue
            if reference.startswith("GUID:"):
                violations.append(f"{name}: GUID reference is not resolvable")
                continue
            target_rank = ASSEMBLY_RANKS.get(reference)
            if target_rank is None:
                violations.append(f"{name}: unknown assembly reference {reference}")
                continue
            if is_test:
                if target_rank >= 4:
                    violations.append(
                        f"{name}: test assembly must not reference "
                        f"presentation/editor assembly {reference}"
                    )
                continue
            own_rank = ASSEMBLY_RANKS.get(name)
            if own_rank is None:
                violations.append(f"{name}: unknown assembly in reference graph")
                continue
            if name == "Nova.Simulation" and reference == "Nova.AI":
                violations.append("Nova.Simulation must not reference Nova.AI")
                continue
            if target_rank >= own_rank:
                violations.append(
                    f"{name}: forbidden upward/same-layer edge to {reference}"
                )
    return violations


# --------------------------------------------------------------------------
# G0-B criterion checks (MVPRecoveryPlan section 3)
# --------------------------------------------------------------------------


def check_engine_pin() -> tuple[bool, list[str]]:
    """G0-B.1: exact Unity pin, package manifests, pinned .NET 8 SDK."""
    reasons: list[str] = []
    version_file = ROOT / "ProjectSettings/ProjectVersion.txt"
    try:
        version_text = version_file.read_text(encoding="utf-8")
    except OSError as error:
        reasons.append(f"ProjectVersion.txt unreadable: {error}")
    else:
        if f"m_EditorVersion: {UNITY_VERSION}" not in version_text:
            reasons.append(f"ProjectVersion.txt lacks m_EditorVersion {UNITY_VERSION}")
        revision_line = (
            f"m_EditorVersionWithRevision: {UNITY_VERSION} ({UNITY_REVISION})"
        )
        if revision_line not in version_text:
            reasons.append(f"ProjectVersion.txt lacks exact revision {UNITY_REVISION}")
    for relative in ("Packages/manifest.json", "Packages/packages-lock.json"):
        path = ROOT / relative
        try:
            _load_json(path)
        except (OSError, json.JSONDecodeError) as error:
            reasons.append(f"{relative}: {error}")
    try:
        manifest = _load_json(ROOT / "Packages/manifest.json")
    except (OSError, json.JSONDecodeError):
        manifest = {}
    dependencies = manifest.get("dependencies") if isinstance(manifest, dict) else {}
    if not isinstance(dependencies, dict) or (
        "com.unity.render-pipelines.universal" not in dependencies
    ):
        reasons.append("Packages/manifest.json lacks the URP dependency")
    global_json = ROOT / "global.json"
    try:
        sdk_document = _load_json(global_json)
    except (OSError, json.JSONDecodeError) as error:
        reasons.append(f"global.json with exact .NET {DOTNET_SDK_MAJOR} SDK pin: {error}")
    else:
        sdk = sdk_document.get("sdk") if isinstance(sdk_document, dict) else None
        version = sdk.get("version") if isinstance(sdk, dict) else None
        if not isinstance(version, str) or not re.fullmatch(
            rf"{DOTNET_SDK_MAJOR}\.[0-9]+\.[0-9]+", version
        ):
            reasons.append(
                f"global.json sdk.version must be an exact .NET "
                f"{DOTNET_SDK_MAJOR}.x.y pin, got {version!r}"
            )
        roll_forward = sdk.get("rollForward") if isinstance(sdk, dict) else None
        if roll_forward not in (None, "disable"):
            reasons.append(
                f"global.json rollForward must be unset or 'disable', "
                f"got {roll_forward!r}"
            )
    return (not reasons, reasons or ["Unity/URP/.NET pins are exact"])


def check_shared_sources() -> tuple[bool, list[str]]:
    """G0-B.2: tracked SimRunner sharing Core/Simulation sources and defines."""
    reasons: list[str] = []
    tracked = _run(["git", "ls-files", "--error-unmatch", SIMRUNNER_CSPROJ])
    if tracked.returncode != 0:
        reasons.append(f"{SIMRUNNER_CSPROJ} is not tracked in git")
    csproj_path = ROOT / SIMRUNNER_CSPROJ
    try:
        project = ET.parse(csproj_path).getroot()
    except (OSError, ET.ParseError) as error:
        reasons.append(f"{SIMRUNNER_CSPROJ}: {error}")
        return (False, reasons)
    includes = [
        element.get("Include") or ""
        for element in project.iter("Compile")
    ]
    for fragment in ("Scripts\\Core\\", "Scripts\\Simulation\\"):
        if not any(fragment in include for include in includes):
            reasons.append(
                f"{SIMRUNNER_CSPROJ} lacks a Compile Include for {fragment}"
            )
    define_constants = [
        (element.text or "")
        for element in project.iter("DefineConstants")
    ]
    defines = ";".join(define_constants)
    if not re.search(r"DETERMINISTIC|FIXED", defines, re.IGNORECASE):
        reasons.append(
            f"{SIMRUNNER_CSPROJ} declares no determinism-relevant "
            "DefineConstants (expected e.g. a NOVA_*DETERMINISTIC* define)"
        )
    return (not reasons, reasons or ["SimRunner shares sources and defines"])


def check_architecture() -> tuple[bool, list[str]]:
    """G0-B.3: asmdef/architecture boundaries of the live Assets tree."""
    violations = analyze_asmdef_tree(ROOT / "Assets")
    return (
        not violations,
        violations or ["asmdef architecture boundaries hold"],
    )


def _build_prerequisites(platform_label: str) -> tuple[bool, list[str]]:
    """Shared G0-B.4 prerequisites; real builds are environment-bound."""
    reasons: list[str] = []
    settings = ROOT / "ProjectSettings/EditorBuildSettings.asset"
    try:
        settings_text = settings.read_text(encoding="utf-8")
    except OSError as error:
        reasons.append(f"EditorBuildSettings.asset unreadable: {error}")
    else:
        if re.search(r"m_Scenes:\s*\[\s*\]", settings_text):
            reasons.append("EditorBuildSettings scene list is empty")
    build_script = None
    for path in sorted((ROOT / "Assets").rglob("*.cs")):
        try:
            source = path.read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue
        if "BuildPipeline.BuildPlayer" in source or "BuildPlayerOptions" in source:
            build_script = path
            break
    if build_script is None:
        reasons.append("no build script using BuildPipeline.BuildPlayer found")
    reasons.append(
        f"note: the real {platform_label} build itself is environment-bound "
        "and runs in the G0-B build environment, not in this local check"
    )
    return (
        not [reason for reason in reasons if not reason.startswith("note:")],
        reasons,
    )


def check_build_windows() -> tuple[bool, list[str]]:
    """G0-B.4 (Windows x64 reference): build prerequisites only."""
    return _build_prerequisites("Windows-x64")


def check_build_macos() -> tuple[bool, list[str]]:
    """G0-B.4 (macOS arm64): build prerequisites only."""
    return _build_prerequisites("macOS-arm64")


def check_test_dotnet() -> tuple[bool, list[str]]:
    """G0-B.5 (.NET): run .NET tests when the SDK is available."""
    dotnet = shutil.which("dotnet")
    if dotnet is None:
        return (
            False,
            ["dotnet SDK is not on PATH; .NET tests cannot run in this environment"],
        )
    test_projects = sorted((ROOT / "tools").rglob("*Tests*.csproj"))
    if not test_projects:
        return (False, ["no .NET test project found under tools/"])
    reasons: list[str] = []
    for project in test_projects:
        result = _run(
            [dotnet, "test", str(project), "--nologo"],
            timeout=600,
        )
        if result.returncode != 0:
            reasons.append(
                f"dotnet test failed for {project.name}: "
                f"{result.stdout.strip()[-500:]}"
            )
    return (not reasons, reasons or ["dotnet tests are green"])


def check_test_editmode() -> tuple[bool, list[str]]:
    """G0-B.5 (EditMode): run Unity EditMode tests when the editor exists."""
    candidates = [
        Path(
            "/Applications/Unity/Hub/Editor"
            f"/{UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"
        ),
        Path(
            f"C:/Program Files/Unity/Hub/Editor/{UNITY_VERSION}/Editor/Unity.exe"
        ),
        Path.home() / f"Unity/Hub/Editor/{UNITY_VERSION}/Editor/Unity",
    ]
    unity = next((candidate for candidate in candidates if candidate.is_file()), None)
    if unity is None:
        return (
            False,
            [
                f"Unity Editor {UNITY_VERSION} not installed; EditMode tests "
                "cannot run in this environment"
            ],
        )
    with tempfile.TemporaryDirectory(prefix="nova-editmode-") as scratch:
        results_path = Path(scratch) / "editmode-results.xml"
        log_path = Path(scratch) / "editmode.log"
        # Note: -quit must NOT be passed with -runTests, or the editor
        # quits before executing any test.
        result = _run(
            [
                str(unity),
                "-batchmode",
                "-nographics",
                "-runTests",
                "-testPlatform",
                "EditMode",
                "-projectPath",
                str(ROOT),
                "-testResults",
                str(results_path),
                "-logFile",
                str(log_path),
            ],
            timeout=1800,
        )
        try:
            results_text = results_path.read_text(encoding="utf-8")
        except OSError as error:
            return (
                False,
                [
                    f"EditMode results missing ({error}); Unity exit "
                    f"{result.returncode}, see {log_path}"
                ],
            )
        # Count only test-case nodes: result="Passed"/"Failed" also appears on
        # parent fixture/suite nodes, which inflated the numbers systematically
        # (review finding; NUnit3 XML marks each test case individually).
        failed = len(re.findall(r"<test-case\b[^>]*\bresult=\"Failed\"", results_text))
        passed = len(re.findall(r"<test-case\b[^>]*\bresult=\"Passed\"", results_text))
        summary = f"{passed} passed, {failed} failed"
        if result.returncode != 0 or failed:
            return (
                False,
                [
                    f"Unity EditMode tests red ({summary}); Unity exit "
                    f"{result.returncode}, see {log_path}"
                ],
            )
    return (True, [f"Unity EditMode tests are green ({summary})"])


def check_negative_control() -> tuple[bool, list[str]]:
    """G0-B.6: the asmdef check must go red on an injected forbidden edge."""
    with tempfile.TemporaryDirectory(prefix="nova-negative-control-") as scratch:
        sandbox = Path(scratch)
        for path in sorted((ROOT / "Assets").rglob("*.asmdef")):
            shutil.copyfile(path, sandbox / path.name)
        baseline = analyze_asmdef_tree(sandbox)
        simulation = sandbox / "Nova.Simulation.asmdef"
        try:
            document = _load_json(simulation)
        except (OSError, json.JSONDecodeError) as error:
            return (False, [f"cannot prepare negative control: {error}"])
        references = document.setdefault("references", [])
        references.append("Nova.AI")
        simulation.write_text(
            json.dumps(document, indent=4) + "\n", encoding="utf-8"
        )
        injected = analyze_asmdef_tree(sandbox)
    new_violations = [item for item in injected if item not in baseline]
    hits = [
        item
        for item in new_violations
        if "Nova.Simulation" in item and "Nova.AI" in item
    ]
    if not hits:
        return (
            False,
            [
                "asmdef check did not go red on an injected "
                "Nova.Simulation -> Nova.AI edge"
            ],
        )
    return (
        True,
        [
            f"negative control red as required ({hits[0]}); "
            f"baseline violations in current tree: {len(baseline)}"
        ],
    )


def check_evidence_validator() -> tuple[bool, list[str]]:
    """G0-B.7/.9: runner self-check, trust path, self-test, no local pass."""
    reasons: list[str] = []
    runner = ROOT / "quality/scripts/run_gate_check.py"
    try:
        ast.parse(runner.read_text(encoding="utf-8"))
    except (OSError, SyntaxError) as error:
        reasons.append(f"run_gate_check.py: {error}")
    if not AUTHORIZE_WORKFLOW.is_file():
        reasons.append(".github/workflows/quality-gate.yml is missing")
    else:
        workflow_text = AUTHORIZE_WORKFLOW.read_text(encoding="utf-8")
        if "validate_gate_evidence.py --self-test" not in workflow_text:
            reasons.append(
                "quality-gate.yml does not document/run the validator self-test"
            )
    self_test = _run(
        [sys.executable, "quality/scripts/validate_gate_evidence.py", "--self-test"],
        timeout=180,
    )
    if self_test.returncode != 0:
        reasons.append(
            "validator self-test failed: " f"{self_test.stdout.strip()[-500:]}"
        )
    with tempfile.TemporaryDirectory(prefix="nova-local-pass-") as scratch:
        synthetic = Path(scratch) / "LocalPass.json"
        # A non-hex pseudo subject keeps the validator off the git path, so
        # the working-tree 1.3.0 contract is loaded and the run reaches the
        # authorization branch; the verdict must still be rejected.
        synthetic.write_text(
            json.dumps(
                {
                    "schemaVersion": "1.3.0",
                    "gateId": "G0",
                    "subject": {
                        "commitSha": "local-untrusted",
                        "treeSha": "local-untrusted",
                        "dirty": False,
                    },
                    "attempt": {
                        "id": "2026-01-01T00-00-00Z-local",
                        "evidencePath": (
                            "quality/evidence/G0/local-untrusted/"
                            "2026-01-01T00-00-00Z-local/GateEvidence.json"
                        ),
                    },
                    "scope": {},
                    "content": {},
                    "trustBundle": {},
                    "toolchains": {},
                    "coverage": {},
                    "ci": {},
                    "reviewer": {},
                    "verdict": {
                        "result": "pass",
                        "rationale": "synthetic local pass attempt",
                    },
                    "priorGateEvidence": [],
                    "environments": [],
                    "commands": [],
                    "rawMetrics": [],
                    "artifacts": [],
                    "criteria": [],
                }
            ),
            encoding="utf-8",
        )
        attempt = _run(
            [
                sys.executable,
                "quality/scripts/validate_gate_evidence.py",
                str(synthetic),
            ],
            timeout=180,
        )
    if attempt.returncode == 0:
        reasons.append("validator accepted a local pass evidence (exit 0)")
    elif not (
        "E_TRUST_CONTEXT" in attempt.stdout
        or "E_AUTHORIZATION_BOOTSTRAP" in attempt.stdout
    ):
        reasons.append(
            "local pass rejection lacks E_TRUST_CONTEXT/"
            f"E_AUTHORIZATION_BOOTSTRAP: {attempt.stdout.strip()[-500:]}"
        )
    return (
        not reasons,
        reasons
        or [
            "trust path closed: self-test green, local pass rejected, "
            "workflow present"
        ],
    )


def check_no_tracked_binaries() -> tuple[bool, list[str]]:
    """G0-B.8: no tracked generated binaries under tools/ or Assets/."""
    result = _run(["git", "ls-files", "-z", "--", "tools", "Assets"])
    if result.returncode != 0:
        return (False, [f"git ls-files failed: {result.stderr.strip()}"])
    offenders = []
    for raw in result.stdout.split("\0"):
        if not raw:
            continue
        lowered = raw.lower()
        parts = lowered.split("/")
        if (
            lowered.endswith((".dll", ".pdb"))
            or "bin" in parts
            or "obj" in parts
        ):
            offenders.append(raw)
    return (
        not offenders,
        [f"tracked generated binary: {item}" for item in offenders]
        or ["no tracked generated binaries"],
    )


def _not_implemented(gate: str, criterion: str) -> tuple[bool, list[str]]:
    """Registered G1-G5 criteria fail closed until their gate is built."""
    return (
        False,
        [
            f"criterion not implemented: {criterion} belongs to gate {gate}, "
            "whose checks are scheduled after G0 (MVPRecoveryPlan); "
            "this runner never reports a fake pass"
        ],
    )


G0_CHECKS: dict[str, Callable[[], tuple[bool, list[str]]]] = {
    "G0-ENGINE-PIN": check_engine_pin,
    "G0-SHARED-SOURCES": check_shared_sources,
    "G0-ARCHITECTURE": check_architecture,
    "G0-BUILD-WINDOWS": check_build_windows,
    "G0-BUILD-MACOS": check_build_macos,
    "G0-TEST-DOTNET": check_test_dotnet,
    "G0-TEST-EDITMODE": check_test_editmode,
    "G0-NEGATIVE-CONTROL": check_negative_control,
    "G0-EVIDENCE-VALIDATOR": check_evidence_validator,
    "G0-NO-TRACKED-BINARIES": check_no_tracked_binaries,
}

REGISTERED_CRITERIA: dict[str, tuple[str, ...]] = {
    "G0": tuple(G0_CHECKS),
    "G1": (
        "G1-NUMERIC",
        "G1-COMMANDS",
        "G1-STATE",
        "G1-HASHES",
        "G1-SNAPSHOT",
        "G1-REPLAY",
        "G1-PARSER",
        "G1-CROSS-PLATFORM",
        "G1-COVERAGE",
        "G1-FORMAT-RESET",
        "G1-V1",
        "G1-V2",
        "G1-V3",
        "G1-V4",
        "G1-V5A",
    ),
    "G2": (
        "G2-SESSION-PATH",
        "G2-AETHERIUM",
        "G2-GLUTRINNE",
        "G2-FOW",
        "G2-MUTATION-GUARD",
    ),
    "G3": (
        "G3-FILTERED-AI",
        "G3-CANONICAL-INTENTS",
        "G3-REPLAY",
        "G3-SAVE-CONTINUATION",
        "G3-HIDDEN-WORLD",
        "G3-HEADLESS-VALIDITY",
        "G3-V5B",
    ),
    "G4": (
        "G4-MANIFEST",
        "G4-GLUTRINNE",
        "G4-CONTENT",
        "G4-VICTORY",
        "G4-UX-PERSISTENCE",
        "G4-ACCESSIBILITY",
        "G4-UI-ONLY",
        "G4-PROVENANCE",
        "G4-USABILITY",
    ),
    "G5": (
        "G5-HEADLESS-120",
        "G5-DENOMINATOR",
        "G5-QUARANTINE",
        "G5-MVP-FULL-100",
        "G5-MAC-M2",
        "G5-UI-ONLY",
        "G5-TASK-TESTERS",
        "G5-PACING",
        "G5-AUTOSAVE",
        "G5-DEFECTS",
        "G5-INDEPENDENT-REVIEW",
    ),
}


def _resolve_executor(argument: str | None) -> str:
    """Executor: flag wins, then NOVA_GATE_EXECUTOR, then implementation."""
    candidate = argument or os.environ.get("NOVA_GATE_EXECUTOR") or "implementation"
    if candidate not in EXECUTOR_PREFIX:
        raise ValueError(
            f"invalid executor {candidate!r}; expected implementation|reviewer"
        )
    return candidate


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Run one canonical gate check and write a "
        "gate-check-result-v1 artifact.",
    )
    parser.add_argument("--gate", required=True, choices=GATES)
    parser.add_argument("--criterion", required=True)
    parser.add_argument("--subject", required=True, help="subject commit SHA")
    parser.add_argument("--result", required=True, type=Path)
    parser.add_argument(
        "--executor",
        choices=sorted(EXECUTOR_PREFIX),
        default=None,
        help="defaults to NOVA_GATE_EXECUTOR or 'implementation'; the "
        "canonical evidence command omits this flag",
    )
    arguments = parser.parse_args()

    subject = arguments.subject.lower()
    if not COMMIT_SHA_RE.fullmatch(subject):
        parser.error("--subject must be a lowercase hex commit SHA (40-64)")
    try:
        executor = _resolve_executor(arguments.executor)
    except ValueError as error:
        parser.error(str(error))

    criterion = arguments.criterion
    registered = REGISTERED_CRITERIA[arguments.gate]
    if criterion not in registered:
        print(
            f"unknown criterion {criterion!r} for gate {arguments.gate}; "
            f"registered: {', '.join(registered)}",
            file=sys.stderr,
        )
        return 2

    check = G0_CHECKS.get(criterion)
    started = time.monotonic()
    if check is None:
        passed, reasons = _not_implemented(arguments.gate, criterion)
    else:
        passed, reasons = check()
    duration = round(time.monotonic() - started, 3)
    exit_code = 0 if passed else 1

    for reason in reasons:
        if reason.startswith("note:"):
            print(f"NOTE: {reason.removeprefix('note:').strip()}")
        else:
            print(f"{'PASS' if passed else 'FAIL'}: {reason}")

    command = (
        "python3 quality/scripts/run_gate_check.py "
        f"--gate {arguments.gate} --criterion {criterion} "
        f"--subject {subject} --result {arguments.result}"
    )
    artifact = {
        "schemaVersion": RESULT_SCHEMA_VERSION,
        "gateId": arguments.gate,
        "subjectCommitSha": subject,
        "commandId": (
            f"{EXECUTOR_PREFIX[executor]}-{arguments.gate.lower()}-"
            f"{criterion[len(arguments.gate) + 1:].lower()}"
        ),
        "executor": executor,
        "command": command,
        "workingDirectory": os.path.relpath(Path.cwd(), ROOT),
        "durationSeconds": duration,
        "exitCode": exit_code,
        "conclusion": "success" if passed else "failure",
        "checks": [{"id": criterion, "result": "pass" if passed else "fail"}],
        "scenarioIds": [],
    }
    arguments.result.parent.mkdir(parents=True, exist_ok=True)
    arguments.result.write_text(
        json.dumps(artifact, indent=2, ensure_ascii=False, allow_nan=False) + "\n",
        encoding="utf-8",
    )
    print(
        f"{criterion}: {'pass' if passed else 'fail'} "
        f"({duration}s) -> {arguments.result}",
        file=sys.stderr if not passed else sys.stdout,
    )
    return exit_code


if __name__ == "__main__":
    sys.exit(main())
