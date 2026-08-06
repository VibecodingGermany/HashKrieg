---
agent: tester
version: v8.6.0
date: 2026-08-06
datum: 2026-08-06
status: complete
task: Tests für Hauptmenü + Einstellungen (D-083) — GrayboxDemoProofTests mitziehen, Menü- und Persistenz-Tests neu
sprint: sprint-hauptmenue (v8.6.0)
branch: feat/playable-core-loop
spezifikation: docs/production/hashkrieg/08_Sprint_Hauptmenue.md
entscheidung: D-083
---

# Tester-Report: Hauptmenü + Einstellungen

Schreibumfang eingehalten: ausschließlich `Assets/Tests/**`. Keine asmdef wurde
geändert, kein Produktionscode angefasst, keine Datei außerhalb von
`Assets/Tests/` angelegt (außer diesem Report).

---

## 1. Files Changed

### Geändert

- `Assets/Tests/PlayMode/GrayboxDemoProofTests.cs` — beide Tests starten das
  Match jetzt explizit statt auf `AutoStart` zu warten; falscher Kommentar
  entfernt; Klassen-Doku und die `CaptureFrame`-Doku um das Menü ergänzt.

### Neu

- `Assets/Tests/PlayMode/MainMenuTests.cs` — 4 `[UnityTest]`, belegt die
  Abnahmekriterien des Sprints an der laufenden Szene.
- `Assets/Tests/EditMode/Gameplay/GameSettingsTests.cs` — 9 `[Test]`, belegt
  Persistenz, Defaults, Fehlerverhalten und `Sanitize()`.

**Keine `.meta`-Dateien geschrieben.** Unity erzeugt sie beim nächsten Import,
genau wie für die Dateien des Builders. Wer den Generator laufen lässt (siehe
§5), erzeugt sie mit.

---

## 2. GrayboxDemoProofTests — mitgezogen, nicht abgeschaltet

Beide Warteschleifen (`:42-47` und `:96-101`) sind weg. An ihrer Stelle steht
ein Aufruf, der genau da einsteigt, wo „Neues Spiel" einsteigt:

```csharp
private static void StartMatchTheWayTheMenuDoes(MatchBootstrap bootstrap)
{
    bootstrap.StartGrayboxMatch();
    Assert.IsTrue(bootstrap.IsMatchReady, "…");
}
```

**Warum der direkte Aufruf und nicht der Button.** Die Frage war ausdrücklich
gestellt, die Antwort ist keine Bequemlichkeit:

1. `MainMenuController.StartMatch()` ist **privat**. Es gibt keine öffentliche
   Methode am Controller, die der Test rufen könnte.
2. Der Controller liegt in `Nova.Presentation.UI` (Rang 4). Kein
   Test-Assembly darf dorthin referenzieren
   (`quality/scripts/run_gate_check.py:183-188`). Der Typ ist im Test also
   nicht einmal nennbar.
3. Der Klickpfad ist deshalb **nicht** verrenkungsfrei erreichbar — er ist in
   `MainMenuTests` über den UI-Toolkit-Baum implementiert (dort wird der echte
   Button gedrückt) und dort auch inhaltlich zu Hause. Der Proof-Test bleibt,
   was er ist: der Nachweis, dass Simulation und Render laufen.

`StartGrayboxMatch()` ist synchron **und** idempotent. Das hat einen
angenehmen Nebeneffekt: der Proof-Test läuft **auch gegen eine alte
Bootstrap.unity mit `AutoStart: 1`** durch (dann ist der Aufruf ein No-op). Er
ist damit unabhängig davon, ob der Szenengenerator schon lief — anders als
`MainMenuTests`, das genau das prüfen soll.

**Der falsche Kommentar** `// AutoStart fires in Start(), one frame after the
scene loads.` ist ersatzlos entfernt. Ein Grep über `Assets/Tests/**` nach
`AutoStart`, `IsMatchReady`, `Bootstrap.unity` und `FindAnyObjectByType`
brachte sonst nur noch `CanonicalMatchSetupTests.cs:249` — und das setzt
`AutoStart = false` selbst und ruft danach explizit `StartGrayboxMatch()`.
Dieser Test ist von der Änderung **nicht betroffen** und belegt weiterhin
`InitialStateHash`-Parität: `AutoStart = false` ist nachweislich keine
Determinismus-Änderung.

**Screenshots.** Die `> 10 KB`-Asserts bleiben unverändert und bleiben gültig.
Das Menü stört die Captures nicht: `MenuAssetSetup.LoadOrCreatePanelSettings()`
erzeugt ein Screen-Space-Overlay-Panel, das nach allen Kameras komponiert wird
und nie in eine Kamera-`targetTexture` rendert. `camera.Render()` in eine
RenderTexture sieht es also so wenig wie das IMGUI-HUD. Der Kommentar an
`CaptureFrame` sagt das jetzt auch. Einziger sichtbarer Unterschied gegenüber
früher: `demo_01_start.png` und `demo_02_economy.png` zeigen die Kamera dort,
wo `RtsCameraController.Awake()` sie hingestellt hat — `Awake` ruft
`ApplyTransform()` (`:98-106`), bevor das Menü den Rig deaktiviert. Die
Bildinhalte ändern sich also nicht nennenswert.

---

## 3. MainMenuTests (PlayMode) — die Abnahmekriterien

Vier Tests, jeder lädt `Bootstrap.unity` frisch.

| Test | Belegt |
|---|---|
| `BootstrapScene_OpensInTheMainMenuWithNoMatchRunning` | `IsMatchReady == false` und `MatchRunner.IsRunning == false` nach dem Laden; `menu-screen` existiert und ist sichtbar; alle vier Buttons existieren; **„Laden" ist `enabledSelf == false`**; Kamera-Rig und DebugHud sind deaktiviert |
| `MainMenu_PlaysTheLoopedTrackAndTheSceneHasAListener` | genau **ein** `AudioListener`, und zwar auf „Main Camera"; die Menü-`AudioSource` hat den Clip `MUS_MainMenu_Hashkrieg`, `loop == true`, `playOnAwake == false`, `spatialBlend == 0`, `isPlaying == true` |
| `NewGame_StartsTheMatchHidesTheOverlayAndWakesTheCockpit` | Klick auf „Neues Spiel" → `IsMatchReady`, `IsRunning`, Overlay auf `display: none`, Kamera-Rig **und** DebugHud wieder `enabled`, Controller selbst `enabled == false`; nach 2 s: Tick ist gestiegen, Musik spielt nicht mehr |
| `SettingsPanel_MusicVolumeReachesTheSourceAtOnceAndIsPersisted` | „Einstellungen" schaltet die Panels um; **alle acht Regler des Sprint-Umfangs existieren**; Slider-Änderung landet **ohne dazwischenliegendes `yield`** auf `AudioSource.volume`; nach 1,5 s liegt der Wert in der (umgeleiteten) `settings.json` |

**„Beenden" wird nie ausgelöst.** Der Button wird nur auf Existenz geprüft, mit
einem Assert-Text, der sagt warum: im Editor setzt er
`EditorApplication.isPlaying = false` und würde den Testlauf mitnehmen.

**Wie der Button gedrückt wird.** Über den echten Handler, nicht über
Reflection auf die private Methode:

```csharp
using (var submit = new NavigationSubmitEvent { target = button })
{
    button.SendEvent(submit);
}
```

`Button` übersetzt `NavigationSubmitEvent` intern in denselben
`clicked`-Callback, den ein Mausklick auslöst (das ist der Tastatur-/Gamepad-
Pfad). Ein synthetisierter Pointer-Klick wäre zusätzlich von aufgelöster
Layout-Geometrie und vom Element-under-Pointer-Buchhaltung des Panels
abhängig — beides in einem Batchmode-Lauf nicht verlässlich. Dass das Konstrukt
kompiliert (der `target`-Setter ist öffentlich), ist verifiziert, siehe §7.

**Wie das Menü ohne Typreferenz gefunden wird.** `UIDocument`, `AudioSource`,
`AudioListener`, `Button`, `Slider`, `Toggle`, `DropdownField` sind alles
Engine-Typen und im Test-Assembly ohne asmdef-Änderung verfügbar. Nur die drei
Nova-Komponenten `MainMenuController`, `RtsCameraController` und `DebugHud`
sind hinter der Assemblywand — die werden über
`FindObjectsByType<Behaviour>(FindObjectsInactive.Include)` und einen Vergleich
auf `GetType().Name` gesucht. `FindObjectsInactive.Include` ist dabei
tragend: ein **deaktiviertes** Behaviour gilt für diese API als inaktiv, und
genau deaktiviert sind Rig und HUD, solange das Menü steht.

---

## 4. GameSettingsTests (EditMode) — Persistenz

Neun Tests. `[SetUp]` biegt `GameSettingsStore.FilePath` auf ein
Wegwerf-Verzeichnis unter `Application.temporaryCachePath` um, `[TearDown]`
setzt auf `null` zurück (= wieder `persistentDataPath`) und löscht das
Verzeichnis. **Kein Test fasst je die echte Spielerdatei an.**

| Test | Belegt |
|---|---|
| `MusicVolume_DefaultsToFourTenths` | `DefaultMusicVolume == 0.4f` **und** eine frische `GameSettings` startet darauf. Festgenagelt mit der Begründung (−11,8 LUFS), damit es niemand versehentlich auf 1.0 dreht |
| `ApplyAndSave_WritesTheLiveSettingsToDisk` | Schreibrichtung: Datei existiert, `Load()` liefert Lautstärken/Schalter zurück |
| `Load_ReadsEveryFieldBackFromDisk` | Leserichtung gegen **handgeschriebenes JSON**: alle neun Felder inkl. `qualityLevel`, `resolutionWidth/Height`. Pinnt zugleich die JSON-**Feldnamen** — eine Umbenennung würde jede gespeicherte Spielerdatei stillschweigend entwerten |
| `Load_WithoutAFile_ReturnsTheDocumentedDefaults` | fehlende Datei → Defaults, kein Wurf; `qualityLevel` löst auf den Engine-Level auf |
| `Load_WithACorruptFile_FallsBackToDefaultsAndSaysSo` | kaputte Datei → Defaults **und** genau eine `LogWarning` |
| `Sanitize_ClampsVolumesAndTheQualityIndex` | 1.7 → 1, −0.5 → 0, Quality 99 → `names.Length-1`, negative Auflösung → 0 |
| `Sanitize_ResolvesTheUnsetQualityIndexToTheEngineLevel` | −1 → aktueller Engine-Level |
| `Muting_ZeroesTheEffectiveVolumeButKeepsTheStoredLevel` | Stummschalten setzt `EffectiveMusicVolume` auf 0, **löscht aber den gespeicherten Pegel nicht** |
| `FilePath_FallsBackToPersistentDataPathWithoutAnOverride` | die Testnaht selbst: ohne Override landet die Datei in `persistentDataPath` |

**Zu `LogAssert` (die Frage war explizit gestellt).** Ich benutze
`LogAssert.Expect(LogType.Warning, …)` und **nicht**
`ignoreFailingMessages`. Grund: Unitys Testrunner lässt einen Test nur bei
unerwarteten **Errors/Exceptions** scheitern, Warnungen für sich sind harmlos.
`ignoreFailingMessages = true` würde hier also nichts absichern, sondern nur
echte Fehler verstecken. `Expect(...)` dreht das um und macht die Warnung zur
**Zusicherung**: eine kaputte Datei darf den Spieler seine Einstellungen
kosten, aber nicht stillschweigend.

---

## 5. Was ein Testlauf voraussichtlich aufdeckt

Nach Wahrscheinlichkeit sortiert.

1. **`MainMenuTests` fällt komplett durch, bis der Generator gelaufen ist.**
   Das ist kein Testfehler, das ist der Befund. Verifiziert am Arbeitsbaum:
   `Assets/_Project/Scenes/Bootstrap.unity` trägt `AutoStart: 1`, kein
   `MainMenu`-GameObject, kein `UIDocument`, keinen `AudioListener`; und
   `Assets/_Project/UI/HashkriegPanelSettings.asset` existiert noch gar nicht.
   Erster fehlschlagender Assert wäre der `IsMatchReady == false` mit genau
   diesem Hinweistext. **Vor dem Testlauf muss
   `Tools/Project Nova/Create Bootstrap Scene` einmal laufen.** Das ist Risiko
   1 des Builder-Reports, hier unabhängig bestätigt.
2. **`source.isPlaying` im Audio-Test** ist der wackligste Assert des Sets. Er
   ist richtig (die Musik soll hörbar sein), aber er hängt an Unitys
   Audio-Gerät im Batchmode. Läuft der Testrunner mit einem Null-Ausgabegerät
   und meldet FMOD dabei kein laufendes Voice, schlägt er fehl, obwohl das
   Menü im Editor tönt. Falls das passiert: **nicht den Test löschen**, sondern
   auf `clip/loop/playOnAwake` reduzieren und den `isPlaying`-Nachweis in den
   Editor-Handlauf (DemoRunbook) verschieben.
3. **Der Assert auf das Dropdown „Auflösung"** kann auf einer Maschine ohne
   Display-Modi leerlaufen: `BuildResolutionField` steigt aus, wenn
   `Screen.resolutions` leer ist *und* `Screen.width/height` 0 sind. Auf einem
   Rechner mit Bildschirm unkritisch, auf einem headless-CI-Runner denkbar.
4. **`NavigationSubmitEvent`** ist die einzige Stelle, an der ich mich auf
   Verhalten verlasse, das ich nicht ausführen konnte: dass `Button` daraus
   `clicked` macht. Kompiliert sauber gegen Unity 6000.5.4f1. Falls doch nichts
   passiert, schlägt in `NewGame_…` der `IsMatchReady`-Assert fehl, dessen Text
   beide möglichen Ursachen benennt (unverdrahteter Bootstrap vs. Klickpfad).
5. **`Load_WithACorruptFile_…`** setzt voraus, dass
   `JsonUtility.FromJsonOverwrite` bei meinem Müll-String wirklich wirft. Für
   nicht-JSON ist das eine `ArgumentException`; sollte Unity das eines Tages
   still schlucken, fehlt die erwartete Warnung und der Test fällt — mit einer
   sehr eindeutigen Meldung.
6. **`ApplyAndSave_…` fasst die Engine an.** Ich habe das so weit wie möglich
   entschärft (Quality-Index bleibt auf dem laufenden Level,
   `resolutionWidth/Height` bleiben 0, damit `Screen.SetResolution` gar nicht
   erst gerufen wird) und sichere in `[SetUp]/[TearDown]` `vSyncCount` und
   `GetQualityLevel()`. Ohne diese Sicherung würde ein Testlauf
   `ProjectSettings/QualitySettings.asset` im Arbeitsbaum verändern — genau die
   Sorte stiller Änderung, über die §9 des Sprints sich schon einmal beschwert.

---

## 6. Annahmen, die ich treffen musste

1. **Der Generator läuft vor dem Testlauf.** Ohne ihn gibt es kein Menü zu
   testen (siehe §5.1).
2. **Screen-Space-Overlay-Panels landen nicht in `camera.Render()`.** Belegt
   durch die Konfiguration in `MenuAssetSetup` (`targetTexture` wird nie
   gesetzt, `sortingOrder`/`clearColor` sind Overlay-Semantik) und durch die
   Doku-Aussage im Produktionscode selbst.
3. **`Button` beantwortet `NavigationSubmitEvent` mit `clicked`.** Siehe §5.4.
4. **`Screen.SetResolution` und `Screen.fullScreenMode` sind im Editor
   wirkungslos.** Deshalb bleibt `ApplyAndSave` im EditMode-Test bei
   Auflösung 0/0 — dann wird `SetResolution` ohnehin nicht gerufen, und nur
   `fullScreenMode` wird gesetzt.
5. **Der Testlauf teilt sich eine Domain mit anderen EditMode-Tests.**
   `GameSettingsStore.Current` ist statisch; `[TearDown]` setzt jedes Feld
   wieder auf den Wert einer frischen Instanz zurück, damit nichts überläuft.
6. **Die Assert-Texte dürfen Typnamen als Strings tragen.** `MainMenuController`,
   `RtsCameraController`, `DebugHud` sind hinter der Assemblywand. Ein
   Umbenennen bricht die Tests mit einer Meldung, die genau das sagt.

---

## 7. Verifikation

- **Kompiliert.** Beide betroffenen Assemblies wurden lokal mit Unitys eigenem
  Roslyn (`…/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll`, C# 9,
  netstandard2.1) gegen die vollständigen Referenzlisten aus den generierten
  `.csproj` übersetzt:
  - `Nova.PlayMode.Tests` (GrayboxDemoProofTests + MainMenuTests) — **exit 0,
    keine Warnung**
  - `Nova.Gameplay.Tests` (inkl. GameSettingsTests) — **exit 0, keine Warnung**
- **Architektur-Gate grün.** `analyze_asmdef_tree(Assets)` aus
  `quality/scripts/run_gate_check.py` meldet `none`. Es wurde keine asmdef
  angefasst.
- **Nicht ausgeführt.** Beide Suiten brauchen den Unity-Testrunner; PlayMode
  zusätzlich headless-with-graphics und **ohne** `-quit`.

---

## 8. Der Konflikt, den ich nicht auflösen durfte — und die Empfehlung

**Der Auftrag verlangte, `Nova.Presentation.UI` als Referenz in eine
Test-asmdef einzutragen. Das habe ich bewusst nicht getan.** Es hätte den
G0-ARCHITECTURE-Gate gebrochen:

```
run_gate_check.py:183-188
  if is_test and target_rank >= 4:
      "test assembly must not reference presentation/editor assembly …"
```

Und das ist keine graue Regel, die man einmal beugen kann — sie hat in diesem
Repo bereits Geschichte. Commit `5cdb0ce` („refactor(architecture): move
testable logic out of presentation layer") hat das Assembly
`Nova.Presentation.Tests` **aufgelöst**, mit der Begründung, es könne
G0-ARCHITECTURE nie bestehen; `SelectionManager`, `CommandCardPresenter` und
`MinimapRenderer` sind damals nach `Nova.Gameplay` gewandert, `MapDefinitionSO`
nach `Nova.Data`, und die Tests sind unverändert mitgezogen. Eine asmdef
namens `Nova.Presentation.Tests` neu anzulegen wäre das exakte Gegenteil
dieser Entscheidung gewesen.

**Stattdessen:** `GameSettingsTests.cs` liegt in
`Assets/Tests/EditMode/Gameplay/` (Assembly `Nova.Gameplay.Tests`, keine neue
asmdef, keine neue Referenz) und greift auf `GameSettings`/`GameSettingsStore`
über den Typnamen zu. Das ist eine Krücke, und sie ist im Datei-Kopf als solche
markiert.

**Empfehlung an den Inhaber (eine Datei, kein Logikbruch):**

```
Assets/_Project/Scripts/Presentation/UI/GameSettings.cs
  → Assets/_Project/Scripts/Gameplay/UI/GameSettings.cs
  namespace Nova.Presentation.UI → Nova.Gameplay.UI
  + using Nova.Gameplay.UI; in MainMenuController.cs und MenuMusicPlayer.cs
```

Das ist derselbe Griff wie in `5cdb0ce`, es landet im selben Zielordner wie die
drei Vorgänger, und danach ist `GameSettingsTests.cs` ein normaler, getippter
Test: Datei bleibt liegen, Testnamen bleiben, die
Reflection-Hilfsmethoden am Dateiende fallen ersatzlos weg. Die
`MainMenuTests`-Reflection auf `FilePath` verschwindet damit ebenfalls.

**Was auch danach untestbar bleibt:** `MainMenuController` selbst
(`Nova.Presentation.UI`). Die vier PlayMode-Tests kommen über den
UI-Toolkit-Baum und Engine-Typen an sein Verhalten heran, aber nicht an seinen
Typ. Wer dort einmal echte Unit-Tests will, braucht einen Unity-freien
Presenter in `Nova.Gameplay` — dasselbe Muster wie `CommandCardPresenter`.
Für diesen Sprint ist das nicht nötig.

---

## 9. Pflichtabschnitte des @tester-Templates

Das kanonische @tester-Template ist für ein **Web-UX-Gate mit Playwright**
gebaut (Screenshots je Viewport, Browser-Konsole, Core Web Vitals,
a11y-Audit). Dieser Durchgang war ein anderer: automatisierte Unity-Tests
schreiben, nicht eine laufende Oberfläche abfotografieren. Die Abschnitte sind
trotzdem ausgefüllt — mit dem, was in einem Unity-Projekt an ihre Stelle tritt,
und mit einem klaren „gibt es hier nicht", wo es das nicht gibt.

### Screenshots Created

**Keine.** Dieser Durchgang hat keinen Testlauf ausgeführt und keine Bilder
erzeugt. Das Projekt hat weder ein `.playwright-mcp/`- noch ein
`screenshots/`-Verzeichnis, und es wird auch keins bekommen: es gibt keine
Web-Oberfläche, der `playwright`-MCP hat hier nichts zu greifen.

Was es stattdessen gibt: `GrayboxDemoProofTests` **erzeugt beim Lauf** fünf
PNGs nach `output/demo/` — `demo_01_start.png`, `demo_02_economy.png`,
`demo_03_overview.png`, `demo_04_base_alliance.png`, `demo_05_base_legion.png`
— und asserted für jedes `File.Exists` plus `> 10 KB`. Diese Asserts habe ich
bewusst unangetastet gelassen (§2). Sie sind der Bildnachweis dieses Projekts:
ein leeres Batchmode-Capture ist 0–2 KB groß, ein echtes nicht.

Nicht abgedeckt und ehrlich benannt: **wie das Menü aussieht**, hat niemand
gesehen — Layout, Lesbarkeit über dem Key Art, Schriftgrößen. Der
UI-Toolkit-Overlay landet nicht in den RenderTexture-Captures (§2), also kann
auch kein PlayMode-Test ihn abbilden. Das bleibt der Sicht-Prüfung des
Inhabers im Editor.

### Console Errors

- **Kompilierung:** beide Assemblies exit 0, **null Warnungen** (§7). Zwei
  Warnungen, die mein erster Entwurf erzeugt hatte, sind behoben statt
  unterdrückt: `CS0618` (`FindObjectsSortMode` ist in Unity 6.5 veraltet →
  `FindObjectsByType<T>(FindObjectsInactive.Include)`) und `CS0649` für die
  JSON-Sondenfelder (jetzt mit begründetem `#pragma`).
- **Zur Laufzeit erwartet:** genau **eine** Console-Message, und sie ist
  zugesichert statt geduldet —
  `LogAssert.Expect(LogType.Warning, "[GameSettings] Could not read … falling
  back to defaults.")` in `Load_WithACorruptFile_…`.
- **Alles andere ist ein Fehlschlag:** Unitys Testrunner lässt einen Test bei
  unerwartetem `LogError`/`Exception` scheitern. Genau das trifft die
  `Debug.LogError`-Aufrufe, die der Builder in `MainMenuController.LogMissingWiring()`
  und `MenuAssetSetup.LoadRequired()` eingebaut hat: läuft der Generator nicht
  oder fehlt ein Asset, fallen die PlayMode-Tests **auch dann**, wenn die
  Asserts selbst durchgingen. Das ist erwünscht.

### Performance Metrics

**LCP, CLS, INP und FCP sind Web-Vitals** — sie existieren ohne Browser nicht
und werden hier nicht erhoben. Die Zahlen, die in diesem Projekt an ihre
Stelle treten:

| Größe | Wert | Herkunft |
|---|---|---|
| Laufzeit der neuen PlayMode-Tests | ~4 s reine Wartezeit (2 s Tick-Nachweis + 1,5 s Persistenz-Fenster) plus 4× Szenenladen | Konstanten in `MainMenuTests` |
| Laufzeit der neuen EditMode-Tests | vernachlässigbar, kein `WaitFor*`, nur Datei-I/O in `temporaryCachePath` | `GameSettingsTests` |
| Gesparte Wartezeit im Proof | bis zu 2× 15 s Timeout, die mit `AutoStart = false` sicher aufgelaufen wären | §2 |
| Schreib-Coalescing, das ein Test belegt | max. 1 Dateischreibvorgang pro 0,35 s statt bis zu 60/s beim Slider-Ziehen | `MainMenuController._saveIntervalSeconds` |

Nicht gemessen: Framerate, Speicher, Ladezeit des Menüs. Dafür gibt es in
diesem Repo keine Messstrecke, und eine zu erfinden war nicht Auftrag.

### Accessibility

**Kein automatisierter a11y-Nachweis** — für UI Toolkit gibt es in diesem
Projekt kein Audit-Werkzeug, und der `a11y`-MCP prüft Webseiten. Was an
zugänglichkeitsrelevantem Verhalten trotzdem **testgesichert** ist:

- **„Laden" ist als deaktiviert erkennbar, nicht nur ausgegraut.** Der Test
  prüft `enabledSelf == false`; der Builder hat den Zustand bewusst über
  deckende Farben statt über `opacity` gebaut, damit ein `:disabled`-Regelwerk
  des Runtime-Themes nicht zwei Transparenzen übereinanderlegt.
- **Die Erklärung erreicht den Spieler wirklich.** Der Tooltip „kommt später"
  kann ihn nie erreichen (`TooltipEvent` ist Editor-only, und
  `SetEnabled(false)` blockt Pointer-Events); die Information steht deshalb als
  sichtbares Geschwister-Label in voller Deckkraft darunter. Der Test prüft die
  Existenz des Buttons und seinen Zustand — die Lesbarkeit des Hinweises bleibt
  Sichtprüfung.
- **Tastatur-/Gamepad-Bedienung ist belegt**, als Nebenwirkung der Testmethode:
  `MainMenuTests` drückt die Buttons über `NavigationSubmitEvent`, also über
  denselben Pfad, den ein Spieler ohne Maus benutzt. Läuft der Test, ist das
  Menü per Submit bedienbar.

Offen: Kontrast von Text über dem Key Art, Skalierung auf sehr kleinen
Fenstern, Farbfehlsichtigkeit. Alles drei nur per Auge zu klären.

### Decision

**DECISION: APPROVED — mit einer Vorbedingung, die vor dem ersten grünen Lauf
erfüllt sein muss.**

- Die Tests sind geschrieben, kompilieren gegen Unity 6000.5.4f1 ohne Warnung
  und verletzen das Architektur-Gate nicht.
- **Vorbedingung:** `Tools/Project Nova/Create Bootstrap Scene` muss einmal
  laufen. Bis dahin fällt `MainMenuTests` vollständig durch — nicht wegen der
  Tests, sondern weil die eingecheckte `Bootstrap.unity` das Menü noch nicht
  enthält (§5.1). Das ist kein BLOCKED gegen den Builder: sein Report nennt
  denselben Punkt als Risiko 1, und der Generatorlauf ist ein Arbeitsschritt,
  kein Fehler.
- Ein zweiter Punkt bleibt **Inhaberentscheidung**, nicht Gate: der Umzug von
  `GameSettings.cs` nach `Nova.Gameplay` (§8). Ohne ihn bleibt die
  Persistenzprüfung eine Reflection-Krücke.
