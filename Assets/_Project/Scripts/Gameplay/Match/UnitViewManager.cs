using System;
using System.Collections.Generic;
using UnityEngine;
using Nova.Core;
using Nova.Data;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
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
    /// encodes the owning slot's FACTION (<see cref="FactionTint"/>, D-072
    /// palettes) — no longer the raw player slot. Both channels still carry
    /// the distinction twice, which is the shape/colour redundancy the
    /// accessibility baseline requires. Entities whose definition id has a
    /// registered <c>PF_*</c> prefab in <see cref="_assetMappings"/> render as
    /// that prefab instead of a primitive (art drop-in path, ArtAssetStandard.md);
    /// the primitive table below stays the fallback for every unmapped role.
    /// </para>
    /// <para>
    /// Health readout: brightness of that same tint carries the health
    /// fraction, so damage is observable at a glance without a single extra
    /// GameObject, Canvas or draw call — it rides the
    /// <see cref="MaterialPropertyBlock"/> the owner tint already uses. The
    /// value is quantised into <see cref="HealthTintSteps"/> buckets and the
    /// block is only re-applied when the bucket changes, so a match at full
    /// health costs exactly what it cost before. An undamaged unit is tinted
    /// with the unmodified owner colour, i.e. the pre-existing look is
    /// bit-identical.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class UnitViewManager : MonoBehaviour
    {
        /// <summary>Slot count of the primitive pool table (<see cref="PrimitiveType"/> has six members).</summary>
        private const int ShapePoolCount = 6;

        /// <summary>
        /// Quantisation of the health tint. Sixteen buckets are far below the
        /// perceptual threshold of the ramp yet coarse enough that a unit under
        /// sustained fire re-tints a handful of times instead of every frame.
        /// </summary>
        private const int HealthTintSteps = 16;

        /// <summary>Shape key of a view instantiated from a prefab instead of a primitive.</summary>
        private const int PrefabShapeKey = -1;

        /// <summary>
        /// Render height of a prefab view. Zero: the ArtAssetStandard.md
        /// section-1 export convention puts the origin on the ground contact
        /// plane (Y = 0), so the prefab stands on the ground unshifted.
        /// </summary>
        private const float PrefabGroundOffset = 0f;

        [Header("References")]
        [SerializeField] private MatchRunner _matchRunner;
        [SerializeField] private GameObject _unitPrefab;

        [Tooltip("Optional art-pipeline registry (ArtAssetAutoSync). A definition id with a registered PF_* prefab renders as that prefab; everything without one keeps its graybox primitive.")]
        [SerializeField] private AssetMappingRegistrySO _assetMappings;
        [SerializeField] private float _interpolationSpeed = 25f;

        [Header("Fog of War")]
        [Tooltip("Team slot whose committed Fog-of-War view is rendered. -1 follows MatchRunner.Session.LocalSlot.")]
        [SerializeField] private int _viewerTeamOverride = -1;

        [Header("Graybox Faction Colours")]
        [Tooltip("Tint for owners whose faction cannot be resolved (no economy on the runner or a slot outside the declared range).")]
        [SerializeField] private Color _unknownPlayerColor = new Color(0.62f, 0.62f, 0.62f, 1f);

        [Header("Graybox Health Readout")]
        [Tooltip("Colour the owner tint is blended toward as health drops. Dark red reads as damage without inventing a third meaning for the colour channel.")]
        [SerializeField] private Color _damagedColor = new Color(0.42f, 0.05f, 0.05f, 1f);

        [Tooltip("Smallest share of the owner colour a nearly-dead unit keeps. Above zero so the colour channel never stops identifying the owner.")]
        [Range(0f, 1f)]
        [SerializeField] private float _healthTintFloor = 0.25f;

        // Slot-indexed view table (index == EntityId.Index).
        private GameObject[] _viewInstances;
        private Renderer[] _viewRenderers;
        private EntityId[] _boundIds;
        private UnitRole[] _viewRoles;
        private int[] _viewShapeKeys;
        private GameObject[] _viewSourcePrefabs;
        private float[] _viewGroundOffsets;
        private int[] _viewOwners;
        private int[] _viewHealthSteps;
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
        // Prefab views pool per SOURCE prefab, not globally: a recycled
        // Alliance-HQ instance must never resurface as a Legion-Harvester.
        private readonly Dictionary<GameObject, Stack<GameObject>> _prefabPools = new Dictionary<GameObject, Stack<GameObject>>();
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

                // Owner and health share one tint, so they share one upload.
                // Both are compared against the cached value: an undamaged,
                // unchanged unit performs no SetPropertyBlock at all.
                int healthStep = HealthStep(in unit);
                if (_viewOwners[slot] != unit.PlayerId || _viewHealthSteps[slot] != healthStep)
                {
                    _viewOwners[slot] = unit.PlayerId;
                    _viewHealthSteps[slot] = healthStep;
                    ApplyTint(slot, unit.PlayerId, healthStep);
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
            _viewSourcePrefabs = new GameObject[capacity];
            _viewGroundOffsets = new float[capacity];
            _viewOwners = new int[capacity];
            _viewHealthSteps = new int[capacity];
            _lastSeenFrame = new int[capacity];
            _tracked = new bool[capacity];

            for (int i = 0; i < capacity; i++)
            {
                _boundIds[i] = EntityId.Invalid;
                _viewShapeKeys[i] = PrefabShapeKey;
                _viewOwners[i] = -1;
                _viewHealthSteps[i] = -1;
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
            GameObject sourcePrefab = ResolveViewPrefab(in unit);

            if (sourcePrefab != null)
            {
                shapeKey = PrefabShapeKey;
                groundOffset = PrefabGroundOffset;
                if (_prefabPools.TryGetValue(sourcePrefab, out Stack<GameObject> prefabPool) && prefabPool.Count > 0)
                {
                    instance = prefabPool.Pop();
                }
                else
                {
                    instance = Instantiate(sourcePrefab, transform);
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
            _viewSourcePrefabs[slot] = sourcePrefab;
            _viewGroundOffsets[slot] = groundOffset;
            _viewOwners[slot] = -1;       // forces a tint on this frame
            _viewHealthSteps[slot] = -1;  // ... including the health bucket of the recycled instance
            _viewObjectToSlot[instance] = slot;

            if (!_tracked[slot])
            {
                _tracked[slot] = true;
                _activeIndices.Add(slot);
            }
        }

        /// <summary>
        /// The art prefab for this entity, or null for the graybox primitive.
        /// Resolution order: the <see cref="_assetMappings"/> registry entry of
        /// the entity's own faction definition id (the same lookup combat and
        /// economy resolve through — a Legion LightTank gets the Legion prefab,
        /// never the Alliance one), then the single legacy <see cref="_unitPrefab"/>
        /// override. UnitRole.Unit (the construction site) maps to the invalid
        /// definition id 0 and therefore always falls through to the primitive.
        /// </summary>
        private GameObject ResolveViewPrefab(in UnitState unit)
        {
            if (_assetMappings != null)
            {
                EconomySystem economy = _matchRunner != null ? _matchRunner.Economy : null;
                if (economy != null && unit.PlayerId < EconomySystem.MaxPlayers)
                {
                    FactionId faction = economy.GetSlotFaction(unit.PlayerId);
                    int definitionId = SimDefinitions.ToDefinitionId(faction, unit.Role);
                    if (definitionId != 0)
                    {
                        GameObject prefab = _assetMappings.GetUnitPrefab(definitionId);
                        if (prefab == null)
                        {
                            prefab = _assetMappings.GetBuildingPrefab(definitionId);
                        }
                        if (prefab != null)
                        {
                            return prefab;
                        }
                    }
                }
            }
            return _unitPrefab;
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
                _viewSourcePrefabs[slot] = null;
                _viewOwners[slot] = -1;
                _viewHealthSteps[slot] = -1;
                return;
            }

            _viewObjectToSlot.Remove(instance);
            instance.SetActive(false);

            int shapeKey = _viewShapeKeys[slot];
            if (shapeKey == PrefabShapeKey)
            {
                GameObject sourcePrefab = _viewSourcePrefabs[slot];
                if (sourcePrefab != null)
                {
                    if (!_prefabPools.TryGetValue(sourcePrefab, out Stack<GameObject> prefabPool))
                    {
                        prefabPool = new Stack<GameObject>();
                        _prefabPools[sourcePrefab] = prefabPool;
                    }
                    prefabPool.Push(instance);
                }
                else
                {
                    // The registry entry vanished mid-match (asset re-import):
                    // destroy rather than pool under a lost key.
                    Destroy(instance);
                }
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
            _viewSourcePrefabs[slot] = null;
            _viewOwners[slot] = -1;
            _viewHealthSteps[slot] = -1;
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

        private void ApplyTint(int slot, byte playerId, int healthStep)
        {
            Renderer renderer = _viewRenderers[slot];
            if (renderer == null) return;

            Color color = TintFor(playerId, healthStep);
            _propertyBlock.Clear();
            // Built-in RP reads _Color, URP/HDRP read _BaseColor. Setting both
            // keeps the graybox tinted through the pipeline migration instead
            // of falling back to magenta / untinted white.
            FactionTint.ApplyToPropertyBlock(_propertyBlock, color);
            renderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// Owner colour, darkened toward <see cref="_damagedColor"/> by the
        /// health bucket. A full-health unit returns the owner colour
        /// unchanged, so undamaged views look exactly as they did before the
        /// readout existed; <see cref="_healthTintFloor"/> keeps a share of the
        /// owner hue at the brink so colour never stops answering "whose is
        /// it?".
        /// </summary>
        private Color TintFor(byte playerId, int healthStep)
        {
            Color owner = ColorForOwner(playerId);
            if (healthStep >= HealthTintSteps) return owner;

            float fraction = (float)healthStep / HealthTintSteps;
            float blend = Mathf.Lerp(Mathf.Clamp01(_healthTintFloor), 1f, fraction);
            return Color.Lerp(_damagedColor, owner, blend);
        }

        /// <summary>
        /// Health fraction quantised into <see cref="HealthTintSteps"/> buckets
        /// (0 = destroyed, <see cref="HealthTintSteps"/> = untouched). The
        /// division rounds UP, so a unit surviving on a single hit point still
        /// reports bucket 1 and never renders as the fully-damaged colour that
        /// would read as "already dead".
        /// </summary>
        private static int HealthStep(in UnitState unit)
        {
            // A definition-less or not-yet-initialised entity reads as healthy
            // rather than as a corpse.
            if (unit.MaxHealth <= 0 || unit.CurrentHealth >= unit.MaxHealth) return HealthTintSteps;
            if (unit.CurrentHealth <= 0) return 0;

            int step = (unit.CurrentHealth * HealthTintSteps + unit.MaxHealth - 1) / unit.MaxHealth;
            return step < 1 ? 1 : step;
        }

        /// <summary>
        /// Owner colour: the FACTION of the owning slot, read from the
        /// economy state (the single authoritative faction source — the same
        /// lookup combat and economy resolve through). Unresolvable owners
        /// (no economy wired, slot outside the declared range) fall back to
        /// <see cref="_unknownPlayerColor"/> instead of throwing.
        /// </summary>
        private Color ColorForOwner(byte playerId)
        {
            EconomySystem economy = _matchRunner != null ? _matchRunner.Economy : null;
            if (economy != null && playerId < EconomySystem.MaxPlayers)
            {
                return FactionTint.BaseColor(economy.GetSlotFaction(playerId));
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

            foreach (KeyValuePair<GameObject, Stack<GameObject>> entry in _prefabPools)
            {
                Stack<GameObject> pool = entry.Value;
                while (pool.Count > 0)
                {
                    GameObject pooled = pool.Pop();
                    if (pooled != null) Destroy(pooled);
                }
            }
            _prefabPools.Clear();
        }
    }
}
