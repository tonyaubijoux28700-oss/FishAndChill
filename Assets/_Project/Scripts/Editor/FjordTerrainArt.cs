using System;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Le décor des fjords de Norvège (32 px par case, y depuis le haut pendant le dessin) : sapins sombres poudrés de neige,
    /// bouleaux au tronc blanc tacheté, blocs de granit couverts de lichen, touffes de bruyère, plaques de neige, mousse.
    /// </summary>
    public static class FjordTerrainArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Ink = C(0.05f, 0.06f, 0.06f);
        private static readonly Color Snow = C(0.94f, 0.96f, 0.98f);

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
                    float u = (x - cx) / rx, v = (y - cy) / ry;
                    if (u * u + v * v > 1f || p[x, y].a > 0.5f) continue;
                    p[x, y] = C(0.05f, 0.08f, 0.08f, 0.35f);
                }
            }
        }

        public const int PineWidth = 72;
        public const int PineHeight = 128;
        public static readonly Vector2 PinePivot = new Vector2(0.5f, 6f / PineHeight);

        /// <summary>Sapin de Norvège : étages de branches tombantes vert sombre, neige posée sur le dessus de chaque étage.</summary>
        public static Color[,] Pine(int variant)
        {
            var cv = new VolumeCanvas(PineWidth, PineHeight);
            float lean = variant == 1 ? 1.5f : 0f;
            cv.Piece(Box(33f, 96f, 39f, 122f), C(0.30f, 0.20f, 0.14f), 2f, (x, y, c) => (x + y) % 5 == 0 ? Darker(c, 0.7f) : c);
            int tiers = variant == 2 ? 5 : 6;
            for (int t = 0; t < tiers; t++)
            {
                float top = 8f + t * (90f / tiers), bottom = top + 34f;
                float halfBottom = 10f + t * 5.2f;
                float cx = 36f + lean * t * 0.3f;
                Func<float, float, bool> tier = (x, y) => y > top && y < bottom && Mathf.Abs(x - cx) < halfBottom * (y - top) / (bottom - top) + 2f
                    && !(y > bottom - 6f && Mathf.Repeat(x * 0.4f, 1f) < 0.35f);
                cv.Piece(tier, C(0.12f, 0.24f, 0.18f), 6f, (x, y, c) =>
                {
                    float k = (y - top) / (bottom - top);
                    Color g = Color.Lerp(C(0.18f, 0.32f, 0.24f), C(0.08f, 0.16f, 0.12f), k);
                    // Fin liseré de neige le long du bord supérieur de l'étage (la pente), plus épais du côté éclairé.
                    float edge = halfBottom * (y - top) / (bottom - top) + 2f - Mathf.Abs(x - cx);
                    if (edge < 2.2f && k < 0.85f && Hash((int)x / 2, (int)y, t) > 0.25f) return Color.Lerp(Snow, C(0.78f, 0.84f, 0.92f), x > cx ? 0.6f : 0f);
                    if (k < 0.12f) return Snow;
                    return Hash(x, y, t) < 0.08f ? Darker(g, 1.3f) : g;
                });
            }

            Color[,] p = cv.Finish(Ink, 0.6f);
            GroundShadow(p, 36f, 4f, 22f, 4f);
            return p;
        }

        public const int BirchWidth = 64;
        public const int BirchHeight = 104;
        public static readonly Vector2 BirchPivot = new Vector2(0.5f, 6f / BirchHeight);

        /// <summary>Bouleau : tronc blanc fin aux taches noires, branches fines, feuillage léger vert-jaune.</summary>
        public static Color[,] Birch(int variant)
        {
            var cv = new VolumeCanvas(BirchWidth, BirchHeight);
            float s = variant == 0 ? 1f : -1f;
            Color bark = C(0.90f, 0.88f, 0.82f);
            cv.Piece(Union(Capsule(32f, 100f, 32f + s * 3f, 40f, 2.6f), Capsule(32f + s * 2f, 64f, 32f + s * 14f, 46f, 1.4f), Capsule(32f + s * 3f, 54f, 32f - s * 12f, 38f, 1.3f)), bark, 2f,
                (x, y, c) => Hash(x / 2, y / 3, 7) < 0.18f ? C(0.10f, 0.10f, 0.10f) : c);
            foreach ((float cx, float cy, float r) in new[] { (32f + s * 3f, 30f, 18f), (32f + s * 14f, 40f, 12f), (32f - s * 12f, 34f, 12f), (32f, 18f, 12f) })
            {
                cv.Piece((x, y) => (x - cx) * (x - cx) / (r * r) + (y - cy) * (y - cy) / (r * r * 0.8f) < 1f && Mathf.PerlinNoise(x * 0.3f, y * 0.3f) > 0.28f,
                    C(0.56f, 0.66f, 0.28f), 4f, (x, y, c) => Hash(x, y, 9) < 0.15f ? C(0.80f, 0.74f, 0.30f) : Mathf.PerlinNoise(x * 0.2f, y * 0.2f) < 0.4f ? Darker(c, 0.75f) : c);
            }

            Color[,] p = cv.Finish(Ink, 0.6f);
            GroundShadow(p, 32f, 4f, 16f, 3f);
            return p;
        }

        public const int BoulderWidth = 64;
        public const int BoulderHeight = 48;
        public static readonly Vector2 BoulderPivot = new Vector2(0.5f, 6f / BoulderHeight);

        /// <summary>Bloc de granit erratique : gris, taches de lichen jaune et vert, un peu de neige dessus.</summary>
        public static Color[,] Boulder(int variant)
        {
            var cv = new VolumeCanvas(BoulderWidth, BoulderHeight);
            float w = variant == 0 ? 28f : 22f, h = variant == 0 ? 19f : 16f;
            cv.Piece((x, y) => (x - 32f) * (x - 32f) / (w * w) + (y - 26f) * (y - 26f) / (h * h) < 1f + Mathf.PerlinNoise(x * 0.15f, y * 0.15f + variant) * 0.25f && y < 44,
                C(0.48f, 0.49f, 0.50f), 8f, (x, y, c) =>
                {
                    float n = Mathf.PerlinNoise(x * 0.2f + variant * 5f, y * 0.2f);
                    if (n > 0.72f) return C(0.70f, 0.66f, 0.30f);    // lichen jaune
                    if (n < 0.22f) return C(0.40f, 0.50f, 0.34f);    // lichen vert
                    if (y < 18 && Mathf.PerlinNoise(x * 0.3f, y * 0.3f) > 0.5f) return Snow;
                    return Hash(x, y, 4) < 0.08f ? Darker(c, 0.8f) : c;
                });
            Color[,] p = cv.Finish(Ink, 0.55f);
            GroundShadow(p, 32f, 4f, w, 4f);
            return p;
        }

        public const int HeatherWidth = 32;
        public const int HeatherHeight = 24;
        public static readonly Vector2 HeatherPivot = new Vector2(0.5f, 4f / HeatherHeight);

        /// <summary>Touffe de bruyère : petits brins sombres couverts de fleurs mauves.</summary>
        public static Color[,] Heather(int variant)
        {
            var cv = new VolumeCanvas(HeatherWidth, HeatherHeight);
            cv.Piece((x, y) => (x - 16f) * (x - 16f) / 144f + (y - 14f) * (y - 14f) / 64f < 1f && Hash((int)x, (int)y, variant) > 0.15f, C(0.30f, 0.34f, 0.20f), 3f,
                (x, y, c) => Hash(x, y, variant + 3) < 0.45f ? (variant == 1 ? C(0.80f, 0.52f, 0.76f) : C(0.66f, 0.36f, 0.62f)) : c);
            return cv.Finish(Ink, 0.4f);
        }

        public const int SnowWidth = 96;
        public const int SnowHeight = 48;

        /// <summary>Plaque de neige posée au sol, bord irrégulier, ombre bleutée (à plat).</summary>
        public static Color[,] SnowPatch(int variant)
        {
            var p = new Color[SnowWidth, SnowHeight];
            for (int x = 0; x < SnowWidth; x++)
            {
                for (int y = 0; y < SnowHeight; y++)
                {
                    float u = (x - 48f) / 46f, v = (y - 24f) / 22f;
                    float d = u * u + v * v + (Mathf.PerlinNoise(x * 0.12f + variant * 7f, y * 0.12f) - 0.5f) * 0.6f;
                    if (d < 1f) p[x, y] = d > 0.8f ? C(0.80f, 0.86f, 0.94f) : y < 20 ? C(0.84f, 0.89f, 0.96f) : Snow;
                }
            }

            return p;
        }
    }
}
