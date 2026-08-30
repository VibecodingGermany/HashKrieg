# USP – Alleinstellungsmerkmale von Hashkrieg

**Version:** 0.2.0 | **Status:** Entwurf (Korrekturlauf Sprint 2) | **Verantwortungsbereich:** Game Director | **Sprint:** 2

## Zweck

Definiert und belegt die Alleinstellungsmerkmale von Hashkrieg: den Kern-USP (Aetherium), die Sekundär-USPs und die konkrete Abgrenzung zu den relevanten Wettbewerbern. Dieses Dokument ist die verbindliche Basis für Store-Texte, Key-Art-Briefings und alle Design-Trade-offs ("stärkt das Feature einen USP oder verwässert es ihn?"). Ein Feature, das keinen USP stützt und nicht Säule 1 (Kernloop) dient, ist Scope-Risiko.

## Abhängigkeiten

- [./Vision.md](./Vision.md) – Leitbild und Design-Säulen
- [./TargetAudience.md](./TargetAudience.md) – Personas, auf die die USPs zielen
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-010, D-011, D-012, D-017
- [../research/RTS_Markt_Wettbewerb.md](../research/RTS_Markt_Wettbewerb.md) – USP-Bewertung (§6), Wettbewerberdaten (§2)

## Kern-USP: Aetherium – die lebendige Ressource

> **"Die einzige Ressource im Genre, die zurückschlägt: Sie wächst nach, erobert die Karte – und bestraft deine Gier."**

Formulierung für Steam-Store-Kurztext: *"Harvest a living crystal. Aetherium regrows, spreads across the map and reshapes the battlefield — but overharvest it, and it's gone for good."*

### Spielmechanik-Belege (gemäß D-010)

Der USP ist kein Marketing-Versprechen, sondern vier konkrete, datengetriebene Regelsysteme:

| Mechanik | Regel | Spielerlebnis |
|---|---|---|
| **Nachwachsen** | Sichtbare Kristalle regenerieren, solange der Mutterkristall lebt | Felder sind dauerhafte Einkommensquellen, kein 5-Minuten-Timeout wie in C&C |
| **Endliche Reserve** | Der Mutterkristall hat eine endliche Gesamtreserve | Map-Control bleibt relevant; kein flaches Unendlich-Late-Game wie bei SupCom/BAR |
| **Ausbreitung** | Felder breiten sich langsam in freien Raum aus und verändern das Terrain sichtbar | Die Karte von Minute 5 ist nicht die Karte von Minute 25; neue Fronten entstehen ohne Map-Skript |
| **Überernte** | Zu aggressives Abernten schädigt den Mutterkristall dauerhaft | Ökonomie wird zur Risikoabwägung: kurzfristiger Spike vs. langfristiges Einkommen |

### Warum das trägt (Marktbeleg)

- Tempest Rising nutzt exakt diesen Haken (nachwachsende Felder, die die Karte verändern) und wurde dafür als "frische Variante der Tiberium-Formel" mit ~87 % positiven Reviews belohnt (Markt-Research §6.1). Der Mechanismus ist **bewiesen attraktiv und dennoch nicht ausgereizt**.
- Hashkrieg geht zwei Schritte weiter als Tempest Rising: **Ausbreitung mit Terrainveränderung** und **Überernte als dauerhafte Spielerentscheidung** – die Ressource hat Gedächtnis. Das ist die eigentliche Differenzierung innerhalb des belegten Haken-Musters.
- Technisch tragfähig: datengetrieben (flache ScriptableObject-Datensätze für Wachstumsraten, Feldzustände, Überernte-Schwellen), auf demselben Grid wie Pathfinding/Fog of War (Konsequenz D-010).

### USP-Regel für alle weiteren Dokumente

Jedes System, das Aetherium berührt (Karten-Layout, KI, Fraktionsdesign, Superwaffen), muss die Lebendigkeit **sichtbar und spielrelevant** machen. Ein Aetherium, das man nur in der Ressourcenanzeige bemerkt, ist ein gescheiterter USP. Konkret: Kristallsturm-Superwaffe der Evolvierten, Feldbewirtschaftung der KI, Ausbreitungskorridore im Map-Design, Zerstörbarkeit der Felder durch Waffen (D-012) – alles Ausprägungen desselben USP.

**Verbindliche Anforderung – Lesbarkeit von Feldzustand und Überernte:** Der USP steht und fällt damit, dass Feldzustand (Reserve, Wachstum, Ausbreitung) und Überernte-Schaden für den Spieler **vorhersehbar und auf den ersten Blick lesbar** sind – über UI (Feldzustands-/Überernte-Anzeige, Warnung vor dem Schwellenwert) und VFX (Pulsieren, Leuchten, sichtbarer Verfall des Mutterkristalls). Fehlt dieses Feedback, erlebt der Spieler Überernte als unfaire versteckte Strafe statt als bewusste Risikoabwägung – dann ist der Kern-USP gefährdet (USP-Risiko, an den Game Director zu eskalieren). Dies ist eine offizielle Anforderung an das UI-Konzept (Sprint 3) und an `gamedesign/Resources.md` (Feedback-Regeln und Schwellenwerte).

## Sekundär-USPs

Sekundär heißt: sie differenzieren, tragen aber allein keine Kaufentscheidung. Reihenfolge = Priorität.

### S2 – Echte mechanische Asymmetrie: die Evolvierte Wachstumsbauweise (D-011)

Drei Fraktionen sind Hygienefaktor (Markt-Research §6.3) – **die Bauweise der Evolvierten ist es nicht.** Keim pflanzen statt konstruieren, Reifung beschleunigt durch Aetherium-Nähe, Regeneration statt Reparatur: Das ist der einzige fraktionsbezogene Mechanik-Unterschied, den kein aktueller Wettbewerber bietet, und er verzahnt Fraktionsidentität direkt mit dem Kern-USP (Evolvierte spielen *mit* der lebendigen Ressource statt nur *auf* ihr). Kommunikation: Nicht "3 Fraktionen", sondern "eine Fraktion, die ihre Basis wachsen lässt".

### S3 – Gezielte Zerstörbarkeit mit taktischem Nutzen (D-012)

Wald abfackeln, um Sicht- und Deckungslinien zu ändern; Brücken sprengen, um Engpässe zu schließen; Aetherium-Felder unter Beschuss vernichten, um dem Gegner die Zukunft zu nehmen. Bewusst **gezielt statt vollständig** – Hashkrieg verspricht Zerstörung als Taktik, nicht als Physik-Spielzeug. Abgrenzung zu CoH (Deckungssystem-Zerstörung) und BAR/Zero-K (Terraforming): Bei Hashkrieg ist Zerstörung kuratiert, lesbar und immer mit klarer Kosten-Nutzen-Frage verbunden.

### S4 – Biome mit spielbaren Hazards (D-017)

10 Biome als Themen-Bibliothek; Wetter pro Biom; auf atmosphärenlosen Karten (Mond, Mars) Hazards statt Wetter – Staubstürme und Strahlungsfronten, die Sichtweite, Bewegung oder Schildsysteme beeinflussen. Karten unterscheiden sich damit nicht nur optisch, sondern taktil. Sekundär-USP, weil Umwelteffekte im Genre bekannt sind – die Kombination aus Hazard-Regeln + Aetherium-Ausbreitung (dynamische Karte auf zwei Ebenen) ist jedoch eigenständig.

### Bewusst NICHT als USP kommuniziert

- **100–500+ Einheiten bei 60 FPS:** Hygienefaktor; BAR bietet kostenlos 10k-Einheiten-Schlachten (Markt-Research §5).
- **3 Fraktionen an sich:** Genre-Standard-Erwartungswert.
- **Gezielte Zerstörbarkeit als "vollständig zerstörbare Welt":** Führt Erwartungen in die Irre, die D-012 explizit nicht bedient (keine Terrain-Deformation).

## Abgrenzung zum Wettbewerb

| | **Hashkrieg** | **Tempest Rising** | **Age of Empires IV** | **Beyond All Reason** |
|---|---|---|---|---|
| Modell | Premium ~30–40 €, SP/Skirmish-first (D-007) | Premium $39,99, SP-getrieben | Premium + Game Pass, AAA-Live-Pflege | Kostenlos, Open Source, Community |
| Ressource | **Lebendig: nachwachsend + ausbreitend + Überernte-Gedächtnis** (D-010) | Nachwachsende Tempest-Felder, kartenverändernd | Klassisch endlich (Nahrung/Holz/Gold/Stein) | Unendlich (Metall-Extraktoren, Energy) |
| Fraktionen | 3 asymmetrisch, inkl. Wachstums-Bauweise (D-011) | 2(+1) asymmetrisch, klassisch gebaut | 10+ Zivilisationen, Varianten-Asymmetrie | 2 Fraktionen, riesige Unit-Roster |
| Zerstörbarkeit | Gezielt: Vegetation, Brücken, Felder (D-012) | Begrenzt | Begrenzt (Belagerungsschaden) | Terraforming/Deformation, MP-fokussiert |
| Skalierung | 100–500+ Einheiten, 1v1–3v3/FFA-6, Matchdauer 20–35 min | Klassische C&C-Skalierung | Klassische AoE-Skalierung | Bis 100 Spieler, 10k Einheiten |
| Zielspieler | Solo/Skirmish-Nostalgiker (H1) | Dasselbe Segment – **direkter Hauptwettbewerber** | Historie-Fans, breiter Mainstream | Hardcore-MP/Sandbox-Community |

**Abgrenzungs-Kernaussagen:**

1. **vs. Tempest Rising:** Hashkrieg ist *nicht* "noch ein C&C-Erbe". Tempest Rising belegt den Markt, Hashkrieg überbietet den Haken: Aetherium hat Gedächtnis (Überernte) und Zukunft (Ausbreitung), dazu die Evolvierte Wachstumsbauweise als zweite mechanische Säule. Wo Tempest Rising Nostalgie poliert, evolviert Hashkrieg die Formel.
2. **vs. Age of Empires IV:** Kein Wettbewerb um dasselbe Publikum. AoE IV ist historisch-breit und AAA-gepflegt; Hashkrieg ist Sci-Fi, fokussiert, Kleinstudio-Premium. Wir konkurrieren um "RTS-Abend"-Zeit, nicht um Features – und verlieren jeden Feature-Vergleich, also führen wir ihn nicht.
3. **vs. Beyond All Reason:** BAR gewinnt jede Skalierungs- und Preis-Debatte (kostenlos, 10k Einheiten, 100 Spieler). Hashkrieg konkurriert dort bewusst nicht: Unser Versprechen ist ein **kuratiertes, poliertes Solo-Erlebnis mit 20–35-minütigen Matches** – das Gegenteil der BAR-Sandbox. BAR-Spieler sind nicht unsere Käufer; unsere Käufer wollen kein 3-Stunden-Megamatch.

## Offene Punkte

- **Store-Text-Sprache:** Verbindliche USP-Formulierungen liegen deutsch vor; finale englische Store-Texte (Steam) sind nicht Gegenstand von Sprint 2 – Übergabe an Produktionsplanung/Marketing-Konzept. Status: offen.
- **Hazard-Balance (S4):** Ob Hazards rein negativ (Strahlung schadet) oder auch positiv nutzbar sind (z. B. Staubsturm als Angriffsdeckung), ist noch nicht entschieden – offener Design-Punkt für `gamedesign/Biomes.md`, beeinflusst die Stärke von S4. Status: offen.

Entschieden seit 0.1.0: **Kampagne als USP-Faktor** (D-020 – Solo-Kampagne kommt in Phase 3; sie wird bei der Phase-3-Planung als Sekundär-USP aufgenommen, ist aber kein Launch-Kommunikations-USP, da kein MVP-/Alpha-/Release-Umfang); **Überernte-Sichtbarkeit** (als verbindliche Anforderung in Abschnitt "USP-Regel für alle weiteren Dokumente" überführt – UI Sprint 3 und `gamedesign/Resources.md`).

## Nächste Schritte

- `gamedesign/Resources.md` / `Economy.md`: USP-Regeln (Nachwachsen, Ausbreitung, Überernte) in tunbare Zahlen gießen (Richtwerte v0.1, datengetrieben) – inkl. der verbindlichen Feedback-Regeln für Feldzustand/Überernte (s. USP-Regel oben).
- `gamedesign/Factions.md` / `Buildings.md`: Evolvierte Wachstumsbauweise (S2) als vollständiges Regel-Delta ausformulieren.
- `gamedesign/Maps.md` / `Biomes.md`: Hazard-Regeln (S4) und Zerstörbarkeits-Liste (S3) pro Biom definieren.
- Kampagne (D-020, Phase 3): Sekundär-USP-Aufnahme bei Phase-3-Planung mit [../gamedesign/Campaign.md](../gamedesign/Campaign.md) abstimmen.
- Nach Sprint-2-Konsistenzreview: USP-Statement (1 Satz) für Wiki-Index und Sprint-Bericht einfrieren.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung: Kern-USP Aetherium, Sekundär-USPs S2–S4, Wettbewerbsabgrenzung | Game Director |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Game-Design-Spezialist |
