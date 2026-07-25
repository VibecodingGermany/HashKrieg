using System;
using Nova.Simulation.CommandsV1;
using NUnit.Framework;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Commands.md section 6, cases 4-7 plus trust-boundary behaviour of the
    /// ingress: reordered input seals into the identical sorted batch,
    /// byte-identical and conflicting duplicates, sequence overflow and replay
    /// attacks, queue/batch backpressure, tick window and session actions.
    /// </summary>
    [TestFixture]
    public sealed class CommandIngressTests
    {
        private static byte[] StopRecord(byte slot, uint sequence, uint enqueueTick = 0, uint targetTick = 1)
        {
            return CommandTestUtil.CraftRecord(
                enqueueTick, targetTick, slot, sequence, (ushort)CommandKind.Stop, 1,
                CommandTestUtil.PayloadBytes(new StopPayload(new[] { CommandTestUtil.EntityId(1, 1) })));
        }

        [Test]
        public void TrustBoundary_IntentCarriesNoSlotSequenceOrTargetTick()
        {
            // Compile-time contract: CommandIntent exposes only kind, version and
            // payload bytes; CommandRecord's constructor is internal. Here the
            // runtime side: the ingress assigns all three authoritatively.
            var ingress = CommandTestUtil.CreateIngress(localSlot: 1);
            var intent = CommandIntent.Create(new StopPayload(new[] { CommandTestUtil.EntityId(1, 1) }));
            Assert.That(ingress.TrySubmitIntent(intent, out _), Is.EqualTo(CommandIngressResult.Accepted));

            CommandBatch batch = ingress.SealTickBatch(1);
            Assert.That(batch.Count, Is.EqualTo(1));
            Assert.That(batch.Records[0].PlayerSlot, Is.EqualTo((byte)1), "session-bound slot");
            Assert.That(batch.Records[0].Sequence, Is.EqualTo(1u), "sequences start at 1");
            Assert.That(batch.Records[0].TargetTick, Is.EqualTo(1u), "enqueue + InputDelayTicks");
        }

        [Test]
        public void ReorderedInput_SealsIntoIdenticalSortedBatch()
        {
            // Case 4: the same records accepted in scrambled order produce the
            // byte-identical sealed batch, sorted by (TargetTick, PlayerSlot, Sequence).
            byte[] a = StopRecord(slot: 0, sequence: 1);
            byte[] b = StopRecord(slot: 0, sequence: 2);
            byte[] c = StopRecord(slot: 1, sequence: 1);
            byte[] d = StopRecord(slot: 1, sequence: 2);

            var inOrder = CommandTestUtil.CreateIngress();
            foreach (byte[] record in new[] { a, b, c, d })
            {
                Assert.That(inOrder.TryAcceptRecordBytes(record, out _), Is.EqualTo(CommandIngressResult.Accepted));
            }

            var scrambled = CommandTestUtil.CreateIngress();
            foreach (byte[] record in new[] { d, a, c, b })
            {
                Assert.That(scrambled.TryAcceptRecordBytes(record, out _), Is.EqualTo(CommandIngressResult.Accepted));
            }

            CommandBatch first = inOrder.SealTickBatch(1);
            CommandBatch second = scrambled.SealTickBatch(1);
            Assert.That(second.Serialize(), Is.EqualTo(first.Serialize()));

            Assert.That(first.Count, Is.EqualTo(4));
            Assert.That(first.Records[0].PlayerSlot, Is.EqualTo((byte)0));
            Assert.That(first.Records[0].Sequence, Is.EqualTo(1u));
            Assert.That(first.Records[1].Sequence, Is.EqualTo(2u));
            Assert.That(first.Records[2].PlayerSlot, Is.EqualTo((byte)1));
            Assert.That(first.Records[3].Sequence, Is.EqualTo(2u));
        }

        [Test]
        public void ByteIdenticalDuplicate_IsAcceptedExactlyOnce()
        {
            // Case 5a: byte-identical re-delivery is idempotent while pending.
            var ingress = CommandTestUtil.CreateIngress();
            byte[] record = StopRecord(slot: 0, sequence: 1);
            Assert.That(ingress.TryAcceptRecordBytes(record, out _), Is.EqualTo(CommandIngressResult.Accepted));
            Assert.That(ingress.TryAcceptRecordBytes(record, out _), Is.EqualTo(CommandIngressResult.DuplicateIgnored));
            Assert.That(ingress.PendingCount, Is.EqualTo(1));
        }

        [Test]
        public void ConflictingDuplicate_SameKeyDifferentBytes_IsRejected()
        {
            // Case 5b: same (PlayerSlot, Sequence), different payload content.
            var ingress = CommandTestUtil.CreateIngress();
            Assert.That(
                ingress.TryAcceptRecordBytes(StopRecord(slot: 0, sequence: 1), out _),
                Is.EqualTo(CommandIngressResult.Accepted));

            byte[] conflict = CommandTestUtil.CraftRecord(
                0, 1, 0, 1, (ushort)CommandKind.Stop, 1,
                CommandTestUtil.PayloadBytes(new StopPayload(new[] { CommandTestUtil.EntityId(2, 1) })));
            Assert.That(
                ingress.TryAcceptRecordBytes(conflict, out CommandRejectReason reason),
                Is.EqualTo(CommandIngressResult.Rejected));
            Assert.That(reason, Is.EqualTo(CommandRejectReason.DedupeConflict));
            Assert.That(ingress.PendingCount, Is.EqualTo(1), "conflict must not replace the original");
        }

        [Test]
        public void CompletedSequence_CannotBypassDedupe_ReplayAttackIsDropped()
        {
            // Case 6a: a record re-delivered after its sequence was sealed is a
            // completed duplicate and is never re-applied.
            var ingress = CommandTestUtil.CreateIngress();
            byte[] record = StopRecord(slot: 0, sequence: 1);
            Assert.That(ingress.TryAcceptRecordBytes(record, out _), Is.EqualTo(CommandIngressResult.Accepted));
            CommandBatch batch = ingress.SealTickBatch(1);
            Assert.That(batch.Count, Is.EqualTo(1));

            Assert.That(
                ingress.TryAcceptRecordBytes(record, out _),
                Is.EqualTo(CommandIngressResult.DuplicateIgnored));
            Assert.That(ingress.PendingCount, Is.EqualTo(0));
            Assert.That(ingress.SealTickBatch(1).Count, Is.EqualTo(0), "no re-application");
        }

        [Test]
        public void SequenceZero_IsAStructuralError()
        {
            // Case 6b: sequences start at 1; 0 never enters the stream.
            var ingress = CommandTestUtil.CreateIngress();
            Assert.That(
                ingress.TryAcceptRecordBytes(StopRecord(slot: 0, sequence: 0), out CommandRejectReason reason),
                Is.EqualTo(CommandIngressResult.Rejected));
            Assert.That(reason, Is.EqualTo(CommandRejectReason.SequenceZero));
        }

        [Test]
        public void SequenceOverflow_SessionIsNotContinuedWithReusedSequence()
        {
            // Case 6c: uint32 overflow of the per-player sequence. Crafted state
            // with nextLocalSequence = uint.MaxValue: the last value is assigned
            // once, then the state refuses further assignment instead of wrapping.
            var bytes = new System.Collections.Generic.List<byte> { CommandDedupeState.StateVersion };
            for (int slot = 0; slot < CommandLimits.ReservedPlayerSlots; slot++)
            {
                uint next = slot == 0 ? uint.MaxValue : 1u;
                bytes.AddRange(BitConverter.GetBytes(next));
                bytes.AddRange(BitConverter.GetBytes(0u));
                bytes.AddRange(BitConverter.GetBytes((ushort)0));
            }
            Assert.That(BitConverter.IsLittleEndian, Is.True, "test assumes LE craft bytes");
            Assert.That(CommandDedupeState.TryDeserialize(bytes.ToArray(), out CommandDedupeState state), Is.True);

            Assert.That(state.TryAssignLocalSequence(0, out uint last), Is.True);
            Assert.That(last, Is.EqualTo(uint.MaxValue));
            Assert.That(state.TryAssignLocalSequence(0, out _), Is.False, "overflow: no wrap into reuse");
        }

        [Test]
        public void Backpressure_BatchCapacityPerTick_IsEnforcedBeforeSealing()
        {
            // Case 7a: the 257th record for one target tick is rejected before sealing.
            var ingress = CommandTestUtil.CreateIngress();
            for (int i = 0; i < CommandLimits.MaxBatchRecordsPerTick; i++)
            {
                CommandIngressResult result = ingress.TrySubmitIntent(
                    CommandIntent.Create(new StopPayload(new[] { CommandTestUtil.EntityId(1, 1) })),
                    out CommandRejectReason reason);
                Assert.That(result, Is.EqualTo(CommandIngressResult.Accepted), $"record {i + 1}: {reason}");
            }
            Assert.That(
                ingress.TrySubmitIntent(
                    CommandIntent.Create(new StopPayload(new[] { CommandTestUtil.EntityId(1, 1) })),
                    out CommandRejectReason overflow),
                Is.EqualTo(CommandIngressResult.Rejected));
            Assert.That(overflow, Is.EqualTo(CommandRejectReason.BatchCapacityExceeded));

            CommandBatch batch = ingress.SealTickBatch(1);
            Assert.That(batch.Count, Is.EqualTo(CommandLimits.MaxBatchRecordsPerTick));
        }

        [Test]
        public void Backpressure_PendingQueue_IsEnforcedBeforeSealing()
        {
            // Case 7b: the 1025th pending record is rejected before sealing.
            var ingress = CommandTestUtil.CreateIngress();
            int accepted = 0;
            while (ingress.Session.CurrentTick < 10)
            {
                var intent = CommandIntent.Create(new StopPayload(new[] { CommandTestUtil.EntityId(1, 1) }));
                CommandIngressResult result = ingress.TrySubmitIntent(intent, out _);
                if (result != CommandIngressResult.Accepted) break;
                accepted++;
                if (ingress.PendingCount % CommandLimits.MaxBatchRecordsPerTick == 0)
                {
                    // Never seal: records stay pending across their target tick.
                    ingress.Session.AdvanceTick();
                }
            }
            Assert.That(accepted, Is.EqualTo(CommandLimits.MaxPendingRecords));
            Assert.That(
                ingress.TrySubmitIntent(
                    CommandIntent.Create(new StopPayload(new[] { CommandTestUtil.EntityId(1, 1) })),
                    out CommandRejectReason reason),
                Is.EqualTo(CommandIngressResult.Rejected));
            Assert.That(reason, Is.EqualTo(CommandRejectReason.PendingQueueFull));
        }

        [Test]
        public void TickWindow_TargetTickMustMatchEnqueuePlusDelay_AndBeFuture()
        {
            var ingress = CommandTestUtil.CreateIngress();
            byte[] payload = CommandTestUtil.PayloadBytes(new StopPayload(new[] { CommandTestUtil.EntityId(1, 1) }));

            // Target tick not equal to enqueue + InputDelayTicks (= 1).
            Assert.That(
                ingress.TryAcceptRecordBytes(
                    CommandTestUtil.CraftRecord(0, 5, 0, 1, (ushort)CommandKind.Stop, 1, payload),
                    out CommandRejectReason wrongDelay),
                Is.EqualTo(CommandIngressResult.Rejected));
            Assert.That(wrongDelay, Is.EqualTo(CommandRejectReason.TickWindowViolation));

            // Target tick in the past (session already at tick 3).
            for (int i = 0; i < 3; i++) ingress.Session.AdvanceTick();
            Assert.That(
                ingress.TryAcceptRecordBytes(
                    CommandTestUtil.CraftRecord(1, 2, 0, 1, (ushort)CommandKind.Stop, 1, payload),
                    out CommandRejectReason past),
                Is.EqualTo(CommandIngressResult.Rejected));
            Assert.That(past, Is.EqualTo(CommandRejectReason.TickWindowViolation));

            // Replay import path: historical ticks accepted only via the
            // fingerprint-checked import entry point.
            Assert.That(
                ingress.TryAcceptHistoricalRecordBytes(
                    CommandTestUtil.CraftRecord(1, 2, 0, 1, (ushort)CommandKind.Stop, 1, payload),
                    out _),
                Is.EqualTo(CommandIngressResult.Accepted));
        }

        [Test]
        public void SessionActions_AreValidatedQueuedAndNeverSealedAsRecords()
        {
            var ingress = CommandTestUtil.CreateIngress();
            foreach (CommandKind kind in new[]
            {
                CommandKind.PauseRequest, CommandKind.UnpauseRequest,
                CommandKind.SaveRequest, CommandKind.LoadRequest,
            })
            {
                Assert.That(
                    ingress.TrySubmitIntent(CommandIntent.ForSessionAction(kind), out _),
                    Is.EqualTo(CommandIngressResult.Accepted), kind.ToString());
            }
            Assert.That(ingress.PendingCount, Is.EqualTo(0), "session actions are no stream records");
            Assert.That(ingress.PendingSessionActionCount, Is.EqualTo(4));

            SessionActionRequest[] actions = ingress.TakePendingSessionActions();
            Assert.That(actions.Length, Is.EqualTo(4));
            Assert.That(actions[0].Kind, Is.EqualTo(CommandKind.PauseRequest));
            Assert.That(actions[0].EnqueueTick, Is.EqualTo(0u));
            Assert.That(ingress.PendingSessionActionCount, Is.EqualTo(0));

            // ForSessionAction refuses stream kinds.
            Assert.Throws<ArgumentException>(() => CommandIntent.ForSessionAction(CommandKind.Move));
        }
    }
}
