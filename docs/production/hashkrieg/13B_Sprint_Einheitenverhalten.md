# Sprint 13B: Einheitenverhalten und Fraktionsidentität

**Status:** geplant | **Bearbeitung:** externer Beitragender, PR-only vom Fork | **Parallel zu:** [13](13_Sprint_Netzpartie.md), [14](14_Sprint_Lobby.md), [15](15_Sprint_Netzstabilitaet.md) | **Regelwerk:** [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md) | **Leitsatz:** zwei Fraktionen, die sich gleich spielen, sind eine Fraktion

## Ziel

Einheiten verhalten sich wie Einheiten, und die beiden Fraktionen spielen sich
unterschiedlich. Heute tun sie beides nicht: Die KI ist eine Skirmish-Notlösung,
und Allianz wie Legion greifen auf dieselben Waffenprofile zu.

Dieser Strang läuft **fortlaufend neben** dem Netzstrang. Er hat deshalb
Meilensteine statt eines Enddatums — das Tempo bestimmt der Beitragende, nicht
ein Sprintkalender.

## Schreibhoheit

Dieser Strang besitzt exklusiv:

| Pfad | Inhalt |
|---|---|
| `Assets/_Project/Scripts/AI/` | `SkirmishAiSystem`, `AiFactionProfile`, `AiPeerCommandTransport` |
| `Assets/_Project/Scripts/AI.Data/` | KI-Datenschicht |
| `Assets/_Project/Scripts/Simulation/Movement/` | `MovementSystem` |
| `Assets/_Project/Scripts/Simulation/Combat/` | `CombatSystem`, `WeaponProfiles`, `DamageMatrix`, `ArmorClass`, `DamageType` |
| `Assets/_Project/Scripts/Simulation/Factions/` | `EvolvedFactionSystem`, `BiomassGrid` |
| eigene neue Testdateien unter `tools/Nova.SimRunner.Tests/` | |

Geteilt und nur nach Absprache: `Simulation/Definitions/` — `WeaponDefinition`
und `UnitDefinition` werden hier gebraucht, `BuildingDefinition` und
`SimDefinitions` gehören dem Wirtschaftsstrang.

**Ausdrücklich nicht in diesem Strang:** `Simulation/Construction/`,
`Simulation/Economy/`, `Scripts/Networking/`, `Scripts/Gameplay/`,
`Simulation/Replays/`, `Simulation/Snapshots/`, `Simulation/State/`.

## Pakete

### B1 · Waffenidentität der Legion

Heute ist die Eingabeschicht auf Allianz-DefIds 1–17 hartverdrahtet und beide
Fraktionen benutzen dieselben Profile. Die Legion bekommt eine eigene
Waffencharakteristik — Salven statt Einzelschuss, Flächenschaden statt
Punktschaden — sodass eine Fraktionswahl überhaupt eine Entscheidung wird.

Die **Auswahloberfläche** dafür liegt nicht hier, sondern in
[Sprint 14](14_Sprint_Lobby.md): gewählt wird in der Lobby.

### B2 · Rüstungsklassen, die man merkt

`DamageMatrix` und `ArmorClass` existieren, aber die Werte sind flach genug,
dass Einheitenwahl kaum zählt. Ziel ist ein Schere-Stein-Papier, das ein Spieler
nach drei Partien ohne Tabelle erklären kann.

### B3 · Bewegung, die nicht dumm aussieht

Truppenführung (D-088) ordnet Gruppen bereits. Offen bleibt das Verhalten am
Ziel und unterwegs: Einheiten, die sich gegenseitig blockieren, Umwege statt
Warten, sinnvolles Abstandhalten von Fernkämpfern.

### B4 · Eine KI, die nicht nur baut

Die Skirmish-KI baut und schickt Truppen los. Was fehlt, ist Reaktion:
Angriffserkennung, Rückzug, Zielpriorisierung, Verteidigung des eigenen
Aetherium-Feldes.

### B5 · Tests, die das Verhalten festhalten

Jedes Paket bringt eigene Tests unter `tools/Nova.SimRunner.Tests/` mit. Nicht
als Formalie: Ohne sie ist im Nachhinein nicht unterscheidbar, ob eine
Verhaltensänderung gewollt war.

## Die drei Regeln, die nicht verhandelbar sind

Sie stehen ausführlich in [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md)
und hier in Kurzform, weil sie in diesem Strang scharf werden:

1. **Determinismus.** Kein `float`, kein `DateTime.Now`, kein
   `System.Random`, keine Abhängigkeit von Iterationsreihenfolge einer Hashmap.
   Die Simulation muss auf zwei Rechnern Bit für Bit dasselbe rechnen — sonst
   bricht die Netzpartie ab, und zwar zu Recht.
2. **Baselines nie im selben PR.** Ein PR ändert Verhalten **oder** setzt eine
   Determinismus-Baseline neu, nie beides. Sonst wird eine unbemerkte
   Verhaltensänderung grün durch die CI gewinkt.
3. **Transport-Verträge sind fremdes Terrain.** `ICommandTransport` und
   `ICommandSubmissionReadiness` werden benutzt, nicht geändert.

## Ablauf für Beiträge

1. Fork des Repositories, kurzer Topic-Branch (`feat/`, `fix/`, `refactor/`).
2. `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release`
   lokal grün.
3. Zeile unter `[Unreleased]` in `CHANGELOG.md`.
4. Conventional Commit, PR vom Fork nach `main`.
5. Ein Maintainer liest und merged. Selbst-Merge gibt es in diesem Strang nicht.

Was ein PR beschreiben muss: was und warum, welche Pakete er berührt, und —
bei Verhaltensänderung — **was im laufenden Spiel zu sehen war**. Ein grüner
Test ohne gespielte Beobachtung reicht in diesem Projekt nicht (siehe
[GOVERNANCE.md](../../../GOVERNANCE.md)).

## Bewusst nicht in diesem Strang

| | Warum |
|---|---|
| Wirtschaft, Bauketten, Platzierungsregeln | Strang C, bleibt beim Maintainer-Team (Sprint 16) |
| Netzwerk, Lobby, Matchrahmen | Sprints 13–15 |
| Speicherformat, Snapshots, Replays | Inhaberentscheidung mit D-ID |
| Neue Fraktionen jenseits Allianz/Legion | ausserhalb MS-1 |

## Fertig wenn

Pro Paket, nicht für den Strang als Ganzes:

1. `dotnet test` grün, ohne angepasste Baseline im selben PR.
2. Neue Tests halten das beabsichtigte Verhalten fest.
3. Ein Mensch hat die Änderung im laufenden Spiel gesehen und es im PR notiert.
4. Ein Maintainer hat den PR gelesen und gemergt.

Der Strang gilt als abgeschlossen, wenn B1–B4 gemergt sind und eine Partie
Allianz gegen Legion sich erkennbar anders spielt als Allianz gegen Allianz.

## Changelog-Notiz

Pro PR eine Zeile unter `[Unreleased]`. Kein Sammel-Eintrag am Ende.

## Versionsrelevanz

`minor` pro Paket — Verhaltensänderungen ohne Vertragsbruch. Eine Änderung am
Zustandslayout wäre `major` und braucht vorher eine D-ID.
