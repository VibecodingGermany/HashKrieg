using Nova.Simulation.CommandsV1;
using NUnit.Framework;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Commands.md section 6, case 9: snapshot/restore with pending commands.
    /// The authoritative dedupe/sequence state (including pending records) and
    /// the pending session actions serialize deterministically; a restored
    /// ingress continues with identical acceptance, dedupe and sealing
    /// behaviour (SimulationCore.md section 3).
    /// </summary>
    [TestFixture]
    public sealed class CommandSnapshotTests
    {
        private static CommandIngress BuildIngressWithPendingState()
        {
            var ingress = CommandTestUtil.CreateIngress();
            Assert.That(
                ingress.TrySubmitIntent(
                    CommandIntent.Create(new StopPayload(new[] { CommandTestUtil.EntityId(1, 1) })), out _),
                Is.EqualTo(CommandIngressResult.Accepted));
            Assert.That(
                ingress.TrySubmitIntent(
                    CommandIntent.Create(new MovePayload(
                        new[] { CommandTestUtil.EntityId(2, 1) },
                        Nova.Core.SimFixed.FromInt(3), Nova.Core.SimFixed.FromInt(4))), out _),
                Is.EqualTo(CommandIngressResult.Accepted));
            Assert.That(
                ingress.TrySubmitIntent(CommandIntent.ForSessionAction(CommandKind.SaveRequest), out _),
                Is.EqualTo(CommandIngressResult.Accepted));
            return ingress;
        }

        [Test]
        public void SnapshotRestore_PendingCommands_ContinueIdentically()
        {
            CommandIngress original = BuildIngressWithPendingState();
            byte[] stateBytes = original.SerializeState();

            var restored = CommandTestUtil.CreateIngress();
            Assert.That(restored.TryRestoreState(stateBytes), Is.True);

            // Sequence assignment continues without reuse.
            Assert.That(
                restored.DedupeState.NextLocalSequence(0),
                Is.EqualTo(original.DedupeState.NextLocalSequence(0)));

            // Pending records survive: both seal the byte-identical batch.
            CommandBatch originalBatch = original.SealTickBatch(1);
            CommandBatch restoredBatch = restored.SealTickBatch(1);
            Assert.That(restoredBatch.Count, Is.EqualTo(2));
            Assert.That(restoredBatch.Serialize(), Is.EqualTo(originalBatch.Serialize()));

            // Session actions survive in submission order.
            SessionActionRequest[] actions = restored.TakePendingSessionActions();
            Assert.That(actions.Length, Is.EqualTo(1));
            Assert.That(actions[0].Kind, Is.EqualTo(CommandKind.SaveRequest));
        }

        [Test]
        public void SnapshotRestore_DedupeState_KeepsAcceptanceBehaviour()
        {
            CommandIngress original = BuildIngressWithPendingState();

            // Capture the pending record bytes for a re-delivery attack.
            original.DedupeState.TryGetPending(0, 1, out CommandRecord pending);
            byte[] redelivery = pending.Serialize();

            byte[] stateBytes = original.SerializeState();
            var restored = CommandTestUtil.CreateIngress();
            Assert.That(restored.TryRestoreState(stateBytes), Is.True);

            // Byte-identical re-delivery is still idempotent after restore.
            Assert.That(
                restored.TryAcceptRecordBytes(redelivery, out _),
                Is.EqualTo(CommandIngressResult.DuplicateIgnored));

            // A conflicting same-key record is still a deterministic conflict.
            byte[] conflict = CommandTestUtil.CraftRecord(
                0, 1, 0, 1, (ushort)CommandKind.Stop, 1,
                CommandTestUtil.PayloadBytes(new StopPayload(new[] { CommandTestUtil.EntityId(5, 1) })));
            Assert.That(
                restored.TryAcceptRecordBytes(conflict, out CommandRejectReason reason),
                Is.EqualTo(CommandIngressResult.Rejected));
            Assert.That(reason, Is.EqualTo(CommandRejectReason.DedupeConflict));

            // Completed sequences still cannot re-enter after restore: seal the
            // pending records, snapshot again, restore again, re-deliver.
            restored.SealTickBatch(1);
            var restoredAgain = CommandTestUtil.CreateIngress();
            Assert.That(restoredAgain.TryRestoreState(restored.SerializeState()), Is.True);
            Assert.That(
                restoredAgain.TryAcceptRecordBytes(redelivery, out _),
                Is.EqualTo(CommandIngressResult.DuplicateIgnored));
        }

        [Test]
        public void Snapshot_SerializationIsDeterministic_AndParserIsStrict()
        {
            CommandIngress first = BuildIngressWithPendingState();
            CommandIngress second = BuildIngressWithPendingState();
            Assert.That(second.SerializeState(), Is.EqualTo(first.SerializeState()),
                "same history => same state bytes");

            // Restore -> serialize roundtrips byte-identically.
            var restored = CommandTestUtil.CreateIngress();
            byte[] stateBytes = first.SerializeState();
            Assert.That(restored.TryRestoreState(stateBytes), Is.True);
            Assert.That(restored.SerializeState(), Is.EqualTo(stateBytes));

            // Truncated and trailing-garbage states are rejected without mutation.
            var truncated = new byte[stateBytes.Length - 1];
            System.Array.Copy(stateBytes, truncated, truncated.Length);
            var victim = CommandTestUtil.CreateIngress();
            Assert.That(victim.TryRestoreState(truncated), Is.False);
            var padded = new byte[stateBytes.Length + 1];
            System.Array.Copy(stateBytes, padded, stateBytes.Length);
            Assert.That(victim.TryRestoreState(padded), Is.False);
            Assert.That(victim.TryRestoreState(new byte[] { 99, 0, 0 }), Is.False);
        }

        /// <summary>
        /// Crafts dedupe-state bytes (format of CommandDedupeState.Serialize)
        /// with the given pending record bytes in exactly one slot's block;
        /// next=1, watermark=0 everywhere.
        /// </summary>
        private static byte[] CraftDedupeState(int pendingSlot, params byte[][] pendingRecords)
        {
            var bytes = new System.Collections.Generic.List<byte> { CommandDedupeState.StateVersion };
            for (int slot = 0; slot < CommandLimits.ReservedPlayerSlots; slot++)
            {
                byte[][] pending = slot == pendingSlot ? pendingRecords : null;
                bytes.AddRange(System.BitConverter.GetBytes(1u));
                bytes.AddRange(System.BitConverter.GetBytes(0u));
                bytes.AddRange(System.BitConverter.GetBytes((ushort)(pending?.Length ?? 0)));
                if (pending != null)
                {
                    foreach (byte[] recordBytes in pending)
                    {
                        bytes.AddRange(System.BitConverter.GetBytes((ushort)recordBytes.Length));
                        bytes.AddRange(recordBytes);
                    }
                }
            }
            return bytes.ToArray();
        }

        private static byte[] WrapIngressState(byte[] dedupeBytes)
        {
            var bytes = new System.Collections.Generic.List<byte> { 1 };
            bytes.AddRange(System.BitConverter.GetBytes((uint)dedupeBytes.Length));
            bytes.AddRange(dedupeBytes);
            bytes.AddRange(System.BitConverter.GetBytes((ushort)0)); // no session actions
            return bytes.ToArray();
        }

        private static byte[] StopRecord(byte slot, uint sequence)
        {
            return CommandTestUtil.CraftRecord(
                0, 1, slot, sequence, (ushort)CommandKind.Stop, 1,
                CommandTestUtil.PayloadBytes(new StopPayload(new[] { CommandTestUtil.EntityId(1, 1) })));
        }

        [Test]
        public void Restore_RejectsDuplicatePendingKey_WithoutThrowingOrMutating()
        {
            // Manipulated snapshot: the same (slot, sequence) pending key twice.
            byte[] stateBytes = CraftDedupeState(0, StopRecord(0, 1), StopRecord(0, 1));
            Assert.That(CommandDedupeState.TryDeserialize(stateBytes, out CommandDedupeState state), Is.False);
            Assert.That(state, Is.Null, "no partial state escapes on failure");

            var ingress = CommandTestUtil.CreateIngress();
            Assert.That(ingress.TryRestoreState(WrapIngressState(stateBytes)), Is.False);
            Assert.That(ingress.PendingCount, Is.EqualTo(0), "failed restore must not mutate");
        }

        [Test]
        public void Restore_RevalidatesPendingRecordContent()
        {
            // Session action as pending stream record.
            byte[] sessionKind = CommandTestUtil.CraftRecord(
                0, 1, 0, 1, (ushort)CommandKind.PauseRequest, 1, System.Array.Empty<byte>());
            Assert.That(
                CommandDedupeState.TryDeserialize(CraftDedupeState(0, sessionKind), out _),
                Is.False);

            // Sequence 0 in a pending record.
            Assert.That(
                CommandDedupeState.TryDeserialize(CraftDedupeState(0, StopRecord(0, 0)), out _),
                Is.False);

            // Record stored in a slot block it does not belong to.
            Assert.That(
                CommandDedupeState.TryDeserialize(CraftDedupeState(1, StopRecord(0, 1)), out _),
                Is.False);

            // Pending record for a slot the restoring session does not run:
            // structurally valid, but rejected by the ingress session binding.
            byte[] foreignSlot = CraftDedupeState(5, StopRecord(5, 1));
            Assert.That(CommandDedupeState.TryDeserialize(foreignSlot, out _), Is.True);
            var ingress = CommandTestUtil.CreateIngress();
            Assert.That(ingress.TryRestoreState(WrapIngressState(foreignSlot)), Is.False);
            Assert.That(ingress.PendingCount, Is.EqualTo(0));

            // A valid snapshot still restores cleanly.
            CommandIngress original = BuildIngressWithPendingState();
            var ok = CommandTestUtil.CreateIngress();
            Assert.That(ok.TryRestoreState(original.SerializeState()), Is.True);
            Assert.That(ok.PendingCount, Is.EqualTo(2));
        }
    }
}
