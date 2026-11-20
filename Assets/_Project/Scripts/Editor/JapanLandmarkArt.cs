using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les repères de la baie d'Izu, dans le style en volume (32 px par case, y depuis le haut pendant le dessin) :
    /// sanctuaire shinto, torii (dans l'eau ou sur l'allée), maisons de pêcheurs, barque, onsen fumant, rochers mariés,
    /// mont Fuji ; et le pont rouge (16 px par case, comme le sol).
    /// </summary>
    public static class JapanLandmarkArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Vermilion = C(0.86f, 0.24f, 0.16f);
        private static readonly Color Black = C(0.14f, 0.12f, 0.12f);
        private static readonly Color Wood = C(0.46f, 0.32f, 0.20f);
        private static readonly Color WoodDark = C(0.28f, 0.18f, 0.12f);
        private static readonly Color Plaster = C(0.94f, 0.92f, 0.86f);
        private static readonly Color Tile = C(0.30f, 0.34f, 0.40f);
        private static readonly Color Stone = C(0.62f, 0.62f, 0.60f);
        private static readonly Color StoneDark = C(0.40f, 0.40f, 0.42f);
        private static readonly Color Rope = C(0.86f, 0.76f, 0.50f);
        private static readonly Color Paper = C(0.98f, 0.98f, 0.96f);
        private static readonly Color Moss = C(0.34f, 0.50f, 0.28f);
        private static readonly Color Ink = C(0.10f, 0.08f, 0.08f);

        private static void GroundShadow(Color[,] p, int rows, float alpha = 0.3f)
        {
            for (int x = 2; x < p.GetLength(0) - 2; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (p[x, y].a < 0.01f) p[x, y] = C(0.04f, 0.10f, 0.06f, alpha);
                }
            }
        }

        /// <summary>Toit japonais à pans recourbés : bord bas relevé aux deux bouts, tuiles en rangs.</summary>
        private static void CurvedRoof(VolumeCanvas cv, float cx, float top, float bottom, float halfTop, float halfBottom, Color color, float lift)
        {
            cv.Piece((x, y) =>
            {
                if (y < top || y > bottom + lift) return false;
                float t = (y - top) / (bottom - top);
                float half = Mathf.Lerp(halfTop, halfBottom, Mathf.Clamp01(t));
                float dx = Mathf.Abs(x - cx) / halfBottom;
                float edge = bottom - lift * dx * dx * dx;   // les coins se relèvent
                return Mathf.Abs(x - cx) < half && y < edge;
            }, color, 4f, (x, y, c) => ((int)x + (int)(y / 5) * 3) % 6 == 0 ? Darker(c, 0.72f) : (int)y % 5 == 0 ? Darker(c, 1.18f) : c);
        }

        // ---------- Sanctuaire ----------

        public const int ShrineWidth = 192;
        public const int ShrineHeight = 176;
        public static readonly Vector2 ShrinePivot = new Vector2(0.5f, 6f / ShrineHeight);

        /// <summary>
        /// Sanctuaire shinto : soubassement de pierre et marches, piliers vermillon, murs blancs, portes à claire-voie,
        /// grand toit sombre aux coins relevés, chigi croisés au faîte, corde shimenawa et papiers shide, tronc à
        /// offrandes, deux chiens-lions de pierre.
        /// </summary>
        public static Color[,] Shrine()
        {
            var cv = new VolumeCanvas(ShrineWidth, ShrineHeight);
            cv.Piece(Box(20, 146, 172, 166), Stone, 2.5f, (x, y, c) => x % 16 == 0 || y == 156 ? StoneDark : c);
            cv.Piece(Box(76, 158, 116, 172), Stone, 2f, (x, y, c) => (int)y % 4 == 0 ? StoneDark : c);   // marches
            cv.Piece(Box(28, 92, 164, 148), Plaster, 2.5f);
            // Portes à claire-voie au milieu, piliers et poutres vermillon.
            cv.Piece(Box(70, 100, 122, 148), Wood, 2f, (x, y, c) => (int)x % 6 == 0 || (int)y % 8 == 0 ? WoodDark : C(0.20f, 0.14f, 0.10f));
            foreach (int px in new[] { 30, 66, 126, 162 })
            {
                cv.Piece(Box(px - 4, 90, px + 4, 150), Vermilion, 3f);
            }

            cv.Piece(Box(24, 86, 168, 96), Vermilion, 2.5f);
            // Tronc à offrandes et chiens-lions.
            cv.Piece(Box(84, 136, 108, 152), Wood, 2.5f, (x, y, c) => (int)y % 4 == 0 && y < 142 ? WoodDark : c);
            foreach (int lx in new[] { 8, 184 })
            {
                cv.Piece(Box(lx - 7, 150, lx + 7, 166), Stone, 2f);
                cv.Piece(Union(Ellipse(lx, 140f, 6f, 9f), Ellipse(lx, 130f, 5.5f, 5f)), Stone, 4f, (x, y, c) => Mathf.Abs(y - 128f) < 1f && Mathf.Abs(x - lx) < 3f ? StoneDark : c);
            }

            // Grand toit, faîtage, chigi croisés.
            CurvedRoof(cv, 96f, 26f, 90f, 30f, 94f, Tile, 14f);
            cv.Piece(Box(62, 20, 130, 30), Black, 2.5f);
            foreach (int sx in new[] { 62, 130 })
            {
                cv.Piece(Capsule(sx, 26f, sx - (sx < 96 ? 10f : -10f), 6f, 2f), WoodDark, 1.5f);
                cv.Piece(Capsule(sx, 26f, sx + (sx < 96 ? 10f : -10f), 6f, 2f), WoodDark, 1.5f);
            }

            // Shimenawa : grosse corde tressée qui pend en guirlande, papiers shide en zigzag.
            cv.Piece((x, y) => x > 40 && x < 152 && Mathf.Abs(y - (98f + Mathf.Sin((x - 40f) / 112f * Mathf.PI) * 8f)) < 3.2f, Rope, 2.5f,
                (x, y, c) => ((int)x + (int)y) % 5 == 0 ? Darker(c, 0.75f) : c);
            foreach (int sx in new[] { 60, 96, 132 })
            {
                float sy = 98f + Mathf.Sin((sx - 40f) / 112f * Mathf.PI) * 8f;
                cv.Piece((x, y) => y > sy + 2f && y < sy + 18f && Mathf.Abs(x - (sx + (((int)((y - sy) / 4f)) % 2 == 0 ? -1.5f : 1.5f))) < 2f, Paper, 1f);
            }

            Color[,] p = cv.Finish(Ink);
            GroundShadow(p, 6);
            return p;
        }

        // ---------- Torii ----------

        public const int ToriiWidth = 160;
        public const int ToriiHeight = 150;
        public static readonly Vector2 ToriiPivot = new Vector2(0.5f, 8f / ToriiHeight);

        /// <summary>
        /// Grand torii vermillon : deux piliers à socle noir, kasagi noir recourbé, nuki, plaque au milieu.
        /// <paramref name="inWater"/> : piliers dans l'eau (reflets et ronds) au lieu de l'ombre au sol.
        /// </summary>
        public static Color[,] Torii(bool inWater)
        {
            var cv = new VolumeCanvas(ToriiWidth, ToriiHeight);
            foreach (float px in new[] { 38f, 122f })
            {
                cv.Piece((x, y) => y > 30 && y < 140 && Mathf.Abs(x - px) < 7f - (140f - y) * 0.012f, Vermilion, 5f);
                cv.Piece(Box(px - 9, 128, px + 9, 142), Black, 3f);
            }

            cv.Piece(Box(16, 50, 144, 60), Vermilion, 3f);                                                   // nuki
            cv.Piece(Box(72, 28, 88, 52), Vermilion, 2.5f);                                                   // tige de la plaque
            cv.Piece(Box(68, 34, 92, 50), Black, 2.5f, (x, y, c) => Mathf.Abs(x - 80f) < 4f && (int)y % 4 != 0 ? C(0.86f, 0.74f, 0.32f) : c);
            cv.Piece((x, y) => y > 18 && y < 32 && Mathf.Abs(x - 80f) < 72f, Vermilion, 4f);                 // shimaki
            cv.Piece((x, y) =>
            {
                float dx = (x - 80f) / 80f;
                float bottom = 20f - dx * dx * 10f;
                return Mathf.Abs(x - 80f) < 80f && y > bottom - 9f && y < bottom;
            }, Black, 4f);                                                                                    // kasagi recourbé
            Color[,] p = cv.Finish(Ink);
            if (inWater)
            {
                foreach (float px in new[] { 38f, 122f })
                {
                    for (int x = 0; x < ToriiWidth; x++)
                    {
                        for (int y = 0; y < 14; y++)
                        {
                            float u = (x + 0.5f - px) / 15f, v = (y + 0.5f - 8f) / 4f;
                            float d = u * u + v * v;
                            if (d <= 1f && p[x, y].a < 0.01f) p[x, y] = d > 0.7f && (x / 2) % 3 != 0 ? C(0.86f, 0.94f, 0.98f, 0.6f) : C(0.60f, 0.12f, 0.10f, 0.35f);
                        }
                    }
                }
            }
            else
            {
                GroundShadow(p, 8, 0.22f);
            }

            return p;
        }

        // ---------- Maison de pêcheur ----------

        public const int HouseWidth = 128;
        public const int HouseHeight = 128;
        public static readonly Vector2 HousePivot = new Vector2(0.5f, 6f / HouseHeight);

        /// <summary>
        /// Maison de pêcheur (minka) : bois sombre et enduit blanc, shoji, noren bleu à la porte, toit de tuiles grises ;
        /// <paramref name="variant"/> 1 : toit de chaume épais et filets qui sèchent.
        /// </summary>
        public static Color[,] House(int variant)
        {
            var cv = new VolumeCanvas(HouseWidth, HouseHeight);
            cv.Piece(Box(10, 112, 118, 124), Stone, 2f, (x, y, c) => (int)x % 14 == 0 ? StoneDark : c);
            cv.Piece(Box(14, 58, 114, 116), Plaster, 2.5f, (x, y, c) => (int)x % 25 < 3 || y > 100 ? Wood : c);
            cv.Piece(Box(14, 58, 114, 66), Wood, 2f);
            // Shoji à gauche, porte coulissante et noren à droite.
            cv.Piece(Box(24, 72, 58, 98), C(0.96f, 0.94f, 0.84f), 1.5f, (x, y, c) => (int)(x - 24) % 8 == 0 || (int)(y - 72) % 6 == 0 ? WoodDark : c);
            cv.Piece(Box(72, 70, 104, 116), C(0.20f, 0.14f, 0.10f), 1.5f);
            cv.Piece(Box(70, 70, 106, 88), C(0.20f, 0.30f, 0.56f), 2f, (x, y, c) => (int)x % 9 == 0 ? Darker(c, 0.6f) : Mathf.Abs(x - 88f) < 5f && Mathf.Abs(y - 79f) < 4f ? Paper : c);
            if (variant == 1)
            {
                cv.Piece((x, y) => y > 10 && y < 62 && Mathf.Abs(x - 64f) < 62f - (62f - y) * 0.8f, C(0.66f, 0.58f, 0.38f), 7f,
                    (x, y, c) => ((int)x + (int)y / 3) % 5 == 0 ? Darker(c, 0.72f) : (int)y % 9 == 0 ? Darker(c, 0.86f) : c);
                cv.Piece(Box(36, 8, 92, 16), C(0.40f, 0.34f, 0.24f), 2.5f);
                cv.Piece((x, y) => x > 4 && x < 30 && y > 66 && y < 108 && (((int)x + (int)y) % 4 == 0 || ((int)x - (int)y + 200) % 4 == 0), C(0.30f, 0.30f, 0.26f), 0.5f);   // filet
            }
            else
            {
                CurvedRoof(cv, 64f, 14f, 60f, 26f, 64f, Tile, 8f);
                cv.Piece(Box(36, 10, 92, 18), Darker(Tile, 0.8f), 2.5f);
            }

            Color[,] p = cv.Finish(Ink);
            GroundShadow(p, 6);
            return p;
        }

        // ---------- Barque ----------

        public const int BoatWidth = 112;
        public const int BoatHeight = 52;

        /// <summary>Barque de pêche en bois tirée sur le sable : coque claire, bordé sombre, rame, petit filet roulé.</summary>
        public static Color[,] Boat()
        {
            var cv = new VolumeCanvas(BoatWidth, BoatHeight);
            System.Func<float, float, bool> hull = (x, y) =>
            {
                float t = (x - 56f) / 52f;
                float half = 14f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(Mathf.Abs(t), 3f)));
                return Mathf.Abs(t) < 1f && Mathf.Abs(y - 24f) < half;
            };
            cv.Piece(hull, C(0.70f, 0.56f, 0.38f), 5f, (x, y, c) => Mathf.Abs(y - 24f) > 10f ? WoodDark : (int)y % 5 == 0 ? Darker(c, 0.85f) : c);
            cv.Piece((x, y) => hull(56f + (x - 56f) * 1.1f, 24f + (y - 24f) * 1.5f), C(0.56f, 0.42f, 0.28f), 3f);
            cv.Piece(Box(40, 14, 44, 34), WoodDark, 1.5f);
            cv.Piece(Box(70, 14, 74, 34), WoodDark, 1.5f);
            cv.Piece(Capsule(24f, 30f, 92f, 20f, 1.6f), C(0.80f, 0.66f, 0.44f), 1.5f);
            cv.Piece(Ellipse(56f, 26f, 8f, 5f), C(0.36f, 0.36f, 0.30f), 3f, (x, y, c) => ((int)x + (int)y) % 3 == 0 ? Darker(c, 0.7f) : c);
            Color[,] p = cv.Finish(Ink);
            for (int x = 6; x < BoatWidth - 6; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    float u = (x + 0.5f - 58f) / 52f, v = (y + 0.5f - 6f) / 5f;
                    if (u * u + v * v <= 1f && p[x, y].a < 0.01f) p[x, y] = C(0.10f, 0.10f, 0.12f, 0.3f);
                }
            }

            return p;
        }

        // ---------- Onsen ----------

        public const int OnsenWidth = 176;
        public const int OnsenHeight = 136;
        public static readonly Vector2 OnsenPivot = new Vector2(0.5f, 6f / OnsenHeight);

        /// <summary>
        /// Onsen en plein air : palissade de bambou au fond, couronne de gros rochers, eau laiteuse turquoise, volutes de
        /// vapeur, seau de bois et petite lanterne.
        /// </summary>
        public static Color[,] Onsen()
        {
            var cv = new VolumeCanvas(OnsenWidth, OnsenHeight);
            cv.Piece(Box(10, 6, 166, 62), C(0.62f, 0.66f, 0.36f), 2f, (x, y, c) => (int)x % 8 < 1 ? Darker(c, 0.6f) : (int)y % 18 == 0 ? C(0.30f, 0.26f, 0.18f) : c);   // palissade
            cv.Piece(Box(10, 2, 166, 8), C(0.30f, 0.26f, 0.18f), 2f);
            cv.Piece(Ellipse(88f, 88f, 82f, 40f), StoneDark, 3f);
            cv.Piece(Ellipse(88f, 90f, 66f, 30f), C(0.62f, 0.86f, 0.86f), 3f,
                (x, y, c) => Mathf.Abs(Mathf.Sin(x * 0.18f + y * 0.4f)) > 0.94f ? C(0.88f, 0.98f, 0.98f) : y < 74 ? Darker(c, 0.86f) : c);
            var random = new System.Random(31);
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f;
                float rx = 88f + Mathf.Cos(a) * 74f, ry = 88f + Mathf.Sin(a) * 35f;
                float r = 8f + random.Next(0, 6);
                cv.Piece(Ellipse(rx, ry, r, r * 0.8f), i % 3 == 0 ? Stone : Darker(Stone, 0.85f), r * 0.6f,
                    (x, y, c) => y < ry - r * 0.4f && (x + y) % 5 == 0 ? Moss : c);
            }

            // Seau de bois et lanterne sur les rochers de devant.
            cv.Piece(Box(130, 104, 146, 122), C(0.72f, 0.56f, 0.34f), 2.5f, (x, y, c) => (int)y % 6 == 0 ? WoodDark : c);
            cv.Piece(Box(28, 96, 40, 122), Stone, 2f);
            cv.Piece(Box(24, 86, 44, 98), Stone, 2f, (x, y, c) => Mathf.Abs(x - 34f) < 4f && y > 89 && y < 96 ? C(1f, 0.86f, 0.50f) : c);
            cv.Piece((x, y) => y > 78 && y < 88 && Mathf.Abs(x - 34f) < 14f - (y - 78f) * 0.2f && y > 78 + Mathf.Abs(x - 34f) * 0.5f, StoneDark, 2f);

            // Volutes de vapeur, semi-transparentes.
            Color[,] p = cv.Finish(Ink);
            foreach ((float sx, float sy, float sr) in new[] { (60f, 60f, 12f), (88f, 52f, 15f), (118f, 62f, 11f), (74f, 36f, 10f), (104f, 30f, 9f) })
            {
                for (int x = 0; x < OnsenWidth; x++)
                {
                    for (int y = 0; y < OnsenHeight; y++)
                    {
                        float yTop = OnsenHeight - 1 - y;   // p est en y depuis le bas
                        float u = (x + 0.5f - sx) / sr, v = (yTop + 0.5f - sy) / (sr * 0.7f);
                        float d = u * u + v * v;
                        if (d > 1f) continue;
                        float alpha = 0.45f * (1f - d);
                        p[x, y] = Color.Lerp(p[x, y].a > 0.01f ? p[x, y] : C(1f, 1f, 1f, 0f), C(1f, 1f, 1f, 1f), alpha);
                        p[x, y].a = Mathf.Max(p[x, y].a, alpha);
                    }
                }
            }

            GroundShadow(p, 6);
            return p;
        }

        // ---------- Rochers mariés ----------

        public const int RocksWidth = 144;
        public const int RocksHeight = 112;

        /// <summary>Deux rochers dans la mer, le grand coiffé d'un petit torii, reliés par une grosse corde à papiers shide.</summary>
        public static Color[,] WeddedRocks()
        {
            var cv = new VolumeCanvas(RocksWidth, RocksHeight);
            Color rock = C(0.44f, 0.42f, 0.42f);
            System.Func<int, int, Color, Color> strata = (x, y, c) => (x * 2 + y * 3) % 13 == 0 ? Darker(c, 0.75f) : y < 40 && (x + y) % 7 == 0 ? Moss : c;
            cv.Piece(Union(Ellipse(44f, 64f, 30f, 36f), Ellipse(40f, 88f, 36f, 16f)), rock, 12f, strata);
            cv.Piece(Union(Ellipse(110f, 78f, 22f, 24f), Ellipse(112f, 92f, 26f, 12f)), Darker(rock, 0.95f), 10f, strata);
            cv.Piece(Box(36, 20, 52, 32), Vermilion, 2f);                                     // petit torii au sommet
            cv.Piece(Box(32, 16, 56, 21), Black, 2f);
            cv.Piece((x, y) => x > 60 && x < 100 && Mathf.Abs(y - (54f + Mathf.Sin((x - 60f) / 40f * Mathf.PI) * 6f)) < 3.6f, Rope, 2.5f,
                (x, y, c) => ((int)x + (int)y) % 5 == 0 ? Darker(c, 0.75f) : c);
            foreach (int sx in new[] { 70, 80, 90 })
            {
                float sy = 54f + Mathf.Sin((sx - 60f) / 40f * Mathf.PI) * 6f;
                cv.Piece((x, y) => y > sy + 2f && y < sy + 13f && Mathf.Abs(x - (sx + (((int)((y - sy) / 3f)) % 2 == 0 ? -1f : 1f))) < 1.6f, Paper, 1f);
            }

            Color[,] p = cv.Finish(Ink, 0.5f);
            // Écume au pied des rochers (y depuis le bas).
            for (int x = 0; x < RocksWidth; x++)
            {
                for (int y = 0; y < 22; y++)
                {
                    bool foam = (Mathf.Abs((x - 40f) / 42f) < 1f || Mathf.Abs((x - 112f) / 30f) < 1f) && (x / 3 + y) % 4 == 0;
                    if (p[x, y].a < 0.01f && foam && y > 6) p[x, y] = C(0.92f, 0.98f, 1f, 0.7f);
                }
            }

            return p;
        }

        // ---------- Mont Fuji ----------

        public const int FujiWidth = 416;
        public const int FujiHeight = 224;
        public static readonly Vector2 FujiPivot = new Vector2(0.5f, 0f);

        /// <summary>Mont Fuji au loin : cône régulier bleuté au sommet tronqué, neige en dents le long des ravines.</summary>
        public static Color[,] Fuji()
        {
            var cv = new VolumeCanvas(FujiWidth, FujiHeight);
            Color slope = C(0.42f, 0.48f, 0.62f);
            cv.Piece((x, y) =>
            {
                float t = (y - 20f) / (FujiHeight - 20f);
                float half = 26f + Mathf.Pow(Mathf.Clamp01(t), 1.6f) * 182f;
                return y > 20f && Mathf.Abs(x - 208f) < half;
            }, slope, 8f, (x, y, c) =>
            {
                float t = (y - 20f) / (FujiHeight - 20f);
                float ravine = Mathf.Sin(x * 0.16f) * 0.06f + Mathf.Sin(x * 0.05f + 1f) * 0.04f;
                bool snow = t < 0.36f + ravine + (Mathf.Abs(Mathf.Sin(x * 0.22f)) > 0.75f ? 0.08f : 0f);
                bool shadow = x > 208f + (y - 20f) * 0.2f;
                Color baseColor = snow ? C(0.96f, 0.97f, 1f) : Color.Lerp(slope, C(0.36f, 0.48f, 0.42f), t * t);
                return shadow ? Darker(baseColor, snow ? 0.86f : 0.8f) : baseColor;
            }, 0.6f, 0.5f);
            // Nuage étiré en travers du flanc.
            cv.Piece(Union(Ellipse(120f, 150f, 50f, 9f), Ellipse(160f, 144f, 34f, 10f), Ellipse(300f, 160f, 56f, 8f)), C(0.96f, 0.96f, 0.98f), 5f);
            return cv.Finish(C(0.22f, 0.24f, 0.34f), 0.35f);
        }

        // ---------- Pont rouge ----------

        /// <summary>Pont japonais vermillon (16 px par case) : tablier de bois, rambardes rouges à boules de bronze.</summary>
        public static Color[,] RedBridge(int cells, int rows)
        {
            int w = cells * 16 + 6, h = rows * 16 + 8;
            var p = new Color[w, h];
            Color deck = C(0.58f, 0.42f, 0.28f), deckDark = C(0.40f, 0.28f, 0.18f), brass = C(0.86f, 0.70f, 0.30f);
            for (int x = 0; x < w; x++)
            {
                for (int y = 3; y < h - 4; y++) p[x, y] = x % 5 == 4 ? deckDark : deck;
                foreach (int ry in new[] { 2, h - 4 })
                {
                    p[x, ry] = Vermilion;
                    p[x, ry + 1] = Darker(Vermilion, 1.15f);
                }

                p[x, 1] = Darker(Vermilion, 0.7f);
                p[x, h - 2] = Darker(Vermilion, 0.7f);
                if (x % 12 == 3 || x == w - 3)
                {
                    for (int y = 0; y < h; y++)
                    {
                        if (y < 5 || y > h - 6) p[x, y] = Vermilion;
                    }

                    PixelArt.Set(p, x, h - 1, brass);
                    PixelArt.Set(p, x, 4, brass);
                }
            }

            for (int x = 0; x < w; x++) p[x, 0] = C(0.05f, 0.12f, 0.18f, 0.35f);
            return p;
        }
    }
}
