# Game Loop – Kernloop, Match-Phasen, Spannungsbögen

**Version:** 0.3.0 | **Status:** Entwurf – MS-1-Override verbindlich | **Verantwortungsbereich:** Lead Gameplay Designer | **Sprint:** 2

## Zweck

Beschreibt den spielerischen Kernloop von *Project Nova*, den zeitlichen Ablauf eines Matches bei der Ziel-Matchdauer von 20–35 Minuten (D-010), die beabsichtigten Spannungsbögen und die Anti-Stall-Mechanismen, die aus der Aetherium-Hybridwirtschaft (D-010) erwachsen. Verbindlich für Balancing, KI-Design, Map-Design und alle Wirtschaftsdokumente.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – insb. D-008 (12 Gebäudetypen), D-010 (Aetherium-Hybridwirtschaft, 20–35 min), D-011 (Evolvierte-Wachstum), D-014 (Drohnen), D-015 (Elite-Einheiten), D-016 (Neutrale), D-018 (Modi-Staffelung), D-023 (Superwaffen-Limit 1), D-025 (Alpha-FFA lokal, Online-Modi ab Beta)
- [./CoreGameplay.md](./CoreGameplay.md) – Steuerung und Spielgefühl
- [../gamedesign/VictoryConditions.md](../gamedesign/VictoryConditions.md) – Sieg-/Timeout-Regeln
- [../gamedesign/Economy.md](../gamedesign/Economy.md) – Wirtschaftsparameter im Detail (Sprint 2, parallel)
- [../gamedesign/Resources.md](../gamedesign/Resources.md) – Aetherium-Feldregelwerk inkl. Überernte-Stufenmodell
- [../gamedesign/ResearchTree.md](../gamedesign/ResearchTree.md) – Tech-Tiers 1–3 (Sprint 2, parallel)
- [../gamedesign/Maps.md](../gamedesign/Maps.md) und [../gamedesign/Biomes.md](../gamedesign/Biomes.md) – Kartengerüst (Sprint 2, parallel)
- [../analysis/KnowledgeBase.md](../analysis/KnowledgeBase.md) – Zahlenkorsett (1.000 AE Start, ~300 AE Harvester-Ladung)

## MS-1-Override (D-056)

Der verbindliche MS-1-Loop ist ein lokales Solo-Skirmish Mensch gegen KI auf
Glutrinne:

`Sammeln → Bauen → Produzieren → Angreifen → Kontrollieren → Gewinnen`

Ziel bleiben 20–35 Minuten. Artillerie öffnet statische Situationen; endliche,
nachwachsende und durch Überernte dauerhaft schädigbare Aetherium-Felder
erzwingen Expansion und bilden gemeinsam den Finisher. T3, Superwaffen,
Evolvierte, Luft, Neutrale, Capture, Wetter, Online-Modi und weitere Karten
sind Post-MVP. Die exakte Inhaltsgrenze steht in
[MVPContentManifest.md](../production/MVPContentManifest.md); bei Widerspruch
übersteuert dieser Abschnitt den nachfolgenden Vollspiel-Loop.

## Der Kernloop

```
Sammeln → Bauen → Produzieren → Angreifen → Kontrollieren → Gewinnen
   ↑                                                        |
   └────────── Map-Kontrolle sichert neue Felder ◄──────────┘
```

Jeder Schritt ist eine spielerische Entscheidung mit sichtbarem Feedback; der Loop dreht sich mehrfach pro Match und steigert sich in Skala und Risiko.

| Schritt | Spielerentscheidung | Systemantwort | Relevante Entscheidung |
|---|---|---|---|
| Sammeln | Harvester zuweisen; aggressiv am Rand ernten (Überernte-Risiko) oder nachhaltig | AE-Einkommen steigt; Feld breitet sich aus oder Mutterkristall nimmt Schaden | D-010 |
| Bauen | Welcher der 12 Gebäudetypen, wann, wo; Evolvierte: Keim-Standort nahe Aetherium wählen (Reifebeschleunigung) | Basis wächst, Energiebilanz kippt (Low-Power-Regel) | D-008, D-011 |
| Produzieren | Einheiten-Mix, Produktions-Queues, Energiepriorisierung | Armee formiert sich; Warteschlangen und Sammelpunkte | D-014, D-015 |
| Angreifen | Zeitpunkt und Ziel des Vorstoßes; Feindliche Lager als Nebenziele (Belohnung) | Territorium und Felder wechseln Besitzer; Zerstörung sichtbar (D-012) | D-012, D-016 |
| Kontrollieren | Radar, Scouts/Drohnen, capturebare Geschütztürme, Brücken sichern | Sicht und Map-Druck; Gegner wird von Feldern abgeschnitten | D-014, D-016 |
| Gewinnen | Finalschlag oder wirtschaftlicher Erstickungssieg | Siegbedingung erfüllt (s. VictoryConditions.md) | D-018 |

Der Loop ist bewusst **zyklisch, nicht linear**: Nach jedem Angriff folgt Wiederaufbau und Neusammeln. Die Aetherium-Felder (D-010) sind der Motor, der den Loop in Bewegung hält – sie wachsen, verändern das Terrain und werden knapp.

## Match-Phasen (Ziel-Matchdauer 20–35 min, v0.1-Richtwerte)

Zeitfenster gelten für 1v1 auf Kartengröße S/M (D-017) und sind Startwerte zum Tunen – Abweichungen ±25 % je nach Fraktionsmatchup sind akzeptabel. FFA und Koop (D-018, ab Alpha; Alpha-FFA läuft lokal gegen KI, Online-Modi frühestens Beta, D-025) laufen erfahrungsgemäß 20–40 % länger.

| Phase | Zeitfenster | Typischer Zustand | Spielerfrage |
|---|---|---|---|
| Eröffnung | 0:00–4:00 | HQ + Startressourcen 1.000 AE, 1 Harvester-Kette auf Mutterkristall, Kaserne, erste Scout-Runde | "Greife ich früh an oder baue ich Wirtschaft?" |
| Expansion | 4:00–10:00 | Zweite Raffinerie an Ausläufern, Kraftwerk-Kette, Tier 2 freigeschaltet, erste Scharmützel um Feldränder | "Wo expandiere ich, ohne überdehnt zu sein?" |
| Midgame | 10:00–20:00 | Tier 2 ausgebaut, Fahrzeug-/Luftschlachten, Ausbreitung verändert Wege und Deckung, erste Elite-Einheit (Tier 3 gegen Phasenende) | "Druck machen oder auf Tier 3 / Superwaffe sparen?" |
| Endgame | 20:00–35:00 | Tier 3, Elite-Einheiten (Limit 1–2, D-015), Superwaffe geladen, Felder großteils übererntet oder erschöpft | "Finalschlag jetzt – oder Feld-Vorteil zum Ersticken nutzen?" |

Eskalations-Zielmarken (Startwerte v0.1, begründet über den Einkommenspfad):

- **Tier 2 ~6:00–8:00:** bei normalem Ausbau (2 Raffinerien) erreichbar; Rush verzögert auf ~9:00–10:00. Begründung: Forschungslabor + Tier-2-Forschung sollen zusammen ~2.500–3.500 AE kosten, das entspricht ~8–12 Harvester-Ladungen à ~300 AE.
- **Tier 3 ~14:00–18:00:** erfordert etablierte Expansion; Tier-3-Einheiten erscheinen ab ~16:00, Elite-Einheit frühestens ~18:00 (D-015).
- **Superwaffe einsatzbereit ~22:00–26:00:** Bauzeit + Ladezeit zusammen ~5–7 min nach Tier-3-Abschluss; sie beendet Matches, die "zu lange" stocken, ohne sie allein zu entscheiden.
- **Wirtschaftsknick ~24:00:** Mutterkristalle ohne Ausbreitungs-Management sind übererntet geschwächt; wer nicht kontrolliert, verliert Einkommen – der Taktgeber ins Endgame (D-010).

## Spannungsbögen

Pro Match sind drei Bögen intendiert:

1. **Aufbau-Spannung (Eröffnung):** Knappheit. 1.000 AE reichen nicht für alles – jede frühe Entscheidung hat Opportunitätskosten (Scout vs. zweiter Harvester vs. Kaserne). Erster Kontakt idealerweise bei 2:00–3:30.
2. **Positions-Spannung (Expansion/Midgame):** Umklammerung. Felder, Brücken, neutrale Geschütztürme (D-016) und die wachsende Ausbreitung (D-010) erzeugen Frontlinien, die sich verschieben. Hin-und-Her ist gewünscht: Kein Angriff soll vor ~10:00 match-entscheidend sein (Anti-Rush: Verteidigungsplattform-Module MG/Flak/Rakete sind in Tier 1 verfügbar, D-008).
3. **Vollzugs-Spannung (Endgame):** Countdown. Superwaffen-Timer, Elite-Einheiten und schwindende Felder erzeugen eine Uhr, gegen die beide Seiten spielen. Das Match "will" zwischen 20:00 und 35:00 enden.

Comeback-Mechanismen (verhindern frühe Aufgaben, stützen H1-Solo-Erlebnis, D-007):

- **Überernte als Risiko-Ressource:** Der Rückliegende kann sein Restfeld aggressiv überernten – kurzfristiger AE-Schub gegen dauerhaften Mutterkristall-Schaden. Die Härte der Bestrafung ist verbindlich über das **4-Stufen-Modell in [../gamedesign/Resources.md](../gamedesign/Resources.md)** definiert (Strapaziert → Ausgeblutet → Degeneriert, D-010); dieses Dokument führt keine eigene abweichende Regel. Bewusstes Glücksspiel, keine Gratis-Rettung.
- **Neutrale Lager (D-016):** Objective-Belohnungen geben dem Verfolger Alternativen zum Frontalangriff.
- **Elite-Einheiten (D-015):** Limit 1–2 sorgt für Wendepunkt-Momente ohne Snowball-Flut.
- **Superwaffe:** teuer und sichtbar – Limit 1 pro Spieler mit globaler Bau-Ansage (D-023); der Führende muss investieren, der Rückliegende kann sie sabotieren (Zerstörung im geladenen Zustand = 25-%-Effekt am eigenen Standort, D-023).

## Anti-Stall-Mechanismen

Stall = beide Seiten bunkern, Einkommen stabil, kein Druck. D-010 ist die primäre Antwort, ergänzt durch Map-Design:

| Mechanismus | Wirkung gegen Stall | Quelle |
|---|---|---|
| Feldausbreitung | Kristall kriecht Richtung unerschlossener Gebiete; Terrainveränderung öffnet/schließt Wege – statische Verteidigungslinien veralten | D-010 |
| Überernte-Verfall | Wer nur hinter Mauern sitzt, beschädigt seinen Mutterkristall dauerhaft und verliert langfristig Einkommen | D-010; Stufenmodell: [../gamedesign/Resources.md](../gamedesign/Resources.md) |
| Endliche Mutterkristalle | Einkommen pro Feld sinkt über das Match; Verweigerung von Expansion = langsamer Tod | D-010 |
| Neutrale Geschütztürme (capturebar) | Map-Mitte hat Zähne; Passivität verschenkt sie | D-016 |
| Superwaffen-Timer | Ab ~22:00 existiert eine harte Abschlussdrohung | D-008 |
| Sichtdruck | Radar/Scouts (D-014) machen Turtling aufklärbar und artillerie-anfällig | D-008, D-014 |
| Timeout-Regel | Letztes Netz: optionales Zeitlimit mit Remis-/Wertungsregel | s. [../gamedesign/VictoryConditions.md](../gamedesign/VictoryConditions.md) |

Explizit **keine** Anti-Stall-Maßnahmen: keine plötzlichen Ressourcen-Influx-Events, keine Sudden-Death-Damage-Aura (wäre ein Bruch mit der D-010-Taktgeber-Philosophie und dem H1-Erwartungsprofil, D-007).

## Modus-Notizen (D-018)

- **Solo-Skirmish vs. KI (MVP):** Dieses Dokument ist dafür kalibriert; die KI muss die Phasen-Zielmarken als Verhaltensanker bekommen (s. [../research/KI_Architektur.md](../research/KI_Architektur.md)).
- **Koop vs. KI / FFA (ab Alpha):** Alpha-FFA läuft **lokal gegen KI-Mitspieler**; alle Online-Modi (Koop online, PvP) frühestens Beta, abhängig vom Q-013-Ausgang (D-025). Phasenfenster strecken sich; Anti-Stall läuft über geteilte/konkurrierende Felder.
- **PvP/Survival (ab Beta):** Survival ersetzt den Endgame-Bogen durch Wellen-Eskalation; Details in [../gamedesign/MultiplayerModes.md](../gamedesign/MultiplayerModes.md) (Sprint 2, parallel).

## Offene Punkte

- **Balancing-Simulationsblatt:** Die Zeitfenster oben sind aus dem Zahlenkorsett (1.000 AE Start, ~300 AE/Ladung) abgeleitete Richtwerte – erst Playtests können sie bestätigen. Ob in Sprint 2 zusätzlich ein formales Simulationsblatt (AE/min-Kurven pro Phase) in Balancing.md gefordert wird, ist **weiterhin offen** (Entscheidung Game Director).
- **Überernte-Härte:** entschieden (Korrekturlauf Sprint 2) – verbindlich ist das 4-Stufen-Modell in [../gamedesign/Resources.md](../gamedesign/Resources.md) (Strapaziert/Ausgeblutet/Degeneriert, permanente Schäden gemäß D-010); dieses Dokument führt kein eigenes A/B-Modell mehr.
- **Ausbreitungsgeschwindigkeit pro Biom:** entschieden (Korrekturlauf Sprint 2) – sie variiert pro Biom über den `AetheriumSpreadModifier` in [../gamedesign/Biomes.md](../gamedesign/Biomes.md) (−25 % bis +50 %); die daraus resultierende Matchdauer-Drift wird im Balancing getunt (Balancing.md), nicht über dieses Dokument.

## Nächste Schritte

- Konsistenzabgleich der Zeitmarken mit Economy.md und Balancing.md am Ende von Sprint 2.
- KI-Phasenanker (Eröffnungs-Build-Orders pro Fraktion) als Input für das KI-Design (Sprint 2/3).
- Playtest-Checkliste für Sprint 7: Rush-Resistenz < 10:00, Stall-Rate, tatsächliche Matchdauer-Verteilung vs. Zielkorridor 20–35 min.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Gameplay Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030): D-025-Klarstellung (Alpha-FFA lokal vs. KI, Online-Modi ab Beta), Überernte-Härte an 4-Stufen-Modell aus Resources.md referenziert, Biom-Ausbreitungsmodifier an Biomes.md referenziert, Superwaffen-Limit 1 (D-023) ergänzt | Lead Gameplay Designer |
| 0.3.0 | 2026-07-24 | Geschlossenen MS-1-Kernloop mit Artillerie und endlichen Aetherium-Feldern als Finisher gemäß D-056 festgelegt | Lead Gameplay Designer |
