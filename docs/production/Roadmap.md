# Produktions-Roadmap

**Version:** 2.3.0 | **Status:** messwertbasiertes Rebaseline – ohne Kalenderzusage | **Verantwortungsbereich:** Producer / Executive Producer | **Sprint:** 7

## Zweck

Definiert, **wann** Project Nova wieder geschätzt werden darf. Die frühere
445-PT- und Kalenderplanung ist eine ungetaggte Sprint-6-Momentaufnahme und
keine aktive Baseline. Dieses Dokument enthält bewusst weder Fertigstellungsdatum
noch aktive Gesamt-PT-Summe.

## Abhängigkeiten

- [MVPRecoveryPlan.md](MVPRecoveryPlan.md) – Gate-Sequenz
- [Milestones.md](Milestones.md) – MS-0/MS-1-Definition
- [MVPContentManifest.md](MVPContentManifest.md) – fester MS-1-Scope
- [DecisionLog.md](DecisionLog.md) – D-055, D-056, D-061 und D-063 bis D-066
- [RiskAnalysis.md](RiskAnalysis.md) – R-13 und R-16 bis R-18

## 1. Historische Korrektur

Die Sprint-6-Annahme von 445 Personentagen und ein Zeitstrahl bis 2028 wurden
ohne gemessenen Durchsatz, kanonischen Kern, geschlossenen MVP-Scope oder
integrierte Abnahme erstellt. D-055 hat ihre Verbindlichkeit aufgehoben.
D-056 schließt jetzt den Scope, liefert aber noch keine Durchsatzmessung.

Historische Zahlen dürfen in älteren Berichten zur Nachvollziehbarkeit stehen,
werden jedoch nicht summiert, aktualisiert oder als Prognose weitergeführt.

## 2. Re-Estimate-Punkte

| Zeitpunkt | Zulässige Aussage | Erforderliche Daten |
|---|---|---|
| nach bestandenem G2 | **Aufwandsspanne**, keine Kalenderzusage | tatsächlicher G0–G2-Durchsatz, Rework, offene G3–G5-Tasks, Teamkapazität |
| nach bestandenem G4 | **Kalenderkorridor** für G5/MS-1 | integrierter Produktionsscope, Defekttrend, Test-/Matchdurchsatz, G5-Restarbeit |
| nach bestandenem G5 | Post-MVP-Entscheidung | MS-1-Retrospektive, neue D-ID, neue Scope-/Kapazitätsanalyse |

Die Spanne nach G2 enthält P50/P80-Annahmen und dokumentiert Team-,
Hardware- und CI-Kapazität getrennt. Als Durchsatz zählen nur durch den
subject-unabhängigen D-066-Receipt-Vertrag autorisierte Gate-Ergebnisse mit
vollständiger geordneter Receipt-Kette. Schema 1.2/1.3 ohne Receipt ist nur
eine Integritätsvorstufe und
liefert keine zulässige Durchsatzbasis. Ein Kalenderkorridor vor G4 ist
verboten.

## 3. Aktive Reihenfolge

`G0-A1 Integrity → G0-A2 Receipt → späteres sauberes Subject für G0-B/G0 → G1/V1–V5a → G2 → G3/V5b → G4 → G5`

Jeder Schritt bleibt ergebnisorientiert. Parallelisierung darf die Reihenfolge
der Statusfreigaben nicht umgehen. G0-A1 und G0-A2 werden ohne
Gate-Fortschritt gemergt und können sich nicht selbst autorisieren.

## 4. Produktfragen

Q-018 (Preispunkt) und Q-019 (Telemetrie) bleiben offen. D-056 stellt
Telemetrie aus MS-1 zurück; weder Preis noch Backend werden in eine G0–G5-
Schätzung eingerechnet. Steam, Cloud, Kampagne und Online sind ebenfalls
Post-MVP.

## Offene Punkte

- Kein belastbarer Durchsatz vor G2.
- Kein belastbarer Kalenderkorridor vor G4.
- Post-MVP-Scope ist absichtlich nicht geplant.

## Nächste Schritte

1. G0-A1 ohne Gate-Fortschritt mergen und G0-A2 separat implementieren.
2. Erst am nachfolgenden sauberen Subject autorisierte G0–G2-Messdaten
   sammeln.
3. Nach G2 eine neue Aufwandsspanne mit Annahmen dokumentieren.
4. Nach G4 erstmals einen G5-/MS-1-Kalenderkorridor erstellen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-24 | Erstfassung Sprint 6: Gesamtaufwand (445 PT), Phasenplan 2026–2028, Q-018 (Preispunkt 29,99–39,99 €) und Q-019 (Opt-in Telemetrie) geschlossen | Producer / Executive Producer |
| 1.1.0 | 2026-07-24 | Sprint-6-Schätzung durch D-055 als unbelegte historische Annahme eingestuft; Roadmap bis zur Recovery-Rebaseline entfristet | Producer / Executive Producer |
| 2.0.0 | 2026-07-24 | Aktive 445-PT-/Kalenderplanung entfernt; Aufwandsspanne nach G2 und Kalenderkorridor nach G4 festgelegt | Producer / Executive Producer |
| 2.1.0 | 2026-07-24 | D-063-autorisierte Schema-1.2-Evidence als einzige zulässige Durchsatzbasis festgelegt | Producer / Executive Producer |
| 2.2.0 | 2026-07-24 | D-064: Durchsatzbasis auf subject-unabhängig autorisierte Schema-1.3-Evidence nach zweistufigem G0-A-Bootstrap begrenzt | Producer / Executive Producer |
| 2.3.0 | 2026-07-25 | D-066: Durchsatzbasis auf abgeschlossene zweiphasige Receipt-Autorisierung nach G0-A1/G0-A2 begrenzt | Producer / Executive Producer |
