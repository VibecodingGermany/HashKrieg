# Performance-Budget

**Version:** 1.2.0 | **Status:** verbindlich für MS-1 – G0-A aktiv, Autorisierung gesperrt | **Verantwortungsbereich:** Lead Performance Engineer | **Sprint:** 7

## Zweck

Definiert Messmethode und harte MS-1-Schwellen. Es trennt den vollständigen
100-Einheiten-Produktfall von der synthetischen 500-Agenten-
Architekturreserve.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-052,
  D-058, D-061, D-063 und D-064
- [../production/MVPRecoveryPlan.md](../production/MVPRecoveryPlan.md)
- [MemoryBudget.md](MemoryBudget.md), [Pathfinding.md](Pathfinding.md) und
  [FogOfWar.md](FogOfWar.md)
- [`../../quality/scenarios/mvp-v1.json`](../../quality/scenarios/mvp-v1.json)

## 1. Workload-Grenze

| Workload | Bedeutung |
|---|---|
| `MVP_FULL_100` | vollständiger MS-1-Inhalt; Produkt-/G5-Akzeptanz |
| `SCALE_500_RENDERING` | synthetischer V2-URP-Renderingpfad |
| `SCALE_500_ANIMATION` | synthetischer V3-Animationspfad |
| `SCALE_500_PRECOMBAT` | synthetischer V4/V5a-Architekturspike |
| `SCALE_500_FULL_DIAGNOSTIC` | realer Combat/AI in G3; Diagnose |

100 Produktionseinheiten sind der MS-1-Deckel. 500 Agenten versprechen weder
spielbaren Content noch einen späteren Spieler-Slot-Scope.

## 2. Windows-x64-Referenzmethode

D-052-Referenz: Ryzen 5 5600, RTX 3060, 16 GB, NVMe.

Jede `MVP_FULL_100`-Messung verwendet:

- Windows x64 Standalone;
- IL2CPP Development Build;
- Managed-Pfad, Burst aus;
- 2560×1440 und Profil `NovaReference`;
- VSync aus und Deep Profiling aus;
- festes Replay und identischen Fingerprint;
- 30 Sekunden Warmup;
- drei getrennte Messläufe zu je 120 Sekunden;
- mindestens eine nichtnegative Rohprobe pro Sekunde und exakte Einheit aus
  dem Szenariovertrag;
- keine Ausreißerentfernung und
- alle Rohsamples als gehashtes Artefakt.

P95/P99 sowie Minimum, Maximum und Gleichheit werden je Wiederholung und über
die unveränderte Konkatenation geprüft. Ein einzelner Schwellenbruch ist
Fail; Läufe werden nicht selektiv entfernt oder vorab gepoolt.

Schema 1.3 führt diese Konfiguration als eigenes Windows-x64-Methodenprofil.
Der Start-Command und jede zugehörige Performance-Messung referenzieren
dieselbe `environmentId`.

## 3. MVP_FULL_100-Schwellen

| Metrik | P95 | P99 |
|---|---:|---:|
| Sim gesamt | ≤8,0 ms | ≤12,0 ms |
| Pathfinding | ≤4,0 ms | ≤6,0 ms |
| Fog of War | ≤1,0 ms | ≤1,5 ms |
| Rest-Sim | ≤3,0 ms | ≤4,5 ms |
| CPU-Frame | ≤16,6 ms | ≤24,9 ms |
| GPU-Frame | ≤16,6 ms | ≤24,9 ms |

Zusätzliche P95-Deckel:

- Rendering CPU ≤4,0 ms,
- Animation ≤1,5 ms,
- GPU Render ≤8,0 ms,
- UI ≤1,0 ms.

Simulations-GC ist 0 B pro Tick. „Rest-Sim“ umfasst insbesondere Combat,
Projectiles, Economy, Aetherium, Construction, Production, Technology und
Command-Verarbeitung, aber nicht Path/FoW.

## 4. 500-Agenten-Vertrag

### Pre-Combat V4/V5a

- Pathfinding P95 ≤4,0 ms,
- Pre-Combat-Rest-Sim P95 ≤3,0 ms,
- kein Crash,
- kein unbeschränktes Speicher-/Queue-Wachstum.

Der Lauf enthält repräsentative SpatialHash-, committed FoW-Filter- und
Command-Verarbeitung. V5a ist G2-Eintrittsvoraussetzung.

### Full Diagnostic V5b

Realer Combat und reale KI werden in G3 mit 500 Agenten wiederholt. Pflicht
sind kein Crash, kein unbeschränktes Wachstum und vollständige Rohwerte.
Full-Content-500 besitzt **keine** Produkt-FPS-/Full-Sim-Akzeptanzschwelle.

## 5. MS-0-Spikes

| ID | Schwelle |
|---|---|
| V1 | 10.000 Ticks, exakte Win-x64-/Mac-arm64-Hashes und finale Bytes |
| V2 | Rendering-CPU P95 ≤4 ms |
| V3 | Animation P95 ≤1,5 ms |
| V4 | Path P95 ≤4 ms |
| V5a | Pre-Combat-Rest P95 ≤3 ms |

V1 ist ein exakter Vergleich. Numerische Toleranzwerte gelten nur für
nichtautoritative Diagnostik und dürfen keinen Hash-/Bytevergleich ersetzen.

## 6. Mac-M2-Funktionsmethode

Apple M2, 1920×1080, Medium:

| Metrik | P95 | P99 |
|---|---:|---:|
| Frame | ≤33,3 ms | ≤50,0 ms |

Dies ist eine funktionale Baseline, kein Ersatz für den Windows-
Referenzbenchmark oder den Cross-Plattform-Determinismuslauf.

Das separate Mac-M2-Methodenprofil bindet und vergleicht exakt: macOS-Version,
arm64-Architektur, Apple-M2-Hardwarekonfiguration, Standalone-Build,
Managed-Pfad bei deaktiviertem Burst, 1920×1080, Quality-Profil `Medium`,
VSync, Deep Profiling und festes Replay. Command und Messung verwenden auch
hier dieselbe `environmentId`. Windows- und Mac-Messungen dürfen nicht auf ein
gemeinsames Methodenprofil verweisen.

## 7. Messintegrität

Evidence bindet:

- Subject- und Tree-SHA, `dirty=false`,
- Engine/Revision, .NET, Packages und Hardware,
- Content-/Scenario-/Evidence-Schema-/Validator-Hashes aus Subject-Blobs,
- kanonische kriterienspezifische Build-/Startchecks,
- Rohsamples und Profilerartefakte mit Hashes,
- Checkanzahl und Urteil.

In der Schema-1.2-Integritätsvorstufe bindet jede Performance-Metrik
`methodRef=performanceMethod`, einen 30-s-Warmup und exakt drei
120-s-Runs. Flache Einzelproben, falsche Units, negative Werte oder nur
kombiniert grüne Perzentile sind ungültig.

Unter dem Schema-1.3-Ziel bindet die Messung zusätzlich `environmentId` und
verwendet den plattformspezifischen `methodRef`. Schema 1.2 kann diese
Umgebungsautorität nicht erzeugen und autorisiert keinen Pass.

Fehlender, abgebrochener oder übersprungener Messlauf ist Fail. Nach relevanter
Code-, Definition-, Szenario- oder Toolchainänderung ist die Messung stale.

## 8. Skalierungsdiagnostik

Jeder 500-Agenten-Lauf berichtet zusätzlich:

- Entity-/Projectile-/Queue-Hochwasserstände,
- Flow-Cache-Hits/Fills/Evictions und RefCounts,
- Path-/FoW-/Rest-Verteilung,
- Allocations/GC und
- Speichertrend über Warmup und Messfenster.

Diese Diagnostik darf harte V4/V5a-Schwellen nicht relativieren.

## Offene Punkte

- Keine Schwellenkalibrierung vor realen Messungen. Eine Änderung der hier
  definierten Gates braucht eine neue D-ID.

## Nächste Schritte

1. Zuerst G0-A Trusted-Gate-Bootstrap ohne Gate-Fortschritt herstellen.
2. Instrumentierung, Raw-Sample-Export und getrennte Umgebungsprofile in
   G0-B/G1 schaffen.
3. V4/V5a vor G2 und `MVP_FULL_100` in G4/G5 am jeweils geforderten
   eingefrorenen SHA ausführen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Performance Engineer |
| 0.1.1 | 2026-07-21 | Sim-Tick-Gesamtbudget auf ≤8 ms angehoben mit Unterbudgets (D-042.1, führend Architecture.md); offener Punkt „Budget-Spannung Sim-Tick" als entschieden markiert | Lead Technical Director |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings) | Lead Performance Engineer |
| 1.0.0 | 2026-07-24 | D-061-Messmethode, P95/P99-Schwellen und klare 100-/500-Workload-Trennung festgelegt | Lead Performance Engineer |
| 1.0.1 | 2026-07-24 | Fehlende kanonische V2-/V3-Szenario-IDs in die Workload-Matrix aufgenommen | Lead Performance Engineer |
| 1.1.0 | 2026-07-24 | D-063-Drei-Lauf-Methode, exakte Units, nichtnegative Samples und Per-Run-Schwellenprüfung ergänzt | Lead Performance Engineer |
| 1.2.0 | 2026-07-24 | D-064-`environmentId`-Bindung sowie getrennte Windows-x64-Referenz- und Mac-M2-Funktionsmethoden als Schema-1.3-Ziel ergänzt | Lead Performance Engineer |
