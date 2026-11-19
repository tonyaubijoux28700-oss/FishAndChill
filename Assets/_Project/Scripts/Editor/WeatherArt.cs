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

        public const int BoltWidth = 64;
        public const int BoltHeight = 240;
        public const int BoltVariants = 3;
        public const int ImpactFrames = 3;

        /// <summary>
        /// Un éclair de la tempête des boss, 64×240 px (15 cases de haut : il tombe du haut de l'écran) : tracé brisé qui
        /// descend jusqu'au point d'impact (en bas au milieu, le pivot), cœur blanc, halo, et deux ou trois branches.
        /// </summary>
        public static Color[,] Bolt(int variant)
        {
            var p = new Color[BoltWidth, BoltHeight];
            var random = new System.Random(911 + variant * 37);
            float Rand(float a, float b) => a + (float)random.NextDouble() * (b - a);

            var main = new System.Collections.Generic.List<Vector2>();
            var at = new Vector2(BoltWidth * 0.5f + Rand(-14f, 14f), BoltHeight - 1f);
            main.Add(at);
            while (at.y > 0f)
            {
                float step = Rand(7f, 16f);
                float pull = (BoltWidth * 0.5f - at.x) * Mathf.Clamp01(1f - at.y / BoltHeight) * 0.6f;   // finit au milieu
                at = new Vector2(Mathf.Clamp(at.x + Rand(-9f, 9f) + pull, 4f, BoltWidth - 5f), Mathf.Max(0f, at.y - step));
                main.Add(at);
            }

            main[main.Count - 1] = new Vector2(BoltWidth * 0.5f, 0f);
            DrawPath(p, main, 2.4f, 1f);

            int branches = 2 + variant % 2;
            for (int b = 0; b < branches; b++)
            {
                int from = Mathf.Clamp((int)(main.Count * Rand(0.2f, 0.6f)), 1, main.Count - 2);
                var branch = new System.Collections.Generic.List<Vector2> { main[from] };
                Vector2 q = main[from];
                float side = Rand(0f, 1f) < 0.5f ? -1f : 1f;
                int length = (int)Rand(3f, 6f);
                for (int k = 0; k < length; k++)
                {
                    q = new Vector2(Mathf.Clamp(q.x + side * Rand(3f, 9f), 1f, BoltWidth - 2f), Mathf.Max(0f, q.y - Rand(6f, 12f)));
                    branch.Add(q);
                }

                DrawPath(p, branch, 1.4f, 0.75f);
            }

            return p;
        }

        /// <summary>Trace un éclair : cœur blanc plein, halo plus large et transparent.</summary>
        private static void DrawPath(Color[,] p, System.Collections.Generic.List<Vector2> path, float halo, float strength)
        {
            int w = p.GetLength(0), h = p.GetLength(1);
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector2 a = path[i], b = path[i + 1];
                int steps = Mathf.CeilToInt(Vector2.Distance(a, b) * 2f) + 1;
                for (int s = 0; s <= steps; s++)
                {
                    Vector2 c = Vector2.Lerp(a, b, s / (float)steps);
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        for (int dy = -3; dy <= 3; dy++)
                        {
                            int x = Mathf.RoundToInt(c.x) + dx, y = Mathf.RoundToInt(c.y) + dy;
                            if (x < 0 || y < 0 || x >= w || y >= h) continue;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            float alpha = d <= 1.05f ? strength : d <= halo ? 0.75f * strength : d <= halo + 1.3f ? 0.3f * strength : 0f;
                            if (alpha > p[x, y].a) p[x, y] = new Color(1f, 1f, 1f, alpha);
                        }
                    }
                }
            }
        }

        /// <summary>Lueur au point d'impact : un éclat plein, puis un anneau qui s'élargit (vue plongeante, 48×24 px).</summary>
        public static Color[,] Impact(int frame)
        {
            const int w = 48, h = 24;
            var p = new Color[w, h];
            float radius = 0.6f + frame * 0.2f;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    float u = (x + 0.5f - w * 0.5f) / (w * 0.5f), v = (y + 0.5f - h * 0.5f) / (h * 0.5f);
                    float r = Mathf.Sqrt(u * u + v * v);
                    float a = frame == 0
                        ? Mathf.Clamp01(1f - r / radius) * 1.4f
                        : Mathf.Abs(r - radius) < 0.08f ? 0.9f - frame * 0.25f : r < radius ? 0.25f * (1f - r / radius) : 0f;
                    a = Mathf.Floor(Mathf.Clamp01(a) * 3f + Bayer[x % 4, y % 4]) / 3f;   // tramé, pixel art
                    if (a > 0f) p[x, y] = new Color(1f, 1f, 1f, a);
                }
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
