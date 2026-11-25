using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Artefact du Kraken (fjords de Norvège) : la Corne des marins, corne d'appel viking en os jauni qui s'enroule du
    /// pavillon (en bas à gauche) à l'embouchure (en haut à droite), cerclée de trois bagues d'argent gravées de runes,
    /// lanière de cuir tressé, lueur dorée au fond du pavillon. Même cadre que l'Idole de corail (48×64), cassée en 4
    /// tronçons le long de sa longueur. Tableaux [x, y] avec y depuis le bas.
    /// </summary>
    public static class SeaHornArt
    {
        public const int Width = LegendArt.IdolWidth;
        public const int Height = LegendArt.IdolHeight;
        public const int Fragments = 4;

        private const float Cx = 24f, Cy = 32f;   // milieu, y depuis le haut

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Horn = C(0.86f, 0.76f, 0.54f);
        private static readonly Color HornDark = C(0.48f, 0.36f, 0.20f);
        private static readonly Color Silver = C(0.78f, 0.80f, 0.84f);
        private static readonly Color Leather = C(0.40f, 0.24f, 0.14f);
        private static readonly Color Gold = C(1f, 0.82f, 0.40f);

        private static List<Vector2> Axis() => Curve(new Vector2(12f, 52f), new Vector2(8f, 18f), new Vector2(40f, 8f), 24);

        public static Color[,] HornArt()
        {
            var cv = new VolumeCanvas(Width, Height);
            List<Vector2> axis = Axis();

            // La lanière de cuir tressé, qui pend sous la corne.
            cv.Piece(Tube(Curve(new Vector2(14f, 44f), new Vector2(26f, 62f), new Vector2(36f, 18f), 18), 1.3f, 1.3f), Leather, 1.2f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.65f) : c, 0.9f, 0.5f);

            // La corne : large au pavillon, fine à l'embouchure, stries de croissance.
            cv.Piece(Tube(axis, 9f, 2.2f), Horn, 6f, (x, y, c) =>
            {
                float n = Noise(x / 3f, y / 3f);
                Color k = Color.Lerp(c, HornDark, Mathf.Clamp01((y - 30f) / 30f) * 0.35f + (n < 0.3f ? 0.2f : 0f));
                return (x * 2 + y) % 7 == 0 ? Darker(k, 0.85f) : k;
            }, 0.85f, 0.45f);

            // Le pavillon, ouvert, sombre au fond.
            Vector2 mouth = axis[0];
            cv.Piece(Ellipse(mouth.x, mouth.y + 2f, 9f, 4f), HornDark, 2f);
            cv.Piece(Ellipse(mouth.x, mouth.y + 2f, 6.5f, 2.6f), C(0.10f, 0.06f, 0.03f), 1f);

            // Trois bagues d'argent gravées de runes.
            foreach (int i in new[] { 4, 12, 19 })
            {
                Vector2 at = axis[i], dir = (axis[i + 1] - axis[i - 1]).normalized, n = new Vector2(-dir.y, dir.x);
                float r = Mathf.Lerp(9f, 2.2f, i / (float)(axis.Count - 1)) + 1f;
                Vector2 a = at - n * r, b = at + n * r;
                cv.Piece(Capsule(a.x, a.y, b.x, b.y, 1.6f), Silver, 1.5f, (x, y, c) => (x * 3 + y * 5) % 4 == 0 ? Darker(c, 0.55f) : c, 0.9f, 0.6f);
            }

            // L'embouchure, cerclée d'argent.
            Vector2 tip = axis[axis.Count - 1];
            cv.Piece(Ellipse(tip.x, tip.y, 2.6f, 2.6f), Silver, 1.5f);

            Color[,] p = cv.Finish(C(0.12f, 0.08f, 0.04f), 0.6f);
            Halo(p, Mathf.RoundToInt(mouth.x), Height - 1 - Mathf.RoundToInt(mouth.y + 2f), 10, Gold, 4f);
            return p;
        }

        /// <summary>
        /// Numéro du fragment d'un pixel (y depuis le bas) : la corne se casse en 4 tronçons le long de sa longueur, du
        /// pavillon (0) à l'embouchure (3), aux cassures dentelées ; chaque pixel va au tronçon le plus proche.
        /// </summary>
        public static int PieceOf(int x, int y)
        {
            List<Vector2> axis = Axis();
            var p = new Vector2(x + 0.5f, Height - 1 - y + 0.5f);
            int best = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < axis.Count; i++)
            {
                float d = (axis[i] - p).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = i; }
            }

            float t = best / (float)(axis.Count - 1) + (Mathf.PingPong((p.x + p.y) * 0.7f, 2f) - 1f) * 0.03f;
            return Mathf.Clamp(Mathf.FloorToInt(t * Fragments), 0, Fragments - 1);
        }

        public static Color[,] Piece(int piece) => LegendArt.Cut(HornArt(), PieceOf, piece);

        public static Vector2 PieceCenter(int piece) => LegendArt.Center(Piece(piece));

        public static Color[,] Silhouette() => LegendArt.Silhouette(HornArt());
    }
}
