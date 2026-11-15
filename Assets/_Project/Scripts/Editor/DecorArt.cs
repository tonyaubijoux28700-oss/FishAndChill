using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Décor qui « incruste » les bâtiments dans l'île (16 px par case, même style que les personnages :
    /// formes rondes éclairées par paliers et contour sélectif) : pavés des parvis et chemins, haies, buissons,
    /// massifs de fleurs, lampadaires, tonneaux, caisses, rochers et panneau indicateur.
    /// </summary>
    public static class DecorArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Outline = C(0.08f, 0.10f, 0.08f);
        private static readonly Color Leaf = C(0.27f, 0.60f, 0.28f);
        private static readonly Color LeafDark = C(0.17f, 0.42f, 0.22f);
        private static readonly Color Wood = C(0.62f, 0.42f, 0.24f);
        private static readonly Color Iron = C(0.16f, 0.18f, 0.22f);
        private static readonly Color Brass = C(0.86f, 0.68f, 0.30f);
        private static readonly Color StoneGrey = C(0.66f, 0.66f, 0.64f);
        private static readonly Color[] Blooms =
        {
            C(0.95f, 0.30f, 0.32f), C(1.00f, 0.80f, 0.30f), C(0.98f, 0.96f, 0.92f), C(0.95f, 0.55f, 0.75f), C(0.60f, 0.55f, 0.95f),
        };

        /// <summary>Lumière d'en haut à gauche, comme les personnages.</summary>
        private static readonly Vector3 LightDirection = new Vector3(-0.45f, 0.70f, 0.55f).normalized;

        private static float Step(float value) => Mathf.Round(value * 6f) / 6f;

        /// <summary>Boule éclairée (ellipse) de couleur <paramref name="color"/>, avec un grain optionnel.</summary>
        private static void Blob(Color[,] p, float cx, float cy, float rx, float ry, Color color, int grainSeed = -1)
        {
            var random = grainSeed >= 0 ? new System.Random(grainSeed) : null;
            for (int x = 0; x < p.GetLength(0); x++)
            {
                for (int y = 0; y < p.GetLength(1); y++)
                {
                    float u = (x + 0.5f - cx) / rx;
                    float v = (y + 0.5f - cy) / ry;
                    float d = u * u + v * v;
                    if (d > 1f)
                    {
                        continue;
                    }

                    var normal = new Vector3(u, v, Mathf.Sqrt(1f - d)).normalized;
                    float light = 0.55f + 0.7f * Mathf.Max(0f, Vector3.Dot(normal, LightDirection));
                    if (random != null && random.NextDouble() < 0.18)
                    {
                        light -= 0.15f; // feuillage : petites ombres entre les feuilles
                    }

                    p[x, y] = DetailedFish.Shade(color, Step(light));
                }
            }
        }

        private static Color[,] Finish(Color[,] p)
        {
            PixelArt.SelectiveOutline(p, Outline, 0.55f);
            return p;
        }

        // ---------- Pavés ----------

        /// <summary>Pavés irréguliers beige-gris (16×16, périodique) ; variante 1 avec quelques brins d'herbe.</summary>
        public static Color[,] Paving(int variant)
        {
            var p = new Color[16, 16];
            Color grout = C(0.55f, 0.52f, 0.46f);
            Color[] stones = { C(0.82f, 0.78f, 0.70f), C(0.77f, 0.73f, 0.66f), C(0.86f, 0.83f, 0.76f) };

            // Rangées de pavés de 8×5 environ, décalées d'une rangée à l'autre.
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    int row = y / 4;
                    int shifted = (x + (row % 2) * 4) % 16;
                    int column = shifted / 8;
                    int lx = shifted % 8;
                    int ly = y % 4;
                    bool joint = lx == 7 || ly == 3;
                    if (joint)
                    {
                        p[x, y] = grout;
                        continue;
                    }

                    Color stone = stones[(row * 3 + column + variant) % stones.Length];
                    float light = ly == 2 || lx == 0 ? 1.12f : ly == 0 || lx == 6 ? 0.88f : 1f; // arêtes éclairées
                    p[x, y] = DetailedFish.Shade(stone, light);
                }
            }

            if (variant == 1)
            {
                PixelArt.Set(p, 7, 3, Leaf);
                PixelArt.Set(p, 8, 4, LeafDark);
                PixelArt.Set(p, 3, 11, Leaf);
            }

            return p;
        }

        // ---------- Végétation ----------

        /// <summary>Haie taillée 16×20, avec quelques fleurs si <paramref name="flowers"/>.</summary>
        public static Color[,] Hedge(bool flowers, int seed)
        {
            var p = new Color[16, 20];
            Blob(p, 8f, 9f, 8.6f, 8.5f, Leaf, grainSeed: seed);
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (p[x, y].a > 0f)
                    {
                        p[x, y] = DetailedFish.Shade(LeafDark, 0.7f); // pied à l'ombre
                    }
                }
            }

            if (flowers)
            {
                var random = new System.Random(seed);
                Color bloom = Blooms[seed % Blooms.Length];
                for (int i = 0; i < 6; i++)
                {
                    int x = random.Next(3, 13);
                    int y = random.Next(6, 16);
                    PixelArt.Set(p, x, y, bloom);
                    PixelArt.Set(p, x + 1, y, Color.Lerp(bloom, Color.white, 0.3f));
                }
            }

            return Finish(p);
        }

        /// <summary>Buisson rond 16×14, parfois en fleurs.</summary>
        public static Color[,] Bush(int variant)
        {
            var p = new Color[16, 14];
            Blob(p, 5.5f, 6f, 5.5f, 5f, Leaf, grainSeed: 30 + variant);
            Blob(p, 10.5f, 6.5f, 5.5f, 5.5f, Leaf, grainSeed: 40 + variant);
            if (variant > 0)
            {
                Color bloom = Blooms[variant % Blooms.Length];
                foreach (Vector2Int at in new[] { new Vector2Int(4, 8), new Vector2Int(9, 10), new Vector2Int(12, 6), new Vector2Int(6, 4) })
                {
                    PixelArt.Set(p, at.x, at.y, bloom);
                }
            }

            return Finish(p);
        }

        /// <summary>Massif de fleurs ovale 32×22 : bordure de pierres, terre, fleurs de couleurs.</summary>
        public static Color[,] FlowerBed(int seed)
        {
            var p = new Color[32, 22];
            Blob(p, 16f, 9f, 15.5f, 8.5f, StoneGrey);
            Blob(p, 16f, 10f, 13.5f, 6.8f, C(0.42f, 0.28f, 0.18f));
            var random = new System.Random(seed);
            for (int i = 0; i < 26; i++)
            {
                float a = (float)(random.NextDouble() * Mathf.PI * 2);
                float r = (float)random.NextDouble();
                int x = Mathf.RoundToInt(16f + Mathf.Cos(a) * r * 11f);
                int y = Mathf.RoundToInt(10f + Mathf.Sin(a) * r * 5f);
                Color bloom = Blooms[random.Next(Blooms.Length)];
                PixelArt.Set(p, x, y + 1, Leaf);
                PixelArt.Set(p, x, y + 2, bloom);
                PixelArt.Set(p, x + 1, y + 2, Color.Lerp(bloom, Color.white, 0.35f));
                PixelArt.Set(p, x, y + 3, Color.Lerp(bloom, Color.black, 0.15f));
            }

            return Finish(p);
        }

        // ---------- Mobilier ----------

        /// <summary>Lampadaire en fonte 16×40 : socle, fût, lanterne vitrée à lumière chaude, petit toit.</summary>
        public static Color[,] Lamp()
        {
            var p = new Color[16, 40];
            PixelArt.Rect(p, 5, 0, 10, 2, Iron);
            PixelArt.Rect(p, 6, 3, 9, 4, DetailedFish.Shade(Iron, 1.2f));
            for (int y = 5; y <= 28; y++)
            {
                PixelArt.Set(p, 7, y, DetailedFish.Shade(Iron, 1.25f));
                PixelArt.Set(p, 8, y, Iron);
            }

            // Lanterne.
            PixelArt.Rect(p, 4, 29, 11, 30, Iron);
            PixelArt.Rect(p, 4, 31, 11, 36, Iron);
            PixelArt.Rect(p, 5, 31, 10, 35, C(1.00f, 0.88f, 0.52f));
            PixelArt.Rect(p, 6, 32, 8, 34, C(1.00f, 0.97f, 0.80f));
            PixelArt.Set(p, 7, 31, Iron);
            PixelArt.Rect(p, 3, 37, 12, 37, Iron);
            PixelArt.Rect(p, 5, 38, 10, 38, Iron);
            PixelArt.Set(p, 7, 39, Brass);
            PixelArt.Set(p, 8, 39, Brass);
            return Finish(p);
        }

        /// <summary>Tonneau 14×18, cerclé de fer, éclairé comme un cylindre.</summary>
        public static Color[,] Barrel()
        {
            var p = new Color[14, 18];
            for (int x = 1; x <= 12; x++)
            {
                float u = (x - 6.5f) / 6f;
                float light = Step(1.12f - (u + 1f) * 0.28f);
                int bulge = Mathf.Abs(u) > 0.8f ? 1 : 0;
                for (int y = 1 + bulge; y <= 15 - bulge; y++)
                {
                    Color c = (y == 4 || y == 12) ? Iron : (x % 3 == 0 ? Color.Lerp(Wood, Color.black, 0.15f) : Wood);
                    p[x, y] = DetailedFish.Shade(c, light);
                }
            }

            // Couvercle vu un peu d'en haut.
            for (int x = 2; x <= 11; x++)
            {
                p[x, 16] = DetailedFish.Shade(Wood, 1.2f);
            }

            return Finish(p);
        }

        /// <summary>Deux caisses en bois empilées 16×18.</summary>
        public static Color[,] Crates()
        {
            var p = new Color[16, 18];
            void Crate(int x0, int y0, int size)
            {
                for (int x = x0; x < x0 + size; x++)
                {
                    for (int y = y0; y < y0 + size; y++)
                    {
                        bool frame = x == x0 || x == x0 + size - 1 || y == y0 || y == y0 + size - 1 || x - x0 == y - y0;
                        Color c = frame ? Color.Lerp(Wood, Color.black, 0.25f) : C(0.80f, 0.62f, 0.38f);
                        p[x, y] = DetailedFish.Shade(c, y == y0 + size - 1 ? 1.2f : x > x0 + size / 2 ? 0.9f : 1f);
                    }
                }
            }

            Crate(0, 0, 10);
            Crate(8, 0, 8);
            Crate(3, 10, 8);
            return Finish(p);
        }

        /// <summary>Rocher moussu 16×12.</summary>
        public static Color[,] Rock(int variant)
        {
            var p = new Color[16, 12];
            Blob(p, 7.5f, 4.5f, 7f, 4.8f, StoneGrey);
            if (variant == 1)
            {
                Blob(p, 12f, 3.5f, 3.5f, 3f, C(0.60f, 0.60f, 0.58f));
            }

            for (int x = 3; x <= 9; x++)
            {
                PixelArt.Set(p, x, 8 + (x % 2), Color.Lerp(StoneGrey, Leaf, 0.6f)); // mousse sur le dessus
            }

            return Finish(p);
        }

        /// <summary>
        /// Panneau indicateur 20×30 : poteau et deux flèches, l'aquarium (bleue, au poisson) et la boutique (rouge,
        /// à la pièce), chacune tournée vers son bâtiment.
        /// </summary>
        public static Color[,] Signpost(bool aquariumToTheRight)
        {
            var p = new Color[20, 30];
            for (int y = 0; y <= 26; y++)
            {
                PixelArt.Set(p, 9, y, DetailedFish.Shade(Wood, 1.15f));
                PixelArt.Set(p, 10, y, DetailedFish.Shade(Wood, 0.85f));
            }

            void Arrow(int y0, bool right, Color board, Color icon, bool coin)
            {
                for (int x = 1; x <= 18; x++)
                {
                    int tip = right ? 18 - x : x - 1;
                    for (int y = y0; y <= y0 + 5; y++)
                    {
                        if (tip < 2 && (y == y0 || y == y0 + 5 || (tip == 0 && (y == y0 + 1 || y == y0 + 4))))
                        {
                            continue; // pointe de la flèche
                        }

                        p[x, y] = DetailedFish.Shade(board, y == y0 + 5 ? 1.15f : 1f);
                    }
                }

                int ix = right ? 7 : 12;
                if (coin)
                {
                    PixelArt.Rect(p, ix - 1, y0 + 1, ix + 1, y0 + 4, C(1f, 0.82f, 0.25f));
                }
                else
                {
                    PixelArt.Rect(p, ix - 2, y0 + 2, ix + 1, y0 + 3, icon);
                    PixelArt.Set(p, ix + 2, y0 + 2, icon);
                    PixelArt.Set(p, ix - 3, y0 + 1, icon);
                    PixelArt.Set(p, ix - 3, y0 + 4, icon);
                }
            }

            Arrow(20, aquariumToTheRight, C(0.20f, 0.52f, 0.62f), C(1f, 0.62f, 0.25f), coin: false);
            Arrow(12, !aquariumToTheRight, C(0.80f, 0.28f, 0.24f), Color.white, coin: true);
            return Finish(p);
        }
    }
}
