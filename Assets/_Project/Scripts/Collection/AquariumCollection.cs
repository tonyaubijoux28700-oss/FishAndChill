using System;
using System.Collections.Generic;

namespace FishAndChill.Collection
{
    /// <summary>
    /// L'aquarium : un exemplaire de chaque espèce. Au dépôt, chaque nouvelle espèce y entre ;
    /// tout poisson d'une espèce déjà présente (doublon) est vendu.
    /// </summary>
    public sealed class AquariumCollection
    {
        private readonly Dictionary<string, CaughtFish> _species = new Dictionary<string, CaughtFish>();

        public event Action Changed;

        public int SpeciesCount => _species.Count;
        public IEnumerable<CaughtFish> Specimens => _species.Values;

        public bool Contains(string speciesId) => _species.ContainsKey(speciesId);

        /// <summary>Vide le sac dans l'aquarium et crédite le porte-monnaie pour les doublons vendus.</summary>
        public DepositResult Deposit(Inventory inventory, Wallet wallet)
        {
            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            var result = new DepositResult();
            foreach (CaughtFish fish in inventory.TakeAll())
            {
                if (_species.ContainsKey(fish.SpeciesId))
                {
                    result.Sold.Add(fish);
                    result.CoinsEarned += fish.SalePrice;
                }
                else
                {
                    _species.Add(fish.SpeciesId, fish);
                    result.NewSpecies.Add(fish);
                }
            }

            wallet.Add(result.CoinsEarned);
            if (result.NewSpecies.Count > 0)
            {
                Changed?.Invoke();
            }

            return result;
        }
    }
}
