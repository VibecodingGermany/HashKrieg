using System;

namespace Nova.Gameplay
{
    /// <summary>
    /// Minimap coordinate math: converts between 2D simulation world
    /// coordinates and minimap UI coordinates, and projects the RTS camera's
    /// ground footprint into world space for the viewport rectangle. Pure C#
    /// (no UnityEngine), so the whole surface is EditMode-testable
    /// (MinimapRendererTests); the drawing MonoBehaviour (MinimapHud,
    /// Nova.Presentation.UI) only renders what these functions return.
    /// <para>
    /// ORIENTATION: <see cref="WorldToMinimapCoordinates"/> is the original
    /// unflipped mapping (pinned by SelectionManagerTests). The GUI variants
    /// flip Y because IMGUI is top-left origin while the world runs z-up the
    /// map: world (0, 0) — the local player's corner — sits at the BOTTOM-left
    /// of the minimap, the RTS convention the main camera also opens with.
    /// </para>
    /// </summary>
    public sealed class MinimapRenderer
    {
        private const float Deg2Rad = (float)(Math.PI / 180.0);

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

        /// <summary>
        /// World -> minimap position in IMGUI space (top-left origin): same as
        /// <see cref="WorldToMinimapCoordinates"/> but with Y flipped, so
        /// world z = 0 renders at the bottom edge of the minimap. Inputs are
        /// clamped to the map rectangle, so an off-map entity still lands on
        /// the rim instead of outside the panel.
        /// </summary>
        public static (float uiX, float uiY) WorldToMinimapGuiCoordinates(
            float worldX,
            float worldZ,
            float mapWidth = 128f,
            float mapHeight = 128f,
            float minimapWidth = 256f,
            float minimapHeight = 256f)
        {
            (float uiX, float uiY) = WorldToMinimapCoordinates(
                worldX, worldZ, mapWidth, mapHeight, minimapWidth, minimapHeight);
            return (uiX, minimapHeight - uiY);
        }

        /// <summary>
        /// Minimap position in IMGUI space -> world ground position, the exact
        /// inverse of <see cref="WorldToMinimapGuiCoordinates"/> (click-to-jump).
        /// Clamped into the map rectangle: a click on the rim targets the map
        /// edge, never off-map terrain.
        /// </summary>
        public static (float worldX, float worldZ) MinimapGuiToWorldCoordinates(
            float uiX,
            float uiY,
            float mapWidth = 128f,
            float mapHeight = 128f,
            float minimapWidth = 256f,
            float minimapHeight = 256f)
        {
            float normX = minimapWidth > 0f ? uiX / minimapWidth : 0f;
            float normZ = minimapHeight > 0f ? 1f - uiY / minimapHeight : 0f;
            normX = Math.Max(0f, Math.Min(1f, normX));
            normZ = Math.Max(0f, Math.Min(1f, normZ));
            return (normX * mapWidth, normZ * mapHeight);
        }

        /// <summary>
        /// The four corners of the camera frustum's intersection with the y=0
        /// ground plane, in world XZ — the outline the minimap draws as the
        /// viewport rectangle. Mirrors <c>RtsCameraController</c>'s transform
        /// exactly (Unity rotation convention R = Ry(yaw) * Rx(pitch), roll 0,
        /// camera at <c>focus - forward * (height / sin(pitch))</c>), so the
        /// rectangle cannot drift from what the rig actually shows. Returns
        /// false when a frustum ray runs parallel to or above the horizon
        /// (possible at very flat pitch/fov combinations) — the HUD simply
        /// skips the rectangle that frame instead of drawing garbage.
        /// </summary>
        public static bool TryComputeGroundViewCorners(
            float focusX,
            float focusZ,
            float cameraHeight,
            float pitchDegrees,
            float yawDegrees,
            float verticalFovDegrees,
            float aspect,
            out (float x, float z) bottomLeft,
            out (float x, float z) bottomRight,
            out (float x, float z) topRight,
            out (float x, float z) topLeft)
        {
            bottomLeft = bottomRight = topRight = topLeft = (0f, 0f);
            if (cameraHeight <= 0f || aspect <= 0f) return false;

            float pitch = pitchDegrees * Deg2Rad;
            float yaw = yawDegrees * Deg2Rad;
            float sinPitch = (float)Math.Sin(pitch);
            float cosPitch = (float)Math.Cos(pitch);
            float sinYaw = (float)Math.Sin(yaw);
            float cosYaw = (float)Math.Cos(yaw);
            if (sinPitch < 1e-4f) return false;

            float tanY = (float)Math.Tan(verticalFovDegrees * 0.5f * Deg2Rad);
            if (tanY <= 1e-6f) return false;
            float tanX = tanY * aspect;

            // Camera basis under R = Ry(yaw) * Rx(pitch):
            float forwardX = cosPitch * sinYaw;
            float forwardY = -sinPitch;
            float forwardZ = cosPitch * cosYaw;
            float rightX = cosYaw;
            float rightZ = -sinYaw; // right.y is 0
            float upX = sinPitch * sinYaw;
            float upY = cosPitch;
            float upZ = sinPitch * cosYaw;

            float distance = cameraHeight / sinPitch;
            float camX = focusX - forwardX * distance;
            float camY = -forwardY * distance; // focus.y = 0, so this is exactly cameraHeight
            float camZ = focusZ - forwardZ * distance;

            return TryIntersectGround(
                       camX, camY, camZ,
                       forwardX, forwardY, forwardZ, rightX, rightZ, upX, upY, upZ, tanX, tanY,
                       -1f, -1f, out bottomLeft)
                   && TryIntersectGround(
                       camX, camY, camZ,
                       forwardX, forwardY, forwardZ, rightX, rightZ, upX, upY, upZ, tanX, tanY,
                       1f, -1f, out bottomRight)
                   && TryIntersectGround(
                       camX, camY, camZ,
                       forwardX, forwardY, forwardZ, rightX, rightZ, upX, upY, upZ, tanX, tanY,
                       1f, 1f, out topRight)
                   && TryIntersectGround(
                       camX, camY, camZ,
                       forwardX, forwardY, forwardZ, rightX, rightZ, upX, upY, upZ, tanX, tanY,
                       -1f, 1f, out topLeft);
        }

        /// <summary>
        /// Intersects one frustum corner ray (screen sign sx/sy in [-1, 1])
        /// with the y=0 plane. Ray direction needs no normalization — only
        /// its slope decides the intersection. False when the ray does not
        /// point downward.
        /// </summary>
        private static bool TryIntersectGround(
            float camX, float camY, float camZ,
            float forwardX, float forwardY, float forwardZ,
            float rightX, float rightZ,
            float upX, float upY, float upZ,
            float tanX, float tanY,
            float sx, float sy,
            out (float x, float z) corner)
        {
            corner = (0f, 0f);
            float dirX = forwardX + rightX * tanX * sx + upX * tanY * sy;
            float dirY = forwardY + upY * tanY * sy; // right.y is 0
            float dirZ = forwardZ + rightZ * tanX * sx + upZ * tanY * sy;
            if (dirY >= -1e-6f) return false;

            float t = -camY / dirY;
            corner = (camX + dirX * t, camZ + dirZ * t);
            return true;
        }
    }
}
