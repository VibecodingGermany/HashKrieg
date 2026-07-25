using System;

namespace Nova.Simulation.CommandsV1
{
    /// <summary>
    /// Deterministic outcome code of a state-dependent command evaluation at
    /// the target tick (docs/tech/Commands.md section 4). A state-dependent
    /// failure mutates nothing, produces a <see cref="CommandResult"/> and
    /// stays in the replay.
    /// </summary>
    public enum CommandResultCode : ushort
    {
        /// <summary>All state-dependent checks passed; the command applied.</summary>
        Applied = 1,

        /// <summary>An acting entity is not owned by the commanding player slot.</summary>
        RejectedNotOwned = 2,

        /// <summary>The acting player has no vision of the target (fixed committed view).</summary>
        RejectedNotVisible = 3,

        /// <summary>The target lies outside the legal range.</summary>
        RejectedOutOfRange = 4,

        /// <summary>The player cannot pay the command's cost.</summary>
        RejectedInsufficientResources = 5,

        /// <summary>A referenced entity does not exist (any more) at the target tick.</summary>
        RejectedInvalidTarget = 6,

        /// <summary>A prerequisite (tech, tier unlock) is not met.</summary>
        RejectedPrerequisitesNotMet = 7,

        /// <summary>The ability is on cooldown.</summary>
        RejectedOnCooldown = 8,
    }

    /// <summary>
    /// Deterministic result of evaluating one sealed record against the
    /// authoritative state at its target tick. Identifies the command by its
    /// stream identity (slot, sequence, target tick, kind) plus the outcome
    /// code; results are part of the replay (Commands.md sections 4 and 8 of
    /// SimulationCore.md). Equality is value-based so replay verification can
    /// compare re-executed results byte-exactly.
    /// </summary>
    public readonly struct CommandResult : IEquatable<CommandResult>
    {
        public byte PlayerSlot { get; }
        public uint Sequence { get; }
        public uint TargetTick { get; }
        public CommandKind Kind { get; }
        public CommandResultCode Code { get; }

        public CommandResult(in CommandRecord record, CommandResultCode code)
        {
            PlayerSlot = record.PlayerSlot;
            Sequence = record.Sequence;
            TargetTick = record.TargetTick;
            Kind = record.Kind;
            Code = code;
        }

        /// <summary>True when the command applied; false for every rejection code.</summary>
        public bool Applied => Code == CommandResultCode.Applied;

        public bool Equals(CommandResult other)
        {
            return PlayerSlot == other.PlayerSlot
                && Sequence == other.Sequence
                && TargetTick == other.TargetTick
                && Kind == other.Kind
                && Code == other.Code;
        }

        public override bool Equals(object obj) => obj is CommandResult other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PlayerSlot;
                hash = (hash * 397) ^ (int)Sequence;
                hash = (hash * 397) ^ (int)TargetTick;
                hash = (hash * 397) ^ (int)Kind;
                hash = (hash * 397) ^ (int)Code;
                return hash;
            }
        }

        public static bool operator ==(CommandResult left, CommandResult right) => left.Equals(right);
        public static bool operator !=(CommandResult left, CommandResult right) => !left.Equals(right);

        public override string ToString()
        {
            return $"CommandResult(slot={PlayerSlot}, seq={Sequence}, tick={TargetTick}, {Kind}, {Code})";
        }
    }
}
