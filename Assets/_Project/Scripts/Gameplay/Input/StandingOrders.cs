using System.Collections.Generic;
using UnityEngine;
using Nova.Core;
using EntityId = Nova.Core.EntityId;
using Nova.Simulation.State;

namespace Nova.Gameplay
{
    /// <summary>Client-side orders that last beyond one command (D-117).</summary>
    public enum StandingOrderKind : byte
    {
        None = 0,
        /// <summary>Drive to a point, fight every visible enemy met on the way.</summary>
        AttackMove = 1,
        /// <summary>Shuttle between two points forever, fighting on the way.</summary>
        Patrol = 2,
        /// <summary>Hold a point, engage enemies near it, return when drawn off.</summary>
        Guard = 3,
    }

    /// <summary>What the order layer asks the input to send this round.</summary>
    public enum StandingActionKind : byte
    {
        Move = 1,
        Attack = 2,
        /// <summary>Stop (drops the attack target and any pursuit), then Move.</summary>
        Return = 3,
    }

    public readonly struct StandingAction
    {
        public readonly StandingActionKind Kind;
        public readonly EntityId Unit;
        public readonly Vector2 Point;
        public readonly EntityId Target;

        public StandingAction(StandingActionKind kind, EntityId unit, Vector2 point, EntityId target)
        {
            Kind = kind;
            Unit = unit;
            Point = point;
            Target = target;
        }
    }

    /// <summary>
    /// The world as the order layer may see it — the local player's own units
    /// and only those enemies the fog shows. Implemented by the input
    /// controller; faked in tests.
    /// </summary>
    public interface IStandingOrderWorld
    {
        bool TryGetOwnUnit(EntityId id, out UnitState unit);

        /// <summary>Weapon range of the unit in cells; 0 for unarmed roles.</summary>
        float WeaponRange(in UnitState unit);

        /// <summary>Nearest VISIBLE enemy within <paramref name="radius"/> of <paramref name="centre"/>.</summary>
        bool TryFindVisibleEnemy(Vector2 centre, float radius, out EntityId enemy);

        bool IsAliveAndVisible(EntityId enemy);
    }

    /// <summary>
    /// Attack-move, patrol and guard without touching the command register
    /// (D-117): the frozen schema-v1 kinds (Move, Stop, AttackTarget) are
    /// enough, the same way attack pursuit and harvester escort already work
    /// on the client. Every <see cref="IntervalSeconds"/> the input calls
    /// <see cref="Tick"/>, which inspects each ordered unit and returns the
    /// plain commands to send. Because the commands are recorded, replays
    /// and the relay stay exact; the order state itself is local UI state.
    /// </summary>
    public sealed class StandingOrders
    {
        public const float IntervalSeconds = 0.5f;

        /// <summary>A destination counts as reached within this many cells.</summary>
        public const float ArriveRadius = 1.6f;

        /// <summary>Guards engage enemies this far around their post at least.</summary>
        public const float GuardRadius = 6f;

        /// <summary>A guard drawn further than this from its post breaks off and returns.</summary>
        public const float GuardLeash = 8f;

        /// <summary>Attack-movers notice enemies this far beyond their own weapon range.</summary>
        public const float AcquireMargin = 3f;

        private struct Order
        {
            public StandingOrderKind Kind;
            public Vector2 A;          // patrol start / guard post
            public Vector2 B;          // destination / patrol end
            public bool TowardB;
            public Vector2 LastSent;   // last Move point, to avoid re-sending
            public bool HasSent;
            public EntityId Engaged;
        }

        private readonly Dictionary<EntityId, Order> _orders = new Dictionary<EntityId, Order>();
        private readonly List<EntityId> _scratch = new List<EntityId>(64);

        public int Count => _orders.Count;

        public StandingOrderKind KindOf(EntityId unit)
        {
            return _orders.TryGetValue(unit, out Order o) ? o.Kind : StandingOrderKind.None;
        }

        public void SetAttackMove(EntityId unit, Vector2 destination)
        {
            _orders[unit] = new Order { Kind = StandingOrderKind.AttackMove, B = destination, TowardB = true };
        }

        public void SetPatrol(EntityId unit, Vector2 from, Vector2 to)
        {
            _orders[unit] = new Order { Kind = StandingOrderKind.Patrol, A = from, B = to, TowardB = true };
        }

        public void SetGuard(EntityId unit, Vector2 post)
        {
            _orders[unit] = new Order { Kind = StandingOrderKind.Guard, A = post };
        }

        public void Clear(EntityId unit) => _orders.Remove(unit);

        public void ClearAll() => _orders.Clear();

        /// <summary>One decision round; appends the commands to send to <paramref name="actions"/>.</summary>
        public void Tick(IStandingOrderWorld world, List<StandingAction> actions)
        {
            _scratch.Clear();
            foreach (KeyValuePair<EntityId, Order> kv in _orders) _scratch.Add(kv.Key);
            _scratch.Sort((a, b) => a.Index.CompareTo(b.Index));   // stable command order

            for (int i = 0; i < _scratch.Count; i++)
            {
                EntityId id = _scratch[i];
                Order o = _orders[id];
                if (!world.TryGetOwnUnit(id, out UnitState unit))
                {
                    _orders.Remove(id);
                    continue;
                }

                var pos = new Vector2(unit.Transform.PositionX.ToFloat(), unit.Transform.PositionY.ToFloat());
                float range = world.WeaponRange(in unit);

                if (o.Kind == StandingOrderKind.Guard)
                {
                    TickGuard(id, ref o, in unit, pos, range, world, actions);
                }
                else if (!TickTravel(id, ref o, in unit, pos, range, world, actions))
                {
                    _orders.Remove(id);   // attack-move done
                    continue;
                }
                _orders[id] = o;
            }
        }

        /// <summary>Attack-move and patrol. False when an attack-move has arrived and ends.</summary>
        private static bool TickTravel(EntityId id, ref Order o, in UnitState unit, Vector2 pos, float range,
                                       IStandingOrderWorld world, List<StandingAction> actions)
        {
            // Busy fighting: let combat (and the pursuit it set up) finish.
            if (unit.AttackTarget.IsValid && world.IsAliveAndVisible(unit.AttackTarget)) return true;
            if (o.Engaged.IsValid && world.IsAliveAndVisible(o.Engaged)) return true;
            o.Engaged = EntityId.Invalid;

            if (range > 0f && world.TryFindVisibleEnemy(pos, range + AcquireMargin, out EntityId enemy))
            {
                o.Engaged = enemy;
                o.HasSent = false;
                actions.Add(new StandingAction(StandingActionKind.Attack, id, Vector2.zero, enemy));
                return true;
            }

            Vector2 dest = o.TowardB ? o.B : o.A;
            if ((dest - pos).sqrMagnitude <= ArriveRadius * ArriveRadius)
            {
                if (o.Kind == StandingOrderKind.AttackMove) return false;
                o.TowardB = !o.TowardB;   // patrol turns around
                dest = o.TowardB ? o.B : o.A;
                o.HasSent = false;
            }

            // Send only when the destination changed or the unit came to rest
            // (an engagement, a block, or the previous order ran out).
            if (!o.HasSent || o.LastSent != dest || !unit.IsMoving)
            {
                actions.Add(new StandingAction(StandingActionKind.Move, id, dest, EntityId.Invalid));
                o.LastSent = dest;
                o.HasSent = true;
            }
            return true;
        }

        private static void TickGuard(EntityId id, ref Order o, in UnitState unit, Vector2 pos, float range,
                                      IStandingOrderWorld world, List<StandingAction> actions)
        {
            float fromPost = (pos - o.A).magnitude;
            bool fighting = (unit.AttackTarget.IsValid && world.IsAliveAndVisible(unit.AttackTarget))
                            || (o.Engaged.IsValid && world.IsAliveAndVisible(o.Engaged));

            if (fighting)
            {
                if (fromPost > GuardLeash)
                {
                    o.Engaged = EntityId.Invalid;
                    actions.Add(new StandingAction(StandingActionKind.Return, id, o.A, EntityId.Invalid));
                }
                return;
            }
            o.Engaged = EntityId.Invalid;

            if (range > 0f && world.TryFindVisibleEnemy(o.A, Mathf.Max(GuardRadius, range + 2f), out EntityId enemy))
            {
                o.Engaged = enemy;
                actions.Add(new StandingAction(StandingActionKind.Attack, id, Vector2.zero, enemy));
                return;
            }

            if (fromPost > 1f && !unit.IsMoving)
            {
                actions.Add(new StandingAction(StandingActionKind.Move, id, o.A, EntityId.Invalid));
            }
        }
    }
}
