using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Salle de la baie d'Ha Long à l'aquarium, même découpage que les autres (passage de 2 cases puis salle de 24 cases,
    /// 416×288 à 16 px par case) : sol de tomettes de terre cuite, mur de panneaux de bois laqué rouge à liserés dorés ;
    /// bassin d'eau émeraude où se dressent de petits pitons calcaires couverts de verdure, sable clair, herbiers et
    /// bambous d'eau. Mobilier : jonque miniature sur son socle, lanterne de soie rouge, bambou en pot (style en volume).
    /// </summary>
    public static class AquariumHaLongArt
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

        private static readonly Color Terracotta = C(0.70f, 0.40f, 0.26f);
        private static readonly Color Lacquer = C(0.52f, 0.12f, 0.10f);
        private static readonly Color Gold = C(0.84f, 0.66f, 0.28f);
        private static readonly Color Teak = C(0.36f, 0.22f, 0.14f);
        private static readonly Color DeepSea = C(0.04f, 0.20f, 0.18f);
        private static readonly Color TopSea = C(0.24f, 0.56f, 0.46f);
        private static readonly Color Limestone = C(0.66f, 0.64f, 0.58f);

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
        private static Color S(Color c, float light) => DetailedFish.Shade(c, light);

        /// <summary>Passage puis salle de la baie d'Ha Long, 416×288 (y depuis le bas).</summary>
        public static Color[,] HaLongRoom()
        {
            var p = new Color[Width, Height];

            // Tomettes carrées de terre cuite, joints clairs, teintes un peu inégales.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < StandBottom; y++)
                {
                    int tx = x / 16, ty = y / 16;
                    Color c = (tx * 7 + ty * 3) % 5 == 0 ? S(Terracotta, 0.9f) : (tx + ty) % 2 == 0 ? Terracotta : S(Terracotta, 1.06f);
                    if (x % 16 == 0 || y % 16 == 0) c = C(0.80f, 0.70f, 0.58f);
                    if (Mathf.PerlinNoise(x * 0.15f, y * 0.15f) > 0.76f) c = S(c, 0.92f);
                    p[x, y] = c;
                }
            }

            // Mur du fond : panneaux laqués rouges encadrés de teck, liseré doré.
            for (int x = Room; x < Width; x++)
            {
                for (int y = StandBottom; y < Height; y++)
                {
                    int px = (x - Room) % 32, py = (y - StandBottom) % 40;
                    Color c = px < 3 || py < 3 ? Teak : px == 3 || py == 3 ? Gold : Lacquer;
                    if (c == Lacquer && Mathf.PerlinNoise(x * 0.05f, y * 0.3f) > 0.7f) c = S(c, 1.12f);
                    p[x, y] = c;
                }
            }

            // Meuble du bassin en teck sombre, frise dorée.
            PixelArt.Rect(p, TankLeft - 4, StandBottom, TankRight + 4, TankBottom - 1, Teak);
            for (int x = TankLeft - 4; x <= TankRight + 4; x++)
            {
                if (x % 12 == 0) PixelArt.Rect(p, x, StandBottom + 2, x, TankBottom - 3, S(Teak, 0.7f));
                PixelArt.Set(p, x, TankBottom - 3, Gold);
                if (x % 6 < 3) PixelArt.Set(p, x, StandBottom + 4, Gold);
            }

            DrawTank(p);

            // Murs latéraux sombres ; ouverture du passage à gauche.
            PixelArt.Rect(p, Room, 0, Room + 9, Height - 1, C(0.20f, 0.10f, 0.08f));
            PixelArt.Rect(p, Width - 10, 0, Width - 1, Height - 1, C(0.20f, 0.10f, 0.08f));
            PixelArt.Rect(p, Width - 10, 0, Width - 10, StandBottom, Teak);

            PixelArt.Rect(p, 0, 0, Room + 9, PassageBottom - 1, C(0.06f, 0.06f, 0.07f));
            PixelArt.Rect(p, 0, PassageTop + 6, Room + 9, Height - 1, C(0.06f, 0.06f, 0.07f));
            for (int x = 0; x <= Room + 9; x++)
            {
                for (int y = PassageBottom; y <= PassageTop; y++) p[x, y] = x % 16 == 0 || y % 16 == 0 ? C(0.80f, 0.70f, 0.58f) : Terracotta;
            }

            PixelArt.Rect(p, 0, PassageTop + 1, Room + 9, PassageTop + 5, Teak);
            PixelArt.Rect(p, Room + 10, PassageBottom - 1, Room + 10, PassageTop + 4, Teak);
            return p;
        }

        /// <summary>Bassin d'eau émeraude : sable clair, petits pitons calcaires verdoyants, herbiers, bambous d'eau.</summary>
        private static void DrawTank(Color[,] p)
        {
            int left = TankLeft + Frame;
            int right = TankRight - Frame;
            int bottom = TankBottom + Frame;
            PixelArt.Rect(p, TankLeft, TankBottom, TankRight, TankTop, Teak);
            PixelArt.Rect(p, TankLeft, TankTop - 1, TankRight, TankTop, Gold);

            for (int x = left; x <= right; x++)
            {
                for (int y = bottom; y <= TankTop - Frame; y++)
                {
                    Color c;
                    if (y > WaterLine)
                    {
                        c = C(0.10f, 0.12f, 0.11f);
                    }
                    else
                    {
                        float t = Mathf.InverseLerp(bottom, WaterLine, y);
                        c = Color.Lerp(DeepSea, TopSea, Mathf.Round(t * 7f) / 7f);
                        if (y == WaterLine) c = Color.Lerp(TopSea, Color.white, x % 8 < 4 ? 0.35f : 0.15f);
                    }

                    p[x, y] = c;
                }
            }

            // Fond de sable clair.
            for (int x = left; x <= right; x++)
            {
                int top = bottom + 5 + Mathf.RoundToInt(Mathf.Sin(x * 0.06f) * 2f);
                for (int y = bottom; y <= top; y++) p[x, y] = (x * 5 + y * 3) % 9 == 0 ? C(0.70f, 0.64f, 0.48f) : C(0.86f, 0.80f, 0.62f);
            }

            // Petits pitons calcaires : parois striées, sommets couverts de verdure.
            var random = new System.Random(9292);
            for (int i = 0; i < 4; i++)
            {
                int rx = left + 34 + i * (right - left - 68) / 3 + random.Next(-12, 12);
                int half = 9 + random.Next(0, 6), rh = 30 + random.Next(0, 30);
                for (int y = 0; y <= rh; y++)
                {
                    float k = y / (float)rh;
                    int w = Mathf.RoundToInt(half * (1f - k * 0.45f) * Mathf.Sqrt(Mathf.Clamp01((1f - k) * 6f)));
                    for (int x = -w; x <= w; x++)
                    {
                        Color c = x < -w / 3 ? S(Limestone, 1.08f) : x > w / 3 ? S(Limestone, 0.78f) : Limestone;
                        if ((x + rx) % 4 == 0 && Mathf.PerlinNoise(rx + x * 0.3f, y * 0.05f) > 0.5f) c = S(c, 0.8f);   // coulures
                        if (k > 0.7f && Mathf.PerlinNoise((rx + x) * 0.3f, y * 0.3f) > 0.35f) c = Mathf.PerlinNoise(x * 0.6f, y * 0.6f) > 0.5f ? C(0.30f, 0.52f, 0.22f) : C(0.20f, 0.38f, 0.16f);
                        PixelArt.Set(p, rx + x, bottom + 4 + y, c);
                    }
                }
            }

            // Herbiers et bambous d'eau.
            for (int i = 0; i < 12; i++)
            {
                int kx = left + 8 + random.Next(0, right - left - 16);
                int height = 14 + random.Next(0, 34);
                Color weed = i % 3 == 0 ? C(0.36f, 0.56f, 0.24f) : C(0.24f, 0.46f, 0.26f);
                for (int y = 0; y < height; y++)
                {
                    int x = kx + Mathf.RoundToInt(Mathf.Sin(y * 0.14f + i) * 2f);
                    PixelArt.Set(p, x, bottom + 4 + y, weed);
                    if (i % 4 == 0 && y % 9 == 0) PixelArt.Rect(p, x - 1, bottom + 4 + y, x + 1, bottom + 4 + y, S(weed, 0.7f));   // nœuds du bambou
                }
            }
        }

        /// <summary>Vitre du bassin : cadre de teck à liseré doré, reflets obliques, montants. Même taille que la salle.</summary>
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
                    if (!inside) p[x, y] = y >= TankTop - 1 || y == TankBottom + 1 ? Gold : x < left ? Teak : S(Teak, 0.8f);
                }
            }

            for (int x = left; x <= right; x++)
            {
                for (int y = bottom; y <= WaterLine; y++)
                {
                    int band = (x - y + 900) % 96;
                    if (band < 3 || band == 7) p[x, y] = C(1f, 1f, 1f, band == 7 ? 0.08f : 0.12f);
                }
            }

            foreach (int x in new[] { TankLeft + 115, TankRight - 115 })
            {
                for (int y = bottom; y <= TankTop - Frame; y++)
                {
                    p[x, y] = C(0.30f, 0.18f, 0.10f, 0.9f);
                    p[x + 1, y] = C(0.84f, 0.66f, 0.28f, 0.9f);
                }
            }

            return p;
        }

        // ---------- Mobilier (style en volume, 16 px par case comme les autres meubles) ----------

        /// <summary>Jonque miniature sur un socle de bois laqué : coque sombre, trois voiles rouges lattées.</summary>
        public static Color[,] JunkModel()
        {
            var cv = new VolumeCanvas(56, 56);
            cv.Piece(Box(6, 46, 50, 56), Lacquer, 2f, (x, y, c) => y == 48 ? Gold : c);
            foreach ((float mast, float top, float w) in new[] { (18f, 14f, 10f), (28f, 6f, 13f), (38f, 16f, 9f) })
            {
                cv.Piece(Box(mast - 0.6f, top - 2f, mast + 0.6f, 38f), Teak, 0.5f);
                cv.Piece(Box(mast - 2f, top, mast + w, 34f), C(0.72f, 0.26f, 0.14f), 2f, (x, y, c) => y % 5 == 0 ? Teak : c);
            }

            cv.Piece((x, y) => y > 34 && y < 44 && Mathf.Abs(x - 28f) < 22f - (y - 34f) * 0.8f, Teak, 3f, (x, y, c) => y % 3 == 0 ? S(c, 0.75f) : c);
            return cv.Finish(C(0.06f, 0.04f, 0.03f));
        }

        /// <summary>Lanterne de soie rouge, ronde, cerclée de bambou, gland doré qui pend dessous.</summary>
        public static Color[,] SilkLantern()
        {
            var cv = new VolumeCanvas(24, 44);
            cv.Piece(Box(11, 0, 13, 8), Teak, 1f);
            cv.Piece(Ellipse(12f, 20f, 10f, 12f), C(0.86f, 0.16f, 0.10f), 4f, (x, y, c) => y % 6 == 0 ? C(0.56f, 0.40f, 0.20f) : (x + y) % 9 == 0 ? C(1f, 0.46f, 0.30f) : c);
            cv.Piece(Box(7, 7, 17, 10), Gold, 1f);
            cv.Piece(Box(7, 30, 17, 33), Gold, 1f);
            cv.Piece(Box(11, 33, 13, 43), Gold, 1f);
            return cv.Finish(C(0.10f, 0.04f, 0.03f));
        }

        /// <summary>Bambou en pot : grand pot de céramique bleu et blanc, trois tiges de bambou à nœuds et leurs feuilles.</summary>
        public static Color[,] PottedBamboo()
        {
            var cv = new VolumeCanvas(32, 72);
            foreach ((float x, float top) in new[] { (11f, 4f), (17f, 12f), (22f, 8f) })
            {
                cv.Piece(Box(x - 1.5f, top, x + 1.5f, 56f), C(0.52f, 0.66f, 0.28f), 1.2f, (px, py, c) => py % 10 == 0 ? S(c, 0.65f) : c);
                cv.Piece(Ellipse(x + 4f, top + 6f, 5f, 2f), C(0.30f, 0.54f, 0.22f), 1.5f);
                cv.Piece(Ellipse(x - 4f, top + 14f, 5f, 2f), C(0.26f, 0.48f, 0.20f), 1.5f);
            }

            cv.Piece((x, y) => y > 52 && y < 72 && Mathf.Abs(x - 16f) < 12f - Mathf.Abs(y - 60f) * 0.25f, C(0.90f, 0.92f, 0.94f), 4f,
                (x, y, c) => (y > 58 && y < 64 && (x + y) % 4 < 2) || y == 55 || y == 68 ? C(0.18f, 0.30f, 0.64f) : c);
            return cv.Finish(C(0.06f, 0.06f, 0.06f));
        }
    }
}
