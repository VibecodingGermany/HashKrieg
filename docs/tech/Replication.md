# Replication, Replay und Command-Strom

**Version:** 1.2.0 | **Status:** verbindlich für lokalen Strom und D-089-1v1-Profil; manuelle Netzwerkabnahme offen | **Verantwortungsbereich:** Lead Multiplayer Engineer / Lead Technical Director | **Sprint:** 12

## Zweck

Definiert den lokal und im D-089-TCP-Profil replizierten Command-Strom, den
kanonischen Sim-Replay und die getrennte Relay-Aufzeichnung. Beide Pfade
verwenden dieselben kanonischen Command-Bytes und mutieren Zustand
ausschließlich über die Simulation.

## Abhängigkeiten

- [Commands.md](Commands.md) – kanonische Records und Ingress
- [SimulationCore.md](SimulationCore.md) – Fingerprint/Hashes/Replay
- [Serialization.md](Serialization.md) und [Savegames.md](Savegames.md)
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-057/D-061 und D-089
- [RelayServer.md](RelayServer.md) – Relay-Betrieb und Auslieferung

## 1. Transportprofile

`LocalLoopbackTransport` bleibt der lokale produktive Pfad. Er liefert jeden
serialisierten Command synchron zurück an dieselbe Ingress. Das D-089-
Netzprofil verwendet `RelayMatchClient` über geordnetes, zuverlässiges TCP;
fremde Records treten als rohe kanonische Bytes in dieselbe strukturelle
Ingress-Grenze ein. Beide Transporte dürfen:

- Records nicht neu serialisieren,
- Reihenfolge/Bytes nicht ändern,
- Player/Sequence/TargetTick nicht neu vergeben und
- keinen State direkt setzen.

Der kanonische fingerprinted lokale Defaultwert für `InputDelayTicks` ist 1;
`MatchConfig`/Loopback erlauben 1 bis 60. Der Relay bietet einen während der
Session festen Wert aus demselben Bereich an, standardmäßig 3. Vor `Start`
müssen Fingerprint, Seed, Delay, Definitionshash und Initialsnapshot beider
Peers übereinstimmen.

`TickComplete` gehört nicht zum Command-Strom: Es ist ein Transport-Frame mit
Slot, Zieltick und Record-Anzahl. Der Client markiert seine lokale Completion
selbst, nachdem seine lokalen Records in der Ingress liegen, und wartet nicht
auf ein Relay-Echo. Die Remote-Completion erreicht ihn erst nach exakter
Servervalidierung und Weiterleitung; nur für den Remote-Slot zählt der Barrier
angekommene Records gegen die Ankündigung. Bei aktivierter Aufzeichnung
persistiert der Relay den Tick erst nach bestätigter Completion beider Slots.
Fehlende Vollständigkeit erzeugt Stall, keinen synthetischen Leertick.

## 2. Replizierter Sim-Inhalt

| Inhalt | `ReplayFile` / lokale Client-Sim | D-089-Wire / `NOVAREC2` |
|---|---|---|
| akzeptierte Human-Commands | vollständig | als kanonische Record-Bytes vollständig |
| akzeptierte AI-Commands | vollständig, wenn AI konfiguriert | nicht Teil des aktuellen Mensch/Mensch-1v1 |
| deterministische `CommandResult`s | durch Client-Sim erzeugt und im `ReplayFile` geführt | nie auf dem Wire oder in `NOVAREC2`; entstehen erst in Client-Sim beziehungsweise Playback |
| Fingerprint/Initial-State | vollständig | Startbeweis und `NOVAREC2`-Header |
| State-Hash-Checkpoints | vollständig | alle 50 Ticks; gleiche Hashes als Checkpoint, Mismatch als Desync-Befund |
| Replay-Chain-Hash | vollständig | nie; `NOVAREC2` ist kein `ReplayFile` |
| `TickComplete` | nie | nur Transport-Barrier; der rohe Frame wird nicht als Command oder Replay-Record gespeichert |
| Presentation/Input-Rohdaten | nie | nie |
| laufender Entity-State-Sync | nie | nie |

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

### 3.1 Relay-Aufzeichnung `NOVAREC2`

Der kanonische `ReplayFile` bleibt die Autorität für Sim-Replays. Er enthält
deterministische Resultcodes, die erst die Simulation erzeugen kann. Der nicht
simulierende Relay darf solche Resultcodes nicht erfinden und schreibt deshalb
ein getrenntes Transportformat:

- lückenlose Tickframes einschließlich Ticks ohne Command;
- pro Slot serverseitig exakt bestätigte Record-Anzahlen, Dedupe und Caps;
- Fingerprint und Initialsnapshot aus dem geprüften Startbeweis;
- gleiche State-Hash-Checkpoints alle 50 Ticks;
- bei Desync beide Peer-Hashes, wobei der Diagnosehash genau einem Peer
  entsprechen muss;
- terminaler Footer mit Reason, terminalem Tick, letztem persistierten Tick
  und letztem Checkpoint-Tick;
- gemeinsamer 64-MiB-Höchstwert für Relay-Record und Client-Diagnostik;
- Schreiben nach `.partial` und atomare Veröffentlichung als `.novarec` nur
  nach vollständiger Versiegelung.

`NOVAREC2` führt weder `CommandResult`s noch einen Replay-Chain-Hash. Results
entstehen ausschließlich in der Client-Simulation beziehungsweise beim
Playback; sie werden nicht vom Relay erzeugt und nicht über den Wire
übertragen.

Der `NOVAREC2`-Reader prüft Struktur, Lückenlosigkeit, Grenzen und Footer. Das
engine-freie Playback führt die Records über historische Ingress und den
kanonischen Kernel aus, prüft gespeicherte Checkpoints und liefert den dabei
berechneten Endhash. Der Endhash ist nicht als Autorität im Footer gespeichert;
im A8-Soak wird der berechnete Playback-Wert gegen den Live-Endhash verglichen.

Clientseitige Desync-Diagnostik spoolt Records begrenzt auf Platte statt in
einer wachsenden Liste. Sie kann mehr als 65.536 Records tragen und publiziert
ebenfalls atomar. `NOVAREC1` und `NOVADIAG1` waren unveröffentlichte
Wegwerfformate; es gibt keine Migration.

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

## 7. D-089-Netzgrenze

Der TCP-Relay, feste Delay-Angebote, Barrier, 50-Tick-Hashvergleich,
`NOVAREC2` und Client-Diagnostik sind implementiert. Nicht enthalten sind
`MatchComplete`, Reconnect, Observer, adaptive Delay-Änderung, UDP und
serverseitige Ergebnisarbitration. D-046 bleibt historischer Zielinput für
einen späteren Trust-Anchor, ist aber durch diese Implementierung nicht
aktiviert.

Der lokale Command-Strom darf keine online-unserialisierbaren Payloads oder
hostlokalen IDs enthalten.

## Offene Punkte

- A8 Stufen 2–4 müssen den implementierten Strom in zwei Unity-Fenstern, im
  LAN und über den VPS abnehmen.
- `MatchComplete`, Reconnect, Observer und Ergebnisarbitration werden bei Bedarf
  separat entschieden; `NOVAREC2` behauptet diese Semantik nicht.

## Nächste Schritte

1. A8 Stufe 2 lokal spielen und erst danach LAN/VPS prüfen.
2. `NOVAREC2`-Playback und die kanonischen `ReplayFile`-Tests gemeinsam in der
   CI grün halten, ohne die beiden Formate zu vermischen.
3. Zusätzliche Netzfunktionen nur mit eigener Kompatibilitäts- und
   Security-Entscheidung ergänzen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Multiplayer Engineer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings) | Lead Multiplayer Engineer |
| 1.0.0 | 2026-07-24 | Replikation auf lokalen kanonischen Commandstrom, exaktes Replay und klare Post-MVP-Transportgrenze rebaselined | Lead Multiplayer Engineer / Lead Technical Director |
| 1.1.0 | 2026-07-24 | Input-Delay und nullterminierte Replay-Hashdomäne bytegenau fixiert | Lead Multiplayer Engineer / Lead Technical Director |
| 1.2.0 | 2026-08-07 | D-089-TCP-Profil, `TickComplete`-Barrier, getrenntes `NOVAREC2`-/Diagnostikformat und offene manuelle Abnahme dokumentiert | Lead Multiplayer Engineer / Lead Technical Director |
