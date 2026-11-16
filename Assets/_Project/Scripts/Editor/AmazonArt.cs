using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Pixel art provisoire du fleuve amazonien : recoloration des tuiles de l'île (eau brune couleur thé, rives de
    /// vase ocre, jungle d'un vert profond), grand arbre de la forêt à contreforts, nénuphars géants (Victoria),
    /// fougères des rives et fleurs d'héliconia.
    /// </summary>
    public static class AmazonArt
    {
        public const int TreeWidth = 40;
        public const int TreeHeight = 48;

        /// <summary>Pied du tronc, au centre de la case (comme le cocotier).</summary>
        public static readonly Vector2 TreePivot = new Vector2(20f / TreeWidth, 5f / TreeHeight);

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        // ---------- Recoloration ----------

        /// <summary>Eau du fleuve : brune et trouble, couleur thé au lait.</summary>
        public static Color Water(Color c) => LakeArt.Tint(c, -0.43f, 0.55f, 0.86f);

        /// <summary>Rive : sable devenu vase ocre rouge.</summary>
        public static Color Mud(Color c) => LakeArt.Tint(c, -0.06f, 1.45f, 0.50f);

        /// <summary>Sol de la jungle : vert profond et humide.</summary>
        public static Color Jungle(Color c) => LakeArt.Tint(c, 0.05f, 1.15f, 0.62f);

        public static Color[,] Water(Color[,] p) => LakeArt.Tint(p, -0.43f, 0.55f, 0.86f);
        public static Color[,] Mud(Color[,] p) => LakeArt.Tint(p, -0.06f, 1.45f, 0.50f);
        public static Color[,] Jungle(Color[,] p) => LakeArt.Tint(p, 0.05f, 1.15f, 0.62f);

        // ---------- Grand arbre ----------

        /// <summary>
        /// Arbre géant de la forêt vu de 3/4 : ombre au sol, tronc clair à contreforts, grosse couronne en nuages de
        /// feuillage éclairés en haut à gauche, quelques lianes qui pendent. <paramref name="variant"/> change le vert.
        /// </summary>
        public static Color[,] Tree(int variant)
        {
            var p = new Color[TreeWidth, TreeHeight];
            Color trunk = C(0.62f, 0.54f, 0.42f);
            Color leaves = variant == 0 ? C(0.14f, 0.40f, 0.16f) : C(0.20f, 0.44f, 0.12f);
            Color liana = C(0.24f, 0.40f, 0.14f);

            // Tronc et contreforts (racines en planches) qui s'évasent au pied.
            for (int y = 4; y <= 24; y++)
            {
                float half = y < 9 ? 1.5f + (9 - y) * 1.1f : 1.6f;
                for (int x = 0; x < TreeWidth; x++)
                {
                    float dx = x + 0.5f - 20f;
                    if (Mathf.Abs(dx) > half) continue;
                    float light = dx < -half * 0.3f ? 1.12f : dx > half * 0.4f ? 0.78f : 0.95f;
                    p[x, y] = DetailedFish.Shade(trunk, light);
                }
            }

            // Couronne : grappes de feuillage rondes, éclairées en haut à gauche.
            var random = new System.Random(900 + variant);
            Vector3[] clumps =
            {
                new Vector3(20f, 34f, 12f), new Vector3(11f, 30f, 8f), new Vector3(29f, 30f, 8.5f),
                new Vector3(15f, 40f, 7f), new Vector3(26f, 40f, 7.5f), new Vector3(20f, 26f, 7f),
            };
            foreach (Vector3 clump in clumps)
            {
                for (int x = 0; x < TreeWidth; x++)
                {
                    for (int y = 0; y < TreeHeight; y++)
                    {
                        float dx = (x + 0.5f - clump.x) / clump.z;
                        float dy = (y + 0.5f - clump.y) / (clump.z * 0.8f);
                        float d = dx * dx + dy * dy;
                        if (d > 1f) continue;
                        bool notch = d > 0.8f && (x * 3 + y * 5) % 4 == 0;
                        if (notch) continue;
                        float light = 1f - dx * 0.25f + dy * 0.3f;
                        p[x, y] = DetailedFish.Shade(leaves, Mathf.Round(light * 5f) / 5f);
                    }
                }
            }

            // Petites taches de lumière sur les feuilles.
            for (int i = 0; i < 18; i++)
            {
                int x = 6 + random.Next(0, 28);
                int y = 26 + random.Next(0, 20);
                if (PixelArt.IsFilled(p, x, y)) p[x, y] = DetailedFish.Shade(leaves, 1.3f);
            }

            // Lianes qui pendent de la couronne.
            foreach (int lx in new[] { 9, 30 })
            {
                for (int y = 14; y <= 24; y++)
                {
                    PixelArt.Set(p, lx + (y % 5 == 0 ? 1 : 0), y, liana);
                }
            }

            PixelArt.SelectiveOutline(p, C(0.04f, 0.12f, 0.05f), 0.5f);

            // Ombre au sol après le contour.
            for (int x = 0; x < TreeWidth; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    float dx = (x - 21.5f) / 13f;
                    float dy = (y - 4.5f) / 3.5f;
                    if (dx * dx + dy * dy <= 1f && p[x, y].a < 0.01f) p[x, y] = C(0.02f, 0.08f, 0.03f, 0.4f);
                }
            }

            return p;
        }

        // ---------- Décor du fleuve ----------

        /// <summary>
        /// Nénuphar géant (Victoria amazonica) 16×16 : grande feuille ronde au bord relevé (liseré rouge), nervures en
        /// étoile ; variante 0 avec une fleur blanche, 1 avec une fleur rose (le 2e jour), 2 deux feuilles.
        /// </summary>
        public static Color[,] GiantLily(int variant)
        {
            var p = new Color[16, 16];
            Color leaf = C(0.30f, 0.52f, 0.20f);
            Color rim = C(0.62f, 0.24f, 0.18f);
            void Pad(float cx, float cy, float r)
            {
                for (int x = 0; x < 16; x++)
                {
                    for (int y = 0; y < 16; y++)
                    {
                        float dx = x + 0.5f - cx;
                        float dy = y + 0.5f - cy;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d > r) continue;
                        Color c = d > r - 1.1f ? rim : leaf;
                        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        if (d < r - 1.1f && d > 1.2f && Mathf.Abs(Mathf.DeltaAngle(angle, Mathf.Round(angle / 45f) * 45f)) < 5f)
                        {
                            c = DetailedFish.Shade(leaf, 0.82f); // nervures
                        }

                        p[x, y] = DetailedFish.Shade(c, 1.05f - dy / r * 0.12f);
                    }
                }
            }

            if (variant == 2)
            {
                Pad(5.5f, 6f, 5f);
                Pad(12f, 11.5f, 3.6f);
            }
            else
            {
                Pad(7.5f, 7.5f, 7f);
            }

            PixelArt.SelectiveOutline(p, C(0.10f, 0.18f, 0.08f), 0.5f);
            if (variant != 2)
            {
                Color petal = variant == 0 ? C(0.98f, 0.97f, 0.92f) : C(0.94f, 0.56f, 0.70f);
                foreach (Vector2Int d in new[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1) })
                {
                    PixelArt.Set(p, 10 + d.x, 10 + d.y, petal);
                }

                PixelArt.Set(p, 10, 10, C(0.98f, 0.80f, 0.30f));
            }

            return p;
        }

        /// <summary>Fougère des rives 16×20 : frondes vertes arquées qui partent du pied (on passe à travers).</summary>
        public static Color[,] Fern(int variant)
        {
            var p = new Color[16, 20];
            Color frond = variant == 0 ? C(0.26f, 0.56f, 0.20f) : C(0.34f, 0.60f, 0.18f);
            int count = 6 + variant;
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Lerp(0.35f, Mathf.PI - 0.35f, i / (float)(count - 1));
                float length = 9f + (i % 2) * 3f;
                for (int k = 0; k <= 14; k++)
                {
                    float t = k / 14f;
                    float x = 8f + Mathf.Cos(angle) * length * t;
                    float y = 1f + Mathf.Sin(angle) * length * t - t * t * 4f * Mathf.Abs(Mathf.Cos(angle));
                    PixelArt.Set(p, Mathf.RoundToInt(x), Mathf.RoundToInt(y), DetailedFish.Shade(frond, 1.1f - t * 0.3f));
                    if (k % 2 == 0 && k > 2)
                    {
                        // Folioles de chaque côté de la tige.
                        PixelArt.Set(p, Mathf.RoundToInt(x - Mathf.Sin(angle)), Mathf.RoundToInt(y + Mathf.Cos(angle)), DetailedFish.Shade(frond, 0.85f));
                        PixelArt.Set(p, Mathf.RoundToInt(x + Mathf.Sin(angle)), Mathf.RoundToInt(y - Mathf.Cos(angle)), DetailedFish.Shade(frond, 0.95f));
                    }
                }
            }

            PixelArt.SelectiveOutline(p, C(0.06f, 0.16f, 0.06f), 0.5f);
            return p;
        }

        /// <summary>Sol de jungle fleuri 16×16 : héliconias rouges et jaunes (0) ou orchidées roses (1).</summary>
        public static Color[,] FlowerJungle(int variant)
        {
            Color[,] p = Jungle(TileArt.Grass(0));
            var random = new System.Random(1300 + variant);
            for (int i = 0; i < 2; i++)
            {
                int x = 3 + random.Next(0, 10);
                int y = 3 + random.Next(0, 9);
                if (variant == 0)
                {
                    // Pinces de homard : bractées rouges en zigzag au bout jaune.
                    for (int k = 0; k < 3; k++)
                    {
                        int bx = x + (k % 2 == 0 ? 0 : 1);
                        PixelArt.Set(p, bx, y + k, C(0.90f, 0.20f, 0.14f));
                        PixelArt.Set(p, bx + (k % 2 == 0 ? 1 : -1), y + k, C(0.98f, 0.82f, 0.22f));
                    }

                    PixelArt.Set(p, x, y - 1, C(0.18f, 0.38f, 0.14f));
                }
                else
                {
                    Color petal = C(0.94f, 0.52f, 0.82f);
                    PixelArt.Set(p, x, y, C(0.98f, 0.92f, 0.60f));
                    PixelArt.Set(p, x - 1, y, petal);
                    PixelArt.Set(p, x + 1, y, petal);
                    PixelArt.Set(p, x, y + 1, petal);
                    PixelArt.Set(p, x, y - 1, DetailedFish.Shade(petal, 0.8f));
                }
            }

            return p;
        }
    }
}
