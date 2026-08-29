using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Production;
using Nova.Simulation.State;
using Nova.Simulation.Vision;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Hand-mirrored copy of the canonical Glutrinne terrain predicate
    /// (Gameplay/Match/GlutrinneTerrainMap.cs, 21.7/#94/D-109). The .NET lane
    /// cannot reference the Gameplay assembly, so — exactly like the field
    /// layout (R-1) — the terrain rides as a mirror: canonical in
    /// Gameplay/Match, applied headlessly by Determinism10000Scenario's own
    /// copy, and pinned here cell-exact and by the shared FNV-1a checksum the
    /// EditMode lane pins against the Unity host. ANY edit must be applied to
    /// every copy; the tests below exist to make a one-sided edit red.
    /// </summary>
    internal static class CanonicalTerrainMirror
    {
        public const int CentreX = 62;
        public const int CentreY = 62;
        public const int RingInnerRadius = 14;
        public const int RingOuterRadius = 15;
        public const int CornerGapMinRadius = 11;

        /// <summary>232 ring-band cells minus the 64 corner-gap cells.</summary>
        public const int ImpassableCellCount = 168;

        public static bool IsImpassable(int x, int y)
        {
            int dx = Math.Abs(x - CentreX);
            int dy = Math.Abs(y - CentreY);
            int ring = Math.Max(dx, dy);
            if (ring < RingInnerRadius || ring > RingOuterRadius)
            {
                return false;
            }
            return Math.Min(dx, dy) < CornerGapMinRadius;
        }

        public static int Apply(CostField costField)
        {
            int written = 0;
            for (int y = 0; y < costField.Height; y++)
            {
                for (int x = 0; x < costField.Width; x++)
                {
                    if (!IsImpassable(x, y)) continue;
                    costField.SetCost((ushort)x, (ushort)y, CostField.ImpassableCost);
                    written++;
                }
            }
            return written;
        }
    }

    /// <summary>
    /// Sprint 21 package 21.7 (issue #94, D-109): the centre of the canonical
    /// map is a zone ringed by rock with four chokepoint gaps, and the terrain
    /// has exactly ONE authoritative source per lane with the mirrors pinned.
    /// This suite pins what a hash cannot: the cost field CONTENT (the state
    /// hash covers only the epoch, a mutation count), the D-107 symmetry of
    /// the walls, the measured throat width of the gaps, the
    /// flow-field reachability of every field and HQ from both starts, and
    /// the snapshot/restore identity of a terrain-carrying host.
    /// </summary>
    [TestFixture]
    public sealed class GlutrinneTerrainTests
    {
        private const ulong CanonicalSeed = 0xDE7E000000010271UL;

        /// <summary>
        /// FNV-1a over every cost byte, row-major. The canonical opening's
        /// cost field (terrain + the two HQ footprints) on the Unity host
        /// must hash to this exact value — the EditMode lane
        /// (CanonicalMatchSetupTests) pins the SAME literal against
        /// MatchBootstrap, which chains the Gameplay source, the scenario
        /// mirror and both test mirrors into one equality.
        /// </summary>
        private const ulong PinnedOpeningCostFieldChecksum = 0x68A7C8644C9D06D5UL;

        // ----------------------------------------------------------------
        // Reflection into the scenario (BuildHost/SetupMatch are private)
        // ----------------------------------------------------------------

        private static object InvokeScenarioBuildHost(ulong seed)
        {
            MethodInfo buildHost = typeof(Determinism10000Scenario).GetMethod(
                "BuildHost", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(buildHost, Is.Not.Null,
                "Determinism10000Scenario.BuildHost was renamed — this guard test must follow it.");
            return buildHost.Invoke(null, new object[] { seed, null });
        }

        private static void InvokeScenarioSetupMatch(object host)
        {
            MethodInfo setupMatch = typeof(Determinism10000Scenario).GetMethod(
                "SetupMatch", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(setupMatch, Is.Not.Null,
                "Determinism10000Scenario.SetupMatch was renamed — this guard test must follow it.");
            setupMatch.Invoke(null, new[] { host });
        }

        private static T HostField<T>(object host, string name)
        {
            FieldInfo field = host.GetType().GetField(name);
            Assert.That(field, Is.Not.Null, $"Determinism10000Scenario.Host.{name} was renamed.");
            return (T)field.GetValue(host);
        }

        private static SimulationKernel KernelOf(object host) => HostField<SimulationKernel>(host, "Kernel");
        private static EconomySystem EconomyOf(object host) => HostField<EconomySystem>(host, "Economy");
        private static EntityManager EntitiesOf(object host) => HostField<EntityManager>(host, "Entities");
        private static PathfindingSystem PathfindingOf(object host) => HostField<PathfindingSystem>(host, "Pathfinding");

        internal static ulong ComputeCostFieldChecksum(CostField costs)
        {
            const ulong offsetBasis = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offsetBasis;
            for (ushort y = 0; y < costs.Height; y++)
            {
                for (ushort x = 0; x < costs.Width; x++)
                {
                    hash ^= costs.GetCost(x, y);
                    hash *= prime;
                }
            }
            return hash;
        }

        /// <summary>The two canonical HQ footprints (D-107 origins (4,4)/(118,118), 3x3) — the only non-terrain cost the opening writes.</summary>
        internal static bool IsHqFootprintCell(int x, int y)
        {
            int f = SimDefinitions.BuildingFootprintCells;
            bool local = x >= 4 && x < 4 + f && y >= 4 && y < 4 + f;
            bool enemy = x >= 118 && x < 118 + f && y >= 118 && y < 118 + f;
            return local || enemy;
        }

        // ----------------------------------------------------------------
        // (a) THE MIRROR PIN: content, not just the epoch count
        // ----------------------------------------------------------------

        [Test]
        public void ScenarioHost_AppliesTheMirroredTerrain_CellForCell()
        {
            // BuildHost only — no HQ footprints yet, so the cost field is
            // PURE terrain and any single-cell drift between the scenario's
            // mirror and this reference lands here.
            object host = InvokeScenarioBuildHost(CanonicalSeed);
            CostField costs = PathfindingOf(host).CostField;

            int blocked = 0;
            for (int y = 0; y < costs.Height; y++)
            {
                for (int x = 0; x < costs.Width; x++)
                {
                    bool expected = CanonicalTerrainMirror.IsImpassable(x, y);
                    bool actual = costs.GetCost((ushort)x, (ushort)y) == CostField.ImpassableCost;
                    Assert.That(actual, Is.EqualTo(expected),
                        $"terrain drift at ({x},{y}): the scenario mirror and the reference disagree");
                    if (expected) blocked++;
                }
            }
            Assert.That(blocked, Is.EqualTo(CanonicalTerrainMirror.ImpassableCellCount),
                "232 ring-band cells minus 4x16 corner-gap cells");
            Assert.That(costs.Epoch, Is.EqualTo((uint)CanonicalTerrainMirror.ImpassableCellCount),
                "the epoch after host construction counts exactly the terrain writes");
        }

        [Test]
        public void Terrain_IsD107PointMirrorSymmetric()
        {
            object host = InvokeScenarioBuildHost(CanonicalSeed);
            CostField costs = PathfindingOf(host).CostField;
            // The D-107 mirror (x, y) -> (124 - x, 124 - y) is defined on the
            // 0..124 square: the outer three map rows/columns mirror out of
            // bounds and are map fringe (the weathered edge band), never
            // terrain-bearing.
            for (int y = 0; y <= 124; y++)
            {
                for (int x = 0; x <= 124; x++)
                {
                    Assert.That(
                        costs.GetCost((ushort)(124 - x), (ushort)(124 - y)),
                        Is.EqualTo(costs.GetCost((ushort)x, (ushort)y)),
                        $"D-107: cell ({x},{y}) and its mirror ({124 - x},{124 - y}) must carry the same cost");
                }
            }
        }

        // ----------------------------------------------------------------
        // (b) THE CHOKEPOINT WIDTH — measured, not drawn
        // ----------------------------------------------------------------

        [Test]
        public void Terrain_Chokepoints_MeasureFourCellsAtTheThroat()
        {
            object host = InvokeScenarioBuildHost(CanonicalSeed);
            CostField costs = PathfindingOf(host).CostField;

            // The wall's side middles are solid: the only ways through are
            // the four corner gaps.
            foreach ((int mx, int my) in new[] { (62, 47), (62, 77), (47, 62), (77, 62) })
            {
                Assert.That(costs.GetCost((ushort)mx, (ushort)my), Is.EqualTo(CostField.ImpassableCost),
                    $"the ring's side middle ({mx},{my}) must be wall");
            }

            // Per corner: walking inward along each shell from the corner,
            // the open run must be exactly 5 cells on the outer shell (15)
            // and exactly 4 on the inner shell (14) — the throat. Four cells
            // pass the MS-1 squad gate (six units at the 0.5-cell default
            // radius) four abreast; three would already mean single-file, a
            // blockade.
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sy in new[] { -1, 1 })
                {
                    int outer = OpenRunAlongShell(costs, CanonicalTerrainMirror.RingOuterRadius, sx, sy);
                    int inner = OpenRunAlongShell(costs, CanonicalTerrainMirror.RingInnerRadius, sx, sy);
                    Assert.That(outer, Is.EqualTo(5), $"outer-shell gap run at corner ({sx},{sy})");
                    Assert.That(inner, Is.EqualTo(4), $"inner-shell throat at corner ({sx},{sy})");
                }
            }
        }

        /// <summary>
        /// Counts the open run on one ring shell, starting at the corner cell
        /// and walking away from it along the shell, in both directions; the
        /// run length is the same along x and along y by symmetry of the
        /// predicate, so one direction is measured and the other is asserted
        /// equal. The run ends at the first wall cell.
        /// </summary>
        private static int OpenRunAlongShell(CostField costs, int shellRadius, int sx, int sy)
        {
            int alongY = 0;
            for (int k = shellRadius; k >= 0; k--)
            {
                int x = CanonicalTerrainMirror.CentreX + sx * shellRadius;
                int y = CanonicalTerrainMirror.CentreY + sy * k;
                if (costs.GetCost((ushort)x, (ushort)y) == CostField.ImpassableCost) break;
                alongY++;
            }
            int alongX = 0;
            for (int k = shellRadius; k >= 0; k--)
            {
                int x = CanonicalTerrainMirror.CentreX + sx * k;
                int y = CanonicalTerrainMirror.CentreY + sy * shellRadius;
                if (costs.GetCost((ushort)x, (ushort)y) == CostField.ImpassableCost) break;
                alongX++;
            }
            Assert.That(alongX, Is.EqualTo(alongY), "the gap is symmetric across the corner diagonal");
            return alongY;
        }

        // ----------------------------------------------------------------
        // (c) THE CROSS-LANE CHECKSUM PIN
        // ----------------------------------------------------------------

        [Test]
        public void CanonicalOpening_CostFieldContent_PinnedBySharedChecksum()
        {
            object host = InvokeScenarioBuildHost(CanonicalSeed);
            InvokeScenarioSetupMatch(host);
            CostField costs = PathfindingOf(host).CostField;

            // The full opening's cost field is terrain PLUS the two HQ
            // footprints — assert the composition explicitly, so the checksum
            // below never silently pins a different content than intended.
            for (int y = 0; y < costs.Height; y++)
            {
                for (int x = 0; x < costs.Width; x++)
                {
                    bool expected = CanonicalTerrainMirror.IsImpassable(x, y) || IsHqFootprintCell(x, y);
                    bool actual = costs.GetCost((ushort)x, (ushort)y) == CostField.ImpassableCost;
                    Assert.That(actual, Is.EqualTo(expected),
                        $"opening cost field drift at ({x},{y})");
                }
            }

            ulong checksum = ComputeCostFieldChecksum(costs);
            TestContext.Out.WriteLine($"opening cost field checksum: 0x{checksum:X16}");
            Assert.That(checksum, Is.EqualTo(PinnedOpeningCostFieldChecksum),
                "the EditMode lane pins this same literal against the Unity host — " +
                "a move means one terrain copy drifted from the others");
        }

        // ----------------------------------------------------------------
        // (d) REACHABILITY — the test that keeps a later map edit honest
        // ----------------------------------------------------------------

        [Test]
        public void Terrain_KeepsEveryFieldAndHeadquarterReachable_FromBothStarts()
        {
            object host = InvokeScenarioBuildHost(CanonicalSeed);
            InvokeScenarioSetupMatch(host);
            PathfindingSystem pathfinding = PathfindingOf(host);
            EconomySystem economy = EconomyOf(host);
            EntityManager entities = EntitiesOf(host);

            // Start points are read from the state, not re-literalised: the
            // two Builders of the D-077 opening.
            var starts = new List<(int X, int Y)>();
            var headquarters = new List<GridPos2D>();
            UnitState[] units = entities.RawUnits;
            for (int i = 0; i < entities.Capacity; i++)
            {
                if (!units[i].IsActive) continue;
                int cx = SimFixed.WorldToGrid(units[i].Transform.PositionX);
                int cy = SimFixed.WorldToGrid(units[i].Transform.PositionY);
                if (units[i].Role == UnitRole.Builder) starts.Add((cx, cy));
                else if (units[i].Role == UnitRole.HQ) headquarters.Add(new GridPos2D(cx, cy));
            }
            Assert.That(starts, Has.Count.EqualTo(2), "the D-077 opening spawns exactly one Builder per slot");
            Assert.That(headquarters, Has.Count.EqualTo(2));

            var destinations = new List<GridPos2D>();
            for (ushort id = 1; id <= EconomySystem.MaxFields; id++)
            {
                if (economy.TryGetField(id, out AetheriumField field))
                {
                    destinations.Add(field.GridPos);
                }
            }
            Assert.That(destinations, Has.Count.EqualTo(15), "the 21.7 map registers fifteen fields");
            foreach (GridPos2D hq in headquarters)
            {
                destinations.Add(HqDoorCell(pathfinding.CostField, hq));
            }

            foreach (GridPos2D destination in destinations)
            {
                pathfinding.RequestFlowField(destination);
                foreach ((int sx, int sy) in starts)
                {
                    // The integration wave of the LAST request is the shared
                    // scratch buffer; a destination's field is freshly
                    // generated here because every destination is requested
                    // exactly once.
                    ushort distance = pathfinding.IntegrationField.GetDistance((ushort)sx, (ushort)sy);
                    Assert.That(distance, Is.Not.EqualTo(IntegrationField.Unreachable),
                        $"destination ({destination.X},{destination.Y}) is unreachable from start ({sx},{sy}) — " +
                        "a terrain edit has sealed a field or a base");
                }
            }
        }

        /// <summary>
        /// The HQ footprint is impassable by construction, and its centre is
        /// fully enclosed by its own wall cells, so a wave seeded ON the
        /// centre dies in place. "HQ reachable" therefore means: a unit can
        /// stand beside the footprint. The door cell is the first walkable
        /// cell of the Chebyshev ring two around the centre, in reading
        /// order — deterministic and, for the canonical HQs, open ground.
        /// </summary>
        private static GridPos2D HqDoorCell(CostField costs, GridPos2D hqCentre)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != 2) continue;
                    int x = hqCentre.X + dx;
                    int y = hqCentre.Y + dy;
                    if (costs.IsInBounds(x, y) && costs.IsWalkable((ushort)x, (ushort)y))
                    {
                        return new GridPos2D(x, y);
                    }
                }
            }
            Assert.Fail($"no walkable door cell around the HQ at ({hqCentre.X},{hqCentre.Y})");
            return GridPos2D.Invalid;
        }

        // ----------------------------------------------------------------
        // (e) THE SNAPSHOT CONTRACT, made executable (Randbedingung 3)
        // ----------------------------------------------------------------

        [Test]
        public void SnapshotRestore_ReproducesTheTerrainCarryingCostFieldExactly()
        {
            object source = InvokeScenarioBuildHost(CanonicalSeed);
            InvokeScenarioSetupMatch(source);
            uint epochBefore = PathfindingOf(source).CostField.Epoch;
            Assert.That(epochBefore,
                Is.EqualTo((uint)(CanonicalTerrainMirror.ImpassableCellCount + 2 * 9)),
                "168 terrain writes + two 3x3 HQ footprints — identical on every host before the first snapshot");

            ulong hashBefore = KernelOf(source).CalculateStateHash();
            byte[] snapshot = KernelOf(source).SaveSnapshot();

            // The restore consumer par excellence: a host built by BuildHost
            // and NEVER run through SetupMatch — exactly the scenario's
            // playback path. Because the terrain is part of host
            // construction, this host already carries the walls.
            object restored = InvokeScenarioBuildHost(CanonicalSeed);
            Assert.That(KernelOf(restored).TryRestoreSnapshot(snapshot), Is.True);

            CostField sourceCosts = PathfindingOf(source).CostField;
            CostField restoredCosts = PathfindingOf(restored).CostField;
            for (int y = 0; y < sourceCosts.Height; y++)
            {
                for (int x = 0; x < sourceCosts.Width; x++)
                {
                    Assert.That(restoredCosts.GetCost((ushort)x, (ushort)y),
                        Is.EqualTo(sourceCosts.GetCost((ushort)x, (ushort)y)),
                        $"restored cost field differs at ({x},{y}) — the structural restore proof is broken");
                }
            }
            Assert.That(restoredCosts.Epoch, Is.EqualTo(epochBefore),
                "the serialized epoch is adopted via RestoreEpoch, so later snapshots stay byte-comparable");
            Assert.That(KernelOf(restored).CalculateStateHash(), Is.EqualTo(hashBefore));
            Assert.That(KernelOf(restored).SaveSnapshot(), Is.EqualTo(snapshot),
                "the restored host must round-trip the snapshot byte-identically");
        }
    }
}
