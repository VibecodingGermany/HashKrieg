# kimi-agent report

- when:    2026-08-30T00:16:08Z
- backend: cc
- model:   k3[1m]
- mode:    rw
- dir:     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa
- run:     /Users/denniswestermann/.agent-runs/20260830-021608-73896

## Task

Du arbeitest an "Project Nova" / HashKrieg, einem Unity-RTS. Bericht: Deutsch.

**ARBEITSVERZEICHNIS — der einzige Pfad, unter dem du liest und schreibst:**

    /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa

Es gibt daneben eine Arbeitskopie des Repos unter
`/Volumes/2TB_CodingProjekte/Coding_Projekte/Project Nova`. **Fass die nicht an,
weder lesend noch schreibend.**

## Worum es geht

Das Projekt hieß „Project Nova" und heißt „Hashkrieg". Das GitHub-Repo wurde am
09.08.2026 umbenannt. Die Umbenennung im Bestand ist Issue #14 und läuft in
Stufen; Stufe 1 (Gate-Verträge und lebende URLs) ist erledigt.

**Du machst Stufe 3: die lebende Prosa unter `docs/**`.** Das ist der
großflächige, mechanische Teil — und genau deshalb der, bei dem man am meisten
kaputtmachen kann.

Die Vorarbeit liegt vor: ein vollständiges Inventar unter
`reports/v8.6.0/umbenennung-hashkrieg/01-kimi-inventar.md`. **Lies es zuerst.**
Es teilt alle Fundstellen in fünf Risikoklassen ein und benennt die
Entscheidungslage. Du arbeitest gegen dieses Inventar, prüfst es aber nach — es
ist eine Momentaufnahme vom Vortag, kein Gesetz.

## Die zwei Regeln, an denen alles hängt

**Regel 1 — E-3: nur die Marke, niemals die Code-Identität.** Der Inhaber hat am
06.08.2026 entschieden (`docs/production/hashkrieg/00_Entscheidungen.md`,
Abschnitt E-3): nach außen heißt alles Hashkrieg, die Code-Identität bleibt
`Nova.*`. Konkret **bleiben unverändert**, auch wenn sie in Prosa vorkommen:

- Namensräume und Assembly-Namen: `Nova.Simulation`, `Nova.Gameplay`,
  `Nova.Presentation.UI`, `Nova.Core`, alle siebzehn `*.asmdef`
- Pfade und Werkzeugnamen: `tools/Nova.SimRunner`, `tools/Nova.AiLab`,
  `Nova.SimRunner.Tests`
- Vertragskonstanten: `NOVA_FIXED_POINT`, alle `NOVA_*`-Umgebungsvariablen,
  Hash-Domänen, Datei-Magics
- Typnamen wie `INovaLogger`
- Der Repository-Bezeichner `VibecodingGermany/HashKrieg` (wörtlich, mit großem
  K — das ist die Adresse, kein Prosawort)

Wenn ein Satz lautet „die Nova.Simulation-Assembly des Project-Nova-Projekts",
wird daraus „die `Nova.Simulation`-Assembly des Hashkrieg-Projekts". Der
Unterschied ist die ganze Aufgabe.

**Regel 2 — die Vergangenheit wird nicht umgeschrieben.** Historische Dokumente
behalten den alten Namen, weil er dort der historisch richtige ist. Eine
Umbenennung, die die Vergangenheit umschreibt, macht die Dokumentation
unbrauchbar: dann stimmt kein Sprintbericht mehr mit dem überein, was damals
tatsächlich hieß, wie es hieß.

Nach dem Inventar gehören dazu (prüf jede Zuordnung selbst nach):

- `docs/production/DecisionLog.md` — das Entscheidungsprotokoll, einschließlich
  der Umbenennungsentscheidungen selbst
- `docs/production/sprints/**` und `docs/production/hashkrieg/`:
  `00_Entscheidungen.md`, `01_Bestandsaufnahme.md`, `05_Umbenennung.md`, die
  nummerierten Sprintdateien, und `Testberichte/**`
- `docs/production/StatusSnapshot_2026-08-05.md`
- `docs/research/**`
- Zeitzeugnisse einzelner Zeilen, etwa ein Titel „… für Project Nova" oder ein
  alter Issue-Link, der belegt, wie es damals hieß

**Der Zweifelsfall gehört in den Report, nicht in den Diff.** Wenn du nicht
sicher entscheiden kannst, ob eine Datei lebt oder Geschichte ist, lässt du sie
in Ruhe und listest sie mit Begründung auf. Zu wenig geändert ist ein
Folge-PR; zu viel geändert ist ein Vertrauensverlust in die Doku.

## Was du zusätzlich mitnimmst

Das Inventar nennt einen Nebenbefund: **`README.md:441` behauptet, „Repo, Code
und Wiki laufen weiter unter *Project Nova*"** — das stimmt seit dem 09.08.
nicht mehr. `README.md` ist eine Wurzeldatei und für dich **verboten**; nimm die
Stelle in den Report auf, ich ziehe sie nach. Prüf, ob es in `docs/**`
Entsprechungen gibt, die dasselbe Falsche behaupten — die gehören dir.

Und: **die Schreibweise des neuen Namens ist im Bestand uneinheitlich**
(`HashKrieg` / `Hashkrieg` / `HASHKRIEG`). Für Prosa gilt `Hashkrieg` — ein
normales deutsches Substantiv. `HashKrieg` bleibt nur dort stehen, wo es die
GitHub-Adresse ist, und `HASHKRIEG` nur dort, wo es eine Versalien-Anzeige im
Spiel beschreibt. Vereinheitliche das in deinem Bereich mit.

## Schreibhoheit — verbindlich

ERLAUBT:
  docs/**              außer den oben genannten historischen Dateien
  reports/v8.6.0/umbenennung-hashkrieg/   nur deine eigenen Dateien

VERBOTEN:
  README.md  AGENTS.md  CONTRIBUTING.md  GOVERNANCE.md  SECURITY.md
  CODE_OF_CONDUCT.md  NOTICE  LICENSE  CONTRIBUTOR_LICENSE_AGREEMENT.md
                       Wurzeldateien, Einzelschreiber — ich mache die
  CHANGELOG.md  VERSION  ROADMAP.md  plans/**  global.json
  Assets/**  tools/**  quality/**  .github/**  ProjectSettings/**
  docs/production/DecisionLog.md  und die übrigen historischen Dateien oben

**Rühr keine Datei außerhalb von `docs/**` an**, auch nicht „nur eine Zeile".

## Verifikation

    python3 .github/scripts/check_docs.py

Das ist die Doku-Prüfung, die die CI fährt (`docs-checks.yml`). **Fahr sie
vorher und nachher** und schreib beide Ausgaben wörtlich in den Report. Sie
prüft unter anderem interne Verweise — eine Umbenennung, die einen Dateinamen
oder Anker mitzieht, bricht Links, und genau dafür ist sie da.

Prüf zusätzlich selbst, ob interne Links noch zielen: wenn du eine Überschrift
umbenennst, wandert der Anker mit.

Was du nicht prüfen kannst: ob ein Satz nach der Änderung noch stimmt. Das ist
Lesearbeit, und sie ist der eigentliche Wert dieses Auftrags — ein
Suchen-und-Ersetzen hätte ich auch ohne dich.

## Was du NICHT tust

- Kein `git commit`, `git push`, `git add`, kein PR, kein `gh`-Aufruf.
- Keine Subagenten.
- **Kein blindes Suchen-und-Ersetzen über das ganze Verzeichnis.** Jede
  Fundstelle wird angesehen. Wenn dir das zu viel erscheint: das ist der
  Auftrag, und du hast das Kontextfenster dafür.

## Report

Markdown nach `reports/v8.6.0/umbenennung-hashkrieg/`. Struktur:

  1. Was du geändert hast, nach Verzeichnis gruppiert, mit Trefferzahlen
  2. Was du bewusst NICHT angefasst hast und warum — die Klasse-E-Liste, wie du
     sie am Bestand geprüft hast, mit jeder Abweichung vom Inventar
  3. Die Zweifelsfälle, einzeln, mit deiner Einschätzung
  4. Fundstellen außerhalb deiner Schreibhoheit, die ich nachziehen muss
  5. `check_docs.py` vorher / nachher, wörtlich
  6. Ein CHANGELOG-Vorschlagstext

Schließe mit:

  STATUS: DONE | BLOCKED
  - Befund 1
  - Befund 2
  - Befund 3

## Output

# Stufe 3: Lebende Prosa unter `docs/**` — Ausführungsbericht

**Arbeitsstand:** Branch `docs/s23-umbenennung-stufe3`, Worktree `nova-wt/prosa`.
**Ergebnis:** 48 Dateien geändert, 74 Zeilen (73 inhaltliche Stellen; eine Stelle
geht über einen Zeilenumbruch). `check_docs.py` vorher und nachher grün, Ausgaben
identisch. Keine Datei außerhalb von `docs/**` angefasst.

**Vorgehen:** Kein Suchen-und-Ersetzen. Jede Fundstelle wurde einzeln angesehen
und zugeordnet (Marke-Prosa → geändert; Code-Identität/Ist-Zitat → belassen;
Zeitzeugnis → belassen; Zweifel → belassen und hier gelistet). Das Inventar
(`01-kimi-inventar.md`) wurde gegen den Bestand nachgeprüft; Abweichungen unten
in Abschnitt 2/3.

---

## 1. Was geändert wurde (nach Verzeichnis, mit Trefferzahlen)

Alle Änderungen folgen demselben Muster: „Project Nova" / „Nova" als
*Projekt-Marke in lebender Prosa* → „Hashkrieg". Code-Identität (`Nova.*`,
`NOVA_*`, `tools/Nova.*`, `NovaUnit`, `NovaUrp`, `INovaLogger` …) wurde
überall unverändert gelassen.

### `docs/vision/` — 5 Dateien, 23 Stellen
| Datei | Stellen | Inhalt |
|---|---|---|
| `Vision.md` | 5 | Titel, Zweckzeile, Leitbild-Zitat (Z. 31), „als ‚Nova' erkennbar" (Z. 95), Überschrift „Was Project Nova bewusst NICHT ist" (Z. 98) |
| `USP.md` | 8 | Titel, Zweckzeile, 5 Wettbewerbsvergleiche („Nova geht zwei Schritte weiter…"), Tabellenkopf (Z. 69) |
| `TargetAudience.md` | 7 | Titel, Zweckzeile, Persona-Texte (Z. 59/66/127/129), „Nova-nahe Daten" (Z. 141) |
| `CoreGameplay.md` | 2 | Zweckzeile, „Project Nova richtet sich an H1…" (Z. 21) |
| `GameLoop.md` | 1 | Zweckzeile |

**Nicht angefasst:** `Konzept_Hashkrieg.md:1` („…Compute-Kriegswirtschaft für
Project Nova") — Zeitzeugnis, im Auftrag ausdrücklich als Beispiel genannt.

### `docs/gamedesign/` — 11 Dateien, 11 Stellen
Je die Zweck-Kopfzeile (Z. 7): `Balancing.md`, `Biomes.md`, `Campaign.md`,
`DamageSystem.md`, `Economy.md`, `FogOfWar.md`, `Maps.md`
(„Project-Nova-Karten" → „Hashkrieg-Karten"), `MultiplayerModes.md`,
`ResearchTree.md`, `VictoryConditions.md`; dazu `Resources.md:63`
(„Das unterscheidet Nova von SupCom" → „…Hashkrieg…").

### `docs/tech/` — 7 Dateien, 9 Stellen
| Datei | Stellen | Inhalt |
|---|---|---|
| `NamingConvention.md` | 2 | Titel; Datei-Header-Konvention `// Project Nova – <Zweck>` → `// Hashkrieg – <Zweck>` (§9, Z. 175 — s. Zweifelsfälle) |
| `SimulationCore.md` | 1 | Zweckzeile („Snapshot- und Replay-Vertrag für Hashkrieg") |
| `Rendering.md`, `Lighting.md`, `AudioArchitecture.md`, `AnimationSystem.md` | je 1 | Zweckzeile |
| `AssetBudget.md` | 2 | Zweckzeile; „für Nova **nicht relevant**" (Z. 108) |

### `docs/tech/modules/` — 14 Dateien, 14 Stellen
Je die Zweckzeile (Z. 7, „von *Project Nova*" → „von *Hashkrieg*"):
`AssetIntegration_Spec`, `CombatSystem_Spec`, `CommandSystem_Spec`,
`CommanderSystem_Spec`, `ConstructionSystem_Spec`, `EconomySystem_Spec`,
`EvolvedFaction_Spec`, `LockstepRelay_Spec`, `LockstepReplay_Spec`,
`MapExpansion_Spec`, `ProductionSystem_Spec`, `RtsUi_Spec`, `SkirmishAi_Spec`,
`VisionSystem_Spec`.

Der Status-Zusatz dieser Specs („historischer Prototyp-/Scaffolding-Stand gemäß
D-055") bezeichnet den beschriebenen *Code-Stand*, nicht das Dokument als
Zeitzeugnis — die Specs sind die laufende Modul-Referenz des Wikis. Darum
geändert. `GameDatabase_Spec.md:66` (Menüpfad-Zitat) **nicht** angefasst.

### `docs/assets/` — 7 Dateien, 12 Zeilen / 11 Stellen
| Datei | Stellen | Inhalt |
|---|---|---|
| `Provenance.md` | 2 | Zweckzeile; CREDITS-Vorlage (Z. 216) |
| `ProcurementStrategy.md` | 3 | Zweckzeile; „verfügt Project Nova über 0 € Budget" (Z. 19); **Repo-Bezeichner** `VibecodingGermany/Project_Nova` → `VibecodingGermany/HashKrieg` (Z. 58) |
| `Licenses.md` | 2 | Zweckzeile; **Repo-Bezeichner** (Z. 42) ebenso gezogen |
| `AssetRegister.md`, `VerticalSlice_MS1.md`, `SourceCatalog_MS1.md` (Z. 137, Risikoeinschätzung) | je 1 | Zweckzeile bzw. Fließtext |
| `ArtAssetStandard.md` | 1 (2 Zeilen) | Zweckzeile mit Zeilenumbruch („von *Project⏎Nova*") — Umbruch neu gesetzt |

### `docs/production/` — 3 Dateien, 3 Stellen
| Datei | Inhalt |
|---|---|
| `Roadmap.md:7` | Zweckzeile („wann Hashkrieg wieder geschätzt werden darf") |
| `DemoRunbook.md:7` | Zweckzeile (Runbook ist die gepflegte Vorführ-Anleitung) |
| `OpenQuestions.md:39` | Zwei Issue-URLs in der **offenen** Frage Q-048: `…/Project_Nova/issues/55|56` → `…/HashKrieg/issues/55|56` (s. Zweifelsfälle) |

### `docs/README.md` — 2 Stellen
Titel („# Hashkrieg – Entwicklungs-Wiki") und Zweckzeile („Hashkrieg-Wiki").

---

## 2. Bewusst NICHT angefasst — die Klasse-E-Liste, am Bestand nachgeprüft

| Bereich | Geprüft? | Ergebnis |
|---|---|---|
| `docs/production/DecisionLog.md` | ja (Trefferliste gesichtet) | Unverändert; Entscheidungsprotokoll inkl. E-3 selbst |
| `docs/production/sprints/` (Sprint00–06_Report) | ja | Unverändert; Sprint-Historie |
| `docs/production/hashkrieg/`: `00_Entscheidungen.md`, `01_Bestandsaufnahme.md`, `05_Umbenennung.md`, alle nummerierten Sprintdateien (02–22, inkl. `13-15_Parallelbetrieb`, `16-19_Betatest_Einordnung`, `20_Vorschlag_*`), `Testberichte/**` | ja | Unverändert; Zeitdokumente. Enthalten u. a. alte Repo-URLs — dort historisch richtig |
| `docs/production/StatusSnapshot_2026-08-05.md` | ja | Unverändert (hat ohnehin keinen Projektnamen-Treffer) |
| `docs/research/**` (~9 Dateien mit „Project Nova") | ja | Unverändert; Research-Dokumente aus der Project-Nova-Zeit |
| `docs/vision/Konzept_Hashkrieg.md:1` | ja | Unverändert; Titel „…für Project Nova" ist das vom Auftrag genannte Zeitzeugnis |

**Ergänzungen zur E-Liste (über das Inventar hinaus, eigene Prüfung):**

- **`docs/tech/review/`** (`Review_Performance.md:7`, `Review_Wartbarkeit_Prozess.md:7`)
  — das Inventar nennt dieses Verzeichnis unter Klasse E nicht ausdrücklich.
  Beide sind abgeschlossene Sprint-4-Prüfberichte („Status: Entwurf, Sprint: 4",
  „ändert keine Bestandsdateien"), funktional denselben Zeitzeugnissen
  vergleichbar wie Testberichte. **Nicht angefasst.**
- **`docs/assets/AssetImport_Tripo_2026-08-06.md`** — datiertes Protokoll eines
  abgeschlossenen Imports („Datum: 2026-08-06", „Protokolliert den ersten
  vollständigen Art-Import von *Project Nova*"). Der Name ist dort historisch
  richtig. **Nicht angefasst** (Z. 122 Menüpfad-Zitat wäre ohnehin Code-Ist).
- **`docs/production/hashkrieg/README.md`** — die Übergangs-Planungsmappe. Ihre
  zwei „Project Nova"-Stellen (Titel Z. 1, Einleitung Z. 8) benennen den
  *Übergang* selbst; ohne den alten Namen ist der Titel gegenstandslos
  („Übergang Hashkrieg → Hashkrieg"). **Nicht angefasst.**
- **`docs/production/hashkrieg/AUFTRAG_Grossblock.md`, `AUFTRAG_Verknappungsfolgen.md`**
  — vollständig gesichtet: enthalten ausschließlich `tools/Nova.*`-Code-Pfade
  (E-3: bleiben) und eine korrekte `…/HashKrieg/pull/97`-GitHub-URL. Es gab
  schlicht nichts zu ändern. Unverändert.

**Abweichung vom Inventar:** Das Inventar führt `AUFTRAG_*.md` und
`21_Sprint_*` unter Klasse D als „aktuelle Arbeitsdoku" (zu aktualisieren).
Der Auftrag stuft „die nummerierten Sprintdateien" ausdrücklich als historisch
ein — das hat Vorrang; `21_Sprint_*` und `22_Sprint_*` (die 22×
„HashKrieg"-Prosa-Schreibweise enthalten) bleiben unverändert. An den
AUFTRAG-Dateien gab es, wie oben gezeigt, nichts Zulässiges zu ändern — die
Abweichung hat also keinen Unterschied im Diff erzeugt.

**Code-Ist-Zitate (bleiben, bis Stufe 2 den Code zieht):** Fünf Menüpfad-Zitate
`Tools/Project Nova/…` (`DemoRunbook.md:34,218`, `GrayboxLog.md:384`,
`AssetPackage.md:114`, `GameDatabase_Spec.md:66`) und sechs Build-Artefakt-Namen
(`GrayboxLog.md:91,92,106,108,217,218` — `ProjectNova.exe/.app/_Data`).
Verifiziert gegen den Worktree-Code: `BuildScript.cs:27,34,41` erzeugt die
Artefakte noch unter diesem Namen, alle 7 `[MenuItem]`-Einträge heißen noch
`Tools/Project Nova/…`. Die Doku zitiert hier den Ist-Zustand; sie zu ändern
machte sie falsch. **Das ist der Stufe-2-Nachzug im Doku-Bereich.**
`AssetPackage.md:78` („`Hashkrieg_`, vorher `ProjectNova_`") ist ein
begründender Vorher/Nachher-Vergleich und bleibt ebenfalls.

**Schreibweisen-Vereinheitlichung:** In meinem Bereich gab es kein
prosaisch falsches `HashKrieg`/`HASHKRIEG`: alle `HashKrieg`-Vorkommen in
lebenden Dateien sind GitHub-Adressen (bleiben), das `HASHKRIEG` in
`DemoRunbook.md:47` beschreibt die Versalien-Titelanzeige im Hauptmenü
(bleibt). Die 22 Vorkommen in `21_Sprint_*`/`22_Sprint_*` sind historische
Sprintdateien und bleiben. Alle meine Neuschreibungen nutzen „Hashkrieg".

---

## 3. Zweifelsfälle (einzeln, mit Einschätzung)

1. **`docs/production/OpenQuestions.md:39` — geändert.** Die Datei ist „aktiv
   (laufend)"; Q-048 ist *offen*, die Links zeigen auf lebende Issues.
   Es ist Link-Wartung in einem lebenden Register (Stufe-1-Logik), keine
   Geschichtskorrektur — die historische Herkunft („aus dem ersten Betatest",
   „T-01, Betatest 2026-08-09") steht im Text und bleibt. Risiko für verwechselte
   Historie sehe ich nicht; wer das anders gewichtet, kann die zwei URLs mit
   einem Revert der einen Zeile zurückholen.
2. **`docs/production/DemoRunbook.md:7` — geändert.** Das Runbook ist die
   gepflegte Gebrauchsanleitung für Demo-Vorführungen (Version 0.6.1,
   mehrfach fortgeschrieben), keine Runde-Protokoll-Datei. Nur die Zweckzeile
   geändert; Menüpfad-Zitate und der „HASHKRIEG"-Menütitel bleiben Code-/Spiel-Ist.
3. **`docs/tech/NamingConvention.md:175` — geändert.** Der Datei-Header-Vertrag
   ist Marke im Kommentar-Schema, kein Code-Bezeichner (kein Typ, kein Hash,
   kein Pfad). Nachweislich trägt heute **keine einzige** `.cs`-Datei diesen
   Header (0 Treffer auf `^// Project Nova`) — die Änderung bricht nichts und
   legt die Konvention für künftige Dateien auf die Marke Hashkrieg fest.
   Die Assembly-Zeile darunter (`Nova.Simulation`, Z. 176) bleibt.
4. **`docs/tech/modules/*_Spec.md` — geändert, trotz „historischer Stand" im
   Status-Feld.** Der Status bezeichnet den *beschriebenen Prototyp-Stand*
   (D-055), nicht das Dokument als Geschichtsquelle; die Specs sind die
   laufende Modul-Referenz. 14× „von *Project Nova*" wäre sonst genau die
   Inkonsistenz, die Stufe 3 beseitigen soll.
5. **`docs/tech/review/` und `AssetImport_Tripo_2026-08-06.md` — NICHT
   angefasst** (meine E-Ergänzung, s. Abschnitt 2). Wenn der Inhaber Reviews
   und datierte Protokolle als lebendig ansieht, sind das drei Zeilen in zwei
   Dateien plus eine Zweckzeile — ein Mini-Folge-PR.
6. **`docs/production/hashkrieg/README.md` — NICHT angefasst** (thematisch
   zwingend, s. Abschnitt 2). Falls gewünscht, wäre denkbar, die Mappe um eine
   Statuszeile „Stufe 3 abgeschlossen" zu ergänzen — nicht mein Auftrag.

---

## 4. Fundstellen außerhalb meiner Schreibhoheit (Nachzug durch dich)

1. **`README.md:441`** — behauptet weiter „Repo, Code und Wiki laufen unter
   *Project Nova*"; seit 09.08. falsch. (Wie im Auftrag vorgesehen nur gemeldet.)
2. **Entsprechungen in `docs/**`: keine verblieben.** Die drei Stellen, die
   dasselbe Falsche implizierten (veraltete Repo-Bezeichner
   `VibecodingGermany/Project_Nova` in `docs/assets/ProcurementStrategy.md:58`
   und `docs/assets/Licenses.md:42` sowie die Issue-URLs in
   `docs/production/OpenQuestions.md:39`), lagen in meiner Hoheit und sind
   gezogen. Festgestellt dabei: Stufe 1 (Commit `8a4b28d`) hat nur `quality/**`
   angefasst — der „lebende URLs"-Teil im Doku-Bereich war faktisch offen und
   ist mit dieser Stufe 3 nun erledigt.
3. **Stufe-2-Haken in `docs/**` (nicht meine Stufe, nur vermerkt):** die fünf
   Menüpfad-Zitate und sechs Build-Artefakt-Zitate aus Abschnitt 2 müssen in
   demselben PR mitgezogen werden, der `BuildScript.cs` und die `[MenuItem]`-
   Strings umbenennt — sonst lügt die Doku in dem Moment, in dem der Code
   wahr wird.
4. **`reports/` ist gitignored** (`.gitignore:26`): dieser Report braucht
   `git add -f` oder eine Ablage-Entscheidung (Nebenbefund 4 des Inventars,
   weiterhin aktuell).

---

## 5. `check_docs.py` — vorher / nachher (wörtlich)

**Vorher:**

```
::notice:: 13 docs/-Datei(en) ohne Status-Kopfzeile (empfohlen, nicht verpflichtend):
  - docs/production/hashkrieg/08_Sprint_Hauptmenue.md
  - docs/production/hashkrieg/09_Sprint_Gefecht_und_Rundenrahmen.md
  - docs/production/hashkrieg/10_Sprint_Baubarkeit_und_Kartenbild.md
  - docs/production/hashkrieg/11_Sprint_Truppenfuehrung.md
  - docs/production/hashkrieg/12B_Sprint_Sichtbares_Gefecht.md
  - docs/production/hashkrieg/12_Sprint_Zu_Zweit.md
  - docs/production/hashkrieg/13B_Sprint_Einheitenverhalten.md
  - docs/production/hashkrieg/14_Sprint_Lobby.md
  - docs/production/hashkrieg/15_Sprint_Netzstabilitaet.md
  - docs/production/hashkrieg/16-19_Betatest_Einordnung.md
  - docs/production/hashkrieg/20_Vorschlag_Verknappungsfolgen.md
  - docs/production/hashkrieg/Testberichte/2026-08-09_a434e2c_T-01.md
  - docs/production/hashkrieg/Testberichte/2026-08-10_4053c15_T-01.md
OK: 196 Markdown-Dateien und 5 Quality-JSONs geprüft.
EXIT=0
```

**Nachher:**

```
::notice:: 13 docs/-Datei(en) ohne Status-Kopfzeile (empfohlen, nicht verpflichtend):
  - docs/production/hashkrieg/08_Sprint_Hauptmenue.md
  - docs/production/hashkrieg/09_Sprint_Gefecht_und_Rundenrahmen.md
  - docs/production/hashkrieg/10_Sprint_Baubarkeit_und_Kartenbild.md
  - docs/production/hashkrieg/11_Sprint_Truppenfuehrung.md
  - docs/production/hashkrieg/12B_Sprint_Sichtbares_Gefecht.md
  - docs/production/hashkrieg/12_Sprint_Zu_Zweit.md
  - docs/production/hashkrieg/13B_Sprint_Einheitenverhalten.md
  - docs/production/hashkrieg/14_Sprint_Lobby.md
  - docs/production/hashkrieg/15_Sprint_Netzstabilitaet.md
  - docs/production/hashkrieg/16-19_Betatest_Einordnung.md
  - docs/production/hashkrieg/20_Vorschlag_Verknappungsfolgen.md
  - docs/production/hashkrieg/Testberichte/2026-08-09_a434e2c_T-01.md
  - docs/production/hashkrieg/Testberichte/2026-08-10_4053c15_T-01.md
OK: 196 Markdown-Dateien und 5 Quality-JSONs geprüft.
EXIT=0
```

Identisch, grün, beide Male. Die 13 Notices betreffen ausschließlich
historische Dateien, die nicht angefasst wurden. Zusätzlich selbst geprüft:
Es gibt **keine Anker-Links** auf die fünf geänderten Überschriften
(`Vision.md#`, `USP.md#`, `TargetAudience.md#`, `NamingConvention.md#`,
`docs/README.md#` — 0 Treffer im Repo); Dateinamen wurden nicht umbenannt,
also kann kein interner Link gebrochen sein.

---

## 6. CHANGELOG-Vorschlagstext

```markdown
### Changed
- Dokumentation auf die Marke Hashkrieg gezogen (#14, Stufe 3): Die lebende
  Prosa unter `docs/**` nennt das Projekt jetzt durchgehend Hashkrieg —
  Wiki-Einstieg, Vision/Gamedesign/tech-Kopfzeilen, die 14 Modul-Specs, die
  Asset-Register- und Lizenzdokumente sowie die noch offenen Issue-Verweise in
  OpenQuestions. Code-Identität bleibt gemäß E-3 unverändert (`Nova.*`-Assemblies,
  `NOVA_*`-Vertragskonstanten, `tools/Nova.*`-Pfade); historische Dokumente
  (DecisionLog, Sprint- und Testberichte, Research, die Umbenennungs-Mappe
  selbst) behalten den damals richtigen Namen. Wörtliche Zitate des
  Code-Ist-Zustands (`Tools/Project Nova/…`-Menüpfade, `ProjectNova.*`-Build-Artefakte)
  folgen erst mit Stufe 2 dem Code.
```

---

STATUS: DONE
- 48 Dateien unter `docs/**` auf die Marke Hashkrieg gezogen (74 Zeilen, jede Stelle einzeln gesichtet); Code-Identität (`Nova.*`, `NOVA_*`, `tools/Nova.*`) überall unverändert; `check_docs.py` vorher/nachher identisch grün, keine internen Links betroffen (keine Anker-Verweise auf geänderte Überschriften, keine Datei umbenannt).
- Klasse-E-Liste am Bestand nachgeprüft und um drei eigene Einstufungen ergänzt: `docs/tech/review/` (abgeschlossene Sprint-4-Prüfberichte), `docs/assets/AssetImport_Tripo_2026-08-06.md` (datiertes Protokoll) und `docs/production/hashkrieg/README.md` (Übergangsmappe, alter Name thematisch zwingend) bleiben unverändert; einzige Inventar-Abweichung: 21_/22_Sprint gelten per Auftrag als historisch, nicht als „aktuelle Arbeitsdoku".
- Außerhalb meiner Hoheit: `README.md:441` behauptet weiter, das Repo laufe unter Project Nova (Nachzug durch den Orchestrator); fünf `Tools/Project Nova/…`-Menüpfad-Zitate und sechs `ProjectNova.*`-Build-Artefakt-Zitate in lebenden docs bleiben bewusst stehen, weil der Code sie noch so erzeugt — sie müssen synchron mit Stufe 2 gezogen werden.
## Exit

exit=0

## Progress

    [00:18] · Ich beginne mit der Lektüre des Inventars und dem Ist-Zustand.
    [00:18] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/reports/v8.6.0/umbenennung-hash…
    [00:18] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/reports/v8.6.0/umbenennung-hash…
    [00:37] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && python3 .github/scripts…
    [00:37] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && find docs -name "*.md" …
    [00:52] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && echo "=== 'Project Nova…
    [00:55] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && echo "=== 'HashKrieg' p…
    [02:26] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && grep -rn "Project Nova\…
    [03:05] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && echo "=== MenuItem im C…
    [03:05] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && echo "=== git log Stufe…
    [03:22] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && echo "=== 'Nova' als Pr…
    [03:51] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && echo "=== HashKrieg/HAS…
    [03:53] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && echo "=== Nova in docs/…
    [04:18] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && echo "=== 'Project<NL>N…
    [04:19] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && echo "=== hashkrieg/REA…
    [04:47] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs
    [05:45] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && echo "=== Aussagen 'Rep…
    [06:51] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && echo "=== Stufe-1-Commi…
    [07:19] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && echo "=== AssetImport_T…
    [07:59] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && for f in docs/vision/Vi…
    [08:13] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/Vision.md
    [08:13] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/Vision.md
    [08:30] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/Vision.md
    [08:30] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/Vision.md
    [08:37] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/Vision.md
    [08:37] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/Vision.md
    [08:37] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/Vision.md
    [08:46] · Vision.md erledigt (5 Stellen). Weiter mit USP.md.
    [08:46] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/USP.md
    [08:46] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/USP.md
    [09:00] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/USP.md
    [09:08] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/USP.md
    [09:08] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/USP.md
    [09:13] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/USP.md
    [09:19] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/USP.md
    [09:25] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/USP.md
    [09:31] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/USP.md
    [09:32] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/USP.md
    [09:48] · USP.md erledigt (8 Stellen). Weiter mit TargetAudience.md, CoreGameplay.md und GameLoop.md.
    [09:48] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/TargetAudience.md
    [09:48] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/TargetAudience.md
    [09:48] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/TargetAudience.md
    [10:10] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/TargetAudience.md
    [10:10] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/TargetAudience.md
    [10:10] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/TargetAudience.md
    [10:18] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/TargetAudience.md
    [10:18] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/TargetAudience.md
    [10:25] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/TargetAudience.md
    [10:25] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/TargetAudience.md
    [10:32] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/CoreGameplay.md
    [10:32] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/GameLoop.md
    [10:44] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/CoreGameplay.md
    [10:44] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/CoreGameplay.md
    [10:46] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/vision/GameLoop.md
    [11:00] · `docs/vision/` ist erledigt (23 Stellen). Weiter mit `docs/gamedesign/` — erst die vollständige Trefferliste aller elf Dateien prüfen, ob es neben den Kopfzeil…
    [11:01] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && for f in docs/gamedesig…
    [11:20] · Campaign.md und Balancing.md fehlen in der Ausgabe — ich prüfe sie direkt.
    [11:20] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && grep -n "Nova" docs/gam…
    [11:37] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/Campaign.md
    [11:37] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/Balancing.md
    [11:37] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/Biomes.md
    [11:37] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/DamageSystem.md
    [11:45] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/Economy.md
    [11:45] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/FogOfWar.md
    [11:45] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/Maps.md
    [11:45] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/MultiplayerMode…
    [11:45] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/ResearchTree.md
    [11:48] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/VictoryConditio…
    [11:48] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/Resources.md
    [12:11] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/Campaign.md
    [12:17] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/Balancing.md
    [12:18] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/Biomes.md
    [12:24] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/DamageSystem.md
    [12:24] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/Economy.md
    [12:31] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/FogOfWar.md
    [12:31] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/Maps.md
    [12:37] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/MultiplayerMode…
    [12:38] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/ResearchTree.md
    [12:42] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/VictoryConditio…
    [12:43] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/gamedesign/Resources.md
    [12:52] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && for f in docs/tech/Nami…
    [13:07] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/NamingConvention.md
    [13:07] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/AssetBudget.md
    [13:22] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/NamingConvention.md
    [13:30] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/SimulationCore.md
    [13:31] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/Rendering.md
    [13:31] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/Lighting.md
    [13:31] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/AudioArchitecture.md
    [13:31] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/AssetBudget.md
    [13:31] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/AnimationSystem.md
    [13:43] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/NamingConvention.md
    [13:43] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/NamingConvention.md
    [13:50] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/SimulationCore.md
    [13:50] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/Rendering.md
    [13:57] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/Lighting.md
    [13:57] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/AudioArchitecture.md
    [14:03] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/AssetBudget.md
    [14:03] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/AssetBudget.md
    [14:04] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/AnimationSystem.md
    [14:14] · Jetzt die 14 Modul-Specs in `docs/tech/modules/`.
    [14:14] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/ConstructionS…
    [14:23] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/RtsUi_Spec.md
    [14:23] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/AssetIntegrat…
    [14:23] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/CombatSystem_…
    [14:23] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/CommandSystem…
    [14:23] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/EvolvedFactio…
    [14:32] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/SkirmishAi_Sp…
    [14:32] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/LockstepRepla…
    [14:32] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/EconomySystem…
    [14:32] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/MapExpansion_…
    [14:32] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/VisionSystem_…
    [14:36] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/ProductionSys…
    [14:36] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/LockstepRelay…
    [14:36] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/CommanderSyst…
    [14:58] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/ConstructionS…
    [15:05] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/RtsUi_Spec.md
    [15:05] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/AssetIntegrat…
    [15:13] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/CombatSystem_…
    [15:13] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/CommandSystem…
    [15:13] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/EvolvedFactio…
    [15:20] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/SkirmishAi_Sp…
    [15:20] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/LockstepRepla…
    [15:27] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/EconomySystem…
    [15:27] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/MapExpansion_…
    [15:34] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/VisionSystem_…
    [15:34] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/ProductionSys…
    [15:38] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/LockstepRelay…
    [15:38] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/tech/modules/CommanderSyst…
    [15:53] · `docs/tech/modules/` erledigt (14 Stellen). Weiter mit `docs/assets/` — die lebenden Register- und Rechtsdokumente, inklusive der zwei veralteten Repo-URLs.
    [15:53] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/Provenance.md
    [15:53] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/Provenance.md
    [16:01] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/ProcurementStrategy…
    [16:01] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/ProcurementStrategy…
    [16:01] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/ProcurementStrategy…
    [16:01] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/Licenses.md
    [16:01] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/Licenses.md
    [16:21] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/Provenance.md
    [16:21] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/Provenance.md
    [16:27] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/ProcurementStrategy…
    [16:27] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/ProcurementStrategy…
    [16:34] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/ProcurementStrategy…
    [16:34] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/Licenses.md
    [16:36] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/Licenses.md
    [17:02] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/AssetRegister.md
    [17:02] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/VerticalSlice_MS1.md
    [17:03] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/SourceCatalog_MS1.md
    [17:03] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/ArtAssetStandard.md
    [17:18] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/AssetRegister.md
    [17:25] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/VerticalSlice_MS1.md
    [17:25] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/SourceCatalog_MS1.md
    [17:28] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/assets/ArtAssetStandard.md
    [17:41] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/production/Roadmap.md
    [17:41] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/production/DemoRunbook.md
    [17:43] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/production/OpenQuestions.md
    [17:43] Read     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/README.md
    [17:58] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/production/Roadmap.md
    [18:04] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/production/DemoRunbook.md
    [18:11] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/production/OpenQuestions.md
    [18:11] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/README.md
    [18:12] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs/README.md
    [18:35] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && echo "=== Verbleibende …
    [18:35] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs
    [19:05] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && echo "=== Anker-Links a…
    [19:39] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && echo "=== 'Nova' als ei…
    [19:39] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && ls docs/production/hash…
    [19:58] Grep     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/docs
    [19:58] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && grep -n "Nova" docs/pro…
    [20:17] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && rg --pcre2 -n '(?<![\w.…
    [20:30] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && python3 .github/scripts…
    [20:30] Bash     cd "/Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa" && git diff --stat && echo…
    [23:51] Edit     /Volumes/2TB_CodingProjekte/Coding_Projekte/nova-wt/prosa/reports/v8.6.0/umbenennung-hash…
    [24:30] · Stufe 3 ist abgeschlossen. Zusammenfassung der Arbeit: **Geändert (48 Dateien, 74 Zeilen, jede Stelle einzeln gesichtet):** - `docs/vision/` — 5 Dateien, 23 St…
    [24:31] finished
