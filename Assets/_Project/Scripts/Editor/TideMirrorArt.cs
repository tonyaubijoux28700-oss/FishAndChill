using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Artefact de l'Isonade (Baie d'Izu) : le Miroir des marées, miroir de bronze à manche comme ceux des sanctuaires
    /// japonais, vu de dos : disque patiné de vert-de-gris, bord en relief, vagues en écailles gravées tout autour, et au
    /// centre une queue de poisson hérissée de crochets qui s'enroule autour d'une perle qui luit ; manche entouré d'un
    /// cordon de soie rouge, gland qui pend. Même cadre que l'Idole de corail (48×64), coupé en 4 fragments en quartiers
    /// aux bords dentelés. Tableaux [x, y] avec y depuis le bas.
    /// </summary>
    public static class TideMirrorArt
    {
        public const int Width = LegendArt.IdolWidth;
        public const int Height = LegendArt.IdolHeight;
        public const int Fragments = 4;

        private const float Cx = 24f, Cy = 24f, Radius = 19f;   // y depuis le haut

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Bronze = C(0.66f, 0.48f, 0.24f);
        private static readonly Color BronzeDark = C(0.34f, 0.22f, 0.10f);
        private static readonly Color Verdigris = C(0.30f, 0.62f, 0.58f);
        private static readonly Color Silk = C(0.78f, 0.14f, 0.14f);
        private static readonly Color Pearl = C(0.86f, 0.92f, 1f);

        private static Color Patina(int x, int y, Color c)
        {
            float n = Noise(x / 3.5f + 5f, y / 3.5f);
            if (n < 0.3f) return Color.Lerp(c, Verdigris, 0.7f);
            if (n < 0.38f) return Color.Lerp(c, Verdigris, 0.3f);
            return Hash(x, y) < 0.06f ? Darker(c, 0.8f) : c;
        }

        public static Color[,] Mirror()
        {
            var cv = new VolumeCanvas(Width, Height);

            // Le manche, entouré de soie rouge, et le gland qui pend.
            cv.Piece(Box(Cx - 3.5f, Cy + 14f, Cx + 3.5f, 58f), Bronze, 2.5f, Patina);
            cv.Piece(Box(Cx - 4.2f, Cy + 22f, Cx + 4.2f, 54f), Silk, 2.5f, (x, y, c) => (y + x / 2) % 3 == 0 ? Darker(c, 0.65f) : c);
            cv.Piece(Ellipse(Cx, 58.5f, 4f, 2.6f), BronzeDark, 2f, Patina);
            cv.Piece(Tube(Curve(new Vector2(Cx + 3f, 54f), new Vector2(Cx + 9f, 56f), new Vector2(Cx + 10f, 60f), 8), 1f, 1f), Silk, 1f);
            cv.Piece(Poly(new Vector2(Cx + 8f, 59f), new Vector2(Cx + 12f, 59f), new Vector2(Cx + 13f, 64f), new Vector2(Cx + 7f, 64f)), Silk, 1.4f,
                (x, y, c) => x % 2 == 0 ? Darker(c, 0.7f) : c);

            // Le disque et son bord en relief.
            cv.Piece(Ellipse(Cx, Cy, Radius, Radius), Bronze, 6f, Patina);
            cv.Piece(Ring(Cx, Cy, Radius - 1f, Radius - 1f, 2.6f), BronzeDark, 1.8f, Patina);

            // Vagues en écailles (seigaiha) gravées sur l'anneau extérieur.
            for (int k = 0; k < 14; k++)
            {
                float a = k * Mathf.PI * 2f / 14f;
                float r = Radius - 5.5f;
                cv.Piece(Ring(Cx + Mathf.Cos(a) * r, Cy + Mathf.Sin(a) * r, 2.6f, 2.6f, 0.9f), BronzeDark, 0.6f, null, 0.95f, 0.6f);
            }

            cv.Piece(Ring(Cx, Cy, 11f, 11f, 1f), BronzeDark, 0.6f, null, 0.95f, 0.6f);

            // La queue à crochets qui s'enroule autour de la perle.
            var arm = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i <= 16; i++)
            {
                float t = i / 16f;
                float a = Mathf.PI * 0.6f + t * Mathf.PI * 1.6f;
                float r = Mathf.Lerp(8.5f, 5f, t);
                arm.Add(new Vector2(Cx + Mathf.Cos(a) * r, Cy + Mathf.Sin(a) * r));
            }

            cv.Piece(Tube(arm, 2.2f, 0.9f), BronzeDark, 1.2f, null, 0.9f, 0.6f);
            for (int i = 2; i < arm.Count - 2; i += 3)   // petits crochets le long de la queue
            {
                Vector2 dir = (arm[i + 1] - arm[i]).normalized;
                Vector2 outward = (arm[i] - new Vector2(Cx, Cy)).normalized;
                cv.Piece(Tube(Curve(arm[i], arm[i] + outward * 2.5f, arm[i] + outward * 2f - dir * 2f, 5), 0.6f, 0.3f), BronzeDark, 0.6f, null, 0.95f, 0.6f);
            }

            Vector2 fluke = arm[0];
            cv.Piece(Poly(fluke, fluke + new Vector2(-4f, -3f), fluke + new Vector2(-1f, 1f), fluke + new Vector2(-4f, 4f)), BronzeDark, 1f, null, 0.9f, 0.6f);

            // La perle au centre.
            cv.Piece(Ellipse(Cx, Cy, 3.6f, 3.6f), Pearl, 3f, (x, y, c) => x < Cx && y < Cy ? Color.white : Color.Lerp(c, C(0.6f, 0.7f, 0.86f), 0.25f));

            Color[,] p = cv.Finish(C(0.16f, 0.10f, 0.05f), 0.6f);
            Halo(p, Mathf.RoundToInt(Cx), Height - 1 - Mathf.RoundToInt(Cy), 8, Pearl, 3.5f);
            return p;
        }

        /// <summary>Numéro du fragment d'un pixel (y depuis le bas) : quatre quartiers aux cassures dentelées autour du disque.</summary>
        public static int PieceOf(int x, int y)
        {
            float dx = x + 0.5f - Cx, dy = y + 0.5f - (Height - Cy);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Atan2(dy, dx) + Mathf.PI * 0.25f + (Mathf.PingPong(d * 0.8f, 2f) - 1f) * 0.18f;
            int quarter = Mathf.FloorToInt(Mathf.Repeat(a, Mathf.PI * 2f) / (Mathf.PI * 0.5f));
            // 0 en haut, puis dans le sens des aiguilles d'une montre (le manche va avec le quartier du bas).
            switch (quarter)
            {
                case 1: return 0;
                case 0: return 1;
                case 3: return 2;
                default: return 3;
            }
        }

        public static Color[,] Piece(int piece) => LegendArt.Cut(Mirror(), PieceOf, piece);

        public static Vector2 PieceCenter(int piece) => LegendArt.Center(Piece(piece));

        public static Color[,] Silhouette() => LegendArt.Silhouette(Mirror());
    }
}
