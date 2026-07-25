# Modulspezifikation – Fog of War & Sichtweiten-Grid (`Nova.Simulation.Vision`)

**Version:** 1.0.0 | **Status:** Freigegeben (Phase 1 / Modul 12) | **Verantwortungsbereich:** Lead Technical Director / Sim Engine Architect | **Sprint:** Phase 1 (Modul 12)

## Zweck

Dieses Dokument beschreibt das deterministische **Fog of War & Sichtweiten-System** von *Project Nova*. Das Modul verwaltet pro Spieler ein 2D-Sichtraster (`VisionGrid`) mit drei diskreten Zuständen (`Unexplored`, `Explored`, `Visible`) und aktualisiert Sichtweiten-Radien um Einheiten und Gebäude.

---

## 1. Modul-Architektur

* **Assembly:** `Nova.Simulation.dll` (`noEngineReferences: true`)
* **Sicht-Zustände:**
  1. `Unexplored` (0): Schwarz, Gelände und Einheiten komplett unbekannt.
  2. `Explored` (1): Schattiert, Gelände bekannt, gegnerische Einheiten verborgen.
  3. `Visible` (2): Hell, volle Sichtlinie (Line-of-Sight).

```text
[ EntityManager ] ──► Unit Position (PlayerId, Transform2D)
                             │
                             ▼
                    [ VisionSystem ] (Aktualisierung alle 4 Ticks / 0,2s)
                             │
                             ├── DemoteVisibleToExplored()
                             └── RevealCircle(PlayerId, Center, Radius) ──► VisionGrid
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`VisionSystemTests.cs`](../../../Assets/Tests/EditMode/Simulation/VisionSystemTests.cs) (Validierung der Zustandsübergänge `Unexplored` -> `Visible` -> `Explored` und Sichtradien-Abdeckung).
