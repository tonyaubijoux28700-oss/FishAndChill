using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Artefact du Spectre des épaves (Côte des Squelettes) : la Cloche de l'épave, cloche de navire en bronze sauvée
    /// d'un naufrage : anneau et corde effilochée en haut, bandes gravées, vert-de-gris et bernacles, battant qui pend,
    /// et une lueur verte au fond de la cloche. Même cadre que l'Idole de corail (48×64), coupée en 4 fragments en
    /// quartiers. Tableaux [x, y] avec y depuis le bas.
    /// </summary>
    public static class ShipBellArt
    {
        public const int Width = LegendArt.IdolWidth;
        public const int Height = LegendArt.IdolHeight;
        public const int Fragments = 4;

        private const float Cx = 24f, Cy = 34f;   // milieu de la cloche, y depuis le haut

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Bronze = C(0.66f, 0.46f, 0.20f);
        private static readonly Color BronzeDark = C(0.30f, 0.18f, 0.08f);
        private static readonly Color Verdigris = C(0.32f, 0.62f, 0.52f);
        private static readonly Color Rope = C(0.62f, 0.52f, 0.34f);
        private static readonly Color Ghost = C(0.55f, 1f, 0.72f);

        private static Color Patina(int x, int y, Color c)
        {
            float n = Noise(x / 3.5f + 7f, y / 3.5f);
            if (n < 0.28f) return Color.Lerp(c, Verdigris, 0.75f);
            if (n < 0.36f) return Color.Lerp(c, Verdigris, 0.35f);
            return Hash(x, y) < 0.06f ? Darker(c, 0.8f) : c;
        }

        /// <summary>Profil de la cloche : épaules arrondies en haut, flancs qui s'évasent, lèvre épaisse en bas.</summary>
        private static bool InBell(float x, float y)
        {
            float top = 16f, bottom = 52f;
            if (y < top || y > bottom) return false;
            float k = (y - top) / (bottom - top);
            float half = 9f + 3f * k + 6f * Mathf.Pow(k, 3f);
            if (k < 0.15f) half *= Mathf.Sqrt(k / 0.15f);   // épaules arrondies
            return Mathf.Abs(x - Cx) <= half;
        }

        public static Color[,] Bell()
        {
            var cv = new VolumeCanvas(Width, Height);

            // La corde effilochée qui passe dans l'anneau.
            cv.Piece(Tube(Curve(new Vector2(Cx - 10f, 2f), new Vector2(Cx - 2f, 6f), new Vector2(Cx, 10f), 8), 1.4f, 1.2f), Rope, 1.2f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.65f) : c, 0.9f, 0.5f);
            cv.Piece(Tube(Curve(new Vector2(Cx, 10f), new Vector2(Cx + 6f, 6f), new Vector2(Cx + 12f, 4f), 8), 1.4f, 1f), Rope, 1.2f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.65f) : c, 0.9f, 0.5f);
            cv.Piece(Ring(Cx, 11f, 4f, 4f, 1.6f), BronzeDark, 1.5f, Patina, 0.85f, 0.5f);

            // Le corps de la cloche, sombre à l'intérieur, et la lueur verte au fond.
            cv.Piece(InBell, Bronze, 8f, (x, y, c) =>
            {
                Color k = Patina(x, y, c);
                if (Mathf.Abs(y - 22f) < 1f || Mathf.Abs(y - 26f) < 0.8f || Mathf.Abs(y - 47f) < 1f) k = Darker(k, 0.65f);   // bandes gravées
                return k;
            }, 0.85f, 0.45f);
            cv.Piece(Ellipse(Cx, 52f, 17f, 3.2f), BronzeDark, 2f, Patina, 0.9f, 0.4f);
            cv.Piece(Ellipse(Cx, 52.5f, 14f, 2f), C(0.04f, 0.08f, 0.06f), 1f);

            // Le battant qui pend sous la cloche.
            cv.Piece(Box(Cx - 0.8f, 50f, Cx + 0.8f, 57f), BronzeDark, 1f);
            cv.Piece(Ellipse(Cx, 58f, 2.4f, 2.4f), Bronze, 1.5f, Patina, 0.9f, 0.5f);

            // Bernacles et une petite étoile de mer accrochées au bronze.
            foreach (Vector2 b in new[] { new Vector2(Cx - 9f, 38f), new Vector2(Cx - 6f, 41f), new Vector2(Cx + 10f, 44f), new Vector2(Cx + 7f, 30f) })
            {
                cv.Piece(Ellipse(b.x, b.y, 1.8f, 1.5f), C(0.70f, 0.68f, 0.60f), 1.2f, null, 0.9f, 0.45f);
                cv.Piece(Ellipse(b.x, b.y - 0.3f, 0.6f, 0.5f), C(0.10f, 0.10f, 0.10f), 0.4f);
            }

            var star = new List<Vector2>();
            for (int k = 0; k < 10; k++)
            {
                float a = -Mathf.PI * 0.5f + k * Mathf.PI / 5f, r = k % 2 == 0 ? 4f : 1.6f;
                star.Add(new Vector2(Cx + 4f + Mathf.Cos(a) * r, 36f + Mathf.Sin(a) * r));
            }

            cv.Piece(Poly(star.ToArray()), C(0.90f, 0.46f, 0.30f), 1f, (x, y, c) => Hash(x, y) < 0.3f ? Color.Lerp(c, Color.white, 0.3f) : c, 0.9f, 0.6f);

            Color[,] p = cv.Finish(C(0.10f, 0.06f, 0.02f), 0.6f);
            Halo(p, Mathf.RoundToInt(Cx), Height - 1 - 50, 10, Ghost, 4f);
            return p;
        }

        /// <summary>
        /// Numéro du fragment d'un pixel (y depuis le bas) : la cloche est étroite, elle se casse en quatre morceaux
        /// égaux, haut et bas, gauche et droite, aux cassures dentelées (0 haut gauche, 1 haut droite, 2 bas droite, 3 bas gauche).
        /// </summary>
        public static int PieceOf(int x, int y)
        {
            float fromTop = Height - 1 - y + 0.5f;
            bool top = fromTop < 33f + (Mathf.PingPong(x * 0.9f, 3f) - 1.5f);
            bool left = x + 0.5f < Cx + (Mathf.PingPong(fromTop * 0.7f, 3f) - 1.5f);
            return top ? (left ? 0 : 1) : (left ? 3 : 2);
        }

        public static Color[,] Piece(int piece) => LegendArt.Cut(Bell(), PieceOf, piece);

        public static Vector2 PieceCenter(int piece) => LegendArt.Center(Piece(piece));

        public static Color[,] Silhouette() => LegendArt.Silhouette(Bell());
    }
}
