using System;
using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Sol et végétation de la Grande Barrière redessinée : piste de terre rouge (16 px par case, comme le sol) ;
    /// eucalyptus, pandanus, palétuvier, grand arbre de la forêt tropicale, fougère arborescente, termitière cathédrale,
    /// buisson d'acacia doré, blocs de granit rose et panneau « attention crocodiles » (32 px par case, style en volume) ;
    /// fleurs de brousse (16 px par case).
    /// </summary>
    public static class ReefTerrainArt
    {
        private const int S = 16;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Ink = C(0.08f, 0.14f, 0.08f);

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        /// <summary>Ombre ronde au sol (y depuis le bas), sans toucher aux pixels déjà peints.</summary>
        private static void GroundShadow(Color[,] p, float cx, float cy, float rx, float ry)
        {
            for (int x = 0; x < p.GetLength(0); x++)
            {
                for (int y = 0; y < p.GetLength(1); y++)
                {
                    float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                    if (u * u + v * v <= 1f && p[x, y].a < 0.01f) p[x, y] = C(0.06f, 0.12f, 0.06f, 0.32f);
                }
            }
        }

        /// <summary>Feuillage en touffes : ellipses posées une à une, texture de feuilles.</summary>
        private static void Foliage(VolumeCanvas cv, IEnumerable<(float x, float y, float r)> clumps, Color leaf, float squash, Func<int, int, Color, Color> texture, float bulge = 6f)
        {
            int k = 0;
            foreach ((float x, float y, float r) in clumps)
            {
                cv.Piece(Ellipse(x, y, r, r * squash), k++ % 3 == 1 ? Darker(leaf, 0.88f) : leaf, bulge, texture);
            }
        }

        // ---------- Sol ----------

        /// <summary>Terre rouge tassée de la piste (périodique) : ocre rouge, cailloux clairs, traces plus sombres.</summary>
        public static Color[,] TrackFill(int variant)
        {
            var p = new Color[S, S];
            for (int x = 0; x < S; x++)
            {
                for (int y = 0; y < S; y++)
                {
                    float h = Hash(x, y, 40 + variant);
                    p[x, y] = h < 0.12f ? C(0.58f, 0.26f, 0.14f) : h > 0.95f ? C(0.86f, 0.62f, 0.44f) : h > 0.86f ? C(0.74f, 0.38f, 0.22f) : C(0.68f, 0.32f, 0.18f);
                }
            }

            return p;
        }

        // ---------- Eucalyptus ----------

        public const int EucalyptusWidth = 96;
        public const int EucalyptusHeight = 120;
        public static readonly Vector2 EucalyptusPivot = new Vector2(0.5f, 6f / EucalyptusHeight);

        /// <summary>
        /// Eucalyptus : grand tronc pâle et lisse qui se divise, écorce qui pèle en lanières, feuillage gris-vert clairsemé
        /// en touffes séparées (on voit le ciel entre). <paramref name="variant"/> change la ramure.
        /// </summary>
        public static Color[,] Eucalyptus(int variant)
        {
            var cv = new VolumeCanvas(EucalyptusWidth, EucalyptusHeight);
            Color bark = C(0.88f, 0.84f, 0.78f);
            Func<int, int, Color, Color> peel = (x, y, c) =>
                Mathf.PerlinNoise(x * 0.25f, y * 0.06f + variant) > 0.62f ? C(0.70f, 0.56f, 0.44f) : (x + y * 3) % 17 == 0 ? Darker(c, 0.86f) : c;
            float lean = variant == 0 ? 1f : -1f;
            cv.Piece(Union(Capsule(48f, 116f, 48f + lean * 3f, 64f, 4.6f),
                    Capsule(48f + lean * 3f, 66f, 30f + lean * 4f, 36f, 3f), Capsule(48f + lean * 3f, 66f, 66f + lean * 4f, 32f, 3f),
                    Capsule(48f + lean * 3f, 70f, 50f + lean * 8f, 24f, 2.4f), Capsule(32f + lean * 4f, 40f, 20f, 30f, 1.6f), Capsule(64f + lean * 4f, 36f, 78f, 28f, 1.6f)),
                bark, 3.5f, peel);
            Color leaf = C(0.44f, 0.58f, 0.46f);
            Func<int, int, Color, Color> leaves = (x, y, c) =>
                Hash(x / 2, y / 2, 50 + variant) < 0.16f ? C(0, 0, 0, 0) : (x * 5 + y * 3) % 7 == 0 ? Darker(c, 1.2f) : Hash(x, y, 52) < 0.2f ? Darker(c, 0.84f) : c;
            var clumps = variant == 0
                ? new[] { (20f, 28f, 13f), (48f, 16f, 14f), (76f, 24f, 13f), (34f, 42f, 10f), (64f, 44f, 10f), (54f, 30f, 10f) }
                : new[] { (22f, 22f, 12f), (46f, 14f, 13f), (74f, 30f, 12f), (30f, 40f, 10f), (60f, 40f, 11f), (40f, 28f, 9f) };
            Foliage(cv, clumps, leaf, 0.62f, leaves);
            Color[,] p = cv.Finish(Ink, 0.5f);
            GroundShadow(p, 50f, 5f, 26f, 4.5f);
            return p;
        }

        // ---------- Pandanus ----------

        public const int PandanusWidth = 80;
        public const int PandanusHeight = 88;
        public static readonly Vector2 PandanusPivot = new Vector2(0.5f, 6f / PandanusHeight);

        /// <summary>
        /// Pandanus du haut de plage : tronc annelé posé sur ses racines-échasses, branches qui fourchent, touffes de
        /// longues feuilles en spirale qui retombent, fruits orange en ananas (variante 1).
        /// </summary>
        public static Color[,] Pandanus(int variant)
        {
            var cv = new VolumeCanvas(PandanusWidth, PandanusHeight);
            Color trunk = C(0.54f, 0.44f, 0.32f);
            Func<int, int, Color, Color> rings = (x, y, c) => y % 4 == 0 ? Darker(c, 0.72f) : y % 4 == 1 ? Darker(c, 1.12f) : c;
            // Racines-échasses.
            foreach (float rx in new[] { 30f, 36f, 44f, 50f }) cv.Piece(Capsule(40f, 66f, rx, 84f, 1.4f), Darker(trunk, 0.9f), 1.5f);
            cv.Piece(Union(Capsule(40f, 70f, 40f, 44f, 3.4f), Capsule(40f, 46f, 24f, 30f, 2.4f), Capsule(40f, 46f, 56f, 26f, 2.4f)), trunk, 3f, rings);
            Color leaf = variant == 0 ? C(0.34f, 0.58f, 0.30f) : C(0.40f, 0.60f, 0.28f);
            foreach ((float cx, float cy) in new[] { (24f, 28f), (56f, 24f), (40f, 18f) })
            {
                for (int f = 0; f < 11; f++)
                {
                    float a = (f / 11f) * Mathf.PI * 2f + cx * 0.1f;
                    float len = 15f + (f % 3) * 2f;
                    float ex = cx + Mathf.Cos(a) * len, ey = cy - Mathf.Sin(a) * len * 0.5f + len * 0.45f;   // les feuilles retombent
                    cv.Piece(Capsule(cx, cy, ex, ey, 1.7f), f % 2 == 0 ? leaf : Darker(leaf, 0.84f), 1.5f,
                        (x, y, c) => (x + y) % 5 == 0 ? Darker(c, 1.2f) : c, 0.85f);
                }
            }

            if (variant == 1)
            {
                cv.Piece(Ellipse(44f, 30f, 5f, 6f), C(0.94f, 0.52f, 0.18f), 3f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.76f) : (x - y) % 3 == 0 ? Darker(c, 1.15f) : c);
            }

            Color[,] p = cv.Finish(Ink, 0.5f);
            GroundShadow(p, 40f, 5f, 20f, 4f);
            return p;
        }

        // ---------- Palétuvier ----------

        public const int MangroveWidth = 80;
        public const int MangroveHeight = 80;
        public static readonly Vector2 MangrovePivot = new Vector2(0.5f, 8f / MangroveHeight);

        /// <summary>Palétuvier de la crique : racines-échasses arquées qui plongent dans la vase, couronne vert sombre et dense, propagules.</summary>
        public static Color[,] Mangrove(int variant)
        {
            var cv = new VolumeCanvas(MangroveWidth, MangroveHeight);
            Color root = C(0.44f, 0.34f, 0.24f);
            for (int r = -3; r <= 3; r++)
            {
                var segments = new List<Func<float, float, bool>>();
                Vector2 Point(float t) => new Vector2(40f + r * 11f * t, 50f + t * 24f - Mathf.Sin(t * Mathf.PI) * 8f);
                for (int i = 0; i < 6; i++)
                {
                    Vector2 a = Point(i / 6f), b = Point((i + 1) / 6f);
                    segments.Add(Capsule(a.x, a.y, b.x, b.y, 1.3f));
                }

                cv.Piece(Union(segments.ToArray()), r % 2 == 0 ? root : Darker(root, 0.85f), 1.5f);
            }

            cv.Piece(Capsule(40f, 54f, 40f, 34f, 3f), root, 2.5f);
            Color leaf = variant == 0 ? C(0.18f, 0.40f, 0.22f) : C(0.22f, 0.42f, 0.20f);
            Func<int, int, Color, Color> leaves = (x, y, c) => Hash(x / 2, y / 2, 60 + variant) < 0.22f ? Darker(c, 0.8f) : (x * 3 + y) % 7 == 0 ? Darker(c, 1.22f) : c;
            Foliage(cv, new[] { (22f, 30f, 14f), (58f, 30f, 14f), (40f, 20f, 17f), (40f, 36f, 12f) }, leaf, 0.66f, leaves);
            Color[,] p = cv.Finish(C(0.04f, 0.10f, 0.06f), 0.5f);
            // Propagules qui pendent (y depuis le bas).
            foreach (int px in new[] { 18, 30, 47, 60 })
            {
                for (int k = 0; k < 4; k++) if (p[px, 46 - k].a < 0.5f) p[px, 46 - k] = C(0.40f, 0.50f, 0.20f);
            }

            return p;
        }

        // ---------- Forêt tropicale ----------

        public const int RainTreeWidth = 128;
        public const int RainTreeHeight = 136;
        public static readonly Vector2 RainTreePivot = new Vector2(0.5f, 8f / RainTreeHeight);

        /// <summary>
        /// Grand arbre de la forêt tropicale : contreforts qui s'étalent au pied, haut tronc gris tacheté de lichen, lianes
        /// qui pendent, fougère nid-d'oiseau accrochée au tronc, couronne immense en étages de touffes vert profond.
        /// </summary>
        public static Color[,] RainTree(int variant)
        {
            var cv = new VolumeCanvas(RainTreeWidth, RainTreeHeight);
            Color bark = C(0.50f, 0.48f, 0.42f);
            Func<int, int, Color, Color> lichen = (x, y, c) => Hash(x / 3, y / 3, 70) < 0.12f ? C(0.70f, 0.74f, 0.62f) : x % 7 == 0 ? Darker(c, 0.84f) : c;
            // Contreforts (planches qui s'étalent) et tronc.
            // Contreforts : le pied s'évase en ailes de bois qui plongent dans le sol, en creux entre elles.
            Func<float, float, bool> buttress = (x, y) =>
            {
                if (y < 96 || y > 131) return false;
                float t = (y - 96f) / 35f, dx = Mathf.Abs(x - 64f);
                float spread = 7f + t * t * 26f;
                float notch = Mathf.Abs(Mathf.Sin((x - 64f) * 0.24f));
                return dx < spread && (dx < 7f || notch > 0.35f + (1f - t) * 0.5f || y > 128f);
            };
            cv.Piece(buttress, Darker(bark, 0.92f), 4f, lichen);
            cv.Piece(Capsule(64f, 120f, 64f + (variant == 0 ? 2f : -2f), 50f, 7f), bark, 5f, lichen);
            // Fougère nid-d'oiseau sur le tronc.
            for (int f = 0; f < 7; f++)
            {
                float a = Mathf.PI * (0.1f + f * 0.13f);
                cv.Piece(Capsule(64f, 86f, 64f + Mathf.Cos(a) * 16f, 86f - Mathf.Sin(a) * 9f, 2f), C(0.46f, 0.66f, 0.26f), 1.5f, null, 0.85f);
            }

            Color leaf = variant == 0 ? C(0.14f, 0.38f, 0.18f) : C(0.18f, 0.42f, 0.16f);
            Func<int, int, Color, Color> leaves = (x, y, c) => Hash(x / 3, y / 3, 72 + variant) < 0.2f ? Darker(c, 0.8f) : Hash(x, y, 73) < 0.08f ? Darker(c, 1.25f) : c;
            var clumps = new List<(float, float, float)>
            {
                (26f, 52f, 20f), (102f, 50f, 20f), (64f, 56f, 22f), (40f, 34f, 22f), (88f, 32f, 22f), (64f, 22f, 24f),
                (variant == 0 ? 20f : 108f, 30f, 14f),
            };
            Foliage(cv, clumps, leaf, 0.7f, leaves, 8f);
            Color[,] p = cv.Finish(C(0.03f, 0.10f, 0.05f), 0.5f);
            // Lianes qui pendent de la couronne (y depuis le bas).
            foreach (int lx in variant == 0 ? new[] { 30, 50, 94 } : new[] { 36, 78, 100 })
            {
                int top = 0;
                for (int y = 0; y < RainTreeHeight; y++) if (p[lx, y].a > 0.5f && y > 60) { top = y; break; }
                for (int y = top - 1; y > top - 26 && y > 10; y--) if (p[lx, y].a < 0.5f) p[lx, y] = (y % 5 == 0) ? C(0.30f, 0.50f, 0.20f) : C(0.22f, 0.36f, 0.16f);
            }

            GroundShadow(p, 64f, 6f, 40f, 6f);
            return p;
        }

        public const int TreeFernWidth = 80;
        public const int TreeFernHeight = 88;
        public static readonly Vector2 TreeFernPivot = new Vector2(0.5f, 6f / TreeFernHeight);

        /// <summary>Fougère arborescente : fin tronc fibreux, couronne de grandes frondes découpées qui s'arquent, crosses au centre.</summary>
        public static Color[,] TreeFern(int variant)
        {
            var cv = new VolumeCanvas(TreeFernWidth, TreeFernHeight);
            Color trunk = C(0.36f, 0.26f, 0.18f);
            cv.Piece(Capsule(40f, 84f, 40f + (variant == 0 ? 3f : -3f), 34f, 3.2f), trunk, 3f, (x, y, c) => (x + y * 2) % 5 == 0 ? Darker(c, 0.7f) : c);
            Color frond = variant == 0 ? C(0.32f, 0.62f, 0.26f) : C(0.28f, 0.56f, 0.24f);
            var crown = new Vector2(40f + (variant == 0 ? 3f : -3f), 32f);
            float[] angles = { 0f, 35f, 70f, 110f, 145f, 180f, 215f, 325f, 90f };
            foreach (float deg in angles)
            {
                float a = deg * Mathf.Deg2Rad;
                var pts = new List<Vector2>();
                for (int i = 0; i <= 6; i++)
                {
                    float t = i / 6f;
                    pts.Add(new Vector2(crown.x + Mathf.Cos(a) * 30f * t, crown.y - Mathf.Sin(a) * 18f * t + t * t * 14f));
                }

                var segs = new List<Func<float, float, bool>>();
                for (int i = 0; i < 6; i++) segs.Add(Capsule(pts[i].x, pts[i].y, pts[i + 1].x, pts[i + 1].y, Mathf.Lerp(4f, 1f, i / 6f)));
                Func<float, float, bool> shape = Union(segs.ToArray());
                cv.Piece((x, y) => shape(x, y) && ((int)(x + y) % 3 != 0 || Vector2.Distance(new Vector2(x, y), crown) < 5f), frond, 2f,
                    (x, y, c) => (x * 3 + y) % 4 == 0 ? Darker(c, 1.2f) : c, 0.8f);
            }

            cv.Piece(Ellipse(crown.x, crown.y + 1f, 4f, 3f), C(0.46f, 0.40f, 0.24f), 2f);   // crosses
            Color[,] p = cv.Finish(Ink, 0.5f);
            GroundShadow(p, 40f, 5f, 18f, 3.5f);
            return p;
        }

        // ---------- Termitière cathédrale ----------

        public const int TermiteWidth = 56;
        public const int TermiteHeight = 88;
        public static readonly Vector2 TermitePivot = new Vector2(0.5f, 6f / TermiteHeight);

        /// <summary>Termitière cathédrale : grande tour de terre ocre rouge, flèches et cannelures, base évasée, herbe sèche autour.</summary>
        public static Color[,] Termite(int variant)
        {
            var cv = new VolumeCanvas(TermiteWidth, TermiteHeight);
            Color earth = variant == 0 ? C(0.74f, 0.42f, 0.24f) : C(0.70f, 0.48f, 0.30f);
            Func<int, int, Color, Color> flutes = (x, y, c) => x % 5 == 0 ? Darker(c, 0.78f) : Hash(x, y, 80) < 0.08f ? Darker(c, 1.15f) : (y % 9 == 0 && x % 3 == 0) ? Darker(c, 0.86f) : c;
            Func<float, float, bool> Spire(float cx, float top, float baseHalf) => (x, y) =>
            {
                float t = Mathf.Clamp01((y - top) / (84f - top));
                return y > top && y < 84f && Mathf.Abs(x - cx) < 2f + baseHalf * Mathf.Sqrt(t) + Mathf.Sin(y * 0.5f + cx) * 0.8f;
            };
            cv.Piece(Spire(variant == 0 ? 20f : 34f, 26f, 9f), Darker(earth, 0.92f), 4f, flutes);
            cv.Piece(Spire(28f, 6f, 14f), earth, 5f, flutes);
            cv.Piece(Spire(variant == 0 ? 38f : 18f, 34f, 8f), earth, 4f, flutes);
            Color[,] p = cv.Finish(C(0.20f, 0.10f, 0.06f), 0.5f);
            // Herbe sèche au pied.
            for (int x = 6; x < 50; x += 3)
            {
                int hgt = 3 + (int)(Hash(x, 0, 81 + variant) * 4f);
                for (int y = 4; y < 4 + hgt; y++) if (p[x, y].a < 0.5f) p[x, y] = C(0.78f, 0.70f, 0.36f);
            }

            GroundShadow(p, 28f, 5f, 22f, 4f);
            return p;
        }

        // ---------- Acacia doré ----------

        public const int WattleWidth = 48;
        public const int WattleHeight = 40;
        public static readonly Vector2 WattlePivot = new Vector2(0.5f, 4f / WattleHeight);

        /// <summary>Buisson d'acacia doré (wattle) : feuillage fin vert bleuté couvert de pompons jaune vif.</summary>
        public static Color[,] Wattle(int variant)
        {
            var cv = new VolumeCanvas(WattleWidth, WattleHeight);
            Color leaf = C(0.40f, 0.54f, 0.40f);
            Func<float, float, bool> body = variant == 0
                ? Union(Ellipse(24f, 22f, 18f, 11f), Ellipse(14f, 26f, 9f, 7f), Ellipse(34f, 18f, 10f, 8f))
                : Union(Ellipse(22f, 24f, 16f, 10f), Ellipse(32f, 20f, 11f, 9f), Ellipse(12f, 22f, 8f, 6f));
            cv.Piece(body, leaf, 6f, (x, y, c) => (x * 3 + y) % 5 == 0 ? Darker(c, 0.82f) : c);
            Color[,] p = cv.Finish(Ink, 0.5f);
            for (int i = 0; i < 26; i++)
            {
                int fx = 4 + (int)(Hash(i, variant, 82) * 40f), fy = 8 + (int)(Hash(i, variant, 83) * 26f);
                if (p[fx, fy].a < 0.5f) continue;
                PixelArt.Set(p, fx, fy, C(1f, 0.86f, 0.16f));
                PixelArt.Set(p, fx + 1, fy, C(0.96f, 0.74f, 0.10f));
                PixelArt.Set(p, fx, fy + 1, C(1f, 0.94f, 0.44f));
            }

            GroundShadow(p, 24f, 4f, 19f, 3.5f);
            return p;
        }

        // ---------- Granit rose ----------

        public const int RockWidth = 48;
        public const int RockHeight = 40;
        public static readonly Vector2 RockPivot = new Vector2(0.5f, 6f / RockHeight);

        /// <summary>Bloc de granit rose arrondi par la mer, grains noirs et blancs, lichen orange sur le dessus.</summary>
        public static Color[,] Rock(int variant)
        {
            var cv = new VolumeCanvas(RockWidth, RockHeight);
            Color granite = C(0.80f, 0.62f, 0.56f);
            Func<int, int, Color, Color> grain = (x, y, c) =>
            {
                float h = Hash(x, y, 90 + variant);
                return h < 0.07f ? C(0.26f, 0.22f, 0.22f) : h > 0.94f ? C(0.96f, 0.92f, 0.88f) : y < 14 && Hash(x / 2, y / 2, 91) < 0.12f ? C(0.92f, 0.56f, 0.18f) : c;
            };
            if (variant == 0) cv.Piece(Union(Ellipse(22f, 24f, 18f, 12f), Ellipse(34f, 28f, 11f, 8f)), granite, 9f, grain);
            else
            {
                cv.Piece(Ellipse(18f, 26f, 13f, 10f), Darker(granite, 0.94f), 8f, grain);
                cv.Piece(Ellipse(31f, 22f, 13f, 13f), granite, 10f, grain);
            }

            Color[,] p = cv.Finish(C(0.20f, 0.12f, 0.12f), 0.5f);
            GroundShadow(p, 24f, 5f, 20f, 4f);
            return p;
        }

        // ---------- Panneau crocodiles ----------

        public const int SignWidth = 40;
        public const int SignHeight = 56;
        public static readonly Vector2 SignPivot = new Vector2(0.5f, 4f / SignHeight);

        /// <summary>Panneau « attention crocodiles » sans texte : losange jaune bordé de noir, crocodile dessiné en noir, sur un poteau.</summary>
        public static Color[,] CrocSign()
        {
            var cv = new VolumeCanvas(SignWidth, SignHeight);
            cv.Piece(Box(18f, 22f, 22f, 54f), C(0.56f, 0.56f, 0.58f), 1.5f);
            Func<float, float, bool> diamond = (x, y) => Mathf.Abs(x - 20f) + Mathf.Abs(y - 18f) < 16f;
            cv.Piece(diamond, C(0.14f, 0.12f, 0.10f), 1.5f);
            cv.Piece((x, y) => Mathf.Abs(x - 20f) + Mathf.Abs(y - 18f) < 13.5f, C(0.98f, 0.82f, 0.16f), 1.5f);
            Color[,] p = cv.Finish(C(0.10f, 0.08f, 0.06f), 0.5f);
            // Crocodile de profil (y depuis le bas : le centre du losange est à y = 55 - 18 = 37).
            Color black = C(0.10f, 0.08f, 0.06f);
            for (int x = 11; x <= 29; x++)
            {
                int thick = x < 15 ? 1 : x > 24 ? 1 : 2;
                for (int k = 0; k < thick; k++) PixelArt.Set(p, x, 37 + k + (x < 15 ? 0 : 0), black);
            }

            foreach ((int x, int y) in new[] { (27, 39), (28, 39), (16, 35), (17, 35), (22, 35), (23, 35), (12, 38), (30, 37), (19, 39), (22, 39) }) PixelArt.Set(p, x, y, black);
            GroundShadow(p, 20f, 3f, 7f, 2f);
            return p;
        }

        // ---------- Petit décor (16 px par case) ----------

        /// <summary>Fleurs de brousse : 0 immortelles roses et blanches, 1 touffe de kangourou-patte rouge.</summary>
        public static Color[,] Flowers(int variant)
        {
            var p = new Color[S, S];
            Color leaf = C(0.36f, 0.50f, 0.24f), leafLight = C(0.48f, 0.60f, 0.30f);
            if (variant == 0)
            {
                foreach ((int x, int y) in new[] { (4, 4), (5, 5), (10, 4), (11, 5), (7, 3), (12, 8), (3, 8) }) { PixelArt.Set(p, x, y, leaf); PixelArt.Set(p, x, y + 1, leafLight); }
                foreach ((int cx, int cy, bool pink) in new[] { (5, 8, true), (11, 10, false), (8, 6, true) })
                {
                    Color petal = pink ? C(0.96f, 0.62f, 0.74f) : C(1f, 0.98f, 0.94f);
                    PixelArt.Set(p, cx - 1, cy, petal);
                    PixelArt.Set(p, cx + 1, cy, petal);
                    PixelArt.Set(p, cx, cy + 1, petal);
                    PixelArt.Set(p, cx, cy - 1, Darker(petal, 0.85f));
                    PixelArt.Set(p, cx, cy, C(0.98f, 0.78f, 0.20f));
                }
            }
            else
            {
                // Feuilles en lame à la base, tiges fines, grappes de fleurs-tubes rouges recourbées comme des pattes.
                foreach ((int x, int y) in new[] { (5, 3), (6, 4), (10, 3), (9, 4), (8, 3), (7, 5) }) PixelArt.Set(p, x, y, x % 2 == 0 ? leaf : leafLight);
                Color stem = C(0.30f, 0.40f, 0.20f), red = C(0.88f, 0.16f, 0.14f), redDark = C(0.62f, 0.10f, 0.10f), tip = C(0.40f, 0.62f, 0.26f);
                foreach ((int sx, int top, int bend) in new[] { (6, 10, -1), (9, 12, 1) })
                {
                    for (int y = 4; y < top - 2; y++) PixelArt.Set(p, sx, y, stem);
                    for (int k = 0; k < 3; k++)
                    {
                        int fx = sx + bend * k, fy = top - 2 + k;
                        PixelArt.Set(p, fx, fy, red);
                        PixelArt.Set(p, fx - bend, fy, redDark);
                        PixelArt.Set(p, fx + bend, fy + 1, k == 2 ? tip : red);
                    }
                }
            }

            return p;
        }
    }
}
