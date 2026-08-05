# Art-Asset-Paket (ausserhalb des Repositories)

**Version:** 1.0.0 | **Status:** verbindlich | **Verantwortungsbereich:** Technical Art / Producer | **Sprint:** 7

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

Der MS-1-Art-Stand umfasst rund **105 MB** Binärdaten (92 MB PNG-Texturen,
13,7 MB FBX-Meshes). Das Repository ist derzeit 77 MB gross; der Drop würde es
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
| Datei | `ProjectNova_Art_MS1_2026-08-06.zip` |
| Grösse | rund 113 MB |
| Inhalt | 272 Dateien: je 34× `.fbx`, `.png`, `.mat`, `.prefab` plus 136 `.meta` |
| SHA-256 | `02afd4f4fd245ae6bd70d41b170b675e09948fe12ae5b529c9bd659abbf7fd68` |
| Ablage | [Geteilter Google-Drive-Ordner](https://drive.google.com/drive/folders/1HuOLk1JuykvGxDo0Ey2FNinVXJAUtwS3?usp=sharing) |

Im Drive-Ordner liegt neben dem Zip eine `README.txt` mit demselben
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
3. Paket neu packen, hochladen, und in §3 Dateiname, Grösse und SHA-256
   fortschreiben.

## Offene Punkte

- **Der Drive-Ordner ist per Link freigegeben und dieses Repository ist
  öffentlich.** Damit ist das Paket faktisch für jeden abrufbar, der die
  Repo-Seite liest — das ist eine Verbreitung an Dritte, keine interne
  Weitergabe. Solange der nächste Punkt offen ist, sollte der Ordner entweder
  auf konkrete Personen eingeschränkt oder der Link aus dieser Datei entfernt
  und direkt ausgetauscht werden.
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

1. Geteilten Ordner anlegen, Paket hochladen, Link in §3 eintragen.
2. Lizenzfelder der Provenienzdatensätze bei Tripo nachziehen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-08-06 | Erstfassung: Art-Assets als externes Paket statt im Repo; Ausschlussregeln, Paketinhalt, Installations- und Erweiterungsablauf | Producer / Technical Art |
