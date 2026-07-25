using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Entity store snapshot block v2 suite (EditMode lane): the Q-040(i)
    /// SimFixed layout (SimFixed raw int32 positions/speeds/radii, SimAngle
    /// uint16 rotation) roundtrips byte-exactly, v1 blocks are rejected and
    /// the smallest fixed-point state mutation changes the state hash.
    /// Mirror of the .NET lane EntityStoreSnapshotV2Tests with Unity Test
    /// Framework asserts.
    /// </summary>
    [TestFixture]
    public class EntityStoreSnapshotV2Tests
    {
        private static EntityManager CreateStoreWithUnits()
        {
            var store = new EntityManager(64);
            store.SpawnUnit(
                0,
                new Transform2D(SimFixed.FromFloat(10.5f), SimFixed.FromFloat(-3.25f), SimAngle.FromRaw(8192)),
                SimFixed.FromInt(5),
                SimFixed.FromFloat(0.4f),
                maxHealth: 80);
            store.SpawnUnit(
                1,
                new Transform2D(SimFixed.FromInt(127), SimFixed.FromInt(127), SimAngle.FromRaw(49152)),
                SimFixed.FromFloat(4.5f));
            return store;
        }

        private static byte[] Serialize(EntityManager store)
        {
            var writer = new SnapshotBlockWriter();
            store.WriteState(writer);
            return writer.ToArray();
        }

        [Test]
        public void BlockV2_RoundtripsByteIdentical_AndRestoresExactState()
        {
            EntityManager store = CreateStoreWithUnits();
            byte[] bytes = Serialize(store);

            var restored = new EntityManager(64);
            Assert.IsTrue(restored.TryValidateState(bytes));
            Assert.IsTrue(restored.TryRestoreState(bytes));
            Assert.AreEqual(bytes, Serialize(restored), "restore -> serialize must be byte-identical");

            // Field-level equality of the first unit, including the SimAngle heading.
            Assert.IsTrue(restored.TryGetUnit(new EntityId(0, 1), out UnitState unit));
            Assert.AreEqual(SimFixed.FromFloat(10.5f), unit.Transform.PositionX);
            Assert.AreEqual(SimFixed.FromFloat(-3.25f), unit.Transform.PositionY);
            Assert.AreEqual(SimAngle.FromRaw(8192), unit.Transform.Rotation);
            Assert.AreEqual(SimFixed.FromInt(5), unit.MoveSpeed);
            Assert.AreEqual(SimFixed.FromFloat(0.4f), unit.Radius);
        }

        [Test]
        public void BlockV1Bytes_AreRejected()
        {
            // A v1 block (float layout) is rejected at the version byte; the
            // pre-G1 reset allows the hard cut without a migration path.
            EntityManager store = CreateStoreWithUnits();
            byte[] v2 = Serialize(store);

            var v1 = (byte[])v2.Clone();
            v1[0] = 1; // former StateVersion
            var victim = new EntityManager(64);
            Assert.IsFalse(victim.TryValidateState(v1));
            Assert.IsFalse(victim.TryRestoreState(v1));
            Assert.AreEqual(0, victim.ActiveCount, "a rejected restore must not mutate the store");
        }

        [Test]
        public void HeaderVersion_IsTwo()
        {
            Assert.AreEqual((byte)2, EntityManager.StateVersion);
            Assert.AreEqual((byte)2, Serialize(CreateStoreWithUnits())[0]);
        }

        [Test]
        public void SingleRawUnitPositionChange_ChangesBlockBytesAndHash()
        {
            // Hash sensitivity at the fixed-point resolution: moving a unit
            // by exactly one Q16.16 raw unit changes the serialized block and
            // therefore the canonical state hash.
            var host = new EntityManager(64);
            EntityId id = host.SpawnUnit(
                0, new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)), SimFixed.FromInt(5));

            byte[] before = Serialize(host);

            ref UnitState unit = ref host.GetUnitRef(id);
            unit.Transform = new Transform2D(
                SimFixed.FromRaw(unit.Transform.PositionX.RawValue + 1),
                unit.Transform.PositionY,
                unit.Transform.Rotation);

            byte[] after = Serialize(host);
            Assert.AreNotEqual(before, after, "one raw unit must change the block bytes");
        }
    }
}
