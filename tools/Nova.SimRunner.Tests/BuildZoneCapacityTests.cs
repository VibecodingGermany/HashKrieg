using System.Collections.Generic;
using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Sprint 21 package 21.1 (issue #92, D-108): measures how many buildings
    /// fit into the canonical start zone at MinimumBuildingDistanceCells
    /// 2 vs 1 vs 0 — a number, not a feeling, before any value is changed.
    /// <para>
    /// The start zone is the build-influence area of the canonical player-0
    /// HQ anchor alone (footprint origin (4,4), D-107): every 3x3 footprint
    /// whose footprint-aware Chebyshev distance to the HQ rectangle is at most
    /// <see cref="ConstructionSystem.BuildInfluenceRadiusCells"/>. All fifteen
    /// canonical fields (21.6/21.7 layout) are registered, though only the
    /// start field at (7,7) intersects the zone — the next-nearest field sits
    /// at (44,24), thirty cells past the zone boundary, so the pinned numbers
    /// below are deliberately insensitive to the outfield layout.
    /// </para>
    /// <para>
    /// Two lanes keep the number honest. The REAL lane drives the untouched
    /// system (<see cref="ConstructionSystem.ValidatePlacement"/> +
    /// <see cref="ConstructionSystem.PlaceCompletedBuilding"/>) at the current
    /// constant. The MODEL lane re-implements the exact geometric predicates
    /// (map bounds, footprint occupancy, spacing, non-refinery field distance)
    /// parameterized by the minimum distance; it must reproduce the real
    /// lane's placement set cell-for-cell at the current constant before its
    /// 1-and-0 numbers mean anything. Terrain walkability is not modelled and
    /// not present IN THIS FIXTURE: the canonical terrain ring (21.7, D-109)
    /// sits at Chebyshev distance 14..15 from the map centre, dozens of cells
    /// outside the start zone, so the fixture's bare cost field is the
    /// truthful local picture of the zone — and the pinned numbers are
    /// deliberately insensitive to it.
    /// </para>
    /// <para>
    /// The greedy scan is reading order (y, then x): a deterministic,
    /// documented packing — a lower bound on capacity, not a proven maximum.
    /// "Welcher Footprintgrößen" currently has exactly one answer:
    /// <see cref="SimDefinitions.BuildingFootprintCells"/> = 3 for every
    /// building, so the measurement places Power plants (anchor role,
    /// prerequisite HQ, no power draw, non-refinery field spacing).
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class BuildZoneCapacityTests
    {
        // Canonical player-0 start (D-107): HQ footprint origin (4,4), start
        // field point (7,7) with 9.000 AE. The remaining canonical fields
        // (21.6/21.7 layout, mirrored from MatchBootstrap.FieldLayouts like
        // the four R-1 copies) complete the field set the spacing rule
        // iterates. None of the fourteen outfield fields comes near the zone.
        private const int HqOriginX = 4;
        private const int HqOriginY = 4;
        private const ushort DefHQAlliance = 3;
        private const ushort DefPowerAlliance = 5;
        private const byte Slot = 0;

        private static readonly (ushort Id, int X, int Y, long ReserveAE)[] CanonicalFields =
        {
            (1,  7,   7,   9000L),
            (2,  117, 117, 9000L),
            (3,  24,  40,  9000L),
            (4,  100, 84,  9000L),
            (5,  62,  62,  8000L),
            (6,  54,  54,  8000L),
            (7,  70,  70,  8000L),
            (8,  54,  70,  8000L),
            (9,  70,  54,  8000L),
            (10, 44,  24,  9000L),
            (11, 80,  100, 9000L),
            (12, 44,  80,  12000L),
            (13, 80,  44,  12000L),
            (14, 24,  96,  12000L),
            (15, 100, 28,  12000L),
        };

        private static int F => SimDefinitions.BuildingFootprintCells;
        private static int Grid => ConstructionSystem.GridSize;

        private sealed class Fixture
        {
            public ConstructionSystem Construction { get; }

            public Fixture()
            {
                var entities = new EntityManager(64);
                var economy = new EconomySystem(entities, 0);
                var costField = new CostField((ushort)Grid, (ushort)Grid);
                Construction = new ConstructionSystem(entities, economy, costField);
                var kernel = new SimulationKernel(new SimRandom(42UL));
                kernel.RegisterSystem(economy);
                kernel.RegisterSystem(Construction);
                foreach ((ushort id, int x, int y, long reserve) in CanonicalFields)
                {
                    Assert.That(economy.TryAddField(id, new GridPos2D((ushort)x, (ushort)y), reserve), Is.True,
                        $"canonical field {id} registers");
                }
                kernel.Start();
                Assert.That(
                    Construction.PlaceCompletedBuilding(Slot, DefHQAlliance, HqOriginX, HqOriginY).IsValid,
                    Is.True, "canonical start HQ at (4,4)");
            }
        }

        /// <summary>Exact mirror of ConstructionSystem.RectangleDistance (Chebyshev gap of two rectangles).</summary>
        private static int RectangleDistance(
            int leftMinX, int leftMinY, int leftMaxX, int leftMaxY,
            int rightMinX, int rightMinY, int rightMaxX, int rightMaxY)
        {
            int dx = leftMaxX < rightMinX
                ? rightMinX - leftMaxX
                : rightMaxX < leftMinX ? leftMinX - rightMaxX : 0;
            int dy = leftMaxY < rightMinY
                ? rightMinY - leftMaxY
                : rightMaxY < leftMinY ? leftMinY - rightMaxY : 0;
            return System.Math.Max(dx, dy);
        }

        private static int FootprintDistance(int originAX, int originAY, int originBX, int originBY)
        {
            return RectangleDistance(
                originAX, originAY, originAX + F - 1, originAY + F - 1,
                originBX, originBY, originBX + F - 1, originBY + F - 1);
        }

        /// <summary>The start zone: footprint distance to the HQ anchor rectangle at most the influence radius.</summary>
        private static bool InsideStartZone(int originX, int originY)
        {
            return FootprintDistance(originX, originY, HqOriginX, HqOriginY)
                <= ConstructionSystem.BuildInfluenceRadiusCells;
        }

        /// <summary>
        /// Greedy reading-order packing through the untouched system at the
        /// CURRENT constant: validate, then materialize via the documented
        /// completed-building bypass.
        /// </summary>
        private static List<(int X, int Y)> GreedyPackReal(Fixture f)
        {
            var placed = new List<(int X, int Y)>();
            for (int y = 0; y + F <= Grid; y++)
            {
                for (int x = 0; x + F <= Grid; x++)
                {
                    if (!InsideStartZone(x, y)) continue;
                    if (f.Construction.ValidatePlacement(Slot, DefPowerAlliance, x, y) != CommandResultCode.Applied) continue;
                    Assert.That(f.Construction.PlaceCompletedBuilding(Slot, DefPowerAlliance, x, y).IsValid, Is.True,
                        $"validated placement ({x},{y}) materializes");
                    placed.Add((x, y));
                }
            }
            return placed;
        }

        /// <summary>
        /// The same greedy packing against the mirrored predicates,
        /// parameterized by the minimum building distance. The HQ occupies the
        /// first slot so occupancy and spacing treat it like any placement.
        /// </summary>
        private static List<(int X, int Y)> GreedyPackModel(int minimumDistance)
        {
            var placed = new List<(int X, int Y)> { (HqOriginX, HqOriginY) };
            for (int y = 0; y + F <= Grid; y++)
            {
                for (int x = 0; x + F <= Grid; x++)
                {
                    if (!InsideStartZone(x, y)) continue;
                    if (FootprintDistance(x, y, HqOriginX, HqOriginY) < minimumDistance) continue;

                    bool blocked = false;
                    for (int i = 1; i < placed.Count && !blocked; i++)
                    {
                        // FootprintFree: spacing >= 1 implies no overlap; at
                        // distance 0 the rectangles may touch but never share
                        // a cell — overlap is a negative rectangle gap, so
                        // occupancy needs its own check at every distance.
                        if (RectanglesOverlap(x, y, placed[i].X, placed[i].Y)
                            || FootprintDistance(x, y, placed[i].X, placed[i].Y) < minimumDistance)
                        {
                            blocked = true;
                        }
                    }
                    if (blocked || RectanglesOverlap(x, y, HqOriginX, HqOriginY)) continue;

                    // HasValidFieldSpacing for a non-refinery role: no overlap
                    // with any field point, and at least the documented
                    // non-refinery distance to every field.
                    foreach ((_, int fx, int fy, _) in CanonicalFields)
                    {
                        int distance = RectangleDistance(fx, fy, fx, fy, x, y, x + F - 1, y + F - 1);
                        if (distance < ConstructionSystem.MinimumNonRefineryFieldDistanceCells)
                        {
                            blocked = true;
                            break;
                        }
                    }
                    if (blocked) continue;

                    placed.Add((x, y));
                }
            }
            placed.RemoveAt(0); // the HQ anchor is not part of the answer
            return placed;
        }

        private static bool RectanglesOverlap(int originAX, int originAY, int originBX, int originBY)
        {
            return originAX <= originBX + F - 1 && originBX <= originAX + F - 1
                && originAY <= originBY + F - 1 && originBY <= originAY + F - 1;
        }

        [Test]
        public void StartZoneCapacity_CurrentSpacing_RealSystemMatchesModel_AndPinsTheNumber()
        {
            Assert.That(ConstructionSystem.MinimumBuildingDistanceCells, Is.EqualTo(2),
                "this measurement is pinned against spacing 2 — changing the constant re-opens 21.1 (own PR, own D-ID, RulesHash64 moves)");
            Assert.That(SimDefinitions.BuildingFootprintCells, Is.EqualTo(3),
                "the measurement assumes the single uniform 3x3 footprint");
            Assert.That(ConstructionSystem.BuildInfluenceRadiusCells, Is.EqualTo(8),
                "the start zone is defined by this radius (D-104/D-108)");

            var f = new Fixture();
            List<(int X, int Y)> real = GreedyPackReal(f);
            List<(int X, int Y)> model = GreedyPackModel(ConstructionSystem.MinimumBuildingDistanceCells);

            Assert.That(model, Is.EqualTo(real).AsCollection,
                "the geometric model must reproduce the real system cell-for-cell at the current constant before its variant numbers mean anything");
            Assert.That(real.Count, Is.EqualTo(15),
                "pinned measurement: 15 Power plants fit the canonical start zone at MinimumBuildingDistanceCells = 2");
        }

        [Test]
        public void StartZoneCapacity_SpacingVariants_ModelReportsTheTradeoff()
        {
            Assert.That(ConstructionSystem.MinimumBuildingDistanceCells, Is.EqualTo(2),
                "variant 2 is the current rule; the model is validated against the real system in the sibling test");

            int atTwo = GreedyPackModel(2).Count;
            int atOne = GreedyPackModel(1).Count;
            int atZero = GreedyPackModel(0).Count;

            Assert.That(atTwo, Is.EqualTo(15), "current rule (D-104): one fully empty cell ring between footprints");
            Assert.That(atOne, Is.EqualTo(23), "spacing 1: footprints may touch corner-to-corner, +53% capacity");
            Assert.That(atZero, Is.EqualTo(atOne),
                "spacing 0 changes NOTHING over spacing 1: footprint occupancy already forbids every sub-1 configuration, so the real choice is 2 vs 1");
        }
    }
}
