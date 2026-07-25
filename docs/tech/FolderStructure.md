# Ordner- und Projektstruktur

**Version:** 1.3.0 | **Status:** verbindliches G0-A1/G0-A2/G0-B-Ziel – noch nicht nachgewiesen | **Verantwortungsbereich:** Lead Technical Director / Lead DevOps Engineer | **Sprint:** 7

## Zweck

Definiert die Zielorte und Ownership-Grenzen für G0. Vorhandene Verzeichnisse
gelten bis zum Architekturcheck als Prototyp, nicht als bestandene Struktur.

## Abhängigkeiten

- [Architecture.md](Architecture.md) und
  [DependencyGraph.md](DependencyGraph.md)
- [Deployment.md](Deployment.md)
- [../production/MVPRecoveryPlan.md](../production/MVPRecoveryPlan.md)
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-043,
  D-057, D-059, D-061, D-062, D-063 und D-064

## 1. Zielbaum

```text
Assets/_Project/
├── Scripts/
│   ├── Core/             Nova.Core
│   ├── Simulation/       Nova.Simulation
│   ├── AI/               Nova.AI
│   ├── AI.Data/          AI-Authoring
│   ├── Data/             Definitions-SOs/Registries
│   ├── Gameplay/         MatchSession/Ingress/Composition
│   ├── Presentation/     World View/Camera/Rendering
│   └── UI/               HUD/Settings/Persistence UI
└── Tests/
    ├── EditMode/
    └── PlayMode/

tools/
└── Nova.SimRunner/       versioniertes .NET-Projekt

quality/
├── content/
├── scenarios/
├── schemas/              Schema 1.3 integrity-only; Receipt-Vertrag ist G0-A2-Ziel
├── scripts/              Schema-, Semantik- und fail-closed Gate-Prüfungen
├── package.json          gepinnte Quality-Toolchain
├── package-lock.json     reproduzierbarer Dependency-Lock
└── evidence/             nur reale append-only Versuche
```

`quality/evidence/` wird nicht als leeres Gerüst angelegt.
Das Trusted-Tool-Bundle wird in G0-A1 als separater Checkout geprüft. Erst
der noch offene G0-A2-Authorize-Job darf es als geschützte Autorität
verwenden; es ist kein vom geprüften Subject aufgelöster zweiter In-Tree-Pfad.

## 2. Quellenvertrag

Core/Simulation/AI werden so projektgebunden, dass Unity und SimRunner dieselben
Quelldateien und determinismusrelevanten Defines kompilieren. Unzulässig sind:

- kopierte Source-Bäume unter `tools/`,
- separate Command-/State-/Serializer-/PRNG-/Hash-Implementierungen,
- generierte Quellen ohne versionierten Generator und Golden Output,
- Unity-Abhängigkeiten im SimRunner-Transitivgraphen.

## 3. Assembly-Dateien

Jede produktive Schicht besitzt genau eine klar benannte asmdef je
Verantwortungsbereich. Tests referenzieren Produktassemblies, nicht
umgekehrt. `Nova.Simulation.Burst` darf als reservierte Assembly existieren,
ist für MS-1 aber deaktiviert und keine Produktdependency.

## 4. Definitionen

Statische Definitionsassets sind nach Kategorie geshardet. Ein deterministisch
generierter/validierter Masterindex erzeugt den kanonischen
`DefinitionSnapshot`. Runtime-State gehört nie in SOs oder Registry-Assets.

## 5. Generierter Output

Nicht tracken:

- `Library/`, `Temp/`, `Logs/`, `Build/`, `Builds/`,
- `bin/`, `obj/`, TestResults und Coverage-Ausgaben,
- Player-/Profiler-/Benchmark-Binaries,
- lokale Evidence-Entwürfe oder Dirty-Run-Artefakte.

G0 prüft getrackte Dateien und scheitert bei generierten Binaries.

## 6. G0-Nachweis

Der Architecture Check validiert:

- Verzeichnis→Assembly-Ownership,
- erlaubte Referenzen,
- Source-Parität Unity/SimRunner,
- Paket-/Define-Parität,
- keine generierten Binaries,
- Negative Control gegen eine verbotene Kante,
- ein kanonisches Prüfergebnis je G0-Kriterium einschließlich gebundener
  stdout-, stderr- und Check-Artefakte,
- nach G0-A2 ein append-only `GateAuthorization.json`-Receipt, eine
  geschützte CI-Attestierung und einen unabhängigen Reviewer-Nachweis,
- die vollständige geordnete Receipt-Kette sowie
  `environmentId`-Bindung von Command und Performance-Messung und
- getrennte Windows-x64-Referenz- und Mac-M2-Funktionsmethoden.

Schema 1.3 kann diese Struktur nur auf Integrität prüfen. Jeder Pass-Versuch
bleibt mit `E_AUTHORIZATION_BOOTSTRAP` gesperrt. G0-A1 und G0-A2 werden ohne
Gate-Fortschritt gemergt; erst ein nachfolgender sauberer Subject-Commit darf
G0-B und damit G0 nachweisen.

## Offene Punkte

- Trusted-Tool-Checkout, Schema 1.3 und der kanonische `run_gate_check.py`
  bilden G0-A1. Der zweiphasige Receipt-Pfad folgt in G0-A2. Die exakten
  `.csproj`-/Source-Include-Mechanismen folgen in G0-B und werden versioniert.

## Nächste Schritte

1. G0-A1 ohne Gate-Fortschritt mergen.
2. G0-A2 als separaten Receipt-Authorizer implementieren.
3. Vorhandene Prototypstruktur gegen diesen Zielbaum inventarisieren.
4. Kleinste G0-B-Korrektur ohne Gameplayänderung implementieren.
5. Gate-Runner, Clean Builds/Tests und Negative Controls erst am
   nachfolgenden sauberen Subject in Schema-1.3-Evidence festhalten.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Technical Director |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings): Nova.AI/Nova.AI.Data in Baum & Assembly-Matrix (D-043), GameDatabase-Sharding mit Sub-Registries + generiertem Master-Index (D-049), Managed-first/Feature-Flag-Vermerke (D-045), SimRunner lädt Nova.AI | Lead Technical Director |
| 1.0.0 | 2026-07-24 | Ordnerstruktur als G0-Ziel mit Source-Parität, Quality-Verträgen und Binary-Hygiene rebaselined | Lead Technical Director / Lead DevOps Engineer |
| 1.0.1 | 2026-07-24 | Versionierte Quality-Skripte im G0-Zielbaum ergänzt | Lead Technical Director / Lead DevOps Engineer |
| 1.1.0 | 2026-07-24 | Quality-Toolchain, kanonische Prüfartefakte und geschützte Evidence-Autorisierung nach D-063 ergänzt | Lead Technical Director / Lead DevOps Engineer |
| 1.2.0 | 2026-07-24 | D-064-Trennung von subject-unabhängigem G0-A-Trust-Bundle und nachfolgendem G0-B-Subject in der Zielstruktur verankert | Lead Technical Director / Lead DevOps Engineer |
| 1.3.0 | 2026-07-25 | D-066: Receipt-Datei und getrennte G0-A1-/G0-A2-Zielstruktur verankert | Lead Technical Director / Lead DevOps Engineer |
