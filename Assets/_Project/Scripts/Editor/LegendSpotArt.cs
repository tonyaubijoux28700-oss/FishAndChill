using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Dessins du remous légendaire, à la résolution du jeu (16 px par case) pour rester nets, sans agrandissement :
    /// le remous (spirale sombre qui tourne, bras d'écume, bord tramé) en plusieurs images, et les ondes qui s'élargissent
    /// (une ellipse fine par taille, en vue plongeante comme les ronds dans l'eau). Tableaux [x, y], y depuis le bas.
    /// </summary>
    public static class LegendSpotArt
    {
        public const int PixelsPerUnit = 16;
        public const int EddyWidth = 72;
        public const int EddyHeight = 40;
        public const int EddyFrames = 6;
        public const int WaveFrames = 10;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static float Hash(int x, int y) => Mathf.Repeat(Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f, 1f);

        /// <summary>Une image du remous : la spirale a tourné de <paramref name="frame"/> / <see cref="EddyFrames"/> de bras.</summary>
        public static Color[,] Eddy(int frame)
        {
            var p = new Color[EddyWidth, EddyHeight];
            float cx = EddyWidth * 0.5f, cy = EddyHeight * 0.5f;
            for (int x = 0; x < EddyWidth; x++)
            {
                for (int y = 0; y < EddyHeight; y++)
                {
                    float u = (x + 0.5f - cx) / cx, v = (y + 0.5f - cy) / cy;
                    float r = Mathf.Sqrt(u * u + v * v);
                    if (r > 1f)
                    {
                        continue;
                    }

                    // Bord tramé : le remous se fond dans l'eau.
                    if (r > 0.86f && (x + y) % 2 == 0)
                    {
                        continue;
                    }

                    float a = Mathf.Atan2(v, u) / (Mathf.PI * 2f);
                    float arm = Mathf.Repeat(a * 3f + r * 1.7f + frame / (float)EddyFrames, 1f);
                    Color c = Color.Lerp(C(0.02f, 0.06f, 0.13f), C(0.07f, 0.18f, 0.30f), Mathf.SmoothStep(0f, 1f, r));
                    if (arm < 0.16f && r > 0.18f)
                    {
                        c = r > 0.72f && arm < 0.07f ? C(0.70f, 0.86f, 0.92f) : C(0.18f, 0.36f, 0.48f);   // bras, écume au bord
                    }
                    else if (arm > 0.5f && arm < 0.6f && r > 0.3f)
                    {
                        c = Color.Lerp(c, C(0.01f, 0.03f, 0.07f), 0.6f);                                   // creux entre deux bras
                    }

                    if (r > 0.6f && Hash(x * 7 + frame, y * 3) < 0.03f)
                    {
                        c = C(0.86f, 0.94f, 1f);                                                             // éclats d'écume
                    }

                    c.a = r < 0.3f ? 0.95f : Mathf.Lerp(0.95f, 0.7f, (r - 0.3f) / 0.7f);
                    p[x, y] = c;
                }
            }

            return p;
        }

        /// <summary>Une onde : ellipse fine (largeur 2 × hauteur), de plus en plus grande avec <paramref name="size"/>.</summary>
        public static Color[,] Wave(int size)
        {
            int w = 28 + size * 10;
            int h = w / 2;
            var p = new Color[w, h];
            float cx = w * 0.5f, cy = h * 0.5f;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    float u = (x + 0.5f - cx) / cx, v = (y + 0.5f - cy) / cy;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float edge = 1f / cy;   // un pixel d'épaisseur, là où l'ellipse est la plus fine
                    if (r <= 1f && r > 1f - edge * 1.2f && Hash(Mathf.RoundToInt(Mathf.Atan2(v, u) * 9f), size) > 0.12f)
                    {
                        p[x, y] = Color.white;
                    }
                }
            }

            return p;
        }
    }
}
