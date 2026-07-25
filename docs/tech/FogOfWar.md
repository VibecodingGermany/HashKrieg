# Fog of War – autoritativer Sichtvertrag

**Version:** 1.0.0 | **Status:** verbindlich für MS-1 – G0 aktiv | **Verantwortungsbereich:** Lead Technical Director / Lead Gameplay Engineer | **Sprint:** 7

## Zweck

Definiert die einzige autoritative Sicht für Combat, KI, Player-Snapshots und
Rendering. Das Dokument schließt den früheren toten Tech-Verweis und macht die
Sichtreihenfolge, Kapazität und Datenschutztests implementierbar.

## Abhängigkeiten

- [SimulationCore.md](SimulationCore.md) – Tickordnung und State
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-058 und D-061
- [../gamedesign/FogOfWar.md](../gamedesign/FogOfWar.md) – Vollspiel-Zielbild
- [AIArchitecture.md](AIArchitecture.md) und [Testing.md](Testing.md)

## 1. Datenmodell

FoW wird auf dem 128 × 128 großen 1-m-Grid **pro Team** gespeichert. Jede Zelle
besitzt genau einen Zustand:

1. `Unexplored`,
2. `Explored`,
3. `Visible`.

Beim Recompute werden zuvor sichtbare, aber nicht mehr aufgedeckte Zellen zu
`Explored`; `Unexplored` wird nur durch tatsächliche Sicht verlassen. Die
kanonische Teammaske ist Teil des autoritativen Zustands und des State-Hashes.

## 2. Tickordnung

Der Recompute erfolgt auf jedem zweiten Simulationstick, also mit 5 Hz:

`Movement → FoW Recompute/Commit → Combat → Player Snapshot`

Zwischen zwei Recomputes bleibt die zuletzt festgeschriebene Sicht gültig. Ein
System darf weder eine vorläufige noch eine selbst berechnete Sicht verwenden.
Dadurch entscheidet dieselbe Maske:

- ob Combat ein Ziel legal erfassen oder weiterführen darf,
- welche Weltinformationen die KI erhält,
- welche Entities der menschliche Player-Snapshot enthält und
- was Rendering und Minimap anzeigen.

## 3. MS-1-Sichtmodell

MS-1 verwendet ausschließlich Radien. Nicht enthalten sind:

- Sichtblocker,
- Höhenvorteile,
- Wetter oder Hazards,
- Tarnung und Detektion sowie
- per-Einheit abweichende Spezial-Sichtlogik außerhalb definierter Radien.

Das Radar aktiviert Minimap und sichtbasierte Signatur-Pings. Ein Ping ist kein
Ziel und verleiht keine Targeting-Berechtigung. Ohne `Visible` darf Combat das
gepingte Objekt nicht adressieren.

## 4. Gefilterte Ansichten

`TeamWorldView` wird ausschließlich aus der committed Teammaske erzeugt:

- sichtbare Gegner enthalten die freigegebenen MS-1-Kampfdaten;
- erkundete Zellen enthalten Terrain- und letzten bekannten, ausdrücklich
  erlaubten Präsentationszustand;
- verborgene Entities, Orders, Ressourcen, Produktionsqueues, Ziele und
  Aetherium-Managementdaten fehlen vollständig;
- eigene und verbündete Entities bleiben sichtbar.

KI und Spieler konsumieren denselben Filtertyp. Die KI darf keine direkte
Referenz auf Entity Store, Combat, Economy oder FoW-Interna erhalten.

## 5. Determinismus und Speicher

Radien werden mit ganzzahligem Abstandstest in stabiler Entity-ID-Reihenfolge
gerastert. Team- und Zelliteration sind fest definiert. Temporäre Bitsets dürfen
als Cache neu aufgebaut werden; committed Zustände und der nächste
Recompute-Tick sind zu serialisieren.

Das FoW-Budget auf dem Szenario `MVP_FULL_100` beträgt:

- P95 höchstens 1,0 ms,
- P99 höchstens 1,5 ms und
- 0 B Sim-GC.

## 6. Pflichtprüfungen

Neben Golden- und Roundtrip-Tests sind Hidden-World-Metamorphics verpflichtend:

1. Zwei Zustände unterscheiden sich ausschließlich in verborgenen
   Gegnerinformationen.
2. Die sichtbare Player-Ansicht, die KI-Intents und alle legalen
   Combat-Entscheidungen bleiben identisch.
3. Eine sichtbar werdende Änderung darf erst ab dem festgeschriebenen
   Recompute-Tick wirken.
4. Radar-Pings dürfen keine versteckten Ziel-IDs oder Targeting-Rechte leaken.
5. Save/Restore unmittelbar vor und nach einem FoW-Tick setzt byteidentisch fort.

## Offene Punkte

- Tarnung, Detektoren, Sichtblocker, Höhe und Wetter bleiben Post-MVP und
  benötigen vor Aktivierung einen neuen Vertrag.

## Nächste Schritte

1. FoW-State und Filter-API in G1 festschreiben.
2. Graybox-Radien in G2 über das kanonische Szenario messen.
3. Metamorphic-Suite vor KI-Integration in G3 grün halten.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-24 | Team-State, 5-Hz-Reihenfolge, Radiusmodell und Hidden-World-Tests gemäß D-058/D-061 festgelegt | Lead Technical Director / Lead Gameplay Engineer |
