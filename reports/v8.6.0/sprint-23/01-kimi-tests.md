# kimi-agent report

- when:    2026-08-30T00:15:16Z
- backend: cc
- model:   k3[1m]
- mode:    rw
- dir:     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests
- run:     /Users/denniswestermann/.agent-runs/20260830-021516-73692

## Task

Du arbeitest an "Project Nova" / HashKrieg, einem Unity-RTS mit deterministischer,
ganzzahliger Simulation. Doku und Berichte: Deutsch. Code und Docstrings:
Englisch, wie im Bestand.

**ARBEITSVERZEICHNIS — der einzige Pfad, unter dem du liest und schreibst:**

    /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests

Es gibt daneben eine Arbeitskopie des Repos unter
`/Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova`. **Fass die nicht an,
weder lesend noch schreibend.** Ein Worker vor dir hat dort gearbeitet statt in
seinem Worktree; das Aufräumen hat Zeit gekostet.

## Der rote Faden

Beide Aufgaben haben denselben Kern: **ein Test verspricht mehr, als er hält.**
Beide sind heute folgenlos, und beide würden genau dann zuschlagen, wenn sich
jemand auf sie verlässt.

Vorgeschichte, die du kennen musst: Sprint 21 hat die Kartenmitte zu einem
Gebiet mit unbegehbarem Gelände gemacht (Paket 21.7, Entscheidung D-109). Ein
adversarialer Gegenlese-Lauf hat die Arbeit abgenommen und dabei zwei Löcher
gefunden. Du schließt sie. Sein Bericht liegt unter
`reports/v8.6.0/sprint-21/08-kimi-gegenlesen-karte.md` — **lies ihn erst, wenn
du dir selbst ein Bild gemacht hast**, sonst übernimmst du seine Denkspur.

## Aufgabe 1 — der Erreichbarkeitstest sieht ein Feld unter einer Wand als erreichbar (Issue #126)

`tools/Nova.SimRunner.Tests/GlutrinneTerrainTests.cs` enthält
`Terrain_KeepsEveryFieldAndHeadquarterReachable_FromBothStarts`. Der Test
existiert genau dafür, dass eine spätere Kartenänderung nicht unbemerkt eine
Basis einsperrt.

`IntegrationField.Generate` sät die **Zielzelle ohne Begehbarkeitsprüfung**
(`Assets/_Project/Scripts/Simulation/Pathfinding/IntegrationField.cs`, um Zeile
50). Ein Ziel, das selbst auf einer unbegehbaren Zelle liegt, bekommt trotzdem
Kosten 0 und gilt als erreicht. Der Fall, gegen den der Test existiert, ist
damit genau der Fall, den er nicht sieht.

**Prüf das zuerst selbst nach.** Lies `IntegrationField.Generate` und bilde dir
ein eigenes Urteil, bevor du etwas änderst. Wenn die Behauptung nicht stimmt,
ist das ein vollwertiges Ergebnis — sag es.

**Wo die Behebung hingehört.** `Simulation/Pathfinding/` gehört dem
Einheitenstrang und ist für dich **verboten**. Die Behebung gehört deshalb in
den Test: er prüft die Begehbarkeit seiner Ziele selbst, bevor er die
Erreichbarkeit prüft. Das ist ohnehin die schärfere Variante, weil sie beide
Fehlerbilder trennt — „das Feld liegt auf einer Wand" ist ein anderer Befund als
„das Feld ist umbaut", und ein Test, der beide zu einem grünen Haken
verschmilzt, hilft niemandem.

Beweis, dass die Schärfung wirkt: **verschieb testweise ein Feld auf eine
Wandzelle und zeig, dass der Test rot wird** — vorher wäre er grün geblieben.
Nimm die Änderung wieder raus. Ein Wächter, den niemand rot gesehen hat, ist
eine Behauptung.

## Aufgabe 2 — der Spiegel der Geländetabelle ist in der CI ungeschützt

Das ist die wichtigere der beiden.

Die kanonische Geländetabelle steht zweimal im Repo, und das ist unvermeidlich:
`tools/Nova.SimRunner*` kompiliert nur `Core`, `Simulation`, `Networking`, `AI`
und `AI.Data` — `Gameplay/` ist **nicht** dabei (siehe
`tools/Nova.SimRunner/Nova.SimRunner.csproj`). Also gibt es

- `Assets/_Project/Scripts/Gameplay/Match/GlutrinneTerrainMap.cs` (Unity-Seite,
  die kanonische Quelle) und
- einen handgespiegelten Ausdruck in
  `tools/Nova.SimRunner/Determinism10000Scenario.cs` (headless-Seite).

Beide werden gepinnt: zellgenau und über eine gemeinsame FNV-1a-Prüfsumme. Die
headless-Spur pinnt ihren eigenen Spiegel — die fährt die CI. **Die Spur, die
den Gameplay-Ausdruck pinnt, liegt in `Assets/Tests/EditMode/` und läuft in
KEINER CI** (`.github/workflows/tests.yml` fährt ausschließlich
`dotnet test tools/Nova.SimRunner.Tests`).

**Folge: wenn jemand `GlutrinneTerrainMap.cs` einseitig ändert, bleibt die CI
dauerhaft grün — und Host und Gast rechnen verschiedene Karten.** Das ist ein
Desync, der sich als „die Einheiten laufen beim Gegner woanders" äußert und den
später niemand der Kartenarbeit zuordnet.

**Der Auftrag: schließ das Loch, ohne auf Unity zu warten.** Die Entscheidung,
ob die Unity-Tests in die CI kommen, ist eine Inhaberentscheidung mit
Lizenzkosten und liegt offen (#110). Bis dahin braucht es einen Schutz in der
Spur, die tatsächlich läuft.

> **Das Muster existiert im Bestand schon.** `NoFloatInSimulationTests` (in
> `tools/Nova.SimRunner.Tests/`) liest `Assets/_Project/Scripts/**` als
> **Quelltext** und prüft ihn, obwohl diese Dateien in dieser Assembly gar nicht
> kompiliert werden. Genau so kann die headless-Spur den Gameplay-Ausdruck
> erreichen, ohne ihn zu kompilieren.

Wie du das genau schneidest, entscheidest du — ich gebe dir das Ziel, nicht den
Entwurf. Ein paar Fragen, an denen sich die Qualität entscheidet:

- **Was genau pinnst du?** Die Konstanten allein (Mittelpunkt, Innen- und
  Außenradius, Lückenradius, Zellzahl) sind wenig Text und stabil. Das ganze
  Prädikat als Text zu pinnen wäre spröde — jede Umformatierung macht es rot.
  Es gibt einen Mittelweg; find ihn und begründe ihn.
- **Wie verhindert dein Wächter falschen Alarm?** Ein Schutz, der bei jeder
  harmlosen Docstring-Änderung rot wird, wird nach dem dritten Mal ignoriert.
- **Was fängt er NICHT?** Sag es ausdrücklich. Ein Quelltext-Wächter kann keine
  Semantik prüfen; er kann nur bemerken, dass sich etwas bewegt hat.
- **Und der wichtigste Punkt:** wenn er rot wird, muss aus der Fehlermeldung
  hervorgehen, **was zu tun ist** — nämlich beide Ausdrücke nachziehen, nicht
  den Wächter anpassen. Schreib das in die Meldung, nicht nur in den Docstring.

**Rot-Nachweis auch hier:** ändere testweise eine Konstante in
`GlutrinneTerrainMap.cs`, zeig die rote Meldung wörtlich, nimm die Änderung
zurück.

## Schreibhoheit — verbindlich

ERLAUBT:
  tools/Nova.SimRunner.Tests/          deine Tests und Wächter
  reports/v8.6.0/sprint-23/            nur deine eigenen Dateien

VERBOTEN — ohne Ausnahme:
  Assets/_Project/Scripts/**           komplett. Auch GlutrinneTerrainMap.cs:
                                       du LIEST sie, du änderst sie nicht
  Assets/Tests/**
  tools/Nova.SimRunner/                das Drehbuch und sein Spiegel
  .github/**  quality/**
  CHANGELOG.md  VERSION  ROADMAP.md  README.md  plans/**  global.json

Wenn deine Lösung eine Änderung außerhalb dieser Grenze braucht: **anhalten und
melden.** In dieser Sitzungsreihe haben fünf Worker vor dir angehalten statt zu
raten, und jede dieser Meldungen war wertvoller als eine Lösung.

**Den CHANGELOG fasst du nicht an** — Einzelschreiber. Schreib mir stattdessen
zwei Vorschlagstexte in den Report.

## Verifikation

    "/Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/.dotnet/dotnet" test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release

Das systemweite `dotnet` scheitert an `global.json` (8.0.318, `rollForward:
disable`) — nimm immer den absoluten Pfad oben. Er ist rechnerspezifisch und
gehört in keine committete Datei. **Ändere `global.json` niemals.**

Ausgangsstand: **736/736 grün**. Fahr die Kette vorher und nachher und schreib
beide Ergebnisse wörtlich in den Report.

## Was du NICHT tust

- Kein `git commit`, `git push`, `git add`, kein PR, kein `gh`-Aufruf.
- Keine Subagenten.

## Report

Markdown nach `reports/v8.6.0/sprint-23/`. Struktur:

  1. Aufgabe 1: stimmte die Behauptung? Was hast du geändert, und wie sieht der
     Rot-Nachweis aus?
  2. Aufgabe 2: dein Entwurf und warum dieser; was er fängt, was nicht
  3. Beide Rot-Nachweise wörtlich
  4. Testlauf vorher / nachher, wörtlich
  5. Was unbelegt bleibt
  6. Zwei CHANGELOG-Vorschlagstexte

Schließe mit:

  STATUS: DONE | BLOCKED
  - Befund 1
  - Befund 2
  - Befund 3

## Output

Beide Aufgaben erledigt, alle Sperrgrenzen eingehalten. Zusammenfassung:

**Aufgabe 1 (#126):** Die Behauptung stimmt — und das Loch liegt eine Ebene tiefer als beschrieben: nicht nur `IntegrationField.Generate` sät die Zielzelle ohne Begehbarkeitsprüfung (`IntegrationField.cs:44-53`), auch `RequestFlowField` und sogar `EconomySystem.TryAddField` validieren die Ziel-/Feldposition nicht. Ein Feld auf einer Wand registriert sich, sät sich mit Distanz 0 und gilt als „erreicht". Der Fix liegt wie vorgegeben im Test: `Terrain_KeepsEveryFieldAndHeadquarterReachable_FromBothStarts` prüft Feldziele jetzt selbst auf `IsWalkable`, mit eigener Meldung, die „Feld auf einer Wand" von „Feld umbaut" trennt. Rot-Nachweis: Feld 1 testweise auf Wandzelle (62,47) — alter Code grün, neuer Code rot (Meldung wörtlich im Report), Drehbuch byte-identisch zurückgenommen.

**Aufgabe 2:** Loch zuerst am lebenden Stand bewiesen: einseitiges `RingInnerRadius 14→13` in `GlutrinneTerrainMap.cs` → volle Suite **736/736 grün** — Host und Gast hätten verschiedene Karten gerechnet. Der neue `GlutrinneTerrainSourceGuardTests` folgt dem `NoFloatInSimulationTests`-Muster und pinnt zwei Schichten: die sechs Konstanten als geparste Werte gegen die kompilierte Referenz `CanonicalTerrainMirror`, und das `IsImpassable`-Prädikat beider Quellen whitespace-normalisiert Token für Token **gegeneinander** — bewusst nicht gegen ein drittes Literal, damit „rot" nur „die Kopien laufen auseinander" heißen kann und die Abhilfe zwingend „alle Kopien nachziehen" lautet (steht so in jeder Fehlermeldung, inkl. #110-Hinweis). Beide Beine einzeln rot nachgewiesen (Konstante 14→13; Operator `<`→`<=`), Umformatierung bleibt nachweislich grün. Was er nicht fängt (Semantik, `Apply`-Schleifen, Aufrufstellen), steht ausdrücklich im Report.

Den Gegenlese-Bericht habe ich nicht gebraucht: beide Befunde ließen sich aus dem Code selbst verifizieren — die Denkspur ist eigenständig.

Testläufe wörtlich im Report: vorher 736/736, nachher 739/739. Report: `reports/v8.6.0/sprint-23/02-claude-tests-halten-was-sie-versprechen.md`, inklusive zweier CHANGELOG-Vorschlagstexte. `git status` zeigt nur die zwei Testdateien und den Report.

STATUS: DONE
- Behauptung zu Aufgabe 1 bestätigt und verschärft: weder `IntegrationField.Generate` noch `RequestFlowField` noch `TryAddField` prüfen Begehbarkeit der Zielzelle — Test prüft sie jetzt selbst; Rot-Nachweis alt-grün/neu-rot wörtlich, Drehbuch-Manipulation revertiert
- CI-Loch bewiesen (einseitige Konstantenänderung in `GlutrinneTerrainMap.cs`: 736/736 grün) und per `GlutrinneTerrainSourceGuardTests` geschlossen: Konstanten geparst-gegen-kompiliert, Prädikat Token für Token zwischen beiden Quellen, Fehlalarm-Gegenprobe (Umformatierung) grün
- Suite 736/736 → 739/739 (beides wörtlich im Report); Sperrdateien byte-identisch; zwei CHANGELOG-Vorschläge im Report, CHANGELOG selbst unangetastet

## Exit

exit=0

## Progress

    [00:20] · Ich lese zuerst die relevanten Dateien und bilde mir ein eigenes Urteil, bevor ich den Gegenlese-Bericht anfasse.
    [00:20] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/tools/Nova.SimRunner.Tests/Glut…
    [00:20] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/Assets/_Project/Scripts/Simulat…
    [00:43] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/Assets/_Project/Scripts/Gamepla…
    [00:43] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/tools/Nova.SimRunner
    [00:52] · Jetzt der headless-Spiegel im Drehbuch und das Quelltext-Wächter-Muster.
    [00:52] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/tools/Nova.SimRunner/Determinis…
    [00:52] Glob     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/tools
    [00:59] · Jetzt das Quelltext-Wächter-Muster und die CI.
    [00:59] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/tools/Nova.SimRunner.Tests/NoFl…
    [00:59] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/.github/workflows/tests.yml
    [03:28] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/Assets/_Project/Scripts/Simulat…
    [03:28] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/Assets/_Project/Scripts/Simulat…
    [03:42] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/tools/Nova.SimRunner/Nova.SimRu…
    [03:42] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/tools/Nova.SimRunner/Determinis…
    [03:53] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/Assets/Tests/EditMode
    [03:56] Bash     ls "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/reports/v8.6.0/sprint-23/" …
    [04:11] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests" && "/Volumes/2TB_CodingPro…
    [04:14] agent +  Run full headless test suite (baseline)  (agent)
    [04:38] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/tools/Nova.SimRunner/Determinis…
    [04:50] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests" && sed -i '' 's/new FieldL…
    [04:59] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests" && "/Volumes/2TB_CodingPro…
    [05:37] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/tools/Nova.SimRunner.Tests/Glut…
    [05:47] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests" && "/Volumes/2TB_CodingPro…
    [05:59] · Rot-Nachweis 1 steht. Ich nehme die Drehbuch-Manipulation zurück und prüfe Grün:
    [06:03] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests" && sed -i '' 's/new FieldL…
    [07:03] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/tools/Nova.SimRunner.Tests/Pres…
    [08:25] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests" && sed -i '' 's/ public co…
    [08:33] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests" && "/Volumes/2TB_CodingPro…
    [08:36] agent +  Run full suite against tampered Gameplay source (expect all green = the hole)  (agent)
    [08:57] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests" && sed -i '' 's/ public co…
    [09:12] · Revert sauber. Jetzt schreibe ich den Wächter:
    [10:22] Write    /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/tools/Nova.SimRunner.Tests/Glut…
    [10:30] · Voller Lauf mit dem neuen Wächter:
    [10:30] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests" && "/Volumes/2TB_CodingPro…
    [10:33] agent +  Run full suite with new guard (expect 739 green)  (agent)
    [11:05] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests" && sed -i '' 's/ public co…
    [11:20] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests" && sed -i '' 's/ public co…
    [11:33] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests" && sed -i '' 's| return Ma…
    [11:41] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests" && "/Volumes/2TB_CodingPro…
    [11:50] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests" && git checkout -- Assets/…
    [12:10] · Der Baum enthält nur noch meine erlaubten Änderungen. Finaler voller Lauf:
    [12:10] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests" && "/Volumes/2TB_CodingPro…
    [12:13] agent +  Final full test suite run  (agent)
    [12:33] · 739/739 grün. Jetzt der Report:
    [12:34] Bash     ls "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/reports/v8.6.0/sprint-22/" …
    [12:43] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/CHANGELOG.md
    [14:40] Write    /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests/reports/v8.6.0/sprint-23/02-cla…
    [14:50] · Abschließende Kontrolle des Arbeitsstands:
    [14:50] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/tests" && git status --porcelain …
    [15:23] · Beide Aufgaben erledigt, alle Sperrgrenzen eingehalten. Zusammenfassung: **Aufgabe 1 (#126):** Die Behauptung stimmt — und das Loch liegt eine Ebene tiefer als…
    [15:24] finished
