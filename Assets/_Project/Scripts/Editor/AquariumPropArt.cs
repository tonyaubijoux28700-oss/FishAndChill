using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Mobilier des salles de l'aquarium (pixel art provisoire, pied en bas au milieu), propre à chaque salle :
    /// lac (barque exposée, bancs en rondins, sapins en pot), Amazonie (pirogue, monstera et palmier en pot, tronc moussu),
    /// Japon (bonsaï sur sa table basse, bancs de pierre, rochers du jardin zen).
    /// </summary>
    public static class AquariumPropArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
        private static Color S(Color c, float light) => DetailedFish.Shade(c, light);

        private static float Noise(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 73856093 ^ y * 19349663 ^ seed * 83492791;
                h = (h << 13) ^ h;
                return ((h * (h * h * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647f;
            }
        }

        /// <summary>Ombre douce au sol sous un meuble, posée après le contour.</summary>
        private static void GroundShadow(Color[,] p, float cx, float rx, float ry)
        {
            for (int x = 0; x < p.GetLength(0); x++)
            {
                for (int y = 0; y < Mathf.CeilToInt(ry * 2f) + 1 && y < p.GetLength(1); y++)
                {
                    float dx = (x + 0.5f - cx) / rx;
                    float dy = (y + 0.5f - ry) / ry;
                    if (dx * dx + dy * dy <= 1f && p[x, y].a < 0.01f) p[x, y] = C(0f, 0f, 0f, 0.28f);
                }
            }
        }

        private static Color[,] Finish(Color[,] p, Color outline, float cx, float rx, float ry)
        {
            PixelArt.SelectiveOutline(p, outline, 0.5f);
            GroundShadow(p, cx, rx, ry);
            return p;
        }

        // ---------- Salle du lac ----------

        /// <summary>Vieille barque en bois exposée sur deux tréteaux, avec ses deux rames croisées.</summary>
        public static Color[,] RowboatDisplay()
        {
            var p = new Color[76, 42];
            Color wood = C(0.56f, 0.34f, 0.18f);
            Color paint = C(0.24f, 0.44f, 0.56f);
            Color trestle = C(0.40f, 0.28f, 0.16f);
            foreach (int tx in new[] { 18, 56 })
            {
                CreatureArt.Limb(p, tx - 6, 2, tx, 16, 1.2f, trestle);
                CreatureArt.Limb(p, tx + 6, 2, tx, 16, 1.2f, trestle);
                PixelArt.Rect(p, tx - 7, 15, tx + 7, 17, S(trestle, 1.1f));
            }

            // Coque vue de côté : bordé peint en bleu en haut, bois en dessous, étrave relevée.
            for (int x = 4; x <= 72; x++)
            {
                float t = (x - 4) / 68f;
                float bottom = 18f + Mathf.Pow(Mathf.Abs(t - 0.5f) * 2f, 2.2f) * 10f;
                float top = 32f + (t > 0.85f ? (t - 0.85f) * 30f : 0f) + (t < 0.1f ? (0.1f - t) * 20f : 0f);
                for (int y = Mathf.RoundToInt(bottom); y <= Mathf.RoundToInt(top); y++)
                {
                    bool plankSeam = (y - Mathf.RoundToInt(bottom)) % 4 == 3;
                    Color c = y > top - 4 ? paint : wood;
                    if (plankSeam) c = S(c, 0.75f);
                    if (y >= top - 1) c = S(c, 1.25f);
                    p[x, y] = c;
                }
            }

            // Rames croisées posées dessus.
            CreatureArt.Limb(p, 14, 40, 60, 30, 0.9f, C(0.74f, 0.58f, 0.36f));
            CreatureArt.Limb(p, 60, 40, 14, 30, 0.9f, C(0.70f, 0.54f, 0.32f));
            CreatureArt.Blob(p, 62, 29, 4f, 1.6f, C(0.74f, 0.58f, 0.36f), -0.2f);
            CreatureArt.Blob(p, 12, 29, 4f, 1.6f, C(0.70f, 0.54f, 0.32f), 0.2f);
            return Finish(p, C(0.14f, 0.08f, 0.04f), 38f, 34f, 3f);
        }

        /// <summary>Banc taillé dans un rondin : écorce, bouts sciés aux cernes, deux petits pieds.</summary>
        public static Color[,] LogBench()
        {
            var p = new Color[58, 22];
            Color bark = C(0.38f, 0.24f, 0.14f);
            PixelArt.Rect(p, 9, 2, 13, 9, S(bark, 0.8f));
            PixelArt.Rect(p, 44, 2, 48, 9, S(bark, 0.8f));
            for (int x = 4; x <= 53; x++)
            {
                for (int y = 8; y <= 19; y++)
                {
                    float v = (y - 13.5f) / 6f;
                    Color c = Noise(x / 3, y, 4) > 0.75f ? S(bark, 0.8f) : bark;
                    c = S(c, 1.05f + v * 0.25f);
                    if (y == 19) c = C(0.70f, 0.54f, 0.32f); // assise rabotée
                    p[x, y] = c;
                }
            }

            foreach (int ex in new[] { 4, 53 })
            {
                for (int y = 8; y <= 18; y++)
                {
                    float d = Mathf.Abs(y - 13f);
                    p[ex, y] = d % 3 < 1 ? C(0.62f, 0.44f, 0.24f) : C(0.80f, 0.64f, 0.40f); // cernes
                }
            }

            return Finish(p, C(0.12f, 0.07f, 0.04f), 29f, 26f, 2.5f);
        }

        /// <summary>Petit sapin dans un bac en bois cerclé.</summary>
        public static Color[,] PottedFir()
        {
            var p = new Color[26, 44];
            Color tub = C(0.48f, 0.32f, 0.18f);
            PixelArt.Rect(p, 6, 1, 19, 10, tub);
            PixelArt.Rect(p, 6, 3, 19, 3, C(0.30f, 0.30f, 0.32f));
            PixelArt.Rect(p, 6, 8, 19, 8, C(0.30f, 0.30f, 0.32f));
            PixelArt.Rect(p, 12, 10, 13, 14, C(0.36f, 0.24f, 0.14f));
            Color needles = C(0.14f, 0.38f, 0.24f);
            for (int tier = 0; tier < 4; tier++)
            {
                int baseY = 13 + tier * 7;
                float half = 10f - tier * 2.2f;
                for (int y = baseY; y < baseY + 9 && y < 44; y++)
                {
                    float w = half * (1f - (y - baseY) / 9f) + 0.5f;
                    for (int x = 0; x < 26; x++)
                    {
                        float dx = x + 0.5f - 13f;
                        if (Mathf.Abs(dx) <= w) p[x, y] = S(needles, 1.05f - dx / Mathf.Max(1f, w) * 0.22f);
                    }
                }
            }

            return Finish(p, C(0.04f, 0.10f, 0.06f), 13f, 9f, 2f);
        }

        // ---------- Salle Amazonie ----------

        /// <summary>Pirogue creusée dans un tronc, exposée sur deux supports, avec sa pagaie.</summary>
        public static Color[,] Pirogue()
        {
            var p = new Color[92, 32];
            Color wood = C(0.46f, 0.28f, 0.14f);
            Color stand = C(0.30f, 0.20f, 0.12f);
            PixelArt.Rect(p, 20, 1, 25, 10, stand);
            PixelArt.Rect(p, 66, 1, 71, 10, stand);
            for (int x = 3; x <= 88; x++)
            {
                float t = (x - 3) / 85f;
                float half = Mathf.Sin(t * Mathf.PI) * 6f + 1f;
                float lift = Mathf.Pow(Mathf.Abs(t - 0.5f) * 2f, 3f) * 6f;
                int bottom = Mathf.RoundToInt(17f - half + lift);
                int top = Mathf.RoundToInt(17f + 2f + lift);
                for (int y = bottom; y <= top; y++)
                {
                    Color c = y >= top - 1 ? S(wood, 1.3f) : S(wood, 0.9f + (y - bottom) / Mathf.Max(1f, top - bottom) * 0.3f);
                    if (y == top - 3 && x % 6 < 3 && t > 0.15f && t < 0.85f) c = C(0.86f, 0.70f, 0.40f); // motifs gravés
                    p[x, y] = c;
                }
            }

            CreatureArt.Limb(p, 40, 22, 62, 30, 0.8f, C(0.62f, 0.46f, 0.26f)); // pagaie
            CreatureArt.Blob(p, 64, 30.5f, 3.5f, 1.4f, C(0.62f, 0.46f, 0.26f), 0.35f);
            return Finish(p, C(0.12f, 0.06f, 0.03f), 46f, 42f, 3f);
        }

        /// <summary>Grand monstera aux feuilles fendues dans un pot en terre cuite.</summary>
        public static Color[,] Monstera()
        {
            var p = new Color[38, 46];
            Color pot = C(0.72f, 0.38f, 0.22f);
            for (int y = 1; y <= 12; y++)
            {
                float half = 7f + y * 0.25f;
                for (int x = 0; x < 38; x++)
                {
                    float dx = x + 0.5f - 19f;
                    if (Mathf.Abs(dx) <= half) p[x, y] = S(pot, y >= 11 ? 1.2f : 1f - dx / half * 0.2f);
                }
            }

            Color leaf = C(0.20f, 0.50f, 0.22f);
            foreach (Vector3 l in new[] { new Vector3(10, 26, 0.6f), new Vector3(28, 28, -0.5f), new Vector3(19, 36, 0f), new Vector3(13, 38, 0.4f), new Vector3(26, 39, -0.3f) })
            {
                CreatureArt.Limb(p, 19, 12, l.x, l.y - 4, 0.7f, C(0.26f, 0.44f, 0.18f));
                CreatureArt.Blob(p, l.x, l.y, 7f, 5f, leaf, l.z);
                for (int k = -2; k <= 2; k += 2)
                {
                    PixelArt.Set(p, Mathf.RoundToInt(l.x + k * 2.2f), Mathf.RoundToInt(l.y + 3f), Color.clear); // fentes
                    PixelArt.Set(p, Mathf.RoundToInt(l.x + k * 2.2f), Mathf.RoundToInt(l.y + 4f), Color.clear);
                }
            }

            return Finish(p, C(0.06f, 0.14f, 0.06f), 19f, 10f, 2f);
        }

        /// <summary>Petit palmier en pot : tronc annelé, palmes qui retombent.</summary>
        public static Color[,] PalmPot()
        {
            var p = new Color[40, 54];
            Color pot = C(0.66f, 0.36f, 0.20f);
            PixelArt.Rect(p, 12, 1, 27, 11, pot);
            PixelArt.Rect(p, 11, 10, 28, 12, S(pot, 1.2f));
            for (int y = 12; y <= 36; y++)
            {
                int x = 19 + Mathf.RoundToInt(Mathf.Sin(y * 0.12f) * 1.5f);
                Color c = y % 3 == 0 ? C(0.40f, 0.30f, 0.18f) : C(0.54f, 0.42f, 0.26f);
                PixelArt.Set(p, x, y, c);
                PixelArt.Set(p, x + 1, y, S(c, 0.85f));
            }

            Color frond = C(0.24f, 0.54f, 0.22f);
            for (int f = 0; f < 7; f++)
            {
                float a = Mathf.Lerp(0.2f, Mathf.PI - 0.2f, f / 6f);
                for (int k = 0; k <= 16; k++)
                {
                    float t = k / 16f;
                    float x = 20f + Mathf.Cos(a) * 18f * t;
                    float y = 37f + Mathf.Sin(a) * 12f * t - t * t * 10f;
                    PixelArt.Set(p, Mathf.RoundToInt(x), Mathf.RoundToInt(y), S(frond, 1.15f - t * 0.3f));
                    if (k % 2 == 0) PixelArt.Set(p, Mathf.RoundToInt(x), Mathf.RoundToInt(y) - 1, S(frond, 0.85f));
                }
            }

            return Finish(p, C(0.06f, 0.14f, 0.06f), 20f, 10f, 2f);
        }

        /// <summary>Tronc couché couvert de mousse, qui sert de banc, avec quelques champignons.</summary>
        public static Color[,] MossyTrunk()
        {
            var p = new Color[66, 24];
            Color bark = C(0.34f, 0.24f, 0.16f);
            Color moss = C(0.34f, 0.56f, 0.22f);
            for (int x = 3; x <= 62; x++)
            {
                for (int y = 4; y <= 18; y++)
                {
                    float v = (y - 11f) / 7f;
                    Color c = S(bark, 1f + v * 0.25f);
                    if (y >= 15 - Mathf.RoundToInt(Noise(x / 4, 0, 6) * 3f)) c = S(moss, 0.95f + Noise(x, y, 7) * 0.2f);
                    p[x, y] = c;
                }
            }

            foreach (int mx in new[] { 14, 17, 47 })
            {
                PixelArt.Set(p, mx, 19, C(0.92f, 0.86f, 0.74f));
                PixelArt.Rect(p, mx - 1, 20, mx + 1, 20, C(0.86f, 0.40f, 0.20f)); // chapeau orange
            }

            return Finish(p, C(0.10f, 0.08f, 0.04f), 33f, 30f, 2.5f);
        }

        // ---------- Salle Grande Barrière ----------

        /// <summary>Bénitier géant exposé sur un rocher : deux valves ondulées entrouvertes, manteau bleu-vert tacheté.</summary>
        public static Color[,] GiantClam()
        {
            var p = new Color[46, 30];
            Color rock = C(0.52f, 0.50f, 0.46f);
            CreatureArt.Blob(p, 23, 6, 18f, 5.5f, rock);
            Color shell = C(0.92f, 0.88f, 0.78f);
            Color mantle = C(0.20f, 0.56f, 0.66f);
            for (int x = 6; x <= 40; x++)
            {
                float u = (x - 23f) / 17f;
                float h = Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)) * 13f;
                float wave = Mathf.Sin(x * 0.75f) * 1.5f;
                for (int y = 10; y <= 10 + h + wave; y++)
                {
                    bool lip = Mathf.Abs(y - 17f - wave * 0.5f) < 2.2f;
                    Color c = lip ? mantle : S(shell, 0.9f + (y - 10f) / 15f * 0.3f);
                    if (lip && (x + y) % 4 == 0) c = C(0.54f, 0.86f, 0.90f); // taches irisées
                    if (!lip && x % 5 == 0) c = S(c, 0.82f);                 // côtes de la coquille
                    p[x, y] = c;
                }
            }

            return Finish(p, C(0.18f, 0.16f, 0.14f), 23f, 20f, 2.5f);
        }

        /// <summary>Transat de plage : toile rayée bleu et blanc sur un cadre de bois.</summary>
        public static Color[,] DeckChair()
        {
            var p = new Color[40, 26];
            Color wood = C(0.70f, 0.56f, 0.38f);
            CreatureArt.Limb(p, 4, 1, 14, 10, 0.9f, wood);
            CreatureArt.Limb(p, 36, 1, 30, 10, 0.9f, wood);
            CreatureArt.Limb(p, 10, 2, 34, 22, 0.9f, wood);
            for (int i = 0; i <= 40; i++)
            {
                float t = i / 40f;
                float x = Mathf.Lerp(6f, 33f, t);
                float y = 9f + t * t * 13f;
                for (int k = -2; k <= 2; k++)
                {
                    Color stripe = (i / 4) % 2 == 0 ? C(0.24f, 0.50f, 0.86f) : C(0.96f, 0.96f, 0.94f);
                    PixelArt.Set(p, Mathf.RoundToInt(x), Mathf.RoundToInt(y) + k, stripe);
                }
            }

            return Finish(p, C(0.16f, 0.14f, 0.12f), 20f, 17f, 2f);
        }

        /// <summary>Vieille ancre de marine posée debout, rouillée, avec son cordage.</summary>
        public static Color[,] Anchor()
        {
            var p = new Color[28, 40];
            Color iron = C(0.36f, 0.34f, 0.34f);
            Color rust = C(0.58f, 0.32f, 0.18f);
            CreatureArt.Limb(p, 14, 4, 14, 34, 1.4f, iron);                 // verge
            CreatureArt.Limb(p, 8, 30, 20, 30, 1.1f, iron);                 // jas
            CreatureArt.Blob(p, 14, 36, 3f, 3f, iron);                      // organeau
            CreatureArt.Blob(p, 14, 36, 1.4f, 1.4f, Color.clear);
            for (int k = 0; k <= 14; k++)                                    // bras en croissant
            {
                float a = Mathf.PI * k / 14f;
                CreatureArt.Blob(p, 14 - Mathf.Cos(a) * 10f, 9 - Mathf.Sin(a) * 6f, 1.3f, 1.3f, k % 4 == 0 ? rust : iron);
            }

            CreatureArt.Limb(p, 4, 9, 2, 13, 1.2f, iron);
            CreatureArt.Limb(p, 24, 9, 26, 13, 1.2f, iron);
            for (int y = 6; y < 30; y += 3) PixelArt.Set(p, 15, y, rust);
            CreatureArt.Limb(p, 16, 36, 24, 20, 0.6f, C(0.82f, 0.70f, 0.48f)); // cordage
            return Finish(p, C(0.12f, 0.10f, 0.10f), 14f, 12f, 2f);
        }

        /// <summary>Bouée de sauvetage rouge et blanche accrochée à un poteau de bois.</summary>
        public static Color[,] Lifebuoy()
        {
            var p = new Color[22, 38];
            PixelArt.Rect(p, 10, 1, 12, 34, C(0.62f, 0.46f, 0.30f));
            for (int x = 0; x < 22; x++)
            {
                for (int y = 0; y < 38; y++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(11f, 24f));
                    if (d > 8f || d < 4.2f) continue;
                    float a = Mathf.Atan2(y + 0.5f - 24f, x + 0.5f - 11f);
                    bool red = Mathf.Repeat(a / (Mathf.PI * 0.5f), 1f) < 0.5f;
                    p[x, y] = S(red ? C(0.90f, 0.20f, 0.16f) : C(0.96f, 0.96f, 0.94f), 1.05f - (y - 24f) / 8f * -0.1f);
                }
            }

            return Finish(p, C(0.16f, 0.10f, 0.08f), 11f, 6f, 2f);
        }

        // ---------- Salle Japon ----------

        /// <summary>Bonsaï (pin) dans une coupe bleue, sur une petite table basse en bois sombre.</summary>
        public static Color[,] Bonsai()
        {
            var p = new Color[42, 38];
            Color table = C(0.26f, 0.16f, 0.10f);
            PixelArt.Rect(p, 4, 1, 6, 8, table);
            PixelArt.Rect(p, 35, 1, 37, 8, table);
            PixelArt.Rect(p, 2, 8, 39, 11, S(table, 1.2f));
            PixelArt.Rect(p, 10, 12, 31, 16, C(0.28f, 0.42f, 0.62f)); // coupe bleue
            PixelArt.Rect(p, 9, 16, 32, 17, C(0.36f, 0.52f, 0.72f));
            Color trunk = C(0.36f, 0.26f, 0.18f);
            CreatureArt.Limb(p, 20, 17, 17, 24, 1.6f, trunk);
            CreatureArt.Limb(p, 17, 24, 23, 30, 1.3f, trunk);
            CreatureArt.Limb(p, 18, 23, 9, 26, 0.8f, trunk);
            Color needles = C(0.20f, 0.44f, 0.24f);
            foreach (Vector3 pad in new[] { new Vector3(9, 27, 5f), new Vector3(24, 32, 7f), new Vector3(29, 25, 5f) })
            {
                for (int x = 0; x < 42; x++)
                {
                    for (int y = 0; y < 38; y++)
                    {
                        float dx = (x + 0.5f - pad.x) / pad.z;
                        float dy = (y + 0.5f - pad.y) / 2.4f;
                        if (dx * dx + dy * dy <= 1f) p[x, y] = S(needles, dy > 0 ? 1.2f : 0.9f);
                    }
                }
            }

            return Finish(p, C(0.06f, 0.08f, 0.06f), 21f, 19f, 2f);
        }

        /// <summary>Banc de pierre bas : grande dalle sur deux blocs.</summary>
        public static Color[,] StoneBench()
        {
            var p = new Color[50, 18];
            Color stone = C(0.58f, 0.58f, 0.56f);
            PixelArt.Rect(p, 8, 1, 14, 8, S(stone, 0.8f));
            PixelArt.Rect(p, 35, 1, 41, 8, S(stone, 0.8f));
            for (int x = 2; x <= 47; x++)
            {
                for (int y = 8; y <= 14; y++)
                {
                    Color c = y >= 13 ? S(stone, 1.2f) : stone;
                    if (Noise(x, y, 8) > 0.85f) c = S(c, 0.88f);
                    p[x, y] = c;
                }
            }

            return Finish(p, C(0.16f, 0.16f, 0.16f), 25f, 22f, 2f);
        }

        /// <summary>Rocher du jardin zen, sombre et moussu à la base (0 : grand, 1 : petit, 2 : moyen et plat).</summary>
        public static Color[,] ZenRock(int variant)
        {
            int w = variant == 0 ? 38 : variant == 1 ? 24 : 32;
            int h = variant == 0 ? 30 : variant == 1 ? 18 : 20;
            var p = new Color[w, h];
            Color rock = C(0.34f, 0.34f, 0.36f);
            float cx = w * 0.5f;
            for (int x = 0; x < w; x++)
            {
                for (int y = 2; y < h; y++)
                {
                    float u = (x + 0.5f - cx) / (w * 0.45f);
                    float v = (y - 2f) / (h - 3f);
                    float profile = 1f - u * u;
                    if (variant == 0) profile *= 1f - Mathf.Max(0f, u) * 0.3f;
                    if (v > profile || Mathf.Abs(u) > 1f) continue;
                    float light = 1f - u * 0.25f + (v - 0.5f) * 0.3f;
                    Color c = S(rock, Mathf.Round(light * 5f) / 5f);
                    if (v < 0.18f && Noise(x, y, 3) > 0.35f) c = C(0.32f, 0.48f, 0.22f); // mousse au pied
                    p[x, y] = c;
                }
            }

            return Finish(p, C(0.10f, 0.10f, 0.12f), cx, w * 0.48f, 2f);
        }
    }
}
