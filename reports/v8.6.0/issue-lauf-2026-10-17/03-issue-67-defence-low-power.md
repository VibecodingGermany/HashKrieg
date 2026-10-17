---
issue: 67
paket: P-67
branch: feat/67-defence-offline-low-power
agent: builder (sonnet)
datum: 2026-10-17
status: DONE
---
# Verteidigungsplattformen fallen bei Strommangel aus (#67)

## Aufgabe
`CombatSystem` kannte keinen Strombegriff: Türme feuerten weiter, obwohl ihr Besitzer im Strommangel war (Economy.md verlangt „Verteidigungsplattformen offline“). Umgesetzt ist Variante A mit Ausfallverhalten F (Triage-Empfehlung): Gebäuderollen im Strommangel erfassen nichts, feuern nicht und verwerfen ihr Ziel; Einheiten sind nie betroffen; der Cooldown läuft weiter.

## Was geändert wurde — Files Changed
- `Assets/_Project/Scripts/Simulation/Combat/CombatSystem.cs` — dritter Konstruktorparameter von `ISlotFactionLookup` auf `EconomySystem` (Feld/Parameter `_economy`/`economy`), `IsOffline` (Gebäuderolle UND `IsLowPower` des Besitzers), Gate in Phase 2 (kein Erfassen) und Phase 3 (Ziel verwerfen, kein Schuss), Phase 1 unverändert (Cooldown läuft weiter); Klassen- und Konstruktorkommentar nachgezogen.
- `tools/Nova.SimRunner.Tests/CombatSystemTests.cs` — vier neue Tests (siehe unten).
- `Assets/Tests/EditMode/Simulation/CombatSystemTests.cs` — dieselben vier Tests im EditMode-Spiegel (Datei war bis auf Namespace/Klassenkopf identisch).
- `Assets/_Project/Scripts/Gameplay/UI/ResourceBarPresenter.cs` — `LowPowerWarning` um „· Türme aus“ ergänzt (TargetAudience.md:64: UI muss die Regel erklären).
- `Assets/_Project/Scripts/Gameplay/UI/CommandCardPresenter.cs` — `FormatPowerBalance` hängt bei Defizit „· Türme aus“ an (Baubar = Ort der Bauentscheidung, Spielerfalle durch nicht reservierte Baustellen).
- `Assets/Tests/EditMode/Gameplay/ResourceBarPresenterTests.cs` — zwei erwartete Zeichenketten angepasst.
- `Assets/Tests/EditMode/Gameplay/CommandCardPresenterTests.cs` — erwartete Zeichenkette angepasst.
- `docs/gamedesign/Buildings.md` — Grundprinzip „Energiebilanz“ und Abschnitt Verteidigungsplattform berichtigt (Plattform fällt aus); Version 0.9.0 + Verlaufszeile.
- `docs/gamedesign/Economy.md` — MS-1-Stand der Low-Power-Regel (Plattformen offline, Zielverwurf, Cooldown, ein Tick Latenz, Spielerfalle); Version 0.7.0 + Verlaufszeile.
- `docs/production/DemoRunbook.md` — §4 Schritt 5 und §5 „Zielerfassung“ berichtigt (Plattformen nur bei gedecktem Strom); Version 0.6.2 + Verlaufszeile.
- `docs/production/DecisionLog.md` — Entwurf D-122 vor „## Offene Punkte“ (Kopfversion/Verlauf nicht angefasst).
- `CHANGELOG.md` — ein Eintrag unter `[Unreleased]` / „Geändert“ (#67).
- `reports/v8.6.0/issue-lauf-2026-10-17/03-issue-67-defence-low-power.md` — dieser Bericht (gitignored).

Nicht angefasst: die vier Guard-Dateien, `docs/README.md`, `ScopeLedger.md`, alle Aufrufer von `CombatSystem` (alle 20 Konstruktionsstellen — 16x `new CombatSystem(` plus 4x `new Nova.Simulation.Combat.CombatSystem(` in Assets/ und tools/ — übergeben bereits ein konkretes `EconomySystem`; mit `git grep` geprüft, auch `Determinism10000Scenario.cs` und `Program.cs`).

## Tests und Prüfungen
Neue Tests (beide Lanes): `LowPower_DefensePlatform_StaysSilent_AndResumesWhenPowerReturns` (30 Ticks Stille, Cooldown läuft auf 0, nach Rückkehr feuert sie im ersten Tick sofort: 1000 -> 980), `LowPower_DoesNotSilenceUnits_OnlyBuildings` (Schutztest, auch ohne Gate grün), `LowPower_OnlyTheDeficitOwnersPlatformFalls`, `LowPower_OutageDropsTheHeldTarget_SoRecoveryReacquiresTheNearest`.

Gelaufen (im Worktree, SDK aus dem Hauptbaum):
- `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release` -> 782/782 bestanden (Basis 778 + 4 neue). Kein Pin bewegt sich (CanonicalAiOutcomeTests, MatchFingerprintTests, Determinism10000 laufen im Satz grün; keine Pin-Datei angefasst).
- Negativkontrolle: `IsOffline` temporär auf `false` gezwungen, Filter `CombatSystemTests` -> 3 der 4 neuen Tests rot (Rest 23 grün, der Schutztest erwartungsgemäß weiter grün); Gate danach wiederhergestellt, Vollauf wie oben grün.
- `python3 .github/scripts/check_docs.py` -> OK: 203 Markdown-Dateien und 5 Quality-JSONs geprüft (nur die vorbestehenden Hinweise zu fehlenden Statuszeilen in docs/production/hashkrieg).
- `python3 .github/scripts/check_baseline_guard.py --self-test` -> bestanden; `--changed-files <git diff --name-only> --labels <leere Datei>` -> „passed: the PR changes only one side of the separation“.

NICHT gelaufen:
- Unity-Kompilierung und alle Unity-EditMode-Tests: der Spiegel `Assets/Tests/EditMode/Simulation/CombatSystemTests.cs`, `ResourceBarPresenterTests`, `CommandCardPresenterTests`. Die geänderten Presenter-Dateien (`Gameplay/UI/*.cs`) werden von der dotnet-Spur nicht kompiliert. Die Änderungen sind Zeichenketten bzw. ein Typtausch, der in der dotnet-Spur mit denselben Quellen kompiliert; ungeprüft bleibt nur die Unity-Seite.
- Gespielte Runde (Kraftwerk zerstören, Türme schweigen, Neubau, Türme feuern sofort).
- Es gibt keine Offline-Anzeige je Plattform in der Präsentation (CoreGameplay.md:127 wünscht durchgestrichene Icons) — nicht Teil dieses Pakets.

## Entscheidung (D-122-Entwurf)
Variante A (`EconomySystem` als dritter Parameter, kleinster Eingriff, Präzedenz FoW/Construction) gegen B (neue Schnittstelle, 20 Aufrufstellen), C (`ISlotFactionLookup` erweitern, Name lügt, `State/` heikel) und D (Wirtschaft manipuliert Turmzustand). Ausfallverhalten F (Ziel verwerfen) statt E. Regel-Hash: keine eigene Revision, `RulesHash64` bewegt sich nicht; Reihenfolge zu V4 des Art-Strangs offen. Eintrag: `docs/production/DecisionLog.md`, „### D-122 | Entwurf | …“.

## Nebenbefunde (nichts davon umgesetzt)
1. **Nicht reservierte Baustellen** (`EconomySystem.RecomputePower` überspringt Sites, `ConstructionSystem.cs` Platzierungsprüfung liest nur die fertige Bilanz): Stromverbraucher, die knapp nacheinander gesetzt werden, überzeichnen den Strom über mehrere Ticks, nicht nur im selben Tick (Q-040 k1 beschreibt nur den Same-Tick-Fall). Mit #67 schaltet das nach Fertigstellung die gesamte Verteidigung ab. Neues Issue oder Q-040-Erweiterung — nur mit Freigabe veröffentlichen.
2. **Turm hält ein Ziel außer Reichweite und erfasst nie neu** (Phase 2 überspringt Angreifer mit gültigem Ziel, Phase 3 hält Ziele außer Reichweite). F mildert das nur nach Stromausfällen. Als Kommentar an #51 (Punkt „Kein Nachsetzen“) anhängen — nur mit Freigabe.
3. Eine offline Plattform verbraucht weiter Strom (`RecomputePower` zählt sie); das Defizit kann sich so halten.
4. NeutralUnits.md:92: eroberte neutrale Türme sollen keine Energie brauchen; die Regel würde sie erfassen, sobald solche Rollen existieren.
5. Vertragsflächen-Tabelle in `13-15_Parallelbetrieb.md` (Maintainer-Terrain): neue sechste Fläche CombatSystem (Einheitenstrang) liest `EconomySystem.GetPlayerEconomy().IsLowPower` (Netzstrang). Nachtrag liegt beim Inhaber.
6. Hash-Lücke wie in #108: Build mit und ohne Regel melden dieselbe `RulesHash64`; abgefedert nur durch den Build-Commit-Abgleich der Lobby. Nach dem Merge ist ein neuer Testbuild fällig; der Merge sollte nicht unbedacht im selben Fenster wie der V4-PR des Art-Strangs landen.
7. Die Triage nannte „D-030“ als Quelle der Regel; D-030 betrifft nur die Forschung (Economy.md zitiert es trotzdem). Im Entwurf nicht als Quelle angeführt.
8. Der Berichtsordner `reports/v8.6.0/issue-lauf-2026-10-17/` erscheint in `git status` als untracked (nicht ignoriert); der Integrator fügt ihn wie vorgesehen hinzu.
9. Beim Negativtest habe ich kurz eine Sicherung unter `/tmp` angelegt und sofort wieder gelöscht (nicht im Repo).

## Offene Fragen an den Inhaber
- Reihenfolge zum V4-PR des Art-Strangs: Landet #67 vor V4, deckt V4 die Regel implizit; sonst als vierter Fall in #108 nachtragen.
- Wird die Endnummer D-122 beim Merge bestätigt (D-115/D-116 sind im Art-Branch vergeben)?
- Sollen die beiden Nebenbefunde (Baustellen-Überzeichnung, stehendes Ziel) als Issue/Kommentar veröffentlicht werden?
- Ergänzung der Vertragsflächen-Tabelle (Nebenbefund 5).

## Vorschlag für Commit-Titel und PR-Beschreibung
Commit-Titel: `feat(combat): take defence platforms offline at low power`

PR-Beschreibung:
- **Was:** Gebäuderollen (MS-1: Verteidigungsplattform) im Strommangel des Besitzers erfassen nichts, feuern nicht und verwerfen ihr Ziel; Cooldown läuft weiter, bei Rückkehr des Stroms sofort wieder Feuer; Einheiten nie betroffen. `CombatSystem` bekommt das `EconomySystem` als dritten Parameter. HUD-Warnungen (Ressourcenleiste, Baubar) nennen „Türme aus“.
- **Warum:** Economy.md verlangt „Verteidigungsplattformen offline“; ohne sie bleibt der Angriff auf das Kraftwerk halb wirksam. Fixes #67, Entscheidung D-122 (Entwurf).
- **Bereiche:** `Simulation/Combat/`, `Gameplay/UI/` (zwei Presenter), Tests beider Spuren, Buildings.md, Economy.md, DemoRunbook.md, DecisionLog, CHANGELOG.
- **Changelog:** Eintrag „Verteidigungsplattformen fallen bei Strommangel aus (#67, D-122)“ unter `[Unreleased]` / Geändert.
- **Tests:** `dotnet test` 782/782 (778 + 4 neue), Negativkontrolle (Gate aus -> 3 von 4 neuen Tests rot), `check_docs.py` OK, `check_baseline_guard.py --self-test` OK. Kein Pin, keine Baseline bewegt, `RulesHash64` unverändert (Hash-Lücke wie #108, siehe D-122).
- **Unity-Spuren:** nicht gelaufen — EditMode-Spiegel `CombatSystemTests` (4 neue Tests), `ResourceBarPresenterTests`, `CommandCardPresenterTests`; Unity-Kompilierung der geänderten Presenter.
- **Spielabnahme:** nicht gespielt. Grund: Lauf headless im Issue-Lauf. Ersatznachweis: vier Sim-Tests mit Negativkontrolle. Restrisiko: Balance (Kraftwerksausfall schaltet nun die gesamte Verteidigung ab), Spielerfalle durch nicht reservierte Baustellen, HUD-Textbreite unter Unity ungeprüft (die Leiste misst die Breite zur Laufzeit).
