using System;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Raies de fond, vues de dessus (tête à droite), dans le style de la raie manta (<see cref="RayArt"/>) : disque
    /// plat bombé au milieu, éclairé par paliers d'après son relief, contour sélectif. Le relief vient de la distance
    /// au bord du disque, si bien qu'une seule recette sert à toutes les formes (rond, losange, ailes en faux).
    /// Dessins originaux d'après la forme et la livrée réelles de chaque espèce.
    /// </summary>
    public static class StingrayArt
    {
        private static Color C(float r, float g, float b) => new Color(r, g, b);

        private static readonly Vector3 LightDirection = new Vector3(-0.35f, 0.75f, 0.55f).normalized;

        private const int Width = 112;
        private const int Height = 84;
        private const float Cy = Height / 2f;

        private static float Step(float light) => Mathf.Round(light * 6f) / 6f;

        /// <summary>
        /// Dessine le disque : <paramref name="inside"/> dit si un point (x, y) est dans la raie, <paramref name="paint"/>
        /// donne sa couleur avant l'éclairage (motifs). <paramref name="ridge"/> : hauteur du dos le long de l'axe.
        /// </summary>
        private static void Disc(Color[,] p, Func<float, float, bool> inside, Func<int, int, Color> paint, float ridge = 0.6f, float ridgeFrom = 0f, float ridgeTo = Width)
        {
            var mask = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    mask[x, y] = inside(x + 0.5f, y + 0.5f);
                }
            }

            // Relief : la distance au bord (plat sur les ailes, bombé au centre), plus l'arête du dos.
            const int reach = 12;
            var height = new float[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (!mask[x, y]) continue;
                    float best = reach;
                    for (int dx = -reach; dx <= reach; dx++)
                    {
                        for (int dy = -reach; dy <= reach; dy++)
                        {
                            int qx = x + dx, qy = y + dy;
                            bool outside = qx < 0 || qy < 0 || qx >= Width || qy >= Height || !mask[qx, qy];
                            if (outside) best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dy * dy));
                        }
                    }

                    float axis = Mathf.Abs(y + 0.5f - Cy);
                    float spine = x >= ridgeFrom && x <= ridgeTo ? Mathf.Exp(-axis * axis / 30f) * ridge : 0f;
                    height[x, y] = Mathf.Sqrt(best / reach) * 0.9f + spine;
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (!mask[x, y]) continue;
                    float h(int qx, int qy) => qx < 0 || qy < 0 || qx >= Width || qy >= Height ? 0f : height[qx, qy];
                    float gx = h(x + 1, y) - h(x - 1, y);
                    float gy = h(x, y + 1) - h(x, y - 1);
                    var normal = new Vector3(-gx * 2.6f, -gy * 2.6f, 0.5f).normalized;
                    float light = 0.6f + 0.6f * Mathf.Max(0f, Vector3.Dot(normal, LightDirection));
                    p[x, y] = DetailedFish.Shade(paint(x, y), Step(light));
                }
            }
        }

        /// <summary>Queue qui s'affine, dessinée sous le disque, du point (x0) jusqu'au bout (x1), en ondulant un peu.</summary>
        private static void Tail(Color[,] p, float x0, float x1, float width0, float width1, Color color, float wave = 1.5f)
        {
            int steps = Mathf.CeilToInt(x0 - x1);
            for (int i = 0; i < steps; i++)
            {
                float t0 = i / (float)steps, t1 = (i + 1) / (float)steps;
                float ax = Mathf.Lerp(x0, x1, t0), bx = Mathf.Lerp(x0, x1, t1);
                float ay = Cy + Mathf.Sin(t0 * 5f) * wave * t0, by = Cy + Mathf.Sin(t1 * 5f) * wave * t1;
                CreatureArt.Limb(p, ax, ay, bx, by, Mathf.Lerp(width0, width1, t0), color);
            }
        }

        private static float TailY(float t, float wave = 1.5f) => Cy + Mathf.Sin(t * 5f) * wave * t;

        /// <summary>Yeux sur le dessus de la tête, et derrière chacun l'évent (spiracle) par où la raie respire.</summary>
        private static void Eyes(Color[,] p, float x, float spread, Color iris)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                int ex = Mathf.RoundToInt(x);
                int ey = Mathf.RoundToInt(Cy + side * spread);
                PixelArt.Set(p, ex, ey, iris);
                PixelArt.Set(p, ex + 1, ey, iris);
                PixelArt.Set(p, ex, ey + (side > 0 ? 1 : 0), C(0.02f, 0.02f, 0.03f));
                PixelArt.Set(p, ex + 1, ey + 1, C(0.90f, 0.92f, 0.94f));
                PixelArt.Set(p, ex - 3, ey, Color.Lerp(p[ex - 3, ey], Color.black, 0.45f));
                PixelArt.Set(p, ex - 4, ey, Color.Lerp(p[ex - 4, ey], Color.black, 0.3f));
            }
        }

        /// <summary>Taches rondes semées sur le disque (rayon <paramref name="r"/>), en gardant leurs distances.</summary>
        private static bool[,] Dots(int count, float r, int seed, Func<float, float, bool> inside, float margin)
        {
            var dots = new bool[Width, Height];
            var random = new System.Random(seed);
            var centers = new System.Collections.Generic.List<Vector2>();
            for (int attempt = 0; attempt < count * 30 && centers.Count < count; attempt++)
            {
                var c = new Vector2(random.Next(0, Width), random.Next(0, Height));
                if (!inside(c.x, c.y) || !inside(c.x + margin, c.y) || !inside(c.x - margin, c.y) || !inside(c.x, c.y + margin) || !inside(c.x, c.y - margin)) continue;
                bool far = true;
                foreach (Vector2 o in centers) far &= Vector2.Distance(o, c) > r * 2.6f;
                if (!far) continue;
                centers.Add(c);
                for (int x = (int)(c.x - r - 1); x <= c.x + r + 1; x++)
                {
                    for (int y = (int)(c.y - r - 1); y <= c.y + r + 1; y++)
                    {
                        if (x >= 0 && y >= 0 && x < Width && y < Height && Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) <= r) dots[x, y] = true;
                    }
                }
            }

            return dots;
        }

        // ---------- Les espèces ----------

        /// <summary>
        /// Raie léopard (aigle de mer ocellé) : ailes pointues en faux, tête qui dépasse en bec de canard, dos bleu
        /// nuit semé de points blancs, queue en fouet trois fois plus longue que le corps.
        /// </summary>
        public static Color[,] SpottedEagleRay()
        {
            var p = new Color[Width, Height];
            const float headX = 80f, tipX = 52f, baseX = 40f, halfSpan = 36f;
            bool Inside(float x, float y)
            {
                float t = Mathf.Abs(y - Cy) / halfSpan;
                // La tête : un museau arrondi qui dépasse devant les ailes.
                float hx = (x - headX) / 9f, hy = (y - Cy) / 6f;
                if (x > headX - 4f && hx * hx + hy * hy <= 1f) return true;
                if (t >= 1f) return false;
                float front = tipX + (headX - tipX) * Mathf.Pow(1f - t * t, 1.1f);
                float rear = tipX - 4f - (tipX - 4f - baseX) * Mathf.Pow(1f - t, 2.2f);
                return x >= rear && x <= front;
            }

            Tail(p, baseX + 2f, 0f, 1.6f, 0.5f, C(0.10f, 0.12f, 0.18f), 3f);
            CreatureArt.Limb(p, baseX - 2f, Cy, baseX - 6f, Cy - 0.5f, 0.6f, C(0.80f, 0.78f, 0.70f));  // l'aiguillon
            bool[,] dots = Dots(60, 0.9f, 501, Inside, 2f);
            Disc(p, Inside, (x, y) => dots[x, y] ? C(0.96f, 0.96f, 0.94f) : C(0.14f, 0.17f, 0.26f), 0.5f, baseX, headX + 6f);
            Eyes(p, headX + 1f, 5f, C(0.18f, 0.16f, 0.12f));
            return CreatureArt.Finish(p, C(0.03f, 0.04f, 0.07f));
        }

        /// <summary>
        /// Pastenague à points bleus : disque ovale jaune-olive constellé de points bleu électrique, queue plutôt
        /// courte bordée d'une nageoire en ruban et de deux traits bleus, aiguillon.
        /// </summary>
        public static Color[,] BlueSpottedRibbontail()
        {
            var p = new Color[Width, Height];
            const float cx = 64f, rx = 26f, ry = 22f;
            bool Inside(float x, float y)
            {
                float u = (x - cx) / rx, v = (y - Cy) / ry;
                return u * u + v * v <= 1f;
            }

            // Queue avec la nageoire en ruban (bleue) et les deux traits.
            Tail(p, cx - rx + 4f, 6f, 2.4f, 0.8f, C(0.48f, 0.46f, 0.26f), 1.5f);
            for (float x = 10f; x < 30f; x += 0.5f)
            {
                float t = Mathf.InverseLerp(cx - rx + 4f, 6f, x);
                PixelArt.Set(p, Mathf.RoundToInt(x), Mathf.RoundToInt(TailY(t) - 2f), C(0.20f, 0.52f, 0.92f));
                PixelArt.Set(p, Mathf.RoundToInt(x), Mathf.RoundToInt(TailY(t) + 1f), C(0.24f, 0.58f, 0.96f));
            }

            CreatureArt.Limb(p, 32f, Cy + 1f, 25f, Cy + 2.5f, 0.6f, C(0.86f, 0.84f, 0.74f));
            bool[,] dots = Dots(34, 1.4f, 502, Inside, 2f);
            Disc(p, Inside, (x, y) => dots[x, y] ? C(0.22f, 0.56f, 0.98f) : C(0.66f, 0.62f, 0.34f), 0.7f, cx - rx, cx + rx);
            Eyes(p, cx + rx - 8f, 5f, C(0.60f, 0.52f, 0.24f));
            return CreatureArt.Finish(p, C(0.06f, 0.06f, 0.04f));
        }

        /// <summary>Raie motoro (raie d'eau douce d'Amazonie) : disque rond brun chocolat semé d'ocelles orangés cerclés de noir, queue courte et épaisse à aiguillon.</summary>
        public static Color[,] Motoro()
        {
            var p = new Color[Width, Height];
            const float cx = 62f, r = 26f;
            bool Inside(float x, float y)
            {
                float u = (x - cx) / r, v = (y - Cy) / r;
                return u * u + v * v <= 1f;
            }

            Tail(p, cx - r + 4f, 16f, 3f, 1.2f, C(0.32f, 0.24f, 0.16f), 1f);
            CreatureArt.Limb(p, 30f, Cy + 1.5f, 22f, Cy + 3f, 0.7f, C(0.86f, 0.80f, 0.66f));
            bool[,] rings = Dots(26, 2.6f, 503, Inside, 3f);
            bool[,] centers = new bool[Width, Height];
            for (int x = 1; x < Width - 1; x++)
            {
                for (int y = 1; y < Height - 1; y++)
                {
                    centers[x, y] = rings[x, y] && rings[x - 1, y] && rings[x + 1, y] && rings[x, y - 1] && rings[x, y + 1];
                }
            }

            Disc(p, Inside, (x, y) => centers[x, y] ? C(0.98f, 0.62f, 0.18f) : rings[x, y] ? C(0.08f, 0.06f, 0.04f) : C(0.40f, 0.30f, 0.20f), 0.6f, cx - r, cx + r);
            Eyes(p, cx + r - 9f, 6f, C(0.52f, 0.40f, 0.20f));
            return CreatureArt.Finish(p, C(0.05f, 0.04f, 0.03f));
        }

        /// <summary>
        /// Raie bouclée : disque en losange au museau pointu, brun-gris marbré de taches claires et sombres, rangée
        /// de grosses épines (les « boucles ») le long du dos et de la queue, deux petites dorsales au bout de la queue.
        /// </summary>
        public static Color[,] ThornbackRay()
        {
            var p = new Color[Width, Height];
            const float cx = 62f, front = 30f, rear = 22f, half = 30f;
            bool Inside(float x, float y)
            {
                float u = x >= cx ? (x - cx) / front : (cx - x) / rear;
                float v = Mathf.Abs(y - Cy) / half;
                float k = x >= cx ? 1.05f : 1.7f;      // devant : pointu, derrière : arrondi
                return Mathf.Pow(u, k) + Mathf.Pow(v, 1.35f) <= 1f;
            }

            Tail(p, cx - rear + 4f, 6f, 2.6f, 1.2f, C(0.50f, 0.42f, 0.32f), 1f);
            // Deux petites dorsales et une nageoire au bout.
            CreatureArt.Blob(p, 18f, TailY(Mathf.InverseLerp(cx - rear + 4f, 6f, 18f), 1f) + 1.5f, 2.6f, 1.6f, C(0.48f, 0.40f, 0.30f));
            CreatureArt.Blob(p, 12f, TailY(Mathf.InverseLerp(cx - rear + 4f, 6f, 12f), 1f) + 1.5f, 2.4f, 1.5f, C(0.48f, 0.40f, 0.30f));
            var random = new System.Random(504);
            var marble = new float[Width, Height];
            for (int i = 0; i < 40; i++)
            {
                float mx = random.Next(30, 96), my = random.Next(10, 74), mr = 1.5f + (float)random.NextDouble() * 2.5f, tone = random.NextDouble() < 0.5 ? -1f : 1f;
                for (int x = (int)(mx - mr); x <= mx + mr; x++)
                {
                    for (int y = (int)(my - mr); y <= my + mr; y++)
                    {
                        if (x >= 0 && y >= 0 && x < Width && y < Height && Vector2.Distance(new Vector2(x, y), new Vector2(mx, my)) <= mr) marble[x, y] = tone;
                    }
                }
            }

            Disc(p, Inside, (x, y) => marble[x, y] > 0f ? C(0.80f, 0.72f, 0.56f) : marble[x, y] < 0f ? C(0.34f, 0.28f, 0.20f) : C(0.56f, 0.46f, 0.34f), 0.8f, cx - rear, cx + front);
            // Les boucles : grosses épines claires le long du dos et de la queue, et quelques-unes sur les ailes.
            for (float x = 14f; x < cx + 12f; x += 4f)
            {
                float y = x < cx - rear + 4f ? TailY(Mathf.InverseLerp(cx - rear + 4f, 6f, x), 1f) : Cy;
                PixelArt.Set(p, Mathf.RoundToInt(x), Mathf.RoundToInt(y), C(0.94f, 0.90f, 0.78f));
                PixelArt.Set(p, Mathf.RoundToInt(x) - 1, Mathf.RoundToInt(y), C(0.62f, 0.54f, 0.40f));
            }

            foreach (Vector2 thorn in new[] { new Vector2(66, 50), new Vector2(66, 34), new Vector2(56, 56), new Vector2(56, 28), new Vector2(74, 46), new Vector2(74, 38) })
            {
                PixelArt.Set(p, (int)thorn.x, (int)thorn.y, C(0.94f, 0.90f, 0.78f));
            }

            Eyes(p, cx + 12f, 5f, C(0.62f, 0.56f, 0.30f));
            return CreatureArt.Finish(p, C(0.06f, 0.05f, 0.03f));
        }

        /// <summary>
        /// Torpille noire (raie électrique) : disque presque rond, épais et mou, brun chocolat uni presque noir, queue
        /// courte et charnue à deux dorsales et vraie nageoire caudale ; deux organes électriques en haricot de part et d'autre de la tête.
        /// </summary>
        public static Color[,] ElectricRay()
        {
            var p = new Color[Width, Height];
            const float cx = 64f, rx = 24f, ry = 25f;
            bool Inside(float x, float y)
            {
                float u = (x - cx) / rx, v = (y - Cy) / ry;
                return u * u + v * v <= 1f;
            }

            Color skin = C(0.20f, 0.16f, 0.14f);
            Tail(p, cx - rx + 4f, 14f, 5f, 3f, skin, 0.5f);
            CreatureArt.Blob(p, 30f, Cy + 4.5f, 3.5f, 2.4f, skin);
            CreatureArt.Blob(p, 22f, Cy + 4f, 3f, 2f, skin);
            // Nageoire caudale en éventail arrondi.
            CreatureArt.Blob(p, 11f, Cy, 4.5f, 6f, C(0.18f, 0.14f, 0.12f));
            Disc(p, Inside, (x, y) =>
            {
                // Les organes électriques : deux haricots à peine plus clairs, de part et d'autre de la tête.
                float u = (x - (cx + 6f)) / 7f;
                float v1 = (Mathf.Abs(y - Cy) - 11f) / 5f;
                return u * u + v1 * v1 <= 1f ? C(0.28f, 0.22f, 0.20f) : skin;
            }, 0.5f, cx - rx, cx + rx);
            Eyes(p, cx + rx - 9f, 4f, C(0.40f, 0.34f, 0.24f));
            return CreatureArt.Finish(p, C(0.03f, 0.02f, 0.02f));
        }
    }
}
