# Scope-Ledger der Graybox-Spur

**Version:** 0.6.0 | **Status:** laufendes Register – Graybox-Entwurf D-067 plus verbindliche 12B-Abweichungen D-090 | **Verantwortungsbereich:** Orchestrator / Technical Writer | **Sprint:** 12

## Zweck

Ein Register aller Stellen, an denen die Graybox-Spur hinter dem verbindlichen
MS-1-Inhalt zurückbleibt. Eine Zeile je Verschiebung: **worauf** im Manifest
sie sich bezieht, **womit** die Graybox ersatzweise arbeitet, **wo** sie
zurückkommt und **welche** D-ID-Klausel sie deckt.

**Zeigen statt kopieren.** Jede Zeile nennt ausschließlich den
Schlüsselpfad in
[`../../quality/content/mvp-v1.json`](../../quality/content/mvp-v1.json), nie
dessen Wert. Ein kopierender Ledger wird zur zweiten, driftenden Quelle für
Zahlen; ein zeigender kann das nicht. Das Manifest bleibt byte-identisch und
ist die einzige Autorität für Werte.

Dieses Dokument ist **kein Gate-Nachweis** (D-067 K1). Es beweist nichts
Erreichtes; es macht Fehlendes zählbar.

## Abhängigkeiten

- [`../../quality/content/mvp-v1.json`](../../quality/content/mvp-v1.json) –
  kanonisches MS-1-Manifest, Autorität für alle Werte; unberührt
- [MVPContentManifest.md](MVPContentManifest.md) – Prosa-Erklärung dazu
- [MVPRecoveryPlan.md](MVPRecoveryPlan.md) – Definition der Gates G2–G5
- [DecisionLog.md](DecisionLog.md) – D-067 (Klauseln K1–K5), D-068, D-074
  (Matrixautorität; Quelle der Anhang-Zeilen)
- [`../gamedesign/ArmorSystem.md`](../gamedesign/ArmorSystem.md) – führende
  Quelle der Schaden-gegen-Panzerung-Matrix (D-074)
- [GrayboxLog.md](GrayboxLog.md) – Sitzungsprotokoll der Spur

## Register

Lesart der Spalte „Rückkehr-Gate": das Gate, dessen Kriterien die Verschiebung
auflösen. Steht dort ein zweites Gate in Klammern, verlangt dieses den
funktionalen Anteil, das genannte Gate den vollständigen Inhalt.

| Manifest-Schlüsselpfad | Graybox-Substitut | Rückkehr-Gate | D-ID-Klausel |
|---|---|---|---|
| `startStatePerPlayer.unitRoles` | Startaufstellung des Determinismus-Szenarios portiert; zusätzlich vier Infanterieeinheiten je Slot, damit überhaupt etwas zu sehen ist | G4 | D-067 K1, K2 |
| `map.id`, `map.biome` | die Karte heißt seit GB-003 Glutrinne und zeigt einen Wüsten-Blockout (Sandtönung, Kartenrand-Rahmen, Kristallmarker – reine Präsentation über `GlutrinneBlockoutView` plus Datenasset `MAP_Glutrinne.asset`); weiterhin kein Terrain-/Biom-System und keine Hindernisse, nur die Kantenlänge stimmt | G4 (G2: technisch korrektes Testlayout) | D-067 K1, K2 |
| `map.aetheriumFields` | zwei Felder an festen Zellen nahe den Startbasen statt der im Manifest festgelegten Feldliste; seit GB-003 als Kristallmarker sichtbar, Reserven und Verhalten unverändert | G4 (G2) | D-067 K1, K2 |
| `map.primaryRouteCount` | keine Routenführung; die Ebene ist überall passierbar | G4 (G2) | D-067 K1, K2 |
| `factions[1]` | seit dieser Sitzung Simulationswirklichkeit: 34 fraktionsaufgelöste Definitionen (`SimDefinitions`, Id-Regel und Provenienz per D-075), Slot-Fraktion im Economy-Snapshotblock v2 mit `SetSlotFaction`-Guard, fraktionsaufgelöste Kosten/Bauzeiten/Energie/Waffenwerte, Graybox-Farben aus den D-072-Paletten im `UnitViewManager` und Slot-Fraktionen im Debug-HUD. Verbleibend: das `weaponProfile` der Identität (eigene Zeile) und der untätige KI-Slot (eigene Zeile). Kein Gate-Nachweis — die Zeile bleibt bis zur auflösenden Evidence | G4 | D-067 K1, K2 |
| `factions[1].identity.harvesterCargoAE` | seit dieser Sitzung im Code erfüllt: die Kapazität lebt in der Harvester-Definitionszeile (`SimUnitDefinition.CargoCapacityAE`), das `EconomySystem` klammert die Ernte fraktionsaufgelöst; die Entity-Store-Snapshotvalidierung deckelt auf das fraktionsübergreifende Maximum und bietet die pro-Entity-Fraktionsgrenze als Überladung. Kein Gate-Nachweis — die Zeile bleibt bis zur auflösenden Evidence | G4 | D-067 K1, K2 |
| `factions[0].identity.weaponProfile`, `factions[1].identity.weaponProfile` | Waffentabelle ist seit dieser Sitzung fraktionsaufgelöst (34 Definitionen, `WeaponProfiles` je Fraktion und Rolle, konkrete Vehicles.md-Schadenswerte der drei Legion-Kampffahrzeuge per D-075); `precision`/`single-target` (Allianz) ist durch Hitscan ohne Flugzeit zufällig erfüllt, `salvo`/`splash` (Legion) fehlt weiterhin vollständig — es gibt keine Salven- und keine Flächenwirkung. **Bewusster Konflikt, nicht implementiert** (Splash/Projektile sind nicht Teil dieses Strangs) | G4 (G2: Kampf über den normalen Pfad) | D-067 K1, K2 |
| `mode.aiSlotCount` | Slot 1 spielt seit D-077 mit (feste Build-Order, Infanterie-Wellen, FoW-legal); **offen bleibt** der volle MS-1-Vertrag: alle Gebäude-/Einheitenrollen, Aufklärung, Reaktion auf den Spieler, Sidecar-/TeamWorldView-Typen | G3 | D-067 K1, K2, D-077 |
| `victory.evaluationPoint`, `victory.validResultCodes`, `victory.timeLimitTicks` | seit dieser Sitzung in der Simulation erfüllt (`VictorySystem`, achtes und letztes System, alle drei Ergebniscodes, Tick 27.000, Snapshotblock 107). Offen bleibt die **Auswertung außerhalb der Simulation**: der Host tickt nach der Entscheidung unverändert weiter, es gibt keinen Ergebnisbildschirm, und das Ergebnis erscheint nur als Zeile im Debug-HUD. Kein Gate-Nachweis – die Zeile bleibt bis zur auflösenden Evidence stehen | G2 | D-067 K1, K2 |
| `victory.lastUnitReveal.visibleAndTargetable` | der 600-Tick-Zähler nach D-056 ist implementiert, serialisiert und korrekt (`VictorySystem.IsRevealed`), aber **nichts konsumiert ihn**: die enthüllten Einheiten werden weder sichtbar noch zielbar, weil dafür `FogOfWarSystem` (Maskenüberschreibung) und/oder die Zielerfassung des `CombatSystem` das Flag lesen müssten | G2 | D-067 K1, K2 |
| `persistence.pauseRequired` | `MatchRunner.PauseMatch()` ist seit GB-004 an die Taste P gebunden (Toggle über `StartMatch()` zurück); kein Pausenmenü, keine Eingabesperre jenseits des Tick-Stopps | G2 | D-067 K1, K2 |
| `persistence.manualSlotCount`, `persistence.quicksaveRotation`, `persistence.autosaveSlotCount`, `persistence.backupRecoveryRequired` | kein Save/Load in der Bedienschicht; der Kernel kann Snapshots, es gibt keine Slot-Verwaltung | G4 (G3: identische Fortsetzung) | D-067 K1, K2 |
| `accessibility.inputRebindingRequired` | feste Tastenbelegung im Code (Legacy-Input) | G4 | D-067 K1, K2 |
| `accessibility.uiScalePercent` | Debug-HUD skaliert die GUI-Matrix mit einem festen Faktor, ohne einstellbaren Bereich | G4 | D-067 K1, K2 |
| `accessibility.colorAndShapeRedundancyRequired` | im Substitut bereits eingehalten: Form kodiert Rolle, Farbe kodiert Spieler-Slot – aber auf Laufzeitprimitiven statt auf echter UI | G4 | D-067 K1, K2 |
| `accessibility.reducedShakeRequired`, `accessibility.reducedFlashRequired` | keine Optionen vorhanden; die Graybox erzeugt allerdings auch keine Shake-/Flash-Effekte | G4 | D-067 K1, K2 |
| `accessibility.clientCommandFeedbackMaximumMs` | HUD zeigt das Verdikt des letzten Befehls als Text; nichts davon ist gemessen | G4 | D-067 K1, K2 |
| `acceptance.normalMatchUiOnly` | Bedienung läuft über eine `OnGUI`-Debugüberlagerung, die der Recovery-Plan §5 für das Gate ausdrücklich ausschließt | G4 (G2) | D-067 K1, K2 |
| `capacity.productionUnitCapTotal` | Produktion prüft nur die Entity-Store-Grenze, nicht die Produktionsobergrenze | G4 | D-067 K1, K2 |
| `aetherium.regrowthConsumesReserve`, `aetherium.spreadEnabled`, `aetherium.terrainConsequenceEnabled`, `aetherium.permanentOverharvestDamage`, `aetherium.readableStateAndWarningRequired` | Felder sind endlich und statisch: kein Nachwachsen, keine Ausbreitung, kein Überernteschaden, keine Warnung; im Quellcode als G2-Reservierung vermerkt | G2 | D-067 K1, K2 |
| `aetherium.aiManagementRequired`, `aetherium.contestedExpansionRequired` | kein KI-Feldmanagement, keine umkämpfte Expansion, weil Slot 1 nicht spielt | G3 | D-067 K1, K2 |
| `defenseModules[0]`, `defenseModules[1]` | Einbau von Verteidigungsmodulen wird von der Domänenprüfung abgelehnt; der Dispatcher bietet den Befehl bewusst nicht an, statt eine nie erfüllbare API vorzutäuschen | G4 | D-067 K1, K2 |

### Anhang: Verschiebungen ohne Manifest-Schlüsselpfad (D-074)

Das Manifest modelliert Schadensarten und Panzerungsklassen **nicht** – es
kennt nur Rollen und Fraktionen. Die folgenden Verschiebungen können deshalb
auf keinen Schlüsselpfad zeigen: sie entstehen aus den Fachdokumenten, aus
[`../gamedesign/ArmorSystem.md`](../gamedesign/ArmorSystem.md) über D-074
sowie aus den Einheiten- und Waffentabellen über D-047/D-075. Sie stehen
getrennt, damit die „Zeigen statt kopieren"-Regel des Hauptregisters
unangetastet bleibt. Die Spalte „Quelle" nennt das führende Fachdokument an
Stelle des Schlüsselpfads; Werte stehen auch hier nicht.

| Gegenstand | Quelle | Substitut / Stand | Rückkehr-Gate | D-ID |
|---|---|---|---|---|
| Schadensart „Kristall" | [`../gamedesign/Infantry.md`](../gamedesign/Infantry.md) (aufgehobene Lokaltabelle) | nicht implementiert und **nicht als Schadensart geführt**: Kristall ist Evolvierten-Inhalt, und die Evolvierten sind keine MS-1-Fraktion. Die Zeile ist aus Infantry.md entfernt, nicht in die führende Matrix übernommen | Post-MVP (nicht vor Einführung der Fraktion Evolvierte) | D-074 |
| Panzerungsklasse `Heavy` | [`../gamedesign/ArmorSystem.md`](../gamedesign/ArmorSystem.md) | Spalte ist vollständig implementiert, hat aber **keinen Träger in MS-1**: ArmorSystem.md ordnet Leichten und Kampfpanzer beide `Medium` zu und reserviert `Heavy` für den Heavy Tank, der nicht im MS-1-Roster steht. Die Spalte wird in keinem Match ausgewertet | Post-MVP (mit dem Heavy Tank / Eliten) | D-074 |
| Panzerungsklasse `Air` | [`../gamedesign/ArmorSystem.md`](../gamedesign/ArmorSystem.md) | Spalte implementiert, kein Träger: MS-1 hat kein Luftroster und keine Zielklassen-Trennung Boden/Luft | Post-MVP (mit [`../gamedesign/Aircraft.md`](../gamedesign/Aircraft.md)) | D-074 |
| Schadensarten Feuer, Bio, Strahlung | [`../gamedesign/ArmorSystem.md`](../gamedesign/ArmorSystem.md) | drei der sechs Matrixzeilen sind implementiert, aber unbespielt: das MS-1-Roster führt ausschließlich Kinetisch und Explosiv. Bewusst mitgeführt statt herausgeschnitten – ein späteres Nachschneiden der Tabelle wäre teuer, das Mitführen kostet nichts | Post-MVP | D-074 |
| Tempo-Umrechnung m/s ↔ m/tick | [`../gamedesign/Vehicles.md`](../gamedesign/Vehicles.md), [`../gamedesign/Infantry.md`](../gamedesign/Infantry.md) (m/s-Tabellen) | die GDDs führen Tempo in m/s, die Simulation rechnet m/tick; die Umrechnung ist unratifiziert, deshalb tragen beide Fraktionen die provisorischen m/tick-Bestandswerte — die Fraktionsachse (D-075) differenziert Tempo bewusst nicht | G4 | offen (keine D-ID) |
| Harvester-Panzerungsklasse | [`../gamedesign/Vehicles.md`](../gamedesign/Vehicles.md) (Fahrzeugtabelle) vs. [`../gamedesign/ArmorSystem.md`](../gamedesign/ArmorSystem.md) | die Fahrzeugtabelle stuft den Harvester höher ein als die Simulation (`ArmorClass.Light`); ArmorSystem.md nennt den Harvester gar nicht. Konflikt registriert, nicht entschieden | G4 | D-074 (Autorität), Zuordnung offen |
| Allianz-Schadenstyp der Fahrzeugwaffen | [`../gamedesign/Vehicles.md`](../gamedesign/Vehicles.md) / [`../gamedesign/Infantry.md`](../gamedesign/Infantry.md) (Tabellen nennen Energie) vs. [`../gamedesign/Weapons.md`](../gamedesign/Weapons.md) | die Simulation führt die Allianz-Fahrzeugwaffen als Kinetisch — Weapons.md ist per D-047 führend für Waffenwerte und gewinnt derzeit; die Energie-Zeilen der Einheitentabellen stehen als registrierter Konflikt | G4 | D-047 |

Bewusst **nicht** registriert: die Post-MVP-Anteile von
[`../gamedesign/VictoryConditions.md`](../gamedesign/VictoryConditions.md)
(`VictoryProfile`-ScriptableObject, konfigurierbare Zeitlimits, Aufgabe durch
Spieler oder KI, Stall-Erkennung, Team-/FFA-/Survival-/King-of-the-Hill-Regeln,
Ergebnisstatistik). Der D-056-MS-1-Override schließt sie ausdrücklich aus; sie
bleiben also nicht hinter dem verbindlichen Inhalt zurück, sondern liegen
außerhalb davon. Dieses Register führt nur Rückstände gegenüber MS-1.

## Sprint 12 Strang B – Planabweichungen (D-090)

Dieser getrennte Abschnitt kopiert keine Werte aus dem MS-1-Manifest. Er hält
die vom Inhaber verlangten Abweichungen zwischen dem historischen
[12B-Ausführungsplan](hashkrieg/12B_Sprint_Sichtbares_Gefecht.md) und dem
implementierten Gefechtsfeedback fest. D-090 ist die führende Entscheidung.

| Planpunkt | Ausgeführter Stand | Rückkehr / Restprüfung |
|---|---|---|
| Prozeduraler radialer Partikelverlauf | Unity-Partikelsysteme, Meshes und Laufzeitmaterialien ohne erzeugte oder importierte Textur | nur bei nachgewiesenem visuellen Bedarf neu bewerten |
| Sichtbares Projektil | höchstens 0,1-s-Hitscan-Spur mit beim Auslösen kopiertem Endpunkt; kein Nachführen und keine Flugzeit | Vertrag bleibt, solange Kampf Hitscan ist |
| Differ direkt in zwei parallelen Slot-Arrays | dedizierter `VisibleCombatFrameDiffer` mit vollständiger `EntityId`, Fog-Sicht und `TryGetUnit` | kein Rückbau geplant |
| Tod aus jedem sichtbaren Verschwinden | sichere, bewusst unvollständige Heuristik: eigene mobile Einheiten direkt; Gebäude/fremde Einheiten nur bei Tickdelta 1 und genau einem sichtbaren korrelierten Schuss; Mehrdeutiges bleibt stumm | bessere Ereignisquelle nur mit eigener Entscheidung, ohne Fog-Leak |
| Gebäudetrümmer-/Decal-Fläche | Rauch, Trefferstoß und 0,8-s-Absacken; kein persistenter Rückstand | Content-/Performance-Entscheidung nach visueller Abnahme |
| direkter `AudioSource.PlayOneShot` im Effektcontroller | D-039-konformer Aufruf über `IAudioService`/`UnityAudioService` | historische Musikcontroller separat migrieren |
| `ALR_BaseUnderAttack` in Tier 0 | ausgelassen: Tier 1, keine ausgewählte Quelle und kein auditiv abgenommener 20-s-Vertrag | späterer Audio-Tier-1-Sprint |
| semantisch umbenannte WAV-SFX | genau 35 unveränderte Kenney-OGGs in pack-first-Ablage, 11 Sci-Fi / 11 Impact / 13 Interface | Quelldateien und Hashes bleiben stabil |
| optionale Flipbook-Stufe 5 | ausgelassen | nur bei belegtem Mehrwert nach Stufen 1–4 |
| B5-A/B-State-Hash-Test mit Effektschalter | headless Quellcode-Guard für Produktionsquellen außerhalb `Simulation/**`; `RawUnits` wegen bestehender Altlasten nicht global gesperrt | Guard bei neuen Präsentationspfaden erweitern |
| vollständige Musikprovenienz | ein Musik-Sidecar vorhanden, alle vier Datensätze ehrlich `incomplete`; fehlende lokale Ursprungsdateien/Befehle bleiben benannt | Inhaber liefert nur echte Originalbelege nach |
| Vier-Augen-Prüfer im Sidecar | `verifiedBy` bleibt im Tier-1-Zweierbetrieb mit Begründung leer | zweiten realen Prüfer nachtragen, falls verfügbar |
| Fokuspunkt-Listener | vorhandener Kamera-Listener bleibt bestehen | per Gegenhören mit Fokuspunkt vergleichen |
| lückenlose Cues nach jedem Tick | Tick-Sprünge dürfen Zwischen-Cues verlieren; es gibt kein nachträgliches Effektgewitter | bei künftigem Sim→View-Eventstrom neu bewerten |
| öffentliche Unity-Mixer-Authoring-API | idempotentes Editorwerkzeug nutzt reflektierte Unity-6000.5.4f1-Interna und bricht bei Signaturdrift hart ab | bei Unity-Upgrade gezielt validieren |
| separater Effekt-Schalter | nicht umgesetzt; der geplante B5-Zweck wird durch den Quellcode-Guard erfüllt | nur mit eigenem Accessibility-/Performance-Bedarf |

## Offene Punkte

- D-067 ist ein Entwurf. Ohne Ratifizierung deckt keine Klausel diese Zeilen –
  dann sind es unregistrierte Abweichungen statt befristeter Verschiebungen.
- Das Register erhebt keinen Anspruch auf Vollständigkeit für Bereiche, die
  die Graybox gar nicht berührt (Audio, Art, Lizenzprovenienz, Telemetrie).
  Es deckt, was die Spur tatsächlich angefasst oder ersetzt hat.
- Ob `accessibility.colorAndShapeRedundancyRequired` mit der echten UI
  weiterhin erfüllt ist, entscheidet erst der G4-Stand; die Graybox erfüllt
  nur den Grundsatz, nicht die Umsetzung.
- Die vier Anhang-Zeilen hängen an D-074, das **vom Agenten unter
  Inhaber-Delegation** entschieden wurde. Stimmt der Inhaber die
  Matrixautorität um, ändern sich Zuschnitt und Zahl dieser Zeilen; das
  Hauptregister bleibt davon unberührt.

## Nächste Schritte

1. Register bei jeder weiteren Graybox-Sitzung fortschreiben; neue
   Verschiebungen kommen als Zeile dazu, aufgelöste werden mit dem
   auflösenden Gate-Nachweis entfernt.
2. Zum Verfall der Spur (D-067 K5) prüfen, ob jede Zeile entweder aufgelöst
   oder von einem geplanten Gate-Arbeitspaket abgedeckt ist.
3. Vor G4 die Zeilen gegen das dann geltende Manifest neu abgleichen –
   Schlüsselpfade können sich ändern, Werte gehören weiterhin nicht hierher.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-26 | Erstfassung mit 21 Registerzeilen aus Sitzung GB-001 | Technical Writer |
| 0.2.0 | 2026-07-26 | Sitzung GB-002: Zeile `victory.lastUnitReveal.visibleAndTargetable` ergänzt; die Zeilen zu `victory.*` und `weaponProfile` auf den durch Kampf- und Siegsystem veränderten Stand fortgeschrieben (nicht entfernt – es gibt keinen auflösenden Gate-Nachweis); Anhang mit vier Zeilen ohne Manifest-Schlüsselpfad aus D-074 („Kristall", `Heavy`, `Air`, Feuer/Bio/Strahlung) | Technical Writer |
| 0.3.0 | 2026-07-26 | Fraktions-Sitzung: Zeilen `factions[1]`, `factions[1].identity.harvesterCargoAE` und `weaponProfile` auf den Stand der Fraktions-Achse (D-075) fortgeschrieben — Cargo und Waffentabelle sind implementiert, `salvo`/`splash` bleibt als bewusster Konflikt registriert; drei Anhang-Zeilen ergänzt (Tempo-Umrechnung m/s ↔ m/tick, Harvester-Panzerungsklasse Vehicles vs. Simulationszuordnung, Allianz-Schadenstyp Energie vs. Kinetisch — D-047 gewinnt derzeit); Anhang-Einleitung über D-074 hinaus auf D-047/D-075 verbreitert | Technical Writer |
| 0.4.0 | 2026-08-05 | Sitzung GB-003: Zeilen `map.id`/`map.biome` und `map.aetheriumFields` auf den Stand des Glutrinne-Blockouts fortgeschrieben (Wüsten-Präsentation und Kristallmarker, weiterhin kein Terrain-System und nur zwei registrierte Felder; nicht entfernt – es gibt keinen auflösenden Gate-Nachweis) | Technical Writer |
| 0.5.0 | 2026-08-05 | Sitzung GB-004: Zeile `persistence.pauseRequired` fortgeschrieben (Pause an P gebunden; kein Pausenmenü – Zeile bleibt bis G2) | Technical Writer |
| 0.6.0 | 2026-08-08 | Separaten D-090-Abschnitt mit sämtlichen bekannten Abweichungen des ausgeführten Sprint-12-Strangs B ergänzt; Graybox-Hauptregister und Manifestverweise unverändert erhalten | Technical Writer / Agent (Umsetzung) |
