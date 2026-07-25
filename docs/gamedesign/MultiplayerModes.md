# Multiplayer- und Spielmodi

**Version:** 0.5.0 | **Status:** Vollspiel-Zielbild mit verbindlichem MS-1-Override | **Verantwortungsbereich:** Lead UI/UX Designer | **Sprint:** 7

## Zweck

Definiert die Regeln aller Spielmodi von Project Nova gemäß der Phasenstaffelung **D-018**: Solo-Skirmish (MVP), Koop vs. KI und FFA (Alpha), PvP 1v1/2v2 und Survival (Beta), King of the Hill (Release), Ranked nur nach Re-Evaluierung. Enthält Lobby-/Teamregeln, Map-Pools, datengetriebene Match-Einstellungen sowie Beobachter-/Replay-Anforderungen. Verbindlich für UI/UX, Gameplay-Design und die Multiplayer-/Simulationsplanung (Sprint 3+).

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-056/D-058
  (führender MS-1-Modus und Kapazität) sowie D-007/D-010/D-015–D-018/
  D-020/D-025/D-028/D-029
- [../production/MVPContentManifest.md](../production/MVPContentManifest.md)
- [../research/Multiplayer_Simulation.md](../research/Multiplayer_Simulation.md) – Lockstep-Empfehlung, Replay-/Observer-Modell, Map-Hack-Risiko (§4–§6)
- [../analysis/KnowledgeBase.md](../analysis/KnowledgeBase.md) – Zahlengerüst (Startressourcen 1.000 AE, Harvester ~300 AE)
- [./Economy.md](./Economy.md), [./Maps.md](./Maps.md) – Wirtschafts- und Kartendetails
- [./Campaign.md](./Campaign.md) – Solo-Kampagne Phase 3 (D-020)
- [../production/RiskAnalysis.md](../production/RiskAnalysis.md) – R-02 (MP-Architektur), R-10 (Server-MP nicht als Fundament)

## MS-1-Override (D-056/D-058)

MS-1 besitzt genau einen lokalen Modus: ein Mensch gegen eine KI, Allianz und
Legion, zwei aktive von acht reservierten Slots, Glutrinne S 128×128 bei
klarem Wetter. Je Seite starten ein fertiges HQ, eine fertige Raffinerie, ein
Builder, zwei Harvester und 1.000 AE. Die Start-Raffinerie ist die einzige
Voraussetzungsausnahme und erzeugt keinen zusätzlichen Harvester.

Fest sind Standard-FoW, Spieltempo 1,0, Pause/Save/Load und die
45-Minuten-Grenze aus D-056. Es gibt keine frei konfigurierbaren
Ressourcen-/Starttruppen-/Wetter-/Superwaffen-/Elite-/Zeitlimit-Toggles,
keinen Netzwerktransport und höchstens 100 Produktionseinheiten insgesamt.
Alle abweichenden MatchSettings, der 600er-Deckel sowie Koop, FFA, Survival,
PvP, Observer und Ranked im nachfolgenden Text sind Post-MVP-Zielbild.

## 1. Übersicht und Phasenstaffelung (D-018, D-025)

| Modus | Phase | Spieler | Teams | KI-Beteiligung | lokal/online | Siegbedingung (Kurz) |
|---|---|---|---|---|---|---|
| Solo-Skirmish | MVP | 1 | 1v1 | 1 KI-Gegner | lokal | Vernichtung |
| Koop vs. KI | Alpha (lokal), Beta (online) | Alpha: 1 (+ KI-Verbündeter); Beta: 2 | 2v1–2 | 1–2 KI | Alpha: lokal, **1 menschlicher Spieler + KI-Verbündeter** (D-031.3, kein lokales 2-Spieler-RTS); Online-Koop (2 Spieler) ab Beta (D-025) | Vernichtung |
| FFA | Alpha | 2–6 (Spieler+KI) | jeder gegen jeden | auffüllbar | Alpha: **lokal gegen KI-Mitspieler** (D-025); Online-FFA ab Beta | letzter Überlebender |
| PvP 1v1 | Beta | 2 | 1v1 | nein | online (frühestens Beta, D-025) | Vernichtung |
| PvP 2v2 | Beta | 4 | 2v2 | nein | online (frühestens Beta, D-025) | Vernichtung |
| Survival | Beta | 1–4 | Spieler-Team vs. Wellen | KI-Wellen | lokal/online, Koop-Charakter (D-029) | Wellen überstehen / Endlos |
| King of the Hill | Release | 2–6 | 1v1, 2v2 oder FFA | optional | online (lokal optional) | Zonen-Punkte oder Vernichtung |
| Ranked | nur nach Re-Evaluierung | 2 | 1v1 | nein | online | wie PvP 1v1 |

Begründung der Staffelung siehe D-018: SP-first (D-007), jeder Modus erst, wenn seine technische Basis steht; Ranked hängt an Maphack-Resistenz und Infrastruktur (R-10, Q-013-Ausgang). **D-025-Klarstellung:** Da Netz-MP-Technik frühestens ab Beta steht (Q-013), laufen alle Alpha-Modi **lokal** – FFA in Alpha ist ein Solo-Modus gegen KI-Mitspieler; Koop online und PvP starten frühestens mit Beta.

**Kampagne (D-020):** Die Solo-Kampagne ist **Phase 3** (3 Akte, 12–15 Missionen) und kein Bestandteil der Modi-Staffelung oben; es gibt **keine Koop-Kampagne** – Koop-Spiel läuft über die separaten Szenarien/Modi dieser Datei. Konzeptrahmen: [./Campaign.md](./Campaign.md).

## 2. Match-Einstellungen (datengetrieben)

Alle Einstellungen sind flache Datensätze (`MatchSettings`-ScriptableObject), im Lobby-UI wählbar, mit Defaults je Modus. Werte = Startwerte v0.1 zum Tunen.

| Feld | Typ / Wertebereich | Default | Bemerkung |
|---|---|---|---|
| `StartingResourcesAE` | int, 500–10.000 (Schritt 250) | **1.000 AE** | Standard-Startressourcen laut Zahlengerüst |
| `StartingUnits` | enum: `HarvesterOnly` / `HarvesterPlusDefense` | `HarvesterOnly` | 1 Harvester (+ 1 Verteidigungsmodul-Bausatz in Variante) |
| `AetheriumDensity` | float, 0,5–2,0 | 1,0 | Multiplikator auf Feldgrößen/-Menge (D-010); **Deckel ≤1,5 bei 5–6 Spielern (D-048)** – der Lobby-Dialog begrenzt den Wertebereich spielerzahlabhängig |
| `AetheriumSpreadRate` | float, 0,5–2,0 | 1,0 | Multiplikator Feldausbreitung (D-010) |
| `WeatherEnabled` | bool | true | Biom-Wetter bzw. Hazards (Mond/Mars) gemäß D-017 |
| `FogOfWar` | enum: `Standard` / `Explored` / `Off` | `Standard` | Details: [../research/FogOfWar.md](../research/FogOfWar.md) |
| `SuperweaponsEnabled` | bool | true | Superwaffe-Tier (D-008) an/aus |
| `EliteUnitLimit` | int, 1–2 | 1 (MVP/Alpha) / 2 (Release) | gemäß D-015 |
| `GameSpeed` | enum: `Normal` / `Fast` | `Normal` | Fast ≈ +15 % Sim-Tempo (Richtwert), nur unranked |
| `TimeLimitMin` | int, 0=aus / 20–60 | 0 | bei Ablauf: Punktwertung – die PvP-Timeout-Punkteschlüssel- und Unentschieden-Wertformel wird erst im **Beta-Balancing** festgelegt (D-029) |
| `AIDifficulty` | enum: `Easy`/`Normal`/`Hard` | `Normal` | pro KI-Slot separat |

Siegbedingung "Vernichtung": Ein Spieler/Team ist besiegt, wenn **alle Gebäude (außer Mauern) und alle Einheiten** zerstört sind (führende Regel: [./VictoryConditions.md](./VictoryConditions.md), harmonisiert gemäß D-031.4/D-032); Rest-Einheiten werden nach 60 s ohne Gebäude auf der Karte aufgedeckt (Anti-Stall, Richtwert).

**Globales Einheiten-Deckel (D-048):** Pro Match sind maximal **600 Einheiten** gleichzeitig aktiv (alle Spieler, KI und ggf. Survival-Wellen summiert). Bei Erreichen des Deckels greift ein **Produktionsstopp mit UI-Hinweis** („Maximale Armeegröße erreicht") – Aufträge bleiben in der Queue, laufen aber erst wieder, wenn Einheiten ausscheiden. Das Deckel ist performance-kalibriert, gilt in **allen Modi inkl. Survival-Endlos** (§3.5) und greift nur im Extremfall; D-021 (kein Supply-Mikromanagement) bleibt unberührt.

## 3. Modus-Regeln

### 3.1 Solo-Skirmish (MVP)

- 1 Spieler vs. 1 KI, Kartengröße **S** (MVP-Map-Pool: 1 Karte, D-017).
- Alle Match-Einstellungen aus §2 frei wählbar; KI-Schwierigkeit Easy/Normal/Hard.
- Pausieren und Speichern/Laden jederzeit (SP-Privileg, D-007).
- Ziel-Matchdauer 20–35 Minuten (D-010) als Balancing-Anker für KI-Aggression und Aetherium-Mengen.

### 3.2 Koop vs. KI (Alpha: lokal, 1 Spieler + KI-Verbündeter; Online ab Beta)

- Team aus 2 Parteien vs. 1–2 KI-Gegner; Karten S/M. **Alpha (D-031.3):** 1 menschlicher Spieler + **KI-Verbündeter**, lokal – es gibt **kein lokales 2-Spieler-RTS** (genreunüblich, undefinierte Eingabemechanik). **Online-Koop mit 2 menschlichen Spielern frühestens ab Beta** (Netz-MP frühestens Beta, D-025).
- **Geteilte Sicht** innerhalb des Teams (Team-Fog-of-War); **kein Ressourcentransfer zwischen Teamspielern** (D-029: Wirtschaft bleibt ehrlich, Handelsverbot gilt sinngemäß).
- Jeder Spieler baut eigene Basis; gemeinsame Niederlage/Sieg.
- Drop-in/Drop-out: nein in v0.1 (Lockstep-Reconnect-Problem, siehe Research §4). **Disconnect-Regel (entschieden, D-038):** 60-s-Grace-Period mit Reconnect-Fenster; danach KI-Übernahme der Basis des Ausfallers (Mittel-Difficulty-Profil); Match läuft unpausiert weiter; kein Re-Entry nach Übernahme (führend: [../tech/Networking.md](../tech/Networking.md)).

### 3.3 PvP 1v1 / 2v2 (Beta)

- Wie Skirmish, ohne KI-Slots; Karten S (1v1) / M (2v2), **M/L** für 2v2 ab Beta-Pool.
- Kein Pausieren ohne Gegner-Zustimmung (Pause-Vote, max. 3 Pausen à 60 s pro Spieler – Richtwert).
- Kein Save/Load; Replays werden automatisch aufgezeichnet (§6).
- `GameSpeed` fest auf `Normal`; `FogOfWar` fest auf `Standard` (Wettbewerbs-Defaults).

### 3.4 FFA (Alpha, lokal)

- 2–6 Teilnehmer: in Alpha **1 menschlicher Spieler gegen KI-Mitspieler** (lokal, D-025 – Netz-MP frühestens Beta; Online-FFA ab Beta), Karten **L** (bis FFA-6, D-017).
- Keine Allianzen, kein Team-Chat; Sieger = letzter Überlebender.
- Anti-Stall: Wetter/Hazards laufen unverändert; zusätzlich Aetherium-Knappheit im Spätspiel als natürlicher Taktgeber (D-010, endlicher Mutterkristall) – kein künstlicher Timer nötig.

### 3.5 Survival (Beta)

- **1–4 Spieler** (D-029; lokal/online, Koop-Charakter) verteidigen gegen eskalierende KI-Wellen; **Standard-Karten aus dem Pool mit Verteidigungs-Engstellen** (D-028: keine eigenen Wellen-Karten).
- **Wellen-Regeln (Richtwerte v0.1):**

| Parameter | Startwert | Begründung |
|---|---|---|
| Wellenintervall | 90 s (Welle 1 nach 180 s) | Bauphase am Start; danach Dauerdruck |
| Wellenstärke | Start ≈ 8 Einheiten, +~25 %/Welle (multiplikativ) bis Welle 25; **ab Welle 25 linear statt multiplikativ (D-048)** | Eskalation gedeckelt durch das globale Einheiten-Deckel 600 (D-048) |
| Zusammensetzung | Welle 1–4 Infanterie, ab 5 Fahrzeuge, ab 10 Luft, ab 15 Elite-Mix | Tech-Tiers 1–3 spiegeln |
| Bauphase | 45 s nach jeder 5. Welle (kein Angriff) | Reparatur/Umbau-Fenster |
| Standard-Ziel | Welle 20 überstehen = Sieg (unverändert, bestätigt D-048) | Matchdauer ≈ 35–45 Min. (Survival darf länger als D-010-Anker) |
| Endlos-Modus | optional, Highscore (lokal); **Stärke-Abflachung ab Welle 25 (linear statt multiplikativ) + Despawn älterer Wellenreste; globales Deckel 600 gilt immer (D-048)** | Wiederspielwert ohne Server (D-007); Performance-Kalibrierung statt unbegrenzter Eskalation |

- Niederlage: **alle eigenen Gebäude (außer Mauern) und Einheiten des Teams zerstört** – identisch zur Standard-Vernichtungsregel in [./VictoryConditions.md](./VictoryConditions.md) (D-031.4, dort führend). Zwischen Wellen spawnen Aetherium-Nachschub-Ausläufer am Kartenrand (D-010-Mechanik als Überlebens-Ressource).

### 3.6 King of the Hill (Release)

- 2–6 Spieler, 1v1/2v2/FFA; Karten M/L mit **einer zentralen Zone**.
- **Zonen-Regeln (Richtwerte v0.1):**

| Parameter | Startwert | Begründung |
|---|---|---|
| Zonen-Radius | ~12 m (≈ 6 Rasterkacheln) | Platz für ~20 Einheiten, sichtbar umkämpfbar |
| Kontrolle | ausschließlich eigene Einheiten ≥ 10 s in der Zone | verhindert Durchflug-Capture |
| Contest | gemischte Belegung → Zone neutralisiert, keine Punkte | erzwingt Kampf statt Parken |
| Punkte | 1 Punkt/s während Kontrolle | kontinuierlicher Druck |
| Sieg-Punkte | 300 Punkte (≈ 5 Min. Gesamtkontrolle) | passt in 20–35-Min.-Anker (D-010) |
| Alternativ-Sieg | Vernichtung aller Gegner | KotH darf nicht Turtling belohnen |
| Zone sichtbar | immer (Fog of War ignoriert Zonenstatus) | Lesbarkeit für alle, UI-Pflicht |

### 3.7 Ranked (Vorbehalt – keine Zusage)

Ranked wird gemäß D-018 **nur nach Re-Evaluierung** geplant. Mindestanforderungen vor Evaluierung:

- **Maphack-Resistenz:** Lockstep gibt jedem Client den Vollzustand → clientseitige Fog-of-War-Aufhebung nicht verhinderbar (Research Multiplayer_Simulation §5). Voraussetzung: serverseitiges FoW-Filtering (Hybrid Richtung State-Authority nur für Sichtbarkeit) oder akzeptierte Risiko-Entscheidung des Game Directors.
- **Infrastruktur:** persistenter autoritativer Input-Relay-Server, Matchmaking (ELO/Glicko), Accounts – laufende Kosten kollidieren mit R-10 (Premium, kein Server-Fundament). Business-Case nötig.
- **Anti-Abuse:** Report-/Review-System (Replays liegen kostenlos vor, §6), Disconnect-/Leave-Regeln, Mindest-Spielerbasis für Matchmaking-Qualität.
- Evaluierungszeitpunkt: frühestens nach Release-Metriken (Spielerbasis, Retention).

## 4. Lobby- und Teamregeln

- **Lobby-Modell:** Host-Lobby (Beta), max. 6 Slots (Spieler + KI); Host = erster Beitretender, Migration bei Host-Wechsel (Offener Punkt: techn. Machbarkeit im Lockstep-Relay).
- **Slot-Optionen pro Slot:** Offen / Geschlossen / KI (Easy/Normal/Hard) / Beobachter (ab Beta, §6).
- **Team-Zuweisung:** manuell per Slot oder Auto-Balance; ungerade Teams nur in FFA.
- **Fraktionswahl:** offen sichtbar + `Random`-Option; Wechsel bis Ready-Check abgeschlossen.
- **Ready-Check:** Start erst, wenn alle menschlichen Spieler bereit; Host kann Countdown (5 s) erzwingen.
- **Kartenwahl:** Host wählt aus dem phasengültigen Map-Pool (§5); ab Beta: 3-Karten-Abstimmung (Mehrheit, bei Gleichstand Host-Stimme).
- **Farben:** freie Teamfarben-Wahl, Fraktions-Farben bleiben Default (Allianz Azurblau/Stahlgrau, Legion Rostrot/Ocker, Evolvierte Violett/Bio-Grün).
- **Chat:** Team- und All-Chat (Text) in Lobby und Match; **kein Ingame-Voice-Chat** (D-029: externe Tools decken das ab; Moderations-/Infrastrukturlast entfällt).

## 5. Map-Pools (D-017)

| Phase | Pool gesamt | Solo-Skirmish | Koop | FFA | PvP 1v1 | PvP 2v2 | Survival | KotH |
|---|---|---|---|---|---|---|---|---|
| MVP | 1 (S) | 1 | – | – | – | – | – | – |
| Alpha | 4 (2×S, 1×M, 1×L) | 2 | 2 | 1 (L) | – | – | – | – |
| Beta | 8 | 2 | 3 | 2 | 3 (S) | 2 (M/L) | 1 (Standard-Karte mit Engstellen) | – |
| Release | 12 | 2 | 4 | 3 | 4 | 3 | 2 | 2 (M/L mit Zonen-Layout) |

Zahlen sind Aufteilungs-Richtwerte; finale Zuordnung erfolgt in [./Maps.md](./Maps.md). Modus-spezifische Layout-Anforderungen: Survival nutzt Standard-Karten mit Verteidigungs-Engstellen (D-028, keine eigenen Wellen-Karten); KotH-Karten mit zentraler, symmetrisch erreichbarer Zone.

## 6. Beobachter und Replays

Technische Grundlage und Begründung: [../research/Multiplayer_Simulation.md](../research/Multiplayer_Simulation.md) §4. Im Lockstep-Modell ist ein Replay = Map-Seed + Initialzustand + zeitgestempelter Befehlsstrom (Kilobyte-klein, deterministisch abspielbar); Beobachter sind verzögerte Lockstep-Clients. Anforderungen aus Design-Sicht:

| Anforderung | Regel (Richtwert) | Phase |
|---|---|---|
| Auto-Replay | jedes PvP-/Koop-Match wird lokal als Replay gespeichert (letzte 20 Matches, Richtwert) | Beta |
| Replay-Browser | Liste mit Karte, Modus, Dauer, Spielern; Abspielen mit freier Kamera, Zeitleiste, 0,5×–8× Tempo | Beta |
| Beobachter-Slots | bis zu 2 pro Lobby (Richtwert), Delay 60–120 s gegen Live-Spiel | Beta |
| Observer-UI | Übersichts-Dashboard (Ressourcen, Einheitenzahl, Army-Wert pro Spieler), keine Spieler-Perspektive-Übernahme in v0.1 | Beta |
| Replay-Export | Datei teilbar (klein); Versionsbindung: Replays laufen nur auf exakter Spielversion (Determinismus) | Beta |
| Desync-Nebenutzen | Replays als KI-Test-Fixtures und Desync-Debugging (Research §4) – Entwicklungswerkzeug ab Alpha | Alpha |

## Offene Punkte

- **Ressourcentransfer zwischen Teamspielern:** entschieden (D-029) – kein Transfer, §3.2 entsprechend festgelegt.
- **Survival-Spielerzahl:** entschieden (D-029) – bis 4 Spieler, §3.5 entsprechend festgelegt.
- **FFA in Alpha (lokal vs. online):** entschieden (D-025) – Alpha-FFA = lokal gegen KI-Mitspieler; alle Netz-Modi (Koop online, PvP) frühestens Beta, abhängig vom Q-013-Ausgang.
- **Disconnect-Regel (KI-Übernahme, §3.2):** entschieden (D-038) – 60-s-Grace-Period mit Reconnect-Fenster; danach KI-Übernahme (Mittel-Difficulty-Profil); Match läuft unpausiert weiter; kein Re-Entry nach Übernahme. Führend: [../tech/Networking.md](../tech/Networking.md); §3.2 entsprechend festgelegt.
- **Ingame-Voice-Chat:** entschieden (D-029) – kein Ingame-Voice-Chat; externe Tools decken das ab.
- **Unentschieden-/Timeout-Wertformel:** entschieden (D-029) – PvP-Timeout-Punkteschlüssel und Unentschieden-Wertformel werden erst im Beta-Balancing festgelegt, nicht in diesem Dokument.
- **Skalierungs-Deckel (Einheiten, Survival-Endlos, AetheriumDensity):** entschieden (D-048) – globales Einheiten-Deckel 600/Match mit Produktionsstopp und UI-Hinweis (§2); Survival-Endlos mit Stärke-Abflachung ab Welle 25 (linear) und Despawn älterer Wellenreste (§3.5); `AetheriumDensity` ≤1,5 bei 5–6 Spielern (§2).
- **Host-Migration bei Host-Wechsel (§4):** offen – technische Machbarkeit im Lockstep-Relay mit MP-Engineering in Sprint 3 (Q-013) validieren.

## Nächste Schritte

- In G4 nur das feste MS-1-Matchsetup und die UI dafür abnehmen.
- Variable Lobbys, zusätzliche Modi und Online-Anforderungen erst nach G5
  mit neuer D-ID reaktivieren.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead UI/UX Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030): Modus-Tabelle mit lokal/online (D-025), Kampagne Phase 3 ohne Koop-Kampagne (D-020), kein Ressourcentransfer + Survival bis 4 + kein Voice-Chat + Timeout-Formel erst Beta (D-029), Survival auf Standard-Karten (D-028), Disconnect-Regel vorläufig (Q-013), Links repariert (Economy.md/Maps.md) | Lead UI/UX Designer |
| 0.3.0 | 2026-07-21 | Feinschliff Sprint 2 Runde 2 (D-031) | Lead Gameplay Designer |
| 0.3.1 | 2026-07-21 | Vernichtungs-Definition an VictoryConditions.md harmonisiert (D-032) | Lead Gameplay Designer |
| 0.3.2 | 2026-07-21 | Disconnect-Regel als entschieden markiert und §3.2 auf D-038 angeglichen (60-s-Grace, KI-Übernahme, kein Re-Entry; führend tech/Networking.md) | Lead Technical Director |
| 0.4.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings): D-048 eingearbeitet – globales Einheiten-Deckel 600/Match (Produktionsstopp + UI-Hinweis), Survival-Endlos-Abflachung ab Welle 25 (linear) + Despawn, AetheriumDensity ≤1,5 bei 5–6 Spielern | Lead UI/UX Designer |
| 0.5.0 | 2026-07-24 | Festes MS-1-Matchsetup, 100er-Produktionscap und Post-MVP-Grenze gemäß D-056/D-058 vorangestellt | Lead UI/UX Designer / Lead Technical Director |
