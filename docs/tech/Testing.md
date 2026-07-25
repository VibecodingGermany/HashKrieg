# Teststrategie

**Version:** 1.4.0 | **Status:** verbindlich für MS-1 – G0-A aktiv, Autorisierung gesperrt | **Verantwortungsbereich:** Lead QA Engineer | **Sprint:** 7

## Zweck

Definiert Testpyramide, Coverage, Matchkadenz, Gate-Evidence und
Fehlerdenominator. Die Anforderungen beschreiben den zu bauenden
`quality-gate`; aktuell existiert nur `docs-check`, G0 ist offen.

## Abhängigkeiten

- [../production/MVPRecoveryPlan.md](../production/MVPRecoveryPlan.md)
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-057 bis
  D-064
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
zusätzlich mit `E_AUTHORIZATION_BOOTSTRAP`. G0-A implementiert Schema 1.3,
den subject-unabhängigen Trusted-Tool-Checkout, die vollständige geordnete
`authorizedEvidence`-Kette und die `environmentId`-Bindung. Die Bootstrap-
Änderung wird ohne Gate-Fortschritt geschützt gemergt und darf sich nicht
selbst autorisieren. Erst ein nachfolgender sauberer Subject-Commit darf den
neuen Trustpfad für G0-B verwenden.

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

Die öffentliche CLI führt gepinntes Ajv Draft 2020-12 und die
Cross-Field-Prüfung für Schema 1.2 gemeinsam aus. Diese Prüfung ist
integrity-only: Für `verdict=pass` entsteht unabhängig von einem
`--trust-context <external.json>` zusätzlich
`E_AUTHORIZATION_BOOTSTRAP`. Ohne externen Kontext bleibt außerdem
`E_TRUST_CONTEXT`. Schema 1.2 autorisiert daher kein Gate.

Der Schema-1.3-Zielvertrag führt Manifest, Szenariovertrag, Schema, Python-
Validator, Ajv-Wrapper, `package.json`, Lockdatei, Gate-Runner und Authorize-
Workflow aus einem separaten Trusted-Tool-Checkout aus und bindet
Subject-/Trusted-Commit, SHA-256 sowie die exakte Node-Version. Eine Änderung
an diesem Bundle wird ohne Gate-Fortschritt gemergt und gilt erst für einen
nachfolgenden sauberen Subject-Commit.

Der externe Kontext enthält die vollständige geordnete
`authorizedEvidence`-Kette von G0 bis zum aktuellen Gate. Jeder Eintrag bindet
Gate, Pfad, Evidence-Hash, Subject-Commit/-Tree, CI-Run/-Job sowie CI- und
Review-Attestierung und wird gegen GitHub verifiziert. Fehlende, zusätzliche,
vertauschte oder nur lokale Einträge sind Fail.

`--self-test` erzeugt positive und negative Fälle nur temporär und muss in G0
unter anderem No-op-Commands, falsche/missing Check-Artefakte, lokale
Pass-Autorisierung, falsche Units, negative Samples, unvollständige
Drei-Lauf-Messungen, Szenarioschwellen, Subject-Blob-Hashes,
schemawidrige Vorgänger und eine fehlende Gate-Kette ablehnen. Für G0-A kommen
manipuliertes Subject-Schema/Ajv-Wrapper/Lockfile, unvollständige
`authorizedEvidence`-Ketten, falsche oder widersprüchliche Umgebungen,
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

Der Trust-Kontext autorisiert nicht nur diese unmittelbare Referenz, sondern
die vollständige geordnete Kette von G0 bis zum aktuellen Gate.

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

- Der reale Schema-1.3-/Trusted-Tool-Authorize-Workflow ist G0-A-Arbeit und
  in diesem Rebaseline absichtlich noch nicht als bestanden behauptet.

## Nächste Schritte

1. G0-A Trusted-Tool-Bundle und Schema 1.3 ohne Gate-Fortschritt
   implementieren und geschützt mergen.
2. Am nachfolgenden sauberen Subject den Trustpfad, G0-B-Suiten,
   Umgebungsprofile und Negative Controls beweisen.
3. G1-Golden-/Coverage-Gates vor Gameplay aufbauen und Evidence nur aus
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
