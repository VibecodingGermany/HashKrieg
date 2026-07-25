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

> **Dokumentationsstand 0.11.0 (unveröffentlicht):** Dieses Rebaseline ist ein
> Wiki-/Vertrags-Minor und kein Game-Release. Es wird kein Tag oder Release
> erzeugt; G0, MS-0 und MS-1 bleiben offen.

### Geändert
- **Planung vollständig auf D-056–D-064 rebaselined:** Sprint 7 bleibt bei
  offenem G0; MS-0 und MS-1 sind unerreicht. Milestones, SprintPlanning,
  Roadmap, RiskAnalysis und Sprint06_Report verwenden dieselbe Gate- und
  Evidence-Logik.
- Sprint 6 ist eindeutig als durch D-055 beendet/ersetzt dokumentiert;
  Sprint 7 ist gestartet, aber ausschließlich G0 ist zur Implementierung
  freigegeben.
- Aktive Technikverträge wurden auf Q16.16 ab G1, kanonische Commands/
  Snapshots/Replays, XXH64 Seed 0, feste MS-1-Kapazitäten, committed FoW und
  die getrennten 100-/500-Workloads angeglichen.
- Branch-Governance auf geschütztes `main`, kurze Topic-Branches,
  Squash/lineare Historie, kein dauerhafter Integrationsbranch und explizite
  Agentenautorität pro Commit-/Push-Aktion vereinheitlicht.
- Engine-Pin auf Unity `6000.5.4f1`, Revision `d550df8bd089`, URP korrigiert;
  automatische Editor-Upgrades sind ausgeschlossen.
- Sieg, Remis, 45-Minuten-Limit und Last-Unit-Reveal sind für MS-1 in D-056,
  Inhaltsmanifest, State und maschinenlesbarem Contentvertrag geschlossen.
- VictoryConditions und MultiplayerModes besitzen lokale, führende
  MS-1-Overrides; Commander/Voice/Portrait/Doktrinen sind eindeutig Post-MVP
  und D-009 ist für MS-1 teilersetzt.
- Verbliebene Commander-/Audio-Altformulierungen in Vision, Asset-Register,
  OpenQuestions und AudioArchitecture sind auf Post-MVP vereinheitlicht;
  D-039 ist als vorhandene, durch D-056/D-058 begrenzte Entscheidung verankert.
- Q16.16-Bereich, `DefinitionId`, `EntityId`-Bitlayout, Command-Kappen,
  Schema-/Count-Breiten und nullterminierte XXH64-Domänen sind bytegenau
  festgelegt; Pause/Save/Load sind eindeutig Session-Aktionen.
- Alle 17 alten `docs/tech/modules/*_Spec.md` sind als historischer,
  nicht verbindlicher Prototyp-/Scaffolding-Stand gemäß D-055 markiert.
- V2 und V3 sind als eigene 500-Objekt-Szenarien ergänzt; Rendering,
  Animation und FoW-Budget wurden an 128²/5 Hz sowie MG/Rocket-MS-1
  angeglichen.
- Historische Änderungsverläufe wurden unverändert wiederhergestellt;
  `docs-check` erzwingt Kopfzeile, Pflichtabschnitte, terminale History,
  fünf strikte Quality-JSONs, gepinntes Ajv und die
  Evidence-Negativkontrollen; Änderungen unter `quality/**` lösen den
  Workflow ebenfalls aus.
- Roadmap enthält keine aktive 445-PT- oder Kalenderzusage mehr:
  Aufwandsspanne frühestens nach G2, Kalenderkorridor frühestens nach G4.
- **Recovery-Baseline nach strengem Implementierungs-Audit:** MS-0 ist offen, das MVP ist nicht erreicht und Alpha hat nicht begonnen. Die bisherigen Sprint-7-Einträge belegen nur vorhandene Prototyp-Struktur, nicht fertige oder integrierte Features.
- [ImplementationAudit_2026-07-24.md](docs/production/ImplementationAudit_2026-07-24.md) dokumentiert Testfehler, Integrationslücken, fehlende Akzeptanznachweise und Planungswidersprüche am eingefrorenen Stand `460290e`.
- [MVPRecoveryPlan.md](docs/production/MVPRecoveryPlan.md) ersetzt pauschale Modul-Fertigmeldungen durch sequenzielle Gates G0–G5.
- Sprint-6-Abschluss, Sprint-7-GO, 445-PT-Verbindlichkeit sowie die ungültigen Schließungen Q-018/Q-019 wurden durch D-055 zurückgezogen; R-16 wurde reaktiviert und R-17 ergänzt.

### Entschieden
- **D-056:** Dependency-closed MS-1 mit Allianz/Legion, Glutrinne, neun
  Gebäude- und acht Einheitenrollen je Fraktion, vollständigem D-010-
  Aetherium und definiertem Produktminimum; Q-031/Q-038 geschlossen.
- **D-057:** Kanonischer Q16.16-/Command-/State-/Persistence-Vertrag ab G1;
  exakte Plattformparität, einmaliger Pre-G1-Formatreset; Q-039 geschlossen.
- **D-058:** Feste MS-1-Slots, Entity-/Snapshot-/Flow-Cache-Kappen und
  autoritatives 5-Hz-Team-FoW; Q-032 geschlossen.
- **D-059:** Geschütztes `main` plus kurze Topic-Branches ersetzt D-050.
- **D-060:** Exakter Unity-Pin `6000.5.4f1` ersetzt D-006.
- **D-061:** Ausführbare G0–G5-Gates, unveränderliche Evidence, getrennte
  Full-Content-/Scale-Workloads und feste Laufkadenz; Q-033/Q-034 geschlossen.
- **D-062:** Szenarioassertions und -schwellen an artefaktgebundene
  Rohsamples, Content/Scenario an Subject-Git-Blobs und G1–G5 an eine
  rekursive Same-Subject-Vorgängergate-Kette gebunden.
- **D-063:** Evidence-Schema 1.2 mit kanonischen kriterienspezifischen
  Check-/Log-Artefakten, rekursivem Ajv, exakten Units, drei getrennten
  Performance-Läufen und externem Protected-CI-Trust-Kontext; lokales
  Evidence darf keinen Gate-Pass autorisieren.
- **D-064:** Schema 1.2 bleibt eine fail-closed Integritätsvorstufe. G0-A
  implementiert vor der Plattformarbeit einen subject-unabhängigen,
  nicht selbstautorisierenden Trusted-Gate-Bootstrap; erst Schema 1.3 bindet
  das Trust-Bundle, die vollständige Gate-Kette und exakte Messumgebungen.
- **D-055:** Vorhandenen Code als Prototyp erhalten, Projektstatus auf Recovery zurücksetzen und Fortschritt ausschließlich über reproduzierbare Evidenz qualifizieren.

### Hinzugefügt
- [MVPContentManifest.md](docs/production/MVPContentManifest.md) als
  menschlich lesbare MS-1-Inhaltsgrenze.
- Substantive Technikverträge
  [SimulationCore.md](docs/tech/SimulationCore.md),
  [Commands.md](docs/tech/Commands.md),
  [FogOfWar.md](docs/tech/FogOfWar.md) und
  [CameraSystem.md](docs/tech/CameraSystem.md).
- Maschinenlesbare Verträge
  [`quality/content/mvp-v1.json`](quality/content/mvp-v1.json),
  [`quality/scenarios/mvp-v1.json`](quality/scenarios/mvp-v1.json) und
  [`quality/schemas/GateEvidence.schema.json`](quality/schemas/GateEvidence.schema.json);
  keine Evidence-Platzhalter.
- [`quality/scripts/validate_gate_evidence.py`](quality/scripts/validate_gate_evidence.py)
  für Cross-Field-, Subject-Blob-, Rohsample-/Schwellen-, Gate-Ketten-,
  Artefakt-, Reviewer-, Kriterien- und Gate-Profil-Prüfung mit generierten
  Negativkontrollen.
- Gepinnte Draft-2020-12-Prüfung über
  [`quality/scripts/validate_evidence_schema.mjs`](quality/scripts/validate_evidence_schema.mjs),
  [`quality/package.json`](quality/package.json) und
  [`quality/package-lock.json`](quality/package-lock.json).
- Fail-closed `E_AUTHORIZATION_BOOTSTRAP`-Sperre für jeden
  Schema-1.2-Pass-Versuch sowie R-18 für selbstautorisierende Prüftools und
  ungebundene Messumgebungen.
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
  - **Historische, nicht freigegebene Modulspezifikationen:** `MovementSystem_Spec.md`, `GameplayBridge_Spec.md`, `GameDatabase_Spec.md`, `CommandSystem_Spec.md`, `CombatSystem_Spec.md`, `LockstepReplay_Spec.md`, `EconomySystem_Spec.md`, `ConstructionSystem_Spec.md`, `ProductionSystem_Spec.md`, `VisionSystem_Spec.md`, `SkirmishAi_Spec.md`, `RtsUi_Spec.md`, `AssetIntegration_Spec.md`, `EvolvedFaction_Spec.md`, `CommanderSystem_Spec.md`, `LockstepRelay_Spec.md` und `MapExpansion_Spec.md` unter `docs/tech/modules/`; ihr Inhalt ist forensisch, aktive Verträge führen.

### Behoben

- `HEADLESS_VALID_MATCH` weist G3 nun ausdrücklich als Nutzer aus und stimmt
  damit mit dem verpflichtenden G3-Gate-Profil überein.
- Evidence kann überschrittene Szenarioschwellen, Working-Tree-Digests oder
  isolierte spätere Gates nicht mehr als `pass` akzeptieren.
- No-op-Commands, falsche Units, negative/unterzählige Performance-Samples,
  schemawidrige Vorgänger und lokale Pass-Dateien ohne externen Trust-Kontext
  werden fail-closed abgelehnt.
- Punkt- und Performance-Metrikartefakte im Recovery-Plan eindeutig auf
  `samples` beziehungsweise `measurement` getrennt.
- Aktiven Sprint-7-Scope in G0-A Trusted-Gate-Bootstrap und G0-B
  Plattformbasis geteilt; erst ein nachfolgender sauberer Subject-Commit
  darf G0 belegen.
- Valide Evidence mit `verdict=fail` wird als `VALID NON-PASS EVIDENCE`
  mit Exitcode ungleich null ausgegeben und kann nicht mehr als
  `AUTHORIZED PASS` erscheinen.

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

[Unreleased]: https://github.com/VibecodingGermany/Project_Nova/compare/v0.4.0...HEAD
[0.7.0]: https://github.com/VibecodingGermany/Project_Nova/commit/0baa304
[0.6.0]: https://github.com/VibecodingGermany/Project_Nova/commit/af30ccd
[0.5.0]: https://github.com/VibecodingGermany/Project_Nova/commit/b125229
[0.4.0]: https://github.com/VibecodingGermany/Project_Nova/releases/tag/v0.4.0
[0.3.0]: https://github.com/VibecodingGermany/Project_Nova/commit/2d2d021
[0.2.0]: https://github.com/VibecodingGermany/Project_Nova/commit/2d2d021
[0.1.0]: https://github.com/VibecodingGermany/Project_Nova/commit/2d2d021
