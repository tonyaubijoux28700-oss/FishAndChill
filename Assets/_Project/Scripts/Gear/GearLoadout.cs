using System;
using System.Collections.Generic;

namespace FishAndChill.Gear
{
    /// <summary>Le matériel du joueur : le niveau atteint dans chaque famille (0 au départ).</summary>
    public sealed class GearLoadout
    {
        private readonly Dictionary<GearCategory, int> _levels = new Dictionary<GearCategory, int>();

        public event Action Changed;

        public int Level(GearCategory category)
        {
            return _levels.TryGetValue(category, out int level) ? level : 0;
        }

        /// <summary>Passe au niveau suivant dans cette famille.</summary>
        public void Upgrade(GearCategory category)
        {
            _levels[category] = Level(category) + 1;
            Changed?.Invoke();
        }

        /// <summary>Remet un niveau tel qu'il était dans une sauvegarde.</summary>
        public void SetLevel(GearCategory category, int level)
        {
            _levels[category] = Math.Max(0, level);
            Changed?.Invoke();
        }
    }
}
