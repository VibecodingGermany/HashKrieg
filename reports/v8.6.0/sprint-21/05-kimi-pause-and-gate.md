---
Bericht: 05-kimi-pause-and-gate (Sprint 21)
Worker: Kimi K3 (cc-Backend, --mode rw), 2026-08-18
Auftraggeber: Orchestrator
---

> Hinweis: Der Anfang der Worker-Ausgabe wurde beim Einsammeln abgeschnitten
> (`| tail -30` beim Dispatch, Fehler des Orchestrators). Erhalten sind die
> Entscheidungen, das Testergebnis und das Verdikt.

# Bericht — Paket 21.8: Pausemenü (#105) und der Eingabe-Riegel (#102)

## Dateiliste

| Datei | Änderung |
|---|---|
| `Assets/_Project/Scripts/Gameplay/UI/ModalSurfaceLink.cs` (+ `.meta`) | **Neu.** Statischer Kanal nach `HudPointerLink`-Vorbild: `Open` / `Publish(bool)` / `Reset()`. Semantik: „eine modale Oberfläche besitzt die Eingabe" — ausdrücklich nicht „Match läuft nicht". Pro Frame publiziert, kein Latch. |
| `Assets/_Project/Scripts/Presentation/UI/PauseMenuHud.cs` (+ `.meta`) | **Neu.** Das Pausemenü: ESC/P öffnet und schließt, hält lokale Matches an (`MatchRunner.PauseMatch`, Relay wird gar nicht erst aufgerufen) und setzt nur fort, was es selbst angehalten hat. Einträge: **Fortsetzen**, **Zum Hauptmenü** (`MainMenuController.ReturnToMenu`), **Spiel beenden** (`MainMenuController.Quit`, jetzt `public`). **Einziger Setzer** von `ModalSurfaceLink` (aggregiert eigenen Zustand + `MatchFrameHud.ModalOpen`); `OnDisable` resettet den Kanal — die Wurzel-Abschaltung auf dem Weg ins Menü kann so nie ein hängendes `true` hinterlassen. Liegt auf der UI-Wurzel (Generator), die Welt bleibt darunter sichtbar. |
| `RtsDeviceInput.cs` | Modal-Gate in `Update` direkt hinter dem `IsMenuVisible`-Ausstieg; Gesture-Cleanup beim Ingress-Rebind; P-Block aus `HandleOrders` entfernt (Pause gehört jetzt PauseMenuHud); `LastGestureCancelFrame` für die ESC-Schichtung (Geste zuerst, Menü beim nächsten Druck — RtsDeviceInput läuft auf -200 zuerst); Legende aktualisiert. |
| `MatchFrameHud.cs` | `DrawPausePanel` entfernt (Pause-Oberfläche ist jetzt das Pausemenü; alle gestoppten-Kernel-Zustände haben weiterhin eine Oberfläche: Resultat, Netzwerk, Pausemenü). Neu: `ModalOpen`-Property, einmal pro Frame in `Update` via `ComputeModalState()` abgeleitet — exakt die bisherige OnGUI-Bedingung, Resultat + Netzwerk-Panels. OnGUI zeichnet aus dem einmal abgeleiteten Zustand. |
| `MainMenuController.cs` | `_debugHud`-Feld durch `_gameplayHudRoot` (GameObject) ersetzt; `SetGameplayLayerActive` schaltet jetzt **die Wurzel** plus separat das Kamera-Rig (liegt auf „Main Camera"). Awake-Fallback findet die Wurzel über `RtsDeviceInput` für Szenen aus before-root-Generationen. `Quit()` public. `ReturnToMenu` ruft `PauseMatch` nicht mehr bei Relay-Matches auf (der Weg „laufende Netzpartie verlassen" ist jetzt erreichbar; die Verweigerung würde sonst absichtlich einen Error loggen — Match tickt hinter dem Menü weiter bis zum nächsten Start, wie ein lokal pausiertes). |
| `BootstrapSceneGenerator.cs` | `CreateUiObject` legt `PauseMenuHud` auf die Wurzel und verdrahtet `_runner`/`_matchFrame`/`_input`; `CreateMainMenuObject` verdrahtet `_gameplayHudRoot` statt `_debugHud` und `pauseMenu._menu`. |
| `Assets/Tests/PlayMode/MainMenuTests.cs` | DebugHud-Assertions von `.enabled` auf `gameObject.activeInHierarchy` umgestellt (der Schalter ist jetzt die Wurzel, nicht die Komponente — alter Assert würde sonst fehlschlagen); veraltete „no pause menu"-Meldung korrigiert. |

## Die drei Fehler aus Punkt 3

1. **Ausstiegsklick armiert #102 selbst:** Behoben durch das Gate, nicht durch eine Rect-Liste: `ModalSurfaceLink.Open` → `RtsDeviceInput.Update` steigt aus, bevor `HandleSelection`/`SelectSingle` laufen. Klicks auf „Hauptmenü"/„Fortsetzen" (Resultat-, Netzwerk-, Pause-Panel) selektieren nichts mehr hinter dem Panel; der Drag-State wird fallen gelassen. Der Kamera-Randscroll fällt gratis mit ab, weil das Gate `HudPointerLink.Publish(true)` publiziert.
2. **`_placementMode` überlebt den Menüwechsel:** Doppelt geräumt — (a) der Ingress-Rebind (`EnsureDispatcher`) löscht jetzt `_placementMode`, `_pendingOrder` und `_dragActive` zusammen mit der Auswahl, damit wandert kein Ghost in die nächste Runde; (b) das Gate disarmt armierte Gesten in dem Frame, in dem ein Modal aufgeht (mit Statusmeldung), damit kein Ghost armiert unter einem Panel schwebt.
3. **P-Taste ohne Riegel:** P/ESC sind aus `RtsDeviceInput` heraus in `PauseMenuHud` umgezogen. Pausiert heißt jetzt: Pausemenü offen → Kanal `true` → Selektion, sämtliche Hotkeys (S/A/H/R/Q/B/U…) und die Harvester-Eskorte feuern nicht mehr in den gestoppten Kernel.

Dazu der strukturelle #102-Fix: `SetGameplayLayerActive(false)` schaltet die ganze Wurzel — BuildMenuHud, CommandCardHud und MinimapHud (Audit-Klasse (c)) können über dem Menü weder zeichnen noch klicken, ohne je namentlich katalogisiert worden zu sein.

## „Einstellungen" — bewusst kein zweiter Dialog

Die Einstellungsmaske des Hauptmenüs ist fest in dessen `_screen`-Baum gebaut (`ShowSettings` toggelt gegen das Haupt-Panel mit „Neues Spiel"; „Zurück" führt dorthin). Sie mitten im Match zu zeigen hieße: ganzer Menü-Screen inkl. Key Art über dem Spiel, `IsMenuVisible`-Semantik gebrochen — die Verrenkung, vor der der Auftrag warnt. Stattdessen: drei funktionierende Einträge plus ehrliche Hinweiszeile im Panel („Einstellungen (Ton, Bild, Auflösung) gibt es im Hauptmenü."). Ein Eintrag weniger, keine driftende Zweitmaske.

## Testergebnis

`dotnet test tools/Nova.SimRunner.Tests` (dotnet aus dem Haupt-Checkout; der Worktree hat kein `.dotnet`): **725/725 grün, 0 Fehler.** Der Auftrag nannte 723 — der Branch-Stand (9281341) trägt bereits zwei Tests mehr; meine Änderungen kompilieren ohnehin nicht in dieses Projekt hinein (es referenziert nur Core/Simulation/AI/Networking), die Oberfläche hat nichts bewegt.

## Nicht prüfbar gewesen (ausdrücklich)

- **Kein Unity-Lauf:** Keine Unity-Assembly kompiliert. Symbolnamen/Signaturen sind doppelt gegen die Bestandsdateien geprüft (`PauseMatch()`/`StartMatch()` liefern `bool` — wird genutzt bzw. ignoriert wie im Bestand; `HudChrome.OpaquePanelStyle` ist assembly-intern erreichbar), aber ein Tippfehler fiele hier nicht auf.
- **`Bootstrap.unity` ist NICHT regeneriert** (Maschinenausgabe, Unity nötig). Bis jemand `Tools/Project Nova/Create Bootstrap Scene` laufen lässt, enthält die committete Szene kein `PauseMenuHud` → kein Pausemenü und kein publizierter Riegel (Resultat-Panels zeichnen weiterhin, das Klickdurchfall-Problem bleibt in der alten Szene bestehen). Der Wurzel-Schalter heilt die alte Szene dagegen selbst (Awake-Fallback über `RtsDeviceInput`), sodass die angepassten `MainMenuTests` auch gegen die alte Szene korrekt sind. **Es gibt bewusst keinen neuen PlayMode-Test fürs Pausemenü** — er hinge an der regenerierten Szene und liefe hier auf ein falsches Rot.
- **PlayMode-Tests nicht ausgeführt** (brauchen Unity headless-with-graphics) — die Anpassung in `MainMenuTests.cs` ist nur am Mechanismus gespiegelt, nicht gelaufen.
- **Editor-Assembly** (`BootstrapSceneGenerator.cs`) ebenfalls unkompiliert geprüft.
- Reihenfolge-Annahme verifiziert am Bestand: `RtsDeviceInput` trägt `DefaultExecutionOrder(-200)`, läuft also vor `PauseMenuHud.Update` — die ESC-Schichtung baut darauf. Kanal-Latenz ist konstruktionsbedingt 1 Frame (wie HudPointerLink).
- `DebugHud.cs` unangetastet. Nachlese-Befund: nur `Awake`/`Update`/`OnGUI`, kein `Start`/`OnEnable`/`OnDisable` mit Seiteneffekten; `_visible` (F3) überlebt `SetActive` — der Wurzel-Schalter ist exakt verhaltensgleich zum alten `enabled`-Toggle. Nichts an den anderen Beitragenden zu melden.
- Kamera während des Pausemenüs: Pfeiltasten/Zoom/Rotation bleiben absichtlich bedienbar (Kernel steht, Karteninspektion im Pausezustand ist üblich); nur der Randscroll-Unfall ist gegaten. Falls unerwünscht, wäre das ein Folgepunkt.

STATUS: DONE
- Eingabe-Riegel steht: `ModalSurfaceLink` (pro Frame, ein Setzer, OnDisable-Reset) gatet `RtsDeviceInput.Update` komplett — Selektion, Hotkeys und Harvester-Eskorte feuern nicht mehr in den gestoppten Kernel, und Klicks auf modale Panels selektieren nichts mehr dahinter (#102-Selbstarmierung tot).
- `_placementMode`/`_pendingOrder` überleben weder Modal-Öffnung noch Ingress-Rebind — der Bau-Ghost wandert nicht mehr ins Menü oder in die nächste Runde.
- Pausemenü (ESC/P) mit Fortsetzen / Zum Hauptmenü / Spiel beenden steht; „Einstellungen" ehrlich als Hinweis statt driftender Zweitmaske; `SetGameplayLayerActive` schaltet jetzt die UI-Wurzel (plus Kamera-Rig separat) — Cockpit liegt nicht mehr über dem Hauptmenü.
- Nicht prüfbar ohne Unity: Kompilierung der Unity-/Editor-Assemblies, PlayMode-Tests, und `Bootstrap.unity` muss regeneriert werden, sonst existieren Pausemenü und Riegel in der Szene schlicht nicht (Wurzel-Schalter heilt die alte Szene per Awake-Fallback). SimRunner: 725/725 grün.
/Users/denniswestermann/.claude/skills/kimi/scripts/kimi-agent.sh: line 270: hen: command not found

[exited with code 127]
