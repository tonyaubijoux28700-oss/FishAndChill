using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.Collection
{
    /// <summary>
    /// Règles du marchand (logique pure, testée) :
    /// il n'achète que les doublons — une espèce déjà présente dans l'aquarium, ou un exemplaire en trop
    /// d'une espèce du sac. Il laisse toujours le plus gros exemplaire de chaque nouvelle espèce pour l'aquarium.
    /// Le prix dépend de la taille : de 80 % du prix de base (le plus petit) à 130 % (le plus gros).
    /// </summary>
    public static class FishShop
    {
        public const float MinPriceFactor = 0.8f;
        public const float MaxPriceFactor = 1.3f;

        public static int PriceOf(CaughtFish fish)
        {
            if (fish == null || fish.Species == null)
            {
                return 0;
            }

            Vector2 range = fish.Species.SizeRangeCm;
            float t = range.y > range.x ? Mathf.InverseLerp(range.x, range.y, fish.SizeCm) : 0.5f;
            return Mathf.Max(1, Mathf.RoundToInt(fish.Species.BasePrice * Mathf.Lerp(MinPriceFactor, MaxPriceFactor, t)));
        }

        /// <summary>Le marchand accepte-t-il d'acheter le poisson de la place <paramref name="slot"/> ?</summary>
        public static bool IsSellable(IReadOnlyList<CaughtFish> bag, int slot, AquariumCollection aquarium)
        {
            if (bag == null || slot < 0 || slot >= bag.Count)
            {
                return false;
            }

            CaughtFish fish = bag[slot];
            if (aquarium != null && aquarium.Contains(fish.SpeciesId))
            {
                return true;
            }

            // Nouvelle espèce : on garde le plus gros exemplaire, les autres sont des doublons.
            return KeeperSlot(bag, fish.SpeciesId) != slot;
        }

        /// <summary>Vend le poisson de la place <paramref name="slot"/> ; renvoie le gain (0 si refusé).</summary>
        public static int Sell(Inventory bag, int slot, Wallet wallet, AquariumCollection aquarium)
        {
            if (bag == null || wallet == null || !IsSellable(bag.Fish, slot, aquarium))
            {
                return 0;
            }

            CaughtFish sold = bag.RemoveAt(slot);
            int price = PriceOf(sold);
            wallet.Add(price);
            return price;
        }

        /// <summary>Vend tous les doublons du sac ; renvoie le gain total.</summary>
        public static int SellAll(Inventory bag, Wallet wallet, AquariumCollection aquarium)
        {
            int total = 0;
            for (int slot = bag.Count - 1; slot >= 0; slot--)
            {
                if (IsSellable(bag.Fish, slot, aquarium))
                {
                    total += Sell(bag, slot, wallet, aquarium);
                }
            }

            return total;
        }

        /// <summary>Place du plus gros exemplaire de l'espèce dans le sac (celui que l'on garde), ou -1.</summary>
        public static int KeeperSlot(IReadOnlyList<CaughtFish> bag, string speciesId)
        {
            int keeper = -1;
            for (int i = 0; i < bag.Count; i++)
            {
                if (bag[i].SpeciesId == speciesId && (keeper < 0 || bag[i].SizeCm > bag[keeper].SizeCm))
                {
                    keeper = i;
                }
            }

            return keeper;
        }
    }
}
