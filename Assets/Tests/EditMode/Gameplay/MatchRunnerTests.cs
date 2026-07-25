using NUnit.Framework;
using UnityEngine;
using Nova.Core;
using Nova.Gameplay.Match;

namespace Nova.Gameplay.Tests
{
    [TestFixture]
    public class MatchRunnerTests
    {
        [Test]
        public void MatchRunner_InitializeAndStart_StartsKernelAtTickZero()
        {
            var go = new GameObject("TestMatchRunner");
            var runner = go.AddComponent<MatchRunner>();

            runner.InitializeMatch(100UL, 64, 64, 512);
            runner.StartMatch();

            Assert.IsTrue(runner.IsRunning);
            Assert.AreEqual(Tick.Zero, runner.Kernel.CurrentTick);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void MatchRunner_PauseMatch_StopsKernelExecution()
        {
            var go = new GameObject("TestMatchRunner");
            var runner = go.AddComponent<MatchRunner>();

            runner.InitializeMatch(100UL, 64, 64, 512);
            runner.StartMatch();
            runner.PauseMatch();

            Assert.IsFalse(runner.IsRunning);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void MatchRunner_TickRate_IsCanonical10Hz()
        {
            // F-006 regression: the host tick delta is the canonical 10 Hz
            // constant, not a local 20 Hz value.
            Assert.AreEqual(10, SimClock.TicksPerSecond);
            Assert.AreEqual(0.1f, SimClock.TickDeltaSeconds, 1e-7f);
            Assert.AreEqual(SimClock.TickDeltaSeconds, MatchRunner.TickDeltaTime);
        }
    }
}
