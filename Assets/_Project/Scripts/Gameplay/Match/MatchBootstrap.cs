using System;
using System.Globalization;
using UnityEngine;
using Nova.Core;
using Nova.Networking;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Definitions;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Replays;
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
        /// <summary>Canonical local-match slot. A relay offer may assign the human to slot 1.</summary>
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
        private MatchConfig _pendingConfig;
        private MatchConfig _activeConfig;
        private bool _waitingForOffer;
        private bool _waitingForStart;
        private bool _networkFailureReported;
        private string _networkStatusReason = string.Empty;

        /// <summary>The runner this bootstrap drives (resolved from the same GameObject).</summary>
        public MatchRunner Runner { get; private set; }

        /// <summary>True once the opening position exists and the kernel is stepping.</summary>
        public bool IsMatchReady { get; private set; }

        /// <summary>The current relay client during handshake and after start, or null for a local match.</summary>
        public RelayMatchClient NetworkClient =>
            _pendingConfig?.Transport ?? _activeConfig?.Transport ?? Runner?.RelayClient;

        /// <summary>Public network lifecycle retained through stalls and terminal shutdown.</summary>
        public RelayMatchLifecycle NetworkLifecycle => NetworkClient != null
            ? NetworkClient.Lifecycle
            : (_networkFailureReported ? RelayMatchLifecycle.Ended : RelayMatchLifecycle.Disconnected);

        public string NetworkStatusReason =>
            !string.IsNullOrEmpty(NetworkClient?.StatusReason) ? NetworkClient.StatusReason : _networkStatusReason;

        public int NetworkStalledOnSlot => NetworkClient?.StalledOnSlot ?? -1;
        public double NetworkStallSeconds => NetworkClient?.StallSeconds ?? 0.0;

        /// <summary>Seed the match was started with.</summary>
        public ulong Seed => _activeConfig != null ? _activeConfig.Seed : _seed;

        /// <summary>The validated configuration of the opening position, once built.</summary>
        public MatchConfig ActiveConfig => _activeConfig?.ValidateAndClone();

        /// <summary>Map size in cells (128x128 for the canonical match).</summary>
        public Vector2Int MapSize => new Vector2Int(_mapWidth, _mapHeight);

        /// <summary>Footprint edge length of every MS-1 building, in cells.</summary>
        public int BuildingFootprintCells => SimDefinitions.BuildingFootprintCells;

        // --- HUD-facing read-only geometry -------------------------------

        public ushort LocalFieldId => LocalPlayerLayout.FieldId;
        public ushort EnemyFieldId => EnemyPlayerLayout.FieldId;

        /// <summary>Aetherium field cell of the human player (7, 7).</summary>
        public Vector2Int LocalFieldCell => new Vector2Int(LocalPlayerLayout.FieldX, LocalPlayerLayout.FieldY);

        /// <summary>Aetherium field cell of the opponent (119, 119).</summary>
        public Vector2Int EnemyFieldCell => new Vector2Int(EnemyPlayerLayout.FieldX, EnemyPlayerLayout.FieldY);

        /// <summary>Lower-left footprint origin of the human HQ (4, 4).</summary>
        public Vector2Int LocalHqOrigin => new Vector2Int(LocalPlayerLayout.HqOriginX, LocalPlayerLayout.HqOriginY);

        /// <summary>Lower-left footprint origin of the opponent HQ (120, 120).</summary>
        public Vector2Int EnemyHqOrigin => new Vector2Int(EnemyPlayerLayout.HqOriginX, EnemyPlayerLayout.HqOriginY);

        /// <summary>Center cell of the human HQ footprint — the natural camera start focus.</summary>
        public Vector2Int LocalHqCenterCell => LocalHqOrigin + Vector2Int.one;

        /// <summary>Center cell of the opponent HQ footprint.</summary>
        public Vector2Int EnemyHqCenterCell => EnemyHqOrigin + Vector2Int.one;

        /// <summary>Entity handle of the human HQ (invalid until the match is set up).</summary>
        public EntityId LocalHq => _hq[ConfiguredLocalSlot];

        /// <summary>Entity handle of the opponent HQ.</summary>
        public EntityId EnemyHq => _hq[ConfiguredEnemySlot];

        /// <summary>Entity handle of the human Builder (spawned at 13, 7 — the D-077 opening's only unit).</summary>
        public EntityId LocalBuilder => _builder[ConfiguredLocalSlot];

        /// <summary>The local slot after applying the relay offer (zero for the local default).</summary>
        public byte ConfiguredLocalSlot => _activeConfig != null ? _activeConfig.LocalSlot : LocalSlot;

        private byte ConfiguredEnemySlot => ConfiguredLocalSlot == LocalSlot ? EnemySlot : LocalSlot;
        private SlotLayout LocalPlayerLayout => ConfiguredLocalSlot == LocalSlot ? LocalLayout : EnemyLayout;
        private SlotLayout EnemyPlayerLayout => ConfiguredLocalSlot == LocalSlot ? EnemyLayout : LocalLayout;

        private void Start()
        {
            if (AutoStart)
            {
                StartGrayboxMatch();
            }
        }

        private void Update()
        {
            RelayMatchClient visibleClient = NetworkClient;
            if (visibleClient != null && visibleClient.Phase == RelayClientPhase.Ended)
            {
                Runner?.StopNetworkMatch();
                ReportNetworkFailure(visibleClient.EndReason);
                return;
            }
            if (_pendingConfig == null || IsMatchReady) return;

            RelayMatchClient client = _pendingConfig.Transport;
            if (_waitingForOffer)
            {
                // Before MatchRunner owns an ingress there is nothing for it
                // to poll; bootstrap pumps only this short offer phase. Once
                // initialized, MatchRunner polls on every Update, including
                // WaitingStart, pause and stalls.
                client.Poll();
                if (client.Phase == RelayClientPhase.Ended)
                {
                    ReportNetworkFailure(client.EndReason);
                    return;
                }
                if (!client.HasOffer) return;

                try
                {
                    if (client.ServerDefinitionsHash64 != SimDefinitions.ComputeDefinitionsHash64())
                    {
                        throw new InvalidOperationException(
                            $"relay definitions hash 0x{client.ServerDefinitionsHash64:X16} does not match this build");
                    }
                    MatchConfig offered = _pendingConfig.WithOffer(client);
                    _waitingForOffer = false;
                    BuildOpening(offered);
                    SubmitNetworkProof(offered);
                    _waitingForStart = true;
                }
                catch (Exception exception)
                {
                    client.Disconnect();
                    Runner?.StopNetworkMatch();
                    ReportNetworkFailure(exception.Message);
                }
                return;
            }

            if (_waitingForStart)
            {
                if (client.Phase == RelayClientPhase.Ended)
                {
                    ReportNetworkFailure(client.EndReason);
                    return;
                }
                if (client.Phase == RelayClientPhase.Running)
                {
                    _waitingForStart = false;
                    _pendingConfig = null;
                    IsMatchReady = true;
                    Debug.Log(
                        $"[MatchBootstrap] Network match started as slot {_activeConfig.LocalSlot} " +
                        $"(seed 0x{_activeConfig.Seed:X16}, delay {_activeConfig.InputDelayTicks}).");
                }
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
            if (IsMatchReady || _pendingConfig != null)
            {
                return;
            }

            MatchConfig local = MatchConfig.LocalVsAi(
                _seed, _mapWidth, _mapHeight, _entityCapacity,
                Nova.Simulation.Economy.EconomySystem.CanonicalMatchStartingCreditsAE);

            string relayHost = Environment.GetEnvironmentVariable("NOVA_RELAY_HOST");
            if (string.IsNullOrWhiteSpace(relayHost))
            {
                StartGrayboxMatch(local);
                return;
            }

            if (!TryReadNetworkEnvironment(out int relayPort, out ulong matchToken, out string error))
            {
                // Host was explicitly present: invalid companion values are
                // a network-start failure, never a silent local fallback.
                ReportNetworkFailure(error);
                return;
            }

            MatchConfig network = MatchConfig.NetworkVsHuman(relayHost, relayPort, matchToken);
            network.Seed = local.Seed;
            network.MapWidth = local.MapWidth;
            network.MapHeight = local.MapHeight;
            network.EntityCapacity = local.EntityCapacity;
            network.StartingCredits = local.StartingCredits;
            StartGrayboxMatch(network);
        }

        /// <summary>
        /// Starts from an explicit configuration. Local matches build
        /// synchronously; network matches connect and wait for the relay's
        /// authoritative seed, slot and delay before creating any state.
        /// </summary>
        public void StartGrayboxMatch(MatchConfig config)
        {
            if (IsMatchReady || _pendingConfig != null) return;

            Runner = GetComponent<MatchRunner>();
            if (Runner == null)
            {
                Debug.LogError("[MatchBootstrap] No MatchRunner on this GameObject — cannot start a match.");
                return;
            }

            MatchConfig validated;
            try
            {
                validated = config.ValidateAndClone();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MatchBootstrap] Invalid match configuration: {exception.Message}");
                return;
            }

            if (validated.IsNetworkMatch)
            {
                _pendingConfig = validated;
                _waitingForOffer = true;
                _waitingForStart = false;
                _networkFailureReported = false;
                _networkStatusReason = string.Empty;
                validated.Transport.Connect(validated.RelayHost, validated.RelayPort, validated.MatchToken);
                if (validated.Transport.Phase == RelayClientPhase.Ended)
                {
                    ReportNetworkFailure(validated.Transport.EndReason);
                }
                return;
            }

            BuildOpening(validated);
            IsMatchReady = true;
            LogLocalMatchStarted();
        }

        private void BuildOpening(MatchConfig config)
        {
            _activeConfig = config.ValidateAndClone();
            _seed = _activeConfig.Seed;
            _mapWidth = _activeConfig.MapWidth;
            _mapHeight = _activeConfig.MapHeight;
            _entityCapacity = _activeConfig.EntityCapacity;

            Runner.InitializeMatch(_activeConfig);

            // Faction assignment (economy block v2) comes from MatchConfig.
            // Set BEFORE StartMatch — the SetSlotFaction
            // guard forbids any change once the kernel runs, because the
            // faction bytes are part of the hashed initial state and the
            // match fingerprint. The scenario's BuildHost does the same, in
            // the same order.
            for (int i = 0; i < _activeConfig.ActiveSlots.Length; i++)
            {
                byte slot = _activeConfig.ActiveSlots[i];
                Runner.Economy.SetSlotFaction(slot, _activeConfig.FactionPerSlot[slot]);
            }

            if (!Runner.StartMatch())
            {
                throw new InvalidOperationException("the match runner refused to start the configured kernel");
            }

            // Global slot order is load-bearing for entity ids and snapshots:
            // BOTH clients build slot 0 first, then slot 1, regardless of
            // which one the relay assigned locally.
            SetupSlot(LocalLayout);
            SetupSlot(EnemyLayout);
        }

        private void LogLocalMatchStarted()
        {
            Debug.Log(
                $"[MatchBootstrap] Graybox match started (seed 0x{_seed:X16}, {_mapWidth}x{_mapHeight}, " +
                $"capacity {_entityCapacity}, definition stats {UseDefinitionStats}, " +
                $"start credits {Runner.Economy.GetPlayerEconomy(ConfiguredLocalSlot).AetheriumCredits} AE).");
        }

        private void SubmitNetworkProof(MatchConfig config)
        {
            var occupancy = new byte[CommandLimits.ReservedPlayerSlots];
            var factions = new byte[CommandLimits.ReservedPlayerSlots];
            for (int i = 0; i < factions.Length; i++) factions[i] = (byte)config.FactionPerSlot[i];
            for (int i = 0; i < config.ActiveSlots.Length; i++)
            {
                byte slot = config.ActiveSlots[i];
                occupancy[slot] = (byte)(Contains(config.AiSlots, slot)
                    ? PlayerSlotOccupancy.AI
                    : PlayerSlotOccupancy.Human);
            }

            byte[] snapshot = Runner.Kernel.SaveSnapshot();
            MatchFingerprint fingerprint = MatchFingerprint.CreateCurrent(
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Rules),
                SimDefinitions.ComputeDefinitionsHash64(),
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Map),
                occupancy,
                factions,
                config.Seed,
                Runner.Kernel.CalculateStateHash(),
                config.InputDelayTicks);
            config.Transport.SubmitLocalProof(fingerprint.Serialize(), snapshot);
        }

        /// <summary>
        /// Starts a completely fresh match after one already ran ("Neue
        /// Runde" / a second "Neues Spiel"): stops the old kernel, then
        /// re-runs the whole start path — InitializeMatch rebuilds kernel,
        /// entity store, all systems, the AI peer, both sessions and both
        /// ingress instances, so the sim side is a true reset. The
        /// PRESENTATION side must be reset by the caller (views via
        /// UnitViewManager.ResetViews, camera via ResetToStartFocus; the fog
        /// overlay and minimap self-heal through their fog-instance guards,
        /// and the selection clears itself on the ingress rebind).
        /// </summary>
        public bool RestartMatch()
        {
            if ((_activeConfig != null && _activeConfig.IsNetworkMatch)
                || (_pendingConfig != null && _pendingConfig.IsNetworkMatch))
            {
                Debug.LogError(
                    "[MatchBootstrap] Network restart refused: a fresh relay offer and a fresh transport are required.");
                return false;
            }

            MatchConfig restartConfig = _activeConfig != null
                ? _activeConfig.ValidateAndClone()
                : MatchConfig.LocalVsAi(
                    _seed, _mapWidth, _mapHeight, _entityCapacity,
                    Nova.Simulation.Economy.EconomySystem.CanonicalMatchStartingCreditsAE);
            if (Runner != null && Runner.IsRunning)
            {
                Runner.PauseMatch();
            }
            IsMatchReady = false;
            _activeConfig = null;
            StartGrayboxMatch(restartConfig);
            return IsMatchReady;
        }

        private static bool Contains(byte[] slots, byte slot)
        {
            for (int i = 0; i < slots.Length; i++) if (slots[i] == slot) return true;
            return false;
        }

        private static bool TryReadNetworkEnvironment(
            out int port, out ulong token, out string error)
        {
            port = 0;
            token = 0;
            error = string.Empty;
            string portText = Environment.GetEnvironmentVariable("NOVA_RELAY_PORT");
            string tokenText = Environment.GetEnvironmentVariable("NOVA_MATCH_TOKEN");
            if (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out port)
                || port < 1 || port > 65535)
            {
                error = "NOVA_RELAY_PORT must be an integer from 1 through 65535.";
                return false;
            }
            if (!RelayProtocol.TryParseMatchToken(tokenText, out token))
            {
                error = "NOVA_MATCH_TOKEN must be exactly 16 unprefixed hexadecimal characters and non-zero.";
                return false;
            }
            return true;
        }

        private void ReportNetworkFailure(string reason)
        {
            if (_networkFailureReported) return;
            _networkFailureReported = true;
            _networkStatusReason = string.IsNullOrWhiteSpace(reason)
                ? "relay match ended without a reason"
                : reason;
            _waitingForOffer = false;
            _waitingForStart = false;
            Debug.LogError(IsMatchReady
                ? $"[MatchBootstrap] Network match ended: {_networkStatusReason}"
                : $"[MatchBootstrap] Network match did not start: {_networkStatusReason}");
        }

        private void OnDestroy()
        {
            // Before BuildOpening, MatchRunner does not yet own the pending
            // transport. Bootstrap must close it itself when its GameObject
            // is destroyed during the offer handshake.
            _pendingConfig?.Transport?.Disconnect();
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
