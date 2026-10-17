---
issue: 138
paket: P-138
branch: fix/138-freeze-rules-revisions
agent: builder (opus)
datum: 2026-10-17
status: DONE
---
# Historische Regelrevisionen als Literalzeilen eingefroren, Stolperdraht für die aktuelle Revision

## Aufgabe

`MatchFingerprint.ComputeRulesHash64(rev)` hashte für jede Revision die heutigen
Konstanten; nur die Feldzahl war historisch. #131 (`e4faa75`) hat dadurch die
Identität der Revisionen 1 und 2 und ohne Revisionssprung auch die von Revision 3
verschoben. Umgesetzt ist Option B aus der Triage: Jede Revision trägt eine
eingefrorene Literalzeile, nur `ComputeCurrentRulesHash64` liest live, und ein
Stolperdraht meldet jede Abweichung der Live-Konstanten von der Literalzeile der
aktuellen Revision. Dazu kommen der D-120-Entwurf und ein Changelog-Eintrag.

## Was geändert wurde — Files Changed

- `Assets/_Project/Scripts/Simulation/Replays/MatchFingerprint.cs`: Die Regelidentität ist jetzt zeilenbasiert. Neu sind die private Struktur `RulesField` (Tag, Breite i32/i64, Wert, diagnostischer Name) und die Schreibroutine `HashRulesRow`; der Bytestrom ist unverändert (Stub-Tag 1, Feldzahl = Zeilenlänge + 1, Feld 1 = Revision als u16, dann Tag + Wert je Feld). Im markierten Bereich `// <frozen-rules-rows>` liegen die drei Literalzeilen `FrozenRulesRowV1/V2/V3` und `GetFrozenRulesRow`; unbekannte Revisionen werfen weiter. `BuildLiveCurrentRulesRow` ist die einzige Stelle, die die lebenden Konstanten liest. Neu ist auch die öffentliche Methode `DescribeCurrentRulesDrift()` als Feld-für-Feld-Stolperdraht: Ein Testzugriff ist nötig, und es gibt kein `InternalsVisibleTo`. Berichtigt sind die Docstrings von `RulesRevisionV2` (hieß noch „Current …“), `CurrentRulesRevision` (Append-only-Regel), `ComputeRulesHash64` (Literalzeilen, hingenommene Lücke `0x05CCA8475789AD4A`) und `ComputeCurrentRulesHash64` (Live-Pfad plus Stolperdraht). `using System.Collections.Generic` ist ergänzt.
- `tools/Nova.SimRunner.Tests/MatchFingerprintTests.cs`: Die Pins für Rev 1/2 stehen wieder auf `0x531CE8F614A16CB5` / `0x07725EA26668C9F8` (Herkunft `e1970a9`/`8512e9f`), der Kommentar „NOT actually frozen“ ist entfernt. Die bestehende Assertion `current == ComputeRulesHash64(V3)` trägt jetzt die Stolperdraht-Meldung. Drei Tests sind neu: `RulesRevisionThree_FrozenLiteral_IsTheCurrentStream` (Pin `0xD1B683837D12FCEF` direkt auf V3, plus Wurf für Revision 0 und `CurrentRulesRevision + 1`), `CurrentLiveRules_MatchTheFrozenCurrentRevision` (Feld-für-Feld-Diagnose) und `FrozenRevisions_ReadNoLiveConstants` (Quelltextwächter auf den markierten Bereich: Jeder `RulesField.IntXX(...)`-Eintrag muss ein Ganzzahlliteral tragen, auch hex, und außer den `RulesField`-Fabriken ist kein Memberzugriff erlaubt).
- `Assets/Tests/EditMode/Simulation/MatchFingerprintV1Tests.cs`: EditMode-Spiegel mit denselben Änderungen. Nur die Pfadauflösung unterscheidet sich (`UnityEngine.Application.dataPath`, voll qualifiziert, ohne neues `using UnityEngine`).
- `docs/production/DecisionLog.md`: Der Entwurf `### D-120 | Entwurf | …` steht hinter D-114 und vor „## Offene Punkte“, mit fünf bewerteten Alternativen. Kopfversion und Änderungsverlauf sind unberührt; die zieht der Integrator nach.
- `CHANGELOG.md`: Ein Absatz unter `[Unreleased]` → `### Behoben` mit „(#138, D-120-Entwurf)“.

Unberührt sind die vier Guard-Dateien, `docs/README.md` und `ScopeLedger.md`.

## Tests und Prüfungen

Umgebung: `export DOTNET_ROOT="/Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/.dotnet"; export PATH="$DOTNET_ROOT:$PATH"`, ausgeführt im Worktree.

| Befehl | Ergebnis |
|---|---|
| `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release` | **781/781 grün**, 0 Fehler, 14 s (Basis 778 + 3 neue Tests) |
| `python3 .github/scripts/check_docs.py` | Exit 0, „OK: 203 Markdown-Dateien und 5 Quality-JSONs geprüft.“ (nur die bekannten Hinweise auf fehlende Status-Kopfzeilen) |
| `python3 .github/scripts/check_baseline_guard.py --self-test` | „Baseline guard self-test passed, including the required negative control.“ |

**Selbst nachgemessen** mit einem Scratch-Programm (`scratchpad/build-138/measure`) gegen die frisch gebaute `Nova.SimRunner.Tests.dll` dieses Worktrees:

- `ComputeRulesHash64(1)` = `0x531CE8F614A16CB5`
- `ComputeRulesHash64(2)` = `0x07725EA26668C9F8`
- `ComputeRulesHash64(3)` = `0xD1B683837D12FCEF`
- `ComputeCurrentRulesHash64()` = `0xD1B683837D12FCEF` (Revision 3, also unverändert gegenüber `main`)
- `DescribeCurrentRulesDrift()` liefert 0 Zeilen.
- Referenz mit dem alten Schreiblayout: Rev 3 mit HQ 2.000 = `0x05CCA8475789AD4A` (die hingenommene Lücke), Rev 1 mit HQ 3.000 = `0x1163158B8146B168`, Rev 2 mit HQ 3.000 = `0x259E6977B751109B` (die alten Neu-Pins). Die Triage-Werte sind damit bestätigt.
- Historische Konstanten per `git show` geprüft: `e1970a9` 2000/2000/25/10, `8512e9f` 10/5, `4cdcf07` 3/30/8/2/1/3/2.

**Mutationsproben**, jeweils danach aus einer Sicherungskopie im Scratchpad zurückgespielt, Diff anschließend sauber:

1. `EconomySystem.HqBaseCapacityAE` 3000 → 4000: Rot werden `CurrentLiveRules_MatchTheFrozenCurrentRevision` mit der Meldung `revision 3 field 2 (HqBaseCapacityAE): frozen 3000 …, live 4000 …` und `CurrentRulesHash_MovesPastRevisionTwo…`. Die Pins für Rev 1/2/3 sowie ReplayTests und LockstepNetworkTests bleiben grün (Filterlauf 103/105).
2. Ein Wert der V1-Literalzeile durch `EconomySystem.ExcessDecayPercent` ersetzt: Rot wird `FrozenRevisions_ReadNoLiveConstants` (22 von 23 Einträgen sind Literale).

**Nicht gelaufen:**

- **Unity-EditMode-Spiegel** (`MatchFingerprintV1Tests.cs`): nicht kompiliert und nicht ausgeführt, weil keine CI dafür existiert und das Paket keinen Unity-Start vorsieht. Er ist textgleich zur .NET-Lane bis auf die Pfadauflösung (per `diff` geprüft).
- `check_baseline_guard.py` lief nur als `--self-test`, nicht gegen den realen Diff. Nach der Triage greift er nicht, weil `MatchFingerprintTests.cs` keine der vier `BASELINE_FILES` ist.
- Keine Spielrunde. Am Laufzeitverhalten ändert sich nichts, der aktuelle `RulesHash64` ist bitgleich.

## Entscheidung

Der D-120-Entwurf („Regelrevisionen werden append-only und tragen eingefrorene Literalzeilen“) steht in `docs/production/DecisionLog.md`. Kurzfassung: Revisionen sind append-only und tragen Literalzeilen. Nur `ComputeCurrentRulesHash64` liest live. Jede Wertänderung einer gehashten Konstante bedeutet eine neue Revision, nie einen neuen Pin. Der Stolperdraht und der Quelltextwächter setzen das durch. Die Lücke `0x05CCA8475789AD4A` (Revision 3 vor #131, Builds 10.–31.08.2026) wird hingenommen. Dass #131 Revision 3 ohne Revisionssprung umgedeutet hat (entgegen D-104 Punkt 8), ist festgehalten, ebenso dass die Aussagen in `reports/v8.6.0/sprint-23/13-kimi-reparatur.md` dazu überholt sind. Verworfen wurden: nur V1/V2 einfrieren, V3 rückwirkend teilen (nicht lückenlos, sondern verliert alle Builds seit 31.08.), in #108 aufgehen lassen, Identitätstabelle mit Variantenschlüssel.

### Wie der Art-Strang (`feat/generals-look-and-feel`, RulesRevisionV4 für D-116) nachziehen muss

Landet #138 zuerst, muss der Art-Strang beim Rebase seine V4 so umbauen:

1. `public const ushort RulesRevisionV4 = 4;` bleibt wie im Art-Diff, `CurrentRulesRevision = RulesRevisionV4`.
2. **Nicht** die alten `if (rulesRevision >= …)`-Blöcke wiederherstellen. Stattdessen im Bereich `// <frozen-rules-rows>` eine Zeile `FrozenRulesRowV4` anlegen: die 13 Einträge von V3 unverändert kopieren, dazu
   `RulesField.Int32(15, "MaxRadius", <MovementSpacing.MaxRadius.RawValue als Literal>),` und
   `RulesField.Int32(16, "LargeRadius", <MovementSpacing.LargeRadius.RawValue als Literal>),`.
   Die Rohwerte misst man einmal aus (z. B. per Testausgabe) und trägt sie als Dezimal- oder Hexliteral ein. Ein Ausdruck wie `MovementSpacing.MaxRadius.RawValue` lässt der Quelltextwächter nicht durch.
3. `GetFrozenRulesRow` um `case RulesRevisionV4: return FrozenRulesRowV4;` ergänzen.
4. `BuildLiveCurrentRulesRow` um zwei Live-Einträge erweitern:
   `RulesField.Int32(15, nameof(MovementSpacing.MaxRadius), MovementSpacing.MaxRadius.RawValue),` und entsprechend Feld 16.
   `Movement.` bzw. `using Nova.Simulation.Movement` braucht man nur dort, nicht im Literalbereich.
5. Tests: Der Art-Pin `0x7E2E3E8C4CF1591E` muss sich ergeben, wenn die Literale stimmen. `RulesRevisionThree_FrozenLiteral_IsTheCurrentStream` heißt dann besser `RulesRevisionThree_FrozenLiteral_RemainsByteStable`, sein Kommentar „IsTheCurrentStream“ ist überholt. `CurrentRulesHash_MovesPast…` wie im Art-Diff auf V4 umstellen. Der Quelltextwächter erwartet ≥ 23 Einträge und bleibt ohne Änderung grün. `CurrentLiveRules_MatchTheFrozenCurrentRevision` braucht keine Änderung.

Landet der Art-Strang zuerst, zieht dieses Paket nach. Dann friert es zusätzlich V4 als Literalzeile ein (Werte aus `MovementSpacing`), und der aktuelle Hash ist der V4-Pin.

## Nebenbefunde

- **Feldtags 15/16 und die Nummer V4 sind mehrfach verplant.** Der Art-Strang belegt V4 mit Feldern 15/16 für die Bewegung (D-116). `reports/v8.6.0/sprint-23/13-kimi-reparatur.md` (Zeilen ~290–298) plant ebenfalls eine `RulesRevisionV4` mit Feldern 15/16, dort für `PassiveRepairRateHpPerTick`/`PassiveRepairRadiusCells`, und #108 will auch eine V4. Wer später kommt, bekommt die nächste Revision und die nächsten freien Tags.
- Derselbe Kimi-Bericht behauptet, die Rev-1/2-Pins blieben unverändert. Unter dem alten Code war das falsch, unter dem neuen stimmt es wieder. Im D-120-Entwurf ist das vermerkt, den Bericht selbst habe ich nicht angefasst.
- D-115/D-116 kommen im Art-Code als Verweise vor, stehen aber in keinem DecisionLog. Laut Triage sind sie nicht formal reserviert.
- `docs/production/hashkrieg/21_Sprint_Verknappungsfolgen.md:518` sagt, 21.1 Teil b (Ankerliste, D-108) bewege `RulesHash64`. Die gehashten Konstanten decken die Ankerliste aber nicht ab, das ist genau #108. Die Aussage wirkt ungenau, ich habe sie nicht geprüft.
- `DescribeCurrentRulesDrift()` ist neue öffentliche API in `Simulation/Replays/`. Nach Core Rule 4 wäre ein Blick von @api-guardian angebracht, auch wenn die Methode nur additiv und rein diagnostisch ist.

## Offene Fragen an den Inhaber

1. Gibst du D-120 (Option B) für `Replays/MatchFingerprint.cs` frei? Die Endnummer vergibt der Integrator.
2. Soll eine reine Wertrevision (neue Literalzeile wegen geänderter Balance-Konstante) durch D-120 stehend freigegeben sein, oder braucht jede neue Revision eine eigene D-ID?
3. Reihenfolge gegenüber dem Art-Strang (V4/D-116): #138 zuerst (empfohlen, der Art-Strang legt V4 dann gleich als Literalzeile an) oder Art-Strang zuerst?
4. Bestätigst du die Lücke `0x05CCA8475789AD4A` (Revision 3 vom 10.–31.08.2026) als hingenommen?

## Vorschlag für Commit-Titel und PR-Beschreibung

**Commit-Titel:** `fix(replays): freeze rules revisions as literal rows (#138, D-120)` (66 Zeichen)

**PR-Beschreibung:**

> **Was:** `MatchFingerprint.ComputeRulesHash64(rev)` liest nur noch eingefrorene Literalzeilen je Revision. `ComputeCurrentRulesHash64()` liest als einziger Pfad die lebenden Konstanten. `DescribeCurrentRulesDrift()` vergleicht beide Feld für Feld (Stolperdraht), und ein Quelltextwächter verbietet Live-Referenzen im Literalbereich. D-120-Entwurf im DecisionLog.
>
> **Warum:** Bisher war nur die Feldzahl historisch, die Werte waren immer die heutigen. #131 hat so die Identität der Revisionen 1/2 und, ohne Revisionssprung, auch die von Revision 3 verschoben (#138).
>
> **Bereiche:** `Simulation/Replays/MatchFingerprint.cs` (gesperrter Pfad, „niemand ohne D-ID“ → D-120), `MatchFingerprintTests.cs` plus EditMode-Spiegel, DecisionLog, CHANGELOG.
>
> **Hashes:** Der aktuelle `RulesHash64` bleibt `0xD1B683837D12FCEF`, kein verteilter Build wird ungültig. Die Rev-1/2-Pins kehren zu den Werten vor `e4faa75` zurück: `0x531CE8F614A16CB5` / `0x07725EA26668C9F8`. Keine der vier Guard-Baselines (D-091) ist berührt. Die Pins bewegen sich im selben PR wie der Code, weil der Code ohne sie rot wäre. Der Laufzeit-Hash ist unverändert.
>
> **Changelog:** `[Unreleased]` → Behoben: „Historische Regelrevisionen sind wieder eingefroren (#138, D-120-Entwurf).“
>
> **Tests:** `dotnet test` 781/781 grün. `check_docs.py` OK, `check_baseline_guard.py --self-test` OK. Mutationsproben: Konstante geändert → Stolperdraht nennt sie; Literal durch Live-Referenz ersetzt → Quelltextwächter rot.
>
> **Unity-Spuren:** EditMode-Spiegel `MatchFingerprintV1Tests` **nicht gelaufen** (keine Unity-CI, kein lokaler Unity-Lauf).
>
> **Spielabnahme:** nicht gespielt. Grund: Es gibt keine Verhaltensänderung, der aktuelle Regelhash ist bitgleich. Ersatznachweis: volle Simulationssuite plus Hash-Nachmessung. Restrisiko: Der EditMode-Spiegel kompiliert erst beim nächsten Unity-Lauf.
>
> **Folgen:** Der Art-Strang (RulesRevisionV4, D-116) und #108 hängen ihre Revisionen als Literalzeilen an. Vor dem Selbst-Merge ist ein unabhängiges Read-only-Review nötig (AGENTS.md §3).
>
> Fixes #138, Refs #108

## Unity-Spuren nachgefahren (Integrator, 2026-10-17)

Nach dem Commit mit `tools/unity-tests/run_unity_tests.py` (Branch #110) auf diesem Worktree, Unity 6000.5.4f1, warme Library: **EditMode 666/666, PlayMode 15/15**, keine Fehlschläge, keine Compile-Fehler, Arbeitsbaum danach sauber. Die Zeile „Unity-Spuren: nicht gelaufen“ oben ist damit überholt; der PR-Text nennt die gemessenen Zahlen.
