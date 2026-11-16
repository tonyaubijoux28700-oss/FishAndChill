using System.IO;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Deuxième salle et vie de l'aquarium (pixel art provisoire) : salle du lac (lambris, parquet, bassin d'eau douce
    /// aux galets), sa vitre, plantes animées (3 images), bulle, rayon de lumière, petits poissons des bancs, reflets
    /// d'eau au sol, méduse qui pulse, et visiteurs (le pêcheur recoloré).
    /// </summary>
    public static class AquariumLifeArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        // Salle du lac : passage (2 cases) + salle (24 cases), même hauteur que le grand hall.
        public const int LakeWidth = 416;
        public const int LakeHeight = 288;
        private const int Room = 32;                 // début de la salle (après le passage)
        private const int TankLeft = Room + 20;
        private const int TankRight = LakeWidth - 21;
        private const int TankBottom = 180;
        private const int TankTop = 282;
        private const int Frame = 5;
        private const int WaterLine = 272;
        private const int StandBottom = 166;
        private const int PassageBottom = 51;
        private const int PassageTop = 96;

        private static readonly Color Panel = C(0.56f, 0.38f, 0.23f);
        private static readonly Color PanelDark = C(0.42f, 0.27f, 0.16f);
        private static readonly Color TankWood = C(0.30f, 0.20f, 0.12f);
        private static readonly Color TankWoodLight = C(0.48f, 0.34f, 0.20f);
        private static readonly Color Brass = C(0.86f, 0.68f, 0.30f);
        private static readonly Color BrassDark = C(0.62f, 0.45f, 0.16f);
        private static readonly Color FreshDeep = C(0.08f, 0.27f, 0.27f);
        private static readonly Color FreshTop = C(0.42f, 0.68f, 0.58f);

        private static float Noise(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 73856093 ^ y * 19349663 ^ seed * 83492791;
                h = (h << 13) ^ h;
                return ((h * (h * h * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647f;
            }
        }

        // ======================================================================
        // Salle du lac
        // ======================================================================

        /// <summary>Passage voûté puis salle du lac, 416×288 : parquet, tapis, lambris, grand bassin d'eau douce.</summary>
        public static Color[,] LakeRoom()
        {
            var p = new Color[LakeWidth, LakeHeight];

            // Parquet en lames, joints décalés, tapis vert au milieu.
            for (int x = 0; x < LakeWidth; x++)
            {
                for (int y = 0; y < StandBottom; y++)
                {
                    int row = y / 8;
                    int joint = (x + row * 37) % 64;
                    Color c = row % 2 == 0 ? C(0.70f, 0.52f, 0.34f) : C(0.65f, 0.47f, 0.30f);
                    if (y % 8 == 0 || joint == 0) c = C(0.42f, 0.29f, 0.17f);
                    else if (Noise(x, y, 5) > 0.92f) c = DetailedFish.Shade(c, 0.9f);

                    // Lueur verte du bassin sur le sol.
                    float glow = Mathf.InverseLerp(100f, StandBottom, y);
                    c = Color.Lerp(c, C(0.50f, 0.78f, 0.66f), Mathf.Round(glow * 4f) / 4f * 0.3f);
                    p[x, y] = c;
                }
            }

            DrawRug(p, 128, 36, 320, 120);

            // Mur du fond en lambris vertical, poutre en haut.
            for (int x = Room; x < LakeWidth; x++)
            {
                for (int y = StandBottom; y < LakeHeight; y++)
                {
                    p[x, y] = x % 12 == 0 ? PanelDark : DetailedFish.Shade(Panel, 0.95f + Noise(x / 12, 0, 3) * 0.1f);
                }
            }

            // Meuble du bassin et plaques en laiton.
            PixelArt.Rect(p, TankLeft - 4, StandBottom, TankRight + 4, TankBottom - 1, TankWood);
            PixelArt.Rect(p, TankLeft - 4, TankBottom - 2, TankRight + 4, TankBottom - 1, TankWoodLight);
            foreach (int px in new[] { TankLeft + 60, (TankLeft + TankRight) / 2, TankRight - 60 })
            {
                PixelArt.Rect(p, px - 14, StandBottom + 4, px + 14, StandBottom + 9, BrassDark);
                PixelArt.Rect(p, px - 13, StandBottom + 5, px + 13, StandBottom + 8, Brass);
            }

            DrawFreshTank(p);

            // Murs latéraux en bois sombre ; ouverture du passage à gauche.
            PixelArt.Rect(p, Room, 0, Room + 9, LakeHeight - 1, PanelDark);
            PixelArt.Rect(p, LakeWidth - 10, 0, LakeWidth - 1, LakeHeight - 1, PanelDark);
            PixelArt.Rect(p, LakeWidth - 10, 0, LakeWidth - 10, StandBottom, Panel);

            // Passage : murs au-dessus et au-dessous, sol en pierre, arche en laiton.
            PixelArt.Rect(p, 0, 0, Room + 9, PassageBottom - 1, C(0.10f, 0.12f, 0.12f));
            PixelArt.Rect(p, 0, PassageTop + 6, Room + 9, LakeHeight - 1, C(0.10f, 0.12f, 0.12f));
            for (int x = 0; x <= Room + 9; x++)
            {
                for (int y = PassageBottom; y <= PassageTop; y++)
                {
                    p[x, y] = (x / 8 + y / 8) % 2 == 0 ? C(0.74f, 0.72f, 0.66f) : C(0.68f, 0.66f, 0.60f);
                }
            }

            PixelArt.Rect(p, 0, PassageTop + 1, Room + 9, PassageTop + 4, Brass);
            PixelArt.Rect(p, 0, PassageTop + 5, Room + 9, PassageTop + 5, BrassDark);
            PixelArt.Rect(p, Room + 10, PassageBottom - 1, Room + 10, PassageTop + 4, Brass);
            return p;
        }

        private static void DrawRug(Color[,] p, int x0, int y0, int x1, int y1)
        {
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    int edge = Mathf.Min(Mathf.Min(x - x0, x1 - x), Mathf.Min(y - y0, y1 - y));
                    Color c = edge < 3 ? C(0.80f, 0.70f, 0.44f) : edge < 6 ? C(0.24f, 0.40f, 0.34f) : C(0.30f, 0.50f, 0.42f);
                    if (edge >= 6 && ((x - y + 400) % 24 == 0 || (x + y) % 24 == 0)) c = C(0.38f, 0.60f, 0.50f);
                    p[x, y] = c;
                }
            }
        }

        /// <summary>Bassin d'eau douce : eau vert-bleu, lit de galets, souche immergée, pierres, potamots.</summary>
        private static void DrawFreshTank(Color[,] p)
        {
            int left = TankLeft + Frame;
            int right = TankRight - Frame;
            int bottom = TankBottom + Frame;
            PixelArt.Rect(p, TankLeft, TankBottom, TankRight, TankTop, TankWood);
            PixelArt.Rect(p, TankLeft, TankTop - 1, TankRight, TankTop, TankWoodLight);

            for (int x = left; x <= right; x++)
            {
                for (int y = bottom; y <= TankTop - Frame; y++)
                {
                    Color c;
                    if (y > WaterLine)
                    {
                        c = C(0.18f, 0.24f, 0.22f);
                    }
                    else
                    {
                        float t = Mathf.InverseLerp(bottom, WaterLine, y);
                        c = Color.Lerp(FreshDeep, FreshTop, Mathf.Round(t * 8f) / 8f);
                        if (y == WaterLine) c = Color.Lerp(FreshTop, Color.white, x % 8 < 4 ? 0.5f : 0.25f);
                    }

                    p[x, y] = c;
                }
            }

            // Lit de galets arrondis de toutes les couleurs.
            Color[] pebbles = { C(0.56f, 0.52f, 0.46f), C(0.66f, 0.62f, 0.54f), C(0.46f, 0.40f, 0.34f), C(0.74f, 0.70f, 0.60f), C(0.50f, 0.50f, 0.52f) };
            for (int x = left; x <= right; x++)
            {
                int top = bottom + 6 + Mathf.RoundToInt(Mathf.Sin(x * 0.05f) * 2f);
                for (int y = bottom; y <= top; y++)
                {
                    int cell = (x / 4 + (y / 3) * 7);
                    Color c = pebbles[Mathf.Abs(cell * 31 + y) % pebbles.Length];
                    if (x % 4 == 0 || y % 3 == 0) c = DetailedFish.Shade(c, 0.75f);
                    p[x, y] = c;
                }
            }

            int floor = bottom + 6;
            DrawStone(p, 104, floor, 14, 9, C(0.52f, 0.52f, 0.50f));
            DrawStone(p, 300, floor, 18, 11, C(0.48f, 0.46f, 0.42f));
            DrawStone(p, 350, floor, 9, 6, C(0.60f, 0.58f, 0.52f));
            DrawLog(p, 170, floor + 2, 90);

            // Potamots et roseaux du fond (fixes ; d'autres plantes ondulent en jeu).
            foreach (int wx in new[] { 76, 140, 236, 270, 330, 372 })
            {
                for (int k = 0; k < 30 + (wx * 7) % 26; k++)
                {
                    int sway = Mathf.RoundToInt(Mathf.Sin(k * 0.22f + wx) * 1.5f);
                    PixelArt.Set(p, wx + sway, floor + k, k % 4 == 0 ? C(0.20f, 0.36f, 0.18f) : C(0.30f, 0.48f, 0.22f));
                    if (k % 7 == 4) PixelArt.Set(p, wx + sway + 1, floor + k, C(0.40f, 0.58f, 0.28f));
                }
            }
        }

        private static void DrawStone(Color[,] p, int cx, int baseY, int rx, int ry, Color color)
        {
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                for (int y = baseY; y <= baseY + ry; y++)
                {
                    float u = (x - cx) / (float)rx;
                    float v = (y - baseY) / (float)ry;
                    if (u * u + v * v > 1f) continue;
                    float light = 0.78f + 0.4f * Mathf.Max(0f, -u * 0.5f + v * 0.8f);
                    Color c = Noise(x, y, 9) > 0.85f ? C(0.36f, 0.48f, 0.26f) : color; // un peu de mousse
                    PixelArt.Set(p, x, y, DetailedFish.Shade(c, Mathf.Round(light * 5f) / 5f));
                }
            }
        }

        /// <summary>Souche couchée au fond, avec ses racines et des taches de mousse.</summary>
        private static void DrawLog(Color[,] p, int x0, int y0, int length)
        {
            for (int x = x0; x < x0 + length; x++)
            {
                for (int y = y0; y < y0 + 9; y++)
                {
                    Color c = C(0.36f, 0.24f, 0.14f);
                    if (y == y0 + 8) c = C(0.50f, 0.36f, 0.22f);
                    else if (y == y0) c = C(0.24f, 0.16f, 0.10f);
                    else if ((x + y * 3) % 11 == 0) c = C(0.28f, 0.18f, 0.10f);
                    if (Noise(x, y, 4) > 0.9f) c = C(0.34f, 0.46f, 0.22f);
                    p[x, y] = c;
                }
            }

            // Bout coupé avec ses cernes, et racines.
            for (int y = y0; y < y0 + 9; y++) PixelArt.Set(p, x0 + length, y, C(0.62f, 0.46f, 0.28f));
            for (int k = 0; k < 8; k++)
            {
                PixelArt.Set(p, x0 - k, y0 + 4 + k / 2, C(0.30f, 0.20f, 0.12f));
                PixelArt.Set(p, x0 - k, y0 + 2 - k / 3, C(0.30f, 0.20f, 0.12f));
            }
        }

        /// <summary>Vitre du bassin du lac : cadre de bois, reflets obliques, montants. Même taille que la salle.</summary>
        public static Color[,] LakeTankGlass()
        {
            var p = new Color[LakeWidth, LakeHeight];
            int left = TankLeft + Frame;
            int right = TankRight - Frame;
            int bottom = TankBottom + Frame;
            for (int x = TankLeft; x <= TankRight; x++)
            {
                for (int y = TankBottom; y <= TankTop; y++)
                {
                    bool inside = x >= left && x <= right && y >= bottom && y <= TankTop - Frame;
                    if (!inside) p[x, y] = y >= TankTop - 1 || x < left ? TankWoodLight : TankWood;
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
                    p[x, y] = C(0.30f, 0.20f, 0.12f, 0.9f);
                    p[x + 1, y] = C(0.48f, 0.34f, 0.20f, 0.9f);
                }
            }

            return p;
        }

        // ======================================================================
        // Vie des bassins
        // ======================================================================

        /// <summary>
        /// Plante qui ondule (3 images : penchée à gauche, droite, penchée à droite). Tiges qui partent du bas ;
        /// <paramref name="leaves"/> : feuilles en pointe le long des tiges.
        /// </summary>
        public static Color[,] Plant(int frame, int width, int height, int stems, Color main, Color dark, Color tip, bool broad, int seed)
        {
            var p = new Color[width, height];
            float bend = frame - 1f;
            for (int s = 0; s < stems; s++)
            {
                float baseX = width * 0.5f + (s - (stems - 1) * 0.5f) * Mathf.Max(1.5f, (width - 4f) / Mathf.Max(1, stems));
                int length = Mathf.RoundToInt(height * (0.6f + 0.4f * Noise(s, seed, 1)));
                for (int k = 0; k < length; k++)
                {
                    float t = k / (float)height;
                    float x = baseX + Mathf.Sin(k * 0.22f + s * 1.7f + seed) * 1.2f + bend * t * t * 4f;
                    Color c = k > length - 3 ? tip : (k + s) % 5 == 0 ? dark : main;
                    int ix = Mathf.RoundToInt(x);
                    PixelArt.Set(p, ix, k, c);
                    if (broad && k < length - 4) PixelArt.Set(p, ix + 1, k, (k + s) % 5 == 0 ? dark : DetailedFish.Shade(main, 0.88f));
                    if (k % 6 == 3 && k < length - 3)
                    {
                        int side = (k / 6 + s) % 2 == 0 ? 1 : -1;
                        PixelArt.Set(p, ix + side * (broad ? 2 : 1), k + 1, main);
                        PixelArt.Set(p, ix + side * (broad ? 3 : 2), k + 2, tip);
                    }
                }
            }

            return p;
        }

        /// <summary>Bulle 5×5 : anneau clair et reflet, centre presque transparent.</summary>
        public static Color[,] Bubble()
        {
            // Petit anneau clair de 4×4 et un reflet : se lit tout de suite comme une bulle.
            var p = new Color[4, 4];
            Color ring = C(0.86f, 0.97f, 1f, 0.9f);
            foreach (Vector2Int k in new[] { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(3, 1), new Vector2Int(0, 2), new Vector2Int(3, 2), new Vector2Int(1, 3), new Vector2Int(2, 3) })
            {
                p[k.x, k.y] = ring;
            }

            p[1, 2] = C(1f, 1f, 1f, 1f);
            return p;
        }

        /// <summary>Rayon de lumière 28×76 : bande oblique très transparente, plus forte en haut.</summary>
        public static Color[,] LightRay()
        {
            var p = new Color[28, 76];
            for (int y = 0; y < 76; y++)
            {
                float center = 6f + y * 0.2f;
                float alpha = Mathf.Lerp(0.02f, 0.2f, y / 75f);
                for (int x = 0; x < 28; x++)
                {
                    float d = Mathf.Abs(x - center);
                    if (d < 5f) p[x, y] = C(0.86f, 0.98f, 1f, d < 3f ? alpha : alpha * 0.5f);
                }
            }

            return p;
        }

        /// <summary>Petit poisson d'un banc 8×5, 2 images (queue d'un côté puis de l'autre).</summary>
        public static Color[,] TinyFish(int frame, Color body, Color belly, Color fin)
        {
            var p = new Color[8, 5];
            for (int x = 2; x <= 6; x++) PixelArt.Set(p, x, 2, body);
            for (int x = 3; x <= 5; x++) PixelArt.Set(p, x, 3, body);
            for (int x = 3; x <= 5; x++) PixelArt.Set(p, x, 1, belly);
            PixelArt.Set(p, 6, 3, C(0.05f, 0.05f, 0.05f)); // œil
            PixelArt.Set(p, 0, frame == 0 ? 3 : 1, fin);
            PixelArt.Set(p, 1, 2, fin);
            PixelArt.Set(p, 0, frame == 0 ? 2 : 2, fin);
            return p;
        }

        /// <summary>Reflets d'eau au sol 64×24 (3 images) : réseau de lignes claires qui ondulent, très transparent.</summary>
        public static Color[,] Caustics(int frame, Color tint)
        {
            var p = new Color[64, 24];
            float shift = frame * 2.1f;
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 24; y++)
                {
                    float v = Mathf.Sin(x * 0.3f + shift + Mathf.Sin(y * 0.55f + shift) * 1.5f)
                        + Mathf.Sin(y * 0.5f - shift * 0.7f + Mathf.Sin(x * 0.25f) * 1.5f);
                    if (v > 1.35f)
                    {
                        float fade = 1f - Mathf.Abs(y - 11.5f) / 12f;
                        p[x, y] = new Color(tint.r, tint.g, tint.b, 0.28f * fade);
                    }
                }
            }

            return p;
        }

        /// <summary>Méduse 14×18, 2 images : ombrelle ouverte puis contractée, tentacules qui suivent.</summary>
        public static Color[,] Jelly(int frame, Color color)
        {
            var p = new Color[14, 18];
            float rx = frame == 0 ? 5.5f : 4.2f;
            float ry = frame == 0 ? 4f : 5f;
            const float cx = 6.5f;
            const float cy = 12f;
            for (int x = 0; x < 14; x++)
            {
                for (int y = 0; y < 18; y++)
                {
                    float u = (x - cx) / rx;
                    float v = (y - cy) / ry;
                    if (v >= -0.2f && u * u + v * v <= 1f)
                    {
                        p[x, y] = v > 0.5f ? new Color(Mathf.Min(1f, color.r + 0.2f), Mathf.Min(1f, color.g + 0.2f), Mathf.Min(1f, color.b + 0.2f), 0.85f) : new Color(color.r, color.g, color.b, 0.75f);
                    }
                }
            }

            for (int t = -2; t <= 2; t++)
            {
                for (int k = 1; k <= (frame == 0 ? 8 : 6); k++)
                {
                    int x = Mathf.RoundToInt(cx + t * (frame == 0 ? 2f : 1.4f) + Mathf.Sin(k * 0.9f + t) * 0.8f);
                    PixelArt.Set(p, x, Mathf.RoundToInt(cy - 1 - k), new Color(color.r, color.g, color.b, 0.55f));
                }
            }

            return p;
        }

        // ======================================================================
        // Visiteurs
        // ======================================================================

        /// <summary>
        /// Une image du pêcheur, recolorée en visiteur : la chemise et le short changent de teinte (le chapeau et la peau
        /// restent). Lit le PNG déjà généré du pêcheur.
        /// </summary>
        public static Color[,] Visitor(string fisherPng, float shirtHue, float shortsHue, float shortsSat, float shortsValue)
        {
            string full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, fisherPng);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(full));
            var p = new Color[texture.width, texture.height];
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    Color c = texture.GetPixel(x, y);
                    Color.RGBToHSV(c, out float h, out float s, out float v);
                    if (c.a > 0.5f && s > 0.35f && h > 0.44f && h < 0.6f)
                    {
                        c = Color.HSVToRGB(shirtHue, s, v); // chemise
                    }
                    else if (c.a > 0.5f && s > 0.6f && v > 0.45f && h > 0.03f && h < 0.1f)
                    {
                        c = Color.HSVToRGB(shortsHue, s * shortsSat, v * shortsValue); // short
                    }

                    c.a = texture.GetPixel(x, y).a;
                    p[x, y] = c;
                }
            }

            Object.DestroyImmediate(texture);
            return p;
        }
    }
}
