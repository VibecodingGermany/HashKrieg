# Modulspezifikation – Skirmish-KI (Allianz & Legion) (`Nova.AI`)

**Version:** 1.0.0 | **Status:** Freigegeben (Phase 1 / Modul 13) | **Verantwortungsbereich:** AI Architect / Lead Technical Director | **Sprint:** Phase 1 (Modul 13)

## Zweck

Dieses Dokument beschreibt das deterministische **Skirmish-KI-System** von *Project Nova*. Das Modul führt eine nutzenbasierte Entscheidungsschleife für Allianz- und Legion-Fraktionen aus, bewertet den Wirtschafts- und Energieaufbau, erteilt automatische Baubefehle für Gebäude und Einheiten und formiert Angriffs-Squads.

---

## 1. Modul-Architektur

* **Assembly:** `Nova.AI.dll` (`noEngineReferences: true`)
* **Entscheidungs-Intervall:** Exakt alle **20 Ticks** (1,0 Sekunde).
* **Allokationsfreiheit:** 0 GC Bytes in der Entscheidungsschleife.

```text
[ SimulationKernel ] (Tick % 20 == 0)
         │
         ▼
[ SkirmishAiSystem ] ──► Evaluate Power Margin ──► Request PowerPlant (ConstructionSystem)
         │
         ├── Evaluate Credit Balance ───────► Enqueue Unit Production (ProductionQueueSystem)
         └── Evaluate Army Size ────────────► Dispatch Attack Squad Orders (CommandProcessorSystem)
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`SkirmishAiTests.cs`](../../../Assets/Tests/EditMode/AI/SkirmishAiTests.cs) (Validierung der automatischen Bauplatz- und Produktionsentscheidungen).
