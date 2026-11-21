using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les repères de l'atoll des Maldives, dans le style en volume (32 px par case, y depuis le haut pendant le dessin) :
    /// villa sur l'eau à piscine privée, maison en pierre de corail (toit de feuilles tressées ou de tôle rouge), bar de
    /// plage sous chaume, dhoni (bateau traditionnel à haute proue), balançoire plantée dans le lagon ; la passerelle sur
    /// pilotis et les torches de bambou (16 px par case, comme le sol).
    /// </summary>
    public static class AtollLandmarkArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Teak = C(0.62f, 0.42f, 0.26f);
        private static readonly Color TeakDark = C(0.40f, 0.26f, 0.16f);
        private static readonly Color Driftwood = C(0.80f, 0.68f, 0.50f);
        private static readonly Color Thatch = C(0.80f, 0.66f, 0.40f);
        private static readonly Color Coral = C(0.95f, 0.93f, 0.86f);
        private static readonly Color Turquoise = C(0.20f, 0.66f, 0.70f);
        private static readonly Color Glass = C(0.58f, 0.84f, 0.92f);
        private static readonly Color Pool = C(0.36f, 0.86f, 0.90f);
        private static readonly Color Ink = C(0.12f, 0.09f, 0.07f);

        private static readonly Color WaterShadow = C(0.04f, 0.24f, 0.34f, 0.35f);
        private static readonly Color Foam = C(0.90f, 0.98f, 1f, 0.7f);

        /// <summary>Ombre au sol sous un dessin (ellipse), sans toucher aux pixels déjà peints. y depuis le bas.</summary>
        private static void Shadow(Color[,] p, float cx, float cy, float rx, float ry, Color color)
        {
            for (int x = 0; x < p.GetLength(0); x++)
            {
                for (int y = 0; y < p.GetLength(1); y++)
                {
                    float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                    if (u * u + v * v <= 1f && p[x, y].a < 0.01f) p[x, y] = color;
                }
            }
        }

        /// <summary>Ronds d'eau autour d'un poteau planté dans le lagon (y depuis le bas).</summary>
        private static void Ripple(Color[,] p, float cx, float cy, float r)
        {
            for (int x = 0; x < p.GetLength(0); x++)
            {
                for (int y = 0; y < p.GetLength(1); y++)
                {
                    float u = (x + 0.5f - cx) / r, v = (y + 0.5f - cy) / (r * 0.4f);
                    float d = u * u + v * v;
                    if (p[x, y].a > 0.01f || d > 1f) continue;
                    p[x, y] = d > 0.62f && (x / 2) % 3 != 0 ? Foam : WaterShadow;
                }
            }
        }

        private static Color[,] Mirror(Color[,] p)
        {
            int w = p.GetLength(0), h = p.GetLength(1);
            var m = new Color[w, h];
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++) m[x, y] = p[w - 1 - x, y];
            }

            return m;
        }

        /// <summary>Chaume de feuilles de cocotier : mèches en biais, rangs, bord frangé (texture d'un toit).</summary>
        private static Color ThatchTexture(int x, int y, Color c) =>
            (x + (y / 4) * 2) % 3 == 0 ? Darker(c, 0.74f) : (x * 7 + y) % 9 == 0 ? Darker(c, 1.14f) : y % 7 == 0 ? Darker(c, 0.84f) : c;

        // ---------- Villa sur l'eau ----------

        public const int VillaWidth = 96;
        public const int VillaHeight = 112;

        /// <summary>Pied des pilotis, au centre de la case.</summary>
        public static readonly Vector2 VillaPivot = new Vector2(0.5f, 8f / VillaHeight);

        /// <summary>
        /// Villa sur pilotis : pilotis dans le lagon (ronds d'eau), terrasse de teck, piscine privée turquoise, grande baie
        /// vitrée, murs de lattes claires, toit de chaume en croupe qui déborde, faîtage, échelle vers le lagon, lanterne.
        /// <paramref name="variant"/> 1 : la même, en miroir (piscine à gauche).
        /// </summary>
        public static Color[,] Villa(int variant)
        {
            var cv = new VolumeCanvas(VillaWidth, VillaHeight);
            // Pilotis.
            foreach (float px in new[] { 8f, 28f, 48f, 68f, 88f })
            {
                cv.Piece(Box(px - 2.4f, 80, px + 2.4f, 104), TeakDark, 2f, (x, y, c) => y % 6 == 0 ? Darker(c, 0.75f) : c);
            }

            // Échelle vers le lagon.
            cv.Piece(Union(Box(70, 80, 72, 106), Box(78, 80, 80, 106)), Teak, 1f);
            for (int ry = 86; ry < 106; ry += 5)
            {
                int rung = ry;
                cv.Piece(Box(70, rung, 80, rung + 1.5f), Teak, 1f);
            }

            // Terrasse et son chant.
            cv.Piece(Box(1, 66, 95, 80), Teak, 2.5f, (x, y, c) => x % 6 == 0 ? Darker(c, 0.74f) : (x * 3 + y) % 13 == 0 ? Darker(c, 0.9f) : c);
            cv.Piece(Box(1, 79, 95, 86), TeakDark, 2f, (x, y, c) => x % 12 == 0 ? Darker(c, 0.7f) : c);

            // Piscine privée sur la terrasse, rebord de pierre claire.
            cv.Piece(Box(62, 66, 93, 78), Coral, 1.5f);
            cv.Piece(Box(64, 68, 91, 77), Pool, 1f, (x, y, c) => (x + y * 3) % 11 == 0 ? C(0.86f, 1f, 1f) : y > 74 ? Darker(c, 0.85f) : c);

            // Murs de lattes claires, baie vitrée et petite fenêtre.
            cv.Piece(Box(6, 34, 60, 68), Driftwood, 2.5f, (x, y, c) => x % 4 == 0 ? Darker(c, 0.84f) : c);
            cv.Piece(Box(12, 42, 40, 68), Glass, 1.5f,
                (x, y, c) => x == 26 || x == 12 || x == 39 ? TeakDark : (x - 12) - (68 - y) / 2 is int d && d > 2 && d < 6 ? C(0.92f, 0.98f, 1f) : y > 62 ? Darker(c, 0.85f) : c);
            cv.Piece(Box(45, 44, 56, 56), Glass, 1.5f, (x, y, c) => x == 50 || y == 50 ? TeakDark : c);
            // Lanterne suspendue près de la porte.
            cv.Piece(Box(41, 44, 44, 50), C(1f, 0.84f, 0.42f), 1f);

            // Toit de chaume en croupe, qui déborde sur la terrasse.
            cv.Piece((x, y) => y > 4 && y < 40 && Mathf.Abs(x - 40f) < 11f + (y - 4f) * 0.75f, Thatch, 6f, ThatchTexture);
            cv.Piece((x, y) => y >= 36 && y < 42 && Mathf.Abs(x - 40f) < 38f && ((int)x % 3 != 0 || y < 39), Darker(Thatch, 0.9f), 2f);   // frange
            cv.Piece(Box(31, 2, 49, 8), Darker(Thatch, 0.7f), 2f);   // faîtage

            Color[,] p = cv.Finish(Ink);
            foreach (float px in new[] { 8f, 28f, 48f, 68f, 88f }) Ripple(p, px, 8f, 6f);
            return variant == 1 ? Mirror(p) : p;
        }

        // ---------- Maison en pierre de corail ----------

        public const int HouseWidth = 160;
        public const int HouseHeight = 128;
        public static readonly Vector2 HousePivot = new Vector2(0.5f, 6f / HouseHeight);

        /// <summary>
        /// Maison des Maldives : murs de pierre de corail chaulés (pores), soubassement gris, porte et volets turquoise,
        /// muret de corail de part et d'autre, joali (siège de cordes) sur le côté ; <paramref name="variant"/> 0 : toit de
        /// feuilles de cocotier tressées, 1 : toit de tôle ondulée peinte en rouge.
        /// </summary>
        public static Color[,] House(int variant)
        {
            var cv = new VolumeCanvas(HouseWidth, HouseHeight);
            System.Func<int, int, Color, Color> pores = (x, y, c) =>
            {
                int h = (x * 7 + y * 13) % 23;
                return h == 0 ? Darker(c, 0.78f) : h == 5 ? Darker(c, 0.88f) : y > 112 ? Darker(c, 0.82f) : c;
            };

            // Murets de corail.
            cv.Piece(Box(0, 104, 24, 122), Coral, 2.5f, pores);
            cv.Piece(Box(136, 104, 160, 122), Coral, 2.5f, pores);

            // Murs, soubassement.
            cv.Piece(Box(20, 56, 140, 122), Coral, 3f, pores);
            cv.Piece(Box(20, 114, 140, 122), C(0.72f, 0.72f, 0.70f), 1.5f);

            // Porte à deux battants et volets turquoise.
            cv.Piece(Box(68, 80, 92, 122), Turquoise, 2f, (x, y, c) => x == 80 ? Darker(c, 0.7f) : (x - 68) % 6 == 0 ? Darker(c, 0.86f) : c);
            cv.Dot(77, 102, C(0.96f, 0.84f, 0.40f));
            cv.Dot(83, 102, C(0.96f, 0.84f, 0.40f));
            foreach (int wx in new[] { 32, 106 })
            {
                cv.Piece(Box(wx + 4, 76, wx + 18, 98), C(0.16f, 0.14f, 0.14f), 1f);
                cv.Piece(Box(wx - 2, 74, wx + 6, 100), Turquoise, 1.5f, (x, y, c) => y % 4 == 0 ? Darker(c, 0.8f) : c);
                cv.Piece(Box(wx + 16, 74, wx + 24, 100), Turquoise, 1.5f, (x, y, c) => y % 4 == 0 ? Darker(c, 0.8f) : c);
                cv.Piece(Box(wx + 2, 100, wx + 20, 104), Coral, 1.5f);   // appui
            }

            // Toit.
            System.Func<float, float, bool> roof = (x, y) => y > 10 && y < 62 && Mathf.Abs(x - 80f) < 36f + (y - 10f) * 0.72f;
            if (variant == 1)
            {
                cv.Piece(roof, C(0.74f, 0.20f, 0.16f), 5f, (x, y, c) => x % 5 == 0 ? Darker(c, 0.72f) : x % 5 == 1 ? Darker(c, 1.15f) : (x * 3 + y * 5) % 37 == 0 ? C(0.56f, 0.30f, 0.22f) : c);
                cv.Piece(Box(42, 6, 118, 13), C(0.62f, 0.62f, 0.64f), 2f);
            }
            else
            {
                cv.Piece(roof, C(0.66f, 0.56f, 0.40f), 6f, (x, y, c) => ((x / 3) + (y / 3)) % 2 == 0 ? Darker(c, 0.84f) : (x * 5 + y) % 11 == 0 ? Darker(c, 1.12f) : c);
                cv.Piece(Box(42, 6, 118, 14), C(0.52f, 0.42f, 0.30f), 2.5f);
            }

            // Joali : siège de cordes tendues sur un cadre de bois.
            cv.Piece(Union(Box(142, 96, 145, 126), Box(155, 96, 158, 126), Box(141, 94, 159, 98)), TeakDark, 1.5f);
            cv.Piece((x, y) => x > 144 && x < 156 && y > 104 && y < 116 + Mathf.Sin((x - 144f) / 12f * Mathf.PI) * 4f, C(0.92f, 0.82f, 0.56f), 1.5f,
                (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.78f) : c);

            Color[,] p = cv.Finish(Ink);
            Shadow(p, 80f, 5f, 76f, 5f, C(0.06f, 0.14f, 0.08f, 0.3f));
            return p;
        }

        // ---------- Bar de plage ----------

        public const int BarWidth = 160;
        public const int BarHeight = 136;
        public static readonly Vector2 BarPivot = new Vector2(0.5f, 6f / BarHeight);

        /// <summary>
        /// Bar de plage : poteaux de tronc de cocotier, grand toit de chaume frangé, guirlande de lanternes, comptoir de
        /// planches, étagère de bouteilles, noix de coco à paille, tabourets.
        /// </summary>
        public static Color[,] Bar()
        {
            var cv = new VolumeCanvas(BarWidth, BarHeight);
            Color trunk = C(0.58f, 0.44f, 0.30f);
            System.Func<int, int, Color, Color> rings = (x, y, c) => y % 5 == 0 ? Darker(c, 0.72f) : c;
            foreach (float px in new[] { 32f, 128f }) cv.Piece(Box(px - 3, 40, px + 3, 96), Darker(trunk, 0.85f), 2f, rings);   // poteaux du fond

            // Étagère et bouteilles.
            cv.Piece(Box(40, 70, 120, 74), TeakDark, 1.5f);
            Color[] bottles = { C(0.30f, 0.62f, 0.36f), C(0.86f, 0.56f, 0.20f), C(0.40f, 0.62f, 0.86f), C(0.86f, 0.30f, 0.34f), C(0.94f, 0.90f, 0.78f) };
            for (int i = 0; i < 10; i++)
            {
                float bx = 46f + i * 7.6f;
                cv.Piece(Union(Box(bx - 2.2f, 60, bx + 2.2f, 70), Box(bx - 0.8f, 55, bx + 0.8f, 61)), bottles[i % bottles.Length], 1.5f);
            }

            // Comptoir.
            cv.Piece(Box(26, 92, 134, 124), Teak, 3f, (x, y, c) => y % 6 == 0 ? Darker(c, 0.74f) : (x * 5 + y) % 17 == 0 ? Darker(c, 0.88f) : c);
            cv.Piece(Box(22, 86, 138, 94), Driftwood, 2.5f);
            // Noix de coco à paille et verres sur le comptoir.
            foreach (float nx in new[] { 44f, 92f })
            {
                cv.Piece(Ellipse(nx, 82f, 5.5f, 5f), C(0.50f, 0.34f, 0.18f), 3f, (x, y, c) => (x + y) % 4 == 0 ? Darker(c, 0.8f) : c);
                cv.Piece(Capsule(nx + 1f, 78f, nx + 5f, 70f, 0.9f), C(0.96f, 0.46f, 0.62f), 1f);
                cv.Piece(Box(nx - 3, 74, nx + 3, 78), C(0.98f, 0.94f, 0.82f), 1f);   // tranche de coco ouverte
            }

            cv.Piece(Box(64, 76, 70, 86), C(0.96f, 0.62f, 0.22f, 1f), 1.2f);
            cv.Piece(Box(112, 76, 118, 86), C(0.42f, 0.80f, 0.88f, 1f), 1.2f);

            // Tabourets.
            foreach (float sx in new[] { 42f, 68f, 94f, 120f })
            {
                cv.Piece(Box(sx - 1.5f, 116, sx + 1.5f, 130), TeakDark, 1f);
                cv.Piece(Ellipse(sx, 116f, 7f, 3.2f), Driftwood, 2f);
            }

            // Poteaux de devant.
            foreach (float px in new[] { 14f, 146f }) cv.Piece(Box(px - 3.5f, 40, px + 3.5f, 130), trunk, 2.5f, rings);

            // Grand toit de chaume frangé, guirlande de lanternes.
            cv.Piece((x, y) => y > 4 && y < 46 && Mathf.Abs(x - 80f) < 34f + (y - 4f) * 0.95f, Thatch, 7f, ThatchTexture);
            cv.Piece((x, y) => y >= 42 && y < 50 && Mathf.Abs(x - 80f) < 76f && ((int)x % 3 != 0 || y < 46), Darker(Thatch, 0.88f), 2f);
            cv.Piece(Box(52, 1, 108, 8), Darker(Thatch, 0.7f), 2f);
            Color[,] p = cv.Finish(Ink);
            Color[] bulbs = { C(1f, 0.86f, 0.40f), C(1f, 0.56f, 0.40f), C(0.60f, 0.90f, 0.70f), C(1f, 0.70f, 0.86f) };
            for (int i = 0; i < 14; i++)
            {
                int bx = 10 + i * 11;
                int by = BarHeight - 1 - (54 + Mathf.RoundToInt(Mathf.Sin((bx - 10f) / 22f * Mathf.PI) * 3f));
                PixelArt.Rect(p, bx, by - 1, bx + 1, by, bulbs[i % bulbs.Length]);
            }

            Shadow(p, 80f, 5f, 76f, 5f, C(0.30f, 0.26f, 0.18f, 0.28f));
            return p;
        }

        // ---------- Dhoni ----------

        public const int DhoniWidth = 144;
        public const int DhoniHeight = 88;

        /// <summary>
        /// Dhoni : coque de bois verni à bande blanche et bleue, haute proue recourbée à l'avant, cabine blanche et son auvent
        /// de toile bleue, petit mât, reflet ondulé sur le lagon. Posé au centre de sa case d'eau.
        /// </summary>
        public static Color[,] Dhoni()
        {
            var cv = new VolumeCanvas(DhoniWidth, DhoniHeight);
            // Coque : bord supérieur droit, quille en arc ; la proue monte en col de cygne à gauche.
            System.Func<float, float, bool> hull = (x, y) =>
            {
                if (x < 14 || x > 136) return false;
                float t = (x - 14f) / 122f;
                float top = 52f - (t > 0.9f ? (t - 0.9f) * 60f : 0f);
                float bottom = 72f - Mathf.Pow(Mathf.Abs(t - 0.55f) * 1.8f, 2f) * 10f;
                return y > top && y < bottom;
            };
            cv.Piece(hull, Teak, 5f, (x, y, c) => y < 56 ? C(0.96f, 0.96f, 0.94f) : y < 60 ? C(0.18f, 0.40f, 0.70f) : (x + y * 3) % 9 == 0 ? Darker(c, 0.82f) : y % 5 == 0 ? Darker(c, 0.86f) : c);
            // Haute proue recourbée.
            cv.Piece(Union(Capsule(18f, 58f, 12f, 36f, 4f), Capsule(12f, 36f, 10f, 16f, 3.2f), Capsule(10f, 16f, 18f, 8f, 2.6f)), Teak, 3f,
                (x, y, c) => y % 6 == 0 ? Darker(c, 0.8f) : c);
            // Cabine et auvent.
            cv.Piece(Box(62, 32, 112, 54), C(0.96f, 0.96f, 0.94f), 2.5f, (x, y, c) => (x - 66) % 12 < 6 && y > 37 && y < 45 ? Glass : c);
            cv.Piece(Box(56, 24, 118, 33), C(0.22f, 0.46f, 0.76f), 3f, (x, y, c) => (x / 6) % 2 == 0 ? Darker(c, 1.12f) : c);
            cv.Piece(Box(40, 6, 43, 52), TeakDark, 1.5f);   // mât
            cv.Piece(Capsule(42f, 10f, 70f, 24f, 0.6f), C(0.86f, 0.80f, 0.62f), 0.5f);   // hauban
            Color[,] p = cv.Finish(Ink);
            // Reflet ondulé sous la coque.
            for (int x = 14; x < 136; x++)
            {
                for (int y = 2; y < 18; y++)
                {
                    if (p[x, y].a > 0.01f) continue;
                    float wave = Mathf.Sin(x * 0.4f + y * 1.3f);
                    if (y < 16 - Mathf.Abs(x - 75f) * 0.12f && wave > -0.2f) p[x, y] = wave > 0.7f ? Foam : WaterShadow;
                }
            }

            return p;
        }

        // ---------- Balançoire dans le lagon ----------

        public const int SwingWidth = 64;
        public const int SwingHeight = 104;
        public static readonly Vector2 SwingPivot = new Vector2(0.5f, 8f / SwingHeight);

        /// <summary>Balançoire plantée dans le lagon : portique de bois flotté, cordes, siège de planche, ronds d'eau aux pieds.</summary>
        public static Color[,] Swing()
        {
            var cv = new VolumeCanvas(SwingWidth, SwingHeight);
            cv.Piece(Union(Capsule(10f, 96f, 14f, 10f, 2.6f), Capsule(54f, 96f, 50f, 10f, 2.6f)), Driftwood, 2.5f, (x, y, c) => (x * 3 + y) % 7 == 0 ? Darker(c, 0.8f) : c);
            cv.Piece(Box(4, 5, 60, 12), Driftwood, 2.5f, (x, y, c) => x % 9 == 0 ? Darker(c, 0.8f) : c);
            cv.Piece(Union(Box(22, 12, 23.5f, 72), Box(40.5f, 12, 42, 72)), C(0.86f, 0.76f, 0.54f), 0.8f, (x, y, c) => y % 3 == 0 ? Darker(c, 0.8f) : c);
            cv.Piece(Box(17, 70, 47, 76), Teak, 2f);
            Color[,] p = cv.Finish(Ink);
            Ripple(p, 11f, 8f, 7f);
            Ripple(p, 53f, 8f, 7f);
            return p;
        }

        // ---------- Passerelle et torches (16 px par case) ----------

        /// <summary>
        /// Planches de la passerelle sur pilotis, <paramref name="cells"/> × <paramref name="rows"/> cases (+ 6 px sous le
        /// bord avant : chant et pilotis dans l'eau). Planches posées en travers de la longueur, rebord sombre, clous.
        /// </summary>
        public static Color[,] Boardwalk(int cells, int rows)
        {
            int w = cells * 16, h = rows * 16 + 6;
            bool across = cells >= rows;   // barre du T : planches verticales ; pied : planches horizontales
            var p = new Color[w, h];
            Color plank = C(0.66f, 0.48f, 0.30f), gap = C(0.36f, 0.24f, 0.14f), light = C(0.76f, 0.58f, 0.38f), rim = C(0.46f, 0.32f, 0.20f);
            for (int x = 0; x < w; x++)
            {
                for (int y = 6; y < h; y++)
                {
                    int along = across ? x : y - 6;
                    int side = across ? y - 6 : x;
                    int sideMax = across ? rows * 16 : cells * 16;
                    Color c = along % 4 == 3 ? gap : (along / 4 * 7 + side / 9) % 5 == 0 ? Darker(plank, 0.9f) : plank;
                    if (along % 4 == 0) c = light;
                    if (side == 1 || side == sideMax - 2) c = along % 4 == 1 ? C(0.30f, 0.28f, 0.26f) : rim;   // poutres de rive et clous
                    if (side == 0 || side == sideMax - 1) c = rim;
                    p[x, y] = c;
                }

                // Chant avant, pilotis et leur ombre dans l'eau.
                for (int y = 3; y < 6; y++) p[x, y] = Darker(rim, 0.8f);
                bool post = x % 16 == 2 || x % 16 == 3 || x == w - 3 || x == w - 2;
                p[x, 2] = post ? TeakDark : WaterShadow;
                p[x, 1] = post ? Darker(TeakDark, 0.8f) : WaterShadow;
                p[x, 0] = post ? Foam : C(0, 0, 0, 0);
            }

            return p;
        }

        public const int TorchWidth = 16;
        public const int TorchHeight = 32;
        public static readonly Vector2 TorchPivot = new Vector2(0.5f, 2f / TorchHeight);

        /// <summary>Torche de bambou : tige à nœuds, coupelle, flamme orange (allumée la nuit, comme un lampadaire).</summary>
        public static Color[,] Torch()
        {
            var p = new Color[TorchWidth, TorchHeight];
            Color bamboo = C(0.78f, 0.68f, 0.40f), node = C(0.54f, 0.44f, 0.24f);
            for (int y = 2; y < 22; y++)
            {
                p[7, y] = y % 6 == 0 ? node : Darker(bamboo, 0.85f);
                p[8, y] = y % 6 == 0 ? node : bamboo;
            }

            PixelArt.Rect(p, 5, 21, 10, 23, C(0.36f, 0.26f, 0.16f));
            PixelArt.Rect(p, 6, 24, 9, 26, C(1f, 0.56f, 0.16f));
            PixelArt.Rect(p, 7, 27, 8, 29, C(1f, 0.82f, 0.30f));
            PixelArt.Set(p, 7, 30, C(1f, 0.94f, 0.66f));
            PixelArt.Set(p, 9, 27, C(1f, 0.64f, 0.20f));
            for (int x = 4; x < 12; x++) p[x, 1] = C(0.30f, 0.26f, 0.18f, 0.3f);
            return p;
        }
    }
}
