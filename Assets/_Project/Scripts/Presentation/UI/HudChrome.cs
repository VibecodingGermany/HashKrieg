// Graybox throwaway. Legacy Input + OnGUI. Does NOT satisfy G2/G4 UI criteria
// (see docs/production/MVPRecoveryPlan.md). Replaced when the new Input System and the real UI land.
using UnityEngine;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The shared HUD chrome of the graybox cockpit: ONE generated panel
    /// texture (dark translucent fill, subtle light border ring) plus a 1x1
    /// white pixel, so the status bar, the build bar, the command card and
    /// the minimap all sit on the same restrained frame instead of raw debug
    /// text — one consistent style, no per-panel experiments (this is not an
    /// art pass). The F3 debug panel deliberately keeps its plain
    /// <c>GUI.skin.box</c>: it is a debug view, not cockpit chrome.
    /// <para>
    /// Same generation convention as <see cref="GroundMarkerVisuals"/>:
    /// everything is built at runtime, marked HideAndDontSave and rebuilt
    /// lazily by the getters after a teardown, and no texture or style asset
    /// ever lands on disk. <see cref="DestroyShared"/> is called from the
    /// consuming components' OnDestroy, mirroring the marker layer.
    /// </para>
    /// </summary>
    internal static class HudChrome
    {
        private const int PanelTextureSize = 32;
        private const int PanelBorderPixels = 2;

        private static readonly Color PanelFill = new Color(0.05f, 0.06f, 0.08f, 0.82f);
        private static readonly Color PanelEdge = new Color(0.55f, 0.62f, 0.72f, 0.50f);
        private static readonly Color OpaquePanelFill = new Color(0.05f, 0.06f, 0.08f, 1f);

        private static Texture2D _pixel;
        private static Texture2D _panelTexture;
        private static Texture2D _opaquePanelTexture;
        private static GUIStyle _panelStyle;
        private static GUIStyle _opaquePanelStyle;

        /// <summary>Shared 1x1 white texture for tinted solid fills (progress bars, minimap dots, viewport lines).</summary>
        public static Texture2D Pixel
        {
            get
            {
                if (_pixel == null)
                {
                    _pixel = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    _pixel.SetPixel(0, 0, Color.white);
                    _pixel.Apply();
                }
                return _pixel;
            }
        }

        /// <summary>The chrome panel texture: dark translucent fill with a 2px lighter border ring.</summary>
        public static Texture2D PanelTexture
        {
            get
            {
                if (_panelTexture == null) _panelTexture = CreatePanelTexture();
                return _panelTexture;
            }
        }

        /// <summary>
        /// Box-style chrome for area backgrounds (command card, build bar,
        /// minimap frame). The border slices the 2px ring so the edge stays
        /// crisp at any panel size; the padding keeps content off the edge.
        /// </summary>
        public static GUIStyle PanelStyle
        {
            get
            {
                if (_panelStyle == null)
                {
                    _panelStyle = new GUIStyle
                    {
                        normal = { background = PanelTexture },
                        border = new RectOffset(PanelBorderPixels, PanelBorderPixels, PanelBorderPixels, PanelBorderPixels),
                        padding = new RectOffset(6, 6, 5, 5)
                    };
                }
                return _panelStyle;
            }
        }

        /// <summary>
        /// The fully opaque twin of <see cref="PanelStyle"/> (D-085): same
        /// fill tone and border ring, alpha 1.0. For the F3 debug panel —
        /// world geometry bleeding through dense debug text was what made it
        /// unreadable over the minimap and the build bar.
        /// </summary>
        public static GUIStyle OpaquePanelStyle
        {
            get
            {
                if (_opaquePanelStyle == null)
                {
                    if (_opaquePanelTexture == null) _opaquePanelTexture = CreatePanelTexture(OpaquePanelFill);
                    _opaquePanelStyle = new GUIStyle
                    {
                        normal = { background = _opaquePanelTexture },
                        border = new RectOffset(PanelBorderPixels, PanelBorderPixels, PanelBorderPixels, PanelBorderPixels),
                        padding = new RectOffset(6, 6, 5, 5)
                    };
                }
                return _opaquePanelStyle;
            }
        }

        /// <summary>
        /// Applies the same chrome to an existing text style (the status
        /// bar's label), so a one-line readout gets the identical frame
        /// without becoming a box.
        /// </summary>
        public static void ApplyPanelChrome(GUIStyle style)
        {
            style.normal.background = PanelTexture;
            style.border = new RectOffset(PanelBorderPixels, PanelBorderPixels, PanelBorderPixels, PanelBorderPixels);
            style.padding = new RectOffset(8, 8, 4, 4);
        }

        /// <summary>Destroys the shared textures/style; the next access lazily recreates them.</summary>
        public static void DestroyShared()
        {
            if (_pixel != null)
            {
                Object.Destroy(_pixel);
                _pixel = null;
            }
            if (_panelTexture != null)
            {
                Object.Destroy(_panelTexture);
                _panelTexture = null;
            }
            if (_opaquePanelTexture != null)
            {
                Object.Destroy(_opaquePanelTexture);
                _opaquePanelTexture = null;
            }
            _panelStyle = null;
            _opaquePanelStyle = null;
        }

        private static Texture2D CreatePanelTexture()
        {
            return CreatePanelTexture(PanelFill);
        }

        private static Texture2D CreatePanelTexture(Color fill)
        {
            var texture = new Texture2D(PanelTextureSize, PanelTextureSize, TextureFormat.RGBA32, mipChain: false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < PanelTextureSize; y++)
            {
                for (int x = 0; x < PanelTextureSize; x++)
                {
                    bool isBorder = x < PanelBorderPixels || y < PanelBorderPixels
                        || x >= PanelTextureSize - PanelBorderPixels || y >= PanelTextureSize - PanelBorderPixels;
                    texture.SetPixel(x, y, isBorder ? PanelEdge : fill);
                }
            }
            texture.Apply();
            return texture;
        }
    }
}
