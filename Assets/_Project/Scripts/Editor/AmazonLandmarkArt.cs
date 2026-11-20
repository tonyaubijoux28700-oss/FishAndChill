using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les repères du fleuve, dans le style en volume (32 px par case, y depuis le haut pendant le dessin) : les
    /// maisons du village riverain sur pilotis, le fromager géant (sumaúma) de la clairière, la pirogue amarrée.
    /// </summary>
    public static class AmazonLandmarkArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Wood = C(0.54f, 0.38f, 0.24f);
        private static readonly Color WoodDark = C(0.34f, 0.22f, 0.14f);
        private static readonly Color WoodLight = C(0.68f, 0.52f, 0.34f);
        private static readonly Color Thatch = C(0.78f, 0.64f, 0.38f);
        private static readonly Color Tin = C(0.62f, 0.66f, 0.66f);
        private static readonly Color Glass = C(0.20f, 0.20f, 0.18f);   // fenêtres ouvertes, sans vitre
        private static readonly Color White = C(0.94f, 0.92f, 0.86f);
        private static readonly Color Leaf = C(0.26f, 0.52f, 0.22f);

        // ---------- Maison sur pilotis ----------

        public const int HouseWidth = 128;
        public const int HouseHeight = 144;

        /// <summary>Pied de l'échelle, au milieu.</summary>
        public static readonly Vector2 HousePivot = new Vector2(0.5f, 6f / HouseHeight);

        /// <summary>
        /// Maison riveraine sur pilotis (palafita) : poteaux et échelle, plancher, véranda à balustrade, murs de planches
        /// peintes, fenêtres à volets ouverts, hamac rayé, plantes en pot. <paramref name="variant"/> 0 : planches
        /// turquoise et toit de palmes ; 1 : planches jaunes, encadrements verts et toit de tôle.
        /// </summary>
        public static Color[,] StiltHouse(int variant)
        {
            var cv = new VolumeCanvas(HouseWidth, HouseHeight);
            Color paint = variant == 0 ? C(0.30f, 0.66f, 0.68f) : C(0.94f, 0.78f, 0.34f);
            Color trim = variant == 0 ? White : C(0.30f, 0.58f, 0.36f);

            // Pilotis et croisillons sous le plancher, échelle au milieu.
            foreach (int px in new[] { 14, 38, 90, 114 })
            {
                cv.Piece(Box(px - 3, 100, px + 3, 140), WoodDark, 2f, (x, y, c) => y % 9 == 0 ? Darker(c, 0.8f) : c);
            }

            cv.Piece((x, y) => y > 106 && y < 136 && x > 14 && x < 114 && (Mathf.Abs((x - 14) - (y - 106) * 0.8f) < 1.2f || Mathf.Abs((114 - x) - (y - 106) * 0.8f) < 1.2f), WoodDark, 1f);
            cv.Piece((x, y) => y > 100 && y < 140 && (Mathf.Abs(x - 57f) < 1.6f || Mathf.Abs(x - 71f) < 1.6f || (x > 57 && x < 71 && (int)y % 7 == 0)), WoodLight, 1.2f);

            // Plancher et façade.
            cv.Piece(Box(4, 94, 124, 104), Wood, 2f, (x, y, c) => x % 8 == 0 ? WoodDark : c);
            cv.Piece(Box(12, 44, 116, 96), paint, 2.5f, (x, y, c) => x % 8 == 0 ? Darker(c, 0.76f) : (x * 7 + y * 3) % 31 == 0 ? Darker(c, 0.9f) : y > 88 ? Darker(c, 0.92f) : c);

            // Porte ouverte (sombre), deux fenêtres à volets ouverts.
            cv.Piece(Box(54, 58, 74, 96), trim, 2f);
            cv.Piece(Box(57, 61, 71, 96), C(0.16f, 0.12f, 0.10f), 1.5f);
            foreach (int wx in new[] { 32, 96 })
            {
                cv.Piece(Box(wx - 10, 56, wx + 10, 76), trim, 2f);
                cv.Piece(Box(wx - 8, 58, wx + 8, 74), Glass, 1.5f, (x, y, c) => y > 70 ? C(0.30f, 0.26f, 0.22f) : c);
                cv.Piece(Box(wx - 17, 56, wx - 11, 76), trim, 1.5f, (x, y, c) => y % 3 == 0 ? Darker(c, 0.8f) : c);
                cv.Piece(Box(wx + 11, 56, wx + 17, 76), trim, 1.5f, (x, y, c) => y % 3 == 0 ? Darker(c, 0.8f) : c);
            }

            // Hamac rayé tendu sous la fenêtre de droite.
            Color[] stripes = { C(0.90f, 0.24f, 0.22f), C(0.98f, 0.78f, 0.22f), C(0.22f, 0.50f, 0.80f), White };
            cv.Piece((x, y) => x > 80 && x < 114 && y > 80f + Mathf.Sin((x - 80f) / 34f * Mathf.PI) * 8f && y < 85f + Mathf.Sin((x - 80f) / 34f * Mathf.PI) * 8f,
                stripes[0], 2f, (x, y, c) => stripes[(x / 3) % stripes.Length]);
            cv.Piece(Capsule(80f, 80f, 78f, 76f, 0.6f), WoodDark, 0.5f);
            cv.Piece(Capsule(114f, 80f, 116f, 76f, 0.6f), WoodDark, 0.5f);

            // Balustrade de la véranda (ouverte devant l'échelle).
            cv.Piece(Box(4, 82, 124, 85), WoodLight, 1.5f, (x, y, c) => x > 52 && x < 76 ? Color.clear : c);
            for (int bx = 6; bx < 124; bx += 7)
            {
                if (bx > 50 && bx < 76) continue;
                cv.Piece(Box(bx, 84, bx + 2, 95), WoodLight, 1f);
            }

            // Plantes en pot au bord de la véranda.
            foreach (int fx in new[] { 20, 44 })
            {
                cv.Piece(Box(fx - 4, 86, fx + 4, 94), C(0.76f, 0.40f, 0.24f), 2f);
                cv.Piece(Union(Ellipse(fx, 82f, 6f, 4.5f), Ellipse(fx - 3f, 78f, 3f, 3f), Ellipse(fx + 3f, 78f, 3f, 3f)), Leaf, 2.5f);
            }

            // Toit.
            if (variant == 0)
            {
                cv.Piece((x, y) => y > 6 && y < 50 && Mathf.Abs(x - 64f) < 64f - (50f - y) * 1.0f, Thatch, 6f,
                    (x, y, c) => (x + y / 3) % 5 == 0 ? Darker(c, 0.72f) : y % 8 == 0 ? Darker(c, 0.85f) : (x * 7) % 11 == 0 ? Darker(c, 1.1f) : c);
                cv.Piece((x, y) => y > 44 && y < 54 && x > 1 && x < 127 && ((int)x % 5) < 3, Darker(Thatch, 0.88f), 2f);
            }
            else
            {
                cv.Piece((x, y) => y > 10 && y < 50 && Mathf.Abs(x - 64f) < 64f - (50f - y) * 0.95f, Tin, 3f,
                    (x, y, c) => x % 6 < 2 ? Darker(c, 0.78f) : (x * 3 + y * 5) % 37 == 0 ? C(0.62f, 0.40f, 0.26f) : c);   // tôle ondulée, rouille
                cv.Piece(Box(2, 46, 126, 51), Darker(Tin, 0.85f), 1.5f);
            }

            Color[,] p = cv.Finish(C(0.12f, 0.09f, 0.07f));
            for (int x = 2; x < HouseWidth - 2; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (p[x, y].a < 0.01f) p[x, y] = C(0.04f, 0.10f, 0.04f, 0.32f);
                }
            }

            return p;
        }

        // ---------- Fromager géant ----------

        public const int KapokWidth = 256;
        public const int KapokHeight = 288;
        public static readonly Vector2 KapokPivot = new Vector2(0.5f, 14f / KapokHeight);

        /// <summary>
        /// Fromager géant (sumaúma), 8 cases sur 9 : immenses contreforts en planches, tronc pâle et lisse, grosses
        /// branches presque horizontales, couronne en parasol sombre, lianes, broméliacées rouges et orchidées.
        /// </summary>
        public static Color[,] Kapok()
        {
            var cv = new VolumeCanvas(KapokWidth, KapokHeight);
            Color bark = C(0.62f, 0.58f, 0.50f);
            Color leaves = C(0.14f, 0.36f, 0.16f);
            System.Func<int, int, Color, Color> barkTexture = (x, y, c) =>
                (x * 5 + y / 6) % 11 == 0 ? Darker(c, 0.82f) : (x + y * 3) % 47 == 0 ? C(0.44f, 0.54f, 0.34f) : c;   // stries et lichen

            // Contreforts : lames hautes et minces qui partent du tronc et courent au sol en serpentant.
            foreach ((float endX, float endY) in new[] { (30f, 270f), (66f, 279f), (100f, 282f), (156f, 282f), (190f, 279f), (226f, 270f) })
            {
                float ex = endX, ey = endY;
                cv.Piece((x, y) =>
                {
                    var q = new Vector2(x, y);
                    var a = new Vector2(128f + (ex - 128f) * 0.1f, 176f);
                    var b = new Vector2(ex, ey);
                    float t = Mathf.Clamp01(Vector2.Dot(q - a, b - a) / (b - a).sqrMagnitude);
                    Vector2 axis = a + (b - a) * t + new Vector2(Mathf.Sin(t * 7f + ex) * 2.5f, 0f);
                    float w = Mathf.Lerp(7f, 2f, t);
                    return Vector2.Distance(q, axis) < w && y <= ey;
                }, bark, 3f, (x, y, c) => barkTexture(x, y, y > 250 ? Darker(c, 0.82f) : c));
            }

            // Tronc lisse qui s'évase, et grosses branches presque horizontales sous la couronne.
            cv.Piece((x, y) => y > 112 && y < 266 && Mathf.Abs(x - 128f) < 14f + Mathf.Max(0f, y - 200f) * 0.3f, bark, 10f, barkTexture);
            foreach ((float x0, float y0, float x1, float y1, float r) in new[]
                     { (128f, 126f, 34f, 104f, 6f), (128f, 124f, 222f, 102f, 6f), (124f, 120f, 78f, 86f, 5f), (132f, 120f, 180f, 84f, 5f) })
            {
                cv.Piece(Capsule(x0, y0, x1, y1, r), bark, 4f, barkTexture);
            }

            // Couronne en parasol, très large : des dizaines de bouquets de feuillage ronds, les plus bas posés en dernier.
            System.Func<int, int, Color, Color> foliage = (x, y, c) =>
            {
                int h = (x * 7 + y * 13) % 31;
                if (h == 0 || h == 15) return Darker(c, 0.78f);
                if (h == 7) return Darker(c, 1.22f);
                return c;
            };
            var clumps = new List<Vector3>();
            for (int row = 0; row < 4; row++)
            {
                float cy = 26f + row * 24f;
                float span = row == 0 ? 62f : row == 1 ? 92f : row == 2 ? 104f : 94f;
                int count = row == 0 ? 4 : row == 1 ? 6 : row == 2 ? 7 : 6;
                for (int i = 0; i < count; i++)
                {
                    float cx = 128f + Mathf.Lerp(-span, span, i / (float)(count - 1)) + (row % 2 == 0 ? 0f : 6f);
                    clumps.Add(new Vector3(cx, cy + ((i * 7 + row * 3) % 3 - 1) * 4f, row == 3 ? 17f : 22f));
                }
            }

            foreach (Vector3 clump in clumps)
            {
                Color tone = clump.y > 90f ? Darker(leaves, 0.9f) : (clump.x + clump.y) % 3 < 1 ? Darker(leaves, 1.12f) : leaves;
                cv.Piece(Ellipse(clump.x, clump.y, clump.z, clump.z * 0.72f), tone, clump.z * 0.55f, foliage, 0.7f, 0.36f);
            }

            // Lianes qui tombent de la couronne jusqu'au sol, broméliacées sur les branches, orchidées sur le tronc.
            Color liana = C(0.24f, 0.36f, 0.14f);
            foreach ((float lx, float top, float bottom) in new[] { (58f, 96f, 236f), (80f, 102f, 200f), (196f, 98f, 244f), (176f, 100f, 180f), (112f, 118f, 168f) })
            {
                float px = lx, t0 = top, t1 = bottom;
                cv.Piece((x, y) => y > t0 && y < t1 && Mathf.Abs(x - (px + Mathf.Sin(y * 0.09f + px) * 3f)) < 1.3f, liana, 1f,
                    (x, y, c) => (int)y % 11 == 0 ? Leaf : c);
            }

            foreach ((float bx, float by) in new[] { (70f, 114f), (188f, 112f), (102f, 104f), (156f, 102f) })
            {
                cv.Piece(Union(Ellipse(bx, by, 7f, 4f), Ellipse(bx - 4f, by - 3f, 3f, 3f), Ellipse(bx + 4f, by - 3f, 3f, 3f)), C(0.30f, 0.54f, 0.22f), 3f);
                cv.Piece(Ellipse(bx, by - 4f, 2.5f, 3f), C(0.90f, 0.18f, 0.26f), 2f);
            }

            foreach ((float ox, float oy) in new[] { (118f, 150f), (138f, 186f) })
            {
                cv.Piece(Union(Ellipse(ox - 2f, oy, 2.2f, 1.6f), Ellipse(ox + 2f, oy, 2.2f, 1.6f), Ellipse(ox, oy - 2f, 1.6f, 2.2f)), C(0.92f, 0.60f, 0.86f), 1.5f);
            }

            Color[,] p = cv.Finish(C(0.04f, 0.10f, 0.05f), 0.5f);
            // Grande ombre au sol sous la couronne (y depuis le bas).
            for (int x = 0; x < KapokWidth; x++)
            {
                for (int y = 0; y < 40; y++)
                {
                    float u = (x + 0.5f - 132f) / 124f, v = (y + 0.5f - 16f) / 16f;
                    if (u * u + v * v <= 1f && p[x, y].a < 0.01f) p[x, y] = C(0.02f, 0.07f, 0.03f, 0.4f);
                }
            }

            return p;
        }

        // ---------- Pirogue ----------

        public const int CanoeWidth = 112;
        public const int CanoeHeight = 44;

        /// <summary>Pirogue creusée dans un tronc, amarrée : coque sombre, intérieur clair, pagaie posée, rides autour.</summary>
        public static Color[,] Canoe()
        {
            var cv = new VolumeCanvas(CanoeWidth, CanoeHeight);
            System.Func<float, float, bool> hull = (x, y) =>
            {
                float t = (x - 56f) / 50f;
                float half = 11f * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t * t * t));
                return Mathf.Abs(t) < 1f && Mathf.Abs(y - 22f) < half;
            };
            cv.Piece(hull, C(0.36f, 0.22f, 0.14f), 5f, (x, y, c) => (x / 9) % 2 == 0 && Mathf.Abs(y - 22f) > 7f ? Darker(c, 0.85f) : c);
            cv.Piece((x, y) => hull(56f + (x - 56f) * 1.1f, 22f + (y - 22f) * 1.45f), C(0.66f, 0.48f, 0.30f), 3f,
                (x, y, c) => (int)x % 7 == 0 ? Darker(c, 0.82f) : c);
            cv.Piece(Box(34, 12, 38, 32), WoodDark, 1.5f);   // banc
            cv.Piece(Box(72, 12, 76, 32), WoodDark, 1.5f);
            cv.Piece(Union(Capsule(30f, 26f, 84f, 18f, 1.4f), Ellipse(88f, 17.5f, 6f, 3f)), WoodLight, 2f);   // pagaie
            Color[,] p = cv.Finish(C(0.10f, 0.06f, 0.04f), 0.5f);

            // Rides claires autour de la coque (y depuis le bas).
            for (int x = 0; x < CanoeWidth; x++)
            {
                for (int y = 0; y < CanoeHeight; y++)
                {
                    float u = (x + 0.5f - 56f) / 55f, v = (y + 0.5f - 21f) / 20f;
                    float d = u * u + v * v;
                    if (p[x, y].a < 0.01f && d > 0.82f && d < 1f && (x / 3) % 3 != 0) p[x, y] = C(0.86f, 0.80f, 0.62f, 0.5f);
                }
            }

            return p;
        }
    }
}
