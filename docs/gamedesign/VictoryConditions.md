# Victory Conditions – Sieg, Remis, Timeout, Aufgabe

**Version:** 0.4.0 | **Status:** Vollspiel-Zielbild mit verbindlichem MS-1-Override | **Verantwortungsbereich:** Lead Gameplay Designer | **Sprint:** 7

## Zweck

Definiert, wann ein Match von *Project Nova* endet und wer gewinnt: den Standard-Sieg (Vernichtung), optionale Siegbedingungen je Modus, Remis- und Timeout-Regeln sowie die Aufgabe-Regel. Verbindlich für Match-Flow, UI (Sieg-/Niederlagen-Screens), KI (Aufgabe-/Sieg-Logik) und Balancing der Matchdauer (20–35 min, D-010).

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-056
  (führender MS-1-Siegvertrag) sowie D-007/D-008/D-015/D-016/D-018/D-025/D-029
- [../production/MVPContentManifest.md](../production/MVPContentManifest.md)
- [../vision/GameLoop.md](../vision/GameLoop.md) – Match-Phasen und Anti-Stall-Taktgeber
- [../vision/CoreGameplay.md](../vision/CoreGameplay.md) – UI-Feedback-Prinzipien
- [./MultiplayerModes.md](./MultiplayerModes.md) – Modi-Definitionen (Sprint 2, parallel)
- [./Balancing.md](./Balancing.md) – Matchdauer-Tuning (Sprint 2, parallel)

## MS-1-Override (D-056)

Für G0–G5 gilt ausschließlich:

- Auswertung nach Combat am Tickende;
- Niederlage bei null lebenden Einheiten **und** null lebenden Gebäuden
  einschließlich Baustellen;
- Eliminierung einer Seite: `Victory.Elimination`;
- Eliminierung beider Seiten im selben Tick:
  `Draw.MutualAnnihilation`;
- Tick 27.000 ohne Eliminierung: `Draw.TimeLimit`;
- bei null Gebäuden und höchstens drei Einheiten werden diese nach 600
  ununterbrochenen Ticks sichtbar und zielbar; endet die Bedingung, wird der
  Zähler zurückgesetzt;
- keine automatische KI-Aufgabe und kein Spieler-Surrender-Command.

Optionale Zeitlimits, Mauerausnahmen, Aufgabe, Commander-Feedback,
Team-/Survival-/PvP-Regeln und alle weiteren Profile im Alttext sind
Post-MVP-Zielbild. Sie dürfen den MS-1-Vertrag nicht konfigurieren oder
überschreiben.

## Standard-Sieg: Vernichtung

Gilt für Solo-Skirmish vs. KI (MVP, D-018) und als Default für alle Gefechts-Modi.

**Siegbedingung:** Ein Spieler/Team gewinnt, wenn die gegnerische Seite **keine Gebäude und keine Einheiten** mehr besitzt. Besiegt ist eine Seite, sobald beides erfüllt ist:

1. Alle Gebäude zerstört – alle Typen nach D-008: HQ, Kraftwerk, Raffinerie, Lager, Kaserne, Fahrzeugfabrik, Flugfeld, Forschungslabor, Radar, Verteidigungsplattformen und Superwaffe. **Ausnahme: Mauern zählen nicht zur Vernichtungsbedingung** (Festlegung Korrekturlauf Sprint 2) – eine vergessene Mauer-Ecke darf das Match nicht künstlich verlängern; Versteckspiel verhindert das Last-Unit-Reveal (unten). Bei den Evolvierten zählen Keime (unreife Gebäude, D-011) ebenfalls als Gebäude.
2. Alle Einheiten vernichtet – inklusive Harvester, Drohnen (D-014), Elite-Einheiten (D-015) und gelandeten Luftfahrzeugen.

Zusatzregeln (v0.1):

| Regel | Festlegung | Begründung |
|---|---|---|
| Versteckt-Spiel-Schutz ("Last-Unit-Reveal") | Hat eine Seite nur noch ≤ 3 Einheiten und kein Gebäude, werden ihre Einheiten nach 60 s dauerhaft auf der Minimap aufgedeckt. **Gilt sinngemäß auch für versteckte Gebäude ohne Einheiten:** Hat eine Seite keine Einheiten mehr, aber noch unentdeckte Gebäude (insb. versteckte Evolvierte-Keime, D-011), werden diese nach 60 s aufgedeckt | Verhindert das klassische "Harvester-Versteckspiel" und den Keim-Stall der Evolvierten, die Matchdauer sprengen und H1-Frust erzeugen (D-007); Anti-Stall-Linie aus [../vision/GameLoop.md](../vision/GameLoop.md) |
| Selbstzerstörung | Eigenes Abruppen/Aufgeben des letzten Gebäudes zählt als Niederlage | Kein Ausweichen über Eigenabbruch |
| Gleichzeitige Auslöschung | Vernichten sich beide Seiten im selben Tick (z. B. Superwaffen-Schlagabtausch), gilt **Remis** | Selten, aber muss definiert sein |
| Koop/Team | Ein Team ist besiegt, wenn **alle** Mitglieder besiegt sind; einzeln besiegte Spieler werden Zuschauer | D-018 Koop ab Alpha |

## Optionale Siegbedingungen pro Modus

Modi-Staffelung gemäß D-018; Details je Modus in [./MultiplayerModes.md](./MultiplayerModes.md). Diese Tabelle legt nur die Sieglogik fest (Startwerte v0.1):

| Modus | Verfügbar ab | Siegbedingung | Bemerkung |
|---|---|---|---|
| Solo-Skirmish 1v1 vs. KI | MVP | Vernichtung (Standard) | Optionales Zeitlimit wählbar (s. Timeout) |
| Koop vs. KI | Alpha | Vernichtung (Team) | Geteiltes Schicksal pro Team; **Online-Koop frühestens Beta** (D-025) |
| Free For All | Alpha – **lokal gegen KI-Mitspieler** (D-025) | Vernichtung; letzter verbleibender Spieler gewinnt | Kein Team-Reveal von besiegten Spielern an Verbündete (keine Verbündeten); Online-FFA frühestens Beta (D-025) |
| PvP 1v1 / 2v2 | Beta | Vernichtung (Standard), kein Zeitlimit im Default | Turnier-Profile können Zeitlimit aktivieren |
| Survival | Beta | **Überleben:** Sieg = alle Wellen abgewehrt; Niederlage = **Standard-Vernichtungsregel: alle eigenen Gebäude (außer Mauern) und alle Einheiten zerstört** (D-031.4 – diese Regel ist führend, MultiplayerModes.md zieht gleich) | Ersetzt die frühere Sonderregel (nur Gebäude); Wellen-Definition in MultiplayerModes.md |
| King of the Hill | Release | **Punktsieg:** Erste Seite mit X Hügel-Haltepunkten (Startwert 1.500, Zone generiert 10 Pkt./s) **oder** Vernichtung | Punkteparameter datengetrieben (`VictoryProfile`) |
| Ranked | nur nach Re-Evaluierung (D-018) | wie PvP | Keine eigenen Siegbedingungen |

Alle modus-spezifischen Siegparameter liegen als flache Datensätze vor (ScriptableObject `VictoryProfile`: `mode`, `conditionType`, `pointTarget`, `pointsPerSecond`, `timeLimitMinutes`, `revealThresholdUnits`, `revealDelaySeconds`), damit Modus-Varianten ohne Codeänderung konfigurierbar sind.

**Modus-Verfügbarkeit (D-025):** Alpha-Modi laufen lokal bzw. gegen KI; sämtliche Online-Modi (Koop online, FFA online, PvP) stehen frühestens ab Beta, abhängig vom Ausgang von Q-013 (Simulations-/MP-Modell).

## Remis- und Timeout-Regeln

| Fall | Regel | Begründung |
|---|---|---|
| Zeitlimit (optional, Skirmish/Koop) | Standard: **aus**. Wählbar: 45 / 60 / 90 min. Bei Ablauf: **Remis** (kein Punktesieg) | H1-Solo-Erlebnis (D-007): Ein Solo-Match soll nicht durch eine Wertung "verloren" werden, wenn das Anti-Stall-System (D-010) versagt hat; Remis macht den Stall sichtbar statt ihn zu belohnen |
| Zeitlimit (PvP, Beta) | Turnier-/Lobby-Option; bei Ablauf Wertung nach Punkteschlüssel, Default weiterhin Remis. **Punkteschlüssel und Unentschieden-Wertformel werden erst im Beta-Balancing finalisiert (D-029)** | Wettkampf braucht Entscheidbarkeit; der Schlüssel wird mit dem PvP-Balancing in Beta festgelegt (s. Balancing.md) |
| Gleichzeitige Auslöschung | Remis (s. oben) | – |
| Technischer Abbruch (MP) | 60-s-Grace-Period mit Reconnect-Fenster; danach KI-Übernahme (Mittel-Difficulty-Profil); Match läuft unpausiert weiter; kein Re-Entry nach Übernahme. Führend: [../tech/Networking.md](../tech/Networking.md) (D-038) | Entschieden in D-038 (Sim-/MP-Modell ist mit D-033 entschieden): hält Matches für Verbleibende spielbar, bestraft niemanden für Verbindungsabbrüche und schließt den Informations-Exploit (kein Re-Entry) |
| Stall-Erkennung (Solo) | Kein erzwungenes Match-Ende; stattdessen Hinweis-Dialog ab 40 min ohne Schadensereignis ("Keine Seite macht Fortschritt – Aufgabe oder weiterspielen?") | Soft-Anti-Stall, respektiert Solo-Spielerfreiheit |

## Aufgabe-Regel

- **Manuelle Aufgabe:** Jederzeit über das Spielmenü ("Match aufgeben"). Zählt als Niederlage; Bestätigungsdialog mit Hinweis auf ungespeicherten Fortschritt. Keine Aufgabe-Sperrzeit (SP-first, D-007).
- **Praktische Niederlage (Hinweis, kein Zwang):** Hat eine Seite weder Produktionsfähigkeit (kein HQ, keine Kaserne/Fahrzeugfabrik/Flugfeld, keine baufähige Einheit) noch Einkommen (keine Raffinerie **oder** kein Harvester), zeigt das Spiel einen dezenten Hinweis: "Deine Lage ist aussichtslos – Neu starten oder Aufgabe?" Das Match läuft weiter; der Spieler entscheidet. Begründung: H1-Spieler spielen Matches gern "zu Ende"; ein erzwungenes Ende würde das Solo-Erlebnis beschneiden (D-007).
- **KI-Aufgabe:** Die KI nutzt dieselbe Erkennung: Ist sie praktisch besiegt und hat keine realistische Wiederaufbau-Option, gibt sie auf (mit Abschieds-Voice-Line des Commanders, D-009) statt den Spieler zum Feldzug gegen Rest-Einheiten zu zwingen. Schwelle: praktische Niederlage seit > 90 s **und** Militärwert < 10 % des Spielers (Startwerte v0.1, KI-Tuning in [../research/KI_Architektur.md](../research/KI_Architektur.md)-Folgedokumenten).
- **Statistik:** Aufgabe und praktische Niederlage werden im Ergebnis-Screen unterschieden ("Aufgegeben" vs. "Vernichtet") – relevant für spätere Erfolge/Kampagnen-Auswertung, nicht für Ranked (D-018).

## Sieg-/Niederlagen-Feedback

Sieg und Niederlage lösen denselben Flow aus: Zeitlupe auf den letzten Zerstörungsmoment (2 s), dann Ergebnis-Screen mit Matchzeit, Zerstörungsbilanz, AE-Statistik und – bei Niederlage – optionaler "Weiterspielen als Zuschauer"-Schaltfläche (Solo) sowie Commander-Voice-Line (D-009, Identitäts-Layer). Keine Siegpunkte-Metawährung im MVP (Premium-Titel, D-007).

## Match-Statistik (Ergebnis-Screen)

Der Ergebnis-Screen zeigt folgende Kennzahlen pro Spieler (flache Datensätze, `MatchResult`-Struktur; Grundlage für spätere Erfolge und das Balancing-Telemetry):

| Kennzahl | Zweck |
|---|---|
| Matchdauer, Ergebnis (Sieg/Niederlage/Remis/Aufgabe) | Matchdauer-Kontrolle gegen Zielkorridor 20–35 min (D-010) |
| Gesammelte / ausgegebene AE | Wirtschaftsleistung, Überernte-Nutzung |
| Einheiten produziert / vernichtet / verloren (nach AE-Wert) | Effizienzbilanz, Grundlage PvP-Timeout-Punkteschlüssel |
| Gebäude errichtet / zerstört | Expansions- und Aggressionsverhalten |
| Höchste erreichte Tech-Stufe, Superwaffen-Einsätze | Eskalationskurven-Abgleich mit [../vision/GameLoop.md](../vision/GameLoop.md) |
| Neutral-Interaktionen (Lager geräumt, Türme erobert, D-016) | Map-Design-Feedback |

Im Solo-Skirmish ist der Screen ohne Zeitdruck verlassbar; im MP (ab Beta) führt er in die Lobby zurück. Statistiken sind rein informativ – keine Bestenlisten im MVP.

## Offene Punkte

- **Mauern in der Vernichtungsbedingung:** entschieden (Korrekturlauf Sprint 2) – Mauern zählen **nicht** zur Vernichtungsbedingung; Versteckspiel wird über das Last-Unit-Reveal abgesichert (s. Standard-Sieg).
- **Last-Unit-Reveal für versteckte Gebäude ohne Einheiten:** entschieden (Korrekturlauf Sprint 2) – das Reveal gilt auch für einzelne verbliebene Gebäude ohne eigene Einheiten, insb. versteckte Evolvierte-Keime (D-011).
- **PvP-Timeout-Punkteschlüssel:** entschieden (D-029) – Punkteschlüssel und Unentschieden-Wertformel werden erst im Beta-Balancing finalisiert (Balancing.md); bis dahin Default Remis.
- **Survival-Niederlage:** entschieden (D-031.4) – harmonisiert auf die Standard-Vernichtungsregel dieses Dokuments (s. Standard-Sieg; diese Datei ist führend): Niederlage, wenn alle eigenen Gebäude (außer Mauern) und Einheiten zerstört sind.

## Nächste Schritte

- In G4 ausschließlich den MS-1-Override gegen Contentmanifest und
  Headless-Ergebniscodes abnehmen.
- Optionale `VictoryProfile`, Surrender und KI-Aufgabe erst nach G5 mit neuer
  D-ID planen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Gameplay Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030): Mauern aus Vernichtungsbedingung ausgenommen, Last-Unit-Reveal auf versteckte Gebäude ohne Einheiten (Evolvierte-Keime) ausgeweitet, D-025-Klarstellung (Alpha-FFA lokal, Online-Modi ab Beta), PvP-Timeout-Schlüssel auf Beta-Balancing verschoben (D-029) | Lead Gameplay Designer |
| 0.3.0 | 2026-07-21 | Feinschliff Sprint 2 Runde 2 (D-031) | Lead Gameplay Designer |
| 0.3.1 | 2026-07-21 | Technischer Abbruch (MP) auf finale D-038-Regel angeglichen (60-s-Grace, KI-Übernahme, kein Re-Entry; führend tech/Networking.md) | Lead Technical Director |
| 0.4.0 | 2026-07-24 | Exakten D-056-Sieg-/Remis-/Reveal-Vertrag für MS-1 vorangestellt und widersprechende Altregeln auf Post-MVP begrenzt | Lead Gameplay Designer / Lead Technical Director |
