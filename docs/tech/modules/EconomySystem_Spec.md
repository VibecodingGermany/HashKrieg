# Modulspezifikation – Economy & Energy Grid System (`Nova.Simulation.Economy`)

**Version:** 1.1.0 | **Status:** historischer Prototyp-/Scaffolding-Stand gemäß D-055 – nicht verbindlich | **Verantwortungsbereich:** Lead Technical Director / Sim Engine Architect | **Sprint:** Phase 1 (Modul 9)

## Zweck

Dieses Dokument beschreibt das deterministische **Economy & Energy Grid System** von *Project Nova*. Das Modul verwaltet Aetherium-Guthaben, verarbeitet Entladungen von Sammlereinheiten und berechnet das Energie-Netzwerk. Bei Energieunterdeckung wird automatisch ein **Low-Power-Malus (-50 % Produktions- und Forschungsgeschwindigkeit)** ausgelöst.


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
* **Speichermodell:** Fixed-Size `PlayerEconomyState[8]` (16 Bytes pro Spieler, 0 GC Allokationen).

```text
[ Harvester Unit ] ──► DepositResource(amount)
                               │
                               ▼
[ ResourceHarvestingSystem ] ──► PlayerEconomyState (AetheriumCredits)
                               ▲
                               │
[ EnergyGridSystem ] ──────────┴─► PowerProduced vs. PowerConsumed ──► IsLowPower (-50% Speed)
```

---

## 2. Formeln & Regeln (GDD-Harmonisiert)

* **Low-Power Trigger:** `IsLowPower = PowerConsumed > PowerProduced`
* **Geschwindigkeits-Multiplikator:** `ProductionSpeedMultiplier = IsLowPower ? 0.5f : 1.0f`
* **Sammler-Entladerate:** Standardmäßig **50 Aetherium Credits** pro Entladevorgang an einer Raffinerie.

---

## 3. Qualitätssicherung & Tests

* **Unit Tests:** [`EconomySystemTests.cs`](../../../Assets/Tests/EditMode/Simulation/EconomySystemTests.cs) (Guthabenabbuchungen, Low-Power-Erkennung und Multiplikatorstrafen).

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
