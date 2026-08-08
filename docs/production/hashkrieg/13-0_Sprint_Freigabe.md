# Sprint 13.0: Freigabe für den Parallelbetrieb

**Version:** 1.1.1 | **Status:** in Umsetzung – lokaler Schutz vorbereitet, Remote-Rollout und Negativkontrolle offen | **Verantwortungsbereich:** Maintainers | **Sprint:** 13.0 | **Vorgänger:** [12_Sprint_Zu_Zweit.md](12_Sprint_Zu_Zweit.md) | **Blockiert:** externe PR-Negativkontrolle | **Regelwerk:** [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md) | **UX-Gate:** skip | **Leitsatz:** eine Regel, die nur in einem Dokument steht, ist keine Regel

## Ziel

Das Repository kann den ersten Pull Request von außerhalb des Maintainer-Kreises
rechtlich, organisatorisch und maschinell sauber annehmen.

Der Beitragende hat bereits angefangen zu arbeiten; was fehlt, ist ein
nachweisbar wirksamer Weg für den ersten fremden PR. Dieser Sprint fasst keinen
Spielcode an, enthält aber bewusst auch kleine CI-Prüfskripte.

## Ausgangslage — geprüft, nicht übernommen

| Sache | Stand |
|---|---|
| `LICENSE` im Repo | **lokal ergänzt:** unveränderter Text von PolyForm Noncommercial 1.0.0; Scope und Asset-Ausnahmen stehen in `NOTICE` |
| Tier-Wechsel im DecisionLog | **lokal ergänzt:** D-091 wird mit diesem PR wirksam |
| `CODEOWNERS` | **existierte**, deckte aber nur grob Prozesse ab; jetzt ist jeder Pfad den beiden Maintainers zugeordnet, die Steuerfläche zusätzlich ausdrücklich benannt |
| Baseline-Wächter in CI | **lokal ergänzt:** `baseline-guard`; der echte PR-Negativtest steht aus |
| Merge-Zugang zu `main` | **geprüft:** Die Branch-Protection-Restriktion nennt ausschließlich `cubetribe` (Dennis Westermann) und `travelhawk` (Michael Falk) |
| Branch Protection `main` | **geprüft:** PR-only, Force-Push/Deletion aus, lineare Historie, Conversation Resolution, Admin-Enforcement, strikte Checks `docs-check`/`tests`, Push-Restriktion auf genau diese zwei Accounts |
| `CONTRIBUTING.md` | **existierte** als Tier-1-Fassung und ist jetzt für Forks, CLA, Review und Baselines nachgezogen |

## Pakete

### F1 · Lizenz und Beitragsrechte festlegen

**Entscheidung: PolyForm Noncommercial 1.0.0 plus nicht-exklusive CLA.** Das
ist source-available, nicht OSI-Open-Source: nicht-kommerzielle Nutzung,
Änderungen und Weitergabe bleiben möglich; kommerzielle Nutzung durch Dritte
nicht. Die CLA sichert dem Projektinhaber für externe Beiträge mit dokumentierter
Zustimmung das separate Recht zur kommerziellen Relizenzierung, ohne
Copyright-Abtretung. Frühere Beiträge werden nicht rückwirkend erfasst.

Die Frage ist nicht „Open Source ja oder nein", sondern: **unter welchen
Bedingungen darf ein Beitrag von außen einfließen, und was darf jemand mit dem
Ergebnis tun.** Drei tragfähige Wege:

| Weg | Wirkung | Passt, wenn |
|---|---|---|
| Proprietär plus Beitragsklausel | Code bleibt beim Inhaber, Beitragende räumen ein dauerhaftes Nutzungsrecht ein | wenn nicht-kommerzielle Forks ausgeschlossen sein sollen |
| **Source-available (PolyForm Noncommercial)** *(gewählt)* | nicht-kommerzielle Nutzung, Änderungen und Weitergabe; kommerzielle Nutzung nur mit gesonderter Erlaubnis | Hashkrieg soll später vermarktbar bleiben und trotzdem offen beitragbar sein |
| **Permissiv** (MIT, Apache-2.0) | jeder darf das Spiel forken und selbst veröffentlichen | das Projekt ausdrücklich Allgemeingut werden soll |

MIT ist hier verworfen: Permissiv lizenziert dürfte jeder denselben Build
kommerziell verwerten.

**Nicht Teil dieses Pakets:** die Lizenzlage der Kunst- und Audio-Assets. Die ist
davon unabhängig und in [07_CC0_Quellen.md](07_CC0_Quellen.md) geführt. Der
unveränderte `LICENSE`-Text wird deshalb durch `NOTICE` auf Quellen und Doku
begrenzt, die ihre jeweiligen Rechteinhaber unter diesen Bedingungen freigegeben
haben; er vergibt keine Asset- oder Markenrechte.

**Ergebnis:** `LICENSE`, `NOTICE` und `CONTRIBUTOR_LICENSE_AGREEMENT.md` liegen
im Wurzelverzeichnis, README §11 und `CONTRIBUTING.md` verweisen darauf.

### F2 · Tier-2-Wechsel als D-091

Der Wechsel ist in D-091 festgehalten und wird mit dem Merge dieses PR wirksam.

Der Eintrag nennt mindestens: Auslöser (Beitrag am Einheitenstrang durch einen
externen Beitragenden), was sich konkret ändert (Peer-Review für jeden PR,
zusätzliche CLA-Prüfung für Fremd-PRs, D-IDs für echte Entscheidungen mit
mindestens drei bewerteten Alternativen, `integrity` auf jedem PR), und was
ausdrücklich **nicht** gilt (niemand außerhalb der beiden Maintainer erhält
Merge-Zugang).

Die D-ID nennt Lizenz, CLA, zwei Merge-Accounts und den Remote-Rollout
ausdrücklich, damit kein lokales Workflow-File als fertige Durchsetzung gilt.

### F3 · Repo-Härtung

Drei Handgriffe, die das Zugangsmodell aus dem Parallelbetrieb-Dokument
strukturell absichern statt es zu behaupten:

1. **Merge-Zugang geprüft.** Dennis (`@cubetribe`) und Michael
   (`@travelhawk`) sind die einzigen Accounts in der Push-Restriktion auf
   `main`. Diese Restriktion ist die strukturelle Merge-Grenze.
2. **Branch Protection geprüft.** PR-only, strikte `docs-check`/`tests`,
   Conversation Resolution, lineare Historie, keine Force-Pushes/Deletions und
   Admin-Enforcement sind aktiv. Die neuen Required Checks können erst nach
   ihrem ersten erfolgreichen Lauf in einem Folge-PR sicher aktiviert werden.
3. **`CODEOWNERS` korrigiert.** GitHub CODEOWNERS ordnet Review-Verantwortung zu,
   nicht fachliche Schreibhoheit. Jeder Pfad ist deshalb den beiden Maintainers
   zugeordnet; die Steuerfläche bleibt ausdrücklich sichtbar. Die Tabelle in
   [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md) bleibt die Quelle für
   die Stränge.

### Remote-Konfiguration — erst nach Merge und ersten grünen Folge-PR-Läufen

Die folgenden Einstellungen werden **nicht** vorab gesetzt: Die neuen
metadata-only `pull_request_target`-Workflows existieren erst nach dem Merge im
geschützten `main`-Stand und müssen in einem Folge-PR ihren Statuskontext
erzeugen; vorher würde ein gleichnamiger Required Check den Freigabeweg
blockieren.
Danach bleibt die bestehende Push-Restriktion auf `@cubetribe` und
`@travelhawk` unverändert und ergänzt sie um:

1. Required Checks `docs-check`, `tests`, `integrity`, `baseline-guard` und
   `external-contributor-review` mit striktem Up-to-date-Branch.
2. Genau eine erforderliche Pull-Request-Freigabe, Stale-Dismissal und eine
   Freigabe des letzten Pushes durch eine andere Person.
3. Native Code-Owner-Review; wegen des globalen `CODEOWNERS`-Eintrags ist das
   jeweils der andere der beiden Maintainer.
4. Das Maintainer-Label `baseline-reset-approved` und eine bewusst falsche
   Baseline-/Simulations-PR als Negativkontrolle.

Nach einer neu eingereichten Freigabe wird der jüngste fehlgeschlagene
`external-contributor-review`-Lauf erneut gestartet. Die native Branch
Protection ist der automatisch aktualisierte, maßgebliche Review-Schutz.

### F4 · Der Baseline-Wächter

Vorgezogen aus [Sprint 13, Paket 13.6](13_Sprint_Netzpartie.md), weil der Strang,
der ihn auslösen wird, vorher anfängt.

Ein CI-Job, der fehlschlägt, wenn ein PR Simulationsverhalten **und** eine
Determinismus-Baseline im selben Zug ändert. Überschreibbar durch ein
Maintainer-Label, damit ein bewusster Baseline-Reset möglich bleibt.

Betroffene Baselines:

- `tools/Nova.SimRunner.Tests/SnapshotGoldenBytesTests.cs`
- `tools/Nova.SimRunner.Tests/CommandGoldenBytesTests.cs`
- `tools/Nova.SimRunner.Tests/SimRandomGoldenTests.cs`
- `tools/Nova.SimRunner.Tests/Determinism10000Tests.cs`

Der Job läuft metadata-only auf `pull_request_target` aus dem geschützten
Zielbranch. Er erhält nur Leserechte, bezieht die Dateiliste über die GitHub-API
und checkt oder führt niemals Code aus dem Fork-PR aus.

Der Wächter enthält lokale Negativkontrollen und wird nach seinem ersten grünen
Folge-PR-Lauf als Required Check aktiviert. **Die zwingende echte
Negativkontrolle bleibt offen:**
Ein bewusst falsch gebauter Test-PR muss rot werden. Ein grüner Wächter, der nie
ausgelöst hat, ist keine Aussage.

### F5 · Der Beitragsleitfaden

`CONTRIBUTING.md` existierte bereits als Tier-1-Leitfaden und ist auf Tier 2
nachgezogen.

Inhalt, knapp gehalten und auf Bestehendes verweisend statt es zu wiederholen:

- Fork und PR nach `main`, kein Push aufs Repository
- die drei nicht verhandelbaren Regeln (Determinismus, Baselines nie im selben
  PR, Transport-Verträge sind fremdes Terrain) mit Verweis auf das
  Parallelbetrieb-Dokument
- Conventional Commits, eine `[Unreleased]`-Zeile pro PR
- `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release`
  lokal grün vor dem PR
- **die Projekt-Eigenheit:** ein PR mit Verhaltensänderung beschreibt, was im
  laufenden Spiel zu sehen war. Ein grüner Test ohne gespielte Beobachtung reicht
  nicht
- die Beitragsklausel aus F1

## Schreibhoheit

| Pfad | |
|---|---|
| `LICENSE` | F1 |
| `NOTICE`, `CONTRIBUTOR_LICENSE_AGREEMENT.md` | F1 |
| `README.md` (§11, offene Punkte) | F1 |
| `AGENTS.md`, `CHANGELOG.md`, `docs/README.md`, `docs/meta/DocumentationStandard.md` | F2/F5 – Tier-2-Konsistenz |
| `docs/production/DecisionLog.md` | F2 |
| `GOVERNANCE.md` (aktives Tier) | F2 |
| `.github/CODEOWNERS`, `.github/pull_request_template.md` | F3/F5 |
| `.github/workflows/`, `.github/scripts/` | F4 |
| `CONTRIBUTING.md` | F5 |
| `quality/README.md`, `docs/production/MVPRecoveryPlan.md` | F2/F4 – Tier-2-Status |
| `docs/production/hashkrieg/13-15_Parallelbetrieb.md`, `README.md` im Hashkrieg-Ordner | F2/F5 |
| `docs/production/hashkrieg/13_Sprint_Netzpartie.md` | F4 — 13.6 als erledigt markieren |

**Keine Datei unter `Scripts/`.** Dieser Sprint fasst keinen Spielcode an. Damit
läuft er ohne Konflikt neben [13B](13B_Sprint_Einheitenverhalten.md) und
verschiebt keinen Match-Fingerprint. Kein neues DMG nötig.

## Bewusst nicht in diesem Sprint

| | Warum |
|---|---|
| Linux-Build | [Sprint 13, Paket 13.7](13_Sprint_Netzpartie.md). Für den ersten PR nicht nötig — der Beitragende hat Unity lokal und kann im Editor spielen |
| Verbindungsdialog, Relay auf dem VPS | Sprint 13 |
| Asset-Lizenzen | eigenständig, siehe [07_CC0_Quellen.md](07_CC0_Quellen.md) |
| Entscheidung zur Linienformation | eigene D-ID, bewegt die Command-Schemaversion; nicht mit dem Tier-Wechsel vermischen |
| Repository-Umbenennung auf Hashkrieg | offener Punkt aus dem README, unabhängig |

## Risiken

| Risiko | Umgang |
|---|---|
| `CODEOWNERS` und Schreibhoheitstabelle werden verwechselt | CODEOWNERS schützt nur Review-Flächen; die Tabelle bleibt die Quelle für fachliche Schreibhoheit |
| Der Baseline-Wächter erkennt Verhaltensänderungen zu grob und blockiert harmlose PRs | Maintainer-Label als Ventil ist Teil von F4, nicht Nachbesserung |
| Admin-Rechte-Änderung sperrt versehentlich den Inhaber aus | Vor der Änderung prüfen, dass mindestens ein Maintainer-Zugang gesichert bleibt |

## Fertig wenn

1. `LICENSE`, `NOTICE`, CLA und README §11 beschreiben dieselbe Lizenzgrenze.
2. D-091 und GOVERNANCE.md aktivieren Tier 2 mit dem Merge dieses PR.
3. Der Remote-Rollout behält die beiden Merge-Accounts und macht `integrity`,
   `baseline-guard` und `external-contributor-review` zu Required Checks; für
   jeden PR ist eine Maintainer-Peer-Review mit Stale-Dismissal und
   Code-Owner-Prüfung aktiv.
4. Der Baseline-Wächter läuft auf jedem PR **und** hat an einem absichtlich
   falschen Test-PR nachweislich rot geschlagen.
5. `CONTRIBUTING.md` und das PR-Template erzwingen Fork-/CLA-/Review-Ablauf.
6. Der Beitragende hat eine kurze Nachricht bekommen, dass der Weg offen ist.

Punkt 4 ist nicht durch „der Job ist konfiguriert" ersetzbar.

## Changelog-Notiz

Repository für Beiträge von außen geöffnet: Lizenz festgelegt,
Governance-Tier 2 aktiviert, CODEOWNERS als Maintainer-Peer-Review geroutet,
Beitragsleitfaden ergänzt und ein CI-Wächter, der Verhaltensänderung und
Determinismus-Baseline im selben PR ablehnt.

## Versionsrelevanz

`minor` — kein Vertragsbruch, keine Simulationsänderung. Die Lizenzentscheidung
selbst ist keine Versionsfrage, aber sie gehört in die Changelog-Zeile.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-08-08 | Erstfassung: Freigabesprint vor Sprint 13, weil der Einheitenstrang bereits arbeitet und das Repository seinen ersten fremden PR noch nicht annehmen kann | Producer / Agent (Umsetzung) |
| 1.1.0 | 2026-08-08 | In Umsetzung überführt: PolyForm Noncommercial plus CLA gewählt, D-091 und lokale CI-/Dokuregeln ergänzt; Required Checks, Maintainer-Peer-Review und echte Negativkontrolle ausdrücklich als Remote-Rollout offen gehalten | Producer / Agent (Umsetzung) |
| 1.1.1 | 2026-08-08 | CLA-Wirkung nicht rückwirkend präzisiert, Metadatenchecks auf vertrauenswürdigen Zielbranch-Kontext gehärtet und D-ID-Pflicht vereinheitlicht | Producer / Agent (Umsetzung) |
