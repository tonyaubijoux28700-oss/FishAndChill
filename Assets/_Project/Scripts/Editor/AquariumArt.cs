using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Pixel art de l'aquarium (16 px par case) : façade 96×80 (6 cases de large) coiffée d'un grand dôme de verre
    /// plein d'eau, intérieur 192×128 (12×8 cases) avec le bassin sur le mur du fond, vitre du bassin (posée
    /// par-dessus les poissons) et icône 16×16 pour la minimap.
    /// </summary>
    public static class AquariumArt
    {
        public const int ExteriorWidth = 96;
        public const int ExteriorHeight = 80;
        public static readonly Vector2 ExteriorPivot = new Vector2(0.5f, 3f / ExteriorHeight);

        public const int InteriorWidth = 192;
        public const int InteriorHeight = 128;

        // Le bassin, en pixels de l'intérieur (bas-gauche = coin de la pièce).
        private const int TankLeft = 14;
        private const int TankRight = 177;
        private const int TankBottom = 78;
        private const int TankTop = 124;
        private const int Frame = 3;
        private const int WaterLine = 118;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Outline = C(0.07f, 0.10f, 0.14f);
        private static readonly Color Plaster = C(0.95f, 0.93f, 0.87f);
        private static readonly Color Tile = C(0.18f, 0.56f, 0.62f);
        private static readonly Color TileDark = C(0.12f, 0.42f, 0.48f);
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
        private static readonly Color Rock = C(0.44f, 0.50f, 0.58f);
        private static readonly Color Bubble = C(0.86f, 0.97f, 1.00f);
        private static readonly Color FishOrange = C(1.00f, 0.56f, 0.22f);
        private static readonly Color FishYellow = C(1.00f, 0.86f, 0.30f);

        // ---------- Façade ----------

        public static Color[,] Exterior()
        {
            var p = new Color[ExteriorWidth, ExteriorHeight];

            // Paillasson (l'ombre au sol est ajoutée après le contour).
            PixelArt.Rect(p, 39, 0, 56, 2, Tile);
            PixelArt.Rect(p, 41, 1, 54, 1, C(0.40f, 0.78f, 0.80f));

            // Murs enduits, éclairés depuis la gauche, soubassement en carreaux turquoise.
            for (int x = 4; x <= 91; x++)
            {
                float side = Mathf.InverseLerp(4f, 91f, x);
                for (int y = 3; y <= 41; y++)
                {
                    Color c = DetailedFish.Shade(Plaster, Mathf.Round((1.08f - side * 0.25f) * 6f) / 6f);
                    if (y <= 9)
                    {
                        c = ((x / 6) + (y / 3)) % 2 == 0 ? Tile : TileDark;
                        if (y == 9) c = Color.Lerp(c, Color.white, 0.25f);
                    }

                    p[x, y] = c;
                }
            }

            // Corniche ondulée bleue sous le dôme.
            for (int x = 3; x <= 92; x++)
            {
                int wave = Mathf.RoundToInt(Mathf.Sin(x * 0.5f) * 1f);
                for (int y = 38 + wave; y <= 43; y++)
                {
                    p[x, y] = y >= 42 ? MetalLight : Tile;
                }
            }

            DrawPorthole(p, 20, 23, 9, FishOrange, flip: false);
            DrawPorthole(p, 75, 23, 9, FishYellow, flip: true);
            DrawArchDoor(p);

            // Enseigne en laiton au-dessus de la porte, avec un poisson.
            PixelArt.Rect(p, 37, 32, 58, 37, BrassDark);
            PixelArt.Rect(p, 38, 33, 57, 36, Brass);
            DrawTinyFish(p, 47, 34, Metal, flip: false);

            DrawDome(p);

            PixelArt.SelectiveOutline(p, Outline, 0.6f);

            // Ombre douce au sol, seulement là où il n'y a rien.
            for (int x = 1; x <= 94; x++)
            {
                for (int y = 0; y <= 3; y++)
                {
                    if (!PixelArt.IsFilled(p, x, y))
                    {
                        p[x, y] = C(0.05f, 0.15f, 0.08f, 0.30f);
                    }
                }
            }

            return p;
        }

        private static void DrawArchDoor(Color[,] p)
        {
            const float cx = 47.5f;
            for (int x = 38; x <= 57; x++)
            {
                for (int y = 3; y <= 31; y++)
                {
                    float dx = x - cx;
                    float archY = 22f + Mathf.Sqrt(Mathf.Max(0f, 90f - dx * dx));
                    if (y > archY)
                    {
                        continue;
                    }

                    bool frame = Mathf.Abs(dx) > 8f || y > archY - 2f || y <= 4;
                    Color c = frame ? Metal : Color.Lerp(WaterDeep, WaterTop, Mathf.InverseLerp(5f, 30f, y));
                    if (!frame && Mathf.Abs(dx) < 0.6f)
                    {
                        c = Metal; // milieu de la double porte
                    }
                    else if (!frame && (x - y) % 9 == 0)
                    {
                        c = Color.Lerp(c, Color.white, 0.4f); // reflets sur la vitre
                    }

                    p[x, y] = c;
                }
            }

            PixelArt.Set(p, 45, 14, Brass);
            PixelArt.Set(p, 50, 14, Brass);
        }

        private static void DrawPorthole(Color[,] p, int cx, int cy, int radius, Color fish, bool flip)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                for (int y = cy - radius; y <= cy + radius; y++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d > radius + 0.3f)
                    {
                        continue;
                    }

                    if (d > radius - 2.2f)
                    {
                        // Cerclage en laiton, éclairé en haut à gauche.
                        bool lit = x - cx < 0 && y - cy > 0;
                        PixelArt.Set(p, x, y, lit ? Brass : BrassDark);
                    }
                    else
                    {
                        float t = Mathf.InverseLerp(cy - radius, cy + radius, y);
                        PixelArt.Set(p, x, y, Color.Lerp(WaterDeep, WaterTop, Mathf.Round(t * 4f) / 4f));
                    }
                }
            }

            DrawTinyFish(p, cx, cy - 1, fish, flip);
            PixelArt.Set(p, cx + (flip ? -4 : 4), cy + 3, Bubble);
            PixelArt.Set(p, cx + (flip ? -3 : 3), cy + 5, Bubble);
            PixelArt.Set(p, cx - 4, cy + 4, Color.Lerp(WaterTop, Color.white, 0.6f)); // reflet
            PixelArt.Set(p, cx - 3, cy + 5, Color.Lerp(WaterTop, Color.white, 0.6f));
        }

        /// <summary>Grand dôme de verre plein d'eau, nervures métalliques, poissons, épi doré au sommet.</summary>
        private static void DrawDome(Color[,] p)
        {
            const float cx = 47.5f;
            const float baseY = 43.5f;
            const float rx = 41f;
            const float ry = 33f;
            for (int x = 0; x < ExteriorWidth; x++)
            {
                for (int y = 44; y < ExteriorHeight - 3; y++)
                {
                    float u = (x + 0.5f - cx) / rx;
                    float v = (y + 0.5f - baseY) / ry;
                    if (u * u + v * v > 1f)
                    {
                        continue;
                    }

                    float w = Mathf.Sqrt(Mathf.Max(0f, 1f - v * v));
                    Color c = Color.Lerp(WaterDeep, WaterTop, Mathf.Round(Mathf.Clamp01(v * 1.3f + 0.15f) * 5f) / 5f);

                    // Volume : bords du dôme plus sombres.
                    float edge = Mathf.Sqrt(u * u + v * v);
                    if (edge > 0.86f)
                    {
                        c = Color.Lerp(c, WaterDeep, 0.45f);
                    }

                    // Grand reflet en haut à gauche.
                    if (Mathf.Abs(u + 0.45f) < 0.07f && v > 0.25f && v < 0.75f)
                    {
                        c = Color.Lerp(c, Color.white, 0.55f);
                    }

                    // Nervures : méridiens et parallèles.
                    bool rib = false;
                    foreach (float k in new[] { -0.66f, -0.33f, 0f, 0.33f, 0.66f })
                    {
                        rib |= Mathf.Abs(u - k * w) < 0.55f / rx;
                    }

                    foreach (float h in new[] { 0.4f, 0.75f })
                    {
                        rib |= Mathf.Abs(v - h) < 0.5f / ry;
                    }

                    if (rib)
                    {
                        c = u < 0f ? C(0.88f, 0.94f, 0.95f) : C(0.66f, 0.76f, 0.80f);
                    }

                    p[x, y] = c;
                }
            }

            // Habitants du dôme.
            DrawTinyFish(p, 30, 54, FishOrange, flip: false);
            DrawTinyFish(p, 60, 62, FishYellow, flip: true);
            DrawTinyFish(p, 66, 50, CoralPink, flip: true);
            PixelArt.Set(p, 40, 60, Bubble);
            PixelArt.Set(p, 41, 63, Bubble);
            PixelArt.Set(p, 40, 66, Bubble);

            // Épi doré au sommet.
            PixelArt.Rect(p, 46, 76, 49, 77, BrassDark);
            PixelArt.Rect(p, 47, 77, 48, 79, Brass);
            PixelArt.Set(p, 47, 79, C(1f, 0.95f, 0.70f));
        }

        /// <summary>Petit poisson de 7×3 (tête à droite, ou à gauche si <paramref name="flip"/>).</summary>
        private static void DrawTinyFish(Color[,] p, int cx, int cy, Color color, bool flip)
        {
            int d = flip ? -1 : 1;
            for (int k = -2; k <= 2; k++)
            {
                PixelArt.Set(p, cx + k * d, cy, color);
            }

            PixelArt.Set(p, cx - 1 * d, cy + 1, color);
            PixelArt.Set(p, cx, cy + 1, color);
            PixelArt.Set(p, cx + 1 * d, cy + 1, color);
            PixelArt.Set(p, cx - 1 * d, cy - 1, Color.Lerp(color, Color.black, 0.2f));
            PixelArt.Set(p, cx, cy - 1, Color.Lerp(color, Color.black, 0.2f));
            PixelArt.Set(p, cx - 3 * d, cy + 1, color);   // queue
            PixelArt.Set(p, cx - 3 * d, cy - 1, color);
            PixelArt.Set(p, cx + 1 * d, cy, Color.Lerp(color, Color.black, 0.7f)); // œil
        }

        // ---------- Intérieur ----------

        public static Color[,] Interior()
        {
            var p = new Color[InteriorWidth, InteriorHeight];

            // Sol de carreaux clairs, avec la lueur bleutée du bassin qui tombe dessus.
            Color tileA = C(0.80f, 0.86f, 0.88f);
            Color tileB = C(0.74f, 0.81f, 0.85f);
            Color grout = C(0.60f, 0.68f, 0.72f);
            for (int x = 0; x < InteriorWidth; x++)
            {
                for (int y = 0; y < TankBottom; y++)
                {
                    Color c = ((x / 16) + (y / 16)) % 2 == 0 ? tileA : tileB;
                    if (x % 16 == 0 || y % 16 == 0)
                    {
                        c = grout;
                    }

                    float glow = Mathf.InverseLerp(40f, TankBottom, y);
                    c = Color.Lerp(c, C(0.50f, 0.80f, 0.92f), Mathf.Round(glow * 3f) / 3f * 0.35f);

                    // Reflets mouvants de l'eau (caustiques) près du bassin.
                    if (y > 50 && (x * 3 + y * 5) % 23 == 0)
                    {
                        c = Color.Lerp(c, Color.white, 0.35f);
                    }

                    p[x, y] = c;
                }
            }

            // Mur du fond, bleu nuit.
            for (int x = 0; x < InteriorWidth; x++)
            {
                for (int y = TankBottom; y < InteriorHeight; y++)
                {
                    p[x, y] = (x % 24 == 0) ? C(0.10f, 0.18f, 0.25f) : C(0.13f, 0.23f, 0.31f);
                }
            }

            // Meuble du bassin (plinthe) et plaque en laiton au milieu.
            PixelArt.Rect(p, TankLeft - 2, 72, TankRight + 2, TankBottom - 1, Metal);
            PixelArt.Rect(p, TankLeft - 2, TankBottom - 1, TankRight + 2, TankBottom - 1, MetalLight);
            PixelArt.Rect(p, 86, 73, 105, 76, BrassDark);
            PixelArt.Rect(p, 87, 74, 104, 75, Brass);

            DrawTank(p);

            // Murs latéraux.
            PixelArt.Rect(p, 0, 0, 5, InteriorHeight - 1, C(0.10f, 0.18f, 0.25f));
            PixelArt.Rect(p, InteriorWidth - 6, 0, InteriorWidth - 1, InteriorHeight - 1, C(0.10f, 0.18f, 0.25f));

            // Paillasson de sortie.
            PixelArt.Rect(p, 80, 0, 111, 7, Tile);
            PixelArt.Rect(p, 82, 2, 109, 5, C(0.40f, 0.78f, 0.80f));
            return p;
        }

        /// <summary>Le bassin : cadre, eau en dégradé avec rayons de lumière, sable, rochers, coraux, algues, bulles.</summary>
        private static void DrawTank(Color[,] p)
        {
            int left = TankLeft + Frame;
            int right = TankRight - Frame;
            int bottom = TankBottom + Frame;

            // Cadre.
            PixelArt.Rect(p, TankLeft, TankBottom, TankRight, TankTop, Metal);
            PixelArt.Rect(p, TankLeft, TankTop - 1, TankRight, TankTop, MetalLight);

            for (int x = left; x <= right; x++)
            {
                for (int y = bottom; y <= TankTop - Frame; y++)
                {
                    Color c;
                    if (y > WaterLine)
                    {
                        c = C(0.20f, 0.30f, 0.38f); // air au-dessus de l'eau
                    }
                    else
                    {
                        float t = Mathf.InverseLerp(bottom, WaterLine, y);
                        c = Color.Lerp(WaterDeep, WaterTop, Mathf.Round(t * 6f) / 6f);

                        // Rayons de lumière obliques, plus forts près de la surface.
                        if ((x + y / 2) % 30 < 5)
                        {
                            c = Color.Lerp(c, Color.white, 0.12f * t);
                        }

                        if (y == WaterLine)
                        {
                            c = Color.Lerp(WaterTop, Color.white, x % 6 < 3 ? 0.55f : 0.3f);
                        }
                    }

                    p[x, y] = c;
                }
            }

            // Sable ondulé.
            for (int x = left; x <= right; x++)
            {
                int top = bottom + 4 + Mathf.RoundToInt(Mathf.Sin(x * 0.12f) * 1.5f);
                for (int y = bottom; y <= top; y++)
                {
                    p[x, y] = (x * 7 + y * 3) % 11 == 0 ? SandDark : Sand;
                }

                p[x, top] = Color.Lerp(Sand, Color.white, 0.3f);
            }

            DrawRock(p, 52, bottom + 4, 9, 5);
            DrawRock(p, 132, bottom + 4, 12, 6);
            DrawRock(p, 160, bottom + 4, 6, 4);
            DrawCoral(p, 36, bottom + 5, CoralPink);
            DrawCoral(p, 112, bottom + 5, CoralOrange);
            DrawBrainCoral(p, 82, bottom + 5, C(0.75f, 0.55f, 0.85f));
            DrawWeed(p, 24, bottom + 5, 26);
            DrawWeed(p, 64, bottom + 5, 18);
            DrawWeed(p, 98, bottom + 5, 22);
            DrawWeed(p, 150, bottom + 5, 28);
            DrawWeed(p, 170, bottom + 5, 16);

            // Colonnes de bulles.
            foreach (int bx in new[] { 58, 140 })
            {
                for (int y = bottom + 10; y < WaterLine; y += 6)
                {
                    PixelArt.Set(p, bx + ((y / 6) % 2), y, Bubble);
                }
            }
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
                    PixelArt.Set(p, x, y, DetailedFish.Shade(Rock, Mathf.Round(light * 5f) / 5f));
                }
            }
        }

        private static void DrawCoral(Color[,] p, int x, int baseY, Color color)
        {
            // Corail branchu : un tronc et des branches qui montent en Y.
            Color dark = Color.Lerp(color, Color.black, 0.25f);
            for (int k = 0; k < 10; k++)
            {
                PixelArt.Set(p, x, baseY + k, k < 3 ? dark : color);
            }

            for (int k = 1; k <= 6; k++)
            {
                PixelArt.Set(p, x - k / 2 - 1, baseY + 4 + k, color);
                PixelArt.Set(p, x + k / 2 + 1, baseY + 3 + k, color);
            }

            PixelArt.Set(p, x - 4, baseY + 10, Color.Lerp(color, Color.white, 0.4f));
            PixelArt.Set(p, x + 4, baseY + 9, Color.Lerp(color, Color.white, 0.4f));
            PixelArt.Set(p, x, baseY + 10, Color.Lerp(color, Color.white, 0.4f));
        }

        private static void DrawBrainCoral(Color[,] p, int cx, int baseY, Color color)
        {
            for (int x = cx - 6; x <= cx + 6; x++)
            {
                for (int y = baseY; y <= baseY + 6; y++)
                {
                    float u = (x - cx) / 6f;
                    float v = (y - baseY) / 6f;
                    if (u * u + v * v > 1f)
                    {
                        continue;
                    }

                    Color c = (x + y * 2) % 4 == 0 ? Color.Lerp(color, Color.black, 0.25f) : color;
                    PixelArt.Set(p, x, y, DetailedFish.Shade(c, v > 0.6f ? 1.15f : 0.9f));
                }
            }
        }

        private static void DrawWeed(Color[,] p, int x, int baseY, int height)
        {
            for (int k = 0; k < height; k++)
            {
                int sway = Mathf.RoundToInt(Mathf.Sin(k * 0.35f + x) * 1.5f);
                PixelArt.Set(p, x + sway, baseY + k, k % 3 == 0 ? WeedDark : Weed);
                if (k % 5 == 2)
                {
                    PixelArt.Set(p, x + sway + 1, baseY + k, Weed);
                }
            }
        }

        /// <summary>
        /// Vitre du bassin, posée par-dessus les poissons : cadre, reflets obliques transparents et surface de l'eau.
        /// Même taille et même origine que l'intérieur.
        /// </summary>
        public static Color[,] TankGlass()
        {
            var p = new Color[InteriorWidth, InteriorHeight];
            int left = TankLeft + Frame;
            int right = TankRight - Frame;
            int bottom = TankBottom + Frame;

            // Cadre (recouvre les poissons qui s'approchent des bords).
            for (int x = TankLeft; x <= TankRight; x++)
            {
                for (int y = TankBottom; y <= TankTop; y++)
                {
                    bool inside = x >= left && x <= right && y >= bottom && y <= TankTop - Frame;
                    if (!inside)
                    {
                        p[x, y] = y >= TankTop - 1 ? MetalLight : Metal;
                    }
                }
            }

            // Reflets sur la vitre, très légers.
            for (int x = left; x <= right; x++)
            {
                for (int y = bottom; y <= WaterLine; y++)
                {
                    int band = (x - y + 400) % 70;
                    if (band < 3 || band == 6)
                    {
                        p[x, y] = C(1f, 1f, 1f, band == 6 ? 0.10f : 0.16f);
                    }
                }
            }

            // Montants verticaux de la vitre.
            foreach (int x in new[] { 68, 123 })
            {
                for (int y = bottom; y <= TankTop - Frame; y++)
                {
                    p[x, y] = C(0.13f, 0.24f, 0.30f, 0.85f);
                }
            }

            return p;
        }

        // ---------- Icône minimap ----------

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
            DrawTinyFish(p, 8, 10, FishOrange, flip: false);
            PixelArt.Set(p, 5, 12, Color.white);
            PixelArt.Outline(p, Outline);
            return p;
        }
    }
}
