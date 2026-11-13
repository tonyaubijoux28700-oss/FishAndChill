using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Tuiles détaillées de l'île tropicale (16×16, même taille de pixel que les poissons des cartes),
    /// et cocotier 32×40. Tableaux [x, y], y = 0 en bas. Les motifs sont périodiques sur 16 pixels
    /// pour que les tuiles se raccordent sans couture.
    /// </summary>
    public static class TileArt
    {
        public const int Size = 16;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        // ---------- Eau ----------

        /// <summary>Eau du large : bandes de houle ondulées, crêtes claires, quelques reflets.</summary>
        public static Color[,] DeepWater(int variant)
        {
            Color deep = C(0.12f, 0.50f, 0.64f);
            Color mid = C(0.16f, 0.58f, 0.70f);
            Color crest = C(0.48f, 0.82f, 0.88f);
            Color sparkle = C(0.92f, 0.98f, 1.00f);
            var p = new Color[Size, Size];
            int phase = variant * 5;

            for (int x = 0; x < Size; x++)
            {
                // Ondulation périodique sur 16 pixels.
                float wave = Mathf.Sin((x + phase) * Mathf.PI * 2f / Size) * 1.5f;
                for (int y = 0; y < Size; y++)
                {
                    float band = Mathf.Repeat(y + wave + phase, 8f);
                    Color c = band < 3.5f ? deep : mid;
                    if (band >= 6.5f && band < 7.5f && ((x + phase) % 8) < 5)
                    {
                        c = crest;
                    }

                    p[x, y] = c;
                }
            }

            p[(3 + phase) % Size, (12 + variant) % Size] = sparkle;
            p[(11 + phase) % Size, (5 + variant * 3) % Size] = sparkle;
            return p;
        }

        /// <summary>Lagon peu profond : turquoise clair, fond de sable deviné, écume.</summary>
        public static Color[,] ShallowWater(int variant)
        {
            Color baseColor = C(0.30f, 0.76f, 0.79f);
            Color light = C(0.44f, 0.85f, 0.85f);
            Color sandy = C(0.52f, 0.84f, 0.78f);
            Color foam = C(0.94f, 0.99f, 0.99f);
            var p = new Color[Size, Size];
            var random = new System.Random(500 + variant);

            for (int x = 0; x < Size; x++)
            {
                float wave = Mathf.Sin((x + variant * 4) * Mathf.PI * 2f / Size) * 1.2f;
                for (int y = 0; y < Size; y++)
                {
                    float band = Mathf.Repeat(y + wave, 8f);
                    Color c = band < 4f ? baseColor : light;
                    if (random.NextDouble() < 0.08)
                    {
                        c = sandy;
                    }

                    p[x, y] = c;
                }
            }

            // Petits moutons d'écume.
            for (int i = 0; i < 3 + variant; i++)
            {
                int fx = random.Next(1, Size - 3);
                int fy = random.Next(1, Size - 1);
                p[fx, fy] = foam;
                p[fx + 1, fy] = foam;
                p[fx + 2, fy] = Color.Lerp(foam, light, 0.5f);
            }

            return p;
        }

        // ---------- Sable ----------

        /// <summary>Sable : ondulations obliques, grains ; variante 1 = coquillage, 2 = étoile de mer.</summary>
        public static Color[,] Sand(int variant)
        {
            Color baseColor = C(0.95f, 0.87f, 0.66f);
            Color shade = C(0.89f, 0.79f, 0.57f);
            Color light = C(0.99f, 0.94f, 0.79f);
            var p = new Color[Size, Size];
            var random = new System.Random(700 + variant);

            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    int ripple = (x + y * 2) % 8;
                    Color c = ripple == 0 ? shade : ripple == 1 ? light : baseColor;
                    double roll = random.NextDouble();
                    if (roll < 0.05)
                    {
                        c = shade;
                    }
                    else if (roll < 0.09)
                    {
                        c = light;
                    }

                    p[x, y] = c;
                }
            }

            if (variant == 1)
            {
                // Coquillage rose nacré.
                Color shell = C(0.98f, 0.80f, 0.78f);
                Color shellDark = C(0.85f, 0.58f, 0.58f);
                PixelArt.Rect(p, 6, 6, 8, 7, shell);
                PixelArt.Set(p, 7, 8, shell);
                PixelArt.Set(p, 7, 6, shellDark);
                PixelArt.Set(p, 6, 5, shellDark);
                PixelArt.Set(p, 8, 5, shellDark);
            }
            else if (variant == 2)
            {
                // Étoile de mer orange.
                Color star = C(0.95f, 0.50f, 0.25f);
                Color starDark = C(0.78f, 0.35f, 0.18f);
                int cx = 9, cy = 9;
                PixelArt.Set(p, cx, cy, starDark);
                PixelArt.Set(p, cx, cy + 1, star); PixelArt.Set(p, cx, cy + 2, star);
                PixelArt.Set(p, cx - 1, cy, star); PixelArt.Set(p, cx - 2, cy + 1, star);
                PixelArt.Set(p, cx + 1, cy, star); PixelArt.Set(p, cx + 2, cy + 1, star);
                PixelArt.Set(p, cx - 1, cy - 1, star); PixelArt.Set(p, cx - 2, cy - 2, star);
                PixelArt.Set(p, cx + 1, cy - 1, star); PixelArt.Set(p, cx + 2, cy - 2, star);
            }

            return p;
        }

        // ---------- Herbe ----------

        /// <summary>Herbe : touffes en V ; variante 1 = hibiscus rouges, 2 = frangipaniers blancs.</summary>
        public static Color[,] Grass(int variant)
        {
            Color baseColor = C(0.33f, 0.65f, 0.31f);
            Color dark = C(0.25f, 0.54f, 0.26f);
            Color light = C(0.46f, 0.76f, 0.38f);
            var p = new Color[Size, Size];
            var random = new System.Random(900 + variant);

            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    double roll = random.NextDouble();
                    p[x, y] = roll < 0.10 ? dark : roll < 0.16 ? light : baseColor;
                }
            }

            // Touffes d'herbe : base sombre, brins clairs.
            int[,] tufts = { { 2, 3 }, { 10, 2 }, { 6, 9 }, { 13, 11 }, { 3, 13 } };
            for (int i = 0; i < tufts.GetLength(0); i++)
            {
                int tx = (tufts[i, 0] + variant * 3) % Size;
                int ty = tufts[i, 1];
                PixelArt.Set(p, tx, ty, dark);
                PixelArt.Set(p, tx + 1, ty, dark);
                PixelArt.Set(p, tx - 1, ty + 1, light);
                PixelArt.Set(p, tx + 2, ty + 1, light);
                PixelArt.Set(p, tx, ty + 1, dark);
                PixelArt.Set(p, tx + 1, ty + 2, light);
            }

            if (variant == 1)
            {
                Flower(p, 9, 7, C(0.92f, 0.22f, 0.28f), C(0.98f, 0.85f, 0.30f));
            }
            else if (variant == 2)
            {
                Flower(p, 5, 10, C(0.98f, 0.97f, 0.92f), C(0.98f, 0.82f, 0.30f));
            }

            return p;
        }

        private static void Flower(Color[,] p, int x, int y, Color petal, Color heart)
        {
            PixelArt.Set(p, x, y + 1, petal);
            PixelArt.Set(p, x, y - 1, petal);
            PixelArt.Set(p, x - 1, y, petal);
            PixelArt.Set(p, x + 1, y, petal);
            PixelArt.Set(p, x, y, heart);
        }

        // ---------- Cocotier ----------

        public const int PalmWidth = 32;
        public const int PalmHeight = 40;

        /// <summary>Pied du tronc (en pixels) : placé au centre de la case.</summary>
        public static readonly Vector2 PalmPivot = new Vector2(16f / PalmWidth, 5f / PalmHeight);

        /// <summary>Cocotier vu de 3/4 : ombre au sol, tronc courbé annelé, couronne de palmes, noix de coco.</summary>
        public static Color[,] Palm()
        {
            var p = new Color[PalmWidth, PalmHeight];
            Color shadow = C(0.05f, 0.18f, 0.08f, 0.35f);
            Color trunk = C(0.58f, 0.42f, 0.25f);
            Color trunkDark = C(0.42f, 0.29f, 0.17f);
            Color trunkLight = C(0.72f, 0.56f, 0.36f);
            Color leaf = C(0.20f, 0.56f, 0.24f);
            Color leafDark = C(0.12f, 0.40f, 0.17f);
            Color leafLight = C(0.38f, 0.72f, 0.34f);
            Color coconut = C(0.40f, 0.26f, 0.14f);

            // Ombre portée au sol (ellipse semi-transparente).
            for (int x = 0; x < PalmWidth; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    float dx = (x - 17.5f) / 10f;
                    float dy = (y - 4.5f) / 3.2f;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        p[x, y] = shadow;
                    }
                }
            }

            // Tronc courbé de (16, 5) à (19, 27), 3 pixels de large, anneaux tous les 3 pixels.
            for (int y = 5; y <= 27; y++)
            {
                float t = (y - 5) / 22f;
                int cx = Mathf.RoundToInt(16f + t * t * 3.5f);
                int width = y < 9 ? 2 : 1;
                for (int x = cx - width; x <= cx + 1; x++)
                {
                    Color c = x == cx - width ? trunkDark : x == cx + 1 ? trunkLight : trunk;
                    if ((y - 5) % 3 == 0)
                    {
                        c = Color.Lerp(c, trunkDark, 0.6f);
                    }

                    p[x, y] = c;
                }
            }

            // Couronne : palmes qui partent du sommet et retombent.
            var crown = new Vector2(19.5f, 28.5f);
            float[] angles = { 15f, 50f, 90f, 130f, 165f, 205f, 335f };
            float[] lengths = { 12f, 10f, 8f, 10f, 12f, 9f, 9f };
            for (int f = 0; f < angles.Length; f++)
            {
                float rad = angles[f] * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                for (float s = 0f; s <= lengths[f]; s += 0.35f)
                {
                    float t = s / lengths[f];
                    // Les palmes retombent avec la gravité.
                    Vector2 point = crown + direction * s + Vector2.down * (t * t * 4.5f);
                    float half = Mathf.Lerp(1.6f, 0.4f, t);
                    var side = new Vector2(-direction.y, direction.x);
                    for (float w = -half; w <= half; w += 0.5f)
                    {
                        Vector2 q = point + side * w;
                        int px = Mathf.RoundToInt(q.x);
                        int py = Mathf.RoundToInt(q.y);
                        Color c = w > half * 0.3f ? leafLight : w < -half * 0.3f ? leafDark : leaf;
                        PixelArt.Set(p, px, py, c);
                    }
                }
            }

            // Noix de coco sous la couronne.
            PixelArt.Rect(p, 18, 26, 19, 27, coconut);
            PixelArt.Rect(p, 20, 25, 21, 26, coconut);
            PixelArt.Set(p, 18, 27, Color.Lerp(coconut, Color.white, 0.25f));

            // Contour sombre, sans contourner l'ombre portée.
            OutlineOpaque(p, C(0.06f, 0.18f, 0.09f));
            return p;
        }

        /// <summary>Comme PixelArt.Outline, mais ignore les pixels semi-transparents (ombres).</summary>
        private static void OutlineOpaque(Color[,] p, Color color)
        {
            int width = p.GetLength(0);
            int height = p.GetLength(1);
            var outline = new bool[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (Opaque(p, x, y))
                    {
                        continue;
                    }

                    if (Opaque(p, x + 1, y) || Opaque(p, x - 1, y) || Opaque(p, x, y + 1) || Opaque(p, x, y - 1))
                    {
                        outline[x, y] = true;
                    }
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (outline[x, y])
                    {
                        p[x, y] = color;
                    }
                }
            }
        }

        private static bool Opaque(Color[,] p, int x, int y)
        {
            return x >= 0 && y >= 0 && x < p.GetLength(0) && y < p.GetLength(1) && p[x, y].a > 0.9f;
        }
    }
}
