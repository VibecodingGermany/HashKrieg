# Modulspezifikation – Economy & Energy Grid System (`Nova.Simulation.Economy`)

**Version:** 1.0.0 | **Status:** Freigegeben (Sprint 7 / Phase 1) | **Verantwortungsbereich:** Lead Technical Director / Sim Engine Architect | **Sprint:** Phase 1 (Modul 9)

## Zweck

Dieses Dokument beschreibt das deterministische **Economy & Energy Grid System** von *Project Nova*. Das Modul verwaltet Aetherium-Guthaben, verarbeitet Entladungen von Sammlereinheiten und berechnet das Energie-Netzwerk. Bei Energieunterdeckung wird automatisch ein **Low-Power-Malus (-50 % Produktions- und Forschungsgeschwindigkeit)** ausgelöst.

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
