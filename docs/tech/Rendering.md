# Rendering

**Version:** 0.2.1 | **Status:** Entwurf – MS-1-Override verbindlich | **Verantwortungsbereich:** Lead Graphics Engineer | **Sprint:** 3

## Zweck

Technisches Design der Render-Pipeline von *Project Nova* auf Unity `6000.5.4f1`, Revision `d550df8bd089`, URP (D-060). Das Dokument legt URP-Konfiguration, Draw-Call-Strategie, FoW-Rendering, Minimap, LOD- und Qualitätsstufen fest. API-Skizzen sind Entwürfe; keine Implementierungslogik. Beleuchtung und Post-Processing sind ausgelagert nach [./Lighting.md](./Lighting.md).

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-056 (MS-1-Umfang), D-057 (Sim/View-Trennung), D-058 (Lastkorridore), D-060 (Unity-Pin), D-061 (Abnahme)
- [./FogOfWar.md](./FogOfWar.md) – verbindlicher technischer MS-1-FoW-Vertrag
- [../research/FogOfWar.md](../research/FogOfWar.md) – Ansatz B (Grid/Bitmask-CPU-Modell + Textur-Ausgabe), Full Screen Pass Renderer Feature, RenderGraph-Pflicht, RTHandle-Persistenz
- [../research/Animation_Audio_UI.md](../research/Animation_Audio_UI.md) – Healthbars als gebatchtes Mesh/Shader-Overlay, Minimap via RenderTexture, 3-stufiges LOD
- [../research/Unity_BestPractices.md](../research/Unity_BestPractices.md) – SRP Batcher, GPU Resident Drawer, Profiler-Disziplin
- [../gamedesign/FogOfWar.md](../gamedesign/FogOfWar.md) – drei Sichtzustände, Darstellungsregeln (Easing ~0,3 s, Ghost-Gebäude)
- [../gamedesign/Biomes.md](../gamedesign/Biomes.md) – Farbpaletten, Wetter-/Hazard-VFX-Bedarf
- [./Lighting.md](./Lighting.md) – Post-Processing-Stack, Emissive/Bloom, Licht-Budgets

## MS-1-Override (D-056/D-058/D-060/D-061)

MS-1 rendert exakt zwei aktive Spielerfraktionen auf Glutrinne; acht
Spieler-Slots bleiben nur reserviert. Das produktive Abnahmeszenario umfasst
100 Einheiten. 500 Renderobjekte sind ausschließlich der synthetische
V2-Lasttest und dürfen Dummy-/Scale-Fixtures sein. Verbindlich ist der
FoW-Vertrag aus [FogOfWar.md](FogOfWar.md); Wetter, Hazards, Luft,
Evolvierte, finale Zerstörungsart und weitere Biome sind Post-MVP.

## Grundprinzipien

1. **View ist strikt von der Simulation getrennt (D-033, Regel 2):** Das Rendering liest ausschließlich aus einem präsentationsseitigen Snapshot des Sim-States; es mutiert nichts und wird niemals vom Sim-Tick aufgerufen. Render-Code darf Unity-APIs voll nutzen (D-035).
2. **RenderGraph-konform von Tag 1 (D-060-Konsequenz):** Alle Custom Passes werden als `ScriptableRendererFeature` mit RenderGraph-API gebaut; keine `CommandBuffer.Blit`-Basismuster (Migrationsschuld-Verbot).
3. **Desktop-first, Metal und DX gleichberechtigt:** Kein plattformspezifischer Code-Pfad ohne dokumentierten Grund; alle Texturen/Passes auf R8/RG8/RGBA32 und Standard-URP-Features beschränkt, wo möglich (Metal-Risiko laut FoW-Research niedrig halten).
4. **Datengetrieben:** Render-relevante Parameter (Qualitätsstufen, LOD-Distanzen, Team-Farben, FoW-Darstellung) liegen in ScriptableObjects (`RenderQualityProfile`, `TeamColorProfile`), nicht in Code-Konstanten.

## URP-Setup (Pipeline-Asset-Konfiguration)

Ein zentrales **URP Asset** `Nova-URP-Asset` mit drei Qualitäts-Varianten (s. Qualitätsstufen), ein **Renderer** `Nova-Forward-Renderer` (Forward, kein Forward+ – Anzahl dynamischer Lichter ist bewusst klein gehalten, siehe Lighting.md).

| Einstellung | Festlegung | Begründung |
|---|---|---|
| SRP Batcher | **an** | Basis für geringe CPU-Kosten bei vielen Materialvarianten; alle Custom Shader SRP-Batcher-kompatibel (eine `UnityPerMaterial`-CB-Struktur) |
| GPU Resident Drawer | **an, Spike-Pflicht** | Verspricht Draw-Call-Reduktion ohne manuelles Instancing-Batching; Achtung: primär für statische/instanziierte Renderer ausgelegt – Verhalten mit 500 per `GameObject.SetActive`/Layer-Swap (FoW-Culling) und Code-Animation bewegten Einheiten ist **Pflicht-Validierung am Phase-0-Spike** (DecisionLog, Offene Punkte). Fallback: explizites `Graphics.RenderMeshInstanced`-Batching im eigenen `UnitRenderBatcher` |
| STP / Upscaling | **STP (Spatial-Temporal Post-Upsampling) aktiv ab Stufe Mittel** | Kostenfreie Schärfe bei reduzierter RenderScale auf schwächeren GPUs; TAA-Alternative im URP-6-Kontext. DLSS/FSR-Plugin erst bei Bedarf ab Beta evaluieren |
| Dynamic Batching | **aus** | wirkungslos bei SRP Batcher + GPU Resident Drawer, erzeugt nur CPU-Kosten |
| HDR | **an** | Voraussetzung für Bloom (Aetherium-Glow, Explosionen, Lighting.md) |
| MSAA | 2× (Stufe Hoch: 4×) | RTS-Kamera glättet vor allem Kanten; STP übernimmt Rest |
| RenderGraph-Compatibility Mode | **aus** | erzwingt RenderGraph-konforme Custom Passes von Anfang an (D-060-Konsequenz, keine Migrationsschuld) |
| Depth/Opaque Texture | Depth **an**, Opaque **aus** | Depth für FoW-Höhenfading und Selektionsringe; Opaque-Textur nicht benötigt (kein Refraktions-/Distortion-Effekt im Scope) |
| Schatten | siehe [./Lighting.md](./Lighting.md) (Schatten-Budget) | – |

## Draw-Call-Strategie für den synthetischen 500-Objekt-Test

Ziel: ≤ 300 Draw Calls im Sicht-Set bei 500 synthetischen Renderobjekten +
Basis + Umgebung auf dem Referenz-PC, 60 FPS. Dies ist V2 und keine
Produktiv-Content-Anforderung.

- **Einheiten (bewegt):** Pro Einheitentyp genau **ein Mesh + ein Material** (SRP-Batcher-fähig). Team-Farbe, Beschädigungsstufe und Tarnzustand werden über `MaterialPropertyBlock`-freie, instancing-kompatible Wege gesetzt (per-Instanz über `GraphicsBuffer`/Custom-Data im Shader oder GPU Resident Drawer Batch-Group). Infanterie mit Skinning bleibt kleinster Typen-Teilmenge (Animations-Strategie: Mecanim nur Infanterie, Research-Vorgabe).
- **Gebäude (statisch):** **Kombinierte Meshes pro Basis-Cluster** – beim Bau-Abschluss wird die unbewegliche Basis-Geometrie (Fundament + Rumpf) in Clustern statisch kombiniert; bewegliche Teile (Türme, Radar-Schüssel, Bau-Kran) bleiben eigene Renderer. Begründung: Gebäude ändern sich selten; Kombinieren senkt Draw Calls drastisch bei großen Basen. Re-Kombinierung ereignisgesteuert (Bau/Zerstörung), nie pro Frame.
- **Vegetation/Dekor:** GPU-Instancing über Platzierungs-Daten aus dem Map-Asset; brennbare Vegetation (D-012) bekommt einen Brand-Parameter pro Instanz (s. Overlays).
- **Healthbar-/Selektions-Overlay:** **ein einziges gebatchtes Mesh** (Research-Vorgabe Animation_Audio_UI.md): ein `HealthbarOverlayRenderer` baut pro Frame (bzw. pro Änderung) ein dynamisches Mesh aus Quads für Lebenspunkte, Selektionsrahmen und Gruppen-Marker, gezeichnet mit einem Draw Call in einem Overlay-Material (Depth-Test gegen Szenen-Depth, kein UI-System involviert). Datenquelle: Sim-Snapshot (HP, Owner, Selektion). Kein Canvas, kein uGUI im World-Space für diesen Zweck.
- **Projektile/Tracer:** gepoolte instanziierte Quads/Meshes, ein Material pro Geschoss-Klasse.

```csharp
namespace Nova.Presentation.Rendering
{
    /// <summary>Baut und zeichnet das gebündelte Over-Unit-Overlay (1 Draw Call).</summary>
    public interface IHealthbarOverlayRenderer
    {
        /// <summary>Daten aus dem letzten Sim-Snapshot; wird vom View-Layer pro Frame aufgerufen.</summary>
        void UpdateInstances(in UnitVisualSpan units, SelectionState selection, TeamColorProfile colors);
    }

    /// <summary>Schreibgeschützte, burst-freundliche Sicht auf die sichtbaren Einheiten eines Frames.</summary>
    public readonly struct UnitVisualSpan { /* NativeArray-Wrapper: Position, HP01, Owner, Flags */ }

    /// <summary>SO: Team-/Fraktionsfarben und Overlay-Formsprache (Stylized Military Sci-Fi).</summary>
    public sealed class TeamColorProfile : ScriptableObject
    {
        public Color[] PlayerColors;      // bis zu 8 Spieler (FoW-Team-Bitmasken, Research FoW)
        public Color AllyHealthbar;
        public Color EnemyHealthbar;
        public float SelectionRingWidth;
    }
}
```

## Fog-of-War-Rendering

Umsetzung exakt nach FoW-Research (Ansatz B) und gamedesign/FogOfWar.md:

- **Datenquelle:** Das committed CPU-Sichtgrid aus
  [FogOfWar.md](FogOfWar.md) besitzt für Glutrinne exakt 128×128 Zellen bei
  1 m und wird mit 5 Hz aktualisiert. Der View-Layer erhält aus dem
  gefilterten Snapshot je Commit eine **Textur-Kopie** (`RG8`: R = sichtbar,
  G = erforscht); das sind 163.840 Byte/s ohne Protokoll-Overhead. Er liest
  weder mutable Sim-Strukturen noch den 256²-/5–10-Hz-Vollspielentwurf.
- **Darstellung Hauptkamera:** **Full Screen Pass Renderer Feature** (`NovaFogOfWarFeature`, RenderGraph-konform), injiziert nach `AfterRenderingOpaques`/vor Post-Processing. Das FoW-Material sampelt die FoW-Textur in Welt-XZ-Koordinaten (rekonstruiert aus Depth), upsampled bilinear mit optionalem 3×3-Blur und mappt die drei Zustände: unerforscht → schwarz; erforscht → entsättigt/~40 % Helligkeit (gamedesign-Festlegung); sichtbar → unverändert. Übergang per zeitlichem **Easing (~0,3 s)** im Shader gegen die Vorgänger-Textur (Ping-Pong via `RTHandle`, persistent über Frames – etablierte URP-Praxis), damit der Sicht-Tick nicht poppt.
- **Einheiten-Culling:** Sichtbarkeit einzelner Einheiten kommt nicht aus dem Fullscreen-Pass, sondern aus `IsVisible(position, teamMask)` (Grid-Lookup im View-Layer); versteckte Einheiten werden per Renderer-Deaktivierung/Layer-Swap ausgeblendet (Gemserk-Muster, Research). Damit korreliert Angriffs-/Auswahl-Logik nie mit der Darstellung – beide lesen dieselbe Grid-Wahrheit.
- **Ghost-Gebäude:** Beim Verlust der Sicht auf eine Zelle mit Gebäude wird ein gedämpftes Ghost-Rendering (separater, billiger Shader-Pfad des Gebäude-Materials, `ghostTint`) aktiviert und beim Wiedersehen aktualisiert/entfernt. Ghosts sind View-only, gespeist aus dem FoW-Snapshot.
- **Radar:** Minimap-Pings sind reines UI-Overlay (kein Einfluss auf die Hauptkamera; gamedesign-Festlegung).

## Minimap

- **RenderTexture-Ansatz (Research-Vorgabe):** Dedizierte orthografische Top-Down-Kamera rendert in eine `RenderTexture` (Ziel 512×512, Culling auf Minimap-Layer: Terrain-Proxy, Gebäude-Icons, Einheiten-Dots, Aetherium-Felder). Anzeige im HUD über UI-Toolkit-fähige RenderTexture-Einbindung (UI-Research: machbar, Detail-Validierung offen).
- Die Minimap sampelt **dieselbe FoW-Textur** wie der Hauptpass (eigenes Minimap-Material, gleiche Daten – kein zweites System): unerforscht = schwarz, erforscht = entsättigt, sichtbar = voll.
- **Update-Strategie:** Terrain-/Feld-Layer nur bei Änderung (Aetherium-Ausbreitung, Trümmer) neu rendern; dynamische Dots/Pings pro UI-Tick (10 Hz reichen, Easing im Shader). Spart gegenüber Voll-Render pro Frame deutlich GPU-Zeit.

## Zerstörung, Trümmer, Brände, Verseuchung (D-012)

- **Zerstörungs-Sequenz Gebäude:** Stufen-Modell statt Physik-Simulation: intakt → beschädigt (Deform-/Schadenstextur-Blend via Shader-Parameter `damage01`) → Zerstörungs-VFX (gepoolter Partikel-/Mesh-Debris-Burst, nicht persistent) → **Trümmer-Decal/Overlay** am Boden. Trümmer persistieren als günstige flache Mesh-Variante (oder kombiniert in den Basis-Cluster), weil sie Pathfinding-relevant sind (Dirty-Flagging, D-034) und als Slow-Zone wirken (Biomes.md, Verlassene Stadt).
- **Brände (Vegetation, D-012):** Pro Instanz ein Brand-Parameter (`burn01`) im Vegetations-Instancing-Buffer; Darstellung = gepooltes Partikel-Feuer + Verkohlungs-Blend im Shader. Ausbreitungs-Logik liegt in der Sim; der View liest nur den Zustand. Mond/Mars: keine Brände (D-028) – Parameter dort ungenutzt.
- **Verseuchung/Aetherium-Überernte:** Zonen-Overlay als **Decal-freier Fullscreen- oder Terrain-Layer**, gespeist aus derselben Grid-Infrastruktur wie FoW (zusätzlicher Kanal/eigene kleine Textur aus dem Sim-Grid). Bewusst kein URP-Decal-Projector-Einsatz für globale Zustandsmasken (FoW-Research-Verwerfungsgrund gilt analog).
- **Aetherium-Felder:** Kristall-Meshes per Instancing; Wachstum/Überernte-Stufen als Skalierungs-/Emissive-Parameter pro Instanz; Glow-Intensität ist Sache von [./Lighting.md](./Lighting.md) (Emissive + Bloom).

## LOD-Strategie

Drei LOD-Stufen (Research-Vorgabe), als Distanz-Schwellen im `RenderQualityProfile` datengetrieben; die RTS-Standardkamera (D-019) hat nur ~2–3 relevante Zoom-Höhen, daher LOD-Bias aggressiv zugunsten der mittleren Stufe:

| Stufe | Einheiten | Gebäude | Vegetation |
|---|---|---|---|
| LOD 0 (Nah/Zoom) | volles Mesh, volle Animationsrate | volles Mesh, bewegliche Teile aktiv | volles Mesh + Wind |
| LOD 1 (Standard-RTS-Höhe) | reduziertes Mesh (~40–60 % Tris), Animationsrate 15–30 Hz | volles Mesh, bewegliche Teile reduziert | vereinfacht, kein Wind |
| LOD 2 (Ausgezoomt) | Billboard-fähiges Low-Mesh, Animator pausiert/Einzelpose | Low-Mesh | ausgeblendet/imposter |

Unity-`LODGroup` für statische Objekte; für instanziierte Einheiten LOD-Auswahl im Batcher (Distanzklasse pro Instanz), damit Instancing nicht durch LOD-Wechsel zersplittert.

## Qualitätsstufen (Desktop-Profile)

Drei Profile als `RenderQualityProfile`-SOs, umschaltbar zur Laufzeit; Ziel-Hardware Desktop Win/macOS (D-060):

| Stufe | RenderScale | Schatten | LOD-Bias | Post | Besonderheiten |
|---|---|---|---|---|---|
| Niedrig | 0,75 + STP | reduziert (s. Lighting.md) | +1 Stufe | Bloom an, Rest aus | Minimap 256² |
| Mittel | 1,0 + STP | Standard | Standard | voller Stack | – |
| Hoch | 1,0 | erweitert | −0,5 Stufen | voller Stack + 4× MSAA | Referenz-Profil für Screenshots |

Auto-Detect beim ersten Start (GPU-Tier), manuell übersteuerbar; kein dynamisches Resolution-Scaling im MVP (Komplexität vs. Nutzen).

## Art-Direction-Anbindung

- **Stil:** Stylized Military Sci-Fi – klare Silhouetten, leicht entsättigte Basis-Paletten pro Biom ([../gamedesign/Biomes.md](../gamedesign/Biomes.md)), lesbare Formsprache vor Detailfülle. Shader-Set: URP Lit (Toon-nahe, reduzierte Speculars) + ein projekteigener `NovaUnit`-Shader (Basis-Farbe, Team-Maske, Damage-Blend, Ghost-Tint, Stealth-Dither).
- **Team-Farben:** per **Team-Color-Shader** – Einheiten-Texturen tragen eine dedizierte Maske (Team-Mask-Channel); der Shader ersetzt maskierte Bereiche durch die `PlayerColors` aus `TeamColorProfile`. Fraktions-Identitätsfarben (Allianz/Legion/Evolvierte) bleiben im unmaskierten Bereich erhalten – Spielerfarbe ≠ Fraktionsfarbe, beides gleichzeitig lesbar.
- **Lesbarkeits-Regel (D-019):** Kein Effekt darf Einheitensilhouette, Team-Farbe oder HP-Overlay überdecken; VFX-Dichte pro Gefecht gedeckelt (Budget-Angabe folgt mit VFX-Dokument).

## Offene Punkte

- **GPU Resident Drawer mit dynamischen Einheiten:** Verhalten bei per-Frame-Layer-Swaps (FoW-Culling), Code-animierten Transforms und Skinned Renderern ist unklar – **Pflicht-Validierung am Phase-0-Spike** (DecisionLog, Offene Punkte). Fallback (eigener `UnitRenderBatcher` mit `RenderMeshInstanced`) ist hier vorgedacht, aber nicht entschieden.
- **STP-Qualität auf Metal:** STP-Verhalten/Bildqualität auf Apple Silicon vs. Windows-GPU nicht verifiziert; falls Metal-Artefakte: Stufenplan „STP nur Windows" wäre nötig – Status: offen, Phase-0-Spike.
- **Minimap in UI Toolkit:** RenderTexture-Einbindung im gepinnten Editor hands-on validieren; uGUI-`RawImage` als dokumentierter Fallback – Status: offen, kein G0-Blocker.
- **Ghost-Gebäude & kombinierte Meshes:** Gebäude-Cluster-Kombinierung vs. pro-Gebäude-Ghost-Zustände können kollidieren (Ghost braucht eigenen Materialzustand innerhalb eines kombinierten Meshs); Auflösung (Sub-Mesh-Split vs. Ghost als separates Overlay-Mesh) – Status: offen, Sprint 7.
- **Trümmer-Persistenz-Menge:** Kein Limit für Trümmer-Overlays definiert (Speicher-/Draw-Budget bei langen Matches 35 min); Limit-Politik (Fade-out vs. Cap) ist Design-nahe und hier nicht entschieden – Status: offen, Abstimmung mit Game Design.
- **VFX-Dichte-Budget:** Konkrete Partikel-/Overdraw-Budgets für Gefechte fehlen (VFX-Dokument nicht Gegenstand dieses Auftrags) – Status: offen, Input an Sprint-5-Asset-Pipeline.

## Nächste Schritte

1. Phase-0-Spike: GPU Resident Drawer + STP auf Metal/Windows mit 500 Dummy-Einheiten vermessen (Ergebnis → DecisionLog).
2. Sprint 7: `NovaFogOfWarFeature` (Full Screen Pass, RenderGraph) + FoW-Textur-Pipeline aus dem Sim-Grid implementieren.
3. Sprint 7: `HealthbarOverlayRenderer` + `NovaUnit`-Shader (Team-Maske, Damage, Ghost) umsetzen.
4. `RenderQualityProfile`-SOs anlegen und drei Stufen gegen die Referenzszene profilieren.
5. Offene Punkte Ghost/Mesh-Kombinierung und Trümmer-Limit mit Game Design bzw. im Sprint-7-Review klären.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Graphics Engineer |
| 0.2.0 | 2026-07-24 | MS-1-Renderumfang, synthetischen V2-Lasttest, verbindlichen FoW-Vertrag und Unity-Pin gemäß D-056/D-058/D-060/D-061 festgelegt | Lead Graphics Engineer |
| 0.2.1 | 2026-07-24 | FoW-Upload auf verbindliches 128²-Teamgrid bei exakt 5 Hz korrigiert | Lead Graphics Engineer |
