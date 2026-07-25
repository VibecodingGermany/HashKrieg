# Modulspezifikation – 3. Fraktion: Die Evolvierten (`Nova.Simulation.Factions`)

**Version:** 1.0.0 | **Status:** Freigegeben (Phase 2 / Modul 16) | **Verantwortungsbereich:** Faction Designer / Lead Technical Director | **Sprint:** Phase 2 (Modul 16)

## Zweck

Dieses Dokument beschreibt die organischen Fraktions-Mechaniken der **3. Fraktion "Die Evolvierten"** von *Project Nova*. Das Modul verwaltet die Biomasse-Verbreitung (`BiomassGrid`) und gewährt Einheiten auf Biomasse-Zellen eine passive Lebenspunkte-Regeneration (+2 HP alle 0,5 Sekunden).

---

## 1. Modul-Architektur

* **Assembly:** `Nova.Simulation.dll` (`noEngineReferences: true`)
* **Regenerations-Intervall:** Exakt alle **10 Ticks** (0,5 Sekunden).
* **Allokationsfreiheit:** 0 GC Bytes in der Regenerationsschleife.

```text
[ BiomassGrid ] ──► Track Organic Cells (Biomass Coverage)
                         │
                         ▼
             [ EvolvedFactionSystem ] (Regeneration Tick % 10 == 0)
                         │
                         └── Unit on Biomass? ──► Heal +2 HP (EntityManager)
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`EvolvedFactionTests.cs`](../../../Assets/Tests/EditMode/Simulation/EvolvedFactionTests.cs) (Validierung der Biomasse-Raster-Abdeckung und der passiven Regeneration).
