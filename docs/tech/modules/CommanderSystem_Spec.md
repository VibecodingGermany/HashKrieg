# Modulspezifikation – Commander- & Doktrinen-System (`Nova.Simulation.Commanders`)

**Version:** 1.1.0 | **Status:** historischer Prototyp-/Scaffolding-Stand gemäß D-055 – nicht verbindlich | **Verantwortungsbereich:** Game Designer / Lead Technical Director | **Sprint:** Phase 2 (Modul 17)

## Zweck

Dieses Dokument beschreibt das deterministische **Commander- & Doktrinen-System** von *Hashkrieg*. Das Modul verwaltet den passiven Aufbau von Commander-Energie, führt Cooldown-Timer für aktive Fähigkeiten (`CommanderAbilityDefinition`) aus und wendet Bereichs-Effekte (z. B. Orbital-Schläge oder Schild-Boosts) auf Einheiten an.


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
* **Energie-Generierung:** +1 Commander Energy alle **20 Ticks** (1,0 Sekunde).
* **Allokationsfreiheit:** 0 GC Bytes bei Aktivierung von Fähigkeiten.

```text
[ Player UI / Commander Ability Command ]
                 │
                 ▼
        [ CommanderSystem ] ──► Validate Energy Cost & Cooldown Ticks
                 │
                 ├── Deduct Commander Energy
                 ├── Set Cooldown Timer
                 └── Apply Area Effect ──► Damage Enemy Units in Radius (EntityManager)
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`CommanderSystemTests.cs`](../../../Assets/Tests/EditMode/Simulation/CommanderSystemTests.cs) (Validierung der Energie-Abbuchung, Cooldowns und Treffer-Berechnungen).

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
