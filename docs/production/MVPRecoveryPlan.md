# MVP-Recovery-Plan

**Version:** 1.4.0 | **Status:** verbindlich – G0-A aktiv, Autorisierung gesperrt | **Verantwortungsbereich:** Producer / Lead Technical Director / Lead QA Engineer | **Sprint:** 7

## Zweck

Dieser Plan führt den vorhandenen Prototyp sequenziell zu einem nachweisbaren
MS-1. Fortschritt entsteht nur durch bestandene Gates mit reproduzierbarer
Evidenz, nicht durch Dateien, Typen, isolierte Tests oder Berichte. Aktueller
Stand ist **G0 offen**; MS-0 und MS-1 sind nicht erreicht.

## Abhängigkeiten

- [ImplementationAudit_2026-07-24.md](ImplementationAudit_2026-07-24.md) –
  eingefrorener Ausgangsbefund
- [DecisionLog.md](DecisionLog.md) – D-055 bis D-064
- [MVPContentManifest.md](MVPContentManifest.md) – exakter MS-1-Inhalt
- [Milestones.md](Milestones.md) – Zuordnung von Gates zu MS-0/MS-1
- [../tech/SimulationCore.md](../tech/SimulationCore.md),
  [../tech/Commands.md](../tech/Commands.md) und
  [../tech/Testing.md](../tech/Testing.md)
- [`../../quality/content/mvp-v1.json`](../../quality/content/mvp-v1.json) und
  [`../../quality/scenarios/mvp-v1.json`](../../quality/scenarios/mvp-v1.json)
- [`../../quality/schemas/GateEvidence.schema.json`](../../quality/schemas/GateEvidence.schema.json)
- [`../../quality/scripts/validate_gate_evidence.py`](../../quality/scripts/validate_gate_evidence.py)
- [`../../quality/package.json`](../../quality/package.json) und
  [`../../quality/package-lock.json`](../../quality/package-lock.json) –
  gepinnter Draft-2020-12-Validator

## 1. Status- und Autoritätsregeln

| Aussage | Aktueller Status |
|---|---|
| Sprint 7 | aktiv, Recovery |
| G0 | offen |
| MS-0 | nicht erreicht |
| MS-1 / MVP | nicht erreicht |
| Alpha | nicht begonnen |

Die Gates müssen in der Reihenfolge
`G0 → G1 einschließlich V1–V5a → G2 → G3 → G4 → G5` bestanden werden.
Ein späteres Gate kann ein früheres nicht ersetzen. Relevante Änderungen machen
bestehende Evidenz ab dem betroffenen Gate ungültig.

## 2. Evidenzvertrag

Ein Gate gilt nur mit einer schema- und semantikvaliden Datei unter:

`quality/evidence/G<N>/<subjectSha>/<attempt>/GateEvidence.json`

In diesem Rebaseline werden **keine** Evidence-Platzhalter angelegt. Jeder
Versuch ist append-only und enthält mindestens:

- exakten Commit- und Tree-SHA, `dirty=false`, optionalen vorherigen Versuch
  desselben Gates und ab G1 die unmittelbare Vorgängergate-Evidence;
- SHA-256 der Content-, Szenario-, Evidence-Schema- und
  Semantikvalidator-**Git-Blobs am Subject-Commit**;
- Toolchains, Paketstände und Ausführungsumgebungen;
- kanonische kriterienspezifische Checks, Exitcodes, Coverage sowie
  gehashte `stdout`-/`stderr`-/Check-Artefakte im aktuellen Attempt;
- rohe Messreihen und Artefakte mit Hashes;
- CI-Ergebnis;
- unabhängigen Reviewer und mindestens einen von ihm im Clean Clone
  reproduzierten Befehl;
- Zuordnung jedes Kriteriums zu seinem Nachweis und ein binäres Urteil.

`skipped`, `cancelled` oder ein fehlendes Pflichtresultat ist **fail**. Reviewer
und Implementation Writer dürfen nicht dieselbe Identität sein. In Solo-/KI-
Arbeit ersetzt ein unabhängiges read-only Review die Selbstfreigabe. Sobald
mindestens zwei aktive menschliche Maintainer existieren, ist zusätzlich eine
zweite menschliche Freigabe Pflicht.

Die öffentliche CLI führt gepinntes Ajv Draft 2020-12 und die Semantikprüfung
gemeinsam aus. Schema 1.2 ist nach D-064 jedoch nur eine Integritätsvorstufe:
Jeder Pass-Versuch erhält bis zum Trusted-Gate-Bootstrap zusätzlich
`E_AUTHORIZATION_BOOTSTRAP`. Auch ein syntaktisch passender Aufruf

`python3 quality/scripts/validate_gate_evidence.py --trust-context <external.json> <GateEvidence.json>`

autorisiert deshalb aktuell kein Gate. Ohne den außerhalb des Repos im
geschützten `quality-gate` erzeugten Trust-Kontext bleibt außerdem
`E_TRUST_CONTEXT`.
Der Validator prüft unter anderem Count-Gleichheit, Coverage-Schwellen,
Writer/Reviewer-Trennung, Subject-Blobs, Artefakte, Vorgängerkette,
Gate-Profil und auflösbare Referenzen. Referenzen verwenden ausschließlich
`check:<criterionId>`, `command:<id>`, `artifact:<path>`, `metric:<name>`,
`coverage:<scope>`, `ci:<runId>` oder `scenario:<id>`. Ein
`command:<id>` ersetzt niemals den gleichnamigen Pflichtcheck. Die exakten
Kriterienprofile stehen in
[`mvp-v1.json`](../../quality/scenarios/mvp-v1.json).

D-062 verschärft diese Regeln:

- G1–G5 referenzieren genau das unmittelbare Vorgängergate samt Evidence-
  SHA-256; dessen rekursiv valider `pass` muss denselben Commit und Tree
  belegen. G0 hat keine Vorgängergate-Referenz.
- `gateUsage` und Gate-Profil müssen exakt übereinstimmen.
- Ein referenziertes Szenario bindet mindestens einen ausgeführten Command
  sowie alle Assertions und Schwellenmetriken.
  Namen folgen `scenario.<ID>.<metric>` beziehungsweise
  `scenario.<ID>.assertion.<assertion>`.
- Assertion-Metriken sind `unit=bool`, Samples exakt `[1]`. Jedes
  punktbasierte Metrikartefakt enthält als striktes JSON exakt `name`,
  `unit`, `samples`; D-063 definiert für Performance stattdessen
  `name`, `unit`, `measurement`.
- P95/P99 verwenden Nearest-Rank ohne Interpolation oder
  Ausreißerentfernung; Minimum, Maximum und Gleichheit werden direkt aus
  denselben Rohsamples geprüft.

D-063 ersetzt Schema 1.1 und verschärft:

- aktuelles Dokument und jede Vorgängerevidenz müssen gegen das
  Subject-Schema `1.2.0` bestehen; fehlendes Ajv, defektes Schema oder
  schemawidriger Vorgänger ist fail-closed;
- jedes Kriterium bindet genau einen kanonischen Implementation-Check; der
  Reviewer wiederholt mindestens einen Check als eigene Ausführung;
- `stdout`, `stderr`, Check-Ergebnis, CI und Review sind hashgebundene
  Attempt-Artefakte;
- exakte Einheiten, nichtnegative Samples, 30-s-Warmup sowie drei getrennte
  120-s-Läufe gelten für alle Performance-Szenarien; Schwellen werden pro
  Lauf und kombiniert geprüft;
- ein Trust-Kontext aus dem unveränderten geschützten Workflow auf `main`
  bindet Evidence-Hash, Subject, CI- und Reviewer-Attestierung.

D-064 sperrt den Autorisierungsanspruch von Schema 1.2 und macht folgende
Punkte zum zwingenden G0-A-Exit:

- Schema 1.3 wird aus einem subject-unabhängigen Trusted-Tool-Checkout
  ausgeführt und bindet Manifest, Szenariovertrag, Schema, Python-/Ajv-
  Validator, Paketdateien, Gate-Runner und Authorize-Workflow samt exakter
  Node-Version;
- Trust-Bundle-Änderungen werden zuerst ohne Gate-Fortschritt gemergt und
  dürfen erst einen nachfolgenden sauberen Subject-Commit prüfen;
- der externe Kontext autorisiert die vollständige geordnete Gate-Kette,
  nicht nur das aktuelle Dokument;
- Command und Performance-Messung referenzieren dieselbe geprüfte Umgebung;
  Windows-x64 und Mac M2 verwenden getrennte Methodenprofile;
- Manipulations-, Ketten-, Umgebungs-, Missing-Tool- und Timeout-Negativtests
  sind grün.

## 3. Gate G0 – reproduzierbare Plattform

**Ziel:** Eine saubere, versionierte Ausgangsbasis, noch ohne Feature-Fertigmeldung.

### G0-A – Trusted-Gate-Bootstrap

Zuerst wird D-064 als Bootstrap-Änderung implementiert und ohne Gate-
Fortschritt über den geschützten PR-Prozess gemergt. Der anschließende
saubere Subject-Commit muss Schema 1.3, das externe Trusted Tooling, die
vollständige Autorisierungskette und die Umgebungsbindung beweisen. Eine
Änderung am Trust-Bundle darf nicht zugleich diese Änderung autorisieren.

### G0-B – Plattformbasis

Danach verlangt G0:

1. exaktes Unity-Pin `6000.5.4f1`, Revision `d550df8bd089`, URP sowie
   festgeschriebene .NET-SDK- und Paketversionen;
2. getrennte, versionierte SimRunner-Projekte, die dieselben Core-/Simulation-
   Quellen und determinismusrelevanten Defines wie Unity verwenden;
3. automatisierte asmdef-/Architekturprüfung der Grenzen `Nova.Core`,
   `Nova.Simulation`, `Nova.AI` und Hosts;
4. saubere Windows-x64- und macOS-arm64-Builds;
5. grüne .NET- und EditMode-Tests;
6. einen dokumentierten Architektur-Negative-Control-Lauf, der bei
   absichtlicher Vertragsverletzung rot wird;
7. `run_gate_check.py`, den geschützten `quality-gate`-Trustpfad und die
   D-064-Negativkontrollen einschließlich
   `python3 quality/scripts/validate_gate_evidence.py --self-test`;
8. keine getrackten generierten Binärdateien; und
9. keinerlei Gate-Pass aus einem lokalen/untrusted Evidence-Dokument.

**Exit:** G0-A ist zuvor aus einem älteren Trusted-Tool-Stand aktiviert. Alle
G0-B-Kriterien liegen am selben nachfolgenden sauberen SHA in
Schema-1.3-Evidence vor. Ein lokaler Teilcheck oder Schema 1.2 schließt G0
nicht.

## 4. Gate G1 – kanonischer deterministischer Kern

**Ziel:** Fixed-Point, Commands, State, Snapshot und Replay sind ein
plattformübergreifend identischer Vertrag.

G1 verlangt:

- `SimFixed` Q16.16, 10 Hz, `XorShift128PlusV1` und geprüfte Faults gemäß
  [SimulationCore.md](../tech/SimulationCore.md);
- den versiegelten Command-Pfad einschließlich Schema v1, Sortierung, Dedupe,
  Backpressure und Fehlermodell aus [Commands.md](../tech/Commands.md);
- vollständiges autoritatives State-Inventar;
- XXH64-Domänen mit Seed 0 und exakten Fingerprint;
- byteidentischen Snapshot-Roundtrip;
- mindestens 1.000 identische Fortsetzungsticks nach Restore mit bereits
  gequeuten Commands;
- Replay aller akzeptierten Human-/KI-Commands ohne erneute KI-Anwendung;
- per-Block-Hashsensitivität und Parsergrenzen;
- einmaligen Pre-G1-Kompatibilitätsreset der Prototypformate und
- exakte Windows-x64-/macOS-arm64-Hashes **und** finale Bytes über 10.000 Ticks.

Coverage am G1-SHA:

| Scope | Mindestwert |
|---|---:|
| `Nova.Simulation` gesamt | 80 % |
| Command, PRNG, Serializer, Hash und Replay | je 90 % |
| aktiviertes Command-Inventar | 100 % |

### MS-0-Validierungen V1–V5a

| ID | Nachweis | Gate-Kriterium |
|---|---|---|
| V1 | Cross-Plattform-Fixed-Point | 10.000 Ticks, exakte Hashes und finale Bytes |
| V2 | URP-Renderingpfad | Rendering-CPU P95 ≤4 ms im 500-Objekt-Spike |
| V3 | Animationspfad | Animations-CPU P95 ≤1,5 ms im 500-Objekt-Spike |
| V4 | Flow-Field-Pathfinding | 500 Agenten, Path P95 ≤4 ms |
| V5a | Pre-Combat-Kostenmodell | repräsentative SpatialHash-, FoW-Filter- und Command-Verarbeitung; Rest-Sim P95 ≤3 ms |

V5a ist G2-Eintrittsvoraussetzung. Es implementiert noch keinen fertigen Combat-
oder KI-Scope. MS-0 ist erst erreicht, wenn G0, G1 und V1–V5a am geforderten
Stand bestanden sind.

## 5. Gate G2 – integrierter Player-Kernloop

**Ziel:** Ein menschlicher Spieler durchläuft den Graybox-Kern über
`MatchSession`.

G2 verlangt:

- Start, Pause, Orders, Bau, Produktion, Bewegung, Kampf und Matchende über den
  normalen Session-/Command-Pfad;
- den vollständigen Graybox-Aetherium-Loop aus D-010 einschließlich endlicher
  Reserve, Nachwachsen, Ausbreitung, Überernte, Warnung und Expansion;
- Glutrinne als technisch korrektes 128×128-Testlayout;
- committed FoW zwischen Movement und Combat und
- einen Test, der direkte Mutation außerhalb des Kernels zuverlässig ablehnt.

Debug-UI, Inspector-Manipulation oder direkte State-Aufrufe erfüllen das Gate
nicht.

## 6. Gate G3 – KI, Fortsetzung und reale Last

**Ziel:** Die KI spielt denselben Kern über dieselben Regeln und bleibt
reproduzierbar.

G3 verlangt:

- KI liest ausschließlich die committed, teamgefilterte Ansicht;
- KI erzeugt nur kanonische Intents;
- Human- und KI-Commands sind gemeinsam replaybar;
- Save/Load setzt KI-Sidecar und bereits geplante Aktionen identisch fort;
- Hidden-World-Metamorphics belegen, dass verborgener State weder KI noch
  Combat leakt;
- automatische Matches liefern Hashes, monotone Ticks, gültiges Ergebnis,
  Core-Action-Trace und Checkpoint-Kette und
- **V5b** wiederholt den 500-Agenten-Lauf mit realem Combat und realer KI.

V5b muss ohne Crash oder unbeschränktes Wachstum laufen und alle Rohwerte
berichten. Der vollständige 500-Agenten-Inhalt ist Diagnose, kein
MS-1-Content-Versprechen.

## 7. Gate G4 – exakter Produktionsumfang

**Ziel:** Der MS-1-Inhalt ist vollständig, integriert und bedienbar.

G4 verlangt:

- bytegenaue Übereinstimmung mit
  [`mvp-v1.json`](../../quality/content/mvp-v1.json);
- produktionsfähige Glutrinne mit fünf festgelegten Feldern und zwei Routen;
- Allianz und Legion mit je neun Gebäude- und acht Einheitenrollen;
- HUD, Einstellungen, Pause, zehn manuelle Slots, Quicksave A/B, drei
  Fünf-Minuten-Autosaves, Laden und Backup-Recovery;
- Rebinding, UI-Skalierung 80–150 %, Farb-/Formredundanz und reduzierte
  Shake-/Flash-Optionen;
- normales UI-only-Match mit ≤100-ms-Command-Feedback;
- vollständige Content-/Lizenzprovenienz und
- bestandene G4-Usability-Aufgaben.

Keine zurückgestellte Funktion darf als versteckte Voraussetzung des
Produktionspfads bestehen.

## 8. Gate G5 – eingefrorene MVP-Abnahme

**Ziel:** Derselbe saubere SHA erfüllt automatisierte, manuelle und
Performance-Abnahme.

### Automatisierte Abnahme

- drei vollständige Nightly-Matrizen am selben SHA, zusammen 120 Matches;
- jeder fehlgeschlagene oder ungültige Lauf bleibt im Nenner;
- kein gatekritischer Test in Quarantäne;
- `MVP_FULL_100` auf der D-052-Windows-Referenz;
- Mac-M2-Funktionsbaseline bei 1080p/Medium: P95 ≤33,3 ms, P99 ≤50 ms.

Die Windows-Methode ist: Standalone IL2CPP Development, Managed/Burst aus,
2560×1440, `NovaReference`, VSync und Deep Profiling aus, festes Replay,
30 s Warmup plus drei getrennte 120-s-Messläufe, keine
Ausreißerentfernung und mindestens eine Rohprobe pro Sekunde. Jede Schwelle
muss pro Lauf und über die unveränderte Konkatenation bestehen. Schwellen
stehen verbindlich in
[PerformanceBudget.md](../tech/PerformanceBudget.md) und
[`mvp-v1.json`](../../quality/scenarios/mvp-v1.json).

### Manuelle Abnahme

- zwei UI-only-Matches, je eines als Allianz und Legion;
- drei neue Task-Tester;
- Matchdauer-Median 20–35 Minuten;
- Save/Load-Nachweis an jedem Fünf-Minuten-Autosave-Zeitpunkt von Minute 5 bis
  45;
- null offene P0- oder P1-Defekte.

**Exit:** Unabhängiger Reviewer reproduziert mindestens einen kanonischen
Clean-Clone-Check. Erst eine vom geschützten Trust-Kontext samt vollständiger
Gate-Kette autorisierte 1.3-Evidence mit `verdict=pass` erreicht MS-1.

## 9. Laufkadenz

| Kadenz | Pflichtumfang |
|---|---|
| jeder PR | aggregiertes `quality-gate`; expliziter Docs-only-Scope, niemals Skip; Tests, Coverage, Architektur, Golden und 4 Headless-Matches |
| Nightly | 2 geordnete Fraktionscluster ×20, gespiegelt = 40 Matches |
| Weekly | 2×200 = 400 Matches |
| G5 | 3 Nightly-Matrizen am selben SHA = 120 Matches |

Ein automatisches Match ist nur gültig, wenn Hashes, monotone Ticks, ein
gültiges Ergebnis, Core-Action-Trace und Checkpoint-Kette vorliegen.

## Offene Punkte

- Q-018 (Preis) und Q-019 (Telemetrie) bleiben offen und blockieren kein Gate
  bis G5.
- Nach G5 braucht jeder Alpha-/Post-MVP-Scope eine neue Entscheidung.

## Nächste Schritte

1. Ausschließlich G0-A implementieren und als nicht selbstautorisierende
   Bootstrap-Änderung mergen.
2. Am nachfolgenden sauberen Subject G0-B implementieren und belegen.
3. Danach G1 test-first aufbauen; keine G2-Funktion vor bestandenem V5a.
4. Evidence-Verzeichnisse erst durch reale Gate-Versuche erzeugen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-24 | Evidenzbasierten Recovery-Pfad G0–G5 und empfohlenen reduzierten MVP-Scope definiert | Producer / Lead Technical Director / Lead QA Engineer |
| 1.0.0 | 2026-07-24 | D-056–D-061 als verbindlichen G0–G5-, Evidence- und Abnahmevertrag rebaselined | Producer / Lead Technical Director / Lead QA Engineer |
| 1.1.0 | 2026-07-24 | Evidence-Semantikvalidator, SHA-256-Dateibindung, Referenzsyntax und eigenes G0-Negativgate ergänzt | Producer / Lead Technical Director / Lead QA Engineer |
| 1.1.1 | 2026-07-24 | Gate-Gültigkeit ausdrücklich an Schema- und Semantikprüfung gebunden | Producer / Lead Technical Director / Lead QA Engineer |
| 1.2.0 | 2026-07-24 | D-062: Szenariometriken, Subject-Blob-Hashes und rekursive Same-Subject-Gate-Kette verbindlich ergänzt | Producer / Lead Technical Director / Lead QA Engineer |
| 1.3.0 | 2026-07-24 | D-063: Schema 1.2, kanonische Check-Artefakte, geschützten Trust-Kontext, rekursive Draft-2020-12-Prüfung und Drei-Lauf-Methode verankert | Producer / Lead Technical Director / Lead QA Engineer |
| 1.3.1 | 2026-07-24 | Punkt- und Performance-Metrikartefakte im D-062/D-063-Übergang eindeutig getrennt | Producer / Lead Technical Director / Lead QA Engineer |
| 1.4.0 | 2026-07-24 | D-064: Schema 1.2 auf Integritätsprüfung begrenzt und G0-A Trusted-Gate-Bootstrap vor G0-B eingeführt | Producer / Lead Technical Director / Lead QA Engineer |
