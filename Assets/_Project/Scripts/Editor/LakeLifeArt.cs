using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Vie du lac (pixel art provisoire, même style que les créatures) : cygne tuberculé et canard colvert qui nagent,
    /// libellule, poisson qui saute, ronds d'éclaboussure et ombre de nuage. Tête vers la droite (retournés en jeu).
    /// </summary>
    public static class LakeLifeArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static void Wake(Color[,] p, int frame, int y, int xFrom, int xTo)
        {
            // Sillage : petites vagues claires derrière l'oiseau, qui bougent d'une image à l'autre.
            for (int x = xFrom; x <= xTo; x++)
            {
                if ((x + frame) % 3 == 0) PixelArt.Set(p, x, y, C(0.80f, 0.90f, 0.94f, 0.7f));
                if ((x + frame) % 4 == 1) PixelArt.Set(p, x, y + 1, C(0.70f, 0.84f, 0.90f, 0.45f));
            }
        }

        /// <summary>Cygne tuberculé 28×22 : corps blanc, cou en S, bec orange au tubercule noir. 2 images.</summary>
        public static Color[,] Swan(int frame)
        {
            var p = new Color[28, 22];
            Color white = C(0.96f, 0.96f, 0.94f);
            int bob = frame;
            CreatureArt.Blob(p, 11f, 6f + bob * 0.5f, 9.5f, 4.6f, white);
            CreatureArt.Blob(p, 6f, 8.5f + bob * 0.5f, 5f, 3.2f, white, 0.5f);       // ailes relevées à l'arrière
            CreatureArt.Limb(p, 17f, 8f + bob * 0.5f, 19f, 13f, 1.6f, white);       // base du cou
            CreatureArt.Limb(p, 19f, 13f, 18f, 17f, 1.3f, white);                    // cou en S
            CreatureArt.Blob(p, 19.5f, 18f, 2.6f, 2f, white);                        // tête
            CreatureArt.Limb(p, 21f, 17.5f, 24.5f, 16.5f, 0.9f, C(0.92f, 0.46f, 0.14f)); // bec
            PixelArt.Set(p, 21, 18, C(0.06f, 0.06f, 0.06f));                        // tubercule noir
            PixelArt.Set(p, 20, 19, C(0.06f, 0.06f, 0.06f));                        // œil
            CreatureArt.Finish(p, C(0.36f, 0.42f, 0.48f));
            Wake(p, frame, 1, 0, 22);
            return p;
        }

        /// <summary>Canard colvert mâle 18×14 : tête vert bouteille, collier blanc, poitrine brune, dos gris. 2 images.</summary>
        public static Color[,] Duck(int frame)
        {
            var p = new Color[18, 14];
            float bob = frame * 0.5f;
            CreatureArt.Blob(p, 7.5f, 4.5f + bob, 6.5f, 3.2f, C(0.66f, 0.66f, 0.62f));
            CreatureArt.Blob(p, 11.5f, 4.2f + bob, 3f, 2.8f, C(0.46f, 0.28f, 0.20f));   // poitrine brune
            CreatureArt.Blob(p, 2.5f, 6f + bob, 2f, 1.4f, C(0.10f, 0.10f, 0.12f));      // queue noire
            CreatureArt.Blob(p, 13.5f, 8.5f + bob, 2.6f, 2.4f, C(0.10f, 0.42f, 0.22f)); // tête verte
            for (int x = 11; x <= 15; x++) PixelArt.Set(p, x, Mathf.RoundToInt(6.5f + bob), C(0.96f, 0.96f, 0.94f)); // collier
            CreatureArt.Limb(p, 15.5f, 8f + bob, 17.5f, 7.5f + bob, 0.8f, C(0.92f, 0.80f, 0.20f)); // bec jaune
            PixelArt.Set(p, 14, Mathf.RoundToInt(9f + bob), C(0.04f, 0.04f, 0.04f));
            CreatureArt.Finish(p, C(0.14f, 0.14f, 0.12f));
            Wake(p, frame, 0, 0, 14);
            return p;
        }

        /// <summary>Libellule vue de dessus 14×12 : corps bleu métallique, 4 ailes transparentes qui battent. 2 images.</summary>
        public static Color[,] Dragonfly(int frame)
        {
            var p = new Color[16, 14];
            Color body = C(0.18f, 0.48f, 0.90f);
            Color wing = C(0.88f, 0.96f, 1f, frame == 0 ? 0.9f : 0.6f);
            Color vein = C(0.55f, 0.70f, 0.85f, frame == 0 ? 0.9f : 0.6f);
            // Quatre ailes en ellipses (ouvertes, puis à demi repliées sur l'image 2).
            float span = frame == 0 ? 5.5f : 3.5f;
            foreach (float wx in new[] { 8.5f, 11f })
            {
                foreach (int side in new[] { 1, -1 })
                {
                    for (int x = 0; x < 16; x++)
                    {
                        for (int y = 0; y < 14; y++)
                        {
                            float dx = (x + 0.5f - wx) / 1.4f;
                            float dy = (y + 0.5f - (7f + side * span * 0.55f)) / (span * 0.55f);
                            if (dx * dx + dy * dy <= 1f) p[x, y] = Mathf.Abs(dx) < 0.3f ? vein : wing;
                        }
                    }
                }
            }

            // Corps : long abdomen fin, thorax et grosse tête aux yeux.
            for (int x = 1; x <= 11; x++) PixelArt.Set(p, x, 7, x % 3 == 0 ? DetailedFish.Shade(body, 0.75f) : body);
            CreatureArt.Blob(p, 11.5f, 7.5f, 1.6f, 1.2f, body);
            CreatureArt.Blob(p, 13.5f, 7.5f, 1.3f, 1.5f, C(0.10f, 0.30f, 0.60f));
            return p;
        }

        /// <summary>Petit poisson argenté qui saute, 3 images : sortie, sommet (courbé), plongeon.</summary>
        public static Color[,] JumpingFish(int frame)
        {
            var p = new Color[16, 10];
            Color silver = C(0.80f, 0.84f, 0.86f);
            float angle = frame == 0 ? 0.5f : frame == 1 ? 0f : -0.5f;
            CreatureArt.Blob(p, 8f, 5f, 5f, 2f, silver, angle);
            float tx = 8f - Mathf.Cos(angle) * 5.5f;
            float ty = 5f - Mathf.Sin(angle) * 5.5f;
            CreatureArt.Blob(p, tx, ty, 1.6f, 2.2f, DetailedFish.Shade(silver, 0.85f), angle);
            CreatureArt.Finish(p, C(0.20f, 0.26f, 0.32f));
            return p;
        }

        /// <summary>Rond dans l'eau qui s'élargit (3 images), clair et semi-transparent.</summary>
        public static Color[,] Ring(int frame)
        {
            var p = new Color[24, 12];
            float rx = 4f + frame * 3.5f;
            float ry = rx * 0.45f;
            float alpha = 0.8f - frame * 0.22f;
            for (int x = 0; x < 24; x++)
            {
                for (int y = 0; y < 12; y++)
                {
                    float dx = (x + 0.5f - 12f) / rx;
                    float dy = (y + 0.5f - 6f) / ry;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (Mathf.Abs(d - 1f) < 0.16f) p[x, y] = C(0.86f, 0.94f, 0.98f, alpha);
                }
            }

            return p;
        }

        /// <summary>Ombre de poisson vue de dessus 14×6 (banc d'un coin de pêche) : fuseau sombre et queue, transparent.</summary>
        public static Color[,] FishShadow()
        {
            var p = new Color[14, 6];
            Color shade = C(0.02f, 0.08f, 0.12f, 0.62f);
            for (int x = 3; x < 14; x++)
            {
                float u = (x - 8.5f) / 5.5f;
                float half = Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)) * 2.4f;
                for (int y = 0; y < 6; y++)
                {
                    if (Mathf.Abs(y + 0.5f - 3f) <= half) p[x, y] = shade;
                }
            }

            // Queue fourchue.
            PixelArt.Set(p, 2, 3, shade);
            PixelArt.Set(p, 1, 4, shade);
            PixelArt.Set(p, 0, 5, shade);
            PixelArt.Set(p, 1, 1, shade);
            PixelArt.Set(p, 0, 0, shade);
            return p;
        }

        /// <summary>Reflet de soleil sur l'eau 7×7, 3 images : point, petite étoile, grande étoile en croix.</summary>
        public static Color[,] Glint(int frame)
        {
            var p = new Color[7, 7];
            Color core = C(1f, 1f, 1f, 0.95f);
            Color arm = C(0.85f, 0.97f, 1f, 0.8f);
            PixelArt.Set(p, 3, 3, core);
            for (int k = 1; k <= frame; k++)
            {
                Color c = k == frame && frame == 2 ? C(0.80f, 0.94f, 1f, 0.5f) : arm;
                PixelArt.Set(p, 3 + k, 3, c);
                PixelArt.Set(p, 3 - k, 3, c);
                PixelArt.Set(p, 3, 3 + k, c);
                PixelArt.Set(p, 3, 3 - k, c);
            }

            return p;
        }

        /// <summary>Ombre de nuage 96×56 : tache sombre aux bords tramés, très transparente.</summary>
        public static Color[,] CloudShadow()
        {
            var p = new Color[96, 56];
            Vector3[] puffs = { new Vector3(30, 26, 20), new Vector3(52, 32, 22), new Vector3(70, 24, 18), new Vector3(46, 18, 16) };
            for (int x = 0; x < 96; x++)
            {
                for (int y = 0; y < 56; y++)
                {
                    float best = 99f;
                    foreach (Vector3 puff in puffs)
                    {
                        float dx = (x - puff.x) / puff.z;
                        float dy = (y - puff.y) / (puff.z * 0.7f);
                        best = Mathf.Min(best, dx * dx + dy * dy);
                    }

                    if (best < 0.8f || (best < 1f && (x + y) % 2 == 0))
                    {
                        p[x, y] = C(0.02f, 0.06f, 0.12f, 0.16f);
                    }
                }
            }

            return p;
        }
    }
}
