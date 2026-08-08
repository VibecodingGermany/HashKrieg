# Sprint: Zu zweit — Netzwerk über eigenen Server, sichtbares Gefecht, Wirtschaftsdruck

**Status:** Gesamt-Sprint offen — Stränge A und B technisch umgesetzt; A8 Stufen 2–4, manuelle B-Gefechtsabnahme und Strang C offen (2026-08-08) | **Vorgänger:** [11_Sprint_Truppenfuehrung.md](11_Sprint_Truppenfuehrung.md) (umgesetzt, D-088) | **Leitsatz:** der zweite Spieler ist ein Mensch

## Ergebnis Strang A (2026-08-07)

> Dieser Block beschreibt den ausgeführten Stand. Der danach erhaltene Text
> ist die ursprüngliche Sprintplanung. Wo der Plan noch `ReplayFile`, UDP-
> Zielbild oder offene A6-Verdrahtung nennt, führt die vom Inhaber freigegebene
> Entscheidung [D-089](../DecisionLog.md). Der historische B-Plan wird durch
> [D-090](../DecisionLog.md), den Ergebnisblock unten und das ausgelagerte
> [12B-Dokument](12B_Sprint_Sichtbares_Gefecht.md) eingeordnet.

| Paket | Ergebnis |
|---|---|
| **A1** | `INetworkTransport`, TCP-Verbindung, Relay-Protokoll und Client-/Server-Lebenszyklus liegen Engine-frei in `Nova.Networking`. Eingehende Command-Records passieren dieselbe rohe `CommandIngress`-Grenze wie der lokale Pfad. |
| **A2** | Der Lockstep-Barrier ist implementiert: `TickComplete` bleibt reiner Transport-Frame. Der Client markiert seine lokale Completion selbst und wartet nicht auf deren Relay-Echo; die Remote-Completion öffnet den Barrier erst nach exakter Servervalidierung und vollständigem Record-Eingang. Bei aktivierter Aufzeichnung persistiert der Relay erst nach bestätigter Completion beider Slots. Ein optionaler `ICommandSubmissionReadiness`-Vertrag lässt `ICommandTransport` unverändert; vor `Running` werden Intents und Session-Aktionen ohne Sequenzverbrauch als `TransportNotReady` abgelehnt. Lokaler Default ist 1, Netz-Default 3; `MatchConfig`/Loopback und Netzprofil erlauben 1–60. |
| **A3** | `Nova.RelayServer` nimmt genau zwei TCP-Peers an, prüft Slot, Tickfolge, Counts, Dedupe und Caps, verteilt Records/Barrier-Frames und simuliert nicht. Statt erfundener Resultcodes im kanonischen `ReplayFile` schreibt er den eigenen Transportrecord `NOVAREC2`: lückenlose Tickframes einschließlich Leerticks, 50-Tick-Checkpoints, terminaler Footer und atomare `.partial`→`.novarec`-Publikation unter einem 64-MiB-Limit. |
| **A4** | `Offer`, Fingerprint und Initialsnapshot sperren den Start auf identischen Seed, Delay, Definitionshash, vollständigen Fingerprint und byteidentischen Initialzustand. Mismatch, Timeout und Protokollverletzung starten kein Match. |
| **A5** | Beide Clients senden alle 50 Ticks ihren State-Hash. Ein Mismatch beendet die Session; die Client-Diagnostik nutzt einen begrenzten On-Disk-Spool, kann mehr als 65.536 Records aufnehmen und publiziert atomar. Ein Desync-Hash muss genau einem Peer entsprechen. |
| **A6** | `MatchConfig`, `MatchBootstrap` und `MatchRunner` sind verdrahtet: Seed, lokaler Slot, Fraktionen, AI-Slots, Delay und Transport kommen aus der Konfiguration; `AiSession` entsteht nur für konfigurierte AI-Slots. Der Barrier sitzt in der Tickschleife, Pause ist im Relay-Match deaktiviert, und `RtsDeviceInput` liest nur `MatchRunner.IsRelayMatch`, `RelayCommandsAllowed` und `RelayEndReason`. Damit ist der Spielpfad technisch angeschlossen, aber noch nicht manuell als Netzwerkpartie abgenommen. |
| **A7** | Ein self-contained `linux-x64`-Publish-Baum, GitHub-Actions-Test/Bundle-Workflow, gehärtete systemd-Unit, root-sicheres Env-Beispiel und transaktionales `deploy.sh bootstrap/deploy/rollback` sind vorhanden. Der Workflow enthält ausdrücklich kein SSH, keine Secrets und kein Deploy. Das vollständige Runbook steht in [../../tech/RelayServer.md](../../tech/RelayServer.md). |
| **A8** | **Stufe 1 nachgewiesen:** zwei echte TCP-Clients liefen über den Relay 10.023 Ticks; Checkpoints lagen alle 50 Ticks vor, beide Live-Zustände blieben identisch und das engine-freie `NOVAREC2`-Playback berechnete denselben Endhash. **Stufen 2–4 offen:** keine Zwei-Fenster-Runde, kein LAN-Match und kein VPS-Match. |

### Verifikation und ehrliche Grenze

- `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release --no-restore --nologo`:
  **547/547 grün, 0 übersprungen, 11 s**. Darin lief
  `TwoClients_OverRealRelay_StayBitIdentical_ThroughTick10023` erneut grün; die
  vollständige `LockstepNetworkTests`-Klasse war zusätzlich 62/62 grün (3 s),
  der gezielte Fail-Closed-/Delay-/Timing-Pass 23/23 grün (156 ms).
- `dotnet build tools/Nova.RelayServer/Nova.RelayServer.csproj -c Release --no-restore --nologo`:
  **0 Warnungen, 0 Fehler**. Konfigurations-,
  Argument-, SIGTERM-, Bundle-, Prüfsummen- und gehärtete Extraktor-Smokes
  liefen lokal. Ein offline erzeugter `linux-x64`-Publish-Baum enthielt 186
  Dateien; das ELF wurde auf dem macOS-arm64-Host nicht gestartet.
- Unity EditMode wurde angestoßen, erreichte die Tests aber nicht: Der
  Lizenzhandshake brach mit `505 Unsupported protocol version 1.18.1` ab.
  Es gibt kein Test-XML und keinen grünen Unity-Testnachweis.
- Nicht gelaufen sind ein echtes Linux-/systemd-/root-Deployment, ein Live-Lauf
  des GitHub-Workflows und jedes VPS-Deployment. Es wurden keine Zugangsdaten
  verwendet.
- Damit sind A1–A7 implementiert und A8 Stufe 1 bewiesen; eine gespielte
  Netzwerkpartie, die Stufen 2–4 und die vollständige Definition of Done des
  Sprintziels bleiben offen. `GrayboxLog.md` wurde mangels gespielter Runde
  nicht fortgeschrieben.

## Ergebnis Strang B (2026-08-08)

> D-090 ist der ausgeführte Vertrag. Der nachfolgende Strang-B-Text bleibt als
> ursprüngliche Sprintplanung erhalten und ist keine zweite Ist-Quelle.

| Paket | Ergebnis |
|---|---|
| **B1** | `VisibleCombatFrameDiffer` leitet Schuss, Treffer, sicheren Tod und eigene fertige mobile Einheiten ausschließlich aus der Fog-Sicht und `TryGetUnit`-Snapshots ab. Gleiche Ticks werden ignoriert, Tick-Rücklauf setzt die Baseline zurück; Zwischen-Cues bei Tick-Sprüngen dürfen verloren gehen. Mehrdeutiges Verschwinden bleibt stumm. |
| **B2** | `CombatEffectController` liefert mit Unity-Bordmitteln Mündungsstoß, höchstens 0,1 s lange kopierte Hitscan-Spur, Trefferstoß und kurze Lichter. Der globale Aktivdeckel ist 64, der Lichtdeckel 8; Überlauf wird verworfen. |
| **B3** | Bestätigte Tode halten den View 0,8 s, trennen Picking/Collider sofort und geben exakt die gebundene Poolidentität frei. Gebäude erhalten Rauch, aber keine persistente Trümmer-/Decal-Fläche. Ein PlayMode-Test deckt Slot-Wiederverwendung ab. |
| **B4** | Genau 35 unveränderte Kenney-CC0-OGGs liegen pack-first unter `Audio/Sfx/Kenney`: 11 Sci-Fi, 11 Impact, 13 Interface. Drei Batch-Sidecars enthalten Einzelhashes; vier Suno-Musikdatensätze benennen ihre verbleibenden Beleglücken ausdrücklich als `incomplete`. |
| **B5** | Der ursprüngliche A/B-Effektschaltertest ist durch einen headless Quellcode-Guard ersetzt. Er scannt Produktionsquellen außerhalb `Simulation/**`, verbietet dort `GetUnitRef(` und nicht erlaubte `.Random`-Memberzugriffe; `RawUnits` bleibt wegen bestehender Altlasten außerhalb des minimalen Vertrags. |
| **Audio** | `UnityAudioService` ist der D-039-konforme Tier-0-One-Shot-Owner. Zwölf `SND_*`-Events laufen über `MIX_Master`, 30 One-Shot-/24 räumliche Stimmen, 3–4 Instanzen je Schlüssel, atomare Layer, Prioritäts-Stealing und den vorhandenen SFX-Regler. `ALR_BaseUnderAttack` bleibt Tier 1. Die beiden Legacy-Musikcontroller sind als Übergangsausnahme mit zwei reservierten Stimmen dokumentiert. |

### Verifikation und ehrliche Grenze Strang B

- SimRunner/Quellgrenzen: **549/549 grün**, 0 übersprungen.
- Unity EditMode: **521/521 grün**. Unity PlayMode: **8/9**; der neue
  `CombatDeathViewHoldTests.RecycledSlotCannotReuseTheHeldCorpseView` ist grün,
  allein der bestehende headless `BarracksSpawnDiagnosisTests` scheitert an
  `RenderTexture.Create`.
- Unity-Authoring validierte 35 Importer, zwölf Events, Mixerbusse/-parameter
  sowie genau einen Listener, Service, SFX-Bridge und Effektcontroller in der
  Bootstrap-Szene.
- Ein frischer universeller macOS-Build (arm64 und x86_64) wurde erfolgreich
  erstellt. Die manuelle Sicht-/Gegenhörabnahme mit einem dichten Gefecht,
  insbesondere etwa sechzig feuernden Einheiten, ist **nicht** als bestanden
  behauptet.
- Vollständige Abweichungen, Quellen und Restprüfungen stehen in
  [D-090](../DecisionLog.md), im [ScopeLedger](../ScopeLedger.md) und im
  [12B-Umsetzungsreport](../../../reports/v8.6.0/sprint-12-strang-b/02-umsetzungsreport.md).

Der größte Sprint dieser Reihe, bewusst. Er trägt drei Stränge, weil sie sich
nicht ins Gehege kommen: **A** baut neuen Code in eine heute leere Assembly und
einen neuen Serverprozess, **B** fasst ausschließlich die Präsentation an, **C**
ausschließlich die Simulation. Ihre Schreibbereiche sind disjunkt — genau die
Bedingung, unter der die Hot-File-Regel Parallelität erlaubt.

---

## 0. Vorbedingungen — vor der ersten Zeile Code

| # | Bedingung | Warum |
|---|---|---|
| 0.1 | **Sprint 11 ist committet und der Arbeitsbaum sauber** | Beim Schreiben dieses Plans liegen 30 geänderte Dateien aus D-088 uncommittet im Baum. Ein Sprint dieser Größe darf nicht auf ungesicherter Arbeit aufsetzen |
| 0.2 | **.NET-8-SDK 8.0.318 installiert** | `global.json` pinnt mit `rollForward: disable`, installiert ist nur `10.0.302`. Ohne dieses SDK läuft weder `Nova.SimRunner.Tests` noch der neue Serverprozess. **Für Strang A ist das ein harter Blocker, kein Komfortproblem** — der Zwei-Klienten-Nachweis lebt in dieser Spur |
| 0.3 | **Die gespielte Runde zu D-088** | Governance-Tier 1 verlangt sie, und sie ist offen. Sie liefert nebenbei den Befund, gegen den Strang C geschrieben wird |
| 0.4 | **Dauerregel aus Sprint 09 §2 gilt weiter** | `AssetMappingRegistry.asset`, `Packages/manifest.json` + `packages-lock.json`, `DefaultVolumeProfile.asset` gehören in keinen Commit dieser Reihe. Anmerkung: `manifest.json` trägt heute `com.unity.ai.assistant` (Prerelease) — vor dem ersten Commit prüfen, ob das versehentlich hereingerutscht ist. Ebenso liegen sieben Screenshots (`dashboard-1920.png`, `uebersicht-*.png`, `diagnose-*.png`, `massnahmen-*.png`) und `.playwright-mcp/` unversioniert im Repo-Wurzelverzeichnis |

---

## 1. Wo wir stehen — geprüft am Code, nicht aus dem Masterplan übernommen

Der Masterplan beschreibt `main` @ `15dfe73`, also den Stand vor vier Sprints.
Diese Tabelle ist am heutigen Arbeitsbaum nachgesehen.

| Befund | Beleg |
|---|---|
| **Der Netzwerkpfad ist architektonisch vorbereitet, aber leer** | `Nova.Networking.asmdef` existiert (`noEngineReferences: true`, referenziert `Nova.Core` + `Nova.Simulation`) und enthält **keine einzige `.cs`-Datei** |
| **Der Transport-Vertrag existiert und ist einseitig** | `ICommandTransport` kennt genau `Send(byte[] recordBytes)`. Es gibt keinen Empfangspfad, keinen Verbindungslebenszyklus, keine Peer-Identität |
| **Die Gegenseite des Vertrags ist fertig** | `CommandIngress.TryAcceptRecordBytes(...)` nimmt rohe kanonische Bytes und validiert sie strukturell — laut eigenem Kommentar ausdrücklich, damit „der lokale MS-1-Pfad dieselbe Vertrauensgrenze übt wie ein späterer Netzwerk-Transport" |
| **Die KI läuft bereits über diese Grenze** | `AiPeerCommandTransport` (D-086) fährt die KI byte-gleich zum Netzwerkpfad über eine eigene `CommandIngress`. Der Beweis, dass die Architektur trägt, ist schon erbracht |
| **Es gibt keinen Lockstep-Barrier** | `MatchRunner.StepFixedTick()` ruft `Ingress.SealTickBatch(nextTick)` und tickt sofort weiter — versiegelt also, was da ist, statt zu warten, bis alle Slots geliefert haben |
| **Der Input-Delay ist 1** | `MatchSession(localSlot: 0, activeSlots: {0,1}, inputDelayTicks: 1)`. Bei 10 Hz sind das 100 ms Vorlauf — für Loopback richtig, für eine Internetstrecke zu wenig |
| **Der lokale Slot ist hartverdrahtet** | `MatchRunner` erzeugt `Session` mit `localSlot: 0` und zusätzlich eine `AiSession` mit `localSlot: 1`. Slot 1 ist heute strukturell die KI |
| **Desync-Werkzeug ist vorhanden** | `SimulationKernel.CalculateStateHash()` (NOVA_STATE_V1) und `MatchFingerprint` (486 Zeilen: Schema-Versionen, `NumericModelId = Q16_16_V1`, `TicksPerSecond = 10`, `PrngId = XorShift128PlusV1`) |
| **Der headless-Prozess ist Präzedenz** | `tools/Nova.SimRunner/Nova.SimRunner.csproj` kompiliert `Core` + `Simulation` als `net8.0`-Exe. Ein Serverprozess ist derselbe Handgriff, kein Neuland |
| **Es gibt keinerlei Effektschicht** | Kein `ParticleSystem`, kein `VisualEffect`, kein `LineRenderer` im gesamten Produktionscode (einziger Treffer: `RallyFlagView`). Kein `.anim`, kein `.controller` im ganzen Projekt |
| **Die Präsentation liest ausschließlich pollend** | Es gibt keinen Sim→View-Ereigniskanal. Ein Schuss passiert innerhalb eines Ticks und ist im nächsten Frame nicht mehr am Zustand ablesbar |
| **Die Felder sind unerschöpflich** | `MatchBootstrap.FieldReserveAE = 2_000_000` bei `EconomySystem.HarvestRateAE = 2`/Tick ≈ **14 Stunden je Feld**. Zwei Felder gesamt, eins je Slot |
| **Das Lager ist eine Attrappe** | `PlayerEconomyState.AddCredits` deckelt nichts |
| **Das Radar ist eine Attrappe** | `FogOfWarSystem` erzeugt Pings aus `observer.SightRadius * RadarRadiusMultiplier` **jeder eigenen Einheit** |
| **Low Power ist zu einem Viertel da** | `ProductionSpeedMultiplierQ16` ist der einzige Effekt |
| **Reparatur kostet nichts** | `ProcessRepairOrders` addiert `RepairRateHpPerTick`, ohne je `TrySpendCredits` aufzurufen |

**Die gute Nachricht in einem Satz:** Der Netzwerkstrang ist *Verdrahtung auf
vorbereitetem Grund*, kein Umbau. Das Forschungsdokument
[Multiplayer_Simulation.md](../../research/Multiplayer_Simulation.md) hat 2026-07-21
deterministisches Lockstep über einem autoritativen Input-Relay empfohlen, und
die fünf Architekturregeln aus dessen §7 sind seither eingehalten worden.
Dieser Sprint kassiert die Rendite.

---

# Strang A — Zwei Menschen, ein Match

**Ziel:** Der Inhaber und eine zweite Person spielen über den eigenen VPS eine
vollständige Runde gegeneinander, ohne Desync.

## A1 · Der Transport bekommt eine Gegenrichtung

`ICommandTransport` bleibt unangetastet — er ist die Naht zur Ingress und hat
sich bewährt. Neu in `Nova.Networking`:

```
INetworkTransport : ICommandTransport
    Connect(endpoint, matchToken)   Disconnect()
    Poll()                          -> pumpt eingegangene Frames
    ConnectionState, RoundTripTicks, LastError
```

Eingehende Datensatz-Frames werden über `TryAcceptRecordBytes` in die **lokale**
Ingress gereicht — exakt der Weg, den `AiPeerCommandTransport` heute schon geht.
Fremde Records durchlaufen damit dieselbe strukturelle Validierung wie eigene.

**Assembly-Disziplin:** `Nova.Networking` behält `noEngineReferences: true`.
Kein `UnityEngine`-Typ im Netzwerkcode — sonst ist der Serverprozess nicht mehr
baubar und die headless-Tests fallen aus.

## A2 · Der Lockstep-Barrier — das Herzstück

Heute versiegelt `StepFixedTick()` den Batch für `CurrentTick + 1` und tickt.
Über eine Netzstrecke ist dieser Batch fast immer unvollständig, und
unvollständig heißt: **die beiden Spielstände laufen auseinander.**

Der Umbau in drei Teilen:

**(a) Input-Delay konfigurierbar, MS-1-Netzwert 3.** `inputDelayTicks` wandert
aus der Konstruktorkonstante in die Matchkonfiguration. 3 Ticks bei 10 Hz sind
300 ms Vorlauf — der vom Forschungsdokument genannte RTS-übliche Korridor liegt
bei 2–6 Ticks. Der lokale Einzelspielerpfad behält 1.

> **Vertragsfolge, nicht übersehen:** Der Klassenkommentar von `MatchSession`
> sagt, `InputDelayTicks` sei Teil des Match-Fingerprints. Beim Umbau
> **verifizieren**: wenn ja, sind unter Delay 1 aufgezeichnete Replays nicht mehr
> gegen ein Delay-3-Match validierbar. Das ist eine bewusste, zu
> protokollierende Vertragsänderung — keine, die stillschweigend passiert.

**(b) Die Tickbestätigung — und warum sie *kein* CommandRecord wird.** Ein
Klient darf Tick X erst ausführen, wenn **jeder aktive Slot erklärt hat, dass
sein Input für X vollständig ist.** Ohne Befehle gibt es aber nichts zu senden —
also braucht es eine Leermeldung je Slot und Tick.

Der naheliegende Weg wäre ein neuer `CommandKind`. **Nicht tun.** Sprint 11 hat
Attack-Move genau deshalb verworfen: das v1-Register ist eingefroren und durch
einen Golden-Bytes-Test gesichert. Stattdessen ist die Bestätigung ein
**Transport-Frame**:

```
TickComplete { slot : u8, targetTick : u32, recordCount : u16 }
```

Sie erreicht die Ingress nie, steht in keinem Replay, berührt den State-Hash
nicht und lässt `CommandSchemaVersionV1` unverändert. Ein Klient führt Tick X
aus, sobald für jeden aktiven Slot ein `TickComplete(X)` vorliegt **und** die
angekündigte Anzahl Records eingetroffen ist.

**(c) Stall statt Divergenz.** Fehlt eine Bestätigung, hält die Simulation an —
sichtbar („Warte auf Spieler 2 … 1,4 s"), nicht als Standbild. Es wird nichts
geschätzt, nichts vorweggenommen, nichts verworfen. Ein Lockstep-Klient, der
lieber weiterläuft als zu warten, ist ein kaputter Lockstep-Klient. Nach einem
konfigurierten Zeitfenster (Vorschlag 30 s) gilt der Peer als verloren und das
Match endet geordnet mit Meldung.

## A3 · Der Relay-Server

Neues Projekt `tools/Nova.RelayServer` nach dem Muster von `Nova.SimRunner`:
`net8.0`, `OutputType Exe`, kompiliert `Core` + `Simulation` mit hinein.

Aufgaben, gemäß §5/§6 des Forschungsdokuments („autoritativer Input-Relay"):

1. **Verbindungen annehmen und Slots vergeben** (MS-1: genau zwei).
2. **Jeden Record strukturell validieren** über die vorhandene
   `CommandPayloadValidation` und **jeden Record verwerfen, dessen `PlayerSlot`
   nicht dem des Absenders entspricht.** Das ist der gesamte Autoritätsanspruch
   dieser Stufe — und er verhindert die billigste Form von Betrug.
3. **Weiterleiten** an alle Peers, `TickComplete` eingeschlossen.
4. **State-Hashes einsammeln und vergleichen** (siehe A5).
5. **Mitschreiben:** der Record-Strom des Matches als Replay-Datei auf dem
   Server. Das Format existiert (`ReplayRecorder`/`ReplayFile`) — es kostet fast
   nichts und liefert das Werkzeug, mit dem jeder Desync später nachgespielt wird.

**Transport: TCP, nicht UDP.** Begründung, ausdrücklich festgehalten: Lockstep
braucht *zuverlässige, geordnete* Zustellung — genau das, was TCP liefert und
was man über UDP mühsam nachbaut. Bei zwei Spielern, 10 Hz und Records von
20–60 Byte ist Head-of-Line-Blocking kein reales Problem, und wir sparen eine
ganze Fehlerklasse. UDP/RUDP ist die spätere Optimierung, wenn Spielerzahl oder
Latenz es erzwingen — nicht heute.

## A4 · Handshake und Fingerprint-Sperre

Vor Tick 0 tauschen beide Klienten ihren `MatchFingerprint` (Schema-Versionen,
`NumericModelId`, `TicksPerSecond`, `PrngId`) plus den Definitions-Hash
(`DefinitionsHash64`) und den Seed. **Bei Abweichung startet das Match nicht** —
mit einer lesbaren Meldung, welches Feld abweicht.

Das ist der wertvollste Einzelposten des ganzen Strangs. Ohne ihn desynct ein
Match mit zwei verschieden alten Builds nach vierzig Minuten, und niemand weiß
warum. `MatchFingerprint` existiert seit Sprint 3 genau für diesen Moment.

## A5 · Desync-Erkennung mit Erstbefund

Alle 50 Ticks (5 s) sendet jeder Klient `SimulationKernel.CalculateStateHash()`
mit Ticknummer. Der Server vergleicht. Bei Abweichung:

- beide Klienten halten an und melden „Desync bei Tick N" statt weiterzuspielen;
- beide schreiben einen vollständigen Zustands-Snapshot plus ihren
  Record-Strom in eine Diagnosedatei;
- der Server behält seinen Mitschnitt.

Damit ist jeder Desync reproduzierbar, statt eine Anekdote zu sein. Das
Forschungsdokument nennt genau dieses Werkzeug „Pflicht-Tooling, kein
Nice-to-have".

## A6 · Matchkonfiguration — der Slot wird beweglich

Heute ist Slot 0 der Mensch (Allianz), Slot 1 die KI (Legion). Für ein
Zwei-Menschen-Match muss das konfigurierbar werden:

```
MatchConfig { seed, localSlot, factionPerSlot, aiSlots, inputDelayTicks, transport }
```

- `MatchRunner` erzeugt `AiSession` **nur** für Slots in `aiSlots`.
- `MatchBootstrap` liest Faktionen aus der Konfiguration statt sie festzusetzen.
- Der Seed kommt vom Server, damit beide Klienten identisch starten.

**Nebenwirkung, bewusst nicht ausgebaut:** Damit fällt die strukturelle Hürde
für die Fraktionswahl. Die Eingabeschicht bleibt in diesem Sprint trotzdem auf
Allianz-DefIds hartverdrahtet — Fraktionswahl ist Sprint 13 und hätte hier nur
Fläche gekostet.

## A7 · Betrieb auf dem VPS

- **Ein Dienst, ein unprivilegierter Benutzer, ein Port**, systemd-Unit mit
  `Restart=on-failure`, Journal-Logging.
- **Konfiguration ausschließlich über Umgebungsvariablen** (Port, Slotzahl,
  Match-Token, Logpfad). Keine Zugangsdaten im Repository, kein Token im
  Klartext in der Unit-Datei.
- **Match-Token verpflichtend.** Ein offener Relay-Port im Internet ohne
  gemeinsames Geheimnis heißt: jeder kann sich in die Runde setzen. Ein
  gemeinsamer Match-Code je Runde reicht für diese Stufe vollständig.
- **Nur der Relay-Port offen**, Firewall-Regel dokumentiert.
- **`docs/tech/RelayServer.md`** hält Betrieb, Ports, Umgebungsvariablen,
  Deploy-Schritt und Rollback fest — sonst weiß in vier Wochen niemand mehr, wie
  die Kiste läuft.

> Das Ausrollen auf den VPS macht der Inhaber (oder gibt es ausdrücklich frei).
> Der Sprint liefert Dienst, Unit-Datei, Deploy-Skript und Runbook — **er fasst
> keine Zugangsdaten an und deployt nicht eigenmächtig.**

## A8 · Nachweis, in vier Stufen

| Stufe | Was | Wo |
|---|---|---|
| 1 | **Headless-Soak:** zwei `MatchSession` + Relay im selben Prozess, 10.000 Ticks, beide State-Hashes je 50 Ticks identisch, Endhash gleich | `Nova.SimRunner.Tests` — CI-fähig, das ist die belastbare Stufe |
| 2 | **Zwei Fenster, eine Maschine**, über Loopback, eine vollständige Runde | Unity, von Hand |
| 3 | **Zwei Maschinen im LAN** | von Hand |
| 4 | **Zwei Maschinen über den VPS**, eine vollständige Runde bis zum Ergebnisbildschirm | der eigentliche Sprintzweck |

Stufe 1 ist die einzige, die dauerhaft grün bleiben muss. Stufen 2–4 werden im
[GrayboxLog](../GrayboxLog.md) protokolliert.

---

# Strang B — Man sieht, dass geschossen wird

**Ziel:** Ein Gefecht ist als Gefecht erkennbar. Heute stehen Einheiten
voreinander und Lebensbalken sinken lautlos.

Reine Präsentation. Kein Tick, kein Hash, kein Baseline-Risiko.

## B1 · Der Ereigniskanal — Zustands-Differ, nicht Sim-Events

Die technische Vorbedingung, und zugleich Masterplan 4.2. Die Simulation darf
**keine** Events feuern — das ist der naheliegende Pfusch und er gefährdet den
Determinismus. Stattdessen vergleicht die Präsentation zwei aufeinanderfolgende
Simulationszustände:

| Ereignis | Ableitung aus `UnitState` |
|---|---|
| **Schuss abgefeuert** | `WeaponCooldownTicks` springt von niedrig auf hoch (steigende Flanke) bei gültigem `AttackTarget` |
| **Treffer** | `CurrentHealth` gesunken |
| **Tod** | `IsActive` von `true` auf `false`, oder Entity nicht mehr gültig |

**Genau einmal pro Simulationstick abtasten, nicht pro Frame.** Die View läuft
mit 60 fps, die Simulation mit 10 Hz — ein Differ pro Frame meldet dasselbe
Ereignis sechsmal. `MatchRunner` weiß, wann ein Tick gelaufen ist; dort hängt
der Abgriff.

Dieser Kanal ist gleichzeitig die Vorarbeit für Audio Tier 0 (Masterplan 4.4).
Er wird hier einmal gebaut und später nur noch abonniert.

## B2 · Der Schuss

- **Mündungsfeuer** am Schützen: kurzer Partikelstoß plus ein Lichtimpuls über
  2–3 Frames.
- **Leuchtspur** zum Ziel: Die heutigen Waffen sind Hitscan (Schaden fällt im
  selben Tick an). Eine `LineRenderer`-Spur, die über ~0,1 s ausblendet, liest
  sich richtig und kostet fast nichts. Ein fliegendes Projektil wäre eine Lüge
  über das, was die Simulation tut.
- **Einschlag** am Ziel: Funken- oder Staubstoß, je nach `DamageType`.

**Technik:** Unity-Bordmittel. `com.unity.modules.particlesystem` ist bereits im
Manifest — **kein neues Paket**. VFX Graph wäre ein zusätzliches Paket für
Effekte dieser Größenordnung und lohnt nicht.

**Gepoolt und gedeckelt.** Ein Gefecht mit sechzig Einheiten feuert bei 10 Hz
mehrere hundert Schüsse pro Sekunde. Feste Obergrenze gleichzeitiger Effekte
(Vorschlag 64), Wiederverwendung aus einem Pool, kein `Instantiate` im
laufenden Gefecht. Bei Überlauf werden Effekte verworfen, nicht aufgestaut.

## B3 · Der Tod

**Ehrlich zum Materialstand:** Das Projekt enthält **kein `.anim` und keinen
`.controller`**, und die 34 Modelle sind statische Meshes ohne Rig. Eine
Todes-*Animation* im Sinne von Skelettanimation ist in diesem Sprint nicht
baubar, und niemand sollte sie versprechen. Was geht, wirkt trotzdem:

- **Einheit:** kurzer Zerlegungsstoß, Absacken und Ausblenden über ~0,8 s,
  danach zurück in den Pool.
- **Gebäude:** Rauchsäule, Absacken, eine liegenbleibende Trümmer-Decal-Fläche.
  Ein zerstörtes Gebäude, das einfach verschwindet, nimmt dem Angriff seine
  Quittung.
- **Aufräumen ist Pflicht.** `UnitViewManager` poolt und recycelt Views; der
  Todeseffekt muss den View so lange festhalten, wie er läuft, und ihn danach
  sicher freigeben — sonst zeigt der wiederverwendete View die Leiche der
  vorigen Einheit.

**Vorbereitet, nicht gebaut:** Der Ereigniskanal aus B1 und die Effekt-Schnittstelle
werden so geschnitten, dass eine spätere Rig-Lieferung (Masterplan Phase 5 P2,
„Infanterie-Rigs") nur noch einen Animator hinter dieselbe Ereignisquelle hängt.

## B4 · Texturen und Lizenz

**Erste Wahl: prozedural.** Ein radialer Farbverlauf zur Laufzeit erzeugt,
reicht für Mündungsfeuer, Funken und Rauch vollständig und importiert gar
nichts. Kein Lizenzvorgang, kein Provenienzeintrag, kein Repo-Wachstum.

**Zweite Wahl, wenn es besser aussehen soll: Kenney Particle Pack.**
[docs/assets/Licenses.md](../../assets/Licenses.md) §1 und §6 führen Kenney
ausdrücklich als CC0 und **„vollständig öffentlich im GitHub-Repo erlaubt"** —
die Zeile nennt die Quelle, nicht die Asset-Kategorie, gilt also auch für
Partikeltexturen. Bei Import: Zeile im Import-Protokoll §3 und `PROVENANCE.json`
wie bei jedem anderen Asset.

**Gesperrt bleibt alles andere.** Regel 6 ist Default-Deny. Kein Asset-Store-Fund
„weil es kostenlos ist" ohne dokumentierte Einzelprüfung.

## B5 · Die Determinismus-Wache

Ein Nachweis, ein Test: **State-Hash mit eingeschalteten Effekten muss byte-gleich
sein zum State-Hash mit ausgeschalteten Effekten.** Effekte bekommen dafür einen
Schalter in `GameSettings` (der ohnehin nützlich ist, wenn die Grafikkarte
schwächelt). `UnityEngine.Random` ist in der Effektschicht erlaubt — aber
ausschließlich dort, und der Test beweist es.

---

# Strang C — Die Runde bekommt einen Bogen

**Ziel:** Aetherium ist knapp, und kein Gebäude kostet Geld, ohne etwas zu tun.
Masterplan 1.3 + 3.1 + 3.2 + 3.3 + 3.5.

Solange ein Feld vierzehn Stunden hält, gibt es keinen Grund zu expandieren,
keinen Grund um die Kartenmitte zu kämpfen und keinen Grund, dass eine Runde
endet — und das Spiel widerlegt die zentrale Aussage seiner eigenen Fiktion.
**Für ein Match zwischen zwei Menschen ist das der Unterschied zwischen einer
Vorführung und einer Partie.**

## C1 · Knappheit

| Was | Heute | Ziel |
|---|---|---|
| Feldreserve | 2.000.000 AE | Manifestwerte **9.000 / 15.000 AE** |
| Feldanzahl | 2 (je Slot eins) | **5** — 2 Start, 2 Expansion, 1 umkämpftes Zentrum |
| Ernterate | 2 AE/Tick, als Provisorium markiert | gegen die Zielkurve kalibriert |

Symmetrie ist bei fünf Feldern kein Detail, sondern Pflicht: Beide Startpositionen
müssen gleich weit zu Expansion und Zentrum liegen, sonst ist das erste
Mensch-gegen-Mensch-Match schon durch die Karte entschieden.

## C2 · Das Lager wird ein Gebäude

AE-Obergrenze im `EconomySystem`: HQ 2.000 AE Basis, +2.000 je Lager,
Überschuss verfällt, 25 % Verlust bei Zerstörung (D-024). `AddCredits` deckelt
heute nicht — deshalb gibt es keinen Ausgabenanreiz und keinen Grund, ein Lager
zu bauen.

## C3 · Das Radar wird ein Gebäude

Radar-Abdeckung vom **Gebäude** ableiten statt von jeder eigenen Einheit.
`FogOfWarSystem` multipliziert heute den Sichtradius jeder Einheit — das Gebäude
trägt exakt nichts bei und ist eine reine Kostenfalle.

## C4 · Low Power wird eine Waffe

Die feste vierstufige Abschaltreihenfolge, bei der **Radar und Verteidigung
immer zuerst fallen**. Heute existiert nur der Tempo-Malus. Erst damit wird ein
Angriff auf das gegnerische Kraftwerk taktisch sinnvoll — und im
Zwei-Menschen-Match ist das der erste echte strategische Zug, den es zu finden gibt.

## C5 · Die Bauvoraussetzungs-Kette

`SimBuildingDefinition.PrerequisiteRole` ist ein **einzelnes** Feld; das Design
nennt für sechs von neun Rollen Mehrfachvoraussetzungen. Eine Bitmaske über
`UnitRole` reicht.

> **Vorsicht:** `PrerequisiteRole` geht in `DefinitionsHash64` ein
> (`SimDefinitions.cs`, `hash.WriteUInt8((byte)def.PrerequisiteRole)`). Eine
> Formatänderung ändert den Definitions-Hash — und der ist ab Strang A die
> Sperre, die zwei Klienten zusammenhält. **Dieses Paket und der
> Netzwerk-Handshake müssen zueinander passen; wer C5 nach dem ersten
> VPS-Match nachschiebt, muss beide Seiten gleichzeitig aktualisieren.**

## C6 · Platzierungsregeln und Reparaturkosten

- Bau-Einflussradius (8 Zellen um HQ / Lager / Kraftwerk), Mindestabstand zu
  Aetherium-Feldern, Gebäudeabstand. Heute prüft der Code nur „innerhalb der
  Karte" und „Zelle frei".
- Reparatur kostet 30 % des Neupreises statt nichts. Kostenlose Reparatur
  entwertet jeden Angriff auf Gebäude, sobald ein Builder in der Basis steht.

---

## Reihenfolge und Abwurfliste

```
A1 → A2 → A3 → A4 → A5 → A6 → A7 → A8        (Sprintzweck, nicht verhandelbar)
        ║
        ╠══ B1 → B2 → B3 → B4 → B5           (Präsentation, jederzeit dazwischen)
        ║
        ╚══ C1 → C2 → C3 → C4 → C5 → C6      (Simulation, ein Baseline-Neusatz)
```

- **A und C nie gleichzeitig anfassen.** C ändert den Simulationszustand; ein
  Desync-Befund aus A ist wertlos, wenn sich gleichzeitig die Simulation bewegt.
  Erst A bis mindestens A8/Stufe 1 grün, dann C, dann A8/Stufe 4 wiederholen.
- **B läuft überall dazwischen** — es fasst keine Datei an, die A oder C anfassen.
- **Wenn die Zeit nicht reicht, fällt in dieser Reihenfolge:** C6, dann C5, dann
  C4. Jeder Abwurf kommt mit Begründung in den
  [ScopeLedger](../ScopeLedger.md). Skips sind erlaubt, **stille Skips nicht.**

## Bewusst nicht in diesem Sprint

| Punkt | Warum |
|---|---|
| **Fraktionswahl** | A6 macht sie strukturell möglich, aber die Eingabeschicht ist auf Allianz-DefIds 1–17 hartverdrahtet. Eigener Sprint 13, zusammen mit der Legion-Waffenidentität (Salven-/Flächenschaden) — sonst spielen sich beide Fraktionen im Gefecht ohnehin gleich |
| **Reconnect** | Der teuerste Nachteil des Lockstep-Modells. Braucht Snapshot + Fast-Forward. Für zwei bekannte Spieler an einem Abend ist „Match neu starten" die richtige Antwort |
| **Lobby, Matchmaking, Accounts** | Ein gemeinsamer Match-Code reicht. Alles darüber ist Backend-Arbeit ohne Spielwert |
| **Mehr als zwei Spieler** | Die Slot-Struktur trägt acht, der Server soll zwei können. Aufmachen, wenn zwei laufen |
| **UDP/RUDP, Rollback, Prediction** | Siehe A3. Erst messen, dann optimieren |
| **Audio Tier 0** | Historischer Planstand; durch D-090 ersetzt. Zwölf Events und 35 Kenney-CC0-OGGs sind technisch umgesetzt, ohne neue §1-Lizenzzeile; `ALR_BaseUnderAttack` bleibt Tier 1 |
| **Skelettanimation** | Kein Rig, kein `.anim` im Projekt. Siehe B3 |
| **Attack-Move** | Unverändert aus Sprint 11: neuer `CommandKind` gegen das eingefrorene v1-Register. Und im Netzwerkkontext ändert er zusätzlich den Fingerprint |

## Der ehrliche Preis

**Strang A ändert einen Vertrag.** Der Input-Delay wandert von 1 auf 3, und wenn
er im Fingerprint steht, sind alte Replays nicht mehr gegen neue Matches
validierbar. Bewusst zahlen, im DecisionLog protokollieren.

**Strang C setzt die Baselines neu**, wie Sprint 09 und 11 vor ihm: Fingerprint,
Replay, Snapshot-Hash, Öffnungs-Hashes. Das ist kein Defekt, das ist der Zweck
dieser Tests. Einmal, dokumentiert, mit alten und neuen Werten im Ergebnisblock.

**Strang B kostet nichts am Vertrag** und muss es beweisen (B5).

**Der Serverprozess ist neue Betriebsfläche.** Ein Dienst im Internet ist etwas
anderes als ein Spiel auf dem eigenen Rechner: Er braucht ein Token, eine
Firewallregel, einen unprivilegierten Benutzer und ein Runbook. Das ist kein
Papierkram, das ist der Unterschied zwischen einem Server und einem offenen Port.

## Determinismus — unverändert nicht verhandelbar

Alles aus Sprint 11 gilt weiter, und für das Netzwerk kommt hinzu:

- **Kein Klient rechnet weiter, solange ein Slot fehlt.** Stall ist richtig,
  Weiterlaufen ist ein Fehler.
- **Kein Effekt, keine UI, kein Netzwerkcode schreibt je in den Simulationszustand.**
- **`TickComplete` ist ein Transport-Frame**, nie ein `CommandRecord`. Das
  v1-Register bleibt eingefroren.
- **Der Fingerprint-Vergleich vor Tick 0 ist verpflichtend**, nicht optional.

## Fertig wenn

1. Ich starte den Relay auf dem VPS, gebe einer zweiten Person den Match-Code,
   und **wir spielen eine vollständige Runde gegeneinander** — bauen, ernten,
   kämpfen, Ergebnisbildschirm. Keine Desync-Meldung, kein Auseinanderlaufen.
2. Wenn eine Leitung kurz hängt, **wartet mein Spiel sichtbar** und läuft danach
   weiter, statt sich still vom Gegner wegzuentwickeln.
3. Starte ich gegen einen anderen Build, **beginnt das Match gar nicht erst** und
   sagt mir warum.
4. Wenn meine Panzer schießen, **sehe ich, dass sie schießen** — Mündungsfeuer,
   Leuchtspur, Einschlag. Was stirbt, sackt zusammen und lässt Trümmer zurück,
   statt zu verschwinden.
5. Ein Aetherium-Feld **geht mir während der Runde aus**, und ich muss mich um
   das mittlere Feld mit dem Gegner streiten.
6. Ich baue ein Lager, **weil mein Konto sonst überläuft.** Ich baue ein Radar
   und **sehe mehr als vorher.** Ich schieße das gegnerische Kraftwerk kaputt und
   **seine Verteidigung geht aus.**

---

## Prompt für Kimi

```text
AUFGABE: Zu zweit — Netzwerk ueber eigenen VPS, sichtbares Gefecht, Wirtschaftsdruck
(Hashkrieg, Sprint 12)

Dies ist ein grosser Sprint mit drei Straengen. Er ist auf einen langen,
zusammenhaengenden Lauf ausgelegt. Lies zuerst
docs/production/hashkrieg/12_Sprint_Zu_Zweit.md vollstaendig — dieser Prompt ist die
Kurzfassung, das Dokument ist verbindlich.

VORBEDINGUNGEN — pruefen, bevor du anfaengst
1. Sprint 11 (D-088) ist committet, Arbeitsbaum sauber.
2. .NET-8-SDK 8.0.318 ist installiert. global.json pinnt mit rollForward: disable;
   installiert war zuletzt nur 10.0.302. OHNE DIESES SDK IST STRANG A NICHT
   NACHWEISBAR — das ist ein harter Blocker, kein Komfortproblem.
3. Niemals mitcommitten: AssetMappingRegistry.asset, Packages/manifest.json +
   packages-lock.json, DefaultVolumeProfile.asset, die Screenshots im Repo-Wurzel-
   verzeichnis, .playwright-mcp/.

AUSGANGSLAGE — geprueft, nicht neu diagnostizieren
Der Netzwerkpfad ist ARCHITEKTONISCH VORBEREITET UND LEER:
- Nova.Networking.asmdef existiert (noEngineReferences: true, referenziert Nova.Core
  und Nova.Simulation) und enthaelt KEINE EINZIGE .cs-Datei.
- ICommandTransport kennt nur Send(byte[]). Kein Empfangspfad, kein
  Verbindungslebenszyklus, keine Peer-Identitaet.
- CommandIngress.TryAcceptRecordBytes(...) ist die fertige Gegenseite und laut eigenem
  Kommentar ausdruecklich fuer einen spaeteren Netzwerk-Transport gebaut.
- AiPeerCommandTransport faehrt die KI bereits byte-gleich ueber diese Grenze (D-086).
  Der Beweis, dass die Architektur traegt, ist erbracht.
- MatchRunner.StepFixedTick() ruft Ingress.SealTickBatch(nextTick) und tickt sofort
  weiter. ES GIBT KEINEN LOCKSTEP-BARRIER.
- MatchSession wird mit inputDelayTicks: 1 und localSlot: 0 erzeugt; zusaetzlich
  existiert eine AiSession mit localSlot: 1. Slot 1 ist heute strukturell die KI.
- SimulationKernel.CalculateStateHash() und MatchFingerprint (486 Zeilen) existieren.
- tools/Nova.SimRunner ist die Vorlage fuer einen headless net8.0-Prozess.
Die Zielarchitektur steht seit 2026-07-21 in docs/research/Multiplayer_Simulation.md:
deterministisches Lockstep ueber autoritativem Input-Relay. Halte dich daran.

STRANG A — ZWEI MENSCHEN, EIN MATCH  (Sprintzweck)
A1 Transport: INetworkTransport in Nova.Networking, erweitert ICommandTransport um
   Connect/Disconnect/Poll/ConnectionState. Eingehende Records gehen ueber
   TryAcceptRecordBytes in die lokale Ingress — derselbe Weg wie AiPeerCommandTransport.
   Nova.Networking behaelt noEngineReferences: true. KEIN UnityEngine-Typ darin.
A2 Lockstep-Barrier, das Herzstueck, drei Teile:
   (a) inputDelayTicks konfigurierbar; Netzwert 3 (10 Hz => 300 ms), lokal weiter 1.
       PRUEFE, ob InputDelayTicks im MatchFingerprint steht (der Klassenkommentar von
       MatchSession sagt ja). Wenn ja: bewusste Vertragsaenderung, protokollieren.
   (b) Tickbestaetigung als TRANSPORT-FRAME, NICHT als CommandKind:
         TickComplete { slot:u8, targetTick:u32, recordCount:u16 }
       Sie erreicht die Ingress nie, steht in keinem Replay, beruehrt den State-Hash
       nicht. Das v1-Command-Register bleibt eingefroren (Golden-Bytes-Test!). Tick X
       laeuft, sobald jeder aktive Slot TickComplete(X) geliefert hat UND die
       angekuendigte Recordzahl da ist.
   (c) Fehlt eine Bestaetigung: STALL mit sichtbarer Anzeige ("Warte auf Spieler 2").
       Nicht schaetzen, nicht vorwegnehmen, nicht verwerfen. Nach 30 s gilt der Peer
       als verloren, Match endet geordnet.
A3 Relay-Server: neues Projekt tools/Nova.RelayServer nach dem Muster von
   Nova.SimRunner (net8.0, Exe, kompiliert Core + Simulation mit). Aufgaben:
   Verbindungen und Slots; JEDEN Record ueber CommandPayloadValidation pruefen und
   JEDEN Record verwerfen, dessen PlayerSlot nicht zum Absender passt; weiterleiten;
   State-Hashes vergleichen; den Record-Strom als Replay mitschreiben.
   TRANSPORT IST TCP, NICHT UDP. Begruendung im Sprintdokument A3 — uebernimm sie in
   den Report, damit die Entscheidung nachlesbar ist.
A4 Handshake: vor Tick 0 MatchFingerprint + DefinitionsHash64 + Seed abgleichen. Bei
   Abweichung startet das Match NICHT, mit lesbarer Meldung welches Feld abweicht.
   Das ist der wertvollste Einzelposten des Strangs.
A5 Desync: alle 50 Ticks CalculateStateHash() an den Server, Vergleich. Bei Abweichung
   beide Klienten anhalten, "Desync bei Tick N" melden, Snapshot + Record-Strom
   wegschreiben.
A6 MatchConfig { seed, localSlot, factionPerSlot, aiSlots, inputDelayTicks, transport }.
   AiSession NUR fuer Slots in aiSlots. MatchBootstrap liest Faktionen aus der Konfig.
   Seed kommt vom Server. FRAKTIONSWAHL IM UI IST NICHT TEIL DIESES SPRINTS.
A7 Betrieb: systemd-Unit, unprivilegierter Benutzer, Konfiguration NUR ueber
   Umgebungsvariablen, PFLICHT-Match-Token, Firewallregel, docs/tech/RelayServer.md
   mit Deploy und Rollback. DU DEPLOYST NICHT SELBST und fasst keine Zugangsdaten an —
   du lieferst Dienst, Unit, Skript und Runbook.
A8 Nachweis in vier Stufen: (1) headless Zwei-Klienten-Soak ueber 10.000 Ticks in
   Nova.SimRunner.Tests, Hashes je 50 Ticks identisch — DAS ist die CI-faehige Stufe
   und muss gruen sein; (2) zwei Fenster ueber Loopback; (3) zwei Maschinen im LAN;
   (4) zwei Maschinen ueber den VPS.

STRANG B — MAN SIEHT, DASS GESCHOSSEN WIRD  (reine Praesentation)
B1 Ereigniskanal als ZUSTANDS-DIFFER in der Praesentation. Die Simulation feuert KEINE
   Events — das waere der naheliegende Pfusch und gefaehrdet den Determinismus.
   Ableitung: Schuss = steigende Flanke von WeaponCooldownTicks bei gueltigem
   AttackTarget; Treffer = CurrentHealth gesunken; Tod = IsActive true->false.
   GENAU EINMAL PRO SIMULATIONSTICK ABTASTEN, NICHT PRO FRAME (View 60 fps, Sim 10 Hz).
   Dieser Kanal ist zugleich die Vorarbeit fuer Audio Tier 0 — schneide ihn so, dass
   Audio ihn spaeter nur abonniert.
B2 Schuss: Muendungsfeuer (Partikelstoss + kurzer Lichtimpuls), Leuchtspur per
   LineRenderer mit ~0,1 s Ausblenden (die Waffen sind Hitscan — ein fliegendes
   Projektil waere eine Luege ueber das, was die Sim tut), Einschlag je DamageType.
   Unity-Bordmittel: com.unity.modules.particlesystem ist schon im Manifest. KEIN NEUES
   PAKET, kein VFX Graph. GEPOOLT und auf 64 gleichzeitige Effekte gedeckelt; bei
   Ueberlauf verwerfen statt aufstauen.
B3 Tod: Es gibt im ganzen Projekt KEIN .anim und KEINEN .controller, die 34 Modelle
   sind statische Meshes ohne Rig. Skelettanimation ist NICHT baubar — versprich sie
   nicht. Stattdessen: Einheit sackt ab und blendet ueber ~0,8 s aus, mit
   Zerlegungsstoss; Gebaeude bekommt Rauchsaeule, Absacken und eine liegenbleibende
   Truemmerflaeche. UnitViewManager poolt Views: der Effekt muss den View halten,
   solange er laeuft, und ihn danach sicher freigeben, sonst zeigt der recycelte View
   die Leiche der vorigen Einheit.
B4 Texturen: ERSTE WAHL prozedural zur Laufzeit (radialer Verlauf) — importiert nichts.
   Zweite Wahl Kenney Particle Pack: Licenses.md §1/§6 fuehrt Kenney als CC0 und
   ausdruecklich als repo-tauglich. Bei Import: Import-Protokoll §3 und PROVENANCE.json.
   Alles andere ist Default-Deny.
B5 Determinismus-Wache: Test, dass der State-Hash mit eingeschalteten Effekten
   byte-gleich ist zu dem mit ausgeschalteten. Schalter in GameSettings.

STRANG C — DIE RUNDE BEKOMMT EINEN BOGEN  (Simulation, ein Baseline-Neusatz)
C1 Knappheit: Feldreserve 2.000.000 AE -> Manifestwerte 9.000/15.000. Felder 2 -> 5
   (2 Start, 2 Expansion, 1 umkaempftes Zentrum). Ernterate (heute 2 AE/Tick, als
   Provisorium markiert) gegen die Zielkurve kalibrieren. SYMMETRIE IST PFLICHT —
   beide Startpositionen gleich weit zu Expansion und Zentrum, sonst entscheidet die
   Karte das erste Mensch-gegen-Mensch-Match.
C2 Lager: AE-Obergrenze im EconomySystem, HQ 2.000 Basis, +2.000 je Lager, Ueberschuss
   verfaellt, 25 % Verlust bei Zerstoerung (D-024). PlayerEconomyState.AddCredits
   deckelt heute nichts.
C3 Radar: Abdeckung vom GEBAEUDE ableiten. FogOfWarSystem multipliziert heute den
   Sichtradius JEDER eigenen Einheit — das Gebaeude traegt nichts bei.
C4 Low Power vollstaendig: feste vierstufige Abschaltreihenfolge, Radar und
   Verteidigung fallen IMMER ZUERST. Heute existiert nur der Tempo-Malus.
C5 Bauvoraussetzungen: PrerequisiteRole ist ein EINZELNES Feld, das Design nennt fuer
   sechs von neun Rollen Mehrfachvoraussetzungen. Bitmaske ueber UnitRole.
   ACHTUNG: PrerequisiteRole geht in DefinitionsHash64 ein
   (SimDefinitions.cs: hash.WriteUInt8((byte)def.PrerequisiteRole)). Eine
   Formataenderung aendert den Definitions-Hash — und der ist ab Strang A die Sperre,
   die zwei Klienten zusammenhaelt. Beide Seiten muessen zusammenpassen.
C6 Platzierung und Reparatur: Bau-Einflussradius 8 Zellen um HQ/Lager/Kraftwerk,
   Mindestabstand zu Feldern, Gebaeudeabstand (heute nur "in der Karte" und "Zelle
   frei"). Reparatur kostet 30 % des Neupreises statt nichts — ProcessRepairOrders
   ruft heute nie TrySpendCredits.

REIHENFOLGE
A zuerst und vollstaendig bis A8/Stufe 1 gruen. A UND C NIE GLEICHZEITIG: C bewegt den
Simulationszustand, und ein Desync-Befund aus A ist wertlos, waehrend sich die
Simulation darunter bewegt. B laeuft jederzeit dazwischen — es fasst keine Datei an,
die A oder C anfassen. Nach C: A8/Stufe 4 wiederholen.
Wenn die Zeit nicht reicht, faellt in dieser Reihenfolge: C6, dann C5, dann C4. Jeder
Abwurf mit Begruendung in docs/production/ScopeLedger.md. Skips sind erlaubt, STILLE
SKIPS NICHT.

NICHT IN DIESEM SPRINT
Fraktionswahl im UI (Sprint 13, zusammen mit Legion-Waffenidentitaet). Reconnect.
Lobby/Matchmaking/Accounts. Mehr als zwei Spieler. UDP/Rollback/Prediction. Audio-
Katalog (B1 baut nur den Kanal; der Katalog wartet auf die Lizenzerweiterung in
Licenses.md §1 um Audioquellen — Inhaberentscheidung). Skelettanimation. Attack-Move.

DER EHRLICHE PREIS
Strang A aendert einen Vertrag (Input-Delay 1 -> 3, ggf. fingerprint-relevant).
Strang C setzt die Baselines neu — Fingerprint, Replay, Snapshot-Hash,
Oeffnungs-Hashes, wie in Sprint 09 und 11. Das ist kein Defekt, das ist der Zweck
dieser Tests. Einmal, dokumentiert, mit alten UND neuen Werten im Ergebnisblock.
Strang B kostet nichts am Vertrag und muss das per Test beweisen (B5).

DETERMINISMUS — NICHT VERHANDELBAR
Kein Klient rechnet weiter, solange ein Slot fehlt — Stall ist richtig.
Kein Effekt, keine UI, kein Netzwerkcode schreibt je in den Simulationszustand.
TickComplete ist ein Transport-Frame, nie ein CommandRecord.
Der Fingerprint-Vergleich vor Tick 0 ist verpflichtend.
Alle Regeln aus Sprint 11 gelten unveraendert weiter (feste indexbasierte Reihenfolge,
kein float, Abstandsvergleiche im Quadrat ueber SimFixed).

VERIFIKATION
dotnet test tools/Nova.SimRunner.Tests muss gruen sein, einschliesslich des neuen
Zwei-Klienten-Soaks. Eine Simulationsaenderung ohne gelaufene Simulationstests wird
nicht committet. Unity-EditMode/PlayMode auf der Arbeitsmaschine nachziehen.

FERTIG WENN
Der Relay laeuft auf dem VPS, eine zweite Person bekommt den Match-Code, und wir
spielen eine vollstaendige Runde gegeneinander bis zum Ergebnisbildschirm — ohne
Desync. Haengt die Leitung, wartet mein Spiel sichtbar statt sich wegzuentwickeln.
Gegen einen anderen Build beginnt das Match gar nicht erst und sagt warum. Wenn meine
Panzer schiessen, sehe ich es — Muendungsfeuer, Leuchtspur, Einschlag; was stirbt,
sackt zusammen und laesst Truemmer zurueck. Ein Feld geht mir waehrend der Runde aus,
und ich streite mich mit dem Gegner um das mittlere. Ich baue ein Lager, weil mein
Konto sonst ueberlaeuft, und ein Radar, das mehr zeigt; und wenn ich das gegnerische
Kraftwerk zerstoere, geht seine Verteidigung aus.

ABSCHLUSS
- CHANGELOG.md: Eintrag unter [Unreleased]
- docs/production/DecisionLog.md: D-089 ff. — mindestens Lockstep-Transportmodell
  (TCP-Relay, TickComplete als Transport-Frame), Input-Delay-Vertragsaenderung und die
  Baseline-Neusetzung aus Strang C
- docs/tech/RelayServer.md: neu
- docs/production/hashkrieg/12_Sprint_Zu_Zweit.md: Status auf "umgesetzt" plus
  Ergebnisblock nach dem Muster von Sprint 11
- docs/production/GrayboxLog.md: die gespielten Runden (Stufen 2 bis 4)
- Eigener Branch, main ist PR-only. NICHT pushen ohne ausdrueckliche Freigabe des
  Inhabers. NICHT auf den VPS deployen ohne ausdrueckliche Freigabe.
```
