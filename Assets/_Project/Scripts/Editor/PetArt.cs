using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les animaux de compagnie de l'animalerie, à l'échelle des personnages (16 px par case, images 20×18, pivot aux
    /// pattes) : chat roux tigré, bouledogue français fauve (oreilles de chauve-souris, masque sombre), perroquet vert qui vole. Pour chacun 3 directions
    /// (face, dos, profil vers la droite ; la gauche est le miroir) et 3 images (0 à l'arrêt, 1 et 2 en marche ou en vol).
    /// y compté depuis le haut pendant le dessin.
    /// </summary>
    public static class PetArt
    {
        public const int Width = 20;
        public const int Height = 18;
        public static readonly Vector2 Pivot = new Vector2(0.5f, 1.5f / Height);

        public enum Kind { Cat, Dog, Parrot }
        public enum Facing { Down, Up, Side }

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Eye = C(0.08f, 0.06f, 0.08f);
        private static readonly Color White = C(0.97f, 0.95f, 0.90f);
        private static readonly Color Pink = C(0.95f, 0.55f, 0.60f);
        private static readonly Color Shadow = C(0.05f, 0.10f, 0.06f, 0.28f);

        private static readonly Color CatFur = C(0.96f, 0.60f, 0.24f);
        private static readonly Color CatStripe = C(0.78f, 0.40f, 0.14f);
        private static readonly Color DogFur = C(0.84f, 0.62f, 0.38f);   // fauve
        private static readonly Color Mask = C(0.30f, 0.23f, 0.20f);
        private static readonly Color Feather = C(0.26f, 0.72f, 0.34f);
        private static readonly Color Wing = C(0.18f, 0.48f, 0.82f);
        private static readonly Color Beak = C(0.98f, 0.78f, 0.24f);
        private static readonly Color Crest = C(0.92f, 0.26f, 0.22f);

        public static Color[,] Draw(Kind kind, Facing facing, int frame)
        {
            var cv = new VolumeCanvas(Width, Height);
            switch (kind)
            {
                case Kind.Cat: Cat(cv, facing, frame); break;
                case Kind.Dog: Bulldog(cv, facing, frame); break;
                default: Parrot(cv, facing, frame); break;
            }

            Color[,] p = cv.Finish(C(0.14f, 0.09f, 0.08f), 0.6f);

            // Ombre douce au sol (le perroquet vole : son ombre est plus petite).
            float rx = kind == Kind.Parrot ? 3f : 6f;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    float u = (x + 0.5f - Width / 2f) / rx, v = (y + 0.5f - 0.8f) / 1.2f;
                    if (u * u + v * v <= 1f && p[x, y].a < 0.01f) p[x, y] = Shadow;
                }
            }

            return p;
        }

        // ---------- Chat ----------

        private static void Cat(VolumeCanvas cv, Facing facing, int frame)
        {
            Color fur = CatFur;
            System.Func<int, int, Color, Color> stripes = (x, y, c) => (x + y / 2) % 4 == 0 ? CatStripe : c;
            int lift1 = frame == 1 ? 1 : 0, lift2 = frame == 2 ? 1 : 0;

            if (facing == Facing.Side)
            {
                cv.Piece(Union(Capsule(5f, 11f, 3f, 6f, 1.1f), Capsule(3f, 6f, 4f, 3.5f, 1.1f)), fur, 1.2f, stripes);   // queue dressée
                foreach ((float lx, int lift, bool far) in new[] { (7f, lift2, true), (13f, lift1, true), (8.5f, lift1, false), (14.5f, lift2, false) })
                {
                    cv.Piece(Capsule(lx, 12f, lx + (lift > 0 ? 0.8f : 0f), 15.5f - lift, 1f), far ? Darker(fur, 0.75f) : fur, 1f);
                }

                cv.Piece(Ellipse(10.5f, 11f, 5.5f, 3.2f), fur, 2.5f, stripes);
                cv.Piece(Ellipse(15.5f, 7.5f, 3.2f, 3f), fur, 2f, stripes);
                cv.Piece((x, y) => y > 3 && y < 6 && Mathf.Abs(x - 14.5f) < (y - 3f) * 0.7f, fur, 1f);
                cv.Piece((x, y) => y > 3 && y < 6 && Mathf.Abs(x - 17f) < (y - 3f) * 0.7f, fur, 1f);
                cv.Piece(Ellipse(17.6f, 8.8f, 1.4f, 1.1f), White, 1f);
                cv.Dot(16, 7, Eye);
                return;
            }

            bool back = facing == Facing.Up;
            if (back) cv.Piece(Union(Capsule(12f, 13f, 15f, 10f, 1.1f), Capsule(15f, 10f, 15.5f, 6.5f, 1.1f)), fur, 1.2f, stripes);
            else cv.Piece(Union(Capsule(14.5f, 13f, 15.5f, 8f, 1.1f), Capsule(15.5f, 8f, 14.5f, 6f, 1.1f)), fur, 1.2f, stripes);

            foreach ((float lx, int lift) in new[] { (8f, lift1), (12f, lift2) })
            {
                cv.Piece(Capsule(lx, 13.5f, lx, 16f - lift, 1.1f), fur, 1f);
            }

            cv.Piece(Ellipse(10f, 12.5f, 3.8f, 3f), back ? Darker(fur, 0.92f) : fur, 2.5f, stripes);
            if (!back) cv.Piece(Ellipse(10f, 12.8f, 1.7f, 2f), White, 1.5f);   // poitrail clair
            cv.Piece(Ellipse(10f, back ? 8f : 7.5f, 3.8f, 3.3f), fur, 2f, stripes);
            cv.Piece((x, y) => y > 2.5f && y < 5.5f && Mathf.Abs(x - 7.5f) < (y - 2.5f) * 0.7f, fur, 1f);
            cv.Piece((x, y) => y > 2.5f && y < 5.5f && Mathf.Abs(x - 12.5f) < (y - 2.5f) * 0.7f, fur, 1f);
            if (back) return;
            cv.Piece(Ellipse(10f, 9.3f, 1.8f, 1.4f), White, 1f);   // museau
            cv.Dot(8, 7, Eye);
            cv.Dot(12, 7, Eye);
            cv.Dot(10, 9, Pink);
        }

        // ---------- Bouledogue français ----------

        /// <summary>Trapu, pattes courtes, grandes oreilles de chauve-souris dressées, face plate au masque sombre, robe fauve.</summary>
        private static void Bulldog(VolumeCanvas cv, Facing facing, int frame)
        {
            Color fur = DogFur;
            int lift1 = frame == 1 ? 1 : 0, lift2 = frame == 2 ? 1 : 0;
            if (facing == Facing.Side)
            {
                cv.Piece(Ellipse(4.6f, 10.5f + (frame == 1 ? -0.5f : 0f), 1.2f, 1.1f), fur, 1f);   // queue en tire-bouchon
                foreach ((float lx, int lift, bool far) in new[] { (7f, lift2, true), (13f, lift1, true), (8.5f, lift1, false), (14.5f, lift2, false) })
                {
                    cv.Piece(Capsule(lx, 13f, lx + (lift > 0 ? 0.8f : 0f), 15.5f - lift, 1.2f), far ? Darker(fur, 0.75f) : fur, 1f);
                }

                cv.Piece(Ellipse(10.5f, 11.5f, 5.6f, 3.6f), fur, 3f);
                cv.Piece(Ellipse(12f, 13.2f, 3f, 1.5f), White, 1.5f);                     // poitrail
                cv.Piece(Ellipse(15.5f, 8.2f, 3.5f, 3.3f), fur, 2.5f);                    // grosse tête ronde
                cv.Piece(Ellipse(14.2f, 4.8f, 1.7f, 1.9f), fur, 1.2f, (x, y, c) => y > 4 && Mathf.Abs(x - 14.2f) < 0.7f ? Pink : c);   // oreille dressée, arrondie
                cv.Piece(Ellipse(18.2f, 9.4f, 1.5f, 1.4f), Mask, 1.2f);                   // museau court et sombre
                cv.Dot(19, 9, Eye);
                cv.Dot(16, 7, Eye);
                return;
            }

            bool back = facing == Facing.Up;
            if (back) cv.Piece(Ellipse(10f, 11.5f, 1.2f, 1f), fur, 1f);   // queue
            foreach ((float lx, int lift) in new[] { (7.5f, lift1), (12.5f, lift2) })
            {
                cv.Piece(Capsule(lx, 14f, lx, 16f - lift, 1.3f), fur, 1f);
            }

            cv.Piece(Ellipse(10f, 12.8f, 4.4f, 3.2f), back ? Darker(fur, 0.92f) : fur, 3f);
            if (!back) cv.Piece(Ellipse(10f, 13.4f, 2f, 1.8f), White, 1.5f);   // poitrail
            // Grandes oreilles de chauve-souris, intérieur rose de face.
            foreach (float side in new[] { -1f, 1f })
            {
                float ex = 10f + side * 3.9f;
                cv.Piece(Ellipse(ex, 4.4f, 1.9f, 2.1f), fur, 1.2f, back ? null : (System.Func<int, int, Color, Color>)((x, y, c) => y > 3 && Mathf.Abs(x - ex) < 0.8f ? Pink : c));
            }

            cv.Piece(Ellipse(10f, back ? 8.4f : 8f, 4.7f, 3.4f), fur, 2.5f);   // tête large
            if (back) return;
            cv.Piece(Ellipse(10f, 9.6f, 2.4f, 1.5f), Mask, 1.2f);                // face plate, masque sombre
            cv.Dot(8, 7, Eye);
            cv.Dot(12, 7, Eye);
            cv.Dot(10, 9, Eye);
            cv.Dot(10, 11, Pink);   // langue
        }

        // ---------- Perroquet (vole à hauteur d'épaule, ailes battantes) ----------

        private static void Parrot(VolumeCanvas cv, Facing facing, int frame)
        {
            float lift = frame == 1 ? -1f : 0f;   // petit coup d'aile vers le haut
            bool wingsUp = frame == 1;
            if (facing == Facing.Side)
            {
                cv.Piece(Union(Capsule(6f, 9f + lift, 2.5f, 12f + lift, 1.2f), Capsule(6f, 9.5f + lift, 3f, 13.5f + lift, 0.9f)), Wing, 1f);   // queue
                cv.Piece(Ellipse(9.5f, 8f + lift, 4f, 3f), Feather, 2.5f);
                cv.Piece(wingsUp ? Capsule(9f, 7f + lift, 6f, 2f + lift, 1.6f) : Capsule(9f, 8f + lift, 6.5f, 11f + lift, 1.8f), Wing, 1.5f);
                cv.Piece(Ellipse(13f, 5.5f + lift, 2.8f, 2.6f), Feather, 2f);
                cv.Piece(Ellipse(12.5f, 3.2f + lift, 1.6f, 1f), Crest, 1f);
                cv.Piece((x, y) => y > 4.5f + lift && y < 8f + lift && x > 15f && x < 17.5f - (y - 4.5f - lift) * 0.5f, Beak, 1f);
                cv.Dot(14, Mathf.RoundToInt(5f + lift), Eye);
                return;
            }

            bool back = facing == Facing.Up;
            cv.Piece(Capsule(10f, 10f + lift, 10f, 14f + lift, 1.4f), Wing, 1f);   // queue
            foreach (float side in new[] { -1f, 1f })
            {
                cv.Piece(Capsule(10f + side * 3f, 7f + lift, 10f + side * (wingsUp ? 6.5f : 4.5f), wingsUp ? 3f + lift : 11f + lift, 1.5f), Wing, 1.5f,
                    (x, y, c) => Mathf.Abs(x - 10f) > 6.5f ? Darker(c, 0.85f) : c);
            }

            cv.Piece(Ellipse(10f, 8.5f + lift, 3.5f, 3.6f), Feather, 2.5f, back ? null : (System.Func<int, int, Color, Color>)((x, y, c) => y > 9 + lift ? C(0.98f, 0.86f, 0.30f) : c));
            cv.Piece(Ellipse(10f, 4.8f + lift, 2.8f, 2.5f), Feather, 2f);
            cv.Piece(Ellipse(10f, 2.4f + lift, 1.4f, 1f), Crest, 1f);
            if (back) return;
            cv.Piece(Ellipse(10f, 6.3f + lift, 1.2f, 1.4f), Beak, 1f);
            cv.Dot(8, Mathf.RoundToInt(4.5f + lift), Eye);
            cv.Dot(11, Mathf.RoundToInt(4.5f + lift), Eye);
        }
    }
}
