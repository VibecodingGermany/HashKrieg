using UnityEngine;
using Nova.Gameplay.Match;

namespace Nova.Presentation.Maps
{
    /// <summary>
    /// GLUTRINNE BLOCKOUT — the presentation layer of the first map. It builds
    /// itself at Play from <see cref="MatchBootstrap"/>'s canonical layout, so
    /// what is rendered is exactly what the simulation registered: the
    /// procedural desert ground of the Glutrinne biome, scattered rock
    /// debris, a weathered edge band instead of a hard frame, and an
    /// aetherium crystal cluster on each of the five fields the canonical
    /// match registers.
    /// <para>
    /// Pure presentation: this component reads the bootstrap's layout
    /// properties and spawns primitive-only markers; it never writes into
    /// simulation state. The five-field manifest layout is visible since
    /// Sprint 16.7; primary-route dressing remains later map-art scope.
    /// </para>
    /// <para>
    /// KARTENBILD (D-085): everything here is generated at runtime with a
    /// fixed seed — the ground texture, the rock scatter and the edge veil
    /// (<see cref="GlutrinneTerrainTexture"/>). Assets/_Project/Art/**/*.png
    /// is gitignored, so a downloaded texture would vanish in every fresh
    /// clone; the procedural desert is the permanent baseline a CC0 drop-in
    /// may later decorate, never a prerequisite.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)] // builds after MatchBootstrap.Start (default order 0)
    public sealed class GlutrinneBlockoutView : MonoBehaviour
    {
        /// <summary>Fixed seed of the rock scatter — same desert in every clone and every run.</summary>
        private const uint ScatterSeed = 0xA53A9D1Bu;

        /// <summary>Rock count target of the scatter (60–100 per the sprint brief; attempts may reject near bases/fields).</summary>
        private const int ScatterRockTarget = 84;

        /// <summary>Exclusion radius around each start base (cells) — the opening area stays clear.</summary>
        private const float BaseExclusionRadius = 8f;

        /// <summary>Exclusion radius around each aetherium field (cells) — harvester approach cells stay clear.</summary>
        private const float FieldExclusionRadius = 3.5f;

        /// <summary>Width of the weathered edge band in cells, as a fraction of the 128-cell map for the veil texture.</summary>
        private const float EdgeFadeCells = 3f;

        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchBootstrap _bootstrap;
        [SerializeField] private Renderer _groundRenderer;

        [Header("Glutrinne palette (desert biome)")]
        [Tooltip("Sand tone the procedural ground texture is derived from — the Glutrinne desert read at a glance.")]
        [SerializeField] private Color _sandColor = new Color(0.72f, 0.60f, 0.42f, 1f);
        [Tooltip("Aetherium crystal cyan, matching the resource's established UI colour.")]
        [SerializeField] private Color _crystalColor = new Color(0.20f, 0.85f, 0.90f, 1f);
        [Tooltip("Dark rock of the weathered map edge band.")]
        [SerializeField] private Color _edgeColor = new Color(0.28f, 0.24f, 0.20f, 1f);
        [Tooltip("Scattered rocks and pebbles — between the edge rock and the sand so the debris reads as belonging.")]
        [SerializeField] private Color _rockColor = new Color(0.36f, 0.31f, 0.26f, 1f);

        [Header("Blockout shape")]
        [Tooltip("World units of sand tile repetition: the 512px tile covers four cells, so the grain stays crisp at RTS zoom.")]
        [SerializeField] private Vector2 _groundTextureTiling = new Vector2(32f, 32f);

        // Fixed crystal cluster: seven shards around the field cell centre.
        // Deterministic literal layout — no Random, so editor and player show
        // the identical field marker.
        private static readonly Vector2[] ClusterOffsets =
        {
            new Vector2(0.00f, 0.00f), new Vector2(0.45f, 0.20f),
            new Vector2(-0.40f, 0.35f), new Vector2(0.20f, -0.45f),
            new Vector2(-0.35f, -0.30f), new Vector2(0.55f, -0.25f),
            new Vector2(-0.15f, 0.55f),
        };

        private static readonly float[] ClusterHeights =
        {
            1.10f, 0.65f, 0.80f, 0.55f, 0.70f, 0.50f, 0.60f,
        };

        private void Start()
        {
            if (_bootstrap == null) _bootstrap = FindAnyObjectByType<MatchBootstrap>();
            if (_bootstrap == null)
            {
                Debug.LogError("[GlutrinneBlockoutView] No MatchBootstrap found — blockout disabled.");
                enabled = false;
                return;
            }

            Vector2Int[] fieldCells = _bootstrap.AllFieldCells;
            TintGround();
            BuildScatterRocks(fieldCells);
            BuildWeatheredEdge(_bootstrap.MapSize);
            for (int i = 0; i < fieldCells.Length; i++)
            {
                BuildFieldMarker(fieldCells[i], $"Field_{i + 1}");
            }
        }

        /// <summary>
        /// Runtime URP Lit material for blockout pieces. Unity's primitive
        /// default material is a built-in-RP resource and renders magenta
        /// under URP (GB-004 finding), so the blockout carries its own —
        /// created at Play, never saved, no asset file. Falls back to the
        /// built-in Standard shader in non-URP contexts.
        /// </summary>
        private static Material CreateRuntimeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            material.color = color;
            return material;
        }

        /// <summary>
        /// The desert ground: the procedural sand tile on a runtime URP Lit
        /// material. URP/Lit takes material.mainTexture onto _BaseMap through
        /// its [MainTexture] attribute — the same channel material.color
        /// already used — and the tiling repeats it 32x32 across the map
        /// (four cells per tile). The material colour stays white: the sand
        /// palette lives IN the texture now, tinting it twice would darken it.
        /// </summary>
        private void TintGround()
        {
            if (_groundRenderer == null)
            {
                Debug.LogWarning("[GlutrinneBlockoutView] No ground renderer wired — ground keeps its default grey.");
                return;
            }

            Material material = CreateRuntimeMaterial(Color.white);
            material.mainTexture = GlutrinneTerrainTexture.CreateSandTile(_sandColor, 512);
            material.mainTextureScale = _groundTextureTiling;
            _groundRenderer.sharedMaterial = material;
        }

        /// <summary>
        /// Rock debris: squashed-sphere boulders and pebbles, placed by a
        /// fixed-seed xorshift (no UnityEngine.Random), rejected inside the
        /// exclusion zones around both start bases and all five aetherium
        /// fields, and NEVER carrying a collider — the debris is pure
        /// visual, the sim's grid pathing does not see it (and must not).
        /// </summary>
        private void BuildScatterRocks(Vector2Int[] fieldCells)
        {
            Vector2Int mapSize = _bootstrap.MapSize;
            var scatter = new GameObject("ScatterRocks");
            scatter.transform.SetParent(transform, false);
            Material rockMaterial = CreateRuntimeMaterial(_rockColor);

            uint rng = ScatterSeed;
            int placed = 0;
            for (int attempts = 0; attempts < ScatterRockTarget * 5 && placed < ScatterRockTarget; attempts++)
            {
                float x = 2f + Next01(ref rng) * (mapSize.x - 4f);
                float z = 2f + Next01(ref rng) * (mapSize.y - 4f);
                if (IsExcluded(x, z, fieldCells)) continue;

                float sx = 0.25f + Next01(ref rng) * 0.70f;
                float sy = 0.15f + Next01(ref rng) * 0.40f;
                float sz = 0.25f + Next01(ref rng) * 0.70f;
                float rotationY = Next01(ref rng) * 360f;

                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = $"Rock_{placed}";
                rock.transform.SetParent(scatter.transform, false);
                // Slightly embedded (0.4 instead of 0.5): pebbles sit IN the sand, not on it.
                rock.transform.position = new Vector3(x, sy * 0.4f, z);
                rock.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
                rock.transform.localScale = new Vector3(sx, sy, sz);

                // Pure visual: no collider, so nothing can ever pick or block a rock.
                Destroy(rock.GetComponent<Collider>());
                rock.GetComponent<Renderer>().sharedMaterial = rockMaterial;
                placed++;
            }
        }

        /// <summary>Inside a start-base or aetherium-field exclusion zone the scatter stays out (the D-085 brief).</summary>
        private bool IsExcluded(float x, float z, Vector2Int[] fieldCells)
        {
            Vector2Int localHq = _bootstrap.LocalHqCenterCell;
            Vector2Int enemyHq = _bootstrap.EnemyHqCenterCell;
            if (WithinRadius(x, z, localHq.x + 0.5f, localHq.y + 0.5f, BaseExclusionRadius)
                || WithinRadius(x, z, enemyHq.x + 0.5f, enemyHq.y + 0.5f, BaseExclusionRadius))
            {
                return true;
            }

            for (int i = 0; i < fieldCells.Length; i++)
            {
                Vector2Int field = fieldCells[i];
                if (WithinRadius(x, z, field.x + 0.5f, field.y + 0.5f, FieldExclusionRadius))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool WithinRadius(float x, float z, float cx, float cz, float radius)
        {
            float dx = x - cx;
            float dz = z - cz;
            return dx * dx + dz * dz < radius * radius;
        }

        /// <summary>
        /// The map edge as a weathered band (D-085) INSTEAD of the old flat
        /// dark frame beams: one unlit quad over the whole map whose veil
        /// texture darkens the outer two to three cells toward the rock
        /// tone. The map reads embedded in a weathered border rather than
        /// cut off by a bar. (The gradient cannot live in the repeating
        /// ground tile — a map-edge feature in a 32x32-repeated texture
        /// would repeat every four cells — so it rides this map-scale
        /// overlay; that is what the brief's "Farbverlauf" means rendered.)
        /// </summary>
        private void BuildWeatheredEdge(Vector2Int size)
        {
            Texture2D veil = GlutrinneTerrainTexture.CreateWeatheringVeil(
                _edgeColor, 256, EdgeFadeCells / size.x);

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            var material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
                mainTexture = veil
            };

            GameObject overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            overlay.name = "EdgeWeathering";
            overlay.transform.SetParent(transform, false);
            Destroy(overlay.GetComponent<Collider>());
            // Quad lies in the XY plane; pitching 90 degrees lays it flat,
            // hovering just over the sand to win no depth fight with it.
            overlay.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            overlay.transform.position = new Vector3(size.x * 0.5f, 0.02f, size.y * 0.5f);
            overlay.transform.localScale = new Vector3(size.x, size.y, 1f);
            overlay.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        /// <summary>Fixed-seed xorshift32 in [0, 1) — the deterministic scatter stream (editor and player identical).</summary>
        private static float Next01(ref uint state)
        {
            unchecked
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return (state & 0xFFFFFF) / 16777216f;
            }
        }

        private void BuildFieldMarker(Vector2Int cell, string label)
        {
            var marker = new GameObject($"AetheriumField_{label}_{cell.x}_{cell.y}");
            marker.transform.SetParent(transform, false);
            marker.transform.position = new Vector3(cell.x + 0.5f, 0f, cell.y + 0.5f);

            Material crystalMaterial = CreateRuntimeMaterial(_crystalColor);
            for (int i = 0; i < ClusterOffsets.Length; i++)
            {
                float height = ClusterHeights[i];
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"Crystal_{i}";
                shard.transform.SetParent(marker.transform, false);
                shard.transform.localPosition = new Vector3(ClusterOffsets[i].x, height * 0.5f, ClusterOffsets[i].y);
                shard.transform.localRotation = Quaternion.Euler(0f, 25f * i, 0f);
                shard.transform.localScale = new Vector3(0.35f, height, 0.35f);

                // Pure marker: no collider, so nothing can ever pick or block a crystal.
                Destroy(shard.GetComponent<Collider>());
                shard.GetComponent<Renderer>().sharedMaterial = crystalMaterial;
            }
        }
    }
}
