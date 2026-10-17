---
issue: 132
paket: P-132
branch: chore/132-enforce-clean-beta-builds
agent: builder (sonnet)
datum: 2026-10-17
status: DONE
---
# Beta-Pakete nur aus sauberem Baum (#132)

## Aufgabe
Die Regel „verschickt wird nur, was aus einem sauberen Commit fällt“ stand nur in der README. `build-mac.sh` und `build-linux.sh` hängten bei unsauberem Baum nur `-dirty` an, und `--skip-build` stempelte einen alten Build mit dem aktuellen Commit. Umgesetzt: harte Sperre vor dem Unity-Aufruf, `--allow-dirty`, Sidecar-Datei für `--skip-build`, Warnung bei HEAD ≠ origin/main. Teil 1 von #132 (InputGateDiagnostic.cs) ist überholt, nichts zu tun.

## Was geändert wurde — Files Changed
- tools/packaging/build-mac.sh — neuer Schritt „Quellstand“ vor Unity: Abbruch bei `git status --porcelain` ≠ leer, `--allow-dirty`, `--check-only`, Sidecar-Prüfung bei `--skip-build`, Sidecar schreiben nach Unity-Build, origin/main-Warnung; `--fast` ausgenommen (behält `-dirty`)
- tools/packaging/build-linux.sh — dasselbe (ohne `--fast`, das Linux nicht kennt); Sidecar wird aus dem `tar.gz` ausgeschlossen
- tools/packaging/README.md — 2.0.0 → 2.1.0: Optionstabelle, Abschnitt „Nur aus sauberem Baum“ statt der alten Absatzregel, Bauort (frischer Worktree von origin/main, skip-worktree auf `Assets/_Project/Data/Registries/AssetMappingRegistry.asset` wegen ArtAssetAutoSync), Verlaufszeile
- docs/production/DecisionLog.md — Entwurf D-124 vor „## Offene Punkte“ (Kopfversion/Verlauf nicht angefasst)
- CHANGELOG.md — eine Zeile unter [Unreleased] / Geändert (#132)

## Tests und Prüfungen
- `bash -n` auf beide Skripte: ok.
- `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release`: 778/778 grün (Baseline unverändert, keine C#-Änderung).
- `python3 .github/scripts/check_docs.py`: OK, 203 Markdown-Dateien, 5 Quality-JSONs.
- `python3 .github/scripts/check_baseline_guard.py --self-test`: bestanden.
- Shell-Gegenprobe (läuft in keiner CI) in einem Wegwerf-Repo mit Bare-Origin unter dem Scratchpad (Skripte hineinkopiert, `--check-only` bzw. Fake-Linux-Player):
  - sauberer Baum: beide Skripte Exit 0, „HEAD = origin/main“
  - untrackte Datei: beide Exit 1 mit Pfadliste, vor Unity
  - `--allow-dirty`: Exit 0, Stempel `b4a7bca-dirty`; mac `--fast` unsauber ebenso
  - Linux `--skip-build`: ohne Sidecar Exit 1; Sidecar mit falschem HEAD Exit 1; Zustand dirty vs. clean Exit 1; passender Sidecar Exit 0
  - Linux `--skip-build` vollständig durchgelaufen (Fake-Player): Archiv enthält keine `.build-commit`, Stempel im Archiv stimmt; Wegwerf-Repo bleibt sauber (Builds/ ignoriert)
  - mac `--skip-build` ohne Sidecar: Exit 1
  - Fetch auf nicht existierende URL bzw. ohne `origin`: kein Abbruch, Hinweis/Warnung; HEAD ≠ origin/main: nur Warnung, Exit 0
- NICHT gelaufen: echter Unity-Build, Schreiben des Sidecars nach einem echten Build (Zweig nach dem Unity-Aufruf), Signieren/Notarisieren/DMG, mac `--skip-build` mit passendem Sidecar bis zum Ende (braucht Unity unter /Applications), echtes Verhalten von ArtAssetAutoSync nach Import. Unity-Tests: keine angelegt.

## Entscheidung
D-124 (Entwurf): Alternative 2, harte Sperre in den Skripten; verworfen: Status quo, Klon-Skript, Abbruch im Editor-Hook. HEAD ≠ origin/main bleibt Warnung. Siehe docs/production/DecisionLog.md.

## Nebenbefunde
- Die gitignorierte Kopie tools/build/build-mac.sh im Hauptarbeitsbaum bleibt ohne Sperre (nicht angefasst; Inhaber löscht sie lokal).
- Der Editor-Hook BuildCommitStamp stempelt `-dirty`, wenn Unity die AssetMappingRegistry beim Import verändert, obwohl das Skript „clean“ sah. Abhilfe steht in der README (skip-worktree), nicht im Code.
- Auch harmlose untrackte Dateien (z. B. `.playwright-mcp/` im Hauptbaum) sperren künftig ein Vollpaket.
- Die Triage nannte Art-Branch-Dateien (ArtViewScale.cs usw.) als untrackt-kompilierend: gleiche Falle wie #132, nicht Teil dieses Pakets.
- P-14 (Welle 4) benennt Build-Namen in denselben Skripten/README um und läuft danach; mögliche Merge-Konflikte in den Header-Zeilen.

## Offene Fragen an den Inhaber
- Soll `build-mac.sh --fast` wirklich von der Sperre ausgenommen sein (so umgesetzt) oder auch `--allow-dirty` brauchen?
- Muss ein Release-DMG zwingend HEAD = origin/main sein (dann Warnung zu Abbruch machen)?
- Teil 1 von #132: Bestätigung, dass auf keinem Rechner eine Kopie von InputGateDiagnostic.cs liegt; Kommentar zu #132 nur mit Freigabe.

## Vorschlag für Commit-Titel und PR-Beschreibung
Commit: `build(packaging): refuse beta packages from a dirty tree (#132)`

PR-Beschreibung:
- Was: build-mac.sh/build-linux.sh brechen vor Unity bei unsauberem Baum ab; `--allow-dirty` (Stempel `-dirty`), `--check-only`; `--skip-build` vergleicht gegen `Builds/<Plattform>/.build-commit`; HEAD ≠ origin/main nur Warnung, offline robust; README 2.1.0 mit Bauort.
- Warum: Die Regel stand nur in der Doku; ein Build aus der Arbeitskopie ist nicht reproduzierbar (#132). Entwurf D-124.
- Bereiche: tools/packaging, README, DecisionLog, CHANGELOG. Keine C#-, keine Simulationsänderung.
- Changelog: Geändert, „Beta-Pakete nur aus sauberem Baum …“ (#132).
- Unity-Spuren: nicht gelaufen (kein Unity-Build, kein Unity-Test). Shell-Gegenprobe im Wegwerf-Repo gelaufen, siehe oben.
- Spielabnahme: nicht gespielt; Restrisiko: Sidecar-Schreiben nach echtem Unity-Build und der macOS-Pfad ab Unity sind unverifiziert.
