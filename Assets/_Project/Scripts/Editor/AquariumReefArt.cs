using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Salle Grande Barrière de l'aquarium (pixel art provisoire), même découpage que les autres (passage de 2 cases puis
    /// salle de 24 cases) : cabane de plage aux planches blanchies, fresque marine peinte (horizon, vagues, coraux et
    /// poissons), bassin turquoise plein de coraux colorés sur du sable blanc. Sol : <see cref="AquariumFloorArt.Reef"/>.
    /// </summary>
    public static class AquariumReefArt
    {
        public const int Width = 416;
        public const int Height = 288;
        private const int Room = 32;
        private const int TankLeft = Room + 20;
        private const int TankRight = Width - 21;
        private const int TankBottom = 180;
        private const int TankTop = 282;
        private const int Frame = 5;
        private const int WaterLine = 272;
        private const int StandBottom = 166;
        private const int PassageBottom = 51;
        private const int PassageTop = 96;

        private static readonly Color Driftwood = C(0.80f, 0.76f, 0.68f);
        private static readonly Color DriftwoodDark = C(0.58f, 0.54f, 0.46f);
        private static readonly Color Rope = C(0.82f, 0.70f, 0.48f);
        private static readonly Color DeepSea = C(0.06f, 0.36f, 0.58f);
        private static readonly Color TopSea = C(0.40f, 0.86f, 0.90f);

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
        private static Color S(Color c, float light) => DetailedFish.Shade(c, light);

        private static float Noise(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 73856093 ^ y * 19349663 ^ seed * 83492791;
                h = (h << 13) ^ h;
                return ((h * (h * h * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647f;
            }
        }

        /// <summary>Passage puis salle Grande Barrière, 416×288.</summary>
        public static Color[,] ReefRoom()
        {
            var p = new Color[Width, Height];

            // Sable blanc, coquillages, et un caillebotis de bois flotté jusqu'au bassin.
            AquariumFloorArt.Reef(p);

            // Fresque du mur du fond : ciel clair, horizon, mer qui fonce vers le bas, vagues blanches.
            for (int x = Room; x < Width; x++)
            {
                for (int y = StandBottom; y < Height; y++)
                {
                    float t = Mathf.InverseLerp(StandBottom, Height, y);
                    Color c = t > 0.8f ? C(0.86f, 0.94f, 0.98f) : Color.Lerp(C(0.10f, 0.42f, 0.66f), C(0.30f, 0.74f, 0.86f), Mathf.Round(t / 0.8f * 5f) / 5f);
                    if (t <= 0.8f && Mathf.Sin(x * 0.2f + y * 0.9f) > 0.93f) c = Color.Lerp(c, Color.white, 0.5f);
                    if (Mathf.Abs(t - 0.8f) < 0.01f) c = C(0.96f, 0.98f, 1f);
                    p[x, y] = c;
                }
            }

            // Silhouettes peintes : coraux en bas du mur, petits bancs de poissons.
            for (int x = Room + 12; x < Width - 12; x++)
            {
                float h = 6f + Mathf.Abs(Mathf.Sin(x * 0.13f) * 8f) + Mathf.Abs(Mathf.Sin(x * 0.31f) * 4f);
                for (int y = StandBottom; y < StandBottom + h; y++)
                {
                    p[x, y] = Color.Lerp(p[x, y], C(0.92f, 0.46f, 0.52f), 0.55f);
                }
            }

            var random = new System.Random(55);
            for (int i = 0; i < 14; i++)
            {
                int fx = random.Next(Room + 20, Width - 24);
                int fy = random.Next(StandBottom + 30, StandBottom + 80);
                Color fish = i % 2 == 0 ? C(0.98f, 0.62f, 0.20f) : C(0.98f, 0.92f, 0.40f);
                PixelArt.Rect(p, fx, fy, fx + 3, fy + 1, fish);
                PixelArt.Set(p, fx - 1, fy + 2, fish);
                PixelArt.Set(p, fx - 1, fy - 1, fish);
            }

            // Meuble du bassin en bois flotté, cordage.
            PixelArt.Rect(p, TankLeft - 4, StandBottom, TankRight + 4, TankBottom - 1, Driftwood);
            for (int x = TankLeft - 4; x <= TankRight + 4; x += 6)
            {
                PixelArt.Rect(p, x, StandBottom, x, TankBottom - 1, DriftwoodDark);
            }

            PixelArt.Rect(p, TankLeft - 4, StandBottom + 6, TankRight + 4, StandBottom + 7, Rope);

            DrawTank(p);

            // Murs latéraux en planches blanchies ; ouverture du passage à gauche.
            PixelArt.Rect(p, Room, 0, Room + 9, Height - 1, DriftwoodDark);
            PixelArt.Rect(p, Width - 10, 0, Width - 1, Height - 1, DriftwoodDark);
            PixelArt.Rect(p, Width - 10, 0, Width - 10, StandBottom, Driftwood);

            // Passage : murs, sol de planches, linteau de bois flotté entouré de corde.
            PixelArt.Rect(p, 0, 0, Room + 9, PassageBottom - 1, C(0.12f, 0.14f, 0.16f));
            PixelArt.Rect(p, 0, PassageTop + 6, Room + 9, Height - 1, C(0.12f, 0.14f, 0.16f));
            for (int x = 0; x <= Room + 9; x++)
            {
                for (int y = PassageBottom; y <= PassageTop; y++)
                {
                    p[x, y] = y % 6 == 0 ? DriftwoodDark : S(Driftwood, 0.95f + Noise(x / 8, y / 6, 2) * 0.1f);
                }
            }

            PixelArt.Rect(p, 0, PassageTop + 1, Room + 9, PassageTop + 4, Driftwood);
            for (int x = 0; x <= Room + 9; x += 3) PixelArt.Set(p, x, PassageTop + 2, Rope);
            PixelArt.Rect(p, 0, PassageTop + 5, Room + 9, PassageTop + 5, DriftwoodDark);
            PixelArt.Rect(p, Room + 10, PassageBottom - 1, Room + 10, PassageTop + 4, Driftwood);
            return p;
        }

        /// <summary>Bassin du récif : turquoise lumineux, sable blanc, jardin de coraux de toutes les couleurs.</summary>
        private static void DrawTank(Color[,] p)
        {
            int left = TankLeft + Frame;
            int right = TankRight - Frame;
            int bottom = TankBottom + Frame;
            PixelArt.Rect(p, TankLeft, TankBottom, TankRight, TankTop, DriftwoodDark);
            PixelArt.Rect(p, TankLeft, TankTop - 1, TankRight, TankTop, Driftwood);

            for (int x = left; x <= right; x++)
            {
                for (int y = bottom; y <= TankTop - Frame; y++)
                {
                    Color c;
                    if (y > WaterLine)
                    {
                        c = C(0.14f, 0.18f, 0.20f);
                    }
                    else
                    {
                        float t = Mathf.InverseLerp(bottom, WaterLine, y);
                        c = Color.Lerp(DeepSea, TopSea, Mathf.Round(t * 8f) / 8f);
                        if (y == WaterLine) c = Color.Lerp(TopSea, Color.white, x % 8 < 4 ? 0.55f : 0.3f);
                    }

                    p[x, y] = c;
                }
            }

            for (int x = left; x <= right; x++)
            {
                int top = bottom + 5 + Mathf.RoundToInt(Mathf.Sin(x * 0.06f) * 2f);
                for (int y = bottom; y <= top; y++)
                {
                    p[x, y] = Noise(x, y, 8) > 0.5f ? C(0.94f, 0.92f, 0.84f) : C(0.86f, 0.84f, 0.76f);
                }
            }

            // Jardin de coraux posés sur le sable (les dessins du récif, en couleurs vives).
            int floor = bottom + 5;
            var random = new System.Random(808);
            for (int i = 0; i < 22; i++)
            {
                Color[,] coral = ReefArt.Coral(i % 4);
                int cx = left + 6 + random.Next(0, right - left - 22);
                int cy = floor - 2 + random.Next(0, 10);
                for (int x = 0; x < 16; x++)
                {
                    for (int y = 0; y < 16; y++)
                    {
                        Color c = coral[x, y];
                        if (c.a < 0.01f) continue;
                        c.a = 1f;
                        PixelArt.Set(p, cx + x, cy + y, S(c, 1.15f));
                    }
                }
            }
        }

        /// <summary>Vitre du bassin du récif : cadre de bois flotté, reflets obliques, montants. Même taille que la salle.</summary>
        public static Color[,] TankGlass()
        {
            var p = new Color[Width, Height];
            int left = TankLeft + Frame;
            int right = TankRight - Frame;
            int bottom = TankBottom + Frame;
            for (int x = TankLeft; x <= TankRight; x++)
            {
                for (int y = TankBottom; y <= TankTop; y++)
                {
                    bool inside = x >= left && x <= right && y >= bottom && y <= TankTop - Frame;
                    if (!inside) p[x, y] = y >= TankTop - 1 || x < left ? Driftwood : DriftwoodDark;
                }
            }

            for (int x = left; x <= right; x++)
            {
                for (int y = bottom; y <= WaterLine; y++)
                {
                    int band = (x - y + 900) % 96;
                    if (band < 4 || band == 8) p[x, y] = C(1f, 1f, 1f, band == 8 ? 0.10f : 0.15f);
                }
            }

            foreach (int x in new[] { TankLeft + 115, TankRight - 115 })
            {
                for (int y = bottom; y <= TankTop - Frame; y++)
                {
                    p[x, y] = C(0.58f, 0.54f, 0.46f, 0.9f);
                    p[x + 1, y] = C(0.80f, 0.76f, 0.68f, 0.9f);
                }
            }

            return p;
        }
    }
}
