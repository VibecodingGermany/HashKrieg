# Changelog

Alle nennenswerten Änderungen an *Project Nova* werden in dieser Datei dokumentiert.

Das Format orientiert sich an [Keep a Changelog](https://keepachangelog.com/de/1.1.0/),
die Versionierung folgt (in der aktuellen Doku-Phase) dem Dokumentationsstand des Wikis
([docs/README.md](docs/README.md)). Kategorien: `Hinzugefügt`, `Geändert`, `Behoben`,
`Entfernt`, `Entschieden` (projektspezifisch für DecisionLog-Einträge).

> **Pflege-Regel:** Jeder PR ergänzt einen Eintrag unter `[Unreleased]` (einer pro PR
> genügt, nicht pro Einzeländerung). Beim Abschluss eines Sprints wird `[Unreleased]`
> in eine datierte Version überführt. Details siehe [AGENTS.md](AGENTS.md).

## [Unreleased]

> **Dokumentationsstand 0.17.0 (unveröffentlicht):** Dieses Rebaseline ist ein
> Wiki-/Vertrags-Minor und kein Game-Release. Es wird kein Tag oder Release
> erzeugt; MS-0 und MS-1 bleiben offen.

### Hinzugefügt
- **Verbindungsdialog und Linux-Build (Sprint 13, Pakete 13.1 und 13.7):** Das
  Hauptmenü hat einen Bereich „Netzpartie" mit Serveradresse, Port, maskiertem
  Match-Code, Rollenwahl Host/Gast und einem Statusband, das die Zustände des
  `RelayMatchClient` ehrlich benennt statt stumm zu bleiben. Der TCP-Connect ist
  von blockierend auf poll-getrieben umgestellt, damit ein nicht erreichbarer
  Server Unitys Hauptthread nicht mehr einfriert; Host und Port werden vor dem
  Verbindungsversuch geprüft. Der Relay unterscheidet jetzt zwischen dem
  10-Sekunden-Fenster für den Handshake-Beweis und einem eigenen
  120-Sekunden-Fenster fürs Warten auf den Gegenspieler, weil beides vorher
  denselben Timeout teilte. Dazu `BuildScript.BuildLinux64` und
  `tools/packaging/build-linux.sh`, damit am Netznachweis nicht nur teilnehmen
  kann, wer einen Mac hat.
- **Freigabe für externe Beiträge (D-091, Sprint 13.0):** Der unveränderte
  Text der `PolyForm-Noncommercial-1.0.0`-Lizenz, ein Scope-/Asset-`NOTICE` und
  eine nicht-exklusive Contributor License Agreement schaffen den
  Source-available-Beitragsweg, ohne kommerzielle Forks allgemein freizugeben.
  Die CLA gilt nur für Beiträge mit dokumentierter Zustimmung und erfasst
  bestehende Rechte anderer Beitragender nicht rückwirkend.
  Neue PR-Checks prüfen externe Maintainer-Freigabe auf dem aktuellen Head und
  die Trennung von Simulationsverhalten und Determinismus-Baselines. Jeder PR
  erhält im vorgesehenen Remote-Rollout eine Freigabe des jeweils anderen
  Maintainers. Die beiden Metadatenchecks laufen aus dem geschützten
  Zielbranch, erhalten ausschließlich Leserechte und checken oder führen keinen
  Fork-PR-Code aus. Die Remote-Aktivierung als Required
  Checks samt Peer-Review und der absichtlich falsche Negativ-PR stehen
  ausdrücklich noch aus.
- **Zwei-Spieler-Lockstep über eigenen TCP-Relay (D-089, Sprint 12 Strang A):**
  Der bisher nur vorbereitete Netzwerkpfad ist als Engine-freie
  Zwei-Slot-Implementierung verdrahtet: TCP-Handshake mit Match-Token,
  Slot-/Seed-/Delay-Angebot, Fingerprint- und Initialsnapshot-Sperre,
  `TickComplete`-Barrier, State-Hash-Vergleich alle 50 Ticks sowie geordnete
  Endzustände bei Desync, Peer-Verlust und Protokollverletzung. Ein optionaler
  `ICommandSubmissionReadiness`-Vertrag lässt `ICommandTransport` unverändert
  und weist Eingaben vor Session-Aktion und Sequenzvergabe mit
  `TransportNotReady` ab, bis der Relay-Client tatsächlich `Running` ist.
  `MatchConfig`, `MatchBootstrap` und `MatchRunner` tragen Slot, Fraktionen,
  AI-Slots, Seed, Delay und Transport bis in die Tickschleife; AI entsteht nur
  für konfigurierte Slots, Netzwerkpausen sind gesperrt und die UI liest den
  Lebenszyklus nur über Gameplay-Properties von `MatchRunner` und
  `MatchBootstrap`. Der nicht simulierende Server schreibt
  statt erfundener Replay-Resultcodes das gehärtete Transportformat
  `NOVAREC2` (lückenlose Leer-/Command-Ticks, exakte Counts/Dedupe/Caps,
  50-Tick-Checkpoints, terminaler Footer, 64-MiB-Grenze und atomare
  `.partial`→`.novarec`-Publikation); Client-Diagnostik nutzt einen begrenzten
  On-Disk-Spool und atomare Publikation. Hinzu kommen ein self-contained
  `linux-x64`-Publish-Baum, gepinnter GitHub-Actions-Test/Bundle-Workflow,
  gehärtete systemd-Unit und transaktionales `bootstrap`/`deploy`/`rollback`
  über unveränderliche SHA-Releases. Nachgewiesen sind 547/547
  SimRunner-Tests, Relay-Build und lokale Prozess-/Bundle-Smokes sowie A8
  Stufe 1 mit zwei echten TCP-Clients bis Tick 10.023, Checkpoints alle 50
  Ticks und identischem Live-/Playback-Endhash. Offen bleiben zwei
  Unity-Fenster, LAN und VPS (A8 Stufen 2–4), ein echtes Linux-/systemd-/VPS-
  Deploy und ein Live-Lauf des Workflows. Der Unity-EditMode-Versuch endete vor
  den Tests am Lizenzhandshake `505 Unsupported protocol version 1.18.1`; eine
  gespielte Netzwerkpartie und vollständige DoD werden nicht behauptet.
- **Sicht- und hörbares Gefechtsfeedback (D-090, Sprint 12 Strang B):** Ein
  fog-sicherer, rein lesender Zustands-Differ leitet Schuss, Treffer, sicheren
  Tod und eigene fertige Einheiten aus sichtbaren Snapshots ab, ohne
  Simulation, Netzwerk, Replays oder Hash-Baselines zu verändern. Gepoolte und
  hart gedeckelte Unity-Bordmittel liefern Mündungsstoß, höchstens 0,1 s lange
  Hitscan-Spur, Trefferfunken, Rauch und einen 0,8-s-Todes-Hold mit
  slot-sicherer View-Rückgabe. Der D-039-konforme `UnityAudioService` spielt
  zwölf Tier-0-Ereignisse über `MIX_Master` mit 30 One-Shot-/24 räumlichen
  Stimmen, atomaren Layern, Prioritäts-Stealing und wirksamem SFX-Regler.
  Importiert wurden genau 35 unveränderte Kenney-CC0-OGGs samt Pack-Sidecars;
  die vier Suno-Musikdatensätze bleiben mit ihren real fehlenden Belegen
  ausdrücklich unvollständig. Hinzu kommen ein headless Quellcode-Guard,
  Budget-/Differ-/Pooltests und idempotentes Unity-Authoring. Nachgewiesen sind
  549/549 SimRunner-Tests, 521/521 Unity-EditMode-Tests, der neue
  Slot-Reuse-PlayMode-Test sowie ein erfolgreicher universeller macOS-Build.
  Der PlayMode-Gesamtlauf steht bei 8/9, weil der bestehende headless
  `BarracksSpawnDiagnosisTests` an `RenderTexture.Create` scheitert; die
  manuelle 60-Einheiten-Gegenhör-/Sichtabnahme bleibt offen.
- **Art-Paket ist reproduzierbar geworden — `tools/art/build_art_package.sh`:**
  Der Paketinhalt wird nicht mehr von Hand zusammengestellt, sondern aus
  `.gitignore` abgeleitet — alles, was git im Art-Baum ausschliesst, gehört ins
  Paket, und nur das. Damit können Repo-Ausschluss und Paketinhalt nicht mehr
  auseinanderlaufen. Vor dem Packen prüft das Skript, dass zu jedem Asset sein
  `.meta` vorliegt, und bricht sonst ab: ein Paket ohne GUIDs bräche
  Material-, Prefab- und Registry-Referenzen bei jedem Entwickler anders.
- **Verschickbare macOS-Builds — `tools/packaging/build-mac.sh`:** Ein Lauf
  baut den universellen Player (Intel und Apple Silicon), signiert ihn mit
  Developer ID unter Hardened Runtime, notarisiert App und DMG getrennt und
  legt `Builds/dist/ProjectNova-<commit>.dmg` ab. Der Empfänger zieht die App
  nach „Applications" und startet sie ohne Gatekeeper-Warnung. Der Commit-Hash
  steht im DMG-Namen, in `LIESMICH.txt` und als `NovaBuildCommit` im
  `Info.plist`, weil der Relay Matches zwischen ungleichen Builds absichtlich
  ablehnt (Sprint 12, A4) — die Frage „welchen Build hast du eigentlich?" ist
  damit ohne Rückfrage beantwortbar. Ein Build aus unsauberem Arbeitsbaum
  bekommt `-dirty` und ist als nicht rekonstruierbar markiert. `--fast`
  überspringt Signatur und DMG fürs eigene Probespielen.
- **Sprints 13–15 geplant, erstmals für Parallelbetrieb mit einem externen
  Beitragenden:** Der Netzstrang (13 Netzpartie über den VPS, 14 Lobby über
  Supabase, 15 Netzstabilität) ist so geschnitten, dass er **keine Datei unter
  `Scripts/Simulation/` oder `Scripts/AI*` anfasst** — damit gehört der
  Simulationsraum für die Dauer dieser Sprints allein dem Einheitenstrang
  ([13B](docs/production/hashkrieg/13B_Sprint_Einheitenverhalten.md)), und die
  Schreibhoheiten sind wirklich disjunkt statt zufällig überschneidungsfrei.
  Das Regelwerk dazu steht in
  [13-15_Parallelbetrieb.md](docs/production/hashkrieg/13-15_Parallelbetrieb.md):
  Schreibhoheit je Pfad, Merge-Fenster mit Rebuild-Kadenz (jeder
  simulationsverändernde Merge macht verteilte Builds ungültig, weil der
  Fingerprint ungleiche Builds trennt), Fork-only-Zugang für fremde
  Beitragende — und die Regel, dass ein PR Verhalten **oder** eine
  Determinismus-Baseline ändert, nie beides, weil sonst eine unbemerkte
  Verhaltensänderung grün durch die CI läuft. Strang C aus Sprint 12 rückt
  dafür auf Sprint 16, weil er simulationsverändernd ist.

### Geändert
- **Governance-Tier 2 (D-091):** Externe Beiträge erfolgen nur aus Forks;
  `@cubetribe` (Dennis Westermann) und `@travelhawk` (Michael Falk) bleiben die
  einzigen Accounts mit Merge-Zugang zu `main`. `integrity` läuft auf jedem PR;
  Verträge und öffentliche Doku führen wieder Version und Änderungsverlauf.
- **Truppenführung — Einheiten teilen sich den Platz (D-088, Sprint 11):**
  eine Armee ist kein Haufen mehr. Zwölf markierte Einheiten kommen als
  Gruppe nebeneinander an statt übereinander, frisch gebaute Truppen bilden
  eine Reihe vor der Kaserne statt eines Punkts, und Armeen laufen **um**
  Gebäude herum statt hindurch.
  - **Formationsverteilung beim Move-Befehl:** die Einheiten werden
    deterministisch auf freie Zellen um das Ziel verteilt — kleinster
    Entity-Index bekommt die Zielzelle, die folgenden die expandierenden
    Chebyshev-Ringe in aufsteigender (y, x)-Reihenfolge. Die Gruppe teilt
    sich dabei **ein** Flow-Field; jede Einheit trägt ihre persönliche
    Ankunftszelle im neuen `UnitState.GoalGridPos` (Entity-Store-Block v5).
  - **Separation auch im Stand:** angekommene Einheiten weichen einander
    weiter aus — gedämpft, pro Tick gedeckelt und mit Totzone, also lösend
    statt vibrierend; exakte Überlappungen löst ein Index-Tiebreak.
    Gebäude und Baustellen werden nie verschoben, wirken aber als
    Hindernis. Kein Bewegungsschritt betritt mehr eine unbegehbare Zelle.
  - **Gebäude sind Gelände:** Footprints stehen als unbegehbar im
    Kostenfeld der Wegfindung (Platzierung, Verkauf, Zerstörung). Wer beim
    Platzieren im Footprint steht, wird auf die nächste freie Zelle
    geschoben statt eingemauert; wem unterwegs das Ziel bebaut wird, hält
    an der Wand statt ewig dagegenzulaufen. Dahinter zwei Vertragsänderungen
    (D-088): der Epoch-Restore des Pathfinding-Blocks adoptiert die
    serialisierte Epoch statt sie zu vergleichen, und der Flow-Field-Cache
    regeneriert bei Terrainänderung an Ort statt geleert zu werden.
  - **Produktions-Spawn meidet einheitenbelegte Zellen** (bisher wurden nur
    Gebäude-Footprints geprüft) — fünf gebaute Soldaten stehen als Reihe
    vor der Kaserne.
  - **KI-Folgefix:** die Skirmish-KI wählt Bau-Laufziele footprint-frei;
    ihre feste Westseiten-Regel konnte in einem Nachbargebäude enden und
    die Baustelle dauerhaft pausieren.
- **Art-Paket auf den Nachschub-Stand gehoben
  ([docs/assets/AssetPackage.md](docs/assets/AssetPackage.md) 1.1.0):** Das
  verteilte Paket enthielt noch den Erstimport-Stand vom 2026-08-06 und damit
  weder das ersetzte Allianz-HQ und den ersetzten BattleTank noch das neue
  Aetherium-Mesh. Neue Kennzahlen in §3 (276 Dateien, rund 109 MB, neuer
  SHA-256), Paketname auf die Marke umgestellt (`Hashkrieg_Art_MS1_*` nach
  E-1/E-3), §5 auf das Build-Skript umgestellt. Ausdrücklich festgehalten ist
  jetzt auch der Freigabeweg: personengebundene Ordnerfreigabe statt
  Link-Sharing, und kein Ordnerlink im öffentlichen Repository — solange die
  Lizenzfelder der KI-generierten Modelle offen sind, ist die Freigabe an
  benannte Personen die gedeckte Variante, eine Veröffentlichung nicht.
- **Unity-Projekteinstellungen nachgezogen:** Static Batching für Standalone
  aktiviert (`m_BuildTargetBatching`), und die URP-Shader-Prefiltering-Flags in
  `NovaUrp.asset` sowie die Runtime-Settings-Liste in
  `UniversalRenderPipelineGlobalSettings.asset` stehen auf dem Stand, den der
  Editor beim macOS-Build erzeugt hat. Ohne diese Dateien im Repo bekäme jeder
  Mitarbeitende beim ersten Öffnen des Projekts denselben Diff erneut als
  ungewollte lokale Änderung. Das Define `SENTIS_ANALYTICS_ENABLED` kommt vom
  Paket `com.unity.ai.inference` und wird vom Editor gesetzt, nicht von Hand.

### Verifikation (D-088)
- `dotnet test tools/Nova.SimRunner.Tests`: **438/438 grün** (neun neue
  Truppenführung-Tests je Lane; die Epoch-/Cache-Vertragstests sind auf die
  neue Semantik umgeschrieben).
- Baselines bewusst und dokumentiert neu gesetzt: SimRunner-Hash
  `0xB680C879DEA70B26`, `DETERMINISM_10000`-Fingerprint
  `0xAD8531312FE93F4B`, Final-Hash `0x6916A323202089A9`,
  Playback-Self-Check PASS.
- Die gespielte Runde des Inhabers (DoD: Spielverhalten geändert) steht
  aus.

### Hinzugefügt
- **Gefecht und Rundenrahmen (D-086, D-087, Sprint 09):** aus der Demo wird
  eine Runde — ernten, bauen, kämpfen, gewinnen oder verlieren, neu anfangen.
  - **Harvester fahren von selbst:** der Client-Dispatch (D-085-Präzedenz)
    fährt beide Beine des Erntekreislaufs per Move-Intent — zum Feld bei
    stehendem Ernteauftrag außer Reichweite, zur nächsten eigenen Raffinerie
    bei voller Ladung. Manuelle Züge haben Vorrang; die Sim bleibt unberührt.
  - **Zielerfassung und Feuererwiderung (D-087):** Einheiten ohne Befehl
    erfassen das nächste sichtbare, feindliche Ziel in Reichweite selbst —
    Gebäude eingeschlossen: die Verteidigungsplattform feuert erstmals.
    Deterministisch (Index-Reihenfolge, Ganzzahl-Distanzquadrat, Tiebreak
    kleinster Index); kein neuer CommandKind, kein Snapshot-Bump; die
    kanonischen Baselines blieben unverändert. Attack-Move bleibt eigener
    Sprint. Sechs neue Tests je Lane (tools: 428/428).
  - **Lebensbalken** über beschädigten oder selektierten Einheiten und
    Gebäuden (reine Präsentation).
  - **Ergebnisbildschirm** („Sieg"/„Niederlage"/„Unentschieden" mit Zeitpunkt)
    und **sichtbare Pause** (Overlay statt stillstehendem Bild); Knöpfe
    **Neue Runde** (vollständiger Neustart inklusive View-/Kamera-Reset) und
    **Hauptmenü** (designed extension point des Menüs). Anwendung beenden ist
    nicht mehr der einzige Ausweg.
  - **Ingame-Musik (D-086):** drei Suno-Themen als OGG-Playlist
    (`MusicDirector`) — Matchstart blendet ein, Ergebnis blendet aus, Pause
    läuft weiter, Menü kehrt zur Menümusik zurück; Lautstärke aus
    `GameSettings`, live anwendbar. Suno-Ausnahme erweitert
    ([docs/assets/Licenses.md](docs/assets/Licenses.md) 1.5.0).
  - **Kontrollgruppen 1–9** (Strg/⌘+Zahl setzen, Zahl abrufen, Tote fallen
    beim Abruf heraus) und **additive Auswahl mit Shift** (Klick und Box).
  - **Ablehnungsgründe sichtbar:** ein abgelehnter Befehl erscheint als kurze
    Einblendung über der Bauleiste statt nur im F3-Panel; die Statuszeile
    zeigt im Leerlauf die Kamerabelegung (MMB/Space).
- **Kaserne-Diagnose (Sprint 09 §2.1):** der Befund „keine Soldaten" ist im
  Harness **nicht reproduzierbar** — zwei neue PlayMode-Suiten
  (`GrayboxDemoProofTests.Barracks_ProducesVisibleInfantry`,
  `BarracksSpawnDiagnosisTests`) beweisen Spawn in der Simulation UND sicht-
  bare View in einem echten Match. Ursache in der gemeldeten Sitzung mit
  hoher Wahrscheinlichkeit die damals noch pausierte Baustelle (D-085-Fix)
  bzw. halbiertes Produktionstempo bei LOW POWER.

### Behoben
- **Harvester erntete nicht** (Client-Dispatch, s. o. — gleiche Fehlerklasse
  wie der Builder in D-085).
- **Rally-Geste kaperte den Rechtsklick** bei gemischter Rahmenauswahl —
  Rally gilt nur noch bei ausschließlich selektierten Gebäuden.
- **Randscroll unter der Bauleiste/Minimap:** die Kamera unterdrückt Edge-Pan
  über HUD-Panels (`HudPointerLink`).
- **Weltgesten unter dem Hauptmenü:** Eingabe ist gesperrt, solange das Menü
  sichtbar ist.

### Hinzugefügt
- **Hauptmenü, Menümusik und Einstellungen (D-083):** Wer das Spiel startet,
  landet nicht mehr mitten in einem Match, sondern vor einem Menü mit Key Art,
  Titel „HASHKRIEG" und Musik — und kann das Spiel zum ersten Mal auch wieder
  sauber verlassen.
  - **Vier Einträge — Neues Spiel / Laden / Einstellungen / Beenden.**
    „Neues Spiel" ruft `MatchBootstrap.StartGrayboxMatch()` und blendet das
    Menü aus; „Beenden" ruft `Application.Quit()` (im Editor: Play-Modus
    beenden). Das Menü ist ein **Overlay in der bestehenden `Bootstrap.unity`**,
    keine zweite Szene — das Projekt hat weiterhin **null**
    `SceneManager`-Aufrufe im Produktionscode, und das Menü-Objekt entsteht im
    `BootstrapSceneGenerator`, weil die Szene Maschinenausgabe ist.
  - **„Laden" ist sichtbar, aber ausgegraut** („kommt später"). Die
    Snapshot-Schicht serialisiert den vollständigen Matchzustand und setzt ihn
    hash-identisch fort — aber nichts schreibt je auf Platte, es gibt kein
    Save-Format und keine Slots. Den Eintrag zu verstecken ließe offen, ob das
    Spiel überhaupt speichern kann; ihn anzubieten ließe den Spieler ins Leere
    greifen.
  - **Einstellungen mit Persistenz:** Musik an/aus + Lautstärke, SFX an/aus +
    Lautstärke, Render-Detail über die sechs Quality-Level, vSync, Auflösung,
    Vollbild. Gespeichert wird als lesbares JSON in
    `Application.persistentDataPath/settings.json` — **kein `PlayerPrefs`**
    (nicht inspizierbar, nicht löschbar ohne Werkzeug) und **kein `AudioMixer`**
    (die Musiklautstärke geht direkt auf `AudioSource.volume`). Angewandt wird
    beim Start über `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`, also
    ohne Boot-Objekt; eine kaputte Datei fällt auf die Vorgabewerte zurück,
    statt den Start zu blockieren.
  - **Ehrlich zu den Grenzen:** Der **SFX-Regler wirkt auf nichts** — es gibt
    noch keine SFX; der Wert wird gespeichert und im UI als wirkungslos
    gekennzeichnet. Und alle sechs **Render-Detail-Stufen teilen sich ein
    URP-Asset**: 19 Felder unterscheiden sich real (`lodBias` 0,3–2,0,
    Anisotropie, Partikelbudget), `renderScale`, Schatten und MSAA dagegen
    nicht.
  - **Erstes echtes UI Toolkit-UI:** damit ist der UI-Stack für alles Neue
    gesetzt. `com.unity.modules.uielements` ist ein Engine-Modul und brauchte
    keinen asmdef-Eintrag — `Nova.Presentation.UI.asmdef` referenziert
    weiterhin ausschließlich `Nova.*`-Assemblies. Das bestehende
    `OnGUI`-HUD bleibt Wegwerfcode und wird nicht portiert.
  - **Assets im Repo, Lizenzlage geklärt:** Key Art (OpenAI Image API),
    Menümusik (Suno, Bezahltarif, nahtlos geloopt) und die Schrift Rajdhani
    (OFL-1.1 samt `OFL.txt`) liegen unter `Assets/_Project/UI/` und
    `Assets/_Project/Audio/` — bewusst nicht unter `Art/`, das ist gitignored,
    und ein frischer Clone hätte ein schwarzes, stummes Menü **ohne
    Fehlermeldung**. Freigabe und Ledger-Zeilen:
    [docs/assets/Licenses.md](docs/assets/Licenses.md) 1.3.0 → 1.4.0, inklusive
    benannter Ausnahme von der harten 0-€-Regel für den Suno-Tarif.

### Geändert
- **Das Spiel startet nicht mehr automatisch ins Match.**
  `MatchBootstrap.AutoStart` steht im Szenengenerator auf `false` (D-083). Das
  ist die spürbarste Verhaltensänderung dieses Stands und betrifft jeden, der
  die Demo vorführt oder testet: Play führt ins Hauptmenü, das Match beginnt
  erst mit „Neues Spiel". [Das Demo-Runbook](docs/production/DemoRunbook.md)
  ist entsprechend auf 0.4.0 gezogen — der Ablaufvorschlag beginnt jetzt am
  Menü, und die Zeitmarken zählen ab Matchstart. PlayMode-Tests, die auf
  `IsMatchReady` warten, müssen das Match explizit über `StartGrayboxMatch()`
  starten.
- **4× MSAA statt gar keiner Kantenglättung** (Inhaberentscheidung 2026-08-06).
  `Assets/_Project/Settings/NovaUrp.asset` stand auf `m_MSAA: 1`, was in URP
  „aus" bedeutet — und da auch keine Post-Process-Kantenglättung gesetzt war,
  rendert das Spiel bis hierher vollständig ungeglättet. Der Wert steht jetzt
  auf `4`.
  Damit klärt sich zugleich ein Nebenbefund, der wie ein Fehler aussah: das
  wandernde `antiAliasing`-Feld in `ProjectSettings/QualitySettings.asset` ist
  **kein Schalter, sondern ein Abfallprodukt**. URP liest beim Start
  `m_MSAA` aus dem URP-Asset und schreibt das Ergebnis dorthin zurück
  (`UniversalRenderPipeline.cs`, Zweig `msaaSampleCountNeedsUpdate`). Wer MSAA
  ändern will, ändert das URP-Asset; der QualitySettings-Wert folgt von selbst
  und ist kein sinnvoller Ort für einen Konflikt.
  Weiterhin gilt: alle sechs Quality-Stufen teilen sich dieses eine URP-Asset,
  der Render-Detail-Regler im Menü ändert MSAA also **nicht**.

### Hinzugefügt
- **Bedienbares HUD (D-084, Graybox-Sitzung GB-006):** Die Demo ist jetzt ohne
  Tastenbelegungs-Vorwissen spielbar — alles Sichtbare ist auch anklickbar.
  - **Selektions-Feedback:** grüner Bodenmarker unter jeder selektierten
    Einheit und jedem Gebäude; Drag-Box-Rechteck während der Rahmenauswahl.
  - **Bauleiste** am unteren Bildschirmrand: alle neun MS-1-Gebäude mit Name,
    Hotkey, Kosten und Bauzeit; nicht verfügbare Einträge ausgegraut **mit
    Grund** („benötigt X" / „nicht genug Aetherium"). Klick oder Hotkey öffnet
    die **Platzierung mit Ghost-Vorschau** (grün = gültig, rot = ungültig;
    LMB platziert, RMB/ESC bricht ab). Datenquelle ist `SimDefinitions` —
    `BuildingRegistrySO` hat keine Instanzen und ist nicht verdrahtet (D-084).
  - **Onboarding-Hinweis** beim Start („1. Raffinerie bauen (Y) 2. Harvester
    produzieren (Q) 3. Aetherium ernten (H)"), verschwindet, sobald der
    Wirtschaftskreislauf läuft.
  - **Command Cards:** Kontextpanel für Einheiten (Bewegen/Angreifen/Stopp,
    Harvest/Return für Harvester, Repair-Zielwahl für Builder) und für Gebäude
    (Verkaufen +50 %, Reparieren, Produktions-Queue mit Fortschrittsbalken und
    Abbruch pro Eintrag, Baustellen-Abbruch +75 %; InstallDefenseModule
    ehrlich deaktiviert — G2/G4-Inhalt). Buttons feuern dieselben Intents wie
    die Hotkeys; `CommandCardPresenter` ist das Unity-freie, testbare
    Rollen-Mapping (18 neue EditMode-Tests).
  - **Rally-Point per Rechtsklick:** mit selektiertem Produktionsgebäude setzt
    RMB den Sammelpunkt — sichtbar als Flagge plus Linie vom Gebäude.
  - **Kamera:** Rotation zusätzlich auf mittlerer Maustaste + Drag, **Space**
    setzt die Rotation zurück (Z/X bleibt).
  - **Fog of War sichtbar:** unerkundet schwarz, erkundet abgedunkelt,
    sichtbar klar — als Welt-Overlay aus dem committed Team-View (5 Hz),
    keine Sim-Mutation.
  - **Minimap** unten links: Gelände, Fog-Status, Fraktionspunkte (Feinde nur
    in sichtbaren Zellen), Kamera-Viewport-Rahmen; Klick springt dorthin
    (Kamerakanal über `MinimapCameraLink`, da die beiden Presentation-
    Assemblies einander nicht referenzieren dürfen).
  - **HUD-Chrome:** einheitliche dunkle Panels für Statusleiste, Bauleiste,
    Command Card und Minimap; das F3-Diagnose-Panel bleibt bewusst schlicht.

### Behoben
- **Gebäude rotierten bei Rechtsklick:** Bewegungsbefehle gingen an die
  gesamte Selektion inklusive Gebäuden, und die View schrieb Sim-Rotation auf
  Gebäudemodelle. Jetzt filtriert die Eingabe unbewegliche Rollen aus
  Move/Attack/Stop/Harvest/Return heraus und `UnitViewManager` schreibt keine
  Rotation mehr auf Gebäude-Views.

### Verifikation (GB-006)
- `dotnet test tools/Nova.SimRunner.Tests`: **420/420 grün** (Sim unberührt).
- Unity-EditMode: **445/445 grün** (+18 CommandCardPresenter-, +2
  SelectionManager-Tests); PlayMode **2/2** mit frischen Screenshots in
  `output/demo/` (Nebel-Overlay visuell bestätigt).
- Interaktiver Durchlauf durch den Inhaber steht als DoD-Punkt noch aus.

### Hinzugefügt
- **Baubarkeit, HUD-Zonen und Kartenbild (D-085, Sprint „Baubarkeit und
  Kartenbild"):** Was die Bauleiste verspricht, passiert jetzt auch — ein
  gesetztes Gebäude wird tatsächlich gebaut, und das Spiel sagt, was es tut.
  - **Builder-Auto-Dispatch beim Platzieren:** zusätzlich zum Bau-Befehl
    geht ein normaler Move-Befehl an den Builder, den die Simulation der
    Baustelle zuweisen wird (eigener Builder mit kleinstem Entity-Index —
    dieselbe Spiegelung wie `CommandCardPresenter.TryFindRepairBuilder`),
    auf dieselbe deterministische Nachbarzelle, die die KI ansteuert
    (westlich des Footprints, Ostseite als Kartenrand-Fallback). Läuft über
    den bestehenden Command-Pfad: kein neuer `CommandKind`, kein
    StateVersion-Bump, Hashes/Replays/Fingerprints unverändert. Ohne
    lebenden Builder bleibt das Platzieren erlaubt (Abbruch erstattet 75 %),
    aber Statuszeile und Log warnen sofort.
  - **Baustellen-Zustand auf der Command Card:** eine Zeile in der
    Auswertungsreihenfolge der Simulation — „Kein Builder — Bau pausiert.
    Builder im HQ bauen." → „Builder unterwegs" → „im Bau, 43 % — fertig in
    ~12 s". Prozent und Restzeit kommen aus `BuildTicks`, `ProgressRaw` und
    `ProductionSpeedMultiplierQ16`, nicht aus einer Schätzung; die Regeln
    stecken in `ConstructionSiteStatus` (Nova.Gameplay, Unity-frei, EditMode-
    getestet).
  - **Pausierte Baustellen in der Welt sichtbar:** `ConstructionSiteMarkerView`
    legt einen Bodenrahmen auf jede eigene Baustelle — ruhig bei
    wachsenden, pulsierend amber bei pausierten (kein Builder oder Builder
    noch nicht in Reichweite).
  - **HUD-Zonenmodell (`HudLayout`):** eine Klasse besitzt alle Panel-
    Rechtecke und ist die einzige Stelle, die `Screen.width/height` liest —
    Statusstreifen oben, Minimap unten links, Bauleiste unten mittig,
    Command Card rechts über der Leiste, F3-Panel im verbleibenden freien
    Feld (nie in der Spalte der Card). Überlappung ist damit
    konstruktionsbedingt ausgeschlossen statt pro Panel geflickt; die
    Zonenmathematik (`HudLayoutMath`, Nova.Gameplay) ist EditMode-getestet.
  - **F3-Panel lesbar:** opaker Hintergrund (`HudChrome.OpaquePanelStyle`
    statt `GUI.skin.box`), Höhe auf die Zone begrenzt, Inhalt in einer
    `GUI.ScrollView`.
  - **Kartenbild ohne ein einziges Asset:** prozedurale 512×512-Sandtextur
    zur Laufzeit (deterministisches Wert-Rauschen mit festem Seed, vier
    Sandtöne plus niederfrequente Flecken, nahtlos gekachelt 32×32), 84
    deterministisch gestreute Felsen aus Primitiven ohne Collider (ausgespart
    um Startbasen und Aetheriumfelder), schräge warme Sonne mit weichen
    Schatten, sandfarbener Distanznebel und Ambient-Gradient (im
    Szenengenerator gesetzt — `RenderSettings` ist szenen-serialisiert),
    Kartenrand als drei Zellen breite Verwitterungszone per Overlay-Schleier
    statt des flachen dunklen Balkens, Minimap-Unerkundet als stark
    abgedunkelte Geländesilhouette (~15 %) statt Schwarz.

### Geändert
- **Bauleisten-Buttons auf zwei Zeilen (62 px):** Name (Hotkey) und
  Kosten · Zeit — `wordWrap` aus, harte Kürzung mit „…" gegen die echte
  Stilbreite, kein Label kann mehr aus einem Button laufen. Der Sperrgrund
  („benötigt Raffinerie" / „nicht genug Aetherium") wandert aus dem Button
  in die Statuszeile über der Leiste und erscheint beim Überfahren; dieselbe
  Zeile trägt die Builder-Warnung und den Onboarding-Hinweis.

### Behoben
- **Rechtsklick kannte die HUD-Sperre nicht:** RMB auf Bauleiste, Minimap
  oder Command Card schickte die Armee an den Punkt dahinter — der Zweig
  prüft jetzt `IsPointerOverHud` wie die drei anderen Klickpfade.
- **Roter Baugeist platzierte trotzdem:** der Platzierungsklick prüfte
  `_placementHasCell` statt `_placementValid`; ein ungültiger Klick (auch
  am Kartenrand, wo `ToGridCoordinate` negative Ursprünge still auf 0
  klemmte) setzte das Gebäude woanders hin als der Geist zeigte. Jetzt
  platziert nur der grüne Geist, ein Fehlklick lässt den Geist scharf
  (RMB/ESC bricht ab).
- **Command Card unten abgeschnitten:** `EstimateHeight` rechnete weder
  GUILayout-Margins noch Panel-Padding mit (~40 px) — die untersten Knöpfe
  lagen außerhalb der `BeginArea` und waren nicht klickbar. Die Höhe zählt
  jetzt Zeile für Zeile mit den echten Stil-Werten.

### Verifikation (D-085)
- `dotnet test tools/Nova.SimRunner.Tests`: **420/420 grün** — null Sim-
  Änderung, wie vom Sprint gefordert.
- SimRunner: Hash `0x2FBEC31FBC0BF430` (Standard) und Fingerprint
  `0xF866FDC042D260E1` / Final `0xD8650F4DEDE1494C` (DETERMINISM_10000) —
  **identisch zum Stand vor dem Sprint**.
- `dotnet build` aller betroffenen Projekte: 0 Fehler, 0 Warnungen;
  EditMode-Spiegeltests für `ConstructionSiteStatus` und `HudLayoutMath`
  kompilieren grün (laufen wie alle EditMode-Tests nur in Unity).
- Szenen-Regeneration und PlayMode-Durchlauf stehen als Folgeschritt aus
  (der Szenengenerator verdrahtet die neuen Komponenten; `Bootstrap.unity`
  ist Maschinenausgabe).

### Hinzugefügt
- **Spielbarer RTS-Core-Loop (D-077, Graybox-Sitzung GB-005, ohne Gate-Status):**
  Die Demo ist erstmals ein spielbares, kleines 1v1 im C&C-Stil statt einer
  technischen Testszene.
  - **Start wie im Genre-Original:** jeder Slot startet mit Hauptquartier,
    einem Builder und 3.000 AE (vorher: gratis Raffinerie, zwei Harvester und
    Infanterie). Der Harvester wird jetzt von der **Raffinerie** produziert,
    die Raffinerie setzt **kein Kraftwerk mehr voraus** (ihr Power-Bedarf
    bleibt — ab Raffinerie + Kaserne wird ein Kraftwerk nötig).
  - **Der Computergegner spielt:** `SkirmishAiSystem` (Legion, Slot 1) ist in
    `MatchRunner` registriert und handelt über denselben Intent-Pfad wie ein
    Mensch — Build-Order (Raffinerie, Kaserne, bei Bedarf Kraftwerk), zwei
    Harvester im Auto-Kreis, Infanterieproduktion bis zwölf, Angriffswellen ab
    sechs Kampfeinheiten mit expliziten, Fog-of-War-legalen Attack-Orders.
    End-to-End-Test: die KI besiegt einen passiven Slot deterministisch bei
    Tick 2.242.
  - **Sieg bei Hauptquartier-Zerstörung:** wer sein HQ verliert, ist besiegt
    (zusätzlich zur Totalvernichtung; gleichzeitiger Verlust bleibt Unentschieden).
    Victory-Snapshotblock v1 → v2.
- **Kompakte Statusleiste** (Credits, Power, Spielausgang) als immer sichtbares
  Minimum, bis die echte UI landet.

### Behoben
- **Vollbild-Debug-Overlay entfernt:** das `DebugHud`-Diagnose-Panel ist
  standardmäßig aus und per **F3** zuschaltbar; Szene per
  `BootstrapSceneGenerator` neu erzeugt (Maschinenausgabe).
- **Übereinanderliegende 3D-Assets:** die `PF_*`-Modelle sind nach
  Art-Konvention 3,0 m/Zelle exportiert, die Sim-Welt rechnet 1 Zelle = 1
  Welt-Einheit — Gebäude überlappten, Einheiten steckten unsichtbar in den
  Meshes. `UnitViewManager` normalisiert Prefab-Views jetzt zur Laufzeit aus
  den Mesh-Bounds auf den Sim-Footprint; Modelle bleiben ohne Logikänderung
  austauschbar.
- **Rally-Point auf der Raffinerie** wurde abgelehnt, weil die
  Produzentenrollen-Prüfung den D-077-Umzug des Harvesters nicht kannte —
  sie liest die Rollen jetzt aus der Definitionstabelle.

### Verifikation (GB-005)
- `dotnet test tools/Nova.SimRunner.Tests`: **420/420 grün**; SimRunner-
  Determinismus- und DETERMINISM_10000-Self-Check PASS.
- Unity-EditMode: **425/425 grün** (Batchmode, erstmals inklusive
  InitialStateHash-Parität Bootstrap == Szenario); PlayMode **2/2** mit
  frischen Screenshots in `output/demo/` (Skalierung visuell bestätigt).
- Vertrag `quality/content/mvp-v1.json` 1.0.0 → **1.2.0**.

### Geändert
- **Governance auf Tier 1 zurückgeschnitten (D-076).** Das Repository trug ein
  Regelwerk für ein Projekt mit fremden Beitragenden, Nutzern und Haftung –
  bei zwei Entwicklern war es der Blocker statt das Netz. Zahlen zum Ist-Stand:
  19.729 Zeilen Doku gegen 19.508 Zeilen Spielcode, 6.333 Zeilen
  Governance-Tooling, 59 `docs`- gegen 35 `feat`-Commits und **null** erzeugte
  Gate-Evidence in 148 Commits.
  - Neu: [GOVERNANCE.md](GOVERNANCE.md) regelt die Prozessstrenge über drei
    **Tiers** mit benannten Auslösern. Regeln höherer Tiers werden schlafen
    gelegt statt gelöscht, inklusive dokumentiertem Weckpfad.
  - Die Gate-Kette G0–G5 blockiert keinen Meilensteinfortschritt mehr. Der
    Evidenzvertrag (Receipts, Trusted Tooling, `environmentId`-Bindung) ruht bis
    Tier 3; die Gate-*Inhalte* bleiben als Arbeitsgliederung gültig.
    `quality/` ist vollständig erhalten – siehe [quality/README.md](quality/README.md).
  - Neuer Meilenstein-Nachweis: grüne CI plus eine gespielte und protokollierte
    Runde, statt einer autorisierten Receipt-Kette.
  - Definition of Done 13 → 4 Punkte, PR-Template 11 → 3 Checkboxen.
  - Dokument-Pflichtaufbau, Versionsbump und Änderungsverlauf-Tabelle sind
    freiwillig (Git ist der Änderungsverlauf); `quality/content/mvp-v1.json`
    bleibt versionierter Vertrag. Die ≥3-Alternativen-Pflicht je D-ID ruht bis
    Tier 2, das Sprint-Ritual entfällt.
  - Selbst-Merge bei grüner CI erlaubt. Die Regel „ab zwei aktiven Maintainern
    zweite menschliche Freigabe Pflicht" ist gestrichen – sie hätte genau jetzt
    gegriffen.
  - D-067 (Graybox-Ausnahme, nie ratifiziert) ist damit gegenstandslos; der
    GrayboxLog verliert seine Pflicht, der [ScopeLedger](docs/production/ScopeLedger.md)
    bleibt als ehrliche Lückenliste.

### Geändert
- **3D-Assets liegen als Paket ausserhalb des Repositories.** Der MS-1-Art-Stand
  umfasst rund 105 MB Binärdaten (92 MB PNG, 13,7 MB FBX) und hätte das 77 MB
  grosse Repository mehr als verdoppelt — dauerhaft, da Git-Historie Binärdaten
  nicht vergisst und ein späterer Ausbau einen auf `main` verbotenen
  History-Rewrite bräuchte. Ausgeschlossen wird der **vollständige** Art-Inhalt
  (`*.fbx`, `*.png`, `*.mat`, `*.prefab` samt `.meta`), nicht nur die Binaries:
  Bliebe ein Prefab ohne sein Mesh im Repo, hätte ein frischer Clone unsichtbare
  Einheiten — ohne Prefab fällt `UnitViewManager` sauber auf Graybox-Primitive
  zurück und ein Clone bleibt immer spielbar. `AssetMappingRegistry.asset` wird
  wieder leer eingecheckt (Maschinenausgabe des Imports). Im Repo bleiben die
  34 `PROVENANCE.json` als Lizenz- und Herkunftsnachweis. Paketinhalt,
  SHA-256, Installations- und Erweiterungsablauf:
  [docs/assets/AssetPackage.md](docs/assets/AssetPackage.md).

### Hinzugefügt
- **CI führt erstmals die Spieltests aus.** Neuer Pflicht-Workflow
  [`tests`](.github/workflows/tests.yml): Simulationstests aus
  `tools/Nova.SimRunner.Tests`, das dieselben Core-/Simulation-Quellen wie der
  Unity-Host kompiliert und keine Unity-Lizenz braucht (~8 s lokal). Bis dahin
  prüfte die CI ausschließlich Markdown-Links und die Selbsttests des
  Evidence-Validators – also die Governance, nicht das Spiel.
- `integrity` läuft nur noch bei Änderungen an `quality/**`, statt jeden PR zu
  belasten; so bleibt der schlafende Apparat lauffähig.
- `docs-check` prüft weiterhin hart tote interne Links, UTF-8 und die
  Parsebarkeit der Quality-JSONs, erzwingt aber keinen Dokument-Pflichtaufbau
  und keinen Versionsbump mehr; Node/Ajv ist aus diesem Job entfallen.
- **Demo-Beweis, Wirtschaftsfix und Asset-Integration (Graybox-Sitzung
  GB-004, Diagnosestand, ohne Gate-Status, ohne Evidence):** Erstens ist der
  Harvester-Kreis repariert — `EconomySystem.HasOwnRefineryInReach` misst die
  Abgabe-Reichweite jetzt am Gebäude-Footprint statt am Footprint-Zentrum
  (die Start-Harvester standen Chebyshev 2 vom Zentrum, ihre volle Fracht
  blieb ewig liegen und die Credits froren bei 1.000 AE); zwei
  Regressionstests je Lane, Fingerprint/Checkpoint-Tick-100 bleiben
  unverändert (`0x71045DC037C10250` / `0x9A2B01F88C03599D`), nur der finale
  Zustandshash wandert (`0x29DE64BD1B6A9000` → `0xF25B56F8C3553AAC`).
  Zweitens ist das Projekt jetzt wirklich URP: `UrpProjectSetup` erzeugt und
  verankert `Assets/_Project/Settings/NovaUrp(Renderer).asset` in
  GraphicsSettings und allen Quality-Stufen (zuvor nie zugeordnet — URP-Lit-
  Materialien liefen magenta); Blockout und Graybox-Primitive nutzen
  Laufzeit-URP-Lit-Materialien. Drittens sind alle 34 Tripo-Assets integriert
  (34/34 Materialien + LOD-Prefabs via `ArtAssetPrefabBuilder`,
  Registry-Sync) und alle 17 MS-1-Rollen per Tastatur erreichbar (Pause auf P;
  HQ bewusst unbelegt). PlayMode-Beweistest neu (`Assets/Tests/PlayMode/`):
  lädt die Bootstrap-Szene, prüft Match-Start, Ticks, sichtbare Views und
  wachsende Credits (Log: `tick 30→200, credits 1000→1660 AE`) und schreibt
  fünf Screenshots (`output/demo/`). Verifiziert: 412/412 EditMode-,
  408/408 .NET-, 2/2 PlayMode-Tests, `DETERMINISM_10000` SelfCheck grün.
- **Asset-Bereitschaft, erste Karte und Demo-Vorbereitung (Graybox-Sitzung
  GB-003, Diagnosestand, ohne Gate-Status, ohne Evidence):** Die Art-Ablage
  `Assets/_Project/Art/` existiert jetzt vollständig nach ArtAssetStandard
  (2 Fraktionen × 9 Gebäude- und 8 Einheiten-Rollen, `Shared/`, `Source/`),
  und ein Drop-in-Pfad macht jedes konventionkonform abgelegte Asset sofort
  spielbar: `ArtAssetNaming` (Nova.Data) parst `PF_UNIT_/PF_BLDG_<Faction>_<Role>`
  auf die kanonische Definitions-Id, `ArtAssetAutoSync` (Editor) registriert
  jedes solche Prefab beim Import automatisch in der neuen
  `AssetMappingRegistry.asset` und stempelt die Standard-Import-Settings
  (Scale 1.0, keine FBX-Materialien, BC7, Masken linear) auf `Art/`-Importe;
  der `UnitViewManager` rendert eine registrierte Definitions-Id als Prefab
  (Fraktion×Rolle aufgelöst, Pooling pro Prefab), alles andere bleibt
  Graybox-Primitiv. Erste Karte: `GlutrinneBlockoutView` (Wüstentönung,
  Kartenrand, Kristallmarker auf den zwei real registrierten Feldern) plus
  Datenasset `MAP_Glutrinne.asset` (Graybox-Teilmenge des Manifest-Layouts).
  Doku: `docs/production/DemoRunbook.md` (Demo-Ablauf, Steuerung, bekannte
  Grenzen, Ablage-Anleitung) und `docs/production/StatusSnapshot_2026-08-05.md`
  (datierter Projektstand; Inventur: noch kein einziges 3D-Asset vorhanden).
  Verifiziert: 410/410 EditMode-Tests (+5 neue Namenskonventions-Tests),
  406/406 .NET-Tests, Szenen-Regenerierung headless grün; Simulation/Core
  unberührt, `quality/**` unberührt.
- **Fraktionswirtschaft und Sichtbarkeit der Fraktionsachse (Paket 3+4 der
  Fraktions-Sitzung, Diagnosestand, ohne Gate-Status, ohne Evidence):**
  Erstens ist die Harvester-Ladekapazität kein flaches Provisorium mehr:
  `UnitState.DefaultCargoCapacityAE` ist als einzige Quelle entfallen, die
  Kapazität lebt als `SimUnitDefinition.CargoCapacityAE` in der
  Harvester-Definitionszeile je Fraktion (Allianz 330, Legion 300 —
  `factions[i].identity.harvesterCargoAE` des Manifests) und ist das 21.
  Feld des kanonischen Definitions-Hash-Layouts. Das `EconomySystem`
  klammert die Ernte an der Fraktion des Besitzer-Slots: der
  Legion-Harvester stoppt bei 300, der Allianz-Harvester bei 330. Die
  Entity-Store-Snapshotvalidierung deckelt Cargo auf das
  fraktionsübergreifende Maximum (`MaxHarvesterCargoCapacityAE` = 330),
  weil ein Block beide Fraktionen enthalten kann; Überladungen mit
  `ISlotFactionLookup` prüfen zusätzlich die pro-Entity-Fraktionsgrenze
  (Legion-Cargo 320 wird abgelehnt, Allianz-Cargo 320 nicht) — die
  kanonische Zwei-Phasen-Wiederherstellung bleibt bewusst auf der
  Hartgrenze, weil sie zur Validierungszeit keine blockübergreifende
  Fraktionssicht hat. Zweitens ist die Achse sichtbar: im `UnitViewManager`
  bestimmt jetzt die **Fraktion** die Farbe (D-072-Grundtöne über den neuen
  `FactionTint`-Helper, Tint per `MaterialPropertyBlock` mit beiden
  Properties `_BaseColor` und `_Color` — keine ScriptableObjects, keine
  Assets, keine Materialien), die Rolle weiterhin die Form; das Debug-HUD
  zeigt die Fraktion des lokalen Slots, beider Slots und der aktuellen
  Auswahl. Verifiziert: 406/406 .NET-Tests, 405/405 EditMode-Tests
  (handgespiegelt; die Farbtests laufen nur EditMode-seitig).
- **Fraktionsidentität ist Simulationswirklichkeit: Allianz und Legion spielen
  sich unterschiedlich (Diagnosestand, ohne Gate-Status, ohne Evidence):**
  `EconomySystem` modellierte bis dahin ausdrücklich „no faction differences" —
  beide Slots bauten aus derselben flachen Tabelle. Neu ist erstens die
  Fraktionsachse im Zustand: `FactionId : byte` (`Alliance = 0`, `Legion = 1`,
  der Wire-Wert IST der Manifestindex in `quality/content/mvp-v1.json`) wird je
  Spieler-Slot im Economy-Snapshotblock v2 serialisiert (v1 wird im offenen
  Pre-G1-Formatfenster abgelehnt, nicht migriert), geht als zweites 8-Byte-
  Slot-Array in den `MatchFingerprint` ein und wird von beiden kanonischen
  Setup-Pfaden (`MatchBootstrap` wie `Determinism10000Scenario.SetupMatch`)
  identisch gesetzt — Slot 0 Allianz, Slot 1 Legion —, sodass der
  InitialStateHash-Paritätstest beider Lanes unverändert grün bleibt. Zweitens
  trägt `SimDefinitions` jetzt 34 Definitionen (17 Rollen × 2 Fraktionen) mit
  der dokumentierten Id-Regel: die Allianz-Id IST der `UnitRole`-Wire-Wert
  (1..17), die Legion-Id addiert 17 (18..34) — jede Id ist global eindeutig,
  `CommandIds.IsValidDefinitionId` bleibt wire-kompatibel (`!= 0`). Die Werte
  kommen aus den GDDs (Buildings.md für Kosten/Bauzeit/Energie, Vehicles.md und
  Infantry.md für Einheiten, Weapons.md führend per D-047 für Waffen): Legion
  baut billiger und schneller, die Allianz reicht weiter; wo die GDDs keine
  konkrete Zahl nennen (Gebäude-HP, Fahrzeug-Projektilschaden der Legion),
  gilt die dokumentierte Integer-Prozent-Ableitung `(allianz × 85) / 100` aus
  der Allianz-Zeile — keine erfundenen Absolutwerte. Die Power-Bilanz, der
  Kampf (`WeaponProfiles` ist jetzt fraktions- UND rollenindiziert,
  `CombatSystem` erhält die Slot-Fraktionen über das neue schmale
  `ISlotFactionLookup`, implementiert vom `EconomySystem`), die
  Platzierungs-/Produktionsvalidierung (fremd-fraktionale DefIds werden wie
  unbekannte als `RejectedInvalidTarget` abgelehnt) und die
  Produktionskosten sind fraktionsaufgelöst. Drittens ist `DefinitionsHash64`
  echt: `SimDefinitions.ComputeDefinitionsHash64()` hasht alle 34 Zeilen
  kanonisch (XXH64, Domäne `NOVA_DEFINITIONS_V1`, aufsteigende Ids, uniformes
  20-Felder-Layout je Zeile) und ersetzt den bisherigen
  Leer-Stub im Szenario-Fingerprint — ein Replay mit mutiertem Waffenwert
  scheitert nachweislich am Fingerprint (Test in beiden Lanes). Das
  Szenario-Skript baut jetzt GDD-konform zuerst das Kraftwerk (das
  Startnetz 30/20 kann die Allianz-Kaserne mit 15 Verbrauch nicht versorgen).
  Bewusst weiterhin flach (dokumentiert, nicht vergessen): Harvester-Ladekapazität
  330 AE für beide Fraktionen (Legion 300 ist registrierte
  ScopeLedger-Schuld, Rückkehr-Gate G4), Bewegungsgeschwindigkeiten (GDD-Werte
  in m/s, Simulationsdomäne m/tick — Umrechnung unratifiziert,
  ScopeLedger-Kandidat) und die Verteidigungsplattform-Waffe
  (fraktionsneutrales Modul). Verifiziert: 401/401 .NET-Tests (+19 gegenüber
  382), 397/397 EditMode-Tests (+18 gegenüber 379, handgespiegelte Lanes),
  `DETERMINISM_10000`-SelfCheck grün mit erwartbar bewegten Hashes
  (Checkpoint Tick 100 `0x9A2B01F88C03599D`, finaler Zustandshash
  `0x309240E5B0EFFE6D`). Nicht enthalten: Salven/Flächenwirkung der
  Legion (`[salvo, splash]`, ScopeLedger), Evolvierte, jede
  Docs-Änderung außerhalb dieses Eintrags. `quality/**`,
  `.github/workflows/**` und `VERSION` blieben unberührt; G0, MS-0 und MS-1
  bleiben offen.
- **Kampf ist bewertbar und ein Match kann enden (Diagnosestand, ohne
  Gate-Status, ohne Evidence):** `CombatSystem` wandte bis dahin einen flachen
  Schadenswert von 15 auf jeden Angriff an — ein Kampfpanzer und ein Schütze
  waren offensiv identisch —, und `grep -rn Victory` fand im Repository nichts:
  ein Match konnte nicht enden. Neu ist erstens eine
  Schaden-gegen-Panzerung-Matrix in `Nova.Simulation.Combat`
  (`DamageType`, `ArmorClass`, `DamageMatrix`, `WeaponProfiles`): 36
  ganzzahlige Prozentwerte, angewandt als `(Basisschaden × Prozent) / 100` mit
  Abschneiden, keine Fließkommazahl und kein `SimFixed`; Reichweite und
  Abklingzeit sind seither rollenabhängig statt konstant, die Ziel- und
  Abklinglogik blieb unverändert. Zweitens `Nova.Simulation.Victory.VictorySystem`
  als achtes und letztes System nach Combat mit dem MS-1-Siegvertrag aus D-056:
  `Victory.Elimination`, `Draw.MutualAnnihilation`, `Draw.TimeLimit` bei Tick
  27.000, unwiderruflich eingerastet und in Snapshotblock 107 serialisiert, also
  Teil des kanonischen Zustandshashs. Eingebaut ist eine im Dokument nicht
  vorgesehene Eingriffssperre („Engagement-Latch"): nur Slots, die je eine
  lebende Entität besaßen, nehmen an der Eliminierungsentscheidung teil — ohne
  sie meldete jeder frische Host auf Tick 1 ein beidseitiges Remis. Drittens
  macht das Debug-HUD beides sichtbar (alle vier Ergebniscodes, Abstand zum
  Zeitlimit, Streitkräftezählung aus derselben Quelle, die den Sieg entscheidet,
  Waffenprofil der Auswahl samt Auflösung gegen jede in MS-1 getragene
  Panzerungsklasse), und Einheiten zeigen ihren Gesundheitsstand über den
  bestehenden `MaterialPropertyBlock` ohne ein neues GameObject. Verifiziert:
  Unity-Batchmode-Kompilierung ohne Fehler und ohne Warnungen, 379/379
  EditMode-Tests, 382/382 .NET-Tests (je +41 gegenüber 338/341, identische
  Differenz in beiden handgespiegelten Lanes), `DETERMINISM_10000`-SelfCheck
  grün mit neuer lokaler Baseline (Fingerprint `0xAF9FB211B6C9CACE`,
  Checkpoint Tick 100 `0x01D276820F5FFE15`, finaler Zustandshash
  `0xCB8A545B9710EF54`), zwei Läufe byte-identisch, macOS- und Windows-Player
  gebaut, macOS-Player ausgeführt (640 Ticks, alle acht Systeme initialisiert
  mit Victory an letzter Stelle, null Exceptions). Die Hash-Bewegung wurde
  **getrennt gemessen statt zugeschrieben**: der Fingerprint-Sprung stammt
  vollständig aus dem neuen Snapshotblock, die Zustandshash-Bewegung aus dem
  Kampfmodell. **Nicht verifiziert:** Look and Feel — ob sich das Konterdreieck
  wie eines anfühlt, entscheidet erst ein menschlicher Play-Durchlauf.
  **Nicht enthalten und offen protokolliert:** Es gibt keine Zielerfassung — ein
  Angriffsziel wird ausschließlich durch manuellen Klick gesetzt, Einheiten
  erwidern kein Feuer, Angriffsbewegung existiert nicht und die
  Verteidigungsplattform kann nie schießen; nur 3 der 36 Matrixzellen sind über
  die aktuellen Tastenbelegungen erreichbar; die Sichtbarmachung der letzten
  Einheiten nach D-056 ist berechnet und serialisiert, aber von nichts
  konsumiert; der Host tickt nach der Siegentscheidung unverändert weiter und
  es gibt keinen Ergebnisbildschirm; und `MatchFingerprint` hasht Inhalt
  weiterhin nur als Stub, sieht diese Waffenwertänderung also nicht. Alles
  registriert in [docs/production/ScopeLedger.md](docs/production/ScopeLedger.md)
  und [docs/production/GrayboxLog.md](docs/production/GrayboxLog.md) (Sitzung
  GB-002). `quality/**`, `.github/workflows/**` und `VERSION` blieben unberührt;
  G0, MS-0 und MS-1 bleiben offen.
- **Hashkrieg-Weltentwurf (Entwurf, kein Gate-Nachweis):**
  [docs/vision/Lore.md](docs/vision/Lore.md) (0.1.0) beschreibt Vorgeschichte,
  Aetherium-Ökonomie, die Fraktionen Allianz und Legion sowie den Grund ihres
  Konflikts als Quelle für Fraktionsidentität, Formensprache, Farbwelt und
  Einheitennamen. Neuer Arbeitstitel *Hashkrieg* beschlossen, im Bestand dieses
  Repositories aber noch nicht vollzogen — Repo, Code und übriger Wiki-Bestand
  laufen weiterhin unter *Project Nova*.
- **Concept-Art-Bildstandard (Entwurf, kein Gate-Nachweis):**
  [docs/assets/ConceptArtStyleGuide.md](docs/assets/ConceptArtStyleGuide.md)
  (0.1.0) legt Bildformat, Lichtsetzung, Farbwelt, Renderstil, Formensprache je
  Fraktion, Silhouetten- und Maßstabsregeln, eine wiederverwendbare Prompt-Vorlage
  und Abnahmekriterien für alle Hashkrieg-Concept-Art-Bilder fest.
- **34 Concept-Art-Entwürfe samt Herkunftsnachweis (Entwurf, kein Gate-Nachweis,
  keine Produktionsassets):** [docs/assets/concept-art/](docs/assets/concept-art/README.md)
  enthält je einen Entwurf pro Fraktion (Allianz/Legion) und Rolle (neun Gebäude,
  acht Einheiten) als PNG (`full/`, 1024 × 1024) und JPG (`web/`), einen
  Kontaktbogen, die verwendeten Prompts (`prompts.json`,
  `prompts-scrimage.txt`), zwei Reproduktionsskripte (`tools/`) und zwei
  Stilplatten (`style/`). Herkunft: KI-generiert mit OpenAI `gpt-image-1` über
  `v1/images/edits`, mit einer Materialtafel als Stilreferenz statt eines
  Objektbilds, um unerwünschte Silhouettenvereinheitlichung zu vermeiden;
  vollständiger Nachweis je Bild inklusive SHA-256, Prompt und Lizenzlage in
  `PROVENANCE.json`. Bekannte Schwächen offen dokumentiert: Farbdrift zwischen
  zwei Legion-Erzeugungsläufen, leichte Räumlichkeit statt strenger Frontalität
  bei Legion-Radar und Legion-Bauarbeiter. Es existiert weiterhin kein einziges
  3D-Asset im Projekt.
- **Graybox-Slice: das Spiel ist erstmals sicht- und bedienbar (Diagnosestand,
  ohne Gate-Status, ohne Evidence):** `Bootstrap.unity` enthielt bis dahin nur
  Kamera und Licht, und im Repo existierte keine Zeile Eingabecode; der
  vollständige `MatchRunner` wurde von nichts instanziiert. Neu sind ein
  RTS-Kamerarig (Pfeiltasten/Randschwenk, Rad-Zoom, `Z`/`X`-Drehung), ein
  Match-Bootstrap, der das kanonische Setup des Determinismus-Szenarios in eine
  laufende Szene portiert, ein testbarer Intent-Dispatcher (Normalisierung,
  Chunking an der 100er-Grenze des Command-Schemas, durchgereichte
  Reject-Gründe), Geräteeingabe mit Auswahl/Bewegen/Stop/Angriff/Ernte/
  Rückkehr/Bau/Produktion — jeder Befehl ausschließlich über
  `MatchRunner.Ingress.TrySubmitIntent`, kein MonoBehaviour mutiert
  Simulationszustand —, ein Debug-HUD und eine fog-of-war-getriebene
  Einheitendarstellung, die für verborgene Einheiten gar keinen Proxy erzeugt
  (Form kodiert Rolle, Farbe kodiert Spieler-Slot). `BootstrapSceneGenerator`
  verdrahtet alles im Code; die `.unity`-Datei bleibt Maschinenausgabe.
  Dazu drei Simulationskorrekturen im offenen Pre-G1-Formatfenster (D-068,
  Entwurf): beschränkter Flow-Field-Cache je Ziel statt eines einzigen globalen
  Feldes, `CostField.Epoch` mit Pathfinding-Snapshotblock v2 (Terrainänderungen
  waren zuvor für den kanonischen Zustandshash unsichtbar) und ein
  Harvester-Autozyklus ohne neuen Zustand. Verifiziert: Unity-Batchmode-
  Kompilierung von 13 Assemblies ohne Fehler, 338/338 EditMode-Tests, 341/341
  .NET-Tests, `DETERMINISM_10000`-SelfCheck grün mit neuer lokaler Baseline
  (Fingerprint `0xB455B5E3A0752A36`, Checkpoint Tick 100
  `0x75C54A435FCFAB06`, finaler Zustandshash `0x87F889400D1B6C8C`), macOS-
  und Windows-Player gebaut, macOS-Player ausgeführt (zwei Läufe, null
  Exceptions, alle sieben Systeme initialisiert). **Nicht verifiziert:** Look
  and Feel (kein Rendering im Headless-Lauf) und der Windows-Player, der auf
  diesem macOS-Host nicht gestartet werden konnte. **Nicht enthalten:** Kampf
  ist mit einem flachen Schadenswert ohne Rüstung und Schadenstypen nicht
  bewertbar, es gibt keine Siegauswertung (ein Match kann nicht enden), keine
  KI auf Slot 1, keine Pause und kein Save/Load. Neue Governance-Dokumente
  [docs/production/GrayboxLog.md](docs/production/GrayboxLog.md) (append-only
  Sitzungsprotokoll) und
  [docs/production/ScopeLedger.md](docs/production/ScopeLedger.md) (21
  Registerzeilen, die auf Manifest-Schlüsselpfade zeigen statt Werte zu
  kopieren); die Root-README erklärt Editor-Start, Steuerung und beide Player
  einschließlich Gatekeeper- und SmartScreen-Hinweis. `quality/**`,
  `.github/workflows/**` und `VERSION` blieben unberührt; G0, MS-0 und MS-1
  bleiben offen.
- **G1-Determinismus-Harness DETERMINISM_10000 (macOS-arm64-Hälfte der V1-Messung,
  ohne Gate-Status, ohne Evidence):** `tools/Nova.SimRunner` führt jetzt das
  Szenario `DETERMINISM_10000` aus
  [quality/scenarios/mvp-v1.json](quality/scenarios/mvp-v1.json) aus — CLI
  `--scenario DETERMINISM_10000 [--verify <andere-plattform.json>] [--out <dir>]
  [--platform <tag>] [--ticks 10000] [--checkpoint-interval 100]` (Defaults =
  Vertragswerte; 100er-Checkpoint-Intervall ist die dokumentierte Harness-Wahl,
  SimulationCore.md §9 sagt „je Checkpoint" ohne Zahl: 100 Checkpoints +
  Finalzustand = 101 Hash-Pins). Zweiphasig: Ein dokumentierter, im Code
  deterministischer Generator baut ein kanonisches Match (MS-1-Manifest-
  Startzustand pro Slot plus dokumentiertes Vier-Einheiten-Scharmützel im
  Mittelfeld) und treibt über 10.000 Ticks ein fixes Skript über beide aktiven
  Slots (Slot 0 Mensch via Intents, Slot 1 „KI" via Wire-Records: Ernte/
  Rückkehr-Zyklen, Bau inkl. Abbruch, Produktion inkl. T2 nach Labor und
  Stornierung, Rallypunkte, Bewegung, Fokusfeuer-Angriffe — alle Domänen,
  kein Zufall außerhalb des Sim-PRNG) und zeichnet jeden Tick als kanonischen
  NOVA_REPLAY_CHAIN_V1-Container auf; die gemessene Wiedergabe stellt den
  eingebetteten Startsnapshot auf frischem Host wieder her, spielt jeden Tick
  über denselben versiegelten Pfad wie `ReplayPlayer` (inkl. wertexakter
  Ergebnis-Verifikation) und pinnt alle 100 Ticks `kernel.CalculateStateHash()`
  sowie am Ende `kernel.SaveSnapshot()` (Länge + SHA-256). Artefakte strikt
  nach D-062-Namensschema unter `output/` (gitignoriert, **keine Evidence**):
  Plattform-Profil `scenario.DETERMINISM_10000.<plattform>.json` (dokumentierte
  Wahl, da D-062 keine Metrik für Hash-Serien festlegt — u64 als Hex-Strings,
  Plattformblock in der Vokabular der Evidence-`environments` plus
  `runtimeVersion`/`dotnetSdk` aus global.json) und die Bool-Assertions
  `managed-path-only` (in dieser Managed-.NET-Lane trivial wahr, dokumentierte
  Selbstauskunft) und `same-sources-and-determinism-defines`
  (`#if NOVA_FIXED_POINT`-Selbstauskunft des Builds; Quellidentität per
  csproj-Compile-Include derselben `Assets/_Project`-Quellen). Vergleichsmodus
  `--verify`: lädt das Profil der anderen Plattform, Assertions
  `exact-state-hash-every-checkpoint` und `exact-final-snapshot-bytes` sind
  [1] nur bei vollständiger Gleichheit, sonst [0] mit erster Abweichungsstelle
  und Exit ≠ 0 — Workflow für die spätere Windows-x64-Referenzmessung.
  Gemessene macOS-arm64-Hälfte (Apple M4 Max, .NET 8.0.21, SDK 8.0.318,
  Release): Generator 0,6 s + Wiedergabe 0,2 s (Prozess gesamt ~1,0 s),
  Fingerprint `0xB1126835B5F32BCF`, 100/100 eindeutige Checkpoint-Hashes
  (Tick 100 `0xD1B9E0D000E0A88A` … Tick 10000 `0x25E9E181B19B945C`), finales
  Snapshot 41.839 Bytes, SHA-256
  `1b85b1c166f216b9ab080e3a741b26da8e9412abfbf54818f62f57d7a3d63bb3`;
  dreimaliger Selbstvergleich über Prozessgrenzen hinweg byte- und hash-exakt
  (lokale Determinismus-Baseline, Replay-SHA-256 identisch). **Die
  Windows-x64-Hälfte auf Referenzhardware steht noch aus; V1 ist damit NICHT
  belegt.** .NET-Lane: 9 neue Harness-Tests (Kurz-Determinismus über 100
  Ticks, Generator-Reproduktion, Tamper-Erkennung mit Abweichungsstelle,
  striktes Artefakt-Schema); der vorbestehende SCALE_500-MiniRun-
  Speicher-Assertions-Test schlägt auf diesem Host weiterhin fehl (auch ohne
  diese Änderung, unverändert offen).
- **G1-Coverage-Messinfrastruktur (Diagnose, ohne Gate-Status, ohne Evidence):**
  `tools/Nova.Coverage/coverage.py` führt die .NET-Test-Lane mit
  Coverlet-Instrumentierung aus (`coverlet.msbuild` 6.0.4 als
  PrivateAssets-Referenz in `tools/Nova.SimRunner.Tests`; zwingend
  `/p:IncludeTestAssembly=true`, weil die Core-/Simulation-Quellen per
  Compile-Include INS Test-Assembly kompilieren und Coverlet die Test-Assembly
  sonst standardmäßig ausschließt; der In-Proc-XPlat-Collector lieferte auf
  diesem Host leere Reports) oder konsumiert einen vorhandenen
  Cobertura-Report und aggregiert die Line-Coverage pro G1-Scope aus
  [docs/tech/Testing.md](docs/tech/Testing.md) §4: `Nova.Simulation`
  (Simulation/**, ≥ 80 %), `Command` (CommandsV1/**, ≥ 90 %), `PRNG`
  (Core/SimRandom.cs, ≥ 90 %), `Serializer` (Snapshots/**, ≥ 90 %), `Hash`
  (Core/XxHash64.cs + SimHashWriter.cs, ≥ 90 %), `Replay` (Replays/**, ≥ 90 %)
  und `CommandInventory` (die Payload-Reader/Writer-Pfade der 13 aktivierten
  Command-Kinds = CommandPayloads.cs + CommandPayloadReader.cs +
  CommandPayloadWriter.cs, 100 %). Ausgabe: stdout-Tabelle, striktes
  `output/coverage/coverage-summary.json` (pro Scope name, linePercent,
  requiredPercent, coveredLines, coverableLines samt ungedeckten Zeilen) und
  der Cobertura-Report samt SHA-256 daneben (`output/` ist gitignoriert,
  **keine Evidence**); Schwellen per `--set Scope=PCT` übersteuerbar,
  Exit-Code ≠ 0 bei Schwellenriss. Erste Ist-Messung am G1-Stand (macOS
  arm64, .NET 8 Debug, 300/300 Tests, zwei Läufe mit identischen Werten):
  Nova.Simulation 87,50 % (4607/5265), Command 91,76 % (1125/1226), PRNG
  100 % (57/57), Serializer 98,06 % (404/412), Hash 100 % (225/225), Replay
  91,64 % (592/646), CommandInventory 100 % (352/352) — alle Schwellen
  gehalten; PRNG (zuvor 89,47 %) und CommandInventory (zuvor 97,73 %) wurden
  durch vier gezielte Verhaltenstests geschlossen (`NextInt`-Bereichsablehnung,
  `NextFloat`-Intervall/Determinismus, `CommandPayloadWriter`-Byte-Exaktheit/
  Längentracking und der strukturelle Wurf oberhalb `MaxPayloadBytes`).
- **G1-Performance-Messgerüst V4/V5a (ohne Gate-Status, ohne Evidence):**
  `tools/Nova.SimRunner` führt jetzt das Szenario `SCALE_500_PRECOMBAT` aus
  [quality/scenarios/mvp-v1.json](quality/scenarios/mvp-v1.json) als
  reproduzierbares Harness aus — CLI `--scenario SCALE_500_PRECOMBAT
  [--runs 3] [--warmup-seconds 30] [--measure-seconds 120] [--agents 500]
  [--out <dir>]` (Defaults = `performanceMethod`-Vertragswerte D-052/D-063;
  die Artefakte protokollieren stets die tatsächlich verwendeten Werte; die
  bisherige Demo bleibt der Default-Modus). Workload: 500 Agenten auf der
  kanonischen 128×128-Karte, deterministischer Seed, dauerhafte Move-Last
  über rotierende Re-Target-Slices durch die versiegelte Command-Pipeline
  (eine Flow-Field-Regenerierung pro Tick), Movement-Spatial-Binning,
  5-Hz-FoW-Filterung, kein Combat (Pre-Combat), daher
  `precombatRestSimulationMs` = Gesamt-Tick − Pathfinding. Messung
  ausschließlich im Harness (Stopwatch-Dekorateur plus neuem, dokumentiertem
  Interception-Point: `PathfindingSystem` ist nicht mehr sealed,
  `RequestFlowField` ist virtual — die Flow-Field-Generierung läuft in der
  Command-Anwendung, nicht in einem System-Tick); die Sim-Quellen bleiben
  frei von Mess-Logik, Determinismus unter aktiver Messung ist per Test
  belegt. Pro Lauf frischer Host mit ungemessenem Warmup + gemessenem
  Wall-Clock-Fenster, eine Rohprobe pro Tick. Artefakte strikt nach
  D-062/D-063 (`name`/`unit`/`measurement` mit `methodRef`,
  `warmupSeconds`, `runs[index, measurementSeconds, samples]`) pro Lauf und
  kombiniert sowie Bool-Assertions (`no-crash`,
  `no-unbounded-memory-growth`; dokumentierte Regel: Retained-Heap nach
  vollem GC am Fensterende ≤ 1,10× Baseline nach Warmup) unter `output/`
  (jetzt gitignoriert) — **keine Evidence, kein Gate-Nachweis**. Lokale
  Diagnose auf macOS arm64 (.NET 8 Release, nicht die Windows-D-052-
  Referenz): voller Vertragslauf (30 s + 3×120 s, ~850 Ticks/s,
  ~307.000 Samples) — `pathfindingMs` P95 0,426 ms / P99 0,449 ms (Schwelle
  4,0), `precombatRestSimulationMs` P95 0,940 ms / P99 1,029 ms (Schwelle
  3,0), beide Assertionen PASS, Retained-Heap pro Lauf flach (≤ 11,3 MiB).
- **G1-Replay (ohne Gate-Status, ohne Evidence):** kanonische Replay-Schicht
  in `Nova.Simulation.Replays` gemäß
  [docs/tech/SimulationCore.md](docs/tech/SimulationCore.md) §6/§8 —
  `MatchFingerprint` (State-/Command-/Payload-/Snapshot-/Sidecar-Schema-Versionen
  als u16, `NumericModelId = Q16_16_V1`, 10 Hz, `XorShift128PlusV1`,
  Rules-/Definitions-/MapHash64, 8 Slot-Belegungen, Start-Seed,
  InitialStateHash und `InputDelayTicks` aus
  [docs/tech/Commands.md](docs/tech/Commands.md) §1; kanonische
  LE-Serialisierung, Wertgleichheit, `ComputeHash()` in der
  NOVA_DEFINITIONS_V1-Domäne als dokumentierte Wahl, weil §5 keine eigene
  Fingerprint-Domäne definiert — Q-040-Kandidat), `ReplayRecorder`
  (zeichnet tickweise nur die akzeptierten Records der versiegelten Batches
  samt ihrer `CommandResult`s auf; jeder Tick inklusive leerer, lückenlos),
  Replay-Containerformat v1 (Magic ASCII `NOVAPLAY`, FormatVersion u16 = 1,
  Fingerprint-Bytes, eingebetteter Init-Snapshot — die Hash-Referenz ist als
  Alternative dokumentiert, aber nicht implementiert —, Tick-Frames mit
  Tick u32, RecordCount u16, Records length-prefixed mit je ResultCode u16
  und gespeichertem Kettenwert, Trailer mit End-State-Hash und finalem
  Ketten-Hash), `ReplayFile` (gehärteter Parser: 64-MiB-Hardcap als
  dokumentierte Eigenwahl, da die Spec kein Replay-Cap festlegt — Q-040-Kandidat;
  Längen vor Allokation, strukturelle Revalidierung jeder Record —
  strukturell ungültige Records lehnen das Replay ab —, kanonische
  Record-Reihenfolge, lückenlose Ticks, inkrementelle Kettenverifikation,
  Fingerprint↔Snapshot-Konsistenz) und `ReplayPlayer` (prüft exakte
  Fingerprint-Gleichheit mit benanntem abweichendem Feld, stellt den
  eingebetteten Snapshot in einem frischen Kernel wieder her, spielt die
  Records über `TryAcceptHistoricalRecordBytes` und denselben versiegelten
  Pfad ab — ohne die KI erneut zu instanziieren — und verifiziert pro Tick
  die reproduzierten `CommandResult`s wertgleich sowie den End-State-Hash
  gegen den Trailer). Hash-Kette in der NOVA_REPLAY_CHAIN_V1-Domäne:
  Genesis bindet den Fingerprint, jeder Tick-Schritt bindet Vorgänger-Kette,
  Tick und je Record dessen Bytes plus ResultCode, der Final-Schritt bindet
  den End-State-Hash (exakte Konstruktion im Doc-Kommentar von
  `ReplayFormat`). Damit der Playback den aufgezeichneten End-Hash
  reproduziert, wurde der autoritative Sequenz-Floor des
  `CommandDedupeState` stream-abgeleitet gemacht: jeder akzeptierte Record
  (live wie historisch) hebt den Floor über seine Sequenz
  (`RaiseSequenceFloor`) — dokumentierte Ausnahme: eine durch eine
  abgelehnte lokale Einreichung verbrannte Sequenz kann der Strom nicht
  rekonstruieren (Q-040-Kandidat). Testsuiten in beiden Lanes (Unity
  EditMode + `tools/Nova.SimRunner.Tests`): Golden-Replay über das
  Standard-50-Tick-Match (Human-Slot, aufgezeichnete KI-Records,
  zustandsabhängig abgelehntes Command) mit identischem End-Hash und
  identischer Result-Sequenz, Ketten-Manipulation mitten im Strom
  (Record/Result/Tick) mit Erkennung an der manipulierten Position,
  Fingerprint-Mismatch (Start-Seed, Slot-Belegung, Schema-Version) verweigert
  den Start, Shadow-KI-Vergleich ohne KI-Doppelanwendung und
  Parserhärtung inklusive Truncation-Schleife über jede Byteposition.
  Q-040-Kandidaten: dokumentierte Stub-Content-Hashes
  (`ComputeEmptyContentStubHash` über eine leere Liste in
  NOVA_DEFINITIONS_V1), solange keine kanonischen Definitions-/Map-Quellen
  existieren; fehlende Fingerprint-Hash-Domäne; Replay-Hardcap;
  verbrannte Sequenzen.
- **G1-Kernel-Integration — kanonische Kernel-Bausteine (ohne Gate-Status,
  ohne Evidence):** Der umgebaute `SimulationKernel` akzeptiert als einzigen
  Command-Intake versiegelte `CommandBatch`-Objekte (`SubmitBatch`,
  [docs/tech/Commands.md](docs/tech/Commands.md) §1) und wendet fällige
  Batches in Tickphase 1 selbst über `CommandExecutor` an
  (`LastTickResults`); `BindCommands` bindet den `ICommandStateView`-Adapter
  und optional den Ingress, dessen Dedupe-/Sequenzstate Teil des
  Kernel-Blocks ist ([docs/tech/SimulationCore.md](docs/tech/SimulationCore.md)
  §3). Neuer kanonischer State-Hash: `CalculateStateHash()` ist der
  NOVA_STATE_V1-Container-State-Hash über exakt die Bytes, die
  `SaveSnapshot()` emittiert — Kernel-Block (Tick, PRNG-Wörter,
  Ingress-State, ausstehende Batches als kanonische Record-Bytes) plus ein
  Block pro `IStatefulSimSystem` — und berührt den PRNG nicht.
  Kernel-Snapshots über den Container v1 (`SaveSnapshot()`/
  `TryRestoreSnapshot()`) mit BlockId-Registry `SnapshotBlockIds`
  (Kernel = 1, Systeme ab 100; die finale Registry bleibt Q-040-Thema). Neu:
  `IStatefulSimSystem` (`WriteState`/`TryRestoreState`),
  `UnitCommandStateView` (Executor-Adapter auf EntityManager/Pathfinding,
  übersetzt gepackte Wire-Ids, vgl. Q-040(e)), `SimClock` (zentrale
  10-Hz-Konstante) und `SimRandom.GetState()/SetState()` (PRNG-Wörter
  snapshotfähig, `ISimRandom` unverändert). `MovementSystem`
  (Entity-Store-Block: Units, Generationen, Free-List) und
  `PathfindingSystem` (Flow-Field-Ziel; das Feld selbst wird beim Restore
  deterministisch neu berechnet — abgeleiteter Cache gemäß SimulationCore
  §3) sind stateful. Neue Regressionssuite `KernelIntegrationTests` in
  beiden Lanes (F-001/F-005/F-006, Zwei-Host-Determinismus,
  Snapshot-Nachweis §7.2 über 1.000 Ticks mit vor dem Snapshot gequeueten
  Commands und byteidentischem Roundtrip) sowie `SimRandom`-State-Tests.
- **Review-Folgearbeit Snapshot-Blockformat v1 (ohne Gate-Status, ohne
  Evidence):** zwei bewusste v1-Abweichungen von
  [docs/tech/Serialization.md](docs/tech/Serialization.md) offen deklariert
  statt nachimplementiert — (a) kein Datei-Hash im Container (§2 Punkt 7):
  State-Hash plus exakte Längenarithmetik decken Integrität und Truncation
  bereits ab, ein Datei-Hash wäre redundant; (b) Major-only
  `FormatVersion u16` statt `Major u16 + Minor u16` (§1), Minor implizit 0.
  Beide Punkte sind als Q-040 (g)/(h) in
  [docs/production/OpenQuestions.md](docs/production/OpenQuestions.md)
  (Version 1.11.2) erfasst, vor dem G1-Schema-Freeze per D-ID zu
  ratifizieren, und im Format-Doc-Kommentar (`SnapshotFormat.cs`)
  markiert. Zudem: `SnapshotWriter.AddBlock` lehnt mehr als 65.535 Blöcke
  mit `InvalidOperationException` ab (kein stiller u16-Wrap des
  BlockCount-Felds, Test: 65.536. Block), und ein tautologischer
  Hash-Test wurde zugunsten des bestehenden echten Nachbartests
  (gleicher Content unter zwei BlockIds → gleicher BlockHash,
  differierender State-Hash) in beiden Lanes entfernt.
- **G1-Vorarbeit Snapshot-Blockformat v1 (ohne Gate-Status, ohne Evidence):**
  kanonischer Snapshot-Container in `Nova.Simulation.Snapshots` gemäß
  [docs/tech/SimulationCore.md](docs/tech/SimulationCore.md) §7 und
  [docs/tech/Serialization.md](docs/tech/Serialization.md) §2/§5 — festes
  24-Byte-Envelope (Magic ASCII `NOVASNAP` als dokumentierte Eigenwahl, da
  die Spec keinen Magic-Wert festlegt; FormatVersion u16 = 1, BlockCount,
  PayloadBytes, State-Hash), Block-Tabelle (BlockId u16, Länge u32,
  BlockHash u64 als reiner Content-Hash über `NOVA_FILE_V1`) und State-Hash
  über `NOVA_STATE_V1` in strikt aufsteigender BlockId-Reihenfolge.
  `SnapshotWriter` serialisiert deterministisch unabhängig von der
  Übergabe-Reihenfolge (byteidentischer Roundtrip, §7 Punkt 1);
  `SnapshotBlockWriter`/`SnapshotBlockReader` kodieren Little-Endian
  feldidentisch zu `SimHashWriter` (`SimFixed`/`SimAngle`/`Tick`/`EntityId`,
  Längenpräfixe). Parserhärtung nach §7 Punkt 4: 64-MiB-Hardcap vor dem
  Payload-Parse, alle Längen arithmetisch (long) vor jeder Allokation
  geprüft, Truncation an jeder Position, Bit-Korruption an jedem Byte,
  gefälschte Längenfelder, unbekannte FormatVersion, doppelte/unsortierte
  BlockIds und Trailing Bytes deterministisch über `SnapshotReadError`
  abgelehnt — nie Exception, nie Partial State. Hash-Sensitivität nach §7
  Punkt 3: 1-Bit-Mutation in einem Block ändert exakt dessen BlockHash und
  den State-Hash, keine anderen Blockhashes. 4-MiB-Ziel als dokumentierter
  Warn-/Info-Pfad (`ExceedsSoftTarget`), kein harter Fehler; 0 Blocke sind
  dokumentiert kein kanonisches Artefakt (Writer-Throw, Reader
  `EmptyBlockTable`). Golden-Bytes-Hex-Master als Format-Freeze-Regression.
  Testsuiten in beiden Lanes (Unity EditMode + `tools/Nova.SimRunner.Tests`).
  Der Prototyp-State (`State/`) bleibt bis zur G1-Integration unverändert
  (D-057); das Blockformat ist eine eigenständig getestete Einheit ohne
  State-Semantik. Offene Q-040-Kandidaten: endgültige BlockId-Registry des
  Root-Inventars (Serialization.md §4) bei der G1-Integration, Magic-Wert
  `NOVASNAP` als bis dahin implementierungsseitig eingefrorene Wahl,
  Kompression bleibt Post-G1-Größenmessung vorbehalten (Serialization.md
  „Offene Punkte").
- **G1-Vorarbeit versiegelter Command-Pfad v1 (ohne Gate-Status, ohne
  Evidence):** kanonische Command-Schicht in `Nova.Simulation.CommandsV1`
  gemäß [docs/tech/Commands.md](docs/tech/Commands.md) — numerisch
  eingefrorenes `CommandKind`-Register (13 Stream-Kinds plus Pause/Unpause/
  Save/Load als Session-Aktionen außerhalb des Simulationsstroms), exaktes
  Little-Endian-Record-Format (20-Byte-Header, Längenprüfung vor Allokation),
  `CommandIntent` als einziges UI-/KI-seitiges Objekt, `MatchSession`/
  `CommandIngress` als Autorität für Slot-Bindung, monotone Sequenz (Start 1,
  kein Wrap) und `TargetTick = EnqueueTick + InputDelayTicks`,
  `LocalLoopbackTransport` zurück an denselben validierenden Ingress,
  unveränderlicher sortierter `CommandBatch` ((TargetTick, PlayerSlot,
  Sequence)) als einzige Kernel-Eingabe, autoritativer serialisierbarer
  `CommandDedupeState` (byteidentische Duplikate genau einmal, Konflikte
  deterministisch abgelehnt, abgeschlossene Sequenzen umgehen Dedupe nicht),
  Backpressure-Grenzen 4096/4076/256/1024/100 aus Commands.md §2 und
  deterministisches `CommandResult` für zustandsabhängige Ablehnung ohne
  Mutation (`CommandExecutor` gegen `ICommandStateView`). Alle neun
  Pflichttestfälle aus Commands.md §6 in beiden Lanes (Unity EditMode +
  `tools/Nova.SimRunner.Tests`), 100 % des aktivierten Inventars mit
  Roundtrip- und Golden-Bytes-Tests (Hex-Master aus der kanonischen
  Implementierung als Format-Freeze-Regression). Der defekte Prototyp-Pfad
  (`Commands/`, `SimulationKernel.SubmitCommand`) bleibt unverändert bis zur
  G1-Integration (D-057). Offene Q-040-Kandidaten: Harvest-`FieldId`-Modell,
  Duplikat-Behandlung in Entity-Listen (als struktureller Fehler statt stiller
  Dedupe gewählt), provisorische Obergrenze 64 für ausstehende
  Session-Aktionen.
- **G1-Vorarbeit Hash-Domänen (ohne Gate-Status, ohne Evidence):** kanonischer
  XXH64-Hasher in `Nova.Core` (`XxHash64` One-Shot, `XxHash64State`
  Streaming, safe-managed, explizite Little-Endian-Lanes) plus kanonischer
  Domänen-Writer `SimHashWriter` (ASCII-Präfixe `NOVA_STATE_V1`,
  `NOVA_DEFINITIONS_V1`, `NOVA_FILE_V1`, `NOVA_REPLAY_CHAIN_V1` mit
  `0x00`-Terminierung, Feld-Tags, Längenpräfixe und typsichere Schreiber für
  `SimFixed`/`Tick`/`EntityId` in Little-Endian) gemäß
  [docs/tech/SimulationCore.md](docs/tech/SimulationCore.md) §5. Korrektheit
  über die offiziellen xxHash-Sanity-Vektoren verankert (xxHash-Repo,
  `tests/sanity_test_vectors.h`), nicht über selbst erzeugte Golden-Werte;
  Streaming == One-Shot über alle Längen 0–200 und Stripe-Grenzen
  31/32/33/64/65. Testsuiten in beiden Lanes (Unity EditMode +
  `tools/Nova.SimRunner.Tests`). `StateHashUtility` (FNV-1a-Prototyp) bleibt
  bis zur G1-Integration bestehen (D-057); offener Punkt für Q-040: Der
  bestehende `EntityId`-Typ (int Index + ushort Version) entspricht noch
  nicht dem gepackten `uint32`-Bitlayout aus §1, `WriteEntityId` hasht die
  aktuellen Felder.
- **Q-040 registriert:** G1-Numerik-Detailfragen, die
  [docs/tech/SimulationCore.md](docs/tech/SimulationCore.md) offenlässt
  (`ToInt()`-Rundung, `SimAngle`-Einheit, PRNG-Seeding/-Serialisierung), mit
  dokumentierten Provisorien; G1-blockierend, vor dem G1-Schema-Freeze per
  D-ID zu ratifizieren ([docs/production/OpenQuestions.md](docs/production/OpenQuestions.md)
  1.11.0).
- **G1-Vorarbeit Numerisches Modell (ohne Gate-Status):** `SimFixed`
  (signed Q16.16 auf `int32`, `int64`-Zwischenprodukte, Rundung nearest
  ties-to-even, `WorldToGrid` als floor auch für negative Werte) und
  `SimAngle` (`uint16`, voller Kreis = 65536, definiertes Wraparound,
  Grad-Mapping) in `Nova.Core` gemäß
  [docs/tech/SimulationCore.md](docs/tech/SimulationCore.md) §1. Überlauf,
  Division durch null und Bereichsverletzungen sind geprüfte Fehler
  (`OverflowException`/`DivideByZeroException`); Sättigung und stilles
  Wraparound sind nicht implementiert. `SimRandom` ist gegen
  `XorShift128PlusV1` verifiziert (kanonischer xorshift128+ mit
  (23, 17, 26)); Seeding (SplitMix64) und 32-bit-Ausgabereduktion sind als
  spec-freie Implementierungsdetails dokumentiert. `SimMath` (Float-Prototyp)
  bleibt bis zur G1-Integration bestehen (D-057).
- **G1-Testsuiten Numerik/PRNG in beiden Lanes:** neue EditMode-Assembly
  `Nova.Core.Tests` sowie `SimFixedTests`, `SimAngleTests` und
  `SimRandomGoldenTests` in `tools/Nova.SimRunner.Tests` (Testing.md §3:
  Q16.16-Grenzen, ties-to-even, negatives Welt→Grid-floor,
  Overflow/Div-by-zero als Exceptions, SimAngle-Wrap, PRNG-Golden-Vektoren
  für Seed 0/1 als regressions-gepinnter Golden-Master sowie
  Snapshot-Fortsetzung per `Clone`).
- **G0-B-.NET-Testlane:** `tools/Nova.SimRunner.Tests` (NUnit, net8.0)
  kompiliert dieselben Core-/Simulation-Quellen mit demselben
  `NOVA_FIXED_POINT`-Define wie Unity-Host und SimRunner; 4 Smoke-Tests
  (Tick-/EntityId-Wertesemantik, SimRandom-Sequenzdeterminismus). Damit ist
  `G0-TEST-DOTNET` lokal belegbar (repo-lokales SDK 8.0.318, gitignoriert
  unter `.dotnet/`); `.gitignore` ignoriert Build-Outputs nun für alle
  Tool-Projekte (`tools/**/bin|obj|out`).
- **G0-B-Review-Nacharbeiten:** `Builds/` als Player-Build-Ausgabeverzeichnis in
  `.gitignore` ignoriert; veralteten Klassenkommentar in `SelectionManager`
  korrigiert; G0-B-Interimsplatzierung der Prototyp-Selektions-/HUD-Logik in
  `Nova.Gameplay` in [docs/tech/Architecture.md](docs/tech/Architecture.md)
  §2 dokumentiert (Überführung nach `Nova.UI` mit G2, Version 1.3.1).
- **G0-B-Buildbasis:** `Assets/_Project/Editor/BuildScript.cs` mit den
  CLI-Build-Methoden `BuildWindows64`/`BuildMacOSArm64` (Szenenliste aus den
  EditorBuildSettings, sauberes `companyName`/`productName`, fail-closed bei
  leerer Szenenliste) sowie die minimale Bootstrap-Szene
  `Assets/_Project/Scenes/Bootstrap.unity` (erzeugt über
  `BootstrapSceneGenerator.CreateBootstrapScene`) als einzige aktivierte
  Build-Szene. Echte Player-Builds bleiben ohne installierte Unity-
  Build-Module ein dokumentierter Blocker.
- `global.json` im Repo-Root mit exaktem .NET-SDK-Pin `8.0.318`
  (`rollForward: disable`), damit `dotnet`-Toolchain und Unity-Projekt
  reproduzierbar denselben SDK-Stand verwenden (G0-B).
- **Geschützter Authorize-Workflow `gate-evidence-authorize` (G0-A2, D-066):**
  neuer Job in `.github/workflows/quality-gate.yml`, ausschließlich per
  `workflow_dispatch` auf `main` hinter dem geschützten Environment
  `quality-gate` (Job-Concurrency mit `cancel-in-progress: false`). Die
  Inputs `evidencePath`, `subjectSha`, `trustedSha` (Pflicht) und `notes`
  (optional) laufen über `env:`-Mapping und einen fail-closed Format-Check;
  Guards erzwingen `trustedSha != subjectSha`, `trustedSha` als Ancestor von
  `origin/main`, `GITHUB_SHA == trustedSha` (Dispatch auf dem
  Trusted-Commit) und committed Evidence im Subject-Checkout. Der Job holt
  Trusted Tool (`trusted/`) und Subject (`subject/`) getrennt mit
  `fetch-depth: 0` und `persist-credentials: false`, pinnt Node exakt,
  ermittelt die numerische Job-ID per `gh api` + `jq` und ruft
  `validate_gate_evidence.py --authorize` auf. Nur bei Exit 0 wird das
  Receipt unter dem vom Validator ausgegebenen Versionspfad als Artefakt
  `gate-authorization-G<N>-<runId>-attempt<runAttempt>` hochgeladen
  (`upload-artifact` auf Commit-SHA gepinnt); die Versionierung erfolgt per
  separatem append-only Folge-PR, der Workflow pusht nichts. Voraussetzung
  für den ersten Lauf: einmalige Anlage des `quality-gate`-Environments mit
  Required-Reviewers durch einen Maintainer (Root-of-Trust-Anker, D-066
  Punkt 6). Dokumentiert in `docs/tech/Deployment.md` §7 und
  `docs/tech/Testing.md` §10 (je 1.9.0).
- **G0-A2 zweiphasiger Receipt-Vertrag (D-066) implementiert:** neues Schema
  `quality/schemas/GateAuthorization.schema.json` (`gate-authorization-v1`,
  Draft 2020-12, strikt) bindet Gate, Subject-Commit/-Tree,
  Evidence-Carrier-Commit, Evidence-Pfad/-Hash, Trusted-Tool-Commit,
  Repository, Workflow sowie Run-/Attempt-/Job-ID. Der Validator erhält den
  geschützten Modus `--authorize` (mit `--receipt-out`, `--job-id`,
  `--notes`): Er validiert die Evidence vollständig inklusive
  `priorGateReceipts`, bindet den Lauf aus der GitHub-Actions-Umgebung ohne
  Prüfung der eigenen Conclusion und schreibt bei Erfolg den hashgebundenen
  Receipt-Kandidaten `GateAuthorization.json`; lokal ohne GitHub-Kontext
  bleibt er fail-closed (`E_TRUST_CONTEXT`). Receipts werden append-only
  unter
  `quality/authorizations/G<N>/<subjectSha>/<runId>-attempt<runAttempt>/GateAuthorization.json`
  versioniert. Self-Test auf 64 Semantik- plus 7 Topologie-Kontrollen
  erweitert (Receipt-Emission, falsche Evidence-Hashs, Kettenlücke/
  Vertauschung, doppelte Run-ID, falsches Gate/Subject, fehlende
  GitHub-Umgebung).
- **G0-A Trusted-Gate-Bootstrap (D-064) als Draft-Checkpoint angelegt:**
  Evidence-Schema
  `1.3.0` mit Pflicht-`environmentId` an jedem Command und jeder
  Performance-Messung, strikten Umgebungsfeldern (OS, Architektur, Hardware,
  Build, Managed/Burst, Auflösung, Quality-Profil, VSync, Deep Profiling,
  Replay) sowie einer `trustBundle`-Sektion, die alle neun
  Trust-Bundle-Komponenten (Manifest, Szenariovertrag, Schema,
  Python-Validator, Ajv-Wrapper, `package.json`, Lockdatei, Gate-Runner,
  Authorize-Workflow) per Subject-/Trusted-Commit und SHA-256 plus exakter
  Node-Version bindet.
- `validate_gate_evidence.py` erhält den Trusted-Tool-Modus
  (`--trusted-tool-checkout`): Schema, Ajv-Wrapper und Paketpins kommen
  ausschließlich aus einem subject-unabhängigen, sauberen Checkout;
  Umgebungsfelder werden exakt gegen das Methodenprofil verglichen, und der
  Trust-Kontext `2.0.0` prüft die vollständige geordnete
  `authorizedEvidence`-Kette G0→Gate (Reihenfolge, Vollständigkeit,
  CI-/Review-Attestierung je Glied). Lokale Pass-Versuche bleiben fail-closed
  (`E_AUTHORIZATION_BOOTSTRAP`/`E_TRUST_CONTEXT`); Self-Test um die
  D-064-Angriffspfade (manipuliertes Subject-Schema, Ajv-Wrapper/Lockfile,
  unvollständige/vertauschte Kette, widersprüchliche Umgebung, Missing-Tool
  und Subprozess-Timeout) sowie eine positive Trusted-Baseline erweitert
  (52 Kontrollen).
- **Kanonischer Gate-Runner `quality/scripts/run_gate_check.py`:** führt pro
  Aufruf genau einen Kriterien-Check (`--gate`/`--criterion`/`--subject`/
  `--result`, Executor via `NOVA_GATE_EXECUTOR`) aus und schreibt das strikte
  `gate-check-result-v1`-Artefakt, das der Validator Feld für Feld gegen die
  Evidence abgleicht. Alle zehn G0-B-Kriterien (Engine-Pin, geteilte
  SimRunner-Quellen, asmdef-Architekturgrenzen, Build-Voraussetzungen
  Windows/macOS, .NET-/EditMode-Tests, Architektur-Negative-Control,
  Evidence-Validator/Trustpfad, keine getrackten Binärdateien) sind
  registriert; reale plattformspezifische Builds, der Authorize-Pfad und die
  externe Attestierungsprüfung sind noch nicht abgenommen. Die registrierten
  G1–G5-Kriterien enden fail-closed mit „criterion not implemented".
- **Entwurf des geschützten Authorize-Workflows
  `.github/workflows/quality-gate.yml` (D-064):** Job `integrity` (jeder PR)
  prüft gepinnte Abhängigkeiten,
  Validator-Self-Test, Ajv-Schema-Selbstcheck und Runner-CLI; Job
  `gate-evidence-authorize` läuft nur per `workflow_dispatch` hinter der
  geschützten Umgebung `quality-gate`, nutzt einen subject-unabhängigen
  Trusted-Tool-Checkout (`trusted/`, Pflicht-Input `trustedSha`) und erzeugt
  den externen Trust-Kontext `2.0.0` über
  `.github/scripts/generate_trust_context.py` (17 Schlüssel, geordnete
  `authorizedEvidence`-Kette, `NOVA_TRUST_CONTEXT_SHA256`-Bindung). Der
  Authorize-Job bleibt bis zur Korrektur der Checkout-, Input- und
  Attestierungsbindung nicht merge- oder autorisierungsfähig.
- **G1 Fog of War kanonisch (ohne Gate-Status, ohne Evidence):** erstes
  Domain-System nach Movement auf den kanonischen Vertrag migriert
  ([docs/tech/FogOfWar.md](docs/tech/FogOfWar.md), D-058). `FogOfWarSystem`
  in `Nova.Simulation.Vision` (ersetzt das Prototyp-Scaffolding
  `VisionSystem`/`VisionGrid`): committed 1-m-Grid-Maske pro Team
  (128 × 128, byte-Array, 16 KiB/Team) mit den Zuständen
  `Unexplored`/`Explored`/`Visible`; 5-Hz-Recompute intern auf jedem zweiten
  Tick (`tick % 2 == 0`), registriert nach Movement und vor Combat
  ([docs/tech/SimulationCore.md](docs/tech/SimulationCore.md) §2);
  MS-1-Sichtmodell nur Radien (exakter Zellmittelpunkt-Test in Q16.16,
  Grenze inklusiv, stabile aufsteigende Entity-Index-Reihenfolge);
  `TeamView` als einzige committed Sicht (kein System kann eine vorläufige
  oder selbst berechnete Sicht ziehen), `GetVisibleEntities` (eigene immer,
  fremde nur in `Visible`-Zellen), `GetRadarSignatures` als Minimap-Pings
  ohne `EntityId` und ohne Targeting-Berechtigung (provisorische
  Radar-Reichweite = 2 × Sichtweite — Q-040-Kandidat). Autoritativer
  Snapshot-Block 102 (v1: Dimensionen, Teamzahl, letzter Recompute-Tick,
  beide Team-Masken; hash-sensitiv pro Zelle; zweiphasiges
  TryValidate/TryRestore). `UnitState.SightRadius` als autoritatives
  SimFixed-Feld ergänzt (Default 10 m, provisorisch — Q-040-Kandidat;
  Entity-Store-Block v3, v1/v2 werden hart abgelehnt). Verdrahtung in
  `MatchRunner` und `Nova.SimRunner` in kanonischer Tickreihenfolge.
  Tests in beiden Lanes (`FogOfWarSystemTests`): Zustandsübergänge,
  5-Hz-Kadenz inkl. committed Sicht zwischen Recomputes, exakte
  Kreisrasterung, Teamtrennung, Radar-Vertrag, Hidden-World-Metamorphics
  ([docs/tech/Testing.md](docs/tech/Testing.md) §5: verborgene
  Gegner-Variation lässt committed Maske, Zielmenge und Radar der eigenen
  Sicht bitidentisch; keine Wirkung vor dem Commit-Tick), Snapshot-
  Roundtrip/-Fortsetzung über Recomputes hinweg, Zwei-Kernel-Determinismus
  über 200 Ticks, Parser-Härtung; EntityStore-Suite auf v3 gehoben
  (`EntityStoreSnapshotTests`, vormals `…V2Tests`).
- **G1 Combat kanonisch (ohne Gate-Status, ohne Evidence):** drittes Glied
  der Tickordnung [docs/tech/SimulationCore.md](docs/tech/SimulationCore.md)
  §2 (Movement → FoW → Combat, Schritt 8). `CombatSystem` in
  `Nova.Simulation.Combat` vom Prototyp-Scaffolding auf den kanonischen
  Vertrag migriert: MS-1-Hitscan direkt Einheit-gegen-Einheit (sofortiger
  Schaden, keine Projektile/Flugbahnen, kein Splash), rein ganzzahlig —
  Reichweite `SimFixed`, Schaden int32, Cooldown in ganzen Ticks
  (provisorische Defaults 8 m / 15 / 5 Ticks = 0,5 s bei 10 Hz —
  Q-040-Kandidaten; der alte Kommentar „10 Ticks = 0,5 s" war ein
  20-Hz-Relikt). Tick-Logik in strikt aufsteigender Entity-Index-Reihenfolge:
  Cooldowns herunterzählen, dann Zielvalidierung — ein Schuss ist nur legal,
  wenn das Ziel lebt, die Mittelpunkt-Distanz ≤ `WeaponRange + Radius` des
  Ziels liegt (Grenze inklusiv, exakter Vergleich in aufgeweiteter
  Q32.32-Long-Arithmetik) UND die Zielzelle in der committed Team-Sicht
  `Visible` ist ([docs/tech/FogOfWar.md](docs/tech/FogOfWar.md) §2/§3:
  `Explored` und Radar-Pings verleihen KEINE Targeting-Berechtigung; zwischen
  den 5-Hz-Recomputes gilt die committed Sicht — Feuer läuft bis zum nächsten
  Commit weiter). Tote Ziele werden aus allen Angriffsbefehlen noch im
  selben Tick aufgelöst; lebende, aber unsichtbare oder außer Reichweite
  befindliche Ziele werden gehalten, nicht fallengelassen (Verfolgung ist
  Movement-Sache). **Duell-Asymmetrie (Review-Feststellung):** da die
  Engagement-Phase in aufsteigender Index-Reihenfolge läuft und der Tod
  sofort wirkt, gewinnt bei gegenseitigem Töten im selben Tick immer die
  Einheit mit dem kleineren Index — die Spawn-Reihenfolge entscheidet
  gleichstarke Duelle (deterministisch und spec-konform, aber
  balance-relevant, daher explizit dokumentiert). Einheiten auf Slots ohne committed Team-Sicht (MS-1:
  Team-Index == Slot) können nicht feuern. FoW-Verdrahtung per
  Konstruktor-Injektion des `FogOfWarSystem` durch den Host (der Kernel
  bietet keine Cross-System-API; gleiches Muster wie Movement ←
  EntityStore/Pathfinding), Registrierung NACH FoW in `MatchRunner` und
  `Nova.SimRunner`. Kein eigener Snapshot-Block: der gesamte autoritative
  Combat-State (Health, `WeaponCooldownTicks`, `AttackTarget`) liegt im
  EntityStore-Block 100, Hitscan hält keinen schwebenden Zustand — das System
  ist bewusst `ISimSystem` ohne `IStatefulSimSystem` und damit konform zur
  Registrierungs-Checkliste. Tests in beiden Lanes (`CombatSystemTests`):
  Feuern nur bei lebendig + in Reichweite + `Visible`, kein Feuer bei
  Radar-only/`Explored`/vor dem ersten Commit/ohne Team-Sicht, committed
  Sicht zwischen Recomputes (Sichtverlust auf ungeradem Tick → Feuer stoppt
  erst nach dem Commit), exakte 5-Tick-Cooldown-Kadenz, Tod bei Health ≤ 0
  mit Despawn und Order-Auflösung Dritter, tote Angreifer feuern nicht,
  Reichweiten-Grenzfall inklusiv/exklusiv, Zwei-Kernel-Determinismus über
  300 Ticks mit Gefecht, Hash-Sensitivität auf Health-Änderung, Replay-
  Aufzeichnung/-Playback mit End-Hash-Verifikation, Mid-Combat-Snapshot-
  Restore mit identischer Fortsetzung.
- **G1-Economy kanonisch (ohne Gate-Status, ohne Evidence):** kanonische
  Economy-Domain in `Nova.Simulation.Economy` gemäß
  [docs/tech/SimulationCore.md](docs/tech/SimulationCore.md) §2 (Phasen 2/3)
  und §3 — `PlayerEconomyState` pro Slot mit `AetheriumCredits` (`int64`,
  Start 1000 AE nach `quality/content/mvp-v1.json`, nie negativ —
  `TrySpendCredits` ist die erzwingende Primitive, Ausgaben-Checks bleiben
  Command-/Construction-/Production-Sache), `PowerProvided`/`PowerRequired`
  (`int32`) und `ProductionSpeedMultiplierQ16` als `SimFixed` (Low-Power-Faktor
  exakt 0,5 = Raw 32768, das float-Relikt des Prototyps ist ersetzt);
  `AetheriumField` mit fester Grid-Position und endlichem `RemainingAE`
  (`int64`, kein Nachwachsen — der volle D-010-Loop mit Mutterreserve,
  Regrowth, Spread, Überernte-Schaden und Warnung ist ausdrücklich G2);
  `EconomySystem : ISimSystem, IStatefulSimSystem` mit eigenem Snapshot-Block
  **104 v1** (pro Slot Credits/Power, pro Feld Id/Position/Reserve; Block 103
  ist für den G2-Aetherium-Block reserviert). Gebäude sind in dieser Scheibe
  Entities mit provisorischem Rollen-Enum `UnitRole` {Unit, Builder,
  Harvester, HQ, Refinery, Power} (minimales Gebäude-Modell, Q-040-Kandidat);
  die Power-Bilanz wird jeden Tick aus den lebenden Rollen-Entities neu
  berechnet (HQ liefert 30, Power-Plant 100, Refinery braucht 20 — provisorische
  Werte, Q-040-Kandidaten), sodass eine durch Combat despwante Power-Plant
  deterministisch in Low-Power kippt. Harvest-Kreislauf: Harvester (nur Rolle
  Harvester) mit `HarvestFieldId`-Order sammelt in Reichweite (gleiche Zelle
  oder angrenzend, Chebyshev ≤ 1 — dokumentierte Regel) exakt 2 AE/Tick
  (provisorisch, Q-040-Kandidat) bis `DefaultCargoCapacityAE` 330
  (Fraktions-Split 330/300 noch nicht modelliert — Q-040-Kandidat) oder
  Felderschöpfung; die Order resolved zu Idle (kein Auto-Return, Q-040-Kandidat),
  `ReturnCargo` liefert bei eigener Refinery in Reichweite exakt das Cargo in
  die Credits. Cargo und Harvest-Orders liegen im `UnitState` (EntityStore-
  Block 100 **v4**, harter Schnitt wie bei v3), nicht doppelt im Economy-Block.
  `UnitCommandStateView` verdrahtet `Harvest`/`ReturnCargo` (Harvest-Legalität
  zustandsabhängig: unbekannte FieldId oder Nicht-Harvester →
  `RejectedInvalidTarget`, siehe „Behoben" P2-1/P2-2) und räumt Economy-Orders
  bei `Stop` ab. Harvester-Konkurrenz am selben Feld (P2-3): bei Restreserve
  unter der kombinierten Tick-Nachfrage entscheidet die strikt aufsteigende
  Index-Reihenfolge — der Harvester mit dem kleineren Index sammelt zuerst
  (deterministisch, spec-konform, selbes Muster wie die Combat-Duell-
  Asymmetrie; im `EconomySystem`-Klassenkommentar dokumentiert). Registrierung in `MatchRunner` und `Nova.SimRunner`:
  Economy VOR Pathfinding/Movement (§2 Phasen 2/3 vor 6) — ein Harvester am
  Feld sammelt, bevor Movement desselben Ticks läuft (dokumentiert); der
  SimRunner-End-Hash ändert sich dadurch erwartungsgemäß (ehrlich neu
  erzeugt: `0xA19C77092F1B3FBD`). Prototyp-Scaffolding (`EnergyGridSystem`,
  `ResourceHarvestingSystem`) entfernt; `ConstructionSystem`,
  `ProductionQueueSystem` und `SkirmishAiSystem` minimal auf die neue API
  umgestellt (kein Float-Multiplikator mehr). Tests in beiden Lanes
  (`EconomySystemTests`, `EconomyIntegrationTests`): kompletter Harvest-
  Kreislauf mit exakten Raten, endliches Feld (Rest < Rate, Order-Resolve,
  kein Regrowth), Capacity-Stop, Startbedingungen (1000 AE, Credits nie
  negativ), Low-Power mit exakt 32768 Raw inkl. Combat-Kill-Integration,
  Zwei-Kernel-Determinismus 300 Ticks, Hash-Sensitivität auf Credits,
  Snapshot-Roundtrip + 300-Tick-Fortsetzung, Replay mit Harvest-/Return-
  Intents und End-Hash-Verifikation.
- **G1-Production/Construction (ohne Gate-Status, ohne Evidence):** kanonische
  Produktions- und Bau-Domäne (SimulationCore.md §2 Phasen 4/5) als letzter
  G1-Domain-Slice. `SimDefinitions`
  (`Assets/_Project/Scripts/Simulation/Definitions/`): statische, engine-freie
  Tabelle für die 9 MS-1-Gebäude- und 8 MS-1-Einheitenrollen aus
  [quality/content/mvp-v1.json](quality/content/mvp-v1.json) (je `CostAE`,
  `BuildTicks`, `PowerProvided/Required`, `PrerequisiteRole`, Tier,
  Produzentenzuordnung HQ→Builder/Harvester, Barracks→Infanterie,
  VehicleFactory→Fahrzeuge; Footprint provisorisch 3×3) — alle Werte
  dokumentierte Q-040-Provisorien, bis GameDatabase/Definitions angebunden
  sind; `UnitRole` um die fehlenden Manifest-Rollen erweitert (Entity-Store-
  Block v4 unverändert, nur Validierungsobergrenze); die Power-Rechnung der
  Economy liest die Rollenwerte jetzt aus `SimDefinitions`.
  `ConstructionSystem` (`IStatefulSimSystem`, Block 105): PlaceBuilding mit
  zustandsabhängiger Validierung in fester Reihenfolge (Kosten über
  `TrySpendCredits` → `RejectedInsufficientResources`; unbekannte Definition,
  Footprint außerhalb/belegt → `RejectedInvalidTarget`; Prerequisite-Rolle
  und Power-Regel → `RejectedPrerequisitesNotMet` — Power-Regel: Gebäude mit
  `PowerRequired > 0` nur bei ausreichend freiem Power der zuletzt
  committed Balance, Ausnahme Start-Refinery gemäß Manifest strukturell über
  `PlaceCompletedBuilding` ohne Vorgriff auf Bauvalidierung), Baustellen als
  `Unit`-Rollen-Entities mit 1 HP (Power wirkt erst ab Fertigstellung im
  nächsten Economy-Recompute), Builder-Auto-Zuweisung (kleinster Index),
  Fortschritt exakt Q16.16 (LowPower-Faktor exakt 0,5 raw 32768, keine
  Rundung), Pause bei Builder weg/statt Reichweite (Chebyshev ≤ 1),
  Fertigstellung → Rollen-Entity mit voller HP, ResearchLab-Fertigstellung
  setzt `T2Unlocked[slot]` (Phase 5, Flag liegt dokumentiert im
  Construction-Block), CancelConstruction 75 % Erstattung (Floor), Sell 50 %
  (Floor), Repair durch Builder 10 HP/Tick in Reichweite (provisorisch);
  Belegungs-Grid als abgeleiteter Cache (Rebuild aus Placements, Roundtrip-
  Test). `ProductionSystem` (`IStatefulSimSystem`, Block 106): QueueUnit auf
  fertige Produzenten-Gebäude, Kosten = `CostAE × Count` bei Enqueue, T2-
  Gating (`RejectedPrerequisitesNotMet` vor Freischaltung), Queue max. 5
  Einträge (provisorisch), Fortschritt exakt Q16.16 mit LowPower-Halbierung,
  Spawn am RallyPoint (Default: zwei Zellen östlich der Gebäude-Mitte;
  dokumentierter Ring-Scan 0..8 in aufsteigender (y,x)-Reihenfolge), Pause
  bei vollem Entity Store (Cap) ohne Verlust, `SetRallyPoint`,
  `CancelProduction(Index)` mit voller Erstattung der Restanzahl (Aktion
  selbst kostenlos); Queue auf verkauftem/zerstörtem Gebäude verfällt ohne
  Erstattung (dokumentiert). Kein Forschungsbaum: `ResearchTreeSystem`- und
  `ProductionQueueSystem`-Scaffolding ehrlich entfernt (mvp-v1.json:
  ResearchLab-Fertigstellung = T2, keine Upgrades/Queue, kein T3).
  `CommandExecutor`/`ICommandStateView`: neuer Domain-Hook `ValidateDomain`
  nach den generischen Prüfungen in dokumentierter fester Reihenfolge;
  QueueUnit-Kosten wegen der Count im Payload als Domain-Prüfung;
  `InstallDefenseModule` wird deterministisch mit
  `RejectedPrerequisitesNotMet` abgelehnt (G2/G4-Content); Hosts ohne
  verdrahtete Domänen lehnen diese Kinds ebenso ab. Registrierung in
  `MatchRunner` und `Nova.SimRunner`: Economy → Construction → Production →
  Pathfinding/Movement (Phasen 2/3, 4/5 vor 6) — eine frisch gespawnte
  Einheit trägt im Spawn-Tick noch keine Movement-Order und bewegt sich
  frühestens in Tick T+1 (dokumentiert); `SkirmishAiSystem`-Scaffolding auf
  die kanonischen APIs umgestellt. Golden-Hashes ändern sich erwartbar
  (ehrlich neu). Tests in beiden Lanes (`ConstructionSystemTests`,
  `ProductionSystemTests`, `ProductionConstructionIntegrationTests`):
  Definitions-Integrität, exakter Kostenabzug, Insufficient-Funds-/Belegung-/
  Prerequisite-/Power-Regel-Rejects inkl. Start-Refinery-Ausnahme, Fortschritt
  mit exakter LowPower-Halbierung und Builder-Weg-Pause, Fertigstellung →
  Power ab nächstem Tick, ResearchLab → T2 → T2-Einheit queuebar (vorher
  Reject), Queue-Kosten/Max-Queue/Spawn am RallyPoint/Cancel-Erstattung,
  Entity-Cap-Pause, Sell/Repair-Regeln, Manifest-Startzustand-Fixture
  (HQ+Refinery fertig, 1 Builder, 2 Harvester, 1000 AE, Refinery ohne Power),
  Zwei-Kernel-Determinismus 400 Ticks, Hash-Sensitivität (Blöcke 105/106),
  Snapshot-Roundtrip + Fortsetzung, Replay-Kompatibilität.

### Behoben
- **Review-Nachzügler der Fraktions-Sitzung (P2-1 bis P2-3):** **P2-1** —
  der Legion-Fahrzeugschaden nutzt jetzt die konkreten
  [Vehicles.md](docs/gamedesign/Vehicles.md)-Werte (Räuber 28, Koloss 50,
  Donnerkanone 60) statt der 85-%-Ableitung (die 29/51/93 produzierte);
  die Ableitung gilt nur noch, wo die GDDs schweigen (Scout, Gebäude-HP),
  ein Test in beiden Lanes pinnt die drei Konkretwerte ausdrücklich gegen
  die Ableitungsergebnisse (Teil-Entscheidung in D-075). **P2-2** — der
  `ComputeDefinitionsHash64`-Kommentar stellt klar, dass `SightRadius`
  aktuell kein Definitionsfeld ist und eine künftige fraktionsaufgelöste
  Sichtweite eine neue Feld-Generation im Hash braucht. **P2-3** —
  `EconomySystem.SetSlotFaction` ist jetzt guard-gesichert: die Zuweisung
  ist nur zulässig, solange der registrierte Kernel nie gestartet wurde
  (`CurrentTick == Tick.Zero && !IsRunning`), sonst
  `InvalidOperationException` bei unverändertem Zustand;
  `MatchBootstrap` und `Determinism10000Scenario.BuildHost` weisen die
  Slot-Fraktionen deshalb vor `Kernel.Start()` zu.
- **G0-B-Build-Nachweis echt gemacht (alle 10 G0-Checks grün):**
  `G0-BUILD-WINDOWS`/`G0-BUILD-MACOS` in `run_gate_check.py` führten bislang
  nur Voraussetzungs-Prüfungen aus und scheiterten per Design; sie führen
  jetzt den Player-Build real aus (`Nova.Editor.BuildScript` via
  Unity-Batchmode) und verifizieren das Artefakt — für macOS inklusive
  Bundle-Vollständigkeit (`Contents/MacOS`-Executable), nachdem ein
  fehlgeschlagener Build eine irreführende `.app`-Shell hinterlassen hatte.
  Die fehlenden Unity-Build-Module wurden nachinstalliert (Mac-IL2CPP- und
  Windows-Mono-Modul plus die in der Editor-Installation fehlende
  macOS-Mono-Player-Variation aus dem Basis-Editor-Paket). Damit laufen
  erstmals echte, saubere Windows-x64- und macOS-arm64-Builds reproduzierbar
  aus dem Repository ([docs/tech/Testing.md](docs/tech/Testing.md) 1.9.3).
- **Flaky Memory-Assertion im Perf-Harness (`SCALE_500_PRECOMBAT`):** die
  reine Endpunkt-Regel (Retained nach vollem GC: Fensterende ≤ 1,10×
  Baseline nach Warmup) war für Mini-Läufe mit wenigen Sekunden Messfenster
  zu streng — Allokator-/JIT-/GC-Warm-up-Effekte dominierten das kurze
  Fenster und ließen `PerfHarnessTests.MiniRun_ProducesValidArtifactsWithConsistentNumbers`
  lastabhängig rot werden (z. B. Retained 3,61 → 4,13 MiB = 1,144× in Lauf 1),
  während der 120-s-Vertragslauf stabil PASS blieb. Ehrlicher Fix in zwei
  Teilen, ohne die Vertrags-Assertion zu verwässern: (a) Die Regel ist jetzt
  fensterbasiert — der Retained-Heap wird einmal pro Wall-Sekunde des
  Messfensters (jeweils nach vollem GC, zwischen den Ticks) als Probe
  erfasst; ein Lauf besteht, wenn der MEDIAN des Auswertungsfensters
  (letztes Zehntel der Proben, mindestens die letzten 10) die 1,10×-Baseline
  nicht überschreitet — robust gegen Einzelpunkt-Spitzen, empfindlich für
  jeden echten, anhaltenden Leck-Verlauf (`EvaluateMemoryGrowthBounded` als
  reine, per Unit-Test abgedeckte Funktion: linearer Anstieg → FAIL,
  Warm-up-Spitze danach flach → PASS, Toleranzgrenze strikt). (b) Für
  Messfenster < 30 s wird die Assertion als NOT-APPLICABLE ausgewiesen
  (stdout-Hinweis, Artefakt weiterhin `samples [1]` ohne Gate-Anspruch — ein
  übersprungener Assertion-Nachweis in einem echten Gate-Lauf wäre ein
  FAIL); der Vertragslauf (30 s + 3×120 s) wertet weiterhin strikt aus und
  bleibt PASS.
- **Harvest-Orders ohne zustandsabhängige Validierung (Review-Befunde
  P2-1/P2-2):** `Harvest` auf eine unbekannte FieldId wurde still als No-op
  „angewendet", `Harvest` auf Nicht-Harvester vergab eine tote Order, die das
  Economy-System ignorierte. `ICommandStateView` kennt jetzt
  `AetheriumFieldExists` und `IsHarvester`; der `CommandExecutor` prüft beide
  für `CommandKind.Harvest` in fester Reihenfolge (Feld, dann Rolle) und
  lehnt mit `RejectedInvalidTarget` ab — deterministisch, ohne Mutation, der
  Record bleibt im Replay-Strom und wird beim Playback exakt reproduziert
  (Tests in beiden Lanes: Executor-Doubles, Kernel-Rejects ohne
  Order-Vergabe, Replay mit beiden Reject-Fällen und End-Hash-Verifikation).
- **EditMode-Testzählung im Gate-Runner:** `run_gate_check.py` zählte
  `result="Passed"`-Vorkommen inklusive NUnit-Fixture-/Suite-Knoten und
  überzählte damit systematisch (z. B. 274 statt echter 212 Testfälle).
  Die Zählung wertet jetzt nur `test-case`-Knoten aus (Review-Befund).
- **Atomarer Snapshot-Restore (Serialization.md §5, Review-Auflage P1-1):**
  `SimulationKernel.TryRestoreSnapshot` committete Ingress-, Tick-, PRNG- und
  System-Blöcke sequenziell — ein semantisch invalider späterer Block hinter
  gültigem Container-Hash erzeugte Franken-State. Der Restore ist jetzt
  strikt zweiphasig: Phase A validiert alles mutationsfrei
  (`IStatefulSimSystem.TryValidateState`, neuer Interface-Pfad;
  `CommandIngress.TryValidateState`; Kernel-Block weiterhin komplett in
  lokale Variablen), Phase B committet erst nach vollständigem Erfolg — ein
  Fehler lässt den laufenden Host garantiert bitidentisch. Neue Tests in
  beiden Lanes: `FailedRestore_LeavesHostCompletelyUnchanged` (gefälschter,
  semantisch invalider Entity-Store-Block hinter valide neu gehashtem
  Container sowie Foreign-Capacity-Block; Hash UND `SaveSnapshot()`-Bytes vor
  == nach) und `Restore_IsBlockIdBased_IndependentOfRegistrationOrder`
  (Restore und Fortsetzung bei umgekehrter System-Registrierungsreihenfolge).
- **F-001 — Kanonischer Command-Pfad verwirft Commands**
  ([ImplementationAudit](docs/production/ImplementationAudit_2026-07-24.md)):
  Der Prototyp-Kernel pufferte angenommene Commands, ohne sie je an ein
  System zu übergeben. Jetzt ist `SubmitBatch` der einzige Intake und der
  Kernel wendet fällige Batches in Tickphase 1 selbst an; der
  Regressionstest `SealedMoveCommand_ChangesUnitStateAtTargetTick` (beide
  Lanes) weist die Unit-State-Änderung am TargetTick samt
  `Applied`-`CommandResult` nach (Vorher/Nachher, Kontroll-Host ohne
  Command).
- **F-005 — State-Hash und Replay nicht kanonisch:** Der alte
  `CalculateStateHash()` hashte nur den Tick und mutierte per
  `Random.NextUInt()` den PRNG; `StateHashUtility` nutzte FNV-1a statt
  XXH64. Jetzt kanonischer XXH64/NOVA_STATE_V1-Hash über den vollständigen
  autoritativen Block-State, strikt read-only. Regressionstests
  `StateHash_ReflectsStateMutation_AndStaysStableOnRepeat` und
  `StateHash_MatchesSnapshotHeaderHash` (beide Lanes) sowie
  `SimulationKernel_RepeatedStateHash_IsStable_AndDoesNotConsumePrng`
  (EditMode); zwei aufeinanderfolgende Hashes sind identisch, jede
  State-Mutation (Bewegung, Command-Anwendung) ändert den Hash.
- **F-006 — Verbindliche Tickrate verletzt:** `MatchRunner` (0,05 s) und
  `MovementSystem` (0,05 s) rechneten mit 20 Hz. Jetzt gilt die zentrale
  Konstante `SimClock` (10 Hz / 0,1 s) für Host und Systeme.
  Regressionstests `TickRate_IsCanonical10Hz_MovementCoversOneSecondInTenTicks`
  (beide Lanes: 10 Ticks legen exakt 1 Sekunde Sim-Zeit zurück) und
  `MatchRunner_TickRate_IsCanonical10Hz` (EditMode).
- **EditMode-Suite wieder grün (G0-B):**
  `LockstepRelayBufferTests.CommandEnvelopeNetPacket_Serialization_PreservesValues`
  erwartete ein veraltetes 41-Byte-Paket und der `Deserialize`-Guard
  (`Length < 41`) lehnte die realen 34-Byte-Pakete der eigenen
  `Serialize`-Ausgabe ab; Test und Guard sind auf das tatsächliche
  34-Byte-Wire-Format (4+1+1+4+2+4+2+4+4+8) angeglichen. Hinweis: Die vom
  Gate-Runner gemeldeten „7 Fehler" waren Zählartefakte aggregierter
  NUnit-Suiten — real war genau dieser eine Test rot.
- **G0-A2-Review-N3:** Authorize-Step im Workflow erhält `GH_TOKEN`
  (sonst scheitert jede G1+-Autorisierung an `E_RECEIPT_GITHUB`);
  `check_docs.py` erzwingt in PR-CI append-only für
  `quality/authorizations/` (Guard gegen Attempt-Substitution, N-1);
  Restrisiko in Testing.md/Deployment.md dokumentiert (1.9.2).
- **G0-A2-Review-Härtung (adversariales Re-Review):** Vorgänger-Receipts
  werden im `--authorize`-Modus jetzt online gegen die GitHub-API
  verifiziert (`gh` mit `GH_TOKEN`/`GITHUB_TOKEN`; exakter
  `workflow_dispatch`-Run/-Attempt, Workflow, `conclusion=success`,
  Trusted-Head, erfolgreicher `gate-evidence-authorize`-Job; jeder Mismatch
  oder fehlendes Token/`gh` endet fail-closed `E_RECEIPT_GITHUB`) — ein
  handgeschriebenes Receipt mit erfundener Run-ID wird nicht mehr
  akzeptiert. Szenarioprofile und -schwellen kommen im Trusted-Modus
  ausschließlich aus dem Trusted-Checkout, und `--authorize` verlangt
  zusätzlich die Identität von `content.scenarioSha256` mit dem
  Trusted-Vertrag (`E_SCENARIO_CONTRACT`). Der Authorize-Job erhält
  `permissions: actions: read`, `GITHUB_WORKFLOW_REF` muss exakt
  `...@refs/heads/main` lauten, und `validate_receipt` prüft den
  `evidenceCarrierCommitSha` bei `verify_git` gegen die echte Git-Historie
  (`E_RECEIPT_CARRIER`). Self-Test auf 73 Semantik- plus 7
  Topologie-Kontrollen erweitert (Fake-`gh`-Harness ohne Netzwerk).
- **D-066-Fail-Closed-Korrektur nach zweitem Merge-Review:** Der zuvor als
  geschlossen bezeichnete N-1-Befund war logisch nicht geschlossen. Der
  laufende Authorize-Job verlangte bereits seinen eigenen erfolgreichen
  Abschluss, und Subject-/Evidence-Carrier-Commit waren vermischt. Jeder
  `verdict=pass` endet nun auch mit alten Trust-Argumenten zwingend mit
  `E_AUTHORIZATION_BOOTSTRAP`; der frühere positive Trusted-Topology-Test ist
  ein Negativtest. Die 58 Semantik- und vier Topologie-Kontrollen bleiben
  grün, autorisieren aber bewusst keinen Pass.
- GitHub-Actions im `docs-check`- und `integrity`-Pfad sind auf vollständige
  Commit-SHAs gepinnt; Checkout-Credentials bleiben nicht erhalten und beide
  Workflows verwenden explizit Node `24.4.1`.
- `G0-BUILD-WINDOWS` und `G0-BUILD-MACOS` können nicht länger allein durch
  vorhandene Build-Voraussetzungen `pass` melden. Bis reale
  plattformspezifische Builds angebunden sind, enden beide Kriterien
  ausdrücklich fail-closed.
- **G0-A-Härtung nach adversarialem Review:** Der Authorize-Job führt
  Validator und Ajv nun aus dem Trusted-Checkout aus und liest die Evidence
  über den neuen `--subject-root`-Parameter aus dem Subject-Checkout —
  kein Evidence-Staging nach `trusted/` mehr (Trusted-Cleanliness bleibt
  intakt); neuer CLI-Topologie-End-to-End-Selbsttest
  (`--self-test-topology`, zusätzlich in `--self-test` und im
  Integrity-Job).
- `generate_trust_context.py` verifiziert jedes
  `authorizedEvidence`-Kettenglied gegen die GitHub-API (Run gehört zu
  `quality-gate.yml`, `conclusion=success`, `headSha`=Subject-Commit,
  Job erfolgreich); fehlendes `gh`-Tool/Token ist fail-closed. Neue
  Negativkontrollen: falsches `GITHUB_JOB`/`GITHUB_WORKFLOW_REF`/
  `NOVA_TRUST_CONTEXT_SHA256`, Kettenglied mit fremdem Subject-Commit und
  nur lokal erzeugter Vorgänger.
- Authorize-Job nur noch bei `workflow_dispatch` auf `refs/heads/main`;
  `trustedSha` muss per `git merge-base --is-ancestor` Vorgänger von
  `origin/main` sein und darf nicht der Subject-Commit sein;
  Dispatch-Inputs laufen ausschließlich über `env:`-Mapping.
- `commandId`-Präfix-Konvention (`impl-`/`review-`) wird maschinell
  erzwungen; `NOVA_GATE_EXECUTOR`, Reviewer-Reproduktion und die
  Trusted-/Subject-Hash-Differenz sind in Testing.md/Deployment.md
  dokumentiert.
- **N-1 (Re-Review) geschlossen — Authorize-Run-Bindung:** Die
  GitHub-Verifikation der `authorizedEvidence`-Kette erzwingt jetzt pro
  Eintrag `event=workflow_dispatch`, den erfolgreichen
  `gate-evidence-authorize`-Job (Evidence-`ci.jobName`-Konstante und
  Workflow-Anzeigename darauf vereinheitlicht) und über die Kette
  eindeutige Run-IDs — PR-Event-Runs, reine Integrity-Runs und
  Run-Wiederverwendung werden abgelehnt; neue Generator-Kontrollen mit
  gemockter GitHub-API (kein Netzwerk im Self-Test).
- **G1-Command-Pfad, Review-Nachzügler:** `CommandDedupeState.TryDeserialize`
  wirft bei manipuliertem Snapshot mit doppeltem Pending-Schlüssel nicht mehr
  (`ArgumentException`), sondern liefert `false` ohne Teilmutation; jeder
  deserialisierte Pending-Record wird zusätzlich inhaltlich gegen die
  §4-Grundregeln revalidiert (Slot-/Block-Konsistenz, Stream-Kind statt
  Session-Aktion, Sequenz ≠ 0, kanonischer Payload) und der Ingress lehnt
  Snapshots mit Pending-Records auf für die Session inaktiven Slots ganz ab.
  `CommandIngress.TryAcceptRecordBytes` erzwingt jetzt
  `consumed == bytes.Length` — Trailing-Bytes hinter einem validen Record sind
  ein struktureller Framing-Fehler (neuer Grund `TrailingBytes`), auf dem
  Live- wie auf dem Replay-Import-Pfad. Tests für alle Fälle in beiden Lanes.
- **EntityStore-Restore lehnt negative Geschwindigkeit/Radien ab
  (Review-Befund P2-2):** `EntityManager.TryValidateState` deserialisierte
  `MoveSpeed`, `Radius` und `SightRadius` ohne Vorzeichenprüfung — ein
  manipulierter Snapshot hinter gültigen Container-Hashes konnte einen
  negativen Sichtradius einschleusen, der den nächsten FoW-Recompute per
  `InvalidOperationException` abbricht. Die Validate-Phase weist jetzt alle
  drei Felder bei `RawValue < 0` zurück; der Host bleibt unverändert. Tests
  in beiden Lanes (negierter MoveSpeed/Kollisionsradius/Sichtradius →
  Validate und Restore false, Store unverändert; valider Block weiterhin ok).
- **Builder-Rollenprüfung am Construction-Restore (Review-Befund P2-2):**
  `ConstructionSystem.TryParseState` akzeptierte `AssignedBuilderRaw` ohne
  Rollenprüfung — ein manipulierter Block konnte eine Kampfeinheit als
  Bauarbeiter einschleusen. Die Validate-Phase prüft jetzt jede im aktuellen
  Entity Store sichtbare Zuweisung auf `Role == Builder` und
  Slot-Gleichheit mit der Baustelle (Verstoß → Restore lehnt ab, Host
  unverändert); beim Restore in einen frischen Host ist die Entity-Referenz
  zum Validate-Zeitpunkt nicht beurteilbar (Kernel validiert jeden Block
  gegen den Pre-Restore-Stand) — dieser Fall ist durch dieselbe Rollen-/
  Owner-Prüfung als Defense-in-depth in `ProgressSites` abgedeckt, die eine
  unbrauchbare Zuweisung deterministisch neu auflöst. Tests in beiden Lanes
  (Kampfeinheit als AssignedBuilder → Validate und Restore false, Host
  unverändert; Live-Rollenwechsel → Reassign, Baustelle pausiert).
- **Off-Map-RallyPoint wird zustandsabhängig abgelehnt (Review-Befund
  P2-3):** `SetRallyPoint` akzeptierte Ziele außerhalb der Karte. Die
  Domain-Validierung prüft das SimFixed-Ziel jetzt grid-gemappt (floor)
  gegen die 128×128-Map-Grenzen — außerhalb → `RejectedInvalidTarget`, das
  bestehende Rally bleibt unverändert und laufende Produktion parkt nicht.
  Tests in beiden Lanes (Off-Map/negativ → Reject, Rally unverändert, Queue
  spawnt normal am bisherigen/Default-Rally; Map-Ecke legal; Command-Pfad
  über den versiegelten Intake).

### Entfernt
- Der zirkuläre `workflow_dispatch`-Job `gate-evidence-authorize` und
  `.github/scripts/generate_trust_context.py` wurden aus dem Mergekandidaten
  entfernt. Es existiert weder ein geschützter Authorize-Lauf noch ein
  konfiguriertes `quality-gate`-Environment; G0-A und G0 bleiben offen.

### Entschieden
- **D-075 (Fraktions-Achse in der kanonischen Simulation) — vom Agenten unter
  ausdrücklicher Inhaber-Delegation entschieden (Teil-Entscheidung per
  Inhaber-Sprint-Briefing vorgegeben):** Das Manifest modelliert zwei
  Fraktionen, die Simulation kannte nur eine flache geteilte Tabelle.
  Entschieden gegen die Alternativen „flache Tabelle plus kosmetische
  Fraktion" und „getrennte Definitions-Assemblies je Fraktion": **die
  Fraktions-Achse lebt in `SimDefinitions`** — 34 Definitionen, die
  Allianz-Id IST der `UnitRole`-Wire-Wert (1..17), die Legion-Id addiert
  17, Auflösung über `ToDefinitionId` und die Slot-Fraktion aus dem
  Economy-Zustand (Snapshotblock v2, achtes Fingerprint-Array, vor
  `Kernel.Start()` gebunden und danach guard-gesperrt). Tragend:
  Wire-Kompatibilität (`IsValidDefinitionId` bleibt `!= 0`), der einmalige
  Formatreset im offenen Pre-G1-Fenster (D-068) und die
  D-068-Kostenasymmetrie, die Kosmetik nicht tragen kann. Teil-Entscheidung
  Legion-Fahrzeugschaden: **der konkrete GDD-Wert schlägt die Ableitung**,
  wo Vehicles.md eine Legion-Schadenszeile nennt (28/50/60); die Ableitung
  gilt nur, wo die GDDs schweigen. Offen darin: die Hyäne-Zeile (Legion
  Scout) in Vehicles.md nennt eine von der abgeleiteten 10 abweichende
  konkrete Zahl — der Scout bleibt bis zur Inhaberentscheidung abgeleitet
  (registriert im [DecisionLog](docs/production/DecisionLog.md), überstimmbar).
- **D-074 (Autorität der Schaden-gegen-Panzerung-Matrix) — vom Agenten unter
  ausdrücklicher Inhaber-Delegation entschieden, nicht vom Inhaber selbst:**
  Die Fachdokumentation führte drei einander widersprechende Matrizen
  ([ArmorSystem.md](docs/gamedesign/ArmorSystem.md) 6 × 6,
  [Infantry.md](docs/gamedesign/Infantry.md) 6 × 4 plus einer siebten
  Schadensart „Kristall", [Vehicles.md](docs/gamedesign/Vehicles.md) 5 × 4),
  teils mit gegenläufigen Werten — Energie gegen Schwer 0,75 gegen 1,25,
  Explosiv gegen Gebäude 0,75 gegen 1,25. Entschieden gegen die Alternativen
  „Einheitenkategorie-Achse führend" und „neue zusammengeführte Matrix":
  **ArmorSystem.md ist alleinige Autorität**, seine 36 Werte sind kanonisch und
  bleiben unverändert. Tragend war der Bestand, nicht der Rang: die
  Panzerungsklasse ist laut ArmorSystem.md ein Einheitenattribut, während
  „vs. Fahrzeug" `Light`/`Medium`/`Heavy` zusammenfaltet und damit genau die
  Unterscheidung verliert, aus der Konterspiel entsteht; Vehicles.md verweist in
  seiner eigenen D-047-Regel bereits auf ArmorSystem.md für die Konterlogik;
  und ArmorSystem.md ist als einzige Quelle schon als flacher 36-Zahlen-Satz
  geschrieben. Folgen: „Kristall" ist **keine** Schadensart (Evolvierten-Inhalt
  außerhalb MS-1), und weil ArmorSystem.md Leichten **und** Kampfpanzer der
  Klasse `Medium` zuordnet, bleibt die `Heavy`-Spalte in MS-1 unbespielt —
  festgehalten statt stillschweigend repariert, registriert im
  [ScopeLedger](docs/production/ScopeLedger.md). Der Eintrag ist im
  [DecisionLog](docs/production/DecisionLog.md) ausdrücklich als
  agent-entschieden gekennzeichnet und **jederzeit vom Inhaber überstimmbar**;
  eine Umkehr wäre eine Datenänderung, keine Strukturänderung.
- **D-069:** Kanalbelegung der Art-Mask-Textur — R=Metallic/G=Occlusion/
  B=TeamMask/A=Smoothness (URP-Lit-kompatibel).
- **D-070:** 0-€-Beschaffungspfad für den Art-Strang mit Anbieter-Whitelist
  (CC0-Quellen, Hunyuan3D 2.1 lokal, OpenAI Image API, Sketchfab per
  Einzelfallprüfung) und Blacklist (Meshy/Tripo3D Free-Tier, Default-Deny).
- **D-071:** Art-seitige Grid-Zellgröße 3,0 m und feste MS-1-Gebäude-
  Footprints (Power 3×3, Refinery 4×4, Barracks 3×3, ResearchLab 3×3).
- **D-072:** Fraktionspaletten Allianz (`#8A9199`/`#2C6E9E`/`#4FD8FF`) und
  Legion (`#7A3524`/`#B08430`/`#2B2018`) für MS-1 verbindlich.
- **D-073:** Sonniss-GDC-Bundle-Rohdateien werden gemäß restriktiver
  Lizenzlesart nicht ins öffentliche Repository eingecheckt.
- **D-066 (Fail-Closed-Foundation und zweiphasige Autorisierung):** D-065
  wurde ersetzt. G0-A1 umfasst nur die mergefähige Integritätsgrundlage;
  G0-A2 muss Subject, Evidence-Carrier und Trusted Tooling trennen und
  append-only `GateAuthorization.json`-Receipts nach abgeschlossenem
  geschütztem Lauf verifizieren. Kein Lauf darf seinen eigenen Erfolg
  attestieren.
- **D-065 (Authorize-Run-Bindung der Evidence-Kette):** Replay-/Reuse-
  Befund N-1 aus dem G0-A-Re-Review; entschieden wurde die Event-/Job-/
  Eindeutigkeits-Bindung gegen die Alternativen „Doku abschwächen" und
  „Evidence-Hash als Run-Artefakt", mit ehrlich dokumentiertem Restrisiko
  (Anker: Environment-Protection plus `NOVA_TRUST_CONTEXT_SHA256`).

### Geändert
- **Widersprechende Schadensmatrizen in der Fachdokumentation aufgehoben
  (D-074):** [ArmorSystem.md](docs/gamedesign/ArmorSystem.md) (0.4.0) trägt
  einen Autoritätsvermerk und den Hinweis auf die Implementierung — **keiner
  der 36 Werte wurde geändert**. Die abgedrifteten Lokaltabellen in
  [Infantry.md](docs/gamedesign/Infantry.md) (0.5.0) und
  [Vehicles.md](docs/gamedesign/Vehicles.md) (0.5.0) sind ersatzlos entfernt
  und durch Verweise ersetzt; die Einheiten-Stattabellen, Rollen, Drohnen- und
  Elite-Werte beider Dokumente bleiben unverändert.
  [ScopeLedger.md](docs/production/ScopeLedger.md) (0.2.0) erhält eine Zeile für
  die unkonsumierte Sichtbarmachung der letzten Einheiten, schreibt die Zeilen
  zu Siegauswertung und Waffenprofil auf den neuen Stand fort — ohne sie zu
  entfernen, denn es gibt keinen auflösenden Gate-Nachweis — und führt in einem
  getrennten Anhang vier Verschiebungen ohne Manifest-Schlüsselpfad
  („Kristall", `Heavy`, `Air`, Feuer/Bio/Strahlung), damit die
  „Zeigen statt kopieren"-Regel des Hauptregisters unangetastet bleibt.
  [GrayboxLog.md](docs/production/GrayboxLog.md) (0.2.0) protokolliert die
  Sitzung GB-002 append-only.
- **Movement-State auf SimFixed migriert (Q-040(i)-Auflösung implementiert,
  Ratifizierung per D-ID ausstehend; ohne Gate-Status, ohne Evidence):**
  `Transform2D` speichert Position als `SimFixed` (Q16.16) und Rotation als
  `SimAngle` statt float-Radianten; `UnitState.MoveSpeed`/`Radius` sind
  `SimFixed`; `MovementSystem` rechnet den gesamten Tick-Pfad (Flow-Steering,
  Separation, Normalisierung, Integration, Heading) in kanonischem
  Fixed-Point — Zielrichtung via `SimTrig.Atan2`, der Tick-Schritt als
  exakte Division `MoveSpeed / 10` statt einer gerundeten
  0.1-s-Q16.16-Konstanten. Neu in `Nova.Core`: `SimTrig` — rein ganzzahlige
  CORDIC-Trigonometrie (`Sin`/`Cos` via CORDIC-generierte
  Viertelwellen-Tabelle, `Atan2` via Vectoring-CORDIC, `Sqrt` via
  Integer-Wurzel; gemessener Max-Fehler ≤ 0,5 Q16.16-Rohwerte bei Sin/Cos
  und ≤ 0,5 Winkeleinheiten bei Atan2, Roundtrip exakt über alle 65536
  Winkel; kein float/double im Pfad). `EntityManager`-Snapshot-Block auf
  Version 2 gehoben (SimFixed-Rohwerte int32, SimAngle uint16 statt
  Float-Bitmustern); v1-Blöcke werden abgelehnt (Pre-G1-Reset, kein
  Migrationspfad). Boundary-Konvertierungen `SimFixed.FromFloat`/`ToFloat`
  für Presentation und Prototyp-Scaffolding; `SimMath`-Float-Pfade haben
  keine autoritativen Aufrufer mehr. Zwingende Aufrufer (Vision, Combat,
  Commander, EvolvedFaction, Production, Selection, UnitView, Skirmish-AI,
  SimRunner) und beide Test-Lanes angepasst; der kanonische State-Hash und
  alle bewegungsabhängigen Golden-Werte ändern sich erwartbar durch die
  Numerik-/Formatänderung (ehrlich neu erzeugt, kein Rückwärts-Glätten).
  [docs/production/OpenQuestions.md](docs/production/OpenQuestions.md) Q-040(i).
- **Art-Strang MS-1, Folgeänderungen an bestehenden Dokumenten (ohne
  Gate-Status, ohne Evidence):** Hunyuan3D-Lizenzangabe in
  [docs/assets/Licenses.md](docs/assets/Licenses.md) nach Version
  differenziert und um Anbieter-Whitelist/Blacklist ergänzt;
  Sonniss-Weitergaberegel korrigiert (restriktive Lesart, D-073);
  Synty-Zuordnungen in
  [docs/assets/AssetRegister.md](docs/assets/AssetRegister.md) als durch
  D-054 ersetzt markiert und um eine MS-1-Strategiespalte ergänzt;
  Art-Namensebene in
  [docs/tech/NamingConvention.md](docs/tech/NamingConvention.md) ergänzt.
- **Float-/Double-Numerik im Movement-State als Risiko deklariert
  (Review-Auflage P1-2, Q-040(i)):**
  [docs/production/OpenQuestions.md](docs/production/OpenQuestions.md)
  (Version 1.11.3) benennt offen, dass `Transform2D`-Floats und die
  `SimMath`-Transzendenten (`Atan2`/`Sin`/`Cos`/`Sqrt` auf `System.Math`) im
  hash-relevanten Movement-State zwar pro Runtime bitstabil serialisieren,
  aber zwischen Mono, IL2CPP und .NET nicht garantiert bitidentisch sind —
  ein latenter Cross-Runtime-Desync im Sinne von SimulationCore.md §1/§9.
  Provisorium: Die Prototyp-Floats bleiben bis zur Movement-Domain-Scheibe;
  vor dem G1-Schema-Freeze ist per D-ID zwischen SimFixed-Migration und
  kanonischer Fixed-Point-Approximation zu entscheiden. Doc-Kommentare in
  `MovementSystem` und `Transform2D` verweisen auf Q-040(i); der Kernel
  dokumentiert an `RegisterSystem` die Pflicht, dass jedes System mit
  Match-relevantem State `IStatefulSimSystem` implementieren muss — die
  Scaffolding-Systeme (Combat/Economy/Production/Vision mit 20-Hz- und
  Float-Relikten) sind vor Registrierung zu migrieren (Review-Nachzügler
  P2-1).
- **API-Bruch `SimulationKernel` (intendiert; D-055 erklärt Prototypen zu
  Input, D-057 macht Prototyp-Formate unsupported):** Der Konstruktor nimmt
  jetzt die Konkretklasse `SimRandom` (statt `ISimRandom`, das Interface
  selbst bleibt unverändert); `SubmitCommand(CommandEnvelope)` und die
  `ICommandSink`-Implementierung entfallen ersatzlos — Command-Intake
  ausschließlich über `SubmitBatch`. `MatchRunner` fährt den kanonischen
  10-Hz-Lockstep (pro Tick: Batch versiegeln → submitten → Kernel steppen →
  Session-Tick) und bindet Session/Ingress/Loopback/State-View;
  `tools/Nova.SimRunner` nutzt denselben Host-Pfad, reicht die
  1.000-Unit-Last als echte versiegelte Move-Batches ein und belegt zwei
  identische Runs mit bitgleichem State-Hash.
  `MovementSystem.TickDeltaSeconds` und `MatchRunner.TickDeltaTime` sind
  Aliase der `SimClock`-Konstante. Die drei historischen Modul-Specs
  `CommandSystem_Spec.md`, `LockstepRelay_Spec.md` und
  `LockstepReplay_Spec.md` verweisen nach dem Entfernen der Prototyp-Tests
  auf die neue Suite (je Version 1.1.1).
- **G1-Vorarbeit, Review-Auflagen (P2-1 bis P2-5):** `SimRandom.SetSeed`
  nutzt jetzt kanonisches SplitMix64 mit einem einzigen laufenden Zustand
  (pro Wort um `0x9E3779B97F4A7C15` weitergeschaltet), sodass die beiden
  Zustandswörter auch für Seed 0 statistisch unabhängig sind; die
  PRNG-Sequenz ändert sich dadurch (Pre-G1-Reset, D-057) und die
  Golden-Vektoren für Seed 0/1 sind in beiden Test-Lanes neu erzeugt.
  Testlücken geschlossen: Division mit negativem Divisor/Dividend inklusive
  Tie-Fällen, `SimAngle.FromDegrees` mit gebrochenen Graden inklusive
  Tie-Fällen sowie `MaxValue × MaxValue`-Überlauf. Das Unity-Projekt setzt
  nun `NOVA_FIXED_POINT` in `scriptingDefineSymbols` (Standalone-Gruppe),
  womit Unity- und .NET-Projekte denselben Define-Stand haben
  ([docs/tech/SimulationCore.md](docs/tech/SimulationCore.md) §9).
- **G0-B-Assembly-Bereinigung (D-061-Kontrakt):** Die reinen Logik-Klassen
  `SelectionManager`, `CommandCardPresenter` und `MinimapRenderer` liegen
  jetzt in `Nova.Gameplay` (Host/Bridge), die Definitions-SOs
  `MapDefinitionSO`/`MapBiomeType` in `Nova.Data`; die leeren Assemblies
  `Nova.Presentation.UI`/`Nova.Presentation.Maps` und die Test-Assembly
  `Nova.Presentation.Tests` sind aufgelöst. Die vier Tests wurden ohne
  inhaltliche Änderung nach `Nova.Data.Tests`/`Nova.Gameplay.Tests`
  verschoben (kein Test gelöscht); die historischen Modul-Specs
  `MapExpansion_Spec.md`/`RtsUi_Spec.md` verweisen auf die neuen Pfade.
- `.gitignore`: die pauschale `*.csproj`-Regel ist jetzt root-scharf
  (`/*.csproj`), damit handgeschriebene Tool-Projektdateien versionierbar
  bleiben, während Unity-generierte Root-Projektdateien weiterhin ignoriert
  werden; zusätzlich wird das Unity-generierte Purchasing-Artefakt
  `Assets/Resources/BillingMode.json` (plus `.meta`) ignoriert.
- `tools/Nova.SimRunner/Nova.SimRunner.csproj` ist jetzt versioniert und
  setzt den Determinismus-Define `NOVA_FIXED_POINT`; der Define-Name ist in
  [docs/tech/SimulationCore.md](docs/tech/SimulationCore.md) §9 (Version
  1.1.1) als verbindlich für Unity und SimRunner festgelegt.
- **GateEvidence-Schema auf `1.4.0` und Szenariovertrag auf
  `two-phase-receipt-d066`:** `priorGateReceipts` ist ab G1 Pflicht
  (geordnete Kette G0..G(n-1) aus `{gateId, receiptPath, receiptSha256}`,
  für G0 leer/null) und ersetzt die Evidence-Kette als
  Autorisierungsnachweis; die Same-Subject-`priorGateEvidence`-Kette bleibt
  als Integritätsprüfung erhalten. Der tote `_validate_trust_context`-Rumpf,
  `TRUST_CONTEXT_VERSION="2.0.0"` und `--trust-context` sind entfernt; ein
  `verdict=pass` endet außerhalb von `--authorize` weiterhin mit
  `E_AUTHORIZATION_BOOTSTRAP`. Testing.md §10 und Deployment.md §7 stehen
  auf dem implementierten 1.4-Stand (1.8.0).
- **G0-A in G0-A1/G0-A2 geteilt:** Schema 1.3 und
  `quality-gate / integrity` sind ausschließlich Integrity. Der
  Szenariovertrag meldet `integrity-only-d066`, `ci.jobName` bezeichnet den
  Evidence-Produzenten und der künftige Receipt-Vertrag startet ohne
  Migration mit GateEvidence 1.4.0/Trust-Kontext 3.0.0. Wiki,
  Beitragsregeln, Test- und Deploymentvertrag stehen auf 0.12.0/D-066.
- **Szenariovertrag `mvp-v1.json` auf `1.3.0`:** `authorizationStatus`
  beschreibt den vorgesehenen
  `trusted-tool-checkout-authorization`-Pfad; bis dessen geschützter Merge und
  Abnahme bleiben G0 und jeder lokale/untrusted Pass gesperrt. Die
  Windows-x64-Referenz (`performanceMethod`,
  jetzt mit `os`/`hardware`) und die neue Mac-M2-Funktionsmethode
  (`macM2FunctionalMethod`) sind getrennte Methodenprofile, und
  `MAC_M2_FUNCTIONAL` referenziert letztere.
- **Planung vollständig auf D-056–D-064 rebaselined:** Sprint 7 bleibt bei
  offenem G0; MS-0 und MS-1 sind unerreicht. Milestones, SprintPlanning,
  Roadmap, RiskAnalysis und Sprint06_Report verwenden dieselbe Gate- und
  Evidence-Logik.
- Sprint 6 ist eindeutig als durch D-055 beendet/ersetzt dokumentiert;
  Sprint 7 ist gestartet, aber ausschließlich G0 ist zur Implementierung
  freigegeben.
- Aktive Technikverträge wurden auf Q16.16 ab G1, kanonische Commands/
  Snapshots/Replays, XXH64 Seed 0, feste MS-1-Kapazitäten, committed FoW und
  die getrennten 100-/500-Workloads angeglichen.
- Branch-Governance auf geschütztes `main`, kurze Topic-Branches,
  Squash/lineare Historie, kein dauerhafter Integrationsbranch und explizite
  Agentenautorität pro Commit-/Push-Aktion vereinheitlicht.
- Engine-Pin auf Unity `6000.5.4f1`, Revision `d550df8bd089`, URP korrigiert;
  automatische Editor-Upgrades sind ausgeschlossen.
- Sieg, Remis, 45-Minuten-Limit und Last-Unit-Reveal sind für MS-1 in D-056,
  Inhaltsmanifest, State und maschinenlesbarem Contentvertrag geschlossen.
- VictoryConditions und MultiplayerModes besitzen lokale, führende
  MS-1-Overrides; Commander/Voice/Portrait/Doktrinen sind eindeutig Post-MVP
  und D-009 ist für MS-1 teilersetzt.
- Verbliebene Commander-/Audio-Altformulierungen in Vision, Asset-Register,
  OpenQuestions und AudioArchitecture sind auf Post-MVP vereinheitlicht;
  D-039 ist als vorhandene, durch D-056/D-058 begrenzte Entscheidung verankert.
- Q16.16-Bereich, `DefinitionId`, `EntityId`-Bitlayout, Command-Kappen,
  Schema-/Count-Breiten und nullterminierte XXH64-Domänen sind bytegenau
  festgelegt; Pause/Save/Load sind eindeutig Session-Aktionen.
- Alle 17 alten `docs/tech/modules/*_Spec.md` sind als historischer,
  nicht verbindlicher Prototyp-/Scaffolding-Stand gemäß D-055 markiert.
- V2 und V3 sind als eigene 500-Objekt-Szenarien ergänzt; Rendering,
  Animation und FoW-Budget wurden an 128²/5 Hz sowie MG/Rocket-MS-1
  angeglichen.
- Historische Änderungsverläufe wurden unverändert wiederhergestellt;
  `docs-check` erzwingt Kopfzeile, Pflichtabschnitte, terminale History,
  fünf strikte Quality-JSONs, gepinntes Ajv und die
  Evidence-Negativkontrollen; Änderungen unter `quality/**` lösen den
  Workflow ebenfalls aus.
- Roadmap enthält keine aktive 445-PT- oder Kalenderzusage mehr:
  Aufwandsspanne frühestens nach G2, Kalenderkorridor frühestens nach G4.
- **Recovery-Baseline nach strengem Implementierungs-Audit:** MS-0 ist offen, das MVP ist nicht erreicht und Alpha hat nicht begonnen. Die bisherigen Sprint-7-Einträge belegen nur vorhandene Prototyp-Struktur, nicht fertige oder integrierte Features.
- [ImplementationAudit_2026-07-24.md](docs/production/ImplementationAudit_2026-07-24.md) dokumentiert Testfehler, Integrationslücken, fehlende Akzeptanznachweise und Planungswidersprüche am eingefrorenen Stand `460290e`.
- [MVPRecoveryPlan.md](docs/production/MVPRecoveryPlan.md) ersetzt pauschale Modul-Fertigmeldungen durch sequenzielle Gates G0–G5.
- Sprint-6-Abschluss, Sprint-7-GO, 445-PT-Verbindlichkeit sowie die ungültigen Schließungen Q-018/Q-019 wurden durch D-055 zurückgezogen; R-16 wurde reaktiviert und R-17 ergänzt.
- [docs/tech/Commands.md](docs/tech/Commands.md) 1.1.1 (Review-Klarstellungen
  ohne Vertragsänderung): Reflection-Restrisiko der kompilierseitig
  erzwungenen Vertrauensgrenze dokumentiert, Vertrauensannahme des
  öffentlichen Byte-Intake samt caller-seitiger Fingerprint-Prüfung beim
  Replay-Import festgehalten und die Zustellungsannahme der Watermark-Dedupe
  (zuverlässig geordnet je Spieler; Lücken-Fehlermodell als
  Post-MVP-Netzwerk-Anforderung) präzisiert.
- **Q-040 um Radar-Kadenz erweitert (Review-Befund P2-1):**
  [docs/production/OpenQuestions.md](docs/production/OpenQuestions.md)
  (Version 1.11.5) deklariert als Punkt (j), dass FogOfWar.md §6.3 nur die
  `Visible`-Kadenz an den 5-Hz-Commit-Tick bindet; Provisorium ist die
  Ableitung der Radar-Pings aus 10-Hz-Live-Positionen (Pings feuern auch vor
  dem ersten committed View), Kandidat ist die Bindung an die
  5-Hz-Commit-Ticks — Ratifizierung per D-ID vor dem G1-Schema-Freeze. Der
  `GetRadarSignatures`-Doc-Kommentar verweist auf Q-040(j).
- **Index-Seitenkanal als MS-1-Einschränkung dokumentiert (Review-Befund
  P2-3):** `GetVisibleEntities` vermerkt, dass eigene `EntityId`-Werte die
  Allokationsreihenfolge des geteilten Allocators offenbaren; die
  Datenschutzgrenze nach FogOfWar.md §4 liegt auf View-Ebene, ID-Metadaten
  sind nicht Teil des MS-1-Datenschutzmodells (Härtung Post-MS-1).
- **Construction-Timing-Provisorien dokumentiert (Review-Befunde P2-1/P2-4,
  OpenQuestions 1.11.6):** Q-040 um (k) erweitert — (k1) Same-Tick-Power-Stacking:
  die Power-Deckungsprüfung liest die committed Balance des Vorticks,
  mehrere power-ziehende Placements im selben Tick können die Deckung
  kollektiv überziehen (deterministisch, selbstbestraffend via
  Low-Power-Multiplikator; Kandidat: Placement-Limit pro Tick); (k2)
  Footprint-Sweep-Timing: kampfzerstörte Footprints werden erst im Sweep
  des Folgeticks freigegeben, ein PlaceBuilding exakt im Tick nach der
  Zerstörung findet die Zelle noch belegt (deterministisch). Beide Fakten
  stehen zusätzlich im `ConstructionSystem`-Klassenkommentar; Ratifizierung
  per D-ID vor dem G1-Schema-Freeze.

### Entfernt
- **Prototyp-Command-/Hash-/Replay-/Relay-Pfade (Pre-G1-Reset, D-057;
  ersetzt durch den kanonischen Pfad):** `CommandEnvelope`,
  `CommandType`/`CommandIssuer`, `ICommandSink` und `CommandProcessorSystem`
  (separater Buffer, der nie beliefert wurde — F-001), `StateHashUtility`
  (FNV-1a — F-005), `ReplayBuffer` (rein aufzeichnend, ohne
  Playback-Nachweis — F-005) sowie `CommandEnvelopeNetPacket` und
  `LockstepRelayBuffer` (34-Byte-Prototypformat) samt zugehöriger
  Prototyp-Tests (`CommandSystemTests`, `LockstepReplayTests`,
  `LockstepRelayBufferTests`). Der alte
  `DeterministicSimTests`-CommandEnvelope-Test ist entfallen und durch die
  neue Kernel-Integration-Suite ehrlich ersetzt (dokumentiert im Commit).
- Getrackte Build-Outputs unter `tools/Nova.SimRunner/bin/` sind aus dem
  Git-Index entfernt (die Dateien bleiben lokal erhalten; der Ordner ist
  ignoriert). Damit erfüllt das Repo die G0-B-Regel „keine getrackten
  generierten Binärdateien".

### Entschieden
- **D-056:** Dependency-closed MS-1 mit Allianz/Legion, Glutrinne, neun
  Gebäude- und acht Einheitenrollen je Fraktion, vollständigem D-010-
  Aetherium und definiertem Produktminimum; Q-031/Q-038 geschlossen.
- **D-057:** Kanonischer Q16.16-/Command-/State-/Persistence-Vertrag ab G1;
  exakte Plattformparität, einmaliger Pre-G1-Formatreset; Q-039 geschlossen.
- **D-058:** Feste MS-1-Slots, Entity-/Snapshot-/Flow-Cache-Kappen und
  autoritatives 5-Hz-Team-FoW; Q-032 geschlossen.
- **D-059:** Geschütztes `main` plus kurze Topic-Branches ersetzt D-050.
- **D-060:** Exakter Unity-Pin `6000.5.4f1` ersetzt D-006.
- **D-061:** Ausführbare G0–G5-Gates, unveränderliche Evidence, getrennte
  Full-Content-/Scale-Workloads und feste Laufkadenz; Q-033/Q-034 geschlossen.
- **D-062:** Szenarioassertions und -schwellen an artefaktgebundene
  Rohsamples, Content/Scenario an Subject-Git-Blobs und G1–G5 an eine
  rekursive Same-Subject-Vorgängergate-Kette gebunden.
- **D-063:** Evidence-Schema 1.2 mit kanonischen kriterienspezifischen
  Check-/Log-Artefakten, rekursivem Ajv, exakten Units, drei getrennten
  Performance-Läufen und externem Protected-CI-Trust-Kontext; lokales
  Evidence darf keinen Gate-Pass autorisieren.
- **D-064:** Schema 1.2 bleibt eine fail-closed Integritätsvorstufe. G0-A
  implementiert vor der Plattformarbeit einen subject-unabhängigen,
  nicht selbstautorisierenden Trusted-Gate-Bootstrap; erst Schema 1.3 bindet
  das Trust-Bundle, die vollständige Gate-Kette und exakte Messumgebungen.
- **D-055:** Vorhandenen Code als Prototyp erhalten, Projektstatus auf Recovery zurücksetzen und Fortschritt ausschließlich über reproduzierbare Evidenz qualifizieren.

### Hinzugefügt
- [MVPContentManifest.md](docs/production/MVPContentManifest.md) als
  menschlich lesbare MS-1-Inhaltsgrenze.
- Substantive Technikverträge
  [SimulationCore.md](docs/tech/SimulationCore.md),
  [Commands.md](docs/tech/Commands.md),
  [FogOfWar.md](docs/tech/FogOfWar.md) und
  [CameraSystem.md](docs/tech/CameraSystem.md).
- Maschinenlesbare Verträge
  [`quality/content/mvp-v1.json`](quality/content/mvp-v1.json),
  [`quality/scenarios/mvp-v1.json`](quality/scenarios/mvp-v1.json) und
  [`quality/schemas/GateEvidence.schema.json`](quality/schemas/GateEvidence.schema.json);
  keine Evidence-Platzhalter.
- [`quality/scripts/validate_gate_evidence.py`](quality/scripts/validate_gate_evidence.py)
  für Cross-Field-, Subject-Blob-, Rohsample-/Schwellen-, Gate-Ketten-,
  Artefakt-, Reviewer-, Kriterien- und Gate-Profil-Prüfung mit generierten
  Negativkontrollen.
- Gepinnte Draft-2020-12-Prüfung über
  [`quality/scripts/validate_evidence_schema.mjs`](quality/scripts/validate_evidence_schema.mjs),
  [`quality/package.json`](quality/package.json) und
  [`quality/package-lock.json`](quality/package-lock.json).
- Fail-closed `E_AUTHORIZATION_BOOTSTRAP`-Sperre für jeden
  Schema-1.2-Pass-Versuch sowie R-18 für selbstautorisierende Prüftools und
  ungebundene Messumgebungen.
- **Sprint 7 (Implementierung / MS-0 Phase-0-Spike Kern-Simulation):**
  - **Assembly-Topologie & Engine-Entkopplung (`noEngineReferences: true`):** `Assets/_Project/Scripts/Core/Nova.Core.asmdef`, `Assets/_Project/Scripts/Simulation/Nova.Simulation.asmdef`, `Assets/_Project/Scripts/AI/Nova.AI.asmdef`.
  - **Core Simulation Types (`Nova.Core`):** `EntityId` (versioniertes Handle-Struct), `Tick` (Lockstep-Zähler), `INovaLogger` & `NullNovaLogger`, `SimRandom` (bit-genauer XorShift128+ PRNG).
  - **Simulations-Kernel (`Nova.Simulation`):** `CommandType`, `CommandEnvelope` (boxfreier Transport), `ICommandSink`, `ISimSystem`, `SimulationKernel` (Lockstep-Tick-Engine).
  - **Flow-Field Pathfinding (`Nova.Simulation.Pathfinding`):** `GridPos2D`, `Direction2D`, `CostField` (Kosten-Grid), `IntegrationField` (allokationsfreie Dijkstra-Welle), `FlowField` (8-Wege-Vektor-Feld), `PathfindingSystem`.
  - **Entitätsverwaltung & Bewegungs-System (`Nova.Simulation.State` & `Movement`):** `Transform2D`, `UnitState`, `EntityManager` (vorallokiertes Speicher-Array mit Index-Free-List-Recycling für 0-GC-Spawns), `MovementSystem` ($O(N)$ Spatial-Grid-Binning für flüssige Gruppen-Bewegung mit Sub-Millisekunden-Performanz).
  - **Unity-Gameplay-Brücke (`Nova.Gameplay`):** `MatchRunner` (MonoBehaviour 20-Hz-Akkumulator), `UnitViewManager` (60-FPS-View-Interpolation), `PathfindingTestBootstrap` (500 Einheiten Test-Runner).
  - **GameDatabase Sharding & Master Index (`Nova.Data` & `Nova.Editor`):** Category Sub-Registries (`UnitRegistrySO`, `BuildingRegistrySO`, `WeaponRegistrySO`), Aggregator `GameDatabaseMasterSO`, Editor Generator `GameDatabaseGenerator.cs` (Rebuild & Validierung) sowie Unity-freie `UnitDefinition` Structs für das Match-Setup gemäß D-049.
  - **Command Bus & Order System (`Nova.Simulation.Commands`):** Unboxed Command Transport via `CommandEnvelope`, `CommandProcessorSystem` (`ISimSystem` für `Move`, `Stop`, `AttackTarget`).
  - **Combat & Damage Pipeline (`Nova.Simulation.Combat`):** `WeaponDefinition`, `CombatSystem` (`ISimSystem` für Entfernungsprüfungen, Waffenfrequenzen, Schadensberechnungen und Entitäts-Zerstörung).
  - **State-Hash-/Replay-/Debug-Prototypen (`Nova.Simulation.State`, `Nova.Simulation.Replays`, `Nova.Presentation`):** unvollständiger FNV-1a-Hash, `ReplayBuffer` nur zur Aufzeichnung ohne Playback sowie `FlowFieldDebugView` (Scene View Gizmos); nicht als Lockstep-/Desync-Nachweis abgenommen.
  - **Wirtschafts- & Ressourcen-System (`Nova.Simulation.Economy`):** Phase 1 (Modul 9) - `PlayerEconomyState` Struct (16 Bytes, Aetherium-Guthaben & Energieraster), `ResourceHarvestingSystem` (Sammler-Entladung an Raffinerien) und `EnergyGridSystem` (Low-Power-Erkennung & -50 % Produktions-Strafen).
  - **Basisbau- & Bauplatz-System (`Nova.Simulation.Construction`):** Phase 1 (Modul 10) - `BuildingDefinition` Struct, `ConstructionGrid` (Zellbelegungs- und Bauzonenraster) und `ConstructionSystem` (`ISimSystem` für Gebäudeplatzierung, Bauzeit-Timer und automatische Energienetz-Registrierung bei Fertigstellung).
  - **Einheiten-Produktion & Tech-Tree (`Nova.Simulation.Production`):** Phase 1 (Modul 11) - `ProductionQueueSystem` (`ISimSystem` für Kasernen-/Fabrik-Queues, Bau-Timer & automatisches Spawnen im `EntityManager`) und `ResearchTreeSystem` (Tech-Tier-Freischaltungen [Tier 1, Tier 2] pro Spieler).
  - **Fog of War & Sichtweiten-Grid (`Nova.Simulation.Vision`):** Phase 1 (Modul 12) - `VisionGrid` (Verwaltet 3 diskrete Sichtzustände: `Unexplored`, `Explored`, `Visible` pro Spieler) und `VisionSystem` (`ISimSystem` für periodische Sichtweiten-Aktualisierung um Einheiten und Gebäude).
  - **Skirmish-KI Allianz & Legion (`Nova.AI`):** Phase 1 (Modul 13) - `AiFactionProfile` Struct (Prioritätsgewichtungen) und `SkirmishAiSystem` (`ISimSystem` in `Nova.AI` mit `noEngineReferences: true` für nutzenbasierte KI-Entscheidungen bzgl. Kraftwerksbau, Produktionsauslösung und Truppenbewegung).
  - **RTS-UI & Command-Card (`Nova.Presentation.UI`):** Phase 1 (Modul 14) - `SelectionManager` (Rechtecks-Kollisionsprüfungen für Drag-Box-Mehrfachauswahlen), `CommandCardPresenter` (Koppelung ausgewählter Einheiten an HUD-Buttons) und `MinimapRenderer` (Welt-zu-Minimap-Transformation).
  - **Asset-Integration MS-1 (`Nova.Data`):** Phase 1 (Modul 15) - `AssetMappingRegistrySO` (ScriptableObject-Mapping für 27 Einheiten- & 24 Gebäude-Assets aus Sprint 5 Audit) & GameDatabase-Lookup-Pipeline.
  - **3. Fraktion: Die Evolvierten (`Nova.Simulation.Factions`):** Phase 2 (Modul 16) - `BiomassGrid` (Verwaltet organische Biomasse-Zellen) und `EvolvedFactionSystem` (`ISimSystem` für passive Einheiten-Lebenspunkte-Regeneration [+2 HP / 0,5s] auf Biomasse).
  - **Commander- & Doktrinen-System (`Nova.Simulation.Commanders`):** Phase 2 (Modul 17) - `CommanderAbilityDefinition` Struct (Fähigkeiten-Parameter) und `CommanderSystem` (`ISimSystem` für passiven Energieaufbau [+1 Energy / 1,0s], Cooldowns & Bereichs-Effekte wie Orbital-Schläge).
  - **Command-Relay-Scaffolding (`Nova.Networking`):** Phase-2-Prototyp aus `CommandEnvelopeNetPacket` und In-Memory-`LockstepRelayBuffer`; aktuelle Serialisierung liefert 34 Bytes, während Test und Spezifikation 41 beziehungsweise 37 Bytes erwarten; kein UDP-Transport.
  - **Map- & Biom-Erweiterung (`Nova.Presentation.Maps`):** Phase 2 (Modul 19) - `MapBiomeType` Enum (`Desert`, `Snow`, `JungleIndustrial`) und `MapDefinitionSO` (ScriptableObject-Layouts für 1v1 / 2v2 Karten mit 2–4 Spawn-Punkten & Aetherium-Knoten).
  - **Headless SimRunner & Tests:** Standalone .NET 8 Konsolen-Executable `tools/Nova.SimRunner`, NUnit-EditMode-Testsuiten (`DeterministicSimTests`, `FlowFieldPathfindingTests`, `MovementSystemTests`, `MovementPerformanceTests`, `MatchRunnerTests`, `GameDatabaseTests`, `CommandSystemTests`, `CombatSystemTests`, `LockstepReplayTests`, `EconomySystemTests`, `ConstructionSystemTests`, `ProductionSystemTests`, `VisionSystemTests`, `SkirmishAiTests`, `SelectionManagerTests`, `AssetIntegrationTests`, `EvolvedFactionTests`, `CommanderSystemTests`, `LockstepRelayBufferTests`, `MapDefinitionTests`).
  - **Historische, nicht freigegebene Modulspezifikationen:** `MovementSystem_Spec.md`, `GameplayBridge_Spec.md`, `GameDatabase_Spec.md`, `CommandSystem_Spec.md`, `CombatSystem_Spec.md`, `LockstepReplay_Spec.md`, `EconomySystem_Spec.md`, `ConstructionSystem_Spec.md`, `ProductionSystem_Spec.md`, `VisionSystem_Spec.md`, `SkirmishAi_Spec.md`, `RtsUi_Spec.md`, `AssetIntegration_Spec.md`, `EvolvedFaction_Spec.md`, `CommanderSystem_Spec.md`, `LockstepRelay_Spec.md` und `MapExpansion_Spec.md` unter `docs/tech/modules/`; ihr Inhalt ist forensisch, aktive Verträge führen.

### Behoben

- `HEADLESS_VALID_MATCH` weist G3 nun ausdrücklich als Nutzer aus und stimmt
  damit mit dem verpflichtenden G3-Gate-Profil überein.
- Evidence kann überschrittene Szenarioschwellen, Working-Tree-Digests oder
  isolierte spätere Gates nicht mehr als `pass` akzeptieren.
- No-op-Commands, falsche Units, negative/unterzählige Performance-Samples,
  schemawidrige Vorgänger und lokale Pass-Dateien ohne externen Trust-Kontext
  werden fail-closed abgelehnt.
- Punkt- und Performance-Metrikartefakte im Recovery-Plan eindeutig auf
  `samples` beziehungsweise `measurement` getrennt.
- Aktiven Sprint-7-Scope in G0-A Trusted-Gate-Bootstrap und G0-B
  Plattformbasis geteilt; erst ein nachfolgender sauberer Subject-Commit
  darf G0 belegen.
- Valide Evidence mit `verdict=fail` wird als `VALID NON-PASS EVIDENCE`
  mit Exitcode ungleich null ausgegeben und kann nicht mehr als
  `AUTHORIZED PASS` erscheinen.

## [0.7.0] – 2026-07-24 · Sprint 6: Produktionsplanung

### Hinzugefügt
- **Produktionsdokumentation in `docs/production/`:**
  [Milestones.md](docs/production/Milestones.md) (Meilensteine MS-0 bis MS-4 mit Qualitäts-Gates und Feature-Matrix) und [Roadmap.md](docs/production/Roadmap.md) (Produktions-Roadmap über 445 Personentage Gesamtaufwand, Phasenplan 2026–2028, Adressierung R-16 & R-13).
- **Sprint-6-Abschlussbericht** [Sprint06_Report.md](docs/production/sprints/Sprint06_Report.md) mit Freigabe von **Sprint 7 (Implementierung)**.

### Geändert
- `RiskAnalysis.md` (1.6.0): **R-16 (Zeit-/Kapazitätsmodell)** auf „mitigiert" gesenkt.
- `OpenQuestions.md` (1.8.0): **Q-018 (Preispunkt 29,99–39,99 €)** und **Q-019 (Opt-in Telemetrie)** geschlossen.
- `SprintPlanning.md` (1.6.0): Sprint 6 **abgeschlossen**, Sprint 7 (Implementierung) **bereit (GO)**.
- `docs/README.md` (0.7.0) und Root-`README.md` (Status-Board, Wiki-Version 0.7.0) nachgezogen.

## [0.6.0] – 2026-07-22 · Sprint 5: Asset Audit

### Hinzugefügt
- **Neuer Wiki-Bereich `docs/assets/` (Asset Audit)** mit vier Dokumenten:
  [ProcurementStrategy.md](docs/assets/ProcurementStrategy.md) (Beschaffungsstrategie B,
  BUY/MODIFY/BUILD-Rubrik, 4 Bewertungsdimensionen), [AssetRegister.md](docs/assets/AssetRegister.md)
  (Master-Register über 14 Kategorien mit kanonischen GDD-Zahlen, Lizenz, Kosten-/Aufwands-
  schätzung, Klassifikation), [Licenses.md](docs/assets/Licenses.md) (Lizenz-Register je Quelle)
  und [BuildBacklog.md](docs/assets/BuildBacklog.md) (priorisierter Eigenbau-Backlog ~110–180 PT).
- Sprint-5-Abschlussbericht [Sprint05_Report.md](docs/production/sprints/Sprint05_Report.md).

### Entschieden
- **D-053** Asset-Beschaffungsstrategie **B (Multi-Store-Mix mit Synty als Stil-Anker)**
  ratifiziert: menschliche Fraktionen/Biome/UI-Icons/Basis-Animationen = Kauf; Aetherium,
  komplette Evolvierten-Fraktion und Fraktions-Signaturen = MODIFY/BUILD. Leitplanken:
  URP-K.O.-Kriterium, keine RTS-Komplett-Frameworks, einheitlicher URP-Material-Standard,
  Lizenz-Register-Pflicht, keine Rohdaten im öffentlichen Repo.
- **D-054** **0 € Open-Source & KI-Asset-Pipeline (Inhaberentscheidung):** Ratifizierung einer
  reinen 0 € Open-Source-Beschaffung auf Basis freier CC0-Quellen (Quaternius, Kenney, Sonniss Audio),
  KI-3D/Textur-Generierung (Hunyuan3D, Meshy, Tripo, SD, Blender AI Addons / MCP Server) und
  Community-Kitbashing. **Q-035 (Asset-Budget-Obergrenze)** auf 0 € geschlossen. Alle Assets sind
  für das **öffentliche GitHub-Repository** freigegeben.

### Geändert
- `SprintPlanning.md` (1.4.0): Sprint 5 **abgeschlossen**, Sprint 6 (Produktionsplanung) **GO**;
  `docs/README.md` (0.6.0) und Root-`README.md` (Status-Board, Struktur, Version 0.6.0) nachgezogen.
- **Kanonische Asset-Zahlen gegen die historische `RTS_Asset_Pipeline.md` abgeglichen**
  (Gebäude 36 statt 54 = D-008, Karten 12 statt 10 = D-017, Elite 3→9 statt 15 = D-015,
  Neutrale ohne Händler = D-016, Marine gestrichen = D-013); nicht-destruktiver Korrekturhinweis
  an der Spitze der APL verweist auf das AssetRegister als führende Quelle.
- `RiskAnalysis.md` (1.5.0): **R-04** (visuelle Inkohärenz) und **R-07** (Lizenz-/Kostenfallen)
  auf „mitigiert" gesenkt.

### Behoben
- Root-`README.md` von veraltetem Stand (Version 0.4.0, „Sprint 4 in Arbeit", Status-Board
  „blockiert bis Sprint 3") auf den aktuellen Stand (0.6.0, Sprint 5 abgeschlossen) korrigiert.

## [0.5.0] – 2026-07-21 · Sprint 4: Architecture Review + Governance

### Hinzugefügt
- **Team-/Beitrags-Governance:** `CONTRIBUTING.md` (Team-Ablauf, PR-Pflicht, Release-Flow),
  PR-Vorlage und `CODEOWNERS` sowie ein günstiger, abhängigkeitsfreier CI-Check
  (`docs-check`, GitHub Actions) für tote interne Doku-Links.
- **Sprint 4 – Architecture Review abgeschlossen:** sechs adversariale Review-Berichte unter
  `docs/tech/review/` (Performance, Wartbarkeit & Prozess, Architektur-Kohärenz & Korrektheit,
  Multiplayer & Netcode, Skalierung & Systemgrenzen, GDD↔TDD-Konsistenz; im Wiki-Index verlinkt)
  und der Abschlussbericht [Sprint04_Report.md](docs/production/sprints/Sprint04_Report.md).

### Geändert
- **Repository auf öffentlich umgestellt**, Community-Projekt der Organisation `VibecodingGermany`.
- **`main` ist geschützt – Änderungen nur noch über Pull Requests** (Branch Protection:
  Review + grüne CI, keine direkten Pushes). `AGENTS.md` auf 2.0.0 (PR-only).
- **Sprint-4-Findings in 22 GDD-/TDD-Dokumente eingearbeitet** (Auflösung der Review-Widersprüche):
  Angriffsreichweiten metrisch → Grid-Felder (D-047, 1 Tile = 1 m); Weapons.md/Buildings.md/
  Vehicles.md je einzige führende Wertequelle; Alpha-Mutant-Doppeldefinition aufgelöst;
  **Assembly-Topologie kanonisiert (D-043) inkl. `ModuleOverview.md` vollständig nachgezogen**
  (KI als eigene Unity-freie Assembly `Nova.AI`); Managed-first (D-045); globaler
  600-Einheiten-Deckel (D-048); GameDatabase-Sharding (D-049); Post-Match-Re-Simulation als
  MP-Trust-Anchor (D-046); Quantum-Fallback gestrichen (D-051). `DocumentationStandard.md`
  1.1.0: Grundprinzip „Single Source of Truth für Werte" (D-047).
- **Risikoregister ehrlicher (RiskAnalysis 1.4.0):** neue reale Projektrisiken R-13 Bus-Faktor
  (W=hoch), R-14 ARM↔x86-Determinismus, R-15 KI-Code-Desync, R-16 Zeit-/Kapazität (W=hoch).

### Entschieden
- **D-043–D-052** (Sprint-4-Architecture-Review-Auflösungen, DecisionLog → 1.6.1): Assembly-
  Topologie (D-043), gestuftes Sim-Tick-Modell + Pflicht-Gate V5 (D-044), Managed-first (D-045),
  MP-Trust-Anchor (D-046), Werte-Single-Source (D-047), Skalierungs-Deckel (D-048), CI-Realismus
  + DB-Sharding (D-049), gestuftes Branching (D-050), Quantum-Fallback gestrichen (D-051),
  Referenzhardware (D-052).

## [0.4.0] – 2026-07-21 · Sprint 3: Technical Design

### Hinzugefügt
- Vollständiges Technical Design (23 Dokumente) unter `docs/tech/`: Architektur-Kern
  (Architecture, ModuleOverview, DependencyGraph, FolderStructure, CodingGuidelines,
  NamingConvention), Simulation & Daten (GameState, Serialization, Savegames),
  Multiplayer (Networking, Replication), Gameplay-Systeme (Pathfinding, AIArchitecture),
  Präsentation (Rendering, Lighting, AnimationSystem, InputSystem, AudioArchitecture),
  Budgets & Betrieb (PerformanceBudget, MemoryBudget, AssetBudget, Testing, Deployment).
- Sprint-3-Abschlussbericht ([docs/production/sprints/Sprint03_Report.md](docs/production/sprints/Sprint03_Report.md)).
- Repository-Grundgerüst: Root-`README.md`, `AGENTS.md` (Arbeitsregeln für KI-Agenten),
  `CHANGELOG.md`, `.gitignore` (macOS + Unity-vorbereitet); initiale Spiegelung zu GitHub.

### Entschieden
- 10 Architektur-Entscheidungen (D-033–D-042): determinismus-fähige Command-Simulation
  mit Lockstep-Relay-Zielbild (Q-013), Flow-Field-Pathfinding (Q-014), OOP+Burst statt
  DOTS (Q-015), Nova.SimRunner (Q-020), Burst/Managed-Doppelstruktur, Disconnect-Regel,
  Audio-Backend (FMOD ab Alpha), Forward/Realtime-Licht, Sentry, Sim-Tick-Budget ≤8 ms.

### Geändert
- Detail-Angleichungen GDD↔TDD (Disconnect-Regel final, Sim-Tick-Budget) in
  VictoryConditions, MultiplayerModes, PerformanceBudget, Networking.
- AGENTS.md Regel 1: Push nach jedem Versionsbump dauerhaft freigegeben (Anordnung
  Projektinhaber).

## [0.3.0] – 2026-07-21 · Sprint 2: Game Design

### Hinzugefügt
- Vollständiges Game Design Document (25 Dokumente): Vision, USP, Zielgruppe,
  CoreGameplay, GameLoop sowie das komplette GDD (Fraktionen, Gebäude, Einheiten,
  Wirtschaft, Forschung, Kampf-/Schadens-/Rüstungssystem, Karten, Biome, neutrale
  Einheiten, Fog of War, Commander-System, Multiplayer-Modi, Siegbedingungen,
  Balancing, Kampagne).
- Sprint-2-Abschlussbericht ([docs/production/sprints/Sprint02_Report.md](docs/production/sprints/Sprint02_Report.md)).

### Entschieden
- 26 Entscheidungen (D-007–D-032): Geschäftsmodell (Premium, Singleplayer-first),
  12 Gebäudetypen, Aetherium-Hybridwirtschaft, gezielte Zerstörbarkeit, Capture-System,
  Superwaffen-Limit, Fraktions-Sonderregeln, Kampagnen-Struktur u. a.

### Geändert
- Scope reduziert und beziffert (36 statt 54 Gebäude-Assets, 9 statt 15 Elite-Einheiten;
  Marine-/Drohnen-Inflation gestrichen) – Risiko R-01 teilentschärft.

## [0.2.0] – 2026-07-21 · Sprint 1: Research

### Hinzugefügt
- 10 Research-Dokumente unter `docs/research/`: RTS-Markt/Wettbewerb,
  Multiplayer-Simulation, Unity ECS/DOTS, Pathfinding, Fog of War, Open-Source-RTS-
  Architekturen, Unity Best Practices, KI-Architektur, Animation/Audio/UI,
  Asset-Store-Landschaft – jeweils mit ≥3 verglichenen Alternativen als
  Entscheidungsvorlagen.
- Sprint-1-Abschlussbericht.

## [0.1.0] – 2026-07-21 · Sprint 0: Projektinitialisierung

### Hinzugefügt
- Wiki-Grundgerüst und verbindlicher [Dokumentationsstandard](docs/meta/DocumentationStandard.md).
- Analyse-Dokumente: Wissensbasis, Inkonsistenz-Analyse, Gap-Analyse, Prioritätenliste.
- Produktions-Basis: Sprint-Planung, DecisionLog, OpenQuestions, RiskAnalysis.
- Übernahme der historischen Quelldokumente (`RTS_Game_Design_Outline.md`,
  `RTS_Technisches_Planungsdokument.md`, `RTS_Asset_Pipeline.md`).

[Unreleased]: https://github.com/VibecodingGermany/Project_Nova/compare/v0.4.0...HEAD
[0.7.0]: https://github.com/VibecodingGermany/Project_Nova/commit/0baa304
[0.6.0]: https://github.com/VibecodingGermany/Project_Nova/commit/af30ccd
[0.5.0]: https://github.com/VibecodingGermany/Project_Nova/commit/b125229
[0.4.0]: https://github.com/VibecodingGermany/Project_Nova/releases/tag/v0.4.0
[0.3.0]: https://github.com/VibecodingGermany/Project_Nova/commit/2d2d021
[0.2.0]: https://github.com/VibecodingGermany/Project_Nova/commit/2d2d021
[0.1.0]: https://github.com/VibecodingGermany/Project_Nova/commit/2d2d021
