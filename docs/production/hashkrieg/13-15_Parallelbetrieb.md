# Parallelbetrieb Sprint 13–18 — zwei Stränge, eine Simulation

**Version:** 1.3.0 | **Status:** verbindlich ab Merge des Sprint-13.0-PR | **Verantwortungsbereich:** Maintainers und Strangverantwortliche | **Sprint:** 13–18 | **Gilt für:** [13](13_Sprint_Netzpartie.md), [13B](13B_Sprint_Einheitenverhalten.md), [14](14_Sprint_Lobby.md), [15](15_Sprint_Netzstabilitaet.md), [16](16_Sprint_Wirtschaft.md), [18](18_Sprint_Befehl_und_Auswahl.md) | **Leitsatz:** getrennte Ordner sind billig, getrennte Determinismus-Zustände nicht

## Warum es dieses Dokument gibt

Ab Sprint 13 arbeiten zwei Parteien gleichzeitig am selben Spiel:

- **Der Netzstrang** (Maintainer) bringt zwei Menschen über den eigenen Server
  zusammen — Sprints 13, 14, 15.
- **Der Einheitenstrang** (externer Beitragender) macht das Verhalten von
  Einheiten und Fraktionen — Sprint 13B, fortlaufend.

Beide Stränge sind in ihren Dateien überschneidungsfrei. Sie sind es **nicht**
in ihrer Wirkung: [12_Sprint_Zu_Zweit.md](12_Sprint_Zu_Zweit.md) hält fest,
dass simulationsverändernde Arbeit und Netznachweis nie gleichzeitig laufen
dürfen, weil jede Verhaltensänderung den Match-Fingerprint bewegt. Genau diese
Sperre trennt ungleiche Builds (A4). Sie trennt damit auch zwei Spieler, deren
Client aus unterschiedlichen Commits stammt.

Dieses Dokument löst den Widerspruch, statt ihn auszusitzen.

> **Zum Dateinamen:** Er bleibt `13-15_Parallelbetrieb.md`, obwohl das Dokument
> seit D-092 die Sprints 13–18 abdeckt. Ein Umbenennen bräche **24 eingehende
> Links in zwölf Dateien**. Der Titel ist massgeblich, nicht der Dateiname.

## Die Grundentscheidung: die Trennung läuft über Dateien, nicht über Verhalten

> **Geändert mit [D-092](../DecisionLog.md) (2026-08-09).** Bis dahin galt die
> schärfere Fassung: Sprints 13–15 fassen **gar keine** Datei unter
> `Scripts/Simulation/` an, und der gesamte Simulations-Verhaltensraum gehört
> für ihre Dauer dem Einheitenstrang allein. Diese Fassung ist überholt, weil
> der erste Betatest acht Fehler genau in `Construction/`, `Economy/`,
> `Production/` und dem zugehörigen HUD gemeldet hat — Ordner, die kein Strang bearbeiten durfte, solange
> die Regel galt. Der Wortlaut bleibt hier stehen, damit nachvollziehbar ist,
> wovon abgewichen wurde.

Ab Sprint 16 arbeiten **beide** Stränge in der Simulation. Die Trennung, die den
Parallelbetrieb trägt, ist ab jetzt ausschliesslich die Schreibhoheitstabelle
unten — sie ist vollständig, und jeder Pfad hat genau einen Eigentümer.

Was das kostet und was es nicht kostet:

| | |
|---|---|
| **Dateikonflikte** | unverändert ausgeschlossen — die Tabelle ist disjunkt |
| **Determinismus-Baselines** | ab jetzt können **beide** Stränge sie bewegen. Die Regel „Verhalten und Baseline nie im selben PR" wird damit zur wichtigsten Regel dieses Dokuments, nicht zur zweitwichtigsten |
| **Verteilte Testbuilds** | altern schneller, weil zwei Quellen den Fingerprint bewegen. Das Merge-Fenster unten ist die Antwort |
| **Der Definitions-Hash** | `SimDefinitions` geht in `DefinitionsHash64` ein, und der Relay vergleicht ihn **serverseitig** (`RelayServerCore`). Jede Zahlenänderung dort verlangt einen Relay-Redeploy und gleiche Builds auf beiden Seiten — siehe „Definitions-Hash" unten |

**Sprint 16** ist damit der Strang C aus Sprint 12 (Knappheit, Lager, Radar,
Low Power, Bauvoraussetzungen, Platzierung) plus die Betatest-Befunde im selben
Schreibbereich. Er läuft **parallel** zu 13B, nicht dahinter.

## Schreibhoheit

Die Tabelle ist **vollständig**: jeder Pfad unter `Assets/_Project/Scripts/` hat
einen Eigentümer. Ein unzugeordneter Pfad ist ein Fehler in diesem Dokument, kein
Freiraum.

| Pfad | Eigentümer | Anmerkung |
|---|---|---|
| `Scripts/AI/`, `Scripts/AI.Data/` | **Einheitenstrang** | `AiPeerCommandTransport.cs` darf die Transport-Verträge nicht ändern (siehe unten). `AI.Data/` enthält heute nur das asmdef — die Datenschicht baut der Einheitenstrang auf |
| `tools/Nova.AiLab/`, `tools/Nova.AiLab.Tests/` | **Einheitenstrang** | Messwerkzeug für KI-Läufe. Kein Spielcode, wird nicht ausgeliefert; erzeugte Läufe und Reports gehören nicht ins Repository, sondern in die PR-Beschreibung |
| `Scripts/Presentation/UI/DebugHud.cs` | **Einheitenstrang** | ausdrückliche Ausnahme aus `Presentation/`: ohne eine Anzeige im laufenden Spiel ist KI-Verhalten nicht beobachtbar, und beobachtet werden muss es (Nachweisregel) |
| `Scripts/Simulation/Movement/` | **Einheitenstrang** | |
| `Scripts/Simulation/Combat/` | **Einheitenstrang** | inkl. `WeaponProfiles`, `DamageMatrix`, `ArmorClass` |
| `Scripts/Simulation/Factions/` | **Einheitenstrang** | Legion-Waffenidentität |
| `Scripts/Simulation/Pathfinding/` | **Einheitenstrang (13–18)** | `MovementSystem` hängt im Konstruktor daran; ohne Flow-Field-Zugriff ist B3 nicht lösbar. **`CostField` ist Vertragsfläche** — siehe unten |
| `Scripts/Networking/` | **Netzstrang** | |
| `Scripts/Gameplay/Match/` | **Netzstrang** | `MatchConfig`, `MatchBootstrap`, `MatchRunner` — inkl. der Systemregistrierung |
| `Scripts/Gameplay/UI/`, `Scripts/Gameplay/Input/` | **Netzstrang** | Verbindungs- und Lobbyoberfläche, Fraktionsauswahl |
| `Scripts/Gameplay/Audio/`, `Gameplay/CombatFeedback/` | **Netzstrang** | Präsentationsnah; wandert an den Art-Strang, sobald der besetzt ist |
| `Scripts/Presentation/` | **Netzstrang** | dito; einzige Ausnahme ist `UI/DebugHud.cs`, siehe oben |
| `Scripts/Core/` | **Netzstrang** | Logging und Infrastruktur; Änderungen nur additiv, beide Stränge hängen daran |
| `Scripts/Data/` | **Netzstrang** | Registries und Karten, überwiegend Unity-Assets — mergen schlecht, ein Schreiber |
| `Scripts/Simulation/Vision/` | **Netzstrang** | `FogOfWarSystem`. **Vertragsfläche:** `CombatSystem` konsumiert `GetTeamView` |
| `Scripts/Simulation/Commanders/`, `Victory/` | **Netzstrang** | |
| `Scripts/Simulation/Construction/`, `Economy/`, `Production/` | **Netzstrang — ab Sprint 16 aktiv** | seit D-092 in Arbeit, nicht mehr gesperrt. **Vertragsfläche:** die Platzierungsprüfung (`ValidatePlacement`) liest `Pathfinding.CostField` |
| `tools/Nova.RelayServer/`, `tools/packaging/` | **Netzstrang** | |
| `tools/lobby/` (neu, D-098) | **Netzstrang** | SQL, Policies und Edge Functions der Lobby als Quelltext; ausgerollt wird im Supabase-Projekt ausserhalb des Repos |
| `Scripts/Simulation/Definitions/` | **geteilt — Absprache nötig** | Vertragsfläche: `WeaponDefinition`/`UnitDefinition` braucht der Einheitenstrang, `BuildingDefinition`/`SimDefinitions` der Wirtschaftsstrang |
| `Scripts/Simulation/SimulationKernel.cs` | **niemand ohne D-ID** | Tick-Reihenfolge, siehe „Neue Systeme" |
| `Scripts/Simulation/Systems/` | **niemand ohne D-ID** | `ISimSystem` ist der Systemvertrag selbst |
| `Scripts/Simulation/CommandsV1/` | **niemand ohne D-ID** | Command- und Payload-Schema |
| `Scripts/Simulation/Replays/`, `Snapshots/` | **niemand ohne D-ID** | Speicherformat und Fingerprint — Änderung ist eine Inhaberentscheidung |
| `Scripts/Simulation/State/` — **Layout und Serialisierung** | **niemand ohne D-ID** | Feldbestand, Feldreihenfolge, `StateVersion`, Blockformat. Das ist der Teil, der Snapshots und Replays unlesbar macht |
| `Scripts/Simulation/State/` — **Befehlsanwendung** (`UnitCommandStateView`) | **Netzstrang** | mit D-092 aus dem Frost gelöst: *was* ein bestehender `CommandKind` in den Zustand schreibt, ist Verhalten, nicht Format. **Kein neuer `CommandKind`** — das Register bleibt eingefroren |
| `CHANGELOG.md` | **serialisiert** | ein Eintrag pro PR, Konflikte löst der Mergende |
| `docs/production/hashkrieg/` | **Maintainer** | Planungsstand; Befunde kommen per Mail oder Issue, nicht per PR |

Berührt ein PR fremdes Terrain, wird er nicht gemergt, sondern zurückgegeben.
Das gilt in beide Richtungen.

### Die Trennlinie in `UnitCommandStateView`

Sie verläuft nicht zwischen Befehlsarten, sondern zwischen **Zielsetzung** und
**Ausführung**:

| | Wer | Beispiel |
|---|---|---|
| Was ein Befehl in den Zustand **schreibt** | **Netzstrang** | `Stop` löscht `AttackTarget`; `Move` verteilt Zielzellen; `Harvest` setzt `HarvestFieldId` |
| Wie eine Einheit daraufhin **fährt und schiesst** | **Einheitenstrang** | `MovementSystem` fährt zur Zielzelle, `CombatSystem` erfasst und feuert |

Präzedenz ist [D-088](../DecisionLog.md): die Formationsverteilung in `ApplyMove`
hat das Maintainer-Team gebaut, während `MovementSystem` beim Einheitenstrang lag.
D-092 schreibt diese bereits gelebte Linie nur auf.

### Vertragsflächen in fremdem Besitz

Vier Flächen gehören einem Strang, werden aber vom anderen konsumiert. Dort gilt
zusätzlich: **Verhalten ändern ja, Vertrag ändern nur nach Absprache.**

| Fläche | Eigentümer | Konsument | Was ohne Absprache nicht geht |
|---|---|---|---|
| `Pathfinding.CostField` | Einheitenstrang | `ConstructionSystem` (Platzierungsprüfung, Sprint 16) | Signatur oder Begehbarkeits-Semantik von `IsWalkable` ändern. Flow-Field-Erzeugung und Pathfinding-Interna sind frei |
| `FogOfWarSystem.GetTeamView` | Netzstrang | `CombatSystem` (Zielerlaubnis) | Rückgabeform oder Sichtbarkeitsregel ändern, ohne den Einheitenstrang zu informieren |
| `WeaponProfiles`-Slot `UnitRole.Unit` | Einheitenstrang | `ConstructionSystem` (Baustellen tragen heute diese Rolle) | Sprint 16 löst die Kopplung auf, indem die Baustelle `def.Role` statt `UnitRole.Unit` bekommt. Bis dahin gilt: der Fallback-Schaden von 15 ist **keine** Baustellenregel, sondern ein Nebeneffekt. Wer den Slot umwidmet, sagt es an |
| `UnitState.AttackTarget` | Einheitenstrang (`CombatSystem`) | `UnitCommandStateView` (`Stop` löscht es, Sprint 16) | das Feld **löschen** darf der Netzstrang. Eine Regel, *wann automatisch neu erfasst wird* (D-087, Auto-Zielerfassung), gehört dem Einheitenstrang. Ein „Feuer einstellen" ist deshalb kein Netzstrang-Paket |

## Neue Systeme — wer die Tick-Reihenfolge setzt

Die Tick-Reihenfolge ist die Registrierungsreihenfolge in
`Gameplay/Match/MatchRunner.cs`. Diese Datei gehört dem Netzstrang. Die Regel
„neue Systeme werden eingeordnet, nicht angehängt" wäre für den Einheitenstrang
sonst nicht erfüllbar — er käme an die Registrierung gar nicht heran.

Auflösung, in dieser Reihenfolge:

1. **Bevorzugt:** Neues Verhalten geht in ein bereits registriertes System des
   eigenen Strangs. `SkirmishAiSystem` ist zwischen `Combat` und `Victory`
   registriert und deckt den Reaktionsraum von B4 ab. Dann wird `MatchRunner`
   nicht angefasst.
2. **Wenn ein eigenes System wirklich nötig ist:** Der PR des Einheitenstrangs
   bringt das System mit, **ohne** die Registrierung. Er nennt im Text die
   gewünschte Position und die Begründung. Ein Maintainer setzt die
   Registrierungszeile in einem eigenen, minimalen PR nach.

Damit bleibt die Tick-Reihenfolge eine Inhaberentscheidung, ohne den
Einheitenstrang zu blockieren. Das Einordnen ist der Punkt, nicht der Besitz.

## Was der Einheitenstrang nicht anfassen darf

Drei Verträge halten den Netzstrang zusammen. Sie liegen teils in Dateien, die
dem Einheitenstrang gehören — deshalb stehen sie hier ausdrücklich:

1. **`ICommandTransport` und `ICommandSubmissionReadiness`.** Der Relay-Client
   hängt daran. `AiPeerCommandTransport` darf sie benutzen, nicht ändern.
2. **Der Match-Fingerprint und die Schema-Versionen** in
   `Simulation/Replays/MatchFingerprint.cs`. Wer das Zustandslayout ändert,
   bumpt die Schemaversion — und das ist eine Inhaberentscheidung mit D-ID.
3. **Die Tick-Reihenfolge der Systeme.** Determinismus hängt nicht nur daran,
   *was* ein System rechnet, sondern *wann*. Neue Systeme werden eingeordnet,
   nicht angehängt.

## Die Determinismus-Baselines — die wichtigste Regel

Diese Dateien enthalten festgeschriebene Hashes und Goldbytes:

- `tools/Nova.SimRunner.Tests/SnapshotGoldenBytesTests.cs`
- `tools/Nova.SimRunner.Tests/CommandGoldenBytesTests.cs`
- `tools/Nova.SimRunner.Tests/SimRandomGoldenTests.cs`
- `tools/Nova.SimRunner.Tests/Determinism10000Tests.cs`

Jede Verhaltensänderung in der Simulation lässt sie rot werden. Das ist ihr
Zweck.

> **Ein PR, der Simulationsverhalten ändert **und** im selben Zug eine Baseline
> neu setzt, wird nicht gemergt.**

Der Grund ist nicht Misstrauen, sondern Mechanik: Wer beides zusammen ändert,
bekommt eine grüne CI für eine unbemerkte Verhaltensänderung. Der Test hört
dann auf, ein Test zu sein. Baselines werden in einem **eigenen PR** neu
gesetzt, mit dem alten und dem neuen Wert im Text und einer Begründung, warum
die Änderung gewollt ist.

Das gilt für Beitragende **und** für Maintainer. Wir sind gegen diesen Fehler
nicht immun, nur schneller darin.

## Merge-Fenster und Rebuild-Kadenz

Der Fingerprint sperrt Matches zwischen ungleichen Builds. Verteilte Testbuilds
altern damit bei jedem simulationsverändernden Merge.

| Regel | |
|---|---|
| **Merge-Fenster** | Simulationsändernde PRs werden gesammelt und in Fenstern gemergt, nicht einzeln durchgereicht |
| **Kein Fenster während eines Netznachweises** | Läuft gerade A8 Stufe 2–4 oder eine Abnahmerunde, ist das Fenster zu |
| **Nach jedem Fenster** | Build für **jede** Plattform, an der jemand testet, neuer Build an alle Testenden, alter Build ist ungültig |
| **Der Netzstrang testet gegen einen festen Stand** | Abnahmeläufe nennen den Commit, gegen den sie liefen — sonst ist das Ergebnis nicht zuordenbar |
| **Ein Fenster hat einen Strang** | seit D-092 bewegen beide Stränge Baselines. In einem Fenster mergen wir die PRs **eines** Strangs, prüfen die Baselines, und öffnen dann das nächste. Zwei Stränge in einem Fenster machen einen roten Test nicht zuordenbar |

### Der Definitions-Hash — die teuerste Zahl im Projekt

`SimDefinitions.ComputeDefinitionsHash64()` fasst Gebäude- und Einheitenwerte
zusammen. Der Relay berechnet ihn **im Serverprozess** und lehnt jeden Peer mit
abweichendem Hash ab (`RelayServerCore`). Kein Test pinnt ihn auf ein Literal —
die Bremse ist rein betrieblich:

> **Jede geänderte Zahl in `SimDefinitions` verlangt einen Relay-Redeploy und
> gleiche Builds auf beiden Seiten.** Solange der Relay nur lokal läuft, kostet
> das nichts. Nach dem VPS-Rollout kostet es einen Serverzugang.

Daraus folgt die Reihenfolge: **alles, was `SimDefinitions` anfasst, passiert vor
dem VPS-Rollout, nicht danach.** Das betrifft in Sprint 16 die Feldwerte (C1) und
die Bauvoraussetzungs-Bitmaske (C5).

### Die kanonische Startaufstellung wird an fünf Stellen gepflegt

Wer die Startaufstellung ändert, ändert sie **synchron** in:

1. `Assets/_Project/Scripts/Gameplay/Match/MatchBootstrap.cs` — `SetupSlot`
2. `tools/Nova.SimRunner/Determinism10000Scenario.cs` — `SetupMatch`
3. `tools/Nova.SimRunner.Tests/CanonicalMatchSetupTests.cs`
4. `Assets/Tests/EditMode/Gameplay/CanonicalMatchSetupTests.cs` (handgespiegelt)
5. `Assets/_Project/Scripts/Presentation/Maps/GlutrinneBlockoutView.cs` —
   Feldmarker und Steinstreu-Ausschluss stehen dort als **zwei feste Aufrufe**
   (`LocalFieldCell`, `EnemyFieldCell`) statt als Schleife über die registrierten
   Felder

Die Spiegelung von 3 und 4 ist im Test selbst vermerkt („Any edit to the reference
must be applied to BOTH copies"). Das ist die stillste Falle im ganzen
Parallelbetrieb: vier von fünf Stellen zu pflegen ergibt entweder einen roten
Test, der wie ein Determinismusfehler aussieht und keiner ist — oder Felder ohne
sichtbaren Marker.

### Plattformen

Beide Wege stehen: `tools/packaging/build-mac.sh` und `build-linux.sh` (Paket
13.7, seit `e15f5e6`). Beide brennen denselben `NovaBuildCommit` ein.

```bash
# macOS
defaults read /Applications/ProjectNova.app/Contents/Info.plist NovaBuildCommit

# Linux
cat ProjectNova_Data/NovaBuildCommit.txt
```

Zwei Einschränkungen, die bleiben: beide Skripte laufen **nur auf einem Mac**
(Unity-Hub-Pfad fest verdrahtet, Linux ist eine Cross-Kompilierung und braucht
das Hub-Modul `LinuxStandaloneSupport`), und **kein C#-Code liest den Stempel** —
das Spiel kennt seinen eigenen Build nicht. Solange das so ist, kann keine Lobby
ungleiche Builds im Klartext erklären; der Leser ist Vorarbeit von
[Sprint 14](14_Sprint_Lobby.md).

Die .NET-Toolchain für die SimRunner-Tests richtet sich jeder Strang selbst ein;
das ist keine Bringschuld. `global.json` pinnt `8.0.318` mit
`rollForward: disable` — wer ein neueres SDK hat, baut nicht und muss den
Nachweis über die CI im PR führen.

## Der externe Beitragende — Zugangsmodell

Der Einheitenstrang wird von jemandem bearbeitet, den wir nicht kennen. Das ist
in Ordnung und ausdrücklich gewollt — es verlangt nur ein sauberes Modell.

| | |
|---|---|
| **Zugang** | **Fork.** Kein Collaborator-Eintrag, kein Push auf dieses Repository, keine Mitgliedschaft in `trusted-coders` |
| **Beitrag** | ausschliesslich Pull Request vom Fork nach `main` |
| **Merge** | nur `@cubetribe` (Dennis Westermann) und `@travelhawk` (Michael Falk). Die Push-Restriktion auf `main` erzwingt das strukturell |
| **Review** | jeder PR braucht eine `APPROVED`-Review des jeweils anderen Maintainers auf dem aktuellen Head-Commit; bei Fremd-PRs prüft `external-contributor-review` zusätzlich CLA und explizite Maintainer-Freigabe und wird nach seinem ersten erfolgreichen Folge-PR-Lauf als Required Check geschaltet |
| **CI** | Code ausführende Workflows laufen auf `pull_request`. Die beiden reinen Metadatenprüfungen laufen aus dem geschützten Zielbranch auf `pull_request_target`, erhalten nur Leserechte und checken niemals PR-Code aus |

**Der Tier-Wechsel ist entschieden.** D-091 aktiviert Tier 2 mit dem Merge des
Freigabe-PR, also vor dem ersten fremden PR. Die noch ausstehende Negativkontrolle
des Baseline-Wächters ist keine Lizenz, einen PR vorher zu mergen.

Was Tier 2 gegenüber heute konkret ändert:

- jeder PR braucht eine Maintainer-Peer-Review; Fremd-PRs zusätzlich CLA und
  den gezielten `external-contributor-review`-Check
- echte Architektur-, Design- und Prozessentscheidungen erhalten eine D-ID; ab
  Tier 2 dokumentiert jede neue D-ID mindestens drei bewertete Alternativen
- Verträge und öffentliche Doku brauchen Pflichtaufbau und Versionsbump
- der `integrity`-Job läuft auf jedem PR statt nur bei `quality/**`

## Was diese Trennung nicht leistet

Sie verhindert keine inhaltlichen Konflikte. Wenn der Einheitenstrang das
Gefecht deutlich schneller macht, ändert das, wie sich eine Netzpartie mit drei
Ticks Eingabeverzögerung anfühlt — ohne dass eine gemeinsame Datei berührt
wurde. Solche Befunde gehören in den [GrayboxLog](../GrayboxLog.md), nicht in
einen Merge-Konflikt.

Sie ersetzt auch keine gespielte Runde. Governance-Tier 1 wie 2 verlangen, dass
ein Mensch die Sache im laufenden Spiel gesehen hat. Zwei grüne Stränge, die nie
zusammen gespielt wurden, sind zwei Behauptungen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.3.0 | 2026-08-09 | **D-092:** Trennung von „Verhaltensraum" auf „Dateihoheit" umgestellt — Sprint 16 läuft parallel zu 13B statt dahinter. `Simulation/State/` in Layout (weiter eingefroren) und Befehlsanwendung (Eigentümer des jeweiligen Befehls) getrennt. Zwei Vertragsflächen ergänzt (`WeaponProfiles`-Slot `UnitRole.Unit`, `UnitState.AttackTarget`). Merge-Fenster auf einen Strang je Fenster verschärft. Abschnitte „Definitions-Hash" und „kanonische Startaufstellung an vier Stellen" ergänzt. Plattform-Abschnitt berichtigt: der Linux-Build existiert seit `e15f5e6`, die offene Bringschuld ist stattdessen ein `NovaBuildCommit`-Leser im Spiel | Orchestrator |
| 1.2.2 | 2026-08-09 | Zwei Pfade nachgetragen, die der Einheitenstrang in der Praxis braucht und die die Tabelle nicht kannte: `tools/Nova.AiLab/` samt Tests (Messwerkzeug, kein Spielcode) und `Presentation/UI/DebugHud.cs` als ausdrückliche Ausnahme aus `Presentation/`. Beide Lücken lagen im Dokument, nicht im Verhalten des Beitragenden | Producer / Agent (Umsetzung) |
| 1.1.0 | 2026-08-08 | Nach Prüfbefund des Einheitenstrangs: Schreibhoheitstabelle auf **vollständig** gezogen (zwölf bis dahin unzugeordnete Pfade ergänzt), `Simulation/Pathfinding/` dem Einheitenstrang zugewiesen, Abschnitt „Vertragsflächen in fremdem Besitz" (`CostField`, `GetTeamView`) und Abschnitt „Neue Systeme" ergänzt, der den Widerspruch zwischen Einordnungsregel und Schreibhoheit an `MatchRunner` auflöst; Linux-Build als Bringschuld des Netzstrangs festgehalten | Producer / Agent (Umsetzung) |
| 1.2.0 | 2026-08-08 | D-091: konkrete Merge-Accounts, Maintainer-Peer-Review, CLA-/Review-Prüfung und vorbereiteter Tier-2-Rollout ergänzt | Producer / Agent (Umsetzung) |
| 1.2.1 | 2026-08-08 | Metadata-only Checks auf vertrauenswürdigen Zielbranch-Kontext gehärtet und D-ID-Pflicht auf echte Entscheidungen vereinheitlicht | Producer / Agent (Umsetzung) |
| 1.0.0 | 2026-08-08 | Erstfassung: Schreibhoheit, Baseline-Regel, Merge-Fenster und Zugangsmodell für den Parallelbetrieb 13–15 | Producer / Agent (Umsetzung) |
