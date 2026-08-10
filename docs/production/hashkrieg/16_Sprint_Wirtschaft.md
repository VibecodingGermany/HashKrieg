# Sprint 16: Die Wirtschaft trägt sich selbst — kein Gebäude kostet Geld, ohne etwas zu tun

**Version:** 1.2.0 | **Status:** in Umsetzung | **Verantwortungsbereich:** Netzstrang (Maintainer) | **Sprint:** 16 | **Vorgänger:** [12_Sprint_Zu_Zweit.md](12_Sprint_Zu_Zweit.md) Strang C | **Parallel zu:** [13B](13B_Sprint_Einheitenverhalten.md) | **Regelwerk:** [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md) | **UX-Gate:** human | **Leitsatz:** ein Gebäude, das Strom zieht und nichts tut, ist kein Platzhalter, sondern ein Schaden

## Zweck

Sprint 16 führt zwei Dinge zusammen, die bisher getrennt geplant waren: **Strang
C aus Sprint 12** (Knappheit, Lager, Radar, Low Power, Bauvoraussetzungen,
Platzierung) und die **acht Betatest-Befunde**, die in genau denselben
Schreibbereich fallen.

Er ändert erstmals seit Sprint 13 wieder Simulationsverhalten aus der Hand des
Maintainer-Teams und läuft dabei **parallel** zum Einheitenstrang. Die Regel, die
das trägt, ist [D-095](../DecisionLog.md).

## Herkunft dieser Datei

[../Nutzerfeedback_Ablauf.md](../Nutzerfeedback_Ablauf.md) Schritt 5 verbietet
ausdrücklich, aus einem Testbericht heraus eine Sprintdatei anzulegen. Diese
Datei entsteht deshalb nicht aus dem Bericht, sondern aus der **Inhaberentscheidung
vom 2026-08-09**, die Vorschläge aus
[16-19_Betatest_Einordnung.md](16-19_Betatest_Einordnung.md) anzunehmen und
Sprint 16 vorzuziehen. Die beiden dort offenen Wirtschaftsfragen (#53 Lager,
#54 Radar) sind mit derselben Entscheidung beantwortet.

## Abhängigkeiten

- [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md) — Schreibhoheit, Baseline-Regel, Definitions-Hash
- [12_Sprint_Zu_Zweit.md](12_Sprint_Zu_Zweit.md) §Strang C — die Zielwerte C1–C6
- [16-19_Betatest_Einordnung.md](16-19_Betatest_Einordnung.md) — Herkunft der Issues #43–#58
- [../MVPContentManifest.md](../MVPContentManifest.md) — Feldreserve-Sollwerte

## Ausgangslage — am Code geprüft, nicht aus dem Masterplan übernommen

| Befund | Beleg |
|---|---|
| **Es gibt keine Obergrenze für Aetherium** | `PlayerEconomyState.AddCredits` addiert bedingungslos in ein `long`; `UnitRole.Storage` hat ausser `PowerRequired 5` kein Verhalten |
| **Das Radar tut nichts** | Kein Simulationscode liest `UnitRole.Radar`. `FogOfWarSystem.GetRadarSignatures` hat **null Produktionsaufrufer** — die einzigen Aufrufer sind Tests |
| **Radar strahlt heute aus jeder Einheit** | `GetRadarSignatures` multipliziert die `SightRadius` **jeder** eigenen Entität mit `RadarRadiusMultiplier = 2` |
| **Die Minimap ist immer da** | `MinimapHud.OnGUI` zeichnet bedingungslos, sobald `MatchRunner.FogOfWar` existiert. Kein Schalter, kein Eintrag in `GameSettings` |
| **Der geschenkte Sammler bekommt keinen Befehl** | `ConstructionSystem.GrantFoundingHarvester` spawnt über `EntityManager.SpawnUnit`; `HarvestFieldId` bleibt 0, und nichts holt das nach |
| **…und er wird bei *jeder* Raffinerie geschenkt** | `CompleteSite` ruft `GrantFoundingHarvester` ohne Spieler-Latch und ohne Zähler. Zweite Raffinerie und jeder Wiederaufbau schenken erneut |
| **…und scheitert stumm** | Entity-Store voll, Fraktionsdefinition fehlt oder acht Ringe ohne freie Zelle: die Funktion kehrt ohne Meldung zurück |
| **Einheiten entstehen am Sammelpunkt** | `ProductionSystem.TryFindSpawnCell` probiert **zuerst die Rally-Zelle**, dann Ringe 1..8. Der Sammelpunkt ist damit ein Teleporter, kein Ziel |
| **Baustellen sind bewaffnet** | `SpawnBuildingEntity(completed: false)` vergibt `UnitRole.Unit`; `WeaponProfiles.BuildTable` stempelt genau diesen Slot mit `FallbackAttackDamage = 15`. Mit D-087 (Auto-Zielerfassung) schiessen sie wirklich |
| **Reparatur kostet nichts** | `ProcessRepairOrders` addiert `RepairRateHpPerTick = 10`, ohne je `TrySpendCredits` zu rufen — und ist als einziger Fortschrittspfad nicht von Strommangel betroffen |
| **Low Power ist ein Viertel gebaut** | `ProductionSpeedMultiplierQ16` ist der einzige Effekt |
| **Der Blockergrund ist falsch** | `ValidatePlacement` liefert für drei Ursachen denselben Code `RejectedPrerequisitesNotMet`: fehlendes Vorgängergebäude, fehlende Energie, kein freies Baustellenkontingent (alle 64 gleichzeitigen Baustellen belegt, `MaxSites`). Ein belegter Footprint ist ein anderer Zweig und liefert `RejectedInvalidTarget` |
| **Energie ist bereits sichtbar — nur nicht dort, wo entschieden wird** | `DebugHud.DrawStatusBar` zeigt dauerhaft `<AE> AE | Power <a>/<b>` samt `(LOW POWER)`. Der Tester hat sie nicht gefunden, weil sie nicht am Baumenü steht |

Der letzte Punkt korrigiert die Einordnung: **#48 ist kein fehlendes Feature,
sondern ein Platzierungsfehler.** Das ändert den Aufwand, nicht die Dringlichkeit.

## Schreibhoheit

| Pfad | Paket |
|---|---|
| `Scripts/Simulation/Economy/` | 16.4, 16.6, 16.7 |
| `Scripts/Simulation/Construction/` | 16.1, 16.3, 16.9 |
| `Scripts/Simulation/Production/` | 16.2 |
| `Scripts/Simulation/Vision/FogOfWarSystem.cs` | 16.5 — Vertragsfläche, `GetTeamView` bleibt unverändert |
| `Scripts/Simulation/State/UnitCommandStateView.cs` | 16.10 — **nur Befehlsanwendung**, kein Feld, keine Reihenfolge, kein `StateVersion` |
| `Scripts/Simulation/Definitions/SimDefinitions.cs` | 16.8 — `PrerequisiteRole`. **Geteilt mit 13B, Absprache vor dem PR** |
| `Scripts/Gameplay/Match/MatchBootstrap.cs` | 16.7 — Startaufstellung |
| `Scripts/Presentation/Maps/GlutrinneBlockoutView.cs` | 16.7 — Feldmarker und Steinstreu-Ausschluss |
| `Scripts/Presentation/UI/` (`BuildMenuHud`, `MinimapHud`, `MatchFrameHud`) | 16.5, 16.10 |
| `Scripts/Gameplay/UI/CommandCardPresenter.cs` | 16.10 — Strombedarf am angeklickten Gebäude |
| `tools/Nova.SimRunner/Determinism10000Scenario.cs`, `tools/Nova.SimRunner.Tests/CanonicalMatchSetupTests.cs`, `Assets/Tests/EditMode/Gameplay/CanonicalMatchSetupTests.cs` | 16.7 — Drehbuch und beide Spiegel |

**Grundsätzlich keine Datei unter** `Scripts/Simulation/Combat/`, `Movement/`, `Factions/`,
`Pathfinding/`, `Scripts/AI/`, `AI.Data/`, `Presentation/UI/DebugHud.cs`. Das ist
der Einheitenstrang. Disjunkt gegen [13B](13B_Sprint_Einheitenverhalten.md) —
**ausser an zwei Vertragsflächen:** `Simulation/Definitions/` ist geteilt
(Absprache vor 16.8), und der `WeaponProfiles`-Slot `UnitRole.Unit`, den 16.3
faktisch umwidmet, gehört 13B. **Beides wird vor dem PR angesagt, nicht danach.**

**D-105-Integrationsausnahme für 16.3:** Die geänderte Rollendarstellung hat
beim Zusammenführen zwei Fehler ausserhalb des ursprünglichen Schreibbereichs
offengelegt. Der Projektinhaber darf dafür den kleinsten gebundenen Reparaturdiff
führen: `CombatSystem` schliesst aktive Sites als Angreifer und Ziel aus;
`SkirmishAiSystem` und das kanonische 10.000-Tick-Szenario unterscheiden Sites
vor der Gebäuderolle von fertigen Gebäuden. Gespiegelte Regressionstests und
die Benachrichtigung im PR sind Pflicht; die dauerhafte Stranghoheit ändert sich
nicht.

**Kein neuer `CommandKind`.** Das Register `Simulation/CommandsV1/` bleibt
eingefroren; kein Paket dieses Sprints braucht einen neuen Befehlstyp.

## Pakete

Die Reihenfolge ist nach Kosten sortiert: erst was weder die Startaufstellung
noch `SimDefinitions` anfasst, dann 16.7 (Startaufstellung, fünf Spiegel),
zuletzt 16.8 (`SimDefinitions`, bewegt den Definitions-Hash).

### 16.1 · Der Kreislauf startet von allein (#43)

`GrantFoundingHarvester` setzt dem geschenkten Sammler eine `HarvestFieldId` auf
das nächstgelegene Feld — deterministisch nach Index, nicht nach Fundreihenfolge.

Zwei Fehler im Bestand werden dabei mit korrigiert:

- **Der Latch fehlt.** Geschenkt wird beim **ersten** fertigen Raffineriebau je
  Slot, nicht bei jedem.
  > **Abgeleitet, nicht gespeichert.** `GrantFoundingHarvester` läuft nur, wenn
  > der Slot im Moment des Bauabschlusses keinen lebenden `UnitRole.Harvester`
  > besitzt — Scan über `_entityManager.RawUnits`, Muster wie
  > `FindLowestIndexBuilder`. Ein Zählerfeld im Wirtschaftszustand wäre ein
  > Formatbruch: `WriteState` schreibt je Slot fest `credits`/`provided`/
  > `required`/`faction`, und `TryParseState` lehnt bei `StateVersion != 2` ab.
- **Das stumme Scheitern.** Kommt kein Sammler zustande, wird das protokolliert
  statt verschluckt.

> **Warum das kein Bruch der Befehlsregel ist:** `ConstructionSystem` schreibt
> bereits heute direkt in den Bewegungszustand (`unit.SetTarget(...)` in der
> Verdrängungsroutine). Ein `HarvestFieldId`-Schreiben im selben System ist
> dieselbe Klasse von Zugriff — kein `CommandRecord`, kein neuer Befehlstyp.

### 16.2 · Einheiten verlassen das Gebäude (#46)

`TryFindSpawnCell` sucht ab jetzt am **Footprint** des Produktionsgebäudes statt
an der Rally-Zelle. Die fertige Einheit bekommt danach einen Bewegungsbefehl auf
den Sammelpunkt.

Der Sammelpunkt wird damit wieder ein Ziel. Der Tester hat es so beschrieben:
Einheiten sollen aus der Fabrik herausfahren, notfalls durch das Gebäude-Asset
hindurch, solange es keine Toranimation gibt.

**Nebenwirkung, die dazugehört:** Sobald Einheiten aus dem Gebäude fahren, sieht
man durch die hohlen Gebäude-Assets hindurch (#57). Das ist Art-Arbeit und
ausdrücklich **nicht** in diesem Sprint — der Befund gehört in den GrayboxLog,
nicht in eine Behebung.

### 16.3 · Baustellen schiessen nicht (#44)

Die Baustelle bekommt bei `SpawnBuildingEntity(completed: false)` **`def.Role`
statt `UnitRole.Unit`**. Unbewaffnete Gebäuderollen tragen `AttackDamage = 0`;
damit fällt der Fallback-Schuss weg. Weil eine Verteidigungsplattform selbst
als Gebäuderolle bewaffnet ist, schliesst `CombatSystem` zusätzlich jede aktive
Site als Angreifer und Ziel aus.

Drei Stellen, die das mitzieht:

| Stelle | Was passiert sonst |
|---|---|
| `EconomySystem.RecomputePower` | resolviert Strom rein über `unit.Role` — eine unfertige Baustelle zöge ab sofort vollen Strom. **Muss kompensiert werden** |
| `UnitViewManager` (`IsBuildingRole`, Prefab / Größe / Rotationssperre) | eine Baustelle würde als fertiges Gebäude gerendert. Optik von `ConstructionSiteMarkerView` mitprüfen |
| `SelectionManager.CopyMobileSelection` | fällt in die andere Richtung: die Baustelle verschwindet aus dem Versand mobiler Befehle. Auswählbar ist sie heute schon — `SelectSingle` und `SelectBoxAdditive` prüfen nur `PlayerId`. Die Befehlskarte ist unbetroffen, `TryGetSite` greift vor `IsBuildingRole` |
| `VictorySystem.IsBuilding` | prüft `IsBuildingRole` zuerst und liefert weiterhin `true` — hier ändert sich nichts |

Zusätzlich müssen `SkirmishAiSystem` und das kanonische
`Determinism10000Scenario` Sites vor jeder Rollenauswertung über das
Baustellenregister ausfiltern. Sonst gilt eine unfertige Raffinerie bereits als
Produzent und die KI reicht Folgeaufträge zu früh ein.

`ConstructionSystem.HasFinishedBuilding` ist **nicht** betroffen: es iteriert
`_buildings[]`, das nur `CompleteSite` und `PlaceCompletedBuilding` schreiben.
Bauvoraussetzungen bleiben korrekt.

### 16.4 · Das Lager wird ein Gebäude (#53, C2)

AE-Obergrenze im `EconomySystem`: **genau eine HQ-Kontobasis von 2.000 AE,
sobald mindestens ein fertiges HQ lebt; +2.000 je fertigem Lager**
(D-024/D-096/D-106). Baustellen zählen nicht. Jede neue Einzahlung und
Rückerstattung wird sofort an der aktuellen Grenze gekappt; der Rest verfällt.

Ein bereits vorhandener Bestand oberhalb der Grenze verliert alle 10
Simulationsticks (1 s) **25 % des aktuellen Überhangs**, ganzzahlig abgerundet
und mindestens 1 AE, bis die Grenze erreicht ist. Das gilt für den
3.000-AE-Start, Restore sowie die Grenzsenkung durch Zerstörung oder Verkauf
eines Lagers und den Verlust des letzten HQ. Es gibt keinen zusätzlichen
Einmalverlust. Beim Verkauf wird die Rückerstattung noch gegen die vor dem
Despawn geltende Kapazität gedeckelt; danach sinkt die Grenze.

> **Die Kapazität wird aus dem Gebäudebestand abgeleitet, nicht gespeichert.**
> Ein neues Feld im Wirtschaftszustand bumpt `EconomySystem.StateVersion`, und
> `TryParseState` lehnt danach jede ältere Fassung ab — alle vorhandenen
> Snapshots und Replays wären wertlos, und ob `MatchFingerprint.StateSchemaVersionV1`
> nachziehen muss, wäre eine eigene Inhaberentscheidung. Die abgeleitete Variante
> kostet nichts davon.

Die Zustandsbytes bleiben kompatibel; die **Regelidentität** ändert sich aber.
`RulesHash64` bindet deshalb Revision 1 und die Werte 2.000/2.000/25/10. Alte
Replay-/Snapshot-Dateien bleiben strukturell lesbar, doch eine Replay-Wiedergabe
unter einem anderen Rules-Hash wird vor Tick 1 abgelehnt. So können alte und
neue Peers nicht erst am ersten Zerfallstick desynchronisieren (D-106).

`DepositCapped` bündelt genau vier produktive Gutschriftpfade in diesem
Sprintbereich: Abladen (`EconomySystem`), Streichung (`ProductionSystem`),
Abbruch und Verkauf (`ConstructionSystem`). Nur dieser Helfer ruft produktiv
`PlayerEconomyState.AddCredits` auf.

### 16.5 · Das Radar wird ein Gebäude (#54, C3)

Zwei Wirkungen, beide vom **Gebäude** abgeleitet statt von jeder Einheit:

1. **Die Minimap hängt am Radar.** `MinimapHud` zeichnet nur noch, wenn der
   lokale Slot ein fertiges Radar besitzt. Die Abfrage existiert bereits:
   `ConstructionSystem.HasFinishedBuilding(slot, UnitRole.Radar)`.
2. **Radar-Abdeckung kommt vom Gebäude.** `GetRadarSignatures` leitet ihre
   Reichweite aus dem Radargebäude ab statt aus der `SightRadius` jeder Entität,
   und wird erstmals von der Präsentation konsumiert — heute ruft sie niemand
   ausser Tests.

`FogOfWarSystem.GetTeamView` bleibt **unverändert**. Es ist Vertragsfläche des
Einheitenstrangs (`CombatSystem` liest es für die Zielerlaubnis).

> **Der Verlust ist beabsichtigt und muss erklärt werden.** Eine Minimap
> wegzunehmen, die es immer gab, fühlt sich im ersten Moment als Rückschritt an.
> Der Bauknopf für das Radar sagt deshalb im Klartext, was er freischaltet.

### 16.6 · Low Power wird eine Waffe (C4)

Die feste vierstufige Abschaltreihenfolge, bei der **Radar, Produktion, Bau und
Reparatur** in dieser Ordnung fallen. Heute existiert nur der Tempo-Malus über
`ProductionSpeedMultiplierQ16`, gelesen von `ProductionSystem` und
`ConstructionSystem`.

> **Die Verteidigung fällt hier nicht mit.** Ob ein Turm feuert, entscheidet
> allein `CombatSystem` über `WeaponProfiles.Get(...).IsArmed` — und
> `Simulation/Combat/` kennt heute überhaupt keinen Strombegriff (`grep Power`
> dort: null Treffer). Eine Verteidigungsabschaltung bräuchte einen Stromeingang
> in fremdem Terrain. Sie geht als Befund an 13B, nicht in dieses Paket.

Erst damit wird ein Angriff auf das gegnerische Kraftwerk ein taktischer Zug —
und erst damit wird 16.5 spürbar, weil ein Stromausfall die Minimap mitnimmt.

### 16.7 · Knappheit (C1) — **fasst die Startaufstellung an**, nicht `SimDefinitions`

| Was | Heute | Ziel |
|---|---|---|
| Feldreserve | 2.000.000 AE (≈ 28 h ununterbrochene Ernte eines Sammlers) | Manifestwerte **9.000 / 15.000 AE** |
| Feldanzahl | 2 (je Slot eins) | **5** — 2 Start, 2 Expansion, 1 umkämpftes Zentrum |
| Ernterate | 2 AE/Tick, als Provisorium markiert | gegen die Zielkurve kalibriert |

**Symmetrie ist Pflicht.** Beide Startpositionen müssen gleich weit zu Expansion
und Zentrum liegen — sonst entscheidet die Karte das erste Mensch-gegen-Mensch-Match.

Keiner der drei Zielwerte liegt in `SimDefinitions`: `FieldReserveAE` und die
Feldpositionen stehen in `MatchBootstrap`, `HarvestRateAE` in `EconomySystem`.
Der Definitions-Hash bewegt sich hier **nicht** — das tut nur 16.8.

**Fünf synchrone Stellen** (siehe Regelwerk, „kanonische Startaufstellung"):

1. `Assets/_Project/Scripts/Gameplay/Match/MatchBootstrap.cs` — `SetupSlot`
2. `tools/Nova.SimRunner/Determinism10000Scenario.cs` — `SetupMatch`
3. `tools/Nova.SimRunner.Tests/CanonicalMatchSetupTests.cs`
4. `Assets/Tests/EditMode/Gameplay/CanonicalMatchSetupTests.cs`
5. `Assets/_Project/Scripts/Presentation/Maps/GlutrinneBlockoutView.cs` —
   Feldmarker und Steinstreu-Ausschluss stehen heute als **zwei feste Aufrufe**
   (`LocalFieldCell`, `EnemyFieldCell`) statt als Schleife über die registrierten
   Felder. Ohne diese Stelle stünden nach dem Sprung von 2 auf 5 Felder drei
   Felder ohne Marker und ohne Ausschlusszone.

### 16.8 · Die Bauvoraussetzungs-Kette (C5) — **fasst `SimDefinitions` an**

`SimBuildingDefinition.PrerequisiteRole` ist ein **einzelnes** Feld; das Design
nennt für sechs von neun Rollen Mehrfachvoraussetzungen. Eine Bitmaske über
`UnitRole` reicht.

> **`PrerequisiteRole` geht in `DefinitionsHash64` ein**
> (`hash.WriteUInt8((byte)def.PrerequisiteRole)`). Eine Formatänderung bewegt den
> Definitions-Hash, und der Relay vergleicht ihn serverseitig. Deshalb liegt
> dieses Paket **vor** dem VPS-Rollout, nicht danach.

### 16.9 · Platzierungsregeln und Reparaturkosten (C6)

- **Platzierung:** Bau-Einflussradius 8 Zellen um HQ / Lager / Kraftwerk,
  Mindestabstand zu Aetherium-Feldern, Gebäudeabstand. Heute prüft der Code nur
  „innerhalb der Karte" und „Zelle frei". Die Begehbarkeitsprüfung liest
  `Pathfinding.CostField` — **Vertragsfläche, `IsWalkable` wird benutzt, nicht
  geändert.**
- **Reparatur kostet 30 % des Neupreises.** Zwei Details, die dazugehören:
  - `ProcessRepairOrders` hat **keine Ziel-Deduplikation** — mehrere Bauarbeiter
    am selben Gebäude zahlen im selben Tick mehrfach. Das ist zu lösen, sonst
    ist es der Betatest-Fehler der nächsten Runde.
  - Die Bauphase läuft **nach** `RecomputePower` im selben Tick. Ein Abzug in der
    Reparaturschleife wirkt darum erst im Folgetick auf die Strombilanz. Das ist
    hinnehmbar; die Tickreihenfolge zu drehen wäre `SimulationKernel.cs` und
    damit eine eigene Inhaberentscheidung.

### 16.10 · Ehrliche Rückmeldung am Entscheidungspunkt (#47, #48, #45)

Drei kleine Eingriffe, die zusammengehören, weil sie dasselbe Regelwerk berühren:

- **#47 Der Blockergrund wird hergeleitet, nicht gemeldet.**
  `BuildMenuHud.BlockerReason` kennt heute zwei Ursachen (fehlende Voraussetzung,
  zu wenig Aetherium) und wird um **Energie** und **volles Baustellenkontingent**
  ergänzt. **Nicht** über einen neuen `CommandResultCode` — `CommandsV1/` ist
  eingefroren.
  > **Energie darf nicht in `IsAvailable`.** Sonst wird der Knopf ausgegraut und
  > der Platzierungsmodus ist gar nicht mehr erreichbar. *Grund anzeigen* ist
  > nicht *Knopf sperren*.
- **#48 Energie steht dort, wo gebaut wird.** Zwei Orte, beide vom Issue benannt:
  die Statuszeile über der Baubar zeigt die Strombilanz und beim Überfahren eines
  Knopfes den Strombedarf des Gebäudes; die Befehlskarte zeigt am angeklickten
  Gebäude Bedarf beziehungsweise Erzeugung (`CommandCardPresenter`). Die
  bestehende Anzeige in `DebugHud.DrawStatusBar` bleibt, wo sie ist.
- **#45 „Stoppen" löscht auch den Angriffsbefehl.** `UnitCommandStateView`
  räumt bei `CommandKind.Stop` zusätzlich `AttackTarget` ab. Heute löscht Stop
  Bewegung, Ernte und Reparatur — nur den Angriff nicht.
  > **Was das nicht leistet:** Die Einheit erfasst im nächsten Tick automatisch
  > ein neues Ziel (D-087). Ein echtes „Feuer einstellen" braucht einen
  > Haltezustand in `Simulation/Combat/` und gehört damit dem Einheitenstrang.
  > Das ist als Befund an 13B zu geben, nicht hier zu bauen.

## Bewusst nicht in diesem Sprint

| | Warum |
|---|---|
| Halte-Feuer / „Feuer einstellen" | braucht `Simulation/Combat/` — Einheitenstrang |
| Gebäude-Assets (#57, #58) | Art-Strang, Sprint 19. 16.2 macht #57 sichtbarer, behebt es aber nicht |
| Auswahl, Zielwahl, Formationen (#50, #51, #52) | [Sprint 18](18_Sprint_Befehl_und_Auswahl.md) |
| Reparaturzone an Fabrik und Kaserne (#55) | Balancing, wartet auf eine gespielte Runde mit 16.9 |
| Sanitäter (#56) | nur Doku, keine neue Einheit |
| Feldanatomie, Nachwachsen, Überernte | im ScopeLedger registriert |
| Auto-Zielerfassung ändern | D-087 gehört dem Einheitenstrang |

## Risiken

| Risiko | Umgang |
|---|---|
| **Der Relay lehnt nach 16.8 alle Clients ab** | `PrerequisiteRole` bewegt `DefinitionsHash64`, der Relay vergleicht ihn serverseitig. 16.8 liegt **vor** dem VPS-Rollout; danach kostet dieselbe Änderung einen Serverzugang. 16.7 ist davon **nicht** betroffen |
| **Vier von fünf Spiegeln der Startaufstellung gepflegt** | roter Test, der wie ein Determinismusfehler aussieht — oder drei Aetherium-Felder ohne sichtbaren Marker. Die fünf Stellen stehen in 16.7 |
| **Baseline und Verhalten im selben PR** | wird nicht gemergt. `Determinism10000Scenario.cs` liegt ausserhalb der Guard-Präfixe und darf im selben PR nachgezogen werden — `Determinism10000Tests.cs` nicht |
| **Ein 13B-Merge im selben Fenster** | ein Fenster hat einen Strang (Regelwerk, Merge-Fenster) |
| **Die Minimap-Sperre wird als Rückschritt gelesen** | der Bauknopf erklärt, was das Radar freischaltet; der Befund geht in die nächste Testrunde |
| **`dotnet test` läuft auf der Arbeitsmaschine nicht** | `global.json` pinnt `8.0.318` mit `rollForward: disable`, installiert ist 10.0.302. Der Nachweis läuft über die CI im PR |
| **Reparaturkosten machen Verteidigung unbezahlbar** | 30 % ist ein Startwert, kein Beschluss. Er kommt mit der ersten gespielten Runde auf den Prüfstand |

## Fertig wenn

1. `dotnet test tools/Nova.SimRunner.Tests` ist **in der CI** grün — ohne
   Baseline-Neusetzung im selben PR wie eine Verhaltensänderung.
2. Ein Mensch hat eine Runde gespielt und dabei gesehen:
   - der Sammler fährt nach der ersten Raffinerie von allein los,
   - produzierte Einheiten fahren aus dem Gebäude heraus statt am Sammelpunkt zu
     erscheinen,
   - eine Baustelle schiesst nicht,
   - ein Startfeld geht während der Runde zur Neige,
   - das Konto läuft ohne Lager über,
   - ohne Radar gibt es keine Minimap, mit Radar schon,
   - ein zerstörtes Kraftwerk nimmt Radar und Produktion mit,
   - beim gesperrten Bauknopf steht der **zutreffende** Grund.
3. Der Ablauf steht im [GrayboxLog](../GrayboxLog.md) mit Commit und Endzustand.
4. Die abgeworfenen Pakete stehen mit Begründung im [ScopeLedger](../ScopeLedger.md).

Punkt 2 ist nicht durch Punkt 1 ersetzbar. `Gameplay/` und `Presentation/` sind
in **keinem** CI-Testlauf enthalten; für 16.10 gibt es ausser der gespielten
Runde keinen Nachweis.

## Abwurfliste

Reicht die Zeit nicht, fällt in dieser Reihenfolge: **16.9**, dann **16.8**,
dann **16.6**. Jeder Abwurf mit Begründung in den
[ScopeLedger](../ScopeLedger.md). Skips sind erlaubt, **stille Skips nicht.**

16.1 bis 16.5 fallen nicht — sie sind der Grund für den Sprint.

## Entscheidungen, die dieser Sprint erzeugt

| ID | Inhalt | Wer |
|---|---|---|
| D-096 | Lager erhält eine **abgeleitete** AE-Obergrenze (kein Zustandsfeld); Radar schaltet die Minimap frei und leitet seine Abdeckung vom Gebäude ab | Inhaber (Richtung) / Agent (Ausformung) |
| D-097 | „Stoppen" löscht den Angriffsbefehl; ein Halte-Feuer bleibt beim Einheitenstrang | Inhaber |
| D-106 | AE-Kontobasis gilt einmalig je Slot; vorhandener Überhang zerfällt zustandslos pro Sekunde und die Regelrevision wird im Match-Fingerprint gebunden | Agent unter Inhaberdelegation |

D-096, D-097 und D-106 sind im [DecisionLog](../DecisionLog.md) eingetragen. D-098
(Entwurf) und D-099 stehen dort für [Sprint 17](17_Sprint_Zugangsprotokoll.md),
D-100 bleibt für dessen Paket B vorgemerkt, D-098 gehört zu
[Sprint 14](14_Sprint_Lobby.md). Keine dieser Nummern darf hier verbraucht
werden.

## Changelog-Notiz

Die Wirtschaft trägt sich selbst: Aetherium wird knapp, Lager begrenzt das Konto,
Radar schaltet die Minimap frei, Strommangel schaltet Radar und Verteidigung ab,
Bauvoraussetzungen greifen mehrfach, Platzierung und Reparatur kosten. Dazu die
Betatest-Behebungen: der erste Sammler erntet von allein, Einheiten fahren aus
dem Gebäude, Baustellen schiessen nicht mehr, und der gesperrte Bauknopf nennt
den zutreffenden Grund.

## Versionsrelevanz

`minor` — neue spielbare Fähigkeiten und Verhaltensänderungen, kein Vertragsbruch.
Die Baseline-Neusetzung ist Zweck der Tests, kein Bruch.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.2.0 | 2026-08-10 | D-106 für 16.4 festgeschrieben: einmalige HQ-Kontobasis, periodischer 25-%-Abbau des aktuellen Überhangs und Rules-Hash-Kompatibilitätsgrenze | Codex / Dennis Westermann |
| 1.1.0 | 2026-08-10 | D-105-Integrationsausnahme für 16.3 dokumentiert: aktive Sites sind keine Kampfteilnehmer oder fertigen KI-Produzenten | Codex / Dennis Westermann |
| 1.0.0 | 2026-08-09 | Erstfassung: Strang C aus Sprint 12 und die acht Betatest-Befunde im selben Schreibbereich zu einem Sprint zusammengeführt, am Code geprüft und nach Kosten sortiert | Orchestrator |
