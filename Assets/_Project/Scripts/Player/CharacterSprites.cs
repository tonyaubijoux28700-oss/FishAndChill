using UnityEngine;

namespace FishAndChill.Player
{
    /// <summary>
    /// Images du pêcheur fabriquées en jeu à partir de son apparence (<see cref="CharacterPainter"/>) :
    /// 4 directions × 3 images, 16 px par unité, filtre Point, pivot aux pieds.
    /// </summary>
    public sealed class CharacterSprites
    {
        public const int PixelsPerUnit = 16;

        /// <summary>Pivot aux pieds (bas de l'image).</summary>
        public static readonly Vector2 Pivot = new Vector2(0.5f, 1f / CharacterPainter.Height);

        public readonly Sprite[] Down = new Sprite[3];
        public readonly Sprite[] Up = new Sprite[3];
        public readonly Sprite[] Left = new Sprite[3];
        public readonly Sprite[] Right = new Sprite[3];

        public static CharacterSprites Build(CharacterLook look) => Build(look, null);

        /// <summary>Les images d'un habitant : apparence de pêcheur plus ses accessoires (<see cref="NpcExtras"/>).</summary>
        public static CharacterSprites Build(CharacterLook look, NpcExtras extras)
        {
            var result = new CharacterSprites();
            for (int frame = 0; frame < 3; frame++)
            {
                result.Down[frame] = ToSprite(CharacterPainter.Front(look, extras, frame, back: false), $"Bas_{frame}");
                result.Up[frame] = ToSprite(CharacterPainter.Front(look, extras, frame, back: true), $"Haut_{frame}");
                Color[,] side = CharacterPainter.Side(look, extras, frame);
                result.Right[frame] = ToSprite(side, $"Droite_{frame}");
                result.Left[frame] = ToSprite(PixelShading.MirrorX(side), $"Gauche_{frame}");
            }

            return result;
        }

        /// <summary>Une image (tableau [x, y], y depuis le bas) en sprite.</summary>
        public static Sprite ToSprite(Color[,] pixels, string name)
        {
            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Pecheur_" + name,
            };
            var colors = new Color[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    colors[y * width + x] = pixels[x, y];
                }
            }

            texture.SetPixels(colors);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), Pivot, PixelsPerUnit);
            sprite.name = texture.name;
            return sprite;
        }
    }
}
