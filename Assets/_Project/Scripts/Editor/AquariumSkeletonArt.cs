using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Salle Côte des Squelettes de l'aquarium, même découpage que les autres (passage de 2 cases puis salle de 24 cases,
    /// 416×288 à 16 px par case) : la cale d'un vieux cargo, plancher de pont usé taché de rouille, murs de tôle rivetée
    /// aux hublots ouverts sur la brume ; bassin de l'Atlantique froid, vert-de-gris, forêt de kelp, proue d'une épave au
    /// loin et vertèbre de baleine sur le sable gris. Mobilier : barre à roue, scaphandre, lanterne de navire (style en
    /// volume).
    /// </summary>
    public static class AquariumSkeletonArt
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

        private static readonly Color Deck = C(0.40f, 0.34f, 0.28f);
        private static readonly Color DeckDark = C(0.24f, 0.20f, 0.17f);
        private static readonly Color Plate = C(0.36f, 0.34f, 0.32f);
        private static readonly Color Rust = C(0.52f, 0.26f, 0.14f);
        private static readonly Color Rivet = C(0.20f, 0.18f, 0.17f);
        private static readonly Color DeepSea = C(0.04f, 0.10f, 0.11f);
        private static readonly Color TopSea = C(0.24f, 0.36f, 0.34f);
        private static readonly Color Brass = C(0.72f, 0.56f, 0.26f);

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
        private static Color S(Color c, float light) => DetailedFish.Shade(c, light);

        /// <summary>Passage puis salle Côte des Squelettes, 416×288 (y depuis le bas).</summary>
        public static Color[,] SkeletonRoom()
        {
            var p = new Color[Width, Height];

            // Plancher de pont usé : lames horizontales, clous, taches de rouille.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < StandBottom; y++)
                {
                    int row = y / 8;
                    Color c = y % 8 == 0 ? DeckDark : (row % 2 == 0 ? Deck : S(Deck, 0.92f));
                    if ((x + row * 29) % 56 == 0) c = DeckDark;
                    if ((x + row * 29) % 56 == 3 && y % 8 == 4) c = Rivet;
                    float stain = Mathf.PerlinNoise(x * 0.05f + 3f, y * 0.05f + 1f);
                    if (stain > 0.68f) c = Color.Lerp(c, Rust, 0.35f);
                    p[x, y] = c;
                }
            }

            // Mur du fond : tôles rivetées, coulures de rouille, hublots ouverts sur la mer dans la brume.
            for (int x = Room; x < Width; x++)
            {
                for (int y = StandBottom; y < Height; y++)
                {
                    int px = (x - Room) % 48, py = (y - StandBottom) % 40;
                    Color c = Plate;
                    if (px == 0 || py == 0) c = S(Plate, 0.7f);
                    else if ((px == 3 || px == 45) && py % 6 == 3) c = Rivet;
                    float drip = Mathf.PerlinNoise(x * 0.4f, y * 0.03f);
                    if (drip > 0.7f) c = Color.Lerp(c, Rust, 0.5f);
                    p[x, y] = c;
                }
            }

            // Meuble du bassin en acier riveté.
            PixelArt.Rect(p, TankLeft - 4, StandBottom, TankRight + 4, TankBottom - 1, S(Plate, 0.85f));
            for (int x = TankLeft - 4; x <= TankRight + 4; x++)
            {
                PixelArt.Set(p, x, StandBottom, Rivet);
                if (x % 12 == 0) PixelArt.Set(p, x, StandBottom + 7, Rivet);
            }

            DrawTank(p);

            // Murs latéraux sombres ; ouverture du passage à gauche.
            PixelArt.Rect(p, Room, 0, Room + 9, Height - 1, C(0.18f, 0.16f, 0.15f));
            PixelArt.Rect(p, Width - 10, 0, Width - 1, Height - 1, C(0.18f, 0.16f, 0.15f));
            PixelArt.Rect(p, Width - 10, 0, Width - 10, StandBottom, Plate);

            PixelArt.Rect(p, 0, 0, Room + 9, PassageBottom - 1, C(0.06f, 0.07f, 0.08f));
            PixelArt.Rect(p, 0, PassageTop + 6, Room + 9, Height - 1, C(0.06f, 0.07f, 0.08f));
            for (int x = 0; x <= Room + 9; x++)
            {
                for (int y = PassageBottom; y <= PassageTop; y++) p[x, y] = y % 8 == 0 ? DeckDark : Deck;
            }

            PixelArt.Rect(p, 0, PassageTop + 1, Room + 9, PassageTop + 5, Plate);
            for (int x = 2; x <= Room + 9; x += 6) PixelArt.Set(p, x, PassageTop + 3, Rivet);
            PixelArt.Rect(p, Room + 10, PassageBottom - 1, Room + 10, PassageTop + 4, Plate);
            return p;
        }

        /// <summary>Bassin de l'Atlantique froid : vert-de-gris, forêt de kelp, proue d'épave au loin, sable gris, vertèbre de baleine.</summary>
        private static void DrawTank(Color[,] p)
        {
            int left = TankLeft + Frame;
            int right = TankRight - Frame;
            int bottom = TankBottom + Frame;
            PixelArt.Rect(p, TankLeft, TankBottom, TankRight, TankTop, S(Plate, 0.7f));
            PixelArt.Rect(p, TankLeft, TankTop - 1, TankRight, TankTop, Plate);

            for (int x = left; x <= right; x++)
            {
                for (int y = bottom; y <= TankTop - Frame; y++)
                {
                    Color c;
                    if (y > WaterLine)
                    {
                        c = C(0.10f, 0.11f, 0.12f);
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

            // Proue d'une épave, au loin dans l'eau trouble.
            int sx = left + (right - left) * 3 / 4, sy = bottom + 6;
            for (int x = -40; x <= 30; x++)
            {
                for (int y = 0; y <= 52; y++)
                {
                    float top = x < -10 ? 26f + (x + 40) * 0.9f : 52f - (x + 10) * 0.2f;
                    if (y > top || (x > 22 && y > 20)) continue;
                    int tx = sx + x, ty = sy + y;
                    if (tx < left || tx > right) continue;
                    PixelArt.Set(p, tx, ty, Color.Lerp(p[tx, ty], C(0.02f, 0.05f, 0.05f), 0.55f));
                }
            }

            // Sable gris.
            for (int x = left; x <= right; x++)
            {
                int top = bottom + 5 + Mathf.RoundToInt(Mathf.Sin(x * 0.06f) * 2f);
                for (int y = bottom; y <= top; y++) p[x, y] = (x * 7 + y * 3) % 5 == 0 ? C(0.40f, 0.40f, 0.38f) : C(0.52f, 0.51f, 0.48f);
            }

            // Vertèbre de baleine posée sur le sable.
            int vx = left + (right - left) / 3, vy = bottom + 6;
            for (int x = -14; x <= 14; x++)
            {
                for (int y = 0; y <= 14; y++)
                {
                    float u = x / 14f, v = (y - 5f) / 5f;
                    bool body = u * u + v * v <= 1f;
                    bool spine = Mathf.Abs(x) < 3 && y > 8 && y < 15;
                    bool wings = Mathf.Abs(y - 6) < 2 && Mathf.Abs(x) < 14;
                    if (!body && !spine && !wings) continue;
                    float light = x < -4 ? 1.1f : x > 6 ? 0.78f : 1f;
                    PixelArt.Set(p, vx + x, vy + y, S(C(0.80f, 0.77f, 0.68f), light));
                }
            }

            // Forêt de kelp : longues lanières brunes qui ondulent jusqu'à la surface.
            var random = new System.Random(4141);
            for (int i = 0; i < 9; i++)
            {
                int kx = left + 10 + random.Next(0, right - left - 20);
                int height = 50 + random.Next(0, 40);
                Color kelp = i % 2 == 0 ? C(0.30f, 0.30f, 0.14f) : C(0.38f, 0.34f, 0.16f);
                for (int y = 0; y < height; y++)
                {
                    int x = kx + Mathf.RoundToInt(Mathf.Sin(y * 0.12f + i) * 3f);
                    PixelArt.Set(p, x, bottom + 4 + y, kelp);
                    PixelArt.Set(p, x + 1, bottom + 4 + y, S(kelp, 0.8f));
                    if (y % 9 == 4) PixelArt.Rect(p, x + 2, bottom + 4 + y, x + 5, bottom + 5 + y, S(kelp, 1.15f));
                }
            }
        }

        /// <summary>Vitre du bassin : cadre d'acier riveté, reflets obliques, montants. Même taille que la salle.</summary>
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
                    if (!inside) p[x, y] = (x + y) % 9 == 0 ? Rivet : y >= TankTop - 1 || x < left ? Plate : S(Plate, 0.75f);
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
                    p[x, y] = C(0.22f, 0.20f, 0.19f, 0.9f);
                    p[x + 1, y] = C(0.40f, 0.38f, 0.36f, 0.9f);
                }
            }

            return p;
        }

        // ---------- Mobilier (style en volume, 16 px par case comme les autres meubles) ----------

        /// <summary>Barre à roue de navire en bois sombre et laiton, sur son pied.</summary>
        public static Color[,] ShipWheel()
        {
            var cv = new VolumeCanvas(48, 56);
            Color wood = C(0.40f, 0.26f, 0.16f);
            cv.Piece(Box(20, 30, 28, 54), wood, 2f);
            cv.Piece(Box(12, 50, 36, 56), wood, 2f);
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f;
                cv.Piece(Capsule(24f, 20f, 24f + Mathf.Cos(a) * 19f, 20f + Mathf.Sin(a) * 19f, 1.6f), wood, 1.5f);
            }

            cv.Piece((x, y) => { float d = Mathf.Sqrt((x - 24f) * (x - 24f) + (y - 20f) * (y - 20f)); return d < 15f && d > 11.5f; }, wood, 2f);
            cv.Piece(Ellipse(24f, 20f, 4f, 4f), Brass, 2f);
            return cv.Finish(C(0.10f, 0.07f, 0.05f));
        }

        /// <summary>Vieux scaphandre de cuivre verdi sur un tabouret.</summary>
        public static Color[,] DivingHelmet()
        {
            var cv = new VolumeCanvas(40, 48);
            Color copper = C(0.62f, 0.40f, 0.22f), verdigris = C(0.36f, 0.56f, 0.48f);
            cv.Piece(Union(Box(8, 34, 12, 48), Box(28, 34, 32, 48), Box(6, 30, 34, 36)), C(0.30f, 0.22f, 0.16f), 1.5f);
            cv.Piece(Ellipse(20f, 18f, 14f, 13f), copper, 6f, (x, y, c) => (x * 3 + y * 5) % 11 == 0 ? verdigris : c);
            cv.Piece(Box(4, 26, 36, 32), Brass, 2f);
            cv.Piece(Ellipse(20f, 18f, 6f, 6f), C(0.10f, 0.12f, 0.12f), 2f, (x, y, c) => x == 20 || y == 18 ? Brass : c);
            cv.Piece(Ellipse(9f, 16f, 2.5f, 3f), C(0.10f, 0.12f, 0.12f), 1f);
            cv.Piece(Ellipse(31f, 16f, 2.5f, 3f), C(0.10f, 0.12f, 0.12f), 1f);
            return cv.Finish(C(0.10f, 0.07f, 0.05f));
        }

        /// <summary>Lanterne de navire suspendue à un crochet, flamme pâle derrière une vitre sale.</summary>
        public static Color[,] ShipLantern()
        {
            var cv = new VolumeCanvas(20, 40);
            cv.Piece(Box(8, 2, 11, 40), C(0.22f, 0.20f, 0.19f), 1f);
            cv.Piece(Box(4, 36, 16, 40), C(0.22f, 0.20f, 0.19f), 1.5f);
            cv.Piece(Box(4, 6, 16, 20), C(0.92f, 0.86f, 0.56f), 3f, (x, y, c) => x == 4 || x == 15 || y == 13 ? C(0.30f, 0.26f, 0.22f) : c);
            cv.Piece(Box(3, 4, 17, 7), C(0.30f, 0.26f, 0.22f), 1.5f);
            return cv.Finish(C(0.10f, 0.07f, 0.05f));
        }
    }
}
