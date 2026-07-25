using System;
using System.Collections.Generic;

namespace Nova.Simulation.Pathfinding
{
    /// <summary>
    /// Computes and stores the Dijkstra distance wave from a target grid cell.
    /// Non-allocating wave expansion per calculation run.
    /// </summary>
    public sealed class IntegrationField
    {
        public const ushort Unreachable = ushort.MaxValue;

        private readonly ushort[] _distances;
        private readonly Queue<GridPos2D> _waveQueue;

        public ushort Width { get; }
        public ushort Height { get; }
        public GridPos2D Destination { get; private set; }

        public IntegrationField(ushort width, ushort height)
        {
            Width = width;
            Height = height;
            _distances = new ushort[width * height];
            _waveQueue = new Queue<GridPos2D>(width * height);
            
            Array.Fill(_distances, Unreachable);
        }

        public ushort GetDistance(ushort x, ushort y)
        {
            if (x >= Width || y >= Height) return Unreachable;
            return _distances[y * Width + x];
        }

        /// <summary>
        /// Generates the Dijkstra integration wave starting from the destination cell.
        /// Zero allocations during generation.
        /// </summary>
        public void Generate(CostField costField, GridPos2D destination)
        {
            if (costField == null) throw new ArgumentNullException(nameof(costField));
            if (!costField.IsInBounds(destination.X, destination.Y)) return;

            Destination = destination;
            Array.Fill(_distances, Unreachable);
            _waveQueue.Clear();

            // Seed destination cell with 0 distance
            int destIndex = destination.Y * Width + destination.X;
            _distances[destIndex] = 0;
            _waveQueue.Enqueue(destination);

            // Dijkstra wave expansion
            while (_waveQueue.Count > 0)
            {
                GridPos2D current = _waveQueue.Dequeue();
                ushort currentDist = GetDistance(current.X, current.Y);

                foreach (Direction2D dir in Direction2DUtility.AllCardinalAndDiagonal)
                {
                    var (dx, dy) = Direction2DUtility.GetOffset(dir);
                    int neighborX = current.X + dx;
                    int neighborY = current.Y + dy;

                    if (!costField.IsInBounds(neighborX, neighborY)) continue;

                    ushort nX = (ushort)neighborX;
                    ushort nY = (ushort)neighborY;
                    byte cellCost = costField.GetCost(nX, nY);

                    if (cellCost >= CostField.ImpassableCost) continue;

                    // Diagonal movement cost weighting (14 vs 10 for cardinal)
                    ushort stepCost = (dx != 0 && dy != 0) ? (ushort)(cellCost * 14 / 10) : cellCost;
                    ushort newDist = (ushort)(currentDist + stepCost);

                    int neighborIndex = nY * Width + nX;
                    if (newDist < _distances[neighborIndex])
                    {
                        _distances[neighborIndex] = newDist;
                        _waveQueue.Enqueue(new GridPos2D(nX, nY));
                    }
                }
            }
        }
    }
}
