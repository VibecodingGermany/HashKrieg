# Relay-Server – Betrieb und Deploy

**Version:** 0.1.0 | **Status:** Sprint 12 A1–A5 implementiert, A6 (Verdrahtung MatchRunner/MatchBootstrap) offen | **Verantwortungsbereich:** Lead Multiplayer Engineer | **Sprint:** 12

## Zweck

Der Relay-Server (`tools/Nova.RelayServer`, Binary `nova-relay`) ist der
autoritative Input-Relay für deterministisches Lockstep. Er simuliert nicht
selbst — er verteilt und validiert Befehle zwischen den Clients, hält den
Tick-Takt (Offer/TickComplete) und vergleicht die periodisch eingereichten
State-Hashes, um einen Desync zu erkennen. Protokoll und Frame-Typen sind in
[`Assets/_Project/Scripts/Networking/RelayProtocol.cs`](../../Assets/_Project/Scripts/Networking/RelayProtocol.cs)
definiert; der Server-Kern liegt in
[`RelayServerCore.cs`](../../Assets/_Project/Scripts/Networking/RelayServerCore.cs).

## Protokoll v1 (Kurzform)

Längenpräfigierte Frames über TCP, little-endian:

```
[u32 payloadBytes][u8 type][payload]
```

Header ist 5 Byte fest, `payloadBytes` ist auf 8 MiB gedeckelt
(`RelayProtocol.MaxFramePayloadBytes`). Frame-Typen
(`RelayFrameType`, `RelayProtocol.ProtocolVersion = 1`):

| Typ | Wert | Richtung | Bedeutung |
|---|---|---|---|
| `Hello` | 1 | Client → Server | Protokollversion + Match-Token |
| `Offer` | 2 | Server → Client | Slot-Zuweisung + Match-Parameter (Seed, Delay, Server-Definitions-Hash) |
| `Fingerprint` | 3 | Client → Server | serialisierte MatchFingerprint-Bytes |
| `InitialSnapshot` | 4 | Client → Server | kanonischer Initial-Snapshot |
| `Start` | 5 | Server → Client | beide Peers verifiziert — Match startet |
| `Reject` | 6 | Server → Client | Ablehnung mit menschenlesbarem Grund |
| `CommandRecord` | 7 | beide Richtungen | ein kanonischer Command-Record — der einzige Frame-Typ mit Gameplay-Input |
| `TickComplete` | 8 | beide Richtungen | ein Slot hat für einen Tick vollständig eingereicht — TRANSPORT-Frame, nie ein CommandRecord |
| `StateHash` | 9 | Client → Server | periodischer kanonischer State-Hash für den Desync-Vergleich |
| `Desync` | 10 | Server → Client | die Per-Tick-Hashes weichen ab; beide Clients halten an |
| `PeerLost` | 11 | Server → Client | eine Peer-Verbindung ist verloren; das Match endet geordnet |
| `Ping` | 12 | Client → Server | RTT-Probe |
| `Pong` | 13 | Server → Client | RTT-Echo |

## Betrieb

- Systembenutzer: `novarelay` (unprivilegiert, keine Login-Shell)
- Installationspfad: `/opt/hashkrieg-relay`
- Binary: `nova-relay`
- Aufzeichnungen: `/var/lib/hashkrieg-relay/records`

### Konfiguration

`Program.cs` liest ausschließlich Umgebungsvariablen, keine
Kommandozeilenargumente:

| Variable | Pflicht | Default | Bedeutung |
|---|---|---|---|
| `NOVA_MATCH_TOKEN` | ja | — | geteilter Match-Code (hex u64); der Prozess startet ohne diese Variable nicht |
| `NOVA_RELAY_PORT` | nein | `47777` | der einzige offene Port |
| `NOVA_INPUT_DELAY_TICKS` | nein | `3` | Network-Lockstep-Input-Delay (300 ms bei 10 Hz) |
| `NOVA_RECORD_DIR` | nein | leer = Aufzeichnung aus | Verzeichnis für die `*.novarec`-Command-Stream-Dumps pro Match |
| `NOVA_RELAY_SEED` | nein | `0` = pro Match generiert | fixer Match-Seed für reproduzierbare Testläufe |

Diese Variablen kommen produktiv ausschließlich aus
`/etc/hashkrieg-relay.env` (`chmod 600`, gehört `root:novarelay`) — kein
Secret liegt im Repository.

### systemd-Unit

```ini
[Unit]
Description=Project Nova / Hashkrieg Relay Server
After=network.target

[Service]
Type=simple
User=novarelay
EnvironmentFile=/etc/hashkrieg-relay.env
ExecStart=/opt/hashkrieg-relay/nova-relay
Restart=on-failure
RestartSec=5
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ReadWritePaths=/var/lib/hashkrieg-relay

[Install]
WantedBy=multi-user.target
```

### Deploy

1. Artefakt aus dem GitHub-Actions-Lauf ziehen:
   `gh run download <run-id> -n nova-relay-linux-x64 -D ./relay-artifact`
2. Aktuelle Binary sichern:
   `cp /opt/hashkrieg-relay/nova-relay /opt/hashkrieg-relay/nova-relay.prev`
3. Neue Binary einspielen:
   `cp ./relay-artifact/nova-relay /opt/hashkrieg-relay/nova-relay`
4. Neustart: `systemctl restart hashkrieg-relay`

**Rollback:** `cp /opt/hashkrieg-relay/nova-relay.prev /opt/hashkrieg-relay/nova-relay && systemctl restart hashkrieg-relay`

## Sicherheit

- Das Match-Token ist Pflicht (`NOVA_MATCH_TOKEN`) — ohne gültiges Token bei
  `Hello` wird der Client abgelehnt, und der Prozess startet erst gar nicht
  ohne gesetztes Token.
- Kein Secret im Repository — Token und alle sonstige Konfiguration kommen
  ausschließlich aus `/etc/hashkrieg-relay.env`.
- Der Dienst lauscht zunächst nur auf `127.0.0.1` — kein direkter
  Internet-Zugriff auf den Relay-Port, bis der WebSocket/nginx-Transport
  (siehe Offene Punkte) steht.

## Offene Punkte

- Transport soll auf WebSocket/443 hinter nginx umgestellt werden —
  Cloudflare proxyt auf normalen Tarifen keinen rohen TCP-Port.
- A6 (Verdrahtung in `MatchRunner`/`MatchBootstrap`) fehlt noch — es gibt
  daher noch kein spielbares Netzwerkmatch, nur den Server- und
  Protokoll-Baustein.
