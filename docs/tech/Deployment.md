# Deployment und CI

**Version:** 1.9.2 | **Status:** verbindlich für MS-1 – G0-A1, G0-A2-Receipt-Vertrag und geschützter Authorize-Workflow implementiert, `quality-gate`-Environment, erster realer Lauf und Gate-Pass offen | **Verantwortungsbereich:** Lead DevOps Engineer | **Sprint:** 7

## Zweck

Definiert Toolchain-Pins, Branch-/PR-Fluss, G0-Buildmatrix und Evidence-
Übergabe. Aktuell existieren `docs-check`, der integrity-only PR-Job des
`quality-gate` und der geschützte `workflow_dispatch`-Authorize-Job. Schema
1.2/1.3/1.4 und der Integrity-Workflow autorisieren keinen Pass; der
zweiphasige Authorize-Pfad ist als Vertrag (Schema 1.4, Receipt-Schema,
`--authorize`) und als Workflow implementiert, doch ohne das einmalig
anzulegende `quality-gate`-Environment und einen realen Lauf bleibt die
Autorisierung fail-closed.

## Abhängigkeiten

- [../../AGENTS.md](../../AGENTS.md) und
  [../../CONTRIBUTING.md](../../CONTRIBUTING.md)
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-059 bis
  D-066
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
| `quality-gate / integrity` | vorhanden | Schema-, Semantik-, Topologie- und Runner-Integrität; autorisiert keinen Pass |
| `quality-gate / gate-evidence-authorize` | implementiert, erster Lauf offen | zweiphasiges D-066-Receipt per `workflow_dispatch` hinter geschütztem Environment |

Nach G0 sind beide Required Checks. Docs-only wird im `quality-gate` explizit
klassifiziert und niemals als Workflow-Skip behandelt.

## 4. G0-A und G0-B-Buildmatrix

G0-A1 etabliert den Schema-/Semantikvalidator, die Trusted-/Subject-
Topologie, Umgebungsbindung und den Gate-Runner als nicht autorisierende
Grundlage. G0-A2 implementiert danach den D-066-Receipt-Vertrag mit getrenntem
Subject, Evidence-Carrier und Trusted-Tool-Commit. Beide werden ohne
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
Schema 1.4 für das aktuelle Dokument und jeden Vorgänger; danach prüft
`quality/scripts/validate_gate_evidence.py` Pfad, Digests, Checkprofile,
Schwellen, Reviewer-Trennung und Gate-Profil. Lokal bleibt diese Prüfung
integrity-only: Jeder Pass-Versuch außerhalb des geschützten
`--authorize`-Laufs endet mit `E_AUTHORIZATION_BOOTSTRAP`.

Content-/Scenario-Digests stammen aus den Git-Blobs des Subject-Commits.
Ab G1 enthält Evidence genau eine SHA-256-gebundene Referenz auf das
unmittelbare Vorgängergate; dieses muss rekursiv am selben Commit/Tree
bestehen. Szenarioschwellen verwenden exakte Units, nichtnegative Samples und
drei getrennte 120-s-Läufe nach 30-s-Warmup; sie werden je Lauf und kombiniert
aus artefaktgebundenen Rohdaten berechnet.

Der G0-A1-Integritätsvertrag verlangt:

- Manifest, Szenariovertrag, Schema, Python-Validator, Ajv-Wrapper,
  `package.json`, Lockdatei, Gate-Runner und Integrity-Workflow bilden ein
  hashgebundenes Trust-Bundle. Trusted- und Subject-Checkout bleiben getrennt.
- Eine Änderung an diesem Trust-Bundle wird ohne Gate-Fortschritt geschützt
  gemergt und gilt erst für einen nachfolgenden sauberen Subject-Commit.
- Command und Performance-Messung referenzieren dieselbe `environmentId`.
  Windows-x64-Referenz und Mac-M2-Funktionslauf verwenden getrennte
  Methodenprofile für OS, Architektur, Hardware, Build, Managed/Burst,
  Auflösung, Quality-Profil, VSync, Deep Profiling und Replay.

Ein Pass bleibt außerhalb des geschützten Authorize-Laufs mit
`E_AUTHORIZATION_BOOTSTRAP` gesperrt. Der entfernte D-065-Entwurf verlangte
vom noch laufenden Job bereits seinen eigenen Erfolg und vermischte Subject-
und Evidence-Carrier-Commit.

G0-A2 implementiert deshalb `GateAuthorization.json`
(`gate-authorization-v1`, `quality/schemas/GateAuthorization.schema.json`).
Das Receipt bindet Gate, Subject-Commit/-Tree, Evidence-Carrier-Commit,
Evidence-Pfad/-Hash, Trusted-Tool-Commit, Repository, Workflow sowie
Run-/Attempt-/Job-ID des Authorize-Laufs. Der geschützte Job ruft
`validate_gate_evidence.py --authorize --trusted-tool-checkout <trusted>
--subject-root <subject> --job-id <id> --receipt-out <pfad>` auf; der
Validator bindet die Runtime-Identität aus der GitHub-Actions-Umgebung,
prüft aber nie seine eigene noch ausstehende Conclusion. Das Receipt wird
als CI-Artefakt transportiert und nach erfolgreichem Lauf unverändert per
kleinem Folge-PR append-only unter
`quality/authorizations/G<N>/<subjectSha>/<runId>-attempt<runAttempt>/GateAuthorization.json`
versioniert. Evidence-Schema 1.4 verlangt ab G1 die geordnete
`priorGateReceipts`-Kette G0..G(n-1). Im `--authorize`-Modus verifiziert
der Validator jedes Vorgänger-Receipt online gegen die GitHub-API (`gh`
mit `GH_TOKEN`/`GITHUB_TOKEN`; fehlendes Token oder `gh` ist fail-closed
`E_RECEIPT_GITHUB`): exakter `workflow_dispatch`-Run/-Attempt, Workflow,
`conclusion=success`, Trusted-Head und erfolgreicher
`gate-evidence-authorize`-Job mit der Receipt-Job-ID. Autoritative
Szenarioprofile und Schwellen werden aus dem Trusted-Tool-Stand geladen,
nie aus dem Subject; zusätzlich muss der deklarierte
`content.scenarioSha256` exakt dem SHA-256 des Vertrags im
Trusted-Checkout entsprechen (`E_SCENARIO_CONTRACT`) — Vertragsänderungen
müssen erst auf main landen. Die Bindung `GITHUB_SHA == trustedSha`
bedeutet: Der Trusted-Tool-Stand ist immer der aktuelle main-HEAD zum
Dispatch-Zeitpunkt; Trust-Bundle-Änderungen auf main machen ältere
Evidences stale-by-design. Fehlende, vertauschte, manipulierte oder
wiederverwendete Receipts sind Fail. Fehlender Node-/Ajv-Stack, ein
hängender Subprozess, Skip, Cancel oder Missing enden kontrolliert
fail-closed.

Der geschützte Authorize-Pfad ist als Job `gate-evidence-authorize` in
[.github/workflows/quality-gate.yml](../../.github/workflows/quality-gate.yml)
implementiert. Ablauf:

1. **Trigger:** ausschließlich `workflow_dispatch` auf `main` (Job-`if:` auf
   Event und `refs/heads/main`) hinter dem geschützten GitHub-Environment
   `quality-gate`; Job-Concurrency mit `cancel-in-progress: false`, damit
   laufende Authorize-Versuche nie abgebrochen werden. **Wichtig:** Das
   `quality-gate`-Environment muss einmalig von einem Maintainer in den
   Repo-Einstellungen mit Required-Reviewers-Schutz angelegt werden, bevor
   der erste Authorize-Lauf möglich ist — bewusster Root-of-Trust-Anker
   (D-066 Punkt 6).
2. **Inputs:** `evidencePath` (Pflicht, Form
   `quality/evidence/G<N>/<subjectSha>/<attempt>/GateEvidence.json`),
   `subjectSha` (Pflicht, 40-hex), `trustedSha` (Pflicht, 40-hex), `notes`
   (optional). Alle Inputs werden per `env:`-Mapping übergeben (kein
   `inputs.*` in `run`-Blöcken); der erste Step validiert Format,
   `trustedSha != subjectSha` und die Konsistenz des Evidence-Pfads mit
   `subjectSha` fail-closed.
3. **Checkouts:** Primary-Checkout des Trusted Tools an `trustedSha` nach
   `trusted/`, zweiter Checkout des Subjects an `subjectSha` nach `subject/`
   (beide `fetch-depth: 0`, `persist-credentials: false`). Der Validator
   verlangt `GITHUB_SHA == trusted-checkout-HEAD`; der Dispatch muss deshalb
   auf `main` erfolgen, solange der origin/main-HEAD dem Trusted-Commit
   entspricht — ein Guard-Step erzwingt `GITHUB_SHA == trustedSha` mit
   klarer Fehlermeldung.
4. **Guards (alle fail-closed):** `trustedSha != subjectSha`;
   `git merge-base --is-ancestor trustedSha origin/main`;
   `GITHUB_SHA == trustedSha`; Evidence existiert im Subject-Checkout und
   ist committed (`git log -1 -- <evidencePath>` nicht leer).
5. **Setup:** Node exakt auf `env.NODE_VERSION` gepinnt (muss der
   Evidence-deklarierten `trustBundle.nodeVersion` entsprechen),
   `npm ci --ignore-scripts --no-audit --no-fund --prefix trusted/quality`;
   python3 aus dem ubuntu-latest-Default.
6. **Job-ID:** Ermittlung der numerischen Job-ID über
   `gh api repos/{owner}/{repo}/actions/runs/<runId>/attempts/<attempt>/jobs`
   plus `jq` auf den eigenen Job — keine Selbst-Conclusion-Prüfung. Der Job
   besitzt dafür und für die Receipt-Verifikation `permissions:
   actions: read` (zusätzlich zu `contents: read`).
7. **Authorize:** `python3 trusted/quality/scripts/validate_gate_evidence.py
   --authorize --trusted-tool-checkout trusted --subject-root subject
   --job-id <id> --receipt-out $RUNNER_TEMP/GateAuthorization.json
   subject/<evidencePath>`.
8. **Receipt-Upload nur bei Exit 0:** Das Receipt wird unter genau dem vom
   Validator ausgegebenen Versionspfad
   `quality/authorizations/G<N>/<subjectSha>/<runId>-attempt<runAttempt>/GateAuthorization.json`
   abgelegt und als Artefakt
   `gate-authorization-G<N>-<runId>-attempt<runAttempt>` hochgeladen
   (`upload-artifact` auf vollen Commit-SHA gepinnt, `if: success()`,
   `if-no-files-found: error`). Die Evidence selbst wird nicht verändert.
9. **Kein Push/Commit im Workflow:** Die Versionierung des Receipts erfolgt
   unverändert per separatem append-only Folge-PR unter dem oben genannten
   Pfad.

## 8. Release-Grenze

Wiki 0.12.0 ist kein Game-Release. Tags, GitHub Releases, Deployment und
Store-Publikation benötigen separate ausdrückliche Autorität. G0, MS-0 und
MS-1 bleiben offen; dieser Rebaseline erzeugt keinen Tag.

## Offene Punkte

- Schema 1.4, Receipt-Schema, Receipt-Validierung, der `--authorize`-Modus
  und der geschützte Authorize-Workflow sind als G0-A2 implementiert und per
  Self-Test belegt. Das `quality-gate`-Environment muss noch einmalig von
  einem Maintainer angelegt werden; reale Receipts und ein realer
  Authorize-Lauf existieren noch nicht. .NET-/Paketpins und die Buildmatrix
  folgen in G0-B. Kein Gate-Pass wird durch dieses Dokument als erteilt
  behauptet.

## Nächste Schritte

1. G0-A1 ohne Gate-Fortschritt geschützt mergen.
2. G0-A2 abschließen: `quality-gate`-Environment mit Required-Reviewers
   anlegen, Authorize-Workflow adversarial prüfen, geschützt mergen und den
   ersten realen Authorize-Lauf samt append-only Receipt-Folge-PR belegen.
3. Am nachfolgenden sauberen Subject G0-B-Pins, Buildmatrix, Negative
   Controls und Binary-Hygiene umsetzen und dieses danach mit vollständiger
   Autorisierungskette und Umgebungsbindung als G0 autorisieren.
4. Required-Check-Konfiguration für `integrity` vor diesem Merge und für
   `gate-evidence-authorize` erst nach erfolgreicher realer G0-A2-CI prüfen.

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
| 1.5.0 | 2026-07-25 | G0-A-Umsetzungsstand: Schema 1.3 aktiv, Authorize-Topologie mit `--subject-root` ohne Evidence-Staging, trustedSha-Ancestor-Guard und GitHub-API-Verifikation der Kette dokumentiert | Lead DevOps Engineer |
| 1.6.0 | 2026-07-25 | D-065-Authorize-Run-Bindung (workflow_dispatch-Event, exklusiver `gate-evidence-authorize`-Job, eindeutige Run-IDs) und Restrisiko-Präzisierung aufgenommen | Lead DevOps Engineer |
| 1.7.0 | 2026-07-25 | D-066: G0-A1 auf Integrity begrenzt, zirkulären Authorize-Pfad entfernt und G0-A2 als zweiphasigen Receipt-Vertrag festgelegt | Lead DevOps Engineer |
| 1.8.0 | 2026-07-25 | G0-A2-Umsetzungsstand: Evidence-Schema 1.4.0, GateAuthorization-Receipt-Schema, `--authorize`-CLI und Receipt-Pfad-Konvention implementiert; geschützter Workflow bleibt offen | Lead DevOps Engineer |
| 1.9.0 | 2026-07-25 | Geschützten `workflow_dispatch`-Authorize-Job `gate-evidence-authorize` (Inputs, Guards, Artefakt-Fluss, Folge-PR) in §7 dokumentiert; `quality-gate`-Environment-Anlage als Root-of-Trust-Voraussetzung vermerkt | Lead DevOps Engineer |
| 1.9.1 | 2026-07-25 | Review-Härtung: `actions: read`-Permission, GitHub-API-Verifikation der Vorgänger-Receipts, Trusted-Vertragsdigest (`E_SCENARIO_CONTRACT`) und main-HEAD-Bindung des Trusted-Stands (stale-by-design) dokumentiert | Lead DevOps Engineer |
| 1.9.2 | 2026-07-25 | Review-N3: `GH_TOKEN` am Authorize-Step (G1+ API-Verifikation), append-only-Guard für `quality/authorizations/` in `check_docs.py` (Restrisiko Attempt-Substitution, siehe Testing.md §10) | Lead DevOps Engineer |
