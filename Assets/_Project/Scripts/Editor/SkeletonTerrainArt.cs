using System;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Sol et végétation de la Côte des Squelettes : piste de graviers tassés et lit de rivière à sec craquelé (16 px par
    /// case, comme le sol) ; arbres morts tordus, welwitschia, rochers noirs tachés de guano, fût rouillé, ancre et sa
    /// chaîne (32 px par case, style en volume) ; touffes d'herbe sèche, os blanchis et crâne d'otarie (16 px par case).
    /// </summary>
    public static class SkeletonTerrainArt
    {
        private const int S = 16;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Ink = C(0.08f, 0.07f, 0.07f);
        private static readonly Color Bone = C(0.90f, 0.87f, 0.78f);
        private static readonly Color Rust = C(0.58f, 0.28f, 0.14f);

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        private static void GroundShadow(Color[,] p, float cx, float cy, float rx, float ry, float alpha = 0.32f)
        {
            for (int x = 0; x < p.GetLength(0); x++)
            {
                for (int y = 0; y < p.GetLength(1); y++)
                {
                    float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                    if (u * u + v * v <= 1f && p[x, y].a < 0.01f) p[x, y] = C(0.10f, 0.08f, 0.06f, alpha);
                }
            }
        }

        // ---------- Sols ----------

        /// <summary>Graviers tassés de la piste (périodique) : gris-brun, petits cailloux clairs et sombres.</summary>
        public static Color[,] TrackFill(int variant)
        {
            var p = new Color[S, S];
            for (int x = 0; x < S; x++)
            {
                for (int y = 0; y < S; y++)
                {
                    float h = Hash(x, y, 80 + variant);
                    p[x, y] = h < 0.12f ? C(0.36f, 0.32f, 0.28f) : h > 0.92f ? C(0.70f, 0.66f, 0.58f) : C(0.52f, 0.47f, 0.40f);
                }
            }

            return p;
        }

        /// <summary>Lit de rivière à sec (périodique) : vase grise craquelée en plaques.</summary>
        public static Color[,] RiverbedFill(int variant)
        {
            var p = new Color[S, S];
            for (int x = 0; x < S; x++)
            {
                for (int y = 0; y < S; y++)
                {
                    int cx = (x + variant * 5 + (y / 6) * 3) % 8, cy = (y + variant * 3) % 6;
                    bool crack = cx == 0 || cy == 0;
                    float h = Hash(x, y, 90 + variant);
                    p[x, y] = crack ? C(0.40f, 0.38f, 0.36f) : h < 0.1f ? C(0.62f, 0.60f, 0.56f) : C(0.70f, 0.68f, 0.63f);
                }
            }

            return p;
        }

        // ---------- Arbres morts ----------

        public const int TreeWidth = 80;
        public const int TreeHeight = 88;
        public static readonly Vector2 TreePivot = new Vector2(0.5f, 6f / TreeHeight);

        /// <summary>Arbre mort (acacia desséché) : tronc gris fendu qui se tord, branches nues comme des doigts crochus.</summary>
        public static Color[,] DeadTree(int variant)
        {
            var cv = new VolumeCanvas(TreeWidth, TreeHeight);
            float s = variant == 0 ? 1f : -1f;
            Color bark = C(0.42f, 0.38f, 0.34f);
            Func<float, float, bool> wood = Union(
                Capsule(40f, 84f, 40f + s * 4f, 60f, 4.2f),
                Capsule(40f + s * 4f, 60f, 40f - s * 2f, 42f, 3.2f),
                Capsule(40f - s * 2f, 44f, 40f - s * 18f, 26f, 2.4f),
                Capsule(40f - s * 18f, 26f, 40f - s * 30f, 22f, 1.4f),
                Capsule(40f - s * 18f, 26f, 40f - s * 20f, 10f, 1.3f),
                Capsule(40f - s * 2f, 42f, 40f + s * 6f, 18f, 2.2f),
                Capsule(40f + s * 6f, 18f, 40f + s * 2f, 6f, 1.2f),
                Capsule(40f + s * 6f, 20f, 40f + s * 16f, 12f, 1.1f),
                Capsule(40f + s * 4f, 56f, 40f + s * 24f, 40f, 2.2f),
                Capsule(40f + s * 24f, 40f, 40f + s * 34f, 30f, 1.3f),
                Capsule(40f + s * 24f, 40f, 40f + s * 28f, 26f, 1.1f),
                Capsule(40f - s * 26f, 23f, 40f - s * 33f, 30f, 0.9f));
            cv.Piece(wood, bark, 2.5f, (x, y, c) => (x * 3 + y * 7) % 11 == 0 ? Darker(c, 0.7f) : (x + y) % 9 == 0 ? Darker(c, 1.18f) : c);
            Color[,] p = cv.Finish(Ink, 0.6f);
            GroundShadow(p, 40f, 4f, 14f, 3f);
            return p;
        }

        // ---------- Welwitschia ----------

        public const int WelwitschiaWidth = 64;
        public const int WelwitschiaHeight = 32;
        public static readonly Vector2 WelwitschiaPivot = new Vector2(0.5f, 6f / WelwitschiaHeight);

        /// <summary>Welwitschia : deux longues feuilles rubanées, déchirées en lanières, qui traînent sur le sol autour d'un cœur ligneux.</summary>
        public static Color[,] Welwitschia()
        {
            var cv = new VolumeCanvas(WelwitschiaWidth, WelwitschiaHeight);
            Color leaf = C(0.36f, 0.42f, 0.22f);
            Func<int, int, Color, Color> straps = (x, y, c) => (x + y) % 5 == 0 ? Darker(c, 0.75f) : Mathf.Abs(x - 32) > 24 ? C(0.52f, 0.42f, 0.26f) : c;
            // Deux feuilles fendues en longues lanières qui serpentent sur le sol, de part et d'autre du cœur.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int k = 0; k < 5; k++)
                {
                    float dx = side * (17f + k * 2.6f), dy = -7f + k * 3.6f, bend = (k % 2 == 0 ? 3f : -3f);
                    var segments = new Func<float, float, bool>[4];
                    for (int i = 0; i < 4; i++)
                    {
                        float t0 = i / 4f, t1 = (i + 1) / 4f;
                        segments[i] = Capsule(32f + dx * t0, 19f + dy * t0 + Mathf.Sin(t0 * Mathf.PI) * bend, 32f + dx * t1, 19f + dy * t1 + Mathf.Sin(t1 * Mathf.PI) * bend, Mathf.Lerp(2.4f, 1.1f, t1));
                    }

                    cv.Piece(Union(segments), k % 2 == 0 ? leaf : Darker(leaf, 0.85f), 2f, straps);
                }
            }
            cv.Piece(Ellipse(32f, 18f, 7f, 5f), C(0.40f, 0.30f, 0.20f), 3f, (x, y, c) => (x + y) % 4 == 0 ? Darker(c, 0.75f) : c);
            cv.Piece(Ellipse(30f, 14f, 2.5f, 2f), C(0.70f, 0.30f, 0.22f), 1.5f);   // cônes rouges
            cv.Piece(Ellipse(35f, 15f, 2.5f, 2f), C(0.70f, 0.30f, 0.22f), 1.5f);
            Color[,] p = cv.Finish(Ink, 0.5f);
            GroundShadow(p, 32f, 4f, 28f, 3f, 0.25f);
            return p;
        }

        // ---------- Rochers, fût, ancre ----------

        public const int RockWidth = 40;
        public const int RockHeight = 32;
        public static readonly Vector2 RockPivot = new Vector2(0.5f, 6f / RockHeight);

        /// <summary>Rocher noir de basalte, arêtes vives, taché de guano blanc.</summary>
        public static Color[,] Rock(int variant)
        {
            var cv = new VolumeCanvas(RockWidth, RockHeight);
            Func<float, float, bool> body = variant == 0
                ? Union(Ellipse(18f, 20f, 13f, 8f), Ellipse(26f, 16f, 8f, 7f))
                : Union(Ellipse(20f, 19f, 15f, 8f), Ellipse(14f, 14f, 7f, 6f));
            cv.Piece(body, C(0.20f, 0.20f, 0.22f), 6f,
                (x, y, c) => (x * 5 + y * 3) % 17 == 0 ? C(0.86f, 0.84f, 0.78f) : (x - y) % 7 == 0 ? Darker(c, 0.7f) : (x * 3 + y) % 11 == 0 ? Darker(c, 1.3f) : c);
            Color[,] p = cv.Finish(C(0.04f, 0.04f, 0.05f));
            GroundShadow(p, 20f, 5f, 16f, 3f);
            return p;
        }

        public const int DrumWidth = 32;
        public const int DrumHeight = 40;
        public static readonly Vector2 DrumPivot = new Vector2(0.5f, 5f / DrumHeight);

        /// <summary>Fût de pétrole rouillé, cabossé, cerclé, à moitié ensablé.</summary>
        public static Color[,] Drum()
        {
            var cv = new VolumeCanvas(DrumWidth, DrumHeight);
            cv.Piece(Box(6, 8, 26, 34), Rust, 4f, (x, y, c) => y == 14 || y == 26 ? Darker(c, 0.7f) : Hash(x, y, 5) < 0.15f ? C(0.36f, 0.18f, 0.10f) : (x + y) % 7 == 0 ? Darker(c, 1.2f) : c);
            cv.Piece(Ellipse(16f, 8f, 10f, 3f), Darker(Rust, 0.8f), 1.5f);
            cv.Piece(Box(3, 30, 29, 36), C(0.80f, 0.74f, 0.62f), 2f);   // sable accumulé au pied
            Color[,] p = cv.Finish(Ink);
            GroundShadow(p, 16f, 4f, 13f, 2.5f);
            return p;
        }

        public const int AnchorWidth = 72;
        public const int AnchorHeight = 32;
        public static readonly Vector2 AnchorPivot = new Vector2(0.5f, 6f / AnchorHeight);

        /// <summary>Vieille ancre couchée sur le sable et sa grosse chaîne rouillée qui file vers l'épave.</summary>
        public static Color[,] Anchor()
        {
            var cv = new VolumeCanvas(AnchorWidth, AnchorHeight);
            Color iron = C(0.34f, 0.24f, 0.18f);
            for (int i = 0; i < 6; i++)
            {
                float cx = 40f + i * 5.2f;
                cv.Piece((x, y) => { float u = (x - cx) / 3.2f, v = (y - 18f - (i % 2) * 1.5f) / 2.2f; float d = u * u + v * v; return d < 1f && d > 0.25f; }, Rust, 1.5f);
            }

            cv.Piece(Union(Capsule(8f, 18f, 36f, 18f, 2.2f), Capsule(12f, 8f, 12f, 28f, 1.8f), Capsule(4f, 10f, 12f, 8f, 1.6f), Capsule(4f, 26f, 12f, 28f, 1.6f)), iron, 2.5f,
                (x, y, c) => Hash(x, y, 9) < 0.2f ? Rust : c);
            cv.Piece(Ellipse(38f, 18f, 3f, 3f), iron, 2f);
            Color[,] p = cv.Finish(Ink);
            GroundShadow(p, 36f, 5f, 32f, 3f, 0.25f);
            return p;
        }

        // ---------- Petit décor (16 px par case) ----------

        /// <summary>Touffe d'herbe sèche du désert : 0 jaune paille, 1 grise.</summary>
        public static Color[,] Tuft(int variant)
        {
            var p = new Color[S, S];
            Color blade = variant == 0 ? C(0.76f, 0.66f, 0.40f) : C(0.60f, 0.58f, 0.48f);
            for (int i = 0; i < 9; i++)
            {
                int bx = 3 + i + (i % 3);
                int len = 4 + (i * 5) % 6;
                for (int k = 0; k < len; k++) PixelArt.Set(p, bx + (i < 4 ? -k / 3 : i > 5 ? k / 3 : 0), 3 + k, k > len - 2 ? Darker(blade, 1.15f) : i % 2 == 0 ? blade : Darker(blade, 0.8f));
            }

            for (int x = 3; x < 14; x++) PixelArt.Set(p, x, 2, C(0.20f, 0.16f, 0.10f, 0.3f));
            return p;
        }

        /// <summary>Os blanchis sur le sable : 0 crâne d'otarie, 1 côtes et vertèbres éparses.</summary>
        public static Color[,] Bones(int variant)
        {
            var p = new Color[S, S];
            Color shadow = C(0.40f, 0.36f, 0.30f, 0.45f), dark = C(0.20f, 0.18f, 0.16f);
            if (variant == 0)
            {
                PixelArt.Rect(p, 4, 6, 10, 10, Bone);
                PixelArt.Rect(p, 10, 6, 13, 8, Bone);
                PixelArt.Set(p, 6, 8, dark);
                PixelArt.Set(p, 7, 8, dark);
                PixelArt.Set(p, 12, 7, dark);
                PixelArt.Set(p, 13, 6, C(0.98f, 0.96f, 0.90f));
                for (int x = 4; x <= 13; x++) PixelArt.Set(p, x, 5, shadow);
                for (int x = 5; x <= 9; x++) PixelArt.Set(p, x, 11, Darker(Bone, 1.1f));
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    int bx = 3 + i * 3;
                    for (int k = 0; k < 5; k++) PixelArt.Set(p, bx + (k > 2 ? 1 : 0), 5 + k, Bone);
                    PixelArt.Set(p, bx, 4, shadow);
                }

                PixelArt.Rect(p, 3, 11, 13, 11, Darker(Bone, 0.85f));
                PixelArt.Set(p, 14, 11, Bone);
            }

            return p;
        }
    }
}
