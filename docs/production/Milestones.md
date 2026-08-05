# Meilenstein-Planung

**Version:** 3.0.0 | **Status:** verbindlich (Governance-Tier 1) | **Verantwortungsbereich:** Producer / Game Director | **Sprint:** 7

## Zweck

Definiert die Meilensteine MS-0 und MS-1 und beschreibt, woran man erkennt, dass
sie erreicht sind. Ein Meilenstein ist eine nachgewiesene Ergebnisstufe, keine
Phase, Dateiliste oder Prozentangabe.

Seit D-076 gilt **Governance-Tier 1**: Der Nachweis ist grüne CI plus eine
gespielte Runde, nicht eine autorisierte Receipt-Kette. Die Gate-Definitionen
G0–G5 bleiben als *inhaltliche Gliederung* nützlich – sie beschreiben weiterhin
gut, was in welcher Reihenfolge fertig werden sollte. Sie blockieren nur nichts
mehr.

## Abhängigkeiten

- [../../GOVERNANCE.md](../../GOVERNANCE.md) – was „erreicht" in Tier 1 heißt
- [MVPContentManifest.md](MVPContentManifest.md) – MS-1-Inhalt
- [ScopeLedger.md](ScopeLedger.md) – ehrliche Lückenliste gegen diesen Inhalt
- [MVPRecoveryPlan.md](MVPRecoveryPlan.md) – Gate-Inhalte (Evidenzvertrag schlafend)
- [DecisionLog.md](DecisionLog.md) – D-055 bis D-076
- [Roadmap.md](Roadmap.md) – Schätz- und Terminregeln

## 1. Nachweisstatus

| Meilenstein | Definition | Status am 2026-08-06 |
|---|---|---|
| MS-0 | belastbarer deterministischer Kern (G0 + G1 + V1–V5a) | **offen** – Kern läuft, Cross-Plattform- und Perf-Nachweise stehen aus |
| MS-1 | Closed-Core MVP (G2 + G4 + G5 nach MS-0) | **nicht erreicht** – Lücken siehe [ScopeLedger.md](ScopeLedger.md) |
| Alpha und später | neue Planung erst nach MS-1 | **nicht begonnen** |

Vorhandene Prototypklassen, Tests oder Assets zählen als Input, nicht als
Erfüllung. Ein Meilenstein gilt als erreicht, wenn die CI grün ist **und** ein
Mensch die betroffene Sache im laufenden Spiel gesehen und notiert hat. Das ist
kein weicheres Kriterium als vorher – es ist ein anderes: Es fängt genau den
Fehler, wegen dem das Gate-Regime entstand (ein Modul, das nur auf dem Papier
existiert, überlebt keine Spielrunde), ohne einen Beweisapparat zu verlangen,
den zwei Entwickler nie fertigbauen.

## 2. MS-0 – belastbare Implementierungsbasis

MS-0 besteht ausschließlich aus:

| Bestandteil | Ergebnis |
|---|---|
| G0 | G0-A1 Integrity, G0-A2 Receipt-Authorizer, danach G0-B reproduzierbare Plattform und grüne Basis |
| G1 | kanonischer Fixed-Point-/Command-/State-/Snapshot-/Replay-Kern |
| V1 | exakte Cross-Plattform-Hashes und finale Bytes |
| V2 | tragfähiger URP-Renderingpfad |
| V3 | tragfähiger Animationspfad |
| V4 | Pathfinding-P95 ≤4 ms im 500-Agenten-Spike |
| V5a | Pre-Combat-SpatialHash/FoW/Commands, Rest-Sim-P95 ≤3 ms |

**MS-0-Exit:** Die Simulationstests sind in CI grün, V1–V5a sind je einmal
gemessen und das Ergebnis ist im PR oder im
[GrayboxLog](GrayboxLog.md) notiert. MS-0 enthält noch kein MVP-Versprechen.

Die Messungen brauchen keine `environmentId`-Bindung und keine drei
120-s-Läufe mehr; eine reproduzierbare Zahl mit der Angabe, auf welcher Maschine
sie entstand, genügt. Der strenge Vertrag dafür schläft in
[`../../quality/README.md`](../../quality/README.md).

## 3. MS-1 – Closed-Core MVP

MS-1 ist der exakte Inhalt aus
[MVPContentManifest.md](MVPContentManifest.md) und umfasst:

| Gate | Ergebnis |
|---|---|
| G2 | integrierter Player-Graybox-Kern einschließlich vollständigem Aetherium |
| G3 | gefilterte KI, Replay-/Save-Fortsetzung, FoW-Metamorphics und V5b |
| G4 | exakter Produktionsinhalt, Glutrinne, UI, Settings, Persistence, Accessibility und Provenienz |
| G5 | eingefrorene automatisierte, manuelle und Performance-Abnahme |

**MS-1-Exit:** Der [ScopeLedger](ScopeLedger.md) enthält keine offene Zeile mehr
gegen [`mvp-v1.json`](../../quality/content/mvp-v1.json), die CI ist grün, und
eine vollständige Partie wurde von Anfang bis Sieg durchgespielt und
protokolliert. Die Content-Grenze sind
zwei Fraktionen, eine Karte, neun Gebäude- und
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

1. Erste Demo-Runde spielen und protokollieren (Ablauf: `DemoRunbook.md` aus dem
   Demo-Prep-Strang) – bis heute hat kein Mensch dieses Spiel gespielt.
2. Die G3-Lücke schließen: Der KI-Slot ist untätig, damit fehlt jeder MS-1-Punkt,
   der einen handelnden Gegner braucht.
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
| 2.5.0 | 2026-07-25 | D-066: G0-A1/G0-A2 und vollständige Receipt-Kette als Meilenstein-Voraussetzung festgelegt | Producer / Lead QA Engineer |
| 3.0.0 | 2026-08-06 | D-076: Meilenstein-Exits auf Tier 1 umgestellt – grüne CI plus gespielte und protokollierte Runde statt autorisierter Receipt-Kette; G0–G5 bleiben inhaltliche Gliederung ohne Blockadewirkung | Producer / Game Director |
