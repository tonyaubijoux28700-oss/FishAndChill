using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Salle Japon de l'aquarium (pixel art provisoire), même découpage que la salle du lac (<see cref="AquariumLifeArt.LakeRoom"/>) :
    /// passage voûté de 2 cases puis salle de 24 cases. Sol en tatamis bordés de noir, mur de shōji (papier clair dans une
    /// grille de bois sombre), branches de cerisier peintes, bassin d'eau de mer claire aux rochers sombres et algues.
    /// </summary>
    public static class AquariumJapanArt
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

        private static readonly Color Paper = C(0.94f, 0.90f, 0.80f);
        private static readonly Color Lattice = C(0.26f, 0.18f, 0.12f);
        private static readonly Color Wood = C(0.22f, 0.14f, 0.10f);
        private static readonly Color WoodLight = C(0.40f, 0.26f, 0.16f);
        private static readonly Color Lacquer = C(0.70f, 0.16f, 0.12f);
        private static readonly Color DeepSea = C(0.06f, 0.20f, 0.36f);
        private static readonly Color TopSea = C(0.40f, 0.70f, 0.82f);

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

        /// <summary>Passage voûté puis salle Japon, 416×288.</summary>
        public static Color[,] JapanRoom()
        {
            var p = new Color[Width, Height];

            // Jardin zen : gravier ratissé, pas japonais, engawa de bois devant le bassin.
            AquariumFloorArt.Japan(p);

            // Mur du fond en shōji : papier clair dans une grille de bois sombre.
            for (int x = Room; x < Width; x++)
            {
                for (int y = StandBottom; y < Height; y++)
                {
                    bool frame = (x - Room) % 48 < 3 || (y - StandBottom) % 20 < 2;
                    bool grid = (x - Room) % 12 == 0 || (y - StandBottom) % 10 == 0;
                    p[x, y] = frame ? Lattice : grid ? DetailedFish.Shade(Lattice, 1.3f) : DetailedFish.Shade(Paper, 0.96f + Noise(x, y, 3) * 0.06f);
                }
            }

            // Branches de cerisier peintes sur le papier, en haut à gauche et à droite.
            foreach (Vector3 branch in new[] { new Vector3(Room + 10, Height - 30, 1f), new Vector3(Width - 14, Height - 26, -1f) })
            {
                var random = new System.Random(Mathf.RoundToInt(branch.x));
                for (int k = 0; k < 60; k++)
                {
                    int x = Mathf.RoundToInt(branch.x + branch.z * k);
                    int y = Mathf.RoundToInt(branch.y - k * 0.2f + Mathf.Sin(k * 0.2f) * 3f);
                    PixelArt.Set(p, x, y, C(0.20f, 0.12f, 0.10f));
                    if (k % 6 == 3)
                    {
                        int bx = x + random.Next(-2, 3);
                        int by = y + random.Next(-4, 5);
                        foreach (Vector2Int d in new[] { Vector2Int.zero, Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                        {
                            PixelArt.Set(p, bx + d.x, by + d.y, d == Vector2Int.zero ? C(0.98f, 0.92f, 0.94f) : C(0.96f, 0.68f, 0.78f));
                        }
                    }
                }
            }

            // Meuble du bassin en bois laqué et plaques rouges.
            PixelArt.Rect(p, TankLeft - 4, StandBottom, TankRight + 4, TankBottom - 1, Wood);
            PixelArt.Rect(p, TankLeft - 4, TankBottom - 2, TankRight + 4, TankBottom - 1, WoodLight);
            foreach (int px in new[] { TankLeft + 60, (TankLeft + TankRight) / 2, TankRight - 60 })
            {
                PixelArt.Rect(p, px - 14, StandBottom + 4, px + 14, StandBottom + 9, C(0.40f, 0.08f, 0.06f));
                PixelArt.Rect(p, px - 13, StandBottom + 5, px + 13, StandBottom + 8, Lacquer);
            }

            DrawTank(p);

            // Murs latéraux en bois sombre ; ouverture du passage à gauche.
            PixelArt.Rect(p, Room, 0, Room + 9, Height - 1, Wood);
            PixelArt.Rect(p, Width - 10, 0, Width - 1, Height - 1, Wood);
            PixelArt.Rect(p, Width - 10, 0, Width - 10, StandBottom, WoodLight);

            // Passage : murs, sol en pierre, linteau laqué rouge (comme un petit torii).
            PixelArt.Rect(p, 0, 0, Room + 9, PassageBottom - 1, C(0.10f, 0.10f, 0.10f));
            PixelArt.Rect(p, 0, PassageTop + 6, Room + 9, Height - 1, C(0.10f, 0.10f, 0.10f));
            for (int x = 0; x <= Room + 9; x++)
            {
                for (int y = PassageBottom; y <= PassageTop; y++)
                {
                    p[x, y] = (x / 8 + y / 8) % 2 == 0 ? C(0.70f, 0.70f, 0.68f) : C(0.64f, 0.64f, 0.62f);
                }
            }

            PixelArt.Rect(p, 0, PassageTop + 1, Room + 9, PassageTop + 4, Lacquer);
            PixelArt.Rect(p, 0, PassageTop + 5, Room + 9, PassageTop + 5, C(0.12f, 0.10f, 0.10f));
            PixelArt.Rect(p, Room + 10, PassageBottom - 1, Room + 10, PassageTop + 4, Lacquer);
            return p;
        }

        /// <summary>Bassin d'eau de mer : bleu profond, sable gris, rochers volcaniques sombres, algues brunes.</summary>
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
                        c = C(0.12f, 0.14f, 0.16f);
                    }
                    else
                    {
                        float t = Mathf.InverseLerp(bottom, WaterLine, y);
                        c = Color.Lerp(DeepSea, TopSea, Mathf.Round(t * 8f) / 8f);
                        if (y == WaterLine) c = Color.Lerp(TopSea, Color.white, x % 8 < 4 ? 0.5f : 0.25f);
                    }

                    p[x, y] = c;
                }
            }

            // Sable gris volcanique.
            for (int x = left; x <= right; x++)
            {
                int top = bottom + 5 + Mathf.RoundToInt(Mathf.Sin(x * 0.05f) * 2f);
                for (int y = bottom; y <= top; y++)
                {
                    p[x, y] = Noise(x, y, 8) > 0.5f ? C(0.56f, 0.56f, 0.54f) : C(0.48f, 0.48f, 0.46f);
                }
            }

            // Rochers sombres empilés et algues brunes qui montent.
            int floor = bottom + 5;
            foreach (Vector3 rock in new[] { new Vector3(100, floor, 22), new Vector3(130, floor, 14), new Vector3(290, floor, 26), new Vector3(330, floor + 10, 14), new Vector3(220, floor, 10) })
            {
                int cx = Mathf.RoundToInt(rock.x);
                int r = Mathf.RoundToInt(rock.z);
                for (int x = cx - r; x <= cx + r; x++)
                {
                    for (int y = Mathf.RoundToInt(rock.y); y <= rock.y + r * 0.8f; y++)
                    {
                        float u = (x - cx) / (float)r;
                        float v = (y - rock.y) / (r * 0.8f);
                        if (u * u + v * v > 1f) continue;
                        float light = 0.75f + 0.45f * Mathf.Max(0f, -u * 0.5f + v * 0.8f);
                        Color c = Noise(x, y, 21) > 0.9f ? C(0.30f, 0.40f, 0.26f) : C(0.24f, 0.24f, 0.26f);
                        PixelArt.Set(p, x, y, DetailedFish.Shade(c, Mathf.Round(light * 5f) / 5f));
                    }
                }
            }

            foreach (int wx in new[] { 74, 160, 186, 250, 360, 372 })
            {
                for (int k = 0; k < 34 + (wx * 5) % 24; k++)
                {
                    int sway = Mathf.RoundToInt(Mathf.Sin(k * 0.18f + wx) * 2f);
                    PixelArt.Set(p, wx + sway, floor + k, k % 5 == 0 ? C(0.36f, 0.28f, 0.12f) : C(0.50f, 0.40f, 0.16f));
                    PixelArt.Set(p, wx + sway + 1, floor + k, C(0.42f, 0.34f, 0.14f));
                    if (k % 6 == 3) PixelArt.Set(p, wx + sway + 2, floor + k, C(0.58f, 0.48f, 0.20f));
                }
            }
        }

        /// <summary>Vitre du bassin Japon : cadre de bois sombre, reflets obliques, montants. Même taille que la salle.</summary>
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
                    p[x, y] = C(0.22f, 0.14f, 0.10f, 0.9f);
                    p[x + 1, y] = C(0.40f, 0.26f, 0.16f, 0.9f);
                }
            }

            return p;
        }
    }
}
