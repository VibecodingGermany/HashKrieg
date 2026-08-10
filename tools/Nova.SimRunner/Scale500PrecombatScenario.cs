using System;
using System.Collections.Generic;
using System.Diagnostics;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Production;
using Nova.Simulation.State;
using Nova.Simulation.Vision;

namespace Nova.SimRunner
{
    /// <summary>
    /// Options of the SCALE_500_PRECOMBAT scenario run. The defaults are the
    /// binding contract values of quality/scenarios/mvp-v1.json
    /// (<c>performanceMethod</c>, D-052/D-063): 30 s warmup, exactly 3
    /// separate measurement runs of 120 s wall-clock each. Shorter values are
    /// selectable for diagnosis and tests; the artifacts always record the
    /// actually used values.
    /// </summary>
    internal sealed class ScenarioOptions
    {
        public const string ScenarioId = "SCALE_500_PRECOMBAT";
        public const string MethodRef = "performanceMethod";

        /// <summary>Contract default: 3 repetitions.</summary>
        public int Runs = 3;

        /// <summary>Contract default: 30 s warmup without measurement.</summary>
        public int WarmupSeconds = 30;

        /// <summary>Contract default: 120 s wall-clock per measurement run.</summary>
        public int MeasureSeconds = 120;

        /// <summary>Contract workload: 500 synthetic agents.</summary>
        public int AgentCount = 500;

        /// <summary>Deterministic scenario seed (workload AND simulation).</summary>
        public ulong Seed = 0x5CA1E50000000001UL;

        /// <summary>
        /// Re-target rotation period in simulation ticks: every agent
        /// receives a fresh Move target once per period (see
        /// <see cref="Scale500PrecombatScenario"/> remarks).
        /// </summary>
        public int RetargetPeriodTicks = 20;
    }

    /// <summary>Raw samples of one measurement run (one sample per tick).</summary>
    internal sealed class ScenarioRunSamples
    {
        public double[] PathfindingMs = Array.Empty<double>();
        public double[] PrecombatRestMs = Array.Empty<double>();
        public long[] MemoryBytes = Array.Empty<long>();

        /// <summary>Retained heap (full GC) probed once per wall-second of the window.</summary>
        public long[] RetainedProbes = Array.Empty<long>();
        public long Ticks;
        public double ElapsedSeconds;
    }

    /// <summary>
    /// Tri-state of the no-unbounded-memory-growth assertion. For very short
    /// diagnostic windows (&lt; 30 s measurement) the assertion is
    /// <see cref="NotApplicable"/>: allocator/JIT/GC warm-up effects dominate
    /// a few-second window, so a verdict would measure the environment, not
    /// the workload. Gate-relevant runs use the contract window (120 s) and
    /// are always evaluated strictly. In the D-062 model a skipped assertion
    /// of a REAL gate run is a fail — the not-applicable case exists only
    /// for diagnostic runs without gate claim and is announced on stdout.
    /// </summary>
    internal enum MemoryAssertionVerdict
    {
        NotApplicable,
        Pass,
        Fail,
    }

    /// <summary>Aggregate result of a full scenario execution.</summary>
    internal sealed class ScenarioResult
    {
        public readonly List<ScenarioRunSamples> Runs = new List<ScenarioRunSamples>();
        public bool NoCrash;

        /// <summary>Verdict of the memory assertion (NotApplicable for short diagnostic windows).</summary>
        public MemoryAssertionVerdict MemoryAssertion = MemoryAssertionVerdict.NotApplicable;

        /// <summary>True unless the memory assertion was evaluated and FAILED (compatibility helper).</summary>
        public bool MemoryGrowthBounded
        {
            get => MemoryAssertion != MemoryAssertionVerdict.Fail;
            set => MemoryAssertion = value ? MemoryAssertionVerdict.Pass : MemoryAssertionVerdict.Fail;
        }

        /// <summary>Per run: retained GC heap after the warmup (full GC, baseline).</summary>
        public readonly List<long> MemoryBaselineBytes = new List<long>();

        /// <summary>Per run: retained GC heap at the end of the measured window (full GC).</summary>
        public readonly List<long> MemoryRetainedEndBytes = new List<long>();

        /// <summary>Per run: median of the retained probes in the evaluation window (full GC).</summary>
        public readonly List<long> MemoryWindowMedianBytes = new List<long>();

        /// <summary>Per run: maximum observed GC heap in bytes (non-forcing per-tick samples).</summary>
        public readonly List<long> MemoryMaxObservedBytes = new List<long>();
    }

    /// <summary>
    /// SCALE_500_PRECOMBAT (quality/scenarios/mvp-v1.json; V4/V5a of the
    /// MVP recovery plan): representative SpatialHash-, FoW-filter-, command-
    /// and pathfinding load with 500 synthetic agents BEFORE combat, on the
    /// canonical 128 x 128 map.
    /// <para>
    /// Workload design (why this is representative for V4/V5a):
    /// <list type="bullet">
    /// <item>500 agents spawn at deterministic pseudo-random positions
    /// (harness-local <see cref="SimRandom"/> derived from the scenario seed —
    /// the simulation PRNG is never touched by the workload generator).</item>
    /// <item>Every agent holds a Move order at all times. Re-target
    /// mechanism: the agent set is split into <see cref="ScenarioOptions.RetargetPeriodTicks"/>
    /// slices; each tick exactly one slice (25 of 500 agents at contract
    /// scale, one sealed Move command per tick through the canonical
    /// CommandIngress intake) receives a new pseudo-random target. Mean
    /// travel time between random cells at 4.5 m/s is ~130 ticks, far above
    /// the 20-tick re-target period, so agents never settle — the
    /// representative steady-state load of a pre-combat army
    /// manoeuvre.</item>
    /// <item>Each Move command regenerates the shared 128 x 128 flow field
    /// (IntegrationField Dijkstra wave + FlowField derivation) inside command
    /// application — exactly one full-field recomputation per tick, the V4
    /// pathfinding load.</item>
    /// <item>Movement (spatial binning + 3x3 separation steering for 500
    /// agents = the SpatialHash load), the canonical 5 Hz Fog-of-War
    /// filtering and the sealed command intake (validation, dedupe,
    /// execution) run every tick — the V5a "rest simulation".</item>
    /// <item>NO combat: the CombatSystem is deliberately not registered
    /// (pre-combat scenario). It contributes 0 ms by construction, so
    /// precombatRestSimulationMs = total tick time - pathfinding
    /// time.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Measurement (D-052/D-063 method): <see cref="ScenarioOptions.Runs"/>
    /// separate runs; each run is a FRESH host built from the identical seed
    /// that first executes <see cref="ScenarioOptions.WarmupSeconds"/> of the
    /// identical workload UNMEASURED (warmup brings tick caches and the GC
    /// heap into steady state) and then
    /// <see cref="ScenarioOptions.MeasureSeconds"/> WALL-CLOCK seconds
    /// measured — ticks run unthrottled, one raw sample per tick (at the
    /// observed tick rates this trivially satisfies the >= 1 sample/second
    /// contract). "Separate runs" means no heap-state leakage between runs;
    /// all runs replay the identical deterministic workload.
    /// <c>pathfindingMs</c> is the time inside RequestFlowField (measured via
    /// <see cref="TimedPathfindingSystem"/>, the only outside interception
    /// point — the flow field regenerates inside kernel command application,
    /// not in a system tick). <c>precombatRestSimulationMs</c> is the total
    /// StepTick time minus pathfinding. Simulation sources contain no
    /// measurement logic; all timing lives in this harness and never feeds
    /// back into the simulation (determinism is covered by a test).
    /// </para>
    /// <para>
    /// Memory assertion (no-unbounded-memory-growth), documented rule: the
    /// RETAINED managed heap — GC.GetTotalMemory after a full blocking
    /// collection — is sampled once after the warmup (baseline) and probed
    /// once per wall-second of the measured window. A run passes when the
    /// MEDIAN of the evaluation window (last tenth of the probes, at least
    /// the last 10) does not exceed baseline x 1.10; the scenario assertion
    /// passes when every run passes (see
    /// <see cref="EvaluateMemoryGrowthBounded"/>). Rationale: non-forcing
    /// GetTotalMemory samples under sustained allocation track the runtime's
    /// lazy segment growth (no memory pressure on a dev machine), not leaks —
    /// local diagnosis showed ~6 MiB of apparent within-run growth that a
    /// full collection collapses back to baseline. The windowed median is
    /// robust against single-point spikes yet still catches any sustained
    /// leak. For measurement windows shorter than
    /// <see cref="MinMeasurementSecondsForMemoryAssertion"/> seconds the
    /// assertion is reported as NOT-APPLICABLE (allocator/JIT/GC warm-up
    /// dominates short windows; announced on stdout, artifact samples [1] —
    /// diagnostic runs carry no gate claim; a skipped assertion in a real
    /// gate run would be a fail). The per-tick non-forcing samples are still
    /// recorded and their maximum is reported as diagnosis. GC operates at
    /// runtime level and never influences simulation state, so the
    /// determinism contract is unaffected; probes run between ticks and are
    /// never part of the per-tick timing samples.
    /// </para>
    /// </summary>
    internal static class Scale500PrecombatScenario
    {
        private const ushort MapWidth = 128;
        private const ushort MapHeight = 128;
        private const int SpawnMarginCells = 4;

        /// <summary>
        /// Permitted retained-heap growth ratio (evaluation-window median of
        /// the per-second retained probes / retained baseline after warmup,
        /// both after a full GC) for no-unbounded-memory-growth.
        /// </summary>
        public const double MemoryGrowthTolerance = 1.10;

        /// <summary>
        /// Minimum measurement window in seconds for the memory assertion to
        /// be evaluated. Shorter diagnostic windows are dominated by
        /// allocator/JIT/GC warm-up effects (they measure the environment,
        /// not the workload) and report the assertion as not-applicable; the
        /// contract window (120 s) is always evaluated strictly.
        /// </summary>
        public const int MinMeasurementSecondsForMemoryAssertion = 30;

        /// <summary>
        /// The documented no-unbounded-memory-growth rule as a pure function
        /// (unit-tested): the RETAINED heap is probed once per wall-second of
        /// the measured window (each probe after a full blocking GC). The
        /// evaluation window is the LAST TENTH of the probes, but never fewer
        /// than the last 10 probes (and never more than available); a run
        /// passes when the MEDIAN of that window does not exceed
        /// <paramref name="baselineBytes"/> x <see cref="MemoryGrowthTolerance"/>.
        /// The window median is robust against single-point spikes (GC
        /// timing, one-off structures) while a genuine leak — sustained
        /// growth through the end of the window — still fails.
        /// </summary>
        public static bool EvaluateMemoryGrowthBounded(
            long baselineBytes, IReadOnlyList<long> retainedProbes, out long windowMedianBytes)
        {
            if (retainedProbes == null) throw new ArgumentNullException(nameof(retainedProbes));
            if (retainedProbes.Count == 0) throw new ArgumentException("Probes must not be empty.", nameof(retainedProbes));

            int count = retainedProbes.Count;
            int window = Math.Min(count, Math.Max(count / 10, 10));
            var slice = new long[window];
            for (int i = 0; i < window; i++)
            {
                slice[i] = retainedProbes[count - window + i];
            }
            windowMedianBytes = PerfStatistics.Median(slice);

            if (baselineBytes <= 0)
            {
                return windowMedianBytes <= 0;
            }
            return windowMedianBytes <= baselineBytes * MemoryGrowthTolerance;
        }

        private sealed class Host
        {
            public SimulationKernel Kernel;
            public CommandIngress Ingress;
            public MatchSession Session;
            public TimedPathfindingSystem Pathfinding;
            public TimedStatefulSimSystem Economy;
            public TimedStatefulSimSystem Construction;
            public TimedStatefulSimSystem Production;
            public TimedStatefulSimSystem Movement;
            public TimedStatefulSimSystem FogOfWar;
            public EntityManager Entities;
            public uint[] RawIds;
            public SimRandom WorkloadRandom;
            public int RetargetSliceSize;
            public int RetargetPeriod;
        }

        /// <summary>
        /// Executes the full method. Every run uses a FRESH host built from
        /// the identical seed: first the unmeasured warmup (identical
        /// workload), which brings tick caches and the GC heap into steady
        /// state, then the measured window of the same host. Runs are
        /// therefore separate (no heap-state leakage between runs) yet each
        /// measured window starts from a warmed-up host. Returns the
        /// aggregate result; throws on crash (the caller turns that into the
        /// no-crash assertion).
        /// </summary>
        public static ScenarioResult Run(ScenarioOptions options, INovaLogger logger)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            var result = new ScenarioResult();

            for (int run = 1; run <= options.Runs; run++)
            {
                Console.WriteLine(
                    $"[Run {run}/{options.Runs}] fresh host: {options.WarmupSeconds}s warmup (unmeasured) " +
                    $"+ {options.MeasureSeconds}s measured.");
                var samples = new List<double>(options.MeasureSeconds * 2048);
                var restSamples = new List<double>(options.MeasureSeconds * 2048);
                var memorySamples = new List<long>(options.MeasureSeconds * 2048);
                var collector = new SampleCollector(samples, restSamples, memorySamples);

                RunHost(options, logger, options.WarmupSeconds, options.MeasureSeconds, collector);

                var runSamples = new ScenarioRunSamples
                {
                    PathfindingMs = samples.ToArray(),
                    PrecombatRestMs = restSamples.ToArray(),
                    MemoryBytes = memorySamples.ToArray(),
                    RetainedProbes = collector.RetainedProbes.ToArray(),
                    Ticks = collector.Ticks,
                    ElapsedSeconds = collector.ElapsedSeconds,
                };
                result.Runs.Add(runSamples);

                long max = 0;
                for (int i = 0; i < runSamples.MemoryBytes.Length; i++)
                {
                    if (runSamples.MemoryBytes[i] > max) max = runSamples.MemoryBytes[i];
                }
                bool bounded = EvaluateMemoryGrowthBounded(
                    collector.MemoryBaselineBytes, runSamples.RetainedProbes, out long windowMedian);
                result.MemoryBaselineBytes.Add(collector.MemoryBaselineBytes);
                result.MemoryRetainedEndBytes.Add(collector.MemoryRetainedEndBytes);
                result.MemoryWindowMedianBytes.Add(windowMedian);
                result.MemoryMaxObservedBytes.Add(max);

                Console.WriteLine(
                    $"[Run {run}] {runSamples.Ticks} ticks in {runSamples.ElapsedSeconds:F1}s " +
                    $"({runSamples.Ticks / runSamples.ElapsedSeconds:F0} ticks/s, {runSamples.PathfindingMs.Length} samples). " +
                    $"Retained heap: baseline {collector.MemoryBaselineBytes / 1048576.0:F2} MiB, " +
                    $"window median {windowMedian / 1048576.0:F2} MiB " +
                    $"({(bounded ? "<=" : ">")} {MemoryGrowthTolerance:F2}x baseline), " +
                    $"end {collector.MemoryRetainedEndBytes / 1048576.0:F2} MiB (full GC), " +
                    $"max observed {max / 1048576.0:F2} MiB.");
            }

            result.NoCrash = true;
            if (options.MeasureSeconds < MinMeasurementSecondsForMemoryAssertion)
            {
                result.MemoryAssertion = MemoryAssertionVerdict.NotApplicable;
                Console.WriteLine(
                    $"[Memory] Measurement window {options.MeasureSeconds}s < " +
                    $"{MinMeasurementSecondsForMemoryAssertion}s: no-unbounded-memory-growth is NOT-APPLICABLE " +
                    "(allocator/JIT/GC warm-up dominates short diagnostic windows; the contract window is evaluated strictly).");
            }
            else
            {
                result.MemoryAssertion = MemoryAssertionVerdict.Pass;
                for (int i = 0; i < result.Runs.Count; i++)
                {
                    bool bounded = EvaluateMemoryGrowthBounded(
                        result.MemoryBaselineBytes[i], result.Runs[i].RetainedProbes, out _);
                    if (!bounded)
                    {
                        result.MemoryAssertion = MemoryAssertionVerdict.Fail;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Drives the identical workload for a fixed number of simulation
        /// ticks (timing active) and returns the canonical state hash. Used
        /// by the determinism test: equal seeds must produce equal hashes
        /// even though the Stopwatch wrappers measure every tick.
        /// </summary>
        public static ulong RunFixedTicks(ScenarioOptions options, int tickCount, INovaLogger logger)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            Host host = BuildHost(options, logger ?? NullNovaLogger.Instance);
            var tickStopwatch = new Stopwatch();
            for (int i = 0; i < tickCount; i++)
            {
                StepHostTick(host, tickStopwatch, null);
            }
            ulong hash = host.Kernel.CalculateStateHash();
            host.Kernel.Stop();
            return hash;
        }

        private sealed class SampleCollector
        {
            public readonly List<double> PathfindingMs;
            public readonly List<double> RestMs;
            public readonly List<long> MemoryBytes;
            public readonly List<long> RetainedProbes = new List<long>(256);
            public long Ticks;
            public double ElapsedSeconds;
            public long MemoryBaselineBytes;
            public long MemoryRetainedEndBytes;

            public SampleCollector(List<double> pathfindingMs, List<double> restMs, List<long> memoryBytes)
            {
                PathfindingMs = pathfindingMs;
                RestMs = restMs;
                MemoryBytes = memoryBytes;
            }
        }

        /// <summary>
        /// Runs one fresh host: <paramref name="warmupSeconds"/> wall-clock
        /// unmeasured (identical workload, brings the host into steady
        /// state), then <paramref name="measureSeconds"/> wall-clock with
        /// sampling into <paramref name="collect"/>.
        /// </summary>
        private static void RunHost(
            ScenarioOptions options, INovaLogger logger,
            int warmupSeconds, int measureSeconds, SampleCollector collect)
        {
            Host host = BuildHost(options, logger ?? NullNovaLogger.Instance);

            // Harness hygiene between runs: compact once BEFORE the run so
            // garbage of the previous host does not pollute this run's heap
            // baseline. The within-run measurement itself never forces a
            // collection (GC.GetTotalMemory(false)); GC is a runtime-level
            // operation and cannot affect simulation determinism.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var tickStopwatch = new Stopwatch();

            var warmupClock = Stopwatch.StartNew();
            while (warmupClock.Elapsed.TotalSeconds < warmupSeconds)
            {
                StepHostTick(host, tickStopwatch, null);
            }
            warmupClock.Stop();

            // Retained-heap baseline AFTER the warmup: a full blocking
            // collection separates genuinely retained memory from collectible
            // per-tick garbage. (Non-forcing GetTotalMemory samples track the
            // runtime's lazy segment growth under zero memory pressure, not
            // retention — diagnosis showed multi-MiB "growth" that a full GC
            // collapses back to baseline.)
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            collect.MemoryBaselineBytes = GC.GetTotalMemory(forceFullCollection: false);

            // Measured window: one retained-heap probe (full GC) per
            // wall-second, taken BETWEEN ticks — probes are never part of the
            // per-tick timing samples and cannot influence simulation state.
            var wallClock = Stopwatch.StartNew();
            double nextProbeSeconds = 1.0;
            while (wallClock.Elapsed.TotalSeconds < measureSeconds)
            {
                StepHostTick(host, tickStopwatch, collect);
                if (wallClock.Elapsed.TotalSeconds >= nextProbeSeconds)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    collect.RetainedProbes.Add(GC.GetTotalMemory(forceFullCollection: false));
                    nextProbeSeconds += 1.0;
                }
            }
            wallClock.Stop();
            collect.ElapsedSeconds = wallClock.Elapsed.TotalSeconds;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            collect.MemoryRetainedEndBytes = GC.GetTotalMemory(forceFullCollection: false);

            host.Kernel.Stop();
        }

        /// <summary>
        /// Drives exactly one tick: submits the re-target Move command for
        /// the slice due at the next tick through the sealed canonical
        /// intake, then steps the kernel under the total-tick Stopwatch.
        /// pathfindingMs = time inside RequestFlowField during command
        /// application; rest = total - pathfinding (no combat registered).
        /// </summary>
        private static void StepHostTick(Host host, Stopwatch tickStopwatch, SampleCollector collect)
        {
            uint nextTick = host.Kernel.CurrentTick.Value + 1;
            SubmitRetargetSlice(host, nextTick);

            CommandBatch batch = host.Ingress.SealTickBatch(nextTick);
            if (batch.Count > 0)
            {
                host.Kernel.SubmitBatch(batch);
            }

            host.Pathfinding.BeginTick();
            tickStopwatch.Restart();
            host.Kernel.StepTick();
            tickStopwatch.Stop();
            host.Session.AdvanceTick();

            if (collect != null)
            {
                double pathMs = host.Pathfinding.FlowFieldMsThisTick;
                double totalMs = tickStopwatch.Elapsed.TotalMilliseconds;
                collect.PathfindingMs.Add(pathMs);
                collect.RestMs.Add(totalMs - pathMs);
                collect.MemoryBytes.Add(GC.GetTotalMemory(forceFullCollection: false));
                collect.Ticks++;
            }
        }

        /// <summary>
        /// The documented re-target mechanism: one slice of agents per tick
        /// receives a fresh pseudo-random Move target via the canonical
        /// command intake, so every agent is re-targeted once per
        /// <see cref="ScenarioOptions.RetargetPeriodTicks"/> ticks and the
        /// flow field is recomputed exactly once per tick.
        /// </summary>
        private static void SubmitRetargetSlice(Host host, uint nextTick)
        {
            int slice = (int)((nextTick - 1) % (uint)host.RetargetPeriod);
            int start = slice * host.RetargetSliceSize;
            if (start >= host.RawIds.Length)
            {
                return;
            }
            int count = Math.Min(host.RetargetSliceSize, host.RawIds.Length - start);

            var ids = new uint[count];
            Array.Copy(host.RawIds, start, ids, 0, count);

            int targetX = host.WorkloadRandom.NextInt(SpawnMarginCells, MapWidth - SpawnMarginCells);
            int targetY = host.WorkloadRandom.NextInt(SpawnMarginCells, MapHeight - SpawnMarginCells);

            var payload = new MovePayload(ids, SimFixed.FromInt(targetX), SimFixed.FromInt(targetY));
            CommandIngressResult result = host.Ingress.TrySubmitIntent(CommandIntent.Create(payload), out CommandRejectReason reason);
            if (result != CommandIngressResult.Accepted)
            {
                throw new InvalidOperationException($"Re-target intent rejected: {result} ({reason}).");
            }
        }

        /// <summary>
        /// Builds a fresh pre-combat host from the scenario seed: canonical
        /// system order without the CombatSystem, command pipeline bound, all
        /// agents spawned at deterministic pseudo-random positions.
        /// </summary>
        private static Host BuildHost(ScenarioOptions options, INovaLogger logger)
        {
            var kernel = new SimulationKernel(new SimRandom(options.Seed), logger);

            var entities = new EntityManager(Math.Max(1024, options.AgentCount * 2));
            var pathfinding = new TimedPathfindingSystem(MapWidth, MapHeight);
            var movement = new MovementSystem(entities, pathfinding);
            var economy = new EconomySystem(entities);
            var construction = new ConstructionSystem(entities, economy, pathfinding.CostField);
            var production = new ProductionSystem(entities, economy, construction);
            var fogOfWar = new FogOfWarSystem(entities, construction, economy, teamCount: 2, MapWidth, MapHeight);

            var host = new Host
            {
                Kernel = kernel,
                Pathfinding = pathfinding,
                Economy = new TimedStatefulSimSystem(economy),
                Construction = new TimedStatefulSimSystem(construction),
                Production = new TimedStatefulSimSystem(production),
                Movement = new TimedStatefulSimSystem(movement),
                FogOfWar = new TimedStatefulSimSystem(fogOfWar),
                Entities = entities,
                WorkloadRandom = new SimRandom(options.Seed ^ 0x9E3779B97F4A7C15UL),
            };

            // Canonical tick order (SimulationCore.md section 2), pre-combat:
            // no CombatSystem registered — combat contributes 0 ms by
            // construction, so rest = total - pathfinding.
            kernel.RegisterSystem(host.Economy);
            kernel.RegisterSystem(host.Construction);
            kernel.RegisterSystem(host.Production);
            kernel.RegisterSystem(pathfinding);
            kernel.RegisterSystem(host.Movement);
            kernel.RegisterSystem(host.FogOfWar);

            host.Session = new MatchSession(localSlot: 0, activeSlots: new byte[] { 0, 1 }, inputDelayTicks: 1);
            host.Ingress = new CommandIngress(host.Session);
            _ = new LocalLoopbackTransport(host.Ingress);
            kernel.BindCommands(
                new UnitCommandStateView(entities, pathfinding, economy, construction, production),
                host.Ingress);

            kernel.Start();

            host.RawIds = new uint[options.AgentCount];
            for (int i = 0; i < options.AgentCount; i++)
            {
                int startX = host.WorkloadRandom.NextInt(SpawnMarginCells, MapWidth - SpawnMarginCells);
                int startY = host.WorkloadRandom.NextInt(SpawnMarginCells, MapHeight - SpawnMarginCells);
                EntityId id = entities.SpawnUnit(
                    0,
                    new Transform2D(SimFixed.FromInt(startX), SimFixed.FromInt(startY)),
                    SimFixed.FromRaw(294912), // 4.5 m/s
                    SimFixed.FromRaw(26214)); // ~0.4 m
                host.RawIds[i] = UnitCommandStateView.ToRawEntityId(id);
            }

            int sliceSize = Math.Max(1, (options.AgentCount + options.RetargetPeriodTicks - 1) / options.RetargetPeriodTicks);
            if (sliceSize > CommandLimits.MaxEntityIdsPerCommand)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    $"Re-target slice size {sliceSize} exceeds MaxEntityIdsPerCommand; raise RetargetPeriodTicks.");
            }
            host.RetargetSliceSize = sliceSize;
            host.RetargetPeriod = (options.AgentCount + sliceSize - 1) / sliceSize;

            return host;
        }
    }
}
