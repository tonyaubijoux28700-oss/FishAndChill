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
                case 1: blank = C(0.92f, 0.94f, 0.98f); wrap = C(0.30f, 0.62f, 0.92f); grip = C(0.70f, 0.72f, 0.76f); break;  // légère
                case 2: blank = C(0.24f, 0.48f, 0.32f); wrap = C(0.80f, 0.82f, 0.86f); grip = C(0.20f, 0.20f, 0.22f); break;  // longue
                case 3: blank = C(0.30f, 0.20f, 0.14f); wrap = C(0.92f, 0.50f, 0.16f); grip = C(0.24f, 0.18f, 0.12f); break;  // lourde
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
        public static readonly Vector2Int RodLineEnd = new Vector2Int(84, 60);

        /// <summary>
        /// Courbe de la grande canne (mêmes chiffres que la vitrine en jeu) : le brin monte de la poignée et plie de plus en
        /// plus vers le scion, comme une canne qui travaille.
        /// </summary>
        public static Vector2 RodPoint(float t) => new Vector2(6f + t * 56f + t * t * t * 28f, 6f + t * 150f - t * t * t * 12f);

        /// <summary>
        /// Grande canne pour la vitrine de l'inventaire, 96×160 : poignée en liège bagué, porte-moulinet et moulinet,
        /// brin qui s'affine et plie vers le scion, ligatures et anneaux, et le fil qui retombe en douce courbe jusqu'à
        /// <see cref="RodLineEnd"/> (l'hameçon et l'appât y sont posés en jeu).
        /// </summary>
        public static Color[,] RodLarge(int level)
        {
            var p = new Color[RodWidth, RodHeight];
            Color blank;
            Color wrap;
            Color reel;
            switch (level)
            {
                case 0: blank = C(0.86f, 0.74f, 0.44f); wrap = C(0.56f, 0.40f, 0.20f); reel = C(0.52f, 0.52f, 0.56f); break;
                case 1: blank = C(0.92f, 0.94f, 0.98f); wrap = C(0.30f, 0.62f, 0.92f); reel = C(0.80f, 0.84f, 0.90f); break;  // légère
                case 2: blank = C(0.24f, 0.48f, 0.32f); wrap = C(0.80f, 0.82f, 0.86f); reel = C(0.62f, 0.66f, 0.70f); break;  // longue
                case 3: blank = C(0.30f, 0.20f, 0.14f); wrap = C(0.92f, 0.50f, 0.16f); reel = C(0.40f, 0.40f, 0.44f); break;  // lourde
                default: blank = C(0.14f, 0.26f, 0.56f); wrap = C(0.98f, 0.80f, 0.30f); reel = C(0.96f, 0.80f, 0.36f); break;
            }

            Color cork = C(0.80f, 0.62f, 0.40f);
            Color metal = C(0.62f, 0.64f, 0.70f);
            const int steps = 200;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 a = RodPoint(t);
                Color c;
                float radius;
                if (t < 0.17f)
                {
                    // Poignée en liège, avec ses bagues sombres et son grain.
                    radius = 3.4f - t * 2f;
                    c = (i / 5) % 2 == 0 ? cork : DetailedFish.Shade(cork, 0.88f);
                    if (i % 11 == 0) c = DetailedFish.Shade(cork, 0.6f);
                }
                else if (t < 0.21f)
                {
                    radius = 2.4f;
                    c = metal; // porte-moulinet
                }
                else
                {
                    // Épaisseur du brin : fin pour la légère, épais pour la lourde.
                    float girth = level == 1 ? 0.8f : level == 3 ? 1.45f : level == 2 ? 0.9f : 1f;
                    radius = Mathf.Lerp(1.8f, 0.55f, (t - 0.21f) / 0.79f) * girth;
                    c = blank;
                    if (level == 0 && i % 26 == 0) c = DetailedFish.Shade(blank, 0.68f); // nœuds du bambou
                }

                CreatureArt.Blob(p, a.x, a.y, radius, radius, c);
                // Volume : un reflet clair d'un côté, une ombre de l'autre.
                CreatureArt.Blob(p, a.x - radius * 0.45f, a.y + radius * 0.25f, radius * 0.35f, radius * 0.35f, DetailedFish.Shade(c, 1.3f));
                CreatureArt.Blob(p, a.x + radius * 0.5f, a.y - radius * 0.3f, radius * 0.3f, radius * 0.3f, DetailedFish.Shade(c, 0.75f));
            }

            // Talon arrondi, ligatures et anneaux (plus petits vers le scion).
            Vector2 butt = RodPoint(0f);
            CreatureArt.Blob(p, butt.x, butt.y, 3.6f, 3.6f, DetailedFish.Shade(cork, 0.55f));
            float[] guides = { 0.3f, 0.43f, 0.56f, 0.68f, 0.8f, 0.9f, 0.99f };
            for (int g = 0; g < guides.Length; g++)
            {
                Vector2 a = RodPoint(guides[g]);
                Vector2 next = RodPoint(Mathf.Min(1f, guides[g] + 0.01f));
                Vector2 normal = new Vector2(next.y - a.y, -(next.x - a.x)).normalized; // côté droit du brin
                float ring = Mathf.Lerp(1.8f, 0.9f, g / (float)(guides.Length - 1));
                CreatureArt.Blob(p, a.x, a.y, ring + 0.4f, ring * 0.7f, wrap);
                Vector2 eye = a + normal * (ring + 1.6f);
                CreatureArt.Blob(p, eye.x, eye.y, ring, ring, C(0.84f, 0.86f, 0.90f));
                PixelArt.Set(p, Mathf.RoundToInt(eye.x), Mathf.RoundToInt(eye.y), C(0f, 0f, 0f, 0f));
            }

            // Moulinet sous le porte-moulinet : pied, bobine, flasque et manivelle.
            Vector2 seat = RodPoint(0.19f);
            CreatureArt.Limb(p, seat.x + 1f, seat.y - 1f, seat.x + 6f, seat.y - 5f, 0.9f, DetailedFish.Shade(reel, 0.6f));
            CreatureArt.Blob(p, seat.x + 9f, seat.y - 7f, 6.5f, 6f, reel);
            CreatureArt.Blob(p, seat.x + 9f, seat.y - 7f, 4f, 3.6f, DetailedFish.Shade(reel, 0.72f));
            CreatureArt.Blob(p, seat.x + 9f, seat.y - 7f, 1.6f, 1.6f, DetailedFish.Shade(reel, 1.2f));
            CreatureArt.Blob(p, seat.x + 6.5f, seat.y - 4.5f, 2f, 1.5f, DetailedFish.Shade(reel, 1.35f));
            CreatureArt.Limb(p, seat.x + 9f, seat.y - 7f, seat.x + 15f, seat.y - 12f, 0.7f, C(0.30f, 0.30f, 0.34f));
            CreatureArt.Blob(p, seat.x + 15.5f, seat.y - 12.5f, 1.7f, 1.7f, C(0.88f, 0.30f, 0.24f));

            PixelArt.SelectiveOutline(p, C(0.12f, 0.09f, 0.08f), 0.55f);

            // Le fil : du scion, il retombe en douce courbe jusqu'à l'hameçon (après le contour : fin et clair).
            Vector2 tip = RodPoint(1f);
            int steps2 = Mathf.RoundToInt(tip.y - RodLineEnd.y) * 2;
            for (int i = 0; i <= steps2; i++)
            {
                float s = i / (float)steps2;
                float x = Mathf.Lerp(tip.x, RodLineEnd.x, s) + Mathf.Sin(s * Mathf.PI) * 4f;
                float y = Mathf.Lerp(tip.y - 1f, RodLineEnd.y, s);
                PixelArt.Set(p, Mathf.RoundToInt(x), Mathf.RoundToInt(y), C(0.94f, 0.96f, 0.98f, 0.9f));
            }

            return p;
        }

        public const int ShowcaseWidth = 118;
        public const int ShowcaseHeight = 153;

        /// <summary>
        /// Fond de la vitrine 118×153 (affiché ×4) : présentoir en planches de bois veinées, éclairé par une lumière douce
        /// en haut au centre (les coins plus sombres), et une étagère en bas pour le sac.
        /// </summary>
        public static Color[,] ShowcaseBack()
        {
            var p = new Color[ShowcaseWidth, ShowcaseHeight];
            Color[] planks = { C(0.47f, 0.31f, 0.19f), C(0.43f, 0.28f, 0.17f), C(0.50f, 0.33f, 0.20f) };
            for (int x = 0; x < ShowcaseWidth; x++)
            {
                int plank = x / 13;
                for (int y = 0; y < ShowcaseHeight; y++)
                {
                    Color c = planks[plank % planks.Length];
                    if (x % 13 == 0)
                    {
                        c = C(0.22f, 0.14f, 0.08f); // joint entre deux planches
                    }
                    else
                    {
                        // Veines : lignes ondulées plus sombres, différentes sur chaque planche.
                        float grain = Mathf.Sin(y * 0.21f + plank * 2.3f + Mathf.Sin(y * 0.05f + plank) * 2f) + (x % 13) * 0.35f;
                        if (Mathf.Repeat(grain, 3.1f) < 0.35f) c = DetailedFish.Shade(c, 0.86f);
                        if (((x * 7 + y * 13 + plank * 31) % 97) == 0) c = DetailedFish.Shade(c, 0.7f); // petits nœuds
                    }

                    // Lumière douce en haut au centre, coins plus sombres (par paliers).
                    float dx = (x - ShowcaseWidth * 0.5f) / (ShowcaseWidth * 0.6f);
                    float dy = (y - ShowcaseHeight * 0.85f) / (ShowcaseHeight * 0.9f);
                    float light = 1.2f - Mathf.Sqrt(dx * dx + dy * dy) * 0.6f;
                    c = DetailedFish.Shade(c, Mathf.Round(Mathf.Clamp(light, 0.65f, 1.2f) * 8f) / 8f);

                    // Étagère en bas : dessus clair, chant éclairé, ombre portée sur le fond.
                    if (y < 14)
                    {
                        c = y == 13 ? C(0.74f, 0.54f, 0.32f) : y > 10 ? C(0.60f, 0.42f, 0.24f) : DetailedFish.Shade(C(0.40f, 0.26f, 0.15f), 0.9f + (y % 3) * 0.05f);
                    }
                    else if (y < 17)
                    {
                        c = DetailedFish.Shade(c, 0.7f);
                    }

                    p[x, y] = c;
                }
            }

            return p;
        }

        /// <summary>Support en laiton 10×8 sur lequel repose la canne : une petite fourche vissée au fond.</summary>
        public static Color[,] Peg()
        {
            var p = new Color[10, 8];
            Color brass = C(0.92f, 0.74f, 0.32f);
            for (int y = 0; y < 5; y++)
            {
                PixelArt.Set(p, 1, y + 2, brass);
                PixelArt.Set(p, 8, y + 2, DetailedFish.Shade(brass, 0.8f));
            }

            for (int x = 1; x <= 8; x++)
            {
                PixelArt.Set(p, x, 1, DetailedFish.Shade(brass, x < 5 ? 1.15f : 0.85f));
            }

            PixelArt.Set(p, 4, 0, C(0.40f, 0.30f, 0.12f));
            PixelArt.Set(p, 5, 0, C(0.40f, 0.30f, 0.12f));
            PixelArt.SelectiveOutline(p, C(0.20f, 0.14f, 0.06f), 0.6f);
            return p;
        }

        // ---------- Hameçons ----------

        /// <summary>Hameçons : 0 simple (J fin), 1 triple (trois pointes), 2 circulaire (pointe rentrée), 3 forgé (acier sombre à œillet doré).</summary>
        public static Color[,] Hook(int level)
        {
            var p = Canvas();
            Color metal = level == 3 ? C(0.36f, 0.40f, 0.48f) : C(0.78f, 0.82f, 0.88f);
            Color eye = level == 3 ? C(0.96f, 0.78f, 0.30f) : metal;
            float thick = level == 0 ? 0.8f : level == 2 ? 1.0f : 1.2f;

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
                // Circulaire : hampe courte, grande courbe presque fermée, pointe tournée vers la hampe.
                Line(p, 11f, 27f, 11f, 16f, thick, metal);
                for (int k = 0; k <= 20; k++)
                {
                    float a = Mathf.PI * 1.55f * k / 20f;
                    float x = 16f - Mathf.Cos(a) * 5f;
                    float y = 13f - Mathf.Sin(a) * 6.5f;
                    CreatureArt.Blob(p, x, y, thick, thick, metal);
                }

                Line(p, 16f + Mathf.Cos(Mathf.PI * 0.45f) * 5f, 13f + Mathf.Sin(Mathf.PI * 0.45f) * 6.5f, 13.5f, 17f, 0.7f, metal);
            }
            else if (level == 1)
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

            float eyeX = level == 1 ? 16f : 11f;
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
