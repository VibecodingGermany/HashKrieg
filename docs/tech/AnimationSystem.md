# Animation System

**Version:** 0.2.1 | **Status:** Entwurf – MS-1-Override verbindlich | **Verantwortungsbereich:** Lead Graphics Engineer | **Sprint:** 3

## Zweck

Technisches Design des Animations-Systems von Project Nova: Hybrid-Modell aus Mecanim-Skeletal-Animation (nur Infanterie) und rig-loser Code-/Transform-Animation (Fahrzeuge, Gebäude), 3-stufiges Animations-LOD, Kopplung an Simulations-Events (Mündungsfeuer, Zerstörung, Evolvierte-Wachstum) und datengetriebene AnimationClip-Referenzen über ScriptableObjects. Umsetzungsreif für Sprint 7, ohne Implementierungslogik.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – insb. D-056 (MS-1-Umfang), D-057 (Sim/View-Trennung), D-058 (Lastkorridore), D-060 (Unity-Pin), D-061 (Abnahme)
- [../research/Animation_Audio_UI.md](../research/Animation_Audio_UI.md) – Optionsvergleich Animation (Hybrid-Empfehlung, LOD-Pflicht, Animator-vs.-Playables-Prototyp)
- [../vision/CoreGameplay.md](../vision/CoreGameplay.md) – Feel-Bausteine (Wrack-Liegezeit 20–30 s, Treffer-Feedback ohne Zahlen)
- [../gamedesign/Buildings.md](../gamedesign/Buildings.md) – Baufortschritt, Keim/Reifung (D-011), Modul-Upgrades der Verteidigungsplattform
- [../gamedesign/Infantry.md](../gamedesign/Infantry.md), [../gamedesign/Vehicles.md](../gamedesign/Vehicles.md), [../gamedesign/Aircraft.md](../gamedesign/Aircraft.md) – Einheitentypen und Bewegungsformen
- [./GameState.md](./GameState.md) – Sim-Event-Katalog und Zustandsdaten, aus denen die View liest (Schwestervorgabe D-033)
- [./Pathfinding.md](./Pathfinding.md) – Bewegungsdaten (Geschwindigkeit, Richtung, Steering-Status) als Animations-Input

## MS-1-Override (D-056/D-058/D-060/D-061)

MS-1 animiert nur die in
[MVPContentManifest.md](../production/MVPContentManifest.md) freigegebenen
Allianz-/Legion-Einheiten und neun Gebäuderollen. Luft, Evolvierte,
Flak-Module, aktive Einheitenfähigkeiten und der Vollroster sind Post-MVP.
Die MG- und Rocket-Module der Verteidigungsplattform samt sichtbarem
Sub-Mesh-Wechsel gehören dagegen zum MS-1-Produktionspfad.
Die produktive Abnahme umfasst 100 Einheiten; 500 Animationsobjekte sind ein
synthetischer V3-Lasttest. Der Animationsanteil wird dort mit
CPU-P95 ≤1,5 ms abgenommen. Referenzeditor ist Unity `6000.5.4f1`, Revision
`d550df8bd089`, URP.

## Architektur-Grundsätze

1. **Animation ist reine View.** Sie liest ausschließlich aus dem Simulations-State und aus Sim-Events; sie schreibt niemals in die Simulation zurück (D-033, Regel 2). Animation muss **nicht deterministisch** sein – Clips dürfen frei interpolieren, zufallsvariiert (Varianten-Auswahl) und mit unterschiedlichen Raten aktualisiert werden.
2. **Keine UnityEngine-Abhängigkeit der Sim umkehren:** `Nova.Simulation` kennt keine Animation. Die Brücke ist ein rein lesender View-Adapter in der Präsentations-Assembly.
3. **Datengetrieben:** Alle Clip-Referenzen, LOD-Schwellen und prozeduralen Parameter liegen in Definitions-SOs der GameDatabase (Definitions-only, kein Runtime-State in SOs – Vier-Säulen-Regel).
4. **Namespace-Trennung:** `Nova.Presentation.Animation` (View, Unity-APIs erlaubt) vs. `Nova.Simulation` (Unity-frei, D-035).

## Hybrid-Modell

### Skeletal-Animation: nur humanoide Infanterie (Mecanim)

- Mecanim/Humanoid-Rigs **ausschließlich** für Infanterie; Retargeting über das Humanoid-System erlaubt Wiederverwendung von Mixamo-/Asset-Store-Clips über alle humanoiden Typen (Budget-Hebel, s. Research).
- Pro Infanterietyp genau **ein schlanker Animator-Controller** (Idle/Move/Attack/Death + Spezialzustände), kein tiefes State-Machine-Design – Zustandswahl kommt aus der Sim, nicht aus Animator-Logik.
- Evolvierte-Infanterie (z. B. Alpha-Mutant, D-015) nutzt dasselbe Rig-Schema, sofern humanoid; nicht-humanoide Bio-Einheiten fallen unter die Code-Animations-Sparte.
- Verbindliche Performance-Baselines: GPU Skinning an, `Animator.cullingMode = CullCompletely` außerhalb des Frustums, kein `Update When Offscreen`.

### Rig-lose Code-/Transform-Animation: Fahrzeuge, Gebäude, Luft

Fahrzeuge, Mechs ohne Humanoid-Rig, Gebäude und Lufteinheiten erhalten **kein Skelett**. Animation läuft über einen leichtgewichtigen, Burst-tauglichen View-Controller:

- **Fahrwerks-Simulation:** Räder/Ketten rotieren proportional zur Sim-Geschwindigkeit; Hover-/Bein-Imitation über Sinus-Offsets; Aufhängungs-Nicken bei Beschleunigung/Bremsen aus Geschwindigkeits-Delta der letzten Ticks abgeleitet (View-seitig, nicht-deterministisch erlaubt).
- **Turm-/Waffen-Rotation:** Turm-Transform folgt dem aktuellen Ziel-Vektor aus der Sim mit begrenzter Drehrate (Rate kommt aus dem Unit-SO, identisch zum Sim-Wert, damit View und Sim deckungsgleich wirken).
- **Gebäude:** Baufortschritt als Stufen-/Shader-Blend (Fundament → Gerüst → fertig); in MS-1 sind MG und Rocket der Verteidigungsplattform als austauschbare Turm-Sub-Meshes aktiv, Flak bleibt Post-MVP.
- **Luft:** Banking/Pitch aus Steuerkurve der Luft-Steering-Schicht (D-034), Schwebetrudeln im Idle; kein Rig.
- **Evolvierte-Gebäude (D-011):** Keim → Reifung über Wachstums-Shader (Vertex-Displacement/Blend-Shapes, Wachstumsgrad 0–1 aus Sim-Reifestufe) statt Konstruktions-Stufen; Regenerations-Visualisierung als Pulsieren statt Reparatur-Funken.

## Animations-LOD (3 Stufen, Pflicht)

Konfigurierbar pro Einheitentyp im `AnimationSetSO` (Schwellen in Kamerahöhe/Metern und Einheitenanzahl):

| Stufe | Bedingung (Richtwerte) | Verhalten |
|---|---|---|
| 0 – Voll | Kamerahöhe < ~30 m oder < 80 animierte Einheiten sichtbar | Volle Update-Rate, Blending, Übergänge, Fahrwerks-Details |
| 1 – Reduziert | typische RTS-Kamerahöhe (~30–60 m) | Update-Rate 15–30 Hz, keine komplexen Blends, Turm-Rotation in größeren Schritten |
| 2 – None | Fern/Minimap-Zoom (> ~60 m) oder Offscreen | Animator pausiert/Einzelpose, `CullCompletely`, nur Transform-Bewegung |

Die Schwellen sind Startwerte und werden am Phase-0-Spike und an der 60-FPS-Messung mit 500 Einheiten kalibriert. LOD-Auswahl reagiert zusätzlich auf die Gesamtzahl sichtbar animierter Einheiten (globales Budget, nicht nur Distanz).

## Kopplung Sim → View (Animations-Events)

Richtung ist strikt **Sim-Event → View-Reaktion**. Klassische Unity-`AnimationEvent`s, die in Spiellogik zurückfeuern, sind verboten.

- **Mündungsfeuer-Timing:** Die Sim feuert deterministisch im 10-Hz-Tick (D-033). Der Sim-Event `UnitFired` triggert in der View Muzzle-Flash-VFX, Rückstoß-Animation und (über den AudioService) den Schuss-Sound. Die Angriffs-Clip-Länge wird beim Content-Import gegen die Sim-Angriffsintervall-Zeit gespiegelt; Abweichungen bis zu einem Tick (100 ms) sind toleriert, die View dehnt/staucht den Angriffs-Clip per Speed-Multiplikator.
- **Bewegung:** Locomotion-Clip-Speed wird aus der tatsächlichen Sim-Geschwindigkeit skaliert (kein Foot-Sliding), Richtungswechsel über kurze Turn-Blends in LOD 0, hart in LOD 1.
- **Tod/Zerstörung (D-012):** `UnitDied`/`BuildingDestroyed` startet die Zerstörungs-Sequenz: Death-Clip (Infanterie) bzw. prozedurale Zerstörung (Fahrzeug: Kippen + Explosion + Wrack-Mesh-Swap; Gebäude: Kollaps-Stufen + Trümmer-Mesh). Wracks bleiben 20–30 s liegen (CoreGameplay), danach Fade-out. Trümmer, die Pathfinding-relevant sind, werden von der **Sim** per Dirty-Flag gemeldet (D-034) – die View rendert sie nur.
- **Wachstum (D-011):** `BuildingGrowthStageChanged` setzt den Wachstumsgrad-Uniform des Evolvierte-Shaders; Keim-Pflanzen und Reife-Abschluss erhalten je einen One-Shot-Effekt.
- **Selektions-/Befehls-Feedback** (Befehlsmarker, 0,5 s) ist reine View und läuft über das Input-/UI-System, nicht über dieses Dokument.

## Datenmodell (C#-Skizzen)

```csharp
namespace Nova.Presentation.Animation
{
    // Definitions-SO (GameDatabase, definitions-only)
    public sealed class AnimationSetSO : ScriptableObject
    {
        public AnimationKind Kind;                 // SkeletalHumanoid | CodeVehicle | CodeBuilding | CodeAir | OrganicGrowth
        public AnimationClip[] IdleVariants;       // nur SkeletalHumanoid
        public AnimationClip MoveClip;
        public AnimationClip AttackClip;
        public AnimationClip DeathClip;
        public LocomotionParams Locomotion;        // Fahrwerk: Rad-/Ketten-Radien, Hover-Amplitude
        public TurretParams[] Turrets;             // Transform-Pfade + Drehraten-Spiegel
        public GrowthVisuals Growth;               // nur OrganicGrowth: Shader-Property-IDs, Keim-/Reife-Effekte
        public DestructionVisuals Destruction;     // Wrack-/Trümmer-Meshes, Liegezeit 20–30 s
        public LodThresholds Lod;                  // Distanz- und Anzahl-Schwellen der 3 Stufen
    }

    // Rein lesender Adapter Sim → View
    public interface IAnimationView
    {
        void Bind(EntityHandle entity, AnimationSetSO set);
        void ApplyLod(AnimationLodLevel level);
        void OnSimEvent(in SimAnimationEvent evt); // UnitFired, UnitDied, GrowthStageChanged, ...
        void SyncFromState(in RenderSnapshot snap); // Position, Geschwindigkeit, Ziel-Vektor, Reifestufe
    }

    public readonly struct SimAnimationEvent
    {
        public readonly EntityHandle Entity;
        public readonly SimAnimEventKind Kind;     // Fired | Died | GrowthStage | Damaged | Started | Stopped
        public readonly int Payload;               // z. B. Reifestufe, Waffen-Index
    }
}
```

Der `RenderSnapshot` wird vom View-Adapter aus dem serialisierbaren Sim-State (D-033, Regel 5) erzeugt; Sim und View tauschen keine Unity-Typen aus.

## Performance-Budget

- Ziel im synthetischen V3-Lasttest: 60 FPS bei 500 Animationsobjekten;
  Animations-CPU-P95 ≤ 1,5 ms (LOD 1 dominant).
- Maßnahmen: GPU Skinning, `CullCompletely`, LOD-Ratenbegrenzung, rig-lose Masse-Fraktionen (Legion) ohne Skinning-Kosten, Fahrwerks-Updates als Burst-Job über `NativeArray`-Posen, sobald Profiler es erfordert (D-035-Hotspot-Regel).
- Validierung: Phase-0-Spike „Animator vs. Playables bei 500 Einheiten" (DecisionLog, Offene Punkte) liefert die Messbasis für Controller-Design und LOD-Schwellen.

## Offene Punkte

- **Animator vs. Playables/Animancer:** Phase-0-Spike steht noch aus; Ergebnis entscheidet, ob Infanterie-Controller als Animator-Controller oder Playables-Graph gebaut werden. Bis dahin ist dieses Design backend-neutral gehalten (`IAnimationView`).
- **Animancer-Lizenz:** Kosten/Lite-Einschränkungen nur relevant, falls der Spike für Animancer ausfällt (s. Research, Offene Punkte).
- **LOD-Schwellenwerte:** Tabelle oben enthält Richtwerte; verbindliche Werte erst nach Spike- und 500-Einheiten-Messung.
- **Evolvierte-Wachstums-Shader:** Umfang (eigener Shader vs. generischer Displacement-Shader pro Gebäudetyp) hängt vom Art-Direction-Ergebnis ab; Shader-Aufwand ist erhöht (im Research vermerkt).
- **Nicht-humanoide Evolvierte-Infanterie:** Rig-Strategie (Skelett mit generischem Rig vs. Code-Animation) je nach finalen Konzepten offen.
- **Muzzle-Sync-Toleranz:** Ob die 1-Tick-Toleranz (100 ms) bei schnellen Waffen visuell ausreicht, ist am Prototyp zu prüfen; Alternativlösung (Sim-seitiger Sub-Tick-Zeitpunkt im Event) wäre ein Eingriff in GameState.md und ist nicht eigenständig zu entscheiden.

## Nächste Schritte

- Phase-0-Spike „Animator vs. Playables bei 500 Einheiten" durchführen und Ergebnis hier sowie im DecisionLog nachziehen.
- `AnimationSetSO`-Schema mit DataModel/GameDatabase-Design (Sprint-3-Schwesterdokument) abstimmen.
- Sim-Event-Katalog (`SimAnimationEvent`-Kinds) mit ./GameState.md abgleichen; fehlende Events dort anfordern.
- LOD-Schwellen nach Messung fixieren; Art-Direction-Runde für Wachstums-/Zerstörungs-Shader der Evolvierten einplanen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Graphics Engineer |
| 0.2.0 | 2026-07-24 | MS-1-Animationsumfang, synthetischen 500er-Lasttest, CPU-P95-Budget und Unity-Pin gemäß D-056/D-058/D-060/D-061 festgelegt | Lead Graphics Engineer |
| 0.2.1 | 2026-07-24 | MG/Rocket-Module als MS-1-Pflicht klargestellt und nur Flak zurückgestellt | Lead Graphics Engineer |
