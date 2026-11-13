using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Dessins provisoires des poissons en pixel art (48×24, vus de profil, tête à droite).
    /// Chaque espèce a sa silhouette, ses couleurs et ses motifs inspirés du vrai poisson.
    /// </summary>
    public static class FishArt
    {
        private const string Folder = "Assets/_Project/Art/Fish";
        private const int Width = 48;
        private const int Height = 24;

        public enum Pattern
        {
            None,
            VerticalBars,
            Spots,
            Stripe,
        }

        public enum Tail
        {
            Forked,
            Truncate,
            Round,
        }

        public sealed class Look
        {
            public Color Body;
            public Color Belly;
            public Color Fin;
            public Color PatternColor;
            public Pattern Pattern;
            public int PatternCount;
            public float HalfLength = 14f;
            public float HalfHeight = 6.5f;
            public Tail Tail = Tail.Forked;
            public bool PointedSnout;
            public bool Beak;
        }

        private static readonly Dictionary<string, Look> Looks = new Dictionary<string, Look>
        {
            ["abudefduf-vaigiensis"] = new Look
            {
                Body = new Color(0.93f, 0.90f, 0.62f), Belly = new Color(0.97f, 0.97f, 0.90f), Fin = new Color(0.88f, 0.84f, 0.55f),
                PatternColor = new Color(0.12f, 0.12f, 0.14f), Pattern = Pattern.VerticalBars, PatternCount = 5,
                HalfLength = 12.5f, HalfHeight = 7.5f, Tail = Tail.Forked,
            },
            ["acanthurus-triostegus"] = new Look
            {
                Body = new Color(0.80f, 0.86f, 0.78f), Belly = new Color(0.94f, 0.96f, 0.92f), Fin = new Color(0.74f, 0.80f, 0.72f),
                PatternColor = new Color(0.10f, 0.10f, 0.12f), Pattern = Pattern.VerticalBars, PatternCount = 6,
                HalfLength = 12.5f, HalfHeight = 8f, Tail = Tail.Truncate,
            },
            ["scarus-psittacus"] = new Look
            {
                Body = new Color(0.25f, 0.68f, 0.66f), Belly = new Color(0.55f, 0.82f, 0.74f), Fin = new Color(0.93f, 0.50f, 0.62f),
                PatternColor = new Color(0.93f, 0.55f, 0.65f), Pattern = Pattern.Stripe,
                HalfLength = 14f, HalfHeight = 6.5f, Tail = Tail.Truncate, Beak = true,
            },
            ["cephalopholis-argus"] = new Look
            {
                Body = new Color(0.20f, 0.23f, 0.42f), Belly = new Color(0.26f, 0.29f, 0.48f), Fin = new Color(0.16f, 0.18f, 0.36f),
                PatternColor = new Color(0.55f, 0.78f, 1.00f), Pattern = Pattern.Spots, PatternCount = 26,
                HalfLength = 15f, HalfHeight = 6.5f, Tail = Tail.Round,
            },
            ["caranx-melampygus"] = new Look
            {
                Body = new Color(0.68f, 0.68f, 0.60f), Belly = new Color(0.88f, 0.90f, 0.88f), Fin = new Color(0.25f, 0.45f, 0.85f),
                PatternColor = new Color(0.16f, 0.30f, 0.62f), Pattern = Pattern.Spots, PatternCount = 10,
                HalfLength = 15f, HalfHeight = 7f, Tail = Tail.Forked,
            },
            ["albula-glossodonta"] = new Look
            {
                Body = new Color(0.76f, 0.81f, 0.86f), Belly = new Color(0.95f, 0.96f, 0.98f), Fin = new Color(0.68f, 0.74f, 0.80f),
                PatternColor = new Color(0.62f, 0.68f, 0.76f), Pattern = Pattern.Stripe,
                HalfLength = 17f, HalfHeight = 4.5f, Tail = Tail.Forked, PointedSnout = true,
            },
            ["sphyraena-barracuda"] = new Look
            {
                Body = new Color(0.60f, 0.66f, 0.70f), Belly = new Color(0.92f, 0.94f, 0.95f), Fin = new Color(0.40f, 0.45f, 0.50f),
                PatternColor = new Color(0.16f, 0.19f, 0.23f), Pattern = Pattern.Spots, PatternCount = 9,
                HalfLength = 20f, HalfHeight = 3.5f, Tail = Tail.Forked, PointedSnout = true,
            },
            ["caranx-ignobilis"] = new Look
            {
                Body = new Color(0.42f, 0.44f, 0.48f), Belly = new Color(0.70f, 0.72f, 0.75f), Fin = new Color(0.24f, 0.26f, 0.30f),
                Pattern = Pattern.None, HalfLength = 15f, HalfHeight = 8.5f, Tail = Tail.Forked,
            },
        };

        /// <summary>Dessine le poisson et renvoie son sprite. Espèce inconnue : forme générique à sa couleur provisoire.</summary>
        public static Sprite Generate(string speciesId, Color fallbackColor)
        {
            // Dessins détaillés faits sur mesure (plus grands que le format générique).
            if (speciesId == "esox-lucius")
            {
                return PixelArt.Save($"{Folder}/{speciesId}.png", PikeArt.Draw(), 16, new Vector2(0.5f, 0.5f));
            }

            if (MahanaFishArt.TryGet(speciesId, out DetailedFish detailed))
            {
                return PixelArt.Save($"{Folder}/{speciesId}.png", detailed.Draw(), 16, new Vector2(0.5f, 0.5f));
            }

            if (!Looks.TryGetValue(speciesId, out Look look))
            {
                look = new Look
                {
                    Body = fallbackColor,
                    Belly = Color.Lerp(fallbackColor, Color.white, 0.5f),
                    Fin = Color.Lerp(fallbackColor, Color.black, 0.2f),
                };
            }

            // Graine stable (string.GetHashCode peut changer d'une exécution à l'autre).
            int seed = 17;
            foreach (char c in speciesId)
            {
                seed = seed * 31 + c;
            }

            Color[,] pixels = Draw(look, seed);
            PixelArt.Outline(pixels, Color.Lerp(look.Body, Color.black, 0.75f));
            return PixelArt.Save($"{Folder}/{speciesId}.png", pixels, 16, new Vector2(0.5f, 0.5f));
        }

        private static Color[,] Draw(Look look, int seed)
        {
            var p = new Color[Width, Height];
            float cx = 26f;
            float cy = Height / 2f - 0.5f;
            float a = look.HalfLength;
            float b = look.HalfHeight;
            float tailLength = 7f;
            float tailStart = cx - a + 1.5f;

            // Nageoire dorsale (au-dessus du corps) et anale (en dessous).
            for (int x = Mathf.RoundToInt(cx - a * 0.55f); x <= Mathf.RoundToInt(cx + a * 0.25f); x++)
            {
                float t = Mathf.InverseLerp(cx - a * 0.55f, cx + a * 0.25f, x);
                int fin = Mathf.RoundToInt(Mathf.Lerp(1f, 3.5f, Mathf.Sin(t * Mathf.PI)));
                for (int k = 0; k <= fin; k++)
                {
                    PixelArt.Set(p, x, Mathf.RoundToInt(cy + BodyHalfHeight(look, x, cx, a, b) + k), look.Fin);
                }
            }

            for (int x = Mathf.RoundToInt(cx - a * 0.5f); x <= Mathf.RoundToInt(cx - a * 0.1f); x++)
            {
                for (int k = 0; k <= 2; k++)
                {
                    PixelArt.Set(p, x, Mathf.RoundToInt(cy - BodyHalfHeight(look, x, cx, a, b) - k), look.Fin);
                }
            }

            // Queue.
            for (int x = Mathf.FloorToInt(tailStart - tailLength); x <= Mathf.CeilToInt(tailStart); x++)
            {
                float t = Mathf.Clamp01((tailStart - x) / tailLength);
                float span = 1.2f + t * b * 1.05f;
                for (int y = 0; y < Height; y++)
                {
                    float dy = Mathf.Abs(y - cy);
                    bool inside = dy <= span;
                    if (look.Tail == Tail.Forked && t > 0.45f)
                    {
                        inside &= dy >= (t - 0.45f) * span * 1.4f;
                    }
                    else if (look.Tail == Tail.Round && t > 0.7f)
                    {
                        inside &= dy <= span * (1f - (t - 0.7f) * 2.2f);
                    }

                    if (inside)
                    {
                        PixelArt.Set(p, x, y, look.Fin);
                    }
                }
            }

            // Corps : dégradé du dos vers le ventre.
            for (int x = 0; x < Width; x++)
            {
                float half = BodyHalfHeight(look, x, cx, a, b);
                for (int y = 0; y < Height; y++)
                {
                    float dy = y - cy;
                    if (half > 0f && Mathf.Abs(dy) <= half)
                    {
                        float belly = Mathf.InverseLerp(half * 0.2f, -half, dy);
                        p[x, y] = Color.Lerp(look.Body, look.Belly, belly);
                    }
                }
            }

            DrawPattern(p, look, cx, cy, a, b, seed);

            // Bec du perroquet.
            if (look.Beak)
            {
                for (int x = Mathf.RoundToInt(cx + a - 2.5f); x <= Mathf.RoundToInt(cx + a); x++)
                {
                    for (int y = Mathf.RoundToInt(cy - 1.5f); y <= Mathf.RoundToInt(cy + 0.5f); y++)
                    {
                        if (PixelArt.IsFilled(p, x, y))
                        {
                            p[x, y] = new Color(0.90f, 0.92f, 0.85f);
                        }
                    }
                }
            }

            // Œil.
            int eyeX = Mathf.RoundToInt(cx + a * (look.PointedSnout ? 0.62f : 0.66f));
            int eyeY = Mathf.RoundToInt(cy + b * 0.25f);
            PixelArt.Rect(p, eyeX - 1, eyeY - 1, eyeX, eyeY, Color.white);
            PixelArt.Set(p, eyeX, eyeY - 1, new Color(0.05f, 0.05f, 0.08f));

            // Ouïe.
            int gillX = Mathf.RoundToInt(cx + a * 0.45f);
            for (int y = Mathf.RoundToInt(cy - b * 0.4f); y <= Mathf.RoundToInt(cy + b * 0.3f); y++)
            {
                if (PixelArt.IsFilled(p, gillX, y))
                {
                    p[gillX, y] = Color.Lerp(p[gillX, y], Color.black, 0.25f);
                }
            }

            return p;
        }

        /// <summary>Demi-hauteur du corps à la colonne x (0 = hors du corps).</summary>
        private static float BodyHalfHeight(Look look, float x, float cx, float a, float b)
        {
            float nx = (x - cx) / a;
            if (Mathf.Abs(nx) > 1f)
            {
                return 0f;
            }

            float half = b * Mathf.Sqrt(1f - nx * nx);
            if (look.PointedSnout && nx > 0.35f)
            {
                // Tête effilée (barracuda, banane de mer) : le corps s'amincit en pointe.
                half *= Mathf.Lerp(1f, 0.25f, (nx - 0.35f) / 0.65f);
            }

            return half;
        }

        private static void DrawPattern(Color[,] p, Look look, float cx, float cy, float a, float b, int seed)
        {
            switch (look.Pattern)
            {
                case Pattern.VerticalBars:
                    for (int i = 0; i < look.PatternCount; i++)
                    {
                        int x = Mathf.RoundToInt(Mathf.Lerp(cx - a * 0.75f, cx + a * 0.45f, look.PatternCount == 1 ? 0.5f : i / (float)(look.PatternCount - 1)));
                        for (int y = 0; y < Height; y++)
                        {
                            if (PixelArt.IsFilled(p, x, y) && y > cy - b * 0.6f)
                            {
                                p[x, y] = look.PatternColor;
                            }
                        }
                    }

                    break;

                case Pattern.Spots:
                    var random = new System.Random(seed);
                    for (int i = 0; i < look.PatternCount; i++)
                    {
                        int x = Mathf.RoundToInt(Mathf.Lerp(cx - a * 0.8f, cx + a * 0.5f, (float)random.NextDouble()));
                        int y = Mathf.RoundToInt(Mathf.Lerp(cy - b * 0.8f, cy + b * 0.8f, (float)random.NextDouble()));
                        if (PixelArt.IsFilled(p, x, y))
                        {
                            p[x, y] = look.PatternColor;
                        }
                    }

                    break;

                case Pattern.Stripe:
                    int stripeY = Mathf.RoundToInt(cy);
                    for (int x = Mathf.RoundToInt(cx - a * 0.85f); x <= Mathf.RoundToInt(cx + a * 0.4f); x++)
                    {
                        if (PixelArt.IsFilled(p, x, stripeY))
                        {
                            p[x, stripeY] = look.PatternColor;
                        }
                    }

                    break;
            }
        }
    }
}
