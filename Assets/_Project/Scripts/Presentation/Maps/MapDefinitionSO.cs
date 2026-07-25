using System.Collections.Generic;
using UnityEngine;

namespace Nova.Presentation.Maps
{
    /// <summary>
    /// ScriptableObject defining map layout, spawn points, resource locations, and biome profile.
    /// Supports 1v1 and 2v2 maps across 3 biomes.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMapDefinition", menuName = "Project Nova/Maps/Map Definition")]
    public class MapDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _mapName = "Untitled Map";
        [SerializeField] private MapBiomeType _biome = MapBiomeType.Desert;
        [SerializeField] private ushort _width = 128;
        [SerializeField] private ushort _height = 128;
        [SerializeField] private List<Vector2> _spawnPoints = new List<Vector2>();
        [SerializeField] private List<Vector2> _resourceNodes = new List<Vector2>();

        public string MapName => _mapName;
        public MapBiomeType Biome => _biome;
        public ushort Width => _width;
        public ushort Height => _height;
        public IReadOnlyList<Vector2> SpawnPoints => _spawnPoints;
        public IReadOnlyList<Vector2> ResourceNodes => _resourceNodes;

        public void Initialize(string mapName, MapBiomeType biome, ushort width, ushort height, Vector2[] spawnPoints, Vector2[] resourceNodes)
        {
            _mapName = mapName;
            _biome = biome;
            _width = width;
            _height = height;

            _spawnPoints.Clear();
            if (spawnPoints != null) _spawnPoints.AddRange(spawnPoints);

            _resourceNodes.Clear();
            if (resourceNodes != null) _resourceNodes.AddRange(resourceNodes);
        }

        public bool IsValid()
        {
            if (_width == 0 || _height == 0) return false;
            if (_spawnPoints.Count < 2) return false; // Must support at least 1v1 (2 players)

            for (int i = 0; i < _spawnPoints.Count; i++)
            {
                Vector2 sp = _spawnPoints[i];
                if (sp.x < 0f || sp.x >= _width || sp.y < 0f || sp.y >= _height) return false;
            }

            return true;
        }
    }
}
