using System;
using Nova.Core;

namespace Nova.Simulation.Pathfinding
{
    /// <summary>
    /// Deterministic simulation system for Flow-Field Pathfinding.
    /// Manages cost fields, integration wave propagation, and flow field generation.
    /// <para>
    /// Stateful (<see cref="IStatefulSimSystem"/>): the block stores only the
    /// requested flow-field destination. The integration/flow fields
    /// themselves are a derived cache of (static cost field, destination) and
    /// are rebuilt on restore by re-running the identical deterministic
    /// generation — this is the derived-cache rebuild the spec allows when
    /// the continuation provably matches (docs/tech/SimulationCore.md
    /// section 3); the kernel snapshot proof test covers exactly that.
    /// The canonical pathfinding state (cache eviction metadata, request
    /// queues) is a later G1 slice; the prototype cost field is never mutated
    /// at runtime.
    /// </para>
    /// </summary>
    public sealed class PathfindingSystem : IStatefulSimSystem
    {
        /// <summary>Serialization version of the pathfinding snapshot block.</summary>
        public const byte StateVersion = 1;

        private GridPos2D _flowFieldDestination;
        private bool _hasFlowField;

        public string Name => "PathfindingSystem";

        public CostField CostField { get; }
        public IntegrationField IntegrationField { get; }
        public FlowField FlowField { get; }

        public PathfindingSystem(ushort width, ushort height)
        {
            CostField = new CostField(width, height);
            IntegrationField = new IntegrationField(width, height);
            FlowField = new FlowField(width, height);
            _flowFieldDestination = GridPos2D.Invalid;
            _hasFlowField = false;
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo($"[{Name}] Initialized Flow-Field Grid ({CostField.Width}x{CostField.Height}).");
        }

        public void RequestFlowField(GridPos2D destination)
        {
            IntegrationField.Generate(CostField, destination);
            FlowField.Generate(IntegrationField, CostField);
            _flowFieldDestination = destination;
            _hasFlowField = true;
        }

        public void ExecuteTick(Tick tick)
        {
            // Flow-Field ticks process pending path requests or dynamic cost field updates
        }

        public void Shutdown()
        {
        }

        /// <summary>Snapshot block id of the pathfinding state (registry: <see cref="Snapshots.SnapshotBlockIds"/>).</summary>
        public ushort StateBlockId => Snapshots.SnapshotBlockIds.Pathfinding;

        /// <summary>Writes the flow-field destination; the fields themselves are rebuilt on restore.</summary>
        public void WriteState(Snapshots.SnapshotBlockWriter writer)
        {
            writer.WriteUInt8(StateVersion);
            writer.WriteUInt8(_hasFlowField ? (byte)1 : (byte)0);
            writer.WriteUInt16(_flowFieldDestination.X);
            writer.WriteUInt16(_flowFieldDestination.Y);
        }

        /// <summary>
        /// Fully validates a pathfinding block produced by
        /// <see cref="WriteState"/> without touching the current fields.
        /// </summary>
        public bool TryValidateState(ReadOnlySpan<byte> blockContent)
        {
            return TryParseState(blockContent, out _, out _);
        }

        /// <summary>
        /// Restores the flow-field destination and deterministically rebuilds
        /// the derived fields from it. Malformed input returns false without
        /// touching the current fields.
        /// </summary>
        public bool TryRestoreState(ReadOnlySpan<byte> blockContent)
        {
            if (!TryParseState(blockContent, out bool hasField, out GridPos2D destination))
            {
                return false;
            }

            // Commit: rebuild the derived cache from the canonical inputs so
            // the restored host continues with identical directions.
            if (hasField)
            {
                RequestFlowField(destination);
            }
            else
            {
                _flowFieldDestination = GridPos2D.Invalid;
                _hasFlowField = false;
            }
            return true;
        }

        /// <summary>
        /// Parses and fully validates block content without mutating anything.
        /// </summary>
        private bool TryParseState(
            ReadOnlySpan<byte> blockContent, out bool hasField, out GridPos2D destination)
        {
            hasField = false;
            destination = GridPos2D.Invalid;

            var reader = new Snapshots.SnapshotBlockReader(blockContent);
            if (!reader.TryReadUInt8(out byte version) || version != StateVersion) return false;
            if (!reader.TryReadUInt8(out byte hasFieldRaw) || hasFieldRaw > 1) return false;
            if (!reader.TryReadUInt16(out ushort x)) return false;
            if (!reader.TryReadUInt16(out ushort y)) return false;
            if (reader.Remaining != 0) return false;

            var parsed = new GridPos2D(x, y);
            if (hasFieldRaw == 1 && (!parsed.IsValid || x >= CostField.Width || y >= CostField.Height))
            {
                return false;
            }

            hasField = hasFieldRaw == 1;
            destination = parsed;
            return true;
        }
    }
}
