---
Bericht: 03-kimi-buildzone-overlay (Sprint 21)
Worker: Kimi K3 (cc-Backend, --mode rw), 2026-08-18
Auftraggeber: Orchestrator
---

> Hinweis: Der Anfang der Worker-Ausgabe wurde beim Einsammeln abgeschnitten
> (`| tail -30` beim Dispatch, Fehler des Orchestrators). Erhalten sind die
> Entscheidungen, das Testergebnis und das Verdikt.

| `Assets/_Project/Scripts/Simulation/Construction/ConstructionSystem.cs` | **Minimal:** `IsInsideBuildInfluence` und `HasMinimumBuildingSpacing` von `private` auf `public`, plus Docstrings. Keine Verhaltensänderung — die Methodenkörper sind Byte-identisch. |
| `Assets/_Project/Scripts/Presentation/UI/RtsDeviceInput.cs` | O-Taste als Toggle (`BuildZoneOverlayPinned`, reiner View-State), Handler in `Update()` (greift auch während Platzierung/Pick-Modus), Hilfetext-Legende ergänzt. |
| `Assets/_Project/Editor/BootstrapSceneGenerator.cs` | Ein Block (7 Zeilen) in `CreateUiObject`, direkt hinter der `PlacementGhostView`-Verdrahtung — rebase-freundlich. |
| `Assets/Tests/EditMode/Simulation/BuildZoneOverlayQueryTests.cs` (+`.meta`) | **Neu.** Zwei Tests, die den Overlay-Vertrag pinnen. |

### Getroffene Entscheidungen

1. **Die Regel wird abgefragt, nicht nachgebaut.** Jeder Texel ist die Antwort von `ConstructionSystem.IsInsideBuildInfluence` bzw. `HasMinimumBuildingSpacing` für einen 3×3-Footprint-Ursprung auf dieser Zelle — exakt die beiden Reads, die `ValidatePlacement` selbst konsumiert. Kein Radius, keine Ankerliste im Overlay. Wenn D-108 die Ankerliste öffnet, folgt das Bild von allein.
2. **Drei Zustände:** ausserhalb der Zone = kein Tint; innerhalb + Abstand gewahrt = Grün (Alpha 26); innerhalb + Mindestabstand verletzt = Orange (Alpha 44). Genau die Unterscheidung „innerhalb und trotzdem gesperrt“, die der Testbericht vermisste. Terrain-Begehbarkeit und Feldabstände werden bewusst **nicht** gemalt (Feldabstand ist rollenabhängig — die Refinery invertiert ihn; der Ghost liefert das Vollverdikt am Cursor).
3. **Sichtbarkeit:** automatisch während `PlacementModeActive`, zusätzlich per **O** umschaltbar (O war frei: B/C/V/T/G/F/Y Gebäude, Q/U/N/E/D Einheiten, S/A/H/R/P Befehle, Z/X/Leertaste Kamera, F3–F5 Debug). Der Toggle sitzt in `Update()`, nicht in `HandleOrders`, damit er auch im Platzierungsmodus lebt.
4. **Performance:** Repaint-Kadenz 4 Hz nur solange sichtbar (Bauzustand ändert sich nur auf Tick-Events), Sofort-Repaint beim Einblenden; `FilterMode.Point`, damit Grün/Orange nicht zu einem bedeutungslosen Drittton verschwimmen. Höhe 0.05: über Fog (0.04), unter den Markern (0.06+).
5. **Kartenrand:** Ursprungszellen, deren Footprint die Karte verlassen würde, bleiben leer (Kartengeometrie über die öffentlichen Konstanten, nicht die Zonenregel).
6. **Teststrategie:** Der EditMode-Test pinnt nur die *Relation* (beide Reads sind notwendig für `Applied`; das Paar unterscheidet „draussen“ von „drin, aber gesperrt“) — nie Radiuswerte oder Ankerrollen, damit er D-108 unverändert übersteht. Verifiziert habe ich ihn über eine **transiente** Kopie in der .NET-Spiegel-Lane (kompiliert, 2/2 grün, danach entfernt — `tools/` ist im Enddiff unberührt).

### Testergebnis

**725/725 grün** (`dotnet test tools/Nova.SimRunner.Tests`), davon 0 Fehler, 0 übersprungen. Die „723“ aus dem Auftrag ist veraltet: die Baseline auf HEAD (9281341) zählt bereits **725** — die zwei Kapazitätstests aus 21.1/#92 kamen mit Merge #100 dazu. Gegenprobe auf jungfräulichem HEAD in einem temporären Worktree bestätigt: 725 vorher = 725 nachher, keine Regression. Der mitlaufende `PresentationSourceBoundaryTests`-Scan akzeptiert die neue View-Datei.

### Nicht prüfbar

- **Unity-Kompilierung** der Präsentations- und Editor-Dateien sowie der EditMode-Testdatei (kein Unity hier). Abgemildert: sämtliche verwendeten Symbole/Signatures sind aus funktionierenden Geschwisterdateien gespiegelt (`FogOfWarOverlayView`, `PlacementGhostView`, `ConstructionSystemTests`), asmdef-Referenzen reichen aus, und der Test wurde in der identischen .NET-Quell-Lane tatsächlich kompiliert und ausgeführt.
- **Optik** (Farbwerte, Höhe, Lesbarkeit im laufenden Spiel) — Werte sind SerializeField-Defaults und im Inspector nachjustierbar.
- `./.dotnet/dotnet` existiert im Worktree nicht (untracked SDK); verwendet wurde das gepinnte SDK **8.0.318** aus dem Haupt-Checkout — exakt die Version, die `global.json` verlangt.

STATUS: DONE
- Overlay fragt ausschliesslich die beiden Sim-Reads ab (kein Regel-Nachbau); D-108-Ankeränderung erbt es gratis; ConstructionSystem-Diff ist reine Sichtbarkeit + Docstrings, Körper unverändert.
- Testgate grün: 725/725, Baseline-Vergleich auf sauberem HEAD bestätigt keine Regression („723“ im Auftrag war um die zwei 21.1-Tests veraltet).
- Zwei Zustände unterscheidbar gemalt (Grün = baubar, Orange = in der Zone, aber Mindestabstand verletzt); Sichtbarkeit: Platzierungsmodus + O-Toggle, Legende ergänzt, Szene mit einem 7-Zeilen-Block verdrahtet.
- Nicht verifizierbar blieben Unity-Kompilierung der UI-/EditMode-Dateien und das visuelle Ergebnis — beides oben transparent gemacht.
/Users/denniswestermann/.claude/skills/kimi/scripts/kimi-agent.sh: line 270: hen: command not found

[exited with code 0]
