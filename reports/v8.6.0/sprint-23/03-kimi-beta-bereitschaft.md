# kimi-agent report

- when:    2026-08-30T00:16:55Z
- backend: cc
- model:   k3[1m]
- mode:    ro
- dir:     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova
- run:     /Users/denniswestermann/.agent-runs/20260830-021655-74191

## Task

CONSTRAINT: read-only run. Do not create, edit, or delete any file, and do not run state-changing commands. Report findings only.

Du arbeitest an "Project Nova" / HashKrieg, einem Unity-RTS mit deterministischer,
ganzzahliger Simulation. Bericht: Deutsch.

**Dieser Lauf ist reines Lesen. Ändere keine Datei.** Dein Ergebnis ist ein
Bericht.

Arbeitsverzeichnis: `/Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova`
(die Arbeitskopie — du liest sie, du schreibst nicht hinein).

## Die Frage

Das Projekt steuert auf einen Betatest zu. Zwei Sprints haben gerade
aufgeräumt: die Verknappung ist sichtbar, die Karte trägt 15 Vorkommen statt 5,
die Mitte ist ein umkämpftes Gebiet, man kommt per ESC aus der Partie heraus,
und die Auswahl ist benutzbar geworden.

**Deine Frage ist nicht „was fehlt noch am Spiel". Sie lautet: was trifft einen
fremden Tester in den ersten zehn Minuten, und wie schlimm ist es?**

Das ist ein Unterschied. Ein fehlendes Feature ist kein Problem, wenn niemand
es sucht. Eine Oberfläche, die den Spieler in eine Sackgasse führt, ist eines,
auch wenn dahinter alles funktioniert.

## Deine Quellen

**Zuerst die Berichte echter Testläufe** — dort steht, was Menschen tatsächlich
passiert ist, nicht was wir vermuten:

    docs/production/hashkrieg/Testberichte/

Lies sie alle. Sie sind der wertvollste Input, den du hast.

**Dann der Bestand.** Der Einstiegspfad eines Testers ist:
Anwendung starten → Hauptmenü → eine Partie beginnen → bauen → eine Einheit
bewegen → kämpfen → die Runde beenden. Geh diesen Pfad im Code ab:

    Assets/_Project/Scripts/Presentation/UI/     Hauptmenü, HUD, Pausemenü
    Assets/_Project/Scripts/Gameplay/            Match-Aufbau, Auswahl, Eingabe
    Assets/_Project/Scripts/Simulation/          nur lesend, zum Verstehen

**Dann der Abgleich mit dem, was schon bekannt ist.** Die offenen Issues stehen
auf GitHub; ohne Netzzugang findest du die wichtigsten in den Sprintdateien
unter `docs/production/hashkrieg/` und in den beiden neuesten Sprintdokumenten
(21 und 22), jeweils im Abschnitt „Bewusst nicht in diesem Sprint" und
„Was offen bleibt".

## Was ein brauchbares Ergebnis ausmacht

**Sortiere nach dem, was der Tester erlebt, nicht nach Modulen.** Ein Befund ist
für mich brauchbar, wenn er beantwortet:

1. **Was sieht oder tut der Tester?** In einem Satz, aus seiner Sicht, ohne
   Klassennamen.
2. **Wo im Code liegt es?** Mit `Datei:Zeile`.
3. **Wie schlimm?** Genau drei Stufen, und benutz sie streng:
   - **Sackgasse** — der Tester kommt nicht weiter oder verliert Fortschritt.
     Das sind die einzigen echten Beta-Blocker.
   - **Falscher Eindruck** — es funktioniert, sieht aber kaputt aus, oder es
     ist kaputt und sieht funktionierend aus. Das erzeugt Fehlmeldungen und
     kostet doppelt: einmal den Tester, einmal die Auswertung.
   - **Reibung** — es nervt, hält aber niemanden auf.
4. **Was kostet die Behebung ungefähr?** Grob: eine Zeile, ein Nachmittag, ein
   Sprint.

**Und die Gegenprobe, die ich ausdrücklich will:** was ist in den letzten beiden
Sprints gebaut worden, das ein Tester **nicht** bemerken wird, weil es keine
Anzeige hat? Zwei Sprints Arbeit, die niemand sieht, sind ein
Kommunikationsproblem und keine Softwarefrage — aber ich will wissen, ob wir
eines haben.

## Zwei Dinge, die du wissen musst und die deine Bewertung ändern

- **Unity ist niemandem in diesen Sitzungen zur Verfügung gestanden.** Die
  EditMode- und PlayMode-Tests sind geschrieben, aber seit Tagen nicht gelaufen,
  und die neue Karte hat noch nie jemand gespielt. Wenn du etwas findest, das
  nur ein Editorlauf zeigen könnte, sag es als solches — „unbelegt, braucht
  einen Unity-Lauf" ist ein vollwertiger Befund und keine Schwäche.
- **Die eingecheckte Bootstrap-Szene wird von einem Generator erzeugt**
  (`Assets/_Project/Editor/BootstrapSceneGenerator.cs`). Prüf, ob der Zustand,
  den ein Tester in einem Build sieht, überhaupt dem entspricht, was der Code
  heute tut — eine veraltete Szene ist genau die Sorte Fehler, die einen ganzen
  Testdurchlauf entwertet.

## Was ich NICHT von dir will

- Keine Feature-Wunschliste. Was fehlt, weiß ich; ich will wissen, was **stört**.
- Keine Codequalitäts-Betrachtung. Refactoring-Vorschläge gehören nicht hierher.
- Keine Bewertung des Einheitenstrangs (`Simulation/Combat`, `Movement`,
  `Factions`, `Pathfinding`, `AI`) als Arbeitsauftrag — der gehört einem anderen
  Menschen. Wenn dort etwas den Tester trifft, **melde es**, aber schreib dazu,
  dass es fremdes Gebiet ist.

## Grenzen

- **Nur lesen.** Kein Schreiben, kein `git`-Schreibbefehl, kein `gh`-Aufruf, der
  etwas verändert.
- Tests fahren **darfst** du, das ist Lesen:

      "/Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/.dotnet/dotnet" test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release

- Keine Subagenten.

## Report

Markdown. Struktur:

  1. **Die Antwort in fünf Sätzen.** Ist das Ding betatestreif, ja oder nein,
     und woran hängt es.
  2. Sackgassen — die einzige Liste, die wirklich zählt
  3. Falscher Eindruck
  4. Reibung
  5. Was gebaut wurde und der Tester nicht sehen wird
  6. Was nur ein Unity-Lauf klären kann
  7. Deine Prüfliste: welchen Pfad du abgegangen bist, auch dort, wo du nichts
     gefunden hast

Schließe mit:

  STATUS: DONE | BLOCKED
  - Befund 1
  - Befund 2
  - Befund 3

## Output

Ich habe alle Quellen ausgeschöpft. Hier ist der Bericht.

---

# Beta-Bereitschaft: Was einen fremden Tester in den ersten zehn Minuten trifft

**Geprüfter Stand:** `integration/s21-review` @ `5e60fb8` (== `origin/integration/s21-review`), Arbeitskopie mit untracked Dateien. Quellen: beide Testberichte T-01, Sprintdokumente 16–21, Auftragsdateien, DecisionLog, CHANGELOG, der Einstiegspfad im Code (UI, Gameplay, Simulation lesend), Szene vs. Generator.

## 1. Die Antwort in fünf Sätzen

Betatestreif ist der integrierte Stand im Kernpfad: Hauptmenü, Matchstart, Bauen mit ehrlichen Sperrgründen, Ernte-Auto-Zyklus, ESC/Pausemenü-Ausstieg, Ergebnispanel und Neustart sind im Code geschlossen, und die eingecheckte Szene ist nachweislich frisch (PauseMenuHud, BuildZoneOverlayView, VersionBadge vorhanden und korrekt verdrahtet). Aber die Prämisse „die Karte trägt 15 Vorkommen, die Mitte ist umkämpft" gilt für diesen Integrationsstand **nicht**: 21.6 und 21.7 liegen auf den ungemergten Branches `feat/21.6-map-fields` und `feat/21.7-center-zone` — ein Build von hier zeigt weiterhin die 5-Felder-Karte (`MatchBootstrap.cs:162-169`), also genau die Karte, die T-01 als zu leer gemeldet hat. Der teuerste verbleibende Code-Befund ist eine unsichtbare Mechanik: das Startguthaben (3.000 AE) liegt über der Speicherobergrenze (2.000 AE) und zerfällt ab der ersten Sekunde um 25 % des Überhangs pro Sekunde, während kein UI die Obergrenze zeigt — ein zögernder Tester sieht sein Geld schmelzen und meldet „Geld verschwindet". Der zweite ist der Angriffsgriff: A auf ein entferntes Ziel wird angenommen, aber nichts passiert sichtbar, weil Combat den Befehl nur hält und Movement nicht nähert — das reproduziert T-01s zentrale Kampfbeschwerde in neuer Form. Und alles davon steht auf tönernen Füßen, solange kein Unity-Lauf stattgefunden hat: `Gameplay/` und `Presentation/` linkt die CI nicht, diese Assemblies sind seit Tagen weder kompiliert noch gespielt, und die Arbeitskopie enthält ein untracked DIAG-Overlay, das in einem lokal gebauten Beta-Build jedem Tester ins Hauptmenü leuchtet.

## 2. Sackgassen

**Streng gezählt: keine belegte.** Ich habe den Pfad Start → Menü → Match → Bauen → Bewegen → Kämpfen → Runde beenden vollständig gelesen und keinen Punkt gefunden, an dem der Tester nachweislich festhängt oder Fortschritt verliert. Jede Sackgassen-Kandidatin hat einen belegten Ausweg: „Laden" ist deaktiviert mit ehrlichem Hinweis statt Sackgasse (`MainMenuController.cs:348-365`), die Netzpartie-Pfade haben Abbruch-Buttons und klare Fehlertexte, das Pausemenü führt per „Zum Hauptmenü"/„Spiel beenden" immer heraus, ein wirtschaftlich toter Spieler wird spätestens beim 45-Minuten-Zeitlimit beendet (`VictorySystem.cs:143`), und der T-03-Eingabestillstand nach Pause ist im Code behoben (`MatchRunner.cs:302-320` — Resume am stehenden Tick).

Aber zwei Vorbehalte gehören hierher, weil sie den **Testlauf** (nicht den Tester) entwerten können:

- **Build aus der falschen Baumform.** Ein Build von `integration/s21-review` enthält nicht die Kartenarbeit, die der Beta zeigen soll (s. Abschnitt 5); ein Build von den Feature-Branches wäre ungetestete Integration. Und ein Build aus **dieser Arbeitskopie** (statt einem sauberen Checkout) enthält `InputGateDiagnostic.cs` — untracked, von Unity trotzdem kompiliert, mit `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` (`InputGateDiagnostic.cs:13-19`) spawnt es einen gelben „DIAG"-Block oben links, der in Menü und Match dauerhaft Gate-Zustände, Framezeiten und GC-Werte einblendet. Die Datei sagt selbst „not for commit. Delete after the diagnosis." Kein Tester kommt daran vorbei. **Kosten: eine Zeile** (Datei löschen oder aus sauberem Checkout bauen) — aber wenn es passiert, steht in jedem Screenshot des Betas ein Diagnose-Overlay.
- **Unbelegt, braucht Unity:** der gesamte Kernpfad oben ist Code-Lektüre. Die PlayMode-Tests (`MainMenuTests`, `PauseMenuTests`, `VersionBadgeTests`) sind geschrieben und seit Tagen nicht gelaufen; die Szene habe ich YAML-seitig gegen die Komponentenfelder geprüft (aktuell, inklusive `_repaintIntervalSeconds` aus dem 21.4-Nachschub), aber Kompilierung, Ausführungsreihenfolgen und IMGUI-Layouts kann nur ein Editorlauf belegen.

## 3. Falscher Eindruck

**F1 · Das Startguthaben schmilzt sichtbar, und nichts erklärt es.** Der Tester startet mit 3.000 AE (`EconomySystem.cs:148`), die Obergrenze ohne Lager ist 2.000 (`EconomySystem.cs:158`), und ab Tick 10 verliert der Überschuss 25 % pro Sekunde (`EconomySystem.cs:459-485`): wer die erste halbe Minute damit verbringt, sich umzusehen — genau das tut ein fremder Tester —, sieht den Zähler von 3.000 Richtung 2.000 fallen, ohne etwas ausgegeben zu haben. Die Statusleiste zeigt nur „2750 AE" (`DebugHud.cs:236-239`), keine Obergrenze, kein „Speicher voll", kein Verfallshinweis; `CapacityFor` wird von keiner Präsentationsdatei gelesen (Grep: null Treffer außerhalb der Simulation). Die KI baut dagegen sofort und rutscht durch Ausgeben unter die Grenze — der Verfall bestraft fast nur den lesenden Menschen. Das ist D-106 korrekt umgesetzt und trotzdem die sicherste Fehlmeldung der ersten Testrunde. **Kosten: ein Nachmittag** — Obergrenze in die Bauleisten-Statuszeile (Maintainer-Gebiet; die DebugHud-Statusleiste selbst gehört 13B), z. B. „1.750 / 2.000 AE — Speicher voll: Überschuss verfällt".

**F2 · Das Lager tut weiterhin „nichts" — für den Tester.** Die +2.000-AE-Obergrenze pro Lager ist mechanisch echt (16.4/D-106), aber weder der Baukarten-Button noch die Befehlskarte noch irgendein HUD-Element nennt sie. T-01s Befund „Lager kostet Geld und tut nichts (bekannt)" ist damit **mechanisch behoben und wahrnehmungsseitig offen** — er wird wortgleich wiederkommen. Beim Radar ist das Gegenstück gelöst: die Minimap hängt am fertigen Radar und der Button sagt es (`MinimapHud.cs:150-153,186-193`). **Kosten: eine Zeile** im Button-/Kartenlabel, sobald F1 die Anzeige hat.

**F3 · A auf ein entferntes Ziel: Befehl angenommen, nichts passiert.** `ResolveAttackAt` schickt bei einem Feind unter dem Cursor genau einen AttackTarget-Intent (`RtsDeviceInput.cs:1259-1262`, `RtsIntentDispatcher.cs:167-172`); die Anwendung setzt nur `AttackTarget` (`UnitCommandStateView.cs:288-305`); Combat hält ein Ziel außer Reichweite, ohne sich zu bewegen — „closing the distance is Movement's concern" (`CombatSystem.cs:75-76`), und Movement liest `AttackTarget` nirgends (Grep belegt). Der Tester drückt A auf den gegnerischen Pionier in Sichtweite aber außer Schussweite, die Einheiten bleiben stehen, und die einzige Rückmeldung käme bei einer *Ablehnung* — eine Annahme ist lautlos. Das ist exakt T-01s Geste („gezielt den Pionier angreifen") und exakt sein Erlebnis („nichts passiert" / „random"), nur mit saubererem Innenleben. Kämpfen geht sonst: RMB in die Nähe + Auto-Acquire (D-087) trägt das Gefecht, und die KI macht es genau so (Move voraus, dann Attack). **Fremdes Gebiet** für das eigentliche Nachsetzen (Movement/Combat, 13B) — **aber** der Maintainer-seitige Umweg existiert als Muster im selben File: der Reparaturfluss schickt Move+Repair als Doppelintent (`RtsDeviceInput.cs:1327-1331`, von der Einordnung ausdrücklich für #51 empfohlen). **Kosten: ein Nachmittag** für Move+Attack in `ResolveAttackAt`; das richtige Nachsetzen bleibt 13B.

**F4 · Minimap: jetzt das umgekehrte Missverständnis.** T-01 monierte „Minimap ohne Radar da". Jetzt ist sie ohne Radar **gar nicht da** (D-096) — ein frischer Tester, der den Radar-Button nicht hovert, meldet „keine Minimap" als Bug. Durch den Button-Hinweis abgemildert, aber erwarte diese Meldung. **Kosten: eine Zeile** (Onboarding-Hinweis oder dunkler Platzhalter mit „Radar benötigt").

**F5 · Versionsbadge sagt „dev", wenn der Build nicht über die Skripte läuft.** `BuildInfo.txt` wird nur von den Packaging-Skripten gestempelt; ein Build über die Unity-GUI zeigt `v0.21.0 · dev` (`VersionBadge.cs:65-77`, Nebenbefund aus dem Sprint-21-Bericht). Damit fehlt in Befunden genau die Build-Identität, für die #103 das Badge gebaut hat. **Kosten: null Code** — Prozessregel „Beta-Builds nur über `tools/packaging/`".

## 4. Reibung

- **Die Tastenbelegung existiert nur hinter F3.** Die Legende (Bau-Hotkeys, O-Overlay, Gruppen, H/R) zeichnet ausschließlich das Debug-Panel (`DebugHud.cs:202-204`); gemildert durch den Onboarding-Hint in der Bauleiste (`BuildMenuHud.cs:82-83`), die Kamera-Zeile als Idle-Fallback (`BuildMenuHud.cs:420-422`) und Hotkeys in den Button-Labels — aber Gruppen, O und die Befehlskarten-Gesten bleiben unentdeckbar. **Kosten: ein Nachmittag** (Hilfe-Eintrag im Pausemenü — dort sitzen seit T-03 ohnehin die Einstellungen).
- **Relay-Gast schaut in die falsche Ecke.** Der Kamera-Startfokus ist die serialisierte Slot-0-Ecke (8,6) (`RtsDeviceInput`-unabhängig: `RtsCameraController.cs:54-55,101`); `RequestStartFocusReset` trägt keinen Slot (`MinimapCameraLink.cs:87-90`). Wer als Gast (Slot 1, Basis oben rechts) einer Netzpartie beitritt, öffnet auf die gegnerische Ecke im Nebel. Lokal (Beta-Hauptpfad) unbelegt-problemlos. **Kosten: ein Nachmittag.**
- **Jeder Harvester ab dem zweiten braucht ein manuelles H.** Nur der Gründungs-Harvester bekommt den Feldauftrag automatisch (`ConstructionSystem.cs:813-863`); aus der Warteschlange produzierte Harvester laufen zum Sammelpunkt und idlen, bis H gedrückt wird (die Eskorte greift erst bei `HarvestFieldId != 0`, `RtsDeviceInput.cs:751-757`). T-01s Erwartung („Harvester hat keinen anderen Zweck") gilt für sie weiterhin nicht. **Kosten: ein Nachmittag** (Auto-Ernte am Spawn, Sim-seitig wie der Gründungsfall — Hoheit prüfen, Production ist 13B-nah).
- **Wirtschaftlich tot ohne Ansage.** Wer alles ausgegeben hat und weder Raffinerie noch Harvester besitzt, hat keinen Weg zurück — legitimer RTS-Verlustzustand, aber ohne jede Ansage; das 45-Minuten-Limit beendet die Leiche erst spät. **Kosten: ein Nachmittag** (Statuszeilen-Warnung) oder bewusst so lassen.
- **Sprint 18 ist als Block nicht gelaufen.** 18.1 (Auswahlübersicht über 21.5 hinaus), 18.2 (Zielmarker + Nachsetzen), 18.3 (Formationen) stehen nicht im CHANGELOG; die sichtbare Hälfte ist mit #49/21.5 abgedeckt, der Rest fehlt — bekannt und beschlossen, hier nur der Vollständigkeit halber.

## 5. Was gebaut wurde und der Tester nicht sehen wird

- **Die gesamte Kartenarbeit (21.6 + 21.7) ist im Integrationsstand unsichtbar, weil sie nicht drin ist.** 15 Felder samt Vierer-Mitte liegen auf `feat/21.6-map-fields` (0237f07), die Gelände-/Chokepoint-Hälfte auf `feat/21.7-center-zone` (bcb9ee6, Committext: „Erreichbarkeit und Turmlogik ungespielt", und der Determinismus-Zähler hängt auf einem dritten Branch). Auf dem geprüften Branch registriert `SetupFields` exakt fünf Felder (`MatchBootstrap.cs:940-950`). Das ist die gesuchte Antwort auf die Gegenprobe: **ja, ihr habt ein Kommunikationsproblem** — die Behauptung „die Karte trägt 15 Vorkommen" beschreibt einen Zustand, den kein Build des Integrationsbranches zeigt, und die Arbeit ist zusätzlich nie gespielt worden. Folgepunkt: der Generator-Snapshot `EnsureGlutrinneMapAsset` (`BootstrapSceneGenerator.cs:271-306`) schreibt weiterhin fünf Felder in `MAP_Glutrinne.asset` — heute konsistent zum Code, ab dem 21.6-Merge still veraltet (Laufzeit liest das Asset nichts, aber der Docstring verspricht die kanonische Lage).
- **D-108 (jedes fertige Gebäude erweitert die Bauzone) ist drin und fast unsichtbar.** Einzig das O-Overlay zeigt die wachsende Zone (`BuildZoneOverlayView`, auto während der Platzierung) — wer nie platziert, merkt die Regel nie. Das ist die akzeptable Form von unsichtbar, aber niemand wird dem Spiel die gelöste Enge-Frage (#92) anrechnen.
- **Der T-03-Fix und der Modal-Riegel sind per Natur unsichtbar** (ein Fehler tritt nicht mehr auf) — korrekt so, gehört aber in die Beta-Begleitnotiz, sonst testet niemand gezielt „Pause → Resume → Befehle gehen noch".
- **Die 21.3-Tragzeitmessung (7:33/3:46/2:31 min)** ist eine Zahl in einer Testdatei; der Tester sieht vom Startfeld nur die Feldkarte (21.2). Gut so — aber die Balancing-Absicht dahinter wird ohne Begleittext nicht wahrnehmbar.

## 6. Was nur ein Unity-Lauf klären kann

1. **Der komplette Kernpfad als Ausführung:** Menü → Neues Spiel → Bauen → Ernten → Kämpfen → ESC → Hauptmenü → Neues Spiel. Die Auftragsdatei sagt es selbst: `Nova.SimRunner.Tests` linkt nur Core/Simulation/AI/Networking — für `Gameplay/` und `Presentation/` führt kein CI-Lauf den Code aus, und der letzte Stand dieser Assemblies ist nie gelaufen.
2. **Mein eigener Testlauf ist gescheitert — am Werkzeug, nicht am Projekt.** Ich habe den erlaubten `dotnet test`-Aufruf dreimal gestartet; die Sitzung hat den Hintergrundprozess jedes Mal verwaist (Task-ID vergeben, nie registriert, keine Ausgabedatei). Letzter belegter Stand: 726/726 grün auf `9281341` (Sprint-21-Arbeitsberichte); danach kamen fünf Commits, darunter die MatchRunner-Verhaltensänderung 369987c — die SimRunner-Suite linkt Gameplay ohnehin nicht, ein grüner Lauf würde den T-03-Fix also nicht belegen. **Der offene Testlauf ist hiermit als nicht ausgeführt gemeldet, nicht als grün behauptet.**
3. **Szene:** YAML-seitig aktuell (Verdrahtung und Felder stimmen mit dem heutigen Code überein, `AutoStart=0`), aber Kompilierung der Unity-/Editor-Assemblies und die drei PlayMode-Testklassen sind ungelaufen.
4. **Optik der neuen Oberflächen:** Kristall-Staging (21.2b), Overlay-Farben/Höhe (21.4), Pausemenü mit aufgeklappten Einstellungszeilen (Panel 470 px bei 1,5× Scale — Overflow nur im Lauf sichtbar), Key Art, Rajdhani-Glyphen.
5. **Art-Befunde #57/#58:** Prefabs für alle MS-1-Gebäude und -Einheiten beider Fraktionen liegen im Repo (`Assets/_Project/Art/…`) — ob die Kaserne noch hohl und der Radarturm noch maßstabssprengend ist, kann nur ein laufendes Spiel zeigen.
6. **Die neue Karte (nach Merge von 21.6/21.7):** Erreichbarkeit ist per .NET-Test gepinnt, aber „ungespielt" steht ausdrücklich im Commit — Chokepoint-Breite für Gruppen, Turmlogik, Kartenlesbarkeit.

## 7. Prüfliste — der abgegangene Pfad

| Schritt | Gelesen | Ergebnis |
|---|---|---|
| Testberichte | beide T-01-Berichte vollständig | 16 + 10 Befunde; Abgleich: #43/#44/#45/#46/#47/#49/#50(Teil)/#51(Eingabe)/#53/#54 behoben; #57/#58 Art; #52/#55/#56/#89/#90 offen-beschlossen |
| Anwendung starten | Generator vs. `Bootstrap.unity` (YAML), `MainMenuController.Start/BuildTree`, Assets (Key Art, Fonts, Musik), `bundleVersion` | Szene aktuell, Assets vorhanden; GUI-Builds zeigen „dev" (F5) |
| Hauptmenü | `MainMenuController.cs` inkl. Lobby-Partial, Einstellungen, Netzwerk-Validierung | Ehrliche Deaktivierungen, Abbruchwege überall; kein Befund |
| Partie beginnen | `MatchBootstrap` (StartGrayboxMatch, BuildOpening, SetupFields, RestartMatch), Kamera-Startfokus | **5 Felder, nicht 15** (Hauptbefund); Kamera-Ecke slotfest (Reibung) |
| Bauen | `RtsDeviceInput` Platzierung + Auto-Builder, `ConstructionSystem.ValidatePlacement`/Anker, `BuildMenuHud` Sperrgründe, `BuildZoneOverlayView`, `SimDefinitions` Eröffnungsbilanz | Geschlossener Pfad; **Startguthaben-Verfall unsichtbar (F1)**, Lager-Wirkung unsichtbar (F2) |
| Einheit bewegen/auswählen | Selektion (Klick/Box/Feld-Pick, Micro-Drag-Fallback), Gruppen, `CommandCardHud` (21.5-Schnittmenge), `GroundMarkerVisuals` | Deutlich besser als im T-01-Bericht; Legende nur per F3 (Reibung) |
| Kämpfen | `ResolveAttackAt`, `RtsIntentDispatcher.Attack`, `UnitCommandStateView`, `CombatSystem` (Hold-Regel), `MovementSystem` (kein Nachsetzen) | **F3**; Move+Auto-Acquire trägt das Gefecht; KI-Ökonomie nach Felderschöpfung degradiert (13B, bekannt, gemeldet) |
| Runde beenden | `PauseMenuHud` (ESC-Schichtung, Modal-Riegel, Einstellungen), `MatchFrameHud` (Ergebnis/Netzwerk), `ReturnToMenu`, 45-Min-Limit | Geschlossen, T-03-Fix im Code belegt, spielfrei |
| Bekanntes abgleichen | Sprint 21 („Bewusst nicht"), AUFTRAG_Verknappungsfolgen, 16-19_Einordnung, CHANGELOG, DecisionLog D-096/106/108/109 | Es existiert **kein Sprint-22-Dokument** im Repo — die „zwei Sprints" sind 21 plus die Review-Integrationsrunde (T-03); die Kartenpakete leben auf Branches |
| Tests | `dotnet test`-Erlaubnis | Dreimal gestartet, von der Sitzung verwaist — nicht ausgeführt, s. Abschnitt 6 |

STATUS: DONE
- Hauptbefund: 21.6/21.7 (15 Felder, umkämpfte Mitte) sind auf ungemergten Branches — `integration/s21-review` @ 5e60fb8 spielt weiter die 5-Felder-Karte (`MatchBootstrap.cs:162-169`); die Kartenarbeit ist zudem nie gespielt worden.
- Teuerster Code-Befund: Startguthaben 3.000 AE liegt über der 2.000-Obergrenze und zerfällt ab Sekunde 1 sichtbar am Zähler, ohne dass ein UI Obergrenze oder Verfall zeigt (`EconomySystem.cs:459-485`, `DebugHud.cs:236-239`) — garantierte „Geld verschwindet"-Meldung in Minute eins; ein Nachmittag.
- Angriffsgriff reproduziert T-01s Kampfbeschwerde: A auf entferntes Ziel wird lautlos gehalten statt verfolgt (`RtsDeviceInput.cs:1252-1267`, `CombatSystem.cs:75-76`; Movement liest AttackTarget nie) — Nachsetzen ist 13B-Gebiet, aber der Move+Attack-Doppelintent nach Repair-Vorbild wäre Maintainer-seitig an einem Nachmittag machbar; dazu prozessual: Beta-Build nicht aus dieser Arbeitskopie bauen (untracked `InputGateDiagnostic.cs` blendet jedem Tester ein DIAG-Overlay ein), und kein Kernpfad ist seit den letzten Änderungen je in Unity gelaufen — der dotnet-Testlauf war in dieser Sitzung technisch nicht ausführbar (dreimal verwaister Hintergrundprozess), letzter belegter Stand 726/726 grün auf 9281341.

## Exit

exit=0

## Progress

    [00:15] · Ich beginne mit der Struktur und den Testberichten — das ist der wertvollste Input.
    [00:15] Glob     docs/production/hashkrieg/Testberichte/**/*
    [00:15] Glob     docs/production/hashkrieg/*.md
    [00:15] Glob     Assets/_Project/Scripts/Presentation/UI/**/*.cs
    [00:26] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/docs/production/hashkrieg/Testbe…
    [00:26] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/docs/production/hashkrieg/Testbe…
    [00:26] Glob     docs/production/hashkrieg/**/*22*
    [00:27] Glob     reports/v8.6.0/**/*
    [00:52] · Die beiden Testberichte sind gelesen — sehr ergiebig. Jetzt die Sprintdokumente 21/22 und der Beta-Bereitschaftsbericht aus Sprint 23.
    [00:54] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/docs/production/hashkrieg/21_Spr…
    [00:54] Glob     docs/**/*22*
    [00:54] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/reports/v8.6.0/sprint-23/02-kimi…
    [01:06] · Der Sprint-23-Report ist nur ein Echo meines eigenen Auftrags — kein Inhalt. Jetzt suche ich das Sprint-22-Dokument und lese die Sprint-21-Einzelberichte.
    [01:06] Glob     docs/**/*2[23]*Sprint*
    [01:06] Glob     docs/production/hashkrieg/**/*
    [01:16] Grep     Sprint 22|22_Sprint|sprint-22
    [01:16] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/docs/production/hashkrieg/16-19_…
    [01:28] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/reports/v8.6.0/sprint-21/01-kimi…
    [01:28] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/reports/v8.6.0/sprint-21/05-kimi…
    [01:47] · Sehr aufschlussreich — insbesondere der Hinweis, dass `Bootstrap.unity` nicht regeneriert wurde. Jetzt die übrigen Sprint-21-Berichte und die jüngste Git-Histo…
    [01:47] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/reports/v8.6.0/sprint-21/02-kimi…
    [01:49] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/reports/v8.6.0/sprint-21/03-kimi…
    [01:49] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/reports/v8.6.0/sprint-21/04-kimi…
    [02:06] · Jetzt prüfe ich die Git-Historie und den kritischen Punkt: Ist die eingecheckte Szene aktuell?
    [02:06] Glob     Assets/**/*.unity
    [02:06] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scenes
    [02:18] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Editor/Bootstrap…
    [02:40] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scenes/Bootstrap…
    [02:41] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [02:52] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [03:06] Grep     T-03
    [03:07] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Gameplay…
    [03:29] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/CHANGELOG.md
    [03:48] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/CHANGELOG.md
    [04:08] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Simulati…
    [04:29] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [04:46] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [05:08] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [05:23] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [05:38] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [05:51] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [05:51] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [06:05] Grep     AutoHarvest|auto-harvest|automatisch.*[Ee]rnte|Harvest.*auto|ApplyStop|ClearAttack|Stop.*…
    [06:20] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [06:34] Grep     HarvestFieldId = |AssignHarvest|StartHarvest|Harvester.*spawn|spawned.*harvester|first ha…
    [06:48] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Simulati…
    [06:48] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Simulati…
    [07:02] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Simulati…
    [07:17] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Simulati…
    [07:31] · Stop ist repariert (#45). Jetzt der Angriffspfad (#51) und die Befehlskarte (21.5).
    [07:31] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [07:45] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [07:56] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [08:18] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Gameplay…
    [08:18] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [08:26] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Gameplay…
    [08:26] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [08:37] Grep     Strom|LOW POWER|LowPower|PowerProvided
    [08:47] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [09:00] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [09:11] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/AI
    [09:18] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/AI/Skirm…
    [09:32] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/AI/Skirm…
    [09:52] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/docs/production/DecisionLog.md
    [10:05] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/docs/production/DecisionLog.md
    [10:06] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [10:27] · Radar→Minimap und Lager→AE-Obergrenze sind umgesetzt (16.5/D-106). Jetzt starte ich die erlaubte Test-Suite im Hintergrund und prüfe parallel den Baustellen-Sc…
    [10:27] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Simulati…
    [10:43] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [11:07] Glob     Assets/_Project/UI/**/*
    [11:07] Glob     Assets/_Project/Audio/Music/*
    [11:08] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/ProjectSettings/ProjectSettings.…
    [11:19] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [11:36] Grep     ControlLegend
    [11:37] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [11:58] Grep     RequestStartFocusReset|FocusCameraOn|LocalHqCenterCell
    [12:10] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Gameplay…
    [12:22] TaskOutput {"task_id": "b75c101", "block": true, "timeout": 240000}
    [12:31] Read     /private/tmp/claude-501/-Volumes-2TB-CodingProjekte-Coding-Projekte-Project-Nova/079a9f04…
    [12:38] Glob     /private/tmp/claude-501/-Volumes-2TB-CodingProjekte-Coding-Projekte-Project-Nova/079a9f04…
    [12:53] · Der Hintergrund-Task ist nicht auffindbar — ich starte den Testlauf erneut und warte diesmal aktiv.
    [12:53] TaskList {}
    [13:07] ScheduleWakeup Check whether the dotnet test background run (Nova.SimRunner.Tests) has produced output, …
    [13:21] TaskOutput {"task_id": "b75c101", "block": true, "timeout": 300000}
    [13:33] Glob     /private/tmp/claude-501/-Volumes-2TB-CodingProjekte-Coding-Projekte-Project-Nova/**/*.out…
    [13:44] Skill    {"args": "\"/Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/.dotnet/dotnet\" tes…
    [13:59] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/docs/production/hashkrieg/AUFTRA…
    [14:08] TaskOutput {"task_id": "bb0dzfz", "block": true, "timeout": 120000}
    [14:17] Read     /private/tmp/claude-501/-Volumes-2TB-CodingProjekte-Coding-Projekte-Project-Nova/079a9f04…
    [14:31] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [14:42] Read     /private/tmp/claude-501/-Volumes-2TB-CodingProjekte-Coding-Projekte-Project-Nova/079a9f04…
    [15:00] Glob     /private/tmp/claude-501/-Volumes-2TB-CodingProjekte-Coding-Projekte-Project-Nova/079a9f04…
    [15:16] Grep     MapDefinitionSO|MAP_Glutrinne
    [15:32] Grep     TryAddField
    [15:46] Grep     SetCost|ImpassableCost
    [16:32] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [16:32] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scenes/Bootstrap…
    [17:05] · Die Szene ist aktuell (inkl. `_repaintIntervalSeconds` aus dem 21.4-Nachschub). Jetzt prüfe ich die Eröffnungsökonomie: Strom zum Start und Bauvoraussetzungen.
    [17:05] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Simulati…
    [17:12] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Simulati…
    [17:27] Grep     LastCommandStatus
    [17:35] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [17:52] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [18:03] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [18:12] Grep     ShowTransientNotice
    [18:22] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [18:28] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [18:41] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [18:51] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Presenta…
    [19:00] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/CHANGELOG.md
    [19:20] Grep     AttackTarget|attack target|Zielmarkier|target marker
    [19:38] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Gameplay
    [19:45] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Gameplay…
    [19:57] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Simulati…
    [20:10] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Simulati…
    [20:24] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Simulati…
    [21:28] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Simulati…
    [21:55] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Simulati…
    [22:08] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Simulati…
    [22:31] Grep     CapacityFor|Kapazität|Speicher voll|Überschuss
    [23:25] Grep     Speicher|2\.000|2000|Lager
    [24:53] · Fast vollständig. Letzte Prüfung: Sind Art-Prefabs vorhanden (Graybox vs. Assets), und was zeigt der Startbildschirm nach „Neues Spiel"?
    [24:53] Glob     Assets/_Project/Art/**/*.prefab
    [25:08] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Simulati…
    [29:21] · Ich habe alle Quellen ausgeschöpft. Hier ist der Bericht. --- # Beta-Bereitschaft: Was einen fremden Tester in den ersten zehn Minuten trifft **Geprüfter Stand…
    [29:21] finished
