using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Brochet (Esox lucius) en pixel art détaillé, 96×32, tête à droite.
    /// Dessin original reprenant les traits réels de l'espèce : museau en bec de canard (mâchoire inférieure
    /// plus longue), dorsale et anale très reculées, taches claires en haricot sur fond vert olive,
    /// nageoires orangées striées.
    /// </summary>
    public static class PikeArt
    {
        public const int Width = 96;
        public const int Height = 32;

        private const float CenterY = 15.5f;

        private static readonly Color Back = new Color(0.20f, 0.27f, 0.14f);
        private static readonly Color Flank = new Color(0.40f, 0.50f, 0.26f);
        private static readonly Color Belly = new Color(0.93f, 0.91f, 0.80f);
        private static readonly Color Spot = new Color(0.82f, 0.83f, 0.52f);
        private static readonly Color SpotShade = new Color(0.68f, 0.70f, 0.40f);
        private static readonly Color GillGold = new Color(0.62f, 0.62f, 0.36f);
        private static readonly Color Mouth = new Color(0.14f, 0.13f, 0.09f);
        private static readonly Color Iris = new Color(0.88f, 0.70f, 0.28f);
        private static readonly Color Pupil = new Color(0.06f, 0.05f, 0.04f);
        private static readonly Color FinOrange = new Color(0.82f, 0.46f, 0.20f);
        private static readonly Color FinEdge = new Color(0.94f, 0.66f, 0.36f);
        private static readonly Color FinRay = new Color(0.52f, 0.26f, 0.11f);
        private static readonly Color DorsalBase = new Color(0.64f, 0.63f, 0.30f);
        private static readonly Color DorsalRay = new Color(0.40f, 0.40f, 0.17f);
        private static readonly Color OutlineColor = new Color(0.09f, 0.11f, 0.07f);

        // Profil du corps : demi-hauteurs au-dessus (dos) et au-dessous (ventre) de l'axe, le long de x.
        private static readonly Vector2[] UpperProfile =
        {
            new Vector2(12, 2.3f), new Vector2(20, 3f), new Vector2(30, 5f), new Vector2(45, 6.4f),
            new Vector2(62, 6.4f), new Vector2(72, 5.4f), new Vector2(82, 3.6f), new Vector2(88, 2.2f), new Vector2(93, 1.0f),
        };

        private static readonly Vector2[] LowerProfile =
        {
            new Vector2(12, 2.3f), new Vector2(20, 3f), new Vector2(30, 5f), new Vector2(45, 6.6f),
            new Vector2(62, 6.6f), new Vector2(72, 6.0f), new Vector2(82, 4.3f), new Vector2(88, 3.0f), new Vector2(94, 1.8f),
        };

        public static Color[,] Draw()
        {
            var p = new Color[Width, Height];

            DrawCaudalFin(p);
            DrawDorsalFin(p);
            DrawAnalFin(p);
            DrawPelvicFin(p);
            DrawBody(p);
            DrawSpots(p);
            DrawLateralLine(p);
            DrawHead(p);
            DrawPectoralFin(p);

            PixelArt.Outline(p, OutlineColor);
            return p;
        }

        private static float Upper(float x) => Sample(UpperProfile, x);
        private static float Lower(float x) => Sample(LowerProfile, x);

        private static bool InBody(int x, int y)
        {
            float dy = y - CenterY;
            return x >= 12 && x <= 94 && dy <= Upper(x) && dy >= -Lower(x) && Upper(x) > 0f;
        }

        private static void DrawBody(Color[,] p)
        {
            for (int x = 12; x <= 94; x++)
            {
                float top = CenterY + Upper(x);
                float bottom = CenterY - Lower(x);
                for (int y = Mathf.FloorToInt(bottom); y <= Mathf.CeilToInt(top); y++)
                {
                    if (!InBody(x, y))
                    {
                        continue;
                    }

                    // 0 = dos, 1 = ventre.
                    float t = Mathf.InverseLerp(top, bottom, y);
                    Color c = t < 0.35f
                        ? Color.Lerp(Back, Flank, t / 0.35f)
                        : t < 0.68f ? Flank : Color.Lerp(Flank, Belly, (t - 0.68f) / 0.32f);

                    // Texture d'écailles discrète sur le dos et les flancs.
                    if (t < 0.75f && (x + 2 * y) % 4 == 0)
                    {
                        c = Color.Lerp(c, Color.black, 0.10f);
                    }

                    // Reflet le long du dos.
                    if (Mathf.Abs(y - (top - 1.5f)) < 0.5f && x > 30 && x < 78)
                    {
                        c = Color.Lerp(c, Color.white, 0.08f);
                    }

                    p[x, y] = c;
                }
            }
        }

        /// <summary>Taches claires en forme de haricot, en rangées décalées sur les flancs.</summary>
        private static void DrawSpots(Color[,] p)
        {
            var random = new System.Random(4242);
            float[] rows = { 3.8f, 1.2f, -1.4f, -3.8f };
            for (int r = 0; r < rows.Length; r++)
            {
                float offset = r % 2 == 0 ? 0f : 2.5f;
                for (float x = 24f + offset; x < 74f; x += 5f)
                {
                    int sx = Mathf.RoundToInt(x + (float)(random.NextDouble() - 0.5) * 1.6f);
                    int sy = Mathf.RoundToInt(CenterY + rows[r] + (float)(random.NextDouble() - 0.5) * 1.2f);
                    int length = random.NextDouble() < 0.4 ? 2 : 3;

                    // Pas de taches sur le ventre clair.
                    if (sy < CenterY - Lower(sx) * 0.55f)
                    {
                        continue;
                    }

                    for (int dx = 0; dx < length; dx++)
                    {
                        Paint(p, sx + dx, sy, Spot);
                        if (dx > 0 && dx < length - 1 || length == 2)
                        {
                            Paint(p, sx + dx, sy + 1, Spot);
                        }
                    }

                    Paint(p, sx + length - 1, sy - 1, SpotShade);
                }
            }
        }

        private static void DrawLateralLine(Color[,] p)
        {
            for (int x = 22; x < 78; x++)
            {
                if (x % 6 < 4)
                {
                    int y = Mathf.RoundToInt(CenterY + 0.4f + Mathf.Sin(x * 0.08f) * 0.4f);
                    if (InBody(x, y))
                    {
                        p[x, y] = Color.Lerp(p[x, y], Belly, 0.25f);
                    }
                }
            }
        }

        private static void DrawHead(Color[,] p)
        {
            // Opercule (couvercle des ouïes) : zone dorée bordée d'un arc sombre.
            for (int x = 74; x <= 82; x++)
            {
                for (int y = Mathf.RoundToInt(CenterY - Lower(x)); y <= Mathf.RoundToInt(CenterY + 1); y++)
                {
                    if (InBody(x, y))
                    {
                        p[x, y] = Color.Lerp(p[x, y], GillGold, 0.45f);
                    }
                }
            }

            for (int y = -6; y <= 4; y++)
            {
                int x = Mathf.RoundToInt(74f + (y * y) * 0.08f);
                int py = Mathf.RoundToInt(CenterY + y);
                if (InBody(x, py))
                {
                    p[x, py] = Color.Lerp(p[x, py], Color.black, 0.35f);
                }
            }

            // Gueule : longue fente qui descend légèrement jusqu'au bout du museau.
            for (int x = 81; x <= 93; x++)
            {
                int y = Mathf.RoundToInt(CenterY - 0.6f - (x - 81) * 0.04f);
                if (InBody(x, y))
                {
                    p[x, y] = Mouth;
                }
            }

            // Dents suggérées sur la mâchoire inférieure.
            for (int x = 84; x <= 92; x += 3)
            {
                int y = Mathf.RoundToInt(CenterY - 1.4f - (x - 81) * 0.04f);
                if (InBody(x, y))
                {
                    p[x, y] = Belly;
                }
            }

            // Narine.
            Paint(p, 89, Mathf.RoundToInt(CenterY + 1.2f), Mouth);

            // Œil doré, haut placé sur la tête.
            int ex = 83;
            int ey = Mathf.RoundToInt(CenterY + 1.8f);
            PixelArt.Rect(p, ex - 1, ey - 1, ex + 1, ey + 1, Iris);
            PixelArt.Set(p, ex, ey, Pupil);
            PixelArt.Set(p, ex + 1, ey, Pupil);
            PixelArt.Set(p, ex - 1, ey + 1, Color.white);
        }

        /// <summary>Queue fourchue, base olive, rayons sombres, bord clair.</summary>
        private static void DrawCaudalFin(Color[,] p)
        {
            for (int x = 1; x <= 14; x++)
            {
                float t = (14 - x) / 13f;
                float span = 2.2f + t * 9.5f;
                for (int y = 0; y < Height; y++)
                {
                    float dy = y - CenterY;
                    float ady = Mathf.Abs(dy);
                    bool inside = ady <= span && (t < 0.5f || ady >= (t - 0.5f) * span * 1.5f);
                    if (!inside)
                    {
                        continue;
                    }

                    Color c = Color.Lerp(DorsalBase, FinOrange, Mathf.Clamp01(t * 1.6f));
                    // Rayons qui partent du pédoncule en éventail.
                    float angle = Mathf.Atan2(dy, 14 - x + 0.5f);
                    if (Mathf.Abs(Mathf.Sin(angle * 9f)) < 0.25f)
                    {
                        c = FinRay;
                    }

                    if (ady > span - 1f && t > 0.3f)
                    {
                        c = Color.Lerp(c, FinEdge, 0.5f);
                    }

                    p[x, y] = c;
                }
            }
        }

        /// <summary>Dorsale très reculée, au-dessus de l'anale (signature du brochet).</summary>
        private static void DrawDorsalFin(Color[,] p)
        {
            for (int x = 20; x <= 33; x++)
            {
                float t = Mathf.InverseLerp(20f, 33f, x);
                float height = Mathf.Lerp(7f, 1f, t * t) * Mathf.Clamp01((x - 19.5f) / 2.5f);
                float baseY = CenterY + Upper(x) - 0.5f;
                for (int k = 0; k <= Mathf.RoundToInt(height); k++)
                {
                    Color c = x % 2 == 0 ? DorsalRay : DorsalBase;
                    if (k == Mathf.RoundToInt(height))
                    {
                        c = Color.Lerp(DorsalBase, Color.white, 0.25f);
                    }

                    Paint(p, x, Mathf.RoundToInt(baseY + k), c);
                }
            }

            // Quelques taches sombres sur la dorsale.
            Paint(p, 23, Mathf.RoundToInt(CenterY + Upper(23) + 3), DorsalRay);
            Paint(p, 26, Mathf.RoundToInt(CenterY + Upper(26) + 2), DorsalRay);
        }

        private static void DrawAnalFin(Color[,] p)
        {
            for (int x = 20; x <= 31; x++)
            {
                float t = Mathf.InverseLerp(20f, 31f, x);
                float height = Mathf.Lerp(5.5f, 1f, t * t) * Mathf.Clamp01((x - 19.5f) / 2.5f);
                float baseY = CenterY - Lower(x) + 0.5f;
                for (int k = 0; k <= Mathf.RoundToInt(height); k++)
                {
                    Color c = x % 2 == 0 ? FinRay : FinOrange;
                    if (k == Mathf.RoundToInt(height))
                    {
                        c = FinEdge;
                    }

                    Paint(p, x, Mathf.RoundToInt(baseY - k), c);
                }
            }
        }

        private static void DrawPelvicFin(Color[,] p)
        {
            for (int x = 46; x <= 54; x++)
            {
                float t = Mathf.InverseLerp(54f, 46f, x);
                int height = Mathf.RoundToInt(Mathf.Lerp(1f, 4.5f, t));
                float baseY = CenterY - Lower(x) + 0.5f;
                for (int k = 0; k <= height; k++)
                {
                    Paint(p, x, Mathf.RoundToInt(baseY - k), k == height ? FinEdge : (x % 2 == 0 ? FinRay : FinOrange));
                }
            }
        }

        /// <summary>Pectorale juste derrière les ouïes, posée sur le bas du flanc.</summary>
        private static void DrawPectoralFin(Color[,] p)
        {
            for (int x = 66; x <= 73; x++)
            {
                float t = Mathf.InverseLerp(73f, 66f, x);
                int length = Mathf.RoundToInt(Mathf.Lerp(1f, 4f, t));
                int top = Mathf.RoundToInt(CenterY - 3f + t);
                for (int k = 0; k <= length; k++)
                {
                    Color c = k == length ? FinEdge : (x % 2 == 0 ? FinRay : FinOrange);
                    Paint(p, x, top - k, c);
                }
            }
        }

        private static void Paint(Color[,] p, int x, int y, Color color)
        {
            PixelArt.Set(p, x, y, color);
        }

        private static float Sample(Vector2[] points, float x)
        {
            if (x < points[0].x || x > points[points.Length - 1].x)
            {
                return 0f;
            }

            for (int i = 0; i < points.Length - 1; i++)
            {
                if (x <= points[i + 1].x)
                {
                    float t = Mathf.InverseLerp(points[i].x, points[i + 1].x, x);
                    return Mathf.Lerp(points[i].y, points[i + 1].y, t);
                }
            }

            return points[points.Length - 1].y;
        }
    }
}
