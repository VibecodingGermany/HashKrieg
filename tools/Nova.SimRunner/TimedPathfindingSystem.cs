using System.Diagnostics;
using Nova.Simulation.Pathfinding;

namespace Nova.SimRunner
{
    /// <summary>
    /// Pathfinding system with an externally observable flow-field generation
    /// phase. In the canonical kernel the pathfinding cost does NOT sit in
    /// <see cref="PathfindingSystem.ExecuteTick"/> (empty by design) but in
    /// <see cref="PathfindingSystem.RequestFlowField"/>, which the command
    /// executor invokes synchronously while applying Move commands. The only
    /// outside interception point is the virtual method the base class
    /// explicitly provides for this harness. Timing is read-only
    /// accumulation; it never influences the simulation.
    /// </summary>
    internal sealed class TimedPathfindingSystem : PathfindingSystem
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public TimedPathfindingSystem(ushort width, ushort height)
            : base(width, height)
        {
        }

        /// <summary>
        /// Milliseconds spent in flow-field generation since the last
        /// <see cref="BeginTick"/> (a tick may apply several Move commands).
        /// </summary>
        public double FlowFieldMsThisTick { get; private set; }

        /// <summary>Accumulated flow-field generation milliseconds.</summary>
        public double FlowFieldTotalMs { get; private set; }

        /// <summary>Resets the per-tick accumulator; called by the runner before each StepTick.</summary>
        public void BeginTick()
        {
            FlowFieldMsThisTick = 0.0;
        }

        public override void RequestFlowField(GridPos2D destination)
        {
            _stopwatch.Restart();
            base.RequestFlowField(destination);
            _stopwatch.Stop();
            double ms = _stopwatch.Elapsed.TotalMilliseconds;
            FlowFieldMsThisTick += ms;
            FlowFieldTotalMs += ms;
        }
    }
}
