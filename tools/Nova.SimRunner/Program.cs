using System;
using System.Collections.Generic;
using System.IO;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Combat;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;
using Nova.Simulation.Vision;

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
    /// Headless runner for the canonical kernel. Without arguments it
    /// executes the original determinism/benchmark demo (below). With
    /// <c>--scenario SCALE_500_PRECOMBAT</c> it runs the V4/V5a performance
    /// harness (see <see cref="Scale500PrecombatScenario"/>), with
    /// <c>--scenario DETERMINISM_10000</c> the G1/V1 cross-platform
    /// determinism harness (see <see cref="Determinism10000Scenario"/>).
    /// </summary>
    internal static class Program
    {
        private const ulong Seed = 0xAE70123456789000UL;
        private const int UnitCount = 1000;
        private const int TickCount = 100;

        private static int Main(string[] args)
        {
            Console.WriteLine("=== Project Nova - Headless SimRunner ===");

            string scenarioId = ParseOption(args, "--scenario");
            if (scenarioId != null)
            {
                return RunScenarioMode(args, scenarioId);
            }

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

        // ----------------------------------------------------------------
        // Scenario mode (V4/V5a performance harness)
        // ----------------------------------------------------------------

        /// <summary>
        /// CLI: <c>--scenario SCALE_500_PRECOMBAT [--runs 3]
        /// [--warmup-seconds 30] [--measure-seconds 120] [--agents 500]
        /// [--out &lt;directory&gt;]</c>. Defaults are the contract values of
        /// quality/scenarios/mvp-v1.json (performanceMethod); the artifacts
        /// always record the actually used values. Local runs on non-D-052
        /// hardware are diagnosis, never gate evidence.
        /// </summary>
        private static int RunScenarioMode(string[] args, string scenarioId)
        {
            if (scenarioId == DeterminismOptions.ScenarioId)
            {
                return RunDeterminismScenarioMode(args);
            }
            if (scenarioId != ScenarioOptions.ScenarioId)
            {
                Console.Error.WriteLine(
                    $"[Failure] Unknown scenario '{scenarioId}'. Supported: {ScenarioOptions.ScenarioId}, {DeterminismOptions.ScenarioId}.");
                return 2;
            }

            var options = new ScenarioOptions();
            if (!TryParseIntOption(args, "--runs", ref options.Runs)
                || !TryParseIntOption(args, "--warmup-seconds", ref options.WarmupSeconds)
                || !TryParseIntOption(args, "--measure-seconds", ref options.MeasureSeconds)
                || !TryParseIntOption(args, "--agents", ref options.AgentCount))
            {
                Console.Error.WriteLine("[Failure] Invalid numeric option.");
                return 2;
            }
            if (options.Runs < 1 || options.MeasureSeconds < 1 || options.AgentCount < 1 || options.WarmupSeconds < 0)
            {
                Console.Error.WriteLine("[Failure] Options out of range (runs/measure-seconds/agents >= 1, warmup-seconds >= 0).");
                return 2;
            }

            string outDir = ParseOption(args, "--out")
                ?? Path.Combine("output", "perf",
                    $"{ScenarioOptions.ScenarioId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}Z");

            Console.WriteLine(
                $"[Scenario] {ScenarioOptions.ScenarioId}: agents={options.AgentCount}, " +
                $"warmup={options.WarmupSeconds}s, runs={options.Runs} x {options.MeasureSeconds}s, " +
                $"seed=0x{options.Seed:X16}");
            Console.WriteLine("[Scenario] Method: quality/scenarios/mvp-v1.json performanceMethod (D-052/D-063).");
            Console.WriteLine("[Scenario] NOTE: values outside the Windows-x64 D-052 reference method are DIAGNOSIS, not gate evidence.");

            ScenarioResult result;
            try
            {
                result = Scale500PrecombatScenario.Run(options, NullNovaLogger.Instance);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"[Failure] Scenario crashed: {exception}");
                result = new ScenarioResult { NoCrash = false, MemoryAssertion = MemoryAssertionVerdict.Fail };
            }

            ScenarioArtifacts.WriteScenarioArtifacts(options, outDir, result);
            Console.WriteLine($"[Artifacts] Written to {Path.GetFullPath(outDir)} (NOT gate evidence; not committed).");
            PrintSummary(result);

            if (!result.NoCrash)
            {
                Console.WriteLine("[Assertion] no-crash = FAIL");
                return 1;
            }
            Console.WriteLine("[Assertion] no-crash = PASS");
            switch (result.MemoryAssertion)
            {
                case MemoryAssertionVerdict.NotApplicable:
                    Console.WriteLine(
                        "[Assertion] no-unbounded-memory-growth = NOT-APPLICABLE " +
                        $"(measurement window < {Scale500PrecombatScenario.MinMeasurementSecondsForMemoryAssertion}s: " +
                        "allocator/JIT/GC warm-up dominates short diagnostic windows; artifact samples [1] " +
                        "without gate claim — a skipped assertion in a REAL gate run would be a fail)");
                    break;
                case MemoryAssertionVerdict.Pass:
                    Console.WriteLine(
                        "[Assertion] no-unbounded-memory-growth = PASS " +
                        $"(rule: median of last-tenth retained probes (>= last 10) <= " +
                        $"{Scale500PrecombatScenario.MemoryGrowthTolerance:F2}x retained baseline after warmup, full GC, per run)");
                    break;
                default:
                    Console.WriteLine(
                        "[Assertion] no-unbounded-memory-growth = FAIL " +
                        $"(rule: median of last-tenth retained probes (>= last 10) <= " +
                        $"{Scale500PrecombatScenario.MemoryGrowthTolerance:F2}x retained baseline after warmup, full GC, per run)");
                    break;
            }
            return result.MemoryAssertion == MemoryAssertionVerdict.Fail ? 1 : 0;
        }

        // ----------------------------------------------------------------
        // Scenario mode (G1/V1 determinism harness)
        // ----------------------------------------------------------------

        /// <summary>
        /// CLI: <c>--scenario DETERMINISM_10000 [--verify &lt;other-platform.json&gt;]
        /// [--out &lt;directory&gt;] [--platform &lt;tag&gt;] [--ticks 10000]
        /// [--checkpoint-interval 100]</c>. Runs the fixed 10,000-tick
        /// canonical replay on this machine and writes the platform profile
        /// artifact plus the boolean assertions (D-062 naming) into the out
        /// directory (diagnosis material, gitignored — never gate evidence).
        /// Cross-platform workflow (SimulationCore.md section 9): run once on
        /// macOS arm64 and once on Windows x64, then re-run on either machine
        /// with <c>--verify</c> pointing at the other machine's
        /// scenario.DETERMINISM_10000.&lt;platform&gt;.json — exit code 0
        /// only when every checkpoint state hash and the final snapshot
        /// SHA-256 match exactly. Running twice on the same machine and
        /// verifying one artifact against the other is the local determinism
        /// baseline.
        /// </summary>
        private static int RunDeterminismScenarioMode(string[] args)
        {
            var options = new DeterminismOptions();
            if (!TryParseIntOption(args, "--ticks", ref options.Ticks)
                || !TryParseIntOption(args, "--checkpoint-interval", ref options.CheckpointIntervalTicks))
            {
                Console.Error.WriteLine("[Failure] Invalid numeric option.");
                return 2;
            }
            if (options.Ticks < 1 || options.CheckpointIntervalTicks < 1)
            {
                Console.Error.WriteLine("[Failure] Options out of range (ticks/checkpoint-interval >= 1).");
                return 2;
            }
            options.PlatformId = ParseOption(args, "--platform");
            options.VerifyPath = ParseOption(args, "--verify");

            string outDir = ParseOption(args, "--out")
                ?? Path.Combine("output", "determinism",
                    $"{DeterminismOptions.ScenarioId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}Z");

            Console.WriteLine(
                $"[Scenario] {DeterminismOptions.ScenarioId}: ticks={options.Ticks}, " +
                $"checkpoint-interval={options.CheckpointIntervalTicks}, seed=0x{options.Seed:X16}");
            Console.WriteLine("[Scenario] Contract: quality/scenarios/mvp-v1.json DETERMINISM_10000 (G1/V1); SimulationCore.md sections 7/9.");
            Console.WriteLine("[Scenario] NOTE: artifacts in output/ are DIAGNOSIS, not gate evidence (D-061/D-064).");

            DeterminismRunResult result;
            try
            {
                result = Determinism10000Scenario.Run(options, NullNovaLogger.Instance);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"[Failure] Scenario crashed: {exception}");
                return 1;
            }

            Console.WriteLine(
                $"[Run] generator {result.GeneratorSeconds:F1}s, playback {result.PlaybackSeconds:F1}s, " +
                $"replay {result.ReplayLength} bytes (sha256 {result.ReplaySha256})");
            Console.WriteLine(
                $"[Run] fingerprint 0x{result.FingerprintHash64:X16}, checkpoints {result.Checkpoints.Count}, " +
                $"final snapshot {result.FinalSnapshotLength} bytes (sha256 {result.FinalSnapshotSha256})");
            if (result.Checkpoints.Count > 0)
            {
                Console.WriteLine(
                    $"[Run] first checkpoint tick {result.Checkpoints[0].Tick} hash 0x{result.Checkpoints[0].StateHash64:X16}, " +
                    $"final state hash 0x{result.FinalStateHash:X16}");
            }

            if (!result.PlaybackVerified)
            {
                Console.Error.WriteLine($"[Failure] Playback of the self-generated replay diverged: {result.PlaybackFailure}");
                DeterminismArtifacts.WriteRunArtifacts(outDir, result, comparison: null);
                Console.Error.WriteLine($"[Artifacts] Written to {Path.GetFullPath(outDir)} (run FAILED self-verification).");
                return 1;
            }
            Console.WriteLine("[SelfCheck] Playback reproduced every recorded result and the recorded final state hash.");

            DeterminismComparison comparison = null;
            if (options.VerifyPath != null)
            {
                if (!DeterminismArtifacts.TryLoadProfile(options.VerifyPath, out PlatformProfile other, out string loadError))
                {
                    Console.Error.WriteLine($"[Failure] Cannot verify: {loadError} ({options.VerifyPath})");
                    return 2;
                }
                Console.WriteLine($"[Verify] Against {options.VerifyPath} (platform {other.PlatformId}).");
                comparison = Determinism10000Scenario.Compare(result, other);
                if (comparison.CheckpointsExact && comparison.SnapshotExact)
                {
                    Console.WriteLine("[Verify] All checkpoint state hashes and the final snapshot bytes match EXACTLY.");
                }
                else
                {
                    Console.Error.WriteLine($"[Verify] DIVERGENCE: {comparison.FirstDivergence}");
                }
            }

            DeterminismArtifacts.WriteRunArtifacts(outDir, result, comparison);
            Console.WriteLine($"[Artifacts] Written to {Path.GetFullPath(outDir)} (NOT gate evidence; not committed).");

            Console.WriteLine("[Assertion] managed-path-only = PASS (managed .NET lane; no Burst path exists here)");
            Console.WriteLine($"[Assertion] same-sources-and-determinism-defines = {(result.DeterminismDefineActive ? "PASS" : "FAIL")} " +
                              "(build self-report: NOVA_FIXED_POINT compiled in; sources linked from Assets/_Project)");
            if (comparison != null)
            {
                Console.WriteLine($"[Assertion] exact-state-hash-every-checkpoint = {(comparison.CheckpointsExact ? "PASS" : "FAIL")}");
                Console.WriteLine($"[Assertion] exact-final-snapshot-bytes = {(comparison.SnapshotExact ? "PASS" : "FAIL")}");
                return comparison.CheckpointsExact && comparison.SnapshotExact ? 0 : 1;
            }
            return result.DeterminismDefineActive ? 0 : 1;
        }

        private static void PrintSummary(ScenarioResult result)
        {
            if (result.Runs.Count == 0)
            {
                return;
            }

            Console.WriteLine();
            Console.WriteLine("=== DIAGNOSIS SUMMARY (nearest-rank, no interpolation, no outlier removal — validator semantics) ===");
            PrintMetricSummary("pathfindingMs (threshold P95 <= 4.0 ms)", result.Runs, r => r.PathfindingMs, 4.0);
            PrintMetricSummary("precombatRestSimulationMs (threshold P95 <= 3.0 ms)", result.Runs, r => r.PrecombatRestMs, 3.0);

            Console.WriteLine("[Memory] Retained GC heap per run (MiB): baseline after warmup | window median (rule value) | end of window | max observed (non-forcing)");
            for (int i = 0; i < result.Runs.Count; i++)
            {
                Console.WriteLine(
                    $"  run {i + 1}: {result.MemoryBaselineBytes[i] / 1048576.0:F2} | " +
                    $"{result.MemoryWindowMedianBytes[i] / 1048576.0:F2} | " +
                    $"{result.MemoryRetainedEndBytes[i] / 1048576.0:F2} | max {result.MemoryMaxObservedBytes[i] / 1048576.0:F2}");
            }
        }

        private static void PrintMetricSummary(
            string label, List<ScenarioRunSamples> runs,
            Func<ScenarioRunSamples, double[]> selector, double p95Threshold)
        {
            Console.WriteLine($"[Metric] {label}");
            var combined = new List<double>();
            for (int i = 0; i < runs.Count; i++)
            {
                double[] samples = selector(runs[i]);
                combined.AddRange(samples);
                PrintSummaryLine($"  run {i + 1}", PerfStatistics.Summarize(samples), p95Threshold);
            }
            if (runs.Count > 1)
            {
                PrintSummaryLine("  combined", PerfStatistics.Summarize(combined), p95Threshold);
            }
        }

        private static void PrintSummaryLine(string label, PerfStatistics.Summary s, double p95Threshold)
        {
            Console.WriteLine(
                $"{label}: n={s.Count}, min={s.Min:F3} ms, p95={s.P95:F3} ms, p99={s.P99:F3} ms, " +
                $"max={s.Max:F3} ms | P95 threshold {p95Threshold:F1} ms: {(s.P95 <= p95Threshold ? "within" : "EXCEEDS")} (diagnosis only)");
        }

        private static string ParseOption(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.Ordinal))
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static bool TryParseIntOption(string[] args, string name, ref int target)
        {
            string value = ParseOption(args, name);
            if (value == null)
            {
                return true;
            }
            return int.TryParse(value, out target);
        }

        // ----------------------------------------------------------------
        // Default demo: determinism smoke check + micro benchmark
        // ----------------------------------------------------------------

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
            var economy = new EconomySystem(entities);
            var construction = new Nova.Simulation.Construction.ConstructionSystem(entities, economy);
            var production = new Nova.Simulation.Production.ProductionSystem(entities, economy, construction);
            var fogOfWar = new FogOfWarSystem(entities, teamCount: 2, 128, 128);
            var combat = new CombatSystem(entities, fogOfWar, economy);

            // Canonical tick order (SimulationCore.md section 2): economy
            // (phases 2/3), construction and production (phases 4/5) BEFORE
            // pathfinding/movement (phase 6), then the 5 Hz FoW recompute,
            // then combat.
            kernel.RegisterSystem(economy);
            kernel.RegisterSystem(construction);
            kernel.RegisterSystem(production);
            kernel.RegisterSystem(pathfinding);
            kernel.RegisterSystem(movement);
            kernel.RegisterSystem(fogOfWar);
            kernel.RegisterSystem(combat);

            var session = new MatchSession(localSlot: 0, activeSlots: new byte[] { 0, 1 }, inputDelayTicks: 1);
            var ingress = new CommandIngress(session);
            _ = new LocalLoopbackTransport(ingress);
            kernel.BindCommands(new UnitCommandStateView(entities, pathfinding, economy, construction, production), ingress);

            kernel.Start();

            // Spawn 1,000 active units distributed across the grid, owned by the local slot.
            var rawIds = new uint[UnitCount];
            for (int i = 0; i < UnitCount; i++)
            {
                int startX = 10 + (i % 30);
                int startY = 10 + (i / 30);
                EntityId id = entities.SpawnUnit(
                    0,
                    new Transform2D(SimFixed.FromInt(startX), SimFixed.FromInt(startY)),
                    SimFixed.FromRaw(294912), // 4.5 m/s
                    SimFixed.FromRaw(26214)); // ~0.4 m
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
