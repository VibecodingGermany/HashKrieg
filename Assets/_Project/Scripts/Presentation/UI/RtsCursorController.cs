using UnityEngine;

namespace Nova.Presentation.UI
{
    public enum RtsCursorKind : byte
    {
        Default,
        Move,
        Attack,
        Harvest,
        Repair,
    }

    /// <summary>
    /// Procedural tactical RTS cursor manager. Generates crisp, high-contrast
    /// 32x32 software/hardware cursor textures at runtime without external asset
    /// dependencies, providing immediate context feedback (Attack over enemy,
    /// Harvest over Aetherium, Repair over damaged structure, Move on ground).
    /// </summary>
    public static class RtsCursorController
    {
        private const int Size = 32;

        private static Texture2D _cursorDefault;
        private static Texture2D _cursorMove;
        private static Texture2D _cursorAttack;
        private static Texture2D _cursorHarvest;
        private static Texture2D _cursorRepair;
        private static RtsCursorKind _activeKind = (RtsCursorKind)255;

        public static void SetCursor(RtsCursorKind kind)
        {
            if (_activeKind == kind) return;
            _activeKind = kind;
            EnsureTextures();

            switch (kind)
            {
                case RtsCursorKind.Attack:
                    Cursor.SetCursor(_cursorAttack, new Vector2(16f, 16f), CursorMode.Auto);
                    break;
                case RtsCursorKind.Harvest:
                    Cursor.SetCursor(_cursorHarvest, new Vector2(16f, 16f), CursorMode.Auto);
                    break;
                case RtsCursorKind.Repair:
                    Cursor.SetCursor(_cursorRepair, new Vector2(8f, 8f), CursorMode.Auto);
                    break;
                case RtsCursorKind.Move:
                    Cursor.SetCursor(_cursorMove, new Vector2(16f, 16f), CursorMode.Auto);
                    break;
                default:
                    Cursor.SetCursor(_cursorDefault, new Vector2(1f, 1f), CursorMode.Auto);
                    break;
            }
        }

        public static void ResetToDefault()
        {
            SetCursor(RtsCursorKind.Default);
        }

        private static void EnsureTextures()
        {
            if (_cursorDefault != null) return;

            _cursorDefault = CreateDefaultTexture();
            _cursorAttack = CreateAttackTexture();
            _cursorHarvest = CreateHarvestTexture();
            _cursorRepair = CreateRepairTexture();
            _cursorMove = CreateMoveTexture();
        }

        private static Texture2D CreateDefaultTexture()
        {
            var tex = NewTexture();
            Color border = new Color(0.05f, 0.08f, 0.12f, 1f);
            Color fill = new Color(0.88f, 0.92f, 0.98f, 1f);
            Color accent = new Color(0.18f, 0.82f, 0.98f, 1f);

            // Sleek tactical arrowhead pointing towards (1, 1) top-left (y inverted in tex coords)
            for (int x = 1; x < 20; x++)
            {
                for (int y = 1; y < 20; y++)
                {
                    int invY = Size - 1 - y;
                    if (x + y <= 21 && (x <= y * 0.7f + 1 || y <= x * 0.7f + 1 || (x <= 13 && y <= 13)))
                    {
                        bool isBorder = x == 1 || y == 1 || x + y == 21 || (x == 13 && y >= 7) || (y == 13 && x >= 7);
                        bool isAccent = (x == 2 || y == 2) && !isBorder;
                        tex.SetPixel(x, invY, isBorder ? border : isAccent ? accent : fill);
                    }
                }
            }

            // Stem
            for (int i = 0; i < 7; i++)
            {
                int px = 8 + i;
                int py = 8 + i;
                int invY = Size - 1 - py;
                tex.SetPixel(px, invY, border);
                tex.SetPixel(px + 1, invY, fill);
                tex.SetPixel(px + 2, invY, border);
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateAttackTexture()
        {
            var tex = NewTexture();
            Color red = new Color(1f, 0.18f, 0.15f, 1f);
            Color white = new Color(1f, 0.95f, 0.95f, 1f);
            Color shadow = new Color(0.08f, 0.02f, 0.02f, 0.85f);

            int cx = 16;
            int cy = 16;

            // Center red dot
            SetPixelWithShadow(tex, cx, cy, white, shadow);
            SetPixelWithShadow(tex, cx - 1, cy, red, shadow);
            SetPixelWithShadow(tex, cx + 1, cy, red, shadow);
            SetPixelWithShadow(tex, cx, cy - 1, red, shadow);
            SetPixelWithShadow(tex, cx, cy + 1, red, shadow);

            // 4 reticle chevrons / crosshair notches
            for (int r = 5; r <= 11; r++)
            {
                SetPixelWithShadow(tex, cx + r, cy, red, shadow);
                SetPixelWithShadow(tex, cx - r, cy, red, shadow);
                SetPixelWithShadow(tex, cx, cy + r, red, shadow);
                SetPixelWithShadow(tex, cx, cy - r, red, shadow);
            }

            // Outer corner brackets
            int cornerDist = 10;
            int cornerArm = 3;
            for (int a = 0; a <= cornerArm; a++)
            {
                // Top-left
                SetPixelWithShadow(tex, cx - cornerDist + a, cy - cornerDist, red, shadow);
                SetPixelWithShadow(tex, cx - cornerDist, cy - cornerDist + a, red, shadow);
                // Top-right
                SetPixelWithShadow(tex, cx + cornerDist - a, cy - cornerDist, red, shadow);
                SetPixelWithShadow(tex, cx + cornerDist, cy - cornerDist + a, red, shadow);
                // Bottom-left
                SetPixelWithShadow(tex, cx - cornerDist + a, cy + cornerDist, red, shadow);
                SetPixelWithShadow(tex, cx - cornerDist, cy + cornerDist - a, red, shadow);
                // Bottom-right
                SetPixelWithShadow(tex, cx + cornerDist - a, cy + cornerDist, red, shadow);
                SetPixelWithShadow(tex, cx + cornerDist, cy + cornerDist - a, red, shadow);
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateHarvestTexture()
        {
            var tex = NewTexture();
            Color cyan = new Color(0.18f, 0.92f, 1f, 1f);
            Color darkCyan = new Color(0.06f, 0.55f, 0.72f, 1f);
            Color highlight = new Color(0.85f, 0.98f, 1f, 1f);
            Color border = new Color(0.02f, 0.12f, 0.18f, 0.9f);

            int cx = 16;
            int cy = 16;

            // Diamond crystal shape
            for (int dy = -9; dy <= 9; dy++)
            {
                int maxDx = 9 - Mathf.Abs(dy);
                for (int dx = -maxDx; dx <= maxDx; dx++)
                {
                    int px = cx + dx;
                    int py = cy + dy;
                    if (Mathf.Abs(dx) == maxDx || Mathf.Abs(dy) == 9)
                    {
                        SetPixelSafe(tex, px, py, border);
                    }
                    else if (dx <= 0 && dy <= 0)
                    {
                        SetPixelSafe(tex, px, py, (dx == -1 && dy == -1) ? highlight : cyan);
                    }
                    else
                    {
                        SetPixelSafe(tex, px, py, darkCyan);
                    }
                }
            }

            // Outline shadow
            for (int dy = -10; dy <= 10; dy++)
            {
                int maxDx = 10 - Mathf.Abs(dy);
                SetPixelSafe(tex, cx - maxDx, cy + dy, border);
                SetPixelSafe(tex, cx + maxDx, cy + dy, border);
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateRepairTexture()
        {
            var tex = NewTexture();
            Color yellow = new Color(1f, 0.85f, 0.20f, 1f);
            Color bright = new Color(1f, 0.95f, 0.65f, 1f);
            Color border = new Color(0.12f, 0.08f, 0.02f, 0.9f);

            // Wrench head at (8, 8) and handle angled down-right
            int hx = 9;
            int hy = 9;

            // Wrench circle with cutout
            for (int dy = -5; dy <= 5; dy++)
            {
                for (int dx = -5; dx <= 5; dx++)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= 5.2f && (dist >= 2.0f || dx > 0 || dy > 0))
                    {
                        bool isBorder = dist >= 4.4f || (dist <= 2.6f && !(dx > 0 && dy > 0));
                        SetPixelSafe(tex, hx + dx, hy + dy, isBorder ? border : (dx <= 0 && dy <= 0) ? bright : yellow);
                    }
                }
            }

            // Handle
            for (int i = 3; i <= 14; i++)
            {
                int px = hx + i;
                int py = hy + i;
                SetPixelSafe(tex, px - 1, py, border);
                SetPixelSafe(tex, px, py, yellow);
                SetPixelSafe(tex, px + 1, py, yellow);
                SetPixelSafe(tex, px + 2, py, border);
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateMoveTexture()
        {
            var tex = NewTexture();
            Color green = new Color(0.25f, 1f, 0.40f, 1f);
            Color white = new Color(0.92f, 1f, 0.92f, 1f);
            Color shadow = new Color(0.02f, 0.12f, 0.04f, 0.85f);

            int cx = 16;
            int cy = 16;

            // Center cross dot
            SetPixelWithShadow(tex, cx, cy, white, shadow);

            // 4 directional arrows
            for (int i = 3; i <= 9; i++)
            {
                SetPixelWithShadow(tex, cx + i, cy, green, shadow);
                SetPixelWithShadow(tex, cx - i, cy, green, shadow);
                SetPixelWithShadow(tex, cx, cy + i, green, shadow);
                SetPixelWithShadow(tex, cx, cy - i, green, shadow);
            }

            // Arrow heads
            for (int w = 1; w <= 3; w++)
            {
                SetPixelWithShadow(tex, cx + 9 - w, cy + w, green, shadow);
                SetPixelWithShadow(tex, cx + 9 - w, cy - w, green, shadow);

                SetPixelWithShadow(tex, cx - 9 + w, cy + w, green, shadow);
                SetPixelWithShadow(tex, cx - 9 + w, cy - w, green, shadow);

                SetPixelWithShadow(tex, cx + w, cy + 9 - w, green, shadow);
                SetPixelWithShadow(tex, cx - w, cy + 9 - w, green, shadow);

                SetPixelWithShadow(tex, cx + w, cy - 9 + w, green, shadow);
                SetPixelWithShadow(tex, cx - w, cy - 9 + w, green, shadow);
            }

            tex.Apply();
            return tex;
        }

        private static void SetPixelWithShadow(Texture2D tex, int x, int y, Color c, Color shadow)
        {
            // Drop shadow offset (+1, +1)
            SetPixelSafe(tex, x + 1, y + 1, shadow);
            SetPixelSafe(tex, x, y, c);
        }

        private static void SetPixelSafe(Texture2D tex, int x, int y, Color c)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size) return;
            // Texture coords: (0,0) is bottom-left, but cursor coordinates are top-left
            tex.SetPixel(x, Size - 1 - y, c);
        }

        private static Texture2D NewTexture()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, mipChain: false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
            };
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    tex.SetPixel(x, y, clear);
                }
            }
            return tex;
        }
    }
}
