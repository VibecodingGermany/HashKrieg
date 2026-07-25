# Sprint-6-Bericht – Produktionsplanung

**Version:** 2.3.0 | **Status:** historischer Bericht – durch D-055 beendet/ersetzt | **Verantwortungsbereich:** Executive Producer | **Sprint:** 6

## Zweck

Dokumentiert die Sprint-6-Planung und ihre nachträgliche Korrektur. Sprint 6
ist kein offener Vorgängersprint mehr, sondern durch D-055 beendet und durch
den Recovery-Plan ersetzt. Der Bericht ist keine Freigabe für MS-0, MS-1,
Alpha oder einen Kalender. Führend sind
D-055 bis D-064 und der
[MVP-Recovery-Plan](../MVPRecoveryPlan.md).

## Abhängigkeiten

- [../DecisionLog.md](../DecisionLog.md) – D-055 bis D-064
- [../MVPRecoveryPlan.md](../MVPRecoveryPlan.md)
- [../MVPContentManifest.md](../MVPContentManifest.md)
- [../Milestones.md](../Milestones.md)
- [../Roadmap.md](../Roadmap.md)
- [../ImplementationAudit_2026-07-24.md](../ImplementationAudit_2026-07-24.md)

## 1. Historisches Ergebnis

Sprint 6 erzeugte die ersten Fassungen von Milestones und Roadmap sowie eine
445-PT-/Kalenderannahme. Der damalige Bericht erklärte:

- Sprint 6 für abgeschlossen,
- R-16 für mitigiert,
- Q-018/Q-019 ohne D-ID für geschlossen und
- Sprint 7 pauschal für GO.

Diese Aussagen waren nicht hinreichend belegt.

## 2. Korrektur vom 2026-07-24

Der Implementierungs-Audit und D-055 widerriefen Abschluss und GO. D-056–D-064
ersetzen die fehlerhafte Planungsbasis:

| Frühe Sprint-6-Annahme | Korrigierter Stand |
|---|---|
| Vollumfang als MVP | dependency-closed MS-1 aus D-056 |
| Float bis Beta | kanonisches Q16.16 ab G1, D-057 |
| unklare 6/8-Spieler- und Cachebudgets | feste MS-1-Kappen, D-058 |
| Integrationsbranch ab Sprint 7 | geschütztes `main` + kurze Branches, D-059 |
| ältere Engine-Linie | exakter Pin 6000.5.4f1, D-060 |
| Berichte/Dateien als Fortschritt | ausführbare Gates und append-only Evidence, D-061 |
| selbstdeklarierte/no-op Evidence | Schema 1.2 und kanonische Integritätschecks, aber keine Pass-Autorisierung, D-063/D-064 |
| Subject autorisiert sein eigenes Tooling | zweistufiger subject-unabhängiger Schema-1.3-Trusted-Gate-Bootstrap, D-064 |

MS-0 und MS-1 sind weiterhin nicht erreicht; G0 ist offen.

## 3. Korrigierte Qualitätsbewertung

| Bereich | Bewertung |
|---|---|
| ursprüngliche Scope-Kohärenz | unzureichend |
| ursprüngliche Schätzbasis | unzureichend |
| Entscheidungsdisziplin Q-018/Q-019 | unzureichend |
| historische Nachvollziehbarkeit | erhalten |
| Recovery-Vertrag | vollständig dokumentiert, noch nicht implementiert |

Das Vorliegen dieser Dokumente ist selbst kein Gate-Nachweis.

## 4. Risiko- und Fragenstand

- R-16 bleibt aktiv; Aufwandsspanne erst nach G2, Kalenderkorridor erst nach G4.
- R-17 bleibt aktiv, bis G0-A ohne Selbstautorisierung gemergt und Schema 1.3
  an einem nachfolgenden sauberen Subject real bewiesen ist.
- Q-031–Q-034 und Q-038/Q-039 sind durch D-056–D-061 geschlossen.
- Q-018 und Q-019 bleiben offen und blockieren MS-1 nicht.

## 5. GO/NO-GO

Sprint 7 ist gestartet. Die erste Coding-Arbeit ist G0-A
Trusted-Gate-Bootstrap, danach folgt G0-B Plattformbasis. Die
Trust-Bundle-Änderung selbst erzielt keinen Gate-Fortschritt; erst ein
nachfolgender sauberer Subject-Commit darf G0 beweisen. Es gibt keine
pauschale Freigabe für G1–G5.

## Offene Punkte

- Keine weiteren Sprint-6-Entscheidungen. Post-MVP-Preis und Telemetrie bleiben
  Q-018/Q-019.

## Nächste Schritte

1. G0-A gemäß [../MVPRecoveryPlan.md](../MVPRecoveryPlan.md) ohne
   Gate-Fortschritt herstellen; G0-B erst danach bearbeiten.
2. Keine 445-PT- oder Kalenderzahl als aktive Planung weiterverwenden.
3. Nach G2 Aufwandsspanne und nach G4 Kalenderkorridor neu erstellen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-24 | Sprint 6 (Produktionsplanung) abgeschlossen, GO für Sprint 7 (Implementierung) | Executive Producer |
| 1.1.0 | 2026-07-24 | Abschluss und GO durch D-055 widerrufen; Bericht als historische, nicht führende Momentaufnahme markiert | Executive Producer |
| 2.0.0 | 2026-07-24 | Historische Behauptungen gegen D-056–D-061 korrigiert und nächste Schritte auf G0/G2/G4 ausgerichtet | Executive Producer |
| 2.0.1 | 2026-07-24 | Sprint 6 als beendet/ersetzt und den G0-begrenzten Start von Sprint 7 klargestellt | Executive Producer / Project Owner |
| 2.1.0 | 2026-07-24 | D-062-Evidence-Härtung in die führende Recovery-Baseline aufgenommen | Executive Producer / Lead QA Engineer |
| 2.2.0 | 2026-07-24 | D-063-Authentizitäts- und Drei-Lauf-Härtung in die Recovery-Baseline aufgenommen | Executive Producer / Lead QA Engineer |
| 2.3.0 | 2026-07-24 | D-064-Fail-Closed-Autorisierung und G0-A-vor-G0-B-Reihenfolge in die führende Recovery-Baseline aufgenommen | Executive Producer / Lead QA Engineer |
