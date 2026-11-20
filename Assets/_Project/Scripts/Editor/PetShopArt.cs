using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// L'animalerie, dans le style en volume (<see cref="VolumeCanvas"/>, 32 px par case) : façade en bardage crème
    /// et toit de tuiles corail, enseigne ronde à la patte, vitrines avec un chiot et un chat, porte vert d'eau ;
    /// intérieur 11×8 cases (papier peint à pattes, parquet, rayon de croquettes, cage à perroquet, colliers et laisses,
    /// arbre à chat, clapier, panier du chien) ; comptoir en sprite à part (caisse, bocal de friandises, chat endormi)
    /// pour passer devant le joueur. Disposition calée sur les obstacles de <see cref="World.PetShopBuilding"/>.
    /// y compté depuis le haut.
    /// </summary>
    public static class PetShopArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Cream = C(0.97f, 0.91f, 0.76f);
        private static readonly Color Coral = C(0.90f, 0.46f, 0.36f);
        private static readonly Color Mint = C(0.46f, 0.78f, 0.68f);
        private static readonly Color MintDark = C(0.26f, 0.54f, 0.48f);
        private static readonly Color Wood = C(0.62f, 0.42f, 0.24f);
        private static readonly Color WoodDark = C(0.40f, 0.26f, 0.14f);
        private static readonly Color Glass = C(0.66f, 0.86f, 0.92f);
        private static readonly Color Brass = C(0.88f, 0.70f, 0.32f);
        private static readonly Color Puppy = C(0.80f, 0.58f, 0.34f);
        private static readonly Color PuppyDark = C(0.50f, 0.32f, 0.18f);
        private static readonly Color Ginger = C(0.98f, 0.62f, 0.26f);
        private static readonly Color Grey = C(0.62f, 0.64f, 0.70f);
        private static readonly Color Eye = C(0.08f, 0.06f, 0.08f);

        // ---------- Façade ----------

        public const int ExteriorWidth = 160;
        public const int ExteriorHeight = 144;

        /// <summary>Pied de la façade (la porte).</summary>
        public static readonly Vector2 ExteriorPivot = new Vector2(0.5f, 6f / ExteriorHeight);

        public static Color[,] Exterior()
        {
            var cv = new VolumeCanvas(ExteriorWidth, ExteriorHeight);

            // Murs en bardage crème (planches horizontales), soubassement vert d'eau, angles en bois.
            cv.Piece(Box(8, 58, 152, 134), Cream, 2.5f,
                (x, y, c) => y % 8 == 0 ? Darker(c, 0.82f) : y % 8 == 1 ? Darker(c, 1.06f) : (x * 7 + y * 3) % 31 == 0 ? Darker(c, 0.94f) : c);
            cv.Piece(Box(6, 126, 154, 138), MintDark, 2.5f, (x, y, c) => x % 12 == 0 ? Darker(c, 0.8f) : c);
            cv.Piece(Union(Box(6, 58, 12, 136), Box(148, 58, 154, 136)), Wood, 2f);

            // Vitrine de gauche : un chiot pose ses pattes sur le rebord et tire la langue.
            ShopWindow(cv, 16, 84, 58, 120);
            cv.Piece(Ellipse(37f, 112f, 10f, 8f), Puppy, 4f);                                           // corps
            cv.Piece(Ellipse(37f, 101f, 8f, 7f), Puppy, 4f, (x, y, c) => y > 103 && Mathf.Abs(x - 37f) < 4f ? C(0.96f, 0.88f, 0.74f) : c);
            cv.Piece(Union(Ellipse(29f, 101f, 3f, 6f), Ellipse(45f, 101f, 3f, 6f)), PuppyDark, 3f);      // oreilles
            cv.Piece(Ellipse(37f, 104.5f, 2f, 1.4f), Eye, 1f);                                         // truffe
            cv.Piece(Ellipse(38f, 108f, 1.6f, 2f), C(0.96f, 0.46f, 0.52f), 1f);                        // langue
            cv.Dot(34, 100, Eye);
            cv.Dot(40, 100, Eye);
            cv.Piece(Union(Ellipse(31f, 118f, 3f, 2f), Ellipse(43f, 118f, 3f, 2f)), C(0.96f, 0.88f, 0.74f), 2f);   // pattes

            // Vitrine de droite : un chat roux assis à côté d'un bocal à poisson rouge.
            ShopWindow(cv, 102, 84, 144, 120);
            cv.Piece(Ellipse(114f, 112f, 7f, 8f), Ginger, 4f, (x, y, c) => (y + x / 3) % 5 == 0 ? Darker(c, 0.8f) : c);
            cv.Piece(Ellipse(114f, 100f, 6f, 5.5f), Ginger, 4f);
            cv.Piece(Union((x, y) => y > 91 && y < 97 && Mathf.Abs(x - 110f) < (y - 91f) * 0.6f, (x, y) => y > 91 && y < 97 && Mathf.Abs(x - 118f) < (y - 91f) * 0.6f), Ginger, 2f);
            cv.Dot(112, 100, C(0.30f, 0.60f, 0.30f));
            cv.Dot(116, 100, C(0.30f, 0.60f, 0.30f));
            cv.Dot(114, 102, C(0.90f, 0.50f, 0.50f));
            cv.Piece(Capsule(120f, 117f, 124f, 108f, 1.5f), Ginger, 1.5f);                                // queue
            cv.Piece(Ellipse(134f, 110f, 7f, 7f), Color.Lerp(Glass, Color.white, 0.2f), 3f,
                (x, y, c) => y < 106 ? Color.Lerp(c, Color.white, 0.4f) : c);
            cv.Piece(Union(Ellipse(134f, 111f, 2.5f, 1.6f), (x, y) => x > 135.5f && x < 138.5f && Mathf.Abs(y - 111f) < (x - 135.5f) * 0.7f), C(1f, 0.50f, 0.18f), 1.5f);

            // Auvent vert d'eau et blanc à festons au-dessus des vitrines.
            foreach ((int x0, int x1) in new[] { (12, 62), (98, 148) })
            {
                cv.Piece((x, y) => x > x0 && x < x1 && y > 74 && y < 82 + (((int)x - x0) % 10 < 5 ? 2 : 0), Mint, 2.5f,
                    (x, y, c) => ((x - x0) / 5) % 2 == 0 ? c : C(0.98f, 0.97f, 0.94f));
            }

            // Porte vert d'eau, hublot, clochette, poignée de laiton.
            cv.Piece(Box(62, 80, 98, 136), WoodDark, 2f);
            cv.Piece(Box(65, 83, 95, 136), Mint, 3f, (x, y, c) => x == 80 ? Darker(c, 0.75f) : y > 112 && (x == 70 || x == 90) ? Darker(c, 0.85f) : c);
            cv.Piece(Ellipse(80f, 97f, 8f, 8f), Brass, 3f);
            cv.Piece(Ellipse(80f, 97f, 6f, 6f), Glass, 3f, (x, y, c) => x + y < 172 ? C(0.92f, 0.98f, 1f) : c);
            cv.Piece(Ellipse(76f, 118f, 1.8f, 1.8f), Brass, 1.5f);
            cv.Piece(Ellipse(84f, 118f, 1.8f, 1.8f), Brass, 1.5f);
            cv.Piece(Ellipse(80f, 86f, 2.5f, 2f), Brass, 2f);   // clochette

            // Toit de tuiles corail, débordant, rangées en écailles.
            cv.Piece((x, y) => y > 30 && y < 60 && Mathf.Abs(x - 80f) < 78f - (60f - y) * 0.9f, Coral, 5f,
                (x, y, c) =>
                {
                    int row = y / 6;
                    int col = (x + (row % 2) * 5) % 10;
                    if (y % 6 == 5) return Darker(c, 0.72f);
                    return col == 0 ? Darker(c, 0.82f) : y % 6 < 2 ? Darker(c, 1.1f) : c;
                });
            cv.Piece(Box(14, 56, 146, 62), WoodDark, 2f);

            // Enseigne ronde à la patte, posée sur le toit.
            cv.Piece(Ellipse(80f, 40f, 20f, 18f), Brass, 4f);
            cv.Piece(Ellipse(80f, 40f, 16f, 14f), Mint, 3f);
            cv.Piece(Ellipse(80f, 45f, 7f, 5.5f), Cream, 3f);
            foreach ((float tx, float ty) in new[] { (71f, 36f), (76f, 31.5f), (84f, 31.5f), (89f, 36f) })
            {
                cv.Piece(Ellipse(tx, ty, 2.8f, 3.2f), Cream, 2f);
            }

            // Bac à fleurs sous chaque vitrine, gamelle et os près de la porte.
            foreach (int bx in new[] { 37, 123 })
            {
                cv.Piece(Box(bx - 20, 120, bx + 20, 128), Wood, 2.5f);
                for (int i = 0; i < 6; i++)
                {
                    float fx = bx - 16 + i * 6.4f;
                    cv.Piece(Ellipse(fx, 119f, 3f, 2.5f), C(0.36f, 0.64f, 0.32f), 2f);
                    cv.Piece(Ellipse(fx + 1f, 117.5f, 1.4f, 1.4f), i % 2 == 0 ? C(0.98f, 0.56f, 0.66f) : C(1f, 0.86f, 0.36f), 1.2f);
                }
            }

            cv.Piece(Ellipse(106f, 135f, 6f, 2.5f), C(0.86f, 0.28f, 0.30f), 2.5f, (x, y, c) => y < 134 ? C(0.72f, 0.50f, 0.30f) : c);
            cv.Piece(Union(Capsule(52f, 135f, 58f, 135f, 1.2f), Ellipse(51f, 134f, 1.6f, 1.6f), Ellipse(51f, 136f, 1.6f, 1.6f),
                Ellipse(59f, 134f, 1.6f, 1.6f), Ellipse(59f, 136f, 1.6f, 1.6f)), C(0.98f, 0.96f, 0.90f), 1.2f);

            Color[,] p = cv.Finish(C(0.16f, 0.11f, 0.08f));

            // Ombre au sol et paillasson à pattes sous la porte (y depuis le bas).
            for (int x = 2; x < ExteriorWidth - 2; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    if (p[x, y].a < 0.01f) p[x, y] = C(0.05f, 0.15f, 0.08f, 0.3f);
                }
            }

            for (int x = 64; x <= 96; x++)
            {
                for (int y = 0; y <= 4; y++)
                {
                    bool paw = (x == 74 || x == 86) && y == 2 || (x == 73 || x == 75 || x == 85 || x == 87) && y == 3;
                    p[x, y] = y == 0 || y == 4 ? MintDark : paw ? Cream : (x + y) % 3 == 0 ? Darker(Mint, 0.9f) : Mint;
                }
            }

            return p;
        }

        private static void ShopWindow(VolumeCanvas cv, int x0, int y0, int x1, int y1)
        {
            cv.Piece(Box(x0 - 3, y0 - 3, x1 + 3, y1 + 3), WoodDark, 2f);
            cv.Piece(Box(x0, y0, x1, y1), Glass, 2f,
                (x, y, c) =>
                {
                    int d = (x - x0) - (y - y0);
                    return d > 4 && d < 8 ? C(0.92f, 0.98f, 1f) : Color.Lerp(C(0.98f, 0.92f, 0.78f), c, (y - y0) / (float)(y1 - y0) * 0.6f + 0.2f);
                });
        }

        // ---------- Intérieur (352×256 = 11×8 cases) ----------

        public const int InteriorWidth = 352;
        public const int InteriorHeight = 256;
        private const int WallBottom = 96;   // limite mur / parquet (5 cases au-dessus du bas)

        public static Color[,] Interior()
        {
            var cv = new VolumeCanvas(InteriorWidth, InteriorHeight);
            Color paper = C(0.86f, 0.94f, 0.88f);

            // Papier peint vert d'eau semé de petites pattes, lambris de bois en bas du mur.
            cv.Piece(Box(-1, -1, InteriorWidth + 1, WallBottom), paper, 1.5f,
                (x, y, c) =>
                {
                    if (y > 68) return x % 8 == 0 ? Darker(Wood, 0.8f) : Wood;
                    if (y > 64) return Darker(Wood, 0.7f);
                    int cx = x % 24, cy = (y + (x / 24 % 2) * 12) % 24;
                    bool pad = (cx - 12) * (cx - 12) + (cy - 13) * (cy - 13) <= 4;
                    bool toe = (cy == 9 && (cx == 9 || cx == 12 || cx == 15));
                    return pad || toe ? C(0.70f, 0.86f, 0.78f) : c;
                });
            cv.Piece(Box(-1, WallBottom - 4, InteriorWidth + 1, WallBottom + 1), WoodDark, 1.5f);

            // Parquet chaud, lames décalées.
            cv.Piece(Box(-1, WallBottom, InteriorWidth + 1, InteriorHeight + 1), C(0.78f, 0.56f, 0.34f), 1.2f,
                (x, y, c) =>
                {
                    int row = (y - WallBottom) / 12;
                    int seam = (x + row * 37) % 64;
                    if ((y - WallBottom) % 12 == 0 || seam == 0) return Darker(c, 0.78f);
                    return (row % 3 == 1) ? Darker(c, 0.94f) : (x * 3 + y * 5) % 41 == 0 ? Darker(c, 0.88f) : c;
                }, 0.9f, 0.8f);

            // Murs de côté et bas de la pièce, paillasson vert d'eau.
            cv.Piece(Union(Box(-1, -1, 7, InteriorHeight + 1), Box(InteriorWidth - 7, -1, InteriorWidth + 1, InteriorHeight + 1)), C(0.36f, 0.30f, 0.26f), 2f);
            cv.Piece(Box(-1, InteriorHeight - 4, InteriorWidth + 1, InteriorHeight + 1), C(0.36f, 0.30f, 0.26f), 2f);
            cv.Piece(Box(160, InteriorHeight - 10, 192, InteriorHeight - 3), Mint, 2.5f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.85f) : c);

            // ---------- Rayon de croquettes, à gauche : sacs, boîtes et pelotes ----------
            cv.Piece(Box(12, 12, 104, 128), WoodDark, 2.5f);
            cv.Piece(Box(16, 16, 100, 124), C(0.56f, 0.38f, 0.22f), 1.5f);
            Color[] bags = { C(0.92f, 0.36f, 0.30f), C(0.30f, 0.56f, 0.86f), C(0.98f, 0.80f, 0.30f), C(0.50f, 0.76f, 0.44f) };
            for (int shelf = 0; shelf < 3; shelf++)
            {
                int top = 16 + shelf * 36;
                cv.Piece(Box(14, top + 30, 102, top + 35), Wood, 1.5f);
                for (int j = 0; j < 4; j++)
                {
                    float bx = 26f + j * 20f;
                    Color bag = bags[(j + shelf) % bags.Length];
                    if (shelf == 2 && j % 2 == 1)
                    {
                        // Boîtes de pâtée empilées.
                        cv.Piece(Box(bx - 7, top + 18, bx + 7, top + 30), Color.Lerp(bag, Color.white, 0.2f), 2f, (x, y, c) => y == top + 24 ? Darker(c, 0.75f) : c);
                        continue;
                    }

                    cv.Piece(Box(bx - 8, top + 8, bx + 8, top + 30), bag, 3f,
                        (x, y, c) => Mathf.Abs(x - bx) < 4f && y > top + 15 && y < top + 23 ? C(0.98f, 0.96f, 0.90f) : y < top + 11 ? Darker(c, 0.85f) : c);
                    cv.Piece(Ellipse(bx, top + 19f, 2f, 1.6f), bag, 1f);   // patte sur l'étiquette
                }
            }

            // ---------- Cage à perroquet suspendue ----------
            cv.Piece(Capsule(132f, 0f, 132f, 14f, 0.8f), Brass, 1f);
            for (int bx = 116; bx <= 148; bx += 4)
            {
                float half = 18f * Mathf.Sqrt(Mathf.Max(0f, 1f - ((bx - 132f) / 18.5f) * ((bx - 132f) / 18.5f)));
                cv.Piece(Capsule(bx, 40f - half * 1.1f + 2f, bx, 62f, 0.6f), Brass, 0.8f);
            }

            cv.Piece(Box(112, 60, 152, 66), Brass, 2f);
            cv.Piece(Capsule(122f, 46f, 142f, 46f, 0.9f), WoodDark, 1f);   // perchoir
            cv.Piece(Ellipse(132f, 38f, 5f, 8f), C(0.22f, 0.70f, 0.36f), 3f, (x, y, c) => y > 41 ? C(0.96f, 0.80f, 0.24f) : c);
            cv.Piece(Ellipse(132f, 29f, 4f, 4f), C(0.92f, 0.26f, 0.24f), 3f);
            cv.Piece((x, y) => x > 134 && x < 138 && y > 29 && y < 33 && x - 134 < (33 - y), C(0.98f, 0.86f, 0.40f), 1f);
            cv.Dot(133, 28, Eye);
            cv.Piece(Capsule(131f, 45f, 129f, 52f, 1.3f), C(0.20f, 0.46f, 0.80f), 1f);   // queue

            // ---------- Tableau de colliers et laisses ----------
            cv.Piece(Box(166, 14, 226, 56), C(0.82f, 0.68f, 0.50f), 1.5f, (x, y, c) => (x - 166) % 6 == 3 && (y - 14) % 6 == 3 ? Darker(c, 0.6f) : c);
            cv.Piece(Box(164, 12, 228, 15), WoodDark, 1.5f);
            Color[] collars = { C(0.92f, 0.30f, 0.30f), C(0.30f, 0.56f, 0.90f), C(0.98f, 0.76f, 0.24f), C(0.62f, 0.40f, 0.80f) };
            for (int i = 0; i < 4; i++)
            {
                float lx = 174f + i * 14f;
                cv.Piece(Capsule(lx, 20f, lx, 46f - (i % 2) * 6f, 1.2f), collars[i], 1.2f);
                cv.Piece(Ellipse(lx, 20f, 3f, 3f), collars[(i + 1) % 4], 1.5f, (x, y, c) => Mathf.Abs(x - lx) < 1.5f && Mathf.Abs(y - 20f) < 1.5f ? Color.clear : c);
            }

            // ---------- Arbre à chat, avec un chat gris tout en haut ----------
            cv.Piece(Box(240, 128, 290, 138), C(0.80f, 0.72f, 0.60f), 3f);                    // socle moquette
            cv.Piece(Capsule(264f, 30f, 264f, 132f, 5f), C(0.84f, 0.74f, 0.56f), 4f, (x, y, c) => y % 4 == 0 ? Darker(c, 0.8f) : c);   // poteau en corde
            cv.Piece(Box(242, 74, 286, 82), C(0.80f, 0.72f, 0.60f), 3f);
            cv.Piece(Box(250, 26, 282, 34), C(0.80f, 0.72f, 0.60f), 3f);
            cv.Piece(Ellipse(266f, 18f, 11f, 8f), Grey, 4f, (x, y, c) => x > 272 ? Darker(c, 0.85f) : c);
            cv.Piece(Ellipse(258f, 16f, 6f, 5.5f), Grey, 4f);
            cv.Piece(Union((x, y) => y > 7 && y < 13 && Mathf.Abs(x - 255f) < (y - 7f) * 0.6f, (x, y) => y > 7 && y < 13 && Mathf.Abs(x - 261f) < (y - 7f) * 0.6f), Grey, 2f);
            cv.Piece(Capsule(276f, 22f, 282f, 34f, 1.4f), Grey, 1.5f);   // queue qui pend
            cv.Piece(Capsule(254f, 16f, 256f, 16f, 0.5f), Eye, 0.5f);    // yeux mi-clos
            cv.Piece(Capsule(260f, 16f, 262f, 16f, 0.5f), Eye, 0.5f);
            cv.Piece(Ellipse(276f, 84f, 2f, 2f), C(0.92f, 0.30f, 0.30f), 1.5f);   // pompon
            cv.Piece(Capsule(276f, 82f, 276f, 90f, 0.4f), C(0.95f, 0.95f, 0.90f), 0.4f);

            // ---------- Clapier, à droite : un lapin blanc, du foin ----------
            cv.Piece(Box(296, 54, 344, 128), WoodDark, 2.5f);
            cv.Piece(Box(300, 58, 340, 98), C(0.40f, 0.30f, 0.20f), 1.5f);
            cv.Piece(Box(300, 86, 340, 98), C(0.92f, 0.82f, 0.40f), 2f, (x, y, c) => (x * 3 + y) % 5 == 0 ? Darker(c, 0.8f) : c);   // foin
            cv.Piece(Ellipse(316f, 86f, 8f, 6f), C(0.98f, 0.98f, 0.96f), 4f);
            cv.Piece(Ellipse(324f, 80f, 5f, 4.5f), C(0.98f, 0.98f, 0.96f), 4f);
            cv.Piece(Union(Ellipse(322f, 70f, 1.8f, 5f), Ellipse(327f, 70f, 1.8f, 5f)), C(0.98f, 0.96f, 0.94f), 2f,
                (x, y, c) => y > 68 && y < 74 && (Mathf.Abs(x - 322f) < 0.8f || Mathf.Abs(x - 327f) < 0.8f) ? C(0.96f, 0.70f, 0.76f) : c);
            cv.Dot(326, 79, Eye);
            cv.Dot(328, 81, C(0.96f, 0.60f, 0.66f));
            for (int gx = 302; gx < 340; gx += 5)
            {
                cv.Piece(Box(gx, 58, gx + 1, 98), C(0.70f, 0.72f, 0.76f), 0.5f);   // grillage
            }

            cv.Piece(Box(298, 100, 342, 126), Wood, 3f, (x, y, c) => y == 113 ? Darker(c, 0.7f) : c);   // meuble dessous
            cv.Piece(Ellipse(320f, 108f, 2.5f, 1.5f), Brass, 1.5f);

            // ---------- Panier du chien, au sol à gauche, avec un chiot endormi ----------
            cv.Piece(Ellipse(56f, 196f, 34f, 18f), C(0.68f, 0.46f, 0.28f), 5f, (x, y, c) => (x + y * 2) % 7 == 0 ? Darker(c, 0.82f) : c);
            cv.Piece(Ellipse(56f, 198f, 27f, 12f), C(0.92f, 0.46f, 0.44f), 3f, (x, y, c) => (x / 4 + y / 4) % 2 == 0 ? c : C(0.98f, 0.94f, 0.88f));
            cv.Piece(Ellipse(56f, 197f, 14f, 8f), Puppy, 4f);
            cv.Piece(Ellipse(44f, 195f, 7f, 6f), Puppy, 4f);
            cv.Piece(Ellipse(42f, 190f, 3f, 5f), PuppyDark, 2f);
            cv.Piece(Capsule(40f, 195f, 44f, 195f, 0.4f), Eye, 0.4f);   // œil fermé
            cv.Piece(Ellipse(37.5f, 197f, 1.4f, 1.1f), Eye, 0.8f);

            // Tapis rond devant le comptoir.
            cv.Piece(Ellipse(176f, 196f, 58f, 24f), Mint, 3f,
                (x, y, c) =>
                {
                    float u = (x - 176f) / 58f, v = (y - 196f) / 24f;
                    int ring = (int)(Mathf.Sqrt(u * u + v * v) * 5f);
                    Color band = ring % 2 == 0 ? c : Cream;
                    return (x + y) % 3 == 0 ? Darker(band, 0.92f) : band;
                }, 0.92f);

            // Gamelles d'eau et de croquettes près de l'arbre à chat.
            cv.Piece(Ellipse(250f, 186f, 9f, 4f), C(0.30f, 0.56f, 0.86f), 2.5f, (x, y, c) => y < 186 ? C(0.66f, 0.86f, 0.96f) : c);
            cv.Piece(Ellipse(272f, 186f, 9f, 4f), C(0.92f, 0.36f, 0.30f), 2.5f, (x, y, c) => y < 186 ? C(0.62f, 0.40f, 0.22f) : c);

            return cv.Finish(C(0.16f, 0.12f, 0.10f), 0.4f);
        }

        // ---------- Comptoir ----------

        public const int CounterWidth = 128;
        public const int CounterHeight = 72;
        public static readonly Vector2 CounterPivot = new Vector2(0.5f, 2f / CounterHeight);

        /// <summary>Comptoir en bois vert d'eau : caisse, bocal de friandises en os, chat roux endormi en boule.</summary>
        public static Color[,] Counter()
        {
            var cv = new VolumeCanvas(CounterWidth, CounterHeight);
            cv.Piece(Box(6, 40, 122, 70), MintDark, 2.5f, (x, y, c) => (x - 6) % 16 == 0 ? Darker(c, 0.8f) : c);   // façade
            cv.Piece(Ellipse(64f, 55f, 8f, 7f), Cream, 3f);                                                        // patte peinte
            foreach ((float tx, float ty) in new[] { (56f, 48f), (61f, 45f), (67f, 45f), (72f, 48f) })
            {
                cv.Piece(Ellipse(tx, ty, 2f, 2.2f), Cream, 1.5f);
            }

            cv.Piece(Box(2, 28, 126, 42), Wood, 4f, (x, y, c) => (x * 3 + y) % 19 == 0 ? Darker(c, 0.85f) : c);   // plateau

            // Caisse enregistreuse à gauche.
            cv.Piece(Box(12, 12, 40, 32), C(0.86f, 0.30f, 0.30f), 3f);
            cv.Piece(Box(16, 6, 36, 14), C(0.30f, 0.34f, 0.40f), 2f, (x, y, c) => y > 9 && x > 18 && x < 34 ? C(0.60f, 0.90f, 0.66f) : c);
            for (int k = 0; k < 6; k++)
            {
                cv.Piece(Box(16 + (k % 3) * 7, 18 + (k / 3) * 5, 21 + (k % 3) * 7, 21 + (k / 3) * 5), C(0.96f, 0.94f, 0.88f), 1f);
            }

            // Bocal de friandises en os.
            cv.Piece(Box(50, 12, 66, 32), Color.Lerp(Glass, Color.white, 0.3f), 3f, (x, y, c) => x < 54 ? C(0.96f, 0.99f, 1f) : c);
            cv.Piece(Box(49, 8, 67, 13), C(0.86f, 0.30f, 0.30f), 2f);
            foreach ((float ox, float oy) in new[] { (58f, 18f), (56f, 24f), (60f, 28f) })
            {
                cv.Piece(Union(Capsule(ox - 3f, oy, ox + 3f, oy, 1f), Ellipse(ox - 3.5f, oy - 1f, 1.3f, 1.3f), Ellipse(ox - 3.5f, oy + 1f, 1.3f, 1.3f),
                    Ellipse(ox + 3.5f, oy - 1f, 1.3f, 1.3f), Ellipse(ox + 3.5f, oy + 1f, 1.3f, 1.3f)), C(0.90f, 0.76f, 0.52f), 1f);
            }

            // Chat roux endormi en boule, à droite.
            cv.Piece(Ellipse(98f, 24f, 16f, 9f), Ginger, 5f, (x, y, c) => (x / 3 + y / 2) % 4 == 0 ? Darker(c, 0.8f) : c);
            cv.Piece(Ellipse(86f, 24f, 7f, 6f), Ginger, 4f);
            cv.Piece(Union((x, y) => y > 15 && y < 21 && Mathf.Abs(x - 82f) < (y - 15f) * 0.6f, (x, y) => y > 15 && y < 21 && Mathf.Abs(x - 89f) < (y - 15f) * 0.6f), Ginger, 2f);
            cv.Piece(Capsule(82f, 24f, 84f, 24f, 0.4f), Eye, 0.4f);
            cv.Piece(Capsule(87f, 24f, 89f, 24f, 0.4f), Eye, 0.4f);
            cv.Piece(Capsule(110f, 30f, 88f, 31f, 1.6f), Ginger, 1.5f);   // queue enroulée devant
            return cv.Finish(C(0.16f, 0.11f, 0.08f));
        }

        // ---------- Icône de l'inventaire ----------

        public const int PawSize = 20;

        /// <summary>Empreinte de patte crème (coussinet et quatre doigts), pour l'emplacement de l'animal dans l'inventaire.</summary>
        public static Color[,] PawIcon()
        {
            var p = new Color[PawSize, PawSize];
            Color fill = C(0.97f, 0.91f, 0.76f), shade = C(0.80f, 0.70f, 0.54f), ink = C(0.20f, 0.14f, 0.10f);
            void Blob(float cx, float cy, float rx, float ry)
            {
                for (int x = 0; x < PawSize; x++)
                {
                    for (int y = 0; y < PawSize; y++)
                    {
                        float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                        float d = u * u + v * v;
                        if (d <= 1f) p[x, y] = d > 0.55f && v < 0f ? shade : fill;
                    }
                }
            }

            // y depuis le bas : coussinet en bas, doigts au-dessus.
            Blob(10f, 6.5f, 5.2f, 4.4f);
            Blob(3.6f, 11.5f, 2f, 2.4f);
            Blob(7.6f, 15f, 2f, 2.5f);
            Blob(12.4f, 15f, 2f, 2.5f);
            Blob(16.4f, 11.5f, 2f, 2.4f);
            PixelArt.SelectiveOutline(p, ink, 0.6f);
            return p;
        }

        /// <summary>Icône de la minimap (16×16) : petite boutique crème au toit corail, porte vert d'eau, enseigne ronde.</summary>
        public static Color[,] Icon()
        {
            var p = new Color[16, 16];
            PixelArt.Rect(p, 2, 1, 13, 8, Cream);
            PixelArt.Rect(p, 1, 9, 14, 11, Coral);
            PixelArt.Rect(p, 3, 12, 12, 12, Coral);
            PixelArt.Rect(p, 6, 1, 9, 6, Mint);
            PixelArt.Rect(p, 3, 4, 4, 6, Glass);
            PixelArt.Rect(p, 11, 4, 12, 6, Glass);
            PixelArt.Rect(p, 6, 13, 9, 14, Mint);
            PixelArt.Set(p, 7, 13, Cream);
            PixelArt.Outline(p, C(0.16f, 0.11f, 0.08f));
            return p;
        }
    }
}
