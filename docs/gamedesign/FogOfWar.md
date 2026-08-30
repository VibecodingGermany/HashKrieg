# Fog of War – Design-Regeln

**Version:** 0.3.0 | **Status:** Entwurf (Korrekturlauf Sprint 2) | **Verantwortungsbereich:** Lead Gameplay Designer | **Sprint:** 2

## Zweck

Gameplay-Regelwerk für Sicht, Aufklärung, Radar und Tarnung in *Hashkrieg*. Dieses Dokument definiert die **Design-Seite** (was der Spieler sieht und warum); die technische Umsetzung (grid-/bitmask-basiertes CPU-Sichtmodell, URP-Darstellung) ist in [../research/FogOfWar.md](../research/FogOfWar.md) recherchiert und hier verbindlich referenziert. Alle Sichtparameter sind datengetrieben (ScriptableObject-`VisionProfile`, flache Datensätze) und als Startwerte v0.1 zum Tunen zu verstehen.

## Abhängigkeiten

- [../research/FogOfWar.md](../research/FogOfWar.md) – technische Basis: Ansatz B (Grid/Bitmask), Sicht-Tick 5–10 Hz, URP-Pass, Team-Bitmasken (verbindliche Technikvorlage für Sprint 3)
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-008 (Radar-Gebäude, Verteidigungsplattform), D-014 (Scout-Drohnen), D-019 (Kamera: schräge Top-Down-Perspektive), D-017 (Biome/Wetter/Hazards), D-029 (Artefakt-Sonde nur SP/Koop, Radar-Pings im Team geteilt)
- [./Buildings.md](./Buildings.md) – Radar-Gebäude (Werte, Energieverbrauch)
- [./Infantry.md](./Infantry.md) / [./Vehicles.md](./Vehicles.md) / [./Aircraft.md](./Aircraft.md) – Einheiten-Sichtprofile, Tarn-/Detektor-Einheiten pro Fraktion
- [./Factions.md](./Factions.md) – Fraktions-Stammdaten
- [./Biomes.md](./Biomes.md) – Wetter-/Hazard-Sichtmodifikatoren (D-017)
- [./NeutralUnits.md](./NeutralUnits.md) – Sichtbarkeit Neutraler, Artefakt-Sonde
- [./Economy.md](./Economy.md) – Low-Power-Regel (Radar offline bei Energiedefizit)

## Sichtzustände (drei Zustände, verbindlich)

Übernommen aus [../research/FogOfWar.md](../research/FogOfWar.md), §Begriffsmodell:

| Zustand | Darstellung Hauptkamera | Minimap | Inhalt |
|---|---|---|---|
| **Unerforscht** | vollständig schwarz | schwarz | nichts |
| **Erforscht** | gedämpft/entsättigt (~40 % Helligkeit), kein Zeitverlauf | entsättigt | Terrain + Ghost-Gebäude (s. u.), keine Einheiten, keine Live-Änderungen |
| **Sichtbar** | volle Darstellung | voll | alles: Einheiten, Gebäude, Neutrale, Projektile |

Regeln:

- Der Übergang erforscht ↔ sichtbar läuft mit kurzem Easing (Shader-Interpolation, ~0,3 s), damit der 5–10-Hz-Sicht-Tick nicht "poppt" (Technikvorgabe aus der Research-Vorlage).
- Kamera (D-019) beeinflusst FoW **nicht**: Auszoom zeigt nicht mehr Welt, sondern nur mehr bereits Sichtbares. FoW bleibt informationslimitierend, nicht kamerabegrenzt.
- Startzustand: eigener Basisraum (~Radius 25 m um das HQ) ist zu Matchbeginn erforscht; Rest unerforscht.
- **Temporäre Aufdeckung – Artefakt-Sonde (D-029):** Die Belohnung des schweren Artefakt-Caches ([./NeutralUnits.md](./NeutralUnits.md)) deckt einen Kartenbereich (~gegnerischer Basisraum) für 30 s im Zustand "sichtbar" auf. Die Artefakt-Sonde ist **nur in SP/Koop aktiv**, im PvP deaktiviert (Informations-Balance).

## Sichtmodell: Radius im MVP, Höhen-LoS als Phase-2-Option

- **MVP (verbindlich):** reines Radius-Modell. Sicht einer Quelle = Kreis mit `sightRange`, keine Hindernisblockade. Begründung folgt der Research-Empfehlung (billigstes ausreichendes Modell, MVP-Disziplin).
- **Klippen-Bonus (bereits im MVP, da radiuskompatibel):** Quellen auf Hochgelände erhalten +20 % Sichtweite; Quellen im Tiefland sehen nicht auf Klippen (Radius zählt nur bis zur Klippenkante, umgesetzt als Blocker-Flag auf Klippen-Zellen im selben Grid – kein volles LoS). Erzeugt High-Ground-Grundspannung ohne LoS-Kosten.
- **Phase-2-Option (nicht MVP):** echtes Höhen-LoS (Linien-Rasterung mit Höhenvergleich, Faktor 3–5 Tick-Kosten laut Research). Aktivierung nur nach Profiling im Vertical Slice; designtechnisch dann Pflicht-Review aller Klippen-Karten (Maps.md).
- **Verworfen:** volles 3D-LoS per Raycast (skaliert nicht bei 100–500+ Einheiten, Research §Line-of-Sight).
- **Aetherium-Interaktion (Festlegung Korrekturlauf Sprint 2):** Ausgewachsene Kristallformationen blockieren Sicht **nicht** und reduzieren keine Sichtweiten in überwuchertem Gelände – das MVP-Radius-Modell (keine Hindernisblockade) gilt ausnahmslos. Eine Sicht-Interaktion des Aetheriums ist frühestens gemeinsam mit der Phase-2-Höhen-LoS-Option zu evaluieren (Abhängigkeit Resources.md/Maps.md entfällt damit für den MVP).

**Wetter-/Hazard-Modifikatoren (D-017):** Kartenweite Sicht-Modifikatoren sind in [./Biomes.md](./Biomes.md) entschieden (Wetter-Ereignisse −25 % Sicht; Mars-Staubsturm −50 % Sicht und −25 % Radar-Reichweite). Sie wirken multiplikativ auf `sightRange` bzw. den Radar-Radius und sind dort die einzige Quelle – dieses Dokument definiert nur die Basiswerte.

## Sichtweiten-Klassen

Klassen statt Einzelwerte: jede Einheit/jedes Gebäude referenziert eine Klasse im `VisionProfile`; Fraktionsmodifikatoren (s. u.) multiplizieren die Klasse. Werte in Metern, Richtwerte v0.1.

| klasse | sightRange | beispiele | revealStealth |
|---|---|---|---|
| VIS-INF | 10 m | Standard-Infanterie | nein |
| VIS-INF-RECON | 14 m | Scharfschütze, Aufklärer-Infanterie | nein |
| VIS-VEH-LIGHT | 12 m | leichte Fahrzeuge, Buggys | nein |
| VIS-VEH-HEAVY | 10 m | Panzer, Artillerie (Artillerie feuert auf Spotter-Sicht jenseits eigener Sichtweite) | nein |
| VIS-AIR | 14 m | Luftfahrzeuge (siehen über Klippen-Bonus hinaus +20 % implizit durch Flughöhe) | nein |
| VIS-SCOUT | 18 m | Scout-Drohne (D-014), dedizierte Aufklärer | **ja** |
| VIS-BLD-S | 10 m | Kraftwerk, Lager, Mauer-Abschnitt | nein |
| VIS-BLD-L | 14 m | HQ, Fabriken, Forschungslabor | nein |
| VIS-BLD-DEF | 14 m | Verteidigungsplattform (D-008) | nein |
| VIS-HARVEST | 8 m | Harvester (bewusst blind → Eskort-Anreiz) | nein |

**Fraktionsmodifikatoren** (Asymmetrie als Daten, nicht als Code – Multiplikator auf `sightRange`):

| fraktion | modifikator | begründung |
|---|---|---|
| Allianz | × 1,15 | High-Tech-Sensorik; teure, präzise Aufklärung als Identität |
| Legion | × 0,9 | kurze Sicht, viele Augen: kompensiert über günstige Masse und billige Scout-Drohne |
| Evolvierte | × 1,0 | organische Sensoren, Standardwerte; Sonderregel: getarnte Einheiten der Evolvierten (Burrow) zählen als Detektoren (s. u.) |

## Radar-Gebäude

Radar ist eines der 12 Gebäudetypen (D-008). Es liefert **Signatur-Aufklärung, keine Vollsicht**:

- **Wirkung:** Alle feindlichen Einheiten im Radar-Radius (Richtwert 80 m, datengetrieben) erscheinen als **Minimap-Pings** (Punkte, gefärbt nach Boden/Luft, ohne Einheitentyp, ohne HP, ohne Zahl). Die Hauptkamera bleibt unverändert im FoW – Radar deckt nie im "sichtbar"-Zustand auf, Feuern auf Radar-Pings ist nicht möglich.
- **Sweep-Modell:** Pings aktualisieren mit 1 Hz (Radar-Sweep als drehender Sektor auf der Minimap, darüber hinaus verblassen Pings nach ~4 s). Verzögerung ist gewollt: Radar zeigt Bewegung, keine Momentaufnahme.
- **Eigene/verbündete Einheiten** sind auf der Minimap ohnehin sichtbar (Team-Sicht), Radar betrifft nur Feinde.
- **Low-Power-Regel:** Bei Energiedefizit geht Radar offline (Zahlengerüst: Radar/Verteidigung offline) – Pings stoppen, Gebäude-Icon zeigt Zustand.
- **Gegenmittel:** Radar ist nicht ortbar durch den Gegner außer durch normale Sicht; keine Radar-Gegenmaßnahme im MVP.
- **Radar deckt Tarnung nicht auf** (`isRadar` ≠ `revealStealth`, Flags getrennt).

**Minimap-Pings (UI, nicht Radar):** manuelle Team-Pings (Alt-Klick) und automatische Warn-Pings (eigene Einheit unter Beschuss, Superwaffen-Countdown des Gegners falls entdeckt) sind UI-Features – Auflistung in [../vision/CoreGameplay.md](../vision/CoreGameplay.md), hier nur die Klammer: Warn-Pings erscheinen auch ohne Sicht auf den Ursprungsort, liefern aber keine Einheiteninformation.

## Tarnung und Aufdeckung

- **Modell:** SC2-Detektor-Prinzip (Research-Vorlage): Einheit trägt Flag `stealth`; sie ist für den Gegner nur sichtbar, wenn eine Quelle mit `revealStealth` sie im eigenen `sightRange` erfasst. Enttarnte Einheiten bleiben sichtbar, solange ein Detektor in Reichweite ist, plus 1,5 s Nachlauf.
- **Getarnte Einheiten (MVP-Richtwerte, Finalisierung in Infantry.md/Aircraft.md):**
  - Allianz: **Sniper** (tarnt nur stationär – *Tarnung im Stillstand*; Bewegung bricht die Tarnung mit 2 s Fade-out, der Schuss enttarnt) und **Commando** (*Tarnung* dauerhaft, bis zum ersten Angriff) gemäß [./Infantry.md](./Infantry.md). Beide tragen `stealth`; die unterschiedlichen Auslösebedingungen sind Daten im Einheiten-Record, keine zwei Systeme.
  - Legion: **keine** Tarnung (Fraktionsidentität Masse/Direktheit; Asymmetrie ist Absicht).
  - Evolvierte: Burrow-Fähigkeit ausgewählter Bio-Einheiten (eingegraben = getarnt + unbeweglich, fungieren als Detektoren-Minefeld).
- **Detektoren (Flag `revealStealth`, gemäß D-031.2):** VIS-SCOUT-Einheiten (dedizierte Aufklärer, s. Sichtweiten-Tabelle), Scout-Drohne aller Fraktionen (D-014) und das Detektor-Turm-Upgrade für Verteidigungsplattformen (Tier-3-Upgrade über das Forschungslabor). VIS-INF-RECON-Einheiten (Sniper, Aufklärer-Infanterie) sind **keine** Detektoren (D-031.2) – ihre hohe Sichtweite dient der Aufklärung, nicht der Enttarnung (konsistent zur Sichtweiten-Tabelle: `revealStealth = nein`). Sonderregel Evolvierte: Burrow-Einheiten fungieren als Detektoren (s. Fraktionsmodifikatoren oben).
- Neutrale (Lager, Critters) besitzen weder Tarnung noch Detektion; capturebare Türme erhalten Detektion nur über das o. g. Upgrade, sobald erobert.

## Team-Sicht

- Verbündete Spieler (Koop/Team-Modi ab Alpha, D-018) teilen Sicht vollständig (ODER-Verknüpfung der Team-Bitmasken, Technik laut Research O(1)). **Kein Opt-out**: gemeinsame Sicht ist Standard und nicht pro Match konfigurierbar (Vermeidung von UI-/Fairness-Komplexität).
- Geteilte Sicht umfasst Zustände und Detektion; **Radar-Pings werden im Team geteilt** (D-029, Teamplay-QOL).
- Spectator/Replay sehen alles (Maske "alle", Research-Vorlage).

## Ghost-Gebäude-Regel

- Im Zustand **erforscht** zeigt die Karte den **zuletzt gesehenen Zustand statischer Gebäude** (Position, Typ, Besitzer, Baufortschritt zum Sichtzeitpunkt) als gedämpftes "Ghost".
- Zerstörte/veränderte Gebäude bleiben als Ghost bestehen, **bis die Zelle erneut sichtbar wird** – dann Update oder Entfernen. Verhindert "Gratis-Information" über Abrisse, belohnt Re-Scouting.
- **Keine Ghosts für:** Einheiten jeder Art, Neutrale, capturebare Türme im unbesetzten Zustand nach Besetzung (der Besitzwechsel ist eine Live-Änderung; bis zum Re-Scout gilt der alte Ghost-Zustand), Aetherium-Ausbreitung (Terrainveränderung gemäß D-010 wird erst bei erneuter Sicht aktualisiert – bewusste Informationsasymmetrie des USP).
- Angriff auf Ghost: Einheiten dürfen auf Ghost-Positionen **keine** Angriffsbefehle erhalten (kein Blindfeuer auf Erinnerungen); Flächenangriffe (Artillerie, Superwaffen) auf erforschte Zellen sind erlaubt.

## Einheiten-Sichtbarkeit im Gefecht

- Angreifbar/wählbar ist eine feindliche Einheit nur im Zustand **sichtbar** (Grid-Lookup, kein Physik-Raycast, Research-Vorgabe).
- Verlässt eine Einheit die Sicht während eines Angriffs, bricht der Angriff nach 1,5 s Nachlauf ab (Zielverlust); verhindert "Hängenbleiben" an FoW-Kanten.

## Offene Punkte

- Sicht-Tick-Frequenz (5–10 Hz) und Grid-Auflösung (1 m vs. 2 m) sind Technik-/Performance-Fragen für Sprint 3 gemäß Research-Vorlage; Design hat keine Präferenz, solange Easing-Übergänge unmerklich bleiben – Status: offen, Sprint 3 (Q-013/Q-014-Umfeld).
- Konkrete Tarn-/Detektor-Einheiten jenseits der hier festgelegten Kernliste sind Richtwerte; finale Vollliste entsteht in Infantry.md/Vehicles.md/Aircraft.md (Infanterie 8/Fraktion, Drohnen D-014) – Status: offen, Kollisionsprüfung im Sprint-2-Konsistenzreview. Die Allianz-Doppelbelegung ist geklärt: Sniper (stationär) und Commando (dauerhaft) tragen beide `stealth` (s. Abschnitt Tarnung).
- Wetter-/Hazard-Sichtmodifikatoren sind in [./Biomes.md](./Biomes.md) entschieden und hier referenziert (s. Abschnitt Sichtmodell); Konsolidierung der Modifikator-Tabelle ist dort als Next Step vermerkt – Status: offen, liegt bei Biomes.md.

*(Entschieden im Korrekturlauf Sprint 2: Radar-Pings im Team → geteilt, D-029; Artefakt-Sonde → nur SP/Koop, D-029; Aetherium-Sichtinteraktion → Kristallformationen blockieren Sicht nicht, Festlegung Radius-Modell MVP; Detektor-Regel → VIS-INF-RECON-Einheiten sind keine Detektoren, Aufdeckung nur über VIS-SCOUT-Einheiten, Scout-Drohne (D-014) und Detektor-Turm-Upgrade, D-031.2.)*

## Nächste Schritte

1. Infantry.md/Vehicles.md/Aircraft.md/Buildings.md: `VisionProfile`-Zuordnung pro Einheit/Gebäude anhand der Klassen-Tabelle übernehmen; Tarn-/Detektor-Flags final verteilen.
2. [../vision/CoreGameplay.md](../vision/CoreGameplay.md): Kamera-Verhalten (D-019) gegen FoW-Regeln abgleichen; Minimap-Ping-UI dort spezifizieren.
3. Sprint 3: Diese Regeln + [../research/FogOfWar.md](../research/FogOfWar.md) als FoW-Abschnitt ins Technical Design Document (Datenmodell `VisionSource`/`VisionGrid`, Flags `revealsStealth`/`isRadar`/`stealth`).
4. Vertical Slice (Phase 2): Höhen-LoS-Option profilen (Ziel ≤ 1 ms Sicht-Tick laut Research) und danach über Aktivierung entscheiden.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Gameplay Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Lead Gameplay Designer |
| 0.3.0 | 2026-07-21 | Feinschliff Sprint 2 Runde 2 (D-031) | Lead Gameplay Designer |
