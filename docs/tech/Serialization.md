# Serialisierung

**Version:** 1.1.0 | **Status:** verbindlich für MS-1 – G0 aktiv | **Verantwortungsbereich:** Lead Technical Director | **Sprint:** 7

## Zweck

Definiert kanonische Bytes für State, Snapshots, Savegames und Replay-
Fingerprints. Derselbe Serializer läuft in Unity und SimRunner.

## Abhängigkeiten

- [SimulationCore.md](SimulationCore.md) – Zahlen, Hashdomänen, Fingerprint
- [GameState.md](GameState.md) – Block-/Feldinventar
- [Commands.md](Commands.md) – Command-Records
- [Savegames.md](Savegames.md) und [Replication.md](Replication.md)
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-057/D-058

## 1. Kanonische Kodierung

- Byteordnung: Little Endian.
- Integerbreiten sind explizit; kein nativer `int` im Dateivertrag.
- `SimFixed` schreibt seine signed `int32`-Rohbits.
- `SimAngle` schreibt `uint16`.
- Flags sind `u8` und nur 0/1 gültig.
- Enums besitzen festgeschriebene Integerbreite und definierte Werte.
- Jede Schema-Version schreibt `Major u16`, danach `Minor u16`.
- Arrays schreiben `Count u32`, dann Elemente in definierter Reihenfolge.
- Inaktive Kapazitätsslots werden nach Schema kanonisch nullgeschrieben.
- Keine Reflection-, Dictionary-, GUID-, String- oder Runtime-Layout-
  Serialisierung im autoritativen Payload.

Parser lesen Längen und Counts erst nach Bereichsprüfung und allokieren niemals
aus ungeprüften Werten.

## 2. Snapshot-Envelope

Ein Snapshot enthält:

1. Datei-/Schema-Kennung 1.0,
2. Payload-Länge,
3. vollständigen Match-Fingerprint,
4. geordnete State-Blöcke aus [GameState.md](GameState.md),
5. Blocklängen und Blockhashes,
6. Gesamt-State-Hash und
7. Datei-Hash.

State-/Definitions-/File-/Replay-Chain verwenden XXH64 Seed 0 mit den
ASCII-Präfixen `NOVA_STATE_V1`, `NOVA_DEFINITIONS_V1`, `NOVA_FILE_V1` und
`NOVA_REPLAY_CHAIN_V1`, jeweils unmittelbar gefolgt von einem Nullbyte
`0x00`. Hashbreite ist ausnahmslos `uint64`.

## 3. Fingerprint

Vor dem Payload-Parse werden verglichen:

- alle State-/Command-/Payload-/Snapshot-/AI-Sidecar-Schemata,
- `NumericModelId=Q16_16_V1`,
- 10 Hz und `XorShift128PlusV1`,
- `RulesHash64`, `DefinitionsHash64`, `MapHash64`,
- MatchConfig, acht Slots mit zwei aktiven Belegungen, Seed und
- Initial-State-Hash.

Replay verlangt nach G1 exakte Gleichheit. Ein Save darf nur über eine explizit
registrierte und getestete Migration abweichen.

## 4. Blockreihenfolge

Die Reihenfolge folgt exakt dem Root-Inventar:

1. Header/Fingerprint,
2. PRNG,
3. Allocator,
4. Match/Teams/Players,
5. Entities,
6. Orders/Movement/Path,
7. Combat/Projectiles,
8. Economy/Energy/Aetherium,
9. Construction/Production/Technology,
10. FoW/Environment/Victory,
11. pending Commands/Sequence/Dedupe/Results,
12. Deferred Queues/Cachemetadaten.

Unbekannte Pflichtblöcke sind Fehler. Optionale Blöcke sind in Schema 1.0
nicht vorgesehen.

## 5. Größen- und Fehlergrenzen

| Grenze | Wert |
|---|---:|
| unkomprimiertes MS-1-Ziel | ≤4 MiB |
| Parser-Hardcap | 64 MiB |
| Entity Count | ≤1.024 |
| reservierte Slots | 8 |
| aktive Slots | exakt 2 |

Falsche Länge, Hashfehler, Overflow, ungültiger Count, unbekanntes Schema oder
Fingerprint-Mismatch werden vor State-Mutation abgelehnt. Deserialisierung
erfolgt in einen temporären State; erst vollständiger Erfolg ersetzt den
laufenden Host.

## 6. Roundtrip und Fortsetzung

G1-Pflichten:

- `Serialize(Deserialize(bytes)) == bytes`;
- jede Blockfeldmutation ändert Block- und Gesamt-Hash;
- Restore und frischer Host setzen mindestens 1.000 Ticks mit gequeuten
  Commands byte-/hashidentisch fort;
- Windows x64 und macOS arm64 erzeugen über 10.000 Ticks identische
  Checkpoints und finale Snapshotbytes;
- Truncation, übergroße Längen, unbekannte Blöcke und Bitfehler werden
  deterministisch abgelehnt.

## 7. Kompatibilität

Vor G1 erfolgt einmalig ein vollständiger Formatreset. Prototyp-Saves,
-Replays, -Pakete und -Fixtures werden nicht migriert. Kanonische Schemata
beginnen bei 1.0.

Nach G1:

- Replay: keine Migration, exakter Fingerprint;
- Savegame: nur explizite `from → to`-Migration;
- jede Migration besitzt Golden Input/Output, Fehler- und
  Fortsetzungstests;
- keine stillen Defaultfelder oder Best-Effort-Ladevorgänge.

## Offene Punkte

- Kompression ist eine äußere Storage-Optimierung und wird erst nach Messung
  entschieden; kanonische unkomprimierte Bytes bleiben führend.

## Nächste Schritte

1. Golden Bytes und Blockhash-Tests in G1 erstellen.
2. Parser-Fuzz-/Grenztests gegen 64-MiB-Hardcap ausführen.
3. Serializerquellen zwischen Unity und SimRunner identisch halten.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Technical Director |
| 1.0.0 | 2026-07-24 | Kanonische Little-Endian-Blockserialisierung, XXH64-Domänen, Fingerprint, Limits und Kompatibilitätsreset D-057/D-058 festgelegt | Lead Technical Director |
| 1.1.0 | 2026-07-24 | Versions-/Count-Breiten und nullterminierte Hashdomänen bytegenau festgelegt | Lead Technical Director |
