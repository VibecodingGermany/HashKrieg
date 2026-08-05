using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Nova.Data;

namespace Nova.Editor
{
    /// <summary>
    /// Builds the <c>M_*</c> materials and <c>PF_*</c> prefabs that
    /// <see cref="ArtAssetAutoSync"/> needs, from the <c>SM_*.fbx</c> meshes
    /// dropped under <c>Assets/_Project/Art/</c>.
    /// <para>
    /// The import side of the pipeline (ArtAssetAutoSync) deliberately turns
    /// FBX material import off (ArtAssetStandard.md section 4.1), so a raw FBX
    /// drop has no material and no prefab and therefore never reaches the
    /// registry. This builder closes that gap: for every mesh it creates a URP
    /// Lit material bound to the sibling <c>T_*_BC</c> texture and a prefab
    /// whose <see cref="LODGroup"/> is wired to the <c>_LOD0/_LOD1/_LOD2</c>
    /// children at the standard's screen-height thresholds (section 3).
    /// </para>
    /// <para>
    /// Idempotent: existing materials and prefabs are updated in place, so a
    /// re-run after replacing a mesh keeps GUIDs and every scene reference.
    /// </para>
    /// </summary>
    public static class ArtAssetPrefabBuilder
    {
        /// <summary>LOD0 renders above this share of screen height (ArtAssetStandard.md section 3).</summary>
        private const float Lod0ScreenHeight = 0.08f;

        /// <summary>LOD1 renders down to this share; below it LOD2 takes over and stops casting shadows.</summary>
        private const float Lod1ScreenHeight = 0.02f;

        private const string UrpLitShader = "Universal Render Pipeline/Lit";

        [MenuItem("Tools/Project Nova/Build Art Prefabs From FBX")]
        public static void BuildMenu()
        {
            int built = Build();
            ArtAssetAutoSync.SyncRegistry();
            Debug.Log($"[ArtAssetPrefabBuilder] {built} prefab(s) built or updated, registry re-synced.");
        }

        /// <summary>
        /// Scans every <c>SM_*.fbx</c> under the art root and creates or
        /// updates its material and prefab. Returns the number of prefabs
        /// written. Meshes whose file name violates the convention are skipped
        /// with a warning that names the file, never silently.
        /// </summary>
        public static int Build()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ArtAssetNaming.ArtRoot });
            int built = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (string guid in guids)
                {
                    string fbxPath = AssetDatabase.GUIDToAssetPath(guid);
                    string stem = Path.GetFileNameWithoutExtension(fbxPath);
                    if (!stem.StartsWith("SM_", System.StringComparison.Ordinal)) continue;

                    // SM_UNIT_Alliance_LightTank -> PF_UNIT_Alliance_LightTank
                    string suffix = stem.Substring("SM_".Length);
                    string prefabName = "PF_" + suffix;
                    if (!ArtAssetNaming.TryParsePrefabDefinitionId(prefabName, out _, out _))
                    {
                        Debug.LogWarning($"[ArtAssetPrefabBuilder] '{fbxPath}' does not match the SM_ naming convention (ArtAssetStandard.md section 2) and was skipped.");
                        continue;
                    }

                    string dir = Path.GetDirectoryName(fbxPath).Replace('\\', '/');
                    Material material = CreateOrUpdateMaterial(dir, suffix);
                    if (BuildPrefab(fbxPath, dir, prefabName, material)) built++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return built;
        }

        /// <summary>
        /// Creates or updates <c>M_&lt;suffix&gt;.mat</c> next to the mesh and
        /// binds the sibling <c>T_&lt;suffix&gt;_BC</c> texture as base map.
        /// A missing texture is a warning, not an error — the material stays
        /// usable and the mesh still reaches the scene.
        /// </summary>
        private static Material CreateOrUpdateMaterial(string dir, string suffix)
        {
            string matPath = $"{dir}/M_{suffix}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            if (material == null)
            {
                Shader shader = Shader.Find(UrpLitShader);
                if (shader == null)
                {
                    Debug.LogError($"[ArtAssetPrefabBuilder] shader '{UrpLitShader}' not found — is the URP package present?");
                    return null;
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, matPath);
            }

            string texPath = $"{dir}/T_{suffix}_BC.png";
            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (baseMap != null)
            {
                material.SetTexture("_BaseMap", baseMap);
            }
            else
            {
                Debug.LogWarning($"[ArtAssetPrefabBuilder] no base-colour texture at '{texPath}'; material '{matPath}' stays untextured.");
            }

            // The Tripo-sourced meshes ship base colour only. Until the _MSK
            // mask of ArtAssetStandard.md section 5.1 is authored, keep the
            // surface fully dielectric and mid-rough rather than inheriting
            // URP's shiny default, which reads as wet plastic under the RTS sun.
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Creates or updates the view prefab: a root carrying a
        /// <see cref="LODGroup"/> over the mesh's <c>_LOD0/1/2</c> children,
        /// with LOD2 shadow casting off per ArtAssetStandard.md section 3.
        /// Meshes that ship fewer than three LODs still yield a prefab — the
        /// LODGroup then spans whatever levels exist, so a partial drop is
        /// visible instead of silently missing.
        /// </summary>
        private static bool BuildPrefab(string fbxPath, string dir, string prefabName, Material material)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (model == null) return false;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            if (instance == null) return false;

            try
            {
                instance.name = prefabName;
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                var levels = new List<LOD>();
                var thresholds = new[] { Lod0ScreenHeight, Lod1ScreenHeight, 0.0f };

                for (int lod = 0; lod < 3; lod++)
                {
                    Transform child = FindChildWithSuffix(instance.transform, $"_LOD{lod}");
                    if (child == null) continue;

                    var renderer = child.GetComponent<Renderer>();
                    if (renderer == null) continue;

                    if (material != null) renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = lod == 2 ? ShadowCastingMode.Off : ShadowCastingMode.On;

                    levels.Add(new LOD(thresholds[lod], new[] { renderer }));
                }

                if (levels.Count == 0)
                {
                    Debug.LogWarning($"[ArtAssetPrefabBuilder] '{fbxPath}' has no _LOD0/_LOD1/_LOD2 children; no prefab written.");
                    return false;
                }

                // A single-level group would cull the mesh below its threshold;
                // the last level always reaches zero screen height.
                LOD last = levels[levels.Count - 1];
                last.screenRelativeTransitionHeight = 0f;
                levels[levels.Count - 1] = last;

                LODGroup group = instance.GetComponent<LODGroup>() ?? instance.AddComponent<LODGroup>();
                group.SetLODs(levels.ToArray());
                group.RecalculateBounds();

                PrefabUtility.SaveAsPrefabAsset(instance, $"{dir}/{prefabName}.prefab");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static Transform FindChildWithSuffix(Transform root, string suffix)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t != root && t.name.EndsWith(suffix, System.StringComparison.Ordinal)) return t;
            }

            return null;
        }
    }
}
