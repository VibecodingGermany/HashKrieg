# Game State

**Version:** 1.1.0 | **Status:** verbindlich für MS-1 – G0 aktiv | **Verantwortungsbereich:** Lead Technical Director | **Sprint:** 7

## Zweck

Inventarisiert jeden autoritativen Zustand, der die Zukunft einer MS-1-
Simulation beeinflussen kann. Ein Feld außerhalb dieses Inventars darf nicht
stillschweigend autoritativ werden.

## Abhängigkeiten

- [SimulationCore.md](SimulationCore.md) – numerisches Modell und Hashdomänen
- [Commands.md](Commands.md) – Batch-, Sequenz- und Ergebniszustand
- [Serialization.md](Serialization.md) – kanonische Byte-Reihenfolge
- [FogOfWar.md](FogOfWar.md) und [Pathfinding.md](Pathfinding.md)
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-057/D-058

## 1. Primitive Sim-Typen

| Bedeutung | Typ |
|---|---|
| skalarer Sim-Wert | `SimFixed` Q16.16 auf `int32` |
| Winkel | `SimAngle uint16` |
| Tick/Dauer | `uint32` / ganze Ticks |
| Player/Team | `uint8` |
| Entity | `EntityId uint32`: Bits 0–9 Index, Bits 10–31 Generation; 0 ungültig |
| Definition | `DefinitionId uint16`; 0 ungültig |
| Hash | `uint64` |
| Flag | kanonisches `u8` 0 oder 1 |

Autoritative rohe `float`, `double`, Strings, GUIDs, Dictionaries und
Unity-Typen sind verboten. Sammlungen besitzen feste Kapazität, Count und
stabile Iterationsreihenfolge.

## 2. Root-Inventar

`SimState` enthält in dieser kanonischen Reihenfolge:

1. `StateHeader`: Schema, Tick, vollständiger Fingerprint;
2. `RandomState`: beide `XorShift128PlusV1`-Wörter;
3. `EntityAllocatorState`: nächste/aktive Indizes, Free-List,
   Generationen und Belegungsbits;
4. `MatchState`: Phase, Ergebnis, Endtick, Reveal-Timer und aktive
   Slotbelegung; Pause gehört zum nicht autoritativen `SessionState`;
5. `TeamState[8]` und `PlayerState[8]` mit reservierten, aber nur zwei aktiven
   Slots;
6. `EntityState[1024]`;
7. Systemblöcke in §3;
8. ausstehende Command-Batches, Sequenz-/Dedupe-/Result-State;
9. verzögerte Arbeitsqueues und zukunftsrelevante Cachemetadaten.

Reservierte inaktive Slots werden kanonisch nullgeschrieben und bleiben im
Fingerprint eindeutig inaktiv.

## 3. Systemzustände

| Block | Pflichtinhalt |
|---|---|
| Orders | aktuelle Order, Queue, Ziel-IDs/-Zellen, Fortschritt |
| Movement | Position, Richtung, Geschwindigkeit, Footprint, Bewegungsphase |
| Pathfinding | aktive Zielcluster, Referenzen, Request-/Build-Queue, deterministische LRU-/Eviction-Metadaten |
| Combat | Ziel, Waffenzyklus, Cooldowns, Aggressionszustand soweit im MS-1-Manifest aktiv |
| Projectiles | Owner, Definition, Position/Richtung, Ziel, Restdauer, Schaden |
| Economy | AE-Konto, Cargo je Harvester, Ablade-/Dockzustand |
| Energy | Erzeugung, Verbrauch, Low-Power und betroffene Systemflags |
| Aetherium | Mutterreserve, Sprouts, Regrowth, Spread, Terrainfolge, Overharvest-Stufe/-Akkumulator, Warnzustand |
| Construction | Bauplatz, Definition, Fortschritt, Builder/Owner, Reparatur/Verkauf |
| Production | Queue je Produktionsgebäude, Kostenbindung, Fortschritt, Rally |
| Technology | T1/T2; T2 durch fertiges ResearchLab, keine Forschungsqueue |
| FogOfWar | drei Zustände je Team/Zelle, nächster Recompute-Tick |
| Environment | nur Aetherium-veränderte Zellen; keine sonstige Zerstörung |
| Victory | lebende Units/Buildings je Slot, 600-Tick-Reveal-Timer, Reveal-Flag, Ergebnisgrund, Gewinner-/Verlierer-Slot und Endtick |

Jeder Block hat einen eigenen Schema-Identifier und Blockhash. Eine Mutation
jeder zukunftsrelevanten Feldklasse muss den Block- und Gesamt-State-Hash
ändern.

## 4. Entity State

Ein belegter Entity-Slot enthält:

- ID/Generation, Definition, Owner/Team und Lebenszyklus;
- Transform/Footprint;
- Health/Armor;
- optional aktivierte Modulkomponenten aus §3 mit expliziten Presence-Bits.

Die Komponentenanordnung hängt nicht von Laufzeit-Reflection ab. Zerstören gibt
den Index deterministisch an die serialisierte Free-List zurück. Restore muss
dieselbe nächste Entity-ID erzeugen wie ein nicht unterbrochener Host.

Entity-Indizes liegen zwischen 0 und 1.023, Generationen zwischen 1 und
4.194.303. Neue Slots beginnen mit Generation 1; die Free-List vergibt den
kleinsten freien Index zuerst. Generationsüberlauf ist ein deterministischer
Fault.

## 5. Command- und Queue-State

Der Snapshot enthält:

- alle akzeptierten, noch nicht ausgeführten versiegelten Records;
- letzte/erwartete Sequenz pro reserviertem Spieler;
- Dedupe-Fenster einschließlich Byte-Fingerprint;
- deterministische `CommandResult`-Queue und
- jede Path-/Deferred-Queue, deren Reihenfolge ein späteres Ergebnis ändert.

Strukturell abgelehnte Records gehören nicht in den State. Zustandsabhängig
fehlgeschlagene Records bleiben im Replay-/Resultstrom.

## 6. FoW und gefilterte Views

Der committed FoW-State ist autoritativ. `PlayerSnapshot` und `TeamWorldView`
sind abgeleitete, schreibgeschützte Produkte desselben Ticks. Sie werden nicht
zurück in `SimState` gemischt. Verborgene Gegnerdaten dürfen in ihnen nicht
enthalten sein.

## 7. KI-Sidecar

KI-Zustand ist **nicht** Teil von `SimState` und keine Dependency von
`Nova.Simulation`. Der Session-Host speichert einen versionierten `AiSidecar`
mit allem, was die identische Save-Fortsetzung braucht: Profil-/Schema-ID,
interne Plan-/Timerzustände, eigenes PRNG falls verwendet und letzter
consumierter View-Tick.

Replay-Playback verwendet aufgezeichnete KI-Commands und keinen Sidecar.

## 8. Bewusste Ausschlüsse

Nicht im autoritativen MS-1-State:

- Pause/Unpause des Session-Hosts, Kamera, Selektion, UI-Layout, Audio/VFX
  und Client-Feedback;
- generische Ability-/Status-/Channel-/Aura-Strukturen;
- Forschung/upgrades jenseits des T2-Flags;
- Post-MVP-Luft-, T3-, Elite-, Capture-, Neutral-, Wetter- oder Online-State;
- ausschließlich neu aufbaubare Präsentationscaches.

## 9. Cache-Regel

Ein Cache darf aus dem Snapshot fehlen, wenn ein G1-Test aus denselben
kanonischen Eingaben:

1. identische Cachebytes oder identisches deterministisches Verhalten aufbaut,
2. über mindestens 1.000 Fortsetzungsticks identische State-Hashes liefert und
3. keine zukunftsrelevante Request-/Eviction-Reihenfolge verliert.

Flow-Referenzen und Eviction-Metadaten erfüllen Punkt 3 nicht automatisch und
werden daher serialisiert.

## 10. Kapazitäten

| Store | Hardcap |
|---|---:|
| reservierte Slots | 8 |
| aktive Slots | 2 |
| Entities | 1.024 |
| Produktionseinheiten | 100 |
| Grid | 128×128 |
| Flow Fields | 32 / 8 MiB |

Kapazitätsüberschreitung erzeugt einen deterministischen Fehler und keine
dynamische Vergrößerung.

## Offene Punkte

- Keine für G1. Neue autoritative Felder benötigen Schema-Bump,
  Hashsensitivitätstest und gegebenenfalls Migration.

## Nächste Schritte

1. Root-/Block-Schemata in G1 als Golden Bytes einfrieren.
2. Jede Feldklasse mit Mutation-/Hash-Test abdecken.
3. Save/Restore mit Allocator, Queues und KI-Sidecar fortsetzen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Technical Director |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings): Effekt-/Fähigkeiten-State, Environment-State, MatchSettings, Munition/Buchten/Transport, Elite-/Unit-Counter, SuperweaponState, Neutrale-State, Ausbaustufe 1–3, Keim-Reifung | Lead Technical Director |
| 0.2.1 | 2026-07-21 | Fix F-18 (GDD↔TDD): `SupplyUsed`/`SupplyCap` aus `PlayerState` entfernt (D-021 verbietet Supply-/Pop-System); Begrenzung läuft über `MatchState.GlobalUnitCount` (Deckel 600, D-048) und `EliteCounts` (D-015) | Lead Technical Director |
| 1.0.0 | 2026-07-24 | Autoritatives MS-1-Inventar auf Q16.16, vollständige Queues/Allocator, AI-Sidecar und D-058-Kappen rebaselined | Lead Technical Director |
| 1.1.0 | 2026-07-24 | ID-Bitlayout, Session-Pause und vollständigen Victory-/Reveal-State geschlossen | Lead Technical Director |
