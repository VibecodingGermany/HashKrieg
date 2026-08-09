# Sprint 17: Wer da spielt — Zugangsprotokoll, Sperrliste und Erstmeldung

**Version:** 1.2.0 | **Status:** geplant | **Vorgänger:** [14_Sprint_Lobby.md](14_Sprint_Lobby.md) | **Reihenfolge:** Paket A läuft im [Großauftrag vom 2026-08-09](AUFTRAG_Grossblock.md) als **Block 4, direkt hinter der Lobby** | **Repo-Arbeit nach:** [15](15_Sprint_Netzstabilitaet.md) | **Paket A:** vorgezogen, ohne eine Zeile Repo-Code — baubar, sobald die Lobby-Functions aus Sprint 14 stehen | **Regelwerk:** [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md) | **Leitsatz:** eine Kennung, die man wegwerfen kann, ist trotzdem mehr wert als keine

## Ziel

Seit Sprint 14 nimmt ein Server von uns Verbindungen von fremden Rechnern
entgegen. Heute wissen wir über keinen davon irgendetwas: Wer eine Lobby
anlegt, hinterlässt keine Spur, und wer sie hundertmal pro Minute anlegt, wird
weder gebremst noch erkannt.

Sprint 17 gibt dem Betrieb ein Gedächtnis und eine Handbremse:

1. **Protokoll** — jeder Zugriff hinterlässt einen Eintrag, aus dem sich
   Wiederkehr, Häufung und Missbrauch ablesen lassen.
2. **Sperre** — ein Rechner oder Netz, das schadet, kommt nicht mehr durch die
   Vermittlung.
3. **Erstmeldung** — eine Installation meldet sich beim ersten Start, damit wir
   wissen, wie viele es gibt und woher sie kommen. Sie ist zugleich die
   Struktur, auf der später Lizenzen sitzen.

Der Sprint ändert **nichts am Spiel** — keine Simulation, keine Bedienung,
keinen Netzcode. Er baut Betriebsinfrastruktur um das herum, was schon läuft.

**Eine Ausnahme** hängt der [Großauftrag vom 2026-08-09](AUFTRAG_Grossblock.md)
an Block 4: Paket **17.0** behebt das `.partial`-Leck in
`Scripts/Networking/RelayServerCore.cs`. `ResetMatch` verwirft den
Aufzeichnungsstrom per `Dispose` und merkt sich den Pfad, löscht die
`.partial`-Datei aber nie; nur der Erfolgsweg benennt sie atomar nach `.novarec`
um. Jede abgebrochene Partie lässt eine Datei liegen, die niemand aufräumt. Das
ist aus Sprint 15.5 vorgezogen und der einzige Repo-Code, den dieser Sprint in
der geschlossenen Beta anfasst.

## Was tatsächlich identifizierbar ist — und was nicht

Diese Tabelle ist der Grund, warum der Sprint so geschnitten ist wie er ist.
Sie steht hier, damit die Erwartung an das Ergebnis stimmt.

| Merkmal | Taugt es? | Warum |
|---|---|---|
| **MAC-Adresse** | **nein** | Sie endet am ersten Router. Weder Supabase noch der Relay sehen sie jemals — sie überquert kein NAT. Meldet der Client sie selbst, ist sie eine Selbstauskunft, die unter Windows in den Adaptereigenschaften in einer halben Minute geändert ist |
| **IP-Adresse** | **teilweise** | Der Server sieht sie ohnehin, ohne Zutun des Clients — das macht sie zum einzigen Merkmal, das nicht gefälscht werden kann. Aber sie wechselt (dynamische Zuteilung) und sie ist geteilt: Vodafone Kabel und der gesamte Mobilfunk laufen über CGNAT, eine öffentliche IP trägt dort viele Haushalte. **Brauchbar für kurzfristige Bremsen, ungeeignet für Dauersperren** |
| **Installationskennung** (GUID, von uns vergeben) | **ja, mit Grenze** | Erkennt Wiederkehrer zuverlässig, solange niemand sie absichtlich löscht. Genau das ist ihr ehrlicher Zweck: Statistik und Gelegenheitstäter, nicht Sicherheit |
| **Geräte-Anker** (`SystemInfo.deviceUniqueIdentifier`) | **ja, als zweite Spur** | Überlebt das Löschen unserer Datei und eine Neuinstallation des Spiels. Nicht die Neuinstallation des Betriebssystems. Sein Wert liegt im Abgleich: dieselbe Maschine mit vierzig Kennungen ist ein Befund |
| **Konto oder Lizenzschlüssel** | **ja, hart** | Der einzige Anker, den ein Gebannter nicht wegwerfen kann. Kommt nicht in diesem Sprint — aber die Tabellen hier sind so gebaut, dass er sich später danebensetzen lässt |
| **Steam-ID** | **ja, hart, geschenkt** | D-007 sieht Steam als Vertriebsweg. Läuft der Verkauf dort, liefert Steam einen bannbaren Anker ohne eigenes Kontosystem. Das ist der wahrscheinlichste Endzustand |

Die nüchterne Zusammenfassung: **Auf einem Rechner, den der Gegner
kontrolliert, gibt es keine unfälschbare Identität.** Was dieser Sprint
liefert, ist Reibung und Sichtbarkeit — genug gegen Spam, gegen Skript-Fluten
und gegen den Wütenden, der wiederkommt. Nicht genug gegen jemanden, der
Aufwand investiert. Dagegen hilft nur der harte Anker aus der letzten beiden
Zeilen, und der kommt mit dem Verkauf.

## Pakete

### Paket A — Serverseite, ohne Repo-Code

Alles unter A liegt im Supabase-Projekt ausserhalb des Repositories und
berührt keine Datei unter `Assets/`. Es kollidiert deshalb mit **keiner**
Schreibhoheit und läuft parallel zu Sprint 15 und 13B.

**Voraussetzungsfrei ist es trotzdem nicht.** Paket A erweitert die Edge
Functions aus Sprint 14 — es setzt sie voraus, statt sie mitzubringen. Diese
Voraussetzung ist **erfüllt**: die Lobby liegt seit `b4e75e5` auf `main`, samt
Vertrag, Schema und Function-Quelltexten in
[../../tech/LobbySupabase.md](../../tech/LobbySupabase.md) (D-092 bis D-094).
Paket A ist damit Block 3 des Großauftrags vom 2026-08-09.

> **Aktenstand:** [14_Sprint_Lobby.md](14_Sprint_Lobby.md) trägt weiterhin den
> Status `geplant`, obwohl der Code gemergt ist. Die Sprintdatei wurde beim
> Merge nicht nachgezogen — wer sie als Ist-Stand liest, hält eine gebaute
> Sache für offen.

Das Entscheidende daran: Diese Functions **sehen die IP ohnehin**. Sobald sie
laufen, liefert Paket A Protokoll und IP-Sperre, ohne dass ein einziger Spieler
ein Update braucht.

#### 17.1 · Zugriffsprotokoll

Jeder Aufruf von `create-match`, `join-match`, `set-ready` und `match-status`
schreibt eine Zeile: Zeitpunkt, Endpunkt, Herkunft, Build-Commit, Match-Code,
Ergebnis.

**Für die geschlossene Beta wird die Herkunfts-IP im Klartext gespeichert**,
zusammen mit dem gekürzten Netzpräfix (`/24` bei IPv4, `/48` bei IPv6). Die
frühere Vorgabe „keine rohe IP in der Datenbank" gilt für diese Phase nicht.

Der Grund ist der Zweck des Protokolls. Der Betreiberkreis ist eine geschlossene
Gruppe, und der Betreiber will sehen, wer spielt. Ein nicht umkehrbarer Hash
beantwortet genau diese Frage nicht mehr: Er zeigt, dass jemand wiederkommt, aber
nicht, wer. Damit macht er die Abfrage schwer, für die das Protokoll gebaut wird.

Die Begrenzung, die an die Stelle des Hashes tritt, ist die **Löschfrist von 30
Tagen aus 17.5**. Sie hängt an einem `pg_cron`-Job, nicht an Erinnerung.

Drei geprüfte Wege:

| Weg | Was er leistet | Bewertung |
|---|---|---|
| `HMAC(pepper, ip)` | Wiederkehr bleibt erkennbar, ein Datenbankleck gibt keine Adressliste her | die Betreiberfrage „wer spielt da" bleibt unbeantwortet — im geschlossenen Kreis der falsche Tausch |
| nur das Netzpräfix, keine Adresse | sparsamste Variante, kein Rohwert | trennt zwei Anschlüsse im selben `/24` nicht; die Einzelsperre aus 17.2 verliert ihre Grundlage |
| **Klartext-Adresse plus Präfix, 30 Tage** | beantwortet die Betreiberfrage, trägt Einzel- **und** Netzsperre | **gewählt** für die geschlossene Beta |

Der Pepper (`NOVA_ACCESS_PEPPER`) bleibt: Installationskennung und Geräte-Anker
aus Paket B werden weiterhin serverseitig gehasht. Er liegt ausschliesslich in
der Function-Umgebung — dieselbe Ablageform, in der der Relay heute sein
`NOVA_MATCH_TOKEN` hält: in der root-eigenen Env-Datei
`/etc/hashkrieg-relay.env` mit Modus 0600. Verglichen wird hier nur die Ablage.
`NOVA_MATCH_TOKEN` ist kein Hash-Pepper, sondern das geteilte Match-Token, mit
dem sich ein Client beim Relay ausweist.

**Vor Öffnung der Beta ist neu zu entscheiden.** Sobald der Kreis nicht mehr
geschlossen ist, ändert sich die Rechnung. Die Umstellung auf Hashing steht als
**Q-041** im [Fragenkatalog](../OpenQuestions.md) und ist vor der Öffnung zu
beantworten, nicht danach.

#### 17.2 · Sperrliste, die vor der Vermittlung greift

Eine Tabelle `access_blocks` und eine Prüfung als **erste Anweisung jeder
Function**. Ein Treffer beendet den Aufruf mit `403 blocked`, bevor
irgendetwas anderes passiert.

Drei Sperrarten, mit unterschiedlichen Regeln:

| Art | Gegen | Befristung |
|---|---|---|
| `install` | Installationskennung (Hash) | unbefristet erlaubt |
| `ip` | einzelne Adresse (Klartext, siehe 17.1) | **Pflicht**, höchstens 30 Tage |
| `prefix` | Netz `/24` bzw. `/48` | **Pflicht**, höchstens 7 Tage |

Die Befristungspflicht für IP und Präfix ist kein Formalismus: Hinter einer
CGNAT-Adresse sitzen Unbeteiligte, und eine dynamische Adresse gehört morgen
jemand anderem. Eine unbefristete IP-Sperre sperrt mit Sicherheit irgendwann
den Falschen aus. Die Datenbank erzwingt es per Constraint, nicht per
Disziplin.

#### 17.3 · Bremsen statt sperren

Ein Zählfenster pro Netzpräfix und eines pro Adresse. Ein drittes Fenster pro
Installation kommt mit Paket B — bis dahin bleibt die Sperrart `install` aus
17.2 als Schema angelegt und unbefüllt, weil die Installationskennung erst mit
Paket B über die Leitung geht. Wer die Grenze reisst, bekommt `429` mit
Wartezeit — automatisch, ohne dass jemand eine Entscheidung treffen muss.

Das fängt den mit Abstand häufigsten Fall: nicht den Feind, sondern die
kaputte Schleife. Eine Sperre ist die Ausnahme, das Limit ist der Alltag.

#### 17.4 · Der Bedienweg

Ohne diesen Punkt sind die Tabellen aus 17.1–17.3 Dekoration. Es braucht einen
beschriebenen Weg, wie **du** eine Sperre setzt, ansiehst und zurücknimmst.

Kein Oberflächenbau — fertige SQL-Bausteine im Runbook, ausführbar im
Supabase-Editor: „Wer war das in den letzten 24 Stunden", „sperre diese
Installation", „welche Sperren laufen gerade", „nimm das zurück". Dazu eine
Abfrage für den typischen Befund: *ein Geräte-Anker mit auffällig vielen
Installationskennungen* — das Muster einer Wegwerf-Identität.

#### 17.5 · Fristen, die von selbst laufen

Ein `pg_cron`-Job, damit die Protokolle nicht ewig wachsen und die
Löschfristen nicht an Erinnerung hängen:

| Datensatz | Frist |
|---|---|
| Protokollzeilen | 30 Tage |
| Zählfenster | 1 Stunde |
| Abgelaufene Sperren | 90 Tage nach Ablauf |
| Installationen (`last_seen`) | 24 Monate |
| Tageszahlen, aggregiert | unbegrenzt (keine Kennungen mehr enthalten) |

### Paket B — Clientseite, nach Sprint 15 — **für die geschlossene Beta zurückgestellt**

> **Zurückgestellt, nicht gestrichen.** Der Großauftrag vom 2026-08-09 zieht
> Paket A als Block 4 vor und enthält Paket B nicht. Für die geschlossene Beta
> wird es nicht gebaut: In einem Kreis, den der Betreiber ohnehin kennt, zählt
> eine Installationskennung niemanden, den er nicht kennt. Der Zeitpunkt steht
> als **Q-042** im [Fragenkatalog](../OpenQuestions.md). Die Abschnitte 17.6 bis
> 17.8 bleiben unverändert stehen, damit sie beim Aufgreifen nicht neu
> geschrieben werden müssen.

Paket B fasst `Scripts/Networking/` und `Scripts/Core/` an. Beide gehören dem
Netzstrang, aber Sprint 15 arbeitet dort — B startet deshalb erst, wenn 15
integriert ist.

#### 17.6 · Die Installationskennung

Beim ersten Start eine GUID erzeugen und in `Application.persistentDataPath`
ablegen, neben der `settings.json` aus D-083. Zusätzlich wird
`SystemInfo.deviceUniqueIdentifier` als zweiter Anker gelesen.

Beide Werte gehen bei jedem Lobby-Aufruf roh über die Leitung und werden
**serverseitig gehasht**; die Rohwerte werden nirgends gespeichert.

#### 17.7 · Die Erstmeldung

Legt der Client die Kennungsdatei neu an, meldet er das einmalig an
`/register-install`: Kennung, Geräte-Anker, Betriebssystem grob,
Build-Commit. **Ohne Dialog** — Inhaberentscheidung, siehe unten.

Zwei harte Auflagen an die Umsetzung, beide aus D-007 (Singleplayer-first):

- **Fire-and-forget.** Drei Sekunden Zeitüberschreitung, Fehler werden
  verschluckt. Das Spiel wartet nie auf den Ping und startet ohne Netz
  identisch.
- **Kein Blocker.** Eine fehlgeschlagene Meldung hat keinerlei Folge für das
  Spiel. Es gibt keinen Pfad, auf dem der Ping über Spielbarkeit entscheidet.

#### 17.8 · Transparenz und Widerspruch — **für die geschlossene Beta zurückgestellt**

> **Zurückgestellt.** Der Inhaber hat die Formalitäten für den geschlossenen
> Kreis vertagt. Datenschutzerklärung und Widerspruchsschalter werden in dieser
> Phase nicht gebaut; der Zeitpunkt steht als **Q-042** im
> [Fragenkatalog](../OpenQuestions.md). Der Abschnitt bleibt stehen, weil er mit
> der Öffnung der Beta wieder gilt — spätestens dann zusammen mit Q-041 aus
> 17.1.

Der Inhaber hat gegen einen Zustimmungsdialog entschieden. Zwei Pflichten
bleiben davon unberührt, weil sie nicht an der Rechtsgrundlage hängen:

- **Auskunft (Art. 13 DSGVO):** eine Datenschutzerklärung unter
  `docs/legal/Datenschutz.md`, im Hauptmenü erreichbar. Sie benennt, was
  erhoben wird, warum, wie lange, und wer der Auftragsverarbeiter ist. Es gibt
  im Repository heute kein solches Dokument.
- **Widerspruch (Art. 21 DSGVO):** ein Schalter in `settings.json`. Er stellt
  Erstmeldung und Nutzungsstatistik ab.

Was der Schalter **nicht** abstellt, und was in der Erklärung so stehen muss:
die Missbrauchsabwehr beim Online-Spiel. Wer eine Lobby betritt, wird
protokolliert — dafür gibt es zwingende schutzwürdige Gründe, und die IP sieht
der Server ohnehin. Diese Trennung ist rechtlich sauber und technisch ehrlich.

## Datenmodell

Skizze, verbindlich ausformuliert wird sie in `docs/tech/AccessLog.md`.

```sql
create table installs (
  install_hash  bytea primary key,          -- HMAC(pepper, guid)
  device_hash   bytea,                      -- HMAC(pepper, deviceUniqueIdentifier)
  first_seen    timestamptz not null default now(),
  last_seen     timestamptz not null default now(),
  seen_count    int         not null default 1,
  os            text,                       -- windows | macos | linux
  first_build   text,
  last_build    text
);

create table access_log (
  id           bigserial primary key,
  at           timestamptz not null default now(),
  endpoint     text        not null,
  outcome      text        not null,        -- ok | blocked | rate_limited | build_mismatch | ...
  ip           inet        not null,        -- Klartext (geschlossene Beta, 17.1); Löschfrist 30 Tage
  ip_prefix    inet        not null,        -- /24 bzw. /48, für Netzsperren
  install_hash bytea,                       -- null bis Paket B ausgeliefert ist
  match_code   text,
  build_commit text
);

create table access_blocks (
  id         bigserial primary key,
  kind       text not null check (kind in ('install','ip','prefix')),
  value      text not null,                 -- Installations-Hash (hex), Adresse oder Präfix
  reason     text not null,
  note       text,
  created_at timestamptz not null default now(),
  expires_at timestamptz,
  -- IP- und Präfixsperren sind immer befristet (CGNAT, dynamische Adressen)
  constraint befristung check (kind = 'install' or expires_at is not null)
);
```

Row-Level-Security bleibt wie in Sprint 14: keine Policies, also deny-all für
`anon`. Jeder Zugriff läuft über die Functions mit dem Service-Role-Key.

## Governance: die Tier-Frage

[GOVERNANCE.md](../../../GOVERNANCE.md) nennt „Nutzerdaten im Spiel" als
Auslöser für Tier 3, und [Sprint 14](14_Sprint_Lobby.md) hält in seiner
Risikotabelle fest: „Vor dem ersten Feld, das es wäre, D-ID." IP und
Geräte-Kennung sind personenbezogene Daten. Nach dem Buchstaben weckt dieser
Sprint also die schlafende Gate-Kette G0–G5.

*Aktenberichtigung:* Dieser Satz war hier bisher `docs/tech/LobbySupabase.md`
zugeschrieben. Diese Datei existiert nicht — sie ist an mehreren Stellen
verlinkt, aber nie geschrieben worden, und **wird in Sprint 14 angelegt**
(Runbook zur Serverseite, siehe Großauftrag Block 3). Die Quelle des Satzes ist
Sprint 14 selbst.

**Inhaberentscheidung: die Definition wird präzisiert statt der Kette
geweckt.** Tier 3 hängt künftig an Veröffentlichung, Geld und Publikum — an
einer Steam-Seite, einem bezahlten Build, einem Publisher-Vertrag. Nicht an
jeder personenbezogenen Verarbeitung. Betriebs- und Missbrauchsdaten mit
Löschfristen bleiben Tier 2. Für die geschlossene Beta gilt das auch mit der
Klartext-Adresse aus 17.1: Der Kreis ist geschlossen, die Frist läuft
automatisch, und die Umstellung auf Hashing steht als Q-041 im Fragenkatalog.

Die Begründung, die in die D-ID gehört: Der Tier-3-Apparat beantwortet die
Frage „können wir es Dritten beweisen" — Evidenzketten, Receipts,
Doppelfreigaben. Betriebsprotokolle werfen diese Frage nicht auf. Sie werfen
Datenschutzfragen auf, und die beantwortet man mit Datensparsamkeit und
Fristen, nicht mit einer Gate-Kette.

Umzusetzen ist das in der Tier-Tabelle in `GOVERNANCE.md` (Zeile „Auslöser")
plus einem Satz zur Abgrenzung. Ein Hot-File — serialisiert, ein Schreiber.

## Schreibhoheit

| Pfad | Paket |
|---|---|
| Supabase-Projekt (ausserhalb des Repos) | A: Schema, Functions, Cron-Jobs |
| `docs/tech/AccessLog.md` (neu) | A: Vertrag, Schema, Betriebsabfragen |
| `docs/tech/LobbySupabase.md` (existiert nicht, **wird in Sprint 14 angelegt**) | A: Sperrprüfung in den Vertrag; `register-install` erst mit Paket B |
| `Scripts/Networking/RelayServerCore.cs` | 17.0: **nur der `.partial`-Pfad** — vorgezogen aus 15.5 |
| `docs/tech/RelayServer.md` | 17.0: Aufbewahrungsregel für `.novarec` als **Vorschlag**; die Frist entscheidet der Inhaber (Q-046) |
| `docs/legal/Datenschutz.md` (neu) | B: 17.8 — **zurückgestellt** |
| `Scripts/Core/Identity/` (neu) | B: 17.6 |
| `Scripts/Networking/Lobby/` | B: Kennung im Request, Sperrantwort |
| `GOVERNANCE.md` | Tier-Präzisierung — Hot-File, serialisiert |

**Keine Datei unter `Scripts/Simulation/` oder `Scripts/AI*`.** Der Sprint
fasst den Spielablauf nicht an.

## Bewusst nicht in diesem Sprint

| | Warum |
|---|---|
| Konten, Login, Profile | unverändert aus Sprint 14 — der harte Anker kommt über Steam oder Lizenz, nicht über ein eigenes Kontosystem |
| Lizenzprüfung | die Tabellen hier sind die Vorarbeit; der Verkauf entscheidet die Bauform, und der steht nicht an |
| Sperrverwaltung als Oberfläche | SQL im Runbook reicht für einen Betreiber; eine UI baut man, wenn sie jemand täglich braucht |
| Anti-Cheat, serverseitige Prüfung | der Relay simuliert nicht, das bleibt Absicht (Sprint 15) |
| Geolokalisierung über Grobland hinaus | ohne Zweck, damit ohne Rechtsgrundlage |
| Sperre **und Protokollierung** im Relay | der Relay bleibt dumm: er protokolliert nicht und prüft keine Sperre. Beides liegt in den Lobby-Functions. Wer keine Vermittlung bekommt, bekommt kein Token — das ist die Sperre. Der statische Direktweg aus Sprint 13 bleibt davon unberührt und offen |

## Risiken

| Risiko | Umgang |
|---|---|
| Kennung ist löschbar, Sperre damit umgehbar | so gewollt und offen benannt; der Geräte-Anker macht das Muster sichtbar, der harte Anker kommt mit dem Verkauf |
| IP-Sperre trifft Unbeteiligte hinter CGNAT | Befristung per Datenbank-Constraint erzwungen, Präfixsperre höchstens 7 Tage |
| Ein Fehler in der Sperrprüfung sperrt alle aus | fail-open: schlägt die Abfrage technisch fehl, läuft der Aufruf durch und protokolliert den Fehlschlag. Ein Ausfall darf niemandem das Spiel nehmen |
| Erstmeldung ohne Dialog wird beanstandet | Inhaberentscheidung; die Erstmeldung liegt in Paket B und ist für die geschlossene Beta zurückgestellt. Auskunft und Widerspruch (17.8) sind mit ihr fällig, nicht vorher. Bei einem Steam-Release verlangt Valve zusätzlich eine Privacy-Policy-URL |
| Ping hängt den Spielstart | drei Sekunden Zeitüberschreitung, Fehler verschluckt, kein Pfad vom Ping zur Spielbarkeit (D-007) |
| Pepper geht verloren | alle Hashes werden unbrauchbar, die Zuordnung ist weg. Der Pepper gehört in dieselbe Sicherung wie das geteilte Match-Token `NOVA_MATCH_TOKEN` des Relays aus `/etc/hashkrieg-relay.env` |
| Auftragsverarbeitung Supabase | AV-Vertrag abschliessen, Projektregion EU. Vor Paket A zu klären, nicht danach |

## Fertig wenn

1. `dotnet test tools/Nova.SimRunner.Tests` grün.
2. Eine Sperre auf die eigene Installationskennung verhindert nachweislich den
   Lobby-Beitritt und erklärt es im Klartext.
3. Eine Stichprobe aus `access_log` enthält keine rohe Installationskennung. Die
   Herkunfts-IP steht dort für die geschlossene Beta bewusst im Klartext (17.1).
4. Der Löschjob hat nachweislich gelöscht — eine Zeile älter als die Frist ist
   nach einem Lauf verschwunden.
5. Ein zweiter Rechner erscheint nach dem ersten Start in `installs`, und das
   Spiel startet ohne Netzverbindung unverändert.
6. Die Datenschutzerklärung ist aus dem Hauptmenü erreichbar.
7. Notiert im [GrayboxLog](../GrayboxLog.md).

**Was davon in der geschlossenen Beta gilt:** die Punkte 1, 3, 4 und 7, und
Punkt 2 in der Form „eine Sperre auf die eigene Adresse". Die Punkte 5 und 6
sowie der Kennungsteil von Punkt 2 gehören zu Paket B und gelten, wenn Paket B
aufgegriffen wird.

## Entscheidungen, die dieser Sprint erzeugt

| ID | Inhalt | Wer |
|---|---|---|
| D-098 | Tier-3-Auslöser präzisiert: Veröffentlichung/Geld/Publikum statt jeder personenbezogenen Verarbeitung; Betriebs- und Missbrauchsdaten mit Löschfristen bleiben Tier 2. Hashing ist dafür in der geschlossenen Beta nach D-099 keine Bedingung — die Grenze ist die Frist, nicht der Hash. Q-041 entscheidet vor der Öffnung neu | Inhaber |
| D-099 | Identitätsmodell: Für die geschlossene Beta wird die Herkunfts-IP **im Klartext** gespeichert, dazu das gekürzte Netzpräfix; begrenzt wird sie durch die 30-Tage-Löschfrist statt durch einen Hash. Installations-GUID und Geräte-Anker bleiben serverseitig gepeppert gehasht (Paket B). MAC-Adresse ausdrücklich verworfen; IP-Sperren nur befristet. Die Umstellung auf Hashing ist vor Öffnung der Beta neu zu entscheiden und steht als Q-041 im Fragenkatalog | Inhaber (Richtung) / Agent (Ausformung) |
| D-100 | Erstmeldung beim ersten Start ohne Zustimmungsdialog, gestützt auf berechtigtes Interesse; Auskunft und Widerspruch werden gebaut. Verhältnis zu Q-019 („Opt-in Telemetrie") ist mitzuentscheiden — D-100 ersetzt Q-019 für die Erstmeldung | Inhaber |

## Changelog-Notiz

Zugangsprotokoll und Sperrliste für die Lobby: Herkunfts-IP mit gekürztem
Netzpräfix und automatischer Löschung nach 30 Tagen, befristete IP- und
Netzsperren, Rate-Limit und ein Bedienweg aus fertigen SQL-Bausteinen.
Installationskennung, Erstmeldung, Datenschutzerklärung und
Widerspruchsschalter sind für die geschlossene Beta zurückgestellt.

## Versionsrelevanz

`minor`.

## Danach

Der harte Anker. Sobald der Vertriebsweg feststeht (Steam nach D-007, oder
eigener Verkauf mit Lizenzschlüssel), setzt sich eine bannbare Konto-Kennung
neben `install_hash` — Tabellen, Sperrarten und Bedienweg bleiben, nur die
Spalte kommt dazu. Das ist der Punkt, an dem aus Reibung eine Sperre wird, die
hält.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.2.0 | 2026-08-09 | Vier Widersprüche aus der Prüfung des Großauftrags behoben. 17.3 zählt für die geschlossene Beta pro Netzpräfix und pro Adresse; das Fenster pro Installation kommt mit Paket B, bis dahin bleibt die Sperrart `install` unbefüllt. Paket 17.0 (`.partial`-Leck in `RelayServerCore.ResetMatch`, vorgezogen aus 15.5) als Ausnahme zum „nichts am Spiel" benannt und mit zwei Zeilen in die Schreibhoheit aufgenommen. Die Umgebungsvariable an beiden Stellen auf `NOVA_MATCH_TOKEN` gestellt. **Nachtrag:** `NOVA_RELAY_TOKEN_SECRET` existiert seit D-093 sehr wohl (Secret für die HMAC-Match-Tokens der Lobby); die ursprüngliche Korrektur beruhte auf einem veralteten Stand. Für den Vergleich in 17.1 bleibt `NOVA_MATCH_TOKEN` trotzdem das treffendere Beispiel, weil es die root-eigene Env-Datei beschreibt. D-098 auf den Absatz darüber nachgezogen: Löschfristen tragen die Tier-2-Einstufung, Hashing ist für die geschlossene Beta keine Bedingung | Orchestrator |
| 1.1.0 | 2026-08-09 | Paket A vorgezogen und für die geschlossene Beta vereinfacht: Die Herkunfts-IP wird im Klartext gespeichert, begrenzt durch die 30-Tage-Löschfrist statt durch `HMAC(pepper, ip)`; drei Wege bewertet, die Umstellung auf Hashing steht als Frage im Fragenkatalog. Paket B und 17.8 als zurückgestellt markiert, nicht gelöscht. Die Relay-Zeile um die Protokollierung erweitert. D-099 auf die Klartext-Adresse nachgezogen. Zwei Aktenfehler berichtigt: `docs/tech/LobbySupabase.md` existiert nicht und wird in Sprint 14 angelegt; Paket A ist nicht „sofort baubar", sondern setzt die Lobby-Functions aus Sprint 14 voraus, die heute null Zeilen im Repository haben | Orchestrator |
| 1.0.0 | 2026-08-09 | Erstfassung: Zugriffsprotokoll, Sperrliste, Rate-Limit, Bedienweg und Fristen als Paket A; Installationskennung und Erstmeldung als Paket B | Orchestrator |
