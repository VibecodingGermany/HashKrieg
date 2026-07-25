# Modulspezifikation – GameDatabase Sharding & Master Index (`Nova.Data` & `Nova.Editor`)

**Version:** 1.0.0 | **Status:** Freigegeben (Sprint 7) | **Verantwortungsbereich:** Data Architect / Lead Technical Director | **Sprint:** 7

## Zweck

Dieses Dokument beschreibt die Architektur und das Sharding-Datenmodell des **GameDatabase Master Index** gemäß Decision D-049. Um Merge-Konflikte im Git-Repository bei der parallelen Arbeit mehrerer Entwickler und KI-Agenten zu verhindern, werden Definitionen kategorieweise in Sub-Registries aufgeteilt und über ein automatisches Editor-Tool in einem Master-Index aggregiert.

---

## 1. Modul-Architektur (D-049 Sharding)

```text
[ Assets/_Project/Data/Units/  Buildings/  Weapons/ ... ]
                       │ (Definition SOs)
                       ▼
        [ GameDatabaseGenerator.cs (Nova.Editor) ]
                       │ (Build & Validate)
                       ▼
  ┌────────────────────┴────────────────────┐
  │ Category Sub-Registries                 │
  ├─────────────────────────────────────────┤
  │ Assets/_Project/Data/Registries/        │
  │   ├── UnitRegistry.asset                │
  │   ├── BuildingRegistry.asset            │
  │   └── WeaponRegistry.asset              │
  └────────────────────┬────────────────────┘
                       │
                       ▼
    [ GameDatabaseMaster.asset (Master Index) ]
                       │
                       ▼
        [ Sim-Definition Conversion ]
                       │ (ToSimDefinition)
                       ▼
    [ UnitDefinition Struct (Nova.Simulation) ]
```

---

## 2. Kernkomponenten

### 2.1 Sub-Registries (`Nova.Data.Registries`)
* Kategorieweise getrennte ScriptableObject-Registries (`UnitRegistrySO`, `BuildingRegistrySO`, `WeaponRegistrySO`).
* Verhindert Git-Merge-Konflikte, da Änderungen an Einheitendefinitionen nur `UnitRegistry.asset` berühren.

### 2.2 Master-Index (`GameDatabaseMasterSO`)
* Aggregiert die 8 Kategorie-Registries zu einer zentralen Lookup-Quelle für das Match-Setup.
* Beinhaltet die Validierungsmethode `ValidateMasterIndex(out string error)`, die doppelte IDs und Null-Referenzen im Editor blockiert.

### 2.3 Editor-Generator (`GameDatabaseGenerator`)
* Editor-Tool (`Tools -> Project Nova -> Rebuild Game Database Master Index`), das automatisch alle `UnitDefinitionSO`-Assets scannt, die Sub-Registries aktualisiert und den Master-Index baut.

### 2.4 Simulation Conversion (`Nova.Simulation.Definitions.UnitDefinition`)
* Konvertiert ScriptableObject-Daten beim Match-Setup in immutabele, allokationsfreie C#-Structs für den `SimulationKernel` (Durchsetzung der `noEngineReferences`-Prämisse).

---

## 3. Qualitätssicherung & Tests

* **Unit Tests:** [`GameDatabaseTests.cs`](../../../Assets/Tests/EditMode/Data/GameDatabaseTests.cs) (Validierung der ID-Suche, Null-Handling und Struct-Konvertierung).
