# Modulspezifikation – 3. Fraktion: Die Evolvierten (`Nova.Simulation.Factions`)

**Version:** 1.1.0 | **Status:** historischer Prototyp-/Scaffolding-Stand gemäß D-055 – nicht verbindlich | **Verantwortungsbereich:** Faction Designer / Lead Technical Director | **Sprint:** Phase 2 (Modul 16)

## Zweck

Dieses Dokument beschreibt die organischen Fraktions-Mechaniken der **3. Fraktion "Die Evolvierten"** von *Project Nova*. Das Modul verwaltet die Biomasse-Verbreitung (`BiomassGrid`) und gewährt Einheiten auf Biomasse-Zellen eine passive Lebenspunkte-Regeneration (+2 HP alle 0,5 Sekunden).


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
* **Regenerations-Intervall:** Exakt alle **10 Ticks** (0,5 Sekunden).
* **Allokationsfreiheit:** 0 GC Bytes in der Regenerationsschleife.

```text
[ BiomassGrid ] ──► Track Organic Cells (Biomass Coverage)
                         │
                         ▼
             [ EvolvedFactionSystem ] (Regeneration Tick % 10 == 0)
                         │
                         └── Unit on Biomass? ──► Heal +2 HP (EntityManager)
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`EvolvedFactionTests.cs`](../../../Assets/Tests/EditMode/Simulation/EvolvedFactionTests.cs) (Validierung der Biomasse-Raster-Abdeckung und der passiven Regeneration).

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
