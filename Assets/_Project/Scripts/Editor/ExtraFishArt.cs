using UnityEngine;
using static FishAndChill.Editor.DetailedFish;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Deuxième fournée d'espèces, dessinées d'après l'anatomie et la livrée réelles (dessins originaux) :
    /// 5 de l'Île Mahana (récif indo-pacifique) et 5 du lac d'Annecy (eau douce).
    /// </summary>
    public static class ExtraFishArt
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
                case "chaetodon-auriga": fish = Butterflyfish(); return true;
                case "mulloidichthys-flavolineatus": fish = Goatfish(); return true;
                case "lutjanus-kasmira": fish = BluestripeSnapper(); return true;
                case "rhinecanthus-aculeatus": fish = Picassofish(); return true;
                case "pterois-radiata": fish = Lionfish(); return true;
                case "alburnus-alburnus": fish = Bleak(); return true;
                case "squalius-cephalus": fish = Chub(); return true;
                case "tinca-tinca": fish = Tench(); return true;
                case "cyprinus-carpio": fish = Carp(); return true;
                case "lota-lota": fish = Burbot(); return true;
                default: fish = null; return false;
            }
        }

        // ---------- Île Mahana ----------

        /// <summary>
        /// Poisson-papillon cocher : disque très haut et plat, museau pointu, blanc à fines lignes sombres en chevrons,
        /// arrière jaune vif, bande noire à travers l'œil, tache noire (faux œil) au bord de la dorsale.
        /// </summary>
        private static DetailedFish Butterflyfish()
        {
            var yellow = C(0.98f, 0.82f, 0.18f);
            var fish = new DetailedFish
            {
                Width = 74, Height = 52, CenterY = 25.5f,
                Upper = V((14, 3f), (20, 9f), (28, 16f), (40, 19.5f), (52, 17.5f), (60, 12f), (66, 6f), (71, 2.5f)),
                Lower = V((14, 3f), (20, 8f), (28, 14f), (40, 17f), (52, 15f), (60, 10f), (66, 5f), (71, 1.5f)),
                Back = C(0.95f, 0.95f, 0.90f), Flank = C(0.96f, 0.96f, 0.93f), Belly = C(0.98f, 0.98f, 0.96f),
                FlankStart = 0.30f,
                Caudal = new Tail { Shape = TailShape.Truncate, XTip = 4, XBase = 15, Span = 9f, Base = yellow, Ray = C(0.80f, 0.62f, 0.10f), Edge = C(0.98f, 0.95f, 0.70f) },
                EyeX = 63, EyeYOffset = 3f, EyeRadius = 2, Iris = C(0.15f, 0.15f, 0.15f),
                GillX = 58, GillStrength = 0.2f, MouthXStart = 69, MouthXEnd = 72, MouthYOffset = 0f,
                Seed = 61,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 20, XFront = 54, HeightRear = 6f, HeightFront = 3f, Shape = FinShape.Rounded, Base = yellow, Ray = C(0.82f, 0.64f, 0.10f), Edge = C(0.20f, 0.18f, 0.12f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 20, XFront = 42, HeightRear = 6f, HeightFront = 3f, Shape = FinShape.Rounded, Base = yellow, Ray = C(0.82f, 0.64f, 0.10f), Edge = C(0.20f, 0.18f, 0.12f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 56, BaseYOffset = -2f, Length = 6, Width = 2f, Slope = 0.2f, Base = C(0.96f, 0.96f, 0.90f), Ray = C(0.85f, 0.85f, 0.78f), Edge = C(0.99f, 0.99f, 0.96f) });
            fish.BodyPatterns.Add((x, y, t, c) =>
            {
                if (x < 32)
                {
                    return Color.Lerp(c, yellow, 0.85f); // arrière jaune
                }

                // Chevrons : lignes montantes sur le haut, descendantes sur le bas.
                bool line = t < 0.5f ? (x + y) % 6 == 0 : (x - y + 600) % 6 == 0;
                return line && x < 58 ? Color.Lerp(c, C(0.30f, 0.30f, 0.32f), 0.75f) : c;
            });
            fish.BodyPatterns.Add(VerticalBars(new[] { 62 }, 3, C(0.08f, 0.08f, 0.08f), 1f));
            fish.Details.Add((f, p) =>
            {
                // Faux œil noir cerclé de blanc au bord arrière de la dorsale.
                int cy = Mathf.RoundToInt(f.CenterY + f.UpperAt(24)) + 3;
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        if (dx * dx + dy * dy <= 4 && PixelArt.IsFilled(p, 24 + dx, cy + dy))
                        {
                            p[24 + dx, cy + dy] = dx * dx + dy * dy <= 1 ? C(0.05f, 0.05f, 0.06f) : C(0.15f, 0.12f, 0.08f);
                        }
                    }
                }
            });
            return fish;
        }

        /// <summary>Rouget-barbet à bande jaune : corps allongé blanc argenté, bande jaune et tache noire, deux barbillons.</summary>
        private static DetailedFish Goatfish()
        {
            var yellow = C(0.98f, 0.82f, 0.22f);
            var fin = C(0.90f, 0.88f, 0.70f);
            var fish = new DetailedFish
            {
                Width = 92, Height = 34, CenterY = 16.5f,
                Upper = V((14, 2f), (24, 5f), (40, 8f), (56, 8.5f), (70, 7.5f), (80, 5f), (87, 2f)),
                Lower = V((14, 2f), (24, 4.5f), (40, 7f), (56, 7.5f), (70, 7f), (80, 4.5f), (87, 1.5f)),
                Back = C(0.76f, 0.78f, 0.68f), Flank = C(0.92f, 0.93f, 0.90f), Belly = C(0.98f, 0.98f, 0.96f),
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 10f, Base = C(0.95f, 0.86f, 0.48f), Ray = C(0.78f, 0.66f, 0.28f), Edge = C(0.98f, 0.94f, 0.70f) },
                EyeX = 80, EyeYOffset = 2.5f, EyeRadius = 2, Iris = C(0.85f, 0.75f, 0.30f),
                GillX = 74, MouthXStart = 85, MouthXEnd = 88, MouthYOffset = -2f,
                Seed = 62,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 56, XFront = 66, HeightRear = 3f, HeightFront = 7f, Base = fin, Ray = C(0.70f, 0.68f, 0.50f), Edge = C(0.98f, 0.96f, 0.86f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 32, XFront = 42, HeightRear = 2f, HeightFront = 5f, Base = fin, Ray = C(0.70f, 0.68f, 0.50f), Edge = C(0.98f, 0.96f, 0.86f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 30, XFront = 40, HeightRear = 2f, HeightFront = 5f, Base = fin, Ray = C(0.70f, 0.68f, 0.50f), Edge = C(0.98f, 0.96f, 0.86f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 72, BaseYOffset = -2f, Length = 7, Width = 2f, Slope = 0.25f, Base = C(0.96f, 0.94f, 0.86f), Ray = C(0.82f, 0.80f, 0.70f), Edge = C(0.99f, 0.98f, 0.94f) });
            fish.BodyPatterns.Add(Lines(new[] { 0.40f }, yellow, 0.9f, 16, 82));
            fish.Details.Add((f, p) =>
            {
                // Tache noire sur la bande jaune, sous la première dorsale.
                for (int x = 58; x <= 61; x++)
                {
                    for (int y = 0; y < f.Height; y++)
                    {
                        if (f.InBody(x, y) && Mathf.Abs(f.BodyT(x, y) - 0.40f) < 0.07f) p[x, y] = C(0.08f, 0.08f, 0.08f);
                    }
                }

                // Deux barbillons jaunes sous le menton.
                int chin = Mathf.RoundToInt(f.CenterY - f.LowerAt(84));
                for (int k = 0; k < 4; k++)
                {
                    PixelArt.Set(p, 84 - k / 2, chin - 1 - k, C(0.96f, 0.86f, 0.40f));
                    PixelArt.Set(p, 86 - k / 2, chin - 1 - k, C(0.90f, 0.78f, 0.32f));
                }
            });
            return fish;
        }

        /// <summary>Vivaneau à raies bleues : jaune vif, 4 raies bleu électrique cerclées de sombre, nageoires jaunes.</summary>
        private static DetailedFish BluestripeSnapper()
        {
            var yellow = C(0.98f, 0.82f, 0.22f);
            var fish = new DetailedFish
            {
                Width = 88, Height = 40, CenterY = 19.5f,
                Upper = V((14, 2.5f), (22, 5f), (34, 10f), (48, 12f), (62, 11f), (72, 8f), (80, 4.5f), (85, 2f)),
                Lower = V((14, 2.5f), (22, 4.5f), (34, 9f), (48, 10.5f), (62, 9.5f), (72, 7f), (80, 4f), (85, 1.5f)),
                Back = C(0.92f, 0.70f, 0.16f), Flank = C(0.98f, 0.84f, 0.26f), Belly = C(0.98f, 0.94f, 0.72f),
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 3, XBase = 15, Span = 10f, Base = yellow, Ray = C(0.80f, 0.60f, 0.10f), Edge = C(0.99f, 0.92f, 0.60f) },
                EyeX = 77, EyeYOffset = 3f, EyeRadius = 2, Iris = C(0.85f, 0.30f, 0.20f),
                GillX = 70, MouthXStart = 82, MouthXEnd = 86, MouthYOffset = -0.5f,
                Seed = 63,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 26, XFront = 64, HeightRear = 6f, HeightFront = 5f, Shape = FinShape.Rounded, Base = yellow, Ray = C(0.80f, 0.60f, 0.10f), Edge = C(0.99f, 0.92f, 0.60f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 26, XFront = 36, HeightRear = 3f, HeightFront = 6f, Base = yellow, Ray = C(0.80f, 0.60f, 0.10f), Edge = C(0.99f, 0.92f, 0.60f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 56, XFront = 62, HeightRear = 1f, HeightFront = 6f, Base = yellow, Ray = C(0.80f, 0.60f, 0.10f), Edge = C(0.99f, 0.92f, 0.60f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 68, BaseYOffset = -2f, Length = 8, Width = 2.5f, Slope = 0.2f, Base = C(0.99f, 0.88f, 0.50f), Ray = C(0.86f, 0.68f, 0.24f), Edge = C(0.99f, 0.95f, 0.76f) });
            fish.BodyPatterns.Add(Lines(new[] { 0.16f, 0.34f, 0.52f, 0.70f }, C(0.28f, 0.58f, 0.98f), 0.95f, 18, 84));
            return fish;
        }

        /// <summary>
        /// Baliste Picasso : corps en losange beige, œil haut et reculé, bande noire de l'œil à la pectorale,
        /// traits bleus et jaunes du museau, coin sombre rayé de bleu sur l'arrière, lèvre jaune.
        /// </summary>
        private static DetailedFish Picassofish()
        {
            var tan = C(0.82f, 0.74f, 0.56f);
            var blue = C(0.30f, 0.60f, 0.90f);
            var fish = new DetailedFish
            {
                Width = 84, Height = 46, CenterY = 22.5f,
                Upper = V((14, 3f), (20, 7f), (30, 13f), (44, 16.5f), (56, 15f), (66, 10f), (74, 6f), (80, 2.5f)),
                Lower = V((14, 3f), (20, 6.5f), (30, 12f), (44, 15f), (56, 14f), (66, 11f), (74, 6f), (80, 2f)),
                Back = C(0.72f, 0.64f, 0.46f), Flank = C(0.88f, 0.84f, 0.72f), Belly = C(0.97f, 0.96f, 0.92f),
                FlankStart = 0.30f,
                Caudal = new Tail { Shape = TailShape.Round, XTip = 3, XBase = 15, Span = 9f, Base = tan, Ray = C(0.60f, 0.52f, 0.36f), Edge = C(0.95f, 0.92f, 0.82f) },
                EyeX = 60, EyeYOffset = 7f, EyeRadius = 2, Iris = C(0.92f, 0.70f, 0.20f),
                GillX = 54, GillStrength = 0.2f, MouthXStart = 78, MouthXEnd = 81, MouthYOffset = 0f, MouthColor = C(0.95f, 0.72f, 0.16f),
                Seed = 64,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 22, XFront = 40, HeightRear = 6f, HeightFront = 5f, Shape = FinShape.Rounded, Base = C(0.86f, 0.84f, 0.76f), Ray = C(0.66f, 0.62f, 0.50f), Edge = C(0.98f, 0.97f, 0.92f) });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 50, XFront = 55, HeightRear = 2f, HeightFront = 6f, Base = C(0.40f, 0.36f, 0.30f), Ray = C(0.22f, 0.20f, 0.18f), Edge = C(0.60f, 0.56f, 0.46f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 22, XFront = 40, HeightRear = 6f, HeightFront = 5f, Shape = FinShape.Rounded, Base = C(0.86f, 0.84f, 0.76f), Ray = C(0.66f, 0.62f, 0.50f), Edge = C(0.98f, 0.97f, 0.92f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 52, BaseYOffset = 0f, Length = 6, Width = 2f, Slope = 0.2f, Base = C(0.90f, 0.86f, 0.70f), Ray = C(0.70f, 0.64f, 0.48f), Edge = C(0.98f, 0.96f, 0.88f) });
            fish.BodyPatterns.Add((x, y, t, c) =>
            {
                // Coin sombre à l'arrière du ventre, rayé de bleu en diagonale.
                if (x < 40 && t > 0.45f && x - (t - 0.45f) * 40f < 34f)
                {
                    return (x + y) % 5 == 0 ? blue : Color.Lerp(c, C(0.22f, 0.18f, 0.14f), 0.8f);
                }

                // Bande noire de l'œil jusqu'à la pectorale, en biais.
                float band = x - 57f + (t - 0.2f) * 10f;
                if (band >= 0f && band < 3f && t > 0.15f && t < 0.7f)
                {
                    return C(0.08f, 0.08f, 0.10f);
                }

                return c;
            });
            fish.BodyPatterns.Add(Lines(new[] { 0.32f, 0.40f }, blue, 0.85f, 64, 78));
            fish.BodyPatterns.Add(Lines(new[] { 0.48f }, C(0.95f, 0.72f, 0.16f), 0.9f, 62, 80));
            return fish;
        }

        /// <summary>
        /// Poisson-lion radié : corps brun-rouge rayé de blanc, grandes épines dorsales blanches dressées,
        /// pectorales en éventail aux rayons blancs.
        /// </summary>
        private static DetailedFish Lionfish()
        {
            var white = C(0.96f, 0.95f, 0.92f);
            var fish = new DetailedFish
            {
                Width = 92, Height = 58, CenterY = 26.5f,
                Upper = V((14, 2.5f), (24, 5f), (36, 8.5f), (50, 10f), (64, 9.5f), (74, 7.5f), (82, 5f), (88, 2f)),
                Lower = V((14, 2.5f), (24, 4.5f), (36, 7.5f), (50, 9f), (64, 8.5f), (74, 7f), (82, 4.5f), (88, 1.5f)),
                Back = C(0.40f, 0.12f, 0.10f), Flank = C(0.60f, 0.20f, 0.14f), Belly = C(0.82f, 0.52f, 0.42f),
                ScaleTexture = false,
                Caudal = new Tail { Shape = TailShape.Round, XTip = 3, XBase = 15, Span = 9f, Base = C(0.92f, 0.90f, 0.86f), Ray = C(0.70f, 0.66f, 0.62f), Edge = white },
                EyeX = 80, EyeYOffset = 3f, EyeRadius = 2, Iris = C(0.80f, 0.30f, 0.20f),
                GillX = 72, MouthXStart = 85, MouthXEnd = 89, MouthYOffset = -1f,
                Seed = 65,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 44, XFront = 72, HeightRear = 12f, HeightFront = 17f, Base = C(0.70f, 0.30f, 0.24f), Ray = white, Edge = white, Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 20, XFront = 40, HeightRear = 5f, HeightFront = 6f, Shape = FinShape.Rounded, Base = C(0.90f, 0.86f, 0.82f), Ray = C(0.60f, 0.30f, 0.24f), Edge = white });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 22, XFront = 36, HeightRear = 5f, HeightFront = 6f, Shape = FinShape.Rounded, Base = C(0.90f, 0.86f, 0.82f), Ray = C(0.60f, 0.30f, 0.24f), Edge = white });
            fish.PectoralFins.Add(new Pectoral { BaseX = 68, BaseYOffset = -3f, Length = 22, Width = 7f, Slope = 0.55f, Base = C(0.86f, 0.80f, 0.76f), Ray = white, Edge = C(0.98f, 0.97f, 0.95f) });
            fish.BodyPatterns.Add(VerticalBars(new[] { 20, 27, 34, 41, 48, 55, 62, 69, 76 }, 2, white, 1f, 0.25f));
            return fish;
        }

        // ---------- Lac d'Annecy ----------

        /// <summary>Ablette : petit poisson fin et argenté, dos vert-bleu, bouche tournée vers le haut, longue anale.</summary>
        private static DetailedFish Bleak()
        {
            var grey = C(0.70f, 0.74f, 0.76f);
            var fish = new DetailedFish
            {
                Width = 82, Height = 28, CenterY = 13.5f,
                Upper = V((14, 2f), (24, 4f), (40, 6.5f), (56, 6.5f), (68, 5f), (76, 3f), (80, 1.5f)),
                Lower = V((14, 2f), (24, 4f), (40, 6.5f), (56, 7f), (68, 6f), (76, 4f), (80, 2f)),
                Back = C(0.34f, 0.50f, 0.52f), Flank = C(0.86f, 0.90f, 0.92f), Belly = C(0.97f, 0.98f, 0.98f),
                FlankStart = 0.28f,
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 9f, Base = grey, Ray = C(0.48f, 0.52f, 0.54f), Edge = C(0.38f, 0.42f, 0.44f) },
                EyeX = 74, EyeYOffset = 1.5f, EyeRadius = 2, Iris = C(0.85f, 0.85f, 0.80f),
                GillX = 68, MouthXStart = 78, MouthXEnd = 81, MouthYOffset = 1.5f, MouthSlope = 0.6f,
                Seed = 71,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 38, XFront = 46, HeightRear = 2f, HeightFront = 5f, Base = grey, Ray = C(0.50f, 0.54f, 0.56f), Edge = C(0.86f, 0.90f, 0.92f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 24, XFront = 42, HeightRear = 2f, HeightFront = 5f, Base = grey, Ray = C(0.50f, 0.54f, 0.56f), Edge = C(0.86f, 0.90f, 0.92f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 66, BaseYOffset = -2f, Length = 6, Width = 1.5f, Slope = 0.25f, Base = C(0.90f, 0.92f, 0.92f), Ray = C(0.72f, 0.76f, 0.78f), Edge = C(0.98f, 0.98f, 0.98f) });
            return fish;
        }

        /// <summary>Chevesne : corps en torpille, grosse tête, écailles bordées de sombre, ventrales et anale rouge orangé.</summary>
        private static DetailedFish Chub()
        {
            var orange = C(0.90f, 0.44f, 0.20f);
            var dark = C(0.30f, 0.32f, 0.30f);
            var fish = new DetailedFish
            {
                Width = 94, Height = 38, CenterY = 18.5f,
                Upper = V((14, 3f), (24, 5.5f), (36, 9f), (50, 10.5f), (64, 10.5f), (76, 9f), (84, 6f), (90, 2.5f)),
                Lower = V((14, 3f), (24, 5f), (36, 8.5f), (50, 10f), (64, 10f), (76, 8.5f), (84, 5.5f), (90, 2f)),
                Back = C(0.24f, 0.30f, 0.28f), Flank = C(0.74f, 0.72f, 0.60f), Belly = C(0.94f, 0.92f, 0.86f),
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 11f, Base = dark, Ray = C(0.20f, 0.22f, 0.20f), Edge = C(0.14f, 0.14f, 0.14f) },
                EyeX = 82, EyeYOffset = 2.5f, EyeRadius = 2, Iris = C(0.86f, 0.72f, 0.34f),
                GillX = 74, GillStrength = 0.5f, MouthXStart = 87, MouthXEnd = 91, MouthYOffset = -0.5f,
                Seed = 72,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 42, XFront = 52, HeightRear = 3f, HeightFront = 8f, Base = dark, Ray = C(0.18f, 0.20f, 0.18f), Edge = C(0.40f, 0.42f, 0.40f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 24, XFront = 34, HeightRear = 3f, HeightFront = 7f, Shape = FinShape.Rounded, Base = orange, Ray = C(0.66f, 0.26f, 0.10f), Edge = C(0.98f, 0.62f, 0.38f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 46, XFront = 52, HeightRear = 1f, HeightFront = 6f, Base = orange, Ray = C(0.66f, 0.26f, 0.10f), Edge = C(0.98f, 0.62f, 0.38f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 72, BaseYOffset = -3f, Length = 8, Width = 2.5f, Slope = 0.25f, Base = C(0.94f, 0.66f, 0.44f), Ray = C(0.78f, 0.44f, 0.24f), Edge = C(0.98f, 0.82f, 0.64f) });
            // Bordure sombre des écailles : un filet en losanges sur les flancs.
            fish.BodyPatterns.Add((x, y, t, c) =>
                t > 0.2f && t < 0.75f && x > 18 && x < 74 && ((x + 2 * y) % 7 == 0 || (x - 2 * y + 700) % 7 == 0)
                    ? Color.Lerp(c, C(0.30f, 0.30f, 0.26f), 0.35f) : c);
            return fish;
        }

        /// <summary>Tanche : trapue, vert olive aux reflets dorés, écailles minuscules, œil rouge, nageoires arrondies.</summary>
        private static DetailedFish Tench()
        {
            var fin = C(0.24f, 0.30f, 0.16f);
            var fish = new DetailedFish
            {
                Width = 90, Height = 40, CenterY = 19.5f,
                Upper = V((14, 4f), (24, 7f), (36, 10.5f), (50, 12f), (64, 11.5f), (74, 9f), (82, 5.5f), (87, 2.5f)),
                Lower = V((14, 4f), (24, 6.5f), (36, 9.5f), (50, 11f), (64, 10.5f), (74, 8f), (82, 5f), (87, 2f)),
                Back = C(0.18f, 0.28f, 0.12f), Flank = C(0.42f, 0.52f, 0.22f), Belly = C(0.74f, 0.70f, 0.40f),
                ScaleTexture = false,
                Caudal = new Tail { Shape = TailShape.Truncate, XTip = 3, XBase = 15, Span = 11f, Base = fin, Ray = C(0.14f, 0.18f, 0.10f), Edge = C(0.36f, 0.42f, 0.24f) },
                EyeX = 80, EyeYOffset = 2.5f, EyeRadius = 1, Iris = C(0.90f, 0.30f, 0.16f),
                GillX = 72, GillStrength = 0.4f, MouthXStart = 84, MouthXEnd = 88, MouthYOffset = -0.5f,
                Seed = 73,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 40, XFront = 52, HeightRear = 5f, HeightFront = 7f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.14f, 0.18f, 0.10f), Edge = C(0.36f, 0.42f, 0.24f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 24, XFront = 34, HeightRear = 5f, HeightFront = 6f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.14f, 0.18f, 0.10f), Edge = C(0.36f, 0.42f, 0.24f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 46, XFront = 54, HeightRear = 5f, HeightFront = 6f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.14f, 0.18f, 0.10f), Edge = C(0.36f, 0.42f, 0.24f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 70, BaseYOffset = -4f, Length = 7, Width = 3f, Slope = 0.2f, Base = C(0.34f, 0.40f, 0.20f), Ray = C(0.20f, 0.26f, 0.12f), Edge = C(0.50f, 0.56f, 0.30f) });
            // Reflets dorés sur les flancs, et petit barbillon au coin de la bouche.
            fish.Details.Add(Spots(40, 20, 76, 0.35f, 0.7f, C(0.62f, 0.62f, 0.28f), null, 173, 0.4f));
            fish.Details.Add((f, p) => PixelArt.Set(p, 86, Mathf.RoundToInt(f.CenterY - 1.5f), C(0.30f, 0.32f, 0.16f)));
            return fish;
        }

        /// <summary>Carpe commune : corps haut et bronze doré, grandes écailles, longue dorsale, quatre barbillons.</summary>
        private static DetailedFish Carp()
        {
            var fin = C(0.52f, 0.40f, 0.22f);
            var fish = new DetailedFish
            {
                Width = 96, Height = 46, CenterY = 22.5f,
                Upper = V((14, 3.5f), (24, 7f), (36, 13f), (50, 15.5f), (64, 14f), (76, 10.5f), (85, 6f), (91, 2.5f)),
                Lower = V((14, 3.5f), (24, 6f), (36, 10.5f), (50, 12f), (64, 11.5f), (76, 9f), (85, 5f), (91, 2f)),
                Back = C(0.38f, 0.32f, 0.16f), Flank = C(0.78f, 0.62f, 0.30f), Belly = C(0.94f, 0.86f, 0.62f),
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 12f, Base = C(0.62f, 0.38f, 0.22f), Ray = C(0.44f, 0.26f, 0.14f), Edge = C(0.80f, 0.52f, 0.30f) },
                EyeX = 82, EyeYOffset = 3f, EyeRadius = 2, Iris = C(0.86f, 0.70f, 0.28f),
                GillX = 74, GillStrength = 0.5f, MouthXStart = 88, MouthXEnd = 92, MouthYOffset = -1f,
                Seed = 74,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 30, XFront = 62, HeightRear = 4f, HeightFront = 9f, Base = fin, Ray = C(0.34f, 0.26f, 0.12f), Edge = C(0.70f, 0.56f, 0.32f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 24, XFront = 32, HeightRear = 2f, HeightFront = 7f, Base = C(0.66f, 0.42f, 0.24f), Ray = C(0.44f, 0.26f, 0.14f), Edge = C(0.84f, 0.58f, 0.36f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 50, XFront = 58, HeightRear = 2f, HeightFront = 7f, Base = C(0.66f, 0.42f, 0.24f), Ray = C(0.44f, 0.26f, 0.14f), Edge = C(0.84f, 0.58f, 0.36f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 72, BaseYOffset = -4f, Length = 8, Width = 3f, Slope = 0.25f, Base = C(0.80f, 0.56f, 0.32f), Ray = C(0.58f, 0.36f, 0.18f), Edge = C(0.92f, 0.72f, 0.46f) });
            // Grandes écailles soulignées d'un trait sombre.
            fish.BodyPatterns.Add((x, y, t, c) =>
                t > 0.12f && t < 0.85f && x > 16 && x < 74 && ((x + 2 * y) % 9 == 0 || (x - 2 * y + 900) % 9 == 0)
                    ? Color.Lerp(c, C(0.36f, 0.26f, 0.12f), 0.4f) : c);
            fish.Details.Add((f, p) =>
            {
                // Deux barbillons visibles à la lèvre supérieure et au coin de la bouche.
                int mouth = Mathf.RoundToInt(f.CenterY - 1f);
                for (int k = 0; k < 3; k++)
                {
                    PixelArt.Set(p, 90 - k, mouth - 1 - k, C(0.70f, 0.52f, 0.30f));
                    PixelArt.Set(p, 87 - k, mouth - 1 - k, C(0.62f, 0.44f, 0.24f));
                }
            });
            return fish;
        }

        /// <summary>
        /// Lotte : longue et cylindrique, marbrée de brun, queue arrondie, longues dorsale et anale, barbillon au menton.
        /// Poisson des eaux froides et profondes, actif la nuit.
        /// </summary>
        private static DetailedFish Burbot()
        {
            var fin = C(0.40f, 0.34f, 0.20f);
            var fish = new DetailedFish
            {
                Width = 98, Height = 30, CenterY = 14.5f,
                Upper = V((10, 2f), (20, 4f), (36, 6.5f), (52, 8f), (68, 8.5f), (80, 7.5f), (88, 5f), (94, 2f)),
                Lower = V((10, 2f), (20, 4f), (36, 6f), (52, 7f), (68, 7.5f), (80, 7f), (88, 4.5f), (94, 1.5f)),
                Back = C(0.30f, 0.26f, 0.14f), Flank = C(0.52f, 0.46f, 0.28f), Belly = C(0.80f, 0.76f, 0.60f),
                ScaleTexture = false,
                Caudal = new Tail { Shape = TailShape.Round, XTip = 2, XBase = 11, Span = 7f, Base = fin, Ray = C(0.26f, 0.22f, 0.12f), Edge = C(0.56f, 0.50f, 0.32f) },
                EyeX = 86, EyeYOffset = 3f, EyeRadius = 1, Iris = C(0.70f, 0.62f, 0.30f),
                GillX = 78, GillStrength = 0.3f, MouthXStart = 90, MouthXEnd = 95, MouthYOffset = -0.5f,
                Seed = 75,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 12, XFront = 50, HeightRear = 4f, HeightFront = 4f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.26f, 0.22f, 0.12f), Edge = C(0.56f, 0.50f, 0.32f) });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 56, XFront = 64, HeightRear = 3f, HeightFront = 4f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.26f, 0.22f, 0.12f), Edge = C(0.56f, 0.50f, 0.32f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 12, XFront = 46, HeightRear = 4f, HeightFront = 4f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.26f, 0.22f, 0.12f), Edge = C(0.56f, 0.50f, 0.32f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 76, BaseYOffset = -2f, Length = 6, Width = 2.5f, Slope = 0.2f, Base = C(0.56f, 0.50f, 0.32f), Ray = C(0.36f, 0.30f, 0.18f), Edge = C(0.70f, 0.64f, 0.44f) });
            fish.Details.Add(Spots(90, 12, 86, 0f, 0.75f, C(0.20f, 0.16f, 0.08f), null, 175, 0.6f));
            fish.Details.Add((f, p) =>
            {
                // Barbillon unique sous le menton.
                int chin = Mathf.RoundToInt(f.CenterY - f.LowerAt(92));
                for (int k = 0; k < 3; k++) PixelArt.Set(p, 92, chin - 1 - k, C(0.50f, 0.44f, 0.26f));
            });
            return fish;
        }
    }
}
