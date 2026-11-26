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

            // Chaque canne a sa silhouette : bambou épais à nœuds sans moulinet, légère toute fine, longue qui file d'un
            // coin à l'autre, lourde courte et épaisse au moulinet doré dessus, compétition fine aux garnitures dorées.
            float girth = level == 0 ? 1.2f : level == 1 ? 0.55f : level == 3 ? 1.4f : 0.85f;
            float gripLength = level == 2 ? 9f : level == 3 ? 8f : 7f;
            Line(p, 4f, 4f, 4f + gripLength, 4f + gripLength, level == 1 ? 1.4f : 1.8f, grip);   // poignée
            Line(p, 4f + gripLength, 4f + gripLength, 28f, 28f, girth, blank);                     // brin
            Line(p, 21f, 21f, 29f, 29f, girth * 0.65f, DetailedFish.Shade(blank, 1.1f));
            if (level != 0)
            {
                int rings = level == 3 ? 3 : 4;
                for (int k = 0; k < rings; k++)
                {
                    float t = 13f + k * (16f / rings);                    // anneaux et ligatures
                    PixelArt.Set(p, Mathf.RoundToInt(t) + 1, Mathf.RoundToInt(t) - 1, wrap);
                    if (level == 3) PixelArt.Set(p, Mathf.RoundToInt(t) + 2, Mathf.RoundToInt(t) - 2, C(0.96f, 0.78f, 0.34f));   // rouleaux
                }
            }

            switch (level)
            {
                case 0:
                    // Nœuds du bambou et plioir de bois au lieu d'un moulinet.
                    for (int k = 0; k < 4; k++) PixelArt.Set(p, 13 + k * 4, 13 + k * 4, DetailedFish.Shade(blank, 0.6f));
                    CreatureArt.Blob(p, 11f, 5f, 1.6f, 2.6f, C(0.88f, 0.88f, 0.82f));
                    PixelArt.Set(p, 9, 5, C(0.56f, 0.40f, 0.22f));
                    PixelArt.Set(p, 13, 5, C(0.56f, 0.40f, 0.22f));
                    break;
                case 3:
                    // Gros moulinet tambour doré, sur le dessus.
                    CreatureArt.Blob(p, 8f, 14f, 3.2f, 2.8f, C(0.96f, 0.78f, 0.34f));
                    PixelArt.Set(p, 8, 14, C(0.40f, 0.30f, 0.12f));
                    PixelArt.Set(p, 5, 16, C(0.10f, 0.10f, 0.12f));
                    break;
                case 4:
                    // Moulinet casting profilé sur le dessus.
                    CreatureArt.Blob(p, 7.5f, 12.5f, 2.6f, 1.8f, C(0.16f, 0.22f, 0.40f), 0.75f);
                    PixelArt.Set(p, 7, 12, C(0.96f, 0.78f, 0.34f));
                    break;
                default:
                    // Moulinet à tambour fixe sous la poignée : petit pour la légère, gros pour la longue.
                    float size = level == 2 ? 3.4f : 2.0f;
                    CreatureArt.Blob(p, 10f + size * 0.3f, 6f - size * 0.3f, size, size * 0.85f, level == 2 ? C(0.34f, 0.36f, 0.40f) : C(0.84f, 0.86f, 0.90f));
                    PixelArt.Set(p, Mathf.RoundToInt(10f + size * 0.3f), Mathf.RoundToInt(6f - size * 0.3f), C(0.30f, 0.30f, 0.34f));
                    PixelArt.Set(p, 13, 3, C(0.30f, 0.30f, 0.34f));
                    break;
            }

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
        /// Grande canne pour la vitrine de l'inventaire, 96×160, le long de la même courbe (<see cref="RodPoint"/>) pour
        /// toutes, mais chacune construite à sa façon :
        /// 0 bambou : perche à nœuds sans moulinet, poignée en ficelle, plioir en bois où s'enroule le fil, ligne nouée au
        /// scion ; 1 légère : brin blanc très fin en fibre de verre, poignée de liège en deux morceaux, petit moulinet à
        /// tambour fixe et son pick-up ; 2 longue (surfcasting) : trois brins emmanchés, longue poignée en mousse noire,
        /// gros moulinet à bobine longue, anneaux de lancer géants ; 3 lourde (pêche au gros) : brin épais, talon à
        /// cardan, anneaux à rouleaux, moulinet tambour doré monté dessus avec son levier de frein ; 4 compétition :
        /// carbone tissé à reflets bleus, garnitures dorées, poignée split, moulinet casting profilé.
        /// Le fil retombe jusqu'à <see cref="RodLineEnd"/> (l'hameçon et l'appât y sont posés en jeu).
        /// </summary>
        public static Color[,] RodLarge(int level)
        {
            var p = new Color[RodWidth, RodHeight];
            Color ink = C(0.12f, 0.09f, 0.08f);
            Color metal = C(0.66f, 0.68f, 0.74f);
            Color gold = C(0.96f, 0.78f, 0.34f);

            // Fin de la poignée et du porte-moulinet, selon la canne (t de 0 au talon à 1 au scion).
            float gripEnd, seatEnd;
            switch (level)
            {
                case 0: gripEnd = 0.14f; seatEnd = 0.14f; break;    // pas de porte-moulinet
                case 1: gripEnd = 0.15f; seatEnd = 0.19f; break;
                case 2: gripEnd = 0.24f; seatEnd = 0.28f; break;
                case 3: gripEnd = 0.20f; seatEnd = 0.26f; break;
                default: gripEnd = 0.16f; seatEnd = 0.21f; break;
            }

            Color BlankColor(float t, int i)
            {
                switch (level)
                {
                    case 0:
                    {
                        Color bamboo = Color.Lerp(C(0.94f, 0.80f, 0.44f), C(0.80f, 0.84f, 0.46f), t);
                        float seg = Mathf.Repeat((t - gripEnd) * 7.5f, 1f);
                        return seg < 0.09f ? C(0.46f, 0.34f, 0.16f) : seg < 0.16f ? DetailedFish.Shade(bamboo, 1.2f) : bamboo;
                    }
                    case 1: return t > 0.93f ? C(0.95f, 0.36f, 0.30f) : C(0.94f, 0.95f, 0.97f);
                    case 2: return C(0.22f, 0.46f, 0.32f);
                    case 3: return C(0.16f, 0.12f, 0.10f);
                    default: return (i / 2) % 2 == 0 ? C(0.14f, 0.16f, 0.20f) : C(0.20f, 0.26f, 0.40f);   // carbone tissé
                }
            }

            float BlankRadius(float t)
            {
                float k = Mathf.Clamp01((t - seatEnd) / (1f - seatEnd));
                switch (level)
                {
                    case 0: return Mathf.Lerp(2.4f, 0.8f, k) + (Mathf.Repeat((t - gripEnd) * 7.5f, 1f) < 0.09f ? 0.6f : 0f);   // nœuds renflés
                    case 1: return Mathf.Lerp(1.2f, 0.4f, k);
                    case 2: return k < 0.35f ? 1.9f : k < 0.7f ? 1.4f : Mathf.Lerp(1.0f, 0.5f, (k - 0.7f) / 0.3f);   // trois brins emmanchés
                    case 3: return Mathf.Lerp(2.8f, 1.2f, k);
                    default: return Mathf.Lerp(1.6f, 0.5f, k);
                }
            }

            const int steps = 220;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 a = RodPoint(t);
                Color c;
                float radius;
                if (t < gripEnd)
                {
                    switch (level)
                    {
                        case 0: radius = 2.9f; c = i % 3 == 0 ? C(0.46f, 0.34f, 0.20f) : C(0.66f, 0.52f, 0.32f); break;   // ficelle enroulée
                        case 1:
                            // Liège en deux morceaux, le brin nu entre les deux.
                            bool gap = t > 0.06f && t < 0.09f;
                            radius = gap ? 1.3f : 2.8f;
                            c = gap ? C(0.94f, 0.95f, 0.97f) : (i / 4) % 2 == 0 ? C(0.82f, 0.64f, 0.42f) : C(0.72f, 0.54f, 0.34f);
                            break;
                        case 2: radius = 3.0f; c = i % 9 == 0 ? C(0.20f, 0.20f, 0.22f) : C(0.10f, 0.10f, 0.12f); break;   // mousse noire
                        case 3: radius = 3.6f; c = i % 2 == 0 ? C(0.90f, 0.90f, 0.86f) : C(0.12f, 0.12f, 0.14f); break;   // poignée en cordelette
                        default:
                            bool split = t > 0.07f && t < 0.11f;
                            radius = split ? 1.7f : 3.0f;
                            c = split ? C(0.16f, 0.18f, 0.24f) : C(0.18f, 0.18f, 0.20f);
                            break;
                    }
                }
                else if (t < seatEnd)
                {
                    radius = level == 3 ? 3.2f : 2.4f;
                    c = level == 4 ? gold : level == 3 ? C(0.80f, 0.64f, 0.30f) : metal;   // porte-moulinet
                }
                else
                {
                    radius = BlankRadius(t);
                    c = BlankColor(t, i);
                }

                CreatureArt.Blob(p, a.x, a.y, radius, radius, c);
                CreatureArt.Blob(p, a.x - radius * 0.45f, a.y + radius * 0.25f, radius * 0.35f, radius * 0.35f, DetailedFish.Shade(c, 1.3f));
                CreatureArt.Blob(p, a.x + radius * 0.5f, a.y - radius * 0.3f, radius * 0.3f, radius * 0.3f, DetailedFish.Shade(c, 0.75f));
            }

            // Les emmanchements de la canne longue : une bague à chaque changement de brin.
            if (level == 2)
            {
                foreach (float k in new[] { 0.35f, 0.7f })
                {
                    Vector2 at = RodPoint(seatEnd + k * (1f - seatEnd));
                    CreatureArt.Blob(p, at.x, at.y, 2.4f, 2.4f, metal);
                }
            }

            // Talon : bout de bambou coupé, embout de caoutchouc, cardan, embout doré.
            Vector2 butt = RodPoint(0f);
            switch (level)
            {
                case 0: CreatureArt.Blob(p, butt.x, butt.y, 3f, 3f, C(0.58f, 0.46f, 0.24f)); break;
                case 3:
                    CreatureArt.Blob(p, butt.x, butt.y, 4.4f, 4.4f, C(0.80f, 0.64f, 0.30f));
                    CreatureArt.Limb(p, butt.x - 4f, butt.y - 1f, butt.x + 4f, butt.y + 1f, 1f, C(0.40f, 0.32f, 0.16f));   // fente du cardan
                    break;
                case 4: CreatureArt.Blob(p, butt.x, butt.y, 3.4f, 3.4f, gold); break;
                default: CreatureArt.Blob(p, butt.x, butt.y, 3.4f, 3.4f, C(0.20f, 0.20f, 0.22f)); break;
            }

            // Anneaux : aucun pour le bambou (le fil est noué au scion), fins pour la légère, géants puis petits pour la
            // longue, à rouleaux pour la lourde, minuscules et dorés pour la compétition.
            Color wrap = level == 1 ? C(0.30f, 0.62f, 0.92f) : level == 2 ? C(0.86f, 0.86f, 0.88f) : level == 3 ? C(0.92f, 0.50f, 0.16f) : C(0.30f, 0.52f, 0.96f);
            float[] guides = level == 0 ? new float[0] : level == 3 ? new[] { 0.36f, 0.52f, 0.68f, 0.84f, 0.99f } : new[] { 0.3f, 0.43f, 0.56f, 0.68f, 0.8f, 0.9f, 0.99f };
            for (int g = 0; g < guides.Length; g++)
            {
                Vector2 a = RodPoint(guides[g]);
                Vector2 next = RodPoint(Mathf.Min(1f, guides[g] + 0.01f));
                Vector2 side = level == 3 || level == 4 ? new Vector2(-(next.y - a.y), next.x - a.x).normalized : new Vector2(next.y - a.y, -(next.x - a.x)).normalized;
                float k = g / (float)Mathf.Max(1, guides.Length - 1);
                float ring = level == 2 ? Mathf.Lerp(3.4f, 0.9f, Mathf.Sqrt(k)) : level == 4 ? Mathf.Lerp(1.1f, 0.6f, k) : Mathf.Lerp(1.6f, 0.8f, k);
                CreatureArt.Blob(p, a.x, a.y, BlankRadius(guides[g]) + 0.6f, 1.1f, wrap);   // ligature
                Vector2 eye = a + side * (ring + 1.6f);
                CreatureArt.Limb(p, a.x, a.y, eye.x, eye.y, 0.45f, level == 4 ? gold : metal);
                if (level == 3)
                {
                    // Rouleau : une petite roue dorée dans sa chape.
                    CreatureArt.Blob(p, eye.x, eye.y, 1.8f, 1.8f, C(0.40f, 0.40f, 0.44f));
                    CreatureArt.Blob(p, eye.x, eye.y, 1.1f, 1.1f, gold);
                }
                else
                {
                    CreatureArt.Blob(p, eye.x, eye.y, ring, ring, level == 4 ? gold : C(0.84f, 0.86f, 0.90f));
                    if (ring > 1.4f) CreatureArt.Blob(p, eye.x, eye.y, ring - 0.9f, ring - 0.9f, C(0f, 0f, 0f, 0f));
                    else PixelArt.Set(p, Mathf.RoundToInt(eye.x), Mathf.RoundToInt(eye.y), C(0f, 0f, 0f, 0f));
                }
            }

            // Le moulinet (ou le plioir du bambou).
            Vector2 seat = RodPoint((gripEnd + seatEnd) * 0.5f);
            switch (level)
            {
                case 0:
                {
                    // Plioir de bois en H attaché contre la poignée, le fil enroulé dessus.
                    Vector2 w = RodPoint(0.08f) + new Vector2(6f, -2f);
                    CreatureArt.Limb(p, w.x - 3f, w.y - 6f, w.x - 3f, w.y + 6f, 1.2f, C(0.56f, 0.40f, 0.22f));
                    CreatureArt.Limb(p, w.x + 3f, w.y - 6f, w.x + 3f, w.y + 6f, 1.2f, C(0.56f, 0.40f, 0.22f));
                    CreatureArt.Blob(p, w.x, w.y, 3.2f, 4.2f, C(0.92f, 0.92f, 0.86f));
                    for (int y = -4; y <= 4; y += 2) PixelArt.Set(p, Mathf.RoundToInt(w.x), Mathf.RoundToInt(w.y) + y, C(0.70f, 0.70f, 0.66f));
                    break;
                }
                case 1:
                {
                    // Petit moulinet à tambour fixe, pendu sous la canne, pick-up en fil d'acier.
                    Color body = C(0.84f, 0.86f, 0.90f);
                    CreatureArt.Limb(p, seat.x + 1f, seat.y - 1f, seat.x + 4f, seat.y - 4f, 0.7f, DetailedFish.Shade(body, 0.6f));
                    CreatureArt.Blob(p, seat.x + 6f, seat.y - 6f, 4f, 3.6f, body);
                    CreatureArt.Blob(p, seat.x + 8.5f, seat.y - 3.5f, 2.2f, 2.8f, C(0.30f, 0.62f, 0.92f), -0.8f);   // bobine bleue
                    CreatureArt.Limb(p, seat.x + 6f, seat.y - 6f, seat.x + 10f, seat.y - 10f, 0.5f, C(0.30f, 0.30f, 0.34f));
                    CreatureArt.Blob(p, seat.x + 10.5f, seat.y - 10.5f, 1.2f, 1.2f, C(0.92f, 0.92f, 0.94f));
                    break;
                }
                case 2:
                {
                    // Gros moulinet de surfcasting : bobine longue, grande manivelle.
                    Color body = C(0.30f, 0.32f, 0.36f);
                    CreatureArt.Limb(p, seat.x + 1f, seat.y - 1f, seat.x + 6f, seat.y - 6f, 1.1f, body);
                    CreatureArt.Blob(p, seat.x + 10f, seat.y - 10f, 7.5f, 7f, body);
                    CreatureArt.Blob(p, seat.x + 14f, seat.y - 5f, 3.6f, 6f, C(0.86f, 0.86f, 0.88f), -0.8f);   // bobine longue
                    CreatureArt.Blob(p, seat.x + 10f, seat.y - 10f, 2.2f, 2.2f, C(0.60f, 0.62f, 0.66f));
                    CreatureArt.Limb(p, seat.x + 10f, seat.y - 10f, seat.x + 17f, seat.y - 17f, 0.8f, C(0.24f, 0.24f, 0.26f));
                    CreatureArt.Blob(p, seat.x + 17.5f, seat.y - 17.5f, 2f, 2f, C(0.10f, 0.10f, 0.12f));
                    break;
                }
                case 3:
                {
                    // Moulinet tambour doré, monté sur le dessus, flasques larges, levier de frein rouge.
                    Vector2 top = seat + new Vector2(-7f, 5f);
                    CreatureArt.Blob(p, top.x, top.y, 7f, 6f, gold);
                    CreatureArt.Blob(p, top.x, top.y, 4.6f, 4f, C(0.40f, 0.30f, 0.12f));
                    CreatureArt.Blob(p, top.x, top.y, 3f, 2.6f, C(0.90f, 0.90f, 0.86f));   // le fil sur le tambour
                    CreatureArt.Limb(p, top.x, top.y, top.x - 7f, top.y + 5f, 0.9f, C(0.30f, 0.30f, 0.34f));
                    CreatureArt.Blob(p, top.x - 7.5f, top.y + 5.5f, 1.8f, 1.8f, C(0.10f, 0.10f, 0.12f));
                    CreatureArt.Limb(p, top.x + 2f, top.y + 4f, top.x + 6f, top.y + 9f, 0.9f, C(0.86f, 0.22f, 0.18f));   // levier de frein
                    break;
                }
                default:
                {
                    // Moulinet casting profilé, sur le dessus, flasque bleu nuit, manivelle double.
                    Vector2 top = seat + new Vector2(-4f, 3.5f);
                    CreatureArt.Blob(p, top.x, top.y, 8f, 5f, C(0.18f, 0.26f, 0.50f), 0.75f);
                    CreatureArt.Blob(p, top.x, top.y, 4.6f, 3f, gold, 0.75f);
                    CreatureArt.Blob(p, top.x, top.y, 2f, 1.4f, C(0.86f, 0.88f, 0.92f), 0.75f);
                    CreatureArt.Limb(p, top.x - 6f, top.y + 5f, top.x + 1f, top.y + 9f, 0.7f, C(0.30f, 0.30f, 0.34f));
                    CreatureArt.Blob(p, top.x - 6.5f, top.y + 5f, 1.8f, 1.2f, C(0.10f, 0.10f, 0.12f));
                    CreatureArt.Blob(p, top.x + 1.5f, top.y + 9.2f, 1.8f, 1.2f, C(0.10f, 0.10f, 0.12f));
                    break;
                }
            }

            PixelArt.SelectiveOutline(p, ink, 0.55f);

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
