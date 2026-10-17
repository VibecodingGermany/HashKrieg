---
issue: 110
paket: P-110
branch: chore/110-unity-test-lanes-local
agent: builder (sonnet)
datum: 2026-10-17
status: DONE
---
# Unity-Spuren als lokale Pflichtspur (P-110, #110)

## Aufgabe
Hälfte 1 von #110 (roter PlayMode-Test) ist seit `e3149f9` erledigt. Offen war Hälfte 2: Die
Unity-Tests laufen in keiner CI und hatten weder Kommando noch Auslöser noch Zuständigen. Umgesetzt
ist Option C/D aus der Triage: ein Skript, das beide Spuren ehrlich fährt, ein breiterer Auslöser in
den Vertragsdokumenten, eine Pflichtzeile in der PR-Vorlage und die Entscheidung D-125 (Entwurf).
Dazu der Regressionslauf in Unity auf `a6858d1`.

## Was geändert wurde — Files Changed
- `tools/unity-tests/run_unity_tests.py` (neu, ausführbar) — `edit | play | all` und `--self-test`.
  Version aus `ProjectVersion.txt`, Abbruch bei lebendem Editor (`Library/EditorInstance.json`, Prozess
  wird geprüft; tote PID = Warnung, unlesbare Datei = Abbruch), nie `-quit`, `-nographics` nur
  EditMode, alte Ergebnis-XML und altes Log werden vorher gelöscht, danach Existenz, Frische, Anzahl
  `test-case` größer null, `failed == 0` und Unity-Exitcode 0. `git status` vor/nach mit Inhalts-Hash
  verglichen; Warnung bei `ProjectSettings.asset`, `InitTestScene*`, `.unity-tests`. Ausgabe der
  Beweiszeile „Unity-Spuren: EditMode N/N, PlayMode M/M“. Exit 0 grün, 1 rot/ungültiger Nachweis, 2
  Vorbedingung. Keine Abhängigkeit vom schlafenden Gate-Apparat. Standard-Ergebnisordner
  `output/unity-tests/` (git-ignoriert).
- `AGENTS.md` — 5.0.0 auf 5.1.0, Verlaufszeile; DoD §3 um eine Unity-Zeile ergänzt, §4 ersetzt den
  alten EditMode-Satz durch Auslöserliste, Zuständigkeit, Kommando und `-quit`-Falle. Keine Testanzahl.
- `CONTRIBUTING.md` — 5.0.0 auf 5.1.0, Verlaufszeile; §3 genauso (Absatz „Unity-Spuren“).
- `.github/pull_request_template.md` — Pflichtzeile „Unity-Spuren: EditMode N/N, PlayMode M/M — oder
  nicht gelaufen mit Grund“ in der Checkliste. Die CLA-Prüfung sucht nur einen Teilstring und bleibt
  unberührt.
- `docs/production/DecisionLog.md` — D-125 (Entwurf) am Ende des Entscheidungsteils vor „Offene
  Punkte“. Kopfversion und Änderungsverlauf bewusst unangetastet.
- `CHANGELOG.md` — ein Eintrag unter `[Unreleased]` / „Hinzugefügt“ (Refs #110).
- Nicht angefasst: `.github/workflows/tests.yml`, die vier Guard-Dateien, `docs/README.md`,
  `ScopeLedger.md`, Produktivcode, `MainMenuTests.cs`.

## Tests und Prüfungen
Alle im Worktree `chore-110`, Unity `6000.5.4f1`, Basis `a6858d1` plus diese Änderungen
(die Änderungen betreffen nur Skript und Doku, kein Spielcode).

- `python3 tools/unity-tests/run_unity_tests.py --self-test` — grün. Deckt: fehlende, leere, veraltete
  (mtime 1 h alt), kaputte (kein valides XML), rote, nur-übersprungene und Null-Case-Ergebnisdatei
  (jeweils nicht ok); grüne Datei ok; `-quit` nie im Kommando, `-nographics` nur EditMode;
  Versions-Parsing; Editor-Sperre (lebende PID blockiert, tote PID nicht, Müll blockiert);
  Arbeitsbaum-Diff und Rest-Warnungen.
- Zusätzlich von Hand gegen das echte Projekt mit einem Fake-Editor, der Exit 0 ohne Ergebnisdatei
  liefert (die `-quit`-Falle): Exit 1, „result file missing“. Mit einer `EditorInstance.json` mit
  lebender PID: Exit 2. Ohne Lane-Argument: Exit 2.
- **Unity EditMode** (`run_unity_tests.py edit`): **663/663 grün**, 0 fehlgeschlagen, 0 übersprungen.
  Die Ergebnisdatei trägt `total=663 passed=663 failed=0`, 663 `test-case`-Knoten.
- **Unity PlayMode** (`run_unity_tests.py play`): **15/15 grün**.
- `run_unity_tests.py all` danach nochmals vollständig: **EditMode 663/663, PlayMode 15/15**, Beweiszeile
  `Unity-Spuren: EditMode 663/663, PlayMode 15/15`. Die Warnungen am Ende dieses Laufs betrafen nur
  `CHANGELOG.md` und `DecisionLog.md`, die ich währenddessen selbst geändert habe (Beleg, dass der
  Vergleich greift). In den beiden Einzelläufen: „git status: the run left no new changes“. Es blieben
  weder `InitTestScene*` noch Änderungen an `ProjectSettings.asset` zurück.
- `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release` — **778/778 grün**
  (14 s).
- `python3 .github/scripts/check_docs.py` — OK (203 Markdown-Dateien, 5 Quality-JSONs), nur der übliche
  Hinweis auf 13 Dateien ohne Statuskopf.
- `python3 .github/scripts/check_baseline_guard.py --self-test` — grün.
- `git status` zum Schluss: 5 geänderte Dateien, `tools/unity-tests/` neu; sonst nichts.

**Was nicht gelaufen ist / Einschränkungen:**
- Die Library wurde per `rsync` aus dem Hauptarbeitsbaum (Art-Branch-Stand) übernommen; Unity hat
  Abweichungen neu importiert. Gemessen wurden die Quellen des Worktrees. Die Art-Assets (FBX/Prefabs,
  git-ignoriert) liegen im Worktree nicht, die Szene fährt also mit Graybox-Primitiven. Das ist der
  Stand eines frischen Klons, aber nicht der des Art-Branches.
- PlayMode lief mit Grafik im Batchmode ohne `-nographics`; Ergebnisse unter einem sichtbaren Editor
  oder auf einem anderen Rechner sind nicht gemessen.
- Eine GameCI-/CI-Variante wurde nicht erprobt (bewusst vertagt, D-125).
- Das Skript wurde nur unter macOS gefahren; die Windows-/Linux-Pfade stehen im Code, sind aber nicht
  getestet. Der `--self-test` läuft in keiner CI.
- Kein Issue-Kommentar, kein Folge-Issue, kein Schließen von #110 (nicht im Auftrag).

## Entscheidung
D-125 (Entwurf), `docs/production/DecisionLog.md`: **C — lokal vor dem Merge, mit Skript,
Auslöserliste, Zuständigem und Pflichtzeile im PR**, Wiedervorlage von **A (GameCI, nur EditMode)**
bei (a) wieder erwarteten externen PRs, (b) geklärter Unity-Lizenz in CI, (c) einem weiteren rot
gemergten Unity-Test trotz Pflichtzeile. B (selbstgehosteter Runner) verworfen (öffentliches Repo mit
Fork-PRs). Die verlorene Worker-Empfehlung „PlayMode headless“ ist ausdrücklich verworfen: PlayMode
braucht Grafik. Belegkette #110, #141, #143, #144. Auslöser: `Scripts/{Simulation,Core,AI,AI.Data,Data,
Gameplay,Presentation}/`, `Editor/`, `Scenes/`, `Assets/Tests/`. Zuständig: wer den PR vorbereitet
(Orchestrator bei der Sprint-Integration), der Inhaber prüft die Zeile beim Merge.

## Nebenbefunde
- **SENTIS-Define:** `SENTIS_ANALYTICS_ENABLED` fehlt in `ProjectSettings.asset` seit `85c912b` (#143).
  In meinen Läufen hat Unity die Datei nicht erneut verändert. Eigener kleiner Befund, nicht Teil dieses
  PR.
- **`Networking/` fehlt in der Auslöserliste** der Spezifikation (`Assets/_Project/Scripts/Networking/`
  steht im Baseline-Guard als Verhaltensfläche). Ich habe die Liste wie vorgegeben übernommen.
- **Testkommentare** verweisen noch auf `quality/scripts/run_gate_check.py:462` für die `-quit`-Falle
  (`GrayboxDemoProofTests.cs`, `MainMenuTests.cs`, `BarracksSpawnDiagnosisTests.cs`); das ist veraltet,
  das neue Skript wäre der bessere Verweis. Nicht angefasst (Unity-Tests, fremder Scope).
- `docs/production/hashkrieg/22_Sprint_Findbarkeit_und_Vertraege.md:141, 205` nennt „PlayMode 13/13“;
  gemessen sind 15. Abgeschlossenes Sprint-Dokument, bewusst unverändert.
- Härtung von `MainMenuTests` (Suche nach Name statt Text) ist optional und unnötig, solange der Test
  grün läuft; nicht umgesetzt.
- Der `--self-test` ließe sich in `integrity` einhängen; das wäre eine Änderung unter
  `.github/workflows/` (Sicherheitsfläche) und steht in D-125 als Kandidat für die Wiedervorlage.
- Der PlayMode-Lauf hat `output/demo/` im Worktree angelegt (git-ignoriert), ebenso liegt eine 5-GB-
  `Library/` im Worktree; beides beim Aufräumen des Worktrees mit entfernen.

## Offene Fragen an den Inhaber
1. Ist die Auslöserliste so richtig, oder soll `Assets/_Project/Scripts/**` (inklusive `Networking/`)
   komplett gelten?
2. Zuständigkeit wie in D-125 (PR-Vorbereiter, beim Merge prüft der Inhaber) — einverstanden?
3. Pflichtzeile mit erlaubtem „nicht gelaufen“ — oder soll die Zeile für Auslöser-PRs strenger werden?
4. Soll das verlorene SENTIS-Define zurückgeholt werden (eigener PR)?
5. Wiedervorlage-Bedingungen für die EditMode-CI (a)–(c) so recht? Und ist Unity Personal in CI
   lizenzrechtlich zulässig (kann der Code nicht beantworten)?
6. `Fixes #110` oder `Refs #110`? Ich habe `Refs` gewählt; „Fixes“ erst, wenn D-125 angenommen ist.
7. AGENTS.md ist das Steuerdokument aller Agenten: Die Änderung braucht die ausdrückliche Zustimmung
   des Inhabers.

## Vorschlag für Commit-Titel und PR-Beschreibung
**Commit-Titel:** `chore(tests): run Unity test lanes locally via one script (D-125)`

**PR-Titel:** wie Commit-Titel.

**PR-Beschreibung:**
- **Was:** `tools/unity-tests/run_unity_tests.py` fährt EditMode und PlayMode (nie `-quit`,
  `-nographics` nur EditMode, Editor-Sperre, Frische-/Anzahl-/Fehlerprüfung, Arbeitsbaum-Vergleich,
  `--self-test`). `AGENTS.md` und `CONTRIBUTING.md` auf 5.1.0 mit Auslöserliste, Zuständigkeit und
  Kommando. PR-Vorlage mit Pflichtzeile „Unity-Spuren“. D-125 (Entwurf) im DecisionLog.
- **Warum:** Die Unity-Spuren laufen in keiner CI. #110, #141 und #143 zeigen, dass das Folgen hat; der
  alte Auslöser (nur Präsentation/Gameplay) hätte #141 nicht erfasst. `tests.yml` bleibt unverändert,
  die CI-Aufnahme nur für EditMode ist mit Wiedervorlage-Bedingungen vertagt.
- **Bereiche:** `tools/unity-tests/`, `AGENTS.md`, `CONTRIBUTING.md`, `.github/pull_request_template.md`
  (CODEOWNERS Inhaber), `docs/production/DecisionLog.md`, `CHANGELOG.md`. Kein Spielcode, keine
  Baselines.
- **Changelog:** „Unity-Spuren sind eine lokale Pflichtspur mit Skript, Auslöserliste und Nachweiszeile
  im PR (D-125, Entwurf; Refs #110)“ unter Hinzugefügt.
- **Unity-Spuren:** gelaufen, EditMode 663/663, PlayMode 15/15, Unity 6000.5.4f1, Basis `a6858d1`
  (Regressionsnachweis nach #144, das ohne Unity-Lauf gemergt wurde). Warme Library aus dem
  Hauptarbeitsbaum, Graybox-Primitive statt Art-Assets.
- **Weitere Prüfungen:** `dotnet test` 778/778, `check_docs.py` OK, `check_baseline_guard.py
  --self-test` grün, `run_unity_tests.py --self-test` grün.
- **Spielabnahme:** nicht gespielt. Grund: Der PR ändert kein Spielverhalten. Restrisiko: gering; das
  Skript ist nur unter macOS gefahren, und die Pflichtzeile erzwingt den Lauf nicht, sie macht das
  Auslassen nur sichtbar.
- Refs #110 (Schließen erst nach Annahme von D-125).
