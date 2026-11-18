using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Atoll des Maldives : recoloration des tuiles de l'île (océan Indien d'un bleu profond, lagon turquoise
    /// lumineux, sable de corail très blanc, végétation tropicale bien verte) et bungalow sur pilotis dans le style en
    /// volume (<see cref="VolumeCanvas"/>, 64×80 à 32 px par case : 2 × 2,5 cases).
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

        // ---------- Bungalow sur pilotis ----------

        public const int BungalowWidth = 64;
        public const int BungalowHeight = 80;

        /// <summary>Pied des pilotis du milieu, au centre de la case.</summary>
        public static readonly Vector2 BungalowPivot = new Vector2(0.5f, 8f / BungalowHeight);

        /// <summary>
        /// Bungalow sur pilotis : pilotis dans l'eau (avec leurs reflets), terrasse en planches, murs en bois clair,
        /// fenêtre et porte, toit de chaume pentu à mèches de paille, petite échelle vers le lagon.
        /// </summary>
        public static Color[,] Bungalow()
        {
            var cv = new VolumeCanvas(BungalowWidth, BungalowHeight);
            Color post = C(0.46f, 0.32f, 0.20f), deck = C(0.74f, 0.54f, 0.32f), wall = C(0.88f, 0.74f, 0.52f);
            Color thatch = C(0.84f, 0.70f, 0.42f), glass = C(0.60f, 0.86f, 0.94f), door = C(0.52f, 0.34f, 0.20f);

            // Pilotis et leurs reflets dans l'eau.
            foreach (float px in new[] { 12f, 32f, 52f })
            {
                cv.Piece(Box(px - 2.2f, 52, px + 2.2f, 73), post, 2f, (x, y, c) => y % 6 == 0 ? Darker(c, 0.75f) : c);
                cv.Piece(Box(px - 1.6f, 73, px + 1.6f, 78), C(0.30f, 0.60f, 0.62f, 0.9f), 1f, (x, y, c) => y % 2 == 0 ? c : C(0, 0, 0, 0));
            }

            // Échelle vers le lagon.
            cv.Piece(Union(Box(40, 54, 42, 74), Box(46, 54, 48, 74)), post, 1f);
            for (int ry = 58; ry < 74; ry += 5)
            {
                int rung = ry;
                cv.Piece(Box(40, rung, 48, rung + 1.5f), post, 1f);
            }

            // Terrasse.
            cv.Piece(Box(2, 46, 62, 56), deck, 2.5f, (x, y, c) => x % 6 == 0 ? Darker(c, 0.72f) : (x * 3 + y) % 11 == 0 ? Darker(c, 0.9f) : c);

            // Murs, fenêtre, porte.
            cv.Piece(Box(8, 26, 56, 48), wall, 2.5f, (x, y, c) => x % 5 == 0 ? Darker(c, 0.82f) : c);
            cv.Piece((x, y) => Box(12, 31, 26, 41)(x, y) && Mathf.Abs(x - 19) > 0.6f, glass, 2f,
                (x, y, c) => (x - 12) - (41 - y) is int d && d > 3 && d < 6 ? C(0.92f, 0.98f, 1f) : c);
            cv.Piece(Box(34, 30, 46, 48), door, 2f, (x, y, c) => (x - 34) % 4 == 0 ? Darker(c, 0.8f) : c);
            cv.Dot(43, 39, C(1f, 0.85f, 0.35f));

            // Toit de chaume pentu, qui déborde.
            cv.Piece((x, y) => y > 2 && y < 30 && Mathf.Abs(x - 32f) < 31f - (30f - y) * 0.95f, thatch, 6f,
                (x, y, c) => (x + (y / 4) * 2) % 3 == 0 ? Darker(c, 0.74f) : (x * 7 + y) % 9 == 0 ? Darker(c, 1.12f) : y % 8 == 0 ? Darker(c, 0.84f) : c);
            cv.Piece(Box(28, 1, 36, 5), Darker(thatch, 0.8f), 2f);   // faîtage

            return cv.Finish(C(0.14f, 0.10f, 0.08f));
        }
    }
}
