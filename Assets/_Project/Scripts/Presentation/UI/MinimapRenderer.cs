using System;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// Minimap rendering utility converting 2D simulation world coordinates to minimap UI canvas coordinates.
    /// </summary>
    public sealed class MinimapRenderer
    {
        public static (float uiX, float uiY) WorldToMinimapCoordinates(
            float worldX,
            float worldY,
            float mapWidth = 128f,
            float mapHeight = 128f,
            float minimapWidth = 256f,
            float minimapHeight = 256f)
        {
            float normX = Math.Max(0f, Math.Min(1f, worldX / mapWidth));
            float normY = Math.Max(0f, Math.Min(1f, worldY / mapHeight));

            return (normX * minimapWidth, normY * minimapHeight);
        }
    }
}
