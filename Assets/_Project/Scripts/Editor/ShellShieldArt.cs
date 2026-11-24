using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Artefact du Moha-moha (Grande Barrière) : le Bouclier d'écaille, bouclier ovale fait de plaques d'écaille de
    /// tortue blondes et brunes (marbrures d'écaille), gravé au centre d'une tortue au long cou entourée de vagues,
    /// cerclé d'une corde de fibre tressée, avec une petite branche de corail qui a poussé sur le bord. Même cadre que
    /// l'Idole de corail (48×64), coupé en 4 fragments en quartiers. Tableaux [x, y] avec y depuis le bas.
    /// </summary>
    public static class ShellShieldArt
    {
        public const int Width = LegendArt.IdolWidth;
        public const int Height = LegendArt.IdolHeight;
        public const int Fragments = 4;

        private const float Cx = 24f, Cy = 32f, Rx = 19f, Ry = 27f;   // y depuis le haut

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Amber = C(0.82f, 0.56f, 0.22f);
        private static readonly Color Brown = C(0.36f, 0.18f, 0.08f);
        private static readonly Color Engrave = C(0.22f, 0.11f, 0.05f);
        private static readonly Color Cord = C(0.66f, 0.56f, 0.36f);
        private static readonly Color Coral = C(0.86f, 0.40f, 0.42f);

        /// <summary>Écaille de tortue : fond blond, taches brunes fondues, joints des plaques foncés.</summary>
        private static Color Tortoiseshell(int x, int y, Color c)
        {
            float n = Noise(x / 3f + 3f, y / 5f);
            Color k = Color.Lerp(c, Brown, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 0.8f, n)) * 0.7f);
            // Joints des plaques : une croix et deux arcs.
            float dx = x + 0.5f - Cx, dy = y + 0.5f - Cy;
            bool seam = Mathf.Abs(dx) < 0.7f || Mathf.Abs(dy + 6f) < 0.6f || Mathf.Abs(dy - 10f) < 0.6f;
            return seam ? Darker(k, 0.55f) : Hash(x, y) < 0.05f ? Color.Lerp(k, Color.white, 0.2f) : k;
        }

        public static Color[,] Shield()
        {
            var cv = new VolumeCanvas(Width, Height);
            cv.Piece(Ellipse(Cx, Cy, Rx, Ry), Amber, 7f, Tortoiseshell, 0.85f, 0.45f);

            // Corde tressée tout autour.
            cv.Piece(Ring(Cx, Cy, Rx, Ry, 2.2f), Cord, 1.6f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.65f) : c, 0.9f, 0.5f);

            // La gravure : tortue au long cou, au centre.
            cv.Piece(Ellipse(Cx - 1f, Cy + 2f, 7f, 5f), Engrave, 0.5f, null, 0.95f, 0.5f);
            cv.Piece(Ellipse(Cx - 1f, Cy + 2f, 5.5f, 3.6f), C(0.60f, 0.38f, 0.16f), 0.6f, (x, y, c) => (x + y) % 3 == 0 ? Engrave : c, 0.95f, 0.5f);
            cv.Piece(Tube(Curve(new Vector2(Cx + 5f, Cy + 1f), new Vector2(Cx + 9f, Cy - 2f), new Vector2(Cx + 8f, Cy - 8f), 8), 1.1f, 0.9f), Engrave, 0.5f, null, 0.95f, 0.5f);
            cv.Piece(Ellipse(Cx + 8f, Cy - 9f, 1.8f, 1.4f), Engrave, 0.5f, null, 0.95f, 0.5f);
            foreach (Vector2 f in new[] { new Vector2(Cx + 3f, Cy + 6f), new Vector2(Cx - 5f, Cy + 6f) })
            {
                cv.Piece(Tube(new List<Vector2> { f, f + new Vector2(-2f, 3f) }, 0.9f, 0.6f), Engrave, 0.5f, null, 0.95f, 0.5f);
            }

            cv.Piece(Tube(new List<Vector2> { new Vector2(Cx - 7f, Cy + 3f), new Vector2(Cx - 11f, Cy + 5f) }, 0.8f, 0.5f), Engrave, 0.5f, null, 0.95f, 0.5f);

            // Vagues gravées en haut et en bas.
            foreach (float wy in new[] { Cy - 15f, Cy + 16f })
            {
                for (int k = 0; k < 3; k++)
                {
                    float wx = Cx - 8f + k * 8f;
                    cv.Piece(Ring(wx, wy, 3f, 2.4f, 0.8f), Engrave, 0.5f, null, 0.95f, 0.5f);
                }
            }

            // Une petite branche de corail qui a poussé sur le bord.
            var branch = Curve(new Vector2(Cx + Rx - 3f, Cy + 12f), new Vector2(Cx + Rx + 1f, Cy + 7f), new Vector2(Cx + Rx - 1f, Cy + 2f), 8);
            cv.Piece(Tube(branch, 1.4f, 0.8f), Coral, 1.2f, null, 0.85f, 0.5f);
            cv.Piece(Tube(Curve(branch[4], branch[4] + new Vector2(3f, -1f), branch[4] + new Vector2(3f, -4f), 5), 0.9f, 0.6f), Coral, 1f, null, 0.85f, 0.5f);

            Color[,] p = cv.Finish(C(0.12f, 0.06f, 0.02f), 0.6f);
            Halo(p, Mathf.RoundToInt(Cx), Height - 1 - Mathf.RoundToInt(Cy), 9, C(1f, 0.82f, 0.46f), 4f);
            return p;
        }

        /// <summary>Numéro du fragment d'un pixel (y depuis le bas) : quatre quartiers aux cassures dentelées.</summary>
        public static int PieceOf(int x, int y)
        {
            float dx = x + 0.5f - Cx, dy = y + 0.5f - (Height - Cy);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Atan2(dy, dx) + Mathf.PI * 0.25f + (Mathf.PingPong(d * 0.8f, 2f) - 1f) * 0.18f;
            int quarter = Mathf.FloorToInt(Mathf.Repeat(a, Mathf.PI * 2f) / (Mathf.PI * 0.5f));
            switch (quarter)
            {
                case 1: return 0;
                case 0: return 1;
                case 3: return 2;
                default: return 3;
            }
        }

        public static Color[,] Piece(int piece) => LegendArt.Cut(Shield(), PieceOf, piece);

        public static Vector2 PieceCenter(int piece) => LegendArt.Center(Piece(piece));

        public static Color[,] Silhouette() => LegendArt.Silhouette(Shield());
    }
}
