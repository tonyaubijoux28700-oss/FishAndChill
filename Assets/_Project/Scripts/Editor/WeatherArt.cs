using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Dessins de la météo, à la résolution du jeu (16 px par case) : la goutte de pluie (trait fin qui s'éclaircit vers
    /// le bas), son éclaboussure (petits ronds en vue plongeante qui s'ouvrent) et une nappe de brume (tache douce tramée,
    /// pour garder le pixel art). Blancs : la couleur et l'opacité viennent du jeu. Tableaux [x, y], y depuis le bas.
    /// </summary>
    public static class WeatherArt
    {
        public const int PixelsPerUnit = 16;
        public const int SplashFrames = 3;

        /// <summary>Matrice de Bayer 4×4 : seuils de trame.</summary>
        private static readonly float[,] Bayer =
        {
            { 0f / 16f, 8f / 16f, 2f / 16f, 10f / 16f },
            { 12f / 16f, 4f / 16f, 14f / 16f, 6f / 16f },
            { 3f / 16f, 11f / 16f, 1f / 16f, 9f / 16f },
            { 15f / 16f, 7f / 16f, 13f / 16f, 5f / 16f },
        };

        private static float Hash(int x, int y) => Mathf.Repeat(Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f, 1f);

        private static float Noise(float x, float y)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            return Mathf.Lerp(Mathf.Lerp(Hash(ix, iy), Hash(ix + 1, iy), fx), Mathf.Lerp(Hash(ix, iy + 1), Hash(ix + 1, iy + 1), fx), fy);
        }

        /// <summary>Goutte : 2×9 px, un peu en biais (comme sa chute), plus nette en bas.</summary>
        public static Color[,] Drop()
        {
            var p = new Color[3, 9];
            for (int y = 0; y < 9; y++)
            {
                int x = y < 4 ? 1 : 0;   // le haut est décalé vers la droite : la pluie vient de là
                float a = Mathf.Lerp(1f, 0.25f, y / 8f);
                p[x, y] = new Color(1f, 1f, 1f, a);
                if (y == 0) p[x + 1, y] = new Color(1f, 1f, 1f, 0.5f);
            }

            return p;
        }

        /// <summary>Éclaboussure : une ellipse fine qui s'élargit, et deux gouttelettes au début.</summary>
        public static Color[,] Splash(int frame)
        {
            int w = 5 + frame * 4, h = 3 + frame * 2;
            var p = new Color[w, h + 2];
            float cx = w * 0.5f, cy = h * 0.5f;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    float u = (x + 0.5f - cx) / cx, v = (y + 0.5f - cy) / cy;
                    float r = u * u + v * v;
                    if (r <= 1f && r > (frame == 0 ? 0f : 0.45f))
                    {
                        p[x, y] = Color.white;
                    }
                }
            }

            if (frame == 0)
            {
                p[0, h + 1] = Color.white;
                p[w - 1, h] = Color.white;
            }

            return p;
        }

        /// <summary>Nappe de brume : 160×72 px, bords irréguliers et tramés.</summary>
        public static Color[,] Fog()
        {
            const int w = 160, h = 72;
            var p = new Color[w, h];
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    float u = (x + 0.5f) / w * 2f - 1f, v = (y + 0.5f) / h * 2f - 1f;
                    float shape = 1f - Mathf.Sqrt(u * u + v * v) + (Noise(x / 14f, y / 10f) - 0.5f) * 0.6f;
                    float density = Mathf.Clamp01(shape * 1.6f) * (0.6f + 0.4f * Noise(x / 6f + 30f, y / 5f));
                    // Trois niveaux d'opacité, tramés.
                    float level = Mathf.Floor(density * 3f + Bayer[x % 4, y % 4]) / 3f;
                    if (level > 0f)
                    {
                        p[x, y] = new Color(1f, 1f, 1f, level);
                    }
                }
            }

            return p;
        }
    }
}
