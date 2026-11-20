using System;
using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Common;

namespace FishAndChill.Gear
{
    /// <summary>
    /// Ce qu'on trouve en relevant un casier (logique pure, testée). Le casier se remplit avec les heures de jeu
    /// passées dans l'eau : <see cref="Fill"/> va de 0 (tout juste posé) à 1 (au bout de <c>hoursToFull</c>).
    /// Chaque place du casier a sa chance d'être occupée (la 1re presque sûre quand il est plein, la 3e plus rare), et
    /// les crustacés rares n'y entrent vraiment qu'après une longue attente.
    /// </summary>
    public static class TrapRules
    {
        /// <summary>Chance de chaque place quand le casier est plein (au-delà de 3 places : la dernière valeur).</summary>
        private static readonly float[] SlotChance = { 0.95f, 0.6f, 0.3f };

        /// <summary>En dessous, le casier est encore vide.</summary>
        public const float MinHours = 1f;

        public static float Fill(double hoursInWater, float hoursToFull)
        {
            if (hoursInWater < MinHours) return 0f;
            if (hoursToFull <= 0f) return 1f;
            return (float)Math.Min(1.0, hoursInWater / hoursToFull);
        }

        /// <summary>Heures de jeu encore à attendre avant que le casier soit plein (0 s'il l'est).</summary>
        public static float HoursLeft(double hoursInWater, float hoursToFull) => (float)Math.Max(0.0, hoursToFull - hoursInWater);

        /// <summary>Multiplicateur de fréquence selon la rareté : les rares ont besoin d'un casier bien rempli.</summary>
        public static float RarityFactor(Rarity rarity, float fill)
        {
            switch (rarity)
            {
                case Rarity.Common: return 1f;
                case Rarity.Uncommon: return 0.5f + 0.5f * fill;
                case Rarity.Rare: return 0.15f + 0.85f * fill * fill;
                default: return 0.04f + 0.96f * fill * fill * fill;
            }
        }

        /// <summary>
        /// Tire le contenu du casier parmi <paramref name="candidates"/> (fréquence dans la zone et rareté de chacun) :
        /// de 0 à <paramref name="maxCatch"/> prises. Vide si aucun candidat.
        /// </summary>
        public static List<T> Haul<T>(IList<T> candidates, Func<T, float> weight, Func<T, Rarity> rarity,
            float fill, int maxCatch, Random random)
        {
            var result = new List<T>();
            if (candidates == null || candidates.Count == 0 || fill <= 0f) return result;

            var weights = new float[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                weights[i] = Math.Max(0f, weight(candidates[i])) * RarityFactor(rarity(candidates[i]), fill);
            }

            for (int slot = 0; slot < maxCatch; slot++)
            {
                float chance = fill * SlotChance[Math.Min(slot, SlotChance.Length - 1)];
                if (random.NextDouble() >= chance) break; // une place vide : les suivantes aussi
                int index = WeightedPicker.Pick(weights, random.NextDouble());
                if (index < 0) break;
                result.Add(candidates[index]);
            }

            return result;
        }
    }
}
