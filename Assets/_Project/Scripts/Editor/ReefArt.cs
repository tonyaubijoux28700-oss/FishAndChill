using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Grande Barrière : recoloration des tuiles de l'île (mer cristalline, sable
    /// blanc de silice, herbe sèche de la brousse) et coraux vus à travers l'eau peu profonde du récif (aussi à l'atoll).
    /// </summary>
    public static class ReefArt
    {
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
