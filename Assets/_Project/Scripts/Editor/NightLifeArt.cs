using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// La nuit (pixel art provisoire) : luciole qui clignote et reflet de la lune sur l'eau. Affichés avec un matériau
    /// non éclairé : ils brillent dans le noir.
    /// </summary>
    public static class NightLifeArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        /// <summary>Luciole 7×7 : point jaune-vert entouré d'un halo (image 0 allumée, image 1 à demi éteinte).</summary>
        public static Color[,] Firefly(int frame)
        {
            var p = new Color[7, 7];
            float strength = frame == 0 ? 1f : 0.45f;
            for (int x = 0; x < 7; x++)
            {
                for (int y = 0; y < 7; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(3f, 3f));
                    if (d < 0.6f)
                    {
                        p[x, y] = C(1f, 1f, 0.75f, strength);
                    }
                    else if (d < 1.6f)
                    {
                        p[x, y] = C(0.86f, 1f, 0.40f, 0.75f * strength);
                    }
                    else if (d < 3.1f && (x + y) % 2 == 0)
                    {
                        p[x, y] = C(0.70f, 0.95f, 0.30f, 0.25f * strength);
                    }
                }
            }

            return p;
        }

        /// <summary>
        /// Reflet de la lune 44×64, 3 images : la lune brouillée par les vagues en haut, puis une traînée de petits
        /// traits horizontaux qui s'amincit vers le bas et scintille d'une image à l'autre.
        /// </summary>
        public static Color[,] MoonGlint(int frame)
        {
            var p = new Color[44, 64];
            Color bright = C(0.96f, 0.97f, 1f, 0.95f);
            Color soft = C(0.78f, 0.86f, 1f, 0.6f);

            // Disque de la lune ondulé par les vagues : lignes horizontales décalées.
            for (int y = 50; y < 62; y++)
            {
                float dy = (y - 56f) / 6f;
                float half = Mathf.Sqrt(Mathf.Max(0f, 1f - dy * dy)) * 7f;
                int shift = ((y + frame) % 3) - 1;
                for (int x = Mathf.RoundToInt(22f - half) + shift; x <= Mathf.RoundToInt(22f + half) + shift; x++)
                {
                    if ((y + frame) % 4 != 0)
                    {
                        PixelArt.Set(p, x, y, bright);
                    }
                }
            }

            // Traînée de reflets : des traits courts, plus rares et plus étroits vers le bas.
            for (int y = 2; y < 48; y += 2)
            {
                float t = y / 48f;
                float width = Mathf.Lerp(3f, 12f, t);
                int seed = (y * 73 + frame * 151) % 97;
                int count = 1 + seed % 3;
                for (int k = 0; k < count; k++)
                {
                    int center = 22 + Mathf.RoundToInt(((seed * (k + 3)) % 23 - 11) / 11f * width);
                    int length = 2 + (seed + k) % 4;
                    Color color = (seed + k) % 3 == 0 ? bright : soft;
                    for (int x = center - length / 2; x <= center + length / 2; x++)
                    {
                        PixelArt.Set(p, x, y, color);
                    }
                }
            }

            return p;
        }
    }
}
