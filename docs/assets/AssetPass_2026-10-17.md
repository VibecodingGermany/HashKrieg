# Asset-Pass: Blickrichtung, Teile-Animation, Optik

**Version:** 0.7.0 | **Status:** Entwurf – Graybox-/Demo-Spur, kein Gate-Nachweis | **Verantwortungsbereich:** Technical Art | **Datum:** 2026-10-17

## Zweck

Protokolliert den zweiten Durchgang über alle 34 Tripo-Assets aus
[AssetImport_Tripo_2026-08-06.md](AssetImport_Tripo_2026-08-06.md): Einheiten
schauen jetzt in ihre Fahrtrichtung, Fahrzeuge und einige Gebäude haben
bewegliche Teile, und die Texturen sind auf die D-072-Paletten gehoben. Das
Dokument beschreibt, wie der Durchgang reproduziert wird, wie er geprüft wurde
und was er **nicht** behebt. Die Entscheidung dahinter ist
[D-110](../production/DecisionLog.md).

## Abhängigkeiten

- [AssetImport_Tripo_2026-08-06.md](AssetImport_Tripo_2026-08-06.md) – das Konvertat, von dem dieser Pass ausgeht
- [ArtAssetStandard.md](ArtAssetStandard.md) – Namen, LODs, `_MSK`-Kanäle
- [../tech/AnimationSystem.md](../tech/AnimationSystem.md) – rig-lose Code-Animation für Fahrzeuge und Gebäude
- [AssetPackage.md](AssetPackage.md) – warum die Binärdaten nicht im Repo liegen

## 1. Was falsch war

1. **Bewegungsrichtung (Code, nicht Asset).** Die Simulation misst den Kurs
   mathematisch (`SimTrig.Atan2`: 0 = Sim-+X, gegen den Uhrzeigersinn), Unity
   den Gierwinkel ab +Z im Uhrzeigersinn. `UnitViewManager` setzte den Sim-Winkel
   direkt als Unity-Gier ein. Jede Einheit war dadurch an der Nordost-Diagonale
   gespiegelt: Wer nach Osten fuhr, schaute nach Norden. Die Modelle selbst
   zeigen korrekt nach +Z; geprüft per Draufsicht aller 34 Prefabs.
2. **Risse.** Jedes Tripo-Mesh besteht aus rund 1000 losen Schalen (eine je
   UV-Insel), die teils zentimeterweit auseinanderliegen. Mit Backface-Culling
   sah man durch jede Lücke den Sandboden.
3. **Zu dunkle Texturen.** Tripo backt Licht in die Basisfarbe; die mittlere
   Helligkeit lag zwischen 5 und 21 %. Die Allianz war einheitlich Petrol statt
   Stahlgrau (D-072).
4. **Keine beweglichen Teile.** Jedes Modell war ein einziges Mesh.
5. **Einzelfehler:** Das Doppelrohr der Allianz-Verteidigungsplattform schwebte
   2,5 m vor dem Turm, bei der Legion lag eine lose Platte davor, und am
   Legion-Bauer hing ein 6 m langer Splitter, der über die Renderer-Bounds die
   ganze Einheit im Spiel schrumpfen ließ.

## 2. Werkzeuge

| Werkzeug | Läuft in | Aufgabe |
|---|---|---|
| `tools/art/asset_pass.py` | Blender 5.2 headless | Ein Asset nach Rezept bearbeiten und exportieren |
| `tools/art/asset_recipes.json` | – | Rezepte aller 34 Assets: Teile, Reparaturen, Farbparameter |
| `tools/art/asset_layout.py` + `layout_grid.py` | Blender + System-Python | Orthografische Ansichten mit normiertem Raster als Vorlage für Schnittboxen |
| `Nova.Editor.ArtPreviewCapture` | Unity batchmode | Jedes `PF_*` im echten URP-Renderer fotografieren, wahlweise in Fahrt-/Kampfpose |
| `UnitMotionPresentationTests` | Unity PlayMode | Nachweis in der laufenden Partie, dass jede fahrende Ansicht in Fahrtrichtung schaut |

Quelle ist immer das unveränderte Konvertat unter
`Hashkrieg_Assets/3d/unity_ready`; geschrieben wird nur nach
`Assets/_Project/Art`. Ein Lauf ist also wiederholbar und überschreibt keine
Eingabe. Der Zustand vor diesem Pass liegt zusätzlich als
`Hashkrieg_Assets/backup/Art_vor_AssetPass_2026-10-17.tar.gz`.

```bash
# ein Asset (Blender headless)
/Applications/Blender.app/Contents/MacOS/Blender -b --python tools/art/asset_pass.py -- \
    --asset UNIT_Alliance_BattleTank --src ../Hashkrieg_Assets/3d/unity_ready --dst Assets/_Project/Art
# Prefabs neu bauen und alles fotografieren (Unity batchmode, Editor geschlossen)
Unity -batchmode -projectPath . -executeMethod Nova.Editor.ArtPreviewCapture.CaptureFromCommandLine \
    -captureBuildPrefabs -captureOut "$PWD/output/art-preview" -captureTime 1.3 -logFile cap.log -quit
```

## 3. Was der Pass je Asset tut

1. **Aufräumen:** Splitterinseln (≤ 6 Faces, lang und dünn) entfernen, Rezept-Boxen
   löschen (`drop_boxes`) oder verschieben (`move_boxes`).
2. **Normalen:** UV-Nähte verschweißen (0,2 % der Diagonale), nach Kantenwinkel
   glätten, gewichtete Normalen. Mildert den Knitterlook; die Geometrie selbst
   bleibt Tripo.
3. **Textur:** Belichtung auf eine Zielhelligkeit mit weicher Lichterschulter,
   Sättigung je Fraktion, Gradient-Map auf die D-072-Palette, fremde Farbtöne
   (Aetherium-Kristalle, Warnlichter) bleiben erhalten. Leuchtakzente werden als
   `T_*_EM` ausgeschnitten, dazu `T_*_MSK` nach §5.1 des Standards (B = TeamMask
   bleibt vorerst 0).
4. **Teile:** Trennen nach Rezept und Export als Knoten-Hierarchie:

   | Knoten | Bewegung im Spiel | Assets |
   |---|---|---|
   | `Body` | Federung: Nicken beim Anfahren/Bremsen, Neigen in Kurven, Wippen; Infanterie: Schritt-Wippen | alle |
   | `Turret` | schwenkt mit begrenzter Rate aufs sichtbare Angriffsziel | Kampf-/Leichtpanzer, Artillerie, Verteidigungsplattformen, Legion-Scout (MG), Legion-Bauer (Oberwagen) |
   | `Gun` | Rückstoß bei jedem sichtbaren Schuss | Panzer, Artillerie, Verteidigungsplattformen |
   | `Wheel_*` | rollt mit der gefahrenen Strecke | beide Scouts, Legion-Sammler |
   | `Arm_*` | pendelt beim Arbeiten (Abbauen, Bauen) | Allianz-Sammler, beide Bauer |
   | `Spin_*` | dreht dauernd | Allianz-Radar, Allianz-Raffinerie (Kristall), Allianz-HQ (Antenne) |
   | `Scan_*` | schwenkt hin und her | Legion-Radar |
   | `Leg_*` / `Shin_*` | Oberschenkel um die Hüfte, Unterschenkel ums Knie: Schrittzyklus | alle vier Infanteristen |

   Gebäude „atmen“ zusätzlich über ihre Leuchtakzente und pulsieren schneller,
   solange sie produzieren (nur eigene Gebäude, sonst verriete der Puls fremde
   Produktion durch den Nebel).

Die Unity-Seite (`UnitRigAnimator`) findet die Knoten über den Namen und misst
ihre Achsen einmal im Elternraum. Ein Modell ohne Knoten bekommt einen
`Body` eingefügt, damit auch Infanterie und Gebäude ohne Teile sich bewegen.

## 4. Unity-Import: zwei Fallen

- **Ein FBX mit genau einem Wurzelknoten verliert seine Achsen-Drehung**, wenn
  Unity den Knoten in das Modellobjekt zusammenlegt; das Modell liegt dann auf
  der Seite. Der Import setzt deshalb `preserveHierarchy`.
- **`bakeAxisConversion` ist keine Lösung:** Es dreht die älteren Einzelmesh-FBX
  um 180°, sie fahren dann rückwärts. Der Import setzt es ausdrücklich auf
  `false`, weil ein einmal gesetzter Wert sonst in der `.meta` überlebt.

Der Wurzelknoten trägt in Unity Skalierung 100 (Zentimeter-Umrechnung).
Translationen der Animation werden deshalb in den Maßstab des jeweiligen
Elternknotens umgerechnet.

## 5. Nachweis

- EditMode `ViewHeadingTests`: Abbildung Sim-Kurs → Unity-Gier für acht
  Richtungen und eine schiefe.
- PlayMode `UnitMotionPresentationTests`: sechs Einheiten fahren in sechs
  Richtungen über echte Wegfindung; in 90–100 % der Fahrframes liegt die
  Blickrichtung unter 25° zur gemessenen Bewegung (Ausreißer beim Anfahren
  durch Ausweichsteuerung). **Gegenprobe:** Mit der alten Abbildung schlägt der
  Test fehl (bis 180° Abweichung).
- Läufe vom 2026-10-17 auf diesem Stand: headless 768/768, EditMode 676/676,
  PlayMode 16/16.
- Draufsicht und Fahrt-/Kampfpose aller 34 Prefabs mit `ArtPreviewCapture`
  gesichtet.

## 6. Glow und Licht (zweiter Schritt, D-111)

- **Glow:** Leuchtakzente laufen jetzt durch Bloom. Zwei Masken trennen
  „einfärben“ (Zierstreifen in Akzentfarbe) von „leuchten“ (strengere Schwelle,
  0,5–2 % der Textur); unter dem Leuchtakzent bleibt die Grundfarbe dunkel,
  sonst brennt er weiß aus. URP behält `_EMISSION` nur mit GI-Flag
  `RealtimeEmissive` — mit `None` strich die Material-Validierung das Keyword,
  und die ersten Glow-Versuche zeigten schlicht nichts.
- **Licht:** Die Sonne kommt jetzt von der Kameraseite. Die dunklen
  Allianz-Gebäude lagen am Gegenlicht, nicht an der Textur; trotzdem ziehen die
  Rezepte der Allianz-Gebäude etwas heller (`target_mean` 0,42).
- **Allianz-Radar:** Die 20-m-Säule ist auf 45 % gestaucht (`squash`), sie maß
  im Spiel mehrere Gebäudehöhen.
- **Nachweis in Spielgröße:** `ArtShowcaseTests` stellt alle Gebäude und
  Einheiten beider Fraktionen in die laufende Partie und fotografiert sie durch
  die Hauptkamera (mit Post-Processing) aus 18, 30 und 42 m Höhe nach
  `output/demo/showcase_*.png`.

## 7. Infanterie-Gang (dritter Schritt)

Die Beine der vier Infanteristen sind in Blender als starre Glieder abgetrennt
(Oberschenkel mit Pivot an der Hüfte, Unterschenkel am Knie; die Boxen lassen
herabhängende Hände und Waffen am Körper). `UnitRigAnimator` fährt daraus einen
Zwei-Gelenk-Gang: Oberschenkel ±25°, Knie bis 35° in der Schwungphase, rechts
einen halben Zyklus hinter links; die Hüfte senkt sich genau um die Verkürzung
der gespreizten Beine, damit die Füße am Boden bleiben, und beim Schuss zuckt der
Oberkörper zurück. Die Kadenz folgt der gemessenen Geschwindigkeit, ist aber auf
3,2 Schritte/s gedeckelt: Im Spiel ist ein Soldat rund 1 Einheit groß und läuft
4 Einheiten/s, rutschfrei wären das etwa 9 Schritte/s. `UnitMotionPresentationTests`
nimmt den laufenden Soldaten zusätzlich als Bildfolge auf
(`output/demo/motion_infantry_*.png`).

## 8. Abwehrtürme: Maßstab (D-112)

Jedes Gebäude wird im Spiel auf die Breite seines 3×3-Footprints skaliert. Ein
schlanker Turm maß dadurch rund vier Soldatenhöhen. Der Pass stellt die Türme
jetzt auf einen flachen Betonsockel (`pad` im Rezept), der so breit ist, dass der
Turm nach der Skalierung 0,55 × Footprint hoch ist — rund 1,7 Einheiten, also
etwa doppelte Soldatenhöhe und so hoch wie ein Kampfpanzer. Der Sockel füllt die
blockierte Fläche, statt sie als unsichtbare Wand stehen zu lassen; seine UVs
zeigen auf eine ruhige, ungesättigte Stelle der Textur, er braucht kein
eigenes Material. **Der Footprint selbst bleibt 3×3** — ihn zu verkleinern ist
eine Regeländerung (siehe Offene Punkte).

## 9. Größenverhältnis (D-113)

Bis hierhin bekam jede Einheit die Größe ihres Graybox-Primitivs. Das ergab
Zufallsverhältnisse: ein Kampfpanzer doppelt so hoch wie ein Soldat und drei
Viertel so breit wie eine Kaserne, ein Sammler kürzer als ein leichter Panzer,
der Allianz-Bauroboter kleiner als ein Soldat. Jetzt gilt eine Regel
(`ArtViewScale`):

| Klasse | Regel | Ergebnis (Felder) |
|---|---|---|
| Gebäude | Breite = Footprint (3) | 3 breit, 2,8–5,4 hoch |
| Fahrzeuge | 0,22 Felder je Meter Modellmaß, für alle gleich | Kampfpanzer 1,5–1,7, leichter Panzer 1,3, Scout 1,0, Sammler 1,5–1,8 |
| Infanterie | Mindesthöhe 0,85 (Panzerfaust 0,9), unabhängig von der Waffenbreite | Soldat 0,85 hoch |
| Bauer | Mindesthöhe 1,1 | Allianz-Roboter 1,1, Legion-Bagger 1,4 |

Die Tripo-Modelle sind in Metern im selben Maßstab erzeugt, ihre Verhältnisse
untereinander bleiben damit erhalten. Ein Soldat reicht einem Kampfpanzer bis
etwa zur halben Höhe, ein Panzer hat ungefähr halbe Gebäudebreite: für ein RTS
leicht überzeichnet, aber lesbar. Der Auswahlring folgt jetzt der Modellgröße,
statt fest 2,4 Felder zu messen. Prüfwerkzeug: `ArtPreviewCapture -captureLineup`
stellt alle Modelle einer Fraktion in Spielgröße auf ein Feldraster und
protokolliert die Maße.

## 10. Wracks und Umgebungsbewegung (D-115)

`tools/art/asset_wreck.py` baut aus den getrennten Teilen der LOD0-Stufe ein
statisches Wrack-Mesh (`SM_*_Wreck.fbx`, 0,9–7,8 Tsd. Dreiecke) und eine
verkohlte Textur (`T_*_Wreck_BC.png`). Zufallswerte hängen am Asset-Namen, zwei
Läufe ergeben dasselbe Wrack. Erfahrungen:

- **Wracks aus LOD1 zerfallen**: die reduzierte Stufe der Tripo-Mehrschalen
  zeigt zerfetzte Flächen. Quelle ist deshalb LOD0.
- **Verbeulen nicht entlang der Normalen**: Hunderte loser Schalen zeigen in alle
  Richtungen und reißen auf. Ein glattes Vektorfeld verbiegt das Ganze wie Blech.
- **Bruchkante nach der höchsten Ecke**, nicht nach dem Mittelpunkt, sonst ragen
  große Dreiecke als Spieße über die Ruine; dünne Antennen werden nicht umgelegt.

Der Prefab-Bau hängt das Wrack als inaktives Kind `Wreck` mit eigenem Material
`M_*_Wreck` an. `ViewWrecks` schaltet beim Tod um (kurzes Schwarzwerden, Sprung,
Aufsetzen), lässt das Wrack neun Sekunden qualmen und liegen.

Umgebungsbewegung über `AmbientFx` (ein Staub- und ein Rauchsystem für alle):
Staubfahnen hinter fahrenden Fahrzeugen, Rauch aus `Smoke_*`-Knoten
(Legion-Raffinerie, -Kraftwerk, -HQ; Rezeptfeld `emitters`), Suchschwenk der
Türme nach 2,5 s ohne Ziel, Atmen und Umschauen der Infanterie, Motorvibration.

## 11. Was dieser Pass nicht löst

- **Die Geometrie bleibt Tripo-Qualität.** Flächen sind knubbelig; Glätten und
  doppelseitiges Rendern kaschieren, sie reparieren nicht. Eine
  Geometrie-AO war ein Fehlschlag: Auf überlappenden Schalen backen ganze Flächen
  schwarz. Sie ist deshalb im Rezept abgeschaltet.
- ~~Legion-Kraftwerk und Legion-Forschungslabor sind als Panzer generiert.~~
  Erledigt mit D-119, siehe Abschnitt 12.
- **Infanterie geht mit starren Gliedern, ohne Skelett.** Ober- und Unterschenkel
  sind abgetrennte Teile; an Hüfte und Knie entstehen beim Schwingen kleine
  Fugen, Arme und Waffe bleiben starr. Aus RTS-Distanz liest sich das als Gehen;
  ein Humanoid-Rig mit Gewichtung bleibt ein eigener Schritt.
- **Ketten laufen nicht.** Die Kettentextur liegt verstreut im Tripo-Atlas, ein
  UV-Scrollen ist nicht möglich.
- **Leuchtpuls über `MaterialPropertyBlock`.** Wie schon die Fraktionstönung;
  ArtAssetStandard §5 untersagt das für die 500-Objekt-Last. Bis der
  `NovaUnit`-Shader mit Instanzdaten kommt, ist das bewusst so.
- **TeamMask (B-Kanal) ist leer.** Spiegelpartien (Allianz gegen Allianz) sind
  farblich nicht unterscheidbar.

## 12. Legion-Gebäude und Sammler (Nachtrag, D-119)

**Vertauschte Gebäude.** Fahrzeugfabrik, Kraftwerk und Forschungslabor der Legion
waren schon im Konzeptbild Panzer: Die erste Bildgeneration (`generate.py`) gab
den Legion-Panzer als Referenzbild mit. Neue Konzeptbilder kommen aus
`generate_v2.py` (Materialtafel statt Form; Fabrik und Labor mit dem Zusatz
„Außenansicht eines Gebäudes, kein Fahrzeug“, liegen unter
`Hashkrieg_Assets/img/v2test`). Die Kaserne zerfiel als Tripo-Modell in zwei
Hälften mit schwebendem Wachturm.

Alle vier sind jetzt prozedural in Blender gebaut (`tools/art/procedural/`,
`build_all.sh`), die Textur kommt aus dem Konzeptbild: Box-Projektion (Front aus
dem Objektausschnitt, Seiten aus einem Wandstreifen, Dach aus einer ruhigen
Plattentextur), in Cycles auf Smart-UV gebacken, linear auf mittlere Luminanz
0,11 gehoben. Eine Gamma-Anhebung ließ den Asset-Pass-Grade ausbleichen.

Fallen:

- **Der Splitterfilter frisst modellierte Teile.** `glb_to_unity_fbx.py` trennt
  an UV-Nähten und hält dünne Leiterholme und Antennen für Generatorsplitter.
  Abhilfe: `--keep-slivers`; im Rezept zusätzlich `drop_needles: false` und
  `smooth_angle: 0`.
- **Hunyuan3D 2.1** (frei auf Hugging Face, ZeroGPU-Kontingent nach zwei Formen
  erschöpft) liefert gute Formen, ist aber in der EU nicht lizenziert und wurde
  deshalb verworfen.

**Sammler.** Der Legion-Laster hat eine eigene Mulde `Bed` (Scharnier hinten
unten), der Allianz-Roboter einen leeren Knoten `Cargo_Anchor` auf dem Rücken.
`UnitRigAnimator` hängt daran zur Laufzeit eine Erzladung (dunkles Gestein,
16 Aetherium-Kristalle), die mit `CargoAE` wächst. Beim Entladen (Kante
`CargoAE` > 0 → 0) kippt die Mulde 50° (Roboter: Körper 11°), die Ladung rutscht
heraus, Gesamtdauer 1,85 s. Weil die Simulation in einem Tick entlädt, hält die
Client-Eskorte den eigenen Sammler 1,9 s an der Raffinerie. KI-Sammler fahren
sofort weiter.

## Offene Punkte

- **Footprint der Abwehrtürme** (Inhaberentscheidung): `BuildingFootprintCells = 3`
  gilt global und ist im Code als vorläufig markiert. Ein 1×1- oder 2×2-Footprint
  nur für Türme berührt Platzierung, Wegfindung, KI und den Match-Fingerprint und
  verschiebt die Determinismus-Baselines; er bräuchte einen eigenen Regel-PR.

- Spielerische Abnahme in einer gespielten Runde steht aus; dieser Pass ist
  automatisiert und per Kamera geprüft, nicht gespielt.
- Das Asset-Paket ([AssetPackage.md](AssetPackage.md)) muss neu gebaut werden,
  damit andere den Stand bekommen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-10-17 | Erstfassung: Blickrichtungsfehler, Asset-Pass mit Rezepten, Teile-Animation, Prüfwerkzeuge und Grenzen | Technical Art |
| 0.2.0 | 2026-10-17 | Glow über Bloom, zwei Akzentmasken, Sonne auf der Kameraseite, Radar gestaucht, Showcase in Spielgröße (D-111) | Technical Art |
| 0.3.0 | 2026-10-17 | Infanterie-Gang mit starren Ober- und Unterschenkeln, Knoten `Leg_*`/`Shin_*` | Technical Art |
| 0.4.0 | 2026-10-17 | Abwehrtürme auf Sockel verkleinert, Footprint-Frage als offener Punkt (D-112) | Technical Art |
| 0.5.0 | 2026-10-17 | Größenverhältnis über eine Regel je Klasse, Turm auf 0,55 × Footprint, Größen-Aufstellung (D-113) | Technical Art |
| 0.6.0 | 2026-10-17 | Wrack-Varianten aller Assets und Umgebungsbewegung (D-115) | Technical Art |
| 0.7.0 | 2026-10-17 | Legion-Kraftwerk, -Fabrik, -Labor, -Kaserne prozedural (D-119), Sammler-Ladung und Abkippen | Technical Art |
