using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Artefact du dragon de la baie d'Ha Long : le Tambour de bronze, tambour de bronze ancien vu de trois quarts,
    /// couvert de vert-de-gris : sur la face du dessus, une étoile à douze branches au centre et des cercles gravés
    /// d'oiseaux et de vagues, quatre petites grenouilles sur le bord ; le fût s'évase en bas. Une lueur de jade au cœur
    /// de l'étoile. Même cadre que l'Idole de corail (48×64), cassé en 4 morceaux comme une tarte. Tableaux [x, y] avec y
    /// depuis le bas.
    /// </summary>
    public static class BronzeDrumArt
    {
        public const int Width = LegendArt.IdolWidth;
        public const int Height = LegendArt.IdolHeight;
        public const int Fragments = 4;

        private const float Cx = 24f;
        private const float TopY = 18f;   // milieu de la face du dessus, y depuis le haut

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Bronze = C(0.62f, 0.46f, 0.24f);
        private static readonly Color Patina = C(0.30f, 0.58f, 0.48f);
        private static readonly Color Engraving = C(0.24f, 0.18f, 0.10f);
        private static readonly Color Jade = C(0.56f, 1f, 0.78f);

        private static Color Weathered(int x, int y, Color c)
        {
            float n = Noise(x / 3f, y / 3f);
            return n > 0.55f ? Color.Lerp(c, Patina, Mathf.Clamp01((n - 0.55f) * 3f)) : c;
        }

        public static Color[,] DrumArt()
        {
            var cv = new VolumeCanvas(Width, Height);

            // Le fût : plus étroit au milieu, évasé à la base.
            cv.Piece((x, y) =>
            {
                if (y < TopY || y > 60f) return false;
                float k = (y - TopY) / (60f - TopY);
                float half = 20f - Mathf.Sin(k * Mathf.PI) * 4f + (k > 0.8f ? (k - 0.8f) * 12f : 0f);
                return Mathf.Abs(x - Cx) < half;
            }, Bronze, 8f, (x, y, c) =>
            {
                Color k = Weathered(x, y, c);
                if (y % 9 == 0) return Darker(k, 0.65f);                                       // cerclages gravés
                if (y > 30 && y < 50 && (x + y) % 6 == 0 && y % 9 > 2 && y % 9 < 7) return Darker(k, 0.78f);   // motifs de vagues
                return k;
            }, 0.85f, 0.45f);
            // Deux anses de chaque côté.
            foreach (float side in new[] { -1f, 1f })
            {
                cv.Piece(Tube(Curve(new Vector2(Cx + side * 18f, 22f), new Vector2(Cx + side * 25f, 26f), new Vector2(Cx + side * 18f, 32f), 8), 1.4f, 1.4f), Bronze, 1.2f, Weathered);
            }

            // La face du dessus : étoile au centre, anneaux gravés d'oiseaux en vol.
            cv.Piece(Ellipse(Cx, TopY, 21f, 8f), Darker(Bronze, 1.15f), 3f, (x, y, c) =>
            {
                float u = (x - Cx) / 21f, v = (y - TopY) / 8f;
                float r = Mathf.Sqrt(u * u + v * v);
                float a = Mathf.Atan2(v, u);
                Color k = Weathered(x, y, c);
                if (r < 0.32f && Mathf.Repeat(a / (Mathf.PI * 2f) * 12f, 1f) < 0.45f * (1f - r / 0.32f) + 0.1f) return Darker(Bronze, 1.4f);   // l'étoile
                if (Mathf.Abs(r - 0.45f) < 0.04f || Mathf.Abs(r - 0.8f) < 0.04f) return Engraving;                                  // anneaux
                if (r > 0.5f && r < 0.76f && Mathf.Repeat(a / (Mathf.PI * 2f) * 10f, 1f) < 0.18f) return Engraving;               // les oiseaux
                return k;
            }, 0.9f, 0.55f);

            // Quatre grenouilles sur le bord.
            foreach (float a in new[] { 0.6f, 2.5f, 3.8f, 5.6f })
            {
                float fx = Cx + Mathf.Cos(a) * 17f, fy = TopY + Mathf.Sin(a) * 6.5f;
                cv.Piece(Ellipse(fx, fy - 1f, 2.4f, 1.8f), Patina, 1.5f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.75f) : c);
            }

            Color[,] p = cv.Finish(C(0.10f, 0.08f, 0.04f), 0.6f);
            Halo(p, Mathf.RoundToInt(Cx), Height - 1 - Mathf.RoundToInt(TopY), 9, Jade, 2.5f);
            return p;
        }

        /// <summary>
        /// Numéro du fragment d'un pixel (y depuis le bas) : le tambour se fend en quatre, en haut à gauche et à droite
        /// (la face et le haut du fût), en bas à gauche et à droite (la base), le long de cassures dentelées.
        /// </summary>
        public static int PieceOf(int x, int y)
        {
            float top = Height - 1 - y + 0.5f;
            float jag = Mathf.PingPong((x + y) * 0.8f, 2f) - 1f;
            bool right = x + 0.5f > Cx + jag * 1.5f + (top - 36f) * 0.12f;
            bool lower = top > 36f + jag * 1.5f + (x - Cx) * 0.15f;
            return (lower ? 2 : 0) + (right ? 1 : 0);
        }

        public static Color[,] Piece(int piece) => LegendArt.Cut(DrumArt(), PieceOf, piece);

        public static Vector2 PieceCenter(int piece) => LegendArt.Center(Piece(piece));

        public static Color[,] Silhouette() => LegendArt.Silhouette(DrumArt());
    }
}
