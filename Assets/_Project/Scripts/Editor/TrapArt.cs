using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Le casier à crustacés et sa bouée, dans le style en volume (<see cref="VolumeCanvas"/>, 32 px par case).
    /// Le casier (image de la boutique) : cage en lattes de bois cerclée de corde, filet tendu entre les lattes, entrée
    /// en entonnoir sur le devant, appât de poisson suspendu dedans. La bouée (posée sur l'eau) : flotteur rouge à bande
    /// blanche, petit mât et fanion, cordage qui plonge vers le casier ; deux images (la ride de l'eau change).
    /// </summary>
    public static class TrapArt
    {
        public const int TrapWidth = 56;
        public const int TrapHeight = 44;
        public const int BuoyWidth = 24;
        public const int BuoyHeight = 32;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Wood = C(0.62f, 0.42f, 0.24f);
        private static readonly Color Rope = C(0.86f, 0.76f, 0.52f);
        private static readonly Color Net = C(0.30f, 0.40f, 0.36f);
        private static readonly Color Red = C(0.88f, 0.20f, 0.16f);
        private static readonly Color White = C(0.97f, 0.95f, 0.90f);
        private static readonly Color Ink = C(0.10f, 0.07f, 0.06f);

        /// <summary>Le casier vu de trois quarts : la boîte, son filet, l'entonnoir et l'appât.</summary>
        public static Color[,] Trap()
        {
            var cv = new VolumeCanvas(TrapWidth, TrapHeight);
            const float left = 4f, right = 44f, top = 12f, bottom = 41f, depth = 8f;
            System.Func<int, int, Color, Color> mesh = (x, y, c) => (x + y) % 4 == 0 || (x - y + 64) % 4 == 0 ? Darker(c, 1.4f) : C(0.15f, 0.21f, 0.21f);

            // Le dessus et le flanc droit (en perspective), puis la face avant : du filet tendu.
            cv.Piece((x, y) => y > top - depth && y <= top && x >= left + (top - y) && x <= right + (top - y), Darker(Net, 1.1f), 1f, mesh, 0f);
            cv.Piece((x, y) => x > right && x <= right + depth && y >= top - (x - right) && y <= bottom - (x - right), Darker(Net, 0.85f), 1f, mesh, 0f);
            cv.Piece(Box(left, top, right, bottom), Net, 1f, mesh, 0f);

            // L'appât (un petit poisson argenté) suspendu au milieu.
            cv.Piece(Box(23.5f, top, 24.5f, 21), Rope, 0.6f);
            cv.Piece(Ellipse(24, 23, 5.5f, 2.6f), C(0.72f, 0.78f, 0.82f), 2.5f);

            // L'entonnoir sur le devant : anneau de corde et filet qui rentre.
            cv.Piece((x, y) => { float d = Vector2.Distance(new Vector2(x, y), new Vector2(24, 32)); return d > 4.5f && d < 7.2f; }, Rope, 2f,
                (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.82f) : c);
            cv.Piece(Ellipse(24, 32, 4.5f, 4.5f), C(0.07f, 0.11f, 0.11f), 1f, null, 0f);

            // Les lattes de bois : montants de face, cadre haut et bas, arêtes du dessus et du flanc.
            foreach (float px in new[] { left, 15f, 30f, right - 3f })
            {
                cv.Piece(Box(px, top - 1, px + 3, bottom + 1), Wood, 1.8f, (x, y, c) => y % 6 == 0 ? Darker(c, 0.85f) : c);
            }

            cv.Piece(Box(left, top - 1, right, top + 2), Darker(Wood, 1.08f), 1.8f, (x, y, c) => x % 9 == 0 ? Darker(c, 0.8f) : c);
            cv.Piece(Box(left, bottom - 2, right, bottom + 1), Darker(Wood, 0.9f), 1.8f, (x, y, c) => x % 9 == 4 ? Darker(c, 0.8f) : c);
            cv.Piece(Box(left + depth, top - depth, right + depth, top - depth + 3), Darker(Wood, 1.15f), 1.8f);
            cv.Piece(Box(right + depth - 3, top - depth, right + depth, bottom - depth), Darker(Wood, 0.8f), 1.6f);
            cv.Piece(Capsule(left + 1.5f, top, left + depth + 1.5f, top - depth + 1.5f, 1.6f), Darker(Wood, 1.05f), 1.4f);
            cv.Piece(Capsule(right - 1.5f, top, right + depth - 1.5f, top - depth + 1.5f, 1.6f), Darker(Wood, 1.05f), 1.4f);
            cv.Piece(Capsule(right - 1.5f, bottom - 0.5f, right + depth - 1.5f, bottom - depth, 1.6f), Darker(Wood, 0.85f), 1.4f);

            // La corde nouée au coin, pour la bouée.
            cv.Piece(Capsule(right + depth - 2, top - depth + 1, right + depth + 2, top - depth - 3, 1.6f), Rope, 1.2f, (x, y, c) => (x + y) % 2 == 0 ? Darker(c, 0.85f) : c);
            return cv.Finish(Ink);
        }

        /// <summary>La bouée sur l'eau, vue de dessus et un peu de face. <paramref name="frame"/> : 0 ou 1.</summary>
        public static Color[,] Buoy(int frame)
        {
            var cv = new VolumeCanvas(BuoyWidth, BuoyHeight);
            float cx = 12f, cy = 22f;

            // La ride de l'eau autour (claire, à demi transparente), qui s'élargit d'une image à l'autre.
            float ring = frame == 0 ? 9f : 10.5f;
            cv.Piece((x, y) =>
            {
                float u = (x - cx) / ring, v = (y - (cy + 3f)) / (ring * 0.42f);
                float d = u * u + v * v;
                return d <= 1f && d > 0.62f;
            }, C(0.80f, 0.94f, 1f, 0.55f), 0.6f, null, 0f, 0.9f);

            // Le cordage qui plonge vers le casier (sous l'eau, estompé).
            cv.Piece(Capsule(cx + 1, cy + 4, cx + 4, cy + 9, 1.6f), C(0.70f, 0.64f, 0.46f, 0.6f), 0.6f, null, 0f, 0.8f);

            // Le flotteur : rouge, bande blanche au milieu, bien rond.
            cv.Piece(Ellipse(cx, cy, 6.5f, 5.5f), Red, 4.5f, (x, y, c) => Mathf.Abs(y - cy) < 1.3f ? White : c);

            // Le mât et son fanion jaune.
            cv.Piece(Box(cx - 0.6f, 4f, cx + 0.9f, cy - 4f), C(0.30f, 0.26f, 0.22f), 0.8f);
            float wave = frame == 0 ? 0f : 1f;
            cv.Piece((x, y) => x > cx + 0.9f && x < cx + 7f && y > 4f + (x - cx) * 0.18f * wave && y < 9f - (x - cx) * 0.35f, C(0.98f, 0.80f, 0.22f), 1f);
            return cv.Finish(Ink);
        }
    }
}
