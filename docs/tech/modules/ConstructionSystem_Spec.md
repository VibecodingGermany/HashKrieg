# Modulspezifikation – Construction & Building System (`Nova.Simulation.Construction`)

**Version:** 1.0.0 | **Status:** Freigegeben (Phase 1 / Modul 10) | **Verantwortungsbereich:** Lead Technical Director / Sim Engine Architect | **Sprint:** Phase 1 (Modul 10)

## Zweck

Dieses Dokument beschreibt das deterministische **Construction & Building System** von *Project Nova*. Das Modul validiert Bauplatz-Belegungen auf einem diskreten 2D-Raster (`ConstructionGrid`), führt Bauzeit-Timer aus und registriert fertige Gebäude im Energienetzwerk (`EnergyGridSystem`).

---

## 1. Modul-Architektur

* **Assembly:** `Nova.Simulation.dll` (`noEngineReferences: true`)
* **Speichermodell:** Fixed-Size `BuildingSiteState[128]` (0 GC Allokationen).

```text
[ Player UI / Construction Command ]
                 │
                 ▼
    [ ConstructionSystem ] ──► ConstructionGrid (Cell Occupancy Validation)
                 │
                 ├── Progress Construction Timers (BuildTimeTicks)
                 └── Completion ──► EnergyGridSystem.RegisterPowerProduction / Consumption
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`ConstructionSystemTests.cs`](../../../Assets/Tests/EditMode/Simulation/ConstructionSystemTests.cs) (Rasterbelegung, Abbuchung von Guthaben, Bauzeit-Fortschritt und Netzwerkintegration).
