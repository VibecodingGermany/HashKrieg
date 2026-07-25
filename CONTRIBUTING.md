# Beitragen zu Project Nova

**Version:** 2.4.0 | **Status:** verbindlich | **Verantwortungsbereich:** Maintainers | **Sprint:** 7

## Zweck

Definiert den Branch-, PR-, Review- und Release-Ablauf für Menschen und
KI-Agenten. Detailregeln stehen in [AGENTS.md](AGENTS.md), Dokumentregeln in
[DocumentationStandard.md](docs/meta/DocumentationStandard.md).

## Abhängigkeiten

- [AGENTS.md](AGENTS.md)
- [DecisionLog D-059 und D-064](docs/production/DecisionLog.md)
- [MVPRecoveryPlan.md](docs/production/MVPRecoveryPlan.md)
- [PR-Vorlage](.github/pull_request_template.md)

## 1. Branch-Modell

`main` ist geschützt und PR-only. Es gibt keinen dauerhaften
Integrationsbranch. Zulässige kurze Topic-Branches:

- `feat/<thema>`
- `fix/<thema>`
- `docs/<thema>`
- `chore/<thema>`
- `refactor/<thema>`
- `codex/<thema>`

Branches werden nach Squash-Merge gelöscht; die Historie auf `main` bleibt
linear. Keine direkten Pushes oder Force-Pushes auf `main`, keine
History-Rewrites auf geteilten Branches und keine langlebigen Recovery-Branches.

## 2. Ablauf

1. Aktuelles `main` holen und kurzen Topic-Branch anlegen.
2. Kleine, fokussierte Änderung mit passenden Tests/Dokumentation erstellen.
3. Dokumentversion, Änderungsverlauf und `[Unreleased]` pflegen.
4. Conventional Commit vorbereiten.
5. Branch pushen und PR nach `main` öffnen.
6. Pflichtchecks und unabhängiges Review abwarten.
7. Squash-Merge bei grünen Checks und Freigabe.

Commit, Push, Merge, Release und Tag sind getrennte Autoritätsgrenzen.
KI-Agenten committen oder pushen nur nach einer **ausdrücklichen Anfrage für
die konkrete Aktion**.

## 3. Checks

Pflicht ist:

- `docs-check` und
- nach realer G0-Implementierung `quality-gate`.

Docs-only-PRs deklarieren ihren Scope explizit; das aggregierte
`quality-gate` wird nicht übersprungen. `docs-check` läuft auch für
`quality/**` und installiert die gepinnten Ajv-Abhängigkeiten. Dieses
Rebaseline erzeugt noch keinen Code-CI-/Trust-Workflow und keine
Evidence-Platzhalter.

Schema 1.2 ist nur eine Integritätsvorstufe. Jeder Pass-Versuch muss aktuell
zusätzlich mit `E_AUTHORIZATION_BOOTSTRAP` fehlschlagen. G0-A etabliert zuerst
Schema 1.3 und das subject-unabhängige Trusted Tooling; die Bootstrap-Änderung
wird ohne Gate-Fortschritt gemergt und darf erst einen nachfolgenden sauberen
Subject-Commit prüfen. Danach folgt G0-B.

## 4. Reviews

Im Solo-/KI-Modus ersetzt ein unabhängiges, read-only Review die unmögliche
Autoren-Selbstfreigabe. Der Reviewer ist nicht der Implementation Writer und
reproduziert mindestens einen kanonischen Check als eigene artefaktgebundene
Ausführung. Ein lokales Evidence-Dokument autorisiert keinen Pass; dafür ist
nach G0-A der externe Trust-Kontext des subject-unabhängigen, unveränderten
Trusted-Tool-Bundles erforderlich. Er bindet die vollständige geordnete
`authorizedEvidence`-Kette von G0 bis zum aktuellen Gate.

Sobald mindestens zwei aktive menschliche Maintainer existieren, wird eine
zweite menschliche Freigabe zwingend. CODEOWNERS und Branch Protection dürfen
diese Regel verschärfen.

## 5. Commit-Konvention

Format: `type(scope): imperative summary`, Englisch, höchstens 72 Zeichen.
Typen: `feat`, `fix`, `docs`, `refactor`, `chore`, `test`, `perf`, `build`,
`ci`.

Ein Commit entspricht einer logischen Änderung. Keine Secrets, generierten
Binärdateien oder Debug-Artefakte einchecken.

## 6. Pull Requests

Die Beschreibung nennt:

- Was und warum,
- betroffene Bereiche,
- neue/geänderte D-IDs,
- Changelog-Eintrag,
- ausgeführte Checks und
- bei Gate-Behauptungen den Evidence-Pfad.

Schema 1.2 autorisiert keinen Gate-Pass. Ein PR darf ein Gate erst dann als
bestanden bezeichnen, wenn ein nachfolgender sauberer Subject-Commit mit
Schema 1.3 aus dem Trusted-Tool-Checkout geprüft wurde und sein externer
Trust-Kontext die vollständige geordnete `authorizedEvidence`-Kette samt
Subject-, CI- und Review-Bindung belegt. Eine Trust-Bundle-Änderung darf sich
nicht selbst autorisieren. Performance-Command und -Messung müssen dieselbe
`environmentId` referenzieren; Windows-x64-Referenz und Mac-M2-Funktionslauf
verwenden getrennte Methodenprofile.

## 7. Releases

Nur ein Maintainer darf nach expliziter Freigabe Tag/Release erzeugen.
Wiki-Versionen sind nicht automatisch Game-Releases. Aktuell ist 0.11.0 ein
unveröffentlichter Dokumentationsstand; G0, MS-0 und MS-1 sind offen.

## Offene Punkte

- `quality-gate` und Schema 1.3 werden zuerst in G0-A implementiert und sind
  bis zum Beweis an einem nachfolgenden sauberen Subject keine behauptete
  vorhandene Gate-Autorität.

## Nächste Schritte

1. Bestehenden `docs-check` für dieses Rebaseline verwenden.
2. G0-A einschließlich Gate-Runner als nicht selbstautorisierende Bootstrap-
   Änderung geschützt mergen.
3. G0-B am nachfolgenden sauberen Subject herstellen und dieses erst danach
   mit dem bereits gemergten Schema-1.3-Trustpfad autorisieren; anschließend
   die Branch-Protection gegen D-059 prüfen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-21 | PR-only-Community-Workflow eingeführt | Maintainers |
| 2.0.0 | 2026-07-24 | D-059: kurze Topic-Branches, kein dauerhafter Integrationsbranch, per-action Agentenautorität und gestuftes Review/quality-gate festgelegt | Maintainers |
| 2.1.0 | 2026-07-24 | Semantikvalidierte Gate-Evidence und Wiki-Stand 0.8.1 verankert | Maintainers |
| 2.1.1 | 2026-07-24 | Unveröffentlichten Wiki-Stand auf 0.8.2 fortgeschrieben | Maintainers |
| 2.2.0 | 2026-07-24 | D-062-Same-Subject-Gate-Kette, artefaktgebundene Szenarioschwellen und Wiki-Stand 0.9.0 als PR-Pflicht ergänzt | Maintainers |
| 2.3.0 | 2026-07-24 | D-063-Schema 1.2, Check-Artefakte, Protected-CI-Trust und Wiki-Stand 0.10.0 als PR-Pflicht ergänzt | Maintainers |
| 2.4.0 | 2026-07-24 | D-064-Fail-Closed-Schema 1.2, zweistufigen Trusted-Gate-Bootstrap und Wiki-Stand 0.11.0 als PR-Pflicht ergänzt | Maintainers |
