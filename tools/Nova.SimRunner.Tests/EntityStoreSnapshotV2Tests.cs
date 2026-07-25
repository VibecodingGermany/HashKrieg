using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Entity store snapshot block v2 suite (.NET lane): the Q-040(i)
    /// SimFixed layout (SimFixed raw int32 positions/speeds/radii, SimAngle
    /// uint16 rotation) roundtrips byte-exactly, v1 blocks are rejected and
    /// the smallest fixed-point state mutation changes the state hash.
    /// Mirror of the EditMode lane EntityStoreSnapshotV2Tests.
    /// </summary>
    [TestFixture]
    public sealed class EntityStoreSnapshotV2Tests
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
            Assert.That(restored.TryValidateState(bytes), Is.True);
            Assert.That(restored.TryRestoreState(bytes), Is.True);
            Assert.That(Serialize(restored), Is.EqualTo(bytes), "restore -> serialize must be byte-identical");

            // Field-level equality of the first unit, including the SimAngle heading.
            Assert.That(restored.TryGetUnit(new EntityId(0, 1), out UnitState unit), Is.True);
            Assert.That(unit.Transform.PositionX, Is.EqualTo(SimFixed.FromFloat(10.5f)));
            Assert.That(unit.Transform.PositionY, Is.EqualTo(SimFixed.FromFloat(-3.25f)));
            Assert.That(unit.Transform.Rotation, Is.EqualTo(SimAngle.FromRaw(8192)));
            Assert.That(unit.MoveSpeed, Is.EqualTo(SimFixed.FromInt(5)));
            Assert.That(unit.Radius, Is.EqualTo(SimFixed.FromFloat(0.4f)));
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
            Assert.That(victim.TryValidateState(v1), Is.False);
            Assert.That(victim.TryRestoreState(v1), Is.False);
            Assert.That(victim.ActiveCount, Is.EqualTo(0), "a rejected restore must not mutate the store");
        }

        [Test]
        public void HeaderVersion_IsTwo()
        {
            Assert.That(EntityManager.StateVersion, Is.EqualTo((byte)2));
            Assert.That(Serialize(CreateStoreWithUnits())[0], Is.EqualTo((byte)2));
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
            Assert.That(after, Is.Not.EqualTo(before), "one raw unit must change the block bytes");
        }
    }
}
