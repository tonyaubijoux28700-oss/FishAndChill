using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Vie du fleuve amazonien (pixel art provisoire, même style que les créatures) : dauphin rose (boto) qui fait
    /// surface, caïman dont seuls les yeux et le dos dépassent, papillon morpho bleu et ara qui traverse le ciel.
    /// Tête vers la droite.
    /// </summary>
    public static class AmazonLifeArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        /// <summary>
        /// Dauphin rose de l'Amazone vu de dessus 28×12, en train de faire surface : dos rose bosselé (pas de vraie
        /// nageoire dorsale), long rostre fin, sillage clair. 2 images (dos plus ou moins sorti).
        /// </summary>
        public static Color[,] RiverDolphin(int frame)
        {
            var p = new Color[28, 12];
            Color pink = C(0.92f, 0.62f, 0.64f);
            float rise = frame == 0 ? 1f : 0.75f;
            CreatureArt.Blob(p, 13f, 6f, 9f * rise, 3.2f * rise, pink);                 // dos
            CreatureArt.Blob(p, 21f, 6f, 3.2f, 2.4f, pink);                             // melon (front bombé)
            CreatureArt.Limb(p, 23f, 6f, 27f, 6f, 0.8f, DetailedFish.Shade(pink, 0.9f)); // rostre
            CreatureArt.Limb(p, 13f, 6f, 13f, 6f, 1.2f, DetailedFish.Shade(pink, 1.2f)); // petite crête
            PixelArt.Set(p, 21, 7, C(0.20f, 0.12f, 0.12f));                              // évent
            CreatureArt.Finish(p, C(0.36f, 0.18f, 0.20f));
            // Sillage clair derrière.
            for (int x = 0; x <= 4; x++)
            {
                if (p[x, 6].a < 0.01f) p[x, 6] = C(0.92f, 0.88f, 0.76f, 0.5f);
                if (x % 2 == 0 && p[x, 4].a < 0.01f) p[x, 4] = C(0.92f, 0.88f, 0.76f, 0.35f);
                if (x % 2 == 0 && p[x, 8].a < 0.01f) p[x, 8] = C(0.92f, 0.88f, 0.76f, 0.35f);
            }

            return p;
        }

        /// <summary>
        /// Caïman qui dérive à la surface, vu de dessus 26×10 : seuls dépassent les yeux, les narines et les
        /// écailles du dos, sombres et un peu transparents. 2 images (la queue ondule).
        /// </summary>
        public static Color[,] Caiman(int frame)
        {
            var p = new Color[26, 10];
            Color body = C(0.18f, 0.22f, 0.12f, 0.75f);
            Color ridge = C(0.30f, 0.32f, 0.18f, 0.9f);
            for (int x = 1; x <= 23; x++)
            {
                float t = x / 23f;
                float wave = frame == 0 ? Mathf.Sin(t * 6f) : -Mathf.Sin(t * 6f);
                float half = t < 0.45f ? Mathf.Lerp(0.4f, 2.2f, t / 0.45f) : t < 0.8f ? 2.2f : Mathf.Lerp(2.2f, 1.2f, (t - 0.8f) / 0.2f);
                float cy = 5f + (t < 0.45f ? wave * (0.45f - t) * 2.5f : 0f);
                for (int y = 0; y < 10; y++)
                {
                    if (Mathf.Abs(y + 0.5f - cy) <= half) p[x, y] = body;
                }

                if (x % 2 == 0 && x > 8 && x < 19) PixelArt.Set(p, x, Mathf.RoundToInt(cy), ridge); // écailles du dos
            }

            // Yeux saillants et narines au bout du museau.
            PixelArt.Set(p, 19, 7, C(0.80f, 0.70f, 0.24f));
            PixelArt.Set(p, 19, 3, C(0.80f, 0.70f, 0.24f));
            PixelArt.Set(p, 23, 5, C(0.08f, 0.08f, 0.06f));
            return p;
        }

        /// <summary>Papillon morpho 13×11 : grandes ailes d'un bleu métallique bordées de noir. 2 images (battement).</summary>
        public static Color[,] Morpho(int frame)
        {
            var p = new Color[13, 11];
            Color blue = C(0.20f, 0.56f, 0.98f);
            Color glow = C(0.56f, 0.86f, 1f);
            Color border = C(0.06f, 0.06f, 0.10f);
            float open = frame == 0 ? 1f : 0.35f;
            for (int x = 0; x < 13; x++)
            {
                for (int y = 0; y < 11; y++)
                {
                    float u = Mathf.Abs(x + 0.5f - 6.5f) / open;
                    float fx = (u - 3f) / 3f;
                    float fy = (y + 0.5f - 6.5f) / 3.2f;
                    bool fore = u >= 0.6f && fx * fx + fy * fy <= 1f;
                    float hx = (u - 2.4f) / 2.2f;
                    float hy = (y + 0.5f - 2.8f) / 2.2f;
                    bool rear = u >= 0.6f && hx * hx + hy * hy <= 1f;
                    if (fore) p[x, y] = fx * fx + fy * fy > 0.7f ? border : fy > 0.1f && fx < 0f ? glow : blue;
                    else if (rear) p[x, y] = hx * hx + hy * hy > 0.7f ? border : blue;
                }
            }

            for (int y = 2; y <= 8; y++) PixelArt.Set(p, 6, y, border); // corps
            PixelArt.Set(p, 5, 9, border);
            PixelArt.Set(p, 7, 9, border);
            return p;
        }

        /// <summary>Ara rouge vu de dessous en vol 28×16 : ailes rouges aux pointes bleues, longue queue. 2 images.</summary>
        public static Color[,] Macaw(int frame)
        {
            var p = new Color[28, 16];
            Color red = C(0.88f, 0.16f, 0.12f);
            Color yellow = C(0.98f, 0.80f, 0.20f);
            Color blue = C(0.16f, 0.36f, 0.86f);
            float lift = frame == 0 ? 3f : -1f;
            CreatureArt.Limb(p, 14f, 9f, 8f, 9f + lift, 1.6f, red);
            CreatureArt.Limb(p, 8f, 9f + lift, 2f, 8f + lift * 0.5f, 1.2f, blue);
            CreatureArt.Limb(p, 14f, 9f, 20f, 9f + lift, 1.6f, red);
            CreatureArt.Limb(p, 20f, 9f + lift, 26f, 8f + lift * 0.5f, 1.2f, blue);
            CreatureArt.Limb(p, 11f, 9f + lift * 0.5f, 9f, 9f + lift * 0.8f, 1f, yellow);   // bande jaune des ailes
            CreatureArt.Limb(p, 17f, 9f + lift * 0.5f, 19f, 9f + lift * 0.8f, 1f, yellow);
            CreatureArt.Blob(p, 15f, 9f, 3f, 1.8f, red);
            CreatureArt.Blob(p, 18.5f, 9f, 1.6f, 1.4f, red);                                 // tête
            PixelArt.Set(p, 20, 9, C(0.92f, 0.90f, 0.86f));                                 // bec
            CreatureArt.Limb(p, 12f, 9f, 3f, 9f, 0.8f, red);                                 // longue queue
            CreatureArt.Limb(p, 6f, 9f, 2f, 9f, 0.6f, blue);
            return p;
        }

        /// <summary>Ombre au sol de l'ara : silhouette transparente.</summary>
        public static Color[,] MacawShadow()
        {
            Color[,] bird = Macaw(0);
            for (int x = 0; x < bird.GetLength(0); x++)
            {
                for (int y = 0; y < bird.GetLength(1); y++)
                {
                    if (bird[x, y].a > 0.01f) bird[x, y] = C(0.02f, 0.06f, 0.02f, 0.22f);
                }
            }

            return bird;
        }
    }
}
