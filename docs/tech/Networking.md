# Networking – 1v1-Lockstep über TCP-Command-Relay

**Version:** 0.4.0 | **Status:** D-089-Implementierungsvertrag; manuelle Netzwerkabnahme offen | **Verantwortungsbereich:** Lead Multiplayer Engineer | **Sprint:** 12

## Zweck

Definiert das in Sprint 12 implementierte 1v1-Netzprofil aus **D-089**:
deterministisches Lockstep zweier Client-Simulationen über einen nicht
simulierenden TCP-Command-Relay. Der ältere D-033/D-046-Entwurf für UDP,
Lobby, Reconnect, Observer und serverseitige Ergebnisarbitration bleibt unten
als historisches Vollspiel-Zielbild erhalten, ist aber kein Vertrag des
implementierten Profils. Replikations- und Aufzeichnungsgrenzen stehen in
[Replication.md](Replication.md), der Betrieb in
[RelayServer.md](RelayServer.md).

Geltungsbereich: `LocalLoopback` mit kanonischem Default-Delay 1 und erlaubtem
Bereich 1 bis 60 sowie genau ein Netzprofil für zwei menschliche Slots mit
standardmäßig Delay 3 und demselben erlaubten Bereich. Eine gespielte
Netzwerkpartie ist noch nicht nachgewiesen.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-057 (kanonische Simulation und `LocalLoopback`), D-089 (implementiertes TCP-1v1), D-033/D-046 (teilweise ersetztes historisches Zielbild)
- [../research/Multiplayer_Simulation.md](../research/Multiplayer_Simulation.md) – Modellvergleich, Bandbreitenrechnung, Determinismus-Fallstricke, §6 Umsetzungsoptionen
- [../gamedesign/MultiplayerModes.md](../gamedesign/MultiplayerModes.md) – Lobby-/Teamregeln (§4), Beobachter/Replays (§6), MatchSettings
- [../gamedesign/VictoryConditions.md](../gamedesign/VictoryConditions.md) – Match-Ergebnis-Regeln, technischer Abbruch (§ Konflikt, siehe Offene Punkte)
- [./Replication.md](./Replication.md) – Replikationsumfang, Desync-Detektion, Reconnect, Replays
- [./RelayServer.md](./RelayServer.md) – Prozess-, Environment-, systemd-, Deploy- und Firewallvertrag
- [./GameState.md](./GameState.md) – geplant: Command-Modell, Tick-Loop, Serialisierung (D-033-Regeln 1–5)

## Sprint-12-Implementierungsprofil (D-089)

### Rollen und Datenfluss

- Jeder Client simuliert den vollständigen Gameplay-State lokal über denselben
  deterministischen Kernel. Es gibt keinen laufenden Entity-State-Sync.
- Der Relay nimmt genau zwei TCP-Clients an, bindet jeden an seinen Slot,
  validiert Frames und Command-Records und verteilt den bestätigten Strom. Er
  simuliert nicht und besitzt keine Ergebnisautorität.
- `TickComplete { slot, targetTick, recordCount }` ist ein reiner
  Transport-Frame. Der Client markiert seine lokale Completion selbst und
  sendet sie an den Relay; die Tickausführung wartet nicht auf ein Echo dieses
  eigenen Frames. Die Remote-Completion wird erst nach serverseitiger Prüfung
  von Tickfolge, exakter Anzahl, Dedupe und Caps an den anderen Client
  weitergeleitet. Dessen Barrier öffnet mit lokaler Markierung, vollständig
  eingetroffenen Remote-Records und validierter Remote-Completion. Bei
  aktivierter Aufzeichnung persistiert der Relay den Tick erst nach bestätigter
  Completion beider Slots. Fehlende Vollständigkeit stallt die Simulation,
  statt einen Leertick zu erfinden.
- Alle 50 Ticks melden beide Clients ihren State-Hash. Gleichheit erzeugt einen
  bestätigten Checkpoint; Ungleichheit beendet beide Clients als Desync. Der
  Server re-simuliert nicht.

### Start und Session

Der Relay bietet Slot, aktive Slots, Seed, Input-Delay und Definitionshash an.
Vor `Start` müssen beide Peers denselben vollständigen Fingerprint und einen
byteidentischen Initialsnapshot beweisen; der Beweis bindet insbesondere Seed,
Delay, Definitionshash und Initialzustand. Das Netzprofil verwendet
standardmäßig drei Input-Delay-Ticks, erlaubt 1 bis 60 und ändert den Wert
nicht während einer Session. Der kanonische lokale Defaultwert ist ein Tick;
`MatchConfig`/Loopback erlauben ebenfalls 1 bis 60.

`ICommandTransport` bleibt unverändert. Ein Transport kann zusätzlich
`ICommandSubmissionReadiness` implementieren; die Ingress prüft die
Bereitschaft vor Session-Aktion und Sequenzvergabe. Der Relay-Client erlaubt
Commands nur in `Running`. `MatchConfig`, `MatchBootstrap` und `MatchRunner`
tragen Konfiguration und Barrier bis in die Spielschleife; eine lokale Pause
ist im Relay-Match gesperrt.

### Ende und ausgeschlossener Umfang

Implementierte terminale Pfade sind Desync, Peer-Verlust,
Protokollverletzung, Transportfehler und Barrier-Timeout. Nicht implementiert
sind `MatchComplete`, Reconnect, UDP, Lobby/Matchmaking, Observer, mehr als zwei
Spieler und eine serverseitige Ergebnissimulation. Der rohe TCP-Port wird nicht
durch nginx oder WebSocket ersetzt. Details zu Token, Port und Betrieb stehen
in [RelayServer.md](RelayServer.md).

## 1. Historisches Vollspiel-Zielbild (D-033/D-046, nicht implementiert)

```
┌─────────┐  Commands (zu Tick T+2)   ┌──────────────────┐   Commands (zu Tick T+2)  ┌─────────┐
│ Client A│ ────────────────────────▶ │  Command-Relay   │ ◀──────────────────────── │ Client B│
│ (Sim)   │ ◀──────────────────────── │     -Server      │ ────────────────────────▶ │ (Sim)   │
└─────────┘  Tick-Batches + Hash-Acks └──────────────────┘   Tick-Batches + Hash-Acks └─────────┘
                                            │
                                     ┌──────┴──────┐
                                     │ Match-Record│ (Command-Log → Replay,
                                     │ + Hash-Log  │  Desync-Reports, Observer-Feed)
                                     └─────────────┘
```

**Jeder Client simuliert das komplette Match lokal und deterministisch.** Über das Netz wandern ausschließlich Spieler-Commands und Validierungs-Hashes – kein State-Sync (D-033; Bandbreitenbegründung Research §2.5: <5 kB/s pro Spieler statt 200–300 kB/s).

### 1.1 Historische Server-Rollen (nicht implementiert)

> **Historischer Entwurf:** Die folgende Ergebnisautorität und
> Post-Match-Re-Simulation sind nicht Teil von D-089. Im implementierten Profil
> simuliert der Relay nicht und entscheidet kein Ergebnis.

Der damalige Entwurf sah den Relay-Server **autoritativ über Befehle, Takt und
Ergebnis** (TPD §9, aufgelöst in Research §6/§7):

| Rolle | Beschreibung |
|---|---|
| Befehls-Autorität | Nimmt Commands entgegen, validiert (Slot-Zugehörigkeit, Tick-Fenster, Format, Rate-Limits), verwirft Ungültiges, reiht sie in das kanonische Tick-Batch ein. Clients können keine gegenseitigen Commands fälschen. |
| Taktgeber | Vergibt die globale Tick-Nummerierung und das Verarbeitungsfenster; das Tick-Batch des Servers ist die einzige Wahrheit darüber, welche Commands in welchem Tick ausgeführt werden. |
| Hash-Validierung | Sammelt State-Hashes pro Client und Tick-Intervall, vergleicht, erkennt Desyncs (Details: [./Replication.md](./Replication.md) §2). |
| Match-Ergebnis | Führt das Command-Log als Beweismittel; bestätigt das per Simulation ermittelte Ergebnis und schließt das Match ab (Replay-Persistenz). Bei strittigem Ausgang (abweichende Ergebnis-Hashes, 1v1-Desync/Ergebniskonflikt) entscheidet eine **Post-Match-Re-Simulation des Command-Logs** – SimRunner-basiert, on-demand serverseitig (Trust-Anchor, D-046; Details [./Replication.md](./Replication.md) §2.2). |
| Observer-/Replay-Verteilung | Verteilt den verzögerten Command-Strom an Beobachter ([../gamedesign/MultiplayerModes.md](../gamedesign/MultiplayerModes.md) §6). |

Der Server simuliert **nicht** selbst (kein Gameplay-State) – das hält Hosting-Kosten minimal (D-007: MP ist Feature, nicht Fundament) und vermeidet eine zweite Sim-Implementierung als Desync-Quelle. **Ausnahme (Trust-Anchor, D-046):** Bei strittigem Match-Ausgang läuft serverseitig on-demand eine einmalige Re-Simulation des vollständig vorliegenden Command-Logs über eine `Nova.SimRunner`-Instanz (D-036). Sie ist nicht echtzeitkritisch, kostet Sekunden CPU pro strittigem Match (nicht pro Match) und lässt das Kostenargument „kein Gameplay-State" im Hot-Path intakt.

### 1.2 MVP-Ausprägung: lokaler Server

Im MS-1-Singleplayer führt `LocalLoopbackTransport` Records an denselben
Ingress zurück. Der kanonische fingerprinted lokale Defaultwert für
`InputDelayTicks` ist 1; der zulässige Konfigurationsbereich ist 1 bis 60.
Schema, Sortierung, Dedupe und Fehlermodell stehen verbindlich in
[Commands.md](Commands.md).

## 2. Historischer Eigenbau-UDP-Entwurf (durch D-089 ersetzt)

### 2.1 Transport: Reliable-Ordered-Layer über UDP

Historische Begründung: Das Nachrichtenvolumen ist winzig und homogen
(Command-Batches, Hashes), die Anforderungen speziell (Tick-Takt, keine
Head-of-Line-Blockade bei Positions-unabhängigen Acks), und der Sim-Kern ist
ohnehin Pflicht (Research §6, „Favorit“). Dieser ersetzte Entwurf schloss TCP
wegen angenommener Head-of-Line-Blockade aus und legte auf UDP einen schmalen
Zuverlässigkeits-Layer. D-089 traf für das implementierte 1v1 die gegenteilige
Wahl und verwendet TCP. Der historische UDP-Layer hätte umfasst:

- **Reliable-Ordered nur für Command- und Kontroll-Kanäle** (Sequenznummer + Ack-Bitmap, Retransmit nach 2×RTT oder spätestens 150 ms).
- **Unreliable für Hash-/Heartbeat-Kanal** (Hashes sind redundant, alle N Ticks neu – Verlust egal).
- Paketgröße ≤ 1.200 Byte (unter typischer MTU, kein IP-Fragmenting).
- Verbindung: session-tokenbasiert, keep-alive 1 Hz, Timeout 10 s ohne Paket = "verloren-Verdacht", 30 s = Disconnect-Ereignis (§5).

### 2.2 Protokoll-Design (Skizze)

```csharp
namespace Nova.Net
{
    // Wire-Header: klein, versioniert, little-endian
    public enum PacketType : byte
    {
        Hello, HelloAck,            // Handshake
        CommandSubmit,              // Client → Server: eigene Commands für Tick T+InputDelay
        TickBatch,                  // Server → Clients: kanonische Commands eines Ticks
        StateHash,                  // Client → Server: Hash nach Tick T (unreliable)
        HashMismatch,               // Server → Clients: Desync-Bescheid
        Heartbeat,                  // beidseitig, 1 Hz
        SnapshotRequest, SnapshotChunk, SnapshotDone, // Reconnect (siehe Replication.md §3)
        MatchEnd,                   // Server → Clients: finales Ergebnis + Replay-Ref
        ObserverJoin, ObserverFeed  // verzögerter Strom für Beobachter
    }

    public readonly record struct PacketHeader(
        byte ProtocolVersion,       // hart an Spielversion gebunden (Replay-Kompatibilität)
        PacketType Type,
        uint SessionId,             // serverseitig beim Handshake vergeben
        ushort Sequence,            // pro Richtung, für Reliable-Layer
        ushort AckBits);            // Bitmap der letzten 16 empfangenen Sequenzen

    public readonly record struct TickBatch(
        uint Tick,                  // absolute Sim-Tick-Nummer (10 Hz, D-033)
        byte PlayerMask,            // welche Slots in diesem Batch Commands haben
        byte[] CommandPayload);     // serialisierte Nova.Simulation-Commands, leer = Leertick
}
```

### 2.3 Tick-Vorausplanung (Input-Delay)

- **Sim-Tick: 10 Hz** (D-033). Commands werden **2 Ticks vorausgeplant** (`InputDelay = 2`): Ein Command, den der Spieler während Tick T eingibt, wird im Tick-Batch **T+2** ausgeführt → 200 ms Befehls-Delay, RTS-üblich und akzeptiert (Research §2.4).
- Clients senden ihre Commands für T+2 laufend; der Server schließt das Fenster für T+2 strikt, wenn **seine** Uhr T erreicht, und broadcastet das Batch – fehlende Commands gelten als **Leertick**, verspätet eintreffende Commands werden verworfen. **Es gibt kein globales Warten** (eine einheitliche Stall-Semantik, Review F-02): Pro Slot zählt ein **Stall-Zähler** die durch Verspätung akkumulierten Leerticks; bei mehr als **5 Ticks Gesamt-Stall** (`MaxStallTicks = 5`) gilt der Slot als getrennt und die Disconnect-Logik startet (§5, D-038).
- **Adaptiver Input-Delay:** Der Server darf `InputDelay` mid-match auf 3–6 Ticks erhöhen, wenn ein Teilnehmer dauerhaft >100 ms RTT hat (einheitlich für alle). Jede Änderung wird als serverseitiger **`SetInputDelay`-Meta-Record im kanonischen Command-Log** aufgezeichnet und ab dem darin benannten Tick wirksam – Determinismus und Replay-Fähigkeit bleiben gewahrt, weil der Delay-Verlauf Teil des Stroms ist ([./Replication.md](./Replication.md) §5, Review F-11).

### 2.4 CPU-Lag- vs. Netz-Lag-Erkennung

- Jeder Client reportet im 1-Hz-`Heartbeat` seine **Tick-Ausführungszeit** (Median und P95 der Sim-Ausführung der letzten Sekunde).
- Der Server unterscheidet daraus **Netz-Lag** (verspätete Commands bei normaler Ausführungszeit → Stall-Zähler, §2.3) von **CPU-Lag** (hohe Ausführungszeit bei pünktlichem Versand: der Client rechnet zu langsam und bremst das Match für alle).
- **Konsequenz bei CPU-Lag: keine KI-Übernahme** – der Spieler ist verbunden und sendet. Stattdessen erhält der betroffene Spieler einen **Qualitätshinweis** (Hardware/Last) und die Lobby eine Kennzeichnung; anhaltender CPU-Lag ist ein akzeptiertes Restrisiko des Lockstep-Modells und fließt in die Beta-Telemetry ein.

## 3. Historischer Fallback: Reduzierter MP-Scope (D-051)

Im historischen D-033-Zielbild war der Eigenbau-UDP-Relay der Primärpfad; für
das implementierte D-089-Profil gilt stattdessen TCP. Ein Photon-Quantum-
Fallback existiert **nicht mehr** (D-051): Ein Quantum-Wechsel wäre faktisch
ein Rewrite (Gameplay-Code in Quantum-DSL/ECS, Verlust von `Nova.Simulation`,
SimRunner (D-036) und sämtlichen Fixtures), kein Fallback – und die früheren
Trigger-Kriterien („vertretbarer Aufwand", „1,5×-Budget", „ein Sprint") waren
nicht messbar (Review F-05).

Scheitert der Eigenbau-Pfad, ist der Fallback ein **reduzierter MP-Scope auf derselben Architektur**:

| Parameter | Voller Scope | Reduzierter Scope (Fallback) |
|---|---|---|
| Spieler pro Match | bis 6 | max. 4 |
| Globales Einheiten-Deckel | 600 (D-048) | 300 |
| Regionen | EU + weitere nach Beta-Telemetry | EU-only |

Ein vollständiger Strategiewechsel (Quantum o. ä.) ist davon strikt getrennt: Er wäre eine **neue Grundsatzentscheidung** nach totalem Scheitern des Eigenbaus – kein „Fallback" – und bedürfte eines eigenen DecisionLog-Eintrags mit Alternativprüfung. Die 5 Architekturregeln aus D-033 bleiben in jedem Fall gültig.

## 4. Historischer Lobby- und Match-Flow (nicht implementiert)

Fachliche Regeln: [../gamedesign/MultiplayerModes.md](../gamedesign/MultiplayerModes.md) §4 (Host-Lobby, max. 6 Slots, Slot-Optionen, Ready-Check, Text-Chat; kein Voice-Chat, D-029). Technischer Ablauf:

1. **Lobby (serverseitig, nicht im Lockstep):** Der Relay-Server hostet die Lobby-Rooms; der "Host" der Design-Dokumente ist nur eine **UI-Rolle** (erster Beitretender) mit Rechten für Slot-/Map-/MatchSettings-Wahl – keine technische Autorität. `MatchSettings`-SO wird bei Matchstart in den serialisierten Initialzustand überführt.
2. **Matchstart:** Server friert Slots + Settings ein, erzeugt `MatchConfig` (Map-Seed, Slot→Fraktion/Team, Doktrinenwahl vgl. [../gamedesign/CommanderSystem.md](../gamedesign/CommanderSystem.md), InputDelay), broadcastet sie als Tick-0-Batch. Alle Clients initialisieren `Nova.Simulation` identisch daraus.
3. **Lauf:** §2.3. KI-Slots laufen **auf dem Server? Nein** – KI ist command-erzeugend wie ein Spieler; im Relay-Modell läuft jede KI-Instanz auf genau einem fest zugewiesenen Client/Prozess (bei reinem PvP-Mix: beim Slot-Inhaber bzw. round-robin verteilt). Ihre Commands durchlaufen dieselbe Validierung. (Zuordnungsregel: Offene Punkte; die **Übernahme**-KI nach Disconnect ist dagegen entschieden – deterministisches Sim-Ereignis auf allen Clients, D-046, §5.)
4. **Ende:** Ergebnis aus der lokalen Simulation; Clients melden Ergebnis-Hash, Server bestätigt und persistiert Match-Record (Replay, [./Replication.md](./Replication.md) §5).

## 5. Historische Disconnect-Regel (nicht implementiert)

**Entscheidung: KI-Übernahme nach Grace-Period – kein Pause-Vote, keine sofortige Auto-Niederlage.**

- **Grace-Period 60 s:** Ab Disconnect-Ereignis (§2.1) sendet der Slot Leerticks; das Match läuft **unpausiert** weiter. Der getrennte Spieler kann per Reconnect (Snapshot + Fast-Forward, [./Replication.md](./Replication.md) §3) wieder einsteigen. UI zeigt den Ausfall allen Spielern an.
- **Stall-Auslöser (Review F-02):** Überschreitet ein Slot die Stall-Schwelle aus §2.3 (> 5 Ticks Gesamt-Stall), gilt das als Disconnect-Ereignis – dieselbe Grace-Period-/Übernahme-Logik startet.
- **Nach 60 s ohne Reconnect:** Eine KI-Instanz (Difficulty = Normal/Mittel-Profil, fest, kein Vote) übernimmt den Slot vollständig und spielt zu Ende. Die Übernahme ist ein **deterministisches Sim-Ereignis (D-046)**: Nach Ablauf der Grace-Period schalten **alle Clients tick-synchron** auf dieselbe Ersatz-KI um – ihre Ausgaben sind Sim-Ergebnis, kein Netz-Command-Strom; es gibt **keinen Server-Prozess und keinen Single Point of Failure**. Der ursprüngliche Spieler kann danach **nicht** mehr zurückkehren (Snapshot-Auslieferung an "Fremde" wäre ein Maphack-Vektor).
- **Wertung:** Wird die übernommene Fraktion vernichtet, gilt der getrennte Spieler als besiegt; gewinnt sie, wird das Match für ihn als "unvollständig" ohne Sieg gewertet (kein Farming über Disconnect).

**Begründung (Alternativabwägung):**
- *Pause-Vote verworfen:* missbrauchbar (Taktik-Pausen, Griefing-Verweigerung), friert 20–35-min-Matches ein, benachteiligt disziplinierte Spieler; im Lockstep trivial als DoS nutzbar.
- *Auto-Niederlage verworfen:* bestraft flüchtige Netzprobleme mit Matchverlust und ruiniert 2v2-/Koop-Matches für den Mitspieler (Team-Asymmetrie); Wiedereinstieg wäre unmöglich, obwohl die Reconnect-Technik ohnehin gebaut wird.
- *KI-Übernahme* hält das Match für alle verbleibenden Spieler spielbar, nutzt die ohnehin existierende KI-Schicht (Command-only, erzeugt ausschließlich Commands wie ein menschlicher Slot) und das Reconnect-System, und kostet keinen zusätzlichen Netzwerk-Pfad.

**Konflikt (aufgelöst):** [../gamedesign/VictoryConditions.md](../gamedesign/VictoryConditions.md) definierte ursprünglich "Verbindungsverlust > 120 s = Niederlage". Diese Regel ist durch die finale Festlegung **ersetzt** (KI-Übernahme statt Auto-Niederlage, D-038); die Angleichung von VictoryConditions.md und MultiplayerModes.md §3.2 ist erfolgt (beide verweisen auf dieses Dokument als führend).

## 6. Historische Host-Migration-Bewertung

**Im Relay-Modell entfällt klassische Host-Migration.** Der autoritative Knoten ist der dedizierte Relay-Server, kein Spieler-Client; ein Client-Ausfall (auch des Lobby-"Hosts") berührt weder Takt noch Command-Kanonizität. Verbleibende Fälle:

- **Lobby-Host-Wechsel (pre-match):** UI-Rolle, wird vom Server einfach dem nächsten Client zugewiesen – keine Migration von Spielzustand nötig. Der in MultiplayerModes.md §4 offene Punkt "techn. Machbarkeit im Lockstep-Relay" ist damit beantwortet: trivial, da kein Host-State existiert.
- **Server-Ausfall mid-match:** nicht abgedeckt (Match bricht ab, Command-Log bis zum Ausfall liegt serverseitig vor → technisch wäre Fortsetzung via Snapshot + neuer Session denkbar, wird **nicht** verplant; akzeptiertes Restrisiko, Dokumentation in Offene Punkte).

## 7. Historischer UDP-NAT-/Regionen-/Ping-Entwurf

- **NAT/Traversal (historisch):** Der ersetzte Entwurf nahm ausgehende
  UDP-Verbindungen und einen optionalen Port `443/UDP` an. D-089 verwendet
  ausgehendes TCP zum rohen Relay-Port; weder `443/UDP` noch WebSocket/nginx
  sind implementiert.
- **Regionen (Beta):** Start mit **EU-Zentral** (Primärzielgruppe H1, D-007); Region wird der Session als Matchmaking-/Lobby-Parameter mitgegeben. US-East als zweite Region erst nach Beta-Telemetry.
- **Ping-Anforderungen:** Weiches Limit **RTT ≤ 150 ms** für gutes Spielgefühl (2-Tick-Fenster = 200 ms); darüber greift der adaptive Input-Delay (§2.3) bis max. 6 Ticks (600 ms), darüber gilt der Spieler als stall-gefährdet (Stall-Schwelle §2.3). Lobby zeigt RTT pro Slot an.

## 8. Historische Vollspiel-Maphack-Bewertung

Gemäß D-033 **akzeptiert bis Ranked-Re-Evaluierung**: Jeder Client besitzt den vollständigen Simulationszustand (Lockstep-Struktur), Fog-of-War ist rein clientseitig – clientseitige FoW-Aufhebung ist nicht verhinderbar (SC2-Präzedenz). Gegenmaßnahmen heute: Manipulations-Cheats erzeugen Desyncs und sind per Hash-Validierung + Replay nachweisbar (§1.1, [./Replication.md](./Replication.md) §2); im Konfliktfall liefert die Post-Match-Re-Simulation den Schuldspruch (D-046). Für Ranked (unter Vorbehalt, D-018) bleibt serverseitiges Sichtgrid-Filtering als Re-Evaluationspunkt offen (Research §5).

- **Ghosting durch besiegte Spieler (Review F-09):** Ausgeschiedene Spieler behalten zunächst nur ihre bisherige Team-Sicht; die Beobachter-Vollsicht erhalten sie **erst nach 120 s Delay** – sie stehen damit informationsseitig externen Beobachtern gleich ([./Replication.md](./Replication.md) §4). Live-Vollsicht für Besiegte wäre ein Ghosting-Vektor über externe Voice-Tools (D-029).

## Offene Punkte

- A8 Stufen 2–4 stehen aus: zwei Unity-Fenster über Loopback, zwei Rechner im
  LAN und eine vollständige Partie über den VPS.
- Das aktuelle TCP-Profil hat keine dokumentierte TLS-Schicht. Vor einer
  breiteren Internetfreigabe sind Transportverschlüsselung, Tokenwechsel und
  ein enger Firewall-/Betriebsrahmen neu zu entscheiden.
- `MatchComplete`, Reconnect, Lobby/Matchmaking, Observer, mehr als zwei Slots,
  UDP/RUDP und serverseitige Ergebnisarbitration benötigen jeweils einen neuen
  Vertrag. Die historischen §§1–8 aktivieren diese Funktionen nicht.
- Ein Relay-Ausfall beendet die laufende Session; Fortsetzung oder Migration
  ist nicht implementiert.

## Nächste Schritte

1. A8 Stufe 2 lokal in zwei Unity-Fenstern durchführen und erst danach LAN und
   VPS prüfen.
2. Den in [RelayServer.md](RelayServer.md) beschriebenen Linux-/systemd-Pfad
   erst nach ausdrücklicher Deploy-Freigabe auf dem VPS ausführen.
3. Erweiterungen des engen D-089-Profils separat entscheiden; die historischen
   Vollspielabschnitte sind Anforderungen, keine Implementierungszusage.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Multiplayer Engineer |
| 0.1.1 | 2026-07-21 | Konflikt-Verweise auf VictoryConditions.md/MultiplayerModes.md als aufgelöst markiert (D-038-Angleichung erfolgt) | Lead Technical Director |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings) | Lead Multiplayer Engineer |
| 0.3.0 | 2026-07-24 | Online-Architektur als Post-MVP abgegrenzt und MS-1 auf `LocalLoopback` gemäß D-056/D-057/D-061 festgelegt | Lead Multiplayer Engineer |
| 0.3.1 | 2026-07-24 | Veralteten Phase-0-/8-Hz-Pfad entfernt und Online-Arbeit strikt hinter G5 verschoben | Lead Multiplayer Engineer |
| 0.4.0 | 2026-08-07 | D-089-1v1-Profil (TCP, fester Barrier, Startproof, 50-Tick-Hashes und enge Scopegrenze) als implementierten Vertrag vorangestellt; widersprechenden UDP-/Lobby-/Reconnect-Entwurf als historisch markiert | Lead Multiplayer Engineer / Technical Writer |
