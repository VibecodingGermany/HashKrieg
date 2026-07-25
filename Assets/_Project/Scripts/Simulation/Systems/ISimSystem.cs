using Nova.Core;

namespace Nova.Simulation
{
    /// <summary>
    /// Contract for deterministic simulation sub-systems (e.g. Economy, Movement, Combat, Pathfinding).
    /// </summary>
    public interface ISimSystem
    {
        string Name { get; }
        void Initialize(SimulationKernel kernel);
        void ExecuteTick(Tick tick);
        void Shutdown();
    }
}
