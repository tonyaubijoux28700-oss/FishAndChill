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

        /// <summary>Angle (degrés) de la flèche du joueur pour un regard donné : 0 = vers le haut, sens trigonométrique.</summary>
        public static float FacingAngle(Vector2 facing)
        {
            if (facing.sqrMagnitude < 0.0001f)
            {
                return 180f; // regard vers le bas par défaut
            }

            return Mathf.Atan2(-facing.x, facing.y) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Valeur d'une grille de cases lue en un point continu (en cases, centre de la case (0, 0) = (0, 0)),
        /// par interpolation bilinéaire : sert à arrondir les côtes et à adoucir la profondeur de l'eau.
        /// </summary>
        public static float Bilinear(float[,] grid, float x, float y)
        {
            int w = grid.GetLength(0);
            int h = grid.GetLength(1);
            int x0 = Mathf.Clamp(Mathf.FloorToInt(x), 0, w - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(y), 0, h - 1);
            int x1 = Mathf.Min(x0 + 1, w - 1);
            int y1 = Mathf.Min(y0 + 1, h - 1);
            float tx = Mathf.Clamp01(x - Mathf.Floor(x));
            float ty = Mathf.Clamp01(y - Mathf.Floor(y));
            if (x < 0f) tx = 0f;
            if (y < 0f) ty = 0f;
            float bottom = Mathf.Lerp(grid[x0, y0], grid[x1, y0], tx);
            float top = Mathf.Lerp(grid[x0, y1], grid[x1, y1], tx);
            return Mathf.Lerp(bottom, top, ty);
        }

        /// <summary>Onde qui s'élargit autour du joueur : taille (1 à 2,6) et opacité (1 à 0) selon le temps.</summary>
        public static Vector2 Pulse(float time, float period = 1.4f)
        {
            float t = Mathf.Repeat(time / period, 1f);
            return new Vector2(1f + 1.6f * t, 1f - t);
        }
    }
}
