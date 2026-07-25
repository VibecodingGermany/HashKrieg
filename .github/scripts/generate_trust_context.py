#!/usr/bin/env python3
"""Generate the D-064 trust context for the protected quality-gate workflow.

Runs inside the ``gate-evidence-authorize`` job of
``.github/workflows/quality-gate.yml`` and is executed from the
subject-independent trusted tool checkout (``trusted/``), so the generator
itself is bound by ``trustedToolCommitSha``. The evidence itself is read
from the subject checkout (``--subject-root`` of the validator); nothing is
staged into the trusted checkout.

The output matches trust-context version ``2.0.0`` as enforced by
``quality/scripts/validate_gate_evidence.py`` (exactly 17 keys). The
``authorizedEvidence`` chain is built from the current evidence plus every
prior gate evidence (G0..gate-1) of the same subject commit; any missing
prior gate fails closed.

Every chain entry is verified against the GitHub API (``gh api`` with
``GH_TOKEN``/``GITHUB_TOKEN``; a missing tool or token fails closed). Per
D-065 each entry must prove a real protected authorize run on the entry's
subject commit: ``ciRunId`` must exist in this repository, belong to
``.github/workflows/quality-gate.yml``, have ``event == workflow_dispatch``
and ``conclusion == success`` and ``head_sha == subjectCommitSha``;
``ciJobId`` must be the successful ``gate-evidence-authorize`` job of that
run (matching the evidence-declared job name, which the validator pins to
the same constant). Run ids must be unique across the whole chain, so one
authorize run cannot be replayed or reused for several gates.

Residual risk (documented honestly): the API verification proves "a real
protected authorize run on this subject happened"; binding the *current*
run to the exact evidence bytes happens via ``NOVA_TRUST_CONTEXT_SHA256``
in the validator, and the remaining anchor is the GitHub environment
protection of the manual ``main`` dispatch. The review attestation
(``gate-review-v1``) is hash-bound to the evidence and its artifact, but
carries no PR/review ID, so it cannot be re-verified against the GitHub
API. An evidence-hash-as-run-artifact binding (upload/download inside the
protected job) was considered and rejected in D-065 as added complexity
with its own artifact-retention attack surface.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import subprocess
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


def gh_api(endpoint: str) -> dict:
    """One authenticated GitHub API call; any failure raises (fail-closed)."""
    result = subprocess.run(
        ["gh", "api", endpoint],
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        timeout=60,
    )
    if result.returncode != 0:
        raise ValueError(f"gh api {endpoint} failed: {result.stderr.strip()}")
    try:
        document = json.loads(result.stdout)
    except json.JSONDecodeError as error:
        raise ValueError(f"gh api {endpoint} returned invalid JSON: {error}")
    if not isinstance(document, dict):
        raise ValueError(f"gh api {endpoint} did not return an object")
    return document


def verify_chain_entry(entry: dict, job_name: object) -> list[str]:
    """Verify one authorizedEvidence entry against the GitHub API (D-065)."""
    problems: list[str] = []
    gate = entry["gateId"]
    run_id = entry["ciRunId"]
    try:
        run = gh_api(f"repos/{REPOSITORY}/actions/runs/{run_id}")
    except ValueError as error:
        return [f"{gate}: {error}"]
    if run.get("path") != WORKFLOW_PATH:
        problems.append(
            f"{gate}: run {run_id} belongs to workflow {run.get('path')!r}, "
            f"not {WORKFLOW_PATH!r}"
        )
    if run.get("event") != "workflow_dispatch":
        problems.append(
            f"{gate}: run {run_id} event is {run.get('event')!r}, not "
            "'workflow_dispatch' (only protected authorize runs count)"
        )
    if run.get("conclusion") != "success":
        problems.append(
            f"{gate}: run {run_id} conclusion is {run.get('conclusion')!r}, "
            "not 'success'"
        )
    if run.get("head_sha") != entry["subjectCommitSha"]:
        problems.append(
            f"{gate}: run {run_id} head_sha {run.get('head_sha')!r} does not "
            f"match the entry subject commit {entry['subjectCommitSha']!r}"
        )
    try:
        jobs_document = gh_api(f"repos/{REPOSITORY}/actions/runs/{run_id}/jobs")
    except ValueError as error:
        return problems + [f"{gate}: {error}"]
    jobs = jobs_document.get("jobs")
    job = next(
        (
            candidate
            for candidate in jobs or []
            if str(candidate.get("id")) == str(entry["ciJobId"])
        ),
        None,
    )
    if job is None:
        problems.append(f"{gate}: job {entry['ciJobId']} not found in run {run_id}")
    else:
        if job.get("conclusion") != "success":
            problems.append(
                f"{gate}: job {entry['ciJobId']} conclusion is "
                f"{job.get('conclusion')!r}, not 'success'"
            )
        if job.get("name") != AUTHORIZING_JOB:
            problems.append(
                f"{gate}: job {entry['ciJobId']} is {job.get('name')!r}, not "
                f"the authorize job {AUTHORIZING_JOB!r}"
            )
        if isinstance(job_name, str) and job.get("name") != job_name:
            problems.append(
                f"{gate}: job name {job.get('name')!r} does not match the "
                f"evidence jobName {job_name!r}"
            )
    return problems


def verify_chain(chain: list[dict], job_names: list[object]) -> list[str]:
    """Verify the whole authorizedEvidence chain against the GitHub API.

    Besides the per-entry predicates, run ids must be unique across the
    chain so one protected authorize run cannot be replayed or reused for
    several gates (D-065).
    """
    problems: list[str] = []
    run_ids = [str(entry.get("ciRunId")) for entry in chain]
    if len(set(run_ids)) != len(run_ids):
        problems.append(
            "authorizedEvidence reuses a CI run id across chain entries; "
            "every gate requires its own protected authorize run"
        )
    for entry, job_name in zip(chain, job_names):
        problems.extend(verify_chain_entry(entry, job_name))
    return problems


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--evidence",
        required=True,
        type=Path,
        help="path to the current GateEvidence.json in the subject checkout",
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

    # GitHub verification is mandatory: without the tool or credentials the
    # generator fails closed instead of trusting self-declared chain data.
    if shutil.which("gh") is None:
        print(
            "gh CLI is required to verify the authorizedEvidence chain "
            "against the GitHub API",
            file=sys.stderr,
        )
        return 1
    if not (os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN")):
        print(
            "GH_TOKEN/GITHUB_TOKEN missing; the authorizedEvidence chain "
            "cannot be verified against the GitHub API",
            file=sys.stderr,
        )
        return 1

    chain: list[dict] = []
    documents: list[dict] = []
    for prior_gate in GATE_SEQUENCE[: GATE_SEQUENCE.index(gate_id)]:
        candidates = sorted(
            evidence_path.parents[3].glob(
                f"{prior_gate}/{subject_sha}/*/GateEvidence.json"
            )
        )
        if len(candidates) != 1:
            print(
                f"authorization chain broken: expected exactly one "
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
        documents.append(prior_document)
    chain.append(chain_entry(gate_id, evidence_path, document))
    documents.append(document)

    api_problems = verify_chain(
        chain,
        [nested(entry_document, "ci", "jobName") for entry_document in documents],
    )
    if api_problems:
        for problem in api_problems:
            print(f"github verification failed: {problem}", file=sys.stderr)
        return 1

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
