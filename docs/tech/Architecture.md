# Gesamtarchitektur

**Version:** 1.4.0 | **Status:** verbindlich für MS-1 – G0-A1 Mergekandidat, G0-A2 offen | **Verantwortungsbereich:** Lead Technical Director | **Sprint:** 7

## Zweck

Definiert Schichten, Assembly-Grenzen, Hostfluss und Autorität für MS-1. Details
werden in fokussierten TDDs gepflegt; bei Abweichung führen D-056–D-066 und
[SimulationCore.md](SimulationCore.md).

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-043,
  D-056 bis D-066
- [SimulationCore.md](SimulationCore.md), [Commands.md](Commands.md) und
  [FogOfWar.md](FogOfWar.md)
- [DependencyGraph.md](DependencyGraph.md) und
  [ModuleOverview.md](ModuleOverview.md)
- [../production/MVPContentManifest.md](../production/MVPContentManifest.md)

## 1. Architekturregeln

1. `Nova.Simulation` ist autoritativ, Unity-frei und Q16.16-basiert.
2. Nur versiegelte `CommandBatch`-Objekte und feste Modulticks mutieren State.
3. UI und KI erzeugen ausschließlich `CommandIntent`.
4. Präsentation liest gefilterte Snapshots und besitzt keinen Schreibzugriff.
5. Statische Definitionen werden vor Matchstart in kanonische, Unity-freie
   Records überführt und durch `DefinitionsHash64` gebunden.
6. Singleplayer nutzt mit `LocalLoopbackTransport` denselben Ingress wie jeder
   spätere Transport.
7. Snapshot, Savegame und Replay verwenden denselben State-, Fingerprint- und
   Hashvertrag.
8. Plan, Dateianwesenheit und isolierte Tests sind keine Gate-Evidence.
9. Schema 1.2 prüft nur Evidence-Integrität. Gate-Autorität entsteht erst
   zweistufig aus einem subject-unabhängigen Trusted-Tool-Bundle nach G0-A;
   die Bundle-Änderung kann sich nicht selbst autorisieren.

## 2. Assembly-Topologie

| Ebene | Assembly | Abhängigkeiten | Verantwortung |
|---|---|---|---|
| Basis | `Nova.Core` | keine Engine | `SimFixed`, `Tick`, `EntityId`, stabile Result-/Buffer-Typen |
| Simulation | `Nova.Simulation` | `Nova.Core`, keine Engine | Kernel, Commands, State, Systeme, Snapshot, Replay |
| optionaler Fast Path | `Nova.Simulation.Burst` | Core/Simulation + Burst-Pakete | deaktiviert für MS-1; keine Autorität |
| KI | `Nova.AI` | Core + öffentliche Read-/Intent-Verträge | versionierter Session-Sidecar |
| Definitionen | `Nova.Data` / `Nova.AI.Data` | Core, Unity für SOs | authoring-only Definitionen |
| Host/Bridge | `Nova.Gameplay` | Core/Simulation/AI/Data | MatchSession, Ingress, Loopback, Composition Root |
| View | `Nova.Presentation` | Unity/URP | Kamera, Weltansicht, Interpolation, VFX/Audio |
| UI | `Nova.UI` | Unity UI | HUD, Settings, Pause, Save/Load, Intents |
| Runner | `Nova.SimRunner` | Core/Simulation/AI | headless Fixtures und Plattformnachweis |

`Nova.Simulation` referenziert weder KI, Gameplay, Data, Presentation, UI noch
Unity. Unity und SimRunner kompilieren dieselben Core-/Simulation-Quellen und
determinismusrelevanten Defines.

## 3. Laufzeitfluss

```text
Device/UI ─┐
           ├─> CommandIntent ─> MatchSession/CommandIngress
Nova.AI ───┘                         │
                          LocalLoopbackTransport
                                    │
                            sealed CommandBatch
                                    │
                           SimulationKernel 10 Hz
                                    │
             TeamWorldView / PlayerSnapshot / ViewEvents
                         ┌──────────┴──────────┐
                    Nova.UI             Nova.Presentation
```

`MatchSession` bindet aktive Slots, Seed, Map-/Definitionen und
`InputDelayTicks=1` in den Fingerprint. Der Ingress vergibt Sequenzen und
Zielticks. Client-Feedback darf sofort erscheinen, bezeichnet aber noch keinen
Sim-Erfolg.

## 4. Tickordnung

1. CommandBatch validieren/anwenden,
2. Economy/Energy,
3. Aetherium,
4. Construction/Production/T2,
5. Pathfinding/Movement,
6. FoW auf jedem zweiten Tick committen,
7. Combat/Projectiles,
8. Match-/Victory-State,
9. CommandResults und gefilterte Snapshots.

Combat, KI und Rendering verwenden dieselbe committed Team-Sicht. KI wird
außerhalb des Kernel-Ticks gegen den zuletzt freigegebenen Sidecar-Snapshot
ausgeführt und speist Intents wieder über den Session-Ingress ein.

## 5. Zustand und Persistence

Der autoritative Zustand ist vollständig in [GameState.md](GameState.md)
inventarisiert. [Serialization.md](Serialization.md) definiert kanonische Bytes;
[Savegames.md](Savegames.md) Benutzer-Slots/Recovery; [Replication.md](Replication.md)
den Command-/Replay-Strom.

Ein Replay wendet aufgezeichnete KI-Commands an und instanziiert KI nicht erneut.
Ein Save enthält den für identische Fortsetzung erforderlichen KI-Sidecar.

## 6. Kapazität

MS-1 reserviert acht Slots, aktiviert zwei, nutzt 128×128 Zellen, höchstens
100 Produktionseinheiten, 1.024 Entities und einen Flow-Cache von höchstens
32 Einträgen/8 MiB. 500 Agenten sind ausschließlich synthetische
Architekturreserve. Details:
[MemoryBudget.md](MemoryBudget.md), [Pathfinding.md](Pathfinding.md) und
[PerformanceBudget.md](PerformanceBudget.md).

## 7. Plattform

Unity ist exakt auf `6000.5.4f1`, Revision `d550df8bd089`, URP gepinnt.
Automatische Upgrades sind verboten. Der Managed-Pfad ist der einzige MS-1-
Auslieferungspfad; Burst bleibt aus, bis eine spätere D-ID exakte Feld-, Hash-
und Byteparität belegt.

## 8. Gate-Zuordnung

| Gate | Architektur-Exit |
|---|---|
| G0-A1 | Schema 1.3, Trusted-Checkout-Topologie, Umgebungsbindung und Gate-Runner als Integrity-Basis |
| G0-A2 | getrennte Subject-/Evidence-Carrier-/Trusted-Identitäten und vollständige append-only Receipt-Kette zweiphasig etabliert |
| G0-B / G0 | Projekte/asmdefs/Builds/Tests an einem nachfolgenden sauberen Subject reproduzierbar, Negative Controls grün |
| G1 | kanonischer Kern, Persistence und Plattformparität |
| G2 | Player-Kernloop ausschließlich über Session/Commands |
| G3 | gefilterte KI, Save/Replay-Fortsetzung |
| G4 | produktiver MS-1-Host/UI/Content |
| G5 | gleicher SHA vollständig abgenommen |

## Offene Punkte

- Online-Transport, Burst-Aktivierung und Worker-Tick sind Post-MVP und
  benötigen neue Entscheidungen.

## Nächste Schritte

1. G0-A1 ohne Gate-Fortschritt mergen.
2. G0-A2 als separaten zweiphasigen Receipt-Authorizer implementieren.
3. Assembly- und Quellenparität in G0-B am nachfolgenden sauberen Subject
   herstellen und dieses danach mit Schema 1.3 beweisen.
4. G1-Verträge test-first implementieren und direkte Mutations-/
   Engine-Kanten als Negative Controls absichern.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Technical Director |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043-Topologie): kanonische Assembly-Topologie (§2) inkl. `Nova.AI`/`Nova.AI.Data`; Offene Punkte bereinigt (Burst/Jobs-Frage via D-043/D-045/D-037 gelöst, `GameDatabase`-Sharding nach D-049 nachgetragen) | Lead Technical Director |
| 1.0.0 | 2026-07-24 | Architektur auf D-056–D-061, kanonischen G1-Kern und G0-offene Gate-Grenzen rebaselined | Lead Technical Director |
| 1.1.0 | 2026-07-24 | D-062-Evidence-Semantik als führenden Architektur-Nachweis ergänzt | Lead Technical Director |
| 1.2.0 | 2026-07-24 | D-063-Schema-1.2-/Check-/Trust-Vertrag als verbindliche Gate-Autorität ergänzt | Lead Technical Director |
| 1.3.0 | 2026-07-24 | D-064: Schema 1.2 auf Integrität begrenzt und G0-A-Trust-Bootstrap vor G0-B als Architektur-Exit ergänzt | Lead Technical Director |
| 1.4.0 | 2026-07-25 | D-066: G0-A1-Integrity und G0-A2-Receipt-Autorisierung als getrennte Architektur-Exits festgelegt | Lead Technical Director |
