---
issue: 127
paket: P-127
branch: chore/127-remove-map-definition-so
reviewer: opus
runde: 1
verdict: APPROVED
datum: 2026-10-17
---
# Review 1 zu P-127: MapDefinitionSO ohne Laufzeitkonsumenten abschaffen

Unabhängiges Read-only-Review nach AGENTS.md §3. Ich habe keine Projektdatei geändert. Hilfsdateien liegen
unter `scratchpad/build-127/review/`.

## Was geprüft wurde

- `git status --short`, `git diff` (alle 16 Pfade) und der Builder-Bericht `04-issue-127-remove-map-definition-so.md`,
  abgeglichen mit Issue #127, Analyse/Gegenprüfung `triage/issue-127.json` und Paketspezifikation `paket-P-127.json`.
- Schreibumfang: alle 16 geänderten Pfade liegen im Paket-Schreibumfang, dazu kommt der Bericht. Die vier
  Guard-Dateien sind unverändert (`git diff HEAD --stat` leer). Der Index ist leer (`git diff --cached` leer),
  die Löschungen liegen unstaged im Arbeitsbaum.
- GUID-Gegenprobe (selbst gemessen): GUIDs aus den sechs gelöschten `.meta`-Dateien aus `HEAD` gelesen und mit
  `grep -rl` über `Assets/`, `ProjectSettings/` und `Packages/` des Arbeitsbaums gesucht:
  `a18a6271…` (Test), `3b0d81bd…` (Data/Maps-Ordner), `2fc75419…` (Asset), `e367d5c8…` (Scripts/Data/Maps-Ordner),
  `62af6784…` (MapBiomeType), `6e9d3d14…` (MapDefinitionSO). **Kein Treffer.** Die Ordner `Assets/_Project/Data/Maps`
  und `Assets/_Project/Scripts/Data/Maps` existieren nicht mehr, verwaiste `.meta`-Dateien bleiben keine zurück.
- Symbolsuche: `git grep` auf `MapDefinitionSO|MapBiomeType|CanonicalHqCentreCells|MAP_Glutrinne|EnsureGlutrinneMapAsset|MapDefinitionTests`
  findet keinen Code-, Szenen-, Prefab- oder Asset-Treffer mehr, nur historische Doku (GrayboxLog, StatusSnapshot,
  ImplementationAudit, Sprintdokumente 01/21/22) und die neuen D-123-, Changelog- und Spec-Hinweise.
- Kompilierbarkeit ohne Unity, durch Lesen geprüft: `BootstrapSceneGenerator.cs` benutzt nach dem Entfernen von
  `using Nova.Data;` keinen Typ aus `Assets/_Project/Scripts/Data/` mehr (alle dort deklarierten Typnamen gegen die
  Datei geprüft). `LocalLayout`/`EnemyLayout` in `MatchBootstrap` haben weiter Leser (Z. 367/368/795/796).
  `CanonicalFieldCells` bleibt über `AllFieldCells` (Z. 321) in Gebrauch. `Nova.Data.Tests` behält drei Testdateien
  plus asmdef.
- Konfliktprobe mit dem Art-Branch (lesend, per `git merge-file` im Scratchpad, Merge-Basis `85c912b`):
  - `MatchBootstrap.cs`, PR-Stand gegen `feat/generals-look-and-feel`: genau ein Konflikt bei `RestartMatch`
    (`ApplyLocalMatchSeed` aus main gegen `ApplyLocalSkirmishFaction` aus 73f54b5). Derselbe Konflikt entsteht auch
    mit dem unveränderten main-Stand `a6858d1`. Er gehört also nicht zu diesem PR. Der PR-Hunk (Z. 322–361) mischt sich
    sauber mit dem Art-Hunk @367.
  - `BootstrapSceneGenerator.cs`: konfliktfrei, sowohl gegen die Branch-Spitze als auch gegen den uncommitteten
    Arbeitsbaum-Stand im Hauptbaum. Der Art-Branch fügt weder im Commit noch im Arbeitsbaum neue Leser von
    `CanonicalHqCentreCells`, `MapDefinitionSO` oder `MapBiomeType` hinzu.
- Fachlich: Determinismus nicht berührt (keine Datei unter `Scripts/Simulation`, keine Baseline). D-123 ist
  vollständig. Changelog-Zeile vorhanden. Spec- und ScopeLedger-Inhalte stimmen mit dem Code überein.

## Nachgerechnete Ergebnisse (im Worktree, selbst ausgeführt)

| Prüfung | Befehl | Ergebnis | Builder-Angabe |
|---|---|---|---|
| Headless-Tests | `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release` | **778/778 bestanden**, 0 Fehler, 0 übersprungen (15 s) | 778/778, stimmt |
| Doku-Links | `python3 .github/scripts/check_docs.py` | **OK: 204 Markdown-Dateien**, 5 Quality-JSONs | 203. Die Differenz ist der danach geschriebene Builder-Bericht (203 getrackt + 1 untrackt), also plausibel und kein Widerspruch |
| Baseline-Guard Selbsttest | `python3 .github/scripts/check_baseline_guard.py --self-test` | bestanden, inklusive Negativkontrolle | stimmt |
| Baseline-Guard auf echtem Diff | `--changed-files <17 Pfade> --labels <leer>` | „passed: only one side of the separation“ | (nicht angegeben) |
| Unity (Kompilieren Nova.Editor/Nova.Data.Tests/Gameplay, Generator, EditMode) | — | **nicht gelaufen** (Review-Rahmen) | nicht gelaufen, korrekt ausgewiesen |

## Befunde

### hoch
Keine.

### mittel

1. **Verbotener Git-Befehl benutzt** (Builder-Bericht, Nebenbefunde): Der Builder hat `git rm` und danach `git reset -q`
   ausgeführt. `reset` ist im Paketrahmen ausdrücklich untersagt. Ich habe den Effekt geprüft: Der Index entspricht HEAD,
   der Arbeitsbaum zeigt die gewollten Löschungen, kein Schaden. Der Builder hat es offen gemeldet. Deshalb ist das ein
   Prozessbefund und kein Blocker. *Vorschlag:* Im Builder-Prompt künftig `rm`/`git rm --cached` vermeiden und reine
   Dateisystem-Löschung vorschreiben.
2. **ScopeLedger ohne Kopfversion und Verlaufszeile** (`docs/production/ScopeLedger.md:3` und `:145ff`): Die Zeilen
   `map.id`/`map.biome` (Z. 43) und `map.aetheriumFields` (Z. 44) wurden inhaltlich geändert, unter anderem die
   Terrain-Aussage auf D-109 berichtigt. Version 0.7.0 und der Änderungsverlauf blieben unverändert. Bisher hat jede
   inhaltliche Fortschreibung dieser Datei die Version erhöht (0.3.0–0.7.0). *Vorschlag:* 0.8.0 mit Verlaufszeile
   „#127/D-123: Datenasset-Verweis gestrichen, Terrain-Aussage auf D-109“. Die Arbeit mit P-12-drift (schreibt danach
   in dieselbe Datei) beim Integrieren abstimmen.

### niedrig

3. `docs/tech/modules/MapExpansion_Spec.md:22-27`: Die Spezifikation sah vor, auch die Biom-Liste auf den Wegfall
   hinweisen zu lassen. Erledigt ist das indirekt über den Zweck-Absatz (Z. 7). Das reicht für ein historisches
   Dokument, ist aber nicht wörtlich umgesetzt.
4. `docs/tech/modules/MapExpansion_Spec.md:67`: Als Autor der Verlaufszeile steht „Builder“, die übrigen Zeilen nennen
   Rollen („Lead Technical Director“). *Vorschlag:* Rolle eintragen.
5. `Assets/_Project/Scripts/Gameplay/Match/MatchBootstrap.cs:324-327`: Der gekürzte Docstring begründet `public static`
   noch mit „tooling can read“. Nach dem Wegfall des Generators gibt es außer `AllFieldCells` keinen statischen Leser
   mehr. Das ist sachlich nicht falsch und laut Spezifikation gewollt („Property bleibt“), aber die Begründung ist jetzt
   prospektiv. Kein Handlungszwang.
6. D-123 (`docs/production/DecisionLog.md`, Alternativen): Es gibt die geforderten drei Alternativen, aber nur 1 und 2
   tragen eine explizite Bewertung. Die Bewertung von 3 steckt in der „Begründung“. Formal ausreichend.

## Nebenbefunde (nicht umsetzen)

- `.gitignore` Z. 26: `reports/          # lokale …` — Git kennt keine Inline-Kommentare. Das Muster ist damit der
  Literalstring samt Kommentar und ignoriert `reports/` **nicht** (`git check-ignore` meldet nichts, `git ls-files
  --others --exclude-standard` listet den Bericht). Folge: `check_docs.py` zählt lokale Berichte mit, und der Hinweis
  im Rahmen „reports/ ist gitignored“ stimmt faktisch nicht. Eigene Kleinst-Korrektur außerhalb dieses Pakets.
- Der Konflikt `ApplyLocalMatchSeed`/`ApplyLocalSkirmishFaction` in `MatchBootstrap.RestartMatch` zwischen main (`a6858d1`)
  und dem Art-Branch besteht unabhängig von diesem PR und muss beim Rebase des Art-Branches gelöst werden.
- Die Nebenbefunde des Builders (Feldlage mehrfach literal, ScopeLedger Z. 44 „fünf Felder“, historische Asset-Nennungen)
  kann ich bestätigen. Sie sind korrekt als offen markiert.

## Ehrlichkeit des Builder-Berichts

Stimmt. Testzahl 778/778 und Guard-Selbsttest decken sich mit meiner Messung. Die Abweichung der Doku-Zählung
(203/204) erklärt sich durch die Reihenfolge. GUID-Liste und Symbolsuche stimmen. Unity, Generator-Lauf, EditMode und
Spielabnahme sind als nicht gelaufen ausgewiesen. Den Art-Konflikt hat der Builder als „nicht durchprobiert“ markiert.
Das habe ich nachgeholt: kein Konflikt durch diesen PR. Den `git reset` hat er selbst offengelegt.

## Empfehlung für den Inhaber

**APPROVED.** Mergebar nach grüner Pflicht-CI, wenn vorher oder beim Integrieren Folgendes passiert:
1. ScopeLedger auf 0.8.0 mit Verlaufszeile setzen (Befund 2).
2. Vor dem Merge lokal in Unity öffnen: Nova.Editor, Nova.Gameplay und Nova.Data.Tests müssen fehlerfrei kompilieren.
   Den Generator einmal laufen lassen (es darf kein `Assets/_Project/Data/Maps/` mehr entstehen) und die
   Data-EditMode-Tests ausführen. Ist das nicht geschehen, im PR „Unity nicht gelaufen“ stehen lassen. Das Restrisiko
   ist gering, weil alle Leser per Grep und GUID ausgeschlossen sind, ein Kompilierfehler im Editor-Assembly fiele in
   der CI aber nicht auf.
3. Die Mehrkarten-Richtung bestätigen. Der D-123-Entwurf legt fest, dass eine Wiedereinführung nur als engine-freie
   Kartendefinition nach G5 erfolgt. Das ist eine Inhaberentscheidung.
