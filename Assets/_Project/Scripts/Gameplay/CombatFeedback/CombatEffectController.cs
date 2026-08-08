using System;
using System.Collections.Generic;
using Nova.Gameplay.Audio;
using Nova.Simulation.Combat;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;
using UnityEngine;
using UnityEngine.Pool;

namespace Nova.Gameplay.CombatFeedback
{
    /// <summary>
    /// Presentation-only renderer for the fog-safe events reconstructed by
    /// <see cref="VisibleCombatFrameDiffer"/>. All endpoints are copied values:
    /// a tracer never follows an entity and therefore cannot imply ballistics
    /// that the authoritative hitscan simulation does not have.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatEffectController : MonoBehaviour
    {
        public const int MaxActiveEffects = 64;
        public const int MaxMuzzleLights = 8;
        public const float TracerDurationSeconds = 0.1f;

        private readonly List<TransientEffect> _active = new List<TransientEffect>(MaxActiveEffects);
        private readonly Dictionary<EffectKind, ObjectPool<TransientEffect>> _pools =
            new Dictionary<EffectKind, ObjectPool<TransientEffect>>();

        private int _activeMuzzleLights;
        private Material _tracerMaterial;
        private Material _particleMaterial;
        private Mesh _particleMesh;

        /// <summary>Current global transient count; exposed for budget tests and diagnostics.</summary>
        public int ActiveEffectCount => _active.Count;

        /// <summary>Live point lights, independently capped below the global effect budget.</summary>
        public int ActiveMuzzleLightCount => _activeMuzzleLights;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_pools.Count > 0) return;
            _tracerMaterial = CreateOwnedMaterial(
                "Sprites/Default",
                "Universal Render Pipeline/Particles/Unlit",
                "Standard");
            _particleMaterial = CreateOwnedMaterial(
                "Universal Render Pipeline/Particles/Unlit",
                "Particles/Standard Unlit",
                "Universal Render Pipeline/Unlit");
            CreatePools();
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                TransientEffect effect = _active[i];
                float elapsed = now - effect.StartedAt;
                if (effect.Kind == EffectKind.Tracer)
                {
                    float t = Mathf.Clamp01(elapsed / TracerDurationSeconds);
                    if (effect.Projectile != null)
                    {
                        effect.Projectile.position = Vector3.Lerp(effect.Start, effect.End, t);
                    }
                }

                if (elapsed >= effect.Duration)
                {
                    ReleaseAt(i);
                }
            }
        }

        /// <summary>Consumes one differ batch. Calling with an empty batch is allocation-free.</summary>
        public void Present(IReadOnlyList<CombatFeedbackEvent> events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));
            for (int i = 0; i < events.Count; i++)
            {
                Present(events[i]);
            }
        }

        public void Present(in CombatFeedbackEvent feedback)
        {
            EnsureInitialized();
            switch (feedback.Kind)
            {
                case CombatFeedbackKind.Shot:
                    SpawnMuzzle(feedback.SourcePosition, feedback.DamageType);
                    PlayWeapon(feedback);
                    if (feedback.HasTargetPosition)
                    {
                        SpawnTracer(feedback.SourcePosition, feedback.TargetPosition, feedback.DamageType);
                    }
                    break;

                case CombatFeedbackKind.Hit:
                    SpawnImpact(feedback.TargetPosition, feedback.DamageType);
                    AudioServiceLocator.Play3D(
                        feedback.DamageType == DamageType.Explosive
                            ? SoundEventId.IMP_Explosive
                            : SoundEventId.IMP_Kinetic,
                        feedback.TargetPosition,
                        AudioCategory.Impact);
                    break;

                case CombatFeedbackKind.Death:
                    bool building = SimDefinitions.IsBuildingRole(feedback.TargetRole);
                    // A correlated lethal shot removes the target before a
                    // health-decrease sample can exist. Recreate its impact
                    // here; an uncorrelated own-unit disappearance retains an
                    // invalid SourceId and therefore never invents a hit.
                    if (feedback.SourceId.IsValid)
                    {
                        SpawnImpact(feedback.TargetPosition, feedback.DamageType);
                        AudioServiceLocator.Play3D(
                            feedback.DamageType == DamageType.Explosive
                                ? SoundEventId.IMP_Explosive
                                : SoundEventId.IMP_Kinetic,
                            feedback.TargetPosition,
                            AudioCategory.Impact);
                    }
                    if (building)
                    {
                        Spawn(EffectKind.Smoke, feedback.TargetPosition, feedback.TargetPosition, 1.5f,
                            new Color(0.42f, 0.46f, 0.50f, 0.9f));
                    }
                    AudioServiceLocator.Play3D(
                        building ? SoundEventId.DTH_Building : SoundEventId.DTH_Unit,
                        feedback.TargetPosition,
                        AudioCategory.Unit,
                        VoicePriority.High);
                    break;

                case CombatFeedbackKind.UnitReady:
                    AudioServiceLocator.Play2D(
                        SoundEventId.PRD_UnitReady,
                        AudioCategory.Production,
                        VoicePriority.High);
                    break;
            }
        }

        /// <summary>Releases every live effect immediately on match/view reset.</summary>
        public void ResetEffects()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ReleaseAt(i);
            }
        }

        private void OnDestroy()
        {
            ResetEffects();
            foreach (ObjectPool<TransientEffect> pool in _pools.Values)
            {
                pool.Clear();
            }
            _pools.Clear();
            DestroyOwned(_tracerMaterial);
            DestroyOwned(_particleMaterial);
        }

        private void SpawnMuzzle(Vector3 position, DamageType damageType)
        {
            Color color = WeaponColor(damageType);
            TransientEffect effect = Spawn(EffectKind.Muzzle, position, position, 0.08f, color);
            if (effect == null || effect.Light == null || _activeMuzzleLights >= MaxMuzzleLights) return;

            effect.Light.color = color;
            effect.Light.intensity = 2.2f;
            effect.Light.range = 4f;
            effect.Light.enabled = true;
            effect.OwnsMuzzleLight = true;
            _activeMuzzleLights++;
        }

        private void SpawnTracer(Vector3 start, Vector3 end, DamageType damageType)
        {
            Spawn(EffectKind.Tracer, start, end, TracerDurationSeconds, WeaponColor(damageType));
        }

        private void SpawnImpact(Vector3 position, DamageType damageType)
        {
            Color color = damageType == DamageType.Explosive
                ? new Color(4.2f, 1.15f, 0.15f, 1f)
                : new Color(1.1f, 2.8f, 4.0f, 1f);
            Spawn(EffectKind.Impact, position, position, 0.18f, color);
        }

        private TransientEffect Spawn(
            EffectKind kind,
            Vector3 start,
            Vector3 end,
            float duration,
            Color color)
        {
            if (_active.Count >= MaxActiveEffects) return null;
            if (!_pools.TryGetValue(kind, out ObjectPool<TransientEffect> pool)) return null;

            TransientEffect effect = pool.Get();
            effect.Kind = kind;
            effect.Start = start;
            effect.End = end;
            effect.StartedAt = Time.unscaledTime;
            effect.Duration = Mathf.Max(0.01f, duration);
            effect.OwnsMuzzleLight = false;
            effect.Root.transform.position = start;

            switch (kind)
            {
                case EffectKind.Tracer:
                    ConfigureTracer(effect, start, end, color);
                    break;
                case EffectKind.Muzzle:
                    Emit(effect.Particles, color, 6, 0.13f, 0.08f, 4.5f);
                    break;
                case EffectKind.Impact:
                    Emit(effect.Particles, color, 10, 0.10f, 0.18f, 6f);
                    break;
                case EffectKind.Smoke:
                    Emit(effect.Particles, color, 14, 0.42f, 1.5f, 1.8f);
                    break;
            }

            _active.Add(effect);
            return effect;
        }

        private void ReleaseAt(int index)
        {
            TransientEffect effect = _active[index];
            _active.RemoveAt(index);
            if (effect.OwnsMuzzleLight)
            {
                effect.OwnsMuzzleLight = false;
                _activeMuzzleLights = Mathf.Max(0, _activeMuzzleLights - 1);
            }
            if (effect.Light != null) effect.Light.enabled = false;
            effect.Particles?.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (effect.Line != null) effect.Line.enabled = false;
            if (effect.Projectile != null) effect.Projectile.gameObject.SetActive(false);
            _pools[effect.Kind].Release(effect);
        }

        private void CreatePools()
        {
            foreach (EffectKind kind in Enum.GetValues(typeof(EffectKind)))
            {
                EffectKind captured = kind;
                _pools[kind] = new ObjectPool<TransientEffect>(
                    createFunc: () => CreateEffect(captured),
                    actionOnGet: effect => effect.Root.SetActive(true),
                    actionOnRelease: effect => effect.Root.SetActive(false),
                    actionOnDestroy: effect =>
                    {
                        DestroyOwned(effect.Root);
                    },
                    collectionCheck: true,
                    defaultCapacity: 8,
                    maxSize: MaxActiveEffects);
            }
        }

        private TransientEffect CreateEffect(EffectKind kind)
        {
            var root = new GameObject($"CombatFx_{kind}");
            root.transform.SetParent(transform, false);
            var effect = new TransientEffect(root) { Kind = kind };

            if (kind == EffectKind.Tracer)
            {
                effect.Line = root.AddComponent<LineRenderer>();
                effect.Line.useWorldSpace = true;
                effect.Line.positionCount = 2;
                effect.Line.numCapVertices = 2;
                effect.Line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                effect.Line.receiveShadows = false;
                effect.Line.sharedMaterial = _tracerMaterial;

                var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projectile.name = "HitscanTracerHead";
                projectile.transform.SetParent(root.transform, false);
                projectile.transform.localScale = Vector3.one * 0.09f;
                Collider collider = projectile.GetComponent<Collider>();
                DestroyOwned(collider);
                Renderer projectileRenderer = projectile.GetComponent<Renderer>();
                if (projectileRenderer != null) projectileRenderer.sharedMaterial = _tracerMaterial;
                effect.Projectile = projectile.transform;
            }
            else
            {
                effect.Particles = root.AddComponent<ParticleSystem>();
                ParticleSystem.MainModule main = effect.Particles.main;
                main.loop = false;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = kind == EffectKind.Smoke ? 24 : 16;

                ParticleSystem.EmissionModule emission = effect.Particles.emission;
                emission.enabled = false;
                ParticleSystem.ShapeModule shape = effect.Particles.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = kind == EffectKind.Smoke ? 0.45f : 0.10f;
                ParticleSystemRenderer particleRenderer = effect.Particles.GetComponent<ParticleSystemRenderer>();
                if (particleRenderer != null)
                {
                    particleRenderer.sharedMaterial = _particleMaterial;
                    particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
                    particleRenderer.mesh = GetParticleMesh();
                }

                if (kind == EffectKind.Muzzle)
                {
                    effect.Light = root.AddComponent<Light>();
                    effect.Light.type = LightType.Point;
                    effect.Light.shadows = LightShadows.None;
                    effect.Light.enabled = false;
                }
            }

            root.SetActive(false);
            return effect;
        }

        private Mesh GetParticleMesh()
        {
            if (_particleMesh != null) return _particleMesh;

            // Unity exposes primitive meshes only through CreatePrimitive.
            // Capture its built-in shared mesh once, then destroy the inactive
            // temporary object; no texture or persistent asset is generated.
            GameObject temporary = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            temporary.hideFlags = HideFlags.HideAndDontSave;
            temporary.SetActive(false);
            _particleMesh = temporary.GetComponent<MeshFilter>().sharedMesh;
            DestroyOwned(temporary);
            return _particleMesh;
        }

        private static void DestroyOwned(UnityEngine.Object target)
        {
            if (target == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(target);
                return;
            }
#endif
            Destroy(target);
        }

        private static Material CreateOwnedMaterial(params string[] shaderNames)
        {
            Shader shader = null;
            for (int i = 0; i < shaderNames.Length && shader == null; i++)
            {
                shader = Shader.Find(shaderNames[i]);
            }
            if (shader == null)
            {
                Debug.LogError("[CombatEffectController] No supported runtime VFX shader was found.");
                return null;
            }

            var material = new Material(shader)
            {
                name = "Nova_CombatFx_Runtime",
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent,
            };
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }
            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            }
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            return material;
        }

        private static void ConfigureTracer(
            TransientEffect effect,
            Vector3 start,
            Vector3 end,
            Color color)
        {
            effect.Line.enabled = true;
            effect.Line.startWidth = 0.045f;
            effect.Line.endWidth = 0.018f;
            effect.Line.startColor = color;
            effect.Line.endColor = new Color(color.r, color.g, color.b, 0.15f);
            effect.Line.SetPosition(0, start);
            effect.Line.SetPosition(1, end);

            effect.Projectile.gameObject.SetActive(true);
            effect.Projectile.position = start;
            Renderer renderer = effect.Projectile.GetComponent<Renderer>();
            if (renderer != null)
            {
                MaterialPropertyBlock block = effect.ColorBlock;
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                renderer.SetPropertyBlock(block);
            }
        }

        private static void Emit(
            ParticleSystem particles,
            Color color,
            int count,
            float size,
            float lifetime,
            float speed)
        {
            if (particles == null) return;
            var emit = new ParticleSystem.EmitParams
            {
                startColor = color,
                startLifetime = lifetime,
                startSize = size,
                velocity = UnityEngine.Random.onUnitSphere * speed,
            };
            for (int i = 0; i < count; i++)
            {
                emit.velocity = UnityEngine.Random.onUnitSphere * speed;
                if (emit.velocity.y < 0f) emit.velocity = new Vector3(emit.velocity.x, -emit.velocity.y, emit.velocity.z);
                particles.Emit(emit, 1);
            }
        }

        private static void PlayWeapon(in CombatFeedbackEvent feedback)
        {
            SoundEventId id;
            if (feedback.DamageType == DamageType.Explosive)
            {
                id = SoundEventId.WPN_Explosive;
            }
            else if (feedback.SourceRole == UnitRole.BasicInfantry
                     || feedback.SourceRole == UnitRole.ScoutVehicle)
            {
                id = SoundEventId.WPN_Kinetic_Light;
            }
            else
            {
                id = SoundEventId.WPN_Kinetic_Heavy;
            }
            AudioServiceLocator.Play3D(id, feedback.SourcePosition, AudioCategory.Weapon);
        }

        private static Color WeaponColor(DamageType damageType)
        {
            return damageType == DamageType.Explosive
                ? new Color(4.0f, 1.0f, 0.1f, 1f)
                : new Color(0.5f, 2.5f, 4.0f, 1f);
        }

        private enum EffectKind : byte
        {
            Muzzle,
            Tracer,
            Impact,
            Smoke,
        }

        private sealed class TransientEffect
        {
            public GameObject Root { get; }
            public EffectKind Kind;
            public ParticleSystem Particles;
            public LineRenderer Line;
            public Transform Projectile;
            public Light Light;
            public Vector3 Start;
            public Vector3 End;
            public float StartedAt;
            public float Duration;
            public bool OwnsMuzzleLight;
            public MaterialPropertyBlock ColorBlock { get; } = new MaterialPropertyBlock();

            public TransientEffect(GameObject root) => Root = root;
        }
    }
}
