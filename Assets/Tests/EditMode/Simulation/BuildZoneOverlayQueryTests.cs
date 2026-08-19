using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Sprint 21 package 21.4 (issue #91): pins the contract the build-zone
    /// overlay relies on. The overlay must never re-derive the zone rule, so
    /// what is pinned here is the RELATION between the two public reads and
    /// the validator, never the values behind them — no radius, no anchor
    /// roles: when D-108 re-opens the anchor list, this suite must keep
    /// passing untouched, with the picture simply following the simulation.
    /// <para>
    /// Two statements: (1) IsInsideBuildInfluence and
    /// HasMinimumBuildingSpacing are NECESSARY for an accepted placement —
    /// a cell either query rejects must never validate, or the overlay would
    /// paint "outside/blocked" on cells a click could still place at.
    /// (2) The pair distinguishes "outside the zone" from "inside the zone
    /// but spacing-blocked" — the two states the overlay paints differently,
    /// and the confusion the test report complained about.
    /// </para>
    /// </summary>
    [TestFixture]
    public class BuildZoneOverlayQueryTests
    {
        private const byte Slot = 0;
        private const ushort DefHQAlliance = 3;
        private const ushort DefPowerAlliance = 5;

        private sealed class Fixture
        {
            public ConstructionSystem Construction { get; }

            public Fixture()
            {
                var entities = new EntityManager(64);
                var economy = new EconomySystem(entities, 1000);
                var costField = new CostField(ConstructionSystem.GridSize, ConstructionSystem.GridSize);
                Construction = new ConstructionSystem(entities, economy, costField);
                var kernel = new SimulationKernel(new SimRandom(42UL));
                kernel.RegisterSystem(economy);
                kernel.RegisterSystem(Construction);
                kernel.Start();
                Assert.That(
                    Construction.PlaceCompletedBuilding(Slot, DefHQAlliance, 4, 4).IsValid,
                    Is.True, "canonical-style start HQ anchor at footprint origin (4,4)");
            }
        }

        [Test]
        public void InfluenceAndSpacing_AreNecessaryForAcceptedPlacement()
        {
            var f = new Fixture();
            int size = ConstructionSystem.GridSize;
            int footprint = SimDefinitions.BuildingFootprintCells;
            for (int y = 0; y + footprint <= size; y += 7)
            {
                for (int x = 0; x + footprint <= size; x += 7)
                {
                    if (f.Construction.IsInsideBuildInfluence(Slot, x, y)
                        && f.Construction.HasMinimumBuildingSpacing(x, y))
                    {
                        continue;
                    }
                    Assert.That(
                        f.Construction.ValidatePlacement(Slot, DefPowerAlliance, x, y),
                        Is.Not.EqualTo(CommandResultCode.Applied),
                        $"({x},{y}): a cell either overlay read rejects must never validate");
                }
            }
        }

        [Test]
        public void TheTwoReads_DistinguishOutsideFromInsideButSpacingBlocked()
        {
            var f = new Fixture();

            // (20,20): far from the only anchor — outside the zone, spacing fine.
            Assert.That(f.Construction.IsInsideBuildInfluence(Slot, 20, 20), Is.False, "outside the zone");
            Assert.That(f.Construction.HasMinimumBuildingSpacing(20, 20), Is.True, "far from every footprint");

            // (7,4): one cell from the HQ footprint — inside the zone, spacing-blocked.
            Assert.That(f.Construction.IsInsideBuildInfluence(Slot, 7, 4), Is.True, "inside the zone");
            Assert.That(f.Construction.HasMinimumBuildingSpacing(7, 4), Is.False, "too close to the HQ footprint");
            Assert.That(
                f.Construction.ValidatePlacement(Slot, DefPowerAlliance, 7, 4),
                Is.Not.EqualTo(CommandResultCode.Applied),
                "spacing-blocked stays rejected — the state the second tint exists for");

            // (13,13): inside the zone, spacing kept — and the full validator
            // agrees (no fields registered, open cost field, prerequisite-free
            // Power definition, so nothing else stands in the way).
            Assert.That(f.Construction.IsInsideBuildInfluence(Slot, 13, 13), Is.True, "inside the zone");
            Assert.That(f.Construction.HasMinimumBuildingSpacing(13, 13), Is.True, "clear of every footprint");
            Assert.That(
                f.Construction.ValidatePlacement(Slot, DefPowerAlliance, 13, 13),
                Is.EqualTo(CommandResultCode.Applied),
                "inside + unblocked validates");
        }

        /// <summary>
        /// The overlay's repaint reads MASKS, not per-cell queries (main-thread
        /// hitch fix): the masks must equal the per-cell reads on EVERY origin
        /// of the grid — the picture may never drift from the rule. The fixture
        /// deliberately mixes the four register cases: a completed own anchor,
        /// a second completed own building, an ACTIVE own site (blocks spacing,
        /// never anchors — D-108) and an enemy building (blocks spacing for
        /// everyone, anchors for nobody here).
        /// </summary>
        [Test]
        public void FillBuildZoneMasks_MatchesThePerCellReads_OnEveryCell()
        {
            var entities = new EntityManager(64);
            var economy = new EconomySystem(entities, 5000);
            var costField = new CostField(ConstructionSystem.GridSize, ConstructionSystem.GridSize);
            var construction = new ConstructionSystem(entities, economy, costField);
            var kernel = new SimulationKernel(new SimRandom(42UL));
            kernel.RegisterSystem(economy);
            kernel.RegisterSystem(construction);
            kernel.Start();

            Assert.That(construction.PlaceCompletedBuilding(Slot, DefHQAlliance, 4, 4).IsValid, Is.True, "own HQ anchor");
            Assert.That(construction.PlaceCompletedBuilding(Slot, DefPowerAlliance, 13, 13).IsValid, Is.True, "own completed Power anchor");
            Assert.That(construction.TryPlaceBuilding(Slot, DefPowerAlliance, 20, 4), Is.True, "active own site");
            Assert.That(construction.PlaceCompletedBuilding(1, 20, 40, 40).IsValid, Is.True, "enemy HQ (Legion def 20)");

            int size = ConstructionSystem.GridSize;
            var influence = new bool[size * size];
            var spacingBlocked = new bool[size * size];
            construction.FillBuildZoneMasks(Slot, influence, spacingBlocked);

            int mismatches = 0;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int cell = y * size + x;
                    bool expectedInfluence = construction.IsInsideBuildInfluence(Slot, x, y);
                    bool expectedBlocked = !construction.HasMinimumBuildingSpacing(x, y);
                    if (influence[cell] != expectedInfluence || spacingBlocked[cell] != expectedBlocked)
                    {
                        if (mismatches++ < 5)
                        {
                            TestContext.Out.WriteLine(
                                $"({x},{y}): mask=({influence[cell]},{spacingBlocked[cell]}) direct=({expectedInfluence},{expectedBlocked})");
                        }
                    }
                }
            }
            Assert.That(mismatches, Is.EqualTo(0), "the repaint masks must equal the per-cell reads on every origin of the grid");
        }
    }
}
