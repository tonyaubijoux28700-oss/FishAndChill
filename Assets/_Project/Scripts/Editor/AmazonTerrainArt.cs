using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Terrain et végétation du fleuve redessiné (16 px par case comme le sol ; les plantes sont dessinées avec
    /// <see cref="VolumeCanvas"/>, y depuis le haut pendant le dessin) : terre rouge du chemin, sable pâle du banc,
    /// grands arbres de la jungle (sur la terre ou debout dans l'eau), palmiers açaí, bananiers, héliconias, grandes
    /// feuilles de sous-bois et troncs couchés.
    /// </summary>
    public static class AmazonTerrainArt
    {
        private const int S = 16;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Laterite = C(0.68f, 0.34f, 0.20f);
        private static readonly Color LateriteDark = C(0.54f, 0.25f, 0.15f);
        private static readonly Color LateriteLight = C(0.78f, 0.45f, 0.27f);
        private static readonly Color SandPale = C(0.90f, 0.82f, 0.62f);
        private static readonly Color SandShade = C(0.80f, 0.70f, 0.50f);
        private static readonly Color SandLight = C(0.96f, 0.90f, 0.74f);
        private static readonly Color Bark = C(0.58f, 0.52f, 0.42f);
        private static readonly Color BarkDark = C(0.36f, 0.30f, 0.24f);
        private static readonly Color Ink = C(0.04f, 0.10f, 0.05f);

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

        /// <summary>Terre rouge (latérite) du chemin, périodique : grain, flaques sèches plus sombres, cailloux clairs.</summary>
        public static Color[,] DirtFill(int variant)
        {
            var p = new Color[S, S];
            for (int x = 0; x < S; x++)
            {
                for (int y = 0; y < S; y++)
                {
                    float h = Hash(x, y, 31 + variant);
                    p[x, y] = h < 0.14f ? LateriteDark : h > 0.9f ? LateriteLight : Laterite;
                }
            }

            foreach ((int x, int y) in new[] { (2, 5), (10, 11), (7, 2), (13, 7) })
            {
                int px = (x + variant * 6) % S, py = (y + variant * 5) % S;
                p[px, py] = C(0.86f, 0.62f, 0.44f);
                p[(px + 1) % S, py] = LateriteLight;
            }

            return p;
        }

        /// <summary>Sable pâle du banc (périodique) : rides laissées par le courant.</summary>
        public static Color[,] BeachFill(int variant)
        {
            var p = new Color[S, S];
            for (int x = 0; x < S; x++)
            {
                for (int y = 0; y < S; y++)
                {
                    float h = Hash(x, y, 51 + variant);
                    int ripple = (y * 2 + Mathf.RoundToInt(Mathf.Sin((x + variant * 4) * 0.8f) * 1.5f) + 32) % 6;
                    p[x, y] = ripple == 0 ? SandShade : h > 0.92f ? SandLight : SandPale;
                }
            }

            return p;
        }

        // ---------- Grands arbres ----------

        public const int TreeWidth = 48;
        public const int TreeHeight = 64;

        /// <summary>Pied du tronc (au milieu de la case, comme le cocotier).</summary>
        public static readonly Vector2 TreePivot = new Vector2(24f / TreeWidth, 6f / TreeHeight);

        /// <summary>
        /// Grand arbre de la jungle (48×64) : contreforts au pied, tronc pâle, couronne en nuages de feuillage avec
        /// lianes et broméliacées. <paramref name="variant"/> 0 vert sombre, 1 vert plus jaune, 2 couronne en étages.
        /// <paramref name="flooded"/> : debout dans l'eau (racines échasses, reflet et ronds dans l'eau au lieu de l'ombre).
        /// </summary>
        public static Color[,] JungleTree(int variant, bool flooded = false)
        {
            var cv = new VolumeCanvas(TreeWidth, TreeHeight);
            Color leaves = variant == 1 ? C(0.26f, 0.48f, 0.14f) : variant == 2 ? C(0.12f, 0.36f, 0.20f) : C(0.14f, 0.40f, 0.16f);
            System.Func<int, int, Color, Color> barkTexture = (x, y, c) => (x * 3 + y / 3) % 7 == 0 ? Darker(c, 0.8f) : c;

            if (flooded)
            {
                // Racines échasses qui plongent dans l'eau.
                foreach (float rx in new[] { -7f, -3.5f, 3.5f, 7f })
                {
                    cv.Piece(Capsule(24f + rx * 0.3f, 40f, 24f + rx, 57f, 1.1f), BarkDark, 1.2f);
                }
            }
            else
            {
                // Contreforts : planches de racine qui s'évasent au pied.
                cv.Piece((x, y) => y > 44f && y < 58.5f && Mathf.Abs(x - 24f) < 2.4f + (y - 44f) * 0.55f && ((int)Mathf.Abs(x - 24f) % 4 != 3 || y < 50f), Bark, 2f, barkTexture);
            }

            cv.Piece(Capsule(24f, 26f, 24f, flooded ? 44f : 54f, 2.6f), Bark, 2f, barkTexture);
            // Branches maîtresses.
            cv.Piece(Capsule(24f, 30f, 14f, 22f, 1.3f), Bark, 1.5f);
            cv.Piece(Capsule(24f, 28f, 34f, 21f, 1.3f), Bark, 1.5f);

            System.Func<int, int, Color, Color> foliage = (x, y, c) =>
            {
                float h = Hash(x, y, 70 + variant);
                return h < 0.08f ? Darker(c, 0.78f) : h > 0.94f ? Darker(c, 1.25f) : c;
            };
            if (variant == 2)
            {
                // Couronne en étages, comme les grands émergents.
                cv.Piece(Ellipse(24f, 24f, 22f, 6f), leaves, 4f, foliage);
                cv.Piece(Ellipse(24f, 14f, 16f, 6f), leaves, 4f, foliage);
                cv.Piece(Ellipse(24f, 6f, 9f, 5f), leaves, 4f, foliage);
            }
            else
            {
                cv.Piece(Union(Ellipse(24f, 16f, 15f, 11f), Ellipse(11f, 22f, 10f, 7.5f), Ellipse(37f, 21f, 10f, 7.5f),
                    Ellipse(17f, 7f, 9f, 6f), Ellipse(32f, 8f, 9f, 6f)), leaves, 6f, foliage);
                cv.Piece(Ellipse(15f, 12f, 6f, 4.5f), Darker(leaves, 1.12f), 3f, foliage);
                cv.Piece(Ellipse(31f, 15f, 6f, 4.5f), Darker(leaves, 1.08f), 3f, foliage);
            }

            // Lianes qui pendent et une broméliacée rouge sur une branche.
            Color liana = C(0.22f, 0.38f, 0.12f);
            foreach (float lx in new[] { 8f, 38f, 30f })
            {
                float top = lx == 30f ? 30f : 26f;
                cv.Piece((x, y) => y > top && y < top + 14f && Mathf.Abs(x - (lx + Mathf.Sin(y * 0.5f) * 0.8f)) < 0.7f, liana, 0.6f);
            }

            cv.Piece(Ellipse(15f, 22.5f, 2.6f, 1.6f), C(0.30f, 0.52f, 0.20f), 1.2f);
            cv.Piece(Ellipse(15f, 21.5f, 1.2f, 1f), C(0.88f, 0.20f, 0.22f), 1f);

            Color[,] p = cv.Finish(Ink, 0.5f);
            // Sous le tronc (y depuis le bas) : ombre sur la terre, ou reflet sombre et ronds dans l'eau.
            for (int x = 0; x < TreeWidth; x++)
            {
                for (int y = 0; y < 12; y++)
                {
                    float u = (x + 0.5f - 25f) / (flooded ? 12f : 15f), v = (y + 0.5f - 5.5f) / 4f;
                    float d = u * u + v * v;
                    if (d > 1f || p[x, y].a > 0.01f) continue;
                    if (!flooded) p[x, y] = C(0.02f, 0.08f, 0.03f, 0.42f);
                    else p[x, y] = d > 0.78f && d < 1f && (x + y) % 3 != 0 ? C(0.86f, 0.80f, 0.64f, 0.55f) : C(0.10f, 0.14f, 0.08f, 0.35f);
                }
            }

            return p;
        }

        // ---------- Palmiers, bananiers, sous-bois ----------

        public const int PalmWidth = 32;
        public const int PalmHeight = 56;
        public static readonly Vector2 PalmPivot = new Vector2(16f / PalmWidth, 4f / PalmHeight);

        /// <summary>Touffe de palmiers açaí (32×56) : trois stipes fins et courbés, palmes en plumeau, grappes violettes.</summary>
        public static Color[,] AcaiPalm(int variant)
        {
            var cv = new VolumeCanvas(PalmWidth, PalmHeight);
            Color trunk = C(0.52f, 0.48f, 0.38f);
            Color frond = variant == 0 ? C(0.24f, 0.50f, 0.18f) : C(0.30f, 0.54f, 0.16f);
            (float baseX, float topX, float topY)[] stems = variant == 0
                ? new[] { (15f, 9f, 12f), (16.5f, 22f, 6f), (17f, 17f, 20f) }
                : new[] { (14f, 7f, 16f), (17f, 18f, 5f), (18f, 26f, 14f) };
            foreach ((float bx, float tx, float ty) in stems)
            {
                float baseX = bx, topX = tx, topY = ty;
                cv.Piece((x, y) =>
                {
                    float t = Mathf.InverseLerp(52f, topY, y);
                    if (y > 52f || y < topY) return false;
                    float cx = Mathf.Lerp(baseX, topX, t * t);
                    return Mathf.Abs(x - cx) < 1.1f;
                }, trunk, 1f, (x, y, c) => y % 3 == 0 ? Darker(c, 0.8f) : c);
            }

            foreach ((float bx, float tx, float ty) in stems)
            {
                // Palmes pennées qui partent en étoile et retombent : nervure courbe, folioles en peigne.
                for (int i = 0; i < 6; i++)
                {
                    float angle = i / 6f * Mathf.PI * 2f + 0.3f;
                    float dx = Mathf.Cos(angle) * 9f, dy = Mathf.Sin(angle) * 4f - 2f;
                    var start = new Vector2(tx, ty);
                    var mid = new Vector2(tx + dx * 0.6f, ty + dy * 0.6f - 2f);
                    var end = new Vector2(tx + dx, ty + dy + 4f);   // la pointe retombe
                    Color tone = i % 2 == 0 ? frond : Darker(frond, 1.12f);
                    cv.Piece((x, y) =>
                    {
                        var q = new Vector2(x, y);
                        float d1 = DistanceToSegment(q, start, mid), d2 = DistanceToSegment(q, mid, end);
                        float t = d1 < d2 ? 0.3f * Vector2.Distance(q, start) / Mathf.Max(0.1f, Vector2.Distance(mid, start)) : 0.6f + 0.4f * Vector2.Distance(q, mid) / Mathf.Max(0.1f, Vector2.Distance(end, mid));
                        float d = Mathf.Min(d1, d2);
                        float w = Mathf.Lerp(2.6f, 0.8f, t);
                        bool comb = d > 0.9f && ((int)(x + y) % 2 == 0);
                        return d < w && !comb && t > 0.08f;
                    }, tone, 0.8f, (x, y, c) => DistanceToSegment(new Vector2(x, y), start, mid) < 0.5f || DistanceToSegment(new Vector2(x, y), mid, end) < 0.5f ? Darker(c, 0.75f) : c);
                }

                cv.Piece(Union(Ellipse(tx + 1.5f, ty + 2.5f, 1.4f, 1.8f), Ellipse(tx - 1f, ty + 3f, 1.2f, 1.5f)), C(0.34f, 0.16f, 0.36f), 1f);   // grappes d'açaí
            }

            Color[,] p = cv.Finish(Ink, 0.45f);
            GroundShadow(p, 16f, 3f, 8f, 2.5f);
            return p;
        }

        public const int BananaWidth = 32;
        public const int BananaHeight = 36;
        public static readonly Vector2 BananaPivot = new Vector2(16f / BananaWidth, 3f / BananaHeight);

        /// <summary>Bananier (32×36) : grandes feuilles en pagaie, nervure claire, bords déchirés, régime de bananes.</summary>
        public static Color[,] Banana()
        {
            var cv = new VolumeCanvas(BananaWidth, BananaHeight);
            Color leaf = C(0.36f, 0.62f, 0.22f);
            cv.Piece(Capsule(16f, 16f, 16f, 33f, 2.4f), C(0.48f, 0.56f, 0.28f), 1.5f, (x, y, c) => y % 4 == 0 ? Darker(c, 0.85f) : c);
            // Cinq grandes feuilles en pagaie : les deux du fond d'abord, puis celles qui retombent devant.
            (float bx, float by, float tone)[] leaves = { (6f, 2f, 0.9f), (26f, 2f, 0.9f), (1f, 13f, 1f), (31f, 12f, 1f), (16f, 1f, 1.1f) };
            var a = new Vector2(16f, 16f);
            foreach ((float bx, float by, float tone) in leaves)
            {
                var b = new Vector2(bx, by);
                cv.Piece((x, y) =>
                {
                    var q = new Vector2(x, y);
                    float t = Mathf.Clamp01(Vector2.Dot(q - a, b - a) / (b - a).sqrMagnitude);
                    float w = Mathf.Sin(Mathf.Pow(t, 0.7f) * Mathf.PI) * 4.4f;
                    float d = DistanceToSegment(q, a, b);
                    bool slit = t > 0.45f && d > 1.2f && Mathf.Abs(Mathf.Repeat(t * 9f, 1f) - 0.5f) < 0.07f;   // déchirures
                    return t > 0.18f && d < w && !slit;
                }, Darker(leaf, tone), 2.5f, (x, y, c) => DistanceToSegment(new Vector2(x, y), a, b) < 0.6f ? C(0.70f, 0.82f, 0.42f) : c);
            }

            cv.Piece(Ellipse(19f, 19f, 2.6f, 3.4f), C(0.62f, 0.72f, 0.24f), 1.5f, (x, y, c) => (y % 2 == 0) ? Darker(c, 0.85f) : c);   // régime
            cv.Piece(Ellipse(19f, 24f, 1.2f, 1.6f), C(0.50f, 0.16f, 0.24f), 1f);                                                       // fleur
            Color[,] p = cv.Finish(Ink, 0.45f);
            GroundShadow(p, 16f, 2.5f, 9f, 2.5f);
            return p;
        }

        /// <summary>Touffe d'héliconias debout (16×24) : longues feuilles, bractées rouges en zigzag au bout jaune.</summary>
        public static Color[,] Heliconia(int variant)
        {
            var cv = new VolumeCanvas(16, 24);
            Color leaf = C(0.22f, 0.50f, 0.20f);
            cv.Piece(Union(Capsule(8f, 22f, 2f, 7f, 1.6f), Capsule(8f, 22f, 14f, 9f, 1.6f), Capsule(8f, 22f, 6f, 4f, 1.4f)), leaf, 1.5f,
                (x, y, c) => (x + y) % 5 == 0 ? Darker(c, 0.8f) : c);
            Color bract = variant == 0 ? C(0.92f, 0.20f, 0.16f) : C(0.96f, 0.52f, 0.14f);
            for (int k = 0; k < 4; k++)
            {
                float by = 8f + k * 3f;
                float bx = 9.5f + (k % 2 == 0 ? 1.5f : -1.5f);
                cv.Piece(Ellipse(bx, by, 1.8f, 1.2f), bract, 1f, (x, y, c) => x > bx + 0.5f == (k % 2 == 0) ? C(0.98f, 0.84f, 0.24f) : c);
            }

            Color[,] p = cv.Finish(Ink, 0.45f);
            GroundShadow(p, 8f, 1.5f, 5f, 1.5f);
            return p;
        }

        /// <summary>Grandes feuilles du sous-bois (16×16, en cœur, on passe à travers) ; variante 1 tachée de blanc.</summary>
        public static Color[,] GiantLeaves(int variant)
        {
            var cv = new VolumeCanvas(16, 16);
            Color leaf = variant == 0 ? C(0.20f, 0.46f, 0.20f) : C(0.26f, 0.50f, 0.22f);
            foreach ((float cx, float cy, float r) in new[] { (5f, 8f, 4.4f), (11f, 7f, 4.2f), (8.5f, 11.5f, 3.6f) })
            {
                float lx = cx, ly = cy, lr = r;
                cv.Piece((x, y) => Ellipse(lx, ly, lr, lr * 0.8f)(x, y) && !(y < ly - lr * 0.4f && Mathf.Abs(x - lx) < 0.8f), leaf, 2f,
                    (x, y, c) => Mathf.Abs(x - lx) < 0.5f ? C(0.50f, 0.70f, 0.36f) : variant == 1 && Hash(x, y, 9) < 0.12f ? C(0.82f, 0.88f, 0.76f) : c);
            }

            return cv.Finish(Ink, 0.45f);
        }

        public const int LogWidth = 40;
        public const int LogHeight = 20;
        public static readonly Vector2 LogPivot = new Vector2(8f / LogWidth, 4f / LogHeight);

        /// <summary>Tronc couché (40×20, sur deux cases et demie) : écorce moussue, champignons, souche fendue.</summary>
        public static Color[,] FallenLog()
        {
            var cv = new VolumeCanvas(LogWidth, LogHeight);
            cv.Piece(Capsule(5f, 11f, 35f, 10f, 4.2f), Bark, 3f, (x, y, c) =>
            {
                if (y < 8f && Hash(x, y, 4) < 0.6f) return C(0.30f, 0.50f, 0.20f);   // mousse sur le dessus
                return (x * 2 + y) % 6 == 0 ? Darker(c, 0.8f) : c;
            });
            cv.Piece(Ellipse(36f, 10f, 3.2f, 3.8f), C(0.74f, 0.60f, 0.42f), 1.5f, (x, y, c) =>
                Mathf.Abs(Mathf.Sqrt((x - 36f) * (x - 36f) + (y - 10f) * (y - 10f)) % 1.6f) < 0.5f ? Darker(c, 0.8f) : c);   // cernes
            foreach (float mx in new[] { 13f, 17f, 26f })
            {
                cv.Piece(Ellipse(mx, 6.5f, 2f, 1.2f), C(0.92f, 0.60f, 0.24f), 1f);   // champignons orange
            }

            Color[,] p = cv.Finish(Ink, 0.5f);
            GroundShadow(p, 20f, 3f, 18f, 2.5f);
            return p;
        }

        // ---------- Outils ----------

        /// <summary>Ombre ovale au sol (y depuis le bas), seulement là où l'image est vide.</summary>
        private static void GroundShadow(Color[,] p, float cx, float cy, float rx, float ry)
        {
            for (int x = 0; x < p.GetLength(0); x++)
            {
                for (int y = 0; y < p.GetLength(1); y++)
                {
                    float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                    if (u * u + v * v <= 1f && p[x, y].a < 0.01f) p[x, y] = C(0.02f, 0.08f, 0.03f, 0.38f);
                }
            }
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
