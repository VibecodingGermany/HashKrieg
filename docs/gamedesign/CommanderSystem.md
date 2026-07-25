# Commander-System

**Version:** 0.3.0 | **Status:** Post-MVP-Zielbild gemäß D-056 | **Verantwortungsbereich:** Lead UI/UX Designer | **Sprint:** 7

## Zweck

Definiert das Commander-Vollspielziel gemäß **D-009**. D-056 verschiebt
Commander, Portrait/Key Art, Voice und Doktrinen vollständig hinter G5; dieses
Dokument autorisiert keine MS-1-Implementierung.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-009
  (Vollspielziel, für MS-1 teilweise ersetzt) und D-056 (Commander/Voice
  Post-MVP)
- [RTS_Technisches_Planungsdokument.md](../../RTS_Technisches_Planungsdokument.md) §7.2 – Commander als Signature-Asset
- [../research/Animation_Audio_UI.md](../research/Animation_Audio_UI.md) – Voice-Management, `AudioService`-Abstraktion, FMOD ab Alpha
- [../analysis/KnowledgeBase.md](../analysis/KnowledgeBase.md) – Fraktions-Stammdaten, Zahlengerüst
- [./Factions.md](./Factions.md) – Fraktionsidentitäten, an die die Commander anschließen
- [./Campaign.md](./Campaign.md) – Kampagnen-Dialoge als Einsatzort (Phase 3, D-020; keine Koop-Kampagne, Koop über separate Szenarien)
- [./Biomes.md](./Biomes.md) – Wetter-/Hazard-Event-Namen für reaktive Barks (§4)

## MS-1-Override (D-056)

MS-1 registriert keine Commander-Definitionen, Portraits, Key Arts,
Voice-Profile, Barks oder Doktrinen. UI und MatchResult dürfen keine
Commander-Abhängigkeit besitzen. Der folgende Entwurf bleibt vollständig
Post-MVP und wird erst nach bestandenem G5 mit einer neuen D-ID reaktiviert.

## 1. Grundprinzip (D-009)

Der Commander ist das **Gesicht und die Stimme der Fraktion** – die Schnittstelle, über die das Spiel mit dem Spieler spricht. Er verankert die Unverwechselbarkeit (Signature-Asset, TPD §7.2), ohne ein zweites Balancing-Universum zu eröffnen (Begründung D-009, Risiko R-01). Konsequenzen:

- Commander beeinflussen **keine** Einheitenwerte, Produktion oder Ressourcen im MVP.
- Es gibt **keine** Commander-Einheit auf dem Spielfeld.
- Commander sind **fraktionsgebunden**: Wer die Allianz spielt, erlebt den Allianz-Commander. Es gilt vorerst **ein Commander pro Fraktion**; eine Auswahl zwischen mehreren Commander-Identitäten pro Fraktion (weiterhin ohne Mechanik) ist als **Post-Release-Option** festgelegt (Korrekturlauf Sprint 2, Game Director).
- Für die H1-Zielgruppe (D-007) liefern Commander den C&C-artigen Wiedererkennungswert: markante Stimme, klare Persönlichkeit, prägnante Event-Ansagen.

## 2. Commander-Stammdaten (v0.1, Richtwerte)

Ein Commander pro Fraktion (vorerst, siehe §1). Namen und Biografien sind Arbeitsstände zur finalen Abstimmung mit Narrative. Das Konzept "Der Chor" für die Evolvierten ist vom Game Director **bestätigt** (Korrekturlauf Sprint 2).

| CommanderID | Fraktion | Name (Arbeitstitel) | Persönlichkeit / Stimmlage | Hintergrund (Kurz) |
|---|---|---|---|---|
| `CMD_ALL_01` | Allianz | Kommandantin Dr. Elena Voss | Ruhig, präzise, kontrolliert; tiefe, klare Frauenstimme, trockener Humor | Ehemalige Aetherium-Forscherin, führt den Allianz-Einsatzverband nach dem Erstkontakt mit dem Kristallsturm |
| `CMD_LEG_01` | Legion | Kriegsherr Marcus Kael | Laut, fordernd, raubeinig; tiefer Bariton, aggressive Druckbetonung | Aufstieg durch die Minenrevolten; sieht Aetherium als Beute und das Schlachtfeld als Abrechnung |
| `CMD_EVO_01` | Evolvierte | Der Chor / "Stimme des Schwarms" | Mehrschichtig verzerrt, choral, unheimlich ruhig; Wechsel zwischen "wir" und seltenem "ich" | Fraktionsbewusstsein, das sich in den Kristallfeldern manifestiert; Hintergrund bewusst fragmentarisch |

Jeder Commander ist ein **flacher Datensatz** (ScriptableObject `CommanderDefinition`):

| Feld | Typ | Beispiel | Bemerkung |
|---|---|---|---|
| `CommanderID` | string | `CMD_ALL_01` | Primärschlüssel |
| `FactionID` | enum | `Allianz` | 1:1-Bindung (MVP) |
| `DisplayName` | string (lokalisiert) | "Dr. Elena Voss" | Lokalisierungsschlüssel |
| `PortraitAssetID` | string | `PRT_ALL_Voss` | 2D-Portrait, Signature-Asset |
| `KeyArtAssetID` | string | `KEY_ALL_Voss` | Ladebildschirm/Marketing |
| `VoiceProfileID` | string | `VP_ALL_Voss` | Verweis auf Voice-Line-Set + Sprecher-Profil |
| `BioTextID` | string (lokalisiert) | `BIO_ALL_Voss` | Codex-/Menütext |
| `AccentColor` | color | Azurblau | Fraktionsfarbe laut Stammdaten |

## 3. Einsatzorte

| Einsatzort | Inhalt | Phase |
|---|---|---|
| Fraktionsauswahl / Skirmish-Lobby | Portrait, Name, Kurz-Bio, Voice-Intro beim Bestätigen | MVP |
| Ladebildschirme | Key Art des eigenen Commanders + Tipp-Text | MVP |
| Event-Voiceovers im Match | Ansagen zu Spielzuständen (Katalog in §4), mit Portrait-Einblendung im UI | MVP |
| Kampagnen-Dialoge | Sprechblasen-/Dialogfenster mit Portrait, vertont | Phase 3 (Kampagne, D-020) |
| Codex / Menü | Biografie, Fraktionskontext | Beta |
| Marketing / Key Art | Cover, Store-Assets | Release |

Event-Voiceovers laufen über die `AudioService`-Abstraktion (Research Animation_Audio_UI): nicht deterministisch, reiner Presentation-Layer, frei variierbar. Spam-Begrenzung: max. 1 Event-Ansage alle ~8–12 s (Richtwert), kritische Events (Superwaffe, HQ unter Beschuss) dürfen unterbrechen.

## 4. Voice-Line-Kategorien und Umfang (Richtwerte v0.1)

Mengen sind **Startwerte zum Tunen**, keine Pseudo-Präzision. Begründung der Größenordnung: genug Varianz gegen Wiederholungs-Ermüdung bei 20–35 Min. Matchdauer (D-010), aber klein genug für ein Indie-Aufnahmebudget (1 Sprecher × 3 Fraktionen × ~2 Studio-Tage).

| Kategorie | Auslöser (Beispiel) | Lines/Commander (MVP) | Priorität |
|---|---|---|---|
| Match-Start / Intro | Spielbeginn, Fraktionsbestätigung | 4–6 | hoch |
| Ressourcen-Warnungen | Aetherium knapp, Feld erschöpft, Überernte-Schaden am Mutterkristall (D-010) | 6–8 | hoch |
| Gefahrenmeldungen | Basis unter Beschuss, Einheiten unter Beschuss, Mauer durchbrochen | 8–10 | hoch |
| Superwaffe | Bau begonnen, bereit, abgefeuert, feindliche Superwaffe erkannt (D-008) | 6–8 | hoch |
| Energie | Low-Power-Warnung, Energie wiederhergestellt (Low-Power-Regel) | 3–4 | mittel |
| Produktion/Tech | Tier-2/Tier-3 erreicht, Elite-Einheit verfügbar (D-015) | 5–6 | mittel |
| Sieg/Niederlage | Match-Ende, HQ verloren | 4–5 | hoch |
| Reaktive Barks (flavor) | Neutraler Geschützturm erobert (D-016), Wetter-/Hazard-Start (D-017; Events aus [./Biomes.md](./Biomes.md)) | 6–8 | niedrig |
| Kampagne (Dialogzeilen) | Story-Missionen (Phase 3, D-020) | 40–80 (Phase 3, separat budgetiert) | mittel |
| **Summe MVP (ohne Kampagne)** | | **~42–55 Lines** | |

Release-Ausbauziel (mit FMOD ab Alpha, Varianz-Gruppen): ~80–100 Lines/Commander inkl. 2–3 Alternativtakes für Hochfrequenz-Kategorien. **Vertonung/Lokalisierung (festgelegt im Korrekturlauf Sprint 2):** MVP voll vertont in **Englisch mit deutschen Untertiteln**; zum **Release Deutsch + Englisch voll vertont**. Abhängigkeit: finale Bestätigung des Aufwands hängt am Preis-/Budget-Rahmen (Q-018, offen); die Staffelung EN→DE+EN bleibt davon unberührt, betroffen wäre der Umfang der Alternativtakes.

Als Auslöser für Wetter-/Hazard-Barks gelten die **periodischen** Events aus [./Biomes.md](./Biomes.md): Sandsturm (Wüste), Schneesturm (Schnee), Aschefall (Vulkan), Monsunregen (Dschungel), Sporenflug (Alien-Welt) sowie die Hazards Strahlungsfronten (Mond) und Staubstürme (Mars). Ortsfeste Zonen (Nebelbänke, Smog) lösen keine Barks aus; die Verlassene Stadt hat kein Wetter.

## 5. Signature-Asset-Bezug (TPD §7.2)

TPD §7.2 listet **Commander** explizit unter den individuell zu entwickelnden Signature-Assets. Konsequenz für die Asset-Pipeline:

- **Portraits, Key Art, Voice:** Eigenentwicklung/Beauftragung, **kein** Asset-Store-Generikum. Das Portrait ist Teil der "zentralen UI-Identität" (ebenfalls §7.2) und folgt dem UI-Styleguide.
- **Stimm-Besetzung als Identitätsmerkmal:** Sprecher-Auswahl ist eine Design-Entscheidung (Casting-Vorgaben aus §2), keine reine Budgetfrage – die Stimme trägt die Fraktionsasymmetrie genauso wie Silhouette und Farbe.
- Commander-Assets müssen dem in der Asset-Pipeline definierten Review-Prozess für Signature-Assets folgen (höhere Qualitätslatte als Store-Assets).

## 6. Doktrinen-Ausblick (ab Beta, evaluierbar – D-009)

Kein MVP-Scope. Ab Beta **evaluierbar**: 2 wählbare passive Varianten pro Fraktion, vor Matchbeginn in der Lobby gewählt. Beispielskizzen (keine Festlegung):

| Fraktion | Doktrin A (Skizze) | Doktrin B (Skizze) |
|---|---|---|
| Allianz | "Präzisionsprotokoll": +Reichweite/-Feuerrate | "Logistiknetz": -Baukosten Kraftwerk/Lager |
| Legion | "Massenmobilisierung": -Infanteriekosten | "Feuerwalze": +Flächenschaden Flammenwaffen |
| Evolvierte | "Wachstumsschub": schnellere Gebäudereifung (D-011) | "Verhärtung": +Regeneration außerhalb von Kampf |

Voraussetzungen, bevor das evaluiert werden kann:

- **Eigene Entscheidung im DecisionLog** (D-009 legt nur den frühesten Zeitpunkt fest, nicht die Einführung).
- Datenmodell: `DoctrineDefinition` (flacher Datensatz: ID, FactionID, Modifikator-Liste, UI-Texte), Auflösung der Modifikatoren in der bestehenden Zahlen-Pipeline.
- Lobby-UI-Erweiterung + MP-Sync: Doktrinenwahl ist Teil des Initialzustands/Seeds (Determinismus, vgl. [../research/Multiplayer_Simulation.md](../research/Multiplayer_Simulation.md)).
- Balancing-Pass gegen alle drei Fraktionen (R-01!); Telemetrie/Spieltests vor Freigabe.
- UI-Lesbarkeit: Gegner muss die gewählte Doktrin im Match erkennen können (Anzeige-Entwurf nötig).

## 7. Nicht-Ziele (MVP)

- Kein Commander als spielbare Helden-/RPG-Einheit auf der Karte.
- Keine Commander-Progression, Level, Fähigkeitenbäume oder Freischaltungen.
- Keine Commander-Ausrüstung, Skins oder sonstige Monetarisierung (Premium-Modell, D-007).
- Keine Match-Mechanik, die Commander-Wahl zu einer Balancing-Variable macht.
- Kein Einsatz des Begriffs "Held"/"Hero" für Commander in UI und Doku (Verwechslungsgefahr mit Elite-Einheiten, D-015).

## Offene Punkte

- **Commander-Anzahl pro Fraktion:** entschieden (Korrekturlauf Sprint 2, Game Director) – vorerst genau 1 Commander pro Fraktion; Auswahl mehrerer Identitäten pro Fraktion ist Post-Release-Option (§1).
- **Lokalisierungs-Umfang der Voice:** entschieden (Korrekturlauf Sprint 2, Game Director) – MVP EN vertont + DE-Untertitel, Release DE+EN voll (§4); Rest-Abhängigkeit Preis-/Budget-Frage Q-018.
- **Evolvierte-Commander "Der Chor":** entschieden (Korrekturlauf Sprint 2, Game Director) – Konzept bestätigt (§2).
- **Wetter-/Hazard-Bark-Events:** geklärt – benannte Events aus [./Biomes.md](./Biomes.md) übernommen (§4). Verbleibend: technische Event-Schnittstelle zum Presentation-Layer beim Map-/Biome-Implementationssprint verifizieren.

## Nächste Schritte

- Keine Commander-Arbeit in G0–G5 einplanen.
- Nach G5 Scope, Fraktionszahl, Asset-/Voice-Budget und Doktrinen mit neuer
  D-ID neu bewerten.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead UI/UX Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030): Kampagne Phase 3 (D-020), 1 Commander/Fraktion + Post-Release-Option, "Der Chor" bestätigt, Vertonung EN→DE+EN (Q-018-Abhängigkeit), Wetter-/Hazard-Bark-Events aus Biomes.md | Lead UI/UX Designer |
| 0.3.0 | 2026-07-24 | Commander, Voice, Portrait/Key Art und Doktrinen gemäß D-056 vollständig hinter G5 verschoben | Lead UI/UX Designer / Game Director |
