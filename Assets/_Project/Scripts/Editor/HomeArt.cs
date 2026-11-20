using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Intérieurs des maisons d'habitants (<see cref="VillageArt"/>), style en volume (32 px par case), 288×224 px
    /// (9×7 cases) : le mobilier est peint contre le mur du fond et dans les coins, le milieu de la pièce reste libre.
    /// 0 cabane du pêcheur (planches bleu-gris, poêle, filet, trophée, tonneaux, table au poisson grillé),
    /// 1 maison pastel (carrelage, canapé, bibliothèque, jouets de l'enfant, table fleurie),
    /// 3 chaumière (murs de pierre, cheminée, fauteuil à bascule, lit à courtepointe, chat sur le tapis).
    /// Disposition calée sur les obstacles posés par <c>MapSetup</c> (<see cref="Obstacles"/>). y compté depuis le haut.
    /// </summary>
    public static class HomeArt
    {
        public const int Width = 288;
        public const int Height = 224;
        private const int WallBottom = 80;   // limite mur / sol (4,5 cases au-dessus du bas)

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Wood = C(0.62f, 0.42f, 0.24f);
        private static readonly Color WoodDark = C(0.40f, 0.26f, 0.14f);
        private static readonly Color Glass = C(0.66f, 0.86f, 0.92f);
        private static readonly Color Brass = C(0.88f, 0.70f, 0.32f);
        private static readonly Color Leaf = C(0.34f, 0.62f, 0.30f);
        private static readonly Color Cream = C(0.97f, 0.93f, 0.82f);

        /// <summary>
        /// Pieds des meubles infranchissables pour chaque modèle (en cases, relatifs au coin bas-gauche de la pièce)
        /// et place de chaque habitant (pieds), dans le même ordre que <see cref="Residents"/>.
        /// </summary>
        public static Rect[] Obstacles(int variant)
        {
            switch (variant)
            {
                case 0: return new[] { new Rect(0.3f, 3.4f, 1.6f, 1.2f), new Rect(7.1f, 3.3f, 1.6f, 1.3f), new Rect(6.0f, 1.1f, 2.6f, 1.0f) };
                case 1: return new[] { new Rect(0.8f, 3.6f, 3.0f, 1.0f), new Rect(6.8f, 3.6f, 1.9f, 1.0f), new Rect(4.6f, 1.8f, 1.4f, 0.6f) };
                default: return new[] { new Rect(3.4f, 3.6f, 2.2f, 1.0f), new Rect(6.8f, 3.0f, 1.9f, 1.6f), new Rect(0.9f, 2.6f, 1.4f, 0.9f) };
            }
        }

        /// <summary>Habitants de chaque modèle : (nom du dessin, place des pieds en cases dans la pièce).</summary>
        public static (string Name, Vector2 Feet)[] Residents(int variant)
        {
            switch (variant)
            {
                case 0: return new[] { ("Vieux pêcheur", new Vector2(3.2f, 3.0f)) };
                case 1: return new[] { ("Maman", new Vector2(2.6f, 2.8f)), ("Enfant", new Vector2(6.4f, 1.6f)) };
                default: return new[] { ("Grand-mère", new Vector2(2.6f, 2.0f)) };
            }
        }

        public static Color[,] Interior(int variant)
        {
            var cv = new VolumeCanvas(Width, Height);
            switch (variant)
            {
                case 0: FisherCabin(cv); break;
                case 1: PastelHouse(cv); break;
                default: StoneCottage(cv); break;
            }

            // Murs de côté, bas de la pièce et paillasson au milieu.
            Color edge = C(0.30f, 0.24f, 0.20f);
            cv.Piece(Union(Box(-1, -1, 7, Height + 1), Box(Width - 7, -1, Width + 1, Height + 1)), edge, 2f);
            cv.Piece(Box(-1, Height - 4, Width + 1, Height + 1), edge, 2f);
            cv.Piece(Box(128, Height - 10, 160, Height - 3), C(0.76f, 0.56f, 0.36f), 2.5f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.85f) : c);
            return cv.Finish(C(0.15f, 0.11f, 0.09f), 0.4f);
        }

        private static void Window(VolumeCanvas cv, int x0, int y0, int x1, int y1, Color frame, Color sky, Color sea)
        {
            cv.Piece(Box(x0 - 3, y0 - 3, x1 + 3, y1 + 3), frame, 2f);
            int mid = (y0 + y1) / 2 + 3;
            cv.Piece(Box(x0, y0, x1, y1), sky, 1.5f,
                (x, y, c) =>
                {
                    if (x == (x0 + x1) / 2 || y == (y0 + y1) / 2) return frame;
                    if (y > mid) return (x + y) % 7 == 0 ? Color.Lerp(sea, Color.white, 0.4f) : sea;
                    return (x - y) % 13 == 0 ? Color.Lerp(sky, Color.white, 0.6f) : sky;
                });
            cv.Piece(Box(x0 - 5, y1 + 2, x1 + 5, y1 + 6), frame, 2f);   // rebord
        }

        private static void Planks(VolumeCanvas cv, Color floor, int plank = 12)
        {
            cv.Piece(Box(-1, WallBottom, Width + 1, Height + 1), floor, 1.2f,
                (x, y, c) =>
                {
                    int row = (y - WallBottom) / plank;
                    int seam = (x + row * 41) % 72;
                    if ((y - WallBottom) % plank == 0 || seam == 0) return Darker(c, 0.76f);
                    return row % 3 == 1 ? Darker(c, 0.94f) : (x * 3 + y * 5) % 43 == 0 ? Darker(c, 0.88f) : c;
                }, 0.9f, 0.8f);
        }

        private static void Rug(VolumeCanvas cv, float cx, float cy, float rx, float ry, Color a, Color b)
        {
            cv.Piece(Ellipse(cx, cy, rx, ry), a, 3f,
                (x, y, c) =>
                {
                    float u = (x - cx) / rx, v = (y - cy) / ry;
                    int ring = (int)(Mathf.Sqrt(u * u + v * v) * 5f);
                    Color band = ring % 2 == 0 ? c : b;
                    return (x + y) % 3 == 0 ? Darker(band, 0.92f) : band;
                }, 0.92f);
        }

        // ---------- 0. Cabane du pêcheur ----------

        private static void FisherCabin(VolumeCanvas cv)
        {
            Color wall = C(0.52f, 0.62f, 0.70f);
            cv.Piece(Box(-1, -1, Width + 1, WallBottom), wall, 1.5f, (x, y, c) => x % 12 == 0 ? Darker(c, 0.72f) : (x * 7 + y * 3) % 37 == 0 ? Darker(c, 1.1f) : c);
            cv.Piece(Box(-1, WallBottom - 4, Width + 1, WallBottom + 1), WoodDark, 1.5f);
            Planks(cv, C(0.56f, 0.40f, 0.26f));

            // Fenêtre sur la mer, filet de pêche avec ses flotteurs, trophée : un gros poisson sur sa planche.
            Window(cv, 112, 14, 160, 50, WoodDark, C(0.70f, 0.88f, 0.98f), C(0.24f, 0.58f, 0.80f));
            cv.Piece((x, y) => x > 176 && x < 236 && y > 8 && y < 60 && (((int)x + (int)y) % 6 == 0 || ((int)x - (int)y + 300) % 6 == 0), C(0.90f, 0.86f, 0.66f), 0.6f);
            foreach (int fx in new[] { 184, 204, 224 })
            {
                cv.Piece(Ellipse(fx, 58f, 3.5f, 3.5f), C(0.94f, 0.36f, 0.26f), 2f);
            }

            cv.Piece(Box(28, 12, 92, 36), Wood, 2.5f);
            cv.Piece(Union(Ellipse(56f, 24f, 18f, 8f), (x, y) => x > 72 && x < 86 && Mathf.Abs(y - 24f) < (x - 72) * 0.8f), C(0.40f, 0.56f, 0.62f), 4f,
                (x, y, c) => y > 26 ? C(0.86f, 0.88f, 0.84f) : (x + y) % 5 == 0 ? Darker(c, 0.85f) : c);
            cv.Dot(44, 22, C(0.08f, 0.06f, 0.08f));

            // Poêle à bois à gauche, avec son tuyau et une bouilloire.
            cv.Piece(Capsule(28f, 0f, 28f, 50f, 3f), C(0.28f, 0.28f, 0.30f), 2f);
            cv.Piece(Box(10, 48, 58, 110), C(0.26f, 0.26f, 0.30f), 3f, (x, y, c) => y > 80 && y < 98 && x > 22 && x < 46 ? (x % 5 == 0 ? C(0.20f, 0.20f, 0.22f) : y > 90 ? C(1f, 0.84f, 0.36f) : C(0.96f, 0.50f, 0.18f)) : c);
            cv.Piece(Box(14, 42, 54, 50), C(0.36f, 0.36f, 0.40f), 2f);
            cv.Piece(Ellipse(40f, 38f, 7f, 5f), C(0.30f, 0.52f, 0.66f), 3f);

            // Tonneaux et caisse à droite, bouée accrochée au-dessus.
            foreach ((int bx, int by) in new[] { (242, 104), (268, 104) })
            {
                cv.Piece(Box(bx - 11, by - 34, bx + 11, by), Wood, 4f, (x, y, c) => y == by - 26 || y == by - 8 ? C(0.32f, 0.32f, 0.36f) : x % 5 == 0 ? Darker(c, 0.82f) : c);
            }

            cv.Piece(Ellipse(255f, 30f, 11f, 11f), C(0.94f, 0.30f, 0.26f), 3f,
                (x, y, c) => Mathf.Abs(Mathf.Atan2(y - 30f, x - 255f)) % 1.57f < 0.5f ? Cream : c);
            cv.Piece(Ellipse(255f, 30f, 5f, 5f), C(0.52f, 0.62f, 0.70f), 1f);

            // Table de bois dans le coin bas droit : assiette avec un poisson grillé, deux tabourets.
            cv.Piece(Box(212, 168, 270, 178), WoodDark, 2.5f);
            cv.Piece(Box(208, 156, 274, 170), Wood, 3f, (x, y, c) => (x * 3 + y) % 17 == 0 ? Darker(c, 0.85f) : c);
            cv.Piece(Ellipse(238f, 160f, 10f, 4f), Cream, 2f);
            cv.Piece(Union(Ellipse(237f, 159f, 6f, 2f), (x, y) => x > 242 && x < 247 && Mathf.Abs(y - 159f) < (x - 242) * 0.7f), C(0.80f, 0.56f, 0.30f), 1.5f);
            cv.Piece(Ellipse(198f, 176f, 7f, 5f), Wood, 3f);
            cv.Piece(Ellipse(240f, 186f, 7f, 5f), Wood, 3f);
            Rug(cv, 120f, 150f, 44f, 18f, C(0.30f, 0.46f, 0.62f), Cream);
        }

        // ---------- 1. Maison pastel ----------

        private static void PastelHouse(VolumeCanvas cv)
        {
            Color wall = C(0.98f, 0.86f, 0.78f);
            cv.Piece(Box(-1, -1, Width + 1, WallBottom), wall, 1.5f,
                (x, y, c) => y > 52 ? (x % 8 == 0 ? Darker(Cream, 0.85f) : Cream) : y > 49 ? Darker(Cream, 0.8f) : (x / 16 + y / 16) % 2 == 0 && (x + y) % 16 == 0 ? Darker(c, 0.92f) : c);
            cv.Piece(Box(-1, WallBottom - 4, Width + 1, WallBottom + 1), C(0.86f, 0.80f, 0.70f), 1.5f);
            // Carrelage vert d'eau et blanc.
            cv.Piece(Box(-1, WallBottom, Width + 1, Height + 1), C(0.92f, 0.96f, 0.94f), 1.2f,
                (x, y, c) =>
                {
                    int gx = x / 16, gy = (y - WallBottom) / 16;
                    if (x % 16 == 0 || (y - WallBottom) % 16 == 0) return Darker(c, 0.84f);
                    return (gx + gy) % 2 == 0 ? c : C(0.66f, 0.86f, 0.80f);
                }, 0.9f, 0.8f);

            // Fenêtre aux rideaux roses, deux cadres, une plante suspendue.
            Window(cv, 128, 12, 168, 44, C(0.98f, 0.98f, 0.96f), C(0.74f, 0.90f, 0.98f), C(0.30f, 0.66f, 0.84f));
            cv.Piece(Union(Box(118, 8, 128, 52), Box(168, 8, 178, 52)), C(0.98f, 0.62f, 0.70f), 2.5f, (x, y, c) => x % 3 == 0 ? Darker(c, 0.85f) : c);
            cv.Piece(Box(40, 14, 70, 38), Brass, 2f);
            cv.Piece(Box(43, 17, 67, 35), C(0.56f, 0.80f, 0.92f), 1.5f, (x, y, c) => y > 28 ? C(0.40f, 0.70f, 0.40f) : c);
            cv.Piece(Ellipse(58f, 24f, 4f, 4f), C(1f, 0.86f, 0.36f), 1.5f);
            cv.Piece(Box(80, 20, 100, 40), Brass, 2f);
            cv.Piece(Box(83, 23, 97, 37), C(0.98f, 0.92f, 0.86f), 1.5f, (x, y, c) => Ellipse(90f, 30f, 4f, 5f)(x, y) ? C(0.70f, 0.48f, 0.34f) : c);

            // Canapé à gauche, coussins.
            cv.Piece(Box(26, 58, 122, 108), C(0.36f, 0.58f, 0.76f), 4f, (x, y, c) => y > 84 && y < 88 ? Darker(c, 0.8f) : c);
            cv.Piece(Box(22, 64, 32, 108), C(0.30f, 0.50f, 0.68f), 3f);
            cv.Piece(Box(116, 64, 126, 108), C(0.30f, 0.50f, 0.68f), 3f);
            cv.Piece(Ellipse(48f, 74f, 9f, 7f), C(1f, 0.84f, 0.36f), 3f);
            cv.Piece(Ellipse(100f, 74f, 9f, 7f), C(0.98f, 0.60f, 0.66f), 3f);

            // Bibliothèque à droite.
            cv.Piece(Box(218, 6, 278, 112), WoodDark, 2.5f);
            Color[] spines = { C(0.86f, 0.30f, 0.30f), C(0.30f, 0.56f, 0.86f), C(0.98f, 0.80f, 0.30f), C(0.40f, 0.70f, 0.46f), C(0.66f, 0.46f, 0.80f) };
            for (int shelf = 0; shelf < 3; shelf++)
            {
                int top = 10 + shelf * 34;
                cv.Piece(Box(222, top + 28, 274, top + 32), Wood, 1.5f);
                for (int b = 0; b < 7; b++)
                {
                    int bx = 226 + b * 7;
                    int h = 18 + (b * 5 + shelf * 3) % 8;
                    cv.Piece(Box(bx, top + 28 - h, bx + 6, top + 28), spines[(b + shelf) % spines.Length], 1.5f);
                }
            }

            // Table ronde fleurie, jouets de l'enfant (ballon, cubes), tapis.
            Rug(cv, 150f, 168f, 54f, 22f, C(0.98f, 0.70f, 0.60f), Cream);
            cv.Piece(Ellipse(170f, 150f, 22f, 9f), Wood, 4f);
            cv.Piece(Box(166, 154, 174, 170), WoodDark, 2f);
            cv.Piece(Box(164, 132, 176, 146), C(0.66f, 0.86f, 0.94f), 2.5f);
            foreach ((float fx, float fy, Color fc) in new[] { (166f, 128f, C(0.98f, 0.36f, 0.50f)), (172f, 126f, C(1f, 0.86f, 0.30f)), (176f, 130f, C(0.98f, 0.98f, 0.96f)) })
            {
                cv.Piece(Ellipse(fx, fy, 3.5f, 3.5f), fc, 2f);
            }

            cv.Piece(Ellipse(232f, 176f, 8f, 8f), C(0.94f, 0.30f, 0.30f), 4f, (x, y, c) => Mathf.Abs(x - 232f) < 2f ? Cream : c);
            cv.Piece(Box(246, 172, 256, 182), C(0.30f, 0.56f, 0.90f), 2f);
            cv.Piece(Box(258, 176, 268, 186), C(1f, 0.80f, 0.30f), 2f);
            cv.Piece(Box(251, 162, 261, 172), C(0.46f, 0.76f, 0.44f), 2f);
            cv.Piece(Box(14, 150, 30, 172), C(0.82f, 0.56f, 0.40f), 3f);   // pot de plante dans le coin
            cv.Piece(Union(Ellipse(22f, 138f, 12f, 12f), Ellipse(14f, 146f, 8f, 6f), Ellipse(30f, 146f, 8f, 6f)), Leaf, 4f, (x, y, c) => (x + y) % 6 == 0 ? Darker(c, 0.8f) : c);
        }

        // ---------- 3. Chaumière de pierre ----------

        private static void StoneCottage(VolumeCanvas cv)
        {
            Color stone = C(0.68f, 0.66f, 0.62f);
            cv.Piece(Box(-1, -1, Width + 1, WallBottom), stone, 1.5f,
                (x, y, c) =>
                {
                    int row = y / 10;
                    int seam = (x + row * 9) % 18;
                    if (y % 10 == 0 || seam == 0) return Darker(c, 0.70f);
                    return (row + x / 18) % 3 == 0 ? Darker(c, 0.9f) : (row + x / 18) % 3 == 1 ? Darker(c, 1.06f) : c;
                });
            cv.Piece(Box(-1, 4, Width + 1, 12), WoodDark, 2f);   // poutre
            cv.Piece(Box(-1, WallBottom - 4, Width + 1, WallBottom + 1), WoodDark, 1.5f);
            Planks(cv, C(0.70f, 0.50f, 0.30f), 14);

            // Cheminée de pierre au milieu, feu, chaudron, marmite de soupe.
            cv.Piece(Box(104, 10, 184, 112), C(0.56f, 0.54f, 0.50f), 3f, (x, y, c) => (y / 8 + x / 14) % 2 == 0 && x % 14 == 0 ? Darker(c, 0.7f) : y % 8 == 0 ? Darker(c, 0.78f) : c);
            cv.Piece(Box(98, 40, 190, 50), WoodDark, 2.5f);   // manteau
            cv.Piece(Box(118, 62, 170, 110), C(0.12f, 0.10f, 0.10f), 2f);
            cv.Piece(Union(Capsule(126f, 106f, 160f, 102f, 3f), Capsule(128f, 102f, 162f, 106f, 3f)), C(0.46f, 0.30f, 0.18f), 2.5f);   // bûches
            foreach ((float fx, float fh) in new[] { (134f, 9f), (144f, 14f), (154f, 10f) })
            {
                cv.Piece(Ellipse(fx, 100f - fh * 0.5f, 4.5f, fh * 0.6f), C(1f, 0.56f, 0.16f), 3f, (x, y, c) => y > 98f - fh * 0.3f ? c : C(1f, 0.88f, 0.40f));
            }

            cv.Piece(Capsule(144f, 62f, 144f, 70f, 0.8f), C(0.30f, 0.30f, 0.32f), 0.8f);   // crémaillère
            cv.Piece((x, y) => Ellipse(144f, 78f, 11f, 8f)(x, y) && y > 72, C(0.20f, 0.20f, 0.22f), 3f);   // chaudron
            cv.Piece(Box(132, 71, 156, 74), C(0.30f, 0.30f, 0.32f), 1.5f);
            foreach ((int cx2, Color cc) in new[] { (110, C(0.86f, 0.30f, 0.30f)), (174, C(0.30f, 0.52f, 0.80f)) })
            {
                cv.Piece(Box(cx2 - 5, 28, cx2 + 5, 40), cc, 2f);   // pots sur le manteau
            }

            // Fenêtre à petits carreaux, à gauche ; étagère de pots de confiture.
            Window(cv, 32, 14, 72, 48, WoodDark, C(0.80f, 0.92f, 0.98f), C(0.40f, 0.70f, 0.42f));
            cv.Piece(Box(210, 30, 270, 34), Wood, 1.5f);
            for (int j = 0; j < 5; j++)
            {
                cv.Piece(Box(214 + j * 11, 18, 222 + j * 11, 30), Color.Lerp(C(0.86f, 0.30f, 0.40f), C(1f, 0.70f, 0.30f), j / 4f), 2f, (x, y, c) => y < 21 ? Cream : c);
            }

            // Lit à courtepointe à droite.
            cv.Piece(Box(220, 64, 276, 128), WoodDark, 3f);
            cv.Piece(Box(224, 69, 272, 84), Cream, 3f);
            cv.Piece(Box(224, 84, 272, 124), C(0.86f, 0.40f, 0.36f), 3f,
                (x, y, c) => ((x - 224) / 10 + (y - 84) / 10) % 2 == 0 ? c : ((x - 224) / 10) % 3 == 0 ? C(0.96f, 0.84f, 0.50f) : C(0.40f, 0.62f, 0.80f));

            // Fauteuil à bascule et panier à tricot à gauche.
            cv.Piece(Box(30, 86, 72, 98), Wood, 3f);
            cv.Piece(Box(34, 56, 68, 88), C(0.56f, 0.36f, 0.20f), 3f, (x, y, c) => x % 6 == 0 ? Darker(c, 0.8f) : c);
            cv.Piece(Capsule(26f, 112f, 76f, 112f, 2f), WoodDark, 2f);
            cv.Piece(Capsule(34f, 98f, 32f, 110f, 1.5f), WoodDark, 1.5f);
            cv.Piece(Capsule(66f, 98f, 68f, 110f, 1.5f), WoodDark, 1.5f);
            cv.Piece(Ellipse(88f, 112f, 10f, 7f), C(0.78f, 0.62f, 0.36f), 3f, (x, y, c) => (x + y) % 4 == 0 ? Darker(c, 0.8f) : c);
            cv.Piece(Ellipse(85f, 106f, 4f, 4f), C(0.56f, 0.40f, 0.70f), 2f);
            cv.Piece(Ellipse(92f, 107f, 4f, 4f), C(0.40f, 0.70f, 0.50f), 2f);

            // Tapis tressé devant la cheminée, avec un chat noir et blanc qui dort.
            Rug(cv, 144f, 150f, 46f, 18f, C(0.62f, 0.36f, 0.30f), C(0.90f, 0.80f, 0.56f));
            cv.Piece(Ellipse(150f, 146f, 12f, 7f), C(0.16f, 0.14f, 0.16f), 4f, (x, y, c) => x < 146 && y > 145 ? Cream : c);
            cv.Piece(Ellipse(139f, 145f, 6f, 5f), C(0.16f, 0.14f, 0.16f), 3f, (x, y, c) => y > 146 ? Cream : c);
            cv.Piece(Union((x, y) => y > 136 && y < 142 && Mathf.Abs(x - 136f) < (y - 136f) * 0.6f, (x, y) => y > 136 && y < 142 && Mathf.Abs(x - 142f) < (y - 136f) * 0.6f), C(0.16f, 0.14f, 0.16f), 2f);
            cv.Piece(Capsule(160f, 150f, 146f, 153f, 1.5f), C(0.16f, 0.14f, 0.16f), 1.5f);
        }
    }
}
