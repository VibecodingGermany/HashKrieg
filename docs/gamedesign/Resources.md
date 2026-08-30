# Aetherium – Ressourcen-Regelwerk

**Version:** 0.2.0 | **Status:** Entwurf (Korrekturlauf Sprint 2) | **Verantwortungsbereich:** Lead Gameplay Designer | **Sprint:** 2

## Zweck

Spezifiziert das Aetherium-Feld als lebendiges Spielobjekt: Anatomie, Lebenszyklus, Nachwachsen, Ausbreitung, Überernte, Umweltwirkung und Zerstörbarkeit. Verbindlich für Game Design, Kartenbau (Maps.md), KI (Feldbewirtschaftung) und Technik (Grid-/Datenmodell). Alle Werte sind Richtwerte v0.1 zum Tunen, keine Finalwerte; sie sind bewusst datengetrieben (flache, ScriptableObject-taugliche Datensätze) spezifiziert.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-010 (Hybridwirtschaft), D-012 (gezielte Zerstörbarkeit), D-013 (Wasser nur Terrain-Feature), D-017 (Biome/Hazards), D-027 (Fraktions-Sonderregeln), D-028 (Hazard-Zuordnung)
- [./Economy.md](./Economy.md) – Sammler-Loop, Einkommensraten, Kapazitäten
- [./Factions.md](./Factions.md) – Fraktions-Interaktionen mit Feldern (insb. Evolvierte)
- [./Maps.md](./Maps.md) – Feldplatzierung und -größen pro Karte (geplant)
- [./Buildings.md](./Buildings.md) – Evolvierte-Bau in Aetherium-Nähe (D-011, geplant)
- [./Biomes.md](./Biomes.md) – Wetter/Hazards inkl. Mond/Mars (D-017/D-028); Hazard-Regeln werden ausschließlich dort definiert
- [../analysis/KnowledgeBase.md](../analysis/KnowledgeBase.md) – Quellkontext

## Grundprinzip (gemäß D-010)

Jedes Aetherium-Feld ist ein **Hybrid**: ein endlicher Mutterkristall mit begrenzter Gesamtreserve, umgeben von sichtbaren Ausläufern, die nachwachsen, solange der Mutterkristall lebt. Felder breiten sich langsam aus und verändern das Terrain (Kern-USP). Überernte schädigt den Mutterkristall dauerhaft. Konsequenz für das Match: Map-Control-Druck ohne hartes Ressourcen-Timeout, Ziel-Matchdauer 20–35 Minuten.

## Feldanatomie

| Element | Beschreibung | Daten |
|---|---|---|
| Mutterkristall | Zentrales, nicht erntbares Großobjekt pro Feld; hält die Gesamtreserve; treibt Nachwachsen und Ausbreitung | Reserve in AE; HP; Ausbaustufe 1–3 |
| Ausläufer | Sichtbare, erntbare Kristalle um den Mutterkristall; einzeln abbau- und zerstörbar | Inhalt in AE (max. 300 AE = 1 Harvester-Ladung); Reifegrad 0–100 % |
| Feldfläche (Grid) | Menge der vom Feld belegten Terrain-Tiles; wächst/schwindet mit Ausbaustufe und Überernte | Tile-Set auf dem Simulations-Grid (gleiches Grid wie Pathfinding/FoW, gemäß D-010) |

Der Mutterkristall selbst kann **nicht** geerntet werden – Ertrag entsteht ausschließlich über Ausläufer. Das verhindert "Mutterkristall-Rushing" und macht Felder zu dauerhaften, aber begrenzten Wirtschaftsobjekten.

## Feldgrößen (Richtwerte v0.1)

| Feldtyp | Mutterkristall-Reserve | Ausläufer max. | Ausbaustufe | Einsatz |
|---|---|---|---|---|
| Klein | 4.500 AE | 6 | 1 | Startfeld-Beiwerk, umkämpfte Zwischenfelder |
| Mittel | 9.000 AE | 10 | 2 | Standard-Startfeld und Expansionen |
| Groß | 15.000 AE | 14 | 3 | Zentrales Konfliktfeld / High-Value-Objective |

Begründung: Bei Harvester-Ladung 300 AE (Zahlengerüst) trägt ein mittleres Startfeld ~30 Vollladungen Reserve plus Nachwuchs – genug für die Aufbauphase, aber nicht für das ganze Match; Expansion wird ab ~Min. 8–12 erforderlich (siehe [./Economy.md](./Economy.md), Zielkurven).

## Feldphasen

Ein Feld befindet sich pro Tick in genau einer Phase; die Phase steuert Darstellung und Nachwuchslogik:

| Phase | Bedingung | Verhalten |
|---|---|---|
| Wachsend | Mindestens ein Ausläufer < 100 % Reife | Ausläufer wachsen nach; Ausbreitung aktiv |
| Reif | Alle vorhandenen Ausläufer bei 100 %, Reserve > 0 | Maximale Ernteleistung; Ausbreitung aktiv (bevorzugt: reife Felder streuen neue Ausläufer) |
| Erschöpft | Reserve = 0 | Kein Nachwachsen, keine Ausbreitung; Mutterkristall "verkalkt" (graues, inertes Objekt); vorhandene Rest-Ausläufer bleiben erntbar, bis abgebaut |

Erschöpfte Mutterkristalle bleiben als Terrain-Objekt bestehen (kein Terrain-Umbau, gemäß D-012). Evolvierte-Boni (Regeneration, Reifungs-Beschleunigung) gelten jedoch nur an **lebenden** Feldern – an verkalkten Mutterkristallen entsteht kein Bonus (D-027; konkrete Werte in Buildings.md/Factions.md).

## Nachwachsen – Richtwerte v0.1

| Parameter | Wert | Begründung |
|---|---|---|
| Nachwachsrate pro Ausläufer | 5 AE/s bis 300 AE (≈ 60 s bis voll) | 1 Ladung/Min/Ausläufer; mittleres Feld (10 Ausläufer) regeneriert theoretisch bis zu 3.000 AE/min – deckt 2–3 Harvester dauerhaft, mehr nicht |
| Globales Nachwuchs-Limit | Summe Nachwuchs ≤ Reserve-Rest | Nachwachsen verbraucht die Reserve: jede nachgewachsene AE wird von der Mutterkristall-Reserve abgezogen (1:1) |
| Reifedrossel bei Überernte-Schaden | −25 % Nachwachsrate je Schadensstufe | Koppelt Überernte sichtbar an geringere Feldleistung |

Wichtig: Nachwachsen ist **kein** unendlicher Brunnen – es transferiert Reserve in erntbare Ausläufer. Das unterscheidet Hashkrieg von SupCom (unendlich) und bewahrt C&C-Nähe (endlich), ohne Ressourcen-Timeout.

## Ausbreitungsregeln

| Regel | Wert v0.1 |
|---|---|
| Intervall | Alle 90–120 s prüft ein lebendes Feld (Phase wachsend/reif), ob ein neuer Ausläufer entsteht |
| Radius | Neue Ausläufer entstehen auf freien, geeigneten Tiles im Umkreis von 3 Tiles um bestehende Ausläufer/Mutterkristall |
| Kosten | Jeder neue Ausläufer kostet 300 AE aus der Reserve (Startreife 0 %) |
| Gelände-Filter | Keine Ausbreitung auf: Wasser (D-013), Fels/Steilhang, Straßen/Gebäudefundamente, Brücken; verlangsamt (Intervall ×2) auf: Sand, Schnee/Eis; bevorzugt (Intervall ×0,75) auf: Humus/Wiese, bereits verseuchtem Terrain |
| Maximalausbreitung | Feldfläche ≤ 2× Startfläche; danach nur noch Nachwachsen bestehender Ausläufer |
| Terrainveränderung | Jede neu belegte Tile wird "verseucht" (s. u.; Effekte an das lebende Feld gebunden, D-027) – das ist die sichtbare USP-Umweltveränderung |

Begründung: Langsame, vorhersehbare Ausbreitung erzeugt strategische Feldpflege (Schützen vs. Abblocken mit Mauern/Gebäuden), ohne Pathfinding- oder Simulationsbudget zu sprengen (Intervall-Prüfung statt Kontinuums-Simulation).

## Überernte-Mechanik (gemäß D-010)

Überernte bestraft kurzfristiges Auspressen eines Feldes und schafft die strategische Wahl "schnell jetzt vs. nachhaltig":

| Stufe | Auslöser | Effekt (dauerhaft) |
|---|---|---|
| 0 – Normal | Entnahme ≤ Nachwuchs | – |
| 1 – Strapaziert | Entnahme > Nachwuchs über 60 s kumulativ innerhalb von 5 min | Nachwachsrate −25 %; Reserve verliert zusätzlich 10 % jeder in diesem Zustand entnommenen AE direkt vom Mutterkristall |
| 2 – Ausgeblutet | Stufe 1 erneut erreicht nach Erholungsphase ODER Entnahme > 2× Nachwuchs über 60 s | Nachwachsrate −50 % kumulativ; Zusatzabzug 25 %; Ausbaustufe sinkt um 1 (Ausläufer-Max sinkt, überschüssige Ausläufer verkalken) |
| 3 – Degeneriert | Stufe 2 erneut erreicht | Nachwachsrate −75 %; Feld breitet sich nicht mehr aus; visuell dunkel/rissig |

- Erholung: Kehrt die Entnahme für 120 s unter die Nachwuchsrate, sinkt die Stufe **nicht** zurück – Schäden sind permanent (gemäß D-010 "dauerhaft"); nur der Zusatzabzug-Zähler resettet.
- Auslöser ist allein die Entnahmemenge, unabhängig davon, **wer** erntet: Feindliche Ernte am eigenen Feld löst die Stufen ebenfalls aus (im Korrekturlauf Sprint 2 bestätigt) – Feldvergiftung durch Überernte ist damit eine bewusste Sabotage-Option.
- Anzeige (UX-Pflicht, USP-Anforderung gemäß USP.md): Überernte-Schaden muss für den Spieler **vorhersehbar und sichtbar** sein, sonst trägt der Kern-USP nicht. Konkrete Anforderung: Feldzustand (Phase, Überernte-Stufe, Restreserve) ist jederzeit ohne Mikro-Inspektion lesbar – Farbschimmer je Überernte-Stufe, Feld-Tooltip mit Phase/Reserve/Stufe sowie eine deutliche Vorwarnung (visuell + Audio) vor dem nächsten Stufenaufstieg. Ausarbeitung der UI-/VFX-Regeln ist Sprint-3-Input; verbindliche HUD-Spezifikation folgt in Sprint 4.

## Verseuchtes Terrain – Effekte

Durch Ausbreitung (oder Feldvernichtung) entsteht verseuchtes Terrain. Effekte v0.1:

| Ziel | Effekt |
|---|---|
| Ungeschützte Infanterie (Allianz/Legion) | Leichter Strahlungsschaden (~2 HP/s) beim Stehen/Bewegen auf verseuchten Tiles |
| Fahrzeuge/Luftfahrzeuge/Drohnen | Immun (geschlossene Systeme) |
| Evolvierte (alle Einheiten) | Kein Schaden; stattdessen leichte Regeneration (+1 HP/s) – fraktionsspezifische Asymmetrie, verstärkt deren Heimvorteil an Feldern (Abstimmung mit [./Factions.md](./Factions.md)); gilt **nur auf lebenden Feldern** (Phase wachsend/reif), nicht auf erschöpften (D-027) |
| Bauwerke | Auf verseuchten Tiles ist kein Bau möglich (Allianz/Legion); Evolvierte dürfen bauen und erhalten dort Reifungs-Bonus (D-011, Details in Buildings.md) |
| Bewegung | Kein Speed-Malus (Lesbarkeit/Pathfinding-Budget); Wirkung rein über Schaden/Heilung |

**Bindung an das lebende Feld (D-027):** Alle Effekte dieser Tabelle gelten nur, solange das zugehörige Feld lebt (Phase wachsend/reif). Mit Erschöpfung oder Vernichtung des Feldes wird die Verseuchung **inert** – kein Schaden, keine Regeneration; die Tiles bleiben als verkalkte Terrain-Markierung sichtbar bestehen (kein Terrain-Umbau, D-012). Eine aktive Dekontamination (spielerseitige Reinigung, z. B. per Upgrade) existiert im MVP **nicht** (D-027) – die Verseuchung endet ausschließlich mit Erschöpfung oder Vernichtung des Feldes.

**Hazard-Interaktion (Mond/Mars):** Verseuchung ist ein Feld-Effekt, Wetter/Hazards ein Karten-Effekt; die Hazard-Regeln (Mond: Strahlungsfronten, Mars: Staubstürme, D-028) sind ausschließlich in [./Biomes.md](./Biomes.md) definiert – dieses Dokument definiert keine eigene Hazard-Regel und keine Doppeldefinition.

## Vernichtung von Feldern durch Waffen (gemäß D-012)

| Objekt | HP v0.1 | Regeln |
|---|---|---|
| Ausläufer | 150 | Durch Beschuss zerstörbar; Inhalt verfällt (kein AE-Transfer); Tile bleibt verseucht |
| Mutterkristall | 3.000 (panzerungsähnliche Resistenzen gegen Splitter/Fläche) | Bei Zerstörung: Feld stirbt sofort (Phase erschöpft, keine Rest-Reserve); alle Ausläufer im Umkreis 2 Tiles zerbersten; die entstehende Verseuchungsfläche bleibt als verkalkte Markierung bestehen, wird aber mit dem Tod des Feldes inert (D-027); KEIN Krater/Terrain-Umbau (D-012) |

Begründung: Feldsabotage wird zum echten taktischen Werkzeug (den Gegner von Einkommen abschneiden, Expansionen vergiften), bleibt aber wegen hoher Mutterkristall-HP ein Investitionsziel, kein Beiläufigkeits-Spam.

## Datenmodell (ScriptableObject-tauglich, flach)

Pro Feldinstanz bzw. Felddefinition werden folgende flache Datensätze geführt (Ausprägung in Sprint 3 durch Technik):

| Feld | Typ | Beispiel |
|---|---|---|
| `fieldSizeClass` | enum (S/M/L) | M |
| `motherReserveAE` | int | 9000 |
| `motherHP` | int | 3000 |
| `maxSprouts` | int | 10 |
| `sproutContentAE` | int | 300 |
| `sproutHP` | int | 150 |
| `regenPerSecond` | float | 5.0 |
| `spreadIntervalSec` | float | 105.0 |
| `spreadRadiusTiles` | int | 3 |
| `spreadCostAE` | int | 300 |
| `overharvestThresholdSec` | int | 60 |
| `overharvestReservePenaltyPct` | float | 0.10 |
| `contaminationInfantryDps` | float | 2.0 |
| `contaminationEvolvedHps` | float | 1.0 |

## Offene Punkte

- Keine unentschiedenen Punkte mehr. Die vier Punkte der v0.1.0 wurden im Korrekturlauf Sprint 2 aufgelöst: Dekontaminations-Frage → entschieden, keine aktive Dekontamination im MVP (D-027); Evolvierte-Boni an erschöpften Feldern → entschieden, Boni nur an lebenden Feldern (D-027); Überernte durch Gegner → bestätigt, feindliche Ernte löst die Stufen aus; Mond/Mars-Hazard-Interaktion → per Referenz an [./Biomes.md](./Biomes.md) geklärt (D-028), keine Doppeldefinition hier.
- Tuning-Hinweis (kein Entscheidungsbedarf): Sämtliche Werte bleiben Richtwerte v0.x; Feldgrößen und Überernte-Schwellen werden in den Simulations-Szenarien (s. Nächste Schritte) validiert.

## Nächste Schritte

- Abgleich der Feldgrößen/Ausbreitung mit Maps.md (Feldplatzierung, Abstände Startfeld ↔ Expansion), sobald Maps.md existiert.
- Übergabe der Datenmodell-Felder an Technik (Sprint 3) zusammen mit Grid-Anforderungen aus Pathfinding/FoW.
- Tuning-Testplan: Simulations-Szenarien für 20–35-min-Matches gemeinsam mit [./Economy.md](./Economy.md).

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Gameplay Designer |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 2 (D-020–D-030) | Lead Gameplay Designer |
