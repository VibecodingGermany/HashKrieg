# Verteilbare Builds

**Dokumentversion:** 3.0.0 | **Stand:** 2026-08-09 | **Governance-Tier:** 2

Reproduzierbare Testpakete für macOS, Windows und Linux. macOS wird signiert
und notarisiert als DMG verteilt; Windows als `zip` und Linux als `tar.gz`,
beide mit einem von außen prüfbaren Commit-Stempel.

## Kurzfassung

```bash
tools/packaging/build-mac.sh
tools/packaging/build-windows.sh
tools/packaging/build-linux.sh
```

Ergebnisse:

- macOS: `Builds/dist/ProjectNova-<commit>.dmg`
- Windows x64: `Builds/dist/ProjectNova-win-x64-<commit>.zip`
- Linux x64: `Builds/dist/ProjectNova-linux-x64-<commit>.tar.gz`

## Das Testpaket — eine Datei für Testende

Für Menschen, die keinen Bock auf Plattformwahl haben, schnürt ein viertes
Skript aus dem macOS- und dem Windows-Ergebnis **ein** Paket samt Anleitung:

```bash
tools/packaging/build-mac.sh
tools/packaging/build-windows.sh
tools/packaging/build-testpaket.sh
```

Ergebnis: `Builds/dist/Hashkrieg-Test-<commit>.zip` (~336 MB)

```text
Hashkrieg-Test-<commit>/
  ANLEITUNG - ZUERST LESEN.txt
  PROMPT FUER CHATGPT.txt
  BUILD.txt
  macOS/ProjectNova-<commit>.dmg
  Windows/ProjectNova.exe + Daten
```

Die beiden Textdateien liegen unter `tools/packaging/testpaket/` und werden
beim Schnüren hineinkopiert. Wer die Anleitung ändert, ändert sie **dort** —
nicht in einem verschickten ZIP.

Zwei Eigenheiten, die im Skript stecken:

- **Der Commit kommt aus dem Artefakt, nicht aus `HEAD`.** Sonst verlangt eine
  geänderte Textdatei einen Neubau des ganzen Spiels. Weicht `HEAD` ab, sagt
  das Skript es — als Hinweis, nicht als Abbruch.
- **Der macOS-Stempel wird aus dem gemounteten DMG gelesen**, nicht aus dem
  Dateinamen. Ein umbenanntes DMG fällt sonst nicht auf. Die Notarisierung
  übersteht das äußere ZIP unbeschadet; das Ticket liegt in der Datei, nicht
  in einem erweiterten Attribut.

Der ausführliche Rahmen — Verteilkreis, Rückmeldeweg, Kadenz — steht in der
[Betatest-Anleitung](../../docs/production/Betatest.md).

## Optionen

| Flag | macOS | Windows | Linux |
|---|---|---|---|
| *(keine)* | bauen → signieren → notarisieren → DMG | bauen → stempeln → `zip` | bauen → stempeln → `tar.gz` |
| `--fast` | nur bauen, unsigniert, kein DMG | – | – |
| `--skip-build` | vorhandenen Build neu verpacken | vorhandenen Build neu stempeln und verpacken | vorhandenen Build neu stempeln und verpacken |
| `--open` | Ergebnis danach öffnen | – | – |

Für die eigene Iteration ist `--fast` der richtige Weg: Signieren und
Notarisieren kosten Minuten und bringen auf dem eigenen Rechner nichts. Ein
lokal gebauter Build hat kein Quarantäne-Attribut und startet ohnehin.

## Warum der Commit-Hash im Dateinamen steht

Der Relay-Server sperrt Matches zwischen ungleichen Builds ab (Sprint 12, A4 —
Fingerprint-Sperre). Beide Spieler brauchen also **genau denselben Commit**.
Der Hash steht deshalb im Paketnamen und im Player:

- im DMG-Dateinamen — `ProjectNova-1526d7a.dmg`
- in `LIESMICH.txt` im DMG und im Windows-ZIP
- im `Info.plist` der App unter `NovaBuildCommit`
- unter Windows und Linux in `ProjectNova_Data/NovaBuildCommit.txt`

Alle Player-Stempel lassen sich beim Empfänger ohne Rückfrage prüfen:

```bash
# macOS
defaults read /Applications/ProjectNova.app/Contents/Info.plist NovaBuildCommit
# Linux
cat ProjectNova_Data/NovaBuildCommit.txt
```

```bat
REM Windows
type ProjectNova_Data\NovaBuildCommit.txt
```

Ein Build aus einem unsauberen Arbeitsbaum bekommt `-dirty` angehängt und ist
damit als nicht rekonstruierbar markiert. Für ein gemeinsames Match taugt er
nicht — verschickt wird nur, was aus einem sauberen Commit fällt.

## Voraussetzungen

Für macOS:

- `Developer ID Application: Dennis Westermann (VHUL8MFGQT)` im Login-Keychain
- notarytool-Profil `apple-notary`
- Unity mit `MacStandaloneSupport` in der Version aus `ProjectSettings/ProjectVersion.txt`

Für Windows:

- dieselbe gepinnte Unity-Version aus `ProjectSettings/ProjectVersion.txt`
- das Hub-Modul `Windows Build Support (Mono)` unter
  `PlaybackEngines/WindowsStandaloneSupport`

```bash
"/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" -- --headless \
  install-modules --version 6000.5.4f1 --module windows-mono --childModules
```

Für Linux:

- dieselbe gepinnte Unity-Version aus `ProjectSettings/ProjectVersion.txt`
- das Hub-Modul `Linux Build Support (Mono)` unter
  `PlaybackEngines/LinuxStandaloneSupport`

Die Skripte installieren ein fehlendes Modul nicht. Sie brechen vor dem Build
mit dem erwarteten Pfad ab — `build-windows.sh` nennt dabei den Hub-Befehl von
oben. `--skip-build` verlangt stattdessen einen vorhandenen Player unter
`Builds/Windows64/ProjectNova.exe` beziehungsweise
`Builds/Linux64/ProjectNova.x86_64`.

Der Unity-Editor muss geschlossen sein — er hält eine Sperre auf `Library/`.
Das Skript prüft das und bricht mit klarer Meldung ab.

## Windows ist unsigniert — und das bleibt vorerst so

Der Windows-Player wird von macOS aus mit dem Mono-Backend gebaut und **nicht
signiert**. SmartScreen zeigt beim ersten Start deshalb „Der Computer wurde
durch Windows geschützt"; der Weg darum herum („Weitere Informationen" →
„Trotzdem ausführen") steht in der `LIESMICH.txt` im Paket und in der
[Betatest-Anleitung](../../docs/production/Betatest.md).

Eine Windows-Codesignatur bräuchte ein EV-Zertifikat mit Hardware-Token und
laufender Reputation bei Microsoft. Für ein bis zwei Testende ist das Aufwand
ohne Ertrag — für eine öffentliche Verteilung wäre es eine eigene Entscheidung.

**Ehrliche Einschränkung:** Der Windows-Build entsteht auf einem Mac. Ob er
startet, weiß erst der erste Mensch, der ihn startet. Bis dahin ist „gebaut"
nicht „lauffähig".

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
| 3.1.0 | 2026-08-09 | `build-testpaket.sh` ergänzt: macOS und Windows in einem ZIP samt Anleitung und ChatGPT-Prompt, Commit aus dem Artefakt statt aus `HEAD`, DMG-Stempel aus dem gemounteten Abbild geprüft | Project Nova Team |
| 3.0.0 | 2026-08-09 | Windows-x64-Build als `zip` mit Commit-Stempel und LIESMICH ergänzt; fehlendes Hub-Modul und SmartScreen-Warnung benannt; Verweis auf die Betatest-Anleitung | Project Nova Team |
| 2.0.0 | 2026-08-08 | Linux-x64-Build, Commit-Stempel und `tar.gz`-Verteilung ergänzt; offene Netzabnahmen ehrlich benannt | Project Nova Team |
| 1.0.0 | 2026-08-08 | macOS-Build-, Signatur-, Notarisierungs- und DMG-Weg dokumentiert | Project Nova Team |
