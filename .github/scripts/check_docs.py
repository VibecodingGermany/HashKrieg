#!/usr/bin/env python3
"""Docs-Konsistenz-Check für Project Nova.

Die Dateiliste kommt aus Git statt aus einem Workspace-Walk. Dadurch werden
getrackte und unignorierte neue Markdown-Dateien geprüft, während Library-,
Build- und andere generierte/ignorierte Verzeichnisse außen bleiben.

- HART (exit 1): tote interne Links, ungültiges UTF-8, fehlende Wiki-Kopfzeile,
  nicht-striktes Quality-JSON oder rote Evidence-Semantik-Negativkontrollen.

Aufruf: python3 .github/scripts/check_docs.py
"""

from __future__ import annotations

import json
import os
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LINK_RE = re.compile(r"\[[^\]]*\]\(([^)]+)\)")
HEADER_RE = re.compile(r"\*\*Version:\*\*.*\|\s*\*\*Status:\*\*")
VERSION_RE = re.compile(r"\*\*Version:\*\*\s*([0-9]+\.[0-9]+\.[0-9]+)")
H2_RE = re.compile(r"^##\s+(.+?)\s*$", re.MULTILINE)
REQUIRED_SECTIONS = (
    ("Zweck", re.compile(r"^##(?:\s+\d+\.)?\s+Zweck\s*$", re.MULTILINE)),
    (
        "Abhängigkeiten",
        re.compile(r"^##(?:\s+\d+\.)?\s+Abhängigkeiten\s*$", re.MULTILINE),
    ),
    (
        "Offene Punkte",
        re.compile(r"^##(?:\s+\d+\.)?\s+Offene Punkte\s*$", re.MULTILINE),
    ),
    (
        "Nächste Schritte",
        re.compile(r"^##(?:\s+\d+\.)?\s+Nächste Schritte\s*$", re.MULTILINE),
    ),
    (
        "Änderungsverlauf",
        re.compile(r"^##(?:\s+\d+\.)?\s+Änderungsverlauf\s*$", re.MULTILINE),
    ),
)
QUALITY_JSON = (
    ROOT / "quality/content/mvp-v1.json",
    ROOT / "quality/scenarios/mvp-v1.json",
    ROOT / "quality/schemas/GateEvidence.schema.json",
    ROOT / "quality/package.json",
    ROOT / "quality/package-lock.json",
)
EVIDENCE_VALIDATOR = ROOT / "quality/scripts/validate_gate_evidence.py"


def reject_duplicate_pairs(pairs: list[tuple[str, object]]) -> dict[str, object]:
    result: dict[str, object] = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"doppelter JSON-Schlüssel: {key}")
        result[key] = value
    return result


def reject_constant(value: str) -> None:
    raise ValueError(f"nicht-endliche JSON-Zahl: {value}")


def is_external(target: str) -> bool:
    return target.startswith(("http://", "https://", "mailto:", "tel:", "#"))


def markdown_files() -> list[Path]:
    command = [
        "git",
        "ls-files",
        "--cached",
        "--others",
        "--exclude-standard",
        "-z",
        "--",
        "*.md",
    ]
    try:
        result = subprocess.run(
            command,
            cwd=ROOT,
            check=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )
    except subprocess.CalledProcessError as error:
        stderr = error.stderr.decode("utf-8", errors="replace").strip()
        print(f"::error:: Markdown-Dateiliste konnte nicht ermittelt werden: {stderr}")
        raise

    relative_paths = [
        Path(raw.decode("utf-8"))
        for raw in result.stdout.split(b"\0")
        if raw
    ]
    return [ROOT / relative for relative in sorted(relative_paths)]


def main() -> int:
    dead: list[str] = []
    decode_errors: list[str] = []
    missing_header: list[str] = []
    structure_errors: list[str] = []
    quality_errors: list[str] = []

    try:
        paths = markdown_files()
    except subprocess.CalledProcessError:
        return 1

    for path in paths:
        rel = path.relative_to(ROOT).as_posix()
        try:
            text = path.read_text(encoding="utf-8")
        except UnicodeDecodeError as error:
            message = (
                f"{rel}: UTF-8-Dekodierung bei Byte {error.start} fehlgeschlagen "
                f"({error.reason})"
            )
            decode_errors.append(message)
            print(f"::error file={rel}::{message}")
            continue

        lines = text.splitlines()
        for line_number, line in enumerate(lines, 1):
            for match in LINK_RE.finditer(line):
                target = match.group(1).strip().split(" ")[0].strip()
                if not target or is_external(target):
                    continue
                target_path = target.split("#", 1)[0]
                if not target_path:
                    continue
                resolved = (path.parent / target_path).resolve()
                if not resolved.exists():
                    dead.append(f"{rel}:{line_number}  ->  {target}")

        if rel.startswith("docs/") and not HEADER_RE.search(text):
            missing_header.append(rel)
        if rel.startswith("docs/"):
            for name, pattern in REQUIRED_SECTIONS:
                if not pattern.search(text):
                    structure_errors.append(f"{rel}: Pflichtabschnitt {name} fehlt")
            headings = H2_RE.findall(text)
            if not headings or re.fullmatch(
                r"(?:\d+\.\s+)?Änderungsverlauf", headings[-1]
            ) is None:
                structure_errors.append(
                    f"{rel}: Änderungsverlauf muss der letzte H2-Abschnitt sein"
                )
            version_match = VERSION_RE.search(text)
            if version_match is not None:
                version = re.escape(version_match.group(1))
                if re.search(rf"^\|\s*{version}\s*\|", text, re.MULTILINE) is None:
                    structure_errors.append(
                        f"{rel}: aktueller Versionsstand fehlt im Änderungsverlauf"
                    )

    if missing_header:
        print("::error:: docs/-Dateien ohne Standard-Kopfzeile:")
        for rel in sorted(missing_header):
            print(f"  - {rel}")

    if structure_errors:
        print(f"\n::error:: {len(structure_errors)} Dokumentstrukturfehler:")
        for message in structure_errors:
            print(f"  {message}")

    for path in QUALITY_JSON:
        rel = path.relative_to(ROOT).as_posix()
        try:
            json.loads(
                path.read_text(encoding="utf-8"),
                object_pairs_hook=reject_duplicate_pairs,
                parse_constant=reject_constant,
            )
        except (OSError, UnicodeDecodeError, json.JSONDecodeError, ValueError) as error:
            quality_errors.append(f"{rel}: {error}")

    if not quality_errors:
        environment = os.environ.copy()
        environment["PYTHONDONTWRITEBYTECODE"] = "1"
        result = subprocess.run(
            [sys.executable, str(EVIDENCE_VALIDATOR), "--self-test"],
            cwd=ROOT,
            check=False,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
            env=environment,
        )
        if result.returncode != 0:
            quality_errors.append(
                "Evidence-Semantik-Negativkontrolle fehlgeschlagen:\n"
                + result.stdout.strip()
            )

    if decode_errors:
        print(f"\n::error:: {len(decode_errors)} Markdown-Datei(en) sind nicht UTF-8:")
        for message in decode_errors:
            print(f"  {message}")

    if dead:
        print(f"\n::error:: {len(dead)} tote interne Link(s) gefunden:")
        for entry in dead:
            print(f"  {entry}")

    if quality_errors:
        print(f"\n::error:: {len(quality_errors)} Quality-Vertragsfehler:")
        for message in quality_errors:
            print(f"  {message}")

    if decode_errors or dead or missing_header or structure_errors or quality_errors:
        return 1

    print(
        f"OK: {len(paths)} Markdown-Dateien, {len(QUALITY_JSON)} Quality-JSONs "
        "und Evidence-Negativkontrollen geprüft."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
