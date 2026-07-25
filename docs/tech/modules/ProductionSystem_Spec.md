# Modulspezifikation – Production Queue & Tech-Tree System (`Nova.Simulation.Production`)

**Version:** 1.0.0 | **Status:** Freigegeben (Phase 1 / Modul 11) | **Verantwortungsbereich:** Lead Technical Director / Sim Engine Architect | **Sprint:** Phase 1 (Modul 11)

## Zweck

Dieses Dokument beschreibt das deterministische **Production Queue & Tech-Tree System** von *Project Nova*. Das Modul führt Einheiten-Produktions-Queues in Kasernen und Fabriken aus, berechnet Produktionszeit-Timer unter Berücksichtigung von Low-Power-Mali und spawnt fertiggestellte Einheiten in den `EntityManager`. Zudem verwaltet das Modul die Tech-Tier-Stufen der Spieler.

---

## 1. Modul-Architektur

* **Assembly:** `Nova.Simulation.dll` (`noEngineReferences: true`)
* **Speichermodell:** Fixed-Size `ProductionItemState[128]` (0 GC Allokationen).

```text
[ Player UI / Train Unit Command ]
                 │
                 ▼
     [ ProductionQueueSystem ] ──► ResearchTreeSystem (Tech Tier Check)
                 │
                 ├── Deduct Aetherium Credits (EnergyGridSystem)
                 ├── Progress Production Timers (BuildTimeTicks)
                 └── Completion ──► EntityManager.SpawnUnit()
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`ProductionSystemTests.cs`](../../../Assets/Tests/EditMode/Simulation/ProductionSystemTests.cs) (Einheiten-Produktions-Queues, Guthabenabbuchung, Tech-Tier-Freischaltung und automatische Spawns).
