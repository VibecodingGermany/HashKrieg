---
Bericht: HUD-Zustandsaudit (Paket 21.8, #102 / #105 / #103)
Worker: Kimi K3 (cc-Backend, read-only), 2026-08-18
Auftraggeber: Orchestrator
---

> **Hinweis zur Vollständigkeit:** Die Abschnitte 1 (Inventar) und 2 (Klassen-
> einteilung) fehlen — sie wurden beim Einsammeln der Worker-Ausgabe
> abgeschnitten (`| tail -30` beim Dispatch, Fehler des Orchestrators, nicht des
> Workers). Erhalten sind die Abschnitte 3 bis 5 samt Verdikt; sie tragen die
> Befunde, auf denen Paket 21.8 gebaut wird.


## 3. Klasse (b): Warum geht es heute gut, und wann kippt es?

- **`PlacementGhostView`** — Heute gut, weil der Welt-Space-Ghost hinter dem opaken Menü-Hintergrund unsichtbar ist. Kippt zweifach: (i) ein Pausemenü, das die Welt (abgedunkelt) sichtbar lässt, zeigt den scharfen Ghost mitten im Modalzustand — der Armiert-Zustand `_placementMode` (RtsDeviceInput.cs:164) überlebt `ReturnToMenu` ungelöscht, weil nur die *Auswahl* beim Ingress-Rebind geräumt wird (RtsDeviceInput.cs:468); (ii) derselbe Überlebensweg trägt einen armierten Ghost sogar in die **nächste** Runde („Neues Spiel" → `RestartMatch`). Das ist die #102-Annahme in Reinform: der Riegel ist Eingabezustand, nicht Oberflächenzustand.
- **`ConstructionSiteMarkerView`** — Heute gut, weil Welt-Space (verdeckt) und der Puls (`Time.time`, :69) unsichtbar vor sich hin läuft. Kippt beim selben Szenario wie der Ghost (sichtbare Welt unter Pausemenü); bis dahin nur vergeudete Reads. Kein `IsRunning`-Riegel — unter P-Pause pulsiert es weiter, was heute harmlos ist.
- **`FogOfWarOverlayView`** — Heute gut, weil verdeckt und der Repaint an `fog.LastRecomputeTick` hängt: pausierter Kernel → kein neuer Tick → null Arbeit. Die Fog-Instanz-Wache (:96) heilt den Restart selbst. Kippt nur bei transparentem/ Welt zeigendem Overlay — und selbst dann wäre stehende Fog-Karte kosmetisch korrekt.
- **`CombatEffectController`** — Heute gut, weil Effekte unscaled ≤ 1,5 s leben und ohne Ticks keine neuen Events kommen. Kippt praktisch nicht; maximal spielt ein Todes-Sound in die Menü-Musik hinein, wenn `ReturnToMenu` exakt auf einen Kill fällt.
- **`GlutrinneBlockoutView`** — Heute gut, weil verdeckt und 0,5-s-Kadenz billig. Sein Docstring (:139-141) nennt das Menü sogar — deckt aber nur „noch keine Economy" ab, nicht „Economy überlebt pausiert": es stagt die Scherben hinter dem Menü weiter. Kippt nicht sichtbar; es ist die erste Komponente, deren Docstring die falsche Annahme bereits dokumentiert.
- **`FlowFieldDebugView`** — Heute gut, weil Gizmos nur in der Editor-Scene-View existieren. Kippt nie im Build.
- **`RtsDeviceInput` (OnGUI + Panel-Klicks)** — Heute gut, weil `Update` bei `IsMenuVisible` aussteigt und das Drag-Rechteck einen aktiven Drag braucht. Kippt an zwei Stellen: (i) Die modalen Panels von `MatchFrameHud` (Resultat, Pause) stehen **nicht** in `IsPointerOverHud` (RtsDeviceInput.cs:1102-1107 kennt nur Baukarte, Karte, Minimap) — ein Klick auf „Hauptmenü" läuft in `Update` zuerst durch `HandleSelection`/`SelectSingle` (:819, :1354) und **verändert die Auswahl hinter dem Panel**, bevor `OnGUI` den Button feuert. Der Ausstiegsklick selbst kann also die Auswahl erzeugen, die `CommandCardHud` dann über dem Menü zeichnet — #102 armiert sich teilweise selbst. (ii) **P-Pause hat gar keinen Riegel**: `IsMenuVisible` ist false, also laufen Selektion, sämtliche Hotkeys (S/A/H/R/Q/B/U…) und die Harvester-Eskorte (:634) in den Ingress des gestoppten Kernels weiter — Befehle stauen sich bis zum Resume. Genau hier bricht das geplante Pausemenü mit „derselben Anforderung" ein: für den Menü-Zustand existiert ein Signal, für den Pause-Zustand keines.

## 4. Zustandssignale — was ist heute abfragbar?

| Signal | Ort | Bedeutung | Lesbar ohne neue `MainMenuController`-Abhängigkeit? |
|---|---|---|---|
| `MainMenuController.IsMenuVisible` | MainMenuController.cs:138 | Menü-Oberfläche besitzt den Schirm | Nur innerhalb Nova.Presentation.UI (drei Komponenten verdrahtet bereits: RtsDeviceInput.cs:92, MatchFrameHud.cs:40, MusicDirector.cs:42). Für Nova.Presentation **unmöglich** — Rank-Gate, darum ist `_cameraRig` als `Behaviour` deklariert (:63-66). Sagt nichts über Pause. |
| `MatchBootstrap.IsMatchReady` | MatchBootstrap.cs:213 | „Match-Welt existiert" (Kernel + Systeme gebaut) | **Ja** — `MatchBootstrap` wird bereits aus Presentation.UI und Presentation.Maps referenziert. Bleibt bei Pause **true** — trennt „kein Match" von „pausiert", nicht „läuft" von „pausiert". |
| `MatchRunner.IsRunning` | MatchRunner.cs:157 (`Kernel != null && Kernel.IsRunning`) | Simulationsuhr läuft | **Ja** — bereits der häufigste Riegel. „Pausiert" und „kein Match" lesen beide false; die Unterscheidung liefert erst die Kombination mit `IsMatchReady` (bzw. `Entities != null`). |
| `Runner.PauseMatch()` / `StartMatch()` | MatchRunner.cs:314 / :296 | **Aktionen**, keine Signale | — |
| `_runner.Victory.IsDecided` | via MatchRunner | Matchende gelatcht | **Ja** (MatchFrameHud, MusicDirector, DebugHud lesen es bereits). |
| Abgeleitet: `IsMatchReady && !IsRunning` | MatchFrameHud.cs:94 macht genau das (via `victory != null`) | **„pausiert"** | **Ja** — der dritte Zustand ist heute schon ohne neues Signal ableitbar. |
| `HudPointerLink.PointerOverHud` | HudPointerLink.cs:24 | Zeiger über HUD (pro Frame publiziert) | **Ja** — statischer Kanal in Nova.Gameplay, von beiden Presentation-Assemblies lesbar, **der etablierte Präzedenzfall** für Signale ohne neue Assembly-Kante (mit `MinimapCameraLink`, MinimapCameraLink.cs:26). |
| `_runner.Entities/Economy/FogOfWar != null` | überall | „Welt existiert" | Ja — aber das ist exakt die gebrochene #102-Annahme: **„Welt existiert" ≠ „Match ist aktive Oberfläche"**. |

Fazit: „kein Match" vs. „pausiert" ist über `IsMatchReady` + `IsRunning` sauber trennbar; „Oberfläche gehört einem Modal" (Menü, Pause, Resultat) existiert als Signal nur fürs Hauptmenü (`IsMenuVisible`) und ist rank-bedignt nicht überall lesbar.

## 5. Empfehlung: ein einziger Riegel — und ja, die Wurzel statt des Flags

**Die gemeinsame UI-Wurzel existiert bereits** und ist der bessere Riegel: der Szenengenerator legt **alle** In-Match-HUD-Komponenten auf **ein** GameObject „UI" (BootstrapSceneGenerator.cs:317: RtsDeviceInput, SelectionMarkerView, RallyFlagView, PlacementGhostView, ConstructionSiteMarkerView, BuildMenuHud, CommandCardHud, FogOfWarOverlayView, MinimapHud, HealthBarHud, MatchFrameHud, DebugHud). Damit:

1. **Sichtbarkeit: `SetGameplayLayerActive` (MainMenuController.cs:783) schaltet die Wurzel** (`_gameplayHudRoot.SetActive(active)`) statt einer Komponentenliste. Setzer: weiterhin allein `MainMenuController`. Leser: **niemand** — die Deaktivierung *ist* der Riegel; OnGUI, LateUpdate und Hit-Tests der Kinder sterben mit. Das ist die einzige Lösung, die die berechtigte Sorge des Docstrings (:770-773) strukturell beantwortet: ein Katalog verrottet, eine Wurzel nicht — eine neue HUD-Komponente landet ohnehin im Generator auf diesem Objekt und ist ab dann abgedeckt, **ohne je namentlich genannt zu werden**. Der Kamera-Rig bleibt separater Toggle (er liegt auf „Main Camera", :101, nicht auf der Wurzel). Musik braucht nichts (`MusicDirector` ist bereits Klasse (a)), `UnityAudioService` bleibt passiv — Restklänge verklingen, das ist korrekt so.
2. **Eingabe im dritten Zustand (Pausemenü):** Die Wurzel deckt Menü und Matchende-mit-Menü ab; das Pausemenü will die Wurzel aber typischerweise **an** lassen (Welt/HUD darunter sichtbar) und nur die Weltgesten sperren. Dafür **ein** Flag mit der Semantik „eine modale Oberfläche besitzt die Eingabe" — *nicht* „Match läuft nicht". Konkret nach dem HudPointerLink-Vorbild: ein statischer Kanal in Nova.Gameplay (z. B. `ModalSurfaceLink.Open`), der **pro Frame publiziert** wird (kein Latch mit Gedächtnis — ein hängengebliebenes `true` wäre ein Eingabe-Deadlock). Setzer: genau eine Stelle — die Komponente, die modale Panels zeichnet, heute `MatchFrameHud` (Resultat + Pause), morgen das Pausemenü; sie muss außerhalb der UI-Wurzel leben, damit sie im Menü-Zustand überhaupt publizieren kann (im Menü ist die Wurzel ohnehin aus — niemand liest). Leser: `RtsDeviceInput.Update` (ergänzt `IsMenuVisible` an :424 und behandelt Klicks wie HUD-Klicks — damit sterben auch die Klickdurchfälle auf „Hauptmenü"/„Fortsetzen") und **gratis** die Kamera: `RtsDeviceInput` publiziert bei offenem Modal einfach `HudPointerLink.Publish(true)` — der bestehende Kanal deckt den Rand-Pan ohne neuen Leser ab. Pause und „kein Match" bleiben dabei sauber getrennt: das Flag sagt „Modal offen", die Match-Zustände liest weiterhin, wer sie braucht, aus `IsMatchReady`/`IsRunning`.
3. **DebugHud — Absprache nötig (Einheitenstrang):** `DebugHud` liegt auf derselben Wurzel (:370). `DebugHud.cs` selbst muss **nicht** geändert werden — `_visible` und F3-Zustand überleben `SetActive`, das Verhalten ist identisch zum heutigen `_debugHud.enabled`-Toggle. Aber der Kontrollfluss über die fremde Komponente ändert sich (benannter Einzel-Toggle → Wurzel), und der Wurzel-Ansatz nimmt die Option, DebugHud künftig *über* dem Menü zu zeigen (Diagnose am Menü), ohne die Komponente umzuhängen. Das gehört in die Absprache, bevor `SetGameplayLayerActive` umgestellt wird.

Bewusst **nicht** empfohlen: jeder Komponente einen `IsMenuVisible`-/`IsRunning`-Check zu verpassen. Das ist der Katalog, der mit der nächsten Komponente verrottet — 21.2 hat gerade zwei neue (FieldCard, Scherben-Staging) geliefert.

STATUS: DONE
- Klasse (c) = #102-Mechanismus: BuildMenuHud, CommandCardHud, MinimapHud prüfen nur „Welt existiert" (`Entities`/`Economy`/`FogOfWar != null`); ReturnToMenu pausiert nur, also zeichnen und **klicken** sie über dem Menü (IMGUI liegt immer über UI Toolkit); Minimap-Klicks stellen zusätzlich einen Kamera-Sprung ein, der beim Matchstart ausgeführt wird (MinimapCameraLink.cs:103).
- Klasse (b)-Kritikalste: RtsDeviceInput — die modalen Panels (Resultat/Pause) stehen nicht in `IsPointerOverHud` (RtsDeviceInput.cs:1102), der Klick auf „Hauptmenü" selektiert hinter dem Panel und armiert die Karte über dem Menü selbst; P-Pause hat gar keinen Riegel, alle Hotkeys und die Harvester-Eskorte feuern in den gestoppten Kernel.
- Empfehlung: die bereits existierende UI-Wurzel (BootstrapSceneGenerator.cs:317) als einziger Sichtbarkeits-Riegel statt Komponentenkatalog, plus **ein** pro Frame publiziertes „Modal offen"-Flag nach HudPointerLink-Vorbild für Eingaben im Pause-Zustand; DebugHud liegt auf derselben Wurzel — Verhalten bleibt gleich, aber der Kontrollfluss über die Einheitenstrang-Komponente ändert sich: Absprache erforderlich.

[exited with code 0]
