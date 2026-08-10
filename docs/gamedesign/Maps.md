# Karten (Maps)

**Version:** 0.4.0 | **Status:** Entwurf – MS-1-Override verbindlich | **Verantwortungsbereich:** Lead Level Designer | **Sprint:** 16

## Zweck

Definiert das Layout-Regelwerk für alle Project-Nova-Karten: Größenklassen, Startpositionen, Expansions-Logik, Engstellen-Topologie, Aetherium-Feld-Platzierung (Mengengerüst), neutrale Elemente (gemäß D-016), die Karten-Roadmap (MVP 1 / Alpha 4 / Beta 8 / Release 12, gemäß D-017) und den Karten-Produktionsprozess. Verbindlich für Level Design; Input für Economy-Balancing ([./Economy.md](./Economy.md)), KI (Expansions-/Angriffsrouten) und Asset-Pipeline.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-010 (Aetherium-Hybridwirtschaft, Matchdauer 20–35 Min), D-012 (zerstörbare Elemente), D-013 (Wasser nur Terrain-Feature), D-016 (Neutrale), D-017 (Biome/Karten-Roadmap), D-018 (Modi-Staffelung), D-019 (Kamera/Lesbarkeit), D-022 (Capture-Kanal, Brücken-Reparatur), D-025 (FFA-Klarstellung), D-028 (Karten-/Biome-Festlegungen), D-102 (fünf endliche Glutrinne-Felder), D-107 (kanonische Glutrinne-Layoutachse)
- [./Biomes.md](./Biomes.md) – Biom-Profile, Wetter/Hazards, Biom-Modifikatoren
- [./NeutralUnits.md](./NeutralUnits.md) – Regeln und Belohnungen für Critters, feindliche Lager, capturebare Geschütztürme
- [./MultiplayerModes.md](./MultiplayerModes.md) – Modus-Regeln und -Staffelung (Survival-Anforderungen)
- [./Resources.md](./Resources.md), [./Economy.md](./Economy.md) – Feld-Phasen, Ertragsraten, Startressourcen (1.000 AE)
- [../research/Pathfinding.md](../research/Pathfinding.md), [../research/FogOfWar.md](../research/FogOfWar.md) – Grid- und Sicht-Budgets
- [../production/SprintPlanning.md](../production/SprintPlanning.md) – Phasen MVP/Alpha/Beta/Release

## MS-1-Override (D-056)

MS-1 enthält ausschließlich **Glutrinne**: Wüste, Größe S, 128 × 128 Zellen
bei 1 m/Zelle, klares Wetter und zwei symmetrische Hauptangriffswege.

| Feldtyp | Anzahl | Reserve je Feld |
|---|---:|---:|
| Startfeld | 2 | 9.000 AE |
| natürliche Expansion | 2 | 9.000 AE |
| zentrales Feld | 1 | 15.000 AE |

Die ausgelieferte Glutrinne bewahrt gemäß D-102/D-107 ihre Layoutachse durch
Zelle `(62,62)`: Punkte spiegeln als `(x,y) → (124-x,124-y)`, die unteren
linken Ursprünge der einheitlichen 3×3-MS-1-Gebäude als
`(x,y) → (122-x,122-y)`. Deshalb liegen die beiden HQ-Ursprünge bei `(4,4)`
und `(118,118)`. Diese MS-1-Festlegung ist eine bewusste Ausnahme vom
nachfolgenden generischen Vollspielziel einer Spiegelung an der physischen
Kartenmitte; eine spätere Rezentrierung wäre eine eigene Karten- und
Balanceentscheidung.

Es gibt keine Neutralen, Brücken, Capture-Ziele, Wettereffekte, Hazards oder
Umgebungszerstörung; nur Aetherium darf sich verändern beziehungsweise
dauerhaft beschädigt werden. Alle weiteren Karten, Größen und Biome im
nachfolgenden Vollspielentwurf sind Post-MVP. Führende Inhaltsquelle ist
[MVPContentManifest.md](../production/MVPContentManifest.md).

## Kartengrößen

| Größe | Maße (Welt-Einheiten, Richtwert) | Modi | Spieler | Ziel-Matchdauer |
|---|---|---|---|---|
| S (Small) | ~128 × 128 | 1v1, FFA-3 | 2–3 | 20–25 Min |
| M (Medium) | ~192 × 192 | 2v2, FFA-4 | 4 | 25–30 Min |
| L (Large) | ~256 × 256 | 3v3, FFA-6 | 6 | 30–35 Min |

- Maße sind Richtwerte v0.2; die endgültige Kalibrierung am Pathfinding-Grid (Tile-Größe) und am Performance-Budget (100–500+ Einheiten, 60 FPS) ist eine Sprint-3-Abhängigkeit (Technical Design, Q-014) – keine Pseudo-Präzision.
- Karten sind quadratisch; Spielfeldrand = unpassierbare Kulisse (kein "unsichtbare Wand"-Gefühl).
- Matchdauer-Ziel folgt D-010 (20–35 Min); Größenstaffelung hält die Reisezeit HQ→Gegner grob konstant (~45–60 s für Standard-Fahrzeuge).

## Layout-Regeln

### Startpositionen

- **S (1v1):** 2 Startpositionen, diagonal gegenüber, symmetrisch (Punktsymmetrie zur Kartenmitte). FFA-3: Dreieck, gleiche Kantenabstände.
- **M (2v2):** 4 Startpositionen, je Team eine Kartenhälfte; Partner-Abstand ~25–30 % der Kartenbreite (kooperatives Bauen möglich, aber getrennte Basen).
- **L (3v3/FFA-6):** 6 Startpositionen auf zwei gespiegelten Dreierreihen (3v3) bzw. Sechseck-Ring (FFA-6).
- Jede Startposition: ebene Baufläche ~30 × 30 für HQ + Kerninfrastruktur (D-008: HQ, Kraftwerk, Raffinerie, Kaserne), plus freie Zufahrt in mindestens 2 Richtungen (keine Sackgassen-Spawns).
- Symmetrie-Regel: Alle Startpositionen einer Karte sind in Distanz zu Ressourcen, Engstellen und Kartenmitte äquivalent (±10 %). Asymmetrische Deko ist erlaubt, asymmetrische Topologie nicht (Skirmish-/PvP-Fairness).

### Expansions-Struktur (pro Spieler)

Jeder Spieler hat einen klaren, lesbaren Expansionspfad mit drei Stufen:

1. **Startfeld:** am Basisrand, sicher verteidigbar.
2. **Natürliche Expansion ("Natural"):** ein Feld in Richtung Gegner/Mitte, durch genau eine Engstelle geschützt – der klassische zweite Wirtschaftsstandort.
3. **Drittes Feld / umkämpfte Felder:** Richtung Kartenmitte oder Flanke, offen angreifbar – erzeugt Map-Control-Druck (D-010).

Zusätzlich 1–2 **zentrale Hochwert-Felder** (größere Mutterkristall-Reserve), um die sich das Mid-/Late-Game dreht.

### Engstellen und Hochwege

- Pro Verbindungsroute zwischen zwei Startpositionen mindestens **1 Engstelle** (Breite ~8–12 Einheiten = 3–4 Fahrzeuge nebeneinander) und **1 alternative Weitroute** (2–3× länger, offen) – Angriff braucht Wahl, Verteidigung braucht Schwachpunkt.
- **Hochwege** (erhöhte Plateaus): gewähren Sichtvorteil (Fog of War) und Artillerie-Reichweitengefühl, sind aber selbst nur über Rampen erreichbar. Keine Hochwege mit direkter Schusslinie auf Startbasen (Anti-Cheese-Regel).
- Engstellen aus zerstörbarem Material (Brücken, D-012) mindestens 1× pro Karte M/L: Sprengen = Route dicht, **aber reparierbar** über den D-022-Kanal (5 s, Engineer/Builder/Tunnelgräber, D-028) – temporäre statt dauerhafte Topologie-Entscheidung; Wasser bleibt unpassierbar (D-013).
- Max. 30 % der Kartenfläche unpassierbar (Wasser, Klippen, Sichtblocker-Massen) – genug Bauraum und Manövrierraum für 500+ Einheiten.

## Aetherium-Feld-Platzierung (Mengengerüst v0.2)

Werte sind Startwerte zum Tunen, abgeleitet aus: Startkapital 1.000 AE, Harvester-Ladung ~300 AE, Ziel-Matchdauer 20–35 Min (D-010). Reserve = Gesamtvorrat des Mutterkristalls; Ausläufer wachsen nach (D-010). Abgleich mit den Ertragsraten aus [./Economy.md](./Economy.md) ist erfolgt (siehe Gesamtbudget-Check unten); Werte bleiben Tuningsbasis, keine Fixwerte.

| Feldtyp | Reserve (AE) | Abstand zur eigenen Startposition | Anzahl S | Anzahl M | Anzahl L |
|---|---|---|---|---|---|
| Startfeld | ~6.000 | nah (Basisrand, ~10–15 % Kartenbreite) | 1/Spieler | 1/Spieler | 1/Spieler |
| Natürliche Expansion | ~9.000 | mittel (~25–30 % Kartenbreite, Richtung Gegner) | 1/Spieler | 1/Spieler | 1/Spieler |
| Drittes Feld (Flanke) | ~9.000 | weit (~40 % Kartenbreite, offen) | – | 1/Spieler | 1–2/Spieler |
| Zentrales Hochwert-Feld | ~15.000 | Kartenmitte, äquidistant zu allen | 1 | 1–2 | 2 |

**Platzierungsregeln:**

- Startfeld-Harvester-Laufweg (Raffinerie ↔ Feld) max. ~10 s – frühes Wirtschafts-Feeling ohne Leerlauf.
- Kein Feld näher als ~20 % Kartenbreite an einer **fremden** Startposition (kein Startfeld-Klau im Minuten-1-Rush).
- Zentrale Felder stehen in offenem Gelände oder im Krater/Tal (biomabhängig) – bewusst schwer haltbar.
- Felder grenzen nicht direkt aneinander; Mindestabstand Feldmitte↔Feldmitte ~20 % Kartenbreite, damit die Ausbreitung (D-010) Territorien sichtbar verbindet statt sofort zu fusionieren.
- Ausbreitungsrichtung der Felder ist layoutbewusst: Felder breiten sich bevorzugt entlang von Tälern/Routen aus (Terrainveränderung als taktischer Layer, USP), nie über unpassierbares Gelände.
- Gesamtbudget-Check: Summe aller Reserven pro Karte ≈ 25.000 AE/Spieler (S) bis ~35.000 AE/Spieler (L) – **abgeglichen mit [./Economy.md](./Economy.md)** (Einkommensziele ~500–650 AE/min auf Stufe 1 bis ~1.700–2.100 AE/min auf Stufe 3; Gesamt-AE-Fluss ~25.000–35.000 AE pro 25-min-Match; Startfeld-Reserve ~50 % verbraucht bis Min. 5–12): plausibel und im 20–35-Min-Korridor (D-010), ohne "Ressourcen-Timeout". Werte bleiben Tuningsbasis.
- Biom-Modifikatoren auf Ausbreitung (−25 % bis +50 %) gemäß [./Biomes.md](./Biomes.md) – Reserve-Mengen bleiben biomneutral, nur das Tempo variiert.

## Neutrale Elemente (gemäß D-016)

Details und Belohnungen: [./NeutralUnits.md](./NeutralUnits.md). Platzierungsregeln:

| Element | Platzierung | Anzahl S | Anzahl M | Anzahl L |
|---|---|---|---|---|
| Critters | zufällig wandernd, keine Gameplay-Wirkung | 5–10 | 10–15 | 15–25 |
| Feindliches Lager (Objective) | bewacht ein Drittes Feld oder eine Abkürzung; Belohnung Aetherium | – | 1–2 | 2–3 |
| Capturebarer Geschützturm | deckt zentrale Engstelle oder Hochweg, nie in Reichweite einer Startbasis | 1 | 2 | 3 |

- Lager-Garnison so bemessen, dass ein früher Angriff (Tier 1, ~6–10 Einheiten) sie bezwingt – Objective für die Minuten 5–10, nicht Minute 2.
- Keine Händler, kein Handelssystem (D-016).
- Neutrale blockieren nie die einzige Route zu einer natürlichen Expansion.

## Karten-Roadmap (gemäß D-017)

Biom-Zuordnung (festgelegt, D-017/D-028): 10 Biome als Themen-Bibliothek, 12 Karten → Wüste und Schnee erhalten je eine zweite Karte mit anderem Layout (Doppelbelegung für Karten 11–12 bestätigt, D-028):

| Phase | # | Karte (Arbeitstitel) | Biom | Größe | Fokus |
|---|---|---|---|---|---|
| MVP | 1 | "Glutrinne" | Wüste | S | Referenz-Layout: saubere 1v1-Topologie, Sandsturm, Tutorial-tauglich |
| Alpha | 2 | "Firngraben" | Schnee | S | Engstellen-/Eis-Topologie, 1v1 |
| Alpha | 3 | "Grünschleier" | Dschungel | M | 2v2, Brand-/Sichtachsen-Spiel |
| Alpha | 4 | "Stille Blocks" | Verlassene Stadt | M | 2v2/FFA-4, Geschützturm-Mechanik |
| Beta | 5 | "Ascheschleife" | Vulkan | M | Schadenszonen, zentrales Krater-Feld |
| Beta | 6 | "Moorfenn" | Sumpf | M | Defensiv-Layout, Nebelzonen |
| Beta | 7 | "Kombinat 9" | Industriegebiet | L | 3v3, Fass-Kettenreaktionen |
| Beta | 8 | "Sporenherz" | Alien-Welt | L | 3v3, schnellste Ausbreitung (USP-Showcase) |
| Release | 9 | "Tranquillity" | Mond | S | Hazard-Strahlung, statische Ökonomie |
| Release | 10 | "Roter Sturm" | Mars | M | Staubsturm-Reset, Canyon-Topologie |
| Release | 11 | "Sechs Schwestern" | Wüste II (Variante) | L | FFA-6-Referenz, Sechseck-Ring |
| Release | 12 | "Eismeer-Front" | Schnee II (Variante) | L | 3v3-Team-Referenz |

- Reihenfolge-Logik: MVP/ALPHA = klassische, lesbare Biome für die H1-Zielgruppe (D-007); exotische Biome (Alien, Mond, Mars) erst, wenn Kerngefühl steht; Hazard-Biome (Mond = Strahlungsfronten, Mars = Staubstürme, D-028) zuletzt (höchstes Testrisiko).
- Modus-Verfügbarkeit folgt D-018/D-025: MVP nur Solo-Skirmish 1v1 (Karte 1); Alpha +Koop/FFA lokal (Karten 2–4); Beta +PvP/Survival (Karten 5–8); Release +KotH (alle).
- **Survival (ab Beta) nutzt Standard-Karten, keine eigenen Wellen-Karten (D-028):** Jede Survival-fähige Karte (Größe M/L) braucht mindestens eine eng verteidigbare Engstelle in Reichweite der Startposition(en) (Engstellen-Anforderung; Modus-Details in [./MultiplayerModes.md](./MultiplayerModes.md)).

## Karten-Produktionsprozess

Fünf Stufen, je Karte; jede Stufe mit Exit-Kriterium:

1. **Blockout (Graukasten):** Startpositionen, Routen, Engstellen, Hochwege, Feldflächen als Primitive. *Exit:* 1v1-Selbsttest spielbar, Reisezeiten im Zielkorridor, Symmetrie-Check (±10 %) bestanden.
2. **Terrain:** Höhenprofile, unpassierbare Flächen, Slow-Zonen, Wasser (D-013); Grid-Validierung (Pathfinding/FoW auf demselben Grid). *Exit:* Pfadfindung auf allen Routen fehlerfrei, keine Tile-Leichen.
3. **Aetherium-Felder:** Mutterkristalle, Ausläufer, Ausbreitungszonen nach Mengengerüst; Simulation der Ausbreitung über 10 Min Probelauf. *Exit:* Feldgrenzen lesbar, keine Fusionen, Reserven gemäß Tabelle.
4. **Deko & Zerstörbares:** Vegetation/Dekor (brennbar), Brücken, neutrale Elemente, biomeigene Zerstörbare (D-012, Liste je Biom in [./Biomes.md](./Biomes.md)). *Exit:* Deko blockiert keine Pfade/Bauflächen; Zerstörungs-Zustände (intakt → brennend → Schutt) vollständig.
5. **Playtest & Balance:** KI-Skirmish (min. 10 Matches/Größenklasse), Wetter-/Hazard-Timing, Matchdauer-Messung gegen 20–35-Min-Ziel (D-010). *Exit:* Matchdauer im Korridor, keine dominante Rush- oder Turtle-Route, Lesbarkeits-Check (Wetter-Vorwarnung erkannt).

MVP-Karte "Glutrinne" durchläuft alle fünf Stufen als Referenz und definiert die Zeitschablonen für die übrigen Karten.

## Datenhaltung

Jede Karte ist ein flacher Datensatz (ScriptableObject-tauglich): `MapId, BiomeId, SizeClass, MaxPlayers, StartPositions[], AetheriumFields[]{Type, Position, ReserveAE}, NeutralSpawns[], WeatherId, Chokepoints[], HighGroundZones[]`. Keine verschachtelten Logik-Strukturen; Werte aus diesem Dokument und [./Biomes.md](./Biomes.md) sind die Startwerte v0.2.

## Offene Punkte

- **Kartenmaße vs. Grid (Sprint-3-Abhängigkeit, kein Design-Offenpunkt):** 128/192/256 Welt-Einheiten bleiben Richtwerte. Endgültige Maße folgen der Tile-Größe des gemeinsamen Pathfinding-/FoW-/Ausbreitungs-Grids; Entscheidung mit Q-014 im Technical Design (Sprint 3), danach Größentabelle fixieren.

*Entschieden im Korrekturlauf (D-028): Doppelbelegung Wüste/Schnee für Karten 11–12; Brücken reparierbar über D-022-Kanal; Survival auf Standard-Karten mit Engstellen-Anforderung. Erledigt: Reserve-Mengen mit [./Economy.md](./Economy.md) abgeglichen (siehe Gesamtbudget-Check).*

## Nächste Schritte

1. Blockout "Glutrinne" (Wüste, S) als Referenz-Karte für MVP (Stufe 1–2 des Produktionsprozesses).
2. Neutrale-Spawns mit [./NeutralUnits.md](./NeutralUnits.md) finalisieren (Garnisonsstärke, Belohnung).
3. Grid-Maße nach Q-014-Entscheidung (Sprint 3, Technical Design) festziehen; danach Größentabelle auf fixe Werte heben.
4. Symmetrie-Checkliste und Playtest-Fragebogen als Review-Vorlagen für Stufen 1 und 5 erstellen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung: Größenklassen, Layout-Regeln, Feld-Mengengerüst, Roadmap, Produktionsprozess | Lead Level Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Lead Level Designer |
| 0.3.0 | 2026-07-24 | Glutrinne mit Raster, Feldern, Routen und expliziten Nicht-Zielen als einzige MS-1-Karte gemäß D-056 festgelegt | Lead Level Designer |
| 0.4.0 | 2026-08-10 | D-102/D-107-Override ergänzt: fünf endliche Felder, Glutrinne-Layoutachse durch `(62,62)` und abgeleitete Spiegelung der einheitlichen 3×3-MS-1-Footprints | Agent (unter Delegation) / Dennis Westermann |
