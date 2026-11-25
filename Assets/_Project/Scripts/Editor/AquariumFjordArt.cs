using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Salle des fjords de Norvège à l'aquarium, même découpage que les autres (passage de 2 cases puis salle de 24 cases,
    /// 416×288 à 16 px par case) : une cabane de pêcheur norvégienne, plancher de pin, mur de rondins jointés de mousse ;
    /// bassin d'eau froide du fjord, bleu-vert sombre, laminaires,
    /// rochers couverts d'algues roses, oursins et étoiles de mer. Mobilier : poêle à bois, tonneau au filet, lampe-tempête
    /// (style en volume).
    /// </summary>
    public static class AquariumFjordArt
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

        private static readonly Color Pine = C(0.62f, 0.46f, 0.28f);
        private static readonly Color PineDark = C(0.38f, 0.26f, 0.15f);
        private static readonly Color Log = C(0.44f, 0.30f, 0.18f);
        private static readonly Color Moss = C(0.40f, 0.48f, 0.26f);
        private static readonly Color DeepSea = C(0.03f, 0.09f, 0.13f);
        private static readonly Color TopSea = C(0.18f, 0.36f, 0.42f);
        private static readonly Color Iron = C(0.20f, 0.20f, 0.22f);

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
        private static Color S(Color c, float light) => DetailedFish.Shade(c, light);

        /// <summary>Passage puis salle des fjords, 416×288 (y depuis le bas).</summary>
        public static Color[,] FjordRoom()
        {
            var p = new Color[Width, Height];

            // Plancher de pin : larges lames verticales, nœuds, chevilles.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < StandBottom; y++)
                {
                    int col = x / 14;
                    Color c = x % 14 == 0 ? PineDark : (col % 2 == 0 ? Pine : S(Pine, 0.92f));
                    if ((y + col * 37) % 90 == 0) c = PineDark;
                    if (Mathf.PerlinNoise(x * 0.2f, y * 0.02f) > 0.72f) c = S(c, 0.9f);
                    if ((x * 7 + y * 13) % 211 == 0) c = S(PineDark, 0.8f);   // nœuds
                    p[x, y] = c;
                }
            }

            // Mur du fond : rondins horizontaux jointés de mousse.
            for (int x = Room; x < Width; x++)
            {
                for (int y = StandBottom; y < Height; y++)
                {
                    int ly = (y - StandBottom) % 12;
                    Color c = ly == 0 ? Moss : ly == 1 || ly == 11 ? S(Log, 0.75f) : ly < 5 ? S(Log, 1.12f) : Log;
                    if (ly > 1 && ly < 11 && Mathf.PerlinNoise(x * 0.08f, y * 0.5f) > 0.75f) c = S(c, 0.85f);
                    p[x, y] = c;
                }
            }

            // Meuble du bassin en planches de pin cerclées de fer.
            PixelArt.Rect(p, TankLeft - 4, StandBottom, TankRight + 4, TankBottom - 1, S(Pine, 0.85f));
            for (int x = TankLeft - 4; x <= TankRight + 4; x++)
            {
                if (x % 10 == 0) PixelArt.Rect(p, x, StandBottom, x, TankBottom - 1, S(Pine, 0.65f));
                PixelArt.Set(p, x, StandBottom + 6, Iron);
            }

            DrawTank(p);

            // Murs latéraux sombres ; ouverture du passage à gauche.
            PixelArt.Rect(p, Room, 0, Room + 9, Height - 1, C(0.22f, 0.15f, 0.10f));
            PixelArt.Rect(p, Width - 10, 0, Width - 1, Height - 1, C(0.22f, 0.15f, 0.10f));
            PixelArt.Rect(p, Width - 10, 0, Width - 10, StandBottom, Log);

            PixelArt.Rect(p, 0, 0, Room + 9, PassageBottom - 1, C(0.06f, 0.06f, 0.07f));
            PixelArt.Rect(p, 0, PassageTop + 6, Room + 9, Height - 1, C(0.06f, 0.06f, 0.07f));
            for (int x = 0; x <= Room + 9; x++)
            {
                for (int y = PassageBottom; y <= PassageTop; y++) p[x, y] = x % 14 == 0 ? PineDark : Pine;
            }

            PixelArt.Rect(p, 0, PassageTop + 1, Room + 9, PassageTop + 5, Log);
            PixelArt.Rect(p, Room + 10, PassageBottom - 1, Room + 10, PassageTop + 4, Log);
            return p;
        }

        /// <summary>Bassin d'eau froide du fjord : bleu-vert sombre, laminaires, rochers aux algues roses, oursins, étoiles de mer.</summary>
        private static void DrawTank(Color[,] p)
        {
            int left = TankLeft + Frame;
            int right = TankRight - Frame;
            int bottom = TankBottom + Frame;
            PixelArt.Rect(p, TankLeft, TankBottom, TankRight, TankTop, S(Log, 0.7f));
            PixelArt.Rect(p, TankLeft, TankTop - 1, TankRight, TankTop, Log);

            for (int x = left; x <= right; x++)
            {
                for (int y = bottom; y <= TankTop - Frame; y++)
                {
                    Color c;
                    if (y > WaterLine)
                    {
                        c = C(0.10f, 0.11f, 0.13f);
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

            // Fond de galets gris.
            for (int x = left; x <= right; x++)
            {
                int top = bottom + 5 + Mathf.RoundToInt(Mathf.Sin(x * 0.07f) * 2f);
                for (int y = bottom; y <= top; y++) p[x, y] = (x * 5 + y * 3) % 7 == 0 ? C(0.30f, 0.32f, 0.34f) : C(0.46f, 0.47f, 0.48f);
            }

            // Rochers couverts d'algues calcaires roses.
            var random = new System.Random(5151);
            for (int i = 0; i < 4; i++)
            {
                int rx = left + 30 + i * (right - left - 60) / 3 + random.Next(-10, 10);
                int rw = 14 + random.Next(0, 10), rh = 10 + random.Next(0, 8);
                for (int x = -rw; x <= rw; x++)
                {
                    for (int y = 0; y <= rh; y++)
                    {
                        float u = x / (float)rw, v = y / (float)rh;
                        if (u * u + v * v > 1f) continue;
                        Color c = x < -rw / 3 ? C(0.42f, 0.44f, 0.46f) : x > rw / 3 ? C(0.26f, 0.28f, 0.30f) : C(0.34f, 0.36f, 0.38f);
                        if (Mathf.PerlinNoise((rx + x) * 0.3f, (bottom + y) * 0.3f) > 0.6f) c = C(0.66f, 0.40f, 0.50f);
                        PixelArt.Set(p, rx + x, bottom + 4 + y, c);
                    }
                }

                // Un oursin et une étoile de mer sur chaque rocher.
                PixelArt.Rect(p, rx - 2, bottom + 5 + rh, rx + 2, bottom + 7 + rh, C(0.36f, 0.18f, 0.40f));
                PixelArt.Set(p, rx + rw - 4, bottom + 6, C(0.92f, 0.46f, 0.20f));
                PixelArt.Set(p, rx + rw - 5, bottom + 6, C(0.92f, 0.46f, 0.20f));
                PixelArt.Set(p, rx + rw - 3, bottom + 6, C(0.92f, 0.46f, 0.20f));
                PixelArt.Set(p, rx + rw - 4, bottom + 7, C(0.92f, 0.46f, 0.20f));
                PixelArt.Set(p, rx + rw - 4, bottom + 5, C(0.92f, 0.46f, 0.20f));
            }

            // Laminaires : longues lanières brun-olive qui ondulent jusqu'à la surface.
            for (int i = 0; i < 10; i++)
            {
                int kx = left + 10 + random.Next(0, right - left - 20);
                int height = 46 + random.Next(0, 40);
                Color kelp = i % 2 == 0 ? C(0.34f, 0.32f, 0.14f) : C(0.42f, 0.38f, 0.16f);
                for (int y = 0; y < height; y++)
                {
                    int x = kx + Mathf.RoundToInt(Mathf.Sin(y * 0.11f + i) * 3f);
                    PixelArt.Set(p, x, bottom + 4 + y, kelp);
                    PixelArt.Set(p, x + 1, bottom + 4 + y, S(kelp, 0.8f));
                    if (y > height - 14) PixelArt.Rect(p, x - 1, bottom + 4 + y, x + 3, bottom + 4 + y, S(kelp, 1.1f));   // large fronde au bout
                }
            }
        }

        /// <summary>Vitre du bassin : cadre de pin cerclé de fer, reflets obliques, montants. Même taille que la salle.</summary>
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
                    if (!inside) p[x, y] = x % 24 == 0 ? Iron : y >= TankTop - 1 || x < left ? Log : S(Log, 0.8f);
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
                    p[x, y] = C(0.30f, 0.20f, 0.12f, 0.9f);
                    p[x + 1, y] = C(0.48f, 0.34f, 0.20f, 0.9f);
                }
            }

            return p;
        }

        // ---------- Mobilier (style en volume, 16 px par case comme les autres meubles) ----------

        /// <summary>Poêle à bois en fonte noire sur ses pieds, porte vitrée où rougeoient les braises, tuyau qui monte.</summary>
        public static Color[,] Stove()
        {
            var cv = new VolumeCanvas(40, 64);
            cv.Piece(Box(18, 0, 24, 30), Iron, 1.5f);   // tuyau
            cv.Piece(Box(8, 28, 34, 56), Iron, 4f, (x, y, c) => y == 34 || y == 50 ? S(c, 1.3f) : c);
            cv.Piece(Box(13, 38, 29, 50), C(0.96f, 0.52f, 0.18f), 2f, (x, y, c) => y > 46 ? C(0.70f, 0.22f, 0.10f) : (x + y) % 4 == 0 ? C(1f, 0.80f, 0.36f) : c);
            cv.Piece(Box(10, 56, 13, 64), Iron, 1f);
            cv.Piece(Box(29, 56, 32, 64), Iron, 1f);
            return cv.Finish(C(0.05f, 0.04f, 0.04f));
        }

        /// <summary>Tonneau de bois cerclé de fer, un filet de pêche vert jeté dessus avec ses flotteurs.</summary>
        public static Color[,] NetBarrel()
        {
            var cv = new VolumeCanvas(40, 48);
            cv.Piece(Box(8, 8, 32, 46), Log, 4f, (x, y, c) => x % 5 == 0 ? S(c, 0.75f) : y == 14 || y == 40 ? Iron : c);
            cv.Piece(Ellipse(20f, 8f, 12f, 4f), S(Log, 0.8f), 2f);
            cv.Piece((x, y) => y > 4 && y < 34 && x > 4 && x < 36 && ((x + y) % 5 == 0 || (x - y + 40) % 5 == 0) && Mathf.Abs(x - 20f) < 16f - (34 - y) * 0.2f,
                C(0.30f, 0.48f, 0.42f), 1f);
            cv.Piece(Ellipse(10f, 30f, 2.5f, 2.5f), C(0.90f, 0.40f, 0.20f), 1.5f);
            cv.Piece(Ellipse(29f, 24f, 2.5f, 2.5f), C(0.90f, 0.40f, 0.20f), 1.5f);
            return cv.Finish(C(0.08f, 0.06f, 0.04f));
        }

        /// <summary>Lampe-tempête accrochée à un clou, verre bombé et flamme chaude, armature de fer rouge.</summary>
        public static Color[,] StormLantern()
        {
            var cv = new VolumeCanvas(20, 40);
            cv.Piece(Box(9, 0, 11, 8), Iron, 1f);
            cv.Piece(Box(4, 8, 16, 12), C(0.62f, 0.16f, 0.12f), 1.5f);
            cv.Piece(Ellipse(10f, 22f, 6f, 9f), C(1f, 0.86f, 0.52f), 3f, (x, y, c) => x == 4 || x == 16 ? C(0.62f, 0.16f, 0.12f) : c);
            cv.Piece(Box(3, 30, 17, 36), C(0.62f, 0.16f, 0.12f), 2f);
            return cv.Finish(C(0.08f, 0.05f, 0.04f));
        }
    }
}
