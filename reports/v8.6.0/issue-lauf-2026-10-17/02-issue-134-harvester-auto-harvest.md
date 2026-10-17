---
issue: 134
paket: P-134
branch: feat/134-harvester-born-harvesting
agent: builder (opus)
datum: 2026-10-17
status: DONE
---
# #134: Ein produzierter Sammler wird mit Ernteauftrag geboren, die KI beauftragt fremd beauftragte Sammler neu

## Aufgabe

Bisher bekam nur der Gründungs-Sammler (#43) einen Feldauftrag. Jeder in der
Raffinerie produzierte Sammler stand am Sammelpunkt, bis der Spieler `H`
drückte. Paket P-134 setzt den Sim-Weg um: `ProductionSystem` vergibt beim
Spawn das nächste Feld mit Restbestand ab dem Footprint-Zentrum der Raffinerie,
nach derselben Regel wie die Gründung. Dazu kommt die KI-Härtung gegen den
Hänger bei abweichender Feldwahl (r10), das angepasste Determinismus-Drehbuch,
der nachgezogene KI-Pin, Tests in beiden Lanes und der D-121-Entwurf.

## Was geändert wurde — Files Changed

- `Assets/_Project/Scripts/Simulation/Production/ProductionSystem.cs` — Nach `SpawnUnit`, bei `def.Role == UnitRole.Harvester`, wird die Zentrumszelle aus `building.Transform` bestimmt (Klemmung wie beim Sammelpunkt). Dann `_economy.TryFindNearestField` aufrufen und `HarvestFieldId` setzen; ohne Feld bleibt der Sammler unbeauftragt. Der Sammelpunkt-Befehl bleibt. Klassendoku und Summary von `ExecuteTick` nachgezogen (#134, D-121).
- `Assets/_Project/Scripts/AI/SkirmishAiSystem.cs` — Der Schritt (4) beauftragt jetzt alle Sammler mit `!IsReturningCargo && HarvestFieldId != ownFieldId`, bisher nur die mit `HarvestFieldId == 0`. Die Liste wird gebildet, nachdem `ownFieldId` bekannt ist. Die tote Sammelliste `idleHarvesterRaws` im Entity-Scan entfällt. Klassen-Remarks, Schrittkommentar und Begründung am Aufruf nachgezogen.
- `Assets/_Project/Scripts/AI.Data/AiBehaviorId.cs` — `Revision` von 9 auf 10, mit History-Absatz zu r10. Der Profil-Hash bleibt.
- `tools/Nova.SimRunner/Determinism10000Scenario.cs` — Ein `HashSet<uint> HarvestersOrdered` in `SlotState` ersetzt `IdleHarvesterRaws` durch `UnorderedHarvesterRaws`: Jeder eigene Sammler wird genau einmal beauftragt, also bleibt `CommandKind.Harvest` im Befehlsstrom. Doku an Klasse, `SlotState` und `IssueSlotCommands` angepasst. Der Guard-Test `Determinism10000Tests.cs` ist unverändert.
- `tools/Nova.SimRunner.Tests/CanonicalAiOutcomeTests.cs` — `PinnedEndState` von `0x4A861D9FA2E57C6F` auf `0x7AF32DBA56E393E2`. Im Kommentar stehen „last moved by #134 (D-121)“, der Vorwert, der Vorvorwert und die Messung mit und ohne r10. Der veraltete Hinweis „AiBehaviorId is r7.E34435F9“ ist auf r10.1E6E7AE3 berichtigt. Der Tick 2726 bleibt.
- `tools/Nova.SimRunner.Tests/SkirmishAiTests.cs` — Identitätspin auf `r10.1E6E7AE3` mit kurzem r10-Kommentar. Neuer Helfer `FirstHarvester` und neuer Regressionstest `SkirmishAi_RetasksAHarvesterOrderedOntoAForeignField_AndItMines`.
- `Assets/Tests/EditMode/AI/SkirmishAiTests.cs` — Spiegel: `FirstHarvester` und derselbe Regressionstest. Der EditMode-Spiegel hat keinen Identitätspin, deshalb dort keine r10-Zeile.
- `tools/Nova.SimRunner.Tests/ProductionSystemTests.cs` — Fixture-Helfer `SpawnRefinery` und `ExhaustField` sowie fünf neue Tests. Abgedeckt sind: nächstes Feld, Gleichstand zum Registrierungsindex, erschöpftes Feld übersprungen, kein Feld → kein Auftrag bei laufender Produktion, Infanterie ohne Auftrag mit Sammelpunkt-Befehl. Dazu `using Nova.Simulation.Pathfinding` und die Klassen-Summary.
- `Assets/Tests/EditMode/Simulation/ProductionSystemTests.cs` — 1:1-Spiegel. Der Diff zur .NET-Lane umfasst nur die vier bestehenden Lane-Zeilen (Namespace, Summary, sealed).
- `tools/Nova.SimRunner.Tests/EconomyIntegrationTests.cs` — Neue Fabrik `EcoHost.CreateWithProduction` (Verdrahtung wie `ProductionConstructionIntegrationTests`) und zwei Tests. Erstens fließen Credits ohne jeden Harvest-Befehl; der einzige Befehl ist die QueueUnit. Zweitens löscht `Stop` den Geburtsauftrag, und 600 Ticks lang vergibt nichts neu.
- `Assets/Tests/EditMode/Simulation/EconomyIntegrationTests.cs` — 1:1-Spiegel. **Außerhalb des Schreibumfangs der Paketspezifikation**: Die Spezifikation nennt nur die .NET-Datei. Die Rahmenvorgabe verlangt aber den EditMode-Spiegel, wo der Bestand einen hat, und die Spiegelregel des Repos ebenso. Ich habe ihn deshalb mitgezogen.
- `docs/production/DecisionLog.md` — `### D-121 | Entwurf | Wirtschaft (…)` nach D-114, vor „## Offene Punkte“. Kopfversion und Änderungsverlauf unberührt.
- `CHANGELOG.md` — ein Absatz unter `## [Unreleased]` → `### Geändert`, mit (#134).

Nicht angefasst: `RtsDeviceInput.cs`, `BuildMenuHud.cs`, die vier Guard-Dateien, `MatchFingerprint.cs`, `docs/README.md`, `ScopeLedger.md`.

## Tests und Prüfungen

Alle Befehle im Worktree, mit
`export DOTNET_ROOT="/Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/.dotnet"; export PATH="$DOTNET_ROOT:$PATH"`.

| Befehl | Ergebnis |
|---|---|
| `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release` (Basis a6858d1, vor jeder Änderung) | 778/778 grün |
| derselbe Befehl, Endstand | **786/786 grün** (778 + 8 neue Tests) |
| `… --no-build --filter "FullyQualifiedName~Determinism10000Tests"` | 10/10 grün; die Guard-Datei ist unverändert, `Harvest` steht weiter im Strom |
| `python3 .github/scripts/check_docs.py` | OK, 203 Markdown-Dateien und 5 Quality-JSONs, exit 0 (nur die bekannten Notices zu fehlenden Status-Kopfzeilen) |
| `python3 .github/scripts/check_baseline_guard.py --self-test` | passed, inklusive Negativkontrolle |
| `check_baseline_guard.py --changed-files <git diff --name-only> --labels <leer>` | „Baseline guard passed: the PR changes only one side of the separation.“ |
| `git diff --name-only` gegen die vier Guard-Dateien | keine Treffer |

**Gegenproben in Scratch-Kopien** (`scratchpad/build-134/prodonly` und `noprod`; rsync des Worktrees, der Worktree selbst blieb unberührt):

- Neue Produktion, **alte KI** (r9-Code), neue Tests: 785/786. Rot ist genau `SkirmishAi_RetasksAHarvesterOrderedOntoAForeignField_AndItMines`. Der Regressionstest erkennt den Hänger also.
- **Alte Produktion**, neue KI: 780/786. Rot sind die fünf Geburtsauftrags-Tests (3× ProductionSystem, 2× EconomyIntegration) und der KI-Pin. Der Pin zeigt dort `0x4A861D9FA2E57C6F`, also den alten Wert: r10 allein bewegt die kanonische Partie nicht.
- Neue Produktion mit alter KI ergibt beim KI-Pin `0x7AF32DBA56E393E2`, also denselben Wert wie der Endstand. Der Pin wandert durch die Produktionsänderung.
- In allen drei Varianten blieb der Entscheidungstick 2726 (im `Assert.Multiple` schlug nur der Hash fehl).

**Nicht gelaufen:**

- Unity-EditMode-Tests: Die Spiegel in `Assets/Tests/EditMode/AI/SkirmishAiTests.cs`, `Assets/Tests/EditMode/Simulation/ProductionSystemTests.cs` und `Assets/Tests/EditMode/Simulation/EconomyIntegrationTests.cs` sind weder kompiliert noch ausgeführt. Grund: Unity ist für dieses Paket nicht freigegeben und läuft in keiner CI. Sie sind textgleich zur .NET-Lane bis auf die bestehenden Lane-Unterschiede; das per `diff` geprüft.
- PlayMode-Tests: nicht gelaufen, nicht betroffen.
- Gespielte Runde (zweiten Sammler bauen, er fährt ohne `H` los): **nicht gespielt**. Der Client-Teil (Eskorte ab Sammelpunkt zum Feld) ist nur durch den unveränderten Code von `RtsDeviceInput` begründet, nicht beobachtet.

## Entscheidung

D-121-Entwurf (`docs/production/DecisionLog.md`, „### D-121 | Entwurf | Wirtschaft (ein produzierter Sammler wird mit Ernteauftrag geboren)“):

- Ein produzierter Sammler bekommt im Spawn-Tick `HarvestFieldId` = `TryFindNearestField` ab dem Footprint-Zentrum der Raffinerie, nach Regel und Anker der Gründung.
- Es gibt kein Nachsteuern. Stop und H bleiben beim Spieler, und ohne Feld mit Restbestand gibt es keinen Auftrag.
- Die KI-Härtung (r10) kommt im selben PR, ohne RulesRevision-Bump.

Vier bewertete Alternativen: Sim-Weg (gewählt), der Client-Weg aus `73f54b5`, gewollt lassen mit Hinweis-UI, und der Sim-Weg mit Reichweitengrenze. Die Konsequenzen nennen den Pin im selben PR samt Widerspruch D-101 ↔ D-108/R-3, das Drehbuch, das Merge-Fenster und den neuen Build, den Wegfall des Client-Blocks in P-51 sowie die KI-Ein-Feld-Grenze.

## Nebenbefunde (nicht umgesetzt)

1. Die Klassendoku von `ProductionSystem` behauptet weiter „HQ produces Builder and Harvester“ (QueueUnit-Absatz). Seit D-077 produziert die Raffinerie den Sammler. Die Zeile ist veraltet, nicht von #134 verursacht und unangetastet.
2. Der `_autoAssignedHarvesters`-Block in `RtsDeviceInput.cs` (Art-Branch, `73f54b5`) wird mit diesem PR zu totem Ballast mit abweichender Feldregel (Gleitkomma, ab Sammlerposition). Sein CHANGELOG-Eintrag fehlt; vorgesehen ist die Entfernung in P-51.
3. `docs/production/DemoRunbook.md:61-62` und `:140` beschreiben „Harvester erntet das Feld“ nach Q. Auf `main` war das bisher faktisch falsch, mit diesem PR stimmt es. Kein Doku-Eingriff nötig.
4. Der Onboarding-Text in `BuildMenuHud.cs:85-86` nennt `H` als Schritt 3. Mit diesem PR ist der Schritt für neue Sammler überflüssig. Bewusst nicht angefasst, weil der Art-Branch die Datei umbaut.
5. `tools/Nova.AiLab/reports/behavior-log.md` existiert nicht, obwohl `AiBehaviorId`/`SkirmishAiTests` dorthin verweisen. Die r10-Messung steht deshalb im CHANGELOG und im PR.
6. Der KI-Harness (`SkirmishAiTests`) hat nur zwei Felder mit je 2 Mio. AE. Der kanonische Pin deckt das Ausweichen nach Erschöpfung nicht ab, das tun nur die neuen Unit-Tests.
7. `.gitignore:26` lautet `reports/          # lokale …`. Git kennt keine Kommentare am Zeilenende, deshalb greift das Muster nicht: `git status` zeigt `reports/v8.6.0/issue-lauf-2026-10-17/` als unversioniert (`??`), nicht als ignoriert. Der Bericht taucht daher ohne `-f` in `git status` auf.
8. Die KI-Härtung zwingt jeden KI-Sammler auf genau ein KI-Feld. Für D-114 Stufe B (Ausbau auf mehrere Felder) braucht es eine eigene Regel, zum Beispiel eine Gather-Zelle je Feld.

## Offene Fragen an den Inhaber

1. Sim-Weg (dieser PR) oder Client-Weg (`73f54b5`)? Gewinnt der Sim-Weg, entfällt der `_autoAssignedHarvesters`-Block im Art-PR (P-51).
2. Soll die Feldwahl ab dem Raffineriezentrum (umgesetzt) oder ab der Sammelpunktzelle erfolgen? Und soll es eine Reichweitengrenze geben (T-01 spricht von „in seiner Reichweite“; heute kann ein neuer Sammler nach Erschöpfung des Heimatfelds quer über die Karte geschickt werden)?
3. Pin-Hoheit: D-101 (Pin im selben PR) gegen D-108/Sprint 21 R-3 (zweiter PR, Merge-Fenster mit dem Einheitenstrang). Umgesetzt ist D-101, der Widerspruch gehört in den PR-Text.
4. Merge-Fenster: Am besten zusammen mit der RulesRevision-V4 des Art-Branches mergen, damit der Fingerprint alte Builds abtrennt. Wer als Zweiter mergt, misst den KI-Pin neu (der Art-Branch bewegt ihn selbst auf 2721/0x6E15320BCC3F3EE4, laut Paketspezifikation; von mir nicht gemessen).
5. D-ID-Endnummer: vorläufig D-121, die Vergabe macht der Integrator.

## Vorschlag für Commit-Titel und PR-Beschreibung

**Commit-Titel:** `feat(economy): let produced harvesters start with a field order`

**PR-Beschreibung:**

> **Was:** Ein in der Raffinerie produzierter Sammler bekommt beim Spawn den
> Ernteauftrag auf das nächste Feld mit Restbestand (ab dem Footprint-Zentrum
> der Raffinerie), wie der Gründungs-Sammler seit #43. Die Skirmish-KI
> beauftragt zusätzlich Sammler, deren Auftrag ein fremdes Feld nennt
> (AiBehaviorId r10.1E6E7AE3). Fixes #134.
>
> **Warum:** Nur der Gründungs-Sammler erntete von selbst. Jeder weitere wartete
> unbewacht auf ein `H`, während die KI ihre Sammler längst selbst schickte.
> Ohne die KI-Härtung bleibt ein Sammler mit abweichendem Feldauftrag dauerhaft
> stehen. Möglich war das schon auf `main` über den Gründungs-Sammler. D-121
> (Entwurf) hält Regel, Alternativen und Konsequenzen fest, darunter den
> Client-Weg aus `73f54b5` als Alternative.
>
> **Bereiche:** `Simulation/Production`, `AI`, `AI.Data`, Drehbuch
> `tools/Nova.SimRunner/Determinism10000Scenario.cs`, Tests in beiden Lanes,
> DecisionLog, CHANGELOG.
>
> **Pins:**
> - `CanonicalAiOutcomeTests`: Tick 2726 bleibt, der Endzustand geht von
>   `0x4A861D9FA2E57C6F` auf `0x7AF32DBA56E393E2`. Gemessen auf a6858d1 plus
>   dieser PR. Die Produktionsänderung allein liefert denselben Wert, r10 allein
>   den alten.
> - Nach D-101 im selben PR nachgezogen. Hinweis: D-108/Sprint 21 R-3 verlangen
>   für diesen Pin einen zweiten PR; seit D-114 liegen beide Stränge beim
>   Inhaber.
> - Keine der vier Guard-Dateien ist geändert. `check_baseline_guard` ist grün.
>   Das Drehbuch statt des Guard-Tests wurde angepasst (Sprint 21 R-2).
>
> **Changelog:** `[Unreleased]` → Geändert: „Ein produzierter Sammler erntet von
> selbst (#134, D-121-Entwurf).“
>
> **Tests:**
> - `dotnet test … -c Release`: 786/786 grün (Basis 778).
> - `check_docs.py` OK, `check_baseline_guard.py --self-test` passed.
>
> **Unity-Spuren:** Die EditMode-Spiegel (AI/SkirmishAiTests,
> Simulation/ProductionSystemTests, Simulation/EconomyIntegrationTests) sind
> **nicht gelaufen**; lokal in Unity 6000.5.4f1 auszuführen. PlayMode ist
> nicht betroffen.
>
> **Spielabnahme:** **nicht gespielt.** Ersatznachweise:
> - Headless belegen die Integrationstests den vollen Zyklus ohne
>   Harvest-Befehl.
> - Sie belegen auch, dass `Stop` den Auftrag löscht und nichts neu vergibt.
>
> Restrisiko:
> - Der Client-Teil (Eskorte vom Sammelpunkt zum Feld) ist nicht beobachtet.
> - Der Umweg über einen weit gesetzten Sammelpunkt ist nicht beobachtet.
> - Ebenso nicht beobachtet: das Zusammenspiel mit dem Client-Block aus
>   `73f54b5`, falls beide gleichzeitig landen.
> - Gemischte alte und neue Builds fallen in Netzpartien erst als Relay-Desync
>   auf (kein RulesRevision-Bump). Deshalb im Merge-Fenster mergen und danach
>   neuen Testbuild ausgeben.
>
> 🤖 Generated with [Claude Code](https://claude.com/claude-code)
