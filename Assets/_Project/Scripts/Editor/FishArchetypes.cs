using FishAndChill.Collection;
using UnityEngine;
using static FishAndChill.Editor.DetailedFish;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Dessin d'un poisson à partir de sa ligne de la table des espèces (colonnes Dessin, Dos, Flancs, Ventre, Nageoires,
    /// Motif, CouleurMotif, Queue, Oeil, Extras), sans écrire de code : une silhouette de base (fuseau, haut, allonge,
    /// serpent, rond, disque, poisson-chat), puis les couleurs, le motif et les détails. Même moteur et même style que les
    /// fiches dessinées à la main (<see cref="DetailedFish"/>), qui restent prioritaires quand elles existent.
    /// </summary>
    public static class FishArchetypes
    {
        public static readonly string[] Names = { "fuseau", "haut", "allonge", "serpent", "rond", "disque", "poissonchat" };

        private static Vector2[] V(params (int x, float h)[] points)
        {
            var result = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                result[i] = new Vector2(points[i].x, points[i].h);
            }

            return result;
        }

        private static Color Dark(Color c, float t = 0.4f) => Color.Lerp(c, Color.black, t);
        private static Color Light(Color c, float t = 0.35f) => Color.Lerp(c, Color.white, t);

        public static bool TryBuild(SpeciesRow row, out DetailedFish fish)
        {
            fish = null;
            if (row == null || string.IsNullOrEmpty(row.Drawing) || row.Kind != CreatureKind.Fish)
            {
                return false;
            }

            fish = Base(row.Drawing);
            if (fish == null)
            {
                return false;
            }

            Color fin = row.Fin;
            Color ray = Dark(fin);
            Color edge = Light(fin, 0.3f);
            fish.Back = row.Back;
            fish.Flank = row.Flank;
            fish.Belly = row.Belly;
            fish.Iris = row.Eye;
            fish.Seed = Mathf.Abs(row.Id.GetHashCode() % 997) + 200;

            if (fish.Caudal != null)
            {
                fish.Caudal.Base = fin;
                fish.Caudal.Ray = ray;
                fish.Caudal.Edge = edge;
                switch (row.Tail)
                {
                    case "fourchue": fish.Caudal.Shape = TailShape.Forked; break;
                    case "croissant": fish.Caudal.Shape = TailShape.Lunate; break;
                    case "droite": fish.Caudal.Shape = TailShape.Truncate; break;
                    case "ronde": fish.Caudal.Shape = TailShape.Round; break;
                }
            }

            bool spiny = row.HasExtra("epines");
            foreach (Fin f in fish.Fins)
            {
                f.Base = f.Side == FinSide.Down && f.HeightFront < 4f ? Light(fin, 0.15f) : fin;
                f.Ray = ray;
                f.Edge = edge;
                if (f.Side == FinSide.Up && f.XFront - f.XRear > 12) f.Spiny = spiny;
            }

            foreach (Pectoral p in fish.PectoralFins)
            {
                p.Base = Light(fin, 0.25f);
                p.Ray = ray;
                p.Edge = Light(fin, 0.55f);
            }

            ApplyPattern(fish, row);
            ApplyExtras(fish, row);
            return true;
        }

        /// <summary>La silhouette de base : profils, nageoires, œil, ouïes, bouche.</summary>
        private static DetailedFish Base(string name)
        {
            DetailedFish f;
            switch (name)
            {
                case "fuseau":
                    f = new DetailedFish
                    {
                        Width = 96, Height = 34, CenterY = 16.5f,
                        Upper = V((14, 2.5f), (22, 4.5f), (34, 8.5f), (50, 10f), (66, 9.5f), (78, 7.5f), (86, 4.5f), (92, 2f)),
                        Lower = V((14, 2.5f), (22, 4.5f), (34, 8f), (50, 9.5f), (66, 9f), (78, 7f), (86, 4.5f), (92, 1.5f)),
                        Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 11f },
                        EyeX = 84, EyeYOffset = 2.5f, EyeRadius = 2, GillX = 76, MouthXStart = 86, MouthXEnd = 92, MouthYOffset = -0.5f,
                    };
                    f.Fins.Add(new Fin { Side = FinSide.Up, XRear = 48, XFront = 62, HeightRear = 3f, HeightFront = 8f });
                    f.Fins.Add(new Fin { Side = FinSide.Up, XRear = 26, XFront = 38, HeightRear = 2f, HeightFront = 5f });
                    f.Fins.Add(new Fin { Side = FinSide.Down, XRear = 26, XFront = 36, HeightRear = 2f, HeightFront = 5f });
                    f.Fins.Add(new Fin { Side = FinSide.Down, XRear = 52, XFront = 58, HeightRear = 1f, HeightFront = 5f });
                    f.PectoralFins.Add(new Pectoral { BaseX = 74, BaseYOffset = -2f, Length = 7, Width = 2f, Slope = 0.25f });
                    return f;

                case "haut":
                    f = new DetailedFish
                    {
                        Width = 88, Height = 44, CenterY = 20.5f,
                        Upper = V((14, 3f), (22, 6f), (34, 12f), (48, 15f), (62, 14.5f), (72, 12f), (80, 7.5f), (86, 3f)),
                        Lower = V((14, 3f), (22, 5.5f), (34, 10f), (48, 12f), (62, 11.5f), (72, 9.5f), (80, 6f), (86, 2.5f)),
                        Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 13f },
                        EyeX = 78, EyeYOffset = 4.5f, EyeRadius = 2, GillX = 70, MouthXStart = 82, MouthXEnd = 87, MouthYOffset = -1f,
                    };
                    f.Fins.Add(new Fin { Side = FinSide.Up, XRear = 26, XFront = 66, HeightRear = 5f, HeightFront = 8f, Shape = FinShape.Rounded });
                    f.Fins.Add(new Fin { Side = FinSide.Down, XRear = 24, XFront = 36, HeightRear = 3f, HeightFront = 6f });
                    f.PectoralFins.Add(new Pectoral { BaseX = 68, BaseYOffset = -3f, Length = 10, Width = 2.5f, Slope = 0.3f });
                    return f;

                case "allonge":
                    f = new DetailedFish
                    {
                        Width = 108, Height = 30, CenterY = 14.5f,
                        Upper = V((14, 2.5f), (24, 4f), (40, 6.5f), (60, 7.5f), (80, 7f), (92, 5.5f), (100, 3.5f), (105, 1.5f)),
                        Lower = V((14, 2.5f), (24, 4f), (40, 6.5f), (60, 7.5f), (80, 7f), (92, 5.5f), (100, 3f), (105, 1f)),
                        Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 10f },
                        EyeX = 96, EyeYOffset = 2f, EyeRadius = 2, GillX = 86, MouthXStart = 96, MouthXEnd = 106, MouthYOffset = -0.5f,
                    };
                    f.Fins.Add(new Fin { Side = FinSide.Up, XRear = 24, XFront = 34, HeightRear = 3f, HeightFront = 6f });
                    f.Fins.Add(new Fin { Side = FinSide.Down, XRear = 24, XFront = 34, HeightRear = 3f, HeightFront = 5f });
                    f.Fins.Add(new Fin { Side = FinSide.Down, XRear = 56, XFront = 62, HeightRear = 1f, HeightFront = 4f });
                    f.PectoralFins.Add(new Pectoral { BaseX = 84, BaseYOffset = -2f, Length = 7, Width = 2f, Slope = 0.25f });
                    return f;

                case "serpent":
                    f = new DetailedFish
                    {
                        Width = 116, Height = 22, CenterY = 10.5f,
                        Upper = V((2, 0.8f), (14, 2.5f), (34, 4f), (60, 5f), (84, 5f), (100, 4.5f), (108, 3.5f), (113, 2f)),
                        Lower = V((2, 0.8f), (14, 2.5f), (34, 4f), (60, 5f), (84, 5f), (100, 4.5f), (108, 3f), (113, 1.5f)),
                        ScaleTexture = false,
                        EyeX = 108, EyeYOffset = 1.5f, EyeRadius = 1, GillX = 100, GillStrength = 0.2f, MouthXStart = 110, MouthXEnd = 114, MouthYOffset = -0.5f,
                    };
                    f.Fins.Add(new Fin { Side = FinSide.Up, XRear = 2, XFront = 64, HeightRear = 2f, HeightFront = 2.5f, Shape = FinShape.Rounded });
                    f.Fins.Add(new Fin { Side = FinSide.Down, XRear = 2, XFront = 56, HeightRear = 2f, HeightFront = 2.5f, Shape = FinShape.Rounded });
                    f.PectoralFins.Add(new Pectoral { BaseX = 98, BaseYOffset = -1f, Length = 4, Width = 2f, Slope = 0.1f });
                    return f;

                case "rond":
                    f = new DetailedFish
                    {
                        Width = 84, Height = 46, CenterY = 22.5f,
                        Upper = V((14, 3f), (22, 6f), (32, 12f), (44, 15.5f), (56, 16f), (66, 14f), (74, 10f), (80, 4f)),
                        Lower = V((14, 3f), (22, 6f), (32, 12f), (44, 15f), (56, 15.5f), (66, 13.5f), (74, 9f), (80, 3f)),
                        Caudal = new Tail { Shape = TailShape.Round, XTip = 3, XBase = 15, Span = 10f },
                        EyeX = 70, EyeYOffset = 6f, EyeRadius = 2, GillX = 62, GillStrength = 0.25f, MouthXStart = 78, MouthXEnd = 81, MouthYOffset = 0f,
                    };
                    f.Fins.Add(new Fin { Side = FinSide.Up, XRear = 20, XFront = 28, HeightRear = 3f, HeightFront = 5f, Shape = FinShape.Rounded });
                    f.Fins.Add(new Fin { Side = FinSide.Down, XRear = 20, XFront = 28, HeightRear = 3f, HeightFront = 5f, Shape = FinShape.Rounded });
                    f.PectoralFins.Add(new Pectoral { BaseX = 60, BaseYOffset = 0f, Length = 6, Width = 3f, Slope = 0f });
                    return f;

                case "disque":
                    f = new DetailedFish
                    {
                        Width = 80, Height = 58, CenterY = 28.5f,
                        Upper = V((14, 3f), (20, 9f), (30, 18f), (42, 21f), (54, 20f), (64, 15f), (72, 8f), (76, 3f)),
                        Lower = V((14, 3f), (20, 9f), (30, 17f), (42, 20f), (54, 19f), (64, 14f), (72, 8f), (76, 3f)),
                        Caudal = new Tail { Shape = TailShape.Truncate, XTip = 3, XBase = 15, Span = 9f },
                        EyeX = 68, EyeYOffset = 6f, EyeRadius = 2, GillX = 62, GillStrength = 0.3f, MouthXStart = 74, MouthXEnd = 77, MouthYOffset = 2f,
                    };
                    f.Fins.Add(new Fin { Side = FinSide.Up, XRear = 18, XFront = 58, HeightRear = 6f, HeightFront = 3f, Shape = FinShape.Rounded });
                    f.Fins.Add(new Fin { Side = FinSide.Down, XRear = 18, XFront = 52, HeightRear = 6f, HeightFront = 3f, Shape = FinShape.Rounded });
                    f.PectoralFins.Add(new Pectoral { BaseX = 60, BaseYOffset = -2f, Length = 6, Width = 2f, Slope = 0.2f });
                    return f;

                case "poissonchat":
                    f = new DetailedFish
                    {
                        Width = 108, Height = 40, CenterY = 18.5f,
                        Upper = V((14, 3f), (24, 4.5f), (40, 8f), (60, 10.5f), (78, 11f), (90, 10f), (100, 7f), (105, 3.5f)),
                        Lower = V((14, 3f), (24, 4f), (40, 7f), (60, 9f), (78, 9.5f), (90, 9f), (100, 6f), (105, 3f)),
                        ScaleTexture = false,
                        Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 12f },
                        EyeX = 97, EyeYOffset = 5f, EyeRadius = 1, GillX = 84, GillStrength = 0.3f, MouthXStart = 99, MouthXEnd = 106, MouthYOffset = -1f,
                    };
                    f.Fins.Add(new Fin { Side = FinSide.Up, XRear = 66, XFront = 78, HeightRear = 4f, HeightFront = 9f });
                    f.Fins.Add(new Fin { Side = FinSide.Up, XRear = 22, XFront = 34, HeightRear = 2f, HeightFront = 3f, Shape = FinShape.Rounded });
                    f.Fins.Add(new Fin { Side = FinSide.Down, XRear = 24, XFront = 38, HeightRear = 3f, HeightFront = 5f, Shape = FinShape.Rounded });
                    f.PectoralFins.Add(new Pectoral { BaseX = 84, BaseYOffset = -5f, Length = 9, Width = 2.5f, Slope = 0.3f });
                    return f;

                default:
                    return null;
            }
        }

        private static void ApplyPattern(DetailedFish fish, SpeciesRow row)
        {
            Color c = row.PatternColor;
            int head = fish.GillX;
            switch (row.Pattern)
            {
                case "bandes":
                {
                    int count = 5;
                    var xs = new int[count];
                    for (int i = 0; i < count; i++) xs[i] = Mathf.RoundToInt(Mathf.Lerp(20f, head - 6f, i / (float)(count - 1)));
                    fish.BodyPatterns.Add(VerticalBars(xs, 3, c, 0.8f, 0.05f));
                    break;
                }

                case "points":
                    fish.Details.Add(Spots(36, 16, head + 6, 0.05f, 0.7f, c, null, fish.Seed + 1, 0.35f));
                    break;

                case "lignes":
                    fish.BodyPatterns.Add(Lines(new[] { 0.25f, 0.4f, 0.55f }, c, 0.7f, 14, head));
                    break;

                case "marbre":
                    fish.Details.Add((f, p) =>
                    {
                        var random = new System.Random(f.Seed + 2);
                        for (int i = 0; i < 16; i++)
                        {
                            int cx = random.Next(18, head);
                            int cy = Mathf.RoundToInt(f.CenterY) + random.Next(-8, 8);
                            for (int x = cx - 2; x <= cx + 2; x++)
                            {
                                for (int y = cy - 1; y <= cy + 1; y++)
                                {
                                    if (f.InBody(x, y) && (x + y + i) % 4 != 0) p[x, y] = Color.Lerp(p[x, y], c, 0.7f);
                                }
                            }
                        }
                    });
                    break;

                case "ocelle":
                    fish.Details.Add((f, p) =>
                    {
                        int cx = 19;
                        int cy = Mathf.RoundToInt(f.CenterY + 1f);
                        for (int x = cx - 4; x <= cx + 4; x++)
                        {
                            for (int y = cy - 4; y <= cy + 4; y++)
                            {
                                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                                if (d > 3.1f || !PixelArt.IsFilled(p, x, y)) continue;
                                p[x, y] = d <= 2f ? new Color(0.08f, 0.06f, 0.05f) : c;
                            }
                        }
                    });
                    break;
            }
        }

        private static void ApplyExtras(DetailedFish fish, SpeciesRow row)
        {
            if (row.HasExtra("adipeuse"))
            {
                fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 20, XFront = 24, HeightRear = 1.5f, HeightFront = 3f, Shape = FinShape.Rounded, Base = row.Fin, Ray = row.Fin, Edge = Light(row.Fin) });
            }

            if (row.HasExtra("pinnules"))
            {
                Color yellow = new Color(0.94f, 0.82f, 0.30f);
                fish.Details.Add((f, p) =>
                {
                    for (int x = 40; x >= 18; x -= 3)
                    {
                        int top = Mathf.RoundToInt(f.CenterY + f.UpperAt(x));
                        int bottom = Mathf.RoundToInt(f.CenterY - f.LowerAt(x));
                        PixelArt.Set(p, x, top + 1, yellow);
                        PixelArt.Set(p, x, bottom - 1, yellow);
                    }
                });
            }

            if (row.HasExtra("barbillons") || row.Drawing == "poissonchat")
            {
                Color barbel = Light(row.Belly, 0.2f);
                fish.Details.Add((f, p) =>
                {
                    int mx = f.MouthXEnd - 2;
                    int my = Mathf.RoundToInt(f.CenterY + f.MouthYOffset);
                    for (int b = 0; b < 2; b++)
                    {
                        for (int k = 0; k <= 20; k++)
                        {
                            float t = k / 20f;
                            int x = Mathf.RoundToInt(mx - t * (16f - b * 6f));
                            int y = Mathf.RoundToInt(my - b * 2 - t * (8f + b * 2f) + Mathf.Sin(t * Mathf.PI) * 2f);
                            PixelArt.Set(p, x, y, barbel);
                        }
                    }
                });
            }

            if (row.HasExtra("dents"))
            {
                fish.Details.Add((f, p) =>
                {
                    int y = Mathf.RoundToInt(f.CenterY + f.MouthYOffset);
                    for (int x = f.MouthXStart; x <= f.MouthXEnd; x += 2)
                    {
                        PixelArt.Set(p, x, y - 1, new Color(0.98f, 0.98f, 0.94f));
                    }
                });
            }
        }
    }
}
