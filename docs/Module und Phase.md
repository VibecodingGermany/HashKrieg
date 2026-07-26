# Module und Phasen – tatsächlicher Implementierungsstand

**Version:** 1.5.0 | **Status:** Recovery-Baseline – G0-A1 Mergekandidat, G0-A2 offen | **Verantwortungsbereich:** Lead Technical Director / Producer | **Sprint:** 7

## Zweck

Zeigt den nachweisbaren Implementierungsstand nach dem Audit auf Commit
`460290e`. Statuswerte beruhen auf integrierten Ergebnissen und Gate-Evidenz,
nicht auf vorhandenen Dateien oder APIs.

## Abhängigkeiten

- [production/ImplementationAudit_2026-07-24.md](production/ImplementationAudit_2026-07-24.md)
- [production/MVPRecoveryPlan.md](production/MVPRecoveryPlan.md)
- [production/DecisionLog.md](production/DecisionLog.md) – D-055 bis D-066
- [production/Milestones.md](production/Milestones.md)

## Aktueller Gesamtstatus

| Meilenstein | Behaupteter Altstatus | Nachweisbarer Status |
|---|---|---|
| MS-0 – Spike | abgeschlossen | **offen – Prototypen vorhanden, Gates nicht bestanden** |
| MS-1 – MVP | abgeschlossen | **nicht erreicht – kein integriertes spielbares Match** |
| MS-2 – Alpha | in Arbeit / Module 16–19 fertig | **nicht begonnen – nur isoliertes Scaffolding** |
| MS-3 – Beta | geplant | pausiert |
| MS-4 – Release | geplant | pausiert |

## Modulstatus

| Module | Bereich | Nachweisbarer Stand | Status |
|---|---|---|---|
| 1–3 | Core, Pathfinding, Entity/Movement | frühe Managed-Prototypen; Tick-/Determinismus-/Performance-Gates offen | **Prototyp** |
| 4 | Unity Gameplay Bridge | bindet nur Pathfinding und Movement ein | **unvollständig** |
| 5 | GameDatabase | Teilregistries; Building/Weapon und weitere D-049-Kategorien unvollständig | **Scaffolding** |
| 6 | Command Bus | Kernel verwirft fällige Commands statt sie zu dispatchen | **defekt / P0** |
| 7 | Combat | isolierter Default-Kampf ohne V5-Targeting/FoW-Nachweis | **Scaffolding** |
| 8 | Hash/Replay | mutierender/unvollständiger Hash; kein Replay-Playback | **defekt** |
| 9–12 | Economy, Bau, Produktion, Vision | isolierte Teilimplementierungen ohne Match-Integration | **Scaffolding** |
| 13 | Skirmish-KI | minimale Bau-/Produktionsdemo, kein vollständiges Spielverhalten | **Scaffolding** |
| 14 | UI | Auswahl-/Koordinaten-Hilfstypen, kein integriertes HUD | **Scaffolding** |
| 15 | Asset-Integration | Registry-API und Dummy-Test, keine produktiven Content-Assets | **Scaffolding** |
| 16 | Evolvierte | Biomasse-Regenerationsdemo | **Experiment; kein Alpha-Scope** |
| 17 | Commander/Doktrinen | aktive Ability-Demo widerspricht D-009 | **nicht freigegeben** |
| 18 | Relay | Serialisierung und In-Memory-Buffer, kein Netzwerktransport; Test rot | **defekt / kein Relay** |
| 19 | Maps | generischer Definitionstyp, keine drei Karten | **Scaffolding** |
| 20 | Shader | paralleler uncommitteter Arbeitsstand, nicht auditiert | **nicht bewertet** |

## Recovery-Status

Aktiver Scope ist ausschließlich
[MVPRecoveryPlan G0-A](production/MVPRecoveryPlan.md): der
fail-closed Trusted-Gate-Bootstrap. G0-A1 liefert nur Integrity; jeder
Pass-Versuch bleibt mit `E_AUTHORIZATION_BOOTSTRAP` gesperrt. G0-A2 muss den
zweiphasigen Receipt-Authorizer ergänzen. Erst danach darf G0-B einen
Gate-Status belegen; Feature- und Alpha-Expansion sind bis zum bestandenen
MVP-Gate G5 gestoppt.

## Offene Punkte

- Q-038 ist durch D-056 geschlossen; der reduzierte, abhängige MS-1-Umfang
  steht im [MVPContentManifest](production/MVPContentManifest.md).
- Q-039 ist durch D-057 und D-061 geschlossen; kanonischer Fixed-Point-State
  und ARM64↔x86_64-Nachweis sind verbindliche Gate-Anforderungen.
- Uncommitteten Shader-Stand separat reviewen.

## Nächste Schritte

1. G0-A1 einschließlich Gate-Runner ohne Gate-Fortschritt geschützt mergen.
2. G0-A2 als separaten Receipt-Authorizer implementieren und prüfen.
3. Am nachfolgenden sauberen Subject in G0-B roten Netzwerkpaket-Test und
   Build-Reproduzierbarkeit beheben und erst danach die vollständige Receipt-
   Kette samt `environmentId`-Bindung autorisieren.
4. G1 erst nach bestandenem G0 öffnen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-24 | Halluzinierte MS-0-/MVP-/Alpha-Fertigmeldungen durch evidenzbasierten Iststand ersetzt | Lead Technical Director / Producer |
| 1.1.0 | 2026-07-24 | Q-038/Q-039 gemäß D-056/D-057/D-061 geschlossen und Status auf G0 aktiv synchronisiert | Lead Technical Director / Producer |
| 1.2.0 | 2026-07-24 | D-062-Same-Subject-Gate-Kette als Statusvoraussetzung ergänzt | Lead Technical Director / Producer |
| 1.3.0 | 2026-07-24 | D-063-autorisierte Schema-1.2-Evidence als Statusvoraussetzung ergänzt | Lead Technical Director / Producer |
| 1.4.0 | 2026-07-24 | D-064: Schema 1.2 als Integritätsvorstufe und G0-A vor G0-B als aktive Recovery-Reihenfolge verankert | Lead Technical Director / Producer |
| 1.5.0 | 2026-07-25 | D-066: G0-A1-Integrity und G0-A2-Receipt-Autorisierung im tatsächlichen Recovery-Status getrennt | Lead Technical Director / Producer |
