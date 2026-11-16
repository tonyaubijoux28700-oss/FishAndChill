using System.Collections.Generic;
using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.Gear
{
    /// <summary>Tous les appâts du jeu, dans l'ordre du marchand. Le premier est l'appât de départ, gratuit et illimité.</summary>
    [CreateAssetMenu(menuName = "FishAndChill/Catalogue d'appâts", fileName = "CatalogueAppats")]
    public sealed class BaitCatalog : ScriptableObject
    {
        [SerializeField] private List<Bait> _baits = new List<Bait>();

        public IReadOnlyList<Bait> Baits => _baits;

        /// <summary>Appât de départ (toujours possédé).</summary>
        public Bait Default => _baits.Count > 0 ? _baits[0] : null;

        public Bait Find(string id)
        {
            foreach (Bait bait in _baits)
            {
                if (bait != null && bait.Id == id)
                {
                    return bait;
                }
            }

            return null;
        }

        /// <summary>Appât préféré d'une espèce (le premier qui la compte parmi ses favorites, hors appât de départ).</summary>
        public Bait FavoriteOf(FishSpecies species)
        {
            for (int i = 1; i < _baits.Count; i++)
            {
                if (_baits[i] != null && _baits[i].IsFavorite(species))
                {
                    return _baits[i];
                }
            }

            return Default != null && Default.IsFavorite(species) ? Default : null;
        }

        public void Configure(IEnumerable<Bait> baits)
        {
            _baits = new List<Bait>(baits);
        }
    }
}
