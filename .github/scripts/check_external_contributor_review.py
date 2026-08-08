#!/usr/bin/env python3
"""Require a current maintainer approval and CLA acceptance for external PRs."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys
from typing import Any


MAINTAINERS = frozenset({"cubetribe", "travelhawk"})
CLA_MARKER = "- [x] I agree to the Contributor License Agreement"
DECISIVE_REVIEW_STATES = frozenset({"APPROVED", "CHANGES_REQUESTED", "DISMISSED"})


def _review_key(review: dict[str, Any]) -> tuple[str, int]:
    return (str(review.get("submitted_at") or ""), int(review.get("id") or 0))


def evaluate(
    *, author: str, head_sha: str, body: str, reviews: list[dict[str, Any]]
) -> tuple[bool, str]:
    author = author.lower()
    if author in MAINTAINERS:
        return True, "Maintainer-authored PR: external-contributor review is not required."

    if CLA_MARKER.lower() not in body.lower():
        return (
            False,
            "External contributions require the checked Contributor License Agreement "
            "checkbox in the pull-request template.",
        )

    latest_by_maintainer: dict[str, dict[str, Any]] = {}
    for review in reviews:
        reviewer = str((review.get("user") or {}).get("login") or "").lower()
        state = str(review.get("state") or "").upper()
        if reviewer not in MAINTAINERS or reviewer == author or state not in DECISIVE_REVIEW_STATES:
            continue
        previous = latest_by_maintainer.get(reviewer)
        if previous is None or _review_key(review) > _review_key(previous):
            latest_by_maintainer[reviewer] = review

    approved = [
        reviewer
        for reviewer, review in latest_by_maintainer.items()
        if str(review.get("state") or "").upper() == "APPROVED"
        and str(review.get("commit_id") or "") == head_sha
    ]
    if approved:
        return (
            True,
            "External contribution approved for the current head commit by "
            + ", ".join(sorted(approved))
            + ".",
        )

    return (
        False,
        "External contributions require an APPROVED review by cubetribe or "
        "travelhawk on the current head commit.",
    )


def _self_test() -> None:
    head = "a" * 40
    approved = {
        "id": 1,
        "submitted_at": "2026-08-08T10:00:00Z",
        "state": "APPROVED",
        "commit_id": head,
        "user": {"login": "cubetribe"},
    }
    stale = {**approved, "id": 2, "commit_id": "b" * 40}
    assert evaluate(author="cubetribe", head_sha=head, body="", reviews=[])[0]
    assert not evaluate(author="outside", head_sha=head, body="", reviews=[])[0]
    assert not evaluate(author="outside", head_sha=head, body=CLA_MARKER, reviews=[])[0]
    assert evaluate(author="outside", head_sha=head, body=CLA_MARKER, reviews=[approved])[0]
    assert not evaluate(author="outside", head_sha=head, body=CLA_MARKER, reviews=[stale])[0]
    changed = {**approved, "id": 3, "submitted_at": "2026-08-08T11:00:00Z", "state": "CHANGES_REQUESTED"}
    assert not evaluate(author="outside", head_sha=head, body=CLA_MARKER, reviews=[approved, changed])[0]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--author")
    parser.add_argument("--head-sha")
    parser.add_argument("--body", type=Path)
    parser.add_argument("--reviews", type=Path)
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()

    if args.self_test:
        _self_test()
        print("External-contributor review self-test passed.")
        return 0

    if not all((args.author, args.head_sha, args.body, args.reviews)):
        parser.error("--author, --head-sha, --body, and --reviews are required unless --self-test is used")

    reviews = json.loads(args.reviews.read_text(encoding="utf-8"))
    if not isinstance(reviews, list):
        raise ValueError("reviews payload must be a JSON array")
    allowed, message = evaluate(
        author=args.author,
        head_sha=args.head_sha,
        body=args.body.read_text(encoding="utf-8"),
        reviews=reviews,
    )
    print(message)
    return 0 if allowed else 1


if __name__ == "__main__":
    sys.exit(main())
