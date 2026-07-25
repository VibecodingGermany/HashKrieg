# Project Nova

**Dokumentversion:** 0.12.0 | **Status:** unveröffentlichter Recovery-Stand | **Verantwortungsbereich:** Executive Producer / Technical Writer | **Sprint:** 7

> Modernes Echtzeitstrategiespiel mit Basisbau und der lebendigen
> Kristallressource **Aetherium**. *Project Nova* ist der Arbeitstitel.

## Zweck

Diese Seite ist der Einstieg in Repository, Projektstatus und Dokumentation.
Sie unterscheidet bewusst zwischen vorhandenem Prototypcode und bestandenen
Gates.

## Abhängigkeiten

- [AGENTS.md](AGENTS.md) – verbindliche Arbeitsregeln
- [CONTRIBUTING.md](CONTRIBUTING.md) – Branch-, PR- und Review-Ablauf
- [docs/README.md](docs/README.md) – vollständiger Wiki-Index
- [docs/production/MVPRecoveryPlan.md](docs/production/MVPRecoveryPlan.md) –
  Gates G0 bis G5
- [docs/production/MVPContentManifest.md](docs/production/MVPContentManifest.md) –
  exakter MS-1-Inhalt
- [docs/production/DecisionLog.md](docs/production/DecisionLog.md) – D-064
  und D-066 zum fail-closed Trusted-Gate-Bootstrap

## Projektstatus

**Phase:** Implementierungs-Recovery · **Aktiv:** Sprint 7, G0-A offen ·
**Wiki:** 0.12.0 unveröffentlicht

| Ergebnisstufe | Status |
|---|---|
| Sprint 6 | durch D-055 beendet; Recovery-Rebaseline ersetzt die alte Planung |
| Sprint 7 | gestartet; zuerst G0-A Trusted-Gate-Bootstrap, danach G0-B Plattformbasis |
| G0 | offen |
| MS-0 | nicht erreicht |
| MS-1 / MVP | nicht erreicht |
| Alpha | nicht begonnen |

Das Repository enthält einen unvollständig integrierten Prototyp. Dateien,
Typen und isolierte Tests sind kein Fertignachweis. Führend sind der
[Implementierungs-Audit](docs/production/ImplementationAudit_2026-07-24.md)
und schema- sowie semantikvalide Gate-Evidence; in diesem Rebaseline werden keine
Evidence-Platzhalter erzeugt.

Schema 1.2 prüft aktuell ausschließlich Integrität und autorisiert keinen
Gate-Pass. Jeder Pass-Versuch endet fail-closed mit
`E_AUTHORIZATION_BOOTSTRAP`. G0-A1 liefert Schema 1.3, Trusted-Checkout-
Topologie und Gate-Runner nur als Integritätsgrundlage. G0-A2 muss danach den
zweiphasigen D-066-Receipt-Vertrag mit getrenntem Subject, Evidence-Carrier
und Trusted Tooling implementieren. Erst ein nachfolgender sauberer
Subject-Commit darf damit G0 nachweisen.

## Closed-Core MS-1

D-056 begrenzt MS-1 auf:

- Allianz gegen Legion, Mensch gegen KI;
- Glutrinne, Wüste, S, 128×128, klares Wetter;
- je neun Gebäude- und acht Einheitenrollen;
- vollständiges Aetherium einschließlich endlicher Reserve, Nachwachsen,
  Ausbreitung, permanenter Überernte und KI-Feldmanagement;
- Pause, Save/Load/Recovery und das definierte Accessibility-Minimum.

Evolvierte, Luft, T3, Zusatzkarten, Multiplayer, Kampagne, Telemetrie,
Steam/Cloud und finale Art/Audio sind Post-MVP.

## Tech-Stack

- **Engine:** Unity `6000.5.4f1`, Revision `d550df8bd089`
- **Rendering:** URP
- **Sprache:** C#
- **Simulation:** Unity-freier, autoritativer `Nova.Simulation`-Kern,
  Q16.16-Fixed-Point ab G1
- **Host:** Unity und `Nova.SimRunner` verwenden dieselben Core-/Sim-Quellen

Automatische Editor-Upgrades sind verboten. Eine Re-Evaluierung benötigt nach
G5 oder bei einem belegten Engine-Blocker eine neue D-ID.

## Repository-Struktur

```text
Project Nova/
├── Assets/                Unity-Projekt und Prototypcode
├── docs/                  Living-Documents-Wiki
│   ├── gamedesign/        Vollspiel-GDD mit MS-1-Overrides
│   ├── tech/              technische Verträge
│   └── production/        Entscheidungen, Gates, Risiken, Planung
├── quality/
│   ├── content/           maschinenlesbares MVP-Manifest
│   ├── scenarios/         kanonische Abnahmeszenarien
│   ├── schemas/           Evidence-Schema; keine Platzhalter-Evidence
│   ├── scripts/           Schema-, Semantik- und Integritätsprüfung
│   └── package-lock.json  gepinnte Ajv-Abhängigkeiten
├── tools/                 unter anderem Nova.SimRunner
├── AGENTS.md
├── CONTRIBUTING.md
└── CHANGELOG.md
```

## Arbeitsweise

`main` ist PR-only. Arbeit erfolgt auf kurzen
`feat/`, `fix/`, `docs/`, `chore/`, `refactor/` oder `codex/`-Branches,
gefolgt von Squash-Merge und linearer Historie. Es gibt keinen dauerhaften
Integrationsbranch. Agenten committen oder pushen nur nach ausdrücklicher
Anfrage für die jeweilige Aktion.

Pflichtchecks sind `docs-check` und für Quality-Verträge `integrity`. Dieser
Teil des `quality-gate` prüft nur Verträge und Negative Controls. Ein
Authorize-Job existiert bis G0-A2 bewusst nicht. Eine Änderung am Trust-
Bundle wird ohne Gate-Fortschritt gemergt und kann sich nicht selbst
autorisieren.

## Lizenz

© 2026 VibecodingGermany / Dennis Westermann. Bis eine formale Lizenz
vorliegt, sind Ansehen und Mitwirken per Pull Request erwünscht; eine
Weiterverbreitung als eigenes Werk ist nicht freigegeben.

## Offene Punkte

- Q-018 (Preis) und Q-019 (Telemetrie) bleiben offen und blockieren MS-1 nicht.
- Eine formale Lizenz ist noch festzulegen.

## Nächste Schritte

1. G0-A1 ohne Gate-Fortschritt geschützt mergen.
2. G0-A2 als separaten zweiphasigen Receipt-Authorizer implementieren und
   adversarial prüfen.
3. Am nachfolgenden sauberen Subject G0-B herstellen und dort mit der
   vollständigen Receipt-Kette und Umgebungsbindung G0 beweisen.
4. G1 einschließlich V1–V5a erst nach bestandenem G0 beginnen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.7.1 | 2026-07-24 | Recovery-Baseline nach Implementierungs-Audit | Executive Producer / Lead Technical Director |
| 0.8.0 | 2026-07-24 | Closed-Core MS-1, exakten Engine-Pin, G0-offenen Status und Quality-Verträge D-056–D-061 aufgenommen | Executive Producer / Technical Writer |
| 0.8.1 | 2026-07-24 | Evidence-Semantikvalidator ergänzt und Dokumentstruktur korrigiert | Technical Writer / Lead QA Engineer |
| 0.8.2 | 2026-07-24 | Sprint-6-Endstatus und auf G0 begrenzten Start von Sprint 7 eindeutig formuliert | Executive Producer / Technical Writer |
| 0.9.0 | 2026-07-24 | D-062-Evidence-Kette sowie Victory-, MatchConfig- und Commander-MS-1-Overrides ergänzt | Executive Producer / Technical Writer / Lead QA Engineer |
| 0.10.0 | 2026-07-24 | D-063-Schema 1.2, kanonische Check-Artefakte, Drei-Lauf-Messung und Protected-CI-Trustpfad aufgenommen | Executive Producer / Technical Writer / Lead QA Engineer |
| 0.11.0 | 2026-07-24 | D-064: Schema 1.2 auf Integrität begrenzt, G0-A vor G0-B gestellt und subject-unabhängigen Schema-1.3-Bootstrap verankert | Executive Producer / Technical Writer / Lead QA Engineer |
| 0.12.0 | 2026-07-25 | D-066: G0-A1-Integritätsgrundlage vom zweiphasigen G0-A2-Receipt-Authorizer getrennt und zirkulären Pass-Pfad entfernt | Executive Producer / Technical Writer / Lead QA Engineer |
