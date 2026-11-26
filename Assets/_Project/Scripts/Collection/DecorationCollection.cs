using System;
using System.Collections.Generic;

namespace FishAndChill.Collection
{
    /// <summary>
    /// Les décorations du joueur (logique pure, testée) : combien il en possède de chaque sorte, lesquelles sont posées
    /// dans les bassins (un emplacement par case de chaque bassin), et s'il a le masque pour plonger. Une décoration posée
    /// reste à lui : on la reprend pour la poser ailleurs.
    /// </summary>
    public sealed class DecorationCollection
    {
        private readonly Dictionary<string, int> _owned = new Dictionary<string, int>();
        private readonly Dictionary<string, string> _placed = new Dictionary<string, string>();   // "bassin:case" -> décoration

        public event Action Changed;

        /// <summary>Masque et tuba achetés : on peut plonger.</summary>
        public bool HasMask { get; private set; }

        public IEnumerable<string> OwnedIds => _owned.Keys;

        public static string SlotKey(string tank, int slot) => $"{tank}:{slot}";

        public int Owned(string id) => id != null && _owned.TryGetValue(id, out int n) ? n : 0;

        /// <summary>Combien ne sont posées nulle part (on peut encore les poser).</summary>
        public int Available(string id)
        {
            int placed = 0;
            foreach (string v in _placed.Values) if (v == id) placed++;
            return Owned(id) - placed;
        }

        public void Add(string id, int count = 1)
        {
            if (string.IsNullOrEmpty(id) || count <= 0) return;
            _owned[id] = Owned(id) + count;
            Changed?.Invoke();
        }

        public string PlacedAt(string tank, int slot) => _placed.TryGetValue(SlotKey(tank, slot), out string id) ? id : null;

        /// <summary>Pose une décoration (null : vide la case). Faux si on n'en a plus de libre.</summary>
        public bool Place(string tank, int slot, string id)
        {
            string key = SlotKey(tank, slot);
            string current = PlacedAt(tank, slot);
            if (current == id) return true;
            if (id != null && Available(id) <= 0) return false;
            if (id == null) _placed.Remove(key);
            else _placed[key] = id;
            Changed?.Invoke();
            return true;
        }

        public bool BuyMask()
        {
            if (HasMask) return false;
            HasMask = true;
            Changed?.Invoke();
            return true;
        }

        public IEnumerable<KeyValuePair<string, string>> Placements => _placed;

        /// <summary>Restaure depuis la sauvegarde (les poses de décorations qu'on n'a pas sont ignorées).</summary>
        public void Load(IEnumerable<KeyValuePair<string, int>> owned, IEnumerable<KeyValuePair<string, string>> placed, bool hasMask)
        {
            _owned.Clear();
            _placed.Clear();
            if (owned != null) foreach (var kv in owned) if (!string.IsNullOrEmpty(kv.Key) && kv.Value > 0) _owned[kv.Key] = kv.Value;
            if (placed != null)
            {
                foreach (var kv in placed)
                {
                    if (string.IsNullOrEmpty(kv.Key) || string.IsNullOrEmpty(kv.Value) || Available(kv.Value) <= 0) continue;
                    _placed[kv.Key] = kv.Value;
                }
            }

            HasMask = hasMask;
            Changed?.Invoke();
        }
    }
}
