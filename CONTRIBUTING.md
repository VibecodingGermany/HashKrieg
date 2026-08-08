# Beitragen zu Project Nova

**Version:** 4.0.1 | **Status:** verbindlich ab Merge des Sprint-13.0-PR | **Verantwortungsbereich:** Maintainers | **Sprint:** 13.0 | **Governance-Tier:** 2 ([GOVERNANCE.md](GOVERNANCE.md))

Branch-, PR- und Review-Ablauf für Menschen und KI-Agenten. Detailregeln stehen
in [AGENTS.md](AGENTS.md), Dokumentregeln in
[DocumentationStandard.md](docs/meta/DocumentationStandard.md).

## 1. Branch-Modell

`main` ist geschützt und PR-only. Es gibt keinen dauerhaften Integrationsbranch.
Zulässige kurze Topic-Branches: `feat/`, `fix/`, `docs/`, `chore/`, `refactor/`,
`codex/`.

Squash-Merge, lineare Historie, Branch danach löschen. Keine direkten Pushes oder
Force-Pushes auf `main`, keine History-Rewrites auf geteilten Branches.

## 2. Ablauf

1. Aktuelles `main` holen, kurzen Topic-Branch anlegen.
2. Kleine, fokussierte Änderung mit passenden Tests bauen.
3. `dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release`
   lokal grün bekommen.
4. Zeile unter `[Unreleased]` in [CHANGELOG.md](CHANGELOG.md) ergänzen.
5. Conventional Commit, Branch pushen, PR nach `main` öffnen.
6. CI abwarten, mergen.

Commit, Push, Merge und Release sind getrennte Autoritätsgrenzen. **KI-Agenten
committen oder pushen nur nach einer ausdrücklichen Anfrage für die konkrete
Aktion.**

### Parallelbetrieb Sprint 13–15

Für parallele Beiträge gelten drei Grenzen aus
[13-15_Parallelbetrieb.md](docs/production/hashkrieg/13-15_Parallelbetrieb.md):

- Match-Fingerprint, Schema-Versionen und Tick-Reihenfolge werden nicht ohne
  Inhaberentscheidung und D-ID verändert.
- Simulationsverhalten und Determinismus-Baselines werden nie im selben PR
  geändert.
- `ICommandTransport` und `ICommandSubmissionReadiness` gehören dem
  Netzstrang; andere Stränge dürfen sie benutzen, aber nicht ändern.

## 3. Checks

Pflicht auf jedem PR:

- **`tests`** – die Simulationstests aus `tools/Nova.SimRunner.Tests`. Das ist
  der Check, der euch schützt.
- **`docs-check`** – tote interne Links und UTF-8 in Markdown.
- **`integrity`** – Selbsttests des schlafenden Gate-Apparats. Er läuft in
  Tier 2 auf jedem PR, damit der Apparat nicht unbemerkt verrottet.
- **`baseline-guard`** – Simulationsverhalten und eine Determinismus-Baseline
  dürfen nicht im selben PR geändert werden. Ein dokumentierter Reset braucht
  das Maintainer-Label `baseline-reset-approved`.
- **`external-contributor-review`** – bei einem externen PR müssen die
  CLA-Zustimmung und eine Freigabe eines Maintainers auf dem aktuellen Head
  vorliegen.

Unity-EditMode-Tests laufen mangels CI-Lizenz nicht automatisch. Wer die
Präsentationsschicht (`Assets/_Project/Scripts/{Presentation,Gameplay}`) anfasst,
führt sie lokal aus und schreibt das Ergebnis in den PR.

## 4. Zugänge und Reviews (Tier 2)

`main` ist technisch auf die beiden Maintainer beschränkt:

- [@cubetribe](https://github.com/cubetribe) (Dennis Westermann)
- [@travelhawk](https://github.com/travelhawk) (Michael Falk)

Nur diese Accounts dürfen einen PR nach `main` mergen. Externe arbeiten aus
einem Fork, erhalten weder einen Collaborator-Zugang noch Mitgliedschaft im
Maintainer-Team und öffnen einen PR nach `main`.

Jeder PR braucht eine Freigabe des jeweils anderen Maintainers auf dem aktuellen
Head-Commit. Ein neuer Push macht eine alte Freigabe unwirksam. Der Remote-
Rollout setzt dafür eine erforderliche Freigabe, das Verwerfen veralteter
Freigaben und eine Freigabe des letzten Pushes. Für externe PRs prüft
`external-contributor-review` zusätzlich die CLA-Zustimmung und genau diese
Maintainer-Freigabe. Bis die Einstellungen nach den ersten erfolgreichen Läufen
in Folge-PRs aus dem geschützten `main`-Stand aktiv sind, werden keine externen
PRs gemergt.

Nach einer neu eingereichten Freigabe wird der jüngste fehlgeschlagene
`external-contributor-review`-Lauf erneut gestartet. Die native Branch
Protection bleibt der maßgebliche, automatisch aktualisierte Review-Schutz.

`CODEOWNERS` ordnet jeden Pfad den beiden Maintainers zu; GitHubs native
Code-Owner-Prüfung macht die Peer-Freigabe damit eindeutig. Die ausdrücklich
benannte Steuerfläche (`.github/`, Lizenz, Governance und Planungsdokumente)
bleibt besonders sichtbar, ersetzt aber nicht die Schreibhoheit aus
[13-15_Parallelbetrieb.md](docs/production/hashkrieg/13-15_Parallelbetrieb.md).

## 5. Beitragsrechte

Soweit die jeweiligen Rechteinhaber ihn unter diesen Bedingungen freigegeben
haben, steht der Repository-Code unter
[PolyForm Noncommercial 1.0.0](LICENSE). Nicht-kommerzielle Nutzung, Änderungen
und Weitergabe sind damit erlaubt; kommerzielle Nutzung ist nicht allgemein
freigegeben. Kunst, Audio, Schriften und andere Assets haben ihre eigenen
Rechte, siehe [NOTICE](NOTICE) und
[docs/assets/Licenses.md](docs/assets/Licenses.md).

Mit dem Häkchen im PR-Template akzeptierst du die
[Contributor License Agreement](CONTRIBUTOR_LICENSE_AGREEMENT.md). Du behältst
dein Urheberrecht, gibst dem Projektinhaber aber das Recht, den Beitrag auch
kommerziell und unter anderen Lizenzbedingungen zu verwenden. Ohne dieses
Häkchen wird ein externer PR nicht gemergt.

Die CLA wirkt nur für den konkret mit dokumentierter Zustimmung eingereichten
Beitrag. Frühere Beiträge werden dadurch nicht rückwirkend erfasst; alle
Beitragenden behalten ihr jeweiliges Urheberrecht.

## 6. Commit-Konvention

Format: `type(scope): imperative summary`, Englisch, höchstens 72 Zeichen.
Typen: `feat`, `fix`, `docs`, `refactor`, `chore`, `test`, `perf`, `build`, `ci`.

Ein Commit entspricht einer logischen Änderung. Keine Secrets, generierten
Binärdateien oder Debug-Artefakte einchecken.

## 7. Pull Requests

Die Beschreibung nennt: was und warum, betroffene Bereiche, gegebenenfalls die
D-ID einer echten Architektur-, Design- oder Prozessentscheidung, den
Changelog-Eintrag und – bei Änderungen am Spielverhalten – was du im laufenden
Spiel gesehen hast. Verträge und öffentliche Doku erhalten in Tier 2 außerdem
eine Kopfversion und einen Änderungsverlauf.

## 8. Releases

Nur ein Maintainer erzeugt nach expliziter Freigabe Tag und Release. Wiki-Versionen
sind keine Game-Releases. Es gibt bisher kein veröffentlichtes Release.

## Offene Punkte

- Die neuen Tier-2-Checks und die erforderliche Maintainer-Peer-Review werden
  erst nach dem Merge und ihren ersten erfolgreichen Folge-PR-Läufen in GitHubs
  Branch Protection hinterlegt.

## Nächste Schritte

1. Neue externe Beiträge erst nach dem vollständigen Remote-Rollout annehmen.
2. Den Baseline-Wächter mit einem absichtlich falschen PR nachweislich rot
   auslösen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-21 | PR-only-Community-Workflow eingeführt | Maintainers |
| 2.0.0 | 2026-07-24 | D-059: kurze Topic-Branches, per-action Agentenautorität, gestuftes Review | Maintainers |
| 2.1.0–2.5.0 | 2026-07-24 – 2026-07-25 | Gate-Evidenzregime D-062 bis D-066 als PR-Pflicht verankert | Maintainers |
| 3.0.0 | 2026-08-06 | D-076: auf Tier 1 zurückgeschnitten. Gate- und Evidenzpflichten entfernt, `tests` als Pflichtcheck ergänzt, `integrity` auf `quality/**` begrenzt, Selbst-Merge erlaubt | Maintainers |
| 4.0.0 | 2026-08-08 | D-091: Tier 2 aktiviert; Maintainer-Peer-Review auf aktuellem Head für jeden PR, Fork-only plus CLA für externe Beiträge, Baseline-Wächter und zwei festgelegte Merge-Accounts dokumentiert | Dennis Westermann / Michael Falk |
| 4.0.1 | 2026-08-08 | CLA-Wirkung nicht rückwirkend präzisiert und D-ID-Pflicht auf echte Entscheidungen vereinheitlicht | Dennis Westermann |
