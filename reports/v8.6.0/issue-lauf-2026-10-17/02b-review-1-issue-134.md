---
issue: 134
paket: P-134
reviewer: opus
runde: 1
verdict: APPROVED
datum: 2026-10-17
---
# Review Runde 1: #134, ein produzierter Sammler wird mit Ernteauftrag geboren

Unabhängiges Read-only-Review nach AGENTS.md §3. Geprüft wurde der unkommittierte
Stand im Worktree `feat-134` (Branch `feat/134-harvester-born-harvesting`, Basis
`a6858d1`). Ich habe keine Projektdatei geändert. Geschrieben habe ich nur diesen
Bericht und Scratch-Kopien unter `scratchpad/build-134/review/`.

## Was geprüft wurde

- `git status --short` und `git diff` vollständig: 13 geänderte Dateien, dazu der
  untrackte Builder-Bericht. Abgeglichen mit Issue #134, der Paketspezifikation
  P-134 und den Belegen der Triage.
- Schreibumfang: Alle Dateien liegen im Umfang der Spezifikation. Einzige
  Ausnahme ist `Assets/Tests/EditMode/Simulation/EconomyIntegrationTests.cs`, der
  EditMode-Spiegel der .NET-Datei. Der Builder weist ihn offen aus, und die
  Spiegelregel deckt ihn. `RtsDeviceInput.cs`, `BuildMenuHud.cs`,
  `MatchFingerprint.cs`, `docs/README.md` und `ScopeLedger.md` sind unberührt.
- Die vier Guard-Dateien: `git diff --stat` darauf ist leer.
  `Determinism10000Tests.cs` pinnt keinen Hash, prüft aber `CommandKind.Harvest`
  im Strom (Zeilen 118 und 121). Er ist grün.
- Fachlich: Regel und Anker gegen `ConstructionSystem.GrantFoundingHarvester`
  (Zeilen 1050–1068) verglichen. `building.Transform` steht auf `origin + 1`
  (`SpawnBuildingEntity`, Zeile 1416), und `BuildingFootprintCells` ist 3,
  also `centre = 1`. Damit ist der Anker identisch mit `originX + centre`.
  `TryFindNearestField` (EconomySystem.cs:386) rechnet ganzzahlig mit
  Chebyshev-Distanz, überspringt erschöpfte Felder und löst Gleichstand zum
  niedrigeren Index auf.
- Determinismus: kein float/double in den geänderten Simulationsdateien. Das
  `HashSet<uint>` im Drehbuch nimmt nur `Add` auf und wird nie iteriert. Die
  KI-Liste wird in Entity-Index-Reihenfolge gebaut und danach sortiert. Es gibt
  keine Zeit- oder Zufallsquellen. `HarvestFieldId` ist schon Teil des
  Snapshots, das Layout ändert sich nicht.
- Client-Seite auf `main`: Die Eskorte in `RtsDeviceInput.cs` (ab Zeile 740)
  greift bei `HarvestFieldId != 0`, sobald der Sammler stillsteht. Der
  Sammelpunkt-Lauf und die anschließende Eskorte passen also zusammen. Das ist
  nur gelesen, nicht gespielt.

## Nachgerechnete Ergebnisse (selbst gemessen)

| Befehl | Ergebnis |
|---|---|
| `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release` im Worktree | 786/786 grün, 0 Fehler |
| `python3 .github/scripts/check_docs.py` | OK, 204 Markdown-Dateien und 5 Quality-JSONs, exit 0 |
| `python3 .github/scripts/check_baseline_guard.py --self-test` | passed, inklusive Negativkontrolle |
| `check_baseline_guard.py --changed-files <git diff --name-only> --labels <leer>` | „Baseline guard passed: the PR changes only one side of the separation.“, exit 0 |
| Gegenprobe A: Worktree-Kopie mit `SkirmishAiSystem.cs` von `a6858d1` (neue Produktion, alte KI-Regel) | 785/786. Rot ist nur `SkirmishAi_RetasksAHarvesterOrderedOntoAForeignField_AndItMines`. Der KI-Pin ist dort grün, steht also auf `0x7AF32DBA56E393E2` |
| Gegenprobe B: Worktree-Kopie mit `ProductionSystem.cs` von `a6858d1` (alte Produktion, r10) | 780/786. Rot sind der KI-Pin („But was 0x4A861D9FA2E57C6F“), 3 ProductionSystem-Tests und 2 EconomyIntegration-Tests |

Die Zahl 203 im Builder-Bericht für `check_docs` passt zu meiner 204:
`check_docs.py` zählt auch untrackte Dateien (`git ls-files --others
--exclude-standard`), und `reports/` wird nicht ignoriert (Nebenbefund 7 des
Builders, mit `git check-ignore` bestätigt). Der Builder hat vor seinem Bericht
gemessen. Die Basis 778 habe ich nicht gesondert auf `a6858d1` gemessen. Sie
ergibt sich aber aus 786 minus 8 neuen Tests (5 + 2 + 1, im Diff gezählt).

**Bericht ehrlich:** ja. Alle Zahlen, die ich nachgemessen habe, stimmen: 786,
die beiden Gegenproben 785 und 780, alter und neuer Pin, Tick 2726. Nicht
Gelaufenes ist ausgewiesen: EditMode, PlayMode und die Spielabnahme.

## Befunde

### Hoch

Keine.

### Mittel

1. **Kein RulesRevision-Bump bei geändertem Simulationsverhalten.** Betroffen
   sind `docs/production/DecisionLog.md` (D-121, Konsequenzen) und
   `Assets/_Project/Scripts/Simulation/Replays/MatchFingerprint.cs:127`
   (unverändert V3). Alte und neue Builds handshaken weiter. Sie laufen erst
   beim ersten produzierten Sammler auseinander und fallen dann als
   Relay-Desync auf. D-104 hat für einen vergleichbaren Fall bewusst eine
   Revision angehängt. Die Spezifikation verlangt ausdrücklich „kein Bump“,
   und D-121 nennt die Folge offen, deshalb blockiert das nicht. Der Inhaber
   sollte aber bewusst wählen: entweder im Merge-Fenster mit der RulesRevision
   V4 des Art-Branches mergen, wie der Builder vorschlägt, oder hier selbst V4
   anhängen.

### Niedrig

2. **CHANGELOG.md:261–276.** Der Absatz endet ohne Punkt („…als Relay-Desync
   auf“) und ist mit 16 Zeilen eher ein Bericht als ein kurzer Absatz.
   Vorschlag: Punkt setzen und die Pin-Messung dem PR-Text überlassen.
3. **tools/Nova.SimRunner.Tests/SkirmishAiTests.cs:623–630.** Der Kommentar
   direkt darüber sagt, ein Revisionssprung bei unverändertem Profil-Hash
   „would have meant a rule nobody can switch off“. r10 ist genau so ein Fall.
   Der r10-Zusatz geht darauf nicht ein. In der Sache ist das vertretbar, weil
   r10 einen Hänger behebt und keine Strategieregel einführt. Ein Halbsatz
   dazu würde den Widerspruch im Testkommentar auflösen.
4. **Assets/_Project/Scripts/AI.Data/AiBehaviorId.cs:170.** Der r9-Absatz
   sagt weiter, eine Partie mit nicht-neutraler Persönlichkeit sei „still r9“.
   Ab jetzt heißt das „r10“. Der Satz ist historisch gemeint, liest sich aber
   als Gegenwart.
5. **Testabdeckung des Spielerwegs.**
   `ProducedHarvester_DeliversCredits_WithoutAnyHarvestCommand` legt das Feld
   bewusst in Erntereichweite des Standard-Sammelpunkts (24,22 gegen 23,21).
   Damit beweist der Test den Geburtsauftrag und den Wirtschaftszyklus, aber
   nicht den Weg Sammelpunkt → entferntes Feld. Der hängt an der
   Client-Eskorte, und die ist ungetestet und ungespielt. Der Builder nennt
   das als Restrisiko. Kein Fehler, nur zur Einordnung des Nachweises.
6. **Veraltete Doku außerhalb des Diffs.** Nebenbefund 1 des Builders ist
   bestätigt: `ProductionSystem.cs:38` sagt „HQ produces Builder and
   Harvester“, seit D-077 stimmt das nicht mehr. Das gehört nicht in diesen PR.

## Spezifikationsabgleich

- ProductionSystem: Geburtsauftrag, gleiche Klemmung wie der Sammelpunkt, ohne
  Feld kein Auftrag, Klassendoku nachgezogen. Umgesetzt.
- SkirmishAiSystem: Beauftragt werden `!IsReturningCargo && HarvestFieldId !=
  ownFieldId`. Remarks und Schrittkommentar sind nachgezogen, die tote Liste
  `idleHarvesterRaws` ist entfernt. Umgesetzt.
- AiBehaviorId Revision 10 mit History-Absatz, Profil-Hash unverändert,
  Identitätspin `r10.1E6E7AE3`. Umgesetzt.
- Determinism10000Scenario: Jeder Sammler wird genau einmal beauftragt, über
  ein `HashSet` in `SlotState`. `IdleHarvesterRaws` ist ersetzt, die Doku
  angepasst, der Guard unberührt und grün. Umgesetzt. Neu wird jetzt auch der
  Gründungs-Sammler einmal beauftragt, den das alte Drehbuch ausließ. Das ist
  harmlos und deterministisch, die Replay-Wiedergabe nutzt den aufgezeichneten
  Strom.
- CanonicalAiOutcomeTests: Der Pin ist mit Vorwert, Vorvorwert und „last moved
  by #134“ nachgezogen. Der veraltete r7-Hinweis ist berichtigt, Tick 2726
  bleibt. Umgesetzt und von mir gegengemessen.
- Neue Tests der Spezifikation: alle vorhanden. Nächstes Feld, Gleichstand,
  erschöpftes Feld, kein Feld, Nicht-Sammler, Stop, Integration ohne
  Harvest-Befehl, KI-Regression. Die Tests nageln das Verhalten wirklich fest.
  Gegenprobe B lässt die Geburtsauftrags-Tests rot werden, Gegenprobe A den
  KI-Regressionstest.
- EditMode-Spiegel: per Diff geprüft, textgleich mit der .NET-Lane. Der
  Identitätspin fehlt im Spiegel, weil der EditMode-Bestand keinen hat.
  Nicht kompiliert und nicht gelaufen.
- D-121: Kopfzeile im Nachbarformat, Status „Entwurf …“, Kontext, vier
  bewertete Alternativen (darunter der Client-Weg `73f54b5`), Entscheidung,
  Begründung, Konsequenzen. Steht nach D-114 und vor „## Offene Punkte“.
  Kopfversion und Änderungsverlauf sind unberührt. Vollständig.
- CHANGELOG: ein Eintrag unter `[Unreleased]` → `### Geändert`, mit „(#134)“.
  Die Kategorie passt.

## Nicht gelaufen (auch in diesem Review)

- Unity-EditMode- und PlayMode-Tests. Keine Unity-Freigabe, und Unity läuft in
  keiner CI.
- Spielabnahme: zweiten Sammler bauen und beobachten, dass er ohne `H` erntet.
  Nicht gespielt.

## Empfehlung für den Inhaber

APPROVED. Die Änderung ist korrekt, deterministisch und durch Tests
festgenagelt, die bei Rücknahme rot werden. Der Bericht ist ehrlich. Vor dem
Merge sind zwei Dinge zu entscheiden:

1. Sim-Weg oder Client-Weg `73f54b5`. Bei Sim-Weg fällt der Client-Block in
   P-51 weg.
2. Merge-Fenster und Fingerprint (Befund 1). Mergen zusammen mit der
   RulesRevision V4 des Art-Branches, sonst lieber hier eine Revision anhängen.
   Wer als Zweiter mergt, misst den KI-Pin neu.

Die niedrigen Befunde 2 bis 4 lassen sich beim Commit ohne neue Messung
beheben. Im PR muss stehen: „nicht gespielt“ samt Restrisiko Eskorte, und
„EditMode nicht gelaufen“.
