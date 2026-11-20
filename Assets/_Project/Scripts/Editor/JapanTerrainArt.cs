using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Sols et végétation de la baie redessinée (16 px par case comme le sol ; arbres dessinés avec
    /// <see cref="VolumeCanvas"/>, y depuis le haut pendant le dessin) : dalles de pierre de l'allée, terre claire du
    /// chemin côtier, cerisiers, pins noirs tordus, érables rouges, touffes de bambous, lanternes de pierre, rochers de la
    /// côte, iris et camélias.
    /// </summary>
    public static class JapanTerrainArt
    {
        private const int S = 16;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Ink = C(0.06f, 0.08f, 0.06f);
        private static readonly Color Bark = C(0.32f, 0.24f, 0.20f);

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        // ---------- Sols ----------

        /// <summary>Dalles de pierre grise (périodique) : deux rangs décalés, joints sombres, mousse dans les joints.</summary>
        public static Color[,] StoneFill(int variant)
        {
            var p = new Color[S, S];
            for (int x = 0; x < S; x++)
            {
                for (int y = 0; y < S; y++)
                {
                    int row = y / 8;
                    int sx = (x + (row % 2) * 4 + variant * 2) % 8;
                    bool joint = y % 8 == 0 || sx == 0;
                    float h = Hash(x, y, 60 + variant);
                    Color stone = Color.Lerp(C(0.62f, 0.62f, 0.60f), C(0.70f, 0.70f, 0.66f), Hash(x / 8 + row * 3, row, 61 + variant));
                    p[x, y] = joint ? (h < 0.3f ? C(0.34f, 0.46f, 0.28f) : C(0.40f, 0.40f, 0.40f)) : h < 0.1f ? Darker(stone, 0.9f) : stone;
                }
            }

            return p;
        }

        /// <summary>Terre claire tassée du chemin côtier (périodique), petits graviers.</summary>
        public static Color[,] EarthFill(int variant)
        {
            var p = new Color[S, S];
            for (int x = 0; x < S; x++)
            {
                for (int y = 0; y < S; y++)
                {
                    float h = Hash(x, y, 70 + variant);
                    p[x, y] = h < 0.12f ? C(0.58f, 0.50f, 0.40f) : h > 0.9f ? C(0.80f, 0.74f, 0.62f) : C(0.70f, 0.62f, 0.50f);
                }
            }

            return p;
        }

        // ---------- Arbres ----------

        public const int TreeWidth = 48;
        public const int TreeHeight = 56;
        public static readonly Vector2 TreePivot = new Vector2(24f / TreeWidth, 5f / TreeHeight);

        private static Color[,] FinishTree(VolumeCanvas cv)
        {
            Color[,] p = cv.Finish(Ink, 0.5f);
            for (int x = 0; x < TreeWidth; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    float u = (x + 0.5f - 25f) / 13f, v = (y + 0.5f - 5f) / 3.5f;
                    if (u * u + v * v <= 1f && p[x, y].a < 0.01f) p[x, y] = C(0.04f, 0.10f, 0.06f, 0.38f);
                }
            }

            return p;
        }

        /// <summary>Cerisier en fleurs : tronc sombre qui se divise, couronne en nuages roses, fleurs blanches, pétales au sol.</summary>
        public static Color[,] Sakura()
        {
            var cv = new VolumeCanvas(TreeWidth, TreeHeight);
            cv.Piece(Union(Capsule(24f, 50f, 23f, 32f, 2.4f), Capsule(23f, 34f, 14f, 24f, 1.5f), Capsule(24f, 34f, 34f, 22f, 1.5f)), Bark, 2f,
                (x, y, c) => (x + y * 2) % 7 == 0 ? Darker(c, 0.75f) : c);
            Color pink = C(0.96f, 0.72f, 0.80f);
            System.Func<int, int, Color, Color> blossom = (x, y, c) =>
            {
                float h = Hash(x, y, 81);
                return h < 0.1f ? C(1f, 0.96f, 0.98f) : h > 0.92f ? C(0.86f, 0.50f, 0.64f) : c;
            };
            cv.Piece(Union(Ellipse(24f, 18f, 15f, 10f), Ellipse(11f, 23f, 9f, 7f), Ellipse(37f, 22f, 9f, 7f), Ellipse(18f, 9f, 9f, 6f), Ellipse(31f, 10f, 9f, 6f)), pink, 6f, blossom);
            cv.Piece(Ellipse(16f, 14f, 6f, 4f), C(1f, 0.84f, 0.90f), 3f, blossom);
            Color[,] p = FinishTree(cv);
            for (int i = 0; i < 9; i++) PixelArt.Set(p, 10 + (i * 7) % 28, 1 + (i * 3) % 7, i % 2 == 0 ? pink : C(1f, 0.92f, 0.94f));   // pétales tombés
            return p;
        }

        /// <summary>Pin noir : tronc tordu penché, plateaux de feuillage vert sombre étagés à l'horizontale.</summary>
        public static Color[,] BlackPine(int variant)
        {
            var cv = new VolumeCanvas(TreeWidth, TreeHeight);
            float lean = variant == 0 ? 1f : -1f;
            cv.Piece(Union(Capsule(24f, 50f, 24f + lean * 4f, 36f, 2.4f), Capsule(24f + lean * 4f, 36f, 24f - lean * 2f, 22f, 2f), Capsule(24f - lean * 2f, 22f, 24f + lean * 6f, 10f, 1.6f),
                Capsule(24f + lean * 3f, 32f, 24f + lean * 16f, 28f, 1.3f)), C(0.36f, 0.26f, 0.22f), 2f, (x, y, c) => (x * 3 + y) % 5 == 0 ? Darker(c, 0.7f) : c);
            Color needles = C(0.14f, 0.32f, 0.22f);
            System.Func<int, int, Color, Color> tufts = (x, y, c) => (x + y * 2) % 4 == 0 ? Darker(c, 0.8f) : (x * 5 + y) % 9 == 0 ? Darker(c, 1.25f) : c;
            foreach ((float cx, float cy, float rx) in new[] { (24f + lean * 16f, 27f, 9f), (24f - lean * 6f, 22f, 10f), (24f + lean * 6f, 11f, 9f), (24f - lean * 4f, 33f, 8f) })
            {
                cv.Piece(Ellipse(cx, cy, rx, rx * 0.42f), needles, 3f, tufts);
            }

            return FinishTree(cv);
        }

        /// <summary>Érable du Japon : tronc fin, couronne étalée rouge vif et orangée, feuilles en étoile.</summary>
        public static Color[,] Maple()
        {
            var cv = new VolumeCanvas(TreeWidth, TreeHeight);
            cv.Piece(Union(Capsule(24f, 50f, 24f, 30f, 1.8f), Capsule(24f, 32f, 14f, 22f, 1.2f), Capsule(24f, 32f, 34f, 22f, 1.2f)), C(0.38f, 0.28f, 0.24f), 2f);
            Color red = C(0.84f, 0.22f, 0.14f);
            System.Func<int, int, Color, Color> leaves = (x, y, c) =>
            {
                float h = Hash(x, y, 91);
                return h < 0.12f ? C(0.98f, 0.56f, 0.18f) : h > 0.9f ? Darker(c, 0.7f) : c;
            };
            cv.Piece(Union(Ellipse(24f, 20f, 18f, 9f), Ellipse(12f, 24f, 9f, 6f), Ellipse(36f, 24f, 9f, 6f), Ellipse(24f, 12f, 12f, 6f)), red, 5f, leaves);
            Color[,] p = FinishTree(cv);
            for (int i = 0; i < 7; i++) PixelArt.Set(p, 12 + (i * 5) % 24, 1 + (i * 3) % 6, i % 2 == 0 ? red : C(0.98f, 0.56f, 0.18f));
            return p;
        }

        public const int BambooWidth = 32;
        public const int BambooHeight = 64;
        public static readonly Vector2 BambooPivot = new Vector2(16f / BambooWidth, 4f / BambooHeight);

        /// <summary>Touffe de bambous (32×64) : cinq tiges vert tendre à nœuds, feuilles fines en éventail.</summary>
        public static Color[,] Bamboo(int variant)
        {
            var cv = new VolumeCanvas(BambooWidth, BambooHeight);
            Color cane = variant == 0 ? C(0.52f, 0.70f, 0.30f) : C(0.60f, 0.72f, 0.32f);
            float[] xs = variant == 0 ? new[] { 9f, 13f, 17f, 21f, 25f } : new[] { 8f, 12.5f, 16f, 20.5f, 24f };
            for (int i = 0; i < xs.Length; i++)
            {
                float bx = xs[i], top = 4f + (i * 7 % 5) * 3f;
                cv.Piece((x, y) => y > top && y < 60f && Mathf.Abs(x - (bx + (60f - y) * (bx - 16f) * 0.012f)) < 1.3f, cane, 1.2f,
                    (x, y, c) => ((int)y + i * 3) % 9 == 0 ? Darker(c, 0.7f) : c);
            }

            Color leaf = C(0.30f, 0.54f, 0.22f);
            for (int k = 0; k < 14; k++)
            {
                float lx = 4f + (k * 11) % 24, ly = 6f + (k * 7) % 34;
                float dir = k % 2 == 0 ? 1f : -1f;
                cv.Piece((x, y) =>
                {
                    float t = (x - lx) * dir;
                    return t > 0f && t < 7f && Mathf.Abs(y - (ly + t * 0.45f)) < 1.1f - t * 0.12f;
                }, k % 3 == 0 ? Darker(leaf, 1.15f) : leaf, 0.8f);
            }

            Color[,] p = cv.Finish(Ink, 0.45f);
            for (int x = 4; x < 28; x++)
            {
                for (int y = 0; y < 7; y++)
                {
                    float u = (x + 0.5f - 16.5f) / 11f, v = (y + 0.5f - 3.5f) / 3f;
                    if (u * u + v * v <= 1f && p[x, y].a < 0.01f) p[x, y] = C(0.04f, 0.10f, 0.06f, 0.35f);
                }
            }

            return p;
        }

        // ---------- Décor ----------

        public const int LanternWidth = 16;
        public const int LanternHeight = 32;
        public static readonly Vector2 LanternPivot = new Vector2(0.5f, 2f / LanternHeight);

        /// <summary>Lanterne de pierre (tōrō) : socle, fût, boîte à lumière ajourée et chaude, toit recourbé, pointe.</summary>
        public static Color[,] StoneLantern()
        {
            var cv = new VolumeCanvas(LanternWidth, LanternHeight);
            Color stone = C(0.66f, 0.66f, 0.62f);
            cv.Piece(Box(3, 25, 13, 30), stone, 1.5f);
            cv.Piece(Box(6, 17, 10, 26), stone, 1.5f);
            cv.Piece(Box(4, 15, 12, 18), stone, 1.2f);
            cv.Piece(Box(4.5f, 9, 11.5f, 15.5f), stone, 1.2f, (x, y, c) => x > 5 && x < 11 && y > 10 && y < 15 ? C(1f, 0.82f, 0.46f) : c);
            cv.Piece((x, y) => y > 5 && y < 10 && Mathf.Abs(x - 8f) < 8f - (10f - y) * 0.9f + (y > 8 ? 0.5f : 0f), stone, 1.5f, (x, y, c) => (x + y) % 4 == 0 ? C(0.40f, 0.52f, 0.32f) : c);
            cv.Piece(Ellipse(8f, 4.5f, 1.6f, 2f), stone, 1f);
            Color[,] p = cv.Finish(Ink, 0.5f);
            for (int x = 2; x < 14; x++)
            {
                if (p[x, 0].a < 0.01f) p[x, 0] = C(0.04f, 0.10f, 0.06f, 0.35f);
                if (p[x, 1].a < 0.01f) p[x, 1] = C(0.04f, 0.10f, 0.06f, 0.35f);
            }

            return p;
        }

        /// <summary>Rocher de la côte (16×16) : roche volcanique sombre, mousse et lichen clair ; <paramref name="variant"/> change la forme.</summary>
        public static Color[,] CoastRock(int variant)
        {
            var cv = new VolumeCanvas(16, 16);
            Color rock = C(0.36f, 0.34f, 0.36f);
            System.Func<float, float, bool> shape = variant == 0
                ? Union(Ellipse(8f, 9f, 6.5f, 5.5f), Ellipse(5f, 11f, 4f, 3.5f))
                : Union(Ellipse(9f, 8f, 5.5f, 6f), Ellipse(12f, 11f, 3.5f, 3f));
            cv.Piece(shape, rock, 4f, (x, y, c) => y < 7 && (x + y) % 3 == 0 ? C(0.36f, 0.48f, 0.30f) : (x * 3 + y) % 11 == 0 ? C(0.70f, 0.72f, 0.60f) : c);
            return cv.Finish(Ink, 0.5f);
        }

        /// <summary>Touffe de fleurs (16×16) : iris violets (0) ou camélias rouges sur feuillage sombre (1).</summary>
        public static Color[,] Flowers(int variant)
        {
            var cv = new VolumeCanvas(16, 16);
            if (variant == 0)
            {
                cv.Piece((x, y) => y > 4 && y < 15 && (Mathf.Abs(x - (4f + (15 - y) * 0.15f)) < 0.8f || Mathf.Abs(x - 8f) < 0.8f || Mathf.Abs(x - (12f - (15 - y) * 0.15f)) < 0.8f), C(0.30f, 0.54f, 0.26f), 1f);
                foreach ((float fx, float fy) in new[] { (4.5f, 5f), (8f, 3.5f), (11.5f, 5.5f) })
                {
                    cv.Piece(Union(Ellipse(fx - 1.4f, fy, 1.4f, 1.6f), Ellipse(fx + 1.4f, fy, 1.4f, 1.6f), Ellipse(fx, fy - 1.2f, 1.2f, 1.4f)), C(0.46f, 0.30f, 0.80f), 1.2f,
                        (x, y, c) => Mathf.Abs(x - fx) < 0.6f && y > fy ? C(0.98f, 0.86f, 0.30f) : c);
                }
            }
            else
            {
                cv.Piece(Union(Ellipse(8f, 10f, 6.5f, 4.5f), Ellipse(5f, 7f, 3.5f, 3f), Ellipse(11f, 7f, 3.5f, 3f)), C(0.14f, 0.34f, 0.18f), 3f);
                foreach ((float fx, float fy) in new[] { (5f, 7f), (10.5f, 8f), (8f, 11f) })
                {
                    cv.Piece(Ellipse(fx, fy, 2f, 1.7f), C(0.86f, 0.14f, 0.20f), 1.5f, (x, y, c) => Mathf.Abs(x - fx) < 0.6f && Mathf.Abs(y - fy) < 0.6f ? C(1f, 0.86f, 0.30f) : c);
                }
            }

            return cv.Finish(Ink, 0.45f);
        }
    }
}
