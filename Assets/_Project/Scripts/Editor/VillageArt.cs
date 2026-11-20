using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Maisons des habitants de l'île, dans le style en volume (<see cref="VolumeCanvas"/>, 32 px par case), 128×128 px
    /// (4 cases de large, plus petites que la maison du joueur). Quatre modèles :
    /// 0 cabane du pêcheur (planches bleues, toit de palmes, filet et bouée),
    /// 1 maison pastel (enduit pêche, tuiles de terre cuite, volets verts, pots de fleurs),
    /// 2 maison jaune à véranda (toit de tôle vert d'eau, rocking-chair),
    /// 3 chaumière de pierre (toit de chaume moussu, porte ronde, lanterne).
    /// y compté depuis le haut ; le pied de la façade est à 6 px du bas.
    /// </summary>
    public static class VillageArt
    {
        public const int Variants = 4;
        public const int Width = 128;
        public const int Height = 128;

        /// <summary>Pied de la façade (la porte).</summary>
        public static readonly Vector2 Pivot = new Vector2(0.5f, 6f / Height);

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Glass = C(0.66f, 0.86f, 0.92f);
        private static readonly Color WoodDark = C(0.40f, 0.26f, 0.14f);
        private static readonly Color Wood = C(0.62f, 0.42f, 0.24f);
        private static readonly Color Brass = C(0.88f, 0.70f, 0.32f);
        private static readonly Color Leaf = C(0.34f, 0.62f, 0.30f);

        /// <summary>Une maison ; <paramref name="tint"/> 1 donne une autre couleur (maison pastel lavande aux volets bleus).</summary>
        public static Color[,] House(int variant, int tint = 0)
        {
            var cv = new VolumeCanvas(Width, Height);
            switch (((variant % Variants) + Variants) % Variants)
            {
                case 0: FisherCabin(cv); break;
                case 1: PastelHouse(cv, tint); break;
                case 2: VerandaHouse(cv); break;
                default: StoneCottage(cv); break;
            }

            Color[,] p = cv.Finish(C(0.15f, 0.11f, 0.08f));

            // Ombre au sol (y depuis le bas).
            for (int x = 2; x < Width - 2; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    if (p[x, y].a < 0.01f) p[x, y] = C(0.05f, 0.15f, 0.08f, 0.3f);
                }
            }

            return p;
        }

        // ---------- Pièces communes ----------

        private static void Window(VolumeCanvas cv, float cx, float cy, float w, float h, Color frame, Color? shutter = null)
        {
            if (shutter.HasValue)
            {
                Color s = shutter.Value;
                cv.Piece(Box(cx - w / 2 - 7, cy - h / 2, cx - w / 2 - 1, cy + h / 2), s, 2f, (x, y, c) => y % 3 == 0 ? Darker(c, 0.8f) : c);
                cv.Piece(Box(cx + w / 2 + 1, cy - h / 2, cx + w / 2 + 7, cy + h / 2), s, 2f, (x, y, c) => y % 3 == 0 ? Darker(c, 0.8f) : c);
            }

            cv.Piece(Box(cx - w / 2 - 2, cy - h / 2 - 2, cx + w / 2 + 2, cy + h / 2 + 2), frame, 2f);
            cv.Piece(Box(cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2), Glass, 2f,
                (x, y, c) =>
                {
                    if (Mathf.Abs(x - cx) < 1f || Mathf.Abs(y - cy) < 1f) return frame;
                    int d = (int)(x - cx + w / 2) - (int)(y - cy + h / 2);
                    return d > 1 && d < 4 ? C(0.94f, 0.99f, 1f) : Color.Lerp(c, C(0.30f, 0.50f, 0.62f), (y - (cy - h / 2)) / h * 0.5f);
                });
        }

        private static void Door(VolumeCanvas cv, float cx, float top, float w, Color color, bool round = false)
        {
            float bottom = 122f;
            System.Func<float, float, bool> shape = round
                ? Union(Box(cx - w / 2, top + w / 2, cx + w / 2, bottom), Ellipse(cx, top + w / 2, w / 2, w / 2))
                : Box(cx - w / 2, top, cx + w / 2, bottom);
            cv.Piece(round ? Union(Box(cx - w / 2 - 2, top + w / 2, cx + w / 2 + 2, bottom), Ellipse(cx, top + w / 2, w / 2 + 2, w / 2 + 2)) : Box(cx - w / 2 - 2, top - 2, cx + w / 2 + 2, bottom),
                WoodDark, 2f);
            cv.Piece(shape, color, 3f, (x, y, c) => Mathf.Abs(x - cx) < 0.6f || (int)y % 9 == 0 ? Darker(c, 0.82f) : c);
            cv.Piece(Ellipse(cx + w / 2 - 3f, (top + bottom) / 2f + 2f, 1.5f, 1.5f), Brass, 1.5f);
        }

        private static void Doorstep(VolumeCanvas cv, float cx, float w, Color color)
        {
            cv.Piece(Box(cx - w / 2 - 4, 120, cx + w / 2 + 4, 126), color, 2f);
        }

        private static void FlowerPot(VolumeCanvas cv, float cx, Color flower)
        {
            cv.Piece(Box(cx - 5, 114, cx + 5, 124), C(0.78f, 0.42f, 0.26f), 3f);
            cv.Piece(Ellipse(cx, 111f, 6f, 5f), Leaf, 3f);
            cv.Piece(Ellipse(cx - 2f, 109f, 1.6f, 1.6f), flower, 1.2f);
            cv.Piece(Ellipse(cx + 2.5f, 111f, 1.6f, 1.6f), flower, 1.2f);
        }

        // ---------- 0. Cabane du pêcheur ----------

        private static void FisherCabin(VolumeCanvas cv)
        {
            Color blue = C(0.36f, 0.58f, 0.80f);
            // Pilotis courts et murs en planches verticales bleues délavées.
            cv.Piece(Box(14, 116, 114, 126), WoodDark, 2f, (x, y, c) => x % 14 < 4 ? c : Color.clear);
            cv.Piece(Box(14, 58, 114, 118), blue, 2.5f, (x, y, c) => x % 7 == 0 ? Darker(c, 0.75f) : (x * 3 + y * 7) % 23 == 0 ? Darker(c, 1.12f) : c);
            Window(cv, 36, 84, 20, 16, C(0.96f, 0.94f, 0.88f));
            Door(cv, 82, 80, 20, C(0.96f, 0.94f, 0.88f));
            Doorstep(cv, 82, 20, Wood);

            // Filet de pêche accroché au mur, bouée rouge et blanche, rame.
            cv.Piece((x, y) => x > 18 && x < 56 && y > 96 && y < 114 && (((int)x + (int)y) % 5 == 0 || ((int)x - (int)y + 200) % 5 == 0), C(0.92f, 0.88f, 0.70f), 0.6f);
            cv.Piece(Ellipse(104f, 92f, 7f, 7f), C(0.92f, 0.28f, 0.26f), 3f,
                (x, y, c) => Mathf.Abs(Mathf.Atan2(y - 92f, x - 104f)) % 1.57f < 0.5f ? C(0.98f, 0.96f, 0.92f) : c);
            cv.Piece(Ellipse(104f, 92f, 3f, 3f), blue, 1f);
            cv.Piece(Capsule(8f, 70f, 12f, 118f, 1.4f), Wood, 1.5f);
            cv.Piece(Ellipse(9f, 116f, 3f, 5f), Wood, 2f);

            // Toit de palmes séchées, épais et débordant.
            cv.Piece((x, y) => y > 22 && y < 64 && Mathf.Abs(x - 64f) < 62f - (64f - y) * 1.05f, C(0.82f, 0.68f, 0.40f), 6f,
                (x, y, c) => (x + y / 3) % 5 == 0 ? Darker(c, 0.72f) : y % 9 == 0 ? Darker(c, 0.85f) : (x * 7) % 11 == 0 ? Darker(c, 1.1f) : c);
            cv.Piece((x, y) => y > 56 && y < 66 && x > 2 && x < 126 && ((int)x % 6) < 4, C(0.74f, 0.58f, 0.32f), 2f);   // franges
        }

        // ---------- 1. Maison pastel ----------

        private static void PastelHouse(VolumeCanvas cv, int tint)
        {
            Color peach = tint == 1 ? C(0.80f, 0.74f, 0.94f) : C(0.98f, 0.78f, 0.66f);
            Color green = tint == 1 ? C(0.30f, 0.50f, 0.80f) : C(0.36f, 0.62f, 0.46f);
            cv.Piece(Box(12, 54, 116, 122), peach, 2.5f, (x, y, c) => (x * 5 + y * 3) % 29 == 0 ? Darker(c, 0.93f) : y > 112 ? Darker(c, 0.94f) : c);
            cv.Piece(Box(10, 114, 118, 124), C(0.92f, 0.88f, 0.80f), 2f);   // soubassement blanc
            Window(cv, 32, 82, 16, 18, C(0.98f, 0.96f, 0.92f), green);
            Window(cv, 96, 82, 16, 18, C(0.98f, 0.96f, 0.92f), green);
            Door(cv, 64, 84, 18, green);
            Doorstep(cv, 64, 18, C(0.86f, 0.84f, 0.80f));
            cv.Piece(Ellipse(64f, 72f, 5f, 4f), C(0.98f, 0.96f, 0.92f), 2f);   // imposte
            FlowerPot(cv, 22, C(0.96f, 0.36f, 0.52f));
            FlowerPot(cv, 106, C(1f, 0.84f, 0.30f));

            // Toit de tuiles de terre cuite en rangées, petite cheminée.
            cv.Piece(Box(92, 14, 104, 40), C(0.86f, 0.84f, 0.80f), 2.5f);
            cv.Piece(Box(90, 10, 106, 16), C(0.62f, 0.56f, 0.52f), 2f);
            cv.Piece((x, y) => y > 18 && y < 58 && Mathf.Abs(x - 64f) < 63f - (58f - y) * 1.2f, C(0.82f, 0.44f, 0.28f), 5f,
                (x, y, c) =>
                {
                    int row = (int)y / 6;
                    if ((int)y % 6 == 5) return Darker(c, 0.72f);
                    return ((int)x + row * 4) % 8 == 0 ? Darker(c, 0.82f) : (int)y % 6 < 2 ? Darker(c, 1.1f) : c;
                });
        }

        // ---------- 2. Maison jaune à véranda ----------

        private static void VerandaHouse(VolumeCanvas cv)
        {
            Color yellow = C(0.98f, 0.86f, 0.46f);
            Color mint = C(0.46f, 0.78f, 0.70f);
            cv.Piece(Box(12, 46, 116, 112), yellow, 2.5f, (x, y, c) => y % 7 == 0 ? Darker(c, 0.84f) : c);   // bardage
            Window(cv, 34, 70, 18, 16, C(0.98f, 0.98f, 0.96f));
            Window(cv, 94, 70, 18, 16, C(0.98f, 0.98f, 0.96f));
            Door(cv, 64, 70, 18, mint);

            // Véranda : plancher, poteaux blancs, rambarde, rocking-chair.
            cv.Piece(Box(6, 110, 122, 124), Wood, 3f, (x, y, c) => x % 10 == 0 ? Darker(c, 0.75f) : c);
            foreach (int px in new[] { 10, 50, 78, 118 })
            {
                cv.Piece(Box(px - 2, 44, px + 2, 112), C(0.98f, 0.98f, 0.96f), 2f);
            }

            cv.Piece((x, y) => y > 98 && y < 112 && (x < 50 || x > 78) && x > 8 && x < 120 && ((int)x % 6 < 2 || y < 101), C(0.98f, 0.98f, 0.96f), 1.2f);
            cv.Piece(Union(Box(24, 90, 40, 94), Box(26, 78, 38, 90)), C(0.70f, 0.48f, 0.26f), 2f);   // rocking-chair
            cv.Piece(Capsule(22f, 106f, 42f, 106f, 1.2f), C(0.56f, 0.36f, 0.20f), 1f);
            cv.Piece(Capsule(26f, 94f, 24f, 105f, 1f), C(0.56f, 0.36f, 0.20f), 1f);
            cv.Piece(Capsule(38f, 94f, 40f, 105f, 1f), C(0.56f, 0.36f, 0.20f), 1f);

            // Toit de tôle ondulée vert d'eau, auvent sur la véranda.
            cv.Piece((x, y) => y > 14 && y < 48 && Mathf.Abs(x - 64f) < 64f - (48f - y) * 1.1f, mint, 5f,
                (x, y, c) => (int)x % 5 == 0 ? Darker(c, 0.74f) : (int)x % 5 == 1 ? Darker(c, 1.12f) : c);
            cv.Piece(Box(2, 40, 126, 48), Darker(mint, 0.85f), 2f, (x, y, c) => (int)x % 5 == 0 ? Darker(c, 0.8f) : c);
        }

        // ---------- 3. Chaumière de pierre ----------

        private static void StoneCottage(VolumeCanvas cv)
        {
            Color stone = C(0.70f, 0.68f, 0.62f);
            cv.Piece(Box(14, 58, 114, 122), stone, 2.5f,
                (x, y, c) =>
                {
                    int row = ((int)y - 58) / 8;
                    int seam = ((int)x + row * 7) % 14;
                    if (((int)y - 58) % 8 == 0 || seam == 0) return Darker(c, 0.70f);
                    return (row + (int)x / 14) % 3 == 0 ? Darker(c, 0.9f) : (row + (int)x / 14) % 3 == 1 ? Darker(c, 1.06f) : c;
                });
            Window(cv, 34, 86, 16, 14, WoodDark);
            Window(cv, 96, 86, 14, 14, WoodDark);
            Door(cv, 64, 80, 20, C(0.56f, 0.34f, 0.20f), round: true);
            Doorstep(cv, 64, 20, C(0.56f, 0.54f, 0.50f));

            // Lanterne à côté de la porte, lierre sur le coin gauche.
            cv.Piece(Box(80, 86, 86, 96), C(0.20f, 0.20f, 0.22f), 2f, (x, y, c) => x > 81 && x < 85 && y > 88 ? C(1f, 0.86f, 0.46f) : c);
            cv.Piece((x, y) => x > 12 && x < 26 && y > 60 && y < 112 && ((int)(x * 3 + y * 5) % 7 < 3), Leaf, 2f);

            // Toit de chaume épais, arrondi, moussu par endroits.
            cv.Piece((x, y) => Ellipse(64f, 62f, 64f, 46f)(x, y) && y < 64 && y > 14, C(0.84f, 0.72f, 0.44f), 8f,
                (x, y, c) =>
                {
                    if (((int)x * 7 + (int)y * 3) % 47 < 6 && y < 50) return C(0.46f, 0.62f, 0.32f);   // mousse
                    return ((int)x + (int)y / 2) % 4 == 0 ? Darker(c, 0.78f) : (int)y % 7 == 0 ? Darker(c, 0.88f) : c;
                });
            cv.Piece(Box(84, 8, 96, 26), stone, 2.5f, (x, y, c) => (int)y % 6 == 0 ? Darker(c, 0.75f) : c);   // cheminée
        }
    }
}
