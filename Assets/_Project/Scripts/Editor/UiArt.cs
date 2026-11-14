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

        /// <summary>
        /// Livre de l'index 20×20 : couverture bleu lagon, tranche des pages crème à droite, coins dorés,
        /// poisson doré en relief et marque-page rouge.
        /// </summary>
        public static Color[,] BookIcon()
        {
            var p = new Color[20, 20];
            Color cover = C(0.16f, 0.46f, 0.58f);
            Color coverDark = C(0.10f, 0.32f, 0.42f);
            Color coverLight = C(0.30f, 0.62f, 0.72f);
            Color pages = C(0.96f, 0.91f, 0.76f);
            Color pagesShade = C(0.80f, 0.72f, 0.56f);
            Color gold = C(1.00f, 0.82f, 0.28f);
            Color goldDark = C(0.78f, 0.56f, 0.16f);
            Color ribbon = C(0.86f, 0.22f, 0.24f);

            // Pages (visibles à droite et en bas, le livre est légèrement de trois quarts).
            PixelArt.Rect(p, 4, 1, 17, 17, pages);
            for (int y = 2; y <= 16; y += 2)
            {
                PixelArt.Set(p, 17, y, pagesShade);
            }

            PixelArt.Rect(p, 4, 1, 17, 1, pagesShade);

            // Couverture.
            PixelArt.Rect(p, 2, 3, 15, 18, cover);
            PixelArt.Rect(p, 2, 3, 3, 18, coverDark);           // dos du livre
            PixelArt.Rect(p, 4, 17, 15, 17, coverLight);        // reflet du haut
            PixelArt.Rect(p, 15, 3, 15, 18, coverDark);

            // Coins dorés.
            PixelArt.Rect(p, 13, 16, 14, 17, gold);
            PixelArt.Rect(p, 13, 4, 14, 5, gold);

            // Poisson doré au centre de la couverture (tête à droite).
            PixelArt.Rect(p, 7, 10, 11, 12, gold);
            PixelArt.Rect(p, 8, 9, 10, 13, gold);
            PixelArt.Set(p, 12, 11, gold);
            PixelArt.Rect(p, 5, 9, 5, 13, gold);                 // queue
            PixelArt.Set(p, 6, 10, gold);
            PixelArt.Set(p, 6, 12, gold);
            PixelArt.Set(p, 11, 12, goldDark);                   // œil
            PixelArt.Rect(p, 7, 7, 12, 7, goldDark);             // trait de titre
            PixelArt.Rect(p, 8, 5, 11, 5, goldDark);

            // Marque-page qui dépasse en bas.
            PixelArt.Rect(p, 10, 0, 11, 2, ribbon);
            PixelArt.Set(p, 10, 0, PixelArt.Clear);

            PixelArt.Outline(p, C(0.05f, 0.10f, 0.14f));
            return p;
        }
    }
}
