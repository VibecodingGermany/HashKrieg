# Networking – Zielarchitektur Lockstep über autoritativem Command-Relay

**Version:** 0.3.1 | **Status:** Post-MVP-Zielbild | **Verantwortungsbereich:** Lead Multiplayer Engineer | **Sprint:** 4

## Zweck

Definiert die Netzwerk-Zielarchitektur von Project Nova gemäß **D-033**: deterministisches Lockstep über einem autoritativen Command-Relay-Server (Eigenbau-UDP; Fallback = reduzierter MP-Scope, D-051). Festgelegt werden Server-Rollen inkl. Trust-Anchor (Post-Match-Re-Simulation, D-046), Protokoll-Design, Lobby-/Match-Flow, Disconnect-Regel (final), Host-Migration-Bewertung, NAT/Traversal, Regions-/Ping-Anforderungen und die Maphack-Lage. Was konkret über die Leitung wandert (Commands, Hashes, Snapshots), spezifiziert [./Replication.md](./Replication.md); das Simulationsmodell selbst liegt in [./GameState.md](./GameState.md) (geplant, D-033/D-035).

Geltungsbereich: **MS-1 = `LocalLoopbackTransport` ohne Netzwerk** über die
kanonische Command-Pipeline; das nachfolgende Relay-Modell ist ein
unverbindliches Post-MVP-Zielbild.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-056 (MS-1-Umfang), D-057 (kanonische Simulation und `LocalLoopback`), D-060 (Unity-Pin), D-061 (Abnahme), D-018/D-025 (spätere Modus-Phasen)
- [../research/Multiplayer_Simulation.md](../research/Multiplayer_Simulation.md) – Modellvergleich, Bandbreitenrechnung, Determinismus-Fallstricke, §6 Umsetzungsoptionen
- [../gamedesign/MultiplayerModes.md](../gamedesign/MultiplayerModes.md) – Lobby-/Teamregeln (§4), Beobachter/Replays (§6), MatchSettings
- [../gamedesign/VictoryConditions.md](../gamedesign/VictoryConditions.md) – Match-Ergebnis-Regeln, technischer Abbruch (§ Konflikt, siehe Offene Punkte)
- [./Replication.md](./Replication.md) – Replikationsumfang, Desync-Detektion, Reconnect, Replays
- [./GameState.md](./GameState.md) – geplant: Command-Modell, Tick-Loop, Serialisierung (D-033-Regeln 1–5)

## MS-1-Override (D-056/D-057/D-061)

Die nachfolgende Relay-, Lobby-, NAT-, Regionen- und Online-Architektur ist
vollständig Post-MVP. MS-1 verwendet ausschließlich den in
[Replication.md](Replication.md) definierten `LocalLoopback` über dieselbe
Command-Pipeline; es gibt keinen Netzwerktransport und keine Relay-Abnahme in
G0–G5. Der kanonische State, Hash, Replay und Save/Load werden dennoch so
implementiert, dass ein späterer Transport sie konsumieren kann.

## 1. Zielarchitektur (ab Beta)

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

### 1.1 Server-Rollen (Autorität)

Der Relay-Server ist **autoritativ über Befehle, Takt und Ergebnis** (TPD §9, aufgelöst in Research §6/§7):

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
Ingress zurück. Der fingerprinted lokale `InputDelayTicks`-Wert ist 1; Schema,
Sortierung, Dedupe und Fehlermodell stehen verbindlich in
[Commands.md](Commands.md).

## 2. Eigenbau-UDP-Relay (Primärpfad)

### 2.1 Transport: Reliable-Ordered-Layer über UDP

Begründung Eigenbau: Das Nachrichtenvolumen ist winzig und homogen (Command-Batches, Hashes), die Anforderungen speziell (Tick-Takt, keine Head-of-Line-Blockade bei Positions-unabhängigen Acks), und der Sim-Kern ist ohnehin Pflicht (Research §6, "Favorit"). TCP ist ausgeschlossen (Head-of-Line-Blocking würde Tick-Batches stauen). Auf UDP wird ein schmaler Zuverlässigkeits-Layer gelegt:

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

## 3. Fallback: Reduzierter MP-Scope (D-051)

Primärpfad ist und bleibt der Eigenbau-UDP-Relay (D-033). Ein Photon-Quantum-Fallback existiert **nicht mehr** (D-051): Ein Quantum-Wechsel wäre faktisch ein Rewrite (Gameplay-Code in Quantum-DSL/ECS, Verlust von `Nova.Simulation`, SimRunner (D-036) und sämtlichen Fixtures), kein Fallback – und die früheren Trigger-Kriterien („vertretbarer Aufwand", „1,5×-Budget", „ein Sprint") waren nicht messbar (Review F-05).

Scheitert der Eigenbau-Pfad, ist der Fallback ein **reduzierter MP-Scope auf derselben Architektur**:

| Parameter | Voller Scope | Reduzierter Scope (Fallback) |
|---|---|---|
| Spieler pro Match | bis 6 | max. 4 |
| Globales Einheiten-Deckel | 600 (D-048) | 300 |
| Regionen | EU + weitere nach Beta-Telemetry | EU-only |

Ein vollständiger Strategiewechsel (Quantum o. ä.) ist davon strikt getrennt: Er wäre eine **neue Grundsatzentscheidung** nach totalem Scheitern des Eigenbaus – kein „Fallback" – und bedürfte eines eigenen DecisionLog-Eintrags mit Alternativprüfung. Die 5 Architekturregeln aus D-033 bleiben in jedem Fall gültig.

## 4. Lobby- und Match-Flow

Fachliche Regeln: [../gamedesign/MultiplayerModes.md](../gamedesign/MultiplayerModes.md) §4 (Host-Lobby, max. 6 Slots, Slot-Optionen, Ready-Check, Text-Chat; kein Voice-Chat, D-029). Technischer Ablauf:

1. **Lobby (serverseitig, nicht im Lockstep):** Der Relay-Server hostet die Lobby-Rooms; der "Host" der Design-Dokumente ist nur eine **UI-Rolle** (erster Beitretender) mit Rechten für Slot-/Map-/MatchSettings-Wahl – keine technische Autorität. `MatchSettings`-SO wird bei Matchstart in den serialisierten Initialzustand überführt.
2. **Matchstart:** Server friert Slots + Settings ein, erzeugt `MatchConfig` (Map-Seed, Slot→Fraktion/Team, Doktrinenwahl vgl. [../gamedesign/CommanderSystem.md](../gamedesign/CommanderSystem.md), InputDelay), broadcastet sie als Tick-0-Batch. Alle Clients initialisieren `Nova.Simulation` identisch daraus.
3. **Lauf:** §2.3. KI-Slots laufen **auf dem Server? Nein** – KI ist command-erzeugend wie ein Spieler; im Relay-Modell läuft jede KI-Instanz auf genau einem fest zugewiesenen Client/Prozess (bei reinem PvP-Mix: beim Slot-Inhaber bzw. round-robin verteilt). Ihre Commands durchlaufen dieselbe Validierung. (Zuordnungsregel: Offene Punkte; die **Übernahme**-KI nach Disconnect ist dagegen entschieden – deterministisches Sim-Ereignis auf allen Clients, D-046, §5.)
4. **Ende:** Ergebnis aus der lokalen Simulation; Clients melden Ergebnis-Hash, Server bestätigt und persistiert Match-Record (Replay, [./Replication.md](./Replication.md) §5).

## 5. Disconnect-Regel (FINAL)

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

## 6. Host-Migration-Bewertung

**Im Relay-Modell entfällt klassische Host-Migration.** Der autoritative Knoten ist der dedizierte Relay-Server, kein Spieler-Client; ein Client-Ausfall (auch des Lobby-"Hosts") berührt weder Takt noch Command-Kanonizität. Verbleibende Fälle:

- **Lobby-Host-Wechsel (pre-match):** UI-Rolle, wird vom Server einfach dem nächsten Client zugewiesen – keine Migration von Spielzustand nötig. Der in MultiplayerModes.md §4 offene Punkt "techn. Machbarkeit im Lockstep-Relay" ist damit beantwortet: trivial, da kein Host-State existiert.
- **Server-Ausfall mid-match:** nicht abgedeckt (Match bricht ab, Command-Log bis zum Ausfall liegt serverseitig vor → technisch wäre Fortsetzung via Snapshot + neuer Session denkbar, wird **nicht** verplant; akzeptiertes Restrisiko, Dokumentation in Offene Punkte).

## 7. NAT/Traversal, Regionen, Ping

- **NAT/Traversal:** Im Relay-Modell bauen alle Clients nur **ausgehende** UDP-Verbindungen zum Server auf – kein P2P, kein STUN/TURN, keine Port-Forwarding-Problematik. Symmetrische NATs sind unproblematisch. Firewall-Fallback: Relay zusätzlich auf Port 443/UDP erreichbar machen (Beta-Ops-Entscheidung).
- **Regionen (Beta):** Start mit **EU-Zentral** (Primärzielgruppe H1, D-007); Region wird der Session als Matchmaking-/Lobby-Parameter mitgegeben. US-East als zweite Region erst nach Beta-Telemetry.
- **Ping-Anforderungen:** Weiches Limit **RTT ≤ 150 ms** für gutes Spielgefühl (2-Tick-Fenster = 200 ms); darüber greift der adaptive Input-Delay (§2.3) bis max. 6 Ticks (600 ms), darüber gilt der Spieler als stall-gefährdet (Stall-Schwelle §2.3). Lobby zeigt RTT pro Slot an.

## 8. Maphack-Lage

Gemäß D-033 **akzeptiert bis Ranked-Re-Evaluierung**: Jeder Client besitzt den vollständigen Simulationszustand (Lockstep-Struktur), Fog-of-War ist rein clientseitig – clientseitige FoW-Aufhebung ist nicht verhinderbar (SC2-Präzedenz). Gegenmaßnahmen heute: Manipulations-Cheats erzeugen Desyncs und sind per Hash-Validierung + Replay nachweisbar (§1.1, [./Replication.md](./Replication.md) §2); im Konfliktfall liefert die Post-Match-Re-Simulation den Schuldspruch (D-046). Für Ranked (unter Vorbehalt, D-018) bleibt serverseitiges Sichtgrid-Filtering als Re-Evaluationspunkt offen (Research §5).

- **Ghosting durch besiegte Spieler (Review F-09):** Ausgeschiedene Spieler behalten zunächst nur ihre bisherige Team-Sicht; die Beobachter-Vollsicht erhalten sie **erst nach 120 s Delay** – sie stehen damit informationsseitig externen Beobachtern gleich ([./Replication.md](./Replication.md) §4). Live-Vollsicht für Besiegte wäre ein Ghosting-Vektor über externe Voice-Tools (D-029).

## Offene Punkte

- **Konflikt VictoryConditions.md (aufgelöst, D-038):** Die frühere Regel "Verbindungsverlust > 120 s = Niederlage" widersprach der finalen KI-Übernahme-Regel (§5) – Angleichung erfolgt (VictoryConditions.md 0.3.1, MultiplayerModes.md 0.3.2; dieses Dokument führend).
- **KI-Slot-Ausführungsort regulärer KI-Slots** (§4.3): feste Zuordnung pro Slot (Command-erzeugend) vs. deterministische KI auf allen Clients wie bei der Übernahme-KI. Die **Übernahme-KI nach Disconnect ist entschieden** (deterministisches Sim-Ereignis, kein Server-Prozess, kein SPOF – D-046, §5). Rest zu entscheiden mit Beta-Infrastrukturplanung.
- **Server-Ausfall mid-match:** Fortsetzungs-Szenario bewusst nicht verplant; Restrisiko für lange Matches (20–35 min) dokumentieren.
- **Relay-Backend-Technologie:** Implementierungssprache/-Hosting des Relay-Servers (z. B. .NET auf Basis von `Nova.Simulation`-nahen Serializern) ist Sprint-6-/Beta-Thema; Research nennt Backend-Dienst explizit orthogonal.
- **Post-MVP-Netzprofil:** MS-1 bleibt unveränderlich bei 10 Hz und
  `InputDelayTicks = 1`. Ein späteres Onlineprofil darf davon nur mit neuer
  D-ID und eigener Replay-/Kompatibilitätsbewertung abweichen; es verändert
  keinen Vertrag in G0–G5.
- **Verschlüsselung/Integrität des UDP-Stroms** (DTLS vs. eigenes HMAC-Tagging): vor Beta-Extern-Tests festzulegen.

## Nächste Schritte

1. ~~Angleichung von [../gamedesign/VictoryConditions.md](../gamedesign/VictoryConditions.md) (technischer Abbruch) und [../gamedesign/MultiplayerModes.md](../gamedesign/MultiplayerModes.md) §3.2~~ – **erledigt (D-038, 2026-07-21)**; die Host-Migration-Frage aus MultiplayerModes.md §4 ist oben (§6) beantwortet.
2. Erst nach bestandenem G5 eine eigene Online-Architekturentscheidung mit
   Relay-Hosting, Regionen, Sicherheitsprofil und abweichendem Input-Delay
   vorbereiten.
3. Bis dahin das Zielbild nicht implementieren und keine G0–G5-Gates daran
   koppeln.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Multiplayer Engineer |
| 0.1.1 | 2026-07-21 | Konflikt-Verweise auf VictoryConditions.md/MultiplayerModes.md als aufgelöst markiert (D-038-Angleichung erfolgt) | Lead Technical Director |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings) | Lead Multiplayer Engineer |
| 0.3.0 | 2026-07-24 | Online-Architektur als Post-MVP abgegrenzt und MS-1 auf `LocalLoopback` gemäß D-056/D-057/D-061 festgelegt | Lead Multiplayer Engineer |
| 0.3.1 | 2026-07-24 | Veralteten Phase-0-/8-Hz-Pfad entfernt und Online-Arbeit strikt hinter G5 verschoben | Lead Multiplayer Engineer |
