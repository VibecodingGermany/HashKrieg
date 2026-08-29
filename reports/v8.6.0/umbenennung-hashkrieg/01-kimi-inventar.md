# kimi-agent report

- when:    2026-08-29T07:42:57Z
- backend: cc
- model:   k3[1m]
- mode:    ro
- dir:     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova
- run:     /Users/denniswestermann/.agent-runs/20260829-094257-30361

## Task

CONSTRAINT: read-only run. Do not create, edit, or delete any file, and do not run state-changing commands. Report findings only.

Du arbeitest an einem Unity-RTS-Repo. **Dieser Lauf ist reines Lesen. Aendere
keine einzige Datei.** Dein Ergebnis ist ein Bericht, sonst nichts.

## Worum es geht

Issue #14, offen seit dem 26.07.2026: "Umbenennung Project Nova → Hashkrieg im
Bestand vollziehen". Das Projekt hiess urspruenglich "Project Nova", heisst
inzwischen "HashKrieg" (das GitHub-Repo wurde am 09.08.2026 umbenannt), und im
Bestand steht der alte Name noch an sehr vielen Stellen — Namensraeume,
Assembly-Namen, `.csproj`-Dateien, `ProjectSettings`, Dokumentation, Kommentare,
Testnamen, Ordnernamen.

Die Umbenennung ist bisher nicht angefasst worden, weil niemand weiss, wie gross
sie ist und wo sie gefaehrlich wird. **Genau das sollst du beantworten.** Du hast
ein sehr grosses Kontextfenster; nutze es und sieh dir das ganze Repo an, statt
zu stichproben.

## Was ein brauchbares Ergebnis von einem unbrauchbaren unterscheidet

Ein `grep -c` ueber "Nova" ist wertlos — die Zahl ist gross und sagt nichts.
Wertvoll ist die **Einteilung nach Risiko**. Sortiere jede Fundstelle in eine
dieser Klassen und begruende die Einteilung:

**Klasse A — bricht den Determinismus oder das Speicherformat, wenn man sie
anfasst.** Alles, was in einen Hash, eine Serialisierung, einen Fingerprint
oder einen Netzwerk-Handshake eingeht. Ein Namensraum, dessen voll
qualifizierter Typname in einen Snapshot oder in `MatchFingerprint` einfliesst,
gehoert hierher. Pruefe das wirklich nach — sieh dir an, ob irgendwo
`typeof(...).FullName`, `nameof`, `GetType().Name` oder ein Assembly-Name in
gehashte oder serialisierte Daten wandert. Das ist die wichtigste Frage des
ganzen Auftrags: **kostet die Umbenennung eine Regelrevision oder nicht?**

**Klasse B — bricht den Bau, wenn man sie unvollstaendig anfasst.** Assembly
Definitions (`.asmdef`), `.csproj`-Dateien, `Project Nova.slnx`,
`ProjectSettings/ProjectSettings.asset`, Pfade in `.github/workflows/`, die
`Compile Include`-Pfade in `tools/Nova.SimRunner*`. Hier gilt: alles oder
nichts, halbe Umbenennung = kaputter Bau.

**Klasse C — Unity-Metadaten.** `.meta`-GUIDs, Szenen- und Prefab-Referenzen,
Ordnernamen unter `Assets/`. Was passiert mit Referenzen, wenn ein Ordner
umbenannt wird? Wo liegt die Grenze zwischen "Unity zieht es selbst nach" und
"die Referenz reisst"?

**Klasse D — nur Text.** Dokumentation, Kommentare, Changelog-Historie,
Sprintdateien, Lizenztexte. Ungefaehrlich, aber grossflaechig.

**Klasse E — darf NICHT umbenannt werden.** Historische Eintraege
(CHANGELOG-Vergangenheit, Entscheidungslog, alte Sprintberichte, Lizenz- und
Copyright-Zeilen, Git-Historie), und alles, wo "Project Nova" der historisch
richtige Name ist. Diese Klasse zu benennen ist genauso wichtig wie die
anderen — eine Umbenennung, die die Vergangenheit umschreibt, macht die
Dokumentation unbrauchbar.

Nenne pro Klasse die **Anzahl** und die **konkreten Dateien** (bei Klasse D
reicht eine Zusammenfassung nach Verzeichnis mit Trefferzahl; bei A, B, C
brauche ich jede Datei einzeln mit `Datei:Zeile`).

## Ausserdem zu beantworten

1. **Wie viele Namen sind eigentlich im Spiel?** Nicht nur "Project Nova" und
   "Nova". Auch `Aetherium` steht laut Issue #15 im Verdacht, aus dem alten
   Setting zu stammen. Trenne sauber: was ist Projektname (Nova → HashKrieg),
   was ist Weltfiktion (Aetherium, Glutrinne, Alliance, Legion), und was davon
   steht in #14 ueberhaupt zur Debatte. **#15 ist NICHT dein Auftrag** — aber
   sag, ob die beiden sich beruehren, denn wenn ja, waere es teuer, sie
   getrennt zu machen.
2. **Ein gestufter Ausfuehrungsplan.** In welcher Reihenfolge, in wie vielen
   PRs, und wo genau ist der Punkt, an dem eine Inhaberentscheidung noetig ist
   (Assembly-Namen? Namensraeume? Ordnerstruktur unter `Assets/_Project/`?).
   Jede Stufe: was sie umfasst, wie man sie beweist (welcher Test, welcher
   Bau), und was schiefgehen kann.
3. **Der ehrliche Aufwand.** Nicht in Stunden — in "wie viele Dateien, wie viel
   davon mechanisch, wie viel davon Handarbeit, und welche Stelle ist die
   einzige, die wirklich weh tut".
4. **Die Empfehlung.** Auf einen Satz: jetzt machen, gestaffelt machen, oder
   liegenlassen bis nach dem Betatest — und warum.

## Zwei Dinge, die du wissen musst

- **Es laufen parallel zwei andere Arbeiter im Repo** (Kartenlage und
  Testkette) sowie ein laufender Sprint 21. Deshalb liest du nur. Wenn dein
  Plan eine Stufe enthaelt, die mit laufender Arbeit kollidiert, sag es.
- **Das Repo heisst auf GitHub bereits `VibecodingGermany/HashKrieg`.** Der
  alte Name `Project_Nova` leitet bei GET-Aufrufen weiter, bei POST nicht.
  Sieh nach, wo im Repo noch die alte URL steht — das ist ein eigener,
  billiger und sofort machbarer Teilschritt, und er gehoert als Stufe 1 in
  deinen Plan, wenn du ihn findest.

## Grenzen

- **Nur lesen.** Kein Schreiben, kein `git`-Schreibbefehl, kein `gh`-Aufruf,
  der etwas veraendert, kein Commit, kein Push.
- Keine Subagenten.
- Wenn dir waehrend der Arbeit ein echter Fehler auffaellt, der nichts mit der
  Umbenennung zu tun hat: notier ihn am Ende unter "Nebenbefunde". Nicht
  beheben.

## Report

Markdown, wird committet. Struktur wie oben: die fuenf Klassen mit Zahlen und
Dateien, die vier Fragen, die Nebenbefunde.

Schliesse mit:

  STATUS: DONE | BLOCKED
  - Befund 1
  - Befund 2
  - Befund 3

## Output

Ich habe das gesamte Repo durchsucht (kein Sampling): 2.604 case-sensitive `Nova`-Treffer in 527 Dateien, dazu die Großbuchstaben-Konstanten (`NOVA_*`), `Project Nova` (203/118 Dateien), `ProjectNova` (42/15) und die Kleinschreib-Varianten (`nova-relay`, `novarelay`). Alle risikorelevanten Stellen habe ich einzeln geöffnet und verifiziert. Hier der Bericht.

---

# Inventar & Risikoklassifizierung: Umbenennung Project Nova → Hashkrieg (Issue #14)

**Stand des Repos bei der Untersuchung:** Branch `integration/s21-review`, Sprint 21 läuft, zwei parallele Arbeiter (Kartenlage, Testkette). **Wichtigster Vorbefund:** Die Umbenennung ist bereits Gegenstand einer Inhaberentscheidung (**E-3, 2026-08-06**, `docs/production/hashkrieg/00_Entscheidungen.md:149-167`, bestätigt in `docs/production/DecisionLog.md:1899-1901`): *„Nur die Marke wird umbenannt, Code-Identität bleibt `Nova.*`"* — Namespaces, die 17 asmdefs, `tools/Nova.SimRunner/` und `NOVA_FIXED_POINT` bleiben entscheidungsgemäß unverändert. Ein sechsstufiger Ausführungsplan existiert bereits (`docs/production/hashkrieg/05_Umbenennung.md`, Sprint 7). Beide Dokumente sind in einem Punkt **überholt**: Sie sagen den GitHub-Repo-Rename als Zukunft bzw. „bleibt `Project_Nova`" voraus — tatsächlich ist er am 09.08.2026 geschehen, ohne dass die Gate-Verträge nachgezogen wurden. Daran hängt jetzt die einzige *aktive* Inkonsistenz des Repos (s. Klasse B, Gate-Schemas).

## Gesamtzahlen (Vollzählung, `Library/` ausgenommen)

| Muster | Vorkommen | Dateien |
|---|---|---|
| `Nova` (case-sensitiv, gesamt) | 2.604 | 527 |
| davon `Assets/` | 1.199 | 291 |
| davon `tools/` (ohne `bin/obj`) | 501 | 79 |
| davon `docs/` | 596 | 119 |
| davon `quality/` | 54 | 6 |
| davon `.github/` | 29 | 7 |
| davon Root-Dateien (CHANGELOG 95, README 15, AGENTS 6, …) | 138 | 10 |
| davon `reports/` (**gitignored**, inkl. 11 im Scaffold dieses Laufs) | 87 | 14 |
| `Project Nova` (mit Leerzeichen) | 203 | 118 |
| `ProjectNova` (zusammengezogen) | 42 | 15 |
| `^namespace Nova` | 334 | 334 |
| `^using Nova` | 874 | 220 |
| `NOVA_*` (nur Großschreibung: Domänen, Magics, Defines, Env-Vars; **nicht** in den 2.604 enthalten) | ~140 live + ~60 historisch | s. Klasse A |

---

## Klasse A — Determinismus / Speicherformat / Handshake: **11 Code-/Config-Dateien + 3 Spezifikationsdateien + festzurrende Tests**

### Die Antwort auf die Kernfrage: **Nein, ein Namespace-Rename kostet keine Regelrevision. Ja, die Format-Konstanten sind tabu.**

Ich habe jede `typeof().FullName`-/`GetType().Name`-/`nameof`-/`Assembly`-Stelle geprüft (197 Treffer): **kein einziger Typ- oder Assembly-Name fließt in einen Hash, eine Serialisierung oder einen Handshake.** Der `MatchFingerprint` (`Assets/_Project/Scripts/Simulation/Replays/MatchFingerprint.cs:435-471`) hasht ausschließlich numerische Konstanten und die freien Bezeichner `"Q16_16_V1"` / `"XorShift128PlusV1"` (`MatchFingerprint.cs:100,106`) — beide nova-frei. `Serialize()` (`:364-384`) schreibt keine Namen. Alle `GetType().FullName`-Treffer sind Editor-Reflexion (`Sprint12BAuthoring.cs`), Fehlermeldungen oder Test-Assertions (`Assets/Tests/EditMode/Gameplay/CanonicalMatchSetupTests.cs:244` — Test-seitig, bei Namespace-Rename mechanisch nachzuziehen). Alle `nameof(...)` folgen einem Rename automatisch. **Namespaces und Assembly-Namen sind determinismus-neutral.**

Was dagegen *wirklich* im Hash/Format steckt — diese Zeilen dürfen ohne neue Schema-Version nie angefasst werden:

1. **`Assets/_Project/Scripts/Core/SimHashWriter.cs:27,30,33,36`** — die vier Hash-Domänen `NOVA_STATE_V1`, `NOVA_DEFINITIONS_V1`, `NOVA_FILE_V1`, `NOVA_REPLAY_CHAIN_V1`. Sie präfixen **jeden** State-Hash, Definitions-Hash, Fingerprint und Replay-Chain-Hash. Spezifiziert in `docs/tech/SimulationCore.md:119-122` (§5), `docs/tech/Serialization.md:48-49`, `docs/tech/Replication.md:78`. Änderung = alle Fingerprints ändern sich = Desync gegen jeden alten Build und jedes alte Replay. Das ist die Stelle, die eine Umbenennung zur **Schema-Revision** machen würde.
2. **Datei-Magics** (Binärformate, alt/neu inkompatibel bei Änderung):
   - `Assets/_Project/Scripts/Simulation/Snapshots/SnapshotFormat.cs:57` — `"NOVASNAP"` (Doku `:15-37`)
   - `Assets/_Project/Scripts/Simulation/Replays/ReplayFormat.cs:72` — `"NOVAPLAY"`
   - `Assets/_Project/Scripts/Networking/RelayRecordStream.cs:128` — `"NOVAREC2"` (Fehlertexte `:161,363,456,461`)
   - `Assets/_Project/Scripts/Networking/DesyncDiagnostic.cs:40` — `"NOVADIAG2"`
3. **`Assets/_Project/Scripts/Networking/LobbyToken.cs:59,62`** — HMAC-Kontexte `"NOVA-LOBBY-TOKEN-V1"` / `"NOVA-LOBBY-SEED-V1"`. Gehen in die Token-Bytes ein: Client↔Server-Interop bricht, wenn nur eine Seite umbenennt.
4. **`NOVA_FIXED_POINT`** (Build-Define mit Artefakt-Vertrag): `ProjectSettings/ProjectSettings.asset:594`; `tools/Nova.SimRunner/Nova.SimRunner.csproj:12`; `tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj:11`; `tools/Nova.RelayServer/Nova.RelayServer.csproj:12`; ausgewertet in `tools/Nova.SimRunner/DeterminismArtifacts.cs:131-132` (wird **ins Determinismus-Artefakt serialisiert**) und `tools/Nova.SimRunner/Determinism10000Scenario.cs:279`; hart zugesichert in `tools/Nova.SimRunner.Tests/Determinism10000Tests.cs:241` (`Is.EqualTo("NOVA_FIXED_POINT")`). Ratifiziert in `docs/tech/SimulationCore.md:179,228`; vom Inhaber ausdrücklich als „bleibt" entschieden (`00_Entscheidungen.md:160`).
5. **Festzurrende Tests/Baselines** (brechen bei jeder Klasse-A-Berührung rot): `Assets/Tests/EditMode/Core/SimHashWriterTests.cs:32-35`, `tools/Nova.SimRunner.Tests/SimHashWriterTests.cs:36-45`, `SnapshotGoldenBytesTests`, `CommandGoldenBytesTests`, `SimRandomGoldenTests`, `Determinism10000Tests` — letztere vier stehen unter Baseline-Schutz in `.github/scripts/check_baseline_guard.py:13-16`.

### A-2 (Ops-Vertrag, kein Hash, aber externer Vertrag): Umgebungsvariablen

Kein Determinismus-Bruch, aber jede Änderung bricht Deployments und Runbooks: `tools/Nova.RelayServer/RelayEnvironment.cs:13-20` (acht Variablen: `NOVA_MATCH_TOKEN`, `NOVA_RELAY_BIND`, `NOVA_RELAY_PORT`, `NOVA_RELAY_SLOT_COUNT`, `NOVA_INPUT_DELAY_TICKS`, `NOVA_RECORD_DIR`, `NOVA_RELAY_SEED`, `NOVA_RELAY_TOKEN_SECRET`); `Assets/_Project/Scripts/Gameplay/Match/MatchBootstrap.cs:442,813-814,818,823`; `Assets/_Project/Scripts/Gameplay/Match/LobbyConfig.cs:24-25` (`NOVA_LOBBY_URL`, `NOVA_LOBBY_ANON_KEY`); `quality/scripts/run_gate_check.py:756-757` (`NOVA_GATE_EXECUTOR`); CI-Verdrahtung `.github/workflows/relay-publish.yml:87-98,117-123`; Deployment-Vorlage `tools/Nova.RelayServer/deploy/hashkrieg-relay.env.example:2-17`; Doku-Vertrag `docs/tech/RelayServer.md` (~30 Stellen), `docs/tech/LobbySupabase.md` (~12 Stellen). Unter E-3 bleiben diese unverändert — die Deploy-Hülle heißt bereits `hashkrieg-relay` bei `NOVA_*`-Variablen und User `novarelay` (bewusstes Halbrename-Muster, s. `hashkrieg-relay.service:10-14`).

---

## Klasse B — bricht den Bau bei unvollständiger Umbenennung: **~45 getrackte Dateien + 22 untracked generierte**

### B-1 Assembly-Identität (unter E-3: entfällt; bei E-3-Revision: alles-oder-nichts in einem Commit)

- **17 `.asmdef`** mit Klartext-Referenzen (insgesamt 83 `"Nova.*"`-Strings = name + rootNamespace + references):
  `Assets/_Project/Scripts/Core/Nova.Core.asmdef`, `.../Simulation/Nova.Simulation.asmdef` (referenziert `Nova.Core`), `.../Data/Nova.Data.asmdef`, `.../Gameplay/Nova.Gameplay.asmdef`, `.../Presentation/Nova.Presentation.asmdef`, `.../Presentation/UI/Nova.Presentation.UI.asmdef`, `.../Networking/Nova.Networking.asmdef`, `.../AI/Nova.AI.asmdef`, `.../AI.Data/Nova.AI.Data.asmdef`, `Assets/_Project/Editor/Nova.Editor.asmdef`, `Assets/Tests/PlayMode/Nova.PlayMode.Tests.asmdef`, `Assets/Tests/EditMode/{AI/Nova.AI.Tests, Core/Nova.Core.Tests, Data/Nova.Data.Tests, Gameplay/Nova.Gameplay.Tests, Networking/Nova.Networking.Tests, Simulation/Nova.Simulation.Tests}.asmdef`. Ein einzelner geänderter `name` ohne Mitnahme aller `references` legt die Unity-Kompilation lahm.
- **`.github/workflows/tests.yml:48,51`** — einziger CI-Testschritt: `dotnet restore/test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj`.
- **`.github/workflows/relay-publish.yml`** — 12 Stellen: Pfad-Trigger `:9-10,19-20`, restore/test/publish `:52-57,63`, Binärname `nova-relay` `:75-77,90,171,198`, Deploy-Check `:79-80,168`, Smoke `:90-124`.
- **`tools/Nova.SimRunner/Nova.SimRunner.csproj`** (`:8` RootNamespace, `:12` Define, `:16-18` Compile-Include-Globs auf `Assets/_Project/Scripts/...`) und **`tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj`** (`:8,11,15-38` — u.a. 7 Einzel-Links auf `..\Nova.RelayServer\*` und `..\Nova.SimRunner\*`: Verzeichnis-Umbenennung unter `tools/` bricht diese Links) und **`tools/Nova.RelayServer/Nova.RelayServer.csproj`**.
- **`quality/scripts/run_gate_check.py`** — die gefährlichste Einzelstelle, weil sie schweigt: Schichtenkarte `:78-95` (inkl. **veralteter** Einträge `Nova.Presentation.Maps`/`.Shaders`, `:86-87` — diese Assemblies existieren nicht), Verbot `:194-195`, Kommentar-Vertrag `:136-141`, Negativtest legt Sandbox-`Nova.Simulation.asmdef` an `:510-537`, Build-Erwartungen `Nova.Editor.BuildScript.BuildWindows64` + `Builds/Windows64/ProjectNova.exe` / `Builds/MacOSArm64/ProjectNova.app` `:399-409`, `SIMRUNNER_CSPROJ` `:72`.
- **`Assets/_Project/Editor/BuildScript.cs:7,12-14,20-21,27,34,41`** — Firmen-/Produktname + Ausgabepfade + die `executeMethod`-Klassennamen, die `run_gate_check.py:399-408`, `tools/packaging/build-mac.sh:88`, `tools/packaging/build-linux.sh:61` und `README.md:247` wörtlich referenzieren.
- **Packaging-Closed-Loop:** `tools/packaging/build-mac.sh:38` (`Nova.entitlements` — Dateiname, existiert), `:40` (`ProjectNova.app`), `:105-107` (Info.plist-Key `NovaBuildCommit`), `:144,151,155,163` (DMG-Name/Volname); `tools/packaging/build-linux.sh:24-26,81,88` (`ProjectNova.x86_64`, `ProjectNova_Data/NovaBuildCommit.txt` — Unity leitet den `_Data`-Ordnernamen vom Binary ab; tar-Verifikation liest ihn wörtlich zurück); `tools/packaging/README.md:18-19,40-49,73` (Verifikationsanleitung).
- **NovaBuildCommit-Loop (namensadressierte Resource!):** `Assets/_Project/Editor/BuildCommitStamp.cs:29` (Pfad-Konstante) → `Assets/_Project/Scripts/Gameplay/Match/BuildInfo.cs:40` (`Resources.Load<TextAsset>("NovaBuildCommit")`) → `.gitignore:132-133` → Packaging-Skripte oben. Umbenennen nur als geschlossener Block.
- **`ProjectSettings/ProjectSettings.asset:15-16`** (`companyName`/`productName`) + `:616,623` (metro*) + `:594` (Define, s. Klasse A).
- **Quality-Verträge:** `quality/scenarios/mvp-v1.json:32` (`"qualityProfile": "NovaReference"`) + `:395` (Coverage-Schwelle `"Nova.Simulation": 80`); `tools/Nova.Coverage/coverage.py:36` (Scope-Name, diagnostisch); `quality/scripts/validate_gate_evidence.py:58` (`REPOSITORY = "VibecodingGermany/Project_Nova"`) + Selftests `:2654,3084,3104,3164,3171,3225,3269,4012,4017,4770`; **`quality/schemas/GateAuthorization.schema.json:3,56` und `quality/schemas/GateEvidence.schema.json:3,245,624`** (`$id` + dreimal `"const": "VibecodingGermany/Project_Nova"` — **seit dem Repo-Rename am 09.08. live inkonsistent**: jede Evidence muss weiter den alten Namen behaupten, um zu validieren); `quality/package.json:2` + `quality/package-lock.json:2,8` (`project-nova-quality-contracts`).
- **Sonstige CI/Repo-Mechanik:** `.github/scripts/check_baseline_guard.py:13-16,77-97` (geschützte Testpfade), `.github/pull_request_template.md:1,9`, `.github/CODEOWNERS:1`, `.github/scripts/check_docs.py:2`, `.github/ISSUE_TEMPLATE/config.yml:4,7,10` (alte URLs — Stufe 1).
- **Untracked, aber real auf Disk:** `Project Nova.slnx` (16 Projektverweise auf `Nova.*.csproj`), 20 Root-`.csproj` (Unity-regeneriert, `.gitignore:52-55`; darunter **zwei veraltete**: `Nova.Presentation.Maps.csproj`, `Nova.Presentation.Shaders.csproj` — passend zur Gate-Drift), `.vscode/settings.json:70` (`"dotnet.defaultSolution": "Project Nova.slnx"`; `.vscode/` ist gitignored, `.gitignore:17`).

### B-2 Namespace-/using-Fläche (nur relevant bei E-3-Revision)

334 Dateien mit `namespace Nova.*`-Deklaration (257 unter `Assets/`, 70 unter `tools/`, 7 weitere), 874 `using Nova`-Zeilen in 220 Dateien, plus Typnamen mit Marke (`INovaLogger`, `NullNovaLogger` — `Assets/_Project/Scripts/Core/`). Mechanisch, aber atomar; `sed` auf `Nova\.` lässt die Typen stehen, `sed` auf `Nova` nicht — muss entschieden werden (steht so auch schon in `05_Umbenennung.md:121-128`).

---

## Klasse C — Unity-Metadaten: **17 Dateien, 0 Ordner — und die gute Nachricht: fast nichts reisst**

- **Kein einziger Ordnername unter `Assets/` enthält „Nova".** Die Ordner-Frage aus dem Auftrag entfällt komplett; `tools/Nova.*` liegt außerhalb der Unity-Verwaltung.
- **`Assets/_Project/Scenes/Bootstrap.unity`** — 26× `m_EditorClassIdentifier: Nova.X::Nova.X.Typname` (Zeilen 152, 284, 301, 319, 337, 356, 379, 395, 414, 434, 451, 473, 489, 507, 526, 597, 749, 761, 823, 988, 1162, 1205, 1375, 1393, 1409, 1430). Das ist ein **reines Editor-Anzeigefeld**; die Bindung läuft über die Skript-GUID aus der `.cs.meta`. Ein Namespace-Rename bricht die Szene **nicht**; Unity schreibt die Strings beim nächsten Speichern neu (Fehlalarm-Gefahr bei der Nachkontrolle, kein Fehler).
- Gleiche Form, je 1 Treffer: `Assets/_Project/Audio/Events/SND_*.asset` (12 Dateien: SND_IMP_Kinetic, SND_IMP_Explosive, SND_WPN_Explosive, SND_WPN_Kinetic_Heavy, SND_WPN_Kinetic_Light, SND_UI_Click, SND_UI_Deny, SND_UI_Select, SND_UI_Ack, SND_PRD_UnitReady, SND_DTH_Building, SND_DTH_Unit), `Assets/_Project/Data/Maps/MAP_Glutrinne.asset:14`, `Assets/_Project/Data/Registries/AssetMappingRegistry.asset`.
- **Dateinamen:** `Assets/_Project/Settings/NovaUrp.asset` + `NovaUrpRenderer.asset` (intern `m_Name`, referenziert aus Graphics/QualitySettings per GUID → Umbenennen im Editor sicher, `.meta` wandert mit). `Assets/_Project/Resources/NovaBuildCommit.txt` ist **namensadressiert** (`Resources.Load`) — gehört in den B-1-Loop, nicht hierher.
- **Kein `[SerializeReference]` im Projekt** — keine managed-reference-Typnamen in Assets serialisiert. Enums werden numerisch serialisiert. GUIDs in `.meta` sind namensunabhängig.
- **Laufzeit-Pfade als Nebenwirkung von B-1** (`companyName`/`productName`): `Application.persistentDataPath` wandert → `GameSettings` (`Assets/_Project/Scripts/Presentation/UI/GameSettings.cs:92-94`, `settings.json`) und das Desync-Diagnostik-Verzeichnis (`Assets/_Project/Scripts/Networking/RelayMatchClient.cs:107`, `…/ProjectNova/NetworkDiagnostics`) beginnen leer neu. Kein Datenverlust-Risiko im Graybox-Stand, aber bewusst einzuplanen.

---

## Klasse D — nur Text: **~900 Vorkommen, verteilt**

- `docs/` — 596 Vorkommen / 119 Dateien. Lebendig (zu aktualisieren): u.a. `docs/tech/NamingConvention.md` (21; komplette Namespace-Tabelle), `docs/tech/Architecture.md` (16), `docs/tech/DependencyGraph.md` (20), `docs/tech/FolderStructure.md` (6), `docs/tech/modules/*.md` (~60 über ~25 Specs), `docs/tech/RelayServer.md` + `LobbySupabase.md` (Env-Vertrag, an A-2 gekoppelt), `docs/production/DemoRunbook.md` + `GrayboxLog.md` (zitieren Menüpfade wörtlich), `docs/production/hashkrieg/AUFTRAG_*.md`, `21_Sprint_*` (aktuelle Arbeitsdoku).
- Root — 138/10: `README.md` (15; inkl. `:380-393` Verzeichnisbaum „Project Nova/" und `:441` **überholte** Aussage „Repo … läuft weiter unter Project Nova"), `AGENTS.md` (6), `CONTRIBUTING.md`, `GOVERNANCE.md`, `SECURITY.md`, `CODE_OF_CONDUCT.md`, `.gitignore` (5, davon `:1` Titel-Kommentar + funktionale `:50,132-133`).
- Editor-sichtbare Strings (stehen zwischen D und B): 7× `[MenuItem("Tools/Project Nova/…")]` (`DemoLauncher.cs:17`, `GameDatabaseGenerator.cs:18`, `BootstrapSceneGenerator.cs:52`, `ArtAssetAutoSync.cs:35`, `ArtAssetPrefabBuilder.cs:38`, `UrpProjectSetup.cs:30`, `Sprint12BAuthoring.cs:93`), 7× `CreateAssetMenu(menuName=…)` (6× „Project Nova/…", 1× inkonsistent „Nova/Audio/Sound Event", `SoundEventSO.cs:21`), dazu **Test-Assertions auf diese Pfade**: `Assets/Tests/PlayMode/{VersionBadgeTests.cs:115, PauseMenuTests.cs:59, MainMenuTests.cs:103,388}` und Fehlertexte `MainMenuController.cs:179,221,709`, `VersionBadge.cs:79`, `BootstrapSceneGenerator.cs`. Anzeigestring `DebugHud.cs:192` („Nova graybox HUD").
- Kommentare in ~370 `.cs`-Dateien (der Rest der 1.199/501).

---

## Klasse E — darf NICHT umbenannt werden

- **`CHANGELOG.md`** (95 `Nova` + 8 alte URL-Linkdefs `:2844-2851`) — Versionsgeschichte; höchstens neuer `[Unreleased]`-Eintrag.
- **`docs/production/DecisionLog.md`** (31) — Entscheidungsprotokoll inkl. der Rename-Entscheidungen selbst.
- **Sprint-Historie:** `docs/production/sprints/Sprint01-04_Report.md`, `docs/production/hashkrieg/` (00_Entscheidungen, 01_Bestandsaufnahme, 05_Umbenennung, 07-21_Sprint-*, `Testberichte/`), `docs/production/StatusSnapshot_2026-08-05.md`, `docs/research/*` (~60), `docs/vision/Konzept_Hashkrieg.md:1` (Titel „…für Project Nova" — historisch korrekter Kontext), `docs/production/hashkrieg/16-19_Betatest_Einordnung.md:252` (alter Issue-Link als Zeitzeugnis).
- **Rechtstexte:** `NOTICE` (nennt bewusst **beide** Namen als geschützte Projektnamen, `:19-20` — nicht „umbenennen", nur bei Bedarf redaktionell ergänzen), `CONTRIBUTOR_LICENSE_AGREEMENT.md` (3), `LICENSE` (nova-frei). SECURITY/CODE_OF_CONDUCT-URLs sind lebende Kontaktadressen → Stufe 1, keine Historie.
- **`NOVA_FIXED_POINT`, Hash-Domänen, Magics, HMAC-Kontexte** (Klasse A) gehören faktisch auch hierher: bereits beschlossen (`00_Entscheidungen.md:160`) bzw. nur per Schema-Revision änderbar.
- **`reports/`** — gitignored, nicht Bestand.
- Git-Historie selbst (Tag-Namen, alte Commit-Links) — Redirects funktionieren; nichts umschreiben.

---

## Die vier Fragen

### 1. Wie viele Namen sind im Spiel?

**Fünf Ebenen, sauber trennbar:**
1. **Projektname (steht in #14 zur Debatte):** „Project Nova" → „Hashkrieg". Varianten im Bestand: `Nova.*` (Code-Identität, per E-3 **behalte­n**), `NOVA_*` (Vertragskonstanten, Klasse A/E), `ProjectNova` (Build-Artefakte), `nova-relay`/`novarelay` (Betrieb), „Project Nova" (Texte). Zielseite ist selbst uneinheitlich geschrieben: GitHub `HashKrieg`, Doku/Assets `Hashkrieg`, Menütitel `HASHKRIEG` — vor Stufe 1 zu fixieren.
2. **Weltfiktion (#15, nicht mein Auftrag):** `Aetherium` (Ressource; Code: `AetheriumField.cs`, `…AE`-Suffixe wie `EconomySystem.HqBaseCapacityAE`), `Glutrinne` (Karte: `MAP_Glutrinne.asset`, zwei View-Klassen), `Alliance`/`Legion` (Fraktionen). Relevante Wechselwirkung: die **Faction-Bytes** (0=Alliance, 1=Legion) stecken im Fingerprint (`MatchFingerprint.cs:68,194-209`) — aber nur als **Zahl**, nie als Name. Eine Fiktions-Umbenennung berührt keine Hashes, solange sie nur Bezeichner umbenennt.
3. **Berührung #14/#15:** Bezeichner-disjunkt (`Nova` ≠ `Aetherium`), aber **gleiche Dateien** (`EconomySystem.cs`, `SimDefinitions.cs`, Wirtschafts-Doku) und gleiche Arbeitstechnik (Repo-weiter mechanischer Rename). Getrennt ausführbar ohne technischen Mehrpreis; teuer wäre nur doppelte Doku-Berührung. Hinweis: E-4 (`docs/production/hashkrieg/README.md:104,142-154`) hat bereits entschieden „**Aetherium bleibt die Ressource**" — #15 wäre also selbst eine Entscheidungsrevision, wie jeder #14-Übergriff über die Marke hinaus eine E-3-Revision wäre.
4. **Betriebsnamen:** `hashkrieg-relay` (schon neu) vs. `nova-relay`-Binary/`novarelay`-User/`NOVA_*`-Env (alt) — bewusstes Mischbild, funktioniert.
5. **Qualitätsvertrag-Namen:** `NovaReference` (Profil), `project-nova-quality-contracts` (npm) — frei wählbar, schema-seitig nicht gepinnt.

### 2. Gestufter Ausführungsplan (5 PRs; baut auf `05_Umbenennung.md` auf, aktualisiert ihn)

- **Stufe 1 — GitHub-URLs & Gate-Repo-Konstante (sofort, 1 PR, S):** `.github/ISSUE_TEMPLATE/config.yml:4,7,10`, `README.md:99,349`, `SECURITY.md:11`, `CODE_OF_CONDUCT.md:31`; dann `quality/schemas/*.json` (`const`→Übergangs-`enum` [alt, neu] plus `$id`) und `validate_gate_evidence.py:58` + Selftests. *Beweis:* `validate_gate_evidence.py`-Selftests + CI grün. *Risiko:* ohne `enum` schlagen alte Belege fehl (bekanntes Dilemma aus Stufe 3 des alten Plans — jetzt live). *Kollision:* möglich mit dem Testketten-Arbeiter (`quality/scripts`) → abstimmen.
- **Stufe 2 — Marke & Build-Artefakte (1 PR, S-M):** `ProjectSettings.asset:15-16,616,623`, `BuildScript.cs:20-41`, `run_gate_check.py:399-409` (beide Seiten der doppelten Pfad-Führung!), Packaging-Skripte + `Nova.entitlements`-Dateiname + `packaging/README.md`, Menüpfade (7+7) samt Doku-Zitaten (`README.md:210`, `DemoRunbook.md`, `GrayboxLog.md`, `AssetPackage.md`) und den 4 Test-Assertions, `DebugHud.cs:192`, optional der `NovaBuildCommit`-Loop (B-1) als geschlossener Block. *Beweis:* Gate-Build-Pfad grün (Build erzeugt neue Ausgabe UND Gate findet sie), PlayMode-Tests grün. *Risiken:* `persistentDataPath`-Wechsel (Einstellungen/Diagnostik neu), Info.plist-Key-Änderung muss in der Verifikationsanleitung mitziehen.
- **Stufe 3 — Lebende Prosa (1-2 PRs, S, großflächig):** Root-MDs + `docs/**` lebendige Dokumente (Klasse-D-Liste oben; Klasse E aussparen). *Beweis:* `.github/scripts/check_docs.py` grün, keine toten internen Links. *Risiko:* CHANGELOG versehentlich umschreiben.
- **Stufe 4 — Code-Identität (nur nach ausdrücklicher E-3-Revision; eigener Sprint, 1 atomarer Commit, L):** 17 asmdefs (83 Strings), 334 Namespaces, 874 usings, `tools/Nova.*`-Verzeichnisse + csproj-Links + `tests.yml`/`relay-publish.yml`/`check_baseline_guard.py`-Pfade + `run_gate_check.py`-Schichtenkarte + `mvp-v1.json`-Schwelle + `coverage.py`, danach slnx/csproj-Regeneration, Root-Verzeichnis zuletzt. *Beweis:* Unity kompiliert, alle Tests (Unity + SimRunner) grün, **Determinismus-Fingerprint unverändert** (machbar — Klasse-A-Verifikation oben: Namespaces stehen nicht im Hash), drei Headless-Spuren laufen. *Risiko:* Atomarität vs. parallele Branches; die schweigende Gate-Schichtenkarte (inkl. der Maps/Shaders-Drift zuerst bereinigen). **Nicht während Sprint 21** — dieser Schritt berührt jede `.cs`-Datei und kollidiert hart mit beiden laufenden Arbeitern (deren Dateien, z.B. `Glutrinne*.cs`, `InputGateDiagnostic.cs`, selbst `Nova.*`-Namespaces deklarieren).
- **Stufe 5 — Klasse A:** nur mit neuer Schema-Version (V2) und Dual-Read/Übergang. Aktuell kein Nutzen über Kosmetik hinaus → **nicht einplanen**.
- **Inhaberentscheidung nötig an genau drei Punkten:** (a) E-3 bestätigen oder revidieren (Namespaces/Assemblies — Stufe 4 ja/nein), (b) Übergangsform der Gate-`const` (enum vs. harter Cutover — Stufe 1), (c) Ziel-Schreibweise `HashKrieg` vs. `Hashkrieg` und Betriebsnamen (`nova-relay` → ?).

### 3. Der ehrliche Aufwand

- **E-3-Umfang (Stufen 1-3):** ~60 getrackte Dateien, davon ~85 % mechanisch (URL-/String-Tausch), Handarbeit nur an vier Stellen: Schema-Übergang, Packaging-Skripte, Menüpfad-Doku-Kopplung, README-Strukturabschnitt. Kein Determinismus-Risiko.
- **Voller Code-Rename (Stufe 4):** ~460 getrackte Dateien, fast vollständig mechanisch — aber als **ein** atomarer Commit, der keinen parallelen Branch überlebt. **Die einzige Stelle, die wirklich weh tut, ist `run_gate_check.py`:** Schichtenkarte + Negativtest + Build-Erwartungen verdrahten Assembly-Namen so, dass das Gate nach einem halben Rename *grün über Assemblies lügen* kann, die es nicht mehr gibt — ein Fehler, der sich als Stille äußert, nicht als rotes Licht.
- **Klasse A bleibt in jedem Szenario liegen** — sie zu „bereinigen" wäre der einzige Schritt mit echtem Determinismus-Preis und hat null Funktionsgewinn.

### 4. Empfehlung

**Gestaffelt machen: Stufe 1 sofort (billig, behebt die seit dem 09.08. live liegende Gate-/URL-Inkonsistenz), Stufen 2-3 als eigener kleiner Sprint direkt nach Sprint 21, und die Code-Identität (Stufe 4) liegenlassen, es sei denn, der Inhaber revidiert E-3 ausdrücklich — denn der teure Rest ist unter der bestehenden Entscheidung schon entschieden, und ein atomarer 460-Dateien-Commit hat vor dem Betatest kein tragfähiges Risiko-Fenster.**

---

## Nebenbefunde (nicht Teil der Umbenennung, nicht behoben)

1. **Gate-Drift (bestätigt, immer noch aktuell):** `quality/scripts/run_gate_check.py:86-87` prüft `Nova.Presentation.Maps`/`Nova.Presentation.Shaders` — Assemblies, die nicht existieren; auf Disk liegen zudem veraltete untracked `Nova.Presentation.Maps.csproj`/`Nova.Presentation.Shaders.csproj` im Root. Das Gate läuft grün über Phantom-Assemblies.
2. **README.md:441 ist faktisch falsch:** „Repo, Code und Wiki laufen weiter unter *Project Nova*" — das GitHub-Repo heißt seit 09.08. `HashKrieg`.
3. **`05_Umbenennung.md` Stufe 3 und `00_Entscheidungen.md:159` überholt:** beide setzen den alten Repo-Namen als Zukunft bzw. Dauerzustand voraus; die Evidence-`const`-Falle ist dadurch unbewältigt live.
4. **Prozessfalle Report-Ablage:** `.gitignore:26` ignoriert `reports/` — dieser Bericht (`reports/v8.6.0/umbenennung-hashkrieg/…`) und die Sprint-21-Berichte landen ohne `git add -f` oder Ablageänderung nie im Git, obwohl der Auftrag „wird committet" sagt.
5. **Schreibweise des neuen Namens uneinheitlich** (`HashKrieg`/`Hashkrieg`/`HASHKRIEG` über GitHub, Doku, `HashkriegPanelSettings.asset`, Menütitel) — vor Stufe 1 zu fixieren.
6. **`DebugHud.cs:192`** zeigt „Nova graybox HUD" — spielersichtbarer String, kleiner Bestandsverstoß gegen E-3 („alles, was der Spieler sieht, heißt Hashkrieg"; vermutlich nur Dev-Build).
7. **`CreateAssetMenu`-Inkonsistenz:** `SoundEventSO.cs:21` nutzt „Nova/Audio/…", alle anderen „Project Nova/…" — bei Stufe 2 zu vereinheitlichen.

STATUS: DONE
- Klasse A verifiziert negativ für Namespaces (kein Typ-/Assembly-Name in Hash, Snapshot oder Fingerprint — ein Namespace-Rename kostet KEINE Regelrevision) und positiv für 6 Konstanten-Familien (4 Hash-Domänen, 4 Datei-Magics, 2 HMAC-Kontexte, NOVA_FIXED_POINT-Define, ~14 NOVA_*-Env-Variablen): Diese sind tabu bzw. Ops-Vertrag.
- Der Rename ist kleiner als befürchtet und schon halb entschieden: E-3 (2026-08-06) begrenzt ihn auf die Marke (~60 Dateien, 3 PRs); nur die Gate-Schemas sind seit dem Repo-Rename am 09.08. live inkonsistent (const "VibecodingGermany/Project_Nova" in GateEvidence/GateAuthorization + validate_gate_evidence.py:58) — das ist Stufe 1 und sofort machbar.
- Ein voller Code-Rename wäre ein atomarer Commit über ~460 Dateien (334 Namespaces, 874 usings, 17 asmdefs mit Klartext-Referenzen, dazu CI/Gate/Packaging-Verdrahtung); schmerzhafteste Stelle ist die schweigende Assembly-Karte in run_gate_check.py; während Sprint 21 und der zwei parallelen Arbeiter nicht durchführbar.

## Exit

exit=0

## Progress

    [00:17] · Ich beginne mit der Bestandsaufnahme: Repo-Struktur, dann systematische Suchen nach allen Namensvarianten.
    [00:17] Grep     Nova
    [00:17] Grep     Aetherium|Glutrinne|HashKrieg|Hashkrieg|HASHKRIEG
    [00:17] Glob     *.sln*
    [00:17] Glob     **/*.asmdef
    [00:27] Glob     Assets/**/*.asmdef
    [00:27] Glob     tools/**/*.csproj
    [00:27] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/reports/v8.6.0/umbenennung-hashk…
    [00:27] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Project Nova.slnx
    [00:38] Grep     Assets
    [00:38] Grep     tools
    [00:38] Grep     Assets
    [00:49] · Jetzt lese ich die vollständigen Trefferlisten und suche gezielt nach dem Fingerprint-/Hash-Code.
    [00:50] Read     /Users/denniswestermann/.claude-kimi-bridge/projects/-Volumes-2TB-CodingProjekte-Coding-P…
    [00:50] Grep     Assets/_Project/Scripts
    [00:59] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Simulati…
    [00:59] Grep     Assets/_Project/Scripts
    [01:21] Read     /Users/denniswestermann/.claude-kimi-bridge/projects/-Volumes-2TB-CodingProjekte-Coding-P…
    [01:22] Grep     github\.com[/:][A-Za-z0-9_-]*/[A-Za-z0-9_.-]*
    [01:51] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Core/Sim…
    [01:51] Grep     ProjectSettings/ProjectSettings.asset
    [01:51] Grep     Assets
    [01:51] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/docs/production/hashkrieg/05_Umb…
    [02:07] Read     /Users/denniswestermann/.claude-kimi-bridge/projects/-Volumes-2TB-CodingProjekte-Coding-P…
    [02:27] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Core/Nov…
    [02:27] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Simulati…
    [02:27] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/tools/Nova.SimRunner/Nova.SimRun…
    [02:27] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/tools/Nova.SimRunner.Tests/Nova.…
    [02:40] Grep     .github
    [02:40] Grep     quality
    [03:04] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Editor/BuildScri…
    [03:07] Grep     Assets/_Project/Scripts/Presentation/UI/GameSettings.cs
    [03:07] Grep     NOVA_FIXED_POINT
    [03:07] Grep     Assets
    [03:29] Grep     Assets/_Project/Scenes/Bootstrap.unity
    [03:29] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/.gitignore
    [03:29] Glob     .vscode/*
    [03:29] Glob     *.csproj
    [03:53] Grep     NOVA_[A-Z_]+
    [03:53] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Editor/BuildComm…
    [04:10] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Scripts/Gameplay…
    [04:10] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/tools/Nova.RelayServer/RelayEnvi…
    [04:10] Grep     tools/packaging
    [04:33] Glob     tools/packaging/*
    [04:33] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/tools/Nova.Coverage/coverage.py
    [04:33] Grep     README.md
    [04:33] Grep     Assets/_Project/Scripts/Simulation/Economy/AetheriumField.cs
    [04:53] Grep     Assets/_Project/Scripts/Simulation/Definitions/SimDefinitions.cs
    [04:53] Grep     Project Nova
    [04:54] Grep     ProjectNova
    [04:54] Grep     Assets/_Project/UI/HashkriegPanelSettings.asset
    [05:13] Grep     Assets/_Project/Scripts/Networking/RelayMatchClient.cs
    [05:14] Grep     Assets/_Project/Scripts/Presentation/UI/MainMenuController.cs
    [05:14] Grep     Assets/_Project/Scripts/Presentation/UI/VersionBadge.cs
    [05:14] Grep     Assets/_Project/Audio/Events/SND_UI_Click.asset
    [05:14] Grep     Assets/_Project/Data/Maps/MAP_Glutrinne.asset
    [05:30] Grep     ProjectSettings/ProjectSettings.asset
    [05:33] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/.vscode/settings.json
    [05:33] Grep     docs/tech/SimulationCore.md
    [05:33] Grep     docs/tech/NamingConvention.md
    [05:56] Glob     Assets/_Project/Settings/*
    [05:56] Glob     tools/Nova.RelayServer/**/*
    [05:56] Grep     quality/schemas
    [05:56] Grep     Assets/_Project/Scripts/Presentation/UI/MainMenuController.cs
    [06:11] Grep     docs/production/hashkrieg/README.md
    [06:11] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/quality/schemas/GateEvidence.sch…
    [06:22] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/docs/production/hashkrieg/README…
    [06:23] Grep     docs/production/DecisionLog.md
    [06:38] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/docs/production/DecisionLog.md
    [06:38] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/docs/production/hashkrieg/00_Ent…
    [07:06] Grep     Assets/_Project/Scripts/Presentation/UI/DebugHud.cs
    [07:06] Grep     Assets/Tests
    [07:06] Grep     tools/art
    [07:06] Grep     docs/tech/FolderStructure.md
    [07:34] · Fast vollständig. Letzte Lücken: Nova-Ordnernamen unter Assets, deploy-Verzeichnis, lobby-config-Template, Konzept-Status.
    [07:34] Glob     Assets/**/Nova*
    [07:34] Glob     tools/Nova.RelayServer/deploy/*
    [07:34] Glob     Assets/_Project/Resources/*
    [07:34] Grep     docs/vision/Konzept_Hashkrieg.md
    [07:53] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/tools/Nova.RelayServer/deploy/ha…
    [07:53] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/Assets/_Project/Resources/lobby-…
    [07:53] Grep     tools/Nova.RelayServer/deploy/deploy.sh
    [08:51] · Letzte Verifikationsrunde: NOTICE/Lizenz, Verzeichnis-Aggregate, NovaUrp-Assets.
    [08:51] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova/NOTICE
    [08:51] Glob     LICENSE*
    [08:51] Grep     Assets
    [08:51] Grep     docs
    [09:11] Grep     tools
    [09:11] Grep     quality
    [09:11] Grep     reports
    [09:11] Grep     .github
    [09:39] Grep     ^using Nova
    [09:39] Grep     ^namespace Nova
    [09:39] Grep     "Nova\.[A-Za-z.]+"
    [15:43] · Ich habe das gesamte Repo durchsucht (kein Sampling): 2.604 case-sensitive `Nova`-Treffer in 527 Dateien, dazu die Großbuchstaben-Konstanten (`NOVA_*`), `Proje…
    [15:43] finished
