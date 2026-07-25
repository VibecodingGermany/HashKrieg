using System;

namespace Nova.Simulation.Pathfinding
{
    /// <summary>
    /// Stores the 8-way directional movement vector field generated from an IntegrationField.
    /// Units query this field at their cell coordinates to receive instantaneous, non-colliding flow vectors.
    /// </summary>
    public sealed class FlowField
    {
        private readonly Direction2D[] _directions;

        public ushort Width { get; }
        public ushort Height { get; }

        public FlowField(ushort width, ushort height)
        {
            Width = width;
            Height = height;
            _directions = new Direction2D[width * height];
        }

        public Direction2D GetDirection(ushort x, ushort y)
        {
            if (x >= Width || y >= Height) return Direction2D.None;
            return _directions[y * Width + x];
        }

        /// <summary>
        /// Derives the optimal 8-way flow direction vector for every cell based on neighbor integration values.
        /// Zero allocations during calculation.
        /// </summary>
        public void Generate(IntegrationField integrationField, CostField costField)
        {
            if (integrationField == null) throw new ArgumentNullException(nameof(integrationField));
            if (costField == null) throw new ArgumentNullException(nameof(costField));

            for (ushort y = 0; y < Height; y++)
            {
                for (ushort x = 0; x < Width; x++)
                {
                    int cellIndex = y * Width + x;

                    // Obstacle or unreachable cell gets Direction2D.None
                    if (!costField.IsWalkable(x, y) || integrationField.GetDistance(x, y) == IntegrationField.Unreachable)
                    {
                        _directions[cellIndex] = Direction2D.None;
                        continue;
                    }

                    // Target destination cell
                    if (x == integrationField.Destination.X && y == integrationField.Destination.Y)
                    {
                        _directions[cellIndex] = Direction2D.None;
                        continue;
                    }

                    ushort bestDist = integrationField.GetDistance(x, y);
                    Direction2D bestDir = Direction2D.None;

                    foreach (Direction2D dir in Direction2DUtility.AllCardinalAndDiagonal)
                    {
                        var (dx, dy) = Direction2DUtility.GetOffset(dir);
                        int nX = x + dx;
                        int nY = y + dy;

                        if (!costField.IsInBounds(nX, nY)) continue;

                        ushort neighborDist = integrationField.GetDistance((ushort)nX, (ushort)nY);
                        if (neighborDist < bestDist)
                        {
                            bestDist = neighborDist;
                            bestDir = dir;
                        }
                    }

                    _directions[cellIndex] = bestDir;
                }
            }
        }
    }
}
