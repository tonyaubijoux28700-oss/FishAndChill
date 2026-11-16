using System;
using System.Collections.Generic;

namespace FishAndChill.Gear
{
    /// <summary>
    /// Les appâts du joueur (logique pure, testée) : combien il en reste de chaque sorte (illimité possible), et celui qui
    /// est accroché à la ligne. Un lancer en consomme un ; le dernier utilisé, on revient à l'appât de départ.
    /// </summary>
    public sealed class BaitBag
    {
        public const int Unlimited = -1;

        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
        private readonly List<string> _order = new List<string>();

        public event Action Changed;

        /// <summary>Appât accroché à la ligne (son identifiant).</summary>
        public string Equipped { get; private set; }

        public IReadOnlyList<string> Owned => _order;

        public bool Owns(string id) => id != null && _counts.ContainsKey(id);

        /// <summary>Nombre restant (<see cref="Unlimited"/> = illimité, 0 = aucun).</summary>
        public int Count(string id) => id != null && _counts.TryGetValue(id, out int count) ? count : 0;

        /// <summary>Ajoute un paquet (<paramref name="uses"/> ≤ 0 : illimité).</summary>
        public void Add(string id, int uses)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            if (!_counts.ContainsKey(id))
            {
                _counts[id] = 0;
                _order.Add(id);
            }

            if (uses <= 0 || _counts[id] == Unlimited)
            {
                _counts[id] = Unlimited;
            }
            else
            {
                _counts[id] += uses;
            }

            if (Equipped == null)
            {
                Equipped = id;
            }

            Changed?.Invoke();
        }

        /// <summary>Accroche cet appât (s'il en reste).</summary>
        public bool Equip(string id)
        {
            if (!Owns(id) || Equipped == id)
            {
                return false;
            }

            Equipped = id;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Passe à l'appât suivant (ou précédent) parmi ceux qu'on a.</summary>
        public void Cycle(int direction)
        {
            if (_order.Count < 2)
            {
                return;
            }

            int index = Math.Max(0, _order.IndexOf(Equipped));
            index = ((index + Math.Sign(direction == 0 ? 1 : direction)) % _order.Count + _order.Count) % _order.Count;
            Equip(_order[index]);
        }

        /// <summary>
        /// Un lancer : consomme une utilisation de l'appât accroché. S'il n'en reste plus, on revient à
        /// <paramref name="fallback"/> et la méthode renvoie vrai.
        /// </summary>
        public bool ConsumeEquipped(string fallback)
        {
            if (Equipped == null || Count(Equipped) == Unlimited)
            {
                return false;
            }

            int left = Count(Equipped) - 1;
            if (left > 0)
            {
                _counts[Equipped] = left;
                Changed?.Invoke();
                return false;
            }

            _counts.Remove(Equipped);
            _order.Remove(Equipped);
            Equipped = Owns(fallback) ? fallback : _order.Count > 0 ? _order[0] : null;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Remet un état sauvegardé (sans prévenir : chargement).</summary>
        public void Restore(IEnumerable<KeyValuePair<string, int>> stocks, string equipped)
        {
            _counts.Clear();
            _order.Clear();
            if (stocks != null)
            {
                foreach (KeyValuePair<string, int> stock in stocks)
                {
                    if (!string.IsNullOrEmpty(stock.Key) && (stock.Value > 0 || stock.Value == Unlimited) && !_counts.ContainsKey(stock.Key))
                    {
                        _counts[stock.Key] = stock.Value;
                        _order.Add(stock.Key);
                    }
                }
            }

            Equipped = Owns(equipped) ? equipped : _order.Count > 0 ? _order[0] : null;
        }
    }
}
