# Vertical Slice MS-1 – Asset-Spezifikation

**Version:** 0.2.0 | **Status:** Entwurf – MS-1 Art-Strang verbindlich, kein Gate-Nachweis | **Verantwortungsbereich:** Technical Art / Producer | **Sprint:** 7

## Zweck

Dieses Dokument spezifiziert produktionsreif die **vier Vertical-Slice-Assets** der ersten Kunst-Iteration von Hashkrieg – je ein HQ-Gebäude und ein Leichter Panzer für Allianz und Legion. Es liefert Maße, Silhouetten-Regeln, LOD-Detailverteilung, Team-Farb-Flächen, PBR-Wertekorridore, Tri-/Texturbudgets sowie fertig kopierbare Bild-Prompts für die Image-to-3D-Referenzgenerierung. Es ist die verbindliche Arbeitsgrundlage für den Art-Strang von Sprint 7, ersetzt aber keine Fachdokumente (Gamedesign-Werte, Tri-Budgets) – diese werden zeichengenau referenziert, nicht neu erfunden.

Die vier Assets:

| assetId | Fraktion | Rolle | Anzeigename |
|---|---|---|---|
| `alliance.building.HQ` | Allianz | HQ | Kommandozentrale |
| `alliance.unit.LightTank` | Allianz | LightTank | Lynx |
| `legion.building.HQ` | Legion | HQ | Gefechtsstand |
| `legion.unit.LightTank` | Legion | LightTank | Räuber |

## Abhängigkeiten

- [../gamedesign/Factions.md](../gamedesign/Factions.md) – Fraktions-Formensprache, Farbnamen, Silhouetten-Prinzipien
- [../gamedesign/Buildings.md](../gamedesign/Buildings.md) – Kommandozentrale/Gefechtsstand: Kosten, Bauzeit, TP-Klasse, Grid-Footprint (als Annahme markiert)
- [../gamedesign/Vehicles.md](../gamedesign/Vehicles.md) – Lynx/Räuber: Kosten, HP, Rüstungsklasse, DPS, Fähigkeiten
- [../vision/Vision.md](../vision/Vision.md) – Art-Direction „Stylized Military Sci-Fi", Silhouette > Detail, Referenzrahmen Tempest Rising / C&C3
- [../vision/CoreGameplay.md](../vision/CoreGameplay.md) – Kamera-Pitch 50–60°, Zoomhöhe 18–90 m, Spielerfarbe vor Fraktionsfarbe, Barrierefreiheits-Anforderungen (Farbenblind-Redundanz über Form/Symbol)
- [../tech/AssetBudget.md](../tech/AssetBudget.md) – Tri-Budgets (Gebäude Standard, Fahrzeug leicht) und Texturauflösungen
- [../tech/Rendering.md](../tech/Rendering.md) – Renderpipeline-Rahmen (URP)
- [Licenses.md](Licenses.md), [AssetRegister.md](AssetRegister.md) – Lizenz- und Registrierungsverfahren für produzierte Assets

Referenziert, aber noch nicht existent (im Klartext genannt, nicht verlinkt, da parallel entstehend): `ArtAssetStandard.md`, `ArtManifest_MS1.md`, `Provenance.md`, `SourceCatalog_MS1.md`.

## 1. Konventionen (bindend, vom Orchestrator vorgegeben)

- 1 Unity-Unit = 1 Meter.
- Export: Blender → FBX, Achsen +Y up / −Z forward, Scale 1.0.
- Origin: bei Gebäuden Footprint-Mitte auf Y = 0; bei Fahrzeugen Bodenkontaktebene auf Y = 0.
- Fahrtrichtung: +Z.
- Pfade: `Assets/_Project/Art/Buildings/Alliance/HQ/`, `Assets/_Project/Art/Units/Legion/LightTank/` (analog für die übrigen drei Assets).
- Dateinamen: `SM_BLDG_Alliance_HQ.fbx`, `T_BLDG_Alliance_HQ_BC.png` / `_N.png` / `_MSK.png`, `M_BLDG_Alliance_HQ.mat`, `PF_BLDG_Alliance_HQ.prefab`; LOD-Meshes in der FBX als `SM_BLDG_Alliance_HQ_LOD0/1/2` (analog `SM_VEH_*` für Fahrzeuge).
- Mask-Kanäle: R = Metallic, G = Occlusion, B = TeamMask (0..1-Blendwert, kein Binärwert), A = Smoothness.
- 3 LOD-Stufen Pflicht; Schwellen LOD0 > 8 % Bildschirmhöhe, LOD1 2–8 %, LOD2 < 2 % (Quelle: [AssetBudget.md](../tech/AssetBudget.md) §3). Schatten nur LOD0/LOD1.

Diese Sektion ist keine neue Vorgabe, sondern die für dieses Dokument geltende Zusammenfassung des Orchestrator-Auftrags; bei Widerspruch zu einem künftigen `ArtAssetStandard.md` gilt dort die aktuellere Fassung.

## 2. Farbdefinition (verbindlich für MS-1)

Die Gamedesign-Dokumente nennen für die Teamfarben nur Namen – Allianz „Azurblau/Stahlgrau" ([Factions.md](../gamedesign/Factions.md) Z. 57, Z. 69), Legion „Rostrot/Ocker" ([Factions.md](../gamedesign/Factions.md) Z. 78, Z. 90). Die folgenden Hex-Werte sind durch den Projektinhaber freigegeben und ab sofort **verbindlich für MS-1**. Sie dienen als Grundlage für die Bild-Prompts in Abschnitt 4 und für die Materialarbeit.

### 2.1 Allianz

| Rolle | Hex | Begründungsnotiz |
|---|---|---|
| Grundton (Stahlgrau) | `#8A9199` | Neutrales, kühles Grau – bleibt bei 18–90 m Kameradistanz unter wechselndem Tageslicht als „Metall", nicht als Buntton, lesbar |
| Sekundärton (Azurblau) | `#2C6E9E` | Mittlere Sättigung/Helligkeit, damit die Fläche auch bei kleiner Bildschirmgröße (LOD1/LOD2-Distanz) nicht zu Schwarz absäuft |
| Akzent (Leuchtelemente) | `#4FD8FF` | Hoher Helligkeitskontrast zum Grundton für Energie-/Antennen-Akzente, aus der Vision.md-Vorgabe „saubere Energieeffekte" |
| Spielerfarben-Korridor | Hue 195°–225° (Cyan-Blau-Band), Sättigung ≥ 55 %, Helligkeit 35–65 % | Hält die Spielerfarbe klar im „kühlen" Halbkreis der Allianz-Identität, ohne mit Legion-Rostton zu kollidieren |

### 2.2 Legion

| Rolle | Hex | Begründungsnotiz |
|---|---|---|
| Grundton (Rostrot) | `#7A3524` | Warmer, gebrochener Rotton mit sichtbarem Rost-Charakter statt reinem Sättigungsrot |
| Sekundärton (Ocker) | `#B08430` | Erdiger Gelbton für Plattenflächen und Verwitterung, Kontrastpartner zum Rostrot |
| Akzent (Ruß/Verbrennung) | `#2B2018` | Sehr dunkler Braun-Schwarz-Ton für Ruß-Akzente laut Factions.md „Ruß-Akzente" (Z. 90) |
| Spielerfarben-Korridor | Hue 5°–35° (Rot-Orange-Band), Sättigung ≥ 55 %, Helligkeit 30–55 % | Hält die Spielerfarbe im warmen Halbkreis, deutlich getrennt vom Allianz-Korridor |

### 2.3 Lesbarkeit und Barrierefreiheit

**Kameradistanz:** Bei `zoomMin`/`zoomMax` 18–90 m ([CoreGameplay.md](../vision/CoreGameplay.md) Z. 49) und Pitch 50–60° (Z. 47–48) beträgt die effektive Bildschirmfläche einer Fahrzeug-Silhouette bei 90 m Kamerahöhe nur wenige Pixel. Die Grundton/Sekundärton-Paare oben sind bewusst auf einen Helligkeits-Delta von mindestens 15–20 Punkten (HSL-Lightness) zueinander gesetzt, damit die Zweiton-Struktur (nicht nur der Hue) noch erkennbar bleibt, wenn Farbsättigung durch Entfernungsnebel/Atmosphäre reduziert wird.

**Farbenblind-Gegenmaßnahme:** Blau (Allianz) gegen Rot (Legion) ist die kritischste Verwechslungsachse bei Deuteranopie und Protanopie. Gemäß der Barrierefreiheits-Anforderung in [CoreGameplay.md](../vision/CoreGameplay.md) („drei Farbenblind-Profile, Teamfarben werden dann zusätzlich über Form/Symbol getragen") tragen die Silhouetten-Regeln in Abschnitt 3 die eigentliche Unterscheidung: Allianz ist konsequent eckig-vertikal, Legion konsequent wuchtig-horizontal ([Factions.md](../gamedesign/Factions.md) Z. 69 vs. Z. 90). Zusätzlich ist der Allianz-Grundton (`#8A9199`, Lightness ≈ 60 %) deutlich heller als der Legion-Grundton (`#7A3524`, Lightness ≈ 33 %) – ein reiner Helligkeitskontrast, der ohne funktionierende Rot-Grün- oder Rot-Blau-Wahrnehmung trägt. Diese Kombination (Formensprache + Helligkeitskontrast) ist die geforderte Gegenmaßnahme, keine Ersatzfarbe.

## 3. Spezifikationsblätter

### 3.1 `alliance.building.HQ` – Kommandozentrale

**Referenzblatt:** [reference/REF_BLDG_Alliance_HQ_ortho.png](reference/REF_BLDG_Alliance_HQ_ortho.png) – dient als Bild-Input für die Image-to-3D-Generierung; Provenienz ist in `reference/PROVENANCE.json` nachgewiesen.

**Maße (art-seitige Arbeitsannahme Grid-Zellgröße = 3,0 m, hergeleitet aus Grid-Footprint):** [Buildings.md](../gamedesign/Buildings.md) §2.1 nennt keine Meter-Maße, nur die TP-Klasse „Schwer" und in §6 einen Footprint-Korridor 2×2 bis 4×4 Gridzellen für Gebäude allgemein, wobei HQ explizit in der 4×4-Klasse genannt wird; der Footprint-Wert selbst ist dort als Annahme markiert („Footprints … sind Annahmen; Finalisierung mit Maps.md", [Buildings.md](../gamedesign/Buildings.md) Offene Punkte). Bei einer Grid-Zellgröße von 3,0 m (art-seitige Arbeitsannahme, parallel in `ArtAssetStandard.md` verankert) ergibt ein 4×4-Footprint eine Grundfläche von **12,0 m × 12,0 m** (4 × 3,0 m). Höhe **9 m** (Annahme: TP-Klasse „Schwer" plus „kompakt, gepanzert, vertikal"-Vorgabe aus [Factions.md](../gamedesign/Factions.md) Z. 67 rechtfertigen ein Gebäude, das deutlich höher wirkt als breit ist, ohne die Sichtlinien-Regeln der Top-Down-Kamera zu sprengen).

**Proportionsregeln:** Kompakter, hoher Baukörper (Höhe ≈ 0,75× der Grundflächenkante) statt flacher Fläche – setzt sich gegen die niedrigeren Wirtschaftsgebäude (Kraftwerk, Lager) ab und markiert das HQ als Silhouetten-Ankerpunkt der Basis. Vertikale Kante-zu-Kante-Symmetrie (Frontsymmetrie entlang der Z-Achse), da die Kommandozentrale von allen vier Seiten angreifbar ist und keine "Rückseite" bevorzugen darf.

**Silhouetten-Merkmale (90 m Kamerahöhe):** ein zentraler, klar abgesetzter vertikaler Turm/Mast mit blinkendem Leuchtelement (Akzentfarbe `#4FD8FF`) als höchster Punkt; zwei symmetrische, eckige Seitenflügel mit klaren 90°-Kanten (keine Rundungen, gemäß „Eckig-präzise, klare Kanten" – [Factions.md](../gamedesign/Factions.md) Z. 69); eine markante horizontale Fensterband-/Energieleiste auf mittlerer Höhe, umlaufend, als "Ring", der das Gebäude auch aus reiner Top-Down-Sicht als Rechteck-mit-Ring erkennbar macht.

**Detail-Verteilung LOD0/1/2:**

| Stufe | Was bleibt | Was entfällt |
|---|---|---|
| LOD0 | Vollständige Paneel-Trennfugen, Antennendetails, Fensterband-Einzelsegmente, kleine Rohrleitungen, Leuchtelement mit eigenem Mesh | – |
| LOD1 | Zentralturm, Seitenflügel-Blockform, umlaufende Energieleiste als durchgehendes Band (nicht mehr segmentiert), Leuchtelement als vereinfachtes Mesh | Einzel-Paneelfugen (auf Textur verlagert), kleine Rohrleitungen, Antennen unter 0,5 m |
| LOD2 | Grundsilhouette: Turm + zwei Flügelblöcke als klar getrennte Kuben, Energieleiste nur noch als Texturstreifen | Alle Anbauteile, Turmdetails, separates Leuchtelement-Mesh (wird Teil des Turm-Meshs) |

Silhouette MUSS auf allen drei Stufen erhalten bleiben: Turm-plus-zwei-Flügel-Grundform und der helle Ring auf mittlerer Höhe (auch nur als Textur auf LOD2).

**Team-Farb-Flächen:** Umlaufende Energieleiste/Ring (Flächenanteil ca. 8–12 % der Gesamt-Sichtfläche) und die vertikalen Kantenprofile der Seitenflügel (ca. 5–8 %) tragen die TeamMask; Grundton Stahlgrau bleibt auf den großen Panzerflächen fraktionsfarben-neutral (Team-Overlay wirkt ausschließlich über den Mask-Kanal, siehe Abschnitt 1).

**PBR-Wertekorridore (Hauptmaterialzonen):**

| Zone | Metallic | Smoothness |
|---|---|---|
| Lackiertes Metall (Panzerflächen) | 0,0–0,15 | 0,35–0,55 |
| Blankes Metall (Kanten, Trims) | 0,80–1,0 | 0,55–0,75 |
| Rost | nicht vorgesehen (Allianz-Identität ist rostfrei) | – |
| Gummi/Ketten | nicht vorgesehen (Gebäude ohne Kettenwerk) | – |
| Glas/Emitter (Leuchtelement, Fensterband) | 0,0–0,1 | 0,85–0,98 (mit Emission über eigenen Emission-Map-Kanal, außerhalb der Mask-Textur) |

**Animationselemente:** keine Rig-Animation (Gebäude, rig-lose Code-/Shader-Animation gemäß Konvention); vorgesehen sind rein shaderseitige Effekte (Puls-Emission am Leuchtelement) – deren Implementierung ist ausdrücklich **nicht** Teil dieses Dokuments (siehe Abschnitt 6, Nicht-Ziele).

**Tri-/Texturbudget:** Gebäude Standard gemäß [AssetBudget.md](../tech/AssetBudget.md) §1: LOD0 ≤ 20.000 Tris, LOD1 ≤ 8.000, LOD2 ≤ 2.000 (inkl. etwaiger Bau-Zustands-Meshes im selben Budget). Textur: 1× 2048² Atlas (Albedo/Normal/Mask kombiniert), BC7/BC3, gemäß [AssetBudget.md](../tech/AssetBudget.md) §2.

### 3.2 `legion.building.HQ` – Gefechtsstand

**Referenzblatt:** [reference/REF_BLDG_Legion_HQ_ortho.png](reference/REF_BLDG_Legion_HQ_ortho.png) – dient als Bild-Input für die Image-to-3D-Generierung; Provenienz ist in `reference/PROVENANCE.json` nachgewiesen.

**Maße (Annahme):** gleicher 4×4-Footprint-Korridor wie Kommandozentrale ([Buildings.md](../gamedesign/Buildings.md) §6, als Annahme markiert), bei Grid-Zellgröße 3,0 m also ebenfalls **12,0 m × 12,0 m** Grundfläche (4 × 3,0 m). Höhe **6 m** (Annahme: bewusst niedriger als die Allianz-Kommandozentrale, um die Fraktions-Silhouettenregel „massiv, niedrig, weit gestreut" ([Factions.md](../gamedesign/Factions.md) Z. 88) konsequent umzusetzen – die Legion-Bauweise ist horizontal, nicht vertikal).

**Proportionsregeln:** Breiter, niedriger Baukörper (Höhe ≈ 0,5× der Grundflächenkante), asymmetrisch gegliedert (keine strikte Spiegelsymmetrie) mit unregelmäßig wirkenden Anbauten – setzt die „unregelmäßige Panzerplatten, sichtbare Nieten"-Vorgabe ([Factions.md](../gamedesign/Factions.md) Z. 90) auf Gebäudeebene um, ohne die Grundform unleserlich zu machen.

**Silhouetten-Merkmale (90 m Kamerahöhe):** ein niedriger, breiter Hauptblock mit mindestens zwei asymmetrisch versetzten Schornsteinen/Antennenmasten unterschiedlicher Höhe (kein spiegelsymmetrisches Paar, im Gegensatz zur Allianz); sichtbare Rohrleitungsbündel an einer Gebäudeecke als Erkennungssilhouette; ein niedriger, wuchtiger Vorbau mit sichtbarer Nietenreihe am Übergang zum Hauptblock.

**Detail-Verteilung LOD0/1/2:**

| Stufe | Was bleibt | Was entfällt |
|---|---|---|
| LOD0 | Einzelne Rohrleitungen, Nietenreihen als Geometrie, Rost-/Verwitterungs-Mesh-Kanten, beide Schornsteine mit Binnendetail | – |
| LOD1 | Hauptblock, beide Schornsteine als einfache Zylinder/Kuben, Rohrleitungsbündel als ein zusammengefasstes Mesh | Einzel-Nieten (auf Normal-/Mask-Textur verlagert), Rost-Mesh-Kanten (auf Textur) |
| LOD2 | Hauptblock plus zwei unterschiedlich hohe Schornstein-Stummel als Silhouetten-Cues | Rohrleitungsbündel-Mesh, Vorbau-Detailgeometrie (in Hauptblock verschmolzen) |

Silhouette MUSS erhalten bleiben: der breite, niedrige Hauptblock und die zwei asymmetrisch unterschiedlich hohen Schornsteine – das ist der klarste optische Gegenpunkt zum symmetrischen Allianz-Turm.

**Team-Farb-Flächen:** Vorbau-Nietenband und Schornstein-Basisringe (zusammen ca. 6–10 % Flächenanteil) sowie ein Warnstreifen-Muster an der Hauptblock-Front (ca. 4–6 %) tragen die TeamMask; Grundton Rostrot/Ocker bleibt großflächig fraktionsneutral.

**PBR-Wertekorridore:**

| Zone | Metallic | Smoothness |
|---|---|---|
| Lackiertes Metall (verbleibende intakte Plattenflächen) | 0,0–0,15 | 0,20–0,40 (stumpfer als Allianz – „dreckige" Anmutung) |
| Blankes Metall (frische Kanten, Reparaturflicken) | 0,70–0,90 | 0,45–0,65 |
| Rost (Hauptflächen, dominant) | 0,05–0,25 | 0,10–0,30 |
| Gummi/Ketten | nicht vorgesehen (Gebäude) | – |
| Glas/Emitter (Warnlicht) | 0,0–0,1 | 0,80–0,95 |

**Animationselemente:** keine Rig-Animation; vorgesehen ist shaderseitiger Rauch-/Glut-Effekt an einem Schornstein (Implementierung außerhalb dieses Dokuments, siehe Abschnitt 6).

**Tri-/Texturbudget:** identisch zur Kommandozentrale – Gebäude Standard: LOD0 ≤ 20.000, LOD1 ≤ 8.000, LOD2 ≤ 2.000 Tris; 1× 2048² Texturatlas, BC7/BC3 ([AssetBudget.md](../tech/AssetBudget.md) §1–§2).

### 3.3 `alliance.unit.LightTank` – Lynx

**Referenzblatt:** [reference/REF_UNIT_Alliance_LightTank_ortho.png](reference/REF_UNIT_Alliance_LightTank_ortho.png) – dient als Bild-Input für die Image-to-3D-Generierung; Provenienz ist in `reference/PROVENANCE.json` nachgewiesen.

**Maße (Annahme, aus Fahrzeugklasse „Fahrzeug leicht" hergeleitet):** [Vehicles.md](../gamedesign/Vehicles.md) nennt für den Lynx keine Meter-Maße, nur HP 550, Panzerung Leicht, Schadenstyp Energie, DPS 35, Reichweite 9, Tempo 7 m/s ([Vehicles.md](../gamedesign/Vehicles.md) Z. 108). Aus der Asset-Klasse „Fahrzeug leicht/mittel" ([AssetBudget.md](../tech/AssetBudget.md) §1) und dem Rollen-Vergleich zu APC/Scout in derselben Tabelle wird angenommen: Länge **6,2 m**, Breite **3,4 m**, Höhe **2,6 m** (Annahme: Kompaktpanzer-Proportionen, kleiner als der Battle Tank Aegis, größer als der Scout Jackal, passend zu „schneller Tier-1-Kampf, Raid"-Rolle, [Vehicles.md](../gamedesign/Vehicles.md) Z. 89).

**Proportionsregeln:** Niedrige, geduckte Wanne (Höhe deutlich unter halber Länge) für schnelle Silhouette; Turm sitzt leicht nach vorn versetzt, schmaler als die Wanne – klassisches Leichtpanzer-Profil, das sich klar vom schwereren, breiteren Aegis-Profil absetzt.

**Silhouetten-Merkmale (90 m Kamerahöhe):** ein einzelnes, schlankes Energiegeschütz mit sichtbarem Mündungsleuchten (Akzentfarbe `#4FD8FF`) statt klassischem Kanonenrohr – transportiert den „Energie"-Schadenstyp bereits über die Silhouette; klar abgesetzte, eckige Wannenkanten ohne Rundungen; zwei kleine, symmetrische Seitenpanzerflächen-Vorsprünge als wiedererkennbares Detail auch aus großer Distanz.

**Detail-Verteilung LOD0/1/2:**

| Stufe | Was bleibt | Was entfällt |
|---|---|---|
| LOD0 | Einzelne Kettenglieder/Radkappen-Details, Lüftungsgitter, Antennenpeitsche, Turm-Sensorcluster, Mündungsleuchte als eigenes Mesh | – |
| LOD1 | Wanne, Turm, Geschützrohr als klare Blockformen, Ketten/Räder als durchgehendes Band-Mesh | Einzel-Kettenglieder (auf Normal-Map verlagert), Lüftungsgitter-Geometrie, Antennenpeitsche |
| LOD2 | Wanne + Turm + Geschützrohr als drei klar getrennte einfache Formen | Sensorcluster, Mündungsleuchte-Mesh (Emission nur noch Textur), alle Anbauteile |

Silhouette MUSS erhalten bleiben: die niedrige, geduckte Wanne mit dem schlanken, nach vorn versetzten Turm und dem schmalen Energiegeschütz – das unterscheidet den Lynx auch auf LOD2 vom wuchtigeren Räuber.

**Team-Farb-Flächen:** Turmdach und ein umlaufender Streifen an der oberen Wannenkante (zusammen ca. 10–15 % Flächenanteil – bei einer kleinen Einheit muss der Flächenanteil höher liegen als beim Gebäude, damit die Teamfarbe bei 90 m Distanz überhaupt noch wahrnehmbar ist) tragen die TeamMask.

**PBR-Wertekorridore:**

| Zone | Metallic | Smoothness |
|---|---|---|
| Lackiertes Metall (Wanne, Turm) | 0,0–0,15 | 0,40–0,60 |
| Blankes Metall (Kettenabdeckungen, Kanten) | 0,75–0,95 | 0,55–0,75 |
| Rost | nicht vorgesehen | – |
| Gummi/Ketten (Laufwerk) | 0,0–0,05 | 0,10–0,25 |
| Glas/Emitter (Mündungsleuchte, Sensorcluster) | 0,0–0,1 | 0,85–0,98 |

**Animationselemente:** Laufwerk/Ketten-Rotation (rig-lose Code-Animation gemäß Fraktions-Konvention „rig-lose Code-Animation" für Fahrzeuge, [AssetBudget.md](../tech/AssetBudget.md) §1); Turm-Yaw-Rotation zur Ziel-Nachverfolgung als separates Pivot-Objekt. Die technische Umsetzung dieser Rotationen ist nicht Teil dieses Dokuments – gefordert wird hier nur, dass das Mesh einen separaten Turm-Node mit korrektem Pivot am Turmzentrum liefert.

**Tri-/Texturbudget:** Fahrzeug leicht gemäß [AssetBudget.md](../tech/AssetBudget.md) §1: LOD0 ≤ 8.000 Tris, LOD1 ≤ 3.000, LOD2 ≤ 800. Textur: 1× 1024² Atlas (Albedo/Normal/Mask kombiniert), BC7/BC3 ([AssetBudget.md](../tech/AssetBudget.md) §2).

### 3.4 `legion.unit.LightTank` – Räuber

**Referenzblatt:** [reference/REF_UNIT_Legion_LightTank_ortho.png](reference/REF_UNIT_Legion_LightTank_ortho.png) – dient als Bild-Input für die Image-to-3D-Generierung; Provenienz ist in `reference/PROVENANCE.json` nachgewiesen.

**Maße (Annahme):** [Vehicles.md](../gamedesign/Vehicles.md) nennt HP 480, Panzerung Leicht, Schadenstyp Kinetisch, DPS 28, Reichweite 8, Tempo 7 m/s ([Vehicles.md](../gamedesign/Vehicles.md) Z. 125). Gleiche Fahrzeugklasse „leicht" wie Lynx, daher ähnliche Größenordnung, aber wuchtiger: Länge **6,0 m**, Breite **3,6 m**, Höhe **2,7 m** (Annahme: minimal breiter/niedriger als Lynx, passend zur Legion-Formensprache „wuchtig, horizontal gestreckt" [Factions.md](../gamedesign/Factions.md) Z. 90 – auf Fahrzeugebene als "kompakter und massiger wirkend bei ähnlicher Gesamtlänge" ausgelegt).

**Proportionsregeln:** Breitere Wanne im Verhältnis zur Länge als beim Lynx (Breite/Länge-Verhältnis höher), sichtbar unregelmäßige Panzerplatten-Aufteilung statt glatter Flächen, Turm sitzt zentral statt vorn-versetzt (unterscheidet die Kinetisch-Waffe optisch vom versetzten Energie-Turm der Allianz).

**Silhouetten-Merkmale (90 m Kamerahöhe):** ein kurzes, dickes Kanonenrohr (klassisches Kinetik-Profil, im Gegensatz zum schlanken Energiegeschütz) mit sichtbarem Mündungsbremse-Detail; grob genietete, unregelmäßig gestufte Panzerplatten an der Wannenfront; ein einzelner, seitlich versetzter Auspuff-/Rauchauslass als wiedererkennbares Silhouetten-Detail.

**Detail-Verteilung LOD0/1/2:**

| Stufe | Was bleibt | Was entfällt |
|---|---|---|
| LOD0 | Einzelne Panzerplatten-Stufen als Geometrie, Nieten, Mündungsbremse-Detail, Auspuff-Mesh mit Innengeometrie | – |
| LOD1 | Wanne, Turm, Kanonenrohr als klare Blockformen, Panzerplatten-Stufung nur noch als grobe 2–3-stufige Silhouette | Einzelne Nieten (auf Normal-Map verlagert), Mündungsbremse-Feindetail, Auspuff-Innengeometrie |
| LOD2 | Wanne + Turm + kurzes Kanonenrohr als drei einfache Formen | Panzerplatten-Stufung (auf Textur verlagert), Auspuff-Mesh (Teil der Wanne) |

Silhouette MUSS erhalten bleiben: die breite, wuchtige Wanne mit zentralem Turm und dem kurzen, dicken Kanonenrohr – der klare Formkontrast zum schlanken Lynx-Geschütz.

**Team-Farb-Flächen:** Turmdach und ein Streifen an der Frontpanzerung (zusammen ca. 10–15 % Flächenanteil, analog Lynx-Begründung) tragen die TeamMask.

**PBR-Wertekorridore:**

| Zone | Metallic | Smoothness |
|---|---|---|
| Lackiertes Metall (verbleibende intakte Flächen) | 0,0–0,15 | 0,20–0,40 |
| Blankes Metall (frische Kanten, Reparaturflicken) | 0,70–0,90 | 0,45–0,65 |
| Rost (Wannenunterseite, Kettenabdeckungen) | 0,05–0,25 | 0,10–0,30 |
| Gummi/Ketten (Laufwerk) | 0,0–0,05 | 0,10–0,25 |
| Glas/Emitter (Mündungsblitz-Bereich, kein Dauerleuchten) | 0,0–0,1 | 0,70–0,90 |

**Animationselemente:** Laufwerk/Ketten-Rotation (rig-lose Code-Animation); Turm-Yaw-Rotation über separaten Pivot-Node am Turmzentrum, analog Lynx. Technische Umsetzung außerhalb dieses Dokuments.

**Tri-/Texturbudget:** identisch zum Lynx – Fahrzeug leicht: LOD0 ≤ 8.000, LOD1 ≤ 3.000, LOD2 ≤ 800 Tris; 1× 1024² Texturatlas, BC7/BC3 ([AssetBudget.md](../tech/AssetBudget.md) §1–§2).

## 4. Bild-Briefs für Image-to-3D-Generierung

Alle vier Prompts sind für **orthographische Referenzblätter** ausgelegt: drei Ansichten (front / side / top-down 3/4) auf einem Bild, neutraler Hintergrund, ohne Umgebung. Diese Bilder sind **Referenzmaterial für Image-to-3D-Generierung**, keine finalen Assets. Ihre Herkunft (Tool, Prompt-Version, Lizenzstatus, ggf. Seed) muss nach dem in `Provenance.md` beschriebenen Verfahren dokumentiert werden, sobald diese Datei vorliegt.

**Technische Vorgaben für alle vier Generierungen:** Seitenverhältnis 16:9 (Referenzblatt mit drei Ansichten nebeneinander), Zielauflösung mindestens 2048×1152 px, PNG mit transparentem oder einfarbigem neutralgrauem Hintergrund (`#808080`) zur einfachen Freistellung.

### 4.1 `alliance.building.HQ` – Kommandozentrale

```
Orthographic three-view reference sheet (front view, side view, top-down 3/4 view) of a stylized military sci-fi command headquarters building for a real-time strategy game, in the style of Tempest Rising / Command & Conquer 3, readable silhouette, not photorealistic, not cartoon. Compact, vertical, heavily armored structure: a central tall antenna tower as the highest point with a small glowing cyan beacon light, two symmetrical angular side wings with sharp 90-degree edges and flat armor panels, one continuous horizontal glowing energy band wrapping around the mid-height of the building. Color palette: base tone cool steel gray (#8A9199), secondary tone azure blue (#2C6E9E) on panel edges and trim, bright cyan accent (#4FD8FF) on the energy band and beacon light only. Clean, precise, high-tech faction identity — no rust, no organic shapes, no rounded corners. Neutral flat studio lighting, no dramatic shadows, plain solid gray background (#808080), no ground, no environment, no vegetation, no other objects in frame. No text, no logos, no watermarks, no UI elements. Orthographic projection, no perspective distortion, consistent scale across all three views.
```

**Negative-Prompt-Hinweise:** photorealistic, realistic materials, cartoon, chibi, cel-shaded, organic/biological shapes, rust, rounded/soft edges, perspective camera, fisheye, environment, sky, ground plane, characters, text, watermark, logo, signature, extra limbs, asymmetric silhouette, warm color palette (red/orange), single-view image.

### 4.2 `legion.building.HQ` – Gefechtsstand

```
Orthographic three-view reference sheet (front view, side view, top-down 3/4 view) of a stylized military sci-fi war command bunker/headquarters building for a real-time strategy game, in the style of Tempest Rising / Command & Conquer 3, readable silhouette, not photorealistic, not cartoon. Low, wide, massive industrial structure with an irregular, asymmetric layout: two smokestacks of clearly different heights placed off-center, a bundle of exposed exterior pipes at one corner, a low blocky front section with a visible rivet seam, unpolished welded armor plates of uneven size. Color palette: base tone rust red (#7A3524), secondary tone ochre (#B08430) on plate surfaces, dark soot-black accent (#2B2018) around the smokestacks and exhaust areas. Heavy, industrial, worn faction identity — visible weathering, no clean lines, no symmetry, no glowing energy tech. Neutral flat studio lighting, no dramatic shadows, plain solid gray background (#808080), no ground, no environment, no vegetation, no other objects in frame. No text, no logos, no watermarks, no UI elements. Orthographic projection, no perspective distortion, consistent scale across all three views.
```

**Negative-Prompt-Hinweise:** photorealistic, realistic materials, cartoon, chibi, cel-shaded, organic/biological shapes, clean/pristine metal, symmetric layout, sleek high-tech design, perspective camera, fisheye, environment, sky, ground plane, characters, text, watermark, logo, signature, cool blue color palette, single-view image.

### 4.3 `alliance.unit.LightTank` – Lynx

```
Orthographic three-view reference sheet (front view, side view, top-down 3/4 view) of a stylized military sci-fi light tank for a real-time strategy game, in the style of Tempest Rising / Command & Conquer 3, readable silhouette, not photorealistic, not cartoon. Low, crouched hull with sharp angular edges, no rounded surfaces, tracked light vehicle proportions (approx. 6 meters long). A slim turret offset slightly forward of hull center, mounted with a single thin energy cannon (not a bulky kinetic barrel) featuring a small glowing cyan muzzle emitter. Two small symmetric side armor projections on the hull flanks. Color palette: base tone cool steel gray (#8A9199), secondary tone azure blue (#2C6E9E) on hull edges, bright cyan accent (#4FD8FF) only on the turret roof stripe and the weapon emitter — clean precision high-tech faction look. Neutral flat studio lighting, no dramatic shadows, plain solid gray background (#808080), no ground, no environment, no other objects in frame. No text, no logos, no watermarks, no UI elements. Orthographic projection, no perspective distortion, consistent scale across all three views.
```

**Negative-Prompt-Hinweise:** photorealistic, realistic materials, cartoon, chibi, cel-shaded, organic/biological shapes, rust, bulky kinetic cannon barrel, rounded/soft edges, wheeled vehicle, perspective camera, fisheye, environment, sky, ground plane, characters, text, watermark, logo, signature, warm color palette (red/orange), single-view image.

### 4.4 `legion.unit.LightTank` – Räuber

```
Orthographic three-view reference sheet (front view, side view, top-down 3/4 view) of a stylized military sci-fi light tank for a real-time strategy game, in the style of Tempest Rising / Command & Conquer 3, readable silhouette, not photorealistic, not cartoon. Wide, squat, heavily armored hull (approx. 6 meters long, wider than tall), tracked vehicle, irregular riveted armor plates of uneven size and stagger, a single offset exhaust/smoke vent on one side of the hull rear. A centered turret with a short, thick kinetic cannon barrel featuring a visible muzzle brake. Color palette: base tone rust red (#7A3524), secondary tone ochre (#B08430) on plate surfaces, dark soot-black accent (#2B2018) around the exhaust vent — heavy, industrial, worn faction look. Neutral flat studio lighting, no dramatic shadows, plain solid gray background (#808080), no ground, no environment, no other objects in frame. No text, no logos, no watermarks, no UI elements. Orthographic projection, no perspective distortion, consistent scale across all three views.
```

**Negative-Prompt-Hinweise:** photorealistic, realistic materials, cartoon, chibi, cel-shaded, organic/biological shapes, clean/pristine metal, symmetric smooth plating, slim energy weapon, wheeled vehicle, perspective camera, fisheye, environment, sky, ground plane, characters, text, watermark, logo, signature, cool blue color palette, single-view image.

## 5. Abnahmekriterien Vertical Slice (Definition of Done für den Art-Strang, kein Gate-Nachweis)

Diese Checkliste beschreibt, wann ein produziertes Asset dieses Spezifikationsblatt technisch erfüllt. Sie ist **keine** Aussage über Gate- oder Meilenstein-Erreichung – dafür sind eigene, hier nicht behandelte Prozesse zuständig.

| Kriterium | Prüfung |
|---|---|
| Tri-Budget je LOD eingehalten | Messung: Import-Statistik in Unity (Mesh-Inspector, Vertex-/Triangle-Count je LOD-Slot) gegen die in Abschnitt 3 genannten Werte aus [AssetBudget.md](../tech/AssetBudget.md) §1 |
| 3 LODs vorhanden, Schwellen gesetzt | Sichtprüfung: `LODGroup`-Komponente enthält 3 Renderer-Slots (LOD0/1/2) mit relativen Höhenwerten gemäß §3 der [AssetBudget.md](../tech/AssetBudget.md) (LOD0 > 8 %, LOD1 2–8 %, LOD2 < 2 %) |
| Genau ein Textur-Set, korrekte Kanalbelegung | Messung: pro Asset genau 1 Textur-Atlas je Map-Typ (Albedo/Normal/Mask); Sichtprüfung der Mask-Textur-Kanäle (R=Metallic, G=Occlusion, B=TeamMask, A=Smoothness) per Textur-Kanal-Viewer |
| Material rendert plausibel auch ohne NovaUnit-Shader auf URP Lit | Screenshot-Vergleich: Asset einmal mit Ziel-Shader, einmal mit Standard-URP-Lit-Material gerendert; visuelle Grundform, Grundfarbe und Proportionen müssen im URP-Lit-Fallback erkennbar bleiben (kein Komplettausfall wie fehlende Textur/magenta) |
| Silhouetten-Test: Asset bei 90 m Kamerahöhe an Umriss allein der richtigen Fraktion zuordenbar | Screenshot-Vergleich: Renderausgabe bei `zoomMax` 90 m ([CoreGameplay.md](../vision/CoreGameplay.md)) als reine Schwarz-Silhouette (Farbe entfernt) gegen die in Abschnitt 3 benannten Pflicht-Silhouetten-Merkmale geprüft |
| Teamfarbe an definierter Stelle wirksam | Sichtprüfung: TeamMask-Flächen (siehe Abschnitt 3 je Asset) reagieren im Material-Preview sichtbar auf einen Test-Spielerfarbwechsel |
| Provenienznachweis vollständig | Sichtprüfung: vollständiger Eintrag nach dem `Provenance.md`-Verfahren vorhanden (Quelle des Referenzbilds, Generierungs-Tool, Prompt-Version, Lizenzstatus) |
| Origin/Maßstab/Achsen korrekt | Messung: Import-Inspector zeigt Origin gemäß Abschnitt 1 (Gebäude: Footprint-Mitte auf Y=0; Fahrzeug: Bodenkontaktebene auf Y=0), Skalierungsfaktor 1.0, Achsenkonvention +Y up / −Z forward eingehalten |

## 6. Explizite Nicht-Ziele des Vertical Slice

Der Vertical Slice MS-1 umfasst ausdrücklich **nicht**:

- Keine Anbindung an die GameDatabase oder an ScriptableObject-Datensätze.
- Kein Simulationsverhalten (Bewegung, Kampf, Produktion) der Assets.
- Keine Shader-Implementierung (der NovaUnit-Shader selbst ist nicht Teil dieser Spezifikation, nur seine Anforderungen an die Mask-Textur).
- Keine Prefab-Integration in Spielszenen.
- Keinen Gate- oder Meilenstein-Nachweis jeglicher Art. Dieses Dokument liefert Art-Spezifikationen; es behauptet an keiner Stelle, dass ein Gate erreicht, ein Meilenstein erfüllt oder ein Asset bereits existiert.

## Offene Punkte

- Die in Abschnitt 3 angenommenen Meter-Maße basieren auf als-Annahme-markierten Grid-Footprints aus [Buildings.md](../gamedesign/Buildings.md), umgerechnet mit der art-seitigen Arbeitsannahme Grid-Zellgröße = 3,0 m; eine verbindliche Footprint-Finalisierung mit Maps.md steht laut jener Quelle noch aus und wirkt sich direkt auf die hier genannten Gebäudemaße aus.
- Die genaue technische Kopplung von TeamMask-Flächenanteil und tatsächlicher Pixel-Lesbarkeit bei 90 m Kamerahöhe ist nicht messtechnisch validiert (nur Design-Annahme); ein Silhouetten-Screenshot-Test nach Produktion des ersten Assets sollte die Flächenanteil-Korridore in Abschnitt 3 verifizieren oder korrigieren.
- `ArtAssetStandard.md`, `ArtManifest_MS1.md`, `Provenance.md` und `SourceCatalog_MS1.md` entstehen parallel zu diesem Dokument; sobald sie vorliegen, sollte ein Abgleich erfolgen, ob die hier getroffenen Konventions- und Provenienz-Annahmen (Abschnitt 1 und 4) deckungsgleich sind.

## Nächste Schritte

- Bild-Prompts aus Abschnitt 4 für alle vier Assets generieren, Ergebnisse nach dem `Provenance.md`-Verfahren dokumentieren, sobald dieses vorliegt.
- Image-to-3D-Generierung auf Basis der freigegebenen Referenzblätter starten; erzeugte Meshes gegen die Tri-/Texturbudgets aus Abschnitt 3 prüfen.
- Nach Produktion des ersten Assets: Silhouetten-Screenshot-Test bei 90 m Kamerahöhe durchführen und die Flächenanteil-Korridore in Abschnitt 3 bei Bedarf nachjustieren.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.2.0 | 2026-07-25 | Fraktionspaletten (Abschnitt 2) von Vorschlag auf verbindlich für MS-1 umgestellt (Projektinhaber-Freigabe); HQ-Footprint-Herleitung (Abschnitt 3.1/3.2) auf Grid-Zellgröße 3,0 m umgestellt, Ergebnis 12,0 m × 12,0 m bestätigt sich mit den bereits genannten Werten, keine Zahlenkorrektur nötig; Referenzblatt-Verweise (Abschnitt 3.1–3.4) auf die vier bestehenden `reference/*.png`-Konzeptbilder ergänzt | Technical Art |
| 0.1.0 | 2026-07-25 | Erstfassung: Spezifikation der vier Vertical-Slice-Assets (Kommandozentrale, Lynx, Gefechtsstand, Räuber) inkl. Farbvorschlag, Maße, Silhouetten-Regeln, LOD-Verteilung, PBR-Korridore, Bild-Prompts und Abnahmekriterien | Technical Art |
