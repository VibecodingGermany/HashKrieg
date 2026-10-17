using System;
using System.Collections.Generic;
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

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Truppenführung suite (EditMode lane): the sprint that made units share
    /// space. Covers the three fixes — formation distribution of Move
    /// commands (and the unit-aware spawn search), separation for STANDING
    /// units (damped, dead-zoned, no vibration) and building footprints as
    /// impassable cost-field terrain including the placement push-out.
    /// <para>
    /// Hand-mirrored with the .NET lane copy of this fixture.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class TroopHandlingTests
    {
        private const ulong Seed = 0x5EED42UL;
        private const ushort MapSize = 64;

        /// <summary>Full-stack host mirroring the canonical registration order, cost-field wiring included.</summary>
        private sealed class Host
        {
            public SimulationKernel Kernel;
            public EntityManager Entities;
            public EconomySystem Economy;
            public ConstructionSystem Construction;
            public ProductionSystem Production;
            public PathfindingSystem Pathfinding;
            public MatchSession Session;
            public CommandIngress Ingress;

            public static Host Create()
            {
                var entities = new EntityManager(256);
                var pathfinding = new PathfindingSystem(MapSize, MapSize);
                var movement = new MovementSystem(entities, pathfinding);
                var economy = new EconomySystem(entities, EconomySystem.CanonicalMatchStartingCreditsAE);
                var construction = new ConstructionSystem(entities, economy, pathfinding.CostField);
                var production = new ProductionSystem(entities, economy, construction);

                var kernel = new SimulationKernel(new SimRandom(Seed));
                kernel.RegisterSystem(economy);
                kernel.RegisterSystem(construction);
                kernel.RegisterSystem(production);
                kernel.RegisterSystem(pathfinding);
                kernel.RegisterSystem(movement);

                var session = new MatchSession(localSlot: 0, activeSlots: new byte[] { 0, 1 }, inputDelayTicks: 1);
                var ingress = new CommandIngress(session);
                _ = new LocalLoopbackTransport(ingress);
                kernel.BindCommands(
                    new UnitCommandStateView(entities, pathfinding, economy, construction, production), ingress);
                kernel.Start();
                return new Host
                {
                    Kernel = kernel,
                    Entities = entities,
                    Economy = economy,
                    Construction = construction,
                    Production = production,
                    Pathfinding = pathfinding,
                    Session = session,
                    Ingress = ingress,
                };
            }

            public void StepTick()
            {
                uint nextTick = Kernel.CurrentTick.Value + 1;
                CommandBatch batch = Ingress.SealTickBatch(nextTick);
                if (batch.Count > 0)
                {
                    Assert.That(Kernel.SubmitBatch(batch), Is.True, "a sealed batch must be accepted");
                }
                Kernel.StepTick();
                Session.AdvanceTick();
            }

            public void Step(int ticks)
            {
                for (int i = 0; i < ticks; i++) StepTick();
            }

            public EntityId SpawnUnit(int gridX, int gridY)
            {
                SimDefinitions.TryGetUnit(FactionId.Alliance, UnitRole.BasicInfantry, out SimUnitDefinition def);
                return Entities.SpawnUnit(
                    0,
                    new Transform2D(SimFixed.FromInt(gridX), SimFixed.FromInt(gridY)),
                    def.MoveSpeed,
                    maxHealth: def.MaxHealth,
                    role: def.Role);
            }

            public uint Raw(EntityId id) => UnitCommandStateView.ToRawEntityId(id);

            public GridPos2D CellOf(EntityId id)
            {
                ref readonly UnitState unit = ref Entities.GetUnitRef(id);
                return new GridPos2D(
                    Math.Max(0, SimFixed.WorldToGrid(unit.Transform.PositionX)),
                    Math.Max(0, SimFixed.WorldToGrid(unit.Transform.PositionY)));
            }

            public void SubmitMove(uint[] rawIds, int targetX, int targetY)
            {
                var payload = new MovePayload(rawIds, SimFixed.FromInt(targetX), SimFixed.FromInt(targetY));
                Assert.That(
                    Ingress.TrySubmitIntent(CommandIntent.Create(payload), out CommandRejectReason reason),
                    Is.EqualTo(CommandIngressResult.Accepted), $"move rejected: {reason}");
            }
        }

        // --------------------------------------------------------------
        // Teil 1: formation distribution
        // --------------------------------------------------------------

        [Test]
        public void MoveCommand_AssignsDistinctGoalCells_InEntityIndexOrder()
        {
            Host host = Host.Create();
            var ids = new List<EntityId>();
            for (int i = 0; i < 12; i++)
            {
                ids.Add(host.SpawnUnit(5 + i, 5));
            }

            // Canonical payloads carry the ids sorted (the intake rejects
            // unsorted lists), which here means ascending entity index.
            var raws = new uint[ids.Count];
            for (int i = 0; i < ids.Count; i++)
            {
                raws[i] = host.Raw(ids[i]);
            }
            host.SubmitMove(raws, 30, 30);
            host.Step(2); // the sealed batch applies at its target tick

            var goals = new HashSet<GridPos2D>();
            foreach (EntityId id in ids)
            {
                ref readonly UnitState unit = ref host.Entities.GetUnitRef(id);
                Assert.That(unit.IsMoving, Is.True, $"unit {id.Index} must be under a movement order");
                Assert.That(unit.TargetGridPos, Is.EqualTo(new GridPos2D(30, 30)),
                    $"unit {id.Index} shares the ONE group flow destination");
                Assert.That(goals.Add(unit.GoalGridPos), Is.True,
                    $"unit {id.Index} got a duplicate goal cell {unit.GoalGridPos}");
                int chebyshev = Math.Max(
                    Math.Abs(unit.GoalGridPos.X - 30), Math.Abs(unit.GoalGridPos.Y - 30));
                Assert.That(chebyshev, Is.LessThanOrEqualTo(2),
                    $"12 units fit into rings 0-2; unit {id.Index} landed on ring {chebyshev}");
            }

            // The lowest entity index claims the command target cell itself.
            int lowestIndex = int.MaxValue;
            EntityId lowest = default;
            foreach (EntityId id in ids)
            {
                if (id.Index < lowestIndex)
                {
                    lowestIndex = id.Index;
                    lowest = id;
                }
            }
            Assert.That(host.Entities.GetUnitRef(lowest).GoalGridPos, Is.EqualTo(new GridPos2D(30, 30)),
                "the lowest entity index claims the target cell");
        }

        [Test]
        public void MoveCommand_OntoBuilding_AssignsOnlyWalkableGoalCells()
        {
            Host host = Host.Create();
            ushort powerDef = SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.Power);
            Assert.That(host.Construction.PlaceCompletedBuilding(0, powerDef, 29, 29).IsValid, Is.True);

            var ids = new List<EntityId>();
            var raws = new List<uint>();
            for (int i = 0; i < 5; i++)
            {
                EntityId id = host.SpawnUnit(5 + i, 5);
                ids.Add(id);
                raws.Add(host.Raw(id));
            }
            // The target cell (30,30) sits inside the footprint (29..31)^2.
            host.SubmitMove(raws.ToArray(), 30, 30);
            host.Step(2);

            foreach (EntityId id in ids)
            {
                GridPos2D goal = host.Entities.GetUnitRef(id).GoalGridPos;
                bool insideFootprint = goal.X >= 29 && goal.X <= 31 && goal.Y >= 29 && goal.Y <= 31;
                Assert.That(insideFootprint, Is.False,
                    $"unit {id.Index} must not be sent into the footprint, got {goal}");
                Assert.That(host.Pathfinding.CostField.IsWalkable(goal.X, goal.Y), Is.True);
            }
        }

        [Test]
        public void ProducedUnits_OccupyDistinctCells()
        {
            Host host = Host.Create();
            // A Barracks draws 15 power: without a plant the low-power
            // multiplier halves production speed (documented economy rule).
            ushort powerDef = SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.Power);
            Assert.That(host.Construction.PlaceCompletedBuilding(0, powerDef, 40, 40).IsValid, Is.True);
            ushort barracksDef = SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.Barracks);
            EntityId barracks = host.Construction.PlaceCompletedBuilding(0, barracksDef, 10, 10);
            uint barracksRaw = host.Raw(barracks);
            ushort infantryDef = SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.BasicInfantry);
            Assert.That(host.Production.TryQueueUnit(0, barracksRaw, infantryDef, 5), Is.True);

            // 100 full-power build ticks per infantry, slack for the spawns.
            host.Step(6 * 100 + 50);

            // No stack: every pair of spawns stands at (near-)contact distance
            // or further. Since D-116 infantry spaces at 0.3 cells each, so two
            // soldiers may legitimately share one cell — distance, not cell
            // identity, is the invariant.
            var spawns = new List<int>();
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].Role == UnitRole.BasicInfantry) spawns.Add(i);
            }
            Assert.That(spawns.Count, Is.EqualTo(5), "all five queued infantry spawned");
            for (int a = 0; a < spawns.Count; a++)
            {
                for (int b = a + 1; b < spawns.Count; b++)
                {
                    ref readonly UnitState ua = ref units[spawns[a]];
                    ref readonly UnitState ub = ref units[spawns[b]];
                    SimFixed settled = (MovementSpacing.RadiusOf(in ua) + MovementSpacing.RadiusOf(in ub))
                                       * SimFixed.FromRaw(SimFixed.OneRaw / 8 * 7);
                    Assert.That(ua.Transform.DistanceToSquared(in ub.Transform), Is.GreaterThanOrEqualTo(settled * settled),
                        $"infantry {spawns[a]} and {spawns[b]} stack — the spawn stack is back");
                }
            }
        }

        // --------------------------------------------------------------
        // Teil 2: standing separation
        // --------------------------------------------------------------

        [Test]
        public void StandingSeparation_UnstacksOverlappingUnits_AndSettles()
        {
            Host host = Host.Create();
            // Three units on one cell, slightly offset (exact-zero overlaps
            // are covered by the tie-break test below).
            EntityId a = host.Entities.SpawnUnit(0,
                new Transform2D(SimFixed.FromInt(10) + SimFixed.FromRaw(13107), SimFixed.FromInt(10) + SimFixed.FromRaw(32768)),
                SimFixed.FromInt(4));
            EntityId b = host.Entities.SpawnUnit(0,
                new Transform2D(SimFixed.FromInt(10) + SimFixed.FromRaw(32768), SimFixed.FromInt(10) + SimFixed.FromRaw(32768)),
                SimFixed.FromInt(4));
            EntityId c = host.Entities.SpawnUnit(0,
                new Transform2D(SimFixed.FromInt(10) + SimFixed.FromRaw(52429), SimFixed.FromInt(10) + SimFixed.FromRaw(32768)),
                SimFixed.FromInt(4));

            host.Step(150);

            // The dead zone leaves a small residual overlap (the damped push
            // falls under the steering threshold just before exact contact):
            // settled means "no longer stacked", not "exactly minDist".
            // 7/8 of the contact distance is exact in Q16.16.
            ref readonly UnitState ua = ref host.Entities.GetUnitRef(a);
            ref readonly UnitState ub = ref host.Entities.GetUnitRef(b);
            ref readonly UnitState uc = ref host.Entities.GetUnitRef(c);
            SimFixed minDist = ua.Radius + ub.Radius;
            SimFixed settled = minDist * SimFixed.FromRaw(SimFixed.OneRaw / 8 * 7);
            SimFixed settledSq = settled * settled;
            Assert.That(ua.Transform.DistanceToSquared(in ub.Transform), Is.GreaterThanOrEqualTo(settledSq),
                "a/b must unstack to (near-)contact distance");
            Assert.That(ua.Transform.DistanceToSquared(in uc.Transform), Is.GreaterThanOrEqualTo(settledSq),
                "a/c must unstack to (near-)contact distance");
            Assert.That(ub.Transform.DistanceToSquared(in uc.Transform), Is.GreaterThanOrEqualTo(settledSq),
                "b/c must unstack to (near-)contact distance");

            // No vibration: once settled, the positions are bit-frozen.
            Transform2D pa = ua.Transform;
            Transform2D pb = ub.Transform;
            Transform2D pc = uc.Transform;
            host.Step(50);
            Assert.That(host.Entities.GetUnitRef(a).Transform, Is.EqualTo(pa), "unit a vibrates");
            Assert.That(host.Entities.GetUnitRef(b).Transform, Is.EqualTo(pb), "unit b vibrates");
            Assert.That(host.Entities.GetUnitRef(c).Transform, Is.EqualTo(pc), "unit c vibrates");
        }

        [Test]
        public void StandingSeparation_ExactOverlap_UsesTheIndexTieBreak()
        {
            Host host = Host.Create();
            // Bit-identical positions: the distance-based push is undefined,
            // the entity-index tie-break must separate them anyway.
            EntityId lower = host.SpawnUnit(10, 10);
            EntityId higher = host.SpawnUnit(10, 10);
            Assert.That(lower.Index, Is.LessThan(higher.Index));

            host.Step(150);

            ref readonly UnitState a = ref host.Entities.GetUnitRef(lower);
            ref readonly UnitState b = ref host.Entities.GetUnitRef(higher);
            Assert.That(a.Transform.PositionX != b.Transform.PositionX
                || a.Transform.PositionY != b.Transform.PositionY, Is.True,
                "exactly stacked units must separate");
            // Same dead-zone residual as the offset case: 7/8 of contact,
            // measured with the per-role spacing radius (D-116).
            SimFixed minDist = MovementSpacing.RadiusOf(in a) + MovementSpacing.RadiusOf(in b);
            SimFixed settled = minDist * SimFixed.FromRaw(SimFixed.OneRaw / 8 * 7);
            Assert.That(a.Transform.DistanceToSquared(in b.Transform),
                Is.GreaterThanOrEqualTo(settled * settled),
                "the tie-break separates to (near-)contact distance");
        }

        // --------------------------------------------------------------
        // Teil 3: buildings are terrain
        // --------------------------------------------------------------

        [Test]
        public void Army_RoutesAroundBuilding_NeverThrough()
        {
            Host host = Host.Create();
            // Footprint (30..32, 28..30) squarely on the straight line.
            ushort powerDef = SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.Power);
            Assert.That(host.Construction.PlaceCompletedBuilding(0, powerDef, 30, 28).IsValid, Is.True);

            EntityId unit = host.SpawnUnit(20, 29);
            host.SubmitMove(new[] { host.Raw(unit) }, 40, 29);

            bool arrived = false;
            for (int t = 0; t < 600 && !arrived; t++)
            {
                host.StepTick();
                GridPos2D cell = host.CellOf(unit);
                bool insideFootprint = cell.X >= 30 && cell.X <= 32 && cell.Y >= 28 && cell.Y <= 30;
                Assert.That(insideFootprint, Is.False, $"unit entered the footprint at {cell} (tick {t})");
                arrived = !host.Entities.GetUnitRef(unit).IsMoving;
            }

            Assert.That(arrived, Is.True, "the unit must arrive, not stall at the wall");
            Assert.That(host.CellOf(unit), Is.EqualTo(new GridPos2D(40, 29)));
        }

        [Test]
        public void Placement_PushesStandingUnitOutOfFootprint_AndBlocksTheCostField()
        {
            Host host = Host.Create();
            EntityId unit = host.SpawnUnit(12, 12);
            host.Step(5); // settles; definitely standing

            ushort powerDef = SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.Power);
            Assert.That(host.Construction.PlaceCompletedBuilding(0, powerDef, 11, 11).IsValid, Is.True,
                "placement on a standing unit is legal — the unit is pushed out");

            GridPos2D cell = host.CellOf(unit);
            bool insideFootprint = cell.X >= 11 && cell.X <= 13 && cell.Y >= 11 && cell.Y <= 13;
            Assert.That(insideFootprint, Is.False, $"the unit was pushed out, stands at {cell}");
            Assert.That(host.Pathfinding.CostField.IsWalkable(12, 12), Is.False,
                "the footprint is impassable terrain now");
            Assert.That(host.Pathfinding.CostField.IsWalkable(cell.X, cell.Y), Is.True,
                "the push-out target is walkable");

            // The displacement is stable: the unit stays outside and at rest.
            host.Step(20);
            GridPos2D settled = host.CellOf(unit);
            bool settledInside = settled.X >= 11 && settled.X <= 13 && settled.Y >= 11 && settled.Y <= 13;
            Assert.That(settledInside, Is.False, $"the unit must stay outside, is at {settled}");
        }

        [Test]
        public void Placement_OnMovingUnitsGoal_StopsItAtTheWall()
        {
            Host host = Host.Create();
            EntityId unit = host.SpawnUnit(5, 12);
            host.SubmitMove(new[] { host.Raw(unit) }, 12, 12);
            host.Step(2); // the order applies; the unit is en route

            // The building lands on the goal cell before the unit arrives.
            ushort powerDef = SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.Power);
            Assert.That(host.Construction.PlaceCompletedBuilding(0, powerDef, 11, 11).IsValid, Is.True);

            bool stopped = false;
            for (int t = 0; t < 300 && !stopped; t++)
            {
                host.StepTick();
                GridPos2D cell = host.CellOf(unit);
                bool insideFootprint = cell.X >= 11 && cell.X <= 13 && cell.Y >= 11 && cell.Y <= 13;
                Assert.That(insideFootprint, Is.False, $"unit entered the footprint at {cell} (tick {t})");
                stopped = !host.Entities.GetUnitRef(unit).IsMoving;
            }
            Assert.That(stopped, Is.True,
                "a unit whose goal became a wall must stop beside it, not push against it forever");
        }

        [Test]
        public void Sell_FreesTheFootprintInTheCostField()
        {
            Host host = Host.Create();
            ushort powerDef = SimDefinitions.ToDefinitionId(FactionId.Alliance, UnitRole.Power);
            EntityId building = host.Construction.PlaceCompletedBuilding(0, powerDef, 20, 20);
            Assert.That(building.IsValid, Is.True);
            Assert.That(host.Pathfinding.CostField.IsWalkable(21, 21), Is.False);

            Assert.That(host.Construction.SellBuilding(host.Raw(building)), Is.True);
            Assert.That(host.Pathfinding.CostField.IsWalkable(21, 21), Is.True,
                "selling frees the footprint cells in the cost field");
        }

        // --------------------------------------------------------------
        // D-116: make way, heading, size-aware slots
        // --------------------------------------------------------------

        private static EntityId SpawnRole(Host host, UnitRole role, SimFixed x, SimFixed y, byte slot = 0)
        {
            SimDefinitions.TryGetUnit(slot == 0 ? FactionId.Alliance : FactionId.Legion, role, out SimUnitDefinition def);
            return host.Entities.SpawnUnit(slot, new Transform2D(x, y), def.MoveSpeed, maxHealth: def.MaxHealth, role: def.Role);
        }

        private static SimFixed Half(int cells) => SimFixed.FromInt(cells) + SimFixed.FromRaw(SimFixed.OneRaw / 2);

        [Test]
        public void StandingInfantry_MakesWay_ForADrivingTank()
        {
            Host host = Host.Create();
            // Tank drives east along y = 30.5; a soldier stands right on its line.
            EntityId tank = SpawnRole(host, UnitRole.BattleTank, Half(20), Half(30));
            EntityId soldier = SpawnRole(host, UnitRole.BasicInfantry, Half(26), Half(30));
            host.SubmitMove(new[] { host.Raw(tank) }, 34, 30);

            SimFixed maxTankDrift = SimFixed.Zero;
            for (int t = 0; t < 60; t++)
            {
                host.StepTick();
                SimFixed drift = host.Entities.GetUnitRef(tank).Transform.PositionY - Half(30);
                if (drift < SimFixed.Zero) drift = -drift;
                if (drift > maxTankDrift) maxTankDrift = drift;
            }

            ref readonly UnitState s = ref host.Entities.GetUnitRef(soldier);
            ref readonly UnitState k = ref host.Entities.GetUnitRef(tank);
            SimFixed sideStep = s.Transform.PositionY - Half(30);
            if (sideStep < SimFixed.Zero) sideStep = -sideStep;
            Assert.That(sideStep, Is.GreaterThan(MovementSpacing.RadiusOf(in k)),
                "the soldier must have stepped off the tank's line");
            Assert.That(maxTankDrift, Is.LessThan(SimFixed.FromRaw(SimFixed.OneRaw / 4)),
                "the tank keeps its line through the group instead of being pushed aside");
            Assert.That(SimFixed.WorldToGrid(k.Transform.PositionX), Is.EqualTo(34), "the tank arrived");
        }

        [Test]
        public void FlowField_OpenGround_LeadsStraight_NotDiagonalFirst()
        {
            // Octile costs (D-116): east of the target on open ground the flow
            // points straight east, not into a diagonal staircase.
            Host host = Host.Create();
            EntityId tank = SpawnRole(host, UnitRole.BattleTank, Half(20), Half(30));
            host.SubmitMove(new[] { host.Raw(tank) }, 34, 30);
            host.Step(3);
            FlowField f = host.Pathfinding.GetField(host.Entities.GetUnitRef(tank).TargetGridPos);
            Assert.That(f, Is.Not.Null);
            for (int x = 18; x < 34; x++)
            {
                Assert.That(f.GetDirection((ushort)x, 30), Is.EqualTo(Direction2D.East), $"cell ({x},30)");
            }
            Assert.That(f.GetDirection(30, 33), Is.EqualTo(Direction2D.SouthEast), "off the row, diagonal first while it saves distance");
        }

        [Test]
        public void StandingTank_DoesNotYield_ToInfantry()
        {
            Host host = Host.Create();
            EntityId soldier = SpawnRole(host, UnitRole.BasicInfantry, Half(20), Half(30));
            EntityId tank = SpawnRole(host, UnitRole.BattleTank, Half(25), Half(30));
            host.SubmitMove(new[] { host.Raw(soldier) }, 32, 30);
            host.Step(5);
            ref readonly UnitState k = ref host.Entities.GetUnitRef(tank);
            Assert.That(k.Transform.PositionY, Is.EqualTo(Half(30)), "a tank does not step aside for a soldier");
            Assert.That(k.Transform.PositionX, Is.EqualTo(Half(25)));
        }

        [Test]
        public void EnemyUnits_DoNotYield()
        {
            Host host = Host.Create();
            EntityId tank = SpawnRole(host, UnitRole.BattleTank, Half(20), Half(30));
            EntityId enemy = SpawnRole(host, UnitRole.BasicInfantry, Half(23), Half(31), slot: 1);
            host.SubmitMove(new[] { host.Raw(tank) }, 30, 30);
            host.Step(3);
            ref readonly UnitState e = ref host.Entities.GetUnitRef(enemy);
            Assert.That(e.Transform.PositionY, Is.EqualTo(Half(31)), "make-way is for friends only");
        }

        [Test]
        public void Heading_IgnoresSeparation_InACrowd()
        {
            Host host = Host.Create();
            // A soldier walks east straight through a block of standing soldiers.
            var crowd = new List<EntityId>();
            for (int y = 28; y <= 32; y++)
            {
                for (int x = 24; x <= 27; x++) crowd.Add(SpawnRole(host, UnitRole.BasicInfantry, Half(x), Half(y)));
            }
            EntityId walker = SpawnRole(host, UnitRole.BasicInfantry, Half(18), Half(30));
            host.SubmitMove(new[] { host.Raw(walker) }, 36, 30);
            host.Step(3);

            for (int t = 0; t < 50; t++)
            {
                host.StepTick();
                ref readonly UnitState w = ref host.Entities.GetUnitRef(walker);
                if (!w.IsMoving) break;
                int raw = w.Transform.Rotation.RawValue;
                int offEast = Math.Min(raw, 65536 - raw);
                Assert.That(offEast, Is.LessThanOrEqualTo(8192 + 5461),
                    $"tick {t}: heading swung {offEast * 360.0 / 65536:F0} deg off the path — separation is steering the heading again");
            }
        }

        [Test]
        public void GroupMove_LargeUnits_ClaimSlotsWithAFreeRing()
        {
            Host host = Host.Create();
            var tanks = new List<EntityId>();
            for (int i = 0; i < 6; i++) tanks.Add(SpawnRole(host, UnitRole.BattleTank, Half(10 + i * 2), Half(10)));
            var raws = new uint[tanks.Count];
            for (int i = 0; i < tanks.Count; i++) raws[i] = host.Raw(tanks[i]);
            host.SubmitMove(raws, 40, 40);
            host.StepTick();
            host.StepTick();

            for (int a = 0; a < tanks.Count; a++)
            {
                GridPos2D ga = host.Entities.GetUnitRef(tanks[a]).GoalGridPos;
                for (int b = a + 1; b < tanks.Count; b++)
                {
                    GridPos2D gb = host.Entities.GetUnitRef(tanks[b]).GoalGridPos;
                    int cheb = Math.Max(Math.Abs(ga.X - gb.X), Math.Abs(ga.Y - gb.Y));
                    Assert.That(cheb, Is.GreaterThanOrEqualTo(2), $"tank slots {ga} and {gb} are adjacent");
                }
            }
        }

        private static GridPos2D CellOfTransform(in Transform2D transform)
        {
            return new GridPos2D(
                Math.Max(0, SimFixed.WorldToGrid(transform.PositionX)),
                Math.Max(0, SimFixed.WorldToGrid(transform.PositionY)));
        }
    }
}
