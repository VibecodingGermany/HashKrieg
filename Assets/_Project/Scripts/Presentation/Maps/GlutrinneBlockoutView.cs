using UnityEngine;
using Nova.Gameplay.Match;

namespace Nova.Presentation.Maps
{
    /// <summary>
    /// GLUTRINNE BLOCKOUT — the presentation layer of the first map. It builds
    /// itself at Play from <see cref="MatchBootstrap"/>'s canonical layout, so
    /// what is rendered is exactly what the simulation registered: the desert
    /// ground tint of the Glutrinne biome, an aetherium crystal cluster on
    /// each of the two fields the canonical match actually registers, and a
    /// low frame marking the 128x128 map edge.
    /// <para>
    /// Pure presentation: this component reads the bootstrap's layout
    /// properties and spawns primitive-only markers; it never writes into
    /// simulation state. The full five-field manifest layout with the two
    /// primary attack routes is G4 scope and deliberately NOT shown — the
    /// blockout must not promise fields the match does not have
    /// (docs/production/ScopeLedger.md, rows map.aetheriumFields /
    /// map.primaryRouteCount).
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)] // builds after MatchBootstrap.Start (default order 0)
    public sealed class GlutrinneBlockoutView : MonoBehaviour
    {
        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchBootstrap _bootstrap;
        [SerializeField] private Renderer _groundRenderer;

        [Header("Glutrinne palette (desert biome)")]
        [Tooltip("Sand tone of the ground plane — the Glutrinne desert read at a glance.")]
        [SerializeField] private Color _sandColor = new Color(0.72f, 0.60f, 0.42f, 1f);
        [Tooltip("Aetherium crystal cyan, matching the resource's established UI colour.")]
        [SerializeField] private Color _crystalColor = new Color(0.20f, 0.85f, 0.90f, 1f);
        [Tooltip("Dark rock of the map edge frame.")]
        [SerializeField] private Color _edgeColor = new Color(0.28f, 0.24f, 0.20f, 1f);

        [Header("Blockout shape")]
        [SerializeField] private float _edgeFrameHeight = 0.8f;
        [SerializeField] private float _edgeFrameThickness = 0.5f;

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

            TintGround();
            Vector2Int size = _bootstrap.MapSize;
            BuildEdgeFrame(size);
            BuildFieldMarker(_bootstrap.LocalFieldCell, "Local");
            BuildFieldMarker(_bootstrap.EnemyFieldCell, "Enemy");
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

        /// <summary>Desert ground via a runtime material — no asset is created or modified.</summary>
        private void TintGround()
        {
            if (_groundRenderer == null)
            {
                Debug.LogWarning("[GlutrinneBlockoutView] No ground renderer wired — ground keeps its default grey.");
                return;
            }
            _groundRenderer.sharedMaterial = CreateRuntimeMaterial(_sandColor);
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

        private void BuildEdgeFrame(Vector2Int size)
        {
            float t = _edgeFrameThickness;
            float h = _edgeFrameHeight;
            float w = size.x;
            float d = size.y;

            Material edgeMaterial = CreateRuntimeMaterial(_edgeColor);
            BuildEdgeBeam("EdgeFrame_South", new Vector3(w * 0.5f, h * 0.5f, -t * 0.5f), new Vector3(w + 2f * t, h, t), edgeMaterial);
            BuildEdgeBeam("EdgeFrame_North", new Vector3(w * 0.5f, h * 0.5f, d + t * 0.5f), new Vector3(w + 2f * t, h, t), edgeMaterial);
            BuildEdgeBeam("EdgeFrame_West", new Vector3(-t * 0.5f, h * 0.5f, d * 0.5f), new Vector3(t, h, d), edgeMaterial);
            BuildEdgeBeam("EdgeFrame_East", new Vector3(w + t * 0.5f, h * 0.5f, d * 0.5f), new Vector3(t, h, d), edgeMaterial);
        }

        private void BuildEdgeBeam(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = name;
            beam.transform.SetParent(transform, false);
            beam.transform.position = position;
            beam.transform.localScale = scale;
            beam.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
