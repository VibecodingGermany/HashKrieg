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
    /// Restore is two-phase so a kernel snapshot restore is atomic
    /// (docs/tech/Serialization.md section 5: no partial state):
    /// <see cref="TryValidateState"/> performs the complete parse and every
    /// semantic check without mutating anything;
    /// <see cref="TryRestoreState"/> validates and commits. The kernel calls
    /// <see cref="TryRestoreState"/> only after EVERY block of the snapshot
    /// passed validation, with the identical bytes — an implementation must
    /// not fail in that situation; a failure there is a broken
    /// implementation contract, not bad input. A canonical block is consumed
    /// exactly; trailing bytes are a parse failure.
    /// </para>
    /// </summary>
    public interface IStatefulSimSystem : ISimSystem
    {
        /// <summary>Registered snapshot block id of this system's state.</summary>
        ushort StateBlockId { get; }

        /// <summary>Writes the canonical block content of the system's authoritative state.</summary>
        void WriteState(SnapshotBlockWriter writer);

        /// <summary>
        /// Fully parses and validates block content produced by
        /// <see cref="WriteState"/> — every length, range and semantic
        /// invariant — without mutating the system in any way. Returns false
        /// on malformed input.
        /// </summary>
        bool TryValidateState(ReadOnlySpan<byte> blockContent);

        /// <summary>
        /// Validates and commits block content produced by
        /// <see cref="WriteState"/>. Returns false on malformed input without
        /// mutating anything. After a successful <see cref="TryValidateState"/>
        /// of the identical bytes this call must succeed.
        /// </summary>
        bool TryRestoreState(ReadOnlySpan<byte> blockContent);
    }
}
