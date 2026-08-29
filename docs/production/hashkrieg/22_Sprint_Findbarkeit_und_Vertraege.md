# Sprint 22: Was der Betatest gebrauchen kann — Findbarkeit, Verträge, Aufräumen

**Version:** 1.0.0 | **Status:** in Arbeit | **Verantwortungsbereich:** Maintainer-Strang | **Sprint:** 22 | **Vorgänger:** [21_Sprint_Verknappungsfolgen.md](21_Sprint_Verknappungsfolgen.md) | **Parallel zu:** [13B](13B_Sprint_Einheitenverhalten.md) | **UX-Gate:** human | **Leitsatz:** ein Nachweis, den niemand einsammelt, ist kein Nachweis — und eine Einheit, die man nicht wiederfindet, ist keine Einheit

## Zweck

Sprint 21 hat die Verknappung sichtbar und die Karte bespielbar gemacht. Was
danach zwischen dem Bestand und einem brauchbaren Betatest steht, ist nicht ein
großes Ding, sondern drei kleine, die alle dieselbe Eigenschaft haben: sie sind
**heute schon kaputt oder fehlend**, und niemand merkt es, weil nichts sie
prüft.

Dieser Sprint ist deshalb bewusst schmal. Er baut kein neues System.

## Herkunft dieser Datei

Geschnitten am 29.08.2026 vom Orchestrator während einer autonom laufenden
Sitzung, mit ausdrücklicher Vollmacht des Inhabers, Produktentscheidungen selbst
zu treffen und alles über PRs nach `main` zu führen. Die Entscheidungen, die
dabei gefallen sind, stehen unten unter „Autonom getroffene Entscheidungen" —
sie sind **revidierbar** und ausdrücklich zur Nachprüfung ausgewiesen.

Quellen: Testbericht T-01 vom 09.08.2026 (#50), das Umbenennungs-Inventar aus
Sprint 21 (`reports/v8.6.0/umbenennung-hashkrieg/01-kimi-inventar.md`), und die
Nebenbefunde der Sprint-21-Arbeiter.

## Pakete

### 22.1 · Die Auswahl wird benutzbar (#50) — **Beta-Tor**

Aus dem Betatest, und die Folge ist kein Komfortproblem:

> „Weil ich den Pionier in der Gruppe nicht wiederfand, **konnte ich nicht
> bauen**."

Paket 21.5 hat die Aufstellung geliefert — die Befehlskarte schlüsselt die
Auswahl nach Typ auf. Sie ist nur nicht *benutzbar*: man sieht, was markiert
ist, kann es aber nicht anfassen.

Drei Dinge:

1. **Die Typzeile filtert die Auswahl.** Klick auf „2× Lynx — 180/240 HP"
   reduziert die Auswahl auf diese zwei. Der billigste Weg vom Sehen zum
   Arbeiten, weil die Aufstellung schon da ist.
2. **Doppelklick wählt alle sichtbaren Einheiten derselben Rolle.**
   RTS-Konvention. „Sichtbar" heißt im Kamerabild, nicht auf der ganzen Karte —
   das wäre ein anderer Befehl und würde überraschen.
3. **Eine Taste springt zum nächsten unbeschäftigten Pionier** und zentriert die
   Kamera auf ihn. Ohne das Zentrieren findet man ihn genauso wenig wie vorher.
   Mehrfaches Drücken geht reihum in aufsteigender Entitäts-Reihenfolge.

> **Zwei Nachträge pro neuer Trefferfläche.** `CommandCardHud.EstimateHeight`
> bildet die Höhenrechnung von `OnGUI` Zeile für Zeile nach — der Kommentar dort
> dokumentiert genau diesen Fehler. Und jede neue Trefferfläche gehört in
> `IsPointerOverHud`, sonst schlagen Klicks hinter dem Panel in die Welt durch.

**Kein Simulationseingriff.** Alle Daten liegen im `EntityManager`. Kein neuer
`CommandKind`, keine `RulesHash64`-Bewegung.

**Fertig wenn:** ein Spieler mit dreißig Einheiten auf einem Haufen seinen
Pionier in unter drei Sekunden findet, ohne die Kiste neu zu ziehen.

### 22.2 · Der Gate-Vertrag zeigt auf das richtige Repo (#14, Stufe 1)

Das GitHub-Repo heißt seit dem 09.08.2026 `VibecodingGermany/HashKrieg`. Der
Qualitäts-Gate-Vertrag pinnt weiter den alten Namen als harte Konstante — in
beiden Schemas und im Validator.

Niemandem aufgefallen ist es, weil der Selbsttest des Validators dieselbe alte
Konstante in seinen eigenen Fixtures benutzt: er ist mit sich selbst konsistent
und darum grün. Der echte Autorisierungspfad läuft heute auf „skipping" —
sobald er läuft, bekommt er `github.repository` = `HashKrieg` und fällt am
`const` durch.

**Fertig wenn:** der Selbsttest beide zulässigen Namen einmal durchläuft, statt
nur seine eigene Konstante zu bestätigen.

### 22.3 · Die fünfte und sechste Spiegelstelle (Nachtrag zu R-1)

Sprint 21 hat beim Umsetzen von 21.6 herausgefunden, dass die kanonische
Feldlage nicht an **vier** Stellen literal im Repo steht, wie R-1 annahm,
sondern an **sechs**. Fünf sind mitgezogen. Die sechste —
`Assets/_Project/Editor/BootstrapSceneGenerator.cs:296` — bäckt die alte
Fünf-Felder-Lage in `MapDefinitionSO` und trägt den jetzt falschen Kommentar
„the five fields MatchBootstrap registers".

Sie ist **heute folgenlos**: `MapDefinitionSO` hat außerhalb von `Editor/`
keinen Laufzeitkonsumenten. Genau deshalb gehört sie aufgeräumt, statt
liegenzubleiben — eine stille falsche Kopie ist die Sorte Fehler, die beim
nächsten Kartenwechsel jemanden einen halben Tag kostet.

Zu klären ist dabei die eigentliche Frage: **warum gibt es `MapDefinitionSO`,
wenn es niemand liest?** Entweder es bekommt einen Konsumenten, oder es
verschwindet. Ein drittes gibt es nicht.

### 22.4 · Das Gate läuft grün über Assemblies, die es nicht gibt

`quality/scripts/run_gate_check.py:86-87` führt `Nova.Presentation.Maps` und
`Nova.Presentation.Shaders` in seiner Schichtenkarte. Es gibt keine solchen
`.asmdef`; die beiden `.csproj` im Repo-Wurzelverzeichnis sind untrackte
Unity-Reste.

Das Gate prüft also Schichtgrenzen von Phantom-Assemblies — und meldet dafür
Erfolg. Das ist dieselbe Klasse Fehler wie 22.2: ein Prüfer, der sich selbst
bestätigt.

## Bewusst nicht in diesem Sprint

| Was | Warum |
|---|---|
| **#108 `RulesHash64` deckt die Regeln nicht ab** | Braucht eine `RulesRevisionV4` und damit einen Eingriff in `Simulation/Replays/` — im Parallelbetrieb als „niemand ohne D-ID" geführt. Der Fingerabdruck bewegt sich dabei, und **jeder verteilte Testbuild wird ungültig**. Unmittelbar vor einem Betatest ist das der falsche Moment. Praktisch abgefedert bleibt es ohnehin: die Lobby vergleicht seit D-092/D-094 den Build-Commit. **Sprint 21 hat den Befund allerdings verschärft** — nicht nur die Ankerregel, auch eine komplette Kartenneuschreibung samt unbegehbarem Gelände bewegt keinen einzigen Fingerabdruck. Das gehört in den ersten Sprint **nach** dem Betatest, nicht später |
| **#55 Reparaturzone** | Ändert Simulationsverhalten und damit die Kampfbalance. Das Issue sagt selbst, es gehöre zur MS-1-Balance-Kalibrierung und nicht isoliert eingestreut. Eine passive Heilmechanik unmittelbar vor einem Betatest einzuziehen, verfälscht genau die Rückmeldung, die man vom Betatest haben will |
| **Umbenennung Stufe 2–4** | Stufe 2 (Marke in `ProjectSettings`, Build-Ausgabe, Packaging) lässt sich ohne einen Unity-Build nicht nachweisen. Einen unverifizierbaren Umbau der Build-Ausgabe unmittelbar vor einem Betatest auszuliefern, ist genau die Art Risiko, für die es keinen Gegenwert gibt. Stufe 4 (Code-Identität) ist durch **E-3** ohnehin ausgeschlossen, solange der Inhaber sie nicht ausdrücklich revidiert |
| **Unity-Tests in die CI (#110, zweite Hälfte)** | Inhaberentscheidung: braucht eine Unity-Lizenz als GitHub-Secret, Laufzeit und Geld. Drei Wege mit Empfehlung liegen in `reports/v8.6.0/sprint-21/07-kimi-verifikationskette.md` |
| **#52 Formationen, #49 Auswahlrahmen** | Berühren dasselbe Thema wie 22.1, sind aber eigene Pakete. #52 braucht zudem den Einheitenstrang |

## Autonom getroffene Entscheidungen — zur Nachprüfung

Diese Entscheidungen hat der Orchestrator während der Abwesenheit des Inhabers
selbst getroffen. Jede ist revidierbar; sie stehen hier, damit sie nicht
stillschweigend Bestand bekommen.

| # | Entscheidung | Begründung |
|---|---|---|
| A-1 | Der Gate-Vertrag bekommt einen **`enum` mit beiden Repo-Namen**, keinen harten Schnitt | Ein harter Schnitt macht jeden bereits archivierten Nachweis rückwirkend ungültig. Der neue Name steht an erster Stelle; die alte Zulassung fällt in einem eigenen PR, wenn niemand mehr alte Nachweise liest |
| A-2 | Schreibweise: `VibecodingGermany/HashKrieg` als Adresse, `Hashkrieg` in deutscher Prosa | Die Adresse ist wörtlich, was GitHub sagt. In Prosa ist es ein normales deutsches Substantiv |
| A-3 | Doppelklick wählt nur die **sichtbaren** Einheiten der Rolle, nicht alle auf der Karte | Kartenweit wäre ein anderer Befehl und würde den Spieler überraschen |
| A-4 | Die „nächster Pionier"-Taste **zentriert die Kamera mit** | Ohne Zentrieren findet man ihn genauso wenig wie vorher — die halbe Lösung wäre keine |
| A-5 | Worker fassen den `CHANGELOG` **nicht** an; der Eintrag entsteht beim PR | Alle PRs schreiben in denselben `[Unreleased]`-Block; das hat in einer Sitzung dreimal Merge-Konflikte erzeugt |
| A-6 | #108 wird **nach** dem Betatest angegangen, nicht davor | Der Fingerabdruck bewegt sich und macht jeden verteilten Testbuild ungültig |

## Risiken

**R-1 · Der Betatest wartet.** Alle vier Pakete sind klein, aber 22.1 ist ein
Tor: ohne benutzbare Auswahl bricht der Bauablauf ab, und ein Betatest, in dem
niemand bauen kann, misst nichts.

**R-2 · Niemand hat Unity gefahren.** Sprint 21 hat sechs Pakete geliefert, die
zum Teil nur durch Lesen abgesichert sind — der PlayMode-Fix aus #110, die
EditMode-Wächterkopie, die gesamte neue Kartenoptik. Erwartung für den lokalen
Lauf: **PlayMode 13/13**. Batchmode **ohne** `-quit`, sonst beendet sich Unity
vor dem Testlauf, schreibt keine Ergebnisdatei und meldet trotzdem Erfolg.

**R-3 · Die neue Karte hat noch nie jemand gespielt.** 15 Felder statt 5, ein
Felsring um die Mitte, vier Zufahrten à 4 Zellen. Alles gerechnet und
testgepinnt, nichts davon gespielt. Sprint 21 verlangt in seinem eigenen
„Fertig wenn" ausdrücklich eine gespielte Runde; sie steht aus.

## Fertig wenn

- [ ] Ein Spieler findet seinen Pionier in einem Pulk, ohne die Kiste neu zu
      ziehen (22.1)
- [ ] Der Gate-Selbsttest prüft beide zulässigen Repo-Namen, statt seine eigene
      Konstante zu bestätigen (22.2)
- [ ] Es gibt keine sechste Feldlage-Kopie mehr — oder `MapDefinitionSO` hat
      einen Konsumenten, der sie rechtfertigt (22.3)
- [ ] Die Gate-Schichtenkarte nennt nur Assemblies, die existieren (22.4)
- [ ] `dotnet test tools/Nova.SimRunner.Tests` grün
- [ ] **Eine gespielte Runde auf der neuen Karte** — der Nachtrag aus Sprint 21,
      den die CI nicht leisten kann

## Versionsrelevanz

`minor`. Kein Vertrag bricht: kein neuer `CommandKind`, kein `StateVersion`-Bump,
keine `RulesHash64`-Bewegung. 22.2 ändert einen Prüfvertrag, aber in
erweiternder Richtung — was vorher gültig war, bleibt gültig.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-08-29 | Erstfassung während einer autonomen Sitzung. Vier Pakete aus den Nebenbefunden von Sprint 21 und dem Umbenennungs-Inventar; sechs autonom getroffene Entscheidungen ausdrücklich zur Nachprüfung ausgewiesen | Orchestrator |
