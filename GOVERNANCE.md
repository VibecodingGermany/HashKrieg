# Governance – Project Nova

**Version:** 3.0.0 | **Status:** verbindlich; Tier 2 aktiv | **Verantwortungsbereich:** Project Owner | **Sprint:** 16

## Zweck

**Aktives Tier: 2 — externe Beitragende, ein Projektinhaber**

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
| **Lage** | kleiner interner Kreis, kein Publikum | fremde Beitragende | Veröffentlichung, Geld, Nutzer |
| **Auslöser** | keine fremden Beiträge | erster PR von außerhalb des Maintainer-Kreises | Steam-Seite, bezahlter Build, Publisher-Vertrag, oder Nutzerdaten im Spiel |
| **Vertrauensmodell** | wir vertrauen einander | wir vertrauen dem Code, nicht jedem Absender | wir müssen es Dritten beweisen können |

Ein Tier-Wechsel ist eine Inhaberentscheidung und wird als D-ID im
[DecisionLog](docs/production/DecisionLog.md) festgehalten.

## Entscheidungs- und Merge-Autorität

Dennis Westermann (`@cubetribe`) ist nach D-105 alleiniger Projektinhaber,
Maintainer, Tier-Entscheider und Mergeberechtigter. Michael Falk
(`@travelhawk`) hat keine Governance- oder Maintainer-Rolle mehr in diesem
Projekt. Seine Organisationsmitgliedschaft und historische Urheberschaft
bleiben davon unberührt und begründen keine Projektentscheidungskompetenz.

Der Inhaber darf eigene PRs nach grüner Pflicht-CI und dokumentiertem,
unabhängigem Read-only-Review selbst mergen. Externe PRs brauchen weiterhin die
CLA-Zustimmung und seine `APPROVED`-Review auf dem aktuellen Head. Auch unter
Tier 3 entscheidet der Inhaber allein; technische Prüfung und Gate-Evidenz sind
Nachweise, keine zweite Entscheidungsstimme.

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
| Merge | Inhaber-Selbst-Merge bei grüner Pflicht-CI | ebenso; extern zusätzlich CLA und Inhaberfreigabe auf aktuellem Head | Inhaberentscheidung; aktivierte Tier-3-Gates bleiben Pflicht |
| Unabhängiges Read-only-Review für Inhaber-PRs | ✅ | ✅ | ✅ |
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

Kein Receipt, kein Hash, keine Kette. Punkt 2 ist für den Meilenstein nicht
optional: Er ist genau die Prüfung, die F-001 gefunden hätte.

Für einen einzelnen PR darf der Projektinhaber die manuelle Spielabnahme
ausdrücklich zurückstellen. Der PR nennt dann sichtbar **„nicht gespielt“**, den
Grund, die gelaufenen automatisierten Ersatznachweise und das Restrisiko. Er darf
mit grüner Pflicht-CI gemergt werden, gilt aber weder als spielerisch abgenommen
noch als Nachweis für Punkt 2. Eine spätere gemeinsame Spielrunde kann mehrere
solche PRs abnehmen. Aktivierte Tier-3-Evidenz wird durch diese Regel nicht
erlassen, und „nicht gespielt“ darf niemals als „gespielt“ ausgegeben werden.

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
2. Das geschützte Environment `quality-gate` mit mindestens einem von der
   Authorize-Ausführung unabhängigen Required Reviewer anlegen. Diese Freigabe
   bestätigt technische Evidenz und ist keine Governance-Mitentscheidung.
3. [`docs/production/MVPRecoveryPlan.md`](docs/production/MVPRecoveryPlan.md)
   wieder als führend für Meilensteinstatus erklären.

Der Detailvertrag steht unverändert in
[`quality/README.md`](quality/README.md) und im MVP-Recovery-Plan.

## Verwandte Dokumente

- [AGENTS.md](AGENTS.md) – Arbeitsregeln für Agenten und Menschen
- [CONTRIBUTING.md](CONTRIBUTING.md) – Branch-, PR- und Review-Ablauf
- [docs/meta/DocumentationStandard.md](docs/meta/DocumentationStandard.md) – Doku-Regeln
- [docs/production/DecisionLog.md](docs/production/DecisionLog.md) – D-076 begründet das Tier-Modell; D-091 aktiviert Tier 2; D-105 regelt die alleinige Projektleitung

## Offene Punkte

- `integrity`, `baseline-guard` und `external-contributor-review` laufen weiter,
  sind aber noch nicht als Required Checks in der Branch Protection hinterlegt.
- Die verpflichtende Negativkontrolle des Baseline-Wächters bleibt offen.

## Nächste Schritte

1. Die Main-Restriktion und Review-Routen auf `@cubetribe` als alleinigen
   Projektinhaber synchronisieren, ohne Pflichtchecks zu lockern.
2. Den Baseline-Wächter mit einem absichtlich falschen PR negativ prüfen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 2.1.0 | 2026-08-08 | D-091: Tier 2, externe Beitragsregeln und den noch ausstehenden Remote-Rollout dokumentiert | Dennis Westermann / Michael Falk |
| 2.1.1 | 2026-08-08 | D-ID-Pflicht in allen Tiers auf echte Architektur-, Design- und Prozessentscheidungen präzisiert | Dennis Westermann |
| 3.0.0 | 2026-08-10 | D-105: alleinige Projektleitung und Tier-Entscheidung durch Dennis Westermann, grünen Inhaber-Selbst-Merge sowie ehrliche Zurückstellung manueller Spielabnahmen geregelt; PR-only und Tier-3-Evidenzpflicht bleiben bestehen | Dennis Westermann |
