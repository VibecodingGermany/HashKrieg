# Balancing-Methodik

**Version:** 0.3.0 | **Status:** Entwurf (Korrekturlauf Sprint 4) | **Verantwortungsbereich:** Lead QA Engineer | **Sprint:** 4

## Zweck

Definiert die Balancing-Ziele, Vergleichsmetriken und den Balancing-Prozess von Hashkrieg. Dieses Dokument ist die Methodik- und Governance-Grundlage: Es enthält **keine** Einheiten- oder Gebäudelisten (diese stehen in den jeweiligen Fachdokumenten), sondern legt fest, **wie** Werte geprüft, verglichen, geändert und protokolliert werden. Verbindlich für alle, die Zahlen an Einheiten, Gebäuden, Wirtschaft oder Waffen festlegen.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) (insb. D-007 Zielgruppe/Geschäftsmodell, D-009 Commander ohne Match-Mechanik, D-010 Wirtschaft & Matchdauer, D-011 Evolvierte-Regeneration, D-014 Drohnen, D-015 Elite-Einheiten, D-018 Modi-Staffelung)
- [../research/KI_Architektur.md](../research/KI_Architektur.md) (Headless-KI-gegen-KI-Läufe, seeded Determinismus, datengetriebene Profile)
- [../research/Unity_BestPractices.md](../research/Unity_BestPractices.md) (SO-Datenmodell-Leitplanken)
- [../research/RTS_Markt_Wettbewerb.md](../research/RTS_Markt_Wettbewerb.md) (H1-Erwartungen, Scope-Risiko dreier Fraktionen)
- Geplante Fachdokumente: ./Factions.md, ./Infantry.md, ./Vehicles.md, ./Aircraft.md, ./Buildings.md, ./Economy.md, ./DamageSystem.md, ./ArmorSystem.md, ./ResearchTree.md, ./Maps.md, ./MultiplayerModes.md, ./Campaign.md

## Balancing-Ziele

Reihenfolge ist Priorität; bei Konflikt gilt das höhere Ziel.

| # | Ziel | Messgröße | Zielband (Startwert v0.1) | Herkunft |
|---|---|---|---|---|
| 1 | Matchdauer 1v1 Skirmish | Median bis Sieg (KI-vs-KI + Playtests) | **20–35 min** | D-010 |
| 2 | Keine Dominanzstrategie | Pick-/Winrate je Strategiearchetyp (Rush, Turtle, Tech, Expand) in Simulationsläufen | keine Strategie > 35 % Gesamt-Winrate über alle Matchups | Methodik |
| 3 | Fraktions-Fairness | Winrate pro gerichtetem Matchup (6 Paarungen) | **45–55 %**; Eingriffspflicht außerhalb 42–58 % über ≥ 200 Sim-Matches | Methodik |
| 4 | Lesbarkeit für H1 (C&C-Nostalgiker, Solo/Skirmish) | qualitative Playtest-Befragung; TTK-Richtwerte unten | Konter erkennbar ohne Tooltip-Studium | D-007 |
| 5 | Asymmetrie bleibt spürbar | Fraktionsidentitäts-Checkliste (Factions.md) pro Balance-Iteration | jede Fraktion verliert/gewinnt anders | Fraktions-Stammdaten |

Erläuterungen:

- **Ziel 2 vor Ziel 3:** Eine Fraktion darf 50 % Winrate haben und trotzdem kaputt sein, wenn ein einziger Build alle anderen schlägt. Strategie-Diversität wird deshalb separat gemessen.
- **Geltungsbereich der Winrate-Bänder:** Ziel 3 (45–55 %) gilt ausschließlich für KI-vs-KI-Simulationen (pro Schwierigkeitsstufe separat) und später PvP – **nicht** für Spieler-vs-KI. Die Solo-Erfahrung der H1-Zielgruppe wird nicht über Winrate-Zielbänder balanciert, sondern über die datengetriebenen Difficulty-Profile (`DifficultyProfileSO`: Entscheidungsqualität statt Ressourcenboni) gemäß [../research/KI_Architektur.md](../research/KI_Architektur.md); die Spielbarkeit je Stufe wird qualitativ über die Playtest-Stufen 3–4 geprüft. PvP-Daten existieren frühestens ab Beta (D-018).
- Matchdauer wird als **Median** gemessen, nicht als Mittelwert; Ausreißer über 45 min und unter 12 min werden als eigene Fehlerklasse untersucht (Stagnation bzw. ungestrafte Rushes).
- **Survival-Modus (Beta, D-018):** hat per Definition keine "Matchdauer bis Sieg". Erfolgsmaß ist **Welle 20 überstehen = Sieg** (Endlos-Modus optional, Abgleich mit [MultiplayerModes.md](MultiplayerModes.md) §3.5); Balance-Metriken dort: erreichte Welle und Zeit bis Niederlage je Fraktion und Schwierigkeit. Für den Endlos-Modus gilt (D-048): Stärke-Abflachung ab Welle 25 (linear statt multiplikativ) und Despawn älterer Wellenreste – die Wellenstärke-Skalierung ist damit gedeckelt und als Balance-Hebel nur bis Welle 25 frei.
- **Globales Einheiten-Deckel (D-048):** Pro Match sind maximal **600 Einheiten** gleichzeitig aktiv (Produktionsstopp mit UI-Hinweis bei Erreichen; führend: [MultiplayerModes.md](MultiplayerModes.md) §2). Alle Balancing-Rechnungen und Simulations-Setups müssen innerhalb dieses Deckels aussagefähig sein; Winrate- oder TTK-Aussagen oberhalb des Deckels sind unzulässig.

## TTK-Richtwerte (Time-to-Kill)

Richtwerte in Sekunden, gemessen an 1-gegen-1-Duellen gleicher Tech-Stufe bei voller HP, Startwerte v0.1 zum Tunen. "Neutral" = kein Konter-Bonus, "Konter" = Einheit greift ihren Soft-Counter an.

| Angreifer → Ziel | Neutral | Konter (Ziel ist Soft-Counter-Opfer) | Begründung |
|---|---|---|---|
| Leichte Infanterie → Leichte Infanterie | 6–8 s | 3–4 s | C&C-Feel: Infanterie-Kämpfe schnell und tödlich, Entscheidungen in Sekunden |
| Schwere Infanterie → Leichtes Fahrzeug | 10–14 s | 6–8 s | Panzerabwehr-Inf muss sich lohnen, aber nicht solo Panzer rasieren |
| Panzer → Panzer | 12–16 s | 8–10 s | Kern-Duell des Midgame; lange genug für Micro (Fokus, Rückzug) |
| Flugabwehr → Luftfahrzeug | 4–6 s | 3–4 s | Luft muss bei AA-Präsenz sofort teuer werden, sonst Dominanzstrategie |
| Luftfahrzeug → Fahrzeug | 8–12 s | 5–7 s | Luft als teures, verletzliches Konter-Instrument, nicht als Dauerdruck |
| Belagerung/Artillerie → Gebäude | 20–30 s | 15–20 s | Gebäude-Tod muss reaktionsfähig bleiben (Verteidigungszeit ~30 s Warnung) |
| Standardarmee → Verteidigungsplattform | 25–40 s | 15–20 s (mit Belagerung) | Turtle darf belohnt, aber nicht unknackbar sein |
| Elite-Einheit → Panzer (Tier 3, D-015) | 5–8 s | 3–5 s | Elite ist Endspiel-Höhepunkt; hartes Limit (1 gleichzeitig im MVP/Alpha, 2 ab Release) begrenzt Eskalation |
| Drohne → beliebig | ≥ 2× TTK der vergleichbaren Kerneinheit | – | Drohnen sind Support (D-014), dürfen keine eigene Kill-Rolle aufbauen |
| Armee → Superwaffen-Gebäude | 40–60 s | – | Superwaffe ist investitionsschweres Ziel; Abriss muss Comeback-fähig sein |

Regeln zu den Richtwerten:

- **Gebäude-TTK zählt ab erstem Schaden**, inkl. Evolvierte-Regeneration (D-011): Regenerationsraten werden so gesetzt, dass effektive TTK bei Evolvierten-Gebäuden im selben Band liegt wie bei Allianz/Legion – Regeneration verlängert Belagerungen ohne Fokusfeuer, nicht generell.
- Kein 1v1-Duell einer Kernklasse darf unter **3 s** fallen (Reaktionszeit-Untergrenze für H1) oder über **45 s** steigen (Stagnations-Obergrenze, Ausnahme HQ/Superwaffe).
- Werte leben in [DamageSystem.md](DamageSystem.md) und [ArmorSystem.md](ArmorSystem.md) (Schadensarten/Rüstungstyp-Matrix); dieses Dokument prüft nur die Kohärenz der daraus resultierenden TTKs.

## Kosten-Nutzen-Framework

Alle Einheiten/Gebäude werden über dieselben, aus den flachen SO-Datensätzen ableitbaren Vergleichsmetriken bewertet. Die Metriken sind **Vergleichswerkzeuge, keine Formel für "fairen" Preis** – Asymmetrie entsteht bewusst durch Abweichung von der Norm, die dokumentiert werden muss.

| Metrik | Definition | Zweck |
|---|---|---|
| DPS/AE | Schaden pro Sekunde ÷ Kosten in AE | Offensiv-Effizienz quer über Fraktionen |
| EHP/AE | Effektive HP (HP × Rüstungstyp-Faktor gegen Standardschaden) ÷ AE | Defensiv-Effizienz |
| DPS/(AE·min) | DPS ÷ (Kosten × Bauzeit) | Zeitkosten einpreisen: Legion-Masse vs. Allianz-Qualität |
| AE/min-Ausbeute | Netto-Einkommen je Aufbau (Harvester-Ladung ~300 AE, Feldzustand gemäß D-010) | Wirtschaftstempo, Expansionsdruck |
| Energie-Kosten je Kampfkraft | Watt-Bedarf ÷ (EHP+DPS) | Low-Power-Risiko als versteckte Kosten |
| Konter-Breite | Anzahl Einheitentypen, die diese Einheit soft kontern | Warnt vor Allzweck-Einheiten (Ziel 2) |

Bewertungsregeln:

- **Fraktions-Normen (Startwerte v0.1):** Allianz zahlt ~15–25 % Aufpreis pro EHP/DPS-Punkt und erhält Reichweite/Präzision/Fähigkeiten; Legion zahlt ~10–20 % weniger bei schlechterer Einzeleffizienz und kürzerer Reichweite; Evolvierte zahlen Normalpreis, erhalten Regeneration (EHP über Zeit) statt Reparatur und Aetherium-Nähe-Boni. Abweichungen über 30 % von der Fraktions-Norm sind ein Review-Pflichtfall.
- **Energie ist Teil der Kosten:** Ein Gebäude, das bei Low Power offline geht (Radar, Verteidigung), wird im Framework mit einem Effektivitäts-Abzug bewertet, sobald die Kraftwerks-Deckung der Fraktions-Standardbasis unter 120 % liegt.
- **Wirtschaftsanker:** Standard-Start 1.000 AE; eine Kern-Tier-1-Einheit soll ~5–10 % der Startressourcen kosten (50–100 AE), ein Harvester-Return ~300 AE pro Ladung. Daraus folgt Richtwert: ~2–4 Harvester-Vollladungen finanzieren eine Angriffsgruppe der Mittelklasse – das hält Matchdauer und Expansionsdruck (D-010) kohärent.
- Alle Metriken werden aus denselben SO-Feldern berechnet, die auch die Simulation nutzt – keine separat gepflegten Balance-Tabellen außerhalb des Datenmodells.

## Konter-Dreieck-Kohärenz

Grundstruktur: **Infanterie → Fahrzeuge → Luft → (Flugabwehr) → Infanterie**, aufgelöst über Schadensarten und Rüstungstypen ([DamageSystem.md](DamageSystem.md), [ArmorSystem.md](ArmorSystem.md)), nicht über harte Einheiten-Flags.

Verbindliche Kohärenz-Regeln:

1. **Soft-Counter statt Hard-Counter:** Ein Konter-Verhältnis bedeutet ~25–50 % Effektivitätsvorteil (weniger TTK, besseres EHP-Verhältnis), niemals Unbesiegbarkeit. Kein Paar darf über +60 % hinaus verzerrt sein.
2. **Jede Einheit hat mindestens zwei Konter** in mindestens zwei verschiedenen Erreichbarkeitsstufen (mindestens einer davon ≤ gleiche Tech-Stufe).
3. **Keine Selbstkonter-Dominanz:** Spiegel-Duelle dürfen nie die kosteneffizienteste Antwort auf eine Einheit sein (sonst entsteht Monokultur).
4. **Konter-Dreieck pro Fraktion intern prüfbar:** Jede Fraktion muss ihr Dreieck mit eigenen Einheiten schließen können – keine Fraktion darf auf ein Matchup angewiesen sein, in dem sie nur defensiv existiert.
5. **Asymmetrie-Prüfung:** Konter-Matrix wird pro Matchup (6 gerichtete Paarungen) durchgespielt; ein Matchup, in dem eine Seite keine Antwort unter Tier 3 hat, gilt als Designfehler, nicht als Balance-Problem.

## Balancing-Prozess

### Datengetriebenheit und Werteset-Versionierung

- Sämtliche Balance-relevanten Werte liegen in ScriptableObject-Datensätzen (flache Felder: `costAE`, `buildTime`, `hp`, `armorType`, `damage`, `damageType`, `attackInterval`, `range`, `speed`, `energyDraw`, …). Keine Konstanten im Code.
- Alle Datensätze eines Stands bilden gemeinsam ein **Werteset** mit eigener Versionsnummer (`balance-v0.x`). Ein Werteset ist die kleinste ausspielbare, testbare und rückholbare Einheit.
- Pro Iteration gilt: **ein Parameter-Cluster pro Änderung** (z. B. "Panzer-Cluster" = HP + Kosten aller Kampfpanzer), Einzelwert-Änderungen max. **±10–15 %** pro Schritt. Größere Sprünge erfordern eine Simulations-Begründung aus mindestens 200 KI-Matches.
- Rollback ist ein Check-out des vorherigen Wertesets – keine manuellen Rückbau-Aktionen.

### Messpipeline (aufsteigende Aussagekraft)

| Stufe | Werkzeug | Kadenz | Metriken |
|---|---|---|---|
| 1 | Statisches Framework (DPS/AE, EHP/AE, Konter-Matrix-Lint) | bei jedem Commit | Framework-Verstöße, Regelbrüche 1–5 |
| 2 | **Headless KI-gegen-KI-Simulationsläufe** gemäß [../research/KI_Architektur.md](../research/KI_Architektur.md): seeded, command-basiert, ohne Renderer | Nightly (CI), ≥ 200 Matches je Matchup-Cluster | Winrate je Matchup, Matchdauer-Median, Zeit bis erster Angriff, Strategiearchetyp-Verteilung, First-Expansion-Zeit |
| 3 | Interner Playtest-Kader (Studio, 4–6 Personen, rotierende Fraktionszuweisung) | wöchentlich ab Alpha | Lesbarkeit, Frustmomente, subjektive Fairness (Ziel 4) |
| 4 | Externe Playtests (geschlossene Gruppe aus H1-Zielgruppe) | ab Beta | Wie Stufe 3, plus Onboarding-Verständnis |
| 5 | **Telemetrie (ab Beta, opt-in, anonymisiert; vorbehaltlich Q-019)** | kontinuierlich | Matchdauern, Fraktions-/Strategieverteilung, Kapitulationszeitpunkte, Low-Power-Häufigkeit |

- Simulationsläufe spiegeln bewusst **Mittel-Schwierigkeit** (unverrauschte Utility-Bewertung, Standard-Reaktionslatenz), damit Ergebnisse nicht vom Difficulty-Profil verrauscht werden.
- Telemetrie (Stufe 5) ist **keine Pflicht**, sondern offene Frage Q-019 ([../production/OpenQuestions.md](../production/OpenQuestions.md), Entscheidung Sprint 3/6). Falls sie kommt, ist sie strikt auf die Skirmish-/Modi-Nutzung begrenzt und enthält keine Inhalte von Savegames oder Kampagne (D-007: Premium-Offline-Produkt; Datensparsamkeit ist Verkaufsargument).
- PvP-Balance-Aussagen vor Beta sind unzulässig; bis dahin gilt die KI-Simulation als Primärquelle (D-018).

### Änderungsprotokoll-Pflicht

Jede Wertänderung wird als Eintrag im Balance-Changelog (Abschnitt des jeweiligen Fachdokuments oder separates `BalanceChangelog.md` ab erster Änderung) protokolliert:

`BAL-xxx | Datum | Werteset alt→neu | Parameter + Alt/Neu-Wert | Datenquelle (Sim-Run-ID / Playtest-Protokoll / Telemetrie-Dashboard) | Begründung | Erwarteter Effekt | Review-Ergebnis nach N Matches`

Undokumentierte Wertänderungen gelten als nicht erfolgt (analog D-005). Reverts behalten ihre BAL-ID und verweisen auf die ursprüngliche Änderung.

## Scope-Grenzen

- Kein Commander-Balancing: Commander haben keine Match-Mechanik im MVP (D-009); ein etwaiges Doktrinen-System ab Beta bekommt bei Einführung eine eigene Methodik-Ergänzung.
- Kein Balancing für Marine (D-013) und kein Handels-/Händler-Balancing (D-016).
- Kampagnen-Missionen nutzen Standard-Wertesets plus dokumentierte Skript-Overrides; sie erzeugen keine eigenen Balance-Ziele (siehe ./Campaign.md).
- Wetter/Hazards (D-017) werden als Map-Modifier über denselben Metriken-Rahmen geprüft (Effekt auf Matchdauer und Matchup-Winrate pro Biom), nicht als Fraktions-Balance.

## Offene Punkte

- **Telemetrie (Messpipeline Stufe 5):** offene Frage **Q-019** in [../production/OpenQuestions.md](../production/OpenQuestions.md) (Pflicht-Feature mit eigenem Backend oder Streichung; Entscheidung Sprint 3/6). Status: offen – Stufe 5 gilt bis dahin als vorbehaltlich, nicht als Pflicht.
- **Headless-KI-Runner (Messpipeline Stufe 2):** Aufwand ungeschätzt; offene Frage **Q-020** in [../production/OpenQuestions.md](../production/OpenQuestions.md) (Entscheidung Sprint 3). Status: offen – Stufe 2 dieser Pipeline hängt direkt daran und ist damit ein Sprint-3-Risiko.

Entschieden im Korrekturlauf Sprint 2 (ehemals offen):

- Winrate-Zielband 45–55 % gilt **nicht** für Spieler-vs-KI; die Solo-Erfahrung wird über die Difficulty-Profile gemäß [../research/KI_Architektur.md](../research/KI_Architektur.md) gesteuert (siehe Balancing-Ziele, Erläuterungen).
- Survival-Erfolgsmaß: **Welle 20 überstehen = Sieg** (Endlos-Modus optional), abgeglichen mit [MultiplayerModes.md](MultiplayerModes.md) §3.5.
- Elite-Limit (D-015): **hartes Limit** – 1 Elite-Einheit gleichzeitig im MVP/Alpha, 2 ab Release; kein kostenbasiertes Soft-Limit.

Entschieden im Korrekturlauf Sprint 4 (D-048):

- Skalierungs-Deckel: globales Einheiten-Deckel **600/Match** (Produktionsstopp + UI-Hinweis), Survival-Endlos-Abflachung ab Welle 25 (linear) + Despawn älterer Wellenreste, `AetheriumDensity` ≤1,5 bei 5–6 Spielern – führend: [MultiplayerModes.md](MultiplayerModes.md); Verweise in den Balancing-Zielen ergänzt.

## Nächste Schritte

- Konter-Matrix-Lint-Regeln und Framework-Berechnungen als Schema-Kommentare in DamageSystem.md/ArmorSystem.md bzw. den Einheiten-Dokumenten verankern (Sprint 2, gemeinsam mit den zuständigen Autoren).
- Simulations-Metriken (Matchdauer, Winrate, Strategiearchetyp-Klassifikation) als Anforderungsliste an den Headless-Runner übergeben (Input Sprint 3).
- Playtest-Kader-Plan (Rollen, Rotation, Protokoll-Template) bis Alpha-Start ausarbeiten.
- Erste Werteset-Baseline `balance-v0.1` definieren, sobald die Fachdokumente Zahlen liefern.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead QA Engineer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Lead QA Engineer |
| 0.3.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings): D-048-Verweise ergänzt (globales Einheiten-Deckel 600/Match, Survival-Endlos-Abflachung ab Welle 25) | Lead QA Engineer |
