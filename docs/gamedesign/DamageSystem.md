# Schadenssystem (Damage System)

**Version:** 0.2.0 | **Status:** Entwurf (Korrekturlauf Sprint 2) | **Verantwortungsbereich:** Lead Gameplay Designer | **Sprint:** 2

## Zweck

Definiert, wie in Hashkrieg Schaden entsteht, zugestellt und verrechnet wird: Schadensarten, Treffer-Modelle, Flächenschaden-Falloff, Status-Effekte sowie die Regeln für Friendly Fire und Deckung. Das Dokument ist die Regel-Referenz für alle Kampfsysteme; konkrete Waffenwerte stehen in [Weapons.md](Weapons.md), die Panzerungs-Verrechnung in [ArmorSystem.md](ArmorSystem.md). Alle Werte sind Startwerte **v0.1** zum Tunen (Richtwerte mit Begründung, keine Pseudo-Präzision) und müssen als flache Datensätze (ScriptableObject-tauglich) abbildbar sein.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-010 (Wirtschaft/Matchdauer), D-012 (gezielte Zerstörbarkeit), D-014 (Drohnen), D-015 (Elite-Einheiten), D-017 (Wetter/Hazards), D-019 (Kamera/Lesbarkeit), D-027 (Fraktions-Sonderregeln: EMP, Evolvierte-Immunität)
- [Weapons.md](Weapons.md) – Waffenkatalog (weist jeder Waffe Schadensart und Treffer-Modell zu)
- [ArmorSystem.md](ArmorSystem.md) – Schaden-gegen-Panzerung-Matrix, Regeneration/Reparatur
- [Factions.md](Factions.md) – Fraktionsidentitäten (Damage-Fantasy je Fraktion)
- [Biomes.md](Biomes.md) – Hazards (Strahlungsfronten) als Schadensquelle
- [../analysis/KnowledgeBase.md](../analysis/KnowledgeBase.md) – TPD §8.6 Kampfsystem

## Schadensarten

Sechs Schadensarten bilden den kompletten Schadensraum ab. Jede Waffe hat genau **eine** primäre Schadensart; Status-Effekte sind davon getrennt modelliert (siehe unten). Verrechnung über die Matrix in [ArmorSystem.md](ArmorSystem.md).

| ID | Schadensart | Design-Identität | Typische Quellen |
|---|---|---|---|
| `Kinetic` | Kinetisch | Anti-Infanterie, Flak; Dauerfeuer, präzise | Gewehre, MG, Kanonen, Flak |
| `Energy` | Energie | Ausgewogen, Allianz-High-Tech; gut gegen Luft/Leichtes | Laser, Ionenwaffen, Plasmablitze |
| `Explosive` | Explosiv | Anti-Fahrzeug, Anti-Gebäude; Flächenschaden | Raketen, Artillerie, Granaten, Bomben |
| `Fire` | Feuer/Thermobarisch | Anti-Infanterie, Fläche über Zeit, zündet Vegetation an (D-012) | Flammenwerfer, Brandgeschosse, Thermobarik |
| `Bio` | Bio/Säure | Anti-Infanterie, kontert Regeneration (Evolvierte-Spiegel) | Sporen, Säure, Toxine |
| `Radiation` | Strahlung | Umwelt-/Hazard-Schaden, fraktionsneutral | Aetherium-Verstrahlung, Strahlungsfronten (D-017), Kristallsturm |

Begründung: Die sechs Arten decken die drei Fraktions-Fantasien ab (Allianz = Energie/Kinetisch, Legion = Explosiv/Feuer, Evolvierte = Bio/Kinetisch-Nahkampf) plus eine neutrale Umwelt-Kategorie (Strahlung), die Hazards und den Aetherium-Strahlungshintergrund (GDD-O) spielbar macht. Mehr Arten würden die Matrix unlesbar aufblähen.

## Treffer-Modell

Vier Treffer-Modelle, pro Waffe genau eines:

| ID | Modell | Regel |
|---|---|---|
| `Projectile` | Projektil | Physikalisch simuliertes/sichtbares Geschoss mit Flugzeit; trifft die Position zum Einschlag. Schnelle Projektile (Kanonen) sind faktisch zielsicher, langsame können von bewegten Zielen verfehlt werden. |
| `Hitscan` | Hitscan | Soforttreffer ohne Flugzeit (MG, Laser, Scharfschütze). |
| `Missile` | Rakete | Lenkflugkörper, verfolgt das Ziel, kann durch Punktverteidigung/Gegenmaßnahmen abgefangen werden (Design-Haken für spätere Upgrades); trifft auch Luftziele. |
| `Area` | Fläche | Wirkung am Zielpunkt statt am Zielobjekt (Artillerie, Mörser, Flammen, Superwaffen); Schaden nach Falloff-Regel. |

**Treffsicherheits-Grundregel:** Es gibt **keine Zufalls-Trefferwürfe**. Eine Waffe trifft, wenn das Ziel in Reichweite und Sicht ist; "Ausweichen" entsteht ausschließlich durch Bewegung außerhalb der Projektil-/Feuerlösung. Begründung: deterministische Lesbarkeit (D-019), KI- und Replay-Freundlichkeit, balancierbar ohne Würfelrauschen; entspricht dem C&C-Genre-Standard der H1-Zielgruppe.

**Streuung (einzige Ausnahme):** Artillerie-/Flächenwaffen erhalten einen festen Streuradius um den Zielpunkt (datengetrieben, Richtwert 1–3 Felder je nach Reichweite) – das ist Positions-, kein Treffer-Zufall.

## Flächenschaden-Falloff

Flächenwaffen nutzen einen **Stufen-Falloff mit drei Zonen** (kein kontinuierlicher Gradient):

| Zone | Anteil am Explosionsradius | Schaden |
|---|---|---|
| Kern | 0–33 % | 100 % |
| Mittel | 33–66 % | 50 % |
| Rand | 66–100 % | 25 % |

Begründung: Drei Zonen sind für Spieler lesbar ("Mitte = tot, Rand = Kratzer"), für die Simulation billig (Radius-Check statt Kurvenauswertung bei 500+ Einheiten) und als drei flache Werte pro Waffe speicherbar. Ein stetiger Gradient wurde geprüft und verworfen (schlechtere Lesbarkeit, kein spürbarer Gameplay-Gewinn). Falloff gilt nur für `Area`- und `Missile`/`Explosive`-Waffen mit Radius > 0; `Hitscan`/`Projectile`-Einzelzielwaffen haben keinen Falloff.

## Status-Effekte

Status-Effekte sind eigenständige Datensätze, die Waffen optional referenzieren (eine Waffe kann 0–1 Status-Effekt auslösen). Allgemeine Regeln:

- **Kein Stacking:** Gleicher Effekt auf gleichem Ziel wird **aufgefrischt** (Dauer neu gesetzt), nicht addiert. Begründung: verhindert DoT-Stapel-Exploits, hält die Simulation flach.
- **Tick-Rate:** DoT-Effekte ticken 1×/Sekunde (simulationsfreundlich, gut lesbar).
- **Immunitäten:** Gebäude sind immun gegen Verlangsamung; Evolvierte-Einheiten sind verbindlich immun gegen EMP (D-027.2: biologisch, keine Elektronik); reine Bio-Einheiten erhalten halben Strahlungsschaden über die Matrix (siehe ArmorSystem.md) statt Sonderregeln.

| ID | Effekt | Auslöser (typisch) | Wirkung (Richtwerte v0.1) |
|---|---|---|---|
| `Burning` | Brennen | Feuer/Thermobarisch | DoT: ~3–5 % Max-HP/s für 3–5 s. Zündet brennbare Vegetation/Dekor an (D-012). |
| `Contamination` | Verseuchung | Bio/Säure | DoT: ~2–4 % Max-HP/s für 4–6 s; **blockiert Regeneration** (Evolvierte) während der Wirkdauer – der explizite Konter gegen die Evolvierten-Mechanik. |
| `Slow` | Verlangsamung | Kristallwaffen, Kristallsturm | Bewegung −30–50 %, Angriffsgeschwindigkeit −15–25 %, Dauer 3–6 s. |
| `EMP` | EMP | Allianz-Sturmjäger (EMP-Waffe; D-027.3: EMP bleibt ihm vorbehalten) | Fahrzeuge/Gebäude: bewegungs- und feuerunfähig für 2–4 s; pausiert den Energie-Output getroffener Kraftwerke **nicht** (D-027.9: keine Doppelbestrafung mit der Low-Power-Regel). Evolvierte immun (D-027.2). |

DoT in Prozent vom Max-HP statt absoluter Werte: skaliert automatisch über Tiers und Einheitengrößen, ohne jede Waffe gegen jede Einheit einzeln tunen zu müssen.

## Friendly-Fire-Regel

**Standard: aus.** Eigene und verbündete Einheiten erhalten keinen Schaden durch eigene Waffen, inklusive Flächenschaden und DoT. Optional als Skirmish-/Match-Option "Friendly Fire an" aktivierbar (TPD §8.6 sieht die Option vor).

**Ausnahme Superwaffen:** Die drei Superwaffen (Ionenstrahl, Thermobarisch, Kristallsturm) schädigen **immer alle** Einheiten/Gebäude im Wirkungsbereich, unabhängig von der Friendly-Fire-Option und vom Auslöser. Begründung: Superwaffen sind der strategische Höhepunkt (D-015-Logik des Endspiels); ihre Flächenwirkung als universelle Gefahr ist C&C-Erwartung der H1-Zielgruppe und verhindert risikofreies "in die eigene Schutztruppe feuern".

## Deckungs-Regel

**Standard: aus (kein Deckungssystem).** Gelände, Mauern und Vegetation gewähren keinen Schadens- oder Trefferbonus.

Begründung:
- **Lesbarkeit (D-019):** Bei 100–500+ Einheiten und schräger Top-Down-Kamera sind Deckungszustände pro Einheit visuell nicht vermittelbar; die H1-Zielgruppe erwartet Armee- statt Squad-Mikro.
- **Simulationsbudget:** Projektil-Raycasts gegen Deckungsflags pro Schuss konkurrieren mit Pathfinding/FoW um dasselbe Grid-Budget.
- **Ersatz:** Taktische Positionsvorteile werden über Reichweite, Höhen-Sichtregeln (FogOfWar.md), Brücken/Chokepoints (D-012) und die Schaden-gegen-Panzerung-Matrix geliefert – nicht über einen zusätzlichen Schadensmodifikator.

Eine späte Re-Evaluierung als rein binäres Terrain-Flag (z. B. "Infanterie in Ruinen −25 % Schaden") ist möglich, aber nicht Gegenstand von MVP/Alpha (siehe Offene Punkte).

## Schadenspipeline (Verrechnungsreihenfolge)

1. Treffer-Modell stellt Treffer/Zielpunkt fest (keine Trefferwürfe).
2. Flächenwaffen: Betroffene Ziele per Radius-Zone (Falloff) sammeln.
3. `Endschaden = Basisschaden × Matrix-Multiplikator (Schadensart × Panzerungsklasse)` ([ArmorSystem.md](ArmorSystem.md)) × Falloff-Faktor. **Kein flacher Rüstungsabzug** – nur Multiplikatoren (C&C-Warhead-Modell: eine Zahl pro Kombination, trivial datengetrieben, ohne Schwellenwert-Pathologien).
4. Status-Effekt anhängen bzw. auffrischen (falls Waffe einen definiert).
5. HP-Abzug; Zerstörung löst die Zerstörbarkeits-Regeln aus D-012 aus (Gebäude, Vegetation brennbar, Brücken, Aetherium-Felder beschädigbar).

Datengetrieben (SO-Referenzfelder je Waffe): `damageType`, `hitModel`, `baseDamage`, `areaRadius`, `falloffProfile` (3 Werte), `statusEffect` (optional), `statusChance` (Standard 1.0).

## Offene Punkte

- Entschieden im Korrekturlauf (D-027) und oben verankert: EMP pausiert keinen Kraftwerk-Output (D-027.9, keine Doppelbestrafung mit Low-Power); Evolvierte-EMP-Immunität verbindlich (D-027.2); Ionenstrahl ohne EMP-Nebenwirkung (D-027.3).
- **Deckung (offen):** Re-Evaluierung als binäres Terrain-Flag frühestens nach Alpha-Playtests vorgesehen; benötigt Freigabe durch den Game Director, da es die Kampf-Lesbarkeit fundamental verändert.
- **Punktverteidigung/Gegenmaßnahmen (offen):** Abfang-Mechanik gegen Raketen ist als Design-Haken notiert, aber ohne Träger-Einheit im Zahlengefüge – Zuordnung zu einem Upgrade/Modul bleibt offen (Koordination mit ResearchTree.md nötig).

## Nächste Schritte

- Konsistenzabgleich mit [Weapons.md](Weapons.md) (jede Waffe referenziert genau eine hier definierte Schadensart/ein Treffer-Modell) und [ArmorSystem.md](ArmorSystem.md) (Matrix-Multiplikatoren).
- Playtest-Prototyp (Sprint 3+) kalibriert DoT-Prozentsätze und Falloff-Zonen gegen die Ziel-Matchdauer 20–35 Minuten (D-010).

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Gameplay Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Lead Gameplay Designer |
