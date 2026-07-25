# Modulübersicht

**Version:** 1.0.1 | **Status:** verbindlich für MS-1 – G0 aktiv | **Verantwortungsbereich:** Lead Technical Director | **Sprint:** 7

## Zweck

Ordnet MS-1-Verantwortung und State-Hoheit genau einem Modul zu. Post-MVP-
Module sind keine versteckten Abhängigkeiten.

## Abhängigkeiten

- [Architecture.md](Architecture.md) und
  [DependencyGraph.md](DependencyGraph.md)
- [GameState.md](GameState.md)
- [../production/MVPContentManifest.md](../production/MVPContentManifest.md)
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-056 bis
  D-058

## 1. Autoritative Simulationsmodule

| Reihenfolge | Modul | State-Hoheit |
|---:|---|---|
| 0 | `SimulationCore` | Tick, Fingerprint, PRNG, Entity-Allocator, Batch-/Result-Puffer |
| 1 | `Commands` | Sequenz, Dedupe, strukturell akzeptierte Records |
| 2 | `EconomyEnergy` | AE-Konten, Cargo, Energie, Low-Power |
| 3 | `Aetherium` | Mutterreserven, Sprouts, Regrowth, Spread, Overharvest, Terrainfolge |
| 4 | `Construction` | Platzierung, Baufortschritt, Reparatur/Verkauf |
| 5 | `ProductionTech` | Einheitenqueues und direkte T2-Freischaltung bei ResearchLab-Abschluss |
| 6 | `PathfindingMovement` | Orders, Flow-Cache-Metadaten, Position/Bewegung |
| 7 | `FogOfWar` | committed 3-State-Teamgrid auf jedem zweiten Tick |
| 8 | `CombatProjectiles` | Ziellegalität, Waffen, Projektile, Schaden |
| 9 | `MatchVictory` | Elimination, Reveal-Timer, Ergebnis und Endtick |
| 10 | `SnapshotsResults` | deterministische CommandResults und gefilterte Read-Modelle |

Jedes Modul besitzt seinen serialisierbaren State und mutiert nur in seiner
festen Tickphase. Modulkommunikation erfolgt über stabile IDs, Read-Views und
deterministische Queues, nicht über gegenseitige versteckte Setter.

## 2. Nicht autoritative Module

| Modul | Verantwortung |
|---|---|
| `Nova.AI` | liest committed TeamWorldView, pflegt versionierten Sidecar, erzeugt Intents |
| `MatchSession` | Slots, Fingerprint, Definitionen, CommandIngress, Pause/Unpause und Hostlebenszyklus |
| `LocalLoopbackTransport` | liefert versiegelte Batches lokal zurück |
| `PersistenceHost` | verbindet Sim-Snapshot und AI-Sidecar mit Slot-/Recovery-Policy |
| `PlayerSnapshotBuilder` | filtert committed State für UI/View |
| `Nova.Presentation` | Darstellung, Kamera, Audio/VFX, Interpolation |
| `Nova.UI` | HUD, Settings, Save/Load, Selektion, Intent-Erzeugung |
| `Nova.SimRunner` | headless Ausführung derselben Kernel-/AI-Quellen |

## 3. Bewusste Nicht-Module in MS-1

Es gibt keine autoritativen Module für:

- generische Fähigkeiten, Status, Kanäle oder Auren,
- Forschung/upgrades jenseits direkter T2-Freischaltung,
- Luft/Flak, Mauern, T3, Eliten, Superwaffen oder Drohnen,
- Capture, Neutrale, Brücken, Wetter/Hazards,
- Commander/Doktrinen, Kampagne oder Online.

Fraktionsidentität liegt in kanonischen Weapon-/Economy-Definitionen, nicht in
einem generischen Effektframework.

## 4. Definitionen

Die Produktionsdefinitionen entsprechen exakt
[`mvp-v1.json`](../../quality/content/mvp-v1.json). Authoring-SOs werden beim
Matchstart validiert, sortiert und in einen kanonischen
`DefinitionSnapshot` überführt. Nicht manifestierte IDs sind im MS-1-Build
nicht aktivierbar.

## 5. State- und Cache-Regel

Alle zukunftsrelevanten Daten stehen in [GameState.md](GameState.md).
Abgeleitete Caches können fehlen, wenn Rebuild-Parität bewiesen ist.
Flow-Request-, Referenz- und Eviction-Metadaten sind nicht beliebig abgeleitet
und bleiben autoritativ.

## Offene Punkte

- Post-MVP-Module werden erst mit neuer D-ID und neuem State-/Commandvertrag
  ergänzt.

## Nächste Schritte

1. G0-Assembly-Checks gegen diese Eigentumsmatrix implementieren.
2. G1-State pro Modul mit Snapshot-/Hash-Sensitivitätstests versehen.
3. G2–G3 nur in der festgelegten Reihenfolge integrieren.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Technical Director |
| 0.3.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043): komplette Angleichung an die kanonische Assembly-Topologie – Bridge-Modul auf `Nova.Gameplay` (statt `Nova.Game`) korrigiert; Burst-Verweise auf `Nova.Simulation.Burst` (statt `.Jobs`); KI aus dem Sim-Modul (alt 1.9) herausgelöst und als eigene Unity-freie Module `Nova.AI`/`Nova.AI.Data` in neuem Abschnitt 2 geführt (keine Direktreferenzen auf Economy/Production/Research, Client-Zugriff nur über Commands/`IAiWorldView`); GameDatabase auf Sub-Registries + generierten `GameDatabaseMaster` umgestellt (D-049); Tools-Abschnitt in `Nova.Editor` + `Nova.BuildTools` aufgeteilt (statt `Nova.Tools`); SimRunner referenziert zusätzlich `Nova.AI` (KI-vs-KI-Headless, D-036/D-043); alle Abschnitte neu nummeriert | Lead Technical Director |
| 1.0.0 | 2026-07-24 | Module auf Closed-Core MS-1, feste Tickordnung und klare State-Hoheit rebaselined | Lead Technical Director |
| 1.0.1 | 2026-07-24 | Pause dem Session-Host und Victory den Reveal-/Ergebnis-State eindeutig zugeordnet | Lead Technical Director |
