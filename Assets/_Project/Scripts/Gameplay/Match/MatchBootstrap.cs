using System;
using UnityEngine;
using Nova.Core;
using Nova.Simulation.CommandsV1;
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
    /// It MIRRORS <c>Determinism10000Scenario.SetupMatch</c>
    /// (tools/Nova.SimRunner/Determinism10000Scenario.cs): identical seed,
    /// identical map size, identical entity capacity, identical per-slot
    /// layout AND identical spawn ORDER — field, HQ, Refinery, HarvesterA,
    /// HarvesterB, Builder, four skirmish infantry; slot 0 first, then slot 1.
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
    /// <see cref="SimDefinitions.TryGetUnit"/> so units carry their real
    /// stats. Move speeds are identical either way (the definition table and
    /// the scenario literals agree exactly); the ONLY value that differs is
    /// the Harvester's maxHealth (definition 300 vs. default 100), and
    /// maxHealth IS part of the hashed entity-store block. Set
    /// <see cref="UseDefinitionStats"/> to false to get a byte-exact
    /// <c>SetupMatch</c> mirror — that is the switch the InitialStateHash
    /// test must flip.
    /// </para>
    /// <para>
    /// Command law (docs/tech/Commands.md, sprint hard rule 2): the initial
    /// spawn/placement is the one allowed direct write into simulation state.
    /// Everything after it — including the opening harvest order — goes
    /// through <see cref="MatchRunner.Ingress"/> as a
    /// <see cref="CommandIntent"/>.
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

        // Definition ids of the canonical table (SimDefinitions, manifest order).
        private const ushort DefHQ = 1;
        private const ushort DefRefinery = 3;
        private const ushort DefBuilder = 1;
        private const ushort DefHarvester = 2;
        private const ushort DefBasicInfantry = 3;

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
        private readonly EntityId[] _refinery = new EntityId[2];
        private readonly EntityId[] _harvesterA = new EntityId[2];
        private readonly EntityId[] _harvesterB = new EntityId[2];
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

        /// <summary>Lower-left footprint origin of the human Refinery (8, 4).</summary>
        public Vector2Int LocalRefineryOrigin => new Vector2Int(LocalLayout.RefineryOriginX, LocalLayout.RefineryOriginY);

        /// <summary>Lower-left footprint origin of the opponent Refinery (116, 120).</summary>
        public Vector2Int EnemyRefineryOrigin => new Vector2Int(EnemyLayout.RefineryOriginX, EnemyLayout.RefineryOriginY);

        /// <summary>Entity handle of the human HQ (invalid until the match is set up).</summary>
        public EntityId LocalHq => _hq[LocalSlot];

        /// <summary>Entity handle of the opponent HQ.</summary>
        public EntityId EnemyHq => _hq[EnemySlot];

        /// <summary>Entity handle of the human Refinery.</summary>
        public EntityId LocalRefinery => _refinery[LocalSlot];

        /// <summary>Entity handle of the human Builder (spawned at 13, 7).</summary>
        public EntityId LocalBuilder => _builder[LocalSlot];

        /// <summary>Entity handles of the two human Harvesters, in spawn order.</summary>
        public EntityId LocalHarvesterA => _harvesterA[LocalSlot];
        public EntityId LocalHarvesterB => _harvesterB[LocalSlot];

        private void Start()
        {
            if (AutoStart)
            {
                StartGrayboxMatch();
            }
        }

        /// <summary>
        /// Initializes and starts the runner, builds the canonical opening
        /// position for both slots and submits the human player's opening
        /// harvest order. Idempotent: a second call is a no-op.
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
            Runner.StartMatch();

            // Slot order is load-bearing for entity ids: slot 0 first.
            SetupSlot(LocalLayout);
            SetupSlot(EnemyLayout);
            IsMatchReady = true;

            // Everything past setup is a command. Session tick is still 0 and
            // the input delay is 1, so these land in the batch sealed for tick
            // 1 — exactly where the scenario script issues them.
            SubmitHarvestOrder(_harvesterA[LocalSlot], LocalLayout.FieldId);
            SubmitHarvestOrder(_harvesterB[LocalSlot], LocalLayout.FieldId);

            Debug.Log(
                $"[MatchBootstrap] Graybox match started (seed 0x{_seed:X16}, {_mapWidth}x{_mapHeight}, " +
                $"capacity {_entityCapacity}, definition stats {UseDefinitionStats}).");
        }

        /// <summary>
        /// One slot's MS-1 start state: Aetherium field, completed HQ,
        /// completed Refinery (the manifest's documented prerequisite
        /// exception — no Power plant required), two Harvesters, one Builder,
        /// four skirmish infantry. Spawn order mirrors SetupMatch exactly.
        /// </summary>
        private void SetupSlot(SlotLayout c)
        {
            if (!Runner.Economy.TryAddField(c.FieldId, new GridPos2D(c.FieldX, c.FieldY), FieldReserveAE))
            {
                throw new InvalidOperationException($"[MatchBootstrap] field {c.FieldId} could not be registered");
            }

            _hq[c.Slot] = Runner.Construction.PlaceCompletedBuilding(c.Slot, DefHQ, c.HqOriginX, c.HqOriginY);
            if (!_hq[c.Slot].IsValid)
            {
                throw new InvalidOperationException($"[MatchBootstrap] HQ placement failed for slot {c.Slot}");
            }

            _refinery[c.Slot] = Runner.Construction.PlaceCompletedBuilding(c.Slot, DefRefinery, c.RefineryOriginX, c.RefineryOriginY);
            if (!_refinery[c.Slot].IsValid)
            {
                throw new InvalidOperationException($"[MatchBootstrap] Refinery placement failed for slot {c.Slot}");
            }

            _harvesterA[c.Slot] = Spawn(c.Slot, DefHarvester, c.HarvesterAX, c.HarvesterAY);
            _harvesterB[c.Slot] = Spawn(c.Slot, DefHarvester, c.HarvesterBX, c.HarvesterBY);
            _builder[c.Slot] = Spawn(c.Slot, DefBuilder, c.BuilderX, c.BuilderY);

            for (int i = 0; i < c.SquadX.Length; i++)
            {
                Spawn(c.Slot, DefBasicInfantry, c.SquadX[i], c.SquadY[i]);
            }
        }

        /// <summary>
        /// Spawns one unit from its canonical definition. Role and move speed
        /// always come from <see cref="SimDefinitions"/> (identical to the
        /// scenario literals); maxHealth only when
        /// <see cref="UseDefinitionStats"/> is set — see the class remarks.
        /// </summary>
        private EntityId Spawn(byte slot, ushort unitDefId, int cellX, int cellY)
        {
            if (!SimDefinitions.TryGetUnit(unitDefId, out SimUnitDefinition def))
            {
                throw new InvalidOperationException($"[MatchBootstrap] unknown unit definition {unitDefId}");
            }

            return Runner.Entities.SpawnUnit(
                slot,
                new Transform2D(SimFixed.FromInt(cellX), SimFixed.FromInt(cellY)),
                def.MoveSpeed,
                maxHealth: UseDefinitionStats ? def.MaxHealth : SpawnDefaultMaxHealth,
                role: def.Role);
        }

        /// <summary>
        /// Sends one Harvest order through the ingress — the ONLY legal way a
        /// MonoBehaviour touches a running match.
        /// </summary>
        private bool SubmitHarvestOrder(EntityId harvester, ushort fieldId)
        {
            if (!Runner.Entities.IsValid(harvester))
            {
                return false;
            }

            uint raw = UnitCommandStateView.ToRawEntityId(harvester);
            if (raw == 0u)
            {
                return false;
            }

            var payload = new HarvestPayload(new[] { raw }, fieldId);
            CommandIngressResult result = Runner.Ingress.TrySubmitIntent(
                CommandIntent.Create(payload), out CommandRejectReason reason);
            if (result != CommandIngressResult.Accepted)
            {
                Debug.LogError($"[MatchBootstrap] opening harvest order rejected: {result} ({reason}).");
                return false;
            }

            return true;
        }

        /// <summary>Fixed opening layout of one slot, in grid cells (mirrors Determinism10000Scenario's SlotLayout subset used by SetupMatch).</summary>
        private sealed class SlotLayout
        {
            public byte Slot;
            public ushort FieldId;
            public int FieldX, FieldY;
            public int HqOriginX, HqOriginY;
            public int RefineryOriginX, RefineryOriginY;
            public int HarvesterAX, HarvesterAY, HarvesterBX, HarvesterBY;
            public int BuilderX, BuilderY;
            public int[] SquadX, SquadY;
        }

        /// <summary>
        /// Human base, bottom-left. Both harvesters stand in Chebyshev reach 1
        /// of the field cell AND the Refinery footprint, so the harvest/return
        /// cycle runs without walking.
        /// </summary>
        private static readonly SlotLayout LocalLayout = new SlotLayout
        {
            Slot = LocalSlot,
            FieldId = 1, FieldX = 7, FieldY = 7,
            HqOriginX = 4, HqOriginY = 4,
            RefineryOriginX = 8, RefineryOriginY = 4,
            HarvesterAX = 7, HarvesterAY = 6, HarvesterBX = 7, HarvesterBY = 7,
            BuilderX = 13, BuilderY = 7,
            SquadX = new[] { 56, 57, 56, 57 },
            SquadY = new[] { 62, 62, 63, 63 },
        };

        /// <summary>Opponent base, top-right: the 180-degree mirror of <see cref="LocalLayout"/>.</summary>
        private static readonly SlotLayout EnemyLayout = new SlotLayout
        {
            Slot = EnemySlot,
            FieldId = 2, FieldX = 119, FieldY = 119,
            HqOriginX = 120, HqOriginY = 120,
            RefineryOriginX = 116, RefineryOriginY = 120,
            HarvesterAX = 119, HarvesterAY = 120, HarvesterBX = 119, HarvesterBY = 119,
            BuilderX = 113, BuilderY = 119,
            SquadX = new[] { 65, 66, 65, 66 },
            SquadY = new[] { 62, 62, 63, 63 },
        };
    }
}
