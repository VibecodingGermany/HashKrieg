#!/usr/bin/env python3
"""Generate the D-064 trust context for the protected quality-gate workflow.

Runs inside the ``gate-evidence-authorize`` job of
``.github/workflows/quality-gate.yml`` and is executed from the
subject-independent trusted tool checkout (``trusted/``), so the generator
itself is bound by ``trustedToolCommitSha``.

The output matches trust-context version ``2.0.0`` as enforced by
``quality/scripts/validate_gate_evidence.py`` (exactly 17 keys). The
``authorizedEvidence`` chain is built from the current evidence plus every
prior gate evidence (G0..gate-1) staged under the same subject commit;
any missing prior gate fails closed.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import sys
from pathlib import Path

TRUST_CONTEXT_VERSION = "2.0.0"
REPOSITORY = "VibecodingGermany/Project_Nova"
WORKFLOW_PATH = ".github/workflows/quality-gate.yml"
AUTHORIZING_JOB = "gate-evidence-authorize"
GATE_SEQUENCE = tuple(f"G{number}" for number in range(6))


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(65536), b""):
            digest.update(chunk)
    return digest.hexdigest()


def load_json(path: Path) -> dict:
    with path.open("r", encoding="utf-8") as handle:
        document = json.load(handle)
    if not isinstance(document, dict):
        raise ValueError(f"{path}: root must be an object")
    return document


def nested(document: dict, *keys: str) -> object:
    value: object = document
    for key in keys:
        if not isinstance(value, dict):
            return None
        value = value.get(key)
    return value


def chain_entry(gate_id: str, evidence_path: Path, document: dict) -> dict:
    """One authorizedEvidence entry (keys fixed by the validator)."""
    return {
        "gateId": gate_id,
        "evidencePath": nested(document, "attempt", "evidencePath"),
        "evidenceSha256": sha256_file(evidence_path),
        "subjectCommitSha": nested(document, "subject", "commitSha"),
        "subjectTreeSha": nested(document, "subject", "treeSha"),
        "ciRunId": nested(document, "ci", "runId"),
        "ciJobId": nested(document, "ci", "jobId"),
        "ciAttestationSha256": nested(
            document, "ci", "attestationArtifact", "sha256"
        ),
        "reviewArtifactSha256": nested(
            document, "reviewer", "reviewArtifact", "sha256"
        ),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--evidence",
        required=True,
        type=Path,
        help="path to the staged current GateEvidence.json (in trusted/)",
    )
    parser.add_argument("--trusted-sha", required=True)
    parser.add_argument("--output", required=True, type=Path)
    arguments = parser.parse_args()

    evidence_path = arguments.evidence.resolve()
    try:
        document = load_json(evidence_path)
    except (OSError, json.JSONDecodeError, ValueError) as error:
        print(f"cannot load evidence: {error}", file=sys.stderr)
        return 1

    gate_id = document.get("gateId")
    if gate_id not in GATE_SEQUENCE:
        print(f"unsupported gateId: {gate_id!r}", file=sys.stderr)
        return 1
    subject_sha = nested(document, "subject", "commitSha")
    if not isinstance(subject_sha, str):
        print("evidence subject.commitSha missing", file=sys.stderr)
        return 1

    run_id = os.environ.get("GITHUB_RUN_ID")
    run_attempt = os.environ.get("GITHUB_RUN_ATTEMPT")
    if not run_id or not run_attempt:
        print(
            "GITHUB_RUN_ID/GITHUB_RUN_ATTEMPT missing; the trust context can "
            "only be generated inside the protected workflow",
            file=sys.stderr,
        )
        return 1

    chain: list[dict] = []
    for prior_gate in GATE_SEQUENCE[: GATE_SEQUENCE.index(gate_id)]:
        candidates = sorted(
            evidence_path.parents[3].glob(
                f"{prior_gate}/{subject_sha}/*/GateEvidence.json"
            )
        )
        if len(candidates) != 1:
            print(
                f"authorization chain broken: expected exactly one staged "
                f"{prior_gate} evidence for {subject_sha}, found "
                f"{len(candidates)}",
                file=sys.stderr,
            )
            return 1
        try:
            prior_document = load_json(candidates[0])
        except (OSError, json.JSONDecodeError, ValueError) as error:
            print(f"cannot load prior evidence: {error}", file=sys.stderr)
            return 1
        chain.append(chain_entry(prior_gate, candidates[0], prior_document))
    chain.append(chain_entry(gate_id, evidence_path, document))

    context = {
        "schemaVersion": TRUST_CONTEXT_VERSION,
        "repository": REPOSITORY,
        "workflowPath": WORKFLOW_PATH,
        "authorizingRunId": run_id,
        "authorizingRunAttempt": run_attempt,
        "authorizingJob": AUTHORIZING_JOB,
        "subjectCommitSha": subject_sha,
        "subjectTreeSha": nested(document, "subject", "treeSha"),
        "evidencePath": nested(document, "attempt", "evidencePath"),
        "evidenceSha256": sha256_file(evidence_path),
        "evidenceCiRunId": nested(document, "ci", "runId"),
        "evidenceCiJobId": nested(document, "ci", "jobId"),
        "ciAttestationSha256": nested(
            document, "ci", "attestationArtifact", "sha256"
        ),
        "reviewerId": nested(document, "reviewer", "id"),
        "reviewArtifactSha256": nested(
            document, "reviewer", "reviewArtifact", "sha256"
        ),
        "trustedToolCommitSha": arguments.trusted_sha,
        "authorizedEvidence": chain,
    }
    arguments.output.parent.mkdir(parents=True, exist_ok=True)
    arguments.output.write_text(
        json.dumps(context, indent=2, ensure_ascii=False, allow_nan=False) + "\n",
        encoding="utf-8",
    )
    print(f"trust context written: {arguments.output}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
