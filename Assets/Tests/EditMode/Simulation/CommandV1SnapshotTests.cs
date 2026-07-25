using NUnit.Framework;
using Nova.Simulation.CommandsV1;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Commands.md section 6, case 9 (EditMode lane). Mirror of the .NET lane
    /// CommandSnapshotTests with Unity Test Framework asserts.
    /// </summary>
    [TestFixture]
    public class CommandV1SnapshotTests
    {
        private static CommandIngress BuildIngressWithPendingState()
        {
            var ingress = CommandV1TestUtil.CreateIngress();
            Assert.AreEqual(
                CommandIngressResult.Accepted,
                ingress.TrySubmitIntent(
                    CommandIntent.Create(new StopPayload(new[] { CommandV1TestUtil.EntityId(1, 1) })), out _));
            Assert.AreEqual(
                CommandIngressResult.Accepted,
                ingress.TrySubmitIntent(
                    CommandIntent.Create(new MovePayload(
                        new[] { CommandV1TestUtil.EntityId(2, 1) },
                        Nova.Core.SimFixed.FromInt(3), Nova.Core.SimFixed.FromInt(4))), out _));
            Assert.AreEqual(
                CommandIngressResult.Accepted,
                ingress.TrySubmitIntent(CommandIntent.ForSessionAction(CommandKind.SaveRequest), out _));
            return ingress;
        }

        [Test]
        public void SnapshotRestore_PendingCommands_ContinueIdentically()
        {
            CommandIngress original = BuildIngressWithPendingState();
            byte[] stateBytes = original.SerializeState();

            var restored = CommandV1TestUtil.CreateIngress();
            Assert.IsTrue(restored.TryRestoreState(stateBytes));

            Assert.AreEqual(
                original.DedupeState.NextLocalSequence(0),
                restored.DedupeState.NextLocalSequence(0));

            CommandBatch originalBatch = original.SealTickBatch(1);
            CommandBatch restoredBatch = restored.SealTickBatch(1);
            Assert.AreEqual(2, restoredBatch.Count);
            Assert.AreEqual(originalBatch.Serialize(), restoredBatch.Serialize());

            SessionActionRequest[] actions = restored.TakePendingSessionActions();
            Assert.AreEqual(1, actions.Length);
            Assert.AreEqual(CommandKind.SaveRequest, actions[0].Kind);
        }

        [Test]
        public void SnapshotRestore_DedupeState_KeepsAcceptanceBehaviour()
        {
            CommandIngress original = BuildIngressWithPendingState();

            original.DedupeState.TryGetPending(0, 1, out CommandRecord pending);
            byte[] redelivery = pending.Serialize();

            byte[] stateBytes = original.SerializeState();
            var restored = CommandV1TestUtil.CreateIngress();
            Assert.IsTrue(restored.TryRestoreState(stateBytes));

            Assert.AreEqual(
                CommandIngressResult.DuplicateIgnored,
                restored.TryAcceptRecordBytes(redelivery, out _));

            byte[] conflict = CommandV1TestUtil.CraftRecord(
                0, 1, 0, 1, (ushort)CommandKind.Stop, 1,
                CommandV1TestUtil.PayloadBytes(new StopPayload(new[] { CommandV1TestUtil.EntityId(5, 1) })));
            Assert.AreEqual(
                CommandIngressResult.Rejected,
                restored.TryAcceptRecordBytes(conflict, out CommandRejectReason reason));
            Assert.AreEqual(CommandRejectReason.DedupeConflict, reason);

            restored.SealTickBatch(1);
            var restoredAgain = CommandV1TestUtil.CreateIngress();
            Assert.IsTrue(restoredAgain.TryRestoreState(restored.SerializeState()));
            Assert.AreEqual(
                CommandIngressResult.DuplicateIgnored,
                restoredAgain.TryAcceptRecordBytes(redelivery, out _));
        }

        [Test]
        public void Snapshot_SerializationIsDeterministic_AndParserIsStrict()
        {
            CommandIngress first = BuildIngressWithPendingState();
            CommandIngress second = BuildIngressWithPendingState();
            Assert.AreEqual(first.SerializeState(), second.SerializeState(),
                "same history => same state bytes");

            var restored = CommandV1TestUtil.CreateIngress();
            byte[] stateBytes = first.SerializeState();
            Assert.IsTrue(restored.TryRestoreState(stateBytes));
            Assert.AreEqual(stateBytes, restored.SerializeState());

            var truncated = new byte[stateBytes.Length - 1];
            System.Array.Copy(stateBytes, truncated, truncated.Length);
            var victim = CommandV1TestUtil.CreateIngress();
            Assert.IsFalse(victim.TryRestoreState(truncated));
            var padded = new byte[stateBytes.Length + 1];
            System.Array.Copy(stateBytes, padded, stateBytes.Length);
            Assert.IsFalse(victim.TryRestoreState(padded));
            Assert.IsFalse(victim.TryRestoreState(new byte[] { 99, 0, 0 }));
        }
    }
}
