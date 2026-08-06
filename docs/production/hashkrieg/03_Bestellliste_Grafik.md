# Bestellliste Grafik — was der Grafiker liefern soll

**Version:** 0.1.0 | **Status:** Entwurf – Beschaffungsgrundlage, kein Gate-Nachweis | **Verantwortungsbereich:** Technical Art / Producer | **Sprint:** 7

## Zweck

Die priorisierte Auftragsliste für externe Grafiker. Jede Position nennt
Dateiname nach Konvention, Format, technische Spezifikation, Zielordner und
wofür sie im Spiel gebraucht wird.

Vier Prioritätsstufen: **P0** ohne das ist keine Runde lesbar · **P1** macht das
Spiel verständlich · **P2** macht es gut · **P3** Politur.

## Abhängigkeiten

- [../../assets/ArtAssetStandard.md](../../assets/ArtAssetStandard.md) – Ordner, Namen, LOD-Kette, Import-Presets, Maskenkanäle
- [../../assets/ArtManifest_MS1.md](../../assets/ArtManifest_MS1.md) – Tri-Budgets, Texturgrößen, Footprints je Rolle
- [../../assets/ConceptArtStyleGuide.md](../../assets/ConceptArtStyleGuide.md) – Formensprache und Farbwelt beider Fraktionen
- [../../tech/AssetBudget.md](../../tech/AssetBudget.md) – Tri-, Textur- und Partikelbudgets
- [../../assets/Provenance.md](../../assets/Provenance.md) – Provenienzpflicht vor Repo-Aufnahme

## Was der Grafiker vorab bekommen muss

1. **Die 34 Concept-Art-Blätter** (`Hashkrieg_Assets/img/`, je 1024²). Sie sind
   die **verbindliche Vorlage** für jede Nachbestellung — gearbeitet wird gegen
   die Concepts, nicht gegen die vorhandenen Tripo-Modelle.
2. **Zwei Styleplates** (`Hashkrieg_Assets/style/styleplate_alliance.png` und
   `styleplate_legion.png`).
3. **Vier orthografische Referenzrenders** als Maßstabs- und Stilanker
   (Allianz-/Legion-HQ und -LightTank).
4. Den **Style-Guide** und den **Art-Standard** (Links oben).

**Farbwelt, verbindlich:** Allianz Körper `#8A9199`, Platten `#2C6E9E`,
Leuchtakzent `#58D5E8`. Legion Körper `#7A3524`, Platten `#B08430`,
Leuchtakzent `#FF8A3D`. Leuchtakzent-Korridor 5–12 % der Fläche.

**Technischer Rahmen, verbindlich:** 1 Unity-Unit = 1 m, 1 Grid-Zelle = 3,0 m.
FBX-Export `+Y up` / `−Z forward`, Scale 1.0, Origin Bodenmitte `Y = 0`,
Vorderseite auf `+Z`. LOD0/LOD1/LOD2 in **einer** FBX-Datei, Schwellen 8 % / 2 %,
LOD2 ohne Schatten.

> **Warnung, teuer wenn übersehen:** Der Namensparser akzeptiert exakt
> `PF_<UNIT|BLDG>_<Alliance|Legion>_<Rolle>` mit vier Unterstrich-Teilen und
> **case-sensitiver** Rolle. Name falsch = Asset im Spiel unsichtbar, ohne
> Fehlermeldung.

### Ansichtsregel — zweckgebunden, nicht pauschal (E-5)

Die strikte Frontalelevation der 34 bestehenden Blätter gilt **nur für Assets,
die im Spiel als Rolle gelesen werden müssen.**

| Gruppe | Betrifft | Ansicht |
|---|---|---|
| **A** | Einheiten, Gebäude, Aetherium, Baustellen, Trümmer, Rally-Flagge | strikte Frontalelevation: alle senkrechten Kanten senkrecht und parallel, kein Fluchtpunkt, keine sichtbaren Seitenflächen, keine Aufsicht |
| **B** | Kulissen-Props: Felsen, Kliffs, Vegetation, Wracks | **Dreiviertelansicht erlaubt** — sie dienen nur als Image-to-3D-Eingabe und tragen keine Rollenlesbarkeit |

**Für beide Gruppen unverändert:** freischwebend ohne Boden, Sockel, Geröll oder
Kontaktschatten · flacher Hintergrund `#0B1017` · malerisch statt
fotorealistisch.

Begründung und Herleitung: [00_Entscheidungen.md](00_Entscheidungen.md) E-5.
Ein Gruppe-B-Asset wegen Dreiviertelansicht abzulehnen ist ein Bewertungsfehler.

---

## P0-0 · Vorbedingung: Namenskonvention für alles außerhalb der 34 Rollen

**Muss vor der ersten Lieferung entschieden sein.** Der Art-Standard kennt exakt
vier Muster (`SM_` / `T_` / `M_` / `PF_` mit `BLDG|UNIT` + Fraktion + Rolle).
Für Props, VFX, UI-Icons, Cursor, Terrain und Skybox gibt es **kein Muster und
keinen Zielordner** — die Ordner `Art/UI`, `Art/VFX` und `Art/Terrain`
existieren nicht einmal.

Vorschlag zur Übernahme, konsistent mit dem bestehenden Schema:

| Kategorie | Muster | Zielordner |
|---|---|---|
| Props | `SM_PROP_<Name>.fbx`, `T_PROP_<Name>_BC\|_N\|_MSK.png`, `M_PROP_<Name>.mat`, `PF_PROP_<Name>.prefab` | `Art/Shared/{Meshes,Textures,Materials}/` |
| VFX | `VFX_<Name>.prefab`, `T_VFX_<Name>_<Map>.png` | `Art/Shared/VFX/` |
| UI | `UI_ICON_<Faction>_<Role>.png`, `UI_ICON_CMD_<Command>.png`, `UI_CUR_<Name>.png`, `UI_<Element>.png` | `Art/UI/` |
| Terrain | `T_TERR_<Biome>_<Layer>_BC\|_N.png` | `Art/Shared/Textures/` |

**Wichtig:** `PF_PROP_*` wird bewusst **nicht** auto-registriert (der Parser
verlangt genau vier Namensteile). Props werden von Hand verdrahtet.

---

## P0 — Ohne das ist keine Runde lesbar

### P0-1 · Aetherium-Kristallfeld

> Die einzige Ressource des Spiels ist heute ein Haufen aus **sieben
> eingefärbten Würfeln**. Der gesamte Wirtschaftskreislauf spielt sich an einem
> Objekt ab, das nicht als Rohstoff lesbar ist.

**Dateien:** `SM_PROP_AetheriumCrystal_A/_B/_C.fbx` ·
`T_PROP_AetheriumCrystal_BC/_N/_MSK.png` · `M_PROP_AetheriumCrystal.mat` ·
`PF_PROP_AetheriumCrystalCluster.prefab`
**Zielordner:** `Assets/_Project/Art/Shared/{Meshes,Textures,Materials}/`

**Spezifikation:**
- Drei Splitter-Varianten, je LOD0/1/2 = 1.000 / 400 / 150 Tris
- **Ein** Textursatz 1024² BC/N/MSK für alle drei (Atlas-Pflicht)
- Emissive-Anteil im Kristallkörper — der Glow entsteht über HDR-Emissive plus
  Bloom, es gibt bewusst keine echten Lichtquellen pro Kristall
- Cluster-Prefab: 5–9 Splitter innerhalb **einer** Grid-Zelle (3,0 × 3,0 m),
  Höhe 1,5–4,0 m, Origin Bodenmitte `Y = 0`
- **Zusätzlich eine erschöpfte Variante** (Stumpf, ohne Emissive) — die
  Wirtschaft läuft einen endlichen Zyklus, das Feld muss sichtbar leerlaufen
  können

**Code-Abhängigkeit:** Der Feldmarker hat keinen Skalierungs-Normalisierungspfad
wie die Einheiten-Darstellung — der Maßstab muss beim Einbau einmal von Hand
gesetzt werden.

### P0-2 · Baustellen-Meshes

> **Jedes** Gebäude beider Fraktionen ist während der gesamten Bauzeit ein
> flaches graues Brett. Bau ist die häufigste Aktion im Spiel und hat null
> visuelle Repräsentation.

**Dateien:** `SM_PROP_ConstructionSite_2x2/_3x3/_4x4.fbx` ·
`T_PROP_ConstructionSite_BC/_N/_MSK.png` · `M_PROP_ConstructionSite.mat` ·
`PF_PROP_ConstructionSite_<Größe>.prefab`
**Zielordner:** `Assets/_Project/Art/Shared/`

**Spezifikation:**
- Drei Footprint-Größen: 6,0 m / 9,0 m / 12,0 m Kantenlänge, Origin Bodenmitte
- Inhalt: Fundamentplatte, Gerüst, Materialstapel, Bakenmasten an den Ecken
- Fraktionsneutral, Teamfarbe über die `_MSK`-B-Kanal-Zone an den Baken
- LOD0/1/2 = 2.000 / 800 / 200 Tris, Textursatz 1024²
  *(das Budget-Dokument hat für Baustellen keine Zeile — das ist eine
  art-seitige Vorgabe analog zum Verteidigungsmodul)*

**Deutlich wertvoller, wenn möglich:** Die Gerüstteile so aufteilen, dass der
Baufortschritt in 3–4 Stufen über aktivierte Untergruppen gezeigt werden kann —
das Bausystem führt bereits einen kontinuierlichen Fortschrittswert, der das
treiben könnte.

### P0-3 · Bau-Platzierungs-Ghost

> Es gibt **keine** Bauvorschau. Das Gebäude wird auf Tastendruck sofort an die
> Mausposition gesetzt; fünf Ablehnungsgründe sind unsichtbar. Basenbau ist
> Raten.

**Dateien:** `M_FX_BuildGhost_Valid.mat` · `M_FX_BuildGhost_Invalid.mat` ·
`T_FX_BuildGrid_BC.png` · `T_FX_FootprintEdge_BC.png`
**Zielordner:** `Assets/_Project/Art/Shared/{Materials,Textures}/`

**Spezifikation:** Zwei transparente URP-Materialien (Valid grünlich nahe
`#58D5E8`, Invalid rot), gedacht für die Nutzung **mit dem LOD1-Mesh des
jeweiligen Gebäudes** — es braucht also kein eigenes Mesh. Dazu eine kachelbare
Gitterkachel 256² RGBA (1 Kachel = 1 Zelle = 3,0 m) und eine
Footprint-Kantenmarkierung 512² RGBA für die 2×2/3×3/4×4-Umrisse.
Optional nützlich: eine Reichweiten-Ringtextur 1024² für den 8-Zellen-Bauradius.

### P0-4 · 34 Team-Masken *(die wichtigste Einzelposition der Liste)*

> Vorhanden: 34 BaseColor-Texturen, **0 Masken**. Ohne Maske legt die
> Präsentation die Fraktionsfarbe über **jeden** Renderer des Prefabs — die
> gesamte gelieferte Bemalung wird mit `#8A9199` bzw. `#7A3524` multipliziert
> und zusätzlich nach Trefferzustand abgedunkelt. **Die Arbeit des Grafikers ist
> im Spiel faktisch unsichtbar.**

**Dateien:** `T_<BLDG|UNIT>_<Faction>_<Role>_MSK.png`, 34 Stück
**Format:** Einheiten 1024², Gebäude 2048², **linear (kein sRGB)**, Alpha erhalten

**Kanalbelegung, verbindlich:**
`R` = Metallic · `G` = Occlusion · `B` = TeamMask · `A` = Smoothness

Die TeamMask ist eine **Graustufe 0…1 mit weichen Übergängen**, kein Binärwert.
Flächenkorridor: Allianz 10–20 % (Panzerkanten, Leuchtelemente), Legion 40–60 %
(großflächige Platten).

**Bei gestaffelter Lieferung zuerst:** Allianz-HQ, Legion-HQ,
Allianz-LightTank, Legion-LightTank (die vier Vertical-Slice-Assets).

---

## P1 — Macht das Spiel verständlich

### P1-1 · Icon-Satz für Bauleiste und Befehlskarte

**Dateien:** 34 × `UI_ICON_<Faction>_<Role>.png` + 12 ×
`UI_ICON_CMD_<Command>.png`, je 128² RGBA
**Zielordner:** `Assets/_Project/Art/UI/Icons/`

Ohne Icons gibt es keine Bauleiste, und niemand außer dem Entwickler kann das
Spiel bedienen.

Rollen-Icons aus den vorhandenen Concept-Blättern ableiten (gleiche Silhouette,
auf Icon-Lesbarkeit reduziert), einheitlicher Innenabstand, Fraktionsfarbe nur
als Akzent.

Die zwölf Befehls-Icons sind exakt die ausführbaren Befehle — mehr kann das
Spiel nicht: Move, Stop, Attack, Harvest, ReturnCargo, Build, Repair, Sell,
CancelConstruction, QueueUnit, CancelProduction, SetRallyPoint.

Dazu je zwei Zustandsvarianten (aktiv / gesperrt) für T2-Sperre und fehlende
Voraussetzung. Alle Icons zusammen ≤ 2048² Atlasfläche — bei 46 Icons à 128²
sind das rund 18 % Belegung.

### P1-2 · Auswahlmarker

**Dateien:** `T_FX_SelectionRing_BC.png` (512² RGBA) · `M_FX_SelectionRing.mat` ·
optional `_Enemy` / `_Neutral` · `T_FX_SelectionBox_BC.png` (64², 9-slice)

Ring- oder Klammertextur mit Alpha, weicher Innenrand, für bodennahe Quad- oder
Decal-Projektion. **Neutral weiß ausliefern**, damit die Färbung nach Besitzer
im Material erfolgt; die Größe skaliert der Code pro Rolle.

### P1-3 · Lebensbalken

**Dateien:** `UI_HealthBar_Frame.png` (64×12) · `UI_HealthBar_Fill.png` (64×12),
RGBA mit 9-slice-Rändern
**Zielordner:** `Assets/_Project/Art/UI/`

Heute wird der Trefferzustand als Farbverdunkelung des ganzen Modells kodiert —
das kollidiert direkt mit P0-4 und ist bei zwanzig Einheiten nicht ablesbar.
Hoher Kontrast gegen den Wüstenboden, lesbar bei Kamerahöhe 18–90 m.

### P1-4 · Mauszeiger-Satz

**Dateien:** `UI_CUR_Default`, `_Move`, `_Attack`, `_Harvest`, `_Repair`,
`_Sell`, `_BuildValid`, `_BuildInvalid`, `_NoEntry` — je 32² und 64² RGBA,
**Hotspot dokumentiert**
**Zielordner:** `Assets/_Project/Art/UI/Cursors/`

Der Kontextcursor ist im RTS die Hauptrückmeldung, ob ein Klick bewegt,
angreift, erntet oder ablädt. Alle vier Fälle existieren in der Simulation und
sind für den Spieler ununterscheidbar.

### P1-5 · Minimap-Grafik

**Dateien:** `UI_Minimap_Frame.png` (512²) ·
`T_TERR_Glutrinne_MinimapBase.png` (512², Kartenrelief) ·
`UI_Minimap_Blip_<Unit|Building|Resource|Radar>.png` (je 16²)

**Blips mit klar unterscheidbaren Grundformen** (Punkt / Quadrat / Raute /
Ring), nicht nur Farbe — die Farbe trägt schon den Besitzer.

### P1-6 · 34 Normal Maps

**Dateien:** `T_<BLDG|UNIT>_<Faction>_<Role>_N.png`, Einheiten 1024², Gebäude
2048², Import-Typ Normal Map, Tangenten Mikktspace

Vorhanden: 0 von 34, der Bump-Slot aller 34 Materialien ist leer. Ohne Normalen
lesen sich Panzerplatten, Nieten und Fugen — genau das, was den
Fraktionsunterschied im Concept trägt — als glatte Flächen.

**Gemeinsam mit P0-4 beauftragen**, beide können in einem Zug gebacken werden.

### P1-7 · Glutrinne-Bodentexturen

**Dateien:** `T_TERR_Glutrinne_Sand_BC/_N.png`, `_Rock_BC/_N.png`,
`_Cracked_BC/_N.png`, `_AetheriumStain_BC/_N.png` (je 2048², kachelbar) ·
`T_TERR_Glutrinne_Splat.png` (1024² RGBA)

Der Boden der einzigen Karte ist eine einzige Volltonfläche `#B89A6B`. Auf einer
texturlosen Fläche ist Einheitenbewegung kaum wahrnehmbar — es fehlt jeder
Bezugspunkt. Rahmen: 2–4 Layer à 2048² plus Splat-Map 1024², BC7. Die
Aetherium-Verfärbung als vierter Layer erklärt nebenbei, warum an dieser Stelle
Ressourcen liegen.

### P1-8 · Befehlsmarker

**Dateien:** `T_FX_MoveMarker_BC.png`, `T_FX_AttackMarker_BC.png`,
`T_FX_RallyPoint_BC.png` (je 256² RGBA) · `PF_PROP_RallyFlag.prefab`

Kurzlebige Bodenmarkierung für Move und Attack (Ring bzw. Fadenkreuz, für rund
0,5 s Animation ausgelegt), dauerhafte Fahne für den Rally Point mit
Teamfarben-Zone in der `_MSK`. Der Rally Point ist ohne Marker praktisch
unbenutzbar, weil man nicht sehen kann, wo er gesetzt wurde.

### P1-9 · Legion-Emissive nachziehen

Wörtlich im Importprotokoll dokumentiert: *„Kein Emissive bei Legion. Jedes
Legion-Concept lebt von orangem Glühen. Die Fraktionslesbarkeit auf Distanz ist
dadurch asymmetrisch."*

**Technische Klärung nötig, bevor beauftragt wird:** Der Art-Standard erlaubt
nur **einen** Textursatz (BC/N/MSK) pro Asset — eine vierte Map sprengt formal
das Budget. Saubere Alternativen: Emissive in den Alphakanal der BaseColor legen
oder über die TeamMask-Zone treiben. **Diese Entscheidung gehört in den
Standard, nicht in die Lieferung.**

### P1-10 · Mesh-Nacharbeit am Tripo-Erstsatz — sechs benannte Defekte

Alle sechs sind im Importprotokoll bereits belegt und müssen nicht gesucht
werden:

1. An **beiden** DefensePlatform-Modellen schwebt ein abgelöstes Bruchstück
   neben dem Sockel — im Spiel sichtbar.
2. Allianz-HQ ist 21,1 m und Allianz-Radar 20,0 m hoch (Folge der
   Footprint-Skalierung schlanker Türme). Sichtprüfung in der Spielkamera steht
   aus, gegebenenfalls Höhe deckeln.
3. Dem Allianz-Harvester fehlt der Greifarm (nur ein Stummel).
4. Die Legion-Panzerabwehrinfanterie hat ein statt zwei Werferrohren.
5. **Den Allianz-Panzern fehlt das Doppelrohr, das im Concept LightTank von
   BattleTank unterscheidet** — dadurch sind sie im Spiel nicht auseinander zu
   halten.
6. Legion ResearchLab/Power und Legion LightTank/BattleTank sind aus demselben
   Grund nicht sicher zugeordnet.

**Punkt 5 und 6 sind Spielbarkeit, nicht Kosmetik:** Der Spieler muss teure
T2-Einheiten von billigen T1-Einheiten unterscheiden können.

Zusätzlich: Beide DefensePlatform-Modelle wurden gegen das falsche Tri-Budget
konvertiert (LOD0 ≈ 4.500 statt 1.500 Tris) — bei einer Neulieferung mitziehen.

---

## P2 — Macht es gut

### P2-1 · Kampf-VFX

**Dateien:** `VFX_MuzzleFlash_Kinetic/_Explosive.prefab` · `VFX_Tracer.prefab` ·
`VFX_Impact_Kinetic/_Explosive.prefab` · `T_VFX_CombatAtlas.png` (1024²)
**Zielordner:** `Assets/_Project/Art/Shared/VFX/`

Kampf ist im Bild vollständig unsichtbar. Ohne Mündungsfeuer sieht der Spieler
nicht, **wer auf wen** schießt, und kann seine Armee nicht steuern.

Budget hart vorgegeben: Mündungsfeuer und Tracer ≤ 15 Partikel je Effekt bei
max. 200 gleichzeitig, Treffer und Einschlag ≤ 30 bei max. 150, Gesamtdeckel
10.000 aktive Partikel. Flipbook-Frames in einem gemeinsamen Atlas (global max.
4 Atlanten à 1024² für **alle** VFX). Zwei Ausprägungen genügen — Kinetic und
Explosive sind die einzigen im Spiel genutzten Schadenstypen.

### P2-2 · Zerstörungs-VFX und Trümmer

**Dateien:** `VFX_Explosion_Unit.prefab` · `VFX_Explosion_Building.prefab` ·
`SM_PROP_Rubble_2x2/_3x3/_4x4.fbx` · `T_PROP_Rubble_BC/_N.png`

Einheiten und Gebäude verschwinden heute ohne jeden Übergang. Damit fehlt die
wichtigste Rückmeldung des Spiels — bei einer HQ-Zerstörung entscheidet sich
sogar das Match. Budget: Einheitentod ≤ 80 Partikel (max. 40 gleichzeitig),
Gebäudezerstörung ≤ 150 (max. 10). Trümmer ≤ 800/300/100 Tris.

### P2-3 · Wirtschafts-VFX

**Dateien:** `VFX_HarvestBeam.prefab` · `VFX_HarvestDust.prefab` ·
`VFX_RefineryUnload.prefab` · `VFX_ConstructionDust.prefab` ·
`VFX_RepairSparks.prefab`

Der Wirtschaftskreislauf läuft vollständig ohne Bild. Der Spieler kann nicht
erkennen, ob ein Harvester sammelt, ablädt oder nur herumsteht. Das ist die
häufigste Dauerbewegung im Bild und trägt am meisten zum Eindruck „das Spiel
lebt" bei. Aetherium-Effekte im Cyan der Ressource, Bau- und Reparaturstaub
neutral.

### P2-4 · Himmel und Umgebung

**Dateien:** `T_SKY_Glutrinne_Panorama.hdr` (4096×2048) · `M_SKY_Glutrinne.mat`

Die Szene läuft auf Unitys Default-Skybox und ohne Nebel. Der Himmel liefert
nicht nur Optik, sondern die **Umgebungsbeleuchtung**, aus der alle Modelle ihr
Fülllicht ziehen. Zielstimmung: klares Wüstenprofil, hohe warmweiße harte Sonne,
gebleichter heller Bodenambient. Günstigere Alternative, falls HDR zu schwer
wiegt: Gradient-Cubemap 1024² je Fläche.

### P2-5 · Gelände-Props Glutrinne

**Dateien:** `SM_PROP_Rock_A/_B/_C.fbx` · `SM_PROP_Cliff_Straight/_Corner.fbx` ·
`SM_PROP_DeadShrub_A/_B.fbx` · `SM_PROP_Wreck_A/_B.fbx` + je Textursatz 1024²

Die Karte ist eine leere Fläche mit vier Randbalken. Ohne Props gibt es keine
Orientierung, keine Deckung und keine Wegführung — der Spieler kann Positionen
auf der 128×128-Karte nicht benennen oder wiedererkennen. Die Kliff-Module
ersetzen mittelfristig den Behelfs-Randbalken.

### P2-6 · Mesh-Zerlegung für bewegte Teile

Das Manifest benennt den Animationsbedarf bereits: Turmrotation und Rohrrückstoß
am BattleTank, Rohrrückstoß und Aufstellung an der Artillerie, Ladearm am
Harvester, Bauarm am Builder.

Die gelieferten Tripo-Modelle sind je LOD-Stufe **ein einziges verschmolzenes
Mesh** — Code-Animation ist daran nicht möglich. Ein Panzer, dessen Turm
geradeaus zeigt, während er seitlich schießt, liest sich als kaputt.

**Erforderlich:** Benannte Kindobjekte mit korrekt gesetztem Drehpunkt (Turm um
Y an der Turmachse, Rohr um X am Rohrlager), mindestens in LOD0 und LOD1.
Betrifft BattleTank, LightTank, Artillery und DefensePlatform je Fraktion.

### P2-7 · Infanterie-Rigs und Animationen

**Dateien:** `SM_UNIT_<Faction>_BasicInfantry` und `_AntiArmorInfantry` mit
Humanoid-Rig (Mecanim) + Clips `Idle`, `Walk`, `Shoot`, `Reload`, `Die`

Vier der 34 Assets sind Infanterie, und alle vier gleiten als starre Statuen
über den Boden. Zusätzlich mitzudenken: gekoppeltes Animations-LOD (nah 30–60 Hz,
mittel 15–30 Hz, fern pausiert, abseits des Bildschirms komplett gecullt) —
sonst kippt das Frame-Budget bei 500 Einheiten.

Bewusst P2 und nicht P1: Gleitende Infanterie ist hässlich, aber das Spiel bleibt
bedienbar — und das ist der größte Einzelposten dieser Liste.

### P2-8 · Nebel-Optik

**Dateien:** `T_FX_FogNoise_BC.png` (512², kachelbar) ·
`T_FX_FogEdge_BC.png` (256² Gradient) · `M_FX_FogOfWar.mat`

Die Simulation führt drei Sichtzustände je Team, aber es gibt keine Darstellung
des Nebels selbst. Der Spieler kann nicht sehen, **wo er blind ist** — der halbe
taktische Wert des Systems bleibt ungenutzt.

### P2-9 · Schadenszustände

**Dateien:** `T_FX_DamageDecal_Light/_Heavy.png` (je 1024² RGBA)

Zwei Stufen genügen: leichte Ruß- und Kratzspuren, schwere Beschädigung mit
freigelegtem Metall und Bruchkanten. Fraktionsübergreifend nutzbar, deshalb ein
einziger Satz.

---

## P3 — Politur

### P3-1 · Fraktionslogos

`UI_LOGO_Alliance.png` und `UI_LOGO_Legion.png` (je 1024² RGBA plus 256²- und
64²-Ableitung), zusätzlich als SVG.

Es existiert kein Fraktionszeichen. Die Fraktionsidentität hängt derzeit
vollständig an zwei Hex-Farben. Formensprache aus dem Style-Guide ableiten:
**Allianz** geschlossen, symmetrisch, vertikal — **Legion** offen, asymmetrisch,
waagerecht, sichtbar geflickt.

### P3-2 · Menü- und Rahmenwerk

`UI_KeyArt_MainMenu.png` (2560×1440) · `UI_LoadingScreen_Glutrinne.png`
(2560×1440) · `UI_Frame_Panel.png` · `UI_Button_Normal|_Hover|_Pressed|_Disabled.png`
(9-slice) · `UI_Bar_Resource.png`

Solange die Bauleiste (P1-1) nicht steht, ist Menü-Artwork verfrüht; danach ist
es die erste Stelle, an der das Spiel wie ein Produkt aussieht.

### P3-3 · Portraits — die günstigste Position der Liste

`UI_PORTRAIT_<Faction>_<Role>.png`, 34 × 256² RGBA.

Die 34 Concept-Blätter existieren bereits, sind bereits 1024² und haben bereits
einen einheitlichen Bildaufbau (frontal, zentriert, 78 % Bildhöhe, Hintergrund
`#0B1017`). Ein Zuschnitt plus Herunterskalieren genügt weitgehend.

*Verwandte Baustelle:* Ein Portrait ohne Namen bleibt halb nutzlos — siehe
Masterplan 2.5.

### P3-4 · Ergebnisbildschirme und App-Icon

`UI_Screen_Victory.png`, `_Defeat.png`, `_Draw.png` (je 1920×1080) + App-Icon in
16/32/64/128/256/512/1024.

Drei Bildschirme decken alle vier Ergebniscodes ab, weil sich die beiden
Unentschieden-Fälle einen teilen können. Reine Politur — aber der letzte
Eindruck jeder Runde.

---

## Lieferweg und Abnahme

**Der Grafiker liefert in denselben Ordnerbaum wie die 34 vorhandenen Assets.**
Wie die Binärdaten von dort ins Projekt kommen, hängt an Entscheidung E-1
([README.md](README.md)) — heute schließt `.gitignore` sie aus.

**Provenienz ist Pflicht vor Repo-Aufnahme.** Je Lieferung: `licenseId`,
`licenseUrl`, `providerTermsUrl`, `commercialUseGranted`, `attributionRequired`,
`redistributionAllowed`, verifizierbare Quell-URL, SHA-256, Vier-Augen-Prüfung.
Ausschlusskriterien mit absolutem Importverbot: unklare Lizenz, NC-Lizenz,
Weitergabe untersagt, fehlende Quell-URL.

**Abnahmeprüfung je Asset:** Namensmuster exakt · Origin Bodenmitte `Y = 0` ·
Vorderseite `+Z` · LOD-Kette in einer Datei mit den Standardschwellen ·
Tri-Budget eingehalten · Textursatz vollständig (BC + N + MSK) · Maskenkanäle
korrekt belegt · Silhouette bei Kamerahöhe 18–90 m eindeutig einer Rolle
zuordenbar.

## Offene Punkte

- P0-0 (Namenskonvention) muss vor der ersten Lieferung entschieden werden.
- P1-9 (Emissive-Kanal) braucht eine Standard-Entscheidung, keine Lieferung.
- E-1 (Binärdaten-Ablage) und E-2 (Tripo-Sperre) aus [README.md](README.md)
  entscheiden mit, ob P1-10 eine Nacharbeit oder eine Neubestellung ist.
- Die Footprint-Zuordnung (2×2 / 3×3 / 4×4) ist im Design ausdrücklich als
  Annahme markiert und bei einer Grid-Finalisierung nachzuziehen.
- 30 der 34 Concept-Blätter haben keine schriftliche Abnahme gegen den
  Style-Guide; der geprüfte Pilotstapel zeigte bereits systematische
  Abweichungen beim Füllgrad.

## Nächste Schritte

1. P0-0 entscheiden und in
   [../../assets/ArtAssetStandard.md](../../assets/ArtAssetStandard.md)
   nachtragen.
2. P0-1 bis P0-4 beauftragen — diese vier blockieren die Lesbarkeit jeder Runde.
3. P1-6 und P0-4 gemeinsam beauftragen (ein Backvorgang).
