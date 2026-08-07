using System;
using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Flow-field cache suite (EditMode lane): the pathfinding system keeps one
    /// generated field per destination instead of a single global field.
    /// <para>
    /// Regression under test: with one shared field every Move command
    /// retargeted every already-moving unit — ordering group B somewhere made
    /// group A turn around and follow it. The cache is bounded by the
    /// manifest caps (quality/content/mvp-v1.json,
    /// <c>capacity.flowFieldCacheEntryCap</c> = 32,
    /// <c>flowFieldCacheMiBCap</c> = 8), so eviction is unavoidable and must
    /// be deterministic: lowest last-used tick first, ties broken by the
    /// lowest linear grid index — integers only, no time, no hashing, no
    /// reference identity.
    /// </para>
    /// Hand-mirrored with the .NET lane copy of this fixture.
    /// </summary>
    [TestFixture]
    public sealed class FlowFieldCacheTests
    {
        private const ushort MapSize = 32;

        /// <summary>Half a cell, exact in Q16.16 — units start at cell centers.</summary>
        private static readonly SimFixed HalfCell = SimFixed.FromRaw(SimFixed.OneRaw / 2);

        private static PathfindingSystem NewSystem() => new PathfindingSystem(MapSize, MapSize);

        private static Transform2D AtCell(int x, int y)
        {
            return new Transform2D(
                SimFixed.FromInt(x) + HalfCell,
                SimFixed.FromInt(y) + HalfCell);
        }

        private static GridPos2D CellOf(in Transform2D transform)
        {
            return new GridPos2D(
                SimFixed.WorldToGrid(transform.PositionX),
                SimFixed.WorldToGrid(transform.PositionY));
        }

        /// <summary>Sets the cache's recency clock (the system records it in ExecuteTick).</summary>
        private static void AtTick(PathfindingSystem system, uint tick)
        {
            system.ExecuteTick(new Tick(tick));
        }

        [Test]
        public void TwoGroups_WithDifferentDestinations_EachReachesItsOwnDestination()
        {
            // THE regression this cache exists for. Group A is ordered first,
            // group B second. With a single global flow field the later
            // request overwrote the only field and group A followed group B's
            // destination, never arriving at its own.
            var entities = new EntityManager(16);
            var pathfinding = NewSystem();
            var movement = new MovementSystem(entities, pathfinding);

            var destA = new GridPos2D(3, 28);
            var destB = new GridPos2D(28, 3);

            EntityId a0 = entities.SpawnUnit(0, AtCell(3, 3), SimFixed.FromInt(5));
            EntityId a1 = entities.SpawnUnit(0, AtCell(4, 4), SimFixed.FromInt(5));
            EntityId b0 = entities.SpawnUnit(1, AtCell(28, 28), SimFixed.FromInt(5));
            EntityId b1 = entities.SpawnUnit(1, AtCell(27, 27), SimFixed.FromInt(5));

            entities.GetUnitRef(a0).SetTarget(destA);
            entities.GetUnitRef(a1).SetTarget(destA);
            pathfinding.RequestFlowField(destA);

            entities.GetUnitRef(b0).SetTarget(destB);
            entities.GetUnitRef(b1).SetTarget(destB);
            pathfinding.RequestFlowField(destB);

            Assert.That(pathfinding.FlowFieldCacheCount, Is.EqualTo(2),
                "both destinations must be resident, not overwritten");

            for (uint tick = 1; tick <= 400; tick++)
            {
                pathfinding.ExecuteTick(new Tick(tick));
                movement.ExecuteTick(new Tick(tick));
            }

            AssertArrived(entities, a0, destA);
            AssertArrived(entities, a1, destA);
            AssertArrived(entities, b0, destB);
            AssertArrived(entities, b1, destB);
        }

        private static void AssertArrived(EntityManager entities, EntityId id, GridPos2D destination)
        {
            ref UnitState unit = ref entities.GetUnitRef(id);
            Assert.That(unit.IsMoving, Is.False,
                $"unit {id.Index} must have stopped on arrival at {destination}");
            // Two units share one destination cell in this fixture: the first
            // arrival is pushed off the exact cell by the standing separation
            // (Truppenführung) — arrived means ON or directly ADJACENT to the
            // own destination, and crucially NOT near the other group's.
            GridPos2D cell = CellOf(in unit.Transform);
            int chebyshev = Math.Max(Math.Abs(cell.X - destination.X), Math.Abs(cell.Y - destination.Y));
            Assert.That(chebyshev, Is.LessThanOrEqualTo(1),
                $"unit {id.Index} must stand on or beside its OWN destination cell {destination}, is {cell}");
        }

        [Test]
        public void SecondRequest_KeepsTheFirstDestinationsFieldIntact()
        {
            var system = NewSystem();
            var destA = new GridPos2D(4, 4);
            var destB = new GridPos2D(27, 27);

            system.RequestFlowField(destA);
            FlowField fieldA = system.GetField(destA);
            Direction2D towardsAFromNeighbour = fieldA.GetDirection(4, 3);

            system.RequestFlowField(destB);

            Assert.That(system.GetField(destA), Is.SameAs(fieldA),
                "an unrelated request must not recycle a live destination's field");
            Assert.That(fieldA.GetDirection(4, 3), Is.EqualTo(towardsAFromNeighbour),
                "an unrelated request must not overwrite a live destination's directions");
            Assert.That(system.GetField(destB), Is.Not.SameAs(fieldA));
        }

        [Test]
        public void GetField_IsPureLookup_NeverGenerates()
        {
            // Generation must stay inside the virtual RequestFlowField: it is
            // the only interception point the headless perf harness has
            // (tools/Nova.SimRunner/TimedPathfindingSystem.cs). A generating
            // lookup would silently move pathfinding cost out of the measured
            // window.
            var system = NewSystem();
            var destination = new GridPos2D(9, 9);

            Assert.That(system.FlowField, Is.Null, "nothing is cached before the first request");
            Assert.That(system.GetField(destination), Is.Null);
            Assert.That(system.HasField(destination), Is.False);
            Assert.That(system.FlowFieldCacheCount, Is.EqualTo(0),
                "a lookup miss must not populate the cache");

            system.RequestFlowField(destination);
            Assert.That(system.GetField(destination), Is.Not.Null);
            Assert.That(system.FlowFieldCacheCount, Is.EqualTo(1));
        }

        [Test]
        public void RepeatedRequest_ForCachedDestination_ReusesTheSameField()
        {
            var system = NewSystem();
            var destination = new GridPos2D(12, 20);

            AtTick(system, 3);
            system.RequestFlowField(destination);
            FlowField first = system.GetField(destination);

            AtTick(system, 4);
            system.RequestFlowField(new GridPos2D(21, 6));
            system.RequestFlowField(destination);

            Assert.That(system.GetField(destination), Is.SameAs(first));
            Assert.That(system.FlowFieldCacheCount, Is.EqualTo(2));
            Assert.That(system.FlowField, Is.SameAs(first),
                "the repeated request becomes the most recent entry");
        }

        [Test]
        public void OutOfBoundsDestination_IsIgnored()
        {
            var system = NewSystem();

            system.RequestFlowField(new GridPos2D((int)MapSize, 4));
            system.RequestFlowField(GridPos2D.Invalid);

            Assert.That(system.FlowFieldCacheCount, Is.EqualTo(0));
            Assert.That(system.FlowField, Is.Null);
        }

        [Test]
        public void CacheCapacity_FollowsTheManifestCaps()
        {
            // 32x32 cells at one byte per cell is far below the 8 MiB cap, so
            // the entry cap of 32 is the binding limit.
            Assert.That(FlowFieldCache.MaxEntries, Is.EqualTo(32),
                "capacity.flowFieldCacheEntryCap");
            Assert.That(FlowFieldCache.MaxResidentBytes, Is.EqualTo(8 * 1024 * 1024),
                "capacity.flowFieldCacheMiBCap");
            Assert.That(NewSystem().FlowFieldCacheCapacity, Is.EqualTo(FlowFieldCache.MaxEntries));

            // A map large enough to blow the MiB cap lowers the entry count
            // instead of exceeding the budget; the derivation is pure integer
            // arithmetic, so every host agrees.
            int hugeMap = FlowFieldCache.DeriveCapacity(2048, 2048);
            Assert.That(hugeMap, Is.EqualTo(FlowFieldCache.MaxResidentBytes / (2048 * 2048)));
            Assert.That(hugeMap, Is.LessThan(FlowFieldCache.MaxEntries));
            Assert.That(FlowFieldCache.DeriveCapacity(ushort.MaxValue, ushort.MaxValue),
                Is.GreaterThanOrEqualTo(1), "capacity never collapses to zero");
        }

        [Test]
        public void Eviction_PicksLowestLastUsedTick_ThenLowestGridIndex()
        {
            var system = NewSystem();
            int capacity = system.FlowFieldCacheCapacity;

            // Tick 7: fill the cache completely. Row 1 gives strictly
            // ascending grid indices (y * width + x).
            AtTick(system, 7);
            for (int i = 0; i < capacity; i++)
            {
                system.RequestFlowField(new GridPos2D(i, 1));
            }
            Assert.That(system.FlowFieldCacheCount, Is.EqualTo(capacity));

            // Tick 9: refresh the two lowest grid indices so they are no
            // longer the oldest entries.
            AtTick(system, 9);
            system.RequestFlowField(new GridPos2D(0, 1));
            system.RequestFlowField(new GridPos2D(1, 1));

            // A new destination must evict a tick-7 entry (older beats lower
            // index), and among those the lowest grid index: (2,1).
            system.RequestFlowField(new GridPos2D(5, 5));

            Assert.That(system.HasField(new GridPos2D(2, 1)), Is.False,
                "oldest tick, then lowest grid index, is the victim");
            Assert.That(system.HasField(new GridPos2D(0, 1)), Is.True,
                "a refreshed entry outranks an older one despite its lower index");
            Assert.That(system.HasField(new GridPos2D(1, 1)), Is.True);
            Assert.That(system.HasField(new GridPos2D(3, 1)), Is.True);
            Assert.That(system.HasField(new GridPos2D(5, 5)), Is.True);
            Assert.That(system.FlowFieldCacheCount, Is.EqualTo(capacity),
                "the cache never grows past its capacity");
        }

        [Test]
        public void Eviction_NeverDropsTheMostRecentlyRequestedField()
        {
            var system = NewSystem();
            int capacity = system.FlowFieldCacheCapacity;

            AtTick(system, 2);
            for (int i = 0; i < capacity; i++)
            {
                system.RequestFlowField(new GridPos2D(i, 1));
            }

            // Every entry carries tick 2, so the tie-break alone decides. The
            // just-served entry (31,1) has a high index and would survive
            // anyway; (0,1) is the MRU-protected case: request it last, then
            // force an eviction in the same tick.
            system.RequestFlowField(new GridPos2D(0, 1));
            system.RequestFlowField(new GridPos2D(6, 6));

            Assert.That(system.HasField(new GridPos2D(0, 1)), Is.True,
                "the entry serving the most recent request is never the victim");
            Assert.That(system.HasField(new GridPos2D(1, 1)), Is.False,
                "the next-lowest grid index is evicted instead");
        }

        [Test]
        public void Eviction_BeyondCapacity_IsReproducibleAcrossRuns()
        {
            // Same request sequence, two independent systems: identical
            // survivors and identical recency stamps. Any dependence on
            // hashing, allocation order or wall-clock time would show up here.
            int[] firstRun = RunEvictionSequence(out uint[] firstTicks);
            int[] secondRun = RunEvictionSequence(out uint[] secondTicks);

            Assert.That(secondRun, Is.EqualTo(firstRun),
                "eviction must be a pure function of the request sequence");
            Assert.That(secondTicks, Is.EqualTo(firstTicks));
            Assert.That(firstRun.Length, Is.EqualTo(FlowFieldCache.MaxEntries),
                "the sequence must actually overflow the cache");
        }

        /// <summary>
        /// Requests 96 distinct destinations spread over 8 ticks — three times
        /// the entry cap — and returns the surviving destinations as ascending
        /// grid indices plus their last-used ticks.
        /// </summary>
        private static int[] RunEvictionSequence(out uint[] lastUsedTicks)
        {
            var system = NewSystem();
            var cache = new FlowFieldCache(MapSize, MapSize);

            for (int n = 0; n < 96; n++)
            {
                uint tick = (uint)(n / 12) + 1;
                AtTick(system, tick);

                // 96 distinct in-bounds cells: 24 columns over 4 rows.
                var destination = new GridPos2D(4 + (n % 24), 4 + (n / 24) * 6);
                system.RequestFlowField(destination);

                // Re-touch an already cached destination every fifth step so
                // the recency dimension participates, not only insertion.
                if (n % 5 == 0)
                {
                    system.RequestFlowField(new GridPos2D(4 + (n % 7), 4));
                }
            }

            var survivors = new System.Collections.Generic.List<int>();
            var ticks = new System.Collections.Generic.List<uint>();
            for (ushort y = 0; y < MapSize; y++)
            {
                for (ushort x = 0; x < MapSize; x++)
                {
                    var candidate = new GridPos2D(x, y);
                    if (!system.HasField(candidate)) continue;
                    survivors.Add(cache.GridIndex(candidate));
                    ticks.Add(LastUsedTickOf(system, candidate));
                }
            }

            lastUsedTicks = ticks.ToArray();
            return survivors.ToArray();
        }

        /// <summary>
        /// Reads a destination's last-used tick out of the serialized block —
        /// the only canonical view of the cache directory.
        /// </summary>
        private static uint LastUsedTickOf(PathfindingSystem system, GridPos2D destination)
        {
            var writer = new SnapshotBlockWriter();
            system.WriteState(writer);
            var reader = new SnapshotBlockReader(writer.ToArray());

            reader.TryReadUInt8(out _);        // version
            reader.TryReadUInt32(out _);       // cost field epoch
            reader.TryReadUInt32(out _);       // recency clock
            reader.TryReadUInt8(out _);        // has most-recent entry
            reader.TryReadUInt16(out _);       // most-recent X
            reader.TryReadUInt16(out _);       // most-recent Y
            reader.TryReadUInt8(out byte count);

            for (int i = 0; i < count; i++)
            {
                reader.TryReadUInt16(out ushort x);
                reader.TryReadUInt16(out ushort y);
                reader.TryReadUInt32(out uint tick);
                if (x == destination.X && y == destination.Y) return tick;
            }

            Assert.Fail($"destination {destination} is not in the serialized cache directory");
            return 0;
        }

        [Test]
        public void SerializedDirectory_IsCanonicallyOrdered_IndependentOfInsertionOrder()
        {
            // Two hosts that reached the same cache contents by different
            // insertion orders must write byte-identical blocks, otherwise the
            // state hash would depend on slot layout.
            var ascending = NewSystem();
            var descending = NewSystem();

            AtTick(ascending, 11);
            AtTick(descending, 11);
            for (int i = 0; i < 8; i++)
            {
                ascending.RequestFlowField(new GridPos2D(2 + i, 5));
                descending.RequestFlowField(new GridPos2D(9 - i, 5));
            }

            // Align the most-recent marker; only the insertion order differed.
            ascending.RequestFlowField(new GridPos2D(2, 5));
            descending.RequestFlowField(new GridPos2D(2, 5));

            var ascendingWriter = new SnapshotBlockWriter();
            var descendingWriter = new SnapshotBlockWriter();
            ascending.WriteState(ascendingWriter);
            descending.WriteState(descendingWriter);

            Assert.That(descendingWriter.ToArray(), Is.EqualTo(ascendingWriter.ToArray()));
        }
    }
}
