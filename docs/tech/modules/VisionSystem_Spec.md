# Modulspezifikation – Fog of War & Sichtweiten-Grid (`Nova.Simulation.Vision`)

**Version:** 1.1.1 | **Status:** historischer Prototyp-/Scaffolding-Stand gemäß D-055 – nicht verbindlich | **Verantwortungsbereich:** Lead Technical Director / Sim Engine Architect | **Sprint:** Phase 1 (Modul 12)

## Zweck

Dieses Dokument beschreibt das deterministische **Fog of War & Sichtweiten-System** von *Project Nova*. Das Modul verwaltet pro Spieler ein 2D-Sichtraster (`VisionGrid`) mit drei diskreten Zuständen (`Unexplored`, `Explored`, `Visible`) und aktualisiert Sichtweiten-Radien um Einheiten und Gebäude.


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

* **Unit Tests:** [`FogOfWarSystemTests.cs`](../../../Assets/Tests/EditMode/Simulation/FogOfWarSystemTests.cs) (Validierung der Zustandsübergänge `Unexplored` -> `Visible` -> `Explored` und Sichtradien-Abdeckung; kanonische Nachfolge-Suite des mit dem Prototyp-Scaffolding ersetzten `VisionSystemTests.cs`).

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
| 1.1.1 | 2026-07-25 | Toten Test-Link auf die kanonische Nachfolge-Suite `FogOfWarSystemTests.cs` korrigiert (Prototyp-Scaffolding in G1 ersetzt) | Lead Technical Director |
