using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Pixel art provisoire du lac alpin et du voyage : recoloration des tuiles de l'île (eau plus profonde et plus
    /// verte, galets gris, prés d'altitude), sapin, ponton en planches et barque.
    /// </summary>
    public static class LakeArt
    {
        public const int PineWidth = 32;
        public const int PineHeight = 40;

        /// <summary>Pied du tronc, placé au centre de la case (comme le cocotier).</summary>
        public static readonly Vector2 PinePivot = new Vector2(16f / PineWidth, 5f / PineHeight);

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        // ---------- Recoloration des tuiles de l'île ----------

        /// <summary>Teinte, saturation et luminosité modifiées (pixels transparents gardés).</summary>
        public static Color Tint(Color c, float hueShift, float saturation, float value)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            Color result = Color.HSVToRGB(Mathf.Repeat(h + hueShift, 1f), Mathf.Clamp01(s * saturation), Mathf.Clamp01(v * value));
            result.a = c.a;
            return result;
        }

        public static Color[,] Tint(Color[,] pixels, float hueShift, float saturation, float value)
        {
            int w = pixels.GetLength(0);
            int h = pixels.GetLength(1);
            var result = new Color[w, h];
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    result[x, y] = Tint(pixels[x, y], hueShift, saturation, value);
                }
            }

            return result;
        }

        /// <summary>Eau du lac : plus profonde, plus bleu-vert, moins éclatante que le lagon.</summary>
        public static Color Water(Color c) => Tint(c, 0.03f, 0.80f, 0.70f);

        /// <summary>Galets de la rive : le sable devenu gris.</summary>
        public static Color Gravel(Color c) => Tint(c, 0.02f, 0.22f, 0.68f);

        /// <summary>Pré d'altitude : herbe un peu plus froide et plus sombre.</summary>
        public static Color Meadow(Color c) => Tint(c, 0.02f, 0.92f, 0.86f);

        public static Color[,] Water(Color[,] p) => Tint(p, 0.03f, 0.80f, 0.70f);
        public static Color[,] Gravel(Color[,] p) => Tint(p, 0.02f, 0.22f, 0.68f);
        public static Color[,] Meadow(Color[,] p) => Tint(p, 0.02f, 0.92f, 0.86f);

        // ---------- Sapin ----------

        /// <summary>Sapin vu de 3/4 : ombre au sol, tronc court, étages de branches en triangles éclairés à gauche.</summary>
        public static Color[,] Pine()
        {
            var p = new Color[PineWidth, PineHeight];
            Color shadow = C(0.04f, 0.14f, 0.08f, 0.35f);
            Color trunk = C(0.40f, 0.27f, 0.16f);
            Color needles = C(0.13f, 0.36f, 0.24f);

            for (int y = 4; y <= 10; y++)
            {
                for (int x = 15; x <= 17; x++)
                {
                    p[x, y] = x == 15 ? Color.Lerp(trunk, Color.white, 0.15f) : x == 17 ? Color.Lerp(trunk, Color.black, 0.3f) : trunk;
                }
            }

            // Quatre étages, du plus large (bas) au plus étroit (haut).
            int[] baseY = { 9, 16, 23, 29 };
            int[] halfWidth = { 12, 10, 8, 5 };
            int[] tierHeight = { 10, 9, 8, 9 };
            for (int tier = 0; tier < baseY.Length; tier++)
            {
                for (int y = baseY[tier]; y < baseY[tier] + tierHeight[tier] && y < PineHeight; y++)
                {
                    float t = (y - baseY[tier]) / (float)tierHeight[tier];
                    float half = halfWidth[tier] * (1f - t) + 0.5f;
                    for (int x = 0; x < PineWidth; x++)
                    {
                        float dx = x + 0.5f - 16f;
                        if (Mathf.Abs(dx) > half)
                        {
                            continue;
                        }

                        // Éclairé à gauche, ombre à droite et sous chaque étage ; bords dentelés.
                        float side = dx / Mathf.Max(1f, half);
                        float light = 1.05f - side * 0.25f - (t < 0.18f ? 0.2f : 0f);
                        bool notch = Mathf.Abs(dx) > half - 1.2f && (y + x) % 3 == 0;
                        if (notch)
                        {
                            continue;
                        }

                        p[x, y] = DetailedFish.Shade(needles, Mathf.Round(light * 5f) / 5f);
                    }
                }
            }

            PixelArt.SelectiveOutline(p, C(0.03f, 0.10f, 0.06f), 0.5f);

            // Ombre au sol après le contour (sinon elle serait cerclée de noir).
            for (int x = 0; x < PineWidth; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    float dx = (x - 17.5f) / 10f;
                    float dy = (y - 4.5f) / 3.2f;
                    if (dx * dx + dy * dy <= 1f && p[x, y].a < 0.01f)
                    {
                        p[x, y] = shadow;
                    }
                }
            }

            return p;
        }

        // ---------- Ponton et barque ----------

        /// <summary>Ponton vertical de 2 cases de large et <paramref name="cells"/> cases de long : planches et poteaux.</summary>
        public static Color[,] Dock(int cells)
        {
            int w = 32;
            int h = cells * 16;
            var p = new Color[w, h];
            Color plank = C(0.62f, 0.46f, 0.28f);
            Color gap = C(0.30f, 0.20f, 0.12f);
            Color post = C(0.36f, 0.24f, 0.14f);
            for (int x = 1; x < w - 1; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    bool seam = y % 4 == 3;
                    float grain = ((x * 7 + y * 13) % 11) / 11f;
                    Color c = seam ? gap : Color.Lerp(plank, Color.Lerp(plank, Color.black, 0.2f), grain * 0.5f);
                    if (x == 1 || x == w - 2)
                    {
                        c = Color.Lerp(c, Color.black, 0.35f); // bords plus sombres
                    }

                    p[x, y] = c;
                }
            }

            // Poteaux aux angles et tous les 2 cases, qui dépassent un peu.
            for (int y = 0; y < h; y += 32)
            {
                for (int dy = 0; dy < 4 && y + dy < h; dy++)
                {
                    p[0, y + dy] = post;
                    p[1, y + dy] = post;
                    p[w - 1, y + dy] = post;
                    p[w - 2, y + dy] = post;
                }
            }

            PixelArt.SelectiveOutline(p, C(0.16f, 0.10f, 0.06f), 0.5f);
            return p;
        }

        /// <summary>Barque vue de dessus, amarrée le long du ponton : coque en bois, deux bancs, rames rentrées.</summary>
        public static Color[,] Boat()
        {
            var p = new Color[18, 34];
            Color hull = C(0.56f, 0.30f, 0.18f);
            Color inside = C(0.72f, 0.54f, 0.34f);
            Color bench = C(0.46f, 0.32f, 0.20f);
            for (int y = 1; y < 33; y++)
            {
                float t = (y - 1) / 31f;
                // Proue pointue en haut, poupe arrondie en bas.
                float half = 7.5f * Mathf.Sqrt(Mathf.Sin(Mathf.Clamp01(t * 0.92f + 0.04f) * Mathf.PI));
                for (int x = 0; x < 18; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - 9f);
                    if (dx > half)
                    {
                        continue;
                    }

                    bool rim = dx > half - 1.6f;
                    p[x, y] = rim ? DetailedFish.Shade(hull, x < 9 ? 1.1f : 0.85f) : inside;
                }
            }

            foreach (int y in new[] { 11, 22 })
            {
                for (int x = 3; x <= 14; x++)
                {
                    if (PixelArt.IsFilled(p, x, y)) p[x, y] = bench;
                    if (PixelArt.IsFilled(p, x, y + 1)) p[x, y + 1] = bench;
                }
            }

            PixelArt.SelectiveOutline(p, C(0.18f, 0.08f, 0.04f), 0.5f);
            return p;
        }

        // ---------- Décor du lac ----------

        /// <summary>Touffe de roseaux 16×24 : tiges fines qui ondulent, quelques massettes brunes, pied dans l'eau.</summary>
        public static Color[,] Reeds(int variant)
        {
            var p = new Color[16, 24];
            Color stem = C(0.42f, 0.56f, 0.24f);
            Color stemDark = C(0.26f, 0.38f, 0.16f);
            Color cattail = C(0.44f, 0.28f, 0.16f);
            var random = new System.Random(500 + variant);
            int count = 6 + variant;
            for (int i = 0; i < count; i++)
            {
                float x0 = 3f + random.Next(0, 10);
                int top = 12 + random.Next(0, 10);
                float lean = (float)(random.NextDouble() - 0.5) * 4f;
                Color c = i % 2 == 0 ? stem : stemDark;
                for (int y = 1; y <= top; y++)
                {
                    float t = y / (float)top;
                    PixelArt.Set(p, Mathf.RoundToInt(x0 + lean * t * t), y, c);
                }

                if (i % 3 == 0 && top > 15)
                {
                    int cx = Mathf.RoundToInt(x0 + lean * 0.7f);
                    for (int y = top - 7; y <= top - 3; y++)
                    {
                        PixelArt.Set(p, cx, y, cattail);
                        PixelArt.Set(p, cx + 1, y, DetailedFish.Shade(cattail, 0.8f));
                    }
                }
            }

            PixelArt.SelectiveOutline(p, C(0.10f, 0.16f, 0.06f), 0.5f);
            // Petits ronds dans l'eau au pied de la touffe.
            for (int x = 2; x <= 13; x++)
            {
                if (p[x, 0].a < 0.01f && x % 3 != 0) p[x, 0] = C(0.75f, 0.85f, 0.88f, 0.55f);
            }

            return p;
        }

        /// <summary>Nénuphar 16×16 : feuilles rondes fendues, parfois une fleur blanche ou rose.</summary>
        public static Color[,] Lily(int variant)
        {
            var p = new Color[16, 16];
            Color leaf = C(0.24f, 0.50f, 0.22f);
            void Pad(float cx, float cy, float r, float notch)
            {
                for (int x = 0; x < 16; x++)
                {
                    for (int y = 0; y < 16; y++)
                    {
                        float dx = x + 0.5f - cx;
                        float dy = y + 0.5f - cy;
                        if (dx * dx + dy * dy > r * r) continue;
                        float angle = Mathf.Atan2(dy, dx);
                        if (Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, notch)) < 18f) continue; // la fente
                        float light = 1.05f - dy / r * 0.15f + dx / r * -0.1f;
                        p[x, y] = DetailedFish.Shade(leaf, Mathf.Round(light * 5f) / 5f);
                    }
                }
            }

            Pad(6f, 7f, 5f, 40f);
            if (variant != 1) Pad(12f, 11f, 3f, 200f);
            if (variant == 2) Pad(12.5f, 4f, 2.6f, 120f);
            PixelArt.SelectiveOutline(p, C(0.08f, 0.20f, 0.10f), 0.5f);
            if (variant != 2)
            {
                Color petal = variant == 0 ? C(0.98f, 0.97f, 0.94f) : C(0.96f, 0.70f, 0.80f);
                foreach (Vector2Int d in new[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(1, 1), new Vector2Int(-1, 1) })
                {
                    PixelArt.Set(p, 6 + d.x, 8 + d.y, petal);
                }

                PixelArt.Set(p, 6, 8, C(0.98f, 0.82f, 0.25f));
            }

            return p;
        }

        /// <summary>Pré d'altitude fleuri 16×16 : gentianes bleues, marguerites blanches ou boutons d'or.</summary>
        public static Color[,] FlowerMeadow(int variant)
        {
            Color[,] p = Meadow(TileArt.Grass(0));
            Color[] petals = { C(0.30f, 0.42f, 0.92f), C(0.98f, 0.98f, 0.94f), C(0.98f, 0.84f, 0.20f) };
            Color petal = petals[variant % petals.Length];
            var random = new System.Random(700 + variant);
            for (int i = 0; i < 3; i++)
            {
                int x = 2 + random.Next(0, 12);
                int y = 2 + random.Next(0, 12);
                PixelArt.Set(p, x, y, petal);
                PixelArt.Set(p, x + 1, y, DetailedFish.Shade(petal, 0.85f));
                PixelArt.Set(p, x, y + 1, DetailedFish.Shade(petal, 1.1f));
                PixelArt.Set(p, x, y - 1, C(0.20f, 0.42f, 0.18f));
            }

            return p;
        }

        /// <summary>Icône de minimap du ponton : une petite barque.</summary>
        public static Color[,] BoatIcon()
        {
            Color[,] boat = PixelArt.Downscale(Boat(), 2);
            var icon = new Color[16, 16];
            int w = boat.GetLength(0);
            int h = boat.GetLength(1);
            for (int x = 0; x < w && x + 4 < 16; x++)
            {
                for (int y = 0; y < h && y < 16; y++)
                {
                    icon[x + 4, y] = boat[x, y];
                }
            }

            return icon;
        }
    }
}
