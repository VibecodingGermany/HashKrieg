using System;
using System.Diagnostics;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Snapshots;

namespace Nova.SimRunner
{
    /// <summary>
    /// Stopwatch decorator around a stateful simulation system. The kernel
    /// only ever sees the wrapper (same <see cref="IStatefulSimSystem"/>
    /// contract, same snapshot block id, same tick behaviour); the measured
    /// time is a pure read-only observation of
    /// <see cref="ExecuteTick"/> and never feeds back into the simulation, so
    /// the canonical state hash is unaffected. All measurement logic lives in
    /// the harness — simulation sources stay free of timing code.
    /// </summary>
    internal sealed class TimedStatefulSimSystem : IStatefulSimSystem
    {
        private readonly IStatefulSimSystem _inner;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public TimedStatefulSimSystem(IStatefulSimSystem inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public IStatefulSimSystem Inner => _inner;

        /// <summary>Milliseconds the inner system spent in its most recent tick.</summary>
        public double LastTickMs { get; private set; }

        /// <summary>Accumulated milliseconds over all ticks since construction.</summary>
        public double TotalMs { get; private set; }

        public string Name => _inner.Name;

        public void Initialize(SimulationKernel kernel) => _inner.Initialize(kernel);

        public void ExecuteTick(Tick tick)
        {
            _stopwatch.Restart();
            _inner.ExecuteTick(tick);
            _stopwatch.Stop();
            LastTickMs = _stopwatch.Elapsed.TotalMilliseconds;
            TotalMs += LastTickMs;
        }

        public void Shutdown() => _inner.Shutdown();

        public ushort StateBlockId => _inner.StateBlockId;

        public void WriteState(SnapshotBlockWriter writer) => _inner.WriteState(writer);

        public bool TryValidateState(ReadOnlySpan<byte> blockContent) => _inner.TryValidateState(blockContent);

        public bool TryRestoreState(ReadOnlySpan<byte> blockContent) => _inner.TryRestoreState(blockContent);
    }
}
