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
        /// Dead-zone for the combined steering vector, squared (~1.07e-4,
        /// the Q16.16 rounding of the prototype's 1e-4 float threshold).
        /// </summary>
        private static readonly SimFixed MinSteeringLengthSquared = SimFixed.FromRaw(7);

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
                if (!unit.IsActive || !unit.IsMoving) continue;

                SimFixed curX = unit.Transform.PositionX;
                SimFixed curY = unit.Transform.PositionY;

                ushort gridX = (ushort)Math.Max(0, Math.Min(_gridWidth - 1, SimFixed.WorldToGrid(curX)));
                ushort gridY = (ushort)Math.Max(0, Math.Min(_gridHeight - 1, SimFixed.WorldToGrid(curY)));

                // Arrival check
                if (gridX == unit.TargetGridPos.X && gridY == unit.TargetGridPos.Y)
                {
                    unit.Stop();
                    continue;
                }

                // Query flow direction vector
                Direction2D flowDir = _pathfindingSystem.FlowField.GetDirection(gridX, gridY);
                var (flowDx, flowDy) = Direction2DUtility.GetOffset(flowDir);

                SimFixed moveDx = SimFixed.FromInt(flowDx);
                SimFixed moveDy = SimFixed.FromInt(flowDy);

                // If at flow end or blocked, move directly towards target cell center
                if (flowDir == Direction2D.None)
                {
                    moveDx = (SimFixed.FromInt(unit.TargetGridPos.X) + HalfCell) - curX;
                    moveDy = (SimFixed.FromInt(unit.TargetGridPos.Y) + HalfCell) - curY;
                }

                // O(1) Local 3x3 Spatial Grid Neighborhood Separation
                SimFixed sepDx = SimFixed.Zero;
                SimFixed sepDy = SimFixed.Zero;

                int minGx = Math.Max(0, gridX - 1);
                int maxGx = Math.Min(_gridWidth - 1, gridX + 1);
                int minGy = Math.Max(0, gridY - 1);
                int maxGy = Math.Min(_gridHeight - 1, gridY + 1);

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
                                SimFixed minDist = unit.Radius + other.Radius;

                                if (distSq > SimFixed.Zero && distSq < minDist * minDist)
                                {
                                    SimFixed dist = SimTrig.Sqrt(distSq);
                                    SimFixed pushFactor = (minDist - dist) / dist;
                                    sepDx += (curX - other.Transform.PositionX) * pushFactor;
                                    sepDy += (curY - other.Transform.PositionY) * pushFactor;
                                }
                            }

                            otherIndex = _unitNexts[otherIndex];
                        }
                    }
                }

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
                    SimAngle rotation = SimTrig.Atan2(finalDy, finalDx);

                    unit.Transform = new Transform2D(nextX, nextY, rotation);
                }
            }
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
