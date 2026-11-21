using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Artefact de Jabutiaçu (Fleuve Amazone) : la Grenouille de jade, amulette de pierre verte en forme de grenouille
    /// accroupie vue de dessus (d'après les amulettes-grenouilles taillées des peuples de l'Amazone), veinée et polie,
    /// pattes repliées gravées, yeux incrustés d'or qui luisent, deux trous percés où passe un cordon de fibres tressées.
    /// Même cadre que l'Idole de corail (48×64), coupé en 4 fragments en quartiers aux bords dentelés.
    /// Tableaux [x, y] avec y depuis le bas.
    /// </summary>
    public static class JadeFrogArt
    {
        public const int Width = LegendArt.IdolWidth;
        public const int Height = LegendArt.IdolHeight;
        public const int Fragments = 4;

        private const float Cx = 24f, Cy = 36f;   // y depuis le haut

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Jade = C(0.28f, 0.60f, 0.42f);
        private static readonly Color JadeDark = C(0.10f, 0.30f, 0.20f);
        private static readonly Color JadeLight = C(0.62f, 0.88f, 0.66f);
        private static readonly Color Gold = C(1f, 0.80f, 0.30f);
        private static readonly Color Fiber = C(0.56f, 0.42f, 0.24f);

        /// <summary>Pierre veinée : veines claires et nuages sombres, quelques éclats.</summary>
        private static Color Stone(int x, int y, Color c)
        {
            if (Mathf.Abs(Noise(x / 5f + 3f, y / 9f) - 0.5f) < 0.035f) return Color.Lerp(c, JadeLight, 0.6f);
            float n = Noise(x / 3.5f, y / 3.5f);
            if (n < 0.28f) return Color.Lerp(c, JadeDark, 0.45f);
            return Hash(x, y) < 0.04f ? Color.Lerp(c, JadeLight, 0.5f) : c;
        }

        private static void Groove(VolumeCanvas cv, Vector2 a, Vector2 control, Vector2 b)
            => cv.Piece(Tube(Curve(a, control, b, 8), 0.7f, 0.7f), JadeDark, 0.6f, null, 0.95f, 0.6f);

        public static Color[,] Frog()
        {
            var cv = new VolumeCanvas(Width, Height);

            // Le cordon de fibres tressées qui passe dans les deux trous.
            cv.Piece(Tube(Curve(new Vector2(16, 22), new Vector2(24, -8), new Vector2(32, 22), 20), 1.4f, 1.4f), Fiber, 1.2f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.7f) : c);

            foreach (float side in new[] { -1f, 1f })
            {
                float S(float dx) => Cx + side * dx;

                // Patte arrière repliée : grosse cuisse, pied à trois doigts ramené sous le ventre.
                cv.Piece(Ellipse(S(11f), 50f, 6.5f, 8f), Jade, 4f, Stone);
                cv.Piece(Capsule(S(14f), 57f, S(5f), 59.5f, 2.6f), Jade, 2.2f, Stone);
                for (int k = 0; k < 3; k++)
                {
                    cv.Piece(Ellipse(S(3.5f), 56.5f + k * 2.4f, 1.6f, 1.2f), Jade, 1.2f, Stone);
                }

                // Patte avant repliée contre le flanc, main sous le menton.
                cv.Piece(Capsule(S(11f), 29f, S(15f), 39f, 2.8f), Jade, 2.4f, Stone);
                cv.Piece(Capsule(S(15f), 39f, S(9f), 43f, 2.4f), Jade, 2.2f, Stone);
                for (int k = 0; k < 3; k++)
                {
                    cv.Piece(Ellipse(S(8f - k * 1.8f), 43.5f + k * 0.5f, 1.4f, 1.6f), Jade, 1.2f, Stone);
                }
            }

            // Corps et tête d'un seul bloc poli.
            cv.Piece(Union(Ellipse(Cx, 37f, 13f, 14f), Ellipse(Cx, 21f, 11f, 8.5f)), Jade, 7f, Stone);

            // Gravures : arête du dos, chevrons, bouche.
            Groove(cv, new Vector2(Cx, 28), new Vector2(Cx + 0.5f, 38), new Vector2(Cx, 48));
            for (int k = 0; k < 3; k++)
            {
                float y = 32f + k * 5f;
                Groove(cv, new Vector2(Cx - 7f + k, y - 3f), new Vector2(Cx - 3f, y), new Vector2(Cx, y + 1f));
                Groove(cv, new Vector2(Cx + 7f - k, y - 3f), new Vector2(Cx + 3f, y), new Vector2(Cx, y + 1f));
            }

            Groove(cv, new Vector2(Cx - 7f, 25f), new Vector2(Cx, 28.5f), new Vector2(Cx + 7f, 25f));

            // Les deux trous percés (le cordon y entre).
            foreach (float x in new[] { 16f, 32f })
            {
                cv.Piece(Ellipse(x, 22f, 1.8f, 1.8f), C(0.04f, 0.10f, 0.07f), 1f);
            }

            // Yeux bombés incrustés d'or.
            foreach (float x in new[] { Cx - 6.5f, Cx + 6.5f })
            {
                cv.Piece(Ellipse(x, 16.5f, 4.2f, 3.8f), Jade, 3f, Stone);
                cv.Piece(Ellipse(x, 16.5f, 2.4f, 2.2f), Gold, 2f, (px, py, c) => px < x && py < 16.5f ? C(1f, 0.95f, 0.70f) : c);
            }

            Color[,] p = cv.Finish(C(0.04f, 0.14f, 0.09f), 0.6f);
            foreach (float x in new[] { Cx - 6.5f, Cx + 6.5f }) Halo(p, Mathf.RoundToInt(x), Height - 1 - 16, 6, Gold, 2.5f);
            return p;
        }

        /// <summary>Numéro du fragment d'un pixel (y depuis le bas) : quatre quartiers aux cassures dentelées.</summary>
        public static int PieceOf(int x, int y)
        {
            float dx = x + 0.5f - Cx, dy = y + 0.5f - (Height - Cy);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Atan2(dy, dx) + Mathf.PI * 0.25f + (Mathf.PingPong(d * 0.8f, 2f) - 1f) * 0.18f;
            int quarter = Mathf.FloorToInt(Mathf.Repeat(a, Mathf.PI * 2f) / (Mathf.PI * 0.5f));
            switch (quarter)   // 0 en haut (tête et cordon), puis dans le sens des aiguilles d'une montre
            {
                case 1: return 0;
                case 0: return 1;
                case 3: return 2;
                default: return 3;
            }
        }

        public static Color[,] Piece(int piece) => LegendArt.Cut(Frog(), PieceOf, piece);

        public static Vector2 PieceCenter(int piece) => LegendArt.Center(Piece(piece));

        public static Color[,] Silhouette() => LegendArt.Silhouette(Frog());
    }
}
