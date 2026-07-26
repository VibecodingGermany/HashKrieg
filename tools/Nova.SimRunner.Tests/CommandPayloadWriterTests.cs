using System;
using Nova.Simulation.CommandsV1;
using NUnit.Framework;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Behavior of the canonical payload writer itself (Commands.md section 2):
    /// byte-exact single-value writes, length tracking and the hard structural
    /// cap at <see cref="CommandLimits.MaxPayloadBytes"/>.
    /// </summary>
    [TestFixture]
    public sealed class CommandPayloadWriterTests
    {
        [Test]
        public void WriteUInt8_RoundtripsByteExact_AndTracksLength()
        {
            var writer = new CommandPayloadWriter();
            writer.WriteUInt8(0xAB);
            writer.WriteUInt16(0x1234);

            Assert.That(writer.Length, Is.EqualTo(3));

            var reader = new CommandPayloadReader(writer.ToArray());
            Assert.That(reader.TryReadUInt8(out byte b), Is.True);
            Assert.That(b, Is.EqualTo(0xAB));
            Assert.That(reader.TryReadUInt16(out ushort s), Is.True);
            Assert.That(s, Is.EqualTo(0x1234));
            Assert.That(reader.Remaining, Is.EqualTo(0));
        }

        [Test]
        public void Write_BeyondMaxPayloadBytes_ThrowsStructuralError()
        {
            // Commands.md section 2: an overlong payload is a structural error
            // and must never be produced — the writer fails loudly instead of
            // truncating or growing past the cap.
            var writer = new CommandPayloadWriter();
            Assert.Throws<InvalidOperationException>(() =>
            {
                // Exactly MaxPayloadBytes still fits; the next write crosses the cap.
                for (int i = 0; i <= CommandLimits.MaxPayloadBytes; i += 4)
                {
                    writer.WriteUInt32(unchecked((uint)i));
                }
            });
            Assert.That(writer.Length, Is.LessThanOrEqualTo(CommandLimits.MaxPayloadBytes));
        }
    }
}
