using System;
using Nova.Core;

namespace Nova.Simulation.Pathfinding
{
    /// <summary>
    /// Deterministic simulation system for Flow-Field Pathfinding.
    /// Manages cost fields, integration wave propagation, and flow field generation.
    /// </summary>
    public sealed class PathfindingSystem : ISimSystem
    {
        public string Name => "PathfindingSystem";

        public CostField CostField { get; }
        public IntegrationField IntegrationField { get; }
        public FlowField FlowField { get; }

        public PathfindingSystem(ushort width, ushort height)
        {
            CostField = new CostField(width, height);
            IntegrationField = new IntegrationField(width, height);
            FlowField = new FlowField(width, height);
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo($"[{Name}] Initialized Flow-Field Grid ({CostField.Width}x{CostField.Height}).");
        }

        public void RequestFlowField(GridPos2D destination)
        {
            IntegrationField.Generate(CostField, destination);
            FlowField.Generate(IntegrationField, CostField);
        }

        public void ExecuteTick(Tick tick)
        {
            // Flow-Field ticks process pending path requests or dynamic cost field updates
        }

        public void Shutdown()
        {
        }
    }
}
