# Parallelbetrieb Sprint 13–15 — zwei Stränge, eine Simulation

**Status:** verbindlich | **Gilt für:** [13](13_Sprint_Netzpartie.md), [13B](13B_Sprint_Einheitenverhalten.md), [14](14_Sprint_Lobby.md), [15](15_Sprint_Netzstabilitaet.md) | **Leitsatz:** getrennte Ordner sind billig, getrennte Determinismus-Zustände nicht

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

## Die Grundentscheidung: unser Strang fasst die Simulation nicht an

Sprints 13, 14 und 15 sind so geschnitten, dass sie **keine Datei unter
`Assets/_Project/Scripts/Simulation/` und keine unter `Scripts/AI*` ändern.**
Netzwerk, Matchrahmen, Oberfläche und Relay-Betrieb reichen dafür aus.

Damit gehört der gesamte Simulations-Verhaltensraum für die Dauer von 13–15
dem Einheitenstrang allein. Das ist kein Zugeständnis, sondern der Grund,
warum beide Stränge überhaupt nebeneinander laufen können.

Die Folge für uns: **Strang C aus Sprint 12 (Wirtschaftsdruck) wandert hinter
Sprint 15.** Er bleibt in unserer Hand, aber er ist simulationsverändernd und
würde die Trennung sofort aufheben. Er wird als Sprint 16 geführt.

## Schreibhoheit

Die Tabelle ist **vollständig**: jeder Pfad unter `Assets/_Project/Scripts/` hat
einen Eigentümer. Ein unzugeordneter Pfad ist ein Fehler in diesem Dokument, kein
Freiraum.

| Pfad | Eigentümer | Anmerkung |
|---|---|---|
| `Scripts/AI/`, `Scripts/AI.Data/` | **Einheitenstrang** | `AiPeerCommandTransport.cs` darf die Transport-Verträge nicht ändern (siehe unten). `AI.Data/` enthält heute nur das asmdef — die Datenschicht baut der Einheitenstrang auf |
| `Scripts/Simulation/Movement/` | **Einheitenstrang** | |
| `Scripts/Simulation/Combat/` | **Einheitenstrang** | inkl. `WeaponProfiles`, `DamageMatrix`, `ArmorClass` |
| `Scripts/Simulation/Factions/` | **Einheitenstrang** | Legion-Waffenidentität |
| `Scripts/Simulation/Pathfinding/` | **Einheitenstrang (13–15)** | `MovementSystem` hängt im Konstruktor daran; ohne Flow-Field-Zugriff ist B3 nicht lösbar. **`CostField` ist Vertragsfläche** — siehe unten |
| `Scripts/Networking/` | **Netzstrang** | |
| `Scripts/Gameplay/Match/` | **Netzstrang** | `MatchConfig`, `MatchBootstrap`, `MatchRunner` — inkl. der Systemregistrierung |
| `Scripts/Gameplay/UI/`, `Scripts/Gameplay/Input/` | **Netzstrang** | Verbindungs- und Lobbyoberfläche, Fraktionsauswahl |
| `Scripts/Gameplay/Audio/`, `Gameplay/CombatFeedback/` | **Netzstrang** | Präsentationsnah; wandert an den Art-Strang, sobald der besetzt ist |
| `Scripts/Presentation/` | **Netzstrang** | dito |
| `Scripts/Core/` | **Netzstrang** | Logging und Infrastruktur; Änderungen nur additiv, beide Stränge hängen daran |
| `Scripts/Data/` | **Netzstrang** | Registries und Karten, überwiegend Unity-Assets — mergen schlecht, ein Schreiber |
| `Scripts/Simulation/Vision/` | **Netzstrang** | `FogOfWarSystem`. **Vertragsfläche:** `CombatSystem` konsumiert `GetTeamView` |
| `Scripts/Simulation/Commanders/`, `Victory/` | **Netzstrang** | |
| `Scripts/Simulation/Construction/`, `Economy/`, `Production/` | **Netzstrang (ab Sprint 16)** | in 13–15 fasst sie niemand an |
| `tools/Nova.RelayServer/`, `tools/packaging/` | **Netzstrang** | |
| `Scripts/Simulation/Definitions/` | **geteilt — Absprache nötig** | Vertragsfläche: `WeaponDefinition`/`UnitDefinition` braucht der Einheitenstrang, `BuildingDefinition`/`SimDefinitions` der Wirtschaftsstrang |
| `Scripts/Simulation/SimulationKernel.cs` | **niemand ohne D-ID** | Tick-Reihenfolge, siehe „Neue Systeme" |
| `Scripts/Simulation/Systems/` | **niemand ohne D-ID** | `ISimSystem` ist der Systemvertrag selbst |
| `Scripts/Simulation/CommandsV1/` | **niemand ohne D-ID** | Command- und Payload-Schema |
| `Scripts/Simulation/Replays/`, `Snapshots/`, `State/` | **niemand ohne D-ID** | Speicherformat und Fingerprint — Änderung ist eine Inhaberentscheidung |
| `CHANGELOG.md` | **serialisiert** | ein Eintrag pro PR, Konflikte löst der Mergende |
| `docs/production/hashkrieg/` | **Maintainer** | Planungsstand; Befunde kommen per Mail oder Issue, nicht per PR |

Berührt ein PR fremdes Terrain, wird er nicht gemergt, sondern zurückgegeben.
Das gilt in beide Richtungen.

### Vertragsflächen in fremdem Besitz

Zwei Ordner gehören einem Strang, werden aber vom anderen konsumiert. Dort gilt
zusätzlich: **Verhalten ändern ja, Vertrag ändern nur nach Absprache.**

| Fläche | Eigentümer | Konsument | Was ohne Absprache nicht geht |
|---|---|---|---|
| `Pathfinding.CostField` | Einheitenstrang | `ConstructionSystem` (Platzierungsprüfung, Sprint 16) | Signatur oder Begehbarkeits-Semantik von `IsWalkable` ändern. Flow-Field-Erzeugung und Pathfinding-Interna sind frei |
| `FogOfWarSystem.GetTeamView` | Netzstrang | `CombatSystem` (Zielerlaubnis) | Rückgabeform oder Sichtbarkeitsregel ändern, ohne den Einheitenstrang zu informieren |

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

### Plattformen

`tools/packaging/` enthält heute nur den macOS-Weg. Solange das so ist, kann am
Netznachweis (A8 Stufen 2–4) nur teilnehmen, wer einen Mac hat — der
Einheitenstrang wäre damit von genau der Runde ausgeschlossen, deren Verhalten er
baut.

**Der Linux-Build ist deshalb eine Bringschuld des Netzstrangs** und liegt als
Paket 13.7 in [Sprint 13](13_Sprint_Netzpartie.md). Die .NET-Toolchain für die
SimRunner-Tests richtet sich jeder Strang selbst ein; das ist keine Bringschuld.

Wer den Commit seines Builds prüfen will:

```bash
# macOS
defaults read /Applications/ProjectNova.app/Contents/Info.plist NovaBuildCommit

# Linux (nach 13.7)
cat ProjectNova_Data/NovaBuildCommit.txt
```

## Der externe Beitragende — Zugangsmodell

Der Einheitenstrang wird von jemandem bearbeitet, den wir nicht kennen. Das ist
in Ordnung und ausdrücklich gewollt — es verlangt nur ein sauberes Modell.

| | |
|---|---|
| **Zugang** | **Fork.** Kein Collaborator-Eintrag, kein Push auf dieses Repository, keine Mitgliedschaft in `trusted-coders` |
| **Beitrag** | ausschliesslich Pull Request vom Fork nach `main` |
| **Merge** | nur Maintainer. Die Push-Restriktion auf `main` erzwingt das strukturell, unabhängig von Reviewregeln |
| **Review** | jeder fremde PR wird von einem Maintainer gelesen, bevor er gemergt wird — das ist Tier 2 |
| **CI** | alle Workflows laufen auf `pull_request`, nicht `pull_request_target`. Ein Fork-PR bekommt damit einen schreibgeschützten Token und keine Secrets |

**Das löst einen Tier-Wechsel aus.** [GOVERNANCE.md](../../../GOVERNANCE.md)
nennt als Auslöser für Tier 2 wörtlich den „erster PR von außerhalb des
Maintainer-Kreises". Der Wechsel ist eine Inhaberentscheidung und gehört als
D-ID in den [DecisionLog](../DecisionLog.md), bevor der erste fremde PR
aufschlägt — sonst rutschen wir stillschweigend in ein Tier, dessen Regeln
niemand angeschaltet hat.

Was Tier 2 gegenüber heute konkret ändert:

- fremde PRs brauchen Maintainer-Review (Selbst-Merge bleibt für Maintainer)
- DecisionLog-D-IDs werden Pflicht, nicht nur bei „echten" Entscheidungen
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
| 1.1.0 | 2026-08-08 | Nach Prüfbefund des Einheitenstrangs: Schreibhoheitstabelle auf **vollständig** gezogen (zwölf bis dahin unzugeordnete Pfade ergänzt), `Simulation/Pathfinding/` dem Einheitenstrang zugewiesen, Abschnitt „Vertragsflächen in fremdem Besitz" (`CostField`, `GetTeamView`) und Abschnitt „Neue Systeme" ergänzt, der den Widerspruch zwischen Einordnungsregel und Schreibhoheit an `MatchRunner` auflöst; Linux-Build als Bringschuld des Netzstrangs festgehalten | Producer / Agent (Umsetzung) |
| 1.0.0 | 2026-08-08 | Erstfassung: Schreibhoheit, Baseline-Regel, Merge-Fenster und Zugangsmodell für den Parallelbetrieb 13–15 | Producer / Agent (Umsetzung) |
