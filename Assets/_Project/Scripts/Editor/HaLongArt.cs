using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Baie d'Ha Long : recoloration des tuiles de l'île (eau émeraude de la baie, sable doré, végétation tropicale
    /// d'un vert profond). Repères dans <see cref="HaLongLandmarkArt"/>, végétation dans <see cref="HaLongTerrainArt"/>.
    /// </summary>
    public static class HaLongArt
    {
        public static Color Bay(Color c) => LakeArt.Tint(c, -0.08f, 0.95f, 0.78f);
        public static Color Shallow(Color c) => LakeArt.Tint(c, -0.10f, 0.85f, 0.92f);
        public static Color Sand(Color c) => LakeArt.Tint(c, 0.0f, 0.55f, 1.0f);
        public static Color Green(Color c) => LakeArt.Tint(c, 0.01f, 1.1f, 0.86f);

        public static Color[,] Bay(Color[,] p) => LakeArt.Tint(p, -0.08f, 0.95f, 0.78f);
        public static Color[,] Shallow(Color[,] p) => LakeArt.Tint(p, -0.10f, 0.85f, 0.92f);
        public static Color[,] Sand(Color[,] p) => LakeArt.Tint(p, 0.0f, 0.55f, 1.0f);
        public static Color[,] Green(Color[,] p) => LakeArt.Tint(p, 0.01f, 1.1f, 0.86f);
    }
}
