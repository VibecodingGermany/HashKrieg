# Asset-Nachschub 2026-08-06 — HQ, BattleTank, Aetherium, zwei Kommandanten

**Version:** 0.1.0 | **Status:** Import ausgeführt, Provenienz unvollständig | **Verantwortungsbereich:** Technical Art / Producer | **Sprint:** 7

## Zweck

Protokoll des zweiten Asset-Imports. Er tauscht zwei der 34 Modelle aus dem
Erstimport gegen bessere Fassungen, bringt erstmals ein Aetherium-Mesh ins
Projekt und parkt zwei Kommandanten-Modelle, für die es im MS-1-Rollenmodell
noch keinen Platz gibt.

## Abhängigkeiten

- [ArtAssetStandard.md](ArtAssetStandard.md) – Ordner- und Namenskonvention
- [AssetImport_Tripo_2026-08-06.md](AssetImport_Tripo_2026-08-06.md) – Erstimport, dessen Aufbereitungsschritte hier erstmals als Skript vorliegen
- [Licenses.md](Licenses.md) – Anbieter-Whitelist, Default-Deny
- [../tech/AssetBudget.md](../tech/AssetBudget.md) – Dreiecks- und Texturbudgets

## 1. Was eingebaut wurde

| Rolle | Ziel | Quelle | LOD0/1/2 | Textur |
|---|---|---|---|---|
| Allianz HQ | `Art/Buildings/Alliance/HQ/` | `alliance_HQ_V2.glb` | 19.599 / 7.840 / 1.960 | 2048² |
| Allianz BattleTank | `Art/Units/Alliance/BattleTank/` | `alliance_unit_BattleTank.glb` | 7.839 / 2.940 / 783 | 1024² |
| Aetherium (neu) | `Art/Shared/Meshes/` + `Art/Shared/Textures/` | `teal crystal cluster 3d model.glb` | 991 / 392 / 147 | 1024² |

Alle Stufen liegen innerhalb der Budgets aus [AssetBudget.md](../tech/AssetBudget.md) §1.
Die ersetzten Fassungen liegen unter
`Hashkrieg_Assets/3d/unity_ready/_replaced_2026-08-06/` und sind nicht gelöscht.

**Die Quellmodelle sind rund 500-mal so hoch aufgelöst wie beim Erstimport**
(1,88 bis 1,98 Millionen Dreiecke gegenüber knapp 4.000). Damit ist die
LOD-Regel des Erstimports — feste Verhältnisse 40 % / 12 % der Quelle — nicht
mehr tragfähig: sie hätte LOD0 bei 1,88 Millionen Dreiecken belassen. Die
Kette wird jetzt gegen das **Budget** dezimiert und das Verhältnis gilt nur,
solange es darunter bleibt. Kontrollrender zeigen, dass HQ und BattleTank die
Reduktion ohne sichtbaren Verlust überstehen; beim Aetherium sind am Sockel
Dezimierungsartefakte erkennbar — 1.000 Dreiecke sind für dieses Modell die
harte Grenze.

## 2. Neu: das Aufbereitungsskript

Der Erstimport lief von Hand und hinterließ kein Skript, obwohl
[ArtAssetStandard.md](ArtAssetStandard.md) die Pipeline ausdrücklich
reproduzierbar haben will. Diese Lücke ist geschlossen:
**`tools/art/glb_to_unity_fbx.py`** schreibt die Tabelle aus
[AssetImport_Tripo_2026-08-06.md](AssetImport_Tripo_2026-08-06.md) §2 als
ausführbaren Blender-Batch aus — Splitterfilter, Skalierung, Origin,
Yaw-Korrektur, LOD-Kette, FBX-Export und Texturextraktion.

Zwei Abweichungen gegenüber dem Erstimport, beide notwendig:

1. **Budget statt Verhältnis** bei der LOD-Kette (siehe §1).
2. **BaseColor über den Materialgraphen** statt über die Dateigröße. Die neuen
   GLBs bringen drei Texturen mit; die größte zu nehmen ist ein Münzwurf, und
   eine fälschlich als Albedo verwendete Normal Map färbt das ganze Modell
   blauviolett. Das Skript verfolgt den Link auf den Base-Color-Eingang des
   Principled BSDF und meldet es, wenn es doch raten muss.

## 3. Neue Namenskonvention: `PROP`

[ArtAssetStandard.md](ArtAssetStandard.md) §2 kennt nur `BLDG` und `UNIT`. Das
Aetherium ist beides nicht — es ist ein fraktionsloses Weltobjekt, für das §1
den Ordner `Shared/` ausdrücklich vorsieht, ohne ein Namensmuster zu nennen.
Diese Lücke wird mit dem naheliegenden dritten Token geschlossen:

| Asset-Typ | Muster | Beispiel |
|---|---|---|
| Mesh (Weltobjekt) | `SM_PROP_<Name>.fbx` | `SM_PROP_Aetherium.fbx` |
| Textur BaseColor | `T_PROP_<Name>_BC.png` | `T_PROP_Aetherium_BC.png` |
| Material | `M_PROP_<Name>.mat` | `M_PROP_Aetherium.mat` |

`<Name>` ist PascalCase und fraktionslos. **Noch nicht nachgezogen:**
`ArtAssetNaming.TryParsePrefabName` kennt nur `UNIT` und `BLDG` — ein
`PF_PROP_*`-Prefab würde von `ArtAssetAutoSync` nicht registriert. Für das
Aetherium ist das derzeit folgenlos, weil es kein Prefab-Slot im
`AssetMappingRegistry` gibt: die Kristalle baut `GlutrinneBlockoutView` zur
Laufzeit aus Primitiven. **Wer das Mesh in den Blockout einbaut, muss diesen
Weg zuerst klären** — entweder Parser und Registry um `PROP` erweitern oder das
Mesh direkt im Blockout referenzieren.

## 4. Geparkt: zwei Kommandanten

Zwei Figurenmodelle sind da, für die MS-1 keine Rolle hat — das Rollenmodell
kennt neun Gebäude und acht Einheiten, keine Kommandanten. Sie liegen deshalb
**nicht** im Unity-Projekt, sondern unter
`Hashkrieg_Assets/3d/unsorted/commanders_2026-08-06/`:

| Datei | Ursprung | Bemerkung |
|---|---|---|
| `general_A_cyborg.glb` | `cyborg+action+figure+3d+model.glb` | 78 MB, unbearbeitet |
| `general_B_gunman.glb` | `john+wick+figure+3d+model (2).glb` | 63 MB, unbearbeitet — **Rechtelage prüfen, siehe unten** |

Vor einem Einbau ist zu klären:

1. **Was ein General im Spiel tut.** Es gibt einen `CommanderSystem`-Ansatz in
   der Simulation, aber keine Design-Entscheidung dazu. Ohne die ist unklar, ob
   ein General eine Einheit, ein Porträt oder beides ist — und davon hängt das
   Budget ab (Infanterie 4.000 Tris gegen ein Porträt ohne Budgetgrenze).
2. **Die Rechtelage von `general_B_gunman.glb`.** Der Ursprungsdateiname nennt
   eine Filmfigur, die von einem realen Schauspieler verkörpert wird. Ein
   erkennbares Abbild davon im Spiel berührt Persönlichkeits- und
   Werkrechte — unabhängig davon, dass das Modell KI-generiert ist. Das ist
   keine Rechtsberatung; es ist der Hinweis, dass hier eine bewusste
   Entscheidung nötig ist, bevor die Figur ein Gesicht der Fraktion wird.
   [Licenses.md](Licenses.md) §2 arbeitet mit Default-Deny; nach dieser Logik
   bleibt die Datei gesperrt, bis das geklärt ist.
3. **Die Namenskonvention.** `PROP` (§3) passt nicht — ein General ist kein
   Weltobjekt. Ein vierter Token oder ein eigener `Characters/`-Zweig wäre
   nötig; beides wird bewusst erst festgelegt, wenn Punkt 1 beantwortet ist.

Ebenfalls geparkt: `aetherium_variant_B_unused.glb` — die zweite, nicht
türkisfarbene Kristallvariante aus demselben Download. Aufgehoben für den Fall,
dass Felder optisch variieren sollen.

## 5. Was noch fehlt

1. **Provenienz unvollständig.** Die drei neuen `PROVENANCE.json` tragen Hash,
   Abrufdatum und Aufbereitungsschritte, aber **kein Anbieterfeld**: anders als
   beim Tripo-Import verrät weder der Dateiname noch die eingebettete Textur den
   Generator. Ohne Anbieter lassen sich Lizenz, AGB, kommerzielle Nutzung und
   Rechteübertragung nicht füllen. **Der Inhaber muss den Generator benennen** —
   danach sind die `_TODO`-Listen abarbeitbar.
2. **Kein `_MSK`.** Wie beim Erstimport fehlen Metallic, AO, TeamMask und
   Smoothness. Ohne TeamMask trägt der neue BattleTank keine Spielerfarbe.
3. **Unity-Reimport steht aus.** Die Dateien liegen an Ort und Stelle, die
   `.meta`-Dateien und damit die GUIDs sind unverändert — die bestehenden
   Prefabs zeigen weiter auf dieselben Pfade. Ob die Prefabs die neuen
   LOD-Objektnamen sauber übernehmen, zeigt erst der Editor-Import.

## Offene Punkte

- Anbieter der drei neuen Modelle benennen (blockiert die gesamte Lizenzkette).
- Rechtelage `general_B_gunman.glb` entscheiden.
- `PROP` in `ArtAssetNaming` und `AssetMappingRegistry` nachziehen, sobald das
  Aetherium-Mesh tatsächlich gerendert werden soll.
- Kommandanten-Design klären, dann Ordner- und Namenskonvention festlegen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-08-06 | Zweiter Asset-Import protokolliert: HQ und BattleTank ersetzt, Aetherium neu, zwei Kommandanten geparkt; Aufbereitungsskript `tools/art/glb_to_unity_fbx.py` eingeführt; `PROP`-Namenstoken ergänzt | Technical Art |
