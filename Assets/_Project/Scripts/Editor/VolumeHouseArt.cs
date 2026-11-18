using System;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Intérieur de la maison dans le style en volume (<see cref="VolumeCanvas"/>, 32 px par case) : 288×224 (9×7 cases).
    /// Même disposition que l'ancien dessin (les obstacles de <see cref="World.HouseBuilding"/> restent justes) :
    /// lit à gauche, coffre contre le mur, tapis tressé, table ronde à droite, plante dans le coin, fenêtre sur la mer,
    /// carte encadrée, étagère au poisson doré ; en plus, rideaux, lambris, lampe de chevet, canne au mur, livres.
    /// y compté depuis le haut ; le mur du fond occupe le haut, le parquet le bas (4,6 cases).
    /// </summary>
    public static class VolumeHouseArt
    {
        public const int Width = 288;
        public const int Height = 224;
        private const int WallBottom = 77;   // limite mur / parquet

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        public static Color[,] Interior()
        {
            var cv = new VolumeCanvas(Width, Height);
            Color wood = C(0.70f, 0.48f, 0.27f), woodDark = C(0.46f, 0.29f, 0.15f), woodLight = C(0.80f, 0.58f, 0.34f);
            Color paper = C(0.80f, 0.90f, 0.86f), wainscot = C(0.62f, 0.44f, 0.28f), white = C(0.97f, 0.96f, 0.93f);
            Color red = C(0.84f, 0.26f, 0.26f), gold = C(1f, 0.80f, 0.30f), leaf = C(0.32f, 0.62f, 0.30f), blue = C(0.30f, 0.56f, 0.72f);

            // ---------- Mur du fond : papier peint rayé, lambris, plinthe ----------
            cv.Piece(Box(-1, -1, Width + 1, WallBottom - 18), paper, 1.5f,
                (x, y, c) => (x / 8) % 2 == 0 ? Darker(c, 0.94f) : ((x % 8 == 4 && y % 12 == 6) ? C(0.96f, 0.72f, 0.70f) : c));
            cv.Piece(Box(-1, WallBottom - 19, Width + 1, WallBottom), wainscot, 1.5f,
                (x, y, c) => x % 18 == 0 ? Darker(c, 0.72f) : x % 18 == 1 ? Darker(c, 1.12f) : y == WallBottom - 17 ? Darker(c, 1.15f) : c);
            cv.Piece(Box(-1, WallBottom - 3, Width + 1, WallBottom + 1), woodDark, 1.5f);

            // ---------- Parquet : lames décalées, veinage ----------
            cv.Piece(Box(-1, WallBottom, Width + 1, Height + 1), wood, 1.2f,
                (x, y, c) =>
                {
                    int row = (y - WallBottom) / 12;
                    if ((y - WallBottom) % 12 == 0) return Darker(c, 0.70f);
                    if ((x + row * 37) % 64 == 0) return Darker(c, 0.72f);
                    Color plank = row % 2 == 0 ? c : Color.Lerp(c, woodLight, 0.35f);
                    return (x * 3 + y * 11 + row * 5) % 29 == 0 ? Darker(plank, 0.88f) : plank;
                }, 0.9f, 0.8f);

            // Murs de côté et bas de la pièce (épaisseur sombre).
            cv.Piece(Union(Box(-1, -1, 7, Height + 1), Box(Width - 7, -1, Width + 1, Height + 1)), C(0.40f, 0.28f, 0.20f), 2f);
            cv.Piece(Box(-1, Height - 4, Width + 1, Height + 1), C(0.40f, 0.28f, 0.20f), 2f);
            cv.Piece(Box(128, Height - 10, 158, Height - 3), C(0.74f, 0.54f, 0.32f), 2.5f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.85f) : c);   // paillasson

            // ---------- Fenêtre sur la mer, rideaux ----------
            cv.Piece(Box(112, 8, 176, 58), woodLight, 2.5f);
            cv.Piece((x, y) => Box(117, 13, 171, 53)(x, y) && Mathf.Abs(x - 144) > 1 && Mathf.Abs(y - 33) > 1, C(0.62f, 0.84f, 0.96f), 1f,
                (x, y, c) =>
                {
                    if (y < 33) return Color.Lerp(C(0.64f, 0.86f, 0.98f), C(0.90f, 0.97f, 1f), (y - 13) / 20f);
                    if (y == 34) return C(0.86f, 0.95f, 1f);                               // horizon
                    Color sea = Color.Lerp(C(0.24f, 0.62f, 0.80f), C(0.12f, 0.42f, 0.64f), (y - 34) / 19f);
                    return (x * 5 + y * 3) % 13 == 0 ? C(0.80f, 0.94f, 1f) : sea;          // vaguelettes
                });
            cv.Piece(Ellipse(160f, 22f, 4f, 3f), C(1f, 0.92f, 0.55f), 2f);              // soleil
            foreach (float cx in new[] { 110f, 178f })
            {
                cv.Piece(Union(Capsule(cx, 8f, cx, 56f, 6f), Ellipse(cx, 60f, 6.5f, 5f)), C(0.86f, 0.34f, 0.32f), 5f,
                    (x, y, c) => ((int)x - (int)cx + 20) % 4 == 0 ? Darker(c, 0.82f) : c);   // plis du rideau
            }

            cv.Piece(Box(102, 4, 186, 8), woodDark, 1.5f);                                 // tringle

            // ---------- Carte encadrée et étagère au poisson doré ----------
            cv.Piece(Box(42, 14, 90, 54), woodDark, 2.5f);
            cv.Piece(Box(46, 18, 86, 50), C(0.95f, 0.89f, 0.72f), 1.5f, (x, y, c) => (x * 7 + y * 3) % 31 == 0 ? Darker(c, 0.9f) : c);
            cv.Piece(Union(Ellipse(62f, 33f, 9f, 7f), Ellipse(70f, 38f, 5f, 4f)), C(0.50f, 0.74f, 0.42f), 3f);   // une île
            cv.Piece(Ellipse(64f, 31f, 2f, 2f), red, 1.5f);                               // « tu es ici »
            cv.Piece(Box(190, 34, 250, 40), woodDark, 2f);
            cv.Piece(Union(Ellipse(214f, 26f, 13f, 6f), (x, y) => x > 225 && x < 233 && Mathf.Abs(y - 26f) < (x - 225) * 0.9f), gold, 4f,
                (x, y, c) => (x + y) % 4 == 0 && x > 206 && x < 222 ? Darker(c, 0.82f) : c);
            cv.Dot(205, 25, C(0.08f, 0.06f, 0.08f));
            foreach ((int bx, Color bc) in new[] { (238, red), (242, blue), (246, C(0.40f, 0.62f, 0.34f)) })
            {
                cv.Piece(Box(bx - 2, 20, bx + 2, 34), bc, 1.5f);                           // livres
            }

            // Canne à pêche accrochée au mur.
            cv.Piece(Capsule(6f, 60f, 40f, 4f, 1.1f), woodDark, 1f);
            cv.Piece(Ellipse(12f, 50f, 2.4f, 2.4f), C(0.62f, 0.62f, 0.66f), 2f);

            // ---------- Coffre contre le mur ----------
            cv.Piece(Box(92, 72, 132, 100), woodDark, 2f);
            cv.Piece(Box(94, 74, 130, 98), wood, 4f, (x, y, c) => y == 84 || x == 102 || x == 122 ? Darker(c, 0.7f) : c);
            cv.Piece(Box(108, 82, 116, 90), gold, 2f);

            // ---------- Lit : cadre, tête de lit, oreiller rebondi, couette à carreaux ----------
            cv.Piece(Box(14, 66, 82, 154), woodDark, 2.5f);                                // cadre
            cv.Piece(Union(Box(14, 58, 82, 76), Ellipse(48f, 60f, 34f, 8f)), wood, 4f);   // tête de lit arrondie
            cv.Piece(Box(20, 76, 76, 148), white, 3f);                                    // drap
            cv.Piece(Union(Ellipse(48f, 86f, 23f, 7.5f)), white, 7f, null, 0.84f);         // oreiller
            cv.Piece(Box(18, 98, 78, 148), red, 8f,
                (x, y, c) => (((x - 18) / 10) + ((y - 98) / 10)) % 2 == 0 ? c : C(0.97f, 0.92f, 0.84f), 0.78f);   // couette
            cv.Piece(Box(18, 96, 78, 102), white, 3f);                                    // drap replié
            cv.Piece(Box(12, 150, 84, 156), woodDark, 2f);                                // pied du lit

            // Table de chevet et lampe.
            cv.Piece(Box(84, 104, 98, 124), wood, 3f);
            cv.Piece(Capsule(91f, 92f, 91f, 104f, 1f), C(0.70f, 0.66f, 0.60f), 1f);
            cv.Piece(Union(Ellipse(91f, 90f, 7f, 5f), Box(84, 88, 98, 93)), C(1f, 0.86f, 0.52f), 5f);

            // ---------- Tapis rond tressé ----------
            cv.Piece(Ellipse(146f, 166f, 56f, 32f), C(0.30f, 0.56f, 0.70f), 3f,
                (x, y, c) =>
                {
                    float u = (x - 146f) / 56f, v = (y - 166f) / 32f;
                    int ring = (int)(Mathf.Sqrt(u * u + v * v) * 7f);
                    Color braid = ring % 2 == 0 ? c : C(0.93f, 0.83f, 0.58f);
                    return (x + y) % 3 == 0 ? Darker(braid, 0.88f) : braid;
                }, 0.92f);

            // ---------- Table ronde, tabourets, tasse et assiette ----------
            foreach (float sx in new[] { 200f, 272f })
            {
                cv.Piece(Ellipse(sx, 120f, 7f, 7f), woodDark, 4f);
            }

            cv.Piece(Box(232, 116, 240, 134), woodDark, 2f);                              // pied
            cv.Piece(Ellipse(236f, 112f, 30f, 14f), woodLight, 6f, (x, y, c) => (x * 3 + y) % 17 == 0 ? Darker(c, 0.85f) : c);
            cv.Piece(Ellipse(226f, 110f, 7f, 4f), white, 3f);
            cv.Piece(Ellipse(248f, 108f, 4f, 4f), C(0.88f, 0.88f, 0.86f), 3f);
            cv.Dot(248, 104, C(0.55f, 0.36f, 0.22f));

            // ---------- Grande plante en pot dans le coin ----------
            cv.Piece(Union(Box(250, 78, 274, 98), Ellipse(262f, 78f, 13f, 3f)), C(0.80f, 0.44f, 0.30f), 4f);
            for (int k = 0; k < 9; k++)
            {
                float a = Mathf.Lerp(200f, 340f, k / 8f) * Mathf.Deg2Rad;
                float len = 20f + (k % 3) * 5f;
                cv.Piece(Capsule(262f, 76f, 262f + Mathf.Cos(a) * len, 76f + Mathf.Sin(a) * len, 3f - (k % 2)), k % 2 == 0 ? leaf : Darker(leaf, 0.85f), 2.5f,
                    (x, y, c) => (x + y) % 5 == 0 ? Darker(c, 1.12f) : c, 0.85f);
            }

            return cv.Finish(C(0.20f, 0.13f, 0.09f), 0.4f);
        }
    }
}
