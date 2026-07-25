using System;
using Nova.Core;
using Nova.Simulation;

namespace Nova.SimRunner
{
    internal class ConsoleLogger : INovaLogger
    {
        public bool IsEnabled(LogLevel level) => true;

        public void Log(LogLevel level, string message)
        {
            Console.WriteLine($"[{level}] {message}");
        }

        public void LogTrace(string message) => Log(LogLevel.Trace, message);
        public void LogDebug(string message) => Log(LogLevel.Debug, message);
        public void LogInfo(string message) => Log(LogLevel.Info, message);
        public void LogWarn(string message) => Log(LogLevel.Warn, message);
        public void LogError(string message) => Log(LogLevel.Error, message);
    }

    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("=== Project Nova - Headless SimRunner ===");

            const ulong seed = 0xAE70123456789000UL;
            var logger = new ConsoleLogger();
            var random = new SimRandom(seed);
            var kernel = new SimulationKernel(random, logger);

            var entities = new Nova.Simulation.State.EntityManager(2048);
            var pathfinding = new Nova.Simulation.Pathfinding.PathfindingSystem(128, 128);
            var movement = new Nova.Simulation.Movement.MovementSystem(entities, pathfinding);

            kernel.RegisterSystem(pathfinding);
            kernel.RegisterSystem(movement);
            kernel.Start();

            var target = new Nova.Simulation.Pathfinding.GridPos2D(64, 64);
            pathfinding.RequestFlowField(target);

            // Spawn 1,000 active units distributed across grid
            for (int i = 0; i < 1000; i++)
            {
                float startX = 10f + (i % 30);
                float startY = 10f + (i / 30);
                var id = entities.SpawnUnit(1, new Nova.Simulation.State.Transform2D(startX, startY), 4.5f, 0.4f);
                ref var unit = ref entities.GetUnitRef(id);
                unit.SetTarget(target);
            }

            Console.WriteLine($"[SimRunner] Spawned {entities.ActiveCount} units. Running 100 simulation ticks...");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                kernel.StepTick();
            }
            sw.Stop();

            double totalMs = sw.Elapsed.TotalMilliseconds;
            double avgMs = totalMs / 100.0;
            Console.WriteLine($"[Performance Result] 1,000 Units across 100 Ticks: Total = {totalMs:F2}ms, Avg = {avgMs:F3}ms/tick.");

            ulong finalHash = kernel.CalculateStateHash();
            Console.WriteLine($"[Success] Simulation reached {kernel.CurrentTick}. Final Hash = 0x{finalHash:X16}");

            kernel.Stop();
        }
    }
}
