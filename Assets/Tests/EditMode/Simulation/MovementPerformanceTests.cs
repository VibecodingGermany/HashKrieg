using System.Diagnostics;
using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    [TestFixture]
    public class MovementPerformanceTests
    {
        [Test]
        public void MovementSystem_1000MovingUnits_ExecutesUnderBudget()
        {
            const int unitCount = 1000;
            const int tickCount = 50;

            var entities = new EntityManager(unitCount + 10);
            var pathfinding = new PathfindingSystem(128, 128);
            var movement = new MovementSystem(entities, pathfinding);

            var kernel = new SimulationKernel(new SimRandom(999));
            kernel.RegisterSystem(pathfinding);
            kernel.RegisterSystem(movement);
            kernel.Start();

            var target = new GridPos2D(100, 100);
            pathfinding.RequestFlowField(target);

            // Spawn 1,000 active units distributed across grid
            for (int i = 0; i < unitCount; i++)
            {
                float startX = 10f + (i % 30);
                float startY = 10f + (i / 30);
                EntityId id = entities.SpawnUnit(1, new Transform2D(startX, startY), 4.5f, 0.4f);
                ref UnitState unit = ref entities.GetUnitRef(id);
                unit.SetTarget(target);
            }

            // Warmup tick
            kernel.StepTick();

            // Benchmark 50 ticks
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < tickCount; i++)
            {
                kernel.StepTick();
            }
            sw.Stop();

            double totalMs = sw.Elapsed.TotalMilliseconds;
            double avgMsPerTick = totalMs / tickCount;

            UnityEngine.Debug.Log($"[Performance Benchmark] 1,000 Units across {tickCount} ticks: Total = {totalMs:F2}ms, Avg = {avgMsPerTick:F3}ms/tick.");

            // Performance budget gate: Average tick execution time must be under 2.0 ms per tick
            Assert.Less(avgMsPerTick, 2.0, $"Average tick time {avgMsPerTick:F3}ms exceeded budget limit of 2.0ms");
        }
    }
}
