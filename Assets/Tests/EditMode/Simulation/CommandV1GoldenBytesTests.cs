using System;
using NUnit.Framework;
using Nova.Simulation.CommandsV1;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Commands.md section 6, case 2 (EditMode lane): golden bytes per payload
    /// version. Mirror of the .NET lane CommandGoldenBytesTests with Unity
    /// Test Framework asserts. The hex constants are the format-freeze
    /// regression masters generated from the canonical serializer.
    /// </summary>
    [TestFixture]
    public class CommandV1GoldenBytesTests
    {
        private static readonly string[] GoldenHex =
        {
            // Move
            "2a00000000000100000000010000000100011600030001040000020400000308000000000a000000fdff",
            // Stop
            "2200000000000100000000020000000200010e000300010400000204000003080000",
            // AttackTarget
            "2600000000000100000000030000000300011200030001040000020400000308000009040000",
            // Harvest
            "240000000000010000000004000000040001100003000104000002040000030800000700",
            // ReturnCargo
            "2200000000000100000000050000000500010e000300010400000204000003080000",
            // PlaceBuilding
            "1a0000000000010000000006000000060001060004000c001e00",
            // CancelConstruction
            "180000000000010000000007000000070001040005040000",
            // Repair
            "2600000000000100000000080000000800011200030001040000020400000308000006040000",
            // Sell
            "180000000000010000000009000000090001040007040000",
            // QueueUnit
            "1c000000000001000000000a0000000a000108000804000003000500",
            // CancelProduction
            "1a000000000001000000000b0000000b00010600080400000200",
            // SetRallyPoint
            "20000000000001000000000c0000000c00010c00080400000000010000000200",
            // InstallDefenseModule
            "1a000000000001000000000d0000000d00010600080400000900",
        };

        private static ICommandPayload[] GoldenPayloads()
        {
            uint[] entities =
            {
                CommandV1TestUtil.EntityId(1, 1),
                CommandV1TestUtil.EntityId(2, 1),
                CommandV1TestUtil.EntityId(3, 2),
            };
            return new ICommandPayload[]
            {
                new MovePayload(entities, Nova.Core.SimFixed.FromInt(10), Nova.Core.SimFixed.FromInt(-3)),
                new StopPayload(entities),
                new AttackTargetPayload(entities, CommandV1TestUtil.EntityId(9, 1)),
                new HarvestPayload(entities, 7),
                new ReturnCargoPayload(entities),
                new PlaceBuildingPayload(4, 12, 30),
                new CancelConstructionPayload(CommandV1TestUtil.EntityId(5, 1)),
                new RepairPayload(entities, CommandV1TestUtil.EntityId(6, 1)),
                new SellPayload(CommandV1TestUtil.EntityId(7, 1)),
                new QueueUnitPayload(CommandV1TestUtil.EntityId(8, 1), 3, 5),
                new CancelProductionPayload(CommandV1TestUtil.EntityId(8, 1), 2),
                new SetRallyPointPayload(CommandV1TestUtil.EntityId(8, 1), Nova.Core.SimFixed.FromInt(1), Nova.Core.SimFixed.FromInt(2)),
                new InstallDefenseModulePayload(CommandV1TestUtil.EntityId(8, 1), 9),
            };
        }

        [Test]
        public void GoldenBytes_EveryActivatedKind_MatchesFrozenMaster()
        {
            ICommandPayload[] payloads = GoldenPayloads();
            Assert.AreEqual(GoldenHex.Length, payloads.Length, "100 % of the activated inventory");

            for (int i = 0; i < payloads.Length; i++)
            {
                ICommandPayload payload = payloads[i];
                Assert.AreEqual((CommandKind)(i + 1), payload.Kind);
                Assert.AreEqual(CommandLimits.PayloadVersionV1, payload.Version);

                byte[] actual = CommandV1TestUtil.CraftRecord(
                    enqueueTick: 0, targetTick: 1, playerSlot: 0,
                    sequence: (ushort)payload.Kind, kind: (ushort)payload.Kind,
                    payloadVersion: payload.Version,
                    payload: CommandV1TestUtil.PayloadBytes(payload));

                byte[] expected = HexToBytes(GoldenHex[i]);
                Assert.AreEqual(expected, actual,
                    $"golden bytes mismatch for {payload.Kind} (format freeze)");
            }
        }

        [Test]
        public void GoldenBytes_Parse_BackToEquivalentRecord()
        {
            for (int i = 0; i < GoldenHex.Length; i++)
            {
                byte[] bytes = HexToBytes(GoldenHex[i]);
                Assert.IsTrue(CommandRecord.TryDeserialize(bytes, out CommandRecord record, out int consumed),
                    ((CommandKind)(i + 1)).ToString());
                Assert.AreEqual(bytes.Length, consumed);
                Assert.AreEqual(bytes, record.Serialize());
                Assert.AreEqual((CommandKind)(i + 1), record.Kind);
                Assert.AreEqual(1, record.PayloadVersion);
                Assert.AreEqual(1u, record.TargetTick);
                Assert.AreEqual((uint)(i + 1), record.Sequence);
            }
        }

        private static byte[] HexToBytes(string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }
}
