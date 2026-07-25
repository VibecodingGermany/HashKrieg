# Unity ECS/DOTS vs. klassisches OOP – Architekturvergleich für Project Nova

**Version:** 0.1.1 | **Status:** Entwurf | **Verantwortungsbereich:** Lead Technical Director | **Sprint:** 1

## Zweck

Dieses Dokument beantwortet die Open Question **Q-015** („Unity ECS/DOTS vs. klassisches OOP"). Es vergleicht drei Architekturansätze für den Simulation- und Gameplay-Kern von Project Nova:

- **(a)** Klassische MonoBehaviour-OOP-Architektur
- **(b)** Vollständiges DOTS/ECS (Unity Entities + Burst + Job System)
- **(c)** Hybrider Ansatz (DOTS-Technologien punktuell für Simulation/Pathfinding, OOP für UI/Audio/Präsentation)

Bewertet werden Reifegrad von Unity Entities (Stand 2025/2026), Lernkurve, Debugging/Tooling, Testbarkeit, Asset-Store-Kompatibilität, Determinismus (Wechselwirkung mit Q-013/Lockstep), Wartbarkeit durch KI-Coding-Agenten, Performance-Potenzial bei 100–500+ Einheiten und URP-Kompatibilität. Ziel ist eine belastbare Entscheidungsvorlage für den DecisionLog, die MVP-Disziplin (1 Fraktion, 1 Karte, Kernloop) und die langfristige Multiplayer-Autorität gleichermaßen berücksichtigt.

## Abhängigkeiten

- [KnowledgeBase](../analysis/KnowledgeBase.md) – Projektkontext, Stack-Festlegung (Unity + URP, C#, Windows/macOS), Leitplanken (datengetrieben via ScriptableObjects, MVP-Disziplin, spätere MP-Autorität)
- [OpenQuestions](../production/OpenQuestions.md) – Q-013 (Netzwerkmodell/Lockstep), Q-014, Q-015 (dieses Dokument)

## Ausgangslage und Projektanforderungen

Die architekturrelevanten Rahmenbedingungen aus dem Projekt:

- **100–500+ gleichzeitige Einheiten bei 60 FPS** auf Desktop (Windows/macOS, Entwicklung auf Apple Silicon)
- **Datengetriebene Architektur** (ScriptableObjects als Leitplanke) für Einheiten-, Gebäude- und Fraktionsdefinitionen
- **MVP zuerst**: 1 Fraktion, 1 Karte, 1 Ressource, einfache KI – Time-to-Validation ist das primäre Sprintziel, nicht Maximaleinheiten
- **Multiplayer langfristig** (autoritativer Server; RTS-üblich wäre deterministischer Lockstep, siehe Q-013) – Architektur darf diesen Pfad nicht verbauen
- **Asset-Store-Nutzung wahrscheinlich** (Terrain-/VFX-/UI-Assets), diese sind fast ausnahmslos MonoBehaviour-basiert
- Entwicklung wird maßgeblich von **KI-Coding-Agenten** unterstützt – Wartbarkeit und Trainingsdatenabdeckung des gewählten Stacks sind daher ein reales Kriterium

## Recherchestand: Unity Entities 2025/2026

Faktenlage aus offiziellen Quellen:

- **Entities 1.3** ist die stabile Linie für Unity 6 (LTS), inkl. Entities Graphics 1.3. Unity pflegt die 1.x-Pakete weiter mit Bugfixes. ([Entities-Changelog](https://docs.unity3d.com/Packages/com.unity.entities@1.4/changelog/CHANGELOG.html))
- **Entities 1.4** wurde im März 2025 als **experimental** veröffentlicht (API-Konsolidierung: u. a. Entfernung von `Entities.ForEach` und Aspects zugunsten von `IJobEntity`/Idiomatic ForEach) und ist zum Zeitpunkt der Recherche noch im Pre-Release-Status (Changelog 1.4.7, Juli 2026). ([ECS development status March 2025](https://discussions.unity.com/t/ecs-development-status-milestones-march-2025/1615810), [DOTS-Roadmap September 2024](https://discussions.unity.com/t/dots-development-status-and-milestones-ecs-for-all-september-2024/1519286))
- **Dezember 2025 (Unite Barcelona):** Unity kündigt an, dass Entities, Collections, Mathematics und Entities Graphics ab **Unity 6.4 Core Packages** werden (mit dem Editor ausgeliefert, Versionierung an Editor gekoppelt). Physics und Netcode folgen 2026. Gleichzeitig beginnt die Initiative **„ECS for All"**: `InstanceID` wird durch einen 64-bit-`EntityId`-Typ ersetzt, GameObjects sollen perspektivisch ECS-Komponentendaten tragen können – mit angekündigten, gestaffelten **Breaking Changes** über die Unity-6.x-Releases. ([ECS Development Status December 2025](https://discussions.unity.com/t/ecs-development-status-december-2025/1699284))
- **URP-Kompatibilität:** Entities Graphics unterstützt URP und HDRP, **nicht** die Built-in RP. In URP ist **nur der Forward+-Rendering-Pfad** unterstützt; Canvas/UI-Komponenten werden in Subscenes nicht unterstützt, Feature-Matrix ist noch nicht vollständig. ([Entities Graphics Requirements](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.0/manual/requirements-and-compatibility.html), [Feature Matrix](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.0/manual/entities-graphics-versions.html))
- **Determinismus:** Burst garantiert **keine plattformübergreifende Gleitkomma-Deterministik** (Windows x64 vs. macOS ARM64 können divergieren); ECS ist nur auf identischen CPU-Architekturen deterministisch. `Unity.Mathematics` enthält keinen Fixed-Point-Typ. Für echten Lockstep-Determinismus wäre zusätzlich Fixed-Point-Mathematik (Third-Party oder Eigenbau) nötig – unabhängig davon, ob OOP oder ECS. ([jdxdev zu DOTS-Determinismus](https://www.jdxdev.com/blog/tag/unity/), [Unity Discussions: Determinismus für RTS-Lockstep](https://discussions.unity.com/t/how-do-i-make-unity-deterministic-for-a-deterministic-lockstep-architecture-rts-development/254743))
- **Netcode for Entities** existiert als DOTS-MP-Lösung, ist aber auf Server-Autorität mit Client-Prediction ausgelegt, nicht auf klassischen RTS-Lockstep. (Einschätzung auf Basis der Paketdokumentation; im Detail in Q-013 zu prüfen.)

**Einordnung:** ECS ist 2025/2026 produktiv einsetzbar und strategisch klar von Unity gestützt – aber das Ökosystem befindet sich mitten in einem mehrjährigen Umbruch („ECS for All", Core-Package-Umstellung, API-Deprecations). Wer jetzt ein mehrjähriges Projekt beginnt, muss mit Migrationsaufwänden bei Editor-Upgrades rechnen.

## Vergleich der Ansätze

### (a) Klassische MonoBehaviour-OOP

**Beschreibung:** GameObjects + MonoBehaviours, Manager-/Service-Schicht (z. B. `UnitManager`, `SimulationLoop`), ScriptableObjects als Datencontainer, ggf. zentrale Update-Schleife statt tausender einzelner `Update()`-Calls.

**Projektbezogene Vorteile:**

- Schnellster Weg zum MVP-Kernloop; volle Editor-Integration (Inspector, Play Mode, Debugging mit Breakpoints in jedem Zustand)
- Volle Kompatibilität mit Asset-Store-Assets (UI, VFX, Terrain, A*-Pathfinding-Projekt etc.)
- ScriptableObjects-Datengetriebenheit nativ abgebildet – exakt die gesetzte Leitplanke
- Maximale Trainingsdatenabdeckung für KI-Coding-Agenten; idiomatischer C#-Code, den Agenten zuverlässig lesen, refaktorieren und testen können
- Unit-Tests (NUnit/Unity Test Framework) ohne Sonderwege

**Projektbezogene Nachteile:**

- Bei 500+ Einheiten mit individuellen GameObjects: GC-Druck, Cache-unfreundliche Speicherlayouts, Overhead durch GameObject/Transform-Verwaltung; 60 FPS erreichbar, aber nur mit Disziplin (Pooling, zentrale Manager, ggf. C# Job System + Burst für Hotspots)
- Lockstep-Determinismus (Q-013) nicht gegeben – erfordert wie überall Fixed-Point-Mathematik und strikte Simulations-/Präsentationstrennung, die OOP nicht erzwingt

### (b) Vollständiges DOTS/ECS

**Beschreibung:** Einheiten, Gebäude, Projektile und Ressourcenlogik als Entities mit `IComponentData` und `ISystem`, Rendering via Entities Graphics (URP Forward+), Subscenes für Authoring, Burst + Job System für alle Hotpaths.

**Projektbezogene Vorteile:**

- Höchstes Performance-Potenzial: datenorientierte Layouts (Archetype/Chunks) + Burst + Multithreading skalieren weit über 500 Einheiten hinaus – der 500+-Fall wäre hier mit großer Reserve abgedeckt
- Erzwingt saubere Trennung von Daten und Logik; Simulationszustand liegt flach und serialisierbar vor – hilfreich für spätere Server-Autorität, Replays und Savegames
- Unitys strategische Richtung („ECS for All"); frühe Investition amortisiert sich langfristig

**Projektbezogene Nachteile:**

- **Reifegrad-Risiko genau jetzt:** Entities 1.4 noch experimental, Core-Package-Umstellung (6.4+) und `EntityId`-Migration mit angekündigten Breaking Changes mitten in unserem Projektzeitraum ([Dezember-2025-Status](https://discussions.unity.com/t/ecs-development-status-december-2025/1699284))
- Steile Lernkurve (anderes Denkmodell: Archetypen, Queries, Structural Changes, `EntityCommandBuffer`); erhöht MVP-Risiko massiv
- Debugging-Realität: Breakpoints in Burst-kompiliertem Code eingeschränkt, Zustandsinspektion über dedizierte Fenster (Entities Hierarchy/Systems Window) statt gewohnter Inspector-Workflows; Burst muss zum Debuggen oft deaktiviert werden
- Asset-Store-Bruch: Gekaufte Assets sind MonoBehaviour-basiert und müssten umschrieben/adaptiert werden; UI (Canvas) wird in Subscenes nicht unterstützt – UI ohnehin hyrid zu lösen
- URP-Einschränkungen: nur Forward+, Entities-Graphics-Feature-Matrix unvollständig (Einschätzung: für unseren stylized Look vermutlich ausreichend, aber zu verifizieren)
- Testbarkeit schwieriger: `World`-Setup im Test, System-Scheduling, keine Standard-Mocks
- KI-Coding-Agenten: deutlich weniger Trainingsmaterial, API im Fluss (Deprecations) → höhere Fehlerrate und mehr Halluzinationsanfälligkeit bei ECS-Code
- Determinismus-Vorteil relativiert sich: Burst liefert **keine** Cross-Platform-Float-Deterministik – für Lockstep (Q-013) wäre Fixed-Point-Mathematik ohnehin Pflicht, der Hauptgrund „ECS wegen MP" entfällt damit weitgehend

### (c) Hybrid (empfohlener Ansatz)

**Beschreibung:** Klassische OOP-Architektur als tragendes Gerüst (Szene, UI, Audio, Input, Kamera, Asset-Store-Integration, ScriptableObject-Daten), aber mit zwei klar definierten DOTS-Einschlägen:

1. **Burst + C# Job System** für die rechenintensiven Hotspots (Bewegung/Flocking, Kampfauflösung, Sichtweiten, Aetherium-Wachstum) über `NativeArray`-basierte Datenpuffer – **ohne** vollständiges Entities-Framework
2. Architekturische **Sim/Core-Trennung** von Anfang an: Die Simulation läuft in einer engine-unabhängigen, tick-basierten Kernschicht (reine C#-Klassen, keine UnityEngine-Referenzen außer Math), Präsentation (GameObjects, Animation, VFX) liest nur aus Sim-Zustand. So bleibt die Tür für deterministischen Lockstep (Q-013) und für eine spätere partielle ECS-Migration der Simulation offen.

**Projektbezogene Vorteile:**

- MVP-Geschwindigkeit und Debuggbarkeit von (a), plus genug Performance-Reserve für 500+ Einheiten (500 Units sind für Burst-Jobs auf `NativeArray`-Daten ein moderater Fall; Risiko-Einschätzung: niedrig)
- Asset-Store-Assets bleiben uneingeschränkt nutzbar; URP ohne Forward+-Zwang und ohne Entities-Graphics-Lücken
- ScriptableObjects bleiben Datenquelle; Agenten-freundlicher, idiomatischer Code im Großteil der Codebasis
- Sim/Core-Trennung ist genau die Voraussetzung für autoritativen Server **und** für Lockstep – MP-Option bleibt offen, ohne jetzt Determinismus-Overhead zu bezahlen
- Kein Betting auf ein Paket im Umbruch: Entities kann nach Stabilisierung (Core Packages, 1.4 final) selektiv nachgezogen werden

**Projektbezogene Nachteile:**

- Erfordert von Anfang an architektonische Disziplin (keine UnityEngine-Abhängigkeiten im Sim-Kern, kein direkter Transform-Zugriff aus Gameplay-Logik) – muss reviewt und dokumentiert werden
- Performance-Deckel niedriger als Voll-DOTS: Für den unwahrscheinlichen Fall deutlich >500 Einheiten mit hoher Sim-Komplexität wäre eine spätere ECS-Migration des Sim-Kerns nötig (durch Sim/Core-Trennung vorbereitet, aber nicht trivial)
- Burst-Determinismus-Grenze gilt auch hier; Lockstep erfordert später Fixed-Point-Umstellung des Sim-Kerns (Aufwand unabhängig vom Ansatz)

### Vergleichstabelle

| Kriterium | (a) OOP pur | (b) Voll-DOTS | (c) Hybrid (Burst/Jobs + Sim-Core) |
|---|---|---|---|
| MVP-Geschwindigkeit | ++ | −− | + |
| Performance 500+ Einheiten | 0 (mit Disziplin ok) | ++ | + |
| Reifegrad/Stabilität (2025/26) | ++ | − (Umbruch: Core Packages, EntityId) | + |
| Lernkurve Team | ++ | −− | + |
| Debugging/Tooling | ++ | − | + |
| Testbarkeit | + | − | + |
| Asset-Store-Kompatibilität | ++ | −− | ++ |
| URP-Kompatibilität | ++ | 0 (nur Forward+, Matrix unvollständig) | ++ |
| Determinismus/Lockstep-Option (Q-013) | 0 (Sim-Trennung nötig) | 0 (Burst nicht cross-platform-deterministisch) | + (Sim-Core vorbereitet) |
| Wartbarkeit durch KI-Agenten | ++ | − (wenig Trainingsdaten, API im Fluss) | + |
| Zukunftssicherheit (Unity-Strategie) | 0 | + (langfristig), − (kurzfristig Migration) | + |

(Legende: ++ sehr gut, + gut, 0 neutral/aufwandspflichtig, − schlecht, −− sehr schlecht; Bewertungen sind projektbezogene Einschätzungen auf Basis der zitierten Fakten.)

### Illustratives Struktur-Snippet (Ansatz c)

Kein produktiver Code – nur die Zielstruktur der Sim/Core-Trennung:

```
Sim/                      // reiner C#-Kern, tick-basiert, keine UnityEngine-Refs
  Units/  Combat/  Resources/  Pathing/   <-- Burst-Jobs lesen/schreiben NativeArrays
Presentation/             // MonoBehaviours: liest Sim-Zustand, spielt Animation/VFX
Data/                     // ScriptableObjects: UnitDef, BuildingDef, FactionDef
Bridge/                   // Sim<->Presentation-Mapping, Pooling, Event-Puffer
```

## Empfehlung

**Entscheidungsvorlage für den DecisionLog (Q-015):**

> Das Studio entscheidet sich für den **hybriden Ansatz (c)**: klassische MonoBehaviour-OOP-Architektur mit ScriptableObject-Datengetriebenheit als tragendes Gerüst, ergänzt um Burst + C# Job System (NativeArray-basiert) für Simulations-Hotspots, mit einer strikt engine-unabhängigen, tick-basierten Simulations-Kernschicht (Sim/Core-Trennung). Unity Entities wird für den MVP **nicht** eingesetzt; eine spätere selektive ECS-Migration des Sim-Kerns bleibt durch die Sim/Core-Trennung vorbereitet.
>
> **Begründung:** Der hybride Ansatz maximiert MVP-Geschwindigkeit, Debugging-Realität, Asset-Store- und URP-Kompatibilität sowie Wartbarkeit durch KI-Coding-Agenten, deckt das Performance-Ziel (100–500+ Einheiten bei 60 FPS) mit Burst-Jobs ab und hält über die Sim/Core-Trennung sowohl den autoritativen Multiplayer als auch Lockstep (Q-013) offen.
>
> **Geprüfte und verworfene Alternativen:**
> - **(a) OOP pur:** verworfen, weil ohne vorgezogene Job-/Burst-Schicht und ohne verbindliche Sim/Core-Trennung das 500+-Einheiten-Ziel und die spätere MP-Autorität unnötig riskiert würden – obwohl es die schnellste MVP-Option wäre.
> - **(b) Voll-DOTS/ECS:** verworfen, weil Unity Entities sich 2025/2026 mitten im Umbruch befindet (Entities 1.4 noch experimental, Core-Package-Umstellung ab Unity 6.4, `EntityId`-Migration mit angekündigten Breaking Changes), Asset-Store-Assets und UI/Canvas-Workflows brechen würden, Debugging/Testbarkeit/KI-Agenten-Wartbarkeit deutlich schlechter sind und der vermeintliche Determinismus-Vorteil für Lockstep entfällt (Burst garantiert keine plattformübergreifende Float-Deterministik). ECS wird als spätere, durch die Sim/Core-Trennung vorbereitete Migrationsoption für den Sim-Kern vorgemerkt.

## Offene Punkte

- **Benchmark-Feinschliff:** Belastbare Zahlen für 500 Einheiten (Sim-Tick + Rendering) unter URP auf Apple Silicon und einer Windows-Referenzmaschine stehen aus – der Performance-Vorteil von (c) gegenüber (a) ist plausibel, aber projektintern noch nicht gemessen.
- **Q-013-Wechselwirkung:** Falls Q-013 zugunsten von Lockstep (statt Server-State-Sync) entschieden wird, ist die Fixed-Point-Strategie des Sim-Kerns (Eigenbau vs. Library) separat zu klären – beeinflusst den Sim-Kern von (c) direkt.
- **Entities-Beobachtung:** Re-Evaluierung von Unity Entities nach Erscheinen von Unity 6.4+ (Core Packages) und finaler 1.4/6.x-API – Termin für Review festlegen.
- **UI-Technologie** (UI Toolkit vs. uGUI) ist unabhängig von dieser Entscheidung, sollte aber konsistent zum Präsentations-Layer gewählt werden.

## Nächste Schritte

1. DecisionLog-Eintrag zu Q-015 gemäß obiger Vorlage anlegen (Verantwortung: Lead Technical Director).
2. Sprint-2-Spike: Minimaler Performance-Benchmark „500 Einheiten bewegen + kämpfen" mit Burst-Jobs/NativeArray in OOP-Hülle; Ziel: <8 ms Sim-Tick auf Apple Silicon.
3. Architektur-Leitplanken-Dokument: Sim/Core-Trennung (erlaubte Abhängigkeiten des Sim-Kerns, Tick-Modell, Event-Puffer) als Review-Pflicht definieren.
4. Abstimmung mit Q-013-Ergebnis (Netzwerkmodell); bei Lockstep-Entscheidung Fixed-Point-Recherche als Folge-Research beauftragen.
5. Kalender-Reminder: Entities-Re-Evaluierung nach Unity-6.4-Release.

## Quellen

- [ECS Development Status – December 2025 (Unity Discussions, offizieller ECS-Team-Post)](https://discussions.unity.com/t/ecs-development-status-december-2025/1699284)
- [ECS development status / milestones – March 2025 (Unity Discussions)](https://discussions.unity.com/t/ecs-development-status-milestones-march-2025/1615810)
- [DOTS development status and milestones + ECS for all – September 2024 (Unity Discussions)](https://discussions.unity.com/t/dots-development-status-and-milestones-ecs-for-all-september-2024/1519286)
- [Entities 1.4 Changelog (Unity Manual)](https://docs.unity3d.com/Packages/com.unity.entities@1.4/changelog/CHANGELOG.html)
- [Entities Graphics – Requirements and compatibility (Unity Manual)](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.0/manual/requirements-and-compatibility.html)
- [Entities Graphics – Feature Matrix (Unity Manual)](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.0/manual/entities-graphics-versions.html)
- [jdxdev-Blog: DOTS/Burst-Determinismus (Sekundärquelle, als Einschätzung gewertet)](https://www.jdxdev.com/blog/tag/unity/)
- [Unity Discussions: Determinismus für RTS-Lockstep](https://discussions.unity.com/t/how-do-i-make-unity-deterministic-for-a-deterministic-lockstep-architecture-rts-development/254743)

## Änderungsverlauf

| Version | Datum | Änderung | Verantwortlich |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Research-Erstfassung | Lead Technical Director |
| 0.1.1 | 2026-07-24 | Quellen vor den terminalen Änderungsverlauf verschoben | Technical Writer |
