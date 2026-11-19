using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Artefact de Bramafan (Lac d'Annecy) : le Médaillon des palafittes, pendentif de bronze de l'âge du bronze (comme ceux
    /// retrouvés près des villages sur pilotis du lac), patiné de vert-de-gris, bord perlé, pinces d'écrevisse gravées en
    /// spirale autour d'une ambre qui luit, anneau de suspension. Même cadre que l'Idole de corail (48×64), coupé en
    /// 4 fragments en quartiers aux bords dentelés. Tableaux [x, y] avec y depuis le bas.
    /// </summary>
    public static class MedallionArt
    {
        public const int Width = LegendArt.IdolWidth;
        public const int Height = LegendArt.IdolHeight;
        public const int Fragments = 4;

        private const float Cx = 24f, Cy = 36f, Radius = 21f;   // y depuis le haut

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Bronze = C(0.72f, 0.50f, 0.22f);
        private static readonly Color BronzeDark = C(0.40f, 0.24f, 0.10f);
        private static readonly Color Verdigris = C(0.34f, 0.66f, 0.56f);
        private static readonly Color Amber = C(1f, 0.62f, 0.16f);

        /// <summary>Bronze patiné : vert-de-gris dans les creux, métal clair ailleurs.</summary>
        private static Color Patina(int x, int y, Color c)
        {
            float n = Noise(x / 3.5f, y / 3.5f);
            if (n < 0.3f) return Color.Lerp(c, Verdigris, 0.75f);
            if (n < 0.38f) return Color.Lerp(c, Verdigris, 0.35f);
            return Hash(x, y) < 0.06f ? Darker(c, 0.8f) : c;
        }

        public static Color[,] Medallion()
        {
            var cv = new VolumeCanvas(Width, Height);

            // Anneau de suspension et sa bélière.
            cv.Piece(Ring(Cx, 8f, 5.5f, 5f, 2.2f), Bronze, 2f, Patina);
            cv.Piece(Box(Cx - 3f, 11f, Cx + 3f, 17f), BronzeDark, 2f, Patina);

            // Le disque, son bord perlé.
            cv.Piece(Ellipse(Cx, Cy, Radius, Radius), Bronze, 6f, Patina);
            cv.Piece(Ring(Cx, Cy, Radius - 1f, Radius - 1f, 2.4f), BronzeDark, 1.6f, Patina);
            for (int k = 0; k < 16; k++)
            {
                float a = k * Mathf.PI / 8f;
                cv.Piece(Ellipse(Cx + Mathf.Cos(a) * (Radius - 2f), Cy + Mathf.Sin(a) * (Radius - 2f), 1.3f, 1.3f), Bronze, 1.2f, Patina);
            }

            // Deux pinces d'écrevisse gravées qui tournent en spirale autour du centre.
            foreach (float start in new[] { 0f, Mathf.PI })
            {
                var arm = new System.Collections.Generic.List<Vector2>();
                for (int i = 0; i <= 14; i++)
                {
                    float t = i / 14f;
                    float a = start + t * Mathf.PI * 1.1f;
                    float r = Mathf.Lerp(6.5f, 15f, t);
                    arm.Add(new Vector2(Cx + Mathf.Cos(a) * r, Cy + Mathf.Sin(a) * r));
                }

                cv.Piece(Tube(arm, 1.2f, 2f), BronzeDark, 1f, null, 0.9f, 0.6f);
                Vector2 end = arm[arm.Count - 1];
                Vector2 dir = (end - arm[arm.Count - 2]).normalized;
                Vector2 side = new Vector2(-dir.y, dir.x);
                cv.Piece(Poly(end + side * 2.5f, end + dir * 5f + side * 3f, end + dir * 2f), BronzeDark, 1f, null, 0.9f, 0.6f);   // doigt mobile
                cv.Piece(Poly(end - side * 2.5f, end + dir * 5f - side * 2f, end + dir * 2f), BronzeDark, 1f, null, 0.9f, 0.6f);  // doigt fixe
            }

            // L'ambre au centre, sertie.
            cv.Piece(Ellipse(Cx, Cy, 5.5f, 5.5f), BronzeDark, 2f);
            cv.Piece(Ellipse(Cx, Cy, 4f, 4f), Amber, 3f, (x, y, c) => x < Cx && y < Cy ? C(1f, 0.88f, 0.56f) : c);

            Color[,] p = cv.Finish(C(0.20f, 0.12f, 0.06f), 0.6f);
            Halo(p, Mathf.RoundToInt(Cx), Height - 1 - Mathf.RoundToInt(Cy), 9, Amber, 4f);
            return p;
        }

        /// <summary>Numéro du fragment d'un pixel (y depuis le bas) : quatre quartiers aux cassures dentelées.</summary>
        public static int PieceOf(int x, int y)
        {
            float dx = x + 0.5f - Cx, dy = y + 0.5f - (Height - Cy);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Atan2(dy, dx) + Mathf.PI * 0.25f + (Mathf.PingPong(d * 0.8f, 2f) - 1f) * 0.18f;
            int quarter = Mathf.FloorToInt(Mathf.Repeat(a, Mathf.PI * 2f) / (Mathf.PI * 0.5f));
            // 0 en haut (avec l'anneau), puis dans le sens des aiguilles d'une montre.
            switch (quarter)
            {
                case 1: return 0;
                case 0: return 1;
                case 3: return 2;
                default: return 3;
            }
        }

        public static Color[,] Piece(int piece) => LegendArt.Cut(Medallion(), PieceOf, piece);

        public static Vector2 PieceCenter(int piece) => LegendArt.Center(Piece(piece));

        public static Color[,] Silhouette() => LegendArt.Silhouette(Medallion());
    }
}
