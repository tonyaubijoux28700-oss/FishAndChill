using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Le carnet du pêcheur (pixel art provisoire de l'index) : couverture de cuir cousue, pages de papier jauni,
    /// cases encrées, onglets marque-page, pli central, tampon « complet », petites icônes soleil / lune / pièce.
    /// Sprites étirables en 9 parties, même échelle que la charte (1 pixel = 4 unités d'interface).
    /// </summary>
    public static class BookArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static float Noise(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 73856093 ^ y * 19349663 ^ seed * 83492791;
                h = (h << 13) ^ h;
                return ((h * (h * h * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647f;
            }
        }

        /// <summary>Couverture 40×40 (bord 12) : cuir brun, liseré clair, couture en pointillés, coins dorés.</summary>
        public static Color[,] Cover()
        {
            const int s = 40;
            var p = new Color[s, s];
            Color leather = C(0.40f, 0.22f, 0.12f);
            for (int x = 0; x < s; x++)
            {
                for (int y = 0; y < s; y++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(s - 1 - x, s - 1 - y));
                    bool corner = (x < 2 || x > s - 3) && (y < 2 || y > s - 3);
                    if (corner)
                    {
                        continue; // coins arrondis
                    }

                    Color c = leather;
                    if (edge == 0) c = C(0.16f, 0.08f, 0.04f);
                    else if (edge == 1) c = C(0.56f, 0.34f, 0.18f);
                    else if (edge == 4 && (x + y) % 3 != 0) c = C(0.86f, 0.70f, 0.42f); // couture
                    else c = DetailedFish.Shade(leather, 0.92f + Noise(x, y, 3) * 0.16f);
                    p[x, y] = c;
                }
            }

            // Coins en laiton.
            foreach (Vector2Int k in new[] { new Vector2Int(2, 2), new Vector2Int(s - 7, 2), new Vector2Int(2, s - 7), new Vector2Int(s - 7, s - 7) })
            {
                for (int dx = 0; dx < 5; dx++)
                {
                    for (int dy = 0; dy < 5; dy++)
                    {
                        if (dx + dy < 7) p[k.x + dx, k.y + dy] = dx == 0 || dy == 0 ? C(0.96f, 0.82f, 0.40f) : C(0.80f, 0.62f, 0.24f);
                    }
                }
            }

            return p;
        }

        /// <summary>Page 32×32 (bord 8) : papier crème, bords jaunis, grain léger.</summary>
        public static Color[,] Page()
        {
            const int s = 32;
            var p = new Color[s, s];
            Color paper = C(0.96f, 0.91f, 0.78f);
            Color aged = C(0.86f, 0.76f, 0.56f);
            for (int x = 0; x < s; x++)
            {
                for (int y = 0; y < s; y++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(s - 1 - x, s - 1 - y));
                    Color c = Color.Lerp(aged, paper, Mathf.Clamp01(edge / 6f));
                    if (edge == 0) c = C(0.62f, 0.50f, 0.32f);
                    if (Noise(x, y, 7) > 0.93f) c = DetailedFish.Shade(c, 0.95f);
                    p[x, y] = c;
                }
            }

            return p;
        }

        /// <summary>Case encrée 16×16 (bord 5) : papier un peu plus foncé, filet brun, coins arrondis.</summary>
        public static Color[,] Cell()
        {
            const int s = 16;
            var p = new Color[s, s];
            for (int x = 0; x < s; x++)
            {
                for (int y = 0; y < s; y++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(s - 1 - x, s - 1 - y));
                    bool corner = (x == 0 || x == s - 1) && (y == 0 || y == s - 1);
                    if (corner) continue;
                    p[x, y] = edge == 0 ? C(0.42f, 0.30f, 0.18f) : edge == 1 ? C(0.80f, 0.70f, 0.52f) : C(0.92f, 0.86f, 0.70f);
                }
            }

            return p;
        }

        /// <summary>
        /// Cadre étirable à coins bien arrondis (24×24, rayon 7 px, bords de 9) : liseré foncé, filet clair à l'intérieur,
        /// fond légèrement plus clair en haut (éclairé). Sert aux étiquettes et aux cases de l'inventaire.
        /// </summary>
        public static Color[,] RoundedFrame(Color fill, Color rim, Color highlight)
        {
            const int s = 24;
            const float r = 7f;
            var p = new Color[s, s];
            for (int x = 0; x < s; x++)
            {
                for (int y = 0; y < s; y++)
                {
                    // Distance au bord en tenant compte des coins arrondis.
                    float cx = Mathf.Clamp(x + 0.5f, r, s - r);
                    float cy = Mathf.Clamp(y + 0.5f, r, s - r);
                    float d = r - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    if (d < 0f)
                    {
                        continue;
                    }

                    Color c;
                    if (d < 1.2f) c = rim;
                    else if (d < 2.2f) c = highlight;
                    else if (fill.a <= 0f) continue; // liseré seul : l'intérieur reste transparent
                    else c = Color.Lerp(fill, DetailedFish.Shade(fill, 1.08f), y / (float)s);
                    p[x, y] = c;
                }
            }

            return p;
        }

        /// <summary>Onglet marque-page 20×28 (bords 6/6/6/10), blanc pour être teinté : haut arrondi, ombre en bas.</summary>
        public static Color[,] Ribbon()
        {
            const int w = 20;
            const int h = 28;
            var p = new Color[w, h];
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    bool corner = y > h - 3 && (x < 2 || x > w - 3) && !(y == h - 3 && (x == 1 || x == w - 2));
                    if (corner) continue;
                    int edge = Mathf.Min(x, w - 1 - x);
                    Color c = C(0.94f, 0.94f, 0.94f);
                    if (edge == 0 || y == h - 1) c = C(0.30f, 0.30f, 0.30f);
                    else if (edge == 1 || y == h - 2) c = C(1f, 1f, 1f);
                    else if (y < 4) c = C(0.70f, 0.70f, 0.70f); // ombre sous le bord du livre
                    p[x, y] = c;
                }
            }

            return p;
        }

        /// <summary>Pli central 16×32 : ombre douce qui s'assombrit vers le milieu.</summary>
        public static Color[,] Spine()
        {
            var p = new Color[16, 32];
            for (int x = 0; x < 16; x++)
            {
                float d = Mathf.Abs(x - 7.5f) / 7.5f;
                for (int y = 0; y < 32; y++)
                {
                    p[x, y] = C(0.30f, 0.20f, 0.10f, Mathf.Lerp(0.55f, 0f, d));
                }
            }

            return p;
        }

        /// <summary>Tampon 48×48 : double cercle rouge à l'encre, un peu effacé par endroits.</summary>
        public static Color[,] Stamp()
        {
            const int s = 48;
            var p = new Color[s, s];
            Color ink = C(0.80f, 0.16f, 0.12f, 0.85f);
            for (int x = 0; x < s; x++)
            {
                for (int y = 0; y < s; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(23.5f, 23.5f));
                    bool ring = (d > 20f && d < 23f) || (d > 16.5f && d < 18f);
                    if (ring && Noise(x, y, 11) > 0.12f) p[x, y] = ink;
                }
            }

            return p;
        }

        /// <summary>Soleil 11×11 : disque jaune et rayons.</summary>
        public static Color[,] Sun()
        {
            var p = new Color[11, 11];
            Color core = C(1f, 0.82f, 0.22f);
            for (int x = 0; x < 11; x++)
            {
                for (int y = 0; y < 11; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(5f, 5f));
                    if (d <= 2.6f) p[x, y] = d > 1.8f ? C(0.88f, 0.56f, 0.10f) : core;
                }
            }

            foreach (Vector2Int r in new[] { new Vector2Int(5, 0), new Vector2Int(5, 10), new Vector2Int(0, 5), new Vector2Int(10, 5), new Vector2Int(1, 1), new Vector2Int(9, 9), new Vector2Int(1, 9), new Vector2Int(9, 1) })
            {
                p[r.x, r.y] = C(0.96f, 0.66f, 0.14f);
            }

            return p;
        }

        /// <summary>Lune 11×11 : croissant bleuté.</summary>
        public static Color[,] Moon()
        {
            var p = new Color[11, 11];
            for (int x = 0; x < 11; x++)
            {
                for (int y = 0; y < 11; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(5f, 5f));
                    float cut = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 6.5f));
                    if (d <= 4.6f && cut > 3.6f) p[x, y] = d > 3.8f || cut < 4.4f ? C(0.30f, 0.36f, 0.62f) : C(0.84f, 0.88f, 1f);
                }
            }

            return p;
        }

        /// <summary>Pièce 10×10 : or, reflet, liseré foncé.</summary>
        public static Color[,] Coin()
        {
            var p = new Color[10, 10];
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(4.5f, 4.5f));
                    if (d > 4.6f) continue;
                    Color c = d > 3.6f ? C(0.62f, 0.40f, 0.08f) : C(0.98f, 0.80f, 0.26f);
                    if (d < 2.6f && x + y > 9) c = C(0.90f, 0.66f, 0.16f);
                    if (x == 3 && y == 6 || x == 4 && y == 7) c = C(1f, 0.96f, 0.76f);
                    p[x, y] = c;
                }
            }

            return p;
        }
    }
}
