using System;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Intérieur de la maison dans le style en volume (<see cref="VolumeCanvas"/>, 32 px par case) : 288×224 (9×7 cases).
    /// Les meubles qui bloquent le passage sont ceux de <see cref="World.HouseBuilding"/> (lit, coffre, table, caisse).
    /// Mur du fond : poutre, papier peint, lambris, carte encadrée, fenêtre sur la mer et rideaux, applique, trophée de
    /// pêche, étagère (livres, bocal, bouteille à la mer), guirlande de flotteurs, canne au mur. Parquet : lumière de la
    /// fenêtre, ombre le long des murs. Lit à la taille du pêcheur (oreiller, couette à carreaux), table de chevet et
    /// lampe, descente de lit, coffre, tapis tissé à franges, table à nappe et deux chaises, grande plante, caisse de
    /// matériel près de la porte. y compté depuis le haut ; le mur du fond occupe le haut, le parquet le bas.
    /// </summary>
    public static class VolumeHouseArt
    {
        public const int Width = 288;
        public const int Height = 224;
        private const int WallBottom = 77;   // limite mur / parquet

        // Lit (en pixels, y depuis le haut) : repris par la couette du dormeur et par HouseBuilding.
        private const int BedLeft = 14, BedRight = 60, BedHead = 62, BedFoot = 140;
        private const float PillowX = 37f, PillowY = 84f;
        private const int BlanketTop = 94;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Red = C(0.80f, 0.24f, 0.24f);
        private static readonly Color Cream = C(0.97f, 0.92f, 0.84f);
        private static readonly Color White = C(0.97f, 0.96f, 0.93f);

        public static Color[,] Interior()
        {
            var cv = new VolumeCanvas(Width, Height);
            Color wood = C(0.70f, 0.48f, 0.27f), woodDark = C(0.46f, 0.29f, 0.15f), woodLight = C(0.80f, 0.58f, 0.34f);
            Color paper = C(0.83f, 0.89f, 0.80f), wainscot = C(0.56f, 0.40f, 0.26f);
            Color gold = C(1f, 0.80f, 0.30f), leaf = C(0.32f, 0.62f, 0.30f), blue = C(0.30f, 0.56f, 0.72f);
            Color rope = C(0.82f, 0.70f, 0.48f), brass = C(0.86f, 0.68f, 0.30f);

            // ---------- Mur du fond : poutre, papier peint à motifs, lambris, plinthe ----------
            cv.Piece(Box(-1, -1, Width + 1, WallBottom - 18), paper, 1.5f,
                (x, y, c) =>
                {
                    Color band = (x / 12) % 2 == 0 ? c : Darker(c, 0.95f);
                    bool motif = (x % 12 == 6) && (y % 14 == 7 || y % 14 == 8);                   // petites feuilles
                    bool dot = (x % 12 == 6) && y % 14 == 0;
                    Color shade = Color.Lerp(band, Darker(band, 0.80f), Mathf.Clamp01((12 - y) / 12f));   // ombre sous le plafond
                    return motif ? C(0.55f, 0.72f, 0.56f) : dot ? C(0.92f, 0.68f, 0.62f) : shade;
                });
            cv.Piece(Box(-1, -1, Width + 1, 5), woodDark, 2f, (x, y, c) => x % 40 == 0 ? Darker(c, 0.7f) : c);   // poutre
            cv.Piece(Box(-1, WallBottom - 19, Width + 1, WallBottom), wainscot, 1.5f,
                (x, y, c) => x % 18 == 0 ? Darker(c, 0.72f) : x % 18 == 1 ? Darker(c, 1.12f) : y == WallBottom - 17 ? Darker(c, 1.18f) : c);
            cv.Piece(Box(-1, WallBottom - 21, Width + 1, WallBottom - 17), woodLight, 2f);                   // cimaise
            cv.Piece(Box(-1, WallBottom - 3, Width + 1, WallBottom + 1), woodDark, 1.5f);                    // plinthe

            // ---------- Parquet : lames décalées, veinage, lumière de la fenêtre, ombre le long des murs ----------
            cv.Piece(Box(-1, WallBottom, Width + 1, Height + 1), wood, 1.2f,
                (x, y, c) =>
                {
                    int row = (y - WallBottom) / 12;
                    if ((y - WallBottom) % 12 == 0) return Darker(c, 0.70f);
                    if ((x + row * 37) % 64 == 0) return Darker(c, 0.72f);
                    Color plank = row % 2 == 0 ? c : Color.Lerp(c, woodLight, 0.35f);
                    plank = (x * 3 + y * 11 + row * 5) % 29 == 0 ? Darker(plank, 0.88f) : plank;
                    float depth = y - WallBottom;
                    float sun = depth < 70 && Mathf.Abs(x - 144f - depth * 0.35f) < 30f - depth * 0.12f ? 1f : 0f;   // tache de soleil
                    plank = Color.Lerp(plank, C(1f, 0.86f, 0.62f), sun * 0.28f);
                    float wall = Mathf.Max(Mathf.Clamp01((10f - depth) / 10f), Mathf.Clamp01((16f - Mathf.Min(x, Width - x)) / 16f));
                    return Color.Lerp(plank, Darker(plank, 0.7f), wall * 0.6f);
                }, 0.9f, 0.8f);

            // Murs de côté et bas de la pièce : épaisseur, avec un liseré clair côté pièce.
            Color wallEdge = C(0.40f, 0.28f, 0.20f);
            cv.Piece(Union(Box(-1, -1, 7, Height + 1), Box(Width - 7, -1, Width + 1, Height + 1)), wallEdge, 2f,
                (x, y, c) => x == 6 || x == Width - 7 ? Darker(c, 1.3f) : c);
            cv.Piece(Box(-1, Height - 5, Width + 1, Height + 1), wallEdge, 2f, (x, y, c) => y == Height - 4 ? Darker(c, 1.3f) : c);
            cv.Piece(Box(126, Height - 12, 160, Height - 4), C(0.74f, 0.54f, 0.32f), 2.5f,
                (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.85f) : c);                                   // paillasson

            // ---------- Fenêtre sur la mer, rideaux, embrasses ----------
            cv.Piece(Box(112, 10, 176, 56), woodLight, 2.5f);
            cv.Piece((x, y) => Box(117, 15, 171, 51)(x, y) && Mathf.Abs(x - 144) > 1 && Mathf.Abs(y - 33) > 1, C(0.62f, 0.84f, 0.96f), 1f,
                (x, y, c) =>
                {
                    if (y < 33) return Color.Lerp(C(0.64f, 0.86f, 0.98f), C(0.90f, 0.97f, 1f), (y - 15) / 18f);
                    if (y == 34) return C(0.86f, 0.95f, 1f);                                   // horizon
                    Color sea = Color.Lerp(C(0.24f, 0.62f, 0.80f), C(0.12f, 0.42f, 0.64f), (y - 34) / 17f);
                    return (x * 5 + y * 3) % 13 == 0 ? C(0.80f, 0.94f, 1f) : sea;              // vaguelettes
                });
            cv.Piece(Ellipse(160f, 23f, 4f, 3f), C(1f, 0.92f, 0.55f), 2f);                  // soleil
            cv.Piece(Union(Ellipse(126f, 22f, 5f, 2f), Ellipse(131f, 21f, 4f, 2.5f)), White, 2f);   // nuage
            cv.Piece(Box(108, 54, 180, 60), woodLight, 3f);                                    // rebord
            cv.Piece(Union(Ellipse(170f, 50f, 4f, 3.5f), Box(166, 50, 174, 55)), C(0.80f, 0.44f, 0.30f), 3f);   // pot
            cv.Piece(Union(Ellipse(167f, 45f, 3f, 2.5f), Ellipse(172f, 44f, 3f, 3f)), leaf, 3f);
            cv.Piece(Ellipse(172f, 42f, 1.5f, 1.5f), C(0.98f, 0.62f, 0.72f), 1.5f);           // fleur
            foreach (float cx in new[] { 108f, 180f })
            {
                cv.Piece(Union(Capsule(cx, 8f, cx, 40f, 6f), Capsule(cx, 40f, cx + (cx < 144 ? 2f : -2f), 62f, 4.5f)), C(0.86f, 0.34f, 0.32f), 5f,
                    (x, y, c) => ((int)x - (int)cx + 20) % 4 == 0 ? Darker(c, 0.82f) : c);   // plis du rideau
                cv.Piece(Ellipse(cx, 41f, 6.5f, 2f), gold, 2f);                                // embrasse
            }

            cv.Piece(Box(100, 5, 188, 9), woodDark, 1.5f);                                     // tringle
            cv.Piece(Union(Ellipse(100f, 7f, 3f, 3f), Ellipse(188f, 7f, 3f, 3f)), brass, 2f);

            // ---------- Carte encadrée au-dessus du lit ----------
            cv.Piece(Box(40, 12, 86, 46), woodDark, 2.5f);
            cv.Piece(Box(44, 16, 82, 42), C(0.95f, 0.89f, 0.72f), 1.5f, (x, y, c) => (x * 7 + y * 3) % 31 == 0 ? Darker(c, 0.9f) : c);
            cv.Piece(Union(Ellipse(60f, 29f, 9f, 6f), Ellipse(68f, 33f, 5f, 4f)), C(0.50f, 0.74f, 0.42f), 3f);   // l'île
            cv.Piece(Ellipse(62f, 28f, 2f, 2f), Red, 1.5f);                                      // « tu es ici »
            for (int k = 0; k < 4; k++) cv.Dot(48 + k * 2, 38 - k, C(0.40f, 0.52f, 0.70f));      // la route du bateau

            // Applique murale entre la carte et la fenêtre.
            cv.Piece(Box(92, 26, 98, 34), brass, 2f);
            cv.Piece(Union(Ellipse(95f, 22f, 5f, 4f), Box(90, 22, 100, 26)), C(1f, 0.88f, 0.58f), 5f, null, 0.95f);

            // ---------- Mur de droite : trophée, étagère, filet ----------
            cv.Piece(Union(Ellipse(222f, 22f, 22f, 11f), Box(200, 20, 244, 26)), woodDark, 3f);            // écusson
            cv.Piece(Union(Ellipse(220f, 22f, 15f, 6f), (x, y) => x > 233 && x < 242 && Mathf.Abs(y - 22f) < (x - 233) * 0.8f), C(0.36f, 0.60f, 0.70f), 5f,
                (x, y, c) => y > 23 ? Color.Lerp(c, White, 0.45f) : (x + y) % 5 == 0 ? Darker(c, 0.82f) : c);      // le poisson
            cv.Piece((x, y) => x > 212 && x < 226 && y > 14 && y < 17 && (x - 212) < (y - 14) * 6, C(0.30f, 0.50f, 0.60f), 3f);   // nageoire
            cv.Dot(209, 21, C(0.08f, 0.06f, 0.08f));
            cv.Piece(Box(196, 44, 256, 49), woodDark, 2f);                                     // étagère
            foreach ((int bx, int top, Color bc) in new[] { (200, 30, Red), (204, 32, blue), (208, 29, C(0.40f, 0.62f, 0.34f)), (212, 33, C(0.90f, 0.74f, 0.36f)) })
            {
                cv.Piece(Box(bx - 2, top, bx + 2, 44), bc, 1.5f);                              // livres
            }

            cv.Piece(Ellipse(228f, 37f, 8f, 7f), C(0.70f, 0.88f, 0.96f, 0.95f), 6f,
                (x, y, c) => y > 34 ? C(0.36f, 0.66f, 0.86f) : c, 0.9f);                       // bocal
            cv.Piece(Ellipse(227f, 39f, 2.5f, 1.5f), C(1f, 0.56f, 0.20f), 2f);                 // poisson rouge
            cv.Piece(Union(Capsule(246f, 42f, 246f, 33f, 3.2f), Capsule(246f, 33f, 246f, 28f, 1.2f)), C(0.40f, 0.70f, 0.52f, 0.9f), 4f);   // bouteille
            cv.Piece(Box(244, 32, 248, 38), C(0.95f, 0.90f, 0.74f), 1.5f);                     // le message

            // Guirlande de flotteurs sur sa corde, au-dessus de la table.
            cv.Piece((x, y) => x > 196 && x < 280 && Mathf.Abs(y - (50f + 6f * Mathf.Sin((x - 196) / 84f * Mathf.PI))) < 1f, rope, 1.5f);   // corde
            foreach (float fx in new[] { 206f, 226f, 248f, 268f })
            {
                cv.Piece(Ellipse(fx, 52f + 6f * Mathf.Sin((fx - 196) / 84f * Mathf.PI), 3f, 3f), fx % 2 == 0 ? Red : White, 4f);
            }

            // Canne à pêche accrochée au mur de gauche.
            cv.Piece(Capsule(8f, 58f, 34f, 6f, 1.1f), woodDark, 1f);
            cv.Piece(Ellipse(14f, 48f, 2.4f, 2.4f), C(0.62f, 0.62f, 0.66f), 2f);

            // ---------- Coffre contre le mur ----------
            cv.Piece(Box(92, 72, 132, 100), woodDark, 2f);
            cv.Piece(Box(94, 74, 130, 98), wood, 4f, (x, y, c) => y == 84 || x == 102 || x == 122 ? Darker(c, 0.7f) : c);
            cv.Piece(Box(108, 82, 116, 90), gold, 2f);
            cv.Piece(Box(96, 68, 104, 74), C(0.56f, 0.40f, 0.62f), 2f);                        // livre posé
            cv.Piece(Union(Ellipse(120f, 70f, 5f, 2.5f), Box(115, 66, 125, 70)), C(0.94f, 0.88f, 0.76f), 3f);   // coquillage

            // ---------- Lit à la taille du pêcheur ----------
            DrawBed(cv, wood, woodDark);

            // Table de chevet et lampe.
            cv.Piece(Box(64, 98, 82, 120), wood, 3f, (x, y, c) => y == 108 ? Darker(c, 0.7f) : c);
            cv.Piece(Ellipse(73f, 113f, 1.5f, 1.5f), brass, 1.5f);
            cv.Piece(Capsule(73f, 86f, 73f, 98f, 1f), C(0.70f, 0.66f, 0.60f), 1f);
            cv.Piece(Union(Ellipse(73f, 84f, 7f, 5f), Box(66, 82, 80, 87)), C(1f, 0.86f, 0.52f), 5f);

            // Descente de lit à rayures.
            cv.Piece(Box(16, 150, 58, 166), C(0.92f, 0.82f, 0.60f), 2f,
                (x, y, c) => (y - 150) % 6 < 2 ? C(0.36f, 0.58f, 0.72f) : (x + y) % 4 == 0 ? Darker(c, 0.9f) : c);

            // ---------- Tapis tissé à franges ----------
            cv.Piece(Box(98, 148, 194, 196), C(0.70f, 0.30f, 0.26f), 2.5f,
                (x, y, c) =>
                {
                    float u = Mathf.Abs(x - 146f) / 48f, v = Mathf.Abs(y - 172f) / 24f;
                    if (u > 0.9f || v > 0.84f) return C(0.93f, 0.83f, 0.58f);                   // bordure
                    if (u > 0.82f || v > 0.72f) return C(0.30f, 0.50f, 0.64f);
                    float d = u + v;
                    Color motif = d < 0.25f ? C(0.93f, 0.83f, 0.58f) : d < 0.45f ? C(0.30f, 0.50f, 0.64f) : (int)(d * 10f) % 2 == 0 ? c : Darker(c, 0.88f);
                    return (x + y) % 3 == 0 ? Darker(motif, 0.92f) : motif;
                }, 0.95f);
            for (int x = 100; x < 194; x += 3)
            {
                cv.Piece(Box(x, 144, x + 1.5f, 148), C(0.93f, 0.83f, 0.58f), 1f);                 // franges
                cv.Piece(Box(x, 196, x + 1.5f, 200), C(0.93f, 0.83f, 0.58f), 1f);
            }

            // ---------- Table à nappe, deux chaises ----------
            foreach (float sx in new[] { 200f, 270f })
            {
                bool left = sx < 236f;
                cv.Piece(Box(sx - 8, 104, sx + 8, 122), wood, 4f, (x, y, c) => (y - 104) % 6 == 0 ? Darker(c, 0.8f) : c);   // assise
                float back = left ? sx - 8 : sx + 8;
                cv.Piece(Box(back - 3, 94, back + 3, 124), woodDark, 3f);                                    // dossier
            }

            cv.Piece(Box(232, 116, 240, 136), woodDark, 2f);                                  // pied
            cv.Piece(Ellipse(236f, 112f, 30f, 14f), White, 6f,
                (x, y, c) => (((int)x / 6) + ((int)y / 6)) % 2 == 0 ? c : C(0.60f, 0.78f, 0.90f));         // nappe à carreaux
            cv.Piece((x, y) => y > 112 && y < 128 && Mathf.Abs(x - 236f) < 30f - (y - 112) * 0.2f && !Ellipse(236f, 112f, 30f, 14f)(x, y), White, 3f,
                (x, y, c) => (((int)x / 6) + ((int)y / 6)) % 2 == 0 ? c : C(0.60f, 0.78f, 0.90f), 0.8f);   // retombée
            cv.Piece(Ellipse(226f, 110f, 7f, 4f), White, 3f);                                  // assiette
            cv.Piece(Ellipse(226f, 109f, 4f, 2f), C(0.92f, 0.58f, 0.40f), 2f);                 // un poisson grillé
            cv.Piece(Ellipse(248f, 108f, 4f, 4f), C(0.88f, 0.88f, 0.86f), 3f);                 // tasse
            cv.Dot(248, 104, C(0.55f, 0.36f, 0.22f));
            cv.Piece(Union(Ellipse(238f, 104f, 3f, 4f), Box(237, 96, 239, 102)), C(0.96f, 0.94f, 0.86f), 3f);   // bougie
            cv.Piece(Ellipse(238f, 95f, 1.5f, 2.2f), C(1f, 0.78f, 0.30f), 1f);

            // ---------- Grande plante en pot dans le coin ----------
            cv.Piece(Union(Box(254, 78, 278, 98), Ellipse(266f, 78f, 13f, 3f)), C(0.80f, 0.44f, 0.30f), 4f,
                (x, y, c) => y == 84 ? Darker(c, 0.8f) : c);
            for (int k = 0; k < 9; k++)
            {
                float a = Mathf.Lerp(200f, 340f, k / 8f) * Mathf.Deg2Rad;
                float len = 18f + (k % 3) * 5f;
                cv.Piece(Capsule(266f, 76f, 266f + Mathf.Cos(a) * len, 76f + Mathf.Sin(a) * len, 3f - (k % 2)), k % 2 == 0 ? leaf : Darker(leaf, 0.85f), 2.5f,
                    (x, y, c) => (x + y) % 5 == 0 ? Darker(c, 1.12f) : c, 0.85f);
            }

            // ---------- Caisse de matériel près de la porte (bottes, épuisette) ----------
            cv.Piece(Box(244, 182, 280, 212), wood, 3f, (x, y, c) => y == 192 || y == 202 || x == 246 || x == 278 ? Darker(c, 0.7f) : c);
            cv.Piece(Capsule(254f, 182f, 268f, 150f, 1.2f), woodDark, 1f);                    // manche d'épuisette
            cv.Piece((x, y) => Ellipse(270f, 146f, 7f, 6f)(x, y) && (((int)x + (int)y) % 3 == 0 || ((int)x - (int)y + 300) % 3 == 0), rope, 2f);
            foreach (float bx in new[] { 222f, 233f })
            {
                cv.Piece(Union(Box(bx - 4, 190, bx + 4, 208), Ellipse(bx + 2f, 208f, 6f, 3f)), C(0.94f, 0.80f, 0.22f), 4f);   // bottes de pluie
            }

            return cv.Finish(C(0.20f, 0.13f, 0.09f), 0.4f);
        }

        /// <summary>Le lit : cadre, tête de lit arrondie, drap, oreiller rebondi, couette à carreaux et drap replié.</summary>
        private static void DrawBed(VolumeCanvas cv, Color wood, Color woodDark)
        {
            cv.Piece(Box(BedLeft, BedHead + 6, BedRight, BedFoot), woodDark, 2.5f);                                       // cadre
            cv.Piece(Union(Box(BedLeft, BedHead, BedRight, BedHead + 14), Ellipse((BedLeft + BedRight) / 2f, BedHead, (BedRight - BedLeft) / 2f, 6f)), wood, 4f,
                (x, y, c) => (x - BedLeft) % 9 == 4 && y > BedHead + 2 ? Darker(c, 0.78f) : c);                           // tête de lit
            cv.Piece(Box(BedLeft + 4, BedHead + 14, BedRight - 4, BedFoot - 4), White, 3f);                                // drap
            cv.Piece(Ellipse(PillowX, PillowY, 15f, 6.5f), White, 7f, null, 0.84f);                                       // oreiller
            Blanket(cv, false);
            cv.Piece(Box(BedLeft - 2, BedFoot, BedRight + 2, BedFoot + 6), woodDark, 2f);                                  // pied du lit
        }

        /// <summary>Couette à carreaux ; <paramref name="sleeper"/> : bombée par le corps du dormeur.</summary>
        private static void Blanket(VolumeCanvas cv, bool sleeper)
        {
            Func<float, float, bool> shape = Box(BedLeft + 2, BlanketTop, BedRight - 2, BedFoot - 2);
            if (sleeper) shape = Union(shape, Ellipse(PillowX, BlanketTop + 22f, 12f, 22f));
            cv.Piece(shape, Red, sleeper ? 10f : 8f,
                (x, y, c) => (((int)(x - BedLeft) / 8) + ((int)(y - BlanketTop) / 8)) % 2 == 0 ? c : Cream, 0.78f);
            cv.Piece(Box(BedLeft + 2, BlanketTop - 2, BedRight - 2, BlanketTop + 4), White, 3f);                           // drap replié
        }

        /// <summary>
        /// Couette du dormeur, seule sur une image transparente de la taille de la pièce (même origine) : posée sur le
        /// pêcheur couché pour ne laisser voir que sa tête sur l'oreiller.
        /// </summary>
        public static Color[,] SleeperBlanket()
        {
            var cv = new VolumeCanvas(Width, Height);
            Blanket(cv, true);
            return cv.Finish(C(0.20f, 0.13f, 0.09f), 0.4f);
        }

        /// <summary>Centre de l'oreiller, en cases depuis le coin bas-gauche de la pièce (là où se pose la tête).</summary>
        public static Vector2 PillowCell => new Vector2(PillowX / PixelsPerUnit, (Height - PillowY) / PixelsPerUnit);
    }
}
