using System.Collections.Generic;
using UnityEngine;

namespace Nova.Gameplay.Match
{
    /// <summary>
    /// Timeline of one unloading, presentation only. The simulation empties a
    /// harvester in a single tick; the view plays this ~1.9 s sequence on that
    /// edge: the bed (or the body of a bed-less robot) tips back with an ease
    /// in/out, the pile slides out while it is tipped, and the bed returns.
    /// Pure value logic, no UnityEngine objects, so EditMode tests pin it.
    /// </summary>
    public struct HarvesterDumpCycle
    {
        public const float TiltUpSeconds = 0.6f;
        public const float HoldSeconds = 0.45f;
        public const float ReturnSeconds = 0.8f;
        public const float TotalSeconds = TiltUpSeconds + HoldSeconds + ReturnSeconds;

        /// <summary>The pile lets go once the bed is about three quarters up.</summary>
        public const float ReleaseStartSeconds = 0.45f;
        public const float ReleaseSeconds = 0.9f;

        private float _t;
        private bool _active;

        public bool Active => _active;
        public float Elapsed => _t;

        /// <summary>(Re)starts the sequence from the beginning.</summary>
        public void Start()
        {
            _t = 0f;
            _active = true;
        }

        public void Reset()
        {
            _t = 0f;
            _active = false;
        }

        public void Advance(float dt)
        {
            if (!_active) return;
            _t += Mathf.Max(0f, dt);
            if (_t >= TotalSeconds)
            {
                _t = 0f;
                _active = false;
            }
        }

        /// <summary>0 = bed down, 1 = fully tipped. Smootherstep up, hold, smoothstep back.</summary>
        public float Tilt01
        {
            get
            {
                if (!_active) return 0f;
                if (_t < TiltUpSeconds) return SmootherStep(_t / TiltUpSeconds);
                if (_t < TiltUpSeconds + HoldSeconds) return 1f;
                return 1f - SmootherStep((_t - TiltUpSeconds - HoldSeconds) / ReturnSeconds);
            }
        }

        /// <summary>
        /// 0 = the load still sits in the bed, 1 = it is gone. Accelerates, like
        /// ore sliding down an incline.
        /// </summary>
        public float Release01
        {
            get
            {
                if (!_active) return 0f;
                float x = Mathf.Clamp01((_t - ReleaseStartSeconds) / ReleaseSeconds);
                return x * x;
            }
        }

        private static float SmootherStep(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * x * (x * (x * 6f - 15f) + 10f);
        }
    }

    /// <summary>
    /// The Aetherium load of a harvester, generated at runtime (no asset): a
    /// low faceted mound with a few crystal shards, glowing in Aetherium cyan
    /// through the same URP bloom the art pass uses. It hangs on the bed node
    /// (or on the <c>Cargo_Anchor</c> of a bed-less model) so it tips with it,
    /// and its size follows the fill level smoothly; empty = not rendered.
    /// <para>
    /// The faction/health tint of <c>UnitViewManager</c> is written into every
    /// renderer of the view as a property block of <c>_BaseColor</c>, which
    /// would override a material colour; the pile therefore carries its colour
    /// in a texture and only the emission is a material colour.
    /// </para>
    /// </summary>
    public sealed class CargoPile
    {
        private const int ShardCount = 16;

        private static Mesh _moundMesh;
        private static Mesh _shardMesh;
        private static Material _moundMaterial;
        private static Material _shardMaterial;
        private static bool _materialsFailed;

        private readonly Transform _wrapper;
        private readonly Transform _mound;
        private readonly Transform _shards;
        private readonly Renderer _moundRenderer;
        private readonly Renderer _shardRenderer;
        private readonly Vector3 _size;           // root-local: width, mound height, depth
        private readonly Vector3 _basePosition;   // anchor-local
        private readonly Vector3 _slideAxis;      // anchor-local, toward the unit's rear, per root-local unit
        private readonly Vector3 _downAxis;       // anchor-local, per root-local unit

        private float _fill;
        private float _release;

        /// <summary>Current display fill, 0..1 (after <see cref="Apply"/>).</summary>
        public float Fill => _fill;

        /// <summary>Current slide-out progress, 0..1.</summary>
        public float Release => _release;

        /// <summary>World-space centre of the pile (for the dust burst).</summary>
        public Vector3 WorldCenter => _wrapper != null ? _wrapper.position : Vector3.zero;

        /// <summary>Mound scale after the last <see cref="Apply"/>, in root-local units.</summary>
        public Vector3 MoundScale => _mound != null ? _mound.localScale : Vector3.zero;

        public bool Visible => _moundRenderer != null && _moundRenderer.enabled;

        private CargoPile(Transform root, Transform anchor, Vector3 baseRootLocal, Vector3 sizeRootLocal)
        {
            _size = sizeRootLocal;

            var wrapperGo = new GameObject("Cargo");
            _wrapper = wrapperGo.transform;
            _wrapper.SetParent(anchor, false);
            // The wrapper's axes follow the unit root, and one of its local
            // units is one ROOT-local unit: everything below is then written
            // in root-local metres whatever the FBX axis baking did to the anchor.
            float anchorUnit = Mathf.Max(1e-5f, anchor.TransformVector(Vector3.up).magnitude);
            float rootUnit = Mathf.Max(1e-5f, root.TransformVector(Vector3.up).magnitude);
            _wrapper.localRotation = Quaternion.Inverse(anchor.rotation) * root.rotation;
            _wrapper.localScale = Vector3.one * (rootUnit / anchorUnit);
            _basePosition = anchor.InverseTransformPoint(root.TransformPoint(baseRootLocal));
            _wrapper.localPosition = _basePosition;
            _slideAxis = anchor.InverseTransformVector(root.TransformVector(Vector3.back));
            _downAxis = anchor.InverseTransformVector(root.TransformVector(Vector3.down));

            _mound = CreatePart("Mound", GetMoundMesh(), GetMoundMaterial(), out _moundRenderer);
            _shards = CreatePart("Shards", GetShardMesh(), GetShardMaterial(), out _shardRenderer);
            Apply(0f, 0f);
        }

        /// <summary>
        /// Builds a pile under <paramref name="anchor"/>. <paramref name="baseRootLocal"/>
        /// is the middle of the pile's footprint at the bed floor and
        /// <paramref name="sizeRootLocal"/> its (width, mound height, depth), both
        /// in the unit root's local space. Null when no shader is available.
        /// </summary>
        public static CargoPile Create(Transform root, Transform anchor, Vector3 baseRootLocal, Vector3 sizeRootLocal)
        {
            if (root == null || anchor == null) return null;
            if (GetMoundMaterial() == null || GetShardMaterial() == null) return null;
            return new CargoPile(root, anchor, baseRootLocal, sizeRootLocal);
        }

        /// <summary>
        /// Sets the displayed fill (0 = empty and invisible, 1 = heaped) and how
        /// far the load has slid out of the bed (0 = in, 1 = gone).
        /// </summary>
        public void Apply(float fill, float release)
        {
            _fill = Mathf.Clamp01(fill);
            _release = Mathf.Clamp01(release);
            bool visible = _fill > 0.004f && _release < 0.995f;
            if (_moundRenderer != null) _moundRenderer.enabled = visible;
            if (_shardRenderer != null) _shardRenderer.enabled = visible;
            if (!visible) return;

            float grow = Mathf.Sqrt(_fill);
            float shrink = 1f - 0.65f * _release;
            float widthK = Mathf.Lerp(0.82f, 1f, grow) * shrink;
            float heightK = (0.08f + 0.92f * _fill) * (1f - 0.8f * _release);
            _mound.localScale = new Vector3(_size.x * widthK, _size.y * heightK, _size.z * widthK);

            // The crystals break through the ore only once there is a heap.
            float shardK = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.7f, _fill));
            // Crystals keep a minimum height so they break through even a shallow load.
            _shards.localScale = new Vector3(_size.x * widthK, _size.y * Mathf.Max(heightK, 0.45f) * shardK, _size.z * widthK);
            _shards.gameObject.SetActive(shardK > 0.01f);

            float r = _release;
            _wrapper.localPosition = _basePosition
                                     + _slideAxis * (_size.z * 1.1f * r)
                                     + _downAxis * (_size.y * 0.35f * r);
        }

        // ------------------------------------------------------------ build

        private Transform CreatePart(string name, Mesh mesh, Material material, out Renderer renderer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_wrapper, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            renderer = mr;
            return go.transform;
        }

        private static Material GetMoundMaterial()
        {
            EnsureMaterials();
            return _moundMaterial;
        }

        private static Material GetShardMaterial()
        {
            EnsureMaterials();
            return _shardMaterial;
        }

        private static void EnsureMaterials()
        {
            if (_moundMaterial != null && _shardMaterial != null) return;
            if (_materialsFailed) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) { _materialsFailed = true; return; }

            // Aetherium Cyan of the HUD (0.20, 0.88, 0.92).
            _moundMaterial = MakeMaterial(shader, "Nova_Cargo_Ore",
                new Color(0.20f, 0.17f, 0.15f), new Color(0.02f, 0.10f, 0.12f) * 0.5f, 0.15f);
            _shardMaterial = MakeMaterial(shader, "Nova_Cargo_Crystal",
                new Color(0.10f, 0.62f, 0.72f), new Color(0.20f, 0.88f, 0.92f) * 0.9f, 0.75f);
        }

        private static Material MakeMaterial(Shader shader, string name, Color albedo, Color emission, float smoothness)
        {
            var m = new Material(shader) { name = name, hideFlags = HideFlags.DontSave };
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(new[] { albedo, albedo, albedo, albedo });
            tex.Apply();
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emission);
            m.EnableKeyword("_EMISSION");
            // URP drops _EMISSION during material validation unless the GI flag
            // allows realtime emission (see AssetPass_2026-10-17 section 6).
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return m;
        }

        // ------------------------------------------------------------- mesh

        /// <summary>
        /// Unit mound: footprint radius 0.5 around the origin, height 1, flat
        /// shaded and slightly lumpy. Deterministic.
        /// </summary>
        internal static Mesh GetMoundMesh()
        {
            if (_moundMesh != null) return _moundMesh;
            const int Segments = 14;
            const int Rings = 4;
            var rng = new System.Random(7731);
            // Grid of vertices on a dome, perturbed; triangles then get their own vertices (flat look).
            var grid = new Vector3[(Rings + 1) * Segments];
            for (int ring = 0; ring <= Rings; ring++)
            {
                float rho = 1f - ring / (float)Rings;            // 1 at the rim, 0 at the top
                for (int s = 0; s < Segments; s++)
                {
                    float a = s / (float)Segments * Mathf.PI * 2f;
                    float lump = 0.88f + 0.24f * (float)rng.NextDouble();
                    // Superellipse (n = 4): a rounded rectangle that fills a dump bed.
                    float c4 = Mathf.Pow(Mathf.Abs(Mathf.Cos(a)), 4f), s4 = Mathf.Pow(Mathf.Abs(Mathf.Sin(a)), 4f);
                    float square = Mathf.Pow(c4 + s4, -0.25f);
                    float rad = 0.5f * square * Mathf.Sqrt(rho) * (ring == 0 ? 1f : lump);
                    float h = Mathf.Pow(Mathf.Max(0f, 1f - rho * rho), 0.45f) * (0.85f + 0.3f * (float)rng.NextDouble());
                    if (ring == Rings) h = 1f;
                    grid[ring * Segments + s] = new Vector3(Mathf.Cos(a) * rad, ring == 0 ? 0f : h, Mathf.Sin(a) * rad);
                }
            }
            var verts = new List<Vector3>();
            var tris = new List<int>();
            void Tri(Vector3 a, Vector3 b, Vector3 c)
            {
                int i = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            }
            for (int ring = 0; ring < Rings; ring++)
            {
                for (int s = 0; s < Segments; s++)
                {
                    int s1 = (s + 1) % Segments;
                    Vector3 a = grid[ring * Segments + s], b = grid[ring * Segments + s1];
                    Vector3 c = grid[(ring + 1) * Segments + s], d = grid[(ring + 1) * Segments + s1];
                    // Outward-facing (viewed from outside, clockwise in Unity): a -> c -> b etc.
                    Tri(a, c, b);
                    if (ring + 1 < Rings) Tri(b, c, d);
                }
            }
            _moundMesh = FinishMesh("Nova_CargoMound", verts, tris);
            return _moundMesh;
        }

        /// <summary>
        /// Unit shard cluster: <see cref="ShardCount"/> hexagonal crystals with
        /// pointed tips, rooted inside the mound (y from 0.3) and leaning outward.
        /// </summary>
        internal static Mesh GetShardMesh()
        {
            if (_shardMesh != null) return _shardMesh;
            var rng = new System.Random(4096);
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int k = 0; k < ShardCount; k++)
            {
                float a = (k / (float)ShardCount) * Mathf.PI * 2f + (float)rng.NextDouble() * 0.6f;
                float dist = k == 0 ? 0.04f : 0.10f + 0.30f * (float)rng.NextDouble();
                var root = new Vector3(Mathf.Cos(a) * dist, 0.25f - 0.3f * dist, Mathf.Sin(a) * dist);
                float height = k == 0 ? 1.6f : 0.95f + 0.6f * (float)rng.NextDouble();
                float radius = 0.06f + 0.045f * (float)rng.NextDouble();
                float lean = k == 0 ? 6f : 12f + 18f * (float)rng.NextDouble();
                Quaternion tilt = Quaternion.AngleAxis(lean, new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)))
                                  * Quaternion.AngleAxis((float)rng.NextDouble() * 60f, Vector3.up);
                AddCrystal(verts, tris, root, tilt, radius, height);
            }
            _shardMesh = FinishMesh("Nova_CargoShards", verts, tris);
            return _shardMesh;
        }

        private static void AddCrystal(List<Vector3> verts, List<int> tris, Vector3 root, Quaternion rot, float radius, float height)
        {
            const int Sides = 6;
            var ring0 = new Vector3[Sides];
            var ring1 = new Vector3[Sides];
            float shoulder = height * 0.68f;
            for (int i = 0; i < Sides; i++)
            {
                float ang = i / (float)Sides * Mathf.PI * 2f;
                var p = new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
                ring0[i] = root + rot * p;
                ring1[i] = root + rot * (p + Vector3.up * shoulder);
            }
            Vector3 tip = root + rot * (Vector3.up * height);
            void Tri(Vector3 a, Vector3 b, Vector3 c)
            {
                int n = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);
                tris.Add(n); tris.Add(n + 1); tris.Add(n + 2);
            }
            for (int i = 0; i < Sides; i++)
            {
                int j = (i + 1) % Sides;
                Tri(ring0[i], ring1[i], ring0[j]);
                Tri(ring0[j], ring1[i], ring1[j]);
                Tri(ring1[i], tip, ring1[j]);
            }
        }

        private static Mesh FinishMesh(string name, List<Vector3> verts, List<int> tris)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            var uvs = new Vector2[verts.Count];
            for (int i = 0; i < uvs.Length; i++) uvs[i] = new Vector2(0.5f, 0.5f);
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
