# Vision – Project Nova

**Version:** 0.4.1 | **Status:** Entwurf – MS-1-Recovery abgegrenzt | **Verantwortungsbereich:** Game Director | **Sprint:** 2

## Zweck

Das Leitbild von Project Nova: Was das Spiel sein soll, für wen, und nach welchen Design-Säulen jede spätere Entscheidung (Units, Maps, UI, KI, Content) bewertet wird. Dieses Dokument ist die oberste Design-Referenz des Projekts; alle GDD-Dokumente in `docs/gamedesign/` müssen sich auf mindestens eine Säule begründen können. Es ersetzt die verstreuten Vision-Formulierungen aus dem GDD-Outline und präzisiert sie gemäß den Sprint-2-Entscheidungen D-007 bis D-030.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – Vision-Entscheidungen D-007–D-030 und Recovery-Entscheidungen D-056/D-060
- [../research/RTS_Markt_Wettbewerb.md](../research/RTS_Markt_Wettbewerb.md) – Marktbelege für Positionierung und Erfolgsszenario
- [../analysis/KnowledgeBase.md](../analysis/KnowledgeBase.md) – gemeinsamer Faktenraum
- [./USP.md](./USP.md) – ausformulierte Alleinstellungsmerkmale
- [./TargetAudience.md](./TargetAudience.md) – Personas H1–H4
- Quelldokument Projektroot: `RTS_Game_Design_Outline.md` (historischer Stand)

## MS-1-Liefergrenze

Diese Vision beschreibt bewusst das Vollspiel-Zielbild. Für den aktuell
autorisierten MS-1-Produktionspfad führt D-056 beziehungsweise
[MVPContentManifest.md](../production/MVPContentManifest.md): Allianz und
Legion, ein lokales 1v1 gegen KI auf der klaren Wüstenkarte Glutrinne, je neun
Gebäude- und acht Einheitenrollen sowie der vollständige Aetherium-Kern.
Evolvierte, weitere Karten/Biome, Wetter, Kampagne, Online und sonstige hier
genannte Vollspielbreite sind Post-MVP und keine versteckte Abhängigkeit des
Coding-Starts.

## Leitbild

> **Project Nova ist der klassische Base-Builder für eine Generation, die mit Command & Conquer groß geworden ist – mit einer Ressource, die lebt, und einer Produktionsqualität, die 2026 State of the Art ist.**

Drei Sätze, an denen sich jede Entscheidung messen lassen muss:

1. **Der Kernloop ist heilig.** Basis aufbauen, Aetherium ernten, Armee produzieren, Karte kontrollieren, Gegner brechen. Alles, was diesen Loop nicht stärkt, ist Scope-Risiko (Markt-Research §5: Polish schlägt Feature-Breite).
2. **Aetherium ist der Star, nicht die Fraktionen.** Die lebendige Ressource (D-010) ist der kommunizierbare Kern-USP; die drei Fraktionen sind ihr Bühne und Kontrastmittel.
3. **Solo-First ist Qualitätsversprechen, nicht Verzicht.** Premium, Singleplayer/Skirmish-first (D-007): Wir bauen das RTS, das ein einzelner Spieler an einem Abend kauft, installiert und sofort gut findet – ohne Server, ohne Wartezeiten, ohne Live-Service.

**Erfolgsszenario (Realismus-Deckel, gemäß Markt-Research):** Tempest-Rising-Größenordnung – zehntausende Premium-Verkäufe, >85 % positive Steam-Reviews, langlebiger Singleplayer-Longtail. Nicht AoE-/StarCraft-Größenordnungen.

## Design-Säulen

Jede Säule ist mit ihren Leitfragen und den Entscheidungen verankert, die sie tragen. Bei Konflikten zwischen Säulen gilt die Reihenfolge: Säule 1 > Säule 2 > Säule 3 > Säule 4.

### Säule 1: Klassischer Base-Builder, modern lesbar

Wir spielen die C&C-Formel unverändert stark: Bauen, Ernten, Produzieren, Erobern. Modern ist nicht die Formel, sondern ihre Präsentation und Bedienung.

- **Lesbarkeit vor Realismus:** Klare Silhouetten und Fraktionsfarben (Azurblau/Stahlgrau, Rostrot/Ocker, Violett/Bio-Grün), eindeutige Einheitsrollen, keine visuelle Überladung. Ein Veteran muss jede Einheit auf einen Blick einordnen können; ein Einsteiger darf nie fragen "was war das gerade?".
- **Schräge Top-Down-Perspektive** (echte 3D-Welt, Pitch ~50–60°, Zoom; Rotation optional, Standard aus) gemäß D-019. Der Begriff "isometrisch" wird im Projekt nicht mehr verwendet.
- **Bekannte Ökonomie, spürbare Konsequenzen:** Eine Hauptressource (Aetherium, Währung "AE"), Energie mit Low-Power-Regel (Defizit → Produktion −50 %, Radar/Verteidigung offline). Wer C&C gespielt hat, braucht kein Tutorial für die Basis – nur für Aetherium.
- **Vertraute Grundausstattung:** 12 kuratierte Gebäudetypen pro Fraktion inklusive Mauer und modularer Verteidigungsplattform (D-008) – bewusst die C&C-Checkliste, die H1 erwartet.

### Säule 2: Aetherium als lebendige Ressource

Die Kristallfelder sind kein statisches Wirtschafts-UI-Element, sondern ein zweiter "Spieler" auf der Karte (D-010):

- **Hybridwirtschaft:** Endlicher Mutterkristall + nachwachsende Ausläufer + langsame Feldausbreitung mit Terrainveränderung + Überernte-Risiko (dauerhafter Schaden am Mutterkristall).
- **Map-Control als Wettlauf:** Felder wachsen in Richtung freien Raums – wer die Karte nicht hält, verliert nicht nur Ressourcen, sondern lässt dem Gegner zukünftige Ressourcen entstehen.
- **Gier ist bestraft:** Überernte ist eine strategische Entscheidung mit langfristigem Preis, kein Exploit ohne Kosten.
- **Matchdauer-Anker:** Ziel 20–35 Minuten (D-010); die Feld-Ökonomie ist die primäre Stellschraube für Match-Pacing.

Detailregeln (Raten, Phasen, Überernte-Schwellen) gehören in `gamedesign/Resources.md` bzw. `gamedesign/Economy.md` – die Vision legt nur das Prinzip fest: **Die Ressource hat ein Gedächtnis und eine Zukunft.**

### Säule 3: Asymmetrie mit Identität

Drei Fraktionen sind Genre-Standard – kein USP. Der Anspruch ist, dass sich die Asymmetrie **mechanisch** unterscheidet, nicht nur in Zahlen:

- **Allianz** – High-Tech, präzise, teuer. Wenige, starke Einheiten; Ionenstrahl-Superwaffe. Identität: Qualität, Kontrolle, Overkill-Präzision.
- **Legion** – Masse statt Klasse, günstig. Flammen und Raketen; thermobarische Superwaffe. Identität: Druck, Überwältigung, Opferbereitschaft.
- **Evolvierte** – biologisch mutierte Kristallwesen. Wachstums-Bauweise statt Konstruktion (Keim pflanzen → reift; Aetherium-Nähe beschleunigt; Regeneration statt Reparatur) gemäß D-011; Kristallsturm-Superwaffe. Identität: Anpassung, Heilung, Symbiose mit Aetherium – die einzige Fraktion, die vom USP direkt profitiert.
- **Identitäts-Layer statt Mechanik-Ballast:** Commander liefern im
  Vollspiel-Zielbild Portrait, Voice und Story, aber keine Match-Mechanik
  (D-009). D-056 verschiebt den gesamten Commander-Layer einschließlich
  Portrait und Voice hinter MS-1. Die MS-1-Asymmetrie entsteht ausschließlich
  über Wirtschaft, Werte und Einheitenrollen.
- **Tiefe gestaffelt:** Elite-Einheiten (D-015), Support-Drohnen (D-014) und gezielte Zerstörbarkeit (D-012) geben jeder Fraktion Endspiel-Werkzeuge, ohne die Kernlesbarkeit zu gefährden.

### Säule 4: Solo-First-Qualität

Premium-Qualität heißt: Das Spiel muss ohne jede Online-Komponente vollständig und poliert sein (D-007, D-018).

- **Skirmish ist das Flaggschiff:** MVP = Solo-Skirmish 1v1 vs. KI; Koop/FFA ab Alpha; PvP ab Beta; Ranked nur nach Re-Evaluierung (D-018). Die KI ist damit kein Beiwerk, sondern Hauptgegner – sie muss die Aetherium-Ökonomie verstehen (Konsequenz aus D-010).
- **Kampagne ist der Phase-3-Pfeiler des Solo-Versprechens (D-020):** Lineare Solo-Kampagne mit 3 Akten (12–15 Missionen, je Akt eine Fraktionsperspektive); sie ist der stärkste Kaufgrund der H1-Zielgruppe und dient als Tutorial-Träger. Bewusst kein MVP-/Alpha-Umfang – Konzeptrahmen: [../gamedesign/Campaign.md](../gamedesign/Campaign.md). Koop-Kampagnen gibt es nicht; Koop läuft über separate Szenarien.
- **Matchdauer respektiert die Zielgruppe:** 20–35 Minuten passen in einen Abend; H1 (30–45 Jahre) spielt nicht mehr acht Stunden am Stück.
- **Performance als Feature:** 60 FPS bei 100–500+ Einheiten ist Hygiene, kein Marketing-Argument (Markt-Research §5) – aber ein gebrochenes Performance-Versprechen wäre ein Review-Killer.
- **Karten mit Charakter:** 10 Biome als Themen-Bibliothek, Wetter pro Biom, Hazards auf Mond/Mars (D-017); neutrale Objectives und Critters (D-016) machen Skirmish-Karten wiederholbar statt austauschbar.

## Künstlerischer Anspruch: Stylized Military Sci-Fi

**Richtung:** Stilisiert-realistische Militär-Sci-Fi – glaubwürdiges Hard-Sci-Fi-Waffendesign, das bewusst zugunsten von Lesbarkeit vereinfacht und stilisiert wird. Kein Fotorealismus (Budget, Performance, Zeitlosigkeit), kein Cartoon (Ton der H1-Zielgruppe).

- **Lesbarkeit als Art-Direction-Regel:** Silhouette > Detail. Jede Einheit muss in 2-facher Zoom-Entfernung per Umriss und Fraktionsfarbe identifizierbar sein. Details leben in der Nahansicht (Zoom belohnt Inspektion).
- **Fraktions-Codes:** Allianz = kantig, metallisch, Azurblau/Stahlgrau, saubere Energieeffekte; Legion = massiv, verrostet, Rostrot/Ocker, dreckige Verbrennung; Evolvierte = organisch-kristallin, Violett/Bio-Grün, leuchtende Adern und Wachstumsanimationen (D-011 macht Bauen zum sichtbaren Schauspiel).
- **Aetherium als visueller Anker:** Die Kristalle sind das Wiedererkennungszeichen des Spiels – Screenshots müssen ohne Logo als "Nova" erkennbar sein. Leuchten, Pulsieren, Ausbreitung und Verfall (Überernte) sind Key-VFX.
- **Referenzrahmen:** Näher an Tempest Rising / C&C3-Ästhetik als an AoE IV oder BAR. Stilisierung ist auch Budget-Realismus (Asset-Pipeline, Unity `6000.5.4f1`, Revision `d550df8bd089`, URP).

## Was Project Nova bewusst NICHT ist

Anti-Ziele sind verbindlich: Wer ein Feature vorschlägt, das hier steht, muss zuerst den entsprechenden Decision-Log-Eintrag revidieren lassen.

- **Kein F2P, kein Live-Service, keine Monetarisierung über den Kaufpreis hinaus** (D-007). Keine Battle Passes, keine Währungs-Shops, keine Server-Abhängigkeit. DLC-/Longtail-Strategie folgt dem Premium-Modell, ist aber kein MVP-Thema.
- **Keine Vollzerstörbarkeit / keine Terrain-Deformation** (D-012). Zerstörbar sind Gebäude, Einheiten, Vegetation/Dekor (brennbar), Brücken und Aetherium-Felder – gezielt und taktisch, nicht simulativ.
- **Kein Marine-Combat** (D-013). Wasser ist Terrain-Feature (unpassierbar bzw. Brücken), keine dritte Kampfebene.
- **Kein kompetitives Fundament:** MP ist Feature, nicht Fundament (D-007); Ranked existiert nur unter Vorbehalt (D-018). Wir bauen kein Esports-Spiel.
- **Kein Commander-/RPG-System in MS-1** (D-056; D-009 teilweise ersetzt).
  Keine Portrait-/Voice-Pipeline, Heldeneinheiten mit Leveln oder
  Match-Progression.
- **Kein Handelssystem / keine Händler** (D-016); keine Diplomatie-Simulation.
- **Keine Konsolen-/Mobile-Version:** Desktop Windows/macOS (D-007).
- **Keine Feature-Breite als Strategie:** Lieber ein polierter Kernloop als drei halbe Systeme (Markt-Research §4, Scheiternsursache 3).

## Offene Punkte

- **DLC-/Longtail-Strategie:** Post-MVP; vor G5 keine Planungsabhängigkeit.
- **Preispunkt:** ~30–40 € (D-007) ist der historische Korridor; finaler
  Preispunkt Q-018 bleibt offen und blockiert MS-1 nicht.

Entschieden seit 0.1.0: **Kampagnenstatus** (D-020 – Solo-Kampagne ja, Phase 3; aufgenommen in Säule 4, Konzeptrahmen [../gamedesign/Campaign.md](../gamedesign/Campaign.md)); **Persona-/Sandbox-Frage** (Sandbox-/Skalierungs-Nische ist Nicht-Zielgruppe, begründet über D-007 – siehe [./TargetAudience.md](./TargetAudience.md)).

## Nächste Schritte

- Den MS-1-Produktionspfad ausschließlich gegen D-056 und das
  Inhaltsmanifest prüfen.
- Vollspiel-Säulen erst nach G5 in eine neue Scope-Entscheidung überführen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung: Leitbild, 4 Design-Säulen, Art Direction, Anti-Ziele | Game Director |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Game-Design-Spezialist |
| 0.3.0 | 2026-07-24 | Recovery-Entscheidungen und exakten Unity-Pin gemäß D-056/D-060 eingeordnet | Game Director |
| 0.4.0 | 2026-07-24 | Vollspiel-Vision explizit vom verbindlichen Closed-Core-MS-1-Lieferscope getrennt | Game Director |
| 0.4.1 | 2026-07-24 | Commander-Identitätslayer vollständig als Post-MVP markiert und D-009-Teilersetzung durch D-056 sichtbar gemacht | Game Director |
