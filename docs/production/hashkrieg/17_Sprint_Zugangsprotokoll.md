# Sprint 17: Wer da spielt — Zugangsprotokoll, Sperrliste und Erstmeldung

**Status:** geplant | **Vorgänger:** [14_Sprint_Lobby.md](14_Sprint_Lobby.md) | **Repo-Arbeit nach:** [15](15_Sprint_Netzstabilitaet.md) | **Paket A vorziehbar:** ja, ohne eine Zeile Repo-Code | **Regelwerk:** [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md) | **Leitsatz:** eine Kennung, die man wegwerfen kann, ist trotzdem mehr wert als keine

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
Schreibhoheit und ist **sofort baubar**, parallel zu Sprint 15 und 13B.

Das Entscheidende daran: Die Edge Functions aus Sprint 14 **sehen die IP
bereits**. Paket A liefert Protokoll und IP-Sperre, ohne dass ein einziger
Spieler ein Update braucht.

#### 17.1 · Zugriffsprotokoll

Jeder Aufruf von `create-match`, `join-match`, `set-ready` und `match-status`
schreibt eine Zeile: Zeitpunkt, Endpunkt, Herkunft, Build-Commit, Match-Code,
Ergebnis.

**Keine rohe IP in der Datenbank.** Gespeichert wird `HMAC(pepper, ip)` plus
das gekürzte Netzpräfix (`/24` bei IPv4, `/48` bei IPv6). Der Pepper
(`NOVA_ACCESS_PEPPER`) liegt ausschliesslich in der Function-Umgebung, genau
wie `NOVA_RELAY_TOKEN_SECRET` heute. Wiedererkennung funktioniert über den
Hash unverändert; ein Datenbankleck gibt trotzdem keine Adressliste her.

#### 17.2 · Sperrliste, die vor der Vermittlung greift

Eine Tabelle `access_blocks` und eine Prüfung als **erste Anweisung jeder
Function**. Ein Treffer beendet den Aufruf mit `403 blocked`, bevor
irgendetwas anderes passiert.

Drei Sperrarten, mit unterschiedlichen Regeln:

| Art | Gegen | Befristung |
|---|---|---|
| `install` | Installationskennung (Hash) | unbefristet erlaubt |
| `ip` | einzelne Adresse (Hash) | **Pflicht**, höchstens 30 Tage |
| `prefix` | Netz `/24` bzw. `/48` | **Pflicht**, höchstens 7 Tage |

Die Befristungspflicht für IP und Präfix ist kein Formalismus: Hinter einer
CGNAT-Adresse sitzen Unbeteiligte, und eine dynamische Adresse gehört morgen
jemand anderem. Eine unbefristete IP-Sperre sperrt mit Sicherheit irgendwann
den Falschen aus. Die Datenbank erzwingt es per Constraint, nicht per
Disziplin.

#### 17.3 · Bremsen statt sperren

Ein Zählfenster pro Netzpräfix und pro Installation. Wer die Grenze reisst,
bekommt `429` mit Wartezeit — automatisch, ohne dass jemand eine Entscheidung
treffen muss.

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

### Paket B — Clientseite, nach Sprint 15

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

#### 17.8 · Transparenz und Widerspruch

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
  ip_hash      bytea       not null,        -- HMAC(pepper, ip) — nie die Adresse selbst
  ip_prefix    inet        not null,        -- /24 bzw. /48, für Netzsperren
  install_hash bytea,                       -- null bis Paket B ausgeliefert ist
  match_code   text,
  build_commit text
);

create table access_blocks (
  id         bigserial primary key,
  kind       text not null check (kind in ('install','ip','prefix')),
  value      text not null,                 -- Hash (hex) oder Präfix
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
Auslöser für Tier 3, und [LobbySupabase.md](../../tech/LobbySupabase.md) hält
fest: „Vor dem ersten Feld, das eine Person betrifft, ist eine neue D-ID
fällig." IP und Geräte-Kennung sind personenbezogene Daten. Nach dem Buchstaben
weckt dieser Sprint also die schlafende Gate-Kette G0–G5.

**Inhaberentscheidung: die Definition wird präzisiert statt der Kette
geweckt.** Tier 3 hängt künftig an Veröffentlichung, Geld und Publikum — an
einer Steam-Seite, einem bezahlten Build, einem Publisher-Vertrag. Nicht an
jeder personenbezogenen Verarbeitung. Betriebs- und Missbrauchsdaten mit
gehashten Kennungen, Löschfristen und veröffentlichter Datenschutzerklärung
bleiben Tier 2.

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
| `docs/tech/LobbySupabase.md` | A: Sperrprüfung und `register-install` in den Vertrag |
| `docs/legal/Datenschutz.md` (neu) | B: 17.8 |
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
| Sperre im Relay | der Relay bleibt dumm; wer keine Vermittlung bekommt, bekommt kein Token — das ist die Sperre. Der statische Direktweg aus Sprint 13 bleibt davon unberührt und offen |

## Risiken

| Risiko | Umgang |
|---|---|
| Kennung ist löschbar, Sperre damit umgehbar | so gewollt und offen benannt; der Geräte-Anker macht das Muster sichtbar, der harte Anker kommt mit dem Verkauf |
| IP-Sperre trifft Unbeteiligte hinter CGNAT | Befristung per Datenbank-Constraint erzwungen, Präfixsperre höchstens 7 Tage |
| Ein Fehler in der Sperrprüfung sperrt alle aus | fail-open: schlägt die Abfrage technisch fehl, läuft der Aufruf durch und protokolliert den Fehlschlag. Ein Ausfall darf niemandem das Spiel nehmen |
| Erstmeldung ohne Dialog wird beanstandet | Inhaberentscheidung; Auskunft und Widerspruch (17.8) sind trotzdem gebaut, gehasht wird serverseitig, Fristen laufen automatisch. Bei einem Steam-Release verlangt Valve zusätzlich eine Privacy-Policy-URL |
| Ping hängt den Spielstart | drei Sekunden Zeitüberschreitung, Fehler verschluckt, kein Pfad vom Ping zur Spielbarkeit (D-007) |
| Pepper geht verloren | alle Hashes werden unbrauchbar, die Zuordnung ist weg. Der Pepper gehört in dieselbe Sicherung wie `NOVA_RELAY_TOKEN_SECRET` |
| Auftragsverarbeitung Supabase | AV-Vertrag abschliessen, Projektregion EU. Vor Paket A zu klären, nicht danach |

## Fertig wenn

1. `dotnet test tools/Nova.SimRunner.Tests` grün.
2. Eine Sperre auf die eigene Installationskennung verhindert nachweislich den
   Lobby-Beitritt und erklärt es im Klartext.
3. Eine Stichprobe aus `access_log` enthält weder eine rohe IP noch eine rohe
   Kennung.
4. Der Löschjob hat nachweislich gelöscht — eine Zeile älter als die Frist ist
   nach einem Lauf verschwunden.
5. Ein zweiter Rechner erscheint nach dem ersten Start in `installs`, und das
   Spiel startet ohne Netzverbindung unverändert.
6. Die Datenschutzerklärung ist aus dem Hauptmenü erreichbar.
7. Notiert im [GrayboxLog](../GrayboxLog.md).

## Entscheidungen, die dieser Sprint erzeugt

| ID | Inhalt | Wer |
|---|---|---|
| D-095 | Tier-3-Auslöser präzisiert: Veröffentlichung/Geld/Publikum statt jeder personenbezogenen Verarbeitung; Betriebsdaten mit Hashing und Fristen bleiben Tier 2 | Inhaber |
| D-096 | Identitätsmodell: Installations-GUID plus Geräte-Anker, serverseitig gepeppert gehasht; MAC-Adresse ausdrücklich verworfen; IP-Sperren nur befristet | Inhaber (Richtung) / Agent (Ausformung) |
| D-097 | Erstmeldung beim ersten Start ohne Zustimmungsdialog, gestützt auf berechtigtes Interesse; Auskunft und Widerspruch werden gebaut. Verhältnis zu Q-019 („Opt-in Telemetrie") ist mitzuentscheiden — D-097 ersetzt Q-019 für die Erstmeldung | Inhaber |

## Changelog-Notiz

Zugangsprotokoll und Sperrliste für die Lobby: gehashte Herkunfts- und
Installationskennungen mit automatischen Löschfristen, befristete IP- und
Netzsperren, Rate-Limit, Erstmeldung beim ersten Spielstart, Datenschutz-
erklärung und Widerspruchsschalter.

## Versionsrelevanz

`minor`.

## Danach

Der harte Anker. Sobald der Vertriebsweg feststeht (Steam nach D-007, oder
eigener Verkauf mit Lizenzschlüssel), setzt sich eine bannbare Konto-Kennung
neben `install_hash` — Tabellen, Sperrarten und Bedienweg bleiben, nur die
Spalte kommt dazu. Das ist der Punkt, an dem aus Reibung eine Sperre wird, die
hält.
