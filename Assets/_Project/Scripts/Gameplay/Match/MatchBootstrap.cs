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
    /// <summary>Expected relay slot of a manually started two-player match.</summary>
    public enum NetworkJoinRole
    {
        Host = 0,
        Guest = 1,
    }

    /// <summary>Presentation-safe progress of the menu-driven network join.</summary>
    public enum NetworkJoinPhase
    {
        Idle = 0,
        Connecting = 1,
        CheckingStart = 2,
        WaitingForOpponent = 3,
        Ready = 4,
        Failed = 5,
    }

    /// <summary>Stable, presentation-safe category for a failed network join.</summary>
    public enum NetworkJoinFailure
    {
        None = 0,
        InvalidHost = 1,
        InvalidPort = 2,
        InvalidMatchCode = 3,
        RoleMismatch = 4,
        MatchCodeRejected = 5,
        DefinitionsMismatch = 6,
        FingerprintMismatch = 7,
        SnapshotMismatch = 8,
        PeerLost = 9,
        Stall = 10,
        Desync = 11,
        Technical = 12,
    }

    /// <summary>
    /// Immutable menu-facing join status. It deliberately contains no relay
    /// client, endpoint or match-code data.
    /// </summary>
    public readonly struct NetworkJoinStatus
    {
        public NetworkJoinStatus(
            NetworkJoinPhase phase, NetworkJoinFailure failure, string message)
        {
            Phase = phase;
            Failure = failure;
            Message = message ?? string.Empty;
        }

        public NetworkJoinPhase Phase { get; }
        public NetworkJoinFailure Failure { get; }
        public string Message { get; }
        public bool IsActive => Phase == NetworkJoinPhase.Connecting
            || Phase == NetworkJoinPhase.CheckingStart
            || Phase == NetworkJoinPhase.WaitingForOpponent;
        public bool CanCancel => IsActive;
    }

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
    /// <see cref="MatchRunner.InitializeMatch"/>'s default) — plus the
    /// fifteen finite canonical Aetherium fields (two starts, four natural
    /// expansions, four contested flanks and the five-field contested centre
    /// zone; 21.6/21.7, #93/#94). The Refinery is NO longer
    /// pre-placed: the player builds it (it has no Power-plant prerequisite
    /// since D-077), and the completed Refinery — not the HQ — produces the
    /// Harvesters.
    /// </para>
    /// <para>
    /// It MIRRORS <c>Determinism10000Scenario.SetupMatch</c>
    /// (tools/Nova.SimRunner/Determinism10000Scenario.cs): identical seed,
    /// identical map size, identical entity capacity, identical field and
    /// per-slot layout AND identical entity spawn order — HQ, Builder; slot 0
    /// first, then slot 1. Fields register first in canonical id order but do
    /// not allocate entity ids.
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

        /// <summary>One Aetherium field of the canonical map (16.7, C1).</summary>
        private struct FieldLayout
        {
            public ushort Id;
            public int X, Y;
            public long ReserveAE;
        }

        /// <summary>
        /// The fifteen canonical fields (21.6/21.7, #93/#94 — the T-01
        /// report's "at least roughly twice as many", up from the five of
        /// 16.7/C1): two start fields and TWO natural expansions per side at
        /// 9.000 AE each, four contested flank fields at 12.000 and the
        /// centre as a FIVE-FIELD ZONE (D-109) at 8.000 each — 40.000 AE in
        /// the middle, 28 % of the map's 142.000, the same share the single
        /// 15.000-AE centre field held before (29 % of 51.000), but divisible
        /// and therefore partially holdable: the zone is valuable without
        /// deciding the match alone. Symmetry is binding (D-107): point
        /// coordinates mirror as (x, y) -&gt; (124 - x, 124 - y); a 3x3
        /// footprint's lower-left origin therefore mirrors as
        /// (x, y) -&gt; (122 - x, 122 - y). The pairs 1/2, 3/4, 6/7, 8/9,
        /// 10/11, 12/13 and 14/15 are exact mirrors; the middle field (62,62)
        /// mirrors onto itself. Distances are Chebyshev from the HQ
        /// footprint's centre-facing corner cell ((6,6) against (118,118) —
        /// the convention behind the canonical 34/56): start fields 1,
        /// naturals 34 and 38, the zone's near corner fields 48, its
        /// anti-diagonal pair exactly equidistant (64/64) and its centre 56,
        /// the near flank pair exactly equidistant (74/74), the far flank
        /// pair leaning four cells toward its side (90/94). Reserves:
        /// naturals match the start field (the 21.3 longevity measurement),
        /// flanks pay +33 % for the contested walk, zone fields sit just
        /// below a natural — the zone's prize is its sum, not its single
        /// field.
        /// </summary>
        private static readonly FieldLayout[] FieldLayouts =
        {
            new FieldLayout { Id = 1,  X = 7,   Y = 7,   ReserveAE = 9000L },
            new FieldLayout { Id = 2,  X = 117, Y = 117, ReserveAE = 9000L },
            new FieldLayout { Id = 3,  X = 24,  Y = 40,  ReserveAE = 9000L },
            new FieldLayout { Id = 4,  X = 100, Y = 84,  ReserveAE = 9000L },
            new FieldLayout { Id = 5,  X = 62,  Y = 62,  ReserveAE = 8000L },
            new FieldLayout { Id = 6,  X = 54,  Y = 54,  ReserveAE = 8000L },
            new FieldLayout { Id = 7,  X = 70,  Y = 70,  ReserveAE = 8000L },
            new FieldLayout { Id = 8,  X = 54,  Y = 70,  ReserveAE = 8000L },
            new FieldLayout { Id = 9,  X = 70,  Y = 54,  ReserveAE = 8000L },
            new FieldLayout { Id = 10, X = 44,  Y = 24,  ReserveAE = 9000L },
            new FieldLayout { Id = 11, X = 80,  Y = 100, ReserveAE = 9000L },
            new FieldLayout { Id = 12, X = 44,  Y = 80,  ReserveAE = 12000L },
            new FieldLayout { Id = 13, X = 80,  Y = 44,  ReserveAE = 12000L },
            new FieldLayout { Id = 14, X = 24,  Y = 96,  ReserveAE = 12000L },
            new FieldLayout { Id = 15, X = 100, Y = 28,  ReserveAE = 12000L },
        };

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
        private bool _offerAccepted;
        private int _expectedNetworkSlot = -1;
        private bool _networkFailureReported;
        private string _networkStatusReason = string.Empty;
        private NetworkJoinStatus _networkJoinStatus = new NetworkJoinStatus(
            NetworkJoinPhase.Idle, NetworkJoinFailure.None,
            "Keine Netzverbindung aktiv.");

        /// <summary>The runner this bootstrap drives (resolved from the same GameObject).</summary>
        public MatchRunner Runner { get; private set; }

        /// <summary>True once the opening position exists and the kernel is stepping.</summary>
        public bool IsMatchReady { get; private set; }

        /// <summary>The current relay client during handshake and after start, or null for a local match.</summary>
        public RelayMatchClient NetworkClient =>
            _pendingConfig?.Transport ?? _activeConfig?.Transport;

        /// <summary>Public network lifecycle retained through stalls and terminal shutdown.</summary>
        public RelayMatchLifecycle NetworkLifecycle => NetworkClient != null
            ? NetworkClient.Lifecycle
            : (_networkFailureReported ? RelayMatchLifecycle.Ended : RelayMatchLifecycle.Disconnected);

        public string NetworkStatusReason =>
            !string.IsNullOrEmpty(NetworkClient?.StatusReason) ? NetworkClient.StatusReason : _networkStatusReason;

        public int NetworkStalledOnSlot => NetworkClient?.StalledOnSlot ?? -1;
        public double NetworkStallSeconds => NetworkClient?.StallSeconds ?? 0.0;

        /// <summary>Menu-safe network progress without exposing networking types.</summary>
        public NetworkJoinStatus JoinStatus => _networkJoinStatus;

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

        /// <summary>
        /// The initial reserve of a canonical field (21.2, #86). It lives in
        /// the canonical map layout, NOT in simulation state —
        /// <c>AetheriumField</c> deliberately carries no InitialReserve field
        /// because the Simulation/State/ layout is frozen. Returns false for
        /// an unknown id.
        /// </summary>
        public bool TryGetFieldInitialReserve(ushort fieldId, out long reserveAE)
        {
            for (int i = 0; i < FieldLayouts.Length; i++)
            {
                if (FieldLayouts[i].Id == fieldId)
                {
                    reserveAE = FieldLayouts[i].ReserveAE;
                    return true;
                }
            }
            reserveAE = 0L;
            return false;
        }

        /// <summary>Aetherium field cell of the human player (7, 7).</summary>
        public Vector2Int LocalFieldCell => new Vector2Int(LocalPlayerLayout.FieldX, LocalPlayerLayout.FieldY);

        /// <summary>Aetherium field cell of the opponent (117, 117 — the D-102/D-107 Glutrinne layout-axis mirror).</summary>
        public Vector2Int EnemyFieldCell => new Vector2Int(EnemyPlayerLayout.FieldX, EnemyPlayerLayout.FieldY);

        /// <summary>
        /// All fifteen registered field cells in canonical id order: start
        /// 0/1, expansion 0/1, the five centre-zone fields (middle, diagonal
        /// pair, anti-diagonal pair), second expansion 0/1, contested near
        /// flank 0/1, contested far flank 0/1. Presentation iterates this
        /// list so marker and scatter geometry cannot silently omit a field.
        /// </summary>
        public Vector2Int[] AllFieldCells => CanonicalFieldCells;

        /// <summary>
        /// The same list as <see cref="AllFieldCells"/>, readable WITHOUT a
        /// live component. Editor tooling that bakes the canonical map reads
        /// the layout from here instead of restating it: the field table
        /// already stands in five places by necessity (Unity host, headless
        /// scenario, three test mirrors — risk R-1 of sprint 21), and a sixth
        /// copy in BootstrapSceneGenerator drifted silently through two map
        /// changes before anyone noticed. Its comment still claimed "the five
        /// fields MatchBootstrap registers" when there were fifteen. A copy
        /// nobody reads is not harmless; it is a wrong answer waiting for its
        /// first reader.
        /// </summary>
        public static Vector2Int[] CanonicalFieldCells
        {
            get
            {
                var cells = new Vector2Int[FieldLayouts.Length];
                for (int i = 0; i < FieldLayouts.Length; i++)
                {
                    cells[i] = new Vector2Int(FieldLayouts[i].X, FieldLayouts[i].Y);
                }
                return cells;
            }
        }

        /// <summary>
        /// The two HQ footprint CENTRES in slot order (local, enemy), derived
        /// from the same slot layouts the match spawns from — so the D-107
        /// point mirror p -&gt; 124 - p holds by construction rather than by
        /// a second hand-kept pair of literals. Same purpose as
        /// <see cref="CanonicalFieldCells"/>: editor tooling reads, it does
        /// not restate.
        /// </summary>
        public static Vector2Int[] CanonicalHqCentreCells => new[]
        {
            new Vector2Int(LocalLayout.HqOriginX + 1, LocalLayout.HqOriginY + 1),
            new Vector2Int(EnemyLayout.HqOriginX + 1, EnemyLayout.HqOriginY + 1),
        };

        /// <summary>Lower-left footprint origin of the human HQ (4, 4).</summary>
        public Vector2Int LocalHqOrigin => new Vector2Int(LocalPlayerLayout.HqOriginX, LocalPlayerLayout.HqOriginY);

        /// <summary>Lower-left footprint origin of the opponent HQ (118, 118).</summary>
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
                    if (!_offerAccepted)
                    {
                        if (_expectedNetworkSlot >= 0
                            && client.AssignedSlot != _expectedNetworkSlot)
                        {
                            string roleMessage = _expectedNetworkSlot == (int)NetworkJoinRole.Host
                                ? "Rollenfehler: Der Host erwartet Slot 0 und muss zuerst verbinden."
                                : "Rollenfehler: Der Gast erwartet Slot 1 und verbindet erst nach dem Host.";
                            client.Disconnect();
                            FailNetworkJoin(NetworkJoinFailure.RoleMismatch, roleMessage);
                            return;
                        }
                        if (client.ServerDefinitionsHash64 != SimDefinitions.ComputeDefinitionsHash64())
                        {
                            throw new InvalidOperationException(
                                $"relay definitions hash 0x{client.ServerDefinitionsHash64:X16} does not match this build");
                        }

                        // Deliberately defer building the opening by one host
                        // frame. The status band can therefore show the real
                        // role/definition/start-data check instead of jumping
                        // directly from Connecting to Waiting.
                        _offerAccepted = true;
                        SetJoinStatus(
                            NetworkJoinPhase.CheckingStart,
                            "Prüfe Rolle, Definitionen und Startdaten …");
                        return;
                    }
                    MatchConfig offered = _pendingConfig.WithOffer(client);
                    _waitingForOffer = false;
                    BuildOpening(offered);
                    SubmitNetworkProof(offered);
                    _waitingForStart = true;
                    SetJoinStatus(
                        NetworkJoinPhase.WaitingForOpponent,
                        "Warte auf Gegenspieler — Start und Fingerabdruck werden geprüft …");
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
                    SetJoinStatus(NetworkJoinPhase.Ready, "Netzpartie ist bereit.");
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
        /// Validates menu text and begins a fresh network join. The match code
        /// is parsed only into the in-memory transport configuration and is
        /// never echoed, logged or persisted.
        /// </summary>
        public bool TryStartNetworkJoin(
            string host, string portText, string matchCode, NetworkJoinRole role)
        {
            if (!int.TryParse(
                    portText, NumberStyles.None, CultureInfo.InvariantCulture,
                    out int port))
            {
                FailNetworkJoin(
                    NetworkJoinFailure.InvalidPort,
                    "Der Port muss eine Zahl von 1 bis 65535 sein.");
                return false;
            }
            return TryStartNetworkJoin(host, port, matchCode, role);
        }

        /// <summary>Integer-port overload for tests and non-UI hosts.</summary>
        public bool TryStartNetworkJoin(
            string host, int port, string matchCode, NetworkJoinRole role)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                FailNetworkJoin(
                    NetworkJoinFailure.InvalidHost,
                    "Die Serveradresse darf nicht leer sein.");
                return false;
            }
            if (port < 1 || port > 65535)
            {
                FailNetworkJoin(
                    NetworkJoinFailure.InvalidPort,
                    "Der Port muss zwischen 1 und 65535 liegen.");
                return false;
            }
            if (!Enum.IsDefined(typeof(NetworkJoinRole), role))
            {
                FailNetworkJoin(
                    NetworkJoinFailure.RoleMismatch,
                    "Die gewählte Netzwerkrolle ist ungültig.");
                return false;
            }
            if (!RelayProtocol.TryParseMatchToken(matchCode, out ulong matchToken))
            {
                FailNetworkJoin(
                    NetworkJoinFailure.InvalidMatchCode,
                    "Der Match-Code muss aus genau 16 Hexzeichen bestehen und darf nicht null sein.");
                return false;
            }
            if (IsMatchReady || (_pendingConfig != null && _pendingConfig.IsNetworkMatch))
            {
                FailNetworkJoin(
                    NetworkJoinFailure.Technical,
                    "Eine Partie oder Verbindung ist bereits aktiv.");
                return false;
            }

            ResetNetworkJoin();
            _expectedNetworkSlot = (int)role;
            SetJoinStatus(NetworkJoinPhase.Connecting, "Verbinde mit dem Relay …");

            MatchConfig local = MatchConfig.LocalVsAi(
                _seed, _mapWidth, _mapHeight, _entityCapacity,
                Nova.Simulation.Economy.EconomySystem.CanonicalMatchStartingCreditsAE);
            MatchConfig network = MatchConfig.NetworkVsHuman(
                host.Trim(), port, matchToken, new RelayMatchClient());
            network.Seed = local.Seed;
            network.MapWidth = local.MapWidth;
            network.MapHeight = local.MapHeight;
            network.EntityCapacity = local.EntityCapacity;
            network.StartingCredits = local.StartingCredits;
            StartGrayboxMatch(network);
            return _pendingConfig != null
                && _pendingConfig.IsNetworkMatch
                && _networkJoinStatus.Phase != NetworkJoinPhase.Failed;
        }

        /// <summary>
        /// Lobby entry point (sprint 14, D-092): the LobbySession delivers a
        /// fully populated network configuration — relay endpoint, D-093
        /// match token and BOTH slot factions, which the relay offer does not
        /// carry — so unlike <see cref="TryStartNetworkJoin"/> nothing is
        /// parsed here. Validation runs through the same
        /// <see cref="MatchConfig.ValidateAndClone"/> gauntlet, and from
        /// <see cref="StartGrayboxMatch(MatchConfig)"/> on the relay
        /// handshake is byte-identical to the direct path.
        /// </summary>
        /// <param name="config">The lobby-assembled configuration (NetworkVsHuman plus factions).</param>
        /// <param name="expectedSlot">Slot the lobby assigned (0 = creator, 1 = joiner); the offer is rejected on mismatch.</param>
        public bool TryStartLobbyMatch(MatchConfig config, byte expectedSlot)
        {
            if (config == null)
            {
                FailNetworkJoin(
                    NetworkJoinFailure.Technical,
                    "Die Lobby hat keine Matchkonfiguration geliefert.");
                return false;
            }
            if (expectedSlot != LocalSlot && expectedSlot != EnemySlot)
            {
                FailNetworkJoin(
                    NetworkJoinFailure.RoleMismatch,
                    "Die Lobby hat einen ungültigen Slot zugewiesen.");
                return false;
            }
            if (IsMatchReady || (_pendingConfig != null && _pendingConfig.IsNetworkMatch))
            {
                FailNetworkJoin(
                    NetworkJoinFailure.Technical,
                    "Eine Partie oder Verbindung ist bereits aktiv.");
                return false;
            }

            MatchConfig validated;
            try
            {
                validated = config.ValidateAndClone();
            }
            catch (Exception exception)
            {
                FailNetworkJoin(
                    NetworkJoinFailure.Technical,
                    $"Die Lobby-Matchkonfiguration ist ungültig: {exception.Message}");
                return false;
            }

            ResetNetworkJoin();
            _expectedNetworkSlot = expectedSlot;
            SetJoinStatus(NetworkJoinPhase.Connecting, "Verbinde mit dem Relay …");

            // Same canonical match parameters as the direct path: both lobby
            // clients must open the identical map or their fingerprints diverge.
            validated.Seed = _seed;
            validated.MapWidth = _mapWidth;
            validated.MapHeight = _mapHeight;
            validated.EntityCapacity = _entityCapacity;
            validated.StartingCredits =
                Nova.Simulation.Economy.EconomySystem.CanonicalMatchStartingCreditsAE;
            StartGrayboxMatch(validated);
            return _pendingConfig != null
                && _pendingConfig.IsNetworkMatch
                && _networkJoinStatus.Phase != NetworkJoinPhase.Failed;
        }

        /// <summary>
        /// Cancels connect/proof waiting and clears every prepared network
        /// opening. A later retry always constructs a fresh relay client.
        /// </summary>
        public bool CancelNetworkJoin()
        {
            if (!_networkJoinStatus.CanCancel) return false;
            ResetNetworkJoin();
            return true;
        }

        /// <summary>Clears terminal/cancelled join state and any prepared opening.</summary>
        public void ResetNetworkJoin()
        {
            RelayMatchClient client = NetworkClient;
            client?.Disconnect();
            Runner?.StopNetworkMatch();
            _pendingConfig = null;
            _activeConfig = null;
            _waitingForOffer = false;
            _waitingForStart = false;
            _offerAccepted = false;
            _expectedNetworkSlot = -1;
            _networkFailureReported = false;
            _networkStatusReason = string.Empty;
            IsMatchReady = false;
            SetJoinStatus(NetworkJoinPhase.Idle, "Keine Netzverbindung aktiv.");
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
                _offerAccepted = false;
                _networkFailureReported = false;
                _networkStatusReason = string.Empty;
                if (_networkJoinStatus.Phase == NetworkJoinPhase.Idle)
                {
                    SetJoinStatus(NetworkJoinPhase.Connecting, "Verbinde mit dem Relay …");
                }
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

            // 21.7 (#94, D-109): the canonical Glutrinne terrain. The cost
            // field is no snapshot block, so the restore proof only holds if
            // EVERY host of the canonical match — local, relay peer, and the
            // headless scenario's generator and playback hosts — writes the
            // identical cells before the first snapshot; the headless mirror
            // lives in Determinism10000Scenario.BuildHost and is pinned
            // cell-exact by GlutrinneTerrainTests. Written here, after
            // InitializeMatch and before StartMatch, so no tick, flow-field
            // request or snapshot can ever observe a host without its
            // terrain. The count check guards the canonical map size: on a
            // non-128 map the ring would half-apply and the epoch would
            // silently diverge from every other host.
            int terrainCells = GlutrinneTerrainMap.Apply(Runner.Pathfinding.CostField);
            if (terrainCells != GlutrinneTerrainMap.ImpassableCellCount)
            {
                throw new InvalidOperationException(
                    $"[MatchBootstrap] canonical terrain applied {terrainCells} cells, expected " +
                    $"{GlutrinneTerrainMap.ImpassableCellCount} — the match is not running the canonical 128x128 map");
            }

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
            // which one the relay assigned locally. Field registration
            // spawns nothing and therefore does not shift ids, but it is part
            // of the hashed initial state and must happen in one fixed order.
            SetupFields();
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
                MatchFingerprint.ComputeCurrentRulesHash64(),
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
            ClassifyNetworkFailure(reason, out NetworkJoinFailure failure, out string message);
            FailNetworkJoin(failure, message);
        }

        private void FailNetworkJoin(NetworkJoinFailure failure, string message)
        {
            RelayMatchClient client = NetworkClient;
            client?.Disconnect();
            Runner?.StopNetworkMatch();
            _networkFailureReported = true;
            _networkStatusReason = string.IsNullOrWhiteSpace(message)
                ? "Die Netzpartie wurde ohne technischen Grund beendet."
                : message;
            _waitingForOffer = false;
            _waitingForStart = false;
            _offerAccepted = false;
            _expectedNetworkSlot = -1;
            _pendingConfig = null;
            if (_activeConfig != null && _activeConfig.IsNetworkMatch)
            {
                _activeConfig = null;
                IsMatchReady = false;
            }
            SetJoinStatus(NetworkJoinPhase.Failed, _networkStatusReason, failure);
            Debug.LogError($"[MatchBootstrap] Network join failed: {_networkStatusReason}");
        }

        private void SetJoinStatus(
            NetworkJoinPhase phase, string message,
            NetworkJoinFailure failure = NetworkJoinFailure.None)
        {
            _networkJoinStatus = new NetworkJoinStatus(phase, failure, message);
        }

        private static void ClassifyNetworkFailure(
            string reason, out NetworkJoinFailure failure, out string message)
        {
            string detail = string.IsNullOrWhiteSpace(reason)
                ? "kein technischer Detailgrund"
                : reason.Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (ContainsIgnoreCase(detail, "wrong match code"))
            {
                failure = NetworkJoinFailure.MatchCodeRejected;
                message = "Der Relay hat den Match-Code abgelehnt.";
            }
            else if (ContainsIgnoreCase(detail, "definitions"))
            {
                failure = NetworkJoinFailure.DefinitionsMismatch;
                message = "Die Spieldaten unterscheiden sich vom Relay-Build.";
            }
            else if (ContainsIgnoreCase(detail, "snapshot"))
            {
                failure = NetworkJoinFailure.SnapshotMismatch;
                message = "Der gemeinsame Startzustand stimmt nicht überein.";
            }
            else if (ContainsIgnoreCase(detail, "fingerprint"))
            {
                failure = NetworkJoinFailure.FingerprintMismatch;
                message = "Der Start-Fingerabdruck beider Builds stimmt nicht überein.";
            }
            else if (ContainsIgnoreCase(detail, "stall")
                || ContainsIgnoreCase(detail, "delivered nothing"))
            {
                failure = NetworkJoinFailure.Stall;
                message = "Die Netzpartie wartet zu lange auf Eingaben des Gegenspielers.";
            }
            else if (ContainsIgnoreCase(detail, "desync"))
            {
                failure = NetworkJoinFailure.Desync;
                message = "Die Simulationen sind auseinandergelaufen; die Partie wurde beendet.";
            }
            else if (ContainsIgnoreCase(detail, "waiting for opponent timed out"))
            {
                failure = NetworkJoinFailure.PeerLost;
                message = "Innerhalb von zwei Minuten ist kein Gegenspieler beigetreten.";
            }
            else if (ContainsIgnoreCase(detail, "peer lost")
                || ContainsIgnoreCase(detail, "peer slot")
                || ContainsIgnoreCase(detail, "disconnected"))
            {
                failure = NetworkJoinFailure.PeerLost;
                message = "Die Verbindung zum Gegenspieler ist abgebrochen.";
            }
            else
            {
                failure = NetworkJoinFailure.Technical;
                message = $"Technischer Netzwerkfehler: {detail}";
            }
        }

        private static bool ContainsIgnoreCase(string text, string value)
        {
            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void OnDestroy()
        {
            // Before BuildOpening, MatchRunner does not yet own the pending
            // transport. Bootstrap must close it itself when its GameObject
            // is destroyed during the offer handshake.
            _pendingConfig?.Transport?.Disconnect();
        }

        /// <summary>
        /// Registers all fifteen canonical fields in ascending id order. This
        /// is the exact field pass mirrored by Determinism10000Scenario and
        /// both CanonicalMatchSetupTests lanes.
        /// </summary>
        private void SetupFields()
        {
            for (int i = 0; i < FieldLayouts.Length; i++)
            {
                FieldLayout field = FieldLayouts[i];
                if (!Runner.Economy.TryAddField(field.Id, new GridPos2D(field.X, field.Y), field.ReserveAE))
                {
                    throw new InvalidOperationException($"[MatchBootstrap] field {field.Id} could not be registered");
                }
            }
        }

        /// <summary>
        /// One slot's D-077 start state: a completed HQ and one Builder near
        /// it — nothing else. Fields are registered globally by
        /// <see cref="SetupFields"/>; entity spawn order remains HQ, Builder.
        /// </summary>
        private void SetupSlot(SlotLayout c)
        {
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

        /// <summary>Opponent base, top-right: the exact D-107 footprint mirror of <see cref="LocalLayout"/>.</summary>
        private static readonly SlotLayout EnemyLayout = new SlotLayout
        {
            Slot = EnemySlot,
            FieldId = 2, FieldX = 117, FieldY = 117,
            HqOriginX = 118, HqOriginY = 118,
            BuilderX = 111, BuilderY = 117,
        };
    }
}
