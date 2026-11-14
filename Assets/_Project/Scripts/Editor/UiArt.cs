using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>Icônes d'interface en pixel art.</summary>
    public static class UiArt
    {
        private static Color C(float r, float g, float b) => new Color(r, g, b);

        /// <summary>Sacoche de pêcheur 20×20 : cuir, rabat, boucle dorée, sangle, queue de poisson qui dépasse.</summary>
        public static Color[,] BagIcon()
        {
            var p = new Color[20, 20];
            Color leather = C(0.62f, 0.40f, 0.22f);
            Color leatherDark = C(0.46f, 0.28f, 0.15f);
            Color leatherLight = C(0.76f, 0.52f, 0.30f);
            Color strap = C(0.38f, 0.23f, 0.12f);
            Color buckle = C(1.00f, 0.82f, 0.28f);
            Color fish = C(0.40f, 0.70f, 0.80f);

            // Sangle en arc au-dessus du sac.
            for (int x = 4; x <= 15; x++)
            {
                float t = (x - 9.5f) / 5.5f;
                int y = Mathf.RoundToInt(17f - t * t * 4f);
                PixelArt.Set(p, x, y, strap);
                PixelArt.Set(p, x, y - 1, strap);
            }

            // Queue de poisson qui dépasse du sac.
            PixelArt.Rect(p, 12, 12, 13, 14, fish);
            PixelArt.Set(p, 11, 15, fish);
            PixelArt.Set(p, 14, 15, fish);

            // Corps du sac.
            PixelArt.Rect(p, 2, 1, 17, 11, leather);
            PixelArt.Rect(p, 2, 1, 17, 2, leatherDark);
            PixelArt.Rect(p, 16, 1, 17, 11, leatherDark);
            PixelArt.Rect(p, 3, 3, 3, 10, leatherLight);

            // Rabat et boucle.
            PixelArt.Rect(p, 2, 8, 17, 12, leatherDark);
            PixelArt.Rect(p, 3, 9, 16, 11, leather);
            PixelArt.Rect(p, 8, 6, 11, 9, buckle);
            PixelArt.Rect(p, 9, 7, 10, 8, leatherDark);

            // Coutures.
            for (int x = 4; x <= 15; x += 2)
            {
                PixelArt.Set(p, x, 2, leatherLight);
            }

            PixelArt.Outline(p, C(0.12f, 0.07f, 0.04f));
            return p;
        }
    }
}
