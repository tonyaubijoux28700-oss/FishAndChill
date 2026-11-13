using FishAndChill.World;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Tuiles de transition arrondies (double grille) : pour chacun des 16 masques de coins, dessine la
    /// matière (lagon, sable ou herbe) là où le champ lissé dépasse 0,5, avec une bordure intérieure et
    /// un liseré extérieur (écume, sable mouillé, lisière d'herbe). Le reste est transparent : la couche
    /// du dessous apparaît.
    /// </summary>
    public static class TransitionArt
    {
        public sealed class Style
        {
            /// <summary>Texture de la matière (16×16, périodique).</summary>
            public Color[,] Fill;

            /// <summary>Largeur (en unités de champ) de la bordure intérieure, et sa couleur.</summary>
            public float InnerBand;
            public Color InnerColor;
            public float InnerBlend = 1f;

            /// <summary>Liseré extérieur (au-delà de la frontière), et sa couleur ; null = aucun.</summary>
            public float OuterBand;
            public Color? OuterColor;

            /// <summary>Le liseré extérieur est tramé (un pixel sur deux) pour un fondu doux.</summary>
            public bool DitherOuter;
        }

        public static Color[,] Build(int mask, Style style)
        {
            int size = TileArt.Size;
            var p = new Color[size, size];
            SmoothTerrain.MaskCorners(mask, out float bl, out float br, out float tl, out float tr);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;
                    float f = SmoothTerrain.Field(bl, br, tl, tr, u, v);
                    const float t = SmoothTerrain.Threshold;

                    if (f >= t)
                    {
                        Color c = style.Fill[x, y];
                        if (f < t + style.InnerBand && mask != 15)
                        {
                            c = Color.Lerp(c, style.InnerColor, style.InnerBlend);
                        }

                        p[x, y] = c;
                    }
                    else if (style.OuterColor.HasValue && f >= t - style.OuterBand && mask != 0)
                    {
                        if (!style.DitherOuter || (x + y) % 2 == 0)
                        {
                            p[x, y] = style.OuterColor.Value;
                        }
                    }
                }
            }

            return p;
        }
    }
}
