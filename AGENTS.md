# AGENTS.md – Arbeitsregeln für KI-Agenten & Mitwirkende

**Dokumentversion:** 5.0.0 | **Status:** verbindlich | **Verantwortungsbereich:** Project Owner | **Sprint:** 16 | **Governance-Tier:** 2 ([GOVERNANCE.md](GOVERNANCE.md))

Verbindliche Betriebsanleitung für jeden KI-Coding-Agenten (Claude, Kimi, Codex,
Cursor u. a.) und jede Person, die an *Project Nova* arbeitet. **Lies diese Datei
zuerst.** Sie wird von den gängigen Agenten-Tools automatisch als Kontext geladen.

Welche Regeln in welcher Projektphase gelten, steht in [GOVERNANCE.md](GOVERNANCE.md).
Aktiv ist **Tier 2: externe Beitragende, ein Projektinhaber.** Dennis Westermann
(`@cubetribe`) ist alleiniger Projektinhaber, Maintainer und Mergeberechtigter.

## 1. Projekt in einem Absatz

*Project Nova* ist ein Echtzeitstrategiespiel auf **Unity `6000.5.4f1`**
(Revision `d550df8bd089`), C# und URP. Die Simulation ist deterministisch und
liegt unter `Assets/_Project/Scripts/{Core,Simulation}`; dieselben Quellen
kompilieren headless in `tools/Nova.SimRunner`. Spielbar ist ein lokales 1v1 auf
der Glutrinne-Graybox – Ablauf und ehrliche Grenzen stehen im Demo-Runbook
(`docs/production/DemoRunbook.md`, kommt mit dem Demo-Prep-Strang). Das
strukturierte Wiki liegt unter [`docs/`](docs/).

## 2. Goldene Regeln (nicht verhandelbar)

1. **`main` ist geschützt – Änderungen nur über Pull Requests.** Kurzer
   Topic-Branch → PR → grüne CI → Merge. Keine direkten Pushes auf `main`.
2. **Niemals `main` force-pushen**, keine History-Rewrites auf geteilten Branches.
3. **Keine Secrets ins Repo** – keine Tokens, Keys, `.env`-Inhalte, Passwörter,
   auch nicht in Beispielen oder Commit-Messages.
4. **Agenten committen und pushen nur nach ausdrücklicher Anfrage für die
   jeweilige Aktion.** Commit, Push, Merge und Release sind getrennte
   Autoritätsgrenzen. Eine Freigabe gilt nie für den nächsten Schritt mit.
5. **Nichts als fertig melden, was nicht gelaufen ist.** Dateien, Typen und
   isolierte Unit-Tests sind kein Fertigstellungsnachweis. Wenn du etwas nicht
   ausgeführt hast, sag das. Das ist die wichtigste Regel im Repo – sie entstand
   aus einem realen Befund (siehe [GOVERNANCE.md](GOVERNANCE.md)).
6. **Entscheiden statt raten – und eskalieren.** Bei echten Design- oder
   Architekturalternativen nicht eigenmächtig entscheiden, sondern Optionen samt
   Empfehlung vorlegen. Getroffene Entscheidungen kommen als D-ID ins
   [DecisionLog](docs/production/DecisionLog.md).
7. **Kleine, fokussierte Änderungen.** Ein Commit = eine logische Änderung.

## 3. Definition of Done

Eine Änderung ist fertig, wenn diese vier Punkte stimmen:

- [ ] **Tests grün** – `dotnet test tools/Nova.SimRunner.Tests` lokal, CI bestätigt es
- [ ] **Eintrag unter `[Unreleased]`** in [CHANGELOG.md](CHANGELOG.md) – einer pro PR genügt
- [ ] **Sauberer Conventional Commit**
- [ ] **Als PR eingebracht**, CI grün

Zusätzlich, wenn zutreffend:

- Echte Entscheidung getroffen? → D-ID im [DecisionLog](docs/production/DecisionLog.md)
- Spielverhalten geändert? → im laufenden Spiel ansehen und im PR beschreiben;
  der Projektinhaber darf diese Abnahme ausdrücklich zurückstellen, wenn der PR
  stattdessen „nicht gespielt“, Grund, automatisierte Ersatznachweise und
  Restrisiko nennt. Der PR ist dann mergebar, aber nicht spielerisch abgenommen
  und kein Meilenstein-Nachweis.
- Neues oder entferntes Dokument? → [docs/README.md](docs/README.md)-Index nachziehen
- Inhaber-PR? → Selbst-Merge erst nach grüner Pflicht-CI und dokumentiertem,
  unabhängigem Read-only-Review.
- Externer PR? → Freigabe des Projektinhabers auf dem aktuellen Head und
  bestätigte [Contributor License Agreement](CONTRIBUTOR_LICENSE_AGREEMENT.md)
- Vertrag oder öffentliche Doku? → Kopfversion und Änderungsverlauf pflegen

Weiterhin nicht verlangt: Gate-Evidence, Receipt-Ketten und Performance-Evidenz
mit `environmentId`-Bindung. Sie wachen erst mit Tier 3 auf.

## 4. Tests und Verifikation

```bash
# Simulationstests (rund 400, ~8 s, keine Unity-Lizenz nötig) – der kanonische Check
dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release
```

Das Repository bringt eine lokale SDK unter `.dotnet/` mit. Falls `dotnet` nicht
im PATH liegt:

```bash
export DOTNET_ROOT="$PWD/.dotnet"; export PATH="$DOTNET_ROOT:$PATH"
```

Die CI führt bei jedem PR die Simulationstests aus. Unity-EditMode-Tests laufen
mangels CI-Lizenz nur lokal – wer die Präsentationsschicht anfasst, führt sie aus
und schreibt das Ergebnis in den PR.

## 5. Repository-Struktur

```
GOVERNANCE.md        ← welche Regeln in welcher Phase gelten
AGENTS.md            ← diese Datei
CHANGELOG.md         ← Änderungshistorie (Keep a Changelog)
Assets/_Project/     ← Unity-Projekt; Scripts/{Core,Simulation} = deterministische Sim
tools/               ← headless SimRunner, Testprojekt, Coverage
quality/             ← Gate-Apparat, schlafend bis Tier 3 (siehe quality/README.md)
docs/
├── README.md        ← Wiki-Index – bei neuen/entfernten Dokumenten aktualisieren
├── vision/ gamedesign/   ← GDD
├── tech/            ← Technical Design, Modulspezifikationen
├── production/      ← Roadmap, DecisionLog, ScopeLedger, DemoRunbook
├── assets/          ← Art-Standard, Manifest, Provenienz
├── analysis/ research/   ← abgeschlossene Sprints 0–1
└── meta/            ← Dokumentationsstandard
```

**„Heiße" Dateien – ein Schreiber pro Änderung, nie parallel bearbeiten:**
`CHANGELOG.md`, `docs/README.md`, `docs/production/DecisionLog.md`,
`docs/production/ScopeLedger.md`.

Wer parallel arbeitet (Mensch oder Agent), nennt vorher seinen Schreibumfang.
Überlappende Schreibumfänge laufen nacheinander oder in getrennten Worktrees.

## 6. Doku-Regeln (Kurzfassung)

Verbindlich ist [docs/meta/DocumentationStandard.md](docs/meta/DocumentationStandard.md).
Das Wichtigste:

- **Sprache:** Deutsch für Projektinhalte, Englisch für Code, Identifier, Pfade.
- **Ein Dokument = ein Thema**, Abhängigkeiten relativ verlinken.
- **Keine toten internen Links** – die CI prüft das hart.
- **Entscheidungen** bekommen fortlaufende D-IDs, bleiben bei Revision stehen
  (Status „ersetzt durch D-xxx"), keine stillen Umschreibungen.
- Verträge und öffentliche Doku führen eine Kopfversion und einen
  Änderungsverlauf; für interne Arbeitsnotizen bleibt Git der Verlauf. Ausnahme
  mit Maschinenvertrag: `quality/content/mvp-v1.json` bleibt versioniert.

## 7. Git-Konventionen

**Branches:** kurze Topic-Branches, Präfixe `feat/`, `fix/`, `docs/`, `chore/`,
`refactor/`, `codex/`. Kein dauerhafter Integrationsbranch. Squash-Merge, lineare
Historie, Branch nach dem Merge löschen.

**Commits:** `type(scope): kurze Beschreibung im Imperativ`, Englisch, ≤ 72 Zeichen.
Typen: `feat` · `fix` · `docs` · `refactor` · `chore` · `test` · `perf` · `build` · `ci`.
Der Body erklärt das **Warum** und referenziert D-IDs.

```
feat(economy): let harvesters cycle without a manual return order
fix(pathfinding): pin CostField epoch when flow fields are cached
docs(production): log D-076 governance tier model
```

Keine „wip", „stuff", „fix" ohne Kontext, keine Debug-Reste.

**Pull Requests:** Titel im Conventional-Commit-Stil. Beschreibung nennt Was,
Warum, betroffene Bereiche und den Changelog-Eintrag. Details:
[CONTRIBUTING.md](CONTRIBUTING.md).

## 8. Befehls-Spickzettel

```bash
git switch -c feat/<thema>              # neuer Arbeits-Branch
dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release
python3 .github/scripts/check_docs.py   # tote interne Links finden
git push -u origin <branch>             # nur nach expliziter Anfrage; NIE auf main
gh pr create --fill --base main
```

---

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-21 | Initiale Agenten-Arbeitsregeln | Orchestrator |
| 2.0.0 | 2026-07-21 | Auf PR-only umgestellt, `main` per Branch Protection gesperrt | Orchestrator |
| 3.0.0 | 2026-07-24 | D-059/D-060/D-061: Recovery-Status, Unity-Pin, kurze Topic-Branches, per-action Agentenautorität, Quality-Evidence-Regeln | Orchestrator |
| 3.1.0–3.7.0 | 2026-07-24 – 2026-07-26 | Ausbau des Gate-Evidenzregimes (D-062 bis D-067) | Orchestrator |
| 4.0.0 | 2026-08-06 | D-076: auf Governance-Tier 1 zurückgeschnitten. Gate-Kette, Receipt-Verträge und Evidenzpflicht schlafen gelegt; DoD von 13 auf 4 Punkte; Doku-Ritual freiwillig; Sprint-Ritual entfernt; `dotnet test` als kanonischer CI-Check verankert | Orchestrator |
| 4.1.0 | 2026-08-08 | D-091: Governance-Tier 2 für externe Beiträge aktiviert; Maintainer-Review/CLA für fremde PRs und Versionspflicht für Verträge/öffentliche Doku ergänzt | Dennis Westermann / Michael Falk |
| 5.0.0 | 2026-08-10 | D-105: Dennis Westermann als alleinigen Projektinhaber, Maintainer, Tier-Entscheider und Mergeberechtigten festgelegt; Selbst-Merge bei grüner Pflicht-CI und unabhängigem Review sowie ehrliche Spielabnahme-Zurückstellung geregelt | Dennis Westermann |
