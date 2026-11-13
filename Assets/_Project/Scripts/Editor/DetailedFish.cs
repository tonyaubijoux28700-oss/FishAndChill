using System;
using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Moteur de dessin de poissons en pixel art détaillé (vus de profil, tête à droite).
    /// Une espèce = une fiche : silhouette (profils dos / ventre), dégradé, nageoires, queue, tête, motifs.
    /// </summary>
    public sealed class DetailedFish
    {
        public enum FinSide { Up, Down }
        public enum FinShape { Linear, Falcate, Rounded }
        public enum TailShape { Forked, Lunate, Truncate, Round }

        public sealed class Fin
        {
            public FinSide Side;
            public int XRear;
            public int XFront;
            public float HeightRear;
            public float HeightFront;
            public FinShape Shape = FinShape.Linear;
            public Color Base;
            public Color Ray;
            public Color Edge;
            public bool Spiny;
        }

        public sealed class Pectoral
        {
            public int BaseX;
            public float BaseYOffset;
            public int Length = 6;
            public float Width = 2.5f;
            public float Slope = 0.3f;
            public Color Base;
            public Color Ray;
            public Color Edge;
        }

        public sealed class Tail
        {
            public TailShape Shape = TailShape.Forked;
            public int XTip = 1;
            public int XBase = 14;
            public float Span = 10f;
            public Color Base;
            public Color Ray;
            public Color Edge;
            public Color? BaseTint;
        }

        public int Width = 96;
        public int Height = 32;
        public float CenterY = 15.5f;
        public Vector2[] Upper;
        public Vector2[] Lower;

        public Color Back;
        public Color Flank;
        public Color Belly;
        public float FlankStart = 0.35f;
        public float BellyStart = 0.68f;
        public bool ScaleTexture = true;
        public bool BackShine = true;

        public Tail Caudal;
        public List<Fin> Fins = new List<Fin>();
        public List<Pectoral> PectoralFins = new List<Pectoral>();

        public int EyeX;
        public float EyeYOffset = 2f;
        public int EyeRadius = 1;
        public Color Iris = new Color(0.85f, 0.75f, 0.35f);
        public Color Pupil = new Color(0.05f, 0.05f, 0.06f);

        public int GillX;
        public float GillStrength = 0.35f;
        public Color? GillTint;

        public int MouthXStart;
        public int MouthXEnd;
        public float MouthYOffset = -0.5f;
        public float MouthSlope;
        public Color MouthColor = new Color(0.12f, 0.10f, 0.08f);

        public Color OutlineColor = new Color(0.08f, 0.09f, 0.10f);
        public int Seed = 1;

        /// <summary>Motifs du corps : (x, y, t de 0 = dos à 1 = ventre, couleur actuelle) → nouvelle couleur.</summary>
        public List<Func<int, int, float, Color, Color>> BodyPatterns = new List<Func<int, int, float, Color, Color>>();

        /// <summary>Détails peints après le corps et la tête (bec, dents, écussons…).</summary>
        public List<Action<DetailedFish, Color[,]>> Details = new List<Action<DetailedFish, Color[,]>>();

        // ---------- Géométrie ----------

        public float UpperAt(float x) => Sample(Upper, x);
        public float LowerAt(float x) => Sample(Lower, x);

        public bool InBody(int x, int y)
        {
            float up = UpperAt(x);
            float down = LowerAt(x);
            if (up <= 0f && down <= 0f)
            {
                return false;
            }

            float dy = y - CenterY;
            return dy <= up && dy >= -down;
        }

        /// <summary>Position verticale relative dans le corps : 0 = dos, 1 = ventre.</summary>
        public float BodyT(int x, int y)
        {
            float top = CenterY + UpperAt(x);
            float bottom = CenterY - LowerAt(x);
            return Mathf.InverseLerp(top, bottom, y);
        }

        public Color[,] Draw()
        {
            var p = new Color[Width, Height];
            if (Caudal != null)
            {
                DrawTail(p);
            }

            foreach (Fin fin in Fins)
            {
                DrawFin(p, fin);
            }

            DrawBody(p);
            DrawHead(p);

            foreach (Pectoral pectoral in PectoralFins)
            {
                DrawPectoral(p, pectoral);
            }

            foreach (var detail in Details)
            {
                detail(this, p);
            }

            DrawEye(p);
            PixelArt.Outline(p, OutlineColor);
            return p;
        }

        private void DrawBody(Color[,] p)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (!InBody(x, y))
                    {
                        continue;
                    }

                    float t = BodyT(x, y);
                    Color c = t < FlankStart
                        ? Color.Lerp(Back, Flank, t / FlankStart)
                        : t < BellyStart ? Flank : Color.Lerp(Flank, Belly, (t - BellyStart) / (1f - BellyStart));

                    if (ScaleTexture && t < 0.8f && (x + 2 * y) % 4 == 0)
                    {
                        c = Color.Lerp(c, Color.black, 0.09f);
                    }

                    if (BackShine && t > 0.08f && t < 0.2f && (x % 7) != 0)
                    {
                        c = Color.Lerp(c, Color.white, 0.07f);
                    }

                    foreach (var pattern in BodyPatterns)
                    {
                        c = pattern(x, y, t, c);
                    }

                    p[x, y] = c;
                }
            }
        }

        private void DrawHead(Color[,] p)
        {
            if (GillX > 0)
            {
                float up = UpperAt(GillX);
                float down = LowerAt(GillX);
                for (int y = Mathf.RoundToInt(CenterY - down * 0.85f); y <= Mathf.RoundToInt(CenterY + up * 0.7f); y++)
                {
                    float dy = y - CenterY;
                    int x = Mathf.RoundToInt(GillX + dy * dy * 0.05f);
                    if (InBody(x, y))
                    {
                        p[x, y] = Color.Lerp(p[x, y], Color.black, GillStrength);
                    }

                    if (GillTint.HasValue)
                    {
                        for (int k = 1; k <= 4; k++)
                        {
                            if (InBody(x + k, y) && dy < up * 0.3f)
                            {
                                p[x + k, y] = Color.Lerp(p[x + k, y], GillTint.Value, 0.3f);
                            }
                        }
                    }
                }
            }

            if (MouthXEnd > MouthXStart)
            {
                for (int x = MouthXStart; x <= MouthXEnd; x++)
                {
                    int y = Mathf.RoundToInt(CenterY + MouthYOffset - (x - MouthXStart) * MouthSlope);
                    if (InBody(x, y))
                    {
                        p[x, y] = MouthColor;
                    }
                }
            }
        }

        private void DrawEye(Color[,] p)
        {
            int ey = Mathf.RoundToInt(CenterY + EyeYOffset);
            int r = EyeRadius;
            for (int x = EyeX - r; x <= EyeX + r; x++)
            {
                for (int y = ey - r; y <= ey + r; y++)
                {
                    if (r >= 2 && Mathf.Abs(x - EyeX) == r && Mathf.Abs(y - ey) == r)
                    {
                        continue;
                    }

                    PixelArt.Set(p, x, y, Iris);
                }
            }

            PixelArt.Set(p, EyeX, ey, Pupil);
            if (r >= 2)
            {
                PixelArt.Set(p, EyeX + 1, ey, Pupil);
                PixelArt.Set(p, EyeX, ey - 1, Pupil);
                PixelArt.Set(p, EyeX + 1, ey - 1, Pupil);
            }
            else
            {
                PixelArt.Set(p, EyeX + 1, ey, Pupil);
            }

            PixelArt.Set(p, EyeX - r + (r >= 2 ? 1 : 0), ey + r - (r >= 2 ? 1 : 0), Color.white);
        }

        private void DrawFin(Color[,] p, Fin fin)
        {
            for (int x = fin.XRear; x <= fin.XFront; x++)
            {
                float t = Mathf.InverseLerp(fin.XRear, fin.XFront, x);
                float height;
                switch (fin.Shape)
                {
                    case FinShape.Falcate:
                        height = Mathf.Lerp(fin.HeightRear, fin.HeightFront, t * t * t);
                        break;
                    case FinShape.Rounded:
                        height = Mathf.Lerp(fin.HeightRear, fin.HeightFront, t) + Mathf.Sin(t * Mathf.PI) * 1.5f;
                        break;
                    default:
                        height = Mathf.Lerp(fin.HeightRear, fin.HeightFront, t);
                        break;
                }

                // Fondu à l'avant et à l'arrière pour que la nageoire sorte du corps.
                height *= Mathf.Clamp01((x - fin.XRear + 0.5f) / 1.5f);
                if (fin.Spiny && x % 3 == 0)
                {
                    height -= 1f;
                }

                int h = Mathf.RoundToInt(height);
                float baseY = fin.Side == FinSide.Up ? CenterY + UpperAt(x) - 0.5f : CenterY - LowerAt(x) + 0.5f;
                int direction = fin.Side == FinSide.Up ? 1 : -1;
                for (int k = 0; k <= h; k++)
                {
                    Color c = k == h ? fin.Edge : (x % 2 == 0 ? fin.Ray : fin.Base);
                    PixelArt.Set(p, x, Mathf.RoundToInt(baseY + k * direction), c);
                }
            }
        }

        private void DrawPectoral(Color[,] p, Pectoral fin)
        {
            float baseY = CenterY + fin.BaseYOffset;
            for (int k = 0; k <= fin.Length; k++)
            {
                float half = fin.Width * (1f - k / (float)(fin.Length + 1));
                int x = fin.BaseX - k;
                float cy = baseY - k * fin.Slope;
                for (int y = Mathf.RoundToInt(cy - half); y <= Mathf.RoundToInt(cy + half); y++)
                {
                    bool edge = k == fin.Length || y == Mathf.RoundToInt(cy - half);
                    PixelArt.Set(p, x, y, edge ? fin.Edge : (y % 2 == 0 ? fin.Ray : fin.Base));
                }
            }
        }

        private void DrawTail(Color[,] p)
        {
            Tail tail = Caudal;
            for (int x = tail.XTip; x <= tail.XBase; x++)
            {
                float t = Mathf.InverseLerp(tail.XBase, tail.XTip, x);
                float span = 1.5f + t * tail.Span;
                for (int y = 0; y < Height; y++)
                {
                    float dy = y - CenterY;
                    float ady = Mathf.Abs(dy);
                    bool inside = ady <= span;
                    switch (tail.Shape)
                    {
                        case TailShape.Forked:
                            inside &= t < 0.45f || ady >= (t - 0.45f) * span * 1.5f;
                            break;
                        case TailShape.Lunate:
                            inside &= t < 0.35f || ady >= (t - 0.35f) * (t - 0.35f) * span * 3.2f;
                            break;
                        case TailShape.Round:
                            inside &= t < 0.6f || ady <= span * Mathf.Sqrt(Mathf.Max(0f, 1f - (t - 0.6f) * (t - 0.6f) * 6f));
                            break;
                    }

                    if (!inside)
                    {
                        continue;
                    }

                    Color c = tail.Base;
                    if (tail.BaseTint.HasValue && t < 0.35f)
                    {
                        c = Color.Lerp(tail.BaseTint.Value, tail.Base, t / 0.35f);
                    }

                    float angle = Mathf.Atan2(dy, tail.XBase - x + 0.5f);
                    if (Mathf.Abs(Mathf.Sin(angle * 9f)) < 0.22f && t > 0.15f)
                    {
                        c = tail.Ray;
                    }

                    if (ady > span - 1.2f && t > 0.3f)
                    {
                        c = tail.Edge;
                    }

                    p[x, y] = c;
                }
            }
        }

        private static float Sample(Vector2[] points, float x)
        {
            if (points == null || x < points[0].x || x > points[points.Length - 1].x)
            {
                return 0f;
            }

            for (int i = 0; i < points.Length - 1; i++)
            {
                if (x <= points[i + 1].x)
                {
                    return Mathf.Lerp(points[i].y, points[i + 1].y, Mathf.InverseLerp(points[i].x, points[i + 1].x, x));
                }
            }

            return points[points.Length - 1].y;
        }

        // ---------- Motifs réutilisables ----------

        /// <summary>Bandes verticales (sergent-major, chirurgien bagnard) de la ligne du dos jusqu'à <paramref name="maxT"/>.</summary>
        public static Func<int, int, float, Color, Color> VerticalBars(int[] xs, int width, Color color, float maxT, float slant = 0f)
        {
            return (x, y, t, c) =>
            {
                if (t > maxT)
                {
                    return c;
                }

                foreach (int bx in xs)
                {
                    float shifted = x - (y * slant);
                    if (shifted >= bx && shifted < bx + width)
                    {
                        // Bords de la bande un peu adoucis vers le ventre.
                        float fade = Mathf.InverseLerp(maxT, maxT - 0.12f, t);
                        return Color.Lerp(c, color, Mathf.Lerp(0.55f, 1f, fade));
                    }
                }

                return c;
            };
        }

        /// <summary>Points aléatoires (taille 1 ou 2) dans une zone du corps.</summary>
        public static Action<DetailedFish, Color[,]> Spots(int count, int xMin, int xMax, float tMin, float tMax, Color color, Color? shadow, int seed, float bigChance = 0.3f)
        {
            return (fish, p) =>
            {
                var random = new System.Random(seed);
                for (int i = 0; i < count; i++)
                {
                    int x = random.Next(xMin, xMax);
                    int y = random.Next(0, fish.Height);
                    for (int attempt = 0; attempt < 20 && !(fish.InBody(x, y) && fish.BodyT(x, y) >= tMin && fish.BodyT(x, y) <= tMax); attempt++)
                    {
                        y = random.Next(0, fish.Height);
                    }

                    if (!fish.InBody(x, y))
                    {
                        continue;
                    }

                    p[x, y] = color;
                    if (random.NextDouble() < bigChance && fish.InBody(x + 1, y))
                    {
                        p[x + 1, y] = color;
                    }

                    if (shadow.HasValue && fish.InBody(x, y - 1))
                    {
                        p[x, y - 1] = Color.Lerp(p[x, y - 1], shadow.Value, 0.6f);
                    }
                }
            };
        }

        /// <summary>Lignes horizontales le long du corps, à des positions relatives t (0 = dos).</summary>
        public static Func<int, int, float, Color, Color> Lines(float[] ts, Color color, float strength, int xMin, int xMax)
        {
            return (x, y, t, c) =>
            {
                if (x < xMin || x > xMax)
                {
                    return c;
                }

                foreach (float lt in ts)
                {
                    if (Mathf.Abs(t - lt) < 0.045f)
                    {
                        return Color.Lerp(c, color, strength);
                    }
                }

                return c;
            };
        }
    }
}
