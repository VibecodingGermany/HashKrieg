---
issue: 67
paket: P-67
branch: feat/67-defence-offline-low-power
reviewer: opus
runde: 1
datum: 2026-10-17
verdict: APPROVED
---
# Review Runde 1 – P-67: Verteidigungsplattformen fallen bei Strommangel aus

Unabhängiges Read-only-Review nach AGENTS.md §3. Ich habe keine Projektdatei
geändert; geschrieben habe ich nur diesen Bericht. Mutationsläufe fanden in einer
Wegwerf-Kopie im Scratchpad statt (`scratchpad/build-67/rv1`), nicht im Worktree.

## Was geprüft wurde

- `git status --short` und `git diff` des Worktrees vollständig (12 geänderte
  Dateien, untrackt nur der Berichtsordner), Basis `a6858d1`.
- Abgleich gegen Issue #67, die Analyse samt Gegenprüfung (`triage/issue-67.json`)
  und die Paketspezifikation (`triage/paket-P-67.json`).
- Schreibumfang: alle 12 geänderten Dateien stehen im Schreibumfang des Pakets.
  Die vier Guard-Dateien (D-091) sind unberührt, ebenso `docs/README.md`,
  `ScopeLedger.md`, alle Pin- und Baseline-Dateien.
- Alle Konstruktionsstellen von `CombatSystem` per `git grep "CombatSystem("`
  in `Assets/` und `tools/`: 20 Stellen, jede übergibt eine Variable vom Typ
  `EconomySystem` (auch die nur in Unity kompilierten: `SkirmishAiTests`,
  `CanonicalMatchSetupTests`, `EconomyIntegrationTests`,
  `ProductionConstructionIntegrationTests`, `CombatSystemTests`,
  `WeaponValuesTests` im EditMode-Ordner, `MatchRunner`). Die Variablen namens
  `factions` sind `var factions = new EconomySystem(...)`. Nichts bleibt
  unkompilierbar zurück.
- Registrierungsreihenfolge Economy vor Combat in `MatchRunner.cs:290/296`,
  `Program.cs:379/385`, `Determinism10000Scenario.cs:791/797`,
  `LockstepNetworkTests.cs:339/345` und `:413/419`. `RecomputePower` läuft
  unbedingt am Anfang jedes Economy-Ticks (`EconomySystem.cs:477`), die Bilanz
  ist also abgeleitet und restore-sicher.
- Determinismus: keine float/double, keine Hash-Container, keine Zeit- oder
  Zufallsquelle. `IsOffline` liest nur Rolle und Bilanz; der neue Ausnahmepfad
  von `GetPlayerEconomy` (Slot ≥ 8) ist durch den vorher aufgerufenen
  `GetSlotFaction` schon abgedeckt.
- EditMode-Spiegel `Assets/Tests/EditMode/Simulation/CombatSystemTests.cs`:
  vollständiger `diff` gegen die dotnet-Spur zeigt nur Namespace, Mirror-Kommentar
  und `sealed` – die vier neuen Tests sind identisch.
- UI: `ResourceBarPresenter.LowPowerWarning`, `CommandCardPresenter.FormatPowerBalance`
  und die drei angepassten Erwartungen; weitere Vorkommen der Zeichenketten per
  `git grep` gesucht – keine weitere Stelle, die den alten Text erwartet.
- Doku: Kopfversionen und Verlaufszeilen von Buildings.md (0.8.1 → 0.9.0),
  Economy.md (0.6.0 → 0.7.0), DemoRunbook.md (0.6.1 → 0.6.2) vorhanden;
  inhaltlich gegen den Code geprüft.
- DecisionLog: D-122 steht direkt nach D-114 vor „## Offene Punkte“, Kopfzeile
  `### D-122 | Entwurf | …`, Status, Kontext, vier bewertete Alternativen plus
  zwei Teilentscheidungen (Ausfallverhalten E/F, Regel-Hash), Entscheidung,
  Begründung, Konsequenzen. Kopfversion und Verlaufstabelle unberührt.
- CHANGELOG: ein Absatz unter `## [Unreleased]` → `### Geändert` (Zeile 260 ff.),
  mit „(#67, D-122)“.

## Nachgerechnete Ergebnisse (selbst gemessen)

| Prüfung | Befehl | Ergebnis |
|---|---|---|
| Simulationstests | `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release` | **782/782 bestanden**, 0 Fehler (deckt sich mit dem Builder) |
| Doku-Links | `python3 .github/scripts/check_docs.py` | **OK: 204 Markdown-Dateien und 5 Quality-JSONs** (Builder: 203 – die Differenz ist sein eigener Bericht, den der Scanner über `ls-files --others` mitzählt; vor dessen Anlage waren es 203) |
| Baseline-Guard | `python3 .github/scripts/check_baseline_guard.py --self-test` | **bestanden**, inkl. Negativkontrolle |
| Guard-Dateien | `git diff --name-only` gefiltert | keine der vier berührt |
| Negativkontrolle Gate aus | Kopie, `IsOffline` → `false`, Filter `CombatSystemTests` | **3 von 26 rot** (StaysSilent, OnlyTheDeficitOwners, OutageDrops), 23 grün – bestätigt die Builder-Angabe |
| Mutation E (Ziel behalten) | Kopie, Phase 3 ohne `AttackTarget = Invalid` | **1 rot** (`LowPower_OutageDropsTheHeldTarget_…`) – F ist festgenagelt |
| Mutation Cooldown eingefroren | Kopie, Phase 1 zählt offline Gebäude nicht herunter | **26/26 grün – Mutation überlebt** (siehe Befund M-1) |

Pins: Der Vollauf ist grün, und keine Pin-Datei (`CanonicalAiOutcomeTests`,
`MatchFingerprintTests`, Determinism10000) ist geändert; damit bewegt sich kein
Pin. `RulesHash64` bleibt unverändert (keine Konstante berührt).

Nicht gelaufen (auch nicht beim Reviewer): Unity-Kompilierung, EditMode-Tests
(Spiegel `CombatSystemTests`, `ResourceBarPresenterTests`,
`CommandCardPresenterTests`), gespielte Runde. Der Builder weist alles das
korrekt als nicht gelaufen aus.

## Befunde

### Hoch

Keine.

### Mittel

**M-1 – Der Weiterlauf des Cooldowns ist behauptet, aber nicht getestet.**
`tools/Nova.SimRunner.Tests/CombatSystemTests.cs:270-271` (und Spiegel
`Assets/Tests/EditMode/Simulation/CombatSystemTests.cs:270-271`) prüft
`WeaponCooldownTicks == 0` mit der Meldung „the cooldown keeps running down
while offline“. Die Plattform hat in diesem Test aber nie gefeuert; der Cooldown
war von Anfang an 0, die Prüfung ist leer. Meine Mutation (Phase 1 zählt den
Cooldown offline nicht herunter) überlebt alle 26 Combat-Tests. Auch der
Builder-Bericht beschreibt das als „Cooldown läuft auf 0“ – technisch wahr,
belegt aber nicht, was die Paketspezifikation, D-122 und Economy.md als Regel
festschreiben („nach Rückkehr des Stroms sofort wieder Feuer“). Kein
Korrektheitsfehler im Code (Phase 1 ist unverändert und richtig), daher nicht
blockierend.
*Vorschlag:* den Test so bauen, dass die Plattform erst bei gedecktem Strom
feuert (Cooldown 10), dann ausfällt, 10 Ticks offline läuft, und bei Rückkehr im
ersten Tick sofort trifft; alternativ `WeaponCooldownTicks = 10` direkt setzen,
10 Ticks im Defizit steppen, `== 0` prüfen. In beiden Spuren identisch.

### Niedrig

**N-1 – Bericht widerspricht sich zu „gitignored“.**
`reports/v8.6.0/issue-lauf-2026-10-17/03-issue-67-defence-low-power.md:27` nennt
den Bericht „gitignored“, Nebenbefund 8 desselben Berichts sagt richtig „nicht
ignoriert“. Ursache (vorbestehend, nicht Teil des Pakets): `.gitignore:26`
lautet `reports/          # lokale …` – Git kennt keine Inline-Kommentare, das
Muster enthält also den Kommentartext und greift nie. Nebenbei zählt dadurch
`check_docs.py` die Berichte mit (tote Links in Berichten würden die CI rot
machen, sobald sie committet sind). *Vorschlag:* Zeile 27 im Bericht
korrigieren; den `.gitignore`-Fehler als Nebenbefund an den Inhaber.

**N-2 – Historische Sprintdoku bleibt stehen.**
`docs/production/hashkrieg/16_Sprint_Wirtschaft.md:218` („Die Verteidigung fällt
hier nicht mit“) ist als Sprintplan historisch korrekt und nicht im
Schreibumfang. Kein Handlungsbedarf, allenfalls ein Verweis auf #67 beim
nächsten Anfassen.

**N-3 – CHANGELOG-Absatz ist lang.**
Der Eintrag hat zwölf Zeilen, die Vorgabe erlaubt „eine Zeile oder einen kurzen
Absatz“. Inhaltlich richtig und ehrlich (Hash-Lücke, nicht gelaufene
EditMode-Tests benannt); Kategorie „Geändert“ passt, weil sich bestehendes
Combat-Verhalten ändert. Kürzen optional.

**N-4 – Offline-Zustand ist nicht je Plattform sichtbar.**
Die Warnung „Türme aus“ steht an Ressourcenleiste und Baubar; eine Anzeige an
der Plattform selbst (CoreGameplay.md:127: durchgestrichene Icons) fehlt. Der
Builder nennt das selbst als nicht Teil des Pakets. Für die Spielabnahme
relevant, nicht für den Merge.

## Fachliche Bewertung

- **Korrektheit:** Das Gate sitzt in Phase 2 (nach `IsArmed`, vor der
  Kandidatensuche) und in Phase 3 (nach Ziel-Validierung und `IsArmed`, vor
  Reichweite/Sicht/Cooldown). Offline-Plattformen erfassen nicht, verwerfen ihr
  Ziel und feuern nicht; ein explizit gegebenes Ziel wird ebenfalls verworfen,
  was zu F passt. Einheiten sind über `IsBuildingRole` sicher ausgenommen; im
  MS-1 ist die DefensePlatform die einzige bewaffnete Gebäuderolle. Ein Tick
  Latenz bei im selben Tick zerstörtem Kraftwerk ist korrekt beschrieben.
- **Tests:** Drei der vier neuen Tests nageln das Gate fest (Negativkontrolle),
  der vierte ist als Schutztest ehrlich deklariert; F gegen E ist festgenagelt.
  Lücke nur beim Cooldown (M-1).
- **Doku:** Buildings.md, Economy.md und DemoRunbook.md stimmen mit dem Code
  überein; die Spielerfalle durch nicht reservierte Baustellen ist benannt.
  D-030 wird korrekt nicht als Quelle der Plattformregel angeführt.
- **D-122:** vollständig, Präzedenzlage zum Regel-Hash ehrlich in beide
  Richtungen dargestellt (D-108/#55 ohne Hashbewegung, 16.6 mit Revision).
- **Bericht:** vollständig, Zahlen stimmen mit meiner Messung überein,
  Nicht-Gelaufenes korrekt ausgewiesen, Spielabnahme als „nicht gespielt“ mit
  Grund, Ersatznachweis und Restrisiko. Bis auf N-1 und die geschönte
  Cooldown-Formulierung (M-1) ehrlich.

## Empfehlung für den Inhaber

**APPROVED.** Mergebar nach grüner Pflicht-CI. Vor dem Commit empfehle ich M-1
nachzuziehen (ein Test in zwei Spuren, wenige Zeilen), weil die
Cooldown-Zusage sonst nur im Kommentar existiert. Weiterhin offen und nur vom
Inhaber zu entscheiden: Reihenfolge zum V4-PR des Art-Strangs (Hash-Lücke),
Endnummer der D-ID, Veröffentlichung der zwei Nebenbefunde (Baustellen-
Überzeichnung, stehendes Ziel an #51), Nachtrag der sechsten Vertragsfläche in
`13-15_Parallelbetrieb.md`. Unity-EditMode-Lauf und eine gespielte Runde stehen
aus; der PR muss die Spielabnahme ausdrücklich als „nicht gespielt“ führen.
