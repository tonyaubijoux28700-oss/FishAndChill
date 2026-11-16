using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Pixel art de l'aquarium, un des plus grands bâtiments du jeu (16 px par case) :
    /// façade 192×160 (12×10 cases) avec perron, colonnes, vitrines en arc, rosace, et un immense dôme de verre
    /// plein d'eau surmonté d'une lanterne ; grande salle 480×288 (30×18 cases) avec un bassin géant ;
    /// vitre du bassin (posée par-dessus les poissons), colonne à méduses, banc et icône 16×16 de minimap.
    /// </summary>
    public static class AquariumArt
    {
        public const int ExteriorWidth = 192;
        public const int ExteriorHeight = 160;

        /// <summary>Pivot au pied du perron, au milieu de la façade (la porte).</summary>
        public static readonly Vector2 ExteriorPivot = new Vector2(0.5f, 2f / ExteriorHeight);

        public const int InteriorWidth = 480;
        public const int InteriorHeight = 288;

        // Le bassin géant, en pixels de la salle (bas-gauche = coin de la salle).
        private const int TankLeft = 20;
        private const int TankRight = 459;
        private const int TankBottom = 180;
        private const int TankTop = 282;
        private const int Frame = 5;
        private const int WaterLine = 272;
        private const int StandBottom = 166;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Outline = C(0.07f, 0.10f, 0.14f);
        private static readonly Color Plaster = C(0.95f, 0.93f, 0.87f);
        private static readonly Color Stone = C(0.84f, 0.82f, 0.77f);
        private static readonly Color Tile = C(0.18f, 0.56f, 0.62f);
        private static readonly Color TileDark = C(0.12f, 0.42f, 0.48f);
        private static readonly Color RoofTile = C(0.16f, 0.50f, 0.58f);
        private static readonly Color Metal = C(0.13f, 0.24f, 0.30f);
        private static readonly Color MetalLight = C(0.30f, 0.46f, 0.52f);
        private static readonly Color Brass = C(0.86f, 0.68f, 0.30f);
        private static readonly Color BrassDark = C(0.62f, 0.45f, 0.16f);
        private static readonly Color WaterTop = C(0.46f, 0.82f, 0.88f);
        private static readonly Color WaterDeep = C(0.10f, 0.34f, 0.54f);
        private static readonly Color Sand = C(0.92f, 0.82f, 0.58f);
        private static readonly Color SandDark = C(0.80f, 0.68f, 0.44f);
        private static readonly Color Weed = C(0.30f, 0.66f, 0.36f);
        private static readonly Color WeedDark = C(0.18f, 0.48f, 0.28f);
        private static readonly Color CoralPink = C(0.96f, 0.50f, 0.58f);
        private static readonly Color CoralOrange = C(1.00f, 0.62f, 0.28f);
        private static readonly Color CoralPurple = C(0.75f, 0.55f, 0.85f);
        private static readonly Color Rock = C(0.44f, 0.50f, 0.58f);
        private static readonly Color Bubble = C(0.86f, 0.97f, 1.00f);
        private static readonly Color FishOrange = C(1.00f, 0.56f, 0.22f);
        private static readonly Color FishYellow = C(1.00f, 0.86f, 0.30f);
        private static readonly Color Wood = C(0.62f, 0.42f, 0.24f);
        private static readonly Color Leaf = C(0.26f, 0.62f, 0.30f);

        private static float Step(float value, float steps) => Mathf.Round(value * steps) / steps;

        // ======================================================================
        // Façade
        // ======================================================================

        public static Color[,] Exterior()
        {
            var p = new Color[ExteriorWidth, ExteriorHeight];
            const float cx = 95.5f;           // axe de la porte : tout est symétrique autour (x ↔ 191 - x)
            const int band = 9;               // hauteur du soubassement en carreaux

            // Derrière : toits en tuiles des ailes, puis le dôme.
            DrawWingRoof(p, 7, 66, 66, 14);
            DrawWingRoof(p, 125, 184, 66, 14);
            DrawDome(p, cx, 69f, 76f, 72f, ribs: 7);
            DrawLantern(p, cx, 139);

            // Ailes : murs enduits, soubassement en carreaux, corniche ondulée.
            DrawWall(p, 7, 184, 6, 66, band);
            for (int x = 5; x <= 186; x++)
            {
                int wave = Mathf.RoundToInt(Mathf.Sin(x * 0.45f) * 1f);
                for (int y = 63 + wave; y <= 70; y++)
                {
                    p[x, y] = y >= 69 ? MetalLight : y >= 67 ? Tile : TileDark;
                }
            }

            // Deux vitrines en arc dans chaque aile, entre des pilastres (en miroir).
            int[] windows = { 14, 39 };
            for (int i = 0; i < windows.Length; i++)
            {
                DrawArchWindow(p, windows[i], windows[i] + 18, 19, 56, seed: 11 + i);
                DrawArchWindow(p, 191 - windows[i] - 18, 191 - windows[i], 19, 56, seed: 21 + i);
            }

            foreach (int x in new[] { 7, 33, 58 })
            {
                DrawPilaster(p, x, x + 4, 6, 62, band);
                DrawPilaster(p, 191 - x - 4, 191 - x, 6, 62, band);
            }

            // Pavillon d'entrée, en avant, plus haut que les ailes.
            DrawWall(p, 64, 127, 6, 80, band, lighter: true);
            for (int x = 62; x <= 129; x++)
            {
                for (int y = 78; y <= 84; y++)
                {
                    p[x, y] = y >= 83 ? MetalLight : y >= 81 ? Tile : TileDark;
                }
            }

            DrawColumn(p, 64, 69, 6, 77);
            DrawColumn(p, 122, 127, 6, 77);
            DrawRoseWindow(p, cx, 68f, 6f);
            DrawSign(p, 77, 114, 52, 57);
            DrawArchDoor(p, 81, 110, 6, 35, 15, handleY: 20);

            // Perron : trois marches et un tapis jusqu'à la porte.
            DrawStairs(p, 72, 119, 87, 104);

            // Jardinières avec de petits palmiers de chaque côté du perron.
            DrawPlanter(p, 58);
            DrawPlanter(p, 121);

            PixelArt.SelectiveOutline(p, Outline, 0.6f);

            // Ombre douce au sol, seulement là où il n'y a rien.
            for (int x = 2; x < ExteriorWidth - 2; x++)
            {
                for (int y = 0; y <= 4; y++)
                {
                    if (!PixelArt.IsFilled(p, x, y))
                    {
                        p[x, y] = C(0.05f, 0.15f, 0.08f, 0.28f);
                    }
                }
            }

            return p;
        }

        private static void DrawWall(Color[,] p, int x0, int x1, int y0, int y1, int band, bool lighter = false)
        {
            for (int x = x0; x <= x1; x++)
            {
                float side = Mathf.InverseLerp(x0, x1, x);
                for (int y = y0; y <= y1; y++)
                {
                    Color c = DetailedFish.Shade(Plaster, Step((lighter ? 1.12f : 1.04f) - side * 0.22f, 6f));
                    if (y <= y0 + band)
                    {
                        c = ((x / 8) + (y / 4)) % 2 == 0 ? Tile : TileDark;
                        if (y == y0 + band)
                        {
                            c = Color.Lerp(c, Color.white, 0.3f);
                        }
                    }
                    else if (y % 12 == 0)
                    {
                        c = Color.Lerp(c, Stone, 0.5f); // joints des pierres
                    }

                    p[x, y] = c;
                }
            }
        }

        private static void DrawWingRoof(Color[,] p, int x0, int x1, int yBase, int height)
        {
            for (int y = yBase; y <= yBase + height; y++)
            {
                int inset = (y - yBase) * 3 / 2;
                for (int x = x0 + inset; x <= x1 - inset; x++)
                {
                    Color c = (y - yBase) % 4 == 0 ? Color.Lerp(RoofTile, Color.black, 0.25f) : RoofTile;
                    if ((x + (y / 4) * 3) % 8 == 0)
                    {
                        c = Color.Lerp(RoofTile, Color.black, 0.15f);
                    }

                    p[x, y] = DetailedFish.Shade(c, y > yBase + height - 5 ? 1.15f : 1f);
                }
            }
        }

        private static void DrawPilaster(Color[,] p, int x0, int x1, int y0, int y1, int band)
        {
            for (int x = x0; x <= x1; x++)
            {
                float u = (x - x0) / (float)Mathf.Max(1, x1 - x0) * 2f - 1f;
                float light = Step(1.12f - (u + 1f) * 0.22f, 5f);
                for (int y = y0 + band + 1; y <= y1; y++)
                {
                    p[x, y] = DetailedFish.Shade(Stone, light);
                }
            }

            PixelArt.Rect(p, x0 - 1, y1 - 3, x1 + 1, y1, Brass);
        }

        private static void DrawColumn(Color[,] p, int x0, int x1, int y0, int y1)
        {
            for (int x = x0; x <= x1; x++)
            {
                float u = (x - x0) / (float)(x1 - x0) * 2f - 1f;
                float light = Step(1.15f - (u + 1f) * 0.25f, 5f);
                for (int y = y0; y <= y1; y++)
                {
                    Color c = DetailedFish.Shade(Stone, light);
                    if ((x - x0) % 3 == 1 && y > y0 + 6 && y < y1 - 6)
                    {
                        c = DetailedFish.Shade(Stone, light - 0.12f); // cannelures
                    }

                    p[x, y] = c;
                }
            }

            PixelArt.Rect(p, x0 - 2, y0, x1 + 2, y0 + 3, Stone);
            PixelArt.Rect(p, x0 - 2, y1 - 4, x1 + 2, y1, Brass);
            PixelArt.Rect(p, x0 - 2, y1 - 4, x1 + 2, y1 - 4, BrassDark);
        }

        /// <summary>Vitrine en arc : un petit aquarium vu de la rue (eau, algues, poissons).</summary>
        private static void DrawArchWindow(Color[,] p, int x0, int x1, int y0, int y1, int seed)
        {
            float cx = (x0 + x1) * 0.5f;
            float radius = (x1 - x0) * 0.5f;
            float springY = y1 - radius;
            var random = new System.Random(seed);
            for (int x = x0 - 2; x <= x1 + 2; x++)
            {
                for (int y = y0 - 2; y <= y1 + 2; y++)
                {
                    float dx = x - cx;
                    float top = springY + Mathf.Sqrt(Mathf.Max(0f, (radius + 2f) * (radius + 2f) - dx * dx));
                    if (y > top || Mathf.Abs(dx) > radius + 2f)
                    {
                        continue;
                    }

                    bool frame = Mathf.Abs(dx) > radius - 0.5f || y < y0 || y > springY + Mathf.Sqrt(Mathf.Max(0f, radius * radius - dx * dx)) - 1f;
                    if (frame)
                    {
                        p[x, y] = dx < 0f ? Brass : BrassDark;
                        continue;
                    }

                    float t = Mathf.InverseLerp(y0, y1, y);
                    Color c = Color.Lerp(WaterDeep, WaterTop, Step(t, 5f));
                    if ((x - y + 300) % 17 == 0)
                    {
                        c = Color.Lerp(c, Color.white, 0.35f);
                    }

                    p[x, y] = c;
                }
            }

            for (int x = x0 + 1; x < x1; x++)
            {
                p[x, y0] = Sand;
                p[x, y0 + 1] = x % 3 == 0 ? SandDark : Sand;
            }

            DrawWeed(p, x0 + 4, y0 + 2, 14 + random.Next(8));
            DrawWeed(p, x1 - 5, y0 + 2, 10 + random.Next(8));
        }

        private static void DrawArchDoor(Color[,] p, int x0, int x1, int y0, int springY, int radius, int handleY)
        {
            float cx = (x0 + x1 + 1) * 0.5f;
            for (int x = x0 - 3; x <= x1 + 3; x++)
            {
                for (int y = y0; y <= springY + radius + 3; y++)
                {
                    float dx = x + 0.5f - cx;
                    float outer = springY + Mathf.Sqrt(Mathf.Max(0f, (radius + 3f) * (radius + 3f) - dx * dx));
                    if (y > outer || Mathf.Abs(dx) > radius + 3f)
                    {
                        continue;
                    }

                    float inner = springY + Mathf.Sqrt(Mathf.Max(0f, radius * radius - dx * dx));
                    bool frame = Mathf.Abs(dx) > radius || y > inner;
                    Color c;
                    if (frame)
                    {
                        c = Mathf.Abs(dx) > radius + 1.5f || y > inner + 1.5f ? Stone : Metal;
                    }
                    else
                    {
                        c = Color.Lerp(WaterDeep, WaterTop, Step(Mathf.InverseLerp(y0, springY + radius, y), 5f));
                        if (Mathf.Abs(dx) < 1f || (y > springY && Mathf.Abs(y - springY - 2) < 1))
                        {
                            c = Metal; // montants de la double porte
                        }
                        else if ((x - y + 400) % 13 == 0)
                        {
                            c = Color.Lerp(c, Color.white, 0.45f); // reflets
                        }
                    }

                    p[x, y] = c;
                }
            }

            PixelArt.Rect(p, (int)cx - 4, handleY, (int)cx - 3, handleY + 3, Brass);
            PixelArt.Rect(p, (int)cx + 2, handleY, (int)cx + 3, handleY + 3, Brass);
        }

        private static void DrawRoseWindow(Color[,] p, float cx, float cy, float radius)
        {
            for (int x = Mathf.FloorToInt(cx - radius - 3); x <= Mathf.CeilToInt(cx + radius + 3); x++)
            {
                for (int y = Mathf.FloorToInt(cy - radius - 3); y <= Mathf.CeilToInt(cy + radius + 3); y++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > radius + 3f)
                    {
                        continue;
                    }

                    Color c;
                    if (d > radius)
                    {
                        c = dx < 0f && dy > 0f ? Brass : BrassDark;
                    }
                    else
                    {
                        c = Color.Lerp(WaterDeep, WaterTop, Step((dy / radius + 1f) * 0.5f, 4f));
                        float angle = Mathf.Atan2(dy, dx);
                        if (Mathf.Abs(Mathf.Sin(angle * 4f)) < 0.12f && d > 4f)
                        {
                            c = Brass; // rayons de la rosace
                        }
                    }

                    PixelArt.Set(p, x, y, c);
                }
            }

        }

        private static void DrawSign(Color[,] p, int x0, int x1, int y0, int y1)
        {
            PixelArt.Rect(p, x0, y0, x1, y1, BrassDark);
            PixelArt.Rect(p, x0 + 1, y0 + 1, x1 - 1, y1 - 1, Brass);
            PixelArt.Rect(p, x0 + 1, y1 - 1, x1 - 1, y1 - 1, C(1f, 0.90f, 0.55f));
            int middle = (x0 + x1) / 2;
            // Plaque sobre : un filet gravé et deux rivets.
            int midY = (y0 + y1) / 2;
            PixelArt.Rect(p, x0 + 5, midY, x1 - 5, midY, BrassDark);
            PixelArt.Set(p, x0 + 2, midY, Metal);
            PixelArt.Set(p, x1 - 2, midY, Metal);
            PixelArt.Set(p, middle, midY, Brass);
        }

        private static void DrawStairs(Color[,] p, int left, int right, int carpetLeft, int carpetRight)
        {
            for (int step = 0; step < 3; step++)
            {
                int x0 = left + step * 4;
                int x1 = right - step * 4;
                int y0 = step * 2;
                for (int x = x0; x <= x1; x++)
                {
                    for (int y = y0; y <= y0 + 1; y++)
                    {
                        Color c = y == y0 + 1 ? Color.Lerp(Stone, Color.white, 0.3f) : DetailedFish.Shade(Stone, y == y0 ? 0.75f : 0.9f);
                        if (x % 24 == 0)
                        {
                            c = Color.Lerp(c, Color.black, 0.12f);
                        }

                        p[x, y] = c;
                    }
                }
            }

            // Tapis turquoise qui monte jusqu'à la porte.
            for (int x = carpetLeft; x <= carpetRight; x++)
            {
                for (int y = 0; y <= 6; y++)
                {
                    p[x, y] = x == carpetLeft || x == carpetRight ? BrassDark : y % 3 == 2 ? Color.Lerp(Tile, Color.white, 0.2f) : Tile;
                }
            }
        }

        private static void DrawPlanter(Color[,] p, int x0)
        {
            // Bac en pierre.
            for (int x = x0; x <= x0 + 12; x++)
            {
                for (int y = 0; y <= 8; y++)
                {
                    p[x, y] = DetailedFish.Shade(Stone, y == 8 ? 1.15f : x < x0 + 6 ? 1f : 0.8f);
                }
            }

            // Petit palmier : stipe et palmes.
            int cx = x0 + 6;
            for (int y = 9; y <= 26; y++)
            {
                int sway = (y - 9) / 6;
                PixelArt.Set(p, cx + sway, y, y % 3 == 0 ? C(0.48f, 0.34f, 0.20f) : Wood);
            }

            int topX = cx + 3;
            for (int k = 0; k <= 9; k++)
            {
                int droop = k * k / 12;
                PixelArt.Set(p, topX + k, 27 - droop, Leaf);
                PixelArt.Set(p, topX - k, 27 - droop, Leaf);
                PixelArt.Set(p, topX + k, 28 - droop, Color.Lerp(Leaf, Color.white, 0.2f));
                PixelArt.Set(p, topX - k, 28 - droop, Color.Lerp(Leaf, Color.white, 0.2f));
                PixelArt.Set(p, topX + k / 2, 28 + k / 2, Leaf);
                PixelArt.Set(p, topX - k / 2, 28 + k / 2, Color.Lerp(Leaf, Color.black, 0.15f));
            }
        }

        /// <summary>Dôme de verre plein d'eau, vide : nervures, reflets et rayons de lumière.</summary>
        private static void DrawDome(Color[,] p, float cx, float baseY, float rx, float ry, int ribs)
        {
            int width = p.GetLength(0);
            int height = p.GetLength(1);
            for (int x = 0; x < width; x++)
            {
                for (int y = Mathf.FloorToInt(baseY); y < height; y++)
                {
                    float u = (x + 0.5f - cx) / rx;
                    float v = (y + 0.5f - baseY) / ry;
                    float d = u * u + v * v;
                    if (d > 1f)
                    {
                        continue;
                    }

                    float w = Mathf.Sqrt(Mathf.Max(0f, 1f - v * v));
                    Color c = Color.Lerp(WaterDeep, WaterTop, Step(Mathf.Clamp01(v * 1.2f + 0.1f), 7f));

                    // Volume : bord du dôme plus sombre, cœur un peu plus clair.
                    float edge = Mathf.Sqrt(d);
                    if (edge > 0.9f)
                    {
                        c = Color.Lerp(c, WaterDeep, 0.5f);
                    }
                    else if (edge < 0.45f)
                    {
                        c = Color.Lerp(c, WaterTop, 0.15f);
                    }

                    // Grands reflets en haut à gauche.
                    if ((Mathf.Abs(u + 0.5f * w) < 0.035f || Mathf.Abs(u + 0.4f * w) < 0.015f) && v > 0.2f && v < 0.85f)
                    {
                        c = Color.Lerp(c, Color.white, 0.55f);
                    }

                    // Rayons de lumière dans l'eau.
                    if ((x + y / 2) % 37 < 4)
                    {
                        c = Color.Lerp(c, Color.white, 0.08f);
                    }

                    // Nervures : méridiens et parallèles.
                    bool rib = false;
                    for (int k = 0; k < ribs; k++)
                    {
                        float m = -0.9f + 1.8f * k / (ribs - 1);
                        rib |= Mathf.Abs(u - m * w) < 0.9f / rx;
                    }

                    foreach (float h in new[] { 0.3f, 0.58f, 0.82f })
                    {
                        rib |= Mathf.Abs(v - h) < 0.8f / ry;
                    }

                    if (rib)
                    {
                        c = u < -0.2f ? C(0.90f, 0.95f, 0.96f) : u < 0.4f ? C(0.74f, 0.84f, 0.88f) : C(0.58f, 0.68f, 0.74f);
                    }

                    p[x, y] = c;
                }
            }
        }

        /// <summary>Lanterne au sommet du dôme : anneau, petite coupole vitrée, épi doré et fanion.</summary>
        private static void DrawLantern(Color[,] p, float cx, int y0)
        {
            int x0 = Mathf.RoundToInt(cx) - 9;
            PixelArt.Rect(p, x0, y0, x0 + 18, y0 + 3, Metal);
            PixelArt.Rect(p, x0, y0 + 3, x0 + 18, y0 + 3, MetalLight);
            for (int x = x0 + 2; x <= x0 + 16; x++)
            {
                for (int y = y0 + 4; y <= y0 + 11; y++)
                {
                    float u = (x + 0.5f - cx) / 7.5f;
                    float v = (y - y0 - 4) / 8f;
                    if (u * u + v * v <= 1f)
                    {
                        p[x, y] = Mathf.Abs(u) < 0.12f || Mathf.Abs(Mathf.Abs(u) - 0.55f) < 0.12f ? MetalLight : Color.Lerp(WaterTop, Color.white, 0.3f);
                    }
                }
            }

            int top = y0 + 12;
            int mast = Mathf.RoundToInt(cx);
            PixelArt.Rect(p, mast - 1, top, mast, top + 6, Brass);
            PixelArt.Set(p, mast, top + 7, C(1f, 0.95f, 0.70f));

            // Fanion turquoise qui flotte vers la droite.
            for (int k = 0; k < 12; k++)
            {
                int wave = Mathf.RoundToInt(Mathf.Sin(k * 0.6f));
                int h = 4 - k / 4;
                for (int y = 0; y < h; y++)
                {
                    PixelArt.Set(p, mast + 1 + k, top + 2 + y + wave, y == 0 ? TileDark : Tile);
                }
            }
        }

        // ======================================================================
        // Salle intérieure
        // ======================================================================

        public static Color[,] Interior()
        {
            var p = new Color[InteriorWidth, InteriorHeight];

            DrawFloor(p);

            // Mur du fond, bleu nuit, avec des appliques dorées.
            for (int x = 0; x < InteriorWidth; x++)
            {
                for (int y = StandBottom; y < InteriorHeight; y++)
                {
                    p[x, y] = (x % 40 == 0) ? C(0.10f, 0.18f, 0.25f) : C(0.13f, 0.23f, 0.31f);
                }
            }

            // Meuble du bassin et plaques en laiton.
            PixelArt.Rect(p, TankLeft - 4, StandBottom, TankRight + 4, TankBottom - 1, Metal);
            PixelArt.Rect(p, TankLeft - 4, TankBottom - 2, TankRight + 4, TankBottom - 1, MetalLight);
            PixelArt.Rect(p, TankLeft - 4, StandBottom, TankRight + 4, StandBottom, C(0.08f, 0.15f, 0.20f));
            foreach (int px in new[] { 90, 240, 390 })
            {
                PixelArt.Rect(p, px - 14, StandBottom + 4, px + 14, StandBottom + 9, BrassDark);
                PixelArt.Rect(p, px - 13, StandBottom + 5, px + 13, StandBottom + 8, Brass);
            }

            DrawTank(p);

            // Murs latéraux.
            PixelArt.Rect(p, 0, 0, 9, InteriorHeight - 1, C(0.10f, 0.18f, 0.25f));
            PixelArt.Rect(p, InteriorWidth - 10, 0, InteriorWidth - 1, InteriorHeight - 1, C(0.10f, 0.18f, 0.25f));
            PixelArt.Rect(p, 9, 0, 9, StandBottom, MetalLight);
            PixelArt.Rect(p, InteriorWidth - 10, 0, InteriorWidth - 10, StandBottom, Metal);

            // Passage voûté vers la salle du lac, dans le mur de droite (sol de 3,2 à 6 cases de haut).
            PixelArt.Rect(p, InteriorWidth - 10, 51, InteriorWidth - 1, 96, C(0.11f, 0.16f, 0.27f));
            PixelArt.Rect(p, InteriorWidth - 10, 51, InteriorWidth - 1, 52, C(0.60f, 0.68f, 0.72f));
            PixelArt.Rect(p, InteriorWidth - 11, 50, InteriorWidth - 11, 100, Brass);
            PixelArt.Rect(p, InteriorWidth - 11, 97, InteriorWidth - 1, 100, Brass);
            PixelArt.Rect(p, InteriorWidth - 11, 101, InteriorWidth - 1, 101, BrassDark);

            // Paillasson de sortie.
            PixelArt.Rect(p, 216, 0, 263, 9, Tile);
            PixelArt.Rect(p, 219, 2, 260, 7, C(0.40f, 0.78f, 0.80f));
            return p;
        }

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 73856093 ^ y * 19349663 ^ seed * 83492791;
                h = (h << 13) ^ h;
                return ((h * (h * h * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647f;
            }
        }

        /// <summary>
        /// Marbre sombre : damier de dalles bleu nuit et turquoise profond veinées de clair, joints dorés, allée de marbre
        /// plus foncé bordée de laiton jusqu'au bassin, reflets bleus du bassin sur le sol poli, grand médaillon au poisson.
        /// </summary>
        private static void DrawFloor(Color[,] p)
        {
            Color navy = C(0.11f, 0.16f, 0.27f);
            Color teal = C(0.10f, 0.29f, 0.34f);
            Color vein = C(0.62f, 0.74f, 0.80f);
            Color joint = C(0.72f, 0.56f, 0.24f);
            Color runner = C(0.07f, 0.11f, 0.19f);
            for (int x = 0; x < InteriorWidth; x++)
            {
                for (int y = 0; y < StandBottom; y++)
                {
                    int tx = x / 32;
                    int ty = y / 32;
                    Color c = (tx + ty) % 2 == 0 ? navy : teal;

                    // Veines du marbre : fines lignes claires qui serpentent en biais, différentes sur chaque dalle.
                    float seed = Hash(tx, ty, 3) * 20f;
                    float wave = Mathf.Sin((x + y * 0.6f) * 0.11f + seed) * 4f + Mathf.Sin(y * 0.23f + seed * 2f) * 2f;
                    float line = Mathf.Repeat(x - y * 0.8f + wave + seed * 7f, 23f);
                    if (line < 0.9f)
                    {
                        c = Color.Lerp(c, vein, 0.35f);
                    }
                    else if (line < 1.8f || Hash(x, y, 7) > 0.97f)
                    {
                        c = Color.Lerp(c, vein, 0.14f);
                    }

                    // Léger dégradé dans chaque dalle : le marbre poli accroche la lumière vers le haut.
                    c = DetailedFish.Shade(c, 0.94f + Step((y % 32) / 32f, 3f) * 0.1f);

                    // Joints dorés.
                    if (x % 32 == 0 || y % 32 == 0)
                    {
                        c = (x % 32 == 0 && y % 32 == 0) ? C(0.95f, 0.82f, 0.42f) : joint;
                    }

                    // Allée centrale de marbre noir bordée de laiton, avec une fine vague dorée incrustée.
                    if (x >= 216 && x <= 263)
                    {
                        bool border = x <= 218 || x >= 261;
                        int inlay = Mathf.RoundToInt(240f + Mathf.Sin(y * 0.18f) * 9f);
                        c = border ? Brass : (x == inlay || x == inlay + 1) ? BrassDark : DetailedFish.Shade(runner, 0.95f + Hash(x / 3, y / 3, 5) * 0.1f);
                    }

                    // Le bassin se reflète sur le sol poli : lueur bleue qui monte vers la vitre.
                    float glow = Mathf.InverseLerp(70f, StandBottom, y);
                    c = Color.Lerp(c, C(0.30f, 0.62f, 0.80f), Step(glow, 5f) * 0.32f);
                    if (y > 120 && Hash(x / 2, y / 2, 11) > 0.985f)
                    {
                        c = Color.Lerp(c, Color.white, 0.4f); // éclats de lumière
                    }

                    p[x, y] = c;
                }
            }

            DrawMedallion(p, 240f, 92f, 40f);
        }

        private static void DrawMedallion(Color[,] p, float cx, float cy, float radius)
        {
            for (int x = Mathf.FloorToInt(cx - radius - 3); x <= Mathf.CeilToInt(cx + radius + 3); x++)
            {
                for (int y = Mathf.FloorToInt(cy - radius - 3); y <= Mathf.CeilToInt(cy + radius + 3); y++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > radius + 3f)
                    {
                        continue;
                    }

                    Color c;
                    if (d > radius)
                    {
                        c = Brass;
                    }
                    else if (d > radius - 4f)
                    {
                        float angle = Mathf.Atan2(dy, dx);
                        c = Mathf.Sin(angle * 16f) > 0f ? Tile : C(0.86f, 0.94f, 0.96f);
                    }
                    else
                    {
                        c = Color.Lerp(WaterDeep, Tile, Step((dy / radius + 1f) * 0.5f, 4f));
                    }

                    PixelArt.Set(p, x, y, c);
                }
            }

            // Grand poisson doré au centre.
            // Ronds dans l'eau autour du poisson, puis le poisson lui-même, incrusté en or.
            foreach (float ring in new[] { radius * 0.55f, radius * 0.75f })
            {
                for (int k = 0; k < 160; k++)
                {
                    float a = k / 160f * Mathf.PI * 2f;
                    if (Mathf.Sin(a * 5f) < -0.3f) continue; // anneau en pointillés
                    PixelArt.Set(p, Mathf.RoundToInt(cx + Mathf.Cos(a) * ring), Mathf.RoundToInt(cy + Mathf.Sin(a) * ring * 0.9f), C(0.55f, 0.80f, 0.86f));
                }
            }

            DrawGoldFish(p, Mathf.RoundToInt(cx), Mathf.RoundToInt(cy));
        }

        /// <summary>
        /// Poisson du médaillon, incrusté en or : dessiné par le même moteur que les poissons du jeu (corps arrondi éclairé,
        /// écailles, nageoires à rayons, queue fourchue), dans des tons d'or et de laiton, puis posé au centre.
        /// </summary>
        private static void DrawGoldFish(Color[,] p, int cx, int cy)
        {
            Color gold = C(0.94f, 0.76f, 0.32f);
            Color goldDark = C(0.60f, 0.42f, 0.12f);
            Color goldLight = C(1f, 0.92f, 0.62f);
            var fish = new DetailedFish
            {
                Width = 62, Height = 34, CenterY = 16.5f,
                Upper = new[] { new Vector2(12, 2.5f), new Vector2(18, 5f), new Vector2(27, 9f), new Vector2(38, 10.5f), new Vector2(48, 9.5f), new Vector2(54, 7f), new Vector2(58, 4f), new Vector2(61, 1.5f) },
                Lower = new[] { new Vector2(12, 2.5f), new Vector2(18, 4.5f), new Vector2(27, 8f), new Vector2(38, 9.5f), new Vector2(48, 8.5f), new Vector2(54, 6f), new Vector2(58, 3.5f), new Vector2(61, 1f) },
                Back = C(0.70f, 0.50f, 0.16f), Flank = gold, Belly = goldLight,
                FlankStart = 0.3f,
                Caudal = new DetailedFish.Tail { Shape = DetailedFish.TailShape.Forked, XTip = 1, XBase = 13, Span = 11f, Base = gold, Ray = goldDark, Edge = goldLight },
                EyeX = 55, EyeYOffset = 2.5f, EyeRadius = 2, Iris = C(0.20f, 0.50f, 0.62f), Pupil = C(0.06f, 0.06f, 0.08f),
                GillX = 50, GillStrength = 0.45f, MouthXStart = 59, MouthXEnd = 61, MouthYOffset = -0.5f,
                OutlineColor = C(0.30f, 0.18f, 0.04f),
                Seed = 91,
            };
            fish.Fins.Add(new DetailedFish.Fin { Side = DetailedFish.FinSide.Up, XRear = 26, XFront = 42, HeightRear = 4f, HeightFront = 8f, Shape = DetailedFish.FinShape.Falcate, Base = gold, Ray = goldDark, Edge = goldLight });
            fish.Fins.Add(new DetailedFish.Fin { Side = DetailedFish.FinSide.Down, XRear = 22, XFront = 30, HeightRear = 2f, HeightFront = 5f, Base = gold, Ray = goldDark, Edge = goldLight });
            fish.Fins.Add(new DetailedFish.Fin { Side = DetailedFish.FinSide.Down, XRear = 38, XFront = 43, HeightRear = 1f, HeightFront = 5f, Base = gold, Ray = goldDark, Edge = goldLight });
            fish.PectoralFins.Add(new DetailedFish.Pectoral { BaseX = 48, BaseYOffset = -2f, Length = 7, Width = 2.5f, Slope = 0.3f, Base = goldLight, Ray = gold, Edge = C(1f, 0.97f, 0.82f) });

            Color[,] drawn = fish.Draw();
            int w = drawn.GetLength(0);
            int h = drawn.GetLength(1);
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (drawn[x, y].a > 0.5f)
                    {
                        PixelArt.Set(p, cx - w / 2 + x, cy - h / 2 + y, drawn[x, y]);
                    }
                }
            }

            // Trois petites bulles dorées devant la bouche.
            PixelArt.Set(p, cx + w / 2 + 2, cy + 3, goldLight);
            PixelArt.Set(p, cx + w / 2 + 4, cy + 7, goldLight);
            PixelArt.Set(p, cx + w / 2 + 3, cy + 11, gold);
        }

        /// <summary>Le bassin géant : cadre, eau, sable, rochers, coraux, algues, coffre, bulles.</summary>
        private static void DrawTank(Color[,] p)
        {
            int left = TankLeft + Frame;
            int right = TankRight - Frame;
            int bottom = TankBottom + Frame;

            PixelArt.Rect(p, TankLeft, TankBottom, TankRight, TankTop, Metal);
            PixelArt.Rect(p, TankLeft, TankTop - 1, TankRight, TankTop, MetalLight);

            for (int x = left; x <= right; x++)
            {
                for (int y = bottom; y <= TankTop - Frame; y++)
                {
                    Color c;
                    if (y > WaterLine)
                    {
                        c = C(0.20f, 0.30f, 0.38f);
                    }
                    else
                    {
                        float t = Mathf.InverseLerp(bottom, WaterLine, y);
                        c = Color.Lerp(WaterDeep, WaterTop, Step(t, 8f));
                        if ((x + y / 2) % 46 < 7)
                        {
                            c = Color.Lerp(c, Color.white, 0.12f * t);
                        }

                        if (y == WaterLine)
                        {
                            c = Color.Lerp(WaterTop, Color.white, x % 8 < 4 ? 0.55f : 0.3f);
                        }
                    }

                    p[x, y] = c;
                }
            }

            // Sable ondulé.
            for (int x = left; x <= right; x++)
            {
                int top = bottom + 7 + Mathf.RoundToInt(Mathf.Sin(x * 0.07f) * 2.5f);
                for (int y = bottom; y <= top; y++)
                {
                    p[x, y] = (x * 7 + y * 3) % 11 == 0 ? SandDark : Sand;
                }

                p[x, top] = Color.Lerp(Sand, Color.white, 0.3f);
            }

            int floor = bottom + 6;
            DrawRock(p, 70, floor, 16, 10);
            DrawRock(p, 205, floor, 11, 7);
            DrawRock(p, 300, floor, 20, 13);
            DrawRock(p, 420, floor, 12, 8);
            DrawChest(p, 352, floor);
            DrawCoral(p, 40, floor, CoralPink, 18);
            DrawCoral(p, 128, floor, CoralOrange, 14);
            DrawCoral(p, 256, floor, CoralPink, 12);
            DrawCoral(p, 395, floor, CoralOrange, 16);
            DrawBrainCoral(p, 160, floor, CoralPurple, 9);
            DrawBrainCoral(p, 330, floor, C(0.95f, 0.75f, 0.45f), 7);
            DrawBrainCoral(p, 445, floor, CoralPurple, 6);
            foreach (int wx in new[] { 30, 55, 100, 145, 182, 228, 270, 318, 372, 410, 440 })
            {
                DrawWeed(p, wx, floor, 24 + (wx * 7) % 30);
            }

            // Les bulles sont animées en jeu (TankLife).
        }

        private static void DrawChest(Color[,] p, int x0, int y0)
        {
            PixelArt.Rect(p, x0, y0, x0 + 16, y0 + 9, Wood);
            PixelArt.Rect(p, x0, y0 + 10, x0 + 16, y0 + 13, Color.Lerp(Wood, Color.white, 0.15f));
            PixelArt.Rect(p, x0, y0 + 9, x0 + 16, y0 + 9, BrassDark);
            PixelArt.Rect(p, x0 + 7, y0 + 6, x0 + 9, y0 + 10, Brass);
            PixelArt.Set(p, x0 + 3, y0 + 14, FishYellow);   // pièces qui dépassent
            PixelArt.Set(p, x0 + 12, y0 + 14, FishYellow);
            PixelArt.Set(p, x0 + 2, y0 + 1, Color.Lerp(Wood, Color.black, 0.3f));
        }

        private static void DrawRock(Color[,] p, int cx, int baseY, int rx, int ry)
        {
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                for (int y = baseY; y <= baseY + ry; y++)
                {
                    float u = (x - cx) / (float)rx;
                    float v = (y - baseY) / (float)ry;
                    if (u * u + v * v > 1f)
                    {
                        continue;
                    }

                    float light = 0.75f + 0.45f * Mathf.Max(0f, -u * 0.5f + v * 0.8f);
                    Color c = (x * 5 + y * 3) % 13 == 0 ? Color.Lerp(Rock, Weed, 0.4f) : Rock;
                    PixelArt.Set(p, x, y, DetailedFish.Shade(c, Step(light, 5f)));
                }
            }
        }

        private static void DrawCoral(Color[,] p, int x, int baseY, Color color, int height)
        {
            Color dark = Color.Lerp(color, Color.black, 0.25f);
            Color tip = Color.Lerp(color, Color.white, 0.4f);
            for (int k = 0; k < height; k++)
            {
                PixelArt.Set(p, x, baseY + k, k < 4 ? dark : color);
            }

            PixelArt.Set(p, x, baseY + height, tip);
            for (int branch = 0; branch < 3; branch++)
            {
                int start = 3 + branch * height / 4;
                int length = height / 2 - branch;
                int side = branch % 2 == 0 ? -1 : 1;
                for (int k = 1; k <= length; k++)
                {
                    PixelArt.Set(p, x + side * (k / 2 + 1), baseY + start + k, color);
                }

                PixelArt.Set(p, x + side * (length / 2 + 1), baseY + start + length + 1, tip);
            }
        }

        private static void DrawBrainCoral(Color[,] p, int cx, int baseY, Color color, int radius)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                for (int y = baseY; y <= baseY + radius; y++)
                {
                    float u = (x - cx) / (float)radius;
                    float v = (y - baseY) / (float)radius;
                    if (u * u + v * v > 1f)
                    {
                        continue;
                    }

                    Color c = (x + y * 2) % 4 == 0 ? Color.Lerp(color, Color.black, 0.25f) : color;
                    PixelArt.Set(p, x, y, DetailedFish.Shade(c, v > 0.6f ? 1.15f : u < 0f ? 1f : 0.85f));
                }
            }
        }

        private static void DrawWeed(Color[,] p, int x, int baseY, int height)
        {
            for (int k = 0; k < height; k++)
            {
                int sway = Mathf.RoundToInt(Mathf.Sin(k * 0.3f + x) * 2f);
                PixelArt.Set(p, x + sway, baseY + k, k % 3 == 0 ? WeedDark : Weed);
                if (k % 6 == 3)
                {
                    PixelArt.Set(p, x + sway + 1, baseY + k, Weed);
                    PixelArt.Set(p, x + sway + 2, baseY + k + 1, Color.Lerp(Weed, Color.white, 0.2f));
                }
            }
        }

        // ======================================================================
        // Vitre du bassin (par-dessus les poissons)
        // ======================================================================

        /// <summary>
        /// Vitre du bassin : cadre (recouvre les poissons près des bords), reflets obliques transparents, montants.
        /// Même taille et même origine que la salle.
        /// </summary>
        public static Color[,] TankGlass()
        {
            var p = new Color[InteriorWidth, InteriorHeight];
            int left = TankLeft + Frame;
            int right = TankRight - Frame;
            int bottom = TankBottom + Frame;

            for (int x = TankLeft; x <= TankRight; x++)
            {
                for (int y = TankBottom; y <= TankTop; y++)
                {
                    bool inside = x >= left && x <= right && y >= bottom && y <= TankTop - Frame;
                    if (!inside)
                    {
                        p[x, y] = y >= TankTop - 1 ? MetalLight : x < left ? MetalLight : Metal;
                    }
                }
            }

            for (int x = left; x <= right; x++)
            {
                for (int y = bottom; y <= WaterLine; y++)
                {
                    int band = (x - y + 900) % 96;
                    if (band < 4 || band == 8)
                    {
                        p[x, y] = C(1f, 1f, 1f, band == 8 ? 0.10f : 0.15f);
                    }
                }
            }

            foreach (int x in new[] { 130, 240, 350 })
            {
                for (int y = bottom; y <= TankTop - Frame; y++)
                {
                    p[x, y] = C(0.13f, 0.24f, 0.30f, 0.9f);
                    p[x + 1, y] = C(0.30f, 0.46f, 0.52f, 0.9f);
                }
            }

            return p;
        }

        // ======================================================================
        // Mobilier
        // ======================================================================

        /// <summary>Colonne à méduses 48×88 : cylindre de verre cerclé de laiton, eau bleue, méduses roses. Pivot au pied.</summary>
        public static Color[,] JellyfishColumn()
        {
            const int width = 48;
            const int height = 88;
            var p = new Color[width, height];
            const float cx = 23.5f;

            // Socle et chapeau en laiton (ellipses vues de 3/4).
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float u = (x + 0.5f - cx) / 22f;
                    bool body = Mathf.Abs(u) <= 1f && y >= 8 && y <= 76;
                    bool baseRing = Mathf.Abs(u) <= 1.05f && y <= 10;
                    bool cap = Mathf.Abs(u) <= 1.05f && y >= 76 && y <= 84;
                    float light = Step(1.12f - (u + 1f) * 0.25f, 5f);
                    if (baseRing || cap)
                    {
                        p[x, y] = DetailedFish.Shade(Brass, (y == 10 || y == 84) ? 1.2f : light);
                    }
                    else if (body)
                    {
                        float t = Mathf.InverseLerp(8f, 76f, y);
                        Color c = Color.Lerp(C(0.10f, 0.20f, 0.42f), C(0.36f, 0.62f, 0.90f), Step(t, 6f));
                        c = DetailedFish.Shade(c, light);
                        if (Mathf.Abs(u + 0.55f) < 0.06f)
                        {
                            c = Color.Lerp(c, Color.white, 0.5f); // reflet du verre
                        }

                        p[x, y] = c;
                    }
                }
            }

            // Les méduses sont animées en jeu (TankLife) : la colonne n'a que l'eau et quelques bulles.
            for (int y = 14; y < 74; y += 9)
            {
                PixelArt.Set(p, 34 + (y / 9) % 2, y, Bubble);
            }

            PixelArt.SelectiveOutline(p, Outline, 0.6f);
            return p;
        }

        private static void DrawJellyfish(Color[,] p, int cx, int cy, Color color)
        {
            // Ombrelle en dôme, translucide.
            for (int x = cx - 5; x <= cx + 5; x++)
            {
                for (int y = cy; y <= cy + 5; y++)
                {
                    float u = (x - cx) / 5f;
                    float v = (y - cy) / 5f;
                    if (u * u + v * v <= 1f)
                    {
                        Color c = v > 0.5f ? Color.Lerp(color, Color.white, 0.35f) : color;
                        PixelArt.Set(p, x, y, Color.Lerp(p[x, y], c, 0.8f));
                    }
                }
            }

            // Tentacules ondulants.
            for (int t = -4; t <= 4; t += 2)
            {
                for (int k = 1; k <= 8; k++)
                {
                    int sway = Mathf.RoundToInt(Mathf.Sin(k * 0.8f + t) * 1f);
                    PixelArt.Set(p, cx + t + sway, cy - k, Color.Lerp(p[cx + t + sway, cy - k], color, 0.6f));
                }
            }
        }

        /// <summary>Banc en bois 64×28, face au bassin (vu de dos). Pivot au pied.</summary>
        public static Color[,] Bench()
        {
            const int width = 64;
            const int height = 28;
            var p = new Color[width, height];

            // Pieds en fer forgé.
            foreach (int x in new[] { 6, 55 })
            {
                PixelArt.Rect(p, x, 0, x + 2, 14, Metal);
                PixelArt.Rect(p, x - 1, 0, x + 3, 1, Metal);
            }

            // Assise et dossier en lattes.
            for (int y = 10; y <= 25; y++)
            {
                if (y == 15 || y == 16)
                {
                    continue; // jour entre l'assise et le dossier
                }

                for (int x = 3; x <= 60; x++)
                {
                    float light = y >= 24 || y == 14 ? 1.15f : (y % 3 == 0 ? 0.8f : 1f);
                    p[x, y] = DetailedFish.Shade(Wood, light - (x > 50 ? 0.1f : 0f));
                }
            }

            PixelArt.Rect(p, 4, 15, 5, 16, Metal);
            PixelArt.Rect(p, 58, 15, 59, 16, Metal);
            PixelArt.SelectiveOutline(p, Outline, 0.6f);
            return p;
        }

        // ======================================================================
        // Icône minimap
        // ======================================================================

        /// <summary>Petit aquarium : socle blanc, porte, dôme d'eau avec un poisson orange.</summary>
        public static Color[,] Icon()
        {
            var p = new Color[16, 16];
            PixelArt.Rect(p, 2, 1, 13, 6, Plaster);
            PixelArt.Rect(p, 2, 1, 13, 2, Tile);
            PixelArt.Rect(p, 7, 1, 8, 5, Metal);
            for (int x = 1; x <= 14; x++)
            {
                for (int y = 7; y <= 14; y++)
                {
                    float u = (x + 0.5f - 8f) / 6.6f;
                    float v = (y - 6.5f) / 7.5f;
                    if (u * u + v * v <= 1f)
                    {
                        p[x, y] = Color.Lerp(WaterDeep, WaterTop, Mathf.Clamp01(v + 0.2f));
                    }
                }
            }

            PixelArt.Rect(p, 2, 7, 13, 7, MetalLight);
            DrawFish(p, 8, 10, FishOrange, 0, false);
            PixelArt.Set(p, 5, 12, Color.white);
            PixelArt.Outline(p, Outline);
            return p;
        }

        // ======================================================================
        // Petits motifs
        // ======================================================================

        /// <summary>
        /// Poisson stylisé, tête à droite (à gauche si <paramref name="flip"/>). <paramref name="size"/> : 0 = 7×3,
        /// 1 = 11×5, 2 = 15×7, 3 = 23×11.
        /// </summary>
        private static void DrawFish(Color[,] p, int cx, int cy, Color color, int size, bool flip)
        {
            int halfLength = size == 0 ? 2 : size == 1 ? 4 : size == 2 ? 6 : 9;
            int halfHeight = size == 0 ? 1 : size == 1 ? 2 : size == 2 ? 3 : 5;
            int d = flip ? -1 : 1;
            Color dark = Color.Lerp(color, Color.black, 0.25f);
            Color light = Color.Lerp(color, Color.white, 0.3f);

            for (int k = -halfLength; k <= halfLength; k++)
            {
                float t = k / (float)(halfLength + 1);
                int h = Mathf.RoundToInt(halfHeight * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t)));
                for (int y = -h; y <= h; y++)
                {
                    Color c = y > h / 2 ? light : y < -h / 2 ? dark : color;
                    PixelArt.Set(p, cx + k * d, cy + y, c);
                }
            }

            // Queue en V.
            int tailX = cx - (halfLength + 1) * d;
            for (int k = 0; k <= Mathf.Max(1, halfHeight); k++)
            {
                PixelArt.Set(p, tailX - k * d, cy + k, color);
                PixelArt.Set(p, tailX - k * d, cy - k, dark);
            }

            PixelArt.Set(p, cx + (halfLength - 1) * d, cy + (halfHeight > 1 ? 1 : 0), Color.Lerp(color, Color.black, 0.75f)); // œil
        }
    }
}
