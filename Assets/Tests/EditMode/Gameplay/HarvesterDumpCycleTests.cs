using NUnit.Framework;
using Nova.Gameplay.Match;

namespace Nova.Gameplay.Tests
{
    /// <summary>
    /// The unload timeline of a harvester view (pure logic): ease in/out of
    /// the bed, the pile letting go while it is tipped, a total of about two seconds.
    /// </summary>
    [TestFixture]
    public class HarvesterDumpCycleTests
    {
        private static HarvesterDumpCycle Run(float seconds)
        {
            var c = new HarvesterDumpCycle();
            c.Start();
            const float step = 1f / 60f;
            // Whole frames plus the remainder: a float accumulator stops up to a frame short.
            int frames = (int)(seconds / step);
            for (int i = 0; i < frames; i++) c.Advance(step);
            c.Advance(seconds - frames * step);
            return c;
        }

        [Test]
        public void Idle_IsFlatAndInactive()
        {
            var c = new HarvesterDumpCycle();
            Assert.IsFalse(c.Active);
            Assert.AreEqual(0f, c.Tilt01);
            Assert.AreEqual(0f, c.Release01);
        }

        [Test]
        public void Tilt_RisesWithEaseInOut_HoldsAndReturns()
        {
            Assert.That(Run(0.05f).Tilt01, Is.LessThan(0.02f), "ease-in: slow start");
            Assert.That(Run(HarvesterDumpCycle.TiltUpSeconds * 0.5f).Tilt01, Is.EqualTo(0.5f).Within(0.03f));
            Assert.That(Run(HarvesterDumpCycle.TiltUpSeconds + 0.2f).Tilt01, Is.EqualTo(1f).Within(1e-3f), "hold");
            float back = Run(HarvesterDumpCycle.TiltUpSeconds + HarvesterDumpCycle.HoldSeconds + HarvesterDumpCycle.ReturnSeconds * 0.5f).Tilt01;
            Assert.That(back, Is.EqualTo(0.5f).Within(0.03f), "halfway back at half the return time");
        }

        [Test]
        public void Release_StartsWhenTipped_AndFinishes()
        {
            Assert.AreEqual(0f, Run(HarvesterDumpCycle.ReleaseStartSeconds - 0.02f).Release01, 1e-4f);
            float mid = Run(HarvesterDumpCycle.ReleaseStartSeconds + HarvesterDumpCycle.ReleaseSeconds * 0.5f).Release01;
            Assert.That(mid, Is.EqualTo(0.25f).Within(0.03f), "accelerating slide");
            Assert.That(Run(HarvesterDumpCycle.ReleaseStartSeconds + HarvesterDumpCycle.ReleaseSeconds + 0.02f).Release01, Is.EqualTo(1f).Within(1e-3f));
        }

        [Test]
        public void Cycle_EndsAfterAboutTwoSeconds()
        {
            Assert.That(HarvesterDumpCycle.TotalSeconds, Is.InRange(1.7f, 2.1f));
            Assert.IsTrue(Run(HarvesterDumpCycle.TotalSeconds - 0.05f).Active);
            var done = Run(HarvesterDumpCycle.TotalSeconds + 0.1f);
            Assert.IsFalse(done.Active);
            Assert.AreEqual(0f, done.Tilt01);
        }

        [Test]
        public void Restart_BeginsAgain_AndResetStops()
        {
            var c = Run(1.0f);
            c.Start();
            Assert.That(c.Tilt01, Is.EqualTo(0f).Within(1e-4f));
            c.Advance(0.1f);
            c.Reset();
            Assert.IsFalse(c.Active);
        }
    }
}
