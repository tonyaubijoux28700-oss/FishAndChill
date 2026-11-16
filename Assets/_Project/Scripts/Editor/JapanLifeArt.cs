using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Vie de la baie japonaise (pixel art provisoire) : canard mandarin qui nage, grue du Japon qui traverse le ciel
    /// (et son ombre), pétales de cerisier qui volent. Tête vers la droite.
    /// </summary>
    public static class JapanLifeArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        /// <summary>Canard mandarin vu de 3/4 sur l'eau 16×12 : crête orange et blanche, voiles orange, flancs roux. 2 images.</summary>
        public static Color[,] MandarinDuck(int frame)
        {
            var p = new Color[16, 12];
            CreatureArt.Blob(p, 7f, 4f, 5.5f, 2.8f, C(0.74f, 0.50f, 0.30f));          // corps
            CreatureArt.Blob(p, 5f, 5.5f, 2.4f, 1.4f, C(0.94f, 0.56f, 0.16f));         // voile orange
            CreatureArt.Blob(p, 11.5f, 7.5f, 2.4f, 2.2f, C(0.30f, 0.34f, 0.40f));      // tête
            PixelArt.Set(p, 11, 8, C(0.98f, 0.98f, 0.96f));                              // sourcil blanc
            PixelArt.Set(p, 12, 8, C(0.98f, 0.98f, 0.96f));
            PixelArt.Set(p, 10, 9, C(0.90f, 0.40f, 0.14f));                              // crête
            PixelArt.Set(p, 14, 7, C(0.90f, 0.20f, 0.18f));                              // bec rouge
            PixelArt.Set(p, 12, 7, C(0.06f, 0.06f, 0.06f));                              // œil
            CreatureArt.Finish(p, C(0.16f, 0.10f, 0.08f));
            // Petit sillage qui bouge.
            int shift = frame == 0 ? 0 : 1;
            for (int x = 0; x < 4; x++)
            {
                if (p[x + shift, 1].a < 0.01f && x % 2 == 0) p[x + shift, 1] = C(0.86f, 0.94f, 0.98f, 0.55f);
            }

            return p;
        }

        /// <summary>Grue du Japon en vol 32×16 : grandes ailes blanches aux rémiges noires, calotte rouge, pattes en arrière. 2 images.</summary>
        public static Color[,] Crane(int frame)
        {
            var p = new Color[32, 16];
            Color white = C(0.96f, 0.96f, 0.94f);
            Color black = C(0.10f, 0.10f, 0.12f);
            float lift = frame == 0 ? 4f : -1f;
            CreatureArt.Limb(p, 16f, 8f, 9f, 8f + lift, 1.6f, white);
            CreatureArt.Limb(p, 9f, 8f + lift, 3f, 7f + lift * 0.6f, 1.3f, black);
            CreatureArt.Limb(p, 16f, 8f, 21f, 8f + lift, 1.6f, white);
            CreatureArt.Limb(p, 21f, 8f + lift, 27f, 7f + lift * 0.6f, 1.3f, black);
            CreatureArt.Blob(p, 16f, 8f, 4f, 1.6f, white);
            CreatureArt.Limb(p, 19f, 8f, 26f, 8f, 0.7f, black);                        // long cou noir
            CreatureArt.Blob(p, 27f, 8f, 1.3f, 1f, white);
            PixelArt.Set(p, 27, 9, C(0.86f, 0.12f, 0.12f));                              // calotte rouge
            CreatureArt.Limb(p, 28f, 8f, 31f, 8f, 0.4f, C(0.70f, 0.66f, 0.40f));      // bec
            CreatureArt.Limb(p, 12f, 8f, 4f, 8f, 0.4f, black);                          // pattes en arrière
            return p;
        }

        /// <summary>Ombre au sol de la grue.</summary>
        public static Color[,] CraneShadow()
        {
            Color[,] bird = Crane(0);
            for (int x = 0; x < bird.GetLength(0); x++)
            {
                for (int y = 0; y < bird.GetLength(1); y++)
                {
                    if (bird[x, y].a > 0.01f) bird[x, y] = C(0.02f, 0.06f, 0.08f, 0.22f);
                }
            }

            return bird;
        }

        /// <summary>Pétale de cerisier qui tourne dans le vent 7×7. 2 images (de face, de profil).</summary>
        public static Color[,] Petal(int frame)
        {
            var p = new Color[7, 7];
            Color pink = C(0.98f, 0.76f, 0.84f);
            if (frame == 0)
            {
                CreatureArt.Blob(p, 3.5f, 3.5f, 2.6f, 1.8f, pink, 0.6f);
                PixelArt.Set(p, 2, 4, C(1f, 0.92f, 0.95f));
            }
            else
            {
                CreatureArt.Limb(p, 1.5f, 2f, 5.5f, 5f, 0.8f, pink);
            }

            return p;
        }
    }
}
