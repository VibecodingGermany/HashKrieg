#!/usr/bin/env python3
"""Reject a PR that changes simulation behavior and a golden baseline together."""

from __future__ import annotations

import argparse
from pathlib import Path
import sys


BASELINE_FILES = frozenset(
    {
        "tools/Nova.SimRunner.Tests/SnapshotGoldenBytesTests.cs",
        "tools/Nova.SimRunner.Tests/CommandGoldenBytesTests.cs",
        "tools/Nova.SimRunner.Tests/SimRandomGoldenTests.cs",
        "tools/Nova.SimRunner.Tests/Determinism10000Tests.cs",
    }
)

# These are the deterministic behavior surfaces. Tests are deliberately absent:
# modifying a test is not, by itself, a simulation behavior change.
SIMULATION_PATH_PREFIXES = (
    "Assets/_Project/Scripts/AI/",
    "Assets/_Project/Scripts/AI.Data/",
    "Assets/_Project/Scripts/Core/",
    "Assets/_Project/Scripts/Data/",
    "Assets/_Project/Scripts/Gameplay/Match/",
    "Assets/_Project/Scripts/Networking/",
    "Assets/_Project/Scripts/Simulation/",
    "Assets/_Project/Data/",
)

OVERRIDE_LABEL = "baseline-reset-approved"


def _normalise(path: str) -> str:
    return path.strip().replace("\\", "/").removeprefix("./")


def evaluate(changed_paths: list[str], labels: set[str]) -> tuple[bool, str]:
    """Return whether the PR obeys the baseline separation rule."""
    paths = sorted({_normalise(path) for path in changed_paths if path.strip()})
    baselines = [path for path in paths if path in BASELINE_FILES]
    simulation = [
        path
        for path in paths
        if any(path.startswith(prefix) for prefix in SIMULATION_PATH_PREFIXES)
    ]

    if not baselines or not simulation:
        return True, "Baseline guard passed: the PR changes only one side of the separation."

    if OVERRIDE_LABEL in labels:
        return (
            True,
            "Baseline guard override accepted via "
            f"'{OVERRIDE_LABEL}' (maintainer approval required).",
        )

    return (
        False,
        "A PR may not change deterministic simulation behavior and a golden "
        "baseline together. Split the baseline reset into a follow-up PR, or have "
        f"a maintainer apply '{OVERRIDE_LABEL}' for a documented exception.\n"
        f"Simulation paths: {', '.join(simulation)}\n"
        f"Baseline paths: {', '.join(baselines)}",
    )


def _read_lines(path: Path) -> list[str]:
    return path.read_text(encoding="utf-8").splitlines()


def _self_test() -> None:
    cases = (
        (["Assets/_Project/Scripts/Simulation/Combat/CombatSystem.cs"], set(), True),
        (["tools/Nova.SimRunner.Tests/SimRandomGoldenTests.cs"], set(), True),
        (
            [
                "Assets/_Project/Scripts/Simulation/Combat/CombatSystem.cs",
                "tools/Nova.SimRunner.Tests/SimRandomGoldenTests.cs",
            ],
            set(),
            False,
        ),
        (
            [
                "Assets/_Project/Scripts/Simulation/Combat/CombatSystem.cs",
                "tools/Nova.SimRunner.Tests/SimRandomGoldenTests.cs",
            ],
            {OVERRIDE_LABEL},
            True,
        ),
        (
            [
                "Assets/_Project/Scripts/Networking/LockstepBarrier.cs",
                "tools/Nova.SimRunner.Tests/SimRandomGoldenTests.cs",
            ],
            set(),
            False,
        ),
        (["docs/production/DecisionLog.md"], set(), True),
    )
    for changed_paths, labels, expected in cases:
        actual, message = evaluate(changed_paths, labels)
        assert actual is expected, message


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--changed-files", type=Path)
    parser.add_argument("--labels", type=Path)
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()

    if args.self_test:
        _self_test()
        print("Baseline guard self-test passed, including the required negative control.")
        return 0

    if args.changed_files is None or args.labels is None:
        parser.error("--changed-files and --labels are required unless --self-test is used")

    allowed, message = evaluate(_read_lines(args.changed_files), set(_read_lines(args.labels)))
    print(message)
    return 0 if allowed else 1


if __name__ == "__main__":
    sys.exit(main())
