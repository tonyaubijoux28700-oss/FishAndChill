using System;
using System.Collections.Generic;

namespace FishAndChill.Fishing
{
    /// <summary>
    /// Prévoit les « titillements » avant la touche : le poisson taquine l'appât 1 à 3 fois (le bouchon tressaille),
    /// puis mord vraiment. Ferrer sur un titillement, c'est trop tôt.
    /// </summary>
    public static class NibblePlanner
    {
        /// <summary>Temps minimum entre deux titillements, et entre le dernier et la vraie touche.</summary>
        public const float MinGap = 0.55f;

        /// <summary>Temps minimum avant le premier titillement (le bouchon se pose d'abord).</summary>
        public const float FirstDelay = 0.5f;

        /// <summary>Attente minimale pour placer <paramref name="count"/> titillements avant la touche.</summary>
        public static float MinimumWait(int count) => FirstDelay + count * MinGap;

        /// <summary>
        /// Instants des titillements (en secondes depuis la pose du bouchon), triés, pour une touche à
        /// <paramref name="biteTime"/> secondes. Le nombre est tiré entre <paramref name="min"/> et <paramref name="max"/>,
        /// réduit si l'attente est trop courte pour les espacer.
        /// </summary>
        public static float[] Plan(Random random, float biteTime, int min = 1, int max = 3)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            int count = random.Next(min, max + 1);
            while (count > 0 && MinimumWait(count) > biteTime)
            {
                count--;
            }

            var times = new List<float>(count);
            // On répartit les titillements dans des tranches égales, avec un peu de hasard dans chaque tranche.
            float start = FirstDelay;
            float end = biteTime - MinGap;
            float slot = count > 0 ? (end - start) / count : 0f;
            for (int i = 0; i < count; i++)
            {
                float earliest = i == 0 ? start : Math.Max(start + slot * i, times[i - 1] + MinGap);
                float latest = Math.Max(earliest, start + slot * (i + 1) - (i == count - 1 ? 0f : MinGap * 0.5f));
                times.Add(earliest + (float)random.NextDouble() * (latest - earliest));
            }

            return times.ToArray();
        }
    }
}
