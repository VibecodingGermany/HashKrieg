using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Canonical construction suite (EditMode lane): the MS-1 definition table,
    /// placement validation (cost, occupancy, prerequisite, power rule and
    /// the manifest start-refinery exception), builder-driven progress with
    /// the exact Q16.16 low-power halving, completion into role entities,
    /// the ResearchLab T2 unlock, cancel/sell refund rules, repair orders
    /// and the snapshot block 105 v1 contract. All values are documented
    /// Q-040 provisionals of SimDefinitions.
    /// Mirror of the .NET lane ConstructionSystemTests.
    /// </summary>
    [TestFixture]
    public class ConstructionSystemTests
    {
        private sealed class Fixture
        {
            public EntityManager Entities { get; }
            public EconomySystem Economy { get; }
            public ConstructionSystem Construction { get; }
            public SimulationKernel Kernel { get; }

            public Fixture(long startingCredits = 1000)
            {
                Entities = new EntityManager(64);
                Economy = new EconomySystem(Entities, startingCredits);
                Construction = new ConstructionSystem(Entities, Economy);
                Kernel = new SimulationKernel(new SimRandom(42UL));
                Kernel.RegisterSystem(Economy);
                Kernel.RegisterSystem(Construction);
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

            public void Step(int ticks)
            {
                for (int i = 0; i < ticks; i++) Kernel.StepTick();
            }
        }

        [Test]
        public void Definitions_CoverTheManifestRoles_WithConsistentValues()
        {
            Assert.That(SimDefinitions.BuildingCount, Is.EqualTo(9), "nine MS-1 building roles (mvp-v1.json)");
            Assert.That(SimDefinitions.UnitCount, Is.EqualTo(8), "eight MS-1 unit roles (mvp-v1.json)");

            for (ushort id = 1; id <= 9; id++)
            {
                Assert.That(SimDefinitions.TryGetBuilding(id, out SimBuildingDefinition def), Is.True);
                Assert.That(def.DefinitionId, Is.EqualTo(id));
                Assert.That(def.CostAE, Is.GreaterThan(0));
                Assert.That(def.BuildTicks, Is.GreaterThan(0));
                Assert.That(def.MaxHealth, Is.GreaterThan(0));
                Assert.That(SimDefinitions.IsBuildingRole(def.Role), Is.True);
            }
            Assert.That(SimDefinitions.TryGetBuilding((ushort)10, out _), Is.False, "raw id space ends at 9");
            Assert.That(SimDefinitions.TryGetBuilding((ushort)0, out _), Is.False, "raw id 0 is invalid");

            for (ushort id = 1; id <= 8; id++)
            {
                Assert.That(SimDefinitions.TryGetUnit(id, out SimUnitDefinition def), Is.True);
                Assert.That(def.DefinitionId, Is.EqualTo(id));
                Assert.That(def.CostAE, Is.GreaterThan(0));
                Assert.That(def.BuildTicks, Is.GreaterThan(0));
                Assert.That(def.Tier, Is.EqualTo((byte)(id == 4 || id == 7 || id == 8 ? 2 : 1)),
                    "manifest tiers: T2 = AntiArmorInfantry (4), BattleTank (7), Artillery (8)");
            }

            // Manifest tiers: T2 = AntiArmorInfantry, BattleTank, Artillery.
            Assert.That(SimDefinitions.TryGetUnit(4, out SimUnitDefinition aa) && aa.Tier == 2, Is.True);
            Assert.That(SimDefinitions.TryGetUnit(7, out SimUnitDefinition bt) && bt.Tier == 2, Is.True);
            Assert.That(SimDefinitions.TryGetUnit(8, out SimUnitDefinition ar) && ar.Tier == 2, Is.True);

            // Documented producer assignment (Q-040): HQ -> Builder/Harvester,
            // Barracks -> infantry, VehicleFactory -> vehicles.
            Assert.That(SimDefinitions.TryGetUnit(1, out SimUnitDefinition builder) && builder.ProducerRole == UnitRole.HQ, Is.True);
            Assert.That(SimDefinitions.TryGetUnit(2, out SimUnitDefinition harvester) && harvester.ProducerRole == UnitRole.HQ, Is.True);
            Assert.That(SimDefinitions.TryGetUnit(3, out SimUnitDefinition rifle) && rifle.ProducerRole == UnitRole.Barracks, Is.True);
            Assert.That(SimDefinitions.TryGetUnit(5, out SimUnitDefinition scout) && scout.ProducerRole == UnitRole.VehicleFactory, Is.True);

            // Provisional power figures used by the economy recompute.
            Assert.That(SimDefinitions.TryGetBuilding(UnitRole.HQ, out SimBuildingDefinition hq) && hq.PowerProvided == 30, Is.True);
            Assert.That(SimDefinitions.TryGetBuilding(UnitRole.Power, out SimBuildingDefinition plant) && plant.PowerProvided == 100, Is.True);
            Assert.That(SimDefinitions.TryGetBuilding(UnitRole.Refinery, out SimBuildingDefinition refinery)
                        && refinery.PowerRequired == 20 && refinery.HasPrerequisite && refinery.PrerequisiteRole == UnitRole.Power, Is.True,
                "a non-start refinery requires a completed Power plant (manifest exception covers only the start refinery)");
        }

        [Test]
        public void PlaceBuilding_ChargesExactCost_AndCreatesSiteEntity()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 2, 40, 40).IsValid, Is.True, "power provider");
            f.SpawnBuilder(0, 19, 20);
            f.Step(1); // commit the balance (phase-2 recompute)

            Assert.That(f.Construction.TryPlaceBuilding(0, 5, 20, 20), Is.True, "Barracks def 5");
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(500L),
                "Barracks costs exactly 500 AE (provisional)");
            Assert.That(f.Construction.SiteCount, Is.EqualTo(1));

            // The site entity sits at the footprint center with role Unit and 1 HP.
            bool found = false;
            UnitState[] units = f.Entities.RawUnits;
            for (int i = 0; i < f.Entities.Capacity; i++)
            {
                if (!units[i].IsActive || units[i].Role != UnitRole.Unit) continue;
                found = true;
                Assert.That(units[i].Transform.PositionX, Is.EqualTo(SimFixed.FromInt(21)));
                Assert.That(units[i].Transform.PositionY, Is.EqualTo(SimFixed.FromInt(21)));
                Assert.That(units[i].CurrentHealth, Is.EqualTo(1), "site HP stays 1 until completion (provisional)");
                Assert.That(units[i].MaxHealth, Is.EqualTo(600));
            }
            Assert.That(found, Is.True, "a site entity must exist");
        }

        [Test]
        public void PlaceBuilding_InsufficientFunds_FailsAndMutatesNothing()
        {
            var f = new Fixture();
            f.SpawnBuilder(0, 19, 20);

            Assert.That(f.Construction.TryPlaceBuilding(0, 1, 20, 20), Is.False, "HQ costs 2000, balance is 1000");
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1000L), "a refused placement mutates nothing");
            Assert.That(f.Construction.SiteCount, Is.EqualTo(0));
            Assert.That(f.Construction.IsCellFree(20, 20), Is.True);
        }

        [Test]
        public void PlaceBuilding_OccupiedOrOutOfMap_IsRejectedInvalidTarget()
        {
            var f = new Fixture();
            f.SpawnBuilder(0, 19, 20);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 2, 20, 20).IsValid, Is.True, "Power plant at (20,20)");
            f.Step(1); // commit the balance

            Assert.That(f.Construction.ValidatePlacement(0, 4, 21, 21), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "the 3x3 footprints overlap");
            Assert.That(f.Construction.ValidatePlacement(0, 4, 24, 24), Is.EqualTo(CommandResultCode.Applied),
                "a separate location stays placeable");
            Assert.That(f.Construction.ValidatePlacement(0, 4, 126, 126), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "the footprint must fit the 128x128 grid");
            Assert.That(f.Construction.ValidatePlacement(0, 99, 30, 30), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "an unknown definition id is an invalid target, not a cost failure");
        }

        [Test]
        public void PlaceBuilding_MissingPrerequisite_IsRejectedPrerequisitesNotMet()
        {
            var f = new Fixture();
            f.SpawnBuilder(0, 19, 20);

            Assert.That(f.Construction.ValidatePlacement(0, 3, 20, 20), Is.EqualTo(CommandResultCode.RejectedPrerequisitesNotMet),
                "a Refinery requires a completed own Power plant");
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 2, 40, 40).IsValid, Is.True);
            f.Step(1); // commit the balance (100 provided)
            Assert.That(f.Construction.ValidatePlacement(0, 3, 20, 20), Is.EqualTo(CommandResultCode.Applied));
        }

        [Test]
        public void PlaceBuilding_PowerRule_RequiresSufficientFreePower()
        {
            var f = new Fixture();
            f.SpawnBuilder(0, 19, 20);
            // Committed balance: HQ 30 provided, Refinery 20 required -> 10 free.
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 1, 40, 40).IsValid, Is.True);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 3, 44, 40).IsValid, Is.True);
            f.Step(1); // let the economy recompute the balance

            Assert.That(f.Construction.ValidatePlacement(0, 6, 20, 20), Is.EqualTo(CommandResultCode.RejectedPrerequisitesNotMet),
                "VehicleFactory draws 20 but only 10 are free");
            Assert.That(f.Construction.ValidatePlacement(0, 4, 20, 20), Is.EqualTo(CommandResultCode.Applied),
                "Storage draws exactly the 10 free power");
            Assert.That(f.Construction.ValidatePlacement(0, 2, 60, 60), Is.EqualTo(CommandResultCode.Applied),
                "power-providing buildings are exempt from the rule");
        }

        [Test]
        public void StartRefineryException_CompletedPlacement_BypassesPrerequisiteAndPower()
        {
            var f = new Fixture();
            // Manifest: the STARTING Refinery is the only prerequisite
            // exception — placed completed at match setup, it needs no Power
            // plant and no free power.
            EntityId refinery = f.Construction.PlaceCompletedBuilding(0, 3, 10, 10);
            Assert.That(refinery.IsValid, Is.True);
            Assert.That(f.Entities.GetUnitRef(refinery).Role, Is.EqualTo(UnitRole.Refinery));
            Assert.That(f.Construction.HasFinishedBuilding(0, UnitRole.Refinery), Is.True);

            // ... while the command path still enforces the regular rules.
            Assert.That(f.Construction.ValidatePlacement(0, 3, 20, 20), Is.EqualTo(CommandResultCode.RejectedPrerequisitesNotMet));
        }

        [Test]
        public void SiteProgress_RequiresBuilderInReach_PausesWhenAway()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 2, 40, 40).IsValid, Is.True, "power provider");
            EntityId builder = f.SpawnBuilder(0, 60, 60); // far away
            f.Step(1); // commit the balance
            Assert.That(f.Construction.TryPlaceBuilding(0, 5, 20, 20), Is.True);

            f.Step(20);
            Assert.That(f.Construction.TryGetSite(UnitCommandStateView.ToRawEntityId(SiteEntity(f)), out _, out int progressRaw, out uint assigned),
                Is.True);
            Assert.That(progressRaw, Is.EqualTo(0), "no builder in reach: the site pauses");
            Assert.That(assigned, Is.EqualTo(UnitCommandStateView.ToRawEntityId(builder)),
                "the lowest-index own Builder is auto-assigned at placement");

            // Bring the builder into reach (Chebyshev <= 1 of the footprint).
            f.Entities.GetUnitRef(builder).Transform = new Transform2D(SimFixed.FromInt(19), SimFixed.FromInt(20));
            f.Step(10);
            Assert.That(f.Construction.TryGetSite(UnitCommandStateView.ToRawEntityId(SiteEntity(f)), out _, out progressRaw, out _),
                Is.True);
            Assert.That(progressRaw, Is.EqualTo(10 * SimFixed.OneRaw),
                "full power: exactly one Q16.16 tick of progress per tick");
        }

        [Test]
        public void SiteProgress_LowPower_ExactlyHalvesProgress()
        {
            var f = new Fixture();
            // Low power: a completed Refinery draws 20 with nothing provided.
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 3, 40, 40).IsValid, Is.True);
            f.SpawnBuilder(0, 19, 20);
            Assert.That(f.Construction.TryPlaceBuilding(0, 2, 20, 20), Is.True, "Power plant def 2, 150 ticks");
            f.Step(1);
            Assert.That(f.Economy.GetPlayerEconomy(0).IsLowPower, Is.True);

            uint siteRaw = UnitCommandStateView.ToRawEntityId(SiteEntity(f));
            f.Step(9);
            Assert.That(f.Construction.TryGetSite(siteRaw, out _, out int progressRaw, out _), Is.True);
            Assert.That(progressRaw, Is.EqualTo(10 * (SimFixed.OneRaw / 2)),
                "low power: exactly 0.5 in Q16.16 per tick — no rounding drift");

            f.Step(279); // 289 ticks total: still short of 150 effective
            Assert.That(f.Construction.TryGetSite(siteRaw, out _, out progressRaw, out _), Is.True);
            Assert.That(progressRaw, Is.EqualTo(289 * (SimFixed.OneRaw / 2)));
            Assert.That(f.Entities.GetUnitRef(UnitCommandStateView.ToEntityId(siteRaw)).Role, Is.EqualTo(UnitRole.Unit));

            f.Step(11); // 300 ticks = exactly 150 effective ticks
            Assert.That(f.Entities.GetUnitRef(UnitCommandStateView.ToEntityId(siteRaw)).Role, Is.EqualTo(UnitRole.Power),
                "the plant completes after exactly 300 low-power ticks");
            Assert.That(f.Construction.SiteCount, Is.EqualTo(0));
            Assert.That(f.Construction.BuildingCount, Is.EqualTo(2));
        }

        [Test]
        public void Completion_BecomesRoleEntity_PowerAppliesFromNextTick()
        {
            var f = new Fixture();
            f.SpawnBuilder(0, 19, 20);
            Assert.That(f.Construction.TryPlaceBuilding(0, 2, 20, 20), Is.True);
            uint siteRaw = UnitCommandStateView.ToRawEntityId(SiteEntity(f));

            f.Step(149);
            Assert.That(f.Entities.GetUnitRef(UnitCommandStateView.ToEntityId(siteRaw)).Role, Is.EqualTo(UnitRole.Unit));
            f.Step(1); // tick 150: completion in phase 4
            Assert.That(f.Entities.GetUnitRef(UnitCommandStateView.ToEntityId(siteRaw)).Role, Is.EqualTo(UnitRole.Power));
            Assert.That(f.Entities.GetUnitRef(UnitCommandStateView.ToEntityId(siteRaw)).CurrentHealth, Is.EqualTo(400),
                "completion restores full HP");
            Assert.That(f.Economy.GetPlayerEconomy(0).PowerProvided, Is.EqualTo(0),
                "the economy ran before construction inside the completion tick");
            f.Step(1);
            Assert.That(f.Economy.GetPlayerEconomy(0).PowerProvided, Is.EqualTo(100),
                "power applies from the next economy recompute on");
        }

        [Test]
        public void ResearchLabCompletion_UnlocksT2()
        {
            var f = new Fixture(startingCredits: 3000);
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 2, 40, 40).IsValid, Is.True); // power
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 5, 44, 40).IsValid, Is.True); // barracks prerequisite
            f.SpawnBuilder(0, 19, 20);
            f.Step(1);

            Assert.That(f.Construction.IsT2Unlocked(0), Is.False);
            Assert.That(f.Construction.TryPlaceBuilding(0, 7, 20, 20), Is.True, "ResearchLab def 7");
            f.Step(450);
            Assert.That(f.Construction.IsT2Unlocked(0), Is.True,
                "ResearchLab completion unlocks T2 immediately (mvp-v1.json technology model)");
            Assert.That(f.Construction.IsT2Unlocked(1), Is.False, "the unlock is per slot");
        }

        [Test]
        public void PlaceCompletedBuilding_ResearchLab_UnlocksT2Immediately()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 7, 20, 20).IsValid, Is.True);
            Assert.That(f.Construction.IsT2Unlocked(0), Is.True);
        }

        [Test]
        public void CancelConstruction_Refunds75Percent_AndFreesFootprint()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 2, 40, 40).IsValid, Is.True, "power provider");
            f.SpawnBuilder(0, 19, 20);
            f.Step(1); // commit the balance
            Assert.That(f.Construction.TryPlaceBuilding(0, 5, 20, 20), Is.True); // 500 spent
            uint siteRaw = UnitCommandStateView.ToRawEntityId(SiteEntity(f));

            Assert.That(f.Construction.ValidateCancel(0, siteRaw), Is.EqualTo(CommandResultCode.Applied));
            Assert.That(f.Construction.ValidateCancel(1, siteRaw), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "only the owning slot may cancel");

            Assert.That(f.Construction.CancelConstruction(siteRaw), Is.True);
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(875L),
                "1000 - 500 + 375 (75% floor, provisional)");
            Assert.That(f.Construction.SiteCount, Is.EqualTo(0));
            Assert.That(f.Entities.IsValid(UnitCommandStateView.ToEntityId(siteRaw)), Is.False, "the site entity despawns");
            Assert.That(f.Construction.ValidatePlacement(0, 5, 20, 20), Is.EqualTo(CommandResultCode.Applied),
                "the footprint is free again");
        }

        [Test]
        public void Sell_CompletedBuilding_Refunds50Percent_SiteIsNotSellable()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 2, 40, 40).IsValid, Is.True, "power provider");
            EntityId barracks = f.Construction.PlaceCompletedBuilding(0, 5, 20, 20);
            uint raw = UnitCommandStateView.ToRawEntityId(barracks);

            Assert.That(f.Construction.SellBuilding(raw), Is.True);
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1250L),
                "1000 + 250 (50% floor, provisional)");
            Assert.That(f.Construction.BuildingCount, Is.EqualTo(1), "only the Barracks was sold");
            Assert.That(f.Construction.IsCellFree(20, 20), Is.True);

            f.SpawnBuilder(0, 19, 20);
            f.Step(1); // commit the balance (100 provided, 0 required)
            Assert.That(f.Construction.TryPlaceBuilding(0, 5, 20, 20), Is.True);
            uint siteRaw = UnitCommandStateView.ToRawEntityId(SiteEntity(f));
            Assert.That(f.Construction.ValidateSell(0, siteRaw), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "a site is cancelled, not sold");
        }

        [Test]
        public void Repair_BuilderRestoresHp_InReachOnly_AndResolvesAtFull()
        {
            var f = new Fixture();
            EntityId barracks = f.Construction.PlaceCompletedBuilding(0, 5, 20, 20);
            uint raw = UnitCommandStateView.ToRawEntityId(barracks);
            f.Entities.GetUnitRef(barracks).CurrentHealth = 100;

            EntityId farBuilder = f.SpawnBuilder(0, 60, 60);
            uint farRaw = UnitCommandStateView.ToRawEntityId(farBuilder);
            Assert.That(f.Construction.ValidateRepair(0, new[] { farRaw }, raw), Is.EqualTo(CommandResultCode.Applied),
                "validation checks role and damage, not reach");
            f.Construction.AssignRepairOrder(farRaw, raw);
            f.Step(10);
            Assert.That(f.Entities.GetUnitRef(barracks).CurrentHealth, Is.EqualTo(100),
                "out of reach: the order is held, not dropped");

            f.Entities.GetUnitRef(farBuilder).Transform = new Transform2D(SimFixed.FromInt(19), SimFixed.FromInt(20));
            f.Step(10);
            Assert.That(f.Entities.GetUnitRef(barracks).CurrentHealth, Is.EqualTo(200),
                "10 HP per tick in reach (provisional rate)");
            f.Step(50);
            Assert.That(f.Entities.GetUnitRef(barracks).CurrentHealth, Is.EqualTo(600),
                "repair caps at MaxHealth and the order resolves");
        }

        [Test]
        public void Repair_Validation_RejectsNonBuilder_AndUndamagedTarget()
        {
            var f = new Fixture();
            EntityId barracks = f.Construction.PlaceCompletedBuilding(0, 5, 20, 20);
            uint raw = UnitCommandStateView.ToRawEntityId(barracks);
            EntityId builder = f.SpawnBuilder(0, 19, 20);
            uint builderRaw = UnitCommandStateView.ToRawEntityId(builder);
            EntityId soldier = f.Entities.SpawnUnit(
                0, new Transform2D(SimFixed.FromInt(19), SimFixed.FromInt(21)), SimFixed.FromInt(4), role: UnitRole.BasicInfantry);
            uint soldierRaw = UnitCommandStateView.ToRawEntityId(soldier);

            Assert.That(f.Construction.ValidateRepair(0, new[] { builderRaw }, raw),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget), "the target is undamaged");

            f.Entities.GetUnitRef(barracks).CurrentHealth = 100;
            Assert.That(f.Construction.ValidateRepair(0, new[] { soldierRaw }, raw),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget), "only Builders repair");
            Assert.That(f.Construction.ValidateRepair(1, new[] { builderRaw }, raw),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget), "the builder belongs to slot 0");
        }

        [Test]
        public void DestroyedSite_AbortsWithoutRefund_AndFreesFootprint()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 2, 40, 40).IsValid, Is.True, "power provider");
            f.SpawnBuilder(0, 19, 20);
            f.Step(1); // commit the balance
            Assert.That(f.Construction.TryPlaceBuilding(0, 5, 20, 20), Is.True);
            uint siteRaw = UnitCommandStateView.ToRawEntityId(SiteEntity(f));

            Assert.That(f.Entities.DespawnUnit(UnitCommandStateView.ToEntityId(siteRaw)), Is.True,
                "combat-style kill of the site entity");
            f.Step(1);
            Assert.That(f.Construction.SiteCount, Is.EqualTo(0), "the sweep aborts the dead site");
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(500L), "no refund for a destroyed site");
            Assert.That(f.Construction.IsCellFree(20, 20), Is.True);
        }

        [Test]
        public void Snapshot_Roundtrip_IsByteIdentical_AndTamperingIsRejected()
        {
            var f = new Fixture();
            EntityId builder = f.SpawnBuilder(0, 19, 20);
            Assert.That(f.Construction.TryPlaceBuilding(0, 2, 20, 20), Is.True);
            EntityId barracks = f.Construction.PlaceCompletedBuilding(0, 5, 30, 30);
            f.Entities.GetUnitRef(barracks).CurrentHealth = 100;
            f.Construction.AssignRepairOrder(UnitCommandStateView.ToRawEntityId(builder), UnitCommandStateView.ToRawEntityId(barracks));
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 7, 40, 40).IsValid, Is.True); // T2 flag
            f.Step(10); // accumulate some site progress

            var writer = new SnapshotBlockWriter();
            f.Construction.WriteState(writer);
            byte[] bytes = writer.ToArray();

            var restored = new ConstructionSystem(new EntityManager(64), new EconomySystem(new EntityManager(64)));
            Assert.That(restored.TryValidateState(bytes), Is.True);
            Assert.That(restored.TryRestoreState(bytes), Is.True);

            var rewritten = new SnapshotBlockWriter();
            restored.WriteState(rewritten);
            Assert.That(rewritten.ToArray(), Is.EqualTo(bytes), "serialize -> restore -> serialize is byte-identical");

            // Tampering: unknown definition id.
            byte[] tampered = (byte[])bytes.Clone();
            tampered[2 + 2] = 200; // first site's defId low byte (version, t2, count16, then defId)
            Assert.That(restored.TryValidateState(tampered), Is.False);

            // Trailing bytes are a parse failure.
            var longer = new byte[bytes.Length + 1];
            System.Array.Copy(bytes, longer, bytes.Length);
            Assert.That(restored.TryValidateState(longer), Is.False);
        }

        [Test]
        public void Snapshot_AssignedBuilderRoleViolation_IsRejectedWithoutMutation()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 2, 40, 40).IsValid, Is.True, "power provider");
            f.SpawnBuilder(0, 19, 20);
            EntityId soldier = f.Entities.SpawnUnit(
                0, new Transform2D(SimFixed.FromInt(50), SimFixed.FromInt(50)), SimFixed.FromInt(4),
                role: UnitRole.BasicInfantry);
            f.Step(1);
            Assert.That(f.Construction.TryPlaceBuilding(0, 5, 20, 20), Is.True);

            var writer = new SnapshotBlockWriter();
            f.Construction.WriteState(writer);
            byte[] bytes = writer.ToArray();
            Assert.That(f.Construction.TryValidateState(bytes), Is.True, "the untampered block validates");

            // Tamper: replace the site's assigned builder with the combat
            // unit (offset: version 1 + t2 1 + siteCount 2 + defId 2 +
            // originX 2 + originY 2 + siteEntity 4 = 14, LE uint32).
            byte[] tampered = (byte[])bytes.Clone();
            uint soldierRaw = UnitCommandStateView.ToRawEntityId(soldier);
            tampered[14] = (byte)(soldierRaw & 0xFF);
            tampered[15] = (byte)((soldierRaw >> 8) & 0xFF);
            tampered[16] = (byte)((soldierRaw >> 16) & 0xFF);
            tampered[17] = (byte)((soldierRaw >> 24) & 0xFF);

            Assert.That(f.Construction.TryValidateState(tampered), Is.False,
                "P2-2: a combat unit as assigned builder rejects the block");
            Assert.That(f.Construction.TryRestoreState(tampered), Is.False,
                "restore refuses the tampered block");
            Assert.That(f.Construction.SiteCount, Is.EqualTo(1), "the host is unchanged");
            Assert.That(f.Construction.TryGetSite(
                UnitCommandStateView.ToRawEntityId(SiteEntity(f)), out _, out _, out uint assigned), Is.True);
            Assert.That(assigned, Is.Not.EqualTo(soldierRaw), "the live assignment is unchanged");
        }

        [Test]
        public void ProgressSites_ReassignsNonBuilderAssignment_DefenseInDepth()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 2, 40, 40).IsValid, Is.True, "power provider");
            EntityId builder = f.SpawnBuilder(0, 19, 20);
            f.Step(1);
            Assert.That(f.Construction.TryPlaceBuilding(0, 5, 20, 20), Is.True);
            uint siteRaw = UnitCommandStateView.ToRawEntityId(SiteEntity(f));

            f.Step(10);
            Assert.That(f.Construction.TryGetSite(siteRaw, out _, out int progressRaw, out uint assigned), Is.True);
            Assert.That(progressRaw, Is.EqualTo(10 * SimFixed.OneRaw));
            Assert.That(assigned, Is.EqualTo(UnitCommandStateView.ToRawEntityId(builder)));

            // Defense-in-depth (P2-2): an assignment that no longer names a
            // Builder (here: direct role mutation, standing in for a tampered
            // or stale reference) is dropped and re-resolved like a dead
            // builder — the site pauses instead of letting a combat unit build.
            f.Entities.GetUnitRef(builder).Role = UnitRole.BasicInfantry;
            f.Step(5);
            Assert.That(f.Construction.TryGetSite(siteRaw, out _, out int pausedProgress, out assigned), Is.True);
            Assert.That(assigned, Is.EqualTo(0u), "no own Builder exists to re-assign");
            Assert.That(pausedProgress, Is.EqualTo(10 * SimFixed.OneRaw),
                "the site pauses — the non-builder never progressed it");
        }

        /// <summary>Returns the single active site entity of the fixture.</summary>
        private static EntityId SiteEntity(Fixture f)
        {
            UnitState[] units = f.Entities.RawUnits;
            for (int i = 0; i < f.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].Role == UnitRole.Unit)
                {
                    return units[i].Id;
                }
            }
            throw new System.InvalidOperationException("no site entity found");
        }
    }
}
