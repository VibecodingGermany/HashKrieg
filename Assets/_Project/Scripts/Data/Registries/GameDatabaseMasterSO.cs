using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nova.Data.Registries
{
    /// <summary>
    /// Master Index Aggregator ScriptableObject (D-049 Sharding).
    /// Holds references to the 8 category sub-registries.
    /// Generated and validated deterministically by Nova.Editor tooling.
    /// </summary>
    [CreateAssetMenu(fileName = "GameDatabaseMaster", menuName = "Project Nova/Registries/Master Database Index")]
    public class GameDatabaseMasterSO : ScriptableObject
    {
        [Header("Category Sub-Registries")]
        [SerializeField] private UnitRegistrySO _unitRegistry;
        [SerializeField] private BuildingRegistrySO _buildingRegistry;
        [SerializeField] private WeaponRegistrySO _weaponRegistry;

        public UnitRegistrySO UnitRegistry => _unitRegistry;
        public BuildingRegistrySO BuildingRegistry => _buildingRegistry;
        public WeaponRegistrySO WeaponRegistry => _weaponRegistry;

        public void SetSubRegistries(UnitRegistrySO unitReg, BuildingRegistrySO bldReg, WeaponRegistrySO wpnReg)
        {
            _unitRegistry = unitReg;
            _buildingRegistry = bldReg;
            _weaponRegistry = wpnReg;
        }

        public bool ValidateMasterIndex(out string errorMessage)
        {
            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            if (_unitRegistry != null && _unitRegistry.Entries != null)
            {
                foreach (var unit in _unitRegistry.Entries)
                {
                    if (unit == null)
                    {
                        errorMessage = "UnitRegistry contains a null reference.";
                        return false;
                    }
                    if (string.IsNullOrWhiteSpace(unit.Id))
                    {
                        errorMessage = $"Unit asset '{unit.name}' has empty or missing ID.";
                        return false;
                    }
                    if (!seenIds.Add(unit.Id))
                    {
                        errorMessage = $"Duplicate ID detected: '{unit.Id}' across definitions.";
                        return false;
                    }
                }
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
