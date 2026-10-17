---
issue: 110
paket: P-110
branch: chore/110-unity-test-lanes-local
reviewer: opus
runde: 1
verdict: APPROVED
datum: 2026-10-17
---
# Review 1 – P-110: Unity-Spuren als lokale Pflichtspur

Unabhängiger Read-only-Review nach AGENTS.md §3. Ich habe keine Projektdatei geändert; einzige
geschriebene Datei ist dieser Bericht. Eigene Hilfsdateien liegen unter
`scratchpad/build-110/review1/` (außerhalb des Repos).

## Was geprüft wurde

- `git status --short` und der vollständige `git diff` im Worktree `chore-110` (Basis `a6858d1`),
  dazu der Inhalt der neuen Datei `tools/unity-tests/run_unity_tests.py` (541 Zeilen) und der
  Builder-Bericht `06-issue-110-unity-test-lane.md`.
- Abgleich gegen Issue #110, Paketspezifikation `paket-P-110.json` (Schritte, Schreibumfang, neue
  Tests, Konflikte).
- Geänderte Dateien: `.github/pull_request_template.md`, `AGENTS.md`, `CHANGELOG.md`,
  `CONTRIBUTING.md`, `docs/production/DecisionLog.md`; neu: `tools/unity-tests/`, Bericht. Alles im
  Schreibumfang. `tests.yml`, `ProjectSettings/`, Produktivcode, `docs/README.md`, `ScopeLedger.md`
  unberührt. Die vier Guard-Dateien sind unverändert (`git diff --quiet` auf alle vier: leer).
- Keine untrackten Testrunner-Reste: kein `InitTestScene*`, kein `.unity-tests/`. `output/` (darin
  `output/demo/` aus dem PlayMode-Lauf) und `Library/` sind git-ignoriert.
- Belegkette in D-125 nachgeprüft: `a6858d1` (#144) ändert `MainMenuController.cs`,
  `Nova.Gameplay.asmdef`, `Nova.AI.Tests.asmdef`; `e4faa75` (#141) nennt 754/754 headless;
  `85c912b` (#143) nennt PlayMode 15/15, EditMode 663/663 und entfernt
  `SENTIS_ANALYTICS_ENABLED` aus `ProjectSettings.asset`. `GrayboxLog.md` (Fallstrick `-quit`, PlayMode
  ohne `-nographics`) und `GrayboxDemoProofTests.cs:30` bestätigen die Begründung gegen „PlayMode
  headless“.

## Nachgerechnete Ergebnisse (selbst gemessen)

| Prüfung | Ergebnis |
|---|---|
| `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release` | 778/778 bestanden, 0 Fehler, 14 s |
| `python3 .github/scripts/check_docs.py` | OK, 204 Markdown-Dateien, 5 Quality-JSONs (übliche Statuskopf-Hinweise) |
| `python3 .github/scripts/check_baseline_guard.py --self-test` | bestanden, inkl. Negativkontrolle |
| `python3 tools/unity-tests/run_unity_tests.py --self-test` | Exit 0 |
| Skript ohne Lane-Argument | Exit 2 (argparse-Fehler) |
| Fake-Unity (`exit 0`, keine Ergebnisdatei = `-quit`-Falle), `edit` mit `--results-dir` im Scratchpad | Exit 1, „result file missing“, Beweiszeile „Unity-Spuren NICHT GRUEN: EditMode ROT“ |

**Unity-Ergebnisse** (nicht selbst gestartet, sondern die XML unter `scratchpad/build-110/` mit eigenem
Parser nachgezählt, nur `test-case`-Knoten):

| Datei | `<test-run>`-Kopf | gezählte `test-case` |
|---|---|---|
| `results/editmode-results.xml` | total=663 passed=663 failed=0 skipped=0 | 663 × Passed |
| `results/playmode-results.xml` | total=15 passed=15 failed=0 | 15 × Passed |
| `results-all/editmode-results.xml` | total=663 passed=663 failed=0 | 663 × Passed |
| `results-all/playmode-results.xml` | total=15 passed=15 failed=0 | 15 × Passed |

Die Zeitstempel (15:32–15:36Z) passen zu den Ausgaben `run-edit.out`, `run-play.out`, `run-all.out`;
die Befehlszeilen dort enthalten kein `-quit`, `-nographics` nur bei EditMode. Die Behauptung
„EditMode 663/663, PlayMode 15/15“ ist damit belegt.

**Mutationsprobe des `--self-test`** (Kopien im Scratchpad): Entfernen von Frische-Prüfung,
Fehlschlag-Prüfung, Editor-Sperre, `-nographics`-Trennung oder Einfügen von `-quit` lässt den
Self-Test jeweils rot werden (Exit 1). Das Entfernen der Leer- und der Null-Case-Prüfung bleibt grün,
weil das Verhalten über die XML-Parse- bzw. „no test case passed“-Prüfung erhalten bleibt (kein
Defekt). Das Entfernen der Unity-Exitcode-Prüfung bleibt grün – siehe Befund N2.

## Befunde

### Hoch
Keine.

### Mittel

- **M1 – Auslöserliste deckt nachweislich testrelevante Flächen nicht ab.**
  `AGENTS.md:89-91`, `CONTRIBUTING.md:61-64`, `.github/pull_request_template.md:11-12`,
  `docs/production/DecisionLog.md` (D-125, „Auslöser“). Nicht enthalten sind:
  `Assets/_Project/Scripts/Networking/` (eigene Test-Assembly
  `Assets/Tests/EditMode/Networking/Nova.Networking.Tests.asmdef`; `MatchConfigTests` nutzt
  `Nova.Networking`; der Baseline-Guard führt Networking als Verhaltensfläche),
  `Assets/_Project/Data/` (die Registry- und Karten-Assets prüft `Nova.Data.Tests` –
  `GameDatabaseTests`, `MapDefinitionTests`, `AssetIntegrationTests`), `Assets/_Project/UI/`
  (PanelSettings/Themes, die die PlayMode-`MainMenuTests` – genau die Testklasse aus #110 –
  rendern), `Packages/` und `ProjectSettings/`. Die Liste stammt aus der Spezifikation; der Builder
  hat sie korrekt übernommen und die Lücke selbst als offene Frage gemeldet. Kein Scope-Verstoß,
  aber die Regel soll ja gerade #141-artige Überraschungen fangen.
  *Vorschlag:* Auslöser vereinfachen auf „alles unter `Assets/`, `Packages/`, `ProjectSettings/`“
  – kürzer, nicht lückenhaft, und in allen vier Stellen gleich. Inhaber-Entscheidung.

- **M2 – Editor-Sperre ist unter Windows wirkungslos.** `tools/unity-tests/run_unity_tests.py:183-194`.
  `os.kill(pid, 0)` ist unter Windows keine Existenzprüfung: Signal 0 ist dort
  `CTRL_C_EVENT` und geht über `GenerateConsoleCtrlEvent` an eine Prozessgruppe. Für einen
  GUI-Editor scheitert das typischerweise mit `OSError` → `_pid_alive` liefert `False` → die Sperre
  greift nicht (fail-open), und der Self-Test-Fall „live editor pid blocks the run“ dürfte dort rot
  sein. Der Builder hat Windows/Linux ehrlich als ungetestet ausgewiesen, der Code (Hub-Pfad
  `C:/Program Files/...`) suggeriert aber Unterstützung. Heute betrifft das niemanden (Inhaber auf
  macOS).
  *Vorschlag:* unter `os.name == "nt"` bei vorhandener `EditorInstance.json` fail-closed abbrechen
  oder die Liveness per `ctypes`/`OpenProcess` prüfen; alternativ im Docstring „nur macOS/Linux
  geprüft, Windows: Sperre fail-closed“ festhalten.

### Niedrig

- **N1 – Gefilterter Lauf erzeugt dieselbe Beweiszeile wie ein Volllauf.**
  `run_unity_tests.py:377-383`. Mit `--test-filter Fixture` lautet die letzte Zeile z. B.
  „Unity-Spuren: EditMode 3/3“ – kopierbar in die PR-Pflichtzeile, ohne dass erkennbar ist, dass
  nur ein Ausschnitt lief. *Vorschlag:* bei gesetztem Filter „(gefiltert: …)“ anhängen oder das
  Präfix „Unity-Spuren:“ unterdrücken.
- **N2 – Der Self-Test deckt `run_lane`/`main_run` nicht ab.** Eine Mutation, die die
  Unity-Exitcode-Prüfung (`run_unity_tests.py:321-325`) entfernt, lässt `--self-test` grün. Der
  Builder hat den Pfad von Hand mit einem Fake-Editor geprüft, und ich habe den Fall „Exit 0 ohne
  Ergebnisdatei“ reproduziert (Exit 1). *Vorschlag:* im Self-Test `run_lane` einmal mit einem
  Fake-Binary fahren, das eine grüne XML schreibt und mit 2 endet (erwartet: nicht ok).
- **N3 – Docstring überzeichnet den Arbeitsbaum-Vergleich.** `run_unity_tests.py:22-23` („every
  change the run itself caused“): Der Vergleich sieht jede Änderung während des Laufs, auch fremde.
  Der `all`-Lauf des Builders meldete `CHANGELOG.md` und `DecisionLog.md`, die er parallel selbst
  bearbeitet hat – im Bericht offen erklärt. *Vorschlag:* „every change made while the run was in
  progress“.
- **N4 – `tests.yml:14-15` widerspricht jetzt der neuen Regel** („Wer die Präsentationsschicht
  anfasst …“). Bewusst unangetastet (Sicherheitsfläche, Spezifikation); beim nächsten Workflow-PR
  mitnehmen.
- **N5 – Zahl im Bericht leicht abweichend:** Bericht nennt bei `check_docs.py` 203 Markdown-Dateien,
  die Builder-Rückmeldung und meine Messung 204. Erklärbar dadurch, dass der Bericht selbst erst
  danach entstand; kein Ehrlichkeitsproblem.

## Doku, Changelog, D-ID

- `AGENTS.md` 5.0.0 → 5.1.0 mit Verlaufszeile, Änderung auf DoD-Zeile (§3) und Testabschnitt (§4)
  beschränkt, keine festen Testanzahlen – wie gefordert. Die Änderung am Steuerdokument braucht die
  ausdrückliche Zustimmung des Inhabers (vom Builder als offene Frage gestellt).
- `CONTRIBUTING.md` 5.0.0 → 5.1.0 mit Verlaufszeile, §3 konsistent zu AGENTS.md.
- PR-Vorlage: Pflichtzeile vorhanden; CLA-Marker (`check_external_contributor_review.py:14`) unberührt.
- `CHANGELOG.md`: genau ein Eintrag unter `[Unreleased]` → „Hinzugefügt“, mit „Refs #110“ und D-125.
  Kategorie passt (neues Werkzeug); „Entschieden“ wäre ebenfalls vertretbar.
- D-125: richtige Position (nach D-114, vor „## Offene Punkte“), Kopfzeile im Nachbarformat,
  Status „Entwurf … verbindlich mit dem Merge des PR“, vier bewertete Alternativen (A GameCI,
  B selbstgehosteter Runner, C lokal mit Pflicht, Status quo), Entscheidung, Begründung,
  Konsequenzen inkl. Wiedervorlage-Bedingungen. Kopfversion/Verlauf des DecisionLog unberührt.
- Tote Links: keine (`check_docs.py` OK).
- Determinismus: nicht berührt (kein Code unter `Assets/_Project/Scripts/`).

## Ehrlichkeit des Builder-Berichts

Alle nachprüfbaren Zahlen stimmen mit meiner Messung bzw. den Ergebnis-XML überein (778/778,
663/663, 15/15, Self-Test grün, Fake-Editor Exit 1). Nicht-Gelaufenes ist ausgewiesen: keine CI,
nur macOS, keine Art-Assets im Worktree (Graybox-Primitive), Library per rsync aus dem
Hauptarbeitsbaum, nicht gespielt. Bericht ehrlich.

## Empfehlung für den Inhaber

**APPROVED** – mergebar nach grüner Pflicht-CI, sofern du der AGENTS.md-Änderung ausdrücklich
zustimmst. Vor dem Merge entscheiden:

1. M1: Auslöserliste auf „`Assets/`, `Packages/`, `ProjectSettings/`“ verbreitern (empfohlen) oder
   wie vorliegend mit Networking/Data/UI-Lücke annehmen.
2. M2: Windows-Sperre jetzt fail-closed machen oder als bekannte Grenze im Docstring/D-125 notieren.
3. N1/N2 sind kleine Härtungen am eigenen Skript und können in derselben Runde mit.

Commit-Titel des Builders (`chore(tests): run Unity test lanes locally via one script (D-125)`,
67 Zeichen) passt zur Konvention.
