using System;
using Nova.Core;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;

namespace Nova.Simulation.Movement
{
    /// <summary>
    /// Deterministic simulation system for unit movement and steering.
    /// Combines Flow-Field direction vectors with O(N) spatial grid separation steering.
    /// Runs on fixed tick delta (0.05 seconds = 20 ticks/sec).
    /// </summary>
    public sealed class MovementSystem : ISimSystem
    {
        public const float TickDeltaSeconds = 0.05f; // 20 Ticks / sec

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

                ushort gx = (ushort)Math.Max(0, Math.Min(_gridWidth - 1, (int)Math.Floor(u.Transform.PositionX)));
                ushort gy = (ushort)Math.Max(0, Math.Min(_gridHeight - 1, (int)Math.Floor(u.Transform.PositionY)));
                int cellIndex = gy * _gridWidth + gx;

                _unitNexts[i] = _gridHeads[cellIndex];
                _gridHeads[cellIndex] = i;
            }

            // Step 3: Movement & Separation Steering
            for (int i = 0; i < capacity; i++)
            {
                ref UnitState unit = ref units[i];
                if (!unit.IsActive || !unit.IsMoving) continue;

                float curX = unit.Transform.PositionX;
                float curY = unit.Transform.PositionY;

                ushort gridX = (ushort)Math.Max(0, Math.Min(_gridWidth - 1, (int)Math.Floor(curX)));
                ushort gridY = (ushort)Math.Max(0, Math.Min(_gridHeight - 1, (int)Math.Floor(curY)));

                // Arrival check
                if (gridX == unit.TargetGridPos.X && gridY == unit.TargetGridPos.Y)
                {
                    unit.Stop();
                    continue;
                }

                // Query flow direction vector
                Direction2D flowDir = _pathfindingSystem.FlowField.GetDirection(gridX, gridY);
                var (flowDx, flowDy) = Direction2DUtility.GetOffset(flowDir);

                float moveDx = flowDx;
                float moveDy = flowDy;

                // If at flow end or blocked, move directly towards target cell center
                if (flowDir == Direction2D.None)
                {
                    moveDx = (unit.TargetGridPos.X + 0.5f) - curX;
                    moveDy = (unit.TargetGridPos.Y + 0.5f) - curY;
                }

                // O(1) Local 3x3 Spatial Grid Neighborhood Separation
                float sepDx = 0f;
                float sepDy = 0f;

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
                                float distSq = unit.Transform.DistanceToSquared(in other.Transform);
                                float minDist = unit.Radius + other.Radius;

                                if (distSq > 0f && distSq < minDist * minDist)
                                {
                                    float dist = SimMath.Sqrt(distSq);
                                    float pushFactor = (minDist - dist) / dist;
                                    sepDx += (curX - other.Transform.PositionX) * pushFactor;
                                    sepDy += (curY - other.Transform.PositionY) * pushFactor;
                                }
                            }

                            otherIndex = _unitNexts[otherIndex];
                        }
                    }
                }

                // Combine flow vector and separation
                float finalDx = moveDx + sepDx * 0.5f;
                float finalDy = moveDy + sepDy * 0.5f;

                float lenSq = finalDx * finalDx + finalDy * finalDy;
                if (lenSq > 0.0001f)
                {
                    float len = SimMath.Sqrt(lenSq);
                    finalDx /= len;
                    finalDy /= len;

                    float step = unit.MoveSpeed * TickDeltaSeconds;
                    float nextX = curX + finalDx * step;
                    float nextY = curY + finalDy * step;
                    float rotation = SimMath.Atan2(finalDy, finalDx);

                    unit.Transform = new Transform2D(nextX, nextY, rotation);
                }
            }
        }

        public void Shutdown()
        {
        }
    }
}
