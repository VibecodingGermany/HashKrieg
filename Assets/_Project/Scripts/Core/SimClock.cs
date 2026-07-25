namespace Nova.Core
{
    /// <summary>
    /// Canonical simulation clock constants (docs/tech/SimulationCore.md
    /// sections 1 and 2): the authoritative simulation runs synchronously at
    /// exactly 10 Hz, so one tick advances simulation time by exactly 0.1
    /// seconds. Hosts accumulate wall-clock time against
    /// <see cref="TickDeltaSeconds"/>; simulation systems integrate durations
    /// in whole ticks. This is the single source for the tick rate — no
    /// system or host may define its own.
    /// </summary>
    public static class SimClock
    {
        /// <summary>Authoritative ticks per second (SimulationCore.md section 1).</summary>
        public const int TicksPerSecond = 10;

        /// <summary>Simulation seconds advanced by one tick: 1 / TicksPerSecond.</summary>
        public const float TickDeltaSeconds = 1f / TicksPerSecond;
    }
}
