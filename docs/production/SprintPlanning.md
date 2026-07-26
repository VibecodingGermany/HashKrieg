# Sprint-Planung

**Version:** 2.5.0 | **Status:** Sprint 7 gestartet – G0-A1 Mergekandidat, G0-A2 offen | **Verantwortungsbereich:** Executive Producer / Producer / Project Owner | **Sprint:** 7

## Zweck

Definiert Sprintziele und Exit-Kriterien. Ein Sprintstatus ist nur eine
Arbeitszuordnung; Meilensteine und Gates werden ausschließlich durch
schema- und semantikvalide Evidenz erreicht.

## Abhängigkeiten

- [DecisionLog.md](DecisionLog.md) – D-055 bis D-066
- [MVPRecoveryPlan.md](MVPRecoveryPlan.md) – führender Sprint-7-Ablauf
- [Milestones.md](Milestones.md) – MS-0/MS-1
- [RiskAnalysis.md](RiskAnalysis.md)
- [../meta/DocumentationStandard.md](../meta/DocumentationStandard.md)

## Sprint-Definitionen

| Sprint | Thema | Ergebnis | Status |
|---|---|---|---|
| 0 | Projektinitialisierung | Wissensbasis, Analyse, Wiki-Standard | abgeschlossen |
| 1 | Research | technische und marktbezogene Alternativen | abgeschlossen |
| 2 | Game Design | GDD-Zielbild | abgeschlossen |
| 3 | Technical Design | technische Zielarchitektur | abgeschlossen |
| 4 | Architecture Review | Review-Befunde und D-043–D-052 | abgeschlossen |
| 5 | Asset Audit | Register, Lizenzen, Beschaffungsstrategie | abgeschlossen |
| 6 | Produktionsplanung | alte Abschlussbehauptung widerrufen; durch D-055 beendet und mit D-056–D-066 ersetzt | **beendet / ersetzt** |
| 7 | Implementierungs-Recovery | Gates G0–G5 ohne Statusvorgriff | **gestartet – G0-A1 Mergekandidat, G0-A2 offen** |

## Sprint 7 – Arbeitsvertrag

Sprint 7 schließt Gates in dieser Reihenfolge:

1. G0-A1 integrity-only Trusted-Gate-Grundlage,
2. G0-A2 zweiphasiger Receipt-Authorizer,
3. G0-B reproduzierbare Plattform,
4. G1 kanonischer Kern und V1–V5a,
5. G2 Player-Graybox-Kern,
6. G3 KI/Fortsetzung/V5b,
7. G4 exakter Produktionsumfang und
8. G5 eingefrorene Abnahme.

Sprint 7 ist gestartet. G0-A1 ist Mergekandidat, G0-A2 bleibt blockierende
Folgearbeit. G0-B-Code darf parallel vorbereitet werden, aber erst nach
G0-A2 einen Gate-Status beanspruchen. „Freigegeben“ bezeichnet Arbeitsscope,
nicht ein bestandenes Gate. MS-0 und MS-1 sind nicht erreicht.

## Sprint-Abschluss-Ritual

1. vollständige Dokumentation des tatsächlichen Ergebnisses,
2. unabhängiges read-only Review statt Autoren-Selbstfreigabe in Solo-/KI-Modus,
3. Architecture Review,
4. Update [RiskAnalysis.md](RiskAnalysis.md),
5. Qualitätsbewertung gegen die Gate-Kriterien,
6. Update [OpenQuestions.md](OpenQuestions.md),
7. begründetes GO/NO-GO/Anpassung,
8. Sprint-Bericht, Wiki-Index und `[Unreleased]`,
9. schema- und semantikvalide Evidence für jeden beanspruchten Gate-Status.

Punkt 9 verlangt nach G0-A2 kriterienspezifische Check-Artefakte, getrennte
Subject-/Evidence-Carrier-/Trusted-Identitäten und die vollständige
append-only Receipt-Kette aus dem geschützten `quality-gate`. Schema 1.2/1.3
und G0-A1 sind nur Integritätsvorstufen und können keinen Pass autorisieren.
Performance-Schwellen werden in drei getrennten Läufen pro Lauf und
kombiniert sowie gegen die gebundene Windows- oder Mac-Umgebung geprüft.

Sobald mindestens zwei aktive menschliche Maintainer existieren, ist zusätzlich
eine zweite menschliche Freigabe Pflicht.

## Offene Punkte

- Q-018 und Q-019 sind offen, aber durch D-056 nicht MS-1-blockierend.
- Sprint 7 hat keine Kalenderdauer; [Roadmap.md](Roadmap.md) definiert die
  Re-Estimate-Zeitpunkte.

## Nächste Schritte

1. G0-A1 ohne Gate-Fortschritt geschützt mergen.
2. G0-A2 mit zweiphasigem Receipt, Umgebungsbindung und Negative Controls
   separat implementieren und adversarial prüfen.
3. G0-B parallel vorbereiten, aber erst nach G0-A2 am sauberen Subject
   belegen.
4. Keine Evidence-Platzhalter und keine Vorab-Fertigmeldungen erzeugen.
5. Erst nach bestandenem G0 zu G1 wechseln.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-21 | Sprint-Definitionen 0–7 verabschiedet (Sprint 0) | Executive Producer |
| 1.1.0 | 2026-07-21 | Sprints 1–2 als abgeschlossen markiert, Sprint 3 GO; Exit-Kriterium Sprint 3 um Q-020 ergänzt | Executive Producer |
| 1.2.0 | 2026-07-21 | Sprint 3 als abgeschlossen markiert, Sprint 4 GO | Executive Producer |
| 1.3.0 | 2026-07-21 | Sprint 4 als abgeschlossen markiert, Sprint 5 (Asset Audit) GO | Executive Producer |
| 1.4.0 | 2026-07-22 | Sprint 5 (Asset Audit) als abgeschlossen markiert, Sprint 6 (Produktionsplanung) GO | Executive Producer |
| 1.5.0 | 2026-07-24 | Inhaberentscheidung D-054 (0 € Open-Source & KI-Pipeline, Q-035 geschlossen) in Sprint-6-Vorbereitung eingetragen | Executive Producer |
| 1.6.0 | 2026-07-24 | Sprint 6 (Produktionsplanung) als abgeschlossen markiert, Sprint 7 (Implementierung) GO | Executive Producer |
| 1.7.0 | 2026-07-24 | Sprint-6-Abschluss und pauschales Sprint-7-GO durch D-055 zurückgezogen; Sprint 7 auf Recovery-Gates G0–G5 umgestellt | Executive Producer |
| 2.0.0 | 2026-07-24 | Sprint 7 auf D-056–D-061, G0-offen und evidence-basierte Exit-Regeln rebaselined | Executive Producer / Producer / Project Owner |
| 2.1.0 | 2026-07-24 | Evidence-Semantikprüfung und getrennte G0-Negativkontrollen in den Sprintvertrag aufgenommen | Executive Producer / Producer / Lead QA Engineer |
| 2.1.1 | 2026-07-24 | Sprint 6 als beendet/ersetzt und Sprint 7 mit ausschließlich G0 als gestartet klargestellt | Executive Producer / Project Owner |
| 2.2.0 | 2026-07-24 | D-062-Szenarioschwellen und Same-Subject-Vorgängergate-Kette in den Sprintabschluss aufgenommen | Executive Producer / Lead QA Engineer |
| 2.3.0 | 2026-07-24 | D-063-Schema 1.2, kanonische Check-Artefakte, Drei-Lauf-Messung und Protected-CI-Trust als Sprint-7-Exit ergänzt | Executive Producer / Lead QA Engineer |
| 2.4.0 | 2026-07-24 | D-064 Trusted-Gate-Bootstrap als G0-A vor die Plattformarbeit gestellt und Schema 1.2 für Pass-Autorisierung gesperrt | Executive Producer / Lead QA Engineer |
| 2.5.0 | 2026-07-25 | D-066: G0-A1/G0-A2 getrennt, parallele G0-B-Vorbereitung ohne Gate-Vorgriff erlaubt und Receipt-Exit verankert | Executive Producer / Lead QA Engineer |
