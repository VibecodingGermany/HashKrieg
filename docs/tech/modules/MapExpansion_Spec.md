# Modulspezifikation – Map- & Biom-Erweiterung (`Nova.Presentation.Maps`)

**Version:** 1.1.0 | **Status:** historischer Prototyp-/Scaffolding-Stand gemäß D-055 – nicht verbindlich | **Verantwortungsbereich:** Level Designer / Lead Technical Director | **Sprint:** Phase 2 (Modul 19)

## Zweck

Dieses Dokument beschreibt die Präsentations-Struktur der **Map- & Biom-Erweiterung** von *Project Nova*. Das Modul verwaltet Karten-Layouts (`MapDefinitionSO`) für 1v1- und 2v2-Gefechte, 2 bis 4 Spieler-Spawn-Punkte, Aetherium-Kristallknoten-Positionen sowie drei Biom-Umgebungen (`Desert`, `Snow`, `JungleIndustrial`).


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

* **Assembly:** `Nova.Presentation.Maps.dll` (`noEngineReferences: false`)
* **Biome:**
  1. `Desert` (0): Trockenes Wüsten-Biom.
  2. `Snow` (1): Verschneite Arktis-Gletscher.
  3. `JungleIndustrial` (2): Dschungel / Industriekomplex.

```text
[ MatchRunner / Game Setup ]
              │
              ▼
    [ MapDefinitionSO ] ──► Validate Map Dimensions & Spawn Points (IsValid)
              │
              ├── Spawn Base HQs ─────► ConstructionSystem
              └── Spawn Aetherium ───► ResourceHarvestingSystem
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`MapDefinitionTests.cs`](../../../Assets/Tests/EditMode/Presentation/MapDefinitionTests.cs) (Validierung von Kartenabmessungen, Spawn-Punkten und Biom-Typen).

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
