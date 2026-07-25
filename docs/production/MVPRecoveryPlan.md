# MVP-Recovery-Plan

**Version:** 0.1.0 | **Status:** verbindlicher Recovery-Arbeitsplan | **Verantwortungsbereich:** Producer / Lead Technical Director / Lead QA Engineer | **Sprint:** 7

## Zweck

Führt den auf Commit `460290e` festgestellten Prototyp-Bestand kontrolliert zu
einem tatsächlich spielbaren MVP. Fortschritt wird ausschließlich durch
reproduzierbare Ergebnis-Evidenz anerkannt. Grundlage sind das
[Implementierungs-Audit](ImplementationAudit_2026-07-24.md) und D-055.

## Abhängigkeiten

- [ImplementationAudit_2026-07-24.md](ImplementationAudit_2026-07-24.md)
- [DecisionLog.md](DecisionLog.md) – D-033 bis D-055
- [OpenQuestions.md](OpenQuestions.md) – insbesondere Q-031 bis Q-034, Q-038 und Q-039
- [../tech/Architecture.md](../tech/Architecture.md)
- [../tech/Testing.md](../tech/Testing.md)
- [Milestones.md](Milestones.md)

## 1. Verbindliche Arbeitsregeln

1. **Kein Status durch Dateianzahl:** Klassen, APIs, Specs und Unit-Tests sind
   Zwischenprodukte, keine Feature-Abnahme.
2. **Ein Gate nach dem anderen:** Folgearbeit beginnt erst, wenn alle
   Exit-Kriterien des aktuellen Gates belegt sind.
3. **Keine Alpha-Arbeit:** Evolvierte, Doktrinen, Online-MP und zusätzliche
   Karten bleiben pausiert, bis MVP-G5 bestanden ist.
4. **Ein integrierter Pfad:** UI, KI und später Netzwerk reichen Commands über
   dieselbe kanonische Pipeline ein.
5. **Evidenz im Repository:** Jedes Gate liefert Testreport, Build-/Run-Anleitung
   und verlinkte Artefakte oder Messprotokolle.
6. **Rot stoppt:** Kein Gate-Abschluss bei roten Pflicht-Checks.

## 2. Empfohlener MVP-Scope

Der genaue Content-Scope ist eine Project-Owner-Entscheidung (Q-038). Bis zur
Ratifizierung wird keine breite Content-Produktion begonnen.

| Alternative | Inhalt | Bewertung |
|---|---|---|
| A – Voller bisheriger Scope | 12 Gebäude und 27 Einheiten pro Fraktion | kein realistischer MVP; hohe Integrations- und Balancekosten |
| **B – Repräsentativer Vertical-Slice-MVP** | **2 Fraktionen, je 6 Gebäude-Rollen und 6 Einheiten-Rollen, 1 Karte/1 Biom, Human vs. KI** | **Empfehlung: beweist den gesamten RTS-Kern ohne Content-Explosion** |
| C – Mirror-Tech-Demo | 1 Fraktion gespiegelt, je 4 Gebäude-/Einheiten-Rollen | schnellster Techniknachweis, aber unzureichender Fraktions-/Kontertest |

Empfohlene Rollen für Alternative B:

- **Gebäude:** HQ, Kraftwerk, Raffinerie, Kaserne, Fahrzeugfabrik,
  Verteidigungsplattform
- **Einheiten:** Worker, Harvester, Basisinfanterie, Anti-Armor-Infanterie,
  leichter Kampfpanzer, mobile Flug-/Fernabwehr ohne MVP-Luftcontent
- **Systeme:** Aetherium-Ernte, Energie, Bau, Produktion, Bewegung, Kampf, FoW,
  einfache KI, Vernichtungs-Siegbedingung

Explizite MVP-Nichtziele: Evolvierte, Commander-Mechaniken/Doktrinen, Online-MP,
Air-Units, Tier 3, Elites, Superwaffen, Kampagne, Telemetrie, mehrere Karten,
finale Art/VO und Ranked.

## 3. Recovery-Gates

### G0 – Reproduzierbare grüne Baseline

**Ziel:** Jeder Clean Clone kann den relevanten Code bauen und testen.

**Arbeit:**

- SimRunner-Projektdatei versionieren; generierte `bin/`-/`obj/`-Artefakte aus
  der Versionsverwaltung entfernen.
- Code-CI für Compile, EditMode-Tests und Plain-.NET-Simtests einrichten.
- Paketgrößenkonflikt beheben und alle Tests grün machen.
- Unity-Version und lokale/CI-Aufrufe pinnen.

**Exit-Evidenz:**

- Clean-Clone-Build auf macOS und Windows
- grüne Unity- und .NET-Testreports
- CI blockiert einen absichtlich roten Kontrolltest

### G1 – Kanonischer deterministischer Kern

**Ziel:** Commands, State, Hash und Replay bilden einen einzigen überprüfbaren
Simulationspfad.

**Arbeit:**

- 10-Hz-Konstante zentralisieren und alle Zeitwerte angleichen.
- einen Command-Buffer mit Target-Tick, Sequenz, Validierung, Deduplizierung und
  deterministischer Sortierung implementieren.
- vollständigen serialisierbaren `WorldState` definieren.
- nicht-mutierenden xxHash64 über alle autoritativen Zustände implementieren.
- Replay-Playback mit Golden-Master-Checkpoints bauen.
- Q-031/Q-032 und Q-039 vor Gate-Abschluss entscheiden.

**Exit-Evidenz:**

- End-to-End-Test reicht ausschließlich über `SimulationKernel.SubmitCommand`
  ein und belegt Mutation exakt im Ziel-Tick
- Snapshot-Roundtrip ist bit-identisch
- wiederholtes Hashing verändert den Zustand nicht
- identischer Seed + Command-Log erzeugt reproduzierbare Checkpoint-Hashes
- Cross-Plattform-Evidenz gemäß Q-039

### G2 – Integrierter Graybox-Kernloop

**Ziel:** Ein Mensch kann ein minimales Match starten, steuern und beenden.

**Arbeit:**

- eine versionierte Build-Scene und Match-Komposition erstellen.
- Economy, Construction, Production, Movement, Combat, FoW und Victory über
  Commands integrieren.
- eine Graybox-Karte, Platzhalter-Prefabs und minimales HUD anbinden.

**Exit-Evidenz:**

- Player-Build startet ohne Editor
- manueller und automatisierter Pfad: Start → ernten → bauen → produzieren →
  angreifen → Sieg/Niederlage
- keine direkte State-Mutation aus UI oder Presentation
- reproduzierbares 10-Minuten-Smoke-Match ohne Exception

### G3 – Skirmish-KI und vollständiger Matchablauf

**Ziel:** Die KI kann denselben Kernloop regelkonform spielen.

**Arbeit:**

- KI liest ausschließlich gefilterte World-View-Snapshots.
- KI erzeugt Commands für Economy, Bau, Produktion, Verteidigung und Angriff.
- V5-Kostenmodell mit Spatial Hash, FoW-Filter und KI-Command-Verarbeitung
  implementieren.

**Exit-Evidenz:**

- KI kann Ressourcen gewinnen, Basis aufbauen, Armee produzieren und gewinnen
- mindestens 10 feste Seeds terminieren regelkonform
- kein negatives Konto, keine ungültigen Commands, keine Endlosschleife
- V5-P95-Messbericht auf D-052-Referenzhardware

### G4 – Ratifizierter MVP-Content und Präsentation

**Ziel:** Der in Q-038 gewählte Scope ist vollständig datengetrieben integriert.

**Arbeit:**

- ausgewählte Gebäude-/Einheitenrollen für Allianz und Legion umsetzen.
- produktive Definitionen, Prefabs und Asset-Mappings statt Dummy-Objekten
  liefern.
- lesbares HUD, Auswahl, Command Card, Minimap und Ergebnisanzeige integrieren.
- Aetherium-Referenzdarstellung und Teamfarben-Pass abnehmen.

**Exit-Evidenz:**

- Registry-Validierung belegt jeden erwarteten Content-Eintrag
- keine Platzhalter- oder Null-Registry im Player-Build
- visueller Review auf einer vollständigen Matchaufnahme
- Lizenz-/Provenienzbelege für alle enthaltenen Assets

### G5 – Tatsächliche MVP-Abnahme

**Ziel:** MS-1 wird erstmals evidenzbasiert erreicht.

**Exit-Evidenz:**

- vollständiges 20–35-Minuten-Human-vs.-KI-Skirmish von Start bis Ergebnis
- mindestens 20 automatisierte Matchläufe ohne Crash, Deadlock oder
  Regelverletzung
- Golden-Master-, Integration-, EditMode- und Player-Build-Gates grün
- P95 Sim-Tick ≤8 ms bei 500 Einheiten und P95 Frame-Time ≤16,6 ms auf
  D-052-Referenzhardware im definierten Messszenario
- unabhängiges Review bestätigt Architektur-, Test- und Scope-Konformität
- Project Owner erteilt erst danach das GO für Alpha-Planung

## 4. Reihenfolge und Abhängigkeiten

```text
G0 Grüne Baseline
  → G1 Command/State/Hash/Replay
    → G2 spielbarer Graybox-Kernloop
      → G3 vollständige Skirmish-KI + V5
        → G4 ratifizierter Content
          → G5 MVP-Abnahme
```

Q-038 blockiert G4. Q-039 blockiert G1. Q-031/Q-032 blockieren den Abschluss von
G1; Q-033 wird durch G3/V5 erfüllt.

## 5. Fortschrittsbericht

| Gate | Status | Evidenz |
|---|---|---|
| G0 | **offen** | Test-Suite rot; Code-CI und reproduzierbarer Clean-Clone-Build fehlen |
| G1 | blockiert | Command-Pfad, State-Hash und Replay nicht kanonisch |
| G2 | nicht begonnen | keine Build-Scene / keine vollständige Match-Komposition |
| G3 | nicht begonnen | KI- und V5-Nachweis fehlen |
| G4 | blockiert | Q-038 offen; produktiver Content fehlt |
| G5 | nicht begonnen | kein vollständiges Skirmish-/Performance-Artefakt |

## Offene Punkte

- Q-038: Alternative A/B/C durch den Project Owner ratifizieren.
- Q-039: Fixed-Point-/Cross-Plattform-Gate konsistent zu D-033 festlegen.
- Nach G1 Aufwand und Kalenderplan mit gemessener Velocity neu schätzen.

## Nächste Schritte

1. G0 als einzigen aktiven Implementierungs-Scope öffnen.
2. Keine neuen Gameplay-/Alpha-Module beginnen.
3. Nach grünem G0 einen separaten Review-Commit als Gate-Beleg erstellen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-24 | Evidenzbasierten Recovery-Pfad G0–G5 und empfohlenen reduzierten MVP-Scope definiert | Producer / Lead Technical Director / Lead QA Engineer |
