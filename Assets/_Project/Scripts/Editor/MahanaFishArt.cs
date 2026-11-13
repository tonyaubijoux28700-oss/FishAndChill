using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.DetailedFish;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Fiches de dessin détaillé des 8 espèces de l'Île Mahana, d'après l'anatomie et la livrée réelles
    /// de chaque poisson (dessins originaux).
    /// </summary>
    public static class MahanaFishArt
    {
        private static Color C(float r, float g, float b) => new Color(r, g, b);

        public static bool TryGet(string speciesId, out DetailedFish fish)
        {
            switch (speciesId)
            {
                case "abudefduf-vaigiensis": fish = SergeantMajor(); return true;
                case "acanthurus-triostegus": fish = ConvictSurgeon(); return true;
                case "scarus-psittacus": fish = Parrotfish(); return true;
                case "cephalopholis-argus": fish = PeacockGrouper(); return true;
                case "caranx-melampygus": fish = BluefinTrevally(); return true;
                case "albula-glossodonta": fish = Bonefish(); return true;
                case "sphyraena-barracuda": fish = Barracuda(); return true;
                case "caranx-ignobilis": fish = GiantTrevally(); return true;
                default: fish = null; return false;
            }
        }

        /// <summary>Sergent-major : corps ovale haut, dos jaune, 5 bandes noires verticales.</summary>
        private static DetailedFish SergeantMajor()
        {
            var fin = C(0.82f, 0.80f, 0.58f);
            var fish = new DetailedFish
            {
                Width = 80, Height = 44, CenterY = 21.5f,
                Upper = V((14, 3f), (20, 6f), (30, 12.5f), (45, 14.5f), (58, 13.5f), (66, 10f), (72, 6f), (76, 2.5f)),
                Lower = V((14, 3f), (20, 5.5f), (30, 11f), (45, 12.5f), (58, 11f), (66, 8f), (72, 4.5f), (76, 2f)),
                Back = C(0.88f, 0.80f, 0.28f), Flank = C(0.88f, 0.89f, 0.80f), Belly = C(0.97f, 0.97f, 0.95f),
                FlankStart = 0.32f,
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 11f, Base = fin, Ray = C(0.62f, 0.60f, 0.40f), Edge = C(0.35f, 0.35f, 0.30f) },
                EyeX = 67, EyeYOffset = 3f, EyeRadius = 2, Iris = C(0.92f, 0.82f, 0.40f),
                GillX = 61, MouthXStart = 73, MouthXEnd = 76, MouthYOffset = -0.5f,
                Seed = 11,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 24, XFront = 62, HeightRear = 7f, HeightFront = 4f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.66f, 0.63f, 0.42f), Edge = C(0.95f, 0.92f, 0.70f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 26, XFront = 46, HeightRear = 6f, HeightFront = 4f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.66f, 0.63f, 0.42f), Edge = C(0.95f, 0.92f, 0.70f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 52, XFront = 57, HeightRear = 1f, HeightFront = 5f, Base = fin, Ray = C(0.66f, 0.63f, 0.42f), Edge = C(0.95f, 0.92f, 0.70f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 60, BaseYOffset = -2f, Length = 7, Width = 2.5f, Slope = 0.2f, Base = C(0.92f, 0.92f, 0.82f), Ray = C(0.80f, 0.80f, 0.70f), Edge = C(0.98f, 0.97f, 0.90f) });
            fish.BodyPatterns.Add(VerticalBars(new[] { 25, 34, 43, 52, 60 }, 3, C(0.08f, 0.08f, 0.10f), 0.82f));
            return fish;
        }

        /// <summary>Chirurgien bagnard : ovale vert pâle, 6 fines bandes noires dont une à travers l'œil, épine sur le pédoncule.</summary>
        private static DetailedFish ConvictSurgeon()
        {
            var fin = C(0.80f, 0.84f, 0.70f);
            var fish = new DetailedFish
            {
                Width = 80, Height = 44, CenterY = 21.5f,
                Upper = V((14, 2.5f), (20, 6f), (30, 13.5f), (45, 15.5f), (58, 14f), (66, 10.5f), (71, 6.5f), (75, 2.5f)),
                Lower = V((14, 2.5f), (20, 5.5f), (30, 12f), (45, 13.5f), (58, 12f), (66, 8.5f), (71, 4.5f), (75, 1.5f)),
                Back = C(0.70f, 0.78f, 0.64f), Flank = C(0.82f, 0.88f, 0.80f), Belly = C(0.95f, 0.96f, 0.92f),
                Caudal = new Tail { Shape = TailShape.Lunate, XTip = 3, XBase = 15, Span = 11f, Base = fin, Ray = C(0.62f, 0.68f, 0.56f), Edge = C(0.90f, 0.92f, 0.82f) },
                EyeX = 66, EyeYOffset = 3f, EyeRadius = 2, Iris = C(0.30f, 0.32f, 0.30f),
                GillX = 60, MouthXStart = 73, MouthXEnd = 75, MouthYOffset = -1f,
                Seed = 12,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 22, XFront = 64, HeightRear = 6f, HeightFront = 4f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.64f, 0.70f, 0.56f), Edge = C(0.94f, 0.95f, 0.86f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 24, XFront = 52, HeightRear = 6f, HeightFront = 4f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.64f, 0.70f, 0.56f), Edge = C(0.94f, 0.95f, 0.86f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 54, XFront = 58, HeightRear = 1f, HeightFront = 4f, Base = fin, Ray = C(0.64f, 0.70f, 0.56f), Edge = C(0.94f, 0.95f, 0.86f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 59, BaseYOffset = -1f, Length = 7, Width = 2.5f, Slope = 0.15f, Base = C(0.90f, 0.92f, 0.84f), Ray = C(0.76f, 0.80f, 0.70f), Edge = C(0.97f, 0.97f, 0.92f) });
            fish.BodyPatterns.Add(VerticalBars(new[] { 22, 32, 42, 51, 59, 66 }, 2, C(0.08f, 0.10f, 0.10f), 0.85f));
            fish.Details.Add((f, p) =>
            {
                // Le « scalpel » : épine claire sur le pédoncule caudal.
                int y = Mathf.RoundToInt(f.CenterY);
                PixelArt.Set(p, 17, y, C(0.97f, 0.97f, 0.95f));
                PixelArt.Set(p, 18, y, C(0.97f, 0.97f, 0.95f));
            });
            return fish;
        }

        /// <summary>Perroquet commun : turquoise à grandes écailles bordées de rose, bec soudé, nageoires roses liserées de bleu.</summary>
        private static DetailedFish Parrotfish()
        {
            var pink = C(0.93f, 0.52f, 0.60f);
            var blueEdge = C(0.25f, 0.55f, 0.90f);
            var fish = new DetailedFish
            {
                Width = 96, Height = 40, CenterY = 19.5f,
                Upper = V((14, 3f), (22, 6f), (34, 11f), (50, 12.5f), (66, 12f), (76, 10f), (84, 7f), (89, 4f), (92, 2f)),
                Lower = V((14, 3f), (22, 5.5f), (34, 10f), (50, 11f), (66, 10.5f), (76, 9f), (84, 6f), (89, 3f), (92, 1.5f)),
                Back = C(0.12f, 0.50f, 0.52f), Flank = C(0.22f, 0.68f, 0.64f), Belly = C(0.55f, 0.85f, 0.74f),
                ScaleTexture = false,
                Caudal = new Tail { Shape = TailShape.Lunate, XTip = 2, XBase = 15, Span = 12f, Base = C(0.22f, 0.64f, 0.60f), Ray = pink, Edge = pink },
                EyeX = 82, EyeYOffset = 3f, EyeRadius = 1, Iris = C(0.95f, 0.60f, 0.25f),
                GillX = 76, GillTint = pink,
                MouthXStart = 88, MouthXEnd = 92, MouthYOffset = -1f,
                Seed = 13,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 32, XFront = 76, HeightRear = 4.5f, HeightFront = 3.5f, Base = pink, Ray = C(0.80f, 0.40f, 0.50f), Edge = blueEdge, Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 40, XFront = 66, HeightRear = 4.5f, HeightFront = 3.5f, Base = pink, Ray = C(0.80f, 0.40f, 0.50f), Edge = blueEdge });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 64, XFront = 68, HeightRear = 1f, HeightFront = 4f, Base = pink, Ray = C(0.80f, 0.40f, 0.50f), Edge = blueEdge });
            fish.PectoralFins.Add(new Pectoral { BaseX = 74, BaseYOffset = -1f, Length = 7, Width = 2f, Slope = 0.15f, Base = C(0.35f, 0.70f, 0.85f), Ray = C(0.25f, 0.55f, 0.75f), Edge = blueEdge });

            // Grandes écailles imbriquées, au bord arrière rosé (en forme de « ( » car la tête est à droite).
            fish.BodyPatterns.Add((x, y, t, c) =>
            {
                if (t > 0.85f || x < 20 || x > 78) return c;
                int row = Mathf.FloorToInt(y / 3f);
                int sx = ((x + (row % 2) * 3) % 6 + 6) % 6;
                int sy = ((y % 3) + 3) % 3;
                bool edge = (sx == 0 && sy == 1) || (sx == 1 && sy != 1);
                return edge ? Color.Lerp(c, pink, 0.5f) : c;
            });

            fish.Details.Add((f, p) =>
            {
                // Bandes roses autour du museau.
                for (int x = 80; x <= 88; x++)
                {
                    int y1 = Mathf.RoundToInt(f.CenterY - 2f + (x - 80) * 0.15f);
                    if (f.InBody(x, y1)) p[x, y1] = Color.Lerp(p[x, y1], pink, 0.7f);
                }

                // Bec soudé (dents en plaque), crème avec une ligne de séparation.
                for (int x = 88; x <= 93; x++)
                {
                    for (int y = Mathf.RoundToInt(f.CenterY - 2.5f); y <= Mathf.RoundToInt(f.CenterY + 1.5f); y++)
                    {
                        if (f.InBody(x, y)) p[x, y] = C(0.93f, 0.93f, 0.84f);
                    }

                    int mid = Mathf.RoundToInt(f.CenterY - 0.5f);
                    if (f.InBody(x, mid)) p[x, mid] = C(0.55f, 0.55f, 0.48f);
                }
            });
            return fish;
        }

        /// <summary>Mérou paon : corps brun-bleu nuit criblé de points bleus, queue arrondie, grande gueule.</summary>
        private static DetailedFish PeacockGrouper()
        {
            var dark = C(0.15f, 0.17f, 0.31f);
            var blue = C(0.52f, 0.76f, 1.00f);
            var fish = new DetailedFish
            {
                Width = 96, Height = 40, CenterY = 19.5f,
                Upper = V((14, 4f), (22, 6.5f), (34, 10f), (50, 11.5f), (66, 11f), (76, 9.5f), (84, 7f), (90, 4f), (93, 1.5f)),
                Lower = V((14, 4f), (22, 6f), (34, 9f), (50, 10f), (66, 10f), (76, 9.5f), (84, 7.5f), (90, 5f), (94, 2f)),
                Back = C(0.10f, 0.12f, 0.22f), Flank = C(0.19f, 0.21f, 0.37f), Belly = C(0.27f, 0.29f, 0.45f),
                Caudal = new Tail { Shape = TailShape.Round, XTip = 2, XBase = 15, Span = 10f, Base = dark, Ray = C(0.11f, 0.12f, 0.24f), Edge = C(0.70f, 0.80f, 0.95f) },
                EyeX = 85, EyeYOffset = 3.5f, EyeRadius = 1, Iris = blue,
                GillX = 77, MouthXStart = 83, MouthXEnd = 94, MouthYOffset = -0.5f, MouthSlope = 0.08f,
                MouthColor = C(0.05f, 0.05f, 0.08f),
                Seed = 14,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 30, XFront = 78, HeightRear = 6.5f, HeightFront = 3.5f, Shape = FinShape.Rounded, Base = dark, Ray = C(0.11f, 0.12f, 0.24f), Edge = C(0.35f, 0.55f, 0.95f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 26, XFront = 42, HeightRear = 6f, HeightFront = 3f, Shape = FinShape.Rounded, Base = dark, Ray = C(0.11f, 0.12f, 0.24f), Edge = C(0.35f, 0.55f, 0.95f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 64, XFront = 70, HeightRear = 1f, HeightFront = 5f, Base = dark, Ray = C(0.11f, 0.12f, 0.24f), Edge = C(0.35f, 0.55f, 0.95f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 76, BaseYOffset = -2f, Length = 8, Width = 3f, Slope = 0.15f, Base = C(0.32f, 0.24f, 0.28f), Ray = C(0.25f, 0.18f, 0.22f), Edge = C(0.45f, 0.35f, 0.40f) });

            // Le semis de points bleus, sur le corps et les nageoires.
            fish.Details.Add(Spots(110, 4, 92, 0f, 1f, blue, C(0.05f, 0.06f, 0.12f), 140, 0.15f));
            return fish;
        }

        /// <summary>Carangue à points bleus : bronze et argent semé de points sombres, nageoires bleu électrique, queue en croissant.</summary>
        private static DetailedFish BluefinTrevally()
        {
            var blue = C(0.20f, 0.45f, 0.92f);
            var fish = new DetailedFish
            {
                Width = 96, Height = 40, CenterY = 19.5f,
                Upper = V((13, 1.8f), (20, 3f), (32, 10f), (50, 14f), (64, 13.5f), (74, 11f), (82, 7.5f), (88, 3.5f), (91, 1.5f)),
                Lower = V((13, 1.8f), (20, 3f), (32, 8.5f), (50, 11.5f), (64, 11f), (74, 9.5f), (82, 6.5f), (88, 3f), (91, 1f)),
                Back = C(0.40f, 0.46f, 0.42f), Flank = C(0.70f, 0.72f, 0.64f), Belly = C(0.92f, 0.93f, 0.92f),
                Caudal = new Tail { Shape = TailShape.Lunate, XTip = 1, XBase = 14, Span = 15f, Base = blue, Ray = C(0.14f, 0.32f, 0.72f), Edge = C(0.10f, 0.22f, 0.55f) },
                EyeX = 82, EyeYOffset = 2f, EyeRadius = 2, Iris = C(0.85f, 0.78f, 0.45f),
                GillX = 74, MouthXStart = 85, MouthXEnd = 90, MouthYOffset = -2f, MouthSlope = -0.25f,
                Seed = 15,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 26, XFront = 58, HeightRear = 1.5f, HeightFront = 9f, Shape = FinShape.Falcate, Base = blue, Ray = C(0.14f, 0.32f, 0.72f), Edge = C(0.50f, 0.70f, 1.00f) });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 61, XFront = 68, HeightRear = 2f, HeightFront = 3.5f, Base = C(0.45f, 0.50f, 0.55f), Ray = C(0.30f, 0.34f, 0.40f), Edge = C(0.60f, 0.65f, 0.70f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 26, XFront = 50, HeightRear = 1.5f, HeightFront = 8f, Shape = FinShape.Falcate, Base = blue, Ray = C(0.14f, 0.32f, 0.72f), Edge = C(0.50f, 0.70f, 1.00f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 73, BaseYOffset = 1f, Length = 15, Width = 1.6f, Slope = 0.3f, Base = C(0.72f, 0.80f, 0.88f), Ray = C(0.55f, 0.65f, 0.78f), Edge = C(0.85f, 0.90f, 0.95f) });
            fish.Details.Add(Spots(36, 28, 80, 0.05f, 0.55f, C(0.10f, 0.16f, 0.40f), null, 150, 0.2f));
            fish.Details.Add(Scutes(14, 40));
            return fish;
        }

        /// <summary>Banane de mer : longue et argentée, fines lignes sombres sur le dos, museau conique, queue très fourchue.</summary>
        private static DetailedFish Bonefish()
        {
            var fish = new DetailedFish
            {
                Width = 96, Height = 32, CenterY = 15.5f,
                Upper = V((13, 2f), (20, 3f), (32, 6.5f), (50, 8f), (66, 7.5f), (78, 6f), (86, 4f), (91, 2.2f), (94, 0.8f)),
                Lower = V((13, 2f), (20, 3f), (32, 6f), (50, 7.5f), (66, 7f), (78, 6f), (86, 4.5f), (90, 2.5f), (92, 1f)),
                Back = C(0.42f, 0.52f, 0.60f), Flank = C(0.78f, 0.83f, 0.88f), Belly = C(0.96f, 0.97f, 0.99f),
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 1, XBase = 14, Span = 11f, Base = C(0.70f, 0.76f, 0.82f), Ray = C(0.52f, 0.58f, 0.65f), Edge = C(0.30f, 0.36f, 0.42f) },
                EyeX = 84, EyeYOffset = 1.5f, EyeRadius = 2, Iris = C(0.80f, 0.82f, 0.70f),
                GillX = 77, MouthXStart = 86, MouthXEnd = 90, MouthYOffset = -3f,
                Seed = 16,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 50, XFront = 62, HeightRear = 1.5f, HeightFront = 7f, Base = C(0.72f, 0.76f, 0.80f), Ray = C(0.55f, 0.60f, 0.66f), Edge = C(0.88f, 0.90f, 0.92f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 24, XFront = 31, HeightRear = 1f, HeightFront = 4f, Base = C(0.85f, 0.85f, 0.80f), Ray = C(0.70f, 0.70f, 0.65f), Edge = C(0.95f, 0.95f, 0.92f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 46, XFront = 51, HeightRear = 1f, HeightFront = 4f, Base = C(0.85f, 0.85f, 0.80f), Ray = C(0.70f, 0.70f, 0.65f), Edge = C(0.95f, 0.95f, 0.92f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 76, BaseYOffset = -3f, Length = 7, Width = 1.8f, Slope = 0.15f, Base = C(0.85f, 0.85f, 0.80f), Ray = C(0.70f, 0.70f, 0.65f), Edge = C(0.95f, 0.95f, 0.92f) });
            fish.BodyPatterns.Add(Lines(new[] { 0.12f, 0.24f, 0.36f }, C(0.35f, 0.44f, 0.52f), 0.55f, 18, 80));
            return fish;
        }

        /// <summary>Grand barracuda : très long, mâchoire inférieure saillante et crocs, barres obliques sur le dos, taches noires à l'arrière.</summary>
        private static DetailedFish Barracuda()
        {
            var fin = C(0.40f, 0.45f, 0.50f);
            var fish = new DetailedFish
            {
                Width = 112, Height = 28, CenterY = 13.5f,
                Upper = V((14, 2f), (20, 2.8f), (30, 5f), (50, 6f), (72, 5.8f), (88, 4.5f), (98, 2.8f), (104, 1.5f), (108, 0.8f)),
                Lower = V((14, 2f), (20, 2.8f), (30, 4.8f), (50, 5.8f), (72, 5.6f), (88, 4.6f), (98, 3.5f), (104, 2.4f), (110, 1.4f)),
                Back = C(0.26f, 0.34f, 0.40f), Flank = C(0.70f, 0.75f, 0.78f), Belly = C(0.95f, 0.96f, 0.96f),
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 1, XBase = 15, Span = 9f, Base = C(0.30f, 0.34f, 0.38f), Ray = C(0.20f, 0.23f, 0.26f), Edge = C(0.92f, 0.94f, 0.95f) },
                EyeX = 95, EyeYOffset = 1.5f, EyeRadius = 1, Iris = C(0.80f, 0.80f, 0.55f),
                GillX = 86, MouthXStart = 92, MouthXEnd = 109, MouthYOffset = -0.5f, MouthSlope = 0.02f,
                Seed = 17,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 64, XFront = 71, HeightRear = 1f, HeightFront = 5f, Base = fin, Ray = C(0.28f, 0.32f, 0.36f), Edge = C(0.55f, 0.60f, 0.65f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 28, XFront = 35, HeightRear = 1.5f, HeightFront = 5f, Base = fin, Ray = C(0.28f, 0.32f, 0.36f), Edge = C(0.92f, 0.94f, 0.95f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 26, XFront = 33, HeightRear = 1.5f, HeightFront = 4.5f, Base = fin, Ray = C(0.28f, 0.32f, 0.36f), Edge = C(0.92f, 0.94f, 0.95f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 58, XFront = 63, HeightRear = 1f, HeightFront = 3.5f, Base = C(0.80f, 0.82f, 0.82f), Ray = C(0.62f, 0.65f, 0.66f), Edge = C(0.92f, 0.94f, 0.95f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 85, BaseYOffset = -1.5f, Length = 7, Width = 1.5f, Slope = 0.15f, Base = C(0.75f, 0.78f, 0.80f), Ray = C(0.60f, 0.63f, 0.66f), Edge = C(0.88f, 0.90f, 0.92f) });

            // Barres obliques sombres sur le haut du corps.
            fish.BodyPatterns.Add((x, y, t, c) =>
            {
                if (t > 0.42f || x < 22 || x > 88) return c;
                int shifted = Mathf.RoundToInt(x + y * 0.6f);
                return shifted % 5 < 2 ? Color.Lerp(c, C(0.18f, 0.24f, 0.28f), 0.65f) : c;
            });

            // Taches noires éparses sur l'arrière du flanc.
            fish.Details.Add(Spots(14, 18, 62, 0.5f, 0.85f, C(0.08f, 0.09f, 0.10f), null, 170, 0.5f));

            // Crocs sur la mâchoire inférieure.
            fish.Details.Add((f, p) =>
            {
                for (int x = 95; x <= 107; x += 2)
                {
                    int y = Mathf.RoundToInt(f.CenterY - 1.5f - (x - 92) * 0.02f);
                    if (f.InBody(x, y)) p[x, y] = C(0.98f, 0.98f, 0.96f);
                }
            });
            return fish;
        }

        /// <summary>Carangue géante : corps très haut, front abrupt, gris acier, nageoires sombres en faux, écussons sur le pédoncule.</summary>
        private static DetailedFish GiantTrevally()
        {
            var finDark = C(0.20f, 0.22f, 0.26f);
            var fish = new DetailedFish
            {
                Width = 96, Height = 48, CenterY = 23.5f,
                Upper = V((13, 2f), (20, 3.5f), (30, 12f), (46, 19f), (60, 19f), (70, 16.5f), (78, 12f), (84, 7f), (87, 3f), (89, 1f)),
                Lower = V((13, 2f), (20, 3.5f), (30, 10f), (46, 14.5f), (60, 14f), (70, 12f), (78, 9f), (84, 5.5f), (89, 1.5f)),
                Back = C(0.24f, 0.27f, 0.31f), Flank = C(0.55f, 0.58f, 0.62f), Belly = C(0.83f, 0.85f, 0.87f),
                FlankStart = 0.4f,
                Caudal = new Tail { Shape = TailShape.Lunate, XTip = 1, XBase = 14, Span = 19f, Base = finDark, Ray = C(0.13f, 0.14f, 0.17f), Edge = C(0.10f, 0.11f, 0.13f) },
                EyeX = 79, EyeYOffset = 5f, EyeRadius = 2, Iris = C(0.75f, 0.72f, 0.55f),
                GillX = 71, MouthXStart = 81, MouthXEnd = 88, MouthYOffset = -3f, MouthSlope = -0.3f,
                Seed = 18,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 26, XFront = 52, HeightRear = 1.5f, HeightFront = 11f, Shape = FinShape.Falcate, Base = finDark, Ray = C(0.13f, 0.14f, 0.17f), Edge = C(0.35f, 0.38f, 0.42f) });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 56, XFront = 63, HeightRear = 2f, HeightFront = 4f, Base = finDark, Ray = C(0.13f, 0.14f, 0.17f), Edge = C(0.35f, 0.38f, 0.42f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 26, XFront = 46, HeightRear = 1.5f, HeightFront = 9f, Shape = FinShape.Falcate, Base = finDark, Ray = C(0.13f, 0.14f, 0.17f), Edge = C(0.35f, 0.38f, 0.42f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 58, XFront = 62, HeightRear = 1f, HeightFront = 4f, Base = finDark, Ray = C(0.13f, 0.14f, 0.17f), Edge = C(0.35f, 0.38f, 0.42f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 70, BaseYOffset = 2f, Length = 19, Width = 2f, Slope = 0.35f, Base = C(0.62f, 0.65f, 0.70f), Ray = C(0.48f, 0.51f, 0.56f), Edge = C(0.78f, 0.80f, 0.84f) });
            fish.Details.Add(Spots(40, 30, 76, 0.05f, 0.5f, C(0.16f, 0.18f, 0.21f), null, 180, 0.1f));
            fish.Details.Add(Scutes(14, 42));
            return fish;
        }

        /// <summary>Écussons osseux le long de la ligne latérale arrière (carangues).</summary>
        private static System.Action<DetailedFish, Color[,]> Scutes(int xMin, int xMax)
        {
            return (f, p) =>
            {
                int y = Mathf.RoundToInt(f.CenterY);
                for (int x = xMin; x <= xMax; x++)
                {
                    if (!f.InBody(x, y)) continue;
                    p[x, y] = x % 2 == 0 ? C(0.25f, 0.27f, 0.28f) : C(0.80f, 0.82f, 0.80f);
                }
            };
        }

        private static Vector2[] V(params (int x, float h)[] points)
        {
            var result = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                result[i] = new Vector2(points[i].x, points[i].h);
            }

            return result;
        }
    }
}
