# Core Gameplay – Spielgefühl, Kamera, Steuerung, UI

**Version:** 0.2.0 | **Status:** Entwurf (Korrekturlauf Sprint 2) | **Verantwortungsbereich:** Lead Gameplay Designer | **Sprint:** 2

## Zweck

Definiert das Moment-zu-Moment-Spielerlebnis von *Hashkrieg*: wie sich das Spiel in jeder Sekunde anfühlt, wie die Kamera arbeitet (D-019), wie alle Befehle erteilt werden und nach welchen Prinzipien die UI aufgebaut ist. Verbindlich für UI-, Kamera-, Input- und Gameplay-Implementierung ab Sprint 3/7.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – insb. D-007 (Zielgruppe H1, SP-first), D-010 (Aetherium-Wirtschaft), D-014 (Drohnen), D-015 (Elite-Einheiten), D-019 (Kamera), D-025 (Alpha-FFA lokal vs. KI, Online-Modi frühestens Beta), D-029 (Kamera-Rotation ab Beta, kein Ingame-Voice-Chat)
- [./GameLoop.md](./GameLoop.md) – Kernloop und Match-Phasen
- [../gamedesign/VictoryConditions.md](../gamedesign/VictoryConditions.md) – Sieg-/Niederlagen-Feedback
- [../gamedesign/Infantry.md](../gamedesign/Infantry.md), [../gamedesign/Vehicles.md](../gamedesign/Vehicles.md), [../gamedesign/Aircraft.md](../gamedesign/Aircraft.md) und [../gamedesign/Buildings.md](../gamedesign/Buildings.md) – Einheiten-/Gebäudestammdaten (Sprint 2, parallel)
- [../gamedesign/FogOfWar.md](../gamedesign/FogOfWar.md) – Sichtmodell (Sprint 2, parallel)
- [../research/Animation_Audio_UI.md](../research/Animation_Audio_UI.md) – UI-Technologiewahl
- [../analysis/KnowledgeBase.md](../analysis/KnowledgeBase.md) – Zahlenkorsett (Währung AE, Tech-Tiers)

## Moment-zu-Moment-Spielgefühl

Hashkrieg richtet sich an H1 "C&C-Nostalgiker" (D-007): Das Spielgefühl ist **klassisch-lesbar, direkt und großzügig** – kein APM-Zwang, keine versteckten Systeme, keine verschachtelten Menüs im Gefecht.

Designprinzipien (verbindlich, priorisiert):

1. **Responsivität vor Realismus:** Jeder Befehl gibt innerhalb von ≤ 100 ms sichtbares Feedback (Bewegungsmarker, Befehlsflagge, akustische Bestätigung, Voice-Line der Einheit). Verzögerungen entstehen durch Einheiten-Trägheit, nie durch Input-Latenz.
2. **Ein Befehl = eine Konsequenz:** Rechtsklick macht immer das Kontext-Richtige (sammeln, angreifen, einsteigen, betreten). Es gibt keinen Fall, in dem ein Standard-Rechtsklick "nichts" tut.
3. **Makro belohnt, Mikro optional:** Die Ziel-APM liegt bei ~60–120 (gemütlich bis engagiert). Micro-Tricks (Fokusfeuer, Kiting) sind Vorteil, nie Pflicht – Schwerpunkt liegt auf Wirtschafts- und Produktionsentscheidungen (D-010 als Taktgeber, siehe [GameLoop.md](./GameLoop.md)).
4. **Lesbares Schlachtfeld bei 100–500+ Einheiten:** Fraktionssilhouetten, Teamfarben (Allianz Azurblau/Stahlgrau, Legion Rostrot/Ocker, Evolvierte Violett/Bio-Grün), eindeutige Projektil- und Effekthierarchie. Spielerfarbe hat Vorrang vor Fraktionsfarbe.
5. **Alles erklärt sich selbst:** Icons, Tooltips und Voice-Feedback folgen einer einheitlichen Grammatik (s. UI-Prinzipien). Der Spieler lernt durch Spielen, nicht durch Lexika.

Feel-Bausteine (abstimmungsrelevant für Art/Audio):

| Element | Vorgabe (v0.1) | Begründung |
|---|---|---|
| Befehlsmarker | 0,5 s Einblendung, fraktionsneutrale Form, Teamfarbe | Sofort-Feedback ohne Visuellen Lärm |
| Auswahl-Voice | 1 kurze Zeile pro Einheitentyp, Cooldown 3 s | C&C-Nostalgie (H1), kein Voice-Spam |
| Zerstörungs-Feedback | Wrack bleibt 20–30 s liegen (D-012), danach Fade | Schlachtfeld liest sich als "Geschichte" des Gefechts |
| Treffer-Feedback | Zahleneinblendungen **aus**; Gesundheitsbalken + Trefferblitz | Zahlenflut widerspricht Lesbarkeitsprinzip |
| Spielgeschwindigkeit | 1,0 fest im Skirmish; Pause nur im Solo | SP-first (D-007), keine Speed-Debatten |

## Kamera (gemäß D-019)

Echte 3D-Welt, **schräge Top-Down-Perspektive**. Der Begriff "isometrisch" wird nicht mehr verwendet.

| Parameter (ScriptableObject-Feld) | Startwert v0.1 | Begründung |
|---|---|---|
| `pitchDefault` | 55° | Mitte des D-019-Korridors (50–60°); guter Kompromiss aus Geländelesbarkeit und Einheitensilhouetten |
| `pitchRange` | 50–60° (an Zoom gekoppelt) | Weit raus = flacher (60°), nah rein = steiler (50°), automatisch interpoliert |
| `zoomMin` / `zoomMax` (Kamerahöhe) | 18 m / 90 m | Nah: einzelnes Squad lesbar; Fern: halbe S-Karte im Blick |
| `zoomSteps` | stufenlos (Scrollrad), Ease-Kurve 0,15 s | Modernes Gefühl, keine Zoom-Rasten |
| `rotationEnabled` | `false` (Standard); die Option wird **erst ab Beta** ausgeliefert (D-029) | D-019/D-029: Rotation optional, Standard aus; H1-Zielgruppe erwartet feste Orientierung; Art-Aufwand pro Blickwinkel (Vegetation/Dekor aus allen Winkeln lesbar) erst ab Beta tragbar |
| `rotationSnap` | 45°-Schritte (Q/E), Rücksetzen auf Norden per Taste | Kontrolliert statt frei schwankend |
| `edgeScroll` | an, Stärke in Optionen 0–100 %, im Vollbild-Fenstermodus standardmäßig aus | Klassik-Feature, aber modernen Fenster-Workflows nicht im Weg |
| `screenPanSpeed` | mittlere Voreinstellung, 3 Stufen wählbar | Maus-Kante + Pfeiltasten + MMB-Drag |
| `bookmarks` | 4 Kamerapositionen (Strg+F1–F4 setzen, F1–F4 springen) | Standard-RTS-Komfort |
| `homeKey` | Leertaste → zuletzt gemeldetes Ereignis; Pos1 → HQ | Schnelles Reagieren ohne Minimap-Suche |

Minimap-Klick und Minimap-Ziehen bewegen die Kamera sofort. Die Kamera folgt nie automatisch Einheiten (kein Auto-Follow); Folgen ist ein expliziter Modus (Taste F auf Auswahl).

## Steuerung (vollständig)

Grundmodell: **Linksklick wählt, Rechtsklick handelt.** Alle Belegungen sind in den Optionen frei umlegbar; unten steht die Werkseinstellung.

### Auswahl

| Aktion | Eingabe | Verhalten |
|---|---|---|
| Einzelauswahl | Linksklick | Einheit/Gebäude wählen; Klick auf Gelände = Auswahl aufheben |
| Auswahlrahmen | Linksklick halten + ziehen | Wählt eigene, steuerbare Einheiten im Rechteck; Gebäude nur, wenn keine Einheiten im Rahmen |
| Hinzufügen/Entfernen | Shift + Klick / Shift + Rahmen | Toggle zur bisherigen Auswahl |
| Typauswahl | Doppelklick auf Einheit | Alle Einheiten desselben Typs im sichtbaren Bildschirmbereich |
| Kampfeinheiten-Bildschirm | Taste E (Hold-Auswahl) | Alle Kampfeinheiten im Bildschirm (ohne Harvester/Drohnen) |
| Ganze Armee | Strg + A | Alle Kampfeinheiten kartenweit – bewusst "grober" Befehl, kein Standardweg |
| Kontrollgruppen | Strg + 0–9 setzen, 0–9 abrufen, Shift + 0–9 hinzufügen | 10 Gruppen; Doppeltippen auf Gruppennummer springt zur Gruppe |
| Gruppen-Status | Gruppen-Icons über der Befehlsleiste | Zeigen Füllstand und Lebenspunkte-Aggregat der Gruppe |

### Befehle (Rechtsklick-Kontextlogik)

Rechtsklick wertet das Ziel aus: eigenes Aetherium-Feld → sammeln (Harvester); Feind → angreifen; Transport → einsteigen; verbündetes beschädigtes Gebäude/Einheit → reparieren (Pionier/Repair-Drohne, D-014); Gelände → bewegen. Fehlklick-Sicherheit: Befehl auf Fog-of-War-Gelände ist immer Bewegen.

| Befehl | Taste | Verhalten |
|---|---|---|
| Bewegen | Rechtsklick / M | Bewegen, ignorieren von Feinden (außer Angriff-auf-Weg-Regel der Einheit) |
| Angriff-Bewegen | A + Linksklick | Unterwegs jeden Feind in Reichweite bekämpfen, dann weiterziehen |
| Patrouille | P + Linksklick | Zwischen aktuellem Punkt und Zielpunkt pendeln, Feinde angreifen |
| Bewachen | W + Ziel | Ziel (Einheit/Gebäude) folgen und Angreifer bekämpfen |
| Stoppen | S | Sofort stehen bleiben, Befehlswarteschlange löschen |
| Halten | H | Position halten, Feinde in Reichweite angreifen, **nicht** verfolgen |
| Streuen | X | Ausgewählte Einheiten verteilen sich leicht (Anti-Splash, Artillerie-Konter); **bleibt enthalten – Evaluierung von Nutzung und Balance nach den Alpha-Playtests** (Festlegung Korrekturlauf Sprint 2) |
| Wegpunkte | Shift + beliebige Befehlsfolge | Bis zu 16 Wegpunkte pro Auftrag; sichtbar als Linienzug während Shift gehalten |
| Rückzug/Sammelruf | R | Auswahl bewegt sich zum nächsten eigenen Lager/Raffinerie-Punkt |

### Aggressions-Modi (Einheiten und Verteidigungsplattformen)

Einheiten und Verteidigungsplattformen (D-008) nutzen **dieselben drei Aggressions-Modi**, umschaltbar über die Befehlskachel der Auswahl (Festlegung Korrekturlauf Sprint 2, löst die C&C-uneinheitliche Sonderbehandlung stationärer Verteidigungen ab):

| Modus | Verhalten |
|---|---|
| Halten | Position halten, Feinde in Reichweite angreifen, **nicht** verfolgen (Taste H) |
| Abwehren (Standard) | Feinde in Reichweite angreifen, kurz verfolgen, dann zum Posten zurückkehren |
| Freies Feuer | Jedes feindliche Ziel in Reichweite bekämpfen, Verfolgung erlaubt |

Verteidigungsplattformen starten im Modus **Abwehren**; der Modus ist pro Plattform einzeln schaltbar und im Modul-Upgrade (MG/Flak/Rakete, D-008) unverändert wirksam.

### Formationen

Formationen sind **automatisch, nicht manuell mikromanagt**: Beim Gruppenbewegungsbefehl ordnet sich die Auswahl selbst – schnelle Einheiten flankieren, schwere Fahrzeuge vorn, Artillerie/Support hinten, Infanterie in Deckungsnähe. Kein Formationen-Menü im Gefecht (Mikro-Prinzip). Drei optionale Formationstasten (Linie / Keil / Block, Tasten F6–F8) überschreiben die Automatik bis zum nächsten Stopp-Befehl. Evolvierte nutzen dieselben Formationslogiken (D-011 ändert Bau, nicht Befehle).

### Bau und Produktion

| Aktion | Eingabe | Verhalten |
|---|---|---|
| Bau-Menü | B oder Sidebar-Klick | Raster der 12 Gebäudetypen (D-008), tier-abhängig ausgegraut |
| Platzierung | Linksklick, Ziehen = Ausrichtung | Kollisions- und Ressourcenabstands-Vorschau (grün/rot); Mauer zieht Linien |
| Evolvierte-Platzierung | identisch | Keim wird gepflanzt statt Fundament gelegt; Reifezeit-Timer sichtbar (D-011) |
| Produktions-Queue | Klick auf Icon in Befehlsleiste | max. 9 Aufträge pro Fabrik sichtbar; Shift-Klick = ×5; Rechtsklick = Abbruch mit 75 % Erstattung |
| Sammelpunkt | Rechtsklick mit ausgewählter Fabrik | Sammelpunkt-Flagge; Wegpunkt-fähig |

### Hotkey-Übersicht (Werkseinstellung)

Q/W/E/R-Reihe für Kontext-Befehlskacheln der aktuellen Auswahl (Positionsraster statt Buchstabensalat), A/S/D/F für die zweite Kachelreihe, Strg+0–9 Gruppen, F1–F4 Kamerapositionen, Pos1 HQ, Leertaste letztes Ereignis, Tab = nächste unbeschäftigte Harvester-/Bau-Einheit (Idle-Worker-Cycle). Vollständige Belegungsliste liegt als Datenprofil vor (`InputProfile` ScriptableObject: flache Liste `action → key`), sodass Layouts (inkl. Grid-Layout als vorgefertigtes Profil) ohne Codeänderung tauschbar sind.

## UI-Grundprinzipien

### HUD-Layout (Standard)

- **Oben links:** Ressourcenleiste – AE-Bestand, AE-Einkommen/min, Energiebilanz (bei Defizit rot blinkend, Low-Power-Regel: Produktion −50 %, Radar/Verteidigung offline – sichtbar als durchgestrichene Icons).
- **Oben rechts:** Matchzeit, Superwaffen-Timer (eigene und gesichtete feindliche).
- **Unten links:** Minimap (Sichtbereichsrahmen, Ereignis-Pings, Teamfarben, Aetherium-Felder als eigene Signalfarbe).
- **Unten rechts:** Befehlsleiste (12 Kacheln Kontextbefehle + Karte der Auswahl: Portrait/Icon, Gesundheit, Veteranen-Rang).
- **Unten Mitte:** Kontrollgruppen-Leiste + Idle-Worker-Anzeige.
- **Rechte Kante:** Bau-/Produktions-Sidebar (klassisches C&C-Vorbild für H1), einklappbar.

### Lesbarkeit

- Maximale HUD-Belegung: ≤ 25 % der Bildschirmfläche im Normalzustand (Ziel 60 FPS und Sicht aufs Geschehen haben Vorrang).
- Drei Informationsstufen pro Element: Symbol → Symbol + Wert bei Hover → Volldetails im Tooltip (progressive Disclosure, kein Zahlenfeuerwerk).
- Einheitliche Farbgrammatik: Grün = eigen/positiv, Rot = feind/negativ, Cyan = Aetherium/Wirtschaft, Amber = Warnung (Low Power, Überernte-Risiko).
- Accessibility (verbindlich, D-007-Zielgruppe 30–45 J.): UI-Skalierung 80–150 %, drei Farbenblind-Profile (Teamfarben werden dann zusätzlich über Form/Symbol getragen), Untertitel für alle Voice-Lines, freie Tastenbelegung, reduzierbare Bildschirmeffekte (Shake, Flash).

### HUD-Dichte

Zwei Profile: **Standard** (alle Elemente) und **Kompakt** (Minimap und Befehlsleiste verkleinert, Gruppenleiste nur bei Hover). Wechsel jederzeit im Match möglich. Tooltips haben zwei Stufen (Kurz / Ausführlich mit Zahlen), Umschaltung in den Optionen; Ausführlich zeigt die ScriptableObject-Rohwerte (Schaden, Panzerung, Kosten in AE, Bauzeit).

**Kommunikation:** Es gibt **keinen Ingame-Voice-Chat** (D-029) – externe Tools decken die Team-Absprache ab; Moderations- und Infrastrukturlast entfällt. Team-Kommunikation im Spiel läuft über Minimap-/Radar-Pings.

## Offene Punkte

- **Aggressions-Modi der Verteidigungsplattformen:** entschieden (Korrekturlauf Sprint 2) – Verteidigungsplattformen nutzen dieselben drei Aggressions-Modi wie Einheiten (Halten/Abwehren/Freies Feuer, s. Abschnitt Befehle); Standard: Abwehren.
- **Streuen (X):** entschieden (Korrekturlauf Sprint 2) – der Befehl bleibt enthalten; Nutzung und Balance werden nach den Alpha-Playtests evaluiert.
- **Kamera-Rotation:** entschieden (D-029) – die Rotations-Option (D-019) wird erst ab Beta ausgeliefert (Art-Aufwand pro Blickwinkel); bis dahin feste Nord-Orientierung.

## Nächste Schritte

- Abgleich der Befehlsliste mit [../gamedesign/Infantry.md](../gamedesign/Infantry.md), [../gamedesign/Vehicles.md](../gamedesign/Vehicles.md) und [../gamedesign/Aircraft.md](../gamedesign/Aircraft.md) (alle Einheitenfähigkeiten müssen über das Befehlsraster erreichbar sein).
- UI-Wireframes (Sprint 2/3) auf Basis des HUD-Layouts; Technik-Abgleich mit [../research/Animation_Audio_UI.md](../research/Animation_Audio_UI.md).
- `InputProfile`- und `CameraProfile`-Datenstrukturen in Sprint 3 mit Technical Design finalisieren.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Gameplay Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030): Kamera-Rotation erst ab Beta (D-029), kein Ingame-Voice-Chat (D-029), D-025-Referenz (Alpha-FFA lokal/Online ab Beta), Aggressions-Modi für Verteidigungsplattformen festgelegt, Streuen mit Playtest-Vorbehalt bestätigt, Links repariert (Units.md → Infantry/Vehicles/Aircraft) | Lead Gameplay Designer |
