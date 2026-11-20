using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// L'ascenseur de l'aquarium est dans le mur du bas de chaque salle, qu'on ne voit pas en vue de dessus : seul son
    /// tapis le signale. Tableaux [x, y] avec y depuis le bas.
    /// </summary>
    public static class ElevatorArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Outline = C(0.07f, 0.08f, 0.10f);
        private static readonly Color Brass = C(0.86f, 0.68f, 0.30f);
        private static readonly Color BrassLight = C(0.98f, 0.86f, 0.50f);
        private static readonly Color BrassDark = C(0.56f, 0.40f, 0.16f);

        public const int MatWidth = 48;
        public const int MatHeight = 18;

        private static readonly Color Velvet = C(0.46f, 0.10f, 0.14f);
        private static readonly Color VelvetLight = C(0.60f, 0.16f, 0.18f);
        private static readonly Color VelvetDark = C(0.30f, 0.06f, 0.10f);

        /// <summary>
        /// Le tapis de l'ascenseur, posé contre le mur du bas (48×18 px, 3 cases de large, pivot en bas au milieu) :
        /// l'ascenseur est dans le mur qu'on ne voit pas, le tapis indique sa porte, comme un paillasson. Velours bordeaux,
        /// frise Art déco en laiton, deux triangles « monter / descendre » au centre, seuil de laiton contre le mur.
        /// </summary>
        public static Color[,] Mat()
        {
            var p = new Color[MatWidth, MatHeight];
            const int x0 = 1, x1 = MatWidth - 2, y0 = 0, y1 = MatHeight - 2;

            // Ombre portée sous le bord haut, puis le tapis.
            PixelArt.Rect(p, x0 + 1, y1 + 1, x1 - 1, y1 + 1, C(0f, 0f, 0f, 0.25f));
            PixelArt.Rect(p, x0, y0, x1, y1, BrassDark);
            PixelArt.Rect(p, x0 + 1, y0 + 1, x1 - 1, y1 - 1, Brass);
            PixelArt.Rect(p, x0 + 1, y1 - 1, x1 - 1, y1 - 1, BrassLight);
            PixelArt.Rect(p, x0 + 2, y0 + 3, x1 - 2, y1 - 2, VelvetDark);
            PixelArt.Rect(p, x0 + 3, y0 + 4, x1 - 3, y1 - 3, Velvet);

            // Frise en dents de scie le long des bords (motif Art déco).
            for (int x = x0 + 4; x <= x1 - 4; x++)
            {
                if ((x / 2) % 2 == 0)
                {
                    p[x, y1 - 4] = BrassDark;
                    p[x, y0 + 5] = BrassDark;
                }
            }

            // Reflet du velours.
            for (int x = x0 + 4; x <= x1 - 4; x++)
            {
                for (int y = y0 + 6; y <= y1 - 5; y++)
                {
                    if ((x + y * 3) % 11 == 0) p[x, y] = VelvetLight;
                }
            }

            // Médaillon central : losange de laiton avec les deux triangles.
            int cx = MatWidth / 2, cy = (y0 + y1) / 2 + 1;
            for (int dx = -8; dx <= 8; dx++)
            {
                for (int dy = -6; dy <= 6; dy++)
                {
                    float d = Mathf.Abs(dx) / 8.5f + Mathf.Abs(dy) / 6.5f;
                    if (d <= 1f) p[cx + dx - 1, cy + dy] = d > 0.8f ? BrassDark : VelvetDark;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                // Triangle du haut (pointe vers le haut) et du bas (pointe vers le bas).
                for (int dx = -i; dx <= i; dx++)
                {
                    p[cx + dx - 1, cy + 3 - i] = i == 2 ? Brass : BrassLight;
                    p[cx + dx - 1, cy - 3 + i] = i == 2 ? Brass : BrassLight;
                }
            }

            // Seuil de laiton contre le mur du bas (la porte de l'ascenseur).
            PixelArt.Rect(p, x0, y0, x1, y0 + 1, BrassDark);
            PixelArt.Rect(p, x0 + 1, y0 + 2, x1 - 1, y0 + 2, BrassLight);
            for (int x = x0 + 2; x < x1; x += 4) p[x, y0] = Outline;
            return p;
        }
    }
}
