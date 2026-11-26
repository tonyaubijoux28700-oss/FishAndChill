using UnityEngine;
using static FishAndChill.Editor.DetailedFish;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Fiches de dessin détaillé des poissons du lac d'Annecy (eau douce alpine), d'après l'anatomie et la livrée
    /// réelles de chaque espèce (dessins originaux).
    /// </summary>
    public static class AnnecyFishArt
    {
        private static Color C(float r, float g, float b) => new Color(r, g, b);

        public static bool TryGet(string speciesId, out DetailedFish fish)
        {
            switch (speciesId)
            {
                case "rutilus-rutilus": fish = Roach(); return true;
                case "perca-fluviatilis": fish = Perch(); return true;
                case "salmo-trutta": fish = BrownTrout(); return true;
                case "salvelinus-umbla": fish = ArcticCharr(); return true;
                case "coregonus-lavaretus": fish = Whitefish(); return true;
                case "esox-lucius": fish = Pike(); return true;
                default: fish = null; return false;
            }
        }

        /// <summary>Gardon : corps haut et argenté, dos bleu-vert, œil rouge, nageoires ventrales et anale rouge orangé.</summary>
        private static DetailedFish Roach()
        {
            var red = C(0.86f, 0.36f, 0.22f);
            var grey = C(0.55f, 0.58f, 0.58f);
            var fish = new DetailedFish
            {
                Width = 88, Height = 40, CenterY = 19.5f,
                Upper = V((14, 2.5f), (22, 5f), (34, 10f), (48, 11.5f), (62, 10.5f), (72, 8f), (80, 5f), (85, 2.5f)),
                Lower = V((14, 2.5f), (22, 4.5f), (34, 9f), (48, 10.5f), (62, 9.5f), (72, 7f), (80, 4f), (85, 1.5f)),
                Back = C(0.24f, 0.36f, 0.38f), Flank = C(0.76f, 0.80f, 0.80f), Belly = C(0.95f, 0.96f, 0.94f),
                FlankStart = 0.30f,
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 11f, Base = grey, Ray = C(0.40f, 0.42f, 0.42f), Edge = C(0.30f, 0.32f, 0.34f) },
                EyeX = 77, EyeYOffset = 2.5f, EyeRadius = 2, Iris = C(0.90f, 0.22f, 0.16f),
                GillX = 71, MouthXStart = 83, MouthXEnd = 86, MouthYOffset = 0f,
                Seed = 31,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 40, XFront = 52, HeightRear = 3f, HeightFront = 8f, Base = grey, Ray = C(0.36f, 0.38f, 0.38f), Edge = C(0.30f, 0.32f, 0.34f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 24, XFront = 34, HeightRear = 2f, HeightFront = 6f, Base = red, Ray = C(0.62f, 0.22f, 0.12f), Edge = C(0.95f, 0.55f, 0.36f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 46, XFront = 52, HeightRear = 1f, HeightFront = 6f, Base = red, Ray = C(0.62f, 0.22f, 0.12f), Edge = C(0.95f, 0.55f, 0.36f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 70, BaseYOffset = -4f, Length = 7, Width = 2f, Slope = 0.25f, Base = C(0.92f, 0.62f, 0.48f), Ray = C(0.78f, 0.42f, 0.30f), Edge = C(0.98f, 0.78f, 0.62f) });
            // Fine ligne latérale sombre qui descend vers la queue.
            fish.BodyPatterns.Add(Lines(new[] { 0.42f }, C(0.45f, 0.50f, 0.52f), 0.35f, 18, 70));
            return fish;
        }

        /// <summary>
        /// Perche : vert doré, 6 bandes sombres verticales, deux dorsales (la première épineuse avec une tache noire
        /// à l'arrière), nageoires ventrales et anale rouge vif.
        /// </summary>
        private static DetailedFish Perch()
        {
            var red = C(0.92f, 0.30f, 0.14f);
            var olive = C(0.62f, 0.62f, 0.40f);
            var fish = new DetailedFish
            {
                Width = 90, Height = 42, CenterY = 19.5f,
                Upper = V((14, 3f), (22, 5.5f), (32, 10.5f), (46, 13f), (60, 12f), (70, 9f), (78, 6f), (85, 2.5f)),
                Lower = V((14, 3f), (22, 5f), (32, 9f), (46, 10.5f), (60, 10f), (70, 8f), (78, 5f), (85, 1.5f)),
                Back = C(0.26f, 0.38f, 0.16f), Flank = C(0.62f, 0.68f, 0.30f), Belly = C(0.94f, 0.92f, 0.78f),
                FlankStart = 0.30f,
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 11f, Base = olive, Ray = C(0.42f, 0.42f, 0.24f), Edge = C(0.80f, 0.50f, 0.25f) },
                EyeX = 78, EyeYOffset = 3f, EyeRadius = 2, Iris = C(0.92f, 0.70f, 0.20f),
                GillX = 70, GillStrength = 0.5f, MouthXStart = 82, MouthXEnd = 86, MouthYOffset = 0f,
                Seed = 32,
            };
            // Première dorsale haute et épineuse, seconde dorsale molle.
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 48, XFront = 66, HeightRear = 5f, HeightFront = 9f, Shape = FinShape.Rounded, Base = C(0.52f, 0.54f, 0.40f), Ray = C(0.30f, 0.30f, 0.22f), Edge = C(0.80f, 0.80f, 0.62f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 26, XFront = 44, HeightRear = 4f, HeightFront = 6f, Shape = FinShape.Rounded, Base = olive, Ray = C(0.42f, 0.42f, 0.26f), Edge = C(0.82f, 0.80f, 0.60f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 26, XFront = 36, HeightRear = 2f, HeightFront = 6f, Base = red, Ray = C(0.64f, 0.18f, 0.08f), Edge = C(1.00f, 0.55f, 0.32f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 60, XFront = 66, HeightRear = 1f, HeightFront = 7f, Base = red, Ray = C(0.64f, 0.18f, 0.08f), Edge = C(1.00f, 0.55f, 0.32f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 68, BaseYOffset = -2f, Length = 7, Width = 2.5f, Slope = 0.2f, Base = C(0.92f, 0.78f, 0.42f), Ray = C(0.76f, 0.58f, 0.26f), Edge = C(0.98f, 0.90f, 0.60f) });
            fish.BodyPatterns.Add(VerticalBars(new[] { 22, 31, 40, 49, 57, 64 }, 3, C(0.14f, 0.20f, 0.08f), 0.70f, 0.1f));
            fish.Details.Add((f, p) =>
            {
                // Tache noire à l'arrière de la première dorsale.
                for (int x = 48; x <= 51; x++)
                {
                    for (int y = Mathf.RoundToInt(f.CenterY + f.UpperAt(x)) + 1; y <= Mathf.RoundToInt(f.CenterY + f.UpperAt(x)) + 4; y++)
                    {
                        if (PixelArt.IsFilled(p, x, y)) p[x, y] = C(0.06f, 0.06f, 0.05f);
                    }
                }
            });
            return fish;
        }

        /// <summary>
        /// Truite fario : fuselée, dos brun-olive, flancs dorés semés de points noirs et de points rouges cerclés de
        /// blanc, petite nageoire adipeuse orangée, queue presque droite.
        /// </summary>
        private static DetailedFish BrownTrout()
        {
            var fin = C(0.62f, 0.52f, 0.34f);
            var fish = new DetailedFish
            {
                Width = 96, Height = 36, CenterY = 17.5f,
                Upper = V((13, 3f), (22, 4.5f), (34, 8f), (50, 9.5f), (66, 9f), (78, 7f), (86, 4.5f), (92, 2f)),
                Lower = V((13, 3f), (22, 4.5f), (34, 7.5f), (50, 9f), (66, 8.5f), (78, 6.5f), (86, 4f), (92, 1.5f)),
                Back = C(0.34f, 0.30f, 0.16f), Flank = C(0.76f, 0.62f, 0.32f), Belly = C(0.96f, 0.90f, 0.72f),
                FlankStart = 0.32f,
                Caudal = new Tail { Shape = TailShape.Truncate, XTip = 2, XBase = 14, Span = 10f, Base = fin, Ray = C(0.44f, 0.36f, 0.22f), Edge = C(0.36f, 0.30f, 0.18f) },
                EyeX = 84, EyeYOffset = 2.5f, EyeRadius = 2, Iris = C(0.90f, 0.74f, 0.32f),
                GillX = 77, MouthXStart = 86, MouthXEnd = 92, MouthYOffset = -0.5f, MouthSlope = -0.15f,
                Seed = 33,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 46, XFront = 58, HeightRear = 3f, HeightFront = 7f, Base = fin, Ray = C(0.42f, 0.34f, 0.20f), Edge = C(0.82f, 0.74f, 0.52f) });
            // Nageoire adipeuse : petite, charnue, orangée.
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 21, XFront = 25, HeightRear = 1.5f, HeightFront = 3f, Shape = FinShape.Rounded, Base = C(0.88f, 0.52f, 0.26f), Ray = C(0.88f, 0.52f, 0.26f), Edge = C(0.96f, 0.68f, 0.40f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 26, XFront = 34, HeightRear = 2f, HeightFront = 5f, Base = fin, Ray = C(0.44f, 0.36f, 0.22f), Edge = C(0.95f, 0.88f, 0.70f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 50, XFront = 56, HeightRear = 1f, HeightFront = 5f, Base = fin, Ray = C(0.44f, 0.36f, 0.22f), Edge = C(0.95f, 0.88f, 0.70f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 76, BaseYOffset = -3f, Length = 7, Width = 2f, Slope = 0.25f, Base = C(0.82f, 0.68f, 0.44f), Ray = C(0.62f, 0.48f, 0.28f), Edge = C(0.95f, 0.86f, 0.64f) });
            fish.Details.Add(Spots(26, 24, 84, 0.05f, 0.55f, C(0.10f, 0.08f, 0.06f), null, 331, 0.4f));
            fish.Details.Add((f, p) =>
            {
                // Points rouges cerclés de blanc sur le milieu des flancs.
                var random = new System.Random(332);
                for (int i = 0; i < 9; i++)
                {
                    int x = random.Next(26, 76);
                    int y = Mathf.RoundToInt(f.CenterY + random.Next(-3, 2));
                    if (!f.InBody(x, y) || !f.InBody(x + 1, y + 1) || !f.InBody(x - 1, y - 1)) continue;
                    foreach (Vector2Int d in new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down })
                    {
                        p[x + d.x, y + d.y] = Color.Lerp(p[x + d.x, y + d.y], C(0.98f, 0.96f, 0.90f), 0.7f);
                    }

                    p[x, y] = C(0.85f, 0.16f, 0.12f);
                }
            });
            return fish;
        }

        /// <summary>
        /// Omble chevalier : dos vert-gris sombre, ventre rouge orangé (livrée de fraie), petits points crème,
        /// nageoires paires et anale rouges au bord d'attaque blanc pur, nageoire adipeuse.
        /// </summary>
        private static DetailedFish ArcticCharr()
        {
            var red = C(0.90f, 0.36f, 0.18f);
            var white = C(0.99f, 0.99f, 0.97f);
            var fish = new DetailedFish
            {
                Width = 96, Height = 36, CenterY = 17.5f,
                Upper = V((13, 3f), (22, 4.5f), (34, 8.5f), (50, 10f), (66, 9.5f), (78, 7f), (86, 4.5f), (92, 2f)),
                Lower = V((13, 3f), (22, 4.5f), (34, 8f), (50, 9.5f), (66, 9f), (78, 6.5f), (86, 4f), (92, 1.5f)),
                Back = C(0.22f, 0.28f, 0.26f), Flank = C(0.50f, 0.54f, 0.46f), Belly = C(0.92f, 0.42f, 0.20f),
                FlankStart = 0.30f, BellyStart = 0.58f,
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 14, Span = 10f, Base = C(0.36f, 0.40f, 0.36f), Ray = C(0.24f, 0.28f, 0.24f), Edge = C(0.20f, 0.24f, 0.22f) },
                EyeX = 84, EyeYOffset = 2.5f, EyeRadius = 2, Iris = C(0.80f, 0.70f, 0.40f),
                GillX = 77, MouthXStart = 87, MouthXEnd = 92, MouthYOffset = -0.5f,
                Seed = 34,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 46, XFront = 58, HeightRear = 3f, HeightFront = 7f, Base = C(0.34f, 0.38f, 0.34f), Ray = C(0.22f, 0.26f, 0.22f), Edge = C(0.50f, 0.54f, 0.48f) });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 21, XFront = 25, HeightRear = 1.5f, HeightFront = 3f, Shape = FinShape.Rounded, Base = C(0.32f, 0.36f, 0.32f), Ray = C(0.32f, 0.36f, 0.32f), Edge = C(0.45f, 0.50f, 0.44f) });
            // Bord d'attaque blanc pur : c'est la nageoire la plus « avant » qui porte le liseré (Edge).
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 26, XFront = 34, HeightRear = 2f, HeightFront = 6f, Base = red, Ray = C(0.66f, 0.22f, 0.10f), Edge = white });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 50, XFront = 56, HeightRear = 1f, HeightFront = 6f, Base = red, Ray = C(0.66f, 0.22f, 0.10f), Edge = white });
            fish.PectoralFins.Add(new Pectoral { BaseX = 76, BaseYOffset = -3f, Length = 7, Width = 2f, Slope = 0.25f, Base = red, Ray = C(0.66f, 0.22f, 0.10f), Edge = white });
            fish.Details.Add(Spots(30, 22, 80, 0.15f, 0.55f, C(0.95f, 0.86f, 0.62f), null, 341, 0.1f));
            return fish;
        }

        /// <summary>
        /// Lavaret (corégone) : allongé et argenté, dos gris-bleu, petite tête à museau pointu et bouche petite,
        /// grande queue fourchue, nageoire adipeuse. Le poisson emblématique des lacs savoyards.
        /// </summary>
        private static DetailedFish Whitefish()
        {
            var fin = C(0.62f, 0.66f, 0.70f);
            var fish = new DetailedFish
            {
                Width = 92, Height = 36, CenterY = 17.5f,
                Upper = V((14, 2.5f), (22, 4.5f), (34, 8.5f), (48, 10f), (62, 9f), (74, 6.5f), (82, 4f), (88, 1.5f)),
                Lower = V((14, 2.5f), (22, 4f), (34, 7.5f), (48, 9f), (62, 8.5f), (74, 6f), (82, 3.5f), (88, 1f)),
                Back = C(0.32f, 0.42f, 0.52f), Flank = C(0.80f, 0.84f, 0.86f), Belly = C(0.96f, 0.97f, 0.97f),
                FlankStart = 0.28f,
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 1, XBase = 15, Span = 13f, Base = fin, Ray = C(0.42f, 0.46f, 0.52f), Edge = C(0.30f, 0.34f, 0.40f) },
                EyeX = 82, EyeYOffset = 2f, EyeRadius = 2, Iris = C(0.85f, 0.85f, 0.80f),
                GillX = 75, MouthXStart = 86, MouthXEnd = 88, MouthYOffset = 0f,
                Seed = 35,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 46, XFront = 56, HeightRear = 3f, HeightFront = 8f, Base = fin, Ray = C(0.40f, 0.44f, 0.50f), Edge = C(0.34f, 0.38f, 0.44f) });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 21, XFront = 25, HeightRear = 1.5f, HeightFront = 3f, Shape = FinShape.Rounded, Base = fin, Ray = fin, Edge = C(0.80f, 0.84f, 0.86f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 25, XFront = 34, HeightRear = 2f, HeightFront = 5f, Base = fin, Ray = C(0.48f, 0.52f, 0.56f), Edge = C(0.90f, 0.92f, 0.94f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 48, XFront = 54, HeightRear = 1f, HeightFront = 5f, Base = fin, Ray = C(0.48f, 0.52f, 0.56f), Edge = C(0.90f, 0.92f, 0.94f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 74, BaseYOffset = -3f, Length = 7, Width = 2f, Slope = 0.25f, Base = C(0.86f, 0.88f, 0.90f), Ray = C(0.66f, 0.70f, 0.74f), Edge = C(0.97f, 0.97f, 0.98f) });
            fish.BodyPatterns.Add(Lines(new[] { 0.40f }, C(0.55f, 0.62f, 0.68f), 0.3f, 18, 74));
            return fish;
        }

        /// <summary>
        /// Brochet : très long et fuselé, tête plate en bec de canard (mâchoire inférieure plus longue, gueule fendue
        /// jusque sous l'œil), dorsale et anale très reculées face à face près de la queue, dos vert olive sombre,
        /// flancs semés de taches claires en haricot alignées en rangs, nageoires orangées marbrées de sombre.
        /// </summary>
        private static DetailedFish Pike()
        {
            var orange = C(0.80f, 0.46f, 0.20f);
            var ray = C(0.46f, 0.24f, 0.10f);
            var edge = C(0.94f, 0.66f, 0.36f);
            var olive = C(0.58f, 0.56f, 0.28f);
            var fish = new DetailedFish
            {
                Width = 108, Height = 34, CenterY = 16.5f,
                Upper = V((14, 2.5f), (22, 4f), (32, 6f), (46, 7.5f), (62, 7.5f), (74, 6.5f), (82, 4.4f), (88, 2.8f), (94, 1.9f), (101, 1.4f)),
                Lower = V((14, 2.5f), (22, 4f), (32, 6f), (46, 7.8f), (62, 7.8f), (74, 6.8f), (82, 5f), (88, 3.6f), (94, 2.8f), (100, 2.4f), (104, 1.6f)),
                Back = C(0.18f, 0.26f, 0.13f), Flank = C(0.40f, 0.50f, 0.24f), Belly = C(0.94f, 0.92f, 0.80f),
                FlankStart = 0.24f, BellyStart = 0.72f,
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 11f, Base = orange, Ray = ray, Edge = edge },
                EyeX = 85, EyeYOffset = 2.5f, EyeRadius = 2, Iris = C(0.90f, 0.72f, 0.26f),
                GillX = 76, GillStrength = 0.45f, GillTint = C(0.62f, 0.62f, 0.34f),
                MouthXStart = 86, MouthXEnd = 104, MouthYOffset = -0.5f,
                Seed = 36,
            };
            // Dorsale et anale reculées, face à face, juste devant la queue : le brochet démarre comme une flèche.
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 19, XFront = 32, HeightRear = 6f, HeightFront = 5f, Shape = FinShape.Rounded, Base = olive, Ray = C(0.36f, 0.36f, 0.16f), Edge = C(0.80f, 0.62f, 0.32f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 19, XFront = 30, HeightRear = 5.5f, HeightFront = 4.5f, Shape = FinShape.Rounded, Base = orange, Ray = ray, Edge = edge });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 46, XFront = 54, HeightRear = 2f, HeightFront = 5f, Base = orange, Ray = ray, Edge = edge });
            fish.PectoralFins.Add(new Pectoral { BaseX = 74, BaseYOffset = -4.5f, Length = 7, Width = 2f, Slope = 0.3f, Base = C(0.90f, 0.58f, 0.30f), Ray = C(0.66f, 0.36f, 0.16f), Edge = C(0.98f, 0.76f, 0.48f) });
            fish.Details.Add((f, p) =>
            {
                // Taches claires en haricot, en rangs décalés sur les flancs, plus petites vers la tête.
                var spot = C(0.84f, 0.84f, 0.52f);
                var shade = C(0.30f, 0.38f, 0.18f);
                var random = new System.Random(361);
                for (int row = 0; row < 4; row++)
                {
                    for (int x = 20 + (row % 2) * 3; x < 76; x += 6)
                    {
                        int sx = x + random.Next(-1, 2);
                        float t = 0.14f + (row * 0.15f) + ((float)random.NextDouble() * 0.04f);
                        float top = f.CenterY + f.UpperAt(sx), bottom = f.CenterY - f.LowerAt(sx);
                        int sy = Mathf.RoundToInt(Mathf.Lerp(top, bottom, t));
                        int length = sx > 64 ? 2 : 3;
                        for (int i = 0; i < length; i++)
                        {
                            if (f.InBody(sx + i, sy) && f.BodyT(sx + i, sy) < 0.72f) p[sx + i, sy] = spot;
                        }

                        // Le haricot : un pixel de plus au milieu, une ombre dessous.
                        if (length == 3 && f.InBody(sx + 1, sy + 1)) p[sx + 1, sy + 1] = Color.Lerp(p[sx + 1, sy + 1], spot, 0.7f);
                        if (f.InBody(sx + 1, sy - 1)) p[sx + 1, sy - 1] = Color.Lerp(p[sx + 1, sy - 1], shade, 0.5f);
                    }
                }

                // Marbrures sombres sur la queue, la dorsale et l'anale.
                var mottle = C(0.30f, 0.20f, 0.10f);
                for (int i = 0; i < 40; i++)
                {
                    int x = random.Next(2, 32), y = random.Next(0, f.Height);
                    if (p[x, y].a > 0f && !f.InBody(x, y)) p[x, y] = Color.Lerp(p[x, y], mottle, 0.55f);
                }

                // Bec de canard : petites dents pointues sous la fente, tout le long de la mâchoire inférieure
                // (qui dépasse au bout du museau, voir les profils).
                int mouthY = Mathf.RoundToInt(f.CenterY + f.MouthYOffset);
                for (int x = 89; x <= 103; x += 2)
                {
                    if (f.InBody(x, mouthY - 1)) p[x, mouthY - 1] = C(0.96f, 0.94f, 0.86f);
                }
            });
            return fish;
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
