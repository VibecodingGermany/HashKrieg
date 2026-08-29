using System;
using NUnit.Framework;
using Nova.Core;
using Nova.Gameplay;
using Nova.Simulation;
using Nova.Simulation.Construction;
using Nova.Simulation.Economy;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;

namespace Nova.Gameplay.Tests
{
    /// <summary>
    /// Contract tests for <see cref="IdleBuilderQuery"/> (sprint 22, #50):
    /// the entity-side idle predicate against the four standing-order
    /// markers the Stop command clears, the construction-site assignment
    /// collection against a REAL site (placed and ticked through the
    /// kernel, so the sim's own auto-assignment names the busy Builder),
    /// and the deterministic ascending-index cycle with its single wrap.
    /// The documented blind spot (a standing repair order is not observable
    /// outside ConstructionSystem) is deliberately NOT approximated here —
    /// no test pins a repairing Builder as busy, because the query cannot
    /// see him; the class docstring and the sprint report carry that.
    /// </summary>
    [TestFixture]
    public class IdleBuilderQueryTests
    {
        /// <summary>
        /// A live construction domain, mirroring ConstructionSystemTests'
        /// fixture: entity store, economy, cost field and kernel, so site
        /// creation and Builder auto-assignment run the sim's own code path.
        /// </summary>
        private sealed class Fixture
        {
            public EntityManager Entities { get; }
            public ConstructionSystem Construction { get; }
            public SimulationKernel Kernel { get; }

            public Fixture()
            {
                Entities = new EntityManager(64);
                var economy = new EconomySystem(Entities, 1000);
                var costField = new CostField(ConstructionSystem.GridSize, ConstructionSystem.GridSize);
                Construction = new ConstructionSystem(Entities, economy, costField);
                Kernel = new SimulationKernel(new SimRandom(42UL));
                Kernel.RegisterSystem(economy);
                Kernel.RegisterSystem(Construction);
                if (economy.FieldCount == 0)
                {
                    economy.TryAddField(63, new GridPos2D(20, 24), 9000);
                }
                Kernel.Start();
            }

            public EntityId SpawnBuilder(byte slot, int x, int y)
            {
                return Entities.SpawnUnit(
                    slot,
                    new Transform2D(SimFixed.FromInt(x), SimFixed.FromInt(y)),
                    SimFixed.FromInt(3),
                    role: UnitRole.Builder);
            }
        }

        // ----------------------------------------------------------------
        // The entity-side predicate (the four markers Stop clears)
        // ----------------------------------------------------------------

        [Test]
        public void HasNoEntitySideOrder_FreshBuilder_IsIdle()
        {
            var entities = new EntityManager(8);
            entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(1), SimFixed.FromInt(1)), SimFixed.FromInt(3), role: UnitRole.Builder);
            ref readonly UnitState builder = ref entities.RawUnits[0];

            Assert.IsTrue(IdleBuilderQuery.HasNoEntitySideOrder(in builder));
        }

        [Test]
        public void HasNoEntitySideOrder_AnyStandingOrder_IsBusy()
        {
            var entities = new EntityManager(8);
            EntityId mover = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(1), SimFixed.FromInt(1)), SimFixed.FromInt(3), role: UnitRole.Builder);
            EntityId attacker = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(2), SimFixed.FromInt(2)), SimFixed.FromInt(3), role: UnitRole.Builder);
            EntityId harvester = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(3), SimFixed.FromInt(3)), SimFixed.FromInt(3), role: UnitRole.Builder);
            EntityId returner = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(4), SimFixed.FromInt(4)), SimFixed.FromInt(3), role: UnitRole.Builder);
            EntityId target = entities.SpawnUnit(1, new Transform2D(SimFixed.FromInt(5), SimFixed.FromInt(5)), SimFixed.FromInt(3), role: UnitRole.LightTank);

            entities.GetUnitRef(mover).SetTarget(new GridPos2D(7, 7));
            entities.GetUnitRef(attacker).AttackTarget = target;
            // A Builder never legitimately holds the economy orders — the
            // predicate still reads them as busy (the view's Apply does not
            // role-filter, so a state that should not exist must never pass
            // as free labour).
            entities.GetUnitRef(harvester).HarvestFieldId = 1;
            entities.GetUnitRef(returner).IsReturningCargo = true;

            Assert.IsFalse(IdleBuilderQuery.HasNoEntitySideOrder(in entities.RawUnits[mover.Index]), "movement order");
            Assert.IsFalse(IdleBuilderQuery.HasNoEntitySideOrder(in entities.RawUnits[attacker.Index]), "attack order");
            Assert.IsFalse(IdleBuilderQuery.HasNoEntitySideOrder(in entities.RawUnits[harvester.Index]), "harvest order");
            Assert.IsFalse(IdleBuilderQuery.HasNoEntitySideOrder(in entities.RawUnits[returner.Index]), "return-cargo order");
        }

        // ----------------------------------------------------------------
        // Site assignment collection (construction-side marker)
        // ----------------------------------------------------------------

        [Test]
        public void CollectAssignedBuilderRaws_NoSites_ReturnsEmpty()
        {
            var f = new Fixture();
            f.SpawnBuilder(0, 30, 30);
            var scratch = new uint[ConstructionSystem.MaxSites];

            Assert.AreEqual(0, IdleBuilderQuery.CollectAssignedBuilderRaws(f.Entities, f.Construction, scratch));
            Assert.AreEqual(0, IdleBuilderQuery.CollectAssignedBuilderRaws(null, f.Construction, scratch), "no store, no raws");
            Assert.AreEqual(0, IdleBuilderQuery.CollectAssignedBuilderRaws(f.Entities, null, scratch), "no construction, no raws");
        }

        [Test]
        public void CollectAssignedBuilderRaws_ActiveSite_HoldsTheAutoAssignedBuilder()
        {
            var f = new Fixture();
            // The existing suite's placement pair: a completed HQ anchors
            // the build influence, a Power site lands at (10,10).
            Assert.IsTrue(f.Construction.PlaceCompletedBuilding(0, 3, 0, 10).IsValid, "HQ influence anchor");
            EntityId builder = f.SpawnBuilder(0, 30, 30);
            Assert.IsTrue(f.Construction.TryPlaceBuilding(0, 5, 10, 10), "Power site placement");

            f.Kernel.StepTick(); // ProgressSites auto-assigns the lowest-index own Builder

            var scratch = new uint[ConstructionSystem.MaxSites];
            int count = IdleBuilderQuery.CollectAssignedBuilderRaws(f.Entities, f.Construction, scratch);

            Assert.AreEqual(1, count, "one active site, one assigned Builder");
            Assert.AreEqual(UnitCommandStateView.ToRawEntityId(builder), scratch[0]);
        }

        [Test]
        public void IsIdleBuilder_SiteAssignment_IsTheConstructionSideBusyMarker()
        {
            var f = new Fixture();
            Assert.IsTrue(f.Construction.PlaceCompletedBuilding(0, 3, 0, 10).IsValid, "HQ influence anchor");
            EntityId assigned = f.SpawnBuilder(0, 30, 30);
            EntityId free = f.SpawnBuilder(0, 40, 40);
            Assert.IsTrue(f.Construction.TryPlaceBuilding(0, 5, 10, 10), "Power site placement");
            f.Kernel.StepTick(); // the lowest-index Builder (assigned) gets the site

            var scratch = new uint[ConstructionSystem.MaxSites];
            int count = IdleBuilderQuery.CollectAssignedBuilderRaws(f.Entities, f.Construction, scratch);

            Assert.IsFalse(
                IdleBuilderQuery.IsIdleBuilder(in f.Entities.RawUnits[assigned.Index], 0, scratch.AsSpan(0, count)),
                "a Builder standing still but held by a site is BUSY — his feet do not decide");
            Assert.IsTrue(
                IdleBuilderQuery.IsIdleBuilder(in f.Entities.RawUnits[free.Index], 0, scratch.AsSpan(0, count)),
                "the unassigned Builder with no orders is idle");
        }

        // ----------------------------------------------------------------
        // The deterministic cycle
        // ----------------------------------------------------------------

        [Test]
        public void TryFindNextIdleBuilder_SkipsBusyForeignAndNonBuilders()
        {
            var f = new Fixture();
            EntityId mover = f.SpawnBuilder(0, 1, 1);          // index 0: busy (move order)
            f.Entities.GetUnitRef(mover).SetTarget(new GridPos2D(7, 7));
            f.Entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(2), SimFixed.FromInt(2)), SimFixed.FromInt(2), role: UnitRole.Harvester); // index 1: wrong role
            f.SpawnBuilder(1, 3, 3);                            // index 2: foreign
            EntityId expected = f.SpawnBuilder(0, 4, 4);        // index 3: the only idle own Builder
            var scratch = new uint[ConstructionSystem.MaxSites];

            bool found = IdleBuilderQuery.TryFindNextIdleBuilder(
                f.Entities, f.Construction, 0, -1, scratch, out EntityId builder);

            Assert.IsTrue(found);
            Assert.AreEqual(expected, builder);
        }

        [Test]
        public void TryFindNextIdleBuilder_ToursAscendingAndWrapsOnce()
        {
            var f = new Fixture();
            EntityId first = f.SpawnBuilder(0, 1, 1);
            EntityId second = f.SpawnBuilder(0, 2, 2);
            EntityId third = f.SpawnBuilder(0, 3, 3);
            var scratch = new uint[ConstructionSystem.MaxSites];

            Assert.IsTrue(IdleBuilderQuery.TryFindNextIdleBuilder(f.Entities, f.Construction, 0, -1, scratch, out EntityId tour));
            Assert.AreEqual(first, tour, "a fresh tour starts at the lowest index");
            Assert.IsTrue(IdleBuilderQuery.TryFindNextIdleBuilder(f.Entities, f.Construction, 0, tour.Index, scratch, out tour));
            Assert.AreEqual(second, tour, "strictly after the previous index");
            Assert.IsTrue(IdleBuilderQuery.TryFindNextIdleBuilder(f.Entities, f.Construction, 0, tour.Index, scratch, out tour));
            Assert.AreEqual(third, tour);
            Assert.IsTrue(IdleBuilderQuery.TryFindNextIdleBuilder(f.Entities, f.Construction, 0, tour.Index, scratch, out tour));
            Assert.AreEqual(first, tour, "the round wraps to the bottom exactly once");
        }

        [Test]
        public void TryFindNextIdleBuilder_SoleIdleBuilder_IsReturnedEveryPress()
        {
            var f = new Fixture();
            EntityId only = f.SpawnBuilder(0, 1, 1);
            EntityId mover = f.SpawnBuilder(0, 2, 2);
            f.Entities.GetUnitRef(mover).SetTarget(new GridPos2D(7, 7));
            var scratch = new uint[ConstructionSystem.MaxSites];

            Assert.IsTrue(IdleBuilderQuery.TryFindNextIdleBuilder(f.Entities, f.Construction, 0, -1, scratch, out EntityId tour));
            Assert.AreEqual(only, tour);
            Assert.IsTrue(IdleBuilderQuery.TryFindNextIdleBuilder(f.Entities, f.Construction, 0, tour.Index, scratch, out tour));
            Assert.AreEqual(only, tour, "one idle Builder is the whole round");
        }

        [Test]
        public void TryFindNextIdleBuilder_NoneIdle_ReturnsFalse()
        {
            var f = new Fixture();
            EntityId mover = f.SpawnBuilder(0, 1, 1);
            f.Entities.GetUnitRef(mover).SetTarget(new GridPos2D(7, 7));
            var scratch = new uint[ConstructionSystem.MaxSites];

            Assert.IsFalse(IdleBuilderQuery.TryFindNextIdleBuilder(
                f.Entities, f.Construction, 0, -1, scratch, out EntityId builder));
            Assert.IsFalse(builder.IsValid);
            Assert.IsFalse(IdleBuilderQuery.TryFindNextIdleBuilder(null, f.Construction, 0, -1, scratch, out _), "no store");
            Assert.IsFalse(IdleBuilderQuery.TryFindNextIdleBuilder(f.Entities, null, 0, -1, scratch, out _), "no construction");
        }
    }
}
