# Modulspezifikation – Commander- & Doktrinen-System (`Nova.Simulation.Commanders`)

**Version:** 1.0.0 | **Status:** Freigegeben (Phase 2 / Modul 17) | **Verantwortungsbereich:** Game Designer / Lead Technical Director | **Sprint:** Phase 2 (Modul 17)

## Zweck

Dieses Dokument beschreibt das deterministische **Commander- & Doktrinen-System** von *Project Nova*. Das Modul verwaltet den passiven Aufbau von Commander-Energie, führt Cooldown-Timer für aktive Fähigkeiten (`CommanderAbilityDefinition`) aus und wendet Bereichs-Effekte (z. B. Orbital-Schläge oder Schild-Boosts) auf Einheiten an.

---

## 1. Modul-Architektur

* **Assembly:** `Nova.Simulation.dll` (`noEngineReferences: true`)
* **Energie-Generierung:** +1 Commander Energy alle **20 Ticks** (1,0 Sekunde).
* **Allokationsfreiheit:** 0 GC Bytes bei Aktivierung von Fähigkeiten.

```text
[ Player UI / Commander Ability Command ]
                 │
                 ▼
        [ CommanderSystem ] ──► Validate Energy Cost & Cooldown Ticks
                 │
                 ├── Deduct Commander Energy
                 ├── Set Cooldown Timer
                 └── Apply Area Effect ──► Damage Enemy Units in Radius (EntityManager)
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`CommanderSystemTests.cs`](../../../Assets/Tests/EditMode/Simulation/CommanderSystemTests.cs) (Validierung der Energie-Abbuchung, Cooldowns und Treffer-Berechnungen).
