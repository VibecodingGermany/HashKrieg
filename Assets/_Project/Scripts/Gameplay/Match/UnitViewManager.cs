using System;
using System.Collections.Generic;
using UnityEngine;
using Nova.Core;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.State;
using Nova.Simulation.Vision;
using EntityId = Nova.Core.EntityId;

namespace Nova.Gameplay.Match
{
    /// <summary>
    /// Owns the Unity view GameObjects of the simulation entities the local
    /// player is allowed to see and interpolates them at full frame rate from
    /// the 10-Hz simulation state.
    /// <para>
    /// Fog of War (docs/tech/FogOfWar.md section 4, docs/tech/CameraSystem.md
    /// section 1, docs/tech/InputSystem.md section 2): the view set is fed
    /// exclusively from <see cref="FogOfWarSystem.GetVisibleEntities"/> — the
    /// committed team view, which already contains the viewer's own entities
    /// plus every foreign entity standing in a <c>Visible</c> cell. The raw
    /// entity store is never iterated here, so no proxy exists for a hidden
    /// entity and world picking cannot leak a target id. Entities that die or
    /// leave vision return their view to a per-shape pool within the same
    /// frame.
    /// </para>
    /// <para>
    /// Graybox readability: shape encodes the <see cref="UnitRole"/> and colour
    /// encodes the owning player slot. Both channels carry the same distinction
    /// twice, which is the shape/colour redundancy the accessibility baseline
    /// requires; it is intentionally a switch plus a two-entry colour array and
    /// no asset, registry or material, so it deletes in one commit once real
    /// art lands.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class UnitViewManager : MonoBehaviour
    {
        /// <summary>Slot count of the primitive pool table (<see cref="PrimitiveType"/> has six members).</summary>
        private const int ShapePoolCount = 6;

        /// <summary>Shape key of a view instantiated from <see cref="_unitPrefab"/> instead of a primitive.</summary>
        private const int PrefabShapeKey = -1;

        /// <summary>Render height of a prefab view; the prefab owns its own pivot and scale.</summary>
        private const float PrefabGroundOffset = 0.5f;

        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        [Header("References")]
        [SerializeField] private MatchRunner _matchRunner;
        [SerializeField] private GameObject _unitPrefab;
        [SerializeField] private float _interpolationSpeed = 25f;

        [Header("Fog of War")]
        [Tooltip("Team slot whose committed Fog-of-War view is rendered. -1 follows MatchRunner.Session.LocalSlot.")]
        [SerializeField] private int _viewerTeamOverride = -1;

        [Header("Graybox Player Colours")]
        [Tooltip("Tint per owning player slot. Index 0 is the local MS-1 slot, index 1 the opponent.")]
        [SerializeField]
        private Color[] _playerColors =
        {
            new Color(0.20f, 0.55f, 0.95f, 1f),
            new Color(0.95f, 0.35f, 0.15f, 1f)
        };

        [Tooltip("Tint for owners outside the configured colour array.")]
        [SerializeField] private Color _unknownPlayerColor = new Color(0.62f, 0.62f, 0.62f, 1f);

        // Slot-indexed view table (index == EntityId.Index).
        private GameObject[] _viewInstances;
        private Renderer[] _viewRenderers;
        private EntityId[] _boundIds;
        private UnitRole[] _viewRoles;
        private int[] _viewShapeKeys;
        private float[] _viewGroundOffsets;
        private int[] _viewOwners;
        private int[] _lastSeenFrame;
        private bool[] _tracked;

        // Compact list of slots that currently own a view, so the per-frame
        // sweep is O(visible) instead of O(entity capacity).
        private readonly List<int> _activeIndices = new List<int>(256);
        private readonly List<EntityId> _visibleScratch = new List<EntityId>(256);
        // Keyed by the view GameObject itself, NOT by GetInstanceID(): Unity 6
        // marks Object.GetInstanceID() obsolete-as-error (CS0619). Object
        // overrides Equals/GetHashCode by instance id, so the reference is a
        // valid dictionary key and the lookup semantics are unchanged.
        private readonly Dictionary<GameObject, int> _viewObjectToSlot = new Dictionary<GameObject, int>(256);
        private readonly Stack<GameObject>[] _shapePools = new Stack<GameObject>[ShapePoolCount];
        private readonly Stack<GameObject> _prefabPool = new Stack<GameObject>();
        private MaterialPropertyBlock _propertyBlock;
        private int _frameStamp;
        private bool _fogUnavailableLogged;

        /// <summary>Number of entities that currently own a live view (i.e. are visible to the viewer team).</summary>
        public int VisibleViewCount => _activeIndices.Count;

        /// <summary>
        /// Team slot whose committed Fog-of-War view drives the rendered set.
        /// Negative means "follow <see cref="MatchSession.LocalSlot"/>".
        /// </summary>
        public int ViewerTeamOverride => _viewerTeamOverride;

        public void Initialize(MatchRunner runner, GameObject unitPrefab = null)
        {
            _matchRunner = runner ?? throw new ArgumentNullException(nameof(runner));
            _unitPrefab = unitPrefab;

            EnsureBuffers();
        }

        /// <summary>
        /// Points the view at another team slot; pass a negative value to
        /// follow the session's local slot again. Presentation-only — it
        /// changes what this client renders, never the simulation.
        /// </summary>
        public void SetViewerTeamOverride(int team)
        {
            if (_viewerTeamOverride == team) return;
            _viewerTeamOverride = team;
            ReleaseAllViews();
        }

        /// <summary>
        /// The live view of an entity, if that entity is currently visible to
        /// the viewer team. Hidden entities have no view by construction.
        /// </summary>
        public bool TryGetView(EntityId id, out GameObject view)
        {
            view = null;
            if (_viewInstances == null || !id.IsValid) return false;
            int slot = id.Index;
            if (slot < 0 || slot >= _viewInstances.Length) return false;
            if (_viewInstances[slot] == null || _boundIds[slot] != id) return false;

            view = _viewInstances[slot];
            return true;
        }

        /// <summary>
        /// Resolves a view GameObject (e.g. a raycast hit) back to its entity.
        /// Only entities inside the committed team view can be resolved, which
        /// is what keeps world picking Fog-of-War legal
        /// (docs/tech/CameraSystem.md section 1).
        /// </summary>
        public bool TryGetEntityId(GameObject viewObject, out EntityId id)
        {
            id = EntityId.Invalid;
            if (viewObject == null || _viewInstances == null) return false;

            Transform cursor = viewObject.transform;
            while (cursor != null && cursor != transform)
            {
                if (_viewObjectToSlot.TryGetValue(cursor.gameObject, out int slot))
                {
                    if (_viewInstances[slot] == null) return false;
                    id = _boundIds[slot];
                    return id.IsValid;
                }
                cursor = cursor.parent;
            }
            return false;
        }

        private void LateUpdate()
        {
            if (_matchRunner == null || !_matchRunner.IsRunning) return;

            EntityManager entities = _matchRunner.Entities;
            FogOfWarSystem fog = _matchRunner.FogOfWar;
            if (entities == null) return;
            if (fog == null)
            {
                if (!_fogUnavailableLogged)
                {
                    _fogUnavailableLogged = true;
                    Debug.LogError("[UnitViewManager] No FogOfWarSystem on the MatchRunner; rendering nothing rather than revealing hidden entities.");
                }
                ReleaseAllViews();
                return;
            }

            EnsureBuffers();

            byte viewerTeam = ResolveViewerTeam(fog);
            _frameStamp++;

            // 1) The committed team view is the ONLY source of renderable
            //    entities (own units included, foreign units only in Visible
            //    cells). Nothing here touches EntityManager.RawUnits.
            _visibleScratch.Clear();
            fog.GetVisibleEntities(viewerTeam, _visibleScratch);

            float blend = 1f - Mathf.Exp(-Mathf.Max(0f, _interpolationSpeed) * Time.deltaTime);

            for (int i = 0; i < _visibleScratch.Count; i++)
            {
                EntityId id = _visibleScratch[i];
                if (!entities.TryGetUnit(id, out UnitState unit)) continue;

                int slot = id.Index;
                if (slot < 0 || slot >= _viewInstances.Length) continue;

                // A rebind is required for a recycled slot (new version) and
                // when the role changed in place — a construction site carries
                // UnitRole.Unit until ConstructionSystem promotes it to the
                // finished building role, and the shape must follow.
                bool spawned = false;
                if (_viewInstances[slot] == null || _boundIds[slot] != id || _viewRoles[slot] != unit.Role)
                {
                    ReleaseView(slot);
                    AcquireView(slot, in unit);
                    spawned = true;
                }

                if (_viewOwners[slot] != unit.PlayerId)
                {
                    _viewOwners[slot] = unit.PlayerId;
                    ApplyTint(slot, unit.PlayerId);
                }

                _lastSeenFrame[slot] = _frameStamp;
                ApplyTransform(slot, in unit, spawned ? 1f : blend);
            }

            // 2) Everything that was not reported visible this frame died,
            //    was despawned or slipped back under the fog: the view goes
            //    back into the pool, it is never leaked and never left behind
            //    as a pickable proxy.
            for (int i = _activeIndices.Count - 1; i >= 0; i--)
            {
                int slot = _activeIndices[i];
                if (_lastSeenFrame[slot] == _frameStamp) continue;

                ReleaseView(slot);
                _tracked[slot] = false;
                int last = _activeIndices.Count - 1;
                _activeIndices[i] = _activeIndices[last];
                _activeIndices.RemoveAt(last);
            }
        }

        private byte ResolveViewerTeam(FogOfWarSystem fog)
        {
            int team = _viewerTeamOverride;
            if (team < 0)
            {
                MatchSession session = _matchRunner.Session;
                team = session != null ? session.LocalSlot : 0;
            }
            if (team >= fog.TeamCount) team = fog.TeamCount - 1;
            if (team < 0) team = 0;
            return (byte)team;
        }

        private void EnsureBuffers()
        {
            EntityManager entities = _matchRunner != null ? _matchRunner.Entities : null;
            if (entities == null) return;

            int capacity = entities.Capacity;
            if (_viewInstances != null && _viewInstances.Length == capacity)
            {
                if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
                return;
            }

            ReleaseAllViews();

            _viewInstances = new GameObject[capacity];
            _viewRenderers = new Renderer[capacity];
            _boundIds = new EntityId[capacity];
            _viewRoles = new UnitRole[capacity];
            _viewShapeKeys = new int[capacity];
            _viewGroundOffsets = new float[capacity];
            _viewOwners = new int[capacity];
            _lastSeenFrame = new int[capacity];
            _tracked = new bool[capacity];

            for (int i = 0; i < capacity; i++)
            {
                _boundIds[i] = EntityId.Invalid;
                _viewShapeKeys[i] = PrefabShapeKey;
                _viewOwners[i] = -1;
            }

            // Size the per-frame scratch to the worst case once, so the
            // LateUpdate loop never grows a backing array mid-match.
            if (_visibleScratch.Capacity < capacity) _visibleScratch.Capacity = capacity;
            if (_activeIndices.Capacity < capacity) _activeIndices.Capacity = capacity;

            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
        }

        private void AcquireView(int slot, in UnitState unit)
        {
            GameObject instance;
            int shapeKey;
            float groundOffset;

            if (_unitPrefab != null)
            {
                shapeKey = PrefabShapeKey;
                groundOffset = PrefabGroundOffset;
                if (_prefabPool.Count > 0)
                {
                    instance = _prefabPool.Pop();
                }
                else
                {
                    instance = Instantiate(_unitPrefab, transform);
                }
                instance.SetActive(true);
            }
            else
            {
                GetRoleShape(unit.Role, out PrimitiveType primitive, out Vector3 scale);
                shapeKey = (int)primitive;
                groundOffset = GroundOffset(primitive, scale);

                Stack<GameObject> pool = _shapePools[shapeKey];
                if (pool != null && pool.Count > 0)
                {
                    instance = pool.Pop();
                    instance.SetActive(true);
                }
                else
                {
                    instance = GameObject.CreatePrimitive(primitive);
                    instance.transform.SetParent(transform, false);
                    instance.name = "UnitView_" + primitive;
                }
                instance.transform.localScale = scale;
            }

#if UNITY_EDITOR
            // Editor-only hierarchy readability; the string allocation never
            // ships in a player build.
            instance.name = $"UnitView_{unit.Id.Index}.{unit.Id.Version}_{unit.Role}";
#endif

            _viewInstances[slot] = instance;
            _viewRenderers[slot] = instance.GetComponentInChildren<Renderer>(true);
            _boundIds[slot] = unit.Id;
            _viewRoles[slot] = unit.Role;
            _viewShapeKeys[slot] = shapeKey;
            _viewGroundOffsets[slot] = groundOffset;
            _viewOwners[slot] = -1; // forces a tint on this frame
            _viewObjectToSlot[instance] = slot;

            if (!_tracked[slot])
            {
                _tracked[slot] = true;
                _activeIndices.Add(slot);
            }
        }

        /// <summary>
        /// Returns the view of a slot to its pool. The caller owns the
        /// <see cref="_activeIndices"/> bookkeeping, so a rebind inside the
        /// same frame does not duplicate the slot in the list.
        /// </summary>
        private void ReleaseView(int slot)
        {
            GameObject instance = _viewInstances[slot];
            if (instance == null)
            {
                _boundIds[slot] = EntityId.Invalid;
                _viewRenderers[slot] = null;
                _viewOwners[slot] = -1;
                return;
            }

            _viewObjectToSlot.Remove(instance);
            instance.SetActive(false);

            int shapeKey = _viewShapeKeys[slot];
            if (shapeKey == PrefabShapeKey)
            {
                _prefabPool.Push(instance);
            }
            else
            {
                Stack<GameObject> pool = _shapePools[shapeKey];
                if (pool == null)
                {
                    pool = new Stack<GameObject>();
                    _shapePools[shapeKey] = pool;
                }
                pool.Push(instance);
            }

            _viewInstances[slot] = null;
            _viewRenderers[slot] = null;
            _boundIds[slot] = EntityId.Invalid;
            _viewOwners[slot] = -1;
        }

        private void ApplyTransform(int slot, in UnitState unit, float blend)
        {
            Transform viewTransform = _viewInstances[slot].transform;

            // Presentation boundary: SimFixed/SimAngle -> float happens here
            // and nowhere upstream; the simulation stays authoritative.
            var targetPos = new Vector3(
                unit.Transform.PositionX.ToFloat(),
                _viewGroundOffsets[slot],
                unit.Transform.PositionY.ToFloat());
            Quaternion targetRot = Quaternion.Euler(0f, unit.Transform.Rotation.ToDegrees().ToFloat(), 0f);

            if (blend >= 1f)
            {
                viewTransform.position = targetPos;
                viewTransform.rotation = targetRot;
                return;
            }

            // Frame-rate independent exponential smoothing between the 10-Hz
            // authoritative positions (blend is derived from _interpolationSpeed).
            viewTransform.position = Vector3.Lerp(viewTransform.position, targetPos, blend);
            viewTransform.rotation = Quaternion.Slerp(viewTransform.rotation, targetRot, blend);
        }

        private void ApplyTint(int slot, byte playerId)
        {
            Renderer renderer = _viewRenderers[slot];
            if (renderer == null) return;

            Color color = ColorForPlayer(playerId);
            _propertyBlock.Clear();
            // Built-in RP reads _Color, URP/HDRP read _BaseColor. Setting both
            // keeps the graybox tinted through the pipeline migration instead
            // of falling back to magenta / untinted white.
            _propertyBlock.SetColor(ColorPropertyId, color);
            _propertyBlock.SetColor(BaseColorPropertyId, color);
            renderer.SetPropertyBlock(_propertyBlock);
        }

        private Color ColorForPlayer(byte playerId)
        {
            if (_playerColors != null && playerId < _playerColors.Length)
            {
                return _playerColors[playerId];
            }
            return _unknownPlayerColor;
        }

        /// <summary>
        /// Graybox shape table: every <see cref="UnitRole"/> member maps to a
        /// primitive and a metre-scale. Infantry are thin tall capsules,
        /// vehicles flat wide cubes, buildings large blocks, the harvester the
        /// only cylinder and the radar the only sphere — so role stays readable
        /// without any colour information, and colour then adds the owner.
        /// </summary>
        private static void GetRoleShape(UnitRole role, out PrimitiveType primitive, out Vector3 scale)
        {
            switch (role)
            {
                // Generic entity and, until ConstructionSystem promotes it, the
                // unfinished construction site: a low ground pad.
                case UnitRole.Unit:
                    primitive = PrimitiveType.Cube;
                    scale = new Vector3(1.0f, 0.30f, 1.0f);
                    return;

                // --- Infantry-class: thin tall capsules --------------------
                case UnitRole.Builder:
                    primitive = PrimitiveType.Capsule;
                    scale = new Vector3(0.60f, 0.50f, 0.60f);
                    return;
                case UnitRole.BasicInfantry:
                    primitive = PrimitiveType.Capsule;
                    scale = new Vector3(0.50f, 0.60f, 0.50f);
                    return;
                case UnitRole.AntiArmorInfantry:
                    primitive = PrimitiveType.Capsule;
                    scale = new Vector3(0.55f, 0.78f, 0.55f);
                    return;

                // --- Harvester: the only cylinder --------------------------
                case UnitRole.Harvester:
                    primitive = PrimitiveType.Cylinder;
                    scale = new Vector3(1.10f, 0.45f, 1.10f);
                    return;

                // --- Vehicles: flat wide cubes, longer with weight ---------
                case UnitRole.ScoutVehicle:
                    primitive = PrimitiveType.Cube;
                    scale = new Vector3(1.00f, 0.35f, 1.40f);
                    return;
                case UnitRole.LightTank:
                    primitive = PrimitiveType.Cube;
                    scale = new Vector3(1.30f, 0.45f, 1.70f);
                    return;
                case UnitRole.BattleTank:
                    primitive = PrimitiveType.Cube;
                    scale = new Vector3(1.60f, 0.60f, 2.10f);
                    return;
                case UnitRole.Artillery:
                    primitive = PrimitiveType.Cube;
                    scale = new Vector3(1.20f, 0.50f, 2.40f);
                    return;

                // --- Buildings: large blocks, footprint per function -------
                case UnitRole.HQ:
                    primitive = PrimitiveType.Cube;
                    scale = new Vector3(4.00f, 2.20f, 4.00f);
                    return;
                case UnitRole.Refinery:
                    primitive = PrimitiveType.Cube;
                    scale = new Vector3(3.60f, 1.40f, 2.60f);
                    return;
                case UnitRole.Power:
                    primitive = PrimitiveType.Cube;
                    scale = new Vector3(2.60f, 1.80f, 2.60f);
                    return;
                case UnitRole.Storage:
                    primitive = PrimitiveType.Cube;
                    scale = new Vector3(2.40f, 1.20f, 2.40f);
                    return;
                case UnitRole.Barracks:
                    primitive = PrimitiveType.Cube;
                    scale = new Vector3(3.00f, 1.40f, 2.20f);
                    return;
                case UnitRole.VehicleFactory:
                    primitive = PrimitiveType.Cube;
                    scale = new Vector3(3.40f, 1.40f, 3.00f);
                    return;
                case UnitRole.ResearchLab:
                    primitive = PrimitiveType.Cube;
                    scale = new Vector3(2.40f, 1.90f, 2.40f);
                    return;

                // --- Radar: the only sphere (dome), raised clear of the pad -
                case UnitRole.Radar:
                    primitive = PrimitiveType.Sphere;
                    scale = new Vector3(2.00f, 2.00f, 2.00f);
                    return;

                case UnitRole.DefensePlatform:
                    primitive = PrimitiveType.Cube;
                    scale = new Vector3(1.80f, 1.60f, 1.80f);
                    return;

                default:
                    // Unknown role value (content added ahead of this table):
                    // a loud oversized pad rather than an invisible entity.
                    primitive = PrimitiveType.Cube;
                    scale = new Vector3(1.50f, 1.50f, 1.50f);
                    return;
            }
        }

        /// <summary>
        /// Half height of the scaled primitive, so every graybox body stands on
        /// y = 0 instead of sinking into the ground plane. Unity's capsule and
        /// cylinder meshes are two units tall, cube and sphere one.
        /// </summary>
        private static float GroundOffset(PrimitiveType primitive, Vector3 scale)
        {
            switch (primitive)
            {
                case PrimitiveType.Capsule:
                case PrimitiveType.Cylinder:
                    return scale.y;
                default:
                    return scale.y * 0.5f;
            }
        }

        private void ReleaseAllViews()
        {
            if (_viewInstances != null)
            {
                for (int i = 0; i < _activeIndices.Count; i++)
                {
                    int slot = _activeIndices[i];
                    ReleaseView(slot);
                    _tracked[slot] = false;
                }
            }
            _activeIndices.Clear();
            _viewObjectToSlot.Clear();
        }

        private void OnDestroy()
        {
            ReleaseAllViews();

            for (int i = 0; i < _shapePools.Length; i++)
            {
                Stack<GameObject> pool = _shapePools[i];
                if (pool == null) continue;
                while (pool.Count > 0)
                {
                    GameObject pooled = pool.Pop();
                    if (pooled != null) Destroy(pooled);
                }
            }

            while (_prefabPool.Count > 0)
            {
                GameObject pooled = _prefabPool.Pop();
                if (pooled != null) Destroy(pooled);
            }
        }
    }
}
