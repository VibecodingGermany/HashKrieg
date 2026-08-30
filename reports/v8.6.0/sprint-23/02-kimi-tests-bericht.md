# Sprint 23 — Tests halten, was sie versprechen (Issues #126 / Spiegel-CI-Loch)

**Worker:** claude (Worktree `nova-wt/tests`, Branch `chore/s23-tests-halten-was-sie-versprechen`)
**Datum:** 2026-08-30
**Geändert:** ausschließlich `tools/Nova.SimRunner.Tests/GlutrinneTerrainTests.cs` (+14 Zeilen)
und neu `tools/Nova.SimRunner.Tests/GlutrinneTerrainSourceGuardTests.cs`.
Alle Sperrdateien sind nach den Rot-Nachweisen byte-identisch zurückgenommen
(`git status --porcelain` zeigt nur die beiden Testdateien und diesen Report).

---

## 1. Aufgabe 1 — der Erreichbarkeitstest sah ein Feld unter einer Wand als erreichbar

### Stimmte die Behauptung?

**Ja, vollständig — und das Loch liegt eine Ebene tiefer als angegeben.** Eigenes
Nachlesen vor jeder Änderung:

- `IntegrationField.Generate` (`Assets/_Project/Scripts/Simulation/Pathfinding/IntegrationField.cs:41-53`)
  prüft das Ziel nur auf `IsInBounds` und sät es dann mit Distanz 0 — ohne
  Begehbarkeitsprüfung. Die Wellenausbreitung prüft anschließend nur die
  **Nachbarzellen** (`IntegrationField.cs:73`), nie die Saatzelle. Eine Wandzelle
  als Ziel wird also mit 0 gesät, die Welle läuft aus der Wand heraus, und jeder
  Startpunkt liest eine endliche Distanz.
- Auch `PathfindingSystem.RequestFlowField` (`PathfindingSystem.cs:125-129`) fängt
  das nicht ab: nur `IsValid` und `IsInBounds`.
- Und auch `EconomySystem.TryAddField` (`EconomySystem.cs:311-316`) validiert die
  Feldposition nicht gegen das Kostfeld — ein Feld auf einer Wand registriert sich
  klaglos. Das Loch ist damit Ende-zu-Ende real, nicht nur ein Testartefakt.

### Was geändert wurde

Die Behebung liegt im Test (der Einheitenstrang `Simulation/Pathfinding/` blieb
unangetastet): `Terrain_KeepsEveryFieldAndHeadquarterReachable_FromBothStarts`
prüft jetzt jedes Feldziel auf `CostField.IsWalkable`, **bevor** das Flow-Feld
angefordert wird, mit eigener Fehlermeldung. Die beiden Fehlerbilder sind damit
getrennt benennbar: „das Feld liegt auf einer Wand" (Platzierungsdefekt) ist ein
anderer Befund als „das Feld ist umbaut" (die bisherige `Unreachable`-Assertion).
Die HQ-Türzellen brauchen die Prüfung nicht: `HqDoorCell` liefert per Konstruktion
nur Zellen, die das Kostfeld als begehbar meldet — Kommentar im Test sagt das
ausdrücklich.

### Rot-Nachweis (wörtlich)

Vorher/Nachher gegen denselben Defekt: Feld 1 testweise von (7,7) auf die Wandzelle
(62,47) verschoben (eine Zeile in `Determinism10000Scenario.cs`, danach
zurückgenommen — `git diff` auf `tools/Nova.SimRunner/` leer).

**Mit dem alten Testcode** blieb der Wächter grün — genau der Fall, gegen den der
Test existiert:

```
Bestanden!   : Fehler:     0, erfolgreich:     1, übersprungen:     0, gesamt:     1, Dauer: 27 ms - Nova.SimRunner.Tests.dll (net8.0)
```

**Mit der Schärfung**, identischer manipulierter Stand:

```
  Fehler Terrain_KeepsEveryFieldAndHeadquarterReachable_FromBothStarts [22 ms]
  Fehlermeldung:
     field 1 at (62,47) lies on impassable terrain — a unit can never stand on its own destination. Fix the field layout (or the terrain), not this assertion.
Assert.That(pathfinding.CostField.IsWalkable((ushort)field.GridPos.X, (ushort)field.GridPos.Y), Is.True)
  Expected: True
  But was:  False
```

Nach dem Revert des Drehbuchs: grün (`erfolgreich: 1`).

---

## 2. Aufgabe 2 — Quelltext-Wächter für den Gelände-Spiegel

### Das Loch, nachgewiesen am lebenden Stand

Bevor irgendetwas gebaut wurde: `RingInnerRadius = 14 → 13` in der kanonischen
Quelle `Assets/_Project/Scripts/Gameplay/Match/GlutrinneTerrainMap.cs`, voller
Suite-Lauf — **736/736 grün**. Host und Gast hätten ab diesem Commit verschiedene
Karten gerechnet, und kein Check in der CI hätte es gesehen. (EditMode-Spur pinnt
denselben FNV-1a-Literal `0x68A7C8644C9D06D5UL` gegen den Unity-Host
[`CanonicalMatchSetupTests.cs:469`], läuft aber in keiner CI — #110.)

### Der Entwurf: `GlutrinneTerrainSourceGuardTests.cs` (drei Tests)

Das Muster stand zweimal im Bestand: `NoFloatInSimulationTests` und
`PresentationSourceBoundaryTests` lesen Produktionsquellen als **Text** (Letztere
scannt ausdrücklich `Gameplay/**`). Der Wächter tut dasselbe und pinnt zwei
Schichten:

1. **Konstanten als geparste Werte.** Die sechs Deklarationen (`CentreX`,
   `CentreY`, `RingInnerRadius`, `RingOuterRadius`, `CornerGapMinRadius`,
   `ImpassableCellCount`) werden per Regex aus dem kommentarbereinigten
   Gameplay-Quelltext gelesen und gegen die **kompilierten** Konstanten der
   Test-Referenz `CanonicalTerrainMirror` verglichen (die wiederum per
   zellgenauem Test gegen das Laufzeitverhalten des Drehbuch-Spiegels gepinnt
   ist). Kein drittes Textexemplar der Zahlen — der Vergleich läuft
   Quelltext-gegen-kompilierten-Wert.
2. **Prädikat als normalisierter Tokenstrom.** Der Rumpf von
   `IsImpassable` wird aus **beiden** Quellen (Gameplay-Quelle und
   `Determinism10000Scenario.cs`) extrahiert, kommentarbereinigt und **vollständig
   whitespace-normalisiert**, dann Token für Token **gegeneinander** verglichen —
   bewusst nicht gegen ein drittes Literal im Wächter.
3. **Reichweiten-Selbstnachweis** (wie die „ScanReaches…"-Tests der Vorbilder):
   beide Dateien gefunden, beide Extraktionen nichtleer, mindestens die sechs
   Konstanten geparst — ein Pfad- oder Extraktionsfehler kann den Wächter nicht
   vakuumgrün machen.

### Warum genau dieser Mittelweg

- **Konstanten allein** hätten die Formeländerung `<` → `<=` (weitet jede Lücke
  um eine Zelle, ohne eine Konstante zu berühren) unsichtbar gelassen.
- **Ganzes Prädikat als Rohtext** wäre bei jeder Umformatierung rot geworden.
- Der gewählte Schnitt pinnt die **Bewegung zwischen den Kopien**, nicht deren
  Typografie: Kommentare werden geblankt, sämtlicher Whitespace entfernt. Und weil
  die beiden *Ausdrücke* gegeneinander laufen statt gegen ein Wächter-Literal,
  kann „rot" nur „die Kopien laufen auseinander" bedeuten — die Abhilfe ist damit
  zwingend „beide Kopien nachziehen", niemals „den Wächter anpassen". Genau das
  steht auch so in jeder Fehlermeldung (`MirrorRemedy`), inklusive des Hinweises,
  die EditMode-Spur lokal zu fahren (#110).

### Was der Wächter fängt — und was nicht

**Fängt (in der CI):** jede einseitige Änderung an Konstantenwerten oder am
Prädikat einer der beiden Geländetabellen; Konstanten-Umbenennungen (Parse-Schlag
mit eigener Meldung: der Name ist Spiegelvertrag).

**Fängt bewusst nicht:**
- **Semantik.** Ein konsistent-falsches Ändern *aller* Kopien in einem Zug sieht
  er nicht — dafür bleibt die EditMode-Prüfsummenspur zuständig (lokal laufen
  lassen, #110).
- **Die `Apply`-Stempelschleifen.** Die Rümpfe unterscheiden sich legitim
  (Gameplay trägt einen Null-Check); Inhalt und Schreibzahl sind über den
  zellgenauen Test und die Epoch-Pins verhaltensseitig abgedeckt.
- **Aufrufstellen** (`MatchBootstrap`, `BuildHost`) und eine eventuelle dritte,
  heute nicht existierende Geländequelle.
- **Formatierung und Docstrings** — Nachweis unten.

### Rot-Nachweise (wörtlich)

**Konstantenbein** — `RingInnerRadius 14 → 13` nur in der Gameplay-Quelle.
Genau einer der drei Wächter-Tests rot, die anderen zwei grün (Trennung der
Schichten sichtbar):

```
  Fehler GameplaySource_TerrainConstants_MatchTheHeadlessMirror [14 ms]
  Fehlermeldung:
     constant RingInnerRadius reads 13 in the Gameplay source but the headless mirror computes with 14. The canonical Glutrinne terrain exists as hand-mirrored copies: Assets/_Project/Scripts/Gameplay/Match/GlutrinneTerrainMap.cs (Unity host), GlutrinneTerrain in tools/Nova.SimRunner/Determinism10000Scenario.cs (headless lane) and CanonicalTerrainMirror in tools/Nova.SimRunner.Tests/GlutrinneTerrainTests.cs (pinned reference). One of them moved without the others — left standing, host and guest compute DIFFERENT maps and desync. Apply the change to EVERY copy (and keep the pinned checksums consistent); never silence this guard by editing the guard. The EditMode CanonicalMatchSetupTests pin the same content on the Unity host and are NOT in CI (#110) — run them locally.
Assert.That(actual, Is.EqualTo(expected))
  Expected: 14
  But was:  13
```

**Prädikatbein** — `return Math.Min(dx, dy) < CornerGapMinRadius;` → `<=` nur in
der Gameplay-Quelle:

```
  Fehler GameplaySource_TerrainPredicate_MatchesTheHeadlessMirrorTokenForToken [15 ms]
  Fehlermeldung:
     the IsImpassable predicate moved on one side only (comments and whitespace are ignored in this comparison). The canonical Glutrinne terrain exists as hand-mirrored copies: Assets/_Project/Scripts/Gameplay/Match/GlutrinneTerrainMap.cs (Unity host), GlutrinneTerrain in tools/Nova.SimRunner/Determinism10000Scenario.cs (headless lane) and CanonicalTerrainMirror in tools/Nova.SimRunner.Tests/GlutrinneTerrainTests.cs (pinned reference). One of them moved without the others — left standing, host and guest compute DIFFERENT maps and desync. Apply the change to EVERY copy (and keep the pinned checksums consistent); never silence this guard by editing the guard. The EditMode CanonicalMatchSetupTests pin the same content on the Unity host and are NOT in CI (#110) — run them locally.
  gameplay : intdx=Math.Abs(x-CentreX);intdy=Math.Abs(y-CentreY);intring=Math.Max(dx,dy);if(ring<RingInnerRadius||ring>RingOuterRadius){returnfalse;}returnMath.Min(dx,dy)<=CornerGapMinRadius;
  mirror   : intdx=Math.Abs(x-CentreX);intdy=Math.Abs(y-CentreY);intring=Math.Max(dx,dy);if(ring<RingInnerRadius||ring>RingOuterRadius){returnfalse;}returnMath.Min(dx,dy)<CornerGapMinRadius;
```

**Fehlalarm-Gegenprobe** — Prädikat hart umgebrochen (Zeilen verklebt, Ausdruck
gesplittet, Kommentar angehängt), ohne einen Token zu ändern:

```
Bestanden!   : Fehler:     0, erfolgreich:     3, übersprungen:     0, gesamt:     3, Dauer: 17 ms - Nova.SimRunner.Tests.dll (net8.0)
```

Alle Manipulationen an `GlutrinneTerrainMap.cs` wurden per `git checkout`
zurückgenommen; die Datei ist byte-identisch zum Ausgangstand.

---

## 3. Testlauf vorher / nachher (wörtlich)

Kommando (rechnerspezifischer dotnet-Pfad, steht in keiner committeten Datei):

```
"/Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/.dotnet/dotnet" test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release --nologo
```

**Vorher (Ausgangsstand):**

```
Bestanden!   : Fehler:     0, erfolgreich:   736, übersprungen:     0, gesamt:   736, Dauer: 13 s - Nova.SimRunner.Tests.dll (net8.0)
```

**Nachher (mit Schärfung und Wächter):**

```
Bestanden!   : Fehler:     0, erfolgreich:   739, übersprungen:     0, gesamt:   739, Dauer: 13 s - Nova.SimRunner.Tests.dll (net8.0)
```

Δ +3 Tests (die drei Wächter-Tests); Aufgabe 1 schärft einen bestehenden Test,
ohne die Zahl zu verändern.

---

## 4. Was unbelegt bleibt

- **Die EditMode-Spur steht weiterhin außerhalb der CI** (#110, Inhaberentscheidung
  mit Lizenzkosten). Der Wächter schließt das *Auseinanderlaufen* der Kopien, nicht
  die semantische Wahrheit des Inhalts gegen den Unity-Host — die bleibt an die
  lokal gefahrene `CanonicalMatchSetupTests`-Prüfsumme delegiert.
- **Lokale Umbenennungen im Prädikat** (z. B. `dx` → `deltaX`) machen das
  Prädikatbein rot, obwohl die Semantik steht. Das ist bewusst so (die Kopien
  sollen deckungsgleich lesbar bleiben) und in der Fehlermeldung als
  Nachzieh-Anweisung gerahmt — aber es ist ein bekanntes, seltenes
  Fehlalarm-Restrisiko, kein ausgeschlossenes.
- **Der Wächter liest nur zwei Dateien.** Eine künftige dritte Geländequelle
  (etwa ein Karteneditor) muss ihn kennen; der Klassen-Docstring sagt das.
- Der Rot-Nachweis für Aufgabe 1 manipulierte das Drehbuch eine Zeile weit;
  dass ein *Unity-seitiger* Feld-Layout-Fehler (`MatchBootstrap.FieldLayouts`)
  denselben Weg nähme, ist per Spiegel-Argument plausibel, aber nicht separat
  durchgespielt.

---

## 5. CHANGELOG-Vorschlagstexte (Einzelschreiber übernimmt)

### Vorschlag 1 (Kategorie `Behoben`)

- **Der Erreichbarkeits-Wächter der Glutrinne sah Felder auf Wandzellen als
  „erreichbar" (#126).** `IntegrationField.Generate` sät die Zielzelle ohne
  Begehbarkeitsprüfung mit Distanz 0; ein Feld, das selbst auf einer Wand liegt,
  galt damit als verbunden. `Terrain_KeepsEveryFieldAndHeadquarterReachable_FromBothStarts`
  prüft die Begehbarkeit seiner Feldziele jetzt selbst, bevor er die Erreichbarkeit
  prüft — „Feld liegt auf einer Wand" und „Feld ist umbaut" sind ab jetzt zwei
  getrennt benannte Befunde. Rot nachgewiesen durch testweises Verschieben eines
  Feldes auf eine Wandzelle (alter Code grün, neuer Code rot, Manipulation
  zurückgenommen).

### Vorschlag 2 (Kategorie `Hinzugefügt`)

- **Quelltext-Wächter für den Gelände-Spiegel in der CI-Spur.** Die kanonische
  Geländetabelle liegt zwangsläufig zweimal im Repo (`Gameplay/Match/GlutrinneTerrainMap.cs`
  und der Spiegel in `Determinism10000Scenario`); die Spur, die den Gameplay-Ausdruck
  pinnt, lief bisher in keiner CI. `GlutrinneTerrainSourceGuardTests` liest beide
  Kopien als Quelltext (Muster von `NoFloatInSimulationTests`) und pinnt die
  Konstanten als geparste Werte sowie das `IsImpassable`-Prädikat
  whitespace-normalisiert Token für Token gegeneinander. Eine einseitige
  Kartenänderung macht die CI ab sofort rot — vorher blieb sie dauerhaft grün,
  während Host und Gast verschiedene Karten rechneten (Desync). Die
  Fehlermeldung nennt die Abhilfe: alle Kopien nachziehen, nie den Wächter
  anpassen; die EditMode-Prüfsummenspur bleibt bis #110 lokal zu fahren.

---

STATUS: DONE
- Behauptung zu Aufgabe 1 stimmt und liegt eine Ebene tiefer: weder `IntegrationField.Generate` noch `RequestFlowField` noch `EconomySystem.TryAddField` prüfen die Ziel-/Feldzelle auf Begehbarkeit — Feld auf Wand registriert, sät und „erreicht" sich klaglos; Test prüft Zielbegehbarkeit jetzt selbst, Rot-Nachweis wörtlich (alt grün / neu rot / Revert grün)
- CI-Loch am lebenden Stand bewiesen (einseitiges `RingInnerRadius 14→13` in `GlutrinneTerrainMap.cs`: 736/736 grün) und mit `GlutrinneTerrainSourceGuardTests` geschlossen: Konstanten als geparste Werte gegen die kompilierte Referenz, Prädikat whitespace-normalisiert Token für Token zwischen beiden Quellen, plus Reichweiten-Selbstnachweis; beide Beine einzeln rot nachgewiesen, Umformatierung bleibt grün
- Suite vorher 736/736, nachher 739/739, beides wörtlich im Report; alle Sperrdateien byte-identisch zurückgenommen (`git status` zeigt nur die zwei Testdateien und den Report); Report: `reports/v8.6.0/sprint-23/02-claude-tests-halten-was-sie-versprechen.md` inkl. zweier CHANGELOG-Vorschläge
