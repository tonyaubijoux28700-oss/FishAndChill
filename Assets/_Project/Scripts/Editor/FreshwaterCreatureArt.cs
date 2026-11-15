using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Créatures d'eau douce qui ne sont pas des poissons (lac d'Annecy), même style que <see cref="CreatureArt"/> :
    /// volumes éclairés par paliers et contour sélectif. Dessins originaux d'après l'anatomie réelle, tête à droite.
    /// </summary>
    public static class FreshwaterCreatureArt
    {
        private static Color C(float r, float g, float b) => new Color(r, g, b);

        /// <summary>
        /// Écrevisse signal (Pacifastacus leniusculus) de profil : corps brun-olive lisse, abdomen en anneaux terminé
        /// par un éventail, grosses pinces lisses au dessous rouge et à la tache blanc-bleu à l'articulation (le
        /// « signal »), antennes longues vers l'avant.
        /// </summary>
        public static Color[,] SignalCrayfish()
        {
            var p = new Color[104, 44];
            Color body = C(0.42f, 0.30f, 0.18f);
            Color dark = C(0.22f, 0.15f, 0.09f);
            Color red = C(0.78f, 0.22f, 0.14f);
            Color signal = C(0.80f, 0.92f, 0.94f);

            // Pattes marcheuses, fines, sous le céphalothorax.
            for (int i = 0; i < 4; i++)
            {
                float x = 52f + i * 5f;
                CreatureArt.Limb(p, x, 14f, x - 3f, 8f, 0.9f, Color.Lerp(body, dark, 0.2f));
                CreatureArt.Limb(p, x - 3f, 8f, x - 1f, 3f, 0.8f, Color.Lerp(body, dark, 0.2f));
            }

            // Éventail de la queue.
            for (int lobe = -2; lobe <= 2; lobe++)
            {
                float angle = lobe * 0.36f;
                float lx = 14f - Mathf.Cos(angle) * 6f;
                float ly = 17f + Mathf.Sin(angle) * 8f;
                CreatureArt.Blob(p, lx, ly, 5.5f, 2.3f, lobe == 0 ? Color.Lerp(body, dark, 0.25f) : body, angle);
            }

            // Abdomen : anneaux lisses qui s'élargissent vers la tête, sillon sombre entre chacun.
            for (int s = 0; s < 5; s++)
            {
                float cx = 18f + s * 6.5f;
                float ry = 5f + s * 0.6f;
                CreatureArt.Blob(p, cx, 17f, 4.6f, ry, body);
                int groove = Mathf.RoundToInt(cx + 3f);
                for (int y = Mathf.RoundToInt(17f - ry + 1); y <= Mathf.RoundToInt(17f + ry - 1); y++)
                {
                    PixelArt.Set(p, groove, y, Color.Lerp(body, dark, 0.7f));
                }
            }

            // Céphalothorax lisse, rostre pointu.
            CreatureArt.Blob(p, 60f, 18f, 13f, 8f, body);
            CreatureArt.Limb(p, 70f, 21f, 76f, 21f, 1.2f, body);

            // Œil pédonculé.
            CreatureArt.Blob(p, 72f, 24f, 1.8f, 1.8f, C(0.06f, 0.05f, 0.04f));

            // Antennes longues vers l'avant et vers le haut.
            CreatureArt.Limb(p, 73f, 23f, 90f, 36f, 0.6f, Color.Lerp(body, dark, 0.3f));
            CreatureArt.Limb(p, 90f, 36f, 102f, 40f, 0.5f, Color.Lerp(body, dark, 0.3f));
            CreatureArt.Limb(p, 74f, 21f, 86f, 26f, 0.5f, body);

            // Pince : bras puis grosse pince lisse, dessous rouge, tache « signal » à l'articulation des doigts.
            CreatureArt.Limb(p, 64f, 13f, 75f, 11f, 2.8f, body);
            CreatureArt.Blob(p, 83f, 11f, 9f, 6f, body, 0.05f);
            for (int x = 75; x <= 92; x++)
            {
                for (int y = 5; y <= 8; y++)
                {
                    if (PixelArt.IsFilled(p, x, y)) p[x, y] = Color.Lerp(p[x, y], red, 0.75f);
                }
            }

            CreatureArt.Limb(p, 89f, 14f, 101f, 14f, 2.3f, body);      // doigt fixe
            CreatureArt.Limb(p, 89f, 8f, 100f, 8.5f, 2f, Color.Lerp(body, red, 0.6f)); // doigt mobile
            CreatureArt.Blob(p, 89f, 11f, 2f, 1.8f, signal);
            return CreatureArt.Finish(p, C(0.10f, 0.06f, 0.03f));
        }

        /// <summary>
        /// Grenouille rieuse (Pelophylax ridibundus) assise de profil : peau vert-olive à taches sombres, ligne claire
        /// sur le dos, gros yeux dorés haut placés, tympan brun, longues pattes arrière repliées, ventre blanc taché.
        /// </summary>
        public static Color[,] MarshFrog()
        {
            var p = new Color[72, 48];
            Color skin = C(0.42f, 0.54f, 0.24f);
            Color dark = C(0.20f, 0.26f, 0.12f);
            Color belly = C(0.90f, 0.90f, 0.80f);
            Color stripe = C(0.66f, 0.78f, 0.40f);
            Color gold = C(0.92f, 0.74f, 0.28f);

            // Patte arrière lointaine (derrière), plus sombre.
            CreatureArt.Blob(p, 24f, 12f, 10f, 5f, Color.Lerp(skin, dark, 0.35f), 0.35f);

            // Corps en poire, penché vers l'avant.
            CreatureArt.Blob(p, 32f, 20f, 17f, 10.5f, skin, 0.30f);
            // Ventre clair.
            for (int x = 18; x <= 50; x++)
            {
                for (int y = 8; y <= 18; y++)
                {
                    if (PixelArt.IsFilled(p, x, y) && y < 9 + (x - 18) * 0.3f) p[x, y] = Color.Lerp(p[x, y], belly, 0.8f);
                }
            }

            // Tête large au museau arrondi.
            CreatureArt.Blob(p, 52f, 28f, 11f, 7f, skin, 0.1f);

            // Taches sombres sur le dos et les pattes.
            var random = new System.Random(71);
            for (int i = 0; i < 16; i++)
            {
                int x = random.Next(16, 56);
                int y = random.Next(18, 34);
                if (!PixelArt.IsFilled(p, x, y) || !PixelArt.IsFilled(p, x + 1, y)) continue;
                p[x, y] = Color.Lerp(p[x, y], dark, 0.8f);
                p[x + 1, y] = Color.Lerp(p[x + 1, y], dark, 0.8f);
                if (PixelArt.IsFilled(p, x, y + 1)) p[x, y + 1] = Color.Lerp(p[x, y + 1], dark, 0.6f);
            }

            // Ligne claire au milieu du dos (vue de profil : le long du haut du corps).
            for (int x = 18; x <= 50; x++)
            {
                int y = Mathf.RoundToInt(26f + (x - 18) * 0.28f);
                if (PixelArt.IsFilled(p, x, y + 1) && !PixelArt.IsFilled(p, x, y + 3)) p[x, y] = Color.Lerp(p[x, y], stripe, 0.8f);
            }

            // Patte arrière proche : cuisse, jambe repliée, long pied palmé posé au sol.
            CreatureArt.Blob(p, 22f, 14f, 9f, 5.5f, skin, 0.5f);
            CreatureArt.Limb(p, 14f, 10f, 30f, 6f, 2.4f, skin);
            CreatureArt.Limb(p, 30f, 4f, 44f, 3f, 1.4f, Color.Lerp(skin, dark, 0.15f));
            for (int k = 0; k < 3; k++)
            {
                CreatureArt.Limb(p, 43f, 3f, 48f, 2f + k * 1.5f, 0.6f, Color.Lerp(skin, dark, 0.2f));
            }

            // Bandes sombres sur la cuisse.
            for (int x = 15; x <= 29; x += 4)
            {
                for (int y = 8; y <= 18; y++)
                {
                    if (PixelArt.IsFilled(p, x, y)) p[x, y] = Color.Lerp(p[x, y], dark, 0.55f);
                }
            }

            // Patte avant.
            CreatureArt.Limb(p, 46f, 18f, 50f, 6f, 1.6f, skin);
            CreatureArt.Limb(p, 50f, 6f, 54f, 3f, 1f, skin);

            // Tympan et œil saillant doré.
            CreatureArt.Blob(p, 50f, 29f, 2.6f, 2.6f, C(0.48f, 0.36f, 0.20f));
            CreatureArt.Blob(p, 56f, 34f, 3.6f, 3.4f, skin);
            CreatureArt.Blob(p, 56.5f, 34.5f, 2.4f, 2.2f, gold);
            for (int x = 55; x <= 58; x++) PixelArt.Set(p, x, 34, C(0.05f, 0.05f, 0.04f));
            PixelArt.Set(p, 57, 36, C(1f, 1f, 0.95f));

            // Bouche : longue fente jusque sous l'œil.
            for (int x = 51; x <= 62; x++)
            {
                int y = Mathf.RoundToInt(25f + (x - 51) * 0.08f);
                if (PixelArt.IsFilled(p, x, y)) p[x, y] = Color.Lerp(p[x, y], dark, 0.7f);
            }

            return CreatureArt.Finish(p, C(0.08f, 0.12f, 0.04f));
        }
    }
}
