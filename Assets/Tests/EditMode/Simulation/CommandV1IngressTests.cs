using System;
using NUnit.Framework;
using Nova.Simulation.CommandsV1;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Commands.md section 6, cases 4-7 (EditMode lane). Mirror of the .NET
    /// lane CommandIngressTests with Unity Test Framework asserts.
    /// </summary>
    [TestFixture]
    public class CommandV1IngressTests
    {
        private static byte[] StopRecord(byte slot, uint sequence, uint enqueueTick = 0, uint targetTick = 1)
        {
            return CommandV1TestUtil.CraftRecord(
                enqueueTick, targetTick, slot, sequence, (ushort)CommandKind.Stop, 1,
                CommandV1TestUtil.PayloadBytes(new StopPayload(new[] { CommandV1TestUtil.EntityId(1, 1) })));
        }

        [Test]
        public void TrustBoundary_IntentCarriesNoSlotSequenceOrTargetTick()
        {
            var ingress = CommandV1TestUtil.CreateIngress(localSlot: 1);
            var intent = CommandIntent.Create(new StopPayload(new[] { CommandV1TestUtil.EntityId(1, 1) }));
            Assert.AreEqual(CommandIngressResult.Accepted, ingress.TrySubmitIntent(intent, out _));

            CommandBatch batch = ingress.SealTickBatch(1);
            Assert.AreEqual(1, batch.Count);
            Assert.AreEqual((byte)1, batch.Records[0].PlayerSlot, "session-bound slot");
            Assert.AreEqual(1u, batch.Records[0].Sequence, "sequences start at 1");
            Assert.AreEqual(1u, batch.Records[0].TargetTick, "enqueue + InputDelayTicks");
        }

        [Test]
        public void ReorderedInput_SealsIntoIdenticalSortedBatch()
        {
            byte[] a = StopRecord(slot: 0, sequence: 1);
            byte[] b = StopRecord(slot: 0, sequence: 2);
            byte[] c = StopRecord(slot: 1, sequence: 1);
            byte[] d = StopRecord(slot: 1, sequence: 2);

            var inOrder = CommandV1TestUtil.CreateIngress();
            foreach (byte[] record in new[] { a, b, c, d })
            {
                Assert.AreEqual(CommandIngressResult.Accepted, inOrder.TryAcceptRecordBytes(record, out _));
            }

            var scrambled = CommandV1TestUtil.CreateIngress();
            foreach (byte[] record in new[] { d, a, c, b })
            {
                Assert.AreEqual(CommandIngressResult.Accepted, scrambled.TryAcceptRecordBytes(record, out _));
            }

            CommandBatch first = inOrder.SealTickBatch(1);
            CommandBatch second = scrambled.SealTickBatch(1);
            Assert.AreEqual(first.Serialize(), second.Serialize());

            Assert.AreEqual(4, first.Count);
            Assert.AreEqual((byte)0, first.Records[0].PlayerSlot);
            Assert.AreEqual(1u, first.Records[0].Sequence);
            Assert.AreEqual(2u, first.Records[1].Sequence);
            Assert.AreEqual((byte)1, first.Records[2].PlayerSlot);
            Assert.AreEqual(2u, first.Records[3].Sequence);
        }

        [Test]
        public void ByteIdenticalDuplicate_IsAcceptedExactlyOnce()
        {
            var ingress = CommandV1TestUtil.CreateIngress();
            byte[] record = StopRecord(slot: 0, sequence: 1);
            Assert.AreEqual(CommandIngressResult.Accepted, ingress.TryAcceptRecordBytes(record, out _));
            Assert.AreEqual(CommandIngressResult.DuplicateIgnored, ingress.TryAcceptRecordBytes(record, out _));
            Assert.AreEqual(1, ingress.PendingCount);
        }

        [Test]
        public void ConflictingDuplicate_SameKeyDifferentBytes_IsRejected()
        {
            var ingress = CommandV1TestUtil.CreateIngress();
            Assert.AreEqual(
                CommandIngressResult.Accepted,
                ingress.TryAcceptRecordBytes(StopRecord(slot: 0, sequence: 1), out _));

            byte[] conflict = CommandV1TestUtil.CraftRecord(
                0, 1, 0, 1, (ushort)CommandKind.Stop, 1,
                CommandV1TestUtil.PayloadBytes(new StopPayload(new[] { CommandV1TestUtil.EntityId(2, 1) })));
            Assert.AreEqual(
                CommandIngressResult.Rejected,
                ingress.TryAcceptRecordBytes(conflict, out CommandRejectReason reason));
            Assert.AreEqual(CommandRejectReason.DedupeConflict, reason);
            Assert.AreEqual(1, ingress.PendingCount, "conflict must not replace the original");
        }

        [Test]
        public void CompletedSequence_CannotBypassDedupe_ReplayAttackIsDropped()
        {
            var ingress = CommandV1TestUtil.CreateIngress();
            byte[] record = StopRecord(slot: 0, sequence: 1);
            Assert.AreEqual(CommandIngressResult.Accepted, ingress.TryAcceptRecordBytes(record, out _));
            CommandBatch batch = ingress.SealTickBatch(1);
            Assert.AreEqual(1, batch.Count);

            Assert.AreEqual(
                CommandIngressResult.DuplicateIgnored,
                ingress.TryAcceptRecordBytes(record, out _));
            Assert.AreEqual(0, ingress.PendingCount);
            Assert.AreEqual(0, ingress.SealTickBatch(1).Count, "no re-application");
        }

        [Test]
        public void SequenceZero_IsAStructuralError()
        {
            var ingress = CommandV1TestUtil.CreateIngress();
            Assert.AreEqual(
                CommandIngressResult.Rejected,
                ingress.TryAcceptRecordBytes(StopRecord(slot: 0, sequence: 0), out CommandRejectReason reason));
            Assert.AreEqual(CommandRejectReason.SequenceZero, reason);
        }

        [Test]
        public void SequenceOverflow_SessionIsNotContinuedWithReusedSequence()
        {
            var bytes = new System.Collections.Generic.List<byte> { CommandDedupeState.StateVersion };
            for (int slot = 0; slot < CommandLimits.ReservedPlayerSlots; slot++)
            {
                uint next = slot == 0 ? uint.MaxValue : 1u;
                bytes.AddRange(BitConverter.GetBytes(next));
                bytes.AddRange(BitConverter.GetBytes(0u));
                bytes.AddRange(BitConverter.GetBytes((ushort)0));
            }
            Assert.IsTrue(BitConverter.IsLittleEndian, "test assumes LE craft bytes");
            Assert.IsTrue(CommandDedupeState.TryDeserialize(bytes.ToArray(), out CommandDedupeState state));

            Assert.IsTrue(state.TryAssignLocalSequence(0, out uint last));
            Assert.AreEqual(uint.MaxValue, last);
            Assert.IsFalse(state.TryAssignLocalSequence(0, out _), "overflow: no wrap into reuse");
        }

        [Test]
        public void Backpressure_BatchCapacityPerTick_IsEnforcedBeforeSealing()
        {
            var ingress = CommandV1TestUtil.CreateIngress();
            for (int i = 0; i < CommandLimits.MaxBatchRecordsPerTick; i++)
            {
                CommandIngressResult result = ingress.TrySubmitIntent(
                    CommandIntent.Create(new StopPayload(new[] { CommandV1TestUtil.EntityId(1, 1) })),
                    out CommandRejectReason reason);
                Assert.AreEqual(CommandIngressResult.Accepted, result, $"record {i + 1}: {reason}");
            }
            Assert.AreEqual(
                CommandIngressResult.Rejected,
                ingress.TrySubmitIntent(
                    CommandIntent.Create(new StopPayload(new[] { CommandV1TestUtil.EntityId(1, 1) })),
                    out CommandRejectReason overflow));
            Assert.AreEqual(CommandRejectReason.BatchCapacityExceeded, overflow);

            CommandBatch batch = ingress.SealTickBatch(1);
            Assert.AreEqual(CommandLimits.MaxBatchRecordsPerTick, batch.Count);
        }

        [Test]
        public void Backpressure_PendingQueue_IsEnforcedBeforeSealing()
        {
            var ingress = CommandV1TestUtil.CreateIngress();
            int accepted = 0;
            while (ingress.Session.CurrentTick < 10)
            {
                var intent = CommandIntent.Create(new StopPayload(new[] { CommandV1TestUtil.EntityId(1, 1) }));
                CommandIngressResult result = ingress.TrySubmitIntent(intent, out _);
                if (result != CommandIngressResult.Accepted) break;
                accepted++;
                if (ingress.PendingCount % CommandLimits.MaxBatchRecordsPerTick == 0)
                {
                    ingress.Session.AdvanceTick();
                }
            }
            Assert.AreEqual(CommandLimits.MaxPendingRecords, accepted);
            Assert.AreEqual(
                CommandIngressResult.Rejected,
                ingress.TrySubmitIntent(
                    CommandIntent.Create(new StopPayload(new[] { CommandV1TestUtil.EntityId(1, 1) })),
                    out CommandRejectReason reason));
            Assert.AreEqual(CommandRejectReason.PendingQueueFull, reason);
        }

        [Test]
        public void TickWindow_TargetTickMustMatchEnqueuePlusDelay_AndBeFuture()
        {
            var ingress = CommandV1TestUtil.CreateIngress();
            byte[] payload = CommandV1TestUtil.PayloadBytes(new StopPayload(new[] { CommandV1TestUtil.EntityId(1, 1) }));

            Assert.AreEqual(
                CommandIngressResult.Rejected,
                ingress.TryAcceptRecordBytes(
                    CommandV1TestUtil.CraftRecord(0, 5, 0, 1, (ushort)CommandKind.Stop, 1, payload),
                    out CommandRejectReason wrongDelay));
            Assert.AreEqual(CommandRejectReason.TickWindowViolation, wrongDelay);

            for (int i = 0; i < 3; i++) ingress.Session.AdvanceTick();
            Assert.AreEqual(
                CommandIngressResult.Rejected,
                ingress.TryAcceptRecordBytes(
                    CommandV1TestUtil.CraftRecord(1, 2, 0, 1, (ushort)CommandKind.Stop, 1, payload),
                    out CommandRejectReason past));
            Assert.AreEqual(CommandRejectReason.TickWindowViolation, past);

            Assert.AreEqual(
                CommandIngressResult.Accepted,
                ingress.TryAcceptHistoricalRecordBytes(
                    CommandV1TestUtil.CraftRecord(1, 2, 0, 1, (ushort)CommandKind.Stop, 1, payload),
                    out _));
        }

        [Test]
        public void Accept_RejectsTrailingBytesAfterAValidRecord()
        {
            // The intake accepts exactly one record per call; trailing garbage
            // is a structural framing error.
            var ingress = CommandV1TestUtil.CreateIngress();
            byte[] valid = StopRecord(slot: 0, sequence: 1);
            var withGarbage = new byte[valid.Length + 1];
            Array.Copy(valid, withGarbage, valid.Length);
            withGarbage[valid.Length] = 0xAB;

            Assert.AreEqual(
                CommandIngressResult.Rejected,
                ingress.TryAcceptRecordBytes(withGarbage, out CommandRejectReason reason));
            Assert.AreEqual(CommandRejectReason.TrailingBytes, reason);
            Assert.AreEqual(0, ingress.PendingCount);

            // Same rule on the replay import path.
            Assert.AreEqual(
                CommandIngressResult.Rejected,
                ingress.TryAcceptHistoricalRecordBytes(withGarbage, out CommandRejectReason historicalReason));
            Assert.AreEqual(CommandRejectReason.TrailingBytes, historicalReason);

            // The exact record alone is still accepted.
            Assert.AreEqual(CommandIngressResult.Accepted, ingress.TryAcceptRecordBytes(valid, out _));
        }

        [Test]
        public void SessionActions_AreValidatedQueuedAndNeverSealedAsRecords()
        {
            var ingress = CommandV1TestUtil.CreateIngress();
            foreach (CommandKind kind in new[]
            {
                CommandKind.PauseRequest, CommandKind.UnpauseRequest,
                CommandKind.SaveRequest, CommandKind.LoadRequest,
            })
            {
                Assert.AreEqual(
                    CommandIngressResult.Accepted,
                    ingress.TrySubmitIntent(CommandIntent.ForSessionAction(kind), out _),
                    kind.ToString());
            }
            Assert.AreEqual(0, ingress.PendingCount, "session actions are no stream records");
            Assert.AreEqual(4, ingress.PendingSessionActionCount);

            SessionActionRequest[] actions = ingress.TakePendingSessionActions();
            Assert.AreEqual(4, actions.Length);
            Assert.AreEqual(CommandKind.PauseRequest, actions[0].Kind);
            Assert.AreEqual(0u, actions[0].EnqueueTick);
            Assert.AreEqual(0, ingress.PendingSessionActionCount);

            Assert.Throws<ArgumentException>(() => CommandIntent.ForSessionAction(CommandKind.Move));
        }
    }
}
