# Großauftrag: Von der Attrappe zur Partie

**Version:** 1.0.0 | **Status:** verbindlich | **Erteilt:** 2026-08-09 | **Auftraggeber:** Inhaber | **Ausführung:** Kimi (Maintainer-Seite) | **Umfang:** Blöcke 0–4, entspricht den Sprints 16, 18, 14 und 17 Paket A | **Leitsatz:** ein Block nach dem anderen, jeder für sich abnehmbar

## Vorrangregel

Dieses Dokument ist die **einzige verbindliche Reihenfolge**. Wo es von einer
Sprintdatei abweicht, gilt dieses Dokument. Wo es schweigt, gilt die Sprintdatei.

Wenn dir etwas widersprüchlich vorkommt: **halte an und frag nach.** Rate nicht.
Ein falsch aufgelöster Widerspruch kostet mehr als eine Rückfrage.

## Die Anweisung des Inhabers

Wörtlich sinngemäß, damit du den Rahmen kennst, in dem du entscheidest:

> Wir sind in einer geschlossenen Beta-Gruppe. Ich möchte jetzt erst einmal
> nichts mehr von Lizenzen und von Datenschutz hören. Die Formalitäten können
> wir später klären. Lass uns vorankommen und fertig werden. Wenn es dazu Fragen
> gibt, nimm sie in den Fragenkatalog auf, und wir reden später darüber.

Das heißt konkret für dich:

- **Blockiere nicht** an Lizenz-, Datenschutz-, Provenienz- oder Tier-3-Fragen.
- Wenn eine solche Frage auftaucht: eine Zeile in
  [../OpenQuestions.md](../OpenQuestions.md), weiterarbeiten.
- Das gilt **nicht** für technische Widersprüche und nicht für die Grenzen unten.
  Die sind hart.

## Der Auftrag in einem Satz

Aus dem ersten Betatest sind 16 Issues entstanden (#43–#58). Du behebst sie in
der Reihenfolge, in der sie sich gegenseitig am wenigsten im Weg stehen, und
nimmst dabei die Wirtschaftsmechanik mit, die ohnehin an derselben Stelle fällig
war.

## Was du nie anfasst

Diese Pfade gehören einem **externen Beitragenden** (Einheitenstrang 13B). Ein
PR, der sie berührt, wird nicht gemergt, sondern zurückgegeben:

```
Assets/_Project/Scripts/Simulation/Combat/
Assets/_Project/Scripts/Simulation/Movement/
Assets/_Project/Scripts/Simulation/Factions/
Assets/_Project/Scripts/Simulation/Pathfinding/
Assets/_Project/Scripts/AI/
Assets/_Project/Scripts/AI.Data/
Assets/_Project/Scripts/Presentation/UI/DebugHud.cs
tools/Nova.AiLab/            tools/Nova.AiLab.Tests/
```

Diese sind **eingefroren** und brauchen eine Inhaberentscheidung mit D-ID:

```
Assets/_Project/Scripts/Simulation/CommandsV1/     ← kein neuer CommandKind
Assets/_Project/Scripts/Simulation/Replays/
Assets/_Project/Scripts/Simulation/Snapshots/
Assets/_Project/Scripts/Simulation/Systems/
Assets/_Project/Scripts/Simulation/SimulationKernel.cs
Assets/_Project/Scripts/Simulation/State/  — nur Layout und Serialisierung:
   Feldbestand, Feldreihenfolge, StateVersion, Blockformat
```

**`Simulation/State/UnitCommandStateView.cs` darfst du bearbeiten**, aber nur die
Befehlsanwendung — *was* ein bestehender `CommandKind` mit dem Zustand tut. Das
ist seit [D-092](../DecisionLog.md) ausdrücklich erlaubt. Kein neues Feld, keine
neue Reihenfolge, kein `StateVersion`-Bump.

Und diese vier Dateien fasst **kein PR dieses Auftrags** an:

```
tools/Nova.SimRunner.Tests/SnapshotGoldenBytesTests.cs
tools/Nova.SimRunner.Tests/CommandGoldenBytesTests.cs
tools/Nova.SimRunner.Tests/SimRandomGoldenTests.cs
tools/Nova.SimRunner.Tests/Determinism10000Tests.cs
```

Ausnahme: ein **eigener** PR, der ausschließlich eine Baseline neu setzt, mit
altem und neuem Wert im Text und der Begründung, warum die Änderung gewollt ist.
Nie zusammen mit einer Verhaltensänderung. Das Drehbuch
`tools/Nova.SimRunner/Determinism10000Scenario.cs` ist davon **nicht** betroffen
und darf im selben PR nachgezogen werden.

## Die fünf Blöcke

Arbeite sie **in dieser Reihenfolge** ab. Zwischen zwei Blöcken liegt ein Gate:
erst wenn der vorige gemergt und gespielt gesehen ist, fängt der nächste an.

### Block 0 · Der Auswahlrahmen (Issue #49)

Ein Nachmittag, ein PR, sofort sichtbar.

`GroundMarkerVisuals.BorderPixels` ist heute 6 von `TextureSize = 64` — ein
texturrelativer Randstreifen, kein Bildschirmpixel. Mach ihn dünner.

Und reduziere die **Füllung** (`fill`, heute Alpha 0.28) oder lass sie weg. Sie
ist der eigentliche Grund für den grünen Teppich im Pulk — und damit die halbe
Behebung von #50.

> **Nicht als Pixelwert formulieren.** Die Bildschirmdicke ändert sich mit jedem
> Zoomschritt; „ein Pixel dünn" ist mit dieser Technik nicht erfüllbar. Und
> dieselbe Textur benutzen **vier** Konsumenten: `SelectionMarkerView`,
> `PlacementGhostView`, `RallyFlagView`, `ConstructionSiteMarkerView`. Eine Zahl
> ändert vier Optiken — sieh dir alle vier an, bevor du den PR aufmachst.

Warum zuerst: #50 und #51 in Block 2 wirken erst, wenn die Markierung nicht mehr
verdeckt, was sie zeigen soll.

### Block 1 · Sprint 16 — Die Wirtschaft trägt sich selbst

**Binde Sprintdatei:** [16_Sprint_Wirtschaft.md](16_Sprint_Wirtschaft.md)

Der größte Block. Zehn Pakete, 16.1 bis 16.10, in der dort stehenden Reihenfolge
— sie ist nach Kosten sortiert: erst was weder die Startaufstellung noch
`SimDefinitions` anfasst, dann 16.7 (Startaufstellung, fünf Spiegel), zuletzt
16.8 (`SimDefinitions`, bewegt den Definitions-Hash).

Enthält die Issues #43, #44, #45, #46, #47, #48, #53, #54 und Strang C aus
Sprint 12 (C1–C6).

Zwei Dinge, die du nicht aus der Sprintdatei allein siehst:

- **16.8 muss vor dem VPS-Rollout fertig sein.** Es bewegt `DefinitionsHash64`,
  und der Relay vergleicht ihn serverseitig. Solange er lokal läuft, kostet das
  nichts. 16.7 ist davon **nicht** betroffen — `FieldReserveAE` steht in
  `MatchBootstrap`, `HarvestRateAE` in `EconomySystem`, keines in
  `SimDefinitions`.
- **Die Abwurfliste ist ernst gemeint.** Reicht die Zeit nicht: erst 16.9, dann
  16.8, dann 16.6. Mit Begründung in den [ScopeLedger](../ScopeLedger.md).
  16.1 bis 16.5 fallen nicht.

### Block 2 · Sprint 18 — Befehl und Auswahl

**Binde Sprintdatei:** [18_Sprint_Befehl_und_Auswahl.md](18_Sprint_Befehl_und_Auswahl.md)

Drei Pakete. Enthält die Issues #50, #51, #52.

Fasst **keine** Simulationsdatei an außer der Zielverteilung in `ApplyMove`.
18.3 (Formationsausrichtung) ist die erste Abwurfkandidatin — die Verteilung
selbst existiert seit Sprint 11 bereits.

### Block 3 · Sprint 14 — Die Lobby

**Binde Sprintdatei:** [14_Sprint_Lobby.md](14_Sprint_Lobby.md)

Sprint 14 hat heute **null Zeilen** im Repository. Es gibt keinen Supabase-Client,
keine Edge Function, kein Schema, und `docs/tech/LobbySupabase.md` ist verlinkt,
aber nie geschrieben worden.

Vor 14.1 kommt ein Paket, das die Sprintdatei nicht kennt:

#### 14.0 · Das Spiel kennt seinen eigenen Build

`NovaBuildCommit` wird von **keiner einzigen C#-Datei** gelesen. Beide
Packaging-Skripte brennen ihn ein — macOS als `Info.plist`-Schlüssel, Linux als
`ProjectNova_Data/NovaBuildCommit.txt` — aber das Spiel kann ihn weder anzeigen
noch melden. Ohne diesen Leser ist 14.4 (Build-Abgleich) nicht baubar.

Ablageort für den Leser: `Assets/_Project/Scripts/Gameplay/` (Assembly
`Nova.Gameplay`). **Nicht** `Scripts/Core/`: das ist engine-frei, wird in die
SimRunner-Testlane hineinkompiliert und steht im Baseline-Wächter unter den
Simulationsflächen — jede Änderung dort gälte als Verhaltensänderung.

**Ablage der Serverseite — entschieden mit D-098**, weil 14.1 den Ort offengelassen
hat: SQL, Policies und Edge Functions kommen als Quelltext ins Repository unter
`tools/lobby/`, das Runbook nach `docs/tech/LobbySupabase.md`. Ausgerollt wird im
Supabase-Projekt außerhalb des Repos. Unversionierter Servercode ist genau das,
was in sechs Monaten niemand mehr nachvollzieht.

**Was du nicht kannst:** ausrollen. Du bereitest so vor, dass ein Deploy alles
mitbringt; der Inhaber führt es aus.

### Block 4 · Sprint 17 Paket A — Wer da spielt

**Binde Sprintdatei:** [17_Sprint_Zugangsprotokoll.md](17_Sprint_Zugangsprotokoll.md),
Pakete 17.1 bis 17.5.

Jeder Aufruf der Lobby-Functions schreibt eine Zeile: Zeitpunkt, Endpunkt,
Herkunfts-IP, Netzpräfix, Build-Commit, Match-Code, Ergebnis. Dazu Sperrliste
(`install` / `ip` / `prefix`), Rate-Limit, Bedienweg als fertige SQL-Bausteine im
Runbook, und ein `pg_cron`-Job für die Fristen.

**Für die geschlossene Beta wird die IP im Klartext gespeichert**, dazu das
gekürzte Netzpräfix. Die 30-Tage-Frist aus 17.5 ist die Begrenzung. Die
ursprüngliche Vorgabe „kein Rohwert in der Datenbank" ist damit für diese Phase
aufgehoben; die Umstellung auf Hashing steht als Frage im Fragenkatalog.

**Das Protokoll liegt in den Lobby-Functions, nicht im Relay.** Der Relay bleibt
dumm — das ist eine getroffene Architekturentscheidung und wird hier nicht
angerührt.

**Was du nicht kannst:** ausrollen und betreiben. Du lieferst SQL, Policies,
Functions und Runbook als Quelltext; **der Inhaber führt Deploy und
Betreiberprobe aus** und trägt das Ergebnis nach. Der Nachweis für diesen Block
ist deshalb weder die SimRunner-CI noch eine gespielte Runde, sondern der
Betreibernachweis des Inhabers am ausgerollten Stand.

Am Relay gibt es ausser der Token-Entgegennahme aus Paket 14.5 genau **eine**
Änderung, und sie ist ein echter Fehler:

#### 17.0 · Das `.partial`-Leck

`RelayServerCore.ResetMatch` verwirft den Aufzeichnungsstrom nur per `Dispose`
und merkt sich den Pfad, **löscht ihn aber nie**. Nur der Erfolgsweg benennt
`.partial` atomar nach `.novarec` um. Jede abgebrochene Partie lässt eine Datei
liegen, die nie jemand aufräumt.

Behebe das, und schreib den **Vorschlag** für eine Aufbewahrungsregel für
`.novarec` ins Runbook — die Frist entscheidet der Inhaber (Q-046). Naheliegender
Startwert analog 17.5: 30 Tage.

> **Was ausdrücklich NICHT in diesen Block gehört**, obwohl Sprint 15.5 es
> verspricht:
> - **Kein Umformatieren der Startmeldung.** `deploy.sh` erkennt Bereitschaft an
>   `grep -F '[Relay] ready on '`. Eine strukturierte Logzeile lässt
>   `wait_for_ready` in den Timeout laufen und den Rollback greifen.
> - **Kein automatisches Löschen alter `.novarec`.** Sie sind der einzige
>   Desync-Nachweis (D-089: der Relay simuliert nicht, die Aufnahme *ist* die
>   Reproduktion). Erst eine Aufbewahrungsregel, dann ein Aufräumjob.
> - **Kein Health-Endpunkt.** Er wäre ein zweiter Listener und damit eine neue
>   Firewallregel auf einem Dienst, dessen einzige Betriebsgrenze die enge
>   Quelladress-Firewall ist. Steht im Fragenkatalog.
> - **Keine Logrotation im Repo.** Der Relay schreibt nur nach stdout/stderr, die
>   Unit leitet nach journald. Rotation ist VPS-Konfiguration, kein Repo-Code.
>
> „Dauerbetriebsfest" heißt hier: beliebig viele Partien **nacheinander**.
> `MaxPeers = 2`, nach Matchende setzt `ResetMatch` auf Listening zurück.
> Gleichzeitige Partien bräuchten mehrere Prozesse und gibt es nicht.

## Die sechs stillen Fallen

Diese brechen nichts sofort. Sie brechen später, an einer Stelle, die nicht nach
der Ursache aussieht.

**1 · Die kanonische Startaufstellung wird an vier Stellen gepflegt.**
Wer sie ändert (Paket 16.7), ändert sie synchron in
`Gameplay/Match/MatchBootstrap.cs:SetupSlot`,
`tools/Nova.SimRunner/Determinism10000Scenario.cs:SetupMatch` und **beiden**
`CanonicalMatchSetupTests`-Spiegeln (CI-Kopie unter `tools/`, Unity-Kopie unter
`Assets/Tests/`). Drei von vier ergibt einen roten Test, der wie ein
Determinismusfehler aussieht und keiner ist.

**2 · Die Baustellen-Rolle hängt an der Strombilanz.**
Wenn die Baustelle in 16.3 `def.Role` statt `UnitRole.Unit` bekommt, zieht sie ab
sofort vollen Strom — `EconomySystem.RecomputePower` resolviert rein über die
Rolle. Das muss kompensiert werden. Dazu schaltet `UnitViewManager` an `IsBuildingRole`
(Prefab, Größe, Rotationssperre) — eine Baustelle würde als fertiges Gebäude
gerendert; die Optik von `ConstructionSiteMarkerView` mitprüfen.
`SelectionManager.CopyMobileSelection` fällt in die andere Richtung: die
Baustelle verschwindet aus dem Versand mobiler Befehle. Die Befehlskarte ist
unbetroffen, `TryGetSite` greift vor `IsBuildingRole`.

**3 · Energie darf nicht in `BuildMenuHud.IsAvailable`.**
Sonst wird der Knopf ausgegraut und der Platzierungsmodus ist gar nicht mehr
erreichbar. *Grund anzeigen* ist nicht *Knopf sperren*.

**4 · Jede neue HUD-Zeile braucht zwei Nachträge.**
`CommandCardHud.EstimateHeight` bildet die Höhenrechnung von `OnGUI` Zeile für
Zeile nach — der Kommentar dort dokumentiert genau diesen Fehler aus der
Vergangenheit („~40 px short … visible, but not clickable"). Und jede neue
Trefferfläche gehört in `IsPointerOverHud`, das heute drei Komponenten kennt;
fehlt sie, schlagen Klicks hinter dem Panel in die Welt durch.

**5 · Ein Nachsetz-Takt ohne Bremse flutet den Befehlsstrom.**
`UpdateHarvesterEscort` sieht aus, als hätte es zwei Bremsen — hat aber eine:
`AlreadyHeadingTo` beginnt selbst mit `unit.IsMoving`. Effektiv gilt „nachsetzen,
solange die Einheit steht", und genau das unterdrückt die Neuausgabe, die 18.2
für ein fliehendes Ziel braucht. Übernimm das Muster **nicht** wörtlich:
Idempotenz über die Zielzelle statt über `IsMoving`, plus eine eigene
Mindestkadenz. Ohne Bremse kostet jeder Takt eine Sequenznummer, landet im Replay
und im Relay-Strom und läuft irgendwann in `PendingQueueFull` oder
`SequenceOverflow`.

**6 · `tools/build/` ist ignoriert.**
Der Ordner sieht aus wie der Packaging-Ordner und enthält eine fast identische
Kopie von `build-mac.sh`, ist aber über `.gitignore` ausgeschlossen. Änderungen
dort verschwinden. Der getrackte Weg ist ausschließlich `tools/packaging/`.

## Wie du arbeitest

| | |
|---|---|
| **Branch** | eigener Topic-Branch je Block (`feat/`, `fix/`), `main` ist PR-only |
| **PR-Schnitt** | ein PR je Paket oder je zusammenhängender Gruppe. Lieber mehrere kleine als einer, der drei Ordner öffnet |
| **Verhalten und Baseline** | nie im selben PR. Baseline-Neusetzung ist ein eigener PR mit altem und neuem Wert im Text |
| **CHANGELOG** | genau eine Zeile unter `[Unreleased]`, ganz oben im Abschnitt. Keine datierte Versionsüberschrift anlegen |
| **Commit** | Conventional Commit |
| **Push** | **nie ohne ausdrückliche Freigabe des Inhabers** |
| **Deploy** | nie. Weder VPS noch Supabase |

### Der Nachweis

Es gibt zwei Bedingungen, und **beide** müssen zutreffen:

1. `dotnet test tools/Nova.SimRunner.Tests` ist grün — bei einem
   verhaltensändernden PR **nach** dem unmittelbar folgenden Baseline-PR. Ein
   roter Golden-Byte-Test im Verhaltens-PR ist dort erwartet und kein Blocker;
   der Maintainer setzt entweder das Etikett `baseline-reset-approved` oder
   mergt den Baseline-PR direkt hinterher.
2. Ein Mensch hat die Sache im laufenden Spiel gesehen und es notiert — im PR
   oder im [GrayboxLog](../GrayboxLog.md).

Zwei Dinge, die du dazu wissen musst:

- **Lokal läuft `dotnet test` nicht.** `global.json` pinnt SDK `8.0.318` mit
  `rollForward: disable`; auf der Arbeitsmaschine liegt nur `10.0.302`. Der
  Nachweis läuft über die CI im PR. Behaupte keinen grünen lokalen Lauf.
- **Für Block 0, Block 2 und die HUD-Teile von Block 1 führt kein CI-Lauf
  Gameplay- oder Presentation-Code aus,** und die Unity-EditMode-Tests laufen
  mangels Lizenz nicht (`505 Unsupported protocol version`). Die Quelltext-Wächter
  greifen dort trotzdem: `PresentationSourceBoundaryTests` scannt auf
  `GetUnitRef(` und `.Random` und lässt den Build platzen, ebenso der
  asmdef-Rangcheck. Ein *Verhaltens*nachweis ist dort die gespielte Runde plus
  Screenshot. Schreib in den PR, was du gesehen hast — und wenn du es nicht selbst
  sehen kannst, sag das, statt es zu behaupten.

### Was du selbst entscheidest

Alles Handwerkliche: Benennung, Aufteilung, Reihenfolge innerhalb eines Pakets,
Konstanten ohne Designwirkung. Notier die Entscheidung kurz im PR.

### Was du nicht entscheidest

- Einen neuen `CommandKind` einführen
- `StateVersion`, Schemaversionen oder das Zustandslayout ändern
- Die Tickreihenfolge in `MatchRunner` ändern oder ein neues System registrieren
- Ein Paket streichen, das nicht auf der Abwurfliste steht
- Irgendetwas in fremdem Terrain, auch wenn es „nur eine Zeile" wäre

In allen fünf Fällen: anhalten, im PR oder in einer Nachricht beschreiben, warum
es nötig wäre, und auf die Entscheidung warten.

## Was nicht in diesem Auftrag ist

| | Warum |
|---|---|
| **Sprint 13.2, 13.4, 13.5** (VPS-Deployment, LAN- und VPS-Abnahme) | 13.2 braucht Zugangsdaten, 13.4 und 13.5 brauchen zwei Menschen an zwei Rechnern. Kein Codeauftrag |
| **Sprint 13.3** (zwei Unity-Fenster auf einem Rechner) | allein machbar, aber von einem Menschen an der Maschine — eine gespielte Runde, kein Agentenauftrag |
| **Sprint 15.1–15.4** (Reconnect, Desync-Forensik, adaptive Verzögerung, Rundenabschluss) | eigener Sprint, nach diesem Auftrag |
| **Sprint 19 / Art** (#57 hohle Gebäude, #58 Radarturm-Maßstab) | Art-Arbeit außerhalb des Repositories. 16.2 macht #57 sichtbarer — das ist ein Befund für den GrayboxLog, keine Aufgabe für dich |
| **Halte-Feuer / „Feuer einstellen"** | braucht `Simulation/Combat/` — Einheitenstrang |
| **Issue #42** (Zittern im Pulk) | `Simulation/Movement/` — Einheitenstrang |
| **#55 Reparaturzone, #56 Sanitäter** | im Fragenkatalog, kein Bauauftrag |
| **Attack-Move** | neuer `CommandKind` gegen das eingefrorene v1-Register |

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-08-09 | Erstfassung: Blöcke 0–4 gebündelt, am Code geprüft, Grenzen und stille Fallen benannt | Orchestrator |
