# Modulspezifikation – Map- & Biom-Erweiterung (`Nova.Presentation.Maps`)

**Version:** 1.0.0 | **Status:** Freigegeben (Phase 2 / Modul 19) | **Verantwortungsbereich:** Level Designer / Lead Technical Director | **Sprint:** Phase 2 (Modul 19)

## Zweck

Dieses Dokument beschreibt die Präsentations-Struktur der **Map- & Biom-Erweiterung** von *Project Nova*. Das Modul verwaltet Karten-Layouts (`MapDefinitionSO`) für 1v1- und 2v2-Gefechte, 2 bis 4 Spieler-Spawn-Punkte, Aetherium-Kristallknoten-Positionen sowie drei Biom-Umgebungen (`Desert`, `Snow`, `JungleIndustrial`).

---

## 1. Modul-Architektur

* **Assembly:** `Nova.Presentation.Maps.dll` (`noEngineReferences: false`)
* **Biome:**
  1. `Desert` (0): Trockenes Wüsten-Biom.
  2. `Snow` (1): Verschneite Arktis-Gletscher.
  3. `JungleIndustrial` (2): Dschungel / Industriekomplex.

```text
[ MatchRunner / Game Setup ]
              │
              ▼
    [ MapDefinitionSO ] ──► Validate Map Dimensions & Spawn Points (IsValid)
              │
              ├── Spawn Base HQs ─────► ConstructionSystem
              └── Spawn Aetherium ───► ResourceHarvestingSystem
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`MapDefinitionTests.cs`](../../../Assets/Tests/EditMode/Presentation/MapDefinitionTests.cs) (Validierung von Kartenabmessungen, Spawn-Punkten und Biom-Typen).
