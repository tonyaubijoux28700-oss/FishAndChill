using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Pixel art du phare (16 px par case, y depuis le bas) : 32×96 (2 cases de large, 6 de haut), sur un socle de
    /// rochers ; tour rayée rouge et blanc qui s'affine, galerie à balustrade, lanterne vitrée, dôme rouge. Icône 16×16.
    /// </summary>
    public static class LighthouseArt
    {
        public const int Width = 32;
        public const int Height = 96;
        public static readonly Vector2 Pivot = new Vector2(0.5f, 4f / Height);

        /// <summary>Centre de la lanterne, en unités au-dessus du pivot.</summary>
        public static readonly Vector2 LanternOffset = new Vector2(0f, (77f - 4f) / 16f);

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Red = C(0.86f, 0.22f, 0.22f);
        private static readonly Color White = C(0.97f, 0.96f, 0.93f);
        private static readonly Color Dark = C(0.22f, 0.22f, 0.26f);
        private static readonly Color Rock = C(0.56f, 0.54f, 0.52f);

        public static Color[,] Exterior()
        {
            var p = new Color[Width, Height];

            // Ombre et socle de rochers.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    float u = (x - 15.5f) / 15f, v = (y - 2f) / 3f;
                    if (u * u + v * v <= 1f) p[x, y] = C(0.05f, 0.14f, 0.10f, 0.35f);
                }
            }

            foreach ((float cx, float cy, float rx, float ry) in new[] { (9f, 7f, 7f, 5f), (22f, 7f, 7.5f, 5f), (15.5f, 10f, 9f, 4.5f) })
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 2; y < 16; y++)
                    {
                        float u = (x - cx) / rx, v = (y - cy) / ry;
                        if (u * u + v * v <= 1f)
                        {
                            p[x, y] = v > 0.4f ? C(0.70f, 0.68f, 0.66f) : u > 0.5f ? C(0.44f, 0.42f, 0.42f) : Rock;
                        }
                    }
                }
            }

            // Tour : rayures rouges et blanches, éclairée à gauche, plus étroite en haut.
            for (int y = 12; y <= 66; y++)
            {
                float t = (y - 12) / 54f;
                float half = Mathf.Lerp(10.5f, 6.5f, t);
                bool redBand = ((y - 12) / 9) % 2 == 0;
                Color baseC = redBand ? Red : White;
                for (int x = 0; x < Width; x++)
                {
                    float u = (x + 0.5f - 16f) / half;
                    if (Mathf.Abs(u) > 1f) continue;
                    float light = u < -0.55f ? 1.12f : u > 0.45f ? 0.72f : 1f;
                    p[x, y] = Shade(baseC, light);
                }
            }

            // Porte et petites fenêtres.
            PixelArt.Rect(p, 13, 12, 18, 22, C(0.36f, 0.22f, 0.14f));
            PixelArt.Rect(p, 14, 13, 17, 21, C(0.52f, 0.34f, 0.20f));
            PixelArt.Set(p, 17, 17, C(1f, 0.82f, 0.30f));
            PixelArt.Rect(p, 15, 36, 16, 39, C(0.24f, 0.40f, 0.60f));
            PixelArt.Rect(p, 15, 54, 16, 57, C(0.24f, 0.40f, 0.60f));

            // Galerie et balustrade.
            PixelArt.Rect(p, 5, 66, 26, 68, Dark);
            for (int x = 6; x <= 25; x += 3)
            {
                PixelArt.Rect(p, x, 69, x, 72, Dark);
            }

            PixelArt.Rect(p, 5, 72, 26, 72, Dark);

            // Lanterne vitrée (allumée), montants.
            PixelArt.Rect(p, 9, 69, 22, 84, Dark);
            for (int x = 10; x <= 21; x++)
            {
                for (int y = 70; y <= 83; y++)
                {
                    p[x, y] = Color.Lerp(C(1f, 0.92f, 0.55f), C(1f, 0.98f, 0.82f), 1f - Mathf.Abs(y - 77f) / 7f);
                }
            }

            PixelArt.Rect(p, 13, 70, 13, 83, Dark);
            PixelArt.Rect(p, 18, 70, 18, 83, Dark);

            // Dôme rouge et girouette.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 84; y <= 92; y++)
                {
                    float u = (x + 0.5f - 16f) / 8f, v = (y - 84f) / 8f;
                    if (u * u + v * v <= 1f) p[x, y] = u < -0.4f ? Shade(Red, 1.12f) : u > 0.4f ? Shade(Red, 0.72f) : Red;
                }
            }

            PixelArt.Rect(p, 15, 92, 16, 95, Dark);
            PixelArt.Outline(p, C(0.12f, 0.08f, 0.09f));
            return p;
        }

        public static Color[,] Icon()
        {
            var p = new Color[16, 16];
            for (int y = 1; y <= 10; y++)
            {
                int half = y < 5 ? 3 : 2;
                PixelArt.Rect(p, 8 - half, y, 7 + half, y, (y / 3) % 2 == 0 ? Red : White);
            }

            PixelArt.Rect(p, 5, 11, 10, 13, C(1f, 0.90f, 0.50f));
            PixelArt.Rect(p, 6, 14, 9, 14, Red);
            PixelArt.Outline(p, C(0.12f, 0.08f, 0.09f));
            return p;
        }

        private static Color Shade(Color c, float l) => FishAndChill.Player.PixelShading.Shade(c, l);
    }
}
