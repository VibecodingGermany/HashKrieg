# Modulspezifikation – Unity Gameplay Integration & View Bridge (`Nova.Gameplay`)

**Version:** 1.0.0 | **Status:** Freigegeben (Sprint 7) | **Verantwortungsbereich:** Gameplay Engine Developer | **Sprint:** 7

## Zweck

Dieses Dokument beschreibt die Kopplung des deterministischen C#-Simulationskerns (`Nova.Simulation`) an den Unity Frame-Render-Loop (`Nova.Gameplay` & `Nova.Presentation`). Das Modul treibt die 20-Hz-Simulation über `MatchRunner` an und interpoliert visuelle GameObjects flüssig bei 60–120+ FPS.

---

## 1. Modul-Architektur

```text
[ Unity Frame Loop (Update / LateUpdate) ]
                 │
                 ├── Accumulates Time.deltaTime ──► MatchRunner.cs (20 Ticks/sec)
                 │                                        │
                 │                                        ▼
                 │                              SimulationKernel.StepTick()
                 │                                        │
                 └── UnitViewManager.cs (LateUpdate) ─────┤
                          │                               ▼
                          └── Smooth Lerp ◄── Transform2D State
```

---

## 2. Kernkomponenten

### 2.1 `MatchRunner`
* MonoBehaviour-Komponente, die `Time.deltaTime` akkumuliert und bei $\ge 0{,}05\text{s}$ exakt einen Simulationstick an `SimulationKernel.StepTick()` übergibt.
* Verwaltet die Initialisierung und Lebensdauer von `SimulationKernel`, `EntityManager`, `PathfindingSystem` und `MovementSystem`.

### 2.2 `UnitViewManager`
* Erzeugt und verwaltet visuelle GameObjects für aktive Entitäten im `EntityManager`.
* Interpoliert Position und Rotation in Unity `LateUpdate()` via `Vector3.Lerp` und `Quaternion.Slerp` mit variabler Speed ($25\text{s}^{-1}$), um auch bei 20-Hz-Simulation Ruckeln im Rendering zu verhindern.

### 2.3 `PathfindingTestBootstrap`
* Test-Runner zur Demonstration von 500 gleichzeitigen Einheiten auf einem 128x128-Grid mit Wand-Hindernis.

---

## 3. Qualitätssicherung & Tests

* **Unit Tests:** [`MatchRunnerTests.cs`](../../../Assets/Tests/EditMode/Gameplay/MatchRunnerTests.cs) (Akkumulator-Logik & Match-Lebenszyklus).
* **Integration Tests:** Headless SimRunner & Batchmode Compilation.
