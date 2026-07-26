using System;
using System.Collections.Generic;
using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Replays;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Shared fixtures for the replay EditMode suites. Mirror of the .NET
    /// lane ReplayTestUtil with Unity Test Framework conventions: a full
    /// canonical host, the deterministic shadow "AI" of slot 1 (an intent
    /// generator during the live run, switched off at playback) and the
    /// standard 50-tick live match of SimulationCore.md section 8.
    /// </summary>
    internal static class ReplayV1TestUtil
    {
        internal const ulong Seed = 0x5EED42UL;
        internal const int MatchTicks = 50;
        internal const byte HumanSlot = 0;
        internal const byte AiSlot = 1;

        /// <summary>
        /// The deterministic slot-1 "AI": at fixed ticks it wants a Move of
        /// its units. During the live run the host injects its output as
        /// crafted records; at playback it is never instantiated — the
        /// recorded stream carries its accepted commands (SimulationCore.md
        /// section 4). A shadow instance may only compare diagnostically.
        /// </summary>
        internal static bool ShadowAiWantsMove(int forTick, out int targetX, out int targetY)
        {
            switch (forTick)
            {
                case 5: targetX = 20; targetY = 20; return true;
                case 30: targetX = 25; targetY = 25; return true;
                default: targetX = 0; targetY = 0; return false;
            }
        }

        internal sealed class TestHost
        {
            public SimulationKernel Kernel { get; }
            public EntityManager Entities { get; }
            public PathfindingSystem Pathfinding { get; }
            public MatchSession Session { get; }
            public CommandIngress Ingress { get; }

            private TestHost(
                SimulationKernel kernel, EntityManager entities,
                PathfindingSystem pathfinding, MatchSession session, CommandIngress ingress)
            {
                Kernel = kernel;
                Entities = entities;
                Pathfinding = pathfinding;
                Session = session;
                Ingress = ingress;
            }

            public static TestHost Create(
                ulong seed, int capacity = 256, ushort width = 64, ushort height = 64)
            {
                var entities = new EntityManager(capacity);
                var pathfinding = new PathfindingSystem(width, height);
                var movement = new MovementSystem(entities, pathfinding);
                var economy = new EconomySystem(entities);

                var kernel = new SimulationKernel(new SimRandom(seed));
                kernel.RegisterSystem(economy);
                kernel.RegisterSystem(pathfinding);
                kernel.RegisterSystem(movement);

                var session = new MatchSession(
                    localSlot: HumanSlot, activeSlots: new byte[] { HumanSlot, AiSlot }, inputDelayTicks: 1);
                var ingress = new CommandIngress(session);
                _ = new LocalLoopbackTransport(ingress);
                kernel.BindCommands(new UnitCommandStateView(entities, pathfinding, economy), ingress);

                kernel.Start();
                return new TestHost(kernel, entities, pathfinding, session, ingress);
            }

            /// <summary>
            /// One host lockstep iteration: seal the due batch, submit it,
            /// step the kernel, advance the session; returns the sealed batch
            /// so the recorder can capture exactly the applied records.
            /// </summary>
            public CommandBatch StepTick()
            {
                uint nextTick = Kernel.CurrentTick.Value + 1;
                CommandBatch batch = Ingress.SealTickBatch(nextTick);
                if (batch.Count > 0)
                {
                    Assert.IsTrue(Kernel.SubmitBatch(batch), "a sealed batch must be accepted");
                }
                Kernel.StepTick();
                Session.AdvanceTick();
                return batch;
            }

            /// <summary>Spawns a row of units and returns their packed wire ids in ascending order.</summary>
            public uint[] SpawnUnits(byte owner, int count, float startX, float y)
            {
                var ids = new uint[count];
                for (int i = 0; i < count; i++)
                {
                    EntityId id = Entities.SpawnUnit(owner, new Transform2D(SimFixed.FromFloat(startX + i), SimFixed.FromFloat(y)), SimFixed.FromInt(5));
                    ids[i] = UnitCommandStateView.ToRawEntityId(id);
                }
                Array.Sort(ids);
                return ids;
            }

            /// <summary>Submits a human (slot 0) Move intent and asserts acceptance.</summary>
            public void SubmitHumanMove(uint[] rawIds, int targetX, int targetY)
            {
                var payload = new MovePayload(rawIds, SimFixed.FromInt(targetX), SimFixed.FromInt(targetY));
                Assert.AreEqual(
                    CommandIngressResult.Accepted,
                    Ingress.TrySubmitIntent(CommandIntent.Create(payload), out _));
            }

            /// <summary>
            /// Injects one AI (slot 1) record into the live stream: the record
            /// bytes the AI transport would deliver, accepted through the same
            /// validating intake as any remote record.
            /// </summary>
            public void FeedAiMove(uint sequence, uint[] rawIds, int targetX, int targetY)
            {
                byte[] payload = PayloadBytes(
                    new MovePayload(rawIds, SimFixed.FromInt(targetX), SimFixed.FromInt(targetY)));
                byte[] recordBytes = CraftRecord(
                    enqueueTick: Session.CurrentTick,
                    targetTick: Session.CurrentTick + Session.InputDelayTicks,
                    playerSlot: AiSlot,
                    sequence: sequence,
                    kind: (ushort)CommandKind.Move,
                    payloadVersion: CommandLimits.PayloadVersionV1,
                    payload: payload);
                Assert.AreEqual(
                    CommandIngressResult.Accepted,
                    Ingress.TryAcceptRecordBytes(recordBytes, out _));
            }
        }

        internal sealed class LiveMatch
        {
            public byte[] ReplayBytes;
            public MatchFingerprint Fingerprint;
            public byte[] InitialSnapshotBytes;
            public ulong EndStateHash;
            public uint[] HumanUnits;
            public uint[] AiUnits;
        }

        /// <summary>The match configuration of the standard scenario: slot 0 human, slot 1 AI, six free.</summary>
        internal static byte[] StandardSlots()
        {
            var slots = new byte[CommandLimits.ReservedPlayerSlots];
            slots[HumanSlot] = (byte)PlayerSlotOccupancy.Human;
            slots[AiSlot] = (byte)PlayerSlotOccupancy.AI;
            return slots;
        }

        /// <summary>Builds the standard fingerprint over a host's current (initial) state.</summary>
        internal static MatchFingerprint CreateFingerprint(TestHost host, ulong seed, byte[] slots = null)
        {
            return MatchFingerprint.CreateCurrent(
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Rules),
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Definitions),
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Map),
                slots ?? StandardSlots(),
                seed,
                host.Kernel.CalculateStateHash(),
                host.Session.InputDelayTicks);
        }

        /// <summary>
        /// Runs the standard live match: 8 human units, 4 AI units, 50 ticks
        /// with a human Move at ticks 1 and 20, shadow-AI Moves at ticks 5
        /// and 30 (injected as records) and a state-dependently rejected
        /// command at tick 10 (the human orders an AI-owned unit →
        /// RejectedNotOwned, stays in the stream).
        /// <paramref name="forgeResultAtTick"/> records a forged result code
        /// for the first record of that tick (identity preserved) — a
        /// chain-consistent replay whose recorded results no longer match
        /// re-execution, for playback verification tests.
        /// </summary>
        internal static LiveMatch RunLiveMatch(
            ulong seed = Seed, int ticks = MatchTicks,
            int forgeResultAtTick = 0, CommandResultCode forgedCode = CommandResultCode.Applied)
        {
            var host = TestHost.Create(seed);
            uint[] human = host.SpawnUnits(HumanSlot, 8, 10.5f, 10.5f);
            uint[] ai = host.SpawnUnits(AiSlot, 4, 50.5f, 50.5f);

            MatchFingerprint fingerprint = CreateFingerprint(host, seed);
            byte[] snapshot = host.Kernel.SaveSnapshot();
            var recorder = new ReplayRecorder(fingerprint, snapshot);

            uint aiSequence = 1;
            for (int tick = 1; tick <= ticks; tick++)
            {
                if (tick == 1 || tick == 20)
                {
                    host.SubmitHumanMove(human, 30 + tick, 30 + tick);
                }
                if (tick == 10)
                {
                    // State-dependent rejection: slot 0 commands slot-1 units.
                    // Structurally valid, so it enters the stream; the
                    // executor rejects it deterministically at the target tick.
                    host.SubmitHumanMove(ai, 40, 40);
                }
                if (ShadowAiWantsMove(tick, out int aiX, out int aiY))
                {
                    host.FeedAiMove(aiSequence++, ai, aiX, aiY);
                }

                CommandBatch batch = host.StepTick();
                IReadOnlyList<CommandResult> results = host.Kernel.LastTickResults;
                if (tick == forgeResultAtTick && results.Count > 0)
                {
                    var forged = new CommandResult[results.Count];
                    for (int i = 0; i < forged.Length; i++)
                    {
                        forged[i] = i == 0
                            ? new CommandResult(batch.Records[i], forgedCode)
                            : results[i];
                    }
                    results = forged;
                }
                recorder.RecordTick(host.Kernel.CurrentTick.Value, batch, results);
            }

            ulong endHash = host.Kernel.CalculateStateHash();
            return new LiveMatch
            {
                ReplayBytes = recorder.Finalize(endHash),
                Fingerprint = fingerprint,
                InitialSnapshotBytes = snapshot,
                EndStateHash = endHash,
                HumanUnits = human,
                AiUnits = ai,
            };
        }

        /// <summary>
        /// A minimal match for parser tests: 2 human and 2 AI units, 3 ticks;
        /// tick 1 carries one human and one AI Move (equal-length records),
        /// ticks 2 and 3 are empty.
        /// </summary>
        internal static LiveMatch RunSmallMatch(ulong seed = Seed)
        {
            var host = TestHost.Create(seed);
            uint[] human = host.SpawnUnits(HumanSlot, 2, 10.5f, 10.5f);
            uint[] ai = host.SpawnUnits(AiSlot, 2, 50.5f, 50.5f);

            MatchFingerprint fingerprint = CreateFingerprint(host, seed);
            byte[] snapshot = host.Kernel.SaveSnapshot();
            var recorder = new ReplayRecorder(fingerprint, snapshot);

            for (int tick = 1; tick <= 3; tick++)
            {
                if (tick == 1)
                {
                    host.SubmitHumanMove(human, 30, 30);
                    host.FeedAiMove(1, ai, 20, 20);
                }
                CommandBatch batch = host.StepTick();
                recorder.RecordTick(host.Kernel.CurrentTick.Value, batch, host.Kernel.LastTickResults);
            }

            ulong endHash = host.Kernel.CalculateStateHash();
            return new LiveMatch
            {
                ReplayBytes = recorder.Finalize(endHash),
                Fingerprint = fingerprint,
                InitialSnapshotBytes = snapshot,
                EndStateHash = endHash,
                HumanUnits = human,
                AiUnits = ai,
            };
        }

        /// <summary>A fresh host for playback of the standard scenario.</summary>
        internal static TestHost CreatePlaybackHost(ulong seed = Seed)
        {
            return TestHost.Create(seed);
        }

        /// <summary>
        /// Byte offset of the InitialStateHash field inside the canonical
        /// fingerprint serialization (layout documented on MatchFingerprint).
        /// </summary>
        internal static int InitialStateHashOffsetInFingerprint(MatchFingerprint fingerprint)
        {
            int offset = 5 * 2; // schema versions
            offset += 4 + fingerprint.NumericModelId.Length;
            offset += 2; // ticks per second
            offset += 4 + fingerprint.PrngId.Length;
            offset += 3 * 8; // content hashes
            offset += CommandLimits.ReservedPlayerSlots;
            offset += 8; // start seed
            return offset;
        }

        /// <summary>Builds a raw canonical record byte array field by field (little-endian).</summary>
        internal static byte[] CraftRecord(
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

        internal static byte[] PayloadBytes(ICommandPayload payload)
        {
            var writer = new CommandPayloadWriter();
            payload.WriteTo(writer);
            return writer.ToArray();
        }

        internal static uint EntityId(int index, int generation)
        {
            return unchecked((uint)((generation << 10) | index));
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
