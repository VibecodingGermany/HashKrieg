using System;
using Nova.Simulation.Snapshots;

namespace Nova.Simulation
{
    /// <summary>
    /// A simulation system that owns authoritative, serializable state
    /// (docs/tech/SimulationCore.md section 3). Each stateful system
    /// contributes exactly one snapshot block, identified by a registered
    /// <see cref="StateBlockId"/> (see <see cref="SnapshotBlockIds"/>).
    /// <para>
    /// <see cref="WriteState"/> emits the canonical little-endian block
    /// content. The kernel uses the identical bytes for two purposes, so they
    /// can never drift apart: the snapshot block payload and the
    /// NOVA_STATE_V1 state-hash input (SimulationCore.md sections 5 and 7).
    /// </para>
    /// <para>
    /// <see cref="TryRestoreState"/> must validate every length before
    /// allocating or mutating and must leave the system untouched on failure
    /// (no partial state, SimulationCore.md section 7 point 4). A canonical
    /// block is consumed exactly; trailing bytes are a parse failure.
    /// </para>
    /// </summary>
    public interface IStatefulSimSystem : ISimSystem
    {
        /// <summary>Registered snapshot block id of this system's state.</summary>
        ushort StateBlockId { get; }

        /// <summary>Writes the canonical block content of the system's authoritative state.</summary>
        void WriteState(SnapshotBlockWriter writer);

        /// <summary>
        /// Restores the state previously produced by <see cref="WriteState"/>.
        /// Returns false on malformed input without mutating anything.
        /// </summary>
        bool TryRestoreState(ReadOnlySpan<byte> blockContent);
    }
}
