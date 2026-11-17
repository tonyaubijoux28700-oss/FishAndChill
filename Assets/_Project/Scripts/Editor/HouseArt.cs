using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Pixel art de la maison du joueur (16 px par case, y depuis le bas) : façade 80×80 (5 cases de large, enduit
    /// blanc à colombages, toit de tuiles rouges, cheminée, volets bleus, jardinières), intérieur 144×112 (9×7 cases :
    /// parquet, papier peint, fenêtre sur la mer, lit, tapis, table, coffre, plante) et icône 16×16 pour la minimap.
    /// </summary>
    public static class HouseArt
    {
        public const int ExteriorWidth = 80;
        public const int ExteriorHeight = 80;
        public static readonly Vector2 ExteriorPivot = new Vector2(0.5f, 3f / ExteriorHeight);

        public const int InteriorWidth = 144;
        public const int InteriorHeight = 112;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Outline = C(0.12f, 0.08f, 0.07f);
        private static readonly Color Plaster = C(0.97f, 0.93f, 0.84f);
        private static readonly Color PlasterShade = C(0.88f, 0.82f, 0.72f);
        private static readonly Color Beam = C(0.48f, 0.31f, 0.19f);
        private static readonly Color BeamDark = C(0.36f, 0.22f, 0.13f);
        private static readonly Color Tile = C(0.84f, 0.32f, 0.24f);
        private static readonly Color TileDark = C(0.62f, 0.20f, 0.16f);
        private static readonly Color TileLight = C(0.95f, 0.48f, 0.36f);
        private static readonly Color Brick = C(0.66f, 0.32f, 0.24f);
        private static readonly Color Stone = C(0.66f, 0.64f, 0.60f);
        private static readonly Color Shutter = C(0.28f, 0.50f, 0.78f);
        private static readonly Color ShutterDark = C(0.18f, 0.34f, 0.58f);
        private static readonly Color Glass = C(0.62f, 0.86f, 0.94f);
        private static readonly Color GlassDark = C(0.38f, 0.62f, 0.80f);
        private static readonly Color Wood = C(0.62f, 0.40f, 0.22f);
        private static readonly Color WoodDark = C(0.44f, 0.27f, 0.14f);
        private static readonly Color WoodLight = C(0.74f, 0.52f, 0.30f);
        private static readonly Color Gold = C(1.00f, 0.82f, 0.30f);
        private static readonly Color Leaf = C(0.32f, 0.62f, 0.30f);
        private static readonly Color LeafDark = C(0.20f, 0.44f, 0.22f);

        // ---------- Façade ----------

        public static Color[,] Exterior()
        {
            var p = new Color[ExteriorWidth, ExteriorHeight];

            // Ombre au sol et paillasson.
            PixelArt.Rect(p, 2, 0, 77, 3, C(0.05f, 0.15f, 0.08f, 0.35f));
            PixelArt.Rect(p, 33, 0, 46, 2, C(0.70f, 0.52f, 0.30f));
            PixelArt.Rect(p, 34, 1, 45, 1, C(0.84f, 0.66f, 0.40f));

            // Murs : enduit, colombages, soubassement en pierre.
            for (int x = 4; x <= 75; x++)
            {
                for (int y = 3; y <= 38; y++)
                {
                    p[x, y] = (x * 7 + y * 13) % 23 == 0 ? PlasterShade : Plaster;
                }
            }

            for (int y = 34; y <= 38; y++)
            {
                for (int x = 4; x <= 75; x++)
                {
                    p[x, y] = PlasterShade;   // ombre sous l'avant-toit
                }
            }

            PixelArt.Rect(p, 4, 3, 75, 6, Stone);
            for (int x = 4; x <= 75; x += 6)
            {
                PixelArt.Rect(p, x, 3, x, 6, C(0.52f, 0.50f, 0.48f));
            }

            PixelArt.Rect(p, 4, 7, 6, 38, Beam);
            PixelArt.Rect(p, 73, 7, 75, 38, BeamDark);
            PixelArt.Rect(p, 4, 27, 75, 28, Beam);
            for (int k = 0; k < 8; k++)
            {
                PixelArt.Set(p, 8 + k, 29 + k / 2, Beam);       // croix de Saint-André
                PixelArt.Set(p, 71 - k, 29 + k / 2, BeamDark);
            }

            // Fenêtres à volets et jardinières.
            DrawWindow(p, 13, 12);
            DrawWindow(p, 53, 12);

            // Porte en bois cintrée, poignée, petite lanterne.
            PixelArt.Rect(p, 33, 4, 46, 25, WoodDark);
            for (int x = 35; x <= 44; x++)
            {
                for (int y = 5; y <= 24; y++)
                {
                    bool arch = y >= 22 && (x == 35 || x == 44) || y == 24 && (x == 36 || x == 43);
                    p[x, y] = arch ? WoodDark : (x - 35) % 3 == 0 ? Wood : WoodLight;
                }
            }

            PixelArt.Rect(p, 37, 17, 42, 21, Glass);
            PixelArt.Rect(p, 39, 17, 40, 21, WoodDark);
            PixelArt.Set(p, 42, 12, Gold);
            PixelArt.Rect(p, 48, 18, 50, 23, BeamDark);
            PixelArt.Rect(p, 48, 19, 50, 21, C(1f, 0.86f, 0.45f));

            // Toit de tuiles : rangées décalées, faîtière, bord de l'avant-toit sombre.
            for (int y = 39; y <= 70; y++)
            {
                int inset = (y - 39) * 2 / 3;
                int row = (y - 39) / 4;
                for (int x = inset; x < ExteriorWidth - inset; x++)
                {
                    int inRow = (y - 39) % 4;
                    Color c = inRow == 0 ? TileDark : inRow == 3 ? TileLight : Tile;
                    if ((x + (row % 2) * 3) % 6 == 0 && inRow != 0)
                    {
                        c = TileDark;
                    }

                    if (x > ExteriorWidth - inset - 4)
                    {
                        c = Color.Lerp(c, TileDark, 0.45f);   // côté droit à l'ombre
                    }

                    p[x, y] = c;
                }
            }

            for (int x = 0; x < ExteriorWidth; x++)
            {
                p[x, 39] = TileDark;
            }

            for (int x = 22; x < ExteriorWidth - 22; x++)
            {
                p[x, 70] = TileLight;
                p[x, 71] = TileLight;
            }

            // Cheminée en briques.
            for (int x = 54; x <= 61; x++)
            {
                for (int y = 60; y <= 77; y++)
                {
                    p[x, y] = (y % 3 == 0 || (x + (y / 3) * 2) % 4 == 0) ? C(0.52f, 0.24f, 0.18f) : Brick;
                }
            }

            PixelArt.Rect(p, 53, 76, 62, 78, C(0.40f, 0.36f, 0.34f));

            PixelArt.Outline(p, Outline);
            return p;
        }

        private static void DrawWindow(Color[,] p, int x0, int y0)
        {
            const int w = 14, h = 11;
            PixelArt.Rect(p, x0 - 1, y0 - 1, x0 + w, y0 + h, Beam);
            for (int x = x0; x < x0 + w; x++)
            {
                for (int y = y0; y < y0 + h; y++)
                {
                    Color c = Color.Lerp(GlassDark, Glass, (y - y0) / (float)h);
                    if (x - x0 - (y - y0) == 2 || x - x0 - (y - y0) == 3)
                    {
                        c = C(0.92f, 0.98f, 1f);   // reflet
                    }

                    p[x, y] = c;
                }
            }

            PixelArt.Rect(p, x0 + w / 2, y0, x0 + w / 2, y0 + h - 1, Beam);
            PixelArt.Rect(p, x0, y0 + h / 2, x0 + w - 1, y0 + h / 2, Beam);

            // Volets.
            PixelArt.Rect(p, x0 - 5, y0 - 1, x0 - 2, y0 + h, Shutter);
            PixelArt.Rect(p, x0 + w + 1, y0 - 1, x0 + w + 4, y0 + h, Shutter);
            for (int y = y0; y < y0 + h; y += 2)
            {
                PixelArt.Rect(p, x0 - 5, y, x0 - 2, y, ShutterDark);
                PixelArt.Rect(p, x0 + w + 1, y, x0 + w + 4, y, ShutterDark);
            }

            // Jardinière fleurie.
            PixelArt.Rect(p, x0 - 1, y0 - 4, x0 + w, y0 - 2, Wood);
            for (int x = x0 - 1; x <= x0 + w; x++)
            {
                p[x, y0 - 1] = x % 3 == 0 ? C(0.96f, 0.36f, 0.42f) : x % 3 == 1 ? Leaf : C(1f, 0.86f, 0.30f);
            }
        }

        // ---------- Intérieur ----------

        public static Color[,] Interior()
        {
            var p = new Color[InteriorWidth, InteriorHeight];

            // Parquet.
            for (int x = 0; x < InteriorWidth; x++)
            {
                for (int y = 0; y < 74; y++)
                {
                    int plank = y / 6;
                    Color c = y % 6 == 0 ? WoodDark : (plank % 2 == 0 ? WoodLight : C(0.70f, 0.48f, 0.27f));
                    if ((x + plank * 17) % 32 == 0)
                    {
                        c = WoodDark;   // joints décalés
                    }

                    p[x, y] = c;
                }
            }

            // Papier peint rayé, plinthe.
            for (int x = 0; x < InteriorWidth; x++)
            {
                for (int y = 74; y < InteriorHeight; y++)
                {
                    p[x, y] = (x / 4) % 2 == 0 ? C(0.80f, 0.90f, 0.86f) : C(0.74f, 0.85f, 0.81f);
                }

                PixelArt.Set(p, x, 74, WoodDark);
                PixelArt.Set(p, x, 75, Wood);
                PixelArt.Set(p, x, 76, Wood);
            }

            // Murs de côté (épaisseur sombre) et bas de la pièce.
            PixelArt.Rect(p, 0, 0, 3, InteriorHeight - 1, C(0.42f, 0.30f, 0.22f));
            PixelArt.Rect(p, InteriorWidth - 4, 0, InteriorWidth - 1, InteriorHeight - 1, C(0.42f, 0.30f, 0.22f));
            PixelArt.Rect(p, 0, 0, InteriorWidth - 1, 1, C(0.42f, 0.30f, 0.22f));
            PixelArt.Rect(p, 64, 0, 79, 3, C(0.70f, 0.52f, 0.30f));   // paillasson de la porte

            // Fenêtre sur la mer.
            PixelArt.Rect(p, 58, 84, 86, 106, Wood);
            for (int x = 60; x <= 84; x++)
            {
                for (int y = 86; y <= 104; y++)
                {
                    Color c = y > 95 ? Color.Lerp(C(0.62f, 0.84f, 0.96f), C(0.86f, 0.95f, 1f), (y - 95) / 9f) : Color.Lerp(C(0.10f, 0.46f, 0.66f), C(0.20f, 0.62f, 0.78f), (y - 86) / 9f);
                    if (y == 95 || (y < 95 && (x * 3 + y * 5) % 11 == 0))
                    {
                        c = C(0.80f, 0.94f, 1f);   // horizon et vagues
                    }

                    p[x, y] = c;
                }
            }

            PixelArt.Rect(p, 72, 86, 72, 104, Wood);
            PixelArt.Rect(p, 60, 95, 84, 95, Wood);

            // Étagère avec un poisson empaillé, cadre avec une carte.
            PixelArt.Rect(p, 96, 92, 124, 93, WoodDark);
            for (int x = 100; x <= 116; x++)
            {
                for (int y = 95; y <= 101; y++)
                {
                    float u = (x - 108) / 8f, v = (y - 98) / 3.4f;
                    if (u * u + v * v <= 1f) p[x, y] = C(0.42f, 0.62f, 0.86f);
                }
            }

            PixelArt.Rect(p, 117, 96, 120, 100, C(0.32f, 0.50f, 0.74f));
            PixelArt.Set(p, 104, 99, C(0.1f, 0.1f, 0.1f));
            PixelArt.Rect(p, 22, 86, 44, 104, WoodDark);
            PixelArt.Rect(p, 24, 88, 42, 102, C(0.94f, 0.88f, 0.70f));
            PixelArt.Rect(p, 28, 92, 36, 98, C(0.48f, 0.74f, 0.42f));   // une île sur la carte
            PixelArt.Set(p, 38, 94, C(0.86f, 0.24f, 0.22f));

            // Lit (tête contre le mur, à gauche) : cadre en bois, oreiller, couette à carreaux.
            PixelArt.Rect(p, 8, 38, 40, 74, WoodDark);
            PixelArt.Rect(p, 10, 40, 38, 72, Wood);
            PixelArt.Rect(p, 12, 62, 36, 70, C(0.97f, 0.97f, 0.95f));
            PixelArt.Rect(p, 12, 62, 36, 62, C(0.84f, 0.84f, 0.82f));
            for (int x = 11; x <= 37; x++)
            {
                for (int y = 41; y <= 60; y++)
                {
                    bool check = ((x - 11) / 5 + (y - 41) / 5) % 2 == 0;
                    p[x, y] = check ? C(0.86f, 0.28f, 0.28f) : C(0.97f, 0.92f, 0.84f);
                    if (y == 60) p[x, y] = C(0.97f, 0.97f, 0.95f);   // drap replié
                }
            }

            PixelArt.Rect(p, 8, 36, 40, 38, BeamDark);   // pied du lit

            // Tapis rond tressé au milieu.
            for (int x = 46; x <= 100; x++)
            {
                for (int y = 14; y <= 46; y++)
                {
                    float u = (x - 73) / 27f, v = (y - 30) / 16f;
                    float d = u * u + v * v;
                    if (d <= 1f)
                    {
                        int ring = (int)(Mathf.Sqrt(d) * 6f);
                        p[x, y] = ring % 2 == 0 ? C(0.30f, 0.56f, 0.70f) : C(0.92f, 0.82f, 0.56f);
                    }
                }
            }

            // Table ronde et deux tabourets, à droite.
            for (int x = 104; x <= 132; x++)
            {
                for (int y = 46; y <= 62; y++)
                {
                    float u = (x - 118) / 14f, v = (y - 55) / 7f;
                    if (u * u + v * v <= 1f) p[x, y] = v < -0.5f ? WoodDark : WoodLight;
                }
            }

            PixelArt.Rect(p, 116, 42, 120, 47, WoodDark);
            PixelArt.Rect(p, 112, 56, 116, 59, C(0.97f, 0.97f, 0.95f));   // une tasse et une assiette
            PixelArt.Rect(p, 121, 55, 125, 58, C(0.86f, 0.86f, 0.84f));
            foreach (int sx in new[] { 100, 134 })
            {
                for (int x = sx - 3; x <= sx + 3; x++)
                {
                    for (int y = 46; y <= 52; y++)
                    {
                        float u = (x - sx) / 3.5f, v = (y - 49) / 3.5f;
                        if (u * u + v * v <= 1f) p[x, y] = v < -0.3f ? WoodDark : Wood;
                    }
                }
            }

            // Coffre contre le mur, et une grande plante en pot dans le coin.
            PixelArt.Rect(p, 46, 62, 66, 76, WoodDark);
            PixelArt.Rect(p, 47, 63, 65, 75, Wood);
            PixelArt.Rect(p, 47, 70, 65, 70, WoodDark);
            PixelArt.Rect(p, 55, 66, 57, 69, Gold);
            PixelArt.Rect(p, 126, 64, 136, 74, C(0.76f, 0.42f, 0.28f));
            for (int k = 0; k < 9; k++)
            {
                float a = k / 8f * Mathf.PI;
                for (int r = 0; r < 12; r++)
                {
                    int x = 131 + Mathf.RoundToInt(Mathf.Cos(a) * r * 0.9f);
                    int y = 74 + Mathf.RoundToInt(Mathf.Sin(a) * r);
                    PixelArt.Set(p, x, y, r % 3 == 0 ? LeafDark : Leaf);
                }
            }

            return p;
        }

        // ---------- Icône de la minimap ----------

        public static Color[,] Icon()
        {
            var p = new Color[16, 16];
            PixelArt.Rect(p, 3, 1, 12, 7, Plaster);
            PixelArt.Rect(p, 7, 1, 8, 5, Wood);
            PixelArt.Rect(p, 4, 4, 5, 5, Glass);
            PixelArt.Rect(p, 10, 4, 11, 5, Glass);
            for (int y = 8; y <= 13; y++)
            {
                int inset = y - 8;
                PixelArt.Rect(p, 1 + inset, y, 14 - inset, y, y % 2 == 0 ? Tile : TileDark);
            }

            PixelArt.Rect(p, 11, 10, 12, 14, Brick);
            PixelArt.Outline(p, Outline);
            return p;
        }
    }
}
