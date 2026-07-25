using UnityEngine;
using Nova.Core;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;
using EntityId = Nova.Core.EntityId;

namespace Nova.Gameplay.Match
{
    /// <summary>
    /// Test bootstrap component for demonstrating 500 units moving via Flow-Field navigation in Unity.
    /// Attaches MatchRunner and UnitViewManager dynamically.
    /// </summary>
    [DisallowMultipleComponent]
    public class PathfindingTestBootstrap : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private int _unitCount = 500;
        [SerializeField] private ushort _mapWidth = 128;
        [SerializeField] private ushort _mapHeight = 128;
        [SerializeField] private Vector2Int _destinationCell = new Vector2Int(110, 110);

        private MatchRunner _matchRunner;
        private UnitViewManager _viewManager;

        private void Awake()
        {
            _matchRunner = gameObject.AddComponent<MatchRunner>();
            _viewManager = gameObject.AddComponent<UnitViewManager>();
        }

        private void Start()
        {
            _matchRunner.InitializeMatch(seed: 0xAE70123456789000UL, width: _mapWidth, height: _mapHeight, maxUnits: 1024);
            _viewManager.Initialize(_matchRunner);

            _matchRunner.StartMatch();

            // Set flow field target
            var targetPos = new GridPos2D(_destinationCell.x, _destinationCell.y);
            _matchRunner.Pathfinding.RequestFlowField(targetPos);

            // Add wall obstacle across middle map
            for (ushort y = 40; y <= 80; y++)
            {
                _matchRunner.Pathfinding.CostField.SetCost(60, y, CostField.ImpassableCost);
            }
            // Re-calculate flow field after adding obstacle
            _matchRunner.Pathfinding.RequestFlowField(targetPos);

            // Spawn units at bottom-left corner
            for (int i = 0; i < _unitCount; i++)
            {
                float spawnX = 10f + (i % 25) * 1.2f;
                float spawnY = 10f + (i / 25) * 1.2f;

                EntityId id = _matchRunner.Entities.SpawnUnit(1, new Transform2D(spawnX, spawnY), moveSpeed: 6.0f, radius: 0.4f);
                ref UnitState unit = ref _matchRunner.Entities.GetUnitRef(id);
                unit.SetTarget(targetPos);
            }

            Debug.Log($"[PathfindingTestBootstrap] Spawned {_unitCount} units heading to target {_destinationCell} around wall obstacle.");
        }

        private void OnDrawGizmos()
        {
            // Gizmo visualization for target cell
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(new Vector3(_destinationCell.x + 0.5f, 0.5f, _destinationCell.y + 0.5f), new Vector3(1f, 1f, 1f));

            // Wall obstacle visualization
            Gizmos.color = Color.red;
            for (int y = 40; y <= 80; y++)
            {
                Gizmos.DrawCube(new Vector3(60.5f, 0.5f, y + 0.5f), new Vector3(1f, 1f, 1f));
            }
        }
    }
}
