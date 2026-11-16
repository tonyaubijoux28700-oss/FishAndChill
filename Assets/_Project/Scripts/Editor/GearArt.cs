using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Icônes du matériel (32×32, pixel art provisoire), une par objet : 4 cannes, 4 hameçons, 4 appâts, 4 sacs.
    /// Formes rondes éclairées en haut à gauche et contour sélectif, comme les créatures.
    /// </summary>
    public static class GearArt
    {
        private const int Size = 32;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static Color[,] Canvas() => new Color[Size, Size];

        private static Color[,] Finish(Color[,] p, Color outline)
        {
            PixelArt.SelectiveOutline(p, outline, 0.55f);
            return p;
        }

        private static void Line(Color[,] p, float x0, float y0, float x1, float y1, float radius, Color color)
        {
            CreatureArt.Limb(p, x0, y0, x1, y1, radius, color);
        }

        // ---------- Cannes ----------

        /// <summary>Canne en diagonale, poignée en bas à gauche, moulinet, fil qui pend du scion.</summary>
        public static Color[,] Rod(int level)
        {
            var p = Canvas();
            Color blank;
            Color wrap;
            Color grip;
            switch (level)
            {
                case 0: blank = C(0.86f, 0.74f, 0.44f); wrap = C(0.56f, 0.40f, 0.20f); grip = C(0.62f, 0.44f, 0.24f); break;  // bambou
                case 1: blank = C(0.96f, 0.82f, 0.30f); wrap = C(0.20f, 0.46f, 0.80f); grip = C(0.30f, 0.30f, 0.32f); break;  // fibre de verre
                case 2: blank = C(0.16f, 0.16f, 0.18f); wrap = C(0.86f, 0.22f, 0.20f); grip = C(0.52f, 0.36f, 0.22f); break;  // carbone
                default: blank = C(0.14f, 0.26f, 0.56f); wrap = C(0.98f, 0.80f, 0.30f); grip = C(0.82f, 0.62f, 0.32f); break; // compétition
            }

            Line(p, 4f, 4f, 11f, 11f, 1.8f, grip);                       // poignée
            Line(p, 11f, 11f, 28f, 28f, 0.9f, blank);                    // brin
            Line(p, 21f, 21f, 29f, 29f, 0.6f, DetailedFish.Shade(blank, 1.1f));
            for (int k = 0; k < 4; k++)
            {
                float t = 13f + k * 4f;                                  // anneaux et ligatures
                PixelArt.Set(p, Mathf.RoundToInt(t) + 1, Mathf.RoundToInt(t) - 1, wrap);
            }

            if (level == 0)
            {
                for (int k = 0; k < 3; k++) PixelArt.Set(p, 15 + k * 4, 15 + k * 4, DetailedFish.Shade(blank, 0.7f)); // nœuds du bambou
            }

            // Moulinet sous la poignée.
            CreatureArt.Blob(p, 9.5f, 6f, 2.6f, 2.2f, level == 0 ? C(0.50f, 0.50f, 0.54f) : C(0.70f, 0.72f, 0.76f));
            PixelArt.Set(p, 9, 6, C(0.30f, 0.30f, 0.34f));
            PixelArt.Set(p, 12, 4, C(0.30f, 0.30f, 0.34f));

            // Fil qui pend du scion.
            for (int y = 18; y <= 28; y++) PixelArt.Set(p, 29 + (y < 22 ? 0 : 1), y, C(0.90f, 0.94f, 0.96f, 0.9f));
            PixelArt.Set(p, 30, 17, C(0.95f, 0.30f, 0.25f)); // petit bouchon rouge
            return Finish(p, C(0.12f, 0.09f, 0.08f));
        }

        public const int RodWidth = 96;
        public const int RodHeight = 160;

        /// <summary>Bout du fil de la grande canne (là où pendent l'hameçon et l'appât), en pixels du dessin.</summary>
        public static readonly Vector2Int RodLineEnd = new Vector2Int(86, 62);

        /// <summary>
        /// Grande canne pour la vitrine de l'inventaire, 96×160 : poignée en bas à gauche, moulinet, brin qui monte en
        /// se courbant légèrement vers la droite, anneaux, scion, et le fil qui pend du scion jusqu'à
        /// <see cref="RodLineEnd"/> (l'hameçon et l'appât y sont posés en jeu).
        /// </summary>
        public static Color[,] RodLarge(int level)
        {
            var p = new Color[RodWidth, RodHeight];
            Color blank;
            Color wrap;
            Color grip;
            Color reel;
            switch (level)
            {
                case 0: blank = C(0.86f, 0.74f, 0.44f); wrap = C(0.56f, 0.40f, 0.20f); grip = C(0.62f, 0.44f, 0.24f); reel = C(0.52f, 0.52f, 0.56f); break;
                case 1: blank = C(0.96f, 0.82f, 0.30f); wrap = C(0.20f, 0.46f, 0.80f); grip = C(0.30f, 0.30f, 0.32f); reel = C(0.70f, 0.72f, 0.76f); break;
                case 2: blank = C(0.18f, 0.18f, 0.20f); wrap = C(0.86f, 0.22f, 0.20f); grip = C(0.52f, 0.36f, 0.22f); reel = C(0.78f, 0.80f, 0.84f); break;
                default: blank = C(0.14f, 0.26f, 0.56f); wrap = C(0.98f, 0.80f, 0.30f); grip = C(0.82f, 0.62f, 0.32f); reel = C(0.96f, 0.80f, 0.36f); break;
            }

            // Le brin : une courbe de la poignée (8, 8) au scion (84, 152), qui s'affine vers le bout.
            Vector2 Point(float t) => new Vector2(8f + t * 70f + t * t * 6f, 8f + t * 144f);
            const int steps = 120;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 a = Point(t);
                float radius = t < 0.22f ? 3.2f : Mathf.Lerp(1.7f, 0.6f, (t - 0.22f) / 0.78f);
                Color c = t < 0.22f ? grip : blank;
                if (t < 0.22f && i % 4 == 0) c = DetailedFish.Shade(grip, 0.8f);                   // grain de la poignée
                if (level == 0 && t > 0.22f && i % 18 == 0) c = DetailedFish.Shade(blank, 0.7f);   // nœuds du bambou
                CreatureArt.Blob(p, a.x, a.y, radius, radius, c);
                CreatureArt.Blob(p, a.x - radius * 0.35f, a.y + radius * 0.2f, radius * 0.4f, radius * 0.4f, DetailedFish.Shade(c, 1.25f)); // reflet
            }

            // Talon, ligatures et anneaux le long du brin.
            CreatureArt.Blob(p, Point(0f).x, Point(0f).y, 3.6f, 3.6f, DetailedFish.Shade(grip, 0.7f));
            foreach (float t in new[] { 0.22f, 0.36f, 0.5f, 0.64f, 0.78f, 0.9f })
            {
                Vector2 a = Point(t);
                CreatureArt.Blob(p, a.x, a.y, 2.2f, 1.2f, wrap);
                CreatureArt.Blob(p, a.x + 2.6f, a.y - 1f, 1.3f, 1.3f, C(0.78f, 0.80f, 0.84f));    // anneau
                PixelArt.Set(p, Mathf.RoundToInt(a.x + 2.6f), Mathf.RoundToInt(a.y - 1f), C(0f, 0f, 0f, 0f));
            }

            // Moulinet sous la poignée, avec sa manivelle.
            Vector2 seat = Point(0.17f);
            CreatureArt.Blob(p, seat.x + 7f, seat.y - 3f, 6f, 5.5f, reel);
            CreatureArt.Blob(p, seat.x + 7f, seat.y - 3f, 3.2f, 3f, DetailedFish.Shade(reel, 0.75f));
            CreatureArt.Blob(p, seat.x + 5f, seat.y - 1f, 1.8f, 1.4f, DetailedFish.Shade(reel, 1.3f));
            CreatureArt.Limb(p, seat.x + 7f, seat.y - 3f, seat.x + 13f, seat.y - 8f, 0.7f, C(0.30f, 0.30f, 0.34f));
            CreatureArt.Blob(p, seat.x + 13.5f, seat.y - 8.5f, 1.6f, 1.6f, C(0.86f, 0.30f, 0.24f));
            CreatureArt.Limb(p, seat.x + 2f, seat.y, seat.x + 7f, seat.y - 3f, 0.8f, DetailedFish.Shade(reel, 0.6f)); // pied du moulinet

            PixelArt.SelectiveOutline(p, C(0.12f, 0.09f, 0.08f), 0.55f);

            // Le fil : du scion, il retombe droit jusqu'à l'hameçon (dessiné après le contour, il reste fin et clair).
            Vector2 tip = Point(1f);
            for (int y = Mathf.RoundToInt(tip.y) - 1; y >= RodLineEnd.y; y--)
            {
                float sway = Mathf.Sin((tip.y - y) * 0.05f) * 1.5f;
                int x = Mathf.RoundToInt(Mathf.Lerp(tip.x + 1f, RodLineEnd.x, (tip.y - y) / (tip.y - RodLineEnd.y)) + sway);
                PixelArt.Set(p, x, y, C(0.92f, 0.95f, 0.98f, 0.95f));
            }

            return p;
        }

        // ---------- Hameçons ----------

        /// <summary>Hameçon en J (renforcé : plus épais ; triple : trois pointes ; forgé : acier sombre à œillet doré).</summary>
        public static Color[,] Hook(int level)
        {
            var p = Canvas();
            Color metal = level == 3 ? C(0.36f, 0.40f, 0.48f) : C(0.78f, 0.82f, 0.88f);
            Color eye = level == 3 ? C(0.96f, 0.78f, 0.30f) : metal;
            float thick = level == 0 ? 0.8f : 1.2f;

            void JHook(float cx, float flip)
            {
                Line(p, cx, 27f, cx, 12f, thick, metal);                                     // hampe
                for (int k = 0; k <= 12; k++)                                                // courbure
                {
                    float a = Mathf.PI * k / 12f;
                    float x = cx + flip * (4.5f - Mathf.Cos(a) * 4.5f);
                    float y = 12f - Mathf.Sin(a) * 6f;
                    CreatureArt.Blob(p, x, y, thick, thick, metal);
                }

                Line(p, cx + flip * 9f, 12f, cx + flip * 9f, 16f, thick * 0.9f, metal);   // pointe
                Line(p, cx + flip * 9f, 16f, cx + flip * 7.5f, 14.5f, 0.6f, metal);       // ardillon
            }

            if (level == 2)
            {
                JHook(12f, 1f);
                JHook(20f, -1f);
                Line(p, 16f, 27f, 16f, 10f, thick, metal);
                CreatureArt.Blob(p, 16f, 7f, 1.6f, 1.6f, metal);
            }
            else
            {
                JHook(11f, 1f);
            }

            float eyeX = level == 2 ? 16f : 11f;
            CreatureArt.Blob(p, eyeX, 28.5f, 2f, 2f, eye);
            PixelArt.Set(p, Mathf.RoundToInt(eyeX), 28, C(0f, 0f, 0f, 0f));
            PixelArt.Set(p, Mathf.RoundToInt(eyeX) - 1, 20, C(1f, 1f, 1f));                  // reflet
            return Finish(p, C(0.10f, 0.12f, 0.16f));
        }

        // ---------- Appâts ----------

        public static Color[,] Bait(int level)
        {
            var p = Canvas();
            switch (level)
            {
                case 0:
                    // Mie de pain : tranche à croûte dorée, mie claire et ses trous.
                    CreatureArt.Blob(p, 16f, 14f, 11f, 9f, C(0.80f, 0.52f, 0.24f));
                    CreatureArt.Blob(p, 11f, 21f, 6f, 5f, C(0.80f, 0.52f, 0.24f));
                    CreatureArt.Blob(p, 21f, 21f, 6f, 5f, C(0.80f, 0.52f, 0.24f));
                    CreatureArt.Blob(p, 16f, 14f, 9f, 7f, C(0.98f, 0.92f, 0.76f));
                    CreatureArt.Blob(p, 11f, 20f, 4.5f, 3.5f, C(0.98f, 0.92f, 0.76f));
                    CreatureArt.Blob(p, 21f, 20f, 4.5f, 3.5f, C(0.98f, 0.92f, 0.76f));
                    foreach (Vector2Int h in new[] { new Vector2Int(13, 12), new Vector2Int(19, 15), new Vector2Int(16, 18), new Vector2Int(11, 19), new Vector2Int(20, 10) })
                    {
                        PixelArt.Set(p, h.x, h.y, C(0.86f, 0.76f, 0.56f));
                    }

                    return Finish(p, C(0.36f, 0.20f, 0.08f));

                case 1:
                    // Crevette : corps courbé rose en anneaux, queue en éventail, antennes.
                    for (int k = 0; k < 7; k++)
                    {
                        float a = Mathf.PI * (0.15f + k * 0.12f);
                        CreatureArt.Blob(p, 16f + Mathf.Cos(a) * 8f, 13f + Mathf.Sin(a) * 8f, 3.4f - k * 0.25f, 3.4f - k * 0.25f,
                            k % 2 == 0 ? C(0.98f, 0.56f, 0.48f) : C(0.94f, 0.46f, 0.40f));
                    }

                    CreatureArt.Blob(p, 26f, 10f, 2.4f, 3.2f, C(0.98f, 0.62f, 0.52f));      // queue
                    CreatureArt.Blob(p, 8f, 14f, 3f, 2.6f, C(0.98f, 0.60f, 0.50f));       // tête
                    PixelArt.Set(p, 7, 15, C(0.08f, 0.08f, 0.08f));
                    Line(p, 6f, 15f, 2f, 26f, 0.35f, C(0.90f, 0.40f, 0.34f));             // antennes
                    Line(p, 7f, 16f, 6f, 28f, 0.35f, C(0.90f, 0.40f, 0.34f));
                    return Finish(p, C(0.46f, 0.14f, 0.12f));

                case 2:
                    // Leurre brillant : cuiller argentée tournante, perle rouge, hameçon triple.
                    Line(p, 16f, 30f, 16f, 22f, 0.5f, C(0.80f, 0.84f, 0.88f));
                    CreatureArt.Blob(p, 16f, 21f, 1.4f, 1.4f, C(0.92f, 0.20f, 0.20f));
                    CreatureArt.Blob(p, 16f, 14f, 5f, 7.5f, C(0.82f, 0.86f, 0.92f), 0.35f);
                    CreatureArt.Blob(p, 14.5f, 16f, 2f, 3.5f, C(1f, 1f, 1f), 0.35f);      // reflet
                    Line(p, 16f, 7f, 16f, 4f, 0.5f, C(0.62f, 0.66f, 0.72f));
                    Line(p, 16f, 4f, 13f, 1.5f, 0.5f, C(0.62f, 0.66f, 0.72f));
                    Line(p, 16f, 4f, 19f, 1.5f, 0.5f, C(0.62f, 0.66f, 0.72f));
                    return Finish(p, C(0.14f, 0.16f, 0.22f));

                default:
                    // Calmar entier : manteau pointu blanc rosé moucheté, nageoires, gros œil, tentacules.
                    CreatureArt.Blob(p, 20f, 18f, 9f, 4.6f, C(0.96f, 0.86f, 0.84f));
                    CreatureArt.Blob(p, 28f, 18f, 2.5f, 5f, C(0.94f, 0.78f, 0.78f));      // nageoire
                    CreatureArt.Blob(p, 12f, 18f, 3f, 3.4f, C(0.94f, 0.80f, 0.78f));      // tête
                    PixelArt.Set(p, 12, 19, C(0.08f, 0.08f, 0.10f));
                    PixelArt.Set(p, 11, 20, C(1f, 1f, 1f));
                    for (int t = 0; t < 5; t++)
                    {
                        float y = 15f + t * 1.6f;
                        Line(p, 10f, y, 3f, y + Mathf.Sin(t * 1.3f) * 3f, 0.6f, C(0.92f, 0.70f, 0.72f));
                    }

                    foreach (Vector2Int s in new[] { new Vector2Int(18, 19), new Vector2Int(22, 17), new Vector2Int(25, 19), new Vector2Int(16, 16), new Vector2Int(20, 21) })
                    {
                        PixelArt.Set(p, s.x, s.y, C(0.76f, 0.36f, 0.40f));
                    }

                    return Finish(p, C(0.36f, 0.18f, 0.20f));
            }
        }

        /// <summary>Ver de terre : corps rose annelé en S, un peu plus foncé au bout.</summary>
        public static Color[,] Worm()
        {
            var p = Canvas();
            for (int k = 0; k <= 24; k++)
            {
                float t = k / 24f;
                float x = 5f + t * 22f;
                float y = 16f + Mathf.Sin(t * Mathf.PI * 2.2f) * 6f;
                Color c = k % 3 == 0 ? C(0.80f, 0.42f, 0.46f) : C(0.92f, 0.56f, 0.58f);
                if (k > 8 && k < 12) c = C(0.96f, 0.68f, 0.62f); // collier
                CreatureArt.Blob(p, x, y, 2.2f, 2.2f, c);
            }

            PixelArt.Set(p, 26, 22, C(1f, 0.86f, 0.86f));
            return Finish(p, C(0.40f, 0.14f, 0.18f));
        }

        /// <summary>Appât lumineux de nuit : bâton qui brille en vert, avec son halo.</summary>
        public static Color[,] GlowBait()
        {
            var p = Canvas();
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    float d = Mathf.Abs((x - y) / 1.414f);
                    float along = (x + y) / 2f;
                    if (d < 6f && along > 5f && along < 27f)
                    {
                        p[x, y] = C(0.60f, 1f, 0.50f, Mathf.Lerp(0.35f, 0f, d / 6f));
                    }
                }
            }

            Line(p, 9f, 9f, 23f, 23f, 2.2f, C(0.55f, 0.96f, 0.45f));
            Line(p, 10f, 11f, 21f, 22f, 0.7f, C(0.92f, 1f, 0.86f));         // cœur très lumineux
            CreatureArt.Blob(p, 24.5f, 24.5f, 2f, 2f, C(0.30f, 0.30f, 0.34f)); // embout
            return Finish(p, C(0.08f, 0.30f, 0.10f));
        }

        // ---------- Sacs ----------

        public static Color[,] Bag(int level)
        {
            var p = Canvas();
            switch (level)
            {
                case 0:
                    // Sacoche : petite bourse de cuir fermée par un cordon.
                    CreatureArt.Blob(p, 16f, 12f, 10f, 9f, C(0.60f, 0.40f, 0.22f));
                    CreatureArt.Blob(p, 16f, 22f, 5f, 3f, C(0.66f, 0.46f, 0.26f));
                    Line(p, 11f, 21f, 21f, 21f, 0.7f, C(0.86f, 0.72f, 0.44f));
                    CreatureArt.Blob(p, 13f, 15f, 3f, 3f, C(0.72f, 0.52f, 0.30f));       // lumière
                    return Finish(p, C(0.24f, 0.14f, 0.08f));

                case 1:
                    // Besace : rabat, boucle dorée et bandoulière.
                    Line(p, 5f, 20f, 16f, 30f, 0.9f, C(0.50f, 0.34f, 0.18f));
                    Line(p, 16f, 30f, 27f, 20f, 0.9f, C(0.50f, 0.34f, 0.18f));
                    PixelArt.Rect(p, 4, 4, 27, 20, C(0.56f, 0.70f, 0.40f));
                    PixelArt.Rect(p, 4, 13, 27, 20, C(0.46f, 0.60f, 0.32f));
                    PixelArt.Rect(p, 14, 11, 17, 14, C(0.96f, 0.80f, 0.30f));
                    PixelArt.Rect(p, 5, 5, 6, 12, C(0.64f, 0.78f, 0.48f));
                    return Finish(p, C(0.18f, 0.24f, 0.10f));

                case 2:
                    // Glacière : caisse bleue, couvercle blanc, poignée.
                    PixelArt.Rect(p, 4, 4, 27, 19, C(0.22f, 0.52f, 0.84f));
                    PixelArt.Rect(p, 3, 19, 28, 24, C(0.94f, 0.96f, 0.98f));
                    PixelArt.Rect(p, 11, 25, 20, 27, C(0.72f, 0.76f, 0.80f));
                    PixelArt.Rect(p, 5, 5, 6, 17, C(0.36f, 0.66f, 0.94f));
                    PixelArt.Rect(p, 12, 10, 19, 13, C(0.94f, 0.96f, 0.98f));             // étiquette
                    return Finish(p, C(0.08f, 0.16f, 0.30f));

                default:
                    // Grande glacière : plus large, rouge, couvercle blanc, roulettes et deux poignées.
                    PixelArt.Rect(p, 1, 6, 30, 21, C(0.86f, 0.24f, 0.20f));
                    PixelArt.Rect(p, 0, 21, 31, 26, C(0.96f, 0.96f, 0.96f));
                    PixelArt.Rect(p, 6, 27, 11, 28, C(0.72f, 0.74f, 0.78f));
                    PixelArt.Rect(p, 20, 27, 25, 28, C(0.72f, 0.74f, 0.78f));
                    PixelArt.Rect(p, 2, 7, 3, 19, C(0.96f, 0.40f, 0.34f));
                    CreatureArt.Blob(p, 6f, 4f, 2.2f, 2.2f, C(0.20f, 0.20f, 0.22f));
                    CreatureArt.Blob(p, 25f, 4f, 2.2f, 2.2f, C(0.20f, 0.20f, 0.22f));
                    PixelArt.Rect(p, 11, 12, 20, 15, C(0.96f, 0.96f, 0.96f));
                    return Finish(p, C(0.30f, 0.06f, 0.06f));
            }
        }
    }
}
