using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Sols des salles de l'aquarium (en bas de chaque image de salle, 416 px de large, jusqu'au meuble du bassin) :
    /// parquet en vannerie de chêne et tapis rond tressé (lac), ponton de planches usées, mousse et feuilles (Amazonie),
    /// jardin zen de gravier ratissé, pas japonais et engawa de bois devant le bassin (Japon).
    /// </summary>
    public static class AquariumFloorArt
    {
        public const int StandBottom = 166;
        private const int Room = 32;

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

        /// <summary>Lueur du bassin qui monte vers le fond de la salle.</summary>
        private static Color Glow(Color c, int y, Color tint, float strength)
        {
            float glow = Mathf.InverseLerp(100f, StandBottom, y);
            return Color.Lerp(c, tint, Mathf.Round(glow * 4f) / 4f * strength);
        }

        // ---------- Lac : parquet en vannerie et tapis tressé ----------

        public static void Lake(Color[,] p)
        {
            int w = p.GetLength(0);
            Color oak = C(0.72f, 0.53f, 0.33f);
            Color walnut = C(0.42f, 0.27f, 0.15f);
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < StandBottom; y++)
                {
                    Color c;
                    bool border = x < Room + 18 || x > w - 19 || y < 8;
                    if (border)
                    {
                        // Bordure en noyer, lames dans la longueur.
                        c = y % 6 == 0 || (y < 8 && x % 48 == 0) ? S(walnut, 0.7f) : S(walnut, 0.95f + Noise(x / 16, y / 6, 2) * 0.12f);
                    }
                    else
                    {
                        // Vannerie : carrés de 8 px, deux lames chacun, une fois en long, une fois en travers.
                        int bx = x / 8;
                        int by = y / 8;
                        bool upright = (bx + by) % 2 == 0;
                        int lx = x % 8;
                        int ly = y % 8;
                        int plank = upright ? lx / 4 : ly / 4;
                        bool seam = upright ? lx % 4 == 0 : ly % 4 == 0;
                        c = S(oak, (upright ? 0.94f : 1.04f) + (Noise(bx, by * 3 + plank, 5) - 0.5f) * 0.14f);
                        if (seam) c = S(oak, 0.66f);
                        else if (Noise(x, y, 6) > 0.95f) c = S(c, 0.9f);
                    }

                    p[x, y] = Glow(c, y, C(0.50f, 0.78f, 0.66f), 0.28f);
                }
            }

            // Tapis rond tressé sous la barque : anneaux de couleur, trame en biais.
            Vector2 center = AquariumRoomLayout.ToPixels(AquariumRoomLayout.Lake[0].Foot) + new Vector2(0f, 4f);
            Color[] rings = { C(0.86f, 0.80f, 0.64f), C(0.66f, 0.22f, 0.18f), C(0.86f, 0.80f, 0.64f), C(0.26f, 0.44f, 0.32f), C(0.24f, 0.34f, 0.52f) };
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < StandBottom; y++)
                {
                    float dx = (x + 0.5f - center.x) / 104f;
                    float dy = (y + 0.5f - center.y) / 42f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > 1f) continue;
                    int ring = Mathf.FloorToInt((1f - d) * 11f) % rings.Length;
                    Color c = rings[ring];
                    if ((x + y * 2) % 4 == 0) c = S(c, 0.82f);
                    if (d > 0.96f) c = S(rings[1], 0.7f);
                    p[x, y] = c;
                }
            }
        }

        // ---------- Amazonie : ponton de planches ----------

        public static void Amazon(Color[,] p)
        {
            int w = p.GetLength(0);
            Color teak = C(0.52f, 0.36f, 0.22f);
            Color moss = C(0.30f, 0.50f, 0.20f);
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < StandBottom; y++)
                {
                    int row = y / 7;
                    int end = (x + row * 23) % 56;
                    int plank = (x + row * 23) / 56;
                    Color c = S(teak, 0.9f + Noise(plank, row, 3) * 0.22f);
                    if (Noise(x / 5, y, 4) > 0.86f) c = S(c, 0.84f);          // fil du bois
                    if (y % 7 == 0) c = C(0.10f, 0.07f, 0.05f);              // jour entre deux planches
                    else if (end == 0) c = C(0.16f, 0.10f, 0.06f);           // bout de planche
                    else if ((end == 2 || end == 53) && y % 7 == 3) c = C(0.30f, 0.30f, 0.32f); // clous
                    else if (Noise(x, y, 9) > 0.996f) c = C(0.30f, 0.18f, 0.10f); // nœud

                    // Mousse vers le bas et le long des murs.
                    float edge = Mathf.Min(y, Mathf.Min(x - Room - 10, w - 10 - x));
                    if (edge < 14 && Noise(x / 3, y / 3, 7) > 0.35f + edge / 20f) c = S(moss, 0.9f + Noise(x, y, 8) * 0.25f);
                    p[x, y] = Glow(c, y, C(0.86f, 0.62f, 0.30f), 0.22f);
                }
            }

            // Feuilles mortes tombées sur le ponton.
            var random = new System.Random(71);
            Color[] leaves = { C(0.74f, 0.40f, 0.16f), C(0.62f, 0.50f, 0.20f), C(0.50f, 0.28f, 0.12f) };
            for (int i = 0; i < 40; i++)
            {
                int x = random.Next(Room + 12, w - 12);
                int y = random.Next(4, StandBottom - 6);
                Color leaf = leaves[i % leaves.Length];
                PixelArt.Set(p, x, y, leaf);
                PixelArt.Set(p, x + 1, y, S(leaf, 1.15f));
                PixelArt.Set(p, x + 1, y + 1, S(leaf, 0.85f));
            }
        }

        // ---------- Grande Barrière : sable blanc et caillebotis ----------

        public static void Reef(Color[,] p)
        {
            int w = p.GetLength(0);
            Color sand = C(0.94f, 0.91f, 0.82f);
            Color plank = C(0.78f, 0.72f, 0.62f);
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < StandBottom; y++)
                {
                    // Caillebotis : de l'entrée vers le milieu, puis jusqu'à la terrasse devant le bassin.
                    bool path = (y >= 51 && y <= 92 && x < Room + 140) || (x >= Room + 128 && x <= Room + 162 && y >= 51) || y >= 140;
                    Color c;
                    if (path)
                    {
                        bool across = y >= 140 || y <= 92 && x < Room + 128;
                        int seam = across ? x % 8 : y % 8;
                        c = S(plank, 0.94f + Noise(across ? x / 8 : y / 8, 3, 4) * 0.14f);
                        if (seam == 0) c = C(0.44f, 0.40f, 0.34f);
                    }
                    else
                    {
                        c = Noise(x, y, 4) > 0.7f ? S(sand, 0.93f) : sand;
                        if (Noise(x / 3, y / 2, 5) > 0.9f) c = S(sand, 0.88f); // petites rides du vent
                    }

                    p[x, y] = Glow(c, y, C(0.50f, 0.90f, 0.96f), 0.25f);
                }
            }

            // Coquillages et étoiles de mer sur le sable.
            var random = new System.Random(91);
            for (int i = 0; i < 26; i++)
            {
                int x = random.Next(Room + 14, w - 14);
                int y = random.Next(4, 136);
                bool onPath = (y >= 49 && y <= 94 && x < Room + 142) || (x >= Room + 126 && x <= Room + 164);
                if (onPath) continue;
                if (i % 3 == 0)
                {
                    Color star = C(0.94f, 0.50f, 0.30f);
                    foreach (Vector2Int d in new[] { Vector2Int.zero, Vector2Int.up, Vector2Int.left, Vector2Int.right, new Vector2Int(-1, -1), new Vector2Int(1, -1) })
                    {
                        PixelArt.Set(p, x + d.x, y + d.y, star);
                    }
                }
                else
                {
                    Color shell = i % 2 == 0 ? C(0.98f, 0.86f, 0.80f) : C(0.96f, 0.94f, 0.88f);
                    PixelArt.Rect(p, x, y, x + 2, y + 1, shell);
                    PixelArt.Set(p, x + 1, y + 2, S(shell, 0.85f));
                }
            }
        }

        // ---------- Japon : jardin zen ----------

        /// <summary>Haut de la bande de bois (engawa) devant le bassin.</summary>
        public const int EngawaBottom = 136;

        public static void Japan(Color[,] p)
        {
            int w = p.GetLength(0);
            Color gravel = C(0.86f, 0.85f, 0.80f);
            var rocks = new Vector2[AquariumRoomLayout.JapanRocks.Length];
            for (int i = 0; i < rocks.Length; i++)
            {
                rocks[i] = AquariumRoomLayout.ToPixels(AquariumRoomLayout.JapanRocks[i]) + new Vector2(0f, 6f);
            }

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < StandBottom; y++)
                {
                    Color c;
                    if (y >= EngawaBottom)
                    {
                        // Engawa : planches de bois sombre dans la profondeur, bord éclairé.
                        c = (x / 12) % 2 == 0 ? C(0.40f, 0.26f, 0.16f) : C(0.36f, 0.23f, 0.14f);
                        if (x % 12 == 0) c = C(0.20f, 0.12f, 0.08f);
                        if (y == EngawaBottom || y == EngawaBottom + 1) c = C(0.56f, 0.38f, 0.22f);
                    }
                    else if (y == EngawaBottom - 1)
                    {
                        c = C(0.30f, 0.30f, 0.28f); // ombre du bord de l'engawa
                    }
                    else
                    {
                        c = Noise(x, y, 4) > 0.7f ? S(gravel, 0.92f) : gravel;
                        // Cercles ratissés autour des rochers, sinon vagues parallèles.
                        float nearest = float.MaxValue;
                        foreach (Vector2 r in rocks)
                        {
                            float d = Vector2.Distance(new Vector2((x - r.x) * 0.6f, y - r.y), Vector2.zero);
                            nearest = Mathf.Min(nearest, d);
                        }

                        bool line = nearest < 26f ? nearest % 5f < 1.2f : (y + Mathf.Sin(x * 0.05f) * 2.2f) % 5f < 1.2f;
                        if (line) c = S(gravel, 0.78f);
                    }

                    p[x, y] = Glow(c, y, C(0.60f, 0.80f, 0.92f), 0.18f);
                }
            }

            // Pas japonais : de l'entrée (à gauche) jusqu'à l'engawa.
            Color stone = C(0.56f, 0.56f, 0.54f);
            var start = new Vector2(Room + 14f, 74f);
            var end = new Vector2(Room + 190f, EngawaBottom - 6f);
            for (int i = 0; i < 9; i++)
            {
                float t = i / 8f;
                Vector2 at = Vector2.Lerp(start, end, t) + new Vector2(0f, Mathf.Sin(t * Mathf.PI) * -14f);
                float rx = 7f + (i % 2) * 1.5f;
                float ry = 4.5f;
                for (int x = Mathf.FloorToInt(at.x - rx - 1); x <= Mathf.CeilToInt(at.x + rx + 1); x++)
                {
                    for (int y = Mathf.FloorToInt(at.y - ry - 1); y <= Mathf.CeilToInt(at.y + ry + 1); y++)
                    {
                        float dx = (x + 0.5f - at.x) / rx;
                        float dy = (y + 0.5f - at.y) / ry;
                        float d = dx * dx + dy * dy;
                        if (d > 1.3f) continue;
                        Color c = d > 1f ? S(gravel, 0.7f) : S(stone, 1.05f + dy * 0.15f - dx * 0.08f);
                        PixelArt.Set(p, x, y, c);
                    }
                }
            }
        }
    }
}
