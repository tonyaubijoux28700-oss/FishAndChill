using System;
using UnityEngine;

namespace FishAndChill.Collection
{
    /// <summary>La progression du joueur pendant la partie : sac, aquarium et pièces.</summary>
    public sealed class PlayerProgress : MonoBehaviour
    {
        [SerializeField, Min(1), Tooltip("Places dans le sac au début du jeu.")]
        private int _startingBagCapacity = 3;

        public event Action Changed;

        public Inventory Bag { get; private set; }
        public AquariumCollection Aquarium { get; private set; }
        public Wallet Wallet { get; private set; }

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
            Bag.Changed += RaiseChanged;
            Aquarium.Changed += RaiseChanged;
            Wallet.Changed += RaiseChanged;
        }

        public DepositResult DepositBag() => Aquarium.Deposit(Bag, Wallet);

        private void RaiseChanged() => Changed?.Invoke();
    }
}
