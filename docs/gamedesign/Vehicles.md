# Fahrzeuge (Vehicles)

**Version:** 0.5.0 | **Status:** Entwurf – MS-1-Override verbindlich | **Verantwortungsbereich:** Lead Vehicle Artist / Lead Gameplay Designer | **Sprint:** 4

## Zweck

Spezifikation aller Bodenfahrzeuge für die drei Fraktionen (Allianz, Legion, Evolvierte): 12 Fahrzeugtypen pro Fraktion gemäß Zahlengerüst, dazu Support-Drohnen (gemäß D-014) und Elite-Einheiten (gemäß D-015). Alle Werte sind Startwerte v0.3 zum Tunen – Richtwerte mit Begründung, keine Pseudo-Präzision. Ziel: rock-paper-scissors-fähiges Kontergeflecht, das die Fraktionsidentitäten (Allianz = präzise/teuer, Legion = Masse/günstig, Evolvierte = Regeneration/biologisch) spielmechanisch trägt.

## Abhängigkeiten

- [DecisionLog](../production/DecisionLog.md) – D-008 (12 Gebäudetypen), D-010 (Aetherium-Wirtschaft), D-011 (Evolvierte bauen durch Wachstum), D-014 (Drohnen), D-015 (Elite-Einheiten), D-021 (kein Pop-Limit außer Elite), D-026 (Radar-Fahrzeug, Konter-Lücken), D-034/D-047 (1 Grid-Feld ≙ 1 m, Weapons.md führend für Reichweiten), D-074 (Konter-Matrix führend in ArmorSystem.md)
- [ArmorSystem](./ArmorSystem.md) – **führende Quelle** für Panzerungsklassen und Schaden-gegen-Panzerung-Multiplikatoren (D-074)
- [Factions](./Factions.md) – Fraktionsidentitäten, Ressourcenprofile
- [Buildings](./Buildings.md) – Produktionsgebäude (Fahrzeugfabrik, Raffinerie), Voraussetzungen
- [Aircraft](./Aircraft.md) – geteilte Schadens-/Panzerungstypen, Flak-Konter
- [Weapons](./Weapons.md) – **führende Quelle** für Waffenwerte (Reichweite, Schaden, Abklingzeit), D-047
- [FogOfWar](./FogOfWar.md) – Sichtweiten-Klassen (VIS-*), führend für Sichtwerte (D-047)
- [ResearchTree](./ResearchTree.md) – Tier-1–3-Freischaltungen
- [Economy](./Economy.md) – Währung AE, Startressourcen 1.000 AE, Harvester-Ladung ~300 AE
- [Resources](./Resources.md) – Überernte-Regeln am Mutterkristall (D-010)
- [KnowledgeBase](../analysis/KnowledgeBase.md) – Asset-Umfang 36 Fahrzeuge (APL Paket 05)

## MS-1-Override (D-056)

Für MS-1 gelten genau sechs Fahrzeugrollen je aktiver Fraktion:

| Rolle | Allianz | Legion | Tier |
|---|---|---|---|
| Builder | Pionier „Atlas“ | Vorarbeiter | T1 |
| Harvester | Sammler „Demeter“ | Schürfer | T1 |
| Scout | Jackal-Aufklärer | Hyäne (Buggy) | T1 |
| Leichter Panzer | Lynx | Räuber | T1 |
| Kampfpanzer | Aegis | Koloss | T2 |
| Artillerie | Longbow | Donnerkanone | T2 |

Die Harvester-Kapazität beträgt für Allianz 330 AE und für Legion 300 AE.
Frühere aktive Fähigkeiten, Umschalter, Drohnen, Eliten und alle weiteren
Vollroster-Fahrzeuge sind Post-MVP. MS-1 führt dafür kein generisches
Fähigkeits-, Status-, Kanal- oder Aurensystem ein. Führende Inhaltsquelle ist
[MVPContentManifest.md](../production/MVPContentManifest.md).

## Datenmodell (ScriptableObject-tauglich)

Jede Einheit ist ein flacher Datensatz mit folgenden Feldern:

| Feld | Typ | Beschreibung |
|---|---|---|
| `id` | string | Eindeutige ID, z. B. `ALL_VEH_LIGHT_TANK` |
| `faction` | enum | Allianz / Legion / Evolvierte |
| `vehicleClass` | enum | Einer der 12 Typen |
| `tier` | int | 1–3 |
| `costAE` | int | Kosten in Aetherium-Einheiten |
| `buildTimeS` | float | Bauzeit in Sekunden (bei voller Energie) |
| `hp` | int | Trefferpunkte |
| `armorType` | enum | Leicht / Schwer |
| `damageType` | enum | Kinetisch / Explosiv / Flamme / Energie / Bio |
| `dps` | float | Schaden pro Sekunde (Richtwert) |
| `rangeM` | float | Schussweite in Grid-Feldern (1 Feld ≙ 1 m, D-034/D-047); verbindliche Werte in [Weapons](./Weapons.md) |
| `speedMS` | float | Geschwindigkeit in m/s |
| `sightM` | float | Sichtweite; Werte nur als VIS-Klassen in [FogOfWar](./FogOfWar.md) (D-047) |
| `prerequisites` | string[] | Gebäude-/Tech-Voraussetzungen |
| `abilities` | string[] | Aktive/passive Fähigkeiten |

**Schadens-Konter-Matrix: führend in [ArmorSystem](./ArmorSystem.md) (D-074).**
Die Schaden-gegen-Panzerung-Multiplikatoren stehen dort als 6 × 6-Matrix und
existieren genau einmal (Grundsatzregel D-047 – dieselbe Regel, die diese Datei
in der Verweis-Regel unten für Waffenwerte schon anwendet). Die früher an dieser
Stelle geführte 5 × 4-Tabelle über der Achse „vs. Leicht / vs. Schwer /
vs. Gebäude / vs. Luft" war eine abgeleitete Zusammenfassung, ist von der
führenden Quelle abgedriftet – teils gegenläufig, etwa Energie gegen Schwer
(hier 1,25, führend 0,75) und Explosiv gegen Gebäude (hier 1,25, führend 0,75) –
und wurde mit **D-074 aufgehoben**; sie ist ersatzlos entfernt.

Damit ändert sich auch die Panzerungsachse: Die `armorType`-Angabe im
Datenmodell oben nennt weiterhin `Leicht`/`Schwer` als Rahmenbeschreibung, die
regelwirksame Klasse einer Einheit ist jedoch eine der sechs Klassen aus
[ArmorSystem](./ArmorSystem.md) (`Infantry`, `Light`, `Medium`, `Heavy`,
`Building`, `Air`). Für MS-1 gilt die dortige Zuordnung unverändert: Leichter
Panzer **und** Kampfpanzer sind `Medium`, `Heavy` ist dem Heavy Tank
vorbehalten und damit in MS-1 unbespielt (registriert im
[ScopeLedger](../production/ScopeLedger.md)).

Flak-Waffen (Verteidigungsplattform-Modul, Radar-Fahrzeug nein) besitzen dedizierte Anti-Luft-Werte; Werte verbindlich in [Weapons](./Weapons.md) (D-047), Konter-Logik in [Aircraft](./Aircraft.md). Mobile Luftabwehr der Evolvierten (Konter-Lücke, D-026): Der Kristallmagier (Infanterie) erhält Zielklasse „Beides" (Boden + Luft) – Details in [Infantry](./Infantry.md).

**Verweis-Regel (D-047, Korrekturlauf Sprint 4):** 1 Grid-Feld ≙ 1 m (D-034/D-047). Verbindliche Waffenwerte (Reichweite, Schaden, Abklingzeit) existieren genau einmal – in [Weapons](./Weapons.md) (führend); verbindliche Sichtweiten nur als VIS-Klassen in [FogOfWar](./FogOfWar.md). Diese Datei definiert Rollen, Konter und Einheiten-Rahmenwerte (HP, Kosten, Tempo); die Reichweiten-Spalten folgen den Weapons.md-Korridoren. Grundsatz: Angriffsreichweite > Sichtweite ist Design-Prinzip (Scouting/Spotter, D-047), kein Fehler.

**Fraktions-Grundregeln für Werte:**

- Allianz: ~+25 % Kosten, ~+15 % Einzelwerte (HP/DPS), Energie-Schaden dominant, längere Reichweiten.
- Legion: ~−20 % Kosten, ~−15 % Einzelwerte, kürzere Bauzeiten (Masse statt Klasse), Flamme/Explosiv dominant.
- Evolvierte: mittlere Kosten, Einheiten regenerieren HP langsam außerhalb von Kampf (~1 % HP/s nach 5 s ohne Schaden) statt Reparatur; Reparatur-Fahrzeug ersetzt durch Heil-Symbionten. Bio-Schaden dominant. Evolvierte "bauen" Fahrzeuge konventionell in der Fahrzeugfabrik – die Wachstums-Regel D-011 gilt ausschließlich für Gebäude (bestätigt im Korrekturlauf Sprint 2). Die Produktion wird optisch als Schlüpfen aus Bio-Kokons inszeniert; Kosten und Bauzeit folgen denselben Regeln wie bei den anderen Fraktionen.

## Die 12 Fahrzeugtypen – Rollen und Konter

| # | Typ | Rolle | Konter gegen | Gekontert von |
|---|---|---|---|---|
| 1 | Scout | Frühe Aufklärung, hohe Sicht | Infanterie (Überfahren), ungeschützte Harvester | Jeder Panzer, Verteidigung |
| 2 | APC | Infanterie-Transport (6 Sitze), leicht bewaffnet | Infanterie im Nahkampf | Battle Tank, Artillerie |
| 3 | Light Tank | Schneller Tier-1-Kampf, Raid | Scout, Harvester, Artillerie (Flanke) | Battle Tank, Rocket Launcher |
| 4 | Battle Tank | Rückgrat der Armee, Tier 2 | Light Tank, APC, Gebäude (mittel) | Heavy Tank, Bomber, Artillerie |
| 5 | Heavy Tank | Durchbruch, langsamer Anker | Battle Tank, Verteidigung (Frontal) | Artillerie, Bomber, Flame Tank (Nähe) |
| 6 | Artillery | Indirektes Feuer, große Reichweite, Mindestreichweite | Gebäude, Verteidigung, statische Verbände | Alles in Nahdistanz, Scout-Raid |
| 7 | Rocket Launcher | Flächen-Salven vs. Fahrzeuggruppen + Luft (leicht) | Massierte leichte Einheiten | Battle Tank (Nahdistanz), Fighter |
| 8 | Flame Tank | Nahkampf, entzündet Vegetation/Dekor (D-012), Gebäude-Killer | Infanterie, Gebäude, Bunkerstellungen | Alles auf Distanz, Heavy Tank |
| 9 | Repair Vehicle | Feldreparatur (Evolvierte: Heilung) | – | Alles, unbewaffnet |
| 10 | Radar Vehicle | Mobiles Radar (erweiterte Sicht/Fog-Aufklärung) + Detektor (enttarnt getarnte Einheiten); Feuerleitungs-Verbandsmechanik gestrichen (D-026) | – (Support) | Alles, unbewaffnet |
| 11 | Harvester | Aetherium-Abbau, Ladung ~300 AE | – | Alles, bevorzugtes Raid-Ziel |
| 12 | Builder | Errichtet Gebäude (Allianz/Legion) bzw. pflanzt Keime (Evolvierte, D-011), langsam, teuer | – | Alles, priorisiertes Ziel |

**Begründung Harvester-Wert ~300 AE/Ladung:** Bei Standardstart 1.000 AE (Zahlengerüst) ermöglicht ein Harvester-Trip alle ~60–75 s einen weiteren Light Tank bzw. halben Battle Tank – passt zur Ziel-Matchdauer 20–35 min (D-010) und macht Harvester-Raids zum wirksamen, aber nicht sofort entscheidenden Druckmittel. Überernte-Details (dauerhafter Schaden am Mutterkristall, D-010) werden nicht hier, sondern in [Resources](./Resources.md) und [Economy](./Economy.md) geregelt; die Harvester-Fähigkeit „Überernte-Warnung" ist nur der UI-Hinweis darauf.

## Allianz (Azurblau/Stahlgrau – High-Tech, präzise, teuer)

| Typ | Name | Tier | Kosten (AE) | Bauzeit (s) | HP | Panzerung | Schaden | DPS | Reichweite (Felder ≙ m) | Tempo (m/s) | Fähigkeiten | Voraussetzungen |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Scout | Jackal-Aufklärer | 1 | 300 | 12 | 220 | Leicht | Kinetisch | 15 | 8 | 9 | Zielmarkierung (+10 % Trefferchance Verband, 8 s) | Fahrzeugfabrik |
| APC | Guardian-Transporter | 1 | 500 | 18 | 450 | Leicht | Kinetisch | 20 | 7 | 7 | 6 Transportsitze, Schnell-Entladung | Fahrzeugfabrik |
| Light Tank | Lynx | 1 | 600 | 20 | 550 | Leicht | Energie | 35 | 9 | 7 | – | Fahrzeugfabrik |
| Battle Tank | Aegis | 2 | 900 | 28 | 1.100 | Schwer | Energie | 60 | 10 | 5,5 | Präzisionsmodus (stationär: +25 % Reichweite/Schaden) | + Forschungslabor |
| Heavy Tank | Juggernaut | 3 | 1.400 | 40 | 2.000 | Schwer | Energie | 90 | 10 | 4 | Frontschild (−30 % Schaden frontal) | + Tech Tier 3 |
| Artillery | Longbow | 2 | 1.000 | 32 | 350 | Leicht | Explosiv | 70 | 22 (min. 6) | 4,5 | – | + Forschungslabor |
| Rocket Launcher | Tempest MRLS | 2 | 900 | 30 | 400 | Leicht | Explosiv | 55 (Fläche) | 12 | 5 | Salve vs. Luft möglich (reduziert) | + Forschungslabor |
| Flame Tank | Plasma-Ätzstrahler „Kauter" | 2 | 850 | 28 | 800 | Schwer | Flamme (Plasma) | 65 | 6 | 5 | Entzündet Vegetation/Gebäude (DoT) | + Forschungslabor |
| Repair Vehicle | Feldschmiede "Aeskulap" | 1 | 600 | 22 | 400 | Leicht | – | – | – | 6 | Repariert 40 HP/s (ein Ziel) | Fahrzeugfabrik |
| Radar Vehicle | Argus | 2 | 700 | 25 | 350 | Leicht | – | – | – | 5,5 | Sicht VIS-SCOUT ([FogOfWar](./FogOfWar.md)), Detektor (enttarnt Getarntes in Sichtweite) | + Radar |
| Harvester | Sammler "Demeter" | 1 | 700 | 25 | 800 | Schwer | – | – | – | 5 | Ladung 300 AE, Überernte-Warnung (D-010) | Raffinerie |
| Builder | Pionier "Atlas" | 1 | 800 | 30 | 350 | Leicht | – | – | – | 5 | Errichtet alle 12 Gebäudetypen | HQ |

## Legion (Rostrot/Ocker – Masse statt Klasse, günstig)

| Typ | Name | Tier | Kosten (AE) | Bauzeit (s) | HP | Panzerung | Schaden | DPS | Reichweite (Felder ≙ m) | Tempo (m/s) | Fähigkeiten | Voraussetzungen |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Scout | Hyäne (Buggy) | 1 | 220 | 9 | 180 | Leicht | Kinetisch | 12 | 7 | 9,5 | Molotov-Wurf (kleiner Flächen-DoT) | Fahrzeugfabrik |
| APC | Rammbock | 1 | 400 | 14 | 550 | Leicht | Kinetisch | 15 | 7 | 6,5 | 6 Sitze, Ramme (Schaden beim Aufprall) | Fahrzeugfabrik |
| Light Tank | Räuber | 1 | 450 | 15 | 480 | Leicht | Kinetisch | 28 | 8 | 7 | – | Fahrzeugfabrik |
| Battle Tank | Koloss | 2 | 700 | 22 | 1.250 | Schwer | Explosiv | 50 | 8 | 5 | – | + Forschungslabor |
| Heavy Tank | Behemoth | 3 | 1.100 | 32 | 2.300 | Schwer | Explosiv | 75 | 9 | 3,5 | Doppelrohr-Salve (alle 20 s, Burst) | + Tech Tier 3 |
| Artillery | Donnerkanone | 2 | 800 | 26 | 320 | Leicht | Explosiv | 60 | 18 (min. 6) | 4 | Streufeuer (größere Fläche, weniger Präzision) | + Forschungslabor |
| Rocket Launcher | Hagelsturm | 2 | 700 | 24 | 380 | Leicht | Explosiv | 70 (Fläche) | 10 | 5 | Volle Salve (Nachladezeit 12 s) | + Forschungslabor |
| Flame Tank | Inferno | 2 | 650 | 20 | 950 | Schwer | Flamme | 85 | 5 | 5 | Doppelflakonstrahl, entzündet Vegetation/Gebäude | + Forschungslabor |
| Repair Vehicle | Flickschuster | 1 | 450 | 16 | 380 | Leicht | – | – | – | 6 | Repariert 30 HP/s (Fläche, mehrere Ziele) | Fahrzeugfabrik |
| Radar Vehicle | Lauscher | 2 | 550 | 20 | 320 | Leicht | – | – | – | 5,5 | Sicht VIS-SCOUT ([FogOfWar](./FogOfWar.md)), Detektor (in Sichtweite), Störsender (Feind-Radar −20 % Reichweite, 15 s) | + Radar |
| Harvester | Schürfer | 1 | 550 | 20 | 750 | Schwer | – | – | – | 5 | Ladung 300 AE | Raffinerie |
| Builder | Vorarbeiter | 1 | 650 | 24 | 320 | Leicht | – | – | – | 5 | Errichtet alle 12 Gebäudetypen | HQ |

## Evolvierte (Violett/Bio-Grün – biologisch, Regeneration)

Alle Evolvierten-Fahrzeuge sind lebende Organismen/Kristallsymbiose: Regeneration ~1 % HP/s nach 5 s kampffrei (statt Reparatur). Namen sind Arbeitsnamen für den biologischen Look.

| Typ | Name | Tier | Kosten (AE) | Bauzeit (s) | HP | Panzerung | Schaden | DPS | Reichweite (Felder ≙ m) | Tempo (m/s) | Fähigkeiten | Voraussetzungen |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Scout | Flinkläufer | 1 | 260 | 11 | 200 | Leicht | Bio | 14 | 7 | 9 | Tarnt sich in Aetherium-Feldern (stehend) | Fahrzeugfabrik |
| APC | Brutträger | 1 | 450 | 16 | 500 | Leicht | Bio | 18 | 7 | 6,5 | 6 Sitze; heilt Insassen 2 % HP/s | Fahrzeugfabrik |
| Light Tank | Stachelreiter | 1 | 520 | 17 | 520 | Leicht | Bio | 32 | 9 | 7 | Stachelhagel (kleiner Flächenschaden) | Fahrzeugfabrik |
| Battle Tank | Panzerbestie | 2 | 800 | 25 | 1.150 | Schwer | Bio | 55 | 9 | 5 | Kristallpanzer (+20 % Panzerung bei vollem HP) | + Forschungslabor |
| Heavy Tank | Urtier | 3 | 1.250 | 36 | 2.100 | Schwer | Bio | 85 | 10 | 4 | Berserker (+30 % DPS unter 30 % HP) | + Tech Tier 3 |
| Artillery | Säurespeier | 2 | 900 | 28 | 330 | Leicht | Bio (Säure) | 65 | 20 (min. 6) | 4,5 | Säurepfütze (DoT-Zone, verweigert Gelände) | + Forschungslabor |
| Rocket Launcher | Sporenwerfer | 2 | 800 | 27 | 390 | Leicht | Explosiv (Sporen) | 60 (Fläche) | 11 | 5 | Sporenwolke verlangsamt Gegner 20 % | + Forschungslabor |
| Flame Tank | Bio-Plasma-Käfer | 2 | 750 | 24 | 880 | Schwer | Flamme (Bio-Plasma) | 75 | 6 | 5 | Entzündet Vegetation/Gebäude (DoT) | + Forschungslabor |
| Repair Vehicle | Symbiont | 1 | 520 | 18 | 420 | Leicht | – | – | – | 6 | Heilt 35 HP/s; verdoppelt Regeneration des Ziels | Fahrzeugfabrik |
| Radar Vehicle | Sensorpolyp | 2 | 620 | 22 | 340 | Leicht | – | – | – | 5,5 | Sicht VIS-SCOUT ([FogOfWar](./FogOfWar.md)); spürt getarnte Einheiten in Sichtweite auf | + Radar |
| Harvester | Schlürfer | 1 | 620 | 22 | 780 | Schwer | – | – | – | 5 | Ladung 300 AE; +10 % Abbau in Aetherium-Nähe | Raffinerie |
| Builder | Keimweber | 1 | 720 | 26 | 340 | Leicht | – | – | – | 5 | Pflanzt Gebäude-Keime (D-011); Keim reift schneller in Aetherium-Nähe | HQ |

## Drohnen (gemäß D-014)

Produktion in bestehenden Fabriken (Fahrzeugfabrik; keine neue Gebäudeanforderung). Drohnen sind günstig, fragil, schweben bodennah (kein Luft-Layer, kein Flak-Konter). Pro Fraktion 3 Typen. Kein hartes Stückzahl-Limit – die Begrenzung erfolgt über Kosten und Fragilität (D-014-Klarstellung, Korrekturlauf Sprint 2):

| Fraktion | Scout-Drohne | Repair-Drohne | Kampf-Drohne |
|---|---|---|---|
| Allianz | "Spähkugel" – 150 AE, 8 s, 120 HP, Sicht VIS-SCOUT ([FogOfWar](./FogOfWar.md)), Tarnung | "Nadel" – 200 AE, 10 s, 100 HP, repariert 15 HP/s | "Hornet" – 250 AE, 12 s, 150 HP, Energie 12 DPS |
| Legion | "Motte" – 120 AE, 6 s, 100 HP, Sicht VIS-SCOUT ([FogOfWar](./FogOfWar.md)) | "Schrauber" – 160 AE, 8 s, 90 HP, repariert 12 HP/s (Fläche) | "Wespe" – 200 AE, 10 s, 130 HP, Kinetisch 10 DPS |
| Evolvierte (Bio) | "Sporenauge" – 140 AE, 7 s, 110 HP, Sicht VIS-SCOUT ([FogOfWar](./FogOfWar.md)), tarnt in Feldern | "Heilpolyp" – 180 AE, 9 s, 95 HP, heilt 14 HP/s + Regen-Boost | "Stachelschwarm" – 220 AE, 11 s, 140 HP, Bio 11 DPS |

**Begründung:** Drohnen glätten die Frühspiel-Aufklärung (H1-Zielgruppe, Solo/Skirmish), ohne den Fahrzeug-Mix zu verwässern; ihre niedrigen HP machen sie zu Wegwerf-Werkzeugen, nicht zu Kampfeinheiten.

## Elite-Einheiten (gemäß D-015)

MVP/Alpha: 1 Elite pro Fraktion, Tier 3, **hartes Limit 1 gleichzeitig**; ab Release: 3 Elites pro Fraktion, hartes Limit 2 gleichzeitig (D-015). Das Elite-Limit ist das einzige harte Stückzahl-Limit im Spiel (D-021: kein sonstiges Pop-/Supply-Limit). Kosten-Richtwert 2.500–3.500 AE, Bauzeit 60–90 s – als spätes Machtwort, nicht als früher Rush. Elites zählen als Signature-Assets (TPD §7.2, eigene Entwicklung).

| Fraktion | Elite | Kosten (AE) | Bauzeit (s) | HP | Panzerung | DPS | Reichweite (Felder ≙ m) | Tempo (m/s) | Fähigkeiten | Voraussetzungen |
|---|---|---|---|---|---|---|---|---|---|---|
| Allianz | Titan-Mech | 3.200 | 80 | 6.000 | Schwer | 220 (Energie, Zwillingsgeschütze) | 12 | 4 | Schildkuppel (10 s, absorbiert 1.500 Schaden, CD 45 s); Bodentrampeln (Flächen-Stun) | Fahrzeugfabrik + Forschungslabor + Tier 3 |
| Legion | Mobile Festung | 2.800 | 75 | 8.000 | Schwer | 160 (Vierfach-Mörser, Explosiv-Fläche) | 24 | 3 | Belagerungsmodus (stationär: +50 % Reichweite); 4 Infanterie-Schützenstände | Fahrzeugfabrik + Forschungslabor + Tier 3 |
| Evolvierte | Alpha-Mutant | 3.000 | 70 | 7.000 | Schwer | 200 (Bio, Prankenhiebe Nahkampf + Säurespeichel 8) | 3 | 4,5 | Verschlingen (tötet Fahrzeug <500 HP sofort, heilt 500 HP); Panik-Schrei (Feinde fliehen 4 s, CD 60 s) | Brutkammer (Evolvierte-Kaserne, D-011) + Forschungslabor + Tier 3 |

**Konter-Regel:** Elites verlieren gegen konzentriertes Tier-2/3-Feuer (Artillerie-Fokus, Bomber-Staffeln, Superwaffe) – sie sind Tempo-Druck, keine "Ich-gewinne"-Taste. Limit 1 verhindert Stapelung; der Verlust ist spürbar teuer (Rebuild-Zeit ~1,5 min).

**Alpha-Mutant – führende Definition (Korrekturlauf Sprint 4, Review F-04):** Die hier stehenden Elite-Werte (3.000 AE, 7.000 HP, ~200-DPS-Klasse, Limit 1 MVP / 2 Release gemäß D-015) sind **verbindlich**; [Infantry](./Infantry.md) wird auf einen Verweis auf diese Tabelle reduziert (Auflösung der Doppeldefinition). Der Alpha-Mutant ist die Infanterie-Elite der Evolvierten (D-027) und wird in der **Brutkammer** (Evolvierte-Kaserne, Bio-Äquivalent, D-011) produziert – nicht in der Fahrzeugfabrik. Waffen-Rahmen (Alpha-Pranken) siehe [Weapons](./Weapons.md).

## Offene Punkte

Derzeit keine unentschiedenen Punkte. Stand nach Korrekturlauf Sprint 2 und Sprint 4:

- Flame-Tank-Tonalität: **entschieden** (Game Director, Korrekturlauf Sprint 2) – fraktionsspezifische Namen: Allianz „Plasma-Ätzstrahler", Evolvierte „Bio-Plasma-Käfer"; der Typ bleibt regelmechanisch einer der 12 Typen.
- Evolvierte-Fahrzeugproduktion: **entschieden** – konventionelle Fabrikproduktion mit Bio-Optik (Schlüpfen); D-011 (Wachstum) gilt nur für Gebäude.
- Drohnen-Stückzahl: **entschieden (D-014-Klarstellung)** – 3 Typen pro Fraktion, kein hartes Limit, Begrenzung über Kosten/Fragilität.
- Radar-Fahrzeug-Feuerleitung: **entschieden (D-026)** – Verbandsmechanik gestrichen; Radar-Fahrzeug = mobiler Radar + Detektor.
- Harvester-Überernte: **keine eigenen Regeln hier** – Details in [Resources](./Resources.md) und [Economy](./Economy.md) (D-010).
- Reichweiten-/Sichtweiten-Harmonisierung: **entschieden (D-047, Korrekturlauf Sprint 4)** – 1 Grid-Feld ≙ 1 m; [Weapons](./Weapons.md) führend für Waffenwerte (z. B. Artillerie 18–22 statt 80–85, Panzerkanonen 8–10), [FogOfWar](./FogOfWar.md) führend für Sichtklassen; alle Reichweiten-/Sicht-Spalten hier angeglichen bzw. durch Verweise ersetzt.
- Alpha-Mutant-Doppeldefinition: **historisch entschieden, durch D-056 aus
  MS-1 verschoben** – Werte und Produktionsweg bleiben Post-MVP-Zielbild.
- Schadens-Konter-Matrix: **entschieden (D-074)** – die lokale 5 × 4-Tabelle war
  gegenüber [ArmorSystem](./ArmorSystem.md) abgedriftet und ist aufgehoben;
  führend ist ausschließlich die dortige 6 × 6-Matrix. D-074 wurde vom Agenten
  unter ausdrücklicher Inhaber-Delegation entschieden und ist überstimmbar.

## Nächste Schritte

1. Review durch Game Director und Lead Systems Design (Konter-Matrix, Kostenkurve).
2. Werte-Export als ScriptableObject-Datensätze (Unity `6000.5.4f1`, Revision `d550df8bd089`, flache Struktur oben) für das Balancing-Tool.
3. Abstimmung mit [Economy](./Economy.md): Harvester-Zykluszeiten gegen Ziel-Matchdauer 20–35 min simulieren.
4. Abstimmung mit [Aircraft](./Aircraft.md): gemeinsame Schadens-/Panzerungstypen finalisieren.
5. Erste Playtest-Iteration (Skirmish 1v1 vs. KI, D-018) mit Allianz vs. Legion zur Kalibrierung der ±20 %-Kostenkurve.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Vehicle Artist / Lead Gameplay Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Lead Vehicle Artist / Lead Gameplay Designer |
| 0.3.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings) | Lead Vehicle Artist / Lead Gameplay Designer |
| 0.4.0 | 2026-07-24 | Sechs MS-1-Fahrzeugrollen, Harvester-Kapazitäten und deaktivierte Fähigkeiten gemäß D-056 festgelegt | Lead Vehicle Artist / Lead Gameplay Designer |
| 0.5.0 | 2026-07-26 | Lokale 5 × 4-Schadensmatrix nach D-074 aufgehoben und durch Verweis auf [ArmorSystem](./ArmorSystem.md) ersetzt; Panzerungsachse auf die sechs Klassen der führenden Quelle umgestellt (Leichter Panzer und Kampfpanzer beide `Medium`). Fahrzeugtabellen, Drohnen und Elite-Werte unverändert | Agent (unter Inhaber-Delegation, D-074) |
