# Modulspezifikation – Asset-Integration MS-1 (`Nova.Data`)

**Version:** 1.0.0 | **Status:** Freigegeben (Phase 1 / Modul 15) | **Verantwortungsbereich:** Lead Technical Director / Asset Pipeline Lead | **Sprint:** Phase 1 (Modul 15)

## Zweck

Dieses Dokument beschreibt das **Asset-Integrations-System** von *Project Nova*. Das Modul verknüpft deterministische Simulations-Definitionen (`DefinitionId`) mit visuellen 3D-Modell-Assets (CC0-Asset-Bibliotheken Kenney/Quaternius aus Sprint 5 Asset Audit) für 27 Einheiten- und 24 Gebäudetypen.

---

## 1. Modul-Architektur

* **Assembly:** `Nova.Data.dll` (`noEngineReferences: false`)
* **Hauptkomponente:** `AssetMappingRegistrySO` (ScriptableObject zur Koppelung von Definition-IDs an GameObjects/Prefabs).

```text
[ GameDatabaseMasterSO ] ──► Unit / Building Definition IDs
                                    │
                                    ▼
                      [ AssetMappingRegistrySO ] ──► Prefab Lookups (27 Units & 24 Buildings)
                                    │
                                    ▼
                         [ UnitViewManager ] ──► 60 FPS View Spawns
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`AssetIntegrationTests.cs`](../../../Assets/Tests/EditMode/Data/AssetIntegrationTests.cs) (Validierung der Zuordnungen und Lookup-Funktionalität).
