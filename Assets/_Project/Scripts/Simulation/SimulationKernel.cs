using System;
using System.Collections.Generic;
using Nova.Core;

namespace Nova.Simulation
{
    /// <summary>
    /// Core Lockstep simulation runner.
    /// Manages deterministic tick stepping, command queue processing, PRNG state, and system execution.
    /// Engine-decoupled (no UnityEngine dependency).
    /// </summary>
    public sealed class SimulationKernel : ICommandSink
    {
        private readonly List<ISimSystem> _systems = new List<ISimSystem>();
        private readonly List<CommandEnvelope> _commandBuffer = new List<CommandEnvelope>();
        private readonly Queue<CommandEnvelope> _pendingCommands = new Queue<CommandEnvelope>();

        public Tick CurrentTick { get; private set; } = Tick.Zero;
        public ISimRandom Random { get; }
        public INovaLogger Logger { get; }
        public bool IsRunning { get; private set; }

        public IReadOnlyList<ISimSystem> Systems => _systems;

        public SimulationKernel(ISimRandom random, INovaLogger logger = null)
        {
            Random = random ?? throw new ArgumentNullException(nameof(random));
            Logger = logger ?? NullNovaLogger.Instance;
        }

        public void RegisterSystem(ISimSystem system)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            if (IsRunning) throw new InvalidOperationException("Cannot register systems while simulation kernel is running.");
            
            _systems.Add(system);
        }

        public void Start(Tick initialTick = default)
        {
            CurrentTick = initialTick;
            IsRunning = true;

            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].Initialize(this);
            }

            Logger.LogInfo($"SimulationKernel started at {CurrentTick} with {_systems.Count} systems.");
        }

        public bool SubmitCommand(in CommandEnvelope envelope)
        {
            if (!IsRunning) return false;

            _pendingCommands.Enqueue(envelope);
            return true;
        }

        /// <summary>
        /// Advances the simulation by exactly one tick step.
        /// Processes scheduled commands and executes all registered systems deterministically.
        /// </summary>
        public void StepTick()
        {
            if (!IsRunning) return;

            CurrentTick++;

            // Collect commands scheduled for this tick
            _commandBuffer.Clear();
            int pendingCount = _pendingCommands.Count;
            for (int i = 0; i < pendingCount; i++)
            {
                CommandEnvelope cmd = _pendingCommands.Dequeue();
                if (cmd.TargetTick <= CurrentTick)
                {
                    _commandBuffer.Add(cmd);
                }
                else
                {
                    _pendingCommands.Enqueue(cmd);
                }
            }

            // Execute systems in exact registration order
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].ExecuteTick(CurrentTick);
            }
        }

        public void Stop()
        {
            if (!IsRunning) return;

            for (int i = _systems.Count - 1; i >= 0; i--)
            {
                _systems[i].Shutdown();
            }

            IsRunning = false;
            Logger.LogInfo($"SimulationKernel stopped at {CurrentTick}.");
        }

        /// <summary>
        /// Computes a hash check value of the current kernel state for determinism verification.
        /// </summary>
        public ulong CalculateStateHash()
        {
            unchecked
            {
                ulong hash = 17;
                hash = hash * 31 + CurrentTick.Value;
                hash = hash * 31 + Random.NextUInt();
                return hash;
            }
        }
    }
}
