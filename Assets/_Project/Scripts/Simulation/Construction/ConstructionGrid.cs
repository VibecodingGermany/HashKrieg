using System;

namespace Nova.Simulation.Construction
{
    /// <summary>
    /// Grid tracking building cell occupancy and construction zone validation.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public sealed class ConstructionGrid
    {
        public const byte Unoccupied = 0;

        private readonly byte[] _occupiedCells;

        public ushort Width { get; }
        public ushort Height { get; }

        public ConstructionGrid(ushort width = 128, ushort height = 128)
        {
            Width = width;
            Height = height;
            _occupiedCells = new byte[width * height];
        }

        public bool IsOccupied(ushort x, ushort y)
        {
            if (x >= Width || y >= Height) return true;
            return _occupiedCells[y * Width + x] != Unoccupied;
        }

        public bool CanPlaceBuilding(ushort originX, ushort originY, ushort sizeX, ushort sizeY)
        {
            if (originX + sizeX > Width || originY + sizeY > Height) return false;

            for (ushort y = originY; y < originY + sizeY; y++)
            {
                for (ushort x = originX; x < originX + sizeX; x++)
                {
                    if (IsOccupied(x, y)) return false;
                }
            }

            return true;
        }

        public bool OccupyCells(ushort originX, ushort originY, ushort sizeX, ushort sizeY, byte playerId)
        {
            if (!CanPlaceBuilding(originX, originY, sizeX, sizeY)) return false;

            for (ushort y = originY; y < originY + sizeY; y++)
            {
                for (ushort x = originX; x < originX + sizeX; x++)
                {
                    _occupiedCells[y * Width + x] = (byte)(playerId + 1); // 1-based player ID
                }
            }

            return true;
        }

        public void FreeCells(ushort originX, ushort originY, ushort sizeX, ushort sizeY)
        {
            if (originX + sizeX > Width || originY + sizeY > Height) return;

            for (ushort y = originY; y < originY + sizeY; y++)
            {
                for (ushort x = originX; x < originX + sizeX; x++)
                {
                    _occupiedCells[y * Width + x] = Unoccupied;
                }
            }
        }
    }
}
