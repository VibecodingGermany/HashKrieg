---
Bericht: 02-kimi-version-hud (Sprint 21)
Worker: Kimi K3 (cc-Backend, --mode rw), 2026-08-18
Auftraggeber: Orchestrator
---

> Hinweis: Der Anfang der Worker-Ausgabe wurde beim Einsammeln abgeschnitten
> (`| tail -30` beim Dispatch, Fehler des Orchestrators). Erhalten sind die
> Entscheidungen, das Testergebnis und das Verdikt.

| `Assets/Tests/PlayMode/MainMenuTests.cs` | **Reparatur nötig:** `MenuDocument()` nutzte `FindAnyObjectByType<UIDocument>()` — mit zwei UIDocuments in der Szene wäre das mehrdeutig; findet jetzt per GameObject-Name „MainMenu" |

### Getroffene Entscheidungen

1. **Eigenes UIDocument auf eigenem GameObject** statt Einklinken ins Menü: `MainMenuController.BuildTree()` ruft `root.Clear()` auf und würde das Badge löschen; die IMGUI-Schicht fällt unter #102. Das Badge teilt sich nur die PanelSettings mit dem Menü.
2. **`sortOrder = 1` in `Awake`** (Menü bleibt 0) — zwingend, sonst läge das Badge *unter* dem Vollbild-Key-Art und wäre im Menü unsichtbar.
3. **Editor zeigt immer `dev (Editor)`**, auch wenn ein Stempel von einem früheren Packaging-Lauf im Arbeitsbaum liegt (`Application.isEditor`-Zweig): Eine Editor-Session ist nicht dieser Commit.
4. **Dateiinhalt ist `$COMMIT`** aus den Skripten — exakt `git rev-parse --short HEAD`, plus `-dirty` bei schmutzigem Baum, konsistent mit Info.plist- und Archiv-Stempel derselben Skripte.
5. **Fehlende Datei → `dev`, nie ein Log** (try/catch wie das bestehende `BuildInfo.Load`).
6. **Optik** am Menü orientiert: Rajdhani-Regular, Body-Farbe `(0.88, 0.91, 0.95)`, Opazität 0.55, 12 px, 10/8 px Rand; Werte als `[SerializeField]`-Defaults auf der Komponente („the generator wires, it does not tune").

### Nicht prüfbar (kein Unity hier)

- **Kompilieren:** Alle benutzten Symbole gegen Bestandscode gegengeprüft (`Label(string)`-Ctor + Objekt-Initializer wie `MainMenuController.cs:312`, `FontDefinition.FromFont`, `style.display.value`, `FindObjectsByType<T>(FindObjectsInactive)`-Overload wie `MainMenuTests.cs:152`). `UIDocument.sortOrder` (get/set) ist aus der Unity-Doku, nicht gegen Projektnutzung verifiziert, weil bisher niemand es benutzt.
- **Szene nicht regeneriert:** `Bootstrap.unity` enthält das Badge erst nach „Tools/Project Nova/Create Bootstrap Scene". Bis dahin schlägt der neue PlayMode-Test fehl — mit lauter Anleitung, wie es die bestehenden Tests bei veralteter Szene auch tun.
- **Packaging-Läufe** nicht ausgeführt (nur `bash -n`).
- **Rajdhani-Glyphe `·` (U+00B7)** nicht verifiziert — bei Fehlen greift der Theme-Font; Zeichen steht exakt so in der Vorgabe.
- **Visuelle Überlappung** mit Minimap/Build-Bar nicht gesehen (10/8 px Rand, gedämpft).

### Befund außerhalb des Auftrags

Es existiert bereits ein paralleler Mechanismus: `BuildCommitStamp` (Nova.Editor, `IPreprocessBuildWithReport`) stempelt `NovaBuildCommit.txt` bei **jedem** Player-Build, `BuildInfo.Commit` (Nova.Gameplay.Match) liest ihn (Lobby, D-094). Der Auftrag verlangte ausdrücklich `BuildInfo.txt` aus den Packaging-Skripten — so gebaut, Bestand unangetastet. Konsequenz, die ihr kennen solltet: Ein Build über die Unity-GUI (ohne Skripte) zeigt `dev`, obwohl `NovaBuildCommit.txt` frisch gestempelt wäre; und ein nach einem Packaging-Lauf im Arbeitsbaum liegender `BuildInfo.txt`-Stempel würde in einem späteren GUI-Build dessen alten Hash zeigen. Alternative wäre gewesen, das Badge auf `BuildInfo.Commit` lesen zu lassen — nicht der Auftrag, daher nur als Hinweis.

STATUS: DONE
- VersionBadge als eigenes UIDocument/GameObject umgesetzt: immer sichtbar (Menü + Match), unten links, gedämpft, `PickingMode.Ignore`, String einmalig in `Start()` — Format Editor `v0.21.0 · dev (Editor)`, Build `v0.21.0 · <hash>` aus `Resources/BuildInfo.txt`, fehlend → `dev` ohne Log.
- `bundleVersion` auf `0.21.0` gesetzt; beide Packaging-Skripte stempeln `BuildInfo.txt` vor dem Unity-Aufruf (`$COMMIT` inkl. `-dirty`); Datei + Meta sind git-ignoriert.
- `MainMenuTests.MenuDocument()` repariert (zwei UIDocuments machten `FindAnyObjectByType` mehrdeutig); neuer `VersionBadgeTests`-PlayMode-Test pinnt Format, Nicht-Klickbarkeit, Dokument-Unabhängigkeit und Sortierung.
- Nicht verifizierbar ohne Unity: Kompilieren, Szenen-Regeneration (Badge fehlt in `Bootstrap.unity`, bis der Generator läuft — neuer Test schlägt bis dahin bewusst laut fehl), `UIDocument.sortOrder`-Setter zur Laufzeit, Rajdhani-Glyphe `·`, optisches Ergebnis.
- Nebenbefund: Mit `BuildCommitStamp`→`NovaBuildCommit.txt`→`BuildInfo.Commit` (D-094) existiert ein zweiter, build-hook-basierter Stempelmechanismus; GUI-Builds ohne Skripte zeigen im Badge `dev`.
/Users/denniswestermann/.claude/skills/kimi/scripts/kimi-agent.sh: line 270: hen: command not found

[exited with code 0]
