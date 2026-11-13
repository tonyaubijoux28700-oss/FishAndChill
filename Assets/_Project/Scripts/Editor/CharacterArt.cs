using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Pêcheur en pixel art détaillé 16×24 (même taille de pixel que les poissons des cartes) :
    /// chapeau de paille tressée, chemise hawaïenne à fleurs, short, sandales.
    /// 4 directions × 3 images (0 = à l'arrêt, 1 et 2 = pas de marche). La gauche est le miroir de la droite.
    /// Le pivot est aux pieds : la position du joueur = l'endroit où il se tient.
    /// </summary>
    public static class CharacterArt
    {
        private const string Folder = "Assets/_Project/Art/Characters";
        private const int Width = 16;
        private const int Height = 24;
        private const int PixelsPerUnit = 16;

        /// <summary>Pivot aux pieds (bas de l'image).</summary>
        public static readonly Vector2 Pivot = new Vector2(0.5f, 1f / Height);

        private static readonly Color Hat = new Color(0.94f, 0.82f, 0.48f);
        private static readonly Color HatWeave = new Color(0.86f, 0.71f, 0.38f);
        private static readonly Color HatShade = new Color(0.74f, 0.58f, 0.30f);
        private static readonly Color HatBand = new Color(0.80f, 0.24f, 0.22f);
        private static readonly Color Skin = new Color(0.96f, 0.77f, 0.60f);
        private static readonly Color SkinShade = new Color(0.86f, 0.62f, 0.47f);
        private static readonly Color Blush = new Color(0.96f, 0.60f, 0.55f);
        private static readonly Color Hair = new Color(0.33f, 0.21f, 0.14f);
        private static readonly Color HairLight = new Color(0.48f, 0.32f, 0.20f);
        private static readonly Color Eye = new Color(0.10f, 0.08f, 0.08f);
        private static readonly Color Shirt = new Color(0.15f, 0.62f, 0.74f);
        private static readonly Color ShirtShade = new Color(0.10f, 0.46f, 0.58f);
        private static readonly Color ShirtLight = new Color(0.30f, 0.74f, 0.84f);
        private static readonly Color Petal = new Color(0.99f, 0.95f, 0.84f);
        private static readonly Color PetalPink = new Color(0.96f, 0.55f, 0.62f);
        private static readonly Color Shorts = new Color(0.96f, 0.56f, 0.24f);
        private static readonly Color ShortsShade = new Color(0.78f, 0.40f, 0.16f);
        private static readonly Color Sandal = new Color(0.45f, 0.28f, 0.16f);
        private static readonly Color OutlineColor = new Color(0.15f, 0.10f, 0.09f);

        public sealed class Sprites
        {
            public Sprite[] Down;
            public Sprite[] Up;
            public Sprite[] Left;
            public Sprite[] Right;
        }

        public static Sprites Generate()
        {
            var result = new Sprites { Down = new Sprite[3], Up = new Sprite[3], Left = new Sprite[3], Right = new Sprite[3] };
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
            return PixelArt.Save($"{Folder}/{name}.png", pixels, PixelsPerUnit, Pivot);
        }

        // Les dessins utilisent y compté depuis le HAUT (ligne 0 = haut du chapeau, ligne 23 = semelles).

        private static void DrawHat(Color[,] p, int brimLeft, int brimRight)
        {
            R(p, 5, 1, 10, 3, Hat);
            for (int x = 5; x <= 10; x++)
            {
                for (int y = 1; y <= 3; y++)
                {
                    if ((x + y) % 2 == 0) R(p, x, y, x, y, HatWeave);
                }
            }

            R(p, 5, 4, 10, 4, HatBand);
            R(p, brimLeft, 5, brimRight, 5, Hat);
            R(p, brimLeft + 1, 6, brimRight - 1, 6, HatShade);
            R(p, 6, 1, 7, 1, Color.Lerp(Hat, Color.white, 0.3f));
        }

        private static Color[,] DrawFront(int frame, bool back)
        {
            var p = new Color[Width, Height];
            DrawHat(p, 2, 13);

            // Tête.
            if (back)
            {
                R(p, 4, 7, 11, 11, Hair);
                R(p, 6, 8, 9, 8, HairLight);
                R(p, 4, 11, 11, 11, Color.Lerp(Hair, Color.black, 0.2f));
            }
            else
            {
                R(p, 4, 7, 11, 11, Skin);
                R(p, 4, 7, 4, 9, Hair);
                R(p, 11, 7, 11, 9, Hair);
                R(p, 5, 7, 10, 7, SkinShade); // ombre du chapeau sur le front
                R(p, 6, 9, 6, 9, Eye);
                R(p, 9, 9, 9, 9, Eye);
                R(p, 6, 8, 6, 8, Color.white);
                R(p, 9, 8, 9, 8, Color.white);
                R(p, 5, 10, 5, 10, Blush);
                R(p, 10, 10, 10, 10, Blush);
                R(p, 7, 11, 8, 11, SkinShade);
            }

            // Chemise hawaïenne.
            R(p, 3, 12, 12, 17, Shirt);
            R(p, 11, 12, 12, 17, ShirtShade);
            R(p, 3, 12, 4, 13, ShirtLight);
            if (!back)
            {
                R(p, 7, 12, 8, 13, SkinShade); // col en V
                R(p, 7, 14, 8, 17, ShirtShade); // boutonnage
            }

            FlowerAt(p, 5, 15);
            FlowerAt(p, 10, 13);
            FlowerAt(p, back ? 7 : 4, back ? 16 : 17);

            // Manches et bras qui se balancent.
            R(p, 2, 12, 2, 14, Shirt);
            R(p, 13, 12, 13, 14, ShirtShade);
            int leftHand = frame == 1 ? 15 : 16;
            int rightHand = frame == 2 ? 15 : 16;
            R(p, 2, 15, 2, leftHand + 1, Skin);
            R(p, 13, 15, 13, rightHand + 1, SkinShade);

            // Short.
            R(p, 4, 18, 11, 20, Shorts);
            R(p, 4, 20, 11, 20, ShortsShade);
            R(p, 7, 20, 8, 20, OutlineColor);

            // Jambes et sandales : une jambe se lève à chaque pas.
            DrawLeg(p, 5, frame == 1);
            DrawLeg(p, 9, frame == 2);
            return p;
        }

        private static void DrawLeg(Color[,] p, int x, bool lifted)
        {
            int bottom = lifted ? 22 : 23;
            R(p, x, 21, x + 1, bottom - 1, Skin);
            R(p, x + 1, 21, x + 1, bottom - 1, SkinShade);
            R(p, x, bottom, x + 1, bottom, Sandal);
        }

        private static void FlowerAt(Color[,] p, int x, int y)
        {
            R(p, x, y, x, y, PetalPink);
            R(p, x - 1, y, x - 1, y, Petal);
            R(p, x + 1, y, x + 1, y, Petal);
            R(p, x, y - 1, x, y - 1, Petal);
        }

        private static Color[,] DrawSide(int frame)
        {
            var p = new Color[Width, Height];
            DrawHat(p, 3, 14);

            // Tête de profil (regarde vers la droite).
            R(p, 5, 7, 11, 11, Skin);
            R(p, 5, 7, 7, 10, Hair);
            R(p, 6, 8, 6, 8, HairLight);
            R(p, 7, 9, 7, 9, SkinShade); // oreille
            R(p, 8, 7, 11, 7, SkinShade);
            R(p, 10, 9, 10, 9, Eye);
            R(p, 10, 8, 10, 8, Color.white);
            R(p, 12, 10, 12, 10, Skin); // nez
            R(p, 10, 10, 10, 10, Blush);
            R(p, 11, 11, 11, 11, SkinShade);

            // Corps.
            R(p, 5, 12, 10, 17, Shirt);
            R(p, 5, 12, 5, 17, ShirtShade);
            R(p, 10, 12, 10, 13, ShirtLight);
            FlowerAt(p, 7, 15);

            // Bras qui se balance d'avant en arrière.
            int armX = frame == 1 ? 10 : frame == 2 ? 6 : 8;
            R(p, armX, 13, armX + 1, 14, ShirtShade);
            R(p, armX, 15, armX + 1, 16, Skin);

            // Short.
            R(p, 5, 18, 10, 20, Shorts);
            R(p, 5, 20, 10, 20, ShortsShade);

            // Jambes : ensemble à l'arrêt, écartées pendant le pas.
            if (frame == 1)
            {
                R(p, 9, 21, 10, 22, Skin);
                R(p, 9, 23, 11, 23, Sandal);
                R(p, 5, 21, 6, 22, SkinShade);
                R(p, 4, 23, 6, 23, Color.Lerp(Sandal, Color.black, 0.3f));
            }
            else
            {
                int lift = frame == 2 ? 1 : 0;
                R(p, 7, 21, 8, 22 - lift, Skin);
                R(p, 7, 23 - lift, 9, 23 - lift, Sandal);
            }

            return p;
        }

        /// <summary>Rectangle avec y compté depuis le haut de l'image.</summary>
        private static void R(Color[,] pixels, int xMin, int yTopMin, int xMax, int yTopMax, Color color)
        {
            PixelArt.Rect(pixels, xMin, Height - 1 - yTopMax, xMax, Height - 1 - yTopMin, color);
        }
    }
}
