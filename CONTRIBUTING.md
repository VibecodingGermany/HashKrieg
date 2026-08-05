# Beitragen zu Project Nova

**Version:** 3.0.0 | **Governance-Tier:** 1 ([GOVERNANCE.md](GOVERNANCE.md))

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

## 3. Checks

Pflicht auf jedem PR:

- **`tests`** – die Simulationstests aus `tools/Nova.SimRunner.Tests`. Das ist
  der Check, der euch schützt.
- **`docs-check`** – tote interne Links und UTF-8 in Markdown.

Zusätzlich nur bei Änderungen an `quality/**`:

- **`integrity`** – Selbsttests des schlafenden Gate-Apparats, damit er nicht
  unbemerkt verrottet. Siehe [GOVERNANCE.md](GOVERNANCE.md) und
  [quality/README.md](quality/README.md).

Unity-EditMode-Tests laufen mangels CI-Lizenz nicht automatisch. Wer die
Präsentationsschicht (`Assets/_Project/Scripts/{Presentation,Gameplay}`) anfasst,
führt sie lokal aus und schreibt das Ergebnis in den PR.

## 4. Reviews (Tier 1)

Bei zwei Entwicklern, die sich kennen, ist Pflicht-Review teurer als er nützt:

- **Selbst-Merge ist erlaubt**, sobald die CI grün ist.
- **Review anfordern, wenn** die Änderung fremdes Terrain berührt, du unsicher
  bist, oder sie Simulationsdeterminismus, Speicherformat oder das
  Commandprotokoll anfasst.
- Was ein Agent geschrieben hat, liest vor dem Merge ein Mensch. Nicht als
  Formalie – als der Punkt, an dem Regel 5 aus [AGENTS.md](AGENTS.md) greift.

Ab Tier 2 (erster PR von außerhalb) braucht jeder fremde PR ein
Maintainer-Review. Ab Tier 3 zwei Freigaben. Auslöser und Umschaltung:
[GOVERNANCE.md](GOVERNANCE.md).

## 5. Commit-Konvention

Format: `type(scope): imperative summary`, Englisch, höchstens 72 Zeichen.
Typen: `feat`, `fix`, `docs`, `refactor`, `chore`, `test`, `perf`, `build`, `ci`.

Ein Commit entspricht einer logischen Änderung. Keine Secrets, generierten
Binärdateien oder Debug-Artefakte einchecken.

## 6. Pull Requests

Die Beschreibung nennt: was und warum, betroffene Bereiche, neue oder geänderte
D-IDs, den Changelog-Eintrag und – bei Änderungen am Spielverhalten – was du im
laufenden Spiel gesehen hast.

## 7. Releases

Nur ein Maintainer erzeugt nach expliziter Freigabe Tag und Release. Wiki-Versionen
sind keine Game-Releases. Es gibt bisher kein veröffentlichtes Release.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-21 | PR-only-Community-Workflow eingeführt | Maintainers |
| 2.0.0 | 2026-07-24 | D-059: kurze Topic-Branches, per-action Agentenautorität, gestuftes Review | Maintainers |
| 2.1.0–2.5.0 | 2026-07-24 – 2026-07-25 | Gate-Evidenzregime D-062 bis D-066 als PR-Pflicht verankert | Maintainers |
| 3.0.0 | 2026-08-06 | D-076: auf Tier 1 zurückgeschnitten. Gate- und Evidenzpflichten entfernt, `tests` als Pflichtcheck ergänzt, `integrity` auf `quality/**` begrenzt, Selbst-Merge erlaubt | Maintainers |
