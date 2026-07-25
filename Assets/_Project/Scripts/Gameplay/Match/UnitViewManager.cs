using System;
using System.Collections.Generic;
using UnityEngine;
using Nova.Core;
using Nova.Simulation.State;
using EntityId = Nova.Core.EntityId;

namespace Nova.Gameplay.Match
{
    /// <summary>
    /// Manages Unity visual GameObjects representing simulation units.
    /// Smoothly interpolates render transforms at full frame rate (60/120+ FPS) from the 20-Hz simulation state.
    /// </summary>
    [DisallowMultipleComponent]
    public class UnitViewManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MatchRunner _matchRunner;
        [SerializeField] private GameObject _unitPrefab;
        [SerializeField] private float _interpolationSpeed = 25f;

        private GameObject[] _viewInstances;
        private EntityId[] _boundIds;

        public void Initialize(MatchRunner runner, GameObject unitPrefab = null)
        {
            _matchRunner = runner ?? throw new ArgumentNullException(nameof(runner));
            _unitPrefab = unitPrefab;

            int capacity = runner.Entities.Capacity;
            _viewInstances = new GameObject[capacity];
            _boundIds = new EntityId[capacity];
        }

        private void LateUpdate()
        {
            if (_matchRunner == null || !_matchRunner.IsRunning) return;

            EntityManager entities = _matchRunner.Entities;
            UnitState[] units = entities.RawUnits;
            int capacity = entities.Capacity;

            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState unit = ref units[i];

                if (unit.IsActive)
                {
                    // Spawn view instance if not existing or assigned to older version
                    if (_viewInstances[i] == null || _boundIds[i] != unit.Id)
                    {
                        SpawnViewInstance(i, in unit);
                    }

                    // Interpolate render position and rotation
                    GameObject viewObj = _viewInstances[i];
                    Vector3 targetPos = new Vector3(unit.Transform.PositionX, 0.5f, unit.Transform.PositionY);
                    Quaternion targetRot = Quaternion.Euler(0f, unit.Transform.Rotation * Mathf.Rad2Deg, 0f);

                    viewObj.transform.position = Vector3.Lerp(viewObj.transform.position, targetPos, Time.deltaTime * _interpolationSpeed);
                    viewObj.transform.rotation = Quaternion.Slerp(viewObj.transform.rotation, targetRot, Time.deltaTime * _interpolationSpeed);
                }
                else if (_viewInstances[i] != null)
                {
                    // Despawn view instance when entity becomes inactive
                    Destroy(_viewInstances[i]);
                    _viewInstances[i] = null;
                    _boundIds[i] = EntityId.Invalid;
                }
            }
        }

        private void SpawnViewInstance(int index, in UnitState unit)
        {
            if (_viewInstances[index] != null)
            {
                Destroy(_viewInstances[index]);
            }

            GameObject instance;
            if (_unitPrefab != null)
            {
                instance = Instantiate(_unitPrefab, transform);
            }
            else
            {
                // Fallback procedural capsule visualization
                instance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                instance.transform.SetParent(transform, false);
                instance.transform.localScale = new Vector3(unit.Radius * 2f, 1f, unit.Radius * 2f);
            }

            instance.name = $"UnitView_{unit.Id.Index}_{unit.Id.Version}";
            instance.transform.position = new Vector3(unit.Transform.PositionX, 0.5f, unit.Transform.PositionY);
            instance.transform.rotation = Quaternion.Euler(0f, unit.Transform.Rotation * Mathf.Rad2Deg, 0f);

            _viewInstances[index] = instance;
            _boundIds[index] = unit.Id;
        }

        private void OnDestroy()
        {
            if (_viewInstances != null)
            {
                for (int i = 0; i < _viewInstances.Length; i++)
                {
                    if (_viewInstances[i] != null)
                    {
                        Destroy(_viewInstances[i]);
                    }
                }
            }
        }
    }
}
