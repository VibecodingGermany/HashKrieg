# Modulspezifikation – Production Queue & Tech-Tree System (`Nova.Simulation.Production`)

**Version:** 1.1.0 | **Status:** historischer Prototyp-/Scaffolding-Stand gemäß D-055 – nicht verbindlich | **Verantwortungsbereich:** Lead Technical Director / Sim Engine Architect | **Sprint:** Phase 1 (Modul 11)

## Zweck

Dieses Dokument beschreibt das deterministische **Production Queue & Tech-Tree System** von *Hashkrieg*. Das Modul führt Einheiten-Produktions-Queues in Kasernen und Fabriken aus, berechnet Produktionszeit-Timer unter Berücksichtigung von Low-Power-Mali und spawnt fertiggestellte Einheiten in den `EntityManager`. Zudem verwaltet das Modul die Tech-Tier-Stufen der Spieler.


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
* **Speichermodell:** Fixed-Size `ProductionItemState[128]` (0 GC Allokationen).

```text
[ Player UI / Train Unit Command ]
                 │
                 ▼
     [ ProductionQueueSystem ] ──► ResearchTreeSystem (Tech Tier Check)
                 │
                 ├── Deduct Aetherium Credits (EnergyGridSystem)
                 ├── Progress Production Timers (BuildTimeTicks)
                 └── Completion ──► EntityManager.SpawnUnit()
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`ProductionSystemTests.cs`](../../../Assets/Tests/EditMode/Simulation/ProductionSystemTests.cs) (Einheiten-Produktions-Queues, Guthabenabbuchung, Tech-Tier-Freischaltung und automatische Spawns).

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
