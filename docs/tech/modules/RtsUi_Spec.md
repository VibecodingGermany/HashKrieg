# Modulspezifikation – RTS-UI & Command-Card (`Nova.Presentation.UI`)

**Version:** 1.0.0 | **Status:** Freigegeben (Phase 1 / Modul 14) | **Verantwortungsbereich:** UI/UX Architect / Lead Technical Director | **Sprint:** Phase 1 (Modul 14)

## Zweck

Dieses Dokument beschreibt die Präsentations-Schicht der **RTS-UI & Command-Card** von *Project Nova*. Das Modul verwaltet Einheiten-Auswahlen (Einzelklick & Rechtecks-Drag-Box), verbindet ausgewählte Einheiten mit dynamischen Command-Card-HUD-Buttons (`Move`, `Stop`, `Attack`) und berechnet Koordinaten-Transformationen für das Minimap-Rendering.

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

* **Unit Tests:** [`SelectionManagerTests.cs`](../../../Assets/Tests/EditMode/Presentation/SelectionManagerTests.cs) (Rechtecks-Kollision, Command-Card Flag-Auswertung und Minimap-Skalierung).
