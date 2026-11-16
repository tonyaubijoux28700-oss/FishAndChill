using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Créatures du fleuve amazonien (dessins originaux d'après l'anatomie réelle, même style que <see cref="CreatureArt"/>) :
    /// la tortue matamata et la crevette d'Amazonie.
    /// </summary>
    public static class AmazonCreatureArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        /// <summary>
        /// Matamata de profil, tête à droite : carapace brun-roux à trois crêtes bosselées, cou épais et long garni de
        /// franges de peau, tête plate et triangulaire au museau en tuba, minuscules yeux, pattes courtes palmées.
        /// </summary>
        public static Color[,] Matamata()
        {
            var p = new Color[104, 52];
            Color shell = C(0.44f, 0.30f, 0.18f);
            Color ridge = C(0.30f, 0.20f, 0.12f);
            Color skin = C(0.52f, 0.42f, 0.28f);
            Color fringe = C(0.66f, 0.54f, 0.36f);

            // Pattes (côté caché plus sombre, puis côté visible).
            CreatureArt.Blob(p, 22f, 12f, 6f, 4f, Color.Lerp(skin, Color.black, 0.3f), 0.4f);
            CreatureArt.Blob(p, 60f, 11f, 6f, 4f, Color.Lerp(skin, Color.black, 0.3f), -0.4f);
            CreatureArt.Limb(p, 28f, 18f, 24f, 9f, 3.4f, skin);
            CreatureArt.Limb(p, 54f, 18f, 58f, 8f, 3.4f, skin);

            // Carapace basse et rugueuse : trois crêtes de bosses sur la longueur.
            CreatureArt.Blob(p, 40f, 22f, 30f, 6f, C(0.60f, 0.52f, 0.36f));   // plastron
            CreatureArt.Blob(p, 40f, 27f, 31f, 14f, shell);
            for (int ridgeRow = 0; ridgeRow < 3; ridgeRow++)
            {
                float ry = 30f + ridgeRow * 4f;
                for (int k = 0; k < 5; k++)
                {
                    float rx = 22f + k * 9f + ridgeRow * 2f;
                    if (PixelArt.IsFilled(p, Mathf.RoundToInt(rx), Mathf.RoundToInt(ry))) CreatureArt.Blob(p, rx, ry, 3f, 2.2f, ridge);
                }
            }

            // Cou long et épais, frangé de lambeaux de peau.
            CreatureArt.Limb(p, 66f, 26f, 86f, 26f, 5f, skin);
            for (int x = 66; x <= 86; x += 3)
            {
                CreatureArt.Limb(p, x, 21f, x - 1f, 18f, 1f, fringe);
                CreatureArt.Limb(p, x + 1f, 31f, x, 33f, 0.9f, fringe);
            }

            // Tête plate et triangulaire, museau en tuba.
            for (int x = 82; x <= 100; x++)
            {
                float t = (x - 82) / 18f;
                float half = Mathf.Lerp(7f, 2f, t);
                for (int y = Mathf.RoundToInt(26f - half * 0.6f); y <= Mathf.RoundToInt(26f + half * 0.5f); y++)
                {
                    PixelArt.Set(p, x, y, DetailedFish.Shade(skin, y > 26 ? 1.12f : 0.92f));
                }
            }

            CreatureArt.Limb(p, 99f, 27f, 103f, 28f, 1f, skin);                         // tuba
            PixelArt.Set(p, 92, 28, C(0.06f, 0.05f, 0.04f));                              // petit œil
            CreatureArt.Limb(p, 82f, 22f, 98f, 24f, 0.6f, Color.Lerp(skin, Color.black, 0.4f)); // bouche très large
            CreatureArt.Limb(p, 84f, 30f, 82f, 34f, 1f, fringe);                         // franges de la tête
            CreatureArt.Limb(p, 88f, 30f, 87f, 33f, 1f, fringe);
            CreatureArt.Limb(p, 10f, 24f, 3f, 21f, 2f, skin);                            // petite queue
            return CreatureArt.Finish(p, C(0.12f, 0.08f, 0.04f));
        }

        /// <summary>
        /// Crevette d'Amazonie de profil, tête à droite : corps translucide gris-vert aux reflets bleutés, abdomen
        /// courbé en segments, très longues pinces fines (deuxième paire), rostre dentelé et longues antennes.
        /// </summary>
        public static Color[,] AmazonPrawn()
        {
            var p = new Color[104, 44];
            Color body = C(0.62f, 0.70f, 0.66f);
            Color dark = C(0.34f, 0.40f, 0.40f);
            Color claw = C(0.52f, 0.58f, 0.70f);

            // Pattes fines sous le céphalothorax.
            for (int i = 0; i < 4; i++)
            {
                float x = 56f + i * 5f;
                CreatureArt.Limb(p, x, 17f, x - 3f, 8f, 0.7f, dark);
            }

            // Éventail de la queue.
            for (int lobe = -1; lobe <= 1; lobe++)
            {
                float angle = lobe * 0.42f - 0.5f;
                CreatureArt.Blob(p, 13f - Mathf.Cos(angle) * 4f, 14f + Mathf.Sin(angle) * 6f, 5f, 2f, Color.Lerp(body, claw, 0.4f), angle);
            }

            // Abdomen courbé en six anneaux.
            for (int s = 0; s < 6; s++)
            {
                float cx = 18f + s * 6.5f;
                float cy = 16f + Mathf.Sin(s / 5f * Mathf.PI * 0.8f) * 6f;
                CreatureArt.Blob(p, cx, cy, 4.5f, 4.5f + s * 0.4f, body);
                PixelArt.Set(p, Mathf.RoundToInt(cx + 3f), Mathf.RoundToInt(cy + 2f), Color.Lerp(body, Color.white, 0.5f));
            }

            // Céphalothorax et rostre dentelé.
            CreatureArt.Blob(p, 64f, 20f, 12f, 6.5f, body);
            CreatureArt.Limb(p, 74f, 23f, 86f, 26f, 1f, body);
            for (int x = 76; x <= 84; x += 3) PixelArt.Set(p, x, 27, dark);
            CreatureArt.Blob(p, 74f, 24f, 1.8f, 1.8f, C(0.06f, 0.06f, 0.08f));             // œil
            PixelArt.Set(p, 74, 25, Color.white);

            // Deuxième paire de pattes : très longues pinces fines, l'une derrière l'autre.
            for (int a = 1; a >= 0; a--)
            {
                Color tint = a == 1 ? Color.Lerp(claw, Color.black, 0.3f) : claw;
                CreatureArt.Limb(p, 66f, 15f + a, 80f, 10f + a * 2f, 1.3f, tint);
                CreatureArt.Limb(p, 80f, 10f + a * 2f, 94f, 12f + a * 3f, 1.1f, tint);
                CreatureArt.Limb(p, 94f, 12f + a * 3f, 101f, 14f + a * 3f, 1f, tint);       // doigt
                CreatureArt.Limb(p, 94f, 11f + a * 3f, 100f, 10f + a * 3f, 0.8f, tint);
            }

            // Longues antennes qui partent vers l'avant puis vers l'arrière au-dessus du dos.
            for (int a = 0; a < 2; a++)
            {
                for (int k = 1; k <= 120; k++)
                {
                    float t = k / 120f;
                    float x = 76f + Mathf.Sin(t * Mathf.PI * 0.8f) * (14f - a * 4f) - t * t * (60f - a * 10f);
                    float y = 25f + Mathf.Sin(t * Mathf.PI * 0.6f) * (16f - a * 3f);
                    PixelArt.Set(p, Mathf.RoundToInt(x), Mathf.RoundToInt(y), a == 0 ? C(0.80f, 0.66f, 0.56f) : C(0.62f, 0.52f, 0.46f));
                }
            }

            return CreatureArt.Finish(p, C(0.12f, 0.16f, 0.18f));
        }
    }
}
