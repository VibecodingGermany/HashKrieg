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
    }
}
