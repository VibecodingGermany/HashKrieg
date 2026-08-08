# quality/ – Gate-Apparat (schlafend)

**Version:** 1.1.0 | **Status:** Gate-Kette schlafend in Tier 2 (D-076/D-091) | **Verantwortungsbereich:** Maintainers / Lead QA | **Sprint:** 13.0

## Zweck

Kein Code ist gelöscht. Dieses Verzeichnis bewahrt den Gate-Evidenzvertrag für
den späteren Wechsel auf Tier 3.

Dieses Verzeichnis enthält das Gate-Evidenzregime aus den Entscheidungen D-061
bis D-066: Evidence-Schema, Semantikvalidator, Gate-Runner, kanonisches
MS-1-Manifest und die Szenarienschwellen. Es ist auf **Governance-Tier 3**
ausgelegt – also auf ein Projekt mit fremden Beitragenden, Nutzern und Haftung.

Unter dem aktiven **Tier 2** (externe Beitragende, zwei Maintainer) blockiert es
weiterhin keinen Meilenstein. Was stattdessen gilt, steht in
[../GOVERNANCE.md](../GOVERNANCE.md).

## Abhängigkeiten

- [../GOVERNANCE.md](../GOVERNANCE.md) – aktives Tier und Fertig-Definition
- [../docs/production/MVPRecoveryPlan.md](../docs/production/MVPRecoveryPlan.md) – vollständiger Evidenzvertrag

## Was hier liegt

| Pfad | Inhalt |
|---|---|
| `content/mvp-v1.json` | kanonisches MS-1-Inhaltsmanifest – **weiterhin gültig und führend für alle Sollwerte** |
| `scenarios/mvp-v1.json` | Szenarien, Schwellen, Kriterienprofile der Gates |
| `schemas/GateEvidence.schema.json` | Evidence-Schema 1.4 (Draft 2020-12) |
| `schemas/GateAuthorization.schema.json` | Receipt-Schema des D-066-Vertrags |
| `scripts/validate_gate_evidence.py` | Semantikvalidator, fail-closed (5.202 Zeilen) |
| `scripts/run_gate_check.py` | Gate-Runner |
| `scripts/validate_evidence_schema.mjs` | Ajv-Schemaprüfung |
| `package-lock.json` | gepinnte Validator-Abhängigkeiten |

**`content/mvp-v1.json` schläft nicht.** Es bleibt die einzige Autorität für
MS-1-Sollwerte, und der [ScopeLedger](../docs/production/ScopeLedger.md) zeigt
weiterhin darauf. Nur der *Beweisapparat* drumherum ruht, nicht der Inhalt.

## Was „schlafend" konkret heißt

- Die Gate-Kette `G0 → G1 → … → G5` blockiert keinen Meilensteinfortschritt mehr.
  Was „fertig" heißt, definiert [../GOVERNANCE.md](../GOVERNANCE.md).
- `quality/evidence/` und `quality/authorizations/` existieren nicht und werden
  nicht angelegt. Es gab nie ein reales Artefakt darin.
- Der `integrity`-Job in
  [`../.github/workflows/quality-gate.yml`](../.github/workflows/quality-gate.yml)
  läuft in Tier 2 auf jedem PR. So bleibt der Apparat lauffähig, während externe
  Beiträge eingehen.
- Der Authorize-Pfad (`workflow_dispatch`) ist unverändert vorhanden. Er wurde
  nie ausgeführt; das geschützte Environment `quality-gate` existiert nicht.

## Aufwecken

Der Weg zurück steht in [../GOVERNANCE.md](../GOVERNANCE.md) unter „Was schläft
und wie es aufwacht". Kurz:

1. Tier-Wechsel als D-ID entscheiden.
2. Geschütztes Environment `quality-gate` mit Required Reviewers anlegen.
3. [MVPRecoveryPlan.md](../docs/production/MVPRecoveryPlan.md) wieder als führend
   für den Meilensteinstatus erklären.

Der vollständige Evidenzvertrag – Aufbau, Referenzformen, Vorgängerkette,
Performance-Regeln – steht unverändert in
[MVPRecoveryPlan.md](../docs/production/MVPRecoveryPlan.md) §2.

## Offene Punkte

- Das geschützte Environment `quality-gate` wird erst mit Tier 3 benötigt.

## Nächste Schritte

1. In Tier 2 den `integrity`-Job auf jedem PR grün halten.
2. Bei einem Tier-3-Wechsel den Ablauf unter „Aufwecken" vollständig ausführen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.1.0 | 2026-08-08 | D-091: Tier-2-Status und `integrity` auf jedem PR dokumentiert | Maintainers |
