# Pathfinding

**Version:** 1.0.0 | **Status:** verbindlich für MS-1 – G0 aktiv | **Verantwortungsbereich:** Lead AI Programmer | **Sprint:** 7

## Zweck

Definiert deterministisches Boden-Pathfinding und Movement für Glutrinne,
einschließlich Cachevertrag und V4/V5a-Schwellen.

## Abhängigkeiten

- [SimulationCore.md](SimulationCore.md) und [GameState.md](GameState.md)
- [Commands.md](Commands.md) – Move-/Order-Eingang
- [FogOfWar.md](FogOfWar.md) – Tickordnung Movement→FoW
- [MemoryBudget.md](MemoryBudget.md) und
  [PerformanceBudget.md](PerformanceBudget.md)
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-034,
  D-057/D-058/D-061

## 1. MS-1-Scope

- 128×128 uniformes Grid, 1 m/Zelle;
- ausschließlich Bodeneinheiten;
- Gruppen-Flow-Fields für gemeinsame Zielcluster;
- deterministische lokale Separation;
- statische Glutrinne-Hindernisse sowie Gebäudefootprints;
- keine Luft, Mauern, Brücken, Wetter/Hazards, Höhenlogik oder ORCA.

Aetherium-Terrainfolge darf Kosten/Zulässigkeit nur über den kanonischen
Grid-State ändern. Sonstige Umgebungszerstörung ist deaktiviert.

## 2. Numerik und Ordnung

Alle Positionen, Geschwindigkeiten und Kosten verwenden `SimFixed`/Integer.
Welt→Grid ist floor. Neighbor-Reihenfolge, Heap-/Queue-Tie-Breaks,
Entityiteration und Zielcluster-IDs sind fest definiert. Unity NavMesh,
Physics und autoritative Unity-Mathematik sind verboten.

Movement wird nach Production/Construction und vor FoW ausgeführt. Combat liest
die danach committed Sicht.

## 3. Flow-Field-Key

Ein kanonischer Key enthält:

- Zielcluster-ID,
- Movement-/Footprint-Klasse,
- Grid-/Cost-Revision und
- Definitions-/Mapbindung.

Harvester teilen Flow Fields pro Feldziel und pro Refinery-Cluster; sie
erzeugen keine individuellen Felder. Kampfgruppen teilen ein Feld, solange
Key und Zielcluster identisch sind.

## 4. Cachevertrag

Hardcaps: 32 Einträge und 8 MiB.

- `RefCount > 0`: nie eviktieren.
- Kandidaten: nur `RefCount == 0`.
- Auswahl: deterministische LRU, Tie-Break nach Key.
- Kein Kandidat: deterministische Backpressure.
- zukunftsrelevante Request-/Build-/RefCount-/LRU-/Eviction-Metadaten sind
  Snapshot-State.

Ein aus Snapshot neu aufgebautes Feld muss dieselben Kosten-/Richtungsbytes
liefern, bevor die Simulation fortgesetzt wird.

## 5. Spatial Hash

Movement pflegt eine deterministische SpatialHash-Ansicht für Separation und
V5a. Combat darf später dieselbe committed räumliche Indizierung lesen, aber
nicht in den Movement-State zurückschreiben. Bucket- und Entityreihenfolge sind
stabil; Full-Scan O(n²) ist im Scale-Fixture unzulässig.

## 6. Orders und Fehler

Move-Intents werden über [Commands.md](Commands.md) gebunden. Ungültige
Zielzellen sind strukturell oder zustandsabhängig nach Commandvertrag
abzulehnen. Ein unerreichbares Ziel erzeugt ein deterministisches Resultat und
keine improvisierte Unity-Navigation.

Bei Kapazitäts-/Cache-Backpressure bleiben Order und Ergebnis
reproduzierbar. Kein dynamischer Container wächst über sein Hardcap.

## 7. Validierung

### Korrektheit

- Golden-Felder für bekannte Grids;
- gleiche Ergebnisse bei umgeordneter Eingabe;
- stable Tie-Breaks und negative Weltkoordinaten;
- Gebäude-/Aetherium-Revision invalidiert exakt betroffene Keys;
- Snapshot/Restore mit pending Field Builds;
- Harvester-Cluster-Sharing.

### Performance

`SCALE_500_PRECOMBAT`:

- Pathfinding P95 ≤4 ms,
- kein Crash,
- kein unbeschränktes Wachstum,
- vollständige Hits/Fills/Evictions/RefCount-/Queue-Rohwerte.

Im `MVP_FULL_100`: Path P95≤4 ms und P99≤6 ms.

## 8. Auslieferung

Der Managed-Pfad ist verbindlich. Burst ist in MS-1 deaktiviert und darf
weder Budget noch Korrektheit retten. Eine Aktivierung verlangt eine neue D-ID
und exakte Feld-/Hash-/Byteparität.

## Offene Punkte

- ORCA, Luftnavigation, größere Karten und dynamische Brücken sind Post-MVP.

## Nächste Schritte

1. Fixed-Point-Golden-Felder in G1 erstellen.
2. V4/V5a inklusive Cache-/SpatialHash-Metriken vor G2 ausführen.
3. G2 Glutrinne-/Aetherium-Gridintegration nachweisen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead AI Programmer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings) | Lead AI Programmer |
| 1.0.0 | 2026-07-24 | Pathfinding auf 128²-MS-1, Q16.16, deterministischen 32-/8-MiB-Cache und getrennte 100-/500-Gates rebaselined | Lead AI Programmer |
