using System.Collections.Generic;

namespace Nova.Simulation.Snapshots
{
    /// <summary>One block of a parsed snapshot file.</summary>
    public readonly struct SnapshotBlock
    {
        public ushort BlockId { get; }
        public ulong Hash { get; }

        /// <summary>Owned copy of the block content bytes.</summary>
        public byte[] Content { get; }

        public SnapshotBlock(ushort blockId, ulong hash, byte[] content)
        {
            BlockId = blockId;
            Hash = hash;
            Content = content;
        }
    }

    /// <summary>
    /// Fully validated, parsed snapshot container. Construction happens only
    /// after every length, hash and ordering check passed
    /// (docs/tech/Serialization.md section 5: no partial state escapes a
    /// failed parse).
    /// </summary>
    public sealed class SnapshotFile
    {
        public ushort FormatVersion { get; }

        /// <summary>Canonical NOVA_STATE_V1 state hash from the header (verified).</summary>
        public ulong StateHash { get; }

        /// <summary>Blocks in strictly ascending BlockId order.</summary>
        public IReadOnlyList<SnapshotBlock> Blocks { get; }

        /// <summary>
        /// True when the file exceeds the 4 MiB uncompressed MS-1 target
        /// (SimulationCore.md section 7). Documented warning/info signal
        /// only — never a parse failure.
        /// </summary>
        public bool ExceedsSoftTarget { get; }

        internal SnapshotFile(
            ushort formatVersion, ulong stateHash,
            IReadOnlyList<SnapshotBlock> blocks, bool exceedsSoftTarget)
        {
            FormatVersion = formatVersion;
            StateHash = stateHash;
            Blocks = blocks;
            ExceedsSoftTarget = exceedsSoftTarget;
        }

        /// <summary>Looks up a block by id; content is the owned copy.</summary>
        public bool TryGetBlock(ushort blockId, out byte[] content)
        {
            for (int i = 0; i < Blocks.Count; i++)
            {
                if (Blocks[i].BlockId == blockId)
                {
                    content = Blocks[i].Content;
                    return true;
                }
            }
            content = null;
            return false;
        }
    }
}
