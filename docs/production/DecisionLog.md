# Decision Log

**Version:** 1.9.0 | **Status:** aktiv (laufend) | **Verantwortungsbereich:** Game Director / Lead Technical Director / Project Owner | **Sprint:** 7

## Zweck

Zentrales, unveränderliches Protokoll aller Architektur- und Design-Entscheidungen. Jede Entscheidung enthält Kontext, geprüfte Alternativen (mindestens drei, sofern anwendbar), Begründung und Konsequenzen. Revidierte Entscheidungen bleiben mit Status "ersetzt durch D-xxx" erhalten.

## Abhängigkeiten

- [../meta/DocumentationStandard.md](../meta/DocumentationStandard.md)
- Referenziert aus allen Fachdokumenten per Entscheidungs-ID

## Format

`D-xxx | Status | Kontext | Alternativen | Entscheidung | Begründung | Konsequenzen`

---

### D-001 | verbindlich | Sprint 0

**Kontext:** Wie wird die Projektdokumentation strukturiert?
**Alternativen:** (a) Ein zentrales GDD/TDD-Monolith-Dokument; (b) Wiki aus vielen kleinen verlinkten Markdown-Dateien im Repo; (c) externes Wiki-Tool (Confluence/Notion) außerhalb des Repos.
**Entscheidung:** (b) – Wiki unter `docs/` im Repository.
**Begründung:** Versioniert mit dem Projekt mit, ist für Agenten und Tools direkt les-/schreibbar, erzwingt Kleinteiligkeit und Verlinkung; Monolithen veralten nachweislich, externe Tools entkoppeln Doku vom Code.
**Konsequenzen:** [../meta/DocumentationStandard.md](../meta/DocumentationStandard.md) als verbindlicher Standard; Pflichtabschnitte in jedem Dokument.

### D-002 | verbindlich | Sprint 0

**Kontext:** Wird der im TPD §17 festgelegte Stack (Unity, C#, URP, Windows/macOS, GitHub+LFS) erneut grundlegend verhandelt?
**Alternativen:** (a) Stack ungeprüft übernehmen; (b) komplette Engine-Neubewertung (Unreal, Godot, Eigenbau); (c) TPD-Stack als verbindliche Ausgangslage übernehmen und in Sprint 1 gezielt validieren (Sanity Check).
**Entscheidung:** (c).
**Begründung:** Das TPD ist bereits eine begründete, detaillierte Entscheidungsgrundlage; eine vollständige Neuauflage wäre doppelte Arbeit ohne Erkenntnisgewinn. Sprint 1 prüft den Stack anhand aktueller Marktdaten und dokumentiert Abweichungen nur bei belastbaren Gegenargumenten.
**Konsequenzen:** Research in Sprint 1 fokussiert auf Validierung statt Grundsatzsuche; Engine-Wechsel nur über neue Entscheidung mit Status "ersetzt D-002".

### D-003 | verbindlich | Sprint 0

**Kontext:** Wie wird mit den 12 gefundenen Inkonsistenzen zwischen den Quelldokumenten umgegangen?
**Alternativen:** (a) Sofort in Sprint 0 auflösen; (b) nur erfassen und den Fachsprints zuweisen; (c) Quelldokumente direkt umschreiben.
**Entscheidung:** (b).
**Begründung:** Die Auflösungen sind Design-Entscheidungen (z. B. Gebäude-Scope, Commander-System), die Research (Sprint 1) und das ausgearbeitete GDD (Sprint 2) brauchen. Sprint 0 hat Analyse-Charakter; vorschnelle Festlegungen widersprächen der Qualitätsregel "erst vergleichen, dann entscheiden". Quelldokumente bleiben als historischer Stand unverändert.
**Konsequenzen:** [../analysis/Inconsistencies.md](../analysis/Inconsistencies.md) + Übernahme als Q-001–Q-012 in [OpenQuestions.md](OpenQuestions.md).

### D-004 | verbindlich | Sprint 0

**Kontext:** Werden für alle geforderten Dokumente sofort Platzhalter-Dateien angelegt?
**Alternativen:** (a) Alle ~70 Dokumente als leere Platzhalter anlegen; (b) keine Platzhalter, Dokumente entstehen erst im zuständigen Sprint mit echtem Inhalt; (c) Platzhalter nur für den jeweils nächsten Sprint.
**Entscheidung:** (b).
**Begründung:** Leere Dokumente erzeugen Schein-Fortschritt, veralten sofort und verletzen den eigenen Standard ("Dokumentation ist nie fertig, aber auch nie leer"). Der Index führt geplante Bereiche transparent als "geplant".
**Konsequenzen:** Wiki wächst sprintweise mit Inhalt; Vollständigkeit wird über [../analysis/GapAnalysis.md](../analysis/GapAnalysis.md) verfolgt.

### D-005 | verbindlich | Sprint 0

**Kontext:** Versionierungsschema der Dokumente.
**Alternativen:** (a) Keine Versionen, nur Git-Historie; (b) Semantisches Schema 0.x (Entwurf) / 1.0 (sprint-freigegeben) mit Pflicht-Änderungsverlauf; (c) Datumsversionierung.
**Entscheidung:** (b).
**Begründung:** Versionsstand im Dokumentkopf macht den Reifegrad ohne Git-Zugriff erkennbar; der Pflicht-Änderungsverlauf sichert Nachvollziehbarkeit über Sprint-Grenzen hinweg (Living-Documents-Prinzip).
**Konsequenzen:** Verbindlich in [../meta/DocumentationStandard.md](../meta/DocumentationStandard.md) verankert.

---

### D-006 | verbindlich | Sprint 1

**Kontext:** Validierung von D-002 (Engine-Stack) anhand des Sprint-1-Research [../research/Unity_BestPractices.md](../research/Unity_BestPractices.md); welche Unity-Version wird festgelegt?
**Alternativen:** (a) Unreal Engine 5 (überdimensioniert für den gewählten Stil, verwirft C#-Backend-Option und Asset-Kaufstrategie); (b) Godot 4 (geringste Beleglage für 500+ Einheiten in 3D, kleiner Asset-Markt); (c) Unity mit Voll-DOTS (verworfen, siehe Q-015-Research: Umbruchphase, Asset-Bruch); (d) Unity 6.3 LTS + URP + C#, klassisch mit SO-Datenmodell.
**Entscheidung:** (d) – **Unity 6.3 LTS (6000.3.x), URP, C#**. D-002 damit bestätigt und konkretisiert.
**Begründung:** Kein belastbares Gegenargument gegen den TPD-Stack gefunden; Runtime Fee seit 09/2024 vollständig gestrichen; Unity 6.3 LTS (Support bis 12/2027) deckt MVP bis Produktion; URP liefert mit GPU Resident Drawer, SRP Batcher und Render Graph die passenden Werkzeuge für 100–500+ Einheiten im stilisierten Look.
**Konsequenzen:** Patch-Pinning beim Projekt-Setup; URP-Entwicklung Render-Graph-konform (keine Migrationsschuld); SO-Datenmodell nach den Leitplanken aus [../research/Unity_BestPractices.md](../research/Unity_BestPractices.md); Unity-Reputationsrisiko als R-11 ins Risikoregister.

---

### D-007 | verbindlich | Sprint 2 (Q-016)

**Kontext:** Geschäftsmodell und Zielgruppe.
**Alternativen:** (a) F2P mit Server-MP-Fundament; (b) Premium Singleplayer/Skirmish-first auf Steam; (c) Abo-/Live-Service-Modell.
**Entscheidung:** (b) – Premium (~30–40 €), Singleplayer/Skirmish-first, Steam Windows/macOS; Primärzielgruppe H1 "C&C-Nostalgiker" (Solo/Skirmish, 30–45 J.); kompetitives Segment frühestens Phase 3.
**Begründung:** Markt-Research ([../research/RTS_Markt_Wettbewerb.md](../research/RTS_Markt_Wettbewerb.md)): F2P-/Server-MP-RTS scheitern wiederholt (Stormgate MP-Abschaltung 04/2026); Premium-SP-Titel (Tempest Rising) tragen; passt zu Studio-Kapazität und R-10.
**Konsequenzen:** MP ist Feature, nicht Fundament; Ranked unter Vorbehalt (D-018); TargetAudience.md und USP.md richten sich an H1 aus.

### D-008 | verbindlich | Sprint 2 (Q-001)

**Kontext:** Gebäudetypen pro Fraktion – 11 (GDD-O) vs. 18 (APL).
**Alternativen:** (a) 18 Typen (APL-Scope); (b) 11 Typen (GDD-O); (c) 12 kuratierte Typen.
**Entscheidung:** (c) – 12 Typen: HQ, Kraftwerk, Raffinerie, Lager, Kaserne, Fahrzeugfabrik, Flugfeld, Forschungslabor, Radar, Verteidigungsplattform (modular: MG/Flak/Rakete als Upgrade-Module), Mauer, Superwaffe.
**Begründung:** 18 sprengt Kapazität (R-01) und MVP-Disziplin; 11 unterschlägt Mauer (C&C-Erwartung der H1-Zielgruppe) und die Aufsplittung der Verteidigung – die als Modulsystem statt als Mehrfachtyp gelöst wird.
**Konsequenzen:** 36 Gebäude-Assets (12×3) statt 54; Buildings.md definiert Module und Voraussetzungen; APL Paket 03 wird in Sprint 5 entsprechend korrigiert.

### D-009 | verbindlich | Sprint 2 (Q-002)

**Kontext:** Commander-System – im TPD nur als Signature-Asset genannt.
**Alternativen:** (a) RPG-artiger Commander mit Match-Mechanik und Progression; (b) Commander als rein narrative/präsentative Identität (Portrait, Voice, Story, Key Art); (c) komplett streichen.
**Entscheidung:** (b) – Commander als Identitäts-Layer ohne Match-Mechanik im MVP; optionales Doktrinen-System (kleine passive Fraktions-Varianten) frühestens ab Beta evaluieren.
**Begründung:** Ein mechanisches Commander-System ist ein zweites Balancing-Universum (R-01) und für die H1-Zielgruppe kein Kaufargument; als Identität liefert es die im TPD geforderte Unverwechselbarkeit (Signature-Assets) zu geringen Kosten.
**Konsequenzen:** CommanderSystem.md definiert Identität, Voice-Konzept und Doktrinen-Ausblick; kein Commander-Balancing im MVP.

### D-010 | verbindlich | Sprint 2 (Q-005)

**Kontext:** Aetherium-Wirtschaftsregel – "wächst nach" vs. "erschöpfte Felder".
**Alternativen:** (a) unendliche Felder (flaches Late-Game); (b) rein endliche Felder (klassisches C&C, USP verpufft); (c) Hybrid: endlicher Mutterkristall + nachwachsende Ausläufer + Ausbreitung/Überernte.
**Entscheidung:** (c) – Jedes Feld hat einen Mutterkristall mit endlicher Gesamtreserve; sichtbare Kristalle wachsen nach, solange der Mutterkristall lebt; Felder breiten sich langsam aus und verändern das Terrain (USP); Überernte schädigt den Mutterkristall dauerhaft. Ziel-Matchdauer 20–35 Minuten.
**Begründung:** Macht den recherchierten Kern-USP spielbar, erzeugt Map-Control-Druck ohne hartes Ressourcen-Timeout und unterscheidet Nova von C&C (endlich) und SupCom (unendlich); datengetrieben auf demselben Grid wie Pathfinding/FoW umsetzbar.
**Konsequenzen:** Resources.md/Economy.md spezifizieren Phasen, Raten und Überernte-Regeln; Ausbreitung beeinflusst Karten-Design (Maps.md); KI muss Feldbewirtschaftung verstehen (Input für AIArchitecture).

### D-011 | verbindlich | Sprint 2 (Q-009)

**Kontext:** Evolvierte-Gebäude – organisch? Eigene Bau-Mechanik?
**Alternativen:** (a) identische Bauweise wie andere Fraktionen, nur anderer Look; (b) organisches Wachstum: Keim pflanzen → reift über Zeit, Aetherium-Nähe beschleunigt, Regeneration statt Reparatur; (c) völlig eigenes System (z. B. ein einziger sich ausbreitender Organismus).
**Entscheidung:** (b) – Evolvierte nutzen die gleichen 12 Gebäudetypen (D-008), aber mit Wachstums- statt Konstruktionsmechanik und Regeneration statt Reparatur.
**Begründung:** (a) verschenkt die Fraktionsidentität; (c) ist ein unbalancierbares Sonderuniversum (R-01) und bricht das Produktions-UI; (b) erzeugt spürbare Asymmetrie bei überschaubarem Regel-Delta.
**Konsequenzen:** Buildings.md definiert Keim/Reifung/Beschleunigung; Art-richtung organisch-kristallin (Input EnvironmentAssets); Evolvierte-Harvester/-Builder-Regeln in Economy.md.

### D-012 | verbindlich | Sprint 2 (Q-017)

**Kontext:** Umfang der "vollständig zerstörbaren Umgebung" (Vision).
**Alternativen:** (a) Vollzerstörbarkeit inkl. Terrain-Deformation; (b) gezielte Zerstörbarkeit; (c) keine Umgebungs-Zerstörbarkeit.
**Entscheidung:** (b) – Zerstörbar/beeinflussbar: Gebäude, Einheiten, Vegetation & Dekor (brennbar), Brücken, Aetherium-Felder (durch Waffen beschädigbar/vernichtbar). Nicht zerstörbar: Terrain-Geometrie, Höhen.
**Begründung:** Markt-Research findet keinen Beleg, dass Vollzerstörbarkeit verkauft; Terrain-Deformation kollidiert mit Pathfinding-Budget (Q-014), Netcode und R-05; gezielte Zerstörbarkeit liefert die taktischen Momente (Wald abfackeln, Brücke sprengen) zu beherrschbaren Kosten.
**Konsequenzen:** Vision.md wird entsprechend präzisiert; Maps.md definiert zerstörbare Elemente pro Biom; R-05 bleibt überwacht, aber entschärft.

### D-013 | verbindlich | Sprint 2 (Q-003)

**Kontext:** Marine – APL Paket 07 (optional), GDD-O schweigt, TPD-MVP schließt aus.
**Alternativen:** (a) als vollwertiges Feature einplanen; (b) auf Phase 4+/Post-Release parken; (c) streichen.
**Entscheidung:** (c) – Marine aus dem Produktplan gestrichen; Wasser existiert nur als Terrain-Feature (unpassierbar bzw. Brücken).
**Begründung:** Marine ist ein komplettes Sub-Ökosystem (Assets, Balance, Pathfinding-Ebene) ohne Kernloop-Beitrag; "parken" erzeugt Zombie-Scope (R-01); bei späterem Community-Druck ist eine Neuaufnahme als Erweiterung unabhängig entscheidbar.
**Konsequenzen:** APL Paket 07 entfällt in Sprint 5; Karten ohne Wasser-Kampf-Anforderungen (Maps.md).

### D-014 | verbindlich | Sprint 2 (Q-004)

**Kontext:** Drohnen (APL Paket 09) – Rolle, Fraktionsbezug, Produktion.
**Alternativen:** (a) fraktionsübergreifende Drohnen-Klasse mit eigener Produktion; (b) 2–3 fraktionsspezifische Support-Drohnen, produziert in bestehenden Fabriken; (c) streichen.
**Entscheidung:** (b) – Allianz/Legion: Scout-, Repair-, Kampf-Drohne; Evolvierte: Bio-Äquivalente (Sporen-Schwarm); Produktion über Fahrzeugfabrik/Flugfeld, keine eigene Produktionskette.
**Begründung:** (a) ist Feature-Inflation ohne Design-Beitrag; (c) verschenkt günstige Asymmetrie- und QOL-Werkzeuge (Scouting, Reparatur), die der H1-Zielgruppe vertraut sind.
**Konsequenzen:** Vehicles.md/Aircraft.md führen Drohnen; APL Paket 09 wird in Sprint 5 auf ~6–9 Assets reduziert.

### D-015 | verbindlich | Sprint 2 (Q-006)

**Kontext:** Spezialeinheiten – 5 Typen vs. 15 im APL-Gesamtumfang.
**Alternativen:** (a) 15; (b) 5 fraktionsübergreifend; (c) 1 Elite-Einheit pro Fraktion (MVP/Alpha), 3 pro Fraktion (Release) = 9.
**Entscheidung:** (c) – z. B. Allianz "Titan-Mech", Legion "Mobile Festung", Evolvierte "Alpha-Mutant"; Freischaltung Tech Tier 3, Limit 1–2 gleichzeitig pro Spieler.
**Begründung:** Elite-Einheiten sind Signature-Assets (TPD §7.2) und Endspiel-Höhepunkt; 15 wäre Content-Inflation ohne Balancing-Tragfähigkeit (R-01).
**Konsequenzen:** Vehicles.md definiert Elite-Regeln; ResearchTree.md verankert Tier-3-Freischaltung; APL Paket 08 wird in Sprint 5 korrigiert.

### D-016 | verbindlich | Sprint 2 (Q-007)

**Kontext:** Neutrale Einheiten, insb. "Händler" (impliziert Handelssystem).
**Alternativen:** (a) Handelssystem mit neutralen Händlern; (b) Neutrale als Map-Elemente: Critters, feindliche Lager als Objectives (Aetherium-Belohnung), capturebare Geschütztürme; (c) keine Neutralen.
**Entscheidung:** (b) – Händler und Handelssystem gestrichen.
**Begründung:** Ein Handelssystem ist ein zusätzliches Wirtschafts-UI und Balancing-System ohne Kernloop-Beitrag; (b) liefert Map-Identität, Scouting-Anreize und frühe Konflikte zu geringen Kosten; (c) verschenkt Map-Lebendigkeit.
**Konsequenzen:** NeutralUnits.md definiert Regeln und Belohnungen; Maps.md platziert Objectives.

### D-017 | verbindlich | Sprint 2 (Q-010, Q-012)

**Kontext:** Verhältnis Biom ↔ Karte; Wetter-Regel.
**Alternativen:** (a) 10 Biome = 10 Karten; (b) 10 Biome als Themen-Bibliothek, Karten mit eigenem Layout-Prozess (MVP 1, Alpha 4, Beta 8, Release 12 Karten, Größen S/M/L für 1v1 bis 3v3/FFA-6); (c) weniger Biome (3–4) mit mehr Karten.
**Entscheidung:** (b) – plus: Wetter/Umwelteffekte werden pro Biom definiert; atmosphärenlose Karten (Mond, Mars) erhalten Hazards statt Wetter (Staubstürme, Strahlungsfronten).
**Begründung:** (a) verwechselt Thema mit Layout und produziert austauschbare Karten; (c) verschenkt die Asset-Pipeline-Planung; Hazards statt Wetter löst den Physik-Widerspruch spielerisch (USP-kompatibel).
**Konsequenzen:** Biomes.md definiert 10 Profile inkl. Wetter/Hazards; Maps.md definiert Layout-Regeln und Karten-Roadmap; VFX-Bedarf (Wetter) an Sprint 5.

### D-018 | verbindlich | Sprint 2 (Q-011)

**Kontext:** Phasenzuordnung der Spielmodi.
**Alternativen:** (a) alle Modi zum Release; (b) gestuft: MVP Solo-Skirmish 1v1 vs. KI; Alpha + Koop vs. KI, FFA; Beta + PvP 1v1/2v2, Survival; Release + King of the Hill, Ranked nur nach Re-Evaluierung; (c) MP-first.
**Entscheidung:** (b) – Ranked explizit unter Vorbehalt (Maphack-/Serverkosten-Frage, Q-013-Ausgang).
**Begründung:** Folgt D-007 (SP-first) und der Phasenlogik des TPD; jeder Modus wird erst geplant, wenn seine technische Basis steht; Ranked erfordert Maphack-Resistenz und persistente Infrastruktur, die das Markt-Research als Fundament-Risiko (R-10) ausweist.
**Konsequenzen:** MultiplayerModes.md definiert Regeln je Modus; Produktionsplanung (Sprint 6) übernimmt die Staffelung.

### D-019 | verbindlich | Sprint 2 (Q-008)

**Kontext:** GDD-Formulierung "isometrische Kamera" vs. TPD-Realität.
**Alternativen:** (a) starr isometrisch; (b) echte 3D-Welt, schräge Top-Down-Perspektive, Zoom, optionale Rotation; (c) voll freie Kamera.
**Entscheidung:** (b) – GDD wird präzisiert: RTS-Standardkamera (Pitch ~50–60°, Zoom-Stufen, Rotation optional per Setting, Standard deaktiviert).
**Begründung:** Entspricht TPD §6.2 und Genre-Standard; starre Isometrie würde die 3D-Asset-Strategie unterlaufen; voll freie Kamera schadet Lesbarkeit (TPD §6.3).
**Konsequenzen:** CoreGameplay.md dokumentiert Kamera-Verhalten; "isometrisch" wird im GDD-Wortschatz durch "schräge Top-Down-Perspektive" ersetzt.

---

### D-020 | verbindlich | Sprint 2 (Kampagne)

**Kontext:** D-018 nennt keinen Kampagnen-Modus; Markt-Research zeigt Kampagne als H1-Kaufgrund Nr. 1 (Tempest-Rising-Evidenz).
**Alternativen:** (a) keine Kampagne; (b) Koop-fähige Kampagne; (c) lineare Solo-Kampagne in Phase 3 (3 Akte, 12–15 Missionen, je Akt eine Fraktionsperspektive), Koop über separate Szenarien.
**Entscheidung:** (c).
**Begründung:** Solo-First-Positionierung (D-007) ohne Kampagne wäre widersprüchlich; Koop-Kampagne multipliziert Missionsdesign- und Netcode-Aufwand (Q-013-abhängig).
**Konsequenzen:** [../gamedesign/Campaign.md](../gamedesign/Campaign.md) ist verbindlicher Konzeptrahmen für Phase 3; Kampagne dient als Tutorial-Träger; kein MVP-/Alpha-Umfang.

### D-021 | verbindlich | Sprint 2 (Versorgungssystem)

**Kontext:** Infantry.md führte ein `popLimit`-Feld ein, ohne dass ein Versorgungssystem entschieden war.
**Alternativen:** (a) Supply-System (AoE/SC2-artige Versorgungsgebäude); (b) hartes Pop-Cap; (c) kein Versorgungssystem – Begrenzung nur über Wirtschaft, Produktionszeit und Elite-Limit (D-015).
**Entscheidung:** (c).
**Begründung:** C&C-Tradition der H1-Zielgruppe kennt kein Supply-System; eine Simulations-/UI-Achse weniger; Skalierung bleibt über Ökonomie gesteuert (D-010).
**Konsequenzen:** `popLimit`-Feld entfällt aus allen Datenmodellen außer dem Elite-Limit.

### D-022 | verbindlich | Sprint 2 (Capture-System)

**Kontext:** Engineer/Saboteur-"Einnehmen" und capturebare Türme (D-016) brauchen ein einheitliches Regelwerk; die Evolvierten hatten keine Capture-Einheit (Lücke).
**Alternativen:** (a) kein Capture-System; (b) Sofort-Capture bei Berührung; (c) Kanal-Capture (5 s, Abbruch bei Schaden, Einheit wird verbraucht).
**Entscheidung:** (c) – Einheiten: Engineer (Allianz), Saboteur (Legion), **Tunnelgräber (Evolvierte, schließt die Lücke)**; gilt für feindliche Gebäude und neutrale Türme gleichermaßen.
**Begründung:** Kanal mit Abbruch ist lesbar, konterbar und C&C-vertraut; Sofort-Capture ist frustrierend, kein Capture verschenkt taktische Tiefe und macht D-016-Türme wertlos.
**Konsequenzen:** Infantry.md und NeutralUnits.md werden angeglichen; kein Garrison-System (Besetzen von Gebäuden) im MVP – separate Evaluierung ab Beta.

### D-023 | verbindlich | Sprint 2 (Superwaffen-Limit)

**Kontext:** Buildings.md legte Limit 1 fest, Factions.md fragte an.
**Alternativen:** (a) unbegrenzte Superwaffen; (b) mehrere mit globalem Cooldown-Sharing; (c) Limit 1 pro Spieler mit globaler Bau-Ansage.
**Entscheidung:** (c) – zuzüglich: Zerstörung im geladenen Zustand = 25-%-Effekt am eigenen Standort (Sabotage-Anreiz, Comeback-Mechanik).
**Begründung:** Lesbarkeit und Endspiel-Dramaturgie; unbegrenzte Superwaffen degradieren sie zum Wirtschafts-Spam.
**Konsequenzen:** Buildings.md/Weapons.md/GameLoop.md angeglichen.

### D-024 | verbindlich | Sprint 2 (Lager & Raffinerie)

**Kontext:** Lager-Kapazitätsmechanik (+2.000 AE/Lager) war nicht im Zahlengerüst; Raffinerie-Packaging offen.
**Alternativen:** (a) keine Lager-Kapazität (Lager nutzlos); (b) Kapazität mit hartem Erntestopp bei vollem Konto; (c) Kapazität +2.000 AE je Lager, Überschuss verfällt, anteiliger Verlust bei Lager-Zerstörung; Raffinerie wird mit 1 Harvester geliefert.
**Entscheidung:** (c).
**Begründung:** Silo-Logik ist C&C-Kernerwartung (H1) und gibt dem Lager-Gebäude (D-008) seine Existenzberechtigung; anteiliger statt totaler Verlust bleibt H2-freundlich; Harvester-Packaging entspricht Genre-Standard.
**Konsequenzen:** Economy.md/Buildings.md angleichen; Basis-Kapazität (HQ) wird dort festgelegt.

### D-025 | verbindlich | Sprint 2 (D-018-Klarstellung FFA)

**Kontext:** D-018 sieht FFA ab Alpha vor, Netz-MP-Technik kommt aber frühestens Beta (Q-013) – interner Widerspruch.
**Alternativen:** (a) FFA auf Beta verschieben; (b) Alpha-FFA als lokaler Modus gegen KI-Mitspieler; (c) Netz-MP vorziehen.
**Entscheidung:** (b) – Alpha-FFA = lokal gegen KI; alle Netz-Modi (Koop online, PvP) frühestens Beta, abhängig vom Q-013-Ausgang.
**Begründung:** Erhält die Alpha-Modusvielfalt ohne MP-Technik vorzuziehen (D-007: MP ist Feature, nicht Fundament).
**Konsequenzen:** MultiplayerModes.md präzisiert Modus-Tabelle (lokal vs. online).

### D-026 | verbindlich | Sprint 2 (Konter-Lücken und Einheiten-Korrekturen)

**Kontext:** Konsistenzreview fand Lücken: Evolvierte ohne mobile Flugabwehr (Balancing-Regel "jedes Matchup braucht Tier-≤2-Antwort" verletzt), Radar-Fahrzeug mit überkomplexer Feuerleitung, Parasiten-Königin als MP-Sync-Risiko, Sniper-One-Shot als Frustquelle.
**Alternativen:** (a) Lücken belassen; (b) neue Einheitentypen ergänzen; (c) gezielte Anpassungen bestehender Einheiten.
**Entscheidung:** (c) – (i) Kristallmagier erhält Zielklasse `Both` (Evolvierte-AA); (ii) Radar-Fahrzeug = mobiler Radar + Detektor, Feuerleitungs-Verbandsmechanik gestrichen; (iii) Evolvierte-Luft-Spezialeinheit im MVP = Säure-Bomberin, Parasiten-Königin (dauerhafte Übernahme) erst ab Beta; (iv) Sniper mit 2-Schuss-Profil gegen Standard-Infanterie.
**Begründung:** Neue Typen (b) wären Scope-Inflation (R-01); die Anpassungen schließen Konter-Lücken mit minimalen Regel-Deltas.
**Konsequenzen:** Infantry.md/Vehicles.md/Aircraft.md/Weapons.md angleichen.

### D-027 | verbindlich | Sprint 2 (Fraktions-Sonderregeln)

**Kontext:** Mehrere Sonderregel-Fragen aus dem Konsistenzreview betreffen Asymmetrie-Kernentscheidungen.
**Entscheidungen (je mit verworfener Alternative):**
1. **Kristallsturm interagiert mit Aetherium:** verstärkte Reichweite/Dauer auf Feldern (USP-Moment; Alternative "rein destruktiv" verworfen, Balancing-Beobachtungspflicht).
2. **Evolvierte EMP-immun** (Bio-Asymmetrie; Alternative "EMP wirkt normal" verworfen – würde die EMP-Waffe zum Evolvierten-Konter ohne Gegenwert machen).
3. **Ionenstrahl ohne EMP-Nebenwirkung** (Lesbarkeit; EMP bleibt dem Allianz-Sturmjäger vorbehalten).
4. **Legion bewusst ohne Infanterie-Heiler** (Masse-Identität; Ausgleich über günstige Neuproduktion).
5. **Evolvierte-Elite = Infanterie (Alpha-Mutant) gewollt**; Ausgleich der Elite-Asymmetrie über die Release-Eliten (3/Fraktion, D-015).
6. **Heilschwarm stapelt nicht** auf passive Regeneration (nur aktive Heilung).
7. **Regenerations-Bonus der Evolvierten nur auf lebenden Feldern** (nicht auf erschöpften).
8. **Keine aktive Dekontamination im MVP** – Verseuchung endet mit Feld-Erschöpfung/-Vernichtung (D-010/D-012).
9. **EMP pausiert keinen Kraftwerk-Output** (keine Doppelbestrafung mit Low-Power-Regel).
**Konsequenzen:** Weapons.md/DamageSystem.md/ArmorSystem.md/Economy.md/Resources.md angleichen.

### D-028 | verbindlich | Sprint 2 (Karten- und Biome-Festlegungen)

**Kontext:** Biome-/Karten-Detailfragen aus dem Konsistenzreview.
**Entscheidungen:**
1. **Hazard-Zuordnung:** Mond = Strahlungsfronten (atmosphärenlos, kein Staubsturm), Mars = Staubstürme – D-017 wird als Hazard-Portfolio gelesen (physikalisch sauberste Lesart).
2. **Doppelbelegung Wüste/Schnee** für Release-Karten 11–12 bestätigt (12 Karten, 10 Biome).
3. **Eisbruch-Mechanik (Schnee):** MVP-Fallback "Eis unpassierbar für schwere Fahrzeuge"; volle Zustandsmaschine erst bei ausreichendem Sim-Budget (Q-014).
4. **Brücken reparierbar** (Engineer/Builder/Tunnelgräber-Kanal, D-022-Mechanik).
5. **Infanterie im Vakuum ohne Sonderregeln** – Hazards treffen alle Einheiten gleich (Lesbarkeit).
6. **Legion-Flammenwaffen auf Mond/Mars:** Schaden ja, Brände nein (kein Sauerstoff).
7. **Survival nutzt Standard-Karten** mit Engstellen-Anforderung, keine eigenen Wellen-Karten.
**Konsequenzen:** Biomes.md/Maps.md/MultiplayerModes.md angleichen.

### D-029 | verbindlich | Sprint 2 (Modi- und Komfort-Festlegungen)

**Kontext:** Modi-/UX-Detailfragen aus dem Konsistenzreview.
**Entscheidungen:**
1. **Kein Ressourcentransfer zwischen Teamspielern** (D-016-Handelsverbot gilt sinngemäß; Wirtschaft bleibt ehrlich).
2. **Survival bis 4 Spieler** (lokal/online, Koop-Charakter).
3. **Artefakt-Sonde (30-s-Basisaufdeckung) nur in SP/Koop**, im PvP deaktiviert (Informations-Balance).
4. **Radar-Pings werden im Team geteilt.**
5. **Kamera-Rotation (D-019-Option) erst ab Beta** (Art-Aufwand pro Blickwinkel).
6. **Kein Ingame-Voice-Chat** (externe Tools decken das; Moderations-/Infrastrukturlast entfällt).
7. **PvP-Timeout-Punkteschlüssel und Unentschieden-Wertformel: erst Beta-Balancing.**
**Konsequenzen:** MultiplayerModes.md/CoreGameplay.md angleichen.

### D-030 | verbindlich | Sprint 2 (Forschungs-Regeln)

**Kontext:** ResearchTree.md fragte Tech-Umfang, Ausschluss-Mechanik und Low-Power-Interaktion an.
**Alternativen (Ausschluss):** (a) keine Ausschlüsse; (b) beliebig viele; (c) sparsame Ausschlüsse.
**Entscheidungen:** (c) – gegenseitiger Ausschluss erlaubt, max. 1 Paar pro Fraktion (Tier 2, identitätsstiftend); **Tech-Umfang 12–16/Fraktion bestätigt**; **Low-Power −50 % gilt auch für Forschungsgeschwindigkeit** (Konsistenz zur Energie-Regel).
**Begründung:** Sparsame Ausschlüsse erzeugen Identität ohne Build-Order-Lotterie; mehr wäre Balancing-Lotterie (R-01).
**Konsequenzen:** ResearchTree.md angleichen.

---

### D-031 | verbindlich | Sprint 2 (Feinschliff Konsistenzreview, 2. Runde)

**Kontext:** Der GDD-Korrekturlauf (D-020–D-030) hat sechs Querschnitts-Konflikte zwischen Dokumenten aufgedeckt.
**Entscheidungen:**

1. **HQ-Neuaufbau-Mechanik (SPC_REBASE):** Nach der Tier-2-Forschung kann ein Builder-Fahrzeug (Allianz/Legion) bzw. das Evolvierte-Builder-Äquivalent das neue HQ **eigenständig** errichten – außerhalb der HQ-Bau-Queue. *Alternativen:* (a) Neuaufbau nur bei ≥1 verbleibendem HQ (macht die Forschung im Ernstfall nutzlos); (b) Bau-Queue auch ohne HQ verfügbar (bricht die Queue-Regel aus Buildings.md). *Begründung:* (c) erhält beide Regeln und schließt die Logiklücke.
2. **Detektor-Regel:** VIS-INF-RECON-Einheiten (Sniper, Aufklärer-Infanterie) sind **keine** Detektoren. Tarnungs-Aufdeckung nur durch VIS-SCOUT-Einheiten, Scout-Drohne (D-014) und Detektor-Turm-Upgrade. *Alternativen:* (a) Recon als Detektor (macht den getarnten Sniper zur Detektor-Einheit – Balance-Bruch); (b) Sniper-Sonderklasse (Regel-Inflation).
3. **Alpha-Koop:** Koop vs. KI in Alpha = **1 menschlicher Spieler + KI-Verbündeter**. Kein lokales 2-Spieler-RTS (genreunüblich, undefinierte Eingabemechanik); Online-Koop frühestens Beta (D-025-konform).
4. **Survival-Niederlage harmonisiert:** Niederlage = alle eigenen Gebäude (außer Mauern) und Einheiten zerstört – identisch zur Standard-Vernichtungsregel (VictoryConditions.md führend, MultiplayerModes.md gleichziehen).
5. **Evolvierte-Regenerations-Kompensation:** Ausgleich über **langsamere Regenerationsrate** (Economy.md führend), **nicht** über Baukosten (ArmorSystem.md-Verweis korrigieren).
6. **Verteidigungsplattform-Aggressions-Modi:** Plattformen erhalten die Standard-Modi Halten/Abwehren/Freies Feuer (Standard: Abwehren), identisch zu Einheiten (Buildings.md spiegelt CoreGameplay.md).

**Konsequenzen:** Einarbeitung in Buildings.md, ResearchTree.md, FogOfWar.md, MultiplayerModes.md, VictoryConditions.md, ArmorSystem.md, CoreGameplay.md (Feinschliff-Runde 2).

---

### D-032 | verbindlich | Sprint 2 (Feinschliff Runde 2, Restpunkte)

**Kontext:** Drei Restbefunde aus der Feinschliff-Runde 2 (D-031-Umsetzung).
**Entscheidungen:**
1. **Burrow-Detektion der Evolvierten bleibt** als fraktionsspezifische Sonderregel. D-031.2 wird präzisiert: "nur VIS-SCOUT, Scout-Drohne, Detektor-Turm-Upgrade" beschreibt die fraktionsübergreifenden Detektor-Quellen; Burrow ist die zusätzliche Evolvierte-Quelle. *Alternative* "Burrow-Detektion streichen" verworfen: würde das Tarn-/Gegentarn-Spiel der Bio-Fraktion schwächen (Asymmetrie-Nische, SC2-Präzedenz für Burrow-Mechanik).
2. **Vernichtungs-Definition in MultiplayerModes.md §2** wird an die führende Regel aus VictoryConditions.md angeglichen: alle Gebäude (außer Mauern) + alle Einheiten (konsequent zu D-031.4).
3. **HQ-Grundenergie: +30** – Buildings.md führend; Economy.md bereits im Korrekturlauf angeglichen; Offener Punkt geschlossen.
**Konsequenzen:** Mikro-Edits in MultiplayerModes.md und Buildings.md; FogOfWar.md benötigt keine Änderung (Burrow bereits enthalten).

---

### D-033 | verbindlich | Sprint 3 (Q-013 – Simulations- & Multiplayer-Modell)

**Kontext:** Simulations- und Multiplayer-Architektur; Research-Vorlage [../research/Multiplayer_Simulation.md](../research/Multiplayer_Simulation.md), Vorverhandlung [sprints/Sprint01_Report.md](sprints/Sprint01_Report.md) §3.
**Alternativen:** (a) Striktes deterministisches Lockstep ab sofort (Fixed-Point überall, Unity-Physik-Verbot – bremst den MVP, ohne SP-Nutzen); (b) Server-autoritativer State-Sync (bei 500 voll sichtbaren Einheiten ~200–300 kB/s pro Client, Interest Management greift bei RTS-Gesamtsicht nicht – strukturell ungeeignet laut Research); (c) **Determinismus-fähige, befehlsgetriebene Tick-Simulation jetzt; deterministisches Lockstep über autoritativem Command-Relay-Server als Zielarchitektur ab Beta.**
**Entscheidung:** (c).
**Begründung:** Die fünf Architekturregeln (Command-getriebener fester Tick, strikte Simulation/View-Trennung ohne UnityEngine-APIs im Sim-Pfad, eigener seedbarer PRNG, serialisierbarer State, Singleplayer als "lokaler Server") machen MP später zu einem Transport-Thema statt eines Rewrites, ohne den MVP mit Fixed-Point-Disziplin zu belasten. Lockstep über Command-Relay erfüllt TPD §9 (Server autoritativ über Befehle, Takt, Match-Ergebnis) und liefert Replays/Beobachter gratis. Maphack-Risiko (voller Zustand auf jedem Client) ist für MVP/Koop akzeptabel (SC2-Präzedenz); für Ranked Pflicht-Re-Evaluierung mit serverseitigem Sichtgrid.
**Konsequenzen:** Networking.md/Replication.md/GameState.md spezifizieren die 5 Regeln; Float im MVP erlaubt, Fixed-Point-Umstellung fester Bestandteil der Beta-MP-Arbeiten; Phase-0-Spike validiert Fixed-Point-Determinismus ARM↔x86; Netzwerk-Framework: Eigenbau-UDP-Relay primär, Photon Quantum 3 als dokumentierter Fallback; Disconnect-Regel (KI-Übernahme) und Host-Migration in Networking.md final zu definieren.

### D-034 | verbindlich | Sprint 3 (Q-014 – Pathfinding)

**Kontext:** Pathfinding für 100–500+ Einheiten, Formationen, dynamische Hindernisse; Research-Vorlage [../research/Pathfinding.md](../research/Pathfinding.md).
**Alternativen:** (a) A* auf Uniform Grid allein (skaliert nicht bei "viele Einheiten, ein Ziel", Stau in Engstellen); (b) Unity NavMesh (Performance-Probleme ab ~200–800 Agents, teure Re-Bakes bei zerstörbarer Umgebung, ein Bake pro Radius, keine Lockstep-Eignung – ausgeschieden); (c) A* Pathfinding Project (Granberg, $140/Seat – stark, aber kein natives Flow Field; als dokumentierter Fallback); (d) **Hybrider Eigenbau: uniformes Integer-Grid + Flow Fields (Dijkstra-Maps) für globale Gruppenwegfindung + lokale ORCA-/Boids-Vermeidung, Jobs/Burst.**
**Entscheidung:** (d). MVP-Ausprägung: Grid + Gruppen-Flow-Field + einfache Separation; ORCA folgt in der Alpha.
**Begründung:** Flow Fields sind die für den RTS-Dominanzfall belegte Lösung (Supreme Commander 2, Planetary Annihilation); das Integer-Grid dient doppelt für FoW (1-m-Raster), Biome-Effekte, Aetherium-Ausbreitung und Bauplatzierung – eine Grid-Infrastruktur statt vier Einzelsysteme; Grid-Datenmodell hält Lockstep (D-033) offen.
**Konsequenzen:** tech/Pathfinding.md spezifiziert Grid (Tile-Größe 1 m), Clearance-Layer für 2–3 Radienklassen, ereignisgetriebenes Dirty-Flagging für dynamische Hindernisse (Mauern, Trümmer, D-012), separate Steering-Schicht für Lufteinheiten; CPU-Budget ≤2–4 ms (Phase-0-Spike-Messung); HPA* als mögliche Ergänzung für L-Karten vorgemerkt, nicht verplant.

### D-035 | verbindlich | Sprint 3 (Q-015 – ECS/DOTS)

**Kontext:** Codebasis-Grundarchitektur; Research-Vorlage [../research/Unity_ECS_DOTS.md](../research/Unity_ECS_DOTS.md), bestätigt durch [../research/Unity_BestPractices.md](../research/Unity_BestPractices.md).
**Alternativen:** (a) Vollständiges DOTS/ECS (Entities 1.4 noch experimental, "ECS for All"-Umbruch mit Breaking Changes, bricht Asset-Store-Strategie, kein echter Determinismus-Vorteil, schlechteres Debugging/Tooling); (b) klassisches MonoBehaviour-OOP pur (MP-/Performance-Risiko bei Hotspots); (c) **Klassische MonoBehaviour-OOP + ScriptableObjects als Gerüst, Burst + Job System auf NativeArray-Daten für Simulations-Hotspots (Pathfinding, FoW, Sicht), strikte Trennung mit Unity-freiem `Nova.Simulation`-Kern.**
**Entscheidung:** (c). Kein Unity Entities im MVP; Re-Evaluierung als Sim-Kern-Migrationsoption nach Unity 6.4 (Entities als Core Package).
**Begründung:** Für 500 Einheiten reicht Burst/Jobs gut aus; Voll-DOTS wäre Overkill mit Reifegrad-Risiko in der Umbruchphase; die Asset-Store-Kaufstrategie (MonoBehaviour-basierte Assets) bleibt nutzbar; KI-Coding-Agenten-Wartbarkeit und Testbarkeit sind im OOP/SO-Modell am besten.
**Konsequenzen:** Assembly-Struktur mit Unity-unabhängiger `Nova.Simulation`-Assembly (Voraussetzung für D-033 und D-036); CodingGuidelines.md legt Hotspot-Regeln (kein GC im Tick, UnityEngine.Pool) fest; Präsentationsschicht darf Unity-APIs voll nutzen.

### D-036 | verbindlich | Sprint 3 (Q-020 – Headless-KI-Runner)

**Kontext:** Balancing-Pipeline Stufe 2 (KI-vs-KI-Simulationsläufe, Balancing.md) braucht headless lauffähige Matches; Aufwand war ungeschätzt.
**Alternativen:** (a) Unity-Editor-Batchmode-Runs (langsam, CI-feindlich, Editor-Lizenz nötig); (b) Cloud-Sim-Farm (Overkill für den Bedarf); (c) **`Nova.Simulation` als reine .NET-Assembly ohne Unity-Abhängigkeit + schlanker Konsolen-Runner (`Nova.SimRunner`)**; (d) kein Runner (Balancing-Pipeline Stufe 2 entfällt).
**Entscheidung:** (c).
**Begründung:** Durch D-033 (keine UnityEngine-APIs im Sim-Pfad) und D-035 (Unity-freie Sim-Assembly) ist der Runner ein Nebenprodukt mit geringem Zusatzaufwand – er erzwingt gleichzeitig die Disziplin der Sim/Core-Trennung und liefert reproduzierbare Match-Fixtures für Tests und Desync-Jagd.
**Konsequenzen:** Testing.md definiert CI-Integration (KI-vs-KI-Nachtläufe, Match-Result-Datensatz aus VictoryConditions.md); SimRunner ist Pflicht-Bestandteil des Sim-Kern-Moduls in Sprint 7; Balancing.md Stufe 2 damit abgesichert.

---

### D-037 | verbindlich | Sprint 3 (Burst vs. Unity-freie Simulation)

**Kontext:** D-033/D-035 fordern einen 100 % Unity-freien `Nova.Simulation`-Kern und einen .NET-Konsolen-SimRunner (D-036); D-034 fordert Burst/Jobs für Pathfinding-Hotspots – `Unity.Burst`/`Unity.Jobs` laufen aber nicht in einer Unity-freien Konsolen-App. Von drei TDD-Agenten unabhängig als Spannung gemeldet.
**Alternativen:** (a) Burst-Referenzen im Sim-Kern akzeptieren (SimRunner nicht mehr Unity-frei – bricht D-036 und die Balancing-Pipeline); (b) vollständig auf Burst verzichten (Performance-Risiko gegen D-034-Budget); (c) getrennte Assembly `Nova.Simulation.Burst` mit Managed-Referenzimplementierung und Pflicht-Hash-Parität.
**Entscheidung:** (c) – wie in [../tech/FolderStructure.md](../tech/FolderStructure.md) und [../tech/CodingGuidelines.md](../tech/CodingGuidelines.md) ausgeführt: Sim-Kern bleibt 100 % managed und Unity-frei (`noEngineReferences`); Burst-Optimierungen leben in einer separaten Assembly hinter identischen Interfaces; SimRunner und Golden-Master-Tests fahren den Managed-Pfad; Paritäts-Hash-Tests (Managed ↔ Burst) sind CI-Pflicht.
**Begründung:** Erhält alle drei Entscheidungen gleichzeitig; die Doppelimplementierung ist auf wenige benannte Hotspots begrenzt; Re-Evaluierung nach Phase-0-Messung – hält der Managed-Pfad das ≤2–4-ms-Budget, kann Burst ganz entfallen.
**Konsequenzen:** CI-Paritäts-Tests in [../tech/Testing.md](../tech/Testing.md); Budget-Messung im Phase-0-Spike.

### D-038 | verbindlich | Sprint 3 (Disconnect-Regel final)

**Kontext:** [../tech/Networking.md](../tech/Networking.md) legte die finale Regel fest; [../gamedesign/VictoryConditions.md](../gamedesign/VictoryConditions.md) sagt "Verbindungsverlust > 120 s = Niederlage", [../gamedesign/MultiplayerModes.md](../gamedesign/MultiplayerModes.md) markiert die Regel als "vorläufig" – Bestandskonflikt.
**Alternativen:** (a) Pause-Vote mit Wartezeit (missbrauchbar/Griefing); (b) Auto-Niederlage nach Timeout (bestraft flüchtige Netzprobleme, ruiniert Team-Matches); (c) **60-s-Grace-Period mit Reconnect-Fenster, danach KI-Übernahme; Match läuft unpausiert weiter; kein Re-Entry nach Übernahme (Maphack-Vektor).**
**Entscheidung:** (c).
**Begründung:** Hält Matches für Verbleibende spielbar, bestraft niemanden für Verbindungsabbrüche und schließt den Informations-Exploit; passt zur Relay-Architektur (D-033), in der Host-Migration strukturell entfällt.
**Konsequenzen:** VictoryConditions.md und MultiplayerModes.md werden angeglichen (führend: Networking.md); KI-Übernahme nutzt das Mittel-Difficulty-Profil.

### D-039 | verbindlich | Sprint 3 (Audio-Backend)

**Kontext:** Research-Empfehlung [../research/Animation_Audio_UI.md](../research/Animation_Audio_UI.md) hatte noch keine Entscheidungs-ID (Verfahrenslücke, von AudioArchitecture.md gemeldet).
**Alternativen:** (a) Unity Audio dauerhaft (kein Voice-Priorisierungs-/Stealing-System – skaliert nicht bei 500 Einheiten); (b) FMOD sofort im MVP (Integrations-Overhead vor dem ersten spielbaren Build); (c) Wwise (pro Plattform lizenziert, für diesen Scope überdimensioniert); (d) **Unity Audio im MVP hinter stabiler `IAudioService`-Abstraktion, FMOD als committed Middleware ab Alpha.**
**Entscheidung:** (d).
**Begründung:** Die Abstraktion macht den Middleware-Wechsel zum Nicht-Ereignis; FMOD ist unter $200k Umsatz kostenlos und löst genau das RTS-Kernproblem (hunderte Barks, adaptive Musik); Wwise-Kosten/Nutzen passt nicht.
**Konsequenzen:** [../tech/AudioArchitecture.md](../tech/AudioArchitecture.md) führend; FMOD-Budgetpunkt in Sprint 6 aufnehmen.

### D-040 | verbindlich | Sprint 3 (Renderer- und Licht-Festlegungen)

**Kontext:** [../tech/Rendering.md](../tech/Rendering.md)/[../tech/Lighting.md](../tech/Lighting.md) trafen begründete Festlegungen ohne D-ID.
**Alternativen (Renderer):** (a) Forward+ (unnötig bei kleinem dynamischem Lichtbudget ~8 Punktlichter); (b) **Forward** (ausreichend, günstiger); (c) HDRP (längst verworfen, D-006).
**Alternativen (Licht):** (a) Lightmap-Baking (D-010-Ausbreitung und D-012-Zerstörbarkeit machen statische Bakes zur Lüge); (b) Mixed-Baking (Komplexität ohne Nutzen bei ständig ändernder Topologie); (c) **Realtime-only: ein dominantes Directional Light + Light Probes + Gradient-Ambient.**
**Entscheidung:** (b) Forward bzw. (c) Realtime-only.
**Begründung:** Dynamische Welt (Ausbreitung, Zerstörung, Hazards) verlangt dynamisches Licht; das Lichtbudget ist bewusst klein (VfxLightPool-Cap 8), womit Forward+ keinen Mehrwert hat.
**Konsequenzen:** Kampagnen-Nahaufnahmen (Phase 3) dürfen Forward+ erneut evaluieren.

### D-041 | verbindlich | Sprint 3 (Crash-Reporting)

**Kontext:** [../tech/Deployment.md](../tech/Deployment.md) lieferte die Vergleichsvorlage (D-037-Kandidat, umbenannt).
**Alternativen:** (a) Unity Cloud Diagnostics (komfortabel, aber Vendor-Bindung, Datenschutz-Fragen); (b) **Sentry** (Symbolik, Self-hosting-Option, Datensparsamkeit); (c) kein Crash-Reporting (widerspricht TPD §15 Stabilität).
**Entscheidung:** (b) – Sentry, Self-hosting-Option prüfend.
**Begründung:** Passt zur Premium-Offline-Positionierung (D-007) und Datensparsamkeit; bessere Symbolik für C#-Stacks.
**Konsequenzen:** Integration ab Alpha-Builds; Opt-out-Hinweis in Release-Checkliste.

### D-042 | verbindlich | Sprint 3 (Sim-Budget- und Detailklärungen)

**Kontext:** Drei Querschnitts-Klärungen aus dem TDD-Review.
**Entscheidungen:**
1. **Sim-Tick-Gesamtbudget ≤8 ms** (Architecture.md führend; [../tech/PerformanceBudget.md](../tech/PerformanceBudget.md) wird angeglichen). Unterbudgets: Pathfinding ≤4 ms, FoW ≤1 ms, Rest-Sim ≤3 ms. *Löst die Spannung "D-034 ≤2–4 ms PF bei nur 4 ms Gesamt-Sim" auf.* Bei 10-Hz-Tick (100 ms Fenster) ist 8 ms unkritisch; 30-FPS-Modus degradiert nur die View, nie die Sim.
2. **Trümmer-Persistenz:** Fade-out nach 60 s mit hartem Cap (Design-Festlegung; schützt das Dreieck-Budget aus [../tech/AssetBudget.md](../tech/AssetBudget.md), C&C-typisch).
3. **Replay-Vollaufzeichnung (FoW-Verlauf):** nicht geplant – nur mit Delta-Kodierung machbar (~2,7 GB/Match unkomprimiert); Post-Release-Kandidat.
**Konsequenzen:** PerformanceBudget.md-Angleichung; Rendering.md/GameState.md vermerken Trümmer-Regel.

---

### D-043 | verbindlich | Sprint 4 (Kanonische Assembly-Topologie)

**Kontext:** Review-Befund (3× unabhängig: Architektur-Kohärenz F-1, Wartbarkeit F-01, GDD↔TDD F-10): Drei konkurrierende Assembly-/Namensarchitekturen koexistieren im TDD – Architecture/ModuleOverview/DependencyGraph (`Nova.Game`, `Nova.UI`, `Nova.Tools`, `Nova.Simulation.Jobs`) vs. FolderStructure/CodingGuidelines/NamingConvention (`Nova.Core`, `Nova.Gameplay`, `Nova.Editor`, `Nova.Simulation.Burst`) vs. AIArchitecture (`Nova.AI`, `Nova.AI.Data`).
**Alternativen:** (a) Architecture-Lager; (b) FolderStructure-Lager; (c) Neusynthese.
**Entscheidung:** (c) – kanonische Topologie: `Nova.Core`, `Nova.Simulation` (Unity-frei), `Nova.Simulation.Burst` (D-037), `Nova.AI` (Unity-frei, SimRunner-tauglich), `Nova.AI.Data` (SOs), `Nova.Data` (SOs), `Nova.Gameplay` (Bridge), `Nova.Presentation`, `Nova.UI`, `Nova.Editor`, `Nova.SimRunner` (externes .NET-Projekt), `Nova.BuildTools`. FolderStructure-Lager führend, ergänzt um `Nova.AI`/`Nova.AI.Data`.
**Begründung:** D-037 verlangt die Burst-Trennung, D-036 den SimRunner-Bezug, die KI-Architektur begründet ihre Unity-Freiheit überzeugend (Records statt SOs im Entscheidungspfad); nur eine Neusynthese erfüllt alle drei.
**Konsequenzen:** Architecture.md, ModuleOverview.md, DependencyGraph.md werden angeglichen; Assembly-Name steht im Datei-Header jeder .cs-Datei (Fehlwahl = codebase-weites Rewrite, daher vor Sprint 7 verbindlich).

### D-044 | verbindlich | Sprint 4 (Sim-Tick-Ausführungsmodell + Validierungs-Gate V5)

**Kontext:** Performance-Review F-1/F-7: Rest-Sim-Unterbudget ≤3 ms (Kampf, Wirtschaft, KI) ist unbelegt; synchrones Ausführungsmodell erzeugt Mikro-Ruckler (13,5 ms seriell im Worst Case); Zielsuche ohne Spatial-Struktur wäre O(n²).
**Alternativen:** (a) synchron im Main-Thread; (b) Worker-Tick, View rendert Snapshot n−1; (c) gestuft.
**Entscheidung:** (c) – **MVP synchron** (einfach, 100-ms-Tick-Fenster, MVP-Last 100 Einheiten unkritisch); **Wechsel auf Worker-Tick ab Alpha, falls die P95-Messung >6 ms zeigt** (D-033 bereitet das vor). Zusätzlich **Pflicht-Gate V5 im Phase-0-Spike: Combat-/KI-Kostenmodell** (Targeting mit Spatial-Hash als Pflichtbestand des Kampfmoduls, FoW-Filter, KI-Command-Verarbeitung) – ohne V5 kein Sprint-7-Start des Kampfmoduls.
**Konsequenzen:** PerformanceBudget.md (V5-Gate, Ausführungsmodell), Testing.md (V5-Kriterien), Architecture.md (Worker-Tick-Vorhaltung).

### D-045 | verbindlich | Sprint 4 (Auslieferungspfad Managed-first – D-037 präzisiert)

**Kontext:** Performance-Review F-2 und Wartbarkeit F-03: Bit-Parität Managed↔Burst ist im Float-Regime nicht garantiert; CI misst Managed, das Spiel liefe auf Burst – Messblindheit und Desync-Risiko bei grüner CI.
**Alternativen:** (a) Burst als Primärpfad mit Bit-Paritätsgebot (nicht einlösbar); (b) **Managed als einziger Auslieferungspfad bis zur Fixed-Point-Beta; Burst nur hinter Feature-Flag mit Toleranz-Parität**; (c) Burst komplett streichen.
**Entscheidung:** (b). Toleranz-Parität: relative Abweichung ≤1e-4 im Hash-Vergleich löst Alarm aus, blockiert aber nicht; Bit-Parität wird erst mit Fixed-Point (Beta) relevant und dann neu bewertet.
**Begründung:** CI/Golden-Master und Auslieferung messen denselben Pfad; Burst bleibt als Beschleunigungsoption erhalten, ohne die Determinismus-Kette zu gefährden.
**Konsequenzen:** CodingGuidelines.md/Testing.md/PerformanceBudget.md angleichen; D-037 bleibt gültig, wird durch D-045 präzisiert.

### D-046 | verbindlich | Sprint 4 (MP-Trust-Anchor & deterministische KI-Übernahme)

**Kontext:** Multiplayer-Review F-01/F-03/F-06: Relay ohne eigene Sim hat keinen Trust-Anchor (1v1-Ergebniskonflikt unlösbar, Client-Upload-Snapshot = Manipulationsvektor); Desync-Arbitration im 1v1 unmöglich; Ausführungsort der D-038-Übernahme-KI undefiniert (SPOF).
**Alternativen:** (a) Server-seitige Vollsimulation (Hosting-Kosten, zweite Sim als Desync-Quelle); (b) Client-Mehrheitsvotum (1v1 unlösbar, Kollusion); (c) **Post-Match-Re-Sim + Hash-Kette + deterministische KI-Übernahme.**
**Entscheidung:** (c) – (1) Der Server validiert Match-Ergebnis und schlichtet Desync-/Ergebniskonflikte per **Post-Match-Re-Simulation des Command-Logs** (SimRunner-basiert, on-demand, nicht dauerhaft); (2) Reconnect-Snapshots werden gegen die **Pre-Disconnect-Hash-Historie** des betreffenden Clients geprüft (Upload nur mit lückenloser Hash-Kette); (3) die D-038-KI-Übernahme ist ein **deterministisches Sim-Ereignis**: alle Clients schalten den Slot tick-synchron auf die Ersatz-KI (Mittel-Profil) – kein Server-Prozess, kein SPOF.
**Begründung:** Nutzt die vorhandene Lockstep-/SimRunner-Architektur (D-033/D-036), ohne laufende Server-Sim-Kosten; macht "Server autoritativ über Match-Ergebnis" (TPD §9) einlösbar.
**Konsequenzen:** Networking.md/Replication.md nachschärfen (Beta-Scope); Reconnect- und Desync-Flows finalisieren.

### D-047 | verbindlich | Sprint 4 (Einheiten & Reichweiten – GDD-Harmonisierung)

**Kontext:** GDD↔TDD-Review F-01 (KRITISCH): Weapons.md definiert Reichweiten in Grid-Feldern (Flak 11–12, Artillerie 18–24), Vehicles.md/Aircraft.md in Metern mit dem 2,5–4-fachen Wert (Flak 55 m, Artillerie 80–85 m), FoW-Sichtweiten 8–18 m – ohne führende Quelle nicht implementierbar.
**Alternativen:** (a) Vehicles/Aircraft führend (Flak 55 m) – würde Weapons.md und das Grid-Konzept brechen; (b) Weapons.md führend, 1 Feld = 1 m – Vehicles/Aircraft angleichen; (c) FoW-Sichtweiten hochskalieren – bricht das Scouting-Prinzip.
**Entscheidung:** (b) – **1 Tile = 1 m** (D-034 bestätigt); führende Quelle für Waffenreichweiten ist Weapons.md; Vehicles.md/Aircraft.md werden angeglichen. **Angriffsreichweite > Sichtweite ist Design-Prinzip** (Scouting/Spotter, C&C-konform), kein Fehler: Sichtklassen aus FogOfWar.md bleiben unverändert.
**Konsequenzen:** GDD-Korrekturlauf (Vehicles.md, Aircraft.md, Querverweise); Grundsatzregel "jeder Wert existiert genau einmal, alles andere sind Verweise" wird im DocumentationStandard ergänzt.

### D-048 | verbindlich | Sprint 4 (Skalierungs-Deckel: Einheiten, Survival, Density)

**Kontext:** Skalierungs-Review F-1/F-2 (KRITISCH/HOCH): Die Kalibrierung "500 Einheiten" wird nirgends erzwungen; Survival-Endlos (+25 %/Welle) erreicht Welle 20 ≈ 555 Einheiten allein in einer Welle; FFA-6 mit Density 2,0 sprengt jedes Budget.
**Alternativen:** (a) unbegrenzt (Engine-Bruch absehbar); (b) hartes Pop-Limit pro Spieler (widerspricht D-021-Geist); (c) **globale, performance-kalibrierte Deckel mit lesbaren Regeln.**
**Entscheidung:** (c) – (1) **Globales Einheiten-Deckel 600/Match:** bei Erreichen Produktionsstopp mit UI-Hinweis ("Maximale Armeegröße erreicht"); (2) **Survival:** Welle 20 = Standardsieg (unverändert); Endlos-Modus mit Stärke-Abflachung ab Welle 25 (linear statt multiplikativ) und Despawn älterer Wellenreste – Deckel 600 gilt immer; (3) **MatchSettings `AetheriumDensity` ≤1,5 bei 5–6 Spielern.**
**Begründung:** Macht die 500-Einheiten-Kalibrierung erzwungen statt angenommen; behält D-021 (kein Supply-Mikromanagement), weil der Deckel nur im Extremfall greift.
**Konsequenzen:** MultiplayerModes.md, GameState.md (UnitCounter), PerformanceBudget.md, Balancing.md angleichen.

### D-049 | verbindlich | Sprint 4 (Test-/CI-Realismus, Hash-Breite, Registry-Sharding)

**Kontext:** Skalierungs-Review F-3 (SimRunner-Nightly rechnerisch unmöglich: 22–43 h seriell), Wartbarkeit F-05 (GameDatabase als Single-File = Merge-Konflikt-Magnet), GDD↔TDD-Review (Hash-Breiten-Inkonsistenz xxHash32 vs. xxHash64).
**Entscheidungen:** (1) **SimRunner-CI:** Nightly = 6 Matchup-Cluster × 20 Matches auf 8 parallele Shards; 200-Match-Vollläufe wöchentlich; Zielvorgabe ≤60 s/Match (Managed) statt "<10 s". (2) **xxHash64 überall** (Serialization.md angleichen). (3) **GameDatabase-Sharding:** Sub-Registries pro Kategorie (Units, Buildings, Weapons, Tech, Factions, Maps, Biomes, AI) + generierte Master-Index-Datei statt eines einzelnen Registry-Assets.
**Begründung:** CI muss über Nacht laufen; parallele Agenten-Arbeit (Worktrees, TPD §12) verträgt keine Single-File-Registry; 64-bit-Hashes halbieren die Kollisionswahrscheinlichkeit bei langen Replay-Serien.
**Konsequenzen:** Testing.md, Deployment.md, Serialization.md, FolderStructure.md, NamingConvention.md angleichen.

### D-050 | verbindlich | Sprint 4 (Branching-Modell)

**Kontext:** Wartbarkeit F-07: AGENTS.md (PR→main) vs. Deployment.md/Testing.md (develop-Integration) – zwei Branching-Modelle aktiv; TPD §12 definiert develop.
**Alternativen:** (a) TPD-Modell mit develop sofort; (b) trunk-based main-only dauerhaft; (c) gestuft.
**Entscheidung:** (c) – **Doku-Phase (bis Sprint 6): `main` + kurze Feature-/Sprint-Branches mit PR**; **ab Sprint 7 (Code-Phase): TPD §12 vollständig** (`main`/`develop`/`feature`/`fix`/`art`/`release`).
**Begründung:** develop-Overhead lohnt erst bei parallelisiertem Code; die Doku-Phase profitiert von trunk-basierter Einfachheit; TPD-Modell bleibt das Zielbild für Code.
**Konsequenzen:** AGENTS.md, Deployment.md, Testing.md angleichen.

### D-051 | verbindlich | Sprint 4 (Quantum-Fallback gestrichen)

**Kontext:** Multiplayer-Review F-05: Photon Quantum 3 als "Fallback" wäre faktisch ein Rewrite (Gameplay-Code in Quantum-DSL/ECS), kein Fallback; alle drei Trigger-Kriterien waren nicht messbar.
**Alternativen:** (a) Quantum-Fallback behalten (Schein-Sicherheit); (b) **Fallback = eigenes Relay mit reduziertem Scope**; (c) gar kein Fallback-Konzept.
**Entscheidung:** (b) – Quantum-Fallback gestrichen. Neuer Beta-Fallback bei Scheitern des Eigenbau-Relay: **Reduzierter MP-Scope** (max 4 Spieler, 300 Einheiten, EU-only). Ein vollständiger Strategiewechsel (Quantum o. ä.) wäre eine neue Grundsatzentscheidung nach totalem Scheitern, kein "Fallback".
**Konsequenzen:** Networking.md/Replication.md angleichen; R-12-Risikoregister aktualisieren.

### D-052 | verbindlich | Sprint 4 (Windows-Referenzhardware)

**Kontext:** Offener Punkt aus Sprint 3 (PerformanceBudget): Referenzhardware für alle P95-Messungen fixieren.
**Alternativen (Klasse):** (a) High-End (messwert-fern der Zielgruppe); (b) **Mittelklasse der H1-Zielgruppe**; (c) Minimum-Spec als Referenz (zu pessimistisch für 60-FPS-Ziel).
**Entscheidung:** (b) – **Referenz (60-FPS-Ziel): Ryzen 5 5600 / RTX 3060 / 16 GB / NVMe-SSD**; **Minimum (30-FPS-Ziel): Ryzen 3 3100 / GTX 1050 Ti / 8 GB**; **Mac-Baseline: Apple M2** (Entwicklungs- und Qualitätsplattform, D-006).
**Konsequenzen:** PerformanceBudget.md; Beschaffung in Sprint-6-Planung; Messungen auf Standalone-Builds (nie Editor).

### D-053 | verbindlich | Sprint 5 (Asset-Beschaffungsstrategie)

**Kontext:** Der Asset Audit (Sprint 5) muss eine verbindliche Beschaffungsstrategie ratifizieren; die Entscheidungsvorlage lag als Research-Ergebnis vor ([../research/AssetStore_Landschaft.md](../research/AssetStore_Landschaft.md), „Sprint 5 zu bestätigen").
**Alternativen:** (a) **Asset Store only (Synty-zentriert)** – scheitert an den biologischen Evolvierten und am Signature-Aetherium, Publisher-Abhängigkeit ohne Preishebel; (b) **Multi-Store-Mix mit Synty als Stil-Anker** (Asset Store + Humble-Bundles + CC0 + Fab/Sketchfab + Sonniss-Audio); (c) **BUILD-first** (nur Tools/Audio kaufen) – gefährdet MVP-Disziplin und Zeitplan durch Eigenbau von ~130+ Modellen ohne Qualitätsvorteil auf RTS-Distanz.
**Entscheidung:** (b) – **Multi-Store-Mix mit Synty als Stil-Anker.** Menschliche Fraktionen (Allianz/Legion), Biome, UI-Icons und Basis-Animationen werden gekauft; **Aetherium, die komplette Evolvierten-Fraktion und alle Fraktions-Signaturen werden MODIFY/BUILD.** Leitplanken: URP-Kompatibilität als K.O.-Kriterium (Badge + Testprojekt), **keine RTS-Komplett-Frameworks** (Kollision mit D-033/D-035/D-043), einheitlicher URP-Material-Standard mit Teamfarben-Masken (Gegenmittel R-04), Lizenz-Register-Pflicht ([assets/Licenses.md](../assets/Licenses.md)), keine Rohdaten im öffentlichen Repo.
**Begründung:** Nutzt den dokumentierten Preishebel (Synty-Humble-Bundles ~30 USD statt >600 USD) und CC0 für Prototyping, deckt die menschlichen Fraktionen käuflich ab und reserviert Eigenbau gezielt für das Unverwechselbare. Die nötige Lizenz-/URP-Disziplin institutionalisiert der Audit selbst.
**Konsequenzen:** [../assets/ProcurementStrategy.md](../assets/ProcurementStrategy.md), [../assets/AssetRegister.md](../assets/AssetRegister.md), [../assets/Licenses.md](../assets/Licenses.md), [../assets/BuildBacklog.md](../assets/BuildBacklog.md); Budget-Obergrenze bleibt Inhaberentscheidung (Q-035); reale Käufe erst ab Phase 0/Sprint 7.

### D-054 | verbindlich | Sprint 5 / Inhaberentscheidung (0 € Open-Source & KI-Asset-Pipeline)

**Kontext:** Auflösung von Q-035 (Asset-Budget-Obergrenze). Project Nova wird als rein organisches Open-Source-Projekt ohne festes Studio-Budget entwickelt (0 € Budget-Vorgabe des Project Owners).
**Alternativen:** (a) **Kommerzieller Store-Kauf (Multi-Store-Mix, ehemals D-053)** – verworfen, da $0-Budget vorgegeben ist und gekaufte Rohdaten nicht im öffentlichen Open-Source-Repo weitergegeben werden dürfen; (b) **0 € Open-Source & KI-Asset-Pipeline (gewählt)** – Nutzung freier CC0-Bibliotheken (Quaternius, Kenney, Sonniss GDC Audio, Poly Pizza, OpenGameArt), KI-3D-Generierung (Hunyuan3D, Meshy, Tripo3D), KI-Textur-Generierung (SD / Texture Lab / UI-Icons) und Community-Kitbashing in Blender; (c) **100% Eigenbau ohne KI/CC0** – verworfen, da der reine Eigenbau von ~135+ Modellen ohne CC0-Basen und KI-Drafting das Entwicklungs-Tempo stark bremst.
**Entscheidung:** (b) – **0 € Open-Source & KI-Asset-Pipeline.** Die Beschaffung richtet sich vollständig auf lizenziell freie (CC0/Public Domain) und KI-generierte Assets aus. **Das Asset-Budget beträgt 0 € (Q-035 geschlossen).** Sämtliche Spiel-Assets werden im **öffentlichen GitHub-Repository** mitgeführt und gepflegt (da CC0/KI keine per-Seat- oder Rohdaten-Weitergabeverbote erzwingen).
**Begründung:** Beschluss des Project Owners. Ermöglicht volle Open-Source-Transparenz, schließt Lizenz- & Seat-Kosten aus und nutzt das Potenzial motivierter Community-Volunteers und moderner KI-Workflow-Tools.
**Konsequenzen:** [OpenQuestions.md](OpenQuestions.md) Q-035 geschlossen; [ProcurementStrategy.md](../assets/ProcurementStrategy.md) auf Version 1.1.0 angepasst; KI-Drafting & CC0-Quellen in [AssetRegister.md](../assets/AssetRegister.md) verankert.

### D-055 | verbindlich | Sprint 7 (Recovery-Baseline nach Implementierungs-Audit)

**Kontext:** Der Stand `460290e` dokumentierte Fortschritt bis in MVP und Alpha, obwohl der unabhängige [Implementierungs-Audit](ImplementationAudit_2026-07-24.md) einen roten Test, einen nicht angeschlossenen Command-Pfad, fehlende spielbare Inhalte sowie unvollständige Hash-/Replay-Nachweise belegt. MS-0 wurde weder plattformübergreifend noch auf Referenzhardware nachgewiesen.
**Alternativen:** (a) Bisherige Statusangaben beibehalten und Defekte parallel beheben – verworfen, weil Anwesenheit von Dateien keinen Meilenstein belegt; (b) Code als forensisch wertvollen Prototyp erhalten, unbelegte Statusangaben zurückziehen und jeden Fortschritt erneut über überprüfbare Gates qualifizieren; (c) sämtliche Implementierung verwerfen und auf Sprint 6 zurücksetzen – verworfen, weil brauchbare Prototypteile und Tests vorhanden sind.
**Entscheidung:** (b) – **Recovery-Baseline mit beweispflichtigen Gates.** MS-0 ist offen, das MVP ist nicht erreicht und Alpha hat nicht begonnen. Die Module 1–19 gelten höchstens als Prototyp oder Scaffolding. Aktiver Arbeitsstand ist ausschließlich Gate G0 des [MVP-Recovery-Plans](MVPRecoveryPlan.md).
**Begründung:** Diese Einstufung trennt überprüfbare Laufzeit-Evidenz von erzeugter Struktur und verhindert, dass weitere Planung auf falschen Fertigmeldungen aufbaut.
**Konsequenzen:** Der Sprint-6-Abschluss und das Sprint-7-GO werden zurückgezogen. Roadmap und Meilensteine sind bis zur Neu-Schätzung nicht terminverbindlich. Alpha-Erweiterungen bleiben gesperrt, bis G5 bestanden ist. Der Projektinhaber entscheidet Q-038; Q-039 muss vor Abschluss von G1 technisch und dokumentarisch aufgelöst sein.

---

## Offene Punkte

- Alle Sprint-4-Review-Befunde (105, davon 9 kritisch): 7 entscheidungsbedürftige kritische Befunde sind durch D-043–D-052 entschieden.
- Q-018 (Preispunkt) und Q-019 (Telemetrie) bleiben offen; Sprint 6 hat dafür keine gültige D-ID erzeugt.
- Q-038 (verbindlicher MVP-Zuschnitt) und Q-039 (Fixed-Point-Konflikt) blockieren die Recovery-Gates.
- Fixed-Point-Migration (Beta): Phase-0-Spike-Scope erweitert (Fixed-Point-Pfad für ORCA/Flow-Field evaluieren, Bibliothekswahl, float-Direktfelder im GameState verbieten) – Review F-04 MP.
- Sprint 5 (Asset Audit): D-053/D-054 ratifiziert; **Budget-Obergrenze ist mit 0 € geschlossen (Q-035, D-054)**; Seat-Planung (Q-036) entfällt/gegenstandslos; Bundle-Fenster-Monitoring (Q-037) entfällt zugunsten CC0/KI-Pipeline.

## Nächste Schritte

- Recovery-Gate G0 herstellen und mit reproduzierbaren Nachweisen schließen.
- Q-038 durch den Projektinhaber entscheiden und Q-039 vor Abschluss von G1 auflösen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-21 | D-001 bis D-005 aus Sprint 0 protokolliert | Game Director |
| 1.1.0 | 2026-07-21 | D-006 (Unity 6.3 LTS + URP bestätigt) aus Sprint-1-Validierung | Lead Technical Director |
| 1.2.0 | 2026-07-21 | D-007 bis D-019: verbindliche Game-Design-Grundlagen (Q-001–Q-012, Q-016, Q-017) | Game Director |
| 1.3.0 | 2026-07-21 | D-020 bis D-030: Konsistenzreview-Entscheidungen (Kampagne, Capture, Konter-Lücken, Sonderregeln, Karten, Modi, Forschung) | Game Director |
| 1.3.1 | 2026-07-21 | D-031: Feinschliff Runde 2 (HQ-Neuaufbau, Detektoren, Alpha-Koop, Survival-Harmonisierung, Regen-Kompensation, Plattform-Modi) | Game Director |
| 1.3.2 | 2026-07-21 | D-032: Restpunkte Feinschliff (Burrow-Detektion bestätigt, Vernichtungs-Definition harmonisiert, HQ-Grundenergie +30) | Game Director |
| 1.4.0 | 2026-07-21 | D-033 bis D-036: Architektur-Grundentscheidungen (Sim-/MP-Modell, Pathfinding, OOP+Burst statt DOTS, Headless-SimRunner) | Lead Technical Director |
| 1.5.0 | 2026-07-21 | D-037 bis D-042: TDD-Review-Entscheidungen (Burst/Managed-Doppelstruktur, Disconnect-Regel, Audio-Backend, Renderer/Licht, Sentry, Sim-Budget-Klärungen) | Lead Technical Director |
| 1.6.0 | 2026-07-21 | D-043 bis D-052: Architecture-Review-Entscheidungen (Assembly-Topologie, V5-Gate, Managed-first, MP-Trust-Anchor, Reichweiten-Harmonisierung, Skalierungs-Deckel, CI-Realismus, Branching, Quantum-Fallback gestrichen, Referenzhardware) | Lead Technical Director |
| 1.6.1 | 2026-07-21 | Korrektur „Offene Punkte": Kritisch-Zähler auf 9 (statt 10) berichtigt; präzisiert, dass F-02 (GDD↔TDD) und F-2 (Architektur-Kohärenz) als Doku-Erweiterung in GameState.md gelöst wurden statt durch D-043–D-052, da reine Datenmodell-Ergänzungen ohne eigenen Entscheidungsbedarf | Lead Technical Director |
| 1.7.0 | 2026-07-22 | D-053: Asset-Beschaffungsstrategie B (Multi-Store-Mix mit Synty als Stil-Anker) ratifiziert – Sprint 5 (Asset Audit) | Producer / Lead Technical Director |
| 1.8.0 | 2026-07-24 | D-054: 0 € Open-Source & KI-Asset-Pipeline (Inhaberentscheidung, Q-035 geschlossen) | Project Owner / Producer |
| 1.9.0 | 2026-07-24 | D-055: unbelegte MS-0-/MVP-/Alpha-Status zurückgezogen; beweispflichtige Recovery-Gates verbindlich gemacht | Project Owner / Lead Technical Director |
