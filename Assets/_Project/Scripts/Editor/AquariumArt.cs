using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Bâtiment de l'aquarium en pixel art 48×44 (3 cases de large), vu de 3/4 : toit turquoise, façade crème,
    /// deux grandes vitres avec poissons, algues et bulles, porte vitrée, enseigne en bois au poisson.
    /// Pivot au sol, au milieu de la façade.
    /// </summary>
    public static class AquariumArt
    {
        public const int Width = 48;
        public const int Height = 44;
        public static readonly Vector2 Pivot = new Vector2(0.5f, 2f / Height);

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        public static Color[,] Draw()
        {
            var p = new Color[Width, Height];

            Color shadow = C(0.05f, 0.15f, 0.08f, 0.35f);
            Color wall = C(0.94f, 0.92f, 0.84f);
            Color wallShade = C(0.84f, 0.80f, 0.70f);
            Color stone = C(0.66f, 0.66f, 0.62f);
            Color stoneDark = C(0.52f, 0.52f, 0.50f);
            Color frame = C(0.12f, 0.36f, 0.40f);
            Color roof = C(0.16f, 0.62f, 0.64f);
            Color roofDark = C(0.10f, 0.46f, 0.50f);
            Color roofLight = C(0.36f, 0.78f, 0.76f);
            Color wood = C(0.56f, 0.38f, 0.22f);
            Color woodDark = C(0.40f, 0.26f, 0.15f);
            Color door = C(0.25f, 0.50f, 0.62f);

            // Ombre au sol.
            PixelArt.Rect(p, 1, 0, 46, 2, shadow);

            // Façade.
            PixelArt.Rect(p, 2, 2, 45, 25, wall);
            PixelArt.Rect(p, 2, 2, 3, 25, wallShade);
            PixelArt.Rect(p, 44, 2, 45, 25, wallShade);
            PixelArt.Rect(p, 2, 2, 45, 4, stone);
            for (int x = 3; x < 45; x += 4)
            {
                PixelArt.Set(p, x, 3, stoneDark);
            }

            // Deux grandes vitres d'aquarium.
            DrawTank(p, 5, 8, 18, 21, frame, seed: 1);
            DrawTank(p, 29, 8, 42, 21, frame, seed: 2);

            // Porte vitrée au centre.
            PixelArt.Rect(p, 20, 5, 27, 17, frame);
            PixelArt.Rect(p, 21, 5, 26, 16, door);
            PixelArt.Rect(p, 22, 12, 22, 15, C(0.55f, 0.80f, 0.90f));
            PixelArt.Rect(p, 23, 5, 24, 16, Color.Lerp(door, frame, 0.4f));
            PixelArt.Set(p, 25, 10, C(0.95f, 0.80f, 0.35f)); // poignée

            // Toit en tuiles, vu de dessus en 3/4, qui déborde de la façade.
            for (int y = 26; y <= 41; y++)
            {
                int inset = (y - 26) / 3;
                for (int x = inset; x < Width - inset; x++)
                {
                    Color c = (y - 26) % 3 == 0 ? roofDark : roof;
                    if ((y - 26) % 3 == 2 && (x + y) % 5 == 0)
                    {
                        c = roofLight;
                    }

                    p[x, y] = c;
                }
            }

            PixelArt.Rect(p, 0, 26, Width - 1, 26, roofDark);
            PixelArt.Rect(p, 6, 41, Width - 7, 41, roofLight);

            // Enseigne en bois avec un poisson orange.
            PixelArt.Rect(p, 15, 22, 32, 28, wood);
            PixelArt.Rect(p, 15, 22, 32, 22, woodDark);
            PixelArt.Rect(p, 15, 28, 32, 28, woodDark);
            DrawSignFish(p, 24, 25);

            OutlineOpaque(p, C(0.10f, 0.14f, 0.14f));
            return p;
        }

        private static void DrawTank(Color[,] p, int x0, int y0, int x1, int y1, Color frame, int seed)
        {
            var random = new System.Random(seed);
            PixelArt.Rect(p, x0 - 1, y0 - 1, x1 + 1, y1 + 1, frame);
            for (int y = y0; y <= y1; y++)
            {
                float t = Mathf.InverseLerp(y0, y1, y);
                Color water = Color.Lerp(C(0.14f, 0.46f, 0.66f), C(0.36f, 0.76f, 0.86f), t);
                for (int x = x0; x <= x1; x++)
                {
                    p[x, y] = water;
                }
            }

            // Sable et algues au fond.
            PixelArt.Rect(p, x0, y0, x1, y0, C(0.90f, 0.82f, 0.60f));
            for (int i = 0; i < 3; i++)
            {
                int ax = x0 + 2 + random.Next(0, x1 - x0 - 3);
                int height = 3 + random.Next(0, 4);
                for (int k = 1; k <= height; k++)
                {
                    PixelArt.Set(p, ax + ((k / 2) % 2), y0 + k, C(0.20f, 0.62f, 0.30f));
                }
            }

            // Deux poissons et des bulles.
            DrawTinyFish(p, x0 + 3 + random.Next(0, 3), y0 + 7, C(1.00f, 0.55f, 0.20f), facingRight: true);
            DrawTinyFish(p, x1 - 6 - random.Next(0, 2), y0 + 4, C(0.98f, 0.88f, 0.30f), facingRight: false);
            PixelArt.Set(p, x1 - 2, y1 - 3, C(0.85f, 0.95f, 1.00f));
            PixelArt.Set(p, x1 - 3, y1 - 1, C(0.85f, 0.95f, 1.00f));

            // Reflet sur la vitre.
            for (int k = 0; k < 4; k++)
            {
                PixelArt.Set(p, x0 + 1 + k, y1 - 1 - k, Color.Lerp(p[x0 + 1 + k, y1 - 1 - k], Color.white, 0.5f));
            }
        }

        private static void DrawTinyFish(Color[,] p, int x, int y, Color color, bool facingRight)
        {
            int dir = facingRight ? 1 : -1;
            PixelArt.Set(p, x, y, color);
            PixelArt.Set(p, x + dir, y, color);
            PixelArt.Set(p, x + 2 * dir, y, color);
            PixelArt.Set(p, x + dir, y + 1, color);
            PixelArt.Set(p, x + dir, y - 1, color);
            PixelArt.Set(p, x - dir, y + 1, color); // queue
            PixelArt.Set(p, x - dir, y - 1, color);
            PixelArt.Set(p, x + 2 * dir, y + 1, C(0.05f, 0.05f, 0.05f)); // œil
        }

        private static void DrawSignFish(Color[,] p, int cx, int cy)
        {
            Color orange = C(1.00f, 0.58f, 0.20f);
            PixelArt.Rect(p, cx - 3, cy - 1, cx + 2, cy + 1, orange);
            PixelArt.Set(p, cx + 3, cy, orange);
            PixelArt.Rect(p, cx - 5, cy - 2, cx - 4, cy - 2, orange);
            PixelArt.Rect(p, cx - 5, cy + 2, cx - 4, cy + 2, orange);
            PixelArt.Set(p, cx - 4, cy, orange);
            PixelArt.Set(p, cx + 1, cy + 1, C(0.05f, 0.05f, 0.05f));
            PixelArt.Set(p, cx - 1, cy, C(1.00f, 0.95f, 0.85f));
        }

        private static void OutlineOpaque(Color[,] p, Color color)
        {
            var outline = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Opaque(p, x, y)) continue;
                    if (Opaque(p, x + 1, y) || Opaque(p, x - 1, y) || Opaque(p, x, y + 1) || Opaque(p, x, y - 1))
                    {
                        outline[x, y] = true;
                    }
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (outline[x, y]) p[x, y] = color;
                }
            }
        }

        private static bool Opaque(Color[,] p, int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height && p[x, y].a > 0.9f;
        }
    }
}
