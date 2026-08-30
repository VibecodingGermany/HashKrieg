# Hashkrieg – Entwicklungs-Wiki

**Version:** 0.21.0 | **Status:** unveröffentlichter Arbeitsstand – Sprint 16 technisch umgesetzt; manuelle Strang-C-, Netzwerk-, Gefechts- und Lobby-Abnahmen offen | **Verantwortungsbereich:** Executive Producer / Technical Writer | **Sprint:** 16

## Zweck

Zentraler Einstieg in das versionierte Hashkrieg-Wiki. Der aktuelle Stand
bündelt die technische Umsetzung der Sprint-12-Stränge A und B; er ist kein
Game-Release und ohne die benannten gespielten Abnahmen kein bestandenes Gate.

## Abhängigkeiten

- [../README.md](../README.md) – Repository-Einstieg
- [../GOVERNANCE.md](../GOVERNANCE.md) – Tier-Modell, aktives Governance-Tier
- [../AGENTS.md](../AGENTS.md) und
  [../CONTRIBUTING.md](../CONTRIBUTING.md) – Arbeits- und PR-Regeln
- [../LICENSE](../LICENSE), [../NOTICE](../NOTICE) und
  [../CONTRIBUTOR_LICENSE_AGREEMENT.md](../CONTRIBUTOR_LICENSE_AGREEMENT.md) –
  Lizenzgrenze und Beitragsrechte
- [meta/DocumentationStandard.md](meta/DocumentationStandard.md) –
  Dokumentationsstandard

## Projektstatus

| Stufe | Status |
|---|---|
| Governance | **Tier 2** – externe Beitragende, ein Projektinhaber (`@cubetribe`, D-105) |
| Sprint 12 | Strang A A1–A7 umgesetzt, A8 Stufe 1 nachgewiesen und Stufen 2–4 offen (D-089); Strang B technisch umgesetzt, 60-Einheiten-Gegenhör-/Sichtabnahme offen (D-090) |
| Spielbar | lokales 1v1 auf der Glutrinne-Graybox (Ablauf: `production/DemoRunbook.md`) |
| MS-0 | offen – Kern läuft, Cross-Plattform- und Perf-Nachweise stehen aus |
| MS-1 / MVP | nicht erreicht – Lücken in [ScopeLedger](production/ScopeLedger.md) |
| Alpha | nicht begonnen |

Verbindlicher Stack: Unity `6000.5.4f1`, Revision `d550df8bd089`, URP, C#.
Closed-Core MS-1 ist D-056; deterministischer Kern D-057; Capacity/FoW D-058;
Branching D-059; Engine D-060. Die Evidenz- und Gate-Entscheidungen D-061 bis
D-066 bleiben gültig, ruhen aber bis Tier 3 – siehe
[../GOVERNANCE.md](../GOVERNANCE.md) und [../quality/README.md](../quality/README.md).

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
- [Networking](tech/Networking.md) – D-089-1v1-Profil und historisches Vollspiel-Zielbild
- [RelayServer](tech/RelayServer.md) – Konfiguration, systemd, Artefakt,
  Deploy, Rollback, Firewall und ehrlicher Abnahmestand des TCP-Relays
- [Lobby/Supabase](tech/LobbySupabase.md) – Vertrag, Schema,
  Edge-Function-Referenzen und Betriebspfad der Sprint-14-Lobby (D-092 bis D-094);
  Supabase-Anlage und gespielte Abnahme offen
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
- [Provenance](assets/Provenance.md) (0.2.0, verbindlicher Workflow) –
  Provenienz- und Lizenznachweisverfahren je Asset
- [AssetPackage](assets/AssetPackage.md) (1.0.0) – warum die 3D-Assets als
  Paket ausserhalb des Repos liegen, Inhalt und Installationsablauf
- [AssetImport_Tripo_2026-08-06](assets/AssetImport_Tripo_2026-08-06.md) –
  Importprotokoll der 34 Tripo-Assets (GLB → FBX, LODs, Texturen)
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
- [Nutzerfeedback_Ablauf](production/Nutzerfeedback_Ablauf.md) – **verbindlich:** wie Testberichte aus der Datenbank anonymisiert, zerlegt und zu Sprintvorschlägen gebündelt werden
- [OpenQuestions](production/OpenQuestions.md)
- [RiskAnalysis](production/RiskAnalysis.md)
- [GrayboxLog](production/GrayboxLog.md) – Sitzungsprotokoll der Graybox-Spur (D-067, Entwurf)
- [ScopeLedger](production/ScopeLedger.md) – Zurückstellungen der Graybox-Spur, verweist auf Manifest-Schlüsselpfade
- [DemoRunbook](production/DemoRunbook.md) (0.6.0, Entwurf) – erste Demo-Runde: Ablauf, Steuerung, fünf endliche Aetheriumfelder, bekannte Grenzen und Asset-Ablage
- [StatusSnapshot 2026-08-05](production/StatusSnapshot_2026-08-05.md) (0.1.0) – datierter Projektstand vor dem Eintreffen der ersten 3D-Assets
- [Hashkrieg-Planungsmappe](production/hashkrieg/README.md) und
  [Sprint 12 „Zu zweit"](production/hashkrieg/12_Sprint_Zu_Zweit.md) –
  Strang A A1–A7 implementiert, headless A8 Stufe 1 nachgewiesen;
  Zwei-Fenster-, LAN- und VPS-Partie offen
- [Sprint 13.0 „Freigabe für den Parallelbetrieb"](production/hashkrieg/13-0_Sprint_Freigabe.md) – PolyForm-Lizenz, Tier-2-Entscheidung, Maintainer-/Fork-Modell und CI-Wächter vorbereitet; die externe PR-Negativkontrolle steht noch aus
- [Sprint 12 Strang B „Sichtbares Gefecht"](production/hashkrieg/12B_Sprint_Sichtbares_Gefecht.md) –
  VFX, Tier-0-SFX, Mixer, Provenienz und Quellgrenzen technisch umgesetzt;
  manuelle 60-Einheiten-Gegenhör-/Sichtabnahme offen
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
- Sprint 12 A8 Stufen 2–4 (zwei Unity-Fenster, LAN, VPS) sind nicht gespielt.
- Für Strang B fehlt die manuelle Sicht-/Gegenhörabnahme mit einem dichten
  Gefecht. Die 591/591 EditMode- und 3/3 fokussierten Graybox-PlayMode-Tests
  ersetzen diese menschliche Prüfung nicht.
- Die vier Suno-Musikdatensätze benennen echte, noch fehlende Ursprungs- oder
  Konvertierungsbelege und bleiben bis zu deren Lieferung `incomplete`.

## Nächste Schritte

1. Den vorliegenden macOS-Build visuell und auditiv im dichten Gefecht prüfen.
2. A8 mit zwei Unity-Fenstern, danach im LAN und auf dem VPS spielen.
3. Sprint 16 im laufenden Spiel abnehmen; Strang C ist technisch umgesetzt,
   aber ohne diesen Durchlauf weder gespielt noch vollständig DoD-fertig.

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
| 0.15.0 | 2026-08-05 | GB-003 indexiert: DemoRunbook.md und StatusSnapshot_2026-08-05.md aufgenommen (Asset-Bereitschaft, Glutrinne-Blockout, Demo-Vorbereitung); ScopeLedger 0.4.0 und GrayboxLog 0.3.0 fortgeschrieben; kein Gate-Status | Technical Writer |
| 0.16.0 | 2026-08-07 | D-089-Netzprofil, RelayServer-Runbook und Sprint-12-Strang-A-Stand indexiert; manuelle Netzwerkabnahme ausdrücklich offen | Technical Writer |
| 0.17.0 | 2026-08-08 | D-091 und Sprint 13.0 indexiert: Tier-2-Beitragsmodell, Lizenz- und Merge-Schutz vorbereitet | Technical Writer |
| 0.18.0 | 2026-08-09 | Sprint-14-Lobby indexiert: LobbySupabase.md (Vertrag, Schema, Edge-Function-Referenzen, Betriebspfad) und RelayServer.md 1.1.0 (kurzlebige Lobby-Tokens) aufgenommen, D-092 bis D-094; Supabase-Anlage, Relay-Redeploy und gespielte Abnahme ausdrücklich offen | Agent (Umsetzung) |
| 0.19.0 | 2026-08-10 | D-105 indexiert: Dennis Westermann ist alleiniger Projektinhaber, Tier-Entscheider und Mergeberechtigter; Tier 2 und die externen CLA-/Review-Regeln bleiben aktiv | Technical Writer |
| 0.20.0 | 2026-08-10 | DemoRunbook 0.6.0 indexiert: D-102/Sprint 16.7 ersetzt die alte Zwei-Feld-Demo durch fünf endliche, sichtbare Aetheriumfelder | Codex / Dennis Westermann |
| 0.21.0 | 2026-08-10 | Sprint 16 technisch bis Paket 16.10 abgeschlossen und die 707/707 Headless-, 591/591 EditMode- und fokussierten 3/3 PlayMode-Nachweise klar von der offenen manuellen Strang-C-Abnahme getrennt | Codex / Dennis Westermann |
