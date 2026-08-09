# Sprint 18: Befehl und Auswahl — der Spieler sieht, was er befiehlt

**Version:** 1.0.0 | **Status:** geplant | **Verantwortungsbereich:** Netzstrang (Maintainer) | **Sprint:** 18 | **Vorgänger:** [16_Sprint_Wirtschaft.md](16_Sprint_Wirtschaft.md) | **Parallel zu:** [13B](13B_Sprint_Einheitenverhalten.md) | **Regelwerk:** [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md) | **UX-Gate:** human | **Leitsatz:** der Tester wusste, was er wollte, und konnte es nicht ausdrücken

## Zweck

Der Teil des ersten Betatests, in dem nichts kaputt war und trotzdem nichts ging:
Der Tester wollte den gegnerischen Pionier angreifen und danach das Erntefahrzeug.
Er wollte seinen eigenen Pionier in der Gruppe wiederfinden und weiterbauen. Beides
scheiterte nicht an einem Fehler, sondern daran, dass die Bedienung es nicht
anbietet.

Er fasst genau **eine** Simulationsdatei an — die Zielverteilung in
`UnitCommandStateView.ApplyMove` (Paket 18.3, erste Abwurfkandidatin). Alles
andere lebt in Eingabe und Darstellung.

## Herkunft dieser Datei

Wie [Sprint 16](16_Sprint_Wirtschaft.md): nicht aus dem Testbericht abgeleitet,
sondern aus der Inhaberentscheidung vom 2026-08-09 zu den Vorschlägen in
[16-19_Betatest_Einordnung.md](16-19_Betatest_Einordnung.md).

## Ausgangslage — am Code geprüft

| Befund | Beleg |
|---|---|
| **Eine gegnerische Einheit ist nicht markierbar** | Klickauswahl ruft `TryPickUnit(ownedByLocalSlot: true)`, die Boxauswahl filtert hart auf `u.PlayerId == playerId` |
| **Der Angriffs-Pick ignoriert den Fog of War** | `TryPickUnit` durchsucht `EntityManager.RawUnits` ohne Sichtprüfung — anders als Minimap, Lebensbalken und Einheitenansicht, die alle über `GetVisibleEntities` gehen |
| **Es gibt keinen Zielmarker** | `SelectionMarkerView` zeichnet ausschliesslich Einträge der eigenen Auswahl |
| **Niemand setzt einem Ziel nach** | `UnitCommandStateView` setzt bei `AttackTarget` nur das Feld; `MovementSystem` kennt `AttackTarget` überhaupt nicht |
| **Eine Auswahlübersicht existiert nicht** | Die Befehlskarte zeigt nur die Leiteinheit als „Rolle (+N weitere)". Pro markierter Einheit gibt es einen Bodenmarker und einen Lebensbalken, mehr nicht |
| **Auswahl nach Rolle gibt es nicht** | Klick (Radius 1.5), Box ab 8 Pixeln, additiv mit Shift, Kontrollgruppen Ctrl/Cmd+1..9. Kein Doppelklick-Typ, kein „alle sichtbaren des Typs". Gedeckelt bei `MaxSelectedEntities = 64` |
| **Die Formationsverteilung existiert bereits** | `UnitCommandStateView.ApplyMove` verteilt Ziele über expandierende Chebyshev-Ringe (`FormationMaxRing = 16`). [Sprint 11](11_Sprint_Truppenfuehrung.md) führt sie als umgesetzt und stellt nur die **Ausrichtung** zurück |
| **Das Zwei-Intent-Muster ist erprobt** | `RtsDeviceInput.DispatchMoveAndRepair` schickt `MoveTo`, dann `Repair` — zwei Schema-v1-Intents, Reihenfolge garantiert über `CommandBatch.CompareRecords` |

Der vorletzte Punkt korrigiert die Einordnung: **#52 ist zur Hälfte gebaut.**
Offen ist nur die Ausrichtung, und die „Registerfrage" ist damit beantwortet —
es braucht keinen neuen Befehlstyp.

## Schreibhoheit

| Pfad | Paket |
|---|---|
| `Scripts/Presentation/UI/RtsDeviceInput.cs` | 18.1, 18.2, 18.3 |
| `Scripts/Presentation/UI/SelectionMarkerView.cs`, `GroundMarkerVisuals.cs` | 18.2 |
| `Scripts/Presentation/UI/CommandCardHud.cs` | 18.1 |
| `Scripts/Gameplay/UI/SelectionManager.cs` | 18.1 |
| `Scripts/Simulation/State/UnitCommandStateView.cs` | 18.3 — **nur** die Zielverteilung in `ApplyMove`, kein Feld, kein Format |

**Keine Datei unter** `Simulation/Combat/`, `Movement/`, `Pathfinding/`,
`Scripts/AI*`, `Presentation/UI/DebugHud.cs`. **Kein neuer `CommandKind`.**

## Pakete

### 18.1 · Die Auswahl ist lesbar (#50)

- **Übersicht der markierten Einheiten** in der Befehlskarte, nach Rolle
  gruppiert, mit Anzahl.
- **Auswahl nach Rolle:** ein Klick auf eine Rollengruppe verengt die Auswahl auf
  diese Rolle. Damit ist der Pionier in einem Pulk wiederfindbar.

Der Bericht hält fest, dass **der Bauablauf daran abbrach** — das ist kein
Komfort, das ist ein blockierter Spielzug.

> **Zwei Pflichtstellen, die sonst schweigend brechen:**
> `CommandCardHud.EstimateHeight` bildet die Höhenrechnung von `OnGUI` Zeile für
> Zeile nach; der Kommentar dort dokumentiert genau diesen Fehler aus der
> Vergangenheit („~40 px short … visible, but not clickable"). Und jede neue
> Trefferfläche gehört in `IsPointerOverHud`, das heute genau drei Komponenten
> kennt — sonst schlagen Klicks hinter dem Panel in die Welt durch.

### 18.2 · Das Angriffsziel ist sichtbar und wird verfolgt (#51)

- **Zielmarker:** ein eigener Marker am angegriffenen Gegner, in anderer Farbe
  als der Auswahlmarker.
  > **Nicht in die Auswahl aufnehmen.** Der gesamte Code liest „Selection enthält
  > nur eigene Entities": `SelectionMarkerView` färbt alles grün, `CommandCardHud`
  > bricht bei fremdem `PlayerId` ab, `CopyMobileSelection` und `BuilderSelection`
  > filtern auf den lokalen Slot. Ein separater Marker berührt keinen dieser Pfade.
- **Fog-Prüfung im Angriffs-Pick:** `TryPickUnit(ownedByLocalSlot: false)` fragt
  ab jetzt den Fog of War. Heute lässt sich ein unsichtbarer Gegner anklicken —
  jede Anzeige auf dieser Grundlage würde zeigen, was der Spieler nicht sehen darf.
- **Nachsetzen über zwei Intents:** `MoveTo` in Reichweite, dann `Attack`, nach
  dem Muster von `DispatchMoveAndRepair`. Periodische Neuausgabe nach dem Vorbild
  `UpdateHarvesterEscort`.
  > **Die Eskorte ist das Vorbild, nicht die Vorlage.** Ihre beiden Regeln sind
  > in Wahrheit eine: `AlreadyHeadingTo` beginnt selbst mit `unit.IsMoving`, also
  > wirkt effektiv nur „nachsetzen, solange die Einheit steht". Für ein
  > **fliehendes** Ziel taugt das nicht — genau die Neuausgabe, die den Verfolger
  > auf Kurs hält, wäre unterdrückt. Formuliere die Idempotenzsperre hier **ohne**
  > `IsMoving` (neue Zielzelle ungleich `TargetGridPos`) und gib ihr eine eigene
  > Mindestkadenz. Ohne Bremse kostet jeder Takt eine Sequenznummer, landet im
  > Replay und im Relay-Strom und läuft irgendwann in `PendingQueueFull` oder
  > `SequenceOverflow`.

Ein Nachsetzen **in der Simulation** wäre `Movement/` plus `Combat/` und damit
fremdes Terrain. Der Eingabeweg ist nicht der Notbehelf, sondern der einzige, der
hier zulässig ist.

### 18.3 · Formationsausrichtung (#52) — **erste Abwurfkandidatin**

Die Verteilung existiert. Offen ist die **Ausrichtung**: Linie und Keil — die
zwei der vier bis fünf vom Tester gewünschten Formationen, die die bestehende
Verteilung ohne neuen Befehlstyp trägt. Der Rest wird im
[ScopeLedger](../ScopeLedger.md) registriert.

Umgesetzt wird sie als Ausrichtung der bestehenden Zielverteilung in `ApplyMove`,
**nicht** als neuer Befehlstyp. Damit bleibt das v1-Register eingefroren und es
entstehen keine neuen Golden-Byte-Tests.

Fällt dieses Paket, fällt es zuerst. Es ist das einzige mit einem Wunsch statt
einem blockierten Spielzug dahinter.

## Bewusst nicht in diesem Sprint

| | Warum |
|---|---|
| Attack-Move | neuer `CommandKind` gegen das eingefrorene v1-Register |
| Halte-Feuer | `Simulation/Combat/` — Einheitenstrang |
| Umstieg auf das neue Input System / echte UI | eigener Sprint, siehe „Der ehrliche Preis" |
| Gegnerinformationen (Name, HP) am Zielmarker | erst wenn die Fog-Prüfung steht und geprüft ist |
| Auswahlobergrenze über 64 | kein Befund, der sie verlangt |

## Der ehrliche Preis

`RtsDeviceInput`, `DebugHud`, `MinimapHud`, `MatchFrameHud` und `HealthBarHud`
tragen alle denselben Kopfkommentar: *Graybox throwaway, Legacy Input plus OnGUI,
wird ersetzt, wenn das neue Input System und die echte UI landen.*

Dieser Sprint verlängert planmässige Wegwerfarbeit. Das ist die bewusste
Entscheidung des Inhabers vom 2026-08-09: Die Beta läuft **jetzt**, und ein
blockierter Bauablauf kostet mehr als doppelte Arbeit an einer Oberfläche, die
ohnehin ersetzt wird. Die Frage nach dem Zeitpunkt des UI-Umstiegs steht im
Fragenkatalog.

## Risiken

| Risiko | Umgang |
|---|---|
| Neue Befehlskartenzeilen reissen die Knöpfe ab | `EstimateHeight` in jedem PR mitziehen — der Fehler ist schon einmal passiert |
| Klicks schlagen hinter dem Panel durch | jede neue Trefferfläche in `IsPointerOverHud` registrieren |
| Nachsetzen flutet den Befehlsstrom | `AlreadyHeadingTo` und `IsMoving` sind Pflicht, nicht Kür |
| Der Zielmarker leckt den Fog | die Fog-Prüfung kommt **vor** dem Marker, nicht danach |
| HUD-Code schreibt in den Entity-Store | `PresentationSourceBoundaryTests` scannt auf `GetUnitRef(` und `.Random` und lässt den Build platzen. Lesen nur über `RawUnits` mit `ref readonly` |
| Neue HUD↔Kamera-Kopplung bricht den Gate-Check | `Nova.Presentation` und `Nova.Presentation.UI` haben denselben Rang; gleichrangige Kanten sind verboten. Der etablierte Umweg heisst `MinimapCameraLink` / `HudPointerLink` als `Nova.Gameplay`-Statiken |
| Kein automatischer Nachweis | `Gameplay/` und `Presentation/` sind in keinem CI-Lauf; die EditMode-Tests laufen mangels Lizenz nicht (`505 Unsupported protocol version`) |

## Fertig wenn

1. `dotnet test tools/Nova.SimRunner.Tests` ist in der CI grün.
2. Ein Mensch hat in einer Runde:
   - in einem Pulk von mehr als zwanzig Einheiten den Pionier über die
     Befehlskarte gefunden, ausgewählt und damit gebaut,
   - eine bestimmte gegnerische Einheit angegriffen, den Zielmarker gesehen, und
     die Einheit ist nachgefahren, als das Ziel weglief,
   - dabei keine unsichtbare Einheit anklicken können.
3. Screenshots der drei Punkte liegen im PR.
4. Notiert im [GrayboxLog](../GrayboxLog.md).

Punkt 1 beweist an dieser Schicht nichts. Punkt 2 und 3 sind der Nachweis.

## Changelog-Notiz

Befehl und Auswahl werden lesbar: Übersicht der markierten Einheiten nach Rolle,
gezielte Rollenauswahl, sichtbarer Zielmarker beim Angriff, Nachsetzen ausser
Reichweite und Formationsausrichtung.

## Versionsrelevanz

`minor`.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-08-09 | Erstfassung aus den Vorschlägen der Betatest-Einordnung, am Code geprüft; #52 auf die Ausrichtung verengt, weil die Verteilung seit Sprint 11 existiert | Orchestrator |
