using System.Collections.Generic;
using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.Villagers
{
    /// <summary>
    /// Règles des commandes des habitants (logique pure, testée) : quelle espèce demander, combien elle rapporte, et quel
    /// poisson du sac donner.
    /// </summary>
    public static class RequestRules
    {
        /// <summary>Résultat de <see cref="DeliverySlot"/> : rien de l'espèce dans le sac.</summary>
        public const int NoFish = -1;

        /// <summary>Résultat de <see cref="DeliverySlot"/> : le seul exemplaire, pas encore à l'aquarium, le joueur le garde.</summary>
        public const int KeepForAquarium = -2;

        /// <summary>Chances d'être demandée : communes 3, peu communes 2, rares 1 ; jamais d'épique ni de légendaire.</summary>
        public static int Weight(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common: return 3;
                case Rarity.Uncommon: return 2;
                case Rarity.Rare: return 1;
                default: return 0;
            }
        }

        /// <summary>Tire une espèce parmi les candidates (doublons ignorés) ; null s'il n'y en a aucune possible.</summary>
        public static FishSpecies PickSpecies(System.Random random, IEnumerable<FishSpecies> candidates)
        {
            var pool = new List<FishSpecies>();
            var seen = new HashSet<string>();
            int total = 0;
            foreach (FishSpecies species in candidates)
            {
                if (species == null || !seen.Add(species.Id) || Weight(species.Rarity) == 0) continue;
                pool.Add(species);
                total += Weight(species.Rarity);
            }

            if (total == 0) return null;
            int roll = random.Next(total);
            foreach (FishSpecies species in pool)
            {
                roll -= Weight(species.Rarity);
                if (roll < 0) return species;
            }

            return pool[pool.Count - 1];
        }

        /// <summary>Récompense : deux fois le prix de base, plus un bonus selon la rareté (au moins 10 pièces).</summary>
        public static int Reward(int basePrice, Rarity rarity)
        {
            int bonus = rarity == Rarity.Rare ? 40 : rarity == Rarity.Uncommon ? 15 : 5;
            return Mathf.Max(10, basePrice * 2 + bonus);
        }

        /// <summary>
        /// Place du poisson à donner : le plus petit de l'espèce. <see cref="KeepForAquarium"/> si c'est le seul et que
        /// l'espèce n'est pas encore exposée ; <see cref="NoFish"/> s'il n'y en a pas.
        /// </summary>
        public static int DeliverySlot(IReadOnlyList<CaughtFish> bag, string speciesId, bool inAquarium)
        {
            int smallest = NoFish;
            int count = 0;
            for (int i = 0; i < bag.Count; i++)
            {
                if (bag[i].SpeciesId != speciesId) continue;
                count++;
                if (smallest < 0 || bag[i].SizeCm < bag[smallest].SizeCm) smallest = i;
            }

            if (count == 1 && !inAquarium) return KeepForAquarium;
            return smallest;
        }
    }
}
