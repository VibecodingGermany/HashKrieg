# Modulspezifikation – Lockstep State Hashing, Replay & Visual Debug View (`Nova.Simulation.State` & `Nova.Presentation`)

**Version:** 1.0.0 | **Status:** Freigegeben (Sprint 7) | **Verantwortungsbereich:** Lead Technical Director | **Sprint:** 7

## Zweck

Dieses Dokument beschreibt das **Lockstep State Hashing**, die **Replay-Aufzeichnung** und die **Scene View Debug-Visualisierung** von *Project Nova*. Es garantiert die Erkennung von Desynchronisationen im Multiplayer und ermöglicht die exakte Wiedergabe von aufgezeichneten Matches.

---

## 1. Modul-Architektur

* **Assemblies:** `Nova.Simulation.dll` (`noEngineReferences: true`) & `Nova.Presentation.dll`
* **State Hashing:** FNV-1a 64-Bit Hashing über alle aktiven Entitäten (Index, Version, Position, Rotation, Lebenspunkte).
* **Replay Recording:** `ReplayBuffer` speichert `(TickIndex, CommandEnvelope)` Sequenzen für deterministischen Replay-Loop.
* **Gizmo Visualisierung:** `FlowFieldDebugView` zeichnet 8-Wege-Flussvektoren und Geländehindernisse in der Unity Scene View.

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`LockstepReplayTests.cs`](../../../Assets/Tests/EditMode/Simulation/LockstepReplayTests.cs) (Validierung der Hash-Berechnung und Replay-Replikation).
