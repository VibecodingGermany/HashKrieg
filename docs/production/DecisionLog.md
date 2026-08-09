# Decision Log

**Version:** 1.34.0 | **Status:** aktiv (laufend) | **Verantwortungsbereich:** Game Director / Lead Technical Director / Project Owner | **Sprint:** 16

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

### D-006 | ersetzt durch D-060 | Sprint 1

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

### D-009 | teilweise ersetzt durch D-056 (MS-1) | Sprint 2 (Q-002)

**Kontext:** Commander-System – im TPD nur als Signature-Asset genannt.
**Alternativen:** (a) RPG-artiger Commander mit Match-Mechanik und Progression; (b) Commander als rein narrative/präsentative Identität (Portrait, Voice, Story, Key Art); (c) komplett streichen.
**Entscheidung:** (b) – Commander als Identitäts-Layer ohne Match-Mechanik im MVP; optionales Doktrinen-System (kleine passive Fraktions-Varianten) frühestens ab Beta evaluieren.
**Begründung:** Ein mechanisches Commander-System ist ein zweites Balancing-Universum (R-01) und für die H1-Zielgruppe kein Kaufargument; als Identität liefert es die im TPD geforderte Unverwechselbarkeit (Signature-Assets) zu geringen Kosten.
**Konsequenzen:** CommanderSystem.md definiert Identität, Voice-Konzept und Doktrinen-Ausblick; kein Commander-Balancing im MVP.

**Teilersetzung für MS-1:** D-056 verschiebt Commander, Portrait/Key Art,
Voice-over und Doktrinen vollständig hinter G5. D-009 bleibt ausschließlich
als Post-MVP-/Vollspiel-Zielbild bestehen.

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

### D-033 | teilweise ersetzt durch D-057/D-089 | Sprint 3 (Q-013 – Simulations- & Multiplayer-Modell)

**Kontext:** Simulations- und Multiplayer-Architektur; Research-Vorlage [../research/Multiplayer_Simulation.md](../research/Multiplayer_Simulation.md), Vorverhandlung [sprints/Sprint01_Report.md](sprints/Sprint01_Report.md) §3.
**Alternativen:** (a) Striktes deterministisches Lockstep ab sofort (Fixed-Point überall, Unity-Physik-Verbot – bremst den MVP, ohne SP-Nutzen); (b) Server-autoritativer State-Sync (bei 500 voll sichtbaren Einheiten ~200–300 kB/s pro Client, Interest Management greift bei RTS-Gesamtsicht nicht – strukturell ungeeignet laut Research); (c) **Determinismus-fähige, befehlsgetriebene Tick-Simulation jetzt; deterministisches Lockstep über autoritativem Command-Relay-Server als Zielarchitektur ab Beta.**
**Entscheidung:** (c).
**Begründung:** Die fünf Architekturregeln (Command-getriebener fester Tick, strikte Simulation/View-Trennung ohne UnityEngine-APIs im Sim-Pfad, eigener seedbarer PRNG, serialisierbarer State, Singleplayer als "lokaler Server") machen MP später zu einem Transport-Thema statt eines Rewrites, ohne den MVP mit Fixed-Point-Disziplin zu belasten. Lockstep über Command-Relay erfüllt TPD §9 (Server autoritativ über Befehle, Takt, Match-Ergebnis) und liefert Replays/Beobachter gratis. Maphack-Risiko (voller Zustand auf jedem Client) ist für MVP/Koop akzeptabel (SC2-Präzedenz); für Ranked Pflicht-Re-Evaluierung mit serverseitigem Sichtgrid.
**Konsequenzen:** Networking.md/Replication.md/GameState.md spezifizieren die 5 Regeln; Float im MVP erlaubt, Fixed-Point-Umstellung fester Bestandteil der Beta-MP-Arbeiten; Phase-0-Spike validiert Fixed-Point-Determinismus ARM↔x86; Netzwerk-Framework: Eigenbau-UDP-Relay primär, Photon Quantum 3 als dokumentierter Fallback; Disconnect-Regel (KI-Übernahme) und Host-Migration in Networking.md final zu definieren.

**Fortschreibung:** D-057 ersetzte Float-/Persistence-Anteile; D-089 ersetzt
für das implementierte 1v1-Profil den UDP-Primärpfad durch TCP und nimmt dem
nicht simulierenden Relay die in D-033 angenommene Ergebnisautorität. Die fünf
Grundregeln der befehlsgetriebenen deterministischen Simulation bleiben
verbindlich. Der historische Wortlaut bleibt zur Nachvollziehbarkeit stehen.

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
**MS-1-Status:** Durch D-056/D-058 begrenzt: Unity Audio ist für MS-1
zulässig, Commander/Voice, adaptive Musik, finale Audio-Produktion und FMOD
sind jedoch Post-MVP; 500 Emitter sind ausschließlich synthetische
Architekturlast. Ein FMOD-Termin erfordert eine neue Post-MVP-Entscheidung.

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

### D-044 | teilweise ersetzt durch D-061 | Sprint 4 (Sim-Tick-Ausführungsmodell + Validierungs-Gate V5)

**Kontext:** Performance-Review F-1/F-7: Rest-Sim-Unterbudget ≤3 ms (Kampf, Wirtschaft, KI) ist unbelegt; synchrones Ausführungsmodell erzeugt Mikro-Ruckler (13,5 ms seriell im Worst Case); Zielsuche ohne Spatial-Struktur wäre O(n²).
**Alternativen:** (a) synchron im Main-Thread; (b) Worker-Tick, View rendert Snapshot n−1; (c) gestuft.
**Entscheidung:** (c) – **MVP synchron** (einfach, 100-ms-Tick-Fenster, MVP-Last 100 Einheiten unkritisch); **Wechsel auf Worker-Tick ab Alpha, falls die P95-Messung >6 ms zeigt** (D-033 bereitet das vor). Zusätzlich **Pflicht-Gate V5 im Phase-0-Spike: Combat-/KI-Kostenmodell** (Targeting mit Spatial-Hash als Pflichtbestand des Kampfmoduls, FoW-Filter, KI-Command-Verarbeitung) – ohne V5 kein Sprint-7-Start des Kampfmoduls.
**Konsequenzen:** PerformanceBudget.md (V5-Gate, Ausführungsmodell), Testing.md (V5-Kriterien), Architecture.md (Worker-Tick-Vorhaltung).

### D-045 | teilweise ersetzt durch D-057 | Sprint 4 (Auslieferungspfad Managed-first – D-037 präzisiert)

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

### D-048 | teilweise ersetzt durch D-058 (MS-1) | Sprint 4 (Skalierungs-Deckel: Einheiten, Survival, Density)

**Kontext:** Skalierungs-Review F-1/F-2 (KRITISCH/HOCH): Die Kalibrierung "500 Einheiten" wird nirgends erzwungen; Survival-Endlos (+25 %/Welle) erreicht Welle 20 ≈ 555 Einheiten allein in einer Welle; FFA-6 mit Density 2,0 sprengt jedes Budget.
**Alternativen:** (a) unbegrenzt (Engine-Bruch absehbar); (b) hartes Pop-Limit pro Spieler (widerspricht D-021-Geist); (c) **globale, performance-kalibrierte Deckel mit lesbaren Regeln.**
**Entscheidung:** (c) – (1) **Globales Einheiten-Deckel 600/Match:** bei Erreichen Produktionsstopp mit UI-Hinweis ("Maximale Armeegröße erreicht"); (2) **Survival:** Welle 20 = Standardsieg (unverändert); Endlos-Modus mit Stärke-Abflachung ab Welle 25 (linear statt multiplikativ) und Despawn älterer Wellenreste – Deckel 600 gilt immer; (3) **MatchSettings `AetheriumDensity` ≤1,5 bei 5–6 Spielern.**
**Begründung:** Macht die 500-Einheiten-Kalibrierung erzwungen statt angenommen; behält D-021 (kein Supply-Mikromanagement), weil der Deckel nur im Extremfall greift.
**Konsequenzen:** MultiplayerModes.md, GameState.md (UnitCounter), PerformanceBudget.md, Balancing.md angleichen.

### D-049 | teilweise ersetzt durch D-057/D-061 | Sprint 4 (Test-/CI-Realismus, Hash-Breite, Registry-Sharding)

**Kontext:** Skalierungs-Review F-3 (SimRunner-Nightly rechnerisch unmöglich: 22–43 h seriell), Wartbarkeit F-05 (GameDatabase als Single-File = Merge-Konflikt-Magnet), GDD↔TDD-Review (Hash-Breiten-Inkonsistenz xxHash32 vs. xxHash64).
**Entscheidungen:** (1) **SimRunner-CI:** Nightly = 6 Matchup-Cluster × 20 Matches auf 8 parallele Shards; 200-Match-Vollläufe wöchentlich; Zielvorgabe ≤60 s/Match (Managed) statt "<10 s". (2) **xxHash64 überall** (Serialization.md angleichen). (3) **GameDatabase-Sharding:** Sub-Registries pro Kategorie (Units, Buildings, Weapons, Tech, Factions, Maps, Biomes, AI) + generierte Master-Index-Datei statt eines einzelnen Registry-Assets.
**Begründung:** CI muss über Nacht laufen; parallele Agenten-Arbeit (Worktrees, TPD §12) verträgt keine Single-File-Registry; 64-bit-Hashes halbieren die Kollisionswahrscheinlichkeit bei langen Replay-Serien.
**Konsequenzen:** Testing.md, Deployment.md, Serialization.md, FolderStructure.md, NamingConvention.md angleichen.

### D-050 | ersetzt durch D-059 | Sprint 4 (Branching-Modell)

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

### D-056 | verbindlich — Klausel 2 (Niederlagen-Bedingung) teilweise ersetzt durch D-077 | Sprint 7 (Closed-Core MS-1; Q-031/Q-038)

**Kontext:** Der Audit und Q-038 verlangen einen eindeutigen MVP-Zuschnitt. Der
alte Vollumfang war nicht dependency-closed; der 6/6-Recovery-Vorschlag ließ
Produkt-, Persistence- und Aetherium-Abhängigkeiten offen. Q-031 machte ein
generisches Fähigkeiten-/Status-System implizit zum Startblocker.

**Alternativen:** (a) historischer Vollumfang mit drei Fraktionen, Luft, T3,
Eliten, Neutralen und Zusatzmodi; (b) alter Recovery-Slice mit sechs Gebäuden
und sechs Einheiten je Fraktion; (c) symmetrischer Mirror-Demonstrator ohne
echte Fraktions- und Wirtschaftsidentität; (d) dependency-closed Scope mit
genau den für einen vollständigen Skirmish-Kern nötigen Regeln.

**Entscheidung:** (d) – **Closed-Core MS-1**:

1. Allianz und Legion; lokales Solo Mensch gegen KI; Glutrinne, Wüste, S,
   128×128 bei 1 m, ausschließlich klares Wetter.
2. Neun Gebäude-Rollen je Fraktion: HQ, Power, Refinery, Storage, Barracks,
   VehicleFactory, ResearchLab, Radar, DefensePlatform. Namen sind exakt aus
   [MVPContentManifest.md](MVPContentManifest.md) und den GDD-Tabellen zu
   übernehmen.
3. Acht Einheiten-Rollen je Fraktion: Builder, Harvester, BasicInfantry,
   AntiArmorInfantry, ScoutVehicle, LightTank, BattleTank, Artillery.
4. Start je Seite: fertiges HQ und fertige Raffinerie, ein Builder, zwei
   Harvester, 1.000 AE. Die Start-Raffinerie ist die einzige
   Voraussetzungsausnahme und erzeugt keinen zusätzlichen Harvester.
5. Fertigstellung des ResearchLab schaltet T2 direkt frei; keine
   Forschungs-Upgrades oder Forschungsqueue. DefensePlatform: MG auf T1,
   Rocket auf T2, kein Flak.
6. Kein generisches System für aktive Fähigkeiten, Status, Kanäle oder Auren;
   keine aktiven Toggles nötig. Fraktionsidentität bleibt waffen- und
   wirtschaftslokal: Allianz hochpreisig, präzise, Single-Target, 330-AE-
   Harvester; Legion günstig, schnell produziert, Salven/Splash,
   300-AE-Harvester.
7. Glutrinne enthält zwei Startfelder zu je 9.000 AE, zwei Naturals zu je
   9.000 AE und ein zentrales Feld mit 15.000 AE sowie zwei Routen. Keine
   Neutralen, Brücken, Wetter/Hazards oder Umgebungszerstörung außer
   Aetherium.
8. D-010 gilt vollständig: endliche Reserve, Nachwachsen aus der Reserve,
   Ausbreitung/Terrainfolge, permanenter Überernte-Schaden, lesbarer
   Zustand/Warnung, KI-Management und umkämpfte Expansion. Ziel sind
   20–35 Minuten; Artillerie und endliche Felder beenden das Match.
9. Produktminimum: Pause; zehn manuelle Slots; rotierendes Quicksave A/B;
   drei Autosaves alle fünf Minuten; Load/Backup-Recovery; normales UI-only-
   Match; Rebinding; UI-Skalierung 80–150 %; Farb-/Formredundanz; reduzierte
   Shake-/Flash-Optionen; Client-Command-Feedback ≤100 ms.
10. Niederlage tritt ein, sobald ein Slot keine lebende Einheit und kein
    lebendes Gebäude einschließlich Baustellen mehr besitzt. Werden beide
    Seiten im selben Tick eliminiert, endet das Match als
    `Draw.MutualAnnihilation`; nach 27.000 Ticks ohne Elimination als
    `Draw.TimeLimit`. Besitzt ein Slot 600 Ticks ununterbrochen kein Gebäude
    und höchstens drei Einheiten, werden diese bis zum Ende der Bedingung für
    den Gegner sichtbar und zielbar. MS-1 besitzt weder automatische
    KI-Aufgabe noch einen Spieler-Surrender-Command.

**Begründung:** Nur (d) beweist den eigentlichen Nova-Loop einschließlich
strategischer Aetherium-Feldpflege, Fraktionsunterschied und Produkt-
Wiederaufnahme, ohne Post-MVP-Systeme als Vorbedingungen einzuschleusen.

**Konsequenzen:** Zurückgestellt sind Evolvierte, Luft/Flak, Mauern, T3,
Eliten, Superwaffen, Drohnen, generische Fähigkeiten/Status, Capture,
Neutrale/Brücken, Wetter/Hazards, weitere Karten/Biome, Commander/Doktrinen/
Voice-over, Kampagne, Online/Koop/FFA/Survival/PvP/Ranked, Telemetrie,
Steam/Cloud und finale Art/Audio. D-008, D-009, D-014, D-015, D-016, D-022, D-023,
D-026, D-030 und D-031 werden **nur für MS-1** übersteuert; ihr Vollspiel-
Zielbild bleibt Post-MVP. Q-031 und Q-038 sind geschlossen.

### D-057 | verbindlich | Sprint 7 (kanonische Deterministik und Persistence; Q-039)

**Kontext:** D-033 erlaubte Float im MVP, während MS-0
plattformübergreifende Deterministik verlangte. D-045 definierte eine
technisch ungültige relative Hash-Toleranz. State-, Command-, Save- und
Replay-Verträge waren nicht bytegenau geschlossen.

**Alternativen:** (a) Float bis Beta beibehalten; (b) Fixed-Point ab G1;
(c) Float-Werte vor Hash/Serialisierung quantisieren; (d) parallele
Float-/Fixed-Point-Pfade pflegen.

**Entscheidung:** (b) – der Vertrag aus
[../tech/SimulationCore.md](../tech/SimulationCore.md) und
[../tech/Commands.md](../tech/Commands.md) gilt ab G1:

1. **Zahlen:** `SimFixed` signed Q16.16 auf `int32` mit
   `OneRaw=65536` und Wertebereich `[-32768, 32767.9999847412109375]`,
   `int64`-Zwischenprodukte, nearest ties-to-even, Welt→Grid floor, geprüfte
   deterministische Faults; kein Saturieren oder Wrap außer `SimAngle`
   `uint16`. Autoritative `float`/`double`-/Unity-Mathematik ist verboten.
2. **Grundtypen:** 10 Hz, `Tick uint32`, Dauern in Ticks,
   `XorShift128PlusV1` mit zwei `uint64`-Wörtern, Player/Team `uint8`,
   `DefinitionId uint16` (`0` ungültig) sowie `EntityId uint32`: Bit 0–9
   enthalten Index 0–1.023, Bit 10–31 eine Generation 1–4.194.303;
   Rohwert 0 ist ungültig. Initialgeneration ist 1, freie Indizes werden in
   aufsteigender Reihenfolge vergeben, Generationsüberlauf ist ein
   deterministischer Fault. Allocator/Free-List/Generationen werden
   serialisiert; Hashes sind `uint64`.
3. **Ingress:** UI und KI erzeugen nur `CommandIntent`.
   `MatchSession`/`CommandIngress` bindet Player, vergibt je Spieler
   `Sequence` und setzt `TargetTick` über den fingerprinted
   `InputDelayTicks` (MS-1 exakt 1). Sequenzen beginnen bei 1; 0 und
   `uint32`-Überlauf sind ungültig. `LocalLoopbackTransport` nutzt
   denselben Pfad; der Kernel akzeptiert nur versiegelte `CommandBatch`.
4. **Envelope, Little Endian:** `RecordLength u16`, `EnqueueTick u32`,
   `TargetTick u32`, `PlayerSlot u8`, `Sequence u32`, `CommandKind u16`,
   `PayloadVersion u8`, `PayloadLength u16`, Payload. Keine Floats, Strings,
   GUIDs oder Dictionaries im Payload. Der Header ist 20 Bytes,
   `MaxRecordBytes=4096`, `MaxPayloadBytes=4076`,
   `MaxBatchRecordsPerTick=256`, `MaxPendingRecords=1024` und
   `MaxEntityIdsPerCommand=100`.
5. **Ordnung/Fehler:** Sortierung nach
   `(TargetTick, PlayerSlot, Sequence)`. Byteidentische Wiederholung wird
   einmal angenommen, inhaltlicher Konflikt abgelehnt. Strukturell
   Ungültiges erreicht den Strom nicht; zustandsabhängige Fehler bleiben mit
   deterministischem `CommandResult` und ohne Mutation im Replay.
   Schema v1 testet jedes aktivierte Command sowie invalid/unknown,
   reordered, duplicate und backpressure.
6. **State-Inventar:** Tick/Fingerprint/PRNG, Allocatoren,
   Match/Player/Team/Entity, Orders, Movement, Combat, Projectiles, Economy,
   Energy, Aetherium, Construction, Production, T2, FoW, Environment,
   ausstehende Batches, Sequence/Dedupe sowie Path-/Deferred-Queues und jede
   andere zukunftsrelevante Information. KI ist ein versionierter
   Session-Sidecar für Save/Fortsetzung, keine `Nova.Simulation`-
   Abhängigkeit. Abgeleitete Caches dürfen nur bei bewiesen identischem
   Rebuild fehlen.
7. **Hashes:** State-, Definitions-, File- und Replay-Chain-Domänen verwenden
   XXH64 Seed 0 mit den ASCII-Präfixbytes `NOVA_STATE_V1\0`,
   `NOVA_DEFINITIONS_V1\0`, `NOVA_FILE_V1\0` und
   `NOVA_REPLAY_CHAIN_V1\0`.
8. **Fingerprint:** alle Schemata, `NumericModelId=Q16_16_V1`, 10 Hz, PRNG,
   `RulesHash64`, `DefinitionsHash64`, `MapHash64`, MatchConfig/Slots/Seed
   und initialer State.
9. **Persistence:** Snapshot-Roundtrip ist byteidentisch; Restore und
   frischer Host setzen mindestens 1.000 Ticks einschließlich gequeuter
   Commands identisch fort; jeder State-Block besitzt
   Hashsensitivitätstests. Replay zeichnet alle akzeptierten Human-/KI-
   Commands auf und instanziiert KI beim Playback nicht erneut; Shadow-
   Validierung ist rein diagnostisch.
10. **Kompatibilität:** einmaliger Pre-G1-Reset; Prototyp-Saves, -Replays,
    -Pakete und -Fixtures sind unsupported. Kanonische Schemata beginnen
    1.0. Nach G1: Replay nur bei exaktem Fingerprint; Save-Migration nur
    explizit und getestet.
11. **Parität:** SimRunner und Unity wahren Core/Simulation/AI-Grenzen und
    kompilieren dieselben Quellen/Defines. Windows x64 und macOS arm64
    müssen über 10.000 Ticks exakte Hashes und finale Bytes liefern. Der
    Managed-Pfad shippt; Burst bleibt für MS-1 aus, bis exakte Feld-/Hash-/
    Byteparität bewiesen ist.

**Begründung:** Fixed-Point ab G1 entfernt eine spätere Kernmigration und macht
Snapshots, Replays, Savegames und Cross-Plattform-Evidenz zu einem einzigen
prüfbaren Vertrag. Quantisierung und Doppelpfade verschieben oder verdoppeln
das Risiko.

**Konsequenzen:** Q-039 ist geschlossen. Die Float-Zeitpunkt-Klausel aus D-033
und die Toleranz-Hash-Klausel aus D-045 sind ersetzt. Numerische Toleranzen
gelten nur für nicht autoritative Diagnostik, nie als Hashdistanz.

**Fortschreibung D-089:** „MS-1 exakt 1“ in Klausel 3 bezeichnet den
kanonischen lokalen Defaultwert, nicht eine API-Bereichssperre.
`MatchConfig`/Loopback akzeptieren 1 bis 60; das Netzprofil verwendet
standardmäßig 3 aus demselben Bereich. Der tatsächlich gewählte Wert bleibt
fingerprinted und während einer Session fest.

### D-058 | verbindlich | Sprint 7 (MVP-Kapazität, Cache und FoW; Q-032)

**Kontext:** Aktive Verträge mischten sechs und acht Spieler, 100, 500 und 600
Einheiten, widersprüchliche Snapshotgrößen sowie LRU- und RefCount-Eviction.
FoW hatte weder verbindlichen Takt noch einen gemeinsamen Konsumentenvertrag.

**Alternativen:** (a) volle 6-/8-Spieler-Budgets jetzt; (b) dynamische,
unbegrenzte Strukturen; (c) feste MS-1-Kappen mit reserviertem
Kompatibilitätsschema.

**Entscheidung:** (c):

- Das Format reserviert acht Slots, MS-1 aktiviert exakt zwei.
- Grid 128×128 bei 1 m.
- Produktionseinheitenlimit 100 gesamt; 500-Agenten-Fixtures sind
  synthetische Last, kein Content-Versprechen.
- Entity Store 1.024.
- unkomprimiertes Snapshotziel ≤4 MiB; Parser-Hardcap 64 MiB.
- Flow-Field-Cache ≤32 Einträge und ≤8 MiB. Referenzierte Einträge werden
  nie eviktiert; unter RefCount-null-Einträgen entscheidet deterministische
  LRU. Zukunftsrelevante Request-/Eviction-Metadaten werden serialisiert.
- FoW ist autoritativ pro Team, drei Zustände, Recompute auf jedem zweiten
  Sim-Tick (5 Hz) nach Movement und vor Combat. Dieselbe committed Sicht
  steuert Combat-Legalität, KI, Player-Snapshot und Rendering.
- MS-1 nutzt nur Radien: keine Hindernisse, Höhe, Wetter, Tarnung oder
  Detektion. Radar aktiviert Minimap/Signatur-Pings, aber kein Targeting.
  Hidden-World-Metamorphic-Tests sind Pflicht.

**Begründung:** Feste Kappen machen Speicher, Parser und Spike-Szenarien
beweisbar, ohne das Dateiformat für spätere Slots neu zu brechen.

**Konsequenzen:** Q-032 ist geschlossen; D-048 bleibt als Post-MVP-Historie,
gilt nicht für MS-1. [../tech/MemoryBudget.md](../tech/MemoryBudget.md),
[../tech/Pathfinding.md](../tech/Pathfinding.md) und
[../tech/FogOfWar.md](../tech/FogOfWar.md) sind führend angeglichen.

### D-059 | verbindlich | Sprint 7 (Branch-Modell; ersetzt D-050)

**Kontext:** D-050 wollte mit Sprint 7 einen dauerhaften Integrationsbranch
einführen, während Repository-Schutz, Beiträge und tatsächliche Arbeit PRs
direkt nach `main` vorsehen.

**Alternativen:** (a) GitFlow mit dauerhaftem Integrationsbranch; (b)
geschütztes `main` plus kurze Topic-Branches; (c) direkte Trunk-Pushes; (d)
langfristiger Recovery-Branch.

**Entscheidung:** (b). `main` bleibt PR-only; es gibt keinen dauerhaften
Integrationsbranch. Erlaubte kurze Präfixe sind `feat/`, `fix/`, `docs/`,
`chore/`, `refactor/` und `codex/`. Merge erfolgt per Squash bei linearer
History; Force-Push auf geteilte Branches ist verboten. Pflichtchecks:
`docs-check` und, sobald G0 ihn real erzeugt, `quality-gate`.

Unabhängiges read-only Review ersetzt in Solo-/KI-Arbeit die nicht mögliche
Autoren-Selbstfreigabe. Sobald mindestens zwei aktive menschliche Maintainer
existieren, ist eine zweite menschliche Freigabe Pflicht. Agenten committen
oder pushen nur nach einer **expliziten Anfrage pro Aktion**.

**Begründung:** Kurze Branches minimieren Drift und passen zur aktuellen
Maintainerzahl, ohne Schutz oder unabhängige Prüfung zu schwächen.

**Konsequenzen:** D-050 ist ersetzt. AGENTS.md, CONTRIBUTING.md,
[../tech/Deployment.md](../tech/Deployment.md) und
[../tech/Testing.md](../tech/Testing.md) verwenden ausschließlich dieses
Modell.

### D-060 | verbindlich | Sprint 7 (Engine-Pin; ersetzt D-006)

**Kontext:** Das Repository ist bereits mit Unity `6000.5.4f1` importiert und
gespeichert; ein Papier-Pin auf die ältere Linie würde einen riskanten
Downgrade verlangen.

**Alternativen:** (a) Downgrade auf `6000.3` LTS; (b) aktuellen exakten Editor
`6000.5.4f1` pinnen; (c) automatisch der jeweils neuesten Update-Version
folgen.

**Entscheidung:** (b) – **Unity 6000.5.4f1, Revision `d550df8bd089`, URP**.
Automatische Editor-Upgrades sind verboten.

**Begründung:** Das Projekt liegt bereits in dieser Version vor. Unity
bezeichnet Update-Releases als produktionsreif und als bevorzugte Wahl für
neue beziehungsweise mittig im Zyklus befindliche Projekte; der Pin vermeidet
einen unbewiesenen Downgrade. Offizielle Quellen:
[Unity 6 Releases & Support](https://unity.com/releases/unity-6/support) und
[What’s new in 6000.5.4f1](https://unity.com/releases/editor/whats-new/6000.5.4f1).

**Konsequenzen:** D-006 ist ersetzt. Re-Evaluierung nur mit neuer D-ID nach G5
oder bei einem belegten Engine-Blocker. README, Wiki, AGENTS und aktive
Tech-Verträge verwenden den exakten Pin.

### D-061 | verbindlich; ergänzt durch D-062/D-063 | Sprint 7 (Acceptance, Performance und Evidence; Q-033/Q-034)

**Kontext:** Alte Gates akzeptierten Struktur statt Laufzeit, vermischten
100-Einheiten-MVP und 500-Agenten-Spike, ließen Review- und
Evidence-Fälschungslücken und enthielten tote TDD-Verweise.

**Alternativen:** (a) alte Gates fortführen; (b) nur Berichte ohne harte
Schwellen erzeugen; (c) ausführbare Gates mit unveränderlicher Evidenz und
getrennten Produkt-/Skalierungslasten.

**Entscheidung:** (c):

1. Reihenfolge `G0 → G1 einschließlich V1–V5a → G2 → G3 → G4 → G5`.
   V5a ist G2-Eintritt: repräsentative SpatialHash-, FoW-Filter- und
   Command-Verarbeitung vor Combat plus 500-Agenten-Probes. V5b wiederholt
   mit realem Combat/KI in G3. MS-0 = G0+G1+V1–V5a; MS-1 erst nach G5.
2. 100 Einheiten sind Full-Content-MVP; 500 Agenten sind synthetische
   Architekturreserve. `MVP_FULL_100`: Sim P95≤8/P99≤12 ms; Path≤4/6;
   FoW≤1/1,5; Rest≤3/4,5; Sim-GC 0 B. CPU- und GPU-Frame jeweils
   P95≤16,6/P99≤24,9; Rendering-CPU≤4, Animation≤1,5, GPU-Render≤8,
   UI≤1 ms.
3. D-052-Windows-Methode: Standalone IL2CPP Development, Managed/Burst aus,
   2560×1440 `NovaReference`, VSync/Deep Profiling aus, fixes Replay, 30 s
   Warmup +120 s Messung ×3, keine Ausreißerentfernung, Rohsamples. 500er
   Fixtures: kein Crash/unbeschränktes Wachstum; Path P95≤4 und Pre-Combat-
   Rest P95≤3 bleiben Architekturgate, Full-Content-500 ist Diagnose. Mac M2
   funktional bei 1080p Medium: P95≤33,3/P99≤50 ms.
4. Evidence folgt
   [`GateEvidence.schema.json`](../../quality/schemas/GateEvidence.schema.json)
   und liegt append-only unter
   `quality/evidence/G<N>/<subjectSha>/<attempt>/GateEvidence.json`.
   Exakter Commit/Tree, `dirty=false`, Vorgängerevidenz, SHA-256 der rohen
   Content-/Scenario-Dateibytes, Toolchains, Umgebungen,
   Befehle/Checks/Coverage, Rohmetriken/
   Artefakthashes, CI, Reviewer, Kriterienmap und Urteil sind Pflicht.
   Skip/Cancel/fehlendes Pflichtresultat = fail; relevante Änderungen machen
   Evidenz stale. Reviewer ≠ Writer und reproduziert mindestens einen Clean-
   Clone-Befehl. Keine Evidence-Platzhalter.
5. Jeder PR führt das aggregierte `quality-gate`; Docs-only wird explizit
   klassifiziert, nie übersprungen. Pflicht: Tests, Coverage, Architektur,
   Golden und vier Headless-Matches. Nightly: zwei geordnete
   Fraktionscluster ×20 gespiegelt =40; Weekly 2×200=400; G5 am selben SHA
   drei Nightly-Matrizen=120. Ein Match gilt nur mit Hashes, monotonen Ticks,
   gültigem Ergebnis, Core-Action-Trace und Checkpoint-Kette; Fehler bleiben
   im Nenner.
6. G1-Coverage: Simulation ≥80 %; Command/PRNG/Serializer/Hash/Replay je
   ≥90 %; Command-Inventar 100 %.
7. G0: exakter Engine/.NET-/Paket-Pin, versionierte getrennte SimRunner-
   Projekte, asmdef-/Architekturcheck, saubere Win/Mac-Builds,
   .NET+EditMode, Architektur- und Evidence-Validator-Negative-Control,
   keine generierten Binärdateien getrackt.
   G1: Fixed-Point/Commands/State/Snapshot/Replay/Cross-Plattform. G2:
   Player-Kernloop via MatchSession, vollständiges Graybox-Aetherium, keine
   Direktmutation. G3: KI nur gefilterte Ansicht/kanonische Intents,
   Replay-/Save-Fortsetzung, FoW-Metamorphics, V5b. G4: exaktes
   Produktionsmanifest, Glutrinne, HUD/Settings/Pause/Save/Load/
   Accessibility/Provenienz/Usability. G5: eingefrorene Abnahme.
8. G5: zwei manuelle UI-only-Matches, eines je Fraktion; drei neue
   Task-Tester; Median 20–35 Minuten; jeder Fünf-Minuten-Autosave-Punkt von
   Minute 5–45; null P0/P1; keine gatekritische Quarantäne.

**Begründung:** Ausführbare, hashgebundene Nachweise trennen Requirements von
Erfolg und verhindern, dass synthetische Skalierung als Contentzusage oder
Dateianwesenheit als Gate ausgegeben wird.

**Konsequenzen:** [MVPRecoveryPlan.md](MVPRecoveryPlan.md),
[../tech/Testing.md](../tech/Testing.md),
[../tech/PerformanceBudget.md](../tech/PerformanceBudget.md) und die drei
Quality-Verträge sind führend. Die substanziellen TDDs
[../tech/SimulationCore.md](../tech/SimulationCore.md),
[../tech/Commands.md](../tech/Commands.md),
[../tech/FogOfWar.md](../tech/FogOfWar.md) und
[../tech/CameraSystem.md](../tech/CameraSystem.md) schließen zusammen mit
bereinigten Links Q-033 und Q-034.

### D-062 | teilweise ersetzt durch D-063 | Sprint 7 (Evidence-Semantik und Gate-Kette)

**Kontext:** Ein Gegenbeispiel zeigte, dass bloße Szenarioreferenzen trotz
überschrittener Performancegrenzen als `pass` gelten konnten. Content- und
Szenariohashes wurden aus dem aktuellen Checkout statt aus dem deklarierten
Subject-Commit berechnet; außerdem war ein isolierter G5-Pass ohne
Vorgängergates maschinell möglich.

**Alternativen:** (a) Reviewer prüfen diese Beziehungen ausschließlich
manuell; (b) ein separater, späterer Gate-Aggregator wertet frei benannte
Ergebnisdateien aus; (c) der bestehende Evidence-Vertrag bindet Szenarien,
Rohmetriken, Subject-Blobs und die unmittelbare Gate-Kette vollständig und
selbstprüfend.

**Entscheidung:** (c):

1. `gateUsage` und `gateProfiles.requiredScenarioIds` müssen für G0–G5 exakt
   übereinstimmen. `MVP_FULL_100` und `MAC_M2_FUNCTIONAL` sind
   G5-Abnahmeszenarien; G4 prüft Inhalt und Produktpfad ohne deren
   Performancefreigabe.
2. Ein Kriterium, das `scenario:<ID>` referenziert, bindet alle
   Pflichtmetriken dieses Szenarios und mindestens einen ausgeführten
   Command. Rohmetriken heißen
   `scenario.<ID>.<metric>`; boolesche Assertions heißen
   `scenario.<ID>.assertion.<assertion>`, verwenden `unit=bool` und exakt
   `[1]`. Das zugehörige Rohartefakt ist striktes JSON mit exakt
   `name`, `unit` und `samples`.
3. P95/P99 werden ohne Interpolation per Nearest-Rank über alle unveränderten
   Samples berechnet; Maxima, Minima und Gleichheit werden direkt aus diesen
   Samples geprüft. Kein abgeleiteter Bericht darf Rohsamples ersetzen.
4. Manifest- und Szenario-SHA-256 werden über die Git-Blobs
   `<subjectSha>:<path>` berechnet, nicht über den aktuellen Working Tree.
5. Jede Evidence ab G1 referenziert genau die unmittelbar vorherige
   Gate-Evidence samt SHA-256. Diese muss rekursiv semantikvalide `pass`
   liefern und exakt denselben Subject-Commit und Tree belegen. G0 besitzt
   keine Vorgängergate-Referenz.
6. Das Evidence-Schema ist `1.1.0`. Da noch keine reale Gate-Evidence
   existiert, ist keine Migration und keine historische Umschreibung nötig.

**Begründung:** Nur (c) macht Schwellen, Vertragsstand und Gate-Reihenfolge
aus dem Evidence-Objekt selbst reproduzierbar. Die Same-Subject-Kette ist
strenger als eine Änderungsheuristik, aber eindeutig und vor G5 bezahlbar.

**Konsequenzen:** D-061 bleibt in Scope, Reihenfolge und Schwellen gültig;
D-062 schließt seine maschinellen Durchsetzungslücken.
[`GateEvidence.schema.json`](../../quality/schemas/GateEvidence.schema.json),
[`mvp-v1.json`](../../quality/scenarios/mvp-v1.json) und
[`validate_gate_evidence.py`](../../quality/scripts/validate_gate_evidence.py)
sind gemeinsam führend.

**Revision:** D-063 ersetzt ausschließlich Schema `1.1.0`, flache
Performance-Samples und die selbstdeklarierte Command-/CI-/Reviewer-
Vertrauenskette. Subject-Blob- und Same-Subject-Gate-Regeln bleiben gültig.

### D-063 | teilweise ersetzt durch D-064 | Sprint 7 (Evidence-Authentizität und Messmethode)

**Kontext:** Drei ausführbare Gegenbeispiele bestanden Schema 1.1 und den
Semantikvalidator: Ein `true`-No-op konnte alle G0-Kriterien mit erfundenen
Counts/CI-/Reviewer-Feldern belegen; Performancegrenzen akzeptierten eine
negative Einzelprobe mit beliebiger Einheit statt `30 s + 120 s ×3`; und ein
schemawidriger G0-Vorgänger bestand innerhalb einer G1-Kette, weil rekursiv
nur Semantik geprüft wurde.

**Alternativen:** (a) Schema 1.1 beibehalten und die drei Punkte ausschließlich
dem Reviewer überlassen; (b) maschinenlesbare Gate-Evidence streichen und
Gates nur per Bericht freigeben; (c) inkompatibles Schema 1.2 mit gepinntem
Draft-2020-12-Validator, kanonischen kriterienspezifischen Checks,
artefaktgebundenen Ausgaben, geschütztem externem Trust-Kontext und getrennten
Performance-Läufen.

**Entscheidung:** (c):

1. Evidence und Szenariovertrag wechseln auf `1.2.0`. Da
   `quality/evidence/` noch leer ist, gibt es keine Migration; ein vor Merge
   doch erzeugter 1.1-Versuch wird nicht umgeschrieben, sondern neu
   ausgeführt.
2. Gepinntes Node/Ajv Draft 2020-12 plus `ajv-formats` validiert das aktuelle
   Dokument und jeden Vorgänger fail-closed. Schema und Python-Validator sind
   zusätzlich als SHA-256-gebundene Subject-Git-Blobs Teil der Evidence.
3. Jedes Gate-Kriterium benötigt genau einen gleichnamigen, kanonisch über
   `run_gate_check.py` aufgerufenen Implementation-Check. `stdout`, `stderr`
   und maschinenlesbares Check-Ergebnis liegen gehasht im aktuellen
   Attempt-Verzeichnis; ein `command:<id>` ohne `check:<criterionId>` genügt
   nicht. Der unabhängige Reviewer wiederholt mindestens einen solchen Check
   als separate Reviewer-Ausführung.
4. Ein lokales Evidence-Dokument kann Integrität prüfen, aber keinen
   `pass` autorisieren. Die öffentliche CLI verlangt dafür einen außerhalb
   des Repos erzeugten Trust-Kontext aus dem unveränderten, geschützten
   `quality-gate` auf `main`. Er bindet Evidence-Hash, Subject-Commit/-Tree,
   CI-Attestierung und Reviewer-Attestierung; ohne passenden
   GitHub-Actions-Kontext gilt `E_TRUST_CONTEXT`.
5. Jede Schwelle nennt eine exakte Einheit und nichtnegative Domäne.
   Performance-Metriken besitzen einen 30-s-Warmup und exakt drei getrennte
   120-s-Läufe mit mindestens einer Rohprobe pro Sekunde. P95/P99, Minimum,
   Maximum und Gleichheit müssen sowohl pro Lauf als auch über die
   unveränderte Konkatenation bestehen. `SCALE_500_PRECOMBAT` und die
   Mac-M2-Messung verwenden dieselbe Methode.
6. Die gepinnten Quality-Abhängigkeiten und ihre Lockdatei sind Teil des
   Repositorys. Der bestehende `docs-check` installiert sie und läuft auch
   bei Änderungen unter `quality/`.

**Begründung:** (a) lässt genau die reproduzierten False-Pass-Pfade offen;
(b) verliert die maschinelle Reproduzierbarkeit. (c) trennt überprüfbare
Integrität von externer Autorisierung und bindet jedes behauptete Ergebnis an
einen konkreten Check, dessen Rohartefakte und die festgelegte Messmethode.

**Konsequenzen:** G0 bleibt offen. Sprint 7 darf G0 implementieren, aber kein
Gate darf vor dem realen `run_gate_check.py` und einem geschützten
`quality-gate`-Trustpfad als bestanden gelten. D-064 sperrt nach dem
Angriffstest zusätzlich jede Autorisierung durch Schema 1.2 und legt
Schema 1.3 als Ziel fest. D-061-Scope/-Schwellen und
D-062-Subject-/Gate-Kette bleiben ansonsten unverändert.

### D-064 | verbindlich | Sprint 7 (Trusted-Gate-Bootstrap)

**Kontext:** Der unabhängige Angriffstest gegen D-063 fand drei verbleibende
False-Pass-Pfade: Ein Subject konnte sein eigenes Schema beziehungsweise den
Ajv-Wrapper abschwächen; ein autorisiertes späteres Gate akzeptierte eine nur
lokal erzeugte Vorgängergate-Evidence; und Performance-Messungen waren nicht
an die vorgeschriebene Umgebung gebunden. Schema 1.2 darf deshalb nicht selbst
die Autorität erzeugen, mit der es sich als bestanden erklärt.

**Alternativen:** (a) Schema 1.2 trotz der Befunde autorisieren und die Lücken
später schließen; (b) maschinenlesbare Evidence wieder entfernen und allein
auf PR-Review vertrauen; (c) Autorisierung bis zu einem zweistufigen,
subject-unabhängigen Bootstrap sperren und danach Schema 1.3 mit
Trusted-Tool-, Ketten- und Umgebungsbindung verwenden.

**Entscheidung:** (c):

1. Schema 1.2 bleibt ein Integritätsprüfer, darf aber keinen Gate-Pass
   autorisieren. Die CLI endet bei jedem Pass-Versuch zusätzlich mit
   `E_AUTHORIZATION_BOOTSTRAP`, bis G0 den folgenden Vertrag vollständig
   implementiert.
2. Der geschützte Authorize-Job führt Python-Validator, Evidence-Schema,
   Ajv-Wrapper und `npm ci --ignore-scripts` ausschließlich aus einem
   separaten Trusted-Tool-Checkout aus, nie aus dem zu prüfenden Subject.
   Evidence und externer Trust-Kontext binden mindestens Manifest,
   Szenariovertrag, Schema, Python-Validator, Ajv-Wrapper, `package.json`,
   Lockdatei, Gate-Runner und Authorize-Workflow per Subject-/Trusted-
   Commit und SHA-256 sowie eine exakte Node-Version.
3. Eine Änderung an diesem Trust-Bundle ist eine **Bootstrap-Änderung**:
   Sie kann sich nicht selbst autorisieren, wird ohne Gate-Fortschritt per
   geschütztem PR gemergt und gilt erst für einen nachfolgenden sauberen
   Subject-Commit. Erst dieser Nachfolger darf G0-Evidence erzeugen.
4. Ein Trust-Kontext enthält die vollständige, geordnete
   `authorizedEvidence`-Kette von G0 bis zum aktuellen Gate. Jeder Eintrag
   bindet Gate, Pfad, Evidence-Hash, Subject-Commit/-Tree, CI-Run/-Job sowie
   CI- und Review-Attestierung. Der geschützte Job verifiziert jeden Eintrag
   gegen GitHub; fehlende, zusätzliche, vertauschte oder nur lokal erzeugte
   Vorgänger sind ungültig.
5. Schema 1.3 verlangt `environmentId` an Command und Performance-Messung.
   Beide müssen auf dieselbe deklarierte Umgebung zeigen. Windows-x64-
   Referenzmessung und Mac-M2-Funktionsmessung erhalten getrennte
   Methodenprofile; OS, Architektur, Hardware, Build, Managed/Burst,
   Auflösung, Quality-Profil, VSync, Deep Profiling und Replay werden exakt
   verglichen.
6. Fehlender Node/Ajv-Stack und ein hängender Schema-Subprozess enden
   kontrolliert und fail-closed. Negative Controls decken manipuliertes
   Subject-Schema, Ajv-Wrapper/Lockfile, unvollständige Autorisierungsketten
   und falsche beziehungsweise widersprüchliche Umgebungen ab.

**Begründung:** (a) würde reproduzierte Falschfreigaben akzeptieren; (b)
verliert die wegen der früheren Fertigmeldungen notwendige Reproduzierbarkeit.
(c) schafft einen kleinen, ausdrücklich zu implementierenden Root of Trust
und verhindert Selbstautorisierung, ohne Produktcode hinter weitere
Planungsarbeit zu stellen.

**Konsequenzen:** Sprint 7 ist weiterhin gestartet, aber seine erste
Coding-Arbeit ist **G0-A Trusted-Gate-Bootstrap**. Danach folgt G0-B
Plattform-/Build-Reproduzierbarkeit. Bis G0-A gemergt und an einem
nachfolgenden sauberen Subject bewiesen ist, sind G0–G5 zwingend offen.
D-064 ergänzt D-063 und ersetzt dessen Autorisierungsanspruch für Schema 1.2;
alle übrigen D-063-Prüfungen bleiben verbindliche Vorstufe.

### D-065 | ersetzt durch D-066 | Sprint 7 (Authorize-Run-Bindung der Evidence-Kette)

**Kontext:** Das unabhängige Re-Review von G0-A fand (Befund N-1), dass die
GitHub-Verifikation der `authorizedEvidence`-Kette replay- und reuse-anfällig
war: Ein `pull_request`-Run mit grünem `integrity`-Job bestand die Prüfung,
obwohl er nie etwas autorisiert hatte, und derselbe Authorize-Run war für
beliebig viele Gates wiederverwendbar.

**Alternativen:** (a) Die Dokumentation ehrlich abschwächen („nur ein
grüner Lauf nötig") und den Anker allein auf das Environment-Approval legen;
(b) den Evidence-Hash als Run-Artefakt an den Run binden (Artifact-
Upload/-Download im geschützten Job — stärker, aber komplexer und mit
eigener Angriffsfläche über Artifact-Retention); (c) Event-, Job- und
Eindeutigkeits-Bindung der Ketteneinträge bei der GitHub-Verifikation.

**Entscheidung:** (c):

1. Jeder Ketteneintrag muss einen Run mit `event == workflow_dispatch` des
   Workflows `.github/workflows/quality-gate.yml` belegen; `pull_request`-
   oder andere Runs zählen nie als Autorisierung.
2. Der verifizierte Job muss der Authorize-Job sein: Name exakt
   `gate-evidence-authorize` mit `conclusion == success`. Die Evidence
   (`ci.jobName`) ist per Schema-Konstante auf denselben Wert festgelegt;
   Anzeigename und Job-ID des Workflow-Jobs sind identisch.
3. Run-IDs sind über die gesamte `authorizedEvidence`-Kette eindeutig; jedes
   Gate benötigt seinen eigenen geschützten Authorize-Run.
4. `head_sha == subjectCommitSha` des Eintrags bleibt bestehen.

Alle Prädikate sind fail-closed; fehlendes `gh`-Tool oder Token blockiert
die Kontexterzeugung. Das ehrlich dokumentierte Restrisiko: Die
API-Verifikation belegt „ein echter geschützter Authorize-Lauf auf diesem
Subject hat stattgefunden"; die Bindung des aktuellen Laufs an die exakten
Evidence-Bytes läuft über `NOVA_TRUST_CONTEXT_SHA256`; verbleibender Anker
ist die GitHub-Environment-Protection. Die Review-Attestierung bleibt
hash-gebunden ohne API-Verifikation.

**Begründung:** (a) würde den reproduzierten Replay-/Reuse-Pfad als
akzeptables Risiko festschreiben; (b) schließt ihn zwar vollständig,
fügt aber neue Angriffsfläche (Artifact-Retention/-Substitution) und
Komplexität im geschützten Job hinzu. (c) schließt den Pfad mit bereits
vorhandenen API-Feldern und hält den geschützten Job minimal.

**Konsequenzen:** Durch D-066 ersetzt. Die Event-/Job-Prüfungen bleiben als
Anforderungen für bereits abgeschlossene Vorgänger-Receipts erhalten, dürfen
aber nicht auf den noch laufenden aktuellen Authorize-Job angewendet werden.

### D-066 | verbindlich | Sprint 7 (Fail-Closed-Foundation und zweiphasige Autorisierung)

**Kontext:** Zwei unabhängige Merge-Reviews fanden im G0-A-Entwurf einen
logischen Kreis: Der laufende `gate-evidence-authorize`-Job sollte bereits
`conclusion=success` belegen, bevor er die aktuelle Evidence validierte.
Zugleich setzte der Entwurf Subject-Commit, Evidence-Carrier-Commit und
Trusted-Tool-Commit gleich. Eine eingecheckte Evidence kann weder ihre eigene
zukünftige Carrier-SHA noch die IDs eines später gestarteten Authorize-Runs
enthalten. Der erste echte G0-Lauf wäre deshalb unmöglich gewesen, obwohl die
Offline-Selbsttests grün waren.

**Alternativen:** (a) Den zirkulären Entwurf trotz des Befunds mergen und auf
einen späteren Laufzeitfix hoffen; (b) die technischen Bindungen entfernen
und Gate-Pässe allein per manuellem Review erklären; (c) nur die
Integritätsgrundlage fail-closed mergen, den unmöglichen Authorize-Pfad
entfernen und die Autorisierung als zweiphasigen, append-only Receipt-Vertrag
mit getrennten Identitäten neu aufbauen.

**Entscheidung:** (c):

1. Der aktuelle `quality-gate` führt ausschließlich den PR-Job `integrity`
   aus. Es gibt bis zur Receipt-Implementierung keinen
   `workflow_dispatch`-Authorizer und keinen Trust-Kontext-Generator.
   `verdict=pass` endet unabhängig von übergebenen Trust-Argumenten mit
   `E_AUTHORIZATION_BOOTSTRAP`.
2. GateEvidence-Schema 1.3 bleibt eine unveröffentlichte
   Integritätsvorstufe. `ci` beschreibt den Evidence-erzeugenden
   beziehungsweise prüfenden CI-Job und darf nicht
   `gate-evidence-authorize` bezeichnen. Der spätere Autorisierer ist eine
   getrennte Identität.
3. Der Folgebaustein G0-A2 trennt mindestens
   `subjectCommitSha`, `evidenceCarrierCommitSha` und
   `trustedToolCommitSha`. Sein Autorisierungsvertrag startet ohne Migration
   vorhandener Artefakte mit GateEvidence 1.4.0 und Trust-Kontext 3.0.0;
   ältere Pass-Artefakte bleiben fail-closed. Autoritative Szenarioprofile
   und Schwellen werden aus dem Trusted-Tool-Stand geladen, nie aus einem
   vom Subject änderbaren Vertrag.
4. Ein erfolgreicher geschützter Lauf erzeugt
   `GateAuthorization.json` als hashgebundenen Kandidaten neben der
   unveränderten `GateEvidence.json`. Das Receipt bindet Gate, Subject-
   Commit/-Tree, Evidence-Carrier, Evidence-Pfad/-Hash, Trusted-Tool-Commit,
   Repository, Workflow sowie Run-/Attempt-/Job-ID. Es wird als Artefakt
   transportiert und nach erfolgreichem Lauf unverändert per kleinem
   Folge-PR append-only versioniert.
5. Der aktuelle Lauf wird aus seinem geschützten Runtime-Kontext gebunden,
   aber nie gegen seine noch unmögliche eigene Erfolgs-Conclusion geprüft.
   Erst spätere Gates akzeptieren frühere Receipts und verlangen per GitHub-
   API den exakten `workflow_dispatch`-Run/-Attempt, Workflow, Gate,
   Evidence-Hash und erfolgreichen Authorize-Job. Run-IDs sind über die
   Kette eindeutig.
6. Alle fremden GitHub Actions im Integrity-Pfad werden auf vollständige
   Commit-SHAs gepinnt. Das geschützte Environment wird erst vor dem ersten
   realen Authorize-Lauf als Root of Trust benötigt und darf dessen fehlende
   Implementierung nicht vortäuschen.

**Begründung:** (a) würde eine nachweislich unerreichbare Sicherheitszusage
veröffentlichen. (b) würde die reproduzierten Falschfreigabe-Pfade wieder
öffnen. (c) lässt die bereits nützlichen Schema-, Semantik-, Topologie- und
Runner-Prüfungen nutzbar, hält jeden Gate-Pass technisch gesperrt und schafft
einen implementierbaren Übergang von laufender Validierung zu dauerhaft
prüfbarer Autorisierung.

**Konsequenzen:** G0-A wird in G0-A1 (Integritätsgrundlage; dieser PR) und
G0-A2 (zweiphasiger Receipt-Authorizer; Folge-PR) geteilt. G0-A insgesamt,
G0-B, G0 und alle folgenden Gates bleiben offen. Plattformarbeit darf
parallel vorbereitet werden, aber keinen Gate-Status beanspruchen, bevor
G0-A2 gemergt und an einem späteren sauberen Subject bewiesen ist. D-066
ersetzt D-065 und präzisiert D-064; es gibt weiterhin keine reale
Gate-Evidence und keinen Game-Release.

---

### D-067 | gegenstandslos durch D-076 | Sprint 7 (Graybox-Spur ohne Gate-Autorität)

**Status:** **gegenstandslos durch D-076**
(2026-08-06). Nie ratifiziert, nie in Kraft getreten — und jetzt nicht mehr
nötig: D-067 war die Ausnahme von Regeln, die es seit D-076 nicht mehr gibt. Die
Graybox-Spur braucht keine Sondergenehmigung, weil unter Governance-Tier 1 weder
die Gate-Kette blockiert noch pro Änderung Wiki-Index, Root-README und
Änderungsverläufe nachgezogen werden müssen.

Der Eintrag bleibt vollständig stehen (keine stillen Umschreibungen). Praktische
Folgen des Wegfalls:

- Der [ScopeLedger](ScopeLedger.md) **bleibt** — nicht mehr als Registerpflicht
  einer Ausnahme, sondern als ehrliche Lückenliste gegen `mvp-v1.json`. Die
  Spalte „D-ID-Klausel" ist damit gegenstandslos und kann bei der nächsten
  Berührung des Dokuments entfallen.
- Der [GrayboxLog](GrayboxLog.md) ist **nicht mehr pflichtig**. Er bleibt als
  historisches Sitzungsprotokoll GB-001 bis GB-004 erhalten; neue Sitzungen
  müssen dort nichts mehr eintragen. Was bleibt, ist der Playtest-Nachweis aus
  [../../GOVERNANCE.md](../../GOVERNANCE.md) — der darf hier landen, muss aber nicht.
- Die Verfallsklausel K5 läuft ins Leere und wird nicht mehr überwacht.

Der ursprüngliche Entwurf steht unverändert darunter.

**Kontext:** Der Simulationskern ist headless verifiziert, war aber unsichtbar
und unbedienbar: `Bootstrap.unity` enthielt nur Kamera und Licht, im Repo
existierte keine Zeile Eingabecode. Eine Graybox-Spur hat in einer Sitzung
Präsentations- und Bedienschicht ergänzt (Kamerarig, Szenengenerator-Wiring,
Auswahl/Befehle, Debug-HUD, Einheitenproxies) und erzeugt damit Artefakte, die
wie Gate-Fortschritt aussehen — eine spielbare Szene und zwei Player-Builds —
obwohl [MVPRecoveryPlan.md](MVPRecoveryPlan.md) §5 Debug-UI, Inspector-
Manipulation und direkte State-Aufrufe für G2 ausdrücklich ausschließt.
Gleichzeitig kollidiert die Sitzungsgeschwindigkeit mit der
Dokumentations-DoD aus AGENTS.md §8: Sechs parallel arbeitende Agenten können
nicht pro Datei Wiki-Index, Root-README und Änderungsverläufe serialisiert
nachziehen, ohne die Arbeit zu blockieren. Ohne explizite Regel endet das
entweder im Stillstand oder in genau der undokumentierten Statusbehauptung,
die D-055 zurückgenommen hat.

**Alternativen:** (a) Keine eigene Spur — die Graybox-Arbeit läuft vollständig
unter Sprint-Ritual (§7) und DoD (§8), jede Änderung zieht Index, Root-README,
Risikoanalyse und Änderungsverläufe sofort nach; (b) Fast Lane ohne
Registrierung — die Graybox wird als Prototyp deklariert, von den
AGENTS.md-Regeln ausgenommen und später oder gar nicht dokumentiert;
(c) benannte, zeitlich befristete Spur ohne Gate-Autorität mit eingefrorenem
Schreibumfang, registrierter Dokumentationsschuld, Eskalationsregel und
hartem Verfall.

**Entscheidung:** *Vorschlag — noch nicht in Kraft.* (c), mit fünf Klauseln:

1. **Keine Gate-Autorität (K1).** Kein Artefakt dieser Spur — Szene, Player-
   Build, HUD, Smoke-Test, Screenshot — darf als Evidence oder Teilnachweis
   für G0–G5 zitiert werden. Es ist Diagnose, kein Content- oder
   Gate-Versprechen; dieselbe Formulierung wie bei V5b in
   [MVPRecoveryPlan.md](MVPRecoveryPlan.md) §6. G0, MS-0 und MS-1 bleiben
   unverändert offen.
2. **Eingefrorener Schreibumfang (K2).** Zulässig sind
   `Assets/_Project/Scripts/Presentation/**`,
   `Assets/_Project/Scripts/Gameplay/{Match,Input,UI}/**`,
   `Assets/_Project/Editor/BootstrapSceneGenerator.cs`,
   `Assets/_Project/Scenes/Bootstrap.unity` (ausschließlich als
   Generator-Ausgabe, nie handeditiert), die zugehörigen Testdateien beider
   Lanes sowie die Governance-Dateien dieser Spur. Ausgeschlossen bleiben
   `quality/**`, `.github/workflows/**` und `VERSION`. Änderungen unter
   `Assets/_Project/Scripts/{Simulation,Core}/**` sind nur im Rahmen von
   D-068 zulässig und verlangen je Änderung einen Test in beiden Lanes und
   einen grünen `DETERMINISM_10000`-SelfCheck.
3. **Zeitlich befristeter Dokumentationsschuld-Modus (K3).** Solange die Spur
   läuft, dürfen die DoD-Punkte 1–2 aus AGENTS.md §8 (Änderungsverlauf plus
   Versionsbump in jedem berührten Dokument; Nachziehen von
   [../README.md](../README.md) und Root-README) je Einzeländerung
   aufgeschoben werden — **ausschließlich**, wenn jede Verschiebung als Zeile
   im [ScopeLedger](ScopeLedger.md) registriert und die Sitzung im
   [GrayboxLog](GrayboxLog.md) protokolliert ist. Nicht aufschiebbar sind:
   der `[Unreleased]`-Eintrag im CHANGELOG, eine D-ID für jede echte
   Entscheidung, ein grüner `docs-check`, die Unberührtheit von `quality/**`
   und das Commit-/Push-Verbot ohne ausdrückliche Anfrage. Die Schuld ist
   Schuld, nicht Erlass: Sie wird registriert, verzinst durch den Verfall
   nach K5 und vor dem Spurende beglichen.
4. **Eskalationsregel (K4).** Die Spur hält an und legt dem Inhaber vor,
   sobald eine Änderung `quality/**`, das byte-gepinnte Trust-Bundle oder
   `.github/workflows/**` berührt; sobald eine Simulations-/Core-Änderung den
   `DETERMINISM_10000`-SelfCheck bräche statt nur seine Hashes zu verschieben;
   sobald ein Snapshot-/Zustandsformat nach der ersten G1-Evidence geändert
   werden müsste (siehe D-068); sobald ein Ergebnis als Gate-Status
   beansprucht würde; oder sobald eine neue Assembly beziehungsweise ein
   neuer Rang in `ASSEMBLY_RANKS` nötig wäre.
5. **Verfall (K5).** Die Spur endet mit dem Eintritt in G2 **oder** 21
   Kalendertage nach Ratifizierung, was zuerst eintritt (bei Ratifizierung am
   2026-07-26 also spätestens 2026-08-16). Zum Verfallstermin ist entweder
   die im ScopeLedger registrierte Dokumentationsschuld beglichen oder die
   Spur wird durch eine **neue** D-ID verlängert. Ein stilles Weiterlaufen
   ist ausgeschlossen; nicht beglichene Schuld wird beim Verfall zum
   Blocker für den nächsten Sprint-Abschluss.

**Begründung:** (a) ist regelkonform, hält aber genau die Arbeit auf, die der
Inhaber braucht, um das Spiel überhaupt erstmals zu sehen und zu bedienen —
und erzwingt serialisierte Doku-Schreibvorgänge auf „heißen" Dateien, die
parallele Agentenarbeit strukturell verbietet. (b) wiederholt das
Fehlermuster, das D-055 korrigieren musste: Artefakte ohne Protokoll werden
später als erreichter Status gelesen. (c) hält die Geschwindigkeit, macht aber
jede Abweichung sichtbar statt still, und erst die harte K1-Klausel macht die
aufgeschobene Dokumentation ungefährlich: Was keinen Gate-Status behaupten
darf, kann durch fehlende Doku auch keinen vortäuschen. Der harte Verfall nach
K5 verhindert, dass aus einer befristeten Ausnahme ein Dauerzustand wird.

**Konsequenzen:** Zwei neue Dokumente unter `docs/production/`:
[GrayboxLog.md](GrayboxLog.md) (append-only Sitzungsprotokoll) und
[ScopeLedger.md](ScopeLedger.md) (eine Zeile je Verschiebung, zeigt auf den
Manifest-Schlüsselpfad statt Werte zu kopieren). AGENTS.md verweist in §2
Regel 4 und §8 auf K3 und ist von der Ausnahme ausdrücklich **nicht**
ausgenommen — die Datei, die jede Agenten-Sitzung automatisch lädt, muss die
Abweichung selbst tragen. Der Wiki-Index [../README.md](../README.md) und die
Root-README-Statuszeile sind als erste registrierte Schuld offen (siehe
ScopeLedger). Solange dieser Entwurf nicht ratifiziert ist, entsteht aus ihm
keine Berechtigung: Die Klauseln beschreiben, was die Sitzung getan hat, und
warten auf das Ja oder Nein des Inhabers.

### D-068 | ENTWURF — Inhaberentscheidung ausstehend | Sprint 7 (Sim-Korrekturen im offenen Pre-G1-Formatfenster)

**Status:** ENTWURF — Inhaberentscheidung ausstehend. Dieser Eintrag ist
**nicht in Kraft**. Entscheidender Autor ist **Dennis Westermann (Project
Owner)**. Die drei beschriebenen Codeänderungen liegen im Arbeitsbaum und sind
mit Tests in beiden Lanes und grünem SelfCheck belegt; die Entscheidung, die
hier zur Ratifizierung vorliegt, ist nicht „ist der Code richtig", sondern
„werden Formats- und Hash-Änderungen dieser Art jetzt genommen statt später".

**Kontext:** In der Graybox-Sitzung fielen drei Korrekturen im
Simulationskern an: (1) ein beschränkter Flow-Field-Cache mit 32 Einträgen,
adressiert über das Ziel statt über ein einziges globales Feld — vorher
überschrieb Befehlsgruppe B das Feld von Gruppe A; (2) `CostField.Epoch` plus
Pathfinding-Snapshotblock v2 — vorher lag das Kostenfeld in **keinem**
Snapshotblock, Terrain konnte sich also ändern, ohne den kanonischen
Zustandshash zu bewegen; (3) der Harvester-Autozyklus — eine volle Ladung
behält `HarvestFieldId` und setzt `IsReturningCargo`, sodass
Ernte → Rückkehr → Ernte ohne Befehl weiterläuft. Alle drei verschieben
kanonische Zustandshashes, (2) ändert zusätzlich ein Blockformat.
[MVPRecoveryPlan.md](MVPRecoveryPlan.md) §4 fordert für G1 einen
**einmaligen Pre-G1-Kompatibilitätsreset der Prototypformate**. Dieses Fenster
steht exakt so lange offen, wie keine G1-Evidence existiert — und es existiert
keine: G0 ist offen, MS-0 nicht erreicht.

**Alternativen:** (a) Keine der drei Änderungen jetzt nehmen und alle hinter
die erste G1-Evidence schieben; (b) nur die reinen Verhaltenskorrekturen (1)
und (3) nehmen und die formatberührende Korrektur (2) verschieben; (c) alle
drei jetzt im offenen Pre-Evidence-Fenster nehmen, je Änderung mit Test in
beiden Lanes und grünem `DETERMINISM_10000`-SelfCheck, und das neue
Hash-Tripel als lokale Baseline festschreiben.

**Entscheidung:** *Vorschlag — noch nicht in Kraft.* (c). Neue lokale
Baseline von `DETERMINISM_10000` nach allen drei Korrekturen: Fingerprint
`0xB455B5E3A0752A36`, Checkpoint Tick 100 `0x75C54A435FCFAB06`, finaler
Zustandshash `0x87F889400D1B6C8C`. Vorher galt
`0xB1126835B5F32BCF` / `0xD1B9E0D000E0A88A` / `0x25E9E181B19B945C`. Die
einzige Zusage, die nicht brechen darf, hält:
„Playback reproduced every recorded result and the recorded final state
hash." Das Tripel wurde von drei unabhängig arbeitenden Agenten und einem
separaten Verifikationslauf identisch reproduziert.

**Begründung:** Die Kosten sind asymmetrisch. Heute kostet eine
Hash-Verschiebung eine Zeile in einem Protokoll, weil keine Evidence die alten
Zahlen referenziert. Nach der ersten G1-Evidence entwertet jede dieselbe
Änderung die plattformübergreifende 10.000-Tick-Pinnung (V1) und erzwingt eine
vollständige Neumessung auf Windows-x64 **und** macOS-arm64 samt Neuaufbau der
Evidence-Kette. Korrektur (2) ist zudem keine Optimierung, sondern das
Schließen eines Determinismuslochs: Ohne serialisiertes Kostenfeld ist eine
Terrain-Mutation für den kanonischen Hash unsichtbar, und genau diesen Zustand
müsste G1 sonst zertifizieren. (a) und (b) trügen dieses Loch in die
Evidenzphase; (b) verschiebt außerdem ausgerechnet die einzige Änderung, die
das Fenster wirklich braucht, weil (1) und (3) formatneutral sind.

**Konsequenzen:**

- Neue lokale Baseline wie oben. Jede weitere Verschiebung dieser Zahlen
  verlangt dieselbe Behandlung: Test in beiden Lanes, grüner SelfCheck,
  protokollierte Zahlen.
- **Vor dem G1-Freeze zu klären, eigene D-ID nötig:** Der Restore von
  Pathfinding-Block v2 erzwingt Gleichheit von serialisierter Epoche und
  lebender `CostField.Epoch`. Heute ruft im Kernelpfad nichts `SetCost`, die
  Epoche ist immer 0 und die Prüfung kostenlos. Sobald der Bau zur Laufzeit
  Terrain verändert, lädt ein Snapshot nur noch in einen Host mit identischer
  Mutationszahl — dann muss entweder das Kostenfeld selbst ein serialisierter
  Block werden oder die Prüfung beginnt, legitime Ladevorgänge abzulehnen.
- Das Fenster schließt mit der ersten G1-Evidence. Danach braucht jede
  Formatänderung eine eigene Entscheidung samt vollständiger Neumessung auf
  beiden Plattformen; die Eskalationsklausel K4 aus D-067 greift ab diesem
  Zeitpunkt automatisch.
- Der Harvester-Autozyklus ist notwendig, aber nicht hinreichend: `EconomySystem`
  erzeugt keine Bewegung, und im kanonischen Layout liegen Feld (7,7) und
  Raffinerie-Ursprung (8,4) drei Zellen auseinander. Ein Harvester füllt sich,
  schaltet auf Rückkehr und hält. Das Schließen des Kreises ist G2-Arbeit.
- Kein Artefakt dieser Korrekturen beansprucht Gate-Status; `quality/**` und
  `.github/workflows/**` bleiben unberührt. G0, MS-0 und MS-1 bleiben offen.

---

### D-069 | verbindlich | Sprint 7 (Art-Strang, Kanalbelegung der Art-Mask-Textur)

**Kontext:** Der MS-1-Art-Strang benötigt eine feste Kanalbelegung für die
Maskentextur des `NovaUnit`-Materials; [ArtAssetStandard.md](../assets/ArtAssetStandard.md)
setzt diese Festlegung voraus.

**Alternativen:** (a) R=Metallic/G=Smoothness/B=Occlusion/A=TeamMask –
verlustärmste Maske im Alpha-Block, bricht aber die URP-Lit-Kompatibilität;
(b) separate einkanalige Team-Maskentextur – beste Qualität, verletzt die
Ein-Textur-Set-Regel aus [AssetBudget.md](../tech/AssetBudget.md);
(c) Team-Maske über Vertex Colors – spart eine Texturebene, ist an die
Mesh-Auflösung gebunden und über LOD-Stufen nicht stabil; (d) R=Metallic,
G=Occlusion, B=TeamMask, A=Smoothness.

**Entscheidung:** (d) – R = Metallic · G = Occlusion · B = TeamMask ·
A = Smoothness.

**Begründung:** Metallic in R und Smoothness in A entsprechen der
URP-Lit-Konvention, dadurch rendert jedes Asset auch ohne den
projekteigenen `NovaUnit`-Shader auf reinem URP Lit korrekt – nur ohne
Teamfarbe. Das entkoppelt den Art-Strang vom Shader-Strang. TeamMask in B,
weil eine großflächige weiche Maske die BC7-Kompression im geteilten
RGB-Block am besten verträgt.

**Konsequenzen:** [ArtAssetStandard.md](../assets/ArtAssetStandard.md)
verankert die Kanalbelegung verbindlich für alle 34 MS-1-Assets.

### D-070 | verbindlich | Sprint 7 (Art-Strang, 0-€-Beschaffungspfad)

**Kontext:** Der MS-1-Art-Strang braucht einen verbindlichen
Beschaffungspfad ohne Budget; [SourceCatalog_MS1.md](../assets/SourceCatalog_MS1.md)
und [Licenses.md](../assets/Licenses.md) benötigen eine Whitelist/Blacklist.

**Alternativen:** (a) bezahlter Anbieter-Tier (~20 $/Monat) – klarste
Rechtslage, scheitert am fehlenden Budget; (b) ausschließlich CC0 ohne KI –
maximale Rechtssicherheit, deckt laut Recherche nur einen Teil der
17 Rollen stilistisch ab; (c) Kauf-Kits wie Synty – einheitlicher Stil out
of the box, kostet Geld und schränkt die Weitergabe im öffentlichen Repo
ein; (d) 0-€-Pfad aus CC0-Quellen, Hunyuan3D 2.1 lokal/self-hosted, OpenAI
Image API für 2D-Referenz und Sketchfab nach Einzelfallprüfung.

**Entscheidung:** (d) – erlaubt: CC0-Quellen (Quaternius, Kenney,
Poly Haven, ambientCG), Hunyuan3D 2.1 lokal/self-hosted, OpenAI Image API
für 2D-Referenz, Sketchfab nach dokumentierter Einzelfallprüfung; gesperrt:
Meshy Free-Tier, Tripo3D Free-Tier und jeder Anbieter ohne belegbare
kommerzielle Nutzung und ohne Output-Eigentum im kostenlosen Tier;
Default-Deny für neue Anbieter.

**Begründung:** Kein Budget, MVP-Priorität. Hunyuan3D 2.1 ist der einzige
Pfad, der 0 €, kommerzielle Nutzung und Output-Eigentum zusammenbringt.

**Konsequenzen:** Geringere vertragliche Eigentumssicherheit als ein
bezahlter Tier (Community License statt kommerziellem Vertrag) und eine
Hardware-Abhängigkeit (Größenordnung 16–24 GB VRAM, als Schätzung
markiert), ausdrücklich als erkaufter Preis benannt. Rückfallebene ist
reiner CC0-Kitbash, **kein** bezahlter Dienst.
[SourceCatalog_MS1.md](../assets/SourceCatalog_MS1.md) und
[Licenses.md](../assets/Licenses.md) führen die Whitelist/Blacklist.

### D-071 | verbindlich | Sprint 7 (Art-Strang, Grid-Zellgröße und Gebäude-Footprints)

**Kontext:** [Buildings.md](../gamedesign/Buildings.md) lässt die
Gebäude-Footprints offen; ohne feste Zahl ist keine Modellierung möglich.

**Alternativen:** (a) 2,0-m-Zelle – feinere Basenplatzierung, macht
Gebäude im Verhältnis zu Fahrzeugen zu klein; (b) 4,0-m-Zelle – wuchtigere
Bauten, kostet Platzierungsflexibilität auf der Karte; (c) Modellierung
ohne Grid-Bindung und späteres Skalieren – vermeidet die Festlegung,
erzeugt aber Nacharbeit an jedem Asset und inkonsistente Texel-Density;
(d) 3,0-m-Zelle mit festen Footprints Power 3×3, Refinery 4×4,
Barracks 3×3, ResearchLab 3×3.

**Entscheidung:** (d) – 1 Grid-Zelle = 3,0 m; 2×2 = 6,0 m, 3×3 = 9,0 m,
4×4 = 12,0 m Kantenlänge; Power 3×3, Refinery 4×4, Barracks 3×3,
ResearchLab 3×3, je Fraktion identisch.

**Begründung:** `Buildings.md` markiert die Footprints selbst als offen;
ohne feste Zahl ist keine Modellierung möglich. Die Werte sind
art-seitige Arbeitsannahmen, die die Simulation überschreiben darf –
Modellmaße folgen dann der Zellzahl, nicht umgekehrt.

**Konsequenzen:** [ArtAssetStandard.md](../assets/ArtAssetStandard.md) und
[VerticalSlice_MS1.md](../assets/VerticalSlice_MS1.md) legen die
Footprints als Art-Arbeitsannahme zugrunde, überschreibbar durch die
Simulation.

### D-072 | verbindlich | Sprint 7 (Art-Strang, Fraktionspaletten MS-1)

**Kontext:** [Factions.md](../gamedesign/Factions.md) nennt nur
Farbnamen; der Art-Strang benötigt verbindliche Hex-Werte für Allianz und
Legion.

**Alternativen:** (a) Farbnamen ohne Hex-Werte belassen – maximale
Flexibilität, macht jedes Asset unvergleichbar; (b) kräftigere,
gesättigtere Töne – höherer Wiedererkennungswert, kollidiert mit der
Spielerfarbe, die laut [CoreGameplay.md](../vision/CoreGameplay.md)
Vorrang hat; (c) Farbwahl erst nach dem ersten fertigen Asset –
realitätsnäher, blockiert aber den parallelen Start an mehreren Assets;
(d) feste Hex-Paletten je Fraktion, jetzt entschieden.

**Entscheidung:** (d) – Allianz Grundton `#8A9199`, Sekundär `#2C6E9E`,
Akzent `#4FD8FF`. Legion Grundton `#7A3524`, Sekundär `#B08430`,
Akzent `#2B2018`.

**Begründung:** Die Werte sind auf Lesbarkeit bei 18–90 m Kameradistanz
und auf Unterscheidbarkeit bei Deuteranopie/Protanopie ausgelegt; die
Blau-gegen-Rot-Schwäche wird über Helligkeitskontrast und Formensprache
aufgefangen.

**Konsequenzen:** [ArtAssetStandard.md](../assets/ArtAssetStandard.md) und
[VerticalSlice_MS1.md](../assets/VerticalSlice_MS1.md) führen die
Fraktionspaletten als verbindlich für MS-1.

### D-073 | verbindlich | Sprint 7 (Art-Strang, Sonniss-Weitergabe)

**Kontext:** [Licenses.md](../assets/Licenses.md) §1 und
[AssetRegister.md](../assets/AssetRegister.md) §3.11 widersprachen sich
zur Weitergabe von Sonniss-GDC-Bundle-Rohdateien im öffentlichen
Repository.

**Alternativen:** (a) permissive Lesart, Rohdateien ins Repo – bequem,
riskiert einen Lizenzverstoß im öffentlichen Repository; (b) Rohdateien
in ein separates privates Repository auslagern – sauber, erhöht die
Einrichtungs- und Pflegekomplexität; (c) Sonniss ganz streichen und nur
CC0-Audio nutzen – maximale Sicherheit, verkleinert die verfügbare
Klangbibliothek deutlich; (d) restriktive Lesart: Sonniss-GDC-Bundles
royalty-free zur Verwendung *in* Spielen, nicht zur Weitergabe als
Sammlung im öffentlichen Repo.

**Entscheidung:** (d) – die restriktive Lesart gilt.

**Begründung:** Bei Lizenzunsicherheit gilt die engere Auslegung.

**Konsequenzen:** [Licenses.md](../assets/Licenses.md) korrigiert die
Weitergaberegel; Sonniss-Rohdateien werden nicht ins öffentliche
Repository eingecheckt.

### D-074 | in Kraft — vom Agenten unter ausdrücklicher Inhaber-Delegation entschieden | Sprint 7 (Kampf-Strang, Autorität der Schaden-gegen-Panzerung-Matrix)

**Status:** In Kraft, aber **nicht vom Inhaber selbst getroffen**. Der Inhaber
**Dennis Westermann** hat diese Entscheidung in der Sitzung vom 2026-07-26
ausdrücklich an den ausführenden Agenten **delegiert**; der Agent hat
entschieden und implementiert. AGENTS.md §2 Regel 6 („nicht eigenmächtig
entscheiden") ist damit nicht verletzt, aber auch nicht auf dem Normalweg
erfüllt — die Legitimation stammt aus der Delegation, nicht aus einer
Inhaberprüfung der Optionen. **Der Inhaber kann diese Entscheidung jederzeit
umstoßen.** Bis dahin ist sie verbindlich, weil Code und Tests sie bereits
tragen. Dieser Eintrag ist bewusst als agent-entschieden gekennzeichnet und
wird nicht als Inhaberentscheidung ausgegeben.

**Kontext:** Der Kampf war nicht bewertbar: `CombatSystem` wandte einen flachen
Schadenswert von 15 auf jeden Angriff an, ein Kampfpanzer und ein Schütze waren
offensiv identisch. Für echte Konter braucht die Simulation eine
Schaden-gegen-Panzerung-Matrix — und die Fachdokumentation lieferte dafür
**drei einander widersprechende** Matrizen: [../gamedesign/ArmorSystem.md](../gamedesign/ArmorSystem.md)
mit 6 Schadensarten × 6 Panzerungsklassen, [../gamedesign/Infantry.md](../gamedesign/Infantry.md)
mit 6 × 4 plus einer siebten Schadensart „Kristall" und
[../gamedesign/Vehicles.md](../gamedesign/Vehicles.md) mit 5 × 4 und nur zwei
Panzerungsklassen (`Leicht`/`Schwer`). Die Widersprüche sind nicht
Rundungsrauschen, sondern gegenläufig: Energie gegen Schwer steht in
ArmorSystem.md auf 0,75 und in Vehicles.md auf 1,25; Explosiv gegen Gebäude auf
0,75 gegen 1,25. Ohne Auflösung hätte jede Implementierung eine der drei
Quellen stillschweigend zur Autorität erhoben.

**Alternativen:** (a) [../gamedesign/ArmorSystem.md](../gamedesign/ArmorSystem.md)
ist führend, die 6 × 6-Matrix ist kanonisch, die lokalen Tabellen in Infantry.md
und Vehicles.md werden durch Verweise ersetzt; (b) die Einheitenkategorie-Achse
aus Infantry.md/Vehicles.md („vs. Infanterie / vs. Fahrzeug / vs. Luft /
vs. Gebäude") ist führend, ArmorSystem.md wird auf diese vier Spalten
eingedampft; (c) eine neue, vierte Matrix, die alle drei Quellen zusammenführt
und alle bestehenden Zahlen neu verhandelt.

**Entscheidung:** (a) – [../gamedesign/ArmorSystem.md](../gamedesign/ArmorSystem.md)
ist die alleinige Autorität; ihre 36 Werte sind kanonisch und stehen
unverändert. Repräsentation in der Simulation als ganzzahliger **Prozentwert**
(100 = 1,00) in einer flachen 36-Einträge-Tabelle, angewandt als
`(Basisschaden × Prozent) / 100` in Ganzzahlarithmetik mit Abschneiden — keine
Fließkommazahlen, kein `SimFixed` nötig. Die Matrix behält alle sechs Zeilen und
sechs Spalten, obwohl MS-1 nur vier Schadensarten und fünf Panzerungsklassen
bespielt.

**Begründung:** Vier Gründe, alle aus dem Bestand belegt, keiner erfunden.
Erstens ist die Panzerungsklasse laut ArmorSystem.md ein **Einheitenattribut**
(„jede Einheit hat genau eine Klasse"), während „vs. Fahrzeug" die Klassen
`Light`/`Medium`/`Heavy` zusammenfaltet — genau die Unterscheidung, aus der
Konterspiel entsteht. Zweitens sagt Vehicles.md in seiner eigenen
D-047-Verweisregel, dass verbindliche Waffenwerte nur in
[../gamedesign/Weapons.md](../gamedesign/Weapons.md) und die Konterlogik in
ArmorSystem.md leben — die dortige Matrix widerspricht also der Regel des
eigenen Dokuments. Drittens verweist Weapons.md seinerseits auf ArmorSystem.md.
Viertens ist ArmorSystem.md als einzige der drei Quellen bereits als
„flacher Satz von 36 Zahlen (`damageType × armorClass`), SO-tauglich"
geschrieben, also implementierungsfertig. Die Tabellen in Infantry.md und
Vehicles.md sind abgeleitete Zusammenfassungen, die auseinandergedriftet sind.
Die volle 6 × 6-Ausdehnung bleibt erhalten, weil ein späteres Nachschneiden der
Tabelle teuer ist und das Mitführen nichts kostet.

**Konsequenzen:**
- [../gamedesign/ArmorSystem.md](../gamedesign/ArmorSystem.md) trägt einen
  Autoritätsvermerk; die 36 Werte bleiben unverändert.
- Die lokalen Matrizen in [../gamedesign/Infantry.md](../gamedesign/Infantry.md)
  und [../gamedesign/Vehicles.md](../gamedesign/Vehicles.md) sind **aufgehoben**
  und durch Verweise ersetzt. Die Einheiten-Stattabellen dieser Dokumente
  bleiben unberührt.
- **„Kristall" ist keine Schadensart.** Die siebte Zeile aus Infantry.md ist
  Evolvierten-Inhalt und liegt außerhalb von MS-1 (das Manifest kennt nur
  Allianz und Legion). Registriert im [ScopeLedger](ScopeLedger.md).
- ArmorSystem.md ordnet **Leichten und Kampfpanzer beide der Klasse `Medium`**
  zu und reserviert `Heavy` für den Heavy Tank, der nicht im MS-1-Roster steht.
  Der Agent hatte das zunächst treu übernommen und die daraus folgende
  unbespielte `Heavy`-Spalte als Konsequenz protokolliert statt sie
  stillschweigend zu reparieren.
  **Der Inhaber hat diese Konsequenz überstimmt (2026-07-26, im Chat): der
  Kampfpanzer wird auf `Heavy` hochgestuft.** Damit ist die `Heavy`-Spalte in
  MS-1 bespielt und der von ArmorSystem.md selbst beschriebene Konter
  („Kinetisch 0,25 gegen Schwer erzwingt Raketen/Energie als Antwort auf
  Heavy") wird im MVP tatsächlich erlebbar: Gewehrinfanterie richtet gegen den
  Kampfpanzer kaum noch etwas aus, Raketeninfanterie wird zur Pflichtantwort.
  Das ist eine bewusste **Abweichung von ArmorSystem.md §Panzerungsklassen**,
  keine Konfliktauflösung — ArmorSystem.md bleibt im Übrigen unverändert
  führend, und die 36 Matrixwerte sind nicht angefasst.
  `Air` bleibt unbespielt, es gibt kein Luftroster; das bleibt im
  [ScopeLedger](ScopeLedger.md) registriert. Ein Test in beiden Lanes hält
  positiv fest, dass `Heavy` einen Träger hat, damit eine stille Rückstufung
  auf `Medium` nicht unbemerkt durchgeht.
- Implementiert in `Nova.Simulation.Combat` (`DamageType`, `ArmorClass`,
  `DamageMatrix`, `WeaponProfiles`); Reichweite und Abklingzeit sind seither
  rollenabhängig statt konstant. Der kanonische Zustandshash bewegt sich —
  zulässig im offenen Pre-G1-Formatfenster (D-068, Entwurf).
- Sollte der Inhaber (b) oder (c) vorziehen, sind Matrixwerte und
  Rollenzuordnung Datenänderungen; die Tabellenform, die Prozentdarstellung und
  die Aufrufstelle in `CombatSystem` bleiben davon unberührt.

### D-075 | in Kraft — vom Agenten unter ausdrücklicher Inhaber-Delegation entschieden | Sprint 7 (Fraktions-Strang, Fraktions-Achse in der kanonischen Simulation)

**Status:** In Kraft, aber **nicht vom Inhaber selbst getroffen** — gleiche
Delegationslage wie D-074: der Inhaber **Dennis Westermann** hat den
Fraktions-Strang in der Sprint-Sitzung vom 2026-07-26 ausdrücklich an den
ausführenden Agenten delegiert (die Teil-Entscheidung zur
Legion-Schadensprovenienz hat er per Sprint-Briefing selbst vorgegeben). Der
Agent hat entschieden und implementiert. **Der Inhaber kann diese Entscheidung
jederzeit umstoßen.** Bis dahin ist sie verbindlich, weil Code und Tests sie
bereits tragen.

**Kontext:** Das Manifest ([../../quality/content/mvp-v1.json](../../quality/content/mvp-v1.json))
modelliert zwei Fraktionen mit eigenen Kosten, Bauzeiten, Energie- und
Identitätswerten; die kanonische Simulation kannte bis dahin **eine** flache,
geteilte Definitionstabelle — beide Slots bauten und kämpften mit denselben
Zahlen, „Fraktion" war reiner Manifestinhalt ohne Simulationswirklichkeit.
Für MS-1 muss die Fraktionszugehörigkeit Kosten, Bauzeiten, Energiebilanz,
Waffenwerte und Harvester-Ladekapazität bestimmen, ohne das
Befehls-Wire-Format (Commands.md Schema v1) zu brechen und ohne eine zweite,
driftende Zahlenquelle zu schaffen.

**Alternativen:** (a) die flache geteilte Tabelle beibehalten und Fraktion nur
kosmetisch umsetzen (Farben, Namen) — minimaler Eingriff, erfüllt aber die
Manifest-Kostenasymmetrie nicht und wäre keine Fraktionsidentität; (b) die
Fraktions-Achse in `SimDefinitions` selbst: 34 Definitionen (17 Rollen × 2
Fraktionen), die Allianz-Id IST der `UnitRole`-Wire-Wert (1..17), die
Legion-Id addiert 17 (18..34), Auflösung über `ToDefinitionId(faction, role)`
und die Slot-Fraktion aus dem Economy-Zustand; (c) komplett getrennte
Definitions-Assemblies je Fraktion (zwei Tabellen, zwei Hash-Domänen, zwei
Wire-Namensräume) — maximale Trennung, verdoppelt aber Befehls-Payloads,
Snapshot-Blöcke und jede künftige Wertpflege und macht fraktionsübergreifende
Vergleiche (Konter, Balance) zu einem Cross-Assembly-Problem.

**Entscheidung:** (b) — die Fraktions-Achse lebt in der einen kanonischen
Definitionstabelle. Ergänzend ratifiziert: die Slot-Fraktion ist
Economy-Zustand (Snapshotblock v2, achtes Fingerprint-Array), wird vor
`Kernel.Start()` gebunden und ist danach gesperrt
(`SetSlotFaction`-Guard), und die Harvester-Ladekapazität ist
Definitionsinhalt (`SimUnitDefinition.CargoCapacityAE`, Allianz 330 /
Legion 300) statt eines flachen Provisoriums.

**Begründung:** Drei Gründe. Erstens **Wire-Kompatibilität**: die Id-Regel
hält jede Definitions-Id global eindeutig und ungleich 0, sodass
`CommandIds.IsValidDefinitionId` (`!= 0`) und alle Payload-Layouts unverändert
bleiben — eine Legion-Id ist für Alt-Code schlicht eine weitere gültige Id.
Zweitens ist das **offene Pre-G1-Formatfenster** (D-068, Entwurf) der
eine billige Moment für den Reset: keine Evidence bindet die alten Ids, die
Pre-Fraktions-Ids (überlappende Gebäude- 1–9 und Einheiten- 1–8) konnten
ohne Migrationspfad aus dem Verkehr gezogen werden. Drittens verlangt die
**D-068-Kostenasymmetrie** (Legion baut billiger und schneller) eine
datengetriebene Achse — Kosmetik (a) kann sie nicht tragen, und getrennte
Assemblies (c) kaufen dieselbe Datenmenge zum Preis dauerhafter
Doppelpflege, ohne einen Wire- oder Determinismusvorteil.

**Teil-Entscheidung (Provenienz des Legion-Fahrzeugschadens):** Woher kommt
der Projektilschaden der drei Legion-Kampffahrzeuge, für die Weapons.md
keine Legion-Zeile führt? **Alternativen:** (i) durchgehend die
85-%-Ableitung aus der Allianz-Zeile — einheitlich, überschreibt aber die
konkreten Vehicles.md-Zahlen (die Ableitung produzierte 29/51/93 gegen die
dortigen 28/50/60); (ii) **der konkrete GDD-Wert schlägt die Ableitung**, wo
[../gamedesign/Vehicles.md](../gamedesign/Vehicles.md) eine konkrete
Legion-Schadenszeile nennt; die Ableitung gilt nur, wo die GDDs wirklich
schweigen; (iii) Vehicles.md durchgehend führend für Fahrzeugschaden, auch
auf der Allianz-Seite — verworfen, weil D-047 Weapons.md für Waffenwerte
führend macht und Vehicles.md diese Regel selbst trägt. **Entscheidung:**
(ii), vom Inhaber per Sprint-Briefing vorgegeben: Räuber 28, Koloss 50,
Donnerkanone 60; der Scout bleibt abgeleitet (10).

**Konsequenzen:**
- `SimDefinitions` trägt 34 Definitionen mit dokumentierter Id-Regel;
  `ComputeDefinitionsHash64` hasht die gesamte Tabelle (21-Felder-Layout inkl.
  `cargoCapacityAE`), sodass jede Wertänderung den Replay-Start verweigert.
- Die Ableitungsregel (85 %, Ganzzahl-Prozent) gilt nur noch für Gebäude-HP
  und den Scout-Schaden; die drei Fahrzeugwerte sind authored, nicht
  abgeleitet. Ein Test in beiden Lanes pinnt 28/50/60 ausdrücklich gegen die
  Ableitungsergebnisse 29/51/93.
- **Bekannte verbleibende Spannung:** Vehicles.md nennt in der
  Hyäne-Zeile (Legion Scout) eine konkrete Schadenszahl, die von der
  abgeleiteten 10 abweicht; das Sprint-Briefing hat nur die drei
  Kampffahrzeuge vorgegeben. Der Scout bleibt bis zur Inhaberentscheidung
  abgeleitet — registriert in den Offenen Punkten dieses Protokolls.
- Economy-Snapshotblock v2 (Slot-Fraktion) lehnt v1 ab; der
  `SetSlotFaction`-Guard sperrt die Zuweisung nach `Kernel.Start()`;
  `MatchBootstrap` und `Determinism10000Scenario.BuildHost` weisen die
  Fraktionen vor dem Start zu.
- Die Entity-Store-Snapshotvalidierung deckelt Cargo auf das
  fraktionsübergreifende Maximum (330) und bietet die pro-Entity-
  Fraktionsgrenze als Überladung für Hosts, die die Slot-Fraktionen kennen —
  die kanonische Zwei-Phasen-Wiederherstellung hat zur Validierungszeit
  keine blockübergreifende Fraktionssicht.
- Die Graybox macht die Achse sichtbar: Fraktion bestimmt die Farbe
  (D-072-Paletten), die Rolle weiterhin die Form; das Debug-HUD zeigt die
  Slot-Fraktionen und die Fraktion der Auswahl.
- Sollte der Inhaber (a) oder (c) vorziehen, ist die Achse eine
  Daten-/Strukturänderung an `SimDefinitions` und den Auflöse-Stellen; das
  Id-Schema und die Guard-Semantik sind davon unberührt.

---

### D-076 | verbindlich | Sprint 7 (Governance-Tier-Modell, Gate-Kette schlafend)

**Status:** verbindlich — Inhaberentscheidung vom 2026-08-06 (Dennis Westermann).

**Kontext:** Das Repository trug ein Governance-Regime für ein Projekt, das es
nicht ist. Messbar am Ist-Stand vor dieser Entscheidung:

| Größe | Wert |
|---|---|
| Spielcode `Assets/_Project` | 19.508 Zeilen |
| Dokumentation `docs/` | 19.729 Zeilen in 126 Dateien |
| Governance-Tooling (`quality/scripts`, CI-Skripte) | 6.333 Zeilen |
| Entscheidungen mit ≥3-Alternativen-Pflicht | 75 |
| Commits nach Typ | 59 `docs` gegen 35 `feat` |
| Aktive Entwickler | 2 (147 von 148 Commits von einer Person) |
| Erzeugte Gate-Evidence in 148 Commits | **0** |

Der Gate-Vertrag aus D-061 bis D-066 ist ein Supply-Chain-Provenance-Modell:
getrennte Trusted-Tool-/Subject-Checkouts, append-only Receipts, Verifikation
aller Vorgänger über die GitHub-API, geschütztes Environment mit Required
Reviewers. Sein Bedrohungsmodell lautet „der Committer könnte bösartig sein".
Bei zwei Entwicklern ohne Nutzer, Geld oder Haftung existiert dieses Modell
nicht — und der Apparat war nicht neutral, sondern der Blocker: `quality/evidence/`
und `quality/authorizations/` wurden nie angelegt, MS-0 und MS-1 galten per
Definition als unerreichbar, und die einzige Arbeit, die das Spiel spielbar
machte, lief unter D-067 — einer Ausnahme, die nie ratifiziert wurde.

Gleichzeitig lief **kein einziger Spieltest in der CI**: `docs-check` prüfte
Markdown-Links, `quality-gate` die Selbsttests des Evidence-Validators. Die
Simulationstests liefen ausschließlich manuell auf einem Laptop. Das Regime
prüfte sich selbst, nicht das Produkt.

**Entscheidung:** Governance wird über **Tiers** geregelt statt über eine feste
Regelmenge; Regeln höherer Tiers werden schlafen gelegt statt gelöscht. Details
und Tier-Tabelle: [../../GOVERNANCE.md](../../GOVERNANCE.md). Aktiv ist **Tier 1**
(zwei Entwickler, kein Publikum). Konkret:

1. Die Gate-Kette G0–G5 blockiert keinen Meilensteinfortschritt mehr. Die
   Gate-*Inhalte* bleiben als Arbeitsgliederung gültig
   ([MVPRecoveryPlan.md](MVPRecoveryPlan.md)), der *Evidenzvertrag* schläft.
2. Neuer Meilenstein-Nachweis: **grüne CI plus eine gespielte und protokollierte
   Runde.** Das fängt denselben Fehler wie F-001 — ein Modul, das nur auf dem
   Papier existiert, überlebt keine Spielrunde — ohne Beweisapparat.
3. Neuer Pflichtcheck `tests` in der CI: die Simulationstests aus
   `tools/Nova.SimRunner.Tests`, die dieselben Core-/Simulation-Quellen wie der
   Unity-Host kompilieren und keine Unity-Lizenz brauchen.
4. `integrity` läuft nur noch bei Änderungen an `quality/**`, damit der
   schlafende Apparat lauffähig bleibt, ohne jeden PR zu belasten.
5. Definition of Done: 13 Punkte → 4. PR-Template: 11 Checkboxen → 3.
6. Dokument-Pflichtaufbau, Versionsbump und Änderungsverlauf-Tabelle werden
   freiwillig; Git ist der Änderungsverlauf. Ausnahme: `quality/content/mvp-v1.json`
   bleibt versionierter Vertrag.
7. D-ID-Pflicht bleibt, die ≥3-Alternativen-Pflicht entfällt bis Tier 2.
   Bestehende Einträge werden nicht zurückgebaut.
8. Sprint-Ritual (8 Pflichtschritte) entfällt.
9. Selbst-Merge bei grüner CI ist erlaubt. Die Regel „ab zwei aktiven Maintainern
   ist eine zweite menschliche Freigabe Pflicht" wird gestrichen — sie hätte
   genau jetzt gegriffen und die Arbeit zu zweit verdoppelt teuer gemacht.
10. D-067 wird gegenstandslos; der GrayboxLog verliert seine Pflicht, der
    ScopeLedger bleibt als Lückenliste.

**Verworfen:** (a) Gate-Regime beibehalten und G0-A2 fertigbauen — schätzungsweise
Tage bis Wochen für einen Beweisapparat, den außer den zwei Entwicklern niemand
liest, während der KI-Gegner untätig bleibt; (b) `quality/` löschen — würde die
tatsächlich wertvollen Teile mitnehmen (`mvp-v1.json` als Sollinhalt) und den
Weg zu Tier 3 verbauen; (c) Regeln einfach ignorieren statt sie zu ändern — die
Variante, in der das Repository seit D-067 faktisch schon lief, und die genau
die undokumentierte Statusbehauptung erzeugt, die D-055 zurückgenommen hat.

**Konsequenzen:**

- Nicht mehr führend für den Meilensteinstatus:
  [MVPRecoveryPlan.md](MVPRecoveryPlan.md). Führend sind
  [Milestones.md](Milestones.md) und [../../GOVERNANCE.md](../../GOVERNANCE.md).
- D-061 bis D-066 bleiben inhaltlich unverändert gültig **für Tier 3**. Sie sind
  nicht widerrufen, sondern ruhen.
- Der Weg zurück ist dokumentiert und billig: vier Schritte in
  [../../GOVERNANCE.md](../../GOVERNANCE.md) unter „Was schläft und wie es aufwacht".
- Risiko: Ohne Zwang zur Doku-Pflege driftet das Wiki schneller. Gegenmittel ist
  die Playtest-Notiz, nicht ein neues Formular. Wird die Drift schmerzhaft,
  ist das ein Anlass zur Neubewertung — nicht zur Rückkehr zum Gate-Regime.

---

### D-077 | verbindlich | Spielbarer RTS-Core-Loop (Demo-Reparatur und Vertical Slice)

**Status:** verbindlich — Inhaberentscheidung vom 2026-08-06 (Dennis Westermann).
Startaufstellung (HQ + 1 Builder) und Raffinerie-Prereq (gestrichen) im Dialog
ausdrücklich gewählt; HQ-Sieg-Regel und KI-Aktivierung aus dem Arbeitsauftrag
derselben Sitzung.

**Kontext:** Die erste Demo (GB-004) startete, war aber nicht sinnvoll
spielbar: ein `DebugHud`-OnGUI-Overlay bedeckte fast den gesamten Bildschirm;
die neuen `PF_*`-Modelle (Exportkonvention 1 Zelle = 3,0 m, D-071) standen in
einer Welt mit 1 Zelle = 1 Welt-Einheit und überlagerten sich sämtlich
(Gebäude ~9 m breit, 4 m auseinander, Einheiten unsichtbar in den Meshes);
der KI-Slot war ein nicht registrierter Stub; und die Startaufstellung
(HQ + fertige Raffinerie + 2 Harvester + Builder + 4 Infanterie, 1.000 AE)
schenkte dem Spieler genau den Kernloop, den er spielen sollte.

**Entscheidung:**

1. **Startaufstellung pro Slot: HQ + 1 Builder + 3.000 AE.** Ersetzt das
   bisherige MS-1-Opening. Baustellen brauchen einen Builder in Reichweite —
   strikt „nur HQ" hätte den ersten Kauf (Builder, 800 AE) erzwungen; der
   Inhaber wählte HQ + Builder.
2. **Der Harvester wird von der Raffinerie produziert** (vorher HQ), beide
   Fraktionen. Folgefix: Produzentenrollen liest `ProductionSystem` jetzt aus
   der Definitionstabelle statt aus einer harten Liste — die Rally-Point-
   Validierung hatte die Raffinerie sonst weiter abgelehnt.
3. **Die Raffinerie verliert das Kraftwerk-Prereq** (beide Fraktionen). Ihr
   Power-Bedarf (20/15) bleibt: HQ 30 Power gegen Raffinerie + Kaserne 35
   macht das Kraftwerk ab dem zweiten Gebäude nötig — der Loop startet direkt,
   das Energiemanagement bleibt erhalten.
4. **Sieg zusätzlich bei HQ-Verlust.** Wer ein HQ besaß und es verliert, ist
   besiegt — auch mit Resttruppen (ersetzt Teile von D-056 Klausel 2; die
   Totalvernichtungs-Regel bleibt daneben bestehen). Gleichzeitiger
   HQ-Verlust beider Seiten bleibt Draw.MutualAnnihilation. Kein neuer
   Ergebniscode; Victory-Snapshotblock v1 → v2 (Clean Break, v1 wird
   abgelehnt).
5. **Der KI-Slot spielt.** `SkirmishAiSystem` (Legion, Slot 1) ist in
   `MatchRunner` registriert (nach Combat, vor Victory) und handelt
   ausschließlich über den kanonischen Intent-Pfad — eigene Peer-Session,
   eigene versiegelte Batches, keine direkten State-Writes. Umfang:
   Build-Order Raffinerie → Kaserne (Kraftwerk bei negativem Power-Margin),
   2 Harvester mit Harvest-Orders, Infanterie bis 12, Angriff ab 6
   Kampfeinheiten mit expliziten Attack-Orders nur auf für das KI-Team
   sichtbare Ziele (FoW-legal). Deterministisch, zustandsbasiert,
   20-Tick-Entscheidungskadenz. Füllt `mode.aiSlotCount` in Mindestform.
6. **Präsentations-Reparatur an der View-Grenze, nicht an den Assets.**
   `DebugHud` ist standardmäßig aus (F3 togglet das Diagnose-Panel; eine
   einzeilige Statusleiste mit Credits/Power/Ergebnis bleibt immer sichtbar).
   Prefab-Views normalisiert `UnitViewManager` zur Laufzeit aus den
   Mesh-Bounds auf den Sim-Footprint (Gebäude 3 WE, Einheiten
   Graybox-Tabellengröße, nur Verkleinerung). Die 3,0-m-Exportkonvention
   (D-071) bleibt — Modelle sind austauschbar, ohne Spiellogik oder Prefabs
   anzufassen.

**Verworfen:** (a) die Welt auf 3 m/Zelle umstellen — hätte Sim, Kamera,
Input und Ground angefasst und die testverriegelte Deterministik gefährdet,
nur um Mesh-Größen zu retten; (b) strikt nur HQ ohne Builder — erzwingt eine
Zwangsphase vor dem ersten Bau, vom Inhaber gegen Variante 1 entschieden;
(c) das Raffinerie-Prereq behalten — der Ziel-Loop (HQ → Raffinerie →
Harvester) verlangt den direkten Schritt.

**Konsequenzen:**

- `quality/content/mvp-v1.json`: 1.0.0 → 1.2.0 (Startzustand,
  `harvesterProducer: Refinery`, `refineryPrerequisite: none`,
  `victory.defeatTriggers`).
- D-056 Klausel 2 ist teilweise ersetzt (zusätzlicher Niederlagen-Trigger);
  der Rest von D-056 gilt unverändert.
- `Determinism10000Scenario` fährt den Loop jetzt selbst (Raffinerie
  platzieren, Harvester produzieren, Harvest-Orders); die goldene
  Laufzeit-Hashwerte verschieben sich — im Repo stehen keine Hash-Literale,
  alle Vergleiche rechnen zur Laufzeit.
- Die KI liest ihren eigenen Slot-Zustand direkt statt über die in
  AIArchitecture.md geplanten Sidecar-/TeamWorldView-Typen (dokumentierte
  G1-Vereinfachung); die KI-Peer-Session ist nicht snapshot-serialisiert.
- Bekannt und unverändert offen (ScopeLedger): kein Attack-Move/Auto-Acquire
  (GB-002) — die KI umgeht das mit expliziten Orders, für den Menschen bleibt
  `A` zielpflichtig; nach Sieg tickt der Host weiter, kein
  Ergebnisbildschirm; echte UI statt OnGUI bleibt G4.
- Nachweis: 420/420 .NET-Simulationstests, 425/425 Unity-EditMode-Tests,
  2/2 PlayMode-Tests mit frischen Demo-Screenshots, DETERMINISM_10000
  Self-Check PASS; End-to-End besiegt die KI einen passiven Slot bei Tick
  2242 deterministisch (VictoryElimination, Slot 1).

---

### D-083 | verbindlich | Hauptmenü als Overlay, UI Toolkit als UI-Standard

**Status:** verbindlich. **Vom Agenten unter ausdrücklicher Inhaber-Delegation
entschieden, nicht vom Inhaber selbst** (dritter Eintrag dieser Art nach D-074
und D-075) — betrifft die Punkte 1 bis 4 und 6 und ist überstimmbar; eine
Umkehr ist eine Implementierungsänderung, keine Vertragsänderung.
**Ausgenommen von der Delegation ist Punkt 5:** Assetherkunft (Suno-Bezahltarif,
OpenAI Image API), Schriftwahl (Rajdhani, OFL-1.1) und der Menütitel
„HASHKRIEG" hat der Inhaber (Dennis Westermann) am 2026-08-06 selbst entschieden.

**Nummernwahl:** D-077 ist die letzte im Dokumentkörper belegte ID.
[hashkrieg/00_Entscheidungen.md](hashkrieg/00_Entscheidungen.md) reserviert den
anschließenden Block für die Übertragung der Inhaberentscheidungen E-1 bis E-5
— dort steht unter „Offene Punkte" noch „D-078 bis D-081", die Stand-Tabelle
führt aber inzwischen fünf Entscheidungen, der Block ist also real D-078 bis
D-082. Diese Entscheidung nimmt deshalb **D-083** und überspringt den
reservierten Bereich. Eine übersprungene Nummer ist billig; dieses Protokoll
hat eine ID-Kollision schon einmal teuer bezahlt (D-066–D-070, siehe „Offene
Punkte").

**Kontext:** Das Spiel startete direkt in ein Match: kein Menü, kein Ton, keine
Einstellungen, kein sauberer Weg hinaus. Die Ausgangslage war dünn und dadurch
offen — genau **eine** Szene (`Assets/_Project/Scenes/Bootstrap.unity`,
Maschinenausgabe von `BootstrapSceneGenerator`), **null** `SceneManager`-Aufrufe
in Produktionscode, kein `Application.Quit`, kein `PlayerPrefs`, kein
`AudioListener` und keine Audiodatei, kein Canvas und kein UI Toolkit im
Einsatz. Alles bisherige UI ist `OnGUI` (`DebugHud`, `RtsDeviceInput`) und im
Code selbst als Wegwerf markiert. Das Menü ist damit das erste echte UI des
Projekts — es setzt einen Standard, ob man will oder nicht, und deshalb steht
die Entscheidung hier und nicht nur im Sprintdokument.

**Entscheidung:**

1. **Overlay in der einen Szene statt zweiter Szene.** Das Menü ist ein
   Szenenobjekt in `Bootstrap.unity`, angelegt **im Generator**, nicht von Hand
   (die Szene ist Maschinenausgabe und wird nie handeditiert). `MatchBootstrap`
   ist bereits vorbereitet: `AutoStart` ist ein `[SerializeField] public bool`,
   `StartGrayboxMatch()` ist `public` und idempotent. „Neues Spiel" ruft
   `StartGrayboxMatch()` und blendet das Overlay aus. Eine zweite Szene wäre der
   erste Scene-Flow-Layer des Projekts gewesen — der größte Einzelposten des
   Sprints, ohne Gegenwert für den Spieler.
2. **UI Toolkit statt uGUI — und damit der UI-Stack für alles Neue.**
   `com.unity.modules.uielements` ist ein Engine-Modul und braucht keinen
   asmdef-Eintrag; `Nova.Presentation.UI.asmdef` referenziert weiterhin
   ausschließlich `Nova.*`-Assemblies. uGUI hätte echte Assembly-Referenzen
   (`UnityEngine.UI`, `Unity.TextMeshPro`), EventSystem plus
   StandaloneInputModule in der Szene und einen TMP-Essentials-Import
   gebraucht. Das bestehende `OnGUI`-UI bleibt Wegwerf und wird nicht portiert;
   jedes **neue** UI entsteht in UI Toolkit.
3. **`AutoStart = false` im Generator.** Play führt ab jetzt ins Menü, nicht ins
   Match. Das ist die spürbarste Verhaltensänderung des Sprints und trifft
   jeden, der die Demo vorführt oder einen PlayMode-Test schreibt.
4. **Einstellungen als JSON in `Application.persistentDataPath`, ohne
   `PlayerPrefs` und ohne `AudioMixer`.** `GameSettings`/`GameSettingsStore`
   schreiben eine einzige lesbare `settings.json`; angewandt wird beim Start
   über `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`, also ohne
   Boot-Objekt und szenenunabhängig. Musiklautstärke geht direkt auf
   `AudioSource.volume`. `PlayerPrefs` wäre eine undurchsichtige
   Registry-/plist-Ablage ohne Diff und ohne Löschpfad für einen Vorführer; ein
   `AudioMixer` wäre ein Asset plus Bus-Topologie für genau eine Audioquelle —
   der Audioplan nennt Mixer-Busse als Vorbedingung, das ist Doku-Meinung, keine
   Engine-Einschränkung, und für diesen Umfang falsch.
5. **Assetherkunft, Schrift und Titel (Inhaberentscheidung, nicht delegiert).**
   Menümusik aus **Suno im Bezahltarif**, Key Art aus der **OpenAI Image API
   (gpt-image-1)**, Schrift **Rajdhani** unter **OFL-1.1** samt beiliegender
   `OFL.txt` — alle drei mit kommerzieller Nutzung und Output-Eigentum bzw.
   freier Weitergabe, deshalb Repo-Freigabe. Der Suno-Bezahltarif ist die erste
   benannte Ausnahme von [../assets/Licenses.md](../assets/Licenses.md) §2
   Regel 5 („0 € ist hart für MS-1"). Der Menütitel lautet **„HASHKRIEG"** —
   Vollzug von E-3 („nur die Marke"); `namespace Nova.*` und der
   Repository-Name bleiben unverändert.
6. **„Laden" sichtbar, aber ausgegraut, mit Tooltip „kommt später".** Die
   Snapshot-Schicht kann den vollständigen Matchzustand serialisieren und
   hash-identisch fortsetzen, aber **nichts schreibt je auf Platte**: kein
   Runtime-Datei-I/O, kein Save-Format, keine Slots. Verstecken würde den
   Spieler im Unklaren lassen, ob das Spiel speichern kann; anbieten würde ihn
   ins Leere greifen lassen.

**Verworfen:** (a) zweite Szene mit `SceneManager` — hätte den ersten
Scene-Flow-Layer des Projekts eingeführt, für eine Funktion, die ein Overlay
kostenlos leistet; (b) uGUI/TextMeshPro — echte Assembly-Referenzen,
Szenen-Infrastruktur und ein Extra-Import, ohne dass das Ergebnis besser
aussähe; (c) `PlayerPrefs` für die Einstellungen — nicht inspizierbar, nicht
löschbar ohne Werkzeug, nicht diff-fähig; (d) „Laden" verstecken oder
funktionsfähig vortäuschen — beides belügt den Spieler über den Speicherstand;
(e) SFX-Regler weglassen, bis es SFX gibt — der Wert wird ohnehin gespeichert,
und ein als „noch ohne Wirkung" gekennzeichneter Regler ist ehrlicher als eine
Lücke, die später kommentarlos auftaucht.

**Konsequenzen:**

- `Assets/Tests/PlayMode/GrayboxDemoProofTests.cs` muss das Match explizit über
  `StartGrayboxMatch()` starten — mit `AutoStart = false` reißen sonst die
  15-Sekunden-Assertions auf `IsMatchReady`. Test mitziehen, nicht abschalten.
- [../assets/Licenses.md](../assets/Licenses.md) 1.3.0 → 1.4.0: zwei neue
  §1-Quellen (Suno, OFL-1.1), benannte Ausnahme in §2 Regel 5, Erweiterung der
  Whitelist in Regel 6, drei Ledger-Zeilen in §3.
- [DemoRunbook.md](DemoRunbook.md) 0.3.0 → 0.4.0: der Ablauf beginnt am Menü,
  nicht am Match.
- Der SFX-Regler wird gespeichert und angewandt, **wirkt aber auf nichts** — es
  gibt noch keine SFX. Im UI als solcher gekennzeichnet.
- Alle sechs Render-Detail-Stufen teilen sich **ein** URP-Asset. 19 Felder
  unterscheiden sich real (u. a. `lodBias` 0,3–2,0, Anisotropie,
  `particleRaycastBudget`), aber `renderScale`, Schatten und MSAA nicht. Zwei
  zusätzliche `NovaUrp`-Kopien würden den Unterschied sichtbar machen —
  bewusst nicht in diesem Sprint.
- Bewusst offen: Pause-Menü, Restart, Ergebnisbildschirm, Rückweg vom Match ins
  Menü, Fraktions- und Kartenwahl, Tastenbelegung. Die Fraktionswahl
  insbesondere hängt an `InitialStateHash` und wäre eine Determinismus-, keine
  Menü-Änderung.
- Offene Nachzieharbeit außerhalb dieses Eintrags: Die drei neuen Assets
  (`UI_KeyArt_MainMenu.jpg`, `MUS_MainMenu_Hashkrieg.ogg`, die beiden
  Rajdhani-TTFs) haben noch keinen `PROVENANCE.json`-Datensatz, obwohl
  [../assets/Provenance.md](../assets/Provenance.md) ihn ausdrücklich auch für
  Audio und Fonts vor der Repo-Aufnahme verlangt. Der Ledger-Eintrag in
  `Licenses.md` §3 deckt die Lizenzlage, nicht den Herkunftsnachweis je Datei.
- Ebenfalls nachzuziehen: die Reservierungszeile in
  [hashkrieg/00_Entscheidungen.md](hashkrieg/00_Entscheidungen.md) („D-078 bis
  D-081") deckt E-5 nicht mehr ab und die dortige Begründung „D-077 ist im
  DecisionLog noch nicht eingetragen" ist überholt.

---

### D-084 | verbindlich | Bedienbares HUD (GB-006, Präsentationsschicht)

**Status:** verbindlich — Inhaberauftrag vom 2026-08-06 (Dennis Westermann)
mit ausdrücklichem Aufgabenbrief (neun Punkte, Randbedingungen: kein Sim-
Eingriff, eingefrorenes Command-Register); die hier protokollierten
Einzelfestlegungen sind die Umsetzungsentscheidungen daraus.

**Nummernwahl:** Diese Entscheidung lief parallel zum Hauptmenü-Strang und
trug zunächst die Nummer D-078. D-078 bis D-082 sind jedoch für die
Übertragung der Inhaberentscheidungen E-1 bis E-5 reserviert und D-083 ist
das Hauptmenü (siehe deren Nummernwahl-Vermerk) — deshalb **D-084**.

**Kontext:** Der Inhaber spielte den D-077-Stand erstmals und meldete ihn als
„nicht bedienbar": keine sichtbare Selektion, kein Baumenü, Gebäude rotierten
bei Rechtsklick, Kamera-Rotation unauffindbar, Nebel und Minimap fehlten. Die
Simulation war ausdrücklich nicht das Problem.

**Entscheidung (Umsetzungsfestlegungen):**

1. **Datenquelle der Bauleiste ist `SimDefinitions`**, nicht
   `BuildingRegistrySO` (im Brief vorgeschlagen): das SO hat keine Asset-
   Instanzen und ist zur Laufzeit nicht verdrahtet; `SimDefinitions` ist die
   Tabelle, gegen die der Executor selbst validiert — die Leiste kann so nicht
   vom Sim-Stand abdriften. Wird `BuildingRegistrySO` eines Tages lebendig,
   ist die Quelle austauschbar.
2. **Das eingefrorene Command-Register (schema v1) wird nicht erweitert.**
   Nicht abbildbare Features bleiben **offene Design-Fragen** statt
   Improvisation: Gebäude-Upgrades, Zielprioritäten für Türme,
   Angriffsbewegung/Stellung-halten. Reparieren wird aus vorhandenen Kinds
   komponiert (Move + Repair als zwei Intents, damit der Builder in
   Reichweite läuft); InstallDefenseModule wird deaktiviert mit Grund
   angezeigt, nie abgeschickt.
3. **Alle HUD-Features sind präsentationsseitig.** Der Gebäude-Rotations-Bug
   wurde input- und view-seitig behoben (unbewegliche Rollen aus
   Bewegungsbefehlen filtern; keine Rotations-Schreibzugriffe auf Gebäude-
   Views), Fog-Overlay und Minimap lesen ausschließlich den committed
   Team-View — Determinismus, Snapshots und der 10-Hz-Tick bleiben unberührt.
4. **Kamera-Rotation:** mittlere Maustaste + Drag zusätzlich zu Z/X,
   Reset auf **Space** (das Buchstaben-Budget ist erschöpft; N ist
   Panzerabwehr-Infanterie).
5. **Minimap ↔ Kamera laufen über `MinimapCameraLink`** (statischer Kanal in
   Nova.Gameplay), weil die beiden Presentation-Assemblies einander per
   Rang-Regel nicht referenzieren dürfen.

**Verworfen:** (a) `BuildingRegistrySO` als Leisten-Quelle (s. Punkt 1);
(b) ein Neubau der HUD-Schicht auf UI Toolkit — D-083 setzt UI Toolkit als
Standard für alles Neue und erklärt das OnGUI-HUD zugleich zum nicht zu
portierenden Wegwerfcode; dieser Pass hält sich daran (IMGUI-Idiom der
Graybox, kein Port, keine doppelte UI-Schicht); (c) neue
CommandKinds für Attack-Move/Hold — verboten durch die Randbedingung und
richtig so: das Register ist der MP-Vertrag.

**Konsequenzen:**

- Offene Design-Fragen für einen eigenen Entscheid: **Angriffsbewegung /
  Stellung halten / Feuererwidern** (hängt mit GB-002 zusammen — betrifft
  Combat, also Sim) sowie **Gebäude-Upgrades und Turm-Zielprioritäten**
  (bräuchten Register-Erweiterungen; nur mit neuem D-Eintrag und
  Schema-Versionierung).
- `CommandCardPresenter` ist die testbare Rollen-Aktions-Tabelle; ein Tech
  Tree baut später darauf auf (die Bauleiste ist seine dokumentierte Vorstufe).
- Nachweis: 420/420 .NET, 445/445 EditMode, 2/2 PlayMode, Nebel-Overlay
  visuell bestätigt; interaktiver Inhaber-Durchlauf als DoD-Punkt ausstehend.

---

### D-085 | verbindlich | Baumodell: Builder-Modell bleibt, Auto-Dispatch per Move-Intent

**Status:** verbindlich — Inhaberentscheidung vom 2026-08-06 (Dennis
Westermann) aus der Spielsitzung desselben Tages, schriftlich fixiert in
[hashkrieg/10_Sprint_Baubarkeit_und_Kartenbild.md](hashkrieg/10_Sprint_Baubarkeit_und_Kartenbild.md)
§3; hier protokolliert wie angeordnet.

**Kontext:** In der Spielsitzung ließen sich Gebäude platzieren und das Geld
wurde abgebucht, aber keine Baustelle machte je Fortschritt. Verifizierte
Ursache: `ConstructionSystem.ProgressSites` lässt eine Baustelle nur dann
wachsen, solange der zugewiesene Builder in Chebyshev-Reichweite ≤ 1 des
3×3-Footprints steht, und ohne lebenden eigenen Builder pausiert sie. Die
Zuweisung (eigener Builder mit kleinstem Entity-Index) existierte — aber
niemand schickte den Builder hin. Die KI hat genau diese Verdrahtung für
sich (Move-Payload auf eine Nachbarzelle des Footprints), der menschliche
Platzierungspfad sendete nur den Bau-Befehl. Stirbt der einzige Builder,
frieren alle Baustellen dauerhaft und ohne jede Meldung ein.

**Entscheidung:** Von drei Baumodellen ist gewählt:

1. **Das Builder-Modell bleibt.** Die Reichweiten-Regel in
   `ConstructionSystem` wird nicht angetastet.
2. **Der Builder wird beim Platzieren automatisch zur Baustelle geschickt** —
   ein zusätzlicher Move-Befehl über den ganz normalen Command-Pfad, also
   dieselbe Klasse von Ereignis wie ein Mausklick des Spielers. Die
   Builder-Wahl spiegelt die der Simulation (eigener Builder mit kleinstem
   Entity-Index), die Zielzelle folgt dem deterministischen Muster der KI
   (`originX - 1`, Ostseite als Kartenrand-Fallback).
3. **Die Zustandsanzeige ist Teil derselben Entscheidung, nicht optional:**
   die Baustellen-Card nennt den echten Zustand in der Auswertungsreihenfolge
   der Simulation (kein Builder → Builder unterwegs → im Bau, X % → fertig in
   ~Y s), und ein Platzieren ohne lebenden Builder wird sofort und sichtbar
   gewarnt statt still einzufrieren. Der benannte Preis der Wahl: stirbt der
   Builder auf dem Weg, pausiert der Bau — dann muss der Spieler einen neuen
   Builder bauen und ihn hinschicken.

**Verworfen:** (a) das C&C-Modell (Baustelle wächst, solange ein HQ lebt)
und (b) das Hybridmodell (HQ baut langsam, Builder beschleunigt) — beide
hätten die Reichweiten-Regel in `ConstructionSystem` verändert und damit die
kanonischen Hash-, Replay- und Fingerprint-Baselines gebrochen. Die gewählte
Variante ist eine reine Eingabe-Automatik: keine Regeländerung, kein
Snapshot-Bump, keine neuen Baselines, das Command-Register schema v1 bleibt
eingefroren.

**Konsequenzen:**

- Der Auto-Dispatch läuft ausschließlich über die bestehenden
  schema-v1-Kinds (PlaceBuilding + Move); jede spätere Änderung der
  Bauregeln ist eine Simulationsänderung mit eigenem D-Eintrag und eigenen
  Baselines.
- Nachweis der Determinismus-Disziplin: 420/420 .NET-Tests, SimRunner-Hash
  `0x2FBEC31FBC0BF430` und DETERMINISM_10000-Fingerprint `0xF866FDC042D260E1`
  vor und nach dem Sprint byteweise identisch.

---

### D-086 | verbindlich | Suno-Ausnahme um Ingame-Musik erweitert

**Status:** verbindlich — Inhaberentscheidung vom 2026-08-07 (Dennis
Westermann), im Dialog erteilt (Auswahl „drei Themen": je längste Fassung —
`1_orc`, `2 (2)`, `3 (1)`).

**Kontext:** D-083 hat den Suno-Bezahltarif ausdrücklich zweckgebunden **nur**
für die Menümusik freigegeben („erzeugt kein Präzedenzrecht",
[Licenses.md](../assets/Licenses.md) §2 Regel 5). Sprint 09 bringt Musik ins
Gefecht; die drei verwendeten Themen stammen aus derselben Quelle und
demselben Tarif, fielen aber nicht unter den ursprünglichen Zweck.

**Entscheidung:** Die Regel-5-Ausnahme gilt zusätzlich für die Ingame-Musik
`Assets/_Project/Audio/Music/MUS_Ingame_Hashkrieg_01..03.ogg` (OGG-Vorbis,
aus den Suno-MP3s konvertiert; Streaming-Import, Qualität 0,7, Load In
Background). Die Begrenzung auf Quelle und Zweck sowie das fehlende
Präzedenzrecht bleiben unverändert; die `PROVENANCE.json`-Pflichtfelder
stehen wie bei der Menümusik beim Inhaber aus (Offene Punkte der
Lizenzdatei).

**Verworfen:** (a) MP3-Quelldateien direkt importieren — technisch
gleichwertig (Unity transkodiert beim Import), aber die Projektkonvention ist
OGG; (b) Ingame-Musik streichen, bis die Provenienzdatensätze vorliegen —
die Lizenzlage ist mit der Ausnahme-Erweiterung gedeckt, der Herkunftsnachweis
ist eine nachgelagerte Pflicht, keine Blockade.

**Konsequenzen:** Ledger-Eintrag §3 und Regel-5-Erweiterung in
[Licenses.md](../assets/Licenses.md) (1.5.0) sind Teil dieser Entscheidung.
Repo-Zuwachs ~14 MB ist bewusst in Kauf genommen; Audio fällt nicht unter die
Art-Paket-Regel.

---

### D-087 | verbindlich | Zielerfassung und Feuererwiderung im Kampfsystem

**Status:** verbindlich — Inhaberauftrag vom 2026-08-06/07 (Sprint 09 §4;
Leitsatz „aus der Demo wird eine Runde").

**Kontext:** `AttackTarget` wurde ausschließlich per explizitem Befehl
gesetzt — jeder Schuss brauchte einen Klick, und die einzige bewaffnete
Gebäuderolle (Verteidigungsplattform) konnte nie feuern, weil Gebäude gar
keine Befehle empfangen.

**Entscheidung:** `CombatSystem.ExecuteTick` hat eine **Auto-Acquire-Phase**
zwischen Cooldown und Feuern: jede aktive Einheit ohne gültiges Ziel wählt
das nächste feindliche, sichtbare (committed Team-View), in Waffenreichweite
liegende Ziel — Gebäude eingeschlossen. Determinismus-Regeln: aufsteigende
Index-Scans, Abstandsvergleich im Quadrat in geweiterter Ganzzahlarithmetik,
bei Gleichstand gewinnt der kleinste Entity-Index; explizit gehaltene
Angriffsziele werden nie umgesetzt. Kein neuer `CommandKind`, kein neues
`UnitState`-Feld, keine Snapshot-Versionserhöhung. **Attack-Move bleibt
ausdrücklich ausgespart** (Register- oder Formatänderung, eigener Sprint).

**Konsequenzen:** Erste Simulationsänderung seit D-077 mit Spielverhalten-
Wirkung. Die kanonischen Baselines (`DETERMINISM_10000`-Fingerprint
`0xF866FDC042D260E1`, Final-Hash `0xD8650F4DEDE1494C`) blieben **unverändert**
— die kanonische Partie enthält keinen Fall „bewaffnete Einheit ohne Befehl
neben sichtbarem Feind", sodass die neue Phase dort nie auslöst; die
Verhaltensänderung ist durch sechs neue Tests in beiden Lanes belegt
(Auto-Acquire, Fog-Gate, Nächstes-Ziel/Index-Tiebreak, gehaltene Order,
Plattform feuert, Unbewaffnete erfassen nie). Tools-Lane: 428/428.

---

### D-088 | verbindlich | Truppenführung: Formationsverteilung, Separation im Stand, Gebäude als Gelände

**Status:** verbindlich — Inhaberauftrag vom 2026-08-07
([hashkrieg/11_Sprint_Truppenfuehrung.md](hashkrieg/11_Sprint_Truppenfuehrung.md),
Leitsatz „eine Armee ist kein Haufen"), nach der ersten vollständig
gespielten Runde. Die drei Diagnosen des Sprints (eine Zielzelle für alle,
Separation nur in Bewegung, Gebäude ohne Wegfindungswirkung) waren im
Briefing bewiesen; die Umsetzungswahl unten ist **vom Agenten innerhalb des
Briefings entschieden** (das Briefing stellte Teil 2 ausdrücklich frei) und
damit überstimmbar.

**Entscheidung:**

1. **Formationsverteilung über geteiltes Flow-Ziel plus persönlicher
   Ankunftszelle.** `UnitState` bekommt `GoalGridPos` neben `TargetGridPos`
   (Entity-Store-Block **v5**; v1–v4 werden wie bisher hart abgelehnt): die
   Gruppe teilt sich genau **ein** Flow-Field, jede Einheit bekommt eine
   eigene Zielzelle — kleinster Entity-Index die Befehlszelle, die
   folgenden die expandierenden Chebyshev-Ringe in aufsteigender (y, x)-
   Reihenfolge (die Konvention der Spawn-Suche, kein float, keine
   Distanzsortierung). Derselbe Ring sucht freie Zellen, wenn das
   angeklickte Ziel selbst unbegehbar ist. Die Produktions-Spawn-Suche
   lehnt zusätzlich einheitenbelegte Zellen ab — frisch gebaute Truppen
   bilden eine Reihe statt eines Punkts. Verworfen: ein Flow-Field pro
   Einheit (Cache-Kapazität 32, Thrashing ab der ersten größeren Gruppe).
2. **Separation auch im Stand — Einschleifen-Variante.** Die
   Bewegungsschleife läuft für alle aktiven mobilen Einheiten; ohne
   Bewegungsbefehl ist der Flow-Anteil null und es wirkt nur eine
   **gedämpfte (0,5), gedeckelte (0,25 m/Tick) und mit Totzone belegte**
   Positionskorrektur ohne Rotationsänderung — Entstapeln statt Vibrieren.
   Exakte Überlappung (distanzlos) wird per Entity-Index-Tiebreak gelöst.
   Unbewegliche Entitäten (Gebäude, Baustellen: MoveSpeed 0) werden nie
   verschoben, wirken aber weiter als Hindernis. Kein Schritt betritt eine
   unbegehbare Zelle (Achsen-Fallback gegen Eckstau); ein nachträglich
   unbegehbar gewordenes Ziel gilt als erreicht, sobald keine begehbare
   Nachbarzelle näher liegt.
3. **Gebäude-Footprints ins Kostenfeld — mit neuem Epoch-Vertrag.** Das
   `ConstructionSystem` spiegelt jede Footprint-Änderung als
   Impassable/Open-Schreiben ins `CostField` (optionale Verdrahtung; die
   kanonischen Hosts verdrahten) und schiebt mobile Einheiten beim
   Platzieren aus dem Footprint (Ringsuche; Ziel im Footprint wird auf die
   Ausweichzelle umgesetzt). Zwei Vertragsänderungen im
   `PathfindingSystem`: (a) die serialisierte Epoch wird beim Restore
   **adoptiert statt verglichen** — mit dynamischen Footprints zählt sie
   Mutationshistorie, die ein Block-Restore nicht nachspielen kann; der
   Inhaltsbeweis ist strukturell (Construction-Block restauriert vor
   Pathfinding, Registrationsreihenfolge), und (b) eine Terrainänderung
   **regeneriert die gecachten Flow-Fields an Ort und Stelle** statt den
   Cache zu leeren — einmal pro Tick zusammengefasst, durch die
   Cache-Kapazität begrenzt; bewegte Einheiten verlieren dadurch nie die
   kostenbewusste Führung (kein Direct-Steering durch Wände).

**Konsequenzen:** Bewusst bezahlte Baseline-Neusetzung — SimRunner-Hash
`0x2FBEC31FBC0BF430` → **`0xB680C879DEA70B26`**, `DETERMINISM_10000`-
Fingerprint `0xF866FDC042D260E1` → **`0xAD8531312FE93F4B`** (Final-Hash
`0x6916A323202089A9`, Playback-Self-Check PASS). Der Epoch-Reject-Test
(CostFieldEpochSnapshotTests) wurde durch den Adopt-Vertrag ersetzt; neun
neue Truppenführung-Tests je Lane; Tools-Lane **438/438 grün**. Die
Skirmish-KI musste eine Folge lernen: ihr Bau-Laufziel „Westseite des
Footprints" kann seit (3) in einem Nachbargebäude liegen — Laufziele
werden jetzt footprint-frei gewählt. Attack-Move bleibt ausgespart
(Register-Änderung, siehe D-087).

---

### D-089 | verbindlich | Sprint 12 Strang A (TCP-Lockstep-Relay und Betriebspfad)

**Status:** verbindlich — vom Inhaber am 2026-08-07 freigegeben. Gilt für das
implementierte 1v1-Netzprofil; der lokale Pfad und der kanonische Sim-Replay-
Vertrag aus D-057 bleiben erhalten.

**Kontext:** D-033 legte deterministisches Lockstep über einen nicht
simulierenden Command-Relay als Ziel fest, enthielt aber einen nicht
implementierten UDP/RUDP-Entwurf und schrieb dem Server Ergebnisautorität zu.
Sprint 12 brauchte einen kleinen, nachweisbaren Zwei-Spieler-Pfad mit
zuverlässiger geordneter Zustellung, einem echten Barrier, einer ehrlichen
Aufzeichnung und einem betreibbaren Linux-Artefakt.

**Alternativen:** (a) den historischen Reliable-Ordered-Layer über UDP vor dem
ersten 1v1 bauen; (b) einen serverautoritativen State-Sync beziehungsweise eine
permanent serverseitige Simulation einführen; (c) das vorhandene kanonische
`ReplayFile` auch für den nicht simulierenden Relay verwenden; (d) **TCP für
den 1v1-Transport, Client-Simulation und ein getrenntes Relay-Transportformat.**

**Entscheidung:** (d), mit folgenden verbindlichen Grenzen:

1. **Transport und Autorität.** Der 1v1-Relay verwendet TCP und damit
   zuverlässige, geordnete Zustellung. Er simuliert keinen Gameplay-State und
   entscheidet kein Match-Ergebnis. Er vergibt genau zwei Slots, validiert
   Frame, Absenderslot, Command-Struktur, Tickfolge, angekündigte Counts,
   Dedupe und Kapazitätsgrenzen und verteilt die bestätigten Records.
   `TickComplete` ist ausschließlich ein Transport-/Barrier-Frame; er ist kein
   `CommandKind`, erreicht die Ingress nicht und steht weder im kanonischen
   Sim-Replay noch im State-Hash.
2. **Start- und Eingabevertrag.** Der kanonische lokale Defaultwert für den
   Input-Delay ist 1; `MatchConfig`/Loopback erlauben wie das Netzprofil 1 bis
   60. Das Netzprofil verwendet standardmäßig 3. Der Delay ist während der
   Session fest. Startbeweis und Fingerprint binden beide Peers an denselben
   Delay, Seed, Definitionshash und Initialsnapshot; erst danach geht der
   Client in `Running`. `ICommandTransport` bleibt unverändert. Daneben steht der
   optionale Vertrag `ICommandSubmissionReadiness`: Die Ingress prüft ihn vor
   Session-Aktion und Sequenzvergabe und antwortet bei fehlender Bereitschaft
   mit `Rejected`/`TransportNotReady`, ohne eine Sequenz zu verbrauchen. Der
   Relay-Transport ist nur in `Running` bereit.
3. **Barrier und Desync.** Jeder Client markiert seine lokale Completion selbst,
   nachdem seine lokalen Records in der Ingress liegen, und sendet
   `TickComplete` an den Relay; er wartet vor der Tickausführung nicht auf ein
   Echo der eigenen Completion. Die Completion des anderen Slots erreicht den
   Client erst, nachdem der Relay Tickfolge und exakte Record-Anzahl validiert
   und den Frame weitergeleitet hat. Der Client-Barrier öffnet mit lokaler
   Markierung plus vollständig eingetroffenen Remote-Records und validierter
   Remote-Completion. Bei aktivierter Aufzeichnung persistiert der Relay einen
   Tick erst, wenn er die Completions beider Slots bestätigt hat. Fehlende
   Vollständigkeit erzeugt Stall statt Spekulation. Alle 50 Ticks vergleichen
   die Clients über den Relay ihre State-Hashes; bei Abweichung endet die
   Session. Ein gespeicherter Desync-Hash muss exakt einem der beiden
   Peer-Hashes entsprechen, sonst ist die Evidenz ungültig.
4. **Getrennte Aufzeichnungsformate.** `ReplayFile` bleibt der kanonische
   Sim-Replay einschließlich deterministischer Resultcodes. Der nicht
   simulierende Relay kann diese Resultcodes nicht wahrheitsgemäß erzeugen und
   schreibt deshalb `NOVAREC2`: lückenlose Tickframes einschließlich leerer
   Ticks, serverseitig exakt geprüfte Counts/Dedupe/Caps, Checkpoints alle 50
   Ticks und einen terminalen Footer mit Reason sowie terminalem, persistiertem
   und letztem Checkpoint-Tick. Geschrieben wird zunächst `.partial`; nur eine
   vollständig versiegelte Aufnahme wird atomar als `.novarec` publiziert.
   Der Reader prüft Struktur und Footer; das engine-freie Playback verifiziert
   die gespeicherten Checkpoints und liefert den berechneten Endhash. Erst der
   Soak vergleicht diesen berechneten Wert mit dem Live-Endhash; der Footer
   selbst behauptet keinen Endhash.
   Relay-Aufnahme und Client-Diagnostik verwenden denselben 64-MiB-Höchstwert.
5. **Client-Diagnostik.** Der Client hält den Record-Strom als begrenzten
   On-Disk-Spool statt als dauerhaft wachsende Liste; damit sind mehr als
   65.536 Records möglich. Snapshot, Checkpoint-Identität und Recordstrom
   werden fail-closed geprüft und über `.partial` atomar publiziert.
   `NOVAREC1` und `NOVADIAG1` waren unveröffentlichte Wegwerfformate; es gibt
   keine Migration oder Kompatibilitätszusage.
6. **Spiel- und Betriebspfad.** `MatchConfig`, `MatchBootstrap` und
   `MatchRunner` tragen Seed, Slot, Fraktionen, AI-Slots, Delay und Transport;
   AI-Sessions entstehen nur für konfigurierte AI-Slots. Der Barrier sitzt in
   der Tickschleife, eine lokale Pause ist im Relay-Match gesperrt, und die UI
   liest den Netzwerklebenszyklus nur über Gameplay-Properties von
   `MatchRunner` und `MatchBootstrap`. Der
   `Nova.RelayServer` wird als self-contained `linux-x64`-Publish-Baum in einem
   doppelt gehashten Bundle geliefert. Konfiguration kommt ausschließlich aus
   der Umgebung; das Match-Token ist Pflicht, exakt 16 unpräfixierte
   Hexzeichen und ungleich null. systemd läuft unprivilegiert als
   `novarelay`, und `deploy.sh` aktiviert unveränderliche SHA-Releases mit
   atomaren `current`-/`previous`-Links sowie Readiness und Rollback.

**Verworfen:** UDP/RUDP jetzt — zusätzliche Zuverlässigkeits-, Ack- und
Retransmit-Fehlerklasse ohne Bedarf bei zwei Spielern und 10 Hz;
serverautorativer State-Sync — bricht die vorhandene deterministische
Client-Simulation; `TickComplete` als Command — würde das eingefrorene
Command-v1-Register und die Replay-/Hashgrenze verletzen; `ReplayFile` ohne
Resultcodes — würde eine Autorität vortäuschen, die der Relay nicht besitzt.

**Konsequenzen:** D-033 ist hinsichtlich UDP-Primärpfad und Ergebnisautorität
für dieses Profil teilweise ersetzt; seine fünf Grundregeln bleiben bestehen.
Der Post-Match-Trust-Anchor aus D-046 ist nicht Teil dieser Implementierung.
Ebenso nicht enthalten sind `MatchComplete`, Reconnect, UDP, Observer,
Matchmaking und mehr als zwei Spieler. A8 Stufe 1 belegt zwei echte TCP-Clients
über 10.023 Ticks, 50-Tick-Checkpoints und identische Live-/Playback-Endhashes.
Die manuelle Abnahme mit zwei Unity-Fenstern, im LAN und über den VPS steht aus;
es ist deshalb noch keine gespielte Netzwerkpartie oder vollständige
Sprint-Abnahme behauptet. Betrieb und Grenzen stehen in
[../tech/RelayServer.md](../tech/RelayServer.md).

---

### D-090 | verbindlich | Sprint 12 Strang B (fog-sicheres Gefechtsfeedback und Tier-0-Audio)

**Status:** verbindlich — vom Inhaber am 2026-08-08 mit dem Auftrag bestätigt,
Strang B vollständig umzusetzen und jede Abweichung vom Ausführungsplan
nachvollziehbar festzuhalten. Gilt ausschließlich für Präsentation, Audio und
Asset-Provenienz; Simulation, Netzwerkzustand, Replays, Fingerprints und
deterministische Baselines bleiben unverändert.

**Kontext:** Der Simulationskampf war funktional, aber visuell und akustisch
kaum lesbar. Die Simulation darf wegen Determinismus und Fog of War weder
Unity-Effekte auslösen noch verborgene Ereignisse an die Präsentation melden.
Der 12B-Plan enthielt außerdem widersprüchliche oder nicht ausführbare Details:
direkten `AudioSource.PlayOneShot`-Zugriff trotz D-039, einen unlaufenden
A/B-Effektschaltertest, eine zu vollständige Todeserkennung aus gepolltem
Zustand sowie Anforderungen an Trümmer, Texturen und einen Tier-1-Alarm ohne
gesicherte Quelle.

**Entscheidung:** Strang B wird als rein lesende, fog-sichere
Präsentationsschicht umgesetzt:

1. **Ereignisableitung.** `VisibleCombatFrameDiffer` liest ausschließlich die
   durch `FogOfWarSystem.GetVisibleEntities(viewerTeam)` freigegebene Menge und
   deren `TryGetUnit`-Snapshots; er mutiert weder Simulation noch Netzwerk.
   Derselbe Tick wird ignoriert, bei Tick-Rücklauf wird die Baseline verworfen.
   Nach einem Sprung über mehrere Ticks dürfen Zwischen-Cues verloren gehen;
   erfundene oder nachträglich aufgestaute Ereignisse sind ausdrücklich
   verboten.
2. **Bewusst unvollständige Todesheuristik.** Das Verschwinden einer eigenen,
   mobilen, nicht generischen Einheit darf als Tod gelten. Gebäude, Baustellen
   und fremde Einheiten gelten nur dann als Tod, wenn genau ein Tick vergangen
   ist und genau ein sichtbarer Schuss eindeutig korreliert; bei einem fremden
   Schützen muss dieser zum Betrachterteam gehören. Mehrdeutiges Verschwinden
   bleibt stumm. Diese Untererkennung ist der Preis dafür, Fog-Informationen
   nicht zu leaken.
3. **VFX-Vertrag.** Mündungsstoß, Treffer, Todesstoß, Rauch und Hitscan-Spur
   verwenden Unity-Bordmittel und Laufzeitmaterialien ohne importierte oder
   prozedural erzeugte Textur. Höchstens 64 Effekte und acht kurzlebige Lichter
   sind gleichzeitig aktiv. Der sichtbare Hitscan kopiert seinen Endpunkt beim
   Auslösen, läuft höchstens 0,1 s und folgt dem Ziel nicht. Über Budget wird
   verworfen, nicht aufgestaut. Die optionale Flipbook-Stufe 5 entfällt.
4. **Tod und Poolidentität.** Ein bestätigter Tod hält den bestehenden View
   0,8 s, löst Picking und Collider sofort und gibt danach exakt die gebundene
   Poolidentität frei; Slot-Wiederverwendung darf keinen Leichen-View erben.
   Gebäude erhalten Rauch und Absacken, aber keine persistente Trümmer- oder
   Decal-Fläche.
5. **Audio nach D-039.** Tier-0-One-Shots laufen ausschließlich über
   `IAudioService`/`UnityAudioService`; der Effektcontroller besitzt keine
   `AudioSource` und ruft `PlayOneShot` nicht direkt auf. Die zwölf Schlüssel
   sind `WPN_Kinetic_Light`, `WPN_Kinetic_Heavy`, `WPN_Explosive`,
   `IMP_Kinetic`, `IMP_Explosive`, `DTH_Unit`, `DTH_Building`, `UI_Click`,
   `UI_Select`, `UI_Ack`, `UI_Deny` und `PRD_UnitReady`.
   `DTH_Building` reserviert und startet Low-Frequency- plus Impact-Layer
   atomar. `ALR_BaseUnderAttack` bleibt Tier 1 und ist nicht Teil von 12B.
6. **Stimmen und Mischung.** Das Projektlimit bleibt 32 reale Stimmen; zwei
   sind für die vorhandenen Musikpfade reserviert, daher besitzt der neue
   One-Shot-Pool 30 Stimmen, davon höchstens 24 räumlich. Je Schlüssel gelten
   drei bis vier gleichzeitige Instanzen. Atomare Layer werden vollständig
   reserviert oder gar nicht gestartet. Bei Bedarf wird die älteste Stimme
   strikt niedrigerer Priorität gestohlen; es gibt keine Warteschlange.
   Weltklänge nutzen logarithmischen Rolloff von 15 bis 120 m. Der lineare
   Einstellungswert 0 wird als −80 dB abgebildet. Die angeforderte Priorität
   steuert auch `AudioSource.priority`.
7. **Legacy-Musik als Übergangsausnahme.** `MenuMusicPlayer` und
   `MusicDirector` behalten vorerst ihre vorhandenen `AudioSource`-Lebenszyklen
   und werden über den Music-Mixerbus geführt. Damit ist D-039 für den neuen
   Tier-0-One-Shot-Pfad erfüllt, aber noch nicht für alle historischen
   Musikpfade. Deren Migration bleibt eine ausdrücklich benannte Restschuld;
   die zwei reservierten Stimmen verhindern eine falsche 32-Stimmen-Zusage.
8. **Mixer und Listener.** `MIX_Master` enthält `Master` mit den Kindern
   `Music`, `SFX`, `Voice` und `Ambience`; Master-, Music-, SFX-, Voice- und
   Ambience-dB sind exponiert. Der vorhandene Listener bleibt an der Kamera.
   Ein Fokuspunkt-Listener ist eine spätere Gegenhörentscheidung. Die
   idempotente Editor-Autorisierung verwendet in Unity 6000.5.4f1 reflektierte
   interne Mixer-APIs und bricht bei fehlender Signatur hart ab; dies ist ein
   bewusstes versionssensitives Wartungsrisiko.
9. **Assets und Provenienz.** Importiert werden genau 35 unveränderte Kenney-
   OGGs in pack-first-Ablage: 11 Sci-Fi-, 11 Impact- und 13 Interface-Dateien.
   Die Quelldateinamen und SHA-256 bleiben erhalten. Je Pack gilt ein
   `PROVENANCE.json` mit `files[]`; im Tier-1-Zweierbetrieb bleibt
   `verifiedBy` mit begründeter Ausnahme leer. Die vier vorhandenen Suno-
   Musikdatensätze bleiben ausdrücklich `incomplete`: beim Menütrack fehlen
   ursprüngliche lokale MP3 und Konvertierungsbefehl, bei Ingame 01 privater
   Cover-Stamm und exakter Befehl, bei Ingame 02/03 jeweils der exakte
   Konvertierungsbefehl. Fehlende Belege werden nicht rekonstruiert oder
   erfunden.
10. **Determinismuswache.** Der geplante A/B-Hash-Test wird durch einen
    headless laufenden Quellcode-Guard ersetzt. Er scannt ausschließlich
    Produktionsquellen unter `Assets/_Project/Scripts/**` außerhalb
    `Simulation/**`, verbietet dort `GetUnitRef(` und fremde `.Random`-
    Memberzugriffe und erlaubt explizit `UnityEngine.Random`/`System.Random`.
    Tests werden nicht gescannt. `RawUnits` wird wegen bestehender Altlasten
    nicht global verboten; der neue Differ selbst bleibt nachweislich auf
    Fog-Sicht und `TryGetUnit` beschränkt.
11. **Weitere Planabweichungen.** Es gibt keinen separaten Effektschalter;
    Bloom/VFX und SFX werden über bestehende Präsentations- beziehungsweise
    SFX-Einstellungen geführt. Cooldowns, Gain und Prioritäten verwenden
    konservative Startwerte, weil der Plan keine auditiv abgenommenen
    Einzelwerte vorgab. Ihre endgültige Abstimmung bleibt Teil der manuellen
    Gefechtsabnahme.

**Konsequenzen:** Der Kampf erhält sicht- und hörbares Feedback, ohne einen
Simulationsvertrag oder eine Baseline zu ändern. Automatische Tests können
Budgets, Fog-Grenzen, Ereignisregeln, Poolidentität, Authoring und
Quellabhängigkeiten belegen; ob die Mischung mit etwa sechzig feuernden
Einheiten trägt und die Kamera als Listener gut klingt, kann nur eine gespielte
Gegenhör-/Sichtabnahme entscheiden. Bis diese gelaufen ist, wird Strang B als
technisch umgesetzt, nicht als vollständig spielerisch abgenommen bezeichnet.

---

### D-091 | verbindlich ab Merge | Sprint 13.0 (Tier 2, Source-available-Beiträge und PR-Schutz)

**Status:** Inhaberentscheidung vom 2026-08-08 (Dennis Westermann). Sie wird
mit dem Merge dieses Freigabe-PR wirksam. Michael Falk (`@travelhawk`) und Dennis
Westermann (`@cubetribe`) sind die einzigen Maintainer mit Merge-Zugang zu
`main`.

**Kontext:** Ein externer Beitragender arbeitet am Einheitenstrang. Das bisherige
Tier 1 setzt Vertrauen zwischen zwei Maintainers voraus und enthält weder einen
standardisierten Code-Lizenztext noch eine Regel, die externe Beiträge auf dem
aktuellen Commit an eine Maintainer-Freigabe und einen kommerziellen
Relizenzierungsweg bindet. Die vorhandene Branch Protection schützt `main`
bereits vor Direkt-Pushes und beschränkt den Zugang auf die beiden Maintainer,
hatte aber noch keine Required Checks für externe Review- und Baseline-Regeln.

**Alternativen:**

1. Tier 1 beibehalten und den externen PR vertagen — verworfen, weil die
   Zusammenarbeit bereits begonnen hat und Regeln erst vor dem ersten PR wirken.
2. Tier 2 mit einer proprietären Eigenlizenz und Copyright-Abtretung — verworfen,
   weil eine Eigenlizenz mehr Rechts- und Pflegeaufwand erzeugt und eine
   Abtretung für freiwillige Beiträge unnötig schwergewichtig ist.
3. Tier 2 mit MIT — verworfen, weil MIT kommerzielle Forks, Unterlizenzierung
   und Verkauf durch Dritte erlaubt und damit dem späteren Vermarktungsziel
   widerspricht.
4. **Tier 2 mit PolyForm Noncommercial 1.0.0 und einer nicht-exklusiven
   Contributor License Agreement (CLA)** — gewählt: Quellzugang, Änderungen und
   Weitergabe bleiben für nicht-kommerzielle Zwecke möglich; der Projektinhaber
   erhält für externe Beiträge mit dokumentierter CLA-Zustimmung ein
   gesondertes Recht zur kommerziellen Nutzung und Relizenzierung, ohne den
   Beitragenden das Copyright abzunehmen.

**Entscheidung:** Mit dem Merge dieses PR ist **Governance-Tier 2** aktiv.

1. Der unveränderte Text von `PolyForm-Noncommercial-1.0.0` liegt als
   [`LICENSE`](../../LICENSE) im Repository. Er gilt für originale
   Projekt-Quellen und -Dokumentation, deren jeweilige Rechteinhaber sie unter
   diesen Bedingungen freigegeben haben, soweit keine abweichende Datei- oder
   Drittanbieter-Lizenz gilt. Asset-, Schrift-, Audio- und Markenrechte bleiben
   ausdrücklich getrennt ([`NOTICE`](../../NOTICE),
   [Licenses.md](../assets/Licenses.md)).
2. Für jeden externen PR wird vor dem Merge die Zustimmung zur
   [`CONTRIBUTOR_LICENSE_AGREEMENT.md`](../../CONTRIBUTOR_LICENSE_AGREEMENT.md)
   dokumentiert.
   Der Beitragende behält sein Copyright; Dennis Westermann
   (`VibecodingGermany`) erhält ein dauerhaftes, weltweites, nicht-exklusives,
   gebührenfreies Recht zur kommerziellen Nutzung, Unterlizenzierung und
   Relizenzierung des Beitrags. Bestehende und nicht von einer dokumentierten
   CLA-Zustimmung erfasste Beiträge bleiben bei ihren jeweiligen
   Rechteinhabern; D-091 unterstellt weder eine rückwirkende CLA noch eine
   Copyright-Abtretung.
3. Nur `@cubetribe` und `@travelhawk` dürfen nach `main` mergen. Jeder PR eines
   jeden Autors braucht eine `APPROVED`-Review des jeweils anderen Maintainers
   auf dem aktuellen Head-Commit. Externe PRs brauchen zusätzlich die
   CLA-Zustimmung und den `external-contributor-review`-Check. Native
   Code-Owner-Review ordnet dafür jeden Pfad den beiden Maintainers zu.
4. Ein PR darf Simulationsverhalten und die vier benannten Golden-/Fingerprint-
   Baselines nicht zusammen ändern. Der einzige Ausnahmeweg ist das dokumentierte
   Maintainer-Label `baseline-reset-approved`.
5. `integrity` läuft auf jedem PR. Verträge und öffentliche Doku führen wieder
   Kopfversion und Änderungsverlauf; neue D-IDs dokumentieren mindestens drei
   Alternativen.

**Konsequenzen:** `CODEOWNERS` bindet jeden Pfad an die beiden Maintainers und
markiert die GitHub-Steuerfläche ausdrücklich, kann aber die fachliche
Schreibhoheit nicht abbilden; deren Quelle bleibt
[13-15_Parallelbetrieb.md](hashkrieg/13-15_Parallelbetrieb.md).
Die neuen Review- und Baseline-Jobs laufen als eng begrenzte, metadata-only
`pull_request_target`-Prüfungen aus dem geschützten Zielbranch, checken niemals
PR-Code aus und erhalten ausschließlich Leserechte. Sie werden erst nach ihrem
Merge und einem erfolgreichen Lauf in einem Folge-PR als Required Checks in die
Branch Protection aufgenommen. Nach einer neuen Freigabe wird der jüngste
`external-contributor-review`-Lauf erneut gestartet; die native Branch
Protection bleibt der maßgebliche Review-Schutz. Im selben Rollout wird eine
Code-Owner-Review auf jedem Pfad, eine erforderliche Freigabe und das Verwerfen
veralteter Freigaben aktiviert; bis dahin ist die bestehende Beschränkung auf
beide Maintainer unverändert wirksam.
Die verpflichtende Negativkontrolle mit einem absichtlich falschen PR steht
ebenfalls noch aus.

**Verworfen:** PolyForm Shield — erlaubt kommerzielle, nicht konkurrierende
Nutzung und erzeugt bei einem Spiel eine unscharfe Konkurrenzgrenze;
Copyright-Abtretung — weitergehender als nötig; Selbst-Merge außerhalb einer
Steuerfläche — lässt sich nicht mit verbindlicher nativer Code-Owner-Prüfung
kombinieren und bietet weniger Vier-Augen-Schutz.

---

### D-092 | verbindlich | Sprint 14 (Lobby-Vermittlung: Supabase Edge Functions, schlanker Client, Polling)

**Kontext:** Sprint 14 ersetzt das Abtelefonieren von Serveradresse, Match-Code und Slot durch eine Lobby: Match per kurzem Code anlegen und beitreten, Fraktionswahl, Bereitschaft, Build-Abgleich. Offen waren: Wo läuft die Vermittlungslogik, wie spricht der Client sie an, und wo liegt der Client-Code im Repo?
**Alternativen:** (a) Supabase Edge Functions (Deno) als einzige Serverlogik, dazu ein schlanker HTTPS-Client (HttpClient + Newtonsoft.Json) mit ~1,5-s-Polling, anon-Key im Client, Row-Level-Security deny-all auf den Tabellen; Client-Code engine-frei unter `Scripts/Networking/Lobby/`; (b) direkter PostgREST-/REST-Tabellenzugriff des Clients mit RLS-Policies; (c) Supabase Realtime (WebSocket-Subscriptions) statt Polling; (d) UnityWebRequest + JsonUtility in der Gameplay-/Präsentationsassembly; (e) externes supabase-csharp-SDK oder ein eigenes Vermittlungs-Backend.
**Entscheidung:** (a).
**Begründung:** Das Token-Minting (D-093) braucht ein serverseitiges Geheimnis — nur Functions können es halten; bei (b) läge das Secret im Client oder das Token ungeschützt in der Tabelle. RLS deny-all plus Service-Role-Key ausschliesslich in der Function-Umgebung minimiert die Angriffsfläche; der anon-Key ist per Sprint-Vorgabe kein Geheimnis, bleibt aber unkonfiguriert aus dem Repo (gitignorte `lobby-config.json` bzw. `NOVA_LOBBY_URL`/`NOVA_LOBBY_ANON_KEY`). Polling reicht für genau zwei Spieler; (c) brächte WebSocket-Client und Verbindungsmanagement ohne Mehrwert. Die engine-freie Platzierung macht den Client mit dem kanonischen `dotnet test` prüfbar (22 Tests gegen einen In-Prozess-Mock); (d) hätte ihn in die nur lokal laufenden Unity-EditMode-Tests verwiesen, und JsonUtility trägt die Vertrags-DTOs (verschachtelte Slot-Arrays, Null-Felder) nicht sauber. (e) ist eine schwere Abhängigkeit für fünf flache Endpunkte.
**Konsequenzen:** Neue Dependency `com.unity.nuget.newtonsoft-json` (Unity) bzw. `Newtonsoft.Json` (net8-Tests); `<Compile Remove>` im Relay-csproj, damit der Client nicht ins Relay-Binary wandert; Vertrag, Schema und Function-Referenzquelltexte in [../tech/LobbySupabase.md](../tech/LobbySupabase.md); Lobby-UI in `Presentation/UI`, Glue in `Gameplay` (Schreibhoheit Netzstrang, Assembly-Trennung Presentation.UI ↔ Networking bleibt gewahrt); keine Accounts und keine personenbezogenen Daten — vor dem ersten personenbeziehbaren Feld ist eine neue D-ID fällig. **Vom Agenten unter ausdrücklicher Inhaber-Delegation entschieden — überstimmbar.**

**Verworfen:** (b) PostgREST-Direktzugriff — kann das Minting-Secret nicht serverseitig halten; (c) Realtime — überdimensioniert für eine Zwei-Personen-Lobby; (d) UnityWebRequest in Gameplay — verliert die Headless-Testbarkeit des kanonischen Checks; (e) SDK/Eigenbackend — Abhängigkeit bzw. Betrieb ohne Verhältnis zum Umfang.

---

### D-093 | verbindlich | Sprint 14.5 (kurzlebige HMAC-Match-Tokens für den Relay)

**Kontext:** Der Relay verlangt seit A6 ein Match-Token — bisher ein statischer `ulong` aus `NOVA_MATCH_TOKEN` für die gesamte Prozesslebensdauer, per Anruf abgestimmt. Die Lobby soll pro Match ein kurzlebiges Token vergeben und beiden Clients übergeben, ohne dass der Relay neue Kanäle oder nennenswerten Zustand bekommt und ohne den Direktweg aus Sprint 13 zu brechen.
**Alternativen:** (a) strukturiertes 64-bit-Token [20 bit Expiry-Bucket à 5 min ‖ 12 bit Match-Id ‖ 32 bit HMAC-SHA256-Tag] gegen ein per Konfiguration geteiltes Secret; Relay validiert lokal, Match-Seed wird per HMAC aus dem Token abgeleitet, statisches Token bleibt für den Direktweg; (b) Token-Registry/-Datei am Relay, die die Lobby über einen neuen Provisionierungskanal (SSH/HTTP-Hook auf dem VPS) beschreibt; (c) Relay fragt bei jedem Hello aktiv per REST gegen Supabase (Pull-Validierung); (d) Protokolländerung: längeres signiertes Token mit `ProtocolVersion`-Bump.
**Entscheidung:** (a) — Richtung vom Inhaber im Sprint-Gespräch bestätigt, Ausformung vom Agenten unter Delegation, überstimmbar.
**Begründung:** (a) bricht das Protokoll nicht — Hello bleibt Version + `u64`, `ProtocolVersion` 1 unverändert — und braucht keinen Laufzeit-Kanal zwischen Lobby und Relay ((b)) und keine Kopplung des Relay an Supabase-Verfügbarkeit; (c) hätte zusätzlich den Service-Role-Key auf den Relay-Host gebracht und den Relay weniger „dumm" gemacht. (d) ist die sauberste Langform, sperrt aber alle vorhandenen Builds und widerspricht dem Sprint-Leitsatz, der Relay bleibe, was er ist. Der 32-bit-Tag reicht, weil der Relay Tokens nur im 5-Sekunden-Hello-Fenster prüft und ein Match pro Prozess bindet. Single-Use wird prozessintern über Match-Resets hinweg gehalten und per Expiry gepurgt; die Seed-Ableitung (`HMAC(secret, "NOVA-LOBBY-SEED-V1" ‖ token)`, ODER 1) macht die Lobby zur Seed-Quelle, ohne das Offer-Protokoll anzufassen — das Offer bleibt auf dem Draht autoritativ.
**Konsequenzen:** `LobbyToken.cs` (engine-frei, nur BCL) mit exakt dokumentierter, von der Deno-Function zu spiegelnder Serialisierung; `RelayServerCore` akzeptiert neben dem statischen Token validierte Lobby-Tokens (gleiches Token für beide Peers, Verbrauch nach Reset, abgeleiteter Seed vor dem Offer); neue optionale Env `NOVA_RELAY_TOKEN_SECRET` (64 Hex, fail-closed validiert, `NOVA_MATCH_TOKEN` bleibt Pflicht für den Direktweg); Doku in [../tech/RelayServer.md](../tech/RelayServer.md) 1.1.0 und [../tech/LobbySupabase.md](../tech/LobbySupabase.md); Tests inklusive Zwei-Client-Handshake über echten TCP-Relay. Einschränkungen (dokumentiert): ein gebundenes Token bleibt bis zum Match-Reset gebunden; Bucket-Wrap ca. 2036; die Lobby muss Match-Id-Kollisionen durch Neu-Minten vermeiden.

**Verworfen:** (b) Token-Datei — neuer Provisionierungskanal und VPS-Infra ausserhalb des Repos für ein Problem, das ein Shared Secret ohne Kanal löst; (c) Relay-Pull — Verfügbarkeitskopplung an Supabase und Service-Key auf dem Relay; (d) Protokolländerung — Versionsbruch ohne zwingenden Grund.

---

### D-094 | verbindlich | Sprint 14.4 (Build-Commit zur Laufzeit lesbar)

**Kontext:** Die Lobby soll ungleiche Builds schon beim Beitritt im Klartext erklären („Ihr habt unterschiedliche Versionen — hol dir Build `<commit>`"), bevor der Relay sie spät und technisch abweist (A4). Der Build-Commit steht bisher nur in `Info.plist`/`NovaBuildCommit.txt` der Pakete und wird zur Laufzeit nirgends gelesen.
**Alternativen:** (a) `IPreprocessBuildWithReport` in `Nova.Editor` schreibt `git rev-parse --short HEAD` (plus `-dirty`, analog `build-mac.sh`) vor jedem Player-Build in die gitignorte `Assets/_Project/Resources/NovaBuildCommit.txt`; Runtime-Reader `BuildInfo.Commit` mit Fallback `dev-editor`; (b) `Info.plist` bzw. plattformabhängige Paketdateien zur Laufzeit lesen; (c) eingecheckte generierte C#-Datei (`BuildInfo.g.cs`), die jeder Build neu erzeugt; (d) die Packaging-Skripte schreiben die Datei vor dem Unity-Aufruf.
**Entscheidung:** (a).
**Begründung:** (a) deckt alle Build-Wege (Skripte, Editor-Menü, künftige CI) plattformgleich ab, ohne die laufende Windows-Packaging-Arbeit in `tools/packaging/` zu berühren. (b) ist plattformspezifisch und fragil (Signatur-/Notarisierungsreihenfolge auf macOS, abweichende Ablagen auf Linux/Windows). (c) verschmutzt den Arbeitsbaum oder erzwingt Leer-Commits für einen Wert, der die Paketierung ohnehin kennt. (d) greift in fremde WIP ein und lässt Editor-Builds aussen vor. Der Fallback `dev-editor` hält Editor-gegen-Editor-Spiele ohne Konfiguration spielbar; zwei `dev-editor` gelten als gleicher Build und passieren den Lobby-Abgleich — als dokumentierter Entwicklungsweg, nicht als Schutz.
**Konsequenzen:** `BuildInfo.Commit` in `Gameplay/Match`, `BuildCommitStamp` in `Assets/_Project/Editor/`, `.gitignore`-Einträge für die Stempel-Datei; die Lobby vergleicht den Commit beim Beitritt (409 `build_mismatch` mit beiden Commits im Klartext, Vertrag in [../tech/LobbySupabase.md](../tech/LobbySupabase.md)); der Relay-Fingerprint (A4) bleibt die letzte, technische Sperre unverändert dahinter. **Vom Agenten unter ausdrücklicher Inhaber-Delegation entschieden — überstimmbar.**

**Verworfen:** (b) Plattformdatei-Lese — fragil und dreifach zu pflegen; (c) generierte eingecheckte Quelle — Tree-Dirt bzw. Leer-Commits; (d) Skript-Stempel — kollidiert mit laufender Arbeit an `tools/packaging/` und erfasst Editor-Builds nicht.

---

### D-095 | verbindlich | Sprint 16 (Parallelbetrieb über Dateihoheit statt Verhaltensraum)

**Status:** Inhaberentscheidung vom 2026-08-09. Sie ist in
[13-15_Parallelbetrieb.md](hashkrieg/13-15_Parallelbetrieb.md) Fassung 1.3.0
eingearbeitet; der überholte Wortlaut steht dort weiterhin zitiert, damit
nachvollziehbar bleibt, wovon abgewichen wurde.

**Kontext:** Die bisherige Regel trennte Netzstrang und externen Einheitenstrang
über den *Verhaltensraum*: Sprints 13–15 fassen gar keine Datei unter
`Scripts/Simulation/` an, und die gesamte Simulation gehört für ihre Dauer dem
Einheitenstrang. Der erste Betatest hat sechs Fehler gemeldet, die genau in
`Construction/`, `Economy/` und `Production/` liegen — Ordner, die unter dieser
Regel kein Strang bearbeiten durfte. Zusätzlich lag `Scripts/Simulation/State/`
vollständig im Frost („niemand ohne D-ID"), obwohl zwei der Befunde (#43, #45)
einen schreibenden Zugriff auf den Einheitenzustand brauchen. Zugleich stand
Strang C aus [12_Sprint_Zu_Zweit.md](hashkrieg/12_Sprint_Zu_Zweit.md)
(Knappheit, Lager, Radar, Low Power, Bauvoraussetzungen, Platzierung) hinter dem
Netzstrang in der Warteschlange.

**Alternativen:**

1. Bei der alten Regel bleiben und Sprint 16 hinter Sprint 15 lassen —
   verworfen: sechs Betatest-Befunde liegen in `Construction/`, `Economy/` und
   `Production/` und blieben damit bis nach dem Netzstrang unbearbeitet. Jede
   weitere Testrunde meldet dieselben Fehler erneut.
2. Sprint 16 dem externen Einheitenstrang übergeben — verworfen: der Strang hat
   eine eigene, fortlaufende Paketliste (B1–B5), und Wirtschaft ist ausdrücklich
   nicht sein Gegenstand. Außerdem läge damit die gesamte Simulation bei einem
   externen Beitragenden.
3. Nur die Betatest-Fehler herausziehen und Strang C weiter verschieben —
   verworfen: dieselben Ordner würden zweimal geöffnet, und die Fehler #53
   (Lager) und #54 (Radar) sind ohne die Wirtschaftsmechanik gar nicht behebbar.
4. **Gewählt: Trennung über Dateihoheit, Sprint 16 parallel zu 13B.** Preis:
   beide Stränge bewegen ab jetzt Determinismus-Baselines. Deshalb wird das
   Merge-Fenster auf **einen Strang je Fenster** verschärft.

Für den Frost auf `Scripts/Simulation/State/` zusätzlich geprüft:

5. Den Frost unverändert lassen — verworfen: dann bleiben #43 und #45
   unbehebbar, weil der nötige Zustandszugriff keinem Strang erlaubt wäre.
6. Den Frost ganz aufheben — verworfen: das Zustandslayout ist genau das, was
   Snapshots und Replays unlesbar macht.
7. **Gewählt: Layout bleibt eingefroren, die Befehlsanwendung folgt dem
   Eigentümer des jeweiligen Befehls.**

**Entscheidung:**

1. Die Trennung des Parallelbetriebs ist ab sofort ausschließlich die
   Schreibhoheitstabelle in
   [13-15_Parallelbetrieb.md](hashkrieg/13-15_Parallelbetrieb.md). Sie ist
   vollständig; jeder Pfad unter `Assets/_Project/Scripts/` hat genau einen
   Eigentümer, und ein unzugeordneter Pfad ist ein Fehler im Dokument, kein
   Freiraum. `Construction/`, `Economy/` und `Production/` gehören dem
   Netzstrang und sind ab Sprint 16 aktiv statt gesperrt.
2. **Sprint 16 läuft parallel zu 13B**, nicht dahinter. Er führt Strang C aus
   Sprint 12 und die Betatest-Befunde im selben Schreibbereich zusammen.
3. `Scripts/Simulation/State/` zerfällt in zwei Teile. **Layout und
   Serialisierung** — Feldbestand, Feldreihenfolge, `StateVersion`, Blockformat
   — bleiben eingefroren und brauchen weiterhin eine eigene D-ID. Die
   **Befehlsanwendung** (`UnitCommandStateView`) gehört dem Netzstrang. Die
   Trennlinie verläuft nicht zwischen Befehlsarten, sondern zwischen
   **Zielsetzung und Ausführung**: Was ein Befehl in den Zustand **schreibt**,
   gehört dem Netzstrang — `Stop` löscht `UnitState.AttackTarget`, `Move`
   verteilt Zielzellen, `Harvest` setzt `HarvestFieldId`,
   `CommandKind.AttackTarget` schreibt das Ziel.
   Wie eine Einheit daraufhin **fährt und schießt**, gehört dem Einheitenstrang
   — `MovementSystem` fährt zur Zielzelle, `CombatSystem` erfasst und feuert.
   Präzedenz ist D-088: die Formationsverteilung in `ApplyMove` hat das
   Maintainer-Team gebaut, während `MovementSystem` beim Einheitenstrang lag.
4. **Kein neuer `CommandKind`.** Das Register `Simulation/CommandsV1/` bleibt
   eingefroren; kein Paket aus Sprint 16 oder 18 braucht einen neuen Befehlstyp.
5. Ein Merge-Fenster nimmt die PRs **eines** Strangs auf, prüft die Baselines
   und öffnet dann das nächste.

**Begründung:** Dateikonflikte verhindert die Schreibhoheitstabelle, nicht die
Verhaltensregel — die Tabelle ist disjunkt, und daran ändert Sprint 16 nichts.
Was die Verhaltensregel zusätzlich leistete, war ein Determinismus-Argument:
solange nur ein Strang die Simulation bewegt, ist ein roter Baseline-Test
eindeutig zuordenbar. Genau diese Zuordenbarkeit stellt das Ein-Strang-Fenster
wieder her, ohne einen ganzen Ordnerbaum stillzulegen. Für `State/` gilt die
gleiche Unterscheidung eine Ebene tiefer: *was* ein `CommandKind` mit dem
Zustand tut, ist Verhalten und lässt sich testen; *wie* der Zustand auf Bytes
abgebildet wird, ist Format und entwertet bei jeder Änderung vorhandene
Snapshots und Replays. Nur das Zweite muss eingefroren bleiben.

**Konsequenzen:** [13-15_Parallelbetrieb.md](hashkrieg/13-15_Parallelbetrieb.md)
steht auf 1.3.0 und gilt für 13–18 statt 13–15. Zwei Vertragsflächen sind dort
ergänzt: der `WeaponProfiles`-Slot `UnitRole.Unit`, den Baustellen heute tragen,
und `UnitState.AttackTarget`. Die Regel „Verhalten und Baseline nie im selben
PR" wird damit zur wichtigsten Regel des Dokuments, nicht zur zweitwichtigsten,
denn ab jetzt können beide Stränge Baselines bewegen. Verteilte Testbuilds
altern schneller, weil zwei Quellen den Fingerprint bewegen. Sprint 16 und
Sprint 18 führen eigene, gegen 13B disjunkte Schreibhoheitstabellen.

**Wortlautberichtigung 2026-08-09:** Punkt 3 teilte die Befehlsanwendung
zunächst nach Befehlsarten auf („Wirtschafts- und Baubefehle dem Netzstrang,
Bewegungs- und Kampfbefehle dem Einheitenstrang"). Das widersprach dem
Regelwerk, das `Move` und `Stop` ausdrücklich dem Netzstrang zuschlägt, und es
ist am Code nicht durchführbar: `UnitCommandStateView.Apply` behandelt Move,
Stop, AttackTarget, Harvest, Bau und Produktion in einer einzigen Datei. Der
Wortlaut ist auf die Linie zwischen Zielsetzung und Ausführung nachgezogen. Die
Entscheidung selbst ist unverändert — deshalb keine neue D-ID.

---

### D-096 | verbindlich | Sprint 16 (Lager mit abgeleiteter AE-Obergrenze, Radar schaltet die Minimap frei)

**Status:** Inhaberentscheidung vom 2026-08-09 (Richtung); die Ausformung in
Paketen liegt beim Agenten. Umgesetzt in
[16_Sprint_Wirtschaft.md](hashkrieg/16_Sprint_Wirtschaft.md) 16.4 und 16.5.

**Kontext:** Lager und Radar stehen im MVP-Content-Manifest, sind als Asset
fertig und im Baumenü wählbar — und tun im Code nichts. `AddCredits` addiert
bedingungslos in ein `long`; `UnitRole.Storage` hat außer `PowerRequired 5` kein
Verhalten. Kein Simulationscode liest `UnitRole.Radar`;
`FogOfWarSystem.GetRadarSignatures` hat null Produktionsaufrufer und
multipliziert überdies die `SightRadius` **jeder** eigenen Entität, statt von
einem Gebäude auszugehen. `MinimapHud` zeichnet bedingungslos. Der erste
Betatest hat beides gemeldet (#53, #54). D-024 hat die Silo-Logik bereits
entschieden, aber nicht, wie sie technisch getragen wird.

**Alternativen für das Lager:**

1. Kapazität als neues Feld im Wirtschaftszustand — verworfen: das bumpt
   `EconomySystem.StateVersion`, `TryParseState` lehnt danach jede ältere
   Fassung ab, alle vorhandenen Snapshots und Replays wären wertlos, und ob
   `MatchFingerprint.StateSchemaVersionV1` nachziehen muss, wäre eine eigene
   Inhaberentscheidung.
2. Das Lager ersatzlos streichen — verworfen: es steht im MVP-Content-Manifest
   und ist als Asset fertig. Ein gestrichenes Gebäude ist teurer als ein
   wirksames.
3. Dem Lager eine andere Wirkung geben, etwa auf die Ernterate — verworfen: das
   verschiebt die Wirkung in die Ökonomie, ohne den eigentlichen Fehler zu
   beheben. Das Konto hat keine Obergrenze, also gibt es keinen Ausgabenanreiz.
4. **Gewählt: eine aus dem Gebäudebestand abgeleitete Obergrenze.** Kein neues
   Feld, kein Formatbruch.

**Alternativen für das Radar:**

5. Das Radar streichen — verworfen, gleiche Begründung wie beim Lager.
6. Das Radar gibt nur zusätzliche Sichtweite, ohne an der Minimap zu hängen —
   verworfen: die Minimap ist heute immer sichtbar, das Gebäude bliebe damit für
   den Spieler wirkungslos.
7. **Gewählt: das Radar schaltet die Minimap frei und leitet seine Abdeckung vom
   Gebäude ab.**

**Entscheidung:**

1. **AE-Obergrenze:** HQ 2.000 AE Basis, +2.000 je Lager, Überschuss verfällt,
   25 % Verlust bei Zerstörung. Das setzt D-024 um und ergänzt die dort offen
   gelassene Basis-Kapazität.
2. **Die Kapazität wird aus dem Gebäudebestand abgeleitet, nicht gespeichert.**
   Kein neues Feld im Wirtschaftszustand, kein `StateVersion`-Bump.
3. **Die Minimap hängt am Radar.** `MinimapHud` zeichnet nur noch, wenn der
   lokale Slot ein fertiges Radar besitzt; die Abfrage
   `ConstructionSystem.HasFinishedBuilding(slot, UnitRole.Radar)` existiert
   bereits.
4. **Die Radar-Abdeckung kommt vom Gebäude**, nicht von der `SightRadius` jeder
   Entität. `GetRadarSignatures` wird damit erstmals von der Präsentation
   konsumiert.
5. `FogOfWarSystem.GetTeamView` bleibt unverändert — es ist Vertragsfläche des
   Einheitenstrangs.
6. Der Verlust einer bisher immer vorhandenen Minimap ist beabsichtigt und wird
   am Bauknopf des Radars im Klartext erklärt.

**Begründung:** Ein Gebäude, das Strom zieht und nichts tut, ist kein
Platzhalter, sondern ein Schaden — es kostet den Spieler Aetherium und Energie
und gibt nichts zurück. Beide Wirkungen sind ohne Formatbruch erreichbar, und
genau daran hängt die Entscheidung: die abgeleitete Obergrenze kostet etwas
Rechenzeit pro Abfrage, ein gespeichertes Feld kostet jeden Snapshot und jedes
Replay im Bestand. Die Kopplung der Minimap an das Radar ist zugleich die
einzige Variante, die das Gebäude für den Spieler spürbar macht, ohne eine neue
Anzeige zu erfinden.

**Konsequenzen:** `MatchFingerprint.StateSchemaVersionV1` bleibt unberührt,
vorhandene Snapshots und Replays bleiben lesbar. `AddCredits` hat vier Aufrufer
— Abladen, Streichung, Abbruch und Verkauf —, alle im Schreibbereich des
Netzstrangs. Erst zusammen mit der Low-Power-Abschaltreihenfolge (16.6) wird die
Kopplung zur Waffe: ein zerstörtes Kraftwerk nimmt Radar, Verteidigung und damit
die Minimap mit. Dass die Sperre der Minimap zunächst als Rückschritt gelesen
wird, ist einkalkuliert; der Befund geht bewusst in die nächste Testrunde.
Nachweisbar ist die Sache nur in einer gespielten Runde — `Presentation/` ist in
keinem CI-Testlauf enthalten.

---

### D-097 | verbindlich | Sprint 16 (Stoppen löscht den Angriffsbefehl, Halte-Feuer bleibt beim Einheitenstrang)

**Status:** Inhaberentscheidung vom 2026-08-09. Umgesetzt in
[16_Sprint_Wirtschaft.md](hashkrieg/16_Sprint_Wirtschaft.md) 16.10.

**Kontext:** `UnitCommandStateView` räumt bei `CommandKind.Stop` heute Bewegung,
Ernte und Reparatur ab — den Angriff nicht. `UnitState.AttackTarget` bleibt
stehen, und die Einheit feuert weiter. Der erste Betatest (T-01) hat das als
#45 gemeldet: der Knopf stoppte nichts, was der Tester stoppen wollte.
`AttackTarget` gehört dem Einheitenstrang (`CombatSystem`); die Befehlsanwendung
gehört seit D-095 dem Eigentümer des jeweiligen Befehls.

**Alternativen:**

1. „Stoppen" bekommt ein echtes Halte-Feuer — verworfen: ein Haltezustand gehört
   in `Simulation/Combat/`, das exklusiv dem externen Einheitenstrang gehört.
   Außerdem setzt die Auto-Zielerfassung aus D-087 im nächsten Tick ohnehin ein
   neues Ziel, solange kein Haltezustand existiert.
2. „Stoppen" unverändert lassen — verworfen: der Knopf löscht heute Bewegung,
   Ernte und Reparatur, aber nicht den Angriff. Der Befund ist berechtigt.
3. Den Knopf entfernen — verworfen: er wird in jedem RTS erwartet, und seine
   Wirkung auf Bewegung und Ernte ist heute schon korrekt.
4. **Gewählt: „Stoppen" löscht zusätzlich `AttackTarget`; das Halte-Feuer wird
   als Befund an den Einheitenstrang gegeben.**

**Entscheidung:**

1. `UnitCommandStateView` räumt bei `CommandKind.Stop` zusätzlich
   `UnitState.AttackTarget` ab. Kein neues Feld, kein neuer `CommandKind`.
2. Ein echtes „Feuer einstellen" ist **kein** Netzstrang-Paket. Es braucht einen
   Haltezustand in `Simulation/Combat/` und geht als Befund an
   [13B](hashkrieg/13B_Sprint_Einheitenverhalten.md).
3. In der Schreibhoheitstabelle steht `UnitState.AttackTarget` als
   Vertragsfläche: das Feld **löschen** darf der Netzstrang, eine Regel darüber,
   *wann automatisch neu erfasst wird*, gehört dem Einheitenstrang.

**Begründung:** Die Grenze verläuft zwischen „ein Feld räumen" und „eine Regel
über das Wiederbelegen dieses Feldes aufstellen". Das Erste ist Befehlsanwendung
und damit nach D-095 beim Eigentümer des Befehls; das Zweite ist Kampfverhalten
und gehört zu D-087. Der halbe Schritt ist trotzdem der richtige: Ein Angriff,
der nach dem Stopp-Befehl unverändert weiterläuft, ist ein anderer Fehler als
eine Einheit, die nach dem Stopp erneut selbst ein Ziel erfasst. Der erste ist
mit einer Zeile in der Befehlsanwendung behoben, der zweite braucht fremdes
Terrain.

**Konsequenzen:** Aus Spielersicht bleibt „Stoppen" unvollständig, solange die
Auto-Zielerfassung greift — das ist benannt, nicht behoben, und steht in Sprint
16 wie in [Sprint 18](hashkrieg/18_Sprint_Befehl_und_Auswahl.md) ausdrücklich
unter „bewusst nicht in diesem Sprint". Der Einheitenstrang erhält den Befund
zum Halte-Feuer als Eingang, nicht als Auftrag. Ein Nachweis ist nur in einer
gespielten Runde möglich.

---

### D-098 | ENTWURF — Inhaberentscheidung ausstehend | Sprint 17 (Tier-3-Auslöser präzisiert)

**Status:** **ENTWURF.** Ein Tier-Wechsel ist laut
[GOVERNANCE.md](../../GOVERNANCE.md) ausdrücklich eine Inhaberentscheidung, und
der Inhaber hat sie am 2026-08-09 nicht getroffen — er hat die Formalitäten für
die geschlossene Beta vertagt. Der Entwurf steht hier fertig begründet, damit er
nur noch bestätigt werden muss; die Tier-Tabelle in GOVERNANCE.md bleibt bis
dahin **unverändert**.

> **Nichts im Großauftrag vom 2026-08-09 hängt daran.** Die Blöcke 0 bis 4
> laufen unter Tier 2 weiter, gleich wie diese Frage ausgeht. Sie ist ein
> Vorrat, kein Blocker.

Gegenstand ist die Tier-Tabelle in [GOVERNANCE.md](../../GOVERNANCE.md); der
Anlass steht in
[17_Sprint_Zugangsprotokoll.md](hashkrieg/17_Sprint_Zugangsprotokoll.md),
Abschnitt „Governance: die Tier-Frage".

**Kontext:** Die Tier-Tabelle nennt „Nutzerdaten im Spiel" als Auslöser für
Tier 3, und Sprint 14 hält in seiner Risikotabelle fest: „Vor dem ersten Feld,
das es wäre, D-ID." Sprint 17 protokolliert Herkunfts-IP und Geräte-Kennung —
personenbezogene Daten. Nach dem Buchstaben weckt damit ein reines
Betriebsprotokoll die unter D-076 schlafend gelegte Gate-Kette, obwohl der
Kreis der Spielenden geschlossen ist, kein Build verkauft wird und es kein
Publikum gibt.

**Alternativen:**

1. Den Auslöser buchstabengetreu lesen und die Kette wecken — verworfen: Der
   Tier-3-Apparat beantwortet die Frage „können wir es Dritten beweisen".
   Betriebsprotokolle werfen diese Frage nicht auf. Sie werfen
   Datenschutzfragen auf, und die beantwortet man mit Datensparsamkeit und
   Löschfristen, nicht mit einer Nachweiskette.
2. Sprint 17 zurückstellen, bis die Tier-Frage anderweitig geklärt ist —
   verworfen: Seit Sprint 14 nimmt ein Server von uns Verbindungen fremder
   Rechner entgegen, ohne Protokoll und ohne Handbremse. Das Problem wartet
   nicht auf eine Governance-Debatte.
3. Den datenbezogenen Auslöser ersatzlos streichen — verworfen: Dann fiele auch
   der Fall weg, für den er gedacht war (Konten, Zahlungsdaten, echtes
   Publikum), und Tier 3 hinge allein an Vertragsereignissen.
4. **Gewählt: den Auslöser präzisieren statt die Kette wecken.**

**Entscheidung:**

1. Tier 3 hängt an **Veröffentlichung, Geld und Publikum** — Steam-Seite,
   bezahlter Build, Publisher-Vertrag. Nicht an jeder personenbezogenen
   Verarbeitung.
2. **Betriebs- und Missbrauchsdaten mit Löschfristen bleiben Tier 2.** Für die
   geschlossene Beta gilt das auch mit der Klartext-Adresse aus D-099: Der
   Kreis ist geschlossen, die Frist läuft automatisch als `pg_cron`-Job, und
   die Umstellung auf Hashing steht als Q-041 im
   [Fragenkatalog](OpenQuestions.md).
3. Umzusetzen ist das in der Tier-Tabelle in `GOVERNANCE.md` (Zeile „Auslöser")
   plus einem Satz zur Abgrenzung. Die Datei ist ein Hot-File — serialisiert,
   ein Schreiber.

**Begründung:** Ein Auslöser, der bei jeder personenbezogenen Verarbeitung
greift, verwechselt zwei verschiedene Fragen. Die eine ist die Beweisfrage
gegenüber Dritten — dafür wurde der Tier-3-Apparat gebaut. Die andere ist die
Datenschutzfrage, und deren Werkzeuge sind Zweckbindung, Sparsamkeit und
Fristen. Der bisherige Wortlaut hätte den teuren Apparat für die billige Frage
angeworfen und damit beide Antworten verschlechtert: die Kette wäre für
Protokollzeilen erwacht, und die Löschfrist wäre trotzdem noch nötig gewesen.

**Konsequenzen:** `GOVERNANCE.md` trägt in der Zeile „Auslöser" heute noch
„Nutzerdaten im Spiel" und ist nachzuziehen — serialisiert, nicht parallel zu
einem anderen Schreiber. Die Gate-Kette bleibt schlafend; ihr Weckpfad aus
D-076 ist unberührt. Öffnet sich der Beta-Kreis, ist zusammen mit Q-041 auch
diese Abgrenzung erneut zu prüfen: Publikum ist genau eines der drei Merkmale,
an denen Tier 3 künftig hängt.

---

### D-099 | verbindlich | Sprint 17 (Identitätsmodell und Klartext-IP für die geschlossene Beta)

**Status:** Inhaberentscheidung vom 2026-08-09 (Richtung); die Ausformung in
Paketen liegt beim Agenten. Umgesetzt in
[17_Sprint_Zugangsprotokoll.md](hashkrieg/17_Sprint_Zugangsprotokoll.md) 17.1
bis 17.5.

**Kontext:** Das Zugriffsprotokoll braucht ein Merkmal, an dem sich Wiederkehr,
Häufung und Missbrauch ablesen lassen. Die Merkmalsprüfung in Sprint 17 fällt
nüchtern aus: Die **MAC-Adresse** endet am ersten Router, weder Lobby noch
Relay sehen sie jemals; meldet der Client sie selbst, ist sie eine
Selbstauskunft, die unter Windows in einer halben Minute geändert ist. Die
**IP-Adresse** sieht der Server ohnehin — das einzige Merkmal, das der Client
nicht fälschen kann —, aber sie wechselt und ist hinter CGNAT geteilt. Eine
**Installationskennung** erkennt Wiederkehrer, solange niemand sie löscht; der
**Geräte-Anker** überlebt das Löschen als zweite Spur. Der harte Anker (Konto,
Lizenz, Steam-ID nach D-007) kommt erst mit dem Verkauf. Die frühere Vorgabe
lautete „keine rohe IP in der Datenbank".

**Alternativen:**

1. `HMAC(pepper, ip)` — Wiederkehr bliebe erkennbar, und ein Datenbankleck gäbe
   keine Adressliste her. Verworfen: Die Betreiberfrage „wer spielt da" bleibt
   unbeantwortet. Der Hash zeigt, *dass* jemand wiederkommt, nicht *wer* — im
   geschlossenen Kreis der falsche Tausch.
2. Nur das gekürzte Netzpräfix speichern, keine Adresse — die sparsamste
   Variante, kein Rohwert. Verworfen: Sie trennt zwei Anschlüsse im selben
   `/24` nicht, und damit verliert die Einzelsperre aus 17.2 ihre Grundlage.
3. Die MAC-Adresse als Kennung erheben — verworfen: Sie überquert kein NAT und
   ist als Selbstauskunft trivial fälschbar. Ein Merkmal, das nur der Ehrliche
   korrekt meldet, taugt nicht zur Missbrauchsabwehr.
4. **Gewählt: Klartext-Adresse plus gekürztes Präfix, begrenzt durch eine
   Löschfrist statt durch einen Hash.**

**Entscheidung:**

1. **Für die geschlossene Beta wird die Herkunfts-IP im Klartext gespeichert**,
   dazu das gekürzte Netzpräfix (`/24` bei IPv4, `/48` bei IPv6). Die frühere
   Vorgabe „keine rohe IP in der Datenbank" gilt für diese Phase nicht.
2. Die Begrenzung, die an die Stelle des Hashes tritt, ist die **Löschfrist von
   30 Tagen**. Sie hängt an einem `pg_cron`-Job, nicht an Erinnerung.
3. **Installationskennung und Geräte-Anker bleiben serverseitig gepeppert
   gehasht**; die Rohwerte werden nirgends gespeichert. Der Pepper liegt
   ausschließlich in der Function-Umgebung, wie das Relay-Token-Geheimnis.
4. **Die MAC-Adresse wird ausdrücklich verworfen** und in keiner Ausbaustufe
   erhoben.
5. **IP- und Präfixsperren sind immer befristet** — Adresse höchstens 30 Tage,
   Netz höchstens 7 Tage, erzwungen per Datenbank-Constraint. Nur Sperren auf
   die Installationskennung dürfen unbefristet sein.
6. **Vor Öffnung der Beta ist neu zu entscheiden.** Die Umstellung auf Hashing
   steht als Q-041 im [Fragenkatalog](OpenQuestions.md) und ist vor der
   Öffnung zu beantworten, nicht danach.

**Begründung:** Der Zweck des Protokolls entscheidet über seine Form. Gebaut
wird es, damit der Betreiber eines geschlossenen Kreises sieht, wer spielt —
und genau diese Abfrage macht ein nicht umkehrbarer Hash unmöglich. Was der
Hash schützen soll, schützt hier die Frist: Nach dreißig Tagen ist die Zeile
weg, ohne dass jemand daran denken muss. Bei den Sperren dreht sich das
Argument um: Hinter einer CGNAT-Adresse sitzen Unbeteiligte, und eine
dynamische Adresse gehört morgen jemand anderem — eine unbefristete IP-Sperre
sperrt mit Sicherheit irgendwann den Falschen aus. Die nüchterne Grenze des
ganzen Modells ist benannt: Auf einem Rechner, den der Gegner kontrolliert,
gibt es keine unfälschbare Identität. Geliefert werden Reibung und
Sichtbarkeit, nicht Sicherheit.

**Konsequenzen:** `access_log` führt die Adresse als Klartext-Feld neben dem
Präfix; eine Stichprobe darf trotzdem keine rohe Installationskennung
enthalten. Die Löschfristen sind Teil der Abnahme, nicht Betriebsroutine — ein
Lauf des Jobs muss nachweislich gelöscht haben. Geht der Pepper verloren,
werden alle Hashes unbrauchbar und die Zuordnung ist weg; er gehört in dieselbe
Sicherung wie das Relay-Token-Geheimnis. Der harte Anker kommt mit dem
Vertriebsweg und setzt sich später neben `install_hash`, ohne Tabellen,
Sperrarten oder Bedienweg zu ändern. Q-041 bleibt bis zur Öffnung der Beta
offen.

---

### D-101 | verbindlich | Sprint 16 (der Ausgangspin der kanonischen KI-Partie wird vom Identitätspin getrennt)

**Status:** Inhaberentscheidung vom 2026-08-09, umgesetzt im selben Zug.

**Kontext:** `SkirmishAiTests.AiBehaviorId_TracksWhatTheAiActuallyDoes` pinnte
drei Werte in einer Zusicherung: die KI-Kennung `AiBehaviorId.Value`, den
Entscheidungstick der kanonischen Partie und deren Endzustands-Hash. Die
Kopplung war beabsichtigt — der Kommentar begründet sie damit, dass die Kennung
geänderte Zahlen fängt und der Endzustand geänderte Regeln.

Der erste Wirtschaftssprint hat gezeigt, dass die Kopplung an der falschen Naht
liegt: Entscheidungstick und Endzustand bewegen sich bei **jeder** Änderung an
der Simulation, in der die KI spielt. Paket 16.1 verschob den Endzustand, 16.2
zusätzlich den Tick — beide fassen keine Zeile KI-Code an. Die im Test
hinterlegte Prozedur schickte den Netzstrang damit in Arns Verhaltensjournal,
also in das falsche Buch. Der Test liegt zudem in
`tools/Nova.SimRunner.Tests/`, einem Verzeichnis, das in der
Schreibhoheitstabelle gar keinen Eigentümer hatte.

**Alternativen:**

1. **Arn zieht den Pin nach jedem Merge des Netzstrangs nach.** Verworfen: bei
   zehn Paketen in Sprint 16 sind das zehn Runden Ping-Pong über einen
   Fork-PR-Weg, und jede blockiert einen fertigen Maintainer-PR an einer
   Zahl, die niemand aus seinem Strang bewegt hat.
2. **Der Netzstrang aktualisiert die Zahlen selbst, der Test bleibt, wo er
   ist.** Verworfen: dann schreibt ein Strang regelmäßig in die Testdatei des
   anderen, ohne dass die Tabelle das deckt — genau die Unklarheit, die den
   Fall erzeugt hat. Ausserdem bliebe die irreführende Prozedur im Kommentar
   stehen.
3. **Den Ausgangspin ersatzlos streichen.** Verworfen: er ist der einzige Test,
   der bemerkt, dass die KI die kanonische Partie überhaupt nicht mehr
   entscheidet. Genau das ist in Paket 16.3 passiert (Entscheidungstick 0), und
   ohne den Pin wäre es niemandem aufgefallen.
4. **ANGENOMMEN: der Pin wird geteilt.** Die Kennung bleibt beim
   Einheitenstrang (`SkirmishAiTests.AiBehaviorId_TracksWhichAiThisIs`),
   Entscheidungstick und Endzustand ziehen in `CanonicalAiOutcomeTests` um,
   das der Netzstrang besitzt.

**Entscheidung:**

1. `CanonicalAiOutcomeTests` (neu, Netzstrang) pinnt Entscheidungstick und
   Endzustands-Hash der kanonischen KI-Partie.
2. `SkirmishAiTests` behält die Kennung und deren Prozedur (Revision bumpen,
   Journaleintrag) unverändert.
3. Der Ausgangstest **liest die Kennung mit** und macht die Unterscheidung im
   Fehlertext: Ausgang bewegt **und** Kennung bewegt heisst KI-Änderung;
   Ausgang bewegt und Kennung unverändert heisst Simulationsänderung.
4. **Unentschieden ist ein Defekt, kein verschobener Pin.** Der Ausgangstest
   sichert das zuerst und getrennt zu, damit der Fehlertext beide Fälle
   auseinanderhält.
5. `tools/Nova.SimRunner.Tests/` bekommt eine Eigentümerzeile: geteilt je
   Datei, fremde Testdateien nur nach Ansage. Der KI-Harness (`AiHost`,
   `BuildMatch`) wird Vertragsfläche und dafür `internal`.

**Begründung:** Die Kennung beantwortet „welche KI ist das", der Ausgang
beantwortet „hat sich irgendetwas bewegt". Das sind zwei Fragen, und nur die
erste gehört dem Einheitenstrang. Die Diagnose, die die Kopplung leistete, geht
nicht verloren — sie wandert aus der Zusicherung in den Fehlertext, wo sie
ohnehin hingehört: gelesen wird sie erst, wenn der Test rot ist.

**Konsequenzen:** Vier Deklarationen in `SkirmishAiTests` werden `internal`,
die Zusicherung dort schrumpft auf eine Zeile, der erklärende Kommentar
verweist auf die neue Datei. Der Einheitenstrang ist zu informieren (Issue #75).
Wer den Harness umbenennt, sagt es an. Keine Baseline-Datei ist berührt.

---

### D-102 | verbindlich | Sprint 16 (fünf endliche Aetheriumfelder; Ernterate getrennt kalibrieren)

**Status:** Inhaberfreigabe vom 2026-08-09.

**Kontext:** Die kanonische Glutrinne registriert bislang zwei Felder mit je
2.000.000 AE. Ein Sammler könnte daran rund 28 Stunden ununterbrochen ernten;
Expansion und Kartenmitte haben damit keinen wirtschaftlichen Zweck. Das
MVP-Manifest nennt fünf Felder mit 9.000 beziehungsweise 15.000 AE, während
`EconomySystem.HarvestRateAE = 2` ausdrücklich nur provisorisch ist. Für eine
neue Ernterate existiert weder ein verbindlicher Zielwert noch gespielte
Messung.

**Alternativen:** (a) Feldlayout und Ernterate gleichzeitig nach Gefühl
ändern; (b) die endlichen Manifestfelder liefern und die Rate unverändert
lassen, bis eine gespielte Kurve kalibriert werden kann; (c) Knappheit bis zur
vollständigen Balance-Runde vertagen und die praktisch endlosen Felder
behalten.

**Entscheidung:** (b). Die kanonische Karte registriert in dieser Reihenfolge:

1. Start Slot 0 `(7,7)`, 9.000 AE;
2. Start Slot 1 `(117,117)`, 9.000 AE;
3. Expansion Slot 0 `(24,40)`, 9.000 AE;
4. Expansion Slot 1 `(100,84)`, 9.000 AE;
5. Zentrum `(62,62)`, 15.000 AE.

Die Paarpositionen sind durch `(x,y) → (124-x,124-y)` punktgespiegelt. Felder
werden vor den HQ-/Builder-Entitäten in aufsteigender ID-Reihenfolge
registriert. `HarvestRateAE` bleibt für Paket 16.7 bei **2 AE/Tick**; ihre
Kalibrierung ist sichtbar vertagt und nicht als erledigt zu melden.

**Begründung:** Die Manifestreserven beheben den belegten
Knappheitsdefekt, ohne eine unbelegte zweite Balancevariable zu verändern.
Symmetrie verhindert einen Startvorteil, die feste Reihenfolge hält beide
Lockstep-Hosts und den Headless-Harness identisch.

**Konsequenzen:** Alle fünf Spiegel der Startaufstellung müssen gemeinsam
ziehen; die Präsentation markiert und schützt alle fünf Felder vor
Steinstreuung. Der Definitions-Hash und das Zustandsformat ändern sich nicht,
der gehashte Initialzustand aber schon. Geschützte Determinismus-Baselines
werden nur in einem getrennten Baseline-PR bewegt, falls der Guard dies
verlangt. Die Ernteraten-Kalibrierung braucht eine gespielte Runde mit
Zeitkurve und bleibt bis dahin offen.

## Offene Punkte

- Alle Sprint-4-Review-Befunde (105, davon 9 kritisch): 7 entscheidungsbedürftige kritische Befunde sind durch D-043–D-052 entschieden.
- Q-018 (Preispunkt) und Q-019 (Telemetrie) bleiben offen; Sprint 6 hat dafür keine gültige D-ID erzeugt.
- Q-031–Q-034 sowie Q-038/Q-039 sind durch D-056–D-061 geschlossen;
  D-062–D-064 härten deren Evidence-Nachweis.
- Sprint 5 (Asset Audit): D-053/D-054 ratifiziert; **Budget-Obergrenze ist mit 0 € geschlossen (Q-035, D-054)**; Seat-Planung (Q-036) entfällt/gegenstandslos; Bundle-Fenster-Monitoring (Q-037) entfällt zugunsten CC0/KI-Pipeline.
- **D-ID-Kollision beim Merge des Art-Strangs aufgelöst — vorläufig, Bestätigung
  durch den Inhaber erbeten:** Der Art-Strang (Branch `docs/ms1-art-strand`,
  PR #8) war vor der Governance-Familie abgezweigt und hatte D-066 bis D-070
  unabhängig belegt. Dadurch waren D-066, D-067 und D-068 nach dem Merge je
  zweimal mit völlig unterschiedlichem Inhalt vergeben. Aufgelöst wurde zugunsten
  der Governance-Nummerierung: D-066 (Fail-Closed-Autorisierung) sowie die
  Entwürfe D-067/D-068 behalten ihre IDs, der Art-Strang wurde geschlossen auf
  **D-069 bis D-073** verschoben. Ausschlaggebend war die Referenzlast, nicht
  der Rang: die Governance-IDs sind aus zehn Dokumenten heraus referenziert
  (MVPRecoveryPlan, Milestones, Roadmap, Architecture, RiskAnalysis,
  SprintPlanning und weitere), die Art-IDs außerhalb dieses Protokolls nur aus
  `docs/README.md`. **Inhalt, Wortlaut und Verbindlichkeit der fünf
  Art-Entscheidungen sind unverändert** — verschoben wurden ausschließlich die
  Nummern. Der Inhaber möge die Zuordnung bestätigen oder eine andere Aufteilung
  anweisen; eine erneute Umnummerierung ist mechanisch und billig.
- **D-067 und D-068 sind ENTWÜRFE und nicht in Kraft**; sie warten auf die
  Inhaberentscheidung (Dennis Westermann). Bis dahin gilt für die
  Graybox-Spur der Status quo ohne Gate-Autorität, und die im
  [ScopeLedger](ScopeLedger.md) registrierte Dokumentationsschuld ist offen
  ausgewiesen, nicht erlassen.
- **D-074 ist in Kraft, aber vom Agenten unter Delegation entschieden** — der
  erste Eintrag dieses Protokolls, bei dem der Inhaber die Wahl zwischen
  echten Design-Alternativen ausdrücklich abgegeben hat, statt sie selbst zu
  treffen. Der Inhaber möge die Matrixautorität bestätigen oder überstimmen;
  eine Umkehr ist eine Datenänderung, keine Strukturänderung. Bis dahin gilt
  D-074, weil Code, Tests und die bereinigten Fachdokumente sie tragen.
- **D-075 steht in derselben Delegationslage** (Fraktions-Achse in
  `SimDefinitions`, Variante (b)); die Teil-Entscheidung zur
  Legion-Schadensprovenienz hat der Inhaber per Sprint-Briefing selbst
  vorgegeben. **Offen darin:** Vehicles.md nennt in der Hyäne-Zeile (Legion
  Scout) eine konkrete Schadenszahl, die von der abgeleiteten 10 abweicht —
  das Briefing umfasste nur die drei Kampffahrzeuge (28/50/60). Der Scout
  bleibt abgeleitet, bis der Inhaber die Hyäne-Zeile entscheidet.
- **D-083 steht in derselben Delegationslage** (Menü-Overlay, UI Toolkit,
  `AutoStart = false`, JSON-Einstellungen, „Laden" ausgegraut); die
  Herkunftsangaben zu Menümusik, Key Art, Schrift und Menütitel hat der Inhaber
  dagegen selbst entschieden. **Offen darin:** Für die drei neuen Assets fehlen
  die `PROVENANCE.json`-Datensätze, die
  [../assets/Provenance.md](../assets/Provenance.md) vor der Repo-Aufnahme
  verlangt — bei den KI-Quellen sind `promptText`, `providerTermsUrl`,
  `providerTermsRetrievedAt` und ein wörtliches `outputOwnership`-Zitat
  Pflichtfelder, die nur der Inhaber liefern kann.
- **D-100 steht in derselben Delegationslage** (Ablage der Lobby-Serverseite
  unter `tools/lobby/`). Der Inhaber möge den Ablageort bestätigen oder einen
  anderen anweisen; eine Umkehr verschiebt Dateien und eine Zeile in der
  Schreibhoheitstabelle. Bis dahin gilt D-100.
- **D-078 bis D-082 sind reserviert** für die Übertragung der
  Inhaberentscheidungen E-1 bis E-5 aus
  [hashkrieg/00_Entscheidungen.md](hashkrieg/00_Entscheidungen.md); die dortige
  „Offene Punkte"-Zeile nennt noch den zu kurzen Bereich D-078 bis D-081 und
  begründet die Reservierung mit einem inzwischen erfolgten Eintrag (D-077).
  Beides ist in jener Datei nachzuziehen, nicht hier.
- **D-095 bis D-097 stehen in derselben Delegationslage** wie D-074/D-083
  (Lobby-Vermittlung über Supabase Edge Functions, kurzlebige
  HMAC-Match-Tokens, Build-Commit-Exposition zur Laufzeit); bei D-096 hat der
  Inhaber die Richtung (HMAC-Token) selbst vorgegeben, die Ausformung sowie
  D-095/D-097 hat der Agent unter Delegation entschieden — gekennzeichnet und
  überstimmbar. **Offen darin:** Das Supabase-Projekt ist noch nicht angelegt
  und die Function-Referenzen in
  [../tech/LobbySupabase.md](../tech/LobbySupabase.md) noch nicht gegen ein
  lebendes Projekt gelaufen; der erste Deploy-Lauf gehört dem Inhaber
  (Betriebspfad im selben Dokument).

## Nächste Schritte

- Zuerst G0-A Trusted-Gate-Bootstrap, danach G0-B Plattformbasis herstellen.
- Entscheidungen D-056–D-066 über die Gates G0–G5 umsetzen, ohne Gate-Status
  vorwegzunehmen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.34.0 | 2026-08-09 | D-102 aufgenommen: fünf symmetrische endliche Aetheriumfelder werden geliefert, die mangels belastbarer Zielkurve unveränderte Ernterate von 2 AE/Tick wird ausdrücklich getrennt kalibriert | Project Owner / Agent |
| 1.33.0 | 2026-08-09 | D-101 aufgenommen: der Ausgangspin der kanonischen KI-Partie (Entscheidungstick, Endzustand) wird vom Identitätspin getrennt und zieht in eine Maintainer-Datei; `tools/Nova.SimRunner.Tests/` bekommt erstmals eine Eigentümerzeile | Project Owner / Orchestrator |
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
| 1.10.0 | 2026-07-24 | D-056–D-061: Closed-Core MS-1, kanonische Deterministik/Persistence, Kapazität/FoW, Branch-Modell, Engine-Pin und ausführbare Evidence-/Acceptance-Gates | Project Owner / Game Director / Lead Technical Director / Lead QA Engineer |
| 1.11.0 | 2026-07-24 | D-062: Szenariometriken, Subject-Blobs und rekursive Same-Subject-Gate-Kette für Evidence verbindlich gemacht; D-009 für MS-1 teilersetzt | Project Owner / Lead Technical Director / Lead QA Engineer |
| 1.11.1 | 2026-07-24 | D-039-Folgen an die MS-1-Begrenzung durch D-056/D-058 angeglichen | Project Owner / Lead Technical Director |
| 1.12.0 | 2026-07-24 | D-063: Evidence-Schema 1.2, kanonische Check-Artefakte, geschützten Trust-Kontext, rekursive Draft-2020-12-Prüfung und Drei-Lauf-Messmethode entschieden | Project Owner / Lead Technical Director / Lead QA Engineer |
| 1.13.0 | 2026-07-24 | D-064: Pass-Autorisierung bis zum subject-unabhängigen Trusted-Gate-Bootstrap gesperrt und Schema-1.3-Zielvertrag entschieden | Project Owner / Lead Technical Director / Lead QA Engineer |
| 1.14.0 | 2026-07-25 | D-065: Authorize-Run-Bindung der Evidence-Kette (workflow_dispatch-Event, exklusiver Authorize-Job, eindeutige Run-IDs) nach Re-Review-Befund N-1 entschieden | Project Owner / Lead Technical Director / Lead QA Engineer |
| 1.15.0 | 2026-07-25 | D-066: zirkulären Authorize-Vertrag durch fail-closed G0-A1 und zweiphasigen Receipt-Vertrag für G0-A2 ersetzt | Project Owner / Lead Technical Director / Lead QA Engineer |
| 1.16.0 | 2026-07-26 | D-067 und D-068 als **Entwürfe** aufgenommen (Graybox-Spur ohne Gate-Autorität mit befristetem Dokumentationsschuld-Modus; Sim-Korrekturen im offenen Pre-G1-Formatfenster) – nicht in Kraft, Inhaberentscheidung ausstehend | Technical Writer (Entwurf) / Entscheid: Dennis Westermann |
| 1.17.0 | 2026-07-26 | Art-Strang MS-1 aus PR #8 aufgenommen (Art-Mask-Kanalbelegung, 0-€-Beschaffungspfad mit Whitelist/Blacklist, Grid-Zellgröße 3,0 m mit Gebäude-Footprints, Fraktionspaletten Allianz/Legion, restriktive Sonniss-Weitergaberegel). Beim Merge kollidierten die dort unabhängig vergebenen IDs D-066–D-070 mit D-066/D-067/D-068; der Art-Strang wurde inhaltsgleich auf **D-069–D-073** verschoben, siehe „Offene Punkte" | Technical Art / Producer / Project Owner |
| 1.18.0 | 2026-07-26 | D-074 aufgenommen: [../gamedesign/ArmorSystem.md](../gamedesign/ArmorSystem.md) als alleinige Autorität der Schaden-gegen-Panzerung-Matrix (6 × 6, ganzzahlige Prozentdarstellung), widersprechende Lokaltabellen in Infantry.md/Vehicles.md aufgehoben, „Kristall" und die unbespielte `Heavy`-Spalte in den ScopeLedger verschoben. **Vom Agenten unter ausdrücklicher Inhaber-Delegation entschieden, nicht vom Inhaber selbst** — als solches gekennzeichnet und überstimmbar | Agent (unter Delegation) / Delegation: Dennis Westermann |
| 1.19.0 | 2026-07-26 | D-075 aufgenommen: Fraktions-Achse in der kanonischen Simulation (34 Definitionen in `SimDefinitions`, Id-Regel 1..17/18..34, Slot-Fraktion im Economy-Block v2 mit `SetSlotFaction`-Guard, fraktionsaufgelöste Harvester-Ladekapazität); Teil-Entscheidung Legion-Fahrzeugschaden: konkrete Vehicles.md-Werte 28/50/60 schlagen die 85-%-Ableitung, Ableitung nur wo die GDDs schweigen. **Vom Agenten unter ausdrücklicher Inhaber-Delegation entschieden** (Teil-Entscheidung per Inhaber-Sprint-Briefing vorgegeben) — als solches gekennzeichnet und überstimmbar | Agent (unter Delegation) / Delegation: Dennis Westermann |
| 1.20.0 | 2026-08-06 | D-076 aufgenommen: Governance-Tier-Modell. Gate-Kette G0–G5, Receipt-Verträge und Evidenzpflicht schlafen gelegt (nicht gelöscht, Weckpfad dokumentiert); Meilenstein-Nachweis auf grüne CI plus gespielte Runde umgestellt; `tests` als Pflichtcheck ergänzt; DoD 13→4, PR-Template 11→3; Doku-Ritual und ≥3-Alternativen-Pflicht bis Tier 2 ausgesetzt; D-067 dadurch gegenstandslos | Project Owner / Orchestrator |
| 1.21.0 | 2026-08-06 | D-077 aufgenommen: spielbarer RTS-Core-Loop — Start HQ + 1 Builder + 3.000 AE, Harvester aus der Raffinerie, Raffinerie ohne Kraftwerk-Prereq, Sieg zusätzlich bei HQ-Verlust (D-056 Klausel 2 teilersetzt), Skirmish-KI für Slot 1 registriert und spielend, Debug-HUD standardmäßig aus (F3), Prefab-Views zur Laufzeit auf den Sim-Footprint normalisiert | Project Owner / Agent (Umsetzung) |
| 1.22.0 | 2026-08-06 | D-083 aufgenommen: Hauptmenü als Overlay in `Bootstrap.unity` statt zweiter Szene (`AutoStart = false`, „Neues Spiel" ruft `StartGrayboxMatch()`), UI Toolkit als UI-Stack für alles Neue, Einstellungen als `settings.json` in `Application.persistentDataPath` ohne `PlayerPrefs` und ohne `AudioMixer`, „Laden" sichtbar und ausgegraut; Assetherkunft Suno-Bezahltarif / OpenAI Image API / Rajdhani-OFL und Menütitel „HASHKRIEG" als Inhaberentscheidung. Punkte 1–4 und 6 vom Agenten unter Delegation entschieden und überstimmbar. D-078–D-082 bleiben für E-1 bis E-5 reserviert; Kopfversion holt den Rückstand auf die Verlaufstabelle (1.20.0/1.21.0) auf | Agent (unter Delegation) / Inhaberentscheidung Punkt 5: Dennis Westermann |
| 1.23.0 | 2026-08-06 | D-084 aufgenommen (zunächst als D-078 angelegt, wegen der E-1–E-5-Reservation und D-083 umnummeriert): bedienbares HUD — Bauleiste aus `SimDefinitions` statt `BuildingRegistrySO`, Command-Register schema v1 bleibt eingefroren (Upgrades/Turm-Prioritäten/Angriffsbewegung = offene Design-Fragen), Gebäude-Rotation präsentationsseitig behoben, Kamera MMB-Drag + Space, Minimap-Kamerakanal `MinimapCameraLink` | Project Owner / Agent (Umsetzung) |
| 1.24.0 | 2026-08-06 | D-085 aufgenommen: Baumodell — Builder-Modell bleibt, Builder wird beim Platzieren per Move-Intent über den normalen Command-Pfad automatisch zur Baustelle geschickt (Reichweitenregel unangetastet, keine neuen Baselines); verworfen: C&C-Modell und Hybridmodell wegen Bruch der Hash-/Replay-/Fingerprint-Baselines; Baustellen-Zustandsanzeige ist Teil der Entscheidung | Project Owner / Agent (Umsetzung) |
| 1.25.0 | 2026-08-07 | D-086 (Suno-Ausnahme um Ingame-Musik erweitert, drei Themen als OGG im Repo) und D-087 (Auto-Zielerfassung und Feuererwiderung im CombatSystem; Baselines unverändert, sechs neue Tests je Lane; Attack-Move ausgespart) aufgenommen | Project Owner / Agent (Umsetzung) |
| 1.26.0 | 2026-08-07 | D-088 aufgenommen: Truppenführung — Formationsverteilung mit geteiltem Flow-Ziel und `GoalGridPos` (Entity-Store v5), Separation im Stand (gedämpft, Totzone, Index-Tiebreak), Gebäude-Footprints ins Kostenfeld mit Push-out; Epoch-Restore-Vertrag von Vergleich auf Adoption geändert, Flow-Cache regeneriert an Ort statt Leerung; Baselines bewusst neu gesetzt | Project Owner / Agent (Umsetzung) |
| 1.27.0 | 2026-08-07 | D-089 aufgenommen: implementiertes 1v1-Lockstep über TCP, `TickComplete` als reiner Transport-Barrier, optionales Submission-Readiness-Gate, getrenntes `NOVAREC2`-/Diagnostikformat und fail-closed linux-x64-/systemd-/Deploy-Vertrag; D-033 hinsichtlich UDP und Ergebnisautorität teilweise ersetzt | Project Owner / Agent (Umsetzung) |
| 1.28.0 | 2026-08-08 | D-090 aufgenommen: fog-sicheres sichtbares Gefechtsfeedback, D-039-konformer Tier-0-One-Shot-Service, 35 unveränderte Kenney-OGGs mit Batch-Provenienz, ehrlich unvollständige Suno-Nachweise und headless Quellcode-Guard; sämtliche Abweichungen vom 12B-Plan explizit begrenzt | Project Owner / Agent (Umsetzung) |
| 1.29.0 | 2026-08-08 | D-091 aufgenommen: Tier 2 vor dem ersten externen PR aktiviert; PolyForm Noncommercial plus dokumentierte, nicht rückwirkende CLA für externe Beiträge, zwei Merge-Accounts, Maintainer-Peer-Review auf jedem PR sowie vertrauenswürdige metadata-only Review-/Baseline-Checks entschieden | Dennis Westermann |
| 1.30.0 | 2026-08-09 | D-095 bis D-097 aufgenommen (Sprint 14 Lobby): Vermittlung über Supabase Edge Functions mit schlankem engine-freiem HTTPS-Client, Polling und RLS deny-all; kurzlebige 64-bit-HMAC-Match-Tokens für den Relay (Single-Use über Resets, abgeleiteter Seed, statischer Direktweg unverändert); Build-Commit zur Laufzeit lesbar (Editor-Build-Stempel, `dev-editor`-Fallback). D-096-Richtung vom Inhaber vorgegeben, Ausformung sowie D-095/D-097 vom Agenten unter Delegation — überstimmbar | Agent (unter Delegation) / D-096 Richtung: Dennis Westermann |
| 1.30.0 | 2026-08-09 | D-095, D-096 und D-097 aufgenommen: Parallelbetrieb trennt über Dateihoheit statt Verhaltensraum (Sprint 16 parallel zu 13B, `Simulation/State/` in eingefrorenes Layout und strangeigene Befehlsanwendung geteilt, ein Strang je Merge-Fenster); Lager erhält eine aus dem Gebäudebestand **abgeleitete** AE-Obergrenze statt eines Zustandsfeldes und das Radar schaltet die Minimap frei; „Stoppen" löscht zusätzlich `UnitState.AttackTarget`, das Halte-Feuer bleibt beim Einheitenstrang | Project Owner / Orchestrator |
| 1.31.0 | 2026-08-09 | D-098 bis D-100 nachgetragen, nachdem sie anderswo bereits als geltend zitiert wurden: Tier-3-Auslöser auf Veröffentlichung, Geld und Publikum präzisiert statt auf jede personenbezogene Verarbeitung; Identitätsmodell der geschlossenen Beta mit Klartext-IP plus gekürztem Netzpräfix und 30-Tage-Löschfrist statt `HMAC(pepper, ip)`, MAC-Adresse verworfen, IP- und Präfixsperren zwingend befristet; Lobby-Serverseite als Quelltext unter `tools/lobby/` statt nur im Supabase-Projekt (vom Agenten unter Delegation entschieden, überstimmbar, in den Offenen Punkten vermerkt). D-095 Punkt 3 im Wortlaut auf das Regelwerk nachgezogen — die Befehlsanwendung trennt zwischen Zielsetzung und Ausführung, nicht zwischen Befehlsarten; dieselbe Entscheidung, keine neue D-ID. Kopfzeile von Sprint 13.0 auf 16 berichtigt | Project Owner / Agent (unter Delegation) |
