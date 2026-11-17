using FishAndChill.Player;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Images du pêcheur et du marchand (32×48, pivot aux pieds), dessinées par <see cref="CharacterPainter"/> à partir
    /// de gabarits pixel art faits à la main. Le pêcheur est enregistré ici avec l'apparence par défaut ; en jeu, il est
    /// redessiné selon l'apparence choisie. 4 directions × 3 images (0 = à l'arrêt, 1 et 2 = pas de marche).
    /// </summary>
    public static class CharacterArt
    {
        private const string Folder = "Assets/_Project/Art/Characters";
        private const int PixelsPerUnit = CharacterPainter.PixelsPerUnit;

        /// <summary>Pivot aux pieds (bas de l'image).</summary>
        public static readonly Vector2 Pivot = new Vector2(0.5f, 1f / CharacterPainter.Height);

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
            var look = new CharacterLook(); // apparence par defaut ; en jeu, le pecheur est redessine selon la partie
            for (int frame = 0; frame < 3; frame++)
            {
                result.Down[frame] = Save($"Pecheur_Bas_{frame}", CharacterPainter.Front(look, frame, back: false));
                result.Up[frame] = Save($"Pecheur_Haut_{frame}", CharacterPainter.Front(look, frame, back: true));
                Color[,] side = CharacterPainter.Side(look, frame);
                result.Right[frame] = Save($"Pecheur_Droite_{frame}", side);
                result.Left[frame] = Save($"Pecheur_Gauche_{frame}", PixelArt.MirrorX(side));
            }

            return result;
        }

        /// <summary>Marchand de face : vieux marin au bob blanc, moustache grise, marinière et tablier.</summary>
        public static Color[,] Merchant() => CharacterPainter.Merchant();

        private static Sprite Save(string name, Color[,] pixels)
        {
            return PixelArt.Save($"{Folder}/{name}.png", pixels, PixelsPerUnit, Pivot);
        }
    }
}
