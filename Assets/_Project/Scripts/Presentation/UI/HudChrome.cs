// Graybox throwaway. Legacy Input + OnGUI. Does NOT satisfy G2/G4 UI criteria
// (see docs/production/MVPRecoveryPlan.md). Replaced when the new Input System and the real UI land.
using UnityEngine;
using Nova.Simulation.State;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The shared HUD chrome of the graybox cockpit: generated panel textures,
    /// faction-aware C&amp;C Generals <c>ControlBarScheme</c> colours (Alliance
    /// High-Tech Steel/Cyan vs. Legion Industrial Bronze/Amber), tactical corner
    /// brackets, button states and a 1x1 white pixel, so the status bar, the
    /// build bar, the command card and the minimap all sit on one cohesive
    /// military command frame.
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

        // Alliance scheme (High-Tech Steel & Tactical Cyan)
        private static readonly Color AllianceFillBottom = new Color(0.04f, 0.06f, 0.09f, 0.92f);
        private static readonly Color AllianceFillTop = new Color(0.08f, 0.12f, 0.17f, 0.94f);
        private static readonly Color AllianceEdgeInner = new Color(0.32f, 0.54f, 0.72f, 0.65f);
        private static readonly Color AllianceTopCrest = new Color(0.28f, 0.78f, 0.92f, 0.92f);

        // Legion scheme (Industrial Bronze & Amber-Rust)
        private static readonly Color LegionFillBottom = new Color(0.08f, 0.05f, 0.04f, 0.92f);
        private static readonly Color LegionFillTop = new Color(0.15f, 0.09f, 0.06f, 0.94f);
        private static readonly Color LegionEdgeInner = new Color(0.68f, 0.38f, 0.20f, 0.68f);
        private static readonly Color LegionTopCrest = new Color(0.95f, 0.48f, 0.20f, 0.92f);

        private static readonly Color PanelEdgeOuter = new Color(0.02f, 0.03f, 0.05f, 0.96f);
        private static readonly Color OpaqueFillBottom = new Color(0.04f, 0.06f, 0.09f, 1f);
        private static readonly Color OpaqueFillTop = new Color(0.08f, 0.11f, 0.16f, 1f);

        private static FactionId _activeFaction = FactionId.Alliance;
        private static bool _schemeInitialized;

        private static Texture2D _pixel;
        private static Texture2D _panelTexture;
        private static Texture2D _opaquePanelTexture;
        private static Texture2D _scanlineTexture;
        private static Texture2D _btnNormalTex;
        private static Texture2D _btnHoverTex;
        private static Texture2D _btnActiveTex;
        private static Texture2D _btnDisabledTex;
        private static GUIStyle _panelStyle;
        private static GUIStyle _opaquePanelStyle;

        /// <summary>Currently active faction scheme driving cockpit chrome colours.</summary>
        public static FactionId ActiveFaction => _activeFaction;

        /// <summary>Primary faction crest accent colour (Cyan for Alliance, Industrial Amber-Rust for Legion).</summary>
        public static Color ActiveCrestColor =>
            _activeFaction == FactionId.Legion ? LegionTopCrest : AllianceTopCrest;

        /// <summary>Inner bevel border colour for the active faction's ControlBarScheme.</summary>
        public static Color ActiveBorderColor =>
            _activeFaction == FactionId.Legion ? LegionEdgeInner : AllianceEdgeInner;

        /// <summary>Short tactical header label for the active faction's command bar.</summary>
        public static string FactionCommandLabel(FactionId faction)
        {
            return faction == FactionId.Legion ? "LEGION // KOMMANDO" : "ALLIANZ // KOMMANDO";
        }

        /// <summary>
        /// Switches the shared cockpit scheme to match the local player's
        /// faction (inspired by C&amp;C Generals <c>ControlBarScheme</c>).
        /// Rebuilds the cached panel and button textures only when the faction
        /// actually changes.
        /// </summary>
        public static void SetActiveFaction(FactionId faction)
        {
            if (_schemeInitialized && _activeFaction == faction && _panelTexture != null) return;
            _activeFaction = faction;
            _schemeInitialized = true;

            if (_panelTexture != null)
            {
                Object.Destroy(_panelTexture);
                _panelTexture = null;
            }
            if (_btnNormalTex != null)
            {
                Object.Destroy(_btnNormalTex);
                _btnNormalTex = null;
            }
            if (_btnHoverTex != null)
            {
                Object.Destroy(_btnHoverTex);
                _btnHoverTex = null;
            }
            if (_btnActiveTex != null)
            {
                Object.Destroy(_btnActiveTex);
                _btnActiveTex = null;
            }
            if (_panelStyle != null)
            {
                _panelStyle.normal.background = PanelTexture;
            }
        }

        /// <summary>Shared 1x1 white texture for tinted solid fills (progress bars, minimap dots, viewport lines, accent strips).</summary>
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

        /// <summary>Subtle CRT scanline texture for the Radar Dome and tactical military tickers.</summary>
        public static Texture2D ScanlineTexture
        {
            get
            {
                if (_scanlineTexture == null)
                {
                    _scanlineTexture = new Texture2D(4, 4, TextureFormat.RGBA32, mipChain: false)
                    {
                        hideFlags = HideFlags.HideAndDontSave,
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Repeat
                    };
                    for (int y = 0; y < 4; y++)
                    {
                        Color c = (y % 2 == 0)
                            ? new Color(0f, 0f, 0f, 0.24f)
                            : new Color(1f, 1f, 1f, 0.03f);
                        for (int x = 0; x < 4; x++) _scanlineTexture.SetPixel(x, y, c);
                    }
                    _scanlineTexture.Apply();
                }
                return _scanlineTexture;
            }
        }

        /// <summary>The chrome panel texture: dark tactical gradient fill with a crisp dual-layer border ring and faction top crest.</summary>
        public static Texture2D PanelTexture
        {
            get
            {
                if (_panelTexture == null)
                {
                    bool legion = _activeFaction == FactionId.Legion;
                    _panelTexture = CreatePanelTexture(
                        legion ? LegionFillBottom : AllianceFillBottom,
                        legion ? LegionFillTop : AllianceFillTop,
                        legion ? LegionEdgeInner : AllianceEdgeInner,
                        legion ? LegionTopCrest : AllianceTopCrest);
                }
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
                else if (_panelStyle.normal.background == null)
                {
                    _panelStyle.normal.background = PanelTexture;
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
                    if (_opaquePanelTexture == null)
                    {
                        _opaquePanelTexture = CreatePanelTexture(
                            OpaqueFillBottom, OpaqueFillTop, AllianceEdgeInner, AllianceTopCrest);
                    }
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

        /// <summary>
        /// Applies custom tactical sci-fi button textures and crisp text colors
        /// to a button <see cref="GUIStyle"/> without altering its margins or padding.
        /// </summary>
        public static void ApplyButtonChrome(GUIStyle style)
        {
            EnsureButtonTextures();
            style.border = new RectOffset(2, 2, 2, 2);
            style.normal.background = _btnNormalTex;
            style.normal.textColor = new Color(0.92f, 0.96f, 1.00f, 1f);
            style.hover.background = _btnHoverTex;
            style.hover.textColor = Color.white;
            style.active.background = _btnActiveTex;
            style.active.textColor = _activeFaction == FactionId.Legion
                ? new Color(1.00f, 0.78f, 0.42f, 1f)
                : new Color(0.45f, 0.92f, 1.00f, 1f);
            style.focused.background = _btnHoverTex;
            style.focused.textColor = Color.white;
        }

        /// <summary>
        /// Draws C&amp;C Generals-style L-shaped metallic corner brackets on a
        /// HUD panel or button rect.
        /// </summary>
        public static void DrawTacticalCorners(Rect rect, Color color, float armLength = 6f, float thickness = 1.5f)
        {
            Color prev = GUI.color;
            GUI.color = color;
            Texture2D px = Pixel;

            // Top-left
            GUI.DrawTexture(new Rect(rect.x, rect.y, armLength, thickness), px);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, armLength), px);

            // Top-right
            GUI.DrawTexture(new Rect(rect.xMax - armLength, rect.y, armLength, thickness), px);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, armLength), px);

            // Bottom-left
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, armLength, thickness), px);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - armLength, thickness, armLength), px);

            // Bottom-right
            GUI.DrawTexture(new Rect(rect.xMax - armLength, rect.yMax - thickness, armLength, thickness), px);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMax - armLength, thickness, armLength), px);

            GUI.color = prev;
        }

        /// <summary>
        /// Draws a C&amp;C Generals build/production progress overlay across a
        /// button rect: a rising translucent fill plus a bright leading laser
        /// edge and bottom progress bar.
        /// </summary>
        public static void DrawCooldownOverlay(Rect rect, float progress01, Color accent)
        {
            float clamped = Mathf.Clamp01(progress01);
            if (clamped <= 0f) return;

            Color prev = GUI.color;
            Texture2D px = Pixel;

            // Rising translucent fill from bottom to top (completed portion)
            float fillHeight = (rect.height - 4f) * clamped;
            Rect fillRect = new Rect(rect.x + 2f, rect.yMax - 2f - fillHeight, rect.width - 4f, fillHeight);
            GUI.color = new Color(accent.r, accent.g, accent.b, 0.18f);
            GUI.DrawTexture(fillRect, px);

            // Leading horizontal sweep line
            if (clamped < 0.995f)
            {
                GUI.color = new Color(accent.r, accent.g, accent.b, 0.85f);
                GUI.DrawTexture(new Rect(rect.x + 2f, fillRect.y, rect.width - 4f, 1.5f), px);
            }

            // Crisp bottom progress bar strip
            float barWidth = (rect.width - 4f) * clamped;
            GUI.color = new Color(0.02f, 0.04f, 0.07f, 0.85f);
            GUI.DrawTexture(new Rect(rect.x + 2f, rect.yMax - 5f, rect.width - 4f, 3f), px);
            GUI.color = new Color(accent.r, accent.g, accent.b, 0.95f);
            GUI.DrawTexture(new Rect(rect.x + 2f, rect.yMax - 5f, barWidth, 3f), px);

            GUI.color = prev;
        }

        /// <summary>Category accent color for a building or unit role in the HUD.</summary>
        public static Color RoleAccentColor(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Refinery:
                case UnitRole.Storage:
                case UnitRole.Harvester:
                    return new Color(0.20f, 0.88f, 0.92f, 0.95f); // Aetherium Cyan
                case UnitRole.Power:
                case UnitRole.Builder:
                    return new Color(0.98f, 0.76f, 0.22f, 0.95f); // Industrial Gold
                case UnitRole.Barracks:
                case UnitRole.VehicleFactory:
                case UnitRole.BasicInfantry:
                case UnitRole.AntiArmorInfantry:
                case UnitRole.ScoutVehicle:
                case UnitRole.LightTank:
                case UnitRole.BattleTank:
                case UnitRole.Artillery:
                    return new Color(0.95f, 0.44f, 0.26f, 0.95f); // Combat Coral/Orange
                case UnitRole.ResearchLab:
                case UnitRole.Radar:
                    return new Color(0.56f, 0.68f, 0.98f, 0.95f); // Tech Azure
                case UnitRole.DefensePlatform:
                case UnitRole.HQ:
                default:
                    return new Color(0.36f, 0.88f, 0.56f, 0.95f); // Command Emerald
            }
        }

        /// <summary>Short 3-letter tactical code for a building role on the build bar.</summary>
        public static string BuildingBadgeCode(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.HQ: return "HQ";
                case UnitRole.Refinery: return "RAF";
                case UnitRole.Power: return "NRG";
                case UnitRole.Storage: return "DEP";
                case UnitRole.Barracks: return "KAS";
                case UnitRole.VehicleFactory: return "FAB";
                case UnitRole.ResearchLab: return "LAB";
                case UnitRole.Radar: return "RAD";
                case UnitRole.DefensePlatform: return "DEF";
                default: return "BAU";
            }
        }

        /// <summary>Short 3-letter tactical code for a mobile unit role on the command card.</summary>
        public static string UnitBadgeCode(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Builder: return "PIO";
                case UnitRole.Harvester: return "ern".ToUpperInvariant();
                case UnitRole.BasicInfantry: return "INF";
                case UnitRole.AntiArmorInfantry: return "PAK";
                case UnitRole.ScoutVehicle: return "SPÄ";
                case UnitRole.LightTank: return "LPZ";
                case UnitRole.BattleTank: return "KPZ";
                case UnitRole.Artillery: return "ART";
                default: return "EIN";
            }
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
            if (_scanlineTexture != null)
            {
                Object.Destroy(_scanlineTexture);
                _scanlineTexture = null;
            }
            if (_btnNormalTex != null)
            {
                Object.Destroy(_btnNormalTex);
                _btnNormalTex = null;
            }
            if (_btnHoverTex != null)
            {
                Object.Destroy(_btnHoverTex);
                _btnHoverTex = null;
            }
            if (_btnActiveTex != null)
            {
                Object.Destroy(_btnActiveTex);
                _btnActiveTex = null;
            }
            if (_btnDisabledTex != null)
            {
                Object.Destroy(_btnDisabledTex);
                _btnDisabledTex = null;
            }
            _panelStyle = null;
            _opaquePanelStyle = null;
            _schemeInitialized = false;
        }

        private static void EnsureButtonTextures()
        {
            bool legion = _activeFaction == FactionId.Legion;
            if (_btnNormalTex == null)
            {
                _btnNormalTex = legion
                    ? CreateButtonTexture(
                        new Color(0.14f, 0.09f, 0.07f, 0.95f),
                        new Color(0.22f, 0.14f, 0.10f, 0.95f),
                        new Color(0.54f, 0.32f, 0.18f, 0.75f))
                    : CreateButtonTexture(
                        new Color(0.09f, 0.13f, 0.18f, 0.95f),
                        new Color(0.14f, 0.19f, 0.26f, 0.95f),
                        new Color(0.28f, 0.42f, 0.56f, 0.72f));
            }
            if (_btnHoverTex == null)
            {
                _btnHoverTex = legion
                    ? CreateButtonTexture(
                        new Color(0.24f, 0.15f, 0.10f, 0.98f),
                        new Color(0.34f, 0.21f, 0.13f, 0.98f),
                        new Color(0.95f, 0.56f, 0.24f, 0.95f))
                    : CreateButtonTexture(
                        new Color(0.14f, 0.22f, 0.31f, 0.98f),
                        new Color(0.20f, 0.32f, 0.44f, 0.98f),
                        new Color(0.35f, 0.82f, 0.96f, 0.95f));
            }
            if (_btnActiveTex == null)
            {
                _btnActiveTex = legion
                    ? CreateButtonTexture(
                        new Color(0.36f, 0.20f, 0.12f, 1.00f),
                        new Color(0.24f, 0.13f, 0.08f, 1.00f),
                        new Color(1.00f, 0.68f, 0.32f, 1.00f))
                    : CreateButtonTexture(
                        new Color(0.18f, 0.34f, 0.46f, 1.00f),
                        new Color(0.12f, 0.24f, 0.34f, 1.00f),
                        new Color(0.48f, 0.92f, 1.00f, 1.00f));
            }
            if (_btnDisabledTex == null)
            {
                _btnDisabledTex = CreateButtonTexture(
                    new Color(0.06f, 0.08f, 0.11f, 0.75f),
                    new Color(0.08f, 0.10f, 0.14f, 0.75f),
                    new Color(0.20f, 0.25f, 0.32f, 0.40f));
            }
        }

        private static Texture2D CreatePanelTexture(Color fillBottom, Color fillTop, Color edgeInner, Color topCrest)
        {
            var texture = new Texture2D(PanelTextureSize, PanelTextureSize, TextureFormat.RGBA32, mipChain: false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < PanelTextureSize; y++)
            {
                float v = y / (float)(PanelTextureSize - 1);
                Color rowFill = Color.Lerp(fillBottom, fillTop, v);
                if (y % 2 == 0)
                {
                    rowFill = new Color(rowFill.r * 0.94f, rowFill.g * 0.94f, rowFill.b * 0.94f, rowFill.a);
                }
                for (int x = 0; x < PanelTextureSize; x++)
                {
                    bool isOuter = x == 0 || y == 0 || x == PanelTextureSize - 1 || y == PanelTextureSize - 1;
                    bool isInner = x == 1 || y == 1 || x == PanelTextureSize - 2 || y == PanelTextureSize - 2;
                    Color pixel;
                    if (isOuter)
                    {
                        pixel = PanelEdgeOuter;
                    }
                    else if (y == PanelTextureSize - 2)
                    {
                        pixel = topCrest;
                    }
                    else if (isInner)
                    {
                        pixel = edgeInner;
                    }
                    else
                    {
                        pixel = rowFill;
                    }
                    texture.SetPixel(x, y, pixel);
                }
            }
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateButtonTexture(Color fillBottom, Color fillTop, Color borderColor)
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color outer = new Color(0.02f, 0.03f, 0.05f, 0.95f);
            for (int y = 0; y < size; y++)
            {
                float v = y / (float)(size - 1);
                Color fill = Color.Lerp(fillBottom, fillTop, v);
                for (int x = 0; x < size; x++)
                {
                    bool isOuter = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    bool isInner = x == 1 || y == 1 || x == size - 2 || y == size - 2;
                    texture.SetPixel(x, y, isOuter ? outer : (isInner ? borderColor : fill));
                }
            }
            texture.Apply();
            return texture;
        }
    }
}
