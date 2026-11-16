using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Pixel art provisoire de la baie japonaise : recoloration des tuiles de l'île (mer bleu profond, sable volcanique
    /// gris, herbe couverte de mousse), cerisier en fleurs, pin noir tordu, lanterne de pierre, touffe de bambous,
    /// torii rouge planté dans l'eau et pétales tombés dans l'herbe.
    /// </summary>
    public static class JapanArt
    {
        public const int TreeWidth = 40;
        public const int TreeHeight = 44;

        /// <summary>Pied du tronc, au centre de la case (comme le cocotier).</summary>
        public static readonly Vector2 TreePivot = new Vector2(20f / TreeWidth, 5f / TreeHeight);

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        // ---------- Recoloration ----------

        /// <summary>Mer du Japon : bleu plus profond et plus froid que le lagon.</summary>
        public static Color Water(Color c) => LakeArt.Tint(c, 0.04f, 0.95f, 0.72f);

        /// <summary>Sable volcanique gris-beige.</summary>
        public static Color Sand(Color c) => LakeArt.Tint(c, 0.0f, 0.35f, 0.74f);

        /// <summary>Herbe et mousse, vert un peu jaune et doux.</summary>
        public static Color Moss(Color c) => LakeArt.Tint(c, -0.02f, 0.85f, 0.84f);

        public static Color[,] Water(Color[,] p) => LakeArt.Tint(p, 0.04f, 0.95f, 0.72f);
        public static Color[,] Sand(Color[,] p) => LakeArt.Tint(p, 0.0f, 0.35f, 0.74f);
        public static Color[,] Moss(Color[,] p) => LakeArt.Tint(p, -0.02f, 0.85f, 0.84f);

        private static void Shadow(Color[,] p, float cx, float rx, float ry)
        {
            for (int x = 0; x < p.GetLength(0); x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    float dx = (x - cx) / rx;
                    float dy = (y - 4.5f) / ry;
                    if (dx * dx + dy * dy <= 1f && p[x, y].a < 0.01f) p[x, y] = C(0.04f, 0.08f, 0.06f, 0.35f);
                }
            }
        }

        private static void Crown(Color[,] p, Vector3[] clumps, Color color, int seed, Color? dots)
        {
            var random = new System.Random(seed);
            foreach (Vector3 clump in clumps)
            {
                for (int x = 0; x < p.GetLength(0); x++)
                {
                    for (int y = 0; y < p.GetLength(1); y++)
                    {
                        float dx = (x + 0.5f - clump.x) / clump.z;
                        float dy = (y + 0.5f - clump.y) / (clump.z * 0.75f);
                        float d = dx * dx + dy * dy;
                        if (d > 1f || (d > 0.78f && (x * 5 + y * 3) % 4 == 0)) continue;
                        float light = 1f - dx * 0.22f + dy * 0.28f;
                        p[x, y] = DetailedFish.Shade(color, Mathf.Round(light * 5f) / 5f);
                    }
                }
            }

            if (dots.HasValue)
            {
                for (int i = 0; i < 40; i++)
                {
                    int x = random.Next(2, p.GetLength(0) - 2);
                    int y = random.Next(12, p.GetLength(1) - 2);
                    if (PixelArt.IsFilled(p, x, y)) p[x, y] = dots.Value;
                }
            }
        }

        // ---------- Arbres ----------

        /// <summary>Cerisier du Japon en fleurs : tronc sombre qui se divise, couronne rose pâle en nuages, fleurs blanches.</summary>
        public static Color[,] Sakura()
        {
            var p = new Color[TreeWidth, TreeHeight];
            Color bark = C(0.30f, 0.20f, 0.18f);
            CreatureArt.Limb(p, 20f, 4f, 20f, 16f, 1.8f, bark);
            CreatureArt.Limb(p, 20f, 14f, 12f, 24f, 1.2f, bark);
            CreatureArt.Limb(p, 20f, 14f, 28f, 25f, 1.2f, bark);
            Crown(p, new[]
            {
                new Vector3(20f, 30f, 11f), new Vector3(10f, 27f, 8f), new Vector3(30f, 27f, 8f),
                new Vector3(14f, 36f, 7f), new Vector3(26f, 36f, 7f),
            }, C(0.96f, 0.72f, 0.80f), 31, C(1f, 0.94f, 0.96f));
            PixelArt.SelectiveOutline(p, C(0.40f, 0.18f, 0.26f), 0.5f);
            Shadow(p, 21.5f, 13f, 3.4f);
            return p;
        }

        /// <summary>Pin noir du Japon : tronc tordu penché, plateaux de feuillage vert sombre étagés à l'horizontale.</summary>
        public static Color[,] Pine()
        {
            var p = new Color[TreeWidth, TreeHeight];
            Color bark = C(0.36f, 0.26f, 0.20f);
            CreatureArt.Limb(p, 20f, 4f, 17f, 14f, 1.8f, bark);
            CreatureArt.Limb(p, 17f, 14f, 23f, 24f, 1.5f, bark);
            CreatureArt.Limb(p, 23f, 24f, 18f, 32f, 1.2f, bark);
            CreatureArt.Limb(p, 18f, 18f, 8f, 21f, 1f, bark);
            CreatureArt.Limb(p, 22f, 22f, 32f, 26f, 1f, bark);
            Color needles = C(0.16f, 0.34f, 0.20f);
            foreach (Vector3 pad in new[] { new Vector3(8f, 23f, 7f), new Vector3(32f, 28f, 7f), new Vector3(18f, 34f, 9f), new Vector3(24f, 28f, 5f) })
            {
                for (int x = 0; x < TreeWidth; x++)
                {
                    for (int y = 0; y < TreeHeight; y++)
                    {
                        float dx = (x + 0.5f - pad.x) / pad.z;
                        float dy = (y + 0.5f - pad.y) / 3f;
                        float d = dx * dx + dy * dy;
                        if (d > 1f || (d > 0.7f && (x + y) % 3 == 0)) continue;
                        p[x, y] = DetailedFish.Shade(needles, dy > 0.2f ? 1.2f : dy < -0.4f ? 0.75f : 0.95f);
                    }
                }
            }

            PixelArt.SelectiveOutline(p, C(0.04f, 0.10f, 0.06f), 0.5f);
            Shadow(p, 20.5f, 12f, 3.2f);
            return p;
        }

        /// <summary>Touffe de bambous 24×40 : tiges vertes à nœuds clairs, feuilles fines en éventail en haut.</summary>
        public static Color[,] Bamboo()
        {
            var p = new Color[24, 40];
            Color stem = C(0.46f, 0.66f, 0.30f);
            Color leaf = C(0.32f, 0.56f, 0.24f);
            int[] xs = { 6, 10, 14, 18 };
            int[] tops = { 30, 37, 33, 28 };
            for (int i = 0; i < xs.Length; i++)
            {
                for (int y = 3; y <= tops[i]; y++)
                {
                    Color c = y % 7 == 0 ? C(0.72f, 0.82f, 0.50f) : stem;
                    PixelArt.Set(p, xs[i], y, DetailedFish.Shade(c, 1.1f));
                    PixelArt.Set(p, xs[i] + 1, y, DetailedFish.Shade(c, 0.85f));
                }

                for (int k = 0; k < 4; k++)
                {
                    int ty = tops[i] - k * 4;
                    int dir = (k + i) % 2 == 0 ? 1 : -1;
                    for (int l = 1; l <= 4; l++) PixelArt.Set(p, xs[i] + dir * l, ty - l / 2, leaf);
                }
            }

            PixelArt.SelectiveOutline(p, C(0.08f, 0.18f, 0.06f), 0.5f);
            for (int x = 3; x <= 21; x++)
            {
                for (int y = 1; y <= 4; y++)
                {
                    if (p[x, y].a < 0.01f && Mathf.Abs(x - 12f) / 9f + Mathf.Abs(y - 2.5f) / 2f <= 1f) p[x, y] = C(0.04f, 0.08f, 0.04f, 0.35f);
                }
            }

            return p;
        }

        // ---------- Décor ----------

        /// <summary>Lanterne de pierre (tōrō) 16×28 : socle, fût, boîte à lumière ajourée, toit recourbé, pointe.</summary>
        public static Color[,] StoneLantern()
        {
            var p = new Color[16, 28];
            Color stone = C(0.62f, 0.62f, 0.58f);
            Color dark = C(0.40f, 0.40f, 0.38f);
            PixelArt.Rect(p, 4, 1, 11, 3, dark);         // socle
            PixelArt.Rect(p, 6, 4, 9, 11, stone);        // fût
            PixelArt.Rect(p, 4, 12, 11, 13, dark);       // plateau
            PixelArt.Rect(p, 5, 14, 10, 19, stone);      // boîte à lumière
            PixelArt.Rect(p, 7, 15, 8, 18, C(0.98f, 0.82f, 0.46f));   // ouverture (lueur)
            PixelArt.Rect(p, 2, 20, 13, 21, dark);       // toit
            PixelArt.Rect(p, 4, 22, 11, 23, stone);
            PixelArt.Set(p, 1, 21, dark);
            PixelArt.Set(p, 14, 21, dark);
            PixelArt.Rect(p, 7, 24, 8, 26, stone);       // pointe
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 28; y++)
                {
                    if (PixelArt.IsFilled(p, x, y) && x < 7 && p[x, y] != C(0.98f, 0.82f, 0.46f)) p[x, y] = DetailedFish.Shade(p[x, y], 1.12f);
                }
            }

            PixelArt.SelectiveOutline(p, C(0.16f, 0.16f, 0.16f), 0.5f);
            return p;
        }

        /// <summary>Torii rouge planté dans l'eau 48×48 : deux piliers, deux traverses (la haute recourbée et noire), reflets.</summary>
        public static Color[,] Torii()
        {
            var p = new Color[48, 48];
            Color red = C(0.86f, 0.22f, 0.14f);
            Color black = C(0.12f, 0.10f, 0.10f);
            foreach (int px in new[] { 10, 36 })
            {
                PixelArt.Rect(p, px - 2, 2, px + 1, 38, red);
                PixelArt.Rect(p, px - 2, 2, px - 2, 38, DetailedFish.Shade(red, 1.2f));
                PixelArt.Rect(p, px - 3, 2, px + 2, 4, black);   // pied
            }

            PixelArt.Rect(p, 5, 31, 42, 33, red);                 // traverse basse (nuki)
            PixelArt.Rect(p, 22, 34, 25, 39, red);               // montant central
            for (int x = 1; x <= 46; x++)
            {
                float t = Mathf.Abs(x - 23.5f) / 22.5f;
                int lift = Mathf.RoundToInt(t * t * 3f);          // bouts relevés
                PixelArt.Rect(p, x, 39 + lift, x, 41 + lift, red);
                PixelArt.Rect(p, x, 42 + lift, x, 43 + lift, black);
            }

            PixelArt.SelectiveOutline(p, C(0.30f, 0.06f, 0.04f), 0.5f);
            // Clapot clair au pied des piliers.
            foreach (int px in new[] { 10, 36 })
            {
                for (int x = px - 5; x <= px + 4; x++)
                {
                    if (p[x, 1].a < 0.01f && x % 2 == 0) p[x, 1] = C(0.86f, 0.94f, 0.98f, 0.6f);
                }
            }

            return p;
        }

        /// <summary>Herbe parsemée de pétales de cerisier roses tombés.</summary>
        public static Color[,] PetalGrass(int variant)
        {
            Color[,] p = Moss(TileArt.Grass(0));
            var random = new System.Random(1700 + variant);
            for (int i = 0; i < 5; i++)
            {
                int x = random.Next(1, 15);
                int y = random.Next(1, 15);
                PixelArt.Set(p, x, y, i % 2 == 0 ? C(0.98f, 0.78f, 0.86f) : C(0.96f, 0.66f, 0.76f));
                if (variant == 1) PixelArt.Set(p, x + 1, y, C(0.98f, 0.86f, 0.90f));
            }

            return p;
        }
    }
}
