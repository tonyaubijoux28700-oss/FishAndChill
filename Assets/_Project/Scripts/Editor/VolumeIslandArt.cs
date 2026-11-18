using System;
using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Dessins de l'île dans le style en volume (<see cref="VolumeCanvas"/>, 32 px par case) : cocotier 64×80
    /// (2 × 2,5 cases), rochers moussus 32×24, façade de la boutique 160×128 (5 × 4 cases).
    /// </summary>
    public static class VolumeIslandArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        // ---------- Cocotier ----------

        public const int PalmWidth = 64;
        public const int PalmHeight = 80;

        /// <summary>Pied du tronc, au centre de la case.</summary>
        public static readonly Vector2 PalmPivot = new Vector2(0.5f, 6f / PalmHeight);

        /// <summary>Cocotier : tronc courbe cerclé d'anneaux, palmes arquées à nervure claire et folioles, noix de coco.</summary>
        public static Color[,] Palm()
        {
            var cv = new VolumeCanvas(PalmWidth, PalmHeight);
            Color bark = C(0.58f, 0.42f, 0.26f), leaf = C(0.30f, 0.62f, 0.28f), coconut = C(0.46f, 0.30f, 0.16f);

            // Ombre douce au sol.
            var trunk = new List<Func<float, float, bool>>();
            Vector2 Point(float t) => new Vector2(32f + Mathf.Sin(t * 2.2f) * 6f, 76f - t * 48f);
            for (int i = 0; i < 10; i++)
            {
                Vector2 a = Point(i / 10f), b = Point((i + 1) / 10f);
                trunk.Add(Capsule(a.x, a.y, b.x, b.y, Mathf.Lerp(4.2f, 2.6f, i / 10f)));
            }

            cv.Piece(Union(trunk.ToArray()), bark, 3.5f,
                (x, y, c) => y % 4 == 0 ? Darker(c, 0.72f) : y % 4 == 1 ? Darker(c, 1.12f) : (x * 7 + y) % 9 == 0 ? Darker(c, 0.9f) : c);

            Vector2 crown = Point(1f) + new Vector2(0.5f, 0f);
            float[] angles = { 200f, 160f, 235f, 125f, 270f, 95f, 310f, 60f, 20f, 340f };
            for (int k = 0; k < angles.Length; k++)
            {
                float a = angles[k] * Mathf.Deg2Rad;
                float length = 20f + (k % 3) * 3f;
                var points = new List<Vector2>();
                for (int i = 0; i <= 8; i++)
                {
                    float t = i / 8f;
                    points.Add(new Vector2(crown.x + Mathf.Cos(a) * length * t, crown.y - Mathf.Sin(a) * length * t * 0.55f + t * t * 10f));
                }

                var segments = new List<Func<float, float, bool>>();
                for (int i = 0; i < 8; i++)
                {
                    segments.Add(Capsule(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, Mathf.Lerp(3.6f, 0.8f, i / 8f)));
                }

                Func<float, float, bool> frond = Union(segments.ToArray());
                float cosA = Mathf.Cos(a), sinA = Mathf.Sin(a);
                // Folioles : on découpe la palme en dents régulières, sauf près du cœur.
                bool Leaflet(float x, float y)
                {
                    float d = Vector2.Distance(new Vector2(x, y), crown);
                    return d < 5f || (int)(Mathf.Atan2(y - crown.y, x - crown.x) * 40f + d * 1.2f) % 3 != 0;
                }

                cv.Piece((x, y) => frond(x, y) && Leaflet(x, y), k % 2 == 0 ? leaf : Darker(leaf, 0.86f), 2.2f,
                    (x, y, c) => Mathf.Abs((x - crown.x) * -sinA - (y - crown.y) * cosA) < 0.7f ? Darker(c, 1.22f) : c, 0.78f);
            }

            cv.Piece(Ellipse(crown.x - 3f, crown.y + 4f, 2.6f, 2.6f), coconut, 2.4f);
            cv.Piece(Ellipse(crown.x + 2f, crown.y + 4.5f, 2.6f, 2.6f), coconut, 2.4f);
            cv.Piece(Ellipse(crown.x - 0.5f, crown.y + 6.5f, 2.6f, 2.6f), coconut, 2.4f);
            Color[,] p = cv.Finish(C(0.10f, 0.20f, 0.10f));
            AddGroundShadow(p, 32f, 4.5f, 11f, 3f);
            return p;
        }

        // ---------- Rochers ----------

        public const int RockWidth = 32;
        public const int RockHeight = 24;

        /// <summary>Rocher moussu : blocs arrondis, fissures, mousse sur le dessus.</summary>
        public static Color[,] Rock(int variant)
        {
            var cv = new VolumeCanvas(RockWidth, RockHeight);
            Color stone = variant == 1 ? C(0.66f, 0.62f, 0.56f) : C(0.62f, 0.62f, 0.60f), moss = C(0.40f, 0.62f, 0.30f);
            Func<float, float, bool> body = variant == 1
                ? Union(Ellipse(16f, 15f, 12f, 7f), Ellipse(12f, 11f, 7f, 5.5f))
                : Union(Ellipse(13f, 15f, 10f, 7f), Ellipse(21f, 16f, 8f, 6f), Ellipse(16f, 11f, 7f, 6f));
            cv.Piece(body, stone, 6f,
                (x, y, c) => (x * 3 + y * 7) % 13 == 0 || (x - y == 6 && y > 9 && y < 18) ? Darker(c, 0.7f) : (x * 5 + y) % 11 == 0 ? Darker(c, 1.1f) : c);
            cv.Piece((x, y) => Union(Ellipse(14f, 7.5f, 5f, 2.4f), Ellipse(19f, 9f, 3f, 1.6f))(x, y) && ((int)(x * 1.7f) % 3 != 0 || y > 7f), moss, 1.5f);
            Color[,] p = cv.Finish(C(0.16f, 0.16f, 0.18f));
            AddGroundShadow(p, 16f, 2f, 13f, 2.5f);
            return p;
        }

        // ---------- Boutique ----------

        public const int ShopWidth = 160;
        public const int ShopHeight = 128;

        /// <summary>Pied de la façade (la porte) : même proportion que l'ancien dessin (3 px sur 64).</summary>
        public static readonly Vector2 ShopPivot = new Vector2(0.5f, 6f / ShopHeight);

        /// <summary>
        /// Façade de la boutique : toit de chaume bombé à mèches de paille, enseigne au poisson et à la pièce d'or,
        /// auvent rayé, murs en planches, vitrines (poissons suspendus, canne), porte vitrée, soubassement, paillasson.
        /// </summary>
        public static Color[,] ShopExterior()
        {
            var cv = new VolumeCanvas(ShopWidth, ShopHeight);
            Color plank = C(0.80f, 0.62f, 0.40f), wood = C(0.46f, 0.30f, 0.18f), thatch = C(0.84f, 0.70f, 0.42f);
            Color glass = C(0.62f, 0.86f, 0.94f), teal = C(0.18f, 0.56f, 0.60f), stone = C(0.66f, 0.64f, 0.60f);
            Color white = C(0.97f, 0.96f, 0.92f), red = C(0.86f, 0.24f, 0.22f);

            cv.Piece(Box(8, 58, 152, 120), plank, 2.5f,
                (x, y, c) => y % 8 == 0 ? Darker(c, 0.68f) : y % 8 == 1 ? Darker(c, 1.12f) : (x * 3 + (y / 8) * 37) % 41 == 0 ? Darker(c, 0.75f) : (x + y * 5) % 17 == 0 ? Darker(c, 0.92f) : c);
            cv.Piece(Box(6, 114, 154, 122), stone, 2.5f, (x, y, c) => x % 14 == 0 || y == 118 ? Darker(c, 0.75f) : c);
            cv.Piece(Union(Box(6, 58, 13, 122), Box(147, 58, 154, 122)), wood, 2f);

            foreach (int wx in new[] { 16, 102 })
            {
                cv.Piece(Box(wx, 70, wx + 42, 102), white, 2f);
                int left = wx;
                cv.Piece((x, y) => Box(left + 3, 73, left + 39, 99)(x, y) && Mathf.Abs(x - (left + 21)) > 1, glass, 4f,
                    (x, y, c) =>
                    {
                        int d = (x - left) - (99 - y);
                        return (d > 6 && d < 10) || (d > 13 && d < 15) ? C(0.92f, 0.98f, 1f) : Color.Lerp(Darker(c, 0.85f), c, (99 - y) / 26f);
                    });
            }

            for (int i = 0; i < 3; i++)
            {
                cv.Piece(Capsule(26f + i * 10f, 76f, 26f + i * 10f, 80f, 0.6f), wood, 0.6f);
                cv.Piece(Ellipse(26f + i * 10f, 88f, 2.4f, 5.5f), i == 1 ? C(0.40f, 0.62f, 0.90f) : C(0.86f, 0.86f, 0.80f), 2f);
            }

            cv.Piece(Capsule(108f, 76f, 136f, 96f, 1f), wood, 1f);
            cv.Piece(Ellipse(132f, 92f, 3f, 3f), C(0.95f, 0.75f, 0.35f), 2f);

            cv.Piece(Box(64, 66, 94, 120), wood, 2f);
            cv.Piece(Box(68, 70, 90, 120), teal, 3f, (x, y, c) => y > 96 && (x - 68) % 7 == 0 ? Darker(c, 0.8f) : c);
            cv.Piece((x, y) => Box(71, 74, 87, 92)(x, y) && Mathf.Abs(x - 79) > 1, glass, 3f);
            cv.Dot(85, 100, C(1f, 0.85f, 0.35f));
            cv.Dot(85, 101, C(1f, 0.85f, 0.35f));

            // Auvent rayé, bombé, au bord festonné.
            cv.Piece((x, y) => x > 2 && x < 158 && y > 54 && y < 68 + ((x % 8) < 4 ? 3 : 0), white, 6f, (x, y, c) => (x / 8) % 2 == 0 ? red : c);

            // Toit de chaume arrondi.
            cv.Piece((x, y) => y > 4 && y < 58 && Mathf.Abs(x - 80f) < 78f - (58f - y) * 0.45f && (y > 12 || Mathf.Abs(x - 80f) < 50f), thatch, 9f,
                (x, y, c) => (x + (y / 5) * 3) % 4 == 0 ? Darker(c, 0.74f) : (x * 7 + y) % 9 == 0 ? Darker(c, 1.12f) : y % 10 == 0 ? Darker(c, 0.84f) : c);

            // Enseigne : planche, poisson orange, pièce d'or.
            cv.Piece(Box(46, 16, 114, 44), wood, 2f);
            cv.Piece(Box(49, 19, 111, 41), plank, 3f, (x, y, c) => y % 7 == 0 ? Darker(c, 0.8f) : c);
            cv.Piece(Union(Ellipse(72f, 30f, 11f, 5.5f), (x, y) => x > 56 && x < 62 && Mathf.Abs(y - 30f) < (62 - x) * 0.9f), C(1f, 0.58f, 0.20f), 3.5f,
                (x, y, c) => (x + y) % 4 == 0 && x > 62 && x < 80 ? Darker(c, 0.85f) : c);
            cv.Dot(79, 29, C(0.08f, 0.06f, 0.08f));
            cv.Piece(Ellipse(98f, 30f, 6f, 6f), C(1f, 0.82f, 0.26f), 5f,
                (x, y, c) => Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(98, 30)) - 4f) < 0.6f ? Darker(c, 0.75f) : c);

            Color[,] p = cv.Finish(C(0.14f, 0.09f, 0.06f));

            // Paillasson et ombre au sol, sous la façade (y depuis le bas).
            for (int x = 2; x < ShopWidth - 2; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    if (p[x, y].a < 0.01f) p[x, y] = C(0.05f, 0.15f, 0.08f, 0.3f);
                }
            }

            for (int x = 66; x <= 92; x++)
            {
                for (int y = 0; y <= 4; y++)
                {
                    p[x, y] = y == 0 || y == 4 ? C(0.62f, 0.16f, 0.14f) : (x + y) % 3 == 0 ? C(0.96f, 0.56f, 0.32f) : C(0.86f, 0.24f, 0.22f);
                }
            }

            return p;
        }

        /// <summary>Ombre ovale transparente au pied d'un dessin (y depuis le bas), seulement là où il n'y a rien.</summary>
        private static void AddGroundShadow(Color[,] p, float cx, float cy, float rx, float ry)
        {
            for (int x = 0; x < p.GetLength(0); x++)
            {
                for (int y = 0; y < p.GetLength(1); y++)
                {
                    float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                    if (u * u + v * v <= 1f && p[x, y].a < 0.01f)
                    {
                        p[x, y] = C(0.05f, 0.14f, 0.08f, 0.32f);
                    }
                }
            }
        }
    }
}
