using System;
using UnityEngine;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Combat;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;
using Nova.Simulation.Vision;

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
    /// </summary>
    [DisallowMultipleComponent]
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

        public SimulationKernel Kernel { get; private set; }
        public EntityManager Entities { get; private set; }
        public PathfindingSystem Pathfinding { get; private set; }
        public MovementSystem Movement { get; private set; }

        /// <summary>The canonical economy (credits, power, finite Aetherium harvest; SimulationCore.md section 2, phases 2/3).</summary>
        public EconomySystem Economy { get; private set; }

        /// <summary>The canonical Fog of War (single committed sight for Combat/AI/snapshot/rendering, D-058).</summary>
        public FogOfWarSystem FogOfWar { get; private set; }

        /// <summary>The canonical FoW-gated hitscan combat (SimulationCore.md section 2, order step 8).</summary>
        public CombatSystem Combat { get; private set; }

        /// <summary>Session authority binding the local player slot (MS-1: slot 0 of {0, 1}).</summary>
        public MatchSession Session { get; private set; }

        /// <summary>The only command entry point for UI and AI (intent in, sealed batch out).</summary>
        public CommandIngress Ingress { get; private set; }

        public bool IsRunning => Kernel != null && Kernel.IsRunning;

        public void InitializeMatch(ulong seed, ushort width = 128, ushort height = 128, int maxUnits = 2048)
        {
            _seed = seed;
            _mapWidth = width;
            _mapHeight = height;
            _maxUnits = maxUnits;

            var random = new SimRandom(_seed);
            var logger = new UnityNovaLogger();
            Kernel = new SimulationKernel(random, logger);

            Entities = new EntityManager(_maxUnits);
            Pathfinding = new PathfindingSystem(_mapWidth, _mapHeight);
            Movement = new MovementSystem(Entities, Pathfinding);
            Economy = new EconomySystem(Entities);
            FogOfWar = new FogOfWarSystem(Entities, teamCount: 2, _mapWidth, _mapHeight);
            Combat = new CombatSystem(Entities, FogOfWar);

            // Canonical tick order (SimulationCore.md section 2): economy
            // (phases 2/3) BEFORE pathfinding/movement (phase 6), then the
            // FoW recompute, then combat.
            Kernel.RegisterSystem(Economy);
            Kernel.RegisterSystem(Pathfinding);
            Kernel.RegisterSystem(Movement);
            Kernel.RegisterSystem(FogOfWar);
            Kernel.RegisterSystem(Combat);

            Session = new MatchSession(localSlot: 0, activeSlots: new byte[] { 0, 1 }, inputDelayTicks: 1);
            Ingress = new CommandIngress(Session);
            _transport = new LocalLoopbackTransport(Ingress);
            Kernel.BindCommands(new UnitCommandStateView(Entities, Pathfinding, Economy), Ingress);
        }

        public void StartMatch()
        {
            if (Kernel == null)
            {
                InitializeMatch(_seed, _mapWidth, _mapHeight, _maxUnits);
            }

            _timeAccumulator = 0f;
            Kernel.Start();
        }

        public void PauseMatch()
        {
            if (IsRunning)
            {
                Kernel.Stop();
            }
        }

        private void Update()
        {
            if (!IsRunning) return;

            _timeAccumulator += Time.deltaTime;

            // Step fixed 10-Hz simulation ticks
            while (_timeAccumulator >= TickDeltaTime)
            {
                StepFixedTick();
                _timeAccumulator -= TickDeltaTime;
            }
        }

        /// <summary>
        /// One lockstep iteration: seal the batch due at the next tick
        /// through the ingress, submit it to the kernel (the only intake),
        /// step the kernel, then advance the session tick.
        /// </summary>
        private void StepFixedTick()
        {
            uint nextTick = Kernel.CurrentTick.Value + 1;
            CommandBatch batch = Ingress.SealTickBatch(nextTick);
            if (batch.Count > 0)
            {
                Kernel.SubmitBatch(batch);
            }
            Kernel.StepTick();
            Session.AdvanceTick();
        }

        private void OnDestroy()
        {
            if (IsRunning)
            {
                Kernel.Stop();
            }
        }
    }
}
