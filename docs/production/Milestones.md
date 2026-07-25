# Meilenstein-Planung

**Version:** 2.4.0 | **Status:** verbindlicher Recovery-Stand – G0-A aktiv, Autorisierung gesperrt | **Verantwortungsbereich:** Producer / Game Director / Lead QA Engineer | **Sprint:** 7

## Zweck

Definiert die einzigen aktuell qualifizierbaren Meilensteine MS-0 und MS-1 und
ordnet ihnen ausführbare Gates zu. Ein Meilenstein ist eine nachgewiesene
Ergebnisstufe, keine Phase, Dateiliste oder Prozentangabe.

## Abhängigkeiten

- [MVPRecoveryPlan.md](MVPRecoveryPlan.md) – führende Gate-Kriterien
- [MVPContentManifest.md](MVPContentManifest.md) – MS-1-Inhalt
- [DecisionLog.md](DecisionLog.md) – D-055 bis D-064
- [Roadmap.md](Roadmap.md) – Schätz- und Terminregeln
- [`../../quality/schemas/GateEvidence.schema.json`](../../quality/schemas/GateEvidence.schema.json)
- [`../../quality/scripts/validate_gate_evidence.py`](../../quality/scripts/validate_gate_evidence.py)

## 1. Nachweisstatus

| Meilenstein | Definition | Status am 2026-07-24 |
|---|---|---|
| MS-0 | G0 + G1 + V1–V5a | **offen / nicht erreicht** |
| MS-1 | G2 + G3 + G4 + G5 nach erreichtem MS-0 | **nicht erreicht** |
| Alpha und später | neue Planung erst nach MS-1 | **nicht begonnen** |

Vorhandene Prototypklassen, Tests oder Assets zählen nur als Input. Sie erfüllen
keinen Eintrag dieser Tabelle ohne schema- und semantikvalide Gate-Evidence.
Schema 1.2 kann dabei nur Integrität prüfen; bis zum zweistufigen G0-A-
Bootstrap bleibt jeder Pass mit `E_AUTHORIZATION_BOOTSTRAP` gesperrt.

## 2. MS-0 – belastbare Implementierungsbasis

MS-0 besteht ausschließlich aus:

| Bestandteil | Ergebnis |
|---|---|
| G0 | G0-A Trusted-Gate-Bootstrap, danach G0-B reproduzierbare Plattform und grüne Basis |
| G1 | kanonischer Fixed-Point-/Command-/State-/Snapshot-/Replay-Kern |
| V1 | exakte Cross-Plattform-Hashes und finale Bytes |
| V2 | tragfähiger URP-Renderingpfad |
| V3 | tragfähiger Animationspfad |
| V4 | Pathfinding-P95 ≤4 ms im 500-Agenten-Spike |
| V5a | Pre-Combat-SpatialHash/FoW/Commands, Rest-Sim-P95 ≤3 ms |

**MS-0-Exit:** G0-A wurde zuvor als nicht selbstautorisierende Trust-Bundle-
Änderung ohne Gate-Fortschritt gemergt. G0-B und G1/V1–V5a sind an einem
nachfolgenden sauberen Subject-Commit/-Tree mit Schema 1.3 bestanden. Der
subject-unabhängige externe Trust-Kontext autorisiert die vollständige
geordnete `authorizedEvidence`-Kette von G0 bis zum aktuellen Gate. Schema 1.2
erfüllt diesen Exit nicht. MS-0 enthält noch kein MVP-Versprechen.

## 3. MS-1 – Closed-Core MVP

MS-1 ist der exakte Inhalt aus
[MVPContentManifest.md](MVPContentManifest.md) und umfasst:

| Gate | Ergebnis |
|---|---|
| G2 | integrierter Player-Graybox-Kern einschließlich vollständigem Aetherium |
| G3 | gefilterte KI, Replay-/Save-Fortsetzung, FoW-Metamorphics und V5b |
| G4 | exakter Produktionsinhalt, Glutrinne, UI, Settings, Persistence, Accessibility und Provenienz |
| G5 | eingefrorene automatisierte, manuelle und Performance-Abnahme |

**MS-1-Exit:** subject-unabhängig autorisierter Schema-1.3-G5-`pass` am
abgenommenen SHA mit vollständiger geordneter `authorizedEvidence`-Kette bis
G0. Die Content-Grenze sind zwei Fraktionen, eine Karte, neun Gebäude- und
acht Einheitenrollen je Fraktion sowie 100 Produktionseinheiten. Der
500-Agenten-Lauf bleibt synthetische Architekturreserve.

## 4. Gate-/Meilenstein-Matrix

| Kriterium | MS-0 | MS-1 |
|---|:---:|:---:|
| Engine/Toolchain reproduzierbar | Pflicht | geerbt |
| kanonischer deterministischer Kern | Pflicht | geerbt |
| Cross-Plattform- und Spike-Nachweise | Pflicht | regressionsfrei |
| Player-Kernloop | – | Pflicht |
| Skirmish-KI | – | Pflicht |
| exakter MVP-Content | – | Pflicht |
| UI-/Save-/Accessibility-Produktminimum | – | Pflicht |
| eingefrorene G5-Abnahme | – | Pflicht |

## 5. Post-MVP

Die alten MS-2- bis MS-4-Inhalte sind historische Produktideen, keine aktive
Roadmap. Evolvierte, zusätzliche Karten, Multiplayer, Kampagne, Steam und
sonstige in D-056 zurückgestellte Funktionen werden erst nach G5 mit einer neuen
D-ID, Kapazitätsschätzung und Akzeptanzdefinition geplant. Es gibt aktuell
keinen Alpha-Termin und kein Alpha-GO.

## Offene Punkte

- Q-018 und Q-019 bleiben nicht blockierende Produktfragen.
- Umfang und Benennung künftiger Meilensteine werden erst nach MS-1 entschieden.

## Nächste Schritte

1. G0-A aus [MVPRecoveryPlan.md](MVPRecoveryPlan.md) ohne Gate-Fortschritt
   herstellen und geschützt mergen.
2. Schema 1.3 und den vollständigen Trust-Kontext erst an einem nachfolgenden
   sauberen Subject für G0-B/G0 verwenden.
3. Nach G2 Aufwandsspannen, nach G4 frühestens Kalenderziele neu schätzen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-24 | Erstfassung Sprint 6: Meilensteine MS-0 bis MS-4 definiert, Qualitäts-Gates und Feature-Matrix verankert | Producer / Game Director |
| 1.1.0 | 2026-07-24 | Tatsächlichen Nachweisstatus ergänzt; MS-0/MVP/Alpha durch D-055 zurückgestuft und Zukunftsscope suspendiert | Producer / Game Director |
| 2.0.0 | 2026-07-24 | Meilensteine auf nachweisbare MS-0-/MS-1-Gate-Matrix D-061 rebaselined; Post-MVP entplant | Producer / Game Director / Lead QA Engineer |
| 2.1.0 | 2026-07-24 | Semantikvalidierung als zwingende zweite Evidence-Prüfstufe verankert | Producer / Lead QA Engineer |
| 2.2.0 | 2026-07-24 | D-062-Same-Subject-Vorgängergate-Kette als MS-0-/MS-1-Exit verankert | Producer / Lead QA Engineer |
| 2.3.0 | 2026-07-24 | D-063-Schema 1.2 und Protected-CI-Trust-Autorisierung als Meilenstein-Exit ergänzt | Producer / Lead QA Engineer |
| 2.4.0 | 2026-07-24 | D-064: Schema 1.2 als Integritätsvorstufe zurückgestuft und zweistufigen Schema-1.3-Trust-Bootstrap als Meilenstein-Voraussetzung ergänzt | Producer / Lead QA Engineer |
