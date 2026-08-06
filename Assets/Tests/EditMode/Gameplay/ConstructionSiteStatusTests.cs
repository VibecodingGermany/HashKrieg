using NUnit.Framework;
using Nova.Gameplay;

namespace Nova.Gameplay.Tests
{
    /// <summary>
    /// Contract tests for <see cref="ConstructionSiteStatus"/>: the
    /// presentation mirror of the construction rules behind D-085 (builder
    /// auto-dispatch and the site card's status line). Every rule is pinned
    /// against the sim's own conventions — the AI's deterministic approach
    /// cell, ConstructionSystem's Chebyshev reach, and the Q16.16 progress /
    /// ProductionSpeedMultiplierQ16 accounting — so a drift between what the
    /// HUD says and what the sim does fails here, not in a play session.
    /// </summary>
    [TestFixture]
    public class ConstructionSiteStatusTests
    {
        private const int Footprint = 3; // SimDefinitions.BuildingFootprintCells
        private const int Grid = 128;    // ConstructionSystem.GridSize

        // ----------------------------------------------------------------
        // Approach cell (the D-085 auto-dispatch target; mirrors the AI)
        // ----------------------------------------------------------------

        [Test]
        public void ApproachCell_InteriorOrigin_PicksWestCellOnMiddleRow()
        {
            ConstructionSiteStatus.ApproachCell(10, 10, Footprint, Grid, out int cellX, out int cellY);

            Assert.AreEqual(9, cellX);  // originX - 1, Chebyshev 1 west of the footprint
            Assert.AreEqual(11, cellY); // the footprint's middle row (originY + 1)
        }

        [Test]
        public void ApproachCell_WestMapEdge_FallsBackToEastCell()
        {
            ConstructionSiteStatus.ApproachCell(0, 5, Footprint, Grid, out int cellX, out int cellY);

            Assert.AreEqual(3, cellX); // originX + BuildingFootprintCells: first cell east of the footprint
            Assert.AreEqual(6, cellY);
        }

        [Test]
        public void ApproachCell_BottomMapEdge_ClampsRowIntoGrid()
        {
            ConstructionSiteStatus.ApproachCell(5, Grid - Footprint, Footprint, Grid, out int cellX, out int cellY);

            Assert.AreEqual(4, cellX);
            Assert.AreEqual(Grid - 2, cellY); // originY + 1 = 126 stays inside 0..127
        }

        [Test]
        public void ApproachCell_AnyResult_IsInChebyshevReachOfFootprint()
        {
            // The whole point of the cell: standing on it must satisfy the
            // sim's reach rule, or the dispatched Builder would never build.
            foreach ((int ox, int oy) in new[] { (10, 10), (0, 0), (0, 5), (125, 125), (60, 0) })
            {
                ConstructionSiteStatus.ApproachCell(ox, oy, Footprint, Grid, out int cellX, out int cellY);
                Assert.IsTrue(
                    ConstructionSiteStatus.IsInReachOfFootprint(cellX, cellY, ox, oy, Footprint),
                    $"approach cell ({cellX},{cellY}) of footprint at ({ox},{oy})");
            }
        }

        // ----------------------------------------------------------------
        // Reach mirror (ConstructionSystem.IsInReachOfFootprint)
        // ----------------------------------------------------------------

        [Test]
        public void IsInReachOfFootprint_InsideAndAdjacentCells_AreInReach()
        {
            Assert.IsTrue(ConstructionSiteStatus.IsInReachOfFootprint(10, 10, 10, 10, Footprint)); // on the footprint
            Assert.IsTrue(ConstructionSiteStatus.IsInReachOfFootprint(9, 10, 10, 10, Footprint));  // west adjacent
            Assert.IsTrue(ConstructionSiteStatus.IsInReachOfFootprint(13, 10, 10, 10, Footprint)); // east adjacent
            Assert.IsTrue(ConstructionSiteStatus.IsInReachOfFootprint(9, 9, 10, 10, Footprint));   // diagonal adjacent
            Assert.IsTrue(ConstructionSiteStatus.IsInReachOfFootprint(13, 13, 10, 10, Footprint)); // diagonal adjacent
        }

        [Test]
        public void IsInReachOfFootprint_TwoCellsAway_IsOutOfReach()
        {
            Assert.IsFalse(ConstructionSiteStatus.IsInReachOfFootprint(8, 10, 10, 10, Footprint));
            Assert.IsFalse(ConstructionSiteStatus.IsInReachOfFootprint(14, 10, 10, 10, Footprint));
            Assert.IsFalse(ConstructionSiteStatus.IsInReachOfFootprint(10, 8, 10, 10, Footprint));
            Assert.IsFalse(ConstructionSiteStatus.IsInReachOfFootprint(12, 14, 10, 10, Footprint));
        }

        // ----------------------------------------------------------------
        // Build state and progress accounting
        // ----------------------------------------------------------------

        [Test]
        public void Evaluate_FollowsTheSimsOwnOrder()
        {
            Assert.AreEqual(SiteBuildState.NoBuilder, ConstructionSiteStatus.Evaluate(false, false));
            Assert.AreEqual(SiteBuildState.NoBuilder, ConstructionSiteStatus.Evaluate(false, true));
            Assert.AreEqual(SiteBuildState.BuilderEnRoute, ConstructionSiteStatus.Evaluate(true, false));
            Assert.AreEqual(SiteBuildState.Building, ConstructionSiteStatus.Evaluate(true, true));
        }

        [Test]
        public void Progress01_MapsQ1616TicksToRatio()
        {
            Assert.AreEqual(0f, ConstructionSiteStatus.Progress01(0, 200), 1e-6f);
            Assert.AreEqual(0.5f, ConstructionSiteStatus.Progress01(100 << 16, 200), 1e-6f);
            Assert.AreEqual(1f, ConstructionSiteStatus.Progress01(200 << 16, 200), 1e-6f);
            Assert.AreEqual(1f, ConstructionSiteStatus.Progress01(0, 0)); // degenerate definition: done, never a divide-by-zero
        }

        [Test]
        public void RemainingSecondsCeil_UsesOwnerSpeedNotAnEstimate()
        {
            const int fullPower = 1 << 16;  // ProductionSpeedMultiplierQ16 raw 1.0
            const int lowPower = 1 << 15;   // exact 0.5 under low power

            Assert.AreEqual(20, ConstructionSiteStatus.RemainingSecondsCeil(0, 200, fullPower));
            Assert.AreEqual(40, ConstructionSiteStatus.RemainingSecondsCeil(0, 200, lowPower));
            Assert.AreEqual(10, ConstructionSiteStatus.RemainingSecondsCeil(100 << 16, 200, fullPower));
            Assert.AreEqual(0, ConstructionSiteStatus.RemainingSecondsCeil(200 << 16, 200, fullPower));
            // A single remaining raw tick fraction rounds UP — "fertig in ~0 s" only at exactly zero left.
            Assert.AreEqual(1, ConstructionSiteStatus.RemainingSecondsCeil((200 << 16) - 1, 200, fullPower));
            Assert.AreEqual(-1, ConstructionSiteStatus.RemainingSecondsCeil(0, 200, 0)); // never divide by zero
        }

        // ----------------------------------------------------------------
        // The card's status line
        // ----------------------------------------------------------------

        [Test]
        public void StatusText_NoBuilder_IsTheOwnerFacingWarning()
        {
            Assert.AreEqual(
                "Kein Builder — Bau pausiert. Builder im HQ bauen.",
                ConstructionSiteStatus.StatusText(SiteBuildState.NoBuilder, 0.43f, 12));
        }

        [Test]
        public void StatusText_EnRoute_SaysTheBuilderIsComing()
        {
            Assert.AreEqual(
                "Builder unterwegs — Bau pausiert",
                ConstructionSiteStatus.StatusText(SiteBuildState.BuilderEnRoute, 0.43f, 12));
        }

        [Test]
        public void StatusText_Building_NamesPercentAndEta()
        {
            Assert.AreEqual(
                "im Bau, 43 % — fertig in ~12 s",
                ConstructionSiteStatus.StatusText(SiteBuildState.Building, 0.43f, 12));
            Assert.AreEqual(
                "im Bau, 100 % — fertig in ~0 s",
                ConstructionSiteStatus.StatusText(SiteBuildState.Building, 1.02f, 0)); // clamps, never 102 %
            Assert.AreEqual(
                "im Bau, 43 %",
                ConstructionSiteStatus.StatusText(SiteBuildState.Building, 0.43f, -1)); // no ETA, no lie
        }
    }
}
