# Risikoanalyse

**Version:** 1.7.0 | **Status:** aktiv (laufend) | **Verantwortungsbereich:** Executive Producer / Lead Technical Director | **Sprint:** 7

## Zweck

Lebendes Risikoregister für das Gesamtprojekt. Wird am Ende jedes Sprints aktualisiert: neue Risiken aufnehmen, Eintrittswahrscheinlichkeit/Auswirkung neu bewerten, Maßnahmen wirksamkeitsprüfen.

## Abhängigkeiten

- [../analysis/KnowledgeBase.md](../analysis/KnowledgeBase.md)
- [../analysis/PriorityList.md](../analysis/PriorityList.md)
- Sprint-Berichte unter [sprints/](sprints/)

## Bewertungsskala

Wahrscheinlichkeit (W) und Auswirkung (A): niedrig / mittel / hoch. Risikowert = Kombination, sortiert absteigend.

## Risikoregister

| ID | Risiko | W | A | Beschreibung | Gegenmaßnahmen | Status |
|---|---|---|---|---|---|---|
| R-01 | Scope-Explosion | mittel | hoch | AAA-Anspruch bei ~54 Gebäuden, 24 Infanterie, 36 Fahrzeugen, 21 Lufteinheiten, 10 Biomen übersteigt jede realistische Einzelstudio-Kapazität um Größenordnungen. | Strikte MVP-Disziplin (TPD §14), Phasenmodell, Scope-Entscheidungen in Sprint 2 mit Kapazitätsrealität, "Feature kommt rein, wenn Kern stabil" | teilentschärft (Sprint 2: Scope beziffert – 36 Gebäude-Assets statt 54, 9 Eliten statt 15, Marine/Handel/Drohnen-Inflation gestrichen; Restrisiko: Umsetzungs-Disziplin) |
| R-02 | Multiplayer-Architektur zu spät entschieden | niedrig | hoch | TPD sagt "MP erst nach Singleplayer-Kern" – aber Simulationsmodell (Q-013) und Determinismus-Anforderung müssen **vor** dem Gameplay-Code stehen, sonst teurer Rewrite. | Entscheidung Q-013 spätestens Sprint 3; Simulation von Anfang an determinismusfähig strukturieren; Research Sprint 1 | entschärft (D-033: determinismus-fähige Command-Simulation verbindlich; MP = Transport-Thema, kein Rewrite) |
| R-03 | Pathfinding skaliert nicht | niedrig | hoch | 100–500+ Einheiten, Formationen, dynamische Hindernisse (zerstörbare Umgebung!) überfordern naive Lösungen; größtes Risiko laut TPD Phase 0. | Research Sprint 1 (Q-014); Performance-Spike in TPD Phase 0 mit echten Zahlen; Budgets in Sprint 3 | reduziert (D-034 entschieden: Flow-Field-Hybrid; Restrisiko nur noch Phase-0-Budget-Messung) |
| R-04 | Visuelle Inkohärenz gekaufter Assets | mittel | mittel | Asset-Store-Mix aus vielen Quellen sieht selten wie ein Spiel aus; widerspricht "Stylized Military Sci-Fi"-Anspruch. | Art-Direction-Dokument in Sprint 2; Stil-Kompatibilität als Kaufkriterium (TPD §7.3); Signature-Assets als Stil-Anker; **Asset Audit Sprint 5: D-053 ein Stil-Anker (Synty-Polygon-Look) + einheitlicher URP-Material-Standard mit Teamfarben-Masken; kohärenzkritische/Signature-Assets als BUILD klassifiziert** ([../assets/ProcurementStrategy.md](../assets/ProcurementStrategy.md) §2, [../assets/AssetRegister.md](../assets/AssetRegister.md)) | mitigiert (Strategie + Material-Standard stehen; Restrisiko = Umsetzungsdisziplin bei realem Kauf) |
| R-05 | Zerstörbare Umgebung als versteckter Kostentreiber | niedrig | hoch | "Vollständig zerstörbare Sci-Fi-Umgebung" (Vision) multipliziert Aufwand für Pathfinding (dynamische Hindernisse), Netcode (Zustand), VFX und Level-Design. | Feature in Sprint 2 auf realistischen Umfang spezifizieren (was genau ist zerstörbar?); nicht implizit mitplanen | entschärft (D-012: gezielte Zerstörbarkeit, keine Terrain-Deformation; Vision.md präzisiert) |
| R-06 | Living-Docs-Disziplin bricht ein | mittel | mittel | Dokumentation veraltet, sobald Entscheidungen nicht zurückfließen – genau das, was das Studio vermeiden will. | Pflichtabschnitte + Änderungsverlauf erzwingen (D-005); Sprint-Ritual prüft Dokusynchronität; DecisionLog als Single Source of Truth | aktiv |
| R-07 | Lizenz-/Kostenfallen im Asset Store | mittel | mittel | Kommerzielle Nutzung, Seat-Lizenzen, URP-Kompatibilität können Budget und Veröffentlichung gefährden. | **Sprint 5: Lizenz-Register [../assets/Licenses.md](../assets/Licenses.md) angelegt** (Lizenzrahmen je Quelle, Seat-/Attribution-/Weitergabe-Regeln, Repo-Hygiene); Kaufprüf-Checkliste ([../tech/AssetBudget.md](../tech/AssetBudget.md) §6) verbindlich; URP als K.O.-Kriterium (D-053) | mitigiert (Register + Regeln stehen; Restrisiko = Seat-Planung Q-036 + Budget-Obergrenze Q-035, Inhaberentscheidung) |
| R-08 | WebGL-Randbedingung verbaut Architektur | niedrig | mittel | "WebGL nicht ausschließen" kann zu vorschnellen Einschränkungen führen (Threading, Dateisystem, Speicher). | WebGL ist explizit keine Leitplattform (TPD §5.4); Architekturentscheidungen dokumentieren, wo sie WebGL betreffen; Bewertung erst nach Desktop-Vertical-Slice | aktiv |
| R-09 | Bestätigungsfehler durch Quellenlage | mittel | mittel | Alle Quelldokumente stammen aus einer Hand; kein externer Realitätscheck (Markt, Machbarkeit) erfolgt bisher. | Sprint 1: unabhängige Research-Agenten inkl. Wettbewerbsanalyse; Sprint 4: Review-Agenten mit ausdrücklichem Widerspruchs-Mandat | teilentschärft (Sprint 1 hat externe Markt-/Technikdaten geliefert) |
| R-10 | Geschäftsmodell-Fehlgriff (Server-MP als Fundament) | mittel | hoch | Stormgate (~$40 Mio. Funding) hat im April 2026 den Online-MP abgeschaltet; F2P-/MP-getriebene RTS scheitern wiederholt. Project Nova darf sein Fundament nicht auf Server-MP bauen. | Premium, Singleplayer/Skirmish-first (Markt-Research, Vorlage Q-016); MP als Feature nach stabilem SP-Kern, nicht als Geschäftsmodell | aktiv |
| R-11 | Unity-Reputations-/Plattformrisiko | niedrig | mittel | Runtime-Fee-Debakel 2023/24 hat Vertrauen beschädigt; Engine-Abhängigkeit bleibt ein strategisches Risiko auch nach Streichung der Fee. | Simulationskern Unity-unabhängig halten (Architekturregel aus Research); keine proprietären Unity-Services im Kern; LTS-Pinning | aktiv |
| R-12 | Burst/Managed-Paritätsbruch | niedrig | mittel | D-037 führt zwei Implementierungen der Sim-Hotspots (Managed-Referenz + Burst); weichen sie ab, laufen SimRunner/CI und Live-Build auseinander (stille Desync-Quelle). | Pflicht-Hash-Paritätstests in CI (D-037); Burst nur für benannte Hotspots; Re-Eval nach Phase-0-Messung – Managed reicht ggf., Burst entfällt | aktiv (mitigiert, präzisiert durch D-045 Toleranz-Parität) |
| R-13 | Schlüsselperson-/Bus-Faktor-Risiko | hoch | hoch | Das Projekt wird primär von einer Person zusammen mit KI-Coding-Agenten getragen – kein Team, keine personelle Redundanz in Fachwissen, Entscheidungsfindung oder Review-Kapazität. Ausfall/Verfügbarkeitslücke der Schlüsselperson träfe Planung, Freigaben und Qualitätssicherung gleichzeitig. | Living-Docs-Disziplin (DecisionLog, Wiki) als externalisiertes Wissen statt Kopfwissen; mehrere unabhängige Review-/Widerspruchs-Agenten (Sprint 4) als Teil-Substitut für Peer-Review; Kapazitäts-/Vertretungsplanung vor Produktionsstart (Sprint 6/7) nachholen. | neu, unmitigiert |
| R-14 | Cross-Plattform-Determinismus-Annahme (ARM↔x86) scheitert im Phase-0-Spike | mittel | hoch | Die gesamte MP-Architektur (D-033 Lockstep/Command-Relay, D-037/D-045 Managed-Burst-Parität, D-046 Post-Match-Re-Sim & Trust-Anchor) setzt bit- bzw. toleranzgenauen Determinismus zwischen x86 (Windows-Referenzhardware, D-052) und Apple-Silicon-ARM (Mac-Baseline M2, D-052) voraus. Float-Determinismus über Prozessorarchitekturen hinweg (FMA-Instruktionen, Compiler-/JIT-Optimierungen, Rundungsdrift) ist ein historisch hartes Problem; ein Scheitern des Q-033/Phase-0-Spikes würde D-033/D-037/D-046 nachträglich in Frage stellen. | Phase-0-Spike-Pflichtvalidierung "Fixed-Point-Determinismus ARM↔x86" vor Sprint-7-Start (V1-Gate); D-045 (Managed-first, Toleranz-Parität ≤1e-4) begrenzt den Schaden vorläufig; bei Scheitern definierter Eskalationspfad nötig (Fixed-Point-Pfad vorziehen = Mehraufwand, oder Cross-Play/Ranked-Scope einschränken). | neu, Restrisiko hoch bis zur Spike-Messung |
| R-15 | Fehlerhafter KI-generierter Code in der desync-kritischen Lockstep-Simulation | mittel | hoch | Die Implementierung erfolgt primär durch KI-Coding-Agenten; ein einzelner nicht-deterministischer Fehler im Sim-Kern (z. B. Dictionary-/Set-Iterationsreihenfolge, versehentliche Float- statt Fixed-Operation, unbeabsichtigte UnityEngine-API-Nutzung im Sim-Pfad, GC-Allokation im Tick) erzeugt stille, schwer reproduzierbare Desyncs, die oft erst spät (Multiplayer-Beta, Golden-Master-Langlauf) auffallen. | CI-Determinismus-Gates (V1-Gate, Golden-Master-Hashes, SimRunner-Nightlies mit Sharding, D-049); harte CodingGuidelines-Verbote (kein GC im Tick, keine UnityEngine-APIs im Sim-Pfad, `noEngineReferences`); Burst/Managed-Paritätstests (D-037/D-045); verbindliche Code-Review-Pflicht vor jedem Merge in `Nova.Simulation`. | neu, mitigiert durch Gates – Restrisiko bleibt wegen KI-Anteil an der Implementierung |
| R-16 | Zeit-/Kapazitätsrisiko: keine belastbare Schätzbasis | hoch | hoch | Die Sprint-6-Schätzung von 445 PT wurde ohne gemessenen Durchsatz, validierten MVP-Scope oder belastbare Implementierungsbasis erstellt. | Roadmap durch D-055 entfristet; Aufwand erst nach Q-038 und gemessenen Recovery-Gates neu schätzen. | aktiv – Rebaseline erforderlich |
| R-17 | Falsche Fertigmeldung durch strukturorientierte KI-Implementierung | hoch | hoch | Dateien, Typen und isolierte Tests wurden als fertige Module, MVP- oder Alpha-Fortschritt gewertet, obwohl Laufzeitintegration und Akzeptanznachweise fehlten. | D-055; [Implementierungs-Audit](ImplementationAudit_2026-07-24.md); Gates G0–G5 mit reproduzierbarer Evidenz und unabhängiger Prüfung. | aktiv |

## Offene Punkte

- R-16 ist wieder aktiv: Die 445-PT-Zahl ist keine belastbare Kapazitätsmitigation.
- R-17 ist der unmittelbare Recovery-Auslöser; Status folgt künftig nur aus bestandenen Gates.
- R-13 (Bus-Faktor) bleibt aktiv und wird durch die Open-Source Community- und Volunteer-Organisation in Phase 0/1 begleitet.
- R-14 (Cross-Plattform-Determinismus) und R-15 (KI-Code-Fehler) werden in MS-0 (Phase 0 Spike) empirisch gemessen.

## Nächste Schritte

- Gate G0 schließen; anschließend R-14 und R-03 innerhalb der Gates G1/G2 mit reproduzierbaren Laufzeitnachweisen messen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-21 | Initiales Risikoregister R-01 bis R-09 (Sprint 0) | Executive Producer |
| 1.1.0 | 2026-07-21 | R-10, R-11 neu; R-09 teilentschärft; Fortschritt R-02/R-03 (Sprint 1) | Executive Producer |
| 1.2.0 | 2026-07-21 | R-01 gesenkt (mittel/hoch), R-05 entschärft (Sprint-2-Scope-Entscheidungen D-007–D-032) | Executive Producer |
| 1.3.0 | 2026-07-21 | R-02 entschärft (D-033), R-03 reduziert (D-034), R-12 neu (Burst/Managed-Parität, D-037) – Sprint 3 | Executive Producer |
| 1.4.0 | 2026-07-21 | R-13, R-14, R-15, R-16 neu aufgenommen – Sprint-4-Korrekturlauf | Executive Producer |
| 1.5.0 | 2026-07-22 | R-04 und R-07 auf „mitigiert" gesenkt – Sprint 5 (Asset Audit) | Executive Producer |
| 1.6.0 | 2026-07-24 | R-16 (Zeit-/Kapazitätsrisiko) auf „mitigiert" gesenkt – Sprint 6 (Roadmap.md & Milestones.md, 445 PT) | Executive Producer |
| 1.7.0 | 2026-07-24 | R-16 wegen unbelegter Schätzbasis reaktiviert; R-17 für KI-bedingte falsche Fertigmeldungen aufgenommen | Executive Producer / Lead Technical Director |
