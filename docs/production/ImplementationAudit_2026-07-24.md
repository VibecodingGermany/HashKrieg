# Implementierungs-Audit vom 24. Juli 2026

**Version:** 1.0.0 | **Status:** verbindlicher Review-Befund | **Verantwortungsbereich:** Lead Technical Director / Lead QA Engineer | **Sprint:** 7

## Zweck

Dokumentiert den nachweisbaren Implementierungsstand auf Commit
`460290eaf586956aa6284c414da80356ecdd2c97`. Das Audit trennt vorhandenes
Scaffolding von integrierten, abgenommenen Features und ist die Beweisgrundlage
für die Statuskorrektur D-055 sowie den [MVP-Recovery-Plan](MVPRecoveryPlan.md).

## Abhängigkeiten

- [DecisionLog.md](DecisionLog.md) – D-033 bis D-052 sowie Recovery-Entscheidung D-055
- [Milestones.md](Milestones.md) – bisherige MS-0-/MVP-/Alpha-Gates
- [../tech/Architecture.md](../tech/Architecture.md) – verbindliche Simulationsarchitektur
- [../tech/Testing.md](../tech/Testing.md) – verbindliche Test- und Evidenzstrategie
- [MVPRecoveryPlan.md](MVPRecoveryPlan.md) – Folgeplan

## 1. Review-Basis und Methode

- **Vergleichsbasis:** Sprint-6-Abschluss `e0e84cf`
- **Geprüfter Implementierungsstand:** `460290e`
- **Umfang:** 19 Implementierungs-Commits, 293 Dateien, rund 9.600 neue Zeilen
- **Prüfung:** Code-/Architekturreview, Abgleich gegen DecisionLog/GDD/TDD,
  Bestandsprüfung von Scenes/Assets/CI sowie Unity-EditMode-Testlauf
- **Beobachteter lokaler Testlauf:** 36 Tests, 35 bestanden, 1
  fehlgeschlagen, Exit-Code 2. Der rohe Testreport wurde nicht als Artefakt
  gesichert; reproduzierbar statisch belegt sind 36 Testmethoden und der
  41-/34-/37-Byte-Widerspruch.
- **Nicht einbezogen:** parallel vorhandene, uncommittete Shader-, Test-Assembly-
  und SimRunner-Binäränderungen

Ein Modul gilt in diesem Audit nur dann als fertig, wenn sein Nutzerergebnis
integriert funktioniert und das zugehörige Gate durch reproduzierbare Evidenz
belegt ist. Dateien, Klassen, APIs und isolierte Unit-Tests allein sind kein
Fertigstellungsnachweis.

## 2. Kritische Befunde

### F-001 | P0 | Kanonischer Command-Pfad verwirft Commands

`SimulationKernel.SubmitCommand()` nimmt Commands an. Fällige Commands werden in
einen internen Buffer verschoben, aber nie an den `CommandProcessorSystem`
übergeben. Der vorhandene Test umgeht den Kernel und schreibt direkt in einen
zweiten Buffer. UI-, KI- oder Netzwerkbefehle können deshalb erfolgreich
angenommen werden, ohne den Spielzustand zu verändern.

**Konsequenz:** D-033 „Commands als einzige State-Mutation“ ist nicht
implementiert. Kein Gameplay- oder Lockstep-Gate kann auf diesem Pfad bestehen.

### F-002 | P0 | Test-Suite ist rot

Der Netzwerkpaket-Test erwartet 41 Bytes; die Implementierung serialisiert 34
Bytes. Die Modulspezifikation nennt zusätzlich 37 Bytes. Der geprüfte Stand hat
damit bereits auf Unit-Test-Ebene drei widersprüchliche Paketgrößen.

**Konsequenz:** Der Stand ist nicht mergefähig und das Relay-Modul nicht
abgenommen.

### F-003 | P0 | Kein spielbarer MVP vorhanden

Es gibt keine Build-Scene und keine vollständige Match-Komposition.
`MatchRunner` registriert nur Pathfinding und Movement. Economy, Construction,
Production, Combat, Commands, AI, Vision, Victory und Networking sind nicht in
einen spielbaren Ablauf integriert.

**Konsequenz:** Weder ein Matchstart noch ein vollständiges Skirmish von Aufbau
bis Sieg/Niederlage ist nachgewiesen. MS-1 ist nicht erreicht.

### F-004 | P1 | MS-0-Gates sind unbelegt

`SimMath` verwendet IEEE-754-Floats. Der Determinismustest vergleicht zwei leere
Kernel im selben Prozess; es gibt keinen Windows-x86↔macOS-ARM-Nachweis. Es
existiert kein abgenommenes Aetherium-Referenzasset und kein belastbarer
Standalone-Performancebericht auf D-052-Hardware.

**Konsequenz:** MS-0 ist trotz vorhandener Kernprototypen nicht abgeschlossen.

### F-005 | P1 | State-Hash und Replay sind nicht kanonisch

`SimulationKernel.CalculateStateHash()` verändert durch `Random.NextUInt()` den
PRNG-Zustand und hasht keinen Weltzustand. `StateHashUtility` deckt nur einen
Teil der Unit-Daten ab und ignoriert unter anderem Commands, Economy,
Construction, Production, FoW, Commander, Biomasse und PRNG-State. Verwendet
wird FNV-1a statt des durch D-049 geforderten xxHash64. `ReplayBuffer` zeichnet
nur Einträge auf und besitzt keinen Playback-/Checkpoint-Nachweis.

**Konsequenz:** Desync-Erkennung, Golden Master und Post-Match-Re-Simulation
sind nicht funktionsfähig.

### F-006 | P1 | Verbindliche Tickrate verletzt

Architektur und D-033 schreiben 10 Hz vor. `MatchRunner`, Movement und mehrere
Cooldown-/Regenerationskonstanten rechnen mit 20 Hz.

**Konsequenz:** Zeitbasierte Balancewerte und Netzwerkannahmen sind um Faktor
zwei verschoben.

### F-007 | P1 | MVP-/Alpha-Module sind Scaffolding

- Die KI kann ein hartcodiertes Kraftwerk und einen Rifleman anfordern, aber
  nicht expandieren, verteidigen, angreifen oder gewinnen.
- Das „UDP Relay“ besteht aus Paketserialisierung und einem In-Memory-Dictionary;
  Transport, Server, Client, Lobby und Session fehlen.
- Die angeblich drei verifizierten Karten bestehen aus einer generischen
  `MapDefinitionSO`-Klasse.
- Die Asset-Integration testet zwei zur Laufzeit erzeugte Dummy-GameObjects;
  produktive Unit-/Building-Prefabs fehlen.
- Die Evolvierten-Implementierung umfasst im Wesentlichen Regeneration auf
  Biomasse; Fraktionscontent und organischer Bau fehlen.
- Das Commander-System implementiert aktive Matchfähigkeiten entgegen D-009.

**Konsequenz:** MS-1 ist nicht spielbar; MS-2 wurde fachlich nicht begonnen.

### F-008 | P1 | Test- und Build-Infrastruktur fehlt

GitHub Actions prüft ausschließlich Markdown. C#, Unity, Coverage, Replay,
Integration, Performance und Player-Builds besitzen kein Merge-Gate. Das
SimRunner-`.csproj` wird ignoriert, während erzeugte Binärdateien versioniert
sind; ein Clean Clone kann den Runner nicht reproduzieren.

**Konsequenz:** Weitere Fortschrittsbehauptungen wären ohne vorheriges
CI-/Build-Gate nicht belastbar.

## 3. Planungs- und Governance-Befunde

1. Q-031, Q-032 und Q-033 waren als Vorstart-/Pflichtblocker offen, als Sprint 7
   freigegeben und abhängige Module implementiert wurden.
2. Alpha-Relay widerspricht D-025/D-033 (Online-Modi frühestens Beta).
3. Drei Alpha-Karten widersprechen D-017 (Kartenstaffel 1/4/8/12).
4. Alpha-Doktrinen widersprechen D-009 (erst ab Beta evaluierbar und nur nach
   eigener Entscheidung).
5. Q-018 und Q-019 wurden ohne DecisionLog-Eintrag geschlossen und stehen im
   kanonischen Register weiterhin als offen.
6. Der 445-PT-Punktwert wurde trotz unkalibrierter 110–180-PT-Assetspanne als
   Risikominderung behandelt. Das Zeit-/Kapazitätsrisiko bleibt aktiv.
7. „0 € laufende Serverkosten“ ist mit Relay, Command-/Hash-Speicherung,
   Post-Match-Re-Simulation und Crash-/Telemetrie-Infrastruktur unbelegt.

## 4. Tatsächlicher Iststand

| Bereich | Nachweisbarer Stand | Status |
|---|---|---|
| Unity-Projekt | Projektstruktur und Assemblies angelegt | Prototyp |
| Simulationskern | Basistypen und Tick-Gerüst vorhanden; Command-Pfad defekt | nicht abgenommen |
| Pathfinding/Movement | frühe Managed-Prototypen und Microbenchmark | nicht MS-0-abgenommen |
| Gameplay-Systeme | isolierte Teilimplementierungen ohne Match-Integration | Scaffolding |
| KI | minimale Produktionsdemo | Scaffolding |
| UI/Assets/Karten | Hilfstypen und Dummy-Tests, kein produktiver Content | Scaffolding |
| Multiplayer | Serialisierungs-/Buffer-Prototyp ohne Transport | Scaffolding |
| Tests | kleine Unit-Suite, mindestens ein roter Test | rot |
| CI/Build | keine Code-CI, kein reproduzierbarer SimRunner aus Clean Clone | fehlt |
| Meilensteine | MS-0 offen; MVP nicht erreicht; Alpha nicht begonnen | Recovery erforderlich |

## 5. Offene Punkte

- Q-038: verbindlichen, reduzierten MVP-Content-Scope durch den Project Owner
  ratifizieren.
- Q-039: Widerspruch „Fixed-Point-Gate in MS-0“ versus „Float im MVP erlaubt“
  durch eine eigene Architekturentscheidung auflösen.
- Der uncommittete parallele Shader-Stand wurde nicht bewertet.

## 6. Nächste Schritte

1. D-055 anwenden und alle Fertigmeldungen auf Prototyp-/Scaffolding-Status
   zurücksetzen.
2. [MVPRecoveryPlan.md](MVPRecoveryPlan.md) Gate für Gate ausführen.
3. Keine weitere Alpha-/Content-Expansion beginnen, bevor das MVP-Gate
   vollständig belegt ist.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-24 | Kritisches Implementierungs-Audit auf Commit `460290e`; tatsächlichen Iststand und Recovery-Bedarf festgestellt | Lead Technical Director / Lead QA Engineer |
