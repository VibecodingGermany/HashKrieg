# Risikoanalyse

**Version:** 2.5.0 | **Status:** aktiv – G0-A2 offen | **Verantwortungsbereich:** Executive Producer / Lead Technical Director | **Sprint:** 7

## Zweck

Führt die produkt-, technik- und prozesskritischen Risiken mit aktueller
Mitigation. Ein dokumentierter Vertrag reduziert ein Risiko erst, wenn sein
zugehöriges Gate bestanden ist.

## Abhängigkeiten

- [DecisionLog.md](DecisionLog.md) – D-055 bis D-066
- [MVPRecoveryPlan.md](MVPRecoveryPlan.md) – operative Gates
- [Roadmap.md](Roadmap.md) – Schätzregeln
- [../tech/SimulationCore.md](../tech/SimulationCore.md),
  [../tech/PerformanceBudget.md](../tech/PerformanceBudget.md) und
  [../tech/Testing.md](../tech/Testing.md)

## Bewertungsskala

Wahrscheinlichkeit und Auswirkung sind `niedrig`, `mittel` oder `hoch`.
`reduziert` bezeichnet eine vertragliche Mitigation; `mitigiert` setzt einen
bestandenen Nachweis voraus.

## Risikoregister

| ID | Risiko | W | A | Aktuelle Mitigation | Status |
|---|---|---:|---:|---|---|
| R-01 | Scope-Explosion | mittel | hoch | D-056 schließt MS-1 auf 2 Fraktionen, 1 Karte, 9 Gebäude- und 8 Einheitenrollen; maschinenlesbares Manifest und G4-Vollständigkeitscheck | **reduziert, aktiv bis G5** |
| R-02 | Sim-/MP-Architektur zu spät | niedrig | hoch | D-057 friert Command-, State-, Snapshot- und Replay-Vertrag ab G1 ein; Online bleibt Post-MVP | reduziert, G1 offen |
| R-03 | Pathfinding skaliert nicht | mittel | hoch | 128²-Grid, Cache ≤32/8 MiB, deterministische Eviction; V4/V5a mit 500 Agenten und Path P95≤4 ms | **aktiv bis V4/V5a** |
| R-04 | visuelle Inkohärenz | mittel | mittel | CC0/KI-Provenienz, einheitlicher URP-Materialstandard, G4-Usability/Provenienz | aktiv |
| R-05 | Umgebungszerstörung als Kostentreiber | niedrig | hoch | D-056 erlaubt in MS-1 nur Aetherium als veränderbare Umwelt | reduziert |
| R-06 | Living-Docs-Disziplin bricht | mittel | mittel | Version/History, Docs-Check, Evidence-Autorität und `[Unreleased]` | aktiv |
| R-07 | Lizenz-/Provenienzfehler | mittel | mittel | 0-€-Pipeline, Lizenzregister und G4-Provenienzpflicht | aktiv |
| R-08 | WebGL verbaut Architektur | niedrig | mittel | keine Leitplattform; Desktop-Pins und Parsergrenzen führen | aktiv |
| R-09 | Bestätigungsfehler mangels Peer-Review | mittel | hoch | unabhängiges read-only Review; zweite menschliche Freigabe ab zwei aktiven Maintainers | aktiv |
| R-10 | Online-/Serverfundament verdrängt Solo-Kern | niedrig | hoch | D-056 stellt Online/Koop/PvP/Ranked zurück | reduziert |
| R-11 | Unity-Plattform-/Upgrade-Risiko | mittel | mittel | D-060 pinnt 6000.5.4f1 Revision d550df8bd089; keine automatischen Upgrades; Sim-Kern Unity-frei | **aktiv, Re-Eval nur nach G5/Blocker** |
| R-12 | Managed/Burst-Paritätsbruch | niedrig | hoch | MS-1 shippt Managed, Burst aus; Aktivierung nur nach exakter Feld-/Hash-/Byteparität | **für MS-1 stark reduziert, Post-MVP offen** |
| R-13 | Bus-Faktor / Einzelmaintainer | hoch | hoch | Living Docs, reproduzierbare Befehle, unabhängige Review-Rolle, Evidence mit Clean-Clone-Reproduktion | aktiv |
| R-14 | ARM↔x64-Determinismus scheitert | mittel | hoch | Q16.16 ab G1, identische Quellen/Defines, V1 über 10.000 Ticks mit exakten Hashes und finalen Bytes | **aktiv bis V1** |
| R-15 | KI-generierter Code verletzt Sim-Vertrag | mittel | hoch | Architekturchecks, autoritative Float-/Unity-Verbote, Golden/Metamorphics, Coverage und Reviewer≠Writer | aktiv |
| R-16 | keine belastbare Zeit-/Kapazitätsbasis | hoch | hoch | keine aktive 445-PT-/Kalenderannahme; Aufwandsspanne erst nach G2, Kalenderkorridor erst nach G4 | **aktiv** |
| R-17 | falsche Fertigmeldung durch Struktur oder widersprüchliche Evidence statt Ergebnis | hoch | hoch | D-055/D-061–D-064/D-066; G0-A1 ist fail-closed, G0-A2 verlangt abgeschlossene Receipts statt Selbstattestierung | **aktiv bis Quality-Gate und G5 bewiesen** |
| R-18 | Gate-Subject schwächt eigene Prüftools oder misst auf unzulässiger Umgebung | hoch | hoch | D-064/D-066: getrennte Subject-/Carrier-/Trusted-Identitäten, zweiphasige Receipts, gebundene Windows-/Mac-Profile und Manipulations-Negativtests | **aktiv bis G0-A2 plus nachfolgendes G0 bewiesen** |

## Schwerpunktmaßnahmen

### R-01 – Scope

Jede G2–G4-Anforderung muss auf eine ID in
[`mvp-v1.json`](../../quality/content/mvp-v1.json) zeigen. Ein neues Feature
ersetzt kein defektes Gate. Scope-Erweiterung braucht eine neue D-ID.

### R-03 und R-14 – technische Existenzrisiken

V1, V4 und V5a liegen vor Combat-/Content-Breite. Ein Fehlschlag führt zurück
zu Architektur/Synthese; Schwellen werden nicht nachträglich als
„diagnostisch“ umgedeutet.

### R-16 bis R-18 – Planungsintegrität

Durchsatz wird nur aus tatsächlich abgeschlossenen Gate-Arbeiten ermittelt.
Schema 1.2/1.3 und G0-A1 prüfen Integrität, bleiben aber durch
`E_AUTHORIZATION_BOOTSTRAP` von jeder Pass-Autorisierung ausgeschlossen.
G0-A2 muss Subject, Evidence-Carrier und Trusted Tooling trennen, die
vollständige Receipt-Kette aus bereits abgeschlossenen Läufen autorisieren
und jede Performance-Messung an das exakte Windows- oder Mac-Profil binden.
Erst ein nachfolgender sauberer Subject-Commit darf damit G0 belegen.
Relevante Änderungen entwerten ältere Nachweise. Statusänderung ohne diese
Kette ist ein P1-Prozessdefekt.

## Offene Punkte

- Q-018 und Q-019 bleiben Produktfragen ohne MS-1-Gatewirkung.
- Die tatsächliche Eintrittswahrscheinlichkeit von R-03, R-14, R-17 und
  R-18 kann erst nach realen G0-/G1-Nachweisen gesenkt werden.

## Nächste Schritte

1. G0-A1 geschützt mergen und G0-A2 mit zweiphasigem Receipt,
   Umgebungsbindung und Negativkontrollen separat implementieren.
2. R-03/R-14 nach V1/V4/V5a mit Rohdaten neu bewerten.
3. R-18 erst nach G0-A2 plus nachfolgendem G0, R-16 erst nach G2 und R-17
   erst nach produktivem `quality-gate` abstufen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-21 | Initiales Risikoregister R-01 bis R-09 (Sprint 0) | Executive Producer |
| 1.1.0 | 2026-07-21 | R-10, R-11 neu; R-09 teilentschärft; Fortschritt R-02/R-03 (Sprint 1) | Executive Producer |
| 1.2.0 | 2026-07-21 | R-01 gesenkt (mittel/hoch), R-05 entschärft (Sprint-2-Scope-Entscheidungen D-007–D-032) | Executive Producer |
| 1.3.0 | 2026-07-21 | R-02 entschärft (D-033), R-03 reduziert (D-034), R-12 neu (Burst/Managed-Parität, D-037) – Sprint 3 | Executive Producer |
| 1.4.0 | 2026-07-21 | R-13, R-14, R-15, R-16 neu aufgenommen – Sprint-4-Korrekturlauf | Executive Producer |
| 1.5.0 | 2026-07-22 | R-04 und R-07 auf „mitigiert" gesenkt – Sprint 5 (Asset Audit) | Executive Producer |
| 1.6.0 | 2026-07-24 | R-16 (Zeit-/Kapazitätsrisiko) auf „mitigiert" gesenkt – Sprint 6 (Roadmap.md & Milestones.md, 445 PT) | Executive Producer |
| 1.7.0 | 2026-07-24 | R-16 wegen unbelegter Schätzbasis reaktiviert; R-17 für KI-bedingte falsche Fertigmeldungen aufgenommen | Executive Producer / Lead Technical Director |
| 2.0.0 | 2026-07-24 | R-01/R-03/R-11/R-12/R-14/R-16/R-17 auf D-056–D-061 und offene Gates rebaselined | Executive Producer / Lead Technical Director |
| 2.1.0 | 2026-07-24 | R-17 um semantisch widersprüchliche Evidence, Kriterienprofile und Negativkontrollen erweitert | Executive Producer / Lead QA Engineer |
| 2.2.0 | 2026-07-24 | R-17 mit D-062-Subject-Blob-, Szenariometrik- und Same-Subject-Gate-Ketten-Mitigation gehärtet | Executive Producer / Lead QA Engineer |
| 2.3.0 | 2026-07-24 | R-17 mit D-063-Check-Artefakten, Drei-Lauf-Messung, rekursivem Ajv und Protected-CI-Trust gehärtet | Executive Producer / Lead QA Engineer |
| 2.4.0 | 2026-07-24 | R-18 für selbstautorisierende Prüftools und ungebundene Messumgebungen aufgenommen; D-064-G0-A als Mitigation festgelegt | Executive Producer / Lead QA Engineer |
| 2.5.0 | 2026-07-25 | D-066: zirkuläre Selbstattestierung als aktiven R-17/R-18-Pfad erfasst und zweiphasige G0-A2-Receipts als Mitigation festgelegt | Executive Producer / Lead QA Engineer |
