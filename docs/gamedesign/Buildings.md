# Gebäude – alle Fraktionen

**Version:** 0.6.0 | **Status:** Entwurf – MS-1-Override verbindlich | **Verantwortungsbereich:** Lead Gameplay Designer | **Sprint:** 16

## Zweck

Spielbare Spezifikation aller 12 Gebäudetypen (D-008) für die drei Fraktionen Allianz, Legion und Evolvierte: Namen, Rollen, Voraussetzungen, Kosten, Bauzeiten, Energiebilanz, Trefferpunkte und Besonderheiten. Zusätzlich werden die Detail-Systeme definiert: Verteidigungsplattform-Module, Mauern, Superwaffen, Platzierungsregeln, Verkauf/Reparatur, Evolvierte-Wachstumsmechanik (D-011) und Bauwarteschlangen. Alle Werte sind Richtwerte v0.2 zum Tunen und als flache, ScriptableObject-taugliche Datensätze (gemäß TPD §11/§15) angelegt.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-008 (12 Typen), D-010 (Aetherium-Wirtschaft), D-011 (Evolvierte-Wachstum), D-012 (Zerstörbarkeit), D-022 (Capture-System), D-023 (Superwaffen-Limit), D-024 (Lager & Raffinerie), D-030 (Low-Power/Forschung), D-077 (MS-1-Startzustand), D-096/D-106 (abgeleitete AE-Grenze und Überhang)
- [./Factions.md](./Factions.md) – Fraktionsidentitäten
- [./Economy.md](./Economy.md) – AE-Währung, Energie, Low-Power-Regel, Harvester-Werte, Lager-Kapazität (D-024)
- [./ResearchTree.md](./ResearchTree.md) – Tech-Tiers 1–3
- [./Infantry.md](./Infantry.md), [./NeutralUnits.md](./NeutralUnits.md) – Capture-Regelwerk (D-022)
- [./Weapons.md](./Weapons.md), [./Aircraft.md](./Aircraft.md) – Flak-Modul-Rahmenwerte
- [./Maps.md](./Maps.md) – Baugrid, Terrain, Biome
- [../research/Pathfinding.md](../research/Pathfinding.md) – Grid-/Flow-Field-Grundlage für Platzierung

## MS-1-Override (D-056)

Für MS-1 gelten ausschließlich die neun Rollen aus
[MVPContentManifest.md](../production/MVPContentManifest.md): HQ, Kraftwerk,
Raffinerie, Lager, Kaserne, Fahrzeugfabrik, Forschungslabor, Radar und
Verteidigungsplattform – jeweils mit den dort festgelegten Allianz- und
Legionsnamen. Mauern, Flugfeld und Superwaffe bleiben Post-MVP.

Jede Seite startet mit fertiggestelltem HQ und fertiggestellter Raffinerie. Nur
diese Start-Raffinerie darf ihre normale Kraftwerk-Voraussetzung umgehen und
erzeugt keinen zusätzlichen Harvester. Ein fertiggestelltes Forschungslabor
schaltet T2 unmittelbar frei; Forschung, Forschungsqueue und T3 sind
deaktiviert. Die Verteidigungsplattform unterstützt `MG` auf T1 und `Rocket`
auf T2; `Flak` ist nicht Teil von MS-1. Bei Widerspruch übersteuert dieser
Abschnitt den nachfolgenden Vollspielentwurf.

## 1. Grundprinzipien

- **Führende Quelle für Gebäudewerte (Review F-03, Grundsatzregel D-047):** Dieses Dokument ist die alleinige Quelle für Gebäudekosten, Energiewerte (Erzeugung/Verbrauch) und Bauzeiten. [./Economy.md](./Economy.md) verweist hierher und enthält nur Systemlogik (Einkommensraten, Low-Power-Regel, Lager-Kapazität); doppelte Zahlenpflege ist unzulässig.
- **12 Typen pro Fraktion, identische Rollen, eigene Namen und Werte-Deltas** (D-008, D-011). Die Rolle ist fraktionsübergreifend balancierbar; Kosten/TP/Bauzeit tragen die Fraktionsidentität (Allianz teuer/präzise, Legion günstig/massig, Evolvierte regenerativ/wachsend).
- **Energiebilanz:** Kraftwerke produzieren, fast alles andere verbraucht. Defizit löst die Low-Power-Regel aus: Produktions-, Bau- und Forschungsgeschwindigkeit −50 % (D-030), Radar und Verteidigungsplattformen offline, Superwaffen-Ladung pausiert (D-030) (Zahlengerüst).
- **Lagerkapazität (D-024/D-096/D-106):** Genau eine HQ-Basiskapazität von 2.000 AE je Konto, sobald mindestens ein fertiges HQ lebt; +2.000 AE je fertigem Lager. Neue Einzahlungen werden hart gedeckelt. Vorhandener Überhang verliert unabhängig von seiner Ursache einmal pro Sekunde 25 % des aktuellen Überhangs (Abrundung, mindestens 1 AE) bis zur Grenze.
- **Eroberung (D-022):** Feindliche Gebäude sind eroberbar – eine Capture-Einheit kanalisiert 5 s am Ziel (Abbruch bei Schaden) und wird bei Erfolg verbraucht. Capture-Einheiten: Engineer (Allianz), Saboteur (Legion), Tunnelgräber (Evolvierte). Regelwerk und Details in [./Infantry.md](./Infantry.md) und [./NeutralUnits.md](./NeutralUnits.md); gilt ebenso für neutrale Geschütztürme (D-016).
- **Trefferpunkt-Klassen:** Leicht (400–800 TP), Mittel (900–1.500 TP), Schwer (1.600–2.500 TP). Klasse statt Pseudo-Präzision; exakte Werte entstehen im Balancing-Pass.
- **Währung:** AE (Aetherium-Einheiten). MS-1-Referenz: Startressourcen 3.000 AE (D-077), Harvester-Ladung ~300 AE, Ziel-Matchdauer 20–35 min (D-010).

## 2. Gebäudeübersicht (12 Typen × 3 Fraktionen)

Voraussetzungsschlüssel: HQ = Hauptquartier, KW = Kraftwerk, Raff = Raffinerie, FL = Forschungslabor, T2/T3 = Tech-Tier 2/3.

### 2.1 Hauptquartier (HQ)

| Fraktion | Name | Kosten | Bauzeit | Energie | TP-Klasse |
|---|---|---|---|---|---|
| Allianz | Kommandozentrale | 2.500 AE | 60 s | +30 / −0 | Schwer |
| Legion | Gefechtsstand | 2.000 AE | 50 s | +30 / −0 | Schwer |
| Evolvierte | Herzkristall | 2.300 AE | 55 s (Reifung) | +30 / −0 | Schwer |

- Rolle: Startgebäude, produziert Baufahrzeuge (Allianz/Legion) bzw. Keimträger (Evolvierte), einzige Bauwarteschlange für Gebäude; Niederlage-Bedingung zusammen mit allen Produktionsgebäuden; mindestens ein fertiges HQ aktiviert die einmalige AE-Kontobasis von 2.000 AE, weitere HQs stapeln sie nicht (D-106, siehe §2.4).
- Voraussetzung: keine (Start); Neuaufbau nach Verlust möglich, erst ab T2 über FL-Forschung "Basis-Neugründung" (`SPC_REBASE`, bestätigt im Korrekturlauf Sprint 2). **Ausführungsmechanik (D-031.1):** Nach abgeschlossener Forschung errichtet ein Builder-Fahrzeug (Allianz/Legion) bzw. das Evolvierte-Builder-Äquivalent das neue HQ **eigenständig vor Ort** – außerhalb der HQ-Bau-Queue (§8); die Queue-Regel (Gebäudebau nur über das HQ) bleibt für alle anderen Gebäude unverändert.
- Besonderheit: definiert den Bau-Einflussradius (siehe §6); begrenzte Grundenergie (+30, führend gemäß D-032) damit die Frühphase ohne sofortiges Kraftwerk spielbar ist.

### 2.2 Kraftwerk

| Fraktion | Name | Kosten | Bauzeit | Energie | TP-Klasse |
|---|---|---|---|---|---|
| Allianz | Fusionsreaktor | 450 AE | 15 s | +100 | Mittel |
| Legion | Schwerer Generator | 350 AE | 12 s | +80 | Mittel |
| Evolvierte | Wachstumsknoten | 400 AE | 14 s (Reifung) | +90 | Leicht |

- Rolle: Energieproduktion; Rückgrat der Ökonomie (Low-Power-Regel).
- Voraussetzung: HQ. Legion liefert weniger Leistung pro Gebäude, aber günstiger/schneller (Masse-Identität); Evolvierte-Knoten regeneriert nach Schaden (D-011).
- Besonderheit: Zerstörung erzeugt kleinen Flächenschaden an Nachbarzellen (Risiko bei dichtem Bauen).

### 2.3 Raffinerie

| Fraktion | Name | Kosten | Bauzeit | Energie | TP-Klasse |
|---|---|---|---|---|---|
| Allianz | Aetherium-Aufbereiter | 700 AE | 20 s | −20 | Mittel |
| Legion | Schmelzofen | 550 AE | 16 s | −15 | Mittel |
| Evolvierte | Verdauungsbecken | 650 AE | 18 s (Reifung) | −15 | Mittel |

- Rolle: Abgabepunkt für Harvester (Ladung ~300 AE); wird mit 1 Harvester geliefert (D-024); schaltet Harvester-Produktion frei (über Fahrzeugfabrik) und Lagerbau.
- Voraussetzung: HQ, KW. Platzierung max. 3 Gridzellen vom Aetherium-Feld (siehe §6).
- Besonderheit: Überernte-Interaktion gemäß D-010 wird in Economy.md spezifiziert; Raffinerie selbst zeigt Feldzustand im UI an.

### 2.4 Lager

| Fraktion | Name | Kosten | Bauzeit | Energie | TP-Klasse |
|---|---|---|---|---|---|
| Allianz | Depot | 300 AE | 10 s | −5 | Leicht |
| Legion | Bunkerdepot | 250 AE | 8 s | −5 | Leicht |
| Evolvierte | Speicherkammer | 275 AE | 9 s (Reifung) | −5 | Leicht |

- Rolle: erhöht die abgeleitete AE-Lagerkapazität um +2.000 AE je fertigem, lebendem Lager (D-106); die HQ-Kontobasis beträgt einmalig 2.000 AE. Baustellen geben keine Kapazität.
- Voraussetzung: Raff.
- Besonderheit: Zerstörung oder Verkauf senkt die Grenze sofort. Der dadurch entstehende Überhang verliert einmal pro Sekunde 25 % seines jeweils aktuellen Werts bis zur neuen Grenze; es gibt keinen zusätzlichen Einmalverlust (D-106; Angriffsziel mit wirtschaftlichem Effekt).

### 2.5 Kaserne

| Fraktion | Name | Kosten | Bauzeit | Energie | TP-Klasse |
|---|---|---|---|---|---|
| Allianz | Ausbildungszentrum | 500 AE | 18 s | −15 | Mittel |
| Legion | Rekrutenlager | 400 AE | 14 s | −10 | Mittel |
| Evolvierte | Brutkammer | 450 AE | 16 s (Reifung) | −10 | Mittel |

- Rolle: produziert Infanterie (8 Typen/Fraktion, siehe Infantry.md).
- Voraussetzung: HQ, KW.

### 2.6 Fahrzeugfabrik

| Fraktion | Name | Kosten | Bauzeit | Energie | TP-Klasse |
|---|---|---|---|---|---|
| Allianz | Fahrzeugwerk | 900 AE | 25 s | −25 | Schwer |
| Legion | Montagehalle | 700 AE | 20 s | −20 | Schwer |
| Evolvierte | Metamorphosegrube | 800 AE | 22 s (Reifung) | −20 | Schwer |

- Rolle: produziert Fahrzeuge (12/Fraktion), Harvester und Boden-Support-Drohnen (D-014).
- Voraussetzung: Kaserne, Raff.

### 2.7 Flugfeld

| Fraktion | Name | Kosten | Bauzeit | Energie | TP-Klasse |
|---|---|---|---|---|---|
| Allianz | Luftwaffenbasis | 800 AE | 22 s | −25 | Mittel |
| Legion | Feldflugplatz | 650 AE | 18 s | −20 | Mittel |
| Evolvierte | Sporenhorst | 725 AE | 20 s (Reifung) | −20 | Mittel |

- Rolle: produziert Luftfahrzeuge (7/Fraktion) und Luft-Support-Drohnen (D-014); beinhaltet 4 Landepads, Pads sind Angriffsziele.
- Voraussetzung: Fahrzeugfabrik, T2.

### 2.8 Forschungslabor

| Fraktion | Name | Kosten | Bauzeit | Energie | TP-Klasse |
|---|---|---|---|---|---|
| Allianz | Forschungslabor | 1.000 AE | 30 s | −30 | Mittel |
| Legion | Kriegslabor | 800 AE | 24 s | −25 | Mittel |
| Evolvierte | Synapsenhülle | 900 AE | 27 s (Reifung) | −25 | Mittel |

- Rolle: schaltet T2/T3 frei, enthält Upgrades und Superwaffen-/Elite-Freischaltungen (D-015); Forschungs-Queue separat von Produktions-Queues; Low Power −50 % gilt auch für Forschungsgeschwindigkeit (D-030).
- Voraussetzung: Fahrzeugfabrik.

### 2.9 Radar

| Fraktion | Name | Kosten | Bauzeit | Energie | TP-Klasse |
|---|---|---|---|---|---|
| Allianz | Radarstation | 400 AE | 15 s | −20 | Leicht |
| Legion | Funkposten | 300 AE | 12 s | −15 | Leicht |
| Evolvierte | Sinnesorgan | 350 AE | 13 s (Reifung) | −15 | Leicht |

- Rolle: aktiviert Minimap, zeigt Gegner-Bewegungen in Reichweite; offline bei Low Power.
- Voraussetzung: KW, Kaserne.

### 2.10 Verteidigungsplattform (Modulsystem)

| Fraktion | Name | Kosten (Basis) | Bauzeit | Energie | TP-Klasse |
|---|---|---|---|---|---|
| Allianz | Aegis-Plattform | 400 AE | 12 s | −10 | Mittel |
| Legion | Geschützstellung | 300 AE | 10 s | −8 | Mittel |
| Evolvierte | Rankenturm | 350 AE | 11 s (Reifung) | −8 | Mittel |

- Rolle: stationäre Verteidigung; Basis ist ein unbewaffnetes Podest, Bewaffnung über Module (§3). Offline bei Low Power.
- **Aggressions-Modi (D-031.6):** Die Plattform nutzt dieselben drei Aggressions-Modi wie Einheiten – **Halten / Abwehren (Standard) / Freies Feuer** –, umschaltbar über die Befehlskachel der Auswahl; Logik identisch zu [../vision/CoreGameplay.md](../vision/CoreGameplay.md) (Abschnitt Aggressions-Modi). Der Modus gilt unverändert für alle Modul-Stufen (MG/Flak/Rakete).
- Voraussetzung: Kaserne (MG-Modul), Radar (Flak-Modul), FL (Raketen-Modul).

### 2.11 Mauer

| Fraktion | Name | Kosten/Segment | Bauzeit | Energie | TP-Klasse |
|---|---|---|---|---|---|
| Allianz | Stahlbarriere | 20 AE | 1 s | 0 | Mittel |
| Legion | Schrottwall | 12 AE | 1 s | 0 | Mittel |
| Evolvierte | Chitinwall | 16 AE | 1 s (Reifung) | 0 | Leicht |

- Rolle: passive Sperre, Segmente à 1 Gridzelle; Regeln siehe §4.

### 2.12 Superwaffe

| Fraktion | Name | Effekt | Kosten | Bauzeit | Energie | TP-Klasse |
|---|---|---|---|---|---|---|
| Allianz | Ionenstrahl-Projektor | präziser, anhaltender Strahl, hoher Einzelflächen-Schaden | 2.500 AE | 45 s | −100 | Schwer |
| Legion | Thermobarischer Werfer | großflächige Druckwelle + Brandfläche | 2.000 AE | 40 s | −80 | Schwer |
| Evolvierte | Kristallsturm-Kokon | wachsender Sturm, verstärkt sich auf Aetherium-Feldern | 2.250 AE | 42 s (Reifung) | −90 | Schwer |

- Rolle: Endspiel-Werkzeug (T3); Ladung/Abklingzeit siehe §5. Limit 1 pro Spieler; Bau löst globale Ansage an alle Spieler aus (D-023).

## 3. Detail: Verteidigungsplattform-Modulsystem

Module werden auf dem fertigen Podest installiert (nicht als separates Gebäude), können gewechselt werden (Ausbau → 50 % Rückerstattung).

| Modul | Kosten | Bauzeit | Zieltyp | Stärke | Voraussetzung |
|---|---|---|---|---|---|
| MG | 250 AE | 8 s | Boden (Infanterie) | hohe Feuerrate, geringer Einzelschaden | Kaserne |
| Flak | 300 AE | 8 s | Luft | Flächenschaden gegen Luftverbände (Werte unten) | Radar |
| Rakete | 400 AE | 10 s | Boden (Fahrzeuge) + Luft | langsamer, schwerer Schuss, gute Reichweite | FL |

- Max. 1 Modul pro Plattform; Modulwechsel dauert halbe Bauzeit des neuen Moduls.
- **Flak-Modul-Rahmenwerte** (Abgleich mit [./Weapons.md](./Weapons.md) und [./Aircraft.md](./Aircraft.md)): Reichweite 11–12, Abklingzeit 1,5 s, Schaden 25–40 mit kleinem Flächenradius (1 Zelle); Schadensmultiplikator ×2,0 gegen Lufteinheiten – Flak ist der harte Luft-Konter (2–3 Plattformen machen eine Basis dicht, DPS-Abgleich siehe Offene Punkte).
- Evolvierte-Rankenturm nutzt dieselben Module mit Bio-Äquivalenten (Stachelschwarm/Säuresporen/Impulsdorn), gleiche Zahlen (D-011: Regel-Delta bleibt klein).

## 4. Detail: Mauer-Regeln

- Segment = 1 Gridzelle, Bau per Ziehen (Linie), Kosten pro Segment; Diagonalen nicht erlaubt.
- Pathfinding-Interaktion: Mauern blockieren Boden-Pathfinding und aktualisieren das Flow-Field; Bauauftrag auf von Einheiten belegten Zellen wird verweigert; eigene Einheiten öffnen keine Tore – Durchlässe entstehen durch Verkauf einzelner Segmente (50 % Rückerstattung, §7); kein Tor-Gebäude im MVP (Tor-Modul als Beta-Evaluierung, siehe Offene Punkte).
- Lufteinheiten ignorieren Mauern; Artillerie/Superwaffen können Mauern zerstören (D-012), Nahkampf-Infanterie nicht.
- Zerstörte Segmente hinterlassen 30 s Trümmer (passierbar für Infanterie, nicht für Fahrzeuge).

## 5. Detail: Superwaffen-Gebäude

- **Ladung:** nach Fertigstellung lädt die Waffe 180 s; danach kann der Effekt einmal frei platziert werden.
- **Abklingzeit:** 300 s nach jedem Einsatz; bei Low Power ist die Superwaffe offline – die Ladung pausiert, ohne zurückgesetzt zu werden (bestätigt D-030).
- **Limits (D-023 final):** max. 1 Superwaffen-Gebäude pro Spieler; Bau löst globale Ansage an alle Spieler aus; Bau kostet zusätzlich 50 Energie Dauerlast (in Tabelle enthalten).
- Zerstörung eines geladenen Gebäudes löst den Effekt mit 25 % Stärke am eigenen Standort aus (D-023: Rückschlag-/Risiko-Mechanik, Sabotage-Anreiz).

## 6. Detail: Platzierungsregeln

- **Baugrid:** einheitliches quadratisches Grid (Deckung mit Pathfinding-/FoW-Grid, siehe Maps.md); Gebäudefootprints von 2×2 (Lager, Radar, Plattform) bis 4×4 (HQ, Fabriken, Superwaffe); Rotieren in 90°-Schritten.
- **Einflussradius:** Neubauten nur innerhalb des Radius (8 Zellen) eines eigenen HQ, Lagers oder Kraftwerks; Evolvierte zusätzlich innerhalb des Herzkristall-/Wachstumsknoten-Radius.
- **Abstand zu Aetherium-Feldern:** nur Raffinerien (und Evolvierte-Bauten zwecks Wachstumsbonus, §7) dürfen direkt angrenzen (max. 3 Zellen); alle anderen Gebäude mindestens 2 Zellen Abstand – verhindert Feld-Blockade und hält Harvester-Routen frei (D-010).
- **Kollision:** kein Bau auf Feldern, Steilhängen, Wasser (D-013), Brücken, neutralen Strukturen oder im Mindestabstand von 1 Zelle zu anderen Gebäuden (Ausnahme Mauern).
- Platzierungs-Vorschau zeigt valide (grün) / invalide (rot) Zellen inkl. Begründungs-Tooltip.

## 7. Detail: Verkauf, Reparatur, Evolvierte-Wachstum (D-011)

**Allianz & Legion**

- Verkauf: 50 % Rückerstattung, 3 s Abbau, jederzeit außer unter Beschuss.
- Reparatur: Baufahrzeug oder Repair-Drohne (D-014); Kosten 30 % des Neupreises für volle Reparatur, Reparaturrate ~2 % TP/s.

**Evolvierte (Wachstum statt Konstruktion)**

- **Keim → Reifung:** Bauauftrag pflanzt einen Keim (sofort volle AE-Kosten, Keim hat 25 % der Ziel-TP); das Gebäude reift über die Bauzeit, TP wachsen linear mit.
- **Aetherium-Nähe-Beschleunigung:** Reifung innerhalb von 4 Zellen eines lebenden Feldes −25 % Bauzeit, innerhalb von 8 Zellen −10 %; darüber Normalwert. Keine Stapelung mehrerer Felder.
- **Regeneration statt Reparatur:** alle Evolvierte-Gebäude regenerieren 2 % TP/s, sobald 5 s kein Schaden eingegangen ist; keine manuelle Reparatur möglich.
- **Verkauf = Reabsorption:** 40 % Rückerstattung (weniger als konventionell, kompensiert durch Regeneration), Keime nur 25 %.
- Beschädigte Evolvierte-Gebäude auf/nahe Feldern regenerieren +50 % schneller (Synergie mit Feldpositionierung, bewusstes Risiko wegen Überernte-/Feldzerstörungs-Regeln, D-010/D-012).

## 8. Bauwarteschlangen-Regeln

- **Gebäude-Queue:** nur über das HQ (bzw. Herzkristall), max. 5 Aufträge; mehrere HQs = mehrere Queues.
- **Produktions-Queues:** je Produktionsgebäude eine eigene Queue (max. 9 Einheiten); mehrere Gebäude desselben Typs teilen sich Aufträge automatisch (C&C-QOL-Konvention).
- **Forschungs-Queue:** separat im FL, max. 3, läuft parallel zur Produktion; Low Power −50 % gilt auch hier (D-030).
- Priorisierung: Aufträge pro Queue pausierbar; Abbruch erstattet 100 % (vor Baubeginn) bzw. anteilig 75 % (während des Baus). Low Power verlangsamt laufende Queues um 50 % (Produktion, Bau und Forschung, D-030).
- Superwaffen-Ladung und Evolvierte-Reifung laufen außerhalb der Queues (gebäudeeigener Timer).

## Offene Punkte

- **Tor-Modul (Post-MVP):** Mauern und Tore sind nicht Teil von MS-1. Ein
  späterer Scope entscheidet Durchlass- und Torregel gemeinsam neu.
- **Flak-DPS-Korridor: entschieden (D-047, Korrekturlauf Sprint 4).** [Weapons.md](./Weapons.md) ist die einzige Werte-Quelle für das Flak-Modul (Reichweite 11–12 Felder ≙ m, Schaden 25–40 pro Schuss / 1,5 s Abklingzeit, ×2,0-Multiplikator gegen Luft ≈ 33–53 DPS effektiv); [Aircraft.md](./Aircraft.md) wurde im selben Korrekturlauf auf diesen Korridor angeglichen (der frühere Wert 90 DPS ist überholt und entfällt). Punkt geschlossen.
- **Footprints (Status: Abgleich mit Maps.md läuft):** Exakte Footprints (2×2 bis 4×4) sind Annahmen; Finalisierung mit Maps.md.

Entschieden und entfernt im Korrekturlauf Sprint 4: HQ-Grundenergie (+30 führend in diesem Dokument, D-032 – war bereits als geschlossen markiert).

## Nächste Schritte

- Abgleich der AE-/Energie-Werte mit Economy.md (Harvester-Raten, Low-Power-Definition, HQ-Grundenergie +30, Lagerwerte D-024) und der Tier-Freischaltungen mit ResearchTree.md.
- Footprint- und Grid-Finalisierung mit Maps.md; KI-Bauplatzierungs-Regeln als Input für AIArchitecture.
- Balancing-Pass v0.2 nach erstem spielbaren Skirmish-Prototyp (Sprint 3+).

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Gameplay Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Lead Gameplay Designer |
| 0.3.0 | 2026-07-21 | Feinschliff Sprint 2 Runde 2 (D-031) | Lead Gameplay Designer |
| 0.3.1 | 2026-07-21 | HQ-Grundenergie +30 als führend bestätigt, Offener Punkt geschlossen (D-032) | Lead Gameplay Designer |
| 0.4.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings): als führende Quelle für Gebäudekosten/-energie/-bauzeiten festgelegt (Review F-03, D-047-Grundsatzregel); Offene Punkte bereinigt | Lead Gameplay Designer |
| 0.4.1 | 2026-07-21 | Offener Punkt "Flak-DPS-Korridor" geschlossen: veralteter Querverweis auf Aircraft.md (90 DPS) entfernt, auf Weapons.md als einzige Werte-Quelle (25–40 Schaden/1,5 s, ×2,0 vs. Luft ≈ 33–53 DPS, D-047) umformuliert | Lead Gameplay Designer |
| 0.5.0 | 2026-07-24 | Neun Gebäuderollen, Start-Ausnahme, T2-Freischaltung und MG-/Raketenmodule für MS-1 gemäß D-056 abgegrenzt | Lead Gameplay Designer |
| 0.6.0 | 2026-08-10 | MS-1-Startwert (D-077) und Lagervertrag (D-106) nachgezogen: eine 2.000-AE-HQ-Basis je Konto, +2.000 je fertigem Lager und periodischer 25-%-Abbau des aktuellen Überhangs statt separatem Zerstörungsabzug | Codex / Dennis Westermann |
