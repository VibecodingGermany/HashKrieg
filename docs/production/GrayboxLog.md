# Graybox-Log

**Version:** 0.1.0 | **Status:** Entwurf – append-only Sitzungsprotokoll (trägt D-067, noch nicht ratifiziert) | **Verantwortungsbereich:** Orchestrator / Technical Writer | **Sprint:** 7

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
  (Sim-Korrekturen im Pre-G1-Fenster, Entwurf)
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

## Offene Punkte

- D-067 und D-068 sind Entwürfe. Ohne Ratifizierung existiert der
  Dokumentationsschuld-Modus formal nicht – die Schuld oben ist dann sofort
  fällig statt befristet.
- Der Verfallstermin nach D-067 K5 ist an die Ratifizierung gebunden und
  deshalb noch nicht datiert.
- Ob die Graybox visuell lesbar und die Steuerung brauchbar ist, ist offen,
  bis der Inhaber einmal gespielt hat.

## Nächste Schritte

1. Inhaberentscheidung zu D-067 und D-068 einholen; erst danach ist der
   Schuldmodus in Kraft und der Verfallstermin datierbar.
2. Ersten menschlichen Play-Durchlauf durchführen (Anleitung in der
   Root-[README.md](../../README.md)) und Rückmeldung als GB-002 protokollieren.
3. Schuldzeilen 1–4 abarbeiten, spätestens zum Verfall nach D-067 K5.
4. G2-Arbeit beginnt erst nach bestandenem G0/G1; die Graybox bleibt bis dahin
   Diagnose ohne Gate-Anspruch.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-26 | Erstfassung mit Protokollregeln und Sitzung GB-001 (sichtbarer und bedienbarer Graybox-Slice) | Technical Writer |
