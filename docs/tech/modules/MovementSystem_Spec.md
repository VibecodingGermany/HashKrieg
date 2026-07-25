# Modulspezifikation – Movement System & Entity Storage (`Nova.Simulation.Movement`)

**Version:** 1.0.0 | **Status:** Freigegeben (Sprint 7) | **Verantwortungsbereich:** Lead Technical Director / Sim Engine Architect | **Sprint:** 7

## Zweck

Dieses Dokument beschreibt die Architektur, das Datenmodell und die Performanz-Garantien des Moduls **Entity Storage & Movement System**. Das Modul stellt die vorallokierte Entitätsverwaltung (`EntityManager`) und die deterministische Einheiten-Bewegung (`MovementSystem`) bereit.

---

## 1. Modul-Architektur & Layering

* **Assembly:** `Nova.Simulation.dll` (`noEngineReferences: true`)
* **Abhängigkeiten:** `Nova.Core` (Basistypen `EntityId`, `Tick`, `INovaLogger`, `ISimRandom`)
* **Speicher-Prämisse:** Vorallokierte Arrays und unboxed Structs – **0 Bytes GC-Allokationen** während des Simulations-Ticks.

```text
[ Nova.Core ]
     ▲
     │
[ Nova.Simulation.State ]
     ├── Transform2D (Struct, 12 Bytes)
     ├── UnitState (Struct)
     └── EntityManager (Array-Storage + Free-List Recycling)
     ▲
     │
[ Nova.Simulation.Movement ]
     └── MovementSystem (ISimSystem) ◄─── queries ─── FlowField (PathfindingSystem)
```

---

## 2. Speicher- & Datenmodell

### 2.1 `EntityManager`
* Vorallokiertes lineares Speicher-Array `UnitState[Capacity]` (Standard: 2.048 Slots).
* Indexbasierte Handle-Verwaltung mit 16-Bit-Versionierung zur Vermeidung veralteter Handle-Zugriffe.
* Allokationsfreie Slot-Wiederverwendung via `Stack<int> _freeSlots`.

### 2.2 `MovementSystem`
* Festgelegtes Tick-Intervall: **`TickDeltaSeconds = 0.05s`** (20 Ticks/Sekunde).
* Kombiniert Flussfeld-Richtungsvektoren von `PathfindingSystem` mit einer lokalen Abstands-Steering-Kraft (`separation`), um Stapeln von Einheiten zu verhindern.

---

## 3. Performanz-Budget & Benchmark-Ergebnisse

* **Budget-Oberkalkulation:** ≤ 2,0 ms pro Tick bei 1.000 gleichzeitig bewegten Einheiten.
* **Messung:** Automatisiert über [`MovementPerformanceTests.cs`](../../../Assets/Tests/EditMode/Simulation/MovementPerformanceTests.cs).

---

## 4. Qualitäts- & Testabdeckung

* **Unit Tests:** [`MovementSystemTests.cs`](../../../Assets/Tests/EditMode/Simulation/MovementSystemTests.cs) (Slot-Recycling, Handlegültigkeit, Zielbewegung).
* **Performance Benchmark:** [`MovementPerformanceTests.cs`](../../../Assets/Tests/EditMode/Simulation/MovementPerformanceTests.cs) (1.000 Einheiten über 50 Ticks).
