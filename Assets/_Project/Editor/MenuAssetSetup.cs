using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nova.Editor
{
    /// <summary>
    /// Creates the two UI Toolkit assets the main menu needs and that this
    /// project never had — a theme style sheet and a PanelSettings — and
    /// resolves the menu's content assets by path. Called from
    /// <see cref="BootstrapSceneGenerator"/>; there is no menu item, because
    /// these assets only ever exist for the generated scene.
    /// <para>
    /// ASSETS ON DISK, NOT ScriptableObject.CreateInstance AT RUNTIME. A
    /// PanelSettings built in the player carries no ICU text data, and Unity's
    /// built-in default runtime theme is loaded through EditorGUIUtility —
    /// it does not exist in a build. A runtime-built panel would therefore
    /// ship without fonts and without control styles, and the failure would be
    /// invisible in the Editor. Same shape as
    /// <see cref="UrpProjectSetup"/> and <c>ArtAssetAutoSync.LoadOrCreateRegistry</c>.
    /// </para>
    /// </summary>
    public static class MenuAssetSetup
    {
        public const string UiFolder = "Assets/_Project/UI";
        public const string ThemeFolder = UiFolder + "/Themes";
        public const string ThemePath = ThemeFolder + "/HashkriegRuntimeTheme.tss";
        public const string PanelSettingsPath = UiFolder + "/HashkriegPanelSettings.asset";

        public const string KeyArtPath = UiFolder + "/UI_KeyArt_MainMenu.jpg";
        public const string TitleFontPath = UiFolder + "/Fonts/Rajdhani-Bold.ttf";
        public const string BodyFontPath = UiFolder + "/Fonts/Rajdhani-Regular.ttf";
        public const string MusicClipPath = "Assets/_Project/Audio/Music/MUS_MainMenu_Hashkrieg.ogg";

        /// <summary>
        /// The menu is authored for 1920x1080 and scales from there. The three
        /// values below are all wrong on a code-created PanelSettings, because
        /// <c>Reset()</c> — which carries Unity's inspector defaults — does not
        /// run for <c>ScriptableObject.CreateInstance</c>: the field
        /// initialisers give ConstantPhysicalSize at 1200x800 with match 0.
        /// </summary>
        private static readonly Vector2Int ReferenceResolution = new Vector2Int(1920, 1080);

        /// <summary>
        /// Exactly what Unity's own PanelSettings creator writes. The
        /// "unity-theme" scheme is resolved by the built-in .tss importer and
        /// pulls in the default control styles — including the font and the
        /// <c>.unity-ui-document__root</c> full-screen rule the UIDocument root
        /// depends on.
        /// </summary>
        private const string ThemeSource = "@import url(\"unity-theme://default\");\n";

        /// <summary>
        /// The panel the menu's UIDocument renders into. Screen-space overlay
        /// (the PanelSettings default), so it draws on top of the 3D scene
        /// without a camera and — deliberately — is NOT captured by the
        /// <c>camera.Render()</c> screenshots the PlayMode proof tests take.
        /// </summary>
        public static PanelSettings LoadOrCreatePanelSettings()
        {
            PanelSettings panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panel == null)
            {
                ArtAssetAutoSync.EnsureFolder(UiFolder);
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, PanelSettingsPath);
            }

            panel.themeStyleSheet = LoadOrCreateTheme();
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = ReferenceResolution;
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            // Blend width against height rather than committing to one: the
            // key art is 16:9, and a pure width match would push the menu off
            // the bottom of an ultrawide window.
            panel.match = 0.5f;
            // Only orders panels against each other (an overlay panel is always
            // above the scene). Leaves room for HUD panels below the menu once
            // the OnGUI cockpit is replaced.
            panel.sortingOrder = 100f;
            // The 3D scene stays visible behind the menu; the menu paints its
            // own background over it.
            panel.clearColor = false;

            EditorUtility.SetDirty(panel);
            return panel;
        }

        /// <summary>
        /// Loads a menu asset and says which file is missing instead of
        /// leaving a null reference behind. A menu that boots black with no
        /// console output is the exact failure mode this sprint exists to
        /// remove.
        /// </summary>
        public static T LoadRequired<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError(
                    $"[MenuAssetSetup] Missing {typeof(T).Name} at {path} — the main menu will be wired " +
                    "incompletely and MainMenuController will say so again at Play.");
            }
            return asset;
        }

        private static ThemeStyleSheet LoadOrCreateTheme()
        {
            ThemeStyleSheet theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme != null) return theme;

            // Written as text and imported: .tss is bound to a built-in
            // ScriptedImporter, so there is no .meta handwork and no internal
            // API involved (PanelSettingsCreator and ThemeUtility are both
            // internal, and the built-in theme they hand out is Editor-only).
            ArtAssetAutoSync.EnsureFolder(ThemeFolder);
            File.WriteAllText(ThemePath, ThemeSource);
            AssetDatabase.ImportAsset(ThemePath, ImportAssetOptions.ForceSynchronousImport);

            theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme == null)
            {
                Debug.LogError(
                    $"[MenuAssetSetup] {ThemePath} did not import as a ThemeStyleSheet — the menu would " +
                    "render without fonts and without control styles.");
            }
            return theme;
        }
    }
}
