using Nova.Gameplay.Match;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The always-on version badge (issue #103): one small, muted line in the
    /// bottom-left corner — "v0.21.0 · dev (Editor)" in the Editor,
    /// "v0.21.0 · e650864" in a packaged build — so a test report can never
    /// again be filed against the wrong build.
    /// <para>
    /// OWN UIDocument, NOT the match HUD and NOT the menu's tree. The IMGUI
    /// cockpit is switched off and on across the menu/match transition (issue
    /// #102), and MainMenuController clears and rebuilds its document root on
    /// every return to the menu — a label living in either layer would vanish
    /// with it. This component renders into its own document on its own
    /// GameObject, sorts that document above the menu's full-screen key art
    /// (<see cref="Awake"/>), and holds no reference to the match at all. It
    /// is simply always there.
    /// </para>
    /// <para>
    /// ONE SOURCE FOR THE VERSION, ONE STAMP FOR THE BUILD. The version is
    /// <c>Application.version</c> (ProjectSettings' bundleVersion — there is
    /// no VERSION file and no second source). The build id comes from
    /// the D-094 build stamp that BuildCommitStamp writes, which
    /// (tools/packaging/build-mac.sh, build-linux.sh) stamp with the short
    /// commit hash before the Unity call. A missing or empty stamp is the
    /// normal Editor/fresh-clone situation and reports "dev" — never an
    /// error, never a log line.
    /// </para>
    /// <para>
    /// THE STRING IS FINAL ONCE BUILT: no Update, no per-frame work, no
    /// callbacks. The label never picks (<see cref="PickingMode.Ignore"/>) —
    /// a click in the corner belongs to the minimap or the world behind it.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VersionBadge : MonoBehaviour
    {
        /// <summary>Resource name of the packaging stamp, without extension.</summary>
        /// <summary>Build id when no stamp exists (Editor, fresh clone, hand-made build).</summary>
        private const string FallbackBuildId = "dev";

        /// <summary>
        /// The menu's document keeps the default sortOrder 0; the badge must
        /// draw OVER its full-screen key art and scrim, not under them.
        /// </summary>
        private const int BadgeSortOrder = 1;

        [Header("Wiring (scene generator)")]
        [SerializeField] private UIDocument _document;
        [Tooltip("Rajdhani-Regular — the menu's body font, so the badge speaks the same visual language.")]
        [SerializeField] private Font _font;

        [Header("Layout")]
        [SerializeField] private int _fontSize = 12;
        [SerializeField] private float _marginLeft = 10f;
        [SerializeField] private float _marginBottom = 8f;
        [Tooltip("The menu's body colour, dimmed by _opacity — present, never loud.")]
        [SerializeField] private Color _textColor = new Color(0.88f, 0.91f, 0.95f, 1f);
        [SerializeField] private float _opacity = 0.55f;

        private void Awake()
        {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_document != null && _document.sortOrder < BadgeSortOrder)
            {
                _document.sortOrder = BadgeSortOrder;
            }
        }

        private void Start()
        {
            if (_document == null || _document.rootVisualElement == null)
            {
                Debug.LogError(
                    "[VersionBadge] No UIDocument (or no panel) — the version badge cannot render. " +
                    "Re-run Tools/Project Nova/Create Bootstrap Scene.");
                return;
            }

            var label = new Label(BuildDisplayText())
            {
                name = "version-badge",
                pickingMode = PickingMode.Ignore,
            };
            if (_font != null)
            {
                // Null falls back to the theme's default font; the generator's
                // LoadRequired has already named the missing asset at scene
                // generation time.
                label.style.unityFontDefinition = FontDefinition.FromFont(_font);
            }
            label.style.fontSize = _fontSize;
            label.style.color = _textColor;
            label.style.opacity = _opacity;
            label.style.position = Position.Absolute;
            label.style.left = _marginLeft;
            label.style.bottom = _marginBottom;
            _document.rootVisualElement.Add(label);
        }

        /// <summary>
        /// The one string the badge ever shows.
        /// <para>
        /// THE COMMIT COMES FROM <see cref="BuildInfo"/>, not from a stamp of
        /// this badge's own. That mechanism already exists (D-094): the
        /// <c>BuildCommitStamp</c> build hook writes the short commit before
        /// EVERY player build, so a build made from the Unity GUI carries it
        /// just as one made from the packaging scripts. A second stamp written
        /// by the scripts alone would be a second source that silently
        /// disagrees with the first exactly when someone builds by hand — and
        /// the whole point of this badge is that it never lies about which
        /// build is running.
        /// </para>
        /// <para>
        /// In the Editor <see cref="BuildInfo.Commit"/> reports its own
        /// sentinel, and the badge says so plainly rather than borrowing a
        /// build's identity: an Editor session is not a build.
        /// </para>
        /// </summary>
        private static string BuildDisplayText()
        {
            string commit = BuildInfo.Commit;
            return commit == BuildInfo.EditorCommit
                ? $"v{Application.version} · {FallbackBuildId} (Editor)"
                : $"v{Application.version} · {commit}";
        }
    }
}
