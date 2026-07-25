# Modulspezifikation – Combat & Damage Pipeline (`Nova.Simulation.Combat`)

**Version:** 1.0.0 | **Status:** Freigegeben (Sprint 7) | **Verantwortungsbereich:** Sim Engine Architect / Gameplay Developer | **Sprint:** 7

## Zweck

Dieses Dokument beschreibt die deterministische **Combat & Damage Pipeline** von *Project Nova*. Das Modul führt Entfernungs- und Cooldown-Prüfungen aus, wendet Schaden an und entfernt zerstörte Einheiten allokationsfrei aus dem `EntityManager`.

---

## 1. Modul-Architektur

* **Assembly:** `Nova.Simulation.dll` (`noEngineReferences: true`)
* **Allokationsfreiheit:** 0 GC Bytes in Hot-Loops.

```text
[ EntityManager ] ──► UnitState (AttackTarget, Health, Cooldown)
                             │
                             ▼
                    [ CombatSystem ]
                             │
                             ├── Range Check (Transform2D)
                             ├── Cooldown Decrement
                             └── Despawn on Health <= 0
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`CombatSystemTests.cs`](../../../Assets/Tests/EditMode/Simulation/CombatSystemTests.cs) (Validierung der Waffenfrequenz, Schadensberechnung und Einheitenzerstörung).
