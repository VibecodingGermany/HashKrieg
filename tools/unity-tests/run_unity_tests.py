#!/usr/bin/env python3
"""Run the Unity EditMode and PlayMode test lanes locally and prove they ran.

The CI only runs ``dotnet test tools/Nova.SimRunner.Tests`` (no Unity licence
there). The two Unity lanes under ``Assets/Tests/`` therefore run nowhere unless
somebody starts them by hand, and a lane that runs nowhere warns nobody (#110,
D-125). This script is the one command for that hand run. It exists to make a
green result *believable*, not merely to start the editor:

* ``-quit`` is never passed. Together with ``-runTests`` it makes Unity exit
  before any test ran, write no result file and still report success.
* The previous result file is deleted first, so an old green file can never be
  mistaken for this run.
* After the run it checks that the result file exists, is fresh, contains at
  least one test case and has no failed one. A missing, empty, stale, unreadable
  or red file is a failure (exit 1).
* ``-nographics`` is passed for EditMode only. PlayMode renders (screenshot
  capture, UI Toolkit panels) and needs the GPU.
* It refuses to start while an editor has the project open
  (``Library/EditorInstance.json``), because the batch run would otherwise fail
  with a misleading "project is locked" message.
* ``git status`` is compared before and after, and every change the run itself
  caused is reported. A test run is known to rewrite
  ``ProjectSettings/ProjectSettings.asset`` and to leave ``InitTestScene*``
  scenes behind; this script warns about both and never reverts anything.

Usage (repository root)::

    python3 tools/unity-tests/run_unity_tests.py all     # EditMode, then PlayMode
    python3 tools/unity-tests/run_unity_tests.py edit
    python3 tools/unity-tests/run_unity_tests.py play
    python3 tools/unity-tests/run_unity_tests.py --self-test

The Unity version comes from ``ProjectSettings/ProjectVersion.txt``. The editor
binary is looked up in the Unity Hub default location of the platform, or taken
from ``--unity`` / the ``UNITY_PATH`` environment variable. Results and logs go
to ``output/unity-tests/`` (git-ignored) unless ``--results-dir`` says otherwise.

On success the last line is the evidence line for the pull request template,
for example ``Unity-Spuren: EditMode 10/10, PlayMode 3/3``. Exit codes:
0 = every requested lane is green, 1 = a lane is red or its evidence is
invalid, 2 = a precondition failed (editor open, Unity missing, bad usage).
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import time
import xml.etree.ElementTree as ET

REPO_ROOT = Path(__file__).resolve().parents[2]

LANES = {
    "edit": ("EditMode", True),   # (Unity -testPlatform value, pass -nographics)
    "play": ("PlayMode", False),  # PlayMode renders and needs a graphics device
}

# Files a Unity test run is known to touch. They get a dedicated warning
# because they are the ones that slip into a commit unnoticed.
KNOWN_RESIDUE = (
    (re.compile(r"(^|/)InitTestScene[^/]*\.unity(\.meta)?$"),
     "temporary test-runner scene left behind; delete it before committing"),
    (re.compile(r"^ProjectSettings/ProjectSettings\.asset$"),
     "the run rewrote ProjectSettings.asset (scripting define symbols are the "
     "usual victim); inspect `git diff` and revert it unless intended"),
    (re.compile(r"(^|/)\.unity-tests(/|$)"),
     "test-runner scratch directory left behind; delete it before committing"),
)

# Freshness tolerance for file-system timestamp granularity.
FRESHNESS_SLACK_SECONDS = 2.0


# --------------------------------------------------------------------------
# Result evaluation
# --------------------------------------------------------------------------

class LaneResult:
    """Outcome of evaluating one NUnit3 result file."""

    def __init__(self, ok: bool, passed: int, failed: int, skipped: int,
                 problems: list[str]) -> None:
        self.ok = ok
        self.passed = passed
        self.failed = failed
        self.skipped = skipped
        self.problems = problems

    @property
    def total(self) -> int:
        return self.passed + self.failed + self.skipped

    def fraction(self) -> str:
        """``N/N`` when everything passed, ``P/T`` otherwise."""
        return f"{self.passed}/{self.total}"


def evaluate_results(path: Path, not_before: float) -> LaneResult:
    """Judge a Unity result file. ``not_before`` is the run's start time.

    The file is accepted only if it exists, is not older than the run start,
    parses as NUnit3 XML, contains at least one ``test-case`` and no failed one.
    Counting happens on ``test-case`` nodes only: suites and fixtures carry a
    ``result`` attribute as well and would inflate every number.
    """
    problems: list[str] = []
    try:
        stat = path.stat()
    except OSError:
        return LaneResult(False, 0, 0, 0, [f"result file missing: {path}"])
    if stat.st_size == 0:
        return LaneResult(False, 0, 0, 0, [f"result file is empty: {path}"])
    if stat.st_mtime < not_before - FRESHNESS_SLACK_SECONDS:
        age = not_before - stat.st_mtime
        return LaneResult(
            False, 0, 0, 0,
            [f"result file is stale ({age:.0f} s older than this run): {path}"])
    try:
        root = ET.parse(path).getroot()
    except (ET.ParseError, OSError) as error:
        return LaneResult(False, 0, 0, 0, [f"result file is not valid XML: {error}"])

    passed = failed = skipped = 0
    failed_names: list[str] = []
    for case in root.iter("test-case"):
        outcome = case.get("result", "")
        if outcome == "Passed":
            passed += 1
        elif outcome == "Failed":
            failed += 1
            failed_names.append(case.get("fullname") or case.get("name") or "?")
        else:  # Skipped, Inconclusive, Ignored: visible, but not a failure
            skipped += 1

    if passed + failed + skipped == 0:
        problems.append("result file contains no test case (nothing ran)")
    if failed:
        shown = ", ".join(failed_names[:5])
        more = f" (+{len(failed_names) - 5} more)" if len(failed_names) > 5 else ""
        problems.append(f"{failed} test case(s) failed: {shown}{more}")
    if passed == 0 and not problems:
        problems.append("no test case passed")
    return LaneResult(not problems, passed, failed, skipped, problems)


# --------------------------------------------------------------------------
# Preconditions
# --------------------------------------------------------------------------

def read_unity_version(repo: Path) -> str:
    text = (repo / "ProjectSettings" / "ProjectVersion.txt").read_text(encoding="utf-8")
    match = re.search(r"^m_EditorVersion:\s*(\S+)\s*$", text, re.MULTILINE)
    if not match:
        raise ValueError("m_EditorVersion not found in ProjectSettings/ProjectVersion.txt")
    return match.group(1)


def default_unity_candidates(version: str) -> list[Path]:
    home = Path.home()
    return [
        Path(f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/MacOS/Unity"),
        Path(f"C:/Program Files/Unity/Hub/Editor/{version}/Editor/Unity.exe"),
        home / f"Unity/Hub/Editor/{version}/Editor/Unity",
    ]


def find_unity(version: str, explicit: str | None) -> Path | None:
    chosen = explicit or os.environ.get("UNITY_PATH")
    if chosen:
        path = Path(chosen)
        return path if path.is_file() else None
    return next((c for c in default_unity_candidates(version) if c.is_file()), None)


def _pid_alive(pid: int) -> bool:
    if pid <= 0:
        return False
    try:
        os.kill(pid, 0)
    except ProcessLookupError:
        return False
    except PermissionError:
        return True
    except OSError:
        return False
    return True


def editor_lock_problem(repo: Path) -> str | None:
    """Return a message when an editor appears to have the project open.

    ``Library/EditorInstance.json`` is written by a running editor and records
    its ``process_id``. A file whose process is gone is the leftover of a crash;
    it is reported by the caller as a warning, not as a block. A file that
    cannot be read counts as a live editor (fail closed).
    """
    marker = repo / "Library" / "EditorInstance.json"
    if not marker.exists():
        return None
    pid = None
    try:
        pid = int(json.loads(marker.read_text(encoding="utf-8")).get("process_id", 0))
    except (OSError, ValueError, TypeError, AttributeError):
        return (f"{marker} exists but is unreadable; assuming the editor has the "
                "project open. Close it (or delete the file if no editor runs).")
    if _pid_alive(pid):
        return (f"the Unity editor (pid {pid}) has this project open. Close it "
                "first; a batch run would fail with a misleading lock message.")
    return None


def build_command(unity: Path, repo: Path, lane: str, results: Path, log: Path,
                  test_filter: str | None) -> list[str]:
    platform_name, no_graphics = LANES[lane]
    command = [str(unity), "-batchmode"]
    if no_graphics:
        command.append("-nographics")
    command += [
        "-projectPath", str(repo),
        "-runTests",
        "-testPlatform", platform_name,
        "-testResults", str(results),
        "-logFile", str(log),
    ]
    if test_filter:
        command += ["-testFilter", test_filter]
    # Never "-quit": it makes -runTests exit before any test ran (see docstring).
    return command


# --------------------------------------------------------------------------
# git status comparison
# --------------------------------------------------------------------------

def _file_digest(repo: Path, relative: str) -> str:
    path = repo / relative
    try:
        if path.is_file():
            return hashlib.sha1(path.read_bytes()).hexdigest()
        return "dir" if path.exists() else "gone"
    except OSError:
        return "unreadable"


def git_snapshot(repo: Path) -> dict[str, str] | None:
    """Map every dirty path to ``status:content-digest``; ``None`` if no git."""
    try:
        out = subprocess.run(
            ["git", "-C", str(repo), "status", "--porcelain=v1", "-z",
             "--untracked-files=all"],
            capture_output=True, check=True, timeout=120,
        ).stdout.decode("utf-8", "replace")
    except (OSError, subprocess.SubprocessError):
        return None
    snapshot: dict[str, str] = {}
    entries = out.split("\0")
    index = 0
    while index < len(entries):
        entry = entries[index]
        index += 1
        if len(entry) < 4:
            continue
        status, name = entry[:2], entry[3:]
        if status[0] in "RC":  # rename/copy: the next token is the origin path
            index += 1
        snapshot[name] = f"{status}:{_file_digest(repo, name)}"
    return snapshot


def diff_snapshots(before: dict[str, str], after: dict[str, str]) -> list[str]:
    """Paths the run itself created or changed (new or different digest)."""
    return sorted(p for p, state in after.items() if before.get(p) != state)


def residue_warning(path: str) -> str | None:
    for pattern, advice in KNOWN_RESIDUE:
        if pattern.search(path):
            return advice
    return None


# --------------------------------------------------------------------------
# Running a lane
# --------------------------------------------------------------------------

def run_lane(lane: str, unity: Path, repo: Path, results_dir: Path,
             timeout: int, test_filter: str | None) -> LaneResult:
    platform_name, _ = LANES[lane]
    results = results_dir / f"{platform_name.lower()}-results.xml"
    log = results_dir / f"{platform_name.lower()}-tests.log"
    results_dir.mkdir(parents=True, exist_ok=True)
    for stale in (results, log):
        if stale.exists():
            stale.unlink()

    command = build_command(unity, repo, lane, results, log, test_filter)
    assert "-quit" not in command, "-quit would skip the whole test run"
    print(f"==> {platform_name}: {' '.join(command)}", flush=True)
    started = time.time()
    try:
        completed = subprocess.run(command, timeout=timeout, check=False)
        exit_code: int | None = completed.returncode
    except subprocess.TimeoutExpired:
        exit_code = None
    except OSError as error:
        return LaneResult(False, 0, 0, 0, [f"could not start Unity: {error}"])
    elapsed = time.time() - started

    result = evaluate_results(results, started)
    if exit_code is None:
        result.ok = False
        result.problems.append(f"timed out after {timeout} s (log: {log})")
    elif exit_code != 0:
        # Unity exits 2 when tests fail and 3 when the run itself broke
        # (compile errors, unreadable project). Either way this lane is not green.
        result.ok = False
        result.problems.append(f"Unity exited with code {exit_code} (log: {log})")
    print(f"    {platform_name}: {result.fraction()} passed"
          + (f", {result.failed} failed" if result.failed else "")
          + (f", {result.skipped} skipped/inconclusive" if result.skipped else "")
          + f" in {elapsed:.0f} s", flush=True)
    for problem in result.problems:
        print(f"    PROBLEM: {problem}", flush=True)
    return result


def main_run(args: argparse.Namespace) -> int:
    repo = Path(args.project).resolve()
    try:
        version = read_unity_version(repo)
    except (OSError, ValueError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 2
    unity = find_unity(version, args.unity)
    if unity is None:
        print(f"ERROR: Unity {version} not found. Install it via Unity Hub or point "
              "--unity / UNITY_PATH at the editor binary.", file=sys.stderr)
        return 2
    lock = editor_lock_problem(repo)
    if lock:
        print(f"ERROR: {lock}", file=sys.stderr)
        return 2
    marker = repo / "Library" / "EditorInstance.json"
    if marker.exists():
        print(f"WARNING: stale {marker} (its process is gone); continuing.")

    lanes = ["edit", "play"] if args.lane == "all" else [args.lane]
    results_dir = Path(args.results_dir) if args.results_dir else repo / "output" / "unity-tests"
    results_dir = results_dir.resolve()
    print(f"Unity {version} at {unity}\nResults: {results_dir}", flush=True)

    before = git_snapshot(repo)
    outcomes: dict[str, LaneResult] = {}
    for lane in lanes:
        outcomes[lane] = run_lane(lane, unity, repo, results_dir, args.timeout, args.test_filter)

    # Residue check: report what the run changed in the working tree.
    after = git_snapshot(repo)
    if before is None or after is None:
        print("WARNING: git status unavailable; working-tree changes were not compared.")
    else:
        changed = diff_snapshots(before, after)
        for path in changed:
            advice = residue_warning(path)
            print(f"WARNING: the run changed {path}" + (f" - {advice}" if advice else ""))
        if not changed:
            print("git status: the run left no new changes in the working tree.")

    parts = []
    for lane in lanes:
        label = LANES[lane][0]
        outcome = outcomes[lane]
        parts.append(f"{label} {outcome.fraction()}" if outcome.ok else f"{label} ROT")
    green = all(outcome.ok for outcome in outcomes.values())
    print(("Unity-Spuren: " if green else "Unity-Spuren NICHT GRUEN: ") + ", ".join(parts))
    return 0 if green else 1


# --------------------------------------------------------------------------
# Self-test
# --------------------------------------------------------------------------

def _fake_xml(cases: list[tuple[str, str]]) -> str:
    rows = "".join(
        f'<test-case id="{i}" name="{name}" fullname="Fixture.{name}" result="{result}"/>'
        for i, (name, result) in enumerate(cases)
    )
    return ('<?xml version="1.0" encoding="utf-8"?>'
            '<test-run id="2" result="Passed"><test-suite type="Assembly" '
            f'result="Passed"><test-suite type="TestFixture" result="Passed">{rows}'
            '</test-suite></test-suite></test-run>')


def self_test() -> int:
    failures: list[str] = []

    def expect(label: str, condition: bool) -> None:
        if not condition:
            failures.append(label)

    with tempfile.TemporaryDirectory(prefix="nova-unity-selftest-") as scratch:
        root = Path(scratch)
        now = time.time()

        # Evaluation: every defect yields not-ok, only the green file passes.
        missing = evaluate_results(root / "missing.xml", now)
        expect("missing file must fail", not missing.ok)

        empty_file = root / "empty.xml"
        empty_file.write_text("", encoding="utf-8")
        expect("zero-byte file must fail", not evaluate_results(empty_file, now - 5).ok)

        no_cases = root / "nocases.xml"
        no_cases.write_text(_fake_xml([]), encoding="utf-8")
        result = evaluate_results(no_cases, now - 5)
        expect("file without test cases must fail", not result.ok)
        expect("file without test cases must say so",
               any("no test case" in p for p in result.problems))

        garbage = root / "garbage.xml"
        garbage.write_text("<test-run><broken", encoding="utf-8")
        expect("malformed XML must fail", not evaluate_results(garbage, now - 5).ok)

        stale = root / "stale.xml"
        stale.write_text(_fake_xml([("A", "Passed")]), encoding="utf-8")
        os.utime(stale, (now - 3600, now - 3600))
        stale_result = evaluate_results(stale, now)
        expect("stale file must fail", not stale_result.ok)
        expect("stale file must say so", any("stale" in p for p in stale_result.problems))

        red = root / "red.xml"
        red.write_text(_fake_xml([("A", "Passed"), ("B", "Failed")]), encoding="utf-8")
        red_result = evaluate_results(red, now - 5)
        expect("red file must fail", not red_result.ok)
        expect("red file counts one failure", red_result.failed == 1 and red_result.passed == 1)

        green = root / "green.xml"
        green.write_text(_fake_xml([("A", "Passed"), ("B", "Passed"), ("C", "Passed")]),
                         encoding="utf-8")
        green_result = evaluate_results(green, now - 5)
        expect("green file must pass", green_result.ok)
        expect("green file reports 3/3", green_result.fraction() == "3/3")

        skipped = root / "skipped.xml"
        skipped.write_text(_fake_xml([("A", "Passed"), ("B", "Skipped")]), encoding="utf-8")
        skipped_result = evaluate_results(skipped, now - 5)
        expect("skipped case is visible, not a failure",
               skipped_result.ok and skipped_result.fraction() == "1/2")

        only_skipped = root / "onlyskipped.xml"
        only_skipped.write_text(_fake_xml([("A", "Skipped")]), encoding="utf-8")
        expect("a run in which nothing passed must fail",
               not evaluate_results(only_skipped, now - 5).ok)

        # Command line: -quit never, -nographics only for EditMode.
        unity = Path("/unity")
        edit = build_command(unity, root, "edit", root / "e.xml", root / "e.log", None)
        play = build_command(unity, root, "play", root / "p.xml", root / "p.log", "Fixture")
        expect("-quit must never be passed", "-quit" not in edit and "-quit" not in play)
        expect("EditMode runs with -nographics", "-nographics" in edit)
        expect("PlayMode runs without -nographics", "-nographics" not in play)
        expect("PlayMode uses the PlayMode platform", play[play.index("-testPlatform") + 1] == "PlayMode")
        expect("filter is forwarded", play[-2:] == ["-testFilter", "Fixture"])

        # Unity version parsing.
        project = root / "proj"
        (project / "ProjectSettings").mkdir(parents=True)
        (project / "ProjectSettings" / "ProjectVersion.txt").write_text(
            "m_EditorVersion: 6000.5.4f1\nm_EditorVersionWithRevision: 6000.5.4f1 (d550df8bd089)\n",
            encoding="utf-8")
        expect("version is read from ProjectVersion.txt",
               read_unity_version(project) == "6000.5.4f1")

        # Editor lock: live pid blocks, dead pid does not, garbage blocks.
        (project / "Library").mkdir()
        expect("no marker means no lock", editor_lock_problem(project) is None)
        marker = project / "Library" / "EditorInstance.json"
        marker.write_text(f'{{"process_id": {os.getpid()}}}', encoding="utf-8")
        expect("live editor pid blocks the run", editor_lock_problem(project) is not None)
        marker.write_text('{"process_id": 2147483646}', encoding="utf-8")
        expect("dead editor pid is a stale file, not a lock", editor_lock_problem(project) is None)
        marker.write_text("not json", encoding="utf-8")
        expect("unreadable marker fails closed", editor_lock_problem(project) is not None)

        # Working-tree comparison and residue warnings.
        before = {"a.cs": "M :1"}
        after = {"a.cs": "M :1", "ProjectSettings/ProjectSettings.asset": " M:2",
                 "Assets/InitTestScene123.unity": "??:3"}
        changed = diff_snapshots(before, after)
        expect("only new changes are reported",
               changed == ["Assets/InitTestScene123.unity",
                           "ProjectSettings/ProjectSettings.asset"])
        expect("InitTestScene is flagged", residue_warning("Assets/InitTestScene123.unity") is not None)
        expect("ProjectSettings.asset is flagged",
               residue_warning("ProjectSettings/ProjectSettings.asset") is not None)
        expect("ordinary files are not flagged", residue_warning("Assets/Foo.cs") is None)

    if failures:
        for failure in failures:
            print(f"SELF-TEST FAILED: {failure}", file=sys.stderr)
        return 1
    print("Unity test runner self-test passed "
          "(missing, empty, stale, malformed and red result files all fail; green passes).")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("lane", nargs="?", choices=["edit", "play", "all"],
                        help="which lane to run")
    parser.add_argument("--self-test", action="store_true",
                        help="check the result evaluation against faked result files")
    parser.add_argument("--project", default=str(REPO_ROOT),
                        help="Unity project path (default: this repository)")
    parser.add_argument("--unity", help="path to the Unity binary (default: Hub location "
                                        "for the version in ProjectVersion.txt, or UNITY_PATH)")
    parser.add_argument("--results-dir",
                        help="where result XML and logs go (default: output/unity-tests)")
    parser.add_argument("--timeout", type=int, default=1800,
                        help="seconds per lane before the run is killed (default 1800)")
    parser.add_argument("--test-filter", help="forwarded as -testFilter (e.g. a fixture name)")
    args = parser.parse_args()

    if args.self_test:
        return self_test()
    if not args.lane:
        parser.error("a lane (edit, play, all) is required unless --self-test is used")
    return main_run(args)


if __name__ == "__main__":
    sys.exit(main())
