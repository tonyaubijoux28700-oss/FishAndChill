using System;
using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Sol et végétation de l'atoll redessiné : chemin de sable corallien (16 px par case, comme le sol), cocotiers droits
    /// et penchés vers l'eau, frangipanier en fleurs, buisson de veloutier (32 px par case, style en volume), fleurs
    /// d'hibiscus et d'ipomée, coquillages et coraux blanchis échoués (16 px par case).
    /// </summary>
    public static class AtollTerrainArt
    {
        private const int S = 16;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Ink = C(0.08f, 0.16f, 0.08f);

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        private static void GroundShadow(Color[,] p, float cx, float cy, float rx, float ry)
        {
            for (int x = 0; x < p.GetLength(0); x++)
            {
                for (int y = 0; y < p.GetLength(1); y++)
                {
                    float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                    if (u * u + v * v <= 1f && p[x, y].a < 0.01f) p[x, y] = C(0.05f, 0.16f, 0.10f, 0.32f);
                }
            }
        }

        // ---------- Sol ----------

        /// <summary>Sable corallien tassé du chemin (périodique) : blanc crème, éclats de corail rose et coquillages.</summary>
        public static Color[,] PathFill(int variant)
        {
            var p = new Color[S, S];
            for (int x = 0; x < S; x++)
            {
                for (int y = 0; y < S; y++)
                {
                    float h = Hash(x, y, 90 + variant);
                    p[x, y] = h < 0.1f ? C(0.84f, 0.80f, 0.70f) : h > 0.95f ? C(0.96f, 0.80f, 0.78f) : h > 0.9f ? C(1f, 1f, 0.98f) : C(0.94f, 0.91f, 0.82f);
                }
            }

            return p;
        }

        // ---------- Cocotiers ----------

        public const int PalmWidth = 96;
        public const int PalmHeight = 88;

        /// <summary>Pied du tronc, au centre de la case.</summary>
        public static readonly Vector2 PalmPivot = new Vector2(0.5f, 6f / PalmHeight);

        /// <summary>
        /// Cocotier en volume : tronc annelé, palmes arquées à nervure claire et folioles, noix de coco.
        /// <paramref name="lean"/> : -1 penché vers la gauche, 0 presque droit, 1 penché vers la droite (vers l'eau) ;
        /// <paramref name="variant"/> change la couronne.
        /// </summary>
        public static Color[,] Palm(int lean, int variant)
        {
            var cv = new VolumeCanvas(PalmWidth, PalmHeight);
            Color bark = C(0.60f, 0.46f, 0.30f), leaf = variant == 0 ? C(0.30f, 0.64f, 0.28f) : C(0.36f, 0.66f, 0.24f), coconut = C(0.50f, 0.34f, 0.16f);
            float rise = lean == 0 ? 54f : 44f;
            Vector2 Point(float t) => new Vector2(48f + lean * t * t * 28f + (lean == 0 ? Mathf.Sin(t * 2.2f) * (variant == 0 ? 5f : -5f) : 0f), 82f - t * rise);

            var trunk = new List<Func<float, float, bool>>();
            for (int i = 0; i < 12; i++)
            {
                Vector2 a = Point(i / 12f), b = Point((i + 1) / 12f);
                trunk.Add(Capsule(a.x, a.y, b.x, b.y, Mathf.Lerp(4.4f, 2.6f, i / 12f)));
            }

            cv.Piece(Union(trunk.ToArray()), bark, 3.5f,
                (x, y, c) => (y + (int)(x * 0.3f * lean)) % 4 == 0 ? Darker(c, 0.72f) : (y + (int)(x * 0.3f * lean)) % 4 == 1 ? Darker(c, 1.12f) : (x * 7 + y) % 9 == 0 ? Darker(c, 0.9f) : c);

            Vector2 crown = Point(1f);
            float[] angles = variant == 0 ? new[] { 200f, 160f, 235f, 125f, 270f, 95f, 310f, 60f, 20f, 340f } : new[] { 190f, 150f, 225f, 115f, 262f, 80f, 300f, 45f, 10f, 330f, 245f };
            for (int k = 0; k < angles.Length; k++)
            {
                float a = angles[k] * Mathf.Deg2Rad;
                float length = 22f + (k % 3) * 3.5f;
                var points = new List<Vector2>();
                for (int i = 0; i <= 8; i++)
                {
                    float t = i / 8f;
                    points.Add(new Vector2(crown.x + Mathf.Cos(a) * length * t, crown.y - Mathf.Sin(a) * length * t * 0.55f + t * t * 11f));
                }

                var segments = new List<Func<float, float, bool>>();
                for (int i = 0; i < 8; i++) segments.Add(Capsule(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, Mathf.Lerp(3.8f, 0.8f, i / 8f)));
                Func<float, float, bool> frond = Union(segments.ToArray());
                float cosA = Mathf.Cos(a), sinA = Mathf.Sin(a);
                bool Leaflet(float x, float y)
                {
                    float d = Vector2.Distance(new Vector2(x, y), crown);
                    return d < 5f || (int)(Mathf.Atan2(y - crown.y, x - crown.x) * 40f + d * 1.2f) % 3 != 0;
                }

                cv.Piece((x, y) => frond(x, y) && Leaflet(x, y), k % 2 == 0 ? leaf : Darker(leaf, 0.86f), 2.2f,
                    (x, y, c) => Mathf.Abs((x - crown.x) * -sinA - (y - crown.y) * cosA) < 0.7f ? Darker(c, 1.22f) : c, 0.78f);
            }

            cv.Piece(Ellipse(crown.x - 3f, crown.y + 4f, 2.8f, 2.8f), coconut, 2.4f);
            cv.Piece(Ellipse(crown.x + 2.5f, crown.y + 4.5f, 2.8f, 2.8f), coconut, 2.4f);
            cv.Piece(Ellipse(crown.x - 0.5f, crown.y + 7f, 2.8f, 2.8f), variant == 0 ? coconut : C(0.56f, 0.62f, 0.24f), 2.4f);
            Color[,] p = cv.Finish(C(0.10f, 0.20f, 0.10f));
            // Ombre de la couronne au sol, décalée du côté où penche l'arbre.
            GroundShadow(p, 48f + lean * 18f, 5f, 15f, 3.4f);
            return p;
        }

        // ---------- Frangipanier ----------

        public const int FrangipaniWidth = 64;
        public const int FrangipaniHeight = 72;
        public static readonly Vector2 FrangipaniPivot = new Vector2(0.5f, 5f / FrangipaniHeight);

        /// <summary>Frangipanier : tronc gris qui fourche en branches épaisses, bouquets de feuilles au bout, fleurs blanches à cœur jaune.</summary>
        public static Color[,] Frangipani()
        {
            var cv = new VolumeCanvas(FrangipaniWidth, FrangipaniHeight);
            Color bark = C(0.56f, 0.54f, 0.50f);
            cv.Piece(Union(Capsule(32f, 68f, 31f, 46f, 3.4f), Capsule(31f, 48f, 18f, 30f, 2.4f), Capsule(31f, 48f, 46f, 28f, 2.4f), Capsule(31f, 46f, 33f, 24f, 2.2f),
                Capsule(18f, 32f, 10f, 26f, 1.8f), Capsule(46f, 30f, 54f, 26f, 1.8f)), bark, 2.5f, (x, y, c) => (x * 3 + y * 5) % 11 == 0 ? Darker(c, 0.8f) : c);
            Color leaf = C(0.22f, 0.50f, 0.22f);
            Func<int, int, Color, Color> veins = (x, y, c) => (x + y) % 5 == 0 ? Darker(c, 1.2f) : (x * 3 - y) % 7 == 0 ? Darker(c, 0.82f) : c;
            foreach ((float cx, float cy, float r) in new[] { (11f, 24f, 10f), (32f, 22f, 13f), (52f, 23f, 10f), (22f, 13f, 10f), (42f, 12f, 10f), (32f, 7f, 8f) })
            {
                cv.Piece(Ellipse(cx, cy, r, r * 0.62f), leaf, 5f, veins);
            }

            Color[,] p = cv.Finish(Ink, 0.5f);
            // Fleurs : cinq pétales blancs autour d'un cœur jaune (y depuis le bas).
            Color petal = C(1f, 0.98f, 0.94f), petalShade = C(0.94f, 0.88f, 0.84f), heart = C(1f, 0.82f, 0.28f);
            foreach ((int fx, int fy) in new[] { (8, 50), (18, 46), (28, 52), (38, 49), (48, 52), (56, 48), (22, 59), (40, 60), (32, 64), (13, 55), (51, 57), (31, 44) })
            {
                PixelArt.Rect(p, fx - 1, fy - 1, fx + 1, fy + 1, petal);
                PixelArt.Set(p, fx - 2, fy, petalShade);
                PixelArt.Set(p, fx + 2, fy, petal);
                PixelArt.Set(p, fx, fy + 2, petal);
                PixelArt.Set(p, fx, fy - 2, petalShade);
                PixelArt.Set(p, fx + 1, fy - 1, petalShade);
                PixelArt.Set(p, fx, fy, heart);
                PixelArt.Set(p, fx - 1, fy, heart);
            }

            GroundShadow(p, 32f, 4f, 15f, 3.2f);
            return p;
        }

        // ---------- Buisson de veloutier ----------

        public const int BushWidth = 48;
        public const int BushHeight = 36;
        public static readonly Vector2 BushPivot = new Vector2(0.5f, 4f / BushHeight);

        /// <summary>Buisson de veloutier (scaevola) de lisière de plage : rosettes de feuilles charnues vert clair, petites fleurs blanches en demi-étoile.</summary>
        public static Color[,] Bush(int variant)
        {
            var cv = new VolumeCanvas(BushWidth, BushHeight);
            Color leaf = variant == 0 ? C(0.42f, 0.68f, 0.30f) : C(0.36f, 0.62f, 0.32f);
            Func<int, int, Color, Color> rosettes = (x, y, c) => Hash(x / 3, y / 3, 70 + variant) < 0.25f ? Darker(c, 0.82f) : (x + y * 2) % 7 == 0 ? Darker(c, 1.2f) : c;
            Func<float, float, bool> body = variant == 0
                ? Union(Ellipse(24f, 22f, 18f, 9f), Ellipse(15f, 17f, 9f, 7f), Ellipse(32f, 16f, 10f, 7f))
                : Union(Ellipse(22f, 22f, 16f, 9f), Ellipse(30f, 18f, 11f, 8f), Ellipse(14f, 21f, 8f, 6f));
            cv.Piece(body, leaf, 6f, rosettes);
            Color[,] p = cv.Finish(Ink, 0.5f);
            for (int i = 0; i < 7; i++)
            {
                int fx = 8 + (i * 11 + variant * 5) % 32, fy = 12 + (i * 7) % 16;
                if (p[fx, fy].a < 0.5f) continue;
                PixelArt.Set(p, fx, fy, C(1f, 1f, 0.96f));
                PixelArt.Set(p, fx + 1, fy, C(0.96f, 0.94f, 0.90f));
                PixelArt.Set(p, fx, fy + 1, C(1f, 1f, 0.96f));
            }

            GroundShadow(p, 24f, 4f, 19f, 3.5f);
            return p;
        }

        // ---------- Petit décor (16 px par case) ----------

        /// <summary>Fleurs : 0 hibiscus rouge sur ses feuilles, 1 ipomée rose rampante.</summary>
        public static Color[,] Flowers(int variant)
        {
            var p = new Color[S, S];
            Color leaf = C(0.20f, 0.48f, 0.20f), leafLight = C(0.32f, 0.60f, 0.26f);
            if (variant == 0)
            {
                foreach ((int x, int y) in new[] { (4, 5), (5, 4), (10, 6), (11, 5), (7, 3), (8, 4), (12, 9), (3, 9) }) { PixelArt.Set(p, x, y, leaf); PixelArt.Set(p, x + 1, y, leafLight); }
                foreach ((int cx, int cy) in new[] { (6, 8), (11, 11) })
                {
                    Color petal = C(0.90f, 0.16f, 0.18f);
                    PixelArt.Rect(p, cx - 1, cy - 1, cx + 1, cy + 1, petal);
                    PixelArt.Set(p, cx - 2, cy, Darker(petal, 0.8f));
                    PixelArt.Set(p, cx + 2, cy, Darker(petal, 0.8f));
                    PixelArt.Set(p, cx, cy + 2, Darker(petal, 1.15f));
                    PixelArt.Set(p, cx, cy, C(1f, 0.86f, 0.30f));
                }
            }
            else
            {
                for (int x = 1; x < 15; x++) PixelArt.Set(p, x, 6 + Mathf.RoundToInt(Mathf.Sin(x * 0.7f) * 1.5f), leaf);
                foreach ((int x, int y) in new[] { (3, 8), (8, 4), (12, 9), (6, 9), (10, 7) }) { PixelArt.Set(p, x, y, leafLight); PixelArt.Set(p, x + 1, y, leaf); }
                foreach ((int cx, int cy) in new[] { (4, 10), (11, 10), (8, 7) })
                {
                    Color petal = C(0.88f, 0.46f, 0.76f);
                    PixelArt.Rect(p, cx - 1, cy, cx + 1, cy + 1, petal);
                    PixelArt.Set(p, cx, cy, C(0.98f, 0.86f, 0.96f));
                }
            }

            return p;
        }

        /// <summary>Laisse de mer : 0 coquillages (porcelaine, conque), 1 branche de corail blanchi et petit coquillage.</summary>
        public static Color[,] Shells(int variant)
        {
            var p = new Color[S, S];
            Color shadow = C(0.70f, 0.66f, 0.58f, 0.6f);
            if (variant == 0)
            {
                PixelArt.Rect(p, 3, 5, 6, 7, C(0.86f, 0.70f, 0.52f));
                PixelArt.Set(p, 4, 6, C(0.98f, 0.92f, 0.82f));
                PixelArt.Set(p, 5, 6, C(0.62f, 0.44f, 0.30f));
                for (int x = 3; x <= 6; x++) PixelArt.Set(p, x, 4, shadow);
                PixelArt.Rect(p, 9, 9, 12, 10, C(0.98f, 0.84f, 0.80f));
                PixelArt.Rect(p, 10, 11, 11, 11, C(0.96f, 0.72f, 0.70f));
                PixelArt.Set(p, 13, 10, C(0.92f, 0.62f, 0.60f));
                for (int x = 9; x <= 13; x++) PixelArt.Set(p, x, 8, shadow);
            }
            else
            {
                foreach ((int x, int y) in new[] { (4, 5), (5, 6), (6, 7), (7, 8), (6, 9), (8, 7), (9, 8), (10, 9), (9, 10), (11, 8) }) PixelArt.Set(p, x, y, C(0.98f, 0.96f, 0.92f));
                for (int x = 4; x <= 11; x++) PixelArt.Set(p, x, 4, shadow);
                PixelArt.Rect(p, 11, 3, 12, 4, C(0.90f, 0.78f, 0.62f));
            }

            return p;
        }
    }
}
