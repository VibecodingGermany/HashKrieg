# Produktions-Roadmap (Roadmap)

**Version:** 1.1.0 | **Status:** Rebaseline erforderlich – nicht terminverbindlich | **Verantwortungsbereich:** Producer / Executive Producer | **Sprint:** 7

## Zweck

Historische Zeit-, Aufwands- und Kapazitätsannahmen für *Project Nova*. Nach D-055 ist dieses Dokument **nicht termin- oder scope-verbindlich**: Die 445-PT-Schätzung besitzt keine belastbare Messbasis, R-16 ist nicht mitigiert und Q-018/Q-019 bleiben offen. Eine neue Roadmap darf erst aus gemessenen Recovery-Gates und einem durch Q-038 ratifizierten MVP entstehen.

## Abhängigkeiten

- [Milestones.md](Milestones.md) – Meilenstein-Definitionen MS-0 bis MS-4
- [SprintPlanning.md](SprintPlanning.md) – Sprint-Definitionen und Phasenmodell
- [DecisionLog.md](DecisionLog.md) – D-007 (Geschäftsmodell), D-033 (SimRunner), D-054 (0 € Asset-Pipeline)
- [../assets/BuildBacklog.md](../assets/BuildBacklog.md) – Eigenbau-Assets B-01 bis B-14 (~110–180 PT)
- [RiskAnalysis.md](RiskAnalysis.md) – R-13 (Bus-Faktor) und R-16 (Kapazitätsmodell)

---

## Korrekturvermerk

Der folgende Sprint-6-Plan bleibt aus Gründen der Nachvollziehbarkeit erhalten, ist aber keine Freigabe. Verbindlich sind der [Implementierungs-Audit](ImplementationAudit_2026-07-24.md), D-055 und der [MVP-Recovery-Plan](MVPRecoveryPlan.md). MS-0 ist offen, das MVP nicht erreicht und Alpha nicht begonnen.

## 1. Gesamtaufwandsschätzung (Personentage / PT)

Der Gesamtaufwand für *Project Nova* verteilt sich auf **Engine-/Simulationscode**, **Art-Binaries (0 € Open-Source & KI-Pipeline)** und **Game Design / Level Design / Audio**:

| Bereich | Phase 0 (MS-0 Spike) | Phase 1 (MS-1 MVP) | Phase 2 (MS-2 Alpha) | Phase 3 (MS-3 Beta) | Phase 4 (MS-4 Release) | **Gesamtaufwand** |
|---|---|---|---|---|---|---|
| **Simulations-Kern & Code** | 15 PT | 25 PT | 20 PT | 15 PT | 10 PT | **85 PT** |
| **Pathfinding & AI** | 10 PT | 12 PT | 15 PT | 10 PT | 5 PT | **52 PT** |
| **UI, Systems & Audio-Code** | 6 PT (B-10) | 12 PT | 15 PT | 10 PT | 5 PT | **48 PT** |
| **Assets (CC0 + KI + BuildBacklog)** | 10 PT (B-01/02) | 25 PT (B-14) | 75 PT (B-04–09) | 20 PT (B-13) | 10 PT (B-12) | **140 PT** |
| **Karten, Biome & Level Design** | 2 PT | 8 PT | 15 PT | 25 PT | 5 PT | **55 PT** |
| **QA, Testing, CI & Polish** | 5 PT | 10 PT | 15 PT | 20 PT | 15 PT | **65 PT** |
| **Summe Personentage (PT)** | **48 PT** | **92 PT** | **155 PT** | **100 PT** | **50 PT** | **445 PT** |

---

## 2. Phasenplan & Kapazitätsmodell (R-16 & R-13 Mitigation)

### 2.1 Kapazitätsannahme
Das Projekt basiert auf einem **hybriden Open-Source-Entwicklungsmodell** (Projektinhaber + KI-Coding-Agenten + motivierte Community-Volunteers):
* **Kern-Kapazität:** ~1,5–2,0 effektive Vollzeit-Äquivalente (Projektinhaber + KI-Agenten-Durchsatz).
* **Durchsatz:** ~20–25 Personentage (PT) pro Kalendermonat.
* **Gesamtdauer (445 PT):** ca. 18–22 Kalendermonate bis Release v1.0.

### 2.2 Zeitlicher Ablaufplan (Roadmap-Phasen)

```
2026 Q3              2026 Q4              2027 Q1-Q2           2027 Q3-Q4           2028 Q1
┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐
│     MS-0         │ │     MS-1         │ │     MS-2         │ │     MS-3         │ │     MS-4         │
│ Phase 0: Spike   │ │ Phase 1: MVP     │ │ Phase 2: Alpha   │ │ Phase 3: Beta    │ │ Phase 4: Release │
│  (48 PT / 2 Mon) │ │  (92 PT / 4 Mon) │ │ (155 PT / 7 Mon) │ │ (100 PT / 5 Mon) │ │  (50 PT / 2 Mon) │
└──────────────────┘ └──────────────────┘ └──────────────────┘ └──────────────────┘ └──────────────────┘
```

1. **Phase 0 (Spike & VS / MS-0):** Monatsziel Q3 2026 (2 Monate). Fokus auf Festkomma-Determinismus (ARM↔x86), Pathfinding-Spike, B-01/B-02 Aetherium-Referenz und B-10 RTS-UI.
2. **Phase 1 (MVP / MS-1):** Q4 2026 (4 Monate). Fokus auf spielbare Allianz vs. Legion 1v1-Skirmish-Partie gegen KI.
3. **Phase 2 (Alpha / MS-2):** Q1–Q2 2027 (7 Monate). Fokus auf Evolvierte-Fraktion (B-04 bis B-09, 75 PT Art-Aufwand) und 2–4 Spieler Relay-Multiplayer.
4. **Phase 3 (Beta / MS-3):** Q3–Q4 2027 (5 Monate). Content-Skalierung auf 12 Maps, 10 Biome, Superwaffen, Opt-in Telemetrie und Performance-Polish.
5. **Phase 4 (Release v1.0 / MS-4):** Q1 2028 (2 Monate). Singleplayer-Kampagnen-Pass, Steam-Launch.

---

## 3. Kommerzielles Modell & Telemetrie (Q-018, Q-019)

### 3.1 Preispunkt & Geschäftsmodell (Q-018 offen; historische Annahme)
* **Entscheidung (D-007 Ratifizierung):** **Premium Singleplayer/Skirmish-first** auf Steam (Windows / macOS).
* **Preispunkt:** **29,99 € (Standard)** / **39,99 € (Supporter Edition)**.
* **Grundsätze (Vorschlag):** Kein Pay-to-Win, keine In-Game-Käufe, keine Online-Pflicht. Laufende Serverkosten sind nicht mit 0 € belegt.

### 3.2 Telemetrie-Modell (Q-019 offen; historische Annahme)
* **Entscheidung:** **Opt-in Anonymized Telemetry.**
* **Scope:** Anonymisierte Crash-Reports (Sentry, D-042) und anonymisierte Match-Statistiken (Win-Rates, Fraktions-Wahl, Matchdauer) für Balancing-Passes.
* **Datenschutz:** DSGVO-konform, kein Tracking persönlicher Daten, standardmäßig deaktiviert (Opt-in).

---

## 4. Offene Punkte

- Q-018 und Q-019 benötigen gültige Entscheidungen mit Alternativen.
- Q-038 bestimmt den MVP-Zuschnitt; R-16 benötigt Messdaten statt pauschaler PT.

## Nächste Schritte

1. Recovery-Gates G0–G5 ausführen.
2. Nach G2 den gemessenen Durchsatz und nach G4 den bestätigten Scope neu schätzen.

---

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-24 | Erstfassung Sprint 6: Gesamtaufwand (445 PT), Phasenplan 2026–2028, Q-018 (Preispunkt 29,99–39,99 €) und Q-019 (Opt-in Telemetrie) geschlossen | Producer / Executive Producer |
| 1.1.0 | 2026-07-24 | Sprint-6-Schätzung durch D-055 als unbelegte historische Annahme eingestuft; Roadmap bis zur Recovery-Rebaseline entfristet | Producer / Executive Producer |
