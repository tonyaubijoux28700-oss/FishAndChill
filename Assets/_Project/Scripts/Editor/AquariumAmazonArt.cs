using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Salle Amazonie de l'aquarium (pixel art provisoire), même découpage que la salle du lac (<see cref="AquariumLifeArt.LakeRoom"/>) :
    /// passage voûté de 2 cases puis salle de 24 cases. Sol en tomettes, tapis à motifs en zigzag, mur vert sombre
    /// couvert de grandes feuilles, bassin d'« eau noire » couleur thé au fond de sable et de feuilles mortes, racines.
    /// </summary>
    public static class AquariumAmazonArt
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

        private static readonly Color Wall = C(0.16f, 0.30f, 0.20f);
        private static readonly Color WallDark = C(0.10f, 0.20f, 0.13f);
        private static readonly Color Leaf = C(0.24f, 0.44f, 0.26f);
        private static readonly Color Wood = C(0.26f, 0.16f, 0.10f);
        private static readonly Color WoodLight = C(0.44f, 0.30f, 0.18f);
        private static readonly Color Bronze = C(0.80f, 0.56f, 0.28f);
        private static readonly Color BronzeDark = C(0.54f, 0.34f, 0.14f);
        private static readonly Color DeepTea = C(0.16f, 0.10f, 0.05f);
        private static readonly Color TopTea = C(0.62f, 0.44f, 0.20f);

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static float Noise(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 73856093 ^ y * 19349663 ^ seed * 83492791;
                h = (h << 13) ^ h;
                return ((h * (h * h * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647f;
            }
        }

        /// <summary>Passage voûté puis salle Amazonie, 416×288.</summary>
        public static Color[,] AmazonRoom()
        {
            var p = new Color[Width, Height];

            // Tomettes ocre en losanges, lueur dorée du bassin vers le fond.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < StandBottom; y++)
                {
                    bool joint = (x + y) % 16 == 0 || (x - y + 1600) % 16 == 0;
                    Color c = ((x + y) / 16 + (x - y + 1600) / 16) % 2 == 0 ? C(0.66f, 0.38f, 0.24f) : C(0.60f, 0.34f, 0.22f);
                    if (joint) c = C(0.40f, 0.22f, 0.14f);
                    else if (Noise(x, y, 6) > 0.93f) c = DetailedFish.Shade(c, 0.9f);
                    float glow = Mathf.InverseLerp(100f, StandBottom, y);
                    c = Color.Lerp(c, C(0.86f, 0.62f, 0.30f), Mathf.Round(glow * 4f) / 4f * 0.25f);
                    p[x, y] = c;
                }
            }

            DrawRug(p, 128, 36, 320, 120);

            // Mur du fond vert sombre couvert de grandes feuilles découpées.
            for (int x = Room; x < Width; x++)
            {
                for (int y = StandBottom; y < Height; y++)
                {
                    p[x, y] = DetailedFish.Shade(Wall, 0.95f + Noise(x / 6, y / 6, 2) * 0.1f);
                }
            }

            var random = new System.Random(77);
            for (int i = 0; i < 26; i++)
            {
                DrawLeaf(p, random.Next(Room + 6, Width - 10), random.Next(StandBottom + 4, Height - 4), 6f + (float)random.NextDouble() * 6f,
                    (float)random.NextDouble() * Mathf.PI * 2f, i % 3 == 0 ? DetailedFish.Shade(Leaf, 1.15f) : Leaf);
            }

            // Meuble du bassin et plaques en bronze.
            PixelArt.Rect(p, TankLeft - 4, StandBottom, TankRight + 4, TankBottom - 1, Wood);
            PixelArt.Rect(p, TankLeft - 4, TankBottom - 2, TankRight + 4, TankBottom - 1, WoodLight);
            foreach (int px in new[] { TankLeft + 60, (TankLeft + TankRight) / 2, TankRight - 60 })
            {
                PixelArt.Rect(p, px - 14, StandBottom + 4, px + 14, StandBottom + 9, BronzeDark);
                PixelArt.Rect(p, px - 13, StandBottom + 5, px + 13, StandBottom + 8, Bronze);
            }

            DrawTank(p);

            // Murs latéraux ; ouverture du passage à gauche.
            PixelArt.Rect(p, Room, 0, Room + 9, Height - 1, WallDark);
            PixelArt.Rect(p, Width - 10, 0, Width - 1, Height - 1, WallDark);
            PixelArt.Rect(p, Width - 10, 0, Width - 10, StandBottom, Wall);

            // Passage : murs au-dessus et au-dessous, sol en pierre, arche en bronze.
            PixelArt.Rect(p, 0, 0, Room + 9, PassageBottom - 1, C(0.10f, 0.12f, 0.10f));
            PixelArt.Rect(p, 0, PassageTop + 6, Room + 9, Height - 1, C(0.10f, 0.12f, 0.10f));
            for (int x = 0; x <= Room + 9; x++)
            {
                for (int y = PassageBottom; y <= PassageTop; y++)
                {
                    p[x, y] = (x / 8 + y / 8) % 2 == 0 ? C(0.74f, 0.70f, 0.62f) : C(0.68f, 0.64f, 0.56f);
                }
            }

            PixelArt.Rect(p, 0, PassageTop + 1, Room + 9, PassageTop + 4, Bronze);
            PixelArt.Rect(p, 0, PassageTop + 5, Room + 9, PassageTop + 5, BronzeDark);
            PixelArt.Rect(p, Room + 10, PassageBottom - 1, Room + 10, PassageTop + 4, Bronze);
            return p;
        }

        /// <summary>Grande feuille en ovale pointu, nervure centrale, peinte sur le mur.</summary>
        private static void DrawLeaf(Color[,] p, int cx, int cy, float length, float angle, Color color)
        {
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            int reach = Mathf.CeilToInt(length) + 1;
            for (int x = cx - reach; x <= cx + reach; x++)
            {
                for (int y = cy - reach; y <= cy + reach; y++)
                {
                    if (x < Room + 10 || x >= Width - 10 || y < StandBottom || y >= Height) continue;
                    float u = ((x - cx) * cos + (y - cy) * sin) / length;
                    float v = (-(x - cx) * sin + (y - cy) * cos) / (length * 0.42f);
                    float width = 1f - u * u;
                    if (Mathf.Abs(u) > 1f || Mathf.Abs(v) > width) continue;
                    p[x, y] = Mathf.Abs(v) < 0.12f ? DetailedFish.Shade(color, 0.8f) : color;
                }
            }
        }

        /// <summary>Tapis tissé à motifs en zigzag rouge, ocre et noir.</summary>
        private static void DrawRug(Color[,] p, int x0, int y0, int x1, int y1)
        {
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    int edge = Mathf.Min(Mathf.Min(x - x0, x1 - x), Mathf.Min(y - y0, y1 - y));
                    Color c = edge < 3 ? C(0.20f, 0.12f, 0.08f) : C(0.82f, 0.62f, 0.34f);
                    int band = (y - y0) / 14;
                    int zig = Mathf.Abs(((x - x0) % 16) - 8);
                    if (edge >= 3 && Mathf.Abs((y - y0) % 14 - 3 - zig / 2) < 1) c = band % 2 == 0 ? C(0.70f, 0.18f, 0.12f) : C(0.16f, 0.12f, 0.10f);
                    p[x, y] = c;
                }
            }
        }

        /// <summary>Bassin d'eau noire : thé sombre au fond, sable clair, feuilles mortes, racines plongeantes.</summary>
        private static void DrawTank(Color[,] p)
        {
            int left = TankLeft + Frame;
            int right = TankRight - Frame;
            int bottom = TankBottom + Frame;
            PixelArt.Rect(p, TankLeft, TankBottom, TankRight, TankTop, Wood);
            PixelArt.Rect(p, TankLeft, TankTop - 1, TankRight, TankTop, WoodLight);

            for (int x = left; x <= right; x++)
            {
                for (int y = bottom; y <= TankTop - Frame; y++)
                {
                    Color c;
                    if (y > WaterLine)
                    {
                        c = C(0.16f, 0.14f, 0.10f);
                    }
                    else
                    {
                        float t = Mathf.InverseLerp(bottom, WaterLine, y);
                        c = Color.Lerp(DeepTea, TopTea, Mathf.Round(t * 8f) / 8f);
                        if (y == WaterLine) c = Color.Lerp(TopTea, Color.white, x % 8 < 4 ? 0.45f : 0.2f);
                    }

                    p[x, y] = c;
                }
            }

            // Fond de sable clair semé de feuilles mortes.
            for (int x = left; x <= right; x++)
            {
                int top = bottom + 5 + Mathf.RoundToInt(Mathf.Sin(x * 0.04f) * 2f);
                for (int y = bottom; y <= top; y++)
                {
                    Color c = Noise(x, y, 8) > 0.5f ? C(0.80f, 0.70f, 0.52f) : C(0.74f, 0.64f, 0.46f);
                    if (Noise(x / 3, y / 2, 12) > 0.72f) c = Noise(x, 0, 13) > 0.5f ? C(0.56f, 0.30f, 0.14f) : C(0.42f, 0.24f, 0.12f);
                    p[x, y] = c;
                }
            }

            // Racines et branches immergées, qui plongent depuis la surface.
            int floor = bottom + 5;
            Color root = C(0.30f, 0.20f, 0.12f);
            Color rootLight = C(0.46f, 0.32f, 0.20f);
            foreach (Vector4 r in new[] { new Vector4(90, WaterLine, 150, floor), new Vector4(120, WaterLine, 130, floor), new Vector4(300, WaterLine, 250, floor), new Vector4(340, WaterLine - 10, 372, floor) })
            {
                for (int k = 0; k <= 80; k++)
                {
                    float t = k / 80f;
                    int x = Mathf.RoundToInt(Mathf.Lerp(r.x, r.z, t) + Mathf.Sin(t * 5f) * 4f);
                    int y = Mathf.RoundToInt(Mathf.Lerp(r.y, r.w, t));
                    for (int w = -2; w <= 2; w++)
                    {
                        PixelArt.Set(p, x + w, y, w == -2 ? rootLight : root);
                    }
                }
            }

            // Plantes épées d'Amazonie, fixes (d'autres ondulent en jeu).
            foreach (int wx in new[] { 70, 200, 230, 280, 360 })
            {
                for (int blade = -2; blade <= 2; blade++)
                {
                    for (int k = 0; k < 28; k++)
                    {
                        int x = wx + Mathf.RoundToInt(blade * k * 0.35f);
                        PixelArt.Set(p, x, floor + k, k % 5 == 0 ? C(0.20f, 0.40f, 0.16f) : C(0.30f, 0.54f, 0.22f));
                        PixelArt.Set(p, x + 1, floor + k, C(0.24f, 0.46f, 0.18f));
                    }
                }
            }
        }

        /// <summary>Vitre du bassin Amazonie : cadre de bois sombre, reflets obliques, montants. Même taille que la salle.</summary>
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
                    if (!inside) p[x, y] = y >= TankTop - 1 || x < left ? WoodLight : Wood;
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
                    p[x, y] = C(0.26f, 0.16f, 0.10f, 0.9f);
                    p[x + 1, y] = C(0.44f, 0.30f, 0.18f, 0.9f);
                }
            }

            return p;
        }

        /// <summary>
        /// Ouvre le mur de droite de la salle du lac (même découpage) sur un passage vers la salle Amazonie :
        /// sol de pierre et arche en laiton dans l'épaisseur du mur.
        /// </summary>
        public static void OpenRightPassage(Color[,] p)
        {
            int w = p.GetLength(0);
            for (int x = w - 10; x < w; x++)
            {
                for (int y = PassageBottom; y <= PassageTop; y++)
                {
                    p[x, y] = (x / 8 + y / 8) % 2 == 0 ? C(0.74f, 0.72f, 0.66f) : C(0.68f, 0.66f, 0.60f);
                }

                for (int y = PassageTop + 1; y <= PassageTop + 4; y++) p[x, y] = C(0.86f, 0.68f, 0.30f);
                p[x, PassageTop + 5] = C(0.62f, 0.45f, 0.16f);
            }

            for (int y = PassageBottom - 1; y <= PassageTop + 4; y++) p[w - 11, y] = C(0.86f, 0.68f, 0.30f);
        }
    }
}
