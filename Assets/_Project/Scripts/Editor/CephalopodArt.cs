using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Poulpes, seiches et calmars, de profil (tête à droite), dans le style des créatures (<see cref="CreatureArt"/> :
    /// boules et membres éclairés en volume, contour sélectif). Les bras sont des courbes qui s'affinent et
    /// s'enroulent au bout, avec leurs ventouses claires. Dessins originaux d'après l'anatomie et la livrée réelles.
    /// </summary>
    public static class CephalopodArt
    {
        private static Color C(float r, float g, float b) => new Color(r, g, b);

        /// <summary>
        /// Un bras : part de (x, y) dans la direction <paramref name="angle"/> (degrés), tourne de plus en plus
        /// (<paramref name="curl"/>) et s'affine jusqu'au bout ; ventouses claires du côté intérieur de la courbe.
        /// </summary>
        private static void Arm(Color[,] p, float x, float y, float angle, float length, float curl, float radius, Color skin, Color sucker, bool suckers = true)
        {
            const int steps = 40;
            float step = length / steps;
            float a = angle * Mathf.Deg2Rad;
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)steps;
                a += curl * Mathf.Deg2Rad * t * t * 3f / steps * 4f;
                float nx = x + Mathf.Cos(a) * step, ny = y + Mathf.Sin(a) * step;
                float r = Mathf.Lerp(radius, 0.55f, Mathf.Pow(t, 0.8f));
                CreatureArt.Limb(p, x, y, nx, ny, r, skin);
                if (suckers && i % 3 == 1 && r > 0.9f)
                {
                    // Du côté où le bras s'enroule.
                    float side = curl >= 0f ? 1f : -1f;
                    float sx = nx + Mathf.Cos(a + side * Mathf.PI / 2f) * (r - 0.4f);
                    float sy = ny + Mathf.Sin(a + side * Mathf.PI / 2f) * (r - 0.4f);
                    PixelArt.Set(p, Mathf.RoundToInt(sx), Mathf.RoundToInt(sy), sucker);
                }

                x = nx;
                y = ny;
            }
        }

        /// <summary>Peau marbrée : taches claires et sombres semées sur ce qui est déjà peint.</summary>
        private static void Mottle(Color[,] p, int seed, Color light, Color dark, int count, float maxRadius)
        {
            var random = new System.Random(seed);
            int w = p.GetLength(0), h = p.GetLength(1);
            for (int i = 0; i < count; i++)
            {
                int cx = random.Next(0, w), cy = random.Next(0, h);
                float r = 0.6f + (float)random.NextDouble() * maxRadius;
                Color tone = random.NextDouble() < 0.5 ? light : dark;
                for (int x = cx - (int)r - 1; x <= cx + r + 1; x++)
                {
                    for (int y = cy - (int)r - 1; y <= cy + r + 1; y++)
                    {
                        if (x < 0 || y < 0 || x >= w || y >= h || p[x, y].a < 0.5f) continue;
                        if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) p[x, y] = Color.Lerp(p[x, y], tone, 0.45f);
                    }
                }
            }
        }

        /// <summary>Œil de céphalopode : globe clair, pupille horizontale (ou en W pour la seiche), paupière dessus.</summary>
        private static void Eye(Color[,] p, int x, int y, Color iris, bool wPupil = false)
        {
            CreatureArt.Blob(p, x, y, 2.8f, 2.6f, iris);
            for (int dx = -2; dx <= 2; dx++)
            {
                int dy = wPupil && Mathf.Abs(dx) == 1 ? -1 : 0;
                PixelArt.Set(p, x + dx, y + dy, C(0.04f, 0.03f, 0.03f));
            }

            PixelArt.Set(p, x + 1, y + 1, C(0.96f, 0.96f, 0.94f));
            for (int dx = -2; dx <= 2; dx++) PixelArt.Set(p, x + dx, y + 3, Color.Lerp(iris, Color.black, 0.5f));
        }

        /// <summary>
        /// Poulpe : manteau en sac rejeté en arrière et vers le haut, tête aux gros yeux, huit bras qui partent en
        /// rosace et s'enroulent ; <paramref name="spread"/> écarte plus ou moins les bras.
        /// </summary>
        private static Color[,] Octopus(Color skin, Color dark, Color light, Color sucker, Color iris, int seed, float armLength, float spread = 1f)
        {
            var p = new Color[104, 80];
            Color back = Color.Lerp(skin, Color.black, 0.25f);
            // Les bras partent en éventail sous la tête, vers la gauche, le bas et la droite, et le bout s'enroule
            // vers le haut. Ceux de derrière, plus sombres, d'abord.
            float bx = 60f, by = 30f;
            float[] backAngles = { -140f, -88f, -35f, 15f };
            float[] backCurls = { -55f, -35f, 40f, 60f };
            for (int i = 0; i < 4; i++) Arm(p, bx, by + 1f, -80f + (backAngles[i] + 80f) * spread, armLength * (0.86f + (i % 2) * 0.08f), backCurls[i], 3f, back, Color.Lerp(sucker, back, 0.35f));
            float[] frontAngles = { -168f, -115f, -60f, -8f };
            float[] frontCurls = { -70f, -45f, 50f, 70f };
            for (int i = 0; i < 4; i++) Arm(p, bx, by, -80f + (frontAngles[i] + 80f) * spread, armLength * (1f - (i % 2) * 0.08f), frontCurls[i], 3.6f, skin, sucker);

            // La toile à la base des bras, la tête, et le manteau en sac rejeté en arrière, vers le haut à gauche.
            CreatureArt.Blob(p, bx, by, 8f, 5f, skin);
            CreatureArt.Blob(p, 42f, 51f, 17f, 12.5f, skin, -0.66f);
            CreatureArt.Blob(p, 59f, 37f, 9f, 8f, skin);
            Mottle(p, seed, light, dark, 90, 2.2f);
            Eye(p, 63, 40, iris);
            return p;
        }

        /// <summary>Poulpe cyané (poulpe de jour) : brun chaud marbré de crème, et un faux œil sombre cerclé de bleu à la base des bras.</summary>
        public static Color[,] DayOctopus()
        {
            Color[,] p = Octopus(C(0.58f, 0.40f, 0.26f), C(0.32f, 0.20f, 0.14f), C(0.88f, 0.78f, 0.58f), C(0.92f, 0.82f, 0.70f), C(0.86f, 0.70f, 0.36f), 601, 42f);
            // Le faux œil (ocelle) : rond sombre cerclé de bleu.
            for (int x = 57; x <= 69; x++)
            {
                for (int y = 24; y <= 34; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(63f, 29f));
                    if (p[x, y].a < 0.5f) continue;
                    if (d <= 2.2f) p[x, y] = C(0.10f, 0.08f, 0.10f);
                    else if (d <= 3.6f) p[x, y] = C(0.30f, 0.52f, 0.90f);
                }
            }

            return CreatureArt.Finish(p, C(0.08f, 0.04f, 0.02f));
        }

        /// <summary>Poulpe géant du Pacifique : grand, rouge brique à orangé, peau ridée de plis sombres, très longs bras.</summary>
        public static Color[,] GiantPacificOctopus()
        {
            Color[,] p = Octopus(C(0.72f, 0.30f, 0.20f), C(0.44f, 0.14f, 0.10f), C(0.92f, 0.56f, 0.40f), C(0.98f, 0.82f, 0.72f), C(0.80f, 0.62f, 0.30f), 602, 50f, 1.1f);
            // Plis de la peau : courtes lignes sombres sur le manteau.
            var random = new System.Random(6021);
            for (int i = 0; i < 30; i++)
            {
                int x = random.Next(18, 60), y = random.Next(36, 70);
                for (int k = 0; k < 3; k++)
                {
                    if (p[x + k, y].a > 0.5f) p[x + k, y] = Color.Lerp(p[x + k, y], C(0.30f, 0.08f, 0.06f), 0.5f);
                }
            }

            return CreatureArt.Finish(p, C(0.10f, 0.03f, 0.02f));
        }

        /// <summary>Poulpe commun : gris-brun moucheté de beige et de points sombres, peau granuleuse.</summary>
        public static Color[,] CommonOctopus()
        {
            Color[,] p = Octopus(C(0.56f, 0.46f, 0.38f), C(0.30f, 0.24f, 0.20f), C(0.86f, 0.80f, 0.66f), C(0.90f, 0.84f, 0.76f), C(0.80f, 0.74f, 0.44f), 603, 44f, 0.95f);
            Mottle(p, 6031, C(0.94f, 0.90f, 0.80f), C(0.20f, 0.16f, 0.12f), 140, 0.8f);
            return CreatureArt.Finish(p, C(0.06f, 0.05f, 0.04f));
        }

        /// <summary>
        /// Seiche à larges bras : manteau ovale bordé d'une nageoire en jupe qui ondule, tête aux yeux à pupille en W,
        /// huit bras courts en pinceau devant et deux massues ; livrée brune barrée de blanc.
        /// </summary>
        public static Color[,] BroadclubCuttlefish()
        {
            var p = new Color[104, 56];
            Color skin = C(0.56f, 0.42f, 0.30f);
            Color fin = C(0.84f, 0.76f, 0.62f);
            const float cy = 28f;
            // Nageoire en jupe, tout autour du manteau, qui ondule.
            for (int x = 12; x <= 70; x++)
            {
                float t = Mathf.InverseLerp(12f, 70f, x);
                float half = 13f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(t * 2f - 1f, 2f))) + 2.5f + Mathf.Sin(x * 0.9f) * 0.8f;
                for (int y = Mathf.RoundToInt(cy - half); y <= Mathf.RoundToInt(cy + half); y++)
                {
                    PixelArt.Set(p, x, y, DetailedFish.Shade(fin, y > cy ? 1.05f : 0.86f));
                }
            }

            // Bras en pinceau devant la tête, et les deux massues.
            for (int i = 0; i < 6; i++)
            {
                Arm(p, 80f, cy - 4f + i * 1.6f, -10f + i * 4f, 16f - Mathf.Abs(i - 2.5f) * 2f, (i - 2.5f) * 8f, 2f, skin, fin, false);
            }

            Arm(p, 80f, cy - 2f, -4f, 22f, 20f, 1.2f, Color.Lerp(skin, Color.white, 0.15f), fin, false);
            CreatureArt.Blob(p, 101f, cy + 1f, 2.4f, 1.6f, Color.Lerp(skin, Color.white, 0.15f));

            // Manteau et tête.
            CreatureArt.Blob(p, 42f, cy, 30f, 12f, skin);
            CreatureArt.Blob(p, 76f, cy, 8f, 8f, skin);
            // Barres blanches en travers du dos, et marbrures.
            for (int x = 18; x <= 68; x += 7)
            {
                for (int y = (int)cy; y <= cy + 12; y++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        int bx = x + k + (y - (int)cy) / 3;
                        if (p[bx, y].a > 0.5f && Mathf.Abs(y - cy) < 11f) p[bx, y] = Color.Lerp(p[bx, y], C(0.96f, 0.94f, 0.88f), 0.55f);
                    }
                }
            }

            Mottle(p, 604, C(0.80f, 0.70f, 0.52f), C(0.30f, 0.20f, 0.14f), 60, 1.6f);
            Eye(p, 76, 31, C(0.78f, 0.70f, 0.40f), true);
            return CreatureArt.Finish(p, C(0.08f, 0.05f, 0.03f));
        }

        /// <summary>
        /// Calmar luciole : petit calmar rouge translucide, manteau en torpille à nageoires en losange, gros yeux, bras
        /// devant ; constellé de points lumineux bleus, trois gros au bout des bras du bas.
        /// </summary>
        public static Color[,] FireflySquid()
        {
            var p = new Color[96, 40];
            Color skin = C(0.72f, 0.32f, 0.34f);
            Color glow = C(0.40f, 0.86f, 1f);
            const float cy = 20f;
            // Nageoires en losange au bout du manteau (à gauche).
            for (int x = 6; x <= 26; x++)
            {
                float t = Mathf.InverseLerp(6f, 26f, x);
                float half = 9f * (t < 0.5f ? t * 2f : (1f - t) * 2f) + 2f;
                for (int y = Mathf.RoundToInt(cy - half); y <= Mathf.RoundToInt(cy + half); y++)
                {
                    PixelArt.Set(p, x, y, DetailedFish.Shade(C(0.80f, 0.46f, 0.46f), y > cy ? 1.05f : 0.86f));
                }
            }

            // Bras devant, serrés, les deux du bas plus longs.
            for (int i = 0; i < 6; i++)
            {
                Arm(p, 66f, cy - 3f + i * 1.2f, -14f + i * 5f, 18f + (i == 0 || i == 5 ? 6f : 0f), (i - 2.5f) * 6f, 1.6f, skin, C(0.92f, 0.70f, 0.70f), false);
            }

            // Manteau en torpille et tête.
            for (int i = 0; i < 12; i++)
            {
                float x = Mathf.Lerp(8f, 54f, i / 11f);
                float r = Mathf.Lerp(2f, 6.5f, Mathf.Sqrt(i / 11f));
                CreatureArt.Blob(p, x, cy, 3.5f, r, skin);
            }

            CreatureArt.Blob(p, 60f, cy, 6f, 5.5f, skin);
            // Points lumineux sur tout le corps, et les trois gros au bout des bras.
            var random = new System.Random(605);
            for (int i = 0; i < 46; i++)
            {
                int x = random.Next(6, 70), y = random.Next(8, 32);
                if (p[x, y].a > 0.5f) p[x, y] = Color.Lerp(glow, Color.white, 0.2f);
            }

            foreach (Vector2 tip in new[] { new Vector2(88, 13), new Vector2(87, 26), new Vector2(84, 15) })
            {
                CreatureArt.Blob(p, tip.x, tip.y, 1.6f, 1.6f, glow);
            }

            Eye(p, 61, 22, C(0.34f, 0.30f, 0.40f));
            return CreatureArt.Finish(p, C(0.12f, 0.04f, 0.06f));
        }
    }
}
