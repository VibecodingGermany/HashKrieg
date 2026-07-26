using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Cost field epoch suite (.NET lane). The cost field is static
    /// prototype content and is NOT serialized; everything derived from it
    /// (integration and flow fields) is a derived cache that restore rebuilds.
    /// That rebuild is only legitimate when the terrain provably did not move,
    /// so the pathfinding block carries <see cref="CostField.Epoch"/> and
    /// restore rejects any block whose epoch differs from the live field.
    /// Before this slice the cost field was covered by no snapshot block at
    /// all and the pathfinding block held nothing but a single destination.
    /// <para>
    /// Hand-mirrored with the EditMode lane copy of this fixture.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class CostFieldEpochSnapshotTests
    {
        private const ulong Seed = 0x5EED42UL;
        private const ushort MapSize = 32;

        /// <summary>Kernel host with the two systems the flow-field cache spans.</summary>
        private sealed class TestHost
        {
            public SimulationKernel Kernel { get; }
            public EntityManager Entities { get; }
            public PathfindingSystem Pathfinding { get; }

            private TestHost(SimulationKernel kernel, EntityManager entities, PathfindingSystem pathfinding)
            {
                Kernel = kernel;
                Entities = entities;
                Pathfinding = pathfinding;
            }

            public static TestHost Create(bool withTerrain)
            {
                var entities = new EntityManager(32);
                var pathfinding = new PathfindingSystem(MapSize, MapSize);
                var movement = new MovementSystem(entities, pathfinding);

                var kernel = new SimulationKernel(new SimRandom(Seed));
                kernel.RegisterSystem(pathfinding);
                kernel.RegisterSystem(movement);
                kernel.Start();

                if (withTerrain)
                {
                    ApplyTerrain(pathfinding.CostField);
                }
                return new TestHost(kernel, entities, pathfinding);
            }

            /// <summary>A short wall; four in-bounds writes, so the epoch lands on 4.</summary>
            public static void ApplyTerrain(CostField costField)
            {
                for (ushort y = 10; y < 14; y++)
                {
                    costField.SetCost(16, y, CostField.ImpassableCost);
                }
            }
        }

        [Test]
        public void Epoch_CountsInBoundsWritesOnly()
        {
            var costField = new CostField(MapSize, MapSize);
            Assert.That(costField.Epoch, Is.EqualTo(0u), "a fresh field is the defined zero state");

            costField.SetCost(3, 3, CostField.ImpassableCost);
            Assert.That(costField.Epoch, Is.EqualTo(1u));

            // Re-writing the same value still counts: the epoch is a mutation
            // counter, not a content hash — over-invalidation is always safe.
            costField.SetCost(3, 3, CostField.ImpassableCost);
            Assert.That(costField.Epoch, Is.EqualTo(2u));

            // An out-of-bounds write is a no-op and must not move the epoch,
            // otherwise two hosts could disagree over a rejected write.
            costField.SetCost(MapSize, 3, CostField.ImpassableCost);
            costField.SetCost(3, MapSize, CostField.ImpassableCost);
            Assert.That(costField.Epoch, Is.EqualTo(2u));

            costField.ResetAll();
            Assert.That(costField.Epoch, Is.EqualTo(3u));
        }

        [Test]
        public void CostFieldMutation_DropsTheFlowFieldCache()
        {
            var system = new PathfindingSystem(MapSize, MapSize);
            var oldDestination = new GridPos2D(20, 20);
            system.RequestFlowField(oldDestination);
            Assert.That(system.FlowFieldCacheCount, Is.EqualTo(1));

            TestHost.ApplyTerrain(system.CostField);

            // Invalidation is lazy: it happens at the next tick boundary or on
            // the next request, whichever comes first. Both paths must drop
            // every entry, since a terrain change invalidates all of them.
            system.RequestFlowField(new GridPos2D(6, 6));

            Assert.That(system.HasField(oldDestination), Is.False,
                "fields generated against the previous terrain must not survive");
            Assert.That(system.FlowFieldCacheCount, Is.EqualTo(1));

            var afterTerrain = new GridPos2D(21, 21);
            system.RequestFlowField(afterTerrain);
            TestHost.ApplyTerrain(system.CostField);
            system.ExecuteTick(new Tick(1));
            Assert.That(system.FlowFieldCacheCount, Is.EqualTo(0),
                "the tick boundary invalidates as well, so a snapshot never sees a stale cache");
            Assert.That(system.HasField(afterTerrain), Is.False);
        }

        [Test]
        public void CostFieldMutation_ChangesTheCanonicalStateHash()
        {
            // Before this slice the cost field was in no snapshot block at
            // all, so terrain could move without any hash reacting.
            var host = TestHost.Create(withTerrain: false);
            ulong before = host.Kernel.CalculateStateHash();

            host.Pathfinding.CostField.SetCost(5, 5, CostField.ImpassableCost);

            Assert.That(host.Kernel.CalculateStateHash(), Is.Not.EqualTo(before));
            Assert.That(host.Kernel.CalculateStateHash(), Is.EqualTo(host.Kernel.CalculateStateHash()),
                "hashing stays read-only and repeatable");
        }

        [Test]
        public void Snapshot_RoundTrip_PreservesEpochCacheDirectoryAndStateHash()
        {
            TestHost source = BuildPopulatedHost();
            Assert.That(source.Pathfinding.CostField.Epoch, Is.EqualTo(4u));
            Assert.That(source.Pathfinding.FlowFieldCacheCount, Is.EqualTo(3));

            ulong hashBefore = source.Kernel.CalculateStateHash();
            byte[] snapshot = source.Kernel.SaveSnapshot();

            // Same host, same terrain: the block round-trips exactly.
            Assert.That(source.Kernel.TryRestoreSnapshot(snapshot), Is.True);
            Assert.That(source.Kernel.CalculateStateHash(), Is.EqualTo(hashBefore));
            Assert.That(source.Kernel.SaveSnapshot(), Is.EqualTo(snapshot));

            // Fresh host with identical terrain: the derived cache is rebuilt
            // from the directory and the restored state hash matches the
            // pre-save hash bit for bit.
            TestHost restored = TestHost.Create(withTerrain: true);
            Assert.That(restored.Kernel.TryRestoreSnapshot(snapshot), Is.True);
            Assert.That(restored.Pathfinding.CostField.Epoch,
                Is.EqualTo(source.Pathfinding.CostField.Epoch));
            Assert.That(restored.Kernel.CalculateStateHash(), Is.EqualTo(hashBefore));
            Assert.That(restored.Kernel.SaveSnapshot(), Is.EqualTo(snapshot));

            Assert.That(restored.Pathfinding.FlowFieldCacheCount,
                Is.EqualTo(source.Pathfinding.FlowFieldCacheCount));
            foreach (GridPos2D destination in PopulatedDestinations)
            {
                Assert.That(restored.Pathfinding.HasField(destination), Is.True,
                    $"the rebuilt cache must hold {destination}");
                AssertFieldsMatch(
                    source.Pathfinding.GetField(destination),
                    restored.Pathfinding.GetField(destination),
                    destination);
            }
            Assert.That(restored.Pathfinding.FlowField,
                Is.SameAs(restored.Pathfinding.GetField(PopulatedDestinations[2])),
                "the most recent entry survives the round trip");

            // The continuation is identical, which is what the derived-cache
            // rebuild has to buy us.
            for (int i = 0; i < 20; i++)
            {
                source.Kernel.StepTick();
                restored.Kernel.StepTick();
                Assert.That(restored.Kernel.CalculateStateHash(),
                    Is.EqualTo(source.Kernel.CalculateStateHash()),
                    $"continuation diverged at tick {i + 1}");
            }
        }

        [Test]
        public void Restore_IsRejectedAtomically_WhenTheCostFieldEpochDiffers()
        {
            byte[] snapshot = BuildPopulatedHost().Kernel.SaveSnapshot();

            // Same map size, same everything — except the terrain never moved,
            // so rebuilding the cached fields here would silently produce
            // different directions than the snapshot's host had.
            TestHost mismatched = TestHost.Create(withTerrain: false);
            ulong hashBefore = mismatched.Kernel.CalculateStateHash();
            byte[] stateBefore = mismatched.Kernel.SaveSnapshot();

            Assert.That(mismatched.Kernel.TryRestoreSnapshot(snapshot), Is.False,
                "an unprovable derived-cache rebuild must be rejected, not attempted");
            Assert.That(mismatched.Kernel.CalculateStateHash(), Is.EqualTo(hashBefore));
            Assert.That(mismatched.Kernel.SaveSnapshot(), Is.EqualTo(stateBefore),
                "a rejected restore leaves the host byte-identical");
            Assert.That(mismatched.Pathfinding.FlowFieldCacheCount, Is.EqualTo(0));
        }

        [Test]
        public void Restore_IsRejected_ForNonCanonicalOrMalformedDirectories()
        {
            TestHost source = BuildPopulatedHost();
            byte[] snapshot = source.Kernel.SaveSnapshot();
            Assert.That(TestHost.Create(withTerrain: true).Kernel.TryRestoreSnapshot(snapshot), Is.True,
                "baseline: the untouched snapshot restores");

            var pathfinding = TestHost.Create(withTerrain: true).Pathfinding;
            var writer = new SnapshotBlockWriter();
            source.Pathfinding.WriteState(writer);
            byte[] block = writer.ToArray();

            Assert.That(pathfinding.TryValidateState(block), Is.True, "baseline block is valid");

            // Version 1 blocks are not migrated: the pre-G1 format window is
            // open, so an old block is a hard reject.
            byte[] oldVersion = (byte[])block.Clone();
            oldVersion[0] = 1;
            Assert.That(pathfinding.TryValidateState(oldVersion), Is.False);

            // Epoch mismatch.
            byte[] wrongEpoch = (byte[])block.Clone();
            wrongEpoch[1] = (byte)(wrongEpoch[1] + 1);
            Assert.That(pathfinding.TryValidateState(wrongEpoch), Is.False);

            // Trailing bytes are a structural error.
            var overlong = new byte[block.Length + 1];
            System.Array.Copy(block, overlong, block.Length);
            Assert.That(pathfinding.TryValidateState(overlong), Is.False);

            // Truncation.
            var truncated = new byte[block.Length - 1];
            System.Array.Copy(block, truncated, truncated.Length);
            Assert.That(pathfinding.TryValidateState(truncated), Is.False);

            // Directory entries must be strictly ascending by grid index;
            // swapping the first two entries breaks the canonical order and is
            // rejected instead of silently accepted as a second encoding of
            // the same state.
            byte[] swapped = (byte[])block.Clone();
            const int directoryStart = 15; // version + epoch + tick + hasMru + mruX + mruY + count
            const int entrySize = 8;       // destX + destY + lastUsedTick
            for (int i = 0; i < entrySize; i++)
            {
                byte tmp = swapped[directoryStart + i];
                swapped[directoryStart + i] = swapped[directoryStart + entrySize + i];
                swapped[directoryStart + entrySize + i] = tmp;
            }
            Assert.That(pathfinding.TryValidateState(swapped), Is.False);

            // A rejected block never touches the host.
            Assert.That(pathfinding.FlowFieldCacheCount, Is.EqualTo(0));
        }

        /// <summary>Three destinations, requested across three ticks so the recency stamps differ.</summary>
        private static readonly GridPos2D[] PopulatedDestinations =
        {
            new GridPos2D(4, 26),
            new GridPos2D(26, 4),
            new GridPos2D(26, 26),
        };

        private static TestHost BuildPopulatedHost()
        {
            TestHost host = TestHost.Create(withTerrain: true);

            EntityId first = host.Entities.SpawnUnit(
                0, new Transform2D(SimFixed.FromInt(4), SimFixed.FromInt(4)), SimFixed.FromInt(4));
            EntityId second = host.Entities.SpawnUnit(
                1, new Transform2D(SimFixed.FromInt(27), SimFixed.FromInt(27)), SimFixed.FromInt(4));

            host.Entities.GetUnitRef(first).SetTarget(PopulatedDestinations[0]);
            host.Pathfinding.RequestFlowField(PopulatedDestinations[0]);
            host.Kernel.StepTick();

            host.Entities.GetUnitRef(second).SetTarget(PopulatedDestinations[1]);
            host.Pathfinding.RequestFlowField(PopulatedDestinations[1]);
            host.Kernel.StepTick();

            host.Pathfinding.RequestFlowField(PopulatedDestinations[2]);
            for (int i = 0; i < 5; i++)
            {
                host.Kernel.StepTick();
            }
            return host;
        }

        private static void AssertFieldsMatch(FlowField expected, FlowField actual, GridPos2D destination)
        {
            Assert.That(actual, Is.Not.Null);
            for (ushort y = 0; y < MapSize; y++)
            {
                for (ushort x = 0; x < MapSize; x++)
                {
                    if (expected.GetDirection(x, y) == actual.GetDirection(x, y)) continue;
                    Assert.Fail(
                        $"rebuilt field for {destination} differs at ({x},{y}): " +
                        $"{expected.GetDirection(x, y)} vs {actual.GetDirection(x, y)}");
                }
            }
        }
    }
}
