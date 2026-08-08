# Art-Asset-Paket (ausserhalb des Repositories)

**Version:** 1.1.0 | **Status:** verbindlich | **Verantwortungsbereich:** Technical Art / Producer | **Sprint:** 7

## Zweck

Die produzierten 3D-Assets liegen **nicht im Git-Repository**, sondern als
Paket in einem geteilten Ordner. Dieses Dokument sagt, warum, was im Paket ist
und wie man es installiert.

## Abhängigkeiten

- [ArtAssetStandard.md](ArtAssetStandard.md) – Ordner-, Namens- und Importregeln
- [Provenance.md](Provenance.md) – Provenienzpflicht je Asset
- [../../.gitignore](../../.gitignore) – die ausschliessenden Regeln
- [../production/DemoRunbook.md](../production/DemoRunbook.md) – Drop-in-Ablauf im Spiel

## 1. Warum ausserhalb des Repos

Der MS-1-Art-Stand umfasst rund **110 MB** Binärdaten (94 MB PNG-Texturen,
15 MB FBX-Meshes). Das Repository ist derzeit 77 MB gross; der Drop würde es
mehr als verdoppeln — und zwar **dauerhaft**, weil Git-Historie Binärdaten nie
wieder vergisst. Sie später herauszunehmen bräuchte einen History-Rewrite auf
`main`, den [AGENTS.md](../../AGENTS.md) §2 Regel 2 ausdrücklich verbietet.

Git LFS wäre die Alternative, kostet auf einem öffentlichen Repository aber
Bandbreitenkontingent pro Clone und zwingt jedem Mitwirkenden eine
`git-lfs`-Installation auf. Für zwei Entwickler ist ein geteilter Ordner
billiger und direkter.

## 2. Was ausgeschlossen ist — und was nicht

Ausgeschlossen wird der **vollständige** Art-Inhalt, nicht nur die Binärdaten:

| Im Paket | Im Repository |
|---|---|
| `SM_*.fbx` + `.meta` | `PROVENANCE.json` + `.meta` (Herkunfts-/Lizenznachweis) |
| `T_*.png` + `.meta` | Ordnerstruktur und `.gitkeep` |
| `M_*.mat` + `.meta` | die Drop-in-Pipeline (`ArtAssetNaming`, `ArtAssetAutoSync`) |
| `PF_*.prefab` + `.meta` | `AssetMappingRegistry.asset` (leer, füllt sich beim Import) |

**Warum auch die Prefabs raus müssen:** Bliebe ein `PF_*.prefab` im Repo, während
sein Mesh fehlt, hätte ein frischer Clone *unsichtbare* Einheiten — ein Prefab
ohne Mesh rendert nichts. Ohne Prefab fällt `UnitViewManager` sauber auf die
Graybox-Primitive zurück. Ein Clone ohne Paket ist damit **immer ein
spielbares Graybox-Spiel**, kein kaputtes.

**Warum die Provenienz bleibt:** `PROVENANCE.json` ist der Lizenz- und
Herkunftsnachweis nach [Provenance.md](Provenance.md). Er gehört ins Repo, auch
wenn das Asset selbst es nicht tut — inklusive der offenen Punkte darin.

## 3. Das Paket

| | |
|---|---|
| Datei | `Hashkrieg_Art_MS1_2026-08-08_0904.zip` |
| Grösse | rund 109 MB |
| Inhalt | 276 Dateien: 35× `.fbx`, 35× `.png`, 34× `.mat`, 34× `.prefab` plus 138 `.meta` |
| SHA-256 | `214605f00ce3b356a6178c1308af06125d5e87f7c2d181974896a2ff69f1635b` |
| Repo-Stand | `eef73ae` |
| Ablage | geteilter Ordner, **Zugang auf Anfrage** |

**Der Dateiname trägt Datum und Uhrzeit**, weil an einem Tag mehrfach
nachgeliefert werden kann: zwei Pakete vom selben Tag wären sonst nicht
unterscheidbar, und im geteilten Ordner überschriebe eines das andere still.
Liegen dort mehrere Zips, gilt das mit der spätesten Uhrzeit.

**Der Repo-Stand gehört zur Paketkennung.** Die `.meta`-GUIDs im Paket müssen
zu den Prefab- und Registry-Referenzen im Repository passen; ohne den Commit
ist ein Paket nur halb bestimmt. Das Build-Skript gibt ihn mit aus und
vermerkt, ob der Arbeitsbaum beim Packen sauber war.

Die Zahlen sind nicht mehr symmetrisch: Das Aetherium aus dem Nachschub-Import
([AssetImport_2026-08-06_Nachschub.md](AssetImport_2026-08-06_Nachschub.md))
bringt Mesh und Textur mit, aber weder Material noch Prefab — es wird derzeit
von nichts referenziert. Daher 35 Meshes gegen 34 Materialien.

**Der Paketname trägt jetzt die Marke** (`Hashkrieg_`, vorher `ProjectNova_`).
Das folgt E-1/E-3 aus
[../production/hashkrieg/00_Entscheidungen.md](../production/hashkrieg/00_Entscheidungen.md):
umbenannt wird die Marke, die Code-Identität bleibt `Nova.*`. Ein Paket ist
Marke.

**Zugang:** Das Paket wird nicht öffentlich verlinkt. Wer am Projekt mitentwickelt,
bekommt den Ordner auf Anfrage freigeschaltet — kurze Mail an
**hey@dennis-westermann.de**, Betreff „Hashkrieg Art-Paket".

Der Link zum geteilten Ordner steht **bewusst nicht in diesem Dokument**: Das
Repository ist öffentlich, ein Link hier wäre die Veröffentlichung, die §
„Offene Punkte" gerade ausschliesst. Die Freigabe läuft pro Person über die
Ordnerfreigabe, nicht über einen Link im Repo.

Der Grund steht unter „Offene Punkte": Solange die Lizenzfelder der Assets
ungeklärt sind, ist eine Weitergabe an einen unbestimmten Personenkreis nicht
gedeckt. Freigabe an konkrete Personen ist es.

Im geteilten Ordner liegt neben dem Zip eine `README.txt` mit demselben
Installationsablauf, damit das Paket auch ohne Repository-Kontext verständlich
ist. Nach jeder Paketaktualisierung wandern **beide** mit: Zip und README.

Die `.meta`-Dateien sind Teil des Pakets und **müssen** es bleiben: Sie tragen
die Unity-GUIDs. Ohne sie vergibt jeder Import neue GUIDs, und Material-,
Prefab- und Registry-Referenzen brechen bei jedem Entwickler unterschiedlich.

## 4. Installieren

1. Paket herunterladen und im **Repository-Wurzelverzeichnis** entpacken. Die
   Ordnerstruktur im Zip ist bereits `Assets/_Project/Art/...` und legt sich
   passgenau über die im Repo vorhandene Struktur.
2. Unity öffnen. Der Import stempelt die Standard-Import-Settings
   ([ArtAssetStandard.md](ArtAssetStandard.md) §4) und `ArtAssetAutoSync`
   registriert jedes konventionskonforme `PF_*`-Prefab automatisch in
   `Assets/_Project/Data/Registries/AssetMappingRegistry.asset`.
3. Falls die Registrierung fehlt: `Tools/Project Nova/Sync Art Asset Registry`.
4. Play drücken. Registrierte Rollen erscheinen als Modell, alle übrigen
   bleiben Graybox-Primitiv (Mischbetrieb ist vorgesehen).

Die Registry ist im Repo bewusst **leer** eingecheckt. Sie ist eine
Maschinenausgabe des Imports — wer sie gefüllt committet, erzeugt bei allen
anderen tote Referenzen.

## 5. Neue Assets hinzufügen

Neue Assets kommen **ins Paket, nicht ins Repo**. Ablauf:

1. Asset nach [ArtAssetStandard.md](ArtAssetStandard.md) §1–§2 benennen und ablegen.
2. `PROVENANCE.json` daneben anlegen — die **wird** eingecheckt.
3. `tools/art/build_art_package.sh` laufen lassen. Das Skript legt das Zip
   unter `output/art-package/` ab und gibt die Kennzahlen aus.
4. Die ausgegebenen Werte in §3 fortschreiben, `README.txt` daneben auf den
   neuen Stand bringen, dann **beide** Dateien in den geteilten Ordner laden.

**Der Paketinhalt wird nicht von Hand gepflegt.** Das Skript leitet ihn aus
`.gitignore` ab: alles, was git im Art-Baum ausschliesst, gehört ins Paket —
und nur das. Damit können Repo-Ausschluss und Paketinhalt nicht auseinander
laufen; wer eine `.gitignore`-Regel ändert, ändert das Paket automatisch mit.
Vor dem Packen prüft das Skript, dass zu jedem Asset sein `.meta` vorliegt, und
bricht sonst ab — ein Paket ohne GUIDs wäre schlimmer als gar keines (§3).

## Offene Punkte

- **Lizenzlage der Tripo-Assets ist unvollständig.** In den PROVENANCE-Datensätzen
  sind `licenseId`, `redistributionAllowed`, `commercialUseGranted` und
  `outputOwnership` leer beziehungsweise `null`, und `sourceUrl` fehlt. Solange
  das offen ist, ist die Weitergabe des Pakets an Dritte ungeklärt — für den
  internen Austausch zwischen den Maintainern ist sie unkritisch, für eine
  Veröffentlichung nicht. Siehe die `_TODO`-Blöcke in den Datensätzen und
  [AssetImport_Tripo_2026-08-06.md](AssetImport_Tripo_2026-08-06.md).
- Ob das Paket langfristig bei Git LFS besser aufgehoben ist, entscheidet sich
  mit dem Wechsel auf Governance-Tier 3 (`GOVERNANCE.md`, kommt mit dem
  Governance-PR).

## Nächste Schritte

1. Paket und `README.txt` in den geteilten Ordner laden.
2. **Ordnerfreigabe auf benannte Personen stellen, kein Link-Sharing.** Solange
   die Lizenzfelder offen sind, ist genau die personengebundene Freigabe die
   gedeckte Variante — „jeder mit dem Link" ist es nicht.
3. Lizenzfelder der Provenienzdatensätze nachziehen: Tripo aus dem Erstimport,
   und den bislang unbenannten Generator der drei Nachschub-Modelle
   ([AssetImport_2026-08-06_Nachschub.md](AssetImport_2026-08-06_Nachschub.md) §5).

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-08-06 | Erstfassung: Art-Assets als externes Paket statt im Repo; Ausschlussregeln, Paketinhalt, Installations- und Erweiterungsablauf | Producer / Technical Art |
| 1.1.0 | 2026-08-08 | Paket auf den Nachschub-Stand gehoben (HQ und BattleTank ersetzt, Aetherium neu): neue Kennzahlen in §3, Paketname auf die Marke umgestellt; §5 auf das neue Build-Skript `tools/art/build_art_package.sh` umgestellt, das den Inhalt aus `.gitignore` ableitet; Freigabeweg und Link-Verzicht ausdrücklich festgehalten | Producer / Agent (Umsetzung) |
