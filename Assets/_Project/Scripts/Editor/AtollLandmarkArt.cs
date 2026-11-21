using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les repères de l'atoll des Maldives, dans le style en volume (32 px par case, y depuis le haut pendant le dessin) :
    /// villa sur l'eau à piscine privée, maison en pierre de corail (toit de feuilles tressées ou de tôle rouge), bar de
    /// plage sous chaume, dhoni (bateau traditionnel à haute proue) ; la passerelle sur
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

        public const int VillaWidth = 160;
        public const int VillaHeight = 168;

        /// <summary>Pied des pilotis, au centre de la case.</summary>
        public static readonly Vector2 VillaPivot = new Vector2(0.5f, 10f / VillaHeight);

        /// <summary>
        /// Grande villa sur pilotis (5 × 5 cases, comme la maison du joueur) : pilotis dans le lagon (ronds d'eau), large
        /// terrasse de teck, piscine privée turquoise et transat, grande baie vitrée, murs de lattes claires, haut toit de
        /// chaume en croupe centré sur les murs, frange, faîtage, échelle vers le lagon, lanterne.
        /// <paramref name="variant"/> 1 : la même, en miroir (piscine à gauche).
        /// </summary>
        public static Color[,] Villa(int variant)
        {
            const float axis = 64f;   // axe des murs et du toit
            var cv = new VolumeCanvas(VillaWidth, VillaHeight);
            float[] posts = { 10f, 44f, 80f, 116f, 150f };

            // Pilotis.
            foreach (float px in posts)
            {
                cv.Piece(Box(px - 3.5f, 128, px + 3.5f, 158), TeakDark, 2.5f, (x, y, c) => y % 7 == 0 ? Darker(c, 0.75f) : c);
            }

            // Échelle vers le lagon, sous la piscine.
            cv.Piece(Union(Box(124, 128, 127, 162), Box(136, 128, 139, 162)), Teak, 1f);
            for (int ry = 134; ry < 162; ry += 6)
            {
                int rung = ry;
                cv.Piece(Box(124, rung, 139, rung + 2), Teak, 1f);
            }

            // Large terrasse et son chant.
            cv.Piece(Box(2, 106, 158, 126), Teak, 3f, (x, y, c) => x % 7 == 0 ? Darker(c, 0.74f) : (x * 3 + y) % 13 == 0 ? Darker(c, 0.9f) : c);
            cv.Piece(Box(2, 125, 158, 135), TeakDark, 2.5f, (x, y, c) => x % 14 == 0 ? Darker(c, 0.7f) : c);

            // Piscine privée sur la terrasse, rebord de pierre claire ; transat à gauche.
            cv.Piece(Box(112, 106, 156, 124), Coral, 2f);
            cv.Piece(Box(115, 109, 153, 122), Pool, 1.2f, (x, y, c) => (x + y * 3) % 11 == 0 ? C(0.86f, 1f, 1f) : y > 118 ? Darker(c, 0.85f) : c);
            cv.Piece(Box(4, 110, 20, 122), C(0.96f, 0.94f, 0.88f), 2f, (x, y, c) => y % 4 == 0 ? Darker(c, 0.88f) : c);
            cv.Piece(Box(4, 104, 9, 112), C(0.96f, 0.94f, 0.88f), 2f);

            // Murs de lattes claires, baie vitrée, fenêtre, lanterne.
            cv.Piece(Box(axis - 44, 56, axis + 44, 108), Driftwood, 3f, (x, y, c) => x % 5 == 0 ? Darker(c, 0.84f) : c);
            cv.Piece(Box(axis - 36, 66, axis + 10, 108), Glass, 2f,
                (x, y, c) => x == (int)axis - 36 || x == (int)axis - 13 || x == (int)axis + 9 ? TeakDark
                    : (x - (axis - 36)) - (108 - y) / 2f is float d && d > 3 && d < 8 ? C(0.92f, 0.98f, 1f) : y > 100 ? Darker(c, 0.85f) : c);
            cv.Piece(Box(axis + 18, 68, axis + 38, 90), Glass, 2f, (x, y, c) => x == (int)axis + 28 || y == 79 ? TeakDark : c);
            cv.Piece(Box(axis + 12, 68, axis + 16, 76), C(1f, 0.84f, 0.42f), 1.2f);

            // Haut toit de chaume en croupe, centré sur les murs et qui déborde de chaque côté.
            cv.Piece((x, y) => y > 6 && y < 62 && Mathf.Abs(x - axis) < 18f + (y - 6f) * 0.76f, Thatch, 8f, ThatchTexture);
            cv.Piece((x, y) => y >= 58 && y < 68 && Mathf.Abs(x - axis) < 61f && ((int)x % 3 != 0 || y < 62), Darker(Thatch, 0.9f), 2.5f);   // frange
            cv.Piece(Box(axis - 16, 2, axis + 16, 11), Darker(Thatch, 0.7f), 2.5f);   // faîtage

            Color[,] p = cv.Finish(Ink);
            foreach (float px in posts) Ripple(p, px, 10f, 9f);
            return variant == 1 ? Mirror(p) : p;
        }

        // ---------- Maison en pierre de corail ----------

        public const int HouseWidth = 160;
        public const int HouseHeight = 160;
        public static readonly Vector2 HousePivot = new Vector2(0.5f, 6f / HouseHeight);

        /// <summary>
        /// Maison des Maldives (5 × 5 cases, comme les villas sur l'eau) : murs de pierre de corail chaulés (pores), frise de
        /// corail sculpté sous le toit, soubassement gris, porte à deux battants sous un auvent, deux fenêtres à volets
        /// turquoise et jardinières, murets de corail de part et d'autre, joali (siège de cordes) devant le muret de droite ;
        /// murs et toit centrés sur le même axe. <paramref name="variant"/> 0 : haut toit de feuilles de cocotier tressées,
        /// 1 : toit de tôle ondulée rouge.
        /// </summary>
        public static Color[,] House(int variant)
        {
            const float axis = 80f;
            var cv = new VolumeCanvas(HouseWidth, HouseHeight);
            System.Func<int, int, Color, Color> pores = (x, y, c) =>
            {
                int h = (x * 7 + y * 13) % 23;
                return h == 0 ? Darker(c, 0.78f) : h == 5 ? Darker(c, 0.88f) : y > 142 ? Darker(c, 0.82f) : c;
            };

            // Murets de corail.
            cv.Piece(Box(0, 132, 20, 152), Coral, 2.5f, pores);
            cv.Piece(Box(140, 132, 160, 152), Coral, 2.5f, pores);

            // Murs, frise sculptée, soubassement.
            cv.Piece(Box(18, 76, 142, 152), Coral, 3f, pores);
            cv.Piece(Box(18, 78, 142, 85), C(0.88f, 0.86f, 0.80f), 1.5f, (x, y, c) => (x + (y % 2) * 3) % 6 == 0 ? Darker(c, 0.75f) : c);
            cv.Piece(Box(18, 144, 142, 152), C(0.72f, 0.72f, 0.70f), 1.5f, (x, y, c) => x % 12 == 0 ? Darker(c, 0.8f) : c);

            // Porte à deux battants sous un auvent.
            cv.Piece(Box(axis - 16, 104, axis + 16, 152), Turquoise, 2f,
                (x, y, c) => x == (int)axis ? Darker(c, 0.7f) : (x - (int)axis) % 6 == 0 ? Darker(c, 0.86f) : y == 114 ? Darker(c, 0.8f) : c);
            cv.Dot((int)axis - 3, 130, C(0.96f, 0.84f, 0.40f));
            cv.Dot((int)axis + 2, 130, C(0.96f, 0.84f, 0.40f));
            cv.Piece(Box(axis - 24, 94, axis + 24, 104), variant == 1 ? C(0.74f, 0.20f, 0.16f) : C(0.66f, 0.56f, 0.40f), 3f, (x, y, c) => x % 4 == 0 ? Darker(c, 0.8f) : c);

            // Deux fenêtres à volets turquoise, symétriques, et leurs jardinières.
            foreach (float wc in new[] { axis - 40f, axis + 40f })
            {
                int wx = (int)wc;
                cv.Piece(Box(wx - 9, 98, wx + 9, 130), C(0.16f, 0.14f, 0.14f), 1f, (x, y, c) => x == wx || y == 114 ? C(0.36f, 0.30f, 0.26f) : c);
                cv.Piece(Box(wx - 17, 96, wx - 8, 132), Turquoise, 1.5f, (x, y, c) => y % 4 == 0 ? Darker(c, 0.8f) : c);
                cv.Piece(Box(wx + 8, 96, wx + 17, 132), Turquoise, 1.5f, (x, y, c) => y % 4 == 0 ? Darker(c, 0.8f) : c);
                cv.Piece(Box(wx - 12, 130, wx + 12, 134), Coral, 1.5f);   // appui
                cv.Piece(Box(wx - 9, 134, wx + 9, 141), C(0.70f, 0.40f, 0.26f), 1.5f);   // jardinière
                for (int f = 0; f < 4; f++) cv.Piece(Ellipse(wx - 7 + f * 4.7f, 133f, 2.2f, 2.2f), f % 2 == 0 ? C(0.92f, 0.20f, 0.24f) : C(0.98f, 0.82f, 0.30f), 1f);
            }

            // Haut toit à quatre pans qui déborde, centré sur les murs.
            System.Func<float, float, bool> roof = (x, y) => y > 12 && y < 82 && Mathf.Abs(x - axis) < 40f + (y - 12f) * 0.52f;
            if (variant == 1)
            {
                cv.Piece(roof, C(0.74f, 0.20f, 0.16f), 6f, (x, y, c) => x % 5 == 0 ? Darker(c, 0.72f) : x % 5 == 1 ? Darker(c, 1.15f) : (x * 3 + y * 5) % 37 == 0 ? C(0.56f, 0.30f, 0.22f) : c);
                cv.Piece(Box(axis - 44, 7, axis + 44, 15), C(0.62f, 0.62f, 0.64f), 2f);
            }
            else
            {
                cv.Piece(roof, C(0.66f, 0.56f, 0.40f), 7f, (x, y, c) => ((x / 3) + (y / 3)) % 2 == 0 ? Darker(c, 0.84f) : (x * 5 + y) % 11 == 0 ? Darker(c, 1.12f) : y % 10 == 0 ? Darker(c, 0.78f) : c);
                cv.Piece(Box(axis - 44, 7, axis + 44, 16), C(0.52f, 0.42f, 0.30f), 2.5f);
            }

            // Joali : siège de cordes tendues sur un cadre de bois, devant le muret de droite.
            cv.Piece(Union(Box(141, 126, 144, 158), Box(155, 126, 158, 158), Box(140, 124, 159, 128)), TeakDark, 1.5f);
            cv.Piece((x, y) => x > 143 && x < 156 && y > 136 && y < 146 + Mathf.Sin((x - 143f) / 13f * Mathf.PI) * 4f, C(0.92f, 0.82f, 0.56f), 1.5f,
                (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.78f) : c);

            Color[,] p = cv.Finish(Ink);
            Shadow(p, axis, 5f, 76f, 5f, C(0.06f, 0.14f, 0.08f, 0.3f));
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
