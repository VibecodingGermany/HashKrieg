using System;
using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Economy;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Canonical economy suite (EditMode lane): per-slot credits and power
    /// (SimulationCore.md section 2, phase 2), the finite Aetherium harvest
    /// cycle (phase 3) and the snapshot block 104 v1 contract. G2
    /// reservation: no D-010 regrowth/spread/overharvest — fields are finite
    /// and stay exhausted.
    /// Mirror of the .NET lane EconomySystemTests.
    /// </summary>
    [TestFixture]
    public class EconomySystemTests
    {
        private static EntityManager CreateEntities() => new EntityManager(64);

        private static EntityId SpawnHarvester(EntityManager entities, byte player, int x, int y)
        {
            return entities.SpawnUnit(
                player,
                new Transform2D(SimFixed.FromInt(x), SimFixed.FromInt(y)),
                SimFixed.FromInt(4),
                role: UnitRole.Harvester);
        }

        [Test]
        public void StartConditions_AreCanonicalManifestValues()
        {
            var economy = new EconomySystem(CreateEntities());
            for (byte slot = 0; slot < EconomySystem.MaxPlayers; slot++)
            {
                Assert.That(economy.GetPlayerEconomy(slot).AetheriumCredits, Is.EqualTo(1000L),
                    "startStatePerPlayer.aetheriumAE of quality/content/mvp-v1.json");
            }
        }

        [Test]
        public void Credits_NeverGoNegative_SpendingIsAtomic()
        {
            var economy = new EconomySystem(CreateEntities());
            ref PlayerEconomyState eco = ref economy.GetPlayerEconomy(0);

            Assert.That(eco.TrySpendCredits(2000), Is.False, "overspending must be refused");
            Assert.That(eco.AetheriumCredits, Is.EqualTo(1000L), "a refused spend mutates nothing");

            Assert.That(eco.TrySpendCredits(1000), Is.True);
            Assert.That(eco.AetheriumCredits, Is.EqualTo(0L));
            Assert.That(eco.TrySpendCredits(1), Is.False);
            Assert.That(eco.AetheriumCredits, Is.EqualTo(0L), "the balance can never go negative");

            eco.AddCredits(330);
            Assert.That(eco.AetheriumCredits, Is.EqualTo(330L));
        }

        [Test]
        public void LowPowerMultiplier_IsExactQ16Half()
        {
            var eco = new PlayerEconomyState(0)
            {
                PowerProvided = 0,
                PowerRequired = 1,
            };
            Assert.That(eco.IsLowPower, Is.True);
            Assert.That(eco.ProductionSpeedMultiplierQ16.RawValue, Is.EqualTo(32768),
                "0.5 is exact in Q16.16 — no float relic");

            eco.PowerProvided = 1;
            Assert.That(eco.IsLowPower, Is.False);
            Assert.That(eco.ProductionSpeedMultiplierQ16.RawValue, Is.EqualTo(SimFixed.OneRaw));
        }

        [Test]
        public void PowerRecompute_DerivesFromBuildingRoles_AndDropsOnDespawn()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            kernel.Start();

            EntityId hq = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(5), SimFixed.FromInt(5)), SimFixed.Zero, role: UnitRole.HQ);
            EntityId plant = entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(8), SimFixed.FromInt(5)), SimFixed.Zero, role: UnitRole.Power);
            entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(11), SimFixed.FromInt(5)), SimFixed.Zero, role: UnitRole.Refinery);

            kernel.StepTick();
            Assert.That(economy.GetPlayerEconomy(0).PowerProvided, Is.EqualTo(130), "HQ 30 + plant 100 (provisional)");
            Assert.That(economy.GetPlayerEconomy(0).PowerRequired, Is.EqualTo(20), "refinery 20 (provisional)");
            Assert.That(economy.GetPlayerEconomy(0).IsLowPower, Is.False);

            // Combat-style despawn of the power plant: the next recompute
            // reflects the loss deterministically.
            entities.DespawnUnit(plant);
            kernel.StepTick();
            Assert.That(economy.GetPlayerEconomy(0).PowerProvided, Is.EqualTo(30));
            Assert.That(economy.GetPlayerEconomy(0).IsLowPower, Is.False);

            entities.DespawnUnit(hq);
            kernel.StepTick();
            Assert.That(economy.GetPlayerEconomy(0).PowerProvided, Is.EqualTo(0));
            Assert.That(economy.GetPlayerEconomy(0).IsLowPower, Is.True);
            Assert.That(economy.GetPlayerEconomy(0).ProductionSpeedMultiplierQ16.RawValue, Is.EqualTo(32768));
        }

        [Test]
        public void PowerRecompute_IsFactionResolved_LegionPowerPlantProvides80()
        {
            // The same building ROLE feeds different power depending on the
            // owner slot's faction (Buildings.md section 2: Alliance 100,
            // Legion 80) — the recompute resolves (faction, role), not role.
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            economy.SetSlotFaction(1, FactionId.Legion); // before Start — the guard requires it
            kernel.Start();

            entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(5), SimFixed.FromInt(5)), SimFixed.Zero, role: UnitRole.Power);
            entities.SpawnUnit(1, new Transform2D(SimFixed.FromInt(8), SimFixed.FromInt(5)), SimFixed.Zero, role: UnitRole.Power);
            entities.SpawnUnit(1, new Transform2D(SimFixed.FromInt(11), SimFixed.FromInt(5)), SimFixed.Zero, role: UnitRole.Barracks);

            kernel.StepTick();
            Assert.That(economy.GetPlayerEconomy(0).PowerProvided, Is.EqualTo(100), "Alliance plant");
            Assert.That(economy.GetPlayerEconomy(1).PowerProvided, Is.EqualTo(80), "Legion plant");
            Assert.That(economy.GetPlayerEconomy(1).PowerRequired, Is.EqualTo(10), "Legion Barracks draws 10");
        }

        [Test]
        public void HarvestCycle_GathersExactRate_AndDepositRaisesCreditsExactly()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);

            EntityId harvester = SpawnHarvester(entities, 0, 10, 10);
            entities.GetUnitRef(harvester).HarvestFieldId = 1;

            for (int i = 0; i < 10; i++)
            {
                kernel.StepTick();
            }
            Assert.That(entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(20), "exactly 2 AE per tick");
            Assert.That(economy.TryGetField(1, out AetheriumField field), Is.True);
            Assert.That(field.RemainingAE, Is.EqualTo(8980L), "the field reserve sinks exactly");

            // Deposit at an own refinery in reach (adjacent cell).
            entities.SpawnUnit(0, new Transform2D(SimFixed.FromInt(11), SimFixed.FromInt(10)), SimFixed.Zero, role: UnitRole.Refinery);
            entities.GetUnitRef(harvester).HarvestFieldId = 0;
            entities.GetUnitRef(harvester).IsReturningCargo = true;

            kernel.StepTick();
            Assert.That(entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(0));
            Assert.That(entities.GetUnitRef(harvester).IsReturningCargo, Is.False, "the deposit resolves the order");
            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1020L),
                "credits rise by exactly the cargo");
        }

        [Test]
        public void Harvest_StopsAtCapacity_AndStartsTheReturnLeg()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);

            EntityId harvester = SpawnHarvester(entities, 0, 10, 10);
            ref UnitState unit = ref entities.GetUnitRef(harvester);
            unit.HarvestFieldId = 1;
            unit.CargoAE = UnitState.DefaultCargoCapacityAE - 1; // 329 of 330

            kernel.StepTick();
            unit = ref entities.GetUnitRef(harvester);
            Assert.That(unit.CargoAE, Is.EqualTo(UnitState.DefaultCargoCapacityAE),
                "only the free cargo space is gathered");
            Assert.That(unit.HarvestFieldId, Is.EqualTo((ushort)1), "the field id is retained for the auto-cycle");
            Assert.That(unit.IsReturningCargo, Is.True, "a full cargo starts the return leg");
            Assert.That(economy.TryGetField(1, out AetheriumField field), Is.True);
            Assert.That(field.RemainingAE, Is.EqualTo(8999L));

            kernel.StepTick();
            Assert.That(entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(UnitState.DefaultCargoCapacityAE),
                "no further gathering while the return leg holds without a refinery in reach");
        }

        [Test]
        public void FiniteField_CollectsOnlyRemainder_ThenStaysExhausted()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 3), Is.True);

            EntityId harvester = SpawnHarvester(entities, 0, 10, 10);
            entities.GetUnitRef(harvester).HarvestFieldId = 1;

            kernel.StepTick(); // gathers 2, remainder 1
            kernel.StepTick(); // gathers the last 1, field exhausted, order resolves
            Assert.That(entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(3),
                "a field with less than the rate left yields only the remainder");
            Assert.That(entities.GetUnitRef(harvester).HarvestFieldId, Is.EqualTo((ushort)0),
                "exhaustion resolves the order — the harvester goes idle");
            Assert.That(economy.TryGetField(1, out AetheriumField field), Is.True);
            Assert.That(field.RemainingAE, Is.EqualTo(0L));
            Assert.That(field.IsExhausted, Is.True);

            kernel.StepTick(); // G2 reservation: no regrowth — the field stays exhausted
            Assert.That(entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(3));
            Assert.That(economy.TryGetField(1, out field), Is.True);
            Assert.That(field.RemainingAE, Is.EqualTo(0L));
        }

        [Test]
        public void HarvestOrder_OutOfReach_IsHeldNotDropped()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);

            EntityId harvester = SpawnHarvester(entities, 0, 20, 20); // far away
            entities.GetUnitRef(harvester).HarvestFieldId = 1;

            kernel.StepTick();
            Assert.That(entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(0));
            Assert.That(entities.GetUnitRef(harvester).HarvestFieldId, Is.EqualTo((ushort)1),
                "out-of-reach orders are held — closing the distance is Movement's concern");
        }

        [Test]
        public void HarvestOrder_OnNonHarvesterRole_IsIneffective()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            kernel.Start();
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);

            EntityId soldier = entities.SpawnUnit(
                0, new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)), SimFixed.FromInt(4));
            entities.GetUnitRef(soldier).HarvestFieldId = 1;

            kernel.StepTick();
            Assert.That(entities.GetUnitRef(soldier).CargoAE, Is.EqualTo(0),
                "harvest orders apply to the Harvester role only (documented provisional rule)");
        }

        [Test]
        public void ReturnOrder_WithoutRefineryInReach_IsHeld()
        {
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);
            kernel.Start();

            EntityId harvester = SpawnHarvester(entities, 0, 10, 10);
            ref UnitState unit = ref entities.GetUnitRef(harvester);
            unit.CargoAE = 50;
            unit.IsReturningCargo = true;

            // A foreign refinery in reach does not count.
            entities.SpawnUnit(1, new Transform2D(SimFixed.FromInt(11), SimFixed.FromInt(10)), SimFixed.Zero, role: UnitRole.Refinery);

            kernel.StepTick();
            unit = ref entities.GetUnitRef(harvester);
            Assert.That(unit.CargoAE, Is.EqualTo(50));
            Assert.That(unit.IsReturningCargo, Is.True, "no own refinery in reach: the order holds");
            Assert.That(economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1000L));
        }

        [Test]
        public void TryAddField_ValidatesIdentityAndReserve()
        {
            var economy = new EconomySystem(CreateEntities());
            Assert.That(economy.TryAddField(0, new GridPos2D(1, 1), 9000), Is.False, "id 0 is invalid");
            Assert.That(economy.TryAddField(1, new GridPos2D(1, 1), 0), Is.False, "the reserve must be positive");
            Assert.That(economy.TryAddField(1, GridPos2D.Invalid, 9000), Is.False, "the position must be valid");
            Assert.That(economy.TryAddField(1, new GridPos2D(1, 1), 9000), Is.True);
            Assert.That(economy.TryAddField(1, new GridPos2D(2, 2), 9000), Is.False, "duplicate id");
            Assert.That(economy.FieldCount, Is.EqualTo(1));
        }

        private static byte[] SerializeBlock(EconomySystem economy)
        {
            var writer = new SnapshotBlockWriter();
            economy.WriteState(writer);
            return writer.ToArray();
        }

        [Test]
        public void Block104_RoundtripsByteIdentical_AndRestoresExactState()
        {
            EntityManager entities = CreateEntities();
            var economy = new EconomySystem(entities);
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);
            Assert.That(economy.TryAddField(2, new GridPos2D(50, 50), 15000), Is.True);
            economy.GetPlayerEconomy(0).AddCredits(330);
            economy.GetPlayerEconomy(1).PowerProvided = 30;
            economy.GetPlayerEconomy(1).PowerRequired = 20;

            byte[] bytes = SerializeBlock(economy);

            var restored = new EconomySystem(CreateEntities());
            Assert.That(restored.TryValidateState(bytes), Is.True);
            Assert.That(restored.TryRestoreState(bytes), Is.True);
            Assert.That(SerializeBlock(restored), Is.EqualTo(bytes), "restore -> serialize must be byte-identical");

            Assert.That(restored.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1330L));
            Assert.That(restored.GetPlayerEconomy(1).PowerProvided, Is.EqualTo(30));
            Assert.That(restored.TryGetField(2, out AetheriumField field), Is.True);
            Assert.That(field.GridPos, Is.EqualTo(new GridPos2D(50, 50)));
            Assert.That(field.RemainingAE, Is.EqualTo(15000L));
        }

        [Test]
        public void Block104_RejectsNegativeCreditsReserveAndDuplicateFields_WithoutMutating()
        {
            EntityManager entities = CreateEntities();
            var economy = new EconomySystem(entities);
            Assert.That(economy.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);
            byte[] valid = SerializeBlock(economy);

            // Layout v2: version(1) + 8 slots x (i64 + i32 + i32 + u8) = 1 + 136
            // bytes of slot state, then fieldCount u16, then the field record.
            byte[] negativeCredits = (byte[])valid.Clone();
            negativeCredits[8] = 0xFF; // slot 0 credits: highest byte -> negative
            byte[] negativeReserve = (byte[])valid.Clone();
            negativeReserve[negativeReserve.Length - 1] = 0xFF; // reserve i64: highest byte -> negative

            foreach (byte[] tampered in new[] { negativeCredits, negativeReserve })
            {
                var victim = new EconomySystem(CreateEntities());
                Assert.That(victim.TryValidateState(tampered), Is.False);
                Assert.That(victim.TryRestoreState(tampered), Is.False);
                Assert.That(victim.FieldCount, Is.EqualTo(0), "a rejected restore must not mutate the system");
                Assert.That(victim.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1000L));
            }

            // Duplicate field ids are rejected.
            var economy2 = new EconomySystem(CreateEntities());
            Assert.That(economy2.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);
            Assert.That(economy2.TryAddField(2, new GridPos2D(50, 50), 15000), Is.True);
            byte[] twoFields = SerializeBlock(economy2);
            byte[] duplicate = (byte[])twoFields.Clone();
            int secondFieldIdOffset = 1 + EconomySystem.MaxPlayers * 17 + 2 + 14; // second field record starts with its id
            duplicate[secondFieldIdOffset] = 1;
            duplicate[secondFieldIdOffset + 1] = 0;
            var victim2 = new EconomySystem(CreateEntities());
            Assert.That(victim2.TryValidateState(duplicate), Is.False, "duplicate field id must fail validation");
        }

        [Test]
        public void Block104_SingleCreditChange_ChangesBlockBytes()
        {
            var economy = new EconomySystem(CreateEntities());
            byte[] before = SerializeBlock(economy);
            economy.GetPlayerEconomy(0).AddCredits(1);
            byte[] after = SerializeBlock(economy);
            Assert.That(after, Is.Not.EqualTo(before),
                "one AE of credits must move the block bytes and therefore the canonical state hash");
        }

        // ----------------------------------------------------------------
        // Faction axis (economy block v2)
        // ----------------------------------------------------------------

        [Test]
        public void SlotFaction_DefaultsToAlliance_OnEverySlot()
        {
            var economy = new EconomySystem(CreateEntities());
            for (byte slot = 0; slot < EconomySystem.MaxPlayers; slot++)
            {
                Assert.That(economy.GetSlotFaction(slot), Is.EqualTo(FactionId.Alliance));
                Assert.That(economy.GetPlayerEconomy(slot).Faction, Is.EqualTo(FactionId.Alliance));
            }
        }

        [Test]
        public void SetSlotFaction_AssignsAndReadsBack_ValidatesInput()
        {
            var economy = new EconomySystem(CreateEntities());
            economy.SetSlotFaction(0, FactionId.Alliance);
            economy.SetSlotFaction(1, FactionId.Legion);

            Assert.That(economy.GetSlotFaction(0), Is.EqualTo(FactionId.Alliance));
            Assert.That(economy.GetSlotFaction(1), Is.EqualTo(FactionId.Legion));

            Assert.Throws<ArgumentOutOfRangeException>(() => economy.SetSlotFaction(8, FactionId.Legion));
            Assert.Throws<ArgumentOutOfRangeException>(() => economy.SetSlotFaction(0, (FactionId)2));
            Assert.Throws<ArgumentOutOfRangeException>(() => economy.GetSlotFaction(8));
        }

        [Test]
        public void SetSlotFaction_AfterKernelStart_ThrowsAndLeavesStateUntouched()
        {
            // The faction is part of the hashed initial state and the match
            // fingerprint: once the kernel this economy is registered with
            // has started, the assignment window is closed for good.
            EntityManager entities = CreateEntities();
            var kernel = new SimulationKernel(new SimRandom(42UL));
            var economy = new EconomySystem(entities);
            kernel.RegisterSystem(economy);

            economy.SetSlotFaction(1, FactionId.Legion); // legal before Start
            kernel.Start();

            Assert.Throws<InvalidOperationException>(() => economy.SetSlotFaction(1, FactionId.Alliance),
                "after Start the faction is locked, even at tick zero");
            Assert.Throws<InvalidOperationException>(() => economy.SetSlotFaction(0, FactionId.Legion));
            Assert.That(economy.GetSlotFaction(1), Is.EqualTo(FactionId.Legion),
                "a rejected call mutates nothing");
            Assert.That(economy.GetSlotFaction(0), Is.EqualTo(FactionId.Alliance));

            kernel.StepTick();
            Assert.Throws<InvalidOperationException>(() => economy.SetSlotFaction(1, FactionId.Alliance),
                "and stays locked once ticks have run");
            Assert.That(economy.GetSlotFaction(1), Is.EqualTo(FactionId.Legion));
        }

        [Test]
        public void Block104_Roundtrip_PreservesTheSlotFaction()
        {
            var economy = new EconomySystem(CreateEntities());
            economy.SetSlotFaction(1, FactionId.Legion);
            byte[] bytes = SerializeBlock(economy);

            var restored = new EconomySystem(CreateEntities());
            Assert.That(restored.TryValidateState(bytes), Is.True);
            Assert.That(restored.TryRestoreState(bytes), Is.True);
            Assert.That(restored.GetSlotFaction(1), Is.EqualTo(FactionId.Legion));
            Assert.That(restored.GetSlotFaction(0), Is.EqualTo(FactionId.Alliance));
            Assert.That(SerializeBlock(restored), Is.EqualTo(bytes),
                "the faction byte must roundtrip byte-identical");
        }

        [Test]
        public void Block104_RejectsUndefinedFactionBytes_AndTheRetiredV1Layout()
        {
            var economy = new EconomySystem(CreateEntities());
            economy.SetSlotFaction(1, FactionId.Legion);
            byte[] valid = SerializeBlock(economy);

            // Faction byte of slot 0 sits right after its i64 + i32 + i32.
            byte[] badFaction = (byte[])valid.Clone();
            badFaction[1 + 16] = 2;
            var victim = new EconomySystem(CreateEntities());
            Assert.That(victim.TryValidateState(badFaction), Is.False, "faction 2 is not declared");
            Assert.That(victim.TryRestoreState(badFaction), Is.False);
            Assert.That(victim.GetSlotFaction(0), Is.EqualTo(FactionId.Alliance),
                "a rejected restore must not mutate the system");

            // The retired v1 layout (no faction bytes) is rejected, not migrated.
            var writer = new SnapshotBlockWriter();
            writer.WriteUInt8(1);
            for (int p = 0; p < EconomySystem.MaxPlayers; p++)
            {
                writer.WriteInt64(1000);
                writer.WriteInt32(0);
                writer.WriteInt32(0);
            }
            writer.WriteUInt16(0);
            byte[] v1Block = writer.ToArray();
            var legacy = new EconomySystem(CreateEntities());
            Assert.That(legacy.TryValidateState(v1Block), Is.False,
                "v1 blocks predate the faction axis and are refused outright");
            Assert.That(legacy.TryRestoreState(v1Block), Is.False);
        }

        [Test]
        public void Block104_FactionChange_ChangesBlockBytes()
        {
            var economy = new EconomySystem(CreateEntities());
            byte[] before = SerializeBlock(economy);
            economy.SetSlotFaction(1, FactionId.Legion);
            byte[] after = SerializeBlock(economy);
            Assert.That(after, Is.Not.EqualTo(before),
                "the faction assignment must move the block bytes and therefore the initial state hash");
        }
    }
}
