using System;

namespace Nova.Simulation.Vision
{
    /// <summary>
    /// Deterministic 2D grid storing per-player Fog of War vision states.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public sealed class VisionGrid
    {
        public const int MaxPlayers = 8;

        private readonly byte[][] _playerVisionGrids;

        public ushort Width { get; }
        public ushort Height { get; }

        public VisionGrid(ushort width = 128, ushort height = 128)
        {
            Width = width;
            Height = height;

            int cellCount = width * height;
            _playerVisionGrids = new byte[MaxPlayers][];

            for (int p = 0; p < MaxPlayers; p++)
            {
                _playerVisionGrids[p] = new byte[cellCount];
            }
        }

        public VisionState GetVisionState(byte playerId, ushort x, ushort y)
        {
            if (playerId >= MaxPlayers || x >= Width || y >= Height) return VisionState.Unexplored;
            return (VisionState)_playerVisionGrids[playerId][y * Width + x];
        }

        public void RevealCircle(byte playerId, ushort centerX, ushort centerY, ushort radius)
        {
            if (playerId >= MaxPlayers) return;

            int minX = Math.Max(0, centerX - radius);
            int maxX = Math.Min(Width - 1, centerX + radius);
            int minY = Math.Max(0, centerY - radius);
            int maxY = Math.Min(Height - 1, centerY + radius);

            int rSq = radius * radius;
            byte[] grid = _playerVisionGrids[playerId];

            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - centerY;
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - centerX;
                    if (dx * dx + dy * dy <= rSq)
                    {
                        grid[y * Width + x] = (byte)VisionState.Visible;
                    }
                }
            }
        }

        public void DemoteVisibleToExplored(byte playerId)
        {
            if (playerId >= MaxPlayers) return;

            byte[] grid = _playerVisionGrids[playerId];
            int count = grid.Length;

            for (int i = 0; i < count; i++)
            {
                if (grid[i] == (byte)VisionState.Visible)
                {
                    grid[i] = (byte)VisionState.Explored;
                }
            }
        }
    }
}
