# Dokumentationsstandard

**Version:** 1.7.0 | **Status:** verbindlich | **Verantwortungsbereich:** Technical Writer | **Sprint:** 7

## Zweck

Definiert den verbindlichen Standard für reviewbare Living Documents und
maschinenlesbare Quality-Verträge. Dokumentation beschreibt Anforderungen;
Gate-Erfolg entsteht ausschließlich durch aktuelle, reproduzierbare Evidence.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-001,
  D-005, D-047, D-061 bis D-066
- [../production/MVPRecoveryPlan.md](../production/MVPRecoveryPlan.md)
- [`../../quality/schemas/GateEvidence.schema.json`](../../quality/schemas/GateEvidence.schema.json)
- [`../../quality/scripts/validate_gate_evidence.py`](../../quality/scripts/validate_gate_evidence.py)

## 1. Grundprinzipien

1. **Klein und fokussiert:** ein Dokument behandelt ein Thema.
2. **Living Documents:** jede relevante Änderung erhöht die Version und ergänzt
   den Änderungsverlauf.
3. **Relative Links:** interne Abhängigkeiten werden relativ verlinkt.
4. **Sprache:** deutsche Projektprosa; Code, Identifier und Pfade technisch/
   englisch.
5. **Keine Platzhalter:** weder leere Zukunftsdokumente noch Evidence-Dateien
   ohne realen Lauf.
6. **Single Source of Truth:** ein Zahlenwert hat genau eine führende Quelle;
   andere Dokumente verweisen darauf.
7. **Requirements ≠ Evidence:** Status, Plan, Datei- oder Typanwesenheit beweisen
   kein Gate.
8. **Maschinenlesbare Verträge:** JSON-Manifeste und Szenarien sind gemeinsam
   mit ihrer Markdown-Erklärung zu ändern und müssen parsebar bleiben.

## 2. Pflichtaufbau

Jedes Wiki-Markdown-Dokument enthält in dieser Reihenfolge:

1. Titel,
2. Kopfzeile `Version | Status | Verantwortungsbereich | Sprint`,
3. Zweck,
4. Abhängigkeiten,
5. thematischen Inhalt,
6. Offene Punkte,
7. Nächste Schritte,
8. Änderungsverlauf mit Version, Datum, Änderung und Autor.

Root-Governance-Dateien dürfen eine an ihr Format angepasste Kopfzeile nutzen,
benötigen bei inhaltlicher Änderung aber ebenfalls einen datierten
Änderungsverlauf.

## 3. Versionierung

- `0.x`: Entwurf,
- `1.0`: verbindlicher Erstvertrag,
- Minor-Bump für inhaltliche Erweiterung,
- Patch-Bump für reine Korrektur,
- Major-Bump für grundlegendes Rebaseline.

Wiki-Versionen sind Dokumentationsstände und keine Game-Releases. Status- oder
Strukturänderungen ziehen Wiki-Index, Root-README und `[Unreleased]` nach.

## 4. Entscheidungen

Architektur-, Design- und Prozessentscheidungen erhalten eine D-ID mit
mindestens drei Alternativen, Begründung und Konsequenzen. Revidierte Einträge
bleiben sichtbar und werden `ersetzt durch D-xxx` beziehungsweise
`teilweise ersetzt` markiert.

MS-1-Overrides dürfen ein Vollspiel-Zielbild zeitweise übersteuern, müssen
Scope und Gültigkeitsphase explizit benennen.

## 5. Evidence-Autorität

Schema 1.2 ist ausschließlich eine Integritätsvorstufe. Evidence:

- validiert mit gepinntem Ajv Draft 2020-12 gegen Subject-Schema `1.2.0`
  [`GateEvidence.schema.json`](../../quality/schemas/GateEvidence.schema.json),
- besteht im selben CLI-Lauf die Cross-Field-Prüfung durch
  [`validate_gate_evidence.py`](../../quality/scripts/validate_gate_evidence.py),
- liegt append-only unter
  `quality/evidence/G<N>/<subjectSha>/<attempt>/GateEvidence.json`,
- bindet Commit, Tree, Toolchain, SHA-256 der Content-/Scenario-/Schema-/
  Validator-Git-Blobs am Subject-Commit und Rohartefakte,
- referenziert ab G1 das rekursiv valide unmittelbare Vorgängergate am selben
  Commit/Tree,
- bindet jedes Kriterium an einen kanonischen gleichnamigen Check und jedes
  geforderte Szenario an dessen Pflichtassertions, exakte Units sowie
  Schwellenmetriken,
- benennt Reviewer und Implementation Writer getrennt und belegt die
  Reviewer-Wiederholung als eigene Artefaktausführung,
- autorisiert keinen Pass; jeder Pass-Versuch endet zusätzlich mit
  `E_AUTHORIZATION_BOOTSTRAP`,
- wird bei relevanten Änderungen stale und
- wertet Skip, Cancel oder fehlendes Pflichtresultat als Fail.

Der Zielvertrag wird nach D-066 in zwei G0-A-Bausteinen hergestellt:

- G0-A1 liefert Schema-, Semantik-, Trusted-Checkout-, Umgebungs- und
  Runner-Integritätsprüfungen. Sein PR-Workflow kann keinen Pass
  autorisieren; auch alte Trust-Kontexte enden fail-closed.
- G0-A2 trennt Subject-Commit, Evidence-Carrier-Commit und Trusted-Tool-
  Commit. Ein geschützter Lauf erzeugt nach der Validierung ein
  hashgebundenes `GateAuthorization.json`-Receipt. Kein Lauf darf seinen
  eigenen noch ausstehenden Erfolg attestieren.
- Vorgänger-Receipts werden append-only versioniert und von späteren Gates
  gegen den exakten erfolgreichen GitHub-Run/-Attempt/-Job geprüft.
  Fehlende, zusätzliche, vertauschte, manipulierte oder wiederverwendete
  Receipts sind ungültig.
- Command und Performance-Messung referenzieren dieselbe `environmentId`.
  Windows-x64-Referenzmessung und Mac-M2-Funktionsmessung besitzen getrennte
  Methodenprofile und binden OS, Architektur, Hardware, Build, Managed/Burst,
  Auflösung, Quality-Profil, VSync, Deep Profiling und Replay exakt.
- Fehlender Node-/Ajv-Stack oder ein hängender Schema-Subprozess bleibt
  kontrolliert fail-closed. Negative Controls decken manipuliertes Subject-
  Tooling, unvollständige Ketten und falsche Umgebungen ab.

Dieses Repository legt Evidence-Verzeichnisse erst bei einem realen Versuch an.
Beispiel- oder Platzhalter-Evidence ist verboten.

## 6. Reviews und Links

Jede Änderung prüft:

- interne Links,
- Version und History,
- `[Unreleased]`,
- D-ID- und Werteautorität,
- JSON-Parsebarkeit bei Maschinenverträgen und
- die Abwesenheit unbelegter Gate-/Meilensteinbehauptungen.

Unabhängiges read-only Review ersetzt im Solo-/KI-Modus die
Autoren-Selbstfreigabe. Eine zweite menschliche Freigabe wird ab mindestens
zwei aktiven menschlichen Maintainers Pflicht.

## Offene Punkte

- Keine für den aktuellen Recovery-Vertrag.

## Nächste Schritte

1. G0-A1 ohne Gate-Fortschritt geschützt mergen.
2. G0-A2 separat implementieren und adversarial prüfen.
3. Den vollständigen Trustpfad erst an einem nachfolgenden sauberen Subject
   für reale G0-Evidence verwenden.
4. Nach G5 Review-Rhythmus für Post-MVP neu bewerten.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-21 | Initialer verbindlicher Standard (Sprint 0) | Technical Writer |
| 1.1.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings): Grundprinzip 6 „Single Source of Truth für Werte" ergänzt (D-047) | Technical Writer |
| 1.2.0 | 2026-07-24 | D-061-Evidence-Autorität, Requirements/Evidence-Trennung und Maschinenvertrag-Regeln ergänzt | Technical Writer |
| 1.3.0 | 2026-07-24 | Schema- und Semantikprüfung sowie SHA-256-Dateibindung für Gate-Evidence verankert | Technical Writer |
| 1.4.0 | 2026-07-24 | D-062-Subject-Blob-, Szenariometrik- und Same-Subject-Gate-Ketten-Regeln ergänzt | Technical Writer |
| 1.5.0 | 2026-07-24 | D-063-Schema 1.2, kanonische Check-Artefakte, rekursive Ajv-Prüfung und Protected-CI-Trust-Autorität verankert | Technical Writer |
| 1.6.0 | 2026-07-24 | D-064-Fail-Closed-Schema 1.2, subject-unabhängigen Schema-1.3-Bootstrap, vollständige Autorisierungskette und Umgebungsbindung verankert | Technical Writer |
| 1.7.0 | 2026-07-25 | D-066: Integrity-Basis G0-A1 von zweiphasiger Receipt-Autorisierung G0-A2 getrennt und alle Pass-Behauptungen bis dahin gesperrt | Technical Writer |
