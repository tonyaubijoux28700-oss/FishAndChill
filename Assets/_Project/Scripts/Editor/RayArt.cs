using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Dessins des raies, vues de dessus (ailes déployées, tête vers la droite), dans le style des poissons : volume
    /// éclairé par paliers (<see cref="DetailedFish.Shade"/>) et contour sélectif. Dessins originaux d'après l'anatomie
    /// réelle (raie manta de récif : dos noir, taches blanches aux épaules, lobes céphaliques devant la bouche).
    /// </summary>
    public static class RayArt
    {
        private static Color C(float r, float g, float b) => new Color(r, g, b);

        private static readonly Vector3 LightDirection = new Vector3(-0.35f, 0.75f, 0.55f).normalized;

        private const int Width = 112;
        private const int Height = 84;

        private static float Step(float light) => Mathf.Round(light * 6f) / 6f;

        /// <summary>Raie manta de récif (Mobula alfredi).</summary>
        public static Color[,] ReefManta()
        {
            var p = new Color[Width, Height];
            Color back = C(0.20f, 0.23f, 0.29f);
            Color patch = C(0.80f, 0.83f, 0.86f);
            Color tail = C(0.10f, 0.11f, 0.14f);

            const float cy = Height / 2f;
            const float headX = 88f;   // bord avant de la tête
            const float tipX = 54f;    // pointe des ailes, un peu en arrière de la tête
            const float baseX = 36f;   // départ de la queue
            const float halfSpan = 39f;

            // Queue fine et petite nageoire dorsale, sous le corps.
            CreatureArt.Limb(p, baseX + 2f, cy, 12f, cy + 1f, 1.1f, tail);
            CreatureArt.Blob(p, baseX + 1f, cy, 3.5f, 2.2f, tail);

            // Hauteur du corps : épaisse au milieu, très fine au bout des ailes.
            float HeightAt(float x, float y)
            {
                float t = Mathf.Abs(y + 0.5f - cy) / halfSpan;
                if (t >= 1f)
                {
                    return 0f;
                }

                // Bord d'attaque arrondi, bord de fuite creusé en croissant.
                float front = tipX + (headX - tipX) * Mathf.Pow(1f - t * t, 0.75f);
                float rear = tipX - (tipX - baseX) * Mathf.Pow(1f - t, 1.7f);
                float px = x + 0.5f;
                if (px < rear || px > front)
                {
                    return 0f;
                }

                // Tête plus étroite que les ailes : une encoche de chaque côté, en avant.
                float edge = Mathf.Min(px - rear, front - px);
                float body = 1f - t * t;
                // Corps central bombé, de la tête à la queue.
                float ridge = Mathf.Exp(-t * t * 18f) * Mathf.Clamp01((px - baseX) / 10f) * Mathf.Clamp01((headX - px) / 6f);
                return Mathf.Min(1f, edge / 9f) * (0.25f + 0.45f * body * body) + 0.6f * ridge;
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    float h = HeightAt(x, y);
                    if (h <= 0f)
                    {
                        continue;
                    }

                    float dx = HeightAt(x + 1, y) - HeightAt(x - 1, y);
                    float dy = HeightAt(x, y + 1) - HeightAt(x, y - 1);
                    var normal = new Vector3(-dx * 2.2f, -dy * 2.2f, 0.3f).normalized;
                    float light = 0.55f + 0.7f * Mathf.Max(0f, Vector3.Dot(normal, LightDirection));

                    // Taches blanches des épaules : deux triangles clairs de part et d'autre de la tête.
                    float t = (y + 0.5f - cy) / halfSpan;
                    float at = Mathf.Abs(t);
                    float u = (x + 0.5f - tipX) / (headX - tipX);
                    bool shoulder = at > 0.16f && at < 0.62f && u > 0.35f && u < 0.92f
                        && u > 0.35f + at * 0.55f && u < 1.02f - at * 0.55f;
                    Color color = shoulder ? Color.Lerp(patch, back, Mathf.InverseLerp(0.45f, 0.62f, at)) : back;

                    // Fines rides sur le dos, le long des ailes.
                    if (!shoulder && (int)(at * 40f) % 7 == 0 && u > 0.2f && at > 0.1f)
                    {
                        light -= 0.08f;
                    }

                    p[x, y] = DetailedFish.Shade(color, Step(light));
                }
            }

            // Arête du dos, légèrement plus claire, de la tête à la queue.
            for (int x = (int)baseX + 3; x < (int)headX - 6; x++)
            {
                int y = Mathf.RoundToInt(cy);
                if (PixelArt.IsFilled(p, x, y))
                {
                    p[x, y] = Color.Lerp(p[x, y], C(0.32f, 0.36f, 0.42f), 0.5f);
                }
            }

            // Lobes céphaliques (enroulés devant la bouche) et bouche large entre eux.
            Color lobe = C(0.16f, 0.18f, 0.22f);
            CreatureArt.Limb(p, headX - 2f, cy + 6f, headX + 5f, cy + 7.5f, 3f, lobe);
            CreatureArt.Limb(p, headX - 2f, cy - 6f, headX + 5f, cy - 7.5f, 3f, lobe);
            CreatureArt.Limb(p, headX + 5f, cy + 7.5f, headX + 6f, cy + 4.5f, 2f, lobe);
            CreatureArt.Limb(p, headX + 5f, cy - 7.5f, headX + 6f, cy - 4.5f, 2f, lobe);
            for (int y = Mathf.RoundToInt(cy - 4f); y <= Mathf.RoundToInt(cy + 4f); y++)
            {
                PixelArt.Set(p, (int)headX, y, C(0.05f, 0.05f, 0.07f));
                PixelArt.Set(p, (int)headX - 1, y, C(0.22f, 0.24f, 0.28f));
            }

            // Yeux sur les côtés de la tête.
            foreach (float side in new[] { -1f, 1f })
            {
                int ex = (int)headX - 5;
                int ey = Mathf.RoundToInt(cy + side * 10f);
                PixelArt.Set(p, ex, ey, C(0.02f, 0.02f, 0.03f));
                PixelArt.Set(p, ex + 1, ey, C(0.02f, 0.02f, 0.03f));
                PixelArt.Set(p, ex + 1, ey + (side > 0 ? 1 : -1), C(0.85f, 0.88f, 0.92f));
            }

            return CreatureArt.Finish(p, C(0.03f, 0.04f, 0.07f));
        }
    }
}
