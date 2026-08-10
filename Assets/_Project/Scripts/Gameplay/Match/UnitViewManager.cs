using System;
using System.Collections.Generic;
using UnityEngine;
using Nova.Core;
using Nova.Data;
using Nova.Gameplay.CombatFeedback;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Combat;
using Nova.Simulation.Construction;
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
    /// entity and world picking cannot leak a target id. Fog/ambiguous losses
    /// return within the frame; a safely confirmed death is detached from
    /// picking and held for its 0.8-second presentation before exact pool
    /// return.
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

        /// <summary>
        /// Uniform scale factor that shrinks a prefab's horizontal extent to
        /// its simulation footprint when the measured bounds come out larger
        /// than the footprint. Art is authored at the ArtAssetStandard.md
        /// convention (1 grid cell = 3 m) while the simulation world runs 1
        /// cell = 1 world unit, so the raw PF_* meshes are ~3x too large and
        /// would overlap (GB-005 finding). Normalizing here — at the view
        /// boundary, from the mesh's own bounds — keeps every future drop-in
        /// model swappable without touching prefabs or game logic. Art that
        /// arrives SMALLER than its footprint keeps its authored scale
        /// (factor 1): upscaling a detailed miniature reads worse than a
        /// slightly small model, and the authored look is preserved.
        /// </summary>
        private readonly Dictionary<GameObject, float> _prefabScaleFactors = new Dictionary<GameObject, float>();

        [Header("References")]
        [SerializeField] private MatchRunner _matchRunner;
        [SerializeField] private GameObject _unitPrefab;

        [Tooltip("Presentation-only combat effects. Added lazily for older generated scenes that predate Sprint 12B.")]
        [SerializeField] private CombatEffectController _combatEffects;

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
        private readonly List<VisibleCombatSample> _combatSamples = new List<VisibleCombatSample>(256);
        private readonly List<CombatFeedbackEvent> _combatEvents = new List<CombatFeedbackEvent>(64);
        private readonly VisibleCombatFrameDiffer _combatDiffer = new VisibleCombatFrameDiffer();
        // Keyed by the view GameObject itself, NOT by GetInstanceID(): Unity 6
        // marks Object.GetInstanceID() obsolete-as-error (CS0619). Object
        // overrides Equals/GetHashCode by instance id, so the reference is a
        // valid dictionary key and the lookup semantics are unchanged.
        private readonly Dictionary<GameObject, int> _viewObjectToSlot = new Dictionary<GameObject, int>(256);
        private readonly Stack<GameObject>[] _shapePools = new Stack<GameObject>[ShapePoolCount];
        // Prefab views pool per SOURCE prefab, not globally: a recycled
        // Alliance-HQ instance must never resurface as a Legion-Harvester.
        private readonly Dictionary<GameObject, Stack<GameObject>> _prefabPools = new Dictionary<GameObject, Stack<GameObject>>();
        private readonly List<DeathViewHold> _deathHolds = new List<DeathViewHold>(16);
        private readonly Dictionary<Material, Material> _transparentMaterialCache = new Dictionary<Material, Material>();
        private MaterialPropertyBlock _propertyBlock;
        // Scratch for the all-renderers tint upload (no per-call allocation).
        private readonly List<Renderer> _tintScratch = new List<Renderer>(8);
        // Shared runtime material for graybox primitives: Unity's primitive
        // default material is a built-in-RP resource and renders magenta
        // under URP (GB-004 finding), so primitives carry this URP Lit
        // instance and keep the per-instance faction tint on the property
        // block. Lazy, never saved, destroyed with the component.
        private static Material _primitiveMaterial;

        private static Material PrimitiveMaterial
        {
            get
            {
                if (_primitiveMaterial == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null) shader = Shader.Find("Standard");
                    _primitiveMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                }
                return _primitiveMaterial;
            }
        }
        private int _frameStamp;
        private bool _fogUnavailableLogged;

        /// <summary>Number of entities that currently own a live view (i.e. are visible to the viewer team).</summary>
        public int VisibleViewCount => _activeIndices.Count;

        /// <summary>Views currently held outside the slot table for the 0.8-second death presentation.</summary>
        public int ActiveDeathHoldCount => _deathHolds.Count;

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
            EnsureCombatEffects();
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
            ResetCombatFeedback();
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
            AdvanceDeathHolds(Time.deltaTime);
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
                ResetCombatFeedback();
                return;
            }

            EnsureBuffers();

            byte viewerTeam = ResolveViewerTeam(fog);
            _frameStamp++;

            // 1) The committed team view is the ONLY source of renderable
            //    entities (own units included, foreign units only in Visible
            //    cells). Nothing here touches EntityManager.RawUnits.
            //    Lab exception: FogRevealDebug swaps the feed for the raw
            //    store so the opponent AI can be watched playing. The fog
            //    system itself is untouched either way (see its remarks).
            _visibleScratch.Clear();
            _combatSamples.Clear();
            if (FogRevealDebug.RevealAll)
            {
                FogRevealDebug.CollectAllActive(entities, _visibleScratch);
            }
            else
            {
                fog.GetVisibleEntities(viewerTeam, _visibleScratch);
            }

            // Build the complete fog-safe snapshot before touching a view.
            // A dead slot can be recycled by the simulation in the same tick;
            // diffing first lets BeginDeathHold detach the old object before
            // the replacement is allowed to acquire from that pool.
            for (int i = 0; i < _visibleScratch.Count; i++)
            {
                EntityId id = _visibleScratch[i];
                if (!entities.TryGetUnit(id, out UnitState unit)) continue;
                int slot = id.Index;
                if (slot < 0 || slot >= _viewInstances.Length) continue;

                WeaponProfile weapon = ResolveWeaponProfile(in unit);
                _combatSamples.Add(new VisibleCombatSample(
                    unit.Id,
                    unit.PlayerId,
                    unit.Role,
                    CombatSamplePosition(slot, in unit),
                    unit.CurrentHealth,
                    unit.WeaponCooldownTicks,
                    unit.AttackTarget,
                    weapon.DamageType));
            }

            // The differ runs before the visibility sweep: a confirmed death
            // can detach the old view from its slot while the exact pool key is
            // still available. Fog/despawn losses never enter a death hold.
            _combatEvents.Clear();
            _combatDiffer.Observe(
                _matchRunner.Kernel.CurrentTick.Value,
                viewerTeam,
                _combatSamples,
                _combatEvents);
            EnsureCombatEffects();
            for (int i = 0; i < _combatEvents.Count; i++)
            {
                CombatFeedbackEvent feedback = _combatEvents[i];
                if (feedback.Kind == CombatFeedbackKind.Death)
                {
                    BeginDeathHold(feedback.TargetId);
                }
            }
            _combatEffects?.Present(_combatEvents);

            float blend = 1f - Mathf.Exp(-Mathf.Max(0f, _interpolationSpeed) * Time.deltaTime);
            for (int i = 0; i < _visibleScratch.Count; i++)
            {
                EntityId id = _visibleScratch[i];
                if (!entities.TryGetUnit(id, out UnitState unit)) continue;

                int slot = id.Index;
                if (slot < 0 || slot >= _viewInstances.Length) continue;

                // A rebind is required for a recycled slot (new version) and
                // when the EFFECTIVE view role changed in place: a site
                // carries its definition role since 16.3 (#44), so the
                // site-register flip at completion (not a role change) is
                // what promotes the view from the site pad to the finished
                // building look.
                bool spawned = false;
                if (_viewInstances[slot] == null || _boundIds[slot] != id || _viewRoles[slot] != EffectiveViewRole(in unit))
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

            // 2) Everything not visible now is either already detached into a
            //    confirmed death hold, or is an ambiguous despawn/fog loss
            //    that returns immediately. Neither path leaves a pickable
            //    proxy behind.
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

        private Vector3 CombatSamplePosition(int slot, in UnitState unit)
        {
            if (_viewInstances[slot] != null && _boundIds[slot] == unit.Id)
            {
                return _viewInstances[slot].transform.position;
            }

            // New rows have no view yet. This copied fallback is used for the
            // 2D UnitReady cue and as a safe baseline only; shots cannot occur
            // until a subsequent observation has a bound view.
            return new Vector3(
                unit.Transform.PositionX.ToFloat(),
                SimDefinitions.IsBuildingRole(unit.Role) ? 0.5f : 0.4f,
                unit.Transform.PositionY.ToFloat());
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
            if (_combatSamples.Capacity < capacity) _combatSamples.Capacity = capacity;

            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
            _combatDiffer.Reset(capacity);
        }

        /// <summary>
        /// The role every shape decision is made with (16.3, #44): an
        /// unfinished site carries its definition role in the simulation now,
        /// but it must KEEP the site look — the low generic pad, no art
        /// prefab — until completion. Sites therefore map back to
        /// <see cref="UnitRole.Unit"/> here; one read drives the rebind
        /// trigger, the prefab lookup and the primitive table alike, so the
        /// completion flip (site register, not role) rebinds the view to the
        /// finished building. <see cref="_viewRoles"/> stores this effective
        /// role, which is also what the building-rotation lock reads.
        /// </summary>
        private UnitRole EffectiveViewRole(in UnitState unit)
        {
            ConstructionSystem construction = _matchRunner != null ? _matchRunner.Construction : null;
            if (construction != null && SimDefinitions.IsBuildingRole(unit.Role) && construction.IsActiveSite(unit.Id))
            {
                return UnitRole.Unit;
            }
            return unit.Role;
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
                    // The factor was measured and cached when the first
                    // instance of this source prefab was created below.
                    instance.transform.localScale = Vector3.one * _prefabScaleFactors[sourcePrefab];
                }
                else
                {
                    instance = Instantiate(sourcePrefab, transform);
                    instance.transform.localScale = Vector3.one * NormalizePrefabScale(sourcePrefab, instance, in unit);
                }
                instance.SetActive(true);
            }
            else
            {
                GetRoleShape(EffectiveViewRole(in unit), out PrimitiveType primitive, out Vector3 scale);
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
                    instance.GetComponent<Renderer>().sharedMaterial = PrimitiveMaterial;
                }
                instance.transform.localScale = scale;
                // The primitive pools are shared across roles (one cube pool
                // serves vehicles AND buildings), and building views never
                // receive rotation writes again (see ApplyTransform), so a
                // recycled view must not inherit the previous tenant's
                // heading. Unit views get their rotation from this frame's
                // ApplyTransform anyway, so the reset is free for them.
                instance.transform.rotation = Quaternion.identity;
            }

#if UNITY_EDITOR
            // Editor-only hierarchy readability; the string allocation never
            // ships in a player build.
            instance.name = $"UnitView_{unit.Id.Index}.{unit.Id.Version}_{unit.Role}";
#endif

            _viewInstances[slot] = instance;
            _viewRenderers[slot] = instance.GetComponentInChildren<Renderer>(true);
            _boundIds[slot] = unit.Id;
            _viewRoles[slot] = EffectiveViewRole(in unit);
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
        /// override. The effective view role decides (16.3, #44): a site maps
        /// back to <see cref="UnitRole.Unit"/>, which resolves to the invalid
        /// definition id 0. Active sites also bypass the optional legacy unit
        /// fallback, so they always use the graybox site primitive and never
        /// the finished building's art.
        /// </summary>
        private GameObject ResolveViewPrefab(in UnitState unit)
        {
            if (_assetMappings != null)
            {
                EconomySystem economy = _matchRunner != null ? _matchRunner.Economy : null;
                if (economy != null && unit.PlayerId < EconomySystem.MaxPlayers)
                {
                    FactionId faction = economy.GetSlotFaction(unit.PlayerId);
                    int definitionId = SimDefinitions.ToDefinitionId(faction, EffectiveViewRole(in unit));
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
            ConstructionSystem construction = _matchRunner != null ? _matchRunner.Construction : null;
            if (construction != null && construction.IsActiveSite(unit.Id))
            {
                return null;
            }
            return _unitPrefab;
        }

        /// <summary>
        /// Measures the freshly instantiated prefab view and caches the
        /// uniform scale factor that brings its horizontal extent down to the
        /// simulation footprint of its role (see the field remarks on
        /// <see cref="_prefabScaleFactors"/>). The instance must be unscaled
        /// (fresh <c>Instantiate</c>) and parented under this transform —
        /// world bounds and the local scale applied afterwards share the same
        /// parent, so the ratio stays exact. Meshes without renderers, or art
        /// already smaller than its footprint, keep factor 1.
        /// </summary>
        private float NormalizePrefabScale(GameObject sourcePrefab, GameObject instance, in UnitState unit)
        {
            float factor = 1f;

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                float target = TargetViewSize(EffectiveViewRole(in unit));
                float current = Mathf.Max(bounds.size.x, bounds.size.z);
                if (current > target && current > 1e-4f)
                {
                    factor = target / current;
                }
            }

            _prefabScaleFactors[sourcePrefab] = factor;
            return factor;
        }

        /// <summary>
        /// Horizontal world size a view of this role should occupy. Buildings
        /// fill their 3x3-cell footprint (<see cref="SimDefinitions.BuildingFootprintCells"/>,
        /// 1 cell = 1 world unit); units reuse the graybox shape table's
        /// larger horizontal extent so art and primitive fallback read at the
        /// same size for the same role.
        /// </summary>
        private static float TargetViewSize(UnitRole role)
        {
            if (SimDefinitions.IsBuildingRole(role))
            {
                return SimDefinitions.BuildingFootprintCells;
            }

            GetRoleShape(role, out _, out Vector3 primitiveScale);
            return Mathf.Max(primitiveScale.x, primitiveScale.z);
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
            int shapeKey = _viewShapeKeys[slot];
            GameObject sourcePrefab = _viewSourcePrefabs[slot];
            ReturnInstanceToPool(instance, shapeKey, sourcePrefab);

            _viewInstances[slot] = null;
            _viewRenderers[slot] = null;
            _boundIds[slot] = EntityId.Invalid;
            _viewSourcePrefabs[slot] = null;
            _viewOwners[slot] = -1;
            _viewHealthSteps[slot] = -1;
        }

        /// <summary>
        /// Detaches a confirmed corpse from the slot table immediately while
        /// retaining the exact primitive/prefab pool identity until its short
        /// death presentation completes. The slot is therefore free for a new
        /// EntityId version without ever reusing the corpse object.
        /// </summary>
        private bool BeginDeathHold(EntityId id)
        {
            if (_viewInstances == null || !id.IsValid) return false;
            int slot = id.Index;
            if (slot < 0 || slot >= _viewInstances.Length) return false;
            if (_boundIds[slot] != id || _viewInstances[slot] == null) return false;

            GameObject instance = _viewInstances[slot];
            _viewObjectToSlot.Remove(instance);

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(includeInactive: true);
            Collider[] colliders = instance.GetComponentsInChildren<Collider>(includeInactive: true);
            var originalMaterials = new Material[renderers.Length][];
            var originalColors = new Color[renderers.Length];
            var colliderStates = new bool[colliders.Length];

            for (int i = 0; i < colliders.Length; i++)
            {
                colliderStates[i] = colliders[i] != null && colliders[i].enabled;
                if (colliders[i] != null) colliders[i].enabled = false;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;

                originalMaterials[i] = renderer.sharedMaterials;
                var transparent = new Material[originalMaterials[i].Length];
                for (int m = 0; m < transparent.Length; m++)
                {
                    transparent[m] = TransparentVariant(originalMaterials[i][m]);
                }
                renderer.sharedMaterials = transparent;

                _propertyBlock.Clear();
                renderer.GetPropertyBlock(_propertyBlock);
                Color color = _propertyBlock.GetColor("_BaseColor");
                if (color == default) color = _propertyBlock.GetColor("_Color");
                if (color == default) color = Color.white;
                originalColors[i] = color;
            }

            _deathHolds.Add(new DeathViewHold(
                instance,
                _viewShapeKeys[slot],
                _viewSourcePrefabs[slot],
                instance.transform.position,
                renderers,
                originalMaterials,
                originalColors,
                colliders,
                colliderStates,
                SimDefinitions.IsBuildingRole(_viewRoles[slot])));

            // Detach only. The active-slot sweep owns _tracked/_activeIndices
            // and removes this slot later in the same LateUpdate.
            _viewInstances[slot] = null;
            _viewRenderers[slot] = null;
            _boundIds[slot] = EntityId.Invalid;
            _viewSourcePrefabs[slot] = null;
            _viewOwners[slot] = -1;
            _viewHealthSteps[slot] = -1;
            return true;
        }

        private void AdvanceDeathHolds(float deltaTime)
        {
            if (_deathHolds.Count == 0) return;
            float dt = Mathf.Max(0f, deltaTime);
            for (int i = _deathHolds.Count - 1; i >= 0; i--)
            {
                DeathViewHold hold = _deathHolds[i];
                hold.Elapsed += dt;
                float t = Mathf.Clamp01(hold.Elapsed / DeathViewHold.DurationSeconds);
                float eased = t * t * (3f - 2f * t);
                float sink = hold.IsBuilding ? 0.65f : 0.45f;
                hold.Instance.transform.position = hold.StartPosition + Vector3.down * (sink * eased);

                for (int r = 0; r < hold.Renderers.Length; r++)
                {
                    Renderer renderer = hold.Renderers[r];
                    if (renderer == null) continue;
                    Color color = hold.OriginalColors[r];
                    color.a *= 1f - eased;
                    _propertyBlock.Clear();
                    FactionTint.ApplyToPropertyBlock(_propertyBlock, color);
                    renderer.SetPropertyBlock(_propertyBlock);
                }

                if (t >= 1f) CompleteDeathHoldAt(i);
            }
        }

        private void CompleteDeathHoldAt(int index)
        {
            DeathViewHold hold = _deathHolds[index];
            _deathHolds.RemoveAt(index);

            for (int r = 0; r < hold.Renderers.Length; r++)
            {
                Renderer renderer = hold.Renderers[r];
                if (renderer == null) continue;
                renderer.sharedMaterials = hold.OriginalMaterials[r] ?? Array.Empty<Material>();
                renderer.SetPropertyBlock(null);
            }
            for (int c = 0; c < hold.Colliders.Length; c++)
            {
                if (hold.Colliders[c] != null) hold.Colliders[c].enabled = hold.ColliderStates[c];
            }
            hold.Instance.transform.position = hold.StartPosition;
            ReturnInstanceToPool(hold.Instance, hold.ShapeKey, hold.SourcePrefab);
        }

        private void CompleteAllDeathHolds()
        {
            for (int i = _deathHolds.Count - 1; i >= 0; i--)
            {
                CompleteDeathHoldAt(i);
            }
        }

        private void ReturnInstanceToPool(GameObject instance, int shapeKey, GameObject sourcePrefab)
        {
            if (instance == null) return;
            instance.SetActive(false);

            if (shapeKey == PrefabShapeKey)
            {
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
                return;
            }

            Stack<GameObject> pool = _shapePools[shapeKey];
            if (pool == null)
            {
                pool = new Stack<GameObject>();
                _shapePools[shapeKey] = pool;
            }
            pool.Push(instance);
        }

        private Material TransparentVariant(Material source)
        {
            if (source == null) return null;
            if (_transparentMaterialCache.TryGetValue(source, out Material cached) && cached != null)
            {
                return cached;
            }

            var material = new Material(source)
            {
                name = source.name + "_DeathFade",
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
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            _transparentMaterialCache[source] = material;
            return material;
        }

        private WeaponProfile ResolveWeaponProfile(in UnitState unit)
        {
            EconomySystem economy = _matchRunner != null ? _matchRunner.Economy : null;
            if (economy == null || unit.PlayerId >= EconomySystem.MaxPlayers)
            {
                return WeaponProfiles.Fallback;
            }
            return WeaponProfiles.Get(economy.GetSlotFaction(unit.PlayerId), unit.Role);
        }

        private void EnsureCombatEffects()
        {
            if (_combatEffects != null) return;
            _combatEffects = GetComponent<CombatEffectController>();
            if (_combatEffects == null) _combatEffects = gameObject.AddComponent<CombatEffectController>();
        }

        private void ResetCombatFeedback()
        {
            _combatDiffer.Reset(_viewInstances != null ? _viewInstances.Length : 0);
            _combatSamples.Clear();
            _combatEvents.Clear();
            CompleteAllDeathHolds();
            _combatEffects?.ResetEffects();
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

            // Buildings never rotate: nothing in the simulation orients them,
            // and a building view that mirrors the (unit-shaped) rotation
            // state visibly spins when the state mutates for other reasons.
            // Building views keep the rotation they were acquired with and
            // receive position writes only.
            bool isBuilding = SimDefinitions.IsBuildingRole(_viewRoles[slot]);

            if (blend >= 1f)
            {
                viewTransform.position = targetPos;
                if (!isBuilding)
                {
                    viewTransform.rotation = Quaternion.Euler(0f, unit.Transform.Rotation.ToDegrees().ToFloat(), 0f);
                }
                return;
            }

            // Frame-rate independent exponential smoothing between the 10-Hz
            // authoritative positions (blend is derived from _interpolationSpeed).
            viewTransform.position = Vector3.Lerp(viewTransform.position, targetPos, blend);
            if (!isBuilding)
            {
                Quaternion targetRot = Quaternion.Euler(0f, unit.Transform.Rotation.ToDegrees().ToFloat(), 0f);
                viewTransform.rotation = Quaternion.Slerp(viewTransform.rotation, targetRot, blend);
            }
        }

        private void ApplyTint(int slot, byte playerId, int healthStep)
        {
            GameObject instance = _viewInstances[slot];
            if (instance == null) return;

            Color color = TintFor(playerId, healthStep);
            _propertyBlock.Clear();
            // Built-in RP reads _Color, URP/HDRP read _BaseColor. Setting both
            // keeps the graybox tinted through the pipeline migration instead
            // of falling back to magenta / untinted white.
            FactionTint.ApplyToPropertyBlock(_propertyBlock, color);

            // EVERY renderer of the view gets the block: a prefab's LODGroup
            // switches between its _LOD0/1/2 renderers with camera distance,
            // and tinting only the first would drop the faction colour
            // exactly when the player zooms out.
            instance.GetComponentsInChildren(includeInactive: true, _tintScratch);
            for (int i = 0; i < _tintScratch.Count; i++)
            {
                if (_tintScratch[i] != null)
                {
                    _tintScratch[i].SetPropertyBlock(_propertyBlock);
                }
            }
            _tintScratch.Clear();
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
                // Generic entity — and the unfinished construction site,
                // which EffectiveViewRole maps back here until completion
                // (16.3): a low ground pad.
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

        /// <summary>
        /// Drops every live view AND forgets every binding, for a match
        /// restart (MatchBootstrap.RestartMatch rebuilds the kernel and the
        /// entity store). The per-slot tables are re-sized only when the
        /// capacity changes, so without this the new match's entity ids could
        /// collide with stale bindings of the old one and inherit its views.
        /// </summary>
        public void ResetViews()
        {
            ReleaseAllViews();
            ResetCombatFeedback();
            if (_viewInstances == null) return;
            for (int i = 0; i < _viewInstances.Length; i++)
            {
                _boundIds[i] = EntityId.Invalid;
                _viewShapeKeys[i] = PrefabShapeKey;
                _viewOwners[i] = -1;
                _viewHealthSteps[i] = -1;
                _lastSeenFrame[i] = 0;
                _tracked[i] = false;
            }
            _frameStamp = 0;
        }

        private void ReleaseAllViews()
        {
            CompleteAllDeathHolds();
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
            _combatEffects?.ResetEffects();

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

            foreach (Material material in _transparentMaterialCache.Values)
            {
                if (material != null) Destroy(material);
            }
            _transparentMaterialCache.Clear();

            if (_primitiveMaterial != null)
            {
                Destroy(_primitiveMaterial);
                _primitiveMaterial = null;
            }
        }

        private sealed class DeathViewHold
        {
            public const float DurationSeconds = 0.8f;

            public GameObject Instance { get; }
            public int ShapeKey { get; }
            public GameObject SourcePrefab { get; }
            public Vector3 StartPosition { get; }
            public Renderer[] Renderers { get; }
            public Material[][] OriginalMaterials { get; }
            public Color[] OriginalColors { get; }
            public Collider[] Colliders { get; }
            public bool[] ColliderStates { get; }
            public bool IsBuilding { get; }
            public float Elapsed;

            public DeathViewHold(
                GameObject instance,
                int shapeKey,
                GameObject sourcePrefab,
                Vector3 startPosition,
                Renderer[] renderers,
                Material[][] originalMaterials,
                Color[] originalColors,
                Collider[] colliders,
                bool[] colliderStates,
                bool isBuilding)
            {
                Instance = instance;
                ShapeKey = shapeKey;
                SourcePrefab = sourcePrefab;
                StartPosition = startPosition;
                Renderers = renderers;
                OriginalMaterials = originalMaterials;
                OriginalColors = originalColors;
                Colliders = colliders;
                ColliderStates = colliderStates;
                IsBuilding = isBuilding;
            }
        }
    }
}
