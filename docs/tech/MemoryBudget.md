# Memory-Budget

**Version:** 1.0.0 | **Status:** verbindlich für MS-1 – G0 aktiv | **Verantwortungsbereich:** Lead Performance Engineer | **Sprint:** 7

## Zweck

Definiert feste MS-1-Kapazitäten, Snapshot-/Parsergrenzen und
Flow-Cache-Eviction. Es macht keine unbelegte Gesamt-RAM-Zusage.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-058/D-061
- [GameState.md](GameState.md) und [Serialization.md](Serialization.md)
- [Pathfinding.md](Pathfinding.md) und [FogOfWar.md](FogOfWar.md)
- [PerformanceBudget.md](PerformanceBudget.md)

## 1. Feste Kapazitäten

| Ressource | MS-1-Hardcap/Ziel |
|---|---:|
| reservierte Slots | 8 |
| aktive Slots | exakt 2 |
| Grid | 128×128 × 1 m |
| Produktionseinheiten | 100 gesamt |
| synthetisches Scale-Fixture | 500 Agenten |
| Entity Store | 1.024 |
| unkomprimierter Snapshot | Ziel ≤4 MiB |
| Parser | Hardcap 64 MiB vor Payload-Parse |
| Flow-Cache | ≤32 Einträge und ≤8 MiB |

500 Agenten sind ein Testfixture. Sie erhöhen nicht Produktionseinheitenlimit,
aktive Slots oder Contentumfang.

## 2. Autoritativer Speicher

Folgende Bereiche sind vorallokiert oder besitzen eine fingerprinted feste
Kapazität:

- Entity-Slots, Generationen und Free-List;
- Player-/Team-State für acht reservierte Slots;
- Command-/Result-/Dedupe-Puffer;
- Projectile- und Deferred-Queues;
- Grid-Layer für Movement, Aetherium und FoW;
- Production-/Construction-Queues und
- Snapshot-Writebuffer.

Eine Überschreitung vergrößert keinen Container dynamisch, sondern erzeugt
einen deterministischen, getesteten Fehler.

## 3. Flow-Field-Cache

Zwei Grenzen gelten gleichzeitig: 32 Einträge und 8 MiB. Ein Insert ist nur
zulässig, wenn beide eingehalten werden.

Eviction:

1. Einträge mit `RefCount > 0` sind geschützt.
2. Kandidaten sind ausschließlich `RefCount == 0`.
3. Kandidat ist deterministisch ältester LRU-Eintrag.
4. Gleichstand entscheidet der kanonische Cache-Key.
5. Request-, RefCount-, LRU- und Eviction-Metadaten werden gespeichert, soweit
   sie zukünftige Auswahl beeinflussen.

Ist kein Kandidat vorhanden, liefert die Request-Queue deterministische
Backpressure statt eines stillen Speicherwachstums.

## 4. Snapshot und Parser

Das 4-MiB-Ziel gilt unkomprimiert für `SimState` plus notwendige
Fortsetzungsdaten. Der 64-MiB-Hardcap schützt Parser und Migration vor
feindlichen Längen. Längen/Counts werden vor Allokation geprüft.

G1 berichtet pro Block:

- Nutzbytes,
- Capacity/High-Water-Mark,
- serialisierte Bytes und
- Hash-/Roundtrip-Ergebnis.

Ein Zielbruch >4 MiB ist ein Gatebefund, aber noch kein Parserfehler. >64 MiB
ist immer harter Parsefehler.

## 5. Laufzeitallokationen

Im autoritativen Tick gilt 0 B Managed-GC. Pools und persistente Buffer werden
vor dem Messfenster dimensioniert. Presentation-/UI-Allokationen werden separat
gemessen und dürfen keine Sim-GC-Zahl kaschieren.

## 6. Wachstumstests

V4/V5a/V5b beobachten mindestens:

- Entity-/Projectile-/Command-/Deferred-High-Water-Marks,
- Flow-Cache Bytes, Hits, Fills und Evictions,
- Snapshotgröße,
- Managed Heap/GC im Sim-Tick und
- monotones Wachstum über Warmup +120 s.

„Kein unbeschränktes Wachstum“ verlangt ein Plateau innerhalb fester
Kapazitäten, nicht nur einen ausbleibenden Out-of-Memory-Crash.

## Offene Punkte

- Ein Gesamt-RAM-Budget wird erst aus realen G0/G1-Buildmessungen abgeleitet;
  es ist kein MS-1-Gate in diesem Rebaseline.

## Nächste Schritte

1. Store-/Queue-Kapazitäten in G1 testbar machen.
2. Per-Block-Snapshotgrößen und Parsergrenzen messen.
3. Cache-/Wachstumsmetriken in V4/V5a/V5b aufnehmen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Performance Engineer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 4: Flow-Field-Deckel/Speicher/Eviction an tech/Pathfinding.md §1.1/§2 angeglichen (32→96 Felder, ≤6,5 MB→≈19 MB, LRU→RefCount), 100-MB-Sim-Kappe nachgerechnet | Lead Performance Engineer |
| 1.0.0 | 2026-07-24 | D-058-Kapazitäten, 4-/64-MiB-Grenzen und deterministische 32-/8-MiB-Cache-Eviction festgelegt | Lead Performance Engineer |
