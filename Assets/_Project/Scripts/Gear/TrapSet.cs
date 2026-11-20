using System;
using System.Collections.Generic;
using FishAndChill.Collection;

namespace FishAndChill.Gear
{
    /// <summary>
    /// Les casiers à crustacés du joueur (logique pure, testée) : combien il en possède en tout, et ceux qui sont
    /// posés dans l'eau. Un casier posé reste à lui ; le relever le rend de nouveau disponible.
    /// </summary>
    public sealed class TrapSet
    {
        private readonly List<PlacedTrap> _placed = new List<PlacedTrap>();

        public event Action Changed;

        /// <summary>Casiers possédés en tout (posés compris).</summary>
        public int Owned { get; private set; }

        /// <summary>Casiers dans le sac, prêts à être posés.</summary>
        public int Available => Owned - _placed.Count;

        public IReadOnlyList<PlacedTrap> Placed => _placed;

        public TrapPurchase TryBuy(int price, int maxOwned, Wallet wallet)
        {
            if (wallet == null || price < 0) return TrapPurchase.Invalid;
            if (Owned >= maxOwned) return TrapPurchase.AtMax;
            if (!wallet.TrySpend(price)) return TrapPurchase.NotEnoughCoins;
            Owned++;
            Changed?.Invoke();
            return TrapPurchase.Bought;
        }

        /// <summary>Pose un casier ; faux s'il n'en reste aucun dans le sac.</summary>
        public bool Place(string zoneId, float x, float y, double nowHours)
        {
            if (Available <= 0 || string.IsNullOrEmpty(zoneId)) return false;
            _placed.Add(new PlacedTrap { ZoneId = zoneId, X = x, Y = y, PlacedAtHours = nowHours });
            Changed?.Invoke();
            return true;
        }

        /// <summary>Relève le casier n° <paramref name="index"/> : il revient dans le sac. Null si l'indice est faux.</summary>
        public PlacedTrap Lift(int index)
        {
            if (index < 0 || index >= _placed.Count) return null;
            PlacedTrap trap = _placed[index];
            _placed.RemoveAt(index);
            Changed?.Invoke();
            return trap;
        }

        /// <summary>Le casier posé le plus proche de (x, y) à moins de <paramref name="radius"/>, ou -1.</summary>
        public int NearestPlaced(float x, float y, float radius)
        {
            int best = -1;
            float bestDistance = radius * radius;
            for (int i = 0; i < _placed.Count; i++)
            {
                float dx = _placed[i].X - x;
                float dy = _placed[i].Y - y;
                float d = dx * dx + dy * dy;
                if (d <= bestDistance)
                {
                    bestDistance = d;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>Recharge depuis la sauvegarde (jamais moins de casiers possédés que de casiers posés).</summary>
        public void Load(int owned, IEnumerable<PlacedTrap> placed)
        {
            _placed.Clear();
            if (placed != null)
            {
                foreach (PlacedTrap trap in placed)
                {
                    if (trap != null && !string.IsNullOrEmpty(trap.ZoneId)) _placed.Add(trap);
                }
            }

            Owned = Math.Max(Math.Max(0, owned), _placed.Count);
            Changed?.Invoke();
        }
    }
}
