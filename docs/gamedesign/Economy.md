# Wirtschaftssystem (Economy)

**Version:** 0.6.0 | **Status:** Entwurf – MS-1-Override verbindlich | **Verantwortungsbereich:** Lead Gameplay Designer | **Sprint:** 16

## Zweck

Spezifiziert den Wirtschafts-Kreislauf von Hashkrieg: Sammler-Loop, Lagerkapazität, Energie mit Low-Power-Regel, Einkommensraten-Ziele, Kostenrahmen für Gebäude und Einheiten, Reparatur-/Verkaufsregeln, Zielkurven für 20–35-min-Matches (D-010) und Anti-Stall-Logik. Alle Werte sind Richtwerte v0.1 zum Tunen und als flache, ScriptableObject-taugliche Datensätze angelegt.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-008 (12 Gebäudetypen), D-010 (Hybridwirtschaft, Matchdauer), D-011 (Evolvierte-Wachstum/Regeneration), D-014 (Drohnen), D-015 (Elite-Einheiten), D-016 (Objective-Belohnungen), D-024 (Lager & Raffinerie), D-027 (Regenerations-Bonus), D-030 (Low-Power & Forschung), D-077 (MS-1-Startzustand), D-096/D-106 (abgeleitete AE-Grenze und Überhang), D-104 (kumulative MS-1-Reparaturkosten)
- [./Resources.md](./Resources.md) – Feldregeln, Nachwuchsraten, Überernte
- [./Factions.md](./Factions.md) – Fraktionsprofile (Allianz teuer/präzise, Legion günstig/Masse, Evolvierte organisch)
- [./Buildings.md](./Buildings.md) – **führend für Gebäudekosten, Energiewerte und Bauzeiten** (Review F-03, Grundsatzregel D-047); Bauvoraussetzungen, Evolvierte-Reifung, HQ-Grundenergie
- [./NeutralUnits.md](./NeutralUnits.md) – Objective-Belohnungen 400/800/1.200 AE (D-016)
- [./Infantry.md](./Infantry.md), [./Vehicles.md](./Vehicles.md), [./Aircraft.md](./Aircraft.md) – Einheiten-Details (geplant)
- [../analysis/KnowledgeBase.md](../analysis/KnowledgeBase.md) – Quellkontext (TPD §8.4)

## Ressourcenüberblick

| Ressource | Zweck | Quelle |
|---|---|---|
| AE (Aetherium-Einheiten) | Einzige Bau-/Produktionswährung | Ernte an Feldern, Objective-Belohnungen 400/800/1.200 AE je Lager-Stufe (D-016, siehe [./NeutralUnits.md](./NeutralUnits.md)) |
| Energie | Versorgt Gebäude; Defizit löst Low-Power aus | Kraftwerke |

MS-1-Startressourcen: **3.000 AE** (D-077), Start-Energie 0; das fertige HQ
liefert +30 Grundenergie. Ohne fertiges Lager liegt der Startbestand 1.000 AE
über der HQ-Basis und erzeugt damit sofort den Ausgaben-/Bauanreiz aus D-106.

## Sammler-Loop

`Feld (Ausläufer) → Harvester lädt 300 AE → Anfahrt Raffinerie → Andocken/Abladen → AE auf Konto → Rückkehr`

| Parameter | Wert v0.1 | Begründung |
|---|---|---|
| Harvester-Ladung | 300 AE (= Inhalt eines reifen Ausläufers) | Zahlengerüst; 1 Ausläufer = 1 Fahrt – sauber lesbar |
| Ladezeit | ~8 s am Ausläufer | Spürbare Verwundbarkeit am Feld ohne Micromanagement-Frust |
| Abladezeit | ~3 s an der Raffinerie | Raffinerie-Dock ist der Durchsatz-Engpass, nicht die Ladezeit |
| Docks pro Raffinerie | 1 (weitere Harvester warten in kurzer Queue, max. 3) | Skalierung läuft über mehr Raffinerien/Expansionen, nicht über Dock-Upgrades (MVP-Disziplin) |
| Harvester-Kosten | je Fraktion führend in [./Vehicles.md](./Vehicles.md) (≈550–700 AE, D-047) | Amortisation nach ~2–3 Fahrten; Angriffsziel mit klarer ROI-Rechnung |
| Sinnvolle Harvester pro Feld | 2–3 (mittleres Feld) | Deckt sich mit dem Nachwuchs-Limit in [./Resources.md](./Resources.md); mehr Harvester erzwingt Überernte |
| Transportweg | Kein separates Konvoi-System; Harvester fahren direkt | TPD §8.4 nennt Transportweg – als reine Wegstrecke interpretiert, kein eigenes Mechanik-Layer |

Harvester sind unbewaffnet (Allianz/Legion) und haben mittlere Panzerung; Evolvierte-Sammler ("Zermahler") regenerieren langsam (D-011-Logik auf Einheitenebene, Details in Vehicles.md).

## Lager- und Kapazitätsregeln (D-024/D-096/D-106)

| Regel | Wert v0.2 |
|---|---|
| Konto-Grundkapazität | Genau 2.000 AE, solange mindestens ein fertiges, lebendes HQ existiert; mehrere HQs stapeln die Basis nicht |
| Kapazität pro Lager-Gebäude | +2.000 AE je fertigem, lebendem Lager; Baustellen zählen nicht |
| Neue Einzahlungen | Ernte und Rückerstattungen werden an der aktuellen Grenze hart gekappt; der nicht passende Anteil verfällt |
| Vorhandener Überhang | Alle 10 Sim-Ticks (1 s) verfallen 25 % des aktuellen AE-Anteils oberhalb der Grenze, ganzzahlig abgerundet und mindestens 1 AE, bis die Grenze erreicht ist |
| Kapazitätsverlust | Zerstörung oder Verkauf eines Lagers sowie Verlust des letzten HQ senken die Grenze sofort; der neue Überhang folgt derselben periodischen Regel, ohne zusätzlichen Einmalverlust |
| Verkauf-Reihenfolge | Die Rückerstattung wird noch gegen die vor dem Despawn geltende Kapazität gedeckelt; anschließend sinkt die Grenze |

## Energie-System und Low-Power

| Parameter | Festlegung |
|---|---|
| Erzeugung und Verbrauch je Gebäude | **Führend: [./Buildings.md](./Buildings.md)** (Review F-03 – keine Doppelpflege in diesem Dokument) |
| Bilanz | Energie ist eine **Produktions-Bilanz** (Erzeugung − Verbrauch), keine Lagerressource |

**Low-Power-Regel (Zahlengerüst, verbindlich):** Bei Defizit (Verbrauch > Erzeugung):

1. Radar offline (Minikarte/Karten-Updates stoppen),
2. Verteidigungsplattformen offline,
3. Produktions-, Bau- **und Forschungsgeschwindigkeit** −50 % (Forschung explizit eingeschlossen, D-030),
4. Superwaffen-Countdown pausiert.

Reihenfolge ist fest: Radar und Verteidigung fallen **immer** zuerst, Produktion wird pauschal halbiert – keine spielerseitige Priorisierung im MVP (UI-/Balancing-Aufwand, siehe Offene Punkte). Rückkehr zu positivem Saldo stellt alle Systeme sofort wieder her.

## Einkommensraten-Ziele (AE/min, Richtwerte)

| Expansionsstufe | Setup | Ziel-Einkommen |
|---|---|---|
| Stufe 1 (Start) | 1 Raffinerie, 2 Harvester, mittleres Startfeld | ~500–650 AE/min |
| Stufe 2 (erste Expansion, ~Min. 6–10) | 2 Raffinerien, 4–5 Harvester | ~1.100–1.400 AE/min |
| Stufe 3 (zweite Expansion / Map-Control, ~Min. 12–20) | 3 Raffinerien, 6–7 Harvester | ~1.700–2.100 AE/min |

Begründung: Stufe 1 finanziert eine Aufbau- und Ersteinheiten-Phase ohne Leerlauf; Stufe 2 ermöglicht Tech-Tier-2 plus Dauerproduktion; Stufe 3 ist für Tier 3, Elite-Einheiten (D-015) und Superwaffe nötig – und liegt auf Feldern, die umkämpft sind. Das treibt die D-010-Matchkurve.

## Kostenrahmen v0.1

Fraktions-Profil-Faktor: Allianz ×1,15 (teuer, stark), Legion ×0,85 (günstig, Masse), Evolvierte ×1,0 (Referenz). Die Einheiten-Tabelle unten zeigt Referenzwerte; **Gebäudekosten, -energie und -bauzeiten sind führend in [./Buildings.md](./Buildings.md) definiert** (Review F-03, Grundsatzregel D-047 – keine Doppelpflege hier).

### Gebäude (12 Typen gemäß D-008)

Kosten, Energiebilanz und Bauzeiten aller Gebäude stehen **ausschließlich in [./Buildings.md](./Buildings.md)** (führendes Dokument, Review F-03). Economy.md legt für Gebäude nur den systemischen Rahmen fest: MS-1-Startressourcen (3.000 AE, D-077), Einkommensraten-Ziele, Low-Power-Regel, Lager-/Kapazitätsregeln (D-024/D-096/D-106) und Reparatur-/Verkaufsregeln (Prozentwerte unten).

### Einheiten (Rahmen pro Kategorie und Tech-Tier)

| Kategorie | Tier 1 | Tier 2 | Tier 3 |
|---|---|---|---|
| Infanterie (8/Fraktion) | 100–300 | 300–500 | – |
| Fahrzeuge (12/Fraktion) | 400–700 | 700–1.200 | 1.200–1.800 |
| Luftfahrzeuge (7/Fraktion) | – | 800–1.300 | 1.300–2.000 |
| Drohnen (2–3/Fraktion, D-014) | 200–400 | – | – |
| Elite-Einheit (D-015, 1× MVP) | – | – | 3.000–4.000 |

Begründung: Mit 3.000 AE MS-1-Start (D-077) und ~600 AE/min ist der klassische
Refinery-/Harvester-Aufbau sofort finanzierbar; die 2.000-AE-HQ-Basis erzeugt
gleichzeitig Druck, früh auszugeben oder ein Lager zu errichten. Tier 2 bleibt
nach ~4–6 min, Tier 3 nach ~12–15 min erreichbar – passend zur Ziel-Matchdauer
20–35 min. Feinwerte stehen in den Einheitendokumenten.

## Reparatur- und Verkaufsregeln

| Regel | Wert v0.1 |
|---|---|
| Reparatur (Allianz/Legion, Gebäude) | **MS-1: kumulativ 30 %** der Baukosten für 0→100 % HP (D-104), anteilig über `S(h)`, ohne Rundungsdrift; Rate 10 HP/Tick, bei Low Power 5 HP/Tick |
| Reparatur (Fahrzeuge) | Über Repair-Drohne (D-014) oder Werft-Funktion der Fahrzeugfabrik; bis zur Umsetzung gilt derselbe 30-%-Startwert aus D-104 |
| Evolvierte | Keine aktive Reparatur: Regeneration ~1 % HP/s kostenlos, doppelt so schnell auf/nahe **lebender** Aetherium-Felder (D-011; Einschränkung auf lebende Felder gemäß D-027); kein AE-Abzug – Ausgleich über langsamere Rate |
| Verkauf | 50 % der investierten AE zurück (Basiswert, keine Reparatur-Rückerstattung); 5 s Abwickel-Phase, Gebäude in dieser Zeit verwundbar und funktionslos; Evolvierte "Rückbau" übernimmt dieselbe 50-%-Regel (Resorption) |

## Fraktions-Wirtschaftsmodifier (v0.2)

Harvester-Kosten sind **führend in [./Vehicles.md](./Vehicles.md)** definiert (D-047, Grundsatzregel "jeder Wert existiert genau einmal") – keine Doppelpflege hier (Werte je Fraktion siehe [./Vehicles.md](./Vehicles.md)).

| Fraktion | Harvester (Verweis) | Profil-Wirkung |
|---|---|---|
| Allianz | siehe [./Vehicles.md](./Vehicles.md) ("Demeter") | Teurer, effizienter; weniger Verluste |
| Legion | siehe [./Vehicles.md](./Vehicles.md) ("Schürfer") | Günstig ersetzbar; Masse-Ökonomie |
| Evolvierte | siehe [./Vehicles.md](./Vehicles.md) ("Schlürfer"), Regeneration, heilt sich an lebenden Feldern (D-027) | Synergie mit Feld-/Verseuchungsregeln ([./Resources.md](./Resources.md)) |

## Wirtschafts-Zielkurven (20–35-min-Match, gemäß D-010)

| Phase | Zeitraum | Wirtschaftlicher Ist-Zustand (Ziel) |
|---|---|---|
| Aufbau | 0–5 min | 3.000 AE MS-1-Start + Stufe-1-Einkommen; Raffinerie-/Harvester-Loop, erstes Kraftwerk und Kaserne |
| Expansion | 5–12 min | Startfeld-Reserve ~50 % verbraucht → Zwang zu Stufe 2; erste Feld-Konflikte |
| Dominanz | 12–22 min | Stufe 3 nötig für Tier 3/Elite/Superwaffe; Überernte-Entscheidungen (schnell auspressen vs. nachhaltig) werden matchrelevant |
| Endspiel | 22–35 min | Zentrale Felder erschöpft oder zerstört; Einkommen sinkt natürlich auf ~Stufe-2-Niveau → Matches enden durch Druck, nicht durch Ressourcen-Timeout |

Leitplanke: Gesamt-AE-Fluss pro Spieler über ein typisches 25-min-Match ≈ 25.000–35.000 AE (Ernte + Start), davon ~60 % Einheiten, ~30 % Gebäude/Tech, ~10 % Reparatur/Verluste.

## Anti-Stall-Logik

1. **Endliche Reserve** (D-010): Wer nicht expandiert, verliert ab Min. ~12 Einkommen – defensives Eigraben ist selbstlimitierend.
2. **Überernte-Trade-off** ([./Resources.md](./Resources.md)): Schnelles Auspressen sichert kurzfristig Tempo, ruiniert aber langfristig das eigene Feld – beide Seiten müssen sich bewegen.
3. **Feldsabotage** (D-012): Statische Verteidigung schützt Felder nicht vor gezielter Zerstörung; Turteln ist angreifbar.
4. **Nachwachsen ohne Timeout:** Einkommen versiegt nie vollständig (Restfelder wachsen nach) – kein unentschiedenes Ressourcen-Patt, sondern abnehmende Kurve, die Entscheidungen erzwingt.
5. **Neutrale Objectives** (D-016): Feindliche Lager und capturebare Türme setzen frühe und mittlere Konfliktpunkte mit AE-Belohnung außerhalb der Basen.
6. **Superwaffen-Timer** als Endspiel-Uhr: Wer die Kurve oben hält, forciert die Entscheidung innerhalb der Ziel-Matchdauer.

## Offene Punkte

- **Energie-Priorisierung (offen, frühestens Beta):** Low-Power schaltet im MVP pauschal (Radar/Verteidigung aus, Produktion/Bau/Forschung −50 %, D-030). Ob es später spielerseitige Prioritäten-Steuerung gibt (C&C-ähnliches Power-Management), ist frühestens im Beta-Balancing zu evaluieren – Entscheidung Game Director.
- **Harvester-Ladung 300 AE (Tuning-Hinweis, kein Entscheidungsbedarf):** Zahlengerüst; falls Tuning zeigt, dass der Feld-Loop zu träge ist, sind Ladung (300→400) und Ausläufer-Inhalt in [./Resources.md](./Resources.md) gemeinsam anzupassen (Konsistenzpflicht).
- Entschieden im Korrekturlauf Sprint 2 (entfernt): Raffinerie-Packaging (inkl. 1 Harvester, D-024); Lager-Verlust-Regel (anteiliger Verlust statt Bestandsschutz, D-024); Höhe der Objective-Belohnungen (400/800/1.200 AE, definiert in [./NeutralUnits.md](./NeutralUnits.md)).

## Nächste Schritte

- Feinwerte je Einheit mit Infantry.md/Vehicles.md/Aircraft.md abstimmen (Kostenrahmen als Obergrenze).
- Gebäude-Werte werden seit Review F-03 führend in Buildings.md gepflegt; hier verbleibt nur die Systemlogik (Raten, Low-Power, Lager) – ein Werte-Abgleich entfällt.
- Tuning-Szenarien (Simulationsläufe) für die Zielkurven definieren; erste Validierung im MVP-Vertical-Slice (Sprint 4).
- KI-Anforderung ableiten: KI muss Expansions-Timing (Stufe 2 ab Min. 6–10) und Überernte-Vermeidung beherrschen (Input für AIArchitecture, Sprint 3).

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Gameplay Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Lead Gameplay Designer |
| 0.3.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings): Gebäudekosten/-energie durch Verweise auf Buildings.md ersetzt (Review F-03, D-047-Grundsatzregel); Economy.md behält nur Systemlogik (Raten, Low-Power, Lager) | Lead Gameplay Designer |
| 0.4.0 | 2026-07-21 | F-03 vollständig geschlossen: Harvester-Kosten in der Fraktions-Wirtschaftsmodifier-Tabelle durch Verweis auf die führende Quelle [Vehicles.md](./Vehicles.md) ersetzt (D-047) – keine dritte Zahl mehr neben Vehicles.md (700/550/620 AE) | Lead Gameplay Designer |
| 0.5.0 | 2026-08-10 | MS-1-Start auf 3.000 AE (D-077) nachgezogen und D-106 präzisiert: eine HQ-Basis je Konto, +2.000 je fertigem Lager, harte Einzahlungskappung sowie zustandsloser 25-%-Abbau des aktuellen Überhangs pro Sekunde | Codex / Dennis Westermann |
| 0.6.0 | 2026-08-10 | D-104: Reparaturkosten für MS-1 auf den implementierten kumulativen Startwert von 30 % vereinheitlicht; ganzzahlige `S(h)`-Abrechnung und Low-Power-Rate präzisiert | Project Owner / Agent (unter Delegation) |
