# kimi-agent report

- when:    2026-08-31T07:50:22Z
- backend: cc
- model:   k3[1m]
- mode:    rw
- dir:     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/bauen
- run:     /Users/denniswestermann/.agent-runs/20260831-095022-81019

## Task

Du arbeitest an "Project Nova" / HashKrieg, einem Unity-RTS mit deterministischer,
ganzzahliger Simulation. Doku und Berichte: Deutsch. Code und Docstrings:
Englisch, wie im Bestand.

**ARBEITSVERZEICHNIS — der einzige Pfad, unter dem du liest und schreibst:**

    /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/bauen

Daneben liegt eine Arbeitskopie unter `/Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova`.
**Fass die nicht an, weder lesend noch schreibend.**

## Der Befund — Issue #135, lies ihn zuerst

Der Inhaber hat am 31.08.2026 gespielt und zwei Dinge gemeldet:

> „Ich habe einen zweiten Atlas gebaut. Der hat keine Funktion, der kann keine
> Gebäude bauen."
>
> „Ich wollte bei den Vorkommen in der Mitte ein Hauptquartier bauen. Das ging
> nicht, obwohl ich genug Strom hatte und auch genug Geld."

**Beides ist derselbe Fall, und die Simulation hat recht.**
`ConstructionSystem.IsInsideBuildInfluence` misst die Bauzone von den eigenen
fertigen **Gebäuden** aus, nicht vom Pionier. Der zweite Atlas ist nicht kaputt;
er stand dort, wo niemand bauen darf. Und die Kartenmitte liegt 56 Zellen vom
HQ entfernt, bei `BuildInfluenceRadiusCells = 8`.

**Das Verhalten bleibt. Es geht ausschließlich darum, dass der Spieler es
erfahren kann.** D-108 hat die Regel bewusst so gesetzt, und die Karte aus
Sprint 21 ist auf sie hin gebaut — wer sie ändert, macht den Sprint kaputt.

## Der eigentliche Defekt

`ConstructionSystem.ValidatePlacement` wirft vier Ursachen in **einen**
Rückgabewert (`CommandResultCode.RejectedInvalidTarget`):

- außerhalb der Karte oder besetzt
- `!IsInsideBuildInfluence` — zu weit von den eigenen Gebäuden
- `!HasMinimumBuildingSpacing` — zu dicht am Nachbarn
- `!HasValidFieldSpacing` — falscher Abstand zum Vorkommen

Der Spieler bekommt „geht nicht" und muss raten. Bei genug Geld und Strom ist
„die Einheit ist kaputt" der naheliegendste Schluss — und genau den hat er
gezogen.

## Deine drei Aufgaben

**1. Die Ursachen trennen.** Vier unterscheidbare Gründe statt eines. Wie du
das schneidest, entscheidest du — es gibt bereits ein
`BuildingPlacementBlocker`-Konzept in `BuildMenuHud`, sieh dir an, ob das der
richtige Träger ist oder ob die Simulation einen eigenen Grund zurückgeben
sollte.

> **Die eingefrorene Grenze beachten:** `Simulation/CommandsV1/` ist
> D-ID-pflichtig. Wenn ein neuer `CommandResultCode` das Schema berührt,
> **halt an und melde es** — dann ist der richtige Weg, den Grund
> präsentationsseitig zu ermitteln, indem die vorhandenen öffentlichen Prüfungen
> (`IsInsideBuildInfluence`, `HasMinimumBuildingSpacing`, …) einzeln gefragt
> werden. Prüf, welche davon schon öffentlich sind.

**2. Die Meldung muss die Regel nennen, nicht das Ergebnis.** Nicht „ungültiges
Ziel", sondern der Satz, der den Spieler handeln lässt: *warum* nicht, und *was
stattdessen*. Bei der Bauzone ist die Antwort „deine Bauzone reicht acht Felder
um jedes fertige Gebäude — bau dich in Richtung Mitte vor". Schreib die Texte
auf Deutsch, im Ton des übrigen Spiel-UI.

**3. Prüf, ob das Bauzonen-Overlay in der Kartenmitte überhaupt zeichnet.**
`BuildZoneOverlayView` zeigt sich während des Platzierens (`:107`, an
`PlacementModeActive`). Wenn es dort korrekt „nichts baubar" malt, hat der
Spieler es übersehen und Aufgabe 2 genügt. **Wenn es dort gar nicht zeichnet,
ist „nicht baubar" von „nicht gezeichnet" nicht unterscheidbar — und das wäre
ein eigener Fehler, den du melden musst.** Sieh dir an, ob das Quad die ganze
Karte abdeckt oder nur einen Ausschnitt.

Das ist die Frage, deren Antwort ich am wenigsten kenne. Beantworte sie
sauber, auch wenn sie „das Overlay ist in Ordnung" lautet.

## Schreibhoheit — verbindlich

ERLAUBT:
  Assets/_Project/Scripts/Simulation/Construction/ConstructionSystem.cs
                                       nur lesende Flächen / Rückgabegründe
  Assets/_Project/Scripts/Presentation/UI/BuildMenuHud.cs
  Assets/_Project/Scripts/Presentation/UI/BuildZoneOverlayView.cs
  Assets/Tests/EditMode/Simulation/     Tests zu den getrennten Gründen
  tools/Nova.SimRunner.Tests/           dito
  reports/v8.6.0/sprint-23/             nur deine eigenen Dateien

VERBOTEN:
  Assets/_Project/Scripts/Simulation/Economy/    dort arbeitet ein anderer Worker
  Assets/_Project/Scripts/Simulation/CommandsV1|Snapshots|Replays|Systems|State/
  Assets/_Project/Scripts/Simulation/Combat|Movement|Factions|Pathfinding/
  Assets/_Project/Scripts/AI/  AI.Data/
  Assets/_Project/Scripts/Presentation/UI/DebugHud.cs
  Assets/_Project/Scripts/Presentation/UI/ResourceBarHud.cs  (falls vorhanden — anderer Worker)
  Assets/_Project/Scripts/Gameplay/Match/  Presentation/Maps/
  CHANGELOG.md  VERSION  ROADMAP.md  README.md  plans/**  global.json

**Den CHANGELOG fasst du nicht an.** Vorschlagstext in den Report.

## Handwerkliches

Zwei Nachträge pro neuer HUD-Fläche, im Bestand mehrfach schiefgegangen:
`EstimateHeight` bildet die Höhenrechnung von `OnGUI` Zeile für Zeile nach, und
jede neue Trefferfläche gehört in `IsPointerOverHud` — sonst schlagen Klicks
dahinter in die Welt durch.

Neue `.cs` unter `Assets/` brauchen eine `.meta`-Schwester:
`fileFormatVersion: 2` plus `guid:` mit 32 neuen Hex-Zeichen.

## Verifikation

    "/Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/.dotnet/dotnet" test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release

Ausgangsstand **739/739 grün** — er muss grün bleiben. Vorher und nachher
fahren, beides wörtlich in den Report. Unity hast du nicht: die
Präsentationsseite ist nur durch sorgfältiges Lesen abgesichert. Sag im Report,
was unbelegt bleibt.

## Was du NICHT tust

- Kein `git commit`, `git push`, `git add`, kein PR, kein `gh`-Aufruf.
- Keine Subagenten.
- **Die Bauzonenregel selbst ändern.** Radius, Mindestabstand und Feldabstände
  bleiben, wie sie sind.

## Report

Markdown nach `reports/v8.6.0/sprint-23/`. Struktur:

  1. Wie du die vier Gründe getrennt hast, und ob die Simulation dafür etwas
     Neues zurückgibt oder die Präsentation einzeln fragt
  2. Die Meldungstexte, wörtlich
  3. **Die Antwort auf Frage 3: zeichnet das Overlay in der Mitte?** Mit Beleg
  4. Testlauf vorher / nachher
  5. Was unbelegt bleibt und wie der Inhaber es nachprüft
  6. CHANGELOG-Vorschlagstext

Schließe mit:

  STATUS: DONE | BLOCKED
  - Befund 1
  - Befund 2
  - Befund 3

## Output

