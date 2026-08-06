# Sprint: Hauptmenü + Einstellungen

**Status:** umgesetzt (2026-08-06) | **Entscheidung:** [D-083](../DecisionLog.md) | **Assets:** im Repo, Lizenzlage geklärt (siehe §4 und §8) | **Doku nachgezogen:** Licenses.md 1.4.0, DemoRunbook.md 0.4.0, CHANGELOG `[Unreleased]`

## 1. Ziel

Beim Spielstart erscheint ein Hauptmenü mit Hintergrundbild und Musik: **Neues Spiel / Laden / Einstellungen / Beenden**. Unter Einstellungen: Render-Detail, Musik an-aus + Lautstärke, SFX an-aus + Lautstärke. Die Einstellungen überleben einen Neustart.

**Nicht in diesem Sprint** (bewusst): Fraktionswahl, Kartenauswahl, Pause-Menü, Restart, Ergebnis-Bildschirm, Tastenbelegung. Jedes davon zieht eigene Simulationsfragen nach sich — Fraktionswahl zum Beispiel hängt an `InitialStateHash` und wäre eine Determinismus-Änderung, kein Menü-Feature.

## 2. Der billige Weg: Overlay statt zweiter Szene

**Keine zweite Szene, kein SceneManager.** Das Menü ist ein Overlay in der bestehenden `Bootstrap.unity`.

Begründung, alles verifiziert:

- `MatchBootstrap.AutoStart` ist bereits `[SerializeField] public bool` (`MatchBootstrap.cs:91`), und der Szenengenerator verdrahtet es laut eigenem Tooltip.
- `StartGrayboxMatch()` ist `public` und idempotent (`MatchBootstrap.cs:170-172`).
- Es gibt im ganzen Projekt **null** `SceneManager`-Aufrufe in Produktionscode. Eine zweite Szene wäre der erste Scene-Flow-Layer des Projekts — der größte Einzelposten des Sprints, für nichts.

Also: `AutoStart = false`, Menü zeigen, „Neues Spiel" ruft `StartGrayboxMatch()` und blendet das Menü aus. `Application.Quit()` für Beenden (mit Editor-Zweig).

Das Menü-Objekt wird **im Generator** angelegt, nicht per Hand in der Szene — `BootstrapSceneGenerator.cs:22-24` verbietet Handeditieren ausdrücklich, die Szene ist Maschinenausgabe.

## 3. UI-Stack: UI Toolkit

`com.unity.modules.uielements` ist ein **Engine-Modul** und braucht keinen asmdef-Eintrag. `Nova.Presentation.UI.asmdef` referenziert heute ausschließlich `Nova.*`-Assemblies; UI Toolkit passt ohne jede Änderung daran hinein.

uGUI wäre der teurere Weg: `UnityEngine.UI` und `Unity.TextMeshPro` sind echte Assemblies und müssten als Referenzen eingetragen werden, dazu EventSystem + StandaloneInputModule in der Szene und ein TMP-Essentials-Import.

Das gesamte bisherige UI ist `OnGUI` (`DebugHud.cs`, `RtsDeviceInput.cs`), selbst als „Graybox throwaway" markiert. Das Menü ist das erste echte UI des Projekts — es setzt den Standard, deshalb die Entscheidung hier festhalten.

## 4. Assets — fertig vorbereitet

Liegen außerhalb des Repos in `Hashkrieg_Assets/` (etablierte Konvention, eine Ebene über dem Repo).

| Datei | Quelle | Ziel im Repo |
|---|---|---|
| `Hashkrieg_Assets/ui/UI_KeyArt_MainMenu.jpg` — 2560×1440, 0,94 MB | `projectnove aka hashKrieg.jpg` (1536×1024), auf 16:9 beschnitten, hochskaliert, nachgeschärft, abgedunkelt, Vignette | `Assets/_Project/UI/UI_KeyArt_MainMenu.jpg` |
| `Hashkrieg_Assets/ui/UI_KeyArt_MainMenu.png` — 3,63 MB | dasselbe verlustfrei, **Master, bleibt außerhalb des Repos** | — |
| `Hashkrieg_Assets/audio/MUS_MainMenu_Hashkrieg_loop.ogg` — 2:16, 2,4 MB | `HashKrieg1.mp3`, Kopfstille getrimmt, **nahtlos geloopt** | `Assets/_Project/Audio/Music/MUS_MainMenu_Hashkrieg.ogg` |
| `Hashkrieg_Assets/audio/MUS_MainMenu_Hashkrieg_full.ogg` | dieselbe Spur ohne Loop-Blend, als Vergleich | — |

**Wichtig zur .gitignore:** die Ausschlussregel greift nur auf `Assets/_Project/Art/**`. Deshalb `Assets/_Project/UI/` und `Assets/_Project/Audio/` als Zielordner — dann ist **keine .gitignore-Änderung nötig** und ein frischer Clone hat Bild und Musik. Legt man das Bild nach `Art/`, bootet ein Clone mit schwarzem Menü und **ohne Fehlermeldung**.

**Zum Loop:** das Original beginnt leise (−18 dB RMS) und endet laut und ungefadet (−13 dB). Ein harter Loop hätte eine hörbare Stufe. Die Loop-Fassung blendet die letzten 6 s über den Anfang; Start-RMS −13,6 dB gegen Ende-RMS −12,8 dB, also dicht. `AudioSource.loop = true` genügt, kein Crossfade im Code.

**Pegel:** integriert −11,8 LUFS — für Hintergrundmusik laut. Default-Musiklautstärke auf ~0,4 setzen statt die Datei zu normalisieren (der Master bleibt unangetastet).

**Import-Einstellungen** (Musik): Load Type `Streaming`, Compression `Vorbis`, Quality ~70, **Force To Mono aus**, Preload aus.

## 5. Einstellungen — was real wirkt

| Regler | Realität |
|---|---|
| Musik-Lautstärke + an/aus | `AudioSource.volume`. **Kein AudioMixer nötig.** Der Audioplan nennt Mixer-Busse als Vorbedingung — das ist Doku-Meinung, keine Engine-Einschränkung, und für diesen Scope falsch. |
| SFX-Lautstärke + an/aus | Es gibt noch keine SFX. Wert wird gespeichert und angewandt, sobald es welche gibt. Als solches im UI kennzeichnen, nicht so tun als wirke er. |
| Render-Detail | Die 6 Quality-Level (Very Low … Ultra) existieren und **19 Felder unterscheiden sich real** (u. a. `lodBias` 0,3–2,0, Anisotropie, `skinWeights`, `particleRaycastBudget`). Ein Dropdown auf `QualitySettings.SetQualityLevel()` ist also kein Placebo. |
| — Einschränkung | Alle 6 Level teilen sich **ein** URP-Asset, deshalb unterscheiden sich `renderScale`, Schatten und MSAA nicht. Zwei zusätzliche Kopien von `NovaUrp.asset` mit abweichenden Werten machen den Unterschied sichtbar — halber Tag, optional, gern separat. |
| vSync | `QualitySettings.vSyncCount`, wirkt sofort, ehrlichste Option von allen. |
| Auflösung / Vollbild | `Screen.SetResolution` / `Screen.fullScreenMode`, braucht keine neue Infrastruktur. |

**Persistenz:** es gibt heute null `PlayerPrefs` und keine Settings-Datei. Ein `GameSettings`-Record als JSON nach `Application.persistentDataPath` plus Anwenden beim Start. Der Anwenden-Zeitpunkt braucht **kein** Boot-Objekt — `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` ist szenenunabhängig.

## 6. „Laden" — sichtbar, aber ausgegraut

Die Snapshot-Schicht ist stark: `SimulationKernel.SaveSnapshot()` / `TryRestoreSnapshot()` serialisieren den vollständigen Match-Zustand, und ein 1000-Tick-Continuation-Test beweist, dass ein wiederhergestellter Host tickweise hash-identisch weiterläuft.

**Aber nichts schreibt jemals auf Platte.** Es gibt keinen Runtime-Datei-I/O, kein Save-Format, keine Slots, und `TakePendingSessionActions()` wird von keinem Host-Code je geleert — ein `SaveRequest` liegt heute nur in einer Liste herum.

Deshalb: Button anzeigen, ausgegraut, Tooltip „kommt später". Nicht verstecken (dann fragt sich der Spieler, ob das Spiel speichern kann), nicht anbieten (dann greift er ins Leere).

## 7. Fünf Fallstricke

1. **`BootstrapSceneGenerator.cs:76-80` macht bei jedem Lauf `scenes.Insert(0, Bootstrap)`.** Solange es bei einer Szene bleibt, harmlos — falls doch je eine zweite dazukommt, rutscht sie still auf Index 1 und der Build startet am Menü vorbei. Fix wäre „anhängen falls nicht vorhanden" statt `Insert(0)`, ~15 Minuten. Kein CI-Job ruft den Generator auf, das ist eine reine Lokal-Falle.
2. **`Assets/_Project/Art/**/*.png` ist gitignored.** Siehe §4 — deshalb `Assets/_Project/UI/`.
3. **`.gitignore` hat kein Audio-Muster.** Die OGG landet also in Git. Das ist hier gewollt (2,4 MB, ohne sie ist das Menü stumm), aber es ist eine bewusste Entscheidung, keine Nebenwirkung.
4. **`docs/assets/Licenses.md` ist Default-Deny.** Musik und Key Art brauchen je eine Zeile im Ledger (§3), sonst verbietet die eigene Projektregel den Import. Siehe offene Punkte.
5. **`GrayboxDemoProofTests` asserted `bootstrap.IsMatchReady` binnen 15 s** — an zwei Stellen (`:47`, `:101`). Mit `AutoStart = false` bricht der Test. Er muss das Match explizit starten (`FindAnyObjectByType<MatchBootstrap>().StartGrayboxMatch()`), nicht auf Auto-Start warten.

## 8. Ehemals offene Punkte — vom Inhaber entschieden (2026-08-06)

Alle drei Fragen sind beantwortet. Die Fragen bleiben stehen, damit nachvollziehbar ist, *warum* die Antwort so lautet; protokolliert sind sie als [D-083](../DecisionLog.md) Punkt 5.

- **Herkunft der beiden Dateien.** *(Frage: `Licenses.md §2` Regel 6 ist Default-Deny — ohne dokumentierte Quelle kein Ledger-Eintrag und kein Import.)*
  **Entschieden:** Die Menümusik stammt aus **Suno im Bezahltarif**, das Key Art aus der **OpenAI Image API (gpt-image-1)**. Beide Tarife gewähren kommerzielle Nutzung und Output-Eigentum, damit ist die Repo-Freigabe erteilt. Die OpenAI Image API stand bereits in `Licenses.md` §1; Suno ist neu aufgenommen — und der **erste bezahlte Anbieter-Tier im Projekt**, deshalb als benannte Ausnahme von §2 Regel 5 („0 € ist hart für MS-1") eingetragen, mit Fußnote, dass die Angabe auf der Tarifauskunft des Inhabers beruht und nicht auf einer eigenen AGB-Prüfung. Ledger-Zeilen: `Licenses.md` §3, Datum 2026-08-06.
  **Rest offen:** Die `PROVENANCE.json`-Datensätze fehlen noch (Provenance.md gilt ausdrücklich auch für Audio und Fonts); die KI-Pflichtfelder `promptText`, `providerTermsUrl`, `providerTermsRetrievedAt` und das wörtliche `outputOwnership`-Zitat kann nur der Inhaber liefern.
- **Schriftart.** *(Frage: ohne eigene Schrift fällt UI Toolkit auf die generische Engine-Schrift zurück.)*
  **Entschieden: Rajdhani (Regular + Bold), SIL Open Font License 1.1.** Liegt samt `OFL.txt` in `Assets/_Project/UI/Fonts/`. OFL-1.1 ist neu in `Licenses.md` §1 und in der Whitelist (§2 Regel 6). Zwei Auflagen: Der Lizenztext muss bei jeder Weitergabe mitgehen, und die Schriftdateien dürfen nicht für sich allein verkauft werden. Eine `CREDITS.md` löst das **nicht** aus — die Attributionspflicht in Regel 2 hängt an CC-BY, und OFL-1.1 verlangt keine Namensnennung; der Rajdhani-Header nennt zudem keinen „Reserved Font Name".
- **Titel im Menü.** *(Frage: „Hashkrieg" ist Zielname, der Umbenennungsbeschluss war nicht gefasst.)*
  **Entschieden: „HASHKRIEG".** Der Beschluss ist inzwischen gefasst — [E-3](00_Entscheidungen.md) („nur die Marke — alles nach außen Sichtbare heißt Hashkrieg"). Der Menütitel nimmt also nichts mehr vorweg, sondern vollzieht E-3. Die Code-Identität bleibt unangetastet: `namespace Nova.*` und der Repository-Name ändern sich nicht.

## 9. Nebenbefund, unabhängig von diesem Sprint

Im Arbeitsbaum liegt uncommittet `ProjectSettings/QualitySettings.asset` mit `antiAliasing: 2 → 0` **auf Ultra** — und Ultra ist `m_CurrentQuality` und der Standalone-Default. Da wurde still MSAA für Editor und Desktop-Builds abgeschaltet, offenbar durch eine Editor-Re-Serialisierung. Sollte jemand ansehen, bevor es mitcommittet wird.

---

## 10. Prompt für Kimi

```text
AUFGABE: Hauptmenü + Einstellungen (Hashkrieg, Branch feat/playable-core-loop)

KONTEXT
Das Spiel startet heute direkt in ein Match. Es gibt kein Menü, keinen Ton, keine
Einstellungen und keine Möglichkeit, das Spiel sauber zu beenden. Das Simulations-
und Snapshot-Fundament ist stark, die Präsentationsschicht ist fast leer:
- Genau EINE Szene: Assets/_Project/Scenes/Bootstrap.unity — Maschinenausgabe von
  Assets/_Project/Editor/BootstrapSceneGenerator.cs. NIEMALS von Hand editieren.
- Kein SceneManager, kein Application.Quit, kein PlayerPrefs, kein Audio (kein
  AudioListener, kein AudioSource, keine Datei), kein Canvas, kein UI Toolkit im Einsatz.
- Alles bisherige UI ist OnGUI (DebugHud.cs, RtsDeviceInput.cs), als Wegwerf markiert.

ARCHITEKTUR — SO UND NICHT ANDERS
1. KEINE zweite Szene, kein SceneManager. Das Menü ist ein Overlay in Bootstrap.unity.
   MatchBootstrap.AutoStart ist bereits public [SerializeField] (MatchBootstrap.cs:91),
   StartGrayboxMatch() ist public und idempotent (:170). Also: AutoStart = false,
   Menü zeigen, "Neues Spiel" ruft StartGrayboxMatch() und blendet das Menü aus.
2. UI Toolkit, nicht uGUI. com.unity.modules.uielements ist ein Engine-Modul und
   braucht KEINEN asmdef-Eintrag. Nova.Presentation.UI.asmdef referenziert heute nur
   Nova.*-Assemblies — das soll so bleiben.
3. Das Menü-GameObject wird IM GENERATOR angelegt (BootstrapSceneGenerator), wie jedes
   andere Szenenobjekt. Nicht von Hand in die Szene klicken.

UMFANG
Hauptmenü: Neues Spiel / Laden / Einstellungen / Beenden.
- Hintergrundbild + Menümusik (Assets liegen fertig vor, siehe unten).
- "Laden" wird angezeigt, ist aber AUSGEGRAUT mit Tooltip "kommt später": die Snapshot-
  Schicht kann alles, aber nichts schreibt je auf Platte, es gibt kein Save-Format und
  keine Slots. Nicht verstecken, nicht funktionsfähig vortäuschen.
- "Beenden" = Application.Quit() mit Editor-Zweig.

Einstellungen:
- Musik an/aus + Lautstärke -> AudioSource.volume. KEIN AudioMixer nötig.
- SFX an/aus + Lautstärke -> es gibt noch keine SFX. Wert speichern und anwenden,
  sobald welche existieren, und im UI als "noch ohne Wirkung" kennzeichnen.
- Render-Detail -> QualitySettings.SetQualityLevel() über die 6 vorhandenen Level.
  Das ist kein Placebo: 19 Felder unterscheiden sich real (lodBias 0,3–2,0 usw.).
  Aber: alle 6 teilen sich EIN URP-Asset, also ändern sich renderScale/Schatten/MSAA
  nicht. Das nicht kaschieren; optional zwei NovaUrp-Kopien mit echten Unterschieden.
- vSync an/aus, Auflösung, Vollbild — alles ohne neue Infrastruktur machbar.
- Persistenz: GameSettings als JSON nach Application.persistentDataPath. Anwenden beim
  Start über [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] — kein Boot-Objekt nötig.

NICHT in diesem Sprint: Fraktionswahl, Kartenauswahl, Pause-Menü, Restart, Ergebnis-
Bildschirm, Tastenbelegung. Fraktionswahl insbesondere nicht — die hängt an
InitialStateHash und wäre eine Determinismus-Änderung, kein Menü-Feature.

ASSETS (fertig aufbereitet, nur noch kopieren)
- Hashkrieg_Assets/ui/UI_KeyArt_MainMenu.jpg (2560x1440)
  -> Assets/_Project/UI/UI_KeyArt_MainMenu.jpg
- Hashkrieg_Assets/audio/MUS_MainMenu_Hashkrieg_loop.ogg (2:16, nahtlos geloopt)
  -> Assets/_Project/Audio/Music/MUS_MainMenu_Hashkrieg.ogg
  Import: Load Type Streaming, Vorbis, Quality ~70, Force To Mono AUS, Preload aus.
  AudioSource.loop = true genügt — der Loop ist im File sauber, kein Code-Crossfade.
  Default-Musiklautstärke ~0,4 (die Spur ist mit -11,8 LUFS laut gemastert).
- WICHTIG: Assets/_Project/Art/**/*.png ist gitignored. Deshalb Assets/_Project/UI/ und
  Assets/_Project/Audio/ — dann ist KEINE .gitignore-Änderung nötig. Landet das Bild
  unter Art/, bootet ein frischer Clone mit schwarzem Menü und OHNE Fehlermeldung.
- Es gibt keinen AudioListener in der Szene. Ohne ihn ist alles stumm — im Generator
  an die Kamera hängen.

FALLSTRICKE
- BootstrapSceneGenerator.cs:76-80 macht bei jedem Lauf scenes.Insert(0, Bootstrap).
  Solange es bei einer Szene bleibt harmlos; falls du doch eine zweite anlegst, rutscht
  sie still auf Index 1. Dann auf "anhängen falls nicht vorhanden" umbauen.
- Assets/Tests/PlayMode/GrayboxDemoProofTests.cs asserted bootstrap.IsMatchReady binnen
  15 s an zwei Stellen (:47, :101). Mit AutoStart = false BRICHT der Test. Er muss das
  Match explizit starten, nicht auf Auto-Start warten. Test mitziehen, nicht abschalten.
- docs/assets/Licenses.md ist Default-Deny. Bild und Musik brauchen je eine Ledger-Zeile
  in §3, bevor sie eingecheckt werden. Herkunft beim Owner erfragen — nicht raten.
- Es gibt keine Schriftart im Projekt. Ohne eine fällt UI Toolkit auf die generische
  Engine-Schrift zurück. Wenn du eine einbettest: OFL, und Ledger-Zeile nicht vergessen.

REIHENFOLGE
1. Assets kopieren + AudioListener + Musik läuft beim Start (kleinster testbarer Schritt)
2. Menü-Overlay mit den vier Buttons, Neues Spiel und Beenden funktionsfähig
3. Einstellungen inkl. Persistenz
4. Feinschliff Look

FERTIG WENN
Ich starte das Spiel, höre Musik, sehe das Key Art mit vier Buttons, stelle die
Lautstärke leiser, starte über "Neues Spiel" eine Runde, beende das Spiel — und beim
nächsten Start ist die Lautstärke noch so, wie ich sie eingestellt habe.
```
