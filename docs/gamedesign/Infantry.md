# Infanterie – Einheiten-Design (24 Einheiten)

**Version:** 0.6.0 | **Status:** Entwurf – MS-1-Override verbindlich | **Verantwortungsbereich:** Lead Gameplay Designer | **Sprint:** 4

## Zweck

Vollständige, spielbar spezifizierte Infanterie-Roster aller drei Fraktionen (8 Einheiten pro Fraktion gemäß APL Paket 04 = 24 Einheiten). Verbindlich für Balancing (Sprint 4+), Asset-Produktion (Sprint 5) und die KI-Verhaltensdefinition. Alle Werte sind **Startwerte v0.1 zum Tunen** – Richtwerte mit Begründung, keine Pseudo-Präzision. Alle Datensätze sind flach und ScriptableObject-tauglich definiert (TPD §11).

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – insb. D-008 (Gebäudetypen), D-010 (Wirtschaft/Matchdauer), D-011 (Evolvierte-Mechanik), D-015 (Elite-Regel), D-018 (Modi-Staffelung), D-021 (kein Versorgungssystem), D-022 (Capture-Regelwerk), D-026 (Einheiten-Korrekturen), D-027 (Fraktions-Sonderregeln), D-028 (Brücken-Reparatur), D-074 (Konter-Matrix führend in ArmorSystem.md)
- [./ArmorSystem.md](./ArmorSystem.md) – **führende Quelle** für Panzerungsklassen und Schaden-gegen-Panzerung-Multiplikatoren (D-074)
- [../analysis/KnowledgeBase.md](../analysis/KnowledgeBase.md) – Fraktions-Grunddaten
- `RTS_Asset_Pipeline.md` (Projektroot), Paket 04 – verbindliche Einheitennamen
- `./Factions.md`, `./Economy.md`, `./Buildings.md`, `./Vehicles.md`, `./Aircraft.md`, `./ResearchTree.md` (parallel in Sprint 2 in Arbeit) – Zahlengeüst: Währung AE, Start 1.000 AE, Harvester-Ladung ~300 AE, Tech-Tiers 1–3, Low-Power-Regel

## MS-1-Override (D-056)

MS-1 enthält pro aktiver Fraktion genau zwei Infanterierollen:

| Rolle | Allianz | Legion | Tier |
|---|---|---|---|
| Basisinfanterie | Rifleman | Rekrut | T1 |
| Panzerabwehrinfanterie | Rocket Soldier | Raketenschütze | T2 |

Alle übrigen Einträge dieses Vollrosters sind Post-MVP. Frühere aktive
Fähigkeiten dieser vier Einheiten sind deaktiviert; MS-1 führt kein generisches
System für Fähigkeiten, Status, Kanäle oder Auren ein. Das verbindliche
Gesamtmanifest liegt in
[MVPContentManifest.md](../production/MVPContentManifest.md).

## Werte-Rahmen und Maßstäbe

Damit die 24 Einheiten untereinander und zu Fahrzeugen/Luft/Gebäuden konsistent bleiben, gelten folgende Maßstäbe:

- **Kostenanker:** Standard-Startressourcen 1.000 AE ≈ 8–16 Basissoldaten; eine Harvester-Ladung (~300 AE) ≈ 2–4 Basis-Infanterie. Matchdauer-Ziel 20–35 min (D-010) → frühe Infanterie muss ab Minute 1–2 relevant sein.
- **HP-Klassen:** S = 40–60, M = 80–120, L = 150–250, XL (Elite) = 600+.
- **Tempo:** langsam ~3 m/s, mittel ~4,5 m/s, schnell ~6 m/s (Fahrzeuge-Skala in Vehicles.md).
- **Reichweite:** Nahkampf ≤2 m, kurz ~8 m, mittel ~12 m, lang ~16 m, Belagerung ≥20 m.
- **Bauzeit:** T1 ~5–8 s, T2 ~10–15 s, T3 ~20–30 s (Anker: Ressourcenfluss aus Economy.md; abgeglichen im Korrekturlauf – Economy.md/Buildings.md definieren keine Einheiten-Produktionszeiten, diese Werte sind damit der Referenzrahmen, Feintuning im Balancing-Pass).

### Schadens- und Rüstungsmatrix (Konter-Kohärenz)

**Führend ist [./ArmorSystem.md](./ArmorSystem.md).** Die
Schaden-gegen-Panzerung-Multiplikatoren stehen dort als 6 × 6-Matrix
(6 Schadensarten × 6 Panzerungsklassen) und existieren genau einmal
(Grundsatzregel D-047). Die früher an dieser Stelle geführte 6 × 4-Tabelle über
der Achse „vs. Infanterie / vs. Fahrzeug / vs. Luft / vs. Gebäude" war eine
abgeleitete Zusammenfassung, ist von der führenden Quelle abgedriftet und wurde
mit **D-074 aufgehoben**; sie ist ersatzlos entfernt.

Zwei Folgen für dieses Dokument:

- **Panzerungsklasse statt Einheitenkategorie.** Die Konterachse ist die
  Panzerungsklasse einer Einheit (`Infantry`, `Light`, `Medium`, `Heavy`,
  `Building`, `Air`), nicht ihre Kategorie. „vs. Fahrzeug" faltet `Light`,
  `Medium` und `Heavy` zusammen und kann deshalb kein Konterspiel tragen.
- **„Kristall" ist keine Schadensart** (D-074). Der siebte Eintrag der alten
  Tabelle war Evolvierten-Inhalt; die Evolvierten liegen außerhalb von MS-1.
  Registriert im [ScopeLedger](../production/ScopeLedger.md).

Die `damageType`-Angaben in den Einheitentabellen weiter unten bleiben gültig
und benennen die Zeile der führenden Matrix; verbindliche Waffenwerte
(Reichweite, Schaden, Abklingzeit) stehen in [./Weapons.md](./Weapons.md)
(D-047).

**Rollen-Konter-Prinzip über alle Fraktionen:** Jede Fraktion hat je mindestens eine Infanterie-Antwort auf Fahrzeuge, Luft, Infanterie und Gebäude; die *Form* der Antwort ist asymmetrisch (Allianz präzise/teuer, Legion billig/flächig, Evolvierte nah/regenerierend).

### Datenmodell (ScriptableObject-Felder je Einheit)

`id, faction, displayName, role, tier, costAE, buildTimeSec, hp, armorClass, damageType, dps, rangeM, speedMps, abilities[], prerequisites[], techUnlock, eliteFlag, stealthFlag`

- **Kein Versorgungssystem (D-021):** Es gibt kein `popLimit`-Feld und keine Pop-/Supply-Mechanik. Die Armeegröße wird ausschließlich über Wirtschaft (AE-Fluss), Produktionszeit und das Elite-Limit (D-015, 1–2 gleichzeitig) begrenzt.
- **`stealthFlag`:** `none` | `stationary` (nur im Stillstand getarnt, z. B. Sniper) | `permanent` (dauerhaft getarnt bis zum Angriff, z. B. Commando) – Festlegung gemäß D-026.

### Capture-Regelwerk (D-022)

Einheitliches Regelwerk für alle Capture-Fähigkeiten (*Einnehmen*), verbindlich für Infantry.md und [./NeutralUnits.md](./NeutralUnits.md):

| Regel | Festlegung |
|---|---|
| Mechanik | Kanal-Capture: **5 s Kanal** auf das Ziel; **Abbruch bei Schaden** an der kanalisierenden Einheit (Kanal beginnt von vorn) |
| Kosten | Die Einheit wird bei erfolgreichem Capture **verbraucht** (verschwindet im Ziel) |
| Capture-Einheiten | **Engineer** (Allianz), **Saboteur** (Legion), **Tunnelgräber** (Evolvierte – schließt die Capture-Lücke der Fraktion, D-022) |
| Ziele | Feindliche Gebäude und neutrale capturebare Geschütztürme (D-016) gleichermaßen |
| Brücken-Reparatur (D-028) | Dieselbe Kanal-Mechanik: Engineer, Builder (Baufahrzeug, Vehicles.md) oder Tunnelgräber kanalisieren auf ein zerstörtes Brückensegment und stellen es wieder her (Einheit wird dabei **nicht** verbraucht) |
| Garrison-System | **Nicht im MVP** – Gebäude können nicht von Infanterie besetzt werden; separate Evaluierung frühestens ab Beta (D-022) |

## Allianz – High-Tech, präzise, teuer

Designlinie: Wenige, hochwertige Soldaten; lange Reichweiten, Energie-/Präzisionswaffen; verliert Zahl gegen Zahl, gewinnt Qualität gegen Qualität.

| Einheit | Rolle | Zielt auf | Gekontert von | Kosten | HP | Schaden (DPS/Typ) | Reichw. | Tempo | Tech |
|---|---|---|---|---|---|---|---|---|---|
| Rifleman | Basis-Infanterie | Infanterie | Fläche, Sniper, Fahrzeuge | 120 AE | M (90) | 10 Kinetik | 12 m | mittel | T1 Kaserne |
| Heavy Rifle | Schwere Anti-Infanterie | Infanterie (unterdrückt) | Sniper, Artillerie | 200 AE | L (160) | 16 Kinetik | 13 m | langsam | T2 (+Forschungslabor) |
| Rocket Soldier | Anti-Fahrzeug/Luft | Fahrzeuge, Luft | Infanterie-Fokus | 250 AE | M (100) | 22 Spreng (gelenkt) | 14 m | mittel | T2 |
| Sniper | Präzisions-Entferner | Infanterie (2-Schuss-Profil vs. Standard-Infanterie S/M, D-026) | Fahrzeuge, Nahkampf, FoW-Nähe | 400 AE | S (50) | 40 Kinetik (niedrige Kadenz) | 20 m | mittel | T2 |
| Medic | Support-Heilung | – | alles (unbewaffnet) | 250 AE | S (60) | – | 8 m (Heilung) | mittel | T1 |
| Engineer | Eroberung/Reparatur | Gebäude (capturen), Brücken (reparieren) | alles (unbewaffnet) | 300 AE | S (50) | – | 2 m | mittel | T1 |
| Shield Trooper | Defensiv-Anker | Infanterie, leichte Fahrzeuge | Belagerung, Sniper | 500 AE | L (220 + Schild 100) | 12 Energie | 8 m | langsam | T2 |
| Commando | T3-Spezialist | Gebäude (Sprengsatz), Infanterie | Fahrzeuge, Luft, Zahlen | 1.200 AE | L (180) | 30 Energie | 14 m | schnell | T3 |

**Fähigkeiten:** Rifleman: *Sprint* (kurzer Tempo-Boost). Heavy Rifle: *Unterdrückungsfeuer* (verlangsamt Infanterie im Kegel). Rocket Soldier: *Zielerfassung* (erhöhter Schaden vs. Fahrzeuge nach 1 s Zielen). Sniper: *Tarnung im Stillstand* (`stealthFlag: stationary`, unsichtbar bis Schuss, D-026). Medic: *Feldheilung* (Kanal, heilt 1 Ziel), *Sofortheilung* (Cooldown). Engineer: *Einnehmen* (Kanal-Capture gemäß D-022-Regelwerk: feindliche Gebäude, neutrale Geschütztürme (D-016), Brücken-Reparatur (D-028)), *Reparieren* (Fahrzeuge/Gebäude). Shield Trooper: *Schildwall* (stationär, Energieschild absorbiert Frontalschaden). Commando: *Sprengsatz* (hoher Gebäudeschaden, Kanal), *Tarnung* (`stealthFlag: permanent` – dauerhaft getarnt bis zum ersten Angriff, D-026).

## Legion – Masse statt Klasse, günstig

Designlinie: Niedrige Stückkosten, hohe Stückzahlen; Flächenschaden (Flamme, Mörser) und Salven statt Präzision; gewinnt durch Übermacht und Zersetzung, verliert im Duell gleicher Kosten. **Die Legion hat bewusst keinen Infanterie-Heiler** (D-027): Die Masse-Identität wird über günstige, schnelle Neuproduktion ausgeglichen statt über Erhaltung einzelner Einheiten – gewollte Asymmetrie gegenüber Medic (Allianz) und Bio-Heiler (Evolvierte).

| Einheit | Rolle | Zielt auf | Gekontert von | Kosten | HP | Schaden (DPS/Typ) | Reichw. | Tempo | Tech |
|---|---|---|---|---|---|---|---|---|---|
| Rekrut | Basis-Infanterie (Masse) | Infanterie (durch Zahl) | Fläche, Sniper, Fahrzeuge | 60 AE | S (55) | 7 Kinetik | 11 m | mittel | T1 Kaserne |
| MG-Schütze | Flächen-Anti-Infanterie | Infanterie-Gruppen | Sniper, Artillerie, Fahrzeuge | 120 AE | M (90) | 14 Kinetik (Fläche) | 12 m | langsam | T1 |
| Flammenwerfer | Anti-Infanterie/Gebäude | Infanterie, Gebäude, Vegetation (brennbar, D-012) | Reichweite (Sniper, Mörser) | 180 AE | M (110) | 18 Flamme (Kegel, DoT) | 6 m | mittel | T2 |
| Raketenschütze | Anti-Fahrzeug/Luft | Fahrzeuge, Luft (Salve) | Infanterie-Fokus | 200 AE | M (90) | 18 Spreng (ungelenkt, Salve) | 13 m | mittel | T2 |
| Mörser | Belagerungs-Infanterie | Gebäude, Verteidigung, stehende Gruppen | alles in Nahdistanz (Mindestreichweite 8 m) | 350 AE | S (60) | 25 Spreng (Fläche) | 22 m | langsam | T2 |
| Saboteur | Eroberung/Sabotage | Gebäude (capturen), Produktion (stören) | alles (schwach bewaffnet) | 300 AE | S (60) | 5 Kinetik | 10 m | schnell | T2 |
| Kommando | T3-Spezialist | Infanterie, leichte Fahrzeuge | schwere Fahrzeuge, Luft | 900 AE | L (170) | 24 Kinetik/Spreng-Mix | 13 m | schnell | T3 |
| Offizier | Support/Buff | – (verstärkt Umgebung) | Sniper (Prioritätsziel) | 350 AE | M (100) | 8 Kinetik | 12 m | mittel | T2 |

**Fähigkeiten:** Rekrut: *Sturmangriff* (Gruppen-Tempo-Boost). MG-Schütze: *Niederhaltefeuer* (Suppression-Zone). Flammenwerfer: *Brandfläche* (Boden-DoT, entzündet Vegetation). Raketenschütze: *Salve* (3 Rohre gleichzeitig, Streuung). Mörser: *Rauchgranate* (Sichtblocker). Saboteur: *Einnehmen* (Kanal-Capture gemäß D-022-Regelwerk), *Sabotage* (feindliches Gebäude 15 s außer Betrieb). Kommando: *Thermobarischer Wurf* (kleine Flächen-Granate). Offizier: *Befehlsaura* (+15 % Schaden/Tempo für Infanterie im Umkreis), *Rückzugspfeife* (Morale-Reset, kurzer Tempo-Boost nach hinten).

## Evolvierte – biologisch, Regeneration, Nahkampf

Designlinie: Alle Evolvierten-Infanterie **regeneriert** HP langsam außerhalb des Kampfs (Fraktionsmechanik gemäß D-011, Regeneration statt Reparatur); stärker im Nah-/Mittelbereich, schwächer auf Distanz; Nähe zu Aetherium-Feldern verstärkt Regeneration (Kopplung an D-010).

| Einheit | Rolle | Zielt auf | Gekontert von | Kosten | HP | Schaden (DPS/Typ) | Reichw. | Tempo | Tech |
|---|---|---|---|---|---|---|---|---|---|
| Mutant | Basis-Nahkämpfer | Infanterie (Nahkampf) | Reichweite, Fläche, Flamme | 90 AE | M (100) | 12 Bio (Nahkampf) | 2 m | schnell | T1 Kaserne (Bio-Äquivalent, D-011) |
| Kristallkrieger | Anti-Fahrzeug | Fahrzeuge, Gebäude | Luft, Sniper, Reichweite | 220 AE | L (180) | 20 Kristall | 3 m | mittel | T2 |
| Sporenwerfer | Anti-Luft/DoT | Luft, Infanterie-Gruppen (DoT) | Fahrzeuge, Reichweite | 240 AE | M (90) | 15 Bio (Fläche + DoT) | 14 m | mittel | T2 |
| Berserker | Schock-Nahkampf | Infanterie, leichte Fahrzeuge | Sniper, Kiting, Luft | 300 AE | L (200) | 26 Bio (Nahkampf-Fläche) | 2 m | schnell | T2 |
| Bio-Heiler | Support-Heilung | – | alles (unbewaffnet) | 250 AE | S (60) | – | 8 m (Heilung) | mittel | T1 |
| Kristallmagier | T3-Caster, mobile Flugabwehr (D-026) | Gruppen (Kontrolle/Debuff), Luft (Zielklasse `Both`) | Sniper, Fokus-Feuer | 800 AE | M (110) | 10 Kristall | 14 m | langsam | T3 |
| Tunnelgräber | Infiltration/Belagerung/Capture (D-022) | Gebäude, Mauern (untergräbt), Gebäude (capturen) | alles beim Auftauchen (verwundbar) | 400 AE | M (100) | 8 Bio | 3 m | mittel (unterirdisch schnell) | T2 |
| Alpha-Mutant | **Elite (D-015)** | Infanterie, Fahrzeuge, Gebäude | konzentriertes Sperrfeuer, Superwaffen | – | – | – | – | – | T3, Limit 1–2 |

**Alpha-Mutant (Elite, D-015):** Alle Zahlenwerte (Kosten, HP, DPS, Bauzeit, Reichweite, Produktionsvoraussetzungen) und Fähigkeiten sind **führend in [./Vehicles.md](./Vehicles.md)** (Elite-Tabelle) definiert – keine Doppelpflege in diesem Dokument (Grundsatzregel „jeder Wert existiert genau einmal", D-047; Korrekturlauf Sprint 4). Die Zeile oben dokumentiert nur die Rolle im Infanterie-Kontergeflecht.

**Fähigkeiten:** Alle: *Regeneration* (passiv, ~2 HP/s außerhalb des Kampfs, verdoppelt auf Aetherium-Feld). Mutant: *Ansturm* (Sprung auf Ziel). Kristallkrieger: *Kristalllanze* (Stoßangriff, Bonusschaden vs. Fahrzeuge). Sporenwerfer: *Sporenwolke* (flächiger DoT, wirkt auch vs. Luft). Berserker: *Raserei* (Schaden steigt mit HP-Verlust). Bio-Heiler: *Heilsporen* (Flächen-HoT), *Gestärkte Regeneration* (verdoppelt Regeneration des Ziels). Kristallmagier: *Kristallgefängnis* (hält 1 Einheit fest), *Aether-Resonanz* (verstärkt Regeneration aller Verbündeten im Umkreis); Angriff mit Zielklasse `Both` – Boden und Luft (mobile Evolvierte-Flugabwehr gemäß D-026). Tunnelgräber: *Graben* (unterirdische Bewegung, ignoriert Mauern/Einheitenkollision), *Sprengblase* (Gebäudeschaden beim Auftauchen), *Einnehmen* (Kanal-Capture gemäß D-022-Regelwerk – Evolvierte-Capture-Einheit; auch Brücken-Reparatur nach D-028). Alpha-Mutant: Fähigkeiten und Werte führend in [./Vehicles.md](./Vehicles.md) (Elite-Tabelle, D-015) – keine Doppelpflege hier (D-047-Grundsatzregel).

## Konter-Matrix im Überblick (Rollen-Kohärenz)

| Bedrohung | Allianz-Antwort | Legion-Antwort | Evolvierte-Antwort |
|---|---|---|---|
| Fahrzeuge | Rocket Soldier, Commando | Raketenschütze, Kommando | Kristallkrieger, Alpha-Mutant |
| Luft | Rocket Soldier | Raketenschütze (Salve) | Sporenwerfer, Kristallmagier (D-026) |
| Infanterie | Heavy Rifle, Sniper, Shield Trooper | MG-Schütze, Flammenwerfer, Rekrut-Masse | Mutant, Berserker, Sporenwerfer |
| Gebäude/Mauern | Commando (Sprengsatz), Engineer (capturen, D-022) | Mörser, Flammenwerfer, Saboteur (capturen, D-022) | Tunnelgräber (untergräbt/capturen, D-022), Kristallkrieger, Alpha-Mutant |

Begründung der Asymmetrie: Allianz löst Konter über Reichweite/Qualität (höchste Einzelkosten), Legion über Zahl und Fläche (niedrigste Kosten, höchste Stückzahlen), Evolvierte über Nähe und Nachhaltigkeit (Regeneration kompensiert schwächere Fernkampfwerte). Die Anti-Luft-Abdeckung per Infanterie bleibt bei allen Fraktionen auf T2 beschränkt (einzige Ausnahme: der T3-Kristallmagier der Evolvierten als mobile Flugabwehr, D-026), damit das Flugfeld (D-008) als Anti-Luft-Quelle relevant bleibt.

## Sanitäter (Planung, Refs #56)

Dieser Abschnitt ist reine Planung. Er fügt keine Einheit hinzu, ändert keinen
Code und keine Definition; der Betatest-Wunsch („Sanitäter zunächst nur in die
Planung aufnehmen“) ist damit abgedeckt. Die Allianz-Zeile *Medic* und die
Evolvierte-Zeile *Bio-Heiler* in den Tabellen oben bleiben die Rollenskizze des
Vollspiels; hier steht, was dazu **noch zu klären** ist und wie es sich zu dem
verhält, was MS-1 heute tut.

**MS-1-Stand gegen Vollspielziel**

| Aspekt | MS-1 (ausgeliefert) | Vollspielziel (Planung) |
|---|---|---|
| Heilung eigener Infanterie | Passive Reparaturzone an der Kaserne (#55, Commit `c1a5ebf`): kostenlos, heilt auch im Gefecht, ist aber auf Erholung zwischen Gefechten ausgelegt, für beide aktiven Fraktionen einschließlich der Legion | Zusätzlich aktive Heileinheit (Allianz *Medic*, Evolvierte *Bio-Heiler*) |
| Reparatur von Gebäuden | Builder, kostenpflichtig (D-104) | unverändert, plus Repair-Drohnen (D-014) |
| Reparatur von Fahrzeugen | nur die Fahrzeugfabrik-Zone | Reparaturfahrzeuge (siehe [Vehicles.md](./Vehicles.md)); keine Infanterie |
| Evolvierte-Regeneration | nicht in MS-1 | Fraktionsmechanik gemäß D-011 |
| Sanitäter-Einheit | keine; `UnitRole` und `SimDefinitions` kennen keine Heilrolle | offen, erst wenn der Einheitenbestand ohnehin angefasst wird |

Die Konstanten der Zone stehen ausschließlich im Code
(`ConstructionSystem.PassiveRepairRateHpPerTick`,
`ConstructionSystem.PassiveRepairRadiusCells`, Tickrate `SimClock.TicksPerSecond`);
dieses Dokument nennt keine Zahlen dazu (D-047, jeder Wert existiert genau einmal).
Unter Low Power heilt die Zone nur noch auf geraden Ticks, also mit halber
mittlerer Rate.

**Abgrenzung zur passiven Reparaturzone (#55)**

Beide heilen, dürfen sich aber nicht doppeln. Die Zone ist die Grundversorgung
*zuhause*: ortsgebunden, kostenlos, langsam, zwischen den Gefechten. Ein
Sanitäter wäre die Versorgung *im Feld*: mobil, an eine Produktionskette
gebunden, schneller als die Zone, dafür mit Kosten und einem Platz unter dem
Einheitendeckel des MVP-Manifests. Daraus folgt für die Planung:

- Die Heilrate des Sanitäters muss deutlich über der Zonenrate liegen, sonst
  ist er überflüssig. Der frühere Richtwert aus [ArmorSystem.md](./ArmorSystem.md)
  (ca. 3 % Max-HP/s) liegt nach heutigem Code für MS-1-Infanterie (Max-HP aus `SimDefinitions`) **unter** der
  kostenlosen Zone und ist deshalb nicht mehr tragfähig. Neue Rate: Platzhalter,
  festzulegen im Balancing-Pass nach einer gespielten Runde.
- Die Zone bleibt, was sie ist: Sie ersetzt keinen Sanitäter und wird durch ihn
  nicht abgeschafft.
- Wer das Feldprofil wählt (Kanalheilung auf ein Ziel, Flächenheilung oder
  Sofortheilung mit Cooldown), ist offen.

**Stapelregel (offen).** Vorbild ist die Zone: Dort gewinnt das erste passende
Zonen-Gebäude, es gibt keine Addition. D-027.6 regelt dagegen nur „aktive
Heilung stapelt nicht auf passive Regeneration“ der Evolvierten und ist hier
nicht direkt übertragbar. Zu entscheiden: Stapelt Sanitäter-Heilung auf die
Zone, stapeln mehrere Sanitäter, oder wirkt je Ziel nur die stärkste Quelle?
Empfehlung für die Entscheidung: je Ziel und Tick nur eine Quelle, die
stärkste; das hält die Heilung vorhersagbar und verhindert Sanitäter-Klumpen.

**Umsetzungsform (offen).** D-056 schließt in MS-1 ein generisches
Auren-, Status- und Kanalsystem aus. Ein Sanitäter wäre daher, solange dieses
System fehlt, nur als fest verdrahtete Einzelregel denkbar (Zielwahl, Reichweite
und Heilrate im System selbst, wie bei der Zone), nicht als Fähigkeit im
Sinne der Tabellen oben. Reichweite: Platzhalter, in Zellen anzugeben (die
Tabelle nennt 8 m als Skizze); Werte bei Umsetzung aus `SimDefinitions` ableiten.

**Fraktionsfrage.** D-027.4 gilt: Die Legion bleibt bewusst ohne
Infanterie-Heiler (Ausgleich über günstige Neuproduktion). Das widerspricht der
MS-1-Zone nicht, denn die Zone ist keine Einheit; sie heilt die Legion mit, und
ob diese Annäherung der Masse-Identität schadet, prüft die gespielte Runde.
Das „Legion repariert eher“ aus dem Issue steckt in den Reparaturfahrzeugen
(siehe [Vehicles.md](./Vehicles.md)), die keine Infanterie reparieren. Soll die
Legion doch eine Heilmöglichkeit im Feld bekommen, braucht das eine neue
Entscheidung, die D-027.4 ausdrücklich ersetzt. Allianz *Medic* und Evolvierte
*Bio-Heiler* sind das ausgewiesene Vollspielziel.

**Namensfrage.** Der Anzeigename der Einheit wird nicht hier festgelegt. Die
Einheitennamen stehen insgesamt zur Überarbeitung an (#15, Namensdoktrin in
[06_Narrative.md](../production/hashkrieg/06_Narrative.md)); ein Sanitäter soll
dort mitgedacht werden, damit er nicht in dieselbe Falle läuft (doppelt
vergebene oder nicht zur Fiktion passende Namen). Anzeigenamen fließen nicht in
den Definitions-Hash ein, die Rolle (`UnitRole`) und ihre ID-Vergabe schon;
eine neue Rolle bräuchte daher eine eigene ID-Planung.

**Nicht Teil dieser Planung:** Umsetzung in der Simulation, Eintrag in
`SimDefinitions.cs`, Art-Bestellung und Werte-Tuning. Die offene Frage steht
als Q-048 in [OpenQuestions.md](../production/OpenQuestions.md).

## Offene Punkte

- **Anti-Luft per Infanterie (Status: teilweise entschieden):** Die Evolvierte-Lücke ist durch D-026 geschlossen (Kristallmagier, Zielklasse `Both`). Offen bleibt die langfristige Frage, ob die dual-role Raketen-Infanterie von Allianz/Legion (Fahrzeug + Luft) eigenständig bleibt oder zugunsten von Flugfeld/Flak-Modulen (D-008) abgeschwächt wird – Beobachtung im Balancing-Pass v0.2, Entscheidung Game Director.

Entschieden im Korrekturlauf Sprint 2 (entfernt): Garrison-/Capture-Regelwerk (D-022: Kanal-Capture, kein Garrison im MVP), Elite-Asymmetrie der Evolvierten (D-027: gewollt, Ausgleich über Release-Eliten), Versorgungssystem/`popLimit` (D-021: kein Supply-System), Bauzeit-Anker (abgeglichen: Economy.md/Buildings.md definieren keine Einheiten-Produktionszeiten; die Werte oben sind der Referenzrahmen).

Bereinigt im Korrekturlauf Sprint 4: Alpha-Mutant-Doppelwerte (Kosten/HP/DPS/Fähigkeiten) entfernt – führend ist [./Vehicles.md](./Vehicles.md) (Grundsatzregel D-047, Review-Finding Werte-Duplikation).

## Nächste Schritte

- Übergabe der Asset-Liste (24 Einheiten inkl. Fähigkeiten-VFX-Bedarf) an Sprint 5 (APL Paket 04 bleibt unverändert gültig).
- Definition der Anti-Luft- und Elite-Regeln gemeinsam mit Vehicles.md/Aircraft.md und ResearchTree.md.
- Capture-Regelwerk (D-022) mit [./NeutralUnits.md](./NeutralUnits.md) synchronisieren (capturebare Türme, D-016).
- Balancing-Pass v0.2 (Kosten-, Bauzeit- und Schadens-Feintuning) nach erstem spielbaren Skirmish-Prototyp, danach Version 1.0 zur Sprint-Freigabe.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung: 24 Infanterieeinheiten mit Rollen, Konter-Matrix, Werte-Richtwerten, Fähigkeiten, Tech-Voraussetzungen | Lead Gameplay Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Lead Gameplay Designer |
| 0.3.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings): Alpha-Mutant-Werte/-Fähigkeiten durch Verweis auf Vehicles.md ersetzt (D-047-Grundsatzregel, keine Doppelpflege) | Lead Gameplay Designer |
| 0.4.0 | 2026-07-24 | MS-1 auf Basis- und Panzerabwehrinfanterie von Allianz und Legion begrenzt; aktive Fähigkeiten gemäß D-056 deaktiviert | Lead Gameplay Designer |
| 0.5.0 | 2026-07-26 | Lokale 6 × 4-Schadensmatrix nach D-074 aufgehoben und durch Verweis auf [./ArmorSystem.md](./ArmorSystem.md) ersetzt; „Kristall" als Schadensart gestrichen (in den ScopeLedger verschoben). Einheitentabellen, Rollen und Konter-Matrix unverändert | Agent (unter Inhaber-Delegation, D-074) |
| 0.6.0 | 2026-10-17 | #56 (Planung): Abschnitt „Sanitäter“ ergänzt – MS-1-Stand gegen Vollspielziel, Abgrenzung zur passiven Reparaturzone (#55), Stapelregel, Umsetzungsform, Fraktionsfrage (D-027.4) und Namensfrage; Werte als Platzhalter, keine neue Einheit | Orchestrator |
