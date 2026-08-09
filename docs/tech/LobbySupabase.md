# Lobby über Supabase – Vertrag, Schema und Edge Functions

**Version:** 1.0.0 | **Status:** Vertrag festgelegt; Client (`Nova.Networking.Lobby`) und Relay-Seite implementiert und getestet; Supabase-Projekt noch nicht angelegt (Maintainer-Schritt ausserhalb des Repos) | **Verantwortungsbereich:** Netzstrang / Betrieb | **Sprint:** 14

## Zweck

Die Lobby vermittelt zwei Spieler anhand eines kurzen, vorlesbaren Match-Codes,
ohne dass Serveradresse, Match-Token oder Slot-Zuordnung abtelefoniert werden
müssen. Sie läuft über ein **Supabase-Projekt ausserhalb dieses Repositories**
und ist strikt vom Transport getrennt: Der Relay
([RelayServer.md](RelayServer.md)) bleibt der dumme, zustandsarme
Lockstep-Verteiler für genau zwei Peers; die Lobby ist die Vermittlung mit
Persistenz und Ausfallsicherheit.

Architekturentscheidungen: D-092 (Vermittlung: Edge Functions + schlanker
HTTPS-Client + Polling), D-093 (kurzlebige HMAC-Match-Tokens), D-094
(Build-Commit-Exposition) — alle in
[../production/DecisionLog.md](../production/DecisionLog.md).

## Sicherheits- und Scopegrenze

- **Keine Accounts, keine personenbezogenen Daten.** Match-Code,
  Build-Commit, Fraktionswahl und Bereitschaft sind keine Nutzerdaten; damit
  löst die Lobby keinen Governance-Tier 3 aus. Vor dem ersten Feld, das eine
  Person betrifft, ist eine neue D-ID fällig.
- **Der anon-Key und die Projekt-URL sind kein Geheimnis** (sie stehen im
  ausgelieferten Client), werden aber trotzdem nicht eingecheckt — der Client
  bekommt sie über Konfiguration (`lobby-config.json`, gitignort, oder
  Umgebungsvariablen, siehe unten).
- **Der Service-Role-Key liegt ausschliesslich in der Function-Umgebung.**
  Die Tabelle ist per Row-Level-Security für anon/auth vollständig gesperrt
  (keine Policies = deny all); jeder Zugriff läuft über die Edge Functions.
- **Das HMAC-Token-Secret** (`NOVA_RELAY_TOKEN_SECRET`) liegt nur zweimal:
  in der Function-Umgebung und in `/etc/hashkrieg-relay.env` auf dem
  Relay-Host. Es steckt in keinem Client und in keinem Token — Tokens sind
  gegen dieses Secret gemachte HMACs.
- **Der Match-Code ist kein Geheimnis**, das Token schon: Es ist 30 Minuten
  gültig, einmal verwendbar (Single-Use pro Match, über Relay-Resets hinweg)
  und wird dem Client erst beim Übergang in `starting` ausgeliefert.
- **Ehrliche Grenzen:** Der Tag ist 32 Bit kurz — ausreichend, weil der Relay
  Tokens nur im 5-Sekunden-Hello-Fenster prüft und ein Match pro Prozess
  bindet. Ein Relay bedient ein Match gleichzeitig; die Lobby serialisiert
  (unten). Wer den Code kennt, kann beitreten — das ist bei zwei Bekannten
  gewollt, kein Matchmaking-Schutz.

## Ablauf

```text
Ersteller            Lobby (Edge Functions)           Beitretender
   |  create-match(build, fraktion)  |                     |
   | ------------------------------> | mintet Code+Token   |
   |  { code: "K7F-2Q9", slot: 0 }   |                     |
   | <------------------------------ |                     |
   |           (pollt match-status)  | join-match(code, build, fraktion)
   |                                 | <------------------ |
   |                                 | prüft Build-Commit! |
   |                                 | { slot: 1 } ------> |
   |  beide set-ready(true)          |                     |
   | ------------------------------> | state = starting    |
   |  status: starting + tokenHex    | <------------------ |
   |                                 | (tokenHex ausgeliefert)
   v                                 v                     v
        beide Clients verbinden zum Relay mit demselben Token
        (Hello → Offer mit abgeleitetem Seed → Fingerprint → Start)
```

Der Client pollt `match-status` alle ~1,5 Sekunden. Es gibt bewusst kein
Realtime/WebSocket — zwei Spieler, eine Lobby, kein Publikum.

## HTTP-Vertrag

Basis-URL ist die Functions-URL des Projekts,
`https://<projekt>.supabase.co/functions/v1`. Jeder Aufruf ist ein POST mit
JSON-Body und den Headern `apikey: <anon-key>` und
`Authorization: Bearer <anon-key>`. Fehler tragen im Body immer ein
`error`-Feld; `build_mismatch` zusätzlich `creatorBuild` und `yourBuild`.

| Endpunkt | Request | 200 | Fehler |
|---|---|---|---|
| `/create-match` | `{ "buildCommit": string, "faction": 0\|1 }` | `{ "code": "K7F-2Q9", "relayHost": string, "relayPort": int, "slot": 0 }` | 409 `relay_busy` — ein anderes Match ist aktiv |
| `/join-match` | `{ "code": string, "buildCommit": string, "faction": 0\|1 }` | `{ "relayHost": string, "relayPort": int, "slot": 1, "opponentFaction": 0\|1, "opponentBuild": string }` | 404 `unknown_code` · 410 `expired` · 409 `build_mismatch` (mit `creatorBuild`, `yourBuild`) · 409 `match_full` |
| `/match-status` | `{ "code": string, "slot": 0\|1 }` | `{ "state": "open"\|"ready"\|"starting"\|"closed"\|"expired", "slots": [ slot0 \| null, slot1 \| null ], "tokenHex": string \| null }` | 404 `unknown_code` · 410 `expired` |
| `/set-ready` | `{ "code": string, "slot": 0\|1, "ready": bool }` | `{ "state": string }` | 404 · 410 |
| `/leave-match` | `{ "code": string, "slot": 0\|1 }` | `{}` | — (idempotent) |

Konventionen:

- `faction` bildet `FactionId` ab: `0` = Allianz, `1` = Legion
  (`Assets/_Project/Scripts/Simulation/State/FactionId.cs`).
- `slots[i]` ist `{ "faction": int, "ready": bool, "buildCommit": string }`
  oder `null` für den unbesetzten Slot.
- `tokenHex` ist genau bei `state == "starting"` gesetzt (16 Hexzeichen),
  sonst `null`. Der Client parst es mit `RelayProtocol.TryParseMatchToken`
  und legt es als `MatchConfig.MatchToken` an; es wird nie geloggt oder
  persistiert.
- `buildCommit` ist der zur Laufzeit gelesene Build-Commit (D-094,
  `BuildInfo.Commit`; `dev-editor` im Editor). Der Beitritt verweigert
  ungleiche Commits mit 409 — der Client zeigt im Klartext: „Ihr habt
  unterschiedliche Versionen — hol dir Build `<creatorBuild>`."
- Zwei `dev-editor`-Clients gelten als gleicher Build und passieren die
  Prüfung. Das ist der Entwicklungsweg und so gewollt.

### Match-Code

Sechs Zeichen aus dem Alphabet `ABCDEFGHJKMNPQRSTUVWXYZ23456789`
(32 Zeichen, ohne `0`/`O`/`1`/`I`/`L`), dargestellt als `XXX-XXX`. Eingaben
werden case-insensitiv und ohne Bindestrich normalisiert
(`LobbyCode.TryNormalize` in
[`LobbyCode.cs`](../../Assets/_Project/Scripts/Networking/Lobby/LobbyCode.cs)).
32⁶ ≈ 1,07 Milliarden Codes; Kollisionen beim Anlegen ziehen einen erneuten
Wurf nach sich (unten).

## Match-Token (D-093)

Die Lobby mintet pro Match einen 64-bit-Token, der dem Relay im Hello als
gewöhnlicher `u64` vorgelegt wird — das Drahtprotokoll (Version 1) bleibt
unangetastet. Relay und Lobby teilen nur das statische Secret
`NOVA_RELAY_TOKEN_SECRET` (64 Hexzeichen = 32 Bytes); es gibt keinen
Laufzeit-Kanal zwischen ihnen. Bitlayout und Semantik stehen verbindlich in
[`LobbyToken.cs`](../../Assets/_Project/Scripts/Networking/LobbyToken.cs)
und [RelayServer.md](RelayServer.md); hier die Minting-Seite, die die
Function bitgenau spiegeln muss:

```text
bits 63..44  expiry bucket (20 bit) = floor((unixMs - 1767225600000) / 300000)
             (5-Minuten-Scheiben seit 2026-01-01T00:00:00Z; gültig im Fenster
             [aktuell - 5, aktuell] = 30 Minuten)
bits 43..32  match id (12 bit, Zufall)
bits 31.. 0  tag = HMAC-SHA256(secret,
               ASCII "NOVA-LOBBY-TOKEN-V1"  (19 Bytes)
               ‖ bucket als u32 big-endian  (4 Bytes)
               ‖ match id als u16 big-endian (2 Bytes))  → erste 4 Digest-Bytes, big-endian
```

Der Seed des Matches leitet der Relay selbst ab:
`HMAC-SHA256(secret, ASCII "NOVA-LOBBY-SEED-V1" ‖ token als u64 big-endian)`,
erste 8 Digest-Bytes big-endian, ODER 1. Die Function muss den Seed **nicht**
berechnen oder speichern — er folgt deterministisch aus dem Token, und das
Relay-Offer bleibt auf dem Draht autoritativ.

## Datenbank-Schema

```sql
-- Einmalig im Supabase-SQL-Editor auszuführen. Keine Nutzerdaten: Code,
-- Token und Slot-Zustand sind Match-Arbeitsdaten, keine Personenbeziehung.

create table public.matches (
  id            uuid primary key default gen_random_uuid(),
  code          text not null unique,               -- "K7F2Q9" (ohne Bindestrich)
  token_hex     text not null,                      -- 16 Hex, nur serverseitig sichtbar
  relay_host    text not null,
  relay_port    smallint not null,
  state         text not null default 'open'
                check (state in ('open', 'ready', 'starting', 'closed', 'expired')),
  slot0_faction smallint not null check (slot0_faction in (0, 1)),
  slot0_build   text not null,
  slot0_ready   boolean not null default false,
  slot1_faction smallint check (slot1_faction in (0, 1)),
  slot1_build   text,
  slot1_ready   boolean not null default false,
  created_at    timestamptz not null default now(),
  expires_at    timestamptz not null                -- create-match setzt now() + 30 min
);

-- deny all für anon/auth: es gibt bewusst keine Policies. Sämtlicher
-- Zugriff läuft über die Edge Functions mit Service-Role-Key.
alter table public.matches enable row level security;
```

Es gibt bewusst keine Lösch- oder Aufräum-Jobs: Abgelaufene Matches werden
**lazy** beim nächsten Zugriff als `expired` markiert (siehe Function-Code).
Wer die Tabelle irgendwann leeren will, darf alles mit
`expires_at < now() - interval '1 day'` löschen — fachlich irrelevant ist es
ab `expired`.

## Edge Functions (Deno, Referenzquelltext)

Fünf Functions, deren Ordnername den URL-Pfad bildet:
`supabase/functions/{create-match,join-match,match-status,set-ready,leave-match}/index.ts`
plus einem geteilten Modul `supabase/functions/_shared/lobby.ts`. Secrets in
der Function-Umgebung: `NOVA_RELAY_TOKEN_SECRET` (64 Hex, identisch mit dem
Wert auf dem Relay-Host), `NOVA_RELAY_HOST`, `NOVA_RELAY_PORT`.
`SUPABASE_URL` und `SUPABASE_SERVICE_ROLE_KEY` stellt Supabase automatisch
bereit.

### `_shared/lobby.ts`

```ts
import { createClient } from "jsr:@supabase/supabase-js@2";

export const supabase = () =>
  createClient(
    Deno.env.get("SUPABASE_URL")!,
    Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!,
  );

export const json = (status: number, body: unknown) =>
  new Response(JSON.stringify(body), {
    status,
    headers: { "content-type": "application/json" },
  });

// Rohes Deno.serve liefert eine geworfene Response NICHT als HTTP-Antwort
// aus — der Wrapper macht `throw json(404, ...)` in loadMatch benutzbar.
export const serve = (handler: (req: Request) => Promise<Response>) =>
  Deno.serve(async (req) => {
    try {
      return await handler(req);
    } catch (e) {
      if (e instanceof Response) return e;
      console.error(e);
      return json(500, { error: "internal" });
    }
  });

const ALPHABET = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // 32 Zeichen, ohne 0/O/1/I/L

export function mintCode(): string {
  const random = crypto.getRandomValues(new Uint8Array(6));
  let code = "";
  for (const b of random) code += ALPHABET[b % 32];
  return code;
}

export function normalizeCode(input: unknown): string | null {
  if (typeof input !== "string") return null;
  const code = input.trim().toUpperCase().replace("-", "");
  if (code.length !== 6) return null;
  for (const c of code) if (!ALPHABET.includes(c)) return null;
  return code;
}

export function validFaction(v: unknown): v is number {
  return v === 0 || v === 1;
}

export function validBuild(v: unknown): v is string {
  return typeof v === "string" && v.length >= 1 && v.length <= 64;
}

// --- D-093: bitidentisch zu LobbyToken.cs (Assets/_Project/Scripts/Networking) ---

const BUCKET_EPOCH_MS = 1_767_225_600_000; // 2026-01-01T00:00:00Z
const BUCKET_MS = 300_000;
const TAG_CONTEXT = new TextEncoder().encode("NOVA-LOBBY-TOKEN-V1"); // 19 Bytes

function secretBytes(): Uint8Array {
  const hex = Deno.env.get("NOVA_RELAY_TOKEN_SECRET") ?? "";
  if (!/^[0-9a-fA-F]{64}$/.test(hex)) throw new Error("NOVA_RELAY_TOKEN_SECRET unset or malformed");
  const bytes = new Uint8Array(32);
  for (let i = 0; i < 32; i++) bytes[i] = parseInt(hex.slice(2 * i, 2 * i + 2), 16);
  return bytes;
}

export async function mintTokenHex(): Promise<string> {
  const bucket = Math.floor((Date.now() - BUCKET_EPOCH_MS) / BUCKET_MS);
  const matchId = crypto.getRandomValues(new Uint16Array(1))[0] & 0x0fff;
  const input = new Uint8Array(19 + 4 + 2);
  input.set(TAG_CONTEXT, 0);
  const view = new DataView(input.buffer);
  view.setUint32(19, bucket, false);       // big-endian
  view.setUint16(23, matchId, false);      // big-endian
  const key = await crypto.subtle.importKey(
    "raw", secretBytes(), { name: "HMAC", hash: "SHA-256" }, false, ["sign"]);
  const digest = new Uint8Array(await crypto.subtle.sign("HMAC", key, input));
  const tag = BigInt(
    ((digest[0] << 24) | (digest[1] << 16) | (digest[2] << 8) | digest[3]) >>> 0);
  const token = (BigInt(bucket) << 44n) | (BigInt(matchId) << 32n) | tag;
  return token.toString(16).padStart(16, "0");
}

// Liest ein Match per Code und markiert es lazy als abgelaufen.
// Gibt { row } zurück oder wirft eine vorgefertigte Response.
export async function loadMatch(code: string) {
  const { data: row } = await supabase()
    .from("matches").select("*").eq("code", code).maybeSingle();
  if (!row) throw json(404, { error: "unknown_code" });
  const expired = new Date(row.expires_at).getTime() < Date.now();
  if (expired && row.state !== "closed") {
    if (row.state !== "expired") {
      await supabase().from("matches")
        .update({ state: "expired" }).eq("id", row.id);
    }
    throw json(410, { error: "expired" });
  }
  if (row.state === "closed") throw json(410, { error: "expired" });
  if (row.state === "expired") throw json(410, { error: "expired" });
  return { row };
}

export const relay = () => ({
  relayHost: Deno.env.get("NOVA_RELAY_HOST")!,
  relayPort: Number(Deno.env.get("NOVA_RELAY_PORT")!),
});
```

### `create-match/index.ts`

```ts
import { json, mintCode, mintTokenHex, serve, validBuild, validFaction, relay, supabase }
  from "../_shared/lobby.ts";

serve(async (req) => {
  const { buildCommit, faction } = await req.json().catch(() => ({}));
  if (!validBuild(buildCommit) || !validFaction(faction)) {
    return json(400, { error: "bad_request" });
  }
  // Ein Relay = ein Match: jeder nicht abgelaufene aktive Eintrag sperrt.
  const { data: active } = await supabase()
    .from("matches").select("id")
    .in("state", ["open", "ready", "starting"])
    .gt("expires_at", new Date().toISOString()).limit(1);
  if (active && active.length > 0) return json(409, { error: "relay_busy" });

  for (let attempt = 0; attempt < 5; attempt++) {
    const code = mintCode();
    const { error } = await supabase().from("matches").insert({
      code,
      token_hex: await mintTokenHex(),
      relay_host: relay().relayHost,
      relay_port: relay().relayPort,
      slot0_faction: faction,
      slot0_build: buildCommit,
      expires_at: new Date(Date.now() + 30 * 60_000).toISOString(),
    });
    if (!error) {
      return json(200, { code: `${code.slice(0, 3)}-${code.slice(3)}`, ...relay(), slot: 0 });
    }
    if (error.code !== "23505") return json(500, { error: "internal" }); // unique-Kollision -> neu würfeln
  }
  return json(500, { error: "internal" });
});
```

### `join-match/index.ts`

```ts
import { json, loadMatch, normalizeCode, serve, validBuild, validFaction, supabase }
  from "../_shared/lobby.ts";

serve(async (req) => {
  const body = await req.json().catch(() => ({}));
  const code = normalizeCode(body.code);
  if (!code || !validBuild(body.buildCommit) || !validFaction(body.faction)) {
    return json(400, { error: "bad_request" });
  }
  const { row } = await loadMatch(code);
  if (row.slot1_faction !== null) return json(409, { error: "match_full" });
  if (row.slot0_build !== body.buildCommit) {
    return json(409, {
      error: "build_mismatch", creatorBuild: row.slot0_build, yourBuild: body.buildCommit,
    });
  }
  await supabase().from("matches").update({
    slot1_faction: body.faction, slot1_build: body.buildCommit, state: "ready",
  }).eq("id", row.id);
  return json(200, {
    relayHost: row.relay_host, relayPort: row.relay_port, slot: 1,
    opponentFaction: row.slot0_faction, opponentBuild: row.slot0_build,
  });
});
```

### `match-status/index.ts`

```ts
import { json, loadMatch, normalizeCode, serve, supabase } from "../_shared/lobby.ts";

const slotJson = (row: any, n: 0 | 1) =>
  row[`slot${n}_faction`] === null ? null : {
    faction: row[`slot${n}_faction`],
    ready: row[`slot${n}_ready`],
    buildCommit: row[`slot${n}_build`],
  };

serve(async (req) => {
  const body = await req.json().catch(() => ({}));
  const code = normalizeCode(body.code);
  if (!code) return json(400, { error: "bad_request" });
  let { row } = await loadMatch(code);
  // Förderung bei beidseitiger Bereitschaft hier UND in set-ready: zwei
  // gleichzeitige set-ready-Aufrufe können sonst beide „der andere ist nicht
  // bereit" lesen und den Übergang verpassen. Idempotent und rennfrei, weil
  // beide Seiten pollen.
  if (row.state === "ready" && row.slot0_ready && row.slot1_ready) {
    await supabase().from("matches").update({ state: "starting" }).eq("id", row.id);
    row = { ...row, state: "starting" };
  }
  return json(200, {
    state: row.state,
    slots: [slotJson(row, 0), slotJson(row, 1)],
    tokenHex: row.state === "starting" ? row.token_hex : null,
  });
});
```

### `set-ready/index.ts`

```ts
import { json, loadMatch, normalizeCode, serve, supabase } from "../_shared/lobby.ts";

serve(async (req) => {
  const body = await req.json().catch(() => ({}));
  const code = normalizeCode(body.code);
  const slot = body.slot;
  if (!code || (slot !== 0 && slot !== 1) || typeof body.ready !== "boolean") {
    return json(400, { error: "bad_request" });
  }
  const { row } = await loadMatch(code);
  const patch: Record<string, unknown> = { [`slot${slot}_ready`]: body.ready };
  const otherReady = row[`slot${1 - slot}_ready`] as boolean;
  // Beide bereit -> starting: erst jetzt wird das Token an die Clients gegeben.
  if (body.ready && otherReady && row.slot1_faction !== null) patch.state = "starting";
  else if (row.state === "starting") patch.state = "ready"; // Rücknahme vor dem Verbinden
  await supabase().from("matches").update(patch).eq("id", row.id);
  return json(200, { state: (patch.state as string) ?? row.state });
});
```

### `leave-match/index.ts`

```ts
import { json, loadMatch, normalizeCode, serve, supabase } from "../_shared/lobby.ts";

serve(async (req) => {
  const body = await req.json().catch(() => ({}));
  const code = normalizeCode(body.code);
  const slot = body.slot;
  if (!code || (slot !== 0 && slot !== 1)) return json(400, { error: "bad_request" });
  let row;
  try {
    ({ row } = await loadMatch(code));
  } catch (response) {
    return json(200, {}); // weg ist weg — leave ist idempotent
  }
  if (slot === 0) {
    await supabase().from("matches").update({ state: "closed" }).eq("id", row.id);
  } else {
    // Beitretender geht: Slot frei, Ersteller-Bereitschaft zurücksetzen.
    await supabase().from("matches").update({
      slot1_faction: null, slot1_build: null, slot1_ready: false,
      slot0_ready: false, state: "open",
    }).eq("id", row.id);
  }
  return json(200, {});
});
```

## Match-Lebenszyklus und Relay-Belegung

| `state` | Bedeutung | Übergang durch |
|---|---|---|
| `open` | angelegt, wartet auf Beitretenden | `create-match` |
| `ready` | beide Slots belegt, Bereitschaft ausstehend | `join-match`, `leave-match`, Ready-Rücknahme |
| `starting` | beide bereit; Token wird an Clients ausgeliefert | `set-ready` |
| `closed` | Ersteller hat verlassen | `leave-match` |
| `expired` | `expires_at` überschritten (lazy markiert) | jeder Zugriff |

- **Belegungsregel:** `create-match` lehnt mit 409 `relay_busy` ab, solange
  ein Match in `open`/`ready`/`starting` mit `expires_at` in der Zukunft
  existiert. Der Relay kann genau ein Match — die Lobby serialisiert. Es gibt
  kein „das Match läuft noch"-Signal zurück an die Lobby: das 30-Minuten-TTL
  deckt Lobby-Phase plus Verbindungsaufbau; läuft das Match länger, spielt es
  zu Ende, ohne dass die Lobby es weiss. Ein neues Match ist danach sofort
  wieder möglich.
- **TTL:** 30 Minuten ab Anlage. Das Token-Fenster (D-093) ist deckungsgleich;
  ein Match, das die Lobby-Phase übersteht, stirbt sauber, statt den Relay
  dauerhaft zu belegen.
- **Verbindungsabbruch vor `starting`:** Der wartende Peer am Relay hat ein
  eigenes 120-Sekunden-Fenster (RelayServer.md). Geht das Match in dieser Zeit
  nicht in `starting` über, läuft beides sauber aus.

## Client-Konfiguration

Der Client liest URL und anon-Key in dieser Reihenfolge (siehe
`LobbyConfig.cs` unter `Assets/_Project/Scripts/Gameplay/Match/`):

1. Umgebungsvariablen `NOVA_LOBBY_URL` und `NOVA_LOBBY_ANON_KEY`
   (Entwicklung, headless).
2. `Assets/_Project/Resources/lobby-config.json` — **gitignort**, Vorlage:
   [`lobby-config.example.json`](../../Assets/_Project/Resources/lobby-config.example.json).
   Diese Datei in Testbuilds mit ausliefern (vor dem Unity-Build ablegen;
   sie wandert als TextAsset in `Resources`).

Ist nichts konfiguriert, zeigt das Hauptmenü den Hinweis „Lobby nicht
konfiguriert" und führt auf die Direktverbindung aus Sprint 13 — die bleibt
vollständig erhalten.

## Betrieb: einmalige Einrichtung (Maintainer, ausserhalb des Repos)

1. Supabase-Projekt anlegen (kostenloser Plan genügt); keine Auth, kein
   Storage, keine Realtime-Subscriptions aktivieren.
2. SQL aus dem Abschnitt „Datenbank-Schema" im SQL-Editor ausführen.
3. Functions aus diesem Dokument nach `supabase/functions/` legen und mit
   `supabase functions deploy` veröffentlichen (Function-Namen = URL-Pfade).
4. Secrets setzen:
   `supabase secrets set NOVA_RELAY_TOKEN_SECRET=<64 hex> NOVA_RELAY_HOST=<relay-host> NOVA_RELAY_PORT=<port>`.
5. **Denselben** `NOVA_RELAY_TOKEN_SECRET`-Wert in `/etc/hashkrieg-relay.env`
   auf dem Relay-Host eintragen und den Dienst neu starten
   (`systemctl restart hashkrieg-relay`) — siehe
   [RelayServer.md](RelayServer.md), Abschnitt Konfiguration.
6. anon-Key und Functions-URL in `lobby-config.json` eintragen und Builds
   damit versehen.

Kein Schritt davon erzeugt Repo-Inhalte; Secrets und Keys stehen in keiner
eingecheckten Datei.

## Nachweisstand und offene Schritte

Implementiert und getestet sind: der Client (`Nova.Networking.Lobby`, 22
Tests gegen einen In-Prozess-Mock der Functions), die Relay-Token-Seite
(D-093, eigene Tests inkl. Zwei-Client-Handshake über echten TCP-Relay) und
der UI-/Match-Glue. `dotnet test
tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release` ist mit
601/601 grün (Stand 2026-08-09, Branch `feat/sprint-14-lobby`).

Nicht ausgeführt: Supabase-Projekt anlegen, Functions deployen, Relay-Deploy
mit `NOVA_RELAY_TOKEN_SECRET`, und die verhaltensbezogene Abnahme (zwei
Menschen finden über einen Code zueinander und spielen — Fertig-when-Kriterium
des Sprints). Die Referenz-Functions oben sind gegen den hier dokumentierten
Vertrag geschrieben, aber noch nicht gegen ein lebendes Supabase-Projekt
gelaufen; der erste Deploy-Lauf muss sie einmal manuell durchspielen
(`curl`-Proben pro Endpunkt), bevor Builds sie benutzen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-08-09 | Erstfassung: Vertrag, Schema, Edge-Function-Referenzen und Betriebspfad der Lobby (D-092/D-093/D-094, Sprint 14) | Agent (Umsetzung, unter Delegation) |
