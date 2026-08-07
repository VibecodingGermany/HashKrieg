using System;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Snapshots;

namespace Nova.Networking
{
    /// <summary>Frame types of the relay wire protocol v1 (<see cref="RelayProtocol.ProtocolVersion"/>).</summary>
    public enum RelayFrameType : byte
    {
        /// <summary>client→server: protocol version + match token.</summary>
        Hello = 1,
        /// <summary>server→client: slot assignment + match parameters (seed, delay, server definitions hash).</summary>
        Offer = 2,
        /// <summary>client→server: serialized MatchFingerprint bytes.</summary>
        Fingerprint = 3,
        /// <summary>client→server: canonical initial snapshot bytes.</summary>
        InitialSnapshot = 4,
        /// <summary>server→client: both peers verified — the match starts.</summary>
        Start = 5,
        /// <summary>server→client: refusal with a human-readable reason.</summary>
        Reject = 6,
        /// <summary>both ways: one canonical command record (the only frame that carries gameplay input).</summary>
        CommandRecord = 7,
        /// <summary>both ways: a slot's input for one tick is complete — TRANSPORT frame, never a CommandRecord.</summary>
        TickComplete = 8,
        /// <summary>client→server: periodic canonical state hash for the desync comparison.</summary>
        StateHash = 9,
        /// <summary>server→client: the per-tick hashes diverged; both clients halt.</summary>
        Desync = 10,
        /// <summary>server→client: a peer connection was lost; the match ends ordered.</summary>
        PeerLost = 11,
        /// <summary>client→server: RTT probe.</summary>
        Ping = 12,
        /// <summary>server→client: RTT echo.</summary>
        Pong = 13,
    }

    /// <summary>
    /// Wire framing and payload codecs of the relay protocol v1
    /// (docs/tech/RelayServer.md): every frame is length-prefixed
    /// <c>[u32 payloadBytes][u8 type][payload]</c>, little-endian throughout
    /// — the identical encoding rules as the canonical snapshot/container
    /// formats (Serialization.md section 2), so a hex dump reads the same
    /// everywhere. TCP delivers reliable, ordered byte streams, so one
    /// frame is either complete or not yet arrived; there is no
    /// fragmentation and no reordering to defend against.
    /// <para>
    /// Only <see cref="RelayFrameType.CommandRecord"/> carries gameplay
    /// input — as opaque canonical record bytes that re-enter every client
    /// through the identical structural validation
    /// (<see cref="CommandIngress.TryAcceptRecordBytes"/>).
    /// <see cref="RelayFrameType.TickComplete"/> is deliberately a TRANSPORT
    /// frame: it never reaches the ingress, appears in no replay and does
    /// not touch the frozen command schema v1 (golden-bytes guarded).
    /// </para>
    /// </summary>
    public static class RelayProtocol
    {
        /// <summary>Wire protocol version; a mismatch is rejected at Hello.</summary>
        public const byte ProtocolVersion = 1;

        /// <summary>Hard cap of one frame's payload (record frames are a few KB at most; snapshots dominate).</summary>
        public const int MaxFramePayloadBytes = 8 * 1024 * 1024;

        /// <summary>Header size: u32 payload length + u8 frame type.</summary>
        public const int HeaderBytes = 5;

        /// <summary>Writes one complete frame into <paramref name="dst"/>; returns the byte count written.</summary>
        public static int WriteFrame(byte[] dst, int offset, RelayFrameType type, ReadOnlySpan<byte> payload)
        {
            if (payload.Length > MaxFramePayloadBytes)
            {
                throw new ArgumentException("Frame payload exceeds the protocol cap.", nameof(payload));
            }
            int required = HeaderBytes + payload.Length;
            if (dst.Length - offset < required)
            {
                throw new ArgumentException("Destination buffer too small.", nameof(dst));
            }
            WriteUInt32(dst, offset, unchecked((uint)payload.Length));
            dst[offset + 4] = (byte)type;
            payload.CopyTo(dst.AsSpan(offset + HeaderBytes));
            return required;
        }

        /// <summary>Serializes a frame into a freshly allocated buffer (convenience for the send path).</summary>
        public static byte[] CreateFrame(RelayFrameType type, ReadOnlySpan<byte> payload)
        {
            var bytes = new byte[HeaderBytes + payload.Length];
            WriteFrame(bytes, 0, type, payload);
            return bytes;
        }

        // ------------------------------------------------------------------
        // Payload codecs (fixed-layout frames)
        // ------------------------------------------------------------------

        public static byte[] CreateHelloPayload(ulong matchToken)
        {
            var bytes = new byte[1 + 8];
            bytes[0] = ProtocolVersion;
            WriteUInt64(bytes, 1, matchToken);
            return bytes;
        }

        public static bool TryParseHello(ReadOnlySpan<byte> payload, out byte protocolVersion, out ulong matchToken)
        {
            protocolVersion = 0;
            matchToken = 0;
            if (payload.Length != 1 + 8) return false;
            protocolVersion = payload[0];
            matchToken = ReadUInt64(payload, 1);
            return true;
        }

        public static byte[] CreateOfferPayload(byte slot, byte[] activeSlots, ulong seed, uint inputDelayTicks, ulong serverDefinitionsHash64)
        {
            var bytes = new byte[1 + 1 + activeSlots.Length + 8 + 4 + 8];
            bytes[0] = slot;
            bytes[1] = (byte)activeSlots.Length;
            Array.Copy(activeSlots, 0, bytes, 2, activeSlots.Length);
            int offset = 2 + activeSlots.Length;
            WriteUInt64(bytes, offset, seed);
            WriteUInt32(bytes, offset + 8, inputDelayTicks);
            WriteUInt64(bytes, offset + 12, serverDefinitionsHash64);
            return bytes;
        }

        public static bool TryParseOffer(ReadOnlySpan<byte> payload, out byte slot, out byte[] activeSlots,
            out ulong seed, out uint inputDelayTicks, out ulong serverDefinitionsHash64)
        {
            slot = 0;
            activeSlots = null;
            seed = 0;
            inputDelayTicks = 0;
            serverDefinitionsHash64 = 0;
            if (payload.Length < 2) return false;
            slot = payload[0];
            int count = payload[1];
            if (count == 0 || count > CommandLimits.ReservedPlayerSlots) return false;
            if (payload.Length != 2 + count + 8 + 4 + 8) return false;
            activeSlots = payload.Slice(2, count).ToArray();
            int offset = 2 + count;
            seed = ReadUInt64(payload, offset);
            inputDelayTicks = ReadUInt32(payload, offset + 8);
            serverDefinitionsHash64 = ReadUInt64(payload, offset + 12);
            return true;
        }

        public static byte[] CreateTickCompletePayload(byte slot, uint targetTick, int recordCount)
        {
            var bytes = new byte[1 + 4 + 2];
            bytes[0] = slot;
            WriteUInt32(bytes, 1, targetTick);
            WriteUInt16(bytes, 5, unchecked((ushort)recordCount));
            return bytes;
        }

        public static bool TryParseTickComplete(ReadOnlySpan<byte> payload, out byte slot, out uint targetTick, out int recordCount)
        {
            slot = 0;
            targetTick = 0;
            recordCount = 0;
            if (payload.Length != 1 + 4 + 2) return false;
            slot = payload[0];
            targetTick = ReadUInt32(payload, 1);
            recordCount = ReadUInt16(payload, 5);
            return true;
        }

        public static byte[] CreateStateHashPayload(byte slot, uint tick, ulong stateHash)
        {
            var bytes = new byte[1 + 4 + 8];
            bytes[0] = slot;
            WriteUInt32(bytes, 1, tick);
            WriteUInt64(bytes, 5, stateHash);
            return bytes;
        }

        public static bool TryParseStateHash(ReadOnlySpan<byte> payload, out byte slot, out uint tick, out ulong stateHash)
        {
            slot = 0;
            tick = 0;
            stateHash = 0;
            if (payload.Length != 1 + 4 + 8) return false;
            slot = payload[0];
            tick = ReadUInt32(payload, 1);
            stateHash = ReadUInt64(payload, 5);
            return true;
        }

        public static byte[] CreateSlotTickPayload(RelayFrameType type, byte slot, uint tick)
        {
            // Shared by Desync (slot unused = 255) and PeerLost.
            var bytes = new byte[1 + 4];
            bytes[0] = slot;
            WriteUInt32(bytes, 1, tick);
            return bytes;
        }

        public static bool TryParseSlotTick(ReadOnlySpan<byte> payload, out byte slot, out uint tick)
        {
            slot = 0;
            tick = 0;
            if (payload.Length != 1 + 4) return false;
            slot = payload[0];
            tick = ReadUInt32(payload, 1);
            return true;
        }

        public static byte[] CreateReasonPayload(RelayFrameType type, string reason)
        {
            byte[] text = System.Text.Encoding.UTF8.GetBytes(reason ?? string.Empty);
            var bytes = new byte[2 + text.Length];
            WriteUInt16(bytes, 0, unchecked((ushort)text.Length));
            Array.Copy(text, 0, bytes, 2, text.Length);
            return bytes;
        }

        public static string ParseReasonPayload(ReadOnlySpan<byte> payload)
        {
            if (payload.Length < 2) return string.Empty;
            int length = ReadUInt16(payload, 0);
            if (payload.Length != 2 + length) return string.Empty;
            return System.Text.Encoding.UTF8.GetString(payload.Slice(2, length));
        }

        public static byte[] CreatePingPayload(uint probe)
        {
            var bytes = new byte[4];
            WriteUInt32(bytes, 0, probe);
            return bytes;
        }

        public static bool TryParsePing(ReadOnlySpan<byte> payload, out uint probe)
        {
            probe = 0;
            if (payload.Length != 4) return false;
            probe = ReadUInt32(payload, 0);
            return true;
        }

        // ------------------------------------------------------------------
        // Streaming reader: feed arbitrary chunks, take complete frames
        // ------------------------------------------------------------------

        /// <summary>
        /// Incremental frame cutter for a TCP byte stream: arbitrary chunks
        /// go in, complete frames come out. Allocation happens only when a
        /// frame completes (the payload hand-out); the carry buffer grows
        /// geometrically and is reused.
        /// </summary>
        public sealed class FrameCutter
        {
            private byte[] _buffer = new byte[64 * 1024];
            private int _length;

            /// <summary>Appends <paramref name="chunk"/> to the carry buffer.</summary>
            public void Feed(ReadOnlySpan<byte> chunk)
            {
                if (_length + chunk.Length > _buffer.Length)
                {
                    int newSize = Math.Max(_buffer.Length * 2, _length + chunk.Length);
                    if (newSize > MaxFramePayloadBytes + HeaderBytes)
                    {
                        throw new InvalidOperationException("Relay frame stream exceeds the protocol cap.");
                    }
                    var bigger = new byte[newSize];
                    Array.Copy(_buffer, 0, bigger, 0, _length);
                    _buffer = bigger;
                }
                chunk.CopyTo(_buffer.AsSpan(_length));
                _length += chunk.Length;
            }

            /// <summary>Cuts the next complete frame; false when the buffer holds only a partial one.</summary>
            public bool TryTakeFrame(out RelayFrameType type, out byte[] payload)
            {
                type = 0;
                payload = null;
                if (_length < HeaderBytes) return false;
                uint payloadLength = ReadUInt32(_buffer, 0);
                if (payloadLength > MaxFramePayloadBytes)
                {
                    throw new InvalidOperationException($"Relay frame declares {payloadLength} payload bytes (cap {MaxFramePayloadBytes}).");
                }
                int frameBytes = HeaderBytes + (int)payloadLength;
                if (_length < frameBytes) return false;

                type = (RelayFrameType)_buffer[4];
                payload = new byte[payloadLength];
                Array.Copy(_buffer, HeaderBytes, payload, 0, (int)payloadLength);
                Array.Copy(_buffer, frameBytes, _buffer, 0, _length - frameBytes);
                _length -= frameBytes;
                return true;
            }
        }

        // ------------------------------------------------------------------
        // Little-endian primitives (same rules as SnapshotBlockWriter)
        // ------------------------------------------------------------------

        public static void WriteUInt16(byte[] dst, int offset, ushort value)
        {
            dst[offset] = (byte)value;
            dst[offset + 1] = (byte)(value >> 8);
        }

        public static void WriteUInt32(byte[] dst, int offset, uint value)
        {
            dst[offset] = (byte)value;
            dst[offset + 1] = (byte)(value >> 8);
            dst[offset + 2] = (byte)(value >> 16);
            dst[offset + 3] = (byte)(value >> 24);
        }

        public static void WriteUInt64(byte[] dst, int offset, ulong value)
        {
            WriteUInt32(dst, offset, unchecked((uint)value));
            WriteUInt32(dst, offset + 4, unchecked((uint)(value >> 32)));
        }

        public static ushort ReadUInt16(ReadOnlySpan<byte> src, int offset)
        {
            return (ushort)(src[offset] | (src[offset + 1] << 8));
        }

        public static uint ReadUInt32(ReadOnlySpan<byte> src, int offset)
        {
            return (uint)(src[offset] | (src[offset + 1] << 8) | (src[offset + 2] << 16) | (src[offset + 3] << 24));
        }

        public static ulong ReadUInt64(ReadOnlySpan<byte> src, int offset)
        {
            return ReadUInt32(src, offset) | ((ulong)ReadUInt32(src, offset + 4) << 32);
        }
    }
}
