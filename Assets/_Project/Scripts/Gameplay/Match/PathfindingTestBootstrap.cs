using UnityEngine;
using Nova.Core;
using Nova.Gameplay;
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
            // AI-less local debug scene. The units use the session's own slot
            // so their movement can enter through the same ownership-checked
            // command ingress as real device input.
            _matchRunner.InitializeMatch(seed: 0xAE70123456789000UL, width: _mapWidth, height: _mapHeight, maxUnits: 1024, enableSkirmishAi: false);
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

            // Spawn units at bottom-left, then submit one canonical MoveTo
            // intent. RtsIntentDispatcher chunks the 500 ids at the schema-v1
            // limit; no presentation/debug code mutates UnitState by ref.
            byte localSlot = _matchRunner.Session.LocalSlot;
            var unitIds = new EntityId[_unitCount];
            for (int i = 0; i < _unitCount; i++)
            {
                float spawnX = 10f + (i % 25) * 1.2f;
                float spawnY = 10f + (i / 25) * 1.2f;

                unitIds[i] = _matchRunner.Entities.SpawnUnit(
                    localSlot,
                    new Transform2D(SimFixed.FromFloat(spawnX), SimFixed.FromFloat(spawnY)),
                    moveSpeed: SimFixed.FromFloat(6.0f),
                    radius: SimFixed.FromFloat(0.4f));
            }

            var stateView = new UnitCommandStateView(
                _matchRunner.Entities,
                _matchRunner.Pathfinding,
                _matchRunner.Economy,
                _matchRunner.Construction,
                _matchRunner.Production);
            var dispatcher = new RtsIntentDispatcher(_matchRunner.Ingress, stateView);
            IntentDispatchResult result = dispatcher.MoveTo(
                unitIds,
                SimFixed.FromInt(targetPos.X),
                SimFixed.FromInt(targetPos.Y));
            if (!result.Accepted)
            {
                Debug.LogError($"[PathfindingTestBootstrap] MoveTo was rejected: {result}.");
                return;
            }

            Debug.Log($"[PathfindingTestBootstrap] Spawned {_unitCount} local units and submitted " +
                      $"{result.CommandCount} MoveTo chunks toward {_destinationCell} around the wall obstacle.");
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
