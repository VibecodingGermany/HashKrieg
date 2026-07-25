using System;

namespace Nova.Simulation.CommandsV1
{
    /// <summary>
    /// The only object UI and AI may create (docs/tech/Commands.md section 1).
    /// An intent carries a <see cref="CommandKind"/> plus canonical payload
    /// bytes — never a player slot, a sequence or a target tick. Those are
    /// assigned exclusively by <see cref="CommandIngress"/> when the intent is
    /// sealed into a <see cref="CommandRecord"/>.
    /// <para>
    /// The payload is serialized once, at creation, by the canonical writer,
    /// so the bytes the ingress later validates are exactly the bytes the
    /// producer meant. Intents for session actions carry an empty payload.
    /// </para>
    /// </summary>
    public readonly struct CommandIntent
    {
        private readonly byte[] _payloadBytes;

        public CommandKind Kind { get; }

        public byte PayloadVersion { get; }

        /// <summary>Canonical payload bytes produced by the payload writer.</summary>
        public ReadOnlyMemory<byte> PayloadBytes => _payloadBytes;

        private CommandIntent(CommandKind kind, byte payloadVersion, byte[] payloadBytes)
        {
            Kind = kind;
            PayloadVersion = payloadVersion;
            _payloadBytes = payloadBytes;
        }

        /// <summary>
        /// Creates an intent from a typed schema-v1 payload. The payload is
        /// serialized immediately with the canonical little-endian writer.
        /// </summary>
        public static CommandIntent Create<TPayload>(in TPayload payload)
            where TPayload : struct, ICommandPayload
        {
            var writer = new CommandPayloadWriter();
            payload.WriteTo(writer);
            return new CommandIntent(payload.Kind, payload.Version, writer.ToArray());
        }

        /// <summary>
        /// Creates an intent for a session action (pause/unpause/save/load).
        /// Session actions have an empty version-1 payload in schema v1 and are
        /// never sealed into the simulation record stream (Commands.md section 5).
        /// </summary>
        public static CommandIntent ForSessionAction(CommandKind kind)
        {
            if (!CommandKindInfo.IsSessionAction(kind))
            {
                throw new ArgumentException("Not a session action kind.", nameof(kind));
            }
            return new CommandIntent(kind, CommandLimits.PayloadVersionV1, Array.Empty<byte>());
        }
    }
}
