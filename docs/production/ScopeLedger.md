# Scope-Ledger der Graybox-Spur

**Version:** 0.1.0 | **Status:** Entwurf – Register (trägt D-067, noch nicht ratifiziert) | **Verantwortungsbereich:** Orchestrator / Technical Writer | **Sprint:** 7

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
- [DecisionLog.md](DecisionLog.md) – D-067 (Klauseln K1–K5), D-068
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
| `factions[0].identity.weaponProfile`, `factions[1].identity.weaponProfile` | `CombatSystem` wendet einen einzigen flachen Schadenswert an; keine Rüstung, keine Schadenstypen, kein Salven-/Splash-Verhalten – Kampf ist im Graybox **nicht** bewertbar | G4 (G2: Kampf über den normalen Pfad) | D-067 K1, K2 |
| `mode.aiSlotCount` | Slot 1 erhält keine Befehle und bleibt untätig; die Ingress stempelt nur den lokalen Slot | G3 | D-067 K1, K2 |
| `victory.evaluationPoint`, `victory.validResultCodes`, `victory.timeLimitTicks` | keine Siegauswertung; ein Graybox-Match kann nicht enden | G2 | D-067 K1, K2 |
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

## Offene Punkte

- D-067 ist ein Entwurf. Ohne Ratifizierung deckt keine Klausel diese Zeilen –
  dann sind es unregistrierte Abweichungen statt befristeter Verschiebungen.
- Das Register erhebt keinen Anspruch auf Vollständigkeit für Bereiche, die
  die Graybox gar nicht berührt (Audio, Art, Lizenzprovenienz, Telemetrie).
  Es deckt, was die Spur tatsächlich angefasst oder ersetzt hat.
- Ob `accessibility.colorAndShapeRedundancyRequired` mit der echten UI
  weiterhin erfüllt ist, entscheidet erst der G4-Stand; die Graybox erfüllt
  nur den Grundsatz, nicht die Umsetzung.

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
