# Modulspezifikation – Combat & Damage Pipeline (`Nova.Simulation.Combat`)

**Version:** 1.1.0 | **Status:** historischer Prototyp-/Scaffolding-Stand gemäß D-055 – nicht verbindlich | **Verantwortungsbereich:** Sim Engine Architect / Gameplay Developer | **Sprint:** 7

## Zweck

Dieses Dokument beschreibt die deterministische **Combat & Damage Pipeline** von *Project Nova*. Das Modul führt Entfernungs- und Cooldown-Prüfungen aus, wendet Schaden an und entfernt zerstörte Einheiten allokationsfrei aus dem `EntityManager`.


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
