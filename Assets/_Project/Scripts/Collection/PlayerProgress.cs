using System;
using FishAndChill.Gear;
using UnityEngine;

namespace FishAndChill.Collection
{
    /// <summary>La progression du joueur pendant la partie : sac, aquarium, pièces et matériel.</summary>
    public sealed class PlayerProgress : MonoBehaviour
    {
        [SerializeField, Min(1), Tooltip("Places dans le sac au début du jeu.")]
        private int _startingBagCapacity = 3;

        [SerializeField, Tooltip("Le matériel vendu par le marchand (canne, hameçon, appât, sac).")]
        private GearCatalog _gearCatalog;

        public event Action Changed;

        public Inventory Bag { get; private set; }
        public AquariumCollection Aquarium { get; private set; }
        public Wallet Wallet { get; private set; }
        public GearLoadout Gear { get; private set; }
        public GearCatalog GearCatalog => _gearCatalog;

        /// <summary>Ce que le matériel actuel change à la pêche.</summary>
        public GearEffects GearEffects => GearEffects.From(_gearCatalog, Gear);

        private void Awake()
        {
            EnsureCreated();
        }

        /// <summary>Crée les données si besoin (utile si un autre composant y accède avant Awake).</summary>
        public void EnsureCreated()
        {
            if (Bag != null)
            {
                return;
            }

            Bag = new Inventory(_startingBagCapacity);
            Aquarium = new AquariumCollection();
            Wallet = new Wallet();
            Gear = new GearLoadout();
            Bag.Changed += RaiseChanged;
            Aquarium.Changed += RaiseChanged;
            Wallet.Changed += RaiseChanged;
            Gear.Changed += RaiseChanged;
        }

        public DepositResult DepositBag() => Aquarium.Deposit(Bag);

        /// <summary>
        /// Achète le niveau suivant de cette famille de matériel. Un nouveau sac agrandit tout de suite le sac.
        /// </summary>
        public GearPurchase BuyGear(GearCategory category)
        {
            EnsureCreated();
            GearLine line = _gearCatalog != null ? _gearCatalog.Line(category) : null;
            GearPurchase result = GearShop.TryBuy(line, Gear, Wallet);
            if (result == GearPurchase.Bought && category == GearCategory.Bag)
            {
                Bag.UpgradeCapacity(GearEffects.BagCapacity);
            }

            return result;
        }

        private readonly System.Collections.Generic.HashSet<string> _claimedRewards = new System.Collections.Generic.HashSet<string>();

        /// <summary>Récompenses de collection déjà encaissées (clés « zone:palier », voir <see cref="CollectionRewards"/>).</summary>
        public System.Collections.Generic.ICollection<string> ClaimedRewards => _claimedRewards;

        /// <summary>Encaisse une récompense de collection (une seule fois) : ajoute les pièces.</summary>
        public bool ClaimReward(string key, int coins)
        {
            EnsureCreated();
            if (string.IsNullOrEmpty(key) || !_claimedRewards.Add(key))
            {
                return false;
            }

            Wallet.Add(coins); // déclenche Changed : la sauvegarde suit
            return true;
        }

        /// <summary>Remet les récompenses encaissées (chargement d'une sauvegarde), sans redonner les pièces.</summary>
        public void RestoreClaimedRewards(System.Collections.Generic.IEnumerable<string> keys)
        {
            _claimedRewards.Clear();
            if (keys != null)
            {
                foreach (string key in keys)
                {
                    _claimedRewards.Add(key);
                }
            }
        }

        private void RaiseChanged() => Changed?.Invoke();
    }
}
