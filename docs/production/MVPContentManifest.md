# MVP-Inhaltsmanifest MS-1

**Version:** 1.1.1 | **Status:** verbindlich – G0-A aktiv, Content gesperrt | **Verantwortungsbereich:** Game Director / Producer / Lead Technical Director | **Sprint:** 7

## Zweck

Dieses Dokument ist die menschlich lesbare Inhaltsgrenze für MS-1. Es schließt den
MVP als abhängigen, spielbaren Kern statt als Sammlung isolierter Features. Die
maschinenlesbare Quelle ist
[`../../quality/content/mvp-v1.json`](../../quality/content/mvp-v1.json); bei einer
Abweichung muss beides in derselben Änderung korrigiert werden. Das Manifest ist
eine Anforderung, kein Nachweis: G0, MS-0 und MS-1 sind offen.

## Abhängigkeiten

- [DecisionLog.md](DecisionLog.md) – D-056 (Closed-Core MS-1), D-058
  (Kapazitäten) und D-061 (Abnahme)
- [MVPRecoveryPlan.md](MVPRecoveryPlan.md) – Gates G0 bis G5
- [../gamedesign/Buildings.md](../gamedesign/Buildings.md),
  [../gamedesign/Infantry.md](../gamedesign/Infantry.md) und
  [../gamedesign/Vehicles.md](../gamedesign/Vehicles.md) – führende Namen und
  Balancing-Werte
- [../gamedesign/Resources.md](../gamedesign/Resources.md) – vollständige
  Aetherium-Regeln aus D-010
- [../tech/SimulationCore.md](../tech/SimulationCore.md),
  [../tech/Commands.md](../tech/Commands.md) und
  [../tech/FogOfWar.md](../tech/FogOfWar.md) – technische Verträge
- [`../../quality/scenarios/mvp-v1.json`](../../quality/scenarios/mvp-v1.json) –
  kanonische Abnahmeszenarien

## 1. Produktgrenze

MS-1 ist ein lokales **Solo-Skirmish Mensch gegen KI**:

| Merkmal | Verbindliche Festlegung |
|---|---|
| Parteien | exakt 2 aktive Slots: Mensch und KI |
| Fraktionen | Allianz und Legion |
| Karte | **Glutrinne**, Wüste, Größe S, 128 × 128 Zellen bei 1 m/Zelle |
| Wetter | klar; keine Wetter- oder Hazard-Mechanik |
| Ziel-Matchdauer | 20–35 Minuten |
| Bedienpfad | normales Match ausschließlich über UI und Eingaben; keine Debug-Mutation |
| Ende | Artillerie und endliche Aetherium-Felder sind die Abschlussmittel |

Das Dateiformat reserviert acht Slots, aktiviert in MS-1 aber exakt zwei. Ein
Produktionsmatch akzeptiert insgesamt höchstens 100 gleichzeitig aktive Einheiten.
Der synthetische 500-Agenten-Lastfall ist Architekturreserve und kein Content-
Versprechen.

## 2. Startzustand

Jede Seite beginnt mit:

- einem fertiggestellten HQ,
- einer fertiggestellten Raffinerie,
- einem Builder,
- zwei Harvestern und
- 1.000 AE.

Die Start-Raffinerie ist die **einzige** Ausnahme von ihren normalen
Bauvoraussetzungen. Sie erzeugt beim Matchstart keinen zusätzlichen Harvester.
Alle weiteren Gebäude und Einheiten folgen den regulären Voraussetzungen der
führenden GDD-Dokumente, soweit dieses Manifest sie nicht für MS-1 überschreibt.

## 3. Gebäudeumfang

MS-1 enthält neun Rollen je Fraktion. Die Namen sind aus
[Buildings.md](../gamedesign/Buildings.md) übernommen.

| Rolle / stabile ID | Allianz | Legion |
|---|---|---|
| `HQ` | Kommandozentrale | Gefechtsstand |
| `Power` | Fusionsreaktor | Schwerer Generator |
| `Refinery` | Aetherium-Aufbereiter | Schmelzofen |
| `Storage` | Depot | Bunkerdepot |
| `Barracks` | Ausbildungszentrum | Rekrutenlager |
| `VehicleFactory` | Fahrzeugwerk | Montagehalle |
| `ResearchLab` | Forschungslabor | Kriegslabor |
| `Radar` | Radarstation | Funkposten |
| `DefensePlatform` | Aegis-Plattform | Geschützstellung |

Die Fertigstellung des Forschungslabors schaltet T2 unmittelbar frei. Es gibt in
MS-1 keine Forschungs-Upgrades und keine Forschungswarteschlange. Die
Verteidigungsplattform akzeptiert genau:

- `MG` auf T1 und
- `Rocket` auf T2.

`Flak` ist deaktiviert. Mauern, Flugfeld, Superwaffe und jedes T3-Gebäude sind
nicht im Manifest.

## 4. Einheitenumfang

MS-1 enthält acht Rollen je Fraktion. Die Namen sind aus
[Infantry.md](../gamedesign/Infantry.md) und
[Vehicles.md](../gamedesign/Vehicles.md) übernommen.

| Rolle / stabile ID | Allianz | Legion | Tier |
|---|---|---|---|
| `Builder` | Pionier „Atlas“ | Vorarbeiter | T1 |
| `Harvester` | Sammler „Demeter“ | Schürfer | T1 |
| `BasicInfantry` | Rifleman | Rekrut | T1 |
| `AntiArmorInfantry` | Rocket Soldier | Raketenschütze | T2 |
| `ScoutVehicle` | Jackal-Aufklärer | Hyäne (Buggy) | T1 |
| `LightTank` | Lynx | Räuber | T1 |
| `BattleTank` | Aegis | Koloss | T2 |
| `Artillery` | Longbow | Donnerkanone | T2 |

Für MS-1 ist kein generisches System für aktive Fähigkeiten, Status, Kanäle oder
Auren erlaubt. Frühere aktive Fähigkeiten dieser Einheiten sind deaktiviert.
Fraktionsidentität entsteht ausschließlich lokal über Waffen- und
Wirtschaftsdefinitionen:

- **Allianz:** höhere Kosten, Präzision und Einzelschaden; Harvester-Kapazität
  330 AE.
- **Legion:** niedrigere Kosten, schnellere Produktion, Salven und
  Flächenschaden; Harvester-Kapazität 300 AE.

Aktive Umschalter sind nicht erforderlich. Diese Abgrenzung verhindert, dass ein
generisches Effekt-Framework zum versteckten MS-1-Vorläufer wird.

## 5. Glutrinne

Der MS-1-Datensatz für Glutrinne ist symmetrisch und enthält:

| Element | Anzahl | Reserve |
|---|---:|---:|
| Startfelder | 2 | je 9.000 AE |
| Natürliche Expansionen | 2 | je 9.000 AE |
| Zentrales Feld | 1 | 15.000 AE |
| Hauptangriffswege | 2 | – |

Es gibt keine Neutralen, Brücken, Capture-Ziele, Wettereffekte, Hazards oder
Umgebungszerstörung. Aetherium ist die einzige zerstör- beziehungsweise
veränderbare Umweltkomponente.

## 6. Aetherium als vollständiger Kern

D-010 wird nicht auf einen vereinfachten Sammler-Loop reduziert. MS-1 muss
zusammenhängend liefern:

1. endliche Mutterreserve,
2. Nachwachsen aus dieser Reserve,
3. Ausbreitung mit sichtbarer Terrainfolge,
4. permanenten Schaden durch Überernte,
5. jederzeit lesbaren Zustand und eine Warnung vor Verschlechterung,
6. KI-Management derselben Regeln und
7. umkämpfte Expansionen.

Artillerie öffnet statische Situationen; sinkende Feldreserven erzwingen
Expansion und beenden Turtling. Beide Mechanismen werden in der 20–35-Minuten-
Abnahme gemeinsam bewertet.

## 7. Sieg, Remis und Last-Unit-Reveal

Die Sieglogik wird nach Combat am Ende jedes Sim-Ticks ausgewertet:

- Ein Slot ist eliminiert, sobald er keine lebende Einheit und kein lebendes
  Gebäude einschließlich Baustellen mehr besitzt. Der andere Slot gewinnt mit
  Ergebnisgrund `Victory.Elimination`.
- Sind beide Slots im selben Tick eliminiert, endet das Match als
  `Draw.MutualAnnihilation`.
- Nach exakt 27.000 Ticks beziehungsweise 45 Minuten ohne Elimination endet
  das Match als `Draw.TimeLimit`. Ein Remis ist ein gültiges Ergebnis, aber
  kein Sieg für Balance-Statistiken.
- Besitzt ein Slot 600 Ticks ununterbrochen kein Gebäude und höchstens drei
  Einheiten, werden diese für den Gegner sichtbar und zielbar. Der Timer wird
  zurückgesetzt, sobald die Bedingung nicht mehr gilt.
- MS-1 besitzt weder automatische KI-Aufgabe noch einen Surrender-Befehl für
  den Spieler.

Headless-Ausgaben enthalten mindestens Endtick, Ergebnisgrund sowie Gewinner-
und Verlierer-Slot oder bei Remis zwei `null`-Slots.

## 8. Produktminimum

MS-1 umfasst zusätzlich zum Match:

- Pause,
- zehn manuelle Speicherplätze,
- rotierendes Quicksave-Paar A/B,
- drei rotierende Autosaves im Abstand von fünf Minuten,
- Laden einschließlich Backup-Wiederherstellung,
- frei belegbare Eingaben,
- UI-Skalierung von 80 % bis 150 %,
- Farb- **und** Formredundanz für spielrelevante Zustände,
- reduzierte Kameraerschütterung und reduzierte Lichtblitze sowie
- sicht- oder hörbares Client-Feedback auf einen Befehl innerhalb von höchstens
  100 ms.

Speichern und Laden verwenden den kanonischen Zustand aus
[SimulationCore.md](../tech/SimulationCore.md). Ein UI-only-Match darf weder
Konsolenbefehle noch Inspector-Mutationen benötigen.

## 9. Explizite Nicht-Ziele

Für MS-1 zurückgestellt sind:

- Evolvierte;
- Luftfahrzeuge und Flak;
- Mauern, T3, Eliten, Superwaffen und Drohnen;
- generische Fähigkeiten, Status, Kanäle und Auren;
- Capture, Neutrale und Brücken;
- Wetter, Hazards und weitere Umgebungszerstörung;
- weitere Karten und Biome;
- Commander, Doktrinen und Voice-over;
- Kampagne;
- Online, Koop, FFA, Survival, PvP und Ranked;
- Telemetrie, Steam-Integration und Cloud-Saves sowie
- finale Art- und Audio-Produktion.

D-008, D-014, D-015, D-016, D-022, D-023, D-026, D-030 und D-031 behalten ihr
Vollspiel-Zielbild, werden für MS-1 jedoch durch D-056 übersteuert.

## Offene Punkte

- Q-018 und Q-019 bleiben offen, blockieren MS-1 aber nicht.
- Balancing-Werte außerhalb der ausdrücklich überschriebenen Cargo-Kapazität
  bleiben Tuningwerte ihrer führenden GDD-Dokumente und werden über G5
  abgenommen.

## Nächste Schritte

1. G0-A und G0-B ohne Inhaltsvorgriff herstellen.
2. Definitionen erst nach bestandenem G1 gegen dieses Manifest implementieren.
3. Ab G4 die exakte Manifestvollständigkeit und Provenienz nachweisen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-24 | Verbindlichen dependency-closed MS-1-Umfang gemäß D-056 und D-058 festgelegt | Game Director / Producer / Lead Technical Director |
| 1.1.0 | 2026-07-24 | Sieg-, Remis-, Zeitlimit- und Last-Unit-Reveal-Vertrag geschlossen | Game Director / Lead Technical Director |
| 1.1.1 | 2026-07-24 | Aktiven Sprint-7-Status auf G0-A/G0-B korrigiert; Content bleibt bis nach G1 gesperrt | Game Director / Producer / Lead Technical Director |
