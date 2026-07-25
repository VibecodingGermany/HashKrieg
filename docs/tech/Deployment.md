# Deployment und CI

**Version:** 1.4.0 | **Status:** verbindlich für MS-1 – G0-A aktiv, Autorisierung gesperrt | **Verantwortungsbereich:** Lead DevOps Engineer | **Sprint:** 7

## Zweck

Definiert Toolchain-Pins, Branch-/PR-Fluss, G0-Buildmatrix und Evidence-
Übergabe. Dieses Dokument beschreibt den Zielzustand; aktuell existiert nur
der Docs-Workflow. Schema 1.2 prüft nur Integrität; Code-CI und Pass-
Autorisierung werden zuerst in G0-A und danach G0-B implementiert.

## Abhängigkeiten

- [../../AGENTS.md](../../AGENTS.md) und
  [../../CONTRIBUTING.md](../../CONTRIBUTING.md)
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-059 bis
  D-064
- [Testing.md](Testing.md) und [Architecture.md](Architecture.md)
- [`../../quality/schemas/GateEvidence.schema.json`](../../quality/schemas/GateEvidence.schema.json)
- [`../../quality/scripts/validate_gate_evidence.py`](../../quality/scripts/validate_gate_evidence.py)
- [`../../quality/package-lock.json`](../../quality/package-lock.json) –
  gepinntes Ajv/ajv-formats

## 1. Toolchain-Pins

| Tool | Vertrag |
|---|---|
| Unity | `6000.5.4f1` |
| Unity Revision | `d550df8bd089` |
| Render Pipeline | URP, exakter Paketstand aus Lockfile |
| .NET SDK | exakte Version in `global.json` als G0-Exit |
| Unity Packages | Manifest + Lockfile, keine schwebenden Versionen |

Automatische Editor-Upgrades sind verboten. Runner-Images und lokale
Anleitungen verwenden dieselben Pins. Eine Änderung benötigt eine neue D-ID
nach G5 oder einen belegten Engine-Blocker.

## 2. Branch- und Merge-Modell

`main` ist PR-only. Es gibt keinen dauerhaften Integrationsbranch.

Zulässige kurze Präfixe: `feat/`, `fix/`, `docs/`, `chore/`, `refactor/`,
`codex/`. Merge erfolgt als Squash bei linearer Historie. Direkte oder
Force-Pushes auf `main` und History-Rewrites geteilter Branches sind verboten.

Agenten committen oder pushen nur nach ausdrücklicher Anfrage für die
jeweilige Aktion.

## 3. Pflichtchecks

| Check | Status | Zweck |
|---|---|---|
| `docs-check` | vorhanden | Dokumentstruktur, interne Links, UTF-8, fünf strikte Quality-JSONs, gepinntes Ajv und Evidence-Negativkontrollen; läuft auch bei `quality/**` |
| `quality-gate` | in G0-A/G0-B zu implementieren | subject-unabhängige Autorisierung und aggregierte Tests/Coverage/Architektur/Golden/Matches |

Nach G0 sind beide Required Checks. Docs-only wird im `quality-gate` explizit
klassifiziert und niemals als Workflow-Skip behandelt.

## 4. G0-A und G0-B-Buildmatrix

G0-A etabliert zuerst den subject-unabhängigen Trusted-Tool-Checkout und
Schema 1.3 als nicht selbstautorisierende Bootstrap-Änderung. Sie wird ohne
Gate-Fortschritt gemergt. Erst ein nachfolgender sauberer Subject-Commit darf
die folgende G0-B-Matrix ausführen und G0 belegen.

| Umgebung | Pflicht |
|---|---|
| Windows x64 | sauberer Unity-Standalone-Build und .NET-SimRunner |
| macOS arm64 | sauberer Unity-Standalone-Build und .NET-SimRunner |
| plattformneutral | .NET-Tests und asmdef-/Architekturcheck |
| Unity | EditMode-Tests |

Jede Matrix startet aus sauberem Checkout, verwendet Cache nur mit
Toolchain-/Lockfile-Key und publiziert gehashte Logs/Artefakte. Ein
Negative-Control-Job muss eine absichtlich verbotene Assemblykante erkennen.

## 5. Source- und Binary-Hygiene

- Core/Simulation/AI-Quellen werden zwischen Unity und SimRunner geteilt,
  nicht kopiert.
- Keine generierten DLLs, Build-, Library-, Temp-, TestResult- oder
  Profilerausgaben tracken.
- CI prüft bekannte Binary-/Generatorpfade und schlägt bei getracktem Output
  fehl.
- Lokale Dirty-Builds können diagnostisch laufen, erzeugen aber keine
  Gate-Evidence.

## 6. PR-Qualitätsfluss

Nach G0 enthält jeder PR:

1. Scopeklassifikation,
2. `docs-check`,
3. aggregiertes `quality-gate`,
4. unabhängiges read-only Review,
5. Changelog/Versionspflege.

Sobald mindestens zwei aktive menschliche Maintainer existieren, ist eine
zweite menschliche Freigabe Pflicht. CODEOWNERS darf strengere Zuständigkeit
verlangen.

## 7. Evidence-Publikation

Gateversuche schreiben append-only nach
`quality/evidence/G<N>/<subjectSha>/<attempt>/GateEvidence.json`. Der
Implementation-Job darf keine bestehende Attempt-Datei überschreiben.
Evidence enthält Commit/Tree, `dirty=false`, Toolchains, Umgebungen,
kanonische Checks/Coverage, Content-/Scenario-/Schema-/Validator-SHA-256,
Rohartefakte, CI, Reviewer, Kriterienmap und Urteil. Gepinntes Ajv validiert
Schema 1.2 für das aktuelle Dokument und jeden Vorgänger; danach prüft
`quality/scripts/validate_gate_evidence.py` Pfad, Digests, Checkprofile,
Schwellen, Reviewer-Trennung und Gate-Profil. Diese Prüfung belegt nur
Integrität. Jeder Pass-Versuch endet zusätzlich mit
`E_AUTHORIZATION_BOOTSTRAP`; auch ein äußerlich passender Trust-Kontext
autorisiert unter Schema 1.2 kein Gate.

Content-/Scenario-Digests stammen aus den Git-Blobs des Subject-Commits.
Ab G1 enthält Evidence genau eine SHA-256-gebundene Referenz auf das
unmittelbare Vorgängergate; dieses muss rekursiv am selben Commit/Tree
bestehen. Szenarioschwellen verwenden exakte Units, nichtnegative Samples und
drei getrennte 120-s-Läufe nach 30-s-Warmup; sie werden je Lauf und kombiniert
aus artefaktgebundenen Rohdaten berechnet.

Der Schema-1.3-Zielvertrag für G0-A verlangt:

- Der geschützte Authorize-Job führt Manifest, Szenariovertrag, Schema,
  Python-Validator, Ajv-Wrapper, `package.json`, Lockdatei, Gate-Runner und
  Authorize-Workflow ausschließlich aus einem separaten
  Trusted-Tool-Checkout aus. Subject-/Trusted-Commit, SHA-256 und exakte
  Node-Version sind gebunden.
- Eine Änderung an diesem Trust-Bundle wird ohne Gate-Fortschritt geschützt
  gemergt und gilt erst für einen nachfolgenden sauberen Subject-Commit.
- Der externe Trust-Kontext enthält die vollständige geordnete
  `authorizedEvidence`-Kette von G0 bis zum aktuellen Gate. Jeder Eintrag
  bindet Gate, Pfad, Evidence-Hash, Subject-Commit/-Tree, CI-Run/-Job sowie
  CI- und Review-Attestierung; der Job verifiziert jeden Eintrag gegen
  GitHub.
- Command und Performance-Messung referenzieren dieselbe `environmentId`.
  Windows-x64-Referenz und Mac-M2-Funktionslauf verwenden getrennte
  Methodenprofile für OS, Architektur, Hardware, Build, Managed/Burst,
  Auflösung, Quality-Profil, VSync, Deep Profiling und Replay.

Fehlende, zusätzliche, vertauschte oder nur lokal erzeugte Autorisierungs-
einträge sind Fail. Fehlender Node-/Ajv-Stack, ein hängender Schema-
Subprozess, Skip, Cancel oder Missing enden kontrolliert fail-closed.
Relevante Änderungen machen Evidence stale.

## 8. Release-Grenze

Wiki 0.11.0 ist kein Game-Release. Tags, GitHub Releases, Deployment und
Store-Publikation benötigen separate ausdrückliche Autorität. G0, MS-0 und
MS-1 bleiben offen; dieser Rebaseline erzeugt keinen Tag.

## Offene Punkte

- Schema 1.3, Trusted-Tool-Checkout und der reale geschützte Authorize-Job
  einschließlich `run_gate_check.py` sind G0-A-Deliverables; .NET-/
  Paketpins und die Buildmatrix folgen in G0-B. Nichts davon wird durch
  dieses Dokument als vorhanden oder bestanden behauptet.

## Nächste Schritte

1. G0-A-Trust-Bundle einschließlich Schema 1.3 und Gate-Runner
   subject-unabhängig implementieren und ohne Gate-Fortschritt geschützt
   mergen.
2. Am nachfolgenden sauberen Subject G0-B-Pins, Buildmatrix, Negative
   Controls und Binary-Hygiene umsetzen und dieses danach mit vollständiger
   Autorisierungskette und Umgebungsbindung als G0 autorisieren.
3. Required-Check-Konfiguration erst nach erfolgreicher realer CI prüfen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead DevOps Engineer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings) | Lead DevOps Engineer |
| 1.0.0 | 2026-07-24 | Deployment auf D-059-Branching, D-060-Engine-Pin und G0-/Evidence-Vertrag D-061 rebaselined | Lead DevOps Engineer |
| 1.1.0 | 2026-07-24 | Evidence-Semantikvalidator, SHA-256-Dateibindung und Wiki-Stand 0.8.1 ergänzt | Lead DevOps Engineer |
| 1.1.1 | 2026-07-24 | Tatsächlichen Umfang von `docs-check` und Semantikvalidator-Abhängigkeit präzisiert | Lead DevOps Engineer |
| 1.1.2 | 2026-07-24 | Unveröffentlichten Wiki-Vertragsstand auf 0.8.2 fortgeschrieben | Lead DevOps Engineer |
| 1.2.0 | 2026-07-24 | D-062-Subject-Blob-, Szenariometrik- und rekursive Vorgängergate-Prüfung sowie Wiki-Stand 0.9.0 aufgenommen | Lead DevOps Engineer |
| 1.3.0 | 2026-07-24 | D-063-Schema 1.2, gepinntes Ajv, kanonische Check-Artefakte und extern autorisierten Trust-Kontext aufgenommen | Lead DevOps Engineer |
| 1.4.0 | 2026-07-24 | D-064-Fail-Closed-Schema 1.2, zweistufigen Trusted-Tool-Bootstrap, vollständige Autorisierungskette und Wiki-Stand 0.11.0 verankert | Lead DevOps Engineer |
