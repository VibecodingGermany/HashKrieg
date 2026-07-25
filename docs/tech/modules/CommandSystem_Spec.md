# Modulspezifikation – Command Bus & Order System (`Nova.Simulation.Commands`)

**Version:** 1.1.1 | **Status:** historischer Prototyp-/Scaffolding-Stand gemäß D-055 – nicht verbindlich | **Verantwortungsbereich:** Sim Engine Architect | **Sprint:** 7

## Zweck

Dieses Dokument beschreibt das **Command Bus & Order System** von *Project Nova*. Befehle von Spielern oder KI-Agenten werden in unboxed `CommandEnvelope`-Structs verpackt und im Lockstep-Tick-Loop deterministisch verarbeitet.


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
* **Allokationsfreiheit:** 0 GC Bytes (Ringpuffer aus `CommandEnvelope` Value-Types).

```text
[ Player UI / Network / AI ]
           │
           ├── CommandEnvelope Struct (Value Type)
           ▼
[ CommandProcessorSystem (ICommandSink) ]
           │
           └── ExecuteTick(Tick) ──► Target-Set ──► PathfindingSystem & EntityManager
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** Die Prototyp-Tests (`CommandSystemTests.cs`) wurden mit der G1-Kernel-Integration entfernt (F-001; der alte Buffer-Pfad war defekt). Der kanonische Command-Pfad wird durch [`KernelIntegrationTests.cs`](../../../Assets/Tests/EditMode/Simulation/KernelIntegrationTests.cs) abgedeckt.

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
| 1.1.1 | 2026-07-25 | Toten Prototyp-Testlink nach G1-Kernel-Integration (F-001/F-005, D-057-Reset) korrigiert | Lead Technical Director |
