using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Snapshots;

namespace Nova.Networking
{
    /// <summary>A fully verified per-client desync diagnosis.</summary>
    public sealed class DesyncDiagnosticFile
    {
        public byte LocalSlot { get; }
        public uint DesyncTick { get; }
        public ulong StateHash { get; }
        public byte[] SnapshotBytes { get; }
        public IReadOnlyList<CommandRecord> Records { get; }

        internal DesyncDiagnosticFile(
            byte localSlot, uint desyncTick, ulong stateHash, byte[] snapshotBytes,
            CommandRecord[] records)
        {
            LocalSlot = localSlot;
            DesyncTick = desyncTick;
            StateHash = stateHash;
            SnapshotBytes = snapshotBytes;
            Records = records;
        }
    }

    /// <summary>
    /// Writer and hardened reader for one client's desync evidence: the full
    /// canonical state snapshot plus the canonical command-record stream the
    /// client observed. The format is engine-free and covered by the headless
    /// test lane.
    /// </summary>
    public static class DesyncDiagnostic
    {
        private const int MagicBytes = 9;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("NOVADIAG2");
        private const int FixedEnvelopeBytes = 1 + 4 + 8 + 4 + 4;
        public const int MaxDiagnosticBytes = RelayRecordStream.MaxRecordingBytes;
        internal const int MaxRecordSectionBytes =
            MaxDiagnosticBytes - MagicBytes - FixedEnvelopeBytes;

        public static bool TryWrite(
            string directory, byte localSlot, uint desyncTick,
            ulong stateHash, byte[] snapshotBytes, IReadOnlyList<byte[]> recordBytes,
            out string path, out string error)
        {
            path = null;
            error = string.Empty;
            if (recordBytes == null)
            {
                error = "record stream is null";
                return false;
            }
            using (var spool = new DiagnosticRecordSpool())
            {
                for (int i = 0; i < recordBytes.Count; i++)
                {
                    if (!spool.TryAppend(recordBytes[i], out error))
                    {
                        error = $"record stream item {i}: {error}";
                        return false;
                    }
                }
                return TryWrite(
                    directory, localSlot, desyncTick, stateHash,
                    snapshotBytes, spool, out path, out error);
            }
        }

        internal static bool TryWrite(
            string directory, byte localSlot, uint desyncTick,
            ulong stateHash, byte[] snapshotBytes, DiagnosticRecordSpool recordSpool,
            out string path, out string error)
        {
            path = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(directory))
            {
                error = "diagnostic directory is empty";
                return false;
            }
            if (!TryValidateHeader(
                    localSlot, desyncTick, stateHash, snapshotBytes, recordSpool, out error))
            {
                return false;
            }

            string partialPath = null;
            try
            {
                Directory.CreateDirectory(directory);
                path = Path.Combine(directory,
                    $"desync-slot{localSlot}-tick{desyncTick}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.novadiag");
                partialPath = path + ".partial";
                using (var stream = new FileStream(
                    partialPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(Magic, 0, Magic.Length);
                    stream.WriteByte(localSlot);
                    WriteUInt32(stream, desyncTick);
                    WriteUInt64(stream, stateHash);
                    WriteBlob(stream, snapshotBytes);
                    long countOffset = stream.Position;
                    WriteUInt32(stream, 0);
                    if (!recordSpool.TryCopyThroughTick(
                            stream, desyncTick, MaxDiagnosticBytes,
                            out uint copiedRecords, out error))
                    {
                        path = null;
                        return false;
                    }
                    long endOffset = stream.Position;
                    stream.Position = countOffset;
                    WriteUInt32(stream, copiedRecords);
                    stream.Position = endOffset;
                    stream.Flush();
                }
                File.Move(partialPath, path);
                return true;
            }
            catch (Exception exception)
            {
                path = null;
                error = exception.Message;
                return false;
            }
            finally
            {
                if (!string.IsNullOrEmpty(partialPath) && File.Exists(partialPath))
                {
                    try { File.Delete(partialPath); } catch { /* best effort */ }
                }
            }
        }

        public static bool TryRead(byte[] bytes, out DesyncDiagnosticFile file, out string error)
        {
            file = null;
            error = string.Empty;
            if (bytes == null || bytes.Length < Magic.Length + FixedEnvelopeBytes)
            {
                error = "diagnostic is truncated";
                return false;
            }
            if (bytes.Length > MaxDiagnosticBytes)
            {
                error = "diagnostic exceeds the hard size cap";
                return false;
            }

            int offset = 0;
            for (int i = 0; i < Magic.Length; i++)
            {
                if (bytes[i] != Magic[i])
                {
                    error = "bad NOVADIAG2 magic";
                    return false;
                }
            }
            offset += Magic.Length;
            byte localSlot = bytes[offset++];
            if (localSlot >= CommandLimits.ReservedPlayerSlots)
            {
                error = "local slot is outside the reserved range";
                return false;
            }
            if (!TryReadUInt32(bytes, ref offset, out uint desyncTick)
                || !TryReadUInt64(bytes, ref offset, out ulong stateHash)
                || !TryReadBlob(bytes, ref offset, SnapshotFormat.MaxFileBytes, out byte[] snapshotBytes))
            {
                error = "malformed diagnostic header or snapshot length";
                return false;
            }
            if (!TryReadSnapshotIdentity(
                    snapshotBytes, out uint snapshotTick, out ulong snapshotHash, out string snapshotError))
            {
                error = $"malformed snapshot ({snapshotError})";
                return false;
            }
            if (snapshotTick != desyncTick || snapshotHash != stateHash)
            {
                error = "snapshot tick/hash does not match the diagnostic checkpoint";
                return false;
            }
            if (!TryReadUInt32(bytes, ref offset, out uint recordCount)
                || recordCount > (uint)((bytes.Length - offset) / (4 + CommandLimits.HeaderBytes)))
            {
                error = "invalid diagnostic record count";
                return false;
            }

            var records = new CommandRecord[(int)recordCount];
            var lastSequenceBySlot = new uint[CommandLimits.ReservedPlayerSlots];
            for (int i = 0; i < records.Length; i++)
            {
                if (!TryReadBlob(bytes, ref offset, CommandLimits.MaxRecordBytes, out byte[] raw)
                    || !CommandRecord.TryDeserialize(raw, out CommandRecord record, out int consumed)
                    || consumed != raw.Length)
                {
                    error = $"malformed diagnostic record {i}";
                    return false;
                }
                if (record.PlayerSlot >= CommandLimits.ReservedPlayerSlots
                    || record.Sequence == 0
                    || record.Sequence <= lastSequenceBySlot[record.PlayerSlot]
                    || record.TargetTick > desyncTick)
                {
                    error = $"diagnostic record {i} violates slot/sequence/tick binding";
                    return false;
                }
                if (!CommandPayloadValidation.TryValidateStreamPayload(
                        record.Kind, record.PayloadVersion, record.Payload.Span, out CommandRejectReason reason))
                {
                    error = $"diagnostic record {i} has invalid payload ({reason})";
                    return false;
                }
                if (i > 0 && CommandBatch.CompareRecords(records[i - 1], record) >= 0)
                {
                    error = "diagnostic records are not in strict canonical order";
                    return false;
                }
                lastSequenceBySlot[record.PlayerSlot] = record.Sequence;
                records[i] = record;
            }
            if (offset != bytes.Length)
            {
                error = "diagnostic has trailing bytes";
                return false;
            }

            file = new DesyncDiagnosticFile(localSlot, desyncTick, stateHash, snapshotBytes, records);
            return true;
        }

        internal static bool CanFit(
            int snapshotBytes, long recordSectionBytes, out string error)
        {
            error = string.Empty;
            if (snapshotBytes < 0 || snapshotBytes > SnapshotFormat.MaxFileBytes)
            {
                error = "snapshot length is outside the diagnostic contract";
                return false;
            }
            long total = checked((long)Magic.Length + FixedEnvelopeBytes
                + snapshotBytes + recordSectionBytes);
            if (recordSectionBytes < 0 || total > MaxDiagnosticBytes)
            {
                error =
                    $"diagnostic evidence byte budget exceeded ({total} > {MaxDiagnosticBytes}); " +
                    "complete diagnostic unavailable";
                return false;
            }
            return true;
        }

        private static bool TryValidateHeader(
            byte localSlot, uint desyncTick, ulong stateHash, byte[] snapshotBytes,
            DiagnosticRecordSpool recordSpool, out string error)
        {
            error = string.Empty;
            if (localSlot >= CommandLimits.ReservedPlayerSlots)
            {
                error = "local slot is outside the reserved range";
                return false;
            }
            if (snapshotBytes == null)
            {
                error = "snapshot is null";
                return false;
            }
            if (!TryReadSnapshotIdentity(
                    snapshotBytes, out uint snapshotTick, out ulong snapshotHash, out string snapshotError))
            {
                error = $"snapshot is not canonical ({snapshotError})";
                return false;
            }
            if (snapshotTick != desyncTick || snapshotHash != stateHash)
            {
                error = "snapshot tick/hash does not match the diagnostic checkpoint";
                return false;
            }
            if (recordSpool == null)
            {
                error = "record stream is null";
                return false;
            }
            if (!CanFit(snapshotBytes.Length, 0, out error))
            {
                return false;
            }
            return true;
        }

        private static void WriteBlob(Stream stream, byte[] blob)
        {
            WriteUInt32(stream, unchecked((uint)blob.Length));
            stream.Write(blob, 0, blob.Length);
        }

        private static void WriteUInt32(Stream stream, uint value)
        {
            var bytes = new byte[4];
            RelayProtocol.WriteUInt32(bytes, 0, value);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static void WriteUInt64(Stream stream, ulong value)
        {
            var bytes = new byte[8];
            RelayProtocol.WriteUInt64(bytes, 0, value);
            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>Reads the canonical tick and state hash bound into a snapshot.</summary>
        public static bool TryReadSnapshotIdentity(
            byte[] snapshotBytes, out uint tick, out ulong stateHash, out string error)
        {
            tick = 0;
            stateHash = 0;
            error = string.Empty;
            if (snapshotBytes == null)
            {
                error = "snapshot is null";
                return false;
            }
            if (!SnapshotReader.TryRead(
                    snapshotBytes, out SnapshotFile snapshot, out SnapshotReadError readError))
            {
                error = $"snapshot parse failed ({readError})";
                return false;
            }
            if (!snapshot.TryGetBlock(SnapshotBlockIds.Kernel, out byte[] kernelBlock))
            {
                error = "snapshot has no kernel block";
                return false;
            }
            var reader = new SnapshotBlockReader(kernelBlock);
            if (!reader.TryReadUInt8(out byte version) || version != 1 || !reader.TryReadUInt32(out tick))
            {
                error = "snapshot kernel block has no supported tick";
                tick = 0;
                return false;
            }
            stateHash = snapshot.StateHash;
            return true;
        }

        private static bool TryReadBlob(byte[] source, ref int offset, int maximumLength, out byte[] blob)
        {
            blob = null;
            if (!TryReadUInt32(source, ref offset, out uint length)
                || length > (uint)maximumLength || length > (uint)(source.Length - offset))
            {
                return false;
            }
            blob = new byte[(int)length];
            Array.Copy(source, offset, blob, 0, (int)length);
            offset += (int)length;
            return true;
        }

        private static bool TryReadUInt32(byte[] source, ref int offset, out uint value)
        {
            value = 0;
            if (source.Length - offset < 4) return false;
            value = RelayProtocol.ReadUInt32(source, offset);
            offset += 4;
            return true;
        }

        private static bool TryReadUInt64(byte[] source, ref int offset, out ulong value)
        {
            value = 0;
            if (source.Length - offset < 8) return false;
            value = RelayProtocol.ReadUInt64(source, offset);
            offset += 8;
            return true;
        }
    }

    /// <summary>
    /// Bounded, disk-backed capture of the canonical records a client
    /// actually applied. Entries stay length-prefixed so a final diagnostic
    /// can stream them without retaining one managed object per command.
    /// </summary>
    internal sealed class DiagnosticRecordSpool : IDisposable
    {
        private readonly long _maximumBytes;
        private FileStream _stream;
        private string _path;
        private CommandRecord _lastRecord;
        private readonly uint[] _lastSequenceBySlot =
            new uint[CommandLimits.ReservedPlayerSlots];
        private bool _hasLastRecord;
        private bool _disposed;
        private long _byteLength;

        public long ByteLength => _byteLength;
        public uint RecordCount { get; private set; }

        public DiagnosticRecordSpool(
            long maximumBytes = DesyncDiagnostic.MaxRecordSectionBytes)
        {
            if (maximumBytes < 0 || maximumBytes > DesyncDiagnostic.MaxRecordSectionBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumBytes));
            }
            _maximumBytes = maximumBytes;
        }

        public bool TryAppend(byte[] recordBytes, out string error)
        {
            error = string.Empty;
            if (_disposed)
            {
                error = "diagnostic record spool is disposed";
                return false;
            }
            if (recordBytes == null
                || !CommandRecord.TryDeserialize(
                    recordBytes, out CommandRecord record, out int consumed)
                || consumed != recordBytes.Length
                || record.PlayerSlot >= CommandLimits.ReservedPlayerSlots
                || record.Sequence == 0
                || record.Sequence <= _lastSequenceBySlot[record.PlayerSlot]
                || !CommandPayloadValidation.TryValidateStreamPayload(
                    record.Kind, record.PayloadVersion, record.Payload.Span, out _))
            {
                error = "diagnostic record spool received a malformed record";
                return false;
            }
            if (_hasLastRecord && CommandBatch.CompareRecords(_lastRecord, record) >= 0)
            {
                error = "diagnostic record spool is not in strict canonical order";
                return false;
            }

            long required = checked(_byteLength + 4L + recordBytes.Length);
            if (required > _maximumBytes)
            {
                error =
                    $"diagnostic record-stream byte budget exceeded " +
                    $"({required} > {_maximumBytes}); complete diagnostic unavailable";
                return false;
            }
            if (RecordCount == uint.MaxValue)
            {
                error = "diagnostic record count overflow";
                return false;
            }

            try
            {
                EnsureOpen();
                WriteUInt32(_stream, unchecked((uint)recordBytes.Length));
                _stream.Write(recordBytes, 0, recordBytes.Length);
                _byteLength = required;
                RecordCount++;
                _lastRecord = record;
                _hasLastRecord = true;
                _lastSequenceBySlot[record.PlayerSlot] = record.Sequence;
                return true;
            }
            catch (Exception exception)
            {
                error = $"diagnostic record spool write failed: {exception.Message}";
                return false;
            }
        }

        public bool TryCopyThroughTick(
            Stream destination, uint terminalTick, long maximumOutputBytes,
            out uint copiedRecords, out string error)
        {
            copiedRecords = 0;
            error = string.Empty;
            if (_disposed)
            {
                error = "diagnostic record spool is disposed";
                return false;
            }
            if (destination == null || !destination.CanSeek)
            {
                error = "diagnostic output must be seekable";
                return false;
            }
            if (_stream == null) return true;

            try
            {
                _stream.Flush();
                _stream.Position = 0;
                CommandRecord previous = default;
                bool hasPrevious = false;
                var lastSequenceBySlot = new uint[CommandLimits.ReservedPlayerSlots];
                var lengthBytes = new byte[4];
                while (_stream.Position < _byteLength)
                {
                    if (!TryReadExact(_stream, lengthBytes, 0, lengthBytes.Length))
                    {
                        error = "diagnostic record spool has a truncated length";
                        return false;
                    }
                    uint length = RelayProtocol.ReadUInt32(lengthBytes, 0);
                    if (length < CommandLimits.HeaderBytes
                        || length > CommandLimits.MaxRecordBytes
                        || length > _byteLength - _stream.Position)
                    {
                        error = "diagnostic record spool has an invalid record length";
                        return false;
                    }
                    var raw = new byte[(int)length];
                    if (!TryReadExact(_stream, raw, 0, raw.Length)
                        || !CommandRecord.TryDeserialize(
                            raw, out CommandRecord record, out int consumed)
                        || consumed != raw.Length
                        || record.PlayerSlot >= CommandLimits.ReservedPlayerSlots
                        || record.Sequence == 0
                        || record.Sequence <= lastSequenceBySlot[record.PlayerSlot]
                        || !CommandPayloadValidation.TryValidateStreamPayload(
                            record.Kind, record.PayloadVersion, record.Payload.Span, out _))
                    {
                        error = "diagnostic record spool contains a malformed record";
                        return false;
                    }
                    if (hasPrevious && CommandBatch.CompareRecords(previous, record) >= 0)
                    {
                        error = "diagnostic record spool is not in strict canonical order";
                        return false;
                    }
                    previous = record;
                    hasPrevious = true;
                    lastSequenceBySlot[record.PlayerSlot] = record.Sequence;
                    if (record.TargetTick > terminalTick) break;

                    long required = checked(destination.Position + 4L + raw.Length);
                    if (required > maximumOutputBytes)
                    {
                        error =
                            $"diagnostic evidence byte budget exceeded " +
                            $"({required} > {maximumOutputBytes}); complete diagnostic unavailable";
                        return false;
                    }
                    destination.Write(lengthBytes, 0, lengthBytes.Length);
                    destination.Write(raw, 0, raw.Length);
                    copiedRecords++;
                }
                return true;
            }
            catch (Exception exception)
            {
                error = $"diagnostic record spool read failed: {exception.Message}";
                return false;
            }
            finally
            {
                if (_stream != null) _stream.Position = _byteLength;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _stream?.Dispose(); } catch { /* best effort */ }
            _stream = null;
            if (!string.IsNullOrEmpty(_path) && File.Exists(_path))
            {
                try { File.Delete(_path); } catch { /* DeleteOnClose is the fallback */ }
            }
        }

        private void EnsureOpen()
        {
            if (_stream != null) return;
            _path = Path.Combine(
                Path.GetTempPath(), $"nova-diag-records-{Guid.NewGuid():N}.tmp");
            _stream = new FileStream(
                _path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read,
                4096, FileOptions.DeleteOnClose | FileOptions.SequentialScan);
        }

        private static void WriteUInt32(Stream stream, uint value)
        {
            var bytes = new byte[4];
            RelayProtocol.WriteUInt32(bytes, 0, value);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static bool TryReadExact(
            Stream stream, byte[] buffer, int offset, int count)
        {
            int readTotal = 0;
            while (readTotal < count)
            {
                int read = stream.Read(buffer, offset + readTotal, count - readTotal);
                if (read <= 0) return false;
                readTotal += read;
            }
            return true;
        }
    }
}
