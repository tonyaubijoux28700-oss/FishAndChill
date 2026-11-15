using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Vie de l'Île Mahana (pixel art provisoire, même style que les créatures) : raie pastenague vue de dessus sous
    /// l'eau du lagon, poisson volant, papillon tropical, bernard-l'ermite et frégate dans le ciel. Tête vers la droite.
    /// </summary>
    public static class MahanaLifeArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        /// <summary>
        /// Raie vue de dessus sous l'eau 28×22 : losange aux ailes qui ondulent, longue queue fine. Silhouette sombre
        /// et transparente, comme vue à travers l'eau du lagon. 2 images.
        /// </summary>
        public static Color[,] Ray(int frame)
        {
            var p = new Color[28, 22];
            Color body = C(0.06f, 0.22f, 0.26f, 0.55f);
            Color edge = C(0.04f, 0.16f, 0.20f, 0.65f);
            float flap = frame == 0 ? 1f : 0.82f;
            for (int x = 0; x < 28; x++)
            {
                for (int y = 0; y < 22; y++)
                {
                    // Losange arrondi : nez à droite (x = 25), ailes en haut et en bas.
                    float dx = (x + 0.5f - 17f) / 8.5f;
                    float dy = (y + 0.5f - 11f) / (9.5f * flap);
                    float shape = Mathf.Pow(Mathf.Abs(dx), 1.4f) + Mathf.Pow(Mathf.Abs(dy), 1.2f);
                    if (shape <= 1f) p[x, y] = shape > 0.8f ? edge : body;
                }
            }

            // Queue fine vers l'arrière.
            for (int x = 1; x <= 9; x++) PixelArt.Set(p, x, 11, edge);
            return p;
        }

        /// <summary>
        /// Poisson volant 18×12, 3 images (sortie, plané les « ailes » ouvertes, plongeon) : dos bleu nuit, ventre
        /// argenté, grandes pectorales bleu clair déployées en vol.
        /// </summary>
        public static Color[,] FlyingFish(int frame)
        {
            var p = new Color[18, 12];
            Color back = C(0.16f, 0.30f, 0.58f);
            Color belly = C(0.86f, 0.90f, 0.94f);
            Color wing = C(0.62f, 0.82f, 0.96f, 0.9f);
            float angle = frame == 0 ? 0.35f : frame == 1 ? 0f : -0.35f;
            if (frame == 1)
            {
                // Pectorales déployées comme des ailes, au-dessus et en dessous.
                CreatureArt.Blob(p, 9f, 9f, 4.5f, 2f, wing, -0.25f);
                CreatureArt.Blob(p, 9f, 3f, 4f, 1.6f, DetailedFish.Shade(wing, 0.85f), 0.25f);
            }

            CreatureArt.Blob(p, 9f, 6f, 6f, 1.8f, back, angle);
            for (int x = 4; x <= 14; x++)
            {
                int y = Mathf.RoundToInt(5f + (x - 9f) * Mathf.Tan(angle));
                if (PixelArt.IsFilled(p, x, y)) p[x, y] = belly;
            }

            float tx = 9f - Mathf.Cos(angle) * 6.5f;
            float ty = 6f - Mathf.Sin(angle) * 6.5f;
            CreatureArt.Blob(p, tx, ty, 1.4f, 2.4f, back, angle);
            CreatureArt.Finish(p, C(0.06f, 0.10f, 0.22f));
            return p;
        }

        /// <summary>Papillon tropical vu de dessus 14×12 : ailes orange vif bordées de noir à points blancs. 2 images.</summary>
        public static Color[,] Butterfly(int frame)
        {
            var p = new Color[13, 11];
            Color wing = C(0.98f, 0.58f, 0.14f);
            Color hind = C(0.92f, 0.44f, 0.10f);
            Color border = C(0.10f, 0.08f, 0.08f);
            // Image 1 : ailes ouvertes ; image 2 : ailes presque refermées (vues par la tranche).
            float open = frame == 0 ? 1f : 0.35f;
            for (int x = 0; x < 13; x++)
            {
                for (int y = 0; y < 11; y++)
                {
                    float u = Mathf.Abs(x + 0.5f - 6.5f) / open;
                    // Ailes avant : triangles qui s'évasent vers le haut ; ailes arrière : petites gouttes en bas.
                    bool fore = y >= 4 && y <= 9 && u >= 0.6f && u <= (y - 2.5f) * 0.95f;
                    float hx = (u - 2f) / 2f;
                    float hy = (y + 0.5f - 3f) / 2f;
                    bool rear = u >= 0.6f && hx * hx + hy * hy <= 1f;
                    if (fore) p[x, y] = u > (y - 2.5f) * 0.95f - 1.2f || y == 9 ? border : wing;
                    else if (rear) p[x, y] = hind;
                }
            }

            for (int y = 2; y <= 8; y++) PixelArt.Set(p, 6, y, border); // corps
            PixelArt.Set(p, 5, 9, border); // antennes
            PixelArt.Set(p, 7, 9, border);
            if (frame == 0)
            {
                PixelArt.Set(p, 1, 8, C(1f, 1f, 1f)); // points blancs au bout des ailes
                PixelArt.Set(p, 11, 8, C(1f, 1f, 1f));
            }

            return p;
        }

        /// <summary>Bernard-l'ermite 14×10 : coquille spiralée beige, pattes et pince rouge orangé qui dépassent. 2 images.</summary>
        public static Color[,] HermitCrab(int frame)
        {
            var p = new Color[14, 10];
            Color shell = C(0.86f, 0.74f, 0.56f);
            Color crab = C(0.86f, 0.36f, 0.20f);
            // Pattes (alternent d'une image à l'autre) et grosse pince devant.
            for (int k = 0; k < 3; k++)
            {
                float x = 8f + k * 1.6f;
                float lift = (k + frame) % 2 == 0 ? 0f : 0.8f;
                CreatureArt.Limb(p, x, 3f, x + 0.8f, 0.5f + lift, 0.6f, crab);
            }

            CreatureArt.Blob(p, 11.5f, 4.5f, 2f, 1.6f, crab);
            CreatureArt.Blob(p, 6f, 5f, 4.8f, 3.8f, shell);
            // Spirale de la coquille.
            foreach (Vector2Int s in new[] { new Vector2Int(5, 6), new Vector2Int(6, 6), new Vector2Int(7, 5), new Vector2Int(6, 4), new Vector2Int(4, 5) })
            {
                PixelArt.Set(p, s.x, s.y, DetailedFish.Shade(shell, 0.7f));
            }

            PixelArt.Set(p, 12, 7, C(0.05f, 0.05f, 0.05f)); // œil pédonculé
            CreatureArt.Finish(p, C(0.24f, 0.14f, 0.08f));
            return p;
        }

        /// <summary>Frégate vue de dessous en vol 30×14 : longues ailes coudées noires, queue fourchue. 2 images (battement).</summary>
        public static Color[,] Frigatebird(int frame)
        {
            var p = new Color[30, 14];
            Color black = C(0.10f, 0.10f, 0.13f);
            float lift = frame == 0 ? 3f : -1f;
            // Ailes en « M » : épaule relevée, pointe abaissée.
            CreatureArt.Limb(p, 15f, 7f, 8f, 7f + lift, 1.2f, black);
            CreatureArt.Limb(p, 8f, 7f + lift, 1f, 5f + lift * 0.5f, 0.8f, black);
            CreatureArt.Limb(p, 15f, 7f, 22f, 7f + lift, 1.2f, black);
            CreatureArt.Limb(p, 22f, 7f + lift, 29f, 5f + lift * 0.5f, 0.8f, black);
            CreatureArt.Blob(p, 15f, 7f, 2.2f, 1.4f, black);
            CreatureArt.Limb(p, 16.5f, 7f, 19f, 7f, 0.7f, C(0.30f, 0.30f, 0.32f)); // bec crochu
            CreatureArt.Limb(p, 13f, 7f, 10f, 9f, 0.5f, black);                    // queue fourchue
            CreatureArt.Limb(p, 13f, 7f, 10f, 5f, 0.5f, black);
            return p;
        }

        /// <summary>Ombre au sol de la frégate : silhouette floue et transparente.</summary>
        public static Color[,] FrigatebirdShadow()
        {
            Color[,] bird = Frigatebird(0);
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
