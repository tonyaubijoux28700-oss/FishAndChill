using UnityEngine;
using FishAndChill.World;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// La plongée : le fond marin de Mahana vu de dessus (d'après <see cref="DivePlan"/>, 16 px par case : sable clair aux
    /// rides, taches de lumière, herbiers, massifs de lave couverts d'algues, patates de corail, la corde de la bouée qui
    /// monte vers la surface), et la bouée de plongée posée dans le lagon (anneau rouge et blanc, pavillon « plongeur »).
    /// </summary>
    public static class DiveArt
    {
        public const int PixelsPerUnit = 16;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Sand = C(0.92f, 0.86f, 0.64f);
        private static readonly Color SandDark = C(0.82f, 0.76f, 0.56f);

        /// <summary>Le fond marin, [x, y] avec y depuis le bas, 640×416.</summary>
        public static Color[,] Floor(DivePlan plan)
        {
            int w = DivePlan.Width * PixelsPerUnit, h = DivePlan.Height * PixelsPerUnit;
            var cv = new VolumeCanvas(w, h);
            float Top(Vector2 cell) => h - cell.y * PixelsPerUnit;   // y depuis le haut pendant le dessin

            // Le sable : rides ondulées, grandes taches plus sombres, petits débris de coquillages.
            cv.Piece((x, y) => true, Sand, 0.5f, (x, y, c) =>
            {
                float n = Mathf.PerlinNoise(x * 0.012f, y * 0.012f);
                Color k = Color.Lerp(SandDark, Sand, Mathf.Round(n * 4f) / 4f);
                float ripple = Mathf.Sin(x * 0.18f + Mathf.Sin(y * 0.05f) * 3f + y * 0.06f);
                if (ripple > 0.9f) k = Darker(k, 0.94f);
                else if (ripple < -0.95f) k = Darker(k, 1.05f);
                if (Hash(x, y) < 0.006f) k = C(0.96f, 0.92f, 0.84f);
                return k;
            }, 0f, 1f);

            // Herbiers : touffes de longues feuilles vertes qui se couchent dans le courant.
            foreach ((Vector2 center, float radius) in plan.Seagrass)
            {
                float cx = center.x * PixelsPerUnit, cy = Top(center), r = radius * PixelsPerUnit;
                cv.Piece((x, y) =>
                {
                    float u = (x - cx) / r, v = (y - cy) / (r * 0.7f);
                    return u * u + v * v < 1f && Mathf.Repeat(x * 0.5f + y * 0.15f + Mathf.Sin(y * 0.3f) * 1.5f, 3f) < 1.3f;
                }, C(0.30f, 0.56f, 0.30f), 1.5f, (x, y, c) => Hash(x / 2, y / 2) < 0.3f ? C(0.46f, 0.72f, 0.38f) : c, 0.8f, 0.5f);
            }

            // Massifs de lave noire, couverts d'algues et de petits coraux.
            foreach ((Vector2 center, Vector2 half) in plan.Rocks)
            {
                float cx = center.x * PixelsPerUnit, cy = Top(center);
                float rx = half.x * PixelsPerUnit, ry = half.y * PixelsPerUnit;
                cv.Piece((x, y) =>
                {
                    float u = (x - cx) / rx, v = (y - cy) / ry;
                    return u * u + v * v < 1f + Mathf.Sin(Mathf.Atan2(v, u) * 5f) * 0.12f;
                }, C(0.30f, 0.28f, 0.30f), 10f, (x, y, c) =>
                {
                    if (Mathf.PerlinNoise(x * 0.08f, y * 0.08f) > 0.62f) return C(0.36f, 0.54f, 0.30f);   // algues
                    if (Hash(x / 2, y / 2) < 0.03f) return C(0.94f, 0.56f, 0.62f);                         // petits coraux roses
                    return (x * 7 + y * 11) % 13 == 0 ? Darker(c, 0.6f) : c;
                }, 0.8f, 0.4f);
            }

            // Patates de corail en décor (on ne les ramasse pas).
            int k2 = 0;
            foreach (Vector2 head in plan.CoralHeads)
            {
                float cx = head.x * PixelsPerUnit, cy = Top(head);
                Color coral = k2++ % 3 == 0 ? C(0.86f, 0.70f, 0.40f) : k2 % 3 == 1 ? C(0.62f, 0.48f, 0.70f) : C(0.92f, 0.56f, 0.46f);
                cv.Piece(Ellipse(cx, cy, 14f, 9f), coral, 7f, (x, y, c) => Mathf.Abs(Mathf.Sin(x * 0.8f + y * 0.5f)) < 0.2f ? Darker(c, 0.7f) : Hash(x / 2, y / 2) < 0.08f ? Darker(c, 1.25f) : c);
            }

            // La corde de la bouée : du lest posé sur le sable, elle monte vers la surface (vers le haut de l'écran).
            float rx0 = plan.Rope.x * PixelsPerUnit, ry0 = Top(plan.Rope);
            cv.Piece(Ellipse(rx0, ry0 + 3f, 7f, 4f), C(0.56f, 0.56f, 0.58f), 4f);   // le lest
            cv.Piece(Tube(Curve(new Vector2(rx0, ry0), new Vector2(rx0 + 6f, ry0 - 30f), new Vector2(rx0 - 2f, 0f), 30), 1.4f, 1.4f), C(0.92f, 0.76f, 0.40f), 1.2f,
                (x, y, c) => y % 4 == 0 ? Darker(c, 0.7f) : c);
            Color[,] p = cv.Finish(C(0.06f, 0.10f, 0.10f), 0.45f);

            // Ombres des massifs sur le sable (après le contour), puis la teinte de l'eau par-dessus tout.
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    Color c = p[x, y];
                    float depth = Mathf.PerlinNoise(x * 0.006f + 3f, y * 0.006f);
                    p[x, y] = Color.Lerp(c, C(0.12f, 0.56f, 0.62f), 0.16f + depth * 0.12f);
                    p[x, y].a = 1f;
                }
            }

            return p;
        }

        /// <summary>Taches de lumière qui dansent sur le fond (3 images, 64×64, transparentes, à répéter).</summary>
        public static Color[,] Caustics(int frame)
        {
            var p = new Color[64, 64];
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    float a = Mathf.Sin((x + frame * 3f) * 0.2f + Mathf.Sin(y * 0.15f + frame) * 2f) + Mathf.Sin((y - frame * 2f) * 0.22f + Mathf.Sin(x * 0.12f) * 2f);
                    if (a > 1.45f) p[x, y] = C(0.86f, 1f, 0.96f, 0.22f);
                }
            }

            return p;
        }

        public const int BuoyWidth = 24;
        public const int BuoyHeight = 34;
        public static readonly Vector2 BuoyPivot = new Vector2(0.5f, 4f / BuoyHeight);

        /// <summary>Bouée de plongée dans le lagon : anneau rouge et blanc, petit mât et pavillon « plongeur » (rouge, diagonale blanche).</summary>
        public static Color[,] Buoy()
        {
            var cv = new VolumeCanvas(BuoyWidth, BuoyHeight);
            cv.Piece(Capsule(12f, 28f, 12f, 4f, 0.8f), C(0.40f, 0.40f, 0.44f), 0.8f);   // le mât
            cv.Piece(Box(12.5f, 4f, 22f, 12f), C(0.90f, 0.14f, 0.12f), 1f, (x, y, c) => Mathf.Abs((x - 12.5f) - (y - 4f) * 1.2f) < 1.4f ? C(0.98f, 0.98f, 0.96f) : c);
            cv.Piece(Ellipse(12f, 27f, 9f, 4f), C(0.92f, 0.20f, 0.16f), 3f, (x, y, c) => Mathf.Repeat(Mathf.Atan2(y - 27f, x - 12f) * 2f / Mathf.PI, 1f) < 0.5f ? C(0.96f, 0.96f, 0.94f) : c);
            cv.Piece(Ellipse(12f, 27f, 4.5f, 1.8f), C(0.10f, 0.40f, 0.46f), 1f);
            Color[,] p = cv.Finish(C(0.08f, 0.06f, 0.06f), 0.55f);
            for (int x = 0; x < BuoyWidth; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    float u = (x + 0.5f - 12f) / 11f, v = (y - 2f) / 2f;
                    if (u * u + v * v < 1f && p[x, y].a < 0.5f) p[x, y] = y > 1 ? C(0.92f, 0.98f, 1f, 0.7f) : C(0.04f, 0.20f, 0.24f, 0.35f);   // écume et reflet
                }
            }

            return p;
        }
    }
}
