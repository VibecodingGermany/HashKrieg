# Kanonischer Command-Vertrag

**Version:** 1.2.0 | **Status:** verbindlich für lokalen Pfad und D-089-1v1-Profil | **Verantwortungsbereich:** Lead Technical Director / Lead Multiplayer Engineer | **Sprint:** 12

## Zweck

Definiert die einzige Eingangsschnittstelle für autoritative
Zustandsänderungen. UI, KI, Local Loopback, D-089-TCP-Relay und Replay
verwenden dasselbe Binärformat und dieselben Validierungsregeln.

## Abhängigkeiten

- [SimulationCore.md](SimulationCore.md) – Tick, IDs, Fingerprint und State
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-057, D-061 und D-089
- [InputSystem.md](InputSystem.md) – Client-Intents und Feedback
- [Replication.md](Replication.md) – Command-Stream und Replay

## 1. Vertrauensgrenze

UI und KI erzeugen nur `CommandIntent`. `MatchSession` und `CommandIngress`
besitzen die Autorität, daraus einen Command zu machen:

1. Session bindet `PlayerSlot`.
2. Ingress weist je Spieler eine monoton steigende `Sequence` zu.
3. Ingress setzt `TargetTick = EnqueueTick + InputDelayTicks`.
4. `InputDelayTicks` ist Teil des Match-Fingerprints. Der kanonische lokale
   Defaultwert ist 1; `MatchConfig`/Loopback erlauben 1 bis 60. Im
   D-089-Netzprofil ist der Wert während der Session fest, standardmäßig 3 und
   ebenfalls gültig von 1 bis 60.
5. Der gebundene Transport liefert Records als rohe kanonische Bytes an die
   strukturelle Ingress-Grenze; das gilt für `LocalLoopbackTransport` und den
   TCP-Relay-Pfad.
6. Der Kernel akzeptiert ausschließlich einen validierten, versiegelten
   `CommandBatch`.

`ICommandTransport` bleibt unverändert. Ein Transport mit eigenem
Verbindungslebenszyklus kann zusätzlich `ICommandSubmissionReadiness`
implementieren. `CommandIngress.TrySubmitIntent` prüft diese Bereitschaft vor
Session-Aktion, Payloadverarbeitung und Sequenzvergabe. Ist sie nicht gegeben,
lautet das Ergebnis `Rejected` mit `TransportNotReady`; keine Session-Aktion
wird eingereiht und keine Sequenz verbraucht. Der Relay-Client ist nur in
seiner Phase `Running` bereit. Transporte ohne diesen optionalen Vertrag
behalten das bisherige Always-ready-Verhalten.

Weder UI noch KI dürfen `PlayerSlot`, `Sequence` oder `TargetTick` frei wählen.
Sequenzen beginnen bei 1. Der Wert 0 und ein Überlauf von `uint32` sind
strukturelle Fehler; die Session wird nicht mit einer wiederverwendeten Sequenz
fortgesetzt.

Die Grenze wird kompilierseitig erzwungen (`CommandRecord`/`CommandBatch`
besitzen ausschließlich `internal`-Konstruktoren). Reflection kann sie
prinzipiell umgehen; das ist ein akzeptiertes Restrisiko, weil ein so
manipulierter Prozess nur sich selbst desynchronisiert — der Replay- und
Hash-Nachweis bleibt davon unberührt.

Der Byte-Intake des Ingress (`TryAcceptRecordBytes` /
`TryAcceptHistoricalRecordBytes`) ist öffentlich, weil lokale, Relay- und
Replay-Transporte ihn aufrufen. Jeder Aufruf durchläuft unabhängig vom
Aufrufer die vollständige strukturelle Validierung. Der TCP-Relay prüft davor
zusätzlich Absenderslot, Tickfolge, Dedupe und Kapazitäten. Für den
Replay-Import liegt die geforderte Fingerprint-Prüfung des Stroms beim
Aufrufer — der Ingress erzwingt alle übrigen strukturellen Regeln, kann die
Herkunftsprüfung aber nicht selbst leisten.

## 2. Kanonisches Record-Format

Alle Integer sind Little Endian. Ein Command besteht exakt aus:

| Feld | Typ | Bedeutung |
|---|---|---|
| `RecordLength` | `u16` | Bytes des gesamten Records |
| `EnqueueTick` | `u32` | Tick am Session-Ingress |
| `TargetTick` | `u32` | frühester Ausführungstick |
| `PlayerSlot` | `u8` | sessiongebundener aktiver Slot |
| `Sequence` | `u32` | monoton je Spieler |
| `CommandKind` | `u16` | stabiler Command-Katalog |
| `PayloadVersion` | `u8` | Version des konkreten Payloads |
| `PayloadLength` | `u16` | Payload-Bytes |
| `Payload` | Bytes | kanonischer, kind-spezifischer Inhalt |

`RecordLength` und `PayloadLength` müssen exakt zu den vorhandenen Bytes passen.
Payloads dürfen keine Floats, Strings, GUIDs, Dictionaries oder
laufzeitabhängigen Objektlayouts enthalten. Text- oder Definitionseingaben werden
vorher auf stabile numerische IDs aufgelöst.

Der Header ist exakt 20 Bytes. Verbindliche Grenzen:

| Grenze | Wert |
|---|---:|
| `MaxRecordBytes` | 4.096 |
| `MaxPayloadBytes` | 4.076 |
| `MaxBatchRecordsPerTick` | 256 |
| `MaxPendingRecords` | 1.024 |
| `MaxEntityIdsPerCommand` | 100 |

Ein lokaler Live-Record besitzt
`TargetTick=EnqueueTick+InputDelayTicks`; im kanonischen lokalen Defaultprofil
ist das `EnqueueTick+1`, der erlaubte Bereich bleibt 1 bis 60. Im
D-089-Netzprofil gilt dieselbe Formel mit dem vor Start bewiesenen festen Wert
aus diesem Bereich (Default 3). Replay-Import darf historische Zielticks nur
aus einem bereits fingerprint-geprüften Strom übernehmen.

## 3. Reihenfolge und Duplikate

Ein Batch wird nach `(TargetTick, PlayerSlot, Sequence)` sortiert. Der Ingress
dedupliziert auf `(PlayerSlot, Sequence)`:

- die byteidentische Wiederholung wird genau einmal akzeptiert;
- ein anderer Byteinhalt mit demselben Schlüssel ist ein deterministischer
  Konflikt und wird abgelehnt;
- eine bereits abgeschlossene Sequenz darf den Dedupe-Zustand nicht umgehen;
- Backpressure-Grenzen werden vor dem Versiegeln geprüft.

Der Dedupe- und Sequenzzustand ist autoritativ und wird in Snapshots
serialisiert.

Die Watermark-Dedupe abgeschlossener Sequenzen setzt verbindlich eine
zuverlässige, geordnete Zustellung je Spieler voraus. Sowohl
`LocalLoopbackTransport` als auch der D-089-TCP-Pfad erfüllen das. Ein späterer
ungeordneter Transport dürfte verspätete Sequenzen nicht still verwerfen,
sondern benötigte ein eigenes Lücken-Fehlermodell und eine neue Entscheidung.

## 4. Zwei Validierungsstufen

### Strukturell

Vor Aufnahme in den kanonischen Strom werden geprüft:

- Record-/Payload-Länge und Parsergrenzen,
- aktiver, sessiongebundener Slot,
- bekannte `CommandKind`-/`PayloadVersion`-Kombination,
- kanonische IDs, enum-Bereiche und sortierte Entity-Listen,
- erlaubtes Tickfenster,
- Sequenz-/Dedupe-Regeln und
- Queue-/Batch-Kapazität.

Ein struktureller Fehler wird abgelehnt und nicht aufgezeichnet.

### Zustandsabhängig

Am `TargetTick` werden beispielsweise Besitz, Sicht, Reichweite,
Voraussetzungen, Kosten, Cooldowns oder Zielzustand geprüft. Ein solcher Fehler:

- mutiert keinen Zustand,
- erzeugt ein deterministisches `CommandResult` und
- bleibt im Replay.

Damit bleibt der akzeptierte Byte-Strom auch bei Fehlbedienung vollständig
reproduzierbar.

## 5. MS-1-Command-Inventar v1

Das konkrete `CommandKind`-Register wird im Code numerisch eingefroren und muss
mindestens alle Aktionen des
[MVP-Inhaltsmanifests](../production/MVPContentManifest.md) abdecken:

- Move, Stop und AttackTarget;
- Harvest und ReturnCargo;
- PlaceBuilding, CancelConstruction, Repair und Sell;
- QueueUnit und CancelProduction;
- SetRallyPoint;
- InstallDefenseModule.

Pause, Unpause, Save und Load sind versionierte **Session-Aktionen** und keine
Records des kanonischen Simulationsstroms. Sie werden nur an einer
abgeschlossenen Tickgrenze ausgeführt. Pause verhindert weitere Kernel- und
KI-Ticks; Unpause bleibt im angehaltenen Host empfangbar. Save und Load
verwenden den abgeschlossenen Snapshot und dürfen keinen Tick künstlich
erzeugen.

`TickComplete` ist ebenfalls kein Command und keine Session-Aktion, sondern
ein reiner D-089-Transport-/Barrier-Frame. Er verändert weder das eingefrorene
v1-Register noch Replay-Resultcodes oder State-Hash.

Kamera, Selektion, UI-Skalierung, Rebinding und Client-Feedback sind keine
Simulations-Commands. Nicht aktivierte Fähigkeiten, Forschung, Capture, Luft,
Mauern und Superwaffen erhalten in Schema v1 keinen ausführbaren Payload.

## 6. Schema-Freeze und Tests

G1 friert Command-Schema v1 ein. Pflichtfälle:

1. Roundtrip für jeden aktivierten `CommandKind`;
2. Golden Bytes je Payload-Version;
3. ungültige Längen, unbekannte Kinds/Versionen und ungültige IDs;
4. umgeordnete Eingabe mit identischem sortierten Batch;
5. byteidentische Duplikate und konflikthafte Duplikate;
6. Sequenzüberlauf-/Replay-Angriffe;
7. Queue- und Batch-Backpressure;
8. zustandsabhängige Ablehnung ohne Mutation;
9. Snapshot/Restore mit ausstehenden Commands sowie
10. `TransportNotReady` vor Session-Aktion und Sequenzvergabe.

Das aktivierte Command-Inventar muss 100 % Testabdeckung besitzen.

## Offene Punkte

- Keine. Erweiterungen nach G1 benötigen eine neue Payload-Version oder neue
  `CommandKind`-ID sowie Kompatibilitäts- und Golden-Byte-Tests.

## Nächste Schritte

1. Das eingefrorene numerische Register und seine Golden Bytes unverändert
   halten.
2. UI-, KI- und Netzwerkadapter weiterhin ausschließlich über
   `CommandIntent` beziehungsweise den validierten Byte-Intake führen.
3. Readiness-, Dedupe-, Barrier- und Replay-Grenzen gemeinsam in der
   kanonischen Testsuite absichern.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-24 | Ingress-Autorität, Little-Endian-Envelope, Dedupe und Schema-v1-Tests gemäß D-057 festgelegt | Lead Technical Director / Lead Multiplayer Engineer |
| 1.1.0 | 2026-07-24 | Sequenz-, Record-, Batch- und Payload-Grenzen geschlossen sowie Session-Aktionen aus dem Sim-Commandstrom getrennt | Lead Technical Director / Lead Multiplayer Engineer |
| 1.1.1 | 2026-07-25 | Review-Klarstellungen ohne Vertragsänderung: Reflection-Restrisiko der kompilierseitigen Vertrauensgrenze, Vertrauensannahme des öffentlichen Byte-Intake samt caller-seitiger Fingerprint-Prüfung beim Replay-Import und Zustellungsannahme der Watermark-Dedupe als Post-MVP-Netzwerk-Anforderung | Lead Technical Director / Lead Multiplayer Engineer |
| 1.2.0 | 2026-08-07 | D-089-Netzprofil mit festem Delay 1–60, optionalem Submission-Readiness-Gate, TCP-Zustellungsannahme und `TickComplete` außerhalb des Commandregisters dokumentiert | Lead Technical Director / Lead Multiplayer Engineer |
