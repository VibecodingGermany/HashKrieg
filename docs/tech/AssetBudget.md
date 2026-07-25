# Asset-Budget

**Version:** 0.2.1 | **Status:** Entwurf – MS-1-Override verbindlich | **Verantwortungsbereich:** Lead Performance Engineer | **Sprint:** 3

## Zweck

Dieses Dokument definiert die verbindlichen Asset-Budgets pro Asset-Klasse (Polycount, Texturen, LOD, VFX-Partikel, Audio-Kompression) für Project Nova. Es operationalisiert die Kauf-Prüfung aus TPD §7.3 in messbare Richtwerte, definiert das Gesamtbudget-Tracking für den schlimmsten sichtbaren Fall (500 Einheiten, L-Karte) und stellt sicher, dass Asset-Produktion und Asset-Store-Käufe innerhalb der Frame- und Speicherbudgets ([PerformanceBudget.md](PerformanceBudget.md), [MemoryBudget.md](MemoryBudget.md)) bleiben. Verbindlich für Lead Performance Engineer, Technical Art und Lead Technical Director; Eingabe für alle Asset-Reviews ab Phase 0.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-056 (MS-1-Umfang), D-058 (Lastkorridore), D-060 (Unity-Pin), D-061 (Abnahme)
- RTS_Technisches_Planungsdokument.md §7.3 (Kauf-Prüf-Liste), §15 (Qualitätsziele)
- [../research/Animation_Audio_UI.md](../research/Animation_Audio_UI.md) – 3-stufiges Animations-/Mesh-LOD, Audio-Abstraktion (Unity Audio MVP, FMOD ab Alpha)
- [../research/AssetStore_Landschaft.md](../research/AssetStore_Landschaft.md) – Kauf-Asset-Landschaft
- [./PerformanceBudget.md](PerformanceBudget.md) – GPU ≤8 ms, Rendering-CPU ≤4 ms
- [./MemoryBudget.md](MemoryBudget.md) – Asset-RAM-Deckel 1,8 GB

## MS-1-Override (D-056/D-058/D-060/D-061)

Für MS-1 gilt ausschließlich der Produktionsumfang aus
[MVPContentManifest.md](../production/MVPContentManifest.md) beziehungsweise
[`quality/content/mvp-v1.json`](../../quality/content/mvp-v1.json): zwei Fraktionen,
eine Karte, 100 Einheiten im produktiven Abnahmeszenario. Der 500-Objekt-Korridor
ist nur ein synthetischer V2-/V3-Lasttest und darf Dummy- oder Scale-Fixtures
verwenden; er verlangt weder 500 produktive Einheiten noch den früheren
FFA-/Vollroster-Umfang. Asset-Import und Messungen verwenden Unity
`6000.5.4f1`, Revision `d550df8bd089`, URP.

## 1. Polycount-Budgets (Dreiecke, LOD0)

Zielkorridor: schräge Top-Down-Kamera (D-019), typische Sichtweite mittel bis fern – LOD0 gilt für Nahansicht/Zoom-Stufe 1.

| Asset-Klasse | LOD0 | LOD1 | LOD2 | Bemerkung |
|---|---|---|---|---|
| Infanterie | ≤ 4.000 Tris | ≤ 1.500 | ≤ 400 | Humanoid-Rig (Mecanim, research/Animation_Audio_UI.md); LOD2 darf stark vereinfacht sein |
| Fahrzeug leicht/mittel | ≤ 8.000 | ≤ 3.000 | ≤ 800 | rig-lose Code-Animation |
| Fahrzeug schwer / Elite (D-015) | ≤ 15.000 | ≤ 6.000 | ≤ 1.500 | max. 1–3 Elite-Einheiten gleichzeitig |
| Lufteinheit | ≤ 10.000 | ≤ 4.000 | ≤ 1.000 | eigene Steering-Schicht (D-034) |
| Gebäude Standard | ≤ 20.000 | ≤ 8.000 | ≤ 2.000 | inkl. Bau-Zustände (Skelett/Trümmer-Stufen als separate Meshes im selben Budget) |
| Gebäude Superwaffe (D-023) | ≤ 35.000 | ≤ 14.000 | ≤ 3.500 | hartes Limit 1 pro Spieler |
| Mauer-/Verteidigungsmodul (D-008) | ≤ 1.500/Segment | ≤ 600 | ≤ 200 | viele Instanzen ⇒ strenges Budget |
| Aetherium-Kristall | ≤ 1.000 | ≤ 400 | ≤ 150 | Felder mit vielen Instanzen |
| Vegetation/Zerstörbares (D-012) | ≤ 800/Instanz | ≤ 300 | ≤ 100 | Brand-Zustände per Material-Swap, nicht per Mesh |

**Worst-Case-Tracking (Sichtfeld, nicht Szenario-Gesamt):** Ziel ≤ 1,5 M sichtbare Dreiecke im Referenz-Frame (L-Karte, Massenschlacht, mittlerer Zoom). LOD-System stellt sicher, dass bei 500 Einheiten der weit überwiegende Teil auf LOD1/LOD2 gerendert wird (Beispielrechnung: 300 Einheiten LOD2 à ~600 Tris + 150 LOD1 à ~2.500 + 50 LOD0 à ~6.000 ≈ 0,86 M Tris + Gebäude/Gelände ≈ 0,4 M ≈ 1,26 M ✓).

## 2. Textur-Budgets

| Asset-Klasse | Albedo/Normal/Mask | Format | Bemerkung |
|---|---|---|---|
| Einheit (pro Typ) | 1× 1024² Atlas (alle Maps) | BC7/BC3 (Desktop) | Teamfarben über Mask-Channel + Shader, keine separaten Team-Texturen |
| Gebäude (pro Typ) | 1× 2048² Atlas | BC7/BC3 | |
| Elite/Superwaffe | bis 2× 2048² | BC7/BC3 | Ausnahmebudget |
| Terrain (pro Karte/Biom) | 2–4 Layer à 2048² + Splat-Map 1024² | BC7 | Biome als Themen (D-017/D-028) |
| VFX/Partikel | gemeinsame Atlanten, ≤ 4 Atlanten à 1024² global | BC7/BC3 | Flipbook-Frames im Atlas |
| UI | Atlanten, ≤ 2048² gesamt | BC7 | UI Toolkit |
| FoW-Textur | 128² RG8 | unkomprimiert | verbindlicher MS-1-Vertrag aus tech/FogOfWar.md |

Regeln: (1) **Atlanten-Pflicht** pro Einheitentyp – kein Typ darf mehr als 1 Material/1 Textur-Set benötigen (Draw-Call-Disziplin, SRP-Batcher-Kompatibilität). (2) Mipmaps Pflicht (Speicherfaktor 1,33 bereits in den Memory-Budgets eingerechnet). (3) Textur-RAM-Deckel gesamt: ≤ 1,0 GB geladen (Teil des 1,8-GB-Asset-Deckels, MemoryBudget.md §1).

## 3. LOD- und Culling-Richtlinien

- **3 LOD-Stufen** pro renderbarer Einheit/Gebäude (research/Animation_Audio_UI.md), LOD2-Pflicht auch für „kleine" Assets – kein LOD0-only-Asset.
- LOD-Übergänge: Bildschirmraum-basiert (`LODGroup` mit relativen Höhen), Richtwerte: LOD0 > 8 % Bildschirmhöhe, LOD1 2–8 %, LOD2 < 2 %; Minimap-Zoom ⇒ zusätzlich Renderer-Culling bzw. Icon-Ersatz.
- **Animations-LOD gekoppelt** (research/Animation_Audio_UI.md §Animations-LOD): nah 30–60 Hz, mittel 15–30 Hz, fern pausiert/Einzelpose; `Animator.cullingMode = Cull Completely` abseits Bildschirm.
- Schatten: nur LOD0/LOD1 werfen Schatten; LOD2 shadow-casting aus.

## 4. VFX-/Partikel-Budgets

| Effekt-Typ | Partikel/Effekt | Gleichzeitig aktiv (max) | Bemerkung |
|---|---|---|---|
| Mündungsfeuer/Tracer | ≤ 15 | 200 | sehr kurzlebig |
| Treffer/Einschlag | ≤ 30 | 150 | |
| Explosion mittel (Einheitentod) | ≤ 80 | 40 | Massenschlacht-Deckel |
| Gebäude-Zerstörung (D-012) | ≤ 150 | 10 | plus Trümmer-Mesh-Swap |
| Vegetations-Brand | ≤ 50/Feld | 30 | tickt über Zeit, kein Burst |
| Superwaffen-Einschlag (D-023) | ≤ 500 | 2 | einzige Ausnahme; eigenes GPU-Budget-Fenster (PerformanceBudget.md Reserve) |
| Wetter/Hazard (D-028) | ≤ 1.000 global | 1 | kartenweit, screen-space-nah |

Deckel: **≤ 10.000 aktive Partikel gesamt** pro Frame (P95), Partikel-LOD halbiert Emissionsraten bei 30-FPS-Modus und weitem Zoom. CPU-Partikel nur für gameplay-relevante Kollision; alles andere GPU-Simulation (Visual Effect Graph optional ab Alpha, MVP: Shuriken-Disziplin).

## 5. Audio-Budgets

| Klasse | Format (MVP, Unity Audio) | Format (ab Alpha, FMOD) | Richtwert |
|---|---|---|---|
| Musik | Vorbis q5, gestreamt | FMOD-Bank, gestreamt | ≤ 2 parallele Streams |
| SFX kurz (< 2 s) | ADPCM, dekomprimiert im Speicher | Vorbis in Bank | Stimmen-Limit ≤ 32, Priorisierung nach Nähe |
| SFX lang/Ambience (> 2 s) | Vorbis q3–4, komprimiert im Speicher | Vorbis | ≤ 8 parallele Ambience-Stimmen |
| Sprache/UI | Vorbis q4 | Vorbis | |

RAM-Deckel Audio: ≤ 300 MB geladen (Teil des Asset-Deckels). Die AudioService-Abstraktion (research/Animation_Audio_UI.md) muss beide Backends ohne Gameplay-Änderung bedienen; Budgetzahlen bleiben beim FMOD-Umstieg identisch.

## 6. Kauf-Prüfung (TPD §7.3 operationalisiert)

Jedes Asset-Store-Paket wird gegen diese Richtwerte geprüft; **drei oder mehr „Nein" = Kaufverzicht oder Aufbereitungsaufwand explizit einkalkulieren:**

| §7.3-Punkt | Messbarer Richtwert |
|---|---|
| Unity-Kompatibilität | Unity `6000.5.4f1`, Revision `d550df8bd089`, URP, im Testprojekt nachgewiesen |
| Renderpipeline | URP-nativ oder mit ≤ 1 PT-Tag konvertierbar (Built-in-Shader = Warnung) |
| Lizenz | kommerzielle Nutzung, keine Umsatzbeteiligung, keine Quellangaben-Pflicht im Spiel |
| Polygonzahl | innerhalb §1-Budgets der Klasse (LOD0), sonst Retopologie-Aufwand schätzen |
| Texturauflösung | ≤ 2048² pro Asset, BC-komprimierbar, Mipmaps generierbar |
| LOD-Stufen | ≥ 2 LODs vorhanden, sonst Erstellungsaufwand pro Asset ~0,5–1 PT einkalkulieren |
| Animationen | Humanoid-kompatibel (Infanterie) bzw. nicht erforderlich (Fahrzeuge, Code-Animation) |
| Rigging | Humanoid-Standard-Skelett; exotische Rigs nur bei Einzel-Showcase-Assets |
| Materialaufbau | ≤ 2 Materialien pro Mesh-Renderer, SRP-Batcher-kompatibel |
| Mobile/WebGL-Eignung | für Nova **nicht relevant** (Desktop primär, D-006) – kein K.O.-Kriterium |
| Visuelle Kompatibilität | Stil-Check gegen Signature-Assets (TPD §7.2) durch Art Direction |
| Anpassbarkeit | FBX/Quelle enthalten, Materialien editierbar |
| Dateiformate | FBX + PNG/TGA/PSD; proprietäre Formate nur mit Export-Pfad |
| Support/Aktualität | Update ≤ 18 Monate alt oder Publisher-Historie stabil |

## 7. Gesamtbudget-Tracking

| Bereich | Deckel | Quelle |
|---|---|---|
| Sichtbare Dreiecke (Referenz-Frame) | ≤ 1,5 M | §1 |
| Textur-RAM geladen | ≤ 1,0 GB | §2 |
| Mesh-RAM geladen (3 Fraktionen + Karte + Neutrales, ~90 Einheitentypen total, davon ≤ 40 pro Match) | ≤ 500 MB | §1 + MemoryBudget |
| Audio-RAM geladen | ≤ 300 MB | §5 |
| Animation-Clips/Rigs | ≤ 100 MB | Mecanim-Infanterie + Code-Animation-Daten |
| Aktive Partikel | ≤ 10.000 | §4 |
| **Asset-RAM gesamt** | **≤ 1,8 GB** | MemoryBudget.md §1 |

Tracking-Mechanik: Import-Pipeline validiert Polycount/Texturgrößen/LOD-Anzahl automatisch (Editor-Validator im CI); Verletzungen blockieren den Merge. Laufzeit-Tracking über Budget-Monitor (PerformanceBudget.md §4) im Development-Build.

## 8. Schnittstellen-Skizze (API-Design, keine Implementierung)

```csharp
namespace Nova.Assets
{
    public enum AssetClass { Infantry, VehicleLight, VehicleHeavy, Aircraft,
                             Building, Superweapon, WallModule, Crystal, Vegetation }

    // Budget-Definition pro Klasse, datengetrieben in der GameDatabase.
    public sealed class AssetBudgetSO : ScriptableObject
    {
        public AssetClass Class;
        public int[] MaxTrianglesPerLod;   // Länge 3: LOD0..LOD2
        public int MaxTextureSize;         // 1024 / 2048
        public int MaxMaterialsPerRenderer;
    }

    // CI-/Editor-Validierung eines importierten Assets gegen sein Budget.
    public interface IAssetBudgetValidator
    {
        AssetBudgetReport Validate(UnityEngine.Object asset, AssetBudgetSO budget);
    }

    public struct AssetBudgetReport
    {
        public bool Passed;
        public string[] Violations;        // z. B. "LOD0 5200 > 4000 Tris"
    }
}
```

## Offene Punkte

- **Terrain-Polycount/Verfahren** (Unity Terrain vs. Custom Mesh, Biome-Shader) ist noch keinem TDD zugeordnet; das Dreieck-Budget für Terrain (vorläufig ≤ 300 k sichtbar) muss mit dem Rendersequenzen-TDD (Sprint 6, OpenQuestions) bestätigt werden.
- **Trümmer-Persistenz:** Wie lange bleiben Gebäude-Trümmer (D-012) als gerenderte Meshes stehen? Unbegrenzte Persistenz würde das Worst-Case-Dreieck-Budget sprengen – Design-Entscheidung (Despawn-Timer vs. Deckel) mit Game Design klären.
- **Visual Effect Graph vs. Shuriken** ab Alpha: VFX-Graph verändert die Partikel-Kostenstruktur (GPU statt CPU); Entscheidung mit dem Rendersequenzen-TDD fällen.
- **Post-MVP-Vollroster:** Die früheren Annahmen zu drei Fraktionen, Neutralen und
  FFA sind kein MS-1-Abnahmekriterium; eine spätere Last- und Speicherfreigabe
  benötigt eine eigene Scope-Entscheidung.
- **Textur-Kompressionsformat für macOS** (BC vs. ASTC auf Apple Silicon): BC7 wird von Apple-GPUs nativ unterstützt, aber die tatsächlichen VRAM-Werte sind im Phase-0-Spike zu vermessen.

## Nächste Schritte

1. Sprint 7: Editor-Import-Validator (`IAssetBudgetValidator`) als Teil der Asset-Pipeline implementieren; `AssetBudgetSO`-Einträge für alle Klassen anlegen.
2. Phase 0: Signature-Asset (TPD §7.2) gegen §1/§2-Budgets bauen und als Referenz-Frame vermessen → Budgets ggf. als v0.2.0 nachjustieren.
3. Phase 2 (Alpha): VFX-Graph-Entscheidung, FMOD-Umstieg mit unveränderten Audio-Deckeln, Trümmer-Persistenz-Regel aus der Design-Klärung hier einarbeiten.
4. Laufend: Jede Kauf-Prüfung nach §6 dokumentieren (Checkliste im PR-Template).

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Performance Engineer |
| 0.2.0 | 2026-07-24 | MS-1-Content, 100-/500-Korridore und exakten Unity-Pin gemäß D-056/D-058/D-060/D-061 abgegrenzt | Lead Performance Engineer |
| 0.2.1 | 2026-07-24 | FoW-Texturbudget auf verbindliches 128²-RG8-MS-1-Grid korrigiert | Lead Performance Engineer |
