# Verteilbare Builds

**Dokumentversion:** 2.1.0 | **Stand:** 2026-10-17 | **Governance-Tier:** 2

Reproduzierbare Testpakete für macOS und Linux. macOS wird signiert und
notarisiert als DMG verteilt; Linux wird als `tar.gz` mit einem von außen
prüfbaren Commit-Stempel geliefert.

## Kurzfassung

```bash
tools/packaging/build-mac.sh
tools/packaging/build-linux.sh
```

Ergebnisse:

- macOS: `Builds/dist/ProjectNova-<commit>.dmg`
- Linux x64: `Builds/dist/ProjectNova-linux-x64-<commit>.tar.gz`

## Optionen

| Flag | macOS | Linux |
|---|---|---|
| *(keine)* | bauen → signieren → notarisieren → DMG | bauen → stempeln → `tar.gz` |
| `--fast` | nur bauen, unsigniert, kein DMG | – |
| `--skip-build` | vorhandenen Build neu verpacken | vorhandenen Build neu stempeln und verpacken |
| `--open` | Ergebnis danach öffnen | – |
| `--allow-dirty` | Bauen trotz unsauberem Baum, Stempel behält `-dirty` | wie macOS |
| `--check-only` | nur den Quellstand prüfen, vor Unity aussteigen | wie macOS |

Für die eigene Iteration ist `--fast` der richtige Weg: Signieren und
Notarisieren kosten Minuten und bringen auf dem eigenen Rechner nichts. Ein
lokal gebauter Build hat kein Quarantäne-Attribut und startet ohnehin. `--fast`
ist als einziger Weg von der Sauberkeitsprüfung ausgenommen (siehe unten) und
trägt bei unsauberem Baum den Stempel `-dirty`.

## Warum der Commit-Hash im Dateinamen steht

Der Relay-Server sperrt Matches zwischen ungleichen Builds ab (Sprint 12, A4 —
Fingerprint-Sperre). Beide Spieler brauchen also **genau denselben Commit**.
Der Hash steht deshalb im Paketnamen und im Player:

- im DMG-Dateinamen — `ProjectNova-1526d7a.dmg`
- in `LIESMICH.txt` im DMG
- im `Info.plist` der App unter `NovaBuildCommit`
- unter Linux in `ProjectNova_Data/NovaBuildCommit.txt`

Beide Player-Stempel lassen sich beim Empfänger ohne Rückfrage prüfen:

```bash
defaults read /Applications/ProjectNova.app/Contents/Info.plist NovaBuildCommit
cat ProjectNova_Data/NovaBuildCommit.txt
```

## Nur aus sauberem Baum (#132, D-124)

Verschickt wird nur, was aus einem sauberen Commit fällt. Das erzwingen die
Skripte, die Regel steht nicht mehr nur hier:

- **Vor dem Unity-Aufruf** prüfen beide Skripte `git status --porcelain` über den
  ganzen Baum — dieselbe Definition wie der Editor-Hook `BuildCommitStamp`. Ist
  die Ausgabe nicht leer (auch untrackte Dateien: Unity kompiliert alles unter
  `Assets/` mit), brechen sie ab und nennen die ersten 20 Pfade.
- **`--allow-dirty`** ist der Ausweg für lokale Pakete. Der Stempel behält dann
  `-dirty` und markiert das Paket als nicht rekonstruierbar. Für ein gemeinsames
  Match taugt es nicht.
- **`--fast`** (nur macOS) ist ausgenommen: kein DMG, nur für den eigenen Rechner.
- **`--skip-build`** läuft durch dieselbe Prüfung. Nach jedem erfolgreichen
  Unity-Build schreiben die Skripte `Builds/<Plattform>/.build-commit` (voller
  HEAD und Baumzustand `clean`/`dirty`). `--skip-build` bricht ab, wenn die Datei
  fehlt oder nicht zum aktuellen HEAD und Baumzustand passt — sonst bekäme ein
  alter Build das Etikett des jetzigen Commits. Die Datei kommt nicht ins
  Linux-`tar.gz`.
- **HEAD ≠ `origin/main`** ist nur eine Warnung (nach `git fetch origin main`).
  Offline bricht der Fetch nicht ab; der Vergleich nutzt dann den Stand des
  letzten Fetch und sagt das.
- **`--check-only`** führt nur diese Prüfungen aus und steigt vor Unity aus.

### Wo gebaut wird

Beta-Pakete entstehen in einem frischen Worktree von `origin/main`, nicht in der
Arbeitskopie und nicht im Dauer-Worktree `main-baseline`:

```bash
git fetch origin main
git worktree add ../.hashkrieg-worktrees/beta origin/main
cd ../.hashkrieg-worktrees/beta
git update-index --skip-worktree Assets/_Project/Data/Registries/AssetMappingRegistry.asset
tools/packaging/build-mac.sh
```

`ArtAssetAutoSync` schreibt die `AssetMappingRegistry.asset` beim Import um
(`Assets/_Project/Editor/ArtAssetAutoSync.cs`). Ohne
`skip-worktree` auf dieser Datei steht der Baum nach dem ersten Unity-Lauf
unsauber da, der Editor-Hook stempelt `-dirty`, und ein anschließendes
`--skip-build` scheitert an der Prüfung. Das Art-Paket liegt außerhalb des Repos
(DemoRunbook): ohne es zeigt der Beta Graybox-Primitive.

## Voraussetzungen

Für macOS:

- `Developer ID Application: Dennis Westermann (VHUL8MFGQT)` im Login-Keychain
- notarytool-Profil `apple-notary`
- Unity mit `MacStandaloneSupport` in der Version aus `ProjectSettings/ProjectVersion.txt`

Für Linux:

- dieselbe gepinnte Unity-Version aus `ProjectSettings/ProjectVersion.txt`
- das Hub-Modul `Linux Build Support (Mono)` unter
  `PlaybackEngines/LinuxStandaloneSupport`

`build-linux.sh` installiert ein fehlendes Modul nicht. Es bricht vor dem
Build mit dem erwarteten Pfad ab. `--skip-build` verlangt stattdessen einen
vorhandenen ausführbaren Player unter
`Builds/Linux64/ProjectNova.x86_64`.

Der Unity-Editor muss geschlossen sein — er hält eine Sperre auf `Library/`.
Das Skript prüft das und bricht mit klarer Meldung ab.

## Zwei Entscheidungen, die im Skript stecken

**Universal statt arm64-only.** Der Build enthält beide Architekturen. Auf einem
Apple-Silicon-Mac ist die Intel-Hälfte tote Last, aber sie kostet nur Dateigröße
— und ohne sie startet der Build auf einem Intel-Mac gar nicht. Solange nicht
feststeht, worauf der Mitspieler sitzt, ist Universal die Antwort.

**Signiert wird im Skript, nicht über `notarize.sh --sign`.** Dessen
Suchausdruck fasst bei Unity-Apps auch `PlugIns/*.bundle/Contents` an — ein
nacktes Verzeichnis, an dem `codesign` mit „bundle format unrecognized"
abbricht. Die Reihenfolge steht hier deshalb von Hand: erst lose Dylibs, dann
Plugin-Bundles als Einheit, das Hauptbundle zuletzt.

**Die App wird vor dem DMG notarisiert und gestapelt.** Dadurch trägt auch eine
aus dem DMG herausgezogene App ihr Ticket und startet ohne Online-Prüfung —
zwei Notarisierungsrunden statt einer, dafür kein Rätselraten beim Empfänger
ohne Netz.

## Was das Skript nicht löst

Ein Paket ist noch kein Netz-Nachweis. Zwei Unity-Fenster, LAN und VPS müssen
mit demselben gestempelten Commit tatsächlich gespielt und getrennt
protokolliert werden. Die Skripte liefern nur das zuordenbare Artefakt.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 2.1.0 | 2026-10-17 | Sauberkeitsprüfung vor Unity, `--allow-dirty`, `--check-only`, Build-Sidecar für `--skip-build`, Bauort-Hinweis (#132, D-124) | Project Nova Team |
| 2.0.0 | 2026-08-08 | Linux-x64-Build, Commit-Stempel und `tar.gz`-Verteilung ergänzt; offene Netzabnahmen ehrlich benannt | Project Nova Team |
| 1.0.0 | 2026-08-08 | macOS-Build-, Signatur-, Notarisierungs- und DMG-Weg dokumentiert | Project Nova Team |
