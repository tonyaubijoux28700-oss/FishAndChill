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

        /// <summary>Bulle avec une pièce d'or (le bouledogue a trouvé des pièces), 12×15, même pivot que <see cref="Exclamation"/>.</summary>
        public static Color[,] Coin()
        {
            Color[,] p = Bubble(12, 15);
            for (int x = 0; x < 12; x++)
            {
                for (int y = 5; y < 14; y++)
                {
                    float u = (x + 0.5f - 6f) / 3.6f, v = (y + 0.5f - 9f) / 3.6f;
                    float d = u * u + v * v;
                    if (d > 1f) continue;
                    p[x, y] = d > 0.6f ? GoldDark : (x + y) % 5 == 0 ? new Color(1f, 0.94f, 0.60f) : Gold;
                }
            }

            p[6, 10] = GoldDark;
            p[6, 9] = GoldDark;
            p[6, 8] = GoldDark;
            return p;
        }

        /// <summary>
        /// Bulle avec un cœur rouge (l'animal est content : on le caresse, ou une belle prise), 12×15, même pivot que
        /// <see cref="Exclamation"/>.
        /// </summary>
        public static Color[,] Heart()
        {
            Color[,] p = Bubble(12, 15);
            var red = new Color(0.90f, 0.20f, 0.26f);
            var dark = new Color(0.62f, 0.10f, 0.16f);
            var shine = new Color(1f, 0.66f, 0.70f);
            string[] rows =
            {
                ".##..##.",
                "#ss##..#",
                "#s.....#",
                "#......#",
                ".#....#.",
                "..#..#..",
                "...##...",
            };
            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < rows[r].Length; c++)
                {
                    char ch = rows[r][c];
                    int x = 2 + c, y = 12 - r;
                    // Contour foncé, intérieur rouge, reflet clair en haut à gauche.
                    if (ch == '#') p[x, y] = dark;
                    else if (ch == 's') p[x, y] = shine;
                    else if (r > 0 && r < rows.Length - 1 && IsInside(rows, r, c)) p[x, y] = red;
                }
            }

            return p;
        }

        private static bool IsInside(string[] rows, int r, int c)
        {
            string row = rows[r];
            int first = row.IndexOfAny(new[] { '#', 's' }), last = row.LastIndexOfAny(new[] { '#', 's' });
            return first >= 0 && c > first && c < last;
        }

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
