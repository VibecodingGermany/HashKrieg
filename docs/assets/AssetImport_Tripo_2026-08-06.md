# Art-Import: Tripo-Erstsatz (34 Assets)

**Version:** 0.1.1 | **Status:** Entwurf – Graybox-/Demo-Spur, kein Gate-Nachweis | **Verantwortungsbereich:** Technical Art | **Datum:** 2026-08-06

## Zweck

Protokolliert den ersten vollständigen Art-Import von *Project Nova*: 34
KI-generierte 3D-Modelle (Tripo) wurden den 34 MS-1-Rollen zugeordnet, nach
[ArtAssetStandard.md](ArtAssetStandard.md) aufbereitet und unter
`Assets/_Project/Art/` abgelegt. Es benennt außerdem, was an diesem Satz
**noch nicht** standardkonform ist, damit niemand ihn für fertig hält.

Es ist **kein Gate-Nachweis**. Der Gate-Status steht ausschließlich in
[../production/MVPRecoveryPlan.md](../production/MVPRecoveryPlan.md).

## Abhängigkeiten

- [ArtAssetStandard.md](ArtAssetStandard.md) – Ordner-, Namens-, LOD- und Material-Standard
- [../tech/AssetBudget.md](../tech/AssetBudget.md) – Tri- und Texturbudgets
- [Provenance.md](Provenance.md) – Provenienzpflicht vor Repo-Aufnahme
- [../production/DemoRunbook.md](../production/DemoRunbook.md) – Demo-Ablauf, Asset-Einsatz §6

## 1. Was abgelegt wurde

34 von 34 MS-1-Rollen sind besetzt, je 17 pro Fraktion:

| | Gebäude | Einheiten |
|---|---|---|
| Alliance | 9/9 | 8/8 |
| Legion | 9/9 | 8/8 |

Je Rolle liegen in `Art/<Buildings\|Units>/<Faction>/<Role>/`:

- `SM_<BLDG\|UNIT>_<Faction>_<Role>.fbx` – LOD0/LOD1/LOD2 in einer Datei
- `T_<BLDG\|UNIT>_<Faction>_<Role>_BC.png` – BaseColor (Einheiten 1024², Gebäude 2048²)
- `PROVENANCE.json` – Provenienz-Datensatz, **unvollständig**, siehe §4

## 2. Aufbereitung (Blender 5.2.0 LTS, headless)

Jedes Modell durchlief denselben deterministischen Batch:

| Schritt | Regel |
|---|---|
| Splitter entfernt | lose Inseln ≤ 8 Tris, die > 15 % der Modelldiagonale messen und in der zweiten Achse ≤ 15 % der längsten sind (Nadelform) |
| Skalierung | Gebäude auf Footprint (2×2/3×3/4×4 Zellen × 3,0 m); Einheiten auf größte Kante |
| Origin | Bodenmittelpunkt, `Y = 0`, X/Z zentriert |
| Drehung | Vorderseite auf `+Z` (Tripo liefert durchgehend `+X`; Legion-Builder wich mit 180° ab) |
| LOD-Kette | Decimate auf die Budgets aus `AssetBudget.md` §1 |
| Export | FBX, `+Y` up / `−Z` forward, Scale 1.0, Apply Transform |

Kontrolliert: alle 34 stehen auf `Y = 0`, sind in X/Z zentriert, kein
Tri-Budget verletzt, UVs haben die Dezimierung unbeschädigt überstanden
(texturierter Kontrollrender).

**Maßannahmen.** Die Footprint-Zuordnung folgt `Buildings.md` §215
(2×2 Lager/Radar/Plattform, 4×4 HQ/Fabriken, Rest 3×3) — dort ausdrücklich
als Annahme markiert. Die Einheitenmaße (Infanterie 1,8 m bis Artillerie
8,0 m) sind eine Arbeitsannahme dieses Imports und stehen in
`docs/assets/` sonst nirgends. Beides ist bei einer Grid-Finalisierung
nachzuziehen.

## 3. Zuordnung Modell → Rolle

Die Download-Dateinamen waren generische Generator-Prompts
(`rusty+tank+3d+model.glb`) ohne Rollenbezug. Die Zuordnung erfolgte über
Bildvergleich gegen die 34 Concept-Art-Blätter, mehrfach und unabhängig
geprüft. Der Ursprungsdateiname jedes Assets steht in `PROVENANCE.json`
unter `_sourceFileName`, die vollständige Tabelle in
`Hashkrieg_Assets/3d/unity_ready/convert_report.json`.

**Nicht eindeutig belegt** und beim nächsten Asset-Austausch zuerst zu
prüfen:

| Rolle | Warum unsicher |
|---|---|
| `Legion` ResearchLab / Power | Beide Concepts sind selbst kettengeführte Maschinen mit Schlot; die unterscheidende Lüfterbank fehlt beiden Modellen |
| `Legion` LightTank / BattleTank | Concepts trennen sich fast nur über Kühlung (Lüfterreihe vs. Schlot), am Modell nicht vorhanden |
| `Alliance` LightTank / BattleTank | Das Concept-Unterscheidungsmerkmal (BattleTank = Doppelrohr) existiert an keinem der beiden Modelle |
| `Alliance` VehicleFactory | Modell ist ein Container ohne modelliertes Tor; Zuordnung per Ausschluss |

## 4. Was noch fehlt

1. **Provenienzpflicht nicht erfüllt.** Die 34 `PROVENANCE.json` und
   [provenance-ledger.json](provenance-ledger.json) enthalten die belegbaren
   Felder (SHA-256 der Quelldatei, Abrufdatum, Provider, Prompt,
   Modifikationen). Leer bleiben Lizenz, Anbieter-AGB, Rechteübertragung,
   kommerzielle Nutzung und die Vier-Augen-Verifikation — je Datensatz unter
   `_TODO` einzeln benannt. Bis diese Felder gefüllt sind, ist
   [Provenance.md](Provenance.md) **nicht** erfüllt.
2. **Kein `_MSK`.** Tripo liefert ausschließlich BaseColor. Metallic, AO,
   **TeamMask** und Smoothness aus `ArtAssetStandard.md` §5.1 existieren
   nicht. Ohne TeamMask haben Einheiten keine Spielerfarbe. Die Materialien
   stehen deshalb auf Metallic 0 / Smoothness 0,25 statt auf dem
   URP-Standard.
3. **Kein Emissive bei Legion.** Jedes Legion-Concept lebt von orangem
   Glühen (Ofenglut, Leuchtschlitze); kein einziges der 17 Legion-Modelle
   bringt es mit. Die Alliance-Modelle tragen ihre Teal-Leuchtlinien in der
   BaseColor. Die Fraktionslesbarkeit auf Distanz ist dadurch asymmetrisch.
4. **Detailverlust gegenüber dem Concept.** Wiederkehrend: der Greifarm des
   Alliance-Harvesters ist nur ein Stummel, der Legion-AntiArmorInfantry hat
   ein statt zwei Werferrohren, den Alliance-Panzern fehlt das
   unterscheidende Doppelrohr.
5. **Zwei sehr hohe Gebäude.** Alliance HQ 21,1 m und Radar 20,0 m ergeben
   sich aus der Footprint-Skalierung schlanker Türme. Sichtprüfung in der
   Spielkamera steht aus; ggf. Höhe deckeln.
6. **Restsplitter.** An beiden `DefensePlatform`-Modellen schwebt ein
   kleines abgelöstes Bruchstück neben dem Sockel. Der Nadelfilter greift
   dort nicht (zu kompakt) — Handarbeit oder Austausch.

## Offene Punkte

Die offenen Punkte dieses Imports stehen gesammelt in §4 („Was noch fehlt"):
unvollständige Provenienz (§4.1), fehlende `_MSK`-Masken samt Teamfarben
(§4.2), fehlendes Legion-Emissive (§4.3), Detailverluste gegenüber den
Concepts (§4.4), zwei sehr hohe Gebäude (§4.5) und Restsplitter an den
DefensePlatform-Modellen (§4.6). Dazu kommen die in §3 tabellierten unsicheren
Modell-zu-Rolle-Zuordnungen, die beim nächsten Asset-Austausch zuerst zu
prüfen sind.

## 5. Nächste Schritte

1. In Unity `6000.5.4f1` **Tools → Project Nova → Build Art Prefabs From FBX**
   ausführen. Das erzeugt je Asset `M_*.mat` (URP Lit mit BaseColor) und
   `PF_*.prefab` mit `LODGroup` an den Schwellen 8 % / 2 % und synchronisiert
   anschließend die `AssetMappingRegistry`. Ohne diesen Schritt registriert
   `ArtAssetAutoSync` nichts — es erfasst Prefabs, nicht FBX.
2. Erste Runde im Editor fahren und Maßstab, Blickrichtung und Lesbarkeit in
   der Spielkamera prüfen (`DemoRunbook.md`).
3. Provenienzfelder aus §4.1 nachtragen und verifizieren.
4. `_MSK` für mindestens die vier Vertical-Slice-Assets autorisieren, damit
   Teamfarben greifen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-08-06 | Erstfassung: Import von 34 Tripo-Assets, Aufbereitungsprotokoll, offene Punkte | Technical Art |
| 0.1.1 | 2026-08-06 | Pflichtabschnitt „Offene Punkte" als Sammelverweis auf §4 ergänzt (Dokumentationsstandard, docs-check) | Technical Writer |
