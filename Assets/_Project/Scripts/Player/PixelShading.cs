using UnityEngine;

namespace FishAndChill.Player
{
    /// <summary>
    /// Outils d'éclairage du pixel art utilisables en jeu (le pêcheur est redessiné à la volée selon son apparence) :
    /// paliers d'ombre froide et de lumière chaude, contour sélectif teinté, miroir. Les outils de l'éditeur
    /// (<c>DetailedFish.Shade</c>, <c>PixelArt.SelectiveOutline</c>) passent par ici.
    /// Tableaux [x, y] avec y compté depuis le bas.
    /// </summary>
    public static class PixelShading
    {
        /// <summary>Couleur éclairée : &lt; 1 vers une ombre froide, &gt; 1 vers une lumière chaude.</summary>
        public static Color Shade(Color c, float light)
        {
            if (light < 1f)
            {
                var shadow = new Color(c.r * 0.36f, c.g * 0.40f, c.b * 0.55f + 0.05f, c.a);
                return Color.Lerp(shadow, c, Mathf.InverseLerp(0.40f, 1f, light));
            }

            var warm = new Color(Mathf.Min(1f, c.r * 1.32f + 0.07f), Mathf.Min(1f, c.g * 1.28f + 0.06f), Mathf.Min(1f, c.b * 1.12f + 0.03f), c.a);
            return Color.Lerp(c, warm, Mathf.Clamp01((light - 1f) / 0.25f));
        }

        public static bool IsFilled(Color[,] pixels, int x, int y)
        {
            return x >= 0 && y >= 0 && x < pixels.GetLength(0) && y < pixels.GetLength(1) && pixels[x, y].a > 0.01f;
        }

        /// <summary>
        /// Contour sélectif : chaque pixel vide voisin d'une forme prend une version sombre et teintée de ses voisins,
        /// plus claire du côté éclairé (dessus des formes).
        /// </summary>
        public static void SelectiveOutline(Color[,] pixels, Color color, float ink = 0.45f)
        {
            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);
            var outline = new Color[width, height];
            var has = new bool[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (IsFilled(pixels, x, y))
                    {
                        continue;
                    }

                    Color sum = Color.clear;
                    int count = 0;
                    AddNeighbour(pixels, x + 1, y, ref sum, ref count);
                    AddNeighbour(pixels, x - 1, y, ref sum, ref count);
                    AddNeighbour(pixels, x, y + 1, ref sum, ref count);
                    AddNeighbour(pixels, x, y - 1, ref sum, ref count);
                    if (count == 0)
                    {
                        continue;
                    }

                    Color n = sum / count;
                    bool litSide = IsFilled(pixels, x, y - 1) && !IsFilled(pixels, x, y + 1);
                    Color dark = litSide
                        ? new Color(n.r * 0.50f, n.g * 0.52f, n.b * 0.60f)
                        : new Color(n.r * 0.28f, n.g * 0.30f, n.b * 0.40f);
                    Color o = Color.Lerp(dark, color, litSide ? ink * 0.45f : ink);
                    o.a = 1f;
                    outline[x, y] = o;
                    has[x, y] = true;
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (has[x, y])
                    {
                        pixels[x, y] = outline[x, y];
                    }
                }
            }
        }

        public static Color[,] MirrorX(Color[,] pixels)
        {
            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);
            var result = new Color[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    result[width - 1 - x, y] = pixels[x, y];
                }
            }

            return result;
        }

        private static void AddNeighbour(Color[,] pixels, int x, int y, ref Color sum, ref int count)
        {
            if (IsFilled(pixels, x, y))
            {
                sum += pixels[x, y];
                count++;
            }
        }
    }
}
