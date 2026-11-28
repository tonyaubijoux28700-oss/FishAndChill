using UnityEngine;

namespace FishAndChill.UI
{
    /// <summary>
    /// Perle ronde (12×12 px) dessinée en jeu : blanche nacrée, ombrée vers le bas à droite, reflet en haut à gauche et
    /// contour sombre ; on la teinte à la couleur de la perle (<c>PearlLore.ColorOf</c>), ou en sombre pour la silhouette.
    /// Carnet (page Épuisette) et boîte à bijoux.
    /// </summary>
    public static class PearlSprite
    {
        public const int Size = 12;

        private static Sprite _sprite;

        public static Sprite Get()
        {
            if (_sprite != null) return _sprite;
            var texture = new Texture2D(Size, Size) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            Color[,] pixels = Pixels();
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++) texture.SetPixel(x, y, pixels[x, y]);
            }

            texture.Apply();
            _sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 16f);
            return _sprite;
        }

        /// <summary>Les pixels (y depuis le bas), aussi pour les planches d'aperçu.</summary>
        public static Color[,] Pixels()
        {
            var p = new Color[Size, Size];
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    float dx = x + 0.5f - 6f, dy = y + 0.5f - 6f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    Color c = new Color(0f, 0f, 0f, 0f);
                    if (d <= 5.6f)
                    {
                        if (d > 4.7f) c = new Color(0.34f, 0.28f, 0.26f);
                        else
                        {
                            float light = 0.97f - 0.07f * (dx - dy) - 0.04f * d;   // lumière d'en haut à gauche
                            c = new Color(light, light, light * 1.01f);
                            if (Mathf.Abs(dx + 2f) < 1f && Mathf.Abs(dy - 2f) < 1f) c = Color.white;
                        }
                    }

                    p[x, y] = c;
                }
            }

            return p;
        }
    }
}
