# Project Nova – Entwicklungs-Wiki

**Version:** 0.14.0 | **Status:** unveröffentlichter Recovery-Stand – G0-A1 Mergekandidat, G0-A2 offen | **Verantwortungsbereich:** Executive Producer / Technical Writer | **Sprint:** 7

## Zweck

Zentraler Einstieg in das versionierte Project-Nova-Wiki. Die Version 0.12.0
rebaselined Planung und technische Verträge; sie ist kein Game-Release und
kein bestandenes Gate.

## Abhängigkeiten

- [../README.md](../README.md) – Repository-Einstieg
- [../AGENTS.md](../AGENTS.md) und
  [../CONTRIBUTING.md](../CONTRIBUTING.md) – Arbeits- und PR-Regeln
- [meta/DocumentationStandard.md](meta/DocumentationStandard.md) –
  Dokumentations- und Evidence-Autorität

## Projektstatus

| Stufe | Status |
|---|---|
| Sprint 6 | durch D-055 beendet und durch den Recovery-Plan ersetzt |
| Sprint 7 | gestartet; nur G0-A zur Implementierung freigegeben |
| G0 | G0-A1 Mergekandidat; G0-A2 und G0-B offen |
| MS-0 | nicht erreicht |
| MS-1 / MVP | nicht erreicht |
| Alpha | nicht begonnen |

Verbindlicher Stack: Unity `6000.5.4f1`, Revision `d550df8bd089`, URP, C#.
Closed-Core MS-1 ist D-056; deterministischer Kern D-057; Capacity/FoW D-058;
Branching D-059; Engine D-060; Evidence/Acceptance D-061; durchgesetzte
Szenario-/Subject-/Gate-Kette D-062.
Schema-1.2-/Check-Härtung D-063; subject-unabhängiger
Trusted-Gate-Bootstrap und Schema-1.3-Ziel D-064; fail-closed Trennung von
G0-A1 und zweiphasigem Receipt-Authorizer D-066.

## Meta und Analyse

- [DocumentationStandard.md](meta/DocumentationStandard.md)
- [KnowledgeBase.md](analysis/KnowledgeBase.md)
- [Inconsistencies.md](analysis/Inconsistencies.md)
- [GapAnalysis.md](analysis/GapAnalysis.md)
- [PriorityList.md](analysis/PriorityList.md)

## Research

- [RTS-Markt](research/RTS_Markt_Wettbewerb.md)
- [Multiplayer-Simulation](research/Multiplayer_Simulation.md)
- [Unity ECS/DOTS](research/Unity_ECS_DOTS.md)
- [Pathfinding](research/Pathfinding.md)
- [Fog of War](research/FogOfWar.md)
- [Open-Source-RTS-Architekturen](research/RTS_Architekturen_OpenSource.md)
- [Unity Best Practices](research/Unity_BestPractices.md)
- [KI-Architektur](research/KI_Architektur.md)
- [Animation, Audio und UI](research/Animation_Audio_UI.md)
- [Asset-Store-Landschaft](research/AssetStore_Landschaft.md)

Research ist historischer Entscheidungsinput. Bei Versions- oder Scopekonflikt
führen D-056–D-066.

## Vision und Game Design

- Vision: [Vision](vision/Vision.md), [USP](vision/USP.md),
  [TargetAudience](vision/TargetAudience.md),
  [CoreGameplay](vision/CoreGameplay.md), [GameLoop](vision/GameLoop.md)
- [Lore](vision/Lore.md) (0.1.0, Entwurf) – Weltentwurf für den neuen Arbeitstitel
  *Hashkrieg*: Vorgeschichte, Ökonomie, Fraktionen; Umbenennung im Bestand noch
  nicht vollzogen
- Fraktionen/Content: [Factions](gamedesign/Factions.md),
  [Buildings](gamedesign/Buildings.md), [Infantry](gamedesign/Infantry.md),
  [Vehicles](gamedesign/Vehicles.md), [Aircraft](gamedesign/Aircraft.md)
- Wirtschaft/Forschung: [Resources](gamedesign/Resources.md),
  [Economy](gamedesign/Economy.md),
  [ResearchTree](gamedesign/ResearchTree.md)
- Kampf: [Weapons](gamedesign/Weapons.md),
  [DamageSystem](gamedesign/DamageSystem.md),
  [ArmorSystem](gamedesign/ArmorSystem.md)
- Welt: [Maps](gamedesign/Maps.md), [Biomes](gamedesign/Biomes.md),
  [NeutralUnits](gamedesign/NeutralUnits.md),
  [FogOfWar](gamedesign/FogOfWar.md)
- Meta: [CommanderSystem](gamedesign/CommanderSystem.md),
  [MultiplayerModes](gamedesign/MultiplayerModes.md),
  [VictoryConditions](gamedesign/VictoryConditions.md),
  [Balancing](gamedesign/Balancing.md),
  [Campaign](gamedesign/Campaign.md)

Die GDDs behalten Vollspiel-Zielwerte. Für MS-1 hat
[MVPContentManifest.md](production/MVPContentManifest.md) Vorrang.

## Technical Design

### Kern und Verträge

- [Architecture](tech/Architecture.md)
- [DependencyGraph](tech/DependencyGraph.md)
- [ModuleOverview](tech/ModuleOverview.md)
- [SimulationCore](tech/SimulationCore.md)
- [Commands](tech/Commands.md)
- [GameState](tech/GameState.md)
- [Serialization](tech/Serialization.md)
- [Savegames](tech/Savegames.md)
- [Replication](tech/Replication.md)

Die 17 Dateien unter `tech/modules/*_Spec.md` konservieren ausschließlich den
nicht abgenommenen Prototyp-/Scaffolding-Stand aus D-055. Trotz erhaltener
Detailtexte sind sie nicht verbindlich; bei Konflikten führen die oben
gelisteten Kernverträge.

### Gameplay und Präsentation

- [Pathfinding](tech/Pathfinding.md)
- [FogOfWar](tech/FogOfWar.md)
- [AIArchitecture](tech/AIArchitecture.md)
- [InputSystem](tech/InputSystem.md)
- [CameraSystem](tech/CameraSystem.md)
- [Rendering](tech/Rendering.md)
- [Lighting](tech/Lighting.md)
- [AnimationSystem](tech/AnimationSystem.md)
- [AudioArchitecture](tech/AudioArchitecture.md)

### Struktur, Qualität und Betrieb

- [FolderStructure](tech/FolderStructure.md)
- [CodingGuidelines](tech/CodingGuidelines.md)
- [NamingConvention](tech/NamingConvention.md)
- [PerformanceBudget](tech/PerformanceBudget.md)
- [MemoryBudget](tech/MemoryBudget.md)
- [AssetBudget](tech/AssetBudget.md)
- [Testing](tech/Testing.md)
- [Deployment](tech/Deployment.md)
- [Networking](tech/Networking.md) – Post-MVP-Transportziel
- Architecture Reviews: [Performance](tech/review/Review_Performance.md),
  [Wartbarkeit](tech/review/Review_Wartbarkeit_Prozess.md),
  [Architektur-Kohärenz](tech/review/Review_ArchitekturKohaerenz.md),
  [Multiplayer](tech/review/Review_Multiplayer_Netcode.md),
  [Skalierung](tech/review/Review_Skalierung_Systemgrenzen.md),
  [GDD↔TDD](tech/review/Review_GDD-TDD-Konsistenz.md)

## Assets

- [ProcurementStrategy](assets/ProcurementStrategy.md)
- [AssetRegister](assets/AssetRegister.md)
- [Licenses](assets/Licenses.md)
- [BuildBacklog](assets/BuildBacklog.md)
- [ArtAssetStandard](assets/ArtAssetStandard.md) (0.2.0, Entwurf) –
  Art-Standard (Ordner, Namen, Import, Material, Masken)
- [ArtManifest_MS1](assets/ArtManifest_MS1.md) (0.3.0, Entwurf) –
  Spezifikationsblätter der 34 MS-1-Art-Assets
- [SourceCatalog_MS1](assets/SourceCatalog_MS1.md) (0.2.0, Entwurf) –
  CC0-/KI-Beschaffungskatalog und Lizenzbefunde
- [Provenance](assets/Provenance.md) (0.1.0, Entwurf) –
  Provenienz- und Lizenznachweisverfahren je Asset
- [VerticalSlice_MS1](assets/VerticalSlice_MS1.md) (0.2.0, Entwurf) –
  Vertical-Slice-Spezifikation der vier Erst-Assets
- [ConceptArtStyleGuide](assets/ConceptArtStyleGuide.md) (0.1.0, Entwurf) –
  verbindlicher Bildstandard für Hashkrieg-Concept-Art
- [concept-art/README](assets/concept-art/README.md) (0.1.0, Entwurf) – 34
  Concept-Art-Entwürfe samt Herkunftsnachweis, keine Produktionsassets

## Production und Recovery

- [ImplementationAudit 2026-07-24](production/ImplementationAudit_2026-07-24.md)
- [MVPRecoveryPlan](production/MVPRecoveryPlan.md)
- [MVPContentManifest](production/MVPContentManifest.md)
- [Milestones](production/Milestones.md)
- [SprintPlanning](production/SprintPlanning.md)
- [Roadmap](production/Roadmap.md)
- [DecisionLog](production/DecisionLog.md)
- [OpenQuestions](production/OpenQuestions.md)
- [RiskAnalysis](production/RiskAnalysis.md)
- [GrayboxLog](production/GrayboxLog.md) – Sitzungsprotokoll der Graybox-Spur (D-067, Entwurf)
- [ScopeLedger](production/ScopeLedger.md) – Zurückstellungen der Graybox-Spur, verweist auf Manifest-Schlüsselpfade
- Sprintberichte: [0](production/sprints/Sprint00_Report.md),
  [1](production/sprints/Sprint01_Report.md),
  [2](production/sprints/Sprint02_Report.md),
  [3](production/sprints/Sprint03_Report.md),
  [4](production/sprints/Sprint04_Report.md),
  [5](production/sprints/Sprint05_Report.md),
  [6](production/sprints/Sprint06_Report.md)

## Maschinenlesbare Quality-Verträge

- [`quality/content/mvp-v1.json`](../quality/content/mvp-v1.json) – exakter
  Content-Scope
- [`quality/scenarios/mvp-v1.json`](../quality/scenarios/mvp-v1.json) –
  Workloads, Kadenz, Schwellen und gesperrter Autorisierungsstatus
- [`quality/schemas/GateEvidence.schema.json`](../quality/schemas/GateEvidence.schema.json) –
  Integritätsvorstufe Schema 1.3; kein Pass-Autorisierer
- [`quality/scripts/validate_gate_evidence.py`](../quality/scripts/validate_gate_evidence.py) –
  Cross-Field-, Artefakt-, SHA-/Pfad- und Gate-Profil-Prüfung mit
  fail-closed D-066-Bootstrap-Sperre
- [`quality/scripts/validate_evidence_schema.mjs`](../quality/scripts/validate_evidence_schema.mjs)
  mit [`quality/package-lock.json`](../quality/package-lock.json) – gepinnte
  Draft-2020-12-Prüfung für aktuelle und rekursive Evidence

`quality/evidence/` entsteht nur aus realen Versuchen. Es gibt keine
Platzhalter-Evidence. G0-A1 liefert ausschließlich Integrity. G0-A2 muss den
zweiphasigen `GateAuthorization.json`-Pfad erst implementieren; bis dahin
kann keine Datei einen Gate-Pass erzeugen.

## Quelldokumente

- [RTS_Game_Design_Outline.md](../RTS_Game_Design_Outline.md) – historisch
- [RTS_Technisches_Planungsdokument.md](../RTS_Technisches_Planungsdokument.md) –
  historisch; aktive Verträge führen
- [RTS_Asset_Pipeline.md](../RTS_Asset_Pipeline.md) – historisch

## Offene Punkte

- Q-018 und Q-019 bleiben offen und nicht MS-1-blockierend.
- G0-A2 und damit Gate G0 sind noch nicht nachgewiesen.

## Nächste Schritte

1. G0-A1 ohne Gate-Fortschritt geschützt mergen.
2. G0-A2 als separaten zweiphasigen Receipt-Authorizer implementieren.
3. Am nachfolgenden sauberen Subject G0-B belegen.
4. G1/V1–V5a erst nach bestandenem G0 sequenziell umsetzen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Initiale Wiki-Struktur nach Sprint 0 | Technical Writer |
| 0.2.0 | 2026-07-21 | Research-Bereich (10 Dokumente) aufgenommen, Sprint 1 abgeschlossen | Technical Writer |
| 0.3.0 | 2026-07-21 | Vision- und GDD-Bereich (25 Dokumente) aufgenommen, Sprint 2 abgeschlossen | Technical Writer |
| 0.4.0 | 2026-07-21 | Technical-Design-Bereich (23 Dokumente) aufgenommen, Sprint 3 abgeschlossen | Technical Writer |
| 0.5.0 | 2026-07-21 | Sprint 4 (Architecture Review) abgeschlossen: 6 Reviews, D-043–D-052 | Executive Producer |
| 0.6.0 | 2026-07-22 | Sprint 5 (Asset Audit) abgeschlossen: Asset-Bereich (4 Dokumente), D-053/D-054 | Executive Producer |
| 0.7.0 | 2026-07-24 | Sprint 6 (Produktionsplanung) abgeschlossen: Milestones.md, Roadmap.md, Sprint06_Report.md, Q-018/Q-019 geschlossen, R-16 mitigiert, Sprint 7 GO | Executive Producer |
| 0.7.1 | 2026-07-24 | Recovery-Baseline: Implementierungs-Audit, D-055, tatsächlicher Status und MVP-Gates G0–G5 | Executive Producer / Lead Technical Director |
| 0.8.0 | 2026-07-24 | D-056–D-061, neue Kern-TDDs, MVP-/Scenario-/Evidence-Verträge und G0-offenen Status indexiert | Executive Producer / Technical Writer |
| 0.8.1 | 2026-07-24 | Historische Modulblätter deautorisiert und Evidence-Semantikvalidator indexiert | Executive Producer / Technical Writer |
| 0.8.2 | 2026-07-24 | Sprint-6-Endstatus und G0-begrenzten Start von Sprint 7 präzisiert | Executive Producer / Technical Writer |
| 0.9.0 | 2026-07-24 | D-062-Evidence-Härtung und lokale MS-1-Overrides für Victory, MatchConfig und Commander indexiert | Executive Producer / Technical Writer / Lead QA Engineer |
| 0.10.0 | 2026-07-24 | D-063-Schema 1.2, gepinntes Ajv, kanonische Check-Artefakte, Drei-Lauf-Messung und Protected-CI-Trust indexiert | Executive Producer / Technical Writer / Lead QA Engineer |
| 0.11.0 | 2026-07-24 | D-064 Trusted-Gate-Bootstrap, Schema-1.3-Ziel und fail-closed G0-A-Start indexiert | Executive Producer / Technical Writer / Lead QA Engineer |
| 0.12.0 | 2026-07-25 | D-066: G0-A1 Integrity von G0-A2 Receipt-Autorisierung getrennt und zirkulären Authorize-Pfad zurückgezogen | Executive Producer / Technical Writer / Lead QA Engineer |
| 0.12.1 | 2026-07-25 | Art-Strang MS-1 (D-069–D-073) indexiert: ArtAssetStandard, ArtManifest_MS1, SourceCatalog_MS1, Provenance, VerticalSlice_MS1 – kein Gate-Status, kein Asset im Repository | Technical Writer |
| 0.13.0 | 2026-07-26 | Graybox-Spur indexiert: GrayboxLog und ScopeLedger aufgenommen; Art-Strang-D-IDs nach der Merge-Kollision auf D-069–D-073 nachgeführt | Technical Writer |
| 0.14.0 | 2026-07-26 | Hashkrieg-Weltentwurf und Concept-Art-Strang indexiert: Lore.md, ConceptArtStyleGuide.md und concept-art/README.md aufgenommen; kein Gate-Status | Technical Writer |
