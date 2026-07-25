# KI-Architektur

**Version:** 1.0.0 | **Status:** verbindlich für MS-1 – G0 aktiv | **Verantwortungsbereich:** Lead AI Programmer | **Sprint:** 7

## Zweck

Definiert die MS-1-Skirmish-KI als versionierten Session-Sidecar. Sie spielt
Allianz oder Legion über dieselben committed Informationen und Commands wie
ein Mensch.

## Abhängigkeiten

- [SimulationCore.md](SimulationCore.md) – AI-Sidecar-Grenze
- [Commands.md](Commands.md) – Intent/Ingress
- [FogOfWar.md](FogOfWar.md) – einzige Weltansicht
- [Pathfinding.md](Pathfinding.md)
- [../production/MVPContentManifest.md](../production/MVPContentManifest.md)
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-056,
  D-057 und D-061

## 1. Vertrauensgrenze

`Nova.AI`:

- liest ausschließlich `TeamWorldView` der zuletzt committed Sicht;
- erzeugt ausschließlich `CommandIntent`;
- besitzt keinen direkten Entity-Store-, Economy-, Combat-, FoW- oder
  Pathfinding-Zugriff;
- mutiert keinen Sim-State und
- wird von `Nova.Simulation` nicht referenziert.

Der Session-Host führt die KI nach einem committed Snapshot aus und gibt ihre
Intents an denselben `MatchSession`-/`CommandIngress` wie Human-Intents.
PlayerSlot, Sequence und TargetTick werden nicht von der KI gewählt.

## 2. MS-1-Verhalten

Die KI muss den vollständigen Closed-Core bedienen:

- Startökonomie und Energie,
- Builder-/Harvester-Produktion,
- alle neun Gebäude-Rollen,
- direkte T2-Freischaltung durch ResearchLab,
- alle acht Einheiten-Rollen,
- MG-/Rocket-DefenseModule,
- Aufklärung und committed FoW,
- Angriffe, Rückzug und Artillerie als Finisher,
- D-010-Feldpflege: finite Reserve, Regrowth, Spread,
  Overharvest-Warnung/-Vermeidung und contested Expansion.

Sie darf keine zurückgestellten Fähigkeiten, Capture-, Neutral-, Wetter-,
Luft-, T3- oder Online-Regeln voraussetzen.

## 3. Fraktionsprofile

| Profil | Planungsleitplanke |
|---|---|
| Allianz | höhere Kosten, Erhaltung, Präzision/Single-Target, 330-AE-Cargo |
| Legion | niedrigere Kosten, schnellere Produktion, Salven/Splash, 300-AE-Cargo |

Profile verändern Definitionen/Prioritäten, nicht Regeln oder Sicht. Es gibt
keinen KI-Bonus auf Ressourcen, Sicht, Produktionstick oder Command-Latenz in
der Abnahme.

## 4. Sidecar-State

Der versionierte `AiSidecar` enthält nur fortsetzungsrelevante KI-Daten:

- Schema-/Profil-ID,
- eigener deterministischer PRNG-State, falls genutzt,
- Plan-/Task-/Squad-IDs und stabile Queues,
- Timer in Ticks,
- letzter konsumierter `TeamWorldView`-Tick und
- offene Intents, bevor sie am Ingress gebunden wurden.

Keine Unity-Typen, Floats, Strings, GUIDs oder Dictionaries in
fortsetzungsrelevantem State. Save und Restore müssen dieselben späteren
Intents erzeugen.

## 5. Replay

Live-Matches zeichnen jeden akzeptierten KI-Command gemeinsam mit Human-
Commands auf. Replay-Playback:

- instanziiert die KI nicht,
- wendet keinen AI-Sidecar an und
- spielt nur den kanonischen Command-Strom.

Eine Shadow-KI darf diagnostisch Abweichungen melden, aber keinen Command
hinzufügen, entfernen oder ersetzen.

## 6. Hidden-World-Sicherheit

Metamorphic-Tests verändern ausschließlich verborgene Gegnerdaten. Solange
diese nicht committed sichtbar werden, müssen:

- AI-Intents byteidentisch bleiben,
- Plan-/Sidecar-Hashes identisch bleiben und
- keine Entity-ID, Ressource, Queue oder Zielposition leaken.

Radar-Signatur-Pings dürfen Prioritäten beeinflussen, aber kein Targeting oder
verborgene ID liefern.

## 7. Last und Kadenz

G3 verlangt V5b mit realer KI/Combat bei 500 Agenten: kein Crash, kein
unbeschränktes Wachstum, vollständige Rohwerte. Dies ist Diagnose.
Produktakzeptanz bleibt 100 Einheiten.

Headless-Kadenz und gültige Matchausgaben folgen [Testing.md](Testing.md).
Fehlerhafte/ungültige Matches bleiben im Nenner.

## Offene Punkte

- Schwierigkeitsstufen, Cheats, weitere Fraktionen und Online-AI sind
  Post-MVP.

## Nächste Schritte

1. Read-/Intent-Verträge in G1 einfrieren.
2. G2-Graybox-View für Aetherium und FoW bereitstellen.
3. G3 Hidden-World-, Save-/Replay- und V5b-Nachweise führen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead AI Programmer |
| 1.0.0 | 2026-07-24 | KI auf committed TeamWorldView, kanonische Intents, versionierten Sidecar und Closed-Core-Aetherium D-056/D-057 rebaselined | Lead AI Programmer |
