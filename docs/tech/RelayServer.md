# Relay-Server – Betrieb, Deploy und Rollback

**Version:** 1.1.0 | **Status:** Sprint 12 A1–A7 implementiert; A8 Stufe 1 nachgewiesen, Stufen 2–4 offen | **Verantwortungsbereich:** Lead Multiplayer Engineer / Betrieb | **Sprint:** 12

## Zweck

Der Relay-Server (`tools/Nova.RelayServer`, Prozess `nova-relay`) verbindet
genau zwei Clients für ein deterministisches Lockstep-Match. Er simuliert
nicht. Er prüft und verteilt Command-Records, schließt Ticks über den
`TickComplete`-Barrier, vergleicht State-Hashes und kann den bestätigten
Transportstrom als `NOVAREC2` aufzeichnen.

Die Architekturentscheidung steht in
[D-089](../production/DecisionLog.md), das Protokoll in
[`RelayProtocol.cs`](../../Assets/_Project/Scripts/Networking/RelayProtocol.cs)
und der Serverkern in
[`RelayServerCore.cs`](../../Assets/_Project/Scripts/Networking/RelayServerCore.cs).
Dieses Dokument ist das Runbook für den mit
[`relay-publish.yml`](../../.github/workflows/relay-publish.yml) und
[`deploy.sh`](../../tools/Nova.RelayServer/deploy/deploy.sh) gelieferten
Betriebspfad.

## Sicherheits- und Scopegrenze

- Der aktuelle Transport ist **rohes TCP**, zuverlässig und geordnet. Es gibt
  keinen implementierten WebSocket-, nginx-, TLS- oder UDP-Pfad.
- Das Match-Token wird erst im `Hello` als anwendungsseitiger Berechtigungswert
  geprüft; es ist weder Transportverschlüsselung noch Schutz vor
  Socket-/Slot-Erschöpfung. Den TCP-Port nur bewusst und möglichst auf die
  beiden Quelladressen begrenzt öffnen.
- Der Relay simuliert nicht und bestätigt kein Match-Ergebnis. Es gibt in
  dieser Stufe kein `MatchComplete`, Reconnect, Matchmaking, Lobby, Observer
  oder Host-Migration.
- `TickComplete` ist ausschließlich ein Transport-/Barrier-Frame. Er erreicht
  die Command-Ingress nicht und ist weder Command, Replay-Ergebnis noch
  State-Hash-Ereignis.
- Ein Relay-Prozess bedient nacheinander 1v1-Sessions mit exakt zwei Slots. Das
  ist kein allgemeiner Mehrmatch-Host.
- Secrets liegen ausschließlich in der manuell provisionierten Datei
  `/etc/hashkrieg-relay.env`. Unit, Beispielkonfiguration, Artefakt und
  Deploymentcode enthalten kein echtes Token.

## Protokoll in Kurzform

Frames sind little-endian und längenpräfigiert:

```text
[u32 payloadBytes][u8 type][payload]
```

Der feste Header hat fünf Byte; der Payload ist auf 8 MiB begrenzt.

| Frame | Richtung | Bedeutung |
|---|---|---|
| `Hello` | Client → Server | Protokollversion und Match-Token |
| `Offer` | Server → Client | Slot, aktive Slots, Seed, Input-Delay und Definitionshash |
| `Fingerprint` | Client → Server | kanonischer Match-Fingerprint |
| `InitialSnapshot` | Client → Server | kanonischer Initialzustand |
| `Start` | Server → Client | beide Startbeweise stimmen; Match darf laufen |
| `Reject` | Server → Client | Handshake-/Protokollablehnung |
| `CommandRecord` | Client → Relay → anderer Client | ein kanonischer Gameplay-Command |
| `TickComplete` | Client → Relay → anderer Client | Slot, Zieltick und exakte Record-Anzahl für den Barrier; kein Echo an den Absender |
| `StateHash` | Client → Server | State-Hash am 50-Tick-Checkpoint |
| `Desync` | Server → Client | die beiden Checkpoint-Hashes weichen ab |
| `PeerLost` | Server → Client | Peer verloren; die Session endet |
| `Ping` / `Pong` | Client → Server / Server → Client | RTT-Probe und Echo |

Vor `Start` bindet der Beweis beide Peers an denselben Seed, Input-Delay,
Definitionshash, vollständigen Fingerprint und byteidentischen
Initialsnapshot. Das Netzprofil verwendet standardmäßig drei Ticks Delay; der
erlaubte Bereich ist 1 bis 60. Der kanonische lokale Defaultwert ist ein Tick;
`MatchConfig`/Loopback erlauben ebenfalls 1 bis 60.

Der Absender markiert seine lokale Completion unmittelbar selbst. Der Relay
prüft den eingehenden `TickComplete` gegen seine exakt akzeptierte Record-Anzahl
und leitet ihn nur an den anderen Client weiter. Bei aktivierter Aufzeichnung
persistiert er den Tick erst in `NOVAREC2`, wenn er beide Slot-Completions
bestätigt hat. Dieser Persistenz-Barrier ist nicht mit einem Roundtrip-Gate für
die eigene Client-Completion zu verwechseln.

## Match-Tokens: statisch und Lobby-gemintet

Der Relay akzeptiert seit Sprint 14.5 ([D-093](../production/DecisionLog.md))
zwei Token-Arten im selben 64-Bit-Feld des `Hello`. Das Protokoll bleibt bei
Version 1; ein Lobby-Token ist auf dem Draht nur ein weiterer `u64`-Wert. Der
Codec liegt in
[`LobbyToken.cs`](../../Assets/_Project/Scripts/Networking/LobbyToken.cs), die
Lobby-Edge-Function (Deno) muss ihn bitgenau spiegeln.

**Statisches Token** (`NOVA_MATCH_TOKEN`): wie bisher vom Administrator
provisioniert, unbegrenzt wiederverwendbar; der Match-Seed ist konfiguriert
(`NOVA_RELAY_SEED`) oder wird je Match neu gewürfelt. Bleibt Pflicht und ist
der Direktweg für Tests und Betrieb ohne Lobby.

**Lobby-Token**: kurzlebig, von der externen Lobby (Supabase) pro Match
gemintet. Lobby und Relay teilen ausschließlich das statische HMAC-Secret
`NOVA_RELAY_TOKEN_SECRET` per Konfiguration — es gibt bewusst keinen neuen
Kanal zwischen ihnen. Layout der 64 Bit:

- Bits 63..44: 20-bit Ablauf-Bucket =
  `floor((unixMs − 2026-01-01T00:00:00Z) / 300000)`, also 5-Minuten-Scheiben.
- Bits 43..32: 12-bit Match-Id (Zufallszahl der Lobby).
- Bits 31..0: 32-bit Tag = erste 4 Bytes von
  `HMAC-SHA256(secret, ASCII "NOVA-LOBBY-TOKEN-V1" ‖ bucket als u32 big-endian ‖
  matchId als u16 big-endian)`.

Semantik im Relay:

- Gültig ist ein Lobby-Token, wenn der Tag stimmt (Fixed-Time-Vergleich) und
  sein Bucket im Fenster `[aktuell − 5, aktuell]` liegt — ein 30-Minuten-Fenster.
- **Single-Use:** Der erste Peer bindet das Token an das Match; der zweite Peer
  muss exakt dasselbe Token vorzeigen. Nach dem Match-Reset ist das Token
  verbraucht und nie wieder verwendbar — auch wenn das Match nie gestartet ist.
  Verbrauchte Einträge werden gepurgt, sobald ihr Bucket das Gültigkeitsfenster
  verlassen hat (solche Tokens können nie wieder gültig werden).
- **Seed:** Der Offer-Seed eines Lobby-Matches wird aus dem Token abgeleitet —
  `HMAC-SHA256(secret, ASCII "NOVA-LOBBY-SEED-V1" ‖ token als u64 big-endian)`,
  erste 8 Bytes, Ergebnis `| 1`, damit er nie die Zufallsseed-Konvention 0
  berührt. Beide Clients erhalten so denselben Seed, ohne dass er je übertragen
  oder verhandelt wird.
- Ablehnungen nennen weiterhin nur „wrong match code"; Token und
  Token-Bestandteile erscheinen weder in Logs noch in Fehlertexten.
- Ein gebundenes Token wird erst beim Match-Reset freigegeben beziehungsweise
  verbraucht: Verschwindet der wartende Peer vor Matchbeginn, bleibt das Token
  bis zum Reset gebunden und andere Lobby-Matches warten.

Grenzen dieses Vertrags:

- Der 32-bit Tag ist ein Zulassungs-MAC, keine Transportverschlüsselung;
  Online-Raten scheitert praktisch an den zwei Slots und dem
  5-Sekunden-Handshake-Timeout je Verbindung.
- Der 20-bit Bucket läuft nach 2^20 Scheiben (≈ 9,97 Jahre) über, frühestens
  2036; das Minting danach ist nicht Teil dieses Vertrags.
- Die 12-bit Match-Id kann innerhalb eines Fensters kollidieren: gleiche
  Bucket- und Id-Werte ergeben dasselbe Token. Die Lobby muss Ids so ziehen,
  dass sie innerhalb eines 30-Minuten-Fensters nicht doppelt vergibt.

## Release-Artefakt und Verzeichnislayout

Der Workflow veröffentlicht **kein Single-File-Binary**, sondern einen
self-contained `linux-x64`-Publish-Baum. Das Bundle enthält:

```text
app/                         vollständiger self-contained Publish-Baum
deploy/deploy.sh             Bootstrap, Deploy und Rollback
deploy/hashkrieg-relay.service
deploy/hashkrieg-relay.env.example
BUILD_INFO                   Commit-SHA, SDK 8.0.318, Runtime linux-x64
SHA256SUMS                   exaktes inneres Dateimanifest
```

Zu jedem Archiv
`hashkrieg-relay-<40-stellige-commit-sha>-linux-x64.tar.gz` gehört die äußere
Prüfsumme mit Suffix `.sha256`. Auf dem Server gilt:

```text
/opt/hashkrieg-relay/releases/<commit-sha>/   unveränderliches Release
/opt/hashkrieg-relay/current                  atomarer Link auf das aktive Release
/opt/hashkrieg-relay/previous                 atomarer Link auf den Rollback-Kandidaten
/opt/hashkrieg-relay/current/app/nova-relay   systemd-Executable
/etc/hashkrieg-relay.env                      manuell, root:root, Modus 0600
/etc/hashkrieg-relay.env.example              absichtlich ungültiges Beispiel
/var/lib/hashkrieg-relay/                     systemd-StateDirectory
```

`deploy.sh` akzeptiert nur Linux `x86_64`, muss als root laufen und serialisiert
Operationen mit `flock`. Release-Verzeichnisse sind nach der 40-stelligen
Commit-SHA benannt, root-owned und werden nie überschrieben.

## Konfigurationsvertrag

Der Prozess akzeptiert keine Kommandozeilenargumente. Jede Konfiguration kommt
aus der Umgebung:

| Variable | Pflicht | Default | Gültiger Wert |
|---|---|---|---|
| `NOVA_MATCH_TOKEN` | ja | – | exakt 16 unpräfixierte Hexzeichen, nicht `0000000000000000`; wird nie geloggt |
| `NOVA_RELAY_BIND` | nein | `127.0.0.1` | numerische IPv4- oder IPv6-Adresse; kein Hostname |
| `NOVA_RELAY_PORT` | nein | `47777` | dezimal 1024–65535 |
| `NOVA_RELAY_SLOT_COUNT` | nein | `2` | exakt `2` |
| `NOVA_INPUT_DELAY_TICKS` | nein | `3` | dezimal 1–60 |
| `NOVA_RECORD_DIR` | nein | Aufzeichnung aus | absoluter Pfad, nicht `/` |
| `NOVA_RELAY_SEED` | nein | neuer, zufälliger Seed | wenn gesetzt: exakt 16 unpräfixierte Hexzeichen, nicht null |
| `NOVA_RELAY_TOKEN_SECRET` | nein | nur statisches Token | wenn gesetzt: exakt 64 unpräfixierte Hexzeichen (32 Bytes); wird nie geloggt |

Ist `NOVA_RECORD_DIR` gesetzt, prüft der Prozess vor dem Listener-Start, ob er
das Verzeichnis erstellen, eine Probe schreiben, auf den Datenträger flushen
und wieder löschen kann. Ein Fehler ist ein Konfigurationsfehler; es gibt
keinen stillen Betrieb ohne die verlangte Aufzeichnung.

Exitcodes:

| Code | Bedeutung | systemd-Folge |
|---:|---|---|
| `0` | geordnetes Ende nach `SIGINT`/`SIGTERM` | kein Fehlerrestart |
| `1` | Setup-, Laufzeit- oder Shutdownfehler | `Restart=on-failure` |
| `78` | ungültige Umgebung oder unerlaubtes Argument | durch `RestartPreventExitStatus=78` kein Restart-Loop |

## Zwei Clients verbinden

Der implementierte Spielstart besitzt noch keine Ingame-Lobby und kein Feld
für einen Matchcode. Relay-Modus wird pro Unity-Prozess ausschließlich durch
`NOVA_RELAY_HOST` aktiviert. Beide Prozesse müssen vor ihrem Start dieselben
erreichbaren Host-/Portwerte und dasselbe serverseitig provisionierte Token
erhalten; Platzhalter ersetzen, kein echtes Token in Skripte oder das Repo
schreiben:

```bash
export NOVA_RELAY_HOST='<relay-host-or-ip>'
export NOVA_RELAY_PORT='47777'
export NOVA_MATCH_TOKEN='<same-16-hex-token>'
# Unity-Player beziehungsweise Editor-Prozess aus dieser Umgebung starten
```

`NOVA_RELAY_PORT` ist auf Clientseite bei gesetztem Host Pflicht und muss 1 bis
65.535 sein. `NOVA_MATCH_TOKEN` muss exakt 16 unpräfixierte Hexzeichen lang und
ungleich null sein; sein Wert muss mit der Serverumgebung übereinstimmen. Der
Relay weist den beiden erfolgreich verbundenen Prozessen automatisch Slot 0
und Slot 1 zu.

Fehlt `NOVA_RELAY_HOST` oder ist es leer, startet der bestehende lokale
Mensch-gegen-KI-Pfad. Ist der Host dagegen gesetzt und Port oder Token fehlen
beziehungsweise ungültig, meldet `MatchBootstrap` einen sichtbaren
Netzwerkstartfehler und fällt nicht still auf ein lokales Match zurück.

## Erstmaliges Bootstrap

Das Bootstrap installiert Benutzer, Gruppe, Verzeichnisse, Unit und die
Beispieldatei. Es erstellt, liest oder überschreibt **nicht** die Live-Datei
`/etc/hashkrieg-relay.env`.

Die drei Deploy-Dateien zuerst aus einem geprüften Checkout derselben
Ziel-Commit-SHA auf den Server übertragen. Beispiel vom Administrationsrechner:

```bash
ssh root@relay-host \
  'install -d -o root -g root -m 0700 /root/hashkrieg-relay-bootstrap'
scp tools/Nova.RelayServer/deploy/deploy.sh \
  tools/Nova.RelayServer/deploy/hashkrieg-relay.service \
  tools/Nova.RelayServer/deploy/hashkrieg-relay.env.example \
  root@relay-host:/root/hashkrieg-relay-bootstrap/
```

Dann auf dem Server:

```bash
cd /root/hashkrieg-relay-bootstrap
chmod 0755 deploy.sh
./deploy.sh bootstrap
systemctl is-enabled hashkrieg-relay.service
```

`bootstrap` legt den unprivilegierten Systembenutzer und die Gruppe
`novarelay` an, installiert die Unit nach
`/etc/systemd/system/hashkrieg-relay.service`, kopiert nur das ungültige
Beispiel nach `/etc/hashkrieg-relay.env.example`, führt `daemon-reload` aus
und aktiviert den Dienst für den Boot. Ohne Live-Umgebung wird noch kein
Release gestartet.

## Live-Umgebung manuell provisionieren

Die Datei wird ausschließlich durch einen Administrator angelegt. Der
Create-only-Aufruf mit Dateimodus `x` bricht ab, wenn der Pfad bereits
existiert, und überschreibt deshalb niemals ein vorhandenes Token:

```bash
umask 077
python3 -c 'open("/etc/hashkrieg-relay.env", "x").close()'
chown root:root /etc/hashkrieg-relay.env
chmod 0600 /etc/hashkrieg-relay.env
sudoedit /etc/hashkrieg-relay.env
stat -c '%U:%G %a %n' /etc/hashkrieg-relay.env
```

Existiert die Live-Datei bereits, den Create-only-Aufruf nicht wiederholen;
nur Besitz und Modus prüfen beziehungsweise korrigieren und danach mit
`sudoedit` bearbeiten. Kein Bootstrap- oder Deploy-Schritt ersetzt ihren
Inhalt.

Inhalt für einen bewusst öffentlich gebundenen Einzelhost; den Token-Platzhalter
vor dem ersten Deploy durch einen zufälligen 16-stelligen Hexwert ersetzen:

```dotenv
NOVA_MATCH_TOKEN=REPLACE_WITH_16_HEX_DIGITS
NOVA_RELAY_BIND=0.0.0.0
NOVA_RELAY_PORT=47777
NOVA_RELAY_SLOT_COUNT=2
NOVA_INPUT_DELAY_TICKS=3
NOVA_RECORD_DIR=/var/lib/hashkrieg-relay/records
```

`NOVA_RELAY_SEED` für normale Matches weglassen. Ein gesetzter Seed ist für
reproduzierbare Testläufe gedacht und muss ebenfalls exakt 16 Hexzeichen und
ungleich null sein. Die Live-Datei niemals committen, als Terminalausgabe
anzeigen oder zusammen mit einem Support-Log übertragen.

Eine Formprüfung des Tokens ohne Ausgabe seines Werts:

```bash
awk -F= '
  $1 == "NOVA_MATCH_TOKEN" {
    seen++
    if (NF != 2 || $2 !~ /^[[:xdigit:]]{16}$/ || $2 ~ /^0+$/) bad = 1
  }
  END { exit (seen == 1 && !bad) ? 0 : 1 }
' /etc/hashkrieg-relay.env
```

## Artefakt beziehen und prüfen

Der Workflow läuft auf Pull Requests, `main`-Pushes und manuell. Er verwendet
exakt .NET SDK `8.0.318`, führt die kanonischen Tests aus, publiziert den
self-contained Baum, prüft fehlenden Token/unerlaubte Argumente und einen
geordneten `SIGTERM`, baut beide Prüfsummen und lädt ein Bundle nur bei einem
`main`-Push oder einem manuellen Lauf auf `main` hoch. Er verwendet weder SSH
noch Deployment-Secrets und rollt nichts auf einen Server aus.

Beispiel auf dem Administrationsrechner:

```bash
release_sha=<40-stellige-kleinbuchstaben-commit-sha>
run_id=<github-actions-run-id>
artifact_name="hashkrieg-relay-${release_sha}-linux-x64"
gh run download "${run_id}" --name "${artifact_name}" --dir relay-artifact
(
  cd relay-artifact
  sha256sum -c "${artifact_name}.tar.gz.sha256"
)
```

`gh run download` authentisiert den Ursprung des Actions-Artefakts; die
äußere SHA-Datei erkennt Übertragungsänderungen. Das Deploy prüft danach
erneut die äußere Prüfsumme und zusätzlich das exakte innere Manifest.

## Deploy

Archiv und SHA-Datei in ein root-only Eingangsverzeichnis auf dem Server
übertragen:

```bash
install -d -o root -g root -m 0700 /root/hashkrieg-relay-incoming
```

Vom Administrationsrechner:

```bash
scp relay-artifact/hashkrieg-relay-<sha>-linux-x64.tar.gz \
  relay-artifact/hashkrieg-relay-<sha>-linux-x64.tar.gz.sha256 \
  root@relay-host:/root/hashkrieg-relay-incoming/
```

Erstes Deploy mit dem geprüften Bootstrap-Skript, spätere Deploys wahlweise
mit der geprüften Skriptversion der Ziel-Commit-SHA:

```bash
/root/hashkrieg-relay-bootstrap/deploy.sh deploy \
  /root/hashkrieg-relay-incoming/hashkrieg-relay-<sha>-linux-x64.tar.gz \
  /root/hashkrieg-relay-incoming/hashkrieg-relay-<sha>-linux-x64.tar.gz.sha256
```

Der Ablauf ist fail-closed:

1. Live-Umgebung muss regulär, root-owned und ohne Gruppen-/Weltrechte sein.
2. Archiv und äußere SHA-Datei werden ohne Symlink-Folgen in ein root-only
   Staging kopiert; nur diese stabilen Bytes werden weiterverarbeitet.
3. Äußere SHA-256, Tar-Struktur und inneres `SHA256SUMS` werden geprüft.
   Traversal, Symlinks, Hardlinks, Spezialdateien, Duplikate, unerwartete
   Dateien und Größenüberschreitungen werden abgelehnt.
4. `BUILD_INFO` muss exakt Commit-SHA, SDK `8.0.318` und `linux-x64` binden.
5. Der vollständige Baum wird root-owned normalisiert und unter
   `/opt/hashkrieg-relay/releases/<sha>` unveränderlich abgelegt.
6. Unit sowie `current`/`previous` wechseln atomar. Das Deployment startet den
   Dienst und wartet auf Readiness.
7. Bei fehlgeschlagener Aktivierung werden Unit und Links auf den vorigen Stand
   zurückgesetzt; auch der wiederhergestellte Dienst muss Readiness bestehen.

Readiness bedeutet: innerhalb von 30 Sekunden fünf aufeinanderfolgende
`active`-Beobachtungen mit derselben gültigen systemd-`InvocationID` und einem
`[Relay] ready on ...`-Eintrag genau dieser Invocation. Inaktivität, ein
Invocation-Wechsel, ein fehlender Ready-Eintrag oder `failed` setzt die Serie
zurück beziehungsweise bricht ab.

## Status und Logs

```bash
systemctl status hashkrieg-relay.service --no-pager
systemctl show hashkrieg-relay.service \
  -p ActiveState -p SubState -p Result -p ExecMainStatus -p InvocationID
journalctl -u hashkrieg-relay.service -n 200 --no-pager -o cat
ss -ltnp '( sport = :47777 )'
```

Der Prozess loggt Bind-Adresse, Port, Slotzahl, Input-Delay und ob Recording
aktiv ist, aber nie das Match-Token. Keine Befehle verwenden, die die
EnvironmentFile oder den Prozess-Environment-Inhalt ausgeben.

Die Unit läuft als `novarelay:novarelay`, verwendet
`EnvironmentFile=/etc/hashkrieg-relay.env`,
`ExecStart=/opt/hashkrieg-relay/current/app/nova-relay`, journald,
`StateDirectory=hashkrieg-relay`, `UMask=0077`, `SIGTERM` mit 15 Sekunden
Timeout, `Restart=on-failure` und `RestartPreventExitStatus=78`. Sie entfernt
Capabilities und aktiviert die im Repository stehenden systemd-Härtungen.

## Rollback

Ein Rollback setzt den vorherigen unveränderlichen Release-Baum atomar als
`current`, verschiebt den bisherigen aktiven Baum nach `previous`, installiert
dessen Unit und wendet denselben Readiness-Vertrag wie beim Deploy an:

```bash
/opt/hashkrieg-relay/current/deploy/deploy.sh rollback
```

Fehlt einer der beiden Links oder zeigen beide auf dasselbe Release, wird
abgebrochen. Scheitert der Rollback-Kandidat an Readiness, stellt das Skript
den zuvor aktiven Stand wieder her und prüft auch dessen Readiness. Die
Live-Umgebung wird weder kopiert noch verändert.

## Firewall

Der Dienst braucht einen eingehenden **TCP**-Port, standardmäßig `47777`. Die
Host- und gegebenenfalls Provider-Firewall müssen übereinstimmen. Wo feste
öffentliche Spieleradressen bekannt sind, nur diese freigeben.

Ubuntu/Debian mit UFW, je Spieleradresse wiederholen:

```bash
ufw allow proto tcp from 198.51.100.10 to any port 47777 \
  comment 'Hashkrieg relay player 1'
ufw status numbered
```

RHEL/Fedora mit firewalld, je Spieleradresse wiederholen:

```bash
firewall-cmd --permanent --add-rich-rule='rule family="ipv4" source address="198.51.100.10/32" port port="47777" protocol="tcp" accept'
firewall-cmd --reload
firewall-cmd --list-rich-rules
```

`198.51.100.10` ist eine Dokumentationsadresse und muss ersetzt werden. Bei
dynamischen Spieleradressen ist eine allgemeine Freigabe (`47777/tcp`) eine
bewusste Risikoentscheidung. Das Match-Token ist nur eine nachgelagerte
Anwendungsprüfung im `Hello`, keine frühe Verbindungszulassung. Die
Zweiergrenze ist ausschließlich eine Kapazitätsgrenze und schützt nicht vor
DoS: Der Server reserviert Slots bereits beim TCP-Accept vor einem gültigen
`Hello`, sodass zwei unauthentisierte Verbindungen beide Slots bis zum
Handshake-Timeout vorübergehend blockieren können. Deshalb ist die enge
Quelladress-Firewall die wichtigste Betriebsgrenze. Persistenzsyntax und
Regelreihenfolge unterscheiden sich je Distribution und Cloud-Anbieter. Es
gibt keinen nginx-/WebSocket-Ersatz für den rohen TCP-Port.

## Fehlerbehebung

### Dienst endet mit Status 78

Konfiguration oder Argumentvertrag ist ungültig. `systemd` startet den Prozess
absichtlich nicht in einer Schleife neu.

```bash
stat -c '%U:%G %a %n' /etc/hashkrieg-relay.env
systemctl show hashkrieg-relay.service -p Result -p ExecMainStatus
journalctl -u hashkrieg-relay.service -n 50 --no-pager -o cat
```

Prüfen: Token exakt 16 Hexzeichen und nicht null, numerische Bind-Adresse,
Port 1024–65535, Slotzahl exakt 2, Delay 1–60, Seedformat und absoluter
Record-Pfad ungleich `/`. Keine zusätzlichen Argumente an `nova-relay` geben.

### Dienst endet mit Status 1

Listener-, Polling- oder Shutdownfehler. Häufige Ursachen sind ein belegter
Port oder eine Laufzeitstörung. Listener und letzte Logs prüfen; danach erst
gezielt neu starten:

```bash
ss -ltnp '( sport = :47777 )'
systemctl reset-failed hashkrieg-relay.service
systemctl restart hashkrieg-relay.service
```

### Recording-Preflight scheitert

`NOVA_RECORD_DIR` muss unter dem `StateDirectory` für `novarelay` erstell- und
schreibbar sein. Die Unit erzeugt `/var/lib/hashkrieg-relay` mit Modus `0700`.
Besitz und Rechte prüfen, nicht durch eine Weltfreigabe umgehen:

```bash
stat -c '%U:%G %a %n' /var/lib/hashkrieg-relay
namei -l /var/lib/hashkrieg-relay/records
```

### Deploy meldet fehlende Readiness

Der Prozess muss fünf stabile Beobachtungen derselben Invocation bestehen.
Eine kurze `ready`-Meldung vor einem Crash reicht nicht. Invocation, Status und
Journal gemeinsam prüfen:

```bash
systemctl show hashkrieg-relay.service -p InvocationID -p ActiveState -p Result
journalctl -u hashkrieg-relay.service --no-pager -o cat
```

Das Deploy stellt den vorigen Zustand automatisch wieder her. Nicht manuell
`current` umbiegen; sonst gehen die transaktionalen Invarianten verloren.

### Artefakt wird abgelehnt

Archive und `.sha256` müssen zusammengehören. Der Tar darf ausschließlich den
festen Bundle-Inhalt tragen, und das innere Manifest muss jede Datei außer
`SHA256SUMS` exakt einmal abdecken. Ein bereits vorhandenes Release derselben
Commit-SHA ist absichtlich unveränderlich und wird nicht überschrieben.

### Extern keine Verbindung

`NOVA_RELAY_BIND=127.0.0.1` ist der sichere Default und nur lokal erreichbar.
Für einen externen Host bewusst auf eine öffentliche numerische Adresse oder
`0.0.0.0` binden, Dienst neu starten und danach Listener, Host-Firewall und
Provider-Firewall prüfen. Kein Port-Forwarding am Client ist erforderlich;
beide Clients bauen ausgehende TCP-Verbindungen auf.

## Nachweisstand und offene Abnahme

Stand 2026-08-07 lief lokal
`dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release --no-restore --nologo`
mit 547/547 grünen Tests, 0 übersprungen, in 11 s; darin lief der 10.023-Tick-
Soak erneut grün. Die vollständige `LockstepNetworkTests`-Klasse war zusätzlich
62/62 grün (3 s), der gezielte Fail-Closed-/Delay-/Timing-Pass 23/23 grün
(156 ms).
`dotnet build tools/Nova.RelayServer/Nova.RelayServer.csproj -c Release --no-restore --nologo`
endete mit 0 Warnungen und 0 Fehlern; auch Konfigurations-, Argument- und
geordnete Signal-Smokes wurden ausgeführt. Ein offline aus lokalem Cache
erzeugter self-contained
`linux-x64`-Baum hatte 186 Dateien; äußere und 190 innere Prüfsummen sowie der
gehärtete Extraktor wurden geprüft. A8 Stufe 1 lief mit zwei echten TCP-
Clients über den Relay bis Tick 10.023 und reproduzierte den Live-Endhash im
`NOVAREC2`-Playback; Checkpoints lagen alle 50 Ticks vor.

Nicht ausgeführt wurden ein Linux-ELF-Start, ein echtes systemd-/root-Deploy,
ein Live-Lauf des GitHub-Workflows, eine VPS-Installation und A8 Stufen 2–4
(zwei Unity-Fenster, LAN, VPS). Der Unity-EditMode-Versuch scheiterte vor den
Tests am Lizenzhandshake `505 Unsupported protocol version 1.18.1`; es gibt
daher kein Test-XML und keinen grünen Unity-Nachweis. Der Netzwerkpfad ist
technisch verdrahtet, aber noch nicht durch eine gespielte Netzwerkpartie
abgenommen.

## Nächste Schritte

1. Workflow auf dem neuen PR ausführen und das erzeugte Actions-Artefakt
   prüfen.
2. A8 Stufe 2 mit zwei Unity-Fenstern über Loopback spielen.
3. Danach LAN (Stufe 3) und erst nach ausdrücklicher Freigabe Bootstrap,
   Firewall und Deploy auf dem VPS durchführen.
4. Die vollständige VPS-Partie bis zum Ergebnisbildschirm als A8 Stufe 4
   protokollieren.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-08-07 | Vorläufiges Server-/Protokollblatt für A1–A5 | Agent (Umsetzung) |
| 1.0.0 | 2026-08-07 | D-089-Vertrag, vollständigen Artifact-/systemd-/Deploy-/Rollback-Pfad und ehrlichen A8-Nachweisstand dokumentiert | Technical Writer |
| 1.1.0 | 2026-08-09 | Kurzlebige Lobby-Match-Tokens (D-093, Sprint 14.5) | Agent (Umsetzung) |
