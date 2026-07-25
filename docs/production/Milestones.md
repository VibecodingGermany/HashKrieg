# Meilenstein-Planung (Milestones)

**Version:** 1.1.0 | **Status:** in Recovery-Korrektur – D-055 | **Verantwortungsbereich:** Producer / Game Director | **Sprint:** 7

## Zweck

Definiert die verbindlichen Entwicklungsmeilensteine (**MS-0 bis MS-4**) für *Project Nova*. Für jeden Meilenstein werden Feature-Scope, technische Akzeptanz-Gates, Deliverables und Abnahmekriterien festgelegt. Dieses Dokument stellt sicher, dass die Entwicklung in kontrollierten, messbaren Phasen erfolgt, und verhindert Scope-Explosionen ([RiskAnalysis.md](RiskAnalysis.md), R-01). Verbindlich für: Producer, Lead Technical Director, Game Director, alle Mitwirkenden.

## Abhängigkeiten

- [SprintPlanning.md](SprintPlanning.md) – Sprint-Definitionen und Phasenmodell
- [Roadmap.md](Roadmap.md) – Zeit- und Aufwandsschätzung (Personentage)
- [DecisionLog.md](DecisionLog.md) – Architekturentscheidungen (D-006 Engine, D-007 Geschäftsmodell, D-033 Sim/MP, D-054 0 € Asset-Pipeline)
- [../assets/BuildBacklog.md](../assets/BuildBacklog.md) – Asset-Eigenbau-Backlog (B-01 bis B-14)
- GDD & TDD Dokumentation unter [../gamedesign/](../gamedesign/) und [../tech/](../tech/)

---

## Recovery-Nachweisstatus

| Stufe | Tatsächlicher Status am Stand `460290e` | Führender Nachweis |
|---|---|---|
| MS-0 | **offen** | Cross-Plattform-Determinismus und Referenzhardware-/Shader-Gates fehlen |
| MS-1 MVP | **nicht erreicht** | Keine spielbare Build-Szene und keine integrierte End-to-End-Partie |
| MS-2 Alpha | **nicht begonnen** | Alpha-Dateien sind Scaffolding, keine abgenommenen Features |
| MS-3 / MS-4 | **nicht begonnen** | Vorgelagerte Gates nicht bestanden |

Die nachfolgenden Sprint-6-Spezifikationen bleiben als historische Zielbilder erhalten. Sie sind bis Q-038 und zur Recovery-Rebaseline weder Umfangszusage noch Fertigmeldung. Bei Widersprüchen gelten D-009, D-017, D-025, D-033 und D-055.

## 1. Übersicht der Meilensteine

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│    MS-0     │ ──► │    MS-1     │ ──► │    MS-2     │ ──► │    MS-3     │ ──► │    MS-4     │
│  Phase 0:   │     │ Phase 1:    │     │ Phase 2:    │     │ Phase 3:    │     │ Phase 4:    │
│ Spike / VS  │     │   MVP       │     │   Alpha     │     │   Beta      │     │ Release 1.0 │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
```

| Meilenstein | Phase | Fokus | Hauptziel | Ziel-Qualitätsgate |
|---|---|---|---|---|
| **MS-0** | Phase 0 | **Spike & Vertical Slice** | Festkomma-Determinismus (ARM↔x86), Pathfinding-Spike, UI & Signature-Asset B-01/B-02 | Determinismus-Proof, 60 FPS SimRunner |
| **MS-1** | Phase 1 | **MVP (Minimum Viable Product)** | Spielbarer SP/Skirmish-Kern: 2 Fraktionen (Allianz vs. Legion), 1 Map, 1 Biom, 12 Gebäude, 27 Einheiten, KI | Stabile 1v1-Skirmish-Partie gegen KI |
| **MS-2** | Phase 2 | **Alpha** | 3. Fraktion (Evolvierte), 3 Maps, 3 Biome, Doktrinen, 2–4 Spieler Coop/Skirmish Relay | Vollständiges 3-Fraktionen-Balancing |
| **MS-3** | Phase 3 | **Beta** | 12 Maps, 10 Biome, Superwaffen, Sound/VFX-Pass, Telemetrie (Q-019), Polish | Feature-Complete, Zero Critical Bugs |
| **MS-4** | Phase 4 | **Release (v1.0)** | SP-Kampagne, Steam-Integration, Golden Master, Community-Launch | Gold Master Build, Steam Release |

---

## 2. Detaillierte Meilenstein-Spezifikationen

### MS-0: Phase 0 – Spike & Vertical Slice (V0-Gate)

* **Ziel:** Nachweis der technischen Machbarkeit des Simulationskerns und Etablierung der Referenz-Assets.
* **Feature-Scope:**
  * Fixed-Point-Determinismus-Spike zwischen x86 (Windows) und ARM (Mac M2, D-052).
  * Lockstep/Command-SimRunner ([../tech/Architecture.md](../tech/Architecture.md), D-033).
  * Hybrid-Pathfinding-Spike (Flow-Field + A*, D-034).
  * Build-Asset **B-01/B-02** (Aetherium-Mutterkristall & Shader als Referenz-Frame).
  * Build-Asset **B-10** (RTS-UI Layout: Command-Card, Minimap, Ressourcenleiste).
* **Akzeptanz-Gates (V0-Exit):**
  1. Fixed-Point Math erzeugt 100 % identische SimRunner-Hashes auf Windows (x86) und macOS (ARM).
  2. Flow-Field-Pathfinding bewegt 500 Einheiten bei ≥60 FPS auf Referenzhardware (D-052).
  3. Aetherium-Shader läuft performant unter URP.

---

### MS-1: Phase 1 – MVP (Minimum Viable Product / V1-Gate)

* **Ziel:** Ein vollständig spielbares 1v1-Skirmish-RTS mit menschlichen Fraktionen gegen die KI.
* **Feature-Scope:**
  * **Fraktionen:** Allianz & Legion (menschlich-mechanisch).
  * **Gebäude:** 12 Gebäudetypen pro Fraktion ([../gamedesign/Buildings.md](../gamedesign/Buildings.md), D-008).
  * **Einheiten:** 8 Infanterie-, 12 Fahrzeug- und 7 Lufteinheiten pro Fraktion.
  * **Wirtschaft:** Aetherium-Mutterkristalle, nachwachsende Felder, Überernte ([../gamedesign/Resources.md](../gamedesign/Resources.md), D-010).
  * **Karte:** 1 Karte, 1 Biom (Wüste/Steppe).
  * **KI:** Skirmish-KI mit Basis-Skripting (Wirtschaft, Aufschalten, Angriff).
  * **Assets:** CC0-Bibliotheken (Quaternius/Kenney) + KI-Drafting (D-054).
* **Akzeptanz-Gates (V1-Exit):**
  1. Ein komplettes Skirmish-Match (20–35 Min.) verläuft von HQ-Bau bis Sieg/Niederlage ohne Absturz oder Desync.
  2. P95-Frame-Time ≤ 16,6 ms (60 FPS) auf Referenzhardware (D-052).

---

### MS-2: Phase 2 – Alpha (V2-Gate)

* **Ziel:** Vervollständigung der 3 asymmetrischen Fraktionen und Einführung von Multiplayer-Relay.
* **Feature-Scope:**
  * **Fraktion 3:** Evolvierte (biologisch-organisch, Eigenbau B-04 bis B-09).
  * **Doktrinen:** Commander-Identitäten & passive Doktrinen-Varianten (D-009).
  * **Karten/Biome:** 3 Karten, 3 Biome (Wüste, Schnee, Dschungel/Industrie).
  * **Multiplayer:** 2–4 Spieler Skirmish / Coop über Command-Relay (D-046, D-051).
  * **Audio/VFX:** Sonniss GDC Audio-Einbindung, Teamfarben-Material-Pass (B-14).
* **Akzeptanz-Gates (V2-Exit):**
  1. Drei Fraktionen sind balanciert (Win-Rate zwischen 47 % und 53 % in KI-Simulationen).
  2. 4-Spieler-Match läuft stabil über Command-Relay.

---

### MS-3: Phase 3 – Beta (V3-Gate)

* **Ziel:** Feature-Vollständigkeit, Skalierung aller Karten/Biome, Telemetrie & Polish.
* **Feature-Scope:**
  * **Content:** Alle 12 Karten, alle 10 Biome ([../gamedesign/Maps.md](../gamedesign/Maps.md), [../gamedesign/Biomes.md](../gamedesign/Biomes.md)).
  * **Superwaffen:** 3 Superwaffen-Gebäude & Effekte (D-008, B-13).
  * **Telemetrie:** Opt-in anonymisiertes Crash- & Match-Balancing-System (Q-019).
  * **Audio:** Vollständiger Musik- & SFX-Mix.
  * **Performance:** Memory-Deckel 1,8 GB eingehalten ([../tech/AssetBudget.md](../tech/AssetBudget.md)).
* **Akzeptanz-Gates (V3-Exit):**
  1. Zero Critical (Blocker/Crash) Bugs.
  2. Stabile Performance auf Minimum-Spec (Ryzen 3 3100 / GTX 1050 Ti, ≥30 FPS).

---

### MS-4: Phase 4 – Release v1.0 (Gold Master)

* **Ziel:** Kommerzielle Veröffentlichung auf Steam für Windows und macOS.
* **Feature-Scope:**
  * **Singleplayer-Kampagne:** Story-Missionen mit Commander-Portraits & Voice-Sets (B-12).
  * **Steam-Integration:** Achievements, Cloud-Saves, Steamworks API.
  * **Gold Master Build:** Verifizierte Binary-Hashes, finale Lokalisierung.
* **Akzeptanz-Gates (Release-Exit):**
  1. Green-Light im Steamworks Submission Check.
  2. Vollständig verifiziertes Golden-Master-Release.

---

## 3. Feature-Matrix nach Meilenstein

| Feature-Bereich | MS-0 (Spike) | MS-1 (MVP) | MS-2 (Alpha) | MS-3 (Beta) | MS-4 (Release) |
|---|---|---|---|---|---|
| **Simulations-Kern** | Fixed-Point / Lockstep | Lockstep 1v1 | Lockstep 2–4 Player | Full Re-Sim / Trust-Anchor | Gold Master |
| **Allianz / Legion** | Prototyp | **Vollständig** | Balanciert | Polished | Release-Stand |
| **Evolvierte Fraktion** | – | – | **Vollständig (B-04–B-09)** | Balanciert | Release-Stand |
| **Pathfinding** | Flow-Field Spike | 500 Einheiten | 500+ Formationen | Multi-Threaded | Release-Stand |
| **Asset-Pipeline (D-054)** | B-01/B-02/B-10 | CC0 + KI-Drafting | B-04–B-09 Eigenbau | Full Asset Pass | Gold Master |
| **Karten & Biome** | 1 Test-Grid | 1 Map / 1 Biom | 3 Maps / 3 Biome | 12 Maps / 10 Biome | Release-Stand |
| **Multiplayer** | SimRunner Local | Local vs. KI | Command-Relay (2–4P) | Opt-in Telemetrie | Steam P2P/Relay |
| **Singleplayer-Kampagne** | – | – | – | Skirmish-Story | Full SP Campaign |

---

## 4. Offene Punkte

- Q-038 muss den tatsächlichen MVP-Scope ratifizieren.
- Die alten MS-1- bis MS-4-Umfänge sind bis zur Rebaseline suspendiert.

## Nächste Schritte

1. [MVPRecoveryPlan.md](MVPRecoveryPlan.md) ab G0 sequenziell ausführen.
2. Erst nach bestandenem G5 einen Alpha-Meilenstein neu planen.

---

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-24 | Erstfassung Sprint 6: Meilensteine MS-0 bis MS-4 definiert, Qualitäts-Gates und Feature-Matrix verankert | Producer / Game Director |
| 1.1.0 | 2026-07-24 | Tatsächlichen Nachweisstatus ergänzt; MS-0/MVP/Alpha durch D-055 zurückgestuft und Zukunftsscope suspendiert | Producer / Game Director |
