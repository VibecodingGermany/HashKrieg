# Module und Phasen – tatsächlicher Implementierungsstand

**Version:** 1.0.0 | **Status:** Recovery-Baseline | **Verantwortungsbereich:** Lead Technical Director / Producer | **Sprint:** 7

## Zweck

Zeigt den nachweisbaren Implementierungsstand nach dem Audit auf Commit
`460290e`. Statuswerte beruhen auf integrierten Ergebnissen und Gate-Evidenz,
nicht auf vorhandenen Dateien oder APIs.

## Abhängigkeiten

- [production/ImplementationAudit_2026-07-24.md](production/ImplementationAudit_2026-07-24.md)
- [production/MVPRecoveryPlan.md](production/MVPRecoveryPlan.md)
- [production/DecisionLog.md](production/DecisionLog.md) – D-055
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
[MVPRecoveryPlan G0](production/MVPRecoveryPlan.md): reproduzierbare grüne
Baseline. Feature- und Alpha-Expansion sind bis zum bestandenen MVP-Gate G5
gestoppt.

## Offene Punkte

- Q-038: reduzierten MVP-Content-Scope ratifizieren.
- Q-039: Fixed-Point-/Cross-Plattform-Gate entscheiden.
- Uncommitteten Shader-Stand separat reviewen.

## Nächste Schritte

1. Roten Netzwerkpaket-Test und Build-Reproduzierbarkeit in G0 beheben.
2. Code-CI als Merge-Gate aktivieren.
3. Danach G1 für Command/State/Hash/Replay öffnen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-24 | Halluzinierte MS-0-/MVP-/Alpha-Fertigmeldungen durch evidenzbasierten Iststand ersetzt | Lead Technical Director / Producer |
