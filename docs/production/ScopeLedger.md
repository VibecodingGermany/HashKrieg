# Scope-Ledger der Graybox-Spur

**Version:** 0.2.0 | **Status:** Entwurf – Register (trägt D-067, noch nicht ratifiziert) | **Verantwortungsbereich:** Orchestrator / Technical Writer | **Sprint:** 7

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
| `map.id`, `map.biome` | unbenannte flache Ebene ohne Terrain, Biom oder Hindernisse; nur die Kantenlänge stimmt | G4 (G2: technisch korrektes Testlayout) | D-067 K1, K2 |
| `map.aetheriumFields` | zwei Felder an festen Zellen nahe den Startbasen statt der im Manifest festgelegten Feldliste | G4 (G2) | D-067 K1, K2 |
| `map.primaryRouteCount` | keine Routenführung; die Ebene ist überall passierbar | G4 (G2) | D-067 K1, K2 |
| `factions[1]` | keine Fraktionsidentität; beide Slots benutzen dieselbe Definitionstabelle, Slot 1 stellt nur eine Startbasis | G4 | D-067 K1, K2 |
| `factions[1].identity.harvesterCargoAE` | jeder Harvester benutzt die eine Vorgabe-Ladekapazität aus `UnitState`; im Code als Q-040-Kandidat vermerkt | G4 | D-067 K1, K2 |
| `factions[0].identity.weaponProfile`, `factions[1].identity.weaponProfile` | Rüstungsklassen und Schadensarten existieren seit D-074 (36-Werte-Matrix, `CombatSystem` löst je Angriff darüber auf), aber weiterhin **keine Fraktionsidentität**: beide Slots teilen eine rollenbasierte Waffentabelle. `precision`/`single-target` (Allianz) ist durch Hitscan ohne Flugzeit zufällig erfüllt, `salvo`/`splash` (Legion) fehlt vollständig – es gibt keine Salven- und keine Flächenwirkung | G4 (G2: Kampf über den normalen Pfad) | D-067 K1, K2 |
| `mode.aiSlotCount` | Slot 1 erhält keine Befehle und bleibt untätig; die Ingress stempelt nur den lokalen Slot | G3 | D-067 K1, K2 |
| `victory.evaluationPoint`, `victory.validResultCodes`, `victory.timeLimitTicks` | seit dieser Sitzung in der Simulation erfüllt (`VictorySystem`, achtes und letztes System, alle drei Ergebniscodes, Tick 27.000, Snapshotblock 107). Offen bleibt die **Auswertung außerhalb der Simulation**: der Host tickt nach der Entscheidung unverändert weiter, es gibt keinen Ergebnisbildschirm, und das Ergebnis erscheint nur als Zeile im Debug-HUD. Kein Gate-Nachweis – die Zeile bleibt bis zur auflösenden Evidence stehen | G2 | D-067 K1, K2 |
| `victory.lastUnitReveal.visibleAndTargetable` | der 600-Tick-Zähler nach D-056 ist implementiert, serialisiert und korrekt (`VictorySystem.IsRevealed`), aber **nichts konsumiert ihn**: die enthüllten Einheiten werden weder sichtbar noch zielbar, weil dafür `FogOfWarSystem` (Maskenüberschreibung) und/oder die Zielerfassung des `CombatSystem` das Flag lesen müssten | G2 | D-067 K1, K2 |
| `persistence.pauseRequired` | `MatchRunner.PauseMatch()` existiert, ist aber an keine Eingabe gebunden | G2 | D-067 K1, K2 |
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
kennt nur Rollen und Fraktionen. Die folgenden Verschiebungen entstehen deshalb
aus [`../gamedesign/ArmorSystem.md`](../gamedesign/ArmorSystem.md) über D-074
und können auf keinen Schlüsselpfad zeigen. Sie stehen getrennt, damit die
„Zeigen statt kopieren"-Regel des Hauptregisters unangetastet bleibt. Die
Spalte „Quelle" nennt das führende Fachdokument an Stelle des Schlüsselpfads;
Werte stehen auch hier nicht.

| Gegenstand | Quelle | Substitut / Stand | Rückkehr-Gate | D-ID |
|---|---|---|---|---|
| Schadensart „Kristall" | [`../gamedesign/Infantry.md`](../gamedesign/Infantry.md) (aufgehobene Lokaltabelle) | nicht implementiert und **nicht als Schadensart geführt**: Kristall ist Evolvierten-Inhalt, und die Evolvierten sind keine MS-1-Fraktion. Die Zeile ist aus Infantry.md entfernt, nicht in die führende Matrix übernommen | Post-MVP (nicht vor Einführung der Fraktion Evolvierte) | D-074 |
| Panzerungsklasse `Heavy` | [`../gamedesign/ArmorSystem.md`](../gamedesign/ArmorSystem.md) | Spalte ist vollständig implementiert, hat aber **keinen Träger in MS-1**: ArmorSystem.md ordnet Leichten und Kampfpanzer beide `Medium` zu und reserviert `Heavy` für den Heavy Tank, der nicht im MS-1-Roster steht. Die Spalte wird in keinem Match ausgewertet | Post-MVP (mit dem Heavy Tank / Eliten) | D-074 |
| Panzerungsklasse `Air` | [`../gamedesign/ArmorSystem.md`](../gamedesign/ArmorSystem.md) | Spalte implementiert, kein Träger: MS-1 hat kein Luftroster und keine Zielklassen-Trennung Boden/Luft | Post-MVP (mit [`../gamedesign/Aircraft.md`](../gamedesign/Aircraft.md)) | D-074 |
| Schadensarten Feuer, Bio, Strahlung | [`../gamedesign/ArmorSystem.md`](../gamedesign/ArmorSystem.md) | drei der sechs Matrixzeilen sind implementiert, aber unbespielt: das MS-1-Roster führt ausschließlich Kinetisch und Explosiv. Bewusst mitgeführt statt herausgeschnitten – ein späteres Nachschneiden der Tabelle wäre teuer, das Mitführen kostet nichts | Post-MVP | D-074 |

Bewusst **nicht** registriert: die Post-MVP-Anteile von
[`../gamedesign/VictoryConditions.md`](../gamedesign/VictoryConditions.md)
(`VictoryProfile`-ScriptableObject, konfigurierbare Zeitlimits, Aufgabe durch
Spieler oder KI, Stall-Erkennung, Team-/FFA-/Survival-/King-of-the-Hill-Regeln,
Ergebnisstatistik). Der D-056-MS-1-Override schließt sie ausdrücklich aus; sie
bleiben also nicht hinter dem verbindlichen Inhalt zurück, sondern liegen
außerhalb davon. Dieses Register führt nur Rückstände gegenüber MS-1.

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
