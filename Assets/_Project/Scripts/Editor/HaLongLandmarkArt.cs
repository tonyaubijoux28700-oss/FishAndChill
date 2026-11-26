using System;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les repères de la baie d'Ha Long, dans le style en volume (32 px par case, y depuis le haut pendant le dessin) :
    /// pitons calcaires qui sortent de l'eau (encoche d'érosion au ras de l'eau, coulures, végétation accrochée aux
    /// corniches), pans de montagne de calcaire couverts de jungle et grotte, jonque aux voiles rouges lattées, maison
    /// flottante sur radeau de bidons, cage à poissons, maison de village jaune au toit de tuiles, temple au double toit
    /// recourbé (dragons et perle sur le faîte), bateau-panier de bambou tressé.
    /// </summary>
    public static class HaLongLandmarkArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Limestone = C(0.68f, 0.66f, 0.60f);
        private static readonly Color Jungle = C(0.20f, 0.40f, 0.16f);
        private static readonly Color JungleLight = C(0.38f, 0.58f, 0.22f);
        private static readonly Color Wood = C(0.46f, 0.30f, 0.18f);
        private static readonly Color WoodDark = C(0.24f, 0.15f, 0.09f);
        private static readonly Color Sail = C(0.70f, 0.26f, 0.14f);
        private static readonly Color Tile = C(0.56f, 0.24f, 0.16f);
        private static readonly Color Lacquer = C(0.62f, 0.12f, 0.10f);
        private static readonly Color Gold = C(0.86f, 0.68f, 0.28f);
        private static readonly Color Ink = C(0.05f, 0.06f, 0.05f);
        private static readonly Color WaterShadow = C(0.02f, 0.10f, 0.08f, 0.45f);
        private static readonly Color Foam = C(0.86f, 0.94f, 0.90f);

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        private static void Shadow(Color[,] p, float cx, float cy, float rx, float ry, Color color)
        {
            for (int x = 0; x < p.GetLength(0); x++)
            {
                for (int y = 0; y < p.GetLength(1); y++)
                {
                    float u = (x - cx) / rx, v = (y - cy) / ry;
                    if (u * u + v * v > 1f || p[x, y].a > 0.5f) continue;
                    p[x, y] = color;
                }
            }
        }

        /// <summary>Calcaire : coulures verticales sombres, taches de rouille, végétation sur les corniches.</summary>
        private static Color Karst(int x, int y, Color c, float seed, float top, float bottom, float greenery)
        {
            float k = Mathf.InverseLerp(top, bottom, y);   // 0 en haut, 1 au pied
            float n = Mathf.PerlinNoise(x * 0.07f + seed, y * 0.05f);
            Color s = Color.Lerp(Darker(c, 0.78f), Darker(c, 1.12f), n);
            float streak = Mathf.PerlinNoise(x * 0.22f + seed, y * 0.012f);
            if (streak > 0.64f) s = Darker(s, 0.72f);                                                   // coulures de pluie
            if (Mathf.PerlinNoise(x * 0.05f, y * 0.09f + seed) > 0.74f) s = Color.Lerp(s, C(0.70f, 0.50f, 0.32f), 0.4f);   // rouille
            if (Mathf.Repeat(y + Mathf.PerlinNoise(x * 0.04f, seed) * 24f, 36f) < 2.2f) s = Darker(s, 0.6f);   // corniches
            // La jungle : dense au sommet, en touffes sur les corniches plus bas.
            float leaf = Mathf.PerlinNoise(x * 0.11f + seed * 2f, y * 0.11f);
            float cover = greenery * (1.2f - k * 1.6f) + (Mathf.Repeat(y + Mathf.PerlinNoise(x * 0.04f, seed) * 24f, 36f) < 8f ? 0.25f : 0f);
            if (leaf < cover) s = Hash(x / 2, y / 2, 7) < 0.3f ? JungleLight : Hash(x, y, 8) < 0.15f ? Darker(Jungle, 0.7f) : Jungle;
            return Color.Lerp(s, Darker(s, 0.6f), Mathf.Clamp01((k - 0.6f) * 1.5f));
        }

        // ---------- Pitons ----------

        public const int PitonWidth = 192;
        public const int PitonHeight = 288;
        public static readonly Vector2 PitonPivot = new Vector2(0.5f, 14f / PitonHeight);

        /// <summary>
        /// Piton calcaire qui sort de l'eau, pied au milieu de la base : 0 aiguille haute, 1 double sommet, 2 dôme trapu.
        /// Encoche d'érosion sombre au ras de l'eau, écume et reflet sombre dans l'eau.
        /// </summary>
        public static Color[,] Piton(int variant)
        {
            var cv = new VolumeCanvas(PitonWidth, PitonHeight);
            float seed = variant * 41f + 3f;
            float baseY = PitonHeight - 16f, cx = PitonWidth * 0.5f;
            Func<float, float> half;
            float top;
            switch (variant)
            {
                case 1:
                    top = 40f;
                    half = y =>
                    {
                        float k = (y - top) / (baseY - top);
                        float lobes = Mathf.Max(0f, 1f - Mathf.Pow((y - 60f) / 40f, 2f)) * 18f;
                        return 46f + k * 34f - lobes * 0.2f + Mathf.Sin(y * 0.05f + seed) * 5f;
                    };
                    break;
                case 2:
                    top = 120f;
                    half = y => 78f * Mathf.Sqrt(Mathf.Clamp01((y - top) / 60f)) + Mathf.Sin(y * 0.07f + seed) * 4f;
                    break;
                default:
                    top = 14f;
                    half = y => 18f + Mathf.Pow((y - top) / (baseY - top), 0.8f) * 48f + Mathf.Sin(y * 0.06f + seed) * 5f;
                    break;
            }

            Func<float, float, bool> body = (x, y) =>
            {
                if (y < top || y > baseY) return false;
                float h = half(y);
                // Sommet arrondi, avec deux bosses pour la variante double.
                float crown = variant == 1 ? top + 18f * Mathf.Abs(Mathf.Sin((x - cx) / 40f * Mathf.PI * 0.5f + 0.2f)) : top + Mathf.Pow(Mathf.Abs(x - cx) / Mathf.Max(1f, half(top + 30f)), 2f) * 26f;
                if (y < crown) return false;
                // L'encoche d'érosion : le pied rentre un peu sous la paroi.
                if (y > baseY - 12f) h -= 6f * (y - (baseY - 12f)) / 12f;
                return Mathf.Abs(x - cx) < h;
            };
            cv.Piece(body, Limestone, 18f, (x, y, c) =>
            {
                Color k = Karst(x, y, c, seed, top, baseY, variant == 2 ? 0.9f : 0.75f);
                if (y > baseY - 12f) k = Color.Lerp(k, C(0.14f, 0.16f, 0.12f), 0.7f);   // l'encoche, à l'ombre
                return k;
            }, 0.85f, 0.4f);

            // Quelques arbustes qui débordent du sommet et des corniches.
            for (int i = 0; i < 9; i++)
            {
                float y = top + 6f + Hash(i, variant, 3) * (baseY - top) * 0.6f;
                float side = Hash(i, variant, 4) < 0.5f ? -1f : 1f;
                float x = cx + side * (half(y) - 3f);
                float r = 5f + Hash(i, variant, 5) * 6f;
                cv.Piece(Ellipse(x, y, r, r * 0.75f), Jungle, 4f, (px, py, c) => Hash(px / 2, py / 2, 11) < 0.3f ? JungleLight : c);
            }

            Color[,] p = cv.Finish(Ink, 0.5f);
            // Écume au pied et reflet sombre dans l'eau.
            float footHalf = half(baseY) - 6f;
            for (int x = 0; x < PitonWidth; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    float u = (x - cx) / (footHalf + 10f);
                    if (Mathf.Abs(u) > 1f || p[x, y].a > 0.5f) continue;
                    bool rim = y >= 13 && Hash(x / 2, y, 21) < 0.7f;
                    p[x, y] = rim ? Foam : y > 8 - Mathf.Abs(u) * 6f ? WaterShadow : C(0, 0, 0, 0);
                }
            }

            return p;
        }

        // ---------- Montagnes du nord et grotte ----------

        public const int KarstWidth = 256;
        public const int KarstHeight = 256;
        public static readonly Vector2 KarstPivot = new Vector2(0.5f, 6f / KarstHeight);

        /// <summary>
        /// Pan de montagne de calcaire couvert de jungle, en dômes ; <paramref name="cave"/> : la paroi s'ouvre au pied sur
        /// une grotte sombre aux stalactites, une lanterne rouge de chaque côté.
        /// </summary>
        public static Color[,] KarstRange(int variant, bool cave = false)
        {
            var cv = new VolumeCanvas(KarstWidth, KarstHeight);
            float seed = variant * 23f + 7f;
            // Deux dômes arrondis par pan, séparés par un col, de hauteur variable.
            float lift = variant % 2 == 0 ? 0f : 18f;
            Func<float, float> top = x => 14f + lift + 70f * (1f - Mathf.Sqrt(Mathf.Abs(Mathf.Sin(x / 256f * Mathf.PI * 2f + seed * 0.1f)))) + Mathf.PerlinNoise(x * 0.04f, seed) * 14f;
            Func<float, float, bool> wall = (x, y) => y > top(x) && y < 250f
                && x > 2f + Mathf.PerlinNoise(y * 0.03f, seed) * 10f && x < 254f - Mathf.PerlinNoise(y * 0.03f + 5f, seed) * 10f;
            cv.Piece(wall, Limestone, 18f, (x, y, c) => Karst(x, y, c, seed, top(x), 250f, 1.05f), 0.85f, 0.4f);

            if (cave)
            {
                Func<float, float, bool> mouth = (x, y) => y > 168f && y < 250f && Mathf.Abs(x - 128f) < 40f * Mathf.Sqrt(Mathf.Clamp01((y - 168f) / 30f));
                cv.Piece(mouth, C(0.06f, 0.06f, 0.05f), 6f, (x, y, c) => Color.Lerp(C(0.16f, 0.14f, 0.10f), C(0.02f, 0.02f, 0.02f), Mathf.InverseLerp(250f, 180f, y)), 0.9f, 0.6f);
                for (int i = 0; i < 9; i++)
                {
                    float x = 96f + i * 8f + Hash(i, 1, 31) * 4f;
                    float len = 6f + Hash(i, 2, 31) * 12f;
                    float y0 = 172f + Mathf.Abs(x - 128f) * 0.5f;
                    cv.Piece(Poly(new Vector2(x - 2.5f, y0 - 2f), new Vector2(x + 2.5f, y0 - 2f), new Vector2(x, y0 + len)), C(0.70f, 0.66f, 0.56f), 1.5f);
                }

                foreach (float lx in new[] { 80f, 176f })
                {
                    cv.Piece(Box(lx - 1f, 196f, lx + 1f, 248f), WoodDark, 1f);
                    cv.Piece(Ellipse(lx, 194f, 6f, 8f), C(0.86f, 0.16f, 0.10f), 4f, (x, y, c) => Mathf.Abs(y - 194f) < 1.2f ? Gold : c);
                }
            }

            return cv.Finish(Ink, 0.5f);
        }

        // ---------- Jonque ----------

        public const int JunkWidth = 224;
        public const int JunkHeight = 208;
        public static readonly Vector2 JunkPivot = new Vector2(0.5f, 16f / JunkHeight);

        /// <summary>Jonque de bois sombre, poupe haute, trois mâts aux voiles rouges lattées en éventail, reflet dans l'eau.</summary>
        public static Color[,] Junk()
        {
            var cv = new VolumeCanvas(JunkWidth, JunkHeight);
            // Les voiles, derrière la coque : lattes sombres, bord libre arrondi en éventail.
            foreach ((float mast, float height, float width) in new[] { (64f, 132f, 46f), (118f, 168f, 58f), (168f, 112f, 40f) })
            {
                float foot = 160f, head = foot - height;
                cv.Piece(Box(mast - 1.6f, head - 6f, mast + 1.6f, foot + 10f), WoodDark, 1.2f);
                Func<float, float, bool> sail = (x, y) =>
                {
                    if (y < head || y > foot - 8f) return false;
                    float k = (y - head) / (foot - 8f - head);
                    float reach = width * (0.55f + 0.45f * Mathf.Sin(k * Mathf.PI * 0.9f + 0.2f));
                    return x > mast - width * 0.18f && x < mast + reach - Mathf.Abs(Mathf.Sin(k * 6f * Mathf.PI)) * 3f;
                };
                cv.Piece(sail, Sail, 6f, (x, y, c) =>
                {
                    float k = (y - head) / (foot - 8f - head);
                    if (Mathf.Repeat(k * 6f, 1f) < 0.08f) return WoodDark;                              // les lattes
                    if (Hash(x / 3, y / 3, 5) < 0.06f) return Darker(c, 0.82f);                         // toile rapiécée
                    return Color.Lerp(Darker(c, 1.1f), Darker(c, 0.78f), Mathf.InverseLerp(mast, mast + width, x));
                }, 0.85f, 0.45f);
            }

            // La coque : poupe haute à gauche, proue relevée à droite, bordages, liseré clair, hublots.
            Func<float, float, bool> hull = (x, y) =>
            {
                if (x < 14f || x > 214f) return false;
                float u = (x - 14f) / 200f;
                float deck = 150f - Mathf.Pow(1f - u, 3f) * 34f - Mathf.Pow(u, 4f) * 18f;
                float keel = 188f - Mathf.Pow(Mathf.Abs(u - 0.5f) * 2f, 2.5f) * 26f;
                return y > deck && y < keel;
            };
            cv.Piece(hull, Wood, 8f, (x, y, c) =>
            {
                if (Mathf.Repeat(y, 7f) < 1.1f) return Darker(c, 0.66f);
                if (y > 178f) return Darker(c, 0.6f);
                return Hash(x / 9, (int)(y / 7), 3) < 0.15f ? Darker(c, 0.85f) : c;
            });
            cv.Piece((x, y) => hull(x, y) && !hull(x, y - 4f), C(0.82f, 0.72f, 0.50f), 1.5f);
            foreach (float hx in new[] { 70f, 96f, 122f, 148f }) cv.Piece(Ellipse(hx, 162f, 3f, 3f), C(0.10f, 0.08f, 0.06f), 1f);
            // Cabine sur le pont, toit de bois.
            cv.Piece(Box(36f, 122f, 84f, 146f), C(0.58f, 0.40f, 0.24f), 3f, (x, y, c) => x % 6 == 0 ? Darker(c, 0.7f) : c);
            cv.Piece(Box(32f, 116f, 88f, 124f), WoodDark, 2f);
            cv.Piece(Box(46f, 128f, 56f, 138f), C(0.96f, 0.80f, 0.40f), 1f);
            cv.Piece(Box(64f, 128f, 74f, 138f), C(0.96f, 0.80f, 0.40f), 1f);
            // Petit drapeau rouge en haut du grand mât.
            cv.Piece(Poly(new Vector2(120f, -6f + 0f + 8f), new Vector2(134f, 12f), new Vector2(120f, 16f)), C(0.80f, 0.12f, 0.10f), 1f);

            Color[,] p = cv.Finish(Ink, 0.5f);
            // Reflet sombre et écume le long de la ligne de flottaison.
            for (int x = 0; x < JunkWidth; x++)
            {
                for (int y = 0; y < 18; y++)
                {
                    if (p[x, y].a > 0.5f || x < 20 || x > 210) continue;
                    p[x, y] = y > 14 ? (Hash(x / 2, y, 9) < 0.6f ? Foam : WaterShadow) : y > 6 - (x % 11) * 0.3f ? WaterShadow : C(0, 0, 0, 0);
                }
            }

            return p;
        }

        // ---------- Village flottant ----------

        public const int FloatingWidth = 128;
        public const int FloatingHeight = 136;
        public static readonly Vector2 FloatingPivot = new Vector2(0.5f, 10f / FloatingHeight);

        /// <summary>Maison flottante de pêcheur : murs de planches peintes, toit de tôle ondulée, pot de fleurs, bidons bleus dessous.</summary>
        public static Color[,] FloatingHouse(int variant)
        {
            var cv = new VolumeCanvas(FloatingWidth, FloatingHeight);
            Color wall = variant == 0 ? C(0.36f, 0.58f, 0.66f) : C(0.48f, 0.64f, 0.42f);
            // Bidons bleus sous le plancher.
            for (int i = 0; i < 5; i++) cv.Piece(Ellipse(16f + i * 24f, 124f, 11f, 7f), C(0.16f, 0.36f, 0.66f), 4f);
            cv.Piece(Box(4f, 110f, 124f, 122f), Wood, 2.5f, (x, y, c) => x % 8 == 0 ? Darker(c, 0.66f) : c);
            // Murs et porte, fenêtre aux volets.
            cv.Piece(Box(16f, 52f, 112f, 112f), wall, 3f, (x, y, c) => x % 7 == 0 ? Darker(c, 0.7f) : Hash(x / 7, y / 4, 5) < 0.08f ? Darker(c, 0.86f) : c);
            cv.Piece(Box(54f, 72f, 74f, 112f), C(0.30f, 0.20f, 0.14f), 2f, (x, y, c) => y % 8 == 0 ? Darker(c, 0.7f) : c);
            cv.Piece(Box(24f, 68f, 44f, 86f), C(0.12f, 0.14f, 0.14f), 1f);
            cv.Piece(Box(20f, 68f, 26f, 86f), C(0.86f, 0.70f, 0.30f), 1f);
            cv.Piece(Box(42f, 68f, 48f, 86f), C(0.86f, 0.70f, 0.30f), 1f);
            // Toit de tôle ondulée, débordant.
            cv.Piece((x, y) => y > 22f && y < 56f && Mathf.Abs(x - 64f) < 60f - (56f - y) * 0.6f, C(0.50f, 0.54f, 0.56f), 4f,
                (x, y, c) => x % 6 < 2 ? Darker(c, 0.74f) : Hash(x / 12, y / 8, 9) < 0.12f ? C(0.56f, 0.36f, 0.24f) : c);
            // Pot de fleurs et filet roulé devant.
            cv.Piece(Box(90f, 98f, 102f, 110f), C(0.62f, 0.32f, 0.20f), 2f);
            cv.Piece(Ellipse(96f, 94f, 8f, 6f), C(0.30f, 0.56f, 0.24f), 3f, (x, y, c) => Hash(x, y, 4) < 0.15f ? C(0.94f, 0.40f, 0.50f) : c);
            cv.Piece(Ellipse(30f, 104f, 9f, 5f), C(0.30f, 0.46f, 0.40f), 2f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.7f) : c);
            Color[,] p = cv.Finish(Ink, 0.5f);
            Shadow(p, 64f, 4f, 64f, 5f, WaterShadow);
            return p;
        }

        public const int CageWidth = 96;
        public const int CageHeight = 72;

        /// <summary>Cage à poissons vue de dessus : cadre de planches sur flotteurs, filet sombre, eau à l'intérieur.</summary>
        public static Color[,] Cage()
        {
            var p = new Color[CageWidth, CageHeight];
            for (int x = 0; x < CageWidth; x++)
            {
                for (int y = 6; y < CageHeight; y++)
                {
                    bool frame = x < 8 || x >= CageWidth - 8 || y < 14 || y >= CageHeight - 8;
                    if (frame) p[x, y] = (x + y * 3) % 8 == 0 ? Darker(Wood, 0.7f) : (x % 8 == 0 || y % 8 == 0) ? Darker(Wood, 1.12f) : Wood;
                    else p[x, y] = (x + y) % 6 == 0 || (x - y) % 6 == 0 ? C(0.06f, 0.16f, 0.14f, 0.55f) : C(0.04f, 0.20f, 0.18f, 0.35f);
                }

                for (int y = 0; y < 6; y++) p[x, y] = y > 2 ? Darker(Wood, 0.7f) : (x % 24 < 12 ? C(0.16f, 0.36f, 0.66f) : WaterShadow);
            }

            return p;
        }

        // ---------- Village de la rive ----------

        public const int HouseWidth = 160;
        public const int HouseHeight = 160;
        public static readonly Vector2 HousePivot = new Vector2(0.5f, 6f / HouseHeight);

        /// <summary>Maison de village : murs crépis jaunes (ou bleu pâle), volets verts, toit de tuiles aux bouts relevés, véranda à colonnes.</summary>
        public static Color[,] VillageHouse(int variant)
        {
            var cv = new VolumeCanvas(HouseWidth, HouseHeight);
            Color wall = variant == 0 ? C(0.88f, 0.72f, 0.38f) : C(0.62f, 0.76f, 0.80f);
            cv.Piece(Box(14f, 64f, 146f, 152f), wall, 3f, (x, y, c) => Hash(x / 3, y / 3, 3) < 0.1f ? Darker(c, 0.88f) : y > 140 ? Darker(c, 0.8f) : c);
            foreach (float wx in new[] { 30f, 110f })
            {
                cv.Piece(Box(wx, 86f, wx + 20f, 112f), C(0.10f, 0.12f, 0.12f), 1f);
                cv.Piece(Box(wx - 8f, 86f, wx, 112f), C(0.20f, 0.46f, 0.30f), 1.5f, (x, y, c) => y % 4 == 0 ? Darker(c, 0.7f) : c);
                cv.Piece(Box(wx + 20f, 86f, wx + 28f, 112f), C(0.20f, 0.46f, 0.30f), 1.5f, (x, y, c) => y % 4 == 0 ? Darker(c, 0.7f) : c);
            }

            cv.Piece(Box(68f, 92f, 92f, 152f), C(0.42f, 0.24f, 0.14f), 2f, (x, y, c) => x % 6 == 0 ? Darker(c, 0.7f) : c);
            // Véranda : avancée de tuiles sur deux colonnes.
            foreach (float px in new[] { 20f, 140f }) cv.Piece(Box(px - 3f, 70f, px + 3f, 152f), C(0.90f, 0.86f, 0.76f), 2f);
            Func<float, float, bool> roof = (x, y) =>
            {
                float lift = Mathf.Pow(Mathf.Abs(x - 80f) / 76f, 4f) * 14f;   // les bouts du toit se relèvent
                return y > 14f - lift && y < 70f - lift && Mathf.Abs(x - 80f) < 78f - (70f - y) * 0.45f;
            };
            cv.Piece(roof, Tile, 5f, (x, y, c) =>
            {
                if (x % 7 == 0) return Darker(c, 0.66f);
                if (y % 6 == 0) return Darker(c, 0.82f);
                return Hash(x / 7, y / 6, 9) < 0.1f ? Darker(c, 1.15f) : c;
            });
            cv.Piece(Box(16f, 14f, 144f, 20f), Darker(Tile, 0.7f), 2f);   // faîtage, posé sur le toit
            Color[,] p = cv.Finish(Ink, 0.5f);
            Shadow(p, 80f, 4f, 76f, 5f, C(0.05f, 0.08f, 0.04f, 0.35f));
            return p;
        }

        public const int TempleWidth = 224;
        public const int TempleHeight = 224;
        public static readonly Vector2 TemplePivot = new Vector2(0.5f, 6f / TempleHeight);

        /// <summary>
        /// Temple du village (đình) : socle de pierre et marches, colonnes de laque rouge, portes de bois ajourées, double
        /// toit de tuiles vert sombre aux coins très relevés, deux dragons qui se font face sur le faîte autour d'une perle.
        /// </summary>
        public static Color[,] Temple()
        {
            var cv = new VolumeCanvas(TempleWidth, TempleHeight);
            Color stone = C(0.58f, 0.56f, 0.50f), roofTile = C(0.24f, 0.34f, 0.28f);
            cv.Piece(Box(8f, 196f, 216f, 218f), stone, 3f, (x, y, c) => (x / 12 + y / 6) % 2 == 0 ? Darker(c, 0.9f) : c);
            cv.Piece(Box(84f, 206f, 140f, 220f), Darker(stone, 1.1f), 2f, (x, y, c) => y % 4 == 0 ? Darker(c, 0.7f) : c);
            // Murs de bois sombre et portes ajourées.
            cv.Piece(Box(24f, 120f, 200f, 198f), C(0.30f, 0.18f, 0.12f), 3f);
            for (int i = 0; i < 4; i++)
            {
                float dx = 40f + i * 40f;
                cv.Piece(Box(dx, 136f, dx + 26f, 196f), C(0.46f, 0.28f, 0.16f), 2f, (x, y, c) => (x % 5 == 0 || y % 5 == 0) ? Darker(c, 0.6f) : c);
            }

            foreach (float px in new[] { 28f, 72f, 112f, 152f, 196f })
            {
                cv.Piece(Box(px - 4f, 112f, px + 4f, 198f), Lacquer, 2f, (x, y, c) => y < 118 || y > 192 ? Gold : c);
            }

            // Double toit aux coins relevés.
            Func<float, float, float, float, float, Func<float, float, bool>> Roof = (cx, halfTop, halfBottom, yTop, yBottom) => (x, y) =>
            {
                float lift = Mathf.Pow(Mathf.Clamp01(Mathf.Abs(x - cx) / halfBottom), 5f) * 22f;
                float k = Mathf.InverseLerp(yTop, yBottom, y + lift);
                return y > yTop - lift && y < yBottom - lift && Mathf.Abs(x - cx) < Mathf.Lerp(halfTop, halfBottom, k);
            };
            Func<int, int, Color, Color> tiles = (x, y, c) => x % 6 == 0 ? Darker(c, 0.64f) : y % 5 == 0 ? Darker(c, 0.8f) : Hash(x / 6, y / 5, 3) < 0.08f ? Darker(c, 1.2f) : c;
            // Les coins des toits se relèvent en crosse.
            void Horns(float halfBottom, float yBottom, float size)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    var horn = Curve(new Vector2(112f + side * (halfBottom - size * 1.2f), yBottom - 4f), new Vector2(112f + side * (halfBottom + size * 0.2f), yBottom - 2f),
                        new Vector2(112f + side * (halfBottom + size * 0.2f), yBottom - size * 1.6f), 10);
                    cv.Piece(Tube(horn, size * 0.32f, 1.4f), Darker(roofTile, 0.85f), 2.5f);
                }
            }

            Horns(104f, 120f, 16f);
            cv.Piece(Roof(112f, 70f, 104f, 84f, 124f), roofTile, 5f, tiles);
            cv.Piece(Box(24f, 120f, 200f, 126f), Gold, 1.2f);
            Horns(80f, 82f, 13f);
            cv.Piece(Roof(112f, 46f, 80f, 40f, 86f), roofTile, 5f, tiles);
            cv.Piece(Box(52f, 84f, 172f, 90f), Gold, 1.2f);
            // Faîte : la perle au milieu, un dragon de chaque côté qui ondule vers elle.
            cv.Piece(Box(58f, 34f, 166f, 42f), Darker(roofTile, 0.7f), 2f);
            cv.Piece(Ellipse(112f, 26f, 6f, 6f), C(0.92f, 0.96f, 0.88f), 4f);
            foreach (float side in new[] { -1f, 1f })
            {
                var body = Curve(new Vector2(112f + side * 52f, 34f), new Vector2(112f + side * 34f, 14f), new Vector2(112f + side * 12f, 28f), 12);
                cv.Piece(Tube(body, 4f, 2f), C(0.30f, 0.50f, 0.40f), 3f, (x, y, c) => (x + y) % 4 == 0 ? Darker(c, 0.7f) : c);
                cv.Piece(Ellipse(112f + side * 13f, 26f, 4f, 3.5f), C(0.30f, 0.50f, 0.40f), 3f);
                cv.Piece(Poly(new Vector2(112f + side * 52f, 34f), new Vector2(112f + side * 60f, 18f), new Vector2(112f + side * 46f, 30f)), C(0.30f, 0.50f, 0.40f), 2f);
            }

            Color[,] p = cv.Finish(Ink, 0.5f);
            Shadow(p, 112f, 4f, 108f, 6f, C(0.05f, 0.08f, 0.04f, 0.35f));
            return p;
        }

        // ---------- Bateau-panier ----------

        public const int BasketWidth = 48;
        public const int BasketHeight = 40;

        /// <summary>Bateau-panier (thúng chai) : coque ronde de bambou tressé goudronnée, une rame posée en travers.</summary>
        public static Color[,] BasketBoat()
        {
            var cv = new VolumeCanvas(BasketWidth, BasketHeight);
            cv.Piece(Ellipse(24f, 20f, 21f, 15f), C(0.16f, 0.12f, 0.08f), 5f, (x, y, c) => ((x + y) / 3 + (x - y + 60) / 3) % 2 == 0 ? Darker(c, 1.4f) : c);
            cv.Piece(Ellipse(24f, 19f, 16f, 10f), C(0.62f, 0.48f, 0.26f), 2f, (x, y, c) => ((x + y) / 2 + (x - y + 60) / 2) % 2 == 0 ? Darker(c, 0.78f) : c);
            cv.Piece(Capsule(8f, 30f, 40f, 8f, 1.4f), Wood, 1f);
            Color[,] p = cv.Finish(Ink, 0.5f);
            return p;
        }
    }
}
