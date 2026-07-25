# Changelog

Alle nennenswerten Änderungen an *Project Nova* werden in dieser Datei dokumentiert.

Das Format orientiert sich an [Keep a Changelog](https://keepachangelog.com/de/1.1.0/),
die Versionierung folgt (in der aktuellen Doku-Phase) dem Dokumentationsstand des Wikis
([docs/README.md](docs/README.md)). Kategorien: `Hinzugefügt`, `Geändert`, `Behoben`,
`Entfernt`, `Entschieden` (projektspezifisch für DecisionLog-Einträge).

> **Pflege-Regel:** Jede inhaltliche Änderung ergänzt einen Eintrag unter `[Unreleased]`.
> Beim Abschluss eines Sprints wird `[Unreleased]` in eine datierte Version überführt.
> Details siehe [AGENTS.md](AGENTS.md).

## [Unreleased]

### Geändert
- **Recovery-Baseline nach strengem Implementierungs-Audit:** MS-0 ist offen, das MVP ist nicht erreicht und Alpha hat nicht begonnen. Die bisherigen Sprint-7-Einträge belegen nur vorhandene Prototyp-Struktur, nicht fertige oder integrierte Features.
- [ImplementationAudit_2026-07-24.md](docs/production/ImplementationAudit_2026-07-24.md) dokumentiert Testfehler, Integrationslücken, fehlende Akzeptanznachweise und Planungswidersprüche am eingefrorenen Stand `460290e`.
- [MVPRecoveryPlan.md](docs/production/MVPRecoveryPlan.md) ersetzt pauschale Modul-Fertigmeldungen durch sequenzielle Gates G0–G5.
- Sprint-6-Abschluss, Sprint-7-GO, 445-PT-Verbindlichkeit sowie die ungültigen Schließungen Q-018/Q-019 wurden durch D-055 zurückgezogen; R-16 wurde reaktiviert und R-17 ergänzt.

### Entschieden
- **D-055:** Vorhandenen Code als Prototyp erhalten, Projektstatus auf Recovery zurücksetzen und Fortschritt ausschließlich über reproduzierbare Evidenz qualifizieren.

### Hinzugefügt
- **Sprint 7 (Implementierung / MS-0 Phase-0-Spike Kern-Simulation):**
  - **Assembly-Topologie & Engine-Entkopplung (`noEngineReferences: true`):** `Assets/_Project/Scripts/Core/Nova.Core.asmdef`, `Assets/_Project/Scripts/Simulation/Nova.Simulation.asmdef`, `Assets/_Project/Scripts/AI/Nova.AI.asmdef`.
  - **Core Simulation Types (`Nova.Core`):** `EntityId` (versioniertes Handle-Struct), `Tick` (Lockstep-Zähler), `INovaLogger` & `NullNovaLogger`, `SimRandom` (bit-genauer XorShift128+ PRNG).
  - **Simulations-Kernel (`Nova.Simulation`):** `CommandType`, `CommandEnvelope` (boxfreier Transport), `ICommandSink`, `ISimSystem`, `SimulationKernel` (Lockstep-Tick-Engine).
  - **Flow-Field Pathfinding (`Nova.Simulation.Pathfinding`):** `GridPos2D`, `Direction2D`, `CostField` (Kosten-Grid), `IntegrationField` (allokationsfreie Dijkstra-Welle), `FlowField` (8-Wege-Vektor-Feld), `PathfindingSystem`.
  - **Entitätsverwaltung & Bewegungs-System (`Nova.Simulation.State` & `Movement`):** `Transform2D`, `UnitState`, `EntityManager` (vorallokiertes Speicher-Array mit Index-Free-List-Recycling für 0-GC-Spawns), `MovementSystem` ($O(N)$ Spatial-Grid-Binning für flüssige Gruppen-Bewegung mit Sub-Millisekunden-Performanz).
  - **Unity-Gameplay-Brücke (`Nova.Gameplay`):** `MatchRunner` (MonoBehaviour 20-Hz-Akkumulator), `UnitViewManager` (60-FPS-View-Interpolation), `PathfindingTestBootstrap` (500 Einheiten Test-Runner).
  - **GameDatabase Sharding & Master Index (`Nova.Data` & `Nova.Editor`):** Category Sub-Registries (`UnitRegistrySO`, `BuildingRegistrySO`, `WeaponRegistrySO`), Aggregator `GameDatabaseMasterSO`, Editor Generator `GameDatabaseGenerator.cs` (Rebuild & Validierung) sowie Unity-freie `UnitDefinition` Structs für das Match-Setup gemäß D-049.
  - **Command Bus & Order System (`Nova.Simulation.Commands`):** Unboxed Command Transport via `CommandEnvelope`, `CommandProcessorSystem` (`ISimSystem` für `Move`, `Stop`, `AttackTarget`).
  - **Combat & Damage Pipeline (`Nova.Simulation.Combat`):** `WeaponDefinition`, `CombatSystem` (`ISimSystem` für Entfernungsprüfungen, Waffenfrequenzen, Schadensberechnungen und Entitäts-Zerstörung).
  - **State-Hash-/Replay-/Debug-Prototypen (`Nova.Simulation.State`, `Nova.Simulation.Replays`, `Nova.Presentation`):** unvollständiger FNV-1a-Hash, `ReplayBuffer` nur zur Aufzeichnung ohne Playback sowie `FlowFieldDebugView` (Scene View Gizmos); nicht als Lockstep-/Desync-Nachweis abgenommen.
  - **Wirtschafts- & Ressourcen-System (`Nova.Simulation.Economy`):** Phase 1 (Modul 9) - `PlayerEconomyState` Struct (16 Bytes, Aetherium-Guthaben & Energieraster), `ResourceHarvestingSystem` (Sammler-Entladung an Raffinerien) und `EnergyGridSystem` (Low-Power-Erkennung & -50 % Produktions-Strafen).
  - **Basisbau- & Bauplatz-System (`Nova.Simulation.Construction`):** Phase 1 (Modul 10) - `BuildingDefinition` Struct, `ConstructionGrid` (Zellbelegungs- und Bauzonenraster) und `ConstructionSystem` (`ISimSystem` für Gebäudeplatzierung, Bauzeit-Timer und automatische Energienetz-Registrierung bei Fertigstellung).
  - **Einheiten-Produktion & Tech-Tree (`Nova.Simulation.Production`):** Phase 1 (Modul 11) - `ProductionQueueSystem` (`ISimSystem` für Kasernen-/Fabrik-Queues, Bau-Timer & automatisches Spawnen im `EntityManager`) und `ResearchTreeSystem` (Tech-Tier-Freischaltungen [Tier 1, Tier 2] pro Spieler).
  - **Fog of War & Sichtweiten-Grid (`Nova.Simulation.Vision`):** Phase 1 (Modul 12) - `VisionGrid` (Verwaltet 3 diskrete Sichtzustände: `Unexplored`, `Explored`, `Visible` pro Spieler) und `VisionSystem` (`ISimSystem` für periodische Sichtweiten-Aktualisierung um Einheiten und Gebäude).
  - **Skirmish-KI Allianz & Legion (`Nova.AI`):** Phase 1 (Modul 13) - `AiFactionProfile` Struct (Prioritätsgewichtungen) und `SkirmishAiSystem` (`ISimSystem` in `Nova.AI` mit `noEngineReferences: true` für nutzenbasierte KI-Entscheidungen bzgl. Kraftwerksbau, Produktionsauslösung und Truppenbewegung).
  - **RTS-UI & Command-Card (`Nova.Presentation.UI`):** Phase 1 (Modul 14) - `SelectionManager` (Rechtecks-Kollisionsprüfungen für Drag-Box-Mehrfachauswahlen), `CommandCardPresenter` (Koppelung ausgewählter Einheiten an HUD-Buttons) und `MinimapRenderer` (Welt-zu-Minimap-Transformation).
  - **Asset-Integration MS-1 (`Nova.Data`):** Phase 1 (Modul 15) - `AssetMappingRegistrySO` (ScriptableObject-Mapping für 27 Einheiten- & 24 Gebäude-Assets aus Sprint 5 Audit) & GameDatabase-Lookup-Pipeline.
  - **3. Fraktion: Die Evolvierten (`Nova.Simulation.Factions`):** Phase 2 (Modul 16) - `BiomassGrid` (Verwaltet organische Biomasse-Zellen) und `EvolvedFactionSystem` (`ISimSystem` für passive Einheiten-Lebenspunkte-Regeneration [+2 HP / 0,5s] auf Biomasse).
  - **Commander- & Doktrinen-System (`Nova.Simulation.Commanders`):** Phase 2 (Modul 17) - `CommanderAbilityDefinition` Struct (Fähigkeiten-Parameter) und `CommanderSystem` (`ISimSystem` für passiven Energieaufbau [+1 Energy / 1,0s], Cooldowns & Bereichs-Effekte wie Orbital-Schläge).
  - **Command-Relay-Scaffolding (`Nova.Networking`):** Phase-2-Prototyp aus `CommandEnvelopeNetPacket` und In-Memory-`LockstepRelayBuffer`; aktuelle Serialisierung liefert 34 Bytes, während Test und Spezifikation 41 beziehungsweise 37 Bytes erwarten; kein UDP-Transport.
  - **Map- & Biom-Erweiterung (`Nova.Presentation.Maps`):** Phase 2 (Modul 19) - `MapBiomeType` Enum (`Desert`, `Snow`, `JungleIndustrial`) und `MapDefinitionSO` (ScriptableObject-Layouts für 1v1 / 2v2 Karten mit 2–4 Spawn-Punkten & Aetherium-Knoten).
  - **Headless SimRunner & Tests:** Standalone .NET 8 Konsolen-Executable `tools/Nova.SimRunner`, NUnit-EditMode-Testsuiten (`DeterministicSimTests`, `FlowFieldPathfindingTests`, `MovementSystemTests`, `MovementPerformanceTests`, `MatchRunnerTests`, `GameDatabaseTests`, `CommandSystemTests`, `CombatSystemTests`, `LockstepReplayTests`, `EconomySystemTests`, `ConstructionSystemTests`, `ProductionSystemTests`, `VisionSystemTests`, `SkirmishAiTests`, `SelectionManagerTests`, `AssetIntegrationTests`, `EvolvedFactionTests`, `CommanderSystemTests`, `LockstepRelayBufferTests`, `MapDefinitionTests`).
  - **Modulspezifikationen:** `MovementSystem_Spec.md`, `GameplayBridge_Spec.md`, `GameDatabase_Spec.md`, `CommandSystem_Spec.md`, `CombatSystem_Spec.md`, `LockstepReplay_Spec.md`, `EconomySystem_Spec.md`, `ConstructionSystem_Spec.md`, `ProductionSystem_Spec.md`, `VisionSystem_Spec.md`, `SkirmishAi_Spec.md`, `RtsUi_Spec.md`, `AssetIntegration_Spec.md`, `EvolvedFaction_Spec.md`, `CommanderSystem_Spec.md`, `LockstepRelay_Spec.md` und `MapExpansion_Spec.md` unter `docs/tech/modules/`.

## [0.7.0] – 2026-07-24 · Sprint 6: Produktionsplanung

### Hinzugefügt
- **Produktionsdokumentation in `docs/production/`:**
  [Milestones.md](docs/production/Milestones.md) (Meilensteine MS-0 bis MS-4 mit Qualitäts-Gates und Feature-Matrix) und [Roadmap.md](docs/production/Roadmap.md) (Produktions-Roadmap über 445 Personentage Gesamtaufwand, Phasenplan 2026–2028, Adressierung R-16 & R-13).
- **Sprint-6-Abschlussbericht** [Sprint06_Report.md](docs/production/sprints/Sprint06_Report.md) mit Freigabe von **Sprint 7 (Implementierung)**.

### Geändert
- `RiskAnalysis.md` (1.6.0): **R-16 (Zeit-/Kapazitätsmodell)** auf „mitigiert" gesenkt.
- `OpenQuestions.md` (1.8.0): **Q-018 (Preispunkt 29,99–39,99 €)** und **Q-019 (Opt-in Telemetrie)** geschlossen.
- `SprintPlanning.md` (1.6.0): Sprint 6 **abgeschlossen**, Sprint 7 (Implementierung) **bereit (GO)**.
- `docs/README.md` (0.7.0) und Root-`README.md` (Status-Board, Wiki-Version 0.7.0) nachgezogen.

## [0.6.0] – 2026-07-22 · Sprint 5: Asset Audit

### Hinzugefügt
- **Neuer Wiki-Bereich `docs/assets/` (Asset Audit)** mit vier Dokumenten:
  [ProcurementStrategy.md](docs/assets/ProcurementStrategy.md) (Beschaffungsstrategie B,
  BUY/MODIFY/BUILD-Rubrik, 4 Bewertungsdimensionen), [AssetRegister.md](docs/assets/AssetRegister.md)
  (Master-Register über 14 Kategorien mit kanonischen GDD-Zahlen, Lizenz, Kosten-/Aufwands-
  schätzung, Klassifikation), [Licenses.md](docs/assets/Licenses.md) (Lizenz-Register je Quelle)
  und [BuildBacklog.md](docs/assets/BuildBacklog.md) (priorisierter Eigenbau-Backlog ~110–180 PT).
- Sprint-5-Abschlussbericht [Sprint05_Report.md](docs/production/sprints/Sprint05_Report.md).

### Entschieden
- **D-053** Asset-Beschaffungsstrategie **B (Multi-Store-Mix mit Synty als Stil-Anker)**
  ratifiziert: menschliche Fraktionen/Biome/UI-Icons/Basis-Animationen = Kauf; Aetherium,
  komplette Evolvierten-Fraktion und Fraktions-Signaturen = MODIFY/BUILD. Leitplanken:
  URP-K.O.-Kriterium, keine RTS-Komplett-Frameworks, einheitlicher URP-Material-Standard,
  Lizenz-Register-Pflicht, keine Rohdaten im öffentlichen Repo.
- **D-054** **0 € Open-Source & KI-Asset-Pipeline (Inhaberentscheidung):** Ratifizierung einer
  reinen 0 € Open-Source-Beschaffung auf Basis freier CC0-Quellen (Quaternius, Kenney, Sonniss Audio),
  KI-3D/Textur-Generierung (Hunyuan3D, Meshy, Tripo, SD, Blender AI Addons / MCP Server) und
  Community-Kitbashing. **Q-035 (Asset-Budget-Obergrenze)** auf 0 € geschlossen. Alle Assets sind
  für das **öffentliche GitHub-Repository** freigegeben.

### Geändert
- `SprintPlanning.md` (1.4.0): Sprint 5 **abgeschlossen**, Sprint 6 (Produktionsplanung) **GO**;
  `docs/README.md` (0.6.0) und Root-`README.md` (Status-Board, Struktur, Version 0.6.0) nachgezogen.
- **Kanonische Asset-Zahlen gegen die historische `RTS_Asset_Pipeline.md` abgeglichen**
  (Gebäude 36 statt 54 = D-008, Karten 12 statt 10 = D-017, Elite 3→9 statt 15 = D-015,
  Neutrale ohne Händler = D-016, Marine gestrichen = D-013); nicht-destruktiver Korrekturhinweis
  an der Spitze der APL verweist auf das AssetRegister als führende Quelle.
- `RiskAnalysis.md` (1.5.0): **R-04** (visuelle Inkohärenz) und **R-07** (Lizenz-/Kostenfallen)
  auf „mitigiert" gesenkt.

### Behoben
- Root-`README.md` von veraltetem Stand (Version 0.4.0, „Sprint 4 in Arbeit", Status-Board
  „blockiert bis Sprint 3") auf den aktuellen Stand (0.6.0, Sprint 5 abgeschlossen) korrigiert.

## [0.5.0] – 2026-07-21 · Sprint 4: Architecture Review + Governance

### Hinzugefügt
- **Team-/Beitrags-Governance:** `CONTRIBUTING.md` (Team-Ablauf, PR-Pflicht, Release-Flow),
  PR-Vorlage und `CODEOWNERS` sowie ein günstiger, abhängigkeitsfreier CI-Check
  (`docs-check`, GitHub Actions) für tote interne Doku-Links.
- **Sprint 4 – Architecture Review abgeschlossen:** sechs adversariale Review-Berichte unter
  `docs/tech/review/` (Performance, Wartbarkeit & Prozess, Architektur-Kohärenz & Korrektheit,
  Multiplayer & Netcode, Skalierung & Systemgrenzen, GDD↔TDD-Konsistenz; im Wiki-Index verlinkt)
  und der Abschlussbericht [Sprint04_Report.md](docs/production/sprints/Sprint04_Report.md).

### Geändert
- **Repository auf öffentlich umgestellt**, Community-Projekt der Organisation `VibecodingGermany`.
- **`main` ist geschützt – Änderungen nur noch über Pull Requests** (Branch Protection:
  Review + grüne CI, keine direkten Pushes). `AGENTS.md` auf 2.0.0 (PR-only).
- **Sprint-4-Findings in 22 GDD-/TDD-Dokumente eingearbeitet** (Auflösung der Review-Widersprüche):
  Angriffsreichweiten metrisch → Grid-Felder (D-047, 1 Tile = 1 m); Weapons.md/Buildings.md/
  Vehicles.md je einzige führende Wertequelle; Alpha-Mutant-Doppeldefinition aufgelöst;
  **Assembly-Topologie kanonisiert (D-043) inkl. `ModuleOverview.md` vollständig nachgezogen**
  (KI als eigene Unity-freie Assembly `Nova.AI`); Managed-first (D-045); globaler
  600-Einheiten-Deckel (D-048); GameDatabase-Sharding (D-049); Post-Match-Re-Simulation als
  MP-Trust-Anchor (D-046); Quantum-Fallback gestrichen (D-051). `DocumentationStandard.md`
  1.1.0: Grundprinzip „Single Source of Truth für Werte" (D-047).
- **Risikoregister ehrlicher (RiskAnalysis 1.4.0):** neue reale Projektrisiken R-13 Bus-Faktor
  (W=hoch), R-14 ARM↔x86-Determinismus, R-15 KI-Code-Desync, R-16 Zeit-/Kapazität (W=hoch).

### Entschieden
- **D-043–D-052** (Sprint-4-Architecture-Review-Auflösungen, DecisionLog → 1.6.1): Assembly-
  Topologie (D-043), gestuftes Sim-Tick-Modell + Pflicht-Gate V5 (D-044), Managed-first (D-045),
  MP-Trust-Anchor (D-046), Werte-Single-Source (D-047), Skalierungs-Deckel (D-048), CI-Realismus
  + DB-Sharding (D-049), gestuftes Branching (D-050), Quantum-Fallback gestrichen (D-051),
  Referenzhardware (D-052).

## [0.4.0] – 2026-07-21 · Sprint 3: Technical Design

### Hinzugefügt
- Vollständiges Technical Design (23 Dokumente) unter `docs/tech/`: Architektur-Kern
  (Architecture, ModuleOverview, DependencyGraph, FolderStructure, CodingGuidelines,
  NamingConvention), Simulation & Daten (GameState, Serialization, Savegames),
  Multiplayer (Networking, Replication), Gameplay-Systeme (Pathfinding, AIArchitecture),
  Präsentation (Rendering, Lighting, AnimationSystem, InputSystem, AudioArchitecture),
  Budgets & Betrieb (PerformanceBudget, MemoryBudget, AssetBudget, Testing, Deployment).
- Sprint-3-Abschlussbericht ([docs/production/sprints/Sprint03_Report.md](docs/production/sprints/Sprint03_Report.md)).
- Repository-Grundgerüst: Root-`README.md`, `AGENTS.md` (Arbeitsregeln für KI-Agenten),
  `CHANGELOG.md`, `.gitignore` (macOS + Unity-vorbereitet); initiale Spiegelung zu GitHub.

### Entschieden
- 10 Architektur-Entscheidungen (D-033–D-042): determinismus-fähige Command-Simulation
  mit Lockstep-Relay-Zielbild (Q-013), Flow-Field-Pathfinding (Q-014), OOP+Burst statt
  DOTS (Q-015), Nova.SimRunner (Q-020), Burst/Managed-Doppelstruktur, Disconnect-Regel,
  Audio-Backend (FMOD ab Alpha), Forward/Realtime-Licht, Sentry, Sim-Tick-Budget ≤8 ms.

### Geändert
- Detail-Angleichungen GDD↔TDD (Disconnect-Regel final, Sim-Tick-Budget) in
  VictoryConditions, MultiplayerModes, PerformanceBudget, Networking.
- AGENTS.md Regel 1: Push nach jedem Versionsbump dauerhaft freigegeben (Anordnung
  Projektinhaber).

## [0.3.0] – 2026-07-21 · Sprint 2: Game Design

### Hinzugefügt
- Vollständiges Game Design Document (25 Dokumente): Vision, USP, Zielgruppe,
  CoreGameplay, GameLoop sowie das komplette GDD (Fraktionen, Gebäude, Einheiten,
  Wirtschaft, Forschung, Kampf-/Schadens-/Rüstungssystem, Karten, Biome, neutrale
  Einheiten, Fog of War, Commander-System, Multiplayer-Modi, Siegbedingungen,
  Balancing, Kampagne).
- Sprint-2-Abschlussbericht ([docs/production/sprints/Sprint02_Report.md](docs/production/sprints/Sprint02_Report.md)).

### Entschieden
- 26 Entscheidungen (D-007–D-032): Geschäftsmodell (Premium, Singleplayer-first),
  12 Gebäudetypen, Aetherium-Hybridwirtschaft, gezielte Zerstörbarkeit, Capture-System,
  Superwaffen-Limit, Fraktions-Sonderregeln, Kampagnen-Struktur u. a.

### Geändert
- Scope reduziert und beziffert (36 statt 54 Gebäude-Assets, 9 statt 15 Elite-Einheiten;
  Marine-/Drohnen-Inflation gestrichen) – Risiko R-01 teilentschärft.

## [0.2.0] – 2026-07-21 · Sprint 1: Research

### Hinzugefügt
- 10 Research-Dokumente unter `docs/research/`: RTS-Markt/Wettbewerb,
  Multiplayer-Simulation, Unity ECS/DOTS, Pathfinding, Fog of War, Open-Source-RTS-
  Architekturen, Unity Best Practices, KI-Architektur, Animation/Audio/UI,
  Asset-Store-Landschaft – jeweils mit ≥3 verglichenen Alternativen als
  Entscheidungsvorlagen.
- Sprint-1-Abschlussbericht.

## [0.1.0] – 2026-07-21 · Sprint 0: Projektinitialisierung

### Hinzugefügt
- Wiki-Grundgerüst und verbindlicher [Dokumentationsstandard](docs/meta/DocumentationStandard.md).
- Analyse-Dokumente: Wissensbasis, Inkonsistenz-Analyse, Gap-Analyse, Prioritätenliste.
- Produktions-Basis: Sprint-Planung, DecisionLog, OpenQuestions, RiskAnalysis.
- Übernahme der historischen Quelldokumente (`RTS_Game_Design_Outline.md`,
  `RTS_Technisches_Planungsdokument.md`, `RTS_Asset_Pipeline.md`).

[Unreleased]: https://github.com/VibecodingGermany/Project_Nova/compare/v0.7.0...HEAD
[0.7.0]: https://github.com/VibecodingGermany/Project_Nova/releases/tag/v0.7.0
[0.6.0]: https://github.com/VibecodingGermany/Project_Nova/releases/tag/v0.6.0
[0.5.0]: https://github.com/VibecodingGermany/Project_Nova/releases/tag/v0.5.0
[0.4.0]: https://github.com/VibecodingGermany/Project_Nova/releases/tag/v0.4.0
[0.3.0]: https://github.com/VibecodingGermany/Project_Nova/releases/tag/v0.3.0
[0.2.0]: https://github.com/VibecodingGermany/Project_Nova/releases/tag/v0.2.0
[0.1.0]: https://github.com/VibecodingGermany/Project_Nova/releases/tag/v0.1.0
