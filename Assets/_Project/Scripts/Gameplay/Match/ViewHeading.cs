using UnityEngine;
using Nova.Core;

namespace Nova.Gameplay.Match
{
    /// <summary>
    /// The one place where a simulation heading becomes a Unity yaw.
    /// <para>
    /// The two conventions disagree on both origin and sense of rotation.
    /// The simulation measures mathematically (<see cref="SimTrig.Atan2"/>):
    /// 0 points along sim +X and angles grow toward sim +Y, i.e.
    /// counter-clockwise seen from above. Unity's yaw is 0 along world +Z and
    /// grows clockwise seen from above (toward +X). With sim X mapped to
    /// world X and sim Y to world Z, the yaw is therefore
    /// <c>90° − simDegrees</c>. Feeding the sim angle straight into
    /// <see cref="Quaternion.Euler(float, float, float)"/> mirrored every
    /// heading across the north-east diagonal: a unit driving east looked
    /// north, one driving north looked east, and only the diagonal itself
    /// came out right.
    /// </para>
    /// <para>
    /// Every art prefab faces world +Z at yaw 0 (the -90° Tripo correction in
    /// <c>tools/art/glb_to_unity_fbx.py</c>), so a model rotated by this yaw
    /// looks where the simulation drives it.
    /// </para>
    /// </summary>
    public static class ViewHeading
    {
        /// <summary>Unity yaw in degrees, normalised to [0, 360).</summary>
        public static float YawDegrees(SimAngle simHeading)
        {
            return Mathf.Repeat(90f - simHeading.ToDegrees().ToFloat(), 360f);
        }

        /// <summary>The rotation a unit view takes for a simulation heading.</summary>
        public static Quaternion Rotation(SimAngle simHeading)
        {
            return Quaternion.Euler(0f, YawDegrees(simHeading), 0f);
        }

        /// <summary>
        /// Unity yaw that looks from one world position toward another on the
        /// ground plane; <paramref name="fallbackYaw"/> when they coincide.
        /// </summary>
        public static float YawToward(Vector3 from, Vector3 to, float fallbackYaw)
        {
            float dx = to.x - from.x;
            float dz = to.z - from.z;
            if (dx * dx + dz * dz < 1e-6f) return fallbackYaw;
            return Mathf.Repeat(Mathf.Atan2(dx, dz) * Mathf.Rad2Deg, 360f);
        }
    }
}
