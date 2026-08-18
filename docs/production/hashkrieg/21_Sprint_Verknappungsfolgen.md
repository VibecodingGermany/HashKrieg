# Sprint 21: Die Folgen der Verknappung — endliche Felder werden lesbar und bespielbar

**Version:** 1.1.0 | **Status:** in Arbeit (21.1 läuft) | **Verantwortungsbereich:** Maintainer-Strang | **Sprint:** 21 | **Vorgänger:** [16_Sprint_Wirtschaft.md](16_Sprint_Wirtschaft.md) | **Parallel zu:** [13B](13B_Sprint_Einheitenverhalten.md) | **Regelwerk:** [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md) | **UX-Gate:** human | **Leitsatz:** endliche Felder sind kein Wert, sondern ein Systemwechsel

## Zweck

Mit [#80](https://github.com/VibecodingGermany/HashKrieg/pull/80) bekamen die
Aetheriumfelder eine endliche Reserve. Der Testbericht T-01 vom 10.08.2026
zeigt, dass diese eine Änderung acht weitere Systeme betrifft, die noch auf der
alten Annahme unendlicher Vorkommen stehen. Dieser Sprint zieht sie nach.

Er tut dabei **drei verschiedene Dinge**, und die Unterscheidung ist wichtig:

1. Er macht **sichtbar**, was die Simulation längst exakt weiß (Restbestand,
   Baubereich, Auswahlinhalt). Kein neues Verhalten, nur der Weg nach außen.
2. Er **spricht aus**, was der Code bereits entscheidet, ohne dass es je
   festgelegt wurde (Territoriumswachstum). Eine Festlegung, keine Umsetzung.
3. Er **ändert die Karte** — Felddichte und die Mitte als Gebiet. Das ist die
   einzige echte Verhaltensänderung des Sprints und die teuerste.

## Herkunft dieser Datei

Aus dem [Vorschlag zur Sprintbildung](20_Vorschlag_Verknappungsfolgen.md),
geschnitten aus [Testbericht T-01](Testberichte/2026-08-10_4053c15_T-01.md)
(Build `4053c15`). Der Vorschlag war ausdrücklich kein Sprint; diese Datei ist
die Festplanung nach den Inhaberentscheidungen vom 2026-08-17
(**D-108** Territorium, **D-109** zentrale Zone).

Ein Befund des Vorschlags ist bereits erledigt: **#85** (KI-Livelock auf dem
leeren Feld) wurde vom Einheitenstrang in
[#97](https://github.com/VibecodingGermany/HashKrieg/pull/97) behoben und ist
geschlossen. Die Vertragsflächen **#89** und **#90** (Patrouille, Bewachen)
bleiben ausdrücklich draußen — Begründung unter „Bewusst nicht in diesem Sprint".

## Ausgangslage — am Code geprüft

Alles hier ist gegen `main` @ `3e10c48` verifiziert.

**Der Restbestand existiert exakt und kommt nirgends an.** `AetheriumField.RemainingAE`
ist monoton fallend, `IsExhausted` ist definiert, `EconomySystem` führt beides
sauber über den Snapshot. Die einzige Stelle, die ihn für einen Menschen liest,
ist `Presentation/UI/DebugHud.cs` — und das Debug-HUD ist kein Spiel-UI. Im
Match sind Felder nur *Ziel* eines Harvest-Befehls (`RtsDeviceInput`), kein
anklickbares Objekt mit eigenem Zustand.

**`AetheriumField` kennt keine Anfangsreserve.** Eine Anzeige „6.420 / 9.000"
braucht den Sollwert. Ihn als Feld in den Zustand zu legen wäre eine
Layoutänderung an `Simulation/State/` — **eingefroren**, D-ID-pflichtig, und für
diesen Zweck unnötig: die Anfangsreserve steht in der kanonischen Kartenlage und
ist von dort ableitbar. Siehe Paket 21.2.

**Die Baubereichsregel ist scharf und unsichtbar.** In
`Simulation/Construction/ConstructionSystem.cs` gilt seit D-104:

```csharp
public const int BuildInfluenceRadiusCells   = 8;   // ab JEDEM eigenen Bauanker
public const int MinimumBuildingDistanceCells = 2;  // sperrt Zellen INNERHALB der Zone
```

Der Spieler erfährt beides nur durch Ablehnung. Seit #83 bekommt er immerhin den
Grund genannt — aber erst nach dem Fehlversuch und ohne zu sehen, wo es ginge.

**Die Befehlskarte liest nur die Führungseinheit.** `CommandCardHud.BuildModel`
nimmt `selected[0]`, fragt `GetUnitCommands(faction, leadRole)` und hängt
`(+N weitere)` an den Titel. Die angebotenen Befehle hängen damit von der
*Reihenfolge* der Auswahl ab, nicht von ihrem Inhalt.

**Die Karte kennt kein natürliches Gelände.** `CostField` unterstützt es
vollständig — `OpenCost = 1`, `ImpassableCost = 255`, Zwischenwerte 2..254 für
schweren Grund. Aber es schreibt **niemand** hinein außer
`ConstructionSystem` (Gebäude-Footprints) und einem Test-Bootstrap. Der
`CostField`-Konstruktor füllt alles mit `OpenCost`.

Und die Optik weiß davon nichts: `Presentation/Maps/GlutrinneBlockoutView`
streut ~84 Felsen mit festem Seed und schreibt laut eigener Zusicherung
*„never writes into simulation state"* — die Felsen sind Deko und begehbar.
**Wer Chokepoints baut, muss beide Seiten aus derselben Quelle speisen**, sonst
laufen Einheiten durch Felsen und bleiben an unsichtbaren Wänden stehen. Das ist
der eigentliche Inhalt von Paket 21.7.

**Die Feldlage steht viermal literal im Repo** (siehe „Risiken").

## Schreibhoheit

Dieser Sprint gehört dem **Maintainer-Strang**. Er fasst keine Datei des
Einheitenstrangs an.

**Erlaubt:**

```
Assets/_Project/Scripts/Gameplay/Match/          MatchBootstrap, Kartenlage, Gelände
Assets/_Project/Scripts/Gameplay/UI/             SelectionManager
Assets/_Project/Scripts/Simulation/Economy/      nur lesend erweitern, kein Layout
Assets/_Project/Scripts/Simulation/Construction/ nur Konstanten, siehe 21.1
Assets/_Project/Scripts/Presentation/            Maps, UI, Overlays
tools/Nova.SimRunner/                            Determinismus-Drehbuch
tools/Nova.SimRunner.Tests/                      eigene neue Tests
```

**Verboten — gehört dem Einheitenstrang (13B):**

```
Assets/_Project/Scripts/Simulation/Combat/
Assets/_Project/Scripts/Simulation/Movement/
Assets/_Project/Scripts/Simulation/Factions/
Assets/_Project/Scripts/Simulation/Pathfinding/
Assets/_Project/Scripts/AI/        Assets/_Project/Scripts/AI.Data/
Assets/_Project/Scripts/Presentation/UI/DebugHud.cs
tools/Nova.AiLab/                  tools/Nova.AiLab.Tests/
```

> **Die eine Feinheit, die Paket 21.7 möglich macht:** Gelände wird über die
> **bestehende öffentliche** `CostField.SetCost` aus `Gameplay/Match/`
> geschrieben — genau so, wie es `PathfindingTestBootstrap` heute schon tut.
> `Simulation/Pathfinding/` selbst wird **nicht angefasst**. Die Schreibhoheit
> bleibt damit gewahrt. Was 21.7 trotzdem auslöst, ist eine *Wirkung* auf den
> Einheitenstrang — die KI läuft danach über eine andere Karte, ihr gepinnter
> Ausgang bewegt sich. Das ist eine Absprache- und Merge-Fenster-Frage, keine
> Hoheitsfrage. Siehe „Risiken".

**Eingefroren, D-ID-pflichtig, in diesem Sprint nicht angefasst:**

```
Simulation/CommandsV1/     kein neuer CommandKind
Simulation/Snapshots/      Simulation/Replays/     Simulation/Systems/
Simulation/State/          Layout und Serialisierung
```

## Pakete

Die Reihenfolge ist nach Abhängigkeit sortiert, nicht nach Aufwand. Jedes Paket
ist ein eigener PR und für sich abnehmbar.

### 21.1 · Jedes Gebäude wird Bauanker (#92 → D-108) — **Verhaltensänderung**

> **Diese Beschreibung ist am 2026-08-18 neu gefasst worden.** Sie stand vorher
> auf einer falschen Prämisse: die Erstfassung von D-108 hielt das Kriechen über
> *jedes* Gebäude für den Ist-Zustand und wollte es nur aussprechen. Der Code
> prüft seit D-104 auf HQ, Lager und Kraftwerk. Der Inhaber hat daraufhin in
> Kenntnis der Lage neu entschieden — die Ankerliste wird **geöffnet**. Das
> Paket ist damit kein Dokumentationspaket mehr, sondern das einzige der ersten
> fünf, das Simulationsverhalten ändert.

**a) Die Messung — erledigt.** `tools/Nova.SimRunner.Tests/BuildZoneCapacityTests.cs`
beziffert die Startzone des HQ-Ankers in zwei Spuren: die echte Systemspur über
`ValidatePlacement` + `PlaceCompletedBuilding`, und ein Geometriemodell, das die
Systemspur beim heutigen Wert zellgenau reproduzieren muss, bevor seine
Variantenzahlen gelten.

| `MinimumBuildingDistanceCells` | Gebäude in der Startzone |
|---|---|
| **2** (Ist, D-104) | **15** |
| 1 | 23 (+53 %) |
| 0 | 23 — identisch zu 1, weil die Footprint-Belegung Abstand unter 1 ohnehin verbietet |

Der einzige wirksame Hebel wäre 2 → 1. **Der Wert bleibt bei 2.** Die Messung
weist die gemeldete Enge nicht als Abstandsproblem aus, und die 15 sind ohnehin
nur eine untere Schranke für die *Anfangs*zone — Anker schieben die Grenze mit
jedem Bau nach außen. Der Test pinnt alle drei Konstanten und macht dieses Paket
erneut auf, falls eine davon später fällt.

**b) Die Regeländerung.** In `ConstructionSystem.IsInsideBuildInfluence` entfällt
die Rollenprüfung:

```csharp
// entfällt:
if (def.Role != UnitRole.HQ && def.Role != UnitRole.Storage && def.Role != UnitRole.Power) continue;
```

Jedes eigene, lebende und fertiggestellte Gebäude wird Anker. `BuildInfluenceRadiusCells`
bleibt 8, `MinimumBuildingDistanceCells` bleibt 2.

**c) Der Docstring wird zweimal nachgezogen.** Er sagt heute „from an own
construction anchor" und nennt die Rollenliste nicht — genau diese Auslassung hat
die falsche Erstfassung von D-108 erzeugt. Erst auf den Ist-Zustand *mit*
Rollenliste (zusammen mit der Messung, damit der Fehler sofort aus dem Code
verschwindet), dann mit der Regeländerung auf die neue Fassung.

> **Verhalten und Baseline nie im selben PR.** Teil b bewegt `RulesHash64`, die
> Determinismus-Baselines **und** den gepinnten Ausgang der kanonischen KI-Partie.
> Also: ein PR für die Regel, ein zweiter für die Baselines — und vorher ein mit
> dem Einheitenstrang abgestimmtes Merge-Fenster, weil `CanonicalAiOutcomeTests`
> arn gehört.

**Fertig wenn:** die Messung als Test liegt, der Docstring die geltende Regel
nennt, jedes Gebäude ankert, und die Baselines in einem eigenen PR nachgezogen
sind.

### 21.2 · Der Restbestand wird sichtbar (#86) — **kritisch**

Der wichtigste Punkt des Sprints. Ohne ihn bleibt die Verknappung eine
unsichtbare technische Mechanik.

Zwei Dinge, die zusammengehören:

**a) Das Vorkommen wird anklickbar und zeigt seine Zahl.** Auswahl eines Feldes
in `RtsDeviceInput`, Anzeige in der Befehlskarte im Format des Berichts:
`Aetherium-Vorkommen — 6.420 / 9.000 AE`.

> **Den Sollwert nicht in den Zustand legen.** `AetheriumField` bekommt **kein**
> neues Feld — das wäre `Simulation/State/`-Layout und damit eingefroren. Die
> Anfangsreserve steht in der kanonischen Kartenlage (`MatchBootstrap.FieldLayouts`)
> und wird von dort gelesen. Ein Feld, das der Präsentation gehört, ist die
> richtige Ablage für eine Zahl, die die Präsentation braucht.

**b) Die Karte liest sich ohne Klick.** Ein volles Vorkommen besteht heute aus
etwa sieben blauen Kristallen (`GlutrinneBlockoutView`). Sie verschwinden
schrittweise mit sinkendem Bestand (7 → 6 → … → leer), und ein erschöpftes Feld
ist eindeutig als erschöpft erkennbar — nicht nur „leer", sondern sichtbar
verbraucht.

> Die Stufung ist reine Präsentation und **darf keine Simulationsabfrage pro
> Frame** werden. Der Kristallstand folgt `RemainingAE` über dieselbe Leseschiene
> wie das übrige HUD.

**Fertig wenn:** ein Vorkommen anklickbar ist, Rest und Anfangsreserve zeigt, und
sein Kristallstand im laufenden Match sichtbar abnimmt.

### 21.3 · Die Startmenge wird gerechnet, nicht geraten (#87)

**Erst nach 21.2**, sonst wird wieder geschätzt. Der Tester lag um fast die
Hälfte daneben (schätzte 5.000, es sind 9.000) — genau weil es keine Anzeige gab.

Die Frage ist nicht „9.000 oder 10.000", sondern **wie lange soll ein Startfeld
tragen?** Das hängt an `HarvestRateAE` (heute 2 AE/Tick), der Harvesterzahl und
den Baukosten. Rechne es einmal: wie viele Sekunden Spielzeit trägt ein
Startfeld bei einem, bei zwei, bei drei Harvestern? Setze den Wert danach, nicht
davor, und schreib die Rechnung in die PR-Beschreibung.

> **Vier Spiegel.** Siehe „Risiken" — jede Änderung an der Feldlage geht durch
> alle vier Stellen oder durch keine.

### 21.4 · Der Baubereich wird sichtbar (#91)

**Erst nach 21.1**, sonst zeigt das Overlay eine Regel, die sich danach ändert —
und seit der Neufassung von D-108 ändert sie sich wirklich.

Beim Anklicken des HQ und im Platzierungsmodus wird der erlaubte Bereich
angezeigt. **Nicht als Radius-Ring** — der wäre unehrlich, weil
`MinimumBuildingDistanceCells` Zellen *innerhalb* der Zone sperrt. Einzufärben
sind die **tatsächlich baubaren Zellen** für den gerade gewählten Footprint.
Die Prüfung existiert bereits in `ConstructionSystem`; es geht ausschließlich
darum, die vorhandene Antwort *vor* dem Klick zu zeigen statt danach.

> Damit beantwortet das Overlay nebenbei die Beschwerde aus 21.1 direkt: der
> Spieler sieht, wie viel Platz noch da ist, statt es zu vermuten.

**Fertig wenn:** ein Spieler ohne Fehlversuch erkennt, wo das nächste Gebäude
hinpasst — und warum eine Zelle innerhalb des Radius trotzdem gesperrt ist.

### 21.5 · Die Auswahl sagt die Wahrheit (#88)

**Abhängigkeitsfrei — jederzeit einzeln machbar.** Der handfeste Teil ist Punkt 3.

`CommandCardHud.BuildModel` liest heute `selected[0]` und fragt
`GetUnitCommands(faction, leadRole)`. Daraus folgen drei Lücken:

1. **Keine Typenaufschlüsselung.** „Panzer (+7 weitere)" sagt nicht, ob das
   sieben Panzer sind oder ein Harvester und sechs Pioniere.
2. **Kein Zustand.** HP taucht in der Karte nicht auf.
3. **Die Befehle stammen vom Anführer statt von der Schnittmenge.** Steht ein
   Harvester zufällig auf Position 0, bietet die Karte „Ernten" an, obwohl es
   für die anderen sieben nichts bedeutet — und „Ernten" verschwindet, sobald ein
   Panzer die Führung übernimmt, obwohl ein Harvester mitmarkiert ist. Die
   angebotenen Befehle hängen an der Reihenfolge der Auswahl.

Punkt 3 wird zur **Schnittmenge über alle Rollen der Auswahl**. Punkt 1 und 2
werden zur Aufschlüsselung nach Typ mit Anzahl und Sammel-HP.

> **Zwei Nachträge pro neuer HUD-Zeile.** `CommandCardHud.EstimateHeight` bildet
> die Höhenrechnung von `OnGUI` Zeile für Zeile nach — der Kommentar dort
> dokumentiert genau diesen Fehler („~40 px short … visible, but not
> clickable"). Und jede neue Trefferfläche gehört in `IsPointerOverHud`, sonst
> schlagen Klicks hinter dem Panel in die Welt durch.

Alle Daten liegen bereits in `EntityManager`. Kein Simulationseingriff.

### 21.6 · Die Karte trägt mehr Felder (#93)

**Erst nach 21.1.** Wie weit eine Basis wachsen darf, bestimmt mit, wie dicht
Felder liegen dürfen.

Heute: fünf Felder auf 128×128, davon zwei als Startbasis gebunden. Für zwei
Spieler bleiben **drei umkämpfte Felder** — daraus entsteht keine Entscheidung,
sondern ein Wettlauf: jeder nimmt das nächstgelegene freie Feld, die Mitte
entscheidet den Rest. Der Bericht verlangt „mindestens ungefähr doppelt so
viele".

Kapazitätsseitig ist Luft: `EconomySystem.MaxFields = 64`. Die Zahl ist reine
Kartengestaltung, keine technische Grenze.

Zu liefern: eine neue Feldlage mit begründeter Anzahl und Verteilung, unter
Wahrung der **bindenden Symmetrie** aus D-107 — Punkte spiegeln als
`(x, y) → (124 - x, 124 - y)`. Asymmetrie ist hier kein Geschmacksfehler,
sondern ein Balancefehler.

> Die Mitte bleibt in diesem Paket **ein** Feld. Sie wird erst in 21.7 zum Gebiet
> — erst die Gesamtzahl, dann die Verteilung.

### 21.7 · Die Mitte wird ein Gebiet (#94 → D-109) — **das teuerste Paket**

**Erst nach 21.6.** Zwei Hälften, und die zweite ist die schwierige.

**a) Kartenlage.** Vier bis sechs Felder in der Mitte gruppieren, statt des einen
Feldes bei (62, 62) mit 15.000 AE. Symmetrie nach D-107, Gesamtreserve der Zone
begründet gegen die Startfelder.

**b) Chokepoints — und hier liegt die eigentliche Arbeit.** Schmale Zufahrten
brauchen unbegehbares Gelände. Die Maschinerie existiert (`CostField.ImpassableCost`),
benutzt wird sie nur von `ConstructionSystem`. Zu bauen ist:

> **Eine einzige autoritative Geländequelle**, aus der *beide* Seiten lesen —
> die Simulation über `CostField.SetCost` und die Optik über
> `GlutrinneBlockoutView`. Genau so, wie die fünf Aetheriumfelder es heute schon
> machen: `MatchBootstrap` hält die kanonische Lage, die Blockout-View baut sich
> daraus. Das Gelände gehört in dieselbe Struktur.
>
> **Das ist die Kernanforderung dieses Pakets.** Zwei getrennte Quellen ergeben
> Einheiten, die durch Felsen laufen und an unsichtbaren Wänden hängenbleiben —
> und das ist ein Fehlerbild, das später niemand mehr der Kartenarbeit zuordnet.

Zusätzlich zu prüfen und in der PR zu beantworten:

- Sind die Zufahrten breit genug für die Gruppenbewegung? Ein Chokepoint, durch
  den eine Formation nicht passt, ist kein Engpass, sondern eine Blockade.
- Bleibt jedes Feld und jedes HQ von jedem Startpunkt aus erreichbar? Ein
  Erreichbarkeitstest über das `FlowField` gehört in
  `tools/Nova.SimRunner.Tests/` — sonst sperrt eine spätere Kartenänderung
  unbemerkt eine Basis ein.

**Nicht in diesem Paket:** Verteidigungstürme an den Zufahrten balancieren, und
das Verhalten der KI in der Mitte. Die KI kann heute nicht um ein Gebiet
kämpfen; das ist Sache des Einheitenstrangs und keine Bringschuld dieses Sprints.

## Bewusst nicht in diesem Sprint

| Was | Warum |
|---|---|
| **#89 Patrouille, #90 Bewachen** | Beide brauchen einen Eintrag im **eingefrorenen** `CommandKind`-Register (Schema v1, heute 1–17), eine neue Payload und Zustand pro Einheit im Snapshot. Das ist ein API-/Schema-Vorgang: @api-guardian ist Pflicht und die Versionsrelevanz ist eher `major` als `minor`. Wer sie einem Strang still zuschlägt, bricht entweder die Hoheit oder das Schema |
| **KI-Verhalten an den Feldern** | Expansion, Feldsicherung und Eskorten gehören dem Einheitenstrang (13B). Dieser Sprint macht die Karte reicher; sie zu bespielen ist arns Paket |
| **Verteidigungstürme balancieren** | Erst wenn die Mitte steht und einmal gespielt wurde |
| **Doppelte Feldlage auflösen** | Die Verdopplung zwischen `MatchBootstrap` und `Determinism10000Scenario` wäre einen eigenen Aufräum-Issue wert, aber nicht mitten in einer Kartenänderung |

## Risiken

**R-1 · Die Feldlage steht viermal literal im Repo.** Verifiziert gegen
`main` @ `3e10c48`:

```
Assets/_Project/Scripts/Gameplay/Match/MatchBootstrap.cs:168
tools/Nova.SimRunner/Determinism10000Scenario.cs:210
tools/Nova.SimRunner.Tests/CanonicalMatchSetupTests.cs:85
Assets/Tests/EditMode/Gameplay/CanonicalMatchSetupTests.cs:113
```

Drei von vier ergibt einen roten Test, der wie ein Determinismusfehler aussieht
und keiner ist. Betrifft 21.3, 21.6 und 21.7.

**R-2 · Verhalten und Baseline nie im selben PR.** Die wichtigste Regel des
Parallelbetriebs. **21.1 Teil b**, 21.3, 21.6 und 21.7 ändern Simulationsverhalten und lassen
`SnapshotGoldenBytesTests`, `CommandGoldenBytesTests`, `SimRandomGoldenTests`
und `Determinism10000Tests` rot werden. Das ist ihr Zweck. Die Baseline wird in
einem **eigenen** PR neu gesetzt, mit altem und neuem Wert im Text und der
Begründung, warum die Änderung gewollt ist. Das Drehbuch
`Determinism10000Scenario.cs` ist davon **nicht** betroffen und darf im selben
PR nachgezogen werden.

**R-3 · 21.1 Teil b, 21.6 und 21.7 bewegen den gepinnten KI-Ausgang.** Eine
andere Bauregel und eine andere Karte heißen eine andere kanonische Partie. `CanonicalAiOutcomeTests` gehört dem
Einheitenstrang. **Vor** dem Merge von 21.1 Teil b und 21.6 mit arn abstimmen, in welchem
Merge-Fenster das läuft — „ein Fenster hat einen Strang". Zwei Stränge in einem
Fenster machen einen roten Test nicht zuordenbar.

**R-4 · Verteilte Testbuilds altern.** Der Fingerprint sperrt Matches zwischen
ungleichen Builds. Nach jedem Fenster: Build für jede Plattform, an der jemand
testet, neuer Build an alle Testenden, alter Build ist ungültig.

**R-5 · `tools/build/` ist ignoriert.** Der Ordner sieht aus wie der
Packaging-Ordner, ist aber über `.gitignore` ausgeschlossen. Änderungen dort
verschwinden. Der getrackte Weg ist ausschließlich `tools/packaging/`.

## Fertig wenn

- [ ] Jedes eigene fertiggestellte Gebäude erweitert die Bauzone, und der
      Docstring nennt die geltende Regel statt eines vagen „anchor" (21.1)
- [ ] Ein Spieler liest den Restbestand jedes Vorkommens ab, ohne das Debug-HUD
      zu öffnen — als Zahl und am Kristallstand auf der Karte (21.2)
- [ ] Der Baubereich ist vor dem Klick sichtbar, inklusive der gesperrten Zellen
      innerhalb des Radius (21.4)
- [ ] Die Befehlskarte zeigt bei Mehrfachauswahl Typen, Anzahl und Zustand, und
      bietet nur Befehle an, die für **alle** gewählten Einheiten gelten (21.5)
- [ ] Die Startmenge steht auf einem gerechneten Wert, und die Rechnung ist
      nachlesbar (21.3)
- [ ] Die Karte trägt genug Felder, dass ein Spieler zwischen Alternativen
      wählt statt zu rennen (21.6)
- [ ] Die Mitte ist ein Gebiet mit schmalen Zufahrten, Optik und Begehbarkeit
      stammen aus **einer** Quelle, und ein Erreichbarkeitstest sichert, dass
      keine Basis eingesperrt ist (21.7)
- [ ] `dotnet test tools/Nova.SimRunner.Tests` grün, Baselines in eigenen PRs
      bewegt
- [ ] **Eine gespielte Runde** — dieser Sprint ist überwiegend Oberfläche und
      Kartengefühl; die CI kann davon fast nichts belegen

## Changelog-Notiz

Je Paket eine Zeile unter `[Unreleased]`. Für 21.6 und 21.7 gehört die alte und
die neue Feldlage in den Eintrag, nicht nur „mehr Felder".

## Versionsrelevanz

`minor`. Kein Vertrag bricht: kein neuer `CommandKind`, kein
`StateVersion`-Bump, keine Änderung an `SimDefinitions`.

> **`RulesHash64` bewegt sich aber**, und zwar durch 21.1 Teil b (Ankerliste).
> Das ist eine Regelrevision und kein Schnittstellenbruch: alte und neue Builds
> können nicht miteinander spielen, aber nichts an der Schnittstelle ändert
> sich. Verteilte Testbuilds sind danach ungültig — siehe R-4.
> `MinimumBuildingDistanceCells` und `BuildInfluenceRadiusCells` bleiben
> unverändert; fällt später doch einer der beiden Werte, ist das ein eigener PR
> mit eigener D-ID.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.1.0 | 2026-08-18 | **Paket 21.1 neu gefasst.** Die Erstfassung hielt das Kriechen über jedes Gebäude für den Ist-Zustand; `IsInsideBuildInfluence` prüft seit D-104 auf HQ, Lager und Kraftwerk. Der ausführende Agent hat den Widerspruch gefunden und angehalten, der Inhaber hat D-108 in Kenntnis der Lage neu getroffen: die Ankerliste wird geöffnet. 21.1 ist damit eine Verhaltensänderung mit `RulesHash64`-Bewegung und eigenem Merge-Fenster; die Messung (15/23/23) ist erledigt und `MinimumBuildingDistanceCells` bleibt bei 2. R-2, R-3, Versionsrelevanz und „Fertig wenn" nachgezogen | Orchestrator |
| 1.0.0 | 2026-08-17 | Erstfassung aus [20_Vorschlag_Verknappungsfolgen.md](20_Vorschlag_Verknappungsfolgen.md) nach den Inhaberentscheidungen D-108 und D-109. #85 als vom Einheitenstrang erledigt ausgetragen, #89/#90 als Vertragsflächen ausgeschlossen | Orchestrator |
