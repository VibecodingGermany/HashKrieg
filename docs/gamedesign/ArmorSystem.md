# Panzerungssystem (Armor System)

**Version:** 0.4.0 | **Status:** Entwurf – Matrix führend nach D-074, in Code implementiert | **Verantwortungsbereich:** Lead Gameplay Designer | **Sprint:** 2

## Zweck

Definiert die Panzerungsklassen aller Einheiten und Gebäude, die Schaden-gegen-Panzerung-Matrix (Multiplikatoren, Startwerte v0.1) sowie Regeneration (Evolvierte) und Reparatur im Schadenskontext. Die Matrix ist das zentrale Balancing-Werkzeug für Konter-Beziehungen; die Verrechnungsreihenfolge steht in [DamageSystem.md](DamageSystem.md), die Waffenwerte in [Weapons.md](Weapons.md).

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-011 (Evolvierte: Regeneration statt Reparatur), D-012 (Zerstörbarkeit), D-014 (Drohnen: Repair-Drohne), D-015 (Elite-Einheiten), D-027 (Fraktions-Sonderregeln: Legion ohne Infanterie-Heilung, EMP), **D-074 (dieses Dokument ist alleinige Autorität der Schaden-gegen-Panzerung-Matrix)**
- [../production/ScopeLedger.md](../production/ScopeLedger.md) – Register der in MS-1 unbespielten Matrixanteile
- [DamageSystem.md](DamageSystem.md) – Schadensarten, Pipeline, Status-Effekte
- [Weapons.md](Weapons.md) – Waffenkatalog
- [Factions.md](Factions.md), [Infantry.md](Infantry.md), [Vehicles.md](Vehicles.md), [Aircraft.md](Aircraft.md), [Buildings.md](Buildings.md) – Träger der Panzerungsklassen
- [Economy.md](Economy.md) – Reparaturkosten in AE
- [../analysis/KnowledgeBase.md](../analysis/KnowledgeBase.md) – TPD §8.1 (Panzerung als Einheitenattribut), §8.6 (Panzerungstypen)

## Panzerungsklassen

Sechs Klassen decken das komplette Zahlengefüge ab (Infanterie 8, Fahrzeuge 12, Luftfahrzeuge 7, Drohnen 2–3 pro Fraktion; 12 Gebäudetypen). Jede Einheit/jedes Gebäude hat genau **eine** Klasse – kein Mehrfach-Panzerungsmodell.

| ID | Klasse | Typische Träger | Design-Rolle |
|---|---|---|---|
| `Infantry` | Infanterie | Alle 8 Infanterietypen pro Fraktion | Günstig, verwundbar gegen Feuer/Bio/Kinetisch |
| `Light` | Leicht | Scout, APC, Drohnen (D-014), Radar-/Repair-Fahrzeuge | Schnell, schwach gepanzert |
| `Medium` | Mittel | Light/Battle Tank, Raketenwerfer, mittlere Luftfahrzeuge am Boden | Arbeitstier der Mittelspiel-Armeen |
| `Heavy` | Schwer | Heavy Tank, Elite-Einheiten (D-015), Superpanzer | Langsam, nur Explosiv/Energie brechen sie effizient |
| `Building` | Gebäude | Alle 12 Gebäudetypen (D-008), Mauern, Brücken (D-012), capturebare Geschütztürme (D-016) | Stationär, reparier-/regenerierbar |
| `Air` | Luft | Alle 7 Luftfahrzeugtypen, fliegende Drohnen | Nur von Waffen mit Zielklasse Luft treffbar |

Regeln und Begründungen:
- **Luft als eigene Klasse** statt "Leicht im Flug": Die Matrix muss Luftabwehr (Flak, Raketen) als klare Konter-Beziehung abbilden können; Zielerreichbarkeit (Boden/Luft) ist davon getrennt und in [Weapons.md](Weapons.md) je Waffe definiert.
- **Elite-Einheiten** (D-015) sind Klasse `Heavy`; ihre Sonderstellung entsteht über HP, Waffen und Limit 1–2, nicht über eine siebte Klasse. Begründung: Jede zusätzliche Klasse multipliziert den Matrix-Pflegeaufwand.
- **Vegetation/Dekor** ist keine Panzerungsklasse: Brennbarkeit ist ein Flag (D-012), Feuer-Schaden interagiert direkt mit dem Flag, nicht über die Matrix.
- **Aetherium-Felder**: Der Mutterkristall erhält Klasse `Building`; Ausläufer-Kristalle nutzen eine eigene Zerstörungsregel (Economy.md), um "Aetherium-Felder beschädigbar" (D-012) ohne Balance-Seiteneffekte auf Gebäudekämpfe abzubilden.

## Schaden-gegen-Panzerung-Matrix (Startwerte v0.1)

> **Autorität (D-074).** Die folgende 6 × 6-Matrix ist die **alleinige
> kanonische Quelle** für Schaden-gegen-Panzerung-Multiplikatoren. Die früher
> in [Infantry.md](Infantry.md) und [Vehicles.md](Vehicles.md) geführten
> Lokaltabellen waren abgeleitete Zusammenfassungen, sind auseinandergedriftet
> (u. a. gegenläufige Werte für Energie gegen Schwer und Explosiv gegen
> Gebäude) und sind mit D-074 **aufgehoben**; beide Dokumente verweisen jetzt
> hierher. Die 36 Werte unten sind durch D-074 **nicht verändert** worden.
>
> **In Code implementiert:** `Nova.Simulation.Combat.DamageMatrix` führt genau
> diese 36 Zahlen als ganzzahlige Prozentwerte (100 = 1.00) und wendet sie als
> `(Basisschaden × Prozent) / 100` in Ganzzahlarithmetik an — keine
> Fließkommazahlen in der Simulation. Alle sechs Zeilen und sechs Spalten sind
> vorhanden, obwohl MS-1 nur vier Schadensarten und fünf Panzerungsklassen
> bespielt; die unbespielten Anteile (`Heavy`, `Air`, Feuer/Bio/Strahlung) sind
> im [ScopeLedger](../production/ScopeLedger.md) registriert. D-074 wurde vom
> Agenten unter ausdrücklicher Inhaber-Delegation entschieden und ist
> überstimmbar.

Multiplikator auf den Basisschaden: `1.00` = neutral, `> 1.00` = Konter, `< 1.00` = ineffizient. Werte sind Richtwerte zum Tunen, keine Endwerte.

| Schadensart ↓ / Panzerung → | Infanterie | Leicht | Mittel | Schwer | Gebäude | Luft |
|---|---|---|---|---|---|---|
| Kinetisch | 1.00 | 0.75 | 0.50 | 0.25 | 0.30 | 0.75 |
| Energie | 0.75 | 1.00 | 1.00 | 0.75 | 0.50 | 1.00 |
| Explosiv | 0.75 | 0.75 | 1.00 | 1.00 | 0.75 | 0.50 |
| Feuer/Thermobarisch | 1.50 | 0.75 | 0.50 | 0.25 | 1.00 | 0.25 |
| Bio/Säure | 1.25 | 0.75 | 0.75 | 0.50 | 0.75 | 0.50 |
| Strahlung | 1.00 | 0.75 | 0.75 | 0.75 | 0.50 | 0.75 |

Konter-Logik hinter den Werten:
- **Kinetisch** ist die Infanterie-/Flak-Antwort (MG, Gewehre, Flak): voll wirksam gegen Infanterie, bricht an schwerer Panzerung (0.25 gegen Schwer erzwingt Raketen/Energie als Antwort auf Heavy).
- **Energie** ist das ausgewogene Allianz-Profil: keine harte Schwäche, beste Werte gegen Luft/Leichtes – präzise High-Tech-Identität, aber mit 0.50 gegen Gebäude kein Belagerungswerkzeug.
- **Explosiv** ist der Panzer- und Gebäudebrecher (Raketen, Artillerie): 1.00 gegen Mittel/Schwer, Flächenwirkung kompensiert den 0.75-Wert gegen Infanterie-Ballungen.
- **Feuer/Thermobarisch** ist der Infanterie-Killer und Belagerer der Legion (1.50/1.00), aber nutzlos gegen schwere Panzer und Luft (0.25) – Flammenpanzer brauchen Raketen-Eskorte.
- **Bio/Säure** spiegelt Feuer auf Evolvierten-Seite mit Gebäudetauglichkeit (0.75) und dem Verseuchungs-Konter gegen Regeneration (siehe DamageSystem.md).
- **Strahlung** ist bewusst flach (0.50–1.00, kein Wert > 1.00): Hazards (D-017) und Aetherium-Verstrahlung sollen Zonen verweigern, nicht gezielt kontern; Gebäude halb immun (0.50), damit Strahlungsfronten keine Basen abreißen.

Verrechnung: nur Multiplikatoren, **kein** flacher Rüstungsabzug und keine Rüstungs-Schwellenwerte – siehe Schadenspipeline in [DamageSystem.md](DamageSystem.md). Datengetrieben als flacher Satz von 36 Zahlen (`damageType × armorClass`), SO-tauglich.

## Regeneration (Evolvierte, gemäß D-011)

Evolvierte-Einheiten und -Gebäude regenerieren statt repariert zu werden:

| Parameter | Richtwert v0.1 | Begründung |
|---|---|---|
| Verzögerung nach letztem Schaden | 5 s | Verhindert Regeneration mitten im Gefecht; belohnt Fokusfeuer gegen Evolvierte |
| Regenerationsrate Einheiten | ~2 % Max-HP/s | Spürbar, aber kein Unbesiegbarkeits-Gefühl; vollständige Heilung ~50 s |
| Regenerationsrate Gebäude | ~1 % Max-HP/s | Gebäude sind Hoch-HP-Ziele; schnellere Regeneration würde Belagerung entwerten |
| Blockade durch Verseuchung (Bio/Säure) | vollständig, für Wirkdauer | Expliziter Konter, siehe Status-Effekte in DamageSystem.md |
| Kosten | keine AE-Kosten | Regeneration ist die Fraktionsstärke; der Wirtschaftsvorteil gegenüber der kostenpflichtigen Reparatur (Allianz/Legion) wird über die **langsamere Regenerationsrate** kompensiert – führend ist [Economy.md](Economy.md) (Reparatur-/Verkaufsregeln), **nicht** der Baukostenrahmen (D-031.5); keine eigene Ausgleichsregel in diesem Dokument |

## Reparatur (Allianz & Legion)

Allianz und Legion heilen nicht passiv; Reparatur ist aktive, kostenpflichtige Aktion:

| Weg | Regel (Richtwerte v0.1) |
|---|---|
| Repair-Fahrzeug / Repair-Drohne (D-014) | Reparaturstrahl: ~3–5 % Max-HP/s auf ein Ziel, Reichweite Nahbereich, nur Fahrzeuge/Gebäude, keine Infanterie |
| Gebäudereparatur (Bau-Menü) | Repariert sich selbst auf Befehl: ~1 % Max-HP/s, Kosten ~0.5 AE pro HP (Richtwert, Feinjustage in Economy.md) |
| Infanterie | Nicht reparierbar; Allianz: Medic-Einheit heilt Infanterie (separates Heil-Profil, analog 3 %/s); Legion: **keine Infanterie-Heilung** (D-027.4 – Masse-Identität, Ausgleich über günstige Neuproduktion: Ersatz statt Erhalt) |
| Evolvierte | Keine Reparatur-Mechanik (D-011); Bio-Heiler heilt nur Infanterie |

Kostenprinzip: Reparatur kostet einen Bruchteil des Neubaus (Faustregel: Volle Reparatur ≈ 50 % der Baukosten). Begründung: Erhalt soll sich lohnen, aber nicht gratis sein – das hält AE-Druck (D-010) auch im Verteidigungsspiel aufrecht.

## Wechselwirkungen mit dem Schadenssystem

- **EMP** lahmt mechanische Ziele (DamageSystem.md); eine EMP-gelähmte Repair-Drohne kann nicht reparieren – Reparatur ist unterbrechbar, Regeneration nur durch Verseuchung.
- **Brennen** auf Gebäuden (Klasse `Building`, Feuer-Multiplikator 1.00) plus Gebäudereparatur/-regeneration gleichzeitig: DoT und Heilung verrechnen sich unabhängig (kein Sperr-Flag); es gilt das Netto-Ergebnis. Begründung: keine Sonderregel, Simulation bleibt flach.
- **Brücken** (D-012) nutzen Klasse `Building` mit einem zerstörungsrelevanten HP-Schwellenwert (Definition in Maps.md), damit Explosiv-Schaden sie gezielt sprengen kann.

## Offene Punkte

- Entschieden im Korrekturlauf und oben verankert: Legion bewusst ohne Infanterie-Heiler (D-027.4); der Evolvierte-Regenerationsvorteil gegenüber kostenpflichtiger Reparatur wird über die **langsamere Regenerationsrate** kompensiert (Economy.md führend, **nicht** über Baukosten – Klarstellung D-031.5, keine eigene Regel in diesem Dokument).
- **Flugabwehr-Fahrzeug (offen):** Die Evolvierte-AA-Lücke der Infanterie ist durch D-026 geschlossen (Kristallmagier, Zielklasse `Both`); ob darüber hinaus ein dediziertes AA-Fahrzeug nötig ist, klärt das Balancing (Balancing.md) – aktuell decken Raketen-Infanterie, Raketen-Modul (D-008) und Luftfahrzeuge die Luftabwehr ab.
- **Strahlung gegen Gebäude (offen):** Multiplikator 0.50 verhindert Basis-Abriss durch Strahlungsfronten (D-017); falls Hazards auch Basen bedrohen sollen, ist das eine Karten-/Hazard-Entscheidung außerhalb dieses Dokuments.

## Nächste Schritte

- Klassen-Zuordnung aller 27 Einheitentypen + 12 Gebäudetypen pro Fraktion in Infantry.md/Vehicles.md/Aircraft.md/Buildings.md verankern (jeder Einheitendatensatz referenziert eine Klassen-ID).
- Matrix v0.1 gegen TTK-Zielkorridore (Time-to-Kill je Tier) in Balancing.md kalibrieren, sobald HP-/Schadens-Rahmen aus Weapons.md stehen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Gameplay Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Lead Gameplay Designer |
| 0.3.0 | 2026-07-21 | Feinschliff Sprint 2 Runde 2 (D-031) | Lead Gameplay Designer |
| 0.4.0 | 2026-07-26 | Autoritätsvermerk nach D-074 ergänzt: die 6 × 6-Matrix ist alleinige kanonische Quelle und in `Nova.Simulation.Combat.DamageMatrix` implementiert; Lokaltabellen in Infantry.md/Vehicles.md aufgehoben. **Keiner der 36 Werte geändert** | Agent (unter Inhaber-Delegation, D-074) |
