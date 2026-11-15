using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.Gear
{
    /// <summary>Tout le matériel vendu par le marchand, une famille par catégorie, dans l'ordre d'affichage.</summary>
    [CreateAssetMenu(menuName = "FishAndChill/Catalogue de matériel", fileName = "CatalogueMateriel")]
    public sealed class GearCatalog : ScriptableObject
    {
        [SerializeField] private List<GearLine> _lines = new List<GearLine>();

        public IReadOnlyList<GearLine> Lines => _lines;

        /// <summary>La famille de cette catégorie (null si absente du catalogue).</summary>
        public GearLine Line(GearCategory category)
        {
            foreach (GearLine line in _lines)
            {
                if (line != null && line.Category == category)
                {
                    return line;
                }
            }

            return null;
        }

        /// <summary>Pour les tests et la génération des données.</summary>
        public void Configure(IEnumerable<GearLine> lines)
        {
            _lines = new List<GearLine>(lines);
        }
    }
}
