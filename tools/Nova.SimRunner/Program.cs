using System;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;

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

    /// <summary>
    /// Headless benchmark and determinism smoke check for the canonical
    /// kernel. The identical scenario runs twice; the canonical state hash
    /// must be bit-identical across runs (F-005 regression: hashing is
    /// read-only and covers the full authoritative state).
    /// </summary>
    internal static class Program
    {
        private const ulong Seed = 0xAE70123456789000UL;
        private const int UnitCount = 1000;
        private const int TickCount = 100;

        private static int Main(string[] args)
        {
            Console.WriteLine("=== Project Nova - Headless SimRunner ===");

            ulong hashRun1 = RunOnce(runLabel: "Run 1", logger: new ConsoleLogger());
            ulong hashRun2 = RunOnce(runLabel: "Run 2", logger: NullNovaLogger.Instance);

            Console.WriteLine($"[Determinism] Run 1 Hash = 0x{hashRun1:X16}");
            Console.WriteLine($"[Determinism] Run 2 Hash = 0x{hashRun2:X16}");
            if (hashRun1 != hashRun2)
            {
                Console.WriteLine("[Failure] State hashes differ between identical runs.");
                return 1;
            }

            Console.WriteLine("[Success] State hash is stable across identical runs.");
            return 0;
        }

        /// <summary>
        /// Builds the canonical host (kernel + entity store + pathfinding +
        /// movement + session/ingress command pipeline), spawns the load
        /// fixture, drives all units through the sealed command intake and
        /// steps the kernel. Returns the final canonical state hash.
        /// </summary>
        private static ulong RunOnce(string runLabel, INovaLogger logger)
        {
            var kernel = new SimulationKernel(new SimRandom(Seed), logger);

            var entities = new EntityManager(2048);
            var pathfinding = new PathfindingSystem(128, 128);
            var movement = new MovementSystem(entities, pathfinding);

            kernel.RegisterSystem(pathfinding);
            kernel.RegisterSystem(movement);

            var session = new MatchSession(localSlot: 0, activeSlots: new byte[] { 0, 1 }, inputDelayTicks: 1);
            var ingress = new CommandIngress(session);
            _ = new LocalLoopbackTransport(ingress);
            kernel.BindCommands(new UnitCommandStateView(entities, pathfinding), ingress);

            kernel.Start();

            // Spawn 1,000 active units distributed across the grid, owned by the local slot.
            var rawIds = new uint[UnitCount];
            for (int i = 0; i < UnitCount; i++)
            {
                float startX = 10f + (i % 30);
                float startY = 10f + (i / 30);
                EntityId id = entities.SpawnUnit(0, new Transform2D(startX, startY), 4.5f, 0.4f);
                rawIds[i] = UnitCommandStateView.ToRawEntityId(id);
            }

            // Real command intake: Move orders for the whole fixture in
            // batches of MaxEntityIdsPerCommand, sealed through the ingress.
            var moveTarget = new GridPos2D(64, 64);
            for (int offset = 0; offset < UnitCount; offset += CommandLimits.MaxEntityIdsPerCommand)
            {
                int count = Math.Min(CommandLimits.MaxEntityIdsPerCommand, UnitCount - offset);
                var ids = new uint[count];
                Array.Copy(rawIds, offset, ids, 0, count);
                var payload = new MovePayload(ids, SimFixed.FromInt(moveTarget.X), SimFixed.FromInt(moveTarget.Y));
                CommandIngressResult result = ingress.TrySubmitIntent(CommandIntent.Create(payload), out CommandRejectReason reason);
                if (result != CommandIngressResult.Accepted)
                {
                    throw new InvalidOperationException($"Move intent rejected: {result} ({reason}).");
                }
            }

            Console.WriteLine($"[{runLabel}] Spawned {entities.ActiveCount} units. Running {TickCount} simulation ticks...");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < TickCount; i++)
            {
                uint nextTick = kernel.CurrentTick.Value + 1;
                CommandBatch batch = ingress.SealTickBatch(nextTick);
                if (batch.Count > 0)
                {
                    kernel.SubmitBatch(batch);
                }
                kernel.StepTick();
                session.AdvanceTick();
            }
            sw.Stop();

            double totalMs = sw.Elapsed.TotalMilliseconds;
            double avgMs = totalMs / TickCount;
            Console.WriteLine($"[Performance Result][{runLabel}] {UnitCount} Units across {TickCount} Ticks: Total = {totalMs:F2}ms, Avg = {avgMs:F3}ms/tick.");

            ulong finalHash = kernel.CalculateStateHash();
            Console.WriteLine($"[{runLabel}] Simulation reached {kernel.CurrentTick}. Final Hash = 0x{finalHash:X16}");

            kernel.Stop();
            return finalHash;
        }
    }
}
