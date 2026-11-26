using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Le décor de la baie d'Ha Long (32 px par case, y depuis le haut pendant le dessin) : banian au tronc tordu d'où
    /// pendent des racines aériennes, sous une large couronne vert sombre. Bambous, bananiers, buissons, fleurs et
    /// rochers sont repris des autres zones (recolorés en calcaire pour les rochers).
    /// </summary>
    public static class HaLongTerrainArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Ink = C(0.04f, 0.06f, 0.04f);

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        public const int BanyanWidth = 128;
        public const int BanyanHeight = 136;
        public static readonly Vector2 BanyanPivot = new Vector2(0.5f, 8f / BanyanHeight);

        /// <summary>Banian : tronc tordu fait de racines mêlées, racines aériennes qui pendent jusqu'au sol, couronne large en touffes.</summary>
        public static Color[,] Banyan(int variant)
        {
            var cv = new VolumeCanvas(BanyanWidth, BanyanHeight);
            Color bark = C(0.46f, 0.40f, 0.32f);
            // Racines aériennes, derrière le tronc.
            for (int i = 0; i < 7; i++)
            {
                float x = 30f + i * 11f + Hash(i, variant, 3) * 6f;
                float y0 = 60f + Hash(i, variant, 4) * 10f;
                float y1 = 112f + Hash(i, variant, 5) * 16f;
                cv.Piece(Tube(Curve(new Vector2(x, y0), new Vector2(x + 2f, (y0 + y1) * 0.5f), new Vector2(x - 1f, y1), 8), 1.2f, 0.9f), Darker(bark, 0.85f), 1f);
            }

            // Tronc : plusieurs racines torsadées.
            for (int k = -2; k <= 2; k++)
            {
                var strand = Curve(new Vector2(64f + k * 8f, 128f), new Vector2(64f - k * 4f, 104f), new Vector2(64f + k * 3f, 70f), 10);
                cv.Piece(Tube(strand, 6f, 4f), bark, 4f, (x, y, c) => (y + x / 3) % 6 == 0 ? Darker(c, 0.7f) : Hash(x, y, 9) < 0.06f ? Darker(c, 1.2f) : c);
            }

            // La couronne : une grappe de touffes sombres, éclairées en haut à gauche.
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f + variant;
                float r = 30f + Hash(i, variant, 7) * 10f;
                float cx = 64f + Mathf.Cos(a) * r, cy = 44f + Mathf.Sin(a) * r * 0.55f;
                float rad = 16f + Hash(i, variant, 8) * 8f;
                cv.Piece(Ellipse(cx, cy, rad, rad * 0.8f), C(0.14f, 0.30f, 0.12f), 7f,
                    (x, y, c) => Hash(x / 2, y / 2, 11) < 0.2f ? C(0.30f, 0.48f, 0.18f) : Hash(x, y, 12) < 0.1f ? Darker(c, 0.7f) : c);
            }

            cv.Piece(Ellipse(64f, 40f, 34f, 22f), C(0.18f, 0.36f, 0.14f), 8f, (x, y, c) => Hash(x / 2, y / 2, 13) < 0.22f ? C(0.34f, 0.52f, 0.20f) : c);
            Color[,] p = cv.Finish(Ink, 0.6f);
            for (int x = 0; x < BanyanWidth; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    float u = (x - 64f) / 46f, v = (y - 5f) / 5f;
                    if (u * u + v * v <= 1f && p[x, y].a < 0.5f) p[x, y] = C(0.04f, 0.08f, 0.04f, 0.35f);
                }
            }

            return p;
        }
    }
}
