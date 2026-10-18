using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nova.Core;
using Nova.Gameplay;
using Nova.Simulation.State;
using EntityId = Nova.Core.EntityId;

namespace Nova.Gameplay.Tests
{
    /// <summary>Attack-move, patrol and guard as client-side orders over plain commands (D-117).</summary>
    [TestFixture]
    public class StandingOrdersTests
    {
        private sealed class FakeWorld : IStandingOrderWorld
        {
            public readonly Dictionary<EntityId, UnitState> Own = new Dictionary<EntityId, UnitState>();
            public readonly Dictionary<EntityId, Vector2> Enemies = new Dictionary<EntityId, Vector2>();
            public float Range = 5f;

            public bool TryGetOwnUnit(EntityId id, out UnitState unit) => Own.TryGetValue(id, out unit);
            public float WeaponRange(in UnitState unit) => Range;
            public bool IsAliveAndVisible(EntityId enemy) => Enemies.ContainsKey(enemy);

            public bool TryFindVisibleEnemy(Vector2 centre, float radius, out EntityId enemy)
            {
                enemy = EntityId.Invalid;
                float best = radius;
                foreach (KeyValuePair<EntityId, Vector2> kv in Enemies)
                {
                    float d = (kv.Value - centre).magnitude;
                    if (d <= best) { best = d; enemy = kv.Key; }
                }
                return enemy.IsValid;
            }

            public EntityId Add(int index, float x, float y, bool moving = false)
            {
                var id = new EntityId(index, 1);
                var u = new UnitState { Id = id, IsActive = true, IsMoving = moving, AttackTarget = EntityId.Invalid,
                    Transform = new Transform2D(SimFixed.FromFloat(x), SimFixed.FromFloat(y)) };
                Own[id] = u;
                return id;
            }

            public void Place(EntityId id, float x, float y, bool moving)
            {
                UnitState u = Own[id];
                u.Transform = new Transform2D(SimFixed.FromFloat(x), SimFixed.FromFloat(y));
                u.IsMoving = moving;
                Own[id] = u;
            }
        }

        [Test]
        public void AttackMove_MovesOn_AttacksWhatItMeets_AndEndsOnArrival()
        {
            var world = new FakeWorld();
            EntityId tank = world.Add(1, 10, 10);
            var orders = new StandingOrders();
            orders.SetAttackMove(tank, new Vector2(40, 10));
            var actions = new List<StandingAction>();

            orders.Tick(world, actions);
            Assert.That(actions, Has.Count.EqualTo(1));
            Assert.That(actions[0].Kind, Is.EqualTo(StandingActionKind.Move));

            world.Place(tank, 20, 10, moving: true);
            world.Enemies[new EntityId(9, 1)] = new Vector2(26, 11);
            actions.Clear();
            orders.Tick(world, actions);
            Assert.That(actions[0].Kind, Is.EqualTo(StandingActionKind.Attack), "an enemy in reach is engaged");

            world.Enemies.Clear();
            world.Place(tank, 22, 10, moving: false);
            actions.Clear();
            orders.Tick(world, actions);
            Assert.That(actions[0].Kind, Is.EqualTo(StandingActionKind.Move), "after the fight the drive resumes");
            Assert.That(actions[0].Point, Is.EqualTo(new Vector2(40, 10)));

            world.Place(tank, 39.5f, 10, moving: false);
            actions.Clear();
            orders.Tick(world, actions);
            Assert.That(orders.KindOf(tank), Is.EqualTo(StandingOrderKind.None), "arrived: the order ends");
        }

        [Test]
        public void Patrol_TurnsAroundAtEachEnd()
        {
            var world = new FakeWorld();
            EntityId scout = world.Add(1, 10, 10);
            var orders = new StandingOrders();
            orders.SetPatrol(scout, new Vector2(10, 10), new Vector2(30, 10));
            var actions = new List<StandingAction>();
            orders.Tick(world, actions);
            Assert.That(actions[0].Point, Is.EqualTo(new Vector2(30, 10)));

            world.Place(scout, 29.5f, 10, moving: false);
            actions.Clear();
            orders.Tick(world, actions);
            Assert.That(actions[0].Point, Is.EqualTo(new Vector2(10, 10)), "back to the start");
            Assert.That(orders.KindOf(scout), Is.EqualTo(StandingOrderKind.Patrol), "a patrol never ends by itself");
        }

        [Test]
        public void Move_IsNotResent_WhileTheUnitDrivesThere()
        {
            var world = new FakeWorld();
            EntityId scout = world.Add(1, 10, 10);
            var orders = new StandingOrders();
            orders.SetPatrol(scout, new Vector2(10, 10), new Vector2(30, 10));
            var actions = new List<StandingAction>();
            orders.Tick(world, actions);
            world.Place(scout, 15, 10, moving: true);
            actions.Clear();
            orders.Tick(world, actions);
            Assert.That(actions, Is.Empty, "no command spam while the order is underway");
        }

        [Test]
        public void Guard_Engages_ThenReturnsWhenDrawnOff()
        {
            var world = new FakeWorld();
            EntityId inf = world.Add(1, 10, 10);
            var orders = new StandingOrders();
            orders.SetGuard(inf, new Vector2(10, 10));
            var enemy = new EntityId(9, 1);
            world.Enemies[enemy] = new Vector2(14, 10);
            var actions = new List<StandingAction>();
            orders.Tick(world, actions);
            Assert.That(actions[0].Kind, Is.EqualTo(StandingActionKind.Attack));

            world.Place(inf, 20, 10, moving: true);     // chased too far
            actions.Clear();
            orders.Tick(world, actions);
            Assert.That(actions[0].Kind, Is.EqualTo(StandingActionKind.Return));
            Assert.That(actions[0].Point, Is.EqualTo(new Vector2(10, 10)));
        }

        [Test]
        public void Guard_IgnoresEnemiesFarFromItsPost()
        {
            var world = new FakeWorld();
            EntityId inf = world.Add(1, 10, 10);
            var orders = new StandingOrders();
            orders.SetGuard(inf, new Vector2(10, 10));
            world.Enemies[new EntityId(9, 1)] = new Vector2(30, 10);
            var actions = new List<StandingAction>();
            orders.Tick(world, actions);
            Assert.That(actions, Is.Empty);
        }

        [Test]
        public void DeadUnits_DropTheirOrders()
        {
            var world = new FakeWorld();
            EntityId inf = world.Add(1, 10, 10);
            var orders = new StandingOrders();
            orders.SetGuard(inf, new Vector2(10, 10));
            world.Own.Clear();
            orders.Tick(world, new List<StandingAction>());
            Assert.That(orders.Count, Is.Zero);
        }
    }
}
