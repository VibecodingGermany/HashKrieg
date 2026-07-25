using System;
using System.Collections.Generic;
using Nova.Simulation.CommandsV1;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Shared helpers for the command v1 suites: canonical entity id packing
    /// (SimulationCore.md section 1: bits 0-9 index, bits 10-31 generation),
    /// raw record crafting for wire-level attack tests and a standard
    /// session/ingress/loopback fixture.
    /// </summary>
    internal static class CommandTestUtil
    {
        internal static uint EntityId(int index, int generation)
        {
            return unchecked((uint)((generation << 10) | index));
        }

        /// <summary>Builds a raw canonical record byte array field by field (little-endian).</summary>
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

        internal static CommandIngress CreateIngress(MatchSession session)
        {
            var ingress = new CommandIngress(session);
            _ = new LocalLoopbackTransport(ingress);
            return ingress;
        }

        internal static CommandIngress CreateIngress(byte localSlot = 0, uint inputDelayTicks = 1)
        {
            return CreateIngress(CreateSession(localSlot, inputDelayTicks));
        }

        /// <summary>Submits a valid Stop intent and asserts acceptance.</summary>
        internal static byte[] SubmitStopAndGetBytes(CommandIngress ingress, uint[] entityIds)
        {
            var intent = CommandIntent.Create(new StopPayload(entityIds));
            CommandIngressResult result = ingress.TrySubmitIntent(intent, out CommandRejectReason reason);
            if (result != CommandIngressResult.Accepted)
            {
                throw new InvalidOperationException($"expected acceptance, got {result}/{reason}");
            }
            // The accepted record is the pending one; fetch via dedupe state.
            uint sequence = ingress.DedupeState.NextLocalSequence(ingress.Session.LocalSlot) - 1;
            ingress.DedupeState.TryGetPending(ingress.Session.LocalSlot, sequence, out CommandRecord record);
            return record.Serialize();
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
