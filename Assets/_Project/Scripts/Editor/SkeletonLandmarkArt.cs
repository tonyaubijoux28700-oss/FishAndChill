using System;
using FishAndChill.Player;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les repères de la Côte des Squelettes, dans le style en volume (32 px par case, y depuis le haut pendant le dessin) :
    /// cargo échoué à moitié ensablé, chalutier couché dans les vagues, squelette de baleine, cabane de prospecteur en tôle
    /// rouillée, grandes dunes rouges du Namib.
    /// </summary>
    public static class SkeletonLandmarkArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Rust = C(0.56f, 0.26f, 0.13f);
        private static readonly Color RustDark = C(0.32f, 0.15f, 0.09f);
        private static readonly Color Steel = C(0.36f, 0.38f, 0.40f);
        private static readonly Color Bone = C(0.88f, 0.85f, 0.76f);
        private static readonly Color Sand = C(0.80f, 0.75f, 0.64f);
        private static readonly Color Hole = C(0.08f, 0.06f, 0.06f);
        private static readonly Color Ink = C(0.07f, 0.05f, 0.05f);
        private static readonly Color Foam = C(0.86f, 0.90f, 0.90f, 0.75f);
        private static readonly Color WaterShadow = C(0.04f, 0.10f, 0.12f, 0.4f);

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        /// <summary>Tôle rouillée : coulures sombres verticales, plaques rivetées, taches de rouille vive.</summary>
        private static Color RustTexture(int x, int y, Color c)
        {
            if (x % 22 == 0 || y % 18 == 0) return Darker(c, 0.7f);                          // joints des plaques
            if ((x % 22 == 3 || x % 22 == 19) && y % 4 == 2) return C(0.30f, 0.22f, 0.18f);  // rivets
            float streak = Hash(x / 2, 0, 11);
            if (streak < 0.14f && Mathf.PerlinNoise(x * 0.3f, y * 0.06f) > 0.45f) return Darker(c, 0.66f);   // coulures
            float patch = Mathf.PerlinNoise(x * 0.07f + 3.1f, y * 0.09f + 7.3f);                                 // grandes plaques de rouille
            if (patch > 0.66f) return C(0.70f, 0.36f, 0.16f);
            if (patch < 0.3f) return Darker(c, 0.82f);
            return Hash(x, y, 13) < 0.04f ? Darker(c, 1.15f) : c;
        }

        /// <summary>Peinture écaillée : grandes plaques de rouille dans la peinture claire.</summary>
        private static Color Peeling(int x, int y, Color c)
        {
            float patch = Mathf.PerlinNoise(x * 0.08f + 11f, y * 0.08f + 5f);
            if (patch > 0.62f) return patch > 0.7f ? Rust : C(0.66f, 0.40f, 0.26f);
            return (x * 3 + y) % 13 == 0 ? Darker(c, 0.85f) : c;
        }

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

        // ---------- Cargo échoué ----------

        public const int WreckWidth = 320;
        public const int WreckHeight = 176;
        public static readonly Vector2 WreckPivot = new Vector2(0.5f, 6f / WreckHeight);

        /// <summary>
        /// Cargo échoué : longue coque de tôle rouillée à moitié enfouie, proue relevée à gauche, coque éventrée au milieu
        /// (on voit les membrures), château arrière aux hublots vides et vitres brisées, cheminée penchée, mât de charge
        /// tordu, dune de sable accumulée contre le flanc.
        /// </summary>
        public static Color[,] Wreck()
        {
            var cv = new VolumeCanvas(WreckWidth, WreckHeight);
            // Coque : bord supérieur qui remonte vers la proue, ensablée en bas.
            Func<float, float, bool> hull = (x, y) =>
            {
                if (x < 10 || x > 306) return false;
                float t = (x - 10f) / 296f;
                float top = 92f - (t < 0.18f ? (0.18f - t) * 150f : 0f);
                float bow = t < 0.06f ? (0.06f - t) * 500f : 0f;   // étrave en biais
                return y > top && y < 160f && x > 10f + bow * 0.2f + (160f - y) * (t < 0.1f ? 0.25f : 0f);
            };
            cv.Piece(hull, Rust, 6f, RustTexture);
            // Bande noire de flottaison et ligne de pont.
            cv.Piece((x, y) => hull(x, y) && y > 132 && y < 142, C(0.20f, 0.12f, 0.10f), 2f, RustTexture);
            // Coque éventrée : trou béant et membrures.
            cv.Piece((x, y) => { float u = (x - 150f) / 34f, v = (y - 120f) / 26f; return u * u + v * v < 1f + Mathf.Sin(x * 0.7f) * 0.12f; }, Hole, 1f,
                (x, y, c) => (x - 120) % 12 < 3 ? RustDark : c);
            // Hublots de la coque.
            for (int i = 0; i < 6; i++)
            {
                float hx = 60f + i * 16f;
                cv.Piece(Ellipse(hx, 108f, 3.2f, 3.2f), Hole, 1f);
            }

            // Château arrière : deux étages, vitres brisées, porte arrachée.
            cv.Piece(Box(220, 46, 294, 94), C(0.62f, 0.58f, 0.52f), 4f, Peeling);
            cv.Piece(Box(232, 22, 286, 48), C(0.64f, 0.60f, 0.54f), 3.5f, Peeling);
            for (int i = 0; i < 4; i++)
            {
                int wx = 238 + i * 12;
                cv.Piece(Box(wx, 28, wx + 8, 38), Hole, 1f, (x, y, c) => (x + y) % 5 == 0 ? C(0.40f, 0.46f, 0.48f) : c);
            }

            for (int i = 0; i < 5; i++)
            {
                int wx = 226 + i * 13;
                cv.Piece(Box(wx, 60, wx + 7, 70), Hole, 1f);
            }

            cv.Piece(Box(274, 72, 286, 94), Hole, 1f);
            // Cheminée penchée, mât de charge tordu.
            cv.Piece((x, y) => y > 4 && y < 30 && Mathf.Abs(x - (262f + (30f - y) * 0.35f)) < 7f, C(0.26f, 0.22f, 0.20f), 3f, (x, y, c) => y > 10 && y < 14 ? Rust : c);
            cv.Piece(Union(Capsule(110f, 92f, 104f, 30f, 2.4f), Capsule(104f, 30f, 82f, 46f, 1.6f), Capsule(104f, 34f, 140f, 60f, 1.2f)), Steel, 2f, (x, y, c) => Hash(x, y, 23) < 0.3f ? Rust : c);
            // Sable accumulé contre le flanc.
            cv.Piece((x, y) => y > 150f - Mathf.Sin(x * 0.03f) * 6f - (x > 140 && x < 220 ? 8f : 0f) && y < 172, Sand, 5f, (x, y, c) => (x + y * 3) % 17 == 0 ? Darker(c, 0.88f) : c);
            Color[,] p = cv.Finish(Ink);
            Shadow(p, 160f, 6f, 154f, 6f, C(0.10f, 0.08f, 0.06f, 0.3f));
            return p;
        }

        // ---------- Chalutier couché ----------

        public const int TrawlerWidth = 192;
        public const int TrawlerHeight = 112;

        /// <summary>Chalutier couché sur le flanc dans les vagues : coque rouillée penchée, timonerie vide, mâts brisés, écume autour.</summary>
        public static Color[,] Trawler()
        {
            var cv = new VolumeCanvas(TrawlerWidth, TrawlerHeight);
            Func<float, float, bool> hull = (x, y) =>
            {
                if (x < 14 || x > 178) return false;
                float t = (x - 14f) / 164f;
                float top = 52f + t * 14f;
                float bottom = 88f - Mathf.Pow(Mathf.Abs(t - 0.5f) * 2f, 2f) * 14f;
                return y > top && y < bottom;
            };
            cv.Piece(hull, C(0.50f, 0.28f, 0.18f), 5f, RustTexture);
            cv.Piece((x, y) => hull(x, y) && y < 60f + (x - 14f) / 164f * 14f, C(0.78f, 0.76f, 0.70f), 2f, Peeling);
            // Timonerie penchée.
            cv.Piece((x, y) => y > 22 && y < 66 && Mathf.Abs(x - (110f + (66f - y) * 0.4f)) < 18f, C(0.70f, 0.68f, 0.62f), 3f, Peeling);
            for (int i = 0; i < 3; i++)
            {
                int wx = 98 + i * 10;
                cv.Piece(Box(wx + 6, 30, wx + 12, 38), Hole, 1f);
            }

            cv.Piece(Union(Capsule(60f, 56f, 76f, 10f, 2f), Capsule(150f, 64f, 170f, 30f, 1.8f)), C(0.30f, 0.24f, 0.20f), 1.5f);
            Color[,] p = cv.Finish(Ink);
            // Écume et ressac autour de la coque (y depuis le bas).
            for (int x = 0; x < TrawlerWidth; x++)
            {
                for (int y = 0; y < 34; y++)
                {
                    if (p[x, y].a > 0.01f) continue;
                    float wave = Mathf.Sin(x * 0.25f + y * 0.9f);
                    if (x > 4 && x < 188 && y < 28 - Mathf.Abs(x - 96f) * 0.12f && wave > 0.2f) p[x, y] = wave > 0.75f ? Foam : WaterShadow;
                }
            }

            return p;
        }

        // ---------- Squelette de baleine ----------

        public const int WhaleWidth = 224;
        public const int WhaleHeight = 96;
        public static readonly Vector2 WhalePivot = new Vector2(0.5f, 8f / WhaleHeight);

        /// <summary>Squelette de baleine blanchi : grand crâne et mâchoires arquées à gauche, colonne de vertèbres, côtes dressées comme des arceaux, os de nageoire, sable autour.</summary>
        public static Color[,] Whale()
        {
            var cv = new VolumeCanvas(WhaleWidth, WhaleHeight);
            Func<int, int, Color, Color> bone = (x, y, c) => (x * 3 + y * 5) % 13 == 0 ? Darker(c, 0.82f) : (x + y) % 9 == 0 ? Darker(c, 1.08f) : c;
            // Sable remué sous les os.
            cv.Piece((x, y) => { float u = (x - 112f) / 108f, v = (y - 80f) / 10f; return u * u + v * v < 1f; }, Sand, 2f);
            // Côtes recourbées, dressées vers le ciel.
            for (int i = 0; i < 9; i++)
            {
                float rx = 74f + i * 13f;
                float h = 46f - Mathf.Abs(i - 3f) * 4f;
                cv.Piece(Union(Capsule(rx, 74f, rx - 4f, 74f - h * 0.55f, 2.4f), Capsule(rx - 4f, 74f - h * 0.55f, rx + 2f, 74f - h, 1.6f)), Bone, 2f, bone);
            }

            // Colonne vertébrale.
            for (int i = 0; i < 20; i++)
            {
                float vx = 64f + i * 7.6f;
                float vy = 76f - Mathf.Sin(i / 19f * Mathf.PI) * 4f;
                cv.Piece(Ellipse(vx, vy, 3.6f, 4.2f - i * 0.12f), Bone, 2f, bone);
                cv.Piece(Capsule(vx, vy - 3f, vx + 1f, vy - 9f + i * 0.2f, 1f), Bone, 1f);
            }

            // Crâne et mâchoires.
            cv.Piece(Union(Ellipse(40f, 70f, 26f, 11f), Capsule(14f, 72f, 60f, 64f, 7f)), Bone, 4f, bone);
            cv.Piece((x, y) => { float u = (x - 34f) / 32f, v = (y - 84f) / 10f; float d = u * u + v * v; return d < 1f && d > 0.55f && y > 78f; }, Darker(Bone, 0.92f), 2f, bone);
            cv.Piece(Ellipse(48f, 68f, 3f, 2.5f), Hole, 1f);
            // Os de nageoire.
            cv.Piece(Union(Capsule(92f, 82f, 104f, 90f, 2f), Capsule(104f, 90f, 116f, 88f, 1.6f), Capsule(104f, 90f, 112f, 93f, 1.4f)), Bone, 1.5f);
            Color[,] p = cv.Finish(C(0.30f, 0.26f, 0.20f), 0.5f);
            return p;
        }

        // ---------- Cabane du prospecteur ----------

        public const int ShackWidth = 160;
        public const int ShackHeight = 136;
        public static readonly Vector2 ShackPivot = new Vector2(0.5f, 6f / ShackHeight);

        /// <summary>Cabane de prospecteur abandonnée : tôle ondulée rouillée, toit affaissé et troué, porte qui pend, fenêtre condamnée de planches, éolienne cassée.</summary>
        public static Color[,] Shack()
        {
            var cv = new VolumeCanvas(ShackWidth, ShackHeight);
            Color tin = C(0.52f, 0.44f, 0.38f);
            Func<int, int, Color, Color> corrugated = (x, y, c) => x % 4 == 0 ? Darker(c, 0.72f) : x % 4 == 1 ? Darker(c, 1.12f) : Hash(x / 3, y / 4, 41) < 0.3f ? Rust : c;
            // Éolienne cassée derrière.
            cv.Piece(Union(Capsule(132f, 128f, 140f, 20f, 1.4f), Capsule(150f, 128f, 140f, 20f, 1.4f), Capsule(134f, 90f, 148f, 90f, 1f), Capsule(136f, 60f, 146f, 60f, 1f)), Steel, 1.5f);
            for (int b = 0; b < 3; b++)
            {
                float a = b * 2.1f + 0.4f;
                cv.Piece(Capsule(140f, 20f, 140f + Mathf.Cos(a) * 16f, 20f + Mathf.Sin(a) * 16f, 2f), Steel, 1.2f, (x, y, c) => Hash(x, y, 42) < 0.3f ? Rust : c);
            }

            // Murs.
            cv.Piece(Box(14, 54, 128, 128), tin, 3f, corrugated);
            // Porte qui pend, de travers.
            cv.Piece((x, y) => y > 78 && y < 128 && x > 52 + (y - 78) * 0.1f && x < 76 + (y - 78) * 0.1f, C(0.40f, 0.30f, 0.22f), 2f, (x, y, c) => y % 6 == 0 ? Darker(c, 0.7f) : c);
            cv.Piece(Box(50, 78, 54, 128), Hole, 1f);
            // Fenêtre condamnée.
            cv.Piece(Box(88, 72, 116, 98), Hole, 1f);
            foreach ((float a, float b) in new[] { (74f, 82f), (84f, 92f), (90f, 96f) })
            {
                cv.Piece((x, y) => x > 84 && x < 120 && Mathf.Abs(y - (a + (x - 84) * (b - a) / 36f)) < 2.5f, C(0.52f, 0.40f, 0.28f), 1.5f);
            }

            // Toit affaissé, troué.
            cv.Piece((x, y) => x > 6 && x < 136 && y > 34f + Mathf.Sin((x - 6f) / 130f * Mathf.PI) * 8f && y < 58f + Mathf.Sin((x - 6f) / 130f * Mathf.PI) * 6f, C(0.48f, 0.30f, 0.20f), 3f, corrugated);
            cv.Piece(Ellipse(92f, 46f, 9f, 4f), Hole, 1f);
            // Sable contre les murs.
            cv.Piece((x, y) => y > 120f + Mathf.Sin(x * 0.08f) * 3f && y < 132 && x > 8 && x < 152, Sand, 3f);
            Color[,] p = cv.Finish(Ink);
            Shadow(p, 76f, 5f, 72f, 5f, C(0.10f, 0.08f, 0.06f, 0.3f));
            return p;
        }

        // ---------- Dunes ----------

        public const int DuneWidth = 352;
        public const int DuneHeight = 160;
        public static readonly Vector2 DunePivot = new Vector2(0.5f, 6f / DuneHeight);

        /// <summary>Grande dune rouge du Namib : crête nette en arête, versant au vent clair, versant à l'ombre sombre, rides du vent.</summary>
        public static Color[,] Dune(int variant)
        {
            var p = new Color[DuneWidth, DuneHeight];
            float peakX = variant == 0 ? 210f : 140f;
            Color lit = C(0.84f, 0.52f, 0.32f), shade = C(0.52f, 0.28f, 0.20f), crest = C(0.94f, 0.66f, 0.44f);
            for (int x = 0; x < DuneWidth; x++)
            {
                float t = x < peakX ? x / peakX : (DuneWidth - x) / (DuneWidth - peakX);
                float top = 6f + (1f - Mathf.SmoothStep(0f, 1f, t)) * 150f;           // hauteur de la silhouette (depuis le bas : y = 160 - top)
                for (int y = 0; y < DuneHeight; y++)
                {
                    float fromTop = DuneHeight - 1 - y;
                    if (fromTop < top) continue;
                    // Le versant à l'ombre est à droite de la crête qui serpente vers le bas.
                    float crestX = peakX + (fromTop - top) * (variant == 0 ? 0.6f : -0.6f) * 0.5f + Mathf.Sin(fromTop * 0.05f) * 8f;
                    bool shadowSide = variant == 0 ? x > crestX : x < crestX;
                    Color c = shadowSide ? shade : lit;
                    if (Mathf.Abs(x - crestX) < 1.5f) c = crest;
                    else if (!shadowSide && ((int)(fromTop + x * 0.25f) % 7 == 0)) c = Darker(c, 0.9f);   // rides du vent
                    p[x, y] = c;
                }
            }

            PixelShading.SelectiveOutline(p, C(0.30f, 0.14f, 0.10f), 0.5f);
            return p;
        }
    }
}
