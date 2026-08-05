using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nova.Editor
{
    /// <summary>
    /// One-off project setup: creates the URP pipeline + renderer assets and
    /// assigns them project-wide (GraphicsSettings default plus every quality
    /// level). The project shipped URP global settings but never had a
    /// pipeline asset assigned, so everything rendered through the built-in
    /// pipeline — and URP Lit materials, the art standard's fallback shader,
    /// rendered as magenta the moment real art landed (GB-004 finding).
    /// The project contracts (AGENTS.md, tech/Rendering.md) mandate URP, so
    /// this closes the setup gap rather than inventing a new direction.
    /// <para>
    /// Reflection-only, deliberately: the asmdef reference contract (D-061,
    /// quality/scripts/run_gate_check.py) allows no URP assembly reference
    /// from Nova.Editor, so the URP types are resolved by name at runtime.
    /// </para>
    /// </summary>
    public static class UrpProjectSetup
    {
        private const string SettingsDir = "Assets/_Project/Settings";
        private const string RendererPath = SettingsDir + "/NovaUrpRenderer.asset";
        private const string PipelinePath = SettingsDir + "/NovaUrp.asset";

        [MenuItem("Tools/Project Nova/Setup URP Pipeline")]
        public static void Setup()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                Type assetType = FindType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset");
                Type rendererType = FindType("UnityEngine.Rendering.Universal.UniversalRendererData");
                if (assetType == null || rendererType == null)
                {
                    Debug.LogError("[UrpProjectSetup] URP types not found — is the URP package installed?");
                    return;
                }

                ArtAssetAutoSync.EnsureFolder(SettingsDir);

                var rendererData = ScriptableObject.CreateInstance(rendererType);
                AssetDatabase.CreateAsset(rendererData, RendererPath);

                MethodInfo create = assetType.GetMethod(
                    "Create", BindingFlags.Public | BindingFlags.Static, null, new[] { rendererType }, null);
                if (create == null)
                {
                    Debug.LogError("[UrpProjectSetup] UniversalRenderPipelineAsset.Create(RendererData) not found — URP API changed?");
                    return;
                }

                pipeline = (RenderPipelineAsset)create.Invoke(null, new object[] { rendererData });
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;

            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, applyExpensiveChanges: false);

            AssetDatabase.SaveAssets();
            Debug.Log($"[UrpProjectSetup] URP assigned ({PipelinePath}); the project now renders through URP on every quality level.");
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
