# Teststrategie

**Version:** 1.7.0 | **Status:** verbindlich für MS-1 – G0-A1 implementiert, G0-A2 und Gate-Pass offen | **Verantwortungsbereich:** Lead QA Engineer | **Sprint:** 7

## Zweck

Definiert Testpyramide, Coverage, Matchkadenz, Gate-Evidence und
Fehlerdenominator. `docs-check` und der integrity-only PR-Job des
`quality-gate` existieren; eine Gate-Pass-Autorisierung existiert nicht und
G0 ist offen.

## Abhängigkeiten

- [../production/MVPRecoveryPlan.md](../production/MVPRecoveryPlan.md)
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-057 bis
  D-066
- [SimulationCore.md](SimulationCore.md), [Commands.md](Commands.md) und
  [FogOfWar.md](FogOfWar.md)
- [PerformanceBudget.md](PerformanceBudget.md)
- [`../../quality/scenarios/mvp-v1.json`](../../quality/scenarios/mvp-v1.json)
- [`../../quality/schemas/GateEvidence.schema.json`](../../quality/schemas/GateEvidence.schema.json)
- [`../../quality/scripts/validate_gate_evidence.py`](../../quality/scripts/validate_gate_evidence.py)
- [`../../quality/package-lock.json`](../../quality/package-lock.json) –
  gepinntes Ajv Draft 2020-12

## 1. Ebenen

| Ebene | Schwerpunkt |
|---|---|
| Unit | SimFixed, PRNG, IDs, Payloads, Moduleinvarianten |
| Contract | Golden Bytes, Hashdomänen, Fingerprint, Parser, asmdef-Grenzen |
| Integration | MatchSession→Ingress→Kernel→Snapshots, Save/Load, AI-Sidecar |
| Metamorphic | Hidden World, Umordnung/Dedupe, Restore/Fresh Host |
| Scenario | V1–V5b, MVP_FULL_100, Headless Matches |
| Manual | G4-Usability, G5-UI-only-Matches und neue Task-Tester |

Tests verwenden produktive Quellen und denselben Managed-Pfad wie MS-1.
Test-only-Direktmutation ist nur in expliziten Buildern vor Matchstart erlaubt.

## 2. G0-Baseline

### G0-A – Trusted-Gate-Bootstrap

Schema 1.2 prüft nur Struktur und Semantik. Jeder Pass-Versuch endet aktuell
zusätzlich mit `E_AUTHORIZATION_BOOTSTRAP`.

- **G0-A1 (dieser Stand):** Schema 1.3, Semantikvalidator, Trusted-Checkout-
  Topologie, Umgebungsbindung, Negative Controls und Gate-Runner bilden eine
  integrity-only Grundlage. Der PR-Workflow führt ausschließlich
  `integrity` aus.
- **G0-A2 (offen):** Der zweiphasige D-066-Receipt-Vertrag trennt Subject,
  Evidence-Carrier und Trusted Tooling. Erst dieser Folgebaustein darf einen
  geschützten Authorize-Job einführen.

Die Bootstrap-Arbeit wird ohne Gate-Fortschritt geschützt gemergt und darf
sich nicht selbst autorisieren. Erst ein späterer sauberer Subject-Commit
darf den abgeschlossenen Trustpfad für G0-B verwenden.

### G0-B – Plattformbasis

G0-B implementiert und belegt:

- exakte Unity-, .NET- und Paketpins,
- Windows-x64-/macOS-arm64-Clean-Build,
- .NET- und EditMode-Suiten,
- asmdef-/Architekturcheck,
- Architektur-Negative-Control, die bei absichtlicher verbotener Kante
  fehlschlägt,
- Schema- plus Semantikvalidator und dessen generierte Negativkontrollen,
- Prüfung auf getrackte generierte Binärdateien.

Ein roter Test wird nicht durch Filter, Ignore oder Quarantäne grün gerechnet.

## 3. G1-Pflichtsuiten

### Numerik und PRNG

- Q16.16-Grenzen, ties-to-even, negatives Welt→Grid-floor;
- Overflow/Division-by-zero als geprüfte deterministische Fehler;
- `SimAngle`-Wrap;
- XorShift128PlusV1-Golden-Vektoren und Snapshot-Fortsetzung.

### Commands

- 100 % des aktivierten Inventars;
- Golden Bytes je Payloadversion;
- unknown/invalid/reordered;
- byteidentisches und konflikthaftes Duplicate;
- Sequence/Dedupe über Snapshot;
- Backpressure und zustandsabhängiger Fail ohne Mutation.

### State/Persistence

- jedes autoritative Feld hash-sensitiv;
- Snapshot-Roundtrip byteidentisch;
- Restore/Fresh Host ≥1.000 Ticks mit pending Commands;
- Parser-Hardcap, Truncation und Korruption;
- Replay mit Human+AI-Commands ohne erneute AI-Ausführung.

### Plattform

`DETERMINISM_10000` erzeugt auf Windows x64 und macOS arm64 exakte
Checkpoint-Hashes und finale Bytes. Numerische Toleranzwerte dürfen diesen
Vergleich nicht ersetzen.

## 4. Coverage

Am G1-SHA:

| Scope | Mindest-Line-Coverage |
|---|---:|
| `Nova.Simulation` | 80 % |
| Command | 90 % |
| PRNG | 90 % |
| Serializer | 90 % |
| Hash | 90 % |
| Replay | 90 % |
| aktiviertes Command-Inventar | 100 % Fälle |

Coverage-Artefakte werden gehasht in Evidence aufgenommen. Prozentwerte ohne
Reportartefakt sind ungültig.

## 5. FoW-/KI-Tests

Hidden-World-Metamorphics variieren ausschließlich verborgenen Gegnerstate.
Committed Player-View, KI-Intents und legale Combat-Entscheidungen müssen
identisch bleiben. Zusätzlich:

- Radar-Ping ohne Zielrecht,
- Sichtwechsel erst am 5-Hz-Commit,
- AI liest keine Sim-Infrastruktur,
- Save/Load setzt AI-Sidecar identisch fort,
- Replay wendet AI nicht zweimal an.

## 6. Performance-Validierungen

| ID | Zeitpunkt | Pflicht |
|---|---|---|
| V1 | G1/MS-0 | exakte Plattformparität 10.000 Ticks |
| V2 | MS-0 | Rendering-CPU P95≤4 ms im 500-Objekt-Spike |
| V3 | MS-0 | Animation P95≤1,5 ms im 500-Objekt-Spike |
| V4 | MS-0 | Path P95≤4 ms bei 500 Agenten |
| V5a | vor G2 | SpatialHash+FoW-Filter+Commands; Pre-Combat-Rest P95≤3 ms |
| V5b | G3 | 500 Agenten mit realem Combat/AI; kein Crash/unbegrenztes Wachstum, Rohwerte |

Full-Content-Akzeptanz ist `MVP_FULL_100`; Full-Content-500 bleibt Diagnose.
Performance-Commands und -Messungen referenzieren dieselbe `environmentId`;
Windows-x64-Referenz und Mac-M2-Funktionslauf verwenden getrennte
Methodenprofile.

## 7. Automatische Matchvalidität

Ein Headless-Match zählt nur mit:

1. State-Hashes,
2. monotonen Ticks,
3. gültigem Matchresultat,
4. Core-Action-Trace und
5. lückenloser Checkpoint-Kette.

Crash, Timeout, ungültiger Output oder fehlendes Pflichtfeld bleibt als Fail im
Nenner.

## 8. Kadenz

| Lauf | Umfang |
|---|---|
| jeder PR | aggregiertes `quality-gate`: Tests, Coverage, Architektur, Golden, 4 Headless-Matches |
| Nightly | 2 geordnete Fraktionscluster ×20, gespiegelt =40 |
| Weekly | 2×200=400 |
| G5 | 3 Nightly-Matrizen am selben SHA =120 |

Docs-only-PRs deklarieren Scope explizit; `quality-gate` wird nicht
übersprungen. Bis G0 den Workflow real erzeugt, darf sein Ergebnis nicht
behauptet werden.

## 9. G5

- zwei manuelle UI-only-Matches, je eines pro Fraktion;
- drei neue Task-Tester;
- Median 20–35 Minuten;
- Save/Load an jedem Fünf-Minuten-Autosave-Punkt Minute 5–45;
- null offene P0/P1;
- keine gatekritische Quarantäne;
- `MVP_FULL_100` und Mac-M2-Baseline grün.

## 10. Evidence und Review

Jeder Gate-Versuch ist append-only sowie schema- und semantikvalid. Skip,
Cancel oder fehlendes Pflichtresultat ist Fail. Reviewer und Writer sind
verschieden; der Reviewer reproduziert mindestens einen kanonischen
Clean-Clone-Check als eigene Ausführung. Relevante Änderungen machen frühere
Evidence stale.

Der kanonische Check-Command trägt nie einen Executor-Schalter. Die
Implementation führt ihn unverändert aus; der Reviewer wiederholt denselben
Command im Clean Clone mit der Umgebungsvariable `NOVA_GATE_EXECUTOR=reviewer`
(der Schalter `--executor` existiert nur für lokale Reproduktion). Die
`commandId` folgt maschinell erzwungen dem Präfix `impl-` (Implementation)
beziehungsweise `review-` (Reviewer), etwa `impl-g0-architecture` und
`review-g0-architecture` für denselben Check.

Die öffentliche CLI führt gepinntes Ajv Draft 2020-12 und die
Cross-Field-Prüfung für Schema 1.3 gemeinsam aus. Lokal bleibt diese Prüfung
integrity-only: Für `verdict=pass` entsteht ohne
`--trusted-tool-checkout <checkout>` zusätzlich
`E_AUTHORIZATION_BOOTSTRAP`, ohne `--trust-context <external.json>` bleibt
`E_TRUST_CONTEXT`. Ein lokales Evidence-Dokument autorisiert daher kein Gate.

G0-A1 prüft Manifest, Szenariovertrag, Schema, Python-Validator,
Ajv-Wrapper, `package.json`, Lockdatei, Gate-Runner und Workflow als
Trust-Bundle und testet getrennte Trusted-/Subject-Checkouts. Diese Prüfungen
belegen ausschließlich Integrität. D-066 hat den zirkulären
`gate-evidence-authorize`-Job und seinen Generator entfernt; auch ein
syntaktisch korrekter alter Trust-Kontext endet mit
`E_AUTHORIZATION_BOOTSTRAP`.

G0-A2 muss drei Identitäten trennen:

1. `subjectCommitSha` für den geprüften Produktstand,
2. `evidenceCarrierCommitSha` für die später eingecheckte Evidence und
3. `trustedToolCommitSha` für die unabhängigen Prüftools.

Der aktuelle geschützte Lauf erzeugt erst nach erfolgreicher Validierung
einen hashgebundenen `GateAuthorization.json`-Kandidaten. Er bindet seinen
Runtime-Run/-Attempt/-Job, prüft aber nicht zirkulär seine eigene noch
ausstehende Conclusion. Nach erfolgreichem Job wird das Receipt unverändert
append-only versioniert. Spätere Gates akzeptieren frühere Receipts nur,
wenn GitHub den exakten Run, Attempt, Workflow, Gate, Evidence-Hash und
Authorize-Job als erfolgreich bestätigt. G0 benötigt kein Vorgänger-Receipt;
G1 ohne erfolgreiches G0-Receipt ist ungültig. Szenarioprofile und Schwellen
kommen dabei aus dem Trusted-Tool-Stand, nicht aus dem änderbaren Subject.

`--self-test` erzeugt Struktur-Baselines und negative Fälle nur temporär und
muss in G0
unter anderem No-op-Commands, falsche/missing Check-Artefakte, lokale
Pass-Autorisierung, falsche Units, negative Samples, unvollständige
Drei-Lauf-Messungen, Szenarioschwellen, Subject-Blob-Hashes,
schemawidrige Vorgänger und eine fehlende Gate-Kette ablehnen. Für G0-A kommen
manipuliertes Subject-Schema/Ajv-Wrapper/Lockfile, unvollständige
Autorisierungsketten, falsche oder widersprüchliche Umgebungen,
fehlender Node-/Ajv-Stack und ein hängender Schema-Subprozess hinzu. Kriterien-
und Szenarioprofile kommen aus
[`mvp-v1.json`](../../quality/scenarios/mvp-v1.json).

Ab G1 verweist jeder Gateversuch genau auf die semantikvalide Evidence des
unmittelbaren Vorgängergates am selben Subject-Commit/-Tree. Szenariometriken
heißen `scenario.<ID>.<metric>`; Pflichtassertions
`scenario.<ID>.assertion.<assertion>` mit `unit=bool` und `[1]`. Das
Szenariokriterium referenziert zusätzlich `check:<criterionId>`. Genau dieser
kanonische Implementation-Check muss das Szenario als ausgeführt deklarieren;
ein freier `command:<id>` genügt nicht. `stdout`, `stderr` und Check-Ergebnis
sind gehashte Attempt-Artefakte.

Der zukünftige Receipt-Vertrag autorisiert nicht nur diese unmittelbare
Referenz, sondern die vollständige geordnete Kette von G0 bis zum aktuellen
Gate.

Punktmetriken besitzen exakt `name`, `unit`, `samples`; Performance-Metriken
exakt `name`, `unit`, `measurement`. Letzteres bindet den 30-s-Warmup und drei
getrennte 120-s-Läufe mit mindestens einer nichtnegativen Rohprobe pro
Sekunde. P95/P99 werden per Nearest-Rank je Lauf und kombiniert berechnet;
Ausreißer werden nicht entfernt. Schema 1.3 ergänzt `environmentId` an Command
und Messung; beide Werte müssen identisch sein und auf das getrennte
Windows-x64- beziehungsweise Mac-M2-Methodenprofil zeigen.

Im Solo-/KI-Modus reicht unabhängiges read-only Review. Ab zwei aktiven
menschlichen Maintainers ist eine zweite menschliche Freigabe Pflicht.

## Offene Punkte

- Schema 1.3, Trusted-Checkout-Topologie und Gate-Runner sind als G0-A1-
  Integritätsgrundlage implementiert. G0-A2 mit `GateAuthorization.json`,
  geschütztem Authorize-Workflow und realem Receipt-Lauf ist offen; deshalb
  kann weiterhin kein Gate autorisiert werden.

## Nächste Schritte

1. G0-A1 ohne Gate-Fortschritt geschützt mergen.
2. G0-A2 als separaten zweiphasigen Receipt-Authorizer implementieren,
   adversarial prüfen und geschützt mergen.
3. Am nachfolgenden sauberen Subject den Trustpfad, G0-B-Suiten,
   Umgebungsprofile und Negative Controls beweisen.
4. G1-Golden-/Coverage-Gates vor Gameplay aufbauen und Evidence nur aus
   realen, sauberen Läufen schreiben.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead QA Engineer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings) | Lead QA Engineer |
| 1.0.0 | 2026-07-24 | Testvertrag auf G0–G5, G1-Coverage, V1–V5b, Matchdenominator und D-061-Evidence rebaselined | Lead QA Engineer |
| 1.1.0 | 2026-07-24 | Evidence-Semantikvalidator, Negativkontrollen und maschinenlesbare Gate-Profile ergänzt | Lead QA Engineer |
| 1.2.0 | 2026-07-24 | D-062-Szenariobindung, Nearest-Rank-Schwellen, Subject-Blobs und Same-Subject-Vorgängergates ergänzt | Lead QA Engineer |
| 1.3.0 | 2026-07-24 | D-063-Schema 1.2, Check-Artefakte, externen Trust-Kontext, rekursive Ajv-Prüfung und getrennte Performance-Läufe verankert | Lead QA Engineer |
| 1.4.0 | 2026-07-24 | D-064-Fail-Closed-Autorisierung, zweistufigen Trusted-Tool-Bootstrap, vollständige Kette und Umgebungsprofile verankert | Lead QA Engineer |
| 1.5.0 | 2026-07-25 | G0-A-Umsetzungsstand: Schema 1.3/Trusted-Checkout mit `--subject-root`-Topologie, GitHub-API-Verifikation der Kette, `NOVA_GATE_EXECUTOR`/commandId-Konvention und Restrisiko Review-Attestierung dokumentiert | Lead QA Engineer |
| 1.6.0 | 2026-07-25 | D-065-Authorize-Run-Bindung (workflow_dispatch-Event, exklusiver `gate-evidence-authorize`-Job, eindeutige Run-IDs, `ci.jobName`-Konstante) und Restrisiko-Präzisierung aufgenommen | Lead QA Engineer |
| 1.7.0 | 2026-07-25 | D-066: G0-A1 auf Integrität begrenzt, zirkulären Authorizer zurückgezogen und G0-A2 als zweiphasigen Receipt-Vertrag mit getrennten Subject-/Carrier-/Trusted-Identitäten festgelegt | Lead QA Engineer |
