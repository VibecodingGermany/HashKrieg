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
    /// Issue #135: the fine-grained placement denial. Schema v1 deliberately
    /// keeps ONE CommandResultCode for every geometry cause
    /// (RejectedInvalidTarget) — the code is frozen replay content — so the
    /// distinguishable reason travels outside the command stream through the
    /// read-only <see cref="ConstructionSystem.GetPlacementDenial"/>. This
    /// suite pins every denial value in its own scenario, the first-failure
    /// order where causes overlap, and — over the full 128x128 grid — the
    /// exact agreement between the new reason surface and the unchanged
    /// <see cref="ConstructionSystem.ValidatePlacement"/> mapping, including
    /// the owner's own case (HQ at the start, money and power fine, the
    /// contested centre field 50+ cells away: denied, and the reason is the
    /// build zone, never the Builder).
    /// </summary>
    [TestFixture]
    public sealed class ConstructionPlacementDenialTests
    {
        // Alliance definition ids (SimDefinitions id rule: the Alliance id IS
        // the role wire value).
        private const ushort Hq = 3;
        private const ushort Refinery = 4;
        private const ushort Power = 5;
        private const ushort Barracks = 7;

        private sealed class Fixture
        {
            public EntityManager Entities { get; }
            public EconomySystem Economy { get; }
            public CostField CostField { get; }
            public ConstructionSystem Construction { get; }
            public SimulationKernel Kernel { get; }

            public Fixture(
                long startingCredits = 1000,
                System.Action<EconomySystem> configure = null,
                bool addDefaultField = true,
                int entityCapacity = 64)
            {
                Entities = new EntityManager(entityCapacity);
                Economy = new EconomySystem(Entities, startingCredits);
                CostField = new CostField(ConstructionSystem.GridSize, ConstructionSystem.GridSize);
                Construction = new ConstructionSystem(Entities, Economy, CostField);
                Kernel = new SimulationKernel(new SimRandom(42UL));
                Kernel.RegisterSystem(Economy);
                Kernel.RegisterSystem(Construction);
                // Pre-start configuration hook (e.g. slot factions, fields):
                // the SetSlotFaction guard locks the assignment at Start().
                configure?.Invoke(Economy);
                if (addDefaultField && Economy.FieldCount == 0)
                {
                    Economy.TryAddField(63, new GridPos2D(20, 24), 9000);
                }
                Kernel.Start();
            }

            public void Step(int ticks)
            {
                for (int i = 0; i < ticks; i++) Kernel.StepTick();
            }
        }

        [Test]
        public void GetPlacementDenial_UnknownDefinition_IsUnknownDefinition()
        {
            var f = new Fixture();
            Assert.That(f.Construction.GetPlacementDenial(0, 99, 30, 30),
                Is.EqualTo(ConstructionSystem.PlacementDenial.UnknownDefinition));
            Assert.That(f.Construction.ValidatePlacement(0, 99, 30, 30),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "the frozen schema-v1 bucket for the same cause");
        }

        [Test]
        public void GetPlacementDenial_ForeignFactionDefinition_IsForeignDefinition()
        {
            var f = new Fixture(configure: e => e.SetSlotFaction(1, FactionId.Legion));
            Assert.That(f.Construction.GetPlacementDenial(1, Barracks, 30, 30),
                Is.EqualTo(ConstructionSystem.PlacementDenial.ForeignDefinition),
                "a Legion slot naming the Alliance Barracks row");
            Assert.That(f.Construction.ValidatePlacement(1, Barracks, 30, 30),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget));
        }

        [Test]
        public void GetPlacementDenial_OutOfMapFootprint_IsFootprintOutsideMap()
        {
            var f = new Fixture();
            Assert.That(f.Construction.GetPlacementDenial(0, Power, 126, 126),
                Is.EqualTo(ConstructionSystem.PlacementDenial.FootprintOutsideMap),
                "the 3x3 footprint must fit the 128x128 grid");
            Assert.That(f.Construction.GetPlacementDenial(0, Power, -2, 30),
                Is.EqualTo(ConstructionSystem.PlacementDenial.FootprintOutsideMap),
                "a negative origin leaves the grid on the other side");
            Assert.That(f.Construction.ValidatePlacement(0, Power, 126, 126),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget));
        }

        [Test]
        public void GetPlacementDenial_OccupiedFootprint_IsFootprintOccupied()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, Power, 20, 20).IsValid, Is.True);
            Assert.That(f.Construction.GetPlacementDenial(0, Power, 21, 21),
                Is.EqualTo(ConstructionSystem.PlacementDenial.FootprintOccupied),
                "the footprints overlap — distinguishable from a mere spacing violation");
            Assert.That(f.Construction.ValidatePlacement(0, Power, 21, 21),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget));
        }

        [Test]
        public void GetPlacementDenial_ImpassableTerrain_IsFootprintOnImpassableTerrain_AndBeatsInfluence()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, Hq, 12, 20).IsValid, Is.True);

            f.CostField.SetCost(22, 22, CostField.ImpassableCost);
            Assert.That(f.Construction.GetPlacementDenial(0, Power, 20, 20),
                Is.EqualTo(ConstructionSystem.PlacementDenial.FootprintOnImpassableTerrain),
                "one impassable cell denies the whole footprint, inside the zone");
            Assert.That(f.Construction.ValidatePlacement(0, Power, 20, 20),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget));

            f.CostField.SetCost(62, 62, CostField.ImpassableCost);
            Assert.That(f.Construction.GetPlacementDenial(0, Power, 61, 61),
                Is.EqualTo(ConstructionSystem.PlacementDenial.FootprintOnImpassableTerrain),
                "first failure wins: terrain precedes the build influence in the validator's order");
        }

        [Test]
        public void GetPlacementDenial_CentreFieldWithMoneyAndPower_IsOutsideBuildInfluence_Issue135()
        {
            // The owner's exact report: HQ at the start, a second Atlas at the
            // contested centre field, enough credits and power — and the
            // placement still fails, because the zone is anchored to finished
            // BUILDINGS (D-108), never to the Builder unit.
            var f = new Fixture(
                startingCredits: 6000,
                configure: e => e.TryAddField(1, new GridPos2D(62, 62), 15000),
                addDefaultField: false);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, Hq, 7, 7).IsValid, Is.True,
                "the start HQ near the canonical start position");
            f.Step(1); // commit the balance: 30 provided, 0 required — power is fine

            Assert.That(f.Construction.GetPlacementDenial(0, Hq, 62, 62),
                Is.EqualTo(ConstructionSystem.PlacementDenial.OutsideBuildInfluence),
                "the centre sits ~53 footprint cells from the only anchor; BuildInfluenceRadiusCells is 8");
            Assert.That(f.Construction.ValidatePlacement(0, Hq, 62, 62),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "the old opaque answer for the identical cell");

            f.Entities.SpawnUnit(
                0,
                new Transform2D(SimFixed.FromInt(62), SimFixed.FromInt(62)),
                SimFixed.FromInt(3),
                role: UnitRole.Builder);
            Assert.That(f.Construction.GetPlacementDenial(0, Hq, 62, 62),
                Is.EqualTo(ConstructionSystem.PlacementDenial.OutsideBuildInfluence),
                "the second Atlas standing right there carries no zone — he is not broken");

            // The counter-play the HUD sentence names: chain finished
            // buildings toward the target and the SAME kind of cell turns
            // legal (one footprint row off the field cell itself, which no
            // non-Refinery may ever cover).
            Assert.That(f.Construction.PlaceCompletedBuilding(0, Power, 54, 56).IsValid, Is.True,
                "a chained anchor reaches the centre (PlaceCompletedBuilding is the setup shortcut)");
            f.Step(1);
            Assert.That(f.Construction.GetPlacementDenial(0, Hq, 62, 57),
                Is.EqualTo(ConstructionSystem.PlacementDenial.None),
                "inside the chained zone, off the field cell, the HQ placement is legal");
        }

        [Test]
        public void GetPlacementDenial_AdjacentFootprint_IsTooCloseToBuilding_AfterInfluence()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, Hq, 10, 10).IsValid, Is.True);
            Assert.That(f.Construction.GetPlacementDenial(0, Power, 13, 10),
                Is.EqualTo(ConstructionSystem.PlacementDenial.TooCloseToBuilding),
                "edge-adjacent footprints have distance 1 < MinimumBuildingDistanceCells");
            Assert.That(f.Construction.ValidatePlacement(0, Power, 13, 10),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget));

            // An ENEMY building blocks spacing too — but where the own zone
            // does not reach, the influence failure is reported first.
            Assert.That(f.Construction.PlaceCompletedBuilding(1, Hq, 40, 40).IsValid, Is.True);
            Assert.That(f.Construction.GetPlacementDenial(0, Power, 43, 40),
                Is.EqualTo(ConstructionSystem.PlacementDenial.OutsideBuildInfluence),
                "first failure wins: outside the own zone masks the enemy-adjacent spacing violation");
        }

        [Test]
        public void GetPlacementDenial_FieldGeometry_IsFieldSpacingViolated_PerRole()
        {
            var f = new Fixture(); // default field at (20,24)
            Assert.That(f.Construction.PlaceCompletedBuilding(0, Hq, 12, 18).IsValid, Is.True);
            f.Step(1);

            Assert.That(f.Construction.GetPlacementDenial(0, Power, 18, 22),
                Is.EqualTo(ConstructionSystem.PlacementDenial.FieldSpacingViolated),
                "a non-Refinery footprint covering the field cell itself (distance 0 < 2)");
            Assert.That(f.Construction.GetPlacementDenial(0, Refinery, 18, 14),
                Is.EqualTo(ConstructionSystem.PlacementDenial.FieldSpacingViolated),
                "a Refinery inside the zone but without a field at distance 1..3 — inverted rule, same denial");
            Assert.That(f.Construction.GetPlacementDenial(0, Refinery, 22, 22),
                Is.EqualTo(ConstructionSystem.PlacementDenial.None),
                "the Refinery at field distance 2 inside the zone passes every check");
        }

        [Test]
        public void GetPlacementDenial_MissingPrerequisite_IsMissingPrerequisite()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, Hq, 12, 20).IsValid, Is.True);
            f.Step(1);

            // Geometry passes (influence 6, spacing 6, field distance 2) and
            // the power rule passes (30 free >= 15): the all-of prerequisite
            // HQ + Power is the FIRST failure.
            Assert.That(f.Construction.GetPlacementDenial(0, Barracks, 20, 20),
                Is.EqualTo(ConstructionSystem.PlacementDenial.MissingPrerequisite));
            Assert.That(f.Construction.ValidatePlacement(0, Barracks, 20, 20),
                Is.EqualTo(CommandResultCode.RejectedPrerequisitesNotMet));
        }

        [Test]
        public void GetPlacementDenial_UncoveredPowerDraw_IsInsufficientPower()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, Hq, 12, 20).IsValid, Is.True);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, Refinery, 30, 20).IsValid, Is.True,
                "20 power draw (the setup shortcut bypasses field geometry by contract)");
            f.Step(1); // commit: 30 provided, 20 required — 10 free

            Assert.That(f.Construction.GetPlacementDenial(0, Refinery, 22, 22),
                Is.EqualTo(ConstructionSystem.PlacementDenial.InsufficientPower),
                "the second Refinery draws 20, only 10 are free");
            Assert.That(f.Construction.ValidatePlacement(0, Refinery, 22, 22),
                Is.EqualTo(CommandResultCode.RejectedPrerequisitesNotMet));
        }

        [Test]
        public void GetPlacementDenial_FullSiteRegister_IsSiteCapacityReached()
        {
            var f = new Fixture(startingCredits: 1000000, entityCapacity: 160);
            // Four completed anchors, each covering a 4x4 cluster of site
            // origins (offsets ±4/±8 — every origin well inside the radius-8
            // zone). The HQ anchor satisfies the Power plant's HQ
            // prerequisite; the Power plant draws nothing, so the power rule
            // never fires for these sites.
            Assert.That(f.Construction.PlaceCompletedBuilding(0, Hq, 20, 20).IsValid, Is.True);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, Power, 20, 90).IsValid, Is.True);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, Power, 90, 20).IsValid, Is.True);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, Power, 90, 90).IsValid, Is.True);
            f.Step(1);

            int placed = 0;
            foreach (var anchor in new[] { (20, 20), (20, 90), (90, 20), (90, 90) })
            {
                foreach (int dx in new[] { -8, -4, 4, 8 })
                {
                    foreach (int dy in new[] { -8, -4, 4, 8 })
                    {
                        int originX = anchor.Item1 + dx;
                        int originY = anchor.Item2 + dy;
                        Assert.That(f.Construction.TryPlaceBuilding(0, Power, originX, originY), Is.True,
                            $"site {placed + 1} at ({originX},{originY})");
                        placed++;
                    }
                }
            }
            Assert.That(placed, Is.EqualTo(ConstructionSystem.MaxSites));
            Assert.That(f.Construction.SiteCount, Is.EqualTo(ConstructionSystem.MaxSites));

            // The 65th: free cells, inside the zone, spacing kept, field
            // distance kept, prerequisite met, no power draw — only the
            // register is full.
            Assert.That(f.Construction.GetPlacementDenial(0, Power, 12, 20),
                Is.EqualTo(ConstructionSystem.PlacementDenial.SiteCapacityReached));
            Assert.That(f.Construction.ValidatePlacement(0, Power, 12, 20),
                Is.EqualTo(CommandResultCode.RejectedPrerequisitesNotMet));
        }

        [Test]
        public void GetPlacementDenial_LegalCell_IsNone_AndValidatePlacementApplies()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, Hq, 12, 20).IsValid, Is.True);
            f.Step(1);

            Assert.That(f.Construction.GetPlacementDenial(0, Power, 20, 20),
                Is.EqualTo(ConstructionSystem.PlacementDenial.None));
            Assert.That(f.Construction.ValidatePlacement(0, Power, 20, 20),
                Is.EqualTo(CommandResultCode.Applied));
            Assert.That(f.Construction.SiteCount, Is.EqualTo(0), "both reads are pure — nothing mutates");
        }

        [Test]
        public void GetPlacementDenial_TheFourCollapsedCauses_AreNowDistinct()
        {
            // The #135 defect: these four causes shared ONE result code.
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, Hq, 12, 20).IsValid, Is.True);
            f.Step(1);

            var occupied = f.Construction.GetPlacementDenial(0, Power, 13, 21);
            var influence = f.Construction.GetPlacementDenial(0, Power, 60, 60);
            var spacing = f.Construction.GetPlacementDenial(0, Power, 15, 20);
            var field = f.Construction.GetPlacementDenial(0, Power, 18, 22);

            Assert.That(
                new[] { occupied, influence, spacing, field },
                Is.EquivalentTo(new[]
                {
                    ConstructionSystem.PlacementDenial.FootprintOccupied,
                    ConstructionSystem.PlacementDenial.OutsideBuildInfluence,
                    ConstructionSystem.PlacementDenial.TooCloseToBuilding,
                    ConstructionSystem.PlacementDenial.FieldSpacingViolated,
                }),
                "four distinguishable reasons where the player used to read one");
            Assert.That(f.Construction.ValidatePlacement(0, Power, 13, 21),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget), "occupied: the frozen code stays the shared bucket");
            Assert.That(f.Construction.ValidatePlacement(0, Power, 60, 60),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget), "influence: the frozen code stays the shared bucket");
            Assert.That(f.Construction.ValidatePlacement(0, Power, 15, 20),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget), "spacing: the frozen code stays the shared bucket");
            Assert.That(f.Construction.ValidatePlacement(0, Power, 18, 22),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget), "field: the frozen code stays the shared bucket");
        }

        [Test]
        public void GetPlacementDenial_AgreesWithValidatePlacement_OnEveryCell()
        {
            // The anti-drift pin: GetPlacementDenial is not a second
            // validator — ValidatePlacement maps its result. Over the full
            // grid, with own anchors, an enemy building, an impassable patch
            // and two fields, the reason and the frozen code must agree
            // EVERYWHERE: None <=> Applied, the three register/economy
            // denials <=> RejectedPrerequisitesNotMet, everything else <=>
            // RejectedInvalidTarget.
            var f = new Fixture(
                configure: e =>
                {
                    e.TryAddField(63, new GridPos2D(20, 24), 9000);
                    e.TryAddField(2, new GridPos2D(64, 64), 9000);
                });
            Assert.That(f.Construction.PlaceCompletedBuilding(0, Hq, 12, 20).IsValid, Is.True);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, Power, 60, 60).IsValid, Is.True,
                "second anchor near the (64,64) field; its 100 power keep the Refinery affordable on the power rule");
            Assert.That(f.Construction.PlaceCompletedBuilding(1, Hq, 100, 100).IsValid, Is.True);
            for (int y = 30; y <= 32; y++)
            {
                for (int x = 30; x <= 32; x++)
                {
                    f.CostField.SetCost((ushort)x, (ushort)y, CostField.ImpassableCost);
                }
            }
            f.Step(1);

            int size = ConstructionSystem.GridSize;
            foreach (ushort defId in new[] { Power, Refinery })
            {
                var seen = new bool[12];
                for (int originY = 0; originY < size; originY++)
                {
                    for (int originX = 0; originX < size; originX++)
                    {
                        ConstructionSystem.PlacementDenial denial =
                            f.Construction.GetPlacementDenial(0, defId, originX, originY);
                        CommandResultCode code =
                            f.Construction.ValidatePlacement(0, defId, originX, originY);
                        seen[(int)denial] = true;

                        if (denial == ConstructionSystem.PlacementDenial.None)
                        {
                            Assert.That(code, Is.EqualTo(CommandResultCode.Applied),
                                $"def {defId} at ({originX},{originY}): no denial must validate");
                        }
                        else if (denial == ConstructionSystem.PlacementDenial.MissingPrerequisite
                            || denial == ConstructionSystem.PlacementDenial.InsufficientPower
                            || denial == ConstructionSystem.PlacementDenial.SiteCapacityReached)
                        {
                            Assert.That(code, Is.EqualTo(CommandResultCode.RejectedPrerequisitesNotMet),
                                $"def {defId} at ({originX},{originY}): {denial}");
                        }
                        else
                        {
                            Assert.That(code, Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                                $"def {defId} at ({originX},{originY}): {denial}");
                        }
                    }
                }

                Assert.That(seen[(int)ConstructionSystem.PlacementDenial.None], Is.True,
                    $"def {defId}: some cell on the map is placeable");
                Assert.That(seen[(int)ConstructionSystem.PlacementDenial.FootprintOutsideMap], Is.True,
                    $"def {defId}: the map edge denies on bounds");
                Assert.That(seen[(int)ConstructionSystem.PlacementDenial.FootprintOccupied], Is.True,
                    $"def {defId}: an anchor's own cells deny on occupancy");
                Assert.That(seen[(int)ConstructionSystem.PlacementDenial.FootprintOnImpassableTerrain], Is.True,
                    $"def {defId}: the impassable patch denies on terrain");
                Assert.That(seen[(int)ConstructionSystem.PlacementDenial.OutsideBuildInfluence], Is.True,
                    $"def {defId}: the far map denies on influence");
                Assert.That(seen[(int)ConstructionSystem.PlacementDenial.TooCloseToBuilding], Is.True,
                    $"def {defId}: an anchor's ring denies on spacing");
                Assert.That(seen[(int)ConstructionSystem.PlacementDenial.FieldSpacingViolated], Is.True,
                    $"def {defId}: field geometry denies somewhere on the grid");
            }
        }
    }
}
