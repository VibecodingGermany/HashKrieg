# Dokumentationsstandard

**Version:** 2.1.0 | **Status:** verbindlich (Governance-Tier 2) | **Verantwortungsbereich:** Technical Writer | **Sprint:** 13.0

## Zweck

Definiert, wie Dokumentation in diesem Repository geschrieben wird. Seit D-091
gilt **Governance-Tier 2** ([../../GOVERNANCE.md](../../GOVERNANCE.md)): Tote
Links, UTF-8 und Maschinenverträge bleiben harte Regeln; Verträge und öffentliche
Dokumentation führen wieder einen nachvollziehbaren Aufbau und Versionsstand.

Was früher hier stand und jetzt schläft: der Evidenz- und Gate-Vertrag. Er liegt
unverändert in [`../../quality/README.md`](../../quality/README.md) und
[MVPRecoveryPlan.md](../production/MVPRecoveryPlan.md) §2.

## Abhängigkeiten

- [../../GOVERNANCE.md](../../GOVERNANCE.md) – Tier-Modell, aktives Tier
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-001, D-005,
  D-047, D-076, D-091
- [`../../quality/content/mvp-v1.json`](../../quality/content/mvp-v1.json) –
  einziger weiterhin voll versionierter Vertrag

## 1. Grundprinzipien

1. **Klein und fokussiert:** ein Dokument behandelt ein Thema.
2. **Relative Links:** interne Abhängigkeiten werden relativ verlinkt. Tote
   interne Links brechen die CI. In Tier 2 sind außerdem der unten definierte
   Aufbau und ein nachvollziehbarer Versionsstand für Verträge und öffentliche
   Dokumentation verbindlich.
3. **Sprache:** deutsche Projektprosa; Code, Identifier und Pfade englisch.
4. **Keine Platzhalter:** keine leeren Zukunftsdokumente.
5. **Single Source of Truth:** ein Zahlenwert hat genau eine führende Quelle;
   andere Dokumente verweisen darauf, statt ihn zu kopieren.
6. **Behauptung ≠ Nachweis:** Status, Plan, Datei- oder Typanwesenheit belegen
   kein funktionierendes Feature. Was „fertig" heißt, definiert
   [../../GOVERNANCE.md](../../GOVERNANCE.md).
7. **Maschinenlesbare Verträge:** JSON-Manifeste und Szenarien werden gemeinsam
   mit ihrer Markdown-Erklärung geändert und müssen parsebar bleiben.

## 2. Aufbau

Für Verträge und öffentliche Doku verbindlich, für interne Arbeitsnotizen
empfohlen:

1. Titel,
2. Kopfzeile `Version | Status | Verantwortungsbereich | Sprint`,
3. Zweck,
4. Abhängigkeiten,
5. thematischer Inhalt,
6. Offene Punkte,
7. Nächste Schritte.

Bestehende interne Dokumente behalten ihren Aufbau, bis sie inhaltlich berührt
werden. Öffentliche Einstiegstexte, Governance- und Prozessdokumente sowie
Verträge werden bei einer Änderung auf diesen Aufbau gezogen.

Unveränderte standardisierte Lizenztexte bleiben wortgetreu und erhalten keinen
projektspezifischen Kopf oder Änderungsverlauf. Operative Vorlagen und
Maschinenkonfigurationen wie PR-Templates und Workflow-YAML dokumentiert Git;
ihre menschenlesbaren Verträge bleiben die versionierten Prozessdokumente.

## 3. Versionierung und Änderungsverlauf

Für Verträge und öffentliche Doku verpflichtend. Git ergänzt den Verlauf:
`git log --follow <datei>` liefert Datum, Autor und Begründung genauer als jede
handgepflegte Tabelle.

Wo eine Tabelle bereits existiert, wird sie in diesen Dokumenten weitergeführt.

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

Ab Tier 2 dokumentiert jede neue D-ID mindestens drei bewertete Alternativen.
Bestehende Einträge werden **nicht** zurückgebaut.

Revidierte Einträge bleiben sichtbar und werden `ersetzt durch D-xxx`
beziehungsweise `teilweise ersetzt` markiert. MS-1-Overrides dürfen ein
Vollspiel-Zielbild zeitweise übersteuern, müssen Scope und Gültigkeitsphase aber
explizit benennen.

## 5. Gate-Evidenz (schlafend)

Der vollständige Evidenzvertrag – Schema, Semantikvalidator, Receipt-Kette,
Trusted Tooling, Performance-Methodenprofile – ist unter Tier 1 und 2 nicht in
Kraft.
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
- Kopfversion und Änderungsverlauf für Verträge und öffentliche Doku,
- keine unbelegten Fertig-Behauptungen.

Review-Regeln stehen in [../../CONTRIBUTING.md](../../CONTRIBUTING.md) §4.

## Offene Punkte

- Keine.

## Nächste Schritte

1. Bestehende Dokumente bei der nächsten inhaltlichen Berührung entschlacken,
   nicht auf Vorrat.
2. Beim Wechsel auf Tier 3 den Evidenzvertrag gemäß
   [GOVERNANCE.md](../../GOVERNANCE.md) wieder aktivieren.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-21 | Initialer verbindlicher Standard (Sprint 0) | Technical Writer |
| 1.1.0 | 2026-07-21 | Grundprinzip „Single Source of Truth für Werte" ergänzt (D-047) | Technical Writer |
| 1.2.0–1.7.0 | 2026-07-24 – 2026-07-25 | Ausbau der Evidence-Autorität (D-061 bis D-066) | Technical Writer |
| 2.0.0 | 2026-08-06 | D-076: auf Governance-Tier 1 zurückgeschnitten. Pflichtaufbau, Versionsbump und Änderungsverlauf freiwillig; Evidenzvertrag als schlafend nach `quality/README.md` verwiesen; D-ID-Alternativenpflicht bis Tier 2 ausgesetzt | Technical Writer |
| 2.1.0 | 2026-08-08 | D-091: Tier-2-Pflichten für Verträge und öffentliche Doku sowie ≥3 Alternativen je neuer D-ID wieder aktiviert; Gate-Evidenz bleibt bis Tier 3 schlafend | Technical Writer |
