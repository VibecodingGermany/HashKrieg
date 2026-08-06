using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Production;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Canonical production suite (EditMode lane): QueueUnit cost and validation
    /// (producer assignment, T2 gating, queue capacity, count-scaled
    /// affordability), exact Q16.16 progress with the low-power halving,
    /// deterministic rally-point spawning, CancelProduction refunds, the
    /// entity-store-cap pause and the snapshot block 106 v1 contract. All
    /// values are documented Q-040 provisionals of SimDefinitions. MS-1 has
    /// no research tree: the retired ResearchTreeSystem scaffolding is
    /// deliberately gone (mvp-v1.json technology model).
    /// Mirror of the .NET lane ProductionSystemTests.
    /// </summary>
    [TestFixture]
    public class ProductionSystemTests
    {
        private sealed class Fixture
        {
            public EntityManager Entities { get; }
            public EconomySystem Economy { get; }
            public ConstructionSystem Construction { get; }
            public ProductionSystem Production { get; }
            public SimulationKernel Kernel { get; }

            public Fixture(long startingCredits = 1000, int capacity = 64, System.Action<EconomySystem> configure = null)
            {
                Entities = new EntityManager(capacity);
                Economy = new EconomySystem(Entities, startingCredits);
                Construction = new ConstructionSystem(Entities, Economy);
                Production = new ProductionSystem(Entities, Economy, Construction);
                Kernel = new SimulationKernel(new SimRandom(42UL));
                Kernel.RegisterSystem(Economy);
                Kernel.RegisterSystem(Construction);
                Kernel.RegisterSystem(Production);
                // Pre-start configuration hook (e.g. slot factions): the
                // SetSlotFaction guard locks the assignment at Kernel.Start().
                configure?.Invoke(Economy);
                Kernel.Start();
            }

            /// <summary>
            /// Places a completed Barracks at (10,10) and returns its raw wire
            /// id. Also places a completed Power plant at (40,40) unless
            /// <paramref name="withPower"/> is false — a Barracks draws 15,
            /// so a powered grid keeps production at full speed.
            /// </summary>
            public uint SpawnBarracks(byte slot, bool withPower = true)
            {
                if (withPower)
                {
                    Assert.That(Construction.PlaceCompletedBuilding(slot, 5, 40, 40).IsValid, Is.True);
                }
                EntityId id = Construction.PlaceCompletedBuilding(slot, 7, 10, 10);
                Assert.That(id.IsValid, Is.True);
                return UnitCommandStateView.ToRawEntityId(id);
            }

            public void Step(int ticks)
            {
                for (int i = 0; i < ticks; i++) Kernel.StepTick();
            }
        }

        [Test]
        public void QueueUnit_ChargesCostTimesCount_AtEnqueue()
        {
            var f = new Fixture();
            uint barracks = f.SpawnBarracks(0);

            Assert.That(f.Production.TryQueueUnit(0, barracks, 12, 3), Is.True, "BasicInfantry x3");
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(640L),
                "3 x 120 AE charged in full at enqueue");
            Assert.That(f.Production.TotalQueuedUnits, Is.EqualTo(3));
            Assert.That(f.Production.TryGetQueueEntry(barracks, 0, out ushort defId, out ushort remaining, out int progress), Is.True);
            Assert.That(defId, Is.EqualTo((ushort)12));
            Assert.That(remaining, Is.EqualTo((ushort)3));
            Assert.That(progress, Is.EqualTo(0));
        }

        [Test]
        public void QueueUnit_T2Gating_RejectsBeforeUnlock_AcceptsAfter()
        {
            var f = new Fixture();
            uint barracks = f.SpawnBarracks(0);

            Assert.That(f.Production.ValidateQueueUnit(0, barracks, 13, 1), Is.EqualTo(CommandResultCode.RejectedPrerequisitesNotMet),
                "AntiArmorInfantry is T2 and the slot has no completed ResearchLab");
            Assert.That(f.Production.TryQueueUnit(0, barracks, 13, 1), Is.False);
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1000L), "a rejection charges nothing");

            Assert.That(f.Construction.PlaceCompletedBuilding(0, 9, 20, 20).IsValid, Is.True, "ResearchLab completion unlocks T2");
            Assert.That(f.Production.ValidateQueueUnit(0, barracks, 13, 1), Is.EqualTo(CommandResultCode.Applied));
            Assert.That(f.Production.TryQueueUnit(0, barracks, 13, 1), Is.True);
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(750L));
        }

        [Test]
        public void QueueUnit_WrongProducer_IsRejectedInvalidTarget()
        {
            var f = new Fixture();
            EntityId hq = f.Construction.PlaceCompletedBuilding(0, 3, 50, 50);
            uint hqRaw = UnitCommandStateView.ToRawEntityId(hq);
            uint barracks = f.SpawnBarracks(0);

            Assert.That(f.Production.ValidateQueueUnit(0, barracks, 1, 1), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "the Barracks does not produce Builders");
            Assert.That(f.Production.ValidateQueueUnit(0, hqRaw, 1, 1), Is.EqualTo(CommandResultCode.Applied),
                "the HQ produces the Builder (documented assignment)");
            Assert.That(f.Production.ValidateQueueUnit(1, hqRaw, 1, 1), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "the HQ belongs to slot 0");
        }

        [Test]
        public void QueueUnit_Harvester_IsProducedByTheRefinery_NotTheHq()
        {
            // D-077 producer assignment: the Refinery produces the Harvester
            // in both factions; the HQ lost it (it keeps the Builder).
            var f = new Fixture();
            EntityId hq = f.Construction.PlaceCompletedBuilding(0, 3, 50, 50);
            uint hqRaw = UnitCommandStateView.ToRawEntityId(hq);
            EntityId refinery = f.Construction.PlaceCompletedBuilding(0, 4, 20, 20);
            uint refineryRaw = UnitCommandStateView.ToRawEntityId(refinery);

            Assert.That(f.Production.ValidateQueueUnit(0, hqRaw, 2, 1), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "the HQ no longer produces the Harvester (D-077)");
            Assert.That(f.Production.ValidateQueueUnit(0, refineryRaw, 2, 1), Is.EqualTo(CommandResultCode.Applied),
                "the Refinery does (D-077)");
        }

        [Test]
        public void QueueUnit_SixthEntry_IsRejectedPrerequisitesNotMet()
        {
            var f = new Fixture();
            uint barracks = f.SpawnBarracks(0);

            for (int i = 0; i < ProductionSystem.MaxQueueEntries; i++)
            {
                Assert.That(f.Production.TryQueueUnit(0, barracks, 12, 1), Is.True, $"entry {i + 1}");
            }
            Assert.That(f.Production.ValidateQueueUnit(0, barracks, 12, 1), Is.EqualTo(CommandResultCode.RejectedPrerequisitesNotMet),
                "provisional queue capacity is 5 entries per building");
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(400L), "the rejected sixth entry charges nothing");
        }

        [Test]
        public void QueueUnit_InsufficientFunds_IsRejectedInsufficientResources()
        {
            var f = new Fixture(startingCredits: 150);
            uint barracks = f.SpawnBarracks(0);

            Assert.That(f.Production.ValidateQueueUnit(0, barracks, 12, 2), Is.EqualTo(CommandResultCode.RejectedInsufficientResources),
                "2 x 120 AE exceeds the 150 AE balance — the count-scaled cost is a domain check");
            Assert.That(f.Production.TryQueueUnit(0, barracks, 12, 1), Is.True);
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(30L));
        }

        [Test]
        public void Production_SpawnsAtDefaultRally_AfterExactBuildTicks()
        {
            var f = new Fixture();
            uint barracks = f.SpawnBarracks(0); // center cell (11,11) -> default rally (13,11)
            Assert.That(f.Production.TryQueueUnit(0, barracks, 12, 1), Is.True);

            f.Step(99);
            Assert.That(CountRole(f, UnitRole.BasicInfantry), Is.EqualTo(0), "one tick short of 100");
            f.Step(1);
            Assert.That(CountRole(f, UnitRole.BasicInfantry), Is.EqualTo(1), "spawned after exactly 100 full-power ticks");

            EntityId unit = FindRole(f, UnitRole.BasicInfantry);
            Assert.That(f.Entities.GetUnitRef(unit).Transform.PositionX, Is.EqualTo(SimFixed.FromInt(13)));
            Assert.That(f.Entities.GetUnitRef(unit).Transform.PositionY, Is.EqualTo(SimFixed.FromInt(11)));
            Assert.That(f.Entities.GetUnitRef(unit).MaxHealth, Is.EqualTo(90));
            Assert.That(f.Production.TotalQueuedUnits, Is.EqualTo(0));
        }

        [Test]
        public void Production_LowPower_ExactlyDoublesDuration()
        {
            var f = new Fixture();
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 4, 40, 40).IsValid, Is.True,
                "a completed Refinery (20 required, nothing provided) forces low power");
            uint barracks = f.SpawnBarracks(0, withPower: false);
            Assert.That(f.Production.TryQueueUnit(0, barracks, 12, 1), Is.True);
            f.Step(1);
            Assert.That(f.Economy.GetPlayerEconomy(0).IsLowPower, Is.True);

            f.Step(99); // 100 ticks: 50 effective — not done
            Assert.That(CountRole(f, UnitRole.BasicInfantry), Is.EqualTo(0));
            Assert.That(f.Production.TryGetQueueEntry(barracks, 0, out _, out _, out int progress), Is.True);
            Assert.That(progress, Is.EqualTo(100 * (SimFixed.OneRaw / 2)),
                "exactly 0.5 in Q16.16 per tick — no rounding drift");

            f.Step(100); // 200 ticks: 100 effective — done
            Assert.That(CountRole(f, UnitRole.BasicInfantry), Is.EqualTo(1));
        }

        [Test]
        public void SetRallyPoint_MovesTheSpawnLocation()
        {
            var f = new Fixture();
            uint barracks = f.SpawnBarracks(0);
            f.Production.SetRallyPoint(barracks, SimFixed.FromInt(30), SimFixed.FromInt(30));
            Assert.That(f.Production.TryGetProducer(barracks, out _, out int rallyX, out int rallyY), Is.True);
            Assert.That(rallyX, Is.EqualTo(SimFixed.FromInt(30).RawValue));
            Assert.That(rallyY, Is.EqualTo(SimFixed.FromInt(30).RawValue));

            Assert.That(f.Production.TryQueueUnit(0, barracks, 12, 1), Is.True);
            f.Step(100);
            EntityId unit = FindRole(f, UnitRole.BasicInfantry);
            Assert.That(f.Entities.GetUnitRef(unit).Transform.PositionX, Is.EqualTo(SimFixed.FromInt(30)));
            Assert.That(f.Entities.GetUnitRef(unit).Transform.PositionY, Is.EqualTo(SimFixed.FromInt(30)));
        }

        [Test]
        public void SetRallyPoint_Accepted_OnRefinery()
        {
            // D-077 regression: the Harvester moved to the Refinery, but the
            // producer-role check predated the move and rejected rally points
            // there. Producer roles are read from the definition table now.
            var f = new Fixture();
            EntityId id = f.Construction.PlaceCompletedBuilding(0, 4, 10, 10); // Alliance Refinery
            Assert.That(id.IsValid, Is.True);
            uint refinery = UnitCommandStateView.ToRawEntityId(id);

            Assert.That(f.Production.ValidateSetRallyPoint(0, refinery, SimFixed.FromInt(30), SimFixed.FromInt(30)),
                Is.EqualTo(CommandResultCode.Applied));
            f.Production.SetRallyPoint(refinery, SimFixed.FromInt(30), SimFixed.FromInt(30));
            Assert.That(f.Production.TryGetProducer(refinery, out _, out int rallyX, out int rallyY), Is.True);
            Assert.That(rallyX, Is.EqualTo(SimFixed.FromInt(30).RawValue));
            Assert.That(rallyY, Is.EqualTo(SimFixed.FromInt(30).RawValue));
        }

        [Test]
        public void SpawnSearch_SkipsOccupiedCells_Deterministically()
        {
            var f = new Fixture();
            uint barracks = f.SpawnBarracks(0); // default rally (13,11)
            // Occupy the rally cell with a completed Storage at (13,11).
            Assert.That(f.Construction.PlaceCompletedBuilding(0, 6, 13, 11).IsValid, Is.True);

            Assert.That(f.Production.TryQueueUnit(0, barracks, 12, 1), Is.True);
            f.Step(100);

            // Ring-1 scan in ascending (y, x): (12,10) is Barracks footprint,
            // (13,10) is the first free cell.
            EntityId unit = FindRole(f, UnitRole.BasicInfantry);
            Assert.That(f.Entities.GetUnitRef(unit).Transform.PositionX, Is.EqualTo(SimFixed.FromInt(13)));
            Assert.That(f.Entities.GetUnitRef(unit).Transform.PositionY, Is.EqualTo(SimFixed.FromInt(10)),
                "the documented ring scan skips occupied cells deterministically");
        }

        [Test]
        public void CancelProduction_RunningEntry_RefundsRemainingCount()
        {
            var f = new Fixture();
            uint barracks = f.SpawnBarracks(0);
            Assert.That(f.Production.TryQueueUnit(0, barracks, 12, 3), Is.True); // 360 spent

            f.Step(100); // one infantry spawned
            Assert.That(CountRole(f, UnitRole.BasicInfantry), Is.EqualTo(1));

            Assert.That(f.Production.CancelProduction(barracks, 0), Is.True);
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(880L),
                "1000 - 360 + 240: the running entry refunds its REMAINING count in full");
            Assert.That(f.Production.TotalQueuedUnits, Is.EqualTo(0));
        }

        [Test]
        public void CancelProduction_QueuedEntry_FullRefund_RunningEntryUntouched()
        {
            var f = new Fixture();
            uint barracks = f.SpawnBarracks(0);
            Assert.That(f.Production.TryQueueUnit(0, barracks, 12, 1), Is.True); // entry 0: 120
            Assert.That(f.Production.TryQueueUnit(0, barracks, 12, 2), Is.True); // entry 1: 240

            Assert.That(f.Production.ValidateCancelProduction(0, barracks, 2), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "no entry at index 2");
            Assert.That(f.Production.CancelProduction(barracks, 1), Is.True);
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(880L),
                "1000 - 360 + 240: the unstarted entry refunds in full, the cancel itself is free");
            Assert.That(f.Production.TryGetQueueEntry(barracks, 0, out _, out ushort remaining, out _), Is.True);
            Assert.That(remaining, Is.EqualTo((ushort)1), "the running entry is untouched");
        }

        [Test]
        public void EntityStoreFull_QueuePauses_ResumesAfterSpace()
        {
            var f = new Fixture(capacity: 8);
            uint barracks = f.SpawnBarracks(0);
            Assert.That(f.Production.TryQueueUnit(0, barracks, 12, 1), Is.True);

            // Fill the store to capacity.
            var dummies = new System.Collections.Generic.List<EntityId>();
            while (f.Entities.ActiveCount < f.Entities.Capacity)
            {
                dummies.Add(f.Entities.SpawnUnit(
                    0, new Transform2D(SimFixed.FromInt(50), SimFixed.FromInt(50)), SimFixed.Zero));
            }

            f.Step(150); // long past the 100 build ticks
            Assert.That(CountRole(f, UnitRole.BasicInfantry), Is.EqualTo(0), "no spawn while the store is full");
            Assert.That(f.Production.TryGetQueueEntry(barracks, 0, out _, out ushort remaining, out int progress), Is.True);
            Assert.That(remaining, Is.EqualTo((ushort)1), "the finished unit waits — the queue pauses, nothing is lost");
            Assert.That(progress, Is.EqualTo(100 << 16), "progress clamps at the completion threshold");

            f.Entities.DespawnUnit(dummies[0]);
            f.Step(1);
            Assert.That(CountRole(f, UnitRole.BasicInfantry), Is.EqualTo(1), "the paused queue resumes once space frees up");
        }

        [Test]
        public void SoldBuilding_LosesQueueWithoutRefund()
        {
            var f = new Fixture();
            uint barracks = f.SpawnBarracks(0);
            Assert.That(f.Production.TryQueueUnit(0, barracks, 12, 2), Is.True); // 240 spent -> 760

            Assert.That(f.Construction.SellBuilding(barracks), Is.True); // +250 -> 1010
            f.Step(1);
            Assert.That(f.Production.TotalQueuedUnits, Is.EqualTo(0), "the row is dropped with the dead building");
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1010L),
                "queued units on a sold building are lost WITHOUT refund (documented provisional)");
        }

        [Test]
        public void Snapshot_Roundtrip_IsByteIdentical_AndTamperingIsRejected()
        {
            var f = new Fixture();
            uint barracks = f.SpawnBarracks(0);
            Assert.That(f.Production.TryQueueUnit(0, barracks, 12, 2), Is.True);
            f.Production.SetRallyPoint(barracks, SimFixed.FromInt(30), SimFixed.FromInt(31));
            f.Step(37); // accumulate progress on the running entry

            var writer = new SnapshotBlockWriter();
            f.Production.WriteState(writer);
            byte[] bytes = writer.ToArray();

            var restored = new ProductionSystem(
                new EntityManager(64), new EconomySystem(new EntityManager(64)),
                new ConstructionSystem(new EntityManager(64), new EconomySystem(new EntityManager(64))));
            Assert.That(restored.TryValidateState(bytes), Is.True);
            Assert.That(restored.TryRestoreState(bytes), Is.True);

            var rewritten = new SnapshotBlockWriter();
            restored.WriteState(rewritten);
            Assert.That(rewritten.ToArray(), Is.EqualTo(bytes), "serialize -> restore -> serialize is byte-identical");

            // Tampering: unknown unit definition id in the first entry.
            byte[] tampered = (byte[])bytes.Clone();
            tampered[1 + 2 + 4 + 4 + 4 + 1] = 200; // version, count16, entity, rallyX, rallyY, entries -> defId low byte
            Assert.That(restored.TryValidateState(tampered), Is.False);

            var longer = new byte[bytes.Length + 1];
            System.Array.Copy(bytes, longer, bytes.Length);
            Assert.That(restored.TryValidateState(longer), Is.False);
        }

        [Test]
        public void SetRallyPoint_OffMap_IsRejected_RallyUnchanged_QueueContinues()
        {
            var f = new Fixture();
            uint barracks = f.SpawnBarracks(0);
            f.Production.SetRallyPoint(barracks, SimFixed.FromInt(30), SimFixed.FromInt(30));

            Assert.That(f.Production.ValidateSetRallyPoint(0, barracks, SimFixed.FromInt(200), SimFixed.FromInt(30)),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget), "200 is outside the 128x128 map");
            Assert.That(f.Production.ValidateSetRallyPoint(0, barracks, SimFixed.FromInt(-1), SimFixed.FromInt(30)),
                Is.EqualTo(CommandResultCode.RejectedInvalidTarget), "negative targets floor below the map");
            Assert.That(f.Production.ValidateSetRallyPoint(0, barracks, SimFixed.FromInt(127), SimFixed.FromInt(127)),
                Is.EqualTo(CommandResultCode.Applied), "the map corner is legal");

            Assert.That(f.Production.TryGetProducer(barracks, out _, out int rallyX, out int rallyY), Is.True);
            Assert.That(rallyX, Is.EqualTo(SimFixed.FromInt(30).RawValue), "a rejected rally leaves the existing one unchanged");
            Assert.That(rallyY, Is.EqualTo(SimFixed.FromInt(30).RawValue));

            // The queue is unaffected by the rejected rally (no parking).
            Assert.That(f.Production.TryQueueUnit(0, barracks, 12, 1), Is.True);
            f.Step(100);
            EntityId unit = FindRole(f, UnitRole.BasicInfantry);
            Assert.That(f.Entities.GetUnitRef(unit).Transform.PositionX, Is.EqualTo(SimFixed.FromInt(30)));
            Assert.That(f.Entities.GetUnitRef(unit).Transform.PositionY, Is.EqualTo(SimFixed.FromInt(30)));
        }

        [Test]
        public void QueueUnit_LegionSlot_ChargesLegionCost()
        {
            // The definition id is faction-resolved: the Legion Rekrut
            // (def 29) costs 60 AE (Infantry.md), not the Alliance 120.
            var f = new Fixture(configure: e => e.SetSlotFaction(1, FactionId.Legion));
            uint barracks = f.SpawnBarracks(1);

            Assert.That(f.Production.ValidateQueueUnit(1, barracks, 29, 2), Is.EqualTo(CommandResultCode.Applied));
            Assert.That(f.Production.TryQueueUnit(1, barracks, 29, 2), Is.True);
            Assert.That(f.Economy.GetPlayerEconomy(1).AetheriumCredits, Is.EqualTo(880L),
                "2 x 60 AE at the Legion row");

            f.Step(80); // Legion Rekrut: 80 build ticks at full power
            Assert.That(CountRole(f, UnitRole.BasicInfantry), Is.EqualTo(1));
            EntityId unit = FindRole(f, UnitRole.BasicInfantry);
            Assert.That(f.Entities.GetUnitRef(unit).MaxHealth, Is.EqualTo(55), "Legion HP (Infantry.md)");
        }

        [Test]
        public void QueueUnit_ForeignFactionDefinition_IsRejectedInvalidTarget()
        {
            var f = new Fixture(configure: e => e.SetSlotFaction(1, FactionId.Legion));
            uint barracksA = f.SpawnBarracks(0);
            // Slot 1's buildings stand at distinct cells (the fixture helper
            // uses fixed coordinates).
            Assert.That(f.Construction.PlaceCompletedBuilding(1, 22, 50, 50).IsValid, Is.True);
            EntityId legionBarracks = f.Construction.PlaceCompletedBuilding(1, 24, 30, 30);
            Assert.That(legionBarracks.IsValid, Is.True);
            uint barracksL = UnitCommandStateView.ToRawEntityId(legionBarracks);

            Assert.That(f.Production.ValidateQueueUnit(1, barracksL, 12, 1), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "Legion slot, Alliance BasicInfantry");
            Assert.That(f.Production.ValidateQueueUnit(0, barracksA, 29, 1), Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "Alliance slot, Legion BasicInfantry");
            Assert.That(f.Production.ValidateQueueUnit(1, barracksL, 29, 1), Is.EqualTo(CommandResultCode.Applied),
                "Legion slot, Legion BasicInfantry is legal");
            Assert.That(f.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1000L), "rejections charge nothing");
            Assert.That(f.Economy.GetPlayerEconomy(1).AetheriumCredits, Is.EqualTo(1000L));
        }

        private static int CountRole(Fixture f, UnitRole role)
        {
            int count = 0;
            UnitState[] units = f.Entities.RawUnits;
            for (int i = 0; i < f.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].Role == role) count++;
            }
            return count;
        }

        private static EntityId FindRole(Fixture f, UnitRole role)
        {
            UnitState[] units = f.Entities.RawUnits;
            for (int i = 0; i < f.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].Role == role) return units[i].Id;
            }
            throw new System.InvalidOperationException($"no entity with role {role} found");
        }
    }
}
