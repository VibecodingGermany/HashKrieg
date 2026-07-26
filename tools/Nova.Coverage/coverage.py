#!/usr/bin/env python3
"""G1 line-coverage measurement for the Nova.SimRunner test lane (diagnostic only).

Runs `dotnet test` with the XPlat Code Coverage collector (coverlet.collector)
against tools/Nova.SimRunner.Tests — or consumes an existing Cobertura report —
and aggregates line coverage per G1 scope (docs/tech/Testing.md section 4).

The measured Core/Simulation sources are compile-linked into the test
assembly, so the collector instruments that single assembly; the Cobertura
`filename` attributes still carry the original Assets paths, which the scope
matchers below use.

Outputs (never gate evidence; `output/` is gitignored):
  <out>/coverage.cobertura.xml   raw report artifact (hashed into the summary)
  <out>/coverage-summary.json    strict JSON summary incl. sha256 of the report

Exit code is non-zero when any scope misses its required threshold.
"""

import argparse
import hashlib
import json
import os
import subprocess
import sys
import xml.etree.ElementTree as ET
from datetime import datetime, timezone

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SCRIPTS = "Assets/_Project/Scripts"

# G1 thresholds from docs/tech/Testing.md section 4. Path matchers are
# repo-relative POSIX prefixes or exact files below Assets/_Project/Scripts.
DEFAULT_SCOPES = [
    {
        "name": "Nova.Simulation",
        "requiredPercent": 80.0,
        "prefixes": [f"{SCRIPTS}/Simulation/"],
        "files": [],
    },
    {
        "name": "Command",
        "requiredPercent": 90.0,
        "prefixes": [f"{SCRIPTS}/Simulation/CommandsV1/"],
        "files": [],
    },
    {
        "name": "PRNG",
        "requiredPercent": 90.0,
        "prefixes": [],
        "files": [f"{SCRIPTS}/Core/SimRandom.cs"],
    },
    {
        "name": "Serializer",
        "requiredPercent": 90.0,
        "prefixes": [f"{SCRIPTS}/Simulation/Snapshots/"],
        "files": [],
    },
    {
        "name": "Hash",
        "requiredPercent": 90.0,
        "prefixes": [],
        "files": [
            f"{SCRIPTS}/Core/XxHash64.cs",
            f"{SCRIPTS}/Core/SimHashWriter.cs",
        ],
    },
    {
        "name": "Replay",
        "requiredPercent": 90.0,
        "prefixes": [f"{SCRIPTS}/Simulation/Replays/"],
        "files": [],
    },
    {
        # Payload reader/writer paths of the 13 activated stream command kinds
        # (CommandKind.Move..InstallDefenseModule): the per-kind WriteTo/TryParse
        # structs plus the canonical LE reader/writer they exercise.
        "name": "CommandInventory",
        "requiredPercent": 100.0,
        "prefixes": [],
        "files": [
            f"{SCRIPTS}/Simulation/CommandsV1/CommandPayloads.cs",
            f"{SCRIPTS}/Simulation/CommandsV1/CommandPayloadReader.cs",
            f"{SCRIPTS}/Simulation/CommandsV1/CommandPayloadWriter.cs",
        ],
    },
]


def repo_relative(path):
    """Normalize a Cobertura filename to a repo-relative POSIX path, or None."""
    p = path.replace("\\", "/")
    root = REPO_ROOT.replace("\\", "/")
    if p.startswith(root + "/"):
        return p[len(root) + 1:]
    # coverlet.msbuild writes paths relative to the filesystem root without the
    # leading slash (<source>/</source>), so also match the root minus "/".
    if p.startswith(root[1:] + "/"):
        return p[len(root):]
    # Fall back to suffix matching (e.g. different checkout layout).
    marker = "/Assets/_Project/Scripts/"
    idx = p.find(marker)
    if idx >= 0:
        return p[idx + 1:]
    return None


def scope_matches(scope, rel_path):
    if rel_path is None:
        return False
    if any(rel_path.startswith(pre) for pre in scope["prefixes"]):
        return True
    return rel_path in scope["files"]


def parse_cobertura(report_path):
    """Return {repo_rel_file: {line_no: hits}} aggregated over all classes."""
    files = {}
    tree = ET.parse(report_path)
    for cls in tree.getroot().iter("class"):
        rel = repo_relative(cls.get("filename", ""))
        if rel is None:
            continue
        lines = files.setdefault(rel, {})
        for line in cls.iter("line"):
            no = int(line.get("number"))
            hits = int(line.get("hits"))
            lines[no] = max(lines.get(no, 0), hits)
    return files


def run_tests(dotnet, results_dir, extra_args):
    os.makedirs(results_dir, exist_ok=True)
    # coverlet.msbuild mode: the code under test is compile-linked into the
    # test assembly, which coverlet excludes by default — IncludeTestAssembly
    # is required. The in-proc XPlat collector produced empty reports here.
    cmd = [
        dotnet, "test", "tools/Nova.SimRunner.Tests",
        "--nologo", "-v", "q",
        "/p:CollectCoverage=true",
        "/p:CoverletOutputFormat=cobertura",
        "/p:IncludeTestAssembly=true",
        f"/p:CoverletOutput={os.path.abspath(results_dir)}/coverage",
    ] + extra_args
    print("+ " + " ".join(cmd), flush=True)
    proc = subprocess.run(cmd, cwd=REPO_ROOT)
    if proc.returncode != 0:
        print(f"error: dotnet test failed with exit code {proc.returncode}", file=sys.stderr)
        sys.exit(2)
    report = os.path.join(results_dir, "coverage.cobertura.xml")
    if not os.path.isfile(report):
        print(f"error: no cobertura report found at {report}", file=sys.stderr)
        sys.exit(2)
    return report


def sha256_of(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--report", help="existing Cobertura XML to consume instead of running tests")
    ap.add_argument("--out", default=os.path.join(REPO_ROOT, "output", "coverage"),
                    help="output directory (default: output/coverage)")
    ap.add_argument("--results-dir", help="results directory for the test run (default: <out>/TestResults)")
    ap.add_argument("--dotnet", default=os.path.join(REPO_ROOT, ".dotnet", "dotnet"),
                    help="dotnet executable (default: repo-local SDK)")
    ap.add_argument("--set", action="append", default=[], metavar="SCOPE=PCT",
                    help="override a scope threshold, e.g. --set Command=95 (repeatable)")
    ap.add_argument("--test-arg", action="append", default=[],
                    help="extra argument passed to dotnet test (repeatable)")
    ap.add_argument("--no-fail", action="store_true", help="always exit 0 (pure diagnosis)")
    args = ap.parse_args()

    os.makedirs(args.out, exist_ok=True)

    if args.report:
        report = os.path.abspath(args.report)
        if not os.path.isfile(report):
            print(f"error: report not found: {report}", file=sys.stderr)
            sys.exit(2)
    else:
        results_dir = args.results_dir or os.path.join(args.out, "TestResults")
        report = run_tests(args.dotnet, results_dir, args.test_arg)

    scopes = [dict(s) for s in DEFAULT_SCOPES]
    for override in args.set:
        name, _, pct = override.partition("=")
        match = [s for s in scopes if s["name"].lower() == name.lower()]
        if not match or not pct:
            print(f"error: invalid --set override: {override!r}", file=sys.stderr)
            sys.exit(2)
        match[0]["requiredPercent"] = float(pct)

    files = parse_cobertura(report)

    # Copy the consumed report next to the summary so the hashed artifact is stable.
    stable_report = os.path.join(args.out, "coverage.cobertura.xml")
    if os.path.abspath(report) != os.path.abspath(stable_report):
        with open(report, "rb") as src, open(stable_report, "wb") as dst:
            dst.write(src.read())
        report = stable_report

    summary_scopes = []
    all_passed = True
    rows = []
    for scope in scopes:
        covered = coverable = 0
        uncovered = {}
        for rel, lines in sorted(files.items()):
            if not scope_matches(scope, rel):
                continue
            bad = [no for no, hits in sorted(lines.items()) if hits == 0]
            covered += sum(1 for hits in lines.values() if hits > 0)
            coverable += len(lines)
            if bad:
                uncovered[rel] = bad
        pct = round(100.0 * covered / coverable, 2) if coverable else 0.0
        passed = pct >= scope["requiredPercent"]
        all_passed = all_passed and passed
        rows.append((scope["name"], pct, scope["requiredPercent"], covered, coverable, passed))
        summary_scopes.append({
            "name": scope["name"],
            "linePercent": pct,
            "requiredPercent": scope["requiredPercent"],
            "coveredLines": covered,
            "coverableLines": coverable,
            "passed": passed,
            "uncoveredLines": [{"file": f, "lines": ls} for f, ls in uncovered.items()],
        })

    report_bytes = os.path.getsize(report)
    summary = {
        "tool": "tools/Nova.Coverage/coverage.py",
        "purpose": "G1 coverage diagnosis (docs/tech/Testing.md section 4); not gate evidence",
        "generatedUtc": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "status": "measured",
        "reportArtifact": {
            "path": os.path.relpath(report, REPO_ROOT).replace(os.sep, "/"),
            "sha256": sha256_of(report),
            "bytes": report_bytes,
        },
        "scopes": summary_scopes,
    }
    summary_path = os.path.join(args.out, "coverage-summary.json")
    with open(summary_path, "w", encoding="utf-8") as f:
        json.dump(summary, f, indent=2)
        f.write("\n")

    width = max(len(r[0]) for r in rows)
    print("\nG1 line coverage (diagnostic, not gate evidence)")
    print(f"{'scope'.ljust(width)}  {'line %':>7}  {'required':>8}  {'covered':>8}  {'coverable':>9}  result")
    for name, pct, req, cov, total, passed in rows:
        print(f"{name.ljust(width)}  {pct:6.2f}%  {req:7.2f}%  {cov:8d}  {total:9d}  {'PASS' if passed else 'FAIL'}")
    print(f"\nreport : {os.path.relpath(report, REPO_ROOT)} (sha256 {summary['reportArtifact']['sha256'][:16]}…, {report_bytes} bytes)")
    print(f"summary: {os.path.relpath(summary_path, REPO_ROOT)}")

    if not all_passed and not args.no_fail:
        print("\nerror: at least one scope misses its required threshold", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
