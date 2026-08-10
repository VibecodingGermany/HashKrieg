using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Production;
using Nova.Simulation.Replays;
using Nova.Simulation.State;
using Nova.Simulation.Vision;

namespace Nova.SimRunner
{
    /// <summary>
    /// Options of the DETERMINISM_10000 scenario run. The defaults are the
    /// binding contract values of quality/scenarios/mvp-v1.json (scenario
    /// DETERMINISM_10000, G1/V1): exactly 10,000 ticks with 2 active slots.
    /// The checkpoint interval (every 100 ticks) is a documented harness
    /// choice — SimulationCore.md section 9 requires exact state hashes "per
    /// checkpoint" without fixing a number; 100 checkpoints plus the final
    /// state hash give 101 hash pins over the match. Shorter values are
    /// selectable for tests and diagnosis; the artifacts always record the
    /// actually used values.
    /// </summary>
    internal sealed class DeterminismOptions
    {
        public const string ScenarioId = "DETERMINISM_10000";

        /// <summary>Contract workload: exactly 10,000 ticks.</summary>
        public int Ticks = 10000;

        /// <summary>Documented harness choice: one canonical state hash every 100 ticks.</summary>
        public int CheckpointIntervalTicks = 100;

        /// <summary>Deterministic scenario seed (workload AND simulation).</summary>
        public ulong Seed = 0xDE7E000000010271UL;

        /// <summary>Platform tag for the artifact name (null = auto-detect, e.g. "macos-arm64").</summary>
        public string PlatformId { get; set; }

        /// <summary>Optional path of another platform's profile artifact to verify against (CLI concern).</summary>
        public string VerifyPath { get; set; }
    }

    /// <summary>One recorded checkpoint: canonical state hash after a given tick.</summary>
    internal sealed class CheckpointEntry
    {
        public uint Tick;
        public ulong StateHash64;
    }

    /// <summary>Aggregate result of one DETERMINISM_10000 execution.</summary>
    internal sealed class DeterminismRunResult
    {
        public string PlatformId;
        public int Ticks;
        public int CheckpointIntervalTicks;
        public ulong Seed;
        public readonly List<CheckpointEntry> Checkpoints = new List<CheckpointEntry>();
        public ulong FinalStateHash;
        public int FinalSnapshotLength;
        public string FinalSnapshotSha256;
        public int ReplayLength;
        public string ReplaySha256;
        public ulong FingerprintHash64;

        /// <summary>
        /// True when the playback re-executed every recorded command result
        /// value-exactly and reproduced the recorded final state hash (the
        /// local determinism baseline, independent of any cross-platform
        /// comparison).
        /// </summary>
        public bool PlaybackVerified;

        /// <summary>Human-readable playback divergence detail when <see cref="PlaybackVerified"/> is false.</summary>
        public string PlaybackFailure = "";

        /// <summary>True when the NOVA_FIXED_POINT determinism define was compiled in (build self-report).</summary>
        public bool DeterminismDefineActive;

        public double GeneratorSeconds;
        public double PlaybackSeconds;
    }

    /// <summary>Outcome of comparing a run against another platform's profile artifact.</summary>
    internal sealed class DeterminismComparison
    {
        public bool CheckpointsExact;
        public bool SnapshotExact;

        /// <summary>First divergence found (checkpoint tick and both hashes, or the snapshot difference); empty when exact.</summary>
        public string FirstDivergence = "";
    }

    /// <summary>
    /// DETERMINISM_10000 (quality/scenarios/mvp-v1.json; G1/V1 of the MVP
    /// recovery plan; SimulationCore.md sections 7 and 9): the identical
    /// canonical replay stream must produce EXACT state hashes at every
    /// checkpoint AND exact final snapshot bytes on Windows x64 and macOS
    /// arm64, on the managed path, from the same sources and determinism
    /// defines.
    /// <para>
    /// Two-phase design:
    /// <list type="number">
    /// <item>GENERATOR (deterministic, in code): one canonical match over
    /// exactly <see cref="DeterminismOptions.Ticks"/> ticks with 2 active
    /// slots (slot 0 human, slot 1 "AI"). The command stream is produced by
    /// the fixed, documented <see cref="IssueSlotCommands"/> script — a pure
    /// function of the tick number and deterministic ascending-index queries
    /// of the host state; there is NO randomness outside the simulation PRNG
    /// (the script never touches the SimRandom). Every tick is recorded with
    /// the canonical <see cref="ReplayRecorder"/> (NOVA_REPLAY_CHAIN_V1
    /// container), so the match's command stream exists as a fixed replay
    /// artifact. Identical code and seed produce byte-identical replay
    /// bytes — that is what makes the stream "the same replay" on every
    /// platform without shipping a file.</item>
    /// <item>MEASURED PLAYBACK: a fresh host restores the replay's embedded
    /// initial snapshot and replays every recorded tick through the
    /// identical sealed path (<see cref="CommandIngress.TryAcceptHistoricalRecordBytes"/>,
    /// seal, submit, step — the same path <see cref="ReplayPlayer"/> uses),
    /// re-verifying every recorded command result value-exactly. Every
    /// <see cref="DeterminismOptions.CheckpointIntervalTicks"/> ticks the
    /// canonical state hash (<c>kernel.CalculateStateHash()</c>) is pinned;
    /// at the end the final snapshot bytes (<c>kernel.SaveSnapshot()</c>)
    /// are hashed (SHA-256) and measured.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Workload (D-077 — the classic loop start): the match setup is the
    /// MS-1 manifest start state of quality/content/mvp-v1.json
    /// (startStatePerPlayer) per slot — a COMPLETED HQ, ONE Builder and
    /// 3.000 AE (EconomySystem.CanonicalMatchStartingCreditsAE, wired by
    /// <see cref="BuildHost"/>) — plus five finite canonical Aetherium fields.
    /// Nothing
    /// else is spawned: the script then drives the opening exactly like a
    /// player, for BOTH slots — walk the Builder to the future site, place
    /// the Refinery once it is affordable and the committed grid covers its
    /// draw (construction: placement, auto-assigned builder, site
    /// progression), set the completed Refinery's rally point and queue two
    /// Harvesters at it (production — the Refinery, not the HQ, is the
    /// Harvester's producer since D-077), then issue a Harvest order to
    /// every produced harvester and let the harvest/return auto-cycle
    /// (economy) climb the credit curve for the remaining ticks. Movement
    /// and pathfinding run along via the builder walk. Combat orders are no
    /// longer part of the script: D-077 removed the midfield skirmish
    /// squads, so no own combat unit exists to command — an honest,
    /// documented reduction of the exercised surface, not a hidden one.
    /// Slot 0 commands enter as local intents (human), slot 1 commands as
    /// crafted wire records (the stand-in "AI" transport); state-dependent
    /// rejections stay in the recorded stream with their deterministic
    /// results (Commands.md section 4).
    /// </para>
    /// <para>
    /// Assertions (D-062 naming, bool artifacts): <c>managed-path-only</c>
    /// is trivially true in this .NET lane — the runner is 100% managed C#
    /// and Burst is a Unity compiler path that does not exist here
    /// (documented self-report). <c>same-sources-and-determinism-defines</c>
    /// is the build's self-report that the NOVA_FIXED_POINT define was
    /// compiled in; source identity holds by construction because
    /// tools/Nova.SimRunner/Nova.SimRunner.csproj compiles the same
    /// Assets/_Project/Scripts/Core and /Simulation sources as the Unity
    /// host (SimulationCore.md section 9). The two comparison assertions
    /// (<c>exact-state-hash-every-checkpoint</c>,
    /// <c>exact-final-snapshot-bytes</c>) are only emitted in verify mode
    /// (--verify): [1] on full equality, [0] with the first divergence
    /// printed. Cross-platform workflow: run without --verify on macOS arm64
    /// and on Windows x64, then re-run on either machine with --verify
    /// pointing at the other machine's
    /// scenario.DETERMINISM_10000.&lt;platform&gt;.json; exit code is 0 only
    /// when every checkpoint hash and the final snapshot SHA-256 match. All
    /// artifacts are diagnosis material in output/ (gitignored) — never gate
    /// evidence (D-061/D-064).
    /// </para>
    /// </summary>
    internal static class Determinism10000Scenario
    {
        private const ushort MapWidth = 128;
        private const ushort MapHeight = 128;
        private const byte HumanSlot = 0;
        private const byte AiSlot = 1;
        private const int EntityCapacity = 1024;
        /// <summary>One Aetherium field of the canonical map (16.7, C1).</summary>
        private struct FieldLayout
        {
            public ushort Id;
            public int X, Y;
            public long ReserveAE;
        }

        /// <summary>
        /// The five canonical fields (16.7, C1 — MVPContentManifest section 5):
        /// two start fields and two natural expansions at 9.000 AE each, one
        /// contested centre at 15.000. Every slot-1 coordinate is the point
        /// mirror of slot 0 across the D-102/D-107 Glutrinne layout axis ((x, y) -&gt;
        /// (124 - x, 124 - y)). Registration in ascending id order is part of
        /// the canonical initial state.
        /// </summary>
        private static readonly FieldLayout[] FieldLayouts =
        {
            new FieldLayout { Id = 1, X = 7,   Y = 7,   ReserveAE = 9000L  },
            new FieldLayout { Id = 2, X = 117, Y = 117, ReserveAE = 9000L  },
            new FieldLayout { Id = 3, X = 24,  Y = 40,  ReserveAE = 9000L  },
            new FieldLayout { Id = 4, X = 100, Y = 84,  ReserveAE = 9000L  },
            new FieldLayout { Id = 5, X = 62,  Y = 62,  ReserveAE = 15000L },
        };

        /// <summary>
        /// Faction-resolved definition id of a role for the given slot
        /// (SimDefinitions id rule: Alliance = role wire value, Legion =
        /// role + 17). The slot's faction comes from the economy state —
        /// the single home of the assignment.
        /// </summary>
        private static ushort DefId(Host host, byte slot, UnitRole role)
        {
            return SimDefinitions.ToDefinitionId(host.Economy.GetSlotFaction(slot), role);
        }

        /// <summary>Fixed map layout of one slot's base (all coordinates in grid cells).</summary>
        private sealed class SlotLayout
        {
            public ushort FieldId;
            public int FieldX, FieldY;
            public int HqOriginX, HqOriginY;
            public int BuilderSpawnX, BuilderSpawnY;
            public int RefineryOriginX, RefineryOriginY, RefineryBuildX, RefineryBuildY;
            public int RefineryRallyX, RefineryRallyY;
        }

        /// <summary>
        /// Live handles the script queries against (captured at setup) plus
        /// the two script latches. A latch flips exactly once, as a pure
        /// function of the tick and the polled host state — the script stays
        /// a deterministic function of the match, like the fixed tick table
        /// before D-077.
        /// </summary>
        private sealed class SlotState
        {
            public EntityId Builder;
            public bool RefineryPlaced;
            public bool HarvestersQueued;
        }

        private sealed class Host
        {
            public SimulationKernel Kernel;
            public EntityManager Entities;
            public EconomySystem Economy;
            public ConstructionSystem Construction;
            public MatchSession Session;
            public CommandIngress Ingress;
        }

        /// <summary>
        /// Executes the full scenario: deterministic replay generation, then
        /// the measured playback with checkpoint pinning and the final
        /// snapshot measurement. Returns the aggregate result; throws only on
        /// harness bugs (structurally invalid self-generated commands).
        /// </summary>
        public static DeterminismRunResult Run(DeterminismOptions options, INovaLogger logger)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.Ticks < 1 || options.CheckpointIntervalTicks < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "ticks and checkpoint interval must be >= 1.");
            }

            var result = new DeterminismRunResult
            {
                PlatformId = options.PlatformId ?? DeterminismArtifacts.DetectPlatformId(),
                Ticks = options.Ticks,
                CheckpointIntervalTicks = options.CheckpointIntervalTicks,
                Seed = options.Seed,
#if NOVA_FIXED_POINT
                DeterminismDefineActive = true,
#else
                DeterminismDefineActive = false,
#endif
            };

            // Phase 1: deterministic replay generation (the fixed command stream).
            var generatorClock = Stopwatch.StartNew();
            byte[] replayBytes = GenerateReplay(options, logger ?? NullNovaLogger.Instance,
                out MatchFingerprint fingerprint, out _);
            generatorClock.Stop();
            result.GeneratorSeconds = generatorClock.Elapsed.TotalSeconds;
            result.ReplayLength = replayBytes.Length;
            result.ReplaySha256 = Sha256Hex(replayBytes);
            result.FingerprintHash64 = fingerprint.ComputeHash();

            // Phase 2: measured playback of the fixed stream on a fresh host.
            var playbackClock = Stopwatch.StartNew();
            RunPlayback(options, replayBytes, fingerprint, logger ?? NullNovaLogger.Instance, result);
            playbackClock.Stop();
            result.PlaybackSeconds = playbackClock.Elapsed.TotalSeconds;
            return result;
        }

        /// <summary>
        /// Phase 1, exposed for tests: builds a fresh host, applies the
        /// manifest match setup, runs the fixed script for
        /// <see cref="DeterminismOptions.Ticks"/> ticks and seals the
        /// canonical replay container. Deterministic: identical options and
        /// code produce byte-identical replay bytes.
        /// </summary>
        public static byte[] GenerateReplay(
            DeterminismOptions options, INovaLogger logger,
            out MatchFingerprint fingerprint, out byte[] initialSnapshotBytes)
        {
            Host host = BuildHost(options.Seed, logger);
            SlotState[] slots = SetupMatch(host);

            fingerprint = CreateFingerprint(host, options.Seed);
            initialSnapshotBytes = host.Kernel.SaveSnapshot();
            var recorder = new ReplayRecorder(fingerprint, initialSnapshotBytes);

            uint aiSequence = 1;
            for (int tick = 1; tick <= options.Ticks; tick++)
            {
                IssueSlotCommands(host, slots, HumanSlot, (uint)tick, ref aiSequence);
                IssueSlotCommands(host, slots, AiSlot, (uint)tick, ref aiSequence);

                CommandBatch batch = SealAndSubmit(host);
                recorder.RecordTick(host.Kernel.CurrentTick.Value, batch, host.Kernel.LastTickResults);

                if (tick % 1000 == 0)
                {
                    Console.WriteLine($"[Generator] tick {tick}/{options.Ticks}");
                }
            }

            ulong endHash = host.Kernel.CalculateStateHash();
            byte[] replayBytes = recorder.Finalize(endHash);
            host.Kernel.Stop();
            return replayBytes;
        }

        /// <summary>
        /// Phase 2: restores the replay's initial snapshot into a fresh host
        /// and replays every recorded tick through the sealed historical
        /// intake, re-verifying every recorded result. Pins the canonical
        /// state hash at every checkpoint tick and measures the final
        /// snapshot bytes.
        /// </summary>
        private static void RunPlayback(
            DeterminismOptions options, byte[] replayBytes, MatchFingerprint fingerprint,
            INovaLogger logger, DeterminismRunResult result)
        {
            if (!ReplayFile.TryParse(replayBytes, out ReplayFile replay, out ReplayReadError readError))
            {
                result.PlaybackVerified = false;
                result.PlaybackFailure = $"self-generated replay failed parsing: {readError}";
                return;
            }

            Host host = BuildHost(options.Seed, logger);
            if (!host.Kernel.TryRestoreSnapshot(replay.InitialSnapshotBytes))
            {
                result.PlaybackVerified = false;
                result.PlaybackFailure = "the fresh playback kernel refused the embedded initial snapshot";
                return;
            }
            while (host.Session.CurrentTick < host.Kernel.CurrentTick.Value)
            {
                host.Session.AdvanceTick();
            }

            ReplayTickFrame[] frames = replay.Frames;
            int interval = options.CheckpointIntervalTicks;
            for (int f = 0; f < frames.Length; f++)
            {
                ReplayTickFrame frame = frames[f];
                for (int r = 0; r < frame.RecordCount; r++)
                {
                    CommandIngressResult intake = host.Ingress.TryAcceptHistoricalRecordBytes(
                        frame.RecordBytes[r], out CommandRejectReason reason);
                    if (intake != CommandIngressResult.Accepted)
                    {
                        result.PlaybackVerified = false;
                        result.PlaybackFailure = $"tick {frame.Tick} record {r} rejected at intake: {intake}/{reason}";
                        host.Kernel.Stop();
                        return;
                    }
                }

                CommandBatch batch = host.Ingress.SealTickBatch(frame.Tick);
                if (batch.Count > 0 && !host.Kernel.SubmitBatch(batch))
                {
                    result.PlaybackVerified = false;
                    result.PlaybackFailure = $"kernel refused the sealed batch of tick {frame.Tick}";
                    host.Kernel.Stop();
                    return;
                }
                host.Kernel.StepTick();
                host.Session.AdvanceTick();

                // Deterministic result verification (same contract as ReplayPlayer).
                IReadOnlyList<CommandResult> results = host.Kernel.LastTickResults;
                if (results.Count != frame.RecordCount)
                {
                    result.PlaybackVerified = false;
                    result.PlaybackFailure = $"tick {frame.Tick}: {results.Count} results, expected {frame.RecordCount}";
                    host.Kernel.Stop();
                    return;
                }
                for (int r = 0; r < frame.RecordCount; r++)
                {
                    var expected = new CommandResult(frame.Records[r], frame.ResultCodes[r]);
                    if (results[r] != expected)
                    {
                        result.PlaybackVerified = false;
                        result.PlaybackFailure =
                            $"tick {frame.Tick} record {r}: reproduced {results[r]}, recorded {expected}";
                        host.Kernel.Stop();
                        return;
                    }
                }

                if (frame.Tick % (uint)interval == 0)
                {
                    result.Checkpoints.Add(new CheckpointEntry
                    {
                        Tick = frame.Tick,
                        StateHash64 = host.Kernel.CalculateStateHash(),
                    });
                }

                if (frame.Tick % 1000 == 0)
                {
                    Console.WriteLine($"[Playback] tick {frame.Tick}/{frames.Length}");
                }
            }

            result.FinalStateHash = host.Kernel.CalculateStateHash();
            byte[] snapshotBytes = host.Kernel.SaveSnapshot();
            result.FinalSnapshotLength = snapshotBytes.Length;
            result.FinalSnapshotSha256 = Sha256Hex(snapshotBytes);
            result.PlaybackVerified = result.FinalStateHash == replay.FinalStateHash;
            if (!result.PlaybackVerified)
            {
                result.PlaybackFailure =
                    $"end state hash {result.FinalStateHash:X16} differs from recorded {replay.FinalStateHash:X16}";
            }
            host.Kernel.Stop();
        }

        /// <summary>
        /// Verify mode: compares the own run against another platform's
        /// profile artifact. <c>exact-state-hash-every-checkpoint</c> passes
        /// only when the checkpoint series is identical in length, ticks and
        /// hashes; <c>exact-final-snapshot-bytes</c> only when length and
        /// SHA-256 of the final snapshot match. The first divergence is
        /// reported. A fingerprint mismatch means the two runs did not
        /// execute the same match and fails both assertions.
        /// </summary>
        public static DeterminismComparison Compare(DeterminismRunResult own, PlatformProfile other)
        {
            if (own == null) throw new ArgumentNullException(nameof(own));
            if (other == null) throw new ArgumentNullException(nameof(other));
            var comparison = new DeterminismComparison();

            if (own.FingerprintHash64 != other.FingerprintHash64)
            {
                comparison.FirstDivergence =
                    $"match fingerprint differs (own 0x{own.FingerprintHash64:X16}, other 0x{other.FingerprintHash64:X16}) — not the same match";
                return comparison;
            }
            if (own.Ticks != other.Ticks || own.CheckpointIntervalTicks != other.CheckpointIntervalTicks)
            {
                comparison.FirstDivergence =
                    $"run parameters differ (own {own.Ticks} ticks/{own.CheckpointIntervalTicks} interval, " +
                    $"other {other.Ticks}/{other.CheckpointIntervalTicks})";
                return comparison;
            }
            if (own.Checkpoints.Count != other.Checkpoints.Count)
            {
                comparison.FirstDivergence =
                    $"checkpoint count differs (own {own.Checkpoints.Count}, other {other.Checkpoints.Count})";
                return comparison;
            }

            comparison.CheckpointsExact = true;
            for (int i = 0; i < own.Checkpoints.Count; i++)
            {
                CheckpointEntry ownCheckpoint = own.Checkpoints[i];
                CheckpointEntry otherCheckpoint = other.Checkpoints[i];
                if (ownCheckpoint.Tick != otherCheckpoint.Tick)
                {
                    comparison.CheckpointsExact = false;
                    comparison.FirstDivergence =
                        $"checkpoint {i}: tick differs (own {ownCheckpoint.Tick}, other {otherCheckpoint.Tick})";
                    break;
                }
                if (ownCheckpoint.StateHash64 != otherCheckpoint.StateHash64)
                {
                    comparison.CheckpointsExact = false;
                    comparison.FirstDivergence =
                        $"first state-hash divergence at tick {ownCheckpoint.Tick}: " +
                        $"own 0x{ownCheckpoint.StateHash64:X16}, other 0x{otherCheckpoint.StateHash64:X16}";
                    break;
                }
            }

            comparison.SnapshotExact =
                own.FinalSnapshotLength == other.FinalSnapshotBytes
                && string.Equals(own.FinalSnapshotSha256, other.FinalSnapshotSha256, StringComparison.Ordinal);
            if (!comparison.SnapshotExact && comparison.FirstDivergence.Length == 0)
            {
                comparison.FirstDivergence =
                    $"final snapshot differs (own {own.FinalSnapshotLength} bytes sha256 {own.FinalSnapshotSha256}, " +
                    $"other {other.FinalSnapshotBytes} bytes sha256 {other.FinalSnapshotSha256})";
            }
            return comparison;
        }

        // ----------------------------------------------------------------
        // The fixed workload script (documented generator; no randomness)
        // ----------------------------------------------------------------

        /// <summary>
        /// Issues every scripted command of one slot for the batch sealed at
        /// <paramref name="nextTick"/>. The script is a pure function of the
        /// tick number and deterministic ascending-index host scans; slot 0
        /// commands enter as local intents, slot 1 commands as crafted wire
        /// records. Structural rejection of a self-generated command is a
        /// harness bug and throws; state-dependent rejections are part of the
        /// stream.
        /// <para>
        /// THE D-077 OPENING LOOP — the script drives the match like a
        /// player. Tick 1 walks the Builder next to the future Refinery
        /// site. The Refinery is placed as soon as it is affordable AND the
        /// committed grid balance covers its draw — the same two checks the
        /// placement validator applies (the power rule reads the previous
        /// tick's committed balance, so the first placement passes right
        /// after the first economy recompute committed the HQ's 30; the
        /// Refinery needs no Power plant since D-077). Once a COMPLETED own
        /// Refinery stands — polled per tick, completion is never hardcoded
        /// — the script sets its rally point onto the field edge (in harvest
        /// reach of the field AND return reach of the footprint, so the
        /// cycle closes without walking) and queues two Harvesters: the
        /// Refinery, not the HQ, is the Harvester's producer since D-077.
        /// Every harvester without a standing harvest order then receives a
        /// Harvest command on the slot's field, and the harvest/return
        /// auto-cycle (EconomySystem) runs unaided for the remaining ticks.
        /// Definition ids are faction-resolved per slot (<see cref="DefId"/>):
        /// the same script drives the Alliance rows on slot 0 and the Legion
        /// rows on slot 1.
        /// </para>
        /// </summary>
        private static void IssueSlotCommands(
            Host host, SlotState[] slots, byte slot, uint nextTick, ref uint aiSequence)
        {
            SlotLayout c = slot == HumanSlot ? Slot0Layout : Slot1Layout;
            SlotState state = slots[slot];
            int tick = (int)nextTick;

            if (tick == 1)
            {
                SubmitIfAlive(host, state.Builder, slot, ref aiSequence,
                    ids => new MovePayload(ids, SimFixed.FromInt(c.RefineryBuildX), SimFixed.FromInt(c.RefineryBuildY)));
            }

            // Place the Refinery once it is affordable and the committed grid
            // covers its draw (the placement power rule reads the previous
            // tick's committed balance — see the class remarks).
            if (!state.RefineryPlaced
                && SimDefinitions.TryGetBuilding(host.Economy.GetSlotFaction(slot), UnitRole.Refinery, out SimBuildingDefinition refinery))
            {
                PlayerEconomyState economy = host.Economy.GetPlayerEconomy(slot);
                if (economy.AetheriumCredits >= refinery.CostAE
                    && economy.PowerProvided - economy.PowerRequired >= refinery.PowerRequired)
                {
                    Submit(host, slot, new PlaceBuildingPayload(
                        DefId(host, slot, UnitRole.Refinery), (ushort)c.RefineryOriginX, (ushort)c.RefineryOriginY), ref aiSequence);
                    state.RefineryPlaced = true;
                }
            }

            // Once the completed Refinery stands: rally onto the field edge
            // and queue the two Harvesters (D-077 producer assignment).
            if (!state.HarvestersQueued)
            {
                uint refineryRaw = FindRoleRaw(host, slot, UnitRole.Refinery);
                if (refineryRaw != 0)
                {
                    Submit(host, slot, new SetRallyPointPayload(
                        refineryRaw, SimFixed.FromInt(c.RefineryRallyX), SimFixed.FromInt(c.RefineryRallyY)), ref aiSequence);
                    Submit(host, slot, new QueueUnitPayload(
                        refineryRaw, DefId(host, slot, UnitRole.Harvester), 2), ref aiSequence);
                    state.HarvestersQueued = true;
                }
            }

            // Every harvester without a standing harvest order gets one —
            // covers each produced harvester exactly once.
            uint[] idle = IdleHarvesterRaws(host, slot);
            if (idle.Length > 0)
            {
                Submit(host, slot, new HarvestPayload(idle, c.FieldId), ref aiSequence);
            }
        }

        // ----------------------------------------------------------------
        // Match setup and host construction
        // ----------------------------------------------------------------

        /// <summary>
        /// Slot 0 base layout (bottom-left). Buildings use 3x3 footprint
        /// origins; the build position stands in Chebyshev reach 1 of the
        /// Refinery site, and the rally cell stands in reach 1 of the field
        /// cell AND the Refinery footprint, so the harvest/return cycle
        /// closes without walking (the documented economy reach rule).
        /// </summary>
        private static readonly SlotLayout Slot0Layout = new SlotLayout
        {
            FieldId = 1, FieldX = 7, FieldY = 7,
            HqOriginX = 4, HqOriginY = 4,
            BuilderSpawnX = 13, BuilderSpawnY = 7,
            RefineryOriginX = 8, RefineryOriginY = 4, RefineryBuildX = 10, RefineryBuildY = 7,
            RefineryRallyX = 7, RefineryRallyY = 6,
        };

        /// <summary>Slot 1 base layout (top-right), the exact D-107 point/footprint mirror of slot 0.</summary>
        private static readonly SlotLayout Slot1Layout = new SlotLayout
        {
            FieldId = 2, FieldX = 117, FieldY = 117,
            HqOriginX = 118, HqOriginY = 118,
            BuilderSpawnX = 111, BuilderSpawnY = 117,
            RefineryOriginX = 114, RefineryOriginY = 118, RefineryBuildX = 114, RefineryBuildY = 117,
            RefineryRallyX = 117, RefineryRallyY = 118,
        };

        /// <summary>
        /// Applies the deterministic match setup to a fresh host: the five
        /// canonical fields in ascending id order, then per slot the D-077
        /// start state of quality/content/mvp-v1.json
        /// (startStatePerPlayer) — a COMPLETED HQ, ONE Builder and the 3.000
        /// AE of <see cref="EconomySystem.CanonicalMatchStartingCreditsAE"/>
        /// (wired by <see cref="BuildHost"/>). No
        /// pre-placed Refinery, no Harvesters, no skirmish squad: the loop
        /// start is scripted, not spawned. Deterministic entity order (HQ,
        /// Builder; slot 0 first) means identical entity ids on every
        /// host and platform. The slot factions are already bound —
        /// <see cref="BuildHost"/> assigns them before <c>Kernel.Start()</c>,
        /// which the <see cref="EconomySystem.SetSlotFaction"/> guard
        /// requires.
        /// </summary>
        private static SlotState[] SetupMatch(Host host)
        {
            var slots = new[] { new SlotState(), new SlotState() };
            for (int f = 0; f < FieldLayouts.Length; f++)
            {
                FieldLayout field = FieldLayouts[f];
                if (!host.Economy.TryAddField(field.Id, new GridPos2D(field.X, field.Y), field.ReserveAE))
                {
                    throw new InvalidOperationException($"field {field.Id} could not be registered");
                }
            }
            for (byte slot = 0; slot < 2; slot++)
            {
                SlotLayout c = slot == HumanSlot ? Slot0Layout : Slot1Layout;
                if (!host.Construction.PlaceCompletedBuilding(slot, DefId(host, slot, UnitRole.HQ), c.HqOriginX, c.HqOriginY).IsValid)
                {
                    throw new InvalidOperationException("HQ placement failed");
                }

                slots[slot].Builder = host.Entities.SpawnUnit(
                    slot, new Transform2D(SimFixed.FromInt(c.BuilderSpawnX), SimFixed.FromInt(c.BuilderSpawnY)),
                    SimFixed.FromInt(3), role: UnitRole.Builder);
            }
            return slots;
        }

        /// <summary>
        /// Builds a fresh canonical host: all G1 domains in the canonical
        /// tick order of SimulationCore.md section 2 (economy phases 2/3,
        /// construction and production phases 4/5 BEFORE pathfinding/
        /// movement phase 6, then the 5 Hz FoW recompute, then combat, then
        /// the D-056 victory evaluation LAST), the sealed session/ingress
        /// command pipeline, slots 0+1 active, input delay 1.
        /// </summary>
        private static Host BuildHost(ulong seed, INovaLogger logger)
        {
            var kernel = new SimulationKernel(new SimRandom(seed), logger);

            var entities = new EntityManager(EntityCapacity);
            var pathfinding = new PathfindingSystem(MapWidth, MapHeight);
            var movement = new MovementSystem(entities, pathfinding);
            // The D-077 start balance (3.000 AE): the same constant
            // MatchRunner plumbs into the Unity host, so both hosts hash the
            // identical initial state.
            var economy = new EconomySystem(entities, EconomySystem.CanonicalMatchStartingCreditsAE);
            var construction = new ConstructionSystem(entities, economy, pathfinding.CostField);
            var production = new ProductionSystem(entities, economy, construction);
            var fogOfWar = new FogOfWarSystem(entities, construction, economy, teamCount: 2, MapWidth, MapHeight);
            var combat = new Nova.Simulation.Combat.CombatSystem(entities, fogOfWar, economy, construction);
            var victory = new Nova.Simulation.Victory.VictorySystem(entities, construction);

            kernel.RegisterSystem(economy);
            kernel.RegisterSystem(construction);
            kernel.RegisterSystem(production);
            kernel.RegisterSystem(pathfinding);
            kernel.RegisterSystem(movement);
            kernel.RegisterSystem(fogOfWar);
            kernel.RegisterSystem(combat);
            kernel.RegisterSystem(victory);

            var session = new MatchSession(HumanSlot, activeSlots: new byte[] { HumanSlot, AiSlot }, inputDelayTicks: 1);
            var ingress = new CommandIngress(session);
            _ = new LocalLoopbackTransport(ingress);
            kernel.BindCommands(
                new UnitCommandStateView(entities, pathfinding, economy, construction, production), ingress);

            // Faction assignment (economy block v2): slot 0 Alliance, slot 1
            // Legion. Set BEFORE Kernel.Start() — the SetSlotFaction guard
            // forbids any change once the kernel runs, because the faction
            // bytes are part of the hashed initial state. MatchBootstrap does
            // the same, in the same order.
            economy.SetSlotFaction(HumanSlot, FactionId.Alliance);
            economy.SetSlotFaction(AiSlot, FactionId.Legion);

            kernel.Start();
            return new Host
            {
                Kernel = kernel,
                Entities = entities,
                Economy = economy,
                Construction = construction,
                Session = session,
                Ingress = ingress,
            };
        }

        /// <summary>
        /// The standard match configuration fingerprint: slot 0
        /// human/Alliance, slot 1 AI/Legion, current rules hash, stub map hash and the
        /// REAL canonical definitions hash (SimDefinitions.ComputeDefinitionsHash64
        /// — a replay recorded against a different definition table refuses
        /// to start, SimulationCore.md section 6).
        /// </summary>
        private static MatchFingerprint CreateFingerprint(Host host, ulong seed)
        {
            var slots = new byte[CommandLimits.ReservedPlayerSlots];
            slots[HumanSlot] = (byte)PlayerSlotOccupancy.Human;
            slots[AiSlot] = (byte)PlayerSlotOccupancy.AI;
            var factions = new byte[CommandLimits.ReservedPlayerSlots];
            factions[HumanSlot] = (byte)FactionId.Alliance;
            factions[AiSlot] = (byte)FactionId.Legion;
            return MatchFingerprint.CreateCurrent(
                MatchFingerprint.ComputeCurrentRulesHash64(),
                SimDefinitions.ComputeDefinitionsHash64(),
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Map),
                slots,
                factions,
                seed,
                host.Kernel.CalculateStateHash(),
                host.Session.InputDelayTicks);
        }

        /// <summary>One host lockstep iteration: seal the due batch, submit it, step, advance the session.</summary>
        private static CommandBatch SealAndSubmit(Host host)
        {
            uint nextTick = host.Kernel.CurrentTick.Value + 1;
            CommandBatch batch = host.Ingress.SealTickBatch(nextTick);
            if (batch.Count > 0 && !host.Kernel.SubmitBatch(batch))
            {
                throw new InvalidOperationException($"kernel refused the sealed batch of tick {nextTick}");
            }
            host.Kernel.StepTick();
            host.Session.AdvanceTick();
            return batch;
        }

        // ----------------------------------------------------------------
        // Command submission (slot 0 intents, slot 1 crafted records)
        // ----------------------------------------------------------------

        private delegate TPayload PayloadFactory<TPayload>(uint[] ids) where TPayload : struct, ICommandPayload;

        /// <summary>Submits a single-entity payload when the entity is still alive.</summary>
        private static void SubmitIfAlive<TPayload>(
            Host host, EntityId entity, byte slot, ref uint aiSequence, PayloadFactory<TPayload> factory)
            where TPayload : struct, ICommandPayload
        {
            if (!host.Entities.IsValid(entity))
            {
                return;
            }
            Submit(host, slot, factory(new[] { UnitCommandStateView.ToRawEntityId(entity) }), ref aiSequence);
        }

        /// <summary>
        /// Enters one scripted command into the sealed stream: slot 0 as a
        /// local intent (human path), slot 1 as a crafted canonical wire
        /// record (the stand-in AI transport). Structural rejection of a
        /// self-generated command is a harness bug and throws.
        /// </summary>
        private static void Submit<TPayload>(Host host, byte slot, TPayload payload, ref uint aiSequence)
            where TPayload : struct, ICommandPayload
        {
            if (slot == HumanSlot)
            {
                CommandIngressResult result = host.Ingress.TrySubmitIntent(
                    CommandIntent.Create(payload), out CommandRejectReason reason);
                if (result != CommandIngressResult.Accepted)
                {
                    throw new InvalidOperationException($"scripted human intent rejected: {result} ({reason})");
                }
                return;
            }

            var writer = new CommandPayloadWriter();
            payload.WriteTo(writer);
            byte[] payloadBytes = writer.ToArray();
            byte[] recordBytes = CraftRecord(
                enqueueTick: host.Session.CurrentTick,
                targetTick: host.Session.CurrentTick + host.Session.InputDelayTicks,
                playerSlot: slot,
                sequence: aiSequence++,
                kind: (ushort)payload.Kind,
                payloadVersion: CommandLimits.PayloadVersionV1,
                payload: payloadBytes);
            CommandIngressResult intake = host.Ingress.TryAcceptRecordBytes(recordBytes, out CommandRejectReason rejectReason);
            if (intake != CommandIngressResult.Accepted)
            {
                throw new InvalidOperationException($"scripted AI record rejected: {intake} ({rejectReason})");
            }
        }

        /// <summary>Builds a raw canonical record byte array field by field (little-endian, schema v1).</summary>
        private static byte[] CraftRecord(
            uint enqueueTick, uint targetTick, byte playerSlot, uint sequence,
            ushort kind, byte payloadVersion, byte[] payload)
        {
            int recordLength = CommandLimits.HeaderBytes + payload.Length;
            var bytes = new byte[recordLength];
            WriteUInt16(bytes, 0, (ushort)recordLength);
            WriteUInt32(bytes, 2, enqueueTick);
            WriteUInt32(bytes, 6, targetTick);
            bytes[10] = playerSlot;
            WriteUInt32(bytes, 11, sequence);
            WriteUInt16(bytes, 15, kind);
            bytes[17] = payloadVersion;
            WriteUInt16(bytes, 18, (ushort)payload.Length);
            Array.Copy(payload, 0, bytes, CommandLimits.HeaderBytes, payload.Length);
            return bytes;
        }

        // ----------------------------------------------------------------
        // Deterministic host scans (ascending entity index)
        // ----------------------------------------------------------------

        /// <summary>Raw id of the first active completed/non-site entity of <paramref name="slot"/> with the role, else 0.</summary>
        private static uint FindRoleRaw(Host host, byte slot, UnitRole role)
        {
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].PlayerId == slot && units[i].Role == role
                    && !host.Construction.IsActiveSite(units[i].Id))
                {
                    return UnitCommandStateView.ToRawEntityId(units[i].Id);
                }
            }
            return 0;
        }

        /// <summary>Raw ids of all active own harvesters without a standing harvest order, ascending index.</summary>
        private static uint[] IdleHarvesterRaws(Host host, byte slot)
        {
            var raws = new List<uint>();
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].PlayerId == slot
                    && units[i].Role == UnitRole.Harvester && units[i].HarvestFieldId == 0)
                {
                    raws.Add(UnitCommandStateView.ToRawEntityId(units[i].Id));
                }
            }
            return raws.ToArray();
        }

        // ----------------------------------------------------------------

        internal static string Sha256Hex(byte[] bytes)
        {
            byte[] hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static void WriteUInt16(byte[] bytes, int offset, ushort value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
        }

        private static void WriteUInt32(byte[] bytes, int offset, uint value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
            bytes[offset + 2] = (byte)(value >> 16);
            bytes[offset + 3] = (byte)(value >> 24);
        }
    }
}
