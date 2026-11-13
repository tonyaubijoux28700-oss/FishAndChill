using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Pêcheur provisoire en pixel art 16×16 : chapeau de paille, chemise hawaïenne, short.
    /// 4 directions × 3 images (0 = à l'arrêt, 1 et 2 = pas de marche). La gauche est le miroir de la droite.
    /// </summary>
    public static class CharacterArt
    {
        private const string Folder = "Assets/_Project/Art/Characters";
        private const int Size = 16;

        private static readonly Color Hat = new Color(0.93f, 0.80f, 0.45f);
        private static readonly Color HatShade = new Color(0.80f, 0.65f, 0.34f);
        private static readonly Color HatBand = new Color(0.78f, 0.27f, 0.22f);
        private static readonly Color Skin = new Color(0.96f, 0.76f, 0.58f);
        private static readonly Color SkinShade = new Color(0.85f, 0.62f, 0.46f);
        private static readonly Color Hair = new Color(0.35f, 0.22f, 0.15f);
        private static readonly Color Eye = new Color(0.12f, 0.10f, 0.10f);
        private static readonly Color Shirt = new Color(0.15f, 0.62f, 0.72f);
        private static readonly Color ShirtShade = new Color(0.10f, 0.47f, 0.58f);
        private static readonly Color Flower = new Color(0.99f, 0.93f, 0.80f);
        private static readonly Color Shorts = new Color(0.95f, 0.55f, 0.25f);
        private static readonly Color ShortsShade = new Color(0.78f, 0.40f, 0.17f);
        private static readonly Color OutlineColor = new Color(0.16f, 0.11f, 0.10f);

        public sealed class Sprites
        {
            public Sprite[] Down;
            public Sprite[] Up;
            public Sprite[] Left;
            public Sprite[] Right;
        }

        public static Sprites Generate()
        {
            var result = new Sprites
            {
                Down = new Sprite[3],
                Up = new Sprite[3],
                Left = new Sprite[3],
                Right = new Sprite[3],
            };

            for (int frame = 0; frame < 3; frame++)
            {
                result.Down[frame] = Save($"Pecheur_Bas_{frame}", DrawFront(frame, back: false));
                result.Up[frame] = Save($"Pecheur_Haut_{frame}", DrawFront(frame, back: true));
                Color[,] side = DrawSide(frame);
                result.Right[frame] = Save($"Pecheur_Droite_{frame}", side);
                result.Left[frame] = Save($"Pecheur_Gauche_{frame}", PixelArt.MirrorX(side));
            }

            return result;
        }

        private static Sprite Save(string name, Color[,] pixels)
        {
            PixelArt.Outline(pixels, OutlineColor);
            return PixelArt.Save($"{Folder}/{name}.png", pixels, Size, new Vector2(0.5f, 0.5f));
        }

        // Les dessins utilisent y compté depuis le HAUT (plus naturel), converti par P().

        private static Color[,] DrawFront(int frame, bool back)
        {
            var p = new Color[Size, Size];

            // Chapeau de paille.
            R(p, 6, 1, 9, 1, Hat);
            R(p, 6, 2, 9, 2, HatBand);
            R(p, 3, 3, 12, 3, Hat);
            R(p, 4, 4, 11, 4, HatShade);

            // Tête.
            if (back)
            {
                R(p, 5, 5, 10, 7, Hair);
            }
            else
            {
                R(p, 5, 5, 10, 7, Skin);
                R(p, 5, 5, 5, 6, Hair);
                R(p, 10, 5, 10, 6, Hair);
                R(p, 6, 6, 6, 6, Eye);
                R(p, 9, 6, 9, 6, Eye);
            }

            // Chemise hawaïenne avec manches.
            R(p, 4, 8, 11, 11, Shirt);
            R(p, 4, 11, 11, 11, ShirtShade);
            R(p, 7, 8, 8, 8, back ? Shirt : SkinShade);
            R(p, 3, 8, 3, 9, Shirt);
            R(p, 12, 8, 12, 9, Shirt);
            R(p, 5, 9, 5, 9, Flower);
            R(p, 10, 10, 10, 10, Flower);
            R(p, 7, 11, 7, 11, Flower);

            // Mains : elles se balancent pendant la marche.
            int leftHand = frame == 1 ? 9 : 10;
            int rightHand = frame == 2 ? 9 : 10;
            R(p, 3, leftHand, 3, leftHand + 1, Skin);
            R(p, 12, rightHand, 12, rightHand + 1, Skin);

            // Short.
            R(p, 5, 12, 10, 13, Shorts);
            R(p, 7, 13, 8, 13, ShortsShade);

            // Jambes : une jambe se lève à chaque pas.
            R(p, 5, 14, 6, frame == 1 ? 14 : 15, Skin);
            R(p, 9, 14, 10, frame == 2 ? 14 : 15, Skin);
            return p;
        }

        private static Color[,] DrawSide(int frame)
        {
            var p = new Color[Size, Size];

            // Chapeau (la visière dépasse vers l'avant, à droite).
            R(p, 6, 1, 9, 1, Hat);
            R(p, 6, 2, 9, 2, HatBand);
            R(p, 3, 3, 12, 3, Hat);
            R(p, 5, 4, 11, 4, HatShade);

            // Tête de profil.
            R(p, 5, 5, 10, 7, Skin);
            R(p, 5, 5, 6, 7, Hair);
            R(p, 9, 6, 9, 6, Eye);
            R(p, 11, 7, 11, 7, Skin);

            // Corps.
            R(p, 6, 8, 9, 11, Shirt);
            R(p, 6, 11, 9, 11, ShirtShade);
            R(p, 7, 9, 7, 9, Flower);

            // Bras qui se balance.
            int armX = frame == 1 ? 9 : frame == 2 ? 6 : 8;
            R(p, armX, 9, armX, 10, Shirt);
            R(p, armX, 11, armX, 11, Skin);

            // Short.
            R(p, 6, 12, 9, 13, Shorts);

            // Jambes : écartées pendant le pas, ensemble à l'arrêt.
            if (frame == 0)
            {
                R(p, 7, 14, 8, 15, Skin);
            }
            else if (frame == 1)
            {
                R(p, 9, 14, 10, 15, Skin);
                R(p, 5, 14, 6, 15, SkinShade);
            }
            else
            {
                R(p, 7, 14, 8, 15, Skin);
                R(p, 6, 14, 6, 14, SkinShade);
            }

            return p;
        }

        /// <summary>Rectangle avec y compté depuis le haut de l'image.</summary>
        private static void R(Color[,] pixels, int xMin, int yTopMin, int xMax, int yTopMax, Color color)
        {
            PixelArt.Rect(pixels, xMin, Size - 1 - yTopMax, xMax, Size - 1 - yTopMin, color);
        }
    }
}
