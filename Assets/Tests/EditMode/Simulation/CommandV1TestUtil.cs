using System;
using Nova.Simulation.CommandsV1;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Shared helpers for the command v1 EditMode suites. Mirror of the .NET
    /// lane CommandTestUtil with Unity Test Framework conventions.
    /// </summary>
    internal static class CommandV1TestUtil
    {
        internal static uint EntityId(int index, int generation)
        {
            return unchecked((uint)((generation << 10) | index));
        }

        internal static byte[] CraftRecord(
            uint enqueueTick, uint targetTick, byte playerSlot, uint sequence,
            ushort kind, byte payloadVersion, byte[] payload)
        {
            int recordLength = CommandLimits.HeaderBytes + payload.Length;
            var bytes = new byte[recordLength];
            WriteUInt16(bytes, 0, (ushort)recordLength);
            WriteUInt32(bytes, 2, enqueueTick);
            WriteUInt32(bytes, 6, targetTick);
            bytes[10] = playerSlot;
            WriteUInt32(bytes, 11, sequence);
            WriteUInt16(bytes, 15, kind);
            bytes[17] = payloadVersion;
            WriteUInt16(bytes, 18, (ushort)payload.Length);
            Array.Copy(payload, 0, bytes, CommandLimits.HeaderBytes, payload.Length);
            return bytes;
        }

        internal static byte[] PayloadBytes(ICommandPayload payload)
        {
            var writer = new CommandPayloadWriter();
            payload.WriteTo(writer);
            return writer.ToArray();
        }

        internal static MatchSession CreateSession(byte localSlot = 0, uint inputDelayTicks = 1)
        {
            return new MatchSession(localSlot, new byte[] { 0, 1 }, inputDelayTicks);
        }

        internal static CommandIngress CreateIngress(byte localSlot = 0, uint inputDelayTicks = 1)
        {
            var ingress = new CommandIngress(CreateSession(localSlot, inputDelayTicks));
            _ = new LocalLoopbackTransport(ingress);
            return ingress;
        }

        private static void WriteUInt16(byte[] bytes, int offset, ushort value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
        }

        private static void WriteUInt32(byte[] bytes, int offset, uint value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
            bytes[offset + 2] = (byte)(value >> 16);
            bytes[offset + 3] = (byte)(value >> 24);
        }
    }
}
