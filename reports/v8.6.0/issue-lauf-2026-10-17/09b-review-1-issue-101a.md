---
issue: 101
paket: P-101a
reviewer: opus
runde: 1
verdict: APPROVED
datum: 2026-10-17
---
# Review Runde 1: #101 (P-101a), der Commit-Ring wächst um den Flankenversatz

Unabhängiges Read-only-Review nach AGENTS.md §3. Geprüft wurde der unkommittierte
Stand im Worktree `fix-101a` (Branch `fix/101-flanked-staging-commit-ring`,
gestapelt auf `feat/134-harvester-born-harvesting` @ `dafc7db`). Ich habe keine
Projektdatei geändert. Geschrieben habe ich nur diesen Bericht und eine
Scratch-Kopie unter `scratchpad/review101a/` für die Negativkontrolle.

## Was geprüft wurde

- `git status --short` und `git diff dafc7db` vollständig: 7 geänderte Dateien,
  dazu der untrackte Builder-Bericht. `git diff --check` sauber. Abgeglichen mit
  Issue #101, Paketspezifikation P-101a und der Triage.
- Guard-Dateien (`SnapshotGoldenBytesTests`, `CommandGoldenBytesTests`,
  `SimRandomGoldenTests`, `Determinism10000Tests`) und `CanonicalAiOutcomeTests.cs`:
  kein Diff gegen `dafc7db`.

## Ursache und Fix am Code

- `GetStagingCell` (`SkirmishAiSystem.cs:966-992`) verschiebt die Zelle um
  `(-dy·flank/span, dx·flank/span)`. `IsCommittedToTheWave` (`:1226-1236`) maß
  vorher `StagingDistanceCells + StagingToleranceCells` = 16. Auf der kanonischen
  Karte liegt die geflankte Zelle bei (117,97) bzw. (97,117), Chebyshev 22 vom
  HQ (119,119). Die Ursache stimmt.
- Der Fix bleibt HQ-relativ. Der Ring ist jetzt
  `StagingDistanceCells + |FlankOffsetCells| + StagingToleranceCells`. Die
  Schranke im Docstring habe ich nachgerechnet. Der Grundversatz ist je Achse
  höchstens `distance`, die Flanke je Achse höchstens `|flank|`, weil
  `|dx|,|dy| ≤ span` gilt. Clamp ist 1-Lipschitz und lässt die HQ-Zelle fest,
  also ist der Abstand höchstens `distance + |flank|`. Auf der kanonischen Karte
  wird die Schranke genau erreicht (22). Damit bleibt dieselbe Toleranz von vier
  Feldern wie ungeflankt.
- **Determinismus:** nur `int`-Arithmetik, `Math.Abs` auf {−10, 0, 10}, kein
  Überlauf, keine Iteration, keine float-Werte. `Personality` ist eine
  Konstruktionskonstante und liegt nicht im Zustand.
- **Nebenwirkung, geprüft:** Der Ring speist das Wellenzählen und
  `ResolveGoal`. Dort hängen `DefendHome` und `IsFitToMarch` davon ab.
  Ungeflankt ist das Verhalten bitgleich. Geflankt gelten Einheiten bis 26
  Felder als sammelnd. Auf der diagonalen Karte wächst der Chebyshev-Abstand
  beim Ausrücken genauso wie ungeflankt. Ein Pendeln wie in V002 zeigen weder
  die Matrix noch die neuen Tests.

## Selbst gemessen

| Befehl | Ergebnis |
|---|---|
| `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release` (Worktree) | **793/793 grün**, 17 s |
| `python3 .github/scripts/check_docs.py` | Exit 0, „206 Markdown-Dateien und 5 Quality-JSONs geprüft“ |
| `python3 .github/scripts/check_baseline_guard.py --self-test` | Exit 0, „passed, including the required negative control“ |
| **Negativkontrolle:** Scratch-Kopie der Quellen, nur die Ringzeile auf den alten Stand gesetzt, voller Lauf | **6 rot / 787 grün**: alle ±10-Fälle der drei neuen Tests. Die Flanke-0-Kontrolle, der r11-Pin und `CanonicalAiOutcomeTests` bleiben grün |

Fehlermeldungen ohne Fix: `flank 10, tick 1120: 6 unit(s) counted as already
out`, `flank -10, tick 1100: 1 unit(s) …`, `unit 1033 was sent to attack at tick
1120, before the first wave was ready at tick 1540` (−10: unit 1038, Tick 1100).
Der Verteidigungstest scheitert mit „the wave marched before anybody held“.
Alle sechs Fehler haben also die richtige Ursache.

Die Matrix aus `EveryOpening_FlankAndFaction_FinishesTheMatch` habe ich in beiden
Bäumen aus den `[personality]`-Zeilen gelesen:
- Mit Fix: 2.208–2.871.
- Ohne Fix: 1.987–2.982. Das untere Ende ist Alliance Rush +10.
- Flanke +10 gegenüber 0 ohne Fix: 165 bis 458 Ticks früher.
- Mit Fix liegen alle Flanken 11 bis 159 Ticks hinter Flanke 0.

Alle Zahlen in D-127, im D-114-Statusvermerk und im Bericht stimmen mit dieser
Messung überein. `CanonicalAiOutcomeTests` (Tick 2726, `0x7AF32DBA56E393E2`)
ist in beiden Bäumen grün.

Nicht selbst nachgemessen habe ich:
- die Hold- und Attack-Zählungen 171, 91, 159, 166 und 177 aus der Builder-Probe
- den Anteil „2.000 von 3.000 Seeds geflankt“

Die Testmeldungen stützen die Zählungen qualitativ.

## Tests, Pins, Doku

- **Neue Tests:** 3 Tests mit 7 Fällen in `SkirmishGoalTests`. Sie halten fest:
  - Die Vorbedingung: Die Zelle liegt außerhalb des alten Rings und innerhalb
    von `12 + |flank|`.
  - `Committed == 0` vor der ersten Welle und mindestens ein `Hold`.
  - Kein `Attack` vor `WaveReady`, mit Flanke 0 als Kontrolle.
  - Ein Angriff am HQ erzeugt `DefendHome` und kein `Attack`.

  Ohne den Fix sind sie rot, siehe oben.
- **EditMode-Spiegel:** Signatur und Typen habe ich gegen
  `SkirmishAiSystem`-Konstruktor, `AiFactionProfile.WithPersonality` und die
  asmdef-Referenz `Nova.AI.Data` geprüft. Sie passen. Ausgeführt habe ich den
  Spiegel nicht. Die Behauptung des Builders „12/12 headless“ habe ich nicht
  nachvollzogen. Im Bericht und im PR-Text ist er korrekt als „kein Unity-Lauf“
  ausgewiesen.
- **AiBehaviorId:** `Revision` 10→11. Der r11-Absatz folgt dem r10-Muster. Der
  Profil-Hash bleibt `1E6E7AE3`, weil keine Profilzahl dazukommt. Der Pin in
  `SkirmishAiTests` steht auf `r11.1E6E7AE3`, mit Kommentar.
- **DecisionLog:**
  - `### D-127 | Entwurf | …` steht nach D-121 und vor „## Offene Punkte“.
  - Die Status-Zeile ist wie gefordert. Kontext, vier bewertete Alternativen
    (die drei geforderten und eine Ableitungsvariante), Entscheidung, Begründung
    und Konsequenzen sind vorhanden.
  - D-114 ist nur per eingefügtem Statusvermerk berichtigt, der Originaltext ist
    unverändert.
  - Kopfversion und Verlaufstabelle sind nicht berührt.
- **CHANGELOG:** Der Eintrag steht unter `[Unreleased]` / `### Behoben`
  (Zeile 442) mit „(Refs #101, D-127)“.
- **Bericht:** Er ist ehrlich. Das Nicht-Gelaufene ist vollständig genannt:
  Unity EditMode und PlayMode, keine gespielte Runde, keine CI. Dazu kommen
  Restrisiko und „nicht spielerisch abgenommen“. Das vorübergehende Herausnehmen
  der Ringzeile im Worktree ist offen genannt, und der Endstand enthält die
  Zeile.

## Befunde

Keiner der Schwere hoch oder mittel.

1. **niedrig:** `tools/Nova.SimRunner.Tests/CanonicalAiOutcomeTests.cs:58` sagt
   weiter „AiBehaviorId is r10.1E6E7AE3“. Die Datei liegt außerhalb des
   Schreibumfangs. Der Builder nennt das selbst als offene Frage. Vorschlag: P-42
   zieht die Zeile beim nächsten Anfassen auf r11 nach, oder sie wird als
   Momentaufnahme („zuletzt bewegt bei r10“) formuliert.
2. **niedrig:** Im EditMode-Spiegel steht `using System;` hinter
   `using System.Collections.Generic;`. Vorschlag: alphabetisch sortieren.
3. **niedrig:** Der CHANGELOG-Eintrag hat acht Zeilen, die Vorgabe sagte „eine
   Zeile“. Er passt aber zum Stil der Nachbareinträge. Vorschlag: so lassen oder
   auf zwei bis drei Zeilen kürzen.
4. **niedrig:** Die Probezahlen (Hold- und Attack-Zählungen, 2.000/3.000 Seeds)
   habe ich nicht unabhängig nachgemessen. Vorschlag: Die Probe als Testausgabe
   oder Skript nachvollziehbar machen, falls die Zahlen in D-127 verbindlich
   zitiert bleiben sollen.

## Urteil

**APPROVED.** Die Ursache ist am Code belegt. Der Fix ist minimal, misst weiter
ab dem HQ und ist deterministisch. Die neuen Tests halten das Verhalten fest und
sind ohne Fix nachweislich rot. Die Pins sind konsistent, und alle Zahlen habe
ich selbst gemessen und bestätigt. Offen bleiben nur die im PR ausgewiesenen
Punkte: Unity-Lauf, gespielte Runde und CI.
