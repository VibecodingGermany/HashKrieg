# Replication, Replay und Command-Strom

**Version:** 1.1.0 | **Status:** verbindlich für MS-1 – G0 aktiv | **Verantwortungsbereich:** Lead Multiplayer Engineer / Lead Technical Director | **Sprint:** 7

## Zweck

Definiert den in MS-1 lokal replizierten Command-Strom, Replay und
Hash-Kette. Online-Transport ist Post-MVP; der lokale Pfad darf ihn aber nicht
durch direkte Mutation verbauen.

## Abhängigkeiten

- [Commands.md](Commands.md) – kanonische Records und Ingress
- [SimulationCore.md](SimulationCore.md) – Fingerprint/Hashes/Replay
- [Serialization.md](Serialization.md) und [Savegames.md](Savegames.md)
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-057/D-061

## 1. MS-1-Transport

`LocalLoopbackTransport` ist die einzige produktive Implementierung. Es
transportiert versiegelte `CommandBatch`-Objekte aus dem Session-Ingress zum
Kernel. Es darf:

- Records nicht neu serialisieren,
- Reihenfolge/Bytes nicht ändern,
- Player/Sequence/TargetTick nicht neu vergeben und
- keinen State direkt setzen.

Der fingerprinted MS-1-Wert `InputDelayTicks` ist exakt 1.

## 2. Replizierter Inhalt

| Inhalt | MS-1 |
|---|---|
| akzeptierte Human-Commands | vollständig |
| akzeptierte AI-Commands | vollständig |
| deterministische CommandResults | vollständig |
| Fingerprint/Initial-State | vollständig |
| State-/Replay-Hash-Checkpoints | vollständig |
| Presentation/Input-Rohdaten | nie |
| laufender Entity-State-Sync | nie |

Zustandsabhängig fehlgeschlagene akzeptierte Commands bleiben im Strom.
Strukturell ungültige Records erreichen ihn nicht.

## 3. Replay-Envelope

Ein Replay enthält:

1. Replay-Schema 1.0,
2. vollständigen Match-Fingerprint,
3. initialen Snapshot oder eindeutige, hashgebundene Referenz,
4. chronologische kanonische CommandBatches,
5. deterministische CommandResults,
6. Checkpoints mit State-Hash und
7. Replay-Chain-Hash.

Replay-Ketten verwenden XXH64 Seed 0 mit den ASCII-Bytes
`NOVA_REPLAY_CHAIN_V1`, gefolgt von `0x00`. State,
Definitionen und Datei verwenden ihre getrennten Präfixe aus
[SimulationCore.md](SimulationCore.md).

## 4. AI-Regel

Replay zeichnet den tatsächlich akzeptierten AI-Commandstrom auf. Playback
instanziiert keine KI und lädt keinen AI-Sidecar. Eine Shadow-KI ist optional,
diagnostisch und schreibgeschützt.

Savegames sind anders: Sie speichern den aktuellen Sim-Snapshot plus
versionierten AI-Sidecar, weil die KI nach dem Laden neue Commands erzeugen
muss.

## 5. Playback

Playback:

- prüft Fingerprint vor Ausführung,
- nutzt denselben Kernel/Serializer wie Live,
- akzeptiert keine nachträgliche Recordänderung,
- prüft Checkpoint- und finale Bytes und
- bricht bei Mismatch mit erstem betroffenen Tick/Block ab.

Nach G1 gilt exakter Fingerprint. Replays werden nicht migriert.

## 6. Plattformnachweis

Dasselbe feste Replay läuft über 10.000 Ticks auf Windows x64 und macOS arm64.
Pass verlangt exakte State-Hashes und finale Snapshotbytes. Hashdistanz oder
numerische Toleranz ist kein Kriterium.

## 7. Post-MVP-Grenze

Online-Relay, Reconnect, Observer, Delay-Anpassung und serverseitige
Arbitration sind nicht Teil von MS-1. D-046 bleibt Zielinput, aber eine
Aktivierung benötigt nach G5 einen neuen Transport-/Security-Vertrag.

Der lokale Command-Strom darf keine online-unserialisierbaren Payloads oder
hostlokalen IDs enthalten.

## Offene Punkte

- Online-Protokoll, Reconnect und Observer werden Post-MVP neu entschieden.

## Nächste Schritte

1. Replay-Golden-Bytes und Chain-Hash in G1 implementieren.
2. AI-Playback-ohne-AI in G3 beweisen.
3. Online-Erweiterung erst nach G5 planen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Multiplayer Engineer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings) | Lead Multiplayer Engineer |
| 1.0.0 | 2026-07-24 | Replikation auf lokalen kanonischen Commandstrom, exaktes Replay und klare Post-MVP-Transportgrenze rebaselined | Lead Multiplayer Engineer / Lead Technical Director |
| 1.1.0 | 2026-07-24 | Input-Delay und nullterminierte Replay-Hashdomäne bytegenau fixiert | Lead Multiplayer Engineer / Lead Technical Director |
