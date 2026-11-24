using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Artefact de Rannamaari (Atoll des Maldives) : la Lanterne de corail, celle qui guide le démon depuis l'horizon.
    /// Cage de branches de corail rouge qui se rejoignent en anneau, autour d'une perle qui luit comme la lune ; pied de
    /// corail mort où sont posés des cauris (l'ancienne monnaie des îles), deux cauris qui pendent sur les côtés. Même
    /// cadre que l'Idole de corail (48×64), coupée en 4 fragments en quartiers. Tableaux [x, y] avec y depuis le bas.
    /// </summary>
    public static class CoralLanternArt
    {
        public const int Width = LegendArt.IdolWidth;
        public const int Height = LegendArt.IdolHeight;
        public const int Fragments = 4;

        private const float Cx = 24f, Cy = 31f;   // la perle, y depuis le haut

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Cord = C(0.36f, 0.24f, 0.10f);
        private static readonly Color Coral = C(0.80f, 0.26f, 0.22f);
        private static readonly Color Cowrie = C(0.94f, 0.88f, 0.74f);
        private static readonly Color Moon = C(0.80f, 0.94f, 1f);

        private static Color Worn(int x, int y, Color c)
        {
            float n = Noise(x / 3f + 2f, y / 3f);
            if (n < 0.25f) return Darker(c, 0.75f);
            return Hash(x, y) < 0.06f ? Color.Lerp(c, Color.white, 0.25f) : c;
        }

        private static void CowrieShell(VolumeCanvas cv, float x, float y, bool hanging)
        {
            Color slit = C(0.36f, 0.26f, 0.18f);
            cv.Piece(Ellipse(x, y, hanging ? 1.9f : 2.4f, hanging ? 2.6f : 1.8f), Cowrie, 1.6f,
                (px, py, c) => (hanging ? Mathf.Abs(px - x) : Mathf.Abs(py - y)) < 0.5f ? slit : c, 0.9f, 0.6f);
        }

        public static Color[,] Lantern()
        {
            var cv = new VolumeCanvas(Width, Height);

            // Pied : rocher de corail mort.
            cv.Piece(Ellipse(24f, 54f, 17f, 8f), C(0.62f, 0.58f, 0.50f), 4f, (x, y, c) => Noise(x / 2.5f, y / 2.5f) < 0.35f ? Darker(c, 0.7f) : c);

            // Cage de branches de corail qui se rejoignent en haut, avec leurs rameaux.
            foreach (float bx in new[] { 10f, 17f, 31f, 38f })
            {
                float side = bx < 24f ? -1f : 1f;
                var b = new Vector2(bx, 50f);
                var top = new Vector2(24f + (bx - 24f) * 0.2f, 10f);
                cv.Piece(Tube(Curve(b, new Vector2(bx + side * 6f, 28f), top, 16), 2.2f, 1.2f), Coral, 2f,
                    (x, y, c) => Hash(x, y) < 0.18f ? Color.Lerp(c, Color.white, 0.25f) : Worn(x, y, c), 0.85f, 0.55f);
                var twig = new Vector2(bx + side * 5f, 32f);
                cv.Piece(Tube(Curve(twig, twig + new Vector2(side * 4f, -4f), twig + new Vector2(side * 5f, -9f), 6), 1.1f, 0.5f), Coral, 1.2f, null, 0.85f, 0.55f);
            }

            cv.Piece(Ring(24f, 7f, 4f, 4f, 1.5f), Coral, 1.5f, Worn);

            // La perle qui luit au milieu.
            cv.Piece(Ellipse(Cx, Cy, 6.5f, 6.5f), C(0.90f, 0.95f, 1f), 4f, (x, y, c) => x < Cx - 1 && y < Cy - 1 ? Color.white : Color.Lerp(c, Moon, 0.4f), 0.95f, 0.95f);

            // Cauris posés sur le pied, et deux qui pendent sur les côtés.
            for (int k = 0; k < 5; k++)
            {
                CowrieShell(cv, 12f + k * 6f, 51f + (k % 2) * 1.5f, false);
            }

            foreach (float x in new[] { 9f, 39f })
            {
                cv.Piece(Tube(new List<Vector2> { new Vector2(x, 26), new Vector2(x, 34) }, 0.4f, 0.4f), Cord, 0.5f);
                CowrieShell(cv, x, 37f, true);
            }

            Color[,] p = cv.Finish(C(0.14f, 0.08f, 0.04f), 0.6f);
            Halo(p, Mathf.RoundToInt(Cx), Height - 1 - Mathf.RoundToInt(Cy), 12, Moon, 4f);
            return p;
        }

        /// <summary>Numéro du fragment d'un pixel (y depuis le bas) : quatre quartiers aux cassures dentelées autour de la perle.</summary>
        public static int PieceOf(int x, int y)
        {
            float dx = x + 0.5f - Cx, dy = y + 0.5f - (Height - Cy);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Atan2(dy, dx) + Mathf.PI * 0.25f + (Mathf.PingPong(d * 0.8f, 2f) - 1f) * 0.18f;
            int quarter = Mathf.FloorToInt(Mathf.Repeat(a, Mathf.PI * 2f) / (Mathf.PI * 0.5f));
            // 0 en haut, puis dans le sens des aiguilles d'une montre (le pied va avec le quartier du bas).
            switch (quarter)
            {
                case 1: return 0;
                case 0: return 1;
                case 3: return 2;
                default: return 3;
            }
        }

        public static Color[,] Piece(int piece) => LegendArt.Cut(Lantern(), PieceOf, piece);

        public static Vector2 PieceCenter(int piece) => LegendArt.Center(Piece(piece));

        public static Color[,] Silhouette() => LegendArt.Silhouette(Lantern());
    }
}
