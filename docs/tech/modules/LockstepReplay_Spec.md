# Modulspezifikation – Lockstep State Hashing, Replay & Visual Debug View (`Nova.Simulation.State` & `Nova.Presentation`)

**Version:** 1.1.0 | **Status:** historischer Prototyp-/Scaffolding-Stand gemäß D-055 – nicht verbindlich | **Verantwortungsbereich:** Lead Technical Director | **Sprint:** 7

## Zweck

Dieses Dokument beschreibt das **Lockstep State Hashing**, die **Replay-Aufzeichnung** und die **Scene View Debug-Visualisierung** von *Project Nova*. Es garantiert die Erkennung von Desynchronisationen im Multiplayer und ermöglicht die exakte Wiedergabe von aufgezeichneten Matches.


## Abhängigkeiten

- [../../production/MVPRecoveryPlan.md](../../production/MVPRecoveryPlan.md) – aktiver Gate- und Statusvertrag
- [../../production/DecisionLog.md](../../production/DecisionLog.md) – D-055 bis D-061
- [../ModuleOverview.md](../ModuleOverview.md) – aktive Modul- und State-Hoheit
- [../SimulationCore.md](../SimulationCore.md) und [../Commands.md](../Commands.md) – führende Kernverträge

> **Recovery-Hinweis:** Der folgende Text konserviert den nach D-055 nicht
> abgenommenen Prototyp-/Scaffolding-Stand. Er ist keine Implementierungsfreigabe.
> Bei jedem Konflikt führen die oben verlinkten aktiven Verträge. Eine künftige
> Freigabe erfordert das zuständige Gate, neue Laufzeitevidenz und eine
> inhaltlich rebaselinede Spezifikation.

---

## 1. Modul-Architektur

* **Assemblies:** `Nova.Simulation.dll` (`noEngineReferences: true`) & `Nova.Presentation.dll`
* **State Hashing:** FNV-1a 64-Bit Hashing über alle aktiven Entitäten (Index, Version, Position, Rotation, Lebenspunkte).
* **Replay Recording:** `ReplayBuffer` speichert `(TickIndex, CommandEnvelope)` Sequenzen für deterministischen Replay-Loop.
* **Gizmo Visualisierung:** `FlowFieldDebugView` zeichnet 8-Wege-Flussvektoren und Geländehindernisse in der Unity Scene View.

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`LockstepReplayTests.cs`](../../../Assets/Tests/EditMode/Simulation/LockstepReplayTests.cs) (Validierung der Hash-Berechnung und Replay-Replikation).

## Offene Punkte

- Welche Teile dieses Prototyps nach Abgleich mit D-056 bis D-061 wiederverwendet
  werden, entscheidet erst die Implementierung im zuständigen Gate.

## Nächste Schritte

1. Bestand gegen die aktiven Kern-, Inhalts- und Gate-Verträge prüfen.
2. Widersprechende APIs und Werte nicht übernehmen.
3. Erst nach bestandener Gate-Evidenz eine neue verbindliche Revision erstellen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-24 | Historischen Prototyp-/Scaffolding-Stand dokumentiert | Modulverantwortliche |
| 1.1.0 | 2026-07-24 | Freigabe gemäß D-055 entzogen und aktive Recovery-Verträge als führend verankert | Lead Technical Director |
