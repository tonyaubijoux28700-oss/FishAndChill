using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Atoll des Maldives : recoloration des tuiles de l'île (océan Indien d'un bleu profond, lagon turquoise
    /// lumineux, sable de corail très blanc, végétation tropicale bien verte). Repères dans <see cref="AtollLandmarkArt"/>,
    /// végétation dans <see cref="AtollTerrainArt"/>.
    /// </summary>
    public static class AtollArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        // ---------- Recoloration ----------

        public static Color Ocean(Color c) => LakeArt.Tint(c, -0.02f, 1.18f, 0.92f);
        public static Color Lagoon(Color c) => LakeArt.Tint(c, -0.04f, 1.25f, 1.10f);
        public static Color Sand(Color c) => LakeArt.Tint(c, 0.0f, 0.18f, 1.10f);
        public static Color Green(Color c) => LakeArt.Tint(c, 0.02f, 1.15f, 1.02f);

        public static Color[,] Ocean(Color[,] p) => LakeArt.Tint(p, -0.02f, 1.18f, 0.92f);
        public static Color[,] Lagoon(Color[,] p) => LakeArt.Tint(p, -0.04f, 1.25f, 1.10f);
        public static Color[,] Sand(Color[,] p) => LakeArt.Tint(p, 0.0f, 0.18f, 1.10f);
        public static Color[,] Green(Color[,] p) => LakeArt.Tint(p, 0.02f, 1.15f, 1.02f);
    }
}
