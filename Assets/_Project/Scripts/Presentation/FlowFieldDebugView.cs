using UnityEngine;
using Nova.Gameplay.Match;
using Nova.Simulation.Pathfinding;

namespace Nova.Presentation
{
    /// <summary>
    /// Presentation component visualizing FlowField directional vectors and CostField terrain costs in Unity Scene View via Gizmos.
    /// </summary>
    [DisallowMultipleComponent]
    public class FlowFieldDebugView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MatchRunner _matchRunner;

        [Header("Visualization Settings")]
        [SerializeField] private bool _showFlowVectors = true;
        [SerializeField] private bool _showCostObstacles = true;
        [SerializeField] private float _vectorLength = 0.4f;

        private void OnDrawGizmos()
        {
            if (_matchRunner == null || _matchRunner.Pathfinding == null) return;

            PathfindingSystem pathfinding = _matchRunner.Pathfinding;
            CostField costField = pathfinding.CostField;
            FlowField flowField = pathfinding.FlowField;

            if (costField == null || flowField == null) return;

            ushort width = costField.Width;
            ushort height = costField.Height;

            for (ushort y = 0; y < height; y++)
            {
                for (ushort x = 0; x < width; x++)
                {
                    Vector3 cellCenter = new Vector3(x + 0.5f, 0.1f, y + 0.5f);

                    // Draw Impassable Obstacles
                    if (_showCostObstacles && costField.GetCost(x, y) == CostField.ImpassableCost)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireCube(cellCenter, new Vector3(0.9f, 0.2f, 0.9f));
                        continue;
                    }

                    // Draw Flow Vectors
                    if (_showFlowVectors)
                    {
                        Direction2D dir = flowField.GetDirection(x, y);
                        if (dir != Direction2D.None)
                        {
                            var (dx, dy) = Direction2DUtility.GetOffset(dir);
                            Vector3 dirVector = new Vector3(dx, 0f, dy).normalized * _vectorLength;

                            Gizmos.color = Color.cyan;
                            Gizmos.DrawRay(cellCenter, dirVector);
                        }
                    }
                }
            }
        }
    }
}
