# Art Asset Standard

**Version:** 0.2.0 | **Status:** Entwurf – MS-1 Art-Strang verbindlich, kein Gate-Nachweis | **Verantwortungsbereich:** Technical Art / Producer | **Sprint:** 7

## Zweck

Dieses Dokument legt den verbindlichen Standard für Art-Assets von *Hashkrieg*
fest: Ordnerstruktur unter `Assets/_Project/Art/`, Dateinamenskonvention
für Meshes/Texturen/Materialien/Prefabs, LOD-Konvention, Unity-Import-Settings
und den Material-Standard inklusive der Team-Farben-Masken-Spezifikation. Es
schließt die bislang undokumentierte Lücke zwischen Blender-Quelldateien und
den in [../tech/AssetBudget.md](../tech/AssetBudget.md) definierten Budgets
und macht die Art-Pipeline für MS-1 reproduzierbar. Verbindlich für
Technical Art, Environment-/Character-Art und jeden Asset-Store-Import ab
Sprint 7.

## Abhängigkeiten

- [../tech/AssetBudget.md](../tech/AssetBudget.md) – Polycount-, Textur-, LOD- und Materialbudgets pro Asset-Klasse
- [../tech/NamingConvention.md](../tech/NamingConvention.md) – Daten-Ebene (`UNIT_`/`BLDG_`-ScriptableObjects, `DefinitionKey`-Schema)
- [../tech/Rendering.md](../tech/Rendering.md) – URP-Setup, SRP Batcher, GPU Resident Drawer, Team-Color-Shader-Konzept
- [../vision/CoreGameplay.md](../vision/CoreGameplay.md) – Kamera-Korridor (Zoom 18–90 m, Pitch 50–60°) als Grundlage der Texel-Density-Ableitung
- [Licenses.md](Licenses.md) – Lizenzprüfung für zugekaufte oder fremdbezogene Art-Assets
- [`quality/content/mvp-v1.json`](../../quality/content/mvp-v1.json) – kanonische Rollen-Liste je Fraktion (MS-1-Scope)

## 1. Ordnerstruktur

Alle Art-Assets liegen unter dem bestehenden `Assets/_Project/`-Root, in
einem eigenen `Art/`-Zweig, getrennt von der Daten-Ebene (`Assets/_Project/Data/`,
siehe [../tech/FolderStructure.md](../tech/FolderStructure.md)):

```text
Assets/_Project/Art/
├── Buildings/<Faction>/<Role>/
├── Units/<Faction>/<Role>/
├── Shared/
│   ├── Materials/
│   ├── Textures/
│   └── Meshes/
└── Source/            ← .blend-Quelldateien, nicht in Builds
```

- **`<Faction>`** ∈ `{Alliance, Legion}` – PascalCase, Englisch. Die
  kanonischen Manifest-IDs in `quality/content/mvp-v1.json` sind
  kleingeschrieben (`alliance`, `legion`); die Art-Ordnerebene verwendet die
  PascalCase-Form. Abbildung:

  | Manifest-ID (`factions[].id`) | Ordner-Token |
  |---|---|
  | `alliance` | `Alliance` |
  | `legion` | `Legion` |

  Evolvierte sind kein MS-1-Scope (siehe `AssetBudget.md` §Offene Punkte,
  Post-MVP-Vollroster) und haben daher aktuell keinen Ordner-Token.

- **`<Role>`** ist exakt die Rolle aus `quality/content/mvp-v1.json`
  (`buildings[].role` bzw. `units[].role`), nicht der lokalisierte
  `displayName`. Für MS-1 gültige Rollen:

  | Kategorie | Rollen |
  |---|---|
  | Gebäude | `HQ`, `Power`, `Refinery`, `Storage`, `Barracks`, `VehicleFactory`, `ResearchLab`, `Radar`, `DefensePlatform` |
  | Einheiten | `Builder`, `Harvester`, `BasicInfantry`, `AntiArmorInfantry`, `ScoutVehicle`, `LightTank`, `BattleTank`, `Artillery` |

- **`Shared/`** enthält fraktionsübergreifende Assets (z. B. Aetherium-Kristalle,
  Vegetation, generische VFX-Meshes) sowie Materialien/Texturen, die von
  mehreren `<Faction>/<Role>`-Assets referenziert werden – kein Duplizieren
  von Texturen zwischen Fraktionsordnern.
- **`Source/`** enthält ausschließlich `.blend`-Dateien (eine je Asset oder je
  eng verwandter Asset-Gruppe). Der Ordner wird über `.gitignore`/Build-Filter
  von Spieler-Builds ausgeschlossen; er ist Versionierungs- und
  Nachvollziehbarkeits-Quelle, kein Laufzeit-Content.

**Export-Konvention (Blender → Unity):** 1 Unity-Unit = 1 Meter. FBX-Export
mit `+Y up` / `−Z forward`, Scale Factor `1.0`, Apply Transform aktiv. Origin
liegt am Bodenmittelpunkt: bei Gebäuden auf der Footprint-Mitte mit `Y = 0`,
bei Fahrzeugen auf der Bodenkontaktebene mit `Y = 0`. Die Forward-Achse des
Assets entspricht `+Z` im Unity-Raum. Diese Konvention gilt für jedes Mesh
unabhängig von LOD-Stufe.

### Maßstab: Grid-Zellgröße (art-seitige Arbeitsannahme)

Die Simulation hat die Bau-/Bewegungs-Grid-Zellgröße noch nicht endgültig
festgelegt; `docs/gamedesign/Buildings.md` markiert die Gebäude-Footprints
selbst als offen. Ohne eine feste Zahl ist jedoch keine Gebäude-Modellierung
möglich. Für die Art-Produktion gilt deshalb ab dieser Version die **art-seitige
Arbeitsannahme: 1 Grid-Zelle = 3,0 Meter**. Diese Annahme darf von der
Simulation zu einem späteren Zeitpunkt überschrieben werden; bis dahin ist sie
die einzige Grundlage für Modellmaße.

Daraus ergeben sich die Modellmaße für die MS-1-Footprint-Größen:

| Footprint (Zellen) | Modellmaß (Breite × Tiefe) |
|---|---|
| 2×2 | 6,0 m × 6,0 m |
| 3×3 | 9,0 m × 9,0 m |
| 4×4 | 12,0 m × 12,0 m |

**Regel: Modellmaße folgen der Zellzahl, nicht umgekehrt.** Die im Gamedesign
festgelegte Zellzahl eines Gebäudes bestimmt die Bounding-Box des Meshes –
nicht ein vorab in Blender fixiertes Modellmaß, das anschließend auf eine
Zellzahl zurückgerechnet wird.

## 2. Dateinamenskonvention

| Asset-Typ | Muster | Beispiel |
|---|---|---|
| Mesh (Gebäude) | `SM_BLDG_<Faction>_<Role>.fbx` | `SM_BLDG_Alliance_HQ.fbx` |
| Mesh (Einheit) | `SM_UNIT_<Faction>_<Role>.fbx` | `SM_UNIT_Legion_LightTank.fbx` |
| Textur BaseColor | `T_BLDG_<Faction>_<Role>_BC.png` bzw. `T_UNIT_<Faction>_<Role>_BC.png` | `T_UNIT_Alliance_Builder_BC.png` |
| Textur Normal | `..._N.png` | `T_BLDG_Legion_Barracks_N.png` |
| Textur Mask (Metallic/AO/Team/Smoothness, §5) | `..._MSK.png` | `T_UNIT_Alliance_LightTank_MSK.png` |
| Material | `M_BLDG_<Faction>_<Role>.mat` bzw. `M_UNIT_<Faction>_<Role>.mat` | `M_BLDG_Alliance_Refinery.mat` |
| Prefab | `PF_BLDG_<Faction>_<Role>.prefab` bzw. `PF_UNIT_<Faction>_<Role>.prefab` | `PF_UNIT_Legion_BattleTank.prefab` |

**LOD-Meshes liegen innerhalb derselben FBX-Datei**, nicht als separate
Dateien. Sie folgen der Unity-`LODGroup`-Automatik-Konvention über
Submesh-/Objektnamen mit Suffix `_LOD0` / `_LOD1` / `_LOD2`, z. B.
`SM_UNIT_Alliance_LightTank_LOD0`, `..._LOD1`, `..._LOD2` als drei Objekte
im selben FBX-Export. Der Root-Dateiname trägt kein LOD-Suffix.

**Verhältnis zur Daten-Ebene:** [../tech/NamingConvention.md](../tech/NamingConvention.md)
§4 definiert bereits die Präfixe `UNIT_` und `BLDG_` für
ScriptableObject-Dateien (`UNIT_Allianz_Rifleman.asset`,
`BLDG_Legion_WarFactory.asset`). Diese Konvention ist eine andere Ebene: die
`SM_`/`T_`/`M_`/`PF_`-Präfixe dieses Dokuments bezeichnen Art-Assets (Mesh,
Textur, Material, Prefab) in `Assets/_Project/Art/`, während `UNIT_`/`BLDG_`
Daten-Assets (ScriptableObjects) in `Assets/_Project/Data/` bezeichnen. Beide
Ebenen referenzieren dieselbe `<Faction>/<Role>`-Kombination, verwenden aber
unterschiedliche Fraktions-Token: die Art-Ebene nutzt die englischen
PascalCase-Token aus §1 (`Alliance`, `Legion`), die Daten-Ebene die
deutschen GDD-Token aus `NamingConvention.md` §4 (`Allianz`, `Legion`). Ein
Prefab referenziert typischerweise genau ein `UnitDefinitionSO`/`BuildingDefinitionSO`,
das über den `<Role>`-Teil des Dateinamens identifizierbar ist; eine
programmatische Kopplung der beiden Namensräume ist nicht Gegenstand dieses
Dokuments.

## 3. LOD-Konvention

Drei LOD-Stufen sind für jedes renderbare Einheiten- und Gebäude-Asset
Pflicht – kein LOD0-only-Asset (Quelle: [../tech/AssetBudget.md](../tech/AssetBudget.md) §3).

**Bildschirmraum-Schwellen** (aus `AssetBudget.md` §3, unverändert übernommen):

| LOD-Stufe | Bildschirmhöhen-Anteil | Schatten |
|---|---|---|
| LOD0 | > 8 % | wirft Schatten |
| LOD1 | 2–8 % | wirft Schatten |
| LOD2 | < 2 % | wirft **keinen** Schatten |

**Tri-Budgets pro Asset-Klasse** (Quelle: `AssetBudget.md` §1, Tabelle
„Polycount-Budgets"; hier unverändert referenziert, keine eigenen Werte):

| Asset-Klasse | LOD0 | LOD1 | LOD2 |
|---|---|---|---|
| Infanterie | ≤ 4.000 Tris | ≤ 1.500 | ≤ 400 |
| Fahrzeug leicht/mittel | ≤ 8.000 | ≤ 3.000 | ≤ 800 |
| Fahrzeug schwer / Elite | ≤ 15.000 | ≤ 6.000 | ≤ 1.500 |
| Gebäude Standard | ≤ 20.000 | ≤ 8.000 | ≤ 2.000 |
| Gebäude Superwaffe | ≤ 35.000 | ≤ 14.000 | ≤ 3.500 |
| Mauer-/Verteidigungsmodul | ≤ 1.500/Segment | ≤ 600 | ≤ 200 |
| Aetherium-Kristall | ≤ 1.000 | ≤ 400 | ≤ 150 |
| Vegetation/Zerstörbares | ≤ 800/Instanz | ≤ 300 | ≤ 100 |

Für MS-1 relevant sind primär Gebäude Standard und die Fahrzeug-/
Infanterie-Klassen entsprechend der Rollenliste in §1; Superwaffe, Elite,
Lufteinheit sind laut `AssetBudget.md` MS-1-Override außerhalb des
produktiven Abnahmeszenarios.

**LODGroup-Setup im Prefab:** Jedes `PF_BLDG_...`/`PF_UNIT_...`-Prefab
erhält eine `LODGroup`-Komponente mit drei Einträgen (LOD0/LOD1/LOD2), deren
`Renderer`-Referenzen auf die entsprechenden `_LOD0`/`_LOD1`/`_LOD2`-Objekte
aus der importierten FBX zeigen. Die prozentualen Screen-Relative-Height-Werte
der `LODGroup` folgen den Schwellen oben (8 % / 2 %). Für Einheiten mit
Instancing-Batching (siehe [../tech/Rendering.md](../tech/Rendering.md)
§Draw-Call-Strategie) übernimmt der Batcher die Distanzklasse pro Instanz
statt der Standard-`LODGroup`-Auswertung; das Prefab behält die `LODGroup`
dennoch als Autor-Referenz und für nicht-instanzierte Kontexte (z. B.
Preview, Bau-Menü-Icon-Rendering).

**LOD-Erzeugungsempfehlung:** LOD0 ist das Autoring-Mesh. LOD1 wird manuell
retopologisiert oder per Decimate-Modifier aus LOD0 erzeugt und danach von
Hand nachbearbeitet (kritische Silhouettenkanten, Waffen/Anbauten,
Team-Mask-UV-Inseln dürfen nicht kollabieren). LOD2 darf stärker vereinfacht
sein (Decimate ohne manuelle Nacharbeit ist zulässig), muss aber die
Grundform und Teamfarben-Lesbarkeit aus großer Distanz erhalten.

**Silhouetten-Erhaltungsregel:** Die Außensilhouette (Umriss aus
Standard-Kamerawinkel, Pitch 50–60° gemäß
[../vision/CoreGameplay.md](../vision/CoreGameplay.md)) muss zwischen LOD0
und LOD1 optisch ununterscheidbar bleiben; Detailverlust ist nur bei
Innenkanten, kleinen Anbauten unter ca. 5 % der Objekthöhe und
Oberflächendetails zulässig. Ab LOD1 → LOD2 darf die Silhouette vereinfacht
werden, solange Fraktions- und Rollen-Erkennbarkeit (Gebäude vs. Einheit,
Fahrzeugtyp) erhalten bleibt (Lesbarkeits-Anforderung aus
[../tech/Rendering.md](../tech/Rendering.md) §Art-Direction-Anbindung, D-019).

## 4. Import-Settings

Alle Werte gelten als Standard-Import-Preset; Abweichungen pro Asset sind
möglich, aber im PR zu begründen.

### 4.1 Model-Import

| Einstellung | Wert | Motivation |
|---|---|---|
| Scale Factor | `1.0` | Determinismus – Übereinstimmung mit dem Blender-Export-Standard aus §1 |
| Convert Units | an | Determinismus – verhindert stille Skalierungsabweichungen zwischen Blender- und Unity-Metersystem |
| Read/Write Enabled | **aus** | Speicher – hält keine CPU-Kopie des Mesh im Speicher; MS-1-Content benötigt keinen Laufzeit-Mesh-Zugriff |
| Mesh Compression | **Off** | Determinismus – komprimierte Meshes verändern Vertex-Positionen leicht; deterministische Bounds/Kollisions-Vorschau (siehe [../vision/CoreGameplay.md](../vision/CoreGameplay.md) Bauplatzierung) haben Vorrang vor Speicherersparnis |
| Optimize Mesh | an | Rendering – Vertex-Cache-optimierte Reihenfolge senkt GPU-Kosten ohne sichtbare Nachteile |
| Generate Colliders | aus | Speicher/Rendering – Kollisionsgeometrie wird projektseitig separat definiert, nicht aus dem Art-Mesh generiert |
| Import Materials | **aus** | Determinismus – Materialien werden ausschließlich im Repo gepflegt (§5), kein FBX-generiertes Material darf referenziert werden |
| Normals | Import | Rendering – handautorisierte Normalen aus Blender sind für harte Kanten (Gebäudekanten, Fahrzeugpanels) verbindlich |
| Tangents | Calculate Mikktspace | Rendering – Mikktspace ist konsistent mit dem in Blender gebackenen Normal-Map-Workflow, verhindert Normal-Map-Artefakte |
| Import Animation | nur bei animierten Assets | Speicher – vermeidet leere Animations-Clips für rig-lose Fahrzeuge/Gebäude (Fahrzeuge nutzen Code-Animation, siehe `AssetBudget.md` §1) |
| Weld Vertices | an | Determinismus/Speicher – vermeidet doppelte Vertices an Nahtstellen aus dem Export |

### 4.2 Textur-Import

| Einstellung | Wert | Motivation |
|---|---|---|
| BaseColor sRGB | **an** | Rendering – BaseColor ist ein Farbwert, muss im sRGB-Farbraum interpretiert werden |
| Normal Map Typ | `Normal Map` | Rendering – aktiviert korrekte Tangentenraum-Dekodierung statt naiver Farbinterpretation |
| Mask (`_MSK`) sRGB | **aus** (linear) | Rendering – die Maske kodiert lineare Skalarwerte (Metallic, AO, TeamMask, Smoothness, siehe §5), keine Farbe |
| Mipmaps | Pflicht | Rendering/Speicher – Vorgabe aus [../tech/AssetBudget.md](../tech/AssetBudget.md) §2 (Speicherfaktor 1,33 bereits in den Memory-Budgets eingerechnet) |
| Compression | BC7 (Desktop) | Speicher/Rendering – konsistent mit `AssetBudget.md` §2/§6, native Apple-Silicon-Unterstützung laut `AssetBudget.md` §Offene Punkte |
| Alpha-Kanal `_MSK` | erhalten, kein Alpha-Discard | Rendering – alle vier Maskenkanäle (§5) sind Nutzdaten, keiner darf beim Import verworfen werden |
| Max Size Einheiten | `1024` | Speicher – konsistent mit `AssetBudget.md` §2 (1× 1024²-Atlas pro Einheitentyp) |
| Max Size Gebäude | `2048` | Speicher – konsistent mit `AssetBudget.md` §2 (1× 2048²-Atlas pro Gebäudetyp) |

## 5. Material-Standard

- **Basis:** URP Lit als Fallback-Material für jedes Asset, das (noch) ohne
  den projekteigenen `NovaUnit`-Shader gerendert wird (z. B. Vorschau,
  Asset-Store-Zukauf vor Konvertierung). **Ziel-Shader** ist `NovaUnit`
  (Basis-Farbe, Team-Maske, Damage-Blend, Ghost-Tint, Stealth-Dither, Quelle:
  [../tech/Rendering.md](../tech/Rendering.md) §Art-Direction-Anbindung).
  Dieses Dokument beschreibt ausschließlich die Material-/Asset-Anforderungen
  an den Shader-Vertrag (Kanalbelegung, Textur-Slots); Shader-Implementierung
  oder -System sind nicht Gegenstand dieses Dokuments.
- **Materialbudget:** maximal 2 Materialien pro `MeshRenderer`, 1
  Textur-Set (BaseColor/Normal/Mask) pro Asset (Quelle:
  [../tech/AssetBudget.md](../tech/AssetBudget.md) §2 Regel „Atlanten-Pflicht"
  und §6 „Materialaufbau").
- **Renderpfad-Konformität:** SRP Batcher **an**, GPU Resident Drawer **an**
  (konsistent mit [../tech/Rendering.md](../tech/Rendering.md)
  §URP-Setup), Dynamic Batching **aus** (wirkungslos bei SRP Batcher + GPU
  Resident Drawer, erzeugt nur CPU-Kosten).
- **`MaterialPropertyBlock` ist untersagt.** Team-, Damage- und Tarnzustand
  laufen über `GraphicsBuffer`/Custom-Data pro Instanz, wie in
  [../tech/Rendering.md](../tech/Rendering.md) §Draw-Call-Strategie für die
  synthetische 500-Objekt-Last festgelegt (`MaterialPropertyBlock` würde die
  SRP-Batcher-/GPU-Resident-Drawer-Kompatibilität pro Instanz brechen).
  Materialien in `Assets/_Project/Art/Shared/Materials/` dürfen daher keine
  Property-Block-Overrides referenzieren.

### 5.1 Team-Mask-Kanalbelegung (`_MSK`-Textur, verbindlich)

| Kanal | Inhalt |
|---|---|
| R | Metallic |
| G | Occlusion (AO) |
| B | TeamMask |
| A | Smoothness |

**Begründung:**

- **R (Metallic) und A (Smoothness)** folgen der URP-Lit-Metallic-Smoothness-Konvention.
  Dadurch rendert jedes Asset auch ohne den projekteigenen `NovaUnit`-Shader
  korrekt auf reinem URP Lit (nur ohne Teamfarbe) – konsistent mit der
  Fallback-Anforderung oben.
- **B (TeamMask):** Eine großflächige, weiche Maske verträgt
  BC7-Kompression im geteilten RGB-Block besser als ein hochfrequenter
  Smoothness-Kanal; TeamMask liegt deshalb bewusst nicht neben Metallic
  in einem eng korrelierten Kanalpaar, sondern separiert im B-Kanal.
- **TeamMask ist ein Graustufenwert 0…1** – der Blend-Faktor zwischen
  BaseColor und Spielerfarbe (`TeamColorProfile.PlayerColors`, siehe
  [../tech/Rendering.md](../tech/Rendering.md) §Draw-Call-Strategie), **kein
  Binärwert**. Weiche Übergänge (z. B. Ausbeulungen, Nietenreihen am
  Maskenrand) sind zulässig und gewünscht.

**Alternativenabwägung (Begründung der Wahl):**

- **Alternative A (gewählt, oben spezifiziert):** R = Metallic, G =
  Occlusion, B = TeamMask, A = Smoothness. Vorteil: URP-Lit-Fallback
  funktioniert ohne Custom-Shader; TeamMask liegt in einem
  kompressions-günstigen, großflächigen Kanal. Nachteil: TeamMask und AO
  teilen sich keinen thematisch verwandten Kanalblock, was bei manueller
  Textur-Erstellung leicht verwechselt werden kann.
- **Alternative B (verworfen):** R = Metallic, G = Smoothness, B =
  Occlusion, A = TeamMask. Vorteil: Alpha-Kanal ist oft der am einfachsten
  separat zu exportierende Kanal in gängiger Textur-Software. Nachteil:
  TeamMask im Alpha-Kanal kollidiert potenziell mit Alpha-basierten
  Compositing-Workflows und verliert die URP-Lit-Fallback-Eigenschaft von
  Alternative A (Smoothness ist im Standard-URP-Lit-Workflow im A-Kanal
  erwartet, nicht TeamMask).
- **Alternative C (verworfen):** Separate 1-Kanal-Team-Textur (`_TEAM.png`,
  R8, linear) statt eines gemeinsamen `_MSK`-Kanals. Vorteil: Klarste
  Trennung, keine Kompressions-Kompromisse zwischen Kanälen. Nachteil:
  zusätzlicher Textur-Sample pro Pixel im Shader, zusätzlicher
  Textur-Slot pro Material (kollidiert mit dem 1-Textur-Set-Budget aus
  `AssetBudget.md` §2), zusätzliche Datei pro Asset in der
  Namenskonvention.

## Offene Punkte

- **DecisionLog-Eintrag:** Die Kanalbelegung in §5.1 ist für MS-1
  verbindlich; der formale DecisionLog-Eintrag (D-ID-Vergabe) wird von
  anderer Seite nachgetragen und ist nicht Teil dieses Dokuments.
- **`FolderStructure.md`-Nachtrag:** Der Art-Zweig (`Assets/_Project/Art/`,
  §1) ist hier vollständig spezifiziert, aber bewusst nicht in
  [../tech/FolderStructure.md](../tech/FolderStructure.md) eingetragen, weil
  dieses Dokument als G0-A/G0-B-Nachweisziel gilt und während laufender
  Gate-Arbeit nicht verändert wird. Der Nachtrag erfolgt nach dem
  G0-B-Nachweis.
- **Texel-Density:** Aktuell nicht projektweit definiert. **Weiterhin
  Vorschlag, keine geltende Vorgabe:** Bei Kamera-Nahzoom (Kamerahöhe 18 m,
  Pitch 50°, Quelle: [../vision/CoreGameplay.md](../vision/CoreGameplay.md)
  §Kamera) ergibt sich unter der vereinfachten Annahme eines vertikalen
  Sichtfelds von ca. 35° und einer Bildschirmhöhe von 1080 px ein
  überschlägiger Zielwert von **~75 Pixel pro Meter** (Slant-Distanz ≈ 18 m
  / sin(50°) ≈ 23,5 m; sichtbare Vertikalausdehnung ≈ 2 × 23,5 m ×
  tan(17,5°) ≈ 14,8 m; 1080 px / 14,8 m ≈ 73 px/m, aufgerundet ~75 px/m).
  Diese Rechnung ist eine grobe Näherung ohne verbindliche
  Kamera-FOV-/Auflösungsvorgabe aus einem TDD und muss vor Übernahme in ein
  verbindliches Dokument mit dem tatsächlichen Kamera-FOV und
  Referenzauflösung validiert werden. Der Wert bleibt bewusst Vorschlag,
  da er sich erst an einem realen Asset validieren lässt.
- **Elite-/Superwaffen- und Post-MVP-Rollen** (z. B. Lufteinheiten,
  Evolvierte) sind nicht Teil der Rollenliste in §1, weil sie außerhalb des
  MS-1-Scopes von `quality/content/mvp-v1.json` liegen; Ordner- und
  Namenskonvention gelten identisch, sobald diese Rollen aktiviert werden.
- **Asset-Store-Zukäufe:** Wie zugekaufte Assets (die selten exakt diese
  Ordner-/Namenskonvention mitbringen) in die Struktur migriert werden
  (Umbenennen vs. Re-Export vs. Wrapper-Prefab), ist hier nicht geregelt und
  sollte mit der Kauf-Prüfung aus `AssetBudget.md` §6 verzahnt werden.

## Nächste Schritte

1. Formalen DecisionLog-Eintrag zur §5.1-Kanalbelegung in
   [../production/DecisionLog.md](../production/DecisionLog.md) nachtragen
   (D-ID-Vergabe erfolgt von anderer Seite, nicht Teil dieses Dokuments).
2. Art-Zweig (§1) nach dem G0-B-Nachweis in
   [../tech/FolderStructure.md](../tech/FolderStructure.md) nachtragen.
3. Texel-Density-Vorschlag mit dem tatsächlichen Kamera-FOV/Referenzauflösung
   aus einem Kamera-/Rendering-TDD validieren und bei Bestätigung als
   verbindlicher Wert in eine Folgeversion übernehmen.
4. Erstes Signature-Asset (siehe `AssetBudget.md` §Nächste Schritte) gegen
   diesen Standard bauen und als Referenz für Ordner-, Namens- und
   Import-Settings-Konformität nutzen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-25 | Erstfassung: Ordnerstruktur, Dateinamenskonvention, LOD-Konvention, Import-Settings, Material-Standard und Team-Mask-Kanalbelegung spezifiziert | Technical Art |
| 0.2.0 | 2026-07-25 | Team-Mask-Kanalbelegung (§5.1) von Vorschlag auf für MS-1 verbindlich gehoben, Alternativenabwägung als Begründung in den Fachabschnitt übernommen; Grid-Zellgröße 3,0 m als art-seitige Arbeitsannahme samt Footprint-Maßtabelle ergänzt (§1); Texel-Density bleibt Vorschlag; Hinweis auf ausstehenden `FolderStructure.md`-Nachtrag nach G0-B ergänzt | Technical Art |
