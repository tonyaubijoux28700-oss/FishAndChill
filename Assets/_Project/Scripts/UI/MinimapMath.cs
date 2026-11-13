using UnityEngine;

namespace FishAndChill.UI
{
    /// <summary>Calculs de la minimap (logique pure, testée).</summary>
    public static class MinimapMath
    {
        /// <summary>
        /// Position d'un point du monde sur la carte, de (0, 0) en bas à gauche à (1, 1) en haut à droite,
        /// limitée aux bords de la carte.
        /// </summary>
        public static Vector2 WorldToNormalized(Vector2 worldPosition, Rect worldBounds)
        {
            if (worldBounds.width <= 0f || worldBounds.height <= 0f)
            {
                return new Vector2(0.5f, 0.5f);
            }

            return new Vector2(
                Mathf.Clamp01((worldPosition.x - worldBounds.xMin) / worldBounds.width),
                Mathf.Clamp01((worldPosition.y - worldBounds.yMin) / worldBounds.height));
        }

        /// <summary>Opacité du point qui clignote doucement (entre 0,35 et 1).</summary>
        public static float BlinkAlpha(float time, float blinksPerSecond = 2f)
        {
            return Mathf.Lerp(0.35f, 1f, 0.5f + 0.5f * Mathf.Cos(time * blinksPerSecond * Mathf.PI * 2f));
        }
    }
}
