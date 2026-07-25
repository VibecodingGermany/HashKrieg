using System;
using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.Snapshots;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Block-level codec (EditMode lane): canonical little-endian encoding
    /// and strict truncated-read handling. Mirror of the .NET lane
    /// SnapshotBlockCodecTests.
    /// </summary>
    [TestFixture]
    public class SnapshotV1BlockCodecTests
    {
        [Test]
        public void Writer_EncodesLittleEndianExact()
        {
            var writer = new SnapshotBlockWriter();
            writer.WriteUInt8(0xAB);
            writer.WriteUInt16(0x0102);
            writer.WriteUInt32(0x01020304);
            writer.WriteInt32(-2);
            writer.WriteUInt64(0x0102030405060708UL);
            writer.WriteInt64(-1L);
            writer.WriteSimFixed(SimFixed.One);
            writer.WriteSimAngle(SimAngle.FromRaw(0x8000));
            writer.WriteTick(new Tick(0x01020304));
            writer.WriteEntityId(new EntityId(0x01020304, 0x0506));
            writer.WriteLengthPrefixed(new byte[] { 0x11, 0x22 });

            byte[] expected =
            {
                0xAB,
                0x02, 0x01,
                0x04, 0x03, 0x02, 0x01,
                0xFE, 0xFF, 0xFF, 0xFF,
                0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0x00, 0x00, 0x01, 0x00, // SimFixed.One raw
                0x00, 0x80,
                0x04, 0x03, 0x02, 0x01,
                0x04, 0x03, 0x02, 0x01, 0x06, 0x05, // EntityId index + version
                0x02, 0x00, 0x00, 0x00, 0x11, 0x22,
            };
            Assert.That(writer.ToArray(), Is.EqualTo(expected));
        }

        [Test]
        public void Reader_RoundtripsEveryType()
        {
            var writer = new SnapshotBlockWriter();
            writer.WriteUInt8(0xAB);
            writer.WriteUInt16(0x0102);
            writer.WriteUInt32(0x01020304);
            writer.WriteInt32(-2);
            writer.WriteUInt64(0x0102030405060708UL);
            writer.WriteInt64(-1L);
            writer.WriteSimFixed(SimFixed.FromRaw(-123456));
            writer.WriteSimAngle(SimAngle.FromRaw(0x1234));
            writer.WriteTick(new Tick(99));
            writer.WriteEntityId(new EntityId(7, 3));
            writer.WriteLengthPrefixed(new byte[] { 0x11, 0x22, 0x33 });

            var reader = new SnapshotBlockReader(writer.ToArray());
            Assert.That(reader.TryReadUInt8(out byte u8) && u8 == 0xAB);
            Assert.That(reader.TryReadUInt16(out ushort u16) && u16 == 0x0102);
            Assert.That(reader.TryReadUInt32(out uint u32) && u32 == 0x01020304);
            Assert.That(reader.TryReadInt32(out int i32) && i32 == -2);
            Assert.That(reader.TryReadUInt64(out ulong u64) && u64 == 0x0102030405060708UL);
            Assert.That(reader.TryReadInt64(out long i64) && i64 == -1L);
            Assert.That(
                reader.TryReadSimFixed(out SimFixed fixedValue)
                && fixedValue.RawValue == -123456);
            Assert.That(
                reader.TryReadSimAngle(out SimAngle angle) && angle.RawValue == 0x1234);
            Assert.That(reader.TryReadTick(out Tick tick) && tick.Value == 99);
            Assert.That(
                reader.TryReadEntityId(out EntityId id) && id == new EntityId(7, 3));
            Assert.That(
                reader.TryReadLengthPrefixed(out ReadOnlySpan<byte> data)
                && data.ToArray().Length == 3 && data[2] == 0x33);
            Assert.That(reader.Remaining, Is.EqualTo(0));
        }

        [Test]
        public void Reader_TruncatedAtEveryPrefix_NeverThrows()
        {
            var writer = new SnapshotBlockWriter();
            writer.WriteUInt32(0x01020304);
            writer.WriteEntityId(new EntityId(1, 1));
            writer.WriteLengthPrefixed(new byte[] { 1, 2, 3 });
            byte[] content = writer.ToArray();

            for (int prefix = 0; prefix <= content.Length; prefix++)
            {
                var reader = new SnapshotBlockReader(
                    new ReadOnlySpan<byte>(content, 0, prefix));
                while (reader.TryReadUInt8(out _)) { }
                Assert.That(reader.Remaining, Is.EqualTo(0));
            }
        }

        [Test]
        public void Reader_OversizedLengthPrefix_IsRejected()
        {
            var writer = new SnapshotBlockWriter();
            writer.WriteUInt32(0x7FFFFFFF); // declared length beyond content
            writer.WriteUInt8(0x01);
            var reader = new SnapshotBlockReader(writer.ToArray());
            Assert.That(reader.TryReadLengthPrefixed(out _), Is.False);
        }
    }
}
