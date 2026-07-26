# Graybox-Log

**Version:** 0.2.0 | **Status:** Entwurf – append-only Sitzungsprotokoll (trägt D-067, noch nicht ratifiziert) | **Verantwortungsbereich:** Orchestrator / Technical Writer | **Sprint:** 7

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
