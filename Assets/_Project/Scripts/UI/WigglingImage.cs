using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Image d'interface qui ondule comme un poisson qui frétille : le dessin est découpé en fines tranches verticales
    /// qui se décalent en vague, presque rien à la tête (à droite), de plus en plus fort vers la queue (à gauche).
    /// Les décalages sont arrondis au pixel du dessin pour garder le rendu pixel art. Le calcul est dans
    /// <see cref="WiggleMath"/> (testé).
    /// </summary>
    public sealed class WigglingImage : Image
    {
        [SerializeField, Range(2, 64)] private int _slices = 24;

        /// <summary>Avancée de la vague (radians) : on l'augmente avec le temps.</summary>
        public float Phase { get; set; }

        /// <summary>Amplitude du battement de la queue, en pixels du dessin (0 = immobile).</summary>
        public float Amplitude { get; set; }

        /// <summary>Remet le maillage à jour (à appeler après avoir changé la phase ou l'amplitude).</summary>
        public void Refresh() => SetVerticesDirty();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (sprite == null)
            {
                base.OnPopulateMesh(vh);
                return;
            }

            Rect r = GetPixelAdjustedRect();
            Vector4 uv = DataUtility.GetOuterUV(sprite);
            // Taille à l'écran d'un pixel du dessin : les décalages se font par pixels entiers.
            float pixel = r.height / Mathf.Max(1f, sprite.rect.height);
            Color32 tint = color;
            for (int i = 0; i <= _slices; i++)
            {
                float u = i / (float)_slices;
                float x = r.xMin + u * r.width;
                float offset = Mathf.Round(WiggleMath.Offset(u, Phase, Amplitude)) * pixel;
                float uvX = Mathf.Lerp(uv.x, uv.z, u);
                vh.AddVert(new Vector3(x, r.yMin + offset), tint, new Vector2(uvX, uv.y));
                vh.AddVert(new Vector3(x, r.yMax + offset), tint, new Vector2(uvX, uv.w));
                if (i > 0)
                {
                    int b = i * 2;
                    vh.AddTriangle(b - 2, b - 1, b + 1);
                    vh.AddTriangle(b - 2, b + 1, b);
                }
            }
        }
    }
}
