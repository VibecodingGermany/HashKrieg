---
issue: 138
paket: P-138
branch: fix/138-freeze-rules-revisions
reviewer: opus
runde: 1
datum: 2026-10-17
verdict: APPROVED
---
# Review 1 zu #138: Regelrevisionen als Literalzeilen eingefroren

Unabhängiges Read-only-Review nach AGENTS.md §3. Ich habe keine Projektdatei
geändert. Hilfsprogramme und Mutationsproben liefen in einer Kopie unter
`scratchpad/build-138/review-hash` und `scratchpad/build-138/review-mut`,
nicht im Worktree.

## Was geprüft wurde

- `git status --short` und `git diff` vollständig: geändert sind genau die fünf
  Dateien aus dem Schreibumfang der Paketspezifikation (`MatchFingerprint.cs`,
  `MatchFingerprintTests.cs`, `MatchFingerprintV1Tests.cs`, `DecisionLog.md`,
  `CHANGELOG.md`), dazu untrackt der Builder-Bericht. Die vier Guard-Dateien
  (D-091) sind unberührt, `git status` meldet für sie nichts.
- Abgleich mit Issue #138, Triage und Paketspezifikation P-138: alle neun
  Schritte und alle vier geforderten Tests sind umgesetzt.
- Bytestrom: `HashRulesRow` schreibt Stub-Tag 1, Feldzahl `row.Length + 1`
  (V1 4+1 = 5, V2 6+1 = 7, V3 13+1 = 14), Feld 1 als u16, dann Tag 2/3 als i64
  und Tag 4–14 als i32. Das entspricht dem alten Writer auf `a6858d1`.
- Historische Werte per `git grep <commit>` nachgesehen: `e1970a9` 2000/2000/25/10,
  `8512e9f` zusätzlich 10/5, `4cdcf07` 2000/…/3/30/8/2/1/3/2, `e4faa75` HQ 3000.
  Die Literalzeilen V1/V2/V3 stimmen damit überein. Commitdaten:
  `4cdcf07` 2026-08-10, `e4faa75` 2026-08-31, passend zur benannten Lücke.
- Determinismus: keine float/double, keine Zeit- oder Zufallsquelle, keine
  Dictionary/HashSet-Iteration. `List<string>` und String-Interpolation kommen
  nur im Diagnosepfad `DescribeCurrentRulesDrift` vor, nicht im Hash.
- Statische Initialisierung: In `MatchFingerprint` gibt es nur die drei neuen
  `static readonly`-Zeilen; kein früherer statischer Initialisierer ruft den
  Regelhash auf. Es gibt also kein Null-Risiko durch die Textreihenfolge.
- Aufrufer: Nur `MatchBootstrap` und `Determinism10000Scenario` rufen produktiv
  `ComputeCurrentRulesHash64()` auf. `ComputeRulesHash64(V1/V2)` nutzen nur
  Replay- und Lockstep-Tests. Die Aussage im D-120-Kontext stimmt.
- EditMode-Spiegel: Per `diff` ist der neue Block textgleich zur .NET-Lane bis
  auf `ResolveMatchFingerprintSource` (`UnityEngine.Application.dataPath`, wie
  in `NoFloatInSimulationTests`). Die asmdef `Nova.Simulation.Tests` hat
  Engine-Referenzen. Gelesen, nicht kompiliert.
- Art-Strang: `git diff` im Hauptarbeitsbaum lesend angesehen. V4 liest dort
  `Movement.MovementSpacing.MaxRadius/LargeRadius.RawValue` (Typ `int`). Damit
  ist die Nachzieh-Anleitung im Builder-Bericht (`RulesField.Int32(15/16, …)`
  mit Literal in der Frozen-Zeile, Live-Lesung in `BuildLiveCurrentRulesRow`)
  typkorrekt und vollständig.
- DecisionLog: D-120 steht direkt nach D-114 und vor „## Offene Punkte“. Er hat
  Status „Entwurf“, Kontext, fünf bewertete Alternativen, Entscheidung,
  Begründung und Konsequenzen. Kopfversion und Änderungsverlauf sind unberührt.
  D-104 Punkt 8 (append-only `RulesRevisionV3`) und Q-047 existieren. Der Link
  auf `hashkrieg/13-15_Parallelbetrieb.md` funktioniert, die Zeile 85 dort
  sagt „niemand ohne D-ID“.
- CHANGELOG: Ein Absatz unter `## [Unreleased]` → `### Behoben` mit
  „(#138, D-120-Entwurf)“, ohne Versionsüberschrift.

## Nachgerechnete Ergebnisse

| Prüfung | Befehl | Ergebnis (selbst gemessen) |
|---|---|---|
| Simulationstests | `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release` | **781/781 grün**, 0 Fehler, 14 s |
| Doku-Links | `python3 .github/scripts/check_docs.py` | Exit 0, „OK: 204 Markdown-Dateien …“. Der Builder meldete 203; der Unterschied ist sein eigener, inzwischen angelegter Bericht, denn das Skript zählt auch untrackte `.md` |
| Baseline-Guard | `python3 .github/scripts/check_baseline_guard.py --self-test` | „passed, including the required negative control“ |
| Baseline-Guard gegen den realen Diff | `--changed-files <git diff --name-only> --labels <leer>` | „Baseline guard passed: the PR changes only one side of the separation.“ (lief beim Builder nicht) |

**Hashes**, gemessen mit einem eigenen Scratch-Programm gegen die gebaute
`Nova.SimRunner.Tests.dll`, zusätzlich mit einer **unabhängigen**
Nachimplementierung des alten Schreiblayouts über `SimHashWriter` und
expliziten Werten:

| Größe | neue API | unabhängig, altes Layout |
|---|---|---|
| Rev 1 (HQ 2000) | `0x531CE8F614A16CB5` | `0x531CE8F614A16CB5` |
| Rev 2 (HQ 2000) | `0x07725EA26668C9F8` | `0x07725EA26668C9F8` |
| Rev 3 (HQ 3000) | `0xD1B683837D12FCEF` | `0xD1B683837D12FCEF` |
| aktuell | `0xD1B683837D12FCEF` | – |
| Drift-Zeilen | 0 | – |
| Rev 3 mit HQ 2000 (Lücke) | – | `0x05CCA8475789AD4A` |
| Rev 1/2 mit HQ 3000 (alte Neu-Pins) | – | `0x1163158B8146B168` / `0x259E6977B751109B` |

Der aktuelle Regelhash bewegt sich nicht. Die Rev-1/2-Pins kehren bitgenau zu
den Originalwerten zurück.

**Mutationsproben**, in einer Kopie der Quellen im Scratchpad, nicht im Worktree:

- A: Live-`HqBaseCapacityAE` 3000 → 4000. Gefilterter Lauf
  (`MatchFingerprintTests|ReplayTests|LockstepNetworkTests`): **103/105**. Rot
  werden genau `CurrentLiveRules_MatchTheFrozenCurrentRevision` mit
  „revision 3 field 2 (HqBaseCapacityAE): frozen 3000 (i64, tag 2), live 4000
  (i64, tag 2)“ samt Append-Hinweis und
  `CurrentRulesHash_MovesPastRevisionTwo…`. Die Rev-1/2/3-Pins bleiben grün.
  Die Behauptung des Builders ist bestätigt.
- B: Ein Literal der V1-Zeile durch `EconomySystem.ExcessDecayPercent`
  ersetzt. `FrozenRevisions_ReadNoLiveConstants` wird rot (Expected 23, But
  was 22), 11/12. Bestätigt.
- C (eigene Probe, der verbotene Weg): Frozen-V3-Zeile **und** Live-Konstante
  gemeinsam auf 4000 gesetzt, also Zeile editieren statt Revision anhängen. Der
  Stolperdraht bleibt erwartungsgemäß grün, aber
  `RulesRevisionThree_FrozenLiteral_IsTheCurrentStream` und der Current-Pin
  werden rot („revision 3 canonical stream is frozen“), 10/12. Damit ist auch
  dieser Umweg abgesichert, solange niemand bewusst den Pin neu setzt. Das
  verbietet D-120 Punkt 4.

**Nicht gelaufen** (beim Builder und bei mir): der Unity-EditMode-Spiegel
`MatchFingerprintV1Tests.cs`, weil es keine Unity-CI gibt und kein Unity-Start
vorgesehen war. Eine Spielrunde gab es ebenfalls nicht; das ist vertretbar,
weil der aktuelle `RulesHash64` bitgleich bleibt und die Simulation unverändert
ist.

## Befunde

### hoch

Keine.

### mittel

Keine.

### niedrig

1. **D-120-Kontext überzeichnet den Kimi-Bericht**
   (`docs/production/DecisionLog.md:3689-3692`, gleichlautend im
   Builder-Bericht unter „Nebenbefunde“). Dort steht, unter dem alten Code
   „stimmte beides nicht“. `reports/v8.6.0/sprint-23/13-kimi-reparatur.md`
   entstand aber mit `c1a5ebf` (2026-08-31 10:29), also **vor** `e4faa75`
   (10:42). Zu diesem Zeitpunkt waren die Rev-1/2-Pins `0x531…`/`0x077…` und
   der Current-Pin `0x05CC…` korrekt. Die Aussagen waren nicht falsch, sondern
   strukturell ungesichert und wurden 13 Minuten später durch #131 überholt.
   Vorschlag: „überholt durch #131; unter dem alten Code nicht dauerhaft
   gesichert“.
2. **Zukunftsfestigkeit der Texte für V4**
   (`Assets/_Project/Scripts/Simulation/Replays/MatchFingerprint.cs:293-306`,
   `tools/Nova.SimRunner.Tests/MatchFingerprintTests.cs:108`). Der Docstring
   von `ComputeCurrentRulesHash64` beschreibt fest „Revision 3 retains …“, und
   der Testname `…_IsTheCurrentStream` wird mit V4 falsch. Der Builder nennt
   den Umbenennungsbedarf selbst in der Art-Anleitung. Handlungsbedarf besteht
   jetzt keiner; beim V4-Anhängen mitziehen.
3. **Frozen-Zeilen sind veränderliche Arrays**
   (`MatchFingerprint.cs:417-468`). `private static readonly RulesField[]`
   schützt nur die Referenz, nicht den Inhalt. Die Felder sind privat und
   werden nur gelesen, deshalb gibt es heute kein Risiko. Wer mehr Härte will,
   gibt `GetFrozenRulesRow` als `IReadOnlyList<RulesField>` zurück. Optional.
4. **Quelltextwächter ignoriert Blockkommentare** (`MatchFingerprintTests.cs`,
   `FrozenRevisions_ReadNoLiveConstants`). Entfernt werden nur `//`-Kommentare.
   Ein `/* … */` mit Memberzugriff im Bereich würde einen falschen Alarm
   auslösen, aber nie einen Defekt verdecken: Die Literalprüfung verlangt das
   Literal direkt vor `)`. Kein Handlungsbedarf.
5. **Hinweis zur Annahme im Auftrag:** `reports/` ist in diesem Repo nicht
   gitignored (`git ls-files reports` liefert getrackte Berichte). Der Builder
   hat das korrekt gemeldet. Der Integrator braucht kein `-f`.

## Ehrlichkeit des Builder-Berichts

Alle Zahlen habe ich nachgerechnet, und sie stimmen: 781/781, die Hashes, beide
Mutationsproben einschließlich 103/105 und 22/23. Die Zählung von
`check_docs.py` (203) passt zum Zeitpunkt vor dem Anlegen seines Berichts.
Nicht-Gelaufenes ist ausgewiesen: Unity-Spiegel, Baseline-Guard nur als
Self-Test, keine Spielrunde. Der Bericht hat alle geforderten Abschnitte. Die
Art-Nachzieh-Anleitung ist konkret und typkorrekt.

## Empfehlung für den Inhaber

**APPROVED.** Die Änderung erfüllt #138 vollständig und lässt den aktuellen
Regelhash bitgleich. Die drei Pins sind nachgemessen. Stolperdraht und
Quelltextwächter schlagen nachweislich an. Vor dem Merge bleiben die
Inhaberentscheidungen aus dem Builder-Bericht offen:

1. D-120 freigeben (Option B, append-only Literalzeilen).
2. Entscheiden, ob reine Wertrevisionen durch D-120 stehend freigegeben sind.
   Meine Empfehlung: ja, solange der PR die neue Revision im CHANGELOG nennt;
   eine eigene D-ID nur bei neuen Feldern.
3. Reihenfolge: #138 vor dem Art-Strang. Die Anleitung im Builder-Bericht passt
   zum aktuellen Art-Diff.
4. Die Lücke `0x05CCA8475789AD4A` bestätigen.

Der Unity-EditMode-Spiegel ist nicht gelaufen und sollte beim nächsten
Unity-Start mitlaufen. Befund 1 kann der Integrator beim Merge im D-120-Text
glätten; das ist keine Merge-Bedingung.
