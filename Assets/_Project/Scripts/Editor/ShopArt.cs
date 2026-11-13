using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Pixel art de la boutique (16 px par case) : façade 80×64 (5 cases de large), intérieur 160×112 (10×7 cases),
    /// comptoir 80×22, marchand 16×24 et icône 16×16 pour la minimap.
    /// </summary>
    public static class ShopArt
    {
        public const int ExteriorWidth = 80;
        public const int ExteriorHeight = 64;
        public static readonly Vector2 ExteriorPivot = new Vector2(0.5f, 3f / ExteriorHeight);

        public const int InteriorWidth = 160;
        public const int InteriorHeight = 112;

        public const int CounterWidth = 80;
        public const int CounterHeight = 22;
        public static readonly Vector2 CounterPivot = new Vector2(0.5f, 0f);

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Outline = C(0.12f, 0.08f, 0.06f);
        private static readonly Color Plank = C(0.80f, 0.62f, 0.40f);
        private static readonly Color PlankDark = C(0.64f, 0.47f, 0.29f);
        private static readonly Color PlankLight = C(0.88f, 0.72f, 0.50f);
        private static readonly Color WoodDark = C(0.38f, 0.25f, 0.15f);
        private static readonly Color Teal = C(0.18f, 0.56f, 0.60f);
        private static readonly Color TealDark = C(0.11f, 0.40f, 0.45f);
        private static readonly Color Glass = C(0.62f, 0.86f, 0.92f);
        private static readonly Color GlassDark = C(0.42f, 0.70f, 0.80f);
        private static readonly Color Red = C(0.86f, 0.24f, 0.22f);
        private static readonly Color White = C(0.97f, 0.96f, 0.92f);
        private static readonly Color Thatch = C(0.82f, 0.68f, 0.40f);
        private static readonly Color ThatchDark = C(0.66f, 0.52f, 0.28f);
        private static readonly Color ThatchLight = C(0.92f, 0.80f, 0.52f);
        private static readonly Color GoldCoin = C(1.00f, 0.82f, 0.25f);
        private static readonly Color GoldDark = C(0.80f, 0.58f, 0.12f);
        private static readonly Color Orange = C(1.00f, 0.58f, 0.20f);

        // ---------- Façade ----------

        public static Color[,] Exterior()
        {
            var p = new Color[ExteriorWidth, ExteriorHeight];

            // Ombre au sol et paillasson devant la porte.
            PixelArt.Rect(p, 1, 0, 78, 3, C(0.05f, 0.15f, 0.08f, 0.35f));
            PixelArt.Rect(p, 33, 0, 46, 2, Red);
            PixelArt.Rect(p, 34, 1, 45, 1, C(0.95f, 0.55f, 0.30f));

            // Murs en planches horizontales.
            for (int y = 3; y <= 33; y++)
            {
                for (int x = 3; x <= 76; x++)
                {
                    Color c = (y - 3) % 4 == 0 ? PlankDark : (y - 3) % 4 == 3 ? PlankLight : Plank;
                    if ((x + (y / 4) * 7) % 19 == 0)
                    {
                        c = PlankDark; // joints décalés
                    }

                    p[x, y] = c;
                }
            }

            PixelArt.Rect(p, 3, 3, 76, 5, C(0.62f, 0.62f, 0.58f)); // soubassement
            PixelArt.Rect(p, 3, 3, 4, 33, WoodDark);                 // poteaux d'angle
            PixelArt.Rect(p, 75, 3, 76, 33, WoodDark);

            // Vitrines.
            DrawShopWindow(p, 8, 11, 28, 26, leftSide: true);
            DrawShopWindow(p, 51, 11, 71, 26, leftSide: false);

            // Porte vitrée.
            PixelArt.Rect(p, 33, 4, 46, 26, WoodDark);
            PixelArt.Rect(p, 35, 5, 44, 25, Teal);
            PixelArt.Rect(p, 36, 15, 43, 23, Glass);
            PixelArt.Rect(p, 36, 15, 43, 15, GlassDark);
            PixelArt.Rect(p, 39, 15, 40, 23, TealDark);
            PixelArt.Rect(p, 36, 6, 43, 13, TealDark);
            PixelArt.Set(p, 42, 11, GoldCoin); // poignée

            // Auvent rayé rouge et blanc, bord festonné.
            for (int x = 1; x <= 78; x++)
            {
                Color stripe = (x / 4) % 2 == 0 ? Red : White;
                for (int y = 29; y <= 35; y++)
                {
                    p[x, y] = y == 35 ? Color.Lerp(stripe, Color.black, 0.15f) : stripe;
                }

                if (x % 4 != 0)
                {
                    p[x, 28] = stripe; // festons
                }
            }

            // Toit de chaume.
            for (int y = 36; y <= 59; y++)
            {
                int inset = (y - 36) / 2;
                for (int x = inset; x < ExteriorWidth - inset; x++)
                {
                    Color c = (y - 36) % 4 == 0 ? ThatchDark : Thatch;
                    if ((x * 3 + y) % 7 == 0)
                    {
                        c = ThatchLight;
                    }

                    p[x, y] = c;
                }
            }

            // Enseigne : planche avec un poisson et une pièce.
            PixelArt.Rect(p, 24, 39, 55, 50, WoodDark);
            PixelArt.Rect(p, 25, 40, 54, 49, Plank);
            DrawFishIcon(p, 34, 44, Orange);
            DrawCoin(p, 47, 44);

            OutlineOpaque(p, Outline);
            return p;
        }

        private static void DrawShopWindow(Color[,] p, int x0, int y0, int x1, int y1, bool leftSide)
        {
            PixelArt.Rect(p, x0 - 1, y0 - 1, x1 + 1, y1 + 1, White);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    p[x, y] = (x + y) % 9 == 0 ? GlassDark : Glass;
                }
            }

            PixelArt.Rect(p, (x0 + x1) / 2, y0, (x0 + x1) / 2, y1, White); // croisillon

            if (leftSide)
            {
                // Poissons suspendus à vendre.
                for (int i = 0; i < 3; i++)
                {
                    int fx = x0 + 3 + i * 6;
                    PixelArt.Rect(p, fx + 1, y1 - 4, fx + 1, y1, WoodDark);
                    DrawFishIcon(p, fx + 1, y1 - 7, i == 1 ? C(0.40f, 0.62f, 0.90f) : C(0.85f, 0.85f, 0.80f), vertical: true);
                }
            }
            else
            {
                // Canne à pêche et bocaux d'appâts.
                for (int k = 0; k < 14; k++)
                {
                    PixelArt.Set(p, x0 + 2 + k, y0 + 2 + k, WoodDark);
                }

                PixelArt.Rect(p, x1 - 7, y0 + 1, x1 - 4, y0 + 5, C(0.95f, 0.75f, 0.35f));
                PixelArt.Rect(p, x1 - 3, y0 + 1, x1 - 1, y0 + 4, C(0.55f, 0.85f, 0.55f));
            }
        }

        // ---------- Intérieur ----------

        public static Color[,] Interior()
        {
            var p = new Color[InteriorWidth, InteriorHeight];
            Color floor = C(0.62f, 0.45f, 0.30f);
            Color floorDark = C(0.50f, 0.35f, 0.22f);
            Color floorLight = C(0.70f, 0.52f, 0.35f);
            Color wall = C(0.90f, 0.82f, 0.64f);
            Color wallShade = C(0.82f, 0.73f, 0.55f);

            // Parquet (planches de 6 px, joints décalés).
            for (int y = 0; y < 72; y++)
            {
                for (int x = 0; x < InteriorWidth; x++)
                {
                    int row = y / 6;
                    Color c = y % 6 == 0 ? floorDark : y % 6 == 5 ? floorLight : floor;
                    if ((x + row * 23) % 40 == 0)
                    {
                        c = floorDark;
                    }

                    p[x, y] = c;
                }
            }

            // Mur du fond avec lambris.
            for (int y = 72; y < InteriorHeight; y++)
            {
                for (int x = 0; x < InteriorWidth; x++)
                {
                    p[x, y] = y < 82 ? ((x % 8 == 0) ? WoodDark : PlankDark) : (x % 16 == 0 ? wallShade : wall);
                }
            }

            PixelArt.Rect(p, 0, 82, InteriorWidth - 1, 83, WoodDark);

            // Étagères avec bocaux d'appâts et boîtes.
            DrawShelf(p, 10, 88, 58, seed: 3);
            DrawShelf(p, 10, 100, 58, seed: 5);
            DrawShelf(p, 102, 88, 150, seed: 7);
            DrawShelf(p, 102, 100, 150, seed: 9);

            // Trophées : poissons montés sur planchettes.
            PixelArt.Rect(p, 66, 92, 94, 106, WoodDark);
            PixelArt.Rect(p, 67, 93, 93, 105, Plank);
            DrawFishIcon(p, 76, 99, C(0.30f, 0.70f, 0.66f));
            DrawFishIcon(p, 86, 99, C(1.00f, 0.60f, 0.25f));

            // Murs latéraux.
            PixelArt.Rect(p, 0, 0, 5, InteriorHeight - 1, WoodDark);
            PixelArt.Rect(p, InteriorWidth - 6, 0, InteriorWidth - 1, InteriorHeight - 1, WoodDark);

            // Paillasson de sortie.
            PixelArt.Rect(p, 66, 0, 93, 7, Red);
            PixelArt.Rect(p, 68, 2, 91, 5, C(0.95f, 0.55f, 0.30f));

            // Tapis devant le comptoir.
            PixelArt.Rect(p, 46, 30, 113, 46, C(0.20f, 0.50f, 0.55f));
            PixelArt.Rect(p, 48, 32, 111, 44, C(0.28f, 0.62f, 0.64f));
            for (int x = 50; x < 110; x += 6)
            {
                PixelArt.Rect(p, x, 37, x + 2, 39, C(0.95f, 0.85f, 0.55f));
            }

            return p;
        }

        private static void DrawShelf(Color[,] p, int x0, int y, int x1, int seed)
        {
            var random = new System.Random(seed);
            PixelArt.Rect(p, x0, y, x1, y + 1, WoodDark);
            Color[] jars = { C(0.95f, 0.75f, 0.35f), C(0.55f, 0.85f, 0.55f), C(0.85f, 0.45f, 0.40f), C(0.55f, 0.70f, 0.95f), C(0.90f, 0.88f, 0.80f) };
            for (int x = x0 + 2; x < x1 - 4; x += 7)
            {
                Color jar = jars[random.Next(jars.Length)];
                int height = 4 + random.Next(0, 4);
                PixelArt.Rect(p, x, y + 2, x + 4, y + 1 + height, jar);
                PixelArt.Rect(p, x + 1, y + 2 + height - 1, x + 3, y + 2 + height - 1, Color.Lerp(jar, Color.white, 0.4f));
                PixelArt.Set(p, x, y + 2, Color.Lerp(jar, Color.black, 0.3f));
            }
        }

        // ---------- Comptoir ----------

        public static Color[,] Counter()
        {
            var p = new Color[CounterWidth, CounterHeight];
            PixelArt.Rect(p, 1, 0, 78, 13, PlankDark);
            for (int x = 1; x <= 78; x += 6)
            {
                PixelArt.Rect(p, x, 0, x, 13, WoodDark);
            }

            PixelArt.Rect(p, 0, 14, 79, 18, Plank);          // plateau
            PixelArt.Rect(p, 0, 14, 79, 14, PlankLight);

            // Caisse enregistreuse et poisson sur le comptoir.
            PixelArt.Rect(p, 56, 15, 68, 21, C(0.30f, 0.32f, 0.36f));
            PixelArt.Rect(p, 58, 18, 66, 20, C(0.55f, 0.85f, 0.60f));
            DrawCoin(p, 20, 17);
            DrawCoin(p, 24, 16);
            DrawFishIcon(p, 38, 17, C(0.85f, 0.85f, 0.80f));

            OutlineOpaque(p, Outline);
            return p;
        }

        // ---------- Marchand ----------

        /// <summary>Marchand de face, 16×24 : bob de marin, moustache, tablier, chemise rayée.</summary>
        public static Color[,] Merchant()
        {
            var p = new Color[16, 24];
            Color skin = C(0.86f, 0.64f, 0.46f);
            Color skinShade = C(0.74f, 0.52f, 0.36f);
            Color cap = C(0.96f, 0.96f, 0.94f);
            Color capBand = C(0.20f, 0.36f, 0.66f);
            Color hair = C(0.82f, 0.82f, 0.80f);
            Color shirt = C(0.20f, 0.36f, 0.66f);
            Color apron = C(0.95f, 0.92f, 0.84f);
            Color pants = C(0.30f, 0.26f, 0.22f);

            void R(int x0, int top0, int x1, int top1, Color c) => PixelArt.Rect(p, x0, 23 - top1, x1, 23 - top0, c);

            R(5, 1, 10, 3, cap); R(4, 4, 11, 4, capBand); R(3, 5, 12, 5, cap);
            R(4, 6, 11, 11, skin);
            R(4, 6, 4, 9, hair); R(11, 6, 11, 9, hair);
            R(6, 8, 6, 8, C(0.1f, 0.08f, 0.08f)); R(9, 8, 9, 8, C(0.1f, 0.08f, 0.08f));
            R(5, 10, 10, 10, hair);                 // moustache
            R(7, 9, 8, 9, skinShade);               // nez
            R(3, 12, 12, 17, shirt);
            for (int y = 12; y <= 17; y += 2) R(3, y, 12, y, Color.Lerp(shirt, Color.white, 0.5f));
            R(5, 13, 10, 20, apron);                // tablier
            R(5, 13, 10, 13, Color.Lerp(apron, Color.black, 0.15f));
            R(2, 13, 2, 16, shirt); R(13, 13, 13, 16, shirt);
            R(2, 17, 2, 18, skin); R(13, 17, 13, 18, skin);
            R(5, 21, 6, 22, pants); R(9, 21, 10, 22, pants);
            R(5, 23, 6, 23, WoodDark); R(9, 23, 10, 23, WoodDark);

            PixelArt.Outline(p, Outline);
            return p;
        }

        // ---------- Icône minimap ----------

        /// <summary>Petite boutique : toit, auvent rayé, pièce d'or.</summary>
        public static Color[,] Icon()
        {
            var p = new Color[16, 16];
            for (int y = 10; y <= 14; y++)
            {
                int inset = y - 10;
                PixelArt.Rect(p, 1 + inset, y, 14 - inset, y, Thatch);
            }

            for (int x = 1; x <= 14; x++)
            {
                Color stripe = (x / 2) % 2 == 0 ? Red : White;
                PixelArt.Rect(p, x, 8, x, 9, stripe);
            }

            PixelArt.Rect(p, 2, 1, 13, 7, Plank);
            PixelArt.Rect(p, 6, 1, 9, 5, Teal);
            DrawCoin(p, 12, 3);
            PixelArt.Outline(p, Outline);
            return p;
        }

        // ---------- Petits motifs ----------

        private static void DrawFishIcon(Color[,] p, int cx, int cy, Color color, bool vertical = false)
        {
            if (vertical)
            {
                PixelArt.Rect(p, cx - 1, cy - 2, cx + 1, cy + 2, color);
                PixelArt.Set(p, cx, cy + 3, color);
                PixelArt.Set(p, cx - 1, cy - 3, color);
                PixelArt.Set(p, cx + 1, cy - 3, color);
                PixelArt.Set(p, cx, cy + 1, C(0.05f, 0.05f, 0.05f));
                return;
            }

            PixelArt.Rect(p, cx - 3, cy - 1, cx + 2, cy + 1, color);
            PixelArt.Set(p, cx + 3, cy, color);
            PixelArt.Rect(p, cx - 5, cy - 2, cx - 4, cy - 2, color);
            PixelArt.Rect(p, cx - 5, cy + 2, cx - 4, cy + 2, color);
            PixelArt.Set(p, cx - 4, cy, color);
            PixelArt.Set(p, cx + 1, cy + 1, C(0.05f, 0.05f, 0.05f));
        }

        private static void DrawCoin(Color[,] p, int cx, int cy)
        {
            PixelArt.Rect(p, cx - 1, cy - 2, cx + 1, cy + 2, GoldCoin);
            PixelArt.Rect(p, cx - 2, cy - 1, cx + 2, cy + 1, GoldCoin);
            PixelArt.Set(p, cx, cy, GoldDark);
            PixelArt.Set(p, cx - 1, cy + 1, Color.white);
        }

        private static void OutlineOpaque(Color[,] p, Color color)
        {
            int width = p.GetLength(0);
            int height = p.GetLength(1);
            var outline = new bool[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (Opaque(p, x, y)) continue;
                    if (Opaque(p, x + 1, y) || Opaque(p, x - 1, y) || Opaque(p, x, y + 1) || Opaque(p, x, y - 1))
                    {
                        outline[x, y] = true;
                    }
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (outline[x, y]) p[x, y] = color;
                }
            }
        }

        private static bool Opaque(Color[,] p, int x, int y)
        {
            return x >= 0 && y >= 0 && x < p.GetLength(0) && y < p.GetLength(1) && p[x, y].a > 0.9f;
        }
    }
}
