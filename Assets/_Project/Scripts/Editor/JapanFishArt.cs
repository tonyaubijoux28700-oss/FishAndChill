using UnityEngine;
using static FishAndChill.Editor.DetailedFish;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Fiches de dessin détaillé des poissons de la baie japonaise, d'après l'anatomie et la livrée réelles de chaque
    /// espèce (dessins originaux). Crabe-araignée géant : <see cref="JapanCreatureArt"/>.
    /// </summary>
    public static class JapanFishArt
    {
        private static Color C(float r, float g, float b) => new Color(r, g, b);

        private static Vector2[] V(params (int x, float h)[] points)
        {
            var result = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                result[i] = new Vector2(points[i].x, points[i].h);
            }

            return result;
        }

        public static bool TryGet(string speciesId, out DetailedFish fish)
        {
            switch (speciesId)
            {
                case "scomber-japonicus": fish = Mackerel(); return true;
                case "trachurus-japonicus": fish = HorseMackerel(); return true;
                case "sebastes-inermis": fish = Mebaru(); return true;
                case "sebastiscus-marmoratus": fish = Kasago(); return true;
                case "pagrus-major": fish = Madai(); return true;
                case "takifugu-rubripes": fish = Fugu(); return true;
                case "acanthopagrus-schlegelii": fish = Kurodai(); return true;
                case "lateolabrax-japonicus": fish = Suzuki(); return true;
                case "seriola-quinqueradiata": fish = Buri(); return true;
                case "oplegnathus-fasciatus": fish = Ishidai(); return true;
                case "anguilla-japonica": fish = Eel(); return true;
                case "epinephelus-bruneus": fish = Kue(); return true;
                case "thunnus-orientalis": fish = BluefinTuna(); return true;
                default: fish = null; return false;
            }
        }

        /// <summary>Petites pinnules jaunes entre la dorsale (ou l'anale) et la queue (maquereaux, thons).</summary>
        private static System.Action<DetailedFish, Color[,]> Finlets(int xFrom, int xTo, Color color)
        {
            return (f, p) =>
            {
                for (int x = xFrom; x >= xTo; x -= 3)
                {
                    int top = Mathf.RoundToInt(f.CenterY + f.UpperAt(x));
                    int bottom = Mathf.RoundToInt(f.CenterY - f.LowerAt(x));
                    PixelArt.Set(p, x, top + 1, color);
                    PixelArt.Set(p, x - 1, top + 2, color);
                    PixelArt.Set(p, x, bottom - 1, color);
                    PixelArt.Set(p, x - 1, bottom - 2, color);
                }
            };
        }

        /// <summary>Maquereau (saba) : fuseau, dos bleu-vert zébré de lignes sombres ondulées, ventre argenté, pinnules.</summary>
        private static DetailedFish Mackerel()
        {
            var fin = C(0.46f, 0.54f, 0.56f);
            var fish = new DetailedFish
            {
                Width = 96, Height = 32, CenterY = 15.5f,
                Upper = V((14, 2.5f), (22, 4f), (34, 7.5f), (50, 9f), (66, 8.5f), (78, 6.5f), (86, 4f), (92, 2f)),
                Lower = V((14, 2.5f), (22, 4f), (34, 7f), (50, 8.5f), (66, 8f), (78, 6.5f), (86, 4f), (92, 1.5f)),
                Back = C(0.20f, 0.46f, 0.50f), Flank = C(0.66f, 0.76f, 0.74f), Belly = C(0.94f, 0.96f, 0.94f),
                FlankStart = 0.38f, BellyStart = 0.60f, ScaleTexture = false,
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 11f, Base = fin, Ray = C(0.30f, 0.36f, 0.38f), Edge = C(0.20f, 0.26f, 0.28f) },
                EyeX = 84, EyeYOffset = 2f, EyeRadius = 2, Iris = C(0.86f, 0.86f, 0.80f),
                GillX = 76, GillStrength = 0.4f, MouthXStart = 86, MouthXEnd = 92, MouthYOffset = -0.5f,
                Seed = 81,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 58, XFront = 70, HeightRear = 2f, HeightFront = 6f, Base = fin, Ray = C(0.30f, 0.36f, 0.38f), Edge = C(0.24f, 0.30f, 0.32f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 40, XFront = 46, HeightRear = 2f, HeightFront = 4f, Base = fin, Ray = C(0.30f, 0.36f, 0.38f), Edge = C(0.24f, 0.30f, 0.32f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 40, XFront = 46, HeightRear = 2f, HeightFront = 4f, Base = C(0.80f, 0.84f, 0.84f), Ray = C(0.50f, 0.54f, 0.54f), Edge = C(0.90f, 0.92f, 0.92f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 74, BaseYOffset = -2f, Length = 6, Width = 2f, Slope = 0.2f, Base = C(0.62f, 0.70f, 0.70f), Ray = C(0.40f, 0.46f, 0.46f), Edge = C(0.84f, 0.88f, 0.88f) });
            fish.BodyPatterns.Add((x, y, t, c) =>
            {
                if (t > 0.42f || x > 78) return c;
                float wave = Mathf.Sin(y * 0.9f) * 1.6f;
                return Mathf.Repeat(x + wave + y * 0.6f, 6f) < 1.4f ? Color.Lerp(c, C(0.06f, 0.14f, 0.16f), 0.85f) : c;
            });
            fish.Details.Add(Finlets(36, 18, C(0.70f, 0.74f, 0.50f)));
            return fish;
        }

        /// <summary>Chinchard (aji) : argenté vert-jaune, grand œil, ligne de scutelles osseuses le long du flanc.</summary>
        private static DetailedFish HorseMackerel()
        {
            var fin = C(0.70f, 0.70f, 0.50f);
            var fish = new DetailedFish
            {
                Width = 90, Height = 34, CenterY = 16.5f,
                Upper = V((14, 2.5f), (22, 4f), (34, 8f), (50, 10f), (64, 9.5f), (74, 7.5f), (82, 4.5f), (86, 2f)),
                Lower = V((14, 2.5f), (22, 4f), (34, 7.5f), (50, 9f), (64, 8.5f), (74, 7f), (82, 4.5f), (86, 1.5f)),
                Back = C(0.36f, 0.48f, 0.40f), Flank = C(0.82f, 0.84f, 0.70f), Belly = C(0.96f, 0.96f, 0.92f),
                FlankStart = 0.30f,
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 12f, Base = fin, Ray = C(0.48f, 0.48f, 0.30f), Edge = C(0.86f, 0.80f, 0.40f) },
                EyeX = 78, EyeYOffset = 2.5f, EyeRadius = 3, Iris = C(0.92f, 0.92f, 0.86f),
                GillX = 70, GillStrength = 0.45f, MouthXStart = 82, MouthXEnd = 87, MouthYOffset = -0.5f,
                Seed = 82,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 52, XFront = 62, HeightRear = 3f, HeightFront = 7f, Base = fin, Ray = C(0.48f, 0.48f, 0.30f), Edge = C(0.40f, 0.40f, 0.26f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 22, XFront = 50, HeightRear = 2f, HeightFront = 4f, Base = fin, Ray = C(0.48f, 0.48f, 0.30f), Edge = C(0.86f, 0.80f, 0.40f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 22, XFront = 44, HeightRear = 2f, HeightFront = 4f, Base = fin, Ray = C(0.48f, 0.48f, 0.30f), Edge = C(0.86f, 0.80f, 0.40f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 68, BaseYOffset = -2f, Length = 9, Width = 2f, Slope = 0.25f, Base = C(0.90f, 0.86f, 0.62f), Ray = C(0.66f, 0.62f, 0.40f), Edge = C(0.96f, 0.94f, 0.80f) });
            fish.Details.Add((f, p) =>
            {
                // Scutelles : rangée de petites plaques le long de la ligne latérale, qui descend vers la queue.
                for (int x = 18; x <= 66; x++)
                {
                    float y = f.CenterY + (x > 46 ? 2f : 2f - (46 - x) * 0.08f);
                    if (x % 2 == 0) PixelArt.Set(p, x, Mathf.RoundToInt(y), C(0.46f, 0.50f, 0.42f));
                    else PixelArt.Set(p, x, Mathf.RoundToInt(y), C(0.92f, 0.92f, 0.86f));
                }

                // Tache noire au bord de l'opercule.
                PixelArt.Set(p, 69, Mathf.RoundToInt(f.CenterY + 4f), C(0.10f, 0.10f, 0.10f));
                PixelArt.Set(p, 70, Mathf.RoundToInt(f.CenterY + 4f), C(0.10f, 0.10f, 0.10f));
            });
            return fish;
        }

        /// <summary>Sébaste (mebaru) : corps brun-gris, énormes yeux de chasseur de nuit, dorsale épineuse, bandes floues.</summary>
        private static DetailedFish Mebaru()
        {
            var fin = C(0.36f, 0.32f, 0.28f);
            var fish = new DetailedFish
            {
                Width = 86, Height = 40, CenterY = 19.5f,
                Upper = V((14, 3f), (22, 6f), (34, 11f), (48, 12.5f), (62, 12f), (72, 10f), (79, 6f), (83, 2.5f)),
                Lower = V((14, 3f), (22, 5.5f), (34, 9.5f), (48, 10.5f), (62, 10f), (72, 8.5f), (79, 5f), (83, 2f)),
                Back = C(0.28f, 0.24f, 0.20f), Flank = C(0.50f, 0.44f, 0.36f), Belly = C(0.80f, 0.74f, 0.64f),
                FlankStart = 0.32f,
                Caudal = new Tail { Shape = TailShape.Truncate, XTip = 2, XBase = 15, Span = 11f, Base = fin, Ray = C(0.22f, 0.20f, 0.16f), Edge = C(0.18f, 0.16f, 0.14f) },
                EyeX = 74, EyeYOffset = 4f, EyeRadius = 3, Iris = C(0.86f, 0.72f, 0.46f),
                GillX = 66, GillStrength = 0.5f, MouthXStart = 78, MouthXEnd = 84, MouthYOffset = 0.5f, MouthSlope = 0.15f,
                Seed = 83,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 26, XFront = 64, HeightRear = 6f, HeightFront = 9f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.20f, 0.18f, 0.14f), Edge = C(0.56f, 0.50f, 0.40f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 24, XFront = 34, HeightRear = 4f, HeightFront = 6f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.20f, 0.18f, 0.14f), Edge = C(0.56f, 0.50f, 0.40f), Spiny = true });
            fish.PectoralFins.Add(new Pectoral { BaseX = 64, BaseYOffset = -3f, Length = 9, Width = 3f, Slope = 0.2f, Base = C(0.52f, 0.46f, 0.38f), Ray = C(0.30f, 0.26f, 0.20f), Edge = C(0.66f, 0.60f, 0.50f) });
            fish.BodyPatterns.Add(VerticalBars(new[] { 26, 38, 50, 60 }, 4, C(0.22f, 0.18f, 0.14f), 0.55f, 0.2f));
            return fish;
        }

        /// <summary>Rascasse (kasago) : brun-rouge marbré semé de taches blanches, grosse tête épineuse, grandes pectorales.</summary>
        private static DetailedFish Kasago()
        {
            var fin = C(0.56f, 0.24f, 0.16f);
            var fish = new DetailedFish
            {
                Width = 84, Height = 40, CenterY = 18.5f,
                Upper = V((14, 3f), (22, 5.5f), (34, 9.5f), (48, 11.5f), (60, 12f), (70, 11f), (77, 7f), (81, 3f)),
                Lower = V((14, 3f), (22, 5f), (34, 8.5f), (48, 9.5f), (60, 10f), (70, 9f), (77, 6f), (81, 2.5f)),
                Back = C(0.46f, 0.18f, 0.12f), Flank = C(0.64f, 0.30f, 0.20f), Belly = C(0.86f, 0.62f, 0.50f),
                FlankStart = 0.32f, ScaleTexture = false,
                Caudal = new Tail { Shape = TailShape.Round, XTip = 2, XBase = 15, Span = 11f, Base = fin, Ray = C(0.36f, 0.14f, 0.08f), Edge = C(0.76f, 0.46f, 0.36f) },
                EyeX = 72, EyeYOffset = 5f, EyeRadius = 2, Iris = C(0.86f, 0.58f, 0.32f),
                GillX = 64, GillStrength = 0.5f, MouthXStart = 76, MouthXEnd = 82, MouthYOffset = 0f, MouthSlope = 0.1f,
                Seed = 84,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 26, XFront = 64, HeightRear = 6f, HeightFront = 10f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.30f, 0.10f, 0.06f), Edge = C(0.80f, 0.56f, 0.44f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 24, XFront = 34, HeightRear = 5f, HeightFront = 6f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.30f, 0.10f, 0.06f), Edge = C(0.80f, 0.56f, 0.44f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 62, BaseYOffset = -2f, Length = 12, Width = 4f, Slope = 0.15f, Base = C(0.70f, 0.34f, 0.22f), Ray = C(0.40f, 0.16f, 0.10f), Edge = C(0.86f, 0.56f, 0.44f) });
            fish.Details.Add((f, p) =>
            {
                // Marbrures sombres puis taches blanches.
                var random = new System.Random(841);
                for (int i = 0; i < 14; i++)
                {
                    int cx = random.Next(18, 72);
                    int cy = Mathf.RoundToInt(f.CenterY) + random.Next(-8, 8);
                    for (int x = cx - 2; x <= cx + 2; x++)
                    {
                        for (int y = cy - 1; y <= cy + 1; y++)
                        {
                            if (f.InBody(x, y)) p[x, y] = Color.Lerp(p[x, y], C(0.28f, 0.10f, 0.06f), 0.6f);
                        }
                    }
                }
            });
            fish.Details.Add(Spots(30, 16, 74, 0.1f, 0.85f, C(0.96f, 0.90f, 0.84f), null, 842, 0.1f));
            fish.Details.Add((f, p) =>
            {
                // Épines de la tête au-dessus de l'œil.
                int top = Mathf.RoundToInt(f.CenterY + f.UpperAt(72f));
                PixelArt.Set(p, 70, top + 1, C(0.70f, 0.36f, 0.24f));
                PixelArt.Set(p, 74, top + 1, C(0.70f, 0.36f, 0.24f));
            });
            return fish;
        }

        /// <summary>Pagre japonais (madai) : corps haut rose-rouge semé de points bleu clair, queue au bord noir.</summary>
        private static DetailedFish Madai()
        {
            var fin = C(0.86f, 0.44f, 0.44f);
            var fish = new DetailedFish
            {
                Width = 90, Height = 44, CenterY = 20.5f,
                Upper = V((14, 3f), (22, 6f), (34, 12f), (48, 15f), (62, 14.5f), (72, 12f), (80, 7.5f), (86, 3f)),
                Lower = V((14, 3f), (22, 5.5f), (34, 10f), (48, 12f), (62, 11.5f), (72, 9.5f), (80, 6f), (86, 2.5f)),
                Back = C(0.86f, 0.30f, 0.30f), Flank = C(0.94f, 0.56f, 0.52f), Belly = C(0.98f, 0.88f, 0.84f),
                FlankStart = 0.30f,
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 13f, Base = fin, Ray = C(0.66f, 0.26f, 0.26f), Edge = C(0.12f, 0.10f, 0.12f) },
                EyeX = 78, EyeYOffset = 4.5f, EyeRadius = 2, Iris = C(0.90f, 0.70f, 0.40f),
                GillX = 70, GillStrength = 0.45f, MouthXStart = 82, MouthXEnd = 87, MouthYOffset = -1f,
                Seed = 85,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 26, XFront = 66, HeightRear = 5f, HeightFront = 8f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.66f, 0.26f, 0.26f), Edge = C(0.98f, 0.70f, 0.70f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 24, XFront = 36, HeightRear = 3f, HeightFront = 6f, Base = fin, Ray = C(0.66f, 0.26f, 0.26f), Edge = C(0.98f, 0.70f, 0.70f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 68, BaseYOffset = -3f, Length = 11, Width = 2.5f, Slope = 0.3f, Base = C(0.96f, 0.62f, 0.60f), Ray = C(0.74f, 0.34f, 0.34f), Edge = C(0.98f, 0.82f, 0.80f) });
            fish.Details.Add(Spots(34, 20, 74, 0.05f, 0.5f, C(0.40f, 0.76f, 0.96f), null, 851, 0f));
            return fish;
        }

        /// <summary>Fugu tigre : rond et dodu, dos sombre, grosse tache noire cerclée de blanc derrière la pectorale, ventre blanc, bec.</summary>
        private static DetailedFish Fugu()
        {
            var fin = C(0.36f, 0.36f, 0.34f);
            var fish = new DetailedFish
            {
                Width = 84, Height = 46, CenterY = 22.5f,
                Upper = V((14, 3f), (22, 6f), (32, 12f), (44, 15.5f), (56, 16f), (66, 14f), (74, 10f), (80, 4f)),
                Lower = V((14, 3f), (22, 6f), (32, 12f), (44, 15f), (56, 15.5f), (66, 13.5f), (74, 9f), (80, 3f)),
                Back = C(0.24f, 0.24f, 0.26f), Flank = C(0.36f, 0.36f, 0.38f), Belly = C(0.96f, 0.96f, 0.94f),
                FlankStart = 0.30f, BellyStart = 0.55f, ScaleTexture = false,
                Caudal = new Tail { Shape = TailShape.Truncate, XTip = 3, XBase = 15, Span = 9f, Base = fin, Ray = C(0.20f, 0.20f, 0.20f), Edge = C(0.62f, 0.62f, 0.58f) },
                EyeX = 70, EyeYOffset = 6f, EyeRadius = 2, Iris = C(0.40f, 0.70f, 0.50f),
                GillX = 62, GillStrength = 0.2f, MouthXStart = 79, MouthXEnd = 81, MouthYOffset = 0f,
                Seed = 86,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 20, XFront = 26, HeightRear = 3f, HeightFront = 5f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.20f, 0.20f, 0.20f), Edge = C(0.62f, 0.62f, 0.58f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 20, XFront = 26, HeightRear = 3f, HeightFront = 5f, Shape = FinShape.Rounded, Base = C(0.86f, 0.86f, 0.84f), Ray = C(0.60f, 0.60f, 0.58f), Edge = C(0.96f, 0.96f, 0.94f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 60, BaseYOffset = 0f, Length = 6, Width = 3f, Slope = 0f, Base = C(0.62f, 0.62f, 0.60f), Ray = C(0.40f, 0.40f, 0.38f), Edge = C(0.86f, 0.86f, 0.84f) });
            fish.Details.Add(Spots(40, 24, 72, 0.02f, 0.4f, C(0.14f, 0.14f, 0.16f), null, 861, 0.4f));
            fish.Details.Add((f, p) =>
            {
                // Grosse tache noire cerclée de blanc.
                var center = new Vector2(52f, f.CenterY + 3f);
                for (int x = 44; x <= 60; x++)
                {
                    for (int y = Mathf.RoundToInt(center.y) - 7; y <= Mathf.RoundToInt(center.y) + 7; y++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), center);
                        if (!f.InBody(x, y) || d > 6f) continue;
                        p[x, y] = d > 4.8f ? C(0.96f, 0.96f, 0.94f) : C(0.06f, 0.06f, 0.08f);
                    }
                }

                // Bec de dents soudées.
                int my = Mathf.RoundToInt(f.CenterY);
                PixelArt.Set(p, 80, my, C(0.98f, 0.98f, 0.92f));
                PixelArt.Set(p, 80, my + 1, C(0.98f, 0.98f, 0.92f));
                PixelArt.Set(p, 81, my, C(0.86f, 0.86f, 0.80f));
            });
            return fish;
        }

        /// <summary>Pagre noir (kurodai) : corps haut gris argenté sombre, dorsale épineuse, nageoires noirâtres.</summary>
        private static DetailedFish Kurodai()
        {
            var fin = C(0.26f, 0.28f, 0.30f);
            var fish = new DetailedFish
            {
                Width = 88, Height = 44, CenterY = 20.5f,
                Upper = V((14, 3f), (22, 6f), (34, 12f), (48, 14.5f), (62, 14f), (72, 11.5f), (80, 7f), (85, 3f)),
                Lower = V((14, 3f), (22, 5.5f), (34, 10f), (48, 12f), (62, 11.5f), (72, 9.5f), (80, 6f), (85, 2.5f)),
                Back = C(0.26f, 0.30f, 0.32f), Flank = C(0.58f, 0.62f, 0.64f), Belly = C(0.86f, 0.88f, 0.88f),
                FlankStart = 0.30f,
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 13f, Base = fin, Ray = C(0.14f, 0.16f, 0.18f), Edge = C(0.10f, 0.10f, 0.12f) },
                EyeX = 77, EyeYOffset = 4.5f, EyeRadius = 2, Iris = C(0.80f, 0.80f, 0.74f),
                GillX = 69, GillStrength = 0.45f, MouthXStart = 81, MouthXEnd = 86, MouthYOffset = -1f,
                Seed = 87,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 26, XFront = 66, HeightRear = 5f, HeightFront = 9f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.14f, 0.16f, 0.18f), Edge = C(0.40f, 0.44f, 0.46f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 24, XFront = 36, HeightRear = 3f, HeightFront = 6f, Base = fin, Ray = C(0.14f, 0.16f, 0.18f), Edge = C(0.40f, 0.44f, 0.46f), Spiny = true });
            fish.PectoralFins.Add(new Pectoral { BaseX = 67, BaseYOffset = -3f, Length = 11, Width = 2.5f, Slope = 0.3f, Base = C(0.52f, 0.56f, 0.58f), Ray = C(0.30f, 0.32f, 0.34f), Edge = C(0.70f, 0.74f, 0.76f) });
            fish.BodyPatterns.Add(Lines(new[] { 0.2f, 0.32f, 0.44f, 0.56f }, C(0.40f, 0.44f, 0.46f), 0.35f, 18, 66));
            return fish;
        }

        /// <summary>Bar japonais (suzuki) : allongé, dos gris-vert, flancs argentés, deux dorsales, grande bouche.</summary>
        private static DetailedFish Suzuki()
        {
            var fin = C(0.50f, 0.54f, 0.54f);
            var fish = new DetailedFish
            {
                Width = 100, Height = 36, CenterY = 17.5f,
                Upper = V((14, 3f), (24, 5f), (38, 9f), (54, 10.5f), (70, 10f), (82, 8f), (90, 5f), (96, 2.5f)),
                Lower = V((14, 3f), (24, 5f), (38, 8.5f), (54, 9.5f), (70, 9f), (82, 7.5f), (90, 5f), (96, 2.5f)),
                Back = C(0.36f, 0.44f, 0.44f), Flank = C(0.80f, 0.84f, 0.84f), Belly = C(0.96f, 0.96f, 0.96f),
                FlankStart = 0.30f,
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 11f, Base = fin, Ray = C(0.32f, 0.36f, 0.36f), Edge = C(0.24f, 0.28f, 0.28f) },
                EyeX = 88, EyeYOffset = 3f, EyeRadius = 2, Iris = C(0.86f, 0.86f, 0.80f),
                GillX = 80, GillStrength = 0.5f, MouthXStart = 88, MouthXEnd = 97, MouthYOffset = -0.5f, MouthSlope = -0.1f,
                Seed = 88,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 58, XFront = 74, HeightRear = 3f, HeightFront = 8f, Base = fin, Ray = C(0.32f, 0.36f, 0.36f), Edge = C(0.24f, 0.28f, 0.28f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 30, XFront = 52, HeightRear = 3f, HeightFront = 6f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.32f, 0.36f, 0.36f), Edge = C(0.24f, 0.28f, 0.28f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 28, XFront = 38, HeightRear = 2f, HeightFront = 5f, Base = C(0.82f, 0.84f, 0.84f), Ray = C(0.56f, 0.58f, 0.58f), Edge = C(0.92f, 0.92f, 0.92f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 78, BaseYOffset = -3f, Length = 8, Width = 2f, Slope = 0.25f, Base = C(0.80f, 0.82f, 0.82f), Ray = C(0.56f, 0.58f, 0.58f), Edge = C(0.92f, 0.92f, 0.92f) });
            fish.Details.Add(Spots(14, 40, 80, 0.05f, 0.3f, C(0.18f, 0.22f, 0.22f), null, 881, 0f));
            return fish;
        }

        /// <summary>Sériole (buri) : fuseau puissant, dos bleu-vert, bande jaune sur toute la longueur, queue jaune fourchue.</summary>
        private static DetailedFish Buri()
        {
            var yellow = C(0.92f, 0.80f, 0.24f);
            var fish = new DetailedFish
            {
                Width = 104, Height = 36, CenterY = 17.5f,
                Upper = V((14, 2.5f), (24, 5f), (38, 9f), (56, 11f), (72, 10.5f), (84, 8f), (94, 5f), (99, 2.5f)),
                Lower = V((14, 2.5f), (24, 5f), (38, 8.5f), (56, 10f), (72, 9.5f), (84, 7.5f), (94, 4.5f), (99, 2f)),
                Back = C(0.20f, 0.40f, 0.48f), Flank = C(0.74f, 0.80f, 0.80f), Belly = C(0.96f, 0.96f, 0.96f),
                FlankStart = 0.36f,
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 13f, Base = yellow, Ray = C(0.66f, 0.54f, 0.12f), Edge = C(0.98f, 0.90f, 0.46f) },
                EyeX = 92, EyeYOffset = 2.5f, EyeRadius = 2, Iris = C(0.86f, 0.80f, 0.60f),
                GillX = 84, GillStrength = 0.4f, MouthXStart = 94, MouthXEnd = 100, MouthYOffset = -0.5f,
                Seed = 89,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 26, XFront = 64, HeightRear = 3f, HeightFront = 7f, Base = C(0.40f, 0.56f, 0.40f), Ray = C(0.26f, 0.36f, 0.26f), Edge = yellow });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 24, XFront = 44, HeightRear = 2f, HeightFront = 5f, Base = yellow, Ray = C(0.66f, 0.54f, 0.12f), Edge = C(0.98f, 0.90f, 0.46f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 82, BaseYOffset = -2f, Length = 8, Width = 2f, Slope = 0.25f, Base = C(0.90f, 0.84f, 0.50f), Ray = C(0.66f, 0.58f, 0.24f), Edge = C(0.98f, 0.94f, 0.70f) });
            fish.BodyPatterns.Add(Lines(new[] { 0.40f, 0.44f }, yellow, 0.85f, 14, 92));
            return fish;
        }

        /// <summary>Perroquet rayé (ishidai) : corps haut gris-blanc barré de sept bandes noires, bec de dents soudées.</summary>
        private static DetailedFish Ishidai()
        {
            var fin = C(0.30f, 0.30f, 0.30f);
            var fish = new DetailedFish
            {
                Width = 86, Height = 46, CenterY = 21.5f,
                Upper = V((14, 3f), (22, 6f), (34, 13f), (48, 16f), (60, 15.5f), (70, 13f), (78, 8f), (83, 3f)),
                Lower = V((14, 3f), (22, 6f), (34, 12f), (48, 14f), (60, 13.5f), (70, 11f), (78, 7f), (83, 2.5f)),
                Back = C(0.62f, 0.64f, 0.62f), Flank = C(0.86f, 0.86f, 0.82f), Belly = C(0.94f, 0.94f, 0.90f),
                FlankStart = 0.3f,
                Caudal = new Tail { Shape = TailShape.Truncate, XTip = 2, XBase = 15, Span = 12f, Base = fin, Ray = C(0.16f, 0.16f, 0.16f), Edge = C(0.12f, 0.12f, 0.12f) },
                EyeX = 75, EyeYOffset = 5f, EyeRadius = 2, Iris = C(0.80f, 0.76f, 0.60f),
                GillX = 67, GillStrength = 0.3f, MouthXStart = 81, MouthXEnd = 84, MouthYOffset = -1f,
                Seed = 90,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 24, XFront = 64, HeightRear = 7f, HeightFront = 8f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.16f, 0.16f, 0.16f), Edge = C(0.46f, 0.46f, 0.44f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 22, XFront = 36, HeightRear = 6f, HeightFront = 6f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.16f, 0.16f, 0.16f), Edge = C(0.46f, 0.46f, 0.44f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 66, BaseYOffset = -3f, Length = 8, Width = 2.5f, Slope = 0.2f, Base = C(0.70f, 0.70f, 0.68f), Ray = C(0.40f, 0.40f, 0.38f), Edge = C(0.86f, 0.86f, 0.84f) });
            fish.BodyPatterns.Add(VerticalBars(new[] { 18, 26, 35, 44, 53, 62, 72 }, 4, C(0.10f, 0.10f, 0.12f), 1f, 0.05f));
            fish.Details.Add((f, p) =>
            {
                int my = Mathf.RoundToInt(f.CenterY);
                PixelArt.Set(p, 83, my, C(0.96f, 0.94f, 0.86f));
                PixelArt.Set(p, 83, my - 1, C(0.96f, 0.94f, 0.86f));
                PixelArt.Set(p, 82, my - 2, C(0.86f, 0.84f, 0.76f));
            });
            return fish;
        }

        /// <summary>Anguille du Japon : long corps serpentiforme, dos olive sombre, ventre jaunâtre, nageoire continue.</summary>
        private static DetailedFish Eel()
        {
            var fin = C(0.40f, 0.40f, 0.26f);
            var fish = new DetailedFish
            {
                Width = 116, Height = 22, CenterY = 10.5f,
                Upper = V((2, 0.8f), (14, 2.5f), (34, 4f), (60, 5f), (84, 5f), (100, 4.5f), (108, 3.5f), (113, 2f)),
                Lower = V((2, 0.8f), (14, 2.5f), (34, 4f), (60, 5f), (84, 5f), (100, 4.5f), (108, 3f), (113, 1.5f)),
                Back = C(0.20f, 0.24f, 0.14f), Flank = C(0.38f, 0.40f, 0.24f), Belly = C(0.86f, 0.80f, 0.52f),
                FlankStart = 0.35f, BellyStart = 0.68f, ScaleTexture = false,
                EyeX = 108, EyeYOffset = 1.5f, EyeRadius = 1, Iris = C(0.70f, 0.66f, 0.40f),
                GillX = 100, GillStrength = 0.2f, MouthXStart = 110, MouthXEnd = 114, MouthYOffset = -0.5f,
                Seed = 91,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 2, XFront = 64, HeightRear = 2f, HeightFront = 2.5f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.24f, 0.24f, 0.14f), Edge = C(0.56f, 0.56f, 0.36f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 2, XFront = 56, HeightRear = 2f, HeightFront = 2.5f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.24f, 0.24f, 0.14f), Edge = C(0.56f, 0.56f, 0.36f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 98, BaseYOffset = -1f, Length = 4, Width = 2f, Slope = 0.1f, Base = C(0.56f, 0.56f, 0.36f), Ray = fin, Edge = C(0.70f, 0.70f, 0.50f) });
            return fish;
        }

        /// <summary>Mérou (kue) : massif, brun-violacé barré de bandes sombres obliques, grande bouche, queue ronde.</summary>
        private static DetailedFish Kue()
        {
            var fin = C(0.36f, 0.28f, 0.26f);
            var fish = new DetailedFish
            {
                Width = 104, Height = 46, CenterY = 21.5f,
                Upper = V((14, 4f), (24, 7f), (38, 12f), (56, 15f), (72, 15f), (84, 13f), (94, 8f), (100, 3.5f)),
                Lower = V((14, 4f), (24, 6.5f), (38, 11f), (56, 13f), (72, 13f), (84, 11.5f), (94, 7f), (100, 3f)),
                Back = C(0.36f, 0.26f, 0.26f), Flank = C(0.56f, 0.44f, 0.40f), Belly = C(0.80f, 0.70f, 0.62f),
                FlankStart = 0.32f,
                Caudal = new Tail { Shape = TailShape.Round, XTip = 2, XBase = 16, Span = 13f, Base = fin, Ray = C(0.22f, 0.16f, 0.14f), Edge = C(0.56f, 0.46f, 0.42f) },
                EyeX = 89, EyeYOffset = 5f, EyeRadius = 2, Iris = C(0.80f, 0.64f, 0.40f),
                GillX = 79, GillStrength = 0.55f, MouthXStart = 92, MouthXEnd = 101, MouthYOffset = -1.5f, MouthSlope = 0.15f,
                Seed = 92,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 28, XFront = 76, HeightRear = 7f, HeightFront = 6f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.22f, 0.16f, 0.14f), Edge = C(0.56f, 0.46f, 0.42f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 26, XFront = 38, HeightRear = 6f, HeightFront = 5f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.22f, 0.16f, 0.14f), Edge = C(0.56f, 0.46f, 0.42f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 76, BaseYOffset = -3f, Length = 10, Width = 3.5f, Slope = 0.15f, Base = C(0.56f, 0.44f, 0.40f), Ray = C(0.34f, 0.26f, 0.24f), Edge = C(0.70f, 0.60f, 0.54f) });
            fish.BodyPatterns.Add(VerticalBars(new[] { 22, 36, 50, 64, 76 }, 5, C(0.22f, 0.14f, 0.14f), 0.85f, -0.35f));
            return fish;
        }

        /// <summary>Thon rouge du Pacifique : énorme fuseau, dos bleu nuit, flancs argentés, pinnules jaunes, queue en croissant.</summary>
        private static DetailedFish BluefinTuna()
        {
            var navy = C(0.10f, 0.14f, 0.28f);
            var fish = new DetailedFish
            {
                Width = 112, Height = 44, CenterY = 21.5f,
                Upper = V((14, 2f), (22, 4f), (34, 10f), (52, 15f), (70, 15.5f), (86, 12f), (98, 7f), (106, 3f)),
                Lower = V((14, 2f), (22, 4f), (34, 9f), (52, 14f), (70, 14.5f), (86, 11.5f), (98, 6.5f), (106, 2.5f)),
                Back = navy, Flank = C(0.62f, 0.68f, 0.76f), Belly = C(0.94f, 0.94f, 0.96f),
                FlankStart = 0.36f, BellyStart = 0.62f, ScaleTexture = false,
                Caudal = new Tail { Shape = TailShape.Lunate, XTip = 1, XBase = 15, Span = 16f, Base = C(0.14f, 0.18f, 0.30f), Ray = C(0.08f, 0.10f, 0.20f), Edge = C(0.06f, 0.08f, 0.16f) },
                EyeX = 98, EyeYOffset = 3f, EyeRadius = 2, Iris = C(0.80f, 0.80f, 0.86f),
                GillX = 88, GillStrength = 0.5f, MouthXStart = 100, MouthXEnd = 107, MouthYOffset = -0.5f,
                Seed = 93,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 62, XFront = 78, HeightRear = 2f, HeightFront = 7f, Base = navy, Ray = C(0.06f, 0.08f, 0.16f), Edge = C(0.20f, 0.26f, 0.40f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 44, XFront = 54, HeightRear = 3f, HeightFront = 9f, Shape = FinShape.Falcate, Base = C(0.30f, 0.32f, 0.40f), Ray = C(0.18f, 0.20f, 0.28f), Edge = C(0.80f, 0.70f, 0.30f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 44, XFront = 54, HeightRear = 3f, HeightFront = 8f, Shape = FinShape.Falcate, Base = C(0.70f, 0.70f, 0.74f), Ray = C(0.46f, 0.46f, 0.50f), Edge = C(0.86f, 0.76f, 0.36f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 86, BaseYOffset = 0f, Length = 10, Width = 2f, Slope = 0.25f, Base = C(0.20f, 0.24f, 0.36f), Ray = C(0.10f, 0.12f, 0.22f), Edge = C(0.36f, 0.40f, 0.52f) });
            fish.BodyPatterns.Add(Lines(new[] { 0.46f, 0.56f }, C(0.80f, 0.84f, 0.92f), 0.35f, 30, 84));
            fish.Details.Add(Finlets(40, 18, C(0.96f, 0.84f, 0.24f)));
            return fish;
        }
    }
}
