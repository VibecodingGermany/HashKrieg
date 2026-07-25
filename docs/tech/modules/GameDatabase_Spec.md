# Modulspezifikation – GameDatabase Sharding & Master Index (`Nova.Data` & `Nova.Editor`)

**Version:** 1.1.0 | **Status:** historischer Prototyp-/Scaffolding-Stand gemäß D-055 – nicht verbindlich | **Verantwortungsbereich:** Data Architect / Lead Technical Director | **Sprint:** 7

## Zweck

Dieses Dokument beschreibt die Architektur und das Sharding-Datenmodell des **GameDatabase Master Index** gemäß Decision D-049. Um Merge-Konflikte im Git-Repository bei der parallelen Arbeit mehrerer Entwickler und KI-Agenten zu verhindern, werden Definitionen kategorieweise in Sub-Registries aufgeteilt und über ein automatisches Editor-Tool in einem Master-Index aggregiert.


## Abhängigkeiten

- [../../production/MVPRecoveryPlan.md](../../production/MVPRecoveryPlan.md) – aktiver Gate- und Statusvertrag
- [../../production/DecisionLog.md](../../production/DecisionLog.md) – D-055 bis D-061
- [../ModuleOverview.md](../ModuleOverview.md) – aktive Modul- und State-Hoheit
- [../SimulationCore.md](../SimulationCore.md) und [../Commands.md](../Commands.md) – führende Kernverträge

> **Recovery-Hinweis:** Der folgende Text konserviert den nach D-055 nicht
> abgenommenen Prototyp-/Scaffolding-Stand. Er ist keine Implementierungsfreigabe.
> Bei jedem Konflikt führen die oben verlinkten aktiven Verträge. Eine künftige
> Freigabe erfordert das zuständige Gate, neue Laufzeitevidenz und eine
> inhaltlich rebaselinede Spezifikation.

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

## Offene Punkte

- Welche Teile dieses Prototyps nach Abgleich mit D-056 bis D-061 wiederverwendet
  werden, entscheidet erst die Implementierung im zuständigen Gate.

## Nächste Schritte

1. Bestand gegen die aktiven Kern-, Inhalts- und Gate-Verträge prüfen.
2. Widersprechende APIs und Werte nicht übernehmen.
3. Erst nach bestandener Gate-Evidenz eine neue verbindliche Revision erstellen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-24 | Historischen Prototyp-/Scaffolding-Stand dokumentiert | Modulverantwortliche |
| 1.1.0 | 2026-07-24 | Freigabe gemäß D-055 entzogen und aktive Recovery-Verträge als führend verankert | Lead Technical Director |
