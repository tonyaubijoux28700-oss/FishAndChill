using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les présentoirs du marchand, dans le style en volume (<see cref="VolumeCanvas"/>) : le fond de la vitrine (mur
    /// tapissé, lambris, plancher et un rond de lumière qui tombe d'en haut), le socle à coussin de velours (hameçons,
    /// appâts, sacs), le râtelier des cannes et la flèche qui fait passer à l'objet suivant.
    /// </summary>
    public static class ShopDisplayArt
    {
        public const int BackWidth = 240;
        public const int BackHeight = 135;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Wood = C(0.56f, 0.36f, 0.20f);
        private static readonly Color DarkWood = C(0.34f, 0.21f, 0.12f);
        private static readonly Color Brass = C(0.93f, 0.74f, 0.32f);
        private static readonly Color Velvet = C(0.62f, 0.14f, 0.20f);
        private static readonly Color Ink = C(0.12f, 0.07f, 0.05f);

        /// <summary>Le fond de la vitrine (affiché ×4) : papier peint à rayures, lambris, plancher, rond de lumière.</summary>
        public static Color[,] Back()
        {
            var p = new Color[BackWidth, BackHeight];
            const int floorTop = 30, wainscotTop = 58;
            for (int x = 0; x < BackWidth; x++)
            {
                for (int y = 0; y < BackHeight; y++)
                {
                    Color c;
                    if (y < floorTop)
                    {
                        // Plancher en perspective : lames qui s'écartent vers le bas.
                        float u = (x - BackWidth * 0.5f) / (1f + (floorTop - y) * 0.06f);
                        c = Mathf.Repeat(u, 22f) < 1.2f ? C(0.26f, 0.16f, 0.09f) : (int)(u / 22f + 100) % 2 == 0 ? C(0.50f, 0.33f, 0.19f) : C(0.46f, 0.30f, 0.17f);
                        if (y == floorTop - 1) c = C(0.30f, 0.19f, 0.11f);
                    }
                    else if (y < wainscotTop)
                    {
                        // Lambris : panneaux moulurés, cimaise claire en haut.
                        int panel = x % 40;
                        c = C(0.42f, 0.27f, 0.15f);
                        if (panel < 2 || y < floorTop + 3) c = C(0.30f, 0.19f, 0.11f);
                        else if ((panel == 5 || panel == 36) && y > floorTop + 6 && y < wainscotTop - 6) c = C(0.52f, 0.35f, 0.20f);
                        else if ((y == floorTop + 6 || y == wainscotTop - 6) && panel >= 5 && panel <= 36) c = C(0.52f, 0.35f, 0.20f);
                        if (y >= wainscotTop - 3) c = y == wainscotTop - 1 ? C(0.74f, 0.54f, 0.30f) : C(0.60f, 0.42f, 0.24f);
                    }
                    else
                    {
                        // Papier peint vert sapin à fines rayures dorées et petits motifs.
                        c = x % 12 == 0 ? C(0.30f, 0.38f, 0.26f) : C(0.18f, 0.27f, 0.20f);
                        if (x % 12 == 6 && y % 12 == 6) c = C(0.40f, 0.42f, 0.24f);
                    }

                    // Rond de lumière : cône depuis le haut, plus fort au centre, par paliers.
                    float dx = (x - BackWidth * 0.5f) / (26f + (BackHeight - y) * 0.42f);
                    float cone = Mathf.Clamp01(1f - Mathf.Abs(dx));
                    float pool = y < floorTop + 8 ? Mathf.Clamp01(1f - new Vector2((x - BackWidth * 0.5f) / 70f, (y - 18f) / 14f).magnitude) : 0f;
                    float edge = Mathf.Clamp01(1f - Mathf.Abs(x - BackWidth * 0.5f) / (BackWidth * 0.5f));
                    float light = 0.62f + 0.18f * edge + 0.32f * cone * cone + 0.35f * pool;
                    p[x, y] = Darker(c, Mathf.Round(light * 8f) / 8f);
                }
            }

            return p;
        }

        /// <summary>Le socle (72×60) : colonne de bois cerclée de laiton, plateau, coussin de velours à glands dorés.</summary>
        public static Color[,] Pedestal()
        {
            var cv = new VolumeCanvas(72, 60);
            cv.Piece(Ellipse(36, 54, 30, 5), DarkWood, 3f);                               // pied
            cv.Piece(Box(14, 20, 58, 54), Wood, 6f, (x, y, c) => x % 8 == 0 ? Darker(c, 0.82f) : c);   // colonne cannelée
            cv.Piece(Box(12, 49, 60, 53), Brass, 2f);                                      // cerclage du bas
            cv.Piece(Box(12, 20, 60, 24), Brass, 2f);                                      // cerclage du haut
            cv.Piece(Ellipse(36, 18, 32, 7), Darker(Wood, 1.1f), 3f);                      // plateau
            cv.Piece(Ellipse(36, 15, 26, 6), Velvet, 5f, (x, y, c) => (x + y) % 7 == 0 ? Darker(c, 0.9f) : c);   // coussin
            foreach (float tx in new[] { 11f, 61f })
            {
                cv.Piece(Capsule(tx, 16, tx, 24, 1.3f), Brass, 1f);                        // glands dorés
                cv.Piece(Ellipse(tx, 25, 2.2f, 2.2f), Darker(Brass, 0.9f), 1.5f);
            }

            return cv.Finish(Ink);
        }

        /// <summary>Le râtelier des cannes (72×40) : socle de bois et bague de laiton où la canne se tient debout.</summary>
        public static Color[,] RodStand()
        {
            var cv = new VolumeCanvas(72, 40);
            cv.Piece(Ellipse(36, 34, 32, 5), DarkWood, 3f);
            cv.Piece(Box(8, 14, 64, 34), Wood, 4f, (x, y, c) => y % 6 == 0 ? Darker(c, 0.85f) : c);
            cv.Piece(Box(6, 10, 66, 15), Darker(Wood, 1.12f), 2.5f);
            cv.Piece(Box(26, 20, 46, 28), Brass, 1.5f);                                    // plaque gravée
            cv.Piece(Box(28, 22, 44, 26), Darker(Brass, 0.8f), 0.8f);
            cv.Piece(Ellipse(36, 10, 9, 3.5f), Brass, 2.5f);                               // bague
            cv.Piece(Ellipse(36, 10, 5, 1.8f), C(0.10f, 0.06f, 0.04f), 1f, null, 0f);
            return cv.Finish(Ink);
        }

        /// <summary>La flèche (22×30) qui fait passer à l'objet suivant : médaillon de bois à chevron de laiton (pointe à droite).</summary>
        public static Color[,] Arrow()
        {
            var cv = new VolumeCanvas(22, 30);
            cv.Piece(Ellipse(11, 15, 10, 13.5f), DarkWood, 4f);
            cv.Piece((x, y) => Segment(x, y, 8f, 8f, 14f, 15f) < 2.3f || Segment(x, y, 14f, 15f, 8f, 22f) < 2.3f, Brass, 2.5f);
            return cv.Finish(Ink);
        }

        private static float Segment(float x, float y, float x0, float y0, float x1, float y1)
        {
            var a = new Vector2(x0, y0);
            Vector2 ab = new Vector2(x1, y1) - a;
            float t = Mathf.Clamp01(Vector2.Dot(new Vector2(x, y) - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(new Vector2(x, y), a + ab * t);
        }
    }
}
