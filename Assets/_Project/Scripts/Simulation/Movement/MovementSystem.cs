using System;
using Nova.Core;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;

namespace Nova.Simulation.Movement
{
    /// <summary>
    /// Deterministic simulation system for unit movement and steering.
    /// Combines Flow-Field direction vectors with O(N) spatial grid separation steering.
    /// Runs on the canonical fixed tick rate (<see cref="SimClock.TicksPerSecond"/> = 10 Hz).
    /// <para>
    /// Every unit follows the flow field of its OWN
    /// <see cref="UnitState.TargetGridPos"/>, looked up in the pathfinding
    /// system's bounded multi-destination cache. Reading one shared global
    /// field instead would make any new Move command retarget every already
    /// moving unit on the map. The ARRIVAL cell is the personal
    /// <see cref="UnitState.GoalGridPos"/> instead: a group move shares one
    /// flow destination and distributes goal cells (formation), so the group
    /// costs one cache entry and still unstacks into a line.
    /// </para>
    /// <para>
    /// Separation steering runs for ALL active mobile units, moving or
    /// standing. A moving unit combines flow and separation into its heading;
    /// a standing unit applies a damped, capped, dead-zoned positional
    /// correction only (no heading, no rotation change), so arrived units
    /// unstack without vibrating. Units never enter impassable cells —
    /// buildings occupy the cost field once construction feeds it.
    /// </para>
    /// <para>
    /// Q-040(i) resolution (implemented, ratification pending): the whole
    /// tick path computes in canonical fixed-point — <see cref="SimFixed"/>
    /// positions/speeds, <see cref="SimAngle"/> headings and the purely
    /// integer <see cref="SimTrig"/> transcendentals. No float or double
    /// remains in the hash-relevant movement path, so the results are
    /// bit-identical across Mono/IL2CPP/.NET by construction
    /// (docs/tech/SimulationCore.md sections 1 and 9).
    /// </para>
    /// <para>
    /// Tick delta encoding: 0.1 s is not exactly representable in Q16.16
    /// (6553.6 raw). Instead of rounding the delta itself, the per-tick step
    /// is computed as speed / <see cref="SimClock.TicksPerSecond"/> — the
    /// factor 1/10 is exact and the single division rounds once, ties-to-even
    /// (max 0.5 raw units per tick step), which is the most exact encoding of
    /// the 10 Hz contract available in Q16.16.
    /// </para>
    /// <para>
    /// Stateful (<see cref="IStatefulSimSystem"/>): the movement system owns
    /// the authoritative entity store block in kernel snapshots — unit state,
    /// generations and the free list live in the injected
    /// <see cref="EntityManager"/>; this system only delegates the canonical
    /// serialization. The spatial binning grids are per-tick scratch memory
    /// rebuilt inside <see cref="ExecuteTick"/> and carry no state.
    /// </para>
    /// </summary>
    public sealed class MovementSystem : IStatefulSimSystem
    {
        /// <summary>Exact per-tick divisor of the canonical 10 Hz clock (see class remarks).</summary>
        private static readonly SimFixed TicksPerSecond = SimFixed.FromInt(SimClock.TicksPerSecond);

        /// <summary>Half a grid cell, exact in Q16.16 (target cell centers).</summary>
        private static readonly SimFixed HalfCell = SimFixed.FromRaw(SimFixed.OneRaw / 2);

        /// <summary>Separation steering weight (0.5, exact in Q16.16).</summary>
        private static readonly SimFixed SeparationWeight = SimFixed.FromRaw(SimFixed.OneRaw / 2);

        /// <summary>
        /// Damping of the standing separation (0.5, exact in Q16.16): two
        /// stacked standing units each correct half the overlap per tick, so
        /// a pair converges exactly onto the contact distance instead of
        /// overshooting into vibration.
        /// </summary>
        private static readonly SimFixed StandingSeparationWeight = SimFixed.FromRaw(SimFixed.OneRaw / 2);

        /// <summary>
        /// Maximum positional correction of a standing unit per tick
        /// (0.25 m, exact in Q16.16): bounds the unstacking step inside
        /// dense clusters, where the summed overlap vector can be large.
        /// </summary>
        private static readonly SimFixed MaxStandingStep = SimFixed.FromRaw(SimFixed.OneRaw / 4);

        /// <summary>
        /// Dead-zone for the combined steering vector, squared (~1.07e-4,
        /// the Q16.16 rounding of the prototype's 1e-4 float threshold).
        /// </summary>
        private static readonly SimFixed MinSteeringLengthSquared = SimFixed.FromRaw(7);

        /// <summary>
        /// Heading turn limit per tick (30 degrees, 5461 of 65536 angle units;
        /// D-116): an 8-way flow snap turns over two ticks, a reversal over
        /// six, instead of flipping in one.
        /// </summary>
        private const int MaxTurnUnitsPerTick = 5461;

        /// <summary>
        /// Below this squared length (0.1 cells) the path vector near the goal
        /// is too short to define a heading; the unit keeps the one it has.
        /// </summary>
        private static readonly SimFixed MinHeadingLengthSquared = SimFixed.FromRaw(SimFixed.OneRaw / 100);

        /// <summary>
        /// Neighbour query half-width in cells: spacing radii reach 0.78
        /// (<see cref="MovementSpacing.MaxRadius"/>), so a pair can touch at
        /// 1.56 cells — beyond the old 3x3 window.
        /// </summary>
        private const int QueryCells = 2;

        /// <summary>How far ahead of a mover a standing friend starts to make way (on top of both radii).</summary>
        private static readonly SimFixed YieldLookAhead = SimFixed.FromRaw(SimFixed.OneRaw * 6 / 5);

        /// <summary>Lateral clearance a yielding unit opens beyond both radii (0.15 cells).</summary>
        private static readonly SimFixed YieldMargin = SimFixed.FromRaw((SimFixed.OneRaw * 15 + 50) / 100);

        /// <summary>Share of the missing clearance a yielding unit steps per tick (0.6).</summary>
        private static readonly SimFixed YieldGain = SimFixed.FromRaw((SimFixed.OneRaw * 3) / 5);

        /// <summary>
        /// Weight of a yielding friend's separation on the mover (0.25): the
        /// mover keeps its line and the group parts around it, instead of both
        /// pushing each other off course.
        /// </summary>
        private static readonly SimFixed YielderSeparationWeight = SimFixed.FromRaw(SimFixed.OneRaw / 4);

        private readonly EntityManager _entityManager;
        private readonly PathfindingSystem _pathfindingSystem;

        private readonly int[] _gridHeads;
        private readonly int[] _unitNexts;
        private readonly ushort _gridWidth;
        private readonly ushort _gridHeight;

        public string Name => "MovementSystem";

        public MovementSystem(EntityManager entityManager, PathfindingSystem pathfindingSystem)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _pathfindingSystem = pathfindingSystem ?? throw new ArgumentNullException(nameof(pathfindingSystem));

            _gridWidth = pathfindingSystem.CostField.Width;
            _gridHeight = pathfindingSystem.CostField.Height;
            _gridHeads = new int[_gridWidth * _gridHeight];
            _unitNexts = new int[entityManager.Capacity];
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo($"[{Name}] Initialized movement system with Spatial Binning ({_gridWidth}x{_gridHeight}).");
        }

        public void ExecuteTick(Tick tick)
        {
            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;

            // Step 1: Reset Spatial Binning Grid
            Array.Fill(_gridHeads, -1);

            // Step 2: Bin Active Units into Spatial Grid Buckets
            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive) continue;

                // Canonical world-to-grid mapping: floor, also for negative values.
                ushort gx = (ushort)Math.Max(0, Math.Min(_gridWidth - 1, SimFixed.WorldToGrid(u.Transform.PositionX)));
                ushort gy = (ushort)Math.Max(0, Math.Min(_gridHeight - 1, SimFixed.WorldToGrid(u.Transform.PositionY)));
                int cellIndex = gy * _gridWidth + gx;

                _unitNexts[i] = _gridHeads[cellIndex];
                _gridHeads[cellIndex] = i;
            }

            // Step 3: Movement & Separation Steering
            for (int i = 0; i < capacity; i++)
            {
                ref UnitState unit = ref units[i];
                if (!unit.IsActive) continue;
                // Immobile entities (buildings, construction sites: MoveSpeed
                // 0) occupy bins above, so mobile units keep their distance —
                // but they are never pushed themselves.
                if (unit.MoveSpeed <= SimFixed.Zero) continue;

                SimFixed curX = unit.Transform.PositionX;
                SimFixed curY = unit.Transform.PositionY;

                ushort gridX = (ushort)Math.Max(0, Math.Min(_gridWidth - 1, SimFixed.WorldToGrid(curX)));
                ushort gridY = (ushort)Math.Max(0, Math.Min(_gridHeight - 1, SimFixed.WorldToGrid(curY)));

                bool moving = unit.IsMoving && unit.GoalGridPos.IsValid;

                // Arrival check against the unit's PERSONAL goal cell. A
                // goal cell that turned impassable after the order (a
                // building placed on it) counts as reached once no walkable
                // neighbour is strictly closer — the unit stops at the wall
                // instead of pushing against it forever.
                if (moving)
                {
                    bool atGoal = gridX == unit.GoalGridPos.X && gridY == unit.GoalGridPos.Y;
                    if (atGoal || IsGoalUnreachable(unit.GoalGridPos, gridX, gridY))
                    {
                        unit.Stop();
                        continue;
                    }
                }

                SimFixed moveDx = SimFixed.Zero;
                SimFixed moveDy = SimFixed.Zero;
                if (moving)
                {
                    // Query the flow direction of THIS unit's own destination.
                    // Pure cache lookup, never a generation: a miss (destination
                    // evicted from the bounded cache) yields Direction2D.None and
                    // falls through to the direct-steering path below.
                    FlowField field = _pathfindingSystem.GetField(unit.TargetGridPos);
                    Direction2D flowDir = field != null
                        ? field.GetDirection(gridX, gridY)
                        : Direction2D.None;
                    var (flowDx, flowDy) = Direction2DUtility.GetOffset(flowDir);

                    moveDx = SimFixed.FromInt(flowDx);
                    moveDy = SimFixed.FromInt(flowDy);

                    // If at flow end or blocked, move directly towards the
                    // personal goal cell center.
                    if (flowDir == Direction2D.None)
                    {
                        moveDx = (SimFixed.FromInt(unit.GoalGridPos.X) + HalfCell) - curX;
                        moveDy = (SimFixed.FromInt(unit.GoalGridPos.Y) + HalfCell) - curY;
                    }
                }

                // O(1) local 5x5 spatial-grid neighbourhood separation —
                // computed for moving and standing units alike, so an
                // arrived unit still makes room (and can be pushed).
                // Contact distance uses the per-role spacing radius (D-116).
                SimFixed sepDx = SimFixed.Zero;
                SimFixed sepDy = SimFixed.Zero;
                SimFixed yieldDx = SimFixed.Zero;
                SimFixed yieldDy = SimFixed.Zero;
                SimFixed myRadius = MovementSpacing.RadiusOf(in unit);

                int minGx = Math.Max(0, gridX - QueryCells);
                int maxGx = Math.Min(_gridWidth - 1, gridX + QueryCells);
                int minGy = Math.Max(0, gridY - QueryCells);
                int maxGy = Math.Min(_gridHeight - 1, gridY + QueryCells);

                for (int gy = minGy; gy <= maxGy; gy++)
                {
                    for (int gx = minGx; gx <= maxGx; gx++)
                    {
                        int otherIndex = _gridHeads[gy * _gridWidth + gx];
                        while (otherIndex != -1)
                        {
                            if (otherIndex != i)
                            {
                                ref readonly UnitState other = ref units[otherIndex];
                                SimFixed distSq = unit.Transform.DistanceToSquared(in other.Transform);
                                SimFixed otherRadius = MovementSpacing.RadiusOf(in other);
                                SimFixed minDist = myRadius + otherRadius;

                                // A standing friend no bigger than me makes
                                // way (below); while I drive, its push on me
                                // is weak, so I keep my line through the group.
                                SimFixed weight = SimFixed.One;
                                if (moving && !other.IsMoving && other.MoveSpeed > SimFixed.Zero
                                    && other.PlayerId == unit.PlayerId && otherRadius <= myRadius)
                                {
                                    weight = YielderSeparationWeight;
                                }
                                if (!moving)
                                {
                                    AccumulateYield(in unit, myRadius, in other, otherRadius, otherIndex > i,
                                        ref yieldDx, ref yieldDy);
                                }

                                if (distSq == SimFixed.Zero)
                                {
                                    // Exact overlap (same position, e.g. a
                                    // stacked spawn): the push direction is
                                    // undefined, so break the tie by entity
                                    // index — the higher index yields in +x.
                                    // Deterministic across hosts (indices are
                                    // canonical state).
                                    sepDx += (otherIndex > i ? -minDist : minDist) * weight;
                                }
                                else if (distSq < minDist * minDist)
                                {
                                    SimFixed dist = SimTrig.Sqrt(distSq);
                                    SimFixed pushFactor = (minDist - dist) / dist * weight;
                                    sepDx += (curX - other.Transform.PositionX) * pushFactor;
                                    sepDy += (curY - other.Transform.PositionY) * pushFactor;
                                }
                            }

                            otherIndex = _unitNexts[otherIndex];
                        }
                    }
                }

                if (moving)
                {
                    // Combine flow vector and separation
                    SimFixed finalDx = moveDx + sepDx * SeparationWeight;
                    SimFixed finalDy = moveDy + sepDy * SeparationWeight;

                    SimFixed lenSq = finalDx * finalDx + finalDy * finalDy;
                    if (lenSq > MinSteeringLengthSquared)
                    {
                        SimFixed len = SimTrig.Sqrt(lenSq);
                        finalDx /= len;
                        finalDy /= len;

                        // Exact 1/10 s per tick: divide by the tick rate once
                        // (ties-to-even) instead of multiplying by a rounded
                        // 0.1 s constant (see class remarks).
                        SimFixed step = unit.MoveSpeed / TicksPerSecond;
                        SimFixed nextX = curX + finalDx * step;
                        SimFixed nextY = curY + finalDy * step;
                        // Heading follows the PATH, not path plus separation:
                        // in a crowd the summed push could flip a unit by up
                        // to 180 degrees per tick. Turn-rate limited (D-116).
                        SimAngle rotation = unit.Transform.Rotation;
                        if (moveDx * moveDx + moveDy * moveDy > MinHeadingLengthSquared)
                        {
                            rotation = TurnToward(rotation, SimTrig.Atan2(moveDy, moveDx));
                        }

                        // Never step into an impassable cell (a building
                        // footprint, once construction feeds the cost field):
                        // full step first, then the axis-decomposed fallbacks
                        // so a diagonal past a wall corner cannot stall.
                        if (IsWalkablePosition(nextX, nextY))
                        {
                            unit.Transform = new Transform2D(nextX, nextY, rotation);
                        }
                        else if (IsWalkablePosition(nextX, curY))
                        {
                            unit.Transform = new Transform2D(nextX, curY, rotation);
                        }
                        else if (IsWalkablePosition(curX, nextY))
                        {
                            unit.Transform = new Transform2D(curX, nextY, rotation);
                        }
                        // Fully blocked: hold position and retry next tick.
                    }
                }
                else
                {
                    // Standing separation: a pure positional correction — the
                    // raw overlap vector, damped, capped and dead-zoned. The
                    // push approaches zero as the overlap closes, so arrived
                    // units unstack without vibrating; rotation is untouched.
                    SimFixed pushDx = sepDx * StandingSeparationWeight + yieldDx;
                    SimFixed pushDy = sepDy * StandingSeparationWeight + yieldDy;

                    SimFixed lenSq = pushDx * pushDx + pushDy * pushDy;
                    if (lenSq > MinSteeringLengthSquared)
                    {
                        SimFixed len = SimTrig.Sqrt(lenSq);
                        if (len > MaxStandingStep)
                        {
                            pushDx = pushDx * MaxStandingStep / len;
                            pushDy = pushDy * MaxStandingStep / len;
                        }

                        SimFixed nextX = curX + pushDx;
                        SimFixed nextY = curY + pushDy;
                        if (IsWalkablePosition(nextX, nextY))
                        {
                            unit.Transform = new Transform2D(nextX, nextY, unit.Transform.Rotation);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Make way (D-116): a standing unit that sits in the corridor ahead of
        /// a moving friend at least its own size steps sideways, away from the
        /// mover's line, by a share of the clearance still missing. The mover's
        /// heading is its path direction, so the corridor points where it is
        /// going. A unit dead on the line steps to a side fixed by entity
        /// order, so two hosts always part the same way.
        /// </summary>
        private static void AccumulateYield(in UnitState me, SimFixed myRadius, in UnitState mover, SimFixed moverRadius,
                                            bool moverIsHigherIndex, ref SimFixed yieldDx, ref SimFixed yieldDy)
        {
            if (!mover.IsMoving || mover.MoveSpeed <= SimFixed.Zero) return;
            if (mover.PlayerId != me.PlayerId) return;
            if (moverRadius < myRadius) return;

            SimFixed dirX = SimTrig.Cos(mover.Transform.Rotation);
            SimFixed dirY = SimTrig.Sin(mover.Transform.Rotation);
            SimFixed relX = me.Transform.PositionX - mover.Transform.PositionX;
            SimFixed relY = me.Transform.PositionY - mover.Transform.PositionY;

            SimFixed along = relX * dirX + relY * dirY;
            SimFixed reach = moverRadius + myRadius + YieldLookAhead;
            if (along <= SimFixed.Zero || along >= reach) return;

            SimFixed lateral = relX * -dirY + relY * dirX;   // signed distance off the line
            SimFixed clearance = moverRadius + myRadius + YieldMargin;
            SimFixed absLateral = lateral < SimFixed.Zero ? -lateral : lateral;
            if (absLateral >= clearance) return;

            SimFixed side = lateral > SimFixed.Zero ? SimFixed.One
                : lateral < SimFixed.Zero ? -SimFixed.One
                : (moverIsHigherIndex ? SimFixed.One : -SimFixed.One);
            // Closer ahead of the mover means more urgent.
            SimFixed urgency = SimFixed.One - along / reach;
            SimFixed step = (clearance - absLateral) * YieldGain * urgency * side;
            yieldDx += -dirY * step;
            yieldDy += dirX * step;
        }

        /// <summary>Turns <paramref name="from"/> toward <paramref name="to"/> by at most <see cref="MaxTurnUnitsPerTick"/>.</summary>
        private static SimAngle TurnToward(SimAngle from, SimAngle to)
        {
            int delta = (short)unchecked((ushort)(to.RawValue - from.RawValue));
            if (delta > MaxTurnUnitsPerTick) delta = MaxTurnUnitsPerTick;
            else if (delta < -MaxTurnUnitsPerTick) delta = -MaxTurnUnitsPerTick;
            return SimAngle.FromRaw(unchecked((ushort)(from.RawValue + delta)));
        }

        /// <summary>True when the world position lies inside the map on a walkable cell.</summary>
        private bool IsWalkablePosition(SimFixed x, SimFixed y)
        {
            int gx = SimFixed.WorldToGrid(x);
            int gy = SimFixed.WorldToGrid(y);
            if (gx < 0 || gy < 0 || gx >= _gridWidth || gy >= _gridHeight) return false;
            return _pathfindingSystem.CostField.IsWalkable((ushort)gx, (ushort)gy);
        }

        /// <summary>
        /// True when the goal cell is impassable and no walkable neighbour
        /// cell is strictly closer to it (Chebyshev, matching the 8-way
        /// movement): the unit has reached the wall around its goal.
        /// </summary>
        private bool IsGoalUnreachable(GridPos2D goal, int gridX, int gridY)
        {
            if (_pathfindingSystem.CostField.IsWalkable(goal.X, goal.Y)) return false;
            int current = Math.Max(Math.Abs(gridX - goal.X), Math.Abs(gridY - goal.Y));
            foreach (Direction2D dir in Direction2DUtility.AllCardinalAndDiagonal)
            {
                var (dx, dy) = Direction2DUtility.GetOffset(dir);
                int nx = gridX + dx;
                int ny = gridY + dy;
                if (nx < 0 || ny < 0 || nx >= _gridWidth || ny >= _gridHeight) continue;
                if (!_pathfindingSystem.CostField.IsWalkable((ushort)nx, (ushort)ny)) continue;
                int closer = Math.Max(Math.Abs(nx - goal.X), Math.Abs(ny - goal.Y));
                if (closer < current) return false;
            }
            return true;
        }

        public void Shutdown()
        {
        }

        /// <summary>Snapshot block id of the entity store (registry: <see cref="Snapshots.SnapshotBlockIds"/>).</summary>
        public ushort StateBlockId => Snapshots.SnapshotBlockIds.EntityStore;

        /// <summary>Delegates the canonical entity store serialization to the owned <see cref="EntityManager"/>.</summary>
        public void WriteState(Snapshots.SnapshotBlockWriter writer)
        {
            _entityManager.WriteState(writer);
        }

        /// <summary>Fully validates an entity store block without mutating the manager.</summary>
        public bool TryValidateState(ReadOnlySpan<byte> blockContent)
        {
            return _entityManager.TryValidateState(blockContent);
        }

        /// <summary>Restores the entity store; malformed input leaves the manager untouched.</summary>
        public bool TryRestoreState(ReadOnlySpan<byte> blockContent)
        {
            return _entityManager.TryRestoreState(blockContent);
        }
    }
}
