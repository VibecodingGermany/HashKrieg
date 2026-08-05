# quality/ – Gate-Apparat (schlafend)

**Status: schlafend seit D-076 (2026-08-06). Kein Code ist gelöscht.**

Dieses Verzeichnis enthält das Gate-Evidenzregime aus den Entscheidungen D-061
bis D-066: Evidence-Schema, Semantikvalidator, Gate-Runner, kanonisches
MS-1-Manifest und die Szenarienschwellen. Es ist auf **Governance-Tier 3**
ausgelegt – also auf ein Projekt mit fremden Beitragenden, Nutzern und Haftung.

Unter dem aktiven **Tier 1** (zwei Entwickler, kein Publikum) blockiert es
nichts. Was stattdessen gilt, steht in [../GOVERNANCE.md](../GOVERNANCE.md).

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
  läuft weiterhin – aber nur noch, wenn ein PR `quality/**` anfasst. So bleibt
  der Apparat lauffähig, ohne jeden PR zu belasten.
- Der Authorize-Pfad (`workflow_dispatch`) ist unverändert vorhanden. Er wurde
  nie ausgeführt; das geschützte Environment `quality-gate` existiert nicht.

## Aufwecken

Der Weg zurück steht in [../GOVERNANCE.md](../GOVERNANCE.md) unter „Was schläft
und wie es aufwacht". Kurz:

1. Tier-Wechsel als D-ID entscheiden.
2. Pfadfilter des `integrity`-Jobs entfernen.
3. Geschütztes Environment `quality-gate` mit Required Reviewers anlegen.
4. [MVPRecoveryPlan.md](../docs/production/MVPRecoveryPlan.md) wieder als führend
   für den Meilensteinstatus erklären.

Der vollständige Evidenzvertrag – Aufbau, Referenzformen, Vorgängerkette,
Performance-Regeln – steht unverändert in
[MVPRecoveryPlan.md](../docs/production/MVPRecoveryPlan.md) §2.
