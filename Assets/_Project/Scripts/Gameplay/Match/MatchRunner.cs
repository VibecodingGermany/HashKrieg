using System;
using UnityEngine;
using Nova.AI;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Combat;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;
using Nova.Simulation.Vision;
using Nova.Networking;

namespace Nova.Gameplay.Match
{
    public sealed class UnityNovaLogger : INovaLogger
    {
        public bool IsEnabled(Core.LogLevel level) => true;

        public void Log(Core.LogLevel level, string message)
        {
            switch (level)
            {
                case Core.LogLevel.Warn:
                    Debug.LogWarning($"[Nova.Sim] {message}");
                    break;
                case Core.LogLevel.Error:
                    Debug.LogError($"[Nova.Sim] {message}");
                    break;
                default:
                    Debug.Log($"[Nova.Sim] {message}");
                    break;
            }
        }

        public void LogTrace(string message) => Log(Core.LogLevel.Trace, message);
        public void LogDebug(string message) => Log(Core.LogLevel.Debug, message);
        public void LogInfo(string message) => Log(Core.LogLevel.Info, message);
        public void LogWarn(string message) => Log(Core.LogLevel.Warn, message);
        public void LogError(string message) => Log(Core.LogLevel.Error, message);
    }

    /// <summary>
    /// MonoBehaviour driving the deterministic C# SimulationKernel inside Unity.
    /// Accumulates Time.deltaTime and steps the kernel at the canonical fixed
    /// rate (<see cref="SimClock.TicksPerSecond"/> = 10 Hz, 0.1 s per tick).
    /// <para>
    /// Command flow (docs/tech/Commands.md section 1): UI/AI create only
    /// <see cref="CommandIntent"/> values and hand them to the exposed
    /// <see cref="Ingress"/>; per fixed tick the runner seals the due batch,
    /// submits it to the kernel (the only intake) and steps. Systems never
    /// see commands — the kernel applies them in tick phase 1.
    /// </para>
    /// <para>
    /// Execution order -100: device input runs at -200 and must have enqueued
    /// this frame's intents BEFORE the runner seals the next tick's batch here.
    /// Without a pinned order the observed command latency is frame-ordering
    /// noise instead of the deterministic one-tick input delay. View code
    /// (UnitViewManager, RtsCameraController) reads in LateUpdate and is
    /// therefore always downstream of the step.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public class MatchRunner : MonoBehaviour
    {
        /// <summary>Canonical fixed tick delta (10 Hz, docs/tech/SimulationCore.md section 2).</summary>
        public const float TickDeltaTime = SimClock.TickDeltaSeconds;

        [Header("Match Settings")]
        [SerializeField] private ulong _seed = 12345UL;
        [SerializeField] private ushort _mapWidth = 128;
        [SerializeField] private ushort _mapHeight = 128;
        [SerializeField] private int _maxUnits = 2048;

        private float _timeAccumulator;
        private LocalLoopbackTransport _transport;
        private AiPeerCommandTransport _aiTransport;
        private RelayMatchClient _relayClient;
        private MatchConfig _config;
        private bool _kernelStarted;

        public SimulationKernel Kernel { get; private set; }
        public EntityManager Entities { get; private set; }
        public PathfindingSystem Pathfinding { get; private set; }
        public MovementSystem Movement { get; private set; }

        /// <summary>The canonical economy (credits, power, finite Aetherium harvest; SimulationCore.md section 2, phases 2/3).</summary>
        public EconomySystem Economy { get; private set; }

        /// <summary>The canonical construction domain (placement, sites, repair, T2 unlock; SimulationCore.md section 2, phases 4/5).</summary>
        public Simulation.Construction.ConstructionSystem Construction { get; private set; }

        /// <summary>The canonical production domain (unit queues, rally points; SimulationCore.md section 2, phase 4).</summary>
        public Simulation.Production.ProductionSystem Production { get; private set; }

        /// <summary>The canonical Fog of War (single committed sight for Combat/AI/snapshot/rendering, D-058).</summary>
        public FogOfWarSystem FogOfWar { get; private set; }

        /// <summary>The canonical FoW-gated hitscan combat (SimulationCore.md section 2, order step 8).</summary>
        public CombatSystem Combat { get; private set; }

        /// <summary>
        /// The canonical MS-1 victory contract (D-056): the read-only poll
        /// surface a HUD uses to show whether the match is still running, who
        /// won and when it was decided.
        /// </summary>
        public Simulation.Victory.VictorySystem Victory { get; private set; }

        /// <summary>Session authority binding the local player slot (MS-1: slot 0 of {0, 1}).</summary>
        public MatchSession Session { get; private set; }

        /// <summary>Validated, ownership-safe configuration of the current match.</summary>
        public MatchConfig Config => _config?.ValidateAndClone();

        /// <summary>The bound relay client, or null for the canonical local match.</summary>
        public RelayMatchClient RelayClient => _relayClient;

        /// <summary>True when this runner was initialized with a relay transport.</summary>
        public bool IsRelayMatch => _relayClient != null;

        /// <summary>Local input is always allowed; relay input starts only after the authoritative Start frame.</summary>
        public bool RelayCommandsAllowed =>
            _relayClient == null || _relayClient.Phase == RelayClientPhase.Running;

        /// <summary>Terminal relay reason for presentation, empty while no terminal reason exists.</summary>
        public string RelayEndReason =>
            _relayClient != null && _relayClient.Phase == RelayClientPhase.Ended
                ? _relayClient.EndReason
                : string.Empty;

        /// <summary>The only command entry point for UI and AI (intent in, sealed batch out).</summary>
        public CommandIngress Ingress { get; private set; }

        /// <summary>
        /// The MS-1 skirmish opponent's own peer session (slot 1); null when
        /// <see cref="InitializeMatch"/> ran with <c>enableSkirmishAi: false</c>.
        /// The AI is a session PEER, not a local intent source of
        /// <see cref="Session"/>: its slot, sequences and target ticks are
        /// assigned by this session's authority (docs/tech/AIArchitecture.md
        /// section 1), exactly like a remote player's session.
        /// </summary>
        public MatchSession AiSession { get; private set; }

        /// <summary>
        /// The AI peer's own ingress. Its sealed records are forwarded into
        /// <see cref="Ingress"/>'s validating intake by the
        /// <see cref="AiPeerCommandTransport"/> — byte-for-byte the path a
        /// network peer's records will take. Null when the AI is disabled.
        /// </summary>
        public CommandIngress AiIngress { get; private set; }

        /// <summary>The registered MS-1 skirmish opponent (slot 1, Legion); null when disabled.</summary>
        public SkirmishAiSystem SkirmishAi { get; private set; }

        public bool IsRunning => Kernel != null && Kernel.IsRunning;

        /// <summary>
        /// Builds the full canonical host. <paramref name="startingCredits"/>
        /// defaults to <see cref="EconomySystem.CanonicalMatchStartingCreditsAE"/>
        /// (3.000 AE, D-077 — the manifest start balance), so every
        /// Unity-hosted match opens with the classic loop economy; tests and
        /// tools that need a different balance pass it explicitly.
        /// <paramref name="enableSkirmishAi"/> defaults to ON: the canonical
        /// demo match plays against the MS-1 skirmish AI (slot 1, Legion —
        /// docs/tech/AIArchitecture.md). Debug scenes and harnesses that need
        /// an AI-less match (e.g. PathfindingTestBootstrap) opt out
        /// explicitly.
        /// </summary>
        public void InitializeMatch(ulong seed, ushort width = 128, ushort height = 128, int maxUnits = 2048,
            long startingCredits = EconomySystem.CanonicalMatchStartingCreditsAE, bool enableSkirmishAi = true)
        {
            MatchConfig config = MatchConfig.LocalVsAi(seed, width, height, maxUnits, startingCredits);
            if (!enableSkirmishAi) config.AiSlots = Array.Empty<byte>();
            InitializeMatch(config);
        }

        /// <summary>
        /// Builds the complete host from a validated A6 configuration. The
        /// source is cloned before any state is created, so later caller
        /// mutation cannot change slot authority, factions, AI ownership or
        /// input delay of a running match.
        /// </summary>
        public void InitializeMatch(MatchConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _config = config.ValidateAndClone();

            _seed = _config.Seed;
            _mapWidth = _config.MapWidth;
            _mapHeight = _config.MapHeight;
            _maxUnits = _config.EntityCapacity;

            _transport = null;
            _aiTransport = null;
            _relayClient = _config.Transport;
            _kernelStarted = false;
            AiSession = null;
            AiIngress = null;
            SkirmishAi = null;

            // The two lab switches are process-wide statics and a match is not.
            // Without this, a reveal set while watching the AI in a skirmish
            // would still be on after returning to the menu and joining a
            // lobby — a maphack nobody consciously switched on. Both refuse to
            // engage in a relay match; see FogRevealDebug for why the reveal in
            // particular is not merely unsporting but reaches the state hash.
            FogRevealDebug.ResetForMatch(_relayClient != null);
            MatchSpeedDebug.ResetForMatch(_relayClient != null);

            var random = new SimRandom(_seed);
            var logger = new UnityNovaLogger();
            Kernel = new SimulationKernel(random, logger);

            Entities = new EntityManager(_maxUnits);
            Pathfinding = new PathfindingSystem(_mapWidth, _mapHeight);
            Movement = new MovementSystem(Entities, Pathfinding);
            Economy = new EconomySystem(Entities, _config.StartingCredits);
            Construction = new Simulation.Construction.ConstructionSystem(Entities, Economy, Pathfinding.CostField);
            Production = new Simulation.Production.ProductionSystem(Entities, Economy, Construction);
            FogOfWar = new FogOfWarSystem(Entities, Construction, Economy, teamCount: 2, _mapWidth, _mapHeight);
            Combat = new CombatSystem(Entities, FogOfWar, Economy);
            Victory = new Simulation.Victory.VictorySystem(Entities, Construction);

            Session = new MatchSession(_config.LocalSlot, _config.ActiveSlots, _config.InputDelayTicks);
            Ingress = new CommandIngress(Session);
            if (_relayClient != null)
            {
                _relayClient.BindIngress(Ingress);
            }
            else
            {
                _transport = new LocalLoopbackTransport(Ingress);
            }

            if (_config.AiSlots.Length == 1)
            {
                byte aiSlot = _config.AiSlots[0];
                // The MS-1 skirmish opponent is a session PEER (AIArchitecture.md
                // section 1): it owns a slot-1 session and ingress of its own,
                // so its commands are sealed by the same session authority as a
                // human's — the AI never chooses a slot, a sequence or a target
                // tick itself. The peer transport forwards its records into the
                // host ingress's validating intake, byte-for-byte the path a
                // network peer's records will take.
                AiSession = new MatchSession(aiSlot, _config.ActiveSlots, _config.InputDelayTicks);
                AiIngress = new CommandIngress(AiSession);
                _aiTransport = new AiPeerCommandTransport(AiIngress, Ingress);
                SkirmishAi = new SkirmishAiSystem(
                    aiPlayerId: aiSlot,
                    new AiFactionProfile(_config.FactionPerSlot[aiSlot].ToString(),
                        targetPowerMargin: 0,     // power plant only when the margin would go negative
                        targetArmySize: 12,
                        attackSquadThreshold: 6,
                        targetHarvesterCount: 2),
                    AiIngress, Entities, Economy, Construction, Production, FogOfWar, Victory);
            }

            // Canonical tick order (SimulationCore.md section 2): economy
            // (phases 2/3), construction and production (phases 4/5) BEFORE
            // pathfinding/movement (phase 6), then the FoW recompute, then
            // combat, and match/victory logic LAST. A unit spawned by
            // production in tick T carries no movement order yet and moves
            // earliest in tick T+1. The skirmish AI sits between combat and
            // victory: its decisions read the POST-COMBAT state of the
            // executing tick, and victory still judges last — including the
            // outcome of orders the AI just submitted for T+1 (D-056:
            // "Auswertung nach Combat am Tickende"). The AI is a session
            // sidecar of this host: the headless determinism harness
            // (tools/Nova.SimRunner) registers the plain eight G1 systems.
            Kernel.RegisterSystem(Economy);
            Kernel.RegisterSystem(Construction);
            Kernel.RegisterSystem(Production);
            Kernel.RegisterSystem(Pathfinding);
            Kernel.RegisterSystem(Movement);
            Kernel.RegisterSystem(FogOfWar);
            Kernel.RegisterSystem(Combat);
            if (SkirmishAi != null)
            {
                Kernel.RegisterSystem(SkirmishAi);
            }
            Kernel.RegisterSystem(Victory);

            Kernel.BindCommands(new UnitCommandStateView(Entities, Pathfinding, Economy, Construction, Production), Ingress);
        }

        /// <summary>
        /// Starts a freshly initialized kernel. A relay-backed kernel may be
        /// started exactly once: restarting it would reset the simulation
        /// tick while the remote peer keeps advancing.
        /// </summary>
        public bool StartMatch()
        {
            if (_relayClient != null && _kernelStarted)
            {
                Debug.LogError("[MatchRunner] Relay match start/resume refused: a started network kernel cannot be reset.");
                return false;
            }
            if (Kernel == null)
            {
                InitializeMatch(MatchConfig.LocalVsAi(
                    _seed, _mapWidth, _mapHeight, _maxUnits,
                    EconomySystem.CanonicalMatchStartingCreditsAE));
            }

            _timeAccumulator = 0f;
            Kernel.Start();
            _kernelStarted = true;
            return true;
        }

        /// <summary>Pauses a local match. Relay matches cannot pause independently of their peer.</summary>
        public bool PauseMatch()
        {
            if (_relayClient != null)
            {
                Debug.LogError("[MatchRunner] Local pause refused for a relay match.");
                return false;
            }
            if (IsRunning)
            {
                Kernel.Stop();
            }
            return true;
        }

        /// <summary>Ordered terminal stop used only by the network lifecycle owner.</summary>
        internal void StopNetworkMatch()
        {
            _timeAccumulator = 0f;
            if (IsRunning) Kernel.Stop();
        }

        private void Update()
        {
            // Network progress must never depend on simulation progress: the
            // relay is pumped during handshake, pause and lockstep stalls.
            _relayClient?.Poll();

            if (_relayClient != null && _relayClient.Phase == RelayClientPhase.Ended)
            {
                StopNetworkMatch();
                return;
            }
            if (!IsRunning) return;

            // Diagnostic fast-forward (MatchSpeedDebug), off at 1x.
            // The multiplier scales WALL-CLOCK time, not the simulation: the
            // kernel still steps whole 10-Hz ticks, in order, one at a time —
            // a match watched at 4x ends on the same tick with the same state
            // hash. A relay match is excluded, because two peers at different
            // rates just wait for each other in the lockstep barrier.
            int speed = _relayClient == null ? MatchSpeedDebug.Multiplier : 1;
            _timeAccumulator += Time.deltaTime * speed;

            // Step fixed 10-Hz simulation ticks
            while (_timeAccumulator >= TickDeltaTime)
            {
                if (!TryStepFixedTick())
                {
                    // Do not bank wall-clock time during a stall. Releasing
                    // the barrier resumes at 10 Hz instead of fast-forwarding
                    // through a catch-up burst.
                    _timeAccumulator = 0f;
                    break;
                }
                _timeAccumulator -= TickDeltaTime;
            }
        }

        /// <summary>
        /// One lockstep iteration: seal the batch due at the next tick
        /// through the ingress, submit it to the kernel (the only intake),
        /// step the kernel, then advance the session tick.
        /// <para>
        /// The AI peer clock (<see cref="AiSession"/>) advances BEFORE the
        /// step so it reads the tick about to execute: the AI system decides
        /// inside tick T, and its intents (enqueue T, target T+1) are then
        /// sealed into the batch of T+1 by the next iteration. Advancing it
        /// after the step like the human session would target the
        /// already-sealed tick T instead — such records pass the intake but
        /// are never drained, so every AI order would die in the pending
        /// queue.
        /// </para>
        /// </summary>
        private bool TryStepFixedTick()
        {
            if (_relayClient != null)
            {
                if (!_relayClient.TryStepTick(Kernel)) return false;
                return true;
            }

            uint nextTick = Kernel.CurrentTick.Value + 1;
            CommandBatch batch = Ingress.SealTickBatch(nextTick);
            if (batch.Count > 0)
            {
                Kernel.SubmitBatch(batch);
            }
            AiSession?.AdvanceTick();
            Kernel.StepTick();
            Session.AdvanceTick();
            return true;
        }

        private void OnDestroy()
        {
            if (IsRunning)
            {
                Kernel.Stop();
            }
            _relayClient?.Disconnect();
        }
    }
}
