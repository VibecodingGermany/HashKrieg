# Factions – Fraktions-Masterdokument

**Version:** 0.3.0 | **Status:** Entwurf – MS-1-Override verbindlich | **Verantwortungsbereich:** Lead Gameplay Designer | **Sprint:** 2

## Zweck

Zentrales Fraktions-Masterdokument: definiert Identität, Spielstil-Versprechen, Stärken/Schwächen, Wirtschafts-Besonderheiten, militärische Doktrin, Superwaffen-Rahmen und visuelle Identität der drei Fraktionen (Allianz, Legion, Evolvierte). Es ist die Design-Klammer über den Detaildokumenten – alle Einheiten-, Gebäude- und Wirtschaftswerte in den Fachdokumenten müssen mit den hier definierten Richtwerten und Doktrinen konsistent sein. Keine Implementierung; alle Zahlen sind Startwerte v0.1 zum Tunen.

## Abhängigkeiten

- Verbindliche Entscheidungen: [../production/DecisionLog.md](../production/DecisionLog.md) (D-007, D-008, D-010, D-011, D-013, D-014, D-015, D-019, D-020, D-023, D-027)
- Wissensbasis: [../analysis/KnowledgeBase.md](../analysis/KnowledgeBase.md)
- Markt-/Zielgruppen-Research: [../research/RTS_Markt_Wettbewerb.md](../research/RTS_Markt_Wettbewerb.md)
- Fachdokumente (Sprint 2, hier referenziert): ./Infantry.md, ./Vehicles.md, ./Aircraft.md, ./Buildings.md, ./Economy.md, ./Resources.md, ./ResearchTree.md, ./NeutralUnits.md, ./Campaign.md

## MS-1-Override (D-056)

MS-1 enthält nur Allianz und Legion. Die Evolvierten sowie ihre
Wachstums-/Regenerationsmechaniken sind Post-MVP. Fraktionsidentität entsteht
im geschlossenen Core ausschließlich lokal über Waffen- und
Wirtschaftsdefinitionen:

- Allianz: höhere Kosten, Präzision und Einzelschaden; 330 AE
  Harvester-Kapazität.
- Legion: niedrigere Kosten, schnellere Produktion, Salven und
  Flächenschaden; 300 AE Harvester-Kapazität.

Aktive Umschalter und ein generisches Fähigkeiten-/Statussystem sind nicht
erforderlich. Der exakte Roster steht in
[MVPContentManifest.md](../production/MVPContentManifest.md); der nachfolgende
Drei-Fraktionen-Entwurf bleibt Post-MVP-Zielbild.

## Asymmetrie-Grundsatz

Gemäß D-008 haben alle drei Fraktionen **dieselben 12 Gebäudetypen** (HQ, Kraftwerk, Raffinerie, Lager, Kaserne, Fahrzeugfabrik, Flugfeld, Forschungslabor, Radar, Verteidigungsplattform, Mauer, Superwaffe) und dieselben Einheiten-Kategorien (8 Infanterie, 12 Fahrzeuge, 7 Luftfahrzeuge, 2–3 Drohnen). Asymmetrie entsteht ausschließlich über:

1. **Werte-Asymmetrie:** Kosten, Bauzeit, HP, Schaden, Reichweite, Geschwindigkeit weichen je Fraktion um definierte Faktoren ab.
2. **Mechanik-Asymmetrie:** wenige, klar benennbare Sonderregeln pro Fraktion (Evolvierte: Wachstumsbauweise/Regeneration gemäß D-011; Allianz: Schild-/Präzisionseffekte; Legion: Flächenschaden/Zahlvorteil).
3. **Wirtschafts-Asymmetrie:** unterschiedliche Interaktion mit der Aetherium-Hybridwirtschaft (D-010).

Kein Konter-Design über exklusive Einheitenkategorien; Konter entsteht aus Rollen innerhalb der gemeinsamen Kategorien.

### Richtwerte-Klassen (v0.1, Konsistenzanker für alle Fachdokumente)

| Anker | Allianz | Legion | Evolvierte |
|---|---|---|---|
| Kosten-Niveau (rel. zu Legion = 1,0) | 1,25–1,4 | 1,0 | 1,1–1,25 |
| Bau-/Reifezeit-Niveau | 1,0 | 0,8–0,9 | 1,1–1,3 |
| HP-Niveau | 1,0–1,15 | 0,85–1,0 | 0,9–1,0 + Regeneration |
| Schaden-Niveau | 1,15–1,3 (präzise) | 0,9–1,1 (Fläche) | 1,0–1,15 (DoT/Debuff) |
| Stückzahl im typischen Angriffstrupp | klein (5–10) | groß (12–25) | mittel (8–15) |

Begründung der Richtwerte: Allianz muss sich "teuer aber wirksam" anfühlen (GDD-O-Identität), Legion "billig und viele" – bei 20–35 Min. Matchdauer (D-010) muss die Legion ihren Mengenvorteil früh realisieren können, während die Allianz auf Tech-/Qualitätsvorsprung im Mid-/Late-Game setzt.

## Fraktion: Allianz

**Identität/Fantasy:** High-Tech-Militärbündnis – kühle Präzision, gebündelte Energie, saubere Linien. Farben Azurblau/Stahlgrau. Fantasy: "Jede Einheit ist eine Investition."

**Spielstil-Versprechen:** Wenige, teure, hochwertige Einheiten mit starker Synergie (Radaraufklärung + Reichweite + Präzision). Wer Mikromanagement und Positionierung mag, wird belohnt; wer Einheiten verschwendet, verliert ökonomisch.

**Militärische Doktrin:** Qualität vor Quantität. Lange Reichweiten, starke Einzelziele, aktive Fähigkeiten (z. B. Zielmarkierung, Überladung). Schwach in frühem Spam; stark ab Tier 2/3 mit Forschungs-Upgrades.

**Wirtschafts-Besonderheiten:** Höchste Harvester-Effizienz pro Fahrt (Richtwert: ~300 AE Standardladung, Allianz ~330 AE dank Sammel-Upgrades), aber teuerste Infrastruktur – ein verlorener Harvester schadet überproportional. Allianz spielt defensiv um ihre Felder; Überernte-Risiko (D-010) wird eher vermieden als ausgenutzt.

**Superwaffe – Ionenstrahl:** Gebündelter Orbital-/Energiestrahl, sehr hoher Schaden auf kleinem, präzisem Zielbereich (Gebäude-Killer, Elite-Einheit-Konter). Einsatzlogik: chirurgischer Schlag gegen Schlüsselgebäude (Superwaffe, Forschungslabor) oder Elite-Einheiten; ineffizient gegen breit gestreute Massen – spiegelt die Fraktionsidentität. Effekt-Rahmen (Fläche, Schadenskurve, Cooldown) wird in ./Buildings.md spezifiziert.

**Einheiten-/Gebäude-Philosophie:** Elite-Fokus: MVP-Eliteeinheit "Titan-Mech" (D-015). Drohnen: Scout-, Repair-, Kampfdrohne (D-014). Gebäude kompakt, gepanzert, vertikal – hohe Energieabhängigkeit macht die Low-Power-Regel (Produktion −50 %, Radar/Verteidigung offline) für die Allianz besonders schmerzhaft: Kraftwerke sind ihr strategischer Schwachpunkt. Details: ./Infantry.md, ./Vehicles.md, ./Aircraft.md, ./Buildings.md.

**Silhouetten-Regeln:** Eckig-präzise, klare Kanten, vertikale Akzente (Antennen, Geschütztürme), glatte Panzerflächen; Azurblau als Primärfarbe an Panzerkanten/Leuchtelementen, Stahlgrau als Grundton. Einheiten müssen auf Top-Down-Perspektive (D-019) an ihrer Turm-/Geschützform erkennbar sein.

**Konter-erzwingende Schwächen:**
- Hohe Kosten → früher Druck (Legion-Rush) zwingt die Allianz auf Verteidigungsplattformen statt Expansion.
- Energieabhängigkeit → Angriffe auf Kraftwerke deaktivieren Radar und Verteidigung (Low-Power-Regel).
- Geringe Stückzahlen → Flächenschaden (Legion-Flammen/Raketen, Evolvierte-Kristallsturm) trifft überproportional.

## Fraktion: Legion

**Identität/Fantasy:** Industrialisierte Kriegsmaschinerie – Masse statt Klasse, Rost, Rauch, Flammen und Raketen. Farben Rostrot/Ocker. Fantasy: "Quantität ist eine eigene Qualität."

**Spielstil-Versprechen:** Günstige, schnell produzierte Einheiten in großen Verbänden; Druck ab Minute 1. Wer mapweite Überwältigung und brutale Direktheit mag. Verluste sind einkalkuliert – der Nachschub ist die Strategie.

**Militärische Doktrin:** Überwältigung durch Zahl und Flächenschaden. Flammenwaffen gegen Infanterie und brennbare Vegetation (nutzt D-012-Zerstörbarkeit taktisch: Wälder abfackeln, Sicht-/Deckungslinien zerstören), Raketenschwärme gegen Gebäude und Fahrzeuge. Schwach gegen Reichweiten-/Präzisionskonter und Luftüberlegenheit. **Bewusst ohne Infanterie-Heiler (D-027):** Die Legion erhält keine Heil-Infanterie – Verluste werden über günstige, schnelle Neuproduktion ausgeglichen (Masse-Identität), nicht über Sustain.

**Wirtschafts-Besonderheiten:** Billigste Infrastruktur und kürzeste Bauzeiten (siehe Richtwerte-Tabelle) → früheste zweite Raffinerie, aggressivste Expansion auf neue Aetherium-Ausläufer (D-010). Legion toleriert Überernte am stärksten: Ein dauerhaft geschädigter Mutterkristall ist für sie akzeptabler Tausch gegen sofortigen Mengenvorteil – das ist ihr bewusster "Burn-and-move"-Wirtschaftsstil.

**Superwaffe – Thermobarbombe:** Thermobarische Detonation mit großem Flächenradius und anhaltendem Feuer-Nachbrenneffekt; zündet brennbare Vegetation/Dekor (Synergie mit D-012). Einsatzlogik: Armee-Zermalmer und Basis-Öffner – löscht Truppenkonzentrationen und weicht Verteidigungsringe auf; ineffizient gegen einzelne hochwertige Ziele (Konterbild zur Allianz). Effekt-Rahmen in ./Buildings.md.

**Einheiten-/Gebäude-Philosophie:** Elite-Einheit MVP "Mobile Festung" (D-015) – langsamer, schwer gepanzerter Truppenträger/Belagerer als Anker der Massen. Drohnen: Scout-, Repair-, Kampfdrohne (D-014). Gebäude massiv, niedrig, weit gestreut (Flächenbombardement-resilient); günstige Mauern und Verteidigungsplattformen erlauben "verbrannte Erde"-Verteidigung. Details: ./Infantry.md, ./Vehicles.md, ./Aircraft.md, ./Buildings.md.

**Silhouetten-Regeln:** Wuchtig, horizontal gestreckt, viele Rohre/Raketenständer/Schornsteine; unregelmäßige Panzerplatten, sichtbare Nieten; Rostrot/Ocker großflächig, Ruß-Akzente. Auf Top-Down-Perspektive an Masse und Raketen-/Flammenwerfer-Profilen erkennbar.

**Konter-erzwingende Schwächen:**
- Schwache Einzelwerte → verliert Materialschlachten gegen Allianz-Qualität, wenn der Mengenvorteil nicht früh erzwungen wird.
- Luftschwäche (Richtwert: Legion-Luftfahrzeuge günstig, aber schwach; Flak-Abhängigkeit) → Allianz-Luftüberlegenheit zwingt Flak-Module auf Verteidigungsplattformen.
- Überernte-Stil → dauerhaft geschädigte Felder (D-010) machen späte Spiele ökonomisch riskant; Legion muss vor ~25 Min. entscheiden oder ständig expandieren.

## Fraktion: Evolvierte

**Identität/Fantasy:** Biologisch mutierte Kristallwesen – Symbiose aus Organismus und Aetherium, organisch-kristalline Formen, Regeneration. Farben Violett/Bio-Grün. Fantasy: "Die Basis ist ein lebender Organismus; der Kristall ist Heimat und Nahrung."

**Spielstil-Versprechen:** Territoriale, wachsende Macht: Basen keimen und reifen, Einheiten heilen sich selbst, die Fraktion wird stärker, je länger sie ein Gebiet hält. Wer Positions- und Territorialspiel mag und Geduld mitbringt.

**Militärische Doktrin:** Abnutzung und Terrainbindung. Regeneration (Einheiten und Gebäude heilen über Zeit statt Reparatur) belohnt gestaffelte Gefechte und Rückzug; Schaden-über-Zeit und Debuffs (Kristallsplitter, Verlangsamung) statt Frontalstößen. Schwach im offenen Schnellangriff, stark in der Verteidigung etablierter Gebiete. **EMP-Immunität (D-027):** Als Bio-Fraktion sind Evolvierte immun gegen EMP-Effekte – die Alternative "EMP wirkt normal" wurde verworfen, weil sie die EMP-Waffe zum Evolvierten-Konter ohne Gegenwert gemacht hätte.

**Wirtschafts-Besonderheiten (D-011):** Gebäude werden als **Keim gepflanzt** und **reifen über Zeit** (statt klassischer Bauanimation); **Aetherium-Nähe beschleunigt die Reifung** – Evolvierte bauen naturgemäß nahe an Feldern und kontrollieren diese räumlich enger als andere Fraktionen. **Regeneration statt Reparatur:** Gebäude heilen langsam selbst (kein Reparatur-UI/-Befehl). Einheiten regenerieren außerhalb des Kampfes (Richtwert: volle HP nach ~30–60 s ohne Schaden – Tunen gegen "unkaputtbare" Hit-and-Run-Loops). Evolvierte profitieren direkt von der Feldausbreitung (D-010): Ihre Wirtschaft und ihr Bau-Tempo skalieren mit dem Kristallwachstum, Überernte schadet ihnen daher doppelt (Einkommen + Baugeschwindigkeit).

**Superwaffe – Kristallsturm:** Ein wachsender Sturm aus Kristallsplittern über einem Zielgebiet: anfangs moderater Flächenschaden, dann anhaltender DoT plus Verlangsamung; hinterlässt temporär kristallisiertes Gelände. **Interaktion mit Aetherium-Feldern (D-027):** Auf lebenden Feldern erhält der Kristallsturm verstärkte Reichweite und Dauer (USP-Moment; die Alternative "rein destruktiv" wurde verworfen). Die Interaktion unterliegt einer **Balancing-Beobachtungspflicht** – bei Dominanz werden Reichweiten-/Dauer-Boni in ./Buildings.md nachjustiert. Einsatzlogik: Gebietsverwehrung – sperrt Engpässe, bricht Belagerungen, bestraft statische Truppenansammlungen (Konter zu Legion-Massen und Allianz-Artilleriestellungen). Effekt-Rahmen und Dauer in ./Buildings.md.

**Einheiten-/Gebäude-Philosophie:** Elite-Einheit MVP "Alpha-Mutant" (D-015) – eine **Infanterie**-Elite; diese Elite-Asymmetrie (Allianz/Legion fahren Fahrzeug-Eliten) ist **gewollt (D-027)** und wird über die Release-Eliten (3 pro Fraktion, D-015) ausgeglichen. Bio-Drohnen-Äquivalente über Sporen-Schwärme (D-014): Späher-Schwarm, Heilschwarm (ersetzt Repair), Kampfschwarm – produziert in Fahrzeugfabrik/Flugfeld-Äquivalenten. **Heilschwarm-Regel (D-027):** Der Heilschwarm stapelt **nicht** auf die passive Regeneration – er leistet nur aktive Heilung; Einheiten unter Heilschwarm-Wirkung regenerieren nicht zusätzlich passiv. Gebäude wirken gewachsen (Stacheln, Kristalladern, pulsierende Kerne); Mauern als Kristallbarrieren. Details: ./Infantry.md, ./Vehicles.md, ./Aircraft.md, ./Buildings.md.

**Silhouetten-Regeln:** Organisch-asymmetrisch, Stacheln und Kristallwuchs als Erkennungsmerkmal, keine geraden Kanten; Violett als Grundton, Bio-Grün als Leucht-/Ader-Akzent. Auf Top-Down-Perspektive an kristallinen Auswüchsen und krabbelnden/kriechenden Bewegungsprofilen erkennbar – deutlichster Silhouetten-Kontrast zu den beiden technischen Fraktionen.

**Konter-erzwingende Schwächen:**
- Reifung statt Bau → Gebäude im Aufbau sind lange verwundbar; frühe Raids auf Keime verzögern die gesamte Basis (Legion-Rush ist ihr Alptraum-Szenario).
- Keine Reparatur → gezielter Dauerschaden (Legion-Feuer-Nachbrennen) kontert Regeneration; Fokusfeuer übersteigt den Heilungsdurchsatz.
- Aetherium-Bindung → fernab von Feldern reift langsam und regeneriert schwach; wer Felder zerstört (D-012: Felder sind zerstörbar), schwächt Wirtschaft und Verteidigung zugleich.

## Fraktions-Narrative und Kampagne (D-020)

Die lineare Solo-Kampagne ist **Phase 3** (kein MVP-/Alpha-Umfang): 3 Akte, 12–15 Missionen, **je Akt eine Fraktionsperspektive** (Akt I Allianz, Akt II Legion, Akt III Evolvierte). Die hier definierten Fraktions-Identitäten, Doktrinen und Konter-Profile sind die narrative und spielerische Grundlage der Akt-Dramaturgie; die **Fraktions-Commander fungieren als Erzählstimmen** (Briefings, In-Mission-Funk, Portrait + Voice – rein narrativ-präsentativ, ohne Match-Mechanik, D-009). Verbindlicher Konzeptrahmen: ./Campaign.md; Commander-Identitäten: ./CommanderSystem.md.

## Superwaffen-Rahmen (D-023)

Für alle drei Fraktionen verbindlich:

- **Baulimit:** max. **1 Superwaffen-Gebäude pro Spieler** – Lesbarkeit und Endspiel-Dramaturgie; unbegrenzte Superwaffen degradieren sie zum Wirtschafts-Spam.
- **Globale Bau-Ansage:** Der Baubeginn einer Superwaffe wird allen Spielern angekündigt (C&C-Konvention, H1-Zielgruppe).
- **Rückschlag bei Zerstörung:** Wird eine **geladene** Superwaffe zerstört, entlädt sich der Effekt mit **25 % Stärke am eigenen Standort** (Sabotage-Anreiz, Comeback-Mechanik).

Ladezeiten, Abklingzeiten und Effekt-Detailwerte: ./Buildings.md (§2.12, §5); Waffenwirkung: ./Weapons.md.

## Stärken/Schwächen-Matrix (Gesamtübersicht)

| Dimension | Allianz | Legion | Evolvierte |
|---|---|---|---|
| Frühspiel-Druck | schwach | **sehr stark** | mittel |
| Mid-Game-Tempo | mittel | stark | schwach (Reifung) |
| Late-Game-Stärke | **sehr stark** | mittel | stark (etabliertes Gebiet) |
| Ökonomie-Robustheit | verwundbar (teuer) | robust (billig, expandiert) | gebunden an Felder |
| Luft-Stärke | **stark** | schwach | mittel |
| Flächenschaden | schwach | **stark** | mittel (DoT) |
| Präzisions-/Einzelzielschaden | **stark** | schwach | mittel |
| Basis-Verteidigung | stark (bis Power-Verlust) | stark (günstig, Masse) | **sehr stark** (Regeneration) |
| Mobilität/Map-Control | mittel | stark (Masse überall) | schwach (territorial) |
| Risiko-Profil | Verluste unerschwinglich | Verluste egal, Zeit ist Feind | Felder verlieren = doppelt bestraft |

## Konter-Dreieck (Design-Leitplanke)

- **Legion schlägt Allianz früh** (Masse überrennt teure Startverteidigung) – Allianz kontert mit Verteidigungsplattformen + Radaraufklärung.
- **Allianz schlägt Evolvierte mittel** (Präzision + Reichweite zerstört Keime und Felder aus sicherer Distanz) – Evolvierte kontern mit Kristallsturm auf Artilleriestellungen.
- **Evolvierte schlagen Legion spät** (Regeneration + DoT verwässert Massen-Anstürme) – Legion kontert mit Feuer-Nachbrennen und Feldzerstörung.
- Kein hartes Stein-Schere-Papier: das Dreieck ist zeitachsen-abhängig (früh/mittel/spät), nicht einheitenabhängig – verbindliche Balance-Leitplanke für ./Infantry.md, ./Vehicles.md, ./Aircraft.md.

## Zahlenanker (verbindlich für alle Fachdokumente)

| Anker | Wert | Quelle |
|---|---|---|
| Infanterie / Fahrzeuge / Luftfahrzeuge pro Fraktion | 8 / 12 / 7 | ZahlenGerüst v0.1 |
| Drohnen pro Fraktion | 2–3 (Support: Scout/Repair-Äquivalent/Kampf) | D-014 |
| Elite-Einheiten | 1 (MVP/Alpha) → 3 (Release), Tier 3, Limit 1–2 | D-015 |
| Währung | AE (Aetherium-Einheiten) | ZahlenGerüst |
| Standard-Startressourcen | 1.000 AE | ZahlenGerüst |
| Harvester-Standardladung | ~300 AE | ZahlenGerüst |
| Tech-Tiers | 1–3 | ZahlenGerüst |
| Low-Power-Regel | Defizit → Produktion −50 %, Radar/Verteidigung offline | ZahlenGerüst |
| Superwaffen-Limit | 1 pro Spieler, globale Bau-Ansage, 25-%-Rückschlag bei Zerstörung (geladen) | D-023 |

Alle Werte sind datengetrieben als flache Datensätze (ScriptableObject-tauglich) zu spezifizieren; fraktionsspezifische Werte in den Fachdokumenten als konkrete Zahlen mit Verweis auf die Richtwerte-Klassen oben.

## Offene Punkte

1. ~~**Kristallsturm ↔ Aetherium-Felder**~~ – **entschieden (D-027):** Kristallsturm interagiert mit Aetherium-Feldern (verstärkte Reichweite/Dauer auf Feldern), mit Balancing-Beobachtungspflicht. Eingearbeitet im Abschnitt "Superwaffe – Kristallsturm".
2. ~~**Superwaffen-Baulimit**~~ – **entschieden (D-023):** Limit 1 pro Spieler, globale Bau-Ansage, 25-%-Rückschlag bei Zerstörung im geladenen Zustand. Eingearbeitet im Abschnitt "Superwaffen-Rahmen (D-023)".
3. ~~**Evolvierte-Reparatur-Äquivalent für D-014 (Heilschwarm)**~~ – **entschieden (D-027):** Der Heilschwarm stapelt nicht auf passive Regeneration (nur aktive Heilung). Eingearbeitet in der Evolvierte-Einheiten-Philosophie.
4. **Legion-"Burn-and-move"-Wirtschaft:** Der hier definierte Überernte-Stil der Legion nutzt die D-010-Regel strategisch aus. Status: **offen, Beobachtungsauftrag ans Balancing** – falls die Überernte-Mechanik zum dominanten Legion-Spielmuster wird (Monostil-Risiko), Feintuning der Überernte-Schadenswerte in ./Resources.md.
5. **Kein Marines-Konterprofil nötig:** D-013 (keine Marine, Wasser nur Terrain-Feature) wurde berücksichtigt; keine Fraktion erhält wasserbezogene Stärken/Schwächen. Nur als Konsistenzvermerk, kein Klärungsbedarf.

## Nächste Schritte

- Detailwerte je Kategorie in ./Infantry.md, ./Vehicles.md, ./Aircraft.md, ./Buildings.md gegen die Richtwerte-Klassen und das Konter-Dreieck ausarbeiten.
- Superwaffen-Effekt-Rahmen (Fläche, Schaden, Cooldown, Freischaltung) in ./Buildings.md konkretisieren – Rahmenbedingungen sind mit D-023 entschieden (siehe "Superwaffen-Rahmen").
- Wirtschaftsinteraktion (Reifungsbeschleunigung, Überernte-Profile) mit ./Economy.md und ./Resources.md abstimmen.
- Konsistenzreview am Sprintende: Matrix und Konter-Dreieck gegen alle Fachdokumente prüfen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Gameplay Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Lead Gameplay Designer |
| 0.3.0 | 2026-07-24 | MS-1 auf Allianz und Legion mit lokaler Waffen-/Wirtschaftsasymmetrie gemäß D-056 begrenzt | Lead Gameplay Designer |
