using System.Collections.Generic;
using UnityEngine;
using Nova.Data.Units;

namespace Nova.Data.Registries
{
    /// <summary>
    /// Category Sub-Registry ScriptableObject managing UnitDefinitionSO assets (D-049 Sharding).
    /// </summary>
    [CreateAssetMenu(fileName = "UnitRegistry", menuName = "Project Nova/Registries/Unit Registry")]
    public class UnitRegistrySO : ScriptableObject
    {
        [SerializeField] private List<UnitDefinitionSO> _entries = new List<UnitDefinitionSO>();

        public IReadOnlyList<UnitDefinitionSO> Entries => _entries;

        public void SetEntries(IEnumerable<UnitDefinitionSO> entries)
        {
            _entries.Clear();
            if (entries != null)
            {
                _entries.AddRange(entries);
            }
        }

        public UnitDefinitionSO GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i] != null && _entries[i].Id == id)
                {
                    return _entries[i];
                }
            }
            return null;
        }
    }
}
