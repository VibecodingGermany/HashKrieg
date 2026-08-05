using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Nova.Data;

namespace Nova.Editor
{
    /// <summary>
    /// Drop-in pipeline for art assets (docs/assets/ArtAssetStandard.md):
    /// <para>
    /// 1) Registry sync — every <c>PF_*</c> prefab under
    /// <see cref="ArtAssetNaming.ArtRoot"/> is parsed by
    /// <see cref="ArtAssetNaming.TryParsePrefabName"/> and registered in the
    /// shared <see cref="AssetMappingRegistrySO"/> under its simulation
    /// definition id (Alliance = role wire value, Legion = role + 17). The
    /// registry is rebuilt from a full scan on every sync, so dropped,
    /// renamed or deleted prefabs always converge to the on-disk truth. Runs
    /// automatically after every asset import and via the menu item.
    /// </para>
    /// <para>
    /// 2) Import presets — FBX and texture imports under Art/ get the
    /// standard's section-4 settings applied in the preprocessor (scale 1.0,
    /// no FBX materials, no read/write, no mesh compression; sRGB/normal/
    /// linear-mask rules, 1024 unit / 2048 building cap, BC7 on Standalone).
    /// Per-asset deviations stay possible and are justified in the PR, per
    /// the standard.
    /// </para>
    /// </summary>
    public static class ArtAssetAutoSync
    {
        /// <summary>The single shared registry instance the scene generator wires into the UnitViewManager.</summary>
        public const string RegistryPath = "Assets/_Project/Data/Registries/AssetMappingRegistry.asset";

        [MenuItem("Tools/Project Nova/Sync Art Asset Registry")]
        public static void SyncRegistryMenu()
        {
            int count = SyncRegistry();
            Debug.Log($"[ArtAssetAutoSync] registry sync complete: {count} convention-conformant prefab(s) registered.");
        }

        /// <summary>
        /// Rebuilds the registry from every prefab under Art/. Returns the
        /// number of registered prefabs. Non-conformant names are skipped with
        /// a warning naming the file, never silently dropped.
        /// </summary>
        public static int SyncRegistry()
        {
            AssetMappingRegistrySO registry = LoadOrCreateRegistry();

            var units = new List<UnitAssetMapping>();
            var buildings = new List<BuildingAssetMapping>();

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { ArtAssetNaming.ArtRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string stem = Path.GetFileNameWithoutExtension(path);

                if (!stem.StartsWith("PF_", System.StringComparison.Ordinal))
                {
                    continue; // LOD helper prefabs etc. are not convention targets
                }

                if (!ArtAssetNaming.TryParsePrefabDefinitionId(stem, out int definitionId, out bool isBuilding))
                {
                    Debug.LogWarning($"[ArtAssetAutoSync] '{path}' violates the PF_ naming convention (ArtAssetStandard.md §2) and was NOT registered.");
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                if (isBuilding)
                {
                    buildings.Add(new BuildingAssetMapping { DefinitionId = definitionId, ModelPrefab = prefab });
                }
                else
                {
                    units.Add(new UnitAssetMapping { DefinitionId = definitionId, ModelPrefab = prefab });
                }
            }

            if (!ContentEquals(registry, units, buildings))
            {
                registry.ClearMappings();
                foreach (UnitAssetMapping mapping in units)
                {
                    registry.RegisterUnitMapping(mapping.DefinitionId, mapping.ModelPrefab);
                }
                foreach (BuildingAssetMapping mapping in buildings)
                {
                    registry.RegisterBuildingMapping(mapping.DefinitionId, mapping.ModelPrefab);
                }
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();
            }

            return units.Count + buildings.Count;
        }

        /// <summary>Loads the shared registry, creating it (and its folders) on first use.</summary>
        public static AssetMappingRegistrySO LoadOrCreateRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<AssetMappingRegistrySO>(RegistryPath);
            if (registry != null) return registry;

            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Registries");
            registry = ScriptableObject.CreateInstance<AssetMappingRegistrySO>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
            AssetDatabase.SaveAssets();
            return registry;
        }

        /// <summary>Creates an Assets-relative folder (and its parents) if missing.</summary>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static bool ContentEquals(
            AssetMappingRegistrySO registry,
            List<UnitAssetMapping> units,
            List<BuildingAssetMapping> buildings)
        {
            if (registry.UnitMappings.Count != units.Count) return false;
            if (registry.BuildingMappings.Count != buildings.Count) return false;

            for (int i = 0; i < units.Count; i++)
            {
                if (registry.UnitMappings[i].DefinitionId != units[i].DefinitionId
                    || registry.UnitMappings[i].ModelPrefab != units[i].ModelPrefab)
                {
                    return false;
                }
            }
            for (int i = 0; i < buildings.Count; i++)
            {
                if (registry.BuildingMappings[i].DefinitionId != buildings[i].DefinitionId
                    || registry.BuildingMappings[i].ModelPrefab != buildings[i].ModelPrefab)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Import hook: keeps the registry in sync after every import that
        /// touches Art/, and stamps the standard's import settings onto models
        /// and textures before Unity imports them.
        /// </summary>
        private sealed class Postprocessor : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(
                string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                foreach (string path in importedAssets)
                {
                    if (path.StartsWith(ArtAssetNaming.ArtRoot + "/", System.StringComparison.Ordinal))
                    {
                        SyncRegistry();
                        return;
                    }
                }
                foreach (string path in deletedAssets)
                {
                    if (path.StartsWith(ArtAssetNaming.ArtRoot + "/", System.StringComparison.Ordinal))
                    {
                        SyncRegistry();
                        return;
                    }
                }
            }

            // docs/assets/ArtAssetStandard.md §4.1 — model import preset.
            private void OnPreprocessModel()
            {
                if (!assetPath.StartsWith(ArtAssetNaming.ArtRoot + "/", System.StringComparison.Ordinal)) return;

                var importer = (ModelImporter)assetImporter;
                importer.globalScale = 1f;
                importer.useFileUnits = true;                 // Convert Units
                importer.isReadable = false;                  // Read/Write off
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.optimizeMeshPolygons = true;
                importer.optimizeMeshVertices = true;
                importer.addCollider = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.None; // materials live in the repo
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.CalculateMikk; // Mikktspace
                importer.weldVertices = true;
                importer.importAnimation = false; // animated assets opt in per asset
            }

            // docs/assets/ArtAssetStandard.md §4.2 — texture import preset.
            private void OnPreprocessTexture()
            {
                if (!assetPath.StartsWith(ArtAssetNaming.ArtRoot + "/", System.StringComparison.Ordinal)) return;

                var importer = (TextureImporter)assetImporter;
                string stem = Path.GetFileNameWithoutExtension(assetPath);
                int maxSize = assetPath.Contains("/Units/") ? 1024 : 2048;

                importer.maxTextureSize = maxSize;
                importer.mipmapEnabled = true;

                if (stem.EndsWith("_N", System.StringComparison.Ordinal))
                {
                    importer.textureType = TextureImporterType.NormalMap;
                }
                else if (stem.EndsWith("_MSK", System.StringComparison.Ordinal))
                {
                    // Linear data texture; every channel is payload (R metallic,
                    // G occlusion, B team mask, A smoothness — §5.1).
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = false;
                    importer.alphaSource = TextureImporterAlphaSource.FromInput;
                    importer.alphaIsTransparency = false;
                }
                else
                {
                    importer.sRGBTexture = true; // BaseColor
                }

                var standalone = new TextureImporterPlatformSettings
                {
                    name = "Standalone",
                    overridden = true,
                    maxTextureSize = maxSize,
                    format = TextureImporterFormat.BC7,
                    textureCompression = TextureImporterCompression.CompressedHQ,
                };
                importer.SetPlatformTextureSettings(standalone);
            }
        }
    }
}
