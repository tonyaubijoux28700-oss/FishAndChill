using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Pixel art provisoire de la côte de la Grande Barrière : recoloration des tuiles de l'île (mer cristalline, sable
    /// blanc de silice, herbe sèche de la brousse), eucalyptus, pandanus, palétuvier aux racines-échasses, et coraux vus
    /// à travers l'eau peu profonde du récif.
    /// </summary>
    public static class ReefArt
    {
        public const int TreeWidth = 40;
        public const int TreeHeight = 48;

        public static readonly Vector2 TreePivot = new Vector2(20f / TreeWidth, 5f / TreeHeight);

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
        private static Color S(Color c, float light) => DetailedFish.Shade(c, light);

        // ---------- Recoloration ----------

        /// <summary>Mer de corail : bleu soutenu au large, très claire près des côtes.</summary>
        public static Color Water(Color c) => LakeArt.Tint(c, 0.02f, 1.05f, 0.92f);

        /// <summary>Sable blanc de silice (Whitehaven).</summary>
        public static Color Sand(Color c) => LakeArt.Tint(c, 0.0f, 0.25f, 1.06f);

        /// <summary>Herbe sèche de la brousse, vert olive tirant sur le jaune.</summary>
        public static Color Bush(Color c) => LakeArt.Tint(c, -0.05f, 0.75f, 0.92f);

        public static Color[,] Water(Color[,] p) => LakeArt.Tint(p, 0.02f, 1.05f, 0.92f);
        public static Color[,] Sand(Color[,] p) => LakeArt.Tint(p, 0.0f, 0.25f, 1.06f);
        public static Color[,] Bush(Color[,] p) => LakeArt.Tint(p, -0.05f, 0.75f, 0.92f);

        private static void Shadow(Color[,] p, float cx, float rx, float ry)
        {
            for (int x = 0; x < p.GetLength(0); x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    float dx = (x - cx) / rx;
                    float dy = (y - 4.5f) / ry;
                    if (dx * dx + dy * dy <= 1f && p[x, y].a < 0.01f) p[x, y] = C(0.04f, 0.06f, 0.04f, 0.32f);
                }
            }
        }

        // ---------- Arbres ----------

        /// <summary>Eucalyptus : tronc pâle et lisse qui se divise, feuillage gris-vert clairsemé en touffes.</summary>
        public static Color[,] Eucalyptus()
        {
            var p = new Color[TreeWidth, TreeHeight];
            Color bark = C(0.86f, 0.82f, 0.74f);
            CreatureArt.Limb(p, 20, 4, 19, 20, 1.8f, bark);
            CreatureArt.Limb(p, 19, 18, 11, 30, 1.1f, bark);
            CreatureArt.Limb(p, 19, 18, 28, 32, 1.1f, bark);
            CreatureArt.Limb(p, 20, 10, 20, 14, 1.9f, C(0.70f, 0.62f, 0.52f)); // écorce qui pèle
            Color leaves = C(0.46f, 0.58f, 0.46f);
            foreach (Vector3 clump in new[] { new Vector3(10, 32, 7f), new Vector3(28, 34, 8f), new Vector3(19, 40, 7f), new Vector3(14, 26, 5f), new Vector3(26, 26, 5f) })
            {
                for (int x = 0; x < TreeWidth; x++)
                {
                    for (int y = 0; y < TreeHeight; y++)
                    {
                        float dx = (x + 0.5f - clump.x) / clump.z;
                        float dy = (y + 0.5f - clump.y) / (clump.z * 0.7f);
                        float d = dx * dx + dy * dy;
                        if (d > 1f || (x * 7 + y * 3) % 5 == 0) continue; // feuillage clairsemé
                        p[x, y] = S(leaves, dy > 0.2f ? 1.15f : dy < -0.4f ? 0.8f : 1f);
                    }
                }
            }

            PixelArt.SelectiveOutline(p, C(0.10f, 0.14f, 0.10f), 0.5f);
            Shadow(p, 21f, 12f, 3.2f);
            return p;
        }

        /// <summary>Pandanus du bord de plage : tronc annelé, racines aériennes, touffes de longues feuilles en pointe.</summary>
        public static Color[,] Pandanus()
        {
            var p = new Color[TreeWidth, TreeHeight];
            Color trunk = C(0.50f, 0.42f, 0.30f);
            CreatureArt.Limb(p, 14, 4, 18, 9, 0.8f, trunk);   // racines aériennes
            CreatureArt.Limb(p, 26, 4, 22, 9, 0.8f, trunk);
            CreatureArt.Limb(p, 20, 4, 20, 9, 0.8f, trunk);
            for (int y = 8; y <= 28; y++)
            {
                int x = 20 + Mathf.RoundToInt(Mathf.Sin(y * 0.15f) * 2f);
                Color c = y % 3 == 0 ? S(trunk, 0.75f) : trunk;
                PixelArt.Rect(p, x - 1, y, x + 1, y, c);
            }

            Color leaf = C(0.36f, 0.58f, 0.30f);
            foreach (Vector2 top in new[] { new Vector2(20, 29), new Vector2(14, 25), new Vector2(26, 26) })
            {
                for (int f = 0; f < 8; f++)
                {
                    float a = f / 8f * Mathf.PI * 2f;
                    for (int k = 0; k <= 9; k++)
                    {
                        float t = k / 9f;
                        float x = top.x + Mathf.Cos(a) * 9f * t;
                        float y = top.y + Mathf.Sin(a) * 6f * t - t * t * 3f;
                        PixelArt.Set(p, Mathf.RoundToInt(x), Mathf.RoundToInt(y), S(leaf, 1.15f - t * 0.35f));
                    }
                }
            }

            PixelArt.SelectiveOutline(p, C(0.08f, 0.14f, 0.06f), 0.5f);
            Shadow(p, 21f, 10f, 3f);
            return p;
        }

        /// <summary>Palétuvier : racines-échasses arquées qui plongent, couronne vert sombre et dense.</summary>
        public static Color[,] Mangrove()
        {
            var p = new Color[TreeWidth, TreeHeight];
            Color root = C(0.42f, 0.32f, 0.22f);
            for (int r = -3; r <= 3; r++)
            {
                for (int k = 0; k <= 12; k++)
                {
                    float t = k / 12f;
                    float x = 20f + r * 4.5f * t;
                    float y = 18f - t * 14f + Mathf.Sin(t * Mathf.PI) * 3f;
                    PixelArt.Set(p, Mathf.RoundToInt(x), Mathf.RoundToInt(y), S(root, 1f - t * 0.2f));
                }
            }

            CreatureArt.Limb(p, 20, 16, 20, 24, 1.4f, root);
            Color leaves = C(0.18f, 0.40f, 0.22f);
            foreach (Vector3 clump in new[] { new Vector3(20, 31, 11f), new Vector3(12, 27, 6f), new Vector3(28, 28, 7f), new Vector3(20, 37, 7f) })
            {
                for (int x = 0; x < TreeWidth; x++)
                {
                    for (int y = 0; y < TreeHeight; y++)
                    {
                        float dx = (x + 0.5f - clump.x) / clump.z;
                        float dy = (y + 0.5f - clump.y) / (clump.z * 0.7f);
                        if (dx * dx + dy * dy > 1f) continue;
                        p[x, y] = S(leaves, 1f - dx * 0.2f + dy * 0.3f);
                    }
                }
            }

            PixelArt.SelectiveOutline(p, C(0.04f, 0.10f, 0.06f), 0.5f);
            return p;
        }

        // ---------- Coraux (sous l'eau) ----------

        /// <summary>
        /// Corail vu à travers l'eau claire, 16×16, un peu transparent : 0 cerveau rose, 1 corne de cerf orange,
        /// 2 gorgone rouge en éventail, 3 corail table turquoise.
        /// </summary>
        public static Color[,] Coral(int variant)
        {
            var p = new Color[16, 16];
            switch (variant)
            {
                case 0:
                {
                    Color pink = C(0.92f, 0.56f, 0.66f, 0.85f);
                    for (int x = 0; x < 16; x++)
                    {
                        for (int y = 0; y < 16; y++)
                        {
                            float dx = (x + 0.5f - 8f) / 6.5f;
                            float dy = (y + 0.5f - 7f) / 5f;
                            if (dx * dx + dy * dy > 1f) continue;
                            bool groove = Mathf.Sin(x * 1.3f + Mathf.Sin(y * 0.9f) * 2f) > 0.55f; // sillons du cerveau
                            p[x, y] = groove ? S(pink, 0.75f) : S(pink, 1.05f + dy * 0.15f);
                        }
                    }

                    break;
                }

                case 1:
                {
                    Color orange = C(0.96f, 0.60f, 0.26f, 0.88f);
                    foreach (Vector4 branch in new[] { new Vector4(8, 2, 4, 12), new Vector4(8, 2, 12, 13), new Vector4(8, 2, 8, 14), new Vector4(6, 7, 2, 10), new Vector4(10, 8, 14, 10) })
                    {
                        CreatureArt.Limb(p, branch.x, branch.y, branch.z, branch.w, 0.9f, orange);
                    }

                    break;
                }

                case 2:
                {
                    Color red = C(0.86f, 0.24f, 0.30f, 0.85f);
                    for (int x = 0; x < 16; x++)
                    {
                        for (int y = 2; y < 16; y++)
                        {
                            float dx = (x + 0.5f - 8f) / 7f;
                            float dy = (y - 2f) / 13f;
                            if (dx * dx + dy * dy > 1f || dy < 0f) continue;
                            bool mesh = (x + y) % 3 == 0 || (x - y + 30) % 3 == 0;
                            if (mesh) p[x, y] = S(red, 1.05f - dy * 0.2f);
                        }
                    }

                    CreatureArt.Limb(p, 8, 0, 8, 4, 0.7f, S(red, 0.7f));
                    break;
                }

                default:
                {
                    Color teal = C(0.36f, 0.76f, 0.70f, 0.85f);
                    CreatureArt.Limb(p, 8, 1, 8, 6, 0.9f, S(teal, 0.7f));
                    for (int x = 1; x < 15; x++)
                    {
                        for (int y = 6; y <= 9; y++)
                        {
                            float dx = Mathf.Abs(x + 0.5f - 8f) / 7f;
                            if (dx > 1f - (y - 6) * 0.06f) continue;
                            p[x, y] = S(teal, y == 9 ? 1.25f : y == 6 ? 0.8f : 1f);
                        }
                    }

                    break;
                }
            }

            PixelArt.SelectiveOutline(p, C(0.10f, 0.26f, 0.36f), 0.5f);

            // Vu à travers l'eau : couleurs adoucies vers le bleu du lagon, et transparent.
            Color water = C(0.26f, 0.66f, 0.74f);
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    if (p[x, y].a < 0.01f) continue;
                    Color c = Color.Lerp(p[x, y], water, 0.18f);
                    c.a = 0.82f;
                    p[x, y] = c;
                }
            }

            return p;
        }
    }
}
