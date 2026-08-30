# Modulspezifikation – Skirmish-KI (Allianz & Legion) (`Nova.AI`)

**Version:** 1.1.0 | **Status:** historischer Prototyp-/Scaffolding-Stand gemäß D-055 – nicht verbindlich | **Verantwortungsbereich:** AI Architect / Lead Technical Director | **Sprint:** Phase 1 (Modul 13)

## Zweck

Dieses Dokument beschreibt das deterministische **Skirmish-KI-System** von *Hashkrieg*. Das Modul führt eine nutzenbasierte Entscheidungsschleife für Allianz- und Legion-Fraktionen aus, bewertet den Wirtschafts- und Energieaufbau, erteilt automatische Baubefehle für Gebäude und Einheiten und formiert Angriffs-Squads.


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

* **Assembly:** `Nova.AI.dll` (`noEngineReferences: true`)
* **Entscheidungs-Intervall:** Exakt alle **20 Ticks** (1,0 Sekunde).
* **Allokationsfreiheit:** 0 GC Bytes in der Entscheidungsschleife.

```text
[ SimulationKernel ] (Tick % 20 == 0)
         │
         ▼
[ SkirmishAiSystem ] ──► Evaluate Power Margin ──► Request PowerPlant (ConstructionSystem)
         │
         ├── Evaluate Credit Balance ───────► Enqueue Unit Production (ProductionQueueSystem)
         └── Evaluate Army Size ────────────► Dispatch Attack Squad Orders (CommandProcessorSystem)
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`SkirmishAiTests.cs`](../../../Assets/Tests/EditMode/AI/SkirmishAiTests.cs) (Validierung der automatischen Bauplatz- und Produktionsentscheidungen).

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
