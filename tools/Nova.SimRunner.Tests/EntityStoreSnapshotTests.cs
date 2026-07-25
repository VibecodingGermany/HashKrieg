using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Entity store snapshot block v3 suite (.NET lane): the Q-040(i)
    /// SimFixed layout (SimFixed raw int32 positions/speeds/radii, SimAngle
    /// uint16 rotation) plus the authoritative SimFixed sight radius of the
    /// canonical Fog of War roundtrips byte-exactly, v1/v2 blocks are
    /// rejected and the smallest fixed-point state mutation changes the
    /// block bytes.
    /// Mirror of the EditMode lane EntityStoreSnapshotTests.
    /// </summary>
    [TestFixture]
    public sealed class EntityStoreSnapshotTests
    {
        private static EntityManager CreateStoreWithUnits()
        {
            var store = new EntityManager(64);
            store.SpawnUnit(
                0,
                new Transform2D(SimFixed.FromFloat(10.5f), SimFixed.FromFloat(-3.25f), SimAngle.FromRaw(8192)),
                SimFixed.FromInt(5),
                SimFixed.FromFloat(0.4f),
                maxHealth: 80,
                sightRadius: SimFixed.FromFloat(12.5f));
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
        public void BlockV3_RoundtripsByteIdentical_AndRestoresExactState()
        {
            EntityManager store = CreateStoreWithUnits();
            byte[] bytes = Serialize(store);

            var restored = new EntityManager(64);
            Assert.That(restored.TryValidateState(bytes), Is.True);
            Assert.That(restored.TryRestoreState(bytes), Is.True);
            Assert.That(Serialize(restored), Is.EqualTo(bytes), "restore -> serialize must be byte-identical");

            // Field-level equality of the first unit, including the SimAngle
            // heading and the authoritative sight radius.
            Assert.That(restored.TryGetUnit(new EntityId(0, 1), out UnitState unit), Is.True);
            Assert.That(unit.Transform.PositionX, Is.EqualTo(SimFixed.FromFloat(10.5f)));
            Assert.That(unit.Transform.PositionY, Is.EqualTo(SimFixed.FromFloat(-3.25f)));
            Assert.That(unit.Transform.Rotation, Is.EqualTo(SimAngle.FromRaw(8192)));
            Assert.That(unit.MoveSpeed, Is.EqualTo(SimFixed.FromInt(5)));
            Assert.That(unit.Radius, Is.EqualTo(SimFixed.FromFloat(0.4f)));
            Assert.That(unit.SightRadius, Is.EqualTo(SimFixed.FromFloat(12.5f)));

            // The second unit carries the documented default sight radius.
            Assert.That(restored.TryGetUnit(new EntityId(1, 1), out UnitState second), Is.True);
            Assert.That(second.SightRadius, Is.EqualTo(UnitState.DefaultSightRadius));
        }

        [Test]
        public void LegacyBlockVersionsV1AndV2_AreRejected()
        {
            // v1 (float layout) and v2 (no sight radius) blocks are rejected
            // at the version byte; the pre-G1 reset allows the hard cut
            // without a migration path.
            EntityManager store = CreateStoreWithUnits();
            byte[] v3 = Serialize(store);

            foreach (byte legacyVersion in new byte[] { 1, 2 })
            {
                var legacy = (byte[])v3.Clone();
                legacy[0] = legacyVersion;
                var victim = new EntityManager(64);
                Assert.That(victim.TryValidateState(legacy), Is.False, $"v{legacyVersion} must fail validation");
                Assert.That(victim.TryRestoreState(legacy), Is.False, $"v{legacyVersion} must fail restore");
                Assert.That(victim.ActiveCount, Is.EqualTo(0), "a rejected restore must not mutate the store");
            }
        }

        [Test]
        public void HeaderVersion_IsThree()
        {
            Assert.That(EntityManager.StateVersion, Is.EqualTo((byte)3));
            Assert.That(Serialize(CreateStoreWithUnits())[0], Is.EqualTo((byte)3));
        }

        [Test]
        public void SingleRawUnitPositionChange_ChangesBlockBytes()
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

        [Test]
        public void SingleRawSightRadiusChange_ChangesBlockBytes()
        {
            // The sight radius is authoritative (it drives the FoW recompute),
            // so one raw unit of change must move the serialized block.
            var host = new EntityManager(64);
            EntityId id = host.SpawnUnit(
                0, new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)), SimFixed.FromInt(5));

            byte[] before = Serialize(host);

            ref UnitState unit = ref host.GetUnitRef(id);
            unit.SightRadius = SimFixed.FromRaw(unit.SightRadius.RawValue + 1);

            byte[] after = Serialize(host);
            Assert.That(after, Is.Not.EqualTo(before), "one raw unit of sight radius must change the block bytes");
        }

        [Test]
        public void NegativeSpeedOrRadii_AreRejected_WithoutMutatingTheStore()
        {
            // Restore hardening (SimulationCore.md section 1): a tampered
            // snapshot behind VALID container hashes must not smuggle a
            // negative move speed, collision radius or sight radius into the
            // store — the latter would crash the next FoW recompute. The
            // validate phase rejects the block and the host stays untouched.
            var store = new EntityManager(64);
            store.SpawnUnit(
                0,
                new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)),
                SimFixed.FromFloat(4.5f),
                SimFixed.FromFloat(0.4f),
                sightRadius: SimFixed.FromFloat(12.5f));
            byte[] valid = Serialize(store);

            // The chosen raw values all contain bytes that cannot occur in
            // the header/free-list region, so the first little-endian
            // occurrence is provably the intended unit field.
            foreach (int raw in new[]
            {
                SimFixed.FromFloat(4.5f).RawValue,   // move speed
                SimFixed.FromFloat(0.4f).RawValue,   // collision radius
                SimFixed.FromFloat(12.5f).RawValue   // sight radius
            })
            {
                byte[] tampered = WithNegatedFirstOccurrence(valid, raw);
                var victim = new EntityManager(64);
                Assert.That(victim.TryValidateState(tampered), Is.False,
                    $"raw value {raw} negated must fail validation");
                Assert.That(victim.TryRestoreState(tampered), Is.False,
                    $"raw value {raw} negated must fail restore");
                Assert.That(victim.ActiveCount, Is.EqualTo(0),
                    "a rejected restore must not mutate the store");
            }

            // The untouched block still validates and restores.
            var host = new EntityManager(64);
            Assert.That(host.TryValidateState(valid), Is.True);
            Assert.That(host.TryRestoreState(valid), Is.True);
            Assert.That(host.ActiveCount, Is.EqualTo(1));
        }

        /// <summary>Returns a copy of <paramref name="block"/> with the first little-endian occurrence of <paramref name="raw"/> replaced by its negation.</summary>
        private static byte[] WithNegatedFirstOccurrence(byte[] block, int raw)
        {
            var copy = (byte[])block.Clone();
            for (int i = 0; i + 4 <= copy.Length; i++)
            {
                if (copy[i] == (byte)raw
                    && copy[i + 1] == (byte)(raw >> 8)
                    && copy[i + 2] == (byte)(raw >> 16)
                    && copy[i + 3] == (byte)(raw >> 24))
                {
                    int negated = -raw;
                    copy[i] = (byte)negated;
                    copy[i + 1] = (byte)(negated >> 8);
                    copy[i + 2] = (byte)(negated >> 16);
                    copy[i + 3] = (byte)(negated >> 24);
                    return copy;
                }
            }
            throw new System.InvalidOperationException($"raw value {raw} not found in block");
        }
    }
}
