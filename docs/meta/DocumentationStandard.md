# Dokumentationsstandard

**Version:** 2.0.0 | **Status:** verbindlich (Governance-Tier 1) | **Verantwortungsbereich:** Technical Writer | **Sprint:** 7

## Zweck

Definiert, wie Dokumentation in diesem Repository geschrieben wird. Seit D-076
gilt **Governance-Tier 1** ([../../GOVERNANCE.md](../../GOVERNANCE.md)): Der
Standard beschreibt gute Praxis und erzwingt nur noch das, was maschinell
prüfbar und wirklich teuer im Fehlerfall ist – tote Links, kaputtes UTF-8,
unparsebare Maschinenverträge.

Was früher hier stand und jetzt schläft: der Evidenz- und Gate-Vertrag. Er liegt
unverändert in [`../../quality/README.md`](../../quality/README.md) und
[MVPRecoveryPlan.md](../production/MVPRecoveryPlan.md) §2.

## Abhängigkeiten

- [../../GOVERNANCE.md](../../GOVERNANCE.md) – Tier-Modell, aktives Tier
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-001, D-005,
  D-047, D-076
- [`../../quality/content/mvp-v1.json`](../../quality/content/mvp-v1.json) –
  einziger weiterhin voll versionierter Vertrag

## 1. Grundprinzipien

1. **Klein und fokussiert:** ein Dokument behandelt ein Thema.
2. **Relative Links:** interne Abhängigkeiten werden relativ verlinkt. Tote
   interne Links brechen die CI – das ist die einzige harte Doku-Regel.
3. **Sprache:** deutsche Projektprosa; Code, Identifier und Pfade englisch.
4. **Keine Platzhalter:** keine leeren Zukunftsdokumente.
5. **Single Source of Truth:** ein Zahlenwert hat genau eine führende Quelle;
   andere Dokumente verweisen darauf, statt ihn zu kopieren.
6. **Behauptung ≠ Nachweis:** Status, Plan, Datei- oder Typanwesenheit belegen
   kein funktionierendes Feature. Was „fertig" heißt, definiert
   [../../GOVERNANCE.md](../../GOVERNANCE.md).
7. **Maschinenlesbare Verträge:** JSON-Manifeste und Szenarien werden gemeinsam
   mit ihrer Markdown-Erklärung geändert und müssen parsebar bleiben.

## 2. Empfohlener Aufbau

Bewährt, aber **nicht erzwungen**:

1. Titel,
2. Kopfzeile `Version | Status | Verantwortungsbereich | Sprint`,
3. Zweck,
4. Abhängigkeiten,
5. thematischer Inhalt,
6. Offene Punkte,
7. Nächste Schritte.

Die Kopfzeile ist empfohlen, weil sie beim Überfliegen des Wikis den Reifegrad
zeigt; die CI meldet ihr Fehlen als Hinweis, nicht als Fehler. Bestehende
Dokumente behalten ihren Aufbau – niemand muss sie umbauen.

## 3. Versionierung und Änderungsverlauf

**Freiwillig.** Git ist der Änderungsverlauf: `git log --follow <datei>` liefert
Datum, Autor und Begründung genauer als jede handgepflegte Tabelle, und niemand
vergisst ihn.

Wo eine Tabelle bereits existiert, darf sie stehen bleiben und weitergeführt
werden – vor allem in Governance-Dateien, wo die Absicht hinter einer Änderung
zählt. Sie ist nur keine Bedingung mehr für einen Merge.

**Ausnahme mit Versionspflicht:**
[`../../quality/content/mvp-v1.json`](../../quality/content/mvp-v1.json) ist ein
Vertrag und die einzige Autorität für MS-1-Sollwerte. Änderungen daran werden
versioniert und begründet.

Wiki-Versionen sind Dokumentationsstände und keine Game-Releases.

## 4. Entscheidungen

Architektur-, Design- und Prozessentscheidungen erhalten eine fortlaufende D-ID
im [DecisionLog](../production/DecisionLog.md) mit:

- der Entscheidung,
- der Begründung,
- den Konsequenzen und
- einer Zeile zu dem, was verworfen wurde und warum.

Die frühere Pflicht zu mindestens drei ausformulierten Alternativen entfällt in
Tier 1 (D-076). Sie kommt ab Tier 2 zurück. Bestehende Einträge werden **nicht**
zurückgebaut.

Revidierte Einträge bleiben sichtbar und werden `ersetzt durch D-xxx`
beziehungsweise `teilweise ersetzt` markiert. MS-1-Overrides dürfen ein
Vollspiel-Zielbild zeitweise übersteuern, müssen Scope und Gültigkeitsphase aber
explizit benennen.

## 5. Gate-Evidenz (schlafend)

Der vollständige Evidenzvertrag – Schema, Semantikvalidator, Receipt-Kette,
Trusted Tooling, Performance-Methodenprofile – ist unter Tier 1 nicht in Kraft.
Er steht unverändert in [`../../quality/README.md`](../../quality/README.md) und
[MVPRecoveryPlan.md](../production/MVPRecoveryPlan.md) §2 und wacht mit Tier 3
wieder auf.

Bis dahin gilt: **Dokumente behaupten keinen Gate-Status.** Sie beschreiben, was
ist, und benennen Lücken ehrlich – so wie
[ScopeLedger.md](../production/ScopeLedger.md) es tut.

## 6. Prüfung vor dem Merge

Die CI prüft hart:

- tote interne Links,
- UTF-8-Gültigkeit,
- Parsebarkeit der Quality-JSONs.

Menschlich zu prüfen bleibt:

- Werteautorität (kopiert das Dokument Zahlen, die woanders geführt werden?),
- `[Unreleased]`-Eintrag im CHANGELOG,
- keine unbelegten Fertig-Behauptungen.

Review-Regeln stehen in [../../CONTRIBUTING.md](../../CONTRIBUTING.md) §4.

## Offene Punkte

- Keine.

## Nächste Schritte

1. Bestehende Dokumente bei der nächsten inhaltlichen Berührung entschlacken,
   nicht auf Vorrat.
2. Beim Wechsel auf Tier 2 die D-ID-Alternativenpflicht und die Doku-Versionierung
   für öffentliche Dokumente wieder aktivieren.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-21 | Initialer verbindlicher Standard (Sprint 0) | Technical Writer |
| 1.1.0 | 2026-07-21 | Grundprinzip „Single Source of Truth für Werte" ergänzt (D-047) | Technical Writer |
| 1.2.0–1.7.0 | 2026-07-24 – 2026-07-25 | Ausbau der Evidence-Autorität (D-061 bis D-066) | Technical Writer |
| 2.0.0 | 2026-08-06 | D-076: auf Governance-Tier 1 zurückgeschnitten. Pflichtaufbau, Versionsbump und Änderungsverlauf freiwillig; Evidenzvertrag als schlafend nach `quality/README.md` verwiesen; D-ID-Alternativenpflicht bis Tier 2 ausgesetzt | Technical Writer |
