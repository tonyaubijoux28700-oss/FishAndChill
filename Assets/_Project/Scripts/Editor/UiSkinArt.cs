using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Charte d'interface en pixel art (sprites étirables en 9 parties) : cadre de fenêtre doré à coins arrondis,
    /// case creusée, bandeau de titre, fond d'eau pour les poissons, pierre de rareté, halo.
    /// À 25 px par unité, un pixel de ces sprites fait 4 unités d'interface (≈ la taille d'un pixel du jeu).
    /// </summary>
    public static class UiSkinArt
    {
        public const int PixelsPerUnit = 25;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Ink = C(0.03f, 0.04f, 0.07f);
        private static readonly Color Gold = C(0.93f, 0.72f, 0.28f);
        private static readonly Color GoldLight = C(1.00f, 0.90f, 0.56f);
        private static readonly Color GoldDark = C(0.66f, 0.45f, 0.16f);
        private static readonly Color Navy = C(0.08f, 0.12f, 0.19f);
        private static readonly Color NavyLine = C(0.20f, 0.30f, 0.42f);

        /// <summary>Distance signée (en pixels) à l'intérieur d'un rectangle à coins arrondis : &lt; 0 = dehors.</summary>
        private static float Inside(int x, int y, int width, int height, float radius)
        {
            float px = x + 0.5f;
            float py = y + 0.5f;
            float dx = Mathf.Max(radius - px, 0f, px - (width - radius));
            float dy = Mathf.Max(radius - py, 0f, py - (height - radius));
            if (dx > 0f && dy > 0f)
            {
                return radius - Mathf.Sqrt(dx * dx + dy * dy);
            }

            return Mathf.Min(Mathf.Min(px, width - px), Mathf.Min(py, height - py));
        }

        /// <summary>Cadre de fenêtre 32×32 (bord de 10 px) : contour sombre, bord doré biseauté, filet, fond bleu nuit, rivets.</summary>
        public static Color[,] Panel()
        {
            const int size = 32;
            var p = new Color[size, size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float d = Inside(x, y, size, size, 7f);
                    if (d < 0f) continue;

                    bool upperLeft = x + (size - 1 - y) < size; // moitié haut-gauche : éclairée
                    Color c;
                    if (d < 1f) c = Ink;
                    else if (d < 2f) c = upperLeft ? GoldLight : Gold;
                    else if (d < 3f) c = upperLeft ? Gold : GoldDark;
                    else if (d < 4f) c = Ink;
                    else if (d < 5f) c = NavyLine;
                    else c = Navy;
                    p[x, y] = c;
                }
            }

            // Rivets dorés dans les coins.
            foreach (var (rx, ry) in new[] { (6, 6), (size - 7, 6), (6, size - 7), (size - 7, size - 7) })
            {
                p[rx, ry] = GoldLight;
                p[rx + 1, ry] = Gold;
                p[rx, ry - 1] = Gold;
                p[rx + 1, ry - 1] = GoldDark;
            }

            return p;
        }

        /// <summary>Case creusée 24×24 (bord de 8 px) : liseré, ombre intérieure en haut, fond bleu-vert.</summary>
        public static Color[,] Slot()
        {
            const int size = 24;
            var p = new Color[size, size];
            Color rim = C(0.30f, 0.42f, 0.54f);
            Color rimLight = C(0.46f, 0.60f, 0.72f);
            Color fill = C(0.12f, 0.20f, 0.27f);
            Color shade = C(0.07f, 0.12f, 0.17f);
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float d = Inside(x, y, size, size, 4f);
                    if (d < 0f) continue;

                    Color c;
                    if (d < 1f) c = Ink;
                    else if (d < 2f) c = y < size / 2 ? rimLight : rim; // le bas est éclairé : effet creusé
                    else if (y >= size - 4 && d < 4f) c = shade;        // ombre intérieure sous le bord haut
                    else c = fill;
                    p[x, y] = c;
                }
            }

            return p;
        }

        /// <summary>Bandeau de titre doré 40×16 (bords 10/7) : biseau, dégradé, deux points de couture.</summary>
        public static Color[,] Plaque()
        {
            const int width = 40, height = 16;
            var p = new Color[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float d = Inside(x, y, width, height, 4f);
                    if (d < 0f) continue;

                    Color c;
                    if (d < 1f) c = C(0.30f, 0.18f, 0.06f);
                    else if (d < 2f) c = y > height / 2 ? C(1.00f, 0.95f, 0.70f) : GoldDark;
                    else c = Color.Lerp(Gold, GoldLight, (y - 2f) / (height - 4f));
                    p[x, y] = c;
                }
            }

            p[4, height / 2] = GoldDark;
            p[width - 5, height / 2] = GoldDark;
            return p;
        }

        /// <summary>Fond d'eau 32×32 (bord de 8 px) : coins arrondis, surface claire, dégradé, fond de sable.</summary>
        public static Color[,] WaterBackdrop()
        {
            const int size = 32;
            var p = new Color[size, size];
            Color deep = C(0.10f, 0.36f, 0.52f);
            Color shallow = C(0.30f, 0.66f, 0.76f);
            Color surface = C(0.70f, 0.92f, 0.95f);
            Color sand = C(0.85f, 0.76f, 0.52f);
            Color sandDark = C(0.72f, 0.62f, 0.40f);
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float d = Inside(x, y, size, size, 4f);
                    if (d < 0f) continue;

                    Color c;
                    if (d < 1f) c = Ink;
                    else if (y >= size - 3) c = surface;                         // reflet de surface
                    else if (y <= 3) c = (x + y) % 3 == 0 ? sandDark : sand;      // fond de sable
                    else c = Color.Lerp(deep, shallow, (y - 4f) / (size - 8f));  // dégradé (s'étire sans perte)
                    p[x, y] = c;
                }
            }

            return p;
        }

        /// <summary>Pierre de rareté 9×9 en gris (teintée par la couleur de la rareté) avec reflet.</summary>
        public static Color[,] Gem()
        {
            const int size = 9;
            var p = new Color[size, size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int d = Mathf.Abs(x - 4) + Mathf.Abs(y - 4);
                    if (d > 4) continue;
                    if (d == 4) { p[x, y] = C(0.25f, 0.25f, 0.25f); continue; }
                    bool upper = y > 4 || (y == 4 && x < 4);
                    p[x, y] = upper ? C(0.95f, 0.95f, 0.95f) : C(0.70f, 0.70f, 0.70f);
                }
            }

            p[3, 5] = Color.white;
            p[3, 6] = Color.white;
            p[2, 5] = Color.white;
            return p;
        }

        /// <summary>Halo doux 64×64 (dégradé radial, filtre lissé) à teinter selon la rareté.</summary>
        public static Color[,] Glow()
        {
            const int size = 64;
            var p = new Color[size, size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                    float dy = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                    float t = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    p[x, y] = new Color(1f, 1f, 1f, t * t);
                }
            }

            return p;
        }
    }
}
