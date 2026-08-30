# Lighting & Post-Processing

**Version:** 0.2.1 | **Status:** Entwurf – MS-1-Override verbindlich | **Verantwortungsbereich:** Lead Graphics Engineer | **Sprint:** 3

## Zweck

Technisches Design des Beleuchtungs- und Post-Processing-Konzepts von *Hashkrieg* für Unity `6000.5.4f1`, Revision `d550df8bd089`, URP (D-060). Festgelegt werden: Lichtkonzept pro Biom-Typ, die Realtime-vs.-Baked-Strategie, Schatten-Budget, Aetherium-Glow (Emissive + Bloom), VFX-Beleuchtung (Explosionen) und der URP-Post-Processing-Stack. API-Skizzen sind Entwürfe; keine Implementierungslogik. Pipeline-Setup, FoW und Overlays siehe [./Rendering.md](./Rendering.md).

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-056 (MS-1-Umfang), D-060 (Unity-Pin), D-061 (Abnahme), D-019 (Lesbarkeit)
- [../gamedesign/Biomes.md](../gamedesign/Biomes.md) – 10 Biom-Profile mit Farbpalette/Stimmung und Wetter-/Hazard-Zeitfenstern (verbindliche Stimmungsvorgabe)
- [../gamedesign/Resources.md](../gamedesign/Resources.md) – Überernte-Stufen, Feldzustände (Glow-Träger)
- [../research/FogOfWar.md](../research/FogOfWar.md) – Lesbarkeits-Leitplanken, RenderGraph-Pflicht
- [../research/Unity_BestPractices.md](../research/Unity_BestPractices.md) – URP-Performance-Leitplanken
- [./Rendering.md](./Rendering.md) – Qualitätsstufen, HDR/MSAA, Team-Color-Shader, FoW-Pass-Reihenfolge

## MS-1-Override (D-056/D-060/D-061)

Für MS-1 ist ausschließlich das klare Glutrinne-Lichtprofil verbindlich.
Weitere Biome, Wetterphasen, Hazards, dynamische Tageszeiten und
Evolvierten-spezifische Effekte sind Post-MVP. Abgenommen werden Lesbarkeit,
Teamfarben und funktionales Aetherium-Feedback im freigegebenen Szenario;
finale Art-Qualität ist kein G0–G5-Kriterium.

## Grundprinzipien

1. **Lesbarkeit vor Atmosphäre (D-019):** Lichtstimmung darf Team-Farben, Einheitensilhouetten und HP-Overlays nie überdecken. Kontrast und Schattenlänge sind Budget-Größen, keine Freistil-Parameter.
2. **Ein dominantes Licht:** Pro Szene genau **ein realtime Directional Light** („Sonne"); alle weiteren Lichtquellen sind emissive/fake (Shader, Light Cookies, gepoolte Punktlichter mit hartem Cap).
3. **Kein Baking-Zwang:** Wegen gezielter Zerstörbarkeit und Aetherium-Ausbreitung (D-010/D-012) ist die Umgebung dynamisch → Empfehlung: **vollständig realtime + Light Probes**, kein aufwendiges Lightmap-Baking (Details unten).
4. **Datengetrieben:** Jede Lichtstimmung ist ein flacher Datensatz (`LightingProfile`-SO) pro Biom und Wetter-Phase, referenziert aus dem Biom-Datensatz – konsistent zur Datengetriebenheits-Regel aus [../gamedesign/Biomes.md](../gamedesign/Biomes.md).

## Realtime vs. Baked

**Empfehlung (zur Bestätigung im Sprint-3-Review):** Kein Lightmap-Baking für Gameplay-Szenen.

- **Begründung:** Mauern/Trümmer, brennbare Vegetation, Brücken und Aetherium-Ausbreitung (D-010/D-012) verändern die Szene zur Laufzeit – gebackenes Licht würde ab der ersten Zerstörung lügen. RTS-Kamera (D-019) zeigt große Flächen in mittlerer Detailtiefe: indirektes Licht ist auf dieser Distanz kaum lesbar, der Qualitätsverlust durch Verzicht auf GI ist klein, der Produktionsgewinn (keine Bake-Pipeline pro Karte, keine Bake-Invalidierung) groß.
- **Stattdessen:**
  - **Realtime Directional Light** mit Schatten (einzige Schattenquelle) für Form und Tages-/Hazard-Dramaturgie.
  - **Light Probes** (pro Karte einmalig, grobes Raster) für dynamische Objekte – Einheiten/Gebäude erhalten damit plausibles Umgebungslicht ohne pro-Objekt-Kosten.
  - **Ambient:** URP Environment Lighting (Gradient oder einfache Skybox pro Biom), kein Realtime-GI, kein APV im MVP (Re-Eval ab Alpha, falls Nah-Kamera-Momente in der Kampagne es verlangen).
- **Ausnahme geprüft und verworfen:** Baked GI nur für statische Terrain-Elemente mit Realtime-Overlay für Zerstörbares (Mischmodus) – doppelte Pflege, sichtbarer Helligkeitssprung an Übergängen, kein Mehrwert auf RTS-Distanz.

## Lichtkonzept pro Biom-Typ

Pro Biom ein `LightingProfile` (Sonnenwinkel/-farbe/-intensität, Ambient, Schattenstärke, Post-Referenz). Die Stimmung folgt den Farbpaletten aus [../gamedesign/Biomes.md](../gamedesign/Biomes.md); Werte sind Richtwerte v0.1.

```csharp
namespace Nova.Presentation.Lighting
{
    /// <summary>SO: komplette Lichtstimmung eines Bioms bzw. einer Wetter-Phase.</summary>
    public sealed class LightingProfile : ScriptableObject
    {
        public string BiomeId;
        public float SunAzimuthDeg;        // Richtung der Sonne
        public float SunElevationDeg;      // flach = lange Schatten
        public Color SunColor;
        public float SunIntensity;         // Lux-ähnlich, URP-Einheiten
        public Color AmbientSky;
        public Color AmbientEquator;
        public Color AmbientGround;
        public float ShadowStrength;       // 0..1, Lesbarkeits-Deckel ≤ 0,85
        public VolumeProfile PostVolume;   // Referenz auf URP-Volume (s. Post-Stack)
    }

    /// <summary>View-Service: blendet zwischen LightingProfiles (Wetter-Wechsel, Hazard).</summary>
    public interface ILightingDirector
    {
        void SetBaseProfile(LightingProfile profile);
        /// <summary>Übergang z. B. 15-s-Wetter-Vorwarnung (Biomes.md) als Crossfade.</summary>
        void TransitionTo(LightingProfile target, float durationSeconds);
    }
}
```

| Biom | Sonne | Ambient/Stimmung | Besonderheiten |
|---|---|---|---|
| Wüste | hoch, warm-weiß, hart | gebleicht, heller Boden-Ambient | Sandsturm-Phase: Sonne diffus, Intensität −40 %, Farbe staubig |
| Schnee | tief stehend, kühl-weiß | hohe Bodenreflexion (bläulich) | lange Schatten = Identitätsträger; Schneesturm: flaches Grau |
| Vulkan | tief, rötlich-glutig | dunkler Himmel, warme Ground-Ambient | Lava als **Emissive-Träger** (kein echtes Licht), Aschefall: Dämpfung |
| Dschungel | mittel, warm, weich | sattgrünes Ground-Ambient | Monsun: kühler, flacher; Kronendach-Schatten via Light Cookie |
| Sumpf | flach, diesig-oliv | trübes Grau-Grün, geringe ShadowStrength | Nebelbänke = Volumen-/Fog-Zone, kein Licht-Event |
| Verlassene Stadt | neutral-warm, mittel | Betongrau-Ambient, Staub im Licht (Cookie) | keine Wetter-Variante |
| Industriegebiet | neutral-kühl | rostiges Ground-Ambient | „flackernde Beleuchtung" als **Emissive-/Cookie-Effekt**, keine Realtime-Lichter |
| Alien-Welt | diffus, kühles Violett-Cyan | phosphoreszierendes Ambient | stärkster Emissive-Anteil (Kristalle); Umgebungs-Violett entsättigter als Fraktions-Violett (Biomes.md-Lesbarkeitsregel) |
| Mond | hart, ungefiltert weiß, steil | fast kein Ambient (tiefschwarzer Himmel) | höchste ShadowStrength (0,85); **Strahlungsfront als Licht-Event**: harter Sweep über die Karte (Schatten zeigen Schutzräume – Gameplay-Lesbarkeit) |
| Mars | warm-apricot, mittel | rosiges Ambient | Staubsturm: stärkste Dämpfung aller Biome, orangefarbene Vollabdeckung |

**Wetter-/Hazard-Integration:** Die periodischen Phasen (Biomes.md: 15–20 s Vorwarnung, 20–90 s Wirkung) sind je Biom eigene `LightingProfile`-Varianten; `ILightingDirector.TransitionTo` fährt den Crossfade synchron zur Vorwarnung. Licht folgt damit demselben angekündigten, symmetrischen Rhythmus wie die Gameplay-Effekte.

## Schatten-Budget

| Parameter | Festlegung | Begründung |
|---|---|---|
| Shadow-Cascades | **2** (Stufe Hoch: 3) | RTS-Sichtweite: 4 Cascades verschwenden Auflösung auf nicht lesbare Distanzen |
| Shadow-Distance | ~120 m (profilabhängig 80–160 m) | deckt den typischen Sichtausschnitt bei Standard-Zoom ab; darüber hinaus Schatten-Fade |
| Auflösung | 2048 (Hoch: 4096) | harte RTS-Kanten, stilisiert verzeiht weichere Schatten nicht |
| Schattenwerfer | Einheiten, Gebäude, Vegetation LOD 0/1 | LOD 2 und Trümmer-Overlays werfen keine Schatten |
| ShadowStrength-Deckel | ≤ 0,85 (außer Mond 0,85) | Lesbarkeit von Team-Farben im Schatten (D-019) |
| Soft Shadows | an (Low-Qualität-Kernel) | stilisierter Look, Kosten moderat |

Punktlicht-Schatten: **grundsätzlich aus** (kein Bedarf, teuer); VFX-Licht über Shadow-lose Punktlichter (s. unten).

## Aetherium-Glow (Emissive + Bloom)

- **Emissive-first:** Kristalle, Mutterkristall und Evolvierte-Organik tragen Emissive-Maps im `NovaUnit`-/Kristall-Shader; Intensität ist ein **Instanz-Parameter** gekoppelt an den Feldzustand aus der Sim (Wachstum, Überernte-Stufe, Erschöpfung – Resources.md): lebendig = pulsierend (niedrigfrequente Sinus-Modulation im Shader, View-seitig, nicht-determinismusrelevant), übererntet = flackernd-gedämpft, erschöpft = dunkel/grau.
- **Bloom macht das Licht:** Es gibt **keine echten Lichtquellen** pro Kristall; der Glow entsteht ausschließlich über HDR-Emissive + Bloom-Threshold (s. Post-Stack). Bei hunderten Kristallinstanzen sind Punktlichter budgetmäßig ausgeschlossen.
- **Boden-Aura:** großräumiges Feld-Leuchten als flaches Emissive-Overlay auf dem Terrain-Layer (gleiche Grid-Textur-Infrastruktur wie FoW/Verseuchung, [./Rendering.md](./Rendering.md)) – kein Decal-Projector-Einsatz.
- **Alien-Welt-Sonderstellung:** höchste Emissive-Dichte (USP-Showcase, +50 %-Ausbreitung); Bloom-Threshold dort nicht senken, sondern Emissive-Intensität der Umgebung reduzieren, damit Feld-Glow gegenüber Kristallflora lesbar bleibt.

## VFX-Beleuchtung (Explosionen, Mündungsfeuer, Superwaffen)

- **Gepoolte Shadow-lose Punktlichter:** Ein globaler `VfxLightPool` (Cap: 8 gleichzeitig, Quality-Stufe Niedrig: 4) vergibt kurzlebige Punktlichter für Explosionen, Superwaffen-Einschläge und große Mündungsblitze. Priorisierung nach Nähe zum Kamera-Zentrum und Effektgröße; bei Cap-Überschreitung entfällt das Licht (VFX bleibt – Emissive/Partikel tragen allein).
- **Lebensdauer:** 0,1–0,5 s mit aggressiver Intensitäts-Abklingkurve; keine Schatten, kein Per-Object-Light-Limit-Konflikt (URP Forward, Cap bewusst unter dem Per-Object-Limit gehalten).
- **Mond/Mars (D-028):** Flammen-Brände entfallen, Explosionen behalten Lichtblitz (Physik-unabhängige Action-Lesbarkeit) – kein Widerspruch, da Brände ≠ Explosionslicht.
- **Kostenrahmen:** 8 Shadow-lose Punktlichter sind in URP Forward vernachlässigbar; kein Forward+ nötig (s. [./Rendering.md](./Rendering.md)).

## URP-Post-Processing-Stack

Ein globaler `Volume` mit Basis-Profil; `LightingProfile.PostVolume` legt Biom-Overrides per Volume-Weight-Blend (Crossfade synchron zum Licht-Übergang).

| Effekt | Festlegung | Begründung |
|---|---|---|
| **Bloom** | an, Threshold ~1.0 (HDR), Intensität moderat | Träger des Aetherium-Glows und aller Emissive-Effekte; Threshold so, dass **nur** Emissive blüht, nie Terrain/Units |
| **Color Grading** | an, pro Biom (Tonemapping: Neutral; Lift/Gamma/Gain + leichte Sättigungskurve) | Biom-Identität aus Biomes.md als Grade, nicht als Textur-Umfärbung – Assets bleiben biomen-neutral wiederverwendbar |
| **Vignette** | an, **dezent** (≤ 0,15) | Fokus auf Bildmitte; nie so stark, dass Randinformation (Mini-Map-Nähe, Flanken) verloren geht – D-019 |
| Depth of Field | **aus** | RTS-Übersicht; Fokus-Unschärfe zerstört Lesbarkeit |
| Motion Blur | **aus** | Kamera-Springe/Panning sind RTS-Standard; Blur verschmiert Befehls-Feedback |
| Film Grain / Chromatic Aberration | aus bzw. nur Mond/Alien-Welt als Stilmittel (≤ minimal) | Lesbarkeits-Deckel |
| Fog (URP Fog) | an, pro Biom (Distanz-Nebel), Sumpf/Mars verstärkt | Tiefe und Wetter-Träger; FoW ist davon **getrennt** (eigener Pass, [./Rendering.md](./Rendering.md)) |

**Reihenfolge:** FoW Full Screen Pass läuft **vor** Post-Processing (Injektor-Punkt in Rendering.md), damit FoW-Verdunkelung von Bloom/Grading konsistent mitbehandelt wird (Ghost-Gebäude blühen nicht).

## Offene Punkte

- **Realtime-only-Verzicht auf jegliches Baking:** Empfehlung dieses Dokuments, aber formale DecisionLog-Entscheidung steht aus (Lighting war keiner der D-033–D-036-Blöcke). Risiko: Kampagne (Phase 3, D-020) mit Nah-Kamera-Momenten könnte APV/Baked-GI-Bedarf erzeugen – Status: offen, Entscheidungsvorlage für Sprint-3-Review.
- **Adaptive/Performance-gesteuerte VfxLight-Caps:** Konkrete Kosten der Punktlichter auf schwachen Metal-GPUs unvermessen – Cap 8 ist Einschätzung, keine Messung. Status: offen, Phase-0/Vertical-Slice-Profiling.
- **Mond-Strahlungsfront als Licht-Sweep:** Post-MVP; keine Sprint-7- oder
  MS-1-Aufgabe.
- **Adaptive Tageszeit:** Kein Tag/Nacht-Zyklus im GDD definiert; `LightingProfile`-Modell ist dafür ausgelegt, aber ob Tageszeit überhaupt ein Feature ist (Hazard-Events nutzen das System bereits), ist eine Design-Frage – Status: offen, nicht selbst entschieden.
- **APV (Adaptive Probe Volumes) ab Alpha:** Re-Eval für Kampagnen-Qualität; Unity-6.3-Reife auf Metal zu prüfen. Status: offen, vorgemerkt.

## Nächste Schritte

1. Sprint-3-Review: Realtime-only-Empfehlung als DecisionLog-Entwurf einreichen (mit geprüften Alternativen: Misch-Baking, APV-sofort).
2. Nach G0 für V2 ausschließlich ein klares Glutrinne-Wüstenprofil anlegen;
   keine Wettervariante.
3. In V2 Schatten-Budget und VfxLightPool-Cap gegen das
   `SCALE_500_RENDERING`-Fixture vermessen.
4. In G4 Bloom-Threshold mit dem Aetherium-Emissive-Parameter abstimmen.
5. Wetter-, Mond-, Mars- und APV-Arbeit bis zu einer Post-MVP-D-ID sperren.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Graphics Engineer |
| 0.2.0 | 2026-07-24 | Glutrinne als einzigen MS-1-Lichtumfang und exakten Unity-Pin gemäß D-056/D-060/D-061 festgelegt | Lead Graphics Engineer |
| 0.2.1 | 2026-07-24 | Nächste Schritte auf klares Glutrinne/V2 begrenzt und Wetter-/Mond-Arbeit aus Sprint 7 entfernt | Lead Graphics Engineer |
