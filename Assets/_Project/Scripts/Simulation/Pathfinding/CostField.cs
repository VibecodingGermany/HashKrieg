using System;

namespace Nova.Simulation.Pathfinding
{
    /// <summary>
    /// Stores movement cost values for each cell in a grid sector.
    /// Cost 1 = open terrain, 2..254 = high-cost terrain/rough ground, 255 = impassable obstacle.
    /// </summary>
    public sealed class CostField
    {
        public const byte OpenCost = 1;
        public const byte ImpassableCost = 255;

        private readonly byte[] _costs;

        public ushort Width { get; }
        public ushort Height { get; }

        public CostField(ushort width, ushort height)
        {
            if (width == 0 || height == 0)
            {
                throw new ArgumentException("CostField dimensions must be greater than zero.");
            }

            Width = width;
            Height = height;
            _costs = new byte[width * height];
            
            // Initialize all cells as open terrain
            Array.Fill(_costs, OpenCost);
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public byte GetCost(ushort x, ushort y)
        {
            if (!IsInBounds(x, y)) return ImpassableCost;
            return _costs[y * Width + x];
        }

        public void SetCost(ushort x, ushort y, byte cost)
        {
            if (IsInBounds(x, y))
            {
                _costs[y * Width + x] = cost;
            }
        }

        public bool IsWalkable(ushort x, ushort y)
        {
            return GetCost(x, y) < ImpassableCost;
        }

        public void ResetAll(byte defaultCost = OpenCost)
        {
            Array.Fill(_costs, defaultCost);
        }
    }
}
