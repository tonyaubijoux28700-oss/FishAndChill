using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// L'ascenseur de l'aquarium, posé dans chaque salle (48×88 px, 3 cases de large, pivot au pied) : cage de laiton
    /// Art déco, vitres sur les côtés, porte en grille extensible (losanges) qui s'ouvre sur une cabine chaude, cadran
    /// des étages en demi-cercle avec son aiguille, bouton d'appel. Images : porte fermée, à moitié, ouverte.
    /// Tableaux [x, y] avec y depuis le bas.
    /// </summary>
    public static class ElevatorArt
    {
        public const int Width = 48;
        public const int Height = 88;
        public const int Frames = 3;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Outline = C(0.07f, 0.08f, 0.10f);
        private static readonly Color Brass = C(0.86f, 0.68f, 0.30f);
        private static readonly Color BrassLight = C(0.98f, 0.86f, 0.50f);
        private static readonly Color BrassDark = C(0.56f, 0.40f, 0.16f);
        private static readonly Color Glass = C(0.30f, 0.52f, 0.60f);
        private static readonly Color GlassLight = C(0.62f, 0.82f, 0.86f);
        private static readonly Color CabinWarm = C(0.96f, 0.80f, 0.50f);
        private static readonly Color CabinShade = C(0.66f, 0.46f, 0.26f);
        private static readonly Color Marble = C(0.20f, 0.24f, 0.30f);
        private static readonly Color Red = C(0.86f, 0.26f, 0.22f);

        /// <summary>Une image : 0 porte fermée, 1 à moitié ouverte, 2 ouverte.</summary>
        public static Color[,] Cabin(int frame)
        {
            var p = new Color[Width, Height];
            const int x0 = 3, x1 = 44, floorY = 2, doorTop = 60;

            // Socle de marbre sombre.
            PixelArt.Rect(p, x0 - 2, 0, x1 + 2, floorY + 2, Marble);
            PixelArt.Rect(p, x0 - 2, floorY + 2, x1 + 2, floorY + 2, BrassDark);

            // Cage : montants, vitres latérales.
            for (int x = x0; x <= x1; x++)
            {
                for (int y = floorY + 3; y <= doorTop; y++)
                {
                    bool post = x <= x0 + 2 || x >= x1 - 2;
                    bool side = x <= x0 + 8 || x >= x1 - 8;
                    Color c = post ? (x == x0 || x == x1 - 2 ? BrassDark : Brass)
                        : side ? ((x + y) % 19 < 2 ? GlassLight : Glass)
                        : CabinWarm;
                    if (side && !post && y % 14 == 0) c = BrassDark;   // traverses des vitres
                    p[x, y] = c;
                }
            }

            // Intérieur de la cabine (visible quand la grille s'ouvre) : lumière chaude, lambris, main courante.
            for (int x = x0 + 9; x <= x1 - 9; x++)
            {
                for (int y = floorY + 3; y <= doorTop - 1; y++)
                {
                    Color c = y < floorY + 8 ? CabinShade : y > doorTop - 10 ? Color.Lerp(CabinWarm, Color.white, 0.3f) : CabinWarm;
                    if (y == floorY + 24) c = BrassDark;                                   // main courante
                    if ((x - x0) % 8 == 0 && y < floorY + 24) c = Color.Lerp(c, CabinShade, 0.5f);   // lambris
                    p[x, y] = c;
                }
            }

            // Grille extensible : deux battants en losanges qui se replient vers les côtés.
            float open = frame / (float)(Frames - 1);
            int innerL = x0 + 9, innerR = x1 - 9;
            int half = (innerR - innerL) / 2;
            int fold = Mathf.RoundToInt(half * open * 0.85f);
            for (int side = 0; side < 2; side++)
            {
                int from = side == 0 ? innerL : innerR - (half - fold);
                int to = side == 0 ? innerL + (half - fold) : innerR;
                for (int x = from; x <= to; x++)
                {
                    for (int y = floorY + 3; y <= doorTop - 1; y++)
                    {
                        int u = x - from, v = y;
                        bool lattice = (u + v) % 6 == 0 || (u - v + 600) % 6 == 0;
                        if (lattice || x == from || x == to) p[x, y] = (x == from || x == to) ? BrassDark : Brass;
                    }
                }
            }

            // Fronton : linteau de laiton, cadran des étages et son aiguille, éventail Art déco.
            PixelArt.Rect(p, x0 - 1, doorTop + 1, x1 + 1, doorTop + 4, Brass);
            PixelArt.Rect(p, x0 - 1, doorTop + 1, x1 + 1, doorTop + 1, BrassDark);
            float dcx = Width * 0.5f, dcy = doorTop + 5f;
            for (int x = 0; x < Width; x++)
            {
                for (int y = doorTop + 5; y < Height; y++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - dcx) * (x + 0.5f - dcx) + (y + 0.5f - dcy) * (y + 0.5f - dcy));
                    if (d <= 19f)
                    {
                        float a = Mathf.Atan2(y + 0.5f - dcy, x + 0.5f - dcx);
                        bool ray = Mathf.Repeat(a * 9f / Mathf.PI, 1f) < 0.22f;
                        p[x, y] = d > 17.5f ? BrassDark : d > 11f ? (ray ? BrassLight : Brass) : d > 9.5f ? BrassDark : C(0.96f, 0.92f, 0.80f);
                        if (d <= 9.5f && Mathf.Repeat(a * 6f / Mathf.PI, 1f) < 0.08f && d > 6.5f) p[x, y] = Outline;   // graduations
                    }
                }
            }

            // Aiguille du cadran (penche selon l'image : la cabine arrive).
            float needle = Mathf.Lerp(150f, 90f, open) * Mathf.Deg2Rad;
            for (float t = 0f; t <= 8f; t += 0.5f)
            {
                int nx = Mathf.RoundToInt(dcx - 0.5f + Mathf.Cos(needle) * t), ny = Mathf.RoundToInt(dcy - 0.5f + Mathf.Sin(needle) * t);
                if (nx >= 0 && ny >= 0 && nx < Width && ny < Height) p[nx, ny] = Red;
            }

            // Bouton d'appel sur le montant de droite.
            PixelArt.Rect(p, x1 - 1, floorY + 26, x1 + 1, floorY + 30, BrassDark);
            p[x1, floorY + 28] = frame == 0 ? C(1f, 0.85f, 0.35f) : Red;

            PixelArt.SelectiveOutline(p, Outline, 0.6f);
            return p;
        }
    }
}
