---
issue: 127
paket: P-127
branch: chore/127-remove-map-definition-so
agent: builder (sonnet)
datum: 2026-10-17
status: DONE
---
# MapDefinitionSO ohne Laufzeitkonsumenten abschaffen

## Aufgabe
Issue #127 verlangt die Wahl: `MapDefinitionSO` einen Konsumenten geben oder abschaffen. Umgesetzt ist die Empfehlung der Triage (abschaffen): Typ, Enum, Asset, Test und Generator-Abschnitt entfallen, ebenso `MatchBootstrap.CanonicalHqCentreCells`.

## Was geändert wurde (Files Changed)
- Assets/_Project/Scripts/Data/Maps/MapDefinitionSO.cs (+.meta) — gelöscht, kein Laufzeitleser
- Assets/_Project/Scripts/Data/Maps/MapBiomeType.cs (+.meta) — gelöscht, nur von obigem Typ benutzt
- Assets/_Project/Scripts/Data/Maps.meta — gelöscht (Ordner-.meta)
- Assets/_Project/Data/Maps/MAP_Glutrinne.asset (+.meta) — gelöscht
- Assets/_Project/Data/Maps.meta — gelöscht (Ordner-.meta)
- Assets/Tests/EditMode/Data/MapDefinitionTests.cs (+.meta) — gelöscht, einziger Test des Typs
- Assets/_Project/Editor/BootstrapSceneGenerator.cs — `using Nova.Data;`, Aufruf und `EnsureGlutrinneMapAsset` samt Doc-Kommentar entfernt (`Nova.Data` wird sonst in der Datei nicht gebraucht)
- Assets/_Project/Scripts/Gameplay/Match/MatchBootstrap.cs — `CanonicalHqCentreCells` entfernt (git grep: nur der Generator las sie); Kommentar von `CanonicalFieldCells` gekürzt, Property bleibt (`AllFieldCells`)
- docs/tech/modules/MapExpansion_Spec.md — Version 1.1.2, toten Testlink entfernt, Hinweise auf Wegfall (#127, D-123)
- docs/production/ScopeLedger.md — Zeilen 43/44: Verweis auf das Datenasset gestrichen, Terrain-Aussage auf Stand D-109 (C#-Prädikat) berichtigt
- docs/production/DecisionLog.md — D-123-Entwurf vor „## Offene Punkte"; Kopfversion/Verlauf unberührt
- CHANGELOG.md — eine Zeile unter Entschieden (#127)

## Tests und Prüfungen
- `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release`: 778/778 grün (gemessen). Hinweis: diese Lane kompiliert weder Gameplay noch Editor, belegt die C#-Änderungen also nicht.
- `python3 .github/scripts/check_docs.py`: OK, 203 Markdown-Dateien. `check_baseline_guard.py --self-test`: bestanden.
- `git grep` auf MapDefinitionSO / MapBiomeType / CanonicalHqCentreCells in .cs/.asmdef/.unity/.prefab/.asset: keine Treffer mehr. In Doku/Altberichten (GrayboxLog, Statusberichte, Sprintdokumente) bleibt das Asset historisch genannt, absichtlich nicht angefasst.
- GUID-Gegenprobe (git grep über Assets, ProjectSettings, Packages): MapBiomeType 62af6784…, Asset 2fc75419…, Maps-Ordner e367d5c8… und 3b0d81bd…, Test a18a6271… — jeweils nur in der eigenen gelöschten .meta. Skript-GUID MapDefinitionSO 6e9d3d14… steht nur in der gelöschten .meta und im gelöschten Asset. Kein Verweis bleibt.
- NICHT gelaufen: Unity (Kompilieren von Nova.Editor, Nova.Data.Tests, MatchBootstrap; Generator-Lauf; EditMode-Tests). Keine neuen Tests. Spielabnahme: nicht gespielt.

## Entscheidung
D-123 (Entwurf): abgeschafft; Wiedereinführung nur als engine-freie Kartendefinition nach G5 mit eigener D-ID. Siehe docs/production/DecisionLog.md.

## Nebenbefunde
- Die Feldlage steht weiterhin mehrfach literal (MatchBootstrap, Determinism10000Scenario, Testspiegel); bewusst offen.
- ScopeLedger Z. 44 nennt noch „fünf endliche Felder" (Drift, P-12-drift überlassen).
- Ältere Doku (GrayboxLog, StatusSnapshot, Sprintdokument 22) nennt das Asset historisch.
- Ich habe `git rm` und danach `git reset -q` (nur Index, um die Löschungen unstaged zu lassen) benutzt; Arbeitsbaum-Zustand ist wie gewünscht, Index entspricht HEAD.

## Offene Fragen an den Inhaber
- Keine zwingenden. Bestätigung, dass vor der Mehrkarten-Entscheidung abgeschafft wird (so entschieden im D-123-Entwurf).

## Commit-Titel und PR
Titel: `chore(data): remove MapDefinitionSO without runtime reader`
PR: Was: Typ, Enum, Asset, Test, Generator-Abschnitt und `CanonicalHqCentreCells` entfernt. Warum: kein Laufzeitleser, Inhalt war unbemerkt falsch (#127, D-123). Bereiche: Data, Editor, Gameplay/Match, Doku. Changelog: Entschieden, D-123. Unity-Spuren: nicht gelaufen (Kompilieren, Generator, EditMode). Spielabnahme: nicht gespielt; Restrisiko: Unity-Kompilierfehler in Editor/Gameplay möglich, in der Headless-Lane nicht sichtbar. Textkonflikt: Art-Commit 73f54b5 ändert MatchBootstrap.cs bei ca. Z. 367, mein Hunk endet um Z. 342; kein Überlapp zu erwarten, nicht durchprobiert.
