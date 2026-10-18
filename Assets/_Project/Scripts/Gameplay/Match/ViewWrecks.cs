using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nova.Gameplay.Match
{
    /// <summary>
    /// Destroyed units and buildings that stay on the field (D-115). A dead
    /// art view whose prefab carries an inactive <see cref="WreckNode"/>
    /// child is not faded out and pooled: it goes dark for a moment, swaps
    /// its live model for the wreck mesh (charred, broken — built by
    /// tools/art/asset_wreck.py) and then lies there.
    /// <para>
    /// Wrecks are pure view: they block nothing, are not pickable and ignore
    /// the fog (a wreck is what the player saw die). Two caps keep a long
    /// match bounded — beyond <see cref="MaxUnitWrecks"/> unit wrecks or
    /// <see cref="MaxBuildingWrecks"/> ruins the oldest sinks into the sand.
    /// A new building clears every wreck inside its footprint
    /// (<see cref="ClearArea"/>), so ruins never poke through fresh walls.
    /// </para>
    /// </summary>
    public sealed class ViewWrecks
    {
        /// <summary>Name of the inactive child that carries the destroyed state.</summary>
        public const string WreckNode = "Wreck";

        public const int MaxUnitWrecks = 60;
        public const int MaxBuildingWrecks = 20;

        /// <summary>Live model goes dark and jolts before the swap.</summary>
        public const float BlastSeconds = 0.25f;

        /// <summary>The wreck drops onto the ground after the swap.</summary>
        public const float SettleSeconds = 0.35f;

        /// <summary>A fresh wreck smoulders this long, thinning out.</summary>
        public const float SmokeSeconds = 9f;

        /// <summary>A capped or cleared wreck sinks this long before it is pooled.</summary>
        public const float SinkSeconds = 2.5f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly List<Entry> _entries = new List<Entry>(64);
        private readonly Action<GameObject, int, GameObject> _returnToPool;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        /// <param name="returnToPool">Called with (instance, shapeKey, sourcePrefab) once a wreck is gone and restored.</param>
        public ViewWrecks(Action<GameObject, int, GameObject> returnToPool)
        {
            _returnToPool = returnToPool;
        }

        public int Count => _entries.Count;

        /// <summary>The wreck child of a view, or null when its prefab has none.</summary>
        public static Transform FindWreck(GameObject instance)
        {
            return instance != null ? instance.transform.Find(WreckNode) : null;
        }

        /// <summary>
        /// Turns a detached, dead view into a wreck. The caller has already
        /// removed it from every slot table and disabled its colliders.
        /// </summary>
        public void Begin(GameObject instance, Transform wreck, int shapeKey, GameObject sourcePrefab,
                          bool isBuilding, Collider[] colliders, bool[] colliderStates)
        {
            var live = new List<Renderer>();
            foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.transform.IsChildOf(wreck)) live.Add(r);
            }

            var entry = new Entry
            {
                Instance = instance,
                Wreck = wreck,
                ShapeKey = shapeKey,
                SourcePrefab = sourcePrefab,
                IsBuilding = isBuilding,
                Live = live.ToArray(),
                Colliders = colliders,
                ColliderStates = colliderStates,
                StartPosition = instance.transform.position,
                Size = isBuilding ? 1.5f : 0.6f,
            };
            _entries.Add(entry);
            EnforceCap(isBuilding);
        }

        /// <summary>Sinks every wreck whose position lies within <paramref name="radius"/> (ground plane).</summary>
        public void ClearArea(Vector3 centre, float radius)
        {
            float r2 = radius * radius;
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                Vector3 d = e.StartPosition - centre;
                if (d.x * d.x + d.z * d.z <= r2) StartSink(e, 0.5f);
            }
        }

        public void Advance(float deltaTime)
        {
            float dt = Mathf.Max(0f, deltaTime);
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                Entry e = _entries[i];
                if (e.Instance == null)
                {
                    _entries.RemoveAt(i);
                    continue;
                }
                e.Elapsed += dt;

                if (e.SinkDuration > 0f)
                {
                    e.SinkElapsed += dt;
                    float s = Mathf.Clamp01(e.SinkElapsed / e.SinkDuration);
                    float depth = (e.IsBuilding ? 1.6f : 0.7f) * s * s;
                    e.Instance.transform.position = e.RestPosition + Vector3.down * depth;
                    if (s >= 1f)
                    {
                        Complete(i);
                    }
                    continue;
                }

                if (!e.Swapped)
                {
                    // Blast: live model blackens and jolts up a little.
                    float t = Mathf.Clamp01(e.Elapsed / BlastSeconds);
                    Tint(e.Live, Color.Lerp(Color.white, new Color(0.08f, 0.07f, 0.06f, 1f), t));
                    float jolt = Mathf.Sin(t * Mathf.PI) * 0.12f * e.Size;
                    e.Instance.transform.position = e.StartPosition + Vector3.up * jolt;
                    if (t >= 1f) Swap(e);
                    continue;
                }

                if (e.Elapsed < SmokeSeconds)
                {
                    // Smoulder: dense at first, thinning toward the end.
                    e.SmokeTimer -= dt;
                    if (e.SmokeTimer <= 0f)
                    {
                        float k = e.Elapsed / SmokeSeconds;
                        e.SmokeTimer = Mathf.Lerp(0.18f, 0.9f, k);
                        Vector3 top = e.StartPosition + Vector3.up * (e.IsBuilding ? 1.2f : 0.45f) * e.Size;
                        AmbientFx.Smoke(top, (e.IsBuilding ? 1.1f : 0.55f) * e.Size, 0.85f);
                    }
                }

                if (!e.Settled)
                {
                    // Settle: the wreck drops the last bit and bounces once.
                    float t = Mathf.Clamp01((e.Elapsed - BlastSeconds) / SettleSeconds);
                    float bounce = (1f - t) * Mathf.Abs(Mathf.Cos(t * Mathf.PI * 1.5f)) * 0.18f * e.Size;
                    e.Instance.transform.position = e.StartPosition + Vector3.up * bounce;
                    if (t >= 1f)
                    {
                        e.Settled = true;
                        e.Instance.transform.position = e.StartPosition;
                        e.RestPosition = e.StartPosition;
                    }
                }
            }
        }

        /// <summary>Restores and pools every wreck at once (match reset).</summary>
        public void CompleteAll()
        {
            for (int i = _entries.Count - 1; i >= 0; i--) Complete(i);
        }

        private void Swap(Entry e)
        {
            e.Swapped = true;
            for (int i = 0; i < e.Live.Length; i++)
            {
                if (e.Live[i] != null) e.Live[i].enabled = false;
            }
            e.Wreck.gameObject.SetActive(true);
            e.RestPosition = e.StartPosition;
        }

        private void EnforceCap(bool isBuilding)
        {
            int cap = isBuilding ? MaxBuildingWrecks : MaxUnitWrecks;
            int alive = 0;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                Entry e = _entries[i];
                if (e.IsBuilding != isBuilding || e.SinkDuration > 0f) continue;
                alive++;
                if (alive > cap) StartSink(e, SinkSeconds);
            }
        }

        private static void StartSink(Entry e, float seconds)
        {
            if (e.SinkDuration > 0f) return;
            if (!e.Swapped)
            {
                // Not even swapped yet: skip straight to a short sink of the wreck.
                e.Swapped = true;
                for (int i = 0; i < e.Live.Length; i++)
                {
                    if (e.Live[i] != null) e.Live[i].enabled = false;
                }
                e.Wreck.gameObject.SetActive(true);
            }
            e.Settled = true;
            e.RestPosition = e.Instance.transform.position;
            e.SinkDuration = Mathf.Max(0.05f, seconds);
            e.SinkElapsed = 0f;
        }

        private void Complete(int index)
        {
            Entry e = _entries[index];
            _entries.RemoveAt(index);
            if (e.Instance == null) return;

            for (int i = 0; i < e.Live.Length; i++)
            {
                if (e.Live[i] == null) continue;
                e.Live[i].enabled = true;
                e.Live[i].SetPropertyBlock(null);
            }
            if (e.Wreck != null) e.Wreck.gameObject.SetActive(false);
            for (int c = 0; c < e.Colliders.Length; c++)
            {
                if (e.Colliders[c] != null) e.Colliders[c].enabled = e.ColliderStates[c];
            }
            e.Instance.transform.position = e.StartPosition;
            UnitRigAnimator rig = e.Instance.GetComponent<UnitRigAnimator>();
            if (rig != null) rig.ResetPose();
            _returnToPool?.Invoke(e.Instance, e.ShapeKey, e.SourcePrefab);
        }

        private void Tint(Renderer[] renderers, Color color)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, color);
                _block.SetColor(ColorId, color);
                r.SetPropertyBlock(_block);
            }
        }

        private sealed class Entry
        {
            public GameObject Instance;
            public Transform Wreck;
            public int ShapeKey;
            public GameObject SourcePrefab;
            public bool IsBuilding;
            public Renderer[] Live;
            public Collider[] Colliders;
            public bool[] ColliderStates;
            public Vector3 StartPosition;
            public Vector3 RestPosition;
            public float Size;
            public float Elapsed;
            public bool Swapped;
            public bool Settled;
            public float SinkDuration;
            public float SinkElapsed;
            public float SmokeTimer;
        }
    }
}
