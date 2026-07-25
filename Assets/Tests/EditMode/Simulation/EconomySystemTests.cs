using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.Economy;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    [TestFixture]
    public class EconomySystemTests
    {
        [Test]
        public void PlayerEconomyState_AddAndSpendCredits_UpdatesBalanceCorrectly()
        {
            var eco = new PlayerEconomyState(0, startingCredits: 1000);

            Assert.AreEqual(1000, eco.AetheriumCredits);

            eco.AddCredits(250);
            Assert.AreEqual(1250, eco.AetheriumCredits);

            bool spent = eco.TrySpendCredits(500);
            Assert.IsTrue(spent);
            Assert.AreEqual(750, eco.AetheriumCredits);

            bool cantSpend = eco.TrySpendCredits(2000);
            Assert.IsFalse(cantSpend);
            Assert.AreEqual(750, eco.AetheriumCredits);
        }

        [Test]
        public void EnergyGridSystem_LowPowerDetection_TriggersMultiplierPenalty()
        {
            var grid = new EnergyGridSystem(startingCredits: 500);
            ref PlayerEconomyState p1 = ref grid.GetPlayerEconomy(1);

            Assert.IsFalse(p1.IsLowPower);
            Assert.AreEqual(1.0f, p1.ProductionSpeedMultiplier);

            // Register consumption higher than produced power (Produced: 30, Consumed: 50)
            grid.RegisterPowerConsumption(1, 50);

            Assert.IsTrue(p1.IsLowPower);
            Assert.AreEqual(0.5f, p1.ProductionSpeedMultiplier);
        }
    }
}
