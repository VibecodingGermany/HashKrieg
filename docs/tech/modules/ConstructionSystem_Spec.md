# Modulspezifikation – Construction & Building System (`Nova.Simulation.Construction`)

**Version:** 1.1.0 | **Status:** historischer Prototyp-/Scaffolding-Stand gemäß D-055 – nicht verbindlich | **Verantwortungsbereich:** Lead Technical Director / Sim Engine Architect | **Sprint:** Phase 1 (Modul 10)

## Zweck

Dieses Dokument beschreibt das deterministische **Construction & Building System** von *Hashkrieg*. Das Modul validiert Bauplatz-Belegungen auf einem diskreten 2D-Raster (`ConstructionGrid`), führt Bauzeit-Timer aus und registriert fertige Gebäude im Energienetzwerk (`EnergyGridSystem`).


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

* **Assembly:** `Nova.Simulation.dll` (`noEngineReferences: true`)
* **Speichermodell:** Fixed-Size `BuildingSiteState[128]` (0 GC Allokationen).

```text
[ Player UI / Construction Command ]
                 │
                 ▼
    [ ConstructionSystem ] ──► ConstructionGrid (Cell Occupancy Validation)
                 │
                 ├── Progress Construction Timers (BuildTimeTicks)
                 └── Completion ──► EnergyGridSystem.RegisterPowerProduction / Consumption
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`ConstructionSystemTests.cs`](../../../Assets/Tests/EditMode/Simulation/ConstructionSystemTests.cs) (Rasterbelegung, Abbuchung von Guthaben, Bauzeit-Fortschritt und Netzwerkintegration).

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
