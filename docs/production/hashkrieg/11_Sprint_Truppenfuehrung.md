# Sprint: Truppenführung — Einheiten teilen sich den Platz

**Status:** umgesetzt (2026-08-07) | **Vorgänger:** [09_Sprint_Gefecht_und_Rundenrahmen.md](09_Sprint_Gefecht_und_Rundenrahmen.md) (umgesetzt, D-086/D-087) | **Leitsatz:** eine Armee ist kein Haufen

## Ergebnis (2026-08-07)

- **TEIL 1 umgesetzt — Formationsverteilung:** `UnitCommandStateView.ApplyMove`
  verteilt auf freie Zellen um das Ziel (kleinster Entity-Index → Zielzelle, dann
  Chebyshev-Ringe aufsteigend (y, x), Stempel-Array statt Allokation). Neu ist
  `UnitState.GoalGridPos` (Entity-Store-Block **v5**): die Gruppe teilt sich **ein**
  Flow-Field (`TargetGridPos`), die Ankunft prüft die persönliche Zelle — ein
  Flow-Field pro Einheit wurde wegen der Cache-Kapazität (32) verworfen. Die
  Produktions-Spawn-Suche lehnt jetzt auch einheitenbelegte Zellen ab.
- **TEIL 2 umgesetzt — Separation im Stand (Einschleifen-Variante):** die
  Bewegungsschleife läuft für alle aktiven mobilen Einheiten; stehend wirkt nur
  eine gedämpfte (0,5), gedeckelte (0,25 m/Tick) Positionskorrektur mit Totzone,
  ohne Rotationsänderung. Exakte Überlappung (distanzlos) löst ein
  Entity-Index-Tiebreak. Unbewegliche Entitäten (MoveSpeed 0) werden nie
  verschoben. Gewählt gegen den separaten Entstapelungs-Durchgang, weil ein
  Codepfad weniger Abweichungsfläche hat und der Flow-Anteil ohne Befehl
  ohnehin null ist.
- **TEIL 3 umgesetzt — Gebäude ins Kostenfeld:** `ConstructionSystem` bekommt
  das `CostField` optional injiziert (kanonische Hosts verdrahtet:
  MatchRunner, SimRunner-Szenarien, match-nahe Test-Hosts) und spiegelt jeden
  Footprint als Impassable/Open-Schreiben; Platzierung schiebt mobile Einheiten
  per Ringsuche aus dem Footprint (Ziel im Footprint → umgesetzt auf die
  Ausweichzelle). Falle 1 (Cache-Flut): der Epoch-Sync **regeneriert an Ort**
  statt zu leeren — einmal pro Tick zusammengefasst, durch die Cache-Kapazität
  begrenzt, und bewegte Einheiten verlieren nie die Führung (kein
  Direct-Steering durch Wände). Falle 2 (Einschluss): Push-out plus
  „unbegehbares Ziel gilt an der Wand als erreicht" (keine begehbare
  Nachbarzelle näher). **Vertragsänderung:** die serialisierte Epoch wird beim
  Restore **adoptiert statt verglichen** — Mutationshistorie ist aus dem
  Endzustand nicht nachspielbar; der Inhaltsbeweis ist strukturell
  (Construction-Block restauriert vor Pathfinding).
- **KI-Folgefix (nicht im Briefing, durch Teil 3 ausgelöst):** die
  Westseiten-Laufzielregel der Skirmish-KI konnte in einem Nachbargebäude
  enden — Baustelle pausierte dann dauerhaft (im Diagnoselauf: 2.500 Ticks
  lang 0 Fortschritt). Bau- und Ablade-Laufziele werden jetzt footprint-frei
  gewählt.
- **Verifikation:** `dotnet test tools/Nova.SimRunner.Tests` **438/438 grün**
  (neun neue `TroopHandlingTests` je Lane; `CostFieldEpochSnapshotTests` auf
  den Adopt-/Regenerations-Vertrag umgeschrieben). Baselines bewusst neu
  gesetzt: SimRunner-Hash `0xB680C879DEA70B26` (zwei Läufe bitidentisch),
  DETERMINISM_10000 Fingerprint `0xAD8531312FE93F4B`, Final-Hash
  `0x6916A323202089A9`, Playback-Self-Check PASS. Unity-Batchmode-Kompilierung
  aller Skripte fehlerfrei; die EditMode-Ausführung scheiterte hier an der
  Lizenz (0 Entitlements), nicht am Code — nachzuholen auf der Arbeitsmaschine.
- **Offen / DoD-Rest:** die gespielte Runde des Inhabers („Fertig wenn",
  §6) steht aus.
- **Entscheidung:** als D-088 im [DecisionLog](../DecisionLog.md) protokolliert.

## 1. Wo wir stehen

**Die erste vollständige Runde ist gespielt** (Inhaber, 2026-08-07): Menü, Musik, Basisbau, Ernte, Einheitenproduktion, Gefecht, Rundenabschluss. Der Kernloop ist geschlossen. Damit verschiebt sich die Frage von „funktioniert es überhaupt" zu „fühlt es sich richtig an" — und die erste Antwort darauf ist eindeutig: **die Einheiten stehen alle übereinander auf einer Zelle.**

Das ist kein Schönheitsfehler. Ein Stapel Einheiten auf einer Zelle heißt: keine Formation, keine Frontlinie, kein Flankieren, kein Grund, Gelände zu nutzen. Die gesamte taktische Ebene eines RTS hängt daran, dass Einheiten Platz einnehmen.

## 2. Drei Ursachen, alle nachgewiesen

Der Stapel hat nicht einen Grund, sondern drei — und sie verstärken sich gegenseitig.

### 2.1 Alle Einheiten bekommen dieselbe Zielzelle

`UnitCommandStateView`, Anwendung des Move-Befehls:

```
int gridX = SimMath.Clamp(SimFixed.WorldToGrid(move.TargetX), 0, ...);
int gridY = SimMath.Clamp(SimFixed.WorldToGrid(move.TargetY), 0, ...);
var target = new GridPos2D(gridX, gridY);
for (int i = 0; i < move.EntityIds.Length; i++)
    _entityManager.GetUnitRef(id).SetTarget(target);   // <- identisch fuer alle
```

Zwölf markierte Einheiten bekommen zwölfmal dieselbe Zelle. Sie laufen alle exakt dorthin — nicht *in die Gegend*, sondern auf **einen Quadratmeter**. Es gibt keine Formationsverteilung.

### 2.2 Separation gilt nur für Einheiten in Bewegung

`MovementSystem.cs:114`, die erste Zeile der Bewegungsschleife:

```
if (!unit.IsActive || !unit.IsMoving) continue;
```

Die Abstandsrechnung existiert und ist ordentlich gebaut (3×3-Nachbarschaft über ein Spatial Grid, O(1) pro Einheit). Aber sie läuft **ausschließlich für Einheiten, die sich gerade bewegen.** Wer angekommen ist, ruft `Stop()` und fällt komplett heraus: er schiebt nicht mehr, und er lässt sich nicht mehr schieben.

Damit ist der Stapel nicht nur möglich, sondern **dauerhaft**. Die erste Einheit kommt an und wird zum unbeweglichen Hindernis, das keine Abstoßung mehr erzeugt; alle nachfolgenden laufen in sie hinein und bleiben stehen.

### 2.3 Gebäude blockieren die Wegfindung nicht

Im gesamten Produktionscode gibt es **keinen einzigen Aufruf** von `CostField.SetCost` — die einzigen Aufrufer sind ein Test-Bootstrap und Tests. Das `ConstructionSystem` führt zwar ein eigenes 128×128-Belegungsraster für die Bauplatzprüfung, überträgt es aber nie in das Kostenfeld der Wegfindung.

Folge: **Einheiten laufen durch Gebäude hindurch.** Durch das eigene HQ, durch die Kaserne, durch die gegnerische Basis. Das ist derselbe Befund wie oben aus anderer Richtung — die Simulation kennt keinen belegten Raum.

## 3. Was dieser Sprint macht

### 3.1 Formationsverteilung beim Move-Befehl

Ein Move-Befehl auf eine Zelle verteilt die Einheiten deterministisch auf freie Zellen **um** das Ziel herum, statt alle auf dieselbe zu schicken. Regel wie überall in dieser Simulation: index-basiert und stabil — die Einheit mit dem kleinsten Entity-Index bekommt die Zielzelle, die folgenden die Zellen expandierender Chebyshev-Ringe in fester Reihenfolge (aufsteigend y, dann x). Kein `float`, kein Zufall, kein Sortieren nach Entfernung.

`ProductionSystem.TryFindSpawnCell` macht diese Ringsuche bereits vor — dieselbe Konvention, wiederverwenden statt neu erfinden.

**Gilt für denselben Fehler an drei Stellen:** Move-Befehl, Sammelpunkt (Rally) und Produktions-Spawn. Der Spawn prüft heute nur auf Gebäude-Footprints, nicht auf andere Einheiten — frisch gebaute Truppen stapeln sich deshalb genauso vor der Kaserne.

### 3.2 Separation auch im Stand

Angekommene Einheiten nehmen weiter an der Abstandsrechnung teil. Zwei Möglichkeiten, die Entscheidung liegt bei der Umsetzung:

- die Bewegungsschleife läuft für alle aktiven Einheiten, und der Flow-Anteil ist null, wenn sie nicht unterwegs sind; oder
- ein eigener, billiger Entstapelungs-Durchgang nach der Bewegung, der nur Überlappungen auflöst.

Wichtig in beiden Fällen: Der Schub im Stand muss **gedämpft** sein und eine Totzone haben, sonst zittern dicht stehende Einheiten endlos gegeneinander. Eine stehende Einheit soll sich lösen, nicht vibrieren.

### 3.3 Gebäude in das Kostenfeld eintragen

Das Belegungsraster des `ConstructionSystem` wandert bei jeder Platzierung, jeder Fertigstellung, jedem Abriss und jeder Zerstörung ins `CostField`. Das Kostenfeld hat dafür bereits eine `Epoch`, an der die Flow-Field-Zwischenspeicher ihre Gültigkeit erkennen — der Mechanismus ist da und muss nur bedient werden.

**Achtung, zwei Fallen:**

1. **Cache-Invalidierung.** Jede Änderung erhöht die Epoche und verwirft die zwischengespeicherten Flow-Fields. Ein Spieler, der zwanzig Gebäude hintereinander setzt, darf die Wegfindung nicht zwanzigmal komplett neu rechnen lassen.
2. **Einschluss.** Ein Gebäude auf der falschen Zelle kann eine Einheit dauerhaft einsperren. Was passiert mit einer Einheit, die beim Fertigstellen *im* Footprint steht? Sie muss herausgeschoben werden — sonst steht sie für immer in einer Wand.

## 4. Bewusst nicht in diesem Sprint

| Punkt | Warum |
|---|---|
| **Attack-Move** | Seit Sprint 09 feuern Einheiten von selbst auf alles in Reichweite. Der verbleibende Unterschied — unterwegs anhalten und kämpfen statt weiterlaufen — kostet einen neuen `CommandKind` gegen das eingefrorene v1-Register samt Golden-Bytes-Test. Viel Preis für wenig Zugewinn, **jetzt wo Auto-Zielerfassung steht**. Eigener Sprint, und ehrlicherweise nicht der dringendste. |
| Einheiten als dynamische Hindernisse in der Wegfindung | Gebäude sind statisch und gehören ins Kostenfeld. Einheiten sind es nicht — sie über das Kostenfeld zu lösen, würde bei jeder Bewegung die Flow-Fields verwerfen. Dafür ist die Separation zuständig, und die reicht für MS-1. |
| Formationen mit Ausrichtung (Linie, Keil) | Erst wenn Verteilung überhaupt funktioniert. Eine Keilformation über einem Stapel ist sinnlos. |
| Wirtschaftsdruck, KI-Ausbau, Lager/Radar | Eigene Themen, nicht Truppenführung. |

## 5. Der ehrliche Preis

**Das ist eine Simulationsänderung, und zwar eine tiefe.** Bewegung, Zielzuweisung und Kostenfeld gehören zum kanonischen Zustandsverlauf. Die Baselines in `tools/Nova.SimRunner.Tests` werden rot — Fingerprint, Replay, Snapshot-Hash und die Öffnungs-Hashes. Das ist kein Defekt, sondern der Zweck dieser Tests: Sie melden, dass sich das Spielverhalten geändert hat.

Sprint 09 hat denselben Preis für die Zielerfassung bezahlt. Er wird bewusst und dokumentiert bezahlt, nicht stillschweigend.

**Determinismus-Disziplin, nicht verhandelbar:** Die Ringsuche muss eine feste, indexbasierte Reihenfolge haben. Kein `float`, keine Distanzsortierung über Fließkomma, Abstandsvergleiche im Quadrat über `SimFixed`. Zwei Hosts müssen dieselbe Zelle für dieselbe Einheit wählen — sonst laufen die Spielstände auseinander.

## 6. Fertig wenn

Ich markiere zwölf Einheiten und schicke sie irgendwohin. **Sie kommen als Gruppe an und stehen nebeneinander**, nicht ineinander. Ich baue fünf Soldaten hintereinander, und sie stehen als Reihe vor der Kaserne statt als ein Punkt. Ich schicke eine Armee quer über die Karte, und sie läuft **um** meine Basis herum, nicht hindurch. Und wenn ich ein Gebäude auf eine stehende Einheit setze, wird die Einheit herausgeschoben, statt eingemauert zu werden.

---

## 7. Prompt für Kimi

```text
AUFGABE: Truppenfuehrung — Einheiten teilen sich den Platz (Hashkrieg)

AUSGANGSLAGE
Der Kernloop ist geschlossen; der Inhaber hat am 2026-08-07 eine vollstaendige Runde
gespielt: Menue, Musik, Basisbau, Ernte, Produktion, Gefecht, Rundenabschluss. Sprint 09
(D-086/D-087) und Sprint 10 (D-085) sind umgesetzt und committet.
Der auffaelligste verbleibende Mangel: ALLE EINHEITEN STEHEN UEBEREINANDER AUF EINER
ZELLE. Das ist der Inhalt dieses Sprints.

DREI URSACHEN — alle nachgewiesen, nicht neu diagnostizieren

(1) ALLE EINHEITEN BEKOMMEN DIESELBE ZIELZELLE.
    UnitCommandStateView, Anwendung des Move-Befehls: aus TargetX/TargetY wird EINE
    GridPos2D gerechnet, und die Schleife ueber move.EntityIds ruft fuer jede Einheit
    SetTarget(target) mit demselben Wert. Zwoelf markierte Einheiten laufen auf einen
    Quadratmeter. Es gibt keine Formationsverteilung.

(2) SEPARATION GILT NUR FUER EINHEITEN IN BEWEGUNG.
    MovementSystem.cs:114, erste Zeile der Bewegungsschleife:
      if (!unit.IsActive || !unit.IsMoving) continue;
    Die Abstandsrechnung selbst ist gut gebaut (3x3-Nachbarschaft ueber Spatial Grid,
    O(1) pro Einheit). Aber wer angekommen ist, ruft Stop() und faellt heraus: er schiebt
    nicht mehr und laesst sich nicht mehr schieben. Der Stapel ist damit DAUERHAFT.

(3) GEBAEUDE BLOCKIEREN DIE WEGFINDUNG NICHT.
    Im gesamten Produktionscode gibt es KEINEN Aufruf von CostField.SetCost — die
    einzigen Aufrufer sind ein Test-Bootstrap und Tests. ConstructionSystem fuehrt ein
    eigenes 128x128-Belegungsraster fuer die Bauplatzpruefung, uebertraegt es aber nie
    ins Kostenfeld. Einheiten laufen durch Gebaeude hindurch.

TEIL 1 — FORMATIONSVERTEILUNG
Ein Move-Befehl verteilt die Einheiten deterministisch auf freie Zellen UM das Ziel,
statt alle auf dieselbe zu schicken. Regel: kleinster Entity-Index bekommt die Zielzelle,
die folgenden die Zellen expandierender Chebyshev-Ringe in fester Reihenfolge (aufsteigend
y, dann x). ProductionSystem.TryFindSpawnCell macht genau diese Ringsuche bereits —
wiederverwenden statt neu erfinden.
Derselbe Fehler steckt an DREI Stellen, alle drei beheben:
  a) Move-Befehl
  b) Sammelpunkt (Rally)
  c) Produktions-Spawn — TryFindSpawnCell prueft heute nur auf Gebaeude-Footprints, nicht
     auf andere Einheiten, deshalb stapeln sich auch frisch gebaute Truppen vor der Kaserne

TEIL 2 — SEPARATION AUCH IM STAND
Angekommene Einheiten nehmen weiter an der Abstandsrechnung teil. Zwei Wege, waehle einen
und begruende ihn im Report:
  - die Bewegungsschleife laeuft fuer alle aktiven Einheiten, Flow-Anteil null wenn nicht
    unterwegs; oder
  - ein eigener billiger Entstapelungs-Durchgang nach der Bewegung, der nur Ueberlappungen
    aufloest.
WICHTIG in beiden Faellen: Der Schub im Stand braucht Daempfung und eine Totzone, sonst
zittern dicht stehende Einheiten endlos gegeneinander. Eine stehende Einheit soll sich
loesen, nicht vibrieren. Das ist ein Testfall, kein Nebensatz.

TEIL 3 — GEBAEUDE INS KOSTENFELD
Das Belegungsraster des ConstructionSystem wandert bei Platzierung, Fertigstellung,
Verkauf und Zerstoerung ins CostField. Die Epoch-Mechanik fuer die Cache-Invalidierung
existiert bereits und muss nur bedient werden.
ZWEI FALLEN:
  1. Cache-Invalidierung: jede Aenderung verwirft die zwischengespeicherten Flow-Fields.
     Zwanzig Gebaeude hintereinander duerfen nicht zwanzig Komplettneuberechnungen
     ausloesen.
  2. Einschluss: eine Einheit, die beim Fertigstellen IM Footprint steht, muss
     herausgeschoben werden — sonst steht sie fuer immer in einer Wand.

NICHT IN DIESEM SPRINT
- Attack-Move. Seit Sprint 09 feuern Einheiten von selbst auf alles in Reichweite; der
  Rest (unterwegs anhalten statt weiterlaufen) kostet einen neuen CommandKind gegen das
  eingefrorene v1-Register samt Golden-Bytes-Test. Viel Preis, wenig Zugewinn. Eigener
  Sprint.
- Einheiten als dynamische Hindernisse im Kostenfeld — das wuerde bei jeder Bewegung die
  Flow-Fields verwerfen. Dafuer ist die Separation da.
- Formationen mit Ausrichtung (Linie, Keil) — erst wenn Verteilung ueberhaupt geht.

DER EHRLICHE PREIS
Das ist eine tiefe Simulationsaenderung: Bewegung, Zielzuweisung und Kostenfeld gehoeren
zum kanonischen Zustandsverlauf. Die Baselines in tools/Nova.SimRunner.Tests WERDEN rot —
Fingerprint, Replay, Snapshot-Hash, Oeffnungs-Hashes. Das ist kein Defekt, sondern der
Zweck dieser Tests. Baselines bewusst und dokumentiert neu setzen, nicht stillschweigend.

DETERMINISMUS — NICHT VERHANDELBAR
Ringsuche mit fester, indexbasierter Reihenfolge. Kein float, keine Distanzsortierung
ueber Fliesskomma, Abstandsvergleiche im Quadrat ueber SimFixed. Zwei Hosts muessen
dieselbe Zelle fuer dieselbe Einheit waehlen.

VERIFIKATION
Auf der Arbeitsmaschine fehlt das in global.json gepinnte .NET-8-SDK (rollForward:
disable, installiert ist nur 10.0.302). Dieser Sprint aendert die Simulation — also
entweder das SDK 8.0.318 installieren oder den Nachweis ueber die CI im PR fuehren. Eine
Simulationsaenderung ohne gelaufene Simulationstests wird nicht committet.

FERTIG WENN
Ich markiere zwoelf Einheiten und schicke sie irgendwohin — sie kommen als Gruppe an und
stehen NEBENEINANDER, nicht ineinander. Ich baue fuenf Soldaten hintereinander, und sie
stehen als Reihe vor der Kaserne statt als ein Punkt. Ich schicke eine Armee quer ueber
die Karte, und sie laeuft UM meine Basis herum, nicht hindurch. Und wenn ich ein Gebaeude
auf eine stehende Einheit setze, wird die Einheit herausgeschoben statt eingemauert.

ABSCHLUSS
- CHANGELOG.md: Eintrag unter [Unreleased]
- docs/production/DecisionLog.md: neue D-Nummer fuer die Baseline-Neusetzung
- docs/production/hashkrieg/11_Sprint_Truppenfuehrung.md: Status auf "umgesetzt" plus
  Ergebnisblock
- Eigener Branch, main ist PR-only. NICHT pushen ohne ausdrueckliche Freigabe des Inhabers
```
