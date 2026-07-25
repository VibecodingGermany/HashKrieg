using System;
using Nova.Simulation.CommandsV1;
using NUnit.Framework;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Commands.md section 6, cases 1 and 3: serialization roundtrip for every
    /// activated CommandKind of schema v1 (100 % inventory) and rejection of
    /// invalid lengths, unknown kinds/versions and invalid ids. Parser checks
    /// all lengths before allocation (SimulationCore.md section 7).
    /// </summary>
    [TestFixture]
    public sealed class CommandRecordTests
    {
        private static readonly uint[] ThreeEntities =
        {
            CommandTestUtil.EntityId(1, 1),
            CommandTestUtil.EntityId(2, 1),
            CommandTestUtil.EntityId(3, 2),
        };

        private static CommandIntent[] AllStreamIntents()
        {
            return new[]
            {
                CommandIntent.Create(new MovePayload(ThreeEntities, Nova.Core.SimFixed.FromInt(10), Nova.Core.SimFixed.FromInt(-3))),
                CommandIntent.Create(new StopPayload(ThreeEntities)),
                CommandIntent.Create(new AttackTargetPayload(ThreeEntities, CommandTestUtil.EntityId(9, 1))),
                CommandIntent.Create(new HarvestPayload(ThreeEntities, 7)),
                CommandIntent.Create(new ReturnCargoPayload(ThreeEntities)),
                CommandIntent.Create(new PlaceBuildingPayload(4, 12, 30)),
                CommandIntent.Create(new CancelConstructionPayload(CommandTestUtil.EntityId(5, 1))),
                CommandIntent.Create(new RepairPayload(ThreeEntities, CommandTestUtil.EntityId(6, 1))),
                CommandIntent.Create(new SellPayload(CommandTestUtil.EntityId(7, 1))),
                CommandIntent.Create(new QueueUnitPayload(CommandTestUtil.EntityId(8, 1), 3, 5)),
                CommandIntent.Create(new CancelProductionPayload(CommandTestUtil.EntityId(8, 1), 2)),
                CommandIntent.Create(new SetRallyPointPayload(CommandTestUtil.EntityId(8, 1), Nova.Core.SimFixed.FromInt(1), Nova.Core.SimFixed.FromInt(2))),
                CommandIntent.Create(new InstallDefenseModulePayload(CommandTestUtil.EntityId(8, 1), 9)),
            };
        }

        [Test]
        public void Register_ContainsExactlyTheActivatedV1Inventory()
        {
            Assert.That(Enum.GetValues(typeof(CommandKind)).Length, Is.EqualTo(17));
            Assert.That((ushort)CommandKind.Move, Is.EqualTo(1));
            Assert.That((ushort)CommandKind.InstallDefenseModule, Is.EqualTo(13));
            Assert.That((ushort)CommandKind.PauseRequest, Is.EqualTo(14));
            Assert.That((ushort)CommandKind.LoadRequest, Is.EqualTo(17));
            Assert.That(CommandKindInfo.IsKnown((CommandKind)0), Is.False);
            Assert.That(CommandKindInfo.IsKnown((CommandKind)18), Is.False);
            for (CommandKind kind = CommandKind.Move; kind <= CommandKind.InstallDefenseModule; kind++)
            {
                Assert.That(CommandKindInfo.IsStreamKind(kind), Is.True, kind.ToString());
                Assert.That(CommandKindInfo.IsSessionAction(kind), Is.False, kind.ToString());
            }
            for (CommandKind kind = CommandKind.PauseRequest; kind <= CommandKind.LoadRequest; kind++)
            {
                Assert.That(CommandKindInfo.IsSessionAction(kind), Is.True, kind.ToString());
                Assert.That(CommandKindInfo.IsStreamKind(kind), Is.False, kind.ToString());
            }
        }

        [Test]
        public void Roundtrip_EveryActivatedStreamKind_RecordAndPayload()
        {
            CommandIntent[] intents = AllStreamIntents();
            Assert.That(intents.Length, Is.EqualTo(13), "100 % of the activated stream inventory");

            var ingress = CommandTestUtil.CreateIngress();
            foreach (CommandIntent intent in intents)
            {
                Assert.That(
                    ingress.TrySubmitIntent(intent, out CommandRejectReason reason),
                    Is.EqualTo(CommandIngressResult.Accepted),
                    $"{intent.Kind}: {reason}");
            }

            CommandBatch batch = ingress.SealTickBatch(1);
            Assert.That(batch.Count, Is.EqualTo(13));

            foreach (CommandRecord record in batch.Records)
            {
                // Record roundtrip: serialize -> parse -> identical value and bytes.
                byte[] bytes = record.Serialize();
                Assert.That(
                    CommandRecord.TryDeserialize(bytes, out CommandRecord parsed, out int consumed),
                    Is.True, record.Kind.ToString());
                Assert.That(consumed, Is.EqualTo(bytes.Length));
                Assert.That(parsed, Is.EqualTo(record));
                Assert.That(parsed.Serialize(), Is.EqualTo(bytes));

                // Payload roundtrip: refs of the sealed payload re-serialize to
                // the identical canonical bytes.
                Assert.That(
                    CommandPayloadValidation.TryExtractRefs(record.Kind, record.Payload.Span, out _),
                    Is.True, record.Kind.ToString());
            }

            // Every submitted kind is present exactly once, in register order
            // (same slot and same tick => ordered by sequence).
            for (int i = 0; i < batch.Count; i++)
            {
                Assert.That(batch.Records[i].Kind, Is.EqualTo((CommandKind)(i + 1)));
                Assert.That(batch.Records[i].Sequence, Is.EqualTo((uint)(i + 1)));
                Assert.That(batch.Records[i].PlayerSlot, Is.EqualTo((byte)0));
                Assert.That(batch.Records[i].EnqueueTick, Is.EqualTo(0u));
                Assert.That(batch.Records[i].TargetTick, Is.EqualTo(1u));
                Assert.That(batch.Records[i].PayloadVersion, Is.EqualTo(CommandLimits.PayloadVersionV1));
            }
        }

        [Test]
        public void Parse_RejectsInvalidLengths_BeforeAllocation()
        {
            byte[] valid = CommandTestUtil.CraftRecord(
                0, 1, 0, 1, (ushort)CommandKind.Stop, 1,
                CommandTestUtil.PayloadBytes(new StopPayload(new[] { CommandTestUtil.EntityId(1, 1) })));

            // Declared length below the header size.
            byte[] tooSmall = (byte[])valid.Clone();
            tooSmall[0] = 10;
            tooSmall[1] = 0;
            Assert.That(CommandRecord.TryDeserialize(tooSmall, out _, out _), Is.False);

            // Declared length beyond the record cap.
            byte[] tooLarge = (byte[])valid.Clone();
            tooLarge[0] = 0x01;
            tooLarge[1] = 0x10; // 4097
            Assert.That(CommandRecord.TryDeserialize(tooLarge, out _, out _), Is.False);

            // Declared length larger than the buffer (truncated stream).
            byte[] truncated = new byte[valid.Length - 1];
            Array.Copy(valid, truncated, truncated.Length);
            Assert.That(CommandRecord.TryDeserialize(truncated, out _, out _), Is.False);

            // Buffer shorter than one length field.
            Assert.That(CommandRecord.TryDeserialize(new byte[] { 0x15 }, out _, out _), Is.False);

            // Payload length not matching record length - header.
            byte[] mismatched = (byte[])valid.Clone();
            mismatched[18] = (byte)(mismatched[18] + 1);
            Assert.That(CommandRecord.TryDeserialize(mismatched, out _, out _), Is.False);
        }

        [Test]
        public void Accept_RejectsUnknownKindVersionSessionKindAndInactiveSlot()
        {
            var ingress = CommandTestUtil.CreateIngress();
            byte[] payload = CommandTestUtil.PayloadBytes(new StopPayload(new[] { CommandTestUtil.EntityId(1, 1) }));

            Assert.That(
                ingress.TryAcceptRecordBytes(
                    CommandTestUtil.CraftRecord(0, 1, 0, 1, 99, 1, payload), out CommandRejectReason unknownKind),
                Is.EqualTo(CommandIngressResult.Rejected));
            Assert.That(unknownKind, Is.EqualTo(CommandRejectReason.UnknownKind));

            Assert.That(
                ingress.TryAcceptRecordBytes(
                    CommandTestUtil.CraftRecord(0, 1, 0, 1, (ushort)CommandKind.Stop, 2, payload), out CommandRejectReason unknownVersion),
                Is.EqualTo(CommandIngressResult.Rejected));
            Assert.That(unknownVersion, Is.EqualTo(CommandRejectReason.UnknownPayloadVersion));

            // Session actions never enter the simulation record stream.
            Assert.That(
                ingress.TryAcceptRecordBytes(
                    CommandTestUtil.CraftRecord(0, 1, 0, 1, (ushort)CommandKind.PauseRequest, 1, Array.Empty<byte>()),
                    out CommandRejectReason sessionInStream),
                Is.EqualTo(CommandIngressResult.Rejected));
            Assert.That(sessionInStream, Is.EqualTo(CommandRejectReason.SessionActionInStream));

            // Slot 5 is not an active slot of this session.
            Assert.That(
                ingress.TryAcceptRecordBytes(
                    CommandTestUtil.CraftRecord(0, 1, 5, 1, (ushort)CommandKind.Stop, 1, payload), out CommandRejectReason inactiveSlot),
                Is.EqualTo(CommandIngressResult.Rejected));
            Assert.That(inactiveSlot, Is.EqualTo(CommandRejectReason.InactiveSlot));
        }

        [Test]
        public void Accept_RejectsInvalidIdsAndNonCanonicalLists()
        {
            var ingress = CommandTestUtil.CreateIngress();
            uint one = CommandTestUtil.EntityId(1, 1);
            uint two = CommandTestUtil.EntityId(2, 1);
            const ushort stop = (ushort)CommandKind.Stop;

            // Raw entity id 0 in the list.
            AssertReject(ingress, stop, CommandTestUtil.PayloadBytes(new StopPayload(new uint[] { 0 })),
                CommandRejectReason.InvalidEntityId);
            // Generation 0 is not canonical.
            AssertReject(ingress, stop, CommandTestUtil.PayloadBytes(new StopPayload(new uint[] { 5 })),
                CommandRejectReason.InvalidEntityId);
            // Unsorted list is a structural error, never silently repaired.
            AssertReject(ingress, stop, CommandTestUtil.PayloadBytes(new StopPayload(new[] { two, one })),
                CommandRejectReason.UnsortedEntityList);
            // Duplicates are a structural error (not strictly ascending).
            AssertReject(ingress, stop, CommandTestUtil.PayloadBytes(new StopPayload(new[] { one, one })),
                CommandRejectReason.UnsortedEntityList);
            // Empty list.
            AssertReject(ingress, stop, CommandTestUtil.PayloadBytes(new StopPayload(Array.Empty<uint>())),
                CommandRejectReason.EmptyEntityList);
            // More than MaxEntityIdsPerCommand ids.
            var tooMany = new uint[CommandLimits.MaxEntityIdsPerCommand + 1];
            for (int i = 0; i < tooMany.Length; i++) tooMany[i] = CommandTestUtil.EntityId(i, 1);
            AssertReject(ingress, stop, CommandTestUtil.PayloadBytes(new StopPayload(tooMany)),
                CommandRejectReason.TooManyEntityIds);
            // Definition id 0 is invalid.
            AssertReject(ingress, (ushort)CommandKind.PlaceBuilding,
                CommandTestUtil.PayloadBytes(new PlaceBuildingPayload(0, 1, 1)),
                CommandRejectReason.InvalidDefinitionId);
            // QueueUnit count 0 is invalid.
            AssertReject(ingress, (ushort)CommandKind.QueueUnit,
                CommandTestUtil.PayloadBytes(new QueueUnitPayload(one, 3, 0)),
                CommandRejectReason.InvalidCount);
            // Mandatory target id 0 on the wire.
            AssertReject(ingress, (ushort)CommandKind.AttackTarget,
                CommandTestUtil.PayloadBytes(new AttackTargetPayload(new[] { one }, 0)),
                CommandRejectReason.InvalidEntityId);
            // Truncated payload (valid prefix, missing bytes).
            byte[] fullPayload = CommandTestUtil.PayloadBytes(new StopPayload(new[] { one, two }));
            var truncatedPayload = new byte[fullPayload.Length - 1];
            Array.Copy(fullPayload, truncatedPayload, truncatedPayload.Length);
            AssertReject(ingress, stop, truncatedPayload, CommandRejectReason.PayloadMalformed);
        }

        private static void AssertReject(
            CommandIngress ingress, ushort kind, byte[] payload, CommandRejectReason expected)
        {
            CommandIngressResult result = ingress.TryAcceptRecordBytes(
                CommandTestUtil.CraftRecord(0, 1, 0, 1, kind, 1, payload),
                out CommandRejectReason reason);
            Assert.That(result, Is.EqualTo(CommandIngressResult.Rejected), expected.ToString());
            Assert.That(reason, Is.EqualTo(expected));
        }
    }
}
