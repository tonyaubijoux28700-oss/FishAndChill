using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Fjords de Norvège : recoloration des tuiles de l'île (eau profonde du fjord bleu-vert très sombre, eau peu profonde
    /// froide, rives de galets gris, toundra vert mousse éteint) et gravier du chemin du village (16 px par case).
    /// Repères dans <see cref="FjordLandmarkArt"/>, arbres et rochers dans <see cref="FjordTerrainArt"/>.
    /// </summary>
    public static class FjordArt
    {
        private const int S = 16;

        public static Color Deep(Color c) => LakeArt.Tint(c, 0.02f, 0.55f, 0.48f);
        public static Color Shallow(Color c) => LakeArt.Tint(c, 0.01f, 0.45f, 0.66f);
        public static Color Pebbles(Color c) => LakeArt.Tint(c, 0.05f, 0.10f, 0.78f);
        public static Color Tundra(Color c) => LakeArt.Tint(c, 0.02f, 0.45f, 0.72f);

        public static Color[,] Deep(Color[,] p) => LakeArt.Tint(p, 0.02f, 0.55f, 0.48f);
        public static Color[,] Shallow(Color[,] p) => LakeArt.Tint(p, 0.01f, 0.45f, 0.66f);
        public static Color[,] Pebbles(Color[,] p) => LakeArt.Tint(p, 0.05f, 0.10f, 0.78f);
        public static Color[,] Tundra(Color[,] p) => LakeArt.Tint(p, 0.02f, 0.45f, 0.72f);

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        /// <summary>Gravier du chemin (périodique) : petits cailloux gris clair et sombres sur fond gris-brun.</summary>
        public static Color[,] PathFill(int variant)
        {
            var p = new Color[S, S];
            for (int x = 0; x < S; x++)
            {
                for (int y = 0; y < S; y++)
                {
                    float h = Hash(x, y, 90 + variant);
                    p[x, y] = h < 0.14f ? new Color(0.36f, 0.35f, 0.33f) : h > 0.88f ? new Color(0.74f, 0.74f, 0.72f) : new Color(0.54f, 0.52f, 0.48f);
                }
            }

            return p;
        }
    }
}
