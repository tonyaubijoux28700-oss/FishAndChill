using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Vie de la côte de la Grande Barrière (pixel art provisoire) : tortue de mer vue de dessus sous l'eau, kangourou qui
    /// sautille sur la plage, pélican australien en vol et son ombre. Tête vers la droite.
    /// </summary>
    public static class ReefLifeArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        /// <summary>Tortue de mer vue de dessus sous l'eau 24×20, silhouette sombre et transparente. 2 images (nageoires).</summary>
        public static Color[,] SeaTurtle(int frame)
        {
            var p = new Color[24, 20];
            Color body = C(0.06f, 0.20f, 0.22f, 0.55f);
            Color edge = C(0.04f, 0.14f, 0.16f, 0.65f);
            float sweep = frame == 0 ? 1f : -1f;
            CreatureArt.Limb(p, 14, 13, 18 - sweep * 2f, 18, 1.4f, edge);   // nageoires avant
            CreatureArt.Limb(p, 14, 7, 18 - sweep * 2f, 2, 1.4f, edge);
            CreatureArt.Limb(p, 6, 12, 3, 14 + sweep, 1f, edge);             // nageoires arrière
            CreatureArt.Limb(p, 6, 8, 3, 6 - sweep, 1f, edge);
            for (int x = 0; x < 24; x++)
            {
                for (int y = 0; y < 20; y++)
                {
                    float dx = (x + 0.5f - 11f) / 7.5f;
                    float dy = (y + 0.5f - 10f) / 5.5f;
                    if (dx * dx + dy * dy <= 1f) p[x, y] = dx * dx + dy * dy > 0.75f ? edge : body;
                }
            }

            CreatureArt.Blob(p, 20f, 10f, 2.2f, 1.8f, body); // tête
            return p;
        }

        /// <summary>Kangourou roux de profil 18×22 : 2 images (posé sur la queue, en plein bond).</summary>
        public static Color[,] Kangaroo(int frame)
        {
            var p = new Color[18, 22];
            Color fur = C(0.70f, 0.44f, 0.26f);
            Color belly = C(0.90f, 0.78f, 0.62f);
            float lift = frame == 0 ? 0f : 3f;
            CreatureArt.Limb(p, 6, 6 + lift, 1, 2 + lift * 0.5f, 1f, fur);                    // queue
            CreatureArt.Blob(p, 8f, 8f + lift, 3.5f, 4.5f, fur, 0.3f);                         // corps
            CreatureArt.Blob(p, 9f, 8f + lift, 1.8f, 3f, belly, 0.3f);
            CreatureArt.Limb(p, 7, 5 + lift, frame == 0 ? 10 : 12, 1 + lift * 0.4f, 1.2f, fur); // grande patte
            CreatureArt.Blob(p, 11.5f, 15f + lift, 2.2f, 2f, fur);                             // tête
            CreatureArt.Limb(p, 11, 16 + lift, 10, 20 + lift * 0.3f, 0.6f, fur);               // oreilles
            CreatureArt.Limb(p, 12, 16 + lift, 12.5f, 20 + lift * 0.3f, 0.6f, fur);
            PixelArt.Set(p, 13, Mathf.RoundToInt(15 + lift), C(0.08f, 0.06f, 0.05f));          // œil
            CreatureArt.Limb(p, 10, 10 + lift, 12, 9 + lift, 0.5f, fur);                       // petits bras
            return CreatureArt.Finish(p, C(0.24f, 0.14f, 0.08f));
        }

        /// <summary>Pélican australien en vol 32×16 : grandes ailes blanches aux pointes noires, long bec rosé. 2 images.</summary>
        public static Color[,] Pelican(int frame)
        {
            var p = new Color[32, 16];
            Color white = C(0.96f, 0.96f, 0.94f);
            Color black = C(0.12f, 0.12f, 0.14f);
            float lift = frame == 0 ? 3f : -1f;
            CreatureArt.Limb(p, 15, 8, 8, 8 + lift, 1.7f, white);
            CreatureArt.Limb(p, 8, 8 + lift, 2, 7 + lift * 0.6f, 1.4f, black);
            CreatureArt.Limb(p, 15, 8, 22, 8 + lift, 1.7f, white);
            CreatureArt.Limb(p, 22, 8 + lift, 28, 7 + lift * 0.6f, 1.4f, black);
            CreatureArt.Blob(p, 15f, 8f, 4.5f, 2f, white);
            CreatureArt.Blob(p, 21f, 8.5f, 1.8f, 1.5f, white);                    // tête
            CreatureArt.Limb(p, 22, 8, 30, 7, 0.8f, C(0.94f, 0.70f, 0.66f));      // long bec rosé
            return p;
        }

        public static Color[,] PelicanShadow()
        {
            Color[,] bird = Pelican(0);
            for (int x = 0; x < bird.GetLength(0); x++)
            {
                for (int y = 0; y < bird.GetLength(1); y++)
                {
                    if (bird[x, y].a > 0.01f) bird[x, y] = C(0.02f, 0.08f, 0.10f, 0.22f);
                }
            }

            return bird;
        }
    }
}
