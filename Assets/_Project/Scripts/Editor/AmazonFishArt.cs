using UnityEngine;
using static FishAndChill.Editor.DetailedFish;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Fiches de dessin détaillé des poissons du fleuve amazonien, d'après l'anatomie et la livrée réelles de chaque
    /// espèce (dessins originaux). Matamata et crevette : <see cref="AmazonCreatureArt"/>.
    /// </summary>
    public static class AmazonFishArt
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
                case "pygocentrus-nattereri": fish = Piranha(); return true;
                case "astronotus-ocellatus": fish = Oscar(); return true;
                case "pterygoplichthys-pardalis": fish = Pleco(); return true;
                case "symphysodon-aequifasciatus": fish = Discus(); return true;
                case "cichla-ocellaris": fish = PeacockBass(); return true;
                case "colossoma-macropomum": fish = Tambaqui(); return true;
                case "osteoglossum-bicirrhosum": fish = Arowana(); return true;
                case "pseudoplatystoma-fasciatum": fish = Surubim(); return true;
                case "hydrolycus-scomberoides": fish = Payara(); return true;
                case "phractocephalus-hemioliopterus": fish = RedtailCatfish(); return true;
                case "electrophorus-electricus": fish = ElectricEel(); return true;
                case "arapaima-gigas": fish = Arapaima(); return true;
                default: fish = null; return false;
            }
        }

        /// <summary>Barbillon fin, de la bouche vers l'extérieur, qui retombe un peu.</summary>
        private static void Barbel(Color[,] p, float x0, float y0, float x1, float y1, float sag, Color color)
        {
            for (int k = 0; k <= 24; k++)
            {
                float t = k / 24f;
                float x = Mathf.Lerp(x0, x1, t);
                float y = Mathf.Lerp(y0, y1, t) - Mathf.Sin(t * Mathf.PI) * sag;
                PixelArt.Set(p, Mathf.RoundToInt(x), Mathf.RoundToInt(y), color);
            }
        }

        /// <summary>Ocelle : tache noire cerclée d'orange (leurre pour les prédateurs).</summary>
        private static void Ocellus(DetailedFish f, Color[,] p, int cx, int cy, float r, Color ring)
        {
            for (int x = cx - 4; x <= cx + 4; x++)
            {
                for (int y = cy - 4; y <= cy + 4; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (d > r + 1.1f || x < 0 || y < 0 || x >= f.Width || y >= f.Height || !PixelArt.IsFilled(p, x, y)) continue;
                    p[x, y] = d <= r ? C(0.08f, 0.06f, 0.05f) : ring;
                }
            }
        }

        /// <summary>
        /// Piranha à ventre rouge : corps haut et comprimé, dos gris-olive pailleté d'argent, gorge et ventre rouge
        /// vif, mâchoire inférieure massive aux dents triangulaires, petite adipeuse, œil rouge.
        /// </summary>
        private static DetailedFish Piranha()
        {
            var fin = C(0.40f, 0.40f, 0.38f);
            var fish = new DetailedFish
            {
                Width = 88, Height = 46, CenterY = 22.5f,
                Upper = V((14, 3f), (22, 6f), (34, 12.5f), (48, 15.5f), (62, 15f), (72, 12f), (80, 8f), (85, 3.5f)),
                Lower = V((14, 3f), (22, 6.5f), (34, 13f), (48, 16f), (62, 16f), (72, 14f), (80, 10f), (85, 5f)),
                Back = C(0.36f, 0.40f, 0.38f), Flank = C(0.70f, 0.72f, 0.70f), Belly = C(0.90f, 0.28f, 0.16f),
                FlankStart = 0.28f, BellyStart = 0.60f,
                Caudal = new Tail { Shape = TailShape.Truncate, XTip = 2, XBase = 15, Span = 12f, Base = fin, Ray = C(0.26f, 0.26f, 0.26f), Edge = C(0.10f, 0.10f, 0.10f) },
                EyeX = 77, EyeYOffset = 4f, EyeRadius = 2, Iris = C(0.92f, 0.20f, 0.14f),
                GillX = 70, GillStrength = 0.45f, MouthXStart = 80, MouthXEnd = 87, MouthYOffset = -3f, MouthSlope = 0.1f,
                Seed = 61,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 40, XFront = 52, HeightRear = 3f, HeightFront = 8f, Base = fin, Ray = C(0.24f, 0.24f, 0.22f), Edge = C(0.20f, 0.20f, 0.18f) });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 20, XFront = 24, HeightRear = 1.5f, HeightFront = 3f, Shape = FinShape.Rounded, Base = fin, Ray = fin, Edge = C(0.30f, 0.30f, 0.28f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 22, XFront = 40, HeightRear = 2f, HeightFront = 6f, Base = C(0.70f, 0.24f, 0.16f), Ray = C(0.46f, 0.14f, 0.08f), Edge = C(0.20f, 0.12f, 0.10f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 68, BaseYOffset = -6f, Length = 6, Width = 2f, Slope = 0.2f, Base = C(0.92f, 0.42f, 0.30f), Ray = C(0.72f, 0.24f, 0.16f), Edge = C(0.98f, 0.62f, 0.48f) });
            fish.Details.Add(Spots(40, 22, 72, 0.15f, 0.55f, C(0.88f, 0.90f, 0.88f), null, 611, 0.2f));
            fish.Details.Add((f, p) =>
            {
                // Dents triangulaires blanches à la mâchoire du bas, qui dépasse.
                int y = Mathf.RoundToInt(f.CenterY - 3f);
                for (int x = 80; x <= 86; x += 2)
                {
                    PixelArt.Set(p, x, y + 1, C(0.98f, 0.98f, 0.94f));
                    PixelArt.Set(p, x, y, C(0.98f, 0.98f, 0.94f));
                }
            });
            return fish;
        }

        /// <summary>
        /// Oscar : ovale et trapu, brun-olive sombre marbré de taches orange, grosse ocelle noire cerclée d'orange à la
        /// base de la queue, nageoires molles arrondies, queue ronde.
        /// </summary>
        private static DetailedFish Oscar()
        {
            var fin = C(0.30f, 0.26f, 0.20f);
            var fish = new DetailedFish
            {
                Width = 88, Height = 46, CenterY = 22.5f,
                Upper = V((15, 4f), (24, 8f), (36, 13f), (50, 15f), (64, 14f), (74, 11f), (82, 7f), (86, 3f)),
                Lower = V((15, 4f), (24, 7.5f), (36, 12f), (50, 13.5f), (64, 12.5f), (74, 10f), (82, 6f), (86, 2.5f)),
                Back = C(0.20f, 0.20f, 0.14f), Flank = C(0.36f, 0.32f, 0.22f), Belly = C(0.56f, 0.44f, 0.30f),
                FlankStart = 0.30f,
                Caudal = new Tail { Shape = TailShape.Round, XTip = 2, XBase = 16, Span = 13f, Base = fin, Ray = C(0.20f, 0.18f, 0.14f), Edge = C(0.14f, 0.12f, 0.10f) },
                EyeX = 78, EyeYOffset = 3.5f, EyeRadius = 2, Iris = C(0.92f, 0.50f, 0.16f),
                GillX = 70, GillStrength = 0.4f, MouthXStart = 82, MouthXEnd = 87, MouthYOffset = -1f,
                Seed = 62,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 20, XFront = 66, HeightRear = 9f, HeightFront = 4f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.18f, 0.16f, 0.12f), Edge = C(0.40f, 0.30f, 0.20f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 20, XFront = 40, HeightRear = 8f, HeightFront = 3f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.18f, 0.16f, 0.12f), Edge = C(0.40f, 0.30f, 0.20f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 68, BaseYOffset = -4f, Length = 7, Width = 2.5f, Slope = 0.2f, Base = C(0.48f, 0.42f, 0.30f), Ray = C(0.30f, 0.26f, 0.18f), Edge = C(0.62f, 0.54f, 0.40f) });
            fish.Details.Add((f, p) =>
            {
                // Marbrures orange vif en taches irrégulières sur les flancs.
                var random = new System.Random(621);
                for (int i = 0; i < 16; i++)
                {
                    int cx = random.Next(26, 70);
                    int cy = Mathf.RoundToInt(f.CenterY) + random.Next(-9, 7);
                    for (int x = cx - 2; x <= cx + 2; x++)
                    {
                        for (int y = cy - 1; y <= cy + 1; y++)
                        {
                            if (f.InBody(x, y) && (x + y + i) % 4 != 0) p[x, y] = Color.Lerp(p[x, y], C(0.92f, 0.46f, 0.16f), 0.8f);
                        }
                    }
                }

                Ocellus(f, p, 19, Mathf.RoundToInt(f.CenterY + 2f), 2f, C(0.96f, 0.52f, 0.14f));
            });
            return fish;
        }

        /// <summary>
        /// Pléco léopard (poisson-chat cuirassé) : corps allongé et aplati couvert de plaques, livrée léopard (taches
        /// brunes sur fond ocre), grande dorsale en voile, bouche-ventouse sous la tête, queue en croissant.
        /// </summary>
        private static DetailedFish Pleco()
        {
            var fin = C(0.56f, 0.46f, 0.30f);
            var fish = new DetailedFish
            {
                Width = 96, Height = 40, CenterY = 15.5f,
                Upper = V((14, 3f), (24, 4f), (38, 6.5f), (54, 8.5f), (68, 9f), (78, 8f), (86, 5.5f), (91, 2.5f)),
                Lower = V((14, 2.5f), (24, 3f), (38, 4.5f), (54, 5.5f), (68, 6f), (78, 6f), (86, 4.5f), (91, 2f)),
                Back = C(0.40f, 0.32f, 0.20f), Flank = C(0.70f, 0.58f, 0.38f), Belly = C(0.84f, 0.76f, 0.58f),
                FlankStart = 0.30f,
                Caudal = new Tail { Shape = TailShape.Lunate, XTip = 2, XBase = 15, Span = 11f, Base = fin, Ray = C(0.34f, 0.26f, 0.16f), Edge = C(0.24f, 0.18f, 0.10f) },
                EyeX = 82, EyeYOffset = 5f, EyeRadius = 1, Iris = C(0.80f, 0.70f, 0.40f),
                GillX = 74, GillStrength = 0.3f, MouthXStart = 86, MouthXEnd = 90, MouthYOffset = -2.5f,
                Seed = 63,
            };
            // Grande dorsale en voile.
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 44, XFront = 70, HeightRear = 6f, HeightFront = 14f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.30f, 0.22f, 0.12f), Edge = C(0.26f, 0.18f, 0.10f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 30, XFront = 38, HeightRear = 2f, HeightFront = 4f, Base = fin, Ray = C(0.30f, 0.22f, 0.12f), Edge = C(0.26f, 0.18f, 0.10f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 74, BaseYOffset = -4f, Length = 9, Width = 2.5f, Slope = 0.35f, Base = fin, Ray = C(0.30f, 0.22f, 0.12f), Edge = C(0.72f, 0.62f, 0.42f) });
            fish.Details.Add(Spots(70, 6, 90, 0f, 1f, C(0.24f, 0.16f, 0.08f), null, 631, 0.6f));
            fish.Details.Add((f, p) =>
            {
                // Taches aussi sur la voile et la queue.
                var random = new System.Random(632);
                for (int i = 0; i < 40; i++)
                {
                    int x = random.Next(2, 72);
                    int y = random.Next(0, f.Height);
                    if (!f.InBody(x, y) && PixelArt.IsFilled(p, x, y) && (x + y) % 2 == 0) p[x, y] = C(0.20f, 0.14f, 0.08f);
                }
            });
            return fish;
        }

        /// <summary>
        /// Discus : disque très haut et plat, brun-rouge parcouru de lignes bleu turquoise ondulées, bandes verticales
        /// sombres, dorsale et anale longues qui suivent le bord du disque, œil rouge.
        /// </summary>
        private static DetailedFish Discus()
        {
            var fin = C(0.52f, 0.30f, 0.18f);
            var fish = new DetailedFish
            {
                Width = 80, Height = 60, CenterY = 29.5f,
                Upper = V((14, 3f), (20, 9f), (30, 18f), (42, 22f), (54, 21f), (64, 16f), (72, 9f), (76, 3f)),
                Lower = V((14, 3f), (20, 9f), (30, 18f), (42, 22f), (54, 21f), (64, 16f), (72, 9f), (76, 3f)),
                Back = C(0.58f, 0.30f, 0.18f), Flank = C(0.70f, 0.40f, 0.22f), Belly = C(0.76f, 0.46f, 0.26f),
                FlankStart = 0.3f, ScaleTexture = false,
                Caudal = new Tail { Shape = TailShape.Truncate, XTip = 3, XBase = 15, Span = 9f, Base = C(0.62f, 0.40f, 0.26f), Ray = C(0.44f, 0.26f, 0.16f), Edge = C(0.30f, 0.66f, 0.80f) },
                EyeX = 68, EyeYOffset = 6f, EyeRadius = 2, Iris = C(0.92f, 0.16f, 0.12f),
                GillX = 62, GillStrength = 0.3f, MouthXStart = 74, MouthXEnd = 77, MouthYOffset = 2f,
                Seed = 64,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 18, XFront = 60, HeightRear = 5f, HeightFront = 3f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.36f, 0.20f, 0.12f), Edge = C(0.30f, 0.70f, 0.84f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 18, XFront = 54, HeightRear = 5f, HeightFront = 3f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.36f, 0.20f, 0.12f), Edge = C(0.30f, 0.70f, 0.84f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 60, BaseYOffset = -2f, Length = 6, Width = 2f, Slope = 0.2f, Base = C(0.86f, 0.62f, 0.44f), Ray = C(0.66f, 0.40f, 0.26f), Edge = C(0.95f, 0.80f, 0.62f) });
            // Lignes turquoise ondulées.
            fish.BodyPatterns.Add((x, y, t, c) =>
            {
                float wave = Mathf.Sin(x * 0.35f) * 1.2f;
                return Mathf.Repeat(y + wave, 4f) < 1.1f ? Color.Lerp(c, C(0.30f, 0.76f, 0.86f), 0.85f) : c;
            });
            fish.BodyPatterns.Add(VerticalBars(new[] { 28, 40, 52, 63 }, 2, C(0.30f, 0.14f, 0.08f), 1f));
            return fish;
        }

        /// <summary>
        /// Tucunaré (peacock bass) : allongé et musclé, vert-jaune doré, trois larges bandes noires verticales, ocelle
        /// noire cerclée d'or sur la queue, deux dorsales, nageoires du bas orange, œil rouge, grande bouche.
        /// </summary>
        private static DetailedFish PeacockBass()
        {
            var orange = C(0.92f, 0.46f, 0.16f);
            var fish = new DetailedFish
            {
                Width = 96, Height = 40, CenterY = 18.5f,
                Upper = V((14, 3f), (22, 5f), (34, 9.5f), (50, 11.5f), (66, 11f), (78, 8.5f), (86, 5f), (92, 2.5f)),
                Lower = V((14, 3f), (22, 5f), (34, 9f), (50, 10.5f), (66, 10f), (78, 8f), (86, 5f), (92, 2f)),
                Back = C(0.36f, 0.44f, 0.18f), Flank = C(0.84f, 0.80f, 0.30f), Belly = C(0.96f, 0.90f, 0.64f),
                FlankStart = 0.30f,
                Caudal = new Tail { Shape = TailShape.Truncate, XTip = 2, XBase = 15, Span = 11f, Base = C(0.62f, 0.62f, 0.30f), Ray = C(0.40f, 0.40f, 0.20f), Edge = C(0.80f, 0.60f, 0.20f) },
                EyeX = 84, EyeYOffset = 3f, EyeRadius = 2, Iris = C(0.90f, 0.18f, 0.12f),
                GillX = 76, GillStrength = 0.45f, MouthXStart = 86, MouthXEnd = 93, MouthYOffset = -1f, MouthSlope = -0.1f,
                Seed = 65,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 52, XFront = 70, HeightRear = 5f, HeightFront = 8f, Shape = FinShape.Rounded, Base = C(0.46f, 0.50f, 0.26f), Ray = C(0.26f, 0.28f, 0.14f), Edge = C(0.70f, 0.70f, 0.40f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 26, XFront = 48, HeightRear = 5f, HeightFront = 6f, Shape = FinShape.Rounded, Base = C(0.46f, 0.50f, 0.26f), Ray = C(0.26f, 0.28f, 0.14f), Edge = C(0.70f, 0.70f, 0.40f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 26, XFront = 38, HeightRear = 3f, HeightFront = 6f, Shape = FinShape.Rounded, Base = orange, Ray = C(0.66f, 0.28f, 0.08f), Edge = C(1f, 0.66f, 0.36f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 62, XFront = 68, HeightRear = 1f, HeightFront = 6f, Base = orange, Ray = C(0.66f, 0.28f, 0.08f), Edge = C(1f, 0.66f, 0.36f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 74, BaseYOffset = -3f, Length = 7, Width = 2.5f, Slope = 0.2f, Base = C(0.90f, 0.82f, 0.50f), Ray = C(0.70f, 0.60f, 0.30f), Edge = C(0.98f, 0.92f, 0.66f) });
            fish.BodyPatterns.Add(VerticalBars(new[] { 34, 50, 64 }, 4, C(0.10f, 0.10f, 0.06f), 0.72f));
            fish.Details.Add((f, p) => Ocellus(f, p, 11, Mathf.RoundToInt(f.CenterY + 1f), 1.6f, C(0.98f, 0.80f, 0.20f)));
            return fish;
        }

        /// <summary>
        /// Tambaqui : très haut et lourd, dos presque noir, flancs olive-doré, poitrine et ventre sombres, tête ronde
        /// aux dents de broyeur de graines, petite adipeuse, queue courte.
        /// </summary>
        private static DetailedFish Tambaqui()
        {
            var dark = C(0.14f, 0.14f, 0.12f);
            var fish = new DetailedFish
            {
                Width = 92, Height = 50, CenterY = 24.5f,
                Upper = V((14, 4f), (22, 7f), (34, 14f), (50, 18f), (64, 17f), (74, 14f), (82, 9f), (88, 4f)),
                Lower = V((14, 4f), (22, 7f), (34, 13f), (50, 17f), (64, 17f), (74, 14f), (82, 9f), (88, 3f)),
                Back = C(0.16f, 0.16f, 0.12f), Flank = C(0.62f, 0.58f, 0.30f), Belly = C(0.22f, 0.20f, 0.16f),
                FlankStart = 0.32f, BellyStart = 0.72f,
                Caudal = new Tail { Shape = TailShape.Truncate, XTip = 2, XBase = 15, Span = 13f, Base = dark, Ray = C(0.08f, 0.08f, 0.06f), Edge = C(0.30f, 0.30f, 0.26f) },
                EyeX = 80, EyeYOffset = 4f, EyeRadius = 2, Iris = C(0.80f, 0.70f, 0.40f),
                GillX = 72, GillStrength = 0.5f, MouthXStart = 85, MouthXEnd = 89, MouthYOffset = -2f,
                Seed = 66,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 42, XFront = 54, HeightRear = 3f, HeightFront = 9f, Base = dark, Ray = C(0.08f, 0.08f, 0.06f), Edge = C(0.30f, 0.30f, 0.26f) });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 21, XFront = 25, HeightRear = 1.5f, HeightFront = 3f, Shape = FinShape.Rounded, Base = dark, Ray = dark, Edge = C(0.30f, 0.30f, 0.26f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 22, XFront = 36, HeightRear = 3f, HeightFront = 7f, Base = dark, Ray = C(0.08f, 0.08f, 0.06f), Edge = C(0.30f, 0.30f, 0.26f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 70, BaseYOffset = -8f, Length = 7, Width = 2.5f, Slope = 0.2f, Base = C(0.26f, 0.24f, 0.20f), Ray = C(0.12f, 0.12f, 0.10f), Edge = C(0.40f, 0.38f, 0.32f) });
            return fish;
        }

        /// <summary>
        /// Arowana argenté : long ruban à grandes écailles nacrées, bouche tournée vers le haut avec deux barbillons au
        /// menton, dorsale et anale très en arrière qui rejoignent presque la petite queue pointue.
        /// </summary>
        private static DetailedFish Arowana()
        {
            var fin = C(0.70f, 0.72f, 0.70f);
            var fish = new DetailedFish
            {
                Width = 112, Height = 32, CenterY = 15.5f,
                Upper = V((12, 3f), (20, 5f), (34, 7.5f), (54, 8.5f), (74, 8f), (90, 6.5f), (100, 4.5f), (106, 3f)),
                Lower = V((12, 3f), (20, 6f), (34, 8.5f), (54, 9f), (74, 8f), (90, 6.5f), (100, 4f), (106, 1f)),
                Back = C(0.40f, 0.46f, 0.42f), Flank = C(0.82f, 0.84f, 0.82f), Belly = C(0.94f, 0.94f, 0.92f),
                FlankStart = 0.25f,
                Caudal = new Tail { Shape = TailShape.Round, XTip = 2, XBase = 13, Span = 7f, Base = fin, Ray = C(0.46f, 0.48f, 0.46f), Edge = C(0.40f, 0.44f, 0.60f) },
                EyeX = 99, EyeYOffset = 1.5f, EyeRadius = 2, Iris = C(0.86f, 0.80f, 0.60f),
                GillX = 90, GillStrength = 0.5f, MouthXStart = 100, MouthXEnd = 107, MouthYOffset = 2.5f, MouthSlope = 0.45f,
                Seed = 67,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 14, XFront = 38, HeightRear = 6f, HeightFront = 3f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.46f, 0.48f, 0.46f), Edge = C(0.40f, 0.44f, 0.60f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 14, XFront = 50, HeightRear = 6f, HeightFront = 3f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.46f, 0.48f, 0.46f), Edge = C(0.40f, 0.44f, 0.60f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 88, BaseYOffset = -5f, Length = 9, Width = 2f, Slope = 0.3f, Base = C(0.86f, 0.86f, 0.84f), Ray = C(0.62f, 0.64f, 0.62f), Edge = C(0.96f, 0.96f, 0.94f) });
            fish.BodyPatterns.Add((x, y, t, c) =>
            {
                // Grandes écailles en losanges nacrés.
                bool edge = Mathf.Repeat(x + y * 0.9f, 7f) < 1f || Mathf.Repeat(x - y * 0.9f, 7f) < 1f;
                return edge && x < 88 ? Color.Lerp(c, C(0.56f, 0.66f, 0.78f), 0.35f) : c;
            });
            fish.Details.Add((f, p) =>
            {
                int y = Mathf.RoundToInt(f.CenterY + 2f);
                Barbel(p, 104f, y, 112f, y + 3f, -0.5f, C(0.84f, 0.84f, 0.80f));
                Barbel(p, 104f, y - 1, 111f, y + 1f, -0.5f, C(0.62f, 0.64f, 0.60f));
            });
            return fish;
        }

        /// <summary>
        /// Surubim (poisson-chat tigre) : long et fuselé, tête aplatie et large, dos gris rayé de bandes noires
        /// verticales, ventre blanc, trois paires de longs barbillons, queue fourchue tachetée.
        /// </summary>
        private static DetailedFish Surubim()
        {
            var fin = C(0.46f, 0.46f, 0.44f);
            var fish = new DetailedFish
            {
                Width = 112, Height = 36, CenterY = 16.5f,
                Upper = V((14, 3f), (24, 4.5f), (40, 7.5f), (60, 9f), (78, 9f), (92, 8f), (102, 5.5f), (108, 3f)),
                Lower = V((14, 3f), (24, 4f), (40, 6.5f), (60, 7.5f), (78, 7.5f), (92, 6.5f), (102, 4.5f), (108, 2.5f)),
                Back = C(0.36f, 0.38f, 0.36f), Flank = C(0.60f, 0.62f, 0.58f), Belly = C(0.96f, 0.96f, 0.92f),
                FlankStart = 0.30f, BellyStart = 0.62f, ScaleTexture = false,
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 12f, Base = fin, Ray = C(0.28f, 0.28f, 0.26f), Edge = C(0.20f, 0.20f, 0.18f) },
                EyeX = 100, EyeYOffset = 4.5f, EyeRadius = 1, Iris = C(0.80f, 0.72f, 0.44f),
                GillX = 88, GillStrength = 0.4f, MouthXStart = 102, MouthXEnd = 109, MouthYOffset = -0.5f,
                Seed = 68,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 70, XFront = 80, HeightRear = 3f, HeightFront = 8f, Base = fin, Ray = C(0.28f, 0.28f, 0.26f), Edge = C(0.20f, 0.20f, 0.18f), Spiny = true });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 22, XFront = 34, HeightRear = 2f, HeightFront = 3f, Shape = FinShape.Rounded, Base = fin, Ray = fin, Edge = C(0.24f, 0.24f, 0.22f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 26, XFront = 38, HeightRear = 2f, HeightFront = 5f, Base = C(0.70f, 0.70f, 0.66f), Ray = C(0.44f, 0.44f, 0.42f), Edge = C(0.86f, 0.86f, 0.82f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 86, BaseYOffset = -4f, Length = 8, Width = 2f, Slope = 0.3f, Base = C(0.70f, 0.70f, 0.66f), Ray = C(0.40f, 0.40f, 0.38f), Edge = C(0.86f, 0.86f, 0.82f) });
            fish.BodyPatterns.Add(VerticalBars(new[] { 26, 36, 46, 56, 66, 76, 86, 94 }, 2, C(0.08f, 0.08f, 0.08f), 0.62f, 0.15f));
            fish.Details.Add(Spots(18, 2, 16, 0f, 1f, C(0.10f, 0.10f, 0.10f), null, 681, 0.2f));
            fish.Details.Add((f, p) =>
            {
                int y = Mathf.RoundToInt(f.CenterY);
                Color barbel = C(0.86f, 0.84f, 0.78f);
                Barbel(p, 106f, y + 1, 84f, y - 10f, -2f, barbel);   // longs barbillons du maxillaire
                Barbel(p, 106f, y - 2, 96f, y - 13f, -1f, C(0.70f, 0.68f, 0.62f));
                Barbel(p, 104f, y - 3, 98f, y - 10f, 0f, barbel);
            });
            return fish;
        }

        /// <summary>
        /// Payara (poisson-vampire) : torpille argentée, grand œil, tache sombre derrière les ouïes, et deux énormes
        /// crocs de la mâchoire du bas qui remontent devant le museau.
        /// </summary>
        private static DetailedFish Payara()
        {
            var fin = C(0.70f, 0.72f, 0.74f);
            var fish = new DetailedFish
            {
                Width = 104, Height = 36, CenterY = 16.5f,
                Upper = V((14, 3f), (24, 5f), (40, 8.5f), (58, 10f), (74, 9.5f), (86, 7.5f), (94, 5f), (99, 2.5f)),
                Lower = V((14, 3f), (24, 5f), (40, 8.5f), (58, 9.5f), (74, 9f), (86, 7.5f), (94, 5.5f), (99, 3.5f)),
                Back = C(0.42f, 0.48f, 0.54f), Flank = C(0.84f, 0.86f, 0.88f), Belly = C(0.96f, 0.96f, 0.96f),
                FlankStart = 0.26f,
                Caudal = new Tail { Shape = TailShape.Forked, XTip = 2, XBase = 15, Span = 13f, Base = fin, Ray = C(0.48f, 0.50f, 0.52f), Edge = C(0.20f, 0.22f, 0.26f) },
                EyeX = 90, EyeYOffset = 2.5f, EyeRadius = 3, Iris = C(0.92f, 0.82f, 0.40f),
                GillX = 82, GillStrength = 0.5f, MouthXStart = 92, MouthXEnd = 100, MouthYOffset = -2f, MouthSlope = 0.15f,
                Seed = 69,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 48, XFront = 58, HeightRear = 3f, HeightFront = 9f, Base = fin, Ray = C(0.48f, 0.50f, 0.52f), Edge = C(0.34f, 0.36f, 0.40f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 22, XFront = 44, HeightRear = 2f, HeightFront = 6f, Base = fin, Ray = C(0.48f, 0.50f, 0.52f), Edge = C(0.34f, 0.36f, 0.40f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 80, BaseYOffset = -6f, Length = 11, Width = 2f, Slope = 0.3f, Base = C(0.90f, 0.90f, 0.92f), Ray = C(0.66f, 0.68f, 0.70f), Edge = C(0.98f, 0.98f, 0.98f) });
            fish.Details.Add((f, p) =>
            {
                // Tache sombre derrière les ouïes.
                int cy = Mathf.RoundToInt(f.CenterY + 2f);
                for (int x = 74; x <= 78; x++)
                {
                    for (int y = cy - 2; y <= cy + 2; y++)
                    {
                        if (f.InBody(x, y)) p[x, y] = Color.Lerp(p[x, y], C(0.12f, 0.14f, 0.18f), 0.75f);
                    }
                }

                // Deux crocs : de la mâchoire du bas, ils traversent le museau et dépassent au-dessus.
                int my = Mathf.RoundToInt(f.CenterY - 2f);
                int top = Mathf.RoundToInt(f.CenterY + f.UpperAt(96f)) + 3;
                for (int y = my; y <= top; y++)
                {
                    PixelArt.Set(p, 96, y, C(0.98f, 0.98f, 0.92f));
                    PixelArt.Set(p, 97, y, y > top - 2 ? C(0.98f, 0.98f, 0.92f) : C(0.80f, 0.80f, 0.74f));
                }

                PixelArt.Set(p, 96, top + 1, C(0.98f, 0.98f, 0.92f));
                PixelArt.Set(p, 95, top + 1, C(0.10f, 0.10f, 0.12f));
                PixelArt.Set(p, 98, top, C(0.10f, 0.10f, 0.12f));
            });
            return fish;
        }

        /// <summary>
        /// Pirarara (poisson-chat à queue rouge) : énorme tête large, dos gris très sombre, flancs et ventre blanc-jaune
        /// bien séparés, queue et haut de la dorsale rouge orangé vif, longs barbillons.
        /// </summary>
        private static DetailedFish RedtailCatfish()
        {
            var red = C(0.94f, 0.36f, 0.14f);
            var dark = C(0.18f, 0.18f, 0.20f);
            var fish = new DetailedFish
            {
                Width = 108, Height = 42, CenterY = 19.5f,
                Upper = V((14, 3f), (24, 5f), (40, 9f), (60, 12f), (78, 13f), (90, 12f), (100, 8f), (105, 4f)),
                Lower = V((14, 3f), (24, 4.5f), (40, 8f), (60, 10f), (78, 11f), (90, 10f), (100, 7f), (105, 3f)),
                Back = dark, Flank = C(0.28f, 0.28f, 0.30f), Belly = C(0.96f, 0.92f, 0.72f),
                FlankStart = 0.30f, BellyStart = 0.52f, ScaleTexture = false,
                Caudal = new Tail { Shape = TailShape.Truncate, XTip = 2, XBase = 15, Span = 13f, Base = red, Ray = C(0.70f, 0.20f, 0.08f), Edge = C(1f, 0.58f, 0.30f) },
                EyeX = 97, EyeYOffset = 6f, EyeRadius = 1, Iris = C(0.80f, 0.70f, 0.44f),
                GillX = 84, GillStrength = 0.3f, MouthXStart = 99, MouthXEnd = 106, MouthYOffset = -1f,
                Seed = 70,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 66, XFront = 78, HeightRear = 4f, HeightFront = 10f, Base = dark, Ray = C(0.10f, 0.10f, 0.12f), Edge = red });
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 22, XFront = 34, HeightRear = 2f, HeightFront = 4f, Shape = FinShape.Rounded, Base = dark, Ray = dark, Edge = C(0.40f, 0.20f, 0.12f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 24, XFront = 38, HeightRear = 3f, HeightFront = 6f, Shape = FinShape.Rounded, Base = C(0.30f, 0.30f, 0.32f), Ray = dark, Edge = red });
            fish.PectoralFins.Add(new Pectoral { BaseX = 84, BaseYOffset = -5f, Length = 10, Width = 2.5f, Slope = 0.3f, Base = C(0.30f, 0.30f, 0.32f), Ray = C(0.14f, 0.14f, 0.16f), Edge = C(0.86f, 0.40f, 0.20f) });
            fish.Details.Add((f, p) =>
            {
                int y = Mathf.RoundToInt(f.CenterY);
                Color barbel = C(0.84f, 0.80f, 0.64f);
                Barbel(p, 103f, y + 1, 82f, y - 6f, -3f, barbel);
                Barbel(p, 103f, y - 3, 92f, y - 13f, -1f, barbel);
                Barbel(p, 101f, y - 4, 96f, y - 12f, 0f, C(0.70f, 0.66f, 0.52f));
            });
            return fish;
        }

        /// <summary>
        /// Gymnote électrique (anguille électrique) : très long corps cylindrique sans dorsale, brun-gris sombre, gorge
        /// orangée, une seule longue nageoire anale qui ondule tout le long du ventre jusqu'à la pointe.
        /// </summary>
        private static DetailedFish ElectricEel()
        {
            var fin = C(0.34f, 0.28f, 0.22f);
            var fish = new DetailedFish
            {
                Width = 120, Height = 26, CenterY = 12.5f,
                Upper = V((2, 0.5f), (12, 2f), (30, 4f), (54, 5.5f), (78, 6.5f), (98, 6.5f), (110, 5f), (116, 2.5f)),
                Lower = V((2, 0.5f), (12, 2f), (30, 4f), (54, 5.5f), (78, 6.5f), (98, 6f), (110, 4.5f), (116, 2f)),
                Back = C(0.22f, 0.20f, 0.16f), Flank = C(0.34f, 0.30f, 0.24f), Belly = C(0.86f, 0.52f, 0.24f),
                FlankStart = 0.35f, BellyStart = 0.80f, ScaleTexture = false,
                EyeX = 111, EyeYOffset = 2.5f, EyeRadius = 1, Iris = C(0.60f, 0.50f, 0.30f),
                GillX = 104, GillStrength = 0.3f, MouthXStart = 113, MouthXEnd = 117, MouthYOffset = -1f,
                Seed = 71,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 3, XFront = 96, HeightRear = 2f, HeightFront = 4f, Shape = FinShape.Rounded, Base = fin, Ray = C(0.20f, 0.16f, 0.12f), Edge = C(0.56f, 0.42f, 0.28f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 102, BaseYOffset = -1f, Length = 4, Width = 1.5f, Slope = 0.2f, Base = C(0.50f, 0.40f, 0.30f), Ray = fin, Edge = C(0.66f, 0.54f, 0.40f) });
            // Petites taches claires (organes sensoriels) en rangées le long du dos.
            fish.Details.Add(Spots(30, 10, 100, 0.05f, 0.45f, C(0.56f, 0.50f, 0.40f), null, 711, 0f));
            return fish;
        }

        /// <summary>
        /// Arapaïma : géant au corps long et cylindrique, tête plate osseuse gris-vert, grandes écailles de plus en
        /// plus bordées de rouge vers l'arrière, dorsale et anale molles reculées près de la queue ronde rouge.
        /// </summary>
        private static DetailedFish Arapaima()
        {
            var red = C(0.80f, 0.20f, 0.14f);
            var fish = new DetailedFish
            {
                Width = 120, Height = 38, CenterY = 18.5f,
                Upper = V((14, 4f), (24, 6.5f), (40, 9f), (60, 10.5f), (80, 10.5f), (98, 9f), (108, 6f), (114, 3f)),
                Lower = V((14, 4f), (24, 6.5f), (40, 9f), (60, 10f), (80, 10f), (98, 8.5f), (108, 5.5f), (114, 2.5f)),
                Back = C(0.26f, 0.32f, 0.28f), Flank = C(0.50f, 0.56f, 0.48f), Belly = C(0.82f, 0.80f, 0.70f),
                FlankStart = 0.30f,
                Caudal = new Tail { Shape = TailShape.Round, XTip = 2, XBase = 16, Span = 12f, Base = C(0.46f, 0.20f, 0.16f), Ray = C(0.30f, 0.10f, 0.08f), Edge = red },
                EyeX = 107, EyeYOffset = 3f, EyeRadius = 2, Iris = C(0.80f, 0.70f, 0.40f),
                GillX = 98, GillStrength = 0.55f, MouthXStart = 108, MouthXEnd = 115, MouthYOffset = 0f, MouthSlope = 0.1f,
                Seed = 72,
            };
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 16, XFront = 34, HeightRear = 7f, HeightFront = 4f, Shape = FinShape.Rounded, Base = C(0.40f, 0.24f, 0.20f), Ray = C(0.26f, 0.12f, 0.10f), Edge = red });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 16, XFront = 34, HeightRear = 7f, HeightFront = 4f, Shape = FinShape.Rounded, Base = C(0.40f, 0.24f, 0.20f), Ray = C(0.26f, 0.12f, 0.10f), Edge = red });
            fish.PectoralFins.Add(new Pectoral { BaseX = 96, BaseYOffset = -6f, Length = 8, Width = 2.5f, Slope = 0.3f, Base = C(0.56f, 0.56f, 0.48f), Ray = C(0.36f, 0.36f, 0.30f), Edge = C(0.74f, 0.72f, 0.62f) });
            fish.BodyPatterns.Add((x, y, t, c) =>
            {
                // Bord des grandes écailles, de plus en plus rouge vers la queue.
                bool edge = Mathf.Repeat(x + (y % 6 < 3 ? 0 : 3), 6f) < 1f || y % 6 == 0;
                float redness = Mathf.InverseLerp(80f, 16f, x);
                if (x > 96) return c; // la tête osseuse n'a pas d'écailles
                return edge ? Color.Lerp(Color.Lerp(c, C(0.20f, 0.24f, 0.20f), 0.4f), red, redness) : Color.Lerp(c, red, redness * 0.25f);
            });
            return fish;
        }
    }
}
