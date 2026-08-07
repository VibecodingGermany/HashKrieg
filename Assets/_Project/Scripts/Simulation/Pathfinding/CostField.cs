using System;

namespace Nova.Simulation.Pathfinding
{
    /// <summary>
    /// Stores movement cost values for each cell in a grid sector.
    /// Cost 1 = open terrain, 2..254 = high-cost terrain/rough ground, 255 = impassable obstacle.
    /// <para>
    /// The cost field itself is static prototype content PLUS the building
    /// footprints the construction system writes into it; it is NOT part of
    /// a snapshot block. Everything derived from it (integration fields,
    /// flow fields) is a derived cache that may only be rebuilt on restore
    /// when the cost field provably matches the saving host. Since the
    /// sprint Truppenführung that proof is structural: footprint content is
    /// fully determined by the construction snapshot block (which restores
    /// before pathfinding), and the serialized <see cref="Epoch"/> is
    /// ADOPTED via <see cref="RestoreEpoch"/> so later snapshots stay
    /// byte-comparable — a mutation counter cannot be replayed from final
    /// state, so reject-on-mismatch was unimplementable once footprints
    /// became dynamic.
    /// </para>
    /// </summary>
    public sealed class CostField
    {
        public const byte OpenCost = 1;
        public const byte ImpassableCost = 255;

        private readonly byte[] _costs;

        public ushort Width { get; }
        public ushort Height { get; }

        /// <summary>
        /// Monotonic mutation counter, incremented on every write that reaches
        /// the backing array (<see cref="SetCost"/>, <see cref="ResetAll"/>).
        /// Starts at 0 for a freshly constructed field; the construction fill
        /// is the defined zero state, not a mutation. Wrap-around after 2^32
        /// mutations is unreachable in a match (terrain mutates on
        /// construction events, not per tick) and would only ever cause a
        /// rejected restore, never a silent stale-cache continuation.
        /// </summary>
        public uint Epoch { get; private set; }

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

        /// <summary>
        /// Writes a cell cost and advances <see cref="Epoch"/>. An
        /// out-of-bounds write is a no-op and leaves the epoch untouched, so
        /// the epoch counts exactly the mutations a derived cache must react
        /// to. The epoch advances even when the written value equals the
        /// current one: it is a mutation counter, not a content hash, and
        /// over-invalidation is always safe.
        /// </summary>
        public void SetCost(ushort x, ushort y, byte cost)
        {
            if (IsInBounds(x, y))
            {
                _costs[y * Width + x] = cost;
                Epoch = unchecked(Epoch + 1);
            }
        }

        public bool IsWalkable(ushort x, ushort y)
        {
            return GetCost(x, y) < ImpassableCost;
        }

        /// <summary>Refills the whole field and advances <see cref="Epoch"/>.</summary>
        public void ResetAll(byte defaultCost = OpenCost)
        {
            Array.Fill(_costs, defaultCost);
            Epoch = unchecked(Epoch + 1);
        }

        /// <summary>
        /// Snapshot-restore path of <see cref="PathfindingSystem"/>: adopts
        /// the serialized epoch so the restored host's counter continues in
        /// lockstep with the saving host (both apply the same subsequent
        /// footprint mutations). Runtime code never calls this — runtime
        /// mutations go through <see cref="SetCost"/> and advance the
        /// counter themselves.
        /// </summary>
        public void RestoreEpoch(uint epoch)
        {
            Epoch = epoch;
        }
    }
}
