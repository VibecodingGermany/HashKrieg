# Kanonischer Simulationskern

**Version:** 1.1.1 | **Status:** verbindlich für MS-1 – G0 aktiv | **Verantwortungsbereich:** Lead Technical Director | **Sprint:** 7

## Zweck

Dieses Dokument definiert den einzigen autoritativen Rechen-, Zustands-, Hash-,
Snapshot- und Replay-Vertrag für Project Nova. Es ist die Implementierungsgrenze
für `Nova.Core`, `Nova.Simulation`, `Nova.AI`, den Unity-Host und
`Nova.SimRunner`. Der Vertrag ist noch nicht nachgewiesen; G1 bleibt offen.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-057 bis D-061
- [../production/MVPRecoveryPlan.md](../production/MVPRecoveryPlan.md) – G0/G1
- [Commands.md](Commands.md) – kanonischer Befehlsstrom
- [GameState.md](GameState.md), [Serialization.md](Serialization.md) und
  [Savegames.md](Savegames.md) – abgeleitete Detailverträge
- [FogOfWar.md](FogOfWar.md) und [Pathfinding.md](Pathfinding.md)
- [`../../quality/schemas/GateEvidence.schema.json`](../../quality/schemas/GateEvidence.schema.json)

## 1. Numerisches Modell

| Bestandteil | Verbindliche Repräsentation |
|---|---|
| Skalar | `SimFixed`, signed Q16.16 auf `int32`; `OneRaw=65536`; Bereich `[-32768, 32767.9999847412109375]` |
| Zwischenprodukte | `int64` |
| Rundung | nearest, ties-to-even |
| Welt → Grid | floor, auch für negative Werte |
| Winkel | `SimAngle` auf `uint16`; Wrap ist ausschließlich hier erlaubt |
| Tick | `uint32`, 10 Hz |
| Dauern | ganze Ticks |
| Spieler / Team | `uint8` |
| Definition | `DefinitionId uint16`; Rohwert 0 ungültig |
| Entity | `EntityId uint32`; Bits 0–9 Index, Bits 10–31 Generation; Rohwert 0 ungültig |
| PRNG | `XorShift128PlusV1`, zwei `uint64`-Wörter |
| Hash | `uint64` |

Autoritative Berechnungen dürfen weder `float`, `double`, Unity-Mathematik noch
plattformabhängige Physik verwenden. Überlauf, Division durch null, ungültige
Konvertierung und Werte außerhalb des definierten Bereichs sind deterministische,
geprüfte Fehler. Sättigung und stilles Wraparound sind verboten. Numerische
Toleranzen sind ausschließlich für nicht autoritative Diagnostik zulässig; ein
Hash-Abstand ist niemals ein Gültigkeitskriterium.

Der Entity-Index liegt zwischen 0 und 1.023, die Generation zwischen 1 und
4.194.303. Neue Slots beginnen mit Generation 1. Die serialisierte Free-List
vergibt freie Indizes in aufsteigender Reihenfolge; ein Generationsüberlauf
beendet die Simulation mit einem deterministischen Fault statt eine alte ID
wieder gültig zu machen.

## 2. Zeit und Ausführungsordnung

Die Simulation läuft synchron mit 10 Hz. Der Host liefert ausschließlich versiegelte
`CommandBatch`-Objekte. Die verbindliche MS-1-Reihenfolge lautet:

1. fälligen `CommandBatch` validieren und anwenden,
2. Economy und Energie,
3. Aetherium,
4. Construction und Production,
5. T2-Freischaltung,
6. Pathfinding und Movement,
7. FoW-Recompute auf jedem zweiten Tick,
8. Combat und Projectiles,
9. Match-/Sieglogik,
10. deterministische Ergebnis- und View-Snapshots erzeugen.

FoW liegt nach Movement und vor Combat. Dieselbe festgeschriebene Sicht wird für
Kampflegalität, KI, Player-Snapshot und Rendering verwendet. KI erzeugt Intents
außerhalb dieser Modulliste und ist keine Abhängigkeit von `Nova.Simulation`.

## 3. Autoritativer Zustand

Der kanonische Zustand enthält jede Information, die eine zukünftige
Simulationsentscheidung beeinflussen kann:

- Tick, vollständigen Match-Fingerprint und PRNG-Wörter;
- Entity-Allocator, Free-List und alle Generationen;
- Match-, Spieler-, Team- und Entity-State;
- Orders, Bewegung, Pathfinding, Combat und Projektile;
- Economy, Energie und vollständigen Aetherium-Zustand;
- Construction, Production und T2-Freischaltung;
- FoW und MS-1-Environment-State;
- ausstehende versiegelte Batches;
- Sequenz-, Dedupe- und Konfliktzustand sowie
- Pathfinding- und andere verzögerte Arbeitsqueues einschließlich
  zukunftsrelevanter Cache-Anforderungs-/Eviction-Metadaten.

Der Entity-Allocator ist Teil des Snapshots. Nach Restore muss deshalb nicht nur
der sichtbare Zustand, sondern auch die nächste ID-Vergabe identisch sein.

Abgeleitete Caches dürfen fehlen, wenn ein Test beweist, dass ihr Rebuild aus
kanonischem State dieselben Bytes und dieselbe Fortsetzung erzeugt. Der
Flow-Field-Cache ist nur teilweise abgeleitet: Referenzen, Anforderungsreihenfolge
und jede zukunftsrelevante LRU-/Eviction-Information werden serialisiert.

## 4. KI-Sidecar

`Nova.AI` ist ein versionierter Session-Sidecar:

- Es liest nur die festgeschriebene, teamgefilterte Weltansicht.
- Es erzeugt ausschließlich `CommandIntent`.
- Savegames enthalten den für eine identische Fortsetzung nötigen KI-Sidecar-
  Zustand und dessen Schema-Version.
- Replays zeichnen akzeptierte Human- **und** KI-Commands auf.
- Replay-Playback instanziiert oder wendet die KI nicht erneut an.
- Eine optionale Shadow-KI darf nur diagnostisch gegen den aufgezeichneten Strom
  vergleichen und ihn nie verändern.

`Nova.Simulation` darf `Nova.AI` nicht referenzieren.

## 5. Hash-Domänen

Alle kanonischen 64-Bit-Hashes verwenden **XXH64, Seed 0** und eine
domänenspezifische ASCII-Präfixfolge:

| Domäne | Präfix |
|---|---|
| Simulationszustand | ASCII `NOVA_STATE_V1`, danach Byte `0x00` |
| Definitionen | ASCII `NOVA_DEFINITIONS_V1`, danach Byte `0x00` |
| Datei/Block | ASCII `NOVA_FILE_V1`, danach Byte `0x00` |
| Replay-Kette | ASCII `NOVA_REPLAY_CHAIN_V1`, danach Byte `0x00` |

Jeder Hasher schreibt Feldkennungen, Längen und Werte in kanonischer
Little-Endian-Reihenfolge. Hashes werden nicht über Laufzeitobjekt-Layouts,
Reflection-Reihenfolge oder unbestimmte Containeriteration gebildet.

## 6. Match-Fingerprint

Snapshot, Replay und Command-Strom binden denselben Fingerprint:

- State-, Command-, Payload-, Snapshot- und Sidecar-Schema-Versionen;
- `NumericModelId = Q16_16_V1`;
- 10 Hz;
- `XorShift128PlusV1`;
- `RulesHash64`, `DefinitionsHash64` und `MapHash64`;
- Match-Konfiguration, acht reservierte Slots und die zwei aktiven Belegungen;
- Start-Seed und
- Hash des initialen Zustands.

Fehlt ein Bestandteil oder weicht er ab, startet die Wiedergabe nicht. Vor G1
gilt einmalig ein Kompatibilitätsreset: aktuelle Prototyp-Saves, -Replays,
-Pakete und -Fixtures werden nicht unterstützt; die kanonischen Schemata beginnen
bei 1.0. Nach G1 verlangt Replay exakte Fingerprint-Gleichheit. Savegame-Migration
ist nur als expliziter, getesteter Versionsschritt zulässig.

## 7. Snapshot und Fortsetzung

Das Binärformat ist kanonisch, Little Endian, versioniert und blockweise
gehasht. G1 verlangt:

1. Serialize → Deserialize → Serialize ergibt byteidentische Bytes.
2. Restore und ein frischer Host mit demselben Snapshot laufen mindestens
   1.000 Ticks mit bereits gequeuten Commands weiter und erzeugen pro Tick
   identische Hashes und finale Bytes.
3. Jede relevante State-Blockmutation verändert den zugehörigen Blockhash und
   den State-Hash.
4. Parser prüfen Längen und Kapazitäten vor Allokation.

Das unkomprimierte MS-1-Ziel ist höchstens 4 MiB; der Parser lehnt Dateien über
64 MiB vor dem Payload-Parse ab.

## 8. Replay

Ein Replay enthält Fingerprint, initialen Snapshot oder dessen eindeutige
Referenz, jeden akzeptierten Human-/KI-Command, jeden deterministischen
`CommandResult` und eine Hash-Kette. Zustandsabhängig erfolglose Befehle bleiben
im Strom. Strukturell ungültige Befehle erreichen ihn nicht.

Wiedergabe verwendet denselben Kernel und dieselben Quellen wie der Live-Host.
Unity und SimRunner dürfen keine nachgebauten Serializer oder Sim-Kopien besitzen.

## 9. Plattform- und Assembly-Parität

Unity und `Nova.SimRunner` kompilieren dieselben `Nova.Core`-,
`Nova.Simulation`- und gegebenenfalls `Nova.AI`-Quellen mit denselben
determinismusrelevanten Defines. Getrennte, versionierte Projekte sind zulässig;
kopierte Logik ist es nicht. Der verbindliche Determinismus-Define heißt
`NOVA_FIXED_POINT`; das Unity-Projekt und
`tools/Nova.SimRunner/Nova.SimRunner.csproj` setzen ihn identisch.

G1 verlangt auf Windows x64 und macOS arm64:

- exakte State-Hashes **und**
- exakte finale Snapshot-Bytes

über 10.000 Ticks desselben Replays. Der ausgelieferte MS-1-Pfad ist Managed.
Burst bleibt deaktiviert, bis eine spätere Entscheidung exakte Feld-, Hash- und
Byteparität nachweist.

## 10. Kapazitäten

| Ressource | MS-1-Vertrag |
|---|---:|
| reservierte Spieler-Slots | 8 |
| aktive Slots | exakt 2 |
| Grid | 128 × 128 bei 1 m |
| Produktions-Einheitenlimit | 100 gesamt |
| synthetisches Lastfixture | bis 500 Agenten |
| Entity Store | 1.024 Entities |
| Flow-Field-Cache | höchstens 32 Einträge und 8 MiB |
| Command-Records je Tick | höchstens 256 |
| ausstehende Command-Records | höchstens 1.024 |

Referenzierte Flow Fields werden nie eviktiert. Unter Einträgen mit
Referenzzähler null entscheidet eine deterministische LRU-Reihenfolge mit stabilem
Tie-Break.

## Offene Punkte

- Keine offenen Vertragsentscheidungen für G1. Implementierungsbefunde, die eine
  dieser Festlegungen ändern würden, benötigen eine neue D-ID.
- Q-018 und Q-019 liegen außerhalb des Simulationskerns.

## Nächste Schritte

1. In G0 Assembly- und Projektgrenzen reproduzierbar herstellen.
2. In G1 Typen, Serializer, Hashes, Commands und Cross-Plattform-Fixtures
   testgetrieben implementieren.
3. Erst nach bestandenem G1 Gameplay-Systeme aufbauen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-24 | Kanonischen Fixed-Point-, State-, Hash-, Snapshot- und Replay-Vertrag gemäß D-057/D-058 festgelegt | Lead Technical Director |
| 1.1.0 | 2026-07-24 | Zahlenbereich, ID-Bitlayout, Nullterminierung der Hashdomänen und Command-Kappen bytegenau geschlossen | Lead Technical Director |
| 1.1.1 | 2026-07-25 | `NOVA_FIXED_POINT` als verbindlichen Determinismus-Define-Namen für Unity und SimRunner in §9 festgelegt | Lead Technical Director |
