using System.Collections.Generic;
using FishAndChill.World;
using UnityEngine;

namespace FishAndChill.Collection
{
    /// <summary>Toutes les décorations d'aquarium du jeu.</summary>
    [CreateAssetMenu(menuName = "FishAndChill/Catalogue des décorations", fileName = "Decorations")]
    public sealed class DecorationCatalog : ScriptableObject
    {
        [SerializeField] private Decoration[] _decorations = new Decoration[0];

        public IReadOnlyList<Decoration> All => _decorations;

        public Decoration Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (Decoration d in _decorations)
            {
                if (d != null && d.Id == id) return d;
            }

            return null;
        }

        /// <summary>Les décorations qu'on trouve en plongeant dans une zone.</summary>
        public List<Decoration> OfZone(FishingZone zone)
        {
            var result = new List<Decoration>();
            foreach (Decoration d in _decorations)
            {
                if (d != null && d.Zone == zone) result.Add(d);
            }

            return result;
        }

        [SerializeField, Tooltip("Icône du masque et du tuba (boutique).")] private Sprite _maskIcon;

        public Sprite MaskIcon => _maskIcon;

        public void Configure(Decoration[] decorations, Sprite maskIcon)
        {
            _decorations = decorations;
            _maskIcon = maskIcon;
        }
    }
}
