using System;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les repères de la Grande Barrière, dans le style en volume (32 px par case, y depuis le haut pendant le dessin) :
    /// maison « Queenslander » en bois sur pilotis avec sa véranda et sa citerne, tour des sauveteurs et ses drapeaux rouge
    /// et jaune, plateforme de plongée amarrée sur le récif, barque en aluminium tirée sur le sable.
    /// </summary>
    public static class ReefLandmarkArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Weatherboard = C(0.94f, 0.92f, 0.84f);
        private static readonly Color Trim = C(0.30f, 0.50f, 0.40f);
        private static readonly Color Iron = C(0.70f, 0.74f, 0.74f);
        private static readonly Color RoofRed = C(0.66f, 0.22f, 0.18f);
        private static readonly Color Timber = C(0.56f, 0.40f, 0.26f);
        private static readonly Color Ink = C(0.10f, 0.08f, 0.07f);
        private static readonly Color WaterShadow = C(0.04f, 0.24f, 0.36f, 0.38f);
        private static readonly Color Foam = C(0.92f, 0.98f, 1f, 0.75f);

        private static void Shadow(Color[,] p, float cx, float cy, float rx, float ry, Color color)
        {
            for (int x = 0; x < p.GetLength(0); x++)
            {
                for (int y = 0; y < p.GetLength(1); y++)
                {
                    float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                    if (u * u + v * v <= 1f && p[x, y].a < 0.01f) p[x, y] = color;
                }
            }
        }

        /// <summary>Triangle plein (y depuis le haut).</summary>
        private static Func<float, float, bool> Triangle(Vector2 a, Vector2 b, Vector2 c) => (x, y) =>
        {
            float Side(Vector2 p0, Vector2 p1) => (p1.x - p0.x) * (y - p0.y) - (p1.y - p0.y) * (x - p0.x);
            float s0 = Side(a, b), s1 = Side(b, c), s2 = Side(c, a);
            return (s0 >= 0 && s1 >= 0 && s2 >= 0) || (s0 <= 0 && s1 <= 0 && s2 <= 0);
        };

        /// <summary>Tôle ondulée : ondes verticales claires et sombres.</summary>
        private static Color Corrugated(int x, int y, Color c) => x % 4 == 0 ? Darker(c, 0.74f) : x % 4 == 1 ? Darker(c, 1.16f) : c;

        // ---------- Maison Queenslander ----------

        public const int HouseWidth = 224;
        public const int HouseHeight = 208;
        public static readonly Vector2 HousePivot = new Vector2(0.5f, 6f / HouseHeight);

        /// <summary>
        /// Maison « Queenslander » (7 cases de large) : posée sur des pilotis de bois avec un treillis entre eux, escalier au
        /// milieu, véranda tout le long de la façade avec sa balustrade blanche et son auvent en tôle rayée bombée, murs de
        /// planches à clin crème, portes-fenêtres à volets verts, haut toit de tôle rouge en pavillon, citerne d'eau en tôle
        /// ondulée sur son support à droite, bougainvillier à gauche.
        /// </summary>
        public static Color[,] House()
        {
            const float axis = 104f;
            var cv = new VolumeCanvas(HouseWidth, HouseHeight);
            Func<int, int, Color, Color> boards = (x, y, c) => y % 6 == 0 ? Darker(c, 0.78f) : y % 6 == 1 ? Darker(c, 1.08f) : c;

            // Pilotis et treillis.
            cv.Piece(Box(20, 160, 188, 196), C(0.28f, 0.22f, 0.18f), 1f);
            cv.Piece(Box(22, 162, 186, 196), Trim, 1.5f, (x, y, c) => ((x + y) % 8 == 0 || (x - y + 400) % 8 == 0) ? Darker(c, 1.15f) : (x + y) % 8 < 3 || (x - y + 400) % 8 < 3 ? C(0.20f, 0.16f, 0.14f) : c);
            foreach (int px in new[] { 22, 62, 146, 184 }) cv.Piece(Box(px - 3, 156, px + 3, 200), Timber, 2f);

            // Murs de planches, en retrait derrière la véranda.
            cv.Piece(Box(32, 84, 176, 160), Weatherboard, 3f, boards);
            // Portes-fenêtres à volets verts.
            foreach (float wc in new[] { axis - 46f, axis + 46f })
            {
                int wx = (int)wc;
                cv.Piece(Box(wx - 10, 104, wx + 10, 150), C(0.22f, 0.30f, 0.32f), 1f, (x, y, c) => x == wx || y == 126 ? C(0.86f, 0.86f, 0.80f) : c);
                cv.Piece(Box(wx - 18, 102, wx - 10, 150), Trim, 1.5f, (x, y, c) => y % 3 == 0 ? Darker(c, 0.75f) : c);
                cv.Piece(Box(wx + 10, 102, wx + 18, 150), Trim, 1.5f, (x, y, c) => y % 3 == 0 ? Darker(c, 0.75f) : c);
            }

            cv.Piece(Box(axis - 12, 100, axis + 12, 156), Trim, 2f, (x, y, c) => x == (int)axis ? Darker(c, 0.7f) : (y - 100) % 14 == 0 ? Darker(c, 0.82f) : c);
            cv.Piece(Box(axis - 8, 104, axis + 8, 122), C(0.50f, 0.70f, 0.76f), 1f, (x, y, c) => x == (int)axis ? Trim : c);   // vitre au-dessus

            // Haut toit de tôle rouge en pavillon, centré sur les murs, et son épi.
            cv.Piece((x, y) => y > 14 && y < 88 && Mathf.Abs(x - axis) < 26f + (y - 14f) * 1.02f, RoofRed, 7f, Corrugated);
            cv.Piece(Box(axis - 102, 82, axis + 102, 90), C(0.88f, 0.88f, 0.84f), 1.5f);   // gouttière
            cv.Piece(Capsule(axis, 4f, axis, 16f, 2f), Iron, 1.5f);
            // Auvent de la véranda en tôle rayée bombée (bullnose).
            cv.Piece((x, y) => x > 18 && x < 190 && y > 90 && y < 104 - Mathf.Abs(x - axis) * 0f, Iron, 5f,
                (x, y, c) => (x / 6) % 2 == 0 ? C(0.24f, 0.46f, 0.36f) : C(0.92f, 0.92f, 0.88f));
            // Poteaux et balustrade de la véranda.
            foreach (int px in new[] { 22, 50, 78, 130, 158, 186 }) cv.Piece(Box(px - 2, 102, px + 2, 162), Weatherboard, 1.5f);
            Func<float, float, bool> rail = (x, y) => x > 20 && x < 188 && (Mathf.Abs(x - axis) > 14f) && ((y > 140 && y < 144) || (y > 156 && y < 160) || (y > 144 && y < 156 && (int)x % 6 < 2));
            cv.Piece(rail, Weatherboard, 1f);
            // Escalier au milieu.
            for (int s = 0; s < 6; s++) cv.Piece(Box(axis - 13 - s, 160 + s * 7, axis + 13 + s, 166 + s * 7), Timber, 1.5f, (x, y, c) => y % 7 == 0 ? Darker(c, 1.15f) : c);
            cv.Piece(Union(Capsule(axis - 16, 156, axis - 20, 200, 1.6f), Capsule(axis + 16, 156, axis + 20, 200, 1.6f)), Weatherboard, 1.5f);

            // Citerne d'eau en tôle ondulée sur son support.
            cv.Piece(Union(Box(194, 160, 198, 202), Box(214, 160, 218, 202), Box(192, 158, 220, 164)), Timber, 1.5f);
            cv.Piece(Box(190, 112, 222, 160), Iron, 6f, (x, y, c) => y % 5 == 0 ? Darker(c, 0.74f) : y % 5 == 1 ? Darker(c, 1.14f) : c);
            cv.Piece(Ellipse(206f, 112f, 16f, 4f), Darker(Iron, 1.1f), 2f);
            // Bougainvillier contre le pilier de gauche.
            cv.Piece(Union(Ellipse(14f, 168f, 13f, 14f), Ellipse(10f, 186f, 10f, 10f), Ellipse(22f, 150f, 8f, 9f)), C(0.24f, 0.46f, 0.22f), 5f,
                (x, y, c) => (x * 5 + y * 3) % 7 == 0 || (x * 3 + y) % 11 == 0 ? C(0.88f, 0.20f, 0.56f) : (x + y) % 9 == 0 ? C(0.96f, 0.40f, 0.70f) : c);

            Color[,] p = cv.Finish(Ink, 0.5f);
            Shadow(p, axis + 6f, 5f, 108f, 5f, C(0.06f, 0.12f, 0.06f, 0.3f));
            return p;
        }

        // ---------- Tour des sauveteurs ----------

        public const int TowerWidth = 112;
        public const int TowerHeight = 144;
        public static readonly Vector2 TowerPivot = new Vector2(0.5f, 6f / TowerHeight);

        /// <summary>
        /// Tour des sauveteurs : cabine jaune et rouge à grandes vitres sur quatre jambes croisillonnées, toit plat, échelle,
        /// bouée de sauvetage accrochée, drapeau rouge et jaune sur son mât, deuxième drapeau planté dans le sable, planche
        /// de sauvetage appuyée contre une jambe.
        /// </summary>
        public static Color[,] Tower()
        {
            var cv = new VolumeCanvas(TowerWidth, TowerHeight);
            Color yellow = C(0.98f, 0.80f, 0.18f), red = C(0.86f, 0.18f, 0.14f), white = C(0.96f, 0.96f, 0.94f);
            // Jambes et croisillons.
            cv.Piece(Union(Capsule(34f, 70f, 28f, 138f, 2.4f), Capsule(78f, 70f, 84f, 138f, 2.4f), Capsule(46f, 72f, 44f, 134f, 2f), Capsule(66f, 72f, 68f, 134f, 2f)), white, 2f);
            cv.Piece(Union(Capsule(30f, 96f, 82f, 128f, 1.2f), Capsule(82f, 96f, 30f, 128f, 1.2f), Capsule(32f, 92f, 80f, 92f, 1.2f)), Darker(white, 0.86f), 1f);
            // Échelle.
            cv.Piece(Union(Capsule(52f, 74f, 50f, 140f, 1.2f), Capsule(62f, 74f, 64f, 140f, 1.2f)), Timber, 1f);
            for (int r = 0; r < 8; r++) cv.Piece(Box(50, 80 + r * 8, 64, 82 + r * 8), Timber, 1f);
            // Cabine : bas rouge, vitres, toit jaune.
            cv.Piece(Box(24, 30, 88, 74), yellow, 4f, (x, y, c) => y > 58 ? red : c);
            cv.Piece(Box(30, 36, 82, 56), C(0.36f, 0.58f, 0.70f), 1f, (x, y, c) => (x - 30) % 13 == 0 ? yellow : (x + y) % 11 == 0 ? C(0.70f, 0.86f, 0.92f) : c);
            cv.Piece(Box(16, 20, 96, 32), red, 3f, (x, y, c) => x % 8 < 4 ? c : Darker(c, 0.88f));
            cv.Piece(Box(20, 68, 92, 74), white, 1.5f);   // balcon
            // Bouée de sauvetage sur le balcon.
            cv.Piece((x, y) => { float d = Vector2.Distance(new Vector2(x, y), new Vector2(76f, 66f)); return d < 7f && d > 3.5f; }, white, 2f,
                (x, y, c) => (Mathf.Atan2(y - 66f, x - 76f) * 2f / Mathf.PI + 4f) % 1f < 0.5f ? red : c);
            // Mât et drapeau rouge et jaune (moitié haute rouge, moitié basse jaune).
            cv.Piece(Capsule(92f, 2f, 92f, 22f, 1.2f), Iron, 1f);
            cv.Piece(Box(93, 2, 110, 14), yellow, 1.5f, (x, y, c) => y < 8 ? red : c);
            // Drapeau planté dans le sable, planche de sauvetage.
            cv.Piece(Capsule(10f, 96f, 10f, 140f, 1.1f), Iron, 1f);
            cv.Piece(Box(11, 96, 26, 108), yellow, 1.5f, (x, y, c) => y < 102 ? red : c);
            cv.Piece(Capsule(96f, 92f, 102f, 138f, 4f), C(0.98f, 0.92f, 0.86f), 3f, (x, y, c) => Mathf.Abs(x - 96f - (y - 92f) * 0.13f) < 1.2f ? red : c);
            Color[,] p = cv.Finish(Ink, 0.5f);
            Shadow(p, 58f, 5f, 40f, 5f, C(0.30f, 0.28f, 0.22f, 0.3f));
            return p;
        }

        // ---------- Plateforme de plongée ----------

        public const int PontoonWidth = 288;
        public const int PontoonHeight = 160;

        /// <summary>
        /// Plateforme de plongée amarrée sur le récif (vue au milieu, sur l'eau) : grand pont flottant gris clair à rambarde,
        /// voiles d'ombrage blanches tendues sur des mâts, petite cabine bleue, toboggan jaune vers l'eau, échelles et
        /// plateforme de mise à l'eau, bouées d'amarrage, écume et ombre dans l'eau.
        /// </summary>
        public static Color[,] Pontoon()
        {
            var cv = new VolumeCanvas(PontoonWidth, PontoonHeight);
            Color deck = C(0.82f, 0.82f, 0.80f), hull = C(0.30f, 0.46f, 0.66f), white = C(0.98f, 0.98f, 0.96f), yellow = C(0.98f, 0.78f, 0.16f);
            // Flotteurs et pont.
            cv.Piece(Box(20, 112, 268, 134), hull, 4f, (x, y, c) => y > 128 ? Darker(c, 0.7f) : c);
            cv.Piece(Box(20, 74, 268, 116), deck, 3f, (x, y, c) => x % 10 == 0 ? Darker(c, 0.84f) : (x * 3 + y) % 23 == 0 ? Darker(c, 0.92f) : c);
            // Plateforme basse de mise à l'eau, à droite.
            cv.Piece(Box(232, 118, 284, 136), Darker(deck, 0.9f), 2f, (x, y, c) => x % 8 == 0 ? Darker(c, 0.8f) : c);
            // Cabine bleue.
            cv.Piece(Box(40, 46, 104, 100), C(0.26f, 0.50f, 0.74f), 4f, (x, y, c) => y > 60 && y < 76 && (x - 44) % 14 < 9 ? C(0.70f, 0.88f, 0.94f) : c);
            cv.Piece(Box(34, 38, 110, 50), white, 3f);
            // Mâts et voiles d'ombrage triangulaires.
            foreach (int mx in new[] { 128, 188, 248 }) cv.Piece(Capsule(mx, 20f, mx, 100f, 1.6f), Darker(white, 0.86f), 1.5f);
            cv.Piece(Triangle(new Vector2(128f, 22f), new Vector2(190f, 30f), new Vector2(134f, 62f)), white, 4f, (x, y, c) => (x + y) % 13 == 0 ? Darker(c, 0.92f) : c, 0.75f);
            cv.Piece(Triangle(new Vector2(250f, 20f), new Vector2(186f, 32f), new Vector2(244f, 60f)), C(0.94f, 0.94f, 0.90f), 4f, (x, y, c) => (x - y) % 13 == 0 ? Darker(c, 0.92f) : c, 0.75f);
            // Rambarde.
            cv.Piece((x, y) => x > 20 && x < 268 && ((y > 72 && y < 75) || (y > 74 && y < 86 && (int)x % 14 < 2)), Iron, 1f);
            // Toboggan jaune vers l'eau, à gauche.
            cv.Piece(Union(Capsule(26f, 70f, 4f, 128f, 4f), Box(22, 64, 34, 76)), yellow, 3f, (x, y, c) => (x + y) % 9 == 0 ? Darker(c, 1.15f) : c);
            // Échelles.
            foreach (int lx in new[] { 150, 210 })
            {
                cv.Piece(Union(Box(lx, 114, lx + 2, 146), Box(lx + 10, 114, lx + 12, 146)), Iron, 1f);
                for (int r = 0; r < 4; r++) cv.Piece(Box(lx, 120 + r * 7, lx + 12, 122 + r * 7), Iron, 1f);
            }

            // Bouées d'amarrage.
            foreach ((float bx, float by) in new[] { (8f, 146f), (280f, 150f) }) cv.Piece(Ellipse(bx, by, 5f, 4f), C(0.98f, 0.48f, 0.12f), 3f);
            Color[,] p = cv.Finish(Ink, 0.5f);
            // Ombre et écume autour de la coque (y depuis le bas).
            for (int x = 0; x < PontoonWidth; x++)
            {
                for (int y = 0; y < 30; y++)
                {
                    if (p[x, y].a > 0.01f) continue;
                    bool under = x > 12 && x < 280 && y > 10;
                    if (under) p[x, y] = WaterShadow;
                    else if (x > 8 && x < 284 && y > 6 && (x / 3) % 3 != 0) p[x, y] = Foam;
                }
            }

            return p;
        }

        // ---------- Barque en alu ----------

        public const int TinnieWidth = 112;
        public const int TinnieHeight = 56;
        public static readonly Vector2 TinniePivot = new Vector2(0.5f, 8f / TinnieHeight);

        /// <summary>Barque en aluminium (« tinnie ») tirée sur le sable : coque rivetée grise, bancs de bois, moteur hors-bord relevé, glacière.</summary>
        public static Color[,] Tinnie()
        {
            var cv = new VolumeCanvas(TinnieWidth, TinnieHeight);
            Color alu = C(0.74f, 0.78f, 0.80f);
            Func<float, float, bool> hull = (x, y) => y > 18 && y < 46 && x > 10 + (46 - y) * 0.2f && x < 92 + (y < 30 ? 6f : 0f) - (y - 18f) * 0.15f && (x > 22 || y > 18 + (22 - x) * 1.4f);
            cv.Piece(hull, alu, 4f, (x, y, c) => x % 12 == 0 && y % 3 == 0 ? Darker(c, 0.7f) : y == 30 ? Darker(c, 0.8f) : c);
            cv.Piece((x, y) => hull(x, y) && y < 30 && y > 21 && x > 20 && x < 88, C(0.40f, 0.44f, 0.46f), 2f);   // intérieur
            foreach (int bx in new[] { 38, 62 }) cv.Piece(Box(bx, 20, bx + 8, 30), Timber, 1.5f);
            cv.Piece(Box(68, 22, 84, 32), C(0.96f, 0.96f, 0.94f), 2f, (x, y, c) => y < 25 ? C(0.20f, 0.44f, 0.74f) : c);   // glacière
            // Moteur hors-bord relevé à l'arrière.
            cv.Piece(Union(Box(94, 8, 108, 24), Capsule(101f, 22f, 106f, 40f, 2.4f)), C(0.16f, 0.16f, 0.18f), 3f, (x, y, c) => y > 14 && y < 17 ? C(0.86f, 0.20f, 0.16f) : c);
            Color[,] p = cv.Finish(Ink, 0.5f);
            Shadow(p, 54f, 7f, 46f, 6f, C(0.30f, 0.28f, 0.22f, 0.3f));
            return p;
        }
    }
}
