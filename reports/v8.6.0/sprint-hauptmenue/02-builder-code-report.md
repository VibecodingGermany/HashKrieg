---
agent: builder
version: v8.6.0
date: 2026-08-06
datum: 2026-08-06
status: complete
task: Hauptmenü + Einstellungen als UI-Toolkit-Overlay in Bootstrap.unity (D-083)
sprint: sprint-hauptmenue (v8.6.0)
branch: feat/playable-core-loop
spezifikation: docs/production/hashkrieg/08_Sprint_Hauptmenue.md
entscheidung: D-083
---

# Builder-Report: Hauptmenü + Einstellungen (Code)

Schreibumfang eingehalten: nur die unten gelisteten Dateien wurden angefasst.
`Assets/Tests/**`, `docs/**`, `CHANGELOG.md` und `GameSettings.cs` sind
unberührt. Es gab **keine** Änderung außerhalb des zugewiesenen Umfangs — auch
nicht an `DebugHud.cs` oder `RtsCameraController.cs` (siehe §3.2, das war eine
bewusste Entscheidung gegen einen naheliegenden Zwei-Zeilen-Patch).

---

## IMPLEMENTATION COMPLETE

### Files Created

- `Assets/_Project/Scripts/Presentation/UI/MainMenuController.cs` — Menübaum
  (UI Toolkit, programmatisch in C#), Titel + vier Buttons, Einstellungs-Panel,
  Anbindung an `GameSettingsStore`, Stummschaltung der zwei ungeschützten
  Szenenkomponenten.
- `Assets/_Project/Scripts/Presentation/UI/MenuMusicPlayer.cs` — Menümusik:
  Lautstärke live aus `GameSettingsStore.Applied`, Fade-out beim Matchstart.
- `Assets/_Project/Editor/MenuAssetSetup.cs` — erzeugt `PanelSettings` und
  `ThemeStyleSheet` als Assets, lädt die vier Menü-Assets pfadgebunden und
  meldet jede fehlende Datei mit ihrem Pfad.
- `Assets/_Project/Editor/MenuAssetImportSettings.cs` — `AssetPostprocessor`
  für Musik-, Bild- und Font-Import, pfadgebunden auf `_Project/Audio/**` und
  `_Project/UI/**` (fasst `_Project/Art/**` nicht an).
- `Assets/_Project/UI/Themes/HashkriegRuntimeTheme.tss` — Runtime-Theme, eine
  Zeile: `@import url("unity-theme://default");`.

### Files Modified

- `Assets/_Project/Editor/BootstrapSceneGenerator.cs` — vier Eingriffe:
  `AudioListener` an die Kamera (`CreateCamera`), `bootstrap.AutoStart = false`
  (`CreateMatchObject`), `CreateUiObject` gibt jetzt sein GameObject zurück,
  und die neue Methode `CreateMainMenuObject(...)` legt das `MainMenu`-Objekt an
  und verdrahtet es über das bestehende `WireReference`-Muster.

Keine weitere Datei wurde angefasst.

### Tests Added

**Keine — auftragsgemäß.** `Assets/Tests/**` liegt außerhalb meines
Schreibumfangs und gehört dem Test-Agenten. Zwei Punkte für ihn:

- `Assets/Tests/PlayMode/GrayboxDemoProofTests.cs` **bricht** mit
  `AutoStart = false`: der Test wartet an zwei Stellen passiv auf
  `bootstrap.IsMatchReady`, was nie mehr wahr wird. Beide Warteschleifen
  brauchen davor ein `FindAnyObjectByType<MatchBootstrap>().StartGrayboxMatch();`.
- `Assets/Tests/EditMode/.../CanonicalMatchSetupTests.cs` ist **nicht** betroffen
  und belegt sogar, dass `AutoStart = false` keine Determinismus-Änderung ist —
  er fährt bereits genau diesen Ablauf und asserted `InitialStateHash`-Parität.

Warum ich selbst keine Tests hätte schreiben können, auch mit Schreibrecht:
siehe „Bekannte Grenze" in §5.4 — `Nova.Presentation.UI` liegt auf Rang 4, und
`run_gate_check.py` verbietet Test-Assemblies jede Referenz dorthin.

Nicht erzeugt, weil ohne laufenden Unity-Editor nicht erzeugbar:
`Assets/_Project/UI/HashkriegPanelSettings.asset`, die `.meta`-Dateien der neuen
Skripte und die `.meta` der `.tss`, sowie die neu geschriebene `Bootstrap.unity`.
Siehe §5.1 — **das ist der eine Handgriff, der vor dem Commit fehlt.**
(Key Art, OGG und die beiden TTFs hat der laufende Editor bereits importiert —
mit falschen Defaults; §3.6 erklärt, wie sie geheilt werden.)

---

## 2. Öffentliche Typ- und Feldnamen (wörtlich, für den Test-Agenten)

### `Nova.Presentation.UI.MainMenuController` (MonoBehaviour, sealed, `[DisallowMultipleComponent]`)

Serialisierte Felder (alle `private`, per `WireReference` aus dem Generator gesetzt):

```
_document      UnityEngine.UIElements.UIDocument
_bootstrap     Nova.Gameplay.Match.MatchBootstrap
_music         Nova.Presentation.UI.MenuMusicPlayer
_cameraRig     UnityEngine.Behaviour          // Instanz ist RtsCameraController
_debugHud      Nova.Presentation.UI.DebugHud
_keyArt        UnityEngine.Texture2D
_titleFont     UnityEngine.Font               // Rajdhani-Bold
_bodyFont      UnityEngine.Font               // Rajdhani-Regular
```

Weitere serialisierte Felder (Layout/Farben/Persistenz, nur Defaults, nicht
vom Generator geschrieben):

```
_title (string, "HASHKRIEG") · _titleFontSize · _titleLetterSpacing · _titleRuleWidth
_contentPadding · _buttonFontSize · _buttonWidth · _buttonHeight
_fieldFontSize · _fieldLabelWidth · _settingsWidth · _settingsScrollHeight
_titleColor · _accentColor · _bodyColor · _panelFill · _panelEdge
_buttonFill · _buttonHoverFill · _scrimColor · _disabledTextColor · _disabledEdgeColor
_saveIntervalSeconds
```

Es gibt **keine öffentliche Methode und keine öffentliche Property** an
`MainMenuController` — alles ist privat. Wer den Controller testen will, hat
per Reflection Zugriff auf `StartMatch()`, `Quit()`, `ShowSettings(bool)`,
`CommitSettings(bool)`, `FlushSettings()`, `SetGameplayLayerActive(bool)`.
(Das ist bewusst so und in §6 als bekannte Grenze aufgeführt.)

Namen der erzeugten `VisualElement`s (stabil, als Testanker verwendbar):
`menu-screen`, `menu-scrim`, `menu-content`, `menu-title`, `menu-title-rule`,
`menu-main`, `menu-settings`.

Button-Beschriftungen (wörtlich): `Neues Spiel`, `Laden`, `Einstellungen`,
`Beenden`, `Zurück`.
Feldbeschriftungen: `Musik`, `Lautstärke`, `Soundeffekte`, `SFX-Lautstärke`,
`Render-Detail`, `vSync`, `Auflösung`, `Vollbild`.

### `Nova.Presentation.UI.MenuMusicPlayer` (MonoBehaviour, sealed, `[RequireComponent(typeof(AudioSource))]`)

```
serialisiert : _source (AudioSource) · _clip (AudioClip) · _fadeOutSeconds (float, 1.25)
öffentlich   : bool IsFading { get; }
               void ApplyVolume(GameSettings settings)
               void FadeOutAndStop()
```

### `Nova.Editor.MenuAssetSetup` (static)

```
const string UiFolder          = "Assets/_Project/UI"
const string ThemeFolder       = "Assets/_Project/UI/Themes"
const string ThemePath         = "Assets/_Project/UI/Themes/HashkriegRuntimeTheme.tss"
const string PanelSettingsPath = "Assets/_Project/UI/HashkriegPanelSettings.asset"
const string KeyArtPath        = "Assets/_Project/UI/UI_KeyArt_MainMenu.jpg"
const string TitleFontPath     = "Assets/_Project/UI/Fonts/Rajdhani-Bold.ttf"
const string BodyFontPath      = "Assets/_Project/UI/Fonts/Rajdhani-Regular.ttf"
const string MusicClipPath     = "Assets/_Project/Audio/Music/MUS_MainMenu_Hashkrieg.ogg"

static PanelSettings LoadOrCreatePanelSettings()
static T            LoadRequired<T>(string path) where T : Object
```

### `Nova.Editor.MenuAssetImportSettings` (AssetPostprocessor, sealed)

Einzige öffentliche Signatur: `public override uint GetVersion() => 1;`
(Import-Hash — beim Ändern eines Importwerts hochzählen, sonst behalten bereits
importierte Assets ihre alte `.meta`). Greift auf `Assets/_Project/Audio/**`
(Audio) und `Assets/_Project/UI/**` (Textur, Font); für die vier Menü-Assets bei
jedem Import, für alles andere nur beim ersten
(`assetImporter.importSettingsMissing`). Siehe §3.6.

### Szenenobjekt

Neues GameObject `"MainMenu"` mit `UIDocument` + `AudioSource` +
`MenuMusicPlayer` + `MainMenuController`, angelegt in
`BootstrapSceneGenerator.CreateMainMenuObject(...)`.

---

## 3. Getroffene Entscheidungen

### 3.1 Theme und PanelSettings als Assets, nicht zur Laufzeit

Der Recherchebefund ist bestätigt und übernommen: ein per
`ScriptableObject.CreateInstance` gebautes `PanelSettings` hat keine
ICU-Textdaten, und Unitys eingebautes Default-Runtime-Theme wird über
`EditorGUIUtility.Load` geholt — im Player existiert es nicht. Ein zur Laufzeit
gebautes Menü wäre im Editor korrekt und im Build ohne Schrift und ohne
Control-Styles, also genau die Sorte Fehler, die niemand bemerkt.
`MenuAssetSetup` schreibt beides als Asset, im selben Muster wie
`UrpProjectSetup` und `ArtAssetAutoSync.LoadOrCreateRegistry`.

Werte, die gesetzt werden **müssen**, weil `Reset()` bei `CreateInstance` nicht
läuft und die Feld-Initialisierer falsch sind:
`scaleMode = ScaleWithScreenSize` (statt `ConstantPhysicalSize`),
`referenceResolution = 1920×1080` (statt 1200×800),
`match = 0.5` (statt 0). Dazu `sortingOrder = 100` (Platz für spätere
HUD-Panels darunter) und `clearColor = false`.

Ablageort `Assets/_Project/UI/Themes/` statt Unitys `Assets/UI Toolkit/`: folgt
der Projektkonvention und verhindert nebenbei, dass Unity später ein zweites
Theme anlegt.

### 3.2 Nur zwei Komponenten werden im Menü stummgeschaltet — und keine fremde Datei angefasst

Zehn der elf Szenenkomponenten haben bereits einen „kein Match → nichts tun"-
Guard. Ohne Guard sind genau zwei:

- **`RtsCameraController`** — `LateUpdate` liest ungebremst Mausrad, MMB, Z/X,
  Pfeiltasten und Bildschirmrand-Pan. Wer im Menü die Maus an den Rand führt,
  startet das Match mit verschobener Kamera. `Awake` hat den Startfokus bereits
  gesetzt und `ApplyTransform()` gerufen, also steht ein deaktivierter Rig exakt
  richtig.
- **`DebugHud`** — `DrawStatusBar` läuft vor dem `_visible`-Check, also läge
  „F3: debug panel" über dem Key Art.

Beide werden über `enabled = false` in `MainMenuController.Start()` gelegt und
in `StartMatch()` wieder aktiviert. **Bewusst nicht** über einen Guard *in*
`DebugHud.cs`: das wäre eine Änderung an einer Datei außerhalb meines
Schreibumfangs, und die Menülösung ist reversibel und lokal. Der Ausschalt-Ort
ist `Start()` und nicht der erste Buttonklick, weil UI Toolkit den Legacy-Input
nicht blockiert — die Kamera muss ruhen, **bevor** das Menü das erste Mal
sichtbar ist.

`RtsCameraController` ist als `Behaviour` deklariert, nicht typisiert:
`Nova.Presentation.UI` und `Nova.Presentation` liegen beide auf Rang 4, und
`quality/scripts/run_gate_check.py` verbietet Same-Layer-Kanten. Der Generator
(Rang 5) sieht beide und verdrahtet die konkrete Komponente. Kein asmdef wurde
geändert.

### 3.3 „Laden": ausgegraut, mit sichtbarem Label statt Tooltip

`VisualElement.tooltip` feuert im Runtime-Panel nicht (TooltipEvent ist
Editor-only), und `SetEnabled(false)` unterbindet ohnehin alle Pointer-Events.
Der Tooltip ist trotzdem gesetzt (Spezifikation + Inspector-Dokumentation), der
Hinweis steht aber zusätzlich als dauerhaft sichtbares Label darunter:
**„kommt später — es gibt noch kein Speicherformat"**. Das Label ist ein
Geschwisterelement bei voller Deckkraft, damit das Theme den erklärenden Text
nicht mit dem Button zusammen ausgraut.

Die Ausgrauung selbst läuft über **volldeckende Farben**, nicht über `opacity`:
das Runtime-Theme kann eine eigene `:disabled`-Regel mitbringen, und zwei
gestapelte Deckkräfte wären unleserlich.

### 3.4 Persistenz: Diskrete Regler sofort, Slider zusammengefasst

Toggles und Dropdowns rufen `GameSettingsStore.ApplyAndSave()` unmittelbar —
so wie beauftragt. Bei den beiden Lautstärke-Slidern bin ich abgewichen und
begründe das:

`ApplyAndSave()` ruft `ApplyToEngine()`, und das ruft — sobald der Spieler
einmal eine Auflösung gewählt hat — `Screen.SetResolution(...)`. Ein gezogener
Slider feuert ein Change-Event pro Frame; das wären bis zu 60 Auflösungs-Calls
und 60 Dateischreibvorgänge pro Sekunde, nur weil jemand die Musik leiser
dreht. Slider-Änderungen werden deshalb auf höchstens einen Commit pro
`_saveIntervalSeconds` (Default 0,35 s) zusammengefasst.

**Das Abnahmekriterium bleibt unangetastet**: die Lautstärke geht im selben
Frame direkt an `MenuMusicPlayer.ApplyVolume(...)`, der Spieler hört den Regler
also sofort. Gespült wird zusätzlich beim Verlassen des Einstellungs-Panels, in
`StartMatch()`, in `Quit()` und in `OnDisable()` — es gibt keinen Ausgang, der
eine ungespeicherte Änderung verliert.

### 3.5 `playOnAwake = false`, Musik startet in `OnEnable`

Zweite bewusste Abweichung vom Auftragstext. Mit `playOnAwake` startet die
Engine den Clip, bevor irgendeine gespeicherte Einstellung angewandt wurde: ein
Spieler, der die Musik ausgeschaltet hat, bekäme bei **jedem** Start einen
kurzen Anriss der Menümusik auf dem 0,4-Default zu hören. `MenuMusicPlayer`
setzt deshalb erst die Lautstärke und ruft dann `Play()` — eine Zeile später,
im selben `OnEnable`.

Gleiche Logik bei `loop`, `spatialBlend` und `clip`: die Komponente
konfiguriert ihre eigene `AudioSource`, weil `BootstrapSceneGenerator` laut
eigener Hausregel verdrahtet und nicht tuned. Der Generator hängt die
`AudioSource` an und verdrahtet Referenz + Clip.

### 3.6 Import-Einstellungen: zwei Besitzregeln, weil Unity schneller war

Während dieser Sitzung hat der laufende Editor die drei Asset-Dateien um 17:45
importiert — **bevor** der Postprocessor um 18:00 existierte. Die `.meta`-Dateien
tragen deshalb Unity-Defaults, und die sind falsch (nachgelesen, nicht vermutet):

| Datei | Ist (`.meta` von 17:45) | Soll |
|---|---|---|
| `MUS_MainMenu_Hashkrieg.ogg` | `loadType: 0` (= DecompressOnLoad), `quality: 1` | Streaming, 0,7 |
| | `compressionFormat: 1` (Vorbis) ✓, `preloadAudioData: 0` ✓ | unverändert |
| `UI_KeyArt_MainMenu.jpg` | `maxTextureSize: 2048`, `enableMipMap: 1`, `textureCompression: 1` | 4096, aus, CompressedHQ |

Ein reiner `importSettingsMissing`-Guard hätte für diese Dateien nie mehr
gegriffen — der Postprocessor wäre stillschweigend wirkungslos gewesen. Deshalb
zwei Regeln:

1. Die **vier namentlich bekannten Menü-Assets** (Key Art, OGG, beide TTFs) sind
   Maschinenkonfiguration wie die generierte Szene: ihre Einstellungen werden bei
   **jedem** Import gesetzt, eine veraltete `.meta` heilt sich also selbst.
2. Alles andere unter `_Project/UI/**` und `_Project/Audio/**` bekommt die Werte
   nur beim **ersten** Import, damit eine bewusste Inspector-Änderung überlebt.

Dazu `GetVersion() => 1`: der Rückgabewert geht in den Import-Hash ein, ein neuer
Postprocessor mit dieser Version löst also den Reimport aus. Wer später einen
Wert oben ändert, muss die Zahl hochzählen — sonst behalten bereits importierte
Assets ihre alte `.meta` und die Änderung sieht aus wie ein No-op.

Als Nebenertrag hat die `.ogg.meta` eine offene Frage entschieden:
`defaultSettings.preloadAudioData: 0` steht dort **innerhalb** von
`defaultSettings` — `AudioImporterSampleSettings.preloadAudioData` ist in Unity 6
also tatsächlich ein Feld, und der Code benutzt die richtige Schreibweise.

### 3.7 Render-Detail: die Einschränkung steht im UI

Unter dem Dropdown steht wörtlich: „Wirkt auf LOD-Abstand, Anisotropie,
Texturauflösung und Partikelbudget. Renderskalierung, Schatten und
Kantenglättung bleiben gleich — alle sechs Stufen teilen sich ein URP-Asset."
Auch die Auflösungs-/Vollbild-Sektion sagt, dass sie im Editor wirkungslos ist,
und der SFX-Block, dass es noch keine Soundeffekte gibt.

### 3.8 Optik

Panels und Buttons übernehmen die Farbwerte von `HudChrome` (dunkler
transluzenter Grund, heller Randring). `HudChrome` selbst ist `internal` und
IMGUI-only, die Werte sind also gespiegelt, nicht wiederverwendet — mit
Kommentar. Hover-Tönung läuft über `PointerEnterEvent`/`PointerLeaveEvent`,
weil Inline-Styles kein `:hover` ausdrücken können und ein in C# gebautes
Runtime-Panel kein Stylesheet hat, in das man eine Regel legen könnte.

### 3.9 Fallstrick §7.1 — bestätigt, nicht angefasst

`BootstrapSceneGenerator.cs` macht weiterhin `scenes.RemoveAll(...)` +
`scenes.Insert(0, ScenePath)`. Das bleibt so: es gibt weiterhin **genau eine**
Szene, das Menü ist ein Overlay in `Bootstrap.unity`, und es wurde keine zweite
Szene angelegt. Der Umbau auf „anhängen falls nicht vorhanden" ist damit
weiterhin unnötig und wäre unbestellter Diff.

---

## QUALITY GATES

Vorbemerkung zur Ehrlichkeit dieses Abschnitts: Unity habe ich **nicht**
gestartet. Was hier steht, ist entweder wirklich ausgeführt (4.1–4.3) oder
steht in §5 als „nicht verifiziert". Es gibt keinen Haken, den ich nicht
belegen kann.

| Gate | Status | Beleg |
|---|---|---|
| Kompilierung (Typecheck) | **bestanden** | Unitys Roslyn, beide Assemblies exit 0 — §4.1 |
| Lint / Warnungen | **bestanden** | keine Warnung außer projektüblichen CS0649 — §4.1 |
| Architektur-Gate (`run_gate_check.py`) | **unberührt** | kein asmdef geändert, keine neue Assembly-Kante — §4.1 |
| Unit-/PlayMode-Tests | **nicht ausgeführt** | Testlauf braucht Unity; ein PlayMode-Test bricht bekannt, siehe „Tests Added" |
| UX-Gate (Screenshots) | **nicht anwendbar** | kein Playwright-Ziel; das Menü muss ein Mensch ansehen, §5.3 |

### 4.1 Echte Kompilierung gegen Unity 6000.5.4f1 (kein Augenmaß)

Ich habe Unitys eigenen Roslyn-Compiler
(`.../6000.5.4f1/.../DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll`) direkt
aufgerufen, mit den Referenzlisten und Defines aus den von Unity generierten
`Nova.Presentation.UI.csproj` / `Nova.Editor.csproj` (303 bzw. 313 Referenzen,
`LangVersion 9.0`, `netstandard2.1`, `UNITY_EDITOR` gesetzt), plus den
kompilierten `Nova.*`-Assemblies aus `Library/ScriptAssemblies`:

```
Nova.Presentation.UI.csproj -> exit 0  (14 Quellen)
Nova.Editor.csproj          -> exit 0  (9 Quellen)
```

Keine Fehler, keine Warnungen außer den projektüblichen CS0649
(„SerializeField wird nie zugewiesen"), die auch bestehende Dateien wie
`RtsDeviceInput.cs` erzeugen.

Damit ist belegt: alle benutzten APIs existieren in dieser Unity-Version, alle
Namespaces stimmen, es kompiliert unter C# 9, und **es wird keine Assembly
referenziert, die die beiden asmdefs nicht schon kennen** — ein fehlender
Referenzeintrag wäre hier als CS0246/CS0234 aufgeschlagen. Beide asmdefs sind
unverändert.

### 4.2 API-Existenz einzeln nachgeprüft

Vor dem Schreiben habe ich die riskanten Symbole in den Editor-DLLs
verifiziert (Metadaten-Scan von `UnityEngine.UIElementsModule.dll`,
`UnityEngine.AudioModule.dll`, `UnityEditor.dll`), unter anderem:
`IStyle.set_backgroundSize`, `BackgroundSizeType`, `set_unityFontDefinition`,
`ConvertFontDefinitionToStyleFontDefinition`, `DropdownField`, `set_index`,
`BaseField.get_labelElement`, `add_clicked`, `PanelScaleMode`,
`set_referenceResolution`, `set_screenMatchMode`, `set_match`,
`set_themeStyleSheet`, `AudioClipLoadType`, `AudioCompressionFormat.Vorbis`,
`TextureImporterNPOTScale`, `get_importSettingsMissing`, sowie die
Magic-Method-Namen `OnPreprocessAudio` / `OnPreprocessTexture`.

Ein Punkt war echt strittig und ist jetzt entschieden: `preloadAudioData`. Die
Obsolete-Meldung in `UnityEditor.dll` lautet wörtlich *„Preload audio data has
been moved to AudioImporter.SampleSettings as a per platform local setting"* —
also ist `AudioImporterSampleSettings.preloadAudioData` (Feld) richtig und
`AudioImporter.preloadAudioData` (Property) veraltet. Der Code nutzt das Feld.

### 4.3 Feldnamen gegen `WireReference` abgeglichen

Alle zehn `WireReference`-Strings im Generator wurden gegen die tatsächlichen
`[SerializeField]`-Deklarationen geprüft: `_source`, `_clip`, `_document`,
`_bootstrap`, `_music`, `_cameraRig`, `_debugHud`, `_keyArt`, `_titleFont`,
`_bodyFont` — alle vorhanden. (`WireReference` würde eine Abweichung zur
Generatorlaufzeit als `LogError` melden, aber jetzt schon zu wissen ist
billiger.)

---

## 5. Was noch getan werden muss — und was ich NICHT verifizieren konnte

### 5.1 Der Generator muss einmal laufen (Pflicht, sonst ist der Sprint nicht fertig)

`Tools/Project Nova/Create Bootstrap Scene` einmal ausführen. Erst dieser Lauf

1. erzeugt `Assets/_Project/UI/HashkriegPanelSettings.asset` — ich kann eine
   `PanelSettings`-YAML nicht von Hand schreiben, ohne die interne
   `m_Script`-fileID zu raten, und ein falsch geratenes Asset wäre schlimmer als
   keines;
2. importiert `HashkriegRuntimeTheme.tss` (erzeugt die `.meta`);
3. schreibt `Bootstrap.unity` mit `AudioListener`, `AutoStart: 0` und dem
   `MainMenu`-Objekt neu.

**Die committete `Bootstrap.unity` ist ohne diesen Lauf veraltet** — sie kennt
schon heute fünf der sieben HUD-Komponenten nicht (Stand vom letzten Commit).
Zu committen sind danach: die Szene, das PanelSettings-Asset, die `.tss` samt
`.meta`, und die von Unity beim ersten Import erzeugten `.meta`-Dateien für
Key Art, OGG und die beiden TTFs.

### 5.2 `GrayboxDemoProofTests` bricht — gehört dem Test-Agenten

`Assets/Tests/PlayMode/GrayboxDemoProofTests.cs` wartet an zwei Stellen (etwa
`:42-47` und `:96-101`) passiv auf `bootstrap.IsMatchReady`. Mit
`AutoStart = false` wird das nie wahr. Beide Warteschleifen brauchen davor ein
explizites `FindAnyObjectByType<MatchBootstrap>().StartGrayboxMatch();`. Ich
habe `Assets/Tests/**` auftragsgemäß nicht angefasst.

Die Screenshots selbst sind sicher: `camera.Render()` in eine RenderTexture
erfasst kein ScreenSpaceOverlay-Panel, das Menü kann die Bilder also nicht
verfälschen.

`CanonicalMatchSetupTests` ist **nicht** betroffen — der Test fährt bereits
`AutoStart = false` plus expliziten Start und asserted `InitialStateHash`-
Parität. `AutoStart = false` ist damit nachweislich keine Determinismus-Änderung.

### 5.3 Ehrliche Liste dessen, was ich nicht prüfen konnte

Alles Folgende ist **nur gelesen und begründet, nicht ausgeführt** — Unity habe
ich nicht gestartet:

- **Ob das Menü tatsächlich erscheint.** Kompilieren ist nicht rendern. Das
  Panel, die Theme-Auflösung über `unity-theme://default`, das
  Vollbild-Verhalten der `.unity-ui-document__root`-Klasse und die
  `Cover`-Skalierung des Key Art sind aus Quellenlage abgeleitet, nicht gesehen.
- **Ob die Menümusik hörbar ist.** `AudioListener`, Clip-Import und
  `Play()`-Zeitpunkt sind gesetzt und begründet; gehört habe ich nichts.
- **Das Layout.** Schriftgrößen, Paddings, die 1920×1080-Referenzauflösung und
  ob `_settingsScrollHeight = 480` in einem kleinen Fenster reicht, sind
  geschätzte Startwerte in `[SerializeField]`-Defaults — genau dort, wo sie ein
  Feinschliff ohne Codeänderung korrigieren kann.
- **Ob das Runtime-Theme eine eigene `:disabled`-Regel mitbringt.** Ich habe
  deshalb auf volldeckende Farben statt `opacity` ausgewichen (§3.3); wie
  „Laden" am Ende wirklich aussieht, muss jemand ansehen.
- **Ob der `AssetPostprocessor` die bereits geschriebenen `.meta`-Dateien
  wirklich heilt.** Der Mechanismus ist `GetVersion() => 1` plus die
  Unbedingt-Regel für die vier Menü-Assets (§3.6); ob Unity daraufhin
  tatsächlich reimportiert, habe ich nicht beobachtet. **Kontrollpunkt nach dem
  nächsten Editor-Fokus:** die JPG muss im Inspector `Max Size 4096`,
  `Non-Power of 2: None` und `Generate Mip Maps: aus` zeigen, die OGG
  `Load Type: Streaming` und `Quality 70`. Stimmt das nicht, genügt
  Rechtsklick → Reimport auf die beiden Dateien (oder `.meta` löschen).
- **`QualitySettings.SetQualityLevel(level, applyExpensiveChanges: true)`** löst
  einen Pipeline-Wechsel aus. Derzeit ein No-op, weil `UrpProjectSetup` allen
  sechs Leveln dasselbe URP-Asset zugewiesen hat. Sobald jemand die in §5 der
  Spezifikation erwähnten `NovaUrp`-Kopien anlegt, wird das Dropdown zu einem
  sichtbaren Pipeline-Swap mitten im Frame. Dann dort erneut hinsehen.
- **`Screen.SetResolution` im Editor.** `GameSettingsStore.ApplyAtBoot` läuft
  vor dem Szenenladen; vor dem ersten Speichern sind Breite/Höhe 0 und nur
  `fullScreenMode` wird gesetzt (harmloser Pfad). Sobald der Spieler eine
  Auflösung wählt, sollte der Editor-Fall einmal von Hand getestet werden — das
  Einstellungs-Panel sagt bereits, dass es dort nicht wirkt.

### 5.4 Bekannte Grenze: nichts davon ist automatisiert testbar

`Nova.Presentation.UI` liegt auf Rang 4, und `run_gate_check.py` verbietet
Test-Assemblies jede Referenz auf Rang ≥ 4. Weder `MainMenuController` noch
`MenuMusicPlayer` noch der bereits fertige `GameSettingsStore` sind damit aus
einem EditMode-Test erreichbar — obwohl `GameSettingsStore.FilePath` extra für
Tests überschreibbar gebaut wurde. Das Akzeptanzkriterium „die Lautstärke
überlebt den Neustart" ist deshalb heute nur von Hand nachweisbar.

Der etablierte Ausweg im Repo ist `Assets/_Project/Scripts/Gameplay/UI/`
(Rang 3, `CommandCardPresenter`, `MinimapRenderer`, `SelectionManager` liegen
dort samt Tests). `GameSettings.cs` unverändert dorthin zu verschieben und den
Namespace auf `Nova.Gameplay.UI` zu ziehen wäre der billige Fix — das ist eine
Inhaberentscheidung und lag außerhalb meines Schreibumfangs, deshalb steht es
hier und wurde nicht getan.

---

## 6. Was der Sprint bewusst nicht bekommen hat

Kein Pause-Menü, kein Restart, kein Ergebnisbildschirm, keine Fraktions- oder
Kartenwahl, keine Tastenbelegung, kein Rückweg vom Match ins Menü. Kein
`SceneManager`, keine zweite Szene, kein `.uxml`, kein `.uss`, kein AudioMixer,
kein uGUI, kein TextMeshPro, keine asmdef-Änderung. `ESC` schließt das
Einstellungs-Panel **nicht** — das wäre eine Tastenbindung, und die sind
ausdrücklich außerhalb des Umfangs.
