#!/usr/bin/env python3
"""Validate cross-field semantics of Project Nova GateEvidence files.

JSON Schema remains the structural contract. This standard-library validator
adds comparisons, reference resolution, repository checks and negative
controls that Draft 2020-12 cannot express reliably. Per D-064, Schema 1.3
authorizes a gate pass only from a subject-independent trusted tool checkout
(`--trusted-tool-checkout`) with an externally generated trust context; local
runs remain integrity-only and fail closed on any pass verdict.
"""

from __future__ import annotations

import argparse
import copy
import hashlib
import json
import math
import os
import re
import subprocess
import sys
import tempfile
from pathlib import Path, PurePosixPath
from typing import Any, Callable, Iterable

ROOT = Path(__file__).resolve().parents[2]
SCENARIO_CONTRACT = ROOT / "quality/scenarios/mvp-v1.json"
EVIDENCE_SCHEMA = ROOT / "quality/schemas/GateEvidence.schema.json"
SCHEMA_VALIDATOR = ROOT / "quality/scripts/validate_evidence_schema.mjs"
SCHEMA_VERSION = "1.3.0"
TRUST_CONTEXT_VERSION = "2.0.0"
SCENARIO_AUTHORIZATION_STATUS = "trusted-tool-checkout-authorization"
# D-064: the trust bundle binds every tool/contract component per subject and
# trusted commit plus SHA-256. Component ids are canonical; paths are
# repository-relative and enforced semantically.
TRUST_BUNDLE_COMPONENTS = {
    "manifest": "quality/content/mvp-v1.json",
    "scenarioContract": "quality/scenarios/mvp-v1.json",
    "evidenceSchema": "quality/schemas/GateEvidence.schema.json",
    "evidenceValidator": "quality/scripts/validate_gate_evidence.py",
    "ajvWrapper": "quality/scripts/validate_evidence_schema.mjs",
    "packageManifest": "quality/package.json",
    "packageLock": "quality/package-lock.json",
    "gateRunner": "quality/scripts/run_gate_check.py",
    "authorizeWorkflow": ".github/workflows/quality-gate.yml",
}
CONTENT_COMPONENT_FIELDS = {
    "manifest": ("manifestPath", "manifestSha256"),
    "scenarioContract": ("scenarioPath", "scenarioSha256"),
    "evidenceSchema": ("evidenceSchemaPath", "evidenceSchemaSha256"),
    "evidenceValidator": ("evidenceValidatorPath", "evidenceValidatorSha256"),
    "ajvWrapper": ("ajvWrapperPath", "ajvWrapperSha256"),
    "packageManifest": ("packageManifestPath", "packageManifestSha256"),
    "packageLock": ("packageLockPath", "packageLockSha256"),
    "gateRunner": ("gateRunnerPath", "gateRunnerSha256"),
    "authorizeWorkflow": ("authorizeWorkflowPath", "authorizeWorkflowSha256"),
}
# D-064: environment fields compared exactly against the method profiles of
# the scenario contract (Windows-x64 reference vs. Mac M2 functional).
ENVIRONMENT_FIELDS = (
    "os",
    "architecture",
    "hardware",
    "build",
    "executionPath",
    "burstEnabled",
    "resolution",
    "qualityProfile",
    "vSyncEnabled",
    "deepProfilingEnabled",
    "replay",
)
AUTHORIZED_EVIDENCE_KEYS = {
    "gateId",
    "evidencePath",
    "evidenceSha256",
    "subjectCommitSha",
    "subjectTreeSha",
    "ciRunId",
    "ciJobId",
    "ciAttestationSha256",
    "reviewArtifactSha256",
}
GATE_SEQUENCE = tuple(f"G{number}" for number in range(6))
GATES = set(GATE_SEQUENCE)
ATTEMPT_RE = re.compile(
    r"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}-[0-9]{2}-[0-9]{2}Z-[a-z0-9-]+$"
)


class StrictJsonError(ValueError):
    """Raised when JSON is not strict I-JSON input."""


def _reject_duplicate_pairs(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in pairs:
        if key in result:
            raise StrictJsonError(f"duplicate key: {key}")
        result[key] = value
    return result


def _reject_constant(value: str) -> None:
    raise StrictJsonError(f"non-finite number: {value}")


def strict_load(path: Path) -> Any:
    try:
        return strict_load_bytes(path.read_bytes())
    except UnicodeDecodeError as error:
        raise StrictJsonError(f"invalid UTF-8 at byte {error.start}") from error


def strict_load_bytes(raw: bytes) -> Any:
    try:
        text = raw.decode("utf-8")
    except UnicodeDecodeError as error:
        raise StrictJsonError(f"invalid UTF-8 at byte {error.start}") from error
    try:
        return json.loads(
            text,
            object_pairs_hook=_reject_duplicate_pairs,
            parse_constant=_reject_constant,
        )
    except json.JSONDecodeError as error:
        raise StrictJsonError(
            f"invalid JSON at line {error.lineno}, column {error.colno}: {error.msg}"
        ) from error


def sha256_bytes(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _schema_validation_errors(
    document: dict[str, Any],
    schema_bytes: bytes,
    *,
    validator_path: Path = SCHEMA_VALIDATOR,
    working_directory: Path = ROOT,
    timeout_seconds: float = 30,
) -> list[str]:
    """Validate one strict-loaded document with pinned Ajv Draft 2020-12."""

    if not validator_path.is_file():
        return [f"schema validator is missing: {validator_path}"]
    try:
        schema_document = strict_load_bytes(schema_bytes)
    except StrictJsonError as error:
        return [f"evidence schema is not strict JSON: {error}"]
    if not isinstance(schema_document, dict):
        return ["evidence schema root must be an object"]

    with tempfile.TemporaryDirectory() as temp:
        temp_root = Path(temp)
        schema_path = temp_root / "GateEvidence.schema.json"
        document_path = temp_root / "GateEvidence.json"
        schema_path.write_bytes(schema_bytes)
        document_path.write_text(
            json.dumps(document, ensure_ascii=False, separators=(",", ":")),
            encoding="utf-8",
        )
        try:
            result = subprocess.run(
                ["node", str(validator_path), str(schema_path), str(document_path)],
                cwd=working_directory,
                check=False,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                text=True,
                timeout=timeout_seconds,
            )
        except OSError as error:
            return [f"schema validator unavailable: {error}"]
        except subprocess.TimeoutExpired:
            return [
                f"schema validator timed out after {timeout_seconds:g} seconds"
            ]
    if result.returncode == 0:
        return []
    detail = result.stdout.strip() or f"schema validator exited {result.returncode}"
    return [line for line in detail.splitlines() if line.strip()]


def _git(*arguments: str, root: Path) -> str:
    result = subprocess.run(
        ["git", *arguments],
        cwd=root,
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
    )
    if result.returncode != 0:
        raise ValueError(result.stderr.strip() or "git command failed")
    return result.stdout.strip()


def _git_bytes(*arguments: str, root: Path) -> bytes:
    result = subprocess.run(
        ["git", *arguments],
        cwd=root,
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if result.returncode != 0:
        message = result.stderr.decode("utf-8", errors="replace").strip()
        raise ValueError(message or "git command failed")
    return result.stdout


def _safe_repo_path(root: Path, raw_path: str) -> Path:
    pure = PurePosixPath(raw_path)
    if pure.is_absolute() or not pure.parts or ".." in pure.parts:
        raise ValueError("path must be a non-empty repository-relative path")
    candidate = (root / Path(*pure.parts)).resolve()
    try:
        candidate.relative_to(root.resolve())
    except ValueError as error:
        raise ValueError("path escapes repository root") from error
    return candidate


def _unique_map(
    values: Iterable[Any],
    key: Callable[[Any], str],
    label: str,
    errors: list[tuple[str, str]],
) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for value in values:
        identifier = key(value)
        if identifier in result:
            errors.append(("E_DUPLICATE_ID", f"duplicate {label}: {identifier!r}"))
        else:
            result[identifier] = value
    return result


def _require_mapping(
    value: Any, label: str, errors: list[tuple[str, str]]
) -> dict[str, Any] | None:
    if not isinstance(value, dict):
        errors.append(("E_STRUCTURE", f"{label} must be an object"))
        return None
    return value


def _require_list(
    value: Any, label: str, errors: list[tuple[str, str]]
) -> list[Any] | None:
    if not isinstance(value, list):
        errors.append(("E_STRUCTURE", f"{label} must be an array"))
        return None
    return value


def _load_scenario_contract(
    root: Path, commit_sha: str | None = None
) -> tuple[dict[str, Any], dict[str, dict[str, Any]], dict[str, dict[str, Any]]]:
    relative_path = SCENARIO_CONTRACT.relative_to(ROOT).as_posix()
    if commit_sha is None:
        contract = strict_load(root / Path(relative_path))
    else:
        contract = strict_load_bytes(
            _git_bytes("cat-file", "blob", f"{commit_sha}:{relative_path}", root=root)
        )
    profiles = contract.get("gateProfiles")
    scenarios = contract.get("scenarios")
    if contract.get("schemaVersion") != SCHEMA_VERSION:
        raise StrictJsonError(
            f"scenario contract schemaVersion must be {SCHEMA_VERSION}"
        )
    if contract.get("authorizationStatus") != SCENARIO_AUTHORIZATION_STATUS:
        raise StrictJsonError(
            "scenario contract must declare trusted-tool-checkout authorization"
        )
    if not isinstance(profiles, dict) or not isinstance(scenarios, list):
        raise StrictJsonError("scenario contract lacks gateProfiles/scenarios")
    if set(profiles) != GATES:
        raise StrictJsonError(
            f"scenario contract gate profiles must equal {sorted(GATES)}"
        )
    scenario_map: dict[str, dict[str, Any]] = {}
    for scenario in scenarios:
        if not isinstance(scenario, dict) or not isinstance(scenario.get("id"), str):
            raise StrictJsonError("scenario contract contains an invalid scenario")
        identifier = scenario["id"]
        if identifier in scenario_map:
            raise StrictJsonError(f"duplicate scenario id: {identifier}")
        scenario_map[identifier] = scenario
    methods: dict[str, dict[str, Any]] = {}
    for method_name in ("performanceMethod", "macM2FunctionalMethod"):
        method = contract.get(method_name)
        if not isinstance(method, dict):
            raise StrictJsonError(f"scenario contract lacks {method_name}")
        methods[method_name] = method
    return profiles, scenario_map, methods


def _artifact_objects(document: dict[str, Any]) -> list[dict[str, Any]]:
    artifacts: list[dict[str, Any]] = []
    top_level = document.get("artifacts", [])
    if isinstance(top_level, list):
        artifacts.extend(item for item in top_level if isinstance(item, dict))

    commands = document.get("commands", [])
    if isinstance(commands, list):
        for command in commands:
            if not isinstance(command, dict):
                continue
            for field in ("stdoutArtifact", "stderrArtifact", "checksArtifact"):
                artifact = command.get(field)
                if isinstance(artifact, dict):
                    artifacts.append(artifact)

    coverage = document.get("coverage")
    if isinstance(coverage, dict) and isinstance(coverage.get("reportArtifact"), dict):
        artifacts.append(coverage["reportArtifact"])

    metrics = document.get("rawMetrics", [])
    if isinstance(metrics, list):
        for metric in metrics:
            if isinstance(metric, dict) and isinstance(metric.get("rawArtifact"), dict):
                artifacts.append(metric["rawArtifact"])
    ci = document.get("ci")
    if isinstance(ci, dict) and isinstance(ci.get("attestationArtifact"), dict):
        artifacts.append(ci["attestationArtifact"])
    reviewer = document.get("reviewer")
    if isinstance(reviewer, dict) and isinstance(reviewer.get("reviewArtifact"), dict):
        artifacts.append(reviewer["reviewArtifact"])
    return artifacts


def _validate_scenario_contract(
    profiles: dict[str, Any],
    scenarios: dict[str, dict[str, Any]],
    methods: dict[str, dict[str, Any]],
    errors: list[tuple[str, str]],
) -> None:
    expected_method = {
        "warmupSeconds": 30,
        "measurementSeconds": 120,
        "repetitions": 3,
        "minimumSamplesPerSecond": 1,
        "outlierRemoval": False,
        "rawSamplesRequired": True,
    }
    for method_name, method in methods.items():
        if not isinstance(method, dict):
            errors.append(("E_PERFORMANCE_METHOD", f"{method_name} is missing"))
            continue
        for field, expected in expected_method.items():
            if method.get(field) != expected:
                errors.append(
                    (
                        "E_PERFORMANCE_METHOD",
                        f"{method_name}.{field} must equal {expected!r}",
                    )
                )
        for field in ENVIRONMENT_FIELDS:
            value = method.get(field)
            if value is None or (isinstance(value, str) and not value):
                errors.append(
                    (
                        "E_PERFORMANCE_METHOD",
                        f"{method_name}.{field} must be declared",
                    )
                )

    for gate_id, profile in profiles.items():
        if gate_id not in GATES or not isinstance(profile, dict):
            errors.append(("E_PROFILE", f"invalid gate profile: {gate_id!r}"))
            continue

        required_scenarios = profile.get("requiredScenarioIds")
        if not isinstance(required_scenarios, list) or any(
            not isinstance(item, str) for item in required_scenarios
        ):
            errors.append(
                ("E_PROFILE", f"{gate_id}.requiredScenarioIds must be a string array")
            )
            continue
        if len(required_scenarios) != len(set(required_scenarios)):
            errors.append(
                ("E_PROFILE_SCENARIO_MAP", f"{gate_id} repeats a required scenario")
            )

        declared_for_gate = {
            identifier
            for identifier, scenario in scenarios.items()
            if gate_id in scenario.get("gateUsage", [])
        }
        required_set = set(required_scenarios)
        if declared_for_gate != required_set:
            errors.append(
                (
                    "E_PROFILE_SCENARIO_MAP",
                    f"{gate_id} scenario mapping differs; "
                    f"profileOnly={sorted(required_set - declared_for_gate)}, "
                    f"usageOnly={sorted(declared_for_gate - required_set)}",
                )
            )

        expected_prior = (
            [] if gate_id == "G0" else [GATE_SEQUENCE[int(gate_id[1:]) - 1]]
        )
        if profile.get("requiredPriorGateIds") != expected_prior:
            errors.append(
                (
                    "E_PROFILE_ORDER",
                    f"{gate_id}.requiredPriorGateIds must equal {expected_prior}",
                )
            )

    for identifier, scenario in scenarios.items():
        usages = scenario.get("gateUsage")
        if not isinstance(usages, list) or any(not isinstance(item, str) for item in usages):
            errors.append(
                ("E_PROFILE_SCENARIO_MAP", f"{identifier}.gateUsage must be a string array")
            )
        elif len(usages) != len(set(usages)):
            errors.append(
                ("E_PROFILE_SCENARIO_MAP", f"{identifier} repeats a gate usage")
            )

        assertions = scenario.get("requiredAssertions", [])
        if not isinstance(assertions, list) or any(
            not isinstance(item, str) or not item for item in assertions
        ):
            errors.append(
                (
                    "E_PROFILE_SCENARIO_ASSERTION",
                    f"{identifier}.requiredAssertions must be a string array",
                )
            )
        elif len(assertions) != len(set(assertions)):
            errors.append(
                (
                    "E_PROFILE_SCENARIO_ASSERTION",
                    f"{identifier} repeats a required assertion",
                )
            )

        thresholds = scenario.get("thresholds", {})
        if not isinstance(thresholds, dict):
            errors.append(
                ("E_PROFILE_SCENARIO_THRESHOLD", f"{identifier}.thresholds must be an object")
            )
            continue
        for metric_name, rules in thresholds.items():
            if not isinstance(metric_name, str) or not isinstance(rules, dict) or not rules:
                errors.append(
                    (
                        "E_PROFILE_SCENARIO_THRESHOLD",
                        f"{identifier} has an invalid threshold entry",
                    )
                )
                continue
            allowed = {"unit", "p95Max", "p99Max", "maximum", "minimum", "equals"}
            unknown = set(rules) - allowed
            unit = rules.get("unit")
            numeric_rules = {key: value for key, value in rules.items() if key != "unit"}
            if (
                unknown
                or not isinstance(unit, str)
                or not unit
                or not numeric_rules
                or any(
                isinstance(value, bool) or not isinstance(value, (int, float))
                    for value in numeric_rules.values()
                )
            ):
                errors.append(
                    (
                        "E_PROFILE_SCENARIO_THRESHOLD",
                        f"{identifier}.{metric_name} has invalid rules: {sorted(unknown)}",
                    )
                )

        method_ref = scenario.get("methodRef")
        has_thresholds = isinstance(thresholds, dict) and bool(thresholds)
        timing_units = {
            rules.get("unit")
            for rules in thresholds.values()
            if isinstance(rules, dict)
        } if isinstance(thresholds, dict) else set()
        requires_performance_method = bool(
            has_thresholds and timing_units.intersection({"ms", "bytes/tick"})
        )
        if method_ref is not None and method_ref not in methods:
            errors.append(
                (
                    "E_PERFORMANCE_METHOD",
                    f"{identifier}.methodRef is unknown: {method_ref!r}",
                )
            )
        if requires_performance_method and method_ref not in methods:
            errors.append(
                (
                    "E_PERFORMANCE_METHOD",
                    f"{identifier} timing thresholds require a declared method profile",
                )
            )


def _nearest_rank(samples: list[float], quantile: float) -> float:
    ordered = sorted(samples)
    index = max(0, math.ceil(quantile * len(ordered)) - 1)
    return ordered[index]


def _scenario_metric_name(scenario_id: str, metric_name: str) -> str:
    return f"scenario.{scenario_id}.{metric_name}"


def _scenario_assertion_name(scenario_id: str, assertion_id: str) -> str:
    return f"scenario.{scenario_id}.assertion.{assertion_id}"


def _numeric_sample_runs(
    metric: dict[str, Any] | None,
    metric_name: str,
    expected_unit: str,
    method_ref: str | None,
    performance_method: dict[str, Any] | None,
    errors: list[tuple[str, str]],
) -> list[list[float]] | None:
    if not isinstance(metric, dict):
        errors.append(("E_SCENARIO_METRIC", f"{metric_name}: metric is missing"))
        return None
    if metric.get("unit") != expected_unit:
        errors.append(
            (
                "E_SCENARIO_UNIT",
                f"{metric_name}: unit must be {expected_unit!r}",
            )
        )

    if performance_method is None:
        raw_runs = [metric.get("samples")]
        if metric.get("measurement") is not None:
            errors.append(
                (
                    "E_PERFORMANCE_METHOD",
                    f"{metric_name}: point metric must not declare measurement",
                )
            )
    else:
        measurement = metric.get("measurement")
        if not isinstance(measurement, dict):
            errors.append(
                (
                    "E_PERFORMANCE_METHOD",
                    f"{metric_name}: performance measurement is missing",
                )
            )
            return None
        expected_warmup = performance_method.get("warmupSeconds")
        expected_measurement = performance_method.get("measurementSeconds")
        expected_repetitions = performance_method.get("repetitions")
        minimum_rate = performance_method.get("minimumSamplesPerSecond")
        if measurement.get("methodRef") != method_ref:
            errors.append(
                (
                    "E_PERFORMANCE_METHOD",
                    f"{metric_name}: methodRef must be {method_ref!r}",
                )
            )
        if measurement.get("warmupSeconds") != expected_warmup:
            errors.append(
                (
                    "E_PERFORMANCE_METHOD",
                    f"{metric_name}: warmupSeconds must equal {expected_warmup!r}",
                )
            )
        runs = measurement.get("runs")
        if not isinstance(runs, list) or len(runs) != expected_repetitions:
            errors.append(
                (
                    "E_PERFORMANCE_METHOD",
                    f"{metric_name}: requires exactly {expected_repetitions} runs",
                )
            )
            return None
        raw_runs = []
        for expected_index, run in enumerate(runs, 1):
            if not isinstance(run, dict):
                errors.append(
                    ("E_PERFORMANCE_METHOD", f"{metric_name}: run must be an object")
                )
                return None
            if run.get("index") != expected_index:
                errors.append(
                    (
                        "E_PERFORMANCE_METHOD",
                        f"{metric_name}: run index must equal {expected_index}",
                    )
                )
            if run.get("measurementSeconds") != expected_measurement:
                errors.append(
                    (
                        "E_PERFORMANCE_METHOD",
                        f"{metric_name}: measurementSeconds must equal "
                        f"{expected_measurement!r}",
                    )
                )
            run_samples = run.get("samples")
            minimum_count = (
                int(expected_measurement * minimum_rate)
                if isinstance(expected_measurement, (int, float))
                and isinstance(minimum_rate, (int, float))
                else 1
            )
            if not isinstance(run_samples, list) or len(run_samples) < minimum_count:
                errors.append(
                    (
                        "E_PERFORMANCE_SAMPLE_COUNT",
                        f"{metric_name}: run {expected_index} requires at least "
                        f"{minimum_count} raw samples",
                    )
                )
            raw_runs.append(run_samples)

    numeric_runs: list[list[float]] = []
    integer_units = {"matches", "testers", "defects", "bytes/tick"}
    for run_index, samples in enumerate(raw_runs, 1):
        if not isinstance(samples, list) or not samples:
            errors.append(
                (
                    "E_SCENARIO_METRIC",
                    f"{metric_name}: run {run_index} has no raw samples",
                )
            )
            return None
        numeric: list[float] = []
        for sample in samples:
            if (
                isinstance(sample, bool)
                or not isinstance(sample, (int, float))
                or not math.isfinite(sample)
                or sample < 0
            ):
                errors.append(
                    (
                        "E_SCENARIO_METRIC",
                        f"{metric_name}: samples must be finite non-negative numbers",
                    )
                )
                return None
            if expected_unit in integer_units and (
                not isinstance(sample, int) or isinstance(sample, bool)
            ):
                errors.append(
                    (
                        "E_SCENARIO_UNIT",
                        f"{metric_name}: {expected_unit} samples must be integers",
                    )
                )
                return None
            numeric.append(float(sample))
        numeric_runs.append(numeric)
    return numeric_runs


def _validate_scenario_results(
    required_scenario_ids: list[str],
    scenarios: dict[str, dict[str, Any]],
    methods: dict[str, dict[str, Any]],
    criteria: dict[str, dict[str, Any]],
    implementation_checks: dict[str, dict[str, Any]],
    metrics: dict[str, dict[str, Any]],
    environments: dict[str, dict[str, Any]],
    errors: list[tuple[str, str]],
) -> None:
    for scenario_id in required_scenario_ids:
        scenario = scenarios.get(scenario_id)
        if scenario is None:
            errors.append(("E_GATE_SCENARIO", f"unknown required scenario: {scenario_id}"))
            continue

        bound_references: set[str] = set()
        for criterion in criteria.values():
            refs = criterion.get("evidenceRefs")
            if isinstance(refs, list) and f"scenario:{scenario_id}" in refs:
                bound_references.update(ref for ref in refs if isinstance(ref, str))

        if not bound_references:
            errors.append(
                ("E_GATE_SCENARIO", f"{scenario_id}: no criterion binds the scenario")
            )
            continue
        bound_check_ids = {
            reference.split(":", 1)[1]
            for reference in bound_references
            if reference.startswith("check:")
        }
        if not bound_check_ids:
            errors.append(
                (
                    "E_SCENARIO_COMMAND",
                    f"{scenario_id}: scenario evidence requires a check reference",
                )
            )
        method_ref = scenario.get("methodRef")
        method = methods.get(method_ref) if isinstance(method_ref, str) else None
        expected_duration = None
        if isinstance(method, dict):
            warmup = method.get("warmupSeconds")
            measurement = method.get("measurementSeconds")
            repetitions = method.get("repetitions")
            if all(
                isinstance(value, (int, float))
                for value in (warmup, measurement, repetitions)
            ):
                expected_duration = float(warmup) + float(measurement) * float(
                    repetitions
                )
        scenario_bound = False
        bound_environment_ids: set[str] = set()
        for check_id in bound_check_ids:
            command = implementation_checks.get(check_id)
            scenario_ids = (
                command.get("scenarioIds") if isinstance(command, dict) else None
            )
            if isinstance(scenario_ids, list) and scenario_id in scenario_ids:
                scenario_bound = True
                environment_id = command.get("environmentId")
                if isinstance(environment_id, str):
                    bound_environment_ids.add(environment_id)
                duration = command.get("durationSeconds")
                if (
                    expected_duration is not None
                    and (
                        isinstance(duration, bool)
                        or not isinstance(duration, (int, float))
                        or duration < expected_duration
                    )
                ):
                    errors.append(
                        (
                            "E_PERFORMANCE_DURATION",
                            f"{scenario_id}: bound command must run at least "
                            f"{expected_duration:g} seconds",
                        )
                    )
        if not scenario_bound:
            errors.append(
                (
                    "E_SCENARIO_COMMAND",
                    f"{scenario_id}: no bound implementation check declares the scenario",
                )
            )

        assertions = scenario.get("requiredAssertions", [])
        for assertion_id in assertions if isinstance(assertions, list) else []:
            metric_name = _scenario_assertion_name(scenario_id, assertion_id)
            if f"metric:{metric_name}" not in bound_references:
                errors.append(
                    (
                        "E_SCENARIO_METRIC_REF",
                        f"{scenario_id}: assertion metric is not bound: {metric_name}",
                    )
                )
            metric = metrics.get(metric_name)
            samples = metric.get("samples") if isinstance(metric, dict) else None
            if (
                not isinstance(metric, dict)
                or metric.get("unit") != "bool"
                or samples != [1]
            ):
                errors.append(
                    (
                        "E_SCENARIO_ASSERTION",
                        f"{scenario_id}: assertion did not pass: {assertion_id}",
                    )
                )

        thresholds = scenario.get("thresholds", {})
        for contract_metric, rules in thresholds.items() if isinstance(thresholds, dict) else []:
            metric_name = _scenario_metric_name(scenario_id, contract_metric)
            if f"metric:{metric_name}" not in bound_references:
                errors.append(
                    (
                        "E_SCENARIO_METRIC_REF",
                        f"{scenario_id}: threshold metric is not bound: {metric_name}",
                    )
                )
            if not isinstance(rules, dict):
                continue
            expected_unit = rules.get("unit")
            if not isinstance(expected_unit, str):
                continue
            method_ref = scenario.get("methodRef")
            performance_method = methods.get(method_ref) if isinstance(method_ref, str) else None
            metric = metrics.get(metric_name)
            if performance_method is not None and isinstance(metric, dict):
                measurement = metric.get("measurement")
                if isinstance(measurement, dict):
                    environment_id = measurement.get("environmentId")
                    environment = (
                        environments.get(environment_id)
                        if isinstance(environment_id, str)
                        else None
                    )
                    if environment is None:
                        errors.append(
                            (
                                "E_ENVIRONMENT_REF",
                                f"{metric_name}: unknown measurement environmentId: "
                                f"{environment_id!r}",
                            )
                        )
                    else:
                        if bound_environment_ids and (
                            environment_id not in bound_environment_ids
                        ):
                            errors.append(
                                (
                                    "E_ENVIRONMENT_MISMATCH",
                                    f"{metric_name}: measurement environment "
                                    f"{environment_id!r} differs from the bound "
                                    f"command environment(s) "
                                    f"{sorted(bound_environment_ids)}",
                                )
                            )
                        for field in ENVIRONMENT_FIELDS:
                            if environment.get(field) != performance_method.get(field):
                                errors.append(
                                    (
                                        "E_ENVIRONMENT_PROFILE",
                                        f"{metric_name}: environment {environment_id!r} "
                                        f"field {field} must equal "
                                        f"{method_ref!r} value "
                                        f"{performance_method.get(field)!r}",
                                    )
                                )
            sample_runs = _numeric_sample_runs(
                metric,
                metric_name,
                expected_unit,
                method_ref if isinstance(method_ref, str) else None,
                performance_method,
                errors,
            )
            if sample_runs is None:
                continue
            combined = [sample for run in sample_runs for sample in run]
            evaluated_runs = [(f"run-{index}", run) for index, run in enumerate(sample_runs, 1)]
            if len(sample_runs) > 1:
                evaluated_runs.append(("combined", combined))
            for rule, threshold in rules.items():
                if rule == "unit":
                    continue
                for run_label, samples in evaluated_runs:
                    observations = {
                        "p95Max": _nearest_rank(samples, 0.95),
                        "p99Max": _nearest_rank(samples, 0.99),
                        "maximum": max(samples),
                        "minimum": min(samples),
                    }
                    if rule == "equals":
                        passed = all(sample == float(threshold) for sample in samples)
                        observed: float | list[float] = samples
                    elif rule in {"p95Max", "p99Max", "maximum"}:
                        observed = observations[rule]
                        passed = observed <= float(threshold)
                    elif rule == "minimum":
                        observed = observations[rule]
                        passed = observed >= float(threshold)
                    else:
                        continue
                    if not passed:
                        errors.append(
                            (
                                "E_SCENARIO_THRESHOLD",
                                f"{scenario_id}.{contract_metric}.{rule}.{run_label}: "
                                f"observed {observed!r}, required {threshold!r}",
                            )
                        )


def _validate_previous_chain(
    first_path: str,
    gate_id: str,
    root: Path,
    errors: list[tuple[str, str]],
) -> None:
    seen: set[str] = set()
    current: str | None = first_path
    while current is not None:
        if current in seen:
            errors.append(("E_PREVIOUS_CYCLE", f"evidence chain contains cycle at {current}"))
            return
        seen.add(current)
        try:
            path = _safe_repo_path(root, current)
        except ValueError as error:
            errors.append(("E_PREVIOUS_PATH", f"{current}: {error}"))
            return
        if not path.is_file():
            errors.append(("E_PREVIOUS_MISSING", f"previous evidence missing: {current}"))
            return
        try:
            previous = strict_load(path)
        except (OSError, StrictJsonError) as error:
            errors.append(("E_PREVIOUS_INVALID", f"{current}: {error}"))
            return
        if not isinstance(previous, dict) or previous.get("gateId") != gate_id:
            errors.append(("E_PREVIOUS_GATE", f"{current} does not belong to {gate_id}"))
            return
        attempt = previous.get("attempt")
        if not isinstance(attempt, dict) or attempt.get("evidencePath") != current:
            errors.append(("E_PREVIOUS_PATH", f"{current} does not declare its own path"))
            return
        predecessor = attempt.get("previousEvidence")
        if predecessor is not None and not isinstance(predecessor, str):
            errors.append(("E_PREVIOUS_INVALID", f"{current} has invalid predecessor"))
            return
        current = predecessor


def _validate_trust_context(
    trust_context_path: Path | None,
    document: dict[str, Any],
    evidence_path: Path,
    root: Path,
    errors: list[tuple[str, str]],
    *,
    gate_id: str,
) -> None:
    """Authorize a pass only inside the protected GitHub workflow."""

    if trust_context_path is None:
        errors.append(
            (
                "E_TRUST_CONTEXT",
                "pass evidence requires --trust-context from protected CI",
            )
        )
        return
    resolved_context = trust_context_path.resolve()
    try:
        resolved_context.relative_to(root.resolve())
    except ValueError:
        pass
    else:
        errors.append(
            ("E_TRUST_CONTEXT", "trust context must be generated outside the repository")
        )
        return
    try:
        raw_context = resolved_context.read_bytes()
        context = strict_load_bytes(raw_context)
    except (OSError, StrictJsonError) as error:
        errors.append(("E_TRUST_CONTEXT", str(error)))
        return
    if not isinstance(context, dict):
        errors.append(("E_TRUST_CONTEXT", "trust context root must be an object"))
        return

    expected_keys = {
        "schemaVersion",
        "repository",
        "workflowPath",
        "authorizingRunId",
        "authorizingRunAttempt",
        "authorizingJob",
        "subjectCommitSha",
        "subjectTreeSha",
        "evidencePath",
        "evidenceSha256",
        "evidenceCiRunId",
        "evidenceCiJobId",
        "ciAttestationSha256",
        "reviewerId",
        "reviewArtifactSha256",
        "trustedToolCommitSha",
        "authorizedEvidence",
    }
    if set(context) != expected_keys:
        errors.append(
            (
                "E_TRUST_CONTEXT",
                f"trust context fields differ: {sorted(set(context) ^ expected_keys)}",
            )
        )
        return

    subject = document.get("subject", {})
    attempt = document.get("attempt", {})
    ci = document.get("ci", {})
    reviewer = document.get("reviewer", {})
    review_artifact = (
        reviewer.get("reviewArtifact") if isinstance(reviewer, dict) else None
    )
    ci_artifact = ci.get("attestationArtifact") if isinstance(ci, dict) else None
    try:
        evidence_digest = sha256_file(evidence_path)
    except OSError as error:
        errors.append(("E_TRUST_CONTEXT", f"cannot hash evidence: {error}"))
        evidence_digest = None
    expected_values = {
        "schemaVersion": TRUST_CONTEXT_VERSION,
        "repository": "VibecodingGermany/Project_Nova",
        "workflowPath": ".github/workflows/quality-gate.yml",
        "authorizingJob": "gate-evidence-authorize",
        "subjectCommitSha": subject.get("commitSha"),
        "subjectTreeSha": subject.get("treeSha"),
        "evidencePath": attempt.get("evidencePath"),
        "evidenceSha256": evidence_digest,
        "evidenceCiRunId": ci.get("runId"),
        "evidenceCiJobId": ci.get("jobId"),
        "ciAttestationSha256": (
            ci_artifact.get("sha256") if isinstance(ci_artifact, dict) else None
        ),
        "reviewerId": reviewer.get("id"),
        "reviewArtifactSha256": (
            review_artifact.get("sha256")
            if isinstance(review_artifact, dict)
            else None
        ),
    }
    for field, expected in expected_values.items():
        if context.get(field) != expected:
            errors.append(
                (
                    "E_TRUST_CONTEXT",
                    f"{field} does not match evidence: "
                    f"{context.get(field)!r} != {expected!r}",
                )
            )

    trust_bundle = document.get("trustBundle")
    trusted_commit = (
        trust_bundle.get("trustedCommitSha")
        if isinstance(trust_bundle, dict)
        else None
    )
    if context.get("trustedToolCommitSha") != trusted_commit:
        errors.append(
            (
                "E_TRUST_CONTEXT",
                "trustedToolCommitSha does not match evidence trustBundle: "
                f"{context.get('trustedToolCommitSha')!r} != {trusted_commit!r}",
            )
        )

    chain = context.get("authorizedEvidence")
    if not isinstance(chain, list) or any(
        not isinstance(entry, dict) for entry in chain
    ):
        errors.append(
            (
                "E_AUTHORIZATION_CHAIN",
                "authorizedEvidence must be an array of objects",
            )
        )
        return
    expected_length = GATE_SEQUENCE.index(gate_id) + 1
    if len(chain) != expected_length:
        errors.append(
            (
                "E_AUTHORIZATION_CHAIN",
                f"authorizedEvidence must contain exactly {expected_length} "
                f"entries (G0..{gate_id}), got {len(chain)}",
            )
        )
        return
    for index, entry in enumerate(chain):
        if set(entry) != AUTHORIZED_EVIDENCE_KEYS:
            errors.append(
                (
                    "E_AUTHORIZATION_CHAIN",
                    f"entry {index} fields differ: "
                    f"{sorted(set(entry) ^ AUTHORIZED_EVIDENCE_KEYS)}",
                )
            )
            return
        if entry.get("gateId") != GATE_SEQUENCE[index]:
            errors.append(
                (
                    "E_AUTHORIZATION_CHAIN",
                    f"entry {index} must be {GATE_SEQUENCE[index]}, "
                    f"got {entry.get('gateId')!r}",
                )
            )
            return

    current_entry = chain[-1]
    current_expected = {
        "evidencePath": attempt.get("evidencePath"),
        "evidenceSha256": evidence_digest,
        "subjectCommitSha": subject.get("commitSha"),
        "subjectTreeSha": subject.get("treeSha"),
        "ciRunId": ci.get("runId"),
        "ciJobId": ci.get("jobId"),
        "ciAttestationSha256": (
            ci_artifact.get("sha256") if isinstance(ci_artifact, dict) else None
        ),
        "reviewArtifactSha256": (
            review_artifact.get("sha256")
            if isinstance(review_artifact, dict)
            else None
        ),
    }
    for field, expected in current_expected.items():
        if current_entry.get(field) != expected:
            errors.append(
                (
                    "E_AUTHORIZATION_CHAIN",
                    f"current gate entry {field} does not match evidence: "
                    f"{current_entry.get(field)!r} != {expected!r}",
                )
            )

    for entry in chain[:-1]:
        entry_gate = entry.get("gateId")
        if (
            entry.get("subjectCommitSha") != subject.get("commitSha")
            or entry.get("subjectTreeSha") != subject.get("treeSha")
        ):
            errors.append(
                (
                    "E_AUTHORIZATION_CHAIN",
                    f"{entry_gate}: prior entry must prove the same subject "
                    "commit/tree",
                )
            )
            continue
        raw_prior_path = entry.get("evidencePath")
        if not isinstance(raw_prior_path, str):
            errors.append(
                ("E_AUTHORIZATION_CHAIN", f"{entry_gate}: evidencePath missing")
            )
            continue
        try:
            prior_path = _safe_repo_path(root, raw_prior_path)
        except ValueError as error:
            errors.append(
                ("E_AUTHORIZATION_CHAIN", f"{raw_prior_path}: {error}")
            )
            continue
        if not prior_path.is_file():
            errors.append(
                (
                    "E_AUTHORIZATION_CHAIN",
                    f"authorized prior evidence missing: {raw_prior_path}",
                )
            )
            continue
        if sha256_file(prior_path) != entry.get("evidenceSha256"):
            errors.append(
                (
                    "E_AUTHORIZATION_CHAIN",
                    f"{raw_prior_path}: evidence SHA-256 mismatch",
                )
            )
            continue
        try:
            prior_document = strict_load(prior_path)
        except (OSError, StrictJsonError) as error:
            errors.append(
                ("E_AUTHORIZATION_CHAIN", f"{raw_prior_path}: {error}")
            )
            continue
        prior_ci = prior_document.get("ci")
        prior_ci_artifact = (
            prior_ci.get("attestationArtifact")
            if isinstance(prior_ci, dict)
            else None
        )
        prior_reviewer = prior_document.get("reviewer")
        prior_review_artifact = (
            prior_reviewer.get("reviewArtifact")
            if isinstance(prior_reviewer, dict)
            else None
        )
        prior_verdict = prior_document.get("verdict")
        prior_expected = {
            "gateId": prior_document.get("gateId"),
            "ciRunId": prior_ci.get("runId") if isinstance(prior_ci, dict) else None,
            "ciJobId": prior_ci.get("jobId") if isinstance(prior_ci, dict) else None,
            "ciAttestationSha256": (
                prior_ci_artifact.get("sha256")
                if isinstance(prior_ci_artifact, dict)
                else None
            ),
            "reviewArtifactSha256": (
                prior_review_artifact.get("sha256")
                if isinstance(prior_review_artifact, dict)
                else None
            ),
        }
        if (
            not isinstance(prior_verdict, dict)
            or prior_verdict.get("result") != "pass"
        ):
            errors.append(
                (
                    "E_AUTHORIZATION_CHAIN",
                    f"{raw_prior_path}: prior gate was not authorized as pass",
                )
            )
        for field, actual in prior_expected.items():
            if entry.get(field) != actual:
                errors.append(
                    (
                        "E_AUTHORIZATION_CHAIN",
                        f"{raw_prior_path}: chain entry {field} does not match "
                        f"prior evidence: {entry.get(field)!r} != {actual!r}",
                    )
                )

    environment_values = {
        "GITHUB_ACTIONS": "true",
        "GITHUB_REPOSITORY": "VibecodingGermany/Project_Nova",
        "GITHUB_RUN_ID": str(context.get("authorizingRunId")),
        "GITHUB_RUN_ATTEMPT": str(context.get("authorizingRunAttempt")),
        "GITHUB_JOB": "gate-evidence-authorize",
        "GITHUB_WORKFLOW_REF": (
            "VibecodingGermany/Project_Nova/"
            ".github/workflows/quality-gate.yml@refs/heads/main"
        ),
        "NOVA_TRUST_CONTEXT_SHA256": sha256_bytes(raw_context),
    }
    for name, expected in environment_values.items():
        if os.environ.get(name) != expected:
            errors.append(
                (
                    "E_TRUST_CONTEXT",
                    f"protected environment {name} does not match trust context",
                )
            )


def _node_version() -> str | None:
    try:
        result = subprocess.run(
            ["node", "--version"],
            check=False,
            stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL,
            text=True,
            timeout=30,
        )
    except (OSError, subprocess.TimeoutExpired):
        return None
    if result.returncode != 0:
        return None
    return result.stdout.strip() or None


def _verify_trusted_tooling(
    trusted_checkout: Path,
    trust_bundle: dict[str, Any],
    root: Path,
    errors: list[tuple[str, str]],
) -> None:
    """Verify the subject-independent trusted tool checkout (D-064)."""

    trusted_root = trusted_checkout.resolve()
    if not trusted_root.is_dir():
        errors.append(
            (
                "E_TRUSTED_TOOL",
                f"trusted tool checkout is missing: {trusted_checkout}",
            )
        )
        return
    subject_root = root.resolve()
    if (
        trusted_root == subject_root
        or trusted_root in subject_root.parents
        or subject_root in trusted_root.parents
    ):
        errors.append(
            (
                "E_TRUSTED_TOOL",
                "trusted tool checkout must be independent of the subject "
                "repository",
            )
        )
        return
    try:
        head = _git("rev-parse", "HEAD", root=trusted_root)
        status = _git("status", "--porcelain", root=trusted_root)
    except ValueError as error:
        errors.append(
            (
                "E_TRUSTED_TOOL",
                f"trusted tool checkout is not a git worktree: {error}",
            )
        )
        return
    if status:
        errors.append(
            ("E_TRUSTED_TOOL", "trusted tool checkout has uncommitted changes")
        )
    declared_commit = trust_bundle.get("trustedCommitSha")
    if head != declared_commit:
        errors.append(
            (
                "E_TRUSTED_TOOL",
                f"trusted checkout HEAD {head} does not match declared "
                f"trustedCommitSha {declared_commit!r}",
            )
        )
    components = trust_bundle.get("components")
    if not isinstance(components, dict):
        errors.append(("E_TRUSTED_TOOL", "trustBundle.components must be an object"))
        return
    for component_id, canonical_path in TRUST_BUNDLE_COMPONENTS.items():
        component = components.get(component_id)
        if not isinstance(component, dict):
            errors.append(
                ("E_TRUSTED_TOOL", f"trustBundle component missing: {component_id}")
            )
            continue
        if component.get("path") != canonical_path:
            errors.append(
                (
                    "E_TRUSTED_TOOL",
                    f"{component_id}: path must be {canonical_path!r}",
                )
            )
            continue
        candidate = trusted_root / Path(*PurePosixPath(canonical_path).parts)
        if not candidate.is_file():
            errors.append(
                ("E_TRUSTED_TOOL", f"trusted checkout lacks {canonical_path}")
            )
            continue
        if sha256_file(candidate) != component.get("trustedSha256"):
            errors.append(
                (
                    "E_TRUSTED_TOOL",
                    f"{component_id}: trusted SHA-256 mismatch: {canonical_path}",
                )
            )
    if not (trusted_root / "quality/node_modules/ajv/dist/2020.js").is_file():
        errors.append(
            (
                "E_TRUSTED_TOOL",
                "trusted checkout lacks pinned npm dependencies "
                "(run npm ci --ignore-scripts)",
            )
        )
    node_version = _node_version()
    declared_node = trust_bundle.get("nodeVersion")
    if node_version is None:
        errors.append(
            ("E_TRUSTED_TOOL", "node is unavailable for the trusted Ajv stack")
        )
    elif node_version != declared_node:
        errors.append(
            (
                "E_TRUSTED_TOOL",
                f"node version {node_version!r} does not match declared "
                f"{declared_node!r}",
            )
        )


def validate_document(
    document: Any,
    evidence_path: Path,
    *,
    root: Path = ROOT,
    verify_files: bool = True,
    verify_git: bool = True,
    profiles: dict[str, Any] | None = None,
    scenario_definitions: dict[str, dict[str, Any]] | None = None,
    scenario_methods: dict[str, dict[str, Any]] | None = None,
    evidence_schema_bytes: bytes | None = None,
    trust_context_path: Path | None = None,
    trusted_checkout: Path | None = None,
    require_trust: bool = True,
    _validation_stack: set[str] | None = None,
) -> list[tuple[str, str]]:
    """Return stable `(code, message)` errors for one parsed evidence object."""

    errors: list[tuple[str, str]] = []
    data = _require_mapping(document, "root", errors)
    if data is None:
        return errors
    if data.get("schemaVersion") != SCHEMA_VERSION:
        errors.append(
            ("E_SCHEMA_VERSION", f"schemaVersion must be {SCHEMA_VERSION}")
        )

    subject_candidate = data.get("subject")
    commit_candidate = (
        subject_candidate.get("commitSha")
        if isinstance(subject_candidate, dict)
        else None
    )
    schema_validator_path = SCHEMA_VALIDATOR
    schema_working_directory = ROOT
    if trusted_checkout is not None:
        trusted_root = trusted_checkout.resolve()
        schema_validator_path = trusted_root / Path(
            "quality/scripts/validate_evidence_schema.mjs"
        )
        schema_working_directory = trusted_root
    if evidence_schema_bytes is None:
        try:
            if trusted_checkout is not None:
                # D-064: the schema comes from the subject-independent trusted
                # tool checkout, never from the subject.
                evidence_schema_bytes = (
                    trusted_root / Path("quality/schemas/GateEvidence.schema.json")
                ).read_bytes()
            elif (
                verify_git
                and isinstance(commit_candidate, str)
                and re.fullmatch(r"[0-9a-f]{40,64}", commit_candidate)
            ):
                relative_schema = EVIDENCE_SCHEMA.relative_to(ROOT).as_posix()
                evidence_schema_bytes = _git_bytes(
                    "cat-file",
                    "blob",
                    f"{commit_candidate}:{relative_schema}",
                    root=root,
                )
            else:
                evidence_schema_bytes = EVIDENCE_SCHEMA.read_bytes()
        except (OSError, ValueError) as error:
            error_code = (
                "E_TRUSTED_TOOL" if trusted_checkout is not None else "E_JSON_SCHEMA"
            )
            errors.append((error_code, f"cannot load evidence schema: {error}"))
    if evidence_schema_bytes is not None:
        for message in _schema_validation_errors(
            data,
            evidence_schema_bytes,
            validator_path=schema_validator_path,
            working_directory=schema_working_directory,
        ):
            errors.append(("E_JSON_SCHEMA", message))

    required_objects = (
        "subject",
        "attempt",
        "scope",
        "content",
        "trustBundle",
        "toolchains",
        "coverage",
        "ci",
        "reviewer",
        "verdict",
    )
    objects: dict[str, dict[str, Any]] = {}
    for name in required_objects:
        value = _require_mapping(data.get(name), name, errors)
        if value is not None:
            objects[name] = value

    required_arrays = (
        "priorGateEvidence",
        "environments",
        "commands",
        "rawMetrics",
        "artifacts",
        "criteria",
    )
    arrays: dict[str, list[Any]] = {}
    for name in required_arrays:
        value = _require_list(data.get(name), name, errors)
        if value is not None:
            arrays[name] = value

    if len(objects) != len(required_objects) or len(arrays) != len(required_arrays):
        return errors

    gate_id = data.get("gateId")
    if gate_id not in GATES:
        errors.append(("E_GATE", f"unsupported gateId: {gate_id!r}"))
        return errors

    subject = objects["subject"]
    attempt = objects["attempt"]
    content = objects["content"]
    trust_bundle = objects["trustBundle"]
    coverage = objects["coverage"]
    ci = objects["ci"]
    reviewer = objects["reviewer"]
    verdict = objects["verdict"]

    commit_sha = subject.get("commitSha")
    tree_sha = subject.get("treeSha")
    attempt_id = attempt.get("id")
    declared_path = attempt.get("evidencePath")

    if (
        profiles is None
        or scenario_definitions is None
        or scenario_methods is None
    ):
        try:
            contract_commit = (
                commit_sha
                if verify_git
                and isinstance(commit_sha, str)
                and re.fullmatch(r"[0-9a-f]{40,64}", commit_sha)
                else None
            )
            profiles, scenario_definitions, scenario_methods = _load_scenario_contract(
                root, contract_commit
            )
        except (OSError, StrictJsonError, ValueError) as error:
            errors.append(("E_PROFILE", str(error)))
            return errors
    _validate_scenario_contract(
        profiles, scenario_definitions, scenario_methods, errors
    )
    profile = profiles.get(gate_id)
    if not isinstance(profile, dict):
        errors.append(("E_PROFILE", f"missing profile for {gate_id}"))
        return errors

    if subject.get("dirty") is not False:
        errors.append(("E_DIRTY", "subject.dirty must be false"))
    if not isinstance(commit_sha, str) or not re.fullmatch(r"[0-9a-f]{40,64}", commit_sha):
        errors.append(("E_COMMIT", "subject.commitSha is invalid"))
    if not isinstance(tree_sha, str) or not re.fullmatch(r"[0-9a-f]{40,64}", tree_sha):
        errors.append(("E_TREE", "subject.treeSha is invalid"))
    if not isinstance(attempt_id, str) or ATTEMPT_RE.fullmatch(attempt_id) is None:
        errors.append(("E_ATTEMPT", "attempt.id is invalid"))
    if isinstance(commit_sha, str) and isinstance(attempt_id, str):
        expected_path = (
            f"quality/evidence/{gate_id}/{commit_sha}/{attempt_id}/GateEvidence.json"
        )
        if declared_path != expected_path:
            errors.append(
                ("E_EVIDENCE_PATH", f"declared path must be {expected_path!r}")
            )
        try:
            actual_path = evidence_path.resolve().relative_to(root.resolve()).as_posix()
        except ValueError:
            actual_path = ""
        if verify_files and actual_path != expected_path:
            errors.append(
                ("E_EVIDENCE_LOCATION", f"actual path must be {expected_path!r}")
            )

    previous = attempt.get("previousEvidence")
    if previous == declared_path:
        errors.append(("E_PREVIOUS_CYCLE", "evidence cannot point to itself"))
    elif previous is not None:
        if not isinstance(previous, str):
            errors.append(("E_PREVIOUS_INVALID", "previousEvidence must be null or path"))
        elif verify_files:
            _validate_previous_chain(previous, gate_id, root, errors)

    if verify_git and isinstance(commit_sha, str) and isinstance(tree_sha, str):
        try:
            resolved_commit = _git("rev-parse", f"{commit_sha}^{{commit}}", root=root)
            resolved_tree = _git("rev-parse", f"{commit_sha}^{{tree}}", root=root)
        except ValueError as error:
            errors.append(("E_GIT_SUBJECT", str(error)))
        else:
            if resolved_commit != commit_sha:
                errors.append(("E_COMMIT", "commitSha must be the full commit SHA"))
            if resolved_tree != tree_sha:
                errors.append(("E_TREE", "treeSha does not match subject commit"))

    prior_gate_items = arrays["priorGateEvidence"]
    prior_gate_map = _unique_map(
        (item for item in prior_gate_items if isinstance(item, dict)),
        lambda item: str(item.get("gateId")),
        "prior gate id",
        errors,
    )
    required_prior_gate_ids = profile.get("requiredPriorGateIds", [])
    if not isinstance(required_prior_gate_ids, list):
        errors.append(("E_PROFILE_ORDER", "requiredPriorGateIds must be an array"))
        required_prior_gate_ids = []
    if set(prior_gate_map) != set(required_prior_gate_ids):
        errors.append(
            (
                "E_GATE_ORDER",
                f"{gate_id} prior-gate mismatch; "
                f"required={sorted(required_prior_gate_ids)}, "
                f"declared={sorted(prior_gate_map)}",
            )
        )

    validation_stack = set() if _validation_stack is None else set(_validation_stack)
    if isinstance(declared_path, str):
        if declared_path in validation_stack:
            errors.append(
                ("E_GATE_ORDER_CYCLE", f"prior-gate chain cycles at {declared_path}")
            )
        validation_stack.add(declared_path)

    if verify_files:
        for prior_gate_id, prior_ref in prior_gate_map.items():
            raw_path = prior_ref.get("evidencePath")
            expected_digest = prior_ref.get("sha256")
            if not isinstance(raw_path, str) or not isinstance(expected_digest, str):
                errors.append(
                    ("E_PRIOR_GATE", f"{prior_gate_id}: path or SHA-256 is missing")
                )
                continue
            if raw_path in validation_stack:
                errors.append(
                    ("E_GATE_ORDER_CYCLE", f"prior-gate chain cycles at {raw_path}")
                )
                continue
            try:
                prior_path = _safe_repo_path(root, raw_path)
            except ValueError as error:
                errors.append(("E_PRIOR_GATE", f"{raw_path}: {error}"))
                continue
            if not prior_path.is_file():
                errors.append(("E_PRIOR_GATE_MISSING", f"missing prior gate: {raw_path}"))
                continue
            if sha256_file(prior_path) != expected_digest:
                errors.append(
                    ("E_PRIOR_GATE_DIGEST", f"SHA-256 mismatch: {raw_path}")
                )
                continue
            try:
                prior_document = strict_load(prior_path)
            except (OSError, StrictJsonError) as error:
                errors.append(("E_PRIOR_GATE", f"{raw_path}: {error}"))
                continue
            if not isinstance(prior_document, dict):
                errors.append(("E_PRIOR_GATE", f"{raw_path}: root must be an object"))
                continue
            prior_schema_messages = (
                _schema_validation_errors(
                    prior_document,
                    evidence_schema_bytes,
                    validator_path=schema_validator_path,
                    working_directory=schema_working_directory,
                )
                if evidence_schema_bytes is not None
                else ["subject evidence schema is unavailable"]
            )
            if prior_schema_messages:
                for message in prior_schema_messages:
                    errors.append(
                        (
                            "E_PRIOR_GATE_SCHEMA",
                            f"{raw_path}: {message}",
                        )
                    )
                continue
            if prior_document.get("gateId") != prior_gate_id:
                errors.append(
                    ("E_PRIOR_GATE", f"{raw_path}: gateId does not match {prior_gate_id}")
                )
            prior_attempt = prior_document.get("attempt")
            if (
                not isinstance(prior_attempt, dict)
                or prior_attempt.get("evidencePath") != raw_path
            ):
                errors.append(
                    ("E_PRIOR_GATE", f"{raw_path}: evidencePath is not self-consistent")
                )
            prior_subject = prior_document.get("subject")
            if not isinstance(prior_subject, dict) or (
                prior_subject.get("commitSha") != commit_sha
                or prior_subject.get("treeSha") != tree_sha
            ):
                errors.append(
                    (
                        "E_PRIOR_GATE_SUBJECT",
                        f"{raw_path}: prior gate must prove the same subject commit/tree",
                    )
                )
            prior_verdict = prior_document.get("verdict")
            if not isinstance(prior_verdict, dict) or prior_verdict.get("result") != "pass":
                errors.append(
                    ("E_PRIOR_GATE_VERDICT", f"{raw_path}: prior gate did not pass")
                )
            nested_errors = validate_document(
                prior_document,
                prior_path,
                root=root,
                verify_files=verify_files,
                verify_git=verify_git,
                profiles=profiles,
                scenario_definitions=scenario_definitions,
                scenario_methods=scenario_methods,
                evidence_schema_bytes=evidence_schema_bytes,
                trusted_checkout=trusted_checkout,
                require_trust=False,
                _validation_stack=validation_stack,
            )
            for nested_code, nested_message in nested_errors:
                errors.append(
                    (
                        "E_PRIOR_GATE_INVALID",
                        f"{raw_path}: {nested_code}: {nested_message}",
                    )
                )

    bundle_components = trust_bundle.get("components")
    for component_id, (path_field, digest_field) in CONTENT_COMPONENT_FIELDS.items():
        component = (
            bundle_components.get(component_id)
            if isinstance(bundle_components, dict)
            else None
        )
        if not isinstance(component, dict):
            errors.append(
                ("E_TRUST_BUNDLE", f"trustBundle component missing: {component_id}")
            )
            continue
        canonical_path = TRUST_BUNDLE_COMPONENTS[component_id]
        if component.get("path") != canonical_path:
            errors.append(
                (
                    "E_TRUST_BUNDLE",
                    f"{component_id}: path must be {canonical_path!r}",
                )
            )
        if component.get("subjectSha256") != content.get(digest_field):
            errors.append(
                (
                    "E_TRUST_BUNDLE",
                    f"{component_id}: subjectSha256 must equal "
                    f"content.{digest_field}",
                )
            )

    if trusted_checkout is not None:
        _verify_trusted_tooling(trusted_checkout, trust_bundle, root, errors)

    for path_field, digest_field in CONTENT_COMPONENT_FIELDS.values():
        raw_path = content.get(path_field)
        expected_digest = content.get(digest_field)
        if not isinstance(raw_path, str) or not isinstance(expected_digest, str):
            errors.append(("E_CONTENT", f"{path_field}/{digest_field} missing"))
            continue
        try:
            _safe_repo_path(root, raw_path)
        except ValueError as error:
            errors.append(("E_CONTENT_PATH", f"{raw_path}: {error}"))
            continue
        if verify_git and isinstance(commit_sha, str):
            try:
                subject_bytes = _git_bytes(
                    "cat-file", "blob", f"{commit_sha}:{raw_path}", root=root
                )
            except ValueError as error:
                errors.append(("E_CONTENT_MISSING", f"{raw_path}: {error}"))
            else:
                if sha256_bytes(subject_bytes) != expected_digest:
                    errors.append(
                        (
                            "E_CONTENT_DIGEST",
                            f"subject-blob SHA-256 mismatch: {raw_path}",
                        )
                    )
        elif verify_files:
            path = _safe_repo_path(root, raw_path)
            if not path.is_file():
                errors.append(("E_CONTENT_MISSING", f"missing content file: {raw_path}"))
            elif sha256_file(path) != expected_digest:
                errors.append(("E_CONTENT_DIGEST", f"SHA-256 mismatch: {raw_path}"))

    commands = _unique_map(
        (item for item in arrays["commands"] if isinstance(item, dict)),
        lambda item: str(item.get("id")),
        "command id",
        errors,
    )
    implementation_checks: dict[str, dict[str, Any]] = {}
    reviewer_checks: dict[str, dict[str, Any]] = {}
    for command_id, command in commands.items():
        conclusion = command.get("conclusion")
        exit_code = command.get("exitCode")
        check_count = command.get("checkCount")
        expected_checks = command.get("expectedCheckCount")
        check_items = command.get("checks")
        executor = command.get("executor")
        if conclusion == "success" and exit_code != 0:
            errors.append(
                ("E_COMMAND_EXIT", f"{command_id}: success requires exitCode 0")
            )
        if conclusion == "success" and (
            not isinstance(check_count, int)
            or isinstance(check_count, bool)
            or check_count <= 0
            or not isinstance(expected_checks, int)
            or isinstance(expected_checks, bool)
            or expected_checks <= 0
        ):
            errors.append(
                ("E_COMMAND_COUNT", f"{command_id}: successful checks must be positive")
            )
        if (
            not isinstance(check_items, list)
            or len(check_items) != check_count
            or check_count != expected_checks
            or expected_checks != 1
        ):
            errors.append(
                (
                    "E_COMMAND_COUNT",
                    f"{command_id}: exactly one artifact-bound check is required",
                )
            )
            continue
        check = check_items[0]
        if not isinstance(check, dict) or not isinstance(check.get("id"), str):
            errors.append(("E_COMMAND_CHECK", f"{command_id}: invalid check result"))
            continue
        check_id = check["id"]
        if conclusion == "success" and check.get("result") != "pass":
            errors.append(
                (
                    "E_COMMAND_CHECK",
                    f"{command_id}: successful command requires a passing check",
                )
            )
        target = (
            implementation_checks
            if executor == "implementation"
            else reviewer_checks
            if executor == "reviewer"
            else None
        )
        if target is None:
            errors.append(("E_COMMAND_EXECUTOR", f"{command_id}: invalid executor"))
        elif check_id in target:
            errors.append(
                (
                    "E_DUPLICATE_ID",
                    f"{executor} repeats check {check_id!r}",
                )
            )
        else:
            target[check_id] = command

        checks_artifact = command.get("checksArtifact")
        checks_path = (
            checks_artifact.get("path")
            if isinstance(checks_artifact, dict)
            else None
        )
        if isinstance(commit_sha, str) and isinstance(checks_path, str):
            expected_command = (
                "python3 quality/scripts/run_gate_check.py "
                f"--gate {gate_id} --criterion {check_id} "
                f"--subject {commit_sha} --result {checks_path}"
            )
            if command.get("command") != expected_command:
                errors.append(
                    (
                        "E_COMMAND_PROFILE",
                        f"{command_id}: command must equal {expected_command!r}",
                    )
                )
        attempt_prefix = (
            declared_path.rsplit("/", 1)[0] + f"/commands/{command_id}/"
            if isinstance(declared_path, str)
            else None
        )
        for artifact_field in ("stdoutArtifact", "stderrArtifact", "checksArtifact"):
            artifact = command.get(artifact_field)
            artifact_path = artifact.get("path") if isinstance(artifact, dict) else None
            if (
                not isinstance(attempt_prefix, str)
                or not isinstance(artifact_path, str)
                or not artifact_path.startswith(attempt_prefix)
            ):
                errors.append(
                    (
                        "E_COMMAND_ARTIFACT",
                        f"{command_id}.{artifact_field} must be inside {attempt_prefix!r}",
                    )
                )

    environments = _unique_map(
        (item for item in arrays["environments"] if isinstance(item, dict)),
        lambda item: str(item.get("id")),
        "environment id",
        errors,
    )
    for command_id, command in commands.items():
        environment_id = command.get("environmentId")
        if not isinstance(environment_id, str) or environment_id not in environments:
            errors.append(
                (
                    "E_ENVIRONMENT_REF",
                    f"{command_id}: unknown environmentId: {environment_id!r}",
                )
            )
    metrics = _unique_map(
        (item for item in arrays["rawMetrics"] if isinstance(item, dict)),
        lambda item: str(item.get("name")),
        "metric name",
        errors,
    )
    coverage_scopes_raw = coverage.get("scopes")
    coverage_scope_items = (
        coverage_scopes_raw if isinstance(coverage_scopes_raw, list) else []
    )
    coverage_scopes = _unique_map(
        (
            item
            for item in coverage_scope_items
            if isinstance(item, dict)
        ),
        lambda item: str(item.get("name")),
        "coverage scope",
        errors,
    )

    if coverage.get("status") == "measured":
        if not coverage_scopes:
            errors.append(("E_COVERAGE_SHAPE", "measured coverage requires scopes"))
        if not isinstance(coverage.get("reportArtifact"), dict):
            errors.append(
                ("E_COVERAGE_SHAPE", "measured coverage requires reportArtifact")
            )
    elif coverage.get("status") == "not-applicable":
        if coverage_scopes or coverage.get("reportArtifact") is not None:
            errors.append(
                (
                    "E_COVERAGE_SHAPE",
                    "not-applicable coverage requires empty scopes and null artifact",
                )
            )
    else:
        errors.append(("E_COVERAGE_SHAPE", "unknown coverage status"))

    for name, scope in coverage_scopes.items():
        actual = scope.get("linePercent")
        required = scope.get("requiredPercent")
        if not isinstance(actual, (int, float)) or not isinstance(
            required, (int, float)
        ):
            errors.append(("E_COVERAGE", f"{name}: percentages must be numeric"))
        elif actual < required:
            errors.append(
                ("E_COVERAGE", f"{name}: {actual} is below required {required}")
            )

    required_coverage = profile.get("requiredCoverage", {})
    if not isinstance(required_coverage, dict):
        errors.append(("E_PROFILE", f"{gate_id}.requiredCoverage must be an object"))
    else:
        for name, minimum in required_coverage.items():
            scope = coverage_scopes.get(name)
            if scope is None:
                errors.append(("E_GATE_COVERAGE", f"missing required scope: {name}"))
                continue
            if scope.get("requiredPercent") != minimum:
                errors.append(
                    (
                        "E_GATE_COVERAGE",
                        f"{name}: requiredPercent must equal profile value {minimum}",
                    )
                )
            actual = scope.get("linePercent")
            if not isinstance(actual, (int, float)) or actual < minimum:
                errors.append(
                    ("E_GATE_COVERAGE", f"{name}: coverage is below {minimum}")
                )

    artifacts = _artifact_objects(data)
    top_level_artifacts = {
        item.get("path"): item
        for item in arrays["artifacts"]
        if isinstance(item, dict) and isinstance(item.get("path"), str)
    }
    artifact_index: dict[str, dict[str, Any]] = {}
    evidence_directory = (
        declared_path.rsplit("/", 1)[0] + "/"
        if isinstance(declared_path, str)
        else None
    )
    for artifact in artifacts:
        raw_path = artifact.get("path")
        if not isinstance(raw_path, str):
            errors.append(("E_ARTIFACT_PATH", "artifact path must be a string"))
            continue
        prior = artifact_index.get(raw_path)
        if prior is not None and prior != artifact:
            errors.append(
                ("E_ARTIFACT_DUPLICATE", f"conflicting metadata for {raw_path}")
            )
            continue
        artifact_index[raw_path] = artifact
        if (
            not isinstance(evidence_directory, str)
            or not raw_path.startswith(evidence_directory)
        ):
            errors.append(
                (
                    "E_ARTIFACT_PATH",
                    f"artifact must be inside current attempt: {raw_path}",
                )
            )
        if raw_path not in top_level_artifacts:
            errors.append(
                ("E_ARTIFACT_INDEX", f"nested artifact is not indexed: {raw_path}")
            )

    if len(top_level_artifacts) != len(
        [item for item in arrays["artifacts"] if isinstance(item, dict)]
    ):
        errors.append(("E_ARTIFACT_DUPLICATE", "top-level artifact paths must be unique"))

    if verify_files:
        for raw_path, artifact in top_level_artifacts.items():
            try:
                path = _safe_repo_path(root, raw_path)
            except ValueError as error:
                errors.append(("E_ARTIFACT_PATH", f"{raw_path}: {error}"))
                continue
            if not path.is_file():
                errors.append(("E_ARTIFACT_MISSING", f"missing artifact: {raw_path}"))
                continue
            if path.stat().st_size != artifact.get("bytes"):
                errors.append(("E_ARTIFACT_SIZE", f"byte count mismatch: {raw_path}"))
            if sha256_file(path) != artifact.get("sha256"):
                errors.append(("E_ARTIFACT_DIGEST", f"SHA-256 mismatch: {raw_path}"))

        for metric_name, metric in metrics.items():
            raw_artifact = metric.get("rawArtifact")
            raw_path = (
                raw_artifact.get("path") if isinstance(raw_artifact, dict) else None
            )
            if not isinstance(raw_path, str):
                continue
            try:
                metric_path = _safe_repo_path(root, raw_path)
                metric_artifact = strict_load(metric_path)
            except (OSError, StrictJsonError, ValueError) as error:
                errors.append(
                    ("E_METRIC_ARTIFACT", f"{metric_name}: cannot load {raw_path}: {error}")
                )
                continue
            expected_metric_artifact: dict[str, Any] = {
                "name": metric.get("name"),
                "unit": metric.get("unit"),
            }
            if "samples" in metric:
                expected_metric_artifact["samples"] = metric.get("samples")
            if "measurement" in metric:
                expected_metric_artifact["measurement"] = metric.get("measurement")
            if metric_artifact != expected_metric_artifact:
                errors.append(
                    (
                        "E_METRIC_ARTIFACT",
                        f"{metric_name}: artifact must exactly contain "
                        "name/unit and samples or measurement",
                    )
                )

        for command_id, command in commands.items():
            checks_artifact = command.get("checksArtifact")
            raw_path = (
                checks_artifact.get("path")
                if isinstance(checks_artifact, dict)
                else None
            )
            if not isinstance(raw_path, str):
                continue
            try:
                check_result = strict_load(_safe_repo_path(root, raw_path))
            except (OSError, StrictJsonError, ValueError) as error:
                errors.append(
                    (
                        "E_COMMAND_ARTIFACT",
                        f"{command_id}: cannot load checks artifact: {error}",
                    )
                )
                continue
            expected_result = {
                "schemaVersion": "gate-check-result-v1",
                "gateId": gate_id,
                "subjectCommitSha": commit_sha,
                "commandId": command_id,
                "executor": command.get("executor"),
                "command": command.get("command"),
                "workingDirectory": command.get("workingDirectory"),
                "durationSeconds": command.get("durationSeconds"),
                "exitCode": command.get("exitCode"),
                "conclusion": command.get("conclusion"),
                "checks": command.get("checks"),
                "scenarioIds": command.get("scenarioIds"),
            }
            if check_result != expected_result:
                errors.append(
                    (
                        "E_COMMAND_ARTIFACT",
                        f"{command_id}: checks artifact content differs",
                    )
                )

        ci_artifact = ci.get("attestationArtifact")
        ci_path = ci_artifact.get("path") if isinstance(ci_artifact, dict) else None
        if isinstance(ci_path, str):
            try:
                ci_attestation = strict_load(_safe_repo_path(root, ci_path))
            except (OSError, StrictJsonError, ValueError) as error:
                errors.append(("E_CI_ATTESTATION", f"{ci_path}: {error}"))
            else:
                expected_ci_attestation = {
                    "schemaVersion": "github-actions-attestation-v1",
                    "provider": ci.get("provider"),
                    "repository": ci.get("repository"),
                    "workflowPath": ci.get("workflowPath"),
                    "runId": ci.get("runId"),
                    "runAttempt": ci.get("runAttempt"),
                    "jobId": ci.get("jobId"),
                    "jobName": ci.get("jobName"),
                    "headSha": ci.get("headSha"),
                    "url": ci.get("url"),
                    "conclusion": ci.get("conclusion"),
                }
                if ci_attestation != expected_ci_attestation:
                    errors.append(
                        (
                            "E_CI_ATTESTATION",
                            "CI attestation content differs from evidence",
                        )
                    )

        review_artifact = reviewer.get("reviewArtifact")
        review_path = (
            review_artifact.get("path")
            if isinstance(review_artifact, dict)
            else None
        )
        if isinstance(review_path, str):
            try:
                review_attestation = strict_load(_safe_repo_path(root, review_path))
            except (OSError, StrictJsonError, ValueError) as error:
                errors.append(("E_REVIEW_ATTESTATION", f"{review_path}: {error}"))
            else:
                expected_review_attestation = {
                    "schemaVersion": "gate-review-v1",
                    "gateId": gate_id,
                    "subjectCommitSha": commit_sha,
                    "subjectTreeSha": tree_sha,
                    "reviewerId": reviewer.get("id"),
                    "implementationWriter": data.get("implementationWriter"),
                    "reproducedCommandId": reviewer.get("reproducedCommandId"),
                    "result": "approve",
                }
                if review_attestation != expected_review_attestation:
                    errors.append(
                        (
                            "E_REVIEW_ATTESTATION",
                            "review attestation content differs from evidence",
                        )
                    )

    if ci.get("headSha") != commit_sha:
        errors.append(("E_CI_SUBJECT", "CI headSha must equal subject commitSha"))
    expected_ci_url = (
        "https://github.com/VibecodingGermany/Project_Nova/actions/runs/"
        f"{ci.get('runId')}"
    )
    if ci.get("url") != expected_ci_url:
        errors.append(("E_CI_URL", f"CI url must equal {expected_ci_url!r}"))
    if verify_git and isinstance(commit_sha, str):
        workflow_path = ci.get("workflowPath")
        if isinstance(workflow_path, str):
            try:
                _git_bytes(
                    "cat-file",
                    "blob",
                    f"{commit_sha}:{workflow_path}",
                    root=root,
                )
            except ValueError as error:
                errors.append(("E_CI_WORKFLOW", str(error)))

    writer = data.get("implementationWriter")
    if reviewer.get("id") == writer:
        errors.append(("E_REVIEWER_INDEPENDENCE", "reviewer must differ from writer"))
    reproduced_id = reviewer.get("reproducedCommandId")
    reproduced = commands.get(reproduced_id)
    if reproduced is None:
        errors.append(
            ("E_REPRODUCED_COMMAND", f"unknown reproducedCommandId: {reproduced_id!r}")
        )
    else:
        if reproduced.get("conclusion") != "success" or reproduced.get("exitCode") != 0:
            errors.append(
                ("E_REPRODUCED_COMMAND", "reproduced command must have succeeded")
            )
        if reproduced.get("command") != reviewer.get("cleanCloneCommand"):
            errors.append(
                (
                    "E_REPRODUCED_COMMAND",
                    "cleanCloneCommand must equal the reproduced command text",
                )
            )
        if reproduced.get("executor") != "reviewer":
            errors.append(
                (
                    "E_REPRODUCED_COMMAND",
                    "reproduced command must have executor=reviewer",
                )
            )
        reproduced_checks = reproduced.get("checks")
        reproduced_check_id = (
            reproduced_checks[0].get("id")
            if isinstance(reproduced_checks, list)
            and len(reproduced_checks) == 1
            and isinstance(reproduced_checks[0], dict)
            else None
        )
        if reproduced_check_id not in implementation_checks:
            errors.append(
                (
                    "E_REPRODUCED_COMMAND",
                    "reviewer must reproduce an implementation check",
                )
            )

    criteria = _unique_map(
        (item for item in arrays["criteria"] if isinstance(item, dict)),
        lambda item: str(item.get("id")),
        "criterion id",
        errors,
    )
    required_criteria = profile.get("requiredCriterionIds", [])
    if not isinstance(required_criteria, list):
        errors.append(("E_PROFILE", f"{gate_id}.requiredCriterionIds must be an array"))
        required_criteria = []
    if set(criteria) != set(required_criteria):
        missing = sorted(set(required_criteria) - set(criteria))
        extra = sorted(set(criteria) - set(required_criteria))
        errors.append(
            (
                "E_GATE_CRITERIA",
                f"criterion profile mismatch; missing={missing}, extra={extra}",
            )
        )
    if set(implementation_checks) != set(required_criteria):
        missing_checks = sorted(set(required_criteria) - set(implementation_checks))
        extra_checks = sorted(set(implementation_checks) - set(required_criteria))
        errors.append(
            (
                "E_CRITERION_CHECK_PROFILE",
                f"implementation check profile mismatch; "
                f"missing={missing_checks}, extra={extra_checks}",
            )
        )

    reference_targets = {
        "check": set(implementation_checks),
        "command": set(commands),
        "artifact": set(artifact_index),
        "metric": set(metrics),
        "coverage": set(coverage_scopes),
        "ci": {str(ci.get("runId"))},
        "scenario": set(scenario_definitions),
    }
    referenced_scenarios: set[str] = set()
    for criterion_id, criterion in criteria.items():
        refs = criterion.get("evidenceRefs")
        if not isinstance(refs, list) or not refs:
            errors.append(("E_EVIDENCE_REF", f"{criterion_id}: no evidenceRefs"))
            continue
        if len(refs) != len(set(refs)):
            errors.append(("E_EVIDENCE_REF", f"{criterion_id}: duplicate evidenceRef"))
        required_check_ref = f"check:{criterion_id}"
        if required_check_ref not in refs:
            errors.append(
                (
                    "E_CRITERION_CHECK_PROFILE",
                    f"{criterion_id}: requires {required_check_ref}",
                )
            )
        unrelated_checks = sorted(
            reference
            for reference in refs
            if isinstance(reference, str)
            and reference.startswith("check:")
            and reference != required_check_ref
        )
        if unrelated_checks:
            errors.append(
                (
                    "E_CRITERION_CHECK_PROFILE",
                    f"{criterion_id}: unrelated check refs {unrelated_checks}",
                )
            )
        has_substantive_reference = False
        for reference in refs:
            if not isinstance(reference, str) or ":" not in reference:
                errors.append(
                    ("E_EVIDENCE_REF", f"{criterion_id}: malformed reference {reference!r}")
                )
                continue
            namespace, target = reference.split(":", 1)
            if namespace in {
                "check",
                "artifact",
                "metric",
                "coverage",
                "scenario",
            }:
                has_substantive_reference = True
            if target not in reference_targets.get(namespace, set()):
                errors.append(
                    (
                        "E_EVIDENCE_REF",
                        f"{criterion_id}: unresolved reference {reference!r}",
                    )
                )
            elif namespace == "scenario":
                referenced_scenarios.add(target)
        if not has_substantive_reference:
            errors.append(
                (
                    "E_EVIDENCE_REF",
                    f"{criterion_id}: CI alone is not substantive evidence",
                )
            )

    required_scenarios = profile.get("requiredScenarioIds", [])
    if not isinstance(required_scenarios, list):
        errors.append(("E_PROFILE", f"{gate_id}.requiredScenarioIds must be an array"))
    else:
        missing_scenarios = sorted(set(required_scenarios) - referenced_scenarios)
        extra_scenarios = sorted(referenced_scenarios - set(required_scenarios))
        if missing_scenarios or extra_scenarios:
            errors.append(
                (
                    "E_GATE_SCENARIO",
                    f"scenario reference mismatch; missing={missing_scenarios}, "
                    f"extra={extra_scenarios}",
                )
            )
        _validate_scenario_results(
            required_scenarios,
            scenario_definitions,
            scenario_methods,
            criteria,
            implementation_checks,
            metrics,
            environments,
            errors,
        )

    minimum_metrics = profile.get("requiredMetricMinimums", {})
    if not isinstance(minimum_metrics, dict):
        errors.append(
            ("E_PROFILE", f"{gate_id}.requiredMetricMinimums must be an object")
        )
    else:
        for name, requirement in minimum_metrics.items():
            metric = metrics.get(name)
            samples = metric.get("samples") if isinstance(metric, dict) else None
            minimum = (
                requirement.get("minimum")
                if isinstance(requirement, dict)
                else None
            )
            unit = (
                requirement.get("unit")
                if isinstance(requirement, dict)
                else None
            )
            if (
                not isinstance(samples, list)
                or len(samples) != 1
                or not isinstance(samples[0], (int, float))
                or isinstance(samples[0], bool)
                or not isinstance(minimum, (int, float))
                or samples[0] < minimum
                or metric.get("unit") != unit
            ):
                errors.append(
                    (
                        "E_GATE_METRIC",
                        f"{name} must contain one {unit!r} sample >= {minimum!r}",
                    )
                )

    for metric_name, metric in metrics.items():
        sample_groups: list[Any] = []
        samples = metric.get("samples")
        if isinstance(samples, list):
            sample_groups.append(samples)
        measurement = metric.get("measurement")
        if isinstance(measurement, dict):
            runs = measurement.get("runs")
            if isinstance(runs, list):
                for run in runs:
                    if isinstance(run, dict):
                        sample_groups.append(run.get("samples"))
        for sample_group in sample_groups:
            if isinstance(sample_group, list):
                for sample in sample_group:
                    if isinstance(sample, bool) or not isinstance(sample, (int, float)):
                        errors.append(
                            ("E_METRIC", f"{metric_name}: samples must be numeric")
                        )
                    elif not math.isfinite(sample) or sample < 0:
                        errors.append(
                            (
                                "E_METRIC",
                                f"{metric_name}: samples must be finite and non-negative",
                            )
                        )

    if verdict.get("result") == "pass":
        for command_id, command in commands.items():
            if command.get("conclusion") != "success":
                errors.append(
                    ("E_PASS_COMMAND", f"{command_id}: pass requires success")
                )
        if ci.get("conclusion") != "success":
            errors.append(("E_PASS_CI", "pass requires successful CI"))
        for criterion_id, criterion in criteria.items():
            if criterion.get("result") != "pass":
                errors.append(
                    ("E_PASS_CRITERION", f"{criterion_id}: pass requires criterion pass")
                )
        if require_trust:
            if trusted_checkout is None:
                errors.append(
                    (
                        "E_AUTHORIZATION_BOOTSTRAP",
                        "gate pass authorization requires "
                        "--trusted-tool-checkout with the subject-independent "
                        "D-064 trusted tools",
                    )
                )
            _validate_trust_context(
                trust_context_path,
                data,
                evidence_path,
                root,
                errors,
                gate_id=gate_id,
            )
    elif verdict.get("result") != "fail":
        errors.append(("E_VERDICT", "verdict.result must be pass or fail"))

    return errors


def _self_test_fixture() -> tuple[
    dict[str, Any],
    dict[str, Any],
    dict[str, dict[str, Any]],
    dict[str, dict[str, Any]],
    Path,
]:
    commit = "1" * 40
    attempt_id = "2026-07-24T12-00-00Z-self-test"
    evidence_path = Path(
        f"quality/evidence/G0/{commit}/{attempt_id}/GateEvidence.json"
    )
    attempt_directory = evidence_path.parent.as_posix()

    def artifact(relative_path: str, digest_character: str) -> dict[str, Any]:
        return {
            "path": f"{attempt_directory}/{relative_path}",
            "sha256": digest_character * 64,
            "bytes": 0,
        }

    count_artifact = {
        "path": f"{attempt_directory}/metrics/self-test-count.json",
        "sha256": "0" * 64,
        "bytes": 0,
    }
    assertion_artifact = {
        "path": f"{attempt_directory}/metrics/self-test-assertion.json",
        "sha256": "1" * 64,
        "bytes": 0,
    }
    latency_artifact = {
        "path": f"{attempt_directory}/metrics/self-test-latency.json",
        "sha256": "2" * 64,
        "bytes": 0,
    }
    implementation_stdout = artifact(
        "commands/impl-g0-self-test/stdout.log", "7"
    )
    implementation_stderr = artifact(
        "commands/impl-g0-self-test/stderr.log", "8"
    )
    implementation_checks = artifact(
        "commands/impl-g0-self-test/checks.json", "9"
    )
    reviewer_stdout = artifact("commands/review-g0-self-test/stdout.log", "a")
    reviewer_stderr = artifact("commands/review-g0-self-test/stderr.log", "b")
    reviewer_checks = artifact("commands/review-g0-self-test/checks.json", "c")
    ci_attestation = artifact("attestations/github-actions.json", "d")
    review_attestation = artifact("attestations/review.json", "e")

    implementation_command = (
        "python3 quality/scripts/run_gate_check.py "
        f"--gate G0 --criterion G0-SELF-TEST --subject {commit} "
        f"--result {implementation_checks['path']}"
    )
    reviewer_command = (
        "python3 quality/scripts/run_gate_check.py "
        f"--gate G0 --criterion G0-SELF-TEST --subject {commit} "
        f"--result {reviewer_checks['path']}"
    )

    def command_result(
        command_id: str,
        executor: str,
        command: str,
        stdout_artifact: dict[str, Any],
        stderr_artifact: dict[str, Any],
        checks_artifact: dict[str, Any],
    ) -> dict[str, Any]:
        return {
            "id": command_id,
            "executor": executor,
            "command": command,
            "workingDirectory": ".",
            "environmentId": "test-env",
            "durationSeconds": 0.1,
            "exitCode": 0,
            "conclusion": "success",
            "checkCount": 1,
            "expectedCheckCount": 1,
            "stdoutArtifact": stdout_artifact,
            "stderrArtifact": stderr_artifact,
            "checksArtifact": checks_artifact,
            "checks": [{"id": "G0-SELF-TEST", "result": "pass"}],
            "scenarioIds": ["SELF_TEST_SCENARIO"],
        }

    content_digests = {
        "manifest": "3" * 64,
        "scenarioContract": "4" * 64,
        "evidenceSchema": "5" * 64,
        "evidenceValidator": "6" * 64,
        "ajvWrapper": "f0" * 32,
        "packageManifest": "f1" * 32,
        "packageLock": "f2" * 32,
        "gateRunner": "f3" * 32,
        "authorizeWorkflow": "f4" * 32,
    }
    trusted_digests = {
        component_id: "e" + f"{index:x}" + "e" * 62
        for index, component_id in enumerate(content_digests)
    }
    trust_bundle_components = {
        component_id: {
            "path": TRUST_BUNDLE_COMPONENTS[component_id],
            "subjectSha256": subject_digest,
            "trustedSha256": trusted_digests[component_id],
        }
        for component_id, subject_digest in content_digests.items()
    }

    fixture: dict[str, Any] = {
        "schemaVersion": SCHEMA_VERSION,
        "gateId": "G0",
        "subject": {"commitSha": commit, "treeSha": "2" * 40, "dirty": False},
        "attempt": {
            "id": attempt_id,
            "createdAtUtc": "2026-07-24T12:00:00Z",
            "evidencePath": evidence_path.as_posix(),
            "previousEvidence": None,
        },
        "priorGateEvidence": [],
        "scope": {
            "classification": "code",
            "changedPaths": ["quality/scripts/validate_gate_evidence.py"],
            "relevantContracts": ["quality/schemas/GateEvidence.schema.json"],
        },
        "content": {
            "manifestPath": "quality/content/mvp-v1.json",
            "manifestSha256": content_digests["manifest"],
            "scenarioPath": "quality/scenarios/mvp-v1.json",
            "scenarioSha256": content_digests["scenarioContract"],
            "evidenceSchemaPath": "quality/schemas/GateEvidence.schema.json",
            "evidenceSchemaSha256": content_digests["evidenceSchema"],
            "evidenceValidatorPath": "quality/scripts/validate_gate_evidence.py",
            "evidenceValidatorSha256": content_digests["evidenceValidator"],
            "ajvWrapperPath": "quality/scripts/validate_evidence_schema.mjs",
            "ajvWrapperSha256": content_digests["ajvWrapper"],
            "packageManifestPath": "quality/package.json",
            "packageManifestSha256": content_digests["packageManifest"],
            "packageLockPath": "quality/package-lock.json",
            "packageLockSha256": content_digests["packageLock"],
            "gateRunnerPath": "quality/scripts/run_gate_check.py",
            "gateRunnerSha256": content_digests["gateRunner"],
            "authorizeWorkflowPath": ".github/workflows/quality-gate.yml",
            "authorizeWorkflowSha256": content_digests["authorizeWorkflow"],
        },
        "trustBundle": {
            "trustedRepository": "VibecodingGermany/Project_Nova",
            "trustedCommitSha": "9" * 40,
            "nodeVersion": "v24.4.1",
            "components": trust_bundle_components,
        },
        "toolchains": {
            "unity": {"version": "6000.5.4f1", "revision": "d550df8bd089"},
            "dotnetSdk": "8.0.0",
            "packages": [{"id": "example", "version": "1"}],
        },
        "environments": [
            {
                "id": "test-env",
                "os": "Windows",
                "architecture": "x64",
                "hardware": "Ryzen 5 5600, RTX 3060, 16 GB RAM, NVMe",
                "build": "standalone-il2cpp-development",
                "executionPath": "managed",
                "burstEnabled": False,
                "resolution": "2560x1440",
                "qualityProfile": "NovaReference",
                "vSyncEnabled": False,
                "deepProfilingEnabled": False,
                "replay": "fixed",
            }
        ],
        "commands": [
            command_result(
                "impl-g0-self-test",
                "implementation",
                implementation_command,
                implementation_stdout,
                implementation_stderr,
                implementation_checks,
            ),
            command_result(
                "review-g0-self-test",
                "reviewer",
                reviewer_command,
                reviewer_stdout,
                reviewer_stderr,
                reviewer_checks,
            ),
        ],
        "coverage": {"status": "not-applicable", "scopes": [], "reportArtifact": None},
        "rawMetrics": [
            {
                "name": "self-test-count",
                "unit": "checks",
                "samples": [1],
                "rawArtifact": count_artifact,
            },
            {
                "name": "scenario.SELF_TEST_SCENARIO.assertion.validator-pass",
                "unit": "bool",
                "samples": [1],
                "rawArtifact": assertion_artifact,
            },
            {
                "name": "scenario.SELF_TEST_SCENARIO.latencyMs",
                "unit": "score",
                "samples": [0.5, 0.75, 1.0],
                "rawArtifact": latency_artifact,
            },
        ],
        "artifacts": [
            count_artifact,
            assertion_artifact,
            latency_artifact,
            implementation_stdout,
            implementation_stderr,
            implementation_checks,
            reviewer_stdout,
            reviewer_stderr,
            reviewer_checks,
            ci_attestation,
            review_attestation,
        ],
        "ci": {
            "provider": "github-actions",
            "repository": "VibecodingGermany/Project_Nova",
            "workflowPath": ".github/workflows/quality-gate.yml",
            "runId": "123",
            "runAttempt": 1,
            "jobId": "456",
            "jobName": "quality-gate",
            "headSha": commit,
            "url": "https://github.com/VibecodingGermany/Project_Nova/actions/runs/123",
            "conclusion": "success",
            "attestationArtifact": ci_attestation,
        },
        "implementationWriter": "writer",
        "reviewer": {
            "id": "reviewer",
            "role": "validator",
            "independentFromImplementationWriter": True,
            "cleanCloneCommand": reviewer_command,
            "reproducedCommandId": "review-g0-self-test",
            "reviewArtifact": review_attestation,
        },
        "criteria": [
            {
                "id": "G0-SELF-TEST",
                "requirement": "validator rejects contradictions",
                "evidenceRefs": [
                    "check:G0-SELF-TEST",
                    "ci:123",
                    "scenario:SELF_TEST_SCENARIO",
                    "metric:scenario.SELF_TEST_SCENARIO.assertion.validator-pass",
                    "metric:scenario.SELF_TEST_SCENARIO.latencyMs",
                ],
                "result": "pass",
            }
        ],
        "verdict": {"result": "pass", "rationale": "self-test fixture"},
    }
    profiles = {
        "G0": {
            "requiredPriorGateIds": [],
            "requiredCriterionIds": ["G0-SELF-TEST"],
            "requiredScenarioIds": ["SELF_TEST_SCENARIO"],
            "requiredCoverage": {},
        }
    }
    scenarios = {
        "SELF_TEST_SCENARIO": {
            "id": "SELF_TEST_SCENARIO",
            "gateUsage": ["G0"],
            "requiredAssertions": ["validator-pass"],
            "thresholds": {"latencyMs": {"unit": "score", "maximum": 1.0}},
        }
    }
    methods = {
        "performanceMethod": {
            "os": "Windows",
            "architecture": "x64",
            "hardware": "Ryzen 5 5600, RTX 3060, 16 GB RAM, NVMe",
            "build": "standalone-il2cpp-development",
            "executionPath": "managed",
            "burstEnabled": False,
            "resolution": "2560x1440",
            "qualityProfile": "NovaReference",
            "vSyncEnabled": False,
            "deepProfilingEnabled": False,
            "replay": "fixed",
            "warmupSeconds": 30,
            "measurementSeconds": 120,
            "repetitions": 3,
            "minimumSamplesPerSecond": 1,
            "outlierRemoval": False,
            "rawSamplesRequired": True,
        }
    }
    return fixture, profiles, scenarios, methods, evidence_path


def run_self_test() -> int:
    fixture, profiles, scenarios, methods, evidence_path = _self_test_fixture()

    try:
        (
            repository_profiles,
            repository_scenarios,
            repository_methods,
        ) = _load_scenario_contract(ROOT)
    except (OSError, StrictJsonError, ValueError) as error:
        print(f"SELF-TEST FAIL: repository scenario contract is invalid: {error}")
        return 1
    repository_contract_errors: list[tuple[str, str]] = []
    _validate_scenario_contract(
        repository_profiles,
        repository_scenarios,
        repository_methods,
        repository_contract_errors,
    )
    if repository_contract_errors:
        print(
            "SELF-TEST FAIL: repository scenario/profile contract drift: "
            f"{repository_contract_errors}"
        )
        return 1

    def codes(
        candidate: dict[str, Any],
        active_profiles: dict[str, Any] = profiles,
        active_scenarios: dict[str, dict[str, Any]] = scenarios,
        active_methods: dict[str, dict[str, Any]] = methods,
    ) -> set[str]:
        return {
            code
            for code, _ in validate_document(
                candidate,
                evidence_path,
                root=Path("/repo"),
                verify_files=False,
                verify_git=False,
                profiles=active_profiles,
                scenario_definitions=active_scenarios,
                scenario_methods=active_methods,
                evidence_schema_bytes=EVIDENCE_SCHEMA.read_bytes(),
                require_trust=False,
            )
        }

    cases: list[tuple[str, str, Callable[[dict[str, Any]], None]]] = [
        (
            "schema-version",
            "E_SCHEMA_VERSION",
            lambda value: value.update(schemaVersion="1.0.0"),
        ),
        ("exit", "E_COMMAND_EXIT", lambda value: value["commands"][0].update(exitCode=7)),
        (
            "count",
            "E_COMMAND_COUNT",
            lambda value: value["commands"][0].update(checkCount=0),
        ),
        (
            "no-op-command",
            "E_COMMAND_PROFILE",
            lambda value: value["commands"][0].update(command="true"),
        ),
        (
            "reviewer",
            "E_REVIEWER_INDEPENDENCE",
            lambda value: value["reviewer"].update(id="writer"),
        ),
        (
            "reproduced",
            "E_REPRODUCED_COMMAND",
            lambda value: value["reviewer"].update(reproducedCommandId="missing"),
        ),
        (
            "path",
            "E_EVIDENCE_PATH",
            lambda value: value["attempt"].update(evidencePath="quality/evidence/G1/x"),
        ),
        (
            "reference",
            "E_EVIDENCE_REF",
            lambda value: value["criteria"][0].update(
                evidenceRefs=["command:missing"]
            ),
        ),
        (
            "criterion",
            "E_PASS_CRITERION",
            lambda value: value["criteria"][0].update(result="fail"),
        ),
        (
            "ci",
            "E_PASS_CI",
            lambda value: value["ci"].update(conclusion="cancelled"),
        ),
        (
            "dirty",
            "E_DIRTY",
            lambda value: value["subject"].update(dirty=True),
        ),
        (
            "scenario-assertion",
            "E_SCENARIO_ASSERTION",
            lambda value: value["rawMetrics"][1].update(samples=[0]),
        ),
        (
            "scenario-threshold",
            "E_SCENARIO_THRESHOLD",
            lambda value: value["rawMetrics"][2].update(samples=[2.0]),
        ),
        (
            "scenario-metric-ref",
            "E_SCENARIO_METRIC_REF",
            lambda value: value["criteria"][0].update(
                evidenceRefs=[
                    "check:G0-SELF-TEST",
                    "ci:123",
                    "scenario:SELF_TEST_SCENARIO",
                    "metric:scenario.SELF_TEST_SCENARIO.assertion.validator-pass",
                ]
            ),
        ),
        (
            "scenario-command-ref",
            "E_SCENARIO_COMMAND",
            lambda value: value["criteria"][0].update(
                evidenceRefs=[
                    "command:impl-g0-self-test",
                    "ci:123",
                    "scenario:SELF_TEST_SCENARIO",
                    "metric:scenario.SELF_TEST_SCENARIO.assertion.validator-pass",
                    "metric:scenario.SELF_TEST_SCENARIO.latencyMs",
                ]
            ),
        ),
        (
            "schema-extra-property",
            "E_JSON_SCHEMA",
            lambda value: value.update(forbidden=True),
        ),
        (
            "negative-metric",
            "E_SCENARIO_METRIC",
            lambda value: value["rawMetrics"][2].update(samples=[-1.0]),
        ),
        (
            "wrong-unit",
            "E_SCENARIO_UNIT",
            lambda value: value["rawMetrics"][2].update(unit="seconds"),
        ),
        (
            "command-environment-ref",
            "E_ENVIRONMENT_REF",
            lambda value: value["commands"][0].update(environmentId="unknown-env"),
        ),
        (
            "trust-bundle-path",
            "E_TRUST_BUNDLE",
            lambda value: value["trustBundle"]["components"]["manifest"].update(
                path="quality/content/other.json"
            ),
        ),
        (
            "trust-bundle-subject-digest",
            "E_TRUST_BUNDLE",
            lambda value: value["trustBundle"]["components"]["manifest"].update(
                subjectSha256="0" * 64
            ),
        ),
    ]

    base_errors = codes(copy.deepcopy(fixture))
    if base_errors:
        print(f"SELF-TEST FAIL: valid fixture rejected: {sorted(base_errors)}")
        return 1

    checks = 2
    for name, expected_code, mutate in cases:
        candidate = copy.deepcopy(fixture)
        mutate(candidate)
        actual_codes = codes(candidate)
        checks += 1
        if expected_code not in actual_codes:
            print(
                f"SELF-TEST FAIL: {name} expected {expected_code}, "
                f"got {sorted(actual_codes)}"
            )
            return 1

    trusted_codes = {
        code
        for code, _ in validate_document(
            copy.deepcopy(fixture),
            evidence_path,
            root=Path("/repo"),
            verify_files=False,
            verify_git=False,
            profiles=profiles,
            scenario_definitions=scenarios,
            scenario_methods=methods,
            evidence_schema_bytes=EVIDENCE_SCHEMA.read_bytes(),
            require_trust=True,
        )
    }
    checks += 1
    if not {"E_AUTHORIZATION_BOOTSTRAP", "E_TRUST_CONTEXT"}.issubset(
        trusted_codes
    ):
        print(
            "SELF-TEST FAIL: local pass escaped the bootstrap/trust guards"
        )
        return 1

    fail_fixture = copy.deepcopy(fixture)
    fail_fixture["verdict"] = {
        "result": "fail",
        "rationale": "negative gate attempt",
    }
    fail_errors = validate_document(
        fail_fixture,
        evidence_path,
        root=Path("/repo"),
        verify_files=False,
        verify_git=False,
        profiles=profiles,
        scenario_definitions=scenarios,
        scenario_methods=methods,
        evidence_schema_bytes=EVIDENCE_SCHEMA.read_bytes(),
        require_trust=True,
    )
    checks += 1
    if fail_errors:
        print(
            "SELF-TEST FAIL: valid verdict=fail fixture was rejected: "
            f"{fail_errors}"
        )
        return 1
    if _is_authorized_pass(fail_fixture, fail_errors):
        print("SELF-TEST FAIL: verdict=fail was treated as an authorized pass")
        return 1

    performance_fixture = copy.deepcopy(fixture)
    performance_scenarios = copy.deepcopy(scenarios)
    performance_scenarios["SELF_TEST_SCENARIO"]["methodRef"] = "performanceMethod"
    performance_scenarios["SELF_TEST_SCENARIO"]["thresholds"] = {
        "latencyMs": {"unit": "ms", "p95Max": 1.0}
    }
    performance_metric = performance_fixture["rawMetrics"][2]
    performance_metric["unit"] = "ms"
    performance_metric.pop("samples")
    performance_metric["measurement"] = {
        "methodRef": "performanceMethod",
        "environmentId": "test-env",
        "warmupSeconds": 30,
        "runs": [
            {
                "index": index,
                "measurementSeconds": 120,
                "samples": [0.5] * 120,
            }
            for index in range(1, 4)
        ],
    }
    for command in performance_fixture["commands"]:
        command["durationSeconds"] = 390

    performance_errors = codes(
        copy.deepcopy(performance_fixture),
        profiles,
        performance_scenarios,
        methods,
    )
    checks += 1
    if performance_errors:
        print(
            "SELF-TEST FAIL: valid three-run performance fixture rejected: "
            f"{sorted(performance_errors)}"
        )
        return 1

    performance_cases: list[
        tuple[str, str, Callable[[dict[str, Any]], None]]
    ] = [
        (
            "performance-run-count",
            "E_PERFORMANCE_METHOD",
            lambda value: value["rawMetrics"][2]["measurement"].update(
                runs=value["rawMetrics"][2]["measurement"]["runs"][:2]
            ),
        ),
        (
            "performance-warmup",
            "E_PERFORMANCE_METHOD",
            lambda value: value["rawMetrics"][2]["measurement"].update(
                warmupSeconds=29
            ),
        ),
        (
            "performance-duration",
            "E_PERFORMANCE_METHOD",
            lambda value: value["rawMetrics"][2]["measurement"]["runs"][0].update(
                measurementSeconds=119
            ),
        ),
        (
            "performance-sample-count",
            "E_PERFORMANCE_SAMPLE_COUNT",
            lambda value: value["rawMetrics"][2]["measurement"]["runs"][0].update(
                samples=[0.5]
            ),
        ),
        (
            "per-run-percentile",
            "E_SCENARIO_THRESHOLD",
            lambda value: value["rawMetrics"][2]["measurement"]["runs"][0].update(
                samples=([0.0] * 113) + ([2.0] * 7)
            ),
        ),
        (
            "measurement-environment-ref",
            "E_ENVIRONMENT_REF",
            lambda value: value["rawMetrics"][2]["measurement"].update(
                environmentId="unknown-env"
            ),
        ),
        (
            "measurement-environment-mismatch",
            "E_ENVIRONMENT_MISMATCH",
            lambda value: (
                value["environments"].append(
                    dict(value["environments"][0], id="other-env")
                ),
                value["rawMetrics"][2]["measurement"].update(
                    environmentId="other-env"
                ),
            ),
        ),
        (
            "environment-profile-drift",
            "E_ENVIRONMENT_PROFILE",
            lambda value: value["environments"][0].update(resolution="1920x1080"),
        ),
    ]
    for name, expected_code, mutate in performance_cases:
        candidate = copy.deepcopy(performance_fixture)
        mutate(candidate)
        actual_codes = codes(
            candidate,
            profiles,
            performance_scenarios,
            methods,
        )
        checks += 1
        if expected_code not in actual_codes:
            print(
                f"SELF-TEST FAIL: {name} expected {expected_code}, "
                f"got {sorted(actual_codes)}"
            )
            return 1

    invalid_prior = copy.deepcopy(fixture)
    invalid_prior["forbidden"] = True
    checks += 1
    if not _schema_validation_errors(
        invalid_prior, EVIDENCE_SCHEMA.read_bytes()
    ):
        print("SELF-TEST FAIL: schema-invalid prior fixture was accepted")
        return 1

    coverage_fixture = copy.deepcopy(fixture)
    coverage_fixture["coverage"] = {
        "status": "measured",
        "scopes": [
            {"name": "Nova.Simulation", "linePercent": 79, "requiredPercent": 80}
        ],
        "reportArtifact": fixture["artifacts"][0],
    }
    coverage_profiles = copy.deepcopy(profiles)
    coverage_profiles["G0"]["requiredCoverage"] = {"Nova.Simulation": 80}
    checks += 1
    if "E_COVERAGE" not in codes(coverage_fixture, coverage_profiles):
        print("SELF-TEST FAIL: below-threshold coverage was accepted")
        return 1

    order_fixture = copy.deepcopy(fixture)
    order_fixture["gateId"] = "G1"
    order_fixture["attempt"]["evidencePath"] = order_fixture["attempt"][
        "evidencePath"
    ].replace("/G0/", "/G1/")
    order_profiles = {
        "G1": {
            "requiredPriorGateIds": ["G0"],
            "requiredCriterionIds": ["G0-SELF-TEST"],
            "requiredScenarioIds": ["SELF_TEST_SCENARIO"],
            "requiredCoverage": {},
        }
    }
    order_scenarios = copy.deepcopy(scenarios)
    order_scenarios["SELF_TEST_SCENARIO"]["gateUsage"] = ["G1"]
    checks += 1
    if "E_GATE_ORDER" not in codes(
        order_fixture, order_profiles, order_scenarios
    ):
        print("SELF-TEST FAIL: isolated G1 pass was accepted without G0 evidence")
        return 1

    mapping_scenarios = copy.deepcopy(scenarios)
    mapping_scenarios["SELF_TEST_SCENARIO"]["gateUsage"] = []
    checks += 1
    if "E_PROFILE_SCENARIO_MAP" not in codes(
        copy.deepcopy(fixture), profiles, mapping_scenarios
    ):
        print("SELF-TEST FAIL: scenario/profile mapping drift was accepted")
        return 1

    with tempfile.TemporaryDirectory() as temp:
        repo = Path(temp)
        (repo / "quality/content").mkdir(parents=True)
        (repo / "quality/scenarios").mkdir(parents=True)
        (repo / "quality/schemas").mkdir(parents=True)
        (repo / "quality/scripts").mkdir(parents=True)
        (repo / ".github/workflows").mkdir(parents=True)
        component_bytes = {
            "manifest": b'{"subject":"manifest"}\\n',
            "scenarioContract": b'{"subject":"scenarios"}\\n',
            "evidenceSchema": EVIDENCE_SCHEMA.read_bytes(),
            "evidenceValidator": Path(__file__).read_bytes(),
            "ajvWrapper": (ROOT / "quality/scripts/validate_evidence_schema.mjs").read_bytes(),
            "packageManifest": b'{"name":"subject-quality"}\\n',
            "packageLock": b'{"lockfileVersion":3}\\n',
            "gateRunner": b"#!/usr/bin/env python3\\n",
            "authorizeWorkflow": b"name: quality-gate\\n",
        }
        for component_id, raw_bytes in component_bytes.items():
            (repo / Path(TRUST_BUNDLE_COMPONENTS[component_id])).write_bytes(raw_bytes)
        for arguments in (
            ("init", "-q"),
            ("config", "user.email", "self-test@example.invalid"),
            ("config", "user.name", "Evidence Self Test"),
            ("add", "."),
            ("commit", "-qm", "self test subject"),
        ):
            subprocess.run(
                ["git", *arguments],
                cwd=repo,
                check=True,
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL,
            )
        subject_fixture = copy.deepcopy(fixture)
        subject_commit = _git("rev-parse", "HEAD", root=repo)
        subject_tree = _git("rev-parse", "HEAD^{tree}", root=repo)
        old_commit = subject_fixture["subject"]["commitSha"]

        def replace_subject(value: Any) -> Any:
            if isinstance(value, str):
                return value.replace(old_commit, subject_commit)
            if isinstance(value, list):
                return [replace_subject(item) for item in value]
            if isinstance(value, dict):
                return {key: replace_subject(item) for key, item in value.items()}
            return value

        subject_fixture = replace_subject(subject_fixture)
        subject_fixture["subject"].update(
            commitSha=subject_commit, treeSha=subject_tree
        )
        for component_id, (path_field, digest_field) in CONTENT_COMPONENT_FIELDS.items():
            digest = sha256_file(repo / Path(TRUST_BUNDLE_COMPONENTS[component_id]))
            subject_fixture["content"][digest_field] = digest
            subject_fixture["trustBundle"]["components"][component_id][
                "subjectSha256"
            ] = digest
        subject_errors = validate_document(
            subject_fixture,
            Path(subject_fixture["attempt"]["evidencePath"]),
            root=repo,
            verify_files=False,
            verify_git=True,
            profiles=profiles,
            scenario_definitions=scenarios,
            scenario_methods=methods,
            evidence_schema_bytes=EVIDENCE_SCHEMA.read_bytes(),
            require_trust=False,
        )
        checks += 1
        if subject_errors:
            print(
                "SELF-TEST FAIL: subject-blob baseline was rejected: "
                f"{sorted({code for code, _ in subject_errors})}"
            )
            return 1
        manifest_path = repo / Path(TRUST_BUNDLE_COMPONENTS["manifest"])
        manifest_path.write_bytes(b'{"worktree":"different"}\\n')
        subject_fixture["content"]["manifestSha256"] = sha256_file(manifest_path)
        subject_codes = {
            code
            for code, _ in validate_document(
                subject_fixture,
                Path(subject_fixture["attempt"]["evidencePath"]),
                root=repo,
                verify_files=False,
                verify_git=True,
                profiles=profiles,
                scenario_definitions=scenarios,
                scenario_methods=methods,
                evidence_schema_bytes=EVIDENCE_SCHEMA.read_bytes(),
                require_trust=False,
            )
        }
        checks += 1
        if "E_CONTENT_DIGEST" not in subject_codes:
            print("SELF-TEST FAIL: worktree digest replaced the subject-blob digest")
            return 1

    def substitute(value: Any, old: str, new: str) -> Any:
        if isinstance(value, str):
            return value.replace(old, new)
        if isinstance(value, list):
            return [substitute(item, old, new) for item in value]
        if isinstance(value, dict):
            return {key: substitute(item, old, new) for key, item in value.items()}
        return value

    def build_trusted_harness(
        harness_root: Path,
        *,
        weaken_subject_schema: bool = False,
        document_mutation: Callable[[dict[str, Any]], None] | None = None,
    ) -> tuple[dict[str, Any], Path, Path, Path, Path, bytes]:
        """Create a subject repo, a trusted tool checkout and a trust context."""

        subject_repo = harness_root / "subject"
        trusted_repo = harness_root / "trusted"
        external = harness_root / "external"
        for repository in (subject_repo, trusted_repo):
            (repository / "quality/content").mkdir(parents=True)
            (repository / "quality/scenarios").mkdir(parents=True)
            (repository / "quality/schemas").mkdir(parents=True)
            (repository / "quality/scripts").mkdir(parents=True)
            (repository / ".github/workflows").mkdir(parents=True)
        external.mkdir(parents=True)
        schema_bytes = EVIDENCE_SCHEMA.read_bytes()
        subject_schema_bytes = (
            b'{"type":"object"}\n' if weaken_subject_schema else schema_bytes
        )
        component_bytes = {
            "manifest": b'{"subject":"manifest"}\n',
            "scenarioContract": b'{"subject":"scenarios"}\n',
            "evidenceSchema": subject_schema_bytes,
            "evidenceValidator": Path(__file__).read_bytes(),
            "ajvWrapper": (
                ROOT / "quality/scripts/validate_evidence_schema.mjs"
            ).read_bytes(),
            "packageManifest": b'{"name":"harness-quality"}\n',
            "packageLock": b'{"lockfileVersion":3}\n',
            "gateRunner": b"#!/usr/bin/env python3\n",
            "authorizeWorkflow": b"name: quality-gate\n",
        }
        trusted_component_bytes = dict(
            component_bytes, evidenceSchema=schema_bytes
        )
        for component_id, raw_bytes in component_bytes.items():
            target = subject_repo / Path(TRUST_BUNDLE_COMPONENTS[component_id])
            target.write_bytes(raw_bytes)
        for component_id, raw_bytes in trusted_component_bytes.items():
            target = trusted_repo / Path(TRUST_BUNDLE_COMPONENTS[component_id])
            target.write_bytes(raw_bytes)
        (trusted_repo / ".gitignore").write_text(
            "quality/node_modules/\n", encoding="utf-8"
        )
        (trusted_repo / "quality/node_modules").symlink_to(
            ROOT / "quality/node_modules"
        )
        for repository in (subject_repo, trusted_repo):
            for arguments in (
                ("init", "-q"),
                ("config", "user.email", "self-test@example.invalid"),
                ("config", "user.name", "Evidence Self Test"),
                ("add", "."),
                ("commit", "-qm", "harness"),
            ):
                subprocess.run(
                    ["git", *arguments],
                    cwd=repository,
                    check=True,
                    stdout=subprocess.DEVNULL,
                    stderr=subprocess.DEVNULL,
                )
        subject_commit = _git("rev-parse", "HEAD", root=subject_repo)
        subject_tree = _git("rev-parse", "HEAD^{tree}", root=subject_repo)
        trusted_commit = _git("rev-parse", "HEAD", root=trusted_repo)
        node_version = _node_version()
        if node_version is None:
            raise ValueError("node is unavailable for the trusted harness")

        document = substitute(
            copy.deepcopy(fixture), fixture["subject"]["commitSha"], subject_commit
        )
        document["subject"].update(
            commitSha=subject_commit, treeSha=subject_tree
        )
        document["ci"]["headSha"] = subject_commit
        for component_id, (_, digest_field) in CONTENT_COMPONENT_FIELDS.items():
            subject_digest = sha256_bytes(component_bytes[component_id])
            trusted_digest = sha256_bytes(trusted_component_bytes[component_id])
            document["content"][digest_field] = subject_digest
            component = document["trustBundle"]["components"][component_id]
            component["subjectSha256"] = subject_digest
            component["trustedSha256"] = trusted_digest
        document["trustBundle"]["trustedCommitSha"] = trusted_commit
        document["trustBundle"]["nodeVersion"] = node_version
        if document_mutation is not None:
            document_mutation(document)

        payloads: dict[str, Any] = {}
        for command in document["commands"]:
            payloads[command["stdoutArtifact"]["path"]] = b"stdout\n"
            payloads[command["stderrArtifact"]["path"]] = b""
            payloads[command["checksArtifact"]["path"]] = {
                "schemaVersion": "gate-check-result-v1",
                "gateId": document["gateId"],
                "subjectCommitSha": subject_commit,
                "commandId": command["id"],
                "executor": command["executor"],
                "command": command["command"],
                "workingDirectory": command["workingDirectory"],
                "durationSeconds": command["durationSeconds"],
                "exitCode": command["exitCode"],
                "conclusion": command["conclusion"],
                "checks": command["checks"],
                "scenarioIds": command["scenarioIds"],
            }
        for metric in document["rawMetrics"]:
            metric_payload: dict[str, Any] = {
                "name": metric["name"],
                "unit": metric["unit"],
            }
            if "samples" in metric:
                metric_payload["samples"] = metric["samples"]
            if "measurement" in metric:
                metric_payload["measurement"] = metric["measurement"]
            payloads[metric["rawArtifact"]["path"]] = metric_payload
        ci_section = document["ci"]
        payloads[ci_section["attestationArtifact"]["path"]] = {
            "schemaVersion": "github-actions-attestation-v1",
            "provider": ci_section["provider"],
            "repository": ci_section["repository"],
            "workflowPath": ci_section["workflowPath"],
            "runId": ci_section["runId"],
            "runAttempt": ci_section["runAttempt"],
            "jobId": ci_section["jobId"],
            "jobName": ci_section["jobName"],
            "headSha": ci_section["headSha"],
            "url": ci_section["url"],
            "conclusion": ci_section["conclusion"],
        }
        reviewer_section = document["reviewer"]
        payloads[reviewer_section["reviewArtifact"]["path"]] = {
            "schemaVersion": "gate-review-v1",
            "gateId": document["gateId"],
            "subjectCommitSha": subject_commit,
            "subjectTreeSha": subject_tree,
            "reviewerId": reviewer_section["id"],
            "implementationWriter": document["implementationWriter"],
            "reproducedCommandId": reviewer_section["reproducedCommandId"],
            "result": "approve",
        }
        for artifact in _artifact_objects(document):
            payload = payloads[artifact["path"]]
            raw = (
                payload
                if isinstance(payload, bytes)
                else json.dumps(payload, indent=2).encode("utf-8")
            )
            artifact_path = subject_repo / Path(artifact["path"])
            artifact_path.parent.mkdir(parents=True, exist_ok=True)
            artifact_path.write_bytes(raw)
            artifact["sha256"] = sha256_bytes(raw)
            artifact["bytes"] = len(raw)

        evidence_file = subject_repo / Path(document["attempt"]["evidencePath"])
        evidence_file.parent.mkdir(parents=True, exist_ok=True)
        evidence_raw = json.dumps(document, indent=2).encode("utf-8")
        evidence_file.write_bytes(evidence_raw)
        evidence_digest = sha256_bytes(evidence_raw)
        chain_entry = {
            "gateId": document["gateId"],
            "evidencePath": document["attempt"]["evidencePath"],
            "evidenceSha256": evidence_digest,
            "subjectCommitSha": subject_commit,
            "subjectTreeSha": subject_tree,
            "ciRunId": ci_section["runId"],
            "ciJobId": ci_section["jobId"],
            "ciAttestationSha256": ci_section["attestationArtifact"]["sha256"],
            "reviewArtifactSha256": reviewer_section["reviewArtifact"]["sha256"],
        }
        context = {
            "schemaVersion": TRUST_CONTEXT_VERSION,
            "repository": "VibecodingGermany/Project_Nova",
            "workflowPath": ".github/workflows/quality-gate.yml",
            "authorizingRunId": "999",
            "authorizingRunAttempt": 1,
            "authorizingJob": "gate-evidence-authorize",
            "subjectCommitSha": subject_commit,
            "subjectTreeSha": subject_tree,
            "evidencePath": document["attempt"]["evidencePath"],
            "evidenceSha256": evidence_digest,
            "evidenceCiRunId": ci_section["runId"],
            "evidenceCiJobId": ci_section["jobId"],
            "ciAttestationSha256": ci_section["attestationArtifact"]["sha256"],
            "reviewerId": reviewer_section["id"],
            "reviewArtifactSha256": reviewer_section["reviewArtifact"]["sha256"],
            "trustedToolCommitSha": trusted_commit,
            "authorizedEvidence": [chain_entry],
        }
        context_path = external / "trust-context.json"
        context_raw = json.dumps(context, indent=2).encode("utf-8")
        context_path.write_bytes(context_raw)
        return document, evidence_file, context_path, subject_repo, trusted_repo, context_raw

    def harness_environment(context_raw: bytes) -> dict[str, str]:
        return {
            "GITHUB_ACTIONS": "true",
            "GITHUB_REPOSITORY": "VibecodingGermany/Project_Nova",
            "GITHUB_RUN_ID": "999",
            "GITHUB_RUN_ATTEMPT": "1",
            "GITHUB_JOB": "gate-evidence-authorize",
            "GITHUB_WORKFLOW_REF": (
                "VibecodingGermany/Project_Nova/"
                ".github/workflows/quality-gate.yml@refs/heads/main"
            ),
            "NOVA_TRUST_CONTEXT_SHA256": sha256_bytes(context_raw),
        }

    environment_names = (
        "GITHUB_ACTIONS",
        "GITHUB_REPOSITORY",
        "GITHUB_RUN_ID",
        "GITHUB_RUN_ATTEMPT",
        "GITHUB_JOB",
        "GITHUB_WORKFLOW_REF",
        "NOVA_TRUST_CONTEXT_SHA256",
        "PATH",
    )
    saved_environment = {name: os.environ.get(name) for name in environment_names}
    try:
        with tempfile.TemporaryDirectory() as temp:
            try:
                (
                    trusted_document,
                    trusted_evidence,
                    trusted_context_path,
                    subject_repo,
                    trusted_repo,
                    context_raw,
                ) = build_trusted_harness(Path(temp))
            except (OSError, ValueError) as error:
                print(f"SELF-TEST FAIL: cannot build trusted harness: {error}")
                return 1
            os.environ.update(harness_environment(context_raw))

            def trusted_codes(
                candidate: dict[str, Any],
                context_path: Path,
                active_trusted_repo: Path = trusted_repo,
            ) -> set[str]:
                return {
                    code
                    for code, _ in validate_document(
                        candidate,
                        trusted_evidence,
                        root=subject_repo,
                        verify_files=True,
                        verify_git=True,
                        profiles=profiles,
                        scenario_definitions=scenarios,
                        scenario_methods=methods,
                        trust_context_path=context_path,
                        trusted_checkout=active_trusted_repo,
                        require_trust=True,
                    )
                }

            baseline_errors = trusted_codes(trusted_document, trusted_context_path)
            checks += 1
            if baseline_errors:
                print(
                    "SELF-TEST FAIL: trusted Schema 1.3 baseline was rejected: "
                    f"{sorted(baseline_errors)}"
                )
                return 1
            if not _is_authorized_pass(trusted_document, []):
                print("SELF-TEST FAIL: trusted baseline was not an authorized pass")
                return 1

            lockfile_path = trusted_repo / "quality/package-lock.json"
            lockfile_path.write_bytes(
                b'{"lockfileVersion":3,"tampered":true}\n'
            )
            checks += 1
            if "E_TRUSTED_TOOL" not in trusted_codes(
                trusted_document, trusted_context_path
            ):
                print("SELF-TEST FAIL: tampered trusted lockfile was accepted")
                return 1
            wrapper_path = trusted_repo / Path(
                "quality/scripts/validate_evidence_schema.mjs"
            )
            wrapper_path.write_bytes(
                wrapper_path.read_bytes() + b"\n// tampered\n"
            )
            for arguments in (
                ("add", "."),
                ("commit", "-qm", "tamper wrapper"),
            ):
                subprocess.run(
                    ["git", *arguments],
                    cwd=trusted_repo,
                    check=True,
                    stdout=subprocess.DEVNULL,
                    stderr=subprocess.DEVNULL,
                )
            checks += 1
            if "E_TRUSTED_TOOL" not in trusted_codes(
                trusted_document, trusted_context_path
            ):
                print("SELF-TEST FAIL: tampered trusted Ajv wrapper was accepted")
                return 1
            subprocess.run(
                ["git", "reset", "--hard", "-q", "HEAD~1"],
                cwd=trusted_repo,
                check=True,
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL,
            )

            external = Path(temp) / "external"
            for name, chain in (
                ("incomplete", []),
                (
                    "extra",
                    [
                        strict_load(trusted_context_path)["authorizedEvidence"][0],
                        strict_load(trusted_context_path)["authorizedEvidence"][0],
                    ],
                ),
            ):
                mutated_context = strict_load(trusted_context_path)
                mutated_context["authorizedEvidence"] = chain
                mutated_path = external / f"trust-context-{name}.json"
                mutated_raw = json.dumps(mutated_context).encode("utf-8")
                mutated_path.write_bytes(mutated_raw)
                os.environ["NOVA_TRUST_CONTEXT_SHA256"] = sha256_bytes(mutated_raw)
                checks += 1
                if "E_AUTHORIZATION_CHAIN" not in trusted_codes(
                    trusted_document, mutated_path
                ):
                    print(
                        f"SELF-TEST FAIL: {name} authorization chain was accepted"
                    )
                    return 1

            swapped_context = strict_load(trusted_context_path)
            base_entry = swapped_context["authorizedEvidence"][0]
            swapped_context["authorizedEvidence"] = [
                dict(base_entry, gateId="G1"),
                base_entry,
            ]
            swapped_path = external / "trust-context-swapped.json"
            swapped_raw = json.dumps(swapped_context).encode("utf-8")
            swapped_path.write_bytes(swapped_raw)
            os.environ["NOVA_TRUST_CONTEXT_SHA256"] = sha256_bytes(swapped_raw)
            swapped_document = copy.deepcopy(trusted_document)
            swapped_document["gateId"] = "G1"
            swapped_errors: list[tuple[str, str]] = []
            _validate_trust_context(
                swapped_path,
                swapped_document,
                trusted_evidence,
                subject_repo,
                swapped_errors,
                gate_id="G1",
            )
            checks += 1
            if "E_AUTHORIZATION_CHAIN" not in {
                code for code, _ in swapped_errors
            }:
                print("SELF-TEST FAIL: swapped authorization chain was accepted")
                return 1

        with tempfile.TemporaryDirectory() as temp:
            try:
                (
                    weakened_document,
                    weakened_evidence,
                    weakened_context_path,
                    weakened_subject,
                    weakened_trusted,
                    weakened_context_raw,
                ) = build_trusted_harness(
                    Path(temp),
                    weaken_subject_schema=True,
                    document_mutation=lambda value: value.update(forbidden=True),
                )
            except (OSError, ValueError) as error:
                print(f"SELF-TEST FAIL: cannot build trusted harness: {error}")
                return 1
            os.environ.update(harness_environment(weakened_context_raw))
            weakened_codes = {
                code
                for code, _ in validate_document(
                    weakened_document,
                    weakened_evidence,
                    root=weakened_subject,
                    verify_files=True,
                    verify_git=True,
                    profiles=profiles,
                    scenario_definitions=scenarios,
                    scenario_methods=methods,
                    trust_context_path=weakened_context_path,
                    trusted_checkout=weakened_trusted,
                    require_trust=True,
                )
            }
            checks += 1
            if "E_JSON_SCHEMA" not in weakened_codes:
                print(
                    "SELF-TEST FAIL: manipulated subject schema escaped the "
                    "trusted schema"
                )
                return 1

        missing_checkout_codes = {
            code
            for code, _ in validate_document(
                copy.deepcopy(fixture),
                evidence_path,
                root=Path("/repo"),
                verify_files=False,
                verify_git=False,
                profiles=profiles,
                scenario_definitions=scenarios,
                scenario_methods=methods,
                evidence_schema_bytes=EVIDENCE_SCHEMA.read_bytes(),
                trusted_checkout=Path("/nonexistent-nova-trusted-checkout"),
                require_trust=False,
            )
        }
        checks += 1
        if "E_TRUSTED_TOOL" not in missing_checkout_codes:
            print("SELF-TEST FAIL: missing trusted checkout was accepted")
            return 1

        os.environ["PATH"] = ""
        try:
            node_missing_errors = _schema_validation_errors(
                copy.deepcopy(fixture), EVIDENCE_SCHEMA.read_bytes()
            )
        finally:
            if saved_environment["PATH"] is None:
                os.environ.pop("PATH", None)
            else:
                os.environ["PATH"] = saved_environment["PATH"]
        checks += 1
        if not node_missing_errors:
            print("SELF-TEST FAIL: missing node was accepted fail-open")
            return 1

        with tempfile.TemporaryDirectory() as temp:
            fake_node = Path(temp) / "node"
            fake_node.write_text("#!/bin/sh\n/bin/sleep 5\n", encoding="utf-8")
            fake_node.chmod(0o755)
            os.environ["PATH"] = temp
            try:
                timeout_errors = _schema_validation_errors(
                    copy.deepcopy(fixture),
                    EVIDENCE_SCHEMA.read_bytes(),
                    timeout_seconds=0.2,
                )
            finally:
                if saved_environment["PATH"] is None:
                    os.environ.pop("PATH", None)
                else:
                    os.environ["PATH"] = saved_environment["PATH"]
        checks += 1
        if not any("timed out" in message for message in timeout_errors):
            print("SELF-TEST FAIL: hanging schema subprocess was accepted fail-open")
            return 1
    finally:
        for name, previous in saved_environment.items():
            if previous is None:
                os.environ.pop(name, None)
            else:
                os.environ[name] = previous

    with tempfile.TemporaryDirectory() as temp:
        duplicate_path = Path(temp) / "duplicate.json"
        duplicate_path.write_text('{"a":1,"a":2}', encoding="utf-8")
        non_finite_path = Path(temp) / "non-finite.json"
        non_finite_path.write_text('{"a":NaN}', encoding="utf-8")
        for path in (duplicate_path, non_finite_path):
            checks += 1
            try:
                strict_load(path)
            except StrictJsonError:
                pass
            else:
                print(f"SELF-TEST FAIL: strict loader accepted {path.name}")
                return 1

    print(f"OK: {checks} Evidence-Semantik-Negativkontrollen bestanden.")
    return 0


def _is_authorized_pass(
    document: Any, errors: list[tuple[str, str]]
) -> bool:
    verdict = document.get("verdict") if isinstance(document, dict) else None
    return (
        not errors
        and isinstance(verdict, dict)
        and verdict.get("result") == "pass"
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("evidence", nargs="*", type=Path)
    parser.add_argument(
        "--self-test",
        action="store_true",
        help="run generated positive/negative controls without creating evidence",
    )
    parser.add_argument(
        "--trust-context",
        type=Path,
        help=(
            "external protected-CI trust context; required to authorize "
            "verdict=pass"
        ),
    )
    parser.add_argument(
        "--trusted-tool-checkout",
        type=Path,
        help=(
            "subject-independent trusted tool checkout (schema, validators, "
            "pinned npm dependencies); required to authorize verdict=pass"
        ),
    )
    arguments = parser.parse_args()

    if arguments.self_test:
        return run_self_test()
    if not arguments.evidence:
        parser.error("provide at least one GateEvidence.json or use --self-test")

    failed = False
    for path in arguments.evidence:
        try:
            document = strict_load(path)
        except (OSError, StrictJsonError) as error:
            print(f"{path}: E_STRICT_JSON: {error}")
            failed = True
            continue
        errors = validate_document(
            document,
            path,
            trust_context_path=arguments.trust_context,
            trusted_checkout=arguments.trusted_tool_checkout,
        )
        if errors:
            failed = True
            for code, message in errors:
                print(f"{path}: {code}: {message}")
        elif _is_authorized_pass(document, errors):
            print(f"AUTHORIZED PASS: {path}")
        else:
            print(f"VALID NON-PASS EVIDENCE: {path}")
            failed = True
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
