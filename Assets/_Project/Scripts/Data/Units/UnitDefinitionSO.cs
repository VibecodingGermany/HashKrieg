using UnityEngine;
using Nova.Simulation.Definitions;

namespace Nova.Data.Units
{
    /// <summary>
    /// Definitions-only ScriptableObject for unit parameters.
    /// Immutable at runtime; converted to UnitDefinition C# struct at match setup.
    /// </summary>
    [CreateAssetMenu(fileName = "UNIT_NewUnit", menuName = "Project Nova/Definitions/Unit Definition")]
    public class UnitDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id = "UNIT_Default";
        [SerializeField] private string _displayName = "Default Unit";

        [Header("Movement & Physical")]
        [SerializeField] private float _moveSpeed = 5.0f;
        [SerializeField] private float _radius = 0.5f;

        [Header("Stats & Economy")]
        [SerializeField] private int _maxHealth = 100;
        [SerializeField] private int _aetheriumCost = 50;

        public string Id => _id;
        public string DisplayName => _displayName;
        public float MoveSpeed => _moveSpeed;
        public float Radius => _radius;
        public int MaxHealth => _maxHealth;
        public int AetheriumCost => _aetheriumCost;

        public UnitDefinition ToSimDefinition(int definitionId)
        {
            return new UnitDefinition(
                definitionId: definitionId,
                stringId: _id,
                moveSpeed: _moveSpeed,
                radius: _radius,
                maxHealth: _maxHealth,
                aetheriumCost: _aetheriumCost
            );
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                _id = name;
            }
        }
    }
}
