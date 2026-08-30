# Biome

**Version:** 0.2.0 | **Status:** Entwurf (Korrekturlauf Sprint 2) | **Verantwortungsbereich:** Lead Level Designer | **Sprint:** 2

## Zweck

Definiert die 10 Biome von Hashkrieg als Themen-Bibliothek (gemäß D-017): visuelle Identität, Terrain-Features, zerstörbare Elemente (gemäß D-012), Wetter bzw. Hazards sowie die sparsamen, lesbaren Gameplay-Effekte je Biom. Verbindlich für Level Design, Environment Art (Input für Sprint 5, APL Paket 01/02) und VFX-Planung (Wetter/Hazards). Biome sind Themen, keine fertigen Karten – konkrete Layouts entstehen nach [./Maps.md](./Maps.md). Die Karten-Roadmap (12 Karten, D-017) nutzt Wüste und Schnee je zweimal (Doppelbelegung bestätigt, D-028).

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-010 (Aetherium-Ausbreitung), D-012 (gezielte Zerstörbarkeit), D-013 (keine Marine, Wasser nur Terrain-Feature), D-017 (Biome/Wetter-Hazards), D-019 (Kamera/Lesbarkeit), D-028 (Hazard-Zuordnung Mond/Mars, Eisbruch-Fallback, Vakuum- und Flammen-Regeln, Doppelbelegung)
- [./Maps.md](./Maps.md) – Layout-Regeln und Karten-Roadmap, denen die Biome zugeordnet werden
- [./Resources.md](./Resources.md) – Ausbreitungsraten des Aetheriums, auf die Biom-Modifikatoren wirken
- [./FogOfWar.md](./FogOfWar.md) – Sichtweiten-Basiswerte, die Wetter modifiziert
- [../research/FogOfWar.md](../research/FogOfWar.md), [../research/Pathfinding.md](../research/Pathfinding.md) – technische Budgets für Sicht- und Geländeeffekte
- [../../RTS_Game_Design_Outline.md](../../RTS_Game_Design_Outline.md) – Biom-Liste (Quelle)

## Design-Prinzipien für Biom-Effekte

1. **Lesbarkeit vor Simulation (D-019):** Jeder Effekt muss auf den ersten Blick erkenn- und vorhersagbar sein. Pro Biom maximal **2–3 aktive Gameplay-Effekte**.
2. **Keine versteckten Modifier:** Alle Effekte stehen im Ladebildschirm und im Karten-Tooltip; prozentuale Modifikatoren nur in 25%-Schritten.
3. **Symmetrie:** Effekte gelten kartenweit und fraktionsneutral (Asymmetrie kommt aus Fraktionen, nicht aus Biomen).
4. **Budget:** Gelände-Slow-Zonen und Wetter-Sichteffekte müssen auf demselben Grid wie Pathfinding/FoW laufen – keine zusätzliche Simulations-Ebene.
5. **Datengetrieben:** Jedes Biom ist ein flacher Datensatz (ScriptableObject-tauglich): `BiomeId, Palette, TerrainFeatures[], Destructibles[], WeatherId, SightModifier, SpeedZones, AetheriumSpreadModifier`.

### Zentrale Modifikator-Tabelle (Richtwerte v0.2)

| Biom | Wetter/Hazard | Sicht | Tempo | Aetherium-Ausbreitung |
|---|---|---|---|---|
| Wüste | Sandsturm (periodisch) | −25 % während Sturm | Dünne Sandfelder: −25 % Rad | +25 % (trockener Boden) |
| Schnee | Schneesturm (periodisch) | −25 % während Sturm | Schneeflächen: −25 % Rad | −25 % (Frost hemmt Wachstum) |
| Vulkan | Aschefall (periodisch) | −25 % während Ascheregen | Lavaströme: unpassierbar, Schadenszone | +25 % (geothermische Energie) |
| Dschungel | Monsunregen (periodisch) | −25 % im Regen | Unterholz: −25 % (alle Bodentruppen) | +25 % (feuchtes Biom) |
| Sumpf | Nebelbänke (ortsfest) | −25 % in Nebelzonen | Morast: −25 % (alle Bodentruppen) | −25 % (Wasser verdünnt Kristalle) |
| Verlassene Stadt | – (kein Wetter) | unverändert | Trümmerfelder: −25 % | 0 % (versiegelter Boden) |
| Industriegebiet | Smog (ortsfest, schwach) | −25 % in Smogzonen | unverändert | −25 % (kontaminierter Boden) |
| Alien-Welt | Sporenflug (periodisch) | −25 % während Sporenflug | Schleimpfade: −25 % | +50 % (Heimat des Kristalls – USP-Bühne) |
| Mond | Strahlungsfronten (periodisch) | unverändert | unverändert | 0 % |
| Mars | Staubstürme (periodisch) | −50 % während Sturm | unverändert | 0 % |

*Begründung der Richtwerte: Sicht- und Tempo-Malus je max. 25 % (Mars 50 % als Extrem-Biom), damit Matchdauer 20–35 Min (D-010) nicht kippt. Ausbreitungs-Modifier erzeugen pro Biom ein anderes Wirtschaftstempo, ohne die Phasenlogik aus Resources.md zu brechen. Hazard-Zuordnung Mond = Strahlungsfronten / Mars = Staubstürme bestätigt durch D-028.*

## Biom-Profile

### 1. Wüste

- **Farbpalette/Stimmung:** Sand-Ocker, gebleichtes Weiß, rostige Felsen; harte Mittagssonne, weite Sichtachsen. Klassische C&C-Anmutung (Einstiegsbiom für H1-Zielgruppe, D-007).
- **Terrain-Features:** Dünen (flaches Gelände), Felsplateaus (Hochwege), ausgetrocknete Flussbetten (natürliche Engstellen), Oasen mit Wasserflächen (unpassierbar, D-013).
- **Zerstörbare Elemente (D-012):** Kakteen/Trockengebüsch (brennbar), verfallene Lehmhütten, Holzbrücken über Flussbetten.
- **Wetter:** Sandsturm (periodisch, alle 4–6 Min für 30–45 s): Sicht −25 %, Flugfahrzeuge manövrieren weiter (kein Grounding – Lesbarkeit).
- **Gameplay-Effekte:** Dünne Sandfelder bremsen Radfahrzeuge (−25 %); Aetherium-Ausbreitung +25 %.

### 2. Schnee

- **Farbpalette/Stimmung:** Bläuliches Weiß, Stahlgrau, spärliches Nadelgrün; kaltes, klares Licht, lange Schatten.
- **Terrain-Features:** Schneefelder, zugefrorene Seen (**MVP-Fallback, D-028:** passierbar für Infanterie und leichte Fahrzeuge, unpassierbar für schwere Fahrzeuge; die volle Eisbruch-Zustandsmaschine – Eis bricht unter schweren Fahrzeugen und wird danach dauerhaft unpassierbar – kommt nur bei ausreichendem Simulations-Budget, abhängig von Q-014), vereiste Schluchten als Engstellen.
- **Zerstörbare Elemente:** Nadelbäume (brennbar), verlassene Skihütten, Eisbrücken.
- **Wetter:** Schneesturm (periodisch): Sicht −25 %.
- **Gameplay-Effekte:** Schneeflächen bremsen Radfahrzeuge (−25 %, Ketten unverändert – kleiner, lesbarer Rock-Paper-Scissor-Effekt); Aetherium-Ausbreitung −25 %.
- **Hinweis (D-028):** Der MVP fährt mit dem Fallback "Eis unpassierbar für schwere Fahrzeuge"; die volle Eisbruch-Mechanik ist ein Upgrade bei ausreichendem Sim-Budget (Q-014, Entscheidung Sprint 3).

### 3. Vulkan

- **Farbpalette/Stimmung:** Schwarzer Basalt, glutrotes Orange, Aschgrau; düster, bedrohlich, glimmender Himmel.
- **Terrain-Features:** Lavaströme (unpassierbar, Schadenszone für Einheiten im Kontakt), Obsidianrücken (Hochwege), Krater als natürliche Arenen um zentrale Aetherium-Felder.
- **Zerstörbare Elemente:** Verkohlte Baumleichen (brennbar), Lavaröhren-Brücken (sprengbar), Gasquellen (explodieren bei Beschuss – kleiner Flächenschaden, tactical trigger).
- **Wetter:** Aschefall (periodisch): Sicht −25 %, keine Tempo-Effekte.
- **Gameplay-Effekte:** Aetherium-Ausbreitung +25 %; Gasquellen als einziger aktiver Kampf-Modifier.

### 4. Dschungel

- **Farbpalette/Stimmung:** Sattes Grün in allen Stufen, feuchtes Türkis, warmes Dämmerlicht; dicht, claustrophobisch.
- **Terrain-Features:** Dichtes Unterholz (Slow-Zonen), Flüsse mit Brücken (D-013: unpassierbar außer Brücken), Lichtungen als Bauplätze und Kampfareale.
- **Zerstörbare Elemente:** Tropenwald (brennbar – Brand schafft Sichtachsen, Kern-Moment der gezielten Zerstörbarkeit), Holzstege, Lianenbrücken.
- **Wetter:** Monsunregen (periodisch): Sicht −25 %, Feuer breitet sich im Regen nicht aus (lesbare Wechselwirkung).
- **Gameplay-Effekte:** Unterholz bremst alle Bodentruppen (−25 %), Infanterie erhält im Wald Tarnbonus (Sichtbarkeit −25 % statt kompletter Unsichtbarkeit); Aetherium-Ausbreitung +25 %.

### 5. Sumpf

- **Farbpalette/Stimmung:** Trübes Oliv, Schlammgrau, faules Grün; diesig, faulig, Blasen auf Wasserflächen.
- **Terrain-Features:** Morastflächen (Slow-Zonen), seichte Wasserflächen (unpassierbar), Treibholzinseln als Durchgänge, Nebelbänke (ortsfeste Sichtzonen statt periodischem Wetter – ruhigeres Spielgefühl).
- **Zerstörbare Elemente:** Schilf und Mangroven (brennbar), morsche Pfahlbauten, Fährbrücken.
- **Wetter:** Nebelbänke (ortsfest): Sicht −25 % innerhalb der Zone.
- **Gameplay-Effekte:** Morast bremst alle Bodentruppen (−25 %); Aetherium-Ausbreitung −25 %.
- **Zielgruppe:** Defensives, positionelles Spiel; bewusstes Gegengewicht zum offenen Wüsten-Biom.

### 6. Verlassene Stadt

- **Farbpalette/Stimmung:** Betongrau, verblasste Werbefarben, Rost; post-apokalyptische Ruhe, Staub im Licht.
- **Terrain-Features:** Straßenraster (schnelle Bewegung, unverändertes Tempo), Ruinenblöcke als Sichtblocker und Deckung, Plätze als Bauflächen, U-Bahn-Schächte als unpassierbare Vertiefungen.
- **Zerstörbare Elemente:** Ruinen (beschießbar bis zum Schutt-Feld = Slow-Zone), Fahrzeugwracks, brüchige Brücken/Fußgängerstege.
- **Wetter:** Keines (Stadt-Biom trägt Identität über Layout, nicht über Effekte).
- **Gameplay-Effekte:** Trümmerfelder bremsen (−25 %); Aetherium-Ausbreitung 0 % (versiegelter Boden – Felder wachsen nicht über Straßen, klare Lesbarkeit der Feldgrenzen).
- **Besonderheit:** Capturebare Geschütztürme (D-016) haben hier ihre Heimat-Karte (siehe Maps.md und NeutralUnits.md).

### 7. Industriegebiet

- **Farbpalette/Stimmung:** Rostrot, öliges Schwarz, Warnsignalgelb; funktional, dreckig, flackernde Beleuchtung. Spielt die Farbwelt der Legion (Rostrot/Ocker) nicht aus, um Fraktions-Lesbarkeit zu wahren.
- **Terrain-Features:** Fabrikgelände mit Hallen (Sichtblocker), Rohrleitungen (unpassierbar, teils sprengbar), Ladekräne als Landmarken, Betonflächen (schnelles Fahren).
- **Zerstörbare Elemente:** Chemiefässer (explodieren bei Beschuss – größerer Bruder der Vulkan-Gasquelle), Silos, Pipeline-Brücken.
- **Wetter:** Smog (ortsfest, schwach): Sicht −25 % in Smogzonen.
- **Gameplay-Effekte:** Aetherium-Ausbreitung −25 % (kontaminierter Boden); Chemiefässer als taktische Kettenreaktionen.

### 8. Alien-Welt

- **Farbpalette/Stimmung:** Violett, Bio-Grün, phosphoreszierendes Cyan; fremdartig, lebendig – bewusst nah an der Evolvierten-Identität, aber als *Umgebung* lesbar (Umgebungs-Violett dunkler/entsättigter als Fraktions-Violett).
- **Terrain-Features:** Kristallwälder (Sichtblocker), organische Rücken (Hochwege), pulsierende Aetherium-Vorkommen mit sichtbarer Ausbreitung (USP-Showcase-Biom, D-010).
- **Zerstörbare Elemente:** Kristallflora (zerbrechlich statt brennbar – Variation des D-012-Regelwerks), Bio-Brücken, Sporenkapseln (zerplatzen, kleiner Sicht-Debuff im Radius).
- **Wetter:** Sporenflug (periodisch): Sicht −25 %.
- **Gameplay-Effekte:** Aetherium-Ausbreitung +50 % (höchster Wert aller Biome – diese Karten erzeugen das schnellste Wirtschaftstempo und das aggressivste Territoriumsspiel).

### 9. Mond

- **Farbpalette/Stimmung:** Regolith-Grau, tiefschwarzer Himmel, hartes ungefiltertes Sonnenlicht; leer, still, erhaben (Erde am Himmel als Landmark).
- **Terrain-Features:** Kraterfelder (Vertiefungen ohne Höhenänderung – als Dekor/Darkening, keine Terrain-Deformation, D-012), Bergkämme (Hochwege), Lavatuben-Eingänge (unpassierbar).
- **Zerstörbare Elemente:** Verlassene Lander/Sonden (Wrack-Deko), Felsnadeln; **keine brennbaren Elemente** (kein Sauerstoff – konsequent zu D-012; Legion-Flammenwaffen: Schaden ja, Flächenbrände nein, D-028).
- **Hazards (statt Wetter, D-017/D-028):** **Strahlungsfronten** (periodisch, 20–30 s): Einheiten im offenen Gelände erhalten leichten Schaden über Zeit; Krater-Schatten und Bauwerke schützen. Kein Staubsturm auf dem Mond (atmosphärenlos – Hazard-Zuordnung bestätigt, D-028).
- **Gameplay-Effekte:** Kein Tempo-Modifier; Aetherium-Ausbreitung 0 % (statische Felder – planbare Endgame-Ökonomie).

### 10. Mars

- **Farbpalette/Stimmung:** Rostrot, Apricot-Himmel, verblasstes Rosa in der Dämmerung; weit, menschenleer, Pionier-Romantik.
- **Terrain-Features:** Dünenfelder, Canyonsysteme (starke Engstellen-Topologie), Polareiskappen am Kartenrand (Deko), dried-up Canyons als Hochwege.
- **Zerstörbare Elemente:** Verlassene Habitate, Rover-Wracks, Felsbrücken über Canyons; keine brennbare Vegetation (Legion-Flammenwaffen: Schaden ja, Flächenbrände nein – wie Mond, D-028).
- **Hazards (statt Wetter, D-017/D-028):** **Globale Staubstürme** (periodisch, länger als Standard-Wetter, 60–90 s): Sicht −50 %, Radar-Reichweite −25 %. Der größte Informations-Reset aller Biome – erzeugt Angriffszeitfenster.
- **Gameplay-Effekte:** Aetherium-Ausbreitung 0 %; kein Tempo-Modifier.

## Wetter-/Hazard-System (gemeinsame Regeln)

| Parameter | Standard-Wetter | Mond/Mars-Hazard |
|---|---|---|
| Auslöser | periodisch, Timer sichtbar in UI | periodisch, Timer + Vorwarnung sichtbar |
| Intervall | alle 4–6 Min | alle 5–7 Min |
| Dauer | 30–45 s | 20–30 s (Strahlung) / 60–90 s (Mars-Sturm) |
| Vorwarnung | 15 s (Himmel verdunkelt sich, Audio) | 20 s (UI-Warnung + Audio) |
| Wirkung | Sicht −25 % | Schaden über Zeit (Mond) bzw. Sicht −50 % (Mars) |

- Alle Effekte sind kartenweit bzw. zonenbasiert, symmetrisch und angekündigt – keine Zufallsschläge.
- Wetter beeinflusst **keine** Produktions-, Energie- oder Schadenswerte (Ausnahme: Mond-Strahlung), um die Balancing-Fläche klein zu halten.
- Hazards treffen alle Einheitentypen gleich: Infanterie kämpft im Vakuum (Mond) und unter dünner Atmosphäre (Mars) ohne Sonderregeln (D-028 – Lesbarkeit).
- VFX-/Audio-Bedarf je Biom wird in Sprint 5 (Asset-Pipeline) übernommen (Konsequenz aus D-017).

## Offene Punkte

- **Eisbruch-Vollversion (Schnee):** Design entschieden (D-028): MVP = Fallback "Eis unpassierbar für schwere Fahrzeuge"; volle Zustandsmaschine nur bei ausreichendem Sim-Budget. Status: abhängig von Q-014, Entscheidung in Sprint 3 (Technical Design).
- **Alien-Welt vs. Evolvierten-Farben:** Farbabgrenzung Umgebungs-Violett/Fraktions-Violett muss im Art-Test verifiziert werden (Lesbarkeits-Risiko). Status: offen, Input an Art Direction (Sprint 5).

*Entschieden im Korrekturlauf (D-028): Infanterie im Vakuum ohne Sonderregeln; Legion-Flammenwaffen auf Mond/Mars: Schaden ja, Brände nein; Hazard-Zuordnung Mond = Strahlungsfronten, Mars = Staubstürme.*

## Nächste Schritte

1. Karten-Roadmap in [./Maps.md](./Maps.md) gegen diese Profile abstimmen (Biom-Zuordnung je Phase).
2. Modifikator-Tabelle mit [./Resources.md](./Resources.md) (Ausbreitungsraten) und [./FogOfWar.md](./FogOfWar.md) (Sichtbasis) konsolidieren.
3. Effekt-Budget mit Technical Lead prüfen (Grid-Kosten für Slow-Zonen/Nebel, Q-014).
4. VFX-/Audio-Liste je Biom an Asset-Pipeline (Sprint 5) übergeben.
5. Playtest-Fragebogen: Erkennen Spieler Wetter-Vorwarnung und Zonen-Grenzen zuverlässig?

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung: 10 Biom-Profile, Modifikator-Tabelle, Wetter-/Hazard-System | Lead Level Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Lead Level Designer |
