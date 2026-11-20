using System;
using System.Collections.Generic;
using FishAndChill.Collection;

namespace FishAndChill.Pets
{
    /// <summary>
    /// Les animaux du joueur (logique pure, testée) : ceux qu'il a achetés et celui qui l'accompagne (un seul à la fois,
    /// ou aucun). Changer d'animal est gratuit.
    /// </summary>
    public sealed class PetCollection
    {
        private readonly List<string> _owned = new List<string>();

        public event Action Changed;

        public IReadOnlyList<string> Owned => _owned;

        /// <summary>L'animal qui accompagne le joueur (null : aucun).</summary>
        public string Equipped { get; private set; }

        public bool Owns(string id) => id != null && _owned.Contains(id);

        /// <summary>Achète un animal (pièces dépensées) ; le nouvel animal accompagne aussitôt le joueur.</summary>
        public PetPurchase TryBuy(string id, int price, Wallet wallet)
        {
            if (string.IsNullOrEmpty(id) || wallet == null) return PetPurchase.Invalid;
            if (Owns(id)) return PetPurchase.AlreadyOwned;
            if (!wallet.TrySpend(price)) return PetPurchase.NotEnoughCoins;
            _owned.Add(id);
            Equipped = id;
            Changed?.Invoke();
            return PetPurchase.Bought;
        }

        /// <summary>Prend un animal avec soi (seulement s'il est à nous) ; null le laisse à la maison.</summary>
        public bool Equip(string id)
        {
            if (id != null && !Owns(id)) return false;
            if (Equipped == id) return true;
            Equipped = id;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Recharge depuis la sauvegarde (un animal équipé inconnu est ignoré).</summary>
        public void Load(IEnumerable<string> owned, string equipped)
        {
            _owned.Clear();
            if (owned != null)
            {
                foreach (string id in owned)
                {
                    if (!string.IsNullOrEmpty(id) && !_owned.Contains(id)) _owned.Add(id);
                }
            }

            Equipped = Owns(equipped) ? equipped : null;
            Changed?.Invoke();
        }
    }
}
