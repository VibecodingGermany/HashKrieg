using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nova.Data
{
    [Serializable]
    public struct UnitAssetMapping
    {
        public int DefinitionId;
        public GameObject ModelPrefab;
    }

    [Serializable]
    public struct BuildingAssetMapping
    {
        public int DefinitionId;
        public GameObject ModelPrefab;
    }

    /// <summary>
    /// ScriptableObject registry mapping Simulation Definition IDs to 3D visual prefab assets.
    /// Manages 27 unit definitions and 24 building definitions from Sprint 5 Asset Audit.
    /// </summary>
    [CreateAssetMenu(fileName = "AssetMappingRegistry", menuName = "Project Nova/Data/Asset Mapping Registry")]
    public class AssetMappingRegistrySO : ScriptableObject
    {
        [SerializeField] private List<UnitAssetMapping> _unitMappings = new List<UnitAssetMapping>();
        [SerializeField] private List<BuildingAssetMapping> _buildingMappings = new List<BuildingAssetMapping>();

        public IReadOnlyList<UnitAssetMapping> UnitMappings => _unitMappings;
        public IReadOnlyList<BuildingAssetMapping> BuildingMappings => _buildingMappings;

        public void RegisterUnitMapping(int definitionId, GameObject prefab)
        {
            _unitMappings.Add(new UnitAssetMapping { DefinitionId = definitionId, ModelPrefab = prefab });
        }

        public void RegisterBuildingMapping(int definitionId, GameObject prefab)
        {
            _buildingMappings.Add(new BuildingAssetMapping { DefinitionId = definitionId, ModelPrefab = prefab });
        }

        public GameObject GetUnitPrefab(int definitionId)
        {
            for (int i = 0; i < _unitMappings.Count; i++)
            {
                if (_unitMappings[i].DefinitionId == definitionId)
                    return _unitMappings[i].ModelPrefab;
            }
            return null;
        }

        public GameObject GetBuildingPrefab(int definitionId)
        {
            for (int i = 0; i < _buildingMappings.Count; i++)
            {
                if (_buildingMappings[i].DefinitionId == definitionId)
                    return _buildingMappings[i].ModelPrefab;
            }
            return null;
        }
    }
}
