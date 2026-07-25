# Modulspezifikation – RTS-UI & Command-Card (`Nova.Presentation.UI`)

**Version:** 1.1.1 | **Status:** historischer Prototyp-/Scaffolding-Stand gemäß D-055 – nicht verbindlich | **Verantwortungsbereich:** UI/UX Architect / Lead Technical Director | **Sprint:** Phase 1 (Modul 14)

## Zweck

Dieses Dokument beschreibt die Präsentations-Schicht der **RTS-UI & Command-Card** von *Project Nova*. Das Modul verwaltet Einheiten-Auswahlen (Einzelklick & Rechtecks-Drag-Box), verbindet ausgewählte Einheiten mit dynamischen Command-Card-HUD-Buttons (`Move`, `Stop`, `Attack`) und berechnet Koordinaten-Transformationen für das Minimap-Rendering.


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

* **Assembly:** `Nova.Presentation.UI.dll` (`noEngineReferences: false`)
* **Komponenten:**
  1. `SelectionManager`: Rechtecks-Kollisionsprüfungen für Einheiten-Mehrfachauswahl (`MaxSelectedEntities = 64`).
  2. `CommandCardPresenter`: Mapping der Auswahlzustände auf HUD-Buttons.
  3. `MinimapRenderer`: Transformation von 2D-Simulationskoordinaten auf UI-Minimap-Pixel.

```text
[ Player Mouse Input / Selection Box ]
                 │
                 ▼
        [ SelectionManager ] ──► Read active units from EntityManager
                 │
                 ├── Selected Entities Array
                 ▼
     [ CommandCardPresenter ] ──► Enable Move / Stop / Attack Buttons
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`SelectionManagerTests.cs`](../../../Assets/Tests/EditMode/Gameplay/SelectionManagerTests.cs) (Rechtecks-Kollision, Command-Card Flag-Auswertung und Minimap-Skalierung).

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
| 1.1.1 | 2026-07-25 | Testpfad korrigiert: `SelectionManager`/`CommandCardPresenter`/`MinimapRenderer` liegen nach der G0-B-Assembly-Bereinigung in `Nova.Gameplay`, der Test in `Nova.Gameplay.Tests` | Lead Technical Director |
