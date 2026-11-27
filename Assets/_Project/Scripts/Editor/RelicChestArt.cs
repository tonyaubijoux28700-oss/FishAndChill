using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Coffre aux reliques de l'inventaire (onglet « Reliques »), vu de face et ouvert : couvercle relevé dont on voit
    /// l'intérieur (planches, bandes de fer, plaque de laiton pour le titre), charnières, caisse en bois aux coins de
    /// laiton, serrure, et fond de velours bordeaux où l'interface creuse un emplacement en forme de chaque artefact.
    /// 300×160 px, un pixel = 4 unités d'interface (25 px par unité, comme la charte).
    /// </summary>
    public static class RelicChestArt
    {
        public const int Width = 300;
        public const int Height = 160;

        /// <summary>Le velours, en pixels du dessin (y depuis le bas) : l'interface y range les emplacements.</summary>
        public static readonly RectInt Velvet = new RectInt(12, 18, 276, 92);

        /// <summary>La plaque de laiton du couvercle, où l'interface écrit le titre.</summary>
        public static readonly RectInt Plate = new RectInt(78, 128, 144, 22);

        private const int BoxTop = 118;
        private const int LidBottom = 123;

        private static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

        private static readonly Color Ink = C(20, 12, 8);
        private static readonly Color Wood = C(112, 66, 36);
        private static readonly Color WoodDark = C(74, 42, 22);
        private static readonly Color WoodLight = C(150, 96, 54);
        private static readonly Color LidWood = C(84, 48, 26);
        private static readonly Color Iron = C(54, 52, 56);
        private static readonly Color IronLight = C(96, 94, 100);
        private static readonly Color Brass = C(206, 156, 66);
        private static readonly Color BrassLight = C(246, 214, 130);
        private static readonly Color BrassDark = C(138, 94, 34);
        private static readonly Color VelvetColor = C(96, 20, 34);
        private static readonly Color VelvetDark = C(56, 10, 20);
        private static readonly Color VelvetLight = C(128, 34, 50);

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 982451653;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        /// <summary>Planche de bois : veines étirées en long, nœuds sombres, joints entre les planches.</summary>
        private static Color Plank(int x, int y, Color baseColor, int plankHeight, int seed)
        {
            int row = y / plankHeight;
            if (y % plankHeight == 0) return Color.Lerp(baseColor, Ink, 0.6f);                 // le joint
            if (y % plankHeight == 1) return Color.Lerp(baseColor, WoodLight, 0.25f);          // l'arête éclairée
            float grain = Mathf.PerlinNoise(x * 0.04f + row * 17.3f, y * 0.55f + seed);
            Color c = grain > 0.62f ? Color.Lerp(baseColor, WoodDark, 0.45f) : grain < 0.32f ? Color.Lerp(baseColor, WoodLight, 0.3f) : baseColor;
            float knot = Vector2.Distance(new Vector2(x, y), new Vector2(40f + row * 83f % 220f, row * plankHeight + plankHeight * 0.5f));
            if (knot < 2.2f) c = Color.Lerp(c, Ink, 0.5f);
            else if (knot < 3.4f) c = Color.Lerp(c, WoodDark, 0.5f);
            return c;
        }

        private static void Rivet(Color[,] p, int x, int y)
        {
            p[x, y] = BrassLight;
            p[x + 1, y] = Brass;
            p[x, y - 1] = Brass;
            p[x + 1, y - 1] = BrassDark;
        }

        /// <summary>Coin de laiton en équerre, rivets, contour sombre.</summary>
        private static void Corner(Color[,] p, int x0, int y0, int dx, int dy)
        {
            const int arm = 16, thick = 6;
            for (int i = 0; i < arm; i++)
            {
                for (int j = 0; j < thick; j++)
                {
                    foreach (var (ax, ay) in new[] { (i, j), (j, i) })
                    {
                        int x = x0 + ax * dx, y = y0 + ay * dy;
                        bool edge = ax == arm - 1 || ay == thick - 1 && ax >= thick || ax == thick - 1 && ay >= thick && ay < arm || ay == arm - 1;
                        p[x, y] = edge ? Ink : (ax + ay) % 7 == 0 ? BrassLight : ay < 2 || ax < 2 ? BrassLight : Brass;
                    }
                }
            }

            Rivet(p, x0 + 3 * dx, y0 + 3 * dy + (dy > 0 ? 1 : 0));
            Rivet(p, x0 + 11 * dx, y0 + 3 * dy + (dy > 0 ? 1 : 0));
            Rivet(p, x0 + 3 * dx, y0 + 11 * dy + (dy > 0 ? 1 : 0));
        }

        /// <summary>Enregistre le coffre (Art/UI/Reliques/Coffre.png) et renvoie son sprite.</summary>
        public static Sprite Save() =>
            PixelArt.Save("Assets/_Project/Art/UI/Reliques/Coffre.png", Chest(), UiSkinArt.PixelsPerUnit, new Vector2(0.5f, 0.5f), Vector4.zero, FilterMode.Point);

        public static Color[,] Chest()
        {
            var p = new Color[Width, Height];

            // ---- Le couvercle relevé : un trapèze un peu plus large en haut, on en voit l'intérieur. ----
            for (int y = LidBottom; y < Height; y++)
            {
                float t = (y - LidBottom) / (float)(Height - 1 - LidBottom);
                int inset = Mathf.RoundToInt(Mathf.Lerp(8f, 0f, t));
                for (int x = inset; x < Width - inset; x++)
                {
                    int fromEdge = Mathf.Min(Mathf.Min(x - inset, Width - 1 - inset - x), Mathf.Min(y - LidBottom, Height - 1 - y));
                    Color c;
                    if (fromEdge == 0) c = Ink;
                    else if (fromEdge < 6) c = Plank(x, y, Wood, 40, 3);                     // le cadre du couvercle
                    else if (fromEdge == 6) c = Ink;
                    else
                    {
                        c = Plank(x, y, LidWood, 9, 5);                                        // l'intérieur, plus sombre
                        c = Color.Lerp(c, Ink, Mathf.Clamp01((8f - (y - LidBottom)) / 10f) * 0.5f);   // l'ombre au pied du couvercle
                    }

                    p[x, y] = c;
                }
            }

            // Deux bandes de fer sur le couvercle.
            foreach (int bx in new[] { 44, Width - 52 })
            {
                for (int y = LidBottom + 1; y < Height - 1; y++)
                {
                    for (int x = bx; x < bx + 8; x++)
                    {
                        p[x, y] = x == bx || x == bx + 7 ? Ink : x == bx + 1 ? IronLight : Iron;
                    }

                    if ((y - LidBottom) % 9 == 4) { p[bx + 3, y] = IronLight; p[bx + 4, y] = C(140, 138, 146); }
                }
            }

            // La plaque de laiton (titre), biseautée, avec quatre rivets.
            for (int x = Plate.xMin; x < Plate.xMax; x++)
            {
                for (int y = Plate.yMin; y < Plate.yMax; y++)
                {
                    int e = Mathf.Min(Mathf.Min(x - Plate.xMin, Plate.xMax - 1 - x), Mathf.Min(y - Plate.yMin, Plate.yMax - 1 - y));
                    bool cut = (x - Plate.xMin < 2 || Plate.xMax - 1 - x < 2) && (y - Plate.yMin < 2 || Plate.yMax - 1 - y < 2);
                    if (cut) continue;
                    bool upperLeft = (x - Plate.xMin) * Plate.height < (Plate.yMax - y) * Plate.width;
                    p[x, y] = e == 0 ? Ink : e == 1 ? (upperLeft ? BrassLight : BrassDark) : e == 2 ? Brass : Color.Lerp(BrassDark, Brass, 0.35f + Hash(x, y, 9) * 0.1f);
                }
            }

            Rivet(p, Plate.xMin + 4, Plate.yMin + 5);
            Rivet(p, Plate.xMax - 6, Plate.yMin + 5);
            Rivet(p, Plate.xMin + 4, Plate.yMax - 4);
            Rivet(p, Plate.xMax - 6, Plate.yMax - 4);

            // ---- La fente des charnières, entre le couvercle et la caisse. ----
            for (int y = BoxTop; y < LidBottom; y++)
            {
                for (int x = 6; x < Width - 6; x++) p[x, y] = Ink;
            }

            foreach (int hx in new[] { 50, Width - 64 })
            {
                for (int x = hx; x < hx + 14; x++)
                {
                    for (int y = BoxTop - 2; y < LidBottom + 3; y++)
                    {
                        bool edge = x == hx || x == hx + 13 || y == BoxTop - 2 || y == LidBottom + 2;
                        p[x, y] = edge ? Ink : (x - hx) % 4 == 0 ? BrassDark : y == LidBottom ? BrassLight : Brass;
                    }
                }
            }

            // ---- La caisse : bois, velours au fond, ombre portée par les bords. ----
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < BoxTop; y++)
                {
                    if (p[x, y].a > 0f) continue;
                    bool outline = x == 0 || x == Width - 1 || y == 0 || y == BoxTop - 1;
                    if (outline) { p[x, y] = Ink; continue; }

                    if (Velvet.Contains(new Vector2Int(x, y)))
                    {
                        // Velours : trame fine, plis doux, ombre sous les bords du haut et de gauche.
                        float fold = Mathf.PerlinNoise(x * 0.022f + 3f, y * 0.035f) * 0.7f + Mathf.PerlinNoise(x * 0.07f, y * 0.07f + 9f) * 0.3f;
                        Color c = fold > 0.62f ? Color.Lerp(VelvetColor, VelvetLight, 0.6f) : fold < 0.4f ? Color.Lerp(VelvetColor, VelvetDark, 0.35f) : VelvetColor;
                        if ((x + y) % 3 == 0 && Hash(x, y, 7) > 0.5f) c = Color.Lerp(c, VelvetDark, 0.15f);   // le grain du velours
                        float shade = Mathf.Max(Mathf.Clamp01((7f - (Velvet.yMax - 1 - y)) / 7f), Mathf.Clamp01((5f - (x - Velvet.xMin)) / 5f) * 0.7f);
                        p[x, y] = Color.Lerp(c, Ink, shade * 0.75f);
                        continue;
                    }

                    // Le bord intérieur, juste autour du velours : une arête claire.
                    bool innerLip = x == Velvet.xMin - 1 || x == Velvet.xMax || y == Velvet.yMin - 1 || y == Velvet.yMax;
                    if (innerLip && x >= Velvet.xMin - 1 && x <= Velvet.xMax && y >= Velvet.yMin - 1 && y <= Velvet.yMax)
                    {
                        p[x, y] = y == Velvet.yMin - 1 ? WoodLight : Ink;
                        continue;
                    }

                    // Le bois de la caisse : planches de face en bas, montants sur les côtés.
                    Color w = y < Velvet.yMin ? Plank(x, y, Wood, 8, 1) : Plank(y, x, Wood, 300, 2);
                    if (y == BoxTop - 2) w = WoodLight;
                    p[x, y] = w;
                }
            }

            // Coins de laiton et serrure.
            Corner(p, 1, 1, 1, 1);
            Corner(p, Width - 2, 1, -1, 1);
            Corner(p, 1, BoxTop - 2, 1, -1);
            Corner(p, Width - 2, BoxTop - 2, -1, -1);

            int lx = Width / 2 - 11;
            for (int x = lx; x < lx + 22; x++)
            {
                for (int y = 1; y < 22; y++)
                {
                    int e = Mathf.Min(Mathf.Min(x - lx, lx + 21 - x), Mathf.Min(y - 1, 21 - y));
                    bool cut = (x - lx < 3 || lx + 21 - x < 3) && y < 4;
                    if (cut) continue;
                    p[x, y] = e == 0 ? Ink : e == 1 ? BrassLight : (x - lx) > 16 ? BrassDark : Brass;
                }
            }

            // Le trou de la serrure.
            for (int y = 6; y < 15; y++)
            {
                for (int x = Width / 2 - 2; x < Width / 2 + 2; x++)
                {
                    bool hole = y >= 11 ? Vector2.Distance(new Vector2(x + 0.5f, y), new Vector2(Width / 2f, 12.5f)) < 2.3f : Mathf.Abs(x + 0.5f - Width / 2f) < 1f;
                    if (hole) p[x, y] = Ink;
                }
            }

            return p;
        }
    }
}
