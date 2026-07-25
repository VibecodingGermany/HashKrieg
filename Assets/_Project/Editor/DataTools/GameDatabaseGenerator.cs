using System.IO;
using UnityEditor;
using UnityEngine;
using Nova.Data.Registries;
using Nova.Data.Units;

namespace Nova.Editor.DataTools
{
    /// <summary>
    /// Editor tool for automatic GameDatabase Master Index rebuilding and validation (D-049).
    /// </summary>
    public static class GameDatabaseGenerator
    {
        public const string RegistriesFolderPath = "Assets/_Project/Data/Registries";
        public const string MasterDatabasePath = "Assets/_Project/Data/Registries/GameDatabaseMaster.asset";
        public const string UnitRegistryPath = "Assets/_Project/Data/Registries/UnitRegistry.asset";

        [MenuItem("Tools/Project Nova/Rebuild Game Database Master Index")]
        public static void RebuildMasterIndex()
        {
            if (!AssetDatabase.IsValidFolder(RegistriesFolderPath))
            {
                Directory.CreateDirectory(RegistriesFolderPath);
                AssetDatabase.Refresh();
            }

            // 1. Find all UnitDefinitionSO assets in _Project/Data/
            string[] guids = AssetDatabase.FindAssets("t:UnitDefinitionSO", new[] { "Assets/_Project/Data" });
            var unitDefs = new System.Collections.Generic.List<UnitDefinitionSO>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var unit = AssetDatabase.LoadAssetAtPath<UnitDefinitionSO>(path);
                if (unit != null)
                {
                    unitDefs.Add(unit);
                }
            }

            // 2. Load or Create UnitRegistrySO
            var unitRegistry = AssetDatabase.LoadAssetAtPath<UnitRegistrySO>(UnitRegistryPath);
            if (unitRegistry == null)
            {
                unitRegistry = ScriptableObject.CreateInstance<UnitRegistrySO>();
                AssetDatabase.CreateAsset(unitRegistry, UnitRegistryPath);
            }
            unitRegistry.SetEntries(unitDefs);
            EditorUtility.SetDirty(unitRegistry);

            // 3. Load or Create GameDatabaseMasterSO
            var masterDB = AssetDatabase.LoadAssetAtPath<GameDatabaseMasterSO>(MasterDatabasePath);
            if (masterDB == null)
            {
                masterDB = ScriptableObject.CreateInstance<GameDatabaseMasterSO>();
                AssetDatabase.CreateAsset(masterDB, MasterDatabasePath);
            }

            masterDB.SetSubRegistries(unitRegistry, null, null);
            EditorUtility.SetDirty(masterDB);

            // 4. Validate Master Index
            if (!masterDB.ValidateMasterIndex(out string error))
            {
                Debug.LogError($"[GameDatabaseGenerator] Validation FAILED: {error}");
            }
            else
            {
                Debug.Log($"[GameDatabaseGenerator] Successfully rebuilt Master Index. Found {unitDefs.Count} unit definitions.");
            }

            AssetDatabase.SaveAssets();
        }
    }
}
