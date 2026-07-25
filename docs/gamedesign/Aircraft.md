# Luftfahrzeuge (Aircraft)

**Version:** 0.4.0 | **Status:** Post-MVP-Zielbild | **Verantwortungsbereich:** Lead Vehicle Artist / Lead Gameplay Designer | **Sprint:** 4

## Zweck

Spezifikation aller Luftfahrzeuge für die drei Fraktionen (Allianz, Legion, Evolvierte): 7 Typen pro Fraktion gemäß Zahlengerüst, inklusive Luftkampf-Regeln (Air-to-Air vs. Air-to-Ground, Flak-Konter), Munitions-/Betankungsregeln und Landeplatz-Logik am Flugfeld. Alle Werte sind Startwerte v0.3 zum Tunen – Richtwerte mit Begründung, keine Pseudo-Präzision. Leitprinzip: Lufteinheiten sind teure, gläserne Präzisionswerkzeuge – Flak ist der harte Konter, kein Spieler soll ohne Luftabwehr gegen Luft verlieren können, aber auch kein Luft-only-Rush dominieren.

## Abhängigkeiten

- [DecisionLog](../production/DecisionLog.md) – D-008 (Flugfeld als Gebäudetyp, Verteidigungsplattform mit Flak-Modul), D-012 (Zerstörbarkeit), D-013 (keine Marine), D-018 (Modi-Staffelung), D-026 (Evolvierte-Luft-Spezialeinheit, Konter-Lücken), D-034/D-047 (1 Grid-Feld ≙ 1 m, Weapons.md führend für Reichweiten)
- [Factions](./Factions.md) – Fraktionsidentitäten
- [Buildings](./Buildings.md) – Flugfeld, Verteidigungsplattform (Flak-Modul), Radar
- [Vehicles](./Vehicles.md) – gemeinsame Schadens-/Panzerungstypen, Rocket Launcher als Boden-Flak-Ergänzung
- [Weapons](./Weapons.md) – **führende Quelle** für Waffenwerte (Reichweite, Schaden, Abklingzeit, Flak-Modul), D-047
- [FogOfWar](./FogOfWar.md) – Sichtweiten-Klassen (VIS-*), führend für Sichtwerte (D-047)
- [ResearchTree](./ResearchTree.md) – Tier-1–3-Freischaltungen
- [Economy](./Economy.md) – Währung AE
- [FogOfWar-Research](../research/FogOfWar.md) – Sicht-/Aufklärungsregeln für Luft-Scouting
- [KnowledgeBase](../analysis/KnowledgeBase.md) – Asset-Umfang 21 Luftfahrzeuge (APL Paket 06)

## MS-1-Override (D-056)

Luftfahrzeuge, Flugfeld, Flak und alle luftbezogenen Fähigkeiten sind
vollständig Post-MVP. Keine Definition dieses Dokuments ist ein
G0–G5-Abnahmekriterium. MS-1 umfasst ausschließlich die Bodeneinheiten aus
[MVPContentManifest.md](../production/MVPContentManifest.md).

## Datenmodell (ScriptableObject-tauglich)

Flacher Datensatz pro Einheit – erweitert das Fahrzeug-Schema aus [Vehicles](./Vehicles.md):

| Feld | Typ | Beschreibung |
|---|---|---|
| `id` | string | Eindeutige ID, z. B. `ALL_AIR_FIGHTER` |
| `faction` | enum | Allianz / Legion / Evolvierte |
| `aircraftClass` | enum | Einer der 7 Typen |
| `tier` | int | 1–3 |
| `costAE` | int | Kosten in AE |
| `buildTimeS` | float | Bauzeit in Sekunden |
| `hp` | int | Trefferpunkte (alle Luftfahrzeuge: Panzerungstyp "Leicht" außer Heavy Gunship/Spezial = "Schwer") |
| `damageType` | enum | Kinetisch / Explosiv / Flamme / Energie / Bio |
| `dpsAir` | float | Schaden vs. Luft (0 = kein Air-to-Air) |
| `dpsGround` | float | Schaden vs. Boden |
| `rangeM` | float | Angriffsreichweite in Grid-Feldern (1 Feld ≙ 1 m, D-034/D-047); verbindliche Werte in [Weapons](./Weapons.md) |
| `speedMS` | float | Fluggeschwindigkeit (typisch 12–20 m/s) |
| `sightM` | float | Sichtweite; Werte nur als VIS-Klassen in [FogOfWar](./FogOfWar.md) (D-047) |
| `flightModel` | enum | FixedWing / VTOL (siehe Landeplatz-Regeln) |
| `ammoCount` | int | Munition vor Rückkehr (−1 = unbegrenzt) |
| `prerequisites` | string[] | Gebäude-/Tech-Voraussetzungen |
| `abilities` | string[] | Fähigkeiten |

## Luftkampf-Regeln

**Air-to-Air vs. Air-to-Ground:**

- Nur der **Fighter** ist ein dedizierter Luftüberlegenheitsjäger (hoher `dpsAir`, kein/nur symbolischer Bodenschaden). Er kontert Gunships, Bomber und Transporter hart.
- **Gunships** (inkl. Heavy Gunship) sind Air-to-Ground-Plattformen: kein Air-to-Air, dafür anhaltender Bodenschaden; gekontert von Fighter und Flak.
- **Bomber** liefern hohen Einmal-Schaden gegen Gebäude/Verbände, sind wehrlos gegen Luft.
- **Scout** ist unbewaffnet (Ausnahme: Evolvierte, siehe Tabelle).
- **Spezialeinheit** je Fraktion mit eigenem Profil (siehe Tabellen).

**Flak als harter Konter:**

- Verteidigungsplattform mit **Flak-Modul** (D-008) ist die primäre Luftabwehr: Reichweite 11–12 Felder (≙ m, D-047), 25–40 Schaden pro Schuss bei 1,5 s Abklingzeit, kleiner Flächenradius 1 (kontert Schwärme) – verbindliche Werte in [Weapons](./Weapons.md).
- Rocket Launcher (Fahrzeug) und Kampf-Drohne (leicht) liefern mobile, schwächere Ergänzung; für die Evolvierten zusätzlich der Kristallmagier (Infanterie, Zielklasse „Beides", D-026 – siehe [Infantry](./Infantry.md)).
- Flak-Schaden gegen Luft erhält Multiplikator ×2,0 auf den Basiswert (25–40 Schaden / 1,5 s ≙ effektiv ~33–53 DPS vs. Luft) – Lufteinheiten sind fragil genug, dass 2–3 Flak-Plattformen eine Basis dicht machen, aber Anflug-Routen über Radar-Sicht planbar bleiben (Radar deckt Flak-Positionen auf).

**Verweis-Regel (D-047, Korrekturlauf Sprint 4):** 1 Grid-Feld ≙ 1 m (D-034/D-047). Verbindliche Waffenwerte (Reichweite, Schaden, Abklingzeit) existieren genau einmal – in [Weapons](./Weapons.md) (führend); verbindliche Sichtweiten nur als VIS-Klassen in [FogOfWar](./FogOfWar.md). Diese Datei definiert Rollen, Konter, Munitions-/Landeplatz-Regeln und Einheiten-Rahmenwerte (HP, Kosten, Tempo); die Reichweiten-Spalten folgen den Weapons.md-Korridoren. Grundsatz: Angriffsreichweite > Sichtweite ist Design-Prinzip (Scouting/Spotter, D-047), kein Fehler.

**Munition und Betankung (Entscheidung, begründet):**

- **Bomber und Spezial-Bomber erhalten Munition** (`ammoCount` 1–4 Abwürfe), danach automatische Rückkehr zum Flugfeld und ~10 s Nachladen. Begründung: Ohne Munitionslimit kann ein Bomber-Verband eine Basis endlos und ohne Gegenspiel belagern; das Limit macht Flugfelder strategisch wertvoll (Nachlade-Hub), schafft Angriffsfenster und erzwingt Bomber-Eskorte statt Dauerbeschuss.
- **Fighter, Gunships, Scouts erhalten keine Munition** (Dauerfeuer). Begründung: Mikro-Last (Nachlade-Management für 4–6 Einheitentypen) würde die H1-Zielgruppe (D-007, Solo/Skirmish) überfordern; Dauerfeuer-Gunships sind durch Flak bereits ausreichend gekontert.
- **Keine Treibstoff-/Betankungsregel.** Begründung: Globaler Treibstoffverbrauch erzeugt Frustration ohne taktischen Mehrwert (Einheiten "vergessen" zu tanken) und widerspricht dem Ziel flüssiger 20–35-Minuten-Matches (D-010). Die Munitionsregel für Bomber deckt das Balance-Problem bereits ab.

## Landeplatz-Regeln am Flugfeld

- **Flugfeld hat 4 Landebuchten** (bestätigt im Korrekturlauf Sprint 2). Jedes FixedWing-Luftfahrzeug (Fighter, Bomber, Transport, Spezial) belegt dauerhaft eine Bucht am produzierenden Flugfeld; die Bucht ist Produktions- und Nachladeort.
- **VTOL-Einheiten (Scout, Gunship, Heavy Gunship) schweben frei**, benötigen keine Bucht und können über dem Gelände stationiert werden (typischer RTS-Helikopter).
- **Buchtenlimit = Bau-Limit:** Ohne freie Bucht kann kein weiteres FixedWing-Flugzeug produziert werden → Flugfelder werden zum Engpass und zum lohnenden Angriffsziel.
- **Verlust des Heimat-Flugfelds:** Betroffene FixedWing-Flugzeuge suchen automatisch eine freie Bucht am nächsten eigenen Flugfeld. Gibt es keine, können sie weiter fliegen und kämpfen, aber **nicht nachladen**; Bomber ohne Munition gelten als kampfunfähig und kreisen über dem eigenen HQ. Diese milde Variante ist bestätigt (Korrekturlauf Sprint 2): kein Absturz, kein Nachladen ohne freie Bucht.
- **Bucht-Bauzeit:** Zerstörtes Flugzeug gibt seine Bucht sofort frei; Ersatzproduktion startet sofort.

## Die 7 Typen – Rollen und Konter

| # | Typ | Flugmodell | Rolle | Konter gegen | Gekontert von |
|---|---|---|---|---|---|
| 1 | Scout | VTOL | Schnelle Aufklärung, große Sicht | – | Flak, Fighter |
| 2 | Gunship | VTOL | Anhaltender Air-to-Ground-Schaden vs. Leicht/Infanterie | Infanterie, Light Tank, Harvester | Flak, Fighter, Battle Tank (Flakwerte nein – nur Flak/Fighter) |
| 3 | Fighter | FixedWing | Luftüberlegenheit | Gunship, Bomber, Transport, Scout | Flak (vorsichtig anfliegen), Radar-Frühwarnung |
| 4 | Bomber | FixedWing | Hoher Einmal-Schaden vs. Gebäude/Verbände (Munition!) | Gebäude, Verteidigung, massierte Panzer | Fighter, Flak |
| 5 | Transport | FixedWing | Luftlandung: 8 Infanterie oder 1 leichtes Fahrzeug | – (Strategisch: Umgehung von Fronten) | Flak, Fighter – Verlust killt Fracht |
| 6 | Heavy Gunship | VTOL | Tier-3 Air-to-Ground-Plattform vs. Schwer/Gebäude | Battle Tank, Gebäude | Flak (konzentriert), Fighter-Staffel |
| 7 | Spezialeinheit | je Fraktion | Fraktions-Identität in der Luft (Tier 3) | siehe Tabellen | siehe Tabellen |

## Allianz (Azurblau/Stahlgrau – präzise, teuer)

| Typ | Name | Tier | Kosten (AE) | Bauzeit (s) | HP | DPS Luft | DPS Boden | Reichweite (Felder ≙ m) | Tempo (m/s) | Munition | Fähigkeiten | Voraussetzungen |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Scout | Falke | 1 | 400 | 15 | 250 | – | – | – | 20 | – | Sicht VIS-SCOUT ([FogOfWar](./FogOfWar.md)), Zielmarkierung | Flugfeld |
| Gunship | Valkyrie | 2 | 900 | 28 | 600 | – | 55 (Energie) | 9 | 14 | – | Präzisionssalve (+Schaden vs. Leicht) | + Forschungslabor |
| Fighter | Blitz-Abfangjäger | 2 | 1.000 | 30 | 450 | 85 (Energie) | – | 10 | 20 | – | Nachbrenner (+50 % Tempo, 5 s) | + Forschungslabor |
| Bomber | Hammer | 2 | 1.200 | 35 | 550 | – | 400/Abwurf (Explosiv) | Abwurf | 16 | 3 | Bunkerbrecher (+50 % vs. Gebäude) | + Forschungslabor |
| Transport | Albatros | 1 | 800 | 25 | 700 | – | – | – | 15 | – | 8 Infanterie oder 1 leichtes Fahrzeug | Flugfeld |
| Heavy Gunship | Sturmvogel | 3 | 1.800 | 45 | 1.200 | – | 110 (Energie) | 9 | 12 | – | Schwerer Schild (absorbiert 800, CD 60 s) | + Tier 3 |
| Spezial | EMP-Sturmjäger "Nemesis" | 3 | 2.000 | 50 | 500 | 40 | 150 (Energie) | 10 | 19 | 1 | EMP-Abwurf: feindliche Gebäude/Fahrzeuge im Radius 8 s deaktiviert (Produktion/Verteidigung offline) | + Tier 3 |

**Begründung Spezial:** Passt zur Low-Power-Regel des Zahlengerüsts (Energie-Thema) und zur Präzisions-Identität: zeitkritisches Ausschalten statt Flächenzerstörung.

## Legion (Rostrot/Ocker – günstig, Masse)

| Typ | Name | Tier | Kosten (AE) | Bauzeit (s) | HP | DPS Luft | DPS Boden | Reichweite (Felder ≙ m) | Tempo (m/s) | Munition | Fähigkeiten | Voraussetzungen |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Scout | Fledermaus | 1 | 300 | 12 | 220 | – | – | – | 20 | – | Sicht VIS-SCOUT ([FogOfWar](./FogOfWar.md)), Störpeil (Radar-Verwirrung, 10 s) | Flugfeld |
| Gunship | Hornisse | 2 | 700 | 22 | 550 | – | 45 (Kinetisch) | 8 | 14 | – | Raketenpod-Salve (kleiner Flächenburst) | + Forschungslabor |
| Fighter | Raubvogel | 2 | 800 | 24 | 420 | 75 (Kinetisch) | – | 9 | 20 | – | – | + Forschungslabor |
| Bomber | Teppichbomber "Vulkan" | 2 | 1.000 | 28 | 650 | – | 250/Abwurf (Explosiv, große Fläche) | Abwurf | 14 | 4 | Teppichabwurf (Linie statt Punkt) | + Forschungslabor |
| Transport | Lastenesel | 1 | 650 | 20 | 800 | – | – | – | 13 | – | 8 Infanterie oder 1 leichtes Fahrzeug; Fallschirm-Abwurf (kein Landen nötig) | Flugfeld |
| Heavy Gunship | Fliegende Festung | 3 | 1.500 | 38 | 1.500 | – | 90 (Explosiv, Vierfachrohre) | 9 | 10 | – | Zusatzpanzerung (−20 % Flak-Schaden) | + Tier 3 |
| Spezial | Napalm-Schleuder "Phönix" | 3 | 1.700 | 42 | 600 | – | 350/Abwurf (Flamme) | Abwurf | 15 | 2 | Napalm-Teppich: entzündet Vegetation/Gebäude großflächig (D-012), DoT-Zone 15 s | + Tier 3 |

**Begründung Spezial:** Flächen-Flamme ist Legions Kernidentität; der Brand setzt Gelände außer Gefecht (Vegetation/Dekor brennbar, D-012) und bestraft statisches Verteidigen – Kontrast zur präzisen Allianz-EMP.

## Evolvierte (Violett/Bio-Grün – lebende Flieger, Regeneration)

Alle Evolvierten-Flieger sind Kreaturen: Regeneration ~1 % HP/s nach 5 s kampffrei; Landebuchten des Flugfelds sind Nistkammern (Regel identisch).

| Typ | Name | Tier | Kosten (AE) | Bauzeit (s) | HP | DPS Luft | DPS Boden | Reichweite (Felder ≙ m) | Tempo (m/s) | Munition | Fähigkeiten | Voraussetzungen |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Scout | Schwingenspäher | 1 | 350 | 13 | 240 | 20 (Bio, Notwehr) | – | 7 | 19 | – | Sicht VIS-SCOUT ([FogOfWar](./FogOfWar.md)); tarnt über Aetherium-Feldern (schwebend) | Flugfeld |
| Gunship | Stachelschwarm-Träger | 2 | 800 | 25 | 580 | – | 50 (Bio, Stachelhagel-Fläche) | 8 | 13 | – | Verlangsamende Sporen (−20 % Tempo Ziel, 4 s) | + Forschungslabor |
| Fighter | Sturmklaue | 2 | 900 | 27 | 440 | 80 (Bio, Klauen+Nahdistanz) | – | 8 | 21 | – | Sturzflug (+Burst-Schaden beim Anflug) | + Forschungslabor |
| Bomber | Sporenregen-Bringer | 2 | 1.100 | 31 | 600 | – | 320/Abwurf (Bio-Säure) | Abwurf | 15 | 3 | Säurepfütze (DoT-Zone, verweigert Gelände 12 s) | + Forschungslabor |
| Transport | Flugbrut | 1 | 720 | 22 | 750 | – | – | – | 14 | – | 8 Infanterie oder 1 leichtes Fahrzeug; heilt Fracht 2 % HP/s | Flugfeld |
| Heavy Gunship | Himmelsleviathan | 3 | 1.650 | 42 | 1.350 | – | 100 (Bio) | 9 | 11 | – | Kristallhaut (+25 % Panzerung bei vollem HP); wirft Bio-Brut ab (2 Nahkampf-Kreaturen beim Tod) | + Tier 3 |
| Spezial | Säure-Bomberin „Siechschwinge" | 3 | 1.850 | 46 | 700 | – | 220/Abwurf (Bio-Säure) | Abwurf | 16 | 2 | Konzentrierter Säureabwurf: hoher DoT vs. Schwer/Gebäude, Säurepfütze verweigert Gelände 15 s | + Tier 3 |

**Historischer Vollspielentwurf:** Die Säure-Bomberin war als früheste
Spezialeinheit der Evolvierten vorgesehen (D-026). D-056 verschiebt sie und
die Parasiten-Königin vollständig hinter MS-1.

## Balance-Leitplanken (fraktionsübergreifend)

- Luftkosten liegen ~30–50 % über vergleichbaren Bodenwerten; Luft-only ist bei 1.000 AE Start nicht rush-fähig (frühestes sinnvolles Luftfenster ~Min. 6–8).
- Kein Luftfahrzeug übersteht den Durchflug über 2 Flak-Plattformen (außer Heavy Gunship/Spezial mit Fähigkeit) – Flak-Dichte entscheidet, nicht Flak-DPS-Race.
- Fighter gewinnt 1v1 gegen jede Nicht-Fighter-Lufteinheit in unter 8 s – Luftüberlegenheit ist binär genug, um lesbar zu bleiben (H1-Zielgruppe).
- Radar (Gebäude oder Radar-Fahrzeug) ist für rechtzeitige Flak-Reaktion Pflicht – verzahnt Luftspiel mit dem Radar-Ökosystem aus [Buildings](./Buildings.md).

## Offene Punkte

- Flak-Modul-Werte: **entschieden (D-047)** – [Weapons](./Weapons.md) ist die einzige Werte-Quelle (Flak: 11–12 Felder ≙ m, 25–40 Schaden / 1,5 s, ×2,0 vs. Luft); Aircraft.md ist angeglichen, die Spiegelung in [Buildings](./Buildings.md) erfolgt im selben Korrekturlauf durch den Buildings-Owner (kein Design-Konflikt mehr).
- Reichweiten-/Sichtweiten-Harmonisierung: **entschieden (D-047)** – alle Angriffsreichweiten auf Grid-Felder (1 Feld ≙ 1 m) nach den Weapons.md-Korridoren umgestellt; Sicht-Einzelwerte durch VIS-Klassen-Verweise auf [FogOfWar](./FogOfWar.md) ersetzt.

Entschieden im Korrekturlauf Sprint 2 (aus der Liste entfernt):

- Verlust des Heimat-Flugfelds: milde Variante bestätigt – weiterfliegen, kein Nachladen ohne freie Bucht, kein Absturz.
- Landebuchten: 4 pro Flugfeld im Post-MVP-Zielbild.
- Evolvierte-Scout-Notwehr: als Bio-Asymmetrie akzeptiert (bleibt einziger bewaffneter Scout).
- Parasiten-Königin: **durch D-056 verschoben** – gemeinsam mit der
  Säure-Bomberin Post-MVP.

## Nächste Schritte

1. Review durch Game Director und Lead Systems Design (Luftkampf-Regeln, Buchten-Logik).
2. Verbleibende Verweis-Aufräumarbeiten nach D-047: [Buildings](./Buildings.md) (Flak-Modul) und [Vehicles](./Vehicles.md) (Rocket-Launcher-AA) verweisen auf [Weapons](./Weapons.md) als einzige Werte-Quelle (eine Quelle für die Schadens-Matrix).
3. Flugmodelle (FixedWing-Anflugmuster, VTOL-Schweben) mit dem Pathfinding-Research ([../research/Pathfinding.md](../research/Pathfinding.md)) abstimmen – Höhen-Layer vs. Bodenkollision.
4. Playtest-Iteration: Bomber-Munitionsregel und Flak-Dichte in Skirmish 1v1 vs. KI kalibrieren (D-018).
5. Werte-Export als ScriptableObject-Datensätze für das Balancing-Tool.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Vehicle Artist / Lead Gameplay Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Lead Vehicle Artist / Lead Gameplay Designer |
| 0.3.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings) | Lead Vehicle Artist / Lead Gameplay Designer |
| 0.4.0 | 2026-07-24 | Gesamten Luft-/Flak-Umfang gemäß D-056 als Post-MVP abgegrenzt | Lead Vehicle Artist / Lead Gameplay Designer |
