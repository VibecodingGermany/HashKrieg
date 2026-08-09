using NUnit.Framework;
using Nova.AI;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// The wave threshold as arithmetic, at the states a match cannot produce
    /// on demand.
    /// <para>
    /// WHY THIS FIXTURE EXISTS: mutation testing against the match-level tests
    /// alone. With the arithmetic reachable only through a whole match, both
    /// deleting the negative clamp and ignoring <c>waveStrengthPoints</c>
    /// entirely left the suite green — the shipped army cap simply never
    /// reaches the states that would tell the difference. Every case below is
    /// one that survived.
    /// </para>
    /// <para>
    /// The numbers are the shipped ones: an Alliance rifleman is 100 points, a
    /// Legion recruit 44, the wave asks for 1.200.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class WaveStrengthGateTests
    {
        private const int WavePoints = 1200;
        private const int Rifleman = 100;
        private const int Recruit = 44;

        // ----------------------------------------------------------------
        // (a) The threshold clause — the one the shipped cap never reaches
        // ----------------------------------------------------------------

        /// <summary>
        /// With room to spare, the wave marches on the POINT threshold and not
        /// on an exhausted army cap. Twelve riflemen are exactly 1.200.
        /// <para>
        /// This is the case that pins <c>waveStrengthPoints</c> as a threshold
        /// at all: replace the <c>min</c> with "return what the ring can grow
        /// to" and this test is the one that notices.
        /// </para>
        /// </summary>
        [Test]
        public void TheWaveMarchesOnThePointThreshold_WhileTheCapStillHasRoom()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Ready(gatheredStrength: 1200, gathered: 12, committed: 0, armyCap: 30), Is.True,
                    "twelve riflemen are 1.200 points and the cap allows eighteen more — the threshold released it");
                Assert.That(Ready(gatheredStrength: 1100, gathered: 11, committed: 0, armyCap: 30), Is.False,
                    "eleven riflemen are 1.100 and production can still deliver — nothing forces the wave out yet");

                Assert.That(Threshold(gatheredStrength: 1100, gathered: 11, committed: 0, armyCap: 30),
                    Is.EqualTo(WavePoints),
                    "with room to grow the threshold IS the profile value, not the ceiling");
            });
        }

        /// <summary>
        /// The Legion needs 28 recruits for the same 1.200 points, and that is
        /// the whole point of measuring in strength: twelve of each are not the
        /// same wave.
        /// </summary>
        [Test]
        public void TheWeakerUnitHasToGatherMoreHeadsForTheSameWave()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Ready(27 * Recruit, gathered: 27, committed: 0, armyCap: 40, unit: Recruit), Is.False,
                    "27 recruits are 1.188 points — one short");
                Assert.That(Ready(28 * Recruit, gathered: 28, committed: 0, armyCap: 40, unit: Recruit), Is.True,
                    "28 recruits are 1.232 points");
            });
        }

        // ----------------------------------------------------------------
        // (b) The ceiling clause — never wait for what cannot arrive
        // ----------------------------------------------------------------

        /// <summary>
        /// The r5 rule in points: when the army cap is spent, the ring holds
        /// everything it is ever going to hold and the wave marches, however
        /// far short of the threshold it is. Without this the survivors of an
        /// earlier wave stall every later one — measured in r4 as eleven units
        /// standing at the staging cell until the time limit.
        /// </summary>
        [Test]
        public void AWaveThatCanNoWayGrowFurtherMarchesShort()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Ready(gatheredStrength: 300, gathered: 3, committed: 9, armyCap: 12), Is.True,
                    "three at home, nine already out, cap twelve — nothing else is coming");
                Assert.That(Ready(gatheredStrength: 300, gathered: 3, committed: 8, armyCap: 12), Is.False,
                    "one head still free, so the wave waits for it");
            });
        }

        /// <summary>
        /// WOUNDED UNITS MAY NOT STALL THE WAVE. A ring full of half-dead
        /// survivors is worth less than the threshold and can never make it up,
        /// because the cap is spent — the gate has to let them go.
        /// <para>
        /// This is the defect the lab caught in the first version, which put a
        /// floor of one full-health unit under the threshold: the canonical
        /// match ran 1.650 ticks longer.
        /// </para>
        /// </summary>
        [Test]
        public void WoundedSurvivorsAtAFullCapStillMarch()
        {
            Assert.That(Ready(gatheredStrength: 12 * 30, gathered: 12, committed: 0, armyCap: 12), Is.True,
                "twelve riflemen at a third health are 360 points, and no thirteenth can be built");
        }

        /// <summary>
        /// A cap already overspent — more combat units alive than it allows —
        /// must not push the ceiling BELOW what is standing there, which would
        /// open the gate for a reason that is arithmetic rather than a rule.
        /// <para>
        /// Deleting the clamp leaves every match-level test green, because
        /// production never overshoots the cap on its own. It can still happen:
        /// the cap is compared against alive plus queued, and a queued unit
        /// finishing is not asked for permission twice.
        /// </para>
        /// </summary>
        [Test]
        public void MoreUnitsAliveThanTheCapAllowsDoesNotProduceANegativeCeiling()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Threshold(gatheredStrength: 500, gathered: 5, committed: 10, armyCap: 12),
                    Is.EqualTo(500),
                    "fifteen alive against a cap of twelve — the ceiling is what stands there, not less");
                Assert.That(Ready(gatheredStrength: 500, gathered: 5, committed: 10, armyCap: 12), Is.True);
            });
        }

        // ----------------------------------------------------------------
        // (c) A free head is only free while something can build into it
        // ----------------------------------------------------------------

        /// <summary>
        /// With the Barracks gone, the free heads of the army cap are a promise
        /// nobody can keep. The wave has to march on what it has instead of
        /// waiting out the match — the r4 stall, one level up.
        /// </summary>
        [Test]
        public void WithoutAProducerTheFreeHeadsDoNotCount()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Ready(gatheredStrength: 1100, gathered: 11, committed: 0, armyCap: 30,
                    canProduce: false), Is.True,
                    "no Barracks: 1.100 points is everything this army will ever have, so it goes");
                Assert.That(Ready(gatheredStrength: 1100, gathered: 11, committed: 0, armyCap: 30,
                    canProduce: true), Is.False,
                    "with a Barracks the same army waits — the two answers must differ, or the flag is dead");
            });
        }

        // ----------------------------------------------------------------
        // (d) Degenerate states
        // ----------------------------------------------------------------

        /// <summary>
        /// Nothing gathered and the cap spent on units already fighting: the
        /// threshold collapses to zero and the flag reads "ready". Harmless by
        /// construction — with no gathered unit there is nobody the flag could
        /// send anywhere — and pinned here so the collapse stays a known state
        /// rather than a surprise.
        /// </summary>
        [Test]
        public void AnEmptyRingAtASpentCapIsAZeroThreshold()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Threshold(gatheredStrength: 0, gathered: 0, committed: 12, armyCap: 12),
                    Is.EqualTo(0));
                Assert.That(Ready(gatheredStrength: 0, gathered: 0, committed: 12, armyCap: 12), Is.True);
            });
        }

        /// <summary>
        /// An empty ring that production can still fill waits — the gate must
        /// not read "ready" merely because nothing is there yet.
        /// </summary>
        [Test]
        public void AnEmptyRingWithRoomToBuildWaits()
        {
            Assert.That(Ready(gatheredStrength: 0, gathered: 0, committed: 0, armyCap: 12), Is.False);
        }

        /// <summary>
        /// The comparison is "at least", not "more than": a ring holding
        /// exactly the threshold marches. One operator, pinned once.
        /// </summary>
        [Test]
        public void ExactlyTheThresholdIsEnough()
        {
            Assert.That(Ready(gatheredStrength: 1200, gathered: 12, committed: 0, armyCap: 40), Is.True);
            Assert.That(Ready(gatheredStrength: 1199, gathered: 12, committed: 0, armyCap: 40), Is.False);
        }

        // ----------------------------------------------------------------

        private static long Threshold(
            long gatheredStrength, int gathered, int committed, int armyCap,
            int unit = Rifleman, bool canProduce = true) =>
            WaveStrengthGate.Threshold(
                WavePoints, gatheredStrength, gathered, committed, unit, armyCap, canProduce);

        private static bool Ready(
            long gatheredStrength, int gathered, int committed, int armyCap,
            int unit = Rifleman, bool canProduce = true) =>
            WaveStrengthGate.IsReady(
                WavePoints, gatheredStrength, gathered, committed, unit, armyCap, canProduce);
    }
}
