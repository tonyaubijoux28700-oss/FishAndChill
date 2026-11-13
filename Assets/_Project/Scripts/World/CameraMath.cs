using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>Calculs de caméra (logique pure, testée).</summary>
    public static class CameraMath
    {
        /// <summary>
        /// Centre de caméra le plus proche de <paramref name="center"/> qui garde tout l'écran dans
        /// <paramref name="bounds"/>. Si la carte est plus petite que l'écran, la caméra se centre sur la carte.
        /// </summary>
        public static Vector2 ClampToBounds(Vector2 center, Vector2 halfExtents, Rect bounds)
        {
            return new Vector2(
                ClampAxis(center.x, halfExtents.x, bounds.xMin, bounds.xMax),
                ClampAxis(center.y, halfExtents.y, bounds.yMin, bounds.yMax));
        }

        private static float ClampAxis(float value, float half, float min, float max)
        {
            if (max - min <= half * 2f)
            {
                return (min + max) * 0.5f;
            }

            return Mathf.Clamp(value, min + half, max - half);
        }
    }
}
