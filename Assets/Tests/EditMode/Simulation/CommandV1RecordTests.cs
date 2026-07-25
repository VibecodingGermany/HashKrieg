using System;
using NUnit.Framework;
using Nova.Simulation.CommandsV1;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Commands.md section 6, cases 1 and 3 (EditMode lane). Mirror of the
    /// .NET lane CommandRecordTests with Unity Test Framework asserts.
    /// </summary>
    [TestFixture]
    public class CommandV1RecordTests
    {
        private static readonly uint[] ThreeEntities =
        {
            CommandV1TestUtil.EntityId(1, 1),
            CommandV1TestUtil.EntityId(2, 1),
            CommandV1TestUtil.EntityId(3, 2),
        };

        private static CommandIntent[] AllStreamIntents()
        {
            return new[]
            {
                CommandIntent.Create(new MovePayload(ThreeEntities, Nova.Core.SimFixed.FromInt(10), Nova.Core.SimFixed.FromInt(-3))),
                CommandIntent.Create(new StopPayload(ThreeEntities)),
                CommandIntent.Create(new AttackTargetPayload(ThreeEntities, CommandV1TestUtil.EntityId(9, 1))),
                CommandIntent.Create(new HarvestPayload(ThreeEntities, 7)),
                CommandIntent.Create(new ReturnCargoPayload(ThreeEntities)),
                CommandIntent.Create(new PlaceBuildingPayload(4, 12, 30)),
                CommandIntent.Create(new CancelConstructionPayload(CommandV1TestUtil.EntityId(5, 1))),
                CommandIntent.Create(new RepairPayload(ThreeEntities, CommandV1TestUtil.EntityId(6, 1))),
                CommandIntent.Create(new SellPayload(CommandV1TestUtil.EntityId(7, 1))),
                CommandIntent.Create(new QueueUnitPayload(CommandV1TestUtil.EntityId(8, 1), 3, 5)),
                CommandIntent.Create(new CancelProductionPayload(CommandV1TestUtil.EntityId(8, 1), 2)),
                CommandIntent.Create(new SetRallyPointPayload(CommandV1TestUtil.EntityId(8, 1), Nova.Core.SimFixed.FromInt(1), Nova.Core.SimFixed.FromInt(2))),
                CommandIntent.Create(new InstallDefenseModulePayload(CommandV1TestUtil.EntityId(8, 1), 9)),
            };
        }

        [Test]
        public void Register_ContainsExactlyTheActivatedV1Inventory()
        {
            Assert.AreEqual(17, Enum.GetValues(typeof(CommandKind)).Length);
            Assert.AreEqual(1, (ushort)CommandKind.Move);
            Assert.AreEqual(13, (ushort)CommandKind.InstallDefenseModule);
            Assert.AreEqual(14, (ushort)CommandKind.PauseRequest);
            Assert.AreEqual(17, (ushort)CommandKind.LoadRequest);
            Assert.IsFalse(CommandKindInfo.IsKnown((CommandKind)0));
            Assert.IsFalse(CommandKindInfo.IsKnown((CommandKind)18));
            for (CommandKind kind = CommandKind.Move; kind <= CommandKind.InstallDefenseModule; kind++)
            {
                Assert.IsTrue(CommandKindInfo.IsStreamKind(kind), kind.ToString());
                Assert.IsFalse(CommandKindInfo.IsSessionAction(kind), kind.ToString());
            }
            for (CommandKind kind = CommandKind.PauseRequest; kind <= CommandKind.LoadRequest; kind++)
            {
                Assert.IsTrue(CommandKindInfo.IsSessionAction(kind), kind.ToString());
                Assert.IsFalse(CommandKindInfo.IsStreamKind(kind), kind.ToString());
            }
        }

        [Test]
        public void Roundtrip_EveryActivatedStreamKind_RecordAndPayload()
        {
            CommandIntent[] intents = AllStreamIntents();
            Assert.AreEqual(13, intents.Length, "100 % of the activated stream inventory");

            var ingress = CommandV1TestUtil.CreateIngress();
            foreach (CommandIntent intent in intents)
            {
                Assert.AreEqual(
                    CommandIngressResult.Accepted,
                    ingress.TrySubmitIntent(intent, out CommandRejectReason reason),
                    $"{intent.Kind}: {reason}");
            }

            CommandBatch batch = ingress.SealTickBatch(1);
            Assert.AreEqual(13, batch.Count);

            foreach (CommandRecord record in batch.Records)
            {
                byte[] bytes = record.Serialize();
                Assert.IsTrue(
                    CommandRecord.TryDeserialize(bytes, out CommandRecord parsed, out int consumed),
                    record.Kind.ToString());
                Assert.AreEqual(bytes.Length, consumed);
                Assert.AreEqual(record, parsed);
                Assert.AreEqual(bytes, parsed.Serialize());
                Assert.IsTrue(
                    CommandPayloadValidation.TryExtractRefs(record.Kind, record.Payload.Span, out _),
                    record.Kind.ToString());
            }

            for (int i = 0; i < batch.Count; i++)
            {
                Assert.AreEqual((CommandKind)(i + 1), batch.Records[i].Kind);
                Assert.AreEqual((uint)(i + 1), batch.Records[i].Sequence);
                Assert.AreEqual((byte)0, batch.Records[i].PlayerSlot);
                Assert.AreEqual(0u, batch.Records[i].EnqueueTick);
                Assert.AreEqual(1u, batch.Records[i].TargetTick);
                Assert.AreEqual(CommandLimits.PayloadVersionV1, batch.Records[i].PayloadVersion);
            }
        }

        [Test]
        public void Parse_RejectsInvalidLengths_BeforeAllocation()
        {
            byte[] valid = CommandV1TestUtil.CraftRecord(
                0, 1, 0, 1, (ushort)CommandKind.Stop, 1,
                CommandV1TestUtil.PayloadBytes(new StopPayload(new[] { CommandV1TestUtil.EntityId(1, 1) })));

            byte[] tooSmall = (byte[])valid.Clone();
            tooSmall[0] = 10;
            tooSmall[1] = 0;
            Assert.IsFalse(CommandRecord.TryDeserialize(tooSmall, out _, out _));

            byte[] tooLarge = (byte[])valid.Clone();
            tooLarge[0] = 0x01;
            tooLarge[1] = 0x10; // 4097
            Assert.IsFalse(CommandRecord.TryDeserialize(tooLarge, out _, out _));

            byte[] truncated = new byte[valid.Length - 1];
            Array.Copy(valid, truncated, truncated.Length);
            Assert.IsFalse(CommandRecord.TryDeserialize(truncated, out _, out _));

            Assert.IsFalse(CommandRecord.TryDeserialize(new byte[] { 0x15 }, out _, out _));

            byte[] mismatched = (byte[])valid.Clone();
            mismatched[18] = (byte)(mismatched[18] + 1);
            Assert.IsFalse(CommandRecord.TryDeserialize(mismatched, out _, out _));
        }

        [Test]
        public void Accept_RejectsUnknownKindVersionSessionKindAndInactiveSlot()
        {
            var ingress = CommandV1TestUtil.CreateIngress();
            byte[] payload = CommandV1TestUtil.PayloadBytes(new StopPayload(new[] { CommandV1TestUtil.EntityId(1, 1) }));

            Assert.AreEqual(
                CommandIngressResult.Rejected,
                ingress.TryAcceptRecordBytes(
                    CommandV1TestUtil.CraftRecord(0, 1, 0, 1, 99, 1, payload), out CommandRejectReason unknownKind));
            Assert.AreEqual(CommandRejectReason.UnknownKind, unknownKind);

            Assert.AreEqual(
                CommandIngressResult.Rejected,
                ingress.TryAcceptRecordBytes(
                    CommandV1TestUtil.CraftRecord(0, 1, 0, 1, (ushort)CommandKind.Stop, 2, payload), out CommandRejectReason unknownVersion));
            Assert.AreEqual(CommandRejectReason.UnknownPayloadVersion, unknownVersion);

            Assert.AreEqual(
                CommandIngressResult.Rejected,
                ingress.TryAcceptRecordBytes(
                    CommandV1TestUtil.CraftRecord(0, 1, 0, 1, (ushort)CommandKind.PauseRequest, 1, Array.Empty<byte>()),
                    out CommandRejectReason sessionInStream));
            Assert.AreEqual(CommandRejectReason.SessionActionInStream, sessionInStream);

            Assert.AreEqual(
                CommandIngressResult.Rejected,
                ingress.TryAcceptRecordBytes(
                    CommandV1TestUtil.CraftRecord(0, 1, 5, 1, (ushort)CommandKind.Stop, 1, payload), out CommandRejectReason inactiveSlot));
            Assert.AreEqual(CommandRejectReason.InactiveSlot, inactiveSlot);
        }

        [Test]
        public void Accept_RejectsInvalidIdsAndNonCanonicalLists()
        {
            var ingress = CommandV1TestUtil.CreateIngress();
            uint one = CommandV1TestUtil.EntityId(1, 1);
            uint two = CommandV1TestUtil.EntityId(2, 1);
            const ushort stop = (ushort)CommandKind.Stop;

            AssertReject(ingress, stop, CommandV1TestUtil.PayloadBytes(new StopPayload(new uint[] { 0 })),
                CommandRejectReason.InvalidEntityId);
            AssertReject(ingress, stop, CommandV1TestUtil.PayloadBytes(new StopPayload(new uint[] { 5 })),
                CommandRejectReason.InvalidEntityId);
            AssertReject(ingress, stop, CommandV1TestUtil.PayloadBytes(new StopPayload(new[] { two, one })),
                CommandRejectReason.UnsortedEntityList);
            AssertReject(ingress, stop, CommandV1TestUtil.PayloadBytes(new StopPayload(new[] { one, one })),
                CommandRejectReason.UnsortedEntityList);
            AssertReject(ingress, stop, CommandV1TestUtil.PayloadBytes(new StopPayload(Array.Empty<uint>())),
                CommandRejectReason.EmptyEntityList);
            var tooMany = new uint[CommandLimits.MaxEntityIdsPerCommand + 1];
            for (int i = 0; i < tooMany.Length; i++) tooMany[i] = CommandV1TestUtil.EntityId(i, 1);
            AssertReject(ingress, stop, CommandV1TestUtil.PayloadBytes(new StopPayload(tooMany)),
                CommandRejectReason.TooManyEntityIds);
            AssertReject(ingress, (ushort)CommandKind.PlaceBuilding,
                CommandV1TestUtil.PayloadBytes(new PlaceBuildingPayload(0, 1, 1)),
                CommandRejectReason.InvalidDefinitionId);
            AssertReject(ingress, (ushort)CommandKind.QueueUnit,
                CommandV1TestUtil.PayloadBytes(new QueueUnitPayload(one, 3, 0)),
                CommandRejectReason.InvalidCount);
            AssertReject(ingress, (ushort)CommandKind.AttackTarget,
                CommandV1TestUtil.PayloadBytes(new AttackTargetPayload(new[] { one }, 0)),
                CommandRejectReason.InvalidEntityId);
            byte[] fullPayload = CommandV1TestUtil.PayloadBytes(new StopPayload(new[] { one, two }));
            var truncatedPayload = new byte[fullPayload.Length - 1];
            Array.Copy(fullPayload, truncatedPayload, truncatedPayload.Length);
            AssertReject(ingress, stop, truncatedPayload, CommandRejectReason.PayloadMalformed);
        }

        private static void AssertReject(
            CommandIngress ingress, ushort kind, byte[] payload, CommandRejectReason expected)
        {
            CommandIngressResult result = ingress.TryAcceptRecordBytes(
                CommandV1TestUtil.CraftRecord(0, 1, 0, 1, kind, 1, payload),
                out CommandRejectReason reason);
            Assert.AreEqual(CommandIngressResult.Rejected, result, expected.ToString());
            Assert.AreEqual(expected, reason);
        }
    }
}
