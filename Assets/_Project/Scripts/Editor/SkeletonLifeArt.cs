using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Vie de la Côte des Squelettes (16 px par case) : otarie qui se dandine sur la plage, otarie vue de dessus sous l'eau,
    /// vautour oricou en vol et son ombre. Tête vers la droite.
    /// </summary>
    public static class SkeletonLifeArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        /// <summary>Otarie à fourrure de profil 22×14 sur la plage : 2 images (dressée sur ses nageoires, affalée).</summary>
        public static Color[,] Seal(int frame)
        {
            var p = new Color[22, 14];
            Color fur = C(0.36f, 0.30f, 0.24f), light = C(0.52f, 0.44f, 0.34f);
            float lift = frame == 0 ? 2f : 0f;
            CreatureArt.Blob(p, 9f, 4.5f, 7f, 3.2f, fur, 0.3f);                              // corps
            CreatureArt.Blob(p, 10f, 4f, 4.5f, 1.8f, light, 0.3f);
            CreatureArt.Limb(p, 13, 5, 16, 8 + lift, 2f, fur);                               // cou dressé
            CreatureArt.Blob(p, 17f, 8.5f + lift, 2.6f, 2f, fur);                            // tête
            CreatureArt.Limb(p, 18, 8 + lift, 20.5f, 7.5f + lift, 0.9f, fur);                 // museau
            PixelArt.Set(p, 18, Mathf.RoundToInt(9 + lift), C(0.04f, 0.04f, 0.04f));        // œil
            CreatureArt.Limb(p, 12, 3, 14, 1, 0.9f, fur);                                    // nageoire avant
            CreatureArt.Limb(p, 3, 3, 1, 2 + lift * 0.5f, 0.9f, fur);                        // nageoires arrière
            return CreatureArt.Finish(p, C(0.14f, 0.10f, 0.08f));
        }

        /// <summary>Otarie vue de dessus sous l'eau 22×12, silhouette sombre et transparente. 2 images (nageoires).</summary>
        public static Color[,] SealSwimming(int frame)
        {
            var p = new Color[22, 12];
            Color body = C(0.04f, 0.10f, 0.10f, 0.55f), edge = C(0.02f, 0.06f, 0.06f, 0.65f);
            float sweep = frame == 0 ? 1.5f : -1.5f;
            CreatureArt.Limb(p, 13, 8, 15 - sweep, 11, 1f, edge);
            CreatureArt.Limb(p, 13, 4, 15 - sweep, 1, 1f, edge);
            CreatureArt.Limb(p, 3, 6, 0, 6 + sweep, 1f, edge);
            CreatureArt.Blob(p, 10f, 6f, 8f, 2.6f, body);
            CreatureArt.Blob(p, 18.5f, 6f, 2.2f, 1.8f, body);
            return p;
        }

        /// <summary>Vautour oricou en vol 32×16 : grandes ailes sombres frangées, tête nue rosée. 2 images.</summary>
        public static Color[,] Vulture(int frame)
        {
            var p = new Color[32, 16];
            Color wing = C(0.22f, 0.18f, 0.16f), tip = C(0.10f, 0.08f, 0.08f);
            float lift = frame == 0 ? 2f : -1f;
            CreatureArt.Limb(p, 15, 8, 6, 8 + lift, 2.2f, wing);
            CreatureArt.Limb(p, 6, 8 + lift, 1, 7 + lift, 1.4f, tip);
            CreatureArt.Limb(p, 15, 8, 24, 8 + lift, 2.2f, wing);
            CreatureArt.Limb(p, 24, 8 + lift, 30, 7 + lift, 1.4f, tip);
            CreatureArt.Blob(p, 15f, 8f, 4f, 2.2f, wing);
            CreatureArt.Blob(p, 20f, 8.5f, 1.6f, 1.3f, C(0.80f, 0.52f, 0.50f));   // tête nue
            for (int x = 3; x < 28; x += 3) PixelArt.Set(p, x, Mathf.RoundToInt(6 + lift * 0.6f), tip);   // franges des plumes
            return p;
        }

        public static Color[,] VultureShadow()
        {
            Color[,] bird = Vulture(0);
            for (int x = 0; x < bird.GetLength(0); x++)
            {
                for (int y = 0; y < bird.GetLength(1); y++)
                {
                    if (bird[x, y].a > 0.01f) bird[x, y] = C(0.05f, 0.05f, 0.06f, 0.28f);
                }
            }

            return bird;
        }
    }
}
