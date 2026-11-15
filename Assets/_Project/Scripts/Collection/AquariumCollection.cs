using System;
using System.Collections.Generic;

namespace FishAndChill.Collection
{
    /// <summary>
    /// L'aquarium : un exemplaire de chaque espèce (le plus gros qu'on y ait déposé).
    /// Au dépôt, le plus gros poisson de chaque nouvelle espèce y entre ; un poisson plus gros que celui de
    /// l'aquarium prend sa place (l'ancien revient dans le sac, pour le marchand). Les doublons restent dans le sac.
    /// </summary>
    public sealed class AquariumCollection
    {
        private readonly Dictionary<string, CaughtFish> _species = new Dictionary<string, CaughtFish>();

        public event Action Changed;

        public int SpeciesCount => _species.Count;
        public IEnumerable<CaughtFish> Specimens => _species.Values;

        public bool Contains(string speciesId) => _species.ContainsKey(speciesId);

        /// <summary>L'exemplaire exposé de cette espèce (null si absente).</summary>
        public CaughtFish SpecimenOf(string speciesId)
        {
            return _species.TryGetValue(speciesId, out CaughtFish fish) ? fish : null;
        }

        /// <summary>Remet un exemplaire exposé tel qu'il était dans une sauvegarde.</summary>
        public void Restore(CaughtFish specimen)
        {
            if (specimen == null || specimen.Species == null)
            {
                return;
            }

            _species[specimen.SpeciesId] = specimen;
            Changed?.Invoke();
        }

        /// <summary>Dépose ce qui doit aller dans l'aquarium ; le reste (doublons) reste dans le sac.</summary>
        public DepositResult Deposit(Inventory bag)
        {
            if (bag == null)
            {
                throw new ArgumentNullException(nameof(bag));
            }

            var result = new DepositResult();

            // Le plus gros exemplaire de chaque espèce présente dans le sac.
            var biggest = new Dictionary<string, int>();
            for (int slot = 0; slot < bag.Count; slot++)
            {
                string id = bag.Fish[slot].SpeciesId;
                if (!biggest.TryGetValue(id, out int best) || bag.Fish[slot].SizeCm > bag.Fish[best].SizeCm)
                {
                    biggest[id] = slot;
                }
            }

            // Records : on échange avec l'exemplaire de l'aquarium (les places du sac ne bougent pas).
            var newSlots = new List<int>();
            foreach (KeyValuePair<string, int> entry in biggest)
            {
                CaughtFish current = SpecimenOf(entry.Key);
                if (current == null)
                {
                    newSlots.Add(entry.Value);
                }
                else if (bag.Fish[entry.Value].SizeCm > current.SizeCm)
                {
                    CaughtFish record = bag.Replace(entry.Value, current);
                    _species[entry.Key] = record;
                    result.Upgraded.Add(record);
                }
            }

            // Nouvelles espèces : retirées du sac en partant de la fin pour garder les numéros de place valides.
            newSlots.Sort();
            for (int i = newSlots.Count - 1; i >= 0; i--)
            {
                CaughtFish fish = bag.RemoveAt(newSlots[i]);
                _species.Add(fish.SpeciesId, fish);
                result.NewSpecies.Insert(0, fish);
            }

            result.LeftInBag = bag.Count;
            if (!result.IsEmpty)
            {
                Changed?.Invoke();
            }

            return result;
        }
    }
}
