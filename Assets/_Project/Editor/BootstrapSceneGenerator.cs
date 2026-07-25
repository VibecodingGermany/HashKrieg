using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nova.Editor
{
    /// <summary>
    /// One-shot generator for the minimal Bootstrap scene (G0-B platform
    /// baseline). Run once via:
    ///   Unity -batchmode -projectPath <repo> \
    ///     -executeMethod Nova.Editor.BootstrapSceneGenerator.CreateBootstrapScene -quit
    /// The scene is saved to Assets/_Project/Scenes/Bootstrap.unity and
    /// registered as the only enabled EditorBuildSettings scene.
    /// </summary>
    public static class BootstrapSceneGenerator
    {
        public const string ScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        [MenuItem("Tools/Project Nova/Create Bootstrap Scene")]
        public static void CreateBootstrapScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
            }

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(64f, 60f, -20f);
            camera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

            var lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);
            scenes.RemoveAll(entry => entry.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log($"Bootstrap scene created at {ScenePath} and registered " +
                      "in EditorBuildSettings.");
        }
    }
}
