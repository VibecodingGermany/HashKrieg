using NUnit.Framework;
using Nova.Simulation.Pathfinding;

namespace Nova.Simulation.Tests
{
    [TestFixture]
    public class FlowFieldPathfindingTests
    {
        [Test]
        public void IntegrationField_WaveExpansion_ReachesDestinationWithZeroDistance()
        {
            var system = new PathfindingSystem(10, 10);
            var dest = new GridPos2D(5, 5);

            system.RequestFlowField(dest);

            Assert.AreEqual(0, system.IntegrationField.GetDistance(5, 5));
            Assert.Less(system.IntegrationField.GetDistance(4, 5), IntegrationField.Unreachable);
        }

        [Test]
        public void FlowField_GeneratesValidDirectionVector_TowardsDestination()
        {
            var system = new PathfindingSystem(10, 10);
            var dest = new GridPos2D(5, 5);

            system.RequestFlowField(dest);

            // Neighbor at (4, 5) should point East towards (5, 5)
            Direction2D dirWestOfTarget = system.FlowField.GetDirection(4, 5);
            Assert.AreEqual(Direction2D.East, dirWestOfTarget);

            // Neighbor at (5, 4) should point North towards (5, 5)
            Direction2D dirSouthOfTarget = system.FlowField.GetDirection(5, 4);
            Assert.AreEqual(Direction2D.North, dirSouthOfTarget);
        }

        [Test]
        public void FlowField_NavigatesAroundImpassableObstacle()
        {
            var system = new PathfindingSystem(10, 10);
            var dest = new GridPos2D(5, 5);

            // Place a wall obstacle at (4, 5)
            system.CostField.SetCost(4, 5, CostField.ImpassableCost);

            system.RequestFlowField(dest);

            // Cell behind the wall at (3, 5) must not point East (into wall), but route around
            Direction2D dirBehindWall = system.FlowField.GetDirection(3, 5);
            Assert.AreNotEqual(Direction2D.East, dirBehindWall);
            Assert.AreNotEqual(Direction2D.None, dirBehindWall);
        }
    }
}
