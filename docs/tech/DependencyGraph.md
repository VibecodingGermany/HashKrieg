# Dependency Graph

**Version:** 1.0.0 | **Status:** verbindlich für MS-1 – G0 aktiv | **Verantwortungsbereich:** Lead Technical Director | **Sprint:** 7

## Zweck

Definiert erlaubte Assembly-Referenzen und verbotene Rückkanten. G0 prüft
diese Matrix automatisiert; eine vorhandene asmdef-Datei allein erfüllt sie
nicht.

## Abhängigkeiten

- [Architecture.md](Architecture.md)
- [ModuleOverview.md](ModuleOverview.md)
- [SimulationCore.md](SimulationCore.md)
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-043,
  D-057 und D-061

## 1. Schichten

```text
Nova.UI ───────────────┐
Nova.Presentation ─────┤
Nova.Data / AI.Data ───┤
                       v
                 Nova.Gameplay
                  /    |    \
                 v     v     v
          Nova.Simulation  Nova.AI
                 \       /
                  v     v
                  Nova.Core

Nova.SimRunner ──> Nova.Simulation / Nova.AI / Nova.Core
```

Pfeile zeigen ausschließlich erlaubte Compile-Time-Abhängigkeiten. Zwischen UI
und Presentation vermittelt Gameplay/Core über lokale Client-Verträge; weder
UI noch Presentation darf Simulation mutieren.

## 2. Referenzmatrix

| Assembly | Darf referenzieren | Darf nicht referenzieren |
|---|---|---|
| `Nova.Core` | – | Unity und alle Nova-Hochschichten |
| `Nova.Simulation` | `Nova.Core` | Unity, AI, Data, Gameplay, Presentation, UI |
| `Nova.Simulation.Burst` | Core, Simulation, Burst-Pakete | Gameplay/View/UI; für MS-1 deaktiviert |
| `Nova.AI` | Core, öffentliche Simulation-Read-/Intent-Typen | Unity, Gameplay, Data, direkte Sim-Modulinterna |
| `Nova.Data` | Core, Unity-SO-Authoring | Simulation-State |
| `Nova.AI.Data` | Core, Unity-SO-Authoring | Simulation-State |
| `Nova.Gameplay` | Core, Simulation, AI, Data, AI.Data | – als Composition Root |
| `Nova.Presentation` | Core, Unity/URP | Simulation-State-Schreibzugriff, AI |
| `Nova.UI` | Core, Unity UI | Simulation-State-Schreibzugriff, AI |
| `Nova.SimRunner` | Core, Simulation, AI | jede Unity-Assembly |

Die AI-Verbindung ist ein Clientvertrag: Simulation kennt `Nova.AI` nicht. Der
Save-Host serialisiert den AI-Sidecar getrennt vom Simulationsblock.

## 3. Erlaubte Datenflüsse

- Data SOs → Gameplay → kanonischer `DefinitionSnapshot` → Simulation
- Device/UI → `CommandIntent` → Gameplay/Ingress → `CommandBatch` → Simulation
- Simulation → committed Player-/Team-Snapshots → Gameplay → UI/Presentation/AI
- AI → `CommandIntent` → Gameplay/Ingress
- Simulation + AI-Sidecar → Persistence Host

Keine Gegenrichtung darf einen direkten State-Setter enthalten.

## 4. Verbotene Kanten

G0 muss mindestens diese Negative Controls erkennen:

1. `UnityEngine` oder `Unity.Mathematics` in Core/Simulation,
2. Simulation → AI oder Gameplay,
3. SimRunner → Unity,
4. UI/Presentation → mutable Sim-State,
5. AI → Entity Store, Economy, Combat, FoW-Interna oder Path-Interna,
6. separate Serializer-/PRNG-/Hash-Implementierung im Unity-Host,
7. produktive Abhängigkeit auf `Nova.Simulation.Burst`.

## 5. Source-Parität

SimRunner und Unity verwenden dieselben Quellprojekte oder dieselben
versionierten Source-Includes. Kopierte Verzeichnisse sind verboten. Der
G0-Bericht hält Projektversionen, Defines, Paketstände und negative
Architekturprobe fest.

## Offene Punkte

- Post-MVP-Online-Assemblies sind nicht Teil dieser Matrix.
- Burst kann nur per neuer D-ID und exakter Parität aktiviert werden.

## Nächste Schritte

1. Matrix in G0 als automatischen Architecture Check implementieren.
2. Mindestens eine verbotene Kante als Negative Control injizieren und
   zuverlässig rot nachweisen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Technical Director |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043-Topologie): kanonische Assembly-/Referenzmatrix inkl. `Nova.AI`/`Nova.AI.Data`; Offener Punkt 1 (Burst-Brücke) via D-043/D-045/D-037 gelöst, `Nova.Simulation.Jobs`-Altname entfernt | Lead Technical Director |
| 1.0.0 | 2026-07-24 | Dependency Graph auf MS-1-Hosts, AI-Sidecar, Source-Parität und G0-Negative-Controls rebaselined | Lead Technical Director |
