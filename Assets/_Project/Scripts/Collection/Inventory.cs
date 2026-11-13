using System;
using System.Collections.Generic;

namespace FishAndChill.Collection
{
    /// <summary>Le sac du pêcheur : un nombre limité de places pour les poissons avant l'aquarium.</summary>
    public sealed class Inventory
    {
        private readonly List<CaughtFish> _fish = new List<CaughtFish>();

        public Inventory(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Le sac doit avoir au moins une place.");
            }

            Capacity = capacity;
        }

        public event Action Changed;

        public int Capacity { get; private set; }
        public int Count => _fish.Count;
        public bool IsFull => _fish.Count >= Capacity;
        public IReadOnlyList<CaughtFish> Fish => _fish;

        /// <summary>Ajoute le poisson s'il reste de la place ; renvoie false si le sac est plein.</summary>
        public bool TryAdd(CaughtFish fish)
        {
            if (fish == null)
            {
                throw new ArgumentNullException(nameof(fish));
            }

            if (IsFull)
            {
                return false;
            }

            _fish.Add(fish);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Relâche le poisson de la place <paramref name="slot"/> et met <paramref name="fish"/> à sa place.</summary>
        public CaughtFish Replace(int slot, CaughtFish fish)
        {
            if (fish == null)
            {
                throw new ArgumentNullException(nameof(fish));
            }

            CheckSlot(slot);
            CaughtFish released = _fish[slot];
            _fish[slot] = fish;
            Changed?.Invoke();
            return released;
        }

        public CaughtFish RemoveAt(int slot)
        {
            CheckSlot(slot);
            CaughtFish removed = _fish[slot];
            _fish.RemoveAt(slot);
            Changed?.Invoke();
            return removed;
        }

        /// <summary>Vide le sac et renvoie son contenu (dépôt à l'aquarium).</summary>
        public List<CaughtFish> TakeAll()
        {
            var all = new List<CaughtFish>(_fish);
            _fish.Clear();
            Changed?.Invoke();
            return all;
        }

        /// <summary>Agrandit le sac (amélioration). La capacité ne diminue jamais.</summary>
        public void UpgradeCapacity(int newCapacity)
        {
            if (newCapacity > Capacity)
            {
                Capacity = newCapacity;
                Changed?.Invoke();
            }
        }

        private void CheckSlot(int slot)
        {
            if (slot < 0 || slot >= _fish.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), $"Place {slot + 1} vide ou inexistante.");
            }
        }
    }
}
