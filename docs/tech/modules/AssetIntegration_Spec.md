# Modulspezifikation – Asset-Integration MS-1 (`Nova.Data`)

**Version:** 1.1.0 | **Status:** historischer Prototyp-/Scaffolding-Stand gemäß D-055 – nicht verbindlich | **Verantwortungsbereich:** Lead Technical Director / Asset Pipeline Lead | **Sprint:** Phase 1 (Modul 15)

## Zweck

Dieses Dokument beschreibt das **Asset-Integrations-System** von *Project Nova*. Das Modul verknüpft deterministische Simulations-Definitionen (`DefinitionId`) mit visuellen 3D-Modell-Assets (CC0-Asset-Bibliotheken Kenney/Quaternius aus Sprint 5 Asset Audit) für 27 Einheiten- und 24 Gebäudetypen.


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
