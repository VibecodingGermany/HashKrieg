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

        /// <summary>
        /// Name of the inactive child that carries the destroyed state
        /// (SM_*_Wreck.fbx, D-115); ViewWrecks swaps to it on death.
        /// </summary>
        public const string WreckNode = Nova.Gameplay.Match.ViewWrecks.WreckNode;

        private const string WreckSuffix = "_Wreck";

        /// <summary>HDR gain on the _EM map: accents must read under the full desert sun.</summary>
        private const float EmissionIntensity = 2.0f;

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
                    // Wreck meshes ride inside their parent's prefab, never alone.
                    if (stem.EndsWith(WreckSuffix, System.StringComparison.Ordinal)) continue;

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

            // _MSK (ArtAssetStandard.md section 5.1, written by
            // tools/art/asset_pass.py): R metallic and A smoothness drive URP
            // Lit's metallic map, G its occlusion map. Without one the surface
            // stays fully dielectric and mid-rough rather than inheriting
            // URP's shiny default, which reads as wet plastic under the RTS sun.
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/T_{suffix}_MSK.png");
            if (mask != null)
            {
                material.SetTexture("_MetallicGlossMap", mask);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.SetFloat("_Metallic", 1f);
                material.SetFloat("_Smoothness", 1f);   // multiplies mask.a
                material.SetTexture("_OcclusionMap", mask);
                material.EnableKeyword("_OCCLUSIONMAP");
                material.SetFloat("_OcclusionStrength", 1f);
            }
            else
            {
                material.SetTexture("_MetallicGlossMap", null);
                material.DisableKeyword("_METALLICSPECGLOSSMAP");
                material.SetTexture("_OcclusionMap", null);
                material.DisableKeyword("_OCCLUSIONMAP");
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Smoothness", 0.25f);
            }

            // _EM: the light strips and glow panels cut out by the art pass.
            var emission = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/T_{suffix}_EM.png");
            if (emission != null)
            {
                material.SetTexture("_EmissionMap", emission);
                material.SetColor("_EmissionColor", Color.white * EmissionIntensity);
                material.EnableKeyword("_EMISSION");
                // URP's material validation keeps _EMISSION only while the GI
                // flags say "emissive" (BaseShaderGUI.SetMaterialKeywords);
                // with None it silently strips the keyword on the next import.
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.SetTexture("_EmissionMap", null);
                material.SetColor("_EmissionColor", Color.black);
                material.DisableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }

            // Tripo meshes are ~1000 loose shells with real gaps between them.
            // With back faces culled the desert floor shows through every
            // gap as a bright crack; rendering both faces shows the dark
            // inside instead, which reads as a panel line.
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.doubleSidedGI = true;

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

                // A rigged mesh (tools/art/asset_pass.py) carries one renderer
                // per animated part and LOD — Hull, Turret, Gun, Wheel_* ... —
                // so a level collects EVERY renderer of its suffix, not the first.
                for (int lod = 0; lod < 3; lod++)
                {
                    var renderers = new List<Renderer>();
                    foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!renderer.name.EndsWith($"_LOD{lod}", System.StringComparison.Ordinal)) continue;
                        if (material != null) renderer.sharedMaterial = material;
                        renderer.shadowCastingMode = lod == 2 ? ShadowCastingMode.Off : ShadowCastingMode.On;
                        renderers.Add(renderer);
                    }
                    if (renderers.Count == 0) continue;

                    levels.Add(new LOD(thresholds[lod], renderers.ToArray()));
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

                AttachWreck(instance, fbxPath, dir, prefabName);

                PrefabUtility.SaveAsPrefabAsset(instance, $"{dir}/{prefabName}.prefab");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
        /// <summary>
        /// Adds the sibling <c>SM_*_Wreck.fbx</c> as an inactive child named
        /// <see cref="WreckNode"/> with its own charred material
        /// (<c>M_*_Wreck</c> on <c>T_*_Wreck_BC</c>). The wreck is not part of
        /// the LODGroup: it is a single mesh, shown alone once the live model
        /// is switched off. No wreck file — no child, the view keeps the
        /// fade-out death.
        /// </summary>
        private static void AttachWreck(GameObject instance, string fbxPath, string dir, string prefabName)
        {
            string wreckPath = fbxPath.Substring(0, fbxPath.Length - ".fbx".Length) + WreckSuffix + ".fbx";
            var wreckModel = AssetDatabase.LoadAssetAtPath<GameObject>(wreckPath);
            if (wreckModel == null) return;

            string suffix = prefabName.Substring("PF_".Length) + WreckSuffix;
            string matPath = $"{dir}/M_{suffix}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null)
            {
                material = new Material(Shader.Find(UrpLitShader));
                AssetDatabase.CreateAsset(material, matPath);
            }
            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/T_{suffix}_BC.png");
            material.SetTexture("_BaseMap", baseMap);
            material.SetFloat("_Metallic", 0.1f);
            material.SetFloat("_Smoothness", 0.12f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.doubleSidedGI = true;
            EditorUtility.SetDirty(material);

            var wreck = (GameObject)PrefabUtility.InstantiatePrefab(wreckModel);
            PrefabUtility.UnpackPrefabInstance(wreck, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            wreck.name = WreckNode;
            wreck.transform.SetParent(instance.transform, false);
            foreach (Renderer r in wreck.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterial = material;
                r.shadowCastingMode = ShadowCastingMode.On;
            }
            wreck.SetActive(false);
        }
    }
}
