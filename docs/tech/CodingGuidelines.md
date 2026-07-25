# Coding Guidelines

**Version:** 1.0.0 | **Status:** verbindlich für MS-1 – G0 aktiv | **Verantwortungsbereich:** Lead Technical Director | **Sprint:** 7

## Zweck

Definiert determinismuskritische C#-Regeln für Core, Simulation, AI und Hosts.
Style-Fragen sind nachrangig gegenüber reproduzierbaren Bytes und klaren
Assembly-Grenzen.

## Abhängigkeiten

- [Architecture.md](Architecture.md) und
  [DependencyGraph.md](DependencyGraph.md)
- [SimulationCore.md](SimulationCore.md), [Commands.md](Commands.md) und
  [GameState.md](GameState.md)
- [Testing.md](Testing.md)
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-057/D-061

## 1. Autoritative Numerik

In Core-/Simulation-State und Berechnungen:

- ausschließlich `SimFixed` Q16.16 und definierte Integerbreiten;
- `int64` für Zwischenprodukte;
- nearest ties-to-even;
- Welt→Grid floor;
- checked, deterministische Faults;
- Wrap ausschließlich bei `SimAngle uint16`.

Verboten: `float`, `double`, `decimal`, Unity-/System-Physik,
`Unity.Mathematics`, plattformabhängige Transzendentfunktionen, Saturation und
stilles Overflow. Analyzer und Architecture Tests erzwingen dies ab G1.

## 2. Deterministische Datenstrukturen

- keine autoritative Dictionary-/HashSet-Iteration;
- stabile IDs und explizite Sortierschlüssel;
- feste Kapazität und deterministische Backpressure;
- keine Zeit, Culture, Locale, Thread-ID oder Prozesszufallsquelle;
- kein `System.Random`, `Guid.NewGuid` oder Unity Random;
- Counts/Längen vor Allokation prüfen.

Falls ein Lookup eine Hashstruktur nutzt, wird die Verarbeitung separat in
kanonische Reihenfolge überführt.

## 3. Mutation und Commands

UI und AI erzeugen nur `CommandIntent`. Session/Ingress bindet Slot,
Sequence und TargetTick. Simulation akzeptiert ausschließlich versiegelte
`CommandBatch`-Objekte.

Keine öffentlichen mutable State-Collections, Singleton-Setter,
`FindObjectOfType`-Seiteneffekte oder Test-Backdoors. Systemticks mutieren nur
ihren in [ModuleOverview.md](ModuleOverview.md) zugewiesenen Block.

## 4. Assembly-Regeln

- Core/Simulation: kein Unity-Referenzpaket.
- Simulation referenziert AI nicht.
- AI liest nur committed Read-Views und erzeugt Intents.
- Presentation/UI erhalten gefilterte Snapshots.
- SimRunner und Unity verwenden dieselben Sources/Defines.
- keine kopierten Serializer, PRNGs, Hashes oder Commands.

## 5. Speicher und Hotpaths

Im autoritativen Tick:

- 0 B Managed-GC,
- vorallokierte Buffer/Pools,
- keine LINQ-, Closure-, Boxing- oder Stringformatierung,
- keine dynamische Capacity-Erweiterung,
- High-Water-/Backpressure-Metriken.

Optimierung darf Reihenfolge oder Bytes nicht verändern.

## 6. Managed/Burst

Managed ist der einzige MS-1-Produkt- und Messpfad. Burst bleibt deaktiviert.
Kein Feature-Flag darf im Release unbemerkt eine zweite Autorität aktivieren.

Eine spätere Aktivierung erfordert neue D-ID und exakte Parität für alle
autoritativen Felder, State-Hashes und finalen Bytes. Relative Hash- oder
Numeriktoleranz ist kein Ersatz.

## 7. Serialisierung und Versionen

- Little Endian, explizite Breiten und Feldreihenfolge;
- Schema-/Payload-Versionen sind immutable;
- unbekanntes Pflichtfeld/-schema ist Fehler;
- Replay wird nicht migriert;
- Save-Migration nur explizit, testbar und ohne stillen Default;
- jede neue State-Feldklasse erhält Hashsensitivitäts- und Roundtrip-Test.

## 8. Fehlerbehandlung

Vertragsverletzungen liefern stabile Resultcodes und hinterlassen keinen
teilmutierten State. Parser/Ingress validieren in temporäre Objekte. Logs sind
nicht autoritativ und dürfen keine Reihenfolge beeinflussen.

## 9. Test- und Reviewregeln

Pflicht für determinismuskritische Änderungen:

- Golden Bytes/Vektoren,
- Plattform- und Restore-Fortsetzung,
- negative/invalid cases,
- Coverage gemäß [Testing.md](Testing.md),
- unabhängiger Reviewer ≠ Writer.

Ein deaktivierter, skipped oder quarantined gatekritischer Test ist Fail.

## Offene Punkte

- Konkrete Analyzer-Paket- und SDK-Pins werden in G0 festgelegt.

## Nächste Schritte

1. G0-Architecture-/Analyzer-Grundlage schaffen.
2. G1-Numeric-/Command-/State-Regeln als Blocking Checks aktivieren.
3. Keine Burst-Arbeit vor neuer D-ID beginnen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Technical Director |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings): CommandEnvelope boxfrei + Tick-/Sequenzvergabe + Issuer-Regel (Review F-5/F-7), Managed-first & Toleranz-Parität ≤1e-4 (D-045), Nova.AI/Nova.AI.Data in Layer-Tabelle (D-043), Registry-Sharding-Zugriff (D-049), Analyzer-Enforcement als Sprint-7-Pflicht-Backlog konkretisiert | Lead Technical Director |
| 1.0.0 | 2026-07-24 | Guidelines auf Q16.16 ab G1, exakte Parität, feste Kapazitäten und D-061-Test-/Reviewregeln rebaselined | Lead Technical Director |
