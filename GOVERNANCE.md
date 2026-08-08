# Governance – Project Nova

**Version:** 2.1.1 | **Status:** verbindlich ab Merge des Sprint-13.0-PR; Tier-2-Remote-Rollout offen | **Verantwortungsbereich:** Maintainers | **Sprint:** 13.0

## Zweck

**Aktives Tier: 2 — externe Beitragende, zwei Maintainer**

Dieses Repository regelt seine Prozessstrenge über **Tiers** statt über eine feste
Regelmenge. Jede Regel gehört zu genau einem Tier. Regeln höherer Tiers werden
nicht gelöscht, sondern **schlafen gelegt** – zusammen mit dem Auslöser, der sie
weckt. Hochskalieren heißt dann: Tier wechseln, nicht neu erfinden.

## Abhängigkeiten

- [AGENTS.md](AGENTS.md) – Arbeitsregeln für Agenten und Menschen
- [CONTRIBUTING.md](CONTRIBUTING.md) – Branch-, PR- und Review-Ablauf
- [docs/production/DecisionLog.md](docs/production/DecisionLog.md) – Tier-Entscheidungen D-076 und D-091

## Warum es Tiers gibt

Am 24. Juli 2026 stellte ein [Implementierungs-Audit](docs/production/ImplementationAudit_2026-07-24.md)
fest, dass als fertig gemeldete Module nicht funktionierten: `SimulationKernel.SubmitCommand()`
nahm Commands entgegen und verwarf sie, und der zugehörige Test umging den Kernel.
Das ist das typische Versagensmuster KI-gestützter Entwicklung – Scaffolding wird
als Feature gemeldet.

Die Antwort darauf war ein Evidenzregime mit kryptografischen Gate-Receipts
(G0–G5, rund 6.300 Zeilen Prüf-Tooling). Der Instinkt war richtig. Die Umsetzung
passte zu einem Projekt mit fremden Beitragenden, Nutzern und Haftung. Für zwei
Leute, die sich kennen, war sie der Blocker:

- In 148 Commits erzeugte der Apparat **kein einziges** Evidence-Artefakt
  (`quality/evidence/` und `quality/authorizations/` existierten nie).
- MS-0 und MS-1 galten per Definition als unerreichbar, weil sie nur über Gates
  erreichbar waren und G0 auf einem ungebauten Receipt-Vertrag stand.
- Die CI prüfte Markdown-Links und die Selbsttests des Evidence-Validators – aber
  **nie die Simulationstests des Spiels**.
- Die Arbeit, die das Spiel tatsächlich spielbar machte, lief unter D-067, einer
  Ausnahme, die nie ratifiziert wurde.

Tier 1 behält die Absicht und wirft die Zeremonie weg. **Der Ersatz für
kryptografische Evidenz lautet: Die Tests laufen in CI, und ein Mensch hat das
Spiel gespielt.** Das ist billiger, ehrlicher und fängt genau den Fehler, um den
es ging – denn ein Modul, das nur auf dem Papier existiert, überlebt keine
Spielrunde.

## Die Tiers

| | Tier 1 | Tier 2 (aktiv) | Tier 3 |
|---|---|---|---|
| **Lage** | zwei Entwickler, kein Publikum | fremde Beitragende | Veröffentlichung, Geld, Nutzer |
| **Auslöser** | Status quo | erster PR von außerhalb des Maintainer-Kreises, oder mehr als zwei aktive Maintainer | Steam-Seite, bezahlter Build, Publisher-Vertrag, oder Nutzerdaten im Spiel |
| **Vertrauensmodell** | wir vertrauen einander | wir vertrauen dem Code, nicht jedem Absender | wir müssen es Dritten beweisen können |

Ein Tier-Wechsel ist eine Inhaberentscheidung und wird als D-ID im
[DecisionLog](docs/production/DecisionLog.md) festgehalten.

## Was in welchem Tier gilt

| Mechanismus | Tier 1 | Tier 2 (jetzt) | ab Tier 3 |
|---|---|---|---|
| `main` geschützt, PR-only | ✅ | ✅ | ✅ |
| Conventional Commits | ✅ | ✅ | ✅ |
| Keine Secrets im Repo | ✅ | ✅ | ✅ |
| Agenten committen/pushen nur auf ausdrückliche Anfrage | ✅ | ✅ | ✅ |
| Hot-Files / Schreibhoheit bei paralleler Arbeit | ✅ | ✅ | ✅ |
| **CI: `dotnet test` (Simulationstests)** | ✅ | ✅ | ✅ |
| CI: tote interne Doku-Links | ✅ | ✅ | ✅ |
| CHANGELOG-Eintrag | pro PR | pro PR | pro PR |
| DecisionLog D-IDs | bei echten Architektur-, Design- oder Prozessentscheidungen | ebenso | ebenso |
| Merge | Selbst-Merge erlaubt | jeder PR braucht eine Freigabe des anderen Maintainers; extern zusätzlich CLA | zwei Freigaben |
| Doku: Pflichtaufbau, Versionsbump, Änderungsverlauf | nur `quality/content/mvp-v1.json` | für Verträge und öffentliche Doku | überall |
| DecisionLog: ≥3 Alternativen je Entscheidung | ✖ | ✅ | ✅ |
| Sprint-Ritual (8 Pflichtschritte) | ✖ | ✖ | ✅ |
| `integrity`-Job auf jedem PR | ✖ (nur bei `quality/**`) | ✅ | ✅ |
| **Gate-Kette G0–G5 als Fortschrittsblocker** | 💤 | 💤 | ✅ |
| **G0-A2-Receipt-Vertrag, Trusted Tooling, Protected Environment** | 💤 | 💤 | ✅ |
| Evidenzpflicht, `environmentId`-Bindung, 3×120-s-Perfläufe | 💤 | 💤 | ✅ |

✅ gilt · ✖ gilt nicht · 💤 schlafend, Code bleibt im Repo

## Was „fertig" in Tier 1 und 2 heißt

Ein Meilenstein ist erreicht, wenn **beides** zutrifft:

1. `dotnet test` ist grün (läuft in CI auf jedem PR), und
2. ein Mensch hat die betroffene Sache im laufenden Spiel gesehen und es
   notiert – im PR oder im [GrayboxLog](docs/production/GrayboxLog.md). Den
   Ablauf dafür beschreibt das Demo-Runbook (`docs/production/DemoRunbook.md`,
   kommt mit dem Demo-Prep-Strang).

Kein Receipt, kein Hash, keine Kette. Punkt 2 ist nicht optional: Er ist genau
die Prüfung, die F-001 gefunden hätte.

Was das Spiel gegenüber dem MS-1-Sollinhalt noch schuldig bleibt, steht ehrlich
im [ScopeLedger](docs/production/ScopeLedger.md). Der bleibt – als Lückenliste,
nicht als Gate-Buchhaltung.

## Was schläft und wie es aufwacht

Der Gate-Apparat bleibt vollständig im Repository unter [`quality/`](quality/):
Schemata, Validator, Szenarien, Manifest und der Authorize-Pfad in
[`.github/workflows/quality-gate.yml`](.github/workflows/quality-gate.yml).
Nichts davon ist gelöscht. Seit D-091 läuft der `integrity`-Job auf jedem PR –
der Apparat verrottet also nicht unbemerkt, während externe Beiträge eingehen.

Zum Aufwecken bei Tier 3:

1. Tier-Wechsel als D-ID entscheiden.
2. Das geschützte Environment `quality-gate` mit Required Reviewers anlegen.
3. [`docs/production/MVPRecoveryPlan.md`](docs/production/MVPRecoveryPlan.md)
   wieder als führend für Meilensteinstatus erklären.

Der Detailvertrag steht unverändert in
[`quality/README.md`](quality/README.md) und im MVP-Recovery-Plan.

## Verwandte Dokumente

- [AGENTS.md](AGENTS.md) – Arbeitsregeln für Agenten und Menschen
- [CONTRIBUTING.md](CONTRIBUTING.md) – Branch-, PR- und Review-Ablauf
- [docs/meta/DocumentationStandard.md](docs/meta/DocumentationStandard.md) – Doku-Regeln
- [docs/production/DecisionLog.md](docs/production/DecisionLog.md) – D-076 begründet das Tier-Modell; D-091 aktiviert Tier 2

## Offene Punkte

- Nach dem Merge müssen `integrity`, `baseline-guard` und
  `external-contributor-review` nach ihren ersten erfolgreichen Läufen in
  Folge-PRs als Required Checks in der Branch Protection hinterlegt werden.
- Der Remote-Rollout setzt außerdem eine erforderliche, stale-dismissed
  Maintainer-Freigabe auf dem aktuellen Head, native Code-Owner-Reviews und das
  `baseline-reset-approved`-Label.

## Nächste Schritte

1. Sprint 13.0 als PR einbringen und die neue CI auf dem geschützten Zielbranch
   ausführen lassen.
2. Danach die offene Remote-Konfiguration setzen und mit einem absichtlich
   falschen Baseline-PR negativ prüfen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 2.1.0 | 2026-08-08 | D-091: Tier 2, externe Beitragsregeln und den noch ausstehenden Remote-Rollout dokumentiert | Dennis Westermann / Michael Falk |
| 2.1.1 | 2026-08-08 | D-ID-Pflicht in allen Tiers auf echte Architektur-, Design- und Prozessentscheidungen präzisiert | Dennis Westermann |
