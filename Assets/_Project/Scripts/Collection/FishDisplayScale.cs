using UnityEngine;

namespace FishAndChill.Collection
{
    /// <summary>
    /// Taille d'affichage d'une prise selon sa vraie taille (logique pure, testée) : une ablette de 10 cm paraît petite
    /// sur la carte, une carangue de 150 cm la remplit. Échelle logarithmique, pour que les petits restent lisibles.
    /// </summary>
    public static class FishDisplayScale
    {
        public const float SmallCm = 8f;
        public const float LargeCm = 150f;
        public const float MinScale = 0.32f;

        /// <summary>Part de la largeur maximale (de <see cref="MinScale"/> à 1) pour une prise de cette taille.</summary>
        public static float For(float sizeCm)
        {
            float t = Mathf.InverseLerp(Mathf.Log(SmallCm), Mathf.Log(LargeCm), Mathf.Log(Mathf.Max(1f, sizeCm)));
            return Mathf.Lerp(MinScale, 1f, t);
        }

        /// <summary>
        /// Taille du dessin dans un cadre : largeur proportionnelle à la vraie longueur, proportions du dessin gardées,
        /// sans dépasser la hauteur du cadre.
        /// </summary>
        public static Vector2 Fit(Vector2 box, Vector2 spritePixels, float sizeCm)
        {
            if (spritePixels.x <= 0f || spritePixels.y <= 0f)
            {
                return box;
            }

            float width = box.x * For(sizeCm);
            float height = width * spritePixels.y / spritePixels.x;
            if (height > box.y)
            {
                width *= box.y / height;
                height = box.y;
            }

            return new Vector2(width, height);
        }
    }
}
