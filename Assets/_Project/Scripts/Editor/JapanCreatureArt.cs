using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Créature de la baie japonaise (dessin original d'après l'anatomie réelle, même style que <see cref="CreatureArt"/>) :
    /// le crabe-araignée géant.
    /// </summary>
    public static class JapanCreatureArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        /// <summary>
        /// Crabe-araignée géant vu de face, un peu d'en haut : petite carapace orange en poire couverte de bosses,
        /// pattes immenses et fines, orange tachées de blanc, coudées vers le bas, et deux pinces longues et grêles.
        /// </summary>
        public static Color[,] SpiderCrab()
        {
            var p = new Color[120, 60];
            Color orange = C(0.90f, 0.46f, 0.20f);
            Color dark = C(0.66f, 0.28f, 0.12f);
            Color spot = C(0.98f, 0.92f, 0.82f);

            // Quatre paires de pattes très longues : vers l'extérieur et le haut, puis coudées vers le bas.
            for (int i = 0; i < 4; i++)
            {
                foreach (int side in new[] { -1, 1 })
                {
                    float bx = 60f + side * 8f;
                    float by = 26f - i * 2f;
                    float kx = 60f + side * (26f + i * 7f);
                    float ky = 40f - i * 3f;
                    float fx = 60f + side * (36f + i * 7f);
                    float fy = 4f + i * 2f;
                    Color tint = i % 2 == 0 ? orange : dark;
                    CreatureArt.Limb(p, bx, by, kx, ky, 1.3f, tint);
                    CreatureArt.Limb(p, kx, ky, fx, fy, 1.1f, tint);
                    for (int k = 1; k <= 3; k++)
                    {
                        PixelArt.Set(p, Mathf.RoundToInt(Mathf.Lerp(bx, kx, k / 4f)), Mathf.RoundToInt(Mathf.Lerp(by, ky, k / 4f)) + 1, spot);
                        PixelArt.Set(p, Mathf.RoundToInt(Mathf.Lerp(kx, fx, k / 4f)), Mathf.RoundToInt(Mathf.Lerp(ky, fy, k / 4f)), spot);
                    }
                }
            }

            // Pinces longues et grêles, vers l'avant (en bas).
            foreach (int side in new[] { -1, 1 })
            {
                CreatureArt.Limb(p, 60f + side * 6f, 20f, 60f + side * 16f, 12f, 1.2f, orange);
                CreatureArt.Limb(p, 60f + side * 16f, 12f, 60f + side * 12f, 3f, 1.1f, orange);
                CreatureArt.Limb(p, 60f + side * 12f, 3f, 60f + side * 9f, 1.5f, 0.7f, C(0.30f, 0.16f, 0.10f));
            }

            // Carapace en poire, bosses claires.
            CreatureArt.Blob(p, 60f, 26f, 10f, 9f, orange);
            CreatureArt.Blob(p, 60f, 18f, 5f, 4f, orange);
            foreach (Vector2Int b in new[] { new Vector2Int(56, 30), new Vector2Int(64, 30), new Vector2Int(60, 26), new Vector2Int(55, 23), new Vector2Int(65, 23), new Vector2Int(60, 32) })
            {
                PixelArt.Set(p, b.x, b.y, C(0.98f, 0.70f, 0.42f));
                PixelArt.Set(p, b.x + 1, b.y, C(0.98f, 0.70f, 0.42f));
            }

            // Rostre en deux cornes et petits yeux.
            CreatureArt.Limb(p, 58f, 14f, 56f, 10f, 0.7f, dark);
            CreatureArt.Limb(p, 62f, 14f, 64f, 10f, 0.7f, dark);
            PixelArt.Set(p, 57, 17, C(0.06f, 0.06f, 0.06f));
            PixelArt.Set(p, 63, 17, C(0.06f, 0.06f, 0.06f));
            return CreatureArt.Finish(p, C(0.24f, 0.10f, 0.06f));
        }
    }
}
