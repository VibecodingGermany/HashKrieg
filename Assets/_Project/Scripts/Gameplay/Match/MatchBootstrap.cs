using System;
using UnityEngine;
using Nova.Core;
using Nova.Simulation.Definitions;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;
// Unity 6 introduced UnityEngine.EntityId, so the bare name is ambiguous in
// any file that opens both Nova.Core and UnityEngine (CS0104). The alias pins
// it to the simulation handle, matching UnitViewManager and RtsDeviceInput.
using EntityId = Nova.Core.EntityId;

namespace Nova.Gameplay.Match
{
    /// <summary>
    /// GRAYBOX MATCH SETUP — the component that actually starts a playable
    /// match. It sits next to <see cref="MatchRunner"/> on the same
    /// GameObject, drives <see cref="MatchRunner.InitializeMatch"/> +
    /// <see cref="MatchRunner.StartMatch"/> and applies the canonical opening
    /// position.
    /// <para>
    /// The canonical opening (D-077 — the classic loop start,
    /// quality/content/mvp-v1.json startStatePerPlayer): per slot ONLY a
    /// completed HQ, ONE Builder and 3.000 AE starting credits
    /// (EconomySystem.CanonicalMatchStartingCreditsAE, plumbed in by
    /// <see cref="MatchRunner.InitializeMatch"/>'s default) —
    /// plus one Aetherium field per slot. The Refinery is NO longer
    /// pre-placed: the player builds it (it has no Power-plant prerequisite
    /// since D-077), and the completed Refinery — not the HQ — produces the
    /// Harvesters.
    /// </para>
    /// <para>
    /// It MIRRORS <c>Determinism10000Scenario.SetupMatch</c>
    /// (tools/Nova.SimRunner/Determinism10000Scenario.cs): identical seed,
    /// identical map size, identical entity capacity, identical per-slot
    /// layout AND identical spawn ORDER — field, HQ, Builder; slot 0 first,
    /// then slot 1.
    /// The order is load-bearing: the <see cref="EntityManager"/> hands out
    /// entity ids from a deterministic free list, so any reordering shifts
    /// every id and therefore every state hash. An EditMode test asserts that
    /// this bootstrap and <c>SetupMatch</c> produce the same
    /// <c>MatchFingerprint.InitialStateHash</c> (written by another agent).
    /// </para>
    /// <para>
    /// ONE DELIBERATE DIVERGENCE, behind <see cref="UseDefinitionStats"/>:
    /// <c>SetupMatch</c> spawns through <see cref="EntityManager.SpawnUnit"/>
    /// defaults, which stamps maxHealth 100 on EVERY unit and ignores
    /// <see cref="SimDefinitions"/>. This bootstrap routes spawning through
    /// the slot faction's <see cref="SimUnitDefinition"/> so units carry
    /// their real stats. Move speeds are identical either way (the
    /// definition table is deliberately speed-flat across factions and the
    /// scenario literals agree exactly); the ONLY value that differs is
    /// maxHealth (e.g. the Alliance Builder's 350 vs. the default 100),
    /// and maxHealth IS part of the hashed entity-store block. Set
    /// <see cref="UseDefinitionStats"/> to false to get a byte-exact
    /// <c>SetupMatch</c> mirror — that is the switch the InitialStateHash
    /// test must flip.
    /// </para>
    /// <para>
    /// Command law (docs/tech/Commands.md, sprint hard rule 2): the initial
    /// spawn/placement is the one allowed direct write into simulation state.
    /// The setup submits NO opening commands — the first command of the
    /// D-077 opening is the player's Refinery placement, and it enters
    /// through <see cref="MatchRunner.Ingress"/> like every later order.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MatchRunner))]
    public sealed class MatchBootstrap : MonoBehaviour
    {
        /// <summary>Local (human) slot. MUST be 0 — <see cref="MatchSession"/> binds local slot 0, and the executor rejects orders for entities owned by any other slot as RejectedNotOwned.</summary>
        public const byte LocalSlot = 0;

        /// <summary>The scripted opponent slot of the canonical two-slot match.</summary>
        public const byte EnemySlot = 1;

        /// <summary>Canonical scenario seed (DeterminismOptions.Seed). Required for InitialStateHash parity: the PRNG words are hashed into the kernel state block.</summary>
        public const ulong CanonicalSeed = 0xDE7E000000010271UL;

        /// <summary>Aetherium reserve per field, in AE (Determinism10000Scenario.FieldReserveAE).</summary>
        private const long FieldReserveAE = 2000000L;

        /// <summary>maxHealth stamped by SpawnUnit when no definition stats are applied.</summary>
        private const int SpawnDefaultMaxHealth = 100;

        // Definition ids are faction-resolved (SimDefinitions id rule:
        // Alliance = role wire value, Legion = role + 17) and come from the
        // slot's faction at spawn time — no per-faction literal constants.

        [Header("Match")]
        [Tooltip("Start the match automatically in Start(). The scene generator wires this.")]
        [SerializeField] public bool AutoStart = true;

        [Tooltip("Simulation seed. Keep the canonical value for InitialStateHash parity with DETERMINISM_10000.")]
        [SerializeField] private ulong _seed = CanonicalSeed;

        [Tooltip("Map size in cells. The canonical match is 128x128.")]
        [SerializeField] private ushort _mapWidth = 128;
        [SerializeField] private ushort _mapHeight = 128;

        [Tooltip("Entity store capacity. 1024 = the manifest's capacity.entityStoreCap, NOT MatchRunner's 2048 default.")]
        [SerializeField] private int _entityCapacity = 1024;

        [Tooltip("Spawn units with SimDefinitions stats (real maxHealth). Turn OFF for a byte-exact Determinism10000Scenario.SetupMatch mirror.")]
        [SerializeField] public bool UseDefinitionStats = true;

        private readonly EntityId[] _hq = new EntityId[2];
        private readonly EntityId[] _builder = new EntityId[2];

        /// <summary>The runner this bootstrap drives (resolved from the same GameObject).</summary>
        public MatchRunner Runner { get; private set; }

        /// <summary>True once the opening position exists and the kernel is stepping.</summary>
        public bool IsMatchReady { get; private set; }

        /// <summary>Seed the match was started with.</summary>
        public ulong Seed => _seed;

        /// <summary>Map size in cells (128x128 for the canonical match).</summary>
        public Vector2Int MapSize => new Vector2Int(_mapWidth, _mapHeight);

        /// <summary>Footprint edge length of every MS-1 building, in cells.</summary>
        public int BuildingFootprintCells => SimDefinitions.BuildingFootprintCells;

        // --- HUD-facing read-only geometry -------------------------------

        public ushort LocalFieldId => LocalLayout.FieldId;
        public ushort EnemyFieldId => EnemyLayout.FieldId;

        /// <summary>Aetherium field cell of the human player (7, 7).</summary>
        public Vector2Int LocalFieldCell => new Vector2Int(LocalLayout.FieldX, LocalLayout.FieldY);

        /// <summary>Aetherium field cell of the opponent (119, 119).</summary>
        public Vector2Int EnemyFieldCell => new Vector2Int(EnemyLayout.FieldX, EnemyLayout.FieldY);

        /// <summary>Lower-left footprint origin of the human HQ (4, 4).</summary>
        public Vector2Int LocalHqOrigin => new Vector2Int(LocalLayout.HqOriginX, LocalLayout.HqOriginY);

        /// <summary>Lower-left footprint origin of the opponent HQ (120, 120).</summary>
        public Vector2Int EnemyHqOrigin => new Vector2Int(EnemyLayout.HqOriginX, EnemyLayout.HqOriginY);

        /// <summary>Center cell of the human HQ footprint — the natural camera start focus.</summary>
        public Vector2Int LocalHqCenterCell => LocalHqOrigin + Vector2Int.one;

        /// <summary>Center cell of the opponent HQ footprint.</summary>
        public Vector2Int EnemyHqCenterCell => EnemyHqOrigin + Vector2Int.one;

        /// <summary>Entity handle of the human HQ (invalid until the match is set up).</summary>
        public EntityId LocalHq => _hq[LocalSlot];

        /// <summary>Entity handle of the opponent HQ.</summary>
        public EntityId EnemyHq => _hq[EnemySlot];

        /// <summary>Entity handle of the human Builder (spawned at 13, 7 — the D-077 opening's only unit).</summary>
        public EntityId LocalBuilder => _builder[LocalSlot];

        private void Start()
        {
            if (AutoStart)
            {
                StartGrayboxMatch();
            }
        }

        /// <summary>
        /// Initializes and starts the runner and builds the canonical D-077
        /// opening position for both slots (field, completed HQ, one
        /// Builder). Submits no commands — the loop start is the player's
        /// move. Idempotent: a second call is a no-op.
        /// </summary>
        public void StartGrayboxMatch()
        {
            if (IsMatchReady)
            {
                return;
            }

            Runner = GetComponent<MatchRunner>();
            if (Runner == null)
            {
                Debug.LogError("[MatchBootstrap] No MatchRunner on this GameObject — cannot start a match.");
                return;
            }

            Runner.InitializeMatch(_seed, _mapWidth, _mapHeight, _entityCapacity);

            // Faction assignment (economy block v2): slot 0 plays Alliance,
            // slot 1 plays Legion. Set BEFORE StartMatch — the SetSlotFaction
            // guard forbids any change once the kernel runs, because the
            // faction bytes are part of the hashed initial state and the
            // match fingerprint. The scenario's BuildHost does the same, in
            // the same order.
            Runner.Economy.SetSlotFaction(LocalSlot, FactionId.Alliance);
            Runner.Economy.SetSlotFaction(EnemySlot, FactionId.Legion);

            Runner.StartMatch();

            // Slot order is load-bearing for entity ids: slot 0 first.
            SetupSlot(LocalLayout);
            SetupSlot(EnemyLayout);
            IsMatchReady = true;

            Debug.Log(
                $"[MatchBootstrap] Graybox match started (seed 0x{_seed:X16}, {_mapWidth}x{_mapHeight}, " +
                $"capacity {_entityCapacity}, definition stats {UseDefinitionStats}, " +
                $"start credits {Runner.Economy.GetPlayerEconomy(LocalSlot).AetheriumCredits} AE).");
        }

        /// <summary>
        /// One slot's D-077 start state: an Aetherium field, a completed HQ
        /// and one Builder near it — nothing else. Spawn order mirrors
        /// SetupMatch exactly (field, HQ, Builder).
        /// </summary>
        private void SetupSlot(SlotLayout c)
        {
            if (!Runner.Economy.TryAddField(c.FieldId, new GridPos2D(c.FieldX, c.FieldY), FieldReserveAE))
            {
                throw new InvalidOperationException($"[MatchBootstrap] field {c.FieldId} could not be registered");
            }

            FactionId faction = Runner.Economy.GetSlotFaction(c.Slot);
            ushort hqDefId = SimDefinitions.ToDefinitionId(faction, UnitRole.HQ);

            _hq[c.Slot] = Runner.Construction.PlaceCompletedBuilding(c.Slot, hqDefId, c.HqOriginX, c.HqOriginY);
            if (!_hq[c.Slot].IsValid)
            {
                throw new InvalidOperationException($"[MatchBootstrap] HQ placement failed for slot {c.Slot}");
            }

            _builder[c.Slot] = Spawn(c.Slot, UnitRole.Builder, c.BuilderX, c.BuilderY);
        }

        /// <summary>
        /// Spawns one unit from its faction's canonical definition. Role and
        /// move speed always come from <see cref="SimDefinitions"/>; maxHealth
        /// only when <see cref="UseDefinitionStats"/> is set — see the class
        /// remarks. The faction is the slot's, read from the economy state.
        /// </summary>
        private EntityId Spawn(byte slot, UnitRole role, int cellX, int cellY)
        {
            FactionId faction = Runner.Economy.GetSlotFaction(slot);
            if (!SimDefinitions.TryGetUnit(faction, role, out SimUnitDefinition def))
            {
                throw new InvalidOperationException($"[MatchBootstrap] unknown unit definition ({faction}, {role})");
            }

            return Runner.Entities.SpawnUnit(
                slot,
                new Transform2D(SimFixed.FromInt(cellX), SimFixed.FromInt(cellY)),
                def.MoveSpeed,
                maxHealth: UseDefinitionStats ? def.MaxHealth : SpawnDefaultMaxHealth,
                role: def.Role);
        }

        /// <summary>Fixed opening layout of one slot, in grid cells (mirrors Determinism10000Scenario's SlotLayout subset used by SetupMatch).</summary>
        private sealed class SlotLayout
        {
            public byte Slot;
            public ushort FieldId;
            public int FieldX, FieldY;
            public int HqOriginX, HqOriginY;
            public int BuilderX, BuilderY;
        }

        /// <summary>
        /// Human base, bottom-left: the field sits two cells north-east of
        /// the HQ footprint, the Builder just east of both — the natural
        /// first build (a Refinery beside the field) is one step away.
        /// </summary>
        private static readonly SlotLayout LocalLayout = new SlotLayout
        {
            Slot = LocalSlot,
            FieldId = 1, FieldX = 7, FieldY = 7,
            HqOriginX = 4, HqOriginY = 4,
            BuilderX = 13, BuilderY = 7,
        };

        /// <summary>Opponent base, top-right: the 180-degree mirror of <see cref="LocalLayout"/>.</summary>
        private static readonly SlotLayout EnemyLayout = new SlotLayout
        {
            Slot = EnemySlot,
            FieldId = 2, FieldX = 119, FieldY = 119,
            HqOriginX = 120, HqOriginY = 120,
            BuilderX = 113, BuilderY = 119,
        };
    }
}
