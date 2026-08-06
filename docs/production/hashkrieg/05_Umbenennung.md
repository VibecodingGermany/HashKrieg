# Umbenennung Project Nova → Hashkrieg

**Version:** 0.1.0 | **Status:** Entwurf – Ausführungsanleitung, kein Gate-Nachweis | **Verantwortungsbereich:** Orchestrator | **Sprint:** 7

## Zweck

Der Rename in sechs Stufen, nach Risiko sortiert, mit den Stellen, die man
übersieht — und den drei Stellen, die man **nicht** anfassen darf.

## Abhängigkeiten

- [README.md](README.md) – Entscheidung E-3: wie weit geht die Umbenennung
- [../DecisionLog.md](../DecisionLog.md) – hier fehlt der Beschluss
- [../../vision/Konzept_Hashkrieg.md](../../vision/Konzept_Hashkrieg.md) – Namensquelle, Status „nicht verbindlich"

## Das Ausmaß

**1.590 getrackte Zeilen enthalten „Nova", verteilt auf 97 Dateipfade.**
38 Zeilen nutzen bereits „Hashkrieg" — ausschließlich in der Dokumentation.

## Stufe 0 — Beschluss und Zielschema *(blockiert alles)*

Es gibt **keinen verbindlichen Beschluss**.
[../../vision/Konzept_Hashkrieg.md](../../vision/Konzept_Hashkrieg.md) trägt
Status „Brainstorm – NICHT verbindlich" und erzeugt ausdrücklich keine
DecisionLog-Einträge. Eine Suche nach „Hashkrieg", „Umbenenn" oder „Rename" im
[../DecisionLog.md](../DecisionLog.md) liefert **null Treffer**. Die letzte
vergebene Nummer ist D-076.

Zwei Fragen sind zu beantworten, bevor irgendetwas passiert:

1. **Umfang** — nur die Marke, oder auch die Code-Identität?
2. **Zielschema** — `Hashkrieg.Core` (analog zu heute) oder eine Kurzform wie
   `Hk.Core`? Die Antwort bestimmt 786 Zeilen.

Erst ein Beschluss erzeugt eine D-ID. Ohne ihn ist jede Stufe ab 3 unzulässig.

---

## Stufe 1 — Prosa · Aufwand S · Risiko keins

487 Markdown-Zeilen in `README.md`, `AGENTS.md`, `CONTRIBUTING.md`,
`GOVERNANCE.md` und `docs/`.

**Zwei Ausnahmen:**

- **`CHANGELOG.md` ist Historie.** 76 der Zeilen stehen dort. Alte Einträge
  umzuschreiben fälscht den Verlauf — es gehört nur ein neuer
  `[Unreleased]`-Eintrag dazu.
- **Tote interne Links brechen die CI.** Das Doku-Prüfskript beendet sich mit
  Fehler. Jede umbenannte verlinkte Datei muss dort grün bleiben.

---

## Stufe 2 — Marke · Aufwand S · Risiko gering

**Das ist der „Umzug", der sich unmittelbar anfühlt.** Er kostet einen
Nachmittag und kann jederzeit passieren.

| Ort | Inhalt |
|---|---|
| `ProjectSettings/ProjectSettings.asset` | `companyName`, `productName` |
| `Assets/_Project/Editor/BuildScript.cs` | Firmenname, Produktname, Ausgabepfade |
| `quality/scripts/run_gate_check.py` | **derselbe Ausgabepfad als Erwartungswert** |

> **Die Falle:** Die Build-Ausgabepfade (`ProjectNova.exe` / `.app`) stehen
> **doppelt** — im Build-Skript und als Erwartungswert im Gate-Prüfskript. Wer
> nur eine Seite ändert, bekommt einen Gate-Fehlschlag, der wie ein Build-Fehler
> aussieht.

Nebenwirkung ohne Bedeutung: `companyName` und `productName` bilden unter Unity
den PlayerPrefs-Pfad — lokale Einstellungen gehen verloren. Im Graybox-Stand
irrelevant.

---

## Stufe 3 — GitHub-Repository · Aufwand M · Risiko hoch

Der Repository-Name `VibecodingGermany/Project_Nova` ist **hart validiert**:

- `quality/schemas/GateEvidence.schema.json` — zweimal als `"const"`
- `quality/schemas/GateAuthorization.schema.json` — einmal als `"const"`
- `quality/scripts/validate_gate_evidence.py` — dieselbe Konstante

**Das Dilemma:** Nach einem Repository-Rename schlägt jede *neu erzeugte*
Evidence gegen den alten `const` fehl. Nach Anpassung des `const` schlagen alle
*alten* Belege fehl.

Braucht eine bewusste Entscheidung — etwa ein Übergangs-`enum` mit beiden Namen.
Nicht nebenbei umbenennen.

*Nebenbei zu klären:* GitHub legt eine Weiterleitung vom alten Namen an, aber
lokale Remotes, CI-Badges und externe Links sollten trotzdem nachgezogen werden.

---

## Stufe 4 — Code-Identität · Aufwand L · Risiko **hoch, nur atomar sicher**

### 4a — Die 17 Assembly-Definitionen: alles in EINEM Commit

Alle 17 `.asmdef` referenzieren einander per **Klartext-Namen**, nicht per
`"GUID:…"`:

```json
"references": ["Nova.Core", "Nova.Simulation"]
```

> **Wird ein einziger `name`-Wert geändert, ohne alle referenzierenden Dateien
> mitzuändern, fällt die komplette Unity-Kompilation aus.**

Der Rename muss in einem Commit **alle 17 `name`-Werte, alle 17
`rootNamespace`-Werte und alle `references`-Einträge** treffen — 78 Zeilen. Die
`.asmdef`-**Dateien** selbst umzubenennen ist optional und kann separat
passieren.

### 4b — 786 Namespace- und using-Zeilen in 226 Dateien

226 × `namespace Nova.*` und 560 × `using Nova.*`. Mechanisch — aber die
Abgrenzung ist die Gefahr:

| Nicht anfassen | Warum |
|---|---|
| `INovaLogger`, `NullNovaLogger`, `UnityNovaLogger` | **Typnamen**, keine Marke |
| `"Nova graybox HUD"` im HUD | Anzeigestring |

Ein `sed` auf `Nova\.` (mit Punkt) lässt diese korrekt in Ruhe. Ein `sed` auf
`Nova` benennt sie mit um. Beides ist vertretbar — aber es muss **entschieden
und dann konsistent durchgezogen** werden.

### 4c — Das Gate-Prüfskript hält eine Assembly-Namensmap, die stumm veraltet

`quality/scripts/run_gate_check.py` verdrahtet die Strings `Nova.Core` …
`Nova.Editor` an mehreren Stellen: in der Schichtenkarte, in der Erlaubnisliste,
in der Regelprüfung („`Nova.Simulation` darf `Nova.AI` nicht referenzieren") und
in einem Negativtest, der eine Sandbox-Datei wörtlich als
`Nova.Simulation.asmdef` anlegt.

**Nach einem asmdef-Rename ohne Anpassung prüft das Gate Assemblies, die es
nicht mehr gibt — möglicherweise ohne rot zu werden.** Das ist die gefährlichste
Einzelstelle des ganzen Renames, weil sie schweigt.

### 4d — `tools/Nova.SimRunner`: Verzeichnisnamen sind in CI und Vorlagen verdrahtet

Die beiden `.csproj` unter `tools/` sind die einzigen **getrackten**
Projektdateien (338 Nova-Zeilen). Ihr Pfad steht in:

- `.github/workflows/tests.yml` (zweimal — der einzige Testschritt der CI)
- `.github/pull_request_template.md`
- `quality/scripts/run_gate_check.py`
- `README.md` (zweimal)

Der Tests-`csproj` linkt zudem sieben Quelldateien über `..\Nova.SimRunner\`.

### 4e — Menüpfade sind mit fünf Doku-Stellen gekoppelt

Sechsmal `[MenuItem("Tools/Project Nova/…")]` und siebenmal
`[CreateAssetMenu(menuName = "Project Nova/…")]`. Technisch folgenlos — aber die
Menüpfade sind wörtlich in Anleitungen zitiert: `README.md`,
[../DemoRunbook.md](../DemoRunbook.md) (zweimal),
[../../assets/AssetPackage.md](../../assets/AssetPackage.md),
[../GrayboxLog.md](../GrayboxLog.md).

**Wer den Menüpfad ändert und die Runbooks nicht, macht die Anleitung falsch.**

---

## Stufe 5 — Prüfverträge nachziehen · Aufwand S · Risiko mittel

Coverage-Schwellen und Testfixturen sind auf Assembly-Namen verdrahtet:
`quality/content/mvp-v1.json` setzt eine 80-%-Schwelle für `Nova.Simulation`,
das Validierungsskript nutzt denselben Namen in Selbsttests.

**Bleibt der alte Name stehen, greift die Schwelle nach dem Rename ins Leere und
Coverage-Regression wird nicht mehr bemerkt.**

---

## Stufe 6 — Aufräumen · Aufwand S · Risiko keins

- 17 Root-`.csproj` und `Project Nova.slnx` sind ignoriert und werden von Unity
  neu erzeugt — einmal löschen, sonst zeigt die IDE dauerhaft auf tote Projekte.
- `.vscode/settings.json` zeigt auf `"Project Nova.slnx"` und ist **getrackt**.
- Das Arbeitsverzeichnis heißt selbst `Project Nova` — **zuletzt** umbenennen,
  danach IDE-Caches neu aufbauen lassen.

---

## Was NICHT umbenannt werden darf

### `NOVA_FIXED_POINT` — ein Build-Flag mit Vertragscharakter

Der Define steht in den Projekteinstellungen und beiden `.csproj`, wird per `#if`
ausgewertet, **in das Determinismus-Artefakt geschrieben** und dort per Test hart
zugesichert (`Assert.That(…, Is.EqualTo("NOVA_FIXED_POINT"))`).

Das ist kein Markenname, sondern eine Vertragskonstante zwischen Build,
Artefakt und Test. Umbenennen bricht den Determinismus-Nachweis.

### `INovaLogger` und Verwandte

Typnamen. Können umbenannt werden, müssen aber nicht — und wenn, dann als
separater, bewusster Refactoring-Schritt, nicht als Nebenwirkung eines `sed`.

---

## Vorbestehende Drift, die beim Rename mitkopiert würde

`quality/scripts/run_gate_check.py` führt `Nova.Presentation.Maps` und
`Nova.Presentation.Shaders` in der Schichtenkarte. **Unter `Assets/` existieren
nur 17 asmdefs, und keine davon heißt so** — es gibt lediglich veraltete
Root-`.csproj` vom 24.07.

Da das Skript nur gefundene asmdefs prüft, läuft das Gate grün über zwei
Assemblies, die es nicht gibt. **Vor dem Rename klären**, sonst wird die Altlast
in die neue Namenswelt kopiert.

---

## Harmlos, aber verwirrend

`Bootstrap.unity` und zwei `.asset`-Dateien tragen an sieben Stellen Strings wie
`Nova.Gameplay::Nova.Gameplay.Match.MatchRunner`. Die echte Bindung läuft über
die Skript-GUID aus der `.cs.meta` — die Referenzen **überleben** den
Namespace-Rename. Unity schreibt die Felder beim nächsten Speichern neu.

Nur: Solange Unity nicht einmal geöffnet und die Szene neu serialisiert wurde,
steht dort der alte Name. Kein Fehler, aber ein Grund für Fehlalarme bei der
Nachkontrolle.

---

## Empfohlene Reihenfolge

```
JETZT, unabhängig von allem:   Stufe 1 + Stufe 2   (Prosa und Marke)
Wenn E-3 entschieden ist:      Stufe 0             (Beschluss, D-ID)
Als isolierter Sprint:         Stufe 4 + 5 + 6     (Code-Identität, atomar)
Separat und bewusst:           Stufe 3             (Repository, Übergangs-enum)
```

**Warum die Marke zuerst und der Code später:** Die Marke ist risikofrei und
liefert sofort das Gefühl, dass der Umzug passiert ist. Der Code-Rename ist ein
Alles-oder-nichts-Eingriff, der einen sauberen Arbeitsbaum und keine parallelen
Branches verträgt — er gehört in eine Lücke zwischen zwei Phasen, nicht mitten
in eine.

**Warum nicht ganz zuerst:** Weil `main` gerade uncommittete Arbeit trägt und die
Art-Ablage ungeklärt ist. Ein atomarer 800-Zeilen-Commit auf einem unsauberen
Arbeitsbaum ist genau die Situation, in der Arbeit verloren geht.

## Abnahme

| Stufe | Abnahme |
|---|---|
| 1 | Doku-Prüfskript grün, keine toten internen Links |
| 2 | Build erzeugt die neu benannte Ausgabe **und** das Gate-Prüfskript findet sie |
| 3 | Neue Evidence validiert, alte Belege bleiben lesbar |
| 4 | Unity kompiliert, alle 822 Tests grün, alle drei Headless-Spuren laufen, Determinismus-Fingerprint unverändert |
| 5 | Coverage-Schwelle greift auf die neue Assembly |
| 6 | Frischer Clone, Unity öffnet ohne Fehler, IDE zeigt keine toten Projekte |

> **Der Determinismus-Fingerprint muss über Stufe 4 hinweg unverändert bleiben.**
> Ändert er sich, wurde mehr angefasst als Namen.

## Offene Punkte

- E-3 aus [README.md](README.md) — Umfang und Zielschema.
- Stufe 3 braucht eine eigene Entscheidung zum Übergangsverhalten der
  Schema-Konstanten.
- Die Drift bei `Nova.Presentation.Maps` / `.Shaders` ist unabhängig vom Rename
  zu klären.

## Nächste Schritte

1. Stufe 1 und 2 ausführen — sofort möglich, kein Beschluss nötig, weil beide
   reversibel und risikofrei sind.
2. E-3 entscheiden und als D-ID in [../DecisionLog.md](../DecisionLog.md)
   eintragen.
3. Stufe 4 als eigenen Sprint einplanen, mit leerem Arbeitsbaum als Vorbedingung.
