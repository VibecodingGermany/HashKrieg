---
issue: 101
paket: P-101a
branch: fix/101-flanked-staging-commit-ring
agent: builder (opus)
datum: 2026-10-17
status: DONE
---
# #101 (Vorbedingung): Der flankierte Sammelpunkt liegt im Commit-Ring

## Aufgabe

D-114 versetzt den Sammelpunkt der Skirmish-KI je Persönlichkeit um ±10 Felder
seitlich. `SkirmishAiSystem.IsCommittedToTheWave` maß den Ring für „sammelt
noch / ist ausgerückt“ weiter mit `StagingDistanceCells + StagingToleranceCells`
= 16 ab dem HQ. Der versetzte Sammelpunkt liegt aber auf Chebyshev 22. Paket
P-101a: Defekt selbst nachweisen, dann den Ring um `|FlankOffsetCells|`
erweitern (weiter ab dem HQ gemessen), Docstring, `AiBehaviorId` r11, Tests in
beiden Lanes, D-127-Entwurf, Statusvermerk an D-114 und CHANGELOG.

Gestapelt auf `feat/134-harvester-born-harvesting` @ `dafc7db` (r10).

## Verifikation des Defekts (vor der Änderung)

1. **Tests zuerst, rot auf dem unveränderten Code.** Die neuen Tests in
   `SkirmishGoalTests` liefen gegen `dafc7db` plus nur die Tests:
   6 Fehler, 4 grün (Filter `~Flank`, 10 Tests). Die Meldungen:
   - `flank 10, tick 1120: 6 unit(s) counted as already out before any wave marched`
   - `flank -10, tick 1100: 1 unit(s) counted as already out …`
   - `flank 10: unit 1033 was sent to attack at tick 1120, before the first wave was ready at tick 1540`
   - `flank -10: unit 1038 was sent to attack at tick 1100, before … 1540`
   - `FlankedGatherersStillBreakOffToDefendTheBase(±10)`: es hielt nie jemand, die Welle lief zuerst.
   - Die Kontrolle `NoAttackBeforeWaveReady_WhenFlanked(0)` war grün.
2. **Eigene Probe** (Scratch, nicht im Repo; kompiliert die Quellen dieses
   Worktrees, kanonischer Seed, Ausgewogen, Layout 0, passiver Gegner, bis
   zur Entscheidung):

   | Flanke | Sammelpunkt | erste Welle | erster Angriff | max. „ausgerückt“ vor Welle | Goals vor der Welle | entschieden |
   |---|---|---|---|---|---|---|
   | −10 | (97,117) | 1540 | 1100 | 6 | Advance 113, Attack 91 | 2735 |
   | 0 | (107,107) | 1540 | 1540 | 0 | Advance 27, Hold 177 | 2726 |
   | +10 | (117,97) | 1540 | 1120 | 11 | Advance 33, Attack 171 | 2458 |

   Das bestätigt die Triage-Probe (171 bzw. 91 Attack, kein Hold).
3. **Anteil der Partien:** `AiPersonality.Derive(seed·φ+1, slot 1)` über 3.000
   Seeds: 2.000 geflankt, also zwei Drittel. Der kanonische Seed
   `0xDE7E000000010271` leitet `Rush, flank 0, layout 3, army delta -3` ab und
   ist nicht betroffen.

## Files Changed

- `Assets/_Project/Scripts/AI/SkirmishAiSystem.cs`: `IsCommittedToTheWave` rechnet `ring = StagingDistanceCells + Math.Abs(Personality.FlankOffsetCells) + StagingToleranceCells`. Der Docstring hat einen neuen Absatz (r11, #101, D-127) mit Befund, Chebyshev-Schranke (Versatz je Achse ≤ |flank|, Klemmen zieht nur zum HQ hin) und dem Hinweis, warum nicht relativ zum Sammelpunkt gemessen wird. Der `GetStagingCell`-Docstring verweist auf den mitwachsenden Ring.
- `Assets/_Project/Scripts/AI.Data/AiBehaviorId.cs`: `Revision` 10 → 11, mit r11-History-Absatz. Der Profil-Hash bleibt.
- `tools/Nova.SimRunner.Tests/SkirmishAiTests.cs`: Identitätspin `r10.1E6E7AE3` → `r11.1E6E7AE3` mit r11-Kommentar.
- `tools/Nova.SimRunner.Tests/SkirmishGoalTests.cs`: Neuer Block „The flanked staging cell (r11, #101, D-127)“ mit drei Tests (7 Fälle) und den Helfern `FlankedHost`, `RunToFirstWave`, `AnyWaveReady`, `FirstEngagingArmy`:
  - `FlankedStagingCellIsInsideTheRing_AndProducesHold(±10)`: Vorbedingung, dass der Sammelpunkt außerhalb des alten Rings liegt (> 16) und höchstens `12 + |flank|` vom HQ entfernt ist. Vor der ersten Welle ist `Committed == 0`, und es gibt mindestens ein `Hold`.
  - `NoAttackBeforeWaveReady_WhenFlanked(±10, 0)`: kein `Attack` vor dem ersten `WaveReady`, und an diesem Tick marschiert jemand. Flanke 0 ist die Kontrolle.
  - `FlankedGatherersStillBreakOffToDefendTheBase(±10)`: Ein Feind am HQ, solange am versetzten Sammelpunkt gehalten wird, erzeugt `DefendHome` und kein `Attack`.
- `Assets/Tests/EditMode/AI/SkirmishAiTests.cs`: Der Spiegel bekommt optionale Parameter `goalObserver`/`personality` an `BuildAiHost`/`BuildMatch`, wie in der .NET-Lane. Der Profilaufbau läuft über `.WithPersonality(personality)`; neutral ist das identisch. Neu sind ein `RecordingObserver` und die Tests `SkirmishAi_FlankedStagingCellIsInsideTheRing_AndProducesHold(±10)` und `SkirmishAi_NoAttackBeforeWaveReady_WhenFlanked(±10, 0)`, dazu die Helfer. Der Verteidigungstest ist nicht gespiegelt, weil der Spiegel keine Spawn-Helfer für Feinde hat.
- `docs/production/DecisionLog.md`: `### D-127 | Entwurf | KI-Strang (…)` steht nach D-121 und vor „## Offene Punkte“, mit vier bewerteten Alternativen. D-114 hat einen **Statusvermerk** unter seiner Status-Zeile; der Originaltext bleibt unverändert. Kopfversion und Verlaufstabelle sind unberührt.
- `CHANGELOG.md`: ein Eintrag unter `[Unreleased]` / `### Behoben`, „(Refs #101, D-127)“.
- `reports/v8.6.0/issue-lauf-2026-10-17/09-issue-101a-flanked-staging-ring.md`: dieser Bericht.

Guard-Dateien (`SnapshotGoldenBytesTests`, `CommandGoldenBytesTests`,
`SimRandomGoldenTests`, `Determinism10000Tests`) sind unberührt.
`CanonicalAiOutcomeTests.cs` ist unberührt.

## Tests

Umgebung: `DOTNET_ROOT="/Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/.dotnet"`.

| Befehl | Ergebnis |
|---|---|
| `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release` | **793/793 grün** (Basis 786 + 7 neue Fälle), 14 s |
| `python3 .github/scripts/check_docs.py` | Exit 0, „206 Markdown-Dateien und 5 Quality-JSONs geprüft“ (nach dem Anlegen dieses Berichts; davor 205). 13 bestehende Notices zu Status-Kopfzeilen, nicht von hier |
| `python3 .github/scripts/check_baseline_guard.py --self-test` | Exit 0, „Baseline guard self-test passed, including the required negative control.“ |

Gezielte Belege:
- `CanonicalAiOutcomeTests.CanonicalAiMatch_DecidesOnThePinnedTick_WithThePinnedEndState`
  ist grün und unverändert (Tick 2726, Endzustand `0x7AF32DBA56E393E2`). Die
  Probe zeigt für Flanke 0 vor und nach der Änderung denselben Hash.
- `AiPersonalityTests`: alle grün, auch `EveryOpening_FlankAndFaction_FinishesTheMatch`
  und `SeedsWithDifferentOpenings_PlayDifferentMatches`. Kein Budget geändert.
  Entscheidungsticks der Matrix (24 Partien), vor → nach:
  Spanne 1.987–2.982 → 2.208–2.871. Flanke +10 gegenüber Flanke 0 vorher
  −458 bis −165 Ticks (stückweiser Angriff gegen wehrlosen Gegner), nachher
  +11 bis +159 Ticks über alle Flanken. Gemessen durch vorübergehendes
  Herausnehmen der einen Ringzeile im Worktree, Test, Wiederherstellen
  (Wiederherstellung per `git diff` geprüft).
- Probe nach der Änderung: Flanke −10 Hold 166, kein Attack vor 1540,
  entschieden 2760. Flanke +10 Hold 159, kein Attack vor 1540, entschieden 2739.
  „Ausgerückt“ vor der Welle in beiden Fällen 0.

**EditMode-Spiegel:** Die Quelldatei `Assets/Tests/EditMode/AI/SkirmishAiTests.cs`
wurde zusätzlich headless in einem Scratch-Projekt (NUnit 4.2.2, net8.0, dieselben
Sim-/AI-Quellen) kompiliert und ausgeführt: 12/12 grün (7 bestehende + 5 neue
Fälle). Das ist **kein Unity-Lauf**.

**Nicht gelaufen:**
- Unity-EditMode-Tests im Editor (`Nova.AI.Tests`), keine CI-Lizenz und kein
  Editor-Lauf in diesem Paket.
- Unity-PlayMode-Tests.
- Keine gespielte Runde. Belegt ist nur headless gegen einen passiven Gegner.

## Entscheidung

D-127 (Entwurf): Alternative 1, der Ring wächst um den Flankenbetrag und wird
weiter ab dem HQ gemessen. Abgelehnt: die Messung relativ zum Sammelpunkt (das
dokumentierte 11.000-Tick-Pendeln), die Flanke zu begrenzen (ändert D-114, und
die gekoppelte Ungleichung reißt beim nächsten Profilwechsel) und den Ring aus
dem tatsächlichen Sammelpunkt abzuleiten (gleichwertig, aber eine zweite
Herleitung ohne Gewinn). D-114 ist per Statusvermerk berichtigt: Die Konsequenz
„zwischen rund 2.000 und 3.000 Ticks“ maß den Defekt mit.

## Nebenbefunde

- **Die Docstring-Zeile in `CanonicalAiOutcomeTests.cs` (:58) ist veraltet.**
  Dort steht „AiBehaviorId is r10.1E6E7AE3.“ Der Pin selbst bewegt sich nicht,
  die Kennung ist jetzt aber r11. P-134 hatte die Zeile wie einen aktuellen
  Zeiger berichtigt (r7 → r10). Die Datei gehört dem Netzstrang bzw. P-42 und
  liegt nicht im Schreibumfang. Nicht geändert; Vorschlag: P-42 zieht die Zeile
  mit nach.
- Gegen einen passiven Gegner ist der Defekt *schneller*. Geflankte Partien
  entschieden bis zu 458 Ticks früher, weil die Einzelangriffe auf keinen
  Widerstand treffen. Die headless Matrix hat den Fehler deshalb als „mehr
  Vielfalt“ gezeigt statt als Defekt. Ein Gegner, der zurückschießt, fehlt in
  `AiPersonalityTests`.
- `SkirmishGoalTests.GatheringHost` und mehrere Bestandstests rechnen den Ring
  als `StagingDistanceCells + StagingToleranceCells`. Das stimmt, weil sie mit
  neutraler Persönlichkeit laufen. Mit Flanke müsste dort der Betrag dazu.

## Offene Fragen

- Soll für den Defekt ein eigenes GitHub-Issue angelegt werden? Laut Paket
  nur mit Freigabe; der PR trägt „Refs #101“.
- D-127 ist vorläufig. Die Endnummer wird beim Merge vergeben.

## Commit-Titel

`fix(ai): widen the wave commit ring by the staging flank offset`

## PR-Beschreibung (Entwurf)

**fix(ai): widen the wave commit ring by the staging flank offset**

Refs #101 (Vorbedingung, Paket P-101a). Gestapelt auf #134 (`feat/134-harvester-born-harvesting`).

**Was:** `SkirmishAiSystem.IsCommittedToTheWave` misst den Ring jetzt mit
`StagingDistanceCells + |Personality.FlankOffsetCells| + StagingToleranceCells`
ab dem HQ. `AiBehaviorId` r11 (`r11.1E6E7AE3`), Profil-Hash unverändert.

**Warum:** Der D-114-Flankenversatz ±10 legt den Sammelpunkt auf Chebyshev 22
vom HQ, der Ring maß 16. Jede ankommende Einheit galt als ausgerückt: kein
`Hold`, Einzelangriffe ab Tick 1.100/1.120 statt der vollen Welle ab 1.540,
und `DefendHome` fand keine Wartenden. Das betrifft zwei Drittel der
Menüpartien. Messung relativ zum Sammelpunkt ist bewusst verworfen (siehe
Docstring und D-127).

**Betroffen:** `Scripts/AI/SkirmishAiSystem.cs`, `Scripts/AI.Data/AiBehaviorId.cs`,
`SkirmishAiTests` (Pin), `SkirmishGoalTests` (+3 Tests, 7 Fälle), EditMode-Spiegel
(+2 Tests, 5 Fälle), `DecisionLog` (D-127-Entwurf, Statusvermerk D-114), `CHANGELOG`.

**Baselines:** `CanonicalAiOutcomeTests` unverändert grün (neutrale
Persönlichkeit ohne Flanke: Tick 2726, `0x7AF32DBA56E393E2`). Kein Guard
berührt. Geflankte Partien entscheiden anders. Die
`AiPersonalityTests`-Matrix verschiebt sich von 1.987–2.982 auf 2.208–2.871
Ticks und bleibt grün.

**Changelog:** `[Unreleased]` / Behoben: „Die KI sammelt auch am seitlich
versetzten Sammelpunkt (Refs #101, D-127).“

**Nachweise:**
- `dotnet test tools/Nova.SimRunner.Tests -c Release`: 793/793 grün.
- `check_docs.py`: Exit 0. `check_baseline_guard.py --self-test`: Exit 0.
- Die neuen Tests waren vor der Änderung rot (6 von 10 im Filter, die
  Flanke-0-Kontrolle grün).

**Nicht gelaufen:** Unity-EditMode und PlayMode (keine CI-Lizenz, kein
Editor-Lauf). Die Spiegelquelle ist nur headless mit NUnit kompiliert und
ausgeführt (12/12), das ersetzt den Unity-Lauf nicht.

**Nicht gespielt.** Grund: headless Paket ohne Editor. Ersatznachweis: die
Goal-Aufzeichnung über `IAiGoalObserver` (Hold statt Attack, Committed 0 vor
der Welle, DefendHome am HQ) und die Personality-Matrix. Restrisiko: Wie die
volle Welle vom versetzten Sammelpunkt gegen einen zurückschießenden Menschen
wirkt, ist ungemessen. Ein Ring von 26 lässt ausrückende Einheiten etwas länger
als „sammelnd“ gelten (dieselbe Spanne von vier Feldern hinter dem Sammelpunkt
wie ungeflankt). Nicht spielerisch abgenommen, kein Meilenstein-Nachweis.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
