# Open Questions

**Version:** 1.9.0 | **Status:** aktiv (laufend) | **Verantwortungsbereich:** Executive Producer | **Sprint:** 7

## Zweck

Zentrales Register aller offenen Fragen mit Owner-Sprint und Priorität. Eine Frage gilt als geschlossen, wenn die Entscheidung im [DecisionLog.md](DecisionLog.md) steht und die betroffenen Dokumente aktualisiert sind.

## Abhängigkeiten

- [../analysis/Inconsistencies.md](../analysis/Inconsistencies.md) (Q-001–Q-012)
- [../analysis/PriorityList.md](../analysis/PriorityList.md)
- [DecisionLog.md](DecisionLog.md)

## Offene Fragen

| ID | Prio | Frage | Herkunft | Owner-Sprint | Status |
|---|---|---|---|---|---|
| Q-018 | P3 | Preispunkt: 29,99 / 34,99 / 39,99 €? Markt-Research deckt das nicht ab. | Sprint-2-Review | 6 | offen |
| Q-019 | P2 | Telemetrie-Infrastruktur (Opt-in, ab Beta, für Balancing-Stufe 5): Pflicht-Feature mit eigenem Backend oder Streichung? D-007-Offline-Positionierung beachten. | Balancing.md-Review | 6 | offen (Vorhaltung: Compile-Schaltstelle in tech/Deployment.md) |
| Q-031 | P1 | Fähigkeiten-/Status-Effekt-System (4 Status-Effekte, ~40 Fähigkeiten mit Cooldowns/Kanälen) hat kein Sim-Modul-Zuhause – `EffectState`/`AbilityState` fehlen in GameState.md, kein Modul in ModuleOverview.md. | Review_GDD-TDD-Konsistenz F-02 | 5 (vor Sprint-7-Start) | offen |
| Q-032 | P2 | MemoryBudget-Abgleich: Snapshot-/Grid-Layer-Größe (Serialization.md 50–150 kB vs. GameState.md-Layer-Rechnung ~1–2 MB), Flow-Field-Eviction-Policy (LRU vs. RefCount widersprüchlich), native Speicher-Baseline (macOS 8 GB ungemessen), max. Spielerzahl (6 vs. 8 uneinheitlich). | Review_Skalierung_Systemgrenzen F-2/F-4/F-5/F-12/F-13, Review_Performance F-12 | 5 (vor Phase-0-Spike) | offen |
| Q-033 | P1 | Phase-0-Spike-Erweiterung V5-Gate: Kampf-/KI-Kostenmodell (Zielsuche mit Spatial Hash, Projektil-/AoE-Kosten, KI-Command-Verarbeitung) für Rest-Sim-Unterbudget ≤3 ms – Gate durch D-044 beschlossen, Kampf-Subsystem-TDD mit Kostenmodell noch nicht geschrieben. | D-044, Review_Performance F-1/F-4/F-5 | Phase 0 (Pflicht vor Sprint-7-Kampfmodul-Start) | offen |
| Q-034 | P3 | Tote interne Verweise auf nicht existierende Tech-Dokumente (FogOfWar.md, CameraSystem.md, Commands.md, SimulationCore.md) – FoW-TDD hat höchste fachliche Priorität. | Review_GDD-TDD-Konsistenz F-16 | 6 | offen (in Sprint 5 geprüft: TDD-Authoring, **kein** Asset-Audit-Thema; Owner auf Sprint 6 präzisiert) |
| Q-038 | P0 | Welcher MVP-Zuschnitt wird verbindlich: (A) alter Vollumfang, (B) empfohlenes fokussiertes 2-Fraktionen-Skirmish oder (C) symmetrischer Technik-Demonstrator? | D-055, MVPRecoveryPlan.md | vor G4 | offen – Inhaberentscheidung |
| Q-039 | P0 | Gilt Festkomma deterministisch im kanonischen GameState ab G1 oder erst zur Beta? D-033 und MS-0 verlangen Ersteres, D-045 beschreibt eine spätere Migration. | D-033, D-045, Audit F-004 | vor G1-Abschluss | offen |

## Geschlossene Fragen

| ID | Entscheidung | Geschlossen |
|---|---|---|
| Q-001 | **D-008** – 12 Gebäudetypen/Fraktion (Verteidigung als Modulsystem, inkl. Mauer) | Sprint 2 |
| Q-002 | **D-009** – Commander als Identitäts-Layer, keine Match-Mechanik im MVP | Sprint 2 |
| Q-003 | **D-013** – Marine gestrichen, Wasser nur Terrain-Feature | Sprint 2 |
| Q-004 | **D-014** – 2–3 Support-Drohnen/Fraktion, Produktion in bestehenden Fabriken | Sprint 2 |
| Q-005 | **D-010** – Aetherium-Hybrid: endlicher Mutterkristall + Nachwachsen + Ausbreitung + Überernte | Sprint 2 |
| Q-006 | **D-015** – 1 Elite/Fraktion (MVP), 3 (Release), Tier 3, hartes Limit | Sprint 2 |
| Q-007 | **D-016** – kein Handelssystem; Neutrale = Critters, Lager, capturebare Türme | Sprint 2 |
| Q-008 | **D-019** – schräge Top-Down-Perspektive, "isometrisch" ersetzt | Sprint 2 |
| Q-009 | **D-011** – Evolvierte: Keim→Reifung, Aetherium-Beschleunigung, Regeneration | Sprint 2 |
| Q-010 | **D-017** – Biome als Themen, Karten-Roadmap 1/4/8/12, Größen S/M/L | Sprint 2 |
| Q-011 | **D-018 + D-025** – Modi-Staffelung; Alpha-FFA lokal vs. KI, Online ab Beta | Sprint 2 |
| Q-012 | **D-017 + D-028** – Wetter pro Biom; Mond Strahlung, Mars Staub | Sprint 2 |
| Q-013 | **D-033 + D-037 + D-038** – determinismus-fähige Command-Simulation (5 Regeln), Lockstep über Command-Relay ab Beta; Burst/Managed-Doppelstruktur; Disconnect-Regel final | Sprint 3 |
| Q-014 | **D-034** – Integer-Grid 1 m + Flow Fields + lokale Vermeidung (ORCA ab Alpha), Budget ≤4 ms | Sprint 3 |
| Q-015 | **D-035** – MonoBehaviour-OOP + SO + Burst/Jobs-Hotspots, Unity-freie `Nova.Simulation`, kein Entities im MVP | Sprint 3 |
| Q-016 | **D-007** – Premium SP/Skirmish-first, H1 C&C-Nostalgiker primär | Sprint 2 |
| Q-017 | **D-012** – gezielte Zerstörbarkeit, keine Terrain-Deformation | Sprint 2 |
| Q-020 | **D-036** – `Nova.SimRunner` (.NET-Konsole auf Nova.Simulation) für KI-vs-KI-CI-Läufe | Sprint 3 |
| Q-021 | **D-043** – Kanonische Assembly-Topologie (Neusynthese `Nova.Core`/`Nova.Simulation`/`Nova.Simulation.Burst`/`Nova.AI`/`Nova.AI.Data`/…) statt drei konkurrierender Modelle | Sprint 4 |
| Q-022 | **D-044** – Sim-Tick-Ausführungsmodell gestuft (MVP synchron, Worker-Tick ab Alpha bei P95 >6 ms) + Pflicht-Gate V5 (Kampf-/KI-Kostenmodell) | Sprint 4 |
| Q-023 | **D-045** – Managed-first Auslieferungspfad bis Fixed-Point-Beta; Burst nur hinter Feature-Flag mit Toleranz-Parität (≤1e-4) | Sprint 4 |
| Q-024 | **D-046** – MP-Trust-Anchor: Post-Match-Re-Simulation + Hash-Kette für Reconnect + deterministische, tick-synchrone KI-Übernahme (kein SPOF) | Sprint 4 |
| Q-025 | **D-047** – Reichweiten-Harmonisierung GDD↔TDD: 1 Tile = 1 m, Weapons.md führend, Vehicles.md/Aircraft.md angeglichen | Sprint 4 |
| Q-026 | **D-048** – Skalierungs-Deckel: 600 Einheiten/Match global, Survival-Abflachung ab Welle 25, `AetheriumDensity` ≤1,5 bei 5–6 Spielern | Sprint 4 |
| Q-027 | **D-049** – Test-/CI-Realismus (Shard-Modell SimRunner-Nightly), xxHash64 durchgängig, GameDatabase-Sharding pro Kategorie | Sprint 4 |
| Q-028 | **D-050** – Branching-Modell gestuft: `main` + Kurz-Branches bis Sprint 6, TPD-§12-Vollmodell ab Sprint 7 | Sprint 4 |
| Q-029 | **D-051** – Photon-Quantum-Fallback gestrichen; Beta-Fallback = reduzierter MP-Scope (4 Spieler/300 Einheiten/EU-only) | Sprint 4 |
| Q-030 | **D-052** – Windows-Referenzhardware fixiert (Ryzen 5 5600/RTX 3060 = 60-FPS-Ziel; Ryzen 3 3100/GTX 1050 Ti = 30-FPS-Ziel; Mac-Baseline M2) | Sprint 4 |
| Q-035 | **D-054** – Asset-Budget-Obergrenze 0 €; Open-Source-/KI-Pipeline | Sprint 5 |
| Q-036 | Durch D-054 gegenstandslos: keine Seat-Lizenzen im 0-€-Beschaffungsmodell | Sprint 5 |
| Q-037 | Durch D-054 gegenstandslos: keine Store-/Bundle-Käufe im 0-€-Beschaffungsmodell | Sprint 5 |

## Regeln

- Neue Fragen erhalten die nächste freie ID und werden nie wiederverwendet.
- Bei Sprint-Abschluss wird geprüft, welche Fragen fällig waren und ob sie geschlossen sind.

## Offene Punkte

- **Q-018 und Q-019 bleiben offen:** Die Sprint-6-Dokumente behaupteten Entscheidungen, ohne sie mit einer D-ID und den vorgeschriebenen Alternativen zu protokollieren.
- Neu aus dem Sprint-4-Korrekturlauf offen: Q-031 (Fähigkeiten-/Status-Effekt-System, vor Sprint 7), Q-032 (MemoryBudget-Abgleich, vor Phase-0-Spike), Q-033 (V5-Gate Kampf-/KI-Kostenmodell, Phase 0), Q-034 (tote Doku-Verweise – sequenziert für Sprint 7 Refactoring).
- Recovery-Blocker: Q-038 legt den verbindlichen MVP-Zuschnitt fest; Q-039 löst den Festkomma-Widerspruch.
- Aus Research hängen vier Pflicht-Validierungen am Phase-0-Spike (MS-0 in Sprint 7): Fixed-Point-Determinismus ARM↔x86, URP GPU Resident Drawer, Animator vs. Playables, Pathfinding-CPU-Budget.

## Nächste Schritte

- Zuerst Gate G0 des [MVP-Recovery-Plans](MVPRecoveryPlan.md) schließen.
- Danach Q-038 entscheiden und Q-039 vor Abschluss von G1 auflösen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-21 | Q-001 bis Q-015 eröffnet (Sprint 0) | Executive Producer |
| 1.1.0 | 2026-07-21 | Q-013–Q-015 Research-Status; Q-016, Q-017 neu aus Sprint 1 | Executive Producer |
| 1.2.0 | 2026-07-21 | Q-001–Q-012, Q-016, Q-017 entschieden (D-007–D-030); Q-018–Q-020 neu aus Konsistenzreview | Executive Producer |
| 1.3.0 | 2026-07-21 | Q-001–Q-012, Q-016, Q-017 formal geschlossen (Sprint-2-Abschluss, D-007–D-032) | Executive Producer |
| 1.4.0 | 2026-07-21 | Q-013, Q-014, Q-015, Q-020 formal geschlossen (Sprint 3, D-033–D-038) | Executive Producer |
| 1.5.0 | 2026-07-21 | Q-021–Q-030 neu und sofort geschlossen (Sprint 4, D-043–D-052); Q-031–Q-034 neu aus Review-Folgearbeit (Ability/Status-System, MemoryBudget-Abgleich, V5-Gate-Kostenmodell, tote Verweise) | Executive Producer |
| 1.6.0 | 2026-07-22 | Q-035/Q-036/Q-037 neu aus Sprint 5 (Asset-Budget-Obergrenze, Seat-Planung, Bundle-Fenster, D-053); Q-034 als TDD-Authoring präzisiert und auf Sprint 6 umterminiert | Executive Producer |
| 1.7.0 | 2026-07-24 | Q-035 geschlossen (Asset-Budget-Obergrenze = 0 €, D-054 Inhaberentscheidung) | Executive Producer |
| 1.8.0 | 2026-07-24 | Q-018 (Preispunkt 29,99–39,99 €) und Q-019 (Opt-in Telemetrie) geschlossen – Sprint 6 | Executive Producer |
| 1.9.0 | 2026-07-24 | Ungültige Schließung Q-018/Q-019 zurückgenommen; Q-038 MVP-Zuschnitt und Q-039 Fixed-Point-Konflikt eröffnet | Executive Producer |
