using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Côte des Squelettes : recoloration des tuiles de l'île (Atlantique froid gris-vert, eau peu profonde laiteuse, sable
    /// pâle et terne, plaine de graviers ocre du désert). Repères dans <see cref="SkeletonLandmarkArt"/>, sol et végétation
    /// dans <see cref="SkeletonTerrainArt"/>.
    /// </summary>
    public static class SkeletonArt
    {
        public static Color Ocean(Color c) => LakeArt.Tint(c, -0.05f, 0.42f, 0.62f);
        public static Color Shallow(Color c) => LakeArt.Tint(c, -0.06f, 0.36f, 0.74f);
        public static Color Sand(Color c) => LakeArt.Tint(c, -0.01f, 0.45f, 0.90f);
        public static Color Gravel(Color c) => LakeArt.Tint(c, -0.25f, 0.42f, 0.80f);

        public static Color[,] Ocean(Color[,] p) => LakeArt.Tint(p, -0.05f, 0.42f, 0.62f);
        public static Color[,] Shallow(Color[,] p) => LakeArt.Tint(p, -0.06f, 0.36f, 0.74f);
        public static Color[,] Sand(Color[,] p) => LakeArt.Tint(p, -0.01f, 0.45f, 0.90f);
        public static Color[,] Gravel(Color[,] p) => LakeArt.Tint(p, -0.25f, 0.42f, 0.80f);
    }
}
