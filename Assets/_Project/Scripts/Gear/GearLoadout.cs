using System;
using System.Collections.Generic;

namespace FishAndChill.Gear
{
    /// <summary>
    /// Le matériel du joueur. Pour le sac, le « niveau » est la taille atteinte (on achète le suivant). Pour la canne et
    /// l'hameçon, c'est l'objet <b>équipé</b> parmi ceux qu'on possède (le 0, celui du départ, est toujours à soi).
    /// </summary>
    public sealed class GearLoadout
    {
        private readonly Dictionary<GearCategory, int> _levels = new Dictionary<GearCategory, int>();
        private readonly HashSet<(GearCategory, int)> _owned = new HashSet<(GearCategory, int)>();

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

        /// <summary>Possède-t-on cet objet ? (le 0 de chaque famille, toujours ; et tout ce qui est au niveau atteint ou en dessous)</summary>
        public bool Owns(GearCategory category, int index)
        {
            return index == 0 || index <= Level(category) && category == GearCategory.Bag || _owned.Contains((category, index));
        }

        /// <summary>Ajoute un objet à ce qu'on possède.</summary>
        public void Own(GearCategory category, int index)
        {
            if (index > 0 && _owned.Add((category, index)))
            {
                Changed?.Invoke();
            }
        }

        /// <summary>Équipe un objet qu'on possède.</summary>
        public bool Equip(GearCategory category, int index)
        {
            if (!Owns(category, index) || Level(category) == index)
            {
                return false;
            }

            _levels[category] = index;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Équipe l'objet suivant (ou précédent) parmi ceux qu'on possède dans cette famille.</summary>
        public void Cycle(GearCategory category, int count, int direction = 1)
        {
            int current = Level(category);
            for (int step = 1; step < count; step++)
            {
                int index = ((current + step * (direction < 0 ? -1 : 1)) % count + count) % count;
                if (Owns(category, index))
                {
                    Equip(category, index);
                    return;
                }
            }
        }

        /// <summary>Objets possédés (hors ceux du départ), pour la sauvegarde.</summary>
        public IEnumerable<(GearCategory category, int index)> OwnedItems => _owned;

        /// <summary>Remet un niveau tel qu'il était dans une sauvegarde.</summary>
        public void SetLevel(GearCategory category, int level)
        {
            _levels[category] = Math.Max(0, level);
            Changed?.Invoke();
        }
    }
}
