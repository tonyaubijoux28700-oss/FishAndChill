using System;

namespace FishAndChill.Collection
{
    /// <summary>Les pièces du joueur (gagnées en vendant les doublons).</summary>
    public sealed class Wallet
    {
        public event Action Changed;

        public int Coins { get; private set; }

        public void Add(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (amount == 0)
            {
                return;
            }

            Coins += amount;
            Changed?.Invoke();
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || amount > Coins)
            {
                return false;
            }

            Coins -= amount;
            Changed?.Invoke();
            return true;
        }
    }
}
