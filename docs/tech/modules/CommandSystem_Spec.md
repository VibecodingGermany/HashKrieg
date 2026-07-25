# Modulspezifikation – Command Bus & Order System (`Nova.Simulation.Commands`)

**Version:** 1.0.0 | **Status:** Freigegeben (Sprint 7) | **Verantwortungsbereich:** Sim Engine Architect | **Sprint:** 7

## Zweck

Dieses Dokument beschreibt das **Command Bus & Order System** von *Project Nova*. Befehle von Spielern oder KI-Agenten werden in unboxed `CommandEnvelope`-Structs verpackt und im Lockstep-Tick-Loop deterministisch verarbeitet.

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

* **Unit Tests:** [`CommandSystemTests.cs`](../../../Assets/Tests/EditMode/Simulation/CommandSystemTests.cs) (Validierung der Befehlseinreihung und deterministischen Zielaktualisierung).
