# Verteilbare Builds

Ein Testbuild, den jemand anders installieren und spielen kann — signiert,
notarisiert, als DMG.

## Kurzfassung

```bash
tools/packaging/build-mac.sh
```

Ergebnis: `Builds/dist/ProjectNova-<commit>.dmg`. Verschicken, Empfänger zieht
die App nach „Applications", Doppelklick. Keine Gatekeeper-Warnung, kein
Rechtsklick-Öffnen, keine Erklärung nötig.

## Optionen

| Flag | Wofür |
|---|---|
| *(keine)* | voll: bauen → signieren → notarisieren → DMG → notarisieren |
| `--fast` | nur bauen, unsigniert, kein DMG — für eigenes Probespielen |
| `--skip-build` | vorhandenen Build nur neu verpacken |
| `--open` | Ergebnis danach öffnen |

Für die eigene Iteration ist `--fast` der richtige Weg: Signieren und
Notarisieren kosten Minuten und bringen auf dem eigenen Rechner nichts. Ein
lokal gebauter Build hat kein Quarantäne-Attribut und startet ohnehin.

## Warum der Commit-Hash im Dateinamen steht

Der Relay-Server sperrt Matches zwischen ungleichen Builds ab (Sprint 12, A4 —
Fingerprint-Sperre). Beide Spieler brauchen also **genau denselben Commit**.
Der Hash steht deshalb an drei Stellen:

- im DMG-Dateinamen — `ProjectNova-1526d7a.dmg`
- in `LIESMICH.txt` im DMG
- im `Info.plist` der App unter `NovaBuildCommit`

Die letzte Stelle lässt sich beim Empfänger ohne Rückfrage prüfen:

```bash
defaults read /Applications/ProjectNova.app/Contents/Info.plist NovaBuildCommit
```

Ein Build aus einem unsauberen Arbeitsbaum bekommt `-dirty` angehängt und ist
damit als nicht rekonstruierbar markiert. Für ein gemeinsames Match taugt er
nicht — verschickt wird nur, was aus einem sauberen Commit fällt.

## Voraussetzungen

Alles bereits auf dieser Maschine eingerichtet (siehe `apple-upload`-Skill):

- `Developer ID Application: Dennis Westermann (VHUL8MFGQT)` im Login-Keychain
- notarytool-Profil `apple-notary`
- Unity mit `MacStandaloneSupport` in der Version aus `ProjectSettings/ProjectVersion.txt`

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

Ein Paket macht noch kein Match zu zweit. Solange A6 aus Sprint 12
(`MatchConfig`, beweglicher Slot) nicht steht, startet das Spiel nur Slot 0 =
Mensch gegen Slot 1 = KI, und es gibt keine Oberfläche, in die Serveradresse
und Match-Code eingetippt werden könnten. Das DMG ist die Auslieferung, nicht
die Verbindung.
