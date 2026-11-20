using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Bulles au-dessus de la tête des habitants (16 px par case, comme les personnages ; y depuis le bas, pivot au bout
    /// de la pointe) : <see cref="Exclamation"/> 12×15, « ! » doré, une commande à écouter ; <see cref="Wide"/> 20×16,
    /// bulle vide où le jeu pose la petite image du poisson demandé quand on l'a dans le sac.
    /// </summary>
    public static class BubbleArt
    {
        private static readonly Color Outline = new Color(0.16f, 0.12f, 0.12f);
        private static readonly Color Paper = new Color(0.99f, 0.97f, 0.90f);
        private static readonly Color PaperShade = new Color(0.88f, 0.82f, 0.70f);
        private static readonly Color Gold = new Color(0.98f, 0.72f, 0.16f);
        private static readonly Color GoldDark = new Color(0.78f, 0.44f, 0.10f);

        public static readonly Vector2 ExclamationPivot = new Vector2(6f / 12f, 0f);
        public static readonly Vector2 WidePivot = new Vector2(10f / 20f, 0f);

        public static Color[,] Exclamation()
        {
            Color[,] p = Bubble(12, 15);
            for (int y = 7; y <= 12; y++)
            {
                p[5, y] = y == 12 ? GoldDark : Gold;
                p[6, y] = GoldDark;
            }

            p[5, 5] = Gold;
            p[6, 5] = GoldDark;
            return p;
        }

        public static Color[,] Wide() => Bubble(20, 16);

        /// <summary>Bulle arrondie à fond crème, ombrée en bas, pointe au milieu du bas.</summary>
        private static Color[,] Bubble(int width, int height)
        {
            var p = new Color[width, height];
            int bodyBottom = 3;
            for (int x = 0; x < width; x++)
            {
                for (int y = bodyBottom; y < height; y++)
                {
                    bool corner = (x == 0 || x == width - 1) && (y == bodyBottom || y == height - 1);
                    bool edge = x == 0 || x == width - 1 || y == bodyBottom || y == height - 1;
                    if (corner) continue;
                    p[x, y] = edge ? Outline : y == bodyBottom + 1 ? PaperShade : Paper;
                }
            }

            // Pointe vers la tête.
            int cx = width / 2;
            for (int y = 0; y < bodyBottom; y++)
            {
                int half = y;
                for (int x = cx - half - 1; x <= cx + half; x++)
                {
                    bool border = x == cx - half - 1 || x == cx + half;
                    p[x, y] = border ? Outline : PaperShade;
                }
            }

            p[cx - 1, bodyBottom] = PaperShade;
            p[cx, bodyBottom] = PaperShade;
            return p;
        }
    }
}
