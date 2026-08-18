# Großauftrag: Die Folgen der Verknappung

**Version:** 1.0.0 | **Status:** verbindlich | **Erteilt:** 2026-08-17 | **Auftraggeber:** Inhaber | **Ausführung:** Kimi (Maintainer-Seite) | **Umfang:** Blöcke 1–2, entspricht Sprint 21 und Sprint 18 | **Grundlage:** `main` @ `3e10c48` | **Leitsatz:** erst sichtbar machen, was die Simulation weiß, dann die Karte anfassen

## Vorrangregel

Dieses Dokument ist die **einzige verbindliche Reihenfolge** für die hier
genannten Sprints. Wo es von einer Sprintdatei abweicht, gilt dieses Dokument.
Wo es schweigt, gilt die Sprintdatei.

Wenn dir etwas widersprüchlich vorkommt: **halte an und frag nach.** Rate nicht.
Ein falsch aufgelöster Widerspruch kostet mehr als eine Rückfrage.

Der vorige Auftrag [AUFTRAG_Grossblock.md](AUFTRAG_Grossblock.md) ist damit
abgeschlossen, soweit er Code betraf: Block 0 (#49) und Block 1 (Sprint 16,
Pakete 16.1–16.10) liegen auf `main`. Sein Block 3 (Sprint 17 Paket A) bleibt
offen und ist **nicht** Teil dieses Auftrags.

## Der Auftrag in einem Satz

Aus dem Betatest T-01 vom 10.08.2026 sind zehn Issues entstanden (#85–#94). Einer
davon (#85) ist bereits vom Einheitenstrang behoben. Du erledigst die übrigen
Befunde des Maintainer-Strangs und ziehst den lange geplanten Sprint 18 direkt
hinterher, weil beide dieselben zwei Dateien anfassen — Befehlskarte und
Auswahl.

## Warum diese Reihenfolge

Der Bericht sagt es selbst: die endlichen Felder aus #80 waren kein Wert,
sondern ein Systemwechsel. Acht Systeme stehen noch auf der alten Annahme. Zwei
davon sind **unsichtbar geworden** statt falsch — der Restbestand und der
Baubereich existieren exakt und kommen nirgends an. Die macht dieser Auftrag
zuerst sichtbar, weil danach über Zahlen balanciert wird statt über Gefühl.

Der Beleg dafür steht im Bericht: der Tester schätzte das Startvorkommen auf
5.000 AE, tatsächlich sind es 9.000. Die Fehleinschätzung um fast die Hälfte ist
kein Vorwurf an ihn, sondern die Messgröße für das Problem.

## Was du nie anfasst

Diese Pfade gehören dem **externen Beitragenden** (Einheitenstrang 13B). Ein PR,
der sie berührt, wird nicht gemergt, sondern zurückgegeben:

```
Assets/_Project/Scripts/Simulation/Combat/
Assets/_Project/Scripts/Simulation/Movement/
Assets/_Project/Scripts/Simulation/Factions/
Assets/_Project/Scripts/Simulation/Pathfinding/
Assets/_Project/Scripts/AI/
Assets/_Project/Scripts/AI.Data/
Assets/_Project/Scripts/Presentation/UI/DebugHud.cs
tools/Nova.AiLab/            tools/Nova.AiLab.Tests/
tools/Nova.SimRunner.Tests/CanonicalAiOutcomeTests.cs
tools/Nova.SimRunner.Tests/SkirmishAi*.cs
```

> **Die eine Ausnahme, die du brauchst und die keine ist:** Block 1 Paket 21.7
> schreibt Gelände über die **bestehende öffentliche** `CostField.SetCost` aus
> `Gameplay/Match/` — genauso, wie `PathfindingTestBootstrap` es heute schon
> tut. Du fasst `Simulation/Pathfinding/` dabei **nicht** an. Die Hoheit bleibt
> gewahrt.

Diese sind **eingefroren** und brauchen eine Inhaberentscheidung mit D-ID:

```
Assets/_Project/Scripts/Simulation/CommandsV1/     ← kein neuer CommandKind
Assets/_Project/Scripts/Simulation/Replays/
Assets/_Project/Scripts/Simulation/Snapshots/
Assets/_Project/Scripts/Simulation/Systems/
Assets/_Project/Scripts/Simulation/SimulationKernel.cs
Assets/_Project/Scripts/Simulation/State/  — nur Layout und Serialisierung
```

`Simulation/State/UnitCommandStateView.cs` darfst du bearbeiten, aber nur die
Befehlsanwendung — *was* ein bestehender `CommandKind` mit dem Zustand tut
(D-095). Kein neues Feld, keine neue Reihenfolge, kein `StateVersion`-Bump.

Und diese vier Dateien fasst **kein Verhaltens-PR** an:

```
tools/Nova.SimRunner.Tests/SnapshotGoldenBytesTests.cs
tools/Nova.SimRunner.Tests/CommandGoldenBytesTests.cs
tools/Nova.SimRunner.Tests/SimRandomGoldenTests.cs
tools/Nova.SimRunner.Tests/Determinism10000Tests.cs
```

Ausnahme: ein **eigener** PR, der ausschließlich eine Baseline neu setzt, mit
altem und neuem Wert im Text und der Begründung. Nie zusammen mit einer
Verhaltensänderung. Das Drehbuch `tools/Nova.SimRunner/Determinism10000Scenario.cs`
ist davon **nicht** betroffen und darf im selben PR nachgezogen werden.

## Die zwei Blöcke

Arbeite sie **in dieser Reihenfolge** ab. Zwischen den Blöcken liegt ein Gate:
erst wenn der vorige gemergt und gespielt gesehen ist, fängt der nächste an.

### Block 1 · Sprint 21 — Die Verknappung wird lesbar und bespielbar

**Binde Sprintdatei:** [21_Sprint_Verknappungsfolgen.md](21_Sprint_Verknappungsfolgen.md)

Sieben Pakete, 21.1 bis 21.7, in der dort stehenden Reihenfolge. Sie ist nach
Abhängigkeit sortiert, nicht nach Aufwand:

| | Paket | Issue | Hängt an |
|---|---|---|---|
| 21.1 | Territoriumsregel aussprechen und Enge messen | #92 → D-108 | — |
| 21.2 | **Restbestand sichtbar machen** (kritisch) | #86 | — |
| 21.3 | Startmenge rechnen statt raten | #87 | 21.2 |
| 21.4 | Baubereich sichtbar machen | #91 | 21.1 |
| 21.5 | Auswahl sagt die Wahrheit | #88 | — |
| 21.6 | Karte trägt mehr Felder | #93 | 21.1 |
| 21.7 | **Die Mitte wird ein Gebiet** (teuerstes) | #94 → D-109 | 21.6 |

**21.5 ist abhängigkeitsfrei.** Zieh es vor, wenn du an einer Stelle wartest.

**Die Abwurfliste, falls die Zeit nicht reicht:** erst 21.7, dann 21.6, dann
21.3. **21.1, 21.2, 21.4 und 21.5 fallen nicht** — sie sind der Grund, warum
dieser Sprint existiert. Was fällt, kommt mit Begründung in den
[ScopeLedger](../ScopeLedger.md).

**Zwei Dinge, die du aus der Sprintdatei allein nicht siehst:**

- **21.6 und 21.7 bewegen den gepinnten Ausgang der kanonischen KI-Partie.** Eine
  andere Karte heißt eine andere Partie. `CanonicalAiOutcomeTests` gehört dem
  Einheitenstrang. Beide Pakete brauchen ein eigenes Merge-Fenster, abgestimmt
  mit @arn-c0de — „ein Fenster hat einen Strang". Sag Bescheid, bevor du 21.6
  aufmachst, statt danach.
- **21.2 legt kein Feld in den Zustand.** Die Anzeige „6.420 / 9.000" braucht die
  Anfangsreserve. Sie kommt aus der kanonischen Kartenlage, **nicht** aus einem
  neuen Feld in `AetheriumField` — das wäre `Simulation/State/`-Layout und damit
  eingefroren.

### Block 2 · Sprint 18 — Befehl und Auswahl werden lesbar

**Binde Sprintdatei:** [18_Sprint_Befehl_und_Auswahl.md](18_Sprint_Befehl_und_Auswahl.md)

Drei Pakete, die Issues #50, #51 und #52. Fasst **keine** Simulationsdatei an
außer der Zielverteilung in `ApplyMove`. 18.3 (Formationsausrichtung) ist die
erste Abwurfkandidatin — die Verteilung selbst existiert seit Sprint 11 bereits.

**Warum dieser Block direkt hinter Sprint 21 kommt und nicht davor:** 21.5 (#88)
und 18.1 (#50) fassen beide `CommandCardHud` und `SelectionManager` an, aus
entgegengesetzten Richtungen — 21.5 *liest* eine bestehende Auswahl, 18.1
*stellt* eine her. Sie hintereinander zu bauen spart eine Runde Konfliktauflösung
in genau den zwei Dateien, die dieser Auftrag am häufigsten öffnet.

## Die fünf stillen Fallen

Diese brechen nichts sofort. Sie brechen später, an einer Stelle, die nicht nach
der Ursache aussieht.

**1 · Die Feldlage steht viermal literal im Repo.** Verifiziert gegen `3e10c48`:

```
Assets/_Project/Scripts/Gameplay/Match/MatchBootstrap.cs:168
tools/Nova.SimRunner/Determinism10000Scenario.cs:210
tools/Nova.SimRunner.Tests/CanonicalMatchSetupTests.cs:85
Assets/Tests/EditMode/Gameplay/CanonicalMatchSetupTests.cs:113
```

Drei von vier ergibt einen roten Test, der wie ein Determinismusfehler aussieht
und keiner ist. Betrifft 21.3, 21.6 und 21.7.

**2 · Die Symmetrie ist bindend, nicht kosmetisch.** D-107: Punkte spiegeln als
`(x, y) → (124 - x, 124 - y)`, der Ursprung eines 3×3-Footprints als
`(x, y) → (122 - x, 122 - y)`. Eine asymmetrische Feldlage ist kein
Geschmacksfehler, sondern ein Balancefehler, den niemand als solchen meldet — er
sieht aus wie „die KI ist zu stark".

**3 · Gelände hat heute genau eine Quelle zu viel.** `CostField` kann
unbegehbare Zellen (`ImpassableCost = 255`), aber es schreibt niemand hinein
außer `ConstructionSystem`. Und `GlutrinneBlockoutView` streut ~84 Felsen, die
laut eigener Zusicherung *„never writes into simulation state"* — sie sind
**Deko und begehbar**. Wer in 21.7 Chokepoints baut und beide Seiten getrennt
speist, bekommt Einheiten, die durch Felsen laufen und an unsichtbaren Wänden
hängenbleiben. Optik und Begehbarkeit müssen aus **derselben** Struktur stammen,
so wie die fünf Aetheriumfelder es heute schon tun.

**4 · Jede neue HUD-Zeile braucht zwei Nachträge.** `CommandCardHud.EstimateHeight`
bildet die Höhenrechnung von `OnGUI` Zeile für Zeile nach — der Kommentar dort
dokumentiert genau diesen Fehler aus der Vergangenheit („~40 px short … visible,
but not clickable"). Und jede neue Trefferfläche gehört in `IsPointerOverHud`,
das heute drei Komponenten kennt; fehlt sie, schlagen Klicks hinter dem Panel in
die Welt durch. Betrifft 21.2, 21.5 und den ganzen Block 2.

**5 · `tools/build/` ist ignoriert.** Der Ordner sieht aus wie der
Packaging-Ordner und enthält eine fast identische Kopie von `build-mac.sh`, ist
aber über `.gitignore` ausgeschlossen. Änderungen dort verschwinden. Der
getrackte Weg ist ausschließlich `tools/packaging/`.

## Wie du arbeitest

| | |
|---|---|
| **Branch** | eigener Topic-Branch je Paket (`feat/`, `fix/`), `main` ist PR-only |
| **PR-Schnitt** | ein PR je Paket. Lieber mehrere kleine als einer, der drei Ordner öffnet |
| **Verhalten und Baseline** | nie im selben PR. Baseline-Neusetzung ist ein eigener PR mit altem und neuem Wert im Text |
| **CHANGELOG** | genau eine Zeile unter `[Unreleased]`, ganz oben im Abschnitt. Keine datierte Versionsüberschrift anlegen |
| **Commit** | Conventional Commit |
| **Push** | **nie ohne ausdrückliche Freigabe des Inhabers** |
| **Deploy** | nie. Weder VPS noch Supabase |

### Der Nachweis

Zwei Bedingungen, und **beide** müssen zutreffen:

1. `dotnet test tools/Nova.SimRunner.Tests` ist grün — bei einem
   verhaltensändernden PR **nach** dem unmittelbar folgenden Baseline-PR. Ein
   roter Golden-Byte-Test im Verhaltens-PR ist dort erwartet und kein Blocker.
2. Ein Mensch hat die Sache im laufenden Spiel gesehen und es notiert — im PR
   oder im [GrayboxLog](../GrayboxLog.md).

> **Berichtigung gegenüber [AUFTRAG_Grossblock.md](AUFTRAG_Grossblock.md):**
> dort steht, lokal laufe `dotnet test` nicht. **Das stimmt nicht (mehr).** Im
> Repo-Root liegt ein mitgeliefertes `.dotnet/` mit exakt der in `global.json`
> gepinnten Version `8.0.318`. Der vollständige Lauf dauert rund 14 Sekunden:
>
> ```
> "$PWD/.dotnet/dotnet" test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release
> ```
>
> Das systemweite `dotnet` scheitert an `rollForward: disable` — nimm **immer**
> den Repo-lokalen Pfad. Du kannst und sollst also lokal prüfen, bevor du einen
> PR aufmachst.

Was die CI trotzdem nicht leistet: `Nova.SimRunner.Tests` linkt nur Core,
Simulation, AI, AI.Data und Networking. **Für alles in `Gameplay/` und
`Presentation/` führt kein CI-Lauf den Code aus**, und die Unity-EditMode-Tests
laufen mangels Lizenz nicht. Das betrifft in diesem Auftrag den größten Teil von
21.2, 21.4, 21.5 und den ganzen Block 2. Die Quelltext-Wächter greifen dort
trotzdem: `PresentationSourceBoundaryTests` scannt auf `GetUnitRef(` und
`.Random`, ebenso der asmdef-Rangcheck. Ein *Verhaltens*nachweis ist dort die
gespielte Runde plus Screenshot. Schreib in den PR, was du gesehen hast — und
wenn du es nicht selbst sehen kannst, sag das, statt es zu behaupten.

### Was du selbst entscheidest

Alles Handwerkliche: Benennung, Aufteilung, Reihenfolge innerhalb eines Pakets,
Konstanten ohne Designwirkung, die konkrete Optik eines Overlays. Notier die
Entscheidung kurz im PR.

### Was du nicht entscheidest

- Einen neuen `CommandKind` einführen
- `StateVersion`, Schemaversionen oder das Zustandslayout ändern — auch nicht
  „nur ein Feld" in `AetheriumField`
- Die Tickreihenfolge in `MatchRunner` ändern oder ein neues System registrieren
- Die Anzahl oder Lage der Aetheriumfelder ohne die Symmetrieprüfung aus D-107
- `MinimumBuildingDistanceCells` oder `BuildInfluenceRadiusCells` ändern, bevor
  die Messung aus 21.1 vorliegt
- Ein Paket streichen, das nicht auf der Abwurfliste steht
- Irgendetwas in fremdem Terrain, auch wenn es „nur eine Zeile" wäre

In allen Fällen: anhalten, im PR oder in einer Nachricht beschreiben, warum es
nötig wäre, und auf die Entscheidung warten.

## Was nicht in diesem Auftrag ist

| | Warum |
|---|---|
| **#89 Patrouille, #90 Bewachen** | Brauchen einen Eintrag im eingefrorenen `CommandKind`-Register, eine neue Payload und Zustand pro Einheit im Snapshot. API-/Schema-Vorgang mit @api-guardian, Versionsrelevanz eher `major` |
| **#85 KI erntet auf leerem Feld** | Bereits behoben, [PR #97](https://github.com/VibecodingGermany/HashKrieg/pull/97), Einheitenstrang |
| **KI-Verhalten an den neuen Feldern** | Expansion, Feldsicherung, Eskorten — Einheitenstrang 13B. Dieser Auftrag macht die Karte reicher, nicht die KI klüger |
| **Sprint 17 Paket A** (Zugangsprotokoll) | Offen aus dem vorigen Auftrag, eigener Block nach diesem |
| **Sprint 13.2, 13.4, 13.5** | 13.2 braucht Zugangsdaten, 13.4/13.5 brauchen zwei Menschen an zwei Rechnern. Kein Codeauftrag |
| **Sprint 15.1–15.4** | Eigener Sprint, nach diesem Auftrag |
| **Sprint 19 / Art** (#57, #58) | Art-Arbeit außerhalb des Repositories |
| **#42 Zittern im Pulk, #66/#67 Halte-Feuer und Strommangel in der Verteidigung** | `Simulation/Movement/` und `Simulation/Combat/` — Einheitenstrang |
| **#55 Reparaturzone, #56 Sanitäter** | Im Fragenkatalog, kein Bauauftrag |
| **Verteidigungstürme an den Chokepoints balancieren** | Erst wenn die Mitte steht und einmal gespielt wurde |

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-08-17 | Erstfassung: Sprint 21 und Sprint 18 gebündelt, gegen `main` @ `3e10c48` geprüft, Grenzen und stille Fallen benannt. Enthält die Berichtigung zur lokalen Testbarkeit gegenüber dem vorigen Auftrag | Orchestrator |
