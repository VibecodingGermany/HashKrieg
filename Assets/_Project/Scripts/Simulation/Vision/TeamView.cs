namespace Nova.Simulation.Vision
{
    /// <summary>
    /// Read-only accessor over the committed Fog of War mask of one team
    /// (docs/tech/FogOfWar.md section 2). The mask is owned exclusively by
    /// <see cref="FogOfWarSystem"/> and only mutates inside its 5 Hz
    /// recompute, so every read between two recomputes observes the last
    /// committed view — no system can pull a provisional or self-computed
    /// sight. This is the single authoritative sight source Combat, AI, the
    /// player snapshot and rendering must consume (D-058).
    /// </summary>
    public readonly struct TeamView
    {
        private readonly byte[] _mask;

        public ushort Width { get; }
        public ushort Height { get; }

        internal TeamView(byte[] mask, ushort width, ushort height)
        {
            _mask = mask;
            Width = width;
            Height = height;
        }

        /// <summary>Committed cell state; out-of-range coordinates read as <see cref="VisionState.Unexplored"/>.</summary>
        public VisionState GetCellState(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return VisionState.Unexplored;
            return (VisionState)_mask[y * Width + x];
        }

        /// <summary>True only while the cell is inside the committed live sight — the sole targeting permission (FogOfWar.md section 3).</summary>
        public bool IsVisible(int x, int y) => GetCellState(x, y) == VisionState.Visible;

        /// <summary>True once the cell has ever been seen (Explored or Visible) — terrain/presentation layer.</summary>
        public bool IsExplored(int x, int y) => GetCellState(x, y) != VisionState.Unexplored;
    }
}
