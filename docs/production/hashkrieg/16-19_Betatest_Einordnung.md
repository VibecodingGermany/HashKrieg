# Einordnung des ersten Betatest-Berichts in die Sprintfolge

**Status:** Vorschlag zur Sprintbildung — **keine beschlossene Sprintplanung** | **Quelle:** [Testberichte/2026-08-09_a434e2c_T-01.md](Testberichte/2026-08-09_a434e2c_T-01.md) | **Regelwerk:** [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md), [Nutzerfeedback_Ablauf.md](../Nutzerfeedback_Ablauf.md) | **Leitsatz:** die Schreibhoheit entscheidet die Sprintgrenze, nicht das Thema

## Zweck

Der erste Betatest-Bericht (Build `a434e2c`, 09.08.2026) ist in 16 Issues
zerlegt — #43 bis #58. Dieses Dokument ordnet sie in die bestehende Sprintfolge
ein, damit daraus Sprintdateien entstehen können.

Es ändert **keine Zeile Code** und trifft **keine Entscheidung**. Vier Punkte
brauchen eine Inhaberentscheidung, sie sind unten als solche gekennzeichnet.

## Der Leitgedanke

Die Themen aus dem Bericht sortieren sich nicht nach Gefühl, sondern nach
**Schreibhoheit**. Der Parallelbetrieb ab Sprint 13 teilt die Simulation in
zwei Stränge, und drei der gemeldeten Fehler liegen genau auf der Naht:

- `Simulation/Combat/`, `Movement/`, `AI/`, `Pathfinding/` gehören exklusiv dem
  **Einheitenstrang 13B** (externer Beitragender, PR-only)
- `Simulation/Construction/`, `Economy/`, `Production/` fasst in 13–15
  **niemand** an — sie sind für **Sprint 16** reserviert
- `Scripts/Presentation/`, `Gameplay/`, `Simulation/State/`, `CommandsV1/`
  gehören dem **Maintainer-Team**

Ein Sprint, der diese Grenze überschreitet, hebt die Trennung auf, die den
Parallelbetrieb erst möglich macht. Deshalb ist die Einordnung unten nach
Hoheit geschnitten, nicht nach Thema — sonst läge „alles zum Kampf" in einem
Paket und wäre von zwei Leuten gleichzeitig zu bearbeiten.

## Die vollständige Einordnung

| Issue | Kurz | Hoheit | Vorschlag | Blockiert durch |
|---|---|---|---|---|
| #49 | Auswahlrahmen zu wuchtig | Maintainer | **Sofort** | — |
| #47 | Energie als „nicht genug Aetherium" gemeldet | Maintainer | **Sofort** | — |
| #45 | „Stoppen" bricht Angriff nicht ab | Maintainer | **Sofort** (Kernteil) | — |
| #43 | Sammler startet nicht | Sprint-16-Bereich | **Sprint 16** | — |
| #46 | Einheiten teleportieren zum Sammelpunkt | Sprint-16-Bereich | **Sprint 16** | — |
| #44 | Baustellen schießen | **geteilt** | **Sprint 16** + Absprache 13B | Vertragsfläche |
| #53 | Lager ohne Wirkung | Sprint-16-Bereich | **Sprint 16** | Entscheidung |
| #54 | Radar ohne Wirkung | Sprint-16-Bereich | **Sprint 16** | Entscheidung |
| #48 | Energie fehlt im HUD | Maintainer | **Sprint 16** | — |
| #50 | Einheit im Pulk nicht auffindbar | Maintainer | **Sprint 18** | — |
| #51 | Angriffsziel unsichtbar, kein Nachsetzen | **geteilt** | **Sprint 18** + Absprache 13B | Vertragsfläche |
| #52 | Formationen | **geteilt** | **Sprint 18** | Registerfrage |
| #57 | Gebäude durchsichtig und hohl | Art | **Sprint 19** | — |
| #58 | Radarturm sprengt den Maßstab | Art | **Sprint 19** | #19 |
| #55 | Reparaturzone | Sprint-16-Bereich | **später** | Balancing |
| #56 | Sanitäter einplanen | nur Doku | **jederzeit** | — |

## Sofort — drei Korrekturen ohne Sprintbindung

Diese drei berühren je eine Datei im Maintainer-Bereich, hängen von nichts ab
und kollidieren mit keinem laufenden Strang. Sie brauchen keinen Sprint,
sondern einen Nachmittag.

- **#49 Auswahlrahmen** — eine Konstante in `GroundMarkerVisuals.cs`. Wirkung
  sofort sichtbar, gut geeignet als erster Beitrag von außen
- **#47 Blocker-Grund** — eine Funktion in `BuildMenuHud.cs`. Der Spieler liest
  heute eine **falsche** Begründung; das ist schlimmer als gar keine
- **#45 „Stoppen"** — vier Zeilen in `UnitCommandStateView.cs`. Der Kernteil
  (Angriffsbefehl löschen) liegt vollständig beim Maintainer

Warum zuerst: alle drei sind Fehler, die bei jedem weiteren Testlauf erneut
gemeldet werden. Solange sie stehen, verstellen sie die Sicht auf die Fragen,
die der Bericht offenlassen musste — Tempo, Schwierigkeit, Frust, Spaß.

## Sprint 16 — „Die Wirtschaft trägt sich selbst"

Sprint 16 ist bereits als **Strang C** geplant (Knappheit, Lager, Radar, Low
Power, Bauvoraussetzungen, Platzierungsregeln). Der Betatest trifft ihn
punktgenau: sechs der 16 Issues liegen in genau diesem Bereich. Er braucht
deshalb keine neue Nummer, sondern eine erweiterte Paketliste.

**Vorschlag für die Pakete:**

### 16.1 · Der Kreislauf startet von allein (#43)

Der geschenkte Sammler bekommt einen Erntebefehl. Ohne ihn ist die erste
Spielminute kaputt, und das ist der erste Eindruck jedes Testers.

### 16.2 · Einheiten verlassen das Gebäude (#46)

Spawn am Footprint statt an der Sammelpunkt-Zelle, danach Bewegungsbefehl. Der
Sammelpunkt wird wieder ein Ziel statt eines Teleporters.

### 16.3 · Baustellen schießen nicht (#44)

**Vertragsfläche — siehe unten.** Der Anteil in `Construction/` liegt hier.

### 16.4 · Lager und Radar bekommen eine Wirkung (#53, #54)

Beides war als Strang-C-Inhalt ohnehin geplant. Der Bericht liefert das
Argument, es nicht weiter zu verschieben: beide Gebäude ziehen Energie und
**halbieren bei knapper Bilanz die Produktion**. Sie sind nicht wirkungslos,
sie schaden.

### 16.5 · Energie wird sichtbar (#48)

Vom Tester als wichtigster Einzelpunkt benannt. Gehört inhaltlich zu Low Power
und damit in denselben Sprint — ein Zustand, der die Produktion halbiert, darf
nicht unsichtbar sein.

**Warum diese fünf zusammen:** sie liegen alle im selben Schreibbereich
(`Construction/`, `Economy/`, `Production/`, `Presentation/UI/`), der in 13–15
bewusst unangetastet bleibt. Sie einzeln zu ziehen würde denselben Bereich
mehrfach öffnen.

## Sprint 18 — „Die Truppe gehorcht sichtbar"

Neu vorzuschlagen. Alles, was mit Auswahl, Zielwahl und Gruppenführung zu tun
hat — der Teil des Berichts, in dem der Tester wusste, was er wollte, es aber
nicht ausdrücken konnte.

### 18.1 · Die Auswahl ist lesbar (#50)

Übersicht der markierten Einheiten in der Befehlskarte, Auswahl nach Rolle, der
Pionier auffindbar. Der Bericht hält fest, dass **der Bauablauf daran abbrach**
— das ist kein Komfort.

### 18.2 · Das Angriffsziel ist sichtbar und wird verfolgt (#51)

**Vertragsfläche — siehe unten.** Der Eingabeteil liegt hier.

### 18.3 · Formationen (#52)

Braucht vorab eine Entscheidung zur Registerfrage — siehe unten.

**Warum nach Sprint 16:** #50 und #51 wirken erst, wenn der Auswahlrahmen (#49)
schon dünn ist, sonst verdeckt die Markierung weiter, was sie zeigen soll.

## Sprint 19 — „Die Basis sieht aus wie eine Basis"

Neu vorzuschlagen, Art-Strang.

### 19.1 · Gebäude bekommen einen Körper (#57)

Zuerst eingrenzen, ob Material oder Geometrie — die Behebung unterscheidet sich
vollständig.

### 19.2 · Der Maßstab stimmt (#58)

Hängt an der offenen Frage #19 (`1 Zelle = 3,0 m`, bisher nur art-seitige
Annahme).

**Warum zuletzt:** #57 wird durch #46 verschärft. Sobald Einheiten wie gewünscht
**aus** dem Gebäude fahren, sieht man durch ein hohles Gebäude hindurch, wie sie
darin stehen. Die Art-Korrektur nach der Simulationskorrektur zu machen spart
einen Durchgang.

## Die drei Vertragsflächen mit dem Einheitenstrang

Diese Punkte liegen auf der Naht zwischen Maintainer und 13B. Sie brauchen eine
**Absprache vor der Umsetzung**, nicht danach.

### #44 Baustellen schießen

Die Ursache liegt in `Construction/` (Maintainer, Sprint 16): eine Baustelle
bekommt `UnitRole.Unit` und damit die bewaffnete Fallback-Waffe. Die saubere
Behebung braucht aber, dass `CombatSystem` Baustellen überspringt — und
`Simulation/Combat/` gehört **exklusiv 13B**.

Zwei Wege ohne Hoheitsverletzung:

1. `ConstructionSystem` stellt eine Abfrage bereit, `CombatSystem` konsumiert
   sie — dann braucht 13B eine kleine Änderung, abgestimmt wie bei
   `FogOfWarSystem.GetTeamView`
2. Die Baustelle bekommt eine unbewaffnete Rolle statt `UnitRole.Unit` — bliebe
   vollständig im Maintainer-Bereich, braucht aber eine neue Rolle in
   `Definitions/` (geteilter Bereich)

Weg 2 ist der kleinere Eingriff und sollte geprüft werden, bevor 13B
eingebunden wird.

### #51 Angriffsziel verfolgen

Die Sichtbarkeit des Ziels liegt vollständig beim Maintainer
(`RtsDeviceInput`, `SelectionMarkerView`). Das **Nachsetzen** einer Einheit
außer Reichweite berührt das Zusammenspiel mit `CombatSystem`.

Es gibt einen Weg ohne 13B: der Reparaturbefehl löst dasselbe Problem bereits,
indem er **zwei Intents** schickt — Bewegung, dann Befehl. Dasselbe Muster
trägt hier und bleibt im Eingabebereich.

### #52 Formationen

Die Zielverteilung gehört in die Simulation. Vorab zu klären ist, ob eine
Formation ein **neuer Befehlstyp** wird — dann braucht sie einen Eintrag im bei
G1 eingefrorenen Register plus Golden-Byte-Tests — oder ob sie als mehrere
bestehende `Move`-Befehle aufgelöst wird. Die zweite Variante berührt das
Register nicht und ist deshalb deutlich billiger.

## Was eine Inhaberentscheidung braucht

| Frage | Issue | Warum sie nicht technisch entscheidbar ist |
|---|---|---|
| Bekommt das Lager eine Kapazitätsgrenze, eine andere Wirkung, oder verschwindet es? | #53 | Eine Kapazitätsgrenze greift in die Wirtschaft und die MS-1-Balance |
| Schaltet das Radar die Minimap frei? | #54 | Eine Minimap wegzunehmen, die es bisher immer gab, fühlt sich als Verlust an |
| Ist „Stoppen" ein Halte-Feuer oder löscht es nur den Befehl? | #45 | Ohne Halte-Feuer bleibt der Knopf im Gefecht optisch wirkungslos |
| Formation als neuer Befehlstyp oder als aufgelöste Move-Befehle? | #52 | Das eine bricht den G1-Registerfrost auf, das andere nicht |

Hinweis zu #53/#54: beide hängen an der offenen Grundsatzfrage #12 (Richtung
der Wirtschaft). Option B dort würde die Rolle eines Lagers ohnehin neu
definieren. Solange #12 offen ist, sollte die Wirkung von Lager und Radar
**klein und umkehrbar** gewählt werden.

## Was der Bericht nicht beantwortet hat

Abschnitt 6 des Berichts blieb leer, und zu Tempo, Schwierigkeit, Zähigkeit,
Frust und Spaß kam nichts. Das ist kein Versäumnis des Testers: die 16
gemeldeten Punkte haben den Blick darauf verstellt.

Diese Fragen gehören in den **nächsten** Testdurchlauf, nach den Sofortkorrekturen
und Sprint 16. Vorher sind sie nicht sinnvoll zu beantworten.

## Nicht aus diesem Bericht

[#42](https://github.com/VibecodingGermany/Project_Nova/issues/42) (Zittern im
Pulk) stammt vom externen Beitragenden und liegt in `Simulation/Movement/` —
also **13B**, nicht in dieser Einordnung. Es berührt #52: eine Formation
verteilt die Ziele und nimmt dem Zittern die Grundlage, ersetzt die Behebung
dort aber nicht.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-08-09 | Einordnung der Issues #43–#58 aus dem ersten Betatest-Bericht | Orchestrator |
