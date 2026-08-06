# Graybox-Log

**Version:** 0.5.0 | **Status:** Entwurf – append-only Sitzungsprotokoll (trägt D-067, noch nicht ratifiziert) | **Verantwortungsbereich:** Orchestrator / Technical Writer | **Sprint:** 7

## Zweck

Dieses Dokument protokolliert jede Sitzung der Graybox-Spur. Es ist die
Sichtbarkeitsauflage aus D-067 K3: Der befristete Dokumentationsschuld-Modus
gilt nur, solange jede Sitzung hier steht und jede Verschiebung im
[ScopeLedger](ScopeLedger.md) registriert ist. Das Protokoll ist **append-only** –
Einträge werden ergänzt, nie umgeschrieben oder geglättet.

Es ist ausdrücklich **kein Gate-Nachweis**. Nichts in diesem Dokument belegt
G0–G5 (D-067 K1); der Gate-Status steht ausschließlich in
[MVPRecoveryPlan.md](MVPRecoveryPlan.md) und entsteht ausschließlich aus
autorisierter Evidence.

## Abhängigkeiten

- [DecisionLog.md](DecisionLog.md) – D-067 (Spurregeln, Entwurf), D-068
  (Sim-Korrekturen im Pre-G1-Fenster, Entwurf), D-074 (Matrixautorität, vom
  Agenten unter Inhaber-Delegation entschieden)
- [ScopeLedger.md](ScopeLedger.md) – Registerzeile je Verschiebung
- [MVPRecoveryPlan.md](MVPRecoveryPlan.md) – Gate-Definitionen G0–G5
- [MVPContentManifest.md](MVPContentManifest.md) und
  [`../../quality/content/mvp-v1.json`](../../quality/content/mvp-v1.json) –
  Sollinhalt; von dieser Spur unberührt
- [../../AGENTS.md](../../AGENTS.md) – §2 Regel 4 und §8, die K3 aussetzt

## Protokollregeln

1. Eine Sitzung = ein Eintrag mit fortlaufender ID `GB-NNN`.
2. Ein Eintrag nennt Datum, Branch, Ziel, Schreibumfang, Ergebnis,
   **Verifikation mit echten Zahlen**, aufgeschobene Dokumentation und offene
   Befunde.
3. Nicht belegte Aussagen werden als „nicht verifiziert" markiert. Ein grüner
   Lauf, den niemand ausgeführt hat, ist kein grüner Lauf.
4. Bestehende Einträge bleiben stehen. Korrekturen kommen als neuer Eintrag.

## Sitzung GB-001 – 2026-07-26 – sichtbarer und bedienbarer Slice

**Branch:** `feat/graybox-slice` · **Commit:** keiner (Arbeitsbaum; kein
Commit, kein Push, kein Branch-Wechsel) · **Besetzung:** sechs parallele
Build-Agenten (Harvester-Ökonomie, Flow-Field-Cache, Match-Bootstrap, Kamera,
Intent-Dispatcher, Geräteeingabe/HUD, Einheitenansichten), ein Integrator, ein
Verifikator, ein Dokumentations-Agent.

### Ziel

Der Simulationskern war headless verifiziert, aber unsichtbar und unbedienbar:
`Bootstrap.unity` enthielt nur Kamera und Licht, im Repo existierte keine Zeile
Eingabecode, und `MatchRunner` – ein vollständiges MonoBehaviour – wurde von
nichts instanziiert. Ziel war ein Zustand, in dem ein Mensch das Spiel starten,
sehen und steuern kann.

### Schreibumfang der Sitzung

Präsentations- und Bedienschicht (`Scripts/Presentation/**`,
`Scripts/Presentation/UI/**`), Gameplay-Anbindung (`Scripts/Gameplay/Match/**`,
`Scripts/Gameplay/Input/**`), `Editor/BootstrapSceneGenerator.cs` und die von
ihm erzeugte `Assets/_Project/Scenes/Bootstrap.unity` (Maschinenausgabe, nicht
handeditiert), dazu drei Simulationskorrekturen nach D-068 mit Tests in beiden
Lanes. `quality/**`, `.github/workflows/**` und `VERSION` blieben unberührt.

### Ergebnis

- **Szene:** `Bootstrap.unity` wird vom Generator neu erzeugt und enthält
  Kamera-Rig, 128×128-Boden, ein `Match`-Objekt (`MatchRunner`,
  `MatchBootstrap`, `UnitViewManager`) und ein `UI`-Objekt (`RtsDeviceInput`,
  `DebugHud`). Alle Querverweise sind über `SerializedObject` gesetzt, nicht
  null.
- **Bedienung:** Auswahl (Klick und Box), Bewegen, Stop, Angriff, Ernte,
  Rückkehr, Bau und Produktion. Jeder Befehl läuft ausschließlich über
  `MatchRunner.Ingress.TrySubmitIntent`; kein MonoBehaviour mutiert
  Simulationszustand.
- **Darstellung:** Einheitenproxies aus Primitiven, Form kodiert Rolle, Farbe
  kodiert Spieler-Slot, Sichtbarkeit ausschließlich über
  `FogOfWar.GetVisibleEntities` – ein verborgenes Objekt hat keinen Proxy.
- **Simulation (D-068):** Flow-Field-Cache mit 32 Einträgen je Ziel,
  `CostField.Epoch` plus Pathfinding-Snapshotblock v2, Harvester-Autozyklus.

### Verifikation (echte Zahlen, ausgeführt)

| Prüfung | Ergebnis |
|---|---|
| Unity-Kompilierung (Batchmode, 6000.5.4f1) | Exit 0, keine `error CS` in 13 Assemblies |
| EditMode-Tests (Unity) | 338 / 338 grün, 0 Fehler, 0 übersprungen |
| .NET-Tests (`tools/Nova.SimRunner.Tests`, Release) | 341 / 341 grün |
| `DETERMINISM_10000` SelfCheck | grün: „Playback reproduced every recorded result and the recorded final state hash." |
| Hash-Tripel nach D-068 | Fingerprint `0xB455B5E3A0752A36`, Checkpoint Tick 100 `0x75C54A435FCFAB06`, finaler Zustandshash `0x87F889400D1B6C8C` |
| macOS-arm64-Player | Build erfolgreich, `Builds/MacOSArm64/ProjectNova.app`, Universal Binary (x86_64 + arm64) |
| Windows-x64-Player | Build erfolgreich, `Builds/Windows64/ProjectNova.exe` |
| Headless-Smoke-Test (macOS-Player) | zwei Läufe (40 s, 90 s), null Exceptions, alle sieben Systeme initialisiert, Kernel gestartet |
| `docs-check` (`.github/scripts/check_docs.py`) | grün |

### Ehrliche Grenzen dieser Verifikation

- Der Smoke-Test belegt, dass die Szene lädt, die Komponenten auflösen und der
  Kernel **startet**. Er belegt nicht direkt, dass Ticks weiterlaufen –
  `MatchRunner` protokolliert pro Tick nichts. Tickkorrektheit ist separat
  durch den 10.000-Tick-Determinismuslauf belegt, nicht durch den Player-Lauf.
- Das HUD konnte unter `-nographics` nicht gerendert werden. **Look and feel
  ist unverifiziert**: ob die Graybox lesbar ist und sich die Steuerung
  richtig anfühlt, entscheidet der erste menschliche Play-Durchlauf.
- Der Windows-Player wurde gebaut, aber **nicht ausgeführt** – der Host ist ein
  Mac. `ProjectNova.exe` und `UnityPlayer.dll` tragen ältere Zeitstempel; sie
  sind byte-identisch mit Unitys unverändertem Player-Template (reine
  Startstubs ohne Projektinhalt), der Projektinhalt unter `ProjectNova_Data`
  ist frisch.
- `dotnet test -c Release` im Repo-Wurzelverzeichnis schlägt mit MSB1011 fehl,
  seit der Unity-Batchmode-Lauf IDE-Projektdateien erzeugt (alle
  gitignoriert). Die Testlane braucht den expliziten Projektpfad
  `tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj`.

### Aufgeschobene Dokumentation (D-067 K3, offene Schuld)

| # | Aufgeschoben | Fällig |
|---|---|---|
| 1 | Wiki-Index [../README.md](../README.md) führt `GrayboxLog.md` und `ScopeLedger.md` noch nicht (AGENTS.md §8 DoD 2) | vor Spurende (D-067 K5) |
| 2 | Pathfinding-Snapshotblock v2 (`StateVersion` 2, Epochenbindung) ist in [../tech/SimulationCore.md](../tech/SimulationCore.md) und [../tech/Serialization.md](../tech/Serialization.md) nicht beschrieben | vor G1-Freeze |
| 3 | [RiskAnalysis.md](RiskAnalysis.md) und [OpenQuestions.md](OpenQuestions.md) sind für diese Sitzung nicht nachgezogen (Sprint-Ritual §7 Punkte 4 und 6) | vor Sprint-Abschluss |
| 4 | Änderungsverlauf und Versionsbump in den berührten Fachdokumenten je Einzeländerung (AGENTS.md §8 DoD 1) | vor Spurende (D-067 K5) |

Nicht aufgeschoben und erledigt: `[Unreleased]`-Eintrag im
[CHANGELOG](../../CHANGELOG.md), D-IDs für beide echten Entscheidungen (als
Entwurf), grüner `docs-check`, `quality/**` unberührt, kein Commit und kein
Push.

### Offene Befunde aus der Sitzung

- `Assets/Tests/EditMode/Gameplay/Nova.Gameplay.Tests.asmdef` referenziert
  `Nova.AI` nicht; heute unkritisch, sperrt aber künftige KI-Tests.
- `PathfindingTestBootstrap` spawnt Debugeinheiten mit Slot 1, während der
  lokale Slot 0 ist; unter korrektem Fog of War rendert diese Debugszene
  nichts. Einzeiler, falls sie noch benutzt wird.
- Der Harvester-Kreis schließt sich im echten Layout nicht: Feld und
  Raffinerie liegen drei Zellen auseinander, `EconomySystem` erzeugt keine
  Bewegung. Sichtbare Wirtschaftskurve ist G2-Arbeit (D-068).
- Epochenbindung des Pathfinding-Blocks wird zum Problem, sobald Bau zur
  Laufzeit Terrain verändert – eigene D-ID vor G1-Freeze nötig (D-068).
- Tastenkonflikt entschärft, aber nicht formal geregelt: Kamera nutzt
  Pfeiltasten und Z/X, Befehle nutzen S/A/H/R/B/Q. Beim Rebinding-Slice (G4)
  fällt das ohnehin weg.

## Sitzung GB-002 – 2026-07-26 – Kampfmodell und Siegauswertung

**Branch:** `feat/hashkrieg-concept-art` · **Commit:** keiner (Arbeitsbaum;
kein Commit, kein Push, kein Branch-Wechsel) · **Besetzung:** drei parallele
Build-Agenten (Schadensmatrix, Siegsystem, HUD), zwei Lese-Scouts
(Spielbarkeit, latente Korrektheit), ein Verifikator, ein
Dokumentations-Agent.

**Branch-Befund vorab, unkorrigiert protokolliert:** Der Auftrag nannte
`feat/combat-victory` als ausgecheckten Branch. Das traf nicht zu. Der
Arbeitsbaum stand zu Sitzungsbeginn auf `main` und während der Sitzung auf
`feat/hashkrieg-concept-art`; ein paralleler Strang hat den Branch mitten in
der Arbeit gewechselt. Kein Agent hat den Branch selbst gewechselt oder
committet. **Die gesamte Sitzungsarbeit liegt damit uncommittet auf einem
Branch, für den sie nicht gedacht war** – ein einziges `git checkout` würde sie
vernichten. Das ist der höchste Einzelrisikoposten dieser Sitzung und braucht
eine Inhaberentscheidung (abzweigen, stashen oder cherry-picken), bevor
irgendetwas anderes passiert.

### Ziel

Zwei Lücken machten den Spielstand unbewertbar. Erstens wandte `CombatSystem`
einen flachen Schadenswert von 15 auf jeden Angriff an: Kampfpanzer und Schütze
waren offensiv identisch, es gab weder Panzerung noch Schadensarten noch
Konterspiel. Zweitens fand `grep -rn Victory` im gesamten Repository nichts –
ein Match konnte buchstäblich nicht enden. Ziel war ein Stand, in dem ein Kampf
bewertbar ist und ein Match ein Ergebnis hat.

### Schreibumfang der Sitzung

`Scripts/Simulation/Combat/**`, `Scripts/Simulation/Victory/**`,
`Scripts/Simulation/Definitions/SimDefinitions.cs`,
`Scripts/Simulation/Snapshots/SnapshotBlockIds.cs` (eine additive Konstante),
`Scripts/Gameplay/Match/MatchRunner.cs` und `UnitViewManager.cs`,
`Scripts/Presentation/UI/DebugHud.cs`, `tools/Nova.SimRunner/Determinism10000Scenario.cs`
sowie die gespiegelten Testlanes `Assets/Tests/EditMode/**` und
`tools/Nova.SimRunner.Tests/**`. `quality/**`, `.github/workflows/**` und
`VERSION` blieben unberührt.

### Ergebnis

- **Schadensmatrix (D-074):** Der flache Wert ist ersetzt durch
  `DamageMatrix.Resolve(Basisschaden, Schadensart, Panzerungsklasse)` – eine
  flache 36-Einträge-Tabelle aus ganzzahligen Prozentwerten, angewandt als
  `(Basis × Prozent) / 100` mit Abschneiden. Keine Fließkommazahl, kein
  `SimFixed`. Neue Enums `DamageType`/`ArmorClass` und eine rollenindizierte
  `WeaponProfiles`-Tabelle; Reichweite und Abklingzeit sind seither
  rollenabhängig. Ziel- und Abklinglogik des `CombatSystem` blieben unangetastet
  – geändert hat sich nur, **was** angewandt wird.
- **Siegauswertung (D-056):** `VictorySystem` läuft als achtes und letztes
  System nach Combat, liefert `Victory.Elimination`,
  `Draw.MutualAnnihilation` und `Draw.TimeLimit` (Tick 27.000), rastet das
  Ergebnis unwiderruflich ein und serialisiert es in Snapshotblock 107 – das
  Ergebnis ist damit Teil des kanonischen Zustandshashs und übersteht
  Speichern/Laden.
- **Sichtbarkeit:** Das Debug-HUD zeigt alle vier Ergebniscodes, den Abstand
  zum Zeitlimit, die Streitkräftezählung aus derselben Quelle, über die der
  Sieg entschieden wird, sowie für die Auswahl das Waffenprofil und dessen
  Auflösung gegen jede in MS-1 getragene Panzerungsklasse. Einheiten zeigen
  ihren Gesundheitsstand über den bestehenden `MaterialPropertyBlock`, ohne
  ein einziges neues GameObject.

### Verifikation (echte Zahlen, ausgeführt)

| Prüfung | Ergebnis |
|---|---|
| Unity-Kompilierung (Batchmode, 6000.5.4f1) | Exit 0, 0 `error CS`, 0 `warning CS` über alle 18 Assemblies |
| EditMode-Tests (Unity) | 379 / 379 grün, 0 Fehler, 0 übersprungen (Basis 338, +41) |
| .NET-Tests (`tools/Nova.SimRunner.Tests`, Release) | 382 / 382 grün (Basis 341, +41 – gleiche Differenz, die Lanes sind synchron) |
| `DETERMINISM_10000` SelfCheck | grün: „Playback reproduced every recorded result and the recorded final state hash." |
| Hash-Tripel (neu) | Fingerprint `0xAF9FB211B6C9CACE`, Checkpoint Tick 100 `0x01D276820F5FFE15`, finaler Zustandshash `0xCB8A545B9710EF54` |
| Reproduzierbarkeit | zwei aufeinanderfolgende Läufe byte-identisch bis auf den SHA-256 des Endsnapshots (`e81a0a23…`) |
| macOS-arm64-Player | Build erfolgreich, `Builds/MacOSArm64/ProjectNova.app` |
| Windows-x64-Player | Build erfolgreich, `Builds/Windows64/ProjectNova.exe` |
| Headless-Smoke-Test (macOS-Player) | ~65 s, `SimulationKernel stopped at Tick(640)`, alle acht Systeme initialisiert mit `VictorySystem` an letzter Stelle, null Exceptions |
| `docs-check` (`.github/scripts/check_docs.py`) | grün |

**Hash-Zuordnung, getrennt gemessen statt behauptet:** Die beiden
Simulationsstränge liefen parallel im selben Arbeitsbaum. Beide Agenten haben
ihren Anteil isoliert, indem sie den unveränderten `HEAD` in ein
Scratch-Verzeichnis extrahiert und nur die eigenen Quelldateien überlagert
haben. Ergebnis: Die **Fingerprint-Bewegung**
`0xB455B5E3A0752A36 → 0xAF9FB211B6C9CACE` stammt vollständig aus dem neuen
Snapshotblock des Siegsystems; die **Zustandshash-Bewegung** stammt aus dem
Kampfstrang. Der Kampfstrang allein ließ den Fingerprint unverändert – er
hasht Inhalts-, Slot- und Seed-Eingaben, keine Laufzeitwirkung.

### Ehrliche Grenzen dieser Verifikation

- **Der Fingerprint sieht die Inhaltsänderung nicht.**
  `MatchFingerprint.ComputeEmptyContentStubHash` hasht einen Stub-Tag und eine
  literale Null; `DefinitionsHash64` und `RulesHash64` sind Konstanten. Diese
  Sitzung hat jeden Waffenwert des Rosters neu gesetzt, und ein vor der Sitzung
  aufgezeichnetes Replay besteht die Fingerprint-Prüfung trotzdem – und
  desynchronisiert danach als undurchsichtiger Kettenhash-Fehler. Das ist ein
  Befund, keine Behauptung: es ist genau der Fall, den die Prüfung abweisen
  soll.
- Kein menschlicher Play-Durchlauf. Ob das Konterdreieck sich **anfühlt** wie
  ein Konterdreieck, ist unverifiziert; belegt ist nur, dass die Zahlen so
  landen, wie die Matrix es vorschreibt.
- Der Windows-Player wurde gebaut, aber wie in GB-001 nicht ausgeführt.
- Das HUD wurde nicht gerendert. Die Presentation-Änderungen sind über eine
  vollständige Kompilierung des Skriptbaums gegen die echten
  Unity-Assemblies belegt, nicht über ein Bild.

### Entscheidung dieser Sitzung: D-074, unter Delegation vom Agenten getroffen

Die Fachdokumentation führte **drei einander widersprechende** Schadensmatrizen
(ArmorSystem.md 6 × 6, Infantry.md 6 × 4 plus „Kristall", Vehicles.md 5 × 4),
teils mit gegenläufigen Werten. Der Inhaber hat die Auflösung in dieser Sitzung
**ausdrücklich an den Agenten delegiert**; der Agent hat entschieden
(ArmorSystem.md ist alleinige Autorität) und implementiert. Das ist als
[D-074](DecisionLog.md) protokolliert und dort als agent-entschieden
gekennzeichnet – **nicht** als Inhaberentscheidung ausgegeben. Der Inhaber kann
sie jederzeit umstoßen; eine Umkehr wäre eine Datenänderung, keine
Strukturänderung.

### Aufgeschobene Dokumentation (D-067 K3, offene Schuld)

| # | Aufgeschoben | Fällig |
|---|---|---|
| 1 | Die vier offenen Schuldzeilen aus GB-001 (Wiki-Index, Pathfinding-Block v2 in [../tech/SimulationCore.md](../tech/SimulationCore.md)/[../tech/Serialization.md](../tech/Serialization.md), [RiskAnalysis.md](RiskAnalysis.md)/[OpenQuestions.md](OpenQuestions.md), Änderungsverläufe) bleiben offen und werden durch diese Sitzung nicht kleiner | vor Spurende (D-067 K5) |
| 2 | Snapshotblock 107 (`Victory`, v1, 48 Byte) ist in [../tech/Serialization.md](../tech/Serialization.md) nicht beschrieben; die Blockregistrierung selbst ist ein Q-040-Punkt und vor dem G1-Freeze per D-ID zu ratifizieren | vor G1-Freeze |
| 3 | [RiskAnalysis.md](RiskAnalysis.md) und [OpenQuestions.md](OpenQuestions.md) sind für diese Sitzung nicht nachgezogen; insbesondere fehlt der Fingerprint-Stub-Befund als Risiko-/Fragezeile | vor Sprint-Abschluss |

Nicht aufgeschoben und erledigt: `[Unreleased]`-Eintrag im
[CHANGELOG](../../CHANGELOG.md), D-074 im [DecisionLog](DecisionLog.md),
Bereinigung der widersprechenden Matrizen in
[../gamedesign/Infantry.md](../gamedesign/Infantry.md) und
[../gamedesign/Vehicles.md](../gamedesign/Vehicles.md) samt Versionsbump und
Änderungsverlauf, Autoritätsvermerk in
[../gamedesign/ArmorSystem.md](../gamedesign/ArmorSystem.md), fünf neue
Registerzeilen im [ScopeLedger](ScopeLedger.md), grüner `docs-check`,
`quality/**` unberührt, kein Commit und kein Push.

### Offene Befunde aus der Sitzung

- **Branch- und Verlustrisiko** (siehe Kopf dieses Eintrags) – zuerst zu
  klären.
- **`Bootstrap.unity` ist dreckig**, obwohl die Szene nicht im Schreibumfang
  stand: Der Verifikationslauf regeneriert sie. Die Änderung ist geprüft und
  gutartig (strukturgleich, dazu zwei neue serialisierte Felder an
  `UnitViewManager`), aber sie ist eine echte Arbeitsbaumänderung, die der
  Gate-Lauf selbst verursacht hat.
- **Der Fingerprint deckt keine Inhaltsänderung ab** (siehe „Ehrliche
  Grenzen"). Betrifft jeden künftigen Balancing-Patch; der Fingerprint ist
  außerdem ein Wire-Format auf dem Weg in den G1-Freeze.
- **Keine Zielerfassung.** Ein `AttackTarget` wird im gesamten Repository
  ausschließlich durch den manuellen Klick des Spielers gesetzt. Einheiten
  erwidern kein Feuer, Angriffsbewegung existiert nicht (ein Kommentar in
  `RtsDeviceInput` behauptet fälschlich das Gegenteil), und die
  Verteidigungsplattform – das einzige bewaffnete Gebäude in MS-1 – kann nie
  schießen. Das Konterdreieck ist damit real in der Tabelle, aber im Spiel nur
  durch Handsteuerung jedes einzelnen Angriffs beobachtbar.
- **33 der 36 Matrixzellen sind unerreichbar.** `RtsDeviceInput` bindet zwei
  Gebäude und zwei Einheiten an Tasten; jeder Explosivträger und jedes
  `Medium`-Ziel ist unbaubar. Die Techkette darunter funktioniert vollständig –
  es fehlen nur Tastenbelegungen.
- **`Stop` löscht das Angriffsziel nicht**, obwohl der Befehl an zwei Stellen
  genau das zusagt. Bei flachem 15er-Schaden unsichtbar, mit einer
  110er-Artillerie ein spürbarer Steuerungsfehler.
- **Angriffsbefehle sind auf eigene und auf sich selbst zulässig**: Der
  Command-Executor prüft das Ziel nur auf Existenz, nicht auf Zugehörigkeit.
  Mit lebender Siegauswertung ist das ein Selbst-Eliminierungspfad, der auf
  jedem Peer identisch abspielt und deshalb wie legitimes Spiel aussieht.
- **`tools/Nova.SimRunner/Program.cs`** registriert weiterhin sieben Systeme
  ohne Victory, während sein Kommentar „canonical tick order" behauptet. Heute
  folgenlos (Demopfad, kein gepinnter Test), aber der Kommentar ist unwahr.
- **Schreibumfangsabweichung, bewusst und gemeldet:** Der Kampfstrang hat in
  beiden Lanes `EconomyIntegrationTests` angefasst – ausschließlich das
  Schleifenbudget (100 → 200 Ticks) und einen veralteten Kommentar, keine
  Zusicherung. Grund: Der Angreifer dieses Tests ist eine rollenlose Einheit,
  die ein Kraftwerk beschießt; als `Building` mit 0,30 landet der Abschuss
  jetzt bei Tick 122 statt innerhalb von 100.
- **`UnitRole.Unit` behält den provisorischen 15er-Wert** und die
  `CombatSystem.Default*`-Konstanten bleiben als Aliasse bestehen, weil die
  bestehenden Kampf-Testsuiten sie zusichern und außerhalb des Schreibumfangs
  lagen. Auf dem Inhaltspfad liest sie nichts.
- **Die Streitkräftezählung im HUD umgeht den Fog of War** (sie liest den
  rohen Entitätsspeicher). Das ist auf dem Bildschirm als
  „Forces [debug, ignores fog]" ausgewiesen und darf so nicht in die echte UI
  wandern.

## Sitzung GB-003 – 2026-08-05 – Asset-Bereitschaft und erste Karte

**Branch:** `main` · **Commit:** keiner (Arbeitsbaum; kein Commit, kein Push,
kein Branch-Wechsel – Commit/PR als Inhaberentscheidung vorgelegt) ·
**Besetzung:** ein Orchestrierungs-Agent mit drei Lese-Scouts (Assets,
Code/Szenen, Produktionsdoku).

### Ziel

Der Inhaber hat den bevorstehenden Zufluss der 3D-Assets gemeldet und drei
Vorbereitungen angeordnet: den aktuellen Stand festhalten, die erste Karte
soweit vorbereiten, dass Assets eingesetzt werden können, und die erste
Demo-Runde vorbereiten – inklusive Auskunft, wo die Assets liegen.

### Schreibumfang der Sitzung

Innerhalb D-067 K2: `Scripts/Presentation/Maps/GlutrinneBlockoutView.cs` (neu),
`Scripts/Gameplay/Match/UnitViewManager.cs` (Prefab-Auflösung),
`Editor/BootstrapSceneGenerator.cs` und die von ihm erzeugte
`Bootstrap.unity`. **Bewusste, hiermit gemeldete Abweichungen vom K2-Weißbereich**
(vom Inhaberauftrag gedeckt, der die Asset-Einsatzfähigkeit angeordnet hat;
D-067 ist ohnehin unratifizierter Entwurf):

- `Editor/ArtAssetAutoSync.cs` (neu) – Editor-Tooling außerhalb des
  Generator-Weißbereichs; registriert PF_*-Prefabs und stempelt
  Import-Settings, ohne Spiel- oder Simulationscode zu berühren.
- `Scripts/Data/Registries/ArtAssetNaming.cs` (neu) und
  `Scripts/Data/AssetMappingRegistrySO.cs` (additive `ClearMappings`-Methode) –
  Nova.Data; keine Verhaltensänderung bestehender Pfade.
- `Assets/_Project/Art/**` (leere Standard-Ordnerstruktur mit `.gitkeep`),
  `Assets/_Project/Data/**` (zwei Datenassets: `MAP_Glutrinne.asset`,
  `AssetMappingRegistry.asset`, beide vom Generator/Tooling erzeugt).
- `Assets/Tests/EditMode/Data/ArtAssetNamingTests.cs` (neu, 5 Tests).

`quality/**`, `.github/workflows/**`, `VERSION` und **jeglicher
Simulations-/Core-Code** blieben unberührt – der Determinismus ist von dieser
Sitzung nicht betroffen, kein Hash-Baseline-Thema. Kein Gate-Status beansprucht.

### Ergebnis

- **Stand festgehalten:** [StatusSnapshot_2026-08-05.md](StatusSnapshot_2026-08-05.md)
  (Repo, Gates, Verifikation, Asset-Inventur).
- **Asset-Inventur:** Es liegt **kein einziges 3D-Asset** vor – null Treffer
  repo-weit (`*.fbx/obj/blend/gltf/glb`) und in den Ablageorten des
  Arbeitsrechners (Downloads/Desktop/Dokumente). Vorhanden sind 33
  Konzeptbilder (`docs/assets/concept-art/full/`) und 4 orthografische
  Referenzen (`docs/assets/reference/`).
- **Drop-Zone:** `Assets/_Project/Art/` exakt nach ArtAssetStandard §1
  (Buildings/Units × Alliance/Legion × Rollen, Shared/{Materials,Textures,
  Meshes}, Source/).
- **Pipeline:** `ArtAssetNaming.TryParsePrefabDefinitionId` koppelt
  `PF_UNIT_/PF_BLDG_<Faction>_<Role>` an die kanonische Definitions-Id
  (Allianz = Rollen-Wire-Wert, Legion +17); `ArtAssetAutoSync` baut die
  Registry bei jedem Import unter `Art/` vollständig neu auf (destruktiv-
  idempotent: manuelle Registry-Edits überleben keinen Sync – so gewollt)
  und setzt die §4-Import-Presets (Scale 1.0, keine FBX-Materialien, BC7,
  Masken linear, 1024/2048-Deckel). Menü: `Tools/Project Nova/Sync Art Asset
  Registry`.
- **Darstellung:** `UnitViewManager` löst je Entität Fraktion×Rolle über
  dieselbe Economy-Quelle auf wie Kampf/Wirtschaft und rendert ein
  registriertes Prefab (Pooling pro Quell-Prefab); ohne Treffer bleibt das
  Graybox-Primitiv. Prefab-Ground-Offset von 0,5 auf 0,0 korrigiert
  (ArtAssetStandard-Ursprungskonvention; der Pfad war bislang ungenutzt).
- **Erste Karte:** `GlutrinneBlockoutView` (Sandtönung, Kartenrand-Rahmen,
  Kristallmarker auf exakt den zwei registrierten Feldern) plus
  `MAP_Glutrinne.asset` (Graybox-Teilmenge: 2 Spawns, 2 Felder; kein
  erfundenes 5-Felder-Layout – das ist G4).
- **Demo:** [DemoRunbook.md](DemoRunbook.md) (Ablauf, Steuerung, Grenzen,
  Ablage-Anleitung).

### Verifikation (echte Zahlen, ausgeführt)

| Prüfung | Ergebnis |
|---|---|
| Unity-Kompilierung (Batchmode 6000.5.4f1) | Exit 0 nach zwei Korrekturen (siehe Befunde) |
| EditMode-Tests (Unity) | **410/410 grün**, 0 Fehler, 0 übersprungen (Basis 405, +5 Namenstests) |
| .NET-Tests (`tools/Nova.SimRunner.Tests`, Release) | **406/406 grün** (Baseline zu Sitzungsbeginn; Sim-Quellen unberührt) |
| Szenen-Regenerierung headless | Exit 0; `Bootstrap.unity` enthält `Map`-Objekt und `_assetMappings`-Verdrahtung |
| `docs-check` (`.github/scripts/check_docs.py`) | grün |
| Determinismus | nicht angetastet; letzter Fingerprint (GB-002) `0xAF9FB211B6C9CACE` |

### Ehrliche Grenzen dieser Verifikation

- Kein menschlicher Play-Durchlauf: ob Blockout-Tönung, Kristallmarker und
  Kartenrand **lesbar** sind, ist unverifiziert; belegt ist Kompilierung,
  Szenenstruktur und Tests.
- Die Prefab-Auflösung ist ohne reales Prefab nur als Codepfad und über die
  5 Parser-Tests belegt; der erste echte Drop-in-Test passiert mit dem
  ersten gelieferten Asset.
- Der Prefab-Tint färbt den ersten gefundenen Renderer einer Prefab-Hierarchie
  (Graybox-Näherung; der NovaUnit-Shader-Pfad ersetzt das).
- Player-Builds wurden in dieser Sitzung nicht neu gebaut.

### Aufgeschobene Dokumentation

Keine neue. DoD-Punkte 1–2 sind für diese Sitzung sofort erledigt
(Änderungsverläufe und Versionsbumps in allen berührten Dokumenten,
Wiki-Index-Einträge für DemoRunbook und StatusSnapshot, `[Unreleased]`-Eintrag,
ScopeLedger 0.4.0), weil D-067 unratifiziert ist und der Schuldmodus formal
nicht existiert. Die Altschulden aus GB-001/GB-002 bleiben unverändert offen.

### Offene Befunde aus der Sitzung

- **`-quit` frisst `-runTests`:** Der erste Testlauf (mit `-quit`) endete
  erfolgreich, ohne einen einzigen Test auszuführen – die Ergebnisdatei blieb
  alt. Der Fallstrick ist in `quality/scripts/run_gate_check.py:462`
  dokumentiert, aber leicht zu übersehen; ohne den zweiten Blick auf den
  Zeitstempel wäre ein falscher Grün-Stand protokolliert worden.
- **Unity-6-API-Drift:** `ModelImporterTangents.CalculateMikktspace` heißt
  jetzt `CalculateMikk`, `ModelImporter.importAnimation` ist ein Bool.
  Gegen die installierte Editor-Assembly verifiziert.
- **Nova.Editor referenziert Nova.Simulation nicht.** Die Referenzliste ist
  gate-prüfungsrelevant (Architekturcheck) und wurde **nicht** angefasst;
  stattdessen kapselt `ArtAssetNaming.TryParsePrefabDefinitionId` die
  Id-Auflösung vollständig in Nova.Data.
- **`MAP_`-Präfix:** `MAP_Glutrinne.asset` erweitert die Daten-Namenskonvention
  (`UNIT_`/`BLDG_`) um ein Karten-Präfix – als Arbeitskonvention gesetzt,
  kein DecisionLog-Eintrag (kein Design-Konflikt, jederzeit korrigierbar).
- **Registry ist maschinell:** `AssetMappingRegistry.asset` wird bei jedem
  Sync vollständig neu geschrieben; sie ist Build-Artefakt, kein
  Handarbeits-Dokument.

## Sitzung GB-004 – 2026-08-05 – Demo-Beweis, Wirtschaftsfix, Asset-Ankunft

**Branch:** `feat/glutrinne-demo-prep` · **Commit:** vorerst keiner
(Arbeitsbaum; GB-003 liegt als `577f5be` vor) · **Besetzung:** ein
Orchestrierungs-Agent; parallel lieferte ein zweiter Strang den
Tripo-Asset-Drop samt `Editor/ArtAssetPrefabBuilder.cs` und Import-Protokoll.

### Ziel

Der Inhaber meldete das Eintreffen der Assets „in wenigen Minuten" und
beauftragte: Restvorbereitung, ein Unity-Testlauf als sichtbarer Beweis, dass
es läuft, und nach Möglichkeit Verbesserungen an der Spielsimulation.

### Schreibumfang der Sitzung

Innerhalb D-067 K2: `Scripts/Presentation/UI/RtsDeviceInput.cs`,
`Scripts/Gameplay/Match/UnitViewManager.cs`. Darüber hinaus (vom
Inhaberauftrag „Simulation verbessern" gedeckt, nach D-068-Regeln mit Tests
in beiden Lanes und Determinismuslauf): `Scripts/Simulation/Economy/
EconomySystem.cs` (eine Methode) samt gespiegelten Tests in
`Assets/Tests/EditMode/Simulation/EconomySystemTests.cs` und
`tools/Nova.SimRunner.Tests/EconomySystemTests.cs`. Neu: die PlayMode-
Testinfrastruktur (`Assets/Tests/PlayMode/`), `Editor/UrpProjectSetup.cs`,
Projekt-Renderpipeline-Zuordnung (siehe Ergebnis), kleine Reparaturen an der
parallel gelieferten `Editor/ArtAssetPrefabBuilder.cs` (fehlendes
`using Nova.Data;`, Simulations-freie Parser-Variante). `quality/**`,
Workflows, `VERSION` unberührt.

### Ergebnis

- **Wirtschaftskreislauf repariert (D-068-Regeln).** Befund aus GB-001
  bestätigt und geschlossen: `EconomySystem.HasOwnRefineryInReach` maß die
  Abgabe-Reichweite vom **Footprint-Zentrum** der Raffinerie (die Entität
  spawnt bei origin+1), nicht vom Footprint. Die Start-Harvester standen
  Chebyshev 2 vom Zentrum entfernt, die Rückhol-Phase löste nie aus, volle
  Fracht blieb ewig liegen, die Credits froren bei 1.000 AE. Fix: Reichweite
  = 1 + Footprint-Radius (3×3 → 2). Zwei Regressionstests je Lane: Abgabe
  über den echten Platzierungspfad bei Zentrumsdistanz 2, und der volle
  Autozyklus (165 Ticks laden, Abgabe, Wiederaufnahme) in kanonischer
  Eröffnungsgeometrie.
- **Hash-Bewegung getrennt gemessen statt behauptet** (Verfahren aus GB-002,
  Stash-Überlagerung): Fingerprint `0x71045DC037C10250` und Checkpoint
  Tick 100 `0x9A2B01F88C03599D` sind mit und ohne Fix **identisch** (vor der
  ersten vollen Ladung um Tick 165; Eingaben unverändert); nur der finale
  Zustandshash wandert `0x29DE64BD1B6A9000 → 0xF25B56F8C3553AAC`. Die
  Differenz zum GB-002-Tripel stammt vollständig aus dem Fraktions-Merge
  (#11), nicht aus dieser Sitzung.
- **Bedienung:** Alle 17 MS-1-Rollen sind per Tastatur erreichbar (bisher 4;
  HQ bewusst unbelegt), Pause/Fortsetzen auf P (Tick-Stopp, kein
  Simulationszugriff), irreführender „attack-move"-Kommentar ersetzt durch
  die ehrliche Bezeichnung (keine Zielerfassung bei Ankunft).
- **Asset-Ankunft und Integration:** Alle 34 MS-1-Assets (Tripo,
  LOD0/1/2, BaseColor) landeten konventionkonform unter `Assets/_Project/Art/`
  samt `PROVENANCE.json`, Import-Protokoll
  `docs/assets/AssetImport_Tripo_2026-08-06.md` und
  `docs/assets/provenance-ledger.json`. Headless-Lauf von
  `ArtAssetPrefabBuilder.BuildMenu`: **34/34 Materialien + Prefabs gebaut,
  Registry synchronisiert.** Einschränkungen stehen im Import-Protokoll §4:
  Provenienz **nicht** erfüllt (Lizenzfelder leer), kein `_MSK`/TeamMask,
  kein Legion-Emissive, zwei sehr hohe Türme (Allianz-HQ 21,1 m, Radar 20,0 m),
  Restsplitter an beiden DefensePlatforms.
- **URP-Befund geschlossen:** Das Projekt hatte nie ein Pipeline-Asset
  zugeordnet (`GraphicsSettings.m_CustomRenderPipeline: {fileID: 0}`, alle
  Quality-Stufen leer) und renderte Built-in; die URP-Lit-Materialien der
  Assets liefen magenta (Unity-Defaultmaterialien von Boden/Kristallen
  blieben unauffällig, weil pipeline-agnostisch). `UrpProjectSetup`
  (reflection-basiert, weil der asmdef-Vertrag D-061 keine URP-Referenz
  zulässt) hat `Assets/_Project/Settings/NovaUrp(Renderer).asset` erzeugt
  und allen Quality-Stufen samt GraphicsSettings zugeordnet. Nach der
  Umstellung kippte das Spiegelbild auf: Unitys eingebautes
  Primitive-Defaultmaterial rendert unter URP magenta — Blockout
  (`GlutrinneBlockoutView`) und Graybox-Primitive (`UnitViewManager`,
  Baustellen) tragen seither Laufzeit-URP-Lit-Materialien (keine
  Asset-Dateien, `HideAndDontSave`).
- **Startkamera gerahmt:** Fokus von (4,4)/34 m auf (8,6)/42 m — die erste
  Ansicht zeigt die Basis als Ganzes statt nur den 21-m-HQ-Turm.
- **Sichtbarer Beweis:** PlayMode-Beweistest lädt die Bootstrap-Szene,
  prüft Match-Start, Tickfortschritt, sichtbare Views und wachsende Credits
  und schreibt Screenshots (RenderTexture-Capture;
  `ScreenCapture.CaptureScreenshot` ist unter `-batchmode` ein No-Op) nach
  `output/demo/`: Start, Wirtschaft, Übersicht, beide Basen.

### Verifikation (echte Zahlen, ausgeführt)

| Prüfung | Ergebnis |
|---|---|
| .NET-Tests (`tools/Nova.SimRunner.Tests`, Release) | **408/408 grün** (Basis 406, +2 Economy-Regression) |
| EditMode-Tests (Unity) | **412/412 grün** (Basis 410, +2 gespiegelte Economy-Tests) |
| `DETERMINISM_10000` SelfCheck | grün („Playback reproduced every recorded result and the recorded final state hash") |
| Hash-Tripel (neu) | Fingerprint `0x71045DC037C10250`, Checkpoint Tick 100 `0x9A2B01F88C03599D`, finaler Zustandshash `0xF25B56F8C3553AAC` |
| PlayMode-Tests (Unity) | **2/2 grün** (`BootstrapMatch_RunsRendersAndHarvests`, `SceneViews_RenderOverviewAndBothBases`) |
| Wirtschaftsnachweis im PlayMode-Log | `tick 30→200, credits 1000→1660 AE, visible views 13` |
| Prefab-Bau (headless) | 34/34, Registry-Sync 34 Einträge |
| Screenshots | 5 Dateien in `output/demo/`, vom Agenten eingesehen (siehe Grenzen) |

### Ehrliche Grenzen dieser Verifikation

- **Kein Team-Farb-Autorentest:** Der FactionTint multipliziert per
  Property-Block auf die BaseColor-Textur (ganzkörper-Tint statt
  TeamMask-Arealen); das ersetzt das fehlende `_MSK` nicht, ist aber als
  Zwischenlesbarkeit gedacht. Bewertung der Bildebene bleibt dem menschlichen
  Blick vorbehalten; der Agent hat die fünf PNGs eingesehen und den
  Magenta-Befund (URP) damit erst gefunden und dann als behoben verifiziert.
- **Die zwei sehr hohen Türme** (Import-Protokoll §4.5) sind im Render
  sichtbar und dominieren die Startkamera; Höhendeckel ist Art-Entscheid,
  nicht diese Spur.
- **HUD bleibt ungerendert** (IMGUI zeichnet nicht in RenderTextures);
  Spieler-Builds wurden in dieser Sitzung nicht neu gebaut.
- Der Windows-Player wurde wie in GB-001/002 nicht ausgeführt.

### Aufgeschobene Dokumentation

Keine neue. DoD-Punkte 1–2 sind sofort erledigt (Änderungsverläufe,
Versionen, Index, `[Unreleased]`). Die Altschulden aus GB-001/GB-002 bleiben
unverändert offen.

### Offene Befunde aus der Sitzung

- **Provenienz blockiert Repo-Aufnahme der Assets.** Die 34 Datensätze sind
  belegbar, aber lizenzseitig leer (`_TODO` je Datensatz). Die ~107 MB unter
  `Assets/_Project/Art/**` (plus generierte `.mat`/`.prefab`) bleiben bis
  zur Vervollständigung und Vier-Augen-Prüfung uncommittet — die Entscheidung
  liegt beim Inhaber (siehe auch Frage nach Git-LFS für Binärassets).
- **`Assets/Tests/PlayMode/Nova.PlayMode.Tests.asmdef` durfte nur Ränge < 4
  referenzieren** (Test-Regel des Architekturchecks); der erste Entwurf
  referenzierte Presentation und wurde korrigiert.
- **`ArtAssetPrefabBuilder` kam ohne `using Nova.Data;` und mit einer
  Simulations-referenzierenden Parser-Variante** (kompilierte nicht, weil
  Nova.Editor Nova.Simulation nicht referenziert); beides repariert, die
  Architektur-Referenzliste blieb unangetastet.
- **Parallele Schreibzugriffe:** Der Asset-Drop und der Prefab-Builder
  landeten mitten in laufenden Verifikationsläufen dieser Sitzung. Gutartig
  (neue Dateien, keine Überschreibungen), aber der Grund für zwei
  Fehlläufe; bei parallelen Strängen sollten Läufe und Drops zeitlich
  abgesprochen werden.
- **Bild-Ersteindruck (kein Befund, eine Beobachtung):** Ohne `_MSK` wirken
  die Modelle flächig; die Fraktionsunterscheidung trägt aktuell der Tint,
  nicht das Modell. Priorität für die nächste Art-Runde: `_MSK` für die
  vier Vertical-Slice-Assets (steht auch so im Import-Protokoll).

## Sitzung GB-005 – 2026-08-06 – Spielbarer Core-Loop (D-077)

**Branch:** vorerst keiner (Arbeitsbaum auf `main` @ `15dfe73`) ·
**Besetzung:** ein Orchestrierungs-Agent mit drei abgegrenzten
Umsetzungs-Subagenten (Sim-Loop, Sieg-Regel, KI).

### Ziel

Der Inhaber meldete die GB-004-Demo als nicht spielbar (Vollbild-Debug-Overlay,
alle 3D-Assets übereinander, kein erkennbarer Spielablauf) und beauftragte:
zuerst den Bestand vollständig analysieren und reparieren, dann den klassischen
C&C-Kernloop spielbar machen (HQ → Raffinerie → Harvester → Feld → Geld →
Kaserne → Einheiten → KI-Gegner → Sieg bei HQ-Zerstörung) — ohne paralleles
Zweitsystem.

### Befund

- **Overlay:** `DebugHud` (OnGUI) auf dem `UI`-Objekt, `_visible: 1`,
  `_uiScale: 2` — dokumentiertes Substitut, kein Versehen, aber als Default
  untauglich.
- **„Gestapelte" Assets:** kein Spawn-Fehler, sondern Maßstabskonflikt —
  Exportkonvention 1 Zelle = 3,0 m (D-071) gegen Sim-Welt 1 Zelle = 1 WE;
  ~9 m breite Gebäude standen 4 m auseinander, Einheiten unsichtbar in den
  Meshes.
- **Sim-Kern trug den Loop bereits** (Wirtschaft, Autozyklus, Bau, Produktion,
  Kampf, Sieg); die echten Lücken waren: KI-Stub nicht registriert, Start mit
  gratis Raffinerie/Harvestern, Harvester aus dem HQ, Sieg nur bei
  Totalvernichtung.

### Ergebnis (alle Punkte: D-077)

- Start je Slot HQ + 1 Builder + 3.000 AE; Harvester-Produzent = Raffinerie;
  Raffinerie ohne Kraftwerk-Prereq (Power-Bedarf bleibt); Sieg zusätzlich bei
  HQ-Verlust (Victory-Snapshot v2, Clean Break); `SkirmishAiSystem` registriert
  und spielend (Intent-Pfad, FoW-legal, Infanterie-Wellen); `DebugHud`
  standardmäßig aus (F3), Statusleiste immer an; `UnitViewManager` normiert
  Prefab-Views zur Laufzeit aus den Mesh-Bounds auf den Sim-Footprint.
- Folgefix: `ProductionSystem` liest Produzentenrollen aus der
  Definitionstabelle (Rally-Point auf der Raffinerie wurde sonst abgelehnt).
- Vertrag `quality/content/mvp-v1.json` 1.0.0 → 1.2.0; D-056 Klausel 2
  teilweise ersetzt.

### Verifikation (tatsächlich gelaufen)

- `dotnet test tools/Nova.SimRunner.Tests`: 420/420 grün; SimRunner-Default
  und DETERMINISM_10000 PASS (End-to-End: KI besiegt passiven Slot bei Tick
  2.242, VictoryElimination, deterministisch).
- Unity Batchmode: `Bootstrap.unity` regeneriert (`_visible: 0`); EditMode
  **425/425** (erstmals inklusive InitialStateHash-Parität Bootstrap ==
  Szenario im Editor); PlayMode **2/2** mit frischen Screenshots
  (`output/demo/`, Skalierung visuell bestätigt: keine Überlagerung mehr,
  Baustelle der Raffinerie sichtbar).
- **Fallstrick protokolliert (Wiederholung des GB-004-Befundes):** Ein
  Batchmode-Lauf mit `-quit` neben `-runTests` endete „erfolgreich", ohne
  einen Test auszuführen — die Result-XMLs blieben alt und zeigten
  irreführende Grün-Stände. Erst der Zeitstempel-Check entlarvte es. Die
  Notiz in `quality/scripts/run_gate_check.py` (kein `-quit` mit `-runTests`)
  gilt weiterhin; ohne `-nographics` für PlayMode (Screenshot-Capture braucht
  die GPU).
- Erster **menschlicher** Play-Durchlauf steht weiter aus (Runbook §4 ist auf
  GB-005 aktualisiert).

## Offene Punkte

- D-067 und D-068 sind Entwürfe. Ohne Ratifizierung existiert der
  Dokumentationsschuld-Modus formal nicht – die Schuld oben ist dann sofort
  fällig statt befristet.
- Der Verfallstermin nach D-067 K5 ist an die Ratifizierung gebunden und
  deshalb noch nicht datiert.
- Ob die Graybox visuell lesbar und die Steuerung brauchbar ist, ist offen,
  bis der Inhaber einmal gespielt hat.
- Die Arbeit aus GB-002 liegt uncommittet auf `feat/hashkrieg-concept-art`.
  Solange das so bleibt, ist sie ein `git checkout` von der Vernichtung
  entfernt.
- D-074 ist in Kraft, aber vom Agenten unter Delegation entschieden. Die
  Bestätigung oder Umkehr durch den Inhaber steht aus.

## Nächste Schritte

1. Branchlage aus GB-002 klären und die Arbeit sichern, bevor irgendetwas
   anderes passiert.
2. Inhaberentscheidung zu D-067 und D-068 einholen sowie D-074 bestätigen oder
   umstoßen; erst danach ist der Schuldmodus in Kraft und der Verfallstermin
   datierbar.
3. Ersten menschlichen Play-Durchlauf durchführen (Anleitung in der
   Root-[README.md](../../README.md)) und Rückmeldung als GB-003 protokollieren.
4. Schuldzeilen aus GB-001 und GB-002 abarbeiten, spätestens zum Verfall nach
   D-067 K5.
5. G2-Arbeit beginnt erst nach bestandenem G0/G1; die Graybox bleibt bis dahin
   Diagnose ohne Gate-Anspruch.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-26 | Erstfassung mit Protokollregeln und Sitzung GB-001 (sichtbarer und bedienbarer Graybox-Slice) | Technical Writer |
| 0.2.0 | 2026-07-26 | Sitzung GB-002 (Schadensmatrix nach D-074, Siegauswertung nach D-056, HUD-Sichtbarkeit) angehängt – einschließlich Branch-Befund, getrennter Hash-Zuordnung, Fingerprint-Stub-Befund und elf offenen Befunden | Technical Writer |
| 0.3.0 | 2026-08-05 | Sitzung GB-003 (Asset-Bereitschaft, Glutrinne-Blockout, Demo-Runbook, Status-Snapshot) angehängt – einschließlich K2-Abweichungsdeklaration, Asset-Inventur (null 3D-Assets) und fünf offenen Befunden | Technical Writer |
| 0.4.0 | 2026-08-05 | Sitzung GB-004 (Demo-Beweis, Harvester-Footprint-Fix, URP-Verdrahtung, 34 Tripo-Assets integriert) angehängt – Kopfzeile war ohne Tabellenzeile geblieben, hier nachgezogen | Technical Writer |
| 0.5.0 | 2026-08-06 | Sitzung GB-005 (spielbarer Core-Loop nach D-077) angehängt – Befund Overlay/Maßstab, Sieg bei HQ-Verlust, KI-Slot aktiv, Verifikation 420/425/2 grün, `-quit`-Fallstrick erneut protokolliert | Agent |
| 0.4.0 | 2026-08-05 | Sitzung GB-004 (Wirtschaftsfix nach D-068-Regeln, volle Tastenbelegung, URP-Zuordnung, Tripo-Asset-Integration 34/34, PlayMode-Sichtbeweis mit Screenshots) angehängt – einschließlich getrennter Hash-Zuordnung und fünf offenen Befunden | Technical Writer |
