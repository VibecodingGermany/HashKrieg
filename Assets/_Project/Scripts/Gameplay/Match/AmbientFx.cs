using UnityEngine;

namespace Nova.Gameplay.Match
{
    /// <summary>
    /// Shared, emit-only particle systems for ambient motion: sand kicked up
    /// behind driving vehicles and smoke rising from chimneys and wrecks.
    /// One world-space system per kind serves every emitter in the match —
    /// a hundred tanks cost one renderer, not a hundred (AnimationSystem.md
    /// performance budget). Pure view; nothing here is read back.
    /// </summary>
    public static class AmbientFx
    {
        private static GameObject _root;
        private static ParticleSystem _dust;
        private static ParticleSystem _smoke;
        private static Material _material;
        private static Texture2D _puff;

        /// <summary>Sand cloud at ground level; <paramref name="size"/> in world units.</summary>
        public static void Dust(Vector3 position, Vector3 drift, float size)
        {
            if (!Ensure()) return;
            var p = new ParticleSystem.EmitParams
            {
                position = position + Random.insideUnitSphere * size * 0.25f,
                velocity = drift + new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(0.25f, 0.5f), Random.Range(-0.15f, 0.15f)),
                startSize = size * Random.Range(0.8f, 1.3f),
                startLifetime = Random.Range(0.9f, 1.4f),
                startColor = new Color(0.78f, 0.66f, 0.48f, 0.55f),
                rotation = Random.Range(0f, 360f),
            };
            _dust.Emit(p, 1);
        }

        /// <summary>Small glowing Aetherium-cyan puff (unloading harvesters); <paramref name="size"/> in world units.</summary>
        public static void GlowDust(Vector3 position, Vector3 drift, float size)
        {
            if (!Ensure()) return;
            var p = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = drift,
                startSize = size,
                startLifetime = Random.Range(0.55f, 0.9f),
                startColor = new Color(0.45f, 0.95f, 1f, 0.8f),
                rotation = Random.Range(0f, 360f),
            };
            _dust.Emit(p, 1);
        }

        /// <summary>Dark rising smoke puff; <paramref name="size"/> in world units.</summary>
        public static void Smoke(Vector3 position, float size, float darkness = 0.25f)
        {
            if (!Ensure()) return;
            float g = Mathf.Lerp(0.6f, 0.12f, darkness);
            var p = new ParticleSystem.EmitParams
            {
                position = position + Random.insideUnitSphere * size * 0.15f,
                velocity = new Vector3(Random.Range(-0.12f, 0.12f) + 0.15f, Random.Range(0.7f, 1.1f), Random.Range(-0.12f, 0.12f) + 0.1f),
                startSize = size * Random.Range(0.7f, 1.1f),
                startLifetime = Random.Range(2.2f, 3.2f),
                startColor = new Color(g, g * 0.97f, g * 0.94f, 0.6f),
                rotation = Random.Range(0f, 360f),
            };
            _smoke.Emit(p, 1);
        }

        private static bool Ensure()
        {
            if (_root != null) return _material != null;
            _root = new GameObject("AmbientFx") { hideFlags = HideFlags.DontSave };
            _material = CreateMaterial();
            _dust = Create("Dust", 900, 2.2f);
            _smoke = Create("Smoke", 600, 3.0f);
            return _material != null;
        }

        private static ParticleSystem Create(string name, int max, float growth)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            main.gravityModifier = -0.02f;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = false;

            // grow and fade out over the lifetime
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, growth));
            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.enabled = false;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = _material;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortMode = ParticleSystemSortMode.Distance;
            ps.Play();
            return ps;
        }

        private static Material CreateMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) return null;
            var m = new Material(shader) { name = "Nova_AmbientFx_Runtime", hideFlags = HideFlags.DontSave };
            m.SetTexture("_BaseMap", PuffTexture());
            m.SetTexture("_MainTex", PuffTexture());
            // URP particle unlit: transparent, alpha-blended, no depth write
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        /// <summary>Soft round puff, generated once (no texture asset).</summary>
        private static Texture2D PuffTexture()
        {
            if (_puff != null) return _puff;
            const int n = 64;
            _puff = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float noise = Mathf.PerlinNoise(x * 0.18f, y * 0.18f) * 0.35f + 0.65f;
                    float a = Mathf.Clamp01(1f - d) * noise;
                    a = a * a * (3f - 2f * a);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            _puff.SetPixels32(px);
            _puff.Apply();
            return _puff;
        }
    }
}
