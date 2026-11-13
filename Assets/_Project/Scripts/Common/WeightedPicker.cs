using System;
using System.Collections.Generic;

namespace FishAndChill.Common
{
    /// <summary>Tirage aléatoire pondéré (ex. : quel poisson mord, selon sa fréquence dans la zone).</summary>
    public static class WeightedPicker
    {
        /// <summary>
        /// Renvoie l'index choisi pour un tirage <paramref name="roll"/> entre 0 (inclus) et 1 (exclu),
        /// ou -1 si aucun poids n'est positif. Les poids négatifs ou nuls sont ignorés.
        /// </summary>
        public static int Pick(IReadOnlyList<float> weights, double roll)
        {
            if (weights == null)
            {
                throw new ArgumentNullException(nameof(weights));
            }

            double total = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                total += Math.Max(0f, weights[i]);
            }

            if (total <= 0)
            {
                return -1;
            }

            double threshold = Math.Clamp(roll, 0d, 0.999999d) * total;
            double cumulative = 0;
            int lastPositive = -1;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0f)
                {
                    continue;
                }

                lastPositive = i;
                cumulative += weights[i];
                if (threshold < cumulative)
                {
                    return i;
                }
            }

            return lastPositive;
        }
    }
}
