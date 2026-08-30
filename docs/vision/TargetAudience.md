# Zielgruppen & Personas – Hashkrieg

**Version:** 0.2.0 | **Status:** Entwurf (Korrekturlauf Sprint 2) | **Verantwortungsbereich:** Game Director | **Sprint:** 2

## Zweck

Definiert die Personas H1–H4 von Hashkrieg mit Bedürfnissen, Erwartungen und verbindlichen Design-Ableitungen. Dieses Dokument ist die Priorisierungsreferenz für alle Feature- und Content-Entscheidungen: Bei Zielkonflikten gilt die Staffelung **H1 > H2 > H3 > H4**, und H4 ist vor Phase 3 explizit nicht zu bedienen (D-007, D-018). Die Personas operationalisieren die Zielgruppen-Hypothesen aus dem Markt-Research für die GDD-Arbeit.

## Abhängigkeiten

- [../research/RTS_Markt_Wettbewerb.md](../research/RTS_Markt_Wettbewerb.md) – Zielgruppen-Hypothesen (§7), Kaufverhaltens-Evidenz (§2–§4)
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-007 (Modell/Zielgruppe), D-018 (Modi-Staffelung), D-010 (Matchdauer), D-009 (Commander), D-016 (Neutrale), D-020 (Solo-Kampagne, Phase 3)
- [./Vision.md](./Vision.md) – Design-Säulen, Anti-Ziele
- [./USP.md](./USP.md) – USPs, auf die die Personas reagieren sollen

## Übersicht

| ID | Persona | Priorität | Kernmodus | Marktbeleg |
|---|---|---|---|---|
| **H1** | C&C-Nostalgiker | **Primär (D-007)** | Solo-Skirmish vs. KI | Tempest Rising: ~87 % positiv, Premium, SP-getrieben |
| **H2** | RTS-Einsteiger | Sekundär | Skirmish (niedrige KI), geführte Einstiege | AoE-Franchise: 60 Mio. kumulierte Spieler zeigen Einsteiger-Reservoir |
| **H3** | Koop-Spieler | Sekundär (ab Alpha, D-018) | Koop vs. KI, FFA mit Freunden | SC2-Co-op historisch stark (Einschätzung Research) |
| **H4** | Competitive | Explizit zurückgestellt bis Phase 3 | PvP 1v1/2v2, ggf. Ranked | SC2-Esports aktiv; Stormgate als Warnung |

---

## H1 – Der C&C-Nostalgiker (Primärzielgruppe)

**Profil:** 30–45 Jahre (D-007), hat mit C&C (Tiberian Dawn bis Generals) seine Genre-Prägung erlebt, spielt heute überwiegend solo oder Skirmish gegen die KI. Kauft Premium auf Steam (~30–40 €, D-007; finaler Preispunkt offen: Q-018, Sprint 6) – kein Diskussionspreis, wenn die Reviews stimmen; wishlistet monatelang vor, kauft zum Launch oder im ersten Sale. Hat Familie/Beruf: Spielsessions 1–2 Stunden am Abend. Liest Patchnotes nicht, merkt aber sofort, wenn sich Einheiten "falsch" anfühlen.

### Bedürfnisse

- **Eine Solo-Kampagne als narrativer Anker – Kaufgrund Nr. 1** im Referenzsegment (Tempest-Rising-Evidenz, D-020): 3 Akte, 12–15 Missionen, je Akt eine Fraktionsperspektive; kommt in Phase 3, dient als Tutorial-Träger.
- Der bewährte Kernloop ohne Lernkurven-Umbau: Basis bauen, ernten, Armee aufstellen, Gegner plattmachen.
- Wiedererkennbare Genre-DNA: Mauer bauen können (D-008), Harvester-Terror auf Erntefelder, Superwaffen-Countdown, "Unit ready".
- Ein frischer Haken, der den Kauf rechtfertigt – nicht "C&C, aber generisch" (Scheiternsursache 4 im Markt-Research).
- Respektvolles Pacing: Matches, die in einen Abend passen (20–35 Minuten, D-010).
- Polish: flüssige Performance, sauberes Pathfinding, keine Early-Access-Baustelle.

### Erwartungen

- Sofortige Kontrolle ohne Tutorial-Zwang; UI-Konventionen des Genres werden eingehalten (Rechtsklick-Befehle, Steuergruppen, Bauleiste).
- Drei spürbar unterschiedliche Fraktionen mit klarer Identität (High-Tech vs. Masse vs. Bio) – lesbar auf den ersten Blick.
- Skirmish mit sinnvollen Karten (Wiederholbarkeit durch Biome, Wetter/Hazards, neutrale Objectives, D-016/D-017).
- Eine Ressource, die sich vertraut anfühlt, aber neue Entscheidungen erzeugt (Aetherium, D-010).
- Commander-Präsentation (Portrait, Voice) als Würze, nicht als mechanischer Zwang (D-009).

### Design-Ableitungen

- **Kampagne als Phase-3-Lieferung einplanen (D-020):** Der H1-Kaufgrund Nr. 1 ist die Solo-Kampagne (Rahmen: [../gamedesign/Campaign.md](../gamedesign/Campaign.md)); bis Phase 3 muss die Launch-Kommunikation über Skirmish-/KI-Qualität laufen – das schärft die Qualitätsanforderung an KI und Karten.
- **Kernloop-Polish hat oberste Priorität** – jede Sprint-2-Designentscheidung wird zuerst an Säule 1 (Vision) gemessen.
- **Aetherium-Erklärbarkeit in <60 Sekunden:** Die Hybridwirtschaft muss im ersten Skirmish ohne Tutorial erfassbar sein ("Kristalle wachsen nach, aber nicht unendlich – und brutales Abernten ruiniert das Feld").
- **Klassische Checkliste einhalten:** 12 Gebäudetypen inkl. Mauer und modularer Verteidigung (D-008), Elite-Einheit als Tier-3-Endspielhöhepunkt (D-015), Drohnen als vertraute QOL-Werkzeuge (D-014).
- **Matchdauer-Disziplin:** Ökonomie-Raten und Kartengrößen (S/M/L) so tunen, dass 1v1-Skirmish regelmäßig im 20–35-Minuten-Fenster landet.
- **Review-Erwartung >85 %:** H1 vergibt die Bewertungen, die das Tempest-Rising-Erfolgsszenario ausmachen; unfertige Features werden von dieser Gruppe gnadenloser bewertet als fehlende.

## H2 – Der RTS-Einsteiger

**Profil:** 20–35 Jahre, spielt Strategie eher rundenbasiert oder Survival-/Aufbau-Hybride (Anno, Frostpunk, Against the Storm), hat kein RTS-Muscle-Memory. Findet Hashkrieg über Store-Screenshots (Aetherium-Look) oder Streamer. Kauft Premium, wenn der Einstieg nicht demütigt. Bricht ab, wenn er in Minute 3 von der KI überrannt wird oder nicht versteht, warum seine Produktion halbiert ist (Low-Power).

### Bedürfnisse

- Geführter Einstieg ohne Scham: die ersten 15 Minuten müssen Kernmechaniken spielerisch vermitteln.
- Verständliches Feedback statt Systemwissen vorauszusetzen: Warum ist mein Radar aus? (Low-Power-Regel muss die UI *erklären*, nicht nur anzeigen.)
- Fehlerverzeihende Frühphase: Zeit, die Basis aufzubauen, bevor Druck entsteht.
- Keine APM-Hürde: Hashkrieg darf keine Mikro-Intensität verlangen, die Einsteiger strukturell ausschließt.

### Erwartungen

- Klare Einsteiger-KI-Stufe, die spürbar "mit sich spielen lässt".
- Lesbares Schlachtfeld (Säule 1: Silhouetten, Fraktionsfarben) – Einsteiger lernen über Beobachtung.
- Eine zentrale, nicht überladene Ökonomie: Eine Hauptressource + Energie ist erklärbar; drei Ressourcen wären es nicht.
- Optional: ruhige PvE-Modi (Survival ab Beta, D-018) als druckfreier Einstieg.

### Design-Ableitungen

- **Onboarding ist Designaufgabe, nicht Nachgedanke:** GDD braucht ein Einstiegskonzept (erste Mission/Skirmish-Empfehlung, kontextuelle Hinweise) – Verantwortung [./CoreGameplay.md](./CoreGameplay.md).
- **Low-Power- und Überernte-Regeln brauchen proaktives UI-Feedback** (Warnung vor Defizit, sichtbarer Mutterkristall-Zustand) – beide Systeme sind für H1 intuitiv, für H2 die häufigsten Frustquellen.
- **KI-Schwierigkeitskurve definieren:** Mindestens eine Einsteiger-Stufe ohne frühe Aggression; KI-Design erhält diese Anforderung als Pflicht.
- **Kein Feature für H2 opfern, das H1 bricht:** Einsteiger-Führung passiert über UI/KI/Optionen, nicht über Vereinfachung der Systeme.

## H3 – Der Koop-Spieler

**Profil:** 25–45 Jahre, spielt RTS vor allem mit 1–3 Freunden gegen die KI am Wochenende. Sozialer Druck statt Wettbewerbsdruck: gewinnen will man, tryharden nicht. Kauft Premium, wenn alle in der Gruppe kaufen – Gruppenkauf-Logik, ein überzeugter Spieler zieht 2–3 weitere.

### Bedürfnisse

- Gemeinsam gegen die KI ohne Meta-Zwang: entspannte Koop-Matches mit klarem Shared-Goal.
- Rollenverteilung, die sich natürlich ergibt (einer baut Verteidigung, einer raided, einer hält die Aetherium-Felder).
- Matches mit erzählerischen Momenten: der knapp gehaltene Ansturm, die koordinierte Superwaffen-Salve.
- FFA mit Freunden als Chaotisch-Spaß-Option.

### Erwartungen

- Stabiler, einfacher Koop-Einstieg (Lobby, gemeinsame KI-Gegner, keine Server-Hürden).
- Karten, die Mehrspieler-Ökonomie abbilden: verteilte Aetherium-Felder, die Kooperation beim Ausbreitungs-Management erzeugen (D-010).
- Kein Ranglisten-Druck: Koop bleibt bewusst ungerankt.

### Design-Ableitungen

- **Koop vs. KI und FFA ab Alpha** (D-018) sind die H3-Lieferung; ihre Qualitätskriterien gehören in `gamedesign/MultiplayerModes.md`.
- **3v3/FFA-6-Karten (Größe L, D-017)** so layouten, dass Feld-Ausbreitung territoriale Kooperationsfragen erzeugt ("wessen Harvester darf in den Ausläufer?").
- **Survival (ab Beta)** als Koop-tauglicher Low-Pressure-Modus im Blick behalten – dient H3 wie H2.
- **Keine Koop-exklusiven Systeme:** H3 nutzt denselben Kernloop; kein separates Koop-Balancing-Universum (Scope-Disziplin, D-004-Geist/Markt-Research §4.3).

## H4 – Der Competitive-Spieler (explizit zurückgestellt bis Phase 3)

**Profil:** 20–35 Jahre, SC2-/AoE-Ladder-Hintergrund, hohe APM- und Balance-Erwartung, bewertet gnadenlos und öffentlich. Spielt 1v1 ranked, analysiert Build Orders, erwartet Patch-Kadenz.

### Bedürfnisse

- Balance auf Esports-Niveau, Maphack-Resistenz, stabiler Netcode, Ranked-Infrastruktur, aktive Pflege.

### Erwartungen

- Ein Meta, das Tiefe hat (Build-Order-Vielfalt, Konter-Beziehungen, Matchup-Balance 3×3).

### Design-Ableitungen

- **H4 wird vor Phase 3 bewusst NICHT bedient** (D-007, Research §7). Das ist eine Positionierung, kein Versäumnis: Stormgate zeigt, dass ein Competitive-Versprechen ohne Substanz Reviews und Retention zerstört (Research §4.1).
- **PvP 1v1/2v2 ab Beta** (D-018) ist die technische/demografische Vorstufe; **Ranked nur nach Re-Evaluierung** (Maphack-/Serverkosten-Frage, Q-013-Ausgang).
- **Nichts für H4 designen, das H1–H3 schadet:** Keine APM-Pflicht-Mechaniken, keine Balance-Eingriffe zu Lasten des Skirmish-Spaßes. Trotzdem: Design-Dokumente vermeiden Entscheidungen, die spätere Balance-Fähigkeit strukturell unmöglich machen (z. B. nicht-tunbare Hardcoded-Werte – alles datengetrieben, Zahlen v0.1 als ScriptableObject-Flachdatensätze).
- **Kommunikation:** Store-Texte und Dev-Kommunikation versprechen kein Competitive-Spiel. Wer H4 bedient sehen will, soll AoE IV oder SC2 spielen – das ehrlich zu sagen ist billiger als es zu versprechen.

## Nicht-Zielgruppe: Sandbox-/Skalierungs-Enthusiasten (BAR/Zero-K)

Die im Markt-Research (§7) als Hypothese geführte Sandbox-/Skalierungs-Nische (Beyond All Reason, Zero-K: langlebig, modding-affin, Megamatches mit tausenden Einheiten) ist **explizit keine Zielgruppe** von Hashkrieg; es wird keine Persona H5 angelegt.

**Begründung:** D-007 (Premium, Singleplayer/Skirmish-first) positioniert Hashkrieg bewusst gegen das Skalierungs-Versprechen. BAR gewinnt jede Skalierungs- und Preis-Debatte (kostenlos, Open Source, 100 Spieler, 10k Einheiten) – ein Wettbewerb um diese Nische ist mit der Studio-Kapazität und der 20–35-Minuten-Matchdauer (D-010) nicht zu gewinnen und würde den polierten Solo-Kern verwässern. Modding/Workshop und Einheiten-Skalierung sind keine Planungsannahme; eine spätere Öffnung wäre eine eigenständige neue Entscheidung.

## Priorisierung bei Zielkonflikten

1. **H1 entscheidet immer** bei Konflikten zwischen Personas (D-007).
2. **H2 wird über Führung bedient, nicht über Vereinfachung** – Systemtiefe bleibt H1-gerecht, Zugänglichkeit entsteht über UI, KI-Stufen und Optionen.
3. **H3 bekommt Modi, keine Systeme** – Koop ist Content-/Modus-Aufgabe, kein eigenes Design-Universum.
4. **H4 ist Phase-3-Briefkasten:** Wünsche werden in OpenQuestions gesammelt, nicht in Sprint-2-Dokumente eingearbeitet.
5. **Sandbox-/Skalierungs-Wünsche werden nicht bedient** – Nicht-Zielgruppe (s. oben); kein Skalierungs-Wettbewerb mit BAR (D-007).

## Offene Punkte

- **H2-Evidenz schwach:** Die Einsteiger-Hypothese stützt sich auf Franchise-Kumuliertzahlen (AoE), nicht auf Hashkrieg-nahe Daten. Bei Marketing-Budget für User Research ggf. validieren; kein Sprint-2-Blocker. Status: offen.

Entschieden seit 0.1.0: **Persona-Abweichung/Sandbox-Frage** (Sandbox-/Skalierungs-Nische als Nicht-Zielgruppe deklariert, begründet über D-007 – siehe Abschnitt "Nicht-Zielgruppe"); **Kampagnen-Persona-Lücke** (D-020 – Solo-Kampagne ja, Phase 3; als Kaufgrund Nr. 1 in H1 aufgenommen); **Alterskorridor H1** (30–45 Jahre verbindlich, D-007).

## Nächste Schritte

- Design-Ableitungen H1/H2 an die GDD-Autoren von [./CoreGameplay.md](./CoreGameplay.md), `gamedesign/Resources.md` und `gamedesign/MultiplayerModes.md` als Prüfkriterien weitergeben.
- KI-Anforderungen (Einsteiger-Stufe H2, Feldbewirtschaftung H1) als Input für das KI-Architektur-Design (Sprint 3, aufbauend auf [../research/KI_Architektur.md](../research/KI_Architektur.md)) formulieren.
- Kampagnen-Ableitungen (H1) bei der Phase-3-Planung mit [../gamedesign/Campaign.md](../gamedesign/Campaign.md) abstimmen (D-020).
- Nach Konsistenzreview Sprint 2: Persona-Staffelung in den Sprint-2-Bericht übernehmen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung: Personas H1–H4 mit Bedürfnissen, Erwartungen, Design-Ableitungen | Game Director |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Game-Design-Spezialist |
