using System.Collections.Generic;
using NUnit.Framework;
using Nova.Core;
using Nova.Gameplay;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;

namespace Nova.Gameplay.Tests
{
    /// <summary>
    /// Contract tests for the command-submission layer: canonical chunking of
    /// oversized selections and honest propagation of structural rejections.
    /// The fixture uses the real <see cref="CommandIngress"/> plus the
    /// <see cref="LocalLoopbackTransport"/>, so every assertion is made against
    /// records that actually passed the ingress trust boundary.
    /// </summary>
    [TestFixture]
    public class RtsIntentDispatcherTests
    {
        private sealed class Fixture
        {
            public readonly MatchSession Session;
            public readonly CommandIngress Ingress;
            public readonly LocalLoopbackTransport Transport;
            public readonly RtsIntentDispatcher Dispatcher;

            public Fixture()
            {
                Session = new MatchSession(localSlot: 0, activeSlots: new byte[] { 0, 1 }, inputDelayTicks: 1);
                Ingress = new CommandIngress(Session);
                Transport = new LocalLoopbackTransport(Ingress);
                Dispatcher = new RtsIntentDispatcher(Ingress);
            }

            /// <summary>Seals the batch the freshly submitted intents target (tick + input delay).</summary>
            public CommandBatch SealDueBatch()
            {
                return Ingress.SealTickBatch(Session.CurrentTick + Session.InputDelayTicks);
            }
        }

        /// <summary>Descending order on purpose: normalization must sort, not trust the caller.</summary>
        private static EntityId[] DescendingSelection(int count)
        {
            var selection = new EntityId[count];
            for (int i = 0; i < count; i++)
            {
                selection[i] = new EntityId(count - 1 - i, 1);
            }
            return selection;
        }

        private static uint[] ReadMoveEntityIds(CommandRecord record)
        {
            Assert.AreEqual(CommandKind.Move, record.Kind);
            var reader = new CommandPayloadReader(record.Payload.Span);
            Assert.IsTrue(MovePayload.TryParse(ref reader, out MovePayload move), "sealed Move payload must parse");
            return move.EntityIds;
        }

        [TestCase(1, FactionId.Alliance, 1)]
        [TestCase(17, FactionId.Alliance, 17)]
        [TestCase(1, FactionId.Legion, 18)]
        [TestCase(5, FactionId.Legion, 22)]
        [TestCase(17, FactionId.Legion, 34)]
        public void CanonicalHotkeyDefinition_MapsToTheLocalFactionRole(
            int canonicalId, FactionId faction, int expected)
        {
            Assert.That(RtsIntentDispatcher.TryResolveCanonicalDefinitionId(
                (ushort)canonicalId, faction, out ushort actual), Is.True);
            Assert.That(actual, Is.EqualTo((ushort)expected));
        }

        [Test]
        public void CanonicalHotkeyDefinition_RejectsInvalidOrAlreadyLegionIds()
        {
            Assert.That(RtsIntentDispatcher.TryResolveCanonicalDefinitionId(
                0, FactionId.Legion, out ushort invalid), Is.False);
            Assert.That(invalid, Is.Zero);
            Assert.That(RtsIntentDispatcher.TryResolveCanonicalDefinitionId(
                SimDefinitions.ToDefinitionId(FactionId.Legion, UnitRole.Builder),
                FactionId.Legion, out ushort alreadyMapped), Is.False);
            Assert.That(alreadyMapped, Is.Zero);
        }

        [Test]
        public void MoveTo_SelectionAboveChunkLimit_SplitsIntoChunksCoveringEveryIdExactlyOnce()
        {
            const int selectionSize = 300;
            var fixture = new Fixture();

            IntentDispatchResult result = fixture.Dispatcher.MoveTo(
                DescendingSelection(selectionSize), SimFixed.FromInt(42), SimFixed.FromInt(7));

            Assert.IsTrue(result.Accepted, "300 units must not be one silently rejected command");
            Assert.AreEqual(3, result.CommandCount);
            Assert.AreEqual(selectionSize, result.EntityIdCount);

            CommandBatch batch = fixture.SealDueBatch();
            Assert.AreEqual(3, batch.Count);

            var seen = new List<uint>(selectionSize);
            for (int i = 0; i < batch.Count; i++)
            {
                uint[] chunk = ReadMoveEntityIds(batch.Records[i]);
                Assert.AreEqual(RtsIntentDispatcher.MaxEntityIdsPerCommand, chunk.Length);
                Assert.IsTrue(CommandIds.IsCanonicalEntityList(chunk), "each chunk must be canonical");
                seen.AddRange(chunk);
            }

            // Every id exactly once, as one globally ascending run across chunks.
            var expected = new List<uint>(selectionSize);
            for (int i = 0; i < selectionSize; i++)
            {
                expected.Add(UnitCommandStateView.ToRawEntityId(new EntityId(i, 1)));
            }
            CollectionAssert.AreEqual(expected, seen);
            CollectionAssert.AllItemsAreUnique(seen);
        }

        [Test]
        public void MoveTo_AtAndJustAboveChunkLimit_ProducesExactChunkSizes()
        {
            const int limit = CommandLimits.MaxEntityIdsPerCommand;
            Assert.AreEqual(100, limit, "chunking is pinned to the canonical contract value");

            var atLimit = new Fixture();
            IntentDispatchResult exact = atLimit.Dispatcher.MoveTo(
                DescendingSelection(limit), SimFixed.FromInt(1), SimFixed.FromInt(1));
            Assert.IsTrue(exact.Accepted);
            Assert.AreEqual(1, exact.CommandCount);
            CommandBatch exactBatch = atLimit.SealDueBatch();
            Assert.AreEqual(1, exactBatch.Count);
            Assert.AreEqual(limit, ReadMoveEntityIds(exactBatch.Records[0]).Length);

            var over = new Fixture();
            IntentDispatchResult split = over.Dispatcher.MoveTo(
                DescendingSelection(limit + 1), SimFixed.FromInt(1), SimFixed.FromInt(1));
            Assert.IsTrue(split.Accepted);
            Assert.AreEqual(2, split.CommandCount);
            Assert.AreEqual(limit + 1, split.EntityIdCount);
            CommandBatch overBatch = over.SealDueBatch();
            Assert.AreEqual(2, overBatch.Count);
            Assert.AreEqual(limit, ReadMoveEntityIds(overBatch.Records[0]).Length);
            Assert.AreEqual(1, ReadMoveEntityIds(overBatch.Records[1]).Length);
        }

        [Test]
        public void MoveTo_DuplicateAndStaleHandles_AreNormalizedToACanonicalList()
        {
            var fixture = new Fixture();

            var selection = new[]
            {
                new EntityId(5, 1),
                EntityId.Invalid,
                new EntityId(2, 1),
                new EntityId(5, 1),
                new EntityId(9, 1),
            };

            IntentDispatchResult result = fixture.Dispatcher.MoveTo(
                selection, SimFixed.FromInt(3), SimFixed.FromInt(4));

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(1, result.CommandCount);
            Assert.AreEqual(3, result.EntityIdCount);

            CommandBatch batch = fixture.SealDueBatch();
            uint[] ids = ReadMoveEntityIds(batch.Records[0]);
            CollectionAssert.AreEqual(
                new[]
                {
                    UnitCommandStateView.ToRawEntityId(new EntityId(2, 1)),
                    UnitCommandStateView.ToRawEntityId(new EntityId(5, 1)),
                    UnitCommandStateView.ToRawEntityId(new EntityId(9, 1)),
                },
                ids);
        }

        [Test]
        public void QueueUnit_RejectedByIngress_SurfacesTheStructuralRejectReason()
        {
            var fixture = new Fixture();

            // Count 0 is a structural error of the QueueUnit payload; the
            // dispatcher must report the ingress reason instead of swallowing it.
            IntentDispatchResult result = fixture.Dispatcher.QueueUnit(
                new EntityId(3, 1), unitDefId: 1, count: 0);

            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(CommandIngressResult.Rejected, result.Result);
            Assert.AreEqual(CommandRejectReason.InvalidCount, result.RejectReason);
            Assert.AreEqual(0, result.CommandCount);
            Assert.AreEqual(0, fixture.Ingress.PendingCount, "a rejected command never enters the pending stream");
        }

        [Test]
        public void PlaceBuilding_InvalidDefinitionId_SurfacesTheStructuralRejectReason()
        {
            var fixture = new Fixture();

            IntentDispatchResult result = fixture.Dispatcher.PlaceBuilding(
                buildingDefId: 0, gridX: 8, gridY: 4);

            Assert.AreEqual(CommandIngressResult.Rejected, result.Result);
            Assert.AreEqual(CommandRejectReason.InvalidDefinitionId, result.RejectReason);
            Assert.AreEqual(0, fixture.Ingress.PendingCount);
        }

        [Test]
        public void MoveTo_EmptySelection_ReportsEmptyEntityListWithoutTouchingTheIngress()
        {
            var fixture = new Fixture();

            IntentDispatchResult result = fixture.Dispatcher.MoveTo(
                new EntityId[0], SimFixed.FromInt(1), SimFixed.FromInt(1));

            Assert.AreEqual(CommandIngressResult.Rejected, result.Result);
            Assert.AreEqual(CommandRejectReason.EmptyEntityList, result.RejectReason);
            Assert.AreEqual(0, result.CommandCount);
            Assert.AreEqual(0, fixture.Ingress.PendingCount);
        }
    }
}
