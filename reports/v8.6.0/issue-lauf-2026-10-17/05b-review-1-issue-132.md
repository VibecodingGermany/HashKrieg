---
issue: 132
paket: P-132
reviewer: opus
runde: 1
verdict: APPROVED
datum: 2026-10-17
---
# Review Runde 1 — P-132: Beta-Pakete nur aus sauberem Baum

Unabhängiges Read-only-Review nach AGENTS.md §3. Ich habe keine Projektdatei
geändert. Geschrieben habe ich nur diesen Bericht und Wegwerf-Dateien unter
`scratchpad/build-132/review1/`.

## Was geprüft wurde

- `git status --short` und `git diff` im Worktree `chore-132` (Basis a6858d1),
  vollständig gelesen: `tools/packaging/build-mac.sh`, `build-linux.sh`,
  `README.md`, `docs/production/DecisionLog.md`, `CHANGELOG.md`, dazu der
  untrackte Builder-Bericht `05-issue-132-clean-beta-builds.md`.
- Abgleich gegen Issue #132, die Paketspezifikation `paket-P-132.json` und die
  Analyse samt Gegenprüfung `issue-132.json`.
- Schreibumfang: Geändert sind genau die fünf Dateien aus der Spezifikation plus
  der Bericht. Die vier Guard-Dateien sind unberührt
  (`git diff --stat -- tools/Nova.SimRunner.Tests/` ist leer). Es gibt keine
  C#-Änderung, also auch keine Determinismusfrage.
- Gegengelesen: `BuildCommitStamp.cs:72` nutzt `status --porcelain` ohne
  Zusatzflags. Die Aussage „deckungsgleich“ stimmt. `ArtAssetAutoSync.cs:95-96`
  und `:158-175` machen die Registry beim Import schmutzig, wie in der README
  beschrieben. `Builds/` ist gitignored (`.gitignore:118`).

## Nachgerechnete Ergebnisse (selbst gemessen)

| Prüfung | Befehl | Ergebnis |
|---|---|---|
| Simulationstests | `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release` | 778/778 bestanden, 0 Fehler |
| Doku-Links | `python3 .github/scripts/check_docs.py` | OK, 204 Markdown-Dateien und 5 Quality-JSONs |
| Baseline-Guard | `python3 .github/scripts/check_baseline_guard.py --self-test` | bestanden, inkl. Negativkontrolle |
| Syntax | `bash -n` auf beide Skripte | ok |

Der Builder meldet 203 statt 204 Markdown-Dateien. Den Unterschied macht sein
eigener Bericht. `check_docs.py` liest `git ls-files --others --exclude-standard`,
und `reports/` ist im Worktree tatsächlich nicht ignoriert (siehe Nebenbefund).
Vermutlich lief sein Check, bevor er den Bericht schrieb. Das ist erklärbar und
nicht unehrlich.

**Eigene Shell-Gegenprobe:** Wegwerf-Repo mit Bare-Origin unter
`scratchpad/build-132/review1/`, beide Skripte unverändert hineinkopiert, eine
fiktive Unity-Version. Ergebnisse:

- sauberer Baum, `--check-only`: beide Skripte Exit 0, „HEAD = origin/main“
- untrackte Datei, Vollaufruf: beide Exit 1 mit „Arbeitsbaum nicht sauber“, vor dem Schritt „Preflight“ und vor Unity
- `--allow-dirty --check-only` (Linux) und `--fast --check-only` (mac) bei unsauberem Baum: Exit 0, Stempel `<hash>-dirty`
- mac `--skip-build --allow-dirty` ohne Sidecar: Exit 1
- sauberer Baum, Linux-Vollaufruf: läuft durch die Prüfung und scheitert erst an „Unity … nicht gefunden“. Die Reihenfolge ist also belegt.
- Linux `--skip-build`: ohne Sidecar Exit 1. Mit passendem Sidecar und Fake-Player Exit 0. Das `tar.gz` enthält keine `.build-commit`, und der Baum bleibt sauber.
- Sidecar mit `state=dirty` bei sauberem Baum: Exit 1. Sidecar vom Vorgänger-Commit: Exit 1.
- `origin` auf einen nicht existierenden Pfad gesetzt: HINWEIS plus WARNUNG „HEAD ist nicht origin/main“, Exit 0
- `origin` entfernt: HINWEIS plus „origin/main unbekannt“, Exit 0

Nicht gelaufen (bei mir wie beim Builder): echter Unity-Build, Schreiben des
Sidecars nach einem echten Build, Signieren, Notarisieren und DMG, mac
`--skip-build` bis zum Ende, das Verhalten von ArtAssetAutoSync und
BuildCommitStamp nach echtem Import, Unity-EditMode- und PlayMode-Tests (keine
angelegt).

## Inhaltliche Bewertung

Die Umsetzung trifft das Zielbild der Spezifikation vollständig:

- Die Sperre steht vor Unity und gilt in beiden Skripten einheitlich.
- `--allow-dirty` behält den Stempel `-dirty`. `--fast` ist nur auf macOS ausgenommen.
- `--skip-build` prüft gegen einen Sidecar mit HEAD und Baumzustand.
- HEAD ≠ origin/main ist nur eine Warnung. Das Skript ist offline robust: `if !` und `|| true` greifen unter `set -e`, wie getestet.
- README 2.1.0 hat eine Verlaufszeile.
- Der D-124-Entwurf steht an der richtigen Stelle (nach D-114, vor „## Offene Punkte“). Er ist vollständig: Status Entwurf, Kontext, vier bewertete Alternativen, Entscheidung, Begründung, Konsequenzen mit Nicht-Zielen. Kopfversion und Verlauf des DecisionLog sind unberührt.
- Die CHANGELOG-Zeile steht unter `[Unreleased]` / `### Geändert` (Zeile 260 ff., vor `## [0.7.0]` in Zeile 2958) und nennt (#132).
- Die Docstrings zitieren (#132, D-124).
- Der Bericht weist alles Nicht-Gelaufene aus.

## Befunde

### hoch

Keine.

### mittel

1. **Kein Nach-Check nach dem Unity-Lauf** — `tools/packaging/build-mac.sh:146-160`, `tools/packaging/build-linux.sh:117-131`.
   - Was passiert: Sidecar, Dateiname, `Info.plist` und `NovaBuildCommit.txt` tragen den Zustand von *vor* Unity. Macht der Import den Baum schmutzig (Registry durch ArtAssetAutoSync, Re-Serialisierung), stempelt BuildCommitStamp im Player `<hash>-dirty`. Das äußere Etikett und der Sidecar sagen dagegen „clean“. Die Lobby vergleicht den inneren Stempel (D-094).
   - Bewertung: D-124 benennt das selbst als Konsequenz, deckt es aber nur über eine README-Anleitung ab. Es ist keine Regression, denn der Bestand rechnete ebenfalls vor Unity.
   - Vorschlag: nach erfolgreichem Unity-Build `git status --porcelain` erneut lesen. Ist der Baum von clean auf dirty gekippt, mit klarer Meldung abbrechen (oder mindestens `state=dirty` und `-dirty` setzen). Das kostet drei Zeilen und schließt die Lücke im Skript statt in der Doku.
2. **Bauort-Rezept ist unverifiziert und vermutlich unvollständig** — `tools/packaging/README.md:80-98`.
   - Was passiert: Das Rezept nennt nur `skip-worktree` auf `AssetMappingRegistry.asset`. Unter `Assets/_Project/Art/` sind aber 153 Dateien getrackt, darunter `PROVENANCE.json`, `.gitkeep` und Ordner-Metas. Der Art-Pass hat `PROVENANCE.json` im Hauptbaum bereits verändert (git status dort).
   - Folge: Ein Paket-Drop-in mit neueren Provenienzdateien oder neuen Ordnern macht den Beta-Worktree schmutzig, bevor Unity startet. Die Sperre greift dann, und der dokumentierte Weg führt nicht zum Paket.
   - Vorschlag: im README als „beim ersten echten Bau zu prüfen“ markieren oder den Schritt „Art-Paket einspielen, dann `--check-only`“ ergänzen.

### niedrig

3. **Veralteter Sidecar bei gescheitertem Neubau** — `build-mac.sh:150-160`, `build-linux.sh:121-131`. Schlägt Unity fehl, bleibt der Sidecar des vorigen Builds neben einer womöglich teilweise überschriebenen Ausgabe liegen. Ist HEAD gleich geblieben, akzeptiert `--skip-build` sie danach. Vorschlag: `rm -f "$SIDECAR"` direkt vor dem Unity-Aufruf.
4. **README nennt einen Wegwerf-Worktree** — `tools/packaging/README.md:83`. Dort heißt `main-baseline` „Dauer-Worktree“. Laut Gegenprüfung ist er ein Artefakt dieses Analyselaufs (detached a6858d1, kein `Library/`) und gehört nicht in öffentliche Doku. Vorschlag: den Halbsatz streichen.
5. **Doppelter Abschnittsbanner** — `build-mac.sh:55-56`. `# --- Preflight ---` steht direkt über `# --- Quellstand pruefen ---`. Der alte Banner ist verwaist, weil der Preflight jetzt erst ab Zeile 119 beginnt.
6. **Hilfebereiche** — `build-linux.sh:22`: `sed -n '2,12p'` lässt die neue Regelzeile 13 („Bei unsauberem Arbeitsbaum brechen … ab“) aus. `build-mac.sh:39`: `2,24p` endet mitten im Satz („dessen“), eine Zeile früher als vorher.
7. **Fetch ohne Zeitlimit** — `build-mac.sh:102`, `build-linux.sh:84`. `GIT_TERMINAL_PROMPT=0` deckt keine SSH-Passphrase und keinen hängenden Verbindungsaufbau ab. Vorschlag: `GIT_SSH_COMMAND='ssh -o BatchMode=yes -o ConnectTimeout=5'` setzen.

## Nebenbefunde (außerhalb des Pakets, nicht umsetzen)

- `.gitignore:26` lautet `reports/          # lokale …`. In `.gitignore` gibt es keine Inline-Kommentare, das Muster greift deshalb nicht. `git check-ignore` auf den Bericht liefert rc=1, und `reports/` erscheint als untrackt. Daher kommt auch die Zählung 203/204 in `check_docs.py`. Für dieses Paket ist das harmlos. Es betrifft aber die Annahme „reports/ ist gitignored“ des Laufs und jeden `git status --porcelain`-Check in einem Baum mit lokalen Reports.
- Die Hinweise des Builders zu `tools/build/build-mac.sh` (gitignorierte Kopie ohne Sperre), zu den untrackten kompilierenden Dateien im Art-Branch und zum Konflikt mit P-14 teile ich.

## Empfehlung für den Inhaber

**APPROVED.** Es gibt keinen Befund der Schwere hoch. Die Prüfungen sind grün
und selbst nachgerechnet. Der Schreibumfang ist eingehalten, und der Bericht
ist ehrlich.

- **Vor dem Merge:** Befund 1 (Nach-Check nach Unity) lohnt sich als kleine Nachbesserung, ebenso Befund 4 (eine README-Zeile). Beides blockiert nicht.
- **Nach dem Merge:** Befund 2 erledigt sich erst mit dem ersten echten Beta-Bau aus einem frischen Worktree mit Art-Paket. Diesen Bau und eine Badge-Prüfung (`BuildInfo.Commit` ohne `-dirty`) sollte der Inhaber einmal fahren, bevor er die Regel als abgenommen betrachtet.
- **Offene Inhaberfragen** aus dem Builder-Bericht, die bestehen bleiben:
  - Bleibt `--fast` ausgenommen?
  - Ist für das Release-DMG main Pflicht?
  - Für Teil 1 von #132: Liegt auf keinem Rechner mehr eine Kopie? Der Kommentar wird nur mit Freigabe gepostet.
