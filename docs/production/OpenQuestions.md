# Open Questions

**Version:** 1.11.2 | **Status:** aktiv (laufend) | **Verantwortungsbereich:** Executive Producer | **Sprint:** 7

## Zweck

Zentrales Register aller offenen Fragen mit Owner-Sprint und Priorität. Eine Frage gilt als geschlossen, wenn die Entscheidung im [DecisionLog.md](DecisionLog.md) steht und die betroffenen Dokumente aktualisiert sind.

## Abhängigkeiten

- [../analysis/Inconsistencies.md](../analysis/Inconsistencies.md) (Q-001–Q-012)
- [../analysis/PriorityList.md](../analysis/PriorityList.md)
- [DecisionLog.md](DecisionLog.md) – D-056 bis D-061 schließen die
  Recovery-Fragen

## Offene Fragen

| ID | Prio | Frage | Herkunft | Owner-Sprint | Status |
|---|---|---|---|---|---|
| Q-018 | P3 | Preispunkt: 29,99 / 34,99 / 39,99 €? Markt-Research deckt das nicht ab. | Sprint-2-Review | Post-MVP | offen – nicht MS-1-blockierend |
| Q-019 | P2 | Telemetrie-Infrastruktur: eigenes Opt-in-Backend oder Streichung? D-007-Offline-Positionierung beachten. | Balancing.md-Review | Post-MVP | offen – in D-056 zurückgestellt |
| Q-040 | P2 | G1-Detailfragen, die SimulationCore.md nicht festlegt: (a) `SimFixed.ToInt()`-Rundung (Provisorium: Truncation Richtung 0), (b) `SimAngle`-Einheit (Provisorium: Grad, 360° = 65536), (c) PRNG-Seeding und 64→32-bit-Ausgabereduktion (Provisorium: SplitMix64 + High-32), (d) PRNG-State-Serialisierung im `ISimRandom`-Interface für §3-Snapshots, (e) `EntityId`-Layout (Spec §1: gepacktes `uint32`, 10 bit Index + 22 bit Generation; Bestand: `int` + `ushort` getrennt — Umstellung in G1-Integration), (f) String-Kodierung im Hash-Writer (Provisorium: UTF-8 mit uint32-Längenpräfix), (g) Snapshot-Container v1 ohne Datei-Hash (Serialization.md §2 Punkt 7 sieht einen vor; Provisorium: Verzicht, weil State-Hash plus exakte Längenarithmetik Integrität und Truncation bereits abdecken — Datei-Hash wäre redundant), (h) Snapshot-Container v1 mit Major-only `FormatVersion u16` statt `Major u16 + Minor u16` (Serialization.md §1; Provisorium: Major-only, Minor = implizit 0). Vor dem G1-Schema-Freeze per D-ID zu ratifizieren. | G1-Vorarbeit Numerik-/Hash-Kern | Sprint 7 (vor G1-Freeze) | offen – G1-blockierend, mit dokumentierten Provisorien |

## Geschlossene Fragen

| ID | Entscheidung | Geschlossen |
|---|---|---|
| Q-001 | **D-008** – 12 Gebäudetypen/Fraktion als Vollspiel-Ziel; **D-056** aktiviert 9 in MS-1 | Sprint 2 / Rebaseline |
| Q-002 | **D-009**, für MS-1 teilweise ersetzt durch **D-056** – Commander-Identitätslayer ohne Match-Mechanik bleibt Vollspielziel; Portrait, Voice und Commander-Code sind Post-MVP | Sprint 2 / Rebaseline |
| Q-003 | **D-013** – Marine gestrichen, Wasser nur Terrain-Feature | Sprint 2 |
| Q-004 | **D-014** – Drohnen als Post-MVP-Ziel; **D-056** deaktiviert sie in MS-1 | Sprint 2 / Rebaseline |
| Q-005 | **D-010** – Aetherium-Hybrid: endlicher Mutterkristall + Nachwachsen + Ausbreitung + Überernte | Sprint 2 |
| Q-006 | **D-015** – Eliten als Post-MVP-Ziel; **D-056** deaktiviert T3/Eliten in MS-1 | Sprint 2 / Rebaseline |
| Q-007 | **D-016** – Vollspiel-Neutrale; **D-056** deaktiviert Neutrale/Capture in MS-1 | Sprint 2 / Rebaseline |
| Q-008 | **D-019** – schräge Top-Down-Perspektive, "isometrisch" ersetzt | Sprint 2 |
| Q-009 | **D-011** – Evolvierte: Keim→Reifung, Aetherium-Beschleunigung, Regeneration | Sprint 2 |
| Q-010 | **D-017** – Biome als Themen, Karten-Roadmap 1/4/8/12, Größen S/M/L | Sprint 2 |
| Q-011 | **D-018 + D-025** – Modi-Staffelung; Alpha-FFA lokal vs. KI, Online ab Beta | Sprint 2 |
| Q-012 | **D-017 + D-028** – Wetter pro Biom; Mond Strahlung, Mars Staub | Sprint 2 |
| Q-013 | **D-033 + D-057** – Command-Simulation; ab G1 kanonisches Fixed-Point, Managed-Pfad, exakte Plattformparität | Sprint 3 / Rebaseline |
| Q-014 | **D-034** – Integer-Grid 1 m + Flow Fields + lokale Vermeidung (ORCA ab Alpha), Budget ≤4 ms | Sprint 3 |
| Q-015 | **D-035** – MonoBehaviour-OOP + SO + Burst/Jobs-Hotspots, Unity-freie `Nova.Simulation`, kein Entities im MVP | Sprint 3 |
| Q-016 | **D-007** – Premium SP/Skirmish-first, H1 C&C-Nostalgiker primär | Sprint 2 |
| Q-017 | **D-012** – gezielte Zerstörbarkeit, keine Terrain-Deformation | Sprint 2 |
| Q-020 | **D-036** – `Nova.SimRunner` (.NET-Konsole auf Nova.Simulation) für KI-vs-KI-CI-Läufe | Sprint 3 |
| Q-021 | **D-043** – Kanonische Assembly-Topologie (Neusynthese `Nova.Core`/`Nova.Simulation`/`Nova.Simulation.Burst`/`Nova.AI`/`Nova.AI.Data`/…) statt drei konkurrierender Modelle | Sprint 4 |
| Q-022 | **D-044, V5-Sequenz teilweise ersetzt durch D-061** – MS-1 synchron; V5a vor G2, V5b in G3 | Sprint 4 / Rebaseline |
| Q-023 | **D-045, teilweise ersetzt durch D-057** – MS-1 shippt Managed; Burst aus, bis exakte Feld-/Hash-/Byteparität bewiesen ist | Sprint 4 / Rebaseline |
| Q-024 | **D-046** – MP-Trust-Anchor: Post-Match-Re-Simulation + Hash-Kette für Reconnect + deterministische, tick-synchrone KI-Übernahme (kein SPOF) | Sprint 4 |
| Q-025 | **D-047** – Reichweiten-Harmonisierung GDD↔TDD: 1 Tile = 1 m, Weapons.md führend, Vehicles.md/Aircraft.md angeglichen | Sprint 4 |
| Q-026 | **D-048** Post-MVP; **D-058** – MS-1-Produktion 100 Einheiten, 500 nur synthetische Architekturreserve | Sprint 4 / Rebaseline |
| Q-027 | **D-049, Kadenz teilweise ersetzt durch D-061** – xxHash64 und Registry-Sharding bleiben; Nightly/Weekly folgen 40/400 | Sprint 4 / Rebaseline |
| Q-028 | **D-059 ersetzt D-050** – geschütztes `main`, kurze Topic-Branches, Squash/linear, kein dauerhafter Integrationsbranch | Sprint 4 / Rebaseline |
| Q-029 | **D-051** – Photon-Quantum-Fallback gestrichen; Beta-Fallback = reduzierter MP-Scope (4 Spieler/300 Einheiten/EU-only) | Sprint 4 |
| Q-030 | **D-052** – Windows-Referenzhardware fixiert (Ryzen 5 5600/RTX 3060 = 60-FPS-Ziel; Ryzen 3 3100/GTX 1050 Ti = 30-FPS-Ziel; Mac-Baseline M2) | Sprint 4 |
| Q-035 | **D-054** – Asset-Budget-Obergrenze 0 €; Open-Source-/KI-Pipeline | Sprint 5 |
| Q-036 | Durch D-054 gegenstandslos: keine Seat-Lizenzen im 0-€-Beschaffungsmodell | Sprint 5 |
| Q-037 | Durch D-054 gegenstandslos: keine Store-/Bundle-Käufe im 0-€-Beschaffungsmodell | Sprint 5 |
| Q-031 | **D-056** – kein generisches Fähigkeiten-/Status-/Kanal-/Aura-System in MS-1; Identität lokal in Waffen/Wirtschaft | Sprint 7 |
| Q-032 | **D-058** – feste Slots/Entity-/Snapshot-/Cache-Kappen und deterministische Eviction | Sprint 7 |
| Q-033 | **D-061** – V5a vor G2 und V5b mit realem Combat/KI in G3; ausführbare Schwellen | Sprint 7 |
| Q-034 | **D-061** – substantive Tech-Verträge `SimulationCore`, `Commands`, `FogOfWar`, `CameraSystem` und bereinigte Links | Sprint 7 |
| Q-038 | **D-056** – dependency-closed Allianz-vs.-Legion-Skirmish auf Glutrinne | Sprint 7 |
| Q-039 | **D-057** – kanonisches Q16.16-Fixed-Point und exakte Plattformparität ab G1 | Sprint 7 |

## Regeln

- Neue Fragen erhalten die nächste freie ID und werden nie wiederverwendet.
- Bei Sprint-Abschluss wird geprüft, welche Fragen fällig waren und ob sie geschlossen sind.

## Offene Punkte

- **Q-018 und Q-019 bleiben offen:** Sie sind durch D-056 Post-MVP und
  blockieren G0–G5 nicht.
- **Q-040 ist offen und G1-blockierend:** Die G1-Vorarbeit hat dokumentierte
  Provisorien gesetzt (ToInt-Truncation, SimAngle-Grad, SplitMix64-Seeding);
  vor dem G1-Schema-Freeze ist eine eigene Entscheidung (D-ID) fällig.
- Q-031–Q-034 und Q-038/Q-039 sind in D-056–D-061 geschlossen; ihre
  Implementierung ist weiterhin über die Gates nachzuweisen.

## Nächste Schritte

- Gate G0 des [MVP-Recovery-Plans](MVPRecoveryPlan.md) herstellen.
- Q-018/Q-019 erst nach MS-1 mit mindestens drei Alternativen entscheiden.

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
| 1.10.0 | 2026-07-24 | Q-031–Q-034 und Q-038/Q-039 durch D-056–D-061 geschlossen; Q-018/Q-019 als nicht MS-1-blockierend eingeordnet | Executive Producer |
| 1.10.1 | 2026-07-24 | Teilersetzungen D-044/D-049 durch D-061 in den geschlossenen Fragen sichtbar gemacht | Executive Producer |
| 1.10.2 | 2026-07-24 | Q-002 an die MS-1-Teilersetzung von D-009 durch D-056 angeglichen | Executive Producer |
| 1.11.0 | 2026-07-25 | Q-040 neu: G1-Numerik-Detailfragen (ToInt, SimAngle-Einheit, PRNG-Seeding/-Serialisierung) mit dokumentierten Provisorien, G1-blockierend | Executive Producer |
| 1.11.1 | 2026-07-25 | Q-040 um EntityId-Layout (gepacktes uint32 vs. Bestand) und Hash-Writer-String-Kodierung aus der G1-Hash-Vorarbeit erweitert | Executive Producer |
| 1.11.2 | 2026-07-25 | Q-040 um Snapshot-Container-v1-Abweichungen erweitert: Verzicht auf Datei-Hash (Redundanz zu State-Hash + Längenarithmetik) und Major-only-FormatVersion | Executive Producer |
