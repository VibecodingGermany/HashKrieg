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
    /// Workload (why this exercises every domain): the match setup is the
    /// MS-1 manifest start state of quality/content/mvp-v1.json
    /// (startStatePerPlayer) per slot — a COMPLETED HQ, a COMPLETED Refinery
    /// (the manifest's only prerequisite exception), one Builder, two
    /// Harvesters and 1.000 AE — plus one Aetherium field per slot adjacent
    /// to both harvesters and the refinery, and a documented four-unit
    /// skirmish squad per slot facing each other in weapon range in midfield
    /// (a deliberate addition so the combat domain runs from the opening
    /// ticks without depending on a long production pipeline). The script
    /// then drives, for BOTH slots: harvest and return-cargo cycles
    /// (economy), Barracks/Power/ResearchLab/DefensePlatform/Storage
    /// placements including one CancelConstruction (construction), infantry
    /// queues including one T2 queue after the lab, one CancelProduction and
    /// rally points (production), repeated moves of builders and combat
    /// units (movement/pathfinding), focus-fire AttackTarget orders of the
    /// skirmish squads and later of the produced infantry (combat + FoW),
    /// one Sell and one Stop. Slot 0 commands enter as local intents
    /// (human), slot 1 commands as crafted wire records (the stand-in "AI"
    /// transport); state-dependent rejections stay in the recorded stream
    /// with their deterministic results (Commands.md section 4).
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
        private const long FieldReserveAE = 2000000L;

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

        /// <summary>Script timing (target ticks of the sealed batches).</summary>
        private const int SkirmishFirstOrderTick = 6;
        private const int SkirmishRetargetPeriod = 10;
        private const int SkirmishLastRetargetTick = 56;
        private const int ReturnCargoFirstTick = 360;
        private const int ReturnCargoPeriod = 150;
        private const int ProducedAttackFirstTick = 1000;
        private const int ProducedAttackPeriod = 500;
        private const int ProducedMoveFirstTick = 1250;
        private const int ProducedMovePeriod = 500;

        /// <summary>Fixed map layout of one slot's base (all coordinates in grid cells).</summary>
        private sealed class SlotLayout
        {
            public ushort FieldId;
            public int FieldX, FieldY;
            public int HqOriginX, HqOriginY;
            public int RefineryOriginX, RefineryOriginY;
            public int HarvesterAX, HarvesterAY, HarvesterBX, HarvesterBY;
            public int BuilderSpawnX, BuilderSpawnY;
            public int BarracksOriginX, BarracksOriginY, BarracksBuildX, BarracksBuildY;
            public int PowerOriginX, PowerOriginY, PowerBuildX, PowerBuildY;
            public int LabOriginX, LabOriginY, LabBuildX, LabBuildY;
            public int DefenseOriginX, DefenseOriginY, DefenseBuildX, DefenseBuildY;
            public int StorageOriginX, StorageOriginY, StorageBuildX, StorageBuildY;
            public int RallyX, RallyY, HqRallyX, HqRallyY;
        }

        /// <summary>Live handles the script queries against (captured at setup, plus deterministic scans).</summary>
        private sealed class SlotState
        {
            public EntityId Builder;
            public EntityId HarvesterA;
            public EntityId HarvesterB;
            public EntityId[] Squad = new EntityId[4];
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
        /// Build order (GDD power figures, Buildings.md): the Power plant
        /// comes FIRST (tick 25) — the manifest start grid (HQ 30 provided,
        /// Refinery 20/15 required) cannot power the Alliance Barracks' 15 —
        /// the Barracks follows at tick 320, infantry production once it
        /// stands (tick 540/900). Definition ids are faction-resolved per
        /// slot (<see cref="DefId"/>): the same script drives the Alliance
        /// rows on slot 0 and the Legion rows on slot 1.
        /// </para>
        /// </summary>
        private static void IssueSlotCommands(
            Host host, SlotState[] slots, byte slot, uint nextTick, ref uint aiSequence)
        {
            SlotLayout c = slot == HumanSlot ? Slot0Layout : Slot1Layout;
            SlotState state = slots[slot];
            byte enemy = slot == HumanSlot ? AiSlot : HumanSlot;
            SlotState enemyState = slots[enemy];
            int tick = (int)nextTick;

            switch (tick)
            {
                case 1:
                    SubmitIfAlive(host, state.HarvesterA, slot, ref aiSequence,
                        ids => new HarvestPayload(ids, c.FieldId));
                    SubmitIfAlive(host, state.HarvesterB, slot, ref aiSequence,
                        ids => new HarvestPayload(ids, c.FieldId));
                    SubmitIfAlive(host, state.Builder, slot, ref aiSequence,
                        ids => new MovePayload(ids, SimFixed.FromInt(c.PowerBuildX), SimFixed.FromInt(c.PowerBuildY)));
                    break;
                case 25:
                    Submit(host, slot, new PlaceBuildingPayload(DefId(host, slot, UnitRole.Power), (ushort)c.PowerOriginX, (ushort)c.PowerOriginY), ref aiSequence);
                    break;
                case 300:
                    SubmitIfAlive(host, state.Builder, slot, ref aiSequence,
                        ids => new MovePayload(ids, SimFixed.FromInt(c.BarracksBuildX), SimFixed.FromInt(c.BarracksBuildY)));
                    break;
                case 320:
                    Submit(host, slot, new PlaceBuildingPayload(DefId(host, slot, UnitRole.Barracks), (ushort)c.BarracksOriginX, (ushort)c.BarracksOriginY), ref aiSequence);
                    break;
                case 520:
                    SubmitIfAlive(host, state.Builder, slot, ref aiSequence,
                        ids => new MovePayload(ids, SimFixed.FromInt(c.LabBuildX), SimFixed.FromInt(c.LabBuildY)));
                    break;
                case 540:
                {
                    uint barracks = FindRoleRaw(host, slot, UnitRole.Barracks);
                    if (barracks != 0)
                    {
                        Submit(host, slot, new SetRallyPointPayload(barracks, SimFixed.FromInt(c.RallyX), SimFixed.FromInt(c.RallyY)), ref aiSequence);
                        Submit(host, slot, new QueueUnitPayload(barracks, DefId(host, slot, UnitRole.BasicInfantry), 2), ref aiSequence);
                    }
                    break;
                }
                case 700:
                    Submit(host, slot, new PlaceBuildingPayload(DefId(host, slot, UnitRole.ResearchLab), (ushort)c.LabOriginX, (ushort)c.LabOriginY), ref aiSequence);
                    break;
                case 900:
                {
                    uint barracks = FindRoleRaw(host, slot, UnitRole.Barracks);
                    if (barracks != 0)
                    {
                        Submit(host, slot, new QueueUnitPayload(barracks, DefId(host, slot, UnitRole.BasicInfantry), 2), ref aiSequence);
                    }
                    break;
                }
                case 1200:
                {
                    uint barracks = FindRoleRaw(host, slot, UnitRole.Barracks);
                    if (barracks != 0)
                    {
                        Submit(host, slot, new QueueUnitPayload(barracks, DefId(host, slot, UnitRole.AntiArmorInfantry), 1), ref aiSequence);
                    }
                    break;
                }
                case 1300:
                {
                    uint barracks = FindRoleRaw(host, slot, UnitRole.Barracks);
                    if (barracks != 0)
                    {
                        Submit(host, slot, new CancelProductionPayload(barracks, 0), ref aiSequence);
                    }
                    break;
                }
                case 1450:
                {
                    uint hq = FindRoleRaw(host, slot, UnitRole.HQ);
                    if (hq != 0)
                    {
                        Submit(host, slot, new SetRallyPointPayload(hq, SimFixed.FromInt(c.HqRallyX), SimFixed.FromInt(c.HqRallyY)), ref aiSequence);
                    }
                    break;
                }
                case 1500:
                {
                    uint hq = FindRoleRaw(host, slot, UnitRole.HQ);
                    if (hq != 0)
                    {
                        Submit(host, slot, new QueueUnitPayload(hq, DefId(host, slot, UnitRole.Builder), 1), ref aiSequence);
                    }
                    break;
                }
                case 1700:
                {
                    uint hq = FindRoleRaw(host, slot, UnitRole.HQ);
                    if (hq != 0)
                    {
                        Submit(host, slot, new QueueUnitPayload(hq, DefId(host, slot, UnitRole.Harvester), 1), ref aiSequence);
                    }
                    break;
                }
                case 1980:
                    SubmitIfAlive(host, state.Builder, slot, ref aiSequence,
                        ids => new MovePayload(ids, SimFixed.FromInt(c.DefenseBuildX), SimFixed.FromInt(c.DefenseBuildY)));
                    break;
                case 2000:
                    Submit(host, slot, new PlaceBuildingPayload(DefId(host, slot, UnitRole.DefensePlatform), (ushort)c.DefenseOriginX, (ushort)c.DefenseOriginY), ref aiSequence);
                    break;
                case 2500:
                {
                    uint defense = FindRoleRaw(host, slot, UnitRole.DefensePlatform);
                    if (defense != 0)
                    {
                        Submit(host, slot, new SellPayload(defense), ref aiSequence);
                    }
                    break;
                }
                case 2980:
                    SubmitIfAlive(host, state.Builder, slot, ref aiSequence,
                        ids => new MovePayload(ids, SimFixed.FromInt(c.StorageBuildX), SimFixed.FromInt(c.StorageBuildY)));
                    break;
                case 3000:
                    Submit(host, slot, new PlaceBuildingPayload(DefId(host, slot, UnitRole.Storage), (ushort)c.StorageOriginX, (ushort)c.StorageOriginY), ref aiSequence);
                    break;
                case 3050:
                {
                    uint site = FindConstructionSiteRaw(host, slot);
                    if (site != 0)
                    {
                        Submit(host, slot, new CancelConstructionPayload(site), ref aiSequence);
                    }
                    break;
                }
                case 4000:
                    SubmitIfAlive(host, state.Builder, slot, ref aiSequence, ids => new StopPayload(ids));
                    break;
            }

            // Skirmish focus fire: every SkirmishRetargetPeriod ticks in the
            // opening window the surviving squad re-targets the lowest-index
            // surviving enemy squad member (deterministic ascending scan).
            if (tick >= SkirmishFirstOrderTick && tick <= SkirmishLastRetargetTick
                && (tick - SkirmishFirstOrderTick) % SkirmishRetargetPeriod == 0)
            {
                uint[] ownSquad = AliveRaws(host, state.Squad);
                uint[] enemySquad = AliveRaws(host, enemyState.Squad);
                if (ownSquad.Length > 0 && enemySquad.Length > 0)
                {
                    Submit(host, slot, new AttackTargetPayload(ownSquad, enemySquad[0]), ref aiSequence);
                }
            }

            // Economy cycle: deliver cargo every ReturnCargoPeriod ticks;
            // re-issue the harvest order to any idle harvester one tick later
            // (covers the produced harvester and resolved orders).
            if (tick >= ReturnCargoFirstTick && (tick - ReturnCargoFirstTick) % ReturnCargoPeriod == 0)
            {
                uint[] harvesters = AliveRaws(host, state.HarvesterA, state.HarvesterB);
                if (harvesters.Length > 0)
                {
                    Submit(host, slot, new ReturnCargoPayload(harvesters), ref aiSequence);
                }
            }
            if (tick >= ReturnCargoFirstTick + 1 && (tick - ReturnCargoFirstTick - 1) % ReturnCargoPeriod == 0)
            {
                uint[] idle = IdleHarvesterRaws(host, slot);
                if (idle.Length > 0)
                {
                    Submit(host, slot, new HarvestPayload(idle, c.FieldId), ref aiSequence);
                }
            }

            // Produced army: periodic attack on the lowest-index live enemy
            // unit, alternating midfield moves half a period offset.
            if (tick >= ProducedAttackFirstTick && (tick - ProducedAttackFirstTick) % ProducedAttackPeriod == 0)
            {
                uint[] army = OwnCombatRaws(host, slot);
                uint target = FirstLiveEnemyRaw(host, enemy);
                if (army.Length > 0 && target != 0)
                {
                    Submit(host, slot, new AttackTargetPayload(army, target), ref aiSequence);
                }
            }
            if (tick >= ProducedMoveFirstTick && (tick - ProducedMoveFirstTick) % ProducedMovePeriod == 0)
            {
                uint[] army = OwnCombatRaws(host, slot);
                if (army.Length > 0)
                {
                    int waypoint = ((tick - ProducedMoveFirstTick) / ProducedMovePeriod) % 2 == 0 ? 62 : 66;
                    Submit(host, slot, new MovePayload(army, SimFixed.FromInt(waypoint), SimFixed.FromInt(waypoint)), ref aiSequence);
                }
            }
        }

        // ----------------------------------------------------------------
        // Match setup and host construction
        // ----------------------------------------------------------------

        /// <summary>
        /// Slot 0 base layout (bottom-left). Buildings use 3x3 footprint
        /// origins; build positions stand in Chebyshev reach 1 of their site;
        /// both harvesters stand in reach 1 of the field cell AND the
        /// refinery footprint, so the harvest/return cycle runs without
        /// walking (the documented economy reach rule).
        /// </summary>
        private static readonly SlotLayout Slot0Layout = new SlotLayout
        {
            FieldId = 1, FieldX = 7, FieldY = 7,
            HqOriginX = 4, HqOriginY = 4,
            RefineryOriginX = 8, RefineryOriginY = 4,
            HarvesterAX = 7, HarvesterAY = 6,
            HarvesterBX = 7, HarvesterBY = 7,
            BuilderSpawnX = 13, BuilderSpawnY = 7,
            BarracksOriginX = 13, BarracksOriginY = 9, BarracksBuildX = 13, BarracksBuildY = 8,
            PowerOriginX = 8, PowerOriginY = 8, PowerBuildX = 10, PowerBuildY = 7,
            LabOriginX = 13, LabOriginY = 13, LabBuildX = 14, LabBuildY = 12,
            DefenseOriginX = 4, DefenseOriginY = 9, DefenseBuildX = 5, DefenseBuildY = 8,
            StorageOriginX = 17, StorageOriginY = 4, StorageBuildX = 16, StorageBuildY = 5,
            RallyX = 50, RallyY = 50,
            HqRallyX = 30, HqRallyY = 30,
        };

        /// <summary>Slot 1 base layout (top-right), the 180-degree mirror of slot 0.</summary>
        private static readonly SlotLayout Slot1Layout = new SlotLayout
        {
            FieldId = 2, FieldX = 119, FieldY = 119,
            HqOriginX = 120, HqOriginY = 120,
            RefineryOriginX = 116, RefineryOriginY = 120,
            HarvesterAX = 119, HarvesterAY = 120,
            HarvesterBX = 119, HarvesterBY = 119,
            BuilderSpawnX = 113, BuilderSpawnY = 119,
            BarracksOriginX = 111, BarracksOriginY = 115, BarracksBuildX = 113, BarracksBuildY = 118,
            PowerOriginX = 116, PowerOriginY = 116, PowerBuildX = 116, PowerBuildY = 119,
            LabOriginX = 111, LabOriginY = 111, LabBuildX = 112, LabBuildY = 114,
            DefenseOriginX = 120, DefenseOriginY = 115, DefenseBuildX = 121, DefenseBuildY = 118,
            StorageOriginX = 107, StorageOriginY = 120, StorageBuildX = 110, StorageBuildY = 121,
            RallyX = 76, RallyY = 76,
            HqRallyX = 96, HqRallyY = 96,
        };

        /// <summary>Skirmish squads: four infantry per slot facing each other across a gap of exactly weapon range.</summary>
        private static readonly int[] Squad0X = { 56, 57, 56, 57 };
        private static readonly int[] Squad0Y = { 62, 62, 63, 63 };
        private static readonly int[] Squad1X = { 65, 66, 65, 66 };
        private static readonly int[] Squad1Y = { 62, 62, 63, 63 };

        /// <summary>
        /// Applies the deterministic match setup to a fresh host: per slot
        /// the MS-1 manifest start state (completed HQ + Refinery, one
        /// Builder, two Harvesters, 1.000 AE economy default), one Aetherium
        /// field and the four-unit skirmish squad. Deterministic spawn order
        /// means identical entity ids on every host and platform. The slot
        /// factions are already bound — <see cref="BuildHost"/> assigns them
        /// before <c>Kernel.Start()</c>, which the
        /// <see cref="EconomySystem.SetSlotFaction"/> guard requires.
        /// </summary>
        private static SlotState[] SetupMatch(Host host)
        {
            var slots = new[] { new SlotState(), new SlotState() };
            for (byte slot = 0; slot < 2; slot++)
            {
                SlotLayout c = slot == HumanSlot ? Slot0Layout : Slot1Layout;
                if (!host.Economy.TryAddField(c.FieldId, new GridPos2D(c.FieldX, c.FieldY), FieldReserveAE))
                {
                    throw new InvalidOperationException($"field {c.FieldId} could not be registered");
                }
                if (!host.Construction.PlaceCompletedBuilding(slot, DefId(host, slot, UnitRole.HQ), c.HqOriginX, c.HqOriginY).IsValid)
                {
                    throw new InvalidOperationException("HQ placement failed");
                }
                // The manifest's only prerequisite exception: the start
                // Refinery exists WITHOUT a Power plant and spawns no
                // additional Harvester.
                if (!host.Construction.PlaceCompletedBuilding(slot, DefId(host, slot, UnitRole.Refinery), c.RefineryOriginX, c.RefineryOriginY).IsValid)
                {
                    throw new InvalidOperationException("Refinery placement failed");
                }

                SlotState state = slots[slot];
                state.HarvesterA = host.Entities.SpawnUnit(
                    slot, new Transform2D(SimFixed.FromInt(c.HarvesterAX), SimFixed.FromInt(c.HarvesterAY)),
                    SimFixed.FromRaw(163840), role: UnitRole.Harvester);
                state.HarvesterB = host.Entities.SpawnUnit(
                    slot, new Transform2D(SimFixed.FromInt(c.HarvesterBX), SimFixed.FromInt(c.HarvesterBY)),
                    SimFixed.FromRaw(163840), role: UnitRole.Harvester);
                state.Builder = host.Entities.SpawnUnit(
                    slot, new Transform2D(SimFixed.FromInt(c.BuilderSpawnX), SimFixed.FromInt(c.BuilderSpawnY)),
                    SimFixed.FromInt(3), role: UnitRole.Builder);

                int[] squadX = slot == HumanSlot ? Squad0X : Squad1X;
                int[] squadY = slot == HumanSlot ? Squad0Y : Squad1Y;
                for (int i = 0; i < 4; i++)
                {
                    state.Squad[i] = host.Entities.SpawnUnit(
                        slot, new Transform2D(SimFixed.FromInt(squadX[i]), SimFixed.FromInt(squadY[i])),
                        SimFixed.FromInt(4), role: UnitRole.BasicInfantry);
                }
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
            var economy = new EconomySystem(entities);
            var construction = new ConstructionSystem(entities, economy);
            var production = new ProductionSystem(entities, economy, construction);
            var fogOfWar = new FogOfWarSystem(entities, teamCount: 2, MapWidth, MapHeight);
            var combat = new Nova.Simulation.Combat.CombatSystem(entities, fogOfWar, economy);
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
        /// human/Alliance, slot 1 AI/Legion, stub rules/map hashes and the
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
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Rules),
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

        /// <summary>Raw id of the first active entity of <paramref name="slot"/> with the role, else 0.</summary>
        private static uint FindRoleRaw(Host host, byte slot, UnitRole role)
        {
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].PlayerId == slot && units[i].Role == role)
                {
                    return UnitCommandStateView.ToRawEntityId(units[i].Id);
                }
            }
            return 0;
        }

        /// <summary>
        /// Raw id of the first active construction site of <paramref name="slot"/>
        /// (sites carry the plain Unit role until completion), else 0.
        /// </summary>
        private static uint FindConstructionSiteRaw(Host host, byte slot)
        {
            return FindRoleRaw(host, slot, UnitRole.Unit);
        }

        /// <summary>Raw ids of the live entities of the given handles, ascending by handle order.</summary>
        private static uint[] AliveRaws(Host host, params EntityId[] entities)
        {
            var raws = new List<uint>(entities.Length);
            foreach (EntityId entity in entities)
            {
                if (host.Entities.IsValid(entity))
                {
                    raws.Add(UnitCommandStateView.ToRawEntityId(entity));
                }
            }
            return raws.ToArray();
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

        /// <summary>Raw ids of all active own infantry (T1 and T2), ascending index.</summary>
        private static uint[] OwnCombatRaws(Host host, byte slot)
        {
            var raws = new List<uint>();
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].PlayerId == slot
                    && (units[i].Role == UnitRole.BasicInfantry || units[i].Role == UnitRole.AntiArmorInfantry))
                {
                    raws.Add(UnitCommandStateView.ToRawEntityId(units[i].Id));
                }
            }
            return raws.ToArray();
        }

        /// <summary>Raw id of the lowest-index active entity of <paramref name="enemySlot"/>, else 0.</summary>
        private static uint FirstLiveEnemyRaw(Host host, byte enemySlot)
        {
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].PlayerId == enemySlot)
                {
                    return UnitCommandStateView.ToRawEntityId(units[i].Id);
                }
            }
            return 0;
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
