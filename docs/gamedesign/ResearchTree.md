# Technologiebaum (Research Tree)

**Version:** 0.4.0 | **Status:** Entwurf – MS-1-Override verbindlich | **Verantwortungsbereich:** Lead Gameplay Designer | **Sprint:** 2

## Zweck

Definiert den Forschungsbaum aller drei Fraktionen (Allianz, Legion, Evolvierte) für Project Nova: Tier-Struktur 1–3, die vier Kategorien Wirtschaft / Militär / Spezialfähigkeiten / Superwaffe, konkrete Technologien mit Kosten, Zeiten, Voraussetzungen und Effekten sowie Freischaltpfade für Elite-Einheiten (D-015) und Superwaffen. Verbindlich für Balancing, UI (Forschungsmenü), KI (Forschungsverhalten) und die technische Umsetzung als datengetriebene Datensätze (ScriptableObjects, flache Struktur). Alle Zahlen sind Startwerte v0.1 zum Tunen, keine finalen Balancing-Werte.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – insbesondere D-008 (12 Gebäudetypen, Forschungslabor), D-010 (Aetherium-Hybridwirtschaft, Matchdauer 20–35 min), D-011 (Evolvierte bauen durch Wachstum), D-015 (Elite-Einheiten, Tier-3-Freischaltung), D-030 (Tech-Umfang 12–16, Ausschluss-Regel, Low Power gilt auch für Forschung)
- [../analysis/KnowledgeBase.md](../analysis/KnowledgeBase.md) – Fraktions-Stammdaten, Zahlengerüst (AE, Energie, Startressourcen 1.000 AE, Harvester-Ladung ~300 AE)
- [./Factions.md](./Factions.md) – Fraktionsidentitäten und Spezialtechnologie-Themen (Sprint 2, parallel)
- [./Economy.md](./Economy.md) – Wirtschaftswerte, auf die Wirtschafts-Techs wirken (Sprint 2, parallel)
- [./Infantry.md](./Infantry.md), [./Vehicles.md](./Vehicles.md), [./Aircraft.md](./Aircraft.md) – Einheitenwerte, auf die Militär-Techs wirken; Elite-Einheiten-Definition in Vehicles.md (Sprint 2, parallel)
- [./Buildings.md](./Buildings.md) – Forschungslabor, HQ-Neuaufbau ("Basis-Neugründung", §2.1), Superwaffen-Gebäude
- [RTS_Game_Design_Outline.md](../../RTS_Game_Design_Outline.md) – Kategorien Tier 1–3, Wirtschaft/Militär/Spezialfähigkeiten/Superwaffen

## MS-1-Override (D-056)

MS-1 implementiert keinen Forschungsbaum. Die Fertigstellung des
Forschungslabors schaltet T2 unmittelbar frei. Forschungs-Upgrades,
Forschungswarteschlange, Ausschlussentscheidungen, T3, Eliten und Superwaffen
sind Post-MVP. Die nachfolgenden Tech-Datensätze bleiben Vollspiel-Zielbild und
sind kein G0–G5-Abnahmekriterium. Führende Inhaltsquelle ist
[MVPContentManifest.md](../production/MVPContentManifest.md).

## Designprinzipien

1. **Datengetrieben:** Jede Technologie ist ein flacher Datensatz (ScriptableObject-tauglich) – keine hartcodierten Effekte. Schema siehe Abschnitt "Tech-Datensatz-Schema".
2. **Flach statt tief:** Pro Fraktion ca. 15 Technologien (11 gemeinsames Grundgerüst + 4 fraktionsspezifisch) – innerhalb des bestätigten Korridors von 12–16 Techs pro Fraktion (D-030). Der Baum ist bewusst überschaubar (Skirmish-first, H1-Zielgruppe "C&C-Nostalgiker" gemäß D-007) – Entscheidungen statt Listen abarbeiten.
3. **Klare Tier-Taktung:** Tier 1 = frühe Optimierung, Tier 2 = Mitte des Matches (Spezialisierung, Ausschluss-Entscheidungen), Tier 3 = Endspiel (Elite, Superwaffe). Passt zur Ziel-Matchdauer 20–35 min (D-010).
4. **Keine Pseudo-Präzision:** Werte sind Richtwerte mit Begründung, kalibriert auf Startressourcen 1.000 AE und Harvester-Ladung ~300 AE.
5. **Forschung kostet nur AE und Zeit, keine Energie** – Energie wirkt indirekt über die Low-Power-Regel: Bei Energiedefizit sinkt auch die Forschungsgeschwindigkeit um 50 % (D-030, konsistent zur Produktionsregel).

## Tech-Datensatz-Schema

Jede Technologie wird als flacher Datensatz mit folgenden Feldern spezifiziert:

| Feld | Typ | Beschreibung |
|---|---|---|
| `techId` | string | Eindeutige ID, Schema `TECH_<FRK>_<KAT>_<NAME>` (FRK: AL/LG/EV; KAT: ECO/MIL/SPC/SW) |
| `displayName` | string | Anzeigename (lokalisierbar) |
| `category` | enum | `Economy`, `Military`, `Special`, `Superweapon` |
| `tier` | int | 1–3 |
| `faction` | enum | `Alliance`, `Legion`, `Evolved` |
| `costAE` | int | Kosten in AE |
| `researchTimeS` | float | Forschungsdauer in Sekunden |
| `prereqTechIds` | string[] | Vorausgesetzte Technologien (leer = keine) |
| `prereqBuildings` | string[] | Vorausgesetzte Gebäude (z. B. `ResearchLab`) |
| `exclusiveWith` | string[] | Gegenseitig ausschließende Tech-IDs |
| `effects` | EffectEntry[] | Flache Effektliste: `{ target, attribute, op, value }` (z. B. `{ Harvester, CargoCapacity, Mul, 1.2 }`) |
| `unlockId` | string | Optional: freigeschaltete Einheit/Fähigkeit (Elite, Superwaffe) |

Effekte wirken global auf die Fraktion des Spielers (keine pro-Einzelinstanz-Upgrades). Multiplikative Stufen derselben Upgrade-Linie multiplizieren sich (Stufe 1–3 Panzerung = ×1,1 / ×1,21 / ×1,331).

## Tier-Struktur und Voraussetzungen

| Tier | Voraussetzungen | Rolle im Match | Richtwert Kosten | Richtwert Zeit |
|---|---|---|---|---|
| 1 | Keine (Forschung direkt im jeweiligen Produktionsgebäude bzw. HQ verfügbar) | Minute 2–8: frühe Wirtschafts-/Rüstungsoptimierung | 300–500 AE | 20–30 s |
| 2 | Forschungslabor gebaut (D-008) | Minute 8–18: Spezialisierung, Ausschluss-Techs, Einheiten-Upgrades Stufe 2 | 600–1.000 AE | 40–60 s |
| 3 | Forschungslabor + Tech `T3_ERCORE` ("Tier-3-Freischaltung", je Fraktion 1.200 AE / 60 s) | Minute 18+: Elite-Einheit (D-015), Superwaffenpfad, Upgrades Stufe 3 | 1.200–2.000 AE | 75–120 s |

Begründung der Kostenlogik: Eine Tier-1-Tech kostet etwa 1–1,5 Harvester-Ladungen (~300 AE) – früh erreichbar, aber spürbar. Tier 3 mit ~1.200–2.000 AE entspricht 4–7 Ladungen und ist damit eine bewusste Endspiel-Investition, die bei 20–35 min Matchdauer (D-010) nicht "nebenbei" fällt. Der generische Tier-3-Schlüssel (`T3_ERCORE`) verhindert, dass Spieler das Forschungslabor nur für eine einzelne späte Tech bauen.

## Gemeinsames Grundgerüst (alle Fraktionen)

Diese 11 Technologien existieren für jede Fraktion mit identischen Werten (eigene `techId` pro Fraktion, z. B. `TECH_AL_ECO_YIELD1`). Begründung: gemeinsames Balancing-Rückgrat, Fraktionsasymmetrie entsteht über Einheitenwerte und die 4 Spezialtechs – nicht über versteckte Wirtschaftsunterschiede.

| Tech-ID (Suffix) | Name | Kat. | Tier | Kosten | Zeit | Voraussetzungen | Effekt |
|---|---|---|---|---|---|---|---|
| `ECO_YIELD1` | Verfeinerte Extraktion | ECO | 1 | 400 AE | 25 s | – | Ernteertrag +20 % (Harvester-Ladung ~300 → ~360 AE) |
| `ECO_POWER1` | Energieoptimierung | ECO | 1 | 350 AE | 20 s | – | Kraftwerks-Output +15 % (Low-Power-Risiko senken) |
| `MIL_ARMOR1` | Panzerung Stufe 1 | MIL | 1 | 400 AE | 25 s | – | Alle Einheiten: +10 % Trefferpunkte |
| `MIL_WEAP1` | Waffensysteme Stufe 1 | MIL | 1 | 400 AE | 25 s | – | Alle Einheiten: +10 % Schaden |
| `ECO_YIELD2` | Tiefenextraktion | ECO | 2 | 800 AE | 45 s | Forschungslabor, `ECO_YIELD1` | Ernteertrag +20 % (kumulativ ×1,44); Überernte-Schaden am Mutterkristall −25 % (D-010) |
| `MIL_ARMOR2` | Panzerung Stufe 2 | MIL | 2 | 800 AE | 45 s | Forschungslabor, `MIL_ARMOR1` | +10 % Trefferpunkte (kumulativ ×1,21) |
| `MIL_WEAP2` | Waffensysteme Stufe 2 | MIL | 2 | 800 AE | 45 s | Forschungslabor, `MIL_WEAP1` | +10 % Schaden (kumulativ ×1,21) |
| `SPC_REBASE` | Basis-Neugründung | SPC | 2 | 800 AE | 50 s | Forschungslabor | Ermöglicht den Neuaufbau des HQ nach Verlust: Ein Builder-Fahrzeug (Allianz/Legion) bzw. das Evolvierte-Builder-Äquivalent errichtet das neue HQ **eigenständig** – außerhalb der HQ-Bau-Queue (D-031.1; Gebäuderegeln in [./Buildings.md](./Buildings.md) §2.1) |
| `MIL_ARMOR3` | Panzerung Stufe 3 | MIL | 3 | 1.500 AE | 90 s | `T3_ERCORE`, `MIL_ARMOR2` | +10 % Trefferpunkte (kumulativ ×1,331) |
| `MIL_WEAP3` | Waffensysteme Stufe 3 | MIL | 3 | 1.500 AE | 90 s | `T3_ERCORE`, `MIL_WEAP2` | +10 % Schaden (kumulativ ×1,331) |
| `SPC_T3CORE` | Tier-3-Freischaltung (`T3_ERCORE`) | SPC | 2→3 | 1.200 AE | 60 s | Forschungslabor | Schaltet alle Tier-3-Techs, Elite-Forschung und Superwaffenpfad frei |

Hinweis Evolvierte (D-011): Bei den Evolvierten ersetzt `ECO_YIELD1/2` zusätzlich die Effekt-Zeile "Keim-Reifung −10/−20 % Zeit in Aetherium-Nähe" (gleiche Kosten/Zeit, erweiterter `effects`-Block) – Wachstumsökonomie statt nur Harvester-Ertrag. Reparatur-Techs entfallen (Regeneration ist Baseline). Das Evolvierte-Forschungslabor (Synapsenhülle) forscht mechanisch identisch (gleiche Queue, Kosten und Zeiten); es unterliegt der Wachstumsbauweise mit Keim/Reifung und Regeneration statt Reparatur (D-011, [./Buildings.md](./Buildings.md) §2.8/§7).

## Fraktions-Spezialtechnologien

Je 4 Techs pro Fraktion (2× Tier 2 als Ausschluss-Paar, 1× Tier 3 Spezialfähigkeit, 1× Elite-Freischaltung). Die Tier-2-Paare schließen sich gegenseitig aus (`exclusiveWith`) – Begründung: fraktionsinterne Build-Identität pro Match ("Wie spiele ich die Allianz heute?"), Verdopplung der wahrgenommenen Tiefe ohne zusätzlichen Balancing-Umfang, und verhindert Power-Stacking beider Boni auf derselben Einheitenlinie. Regel bestätigt durch D-030: max. 1 Ausschluss-Paar pro Fraktion, auf Tier 2 beschränkt (identitätsstiftend).

### Allianz – Thema: Präzisionssysteme

| Tech-ID | Name | Kat. | Tier | Kosten | Zeit | Voraussetzungen / Ausschluss | Effekt |
|---|---|---|---|---|---|---|---|
| `TECH_AL_SPC_PRECMUN` | Präzisionsmunition | SPC | 2 | 900 AE | 50 s | Forschungslabor; schließt `OVERCAP` aus | Fahrzeuge/Luft: +15 % Reichweite, +10 % Schaden gegen Fahrzeuge |
| `TECH_AL_SPC_OVERCAP` | Überladene Kondensatoren | SPC | 2 | 900 AE | 50 s | Forschungslabor; schließt `PRECMUN` aus | Verteidigungsplattformen + Ionenstrahl-Superwaffe: +25 % Schaden, −10 % Energieverbrauch |
| `TECH_AL_SPC_AEGIS` | Aegis-Schirmprojektoren | SPC | 3 | 1.600 AE | 90 s | `T3_ERCORE` | Aktivierbare Fähigkeit: Energieschild (absorbiert 500 Schaden, 20 s) für Einheitengruppe im Zielradius, 90 s Cooldown |
| `TECH_AL_MIL_ELITE` | Elite-Freischaltung (Allianz) | MIL | 3 | 1.500 AE | 75 s | `T3_ERCORE` | Schaltet die Allianz-Elite-Einheit frei (Definition in [./Vehicles.md](./Vehicles.md); Limit 1 gleichzeitig im MVP, 2 ab Release gemäß D-015) |

Ausschluss-Begründung Allianz: `PRECMUN` (mobile Offensive) vs. `OVERCAP` (statische Defensive/Superwaffe) erzwingt die Grundrichtung des Spielplans – beides zusammen würde die Allianz gleichzeitig zur besten Angriffs- und besten Bunker-Fraktion machen.

### Legion – Thema: Thermobarik

| Tech-ID | Name | Kat. | Tier | Kosten | Zeit | Voraussetzungen / Ausschluss | Effekt |
|---|---|---|---|---|---|---|---|
| `TECH_LG_SPC_THERMO` | Thermobarische Gefechtsköpfe | SPC | 2 | 800 AE | 45 s | Forschungslabor; schließt `SPLINTER` aus | Raketen-/Flammenwaffen: +20 % Flächenschaden-Radius, Brandflächen halten 5 s länger (synergiert mit brennbarer Vegetation, D-012) |
| `TECH_LG_SPC_SPLINTER` | Splittermunition | SPC | 2 | 800 AE | 45 s | Forschungslabor; schließt `THERMO` aus | Raketen: +30 % Schaden gegen Infanterie, −10 % gegen Gebäude (Anti-Masse-Spezialisierung) |
| `TECH_LG_SPC_MASSPROD` | Kriegswirtschaft | SPC | 3 | 1.400 AE | 80 s | `T3_ERCORE` | Einheitenproduktion −15 % Kosten, −15 % Bauzeit ("Masse statt Klasse" als Endspiel-Verstärker) |
| `TECH_LG_MIL_ELITE` | Elite-Freischaltung (Legion) | MIL | 3 | 1.500 AE | 75 s | `T3_ERCORE` | Schaltet die Legion-Elite-Einheit frei (Definition in [./Vehicles.md](./Vehicles.md); Limit 1 im MVP, 2 ab Release gemäß D-015) |

Ausschluss-Begründung Legion: `THERMO` belohnt Flächenkontrolle und Synergie mit Umgebungsbrand, `SPLINTER` das harte Kontern von Infanteriemassen – beide gleichzeitig ließen Raketen einheitenübergreifend ohne Schwachstelle.

### Evolvierte – Thema: Mutationen

| Tech-ID | Name | Kat. | Tier | Kosten | Zeit | Voraussetzungen / Ausschluss | Effekt |
|---|---|---|---|---|---|---|---|
| `TECH_EV_SPC_AGGRMUT` | Aggressive Mutation | SPC | 2 | 850 AE | 50 s | Forschungslabor (Bio-Äquivalent); schließt `REGENMUT` aus | Alle Einheiten: +15 % Angriffsgeschwindigkeit, −10 % Trefferpunkte (Glas-Cannon-Profil) |
| `TECH_EV_SPC_REGENMUT` | Regenerative Mutation | SPC | 2 | 850 AE | 50 s | Forschungslabor; schließt `AGGRMUT` aus | Regeneration +50 % (Baseline-Regen verdoppelt sich faktisch), +10 % Trefferpunkte |
| `TECH_EV_SPC_SPORE` | Aether-Sporen | SPC | 3 | 1.500 AE | 85 s | `T3_ERCORE` | Aktivierbare Fähigkeit: beschleunigt Aetherium-Feldausbreitung im Zielgebiet 120 s um +100 % (D-010) und heilt eigene Einheiten dort leicht |
| `TECH_EV_MIL_ELITE` | Elite-Freischaltung (Evolvierte) | MIL | 3 | 1.500 AE | 75 s | `T3_ERCORE` | Historisches Post-MVP-Zielbild; in MS-1 deaktiviert (D-056) |

Ausschluss-Begründung Evolvierte: `AGGRMUT` vs. `REGENMUT` ist die klassische Risiko-/Beständigkeits-Entscheidung; beides zusammen würde die Baseline-Schwäche (niedrige Einzelwerte, auf Regeneration angewiesen) vollständig kompensieren.

## Superwaffen-Freischaltpfad

Identisch für alle Fraktionen, drei Stufen (Superwaffen-Gebäude ist einer der 12 Gebäudetypen, D-008):

1. **Tier-3-Zugang:** Forschungslabor + `SPC_T3CORE` (1.200 AE / 60 s).
2. **Superwaffen-Forschung** `TECH_<FRK>_SW_UNLOCK` ("Waffenkalibrierung" / "Zündkaskade" / "Kristallresonanz"): Kategorie `Superweapon`, 2.000 AE / 120 s – die teuerste Tech im Spiel, bewusst als Endspiel-Commitment (~7 Harvester-Ladungen).
3. **Bau des Superwaffen-Gebäudes** (Baukosten in [./Buildings.md](./Buildings.md), Richtwert 2.500 AE): erst nach abgeschlossener `SW_UNLOCK`-Forschung im Baumenü sichtbar. Nach Fertigstellung: Superwaffe (Ionenstrahl / Thermobarische Salve / Kristallsturm) mit Cooldown 300 s, hoher Energiebedarf – bei Low Power offline (Low-Power-Regel: Verteidigung offline, Superwaffe wird explizit mitbehandelt).

Begründung des zweistufigen Gates (Forschung + Bau): verhindert "Superwaffen-Rush" aus einem einzigen großen AE-Polster, schafft zwei aufklärbare/angreifbare Schwachpunkte (Forschungslabor, Gebäude im Bau) und gibt dem Gegner im 20–35-min-Fenster (D-010) eine realistische Reaktionschance.

## Beispiel-Datensatz (Zielbild ScriptableObject)

| Feld | Wert |
|---|---|
| `techId` | `TECH_LG_SPC_THERMO` |
| `displayName` | "Thermobarische Gefechtsköpfe" |
| `category` / `tier` / `faction` | `Special` / `2` / `Legion` |
| `costAE` / `researchTimeS` | `800` / `45.0` |
| `prereqTechIds` / `prereqBuildings` | `[]` / `[ResearchLab]` |
| `exclusiveWith` | `[TECH_LG_SPC_SPLINTER]` |
| `effects` | `[{WeaponRocket, AoERadius, Mul, 1.2}, {WeaponFlame, BurnDurationS, Add, 5.0}]` |
| `unlockId` | – |

## Offene Punkte

- **Radar als zusätzliche Tier-3-Voraussetzung** wurde erwogen (Anti-Rush, Aufklärungsdruck) und verworfen, um den Baum flach zu halten. Status: unentschieden/zurückgestellt – nur relevant, falls der Game Director mehr Gebäude-Zwang wünscht; Radar wäre der erste Kandidat.

(Entschieden und ins Dokument überführt: Low Power gilt für Forschung (D-030), Tech-Umfang 12–16/Fraktion (D-030), Ausschluss-Regel max. 1 Paar auf Tier 2 (D-030), Elite-Definition/-Limit in Vehicles.md (D-015), Evolvierte-Forschungslabor mechanisch identisch (D-011), Ausführungsmechanik HQ-Neuaufbau über Builder-Fahrzeug/Evolvierte-Äquivalent außerhalb der HQ-Queue (D-031.1).)

## Nächste Schritte

1. Abgleich mit [./Vehicles.md](./Vehicles.md), [./Infantry.md](./Infantry.md) und [./Aircraft.md](./Aircraft.md) (Upgrade-Zielattribute, Elite-Werte) sowie [./Economy.md](./Economy.md) (Ernteertrag-Basiswerte, Überernte-Schaden) im Sprint-2-Review.
2. Datensätze als ScriptableObject-Schema an Tech-Design/Engineering übergeben (Felder gemäß Schema-Abschnitt).
3. Erste Balancing-Simulation (Skirmish-Sandbox): Forschungs-Timing gegen Matchdauer-Ziel 20–35 min prüfen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Gameplay Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Lead Gameplay Designer |
| 0.3.0 | 2026-07-21 | Feinschliff Sprint 2 Runde 2 (D-031) | Lead Gameplay Designer |
| 0.4.0 | 2026-07-24 | MS-1 auf unmittelbare T2-Freischaltung ohne Forschung, Queue oder T3 gemäß D-056 begrenzt | Lead Gameplay Designer |
