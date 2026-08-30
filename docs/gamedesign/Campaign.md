# Kampagne (Phase 3 – Konzept)

**Version:** 0.2.0 | **Status:** Entwurf (Korrekturlauf Sprint 2) | **Verantwortungsbereich:** Game Director | **Sprint:** 2

## Zweck

Konzept-Rahmen für die Singleplayer-Kampagne von Hashkrieg. Die Kampagne ist **verbindlich Phase 3 / Post-MVP** (D-020; MVP ist Solo-Skirmish 1v1 vs. KI, gemäß D-018) und wird in diesem Sprint nur dokumentarisch festgelegt, damit Commander (D-009), Fraktionen, Mechaniken und Welt konsistent auf sie hin angelegt werden. Kein Implementierungsauftrag.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) (D-007 Premium/SP-first/H1-Zielgruppe, D-009 Commander als Identitäts-Layer, D-010 Aetherium-Wirtschaft, D-011 Evolvierte-Bauweise, D-018 Modi-Staffelung, D-019 Kamera)
- ./CommanderSystem.md (Commander-Identitäten, Voice-Konzept)
- ./Factions.md (Fraktions-Stammdaten), ./Economy.md, ./Buildings.md, ./ResearchTree.md
- ./Maps.md, ./Biomes.md (Schauplätze, Hazards)
- ./Balancing.md (Standard-Wertesets, Skript-Overrides)
- [../research/RTS_Markt_Wettbewerb.md](../research/RTS_Markt_Wettbewerb.md) (Kampagne als Kaufargument für H1)

## Rahmen

| Parameter | Festlegung | Begründung |
|---|---|---|
| Umfang | **Eine Kampagne, 3 Akte, ~12–15 Missionen** (4–5 pro Akt) | C&C-Erwartung der H1-Zielgruppe; überschaubarer Scope für Studio-Kapazität |
| Perspektive | **Je Akt ein Perspektivwechsel:** Akt I Allianz, Akt II Legion, Akt III Evolvierte | Drei Fraktionen werden alle spielbar erzählt; Lernkurve verteilt sich auf Akte |
| Spieldauer | Ziel ~8–12 Stunden Gesamt (30–50 min je Mission) | Premium-Anker ~30–40 € (D-007); deckt sich mit H1-Referenztiteln |
| Reihenfolge | **Linear: Akt I → II → III (verbindlich, D-020)** | Eskalierende Story, kontrollierte Mechanik-Einführung |
| Schwierigkeitsgrade | 3 Stufen, an die KI-Difficulty-Profile gekoppelt ([../research/KI_Architektur.md](../research/KI_Architektur.md)) | Keine Ressourcenboni, konsistent mit Skirmish |
| Speichern | Speichern jederzeit + Missions-Checkpoints; Missionen einzeln wiederholbar | H1 spielt in Sessions, keine Roguelike-Struktur |
| Wirtschaft/Regeln | Standard-Regelwerk (D-010/D-011) mit dokumentierten Missions-Overrides | Keine Kampagnen-Sonderwirtschaft |

## Story-Prämisse: Aetherium-Fallout

Der **Aetherium-Fall** – der Eintrag des Kristalls in die Welt (Einschlag/Fund, genaue Genesis wird in der Kampagne selbst bewusst lange offengehalten) – hat Ökonomie, Geopolitik und Biologie der Erde verändert. Aetherium ist Energiequelle, Währung und biologischer Katalysator zugleich. Die drei Fraktionen sind drei Antworten auf denselben Fallout:

- **Akt I – Allianz (Kontrolle):** Ein technokratischer Sicherheitspakt will Aetherium-Vorkommen sichern, Feldausbreitung (D-010) eindämmen und die Legions-Aufstände niederschlagen. Der Spieler beginnt als Ordnungsmacht – und stößt auf Belege, dass die Allianz die Mutationsberichte der Evolvierten systematisch unterdrückt.
- **Akt II – Legion (Ausbeutung):** Industriestaaten und Arbeiterarmeen der Verlierer-Regionen sehen in Aetherium die letzte Ressource, die nicht den Reichen gehört. Der Spieler führt den Aufstand aus Akt I fort – und merkt, dass die thermobarische Doktrin der Legion die Felder zerstört, von denen alle leben.
- **Akt III – Evolvierte (Verstehen):** Die Mutierten und Kristallwesen – Produkte des Fallouts, keine Invasoren – kämpfen um ihr Existenzrecht. Der Schlussakt erzwingt die Konfrontation der beiden bisherigen Spielerperspektiven und endet offen genug für eine Fortsetzung, aber abgeschlossen genug für ein Premium-Produkt.

Erzählprinzipien:

- **Drei Wahrheiten, keine Lüge:** Jeder Akt stellt die eigene Fraktion als handelnde Helden dar, ohne die vorherigen Akte zu revidieren – die H1-Zielgruppe soll keine Meta-Verschwörung, sondern klassische C&C-Frontklärung mit Graustufen bekommen.
- **Commander als Erzählstimmen (D-009):** Die Fraktions-Commander führen durch Briefings und In-Mission-Funk (Portrait + Voice, Verweis ./CommanderSystem.md). Sie haben **keine** Match-Mechanik – ihre Rolle ist rein narrativ-präsentativ, inklusive der Möglichkeit, in Kommandomissionen als benannte Figur "am Funk" präsent zu sein, ohne als Einheit auf dem Feld zu existieren.
- **Aetherium als Mitspieler:** Feldausbreitung, Überernte-Folgen und Kristall-Terrainveränderung (D-010) sind Story-Elemente: Missionen zeigen eskalierende Ausbreitung, und mindestens eine Mission pro Akt thematisiert Überernte als moralisches/ökonomisches Dilemma.

## Missionstypen-Varianz

Vier Missionstypen, über die Akte rotiert. Ziel: Kein Akt besteht aus mehr als der Hälfte Base-Building; jeder Typ dient der Lernkurve (nächster Abschnitt).

| Typ | Beschreibung | Anteil (Richtwert) | Lern-/Varianz-Funktion |
|---|---|---|---|
| **Base-Building (Klassiker)** | Aufbau, Wirtschaft, Vernichtung/Eroberung – der Kernloop | ~50 % (6–7 Missionen) | Volle Mechanik; Schwierigkeit über Gegner-Zahl, Feldqualität, Zeitdruck |
| **Kommandomission** | Kleiner, fester Trupp ohne Basis; limitierte Verstärkung als Skript-Events | ~20 % (2–3 Missionen) | Micro, Konter-Dreieck und Fähigkeiten ohne Wirtschaft; erzählerische Spitzen (Infiltration, Rettung) |
| **Verteidigung** | Bestehende Basis/Position gegen Wellen oder Zeitlimit halten | ~15 % (2 Missionen) | Verteidigungsplattform-Module (D-008), Energie-/Low-Power-Management, Mauer-Spiel |
| **Eskorte/Exfiltration** | Konvoi/Harvester-Verbände/Zivilisten durch feindliches Gebiet bringen | ~15 % (2 Missionen) | Mobile Verteidigung, Scouting, Drohnen-Einsatz (D-014) |

Zusatzregeln:

- **Bonusziele:** Jede Mission hat 1–2 optionale Ziele (z. B. "Feld ohne Überernte bewirtschaften", "capturebaren Geschützturm intakt übernehmen", D-016). Belohnung ist kosmetisch/archivalisch (Codex-Einträge), **keine** mechanische Progression – es gibt keine RPG-Ausbauebene.
- **Neutrale als Missionsbausteine (D-016):** Feindliche Lager als Zwischenziele, capturebare Türme als taktische Schlüsselpunkte, Critters als Weltlebendigkeit.
- **Biome/Hazards (D-017):** Mindestens eine Mond-/Mars-Mission mit Hazards (Staubsturm/Strahlungsfront) als Missionsmechanik statt Wetter – Schauplätze aus Maps.md/Biomes.md.

## Verzahnung mit der Tutorial-Lernkurve

Die Kampagne **ist** das Tutorial: Es gibt kein separates Tutorial-Modul. Jede Mechanik wird genau einmal kontrolliert eingeführt, bevor sie Pflicht wird. Leitplanke: Eine neue Mechanik pro Mission, nie zwei gleichzeitig.

| Mission (Richtwert) | Akt | Typ | Eingeführte Mechanik |
|---|---|---|---|
| M1 | I | Kommando (klein) | Bewegung, Angriff, Konter-Grundprinzip Infanterie |
| M2 | I | Base-Building | Bauen, Ernten (Harvester, Feld-Phasen D-010), Energie & Low-Power-Regel |
| M3 | I | Base-Building | Fahrzeuge, Tech-Stufen, Forschungslabor, Radar |
| M4 | I | Verteidigung | Verteidigungsplattform-Module, Mauer, Überernte-Dilemma (Bonusziel) |
| M5 | I | Base-Building (Finale) | Luftfahrzeuge, Flugabwehr, Superwaffe (eigene oder gegnerische) |
| M6+ | II | rotierend | Legion-Doktrin: Masse, Flammen/Raketen, günstige Ökonomie; keine *neuen* Systeme, neue Anwendung |
| M11+ | III | rotierend | **Evolvierte-Systeme: Wachstums-Bauweise, Regeneration statt Reparatur, Aetherium-Nähe-Boni (D-011)** – bewusst spät, da größtes Regel-Delta |

- **Wiederholbarkeit:** Mechanik-Einführungen werden im Codex (Archiv) nachlesbar, nicht als Popups wiederholt.
- **Akt-Wechsel-Regel:** Jeder Akt beginnt mit einer reduzierten Ausgangslage (Kommando- oder Verteidigungsmission), damit der Fraktionswechsel nicht mit voller Basisbaulast kollidiert.
- Abgleich mit [../vision/CoreGameplay.md](../vision/CoreGameplay.md) (Tutorial-Konzept): UI-Tutorialisierung (Hotkeys, Gruppen, Wegpunkte) erfolgt kontextuell in M1–M3 und bleibt in allen Modi konsistent.

## Präsentation und Scope-Grenzen

- **Briefings:** In-Engine-Format – Missionskarte mit eingeblendeten Marker-Animationen, Commander-Portrait + vertontem Funk (D-009). Kein separater Cinematic-Renderpfad.
- **Zwischensequenzen:** In-Engine (Kamera gemäß D-019, skriptgesteuerte Schlacht-Tableaus). **Keine Cinematics im MVP-Sinne** (keine pre-gerenderten Zwischensequenzen, keine Motion-Capture-Produktion).
- **Rendersequenzen optional:** Maximal Intro und Finale dürfen als pre-gerenderte Sequenzen evaluiert werden – **Phase-3-Budget-Entscheidung** in der Produktionsplanung (Sprint 6), kein Planungsstand in Sprint 2.
- **Linear (D-020):** Keine Story-Verzweigungen, keine Wahl-Missionen. Varianz entsteht über Bonusziele und Schwierigkeitsgrade, nicht über verzweigte Struktur.
- **Keine Koop-Kampagne (D-020):** Die Kampagne ist ein reines Solo-Erlebnis; Koop-Spiel erfolgt über separate Szenarien (Modi gemäß MultiplayerModes.md), nicht über Kampagnen-Missionen.
- **Kein Doktrinen-System (D-009):** Die Kampagne führt keine Commander-Perks oder freischaltbare Boni ein; Wiederholbarkeit über Missionen-Rang (Zeit/Verluste/Bonusziele).
- **Bewusst ausgeschlossen:** Marine (D-013), Handelssystem (D-016), Terrain-Deformation (D-012) – Missions-Skripte dürfen diese Systeme nicht "durch die Hintertür" einführen.

## Produktionstechnische Anker (für spätere Sprints)

- Missionen sind **datengetriebene Szenario-Definitionen** auf Standardkarten bzw. kampagnenspezifischen Varianten: Startzustand, Trigger (Zeit/Zone/Verlust), Skript-Waves, Override-Werteset (Verweis ./Balancing.md – Overrides werden versioniert wie Balance-Sets).
- Trigger-Sprache bleibt flach und SO-kompatibel (Bedingung → Effekt), damit Missionsdesign ohne Codeänderung iterierbar ist; Detail-Spezifikation ist Phase-3-Aufgabe, nicht Sprint 2.
- Kampagnen-KI nutzt dieselben Task-Pläne/Utility-Optionen wie Skirmish ([../research/KI_Architektur.md](../research/KI_Architektur.md)), ergänzt um skriptbare Ziel-Marker; keine separate "Kampagnen-KI".

## Offene Punkte

- **Evolvierten-Interludium in Akt II (Phase-3-Option, notiert – nicht entschieden):** Evolvierte-Mechanik erscheint spielerseitig erst ab ~M11 vollständig (D-011-Delta) – Risiko, dass Käufer die dritte Fraktion nie erleben, wenn sie vor Akt III abbrechen. Als **Phase-3-Option** vorgesehen: spielbare Evolvierten-Kommandomission als "Interludium" in Akt II; Ausarbeitung und Entscheidung in der Phase-3-Detailplanung.
- **Genesis des Aetherium-Falls** (Einschlag vs. terrestrischer Fund): weiterhin bewusst offengehalten; finale Festlegung obliegt dem Game Director, da sie Franchise-Potenzial (Fortsetzung) betrifft. Status: unentschieden, kein Sprint-2-Blocker.

Entschieden im Korrekturlauf Sprint 2 (ehemals offen):

- Koop-Fähigkeit: **keine Koop-Kampagne** (D-020) – Koop-Spiel über separate Szenarien.
- Akt-Reihenfolge: **linear (Akt I → II → III)** bestätigt (D-020).
- Rendersequenzen (Intro/Finale): **Phase-3-Budget-Entscheidung** in der Produktionsplanung (Sprint 6) – kein Sprint-2-Handlungsbedarf.

## Nächste Schritte

- Abgleich mit ./CommanderSystem.md: Commander-Namen/-Biografien so definieren, dass die Akt-Perspektiven tragfähig sind (Sprint 2, CommanderSystem-Verantwortlicher).
- Karten-/Biom-Bedarfe der 12–15 Missionen an Maps.md/Biomes.md melden (insb. Hazard-Mission, D-017), damit die Karten-Roadmap Kampagnen-Bedarfe nicht ausschließt.
- Phase 3: Missionsliste mit konkreten Schauplätzen, Zielen und Trigger-Grobstruktur ausarbeiten; Trigger-Spezifikation definieren.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung (Phase-3-Konzeptrahmen) | Game Director |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Game Director |
