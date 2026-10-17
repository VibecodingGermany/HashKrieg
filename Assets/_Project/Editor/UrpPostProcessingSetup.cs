using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Nova.Editor
{
    /// <summary>
    /// Post-processing for the whole project: the glow on emissive light
    /// strips (the _EM maps of tools/art/asset_pass.py) needs URP bloom, and
    /// bloom needed three things the project never had.
    /// <list type="number">
    /// <item><b>Renderer post-process data.</b> <see cref="UrpProjectSetup"/>
    /// created <c>NovaUrpRenderer</c> by <c>CreateInstance</c>, which leaves
    /// <c>postProcessData</c> null — URP then silently renders every camera
    /// without post-processing, whatever volumes exist.</item>
    /// <item><b>A volume profile.</b> Assigned as the pipeline asset's
    /// <c>volumeProfile</c>, it is the default stack of every camera, so no
    /// scene needs a Volume object and preview cameras get it too.</item>
    /// <item><b>Per-camera switch.</b> URP cameras render post-processing only
    /// with <c>renderPostProcessing</c> on; <see cref="EnablePostProcessing"/>
    /// sets it on the serialized camera data.</item>
    /// </list>
    /// <para>
    /// Reflection and serialized properties only, like UrpProjectSetup: the
    /// asmdef contract (D-061, quality/scripts/run_gate_check.py) allows no URP
    /// assembly reference from Nova.Editor. Idempotent: re-running updates the
    /// profile in place and keeps its GUID.
    /// </para>
    /// </summary>
    public static class UrpPostProcessingSetup
    {
        private const string RendererPath = "Assets/_Project/Settings/NovaUrpRenderer.asset";
        private const string PipelinePath = "Assets/_Project/Settings/NovaUrp.asset";
        public const string ProfilePath = "Assets/_Project/Settings/NovaPostProcessProfile.asset";
        private const string PostProcessDataPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";

        // Bloom only catches HDR values above the threshold: the emissive
        // accents (EmissionIntensity in ArtAssetPrefabBuilder) and the sun on
        // the brightest steel, not the desert floor.
        private const float BloomThreshold = 1.0f;
        private const float BloomIntensity = 1.0f;
        private const float BloomScatter = 0.65f;

        [MenuItem("Tools/Project Nova/Setup URP Post-Processing (Bloom)")]
        public static void Apply()
        {
            var renderer = AssetDatabase.LoadMainAssetAtPath(RendererPath);
            var pipeline = AssetDatabase.LoadMainAssetAtPath(PipelinePath);
            var postProcessData = AssetDatabase.LoadMainAssetAtPath(PostProcessDataPath);
            if (renderer == null || pipeline == null)
            {
                Debug.LogError("[UrpPostProcessingSetup] URP assets missing — run Tools/Project Nova/Setup URP Pipeline first.");
                return;
            }
            if (postProcessData == null)
            {
                Debug.LogError($"[UrpPostProcessingSetup] '{PostProcessDataPath}' not found — URP package layout changed?");
                return;
            }

            SetReference(renderer, "postProcessData", postProcessData);
            ScriptableObject profile = LoadOrCreateProfile();
            if (profile == null) return;
            ConfigureBloom(profile);
            SetReference(pipeline, "m_VolumeProfile", profile);

            AssetDatabase.SaveAssets();
            Debug.Log("[UrpPostProcessingSetup] post-process data, bloom profile and pipeline default volume set.");
        }

        /// <summary>Turns URP post-processing on for one camera (adds its URP camera data if missing).</summary>
        public static void EnablePostProcessing(Camera camera)
        {
            Type dataType = FindType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            if (camera == null || dataType == null) return;
            Component data = camera.GetComponent(dataType) ?? camera.gameObject.AddComponent(dataType);
            var so = new SerializedObject(data);
            SerializedProperty flag = so.FindProperty("m_RenderPostProcessing");
            if (flag == null)
            {
                Debug.LogError("[UrpPostProcessingSetup] m_RenderPostProcessing not found — URP camera data changed?");
                return;
            }
            flag.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ScriptableObject LoadOrCreateProfile()
        {
            Type profileType = FindType("UnityEngine.Rendering.VolumeProfile");
            if (profileType == null)
            {
                Debug.LogError("[UrpPostProcessingSetup] VolumeProfile type not found.");
                return null;
            }
            var profile = AssetDatabase.LoadAssetAtPath(ProfilePath, profileType) as ScriptableObject;
            if (profile != null) return profile;

            profile = ScriptableObject.CreateInstance(profileType);
            AssetDatabase.CreateAsset(profile, ProfilePath);
            return profile;
        }

        private static void ConfigureBloom(ScriptableObject profile)
        {
            Type bloomType = FindType("UnityEngine.Rendering.Universal.Bloom");
            if (bloomType == null)
            {
                Debug.LogError("[UrpPostProcessingSetup] URP Bloom type not found.");
                return;
            }

            // VolumeProfile.TryGet/Add are generic or overloaded; the component
            // list is the stable surface.
            var components = (System.Collections.IList)profile.GetType()
                .GetField("components", BindingFlags.Public | BindingFlags.Instance).GetValue(profile);
            ScriptableObject bloom = null;
            foreach (object c in components)
            {
                if (c != null && c.GetType() == bloomType) bloom = (ScriptableObject)c;
            }
            if (bloom == null)
            {
                bloom = ScriptableObject.CreateInstance(bloomType);
                bloom.name = "Bloom";
                bloom.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                components.Add(bloom);
                AssetDatabase.AddObjectToAsset(bloom, profile);
            }

            Override(bloom, "threshold", BloomThreshold);
            Override(bloom, "intensity", BloomIntensity);
            Override(bloom, "scatter", BloomScatter);
            Override(bloom, "highQualityFiltering", true);
            EditorUtility.SetDirty(bloom);
            EditorUtility.SetDirty(profile);
        }

        /// <summary>VolumeParameter&lt;T&gt;.Override(value): sets the value and its override flag.</summary>
        private static void Override(object component, string field, object value)
        {
            object parameter = component.GetType().GetField(field, BindingFlags.Public | BindingFlags.Instance)?.GetValue(component);
            MethodInfo method = parameter?.GetType().GetMethod("Override", new[] { value.GetType() });
            if (method == null)
            {
                Debug.LogError($"[UrpPostProcessingSetup] cannot override '{field}' on {component.GetType().Name}.");
                return;
            }
            method.Invoke(parameter, new[] { value });
        }

        private static void SetReference(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(property);
            if (p == null)
            {
                Debug.LogError($"[UrpPostProcessingSetup] '{property}' not found on {target.name}.");
                return;
            }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null) return type;
            }
            return null;
        }
    }
}
