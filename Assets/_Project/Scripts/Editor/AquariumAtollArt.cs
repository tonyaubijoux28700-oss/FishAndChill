using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Salle Maldives de l'aquarium, même découpage que les autres (passage de 2 cases puis salle de 24 cases, 416×288 à
    /// 16 px par case) : pavillon sur pilotis au plancher de teck, avec deux grands panneaux de verre dans le sol où l'on
    /// voit le lagon (eau turquoise, poissons, reflets) ; murs en rotin tressé ; meuble du bassin en teck sculpté ;
    /// bassin d'océan profond au sable blanc, avec un pâté de corail et la silhouette d'une raie manta au loin.
    /// Mobilier : dhoni (bateau traditionnel) en modèle, lanternes en rotin (dans le style en volume).
    /// </summary>
    public static class AquariumAtollArt
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

        private static readonly Color Teak = C(0.62f, 0.40f, 0.22f);
        private static readonly Color TeakDark = C(0.42f, 0.26f, 0.14f);
        private static readonly Color TeakLight = C(0.74f, 0.52f, 0.30f);
        private static readonly Color Rattan = C(0.86f, 0.72f, 0.48f);
        private static readonly Color RattanDark = C(0.66f, 0.52f, 0.30f);
        private static readonly Color Rope = C(0.82f, 0.70f, 0.48f);
        private static readonly Color DeepOcean = C(0.04f, 0.18f, 0.42f);
        private static readonly Color TopOcean = C(0.18f, 0.62f, 0.84f);

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
        private static Color S(Color c, float light) => DetailedFish.Shade(c, light);

        /// <summary>Passage puis salle Maldives, 416×288 (y depuis le bas).</summary>
        public static Color[,] AtollRoom()
        {
            var p = new Color[Width, Height];

            // Plancher de teck (lames horizontales décalées) avec deux panneaux de verre sur le lagon.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < StandBottom; y++)
                {
                    int row = y / 7;
                    Color c = y % 7 == 0 ? TeakDark : (row % 2 == 0 ? Teak : Color.Lerp(Teak, TeakLight, 0.4f));
                    if ((x + row * 23) % 48 == 0) c = TeakDark;
                    if ((x * 3 + y * 7 + row) % 37 == 0) c = S(c, 0.9f);
                    p[x, y] = c;
                }
            }

            foreach ((int x0, int x1) in new[] { (Room + 34, Room + 150), (Room + 234, Room + 350) })
            {
                DrawGlassFloor(p, x0, 38, x1, 118);
            }

            // Mur du fond en rotin tressé.
            for (int x = Room; x < Width; x++)
            {
                for (int y = StandBottom; y < Height; y++)
                {
                    bool weaveA = ((x / 4) + (y / 4)) % 2 == 0;
                    Color c = weaveA ? Rattan : Color.Lerp(Rattan, RattanDark, 0.45f);
                    if (x % 4 == 0 || y % 4 == 0) c = S(c, 0.86f);
                    p[x, y] = c;
                }
            }

            // Meuble du bassin en teck, frise sculptée en vagues.
            PixelArt.Rect(p, TankLeft - 4, StandBottom, TankRight + 4, TankBottom - 1, Teak);
            for (int x = TankLeft - 4; x <= TankRight + 4; x++)
            {
                int wave = StandBottom + 6 + Mathf.RoundToInt(Mathf.Sin(x * 0.25f) * 2f);
                PixelArt.Set(p, x, wave, TeakLight);
                PixelArt.Set(p, x, wave - 1, TeakDark);
                PixelArt.Set(p, x, StandBottom, TeakDark);
            }

            DrawTank(p);

            // Murs latéraux en teck ; ouverture du passage à gauche.
            PixelArt.Rect(p, Room, 0, Room + 9, Height - 1, TeakDark);
            PixelArt.Rect(p, Width - 10, 0, Width - 1, Height - 1, TeakDark);
            PixelArt.Rect(p, Width - 10, 0, Width - 10, StandBottom, Teak);

            // Passage : murs, plancher, linteau de teck entouré de corde.
            PixelArt.Rect(p, 0, 0, Room + 9, PassageBottom - 1, C(0.10f, 0.12f, 0.16f));
            PixelArt.Rect(p, 0, PassageTop + 6, Room + 9, Height - 1, C(0.10f, 0.12f, 0.16f));
            for (int x = 0; x <= Room + 9; x++)
            {
                for (int y = PassageBottom; y <= PassageTop; y++)
                {
                    p[x, y] = y % 7 == 0 ? TeakDark : Teak;
                }
            }

            PixelArt.Rect(p, 0, PassageTop + 1, Room + 9, PassageTop + 4, TeakLight);
            for (int x = 0; x <= Room + 9; x += 3) PixelArt.Set(p, x, PassageTop + 2, Rope);
            PixelArt.Rect(p, 0, PassageTop + 5, Room + 9, PassageTop + 5, TeakDark);
            PixelArt.Rect(p, Room + 10, PassageBottom - 1, Room + 10, PassageTop + 4, Teak);
            return p;
        }

        /// <summary>Panneau de verre dans le plancher : cadre de teck, lagon turquoise dessous, poissons, reflets.</summary>
        private static void DrawGlassFloor(Color[,] p, int x0, int y0, int x1, int y1)
        {
            PixelArt.Rect(p, x0 - 3, y0 - 3, x1 + 3, y1 + 3, TeakDark);
            PixelArt.Rect(p, x0 - 2, y0 - 2, x1 + 2, y1 + 2, TeakLight);
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    float t = Mathf.InverseLerp(y0, y1, y);
                    Color c = Color.Lerp(C(0.10f, 0.52f, 0.66f), C(0.34f, 0.84f, 0.84f), Mathf.Round(t * 5f) / 5f);
                    if (Mathf.Sin(x * 0.35f + y * 0.6f) > 0.92f) c = Color.Lerp(c, Color.white, 0.4f);   // reflets du soleil
                    if ((x - y + 400) % 40 < 3) c = Color.Lerp(c, Color.white, 0.25f);                    // reflet de la vitre
                    p[x, y] = c;
                }
            }

            var random = new System.Random(x0);
            for (int i = 0; i < 6; i++)
            {
                int fx = random.Next(x0 + 8, x1 - 10), fy = random.Next(y0 + 8, y1 - 8);
                Color fish = i % 3 == 0 ? C(0.98f, 0.82f, 0.30f) : i % 3 == 1 ? C(0.30f, 0.52f, 0.90f) : C(0.96f, 0.96f, 0.96f);
                PixelArt.Rect(p, fx, fy, fx + 4, fy + 1, fish);
                PixelArt.Set(p, fx - 1, fy + 2, fish);
                PixelArt.Set(p, fx - 1, fy - 1, fish);
                PixelArt.Set(p, fx + 4, fy + 1, C(0.08f, 0.08f, 0.10f));
            }

            // Montant du milieu (deux vitres).
            int mid = (x0 + x1) / 2;
            PixelArt.Rect(p, mid - 1, y0, mid + 1, y1, TeakLight);
        }

        /// <summary>Bassin d'océan profond : bleu nuit en bas, turquoise en haut, sable blanc, pâté de corail, raie manta au loin.</summary>
        private static void DrawTank(Color[,] p)
        {
            int left = TankLeft + Frame;
            int right = TankRight - Frame;
            int bottom = TankBottom + Frame;
            PixelArt.Rect(p, TankLeft, TankBottom, TankRight, TankTop, TeakDark);
            PixelArt.Rect(p, TankLeft, TankTop - 1, TankRight, TankTop, TeakLight);

            for (int x = left; x <= right; x++)
            {
                for (int y = bottom; y <= TankTop - Frame; y++)
                {
                    Color c;
                    if (y > WaterLine)
                    {
                        c = C(0.12f, 0.16f, 0.20f);
                    }
                    else
                    {
                        float t = Mathf.InverseLerp(bottom, WaterLine, y);
                        c = Color.Lerp(DeepOcean, TopOcean, Mathf.Round(t * 8f) / 8f);
                        if (y == WaterLine) c = Color.Lerp(TopOcean, Color.white, x % 8 < 4 ? 0.55f : 0.3f);
                    }

                    p[x, y] = c;
                }
            }

            // Silhouette d'une raie manta, au loin dans le bleu.
            int mx = left + (right - left) * 2 / 3, my = bottom + 62;
            for (int x = -26; x <= 26; x++)
            {
                for (int y = -10; y <= 10; y++)
                {
                    float wing = 1f - Mathf.Abs(x) / 26f;
                    if (Mathf.Abs(y + Mathf.Abs(x) * 0.18f - 2f) <= wing * 7f + 1f)
                    {
                        PixelArt.Set(p, mx + x, my + y, Color.Lerp(p[mx + x, my + y], C(0.02f, 0.08f, 0.20f), 0.55f));
                    }
                }
            }

            for (int k = 0; k < 14; k++) PixelArt.Set(p, mx - k / 2, my - 4 - k, Color.Lerp(p[mx - k / 2, my - 4 - k], C(0.02f, 0.08f, 0.20f), 0.45f));   // queue

            // Sable blanc.
            for (int x = left; x <= right; x++)
            {
                int top = bottom + 6 + Mathf.RoundToInt(Mathf.Sin(x * 0.05f) * 2f);
                for (int y = bottom; y <= top; y++)
                {
                    p[x, y] = (x * 7 + y * 3) % 5 == 0 ? C(0.86f, 0.86f, 0.80f) : C(0.96f, 0.95f, 0.88f);
                }
            }

            // Pâté de corail (gros rocher rond) et coraux colorés autour.
            int bx = left + (right - left) / 3, by = bottom + 10;
            for (int x = -22; x <= 22; x++)
            {
                for (int y = 0; y <= 20; y++)
                {
                    float u = x / 22f, v = y / 20f;
                    if (u * u + v * v > 1f) continue;
                    float light = u < -0.3f ? 1.12f : u > 0.4f ? 0.74f : 1f;
                    PixelArt.Set(p, bx + x, by + y, S(C(0.58f, 0.54f, 0.50f), light * (v > 0.7f ? 1.08f : 1f)));
                }
            }

            int floor = bottom + 6;
            var random = new System.Random(919);
            for (int i = 0; i < 16; i++)
            {
                Color[,] coral = ReefArt.Coral(i % 4);
                int cx = i < 5 ? bx - 22 + i * 9 : left + 6 + random.Next(0, right - left - 22);
                int cy = i < 5 ? by + 14 + random.Next(0, 6) : floor - 2 + random.Next(0, 8);
                for (int x = 0; x < 16; x++)
                {
                    for (int y = 0; y < 16; y++)
                    {
                        Color c = coral[x, y];
                        if (c.a < 0.01f) continue;
                        c.a = 1f;
                        PixelArt.Set(p, cx + x, cy + y, S(c, 1.15f));
                    }
                }
            }
        }

        /// <summary>Vitre du bassin : cadre de teck, reflets obliques, montants. Même taille que la salle.</summary>
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
                    if (!inside) p[x, y] = y >= TankTop - 1 || x < left ? TeakLight : TeakDark;
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
                    p[x, y] = C(0.42f, 0.26f, 0.14f, 0.9f);
                    p[x + 1, y] = C(0.74f, 0.52f, 0.30f, 0.9f);
                }
            }

            return p;
        }

        // ---------- Mobilier (style en volume, 16 px par case comme les autres meubles) ----------

        /// <summary>Modèle de dhoni, le bateau traditionnel des Maldives : coque blanche à proue relevée, bande bleue, voile.</summary>
        public static Color[,] Dhoni()
        {
            var cv = new VolumeCanvas(96, 56);
            Color hull = C(0.95f, 0.94f, 0.90f), blue = C(0.20f, 0.46f, 0.78f), wood = C(0.56f, 0.36f, 0.20f), sail = C(0.98f, 0.96f, 0.88f);
            // Socle en bois.
            cv.Piece(Box(14, 46, 82, 54), wood, 2f);
            cv.Piece(Union(Box(24, 40, 28, 47), Box(68, 40, 72, 47)), wood, 1f);
            // Mât et voile.
            cv.Piece(Box(46, 4, 49, 36), wood, 1f);
            cv.Piece((x, y) => y > 6 && y < 32 && x > 49 && x < 49 + (y - 4) * 0.9f, sail, 3f, (x, y, c) => (y % 6 == 0) ? Darker(c, 0.86f) : c);
            // Coque : ventre arrondi, proue qui remonte en volute à droite.
            cv.Piece(Union(Rows(Ellipse(48f, 33f, 38f, 9f), 30f, 42f), Capsule(78f, 34f, 88f, 20f, 3.2f), Ellipse(88f, 18f, 3.4f, 3.4f)), hull, 4f,
                (x, y, c) => y > 34 && y < 38 && x < 82 ? blue : c);
            cv.Piece(Box(14, 29, 80, 32), wood, 1.5f);   // pont
            Color[,] p = cv.Finish(C(0.16f, 0.12f, 0.10f));
            return p;
        }

        /// <summary>Lanterne en rotin suspendue à un poteau : boule tressée qui luit.</summary>
        public static Color[,] RattanLantern()
        {
            var cv = new VolumeCanvas(20, 40);
            Color wood = C(0.50f, 0.32f, 0.18f);
            cv.Piece(Box(8, 2, 11, 40), wood, 1f);
            cv.Piece(Box(4, 36, 16, 40), wood, 1.5f);
            cv.Piece(Ellipse(10f, 12f, 7f, 7.5f), C(1f, 0.82f, 0.46f), 5f,
                (x, y, c) => ((x + y) % 3 == 0 || (x - y + 30) % 3 == 0) ? Darker(c, 0.7f) : c);
            return cv.Finish(C(0.18f, 0.12f, 0.08f));
        }
    }
}
