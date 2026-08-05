using UnityEditor;
using UnityEditor.SceneManagement;

namespace Nova.Editor
{
    /// <summary>
    /// Starts the graybox demo: opens the generated Bootstrap scene and
    /// enters Play mode. Used by the menu item and by the graphical
    /// editor launch
    ///   Unity -projectPath &lt;repo&gt; -executeMethod Nova.Editor.DemoLauncher.PlayDemo
    /// (no -batchmode, no -quit — the editor stays open for the human).
    /// The delayCall defers past startup compilation, so the scene opens
    /// against a settled editor before Play begins.
    /// </summary>
    public static class DemoLauncher
    {
        [MenuItem("Tools/Project Nova/Play Demo (Bootstrap Scene)")]
        public static void PlayDemo()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                EditorSceneManager.OpenScene(BootstrapSceneGenerator.ScenePath);
                EditorApplication.EnterPlaymode();
            };
        }
    }
}
