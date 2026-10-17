using System;
using System.Collections.Generic;
using UnityEngine;
using Nova.Gameplay;
using Nova.Gameplay.Audio;
using Nova.Simulation.Combat;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.State;
using EntityId = Nova.Core.EntityId;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// Group orders of the command bar (D-117): formations, attack-move,
    /// patrol, guard and scatter. Everything here resolves into the frozen
    /// schema-v1 commands (Move, Stop, AttackTarget) — formations as one Move
    /// per unit, the lasting orders through <see cref="StandingOrders"/>,
    /// ticked like the attack pursuit next door.
    /// </summary>
    public sealed partial class RtsDeviceInput
    {
        private readonly StandingOrders _standing = new StandingOrders();
        private readonly List<StandingAction> _standingActions = new List<StandingAction>(64);
        private readonly List<FormationMember> _formationMembers = new List<FormationMember>(64);
        private readonly List<Vector2> _formationSlots = new List<Vector2>(64);
        private readonly EntityId[] _singleScratch = new EntityId[1];
        private readonly EntityId[] _combatScratch = new EntityId[SelectionManager.MaxSelectedEntities];
        private float _standingNextTime;
        private FormationKind _formation = FormationKind.Loose;
        private StandingOrderWorld _standingWorld;

        /// <summary>The marching order applied to every group move (Loose = simulation ring spread).</summary>
        public FormationKind Formation => _formation;

        /// <summary>Name of the armed order pick for HUD hints; empty when none is armed.</summary>
        public string ArmedOrderName
        {
            get
            {
                switch (_pendingOrder)
                {
                    case PendingOrder.Move: return "Bewegen";
                    case PendingOrder.Attack: return "Angriff";
                    case PendingOrder.Harvest: return "Ernten";
                    case PendingOrder.Repair: return "Reparieren";
                    case PendingOrder.AttackMove: return "Angriffsbewegung";
                    case PendingOrder.Patrol: return "Patrouille";
                    default: return string.Empty;
                }
            }
        }

        /// <summary>The standing order every selected mobile unit shares, or None.</summary>
        public StandingOrderKind SelectionStandingOrder
        {
            get
            {
                ReadOnlySpan<EntityId> mobile = CombatSelection(out int count);
                if (count == 0) return StandingOrderKind.None;
                StandingOrderKind first = _standing.KindOf(mobile[0]);
                for (int i = 1; i < count; i++)
                {
                    if (_standing.KindOf(mobile[i]) != first) return StandingOrderKind.None;
                }
                return first;
            }
        }

        /// <summary>
        /// The ARMED part of the mobile selection (D-117, per-unit-type bar). Guard,
        /// patrol, attack-move, scatter and formation moves make no sense for
        /// harvesters or builders, so the group-order hotkeys (W/K/J) and the bar
        /// act on this subset only: a mixed selection is filtered, never blocked.
        /// Backed by a reusable scratch buffer.
        /// </summary>
        private ReadOnlySpan<EntityId> CombatSelection(out int count)
        {
            count = 0;
            ReadOnlySpan<EntityId> mobile = MobileSelection(out int mobileCount);
            if (mobileCount == 0 || _runner == null || _runner.Entities == null) return ReadOnlySpan<EntityId>.Empty;
            FactionId faction = _runner.Economy.GetSlotFaction(_dispatcher != null ? _dispatcher.LocalSlot : (byte)0);
            for (int i = 0; i < mobileCount; i++)
            {
                if (_runner.Entities.TryGetUnit(mobile[i], out UnitState u) && CommandCardPresenter.IsArmedRole(faction, u.Role))
                {
                    _combatScratch[count++] = mobile[i];
                }
            }
            return _combatScratch.AsSpan(0, count);
        }

        public void SetFormation(FormationKind kind)
        {
            _formation = kind;
            _lastCommandStatus = $"Formation: {FormationPlanner.DisplayName(kind)}";
            AudioServiceLocator.Play2D(SoundEventId.UI_Select);
        }

        public void CycleFormation()
        {
            // Hotkey M: a selection without any armed unit (harvesters, builders) has no
            // formations — leave the setting alone instead of changing it invisibly.
            if (_runner != null && _runner.Entities != null && _selection != null)
            {
                MobileSelection(out int mobileCount);
                CombatSelection(out int armedCount);
                if (mobileCount > 0 && armedCount == 0)
                {
                    _lastCommandStatus = "Formation: nur für kampffähige Einheiten";
                    return;
                }
            }
            SetFormation((FormationKind)(((int)_formation + 1) % 5));
        }

        /// <summary>Arms the attack-move pick: the next LMB sends the selection there, fighting on the way.</summary>
        public void RequestAttackMoveOrder()
        {
            if (!EnsureDispatcher()) return;
            CombatSelection(out int armedCount);
            if (armedCount == 0)
            {
                _lastCommandStatus = "Angriffsbewegung: keine kampffähigen Einheiten ausgewählt";
                return;
            }
            ArmOrderPick(PendingOrder.AttackMove, "Attack-move");
        }

        /// <summary>Arms the patrol pick: the next LMB sets the far end, the units shuttle from where they stand.</summary>
        public void RequestPatrolOrder()
        {
            if (!EnsureDispatcher()) return;
            CombatSelection(out int armedCount);
            if (armedCount == 0)
            {
                _lastCommandStatus = "Patrouille: keine kampffähigen Einheiten ausgewählt";
                return;
            }
            ArmOrderPick(PendingOrder.Patrol, "Patrol");
        }

        /// <summary>Guard: every selected unit holds the spot it stands on.</summary>
        public void OrderGuard()
        {
            if (!EnsureDispatcher()) return;
            ReadOnlySpan<EntityId> mobile = CombatSelection(out int count);
            if (count == 0)
            {
                _lastCommandStatus = "Bewachen: keine kampffähigen Einheiten ausgewählt";
                return;
            }
            ClearPursuitAndMarkHarvesters(mobile);
            Report("Stop (guard)", _dispatcher.Stop(mobile));
            for (int i = 0; i < count; i++)
            {
                if (_runner.Entities.TryGetUnit(mobile[i], out UnitState u)) _standing.SetGuard(mobile[i], Ground(in u));
            }
            _lastCommandStatus = $"Bewachen: {count} Einheit(en) halten ihre Stellung";
        }

        /// <summary>Scatter: every selected unit jumps a few cells away from the group's centre.</summary>
        public void OrderScatter()
        {
            if (!EnsureDispatcher()) return;
            ReadOnlySpan<EntityId> mobile = CombatSelection(out int count);
            if (count == 0)
            {
                _lastCommandStatus = "Verteilen: keine kampffähigen Einheiten ausgewählt";
                return;
            }
            ClearPursuitAndMarkHarvesters(mobile);
            Vector2 centre = Vector2.zero;
            int live = 0;
            for (int i = 0; i < count; i++)
            {
                if (!_runner.Entities.TryGetUnit(mobile[i], out UnitState u)) continue;
                centre += Ground(in u);
                live++;
            }
            if (live == 0) return;
            centre /= live;
            for (int i = 0; i < count; i++)
            {
                if (!_runner.Entities.TryGetUnit(mobile[i], out UnitState u)) continue;
                Vector2 p = Ground(in u);
                Vector2 d = p - centre;
                if (d.sqrMagnitude < 0.04f)
                {
                    float a = i * 2.39996f;   // golden angle: deterministic, evenly spread
                    d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                }
                Vector2 to = ClampToMap(p + d.normalized * (2.5f + 0.4f * (i % 3)));
                SendMoveQuiet(mobile[i], to);
            }
            _lastCommandStatus = $"Verteilen: {live} Einheit(en)";
        }

        /// <summary>Move the selection to a world point now (the RMB path), honouring the formation.</summary>
        public void OrderMoveAt(Vector3 at)
        {
            if (!EnsureDispatcher()) return;
            ResolveMoveAt(at);
        }

        /// <summary>Attack-move the selection to a world point now (the armed pick's resolution).</summary>
        public void OrderAttackMoveAt(Vector3 at)
        {
            if (!EnsureDispatcher()) return;
            ResolveAttackMoveAt(at);
        }

        /// <summary>Patrol the selection between where it stands and a world point.</summary>
        public void OrderPatrolAt(Vector3 at)
        {
            if (!EnsureDispatcher()) return;
            ResolvePatrolAt(at);
        }

        // ----------------------------------------------------------------
        // Hooks called from the main file
        // ----------------------------------------------------------------

        /// <summary>Group move honouring the selected formation; Loose leaves the spread to the simulation.</summary>
        private void IssueGroupMove(ReadOnlySpan<EntityId> mobile, Vector3 moveTo)
        {
            // Formations are a combat concept: a harvester/builder-only selection moves plainly.
            CombatSelection(out int armedCount);
            if (armedCount > 0 && TryPlanFormation(mobile, moveTo))
            {
                for (int i = 0; i < _formationMembers.Count; i++) SendMoveQuiet(_formationMembers[i].Id, _formationSlots[i]);
                _lastCommandStatus = $"Move ({FormationPlanner.DisplayName(_formation)}, {_formationMembers.Count} Einheiten)";
                return;
            }
            Report("Move", _dispatcher.MoveTo(mobile, moveTo.x, moveTo.z));
        }

        /// <summary>Attack-move to a ground point: each unit drives to its slot and engages what it meets.</summary>
        private void ResolveAttackMoveAt(Vector3 at)
        {
            ReadOnlySpan<EntityId> mobile = CombatSelection(out int count);
            if (count == 0)
            {
                _lastCommandStatus = "Angriffsbewegung: keine kampffähigen Einheiten ausgewählt";
                return;
            }
            ClearPursuitAndMarkHarvesters(mobile);
            _focusedAttackTarget = EntityId.Invalid;
            AssignDestinations(mobile, at, (id, from, to) => _standing.SetAttackMove(id, to));
            _standingNextTime = 0f;   // first round now, not in half a second
            _markerView?.SpawnClickFeedback(at, ClickFeedbackKind.Attack);
            _lastCommandStatus = $"Angriffsbewegung: {count} Einheit(en)";
        }

        private void ResolvePatrolAt(Vector3 at)
        {
            ReadOnlySpan<EntityId> mobile = CombatSelection(out int count);
            if (count == 0)
            {
                _lastCommandStatus = "Patrouille: keine kampffähigen Einheiten ausgewählt";
                return;
            }
            ClearPursuitAndMarkHarvesters(mobile);
            _focusedAttackTarget = EntityId.Invalid;
            AssignDestinations(mobile, at, (id, from, to) => _standing.SetPatrol(id, from, to));
            _standingNextTime = 0f;
            _markerView?.SpawnClickFeedback(at, ClickFeedbackKind.Move);
            _lastCommandStatus = $"Patrouille: {count} Einheit(en)";
        }

        /// <summary>Per-unit destinations: formation slots when a formation is set, else the click itself.</summary>
        private void AssignDestinations(ReadOnlySpan<EntityId> mobile, Vector3 at, Action<EntityId, Vector2, Vector2> assign)
        {
            bool shaped = TryPlanFormation(mobile, at);
            if (shaped)
            {
                for (int i = 0; i < _formationMembers.Count; i++)
                {
                    assign(_formationMembers[i].Id, _formationMembers[i].Position, _formationSlots[i]);
                }
                return;
            }
            var target = new Vector2(at.x, at.z);
            for (int i = 0; i < mobile.Length; i++)
            {
                if (_runner.Entities.TryGetUnit(mobile[i], out UnitState u)) assign(mobile[i], Ground(in u), target);
            }
        }

        private bool TryPlanFormation(ReadOnlySpan<EntityId> mobile, Vector3 at)
        {
            _formationMembers.Clear();
            for (int i = 0; i < mobile.Length; i++)
            {
                if (!_runner.Entities.TryGetUnit(mobile[i], out UnitState u)) continue;
                _formationMembers.Add(new FormationMember(mobile[i], Ground(in u), MovementSpacing.RadiusOf(in u).ToFloat()));
            }
            return FormationPlanner.Plan(_formation, _formationMembers, new Vector2(at.x, at.z),
                Vector2.zero, MapMax(), _formationSlots);
        }

        private void UpdateStandingOrders()
        {
            if (_standing.Count == 0 || Time.time < _standingNextTime) return;
            _standingNextTime = Time.time + StandingOrders.IntervalSeconds;
            if (_standingWorld == null) _standingWorld = new StandingOrderWorld(this);

            _standingActions.Clear();
            _standing.Tick(_standingWorld, _standingActions);
            for (int i = 0; i < _standingActions.Count; i++)
            {
                StandingAction a = _standingActions[i];
                switch (a.Kind)
                {
                    case StandingActionKind.Move:
                        SendMoveQuiet(a.Unit, a.Point);
                        break;
                    case StandingActionKind.Return:
                        _pursuitTargets.Remove(a.Unit);
                        _singleScratch[0] = a.Unit;
                        _dispatcher.Stop(_singleScratch);
                        SendMoveQuiet(a.Unit, a.Point);
                        break;
                    case StandingActionKind.Attack:
                        SendAttackQuiet(a.Unit, a.Target);
                        break;
                }
            }
        }

        // ----------------------------------------------------------------
        // Quiet dispatch: standing orders must not overwrite the status line
        // or the focused-target ring the player set by hand.
        // ----------------------------------------------------------------

        private void SendMoveQuiet(EntityId unit, Vector2 to)
        {
            _pursuitTargets.Remove(unit);
            _singleScratch[0] = unit;
            _dispatcher.MoveTo(_singleScratch, to.x, to.y);
        }

        private void SendAttackQuiet(EntityId unit, EntityId enemy)
        {
            EntityManager entities = _runner.Entities;
            if (!entities.TryGetUnit(unit, out UnitState attacker) || !entities.TryGetUnit(enemy, out UnitState target)) return;
            _pursuitTargets[unit] = enemy;
            _singleScratch[0] = unit;
            WeaponProfile weapon = WeaponProfiles.Get(_runner.Economy.GetSlotFaction(_dispatcher.LocalSlot), attacker.Role);
            if (!IsWithinWeaponRange(in attacker, in target, weapon.AttackRange))
            {
                _dispatcher.MoveTo(_singleScratch,
                    Nova.Core.SimFixed.FromInt(GridCellOf(target.Transform.PositionX)),
                    Nova.Core.SimFixed.FromInt(GridCellOf(target.Transform.PositionY)));
            }
            _dispatcher.Attack(_singleScratch, enemy);
        }

        private static Vector2 Ground(in UnitState u)
        {
            return new Vector2(u.Transform.PositionX.ToFloat(), u.Transform.PositionY.ToFloat());
        }

        private Vector2 MapMax()
        {
            var field = _runner.Pathfinding.CostField;
            return new Vector2(field.Width - 0.5f, field.Height - 0.5f);
        }

        private Vector2 ClampToMap(Vector2 p)
        {
            Vector2 max = MapMax();
            return new Vector2(Mathf.Clamp(p.x, 0.5f, max.x), Mathf.Clamp(p.y, 0.5f, max.y));
        }

        /// <summary>The order layer's view of the match: own units, and enemies only where the fog shows them.</summary>
        private sealed class StandingOrderWorld : IStandingOrderWorld
        {
            private readonly RtsDeviceInput _input;

            public StandingOrderWorld(RtsDeviceInput input) => _input = input;

            public bool TryGetOwnUnit(EntityId id, out UnitState unit)
            {
                return _input._runner.Entities.TryGetUnit(id, out unit)
                       && unit.PlayerId == _input._dispatcher.LocalSlot
                       && !SimDefinitions.IsBuildingRole(unit.Role);
            }

            public float WeaponRange(in UnitState unit)
            {
                FactionId faction = _input._runner.Economy.GetSlotFaction(_input._dispatcher.LocalSlot);
                WeaponProfile weapon = WeaponProfiles.Get(faction, unit.Role);
                return weapon.IsArmed ? weapon.AttackRange.ToFloat() : 0f;
            }

            public bool IsAliveAndVisible(EntityId enemy)
            {
                return _input._runner.Entities.TryGetUnit(enemy, out UnitState u)
                       && _input.IsForeignEntityVisibleToLocalSlot(in u);
            }

            public bool TryFindVisibleEnemy(Vector2 centre, float radius, out EntityId enemy)
            {
                enemy = EntityId.Invalid;
                EntityManager entities = _input._runner.Entities;
                byte slot = _input._dispatcher.LocalSlot;
                UnitState[] units = entities.RawUnits;
                float best = radius * radius;
                for (int i = 0; i < entities.Capacity; i++)
                {
                    ref readonly UnitState u = ref units[i];
                    if (!u.IsActive || u.PlayerId == slot || u.Role == UnitRole.Unit) continue;
                    float dx = u.Transform.PositionX.ToFloat() - centre.x;
                    float dy = u.Transform.PositionY.ToFloat() - centre.y;
                    float d = dx * dx + dy * dy;
                    if (d > best) continue;
                    if (!_input.IsForeignEntityVisibleToLocalSlot(in u)) continue;
                    best = d;
                    enemy = u.Id;
                }
                return enemy.IsValid;
            }
        }
    }
}
