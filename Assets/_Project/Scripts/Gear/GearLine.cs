using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.Gear
{
    /// <summary>Une famille de matériel et ses niveaux successifs (données de jeu, modifiables dans l'éditeur).</summary>
    [CreateAssetMenu(menuName = "FishAndChill/Matériel", fileName = "Materiel")]
    public sealed class GearLine : ScriptableObject
    {
        [SerializeField] private GearCategory _category;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;

        [SerializeField, Tooltip("Niveau 0 = matériel de départ (gratuit), puis les améliorations dans l'ordre.")]
        private List<GearLevel> _levels = new List<GearLevel>();

        public GearCategory Category => _category;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public Sprite Icon => _icon;
        public IReadOnlyList<GearLevel> Levels => _levels;

        /// <summary>Niveau le plus haut qu'on peut atteindre.</summary>
        public int MaxLevel => Mathf.Max(0, _levels.Count - 1);

        /// <summary>Le niveau demandé, borné aux niveaux existants (null si la famille est vide).</summary>
        public GearLevel At(int level)
        {
            if (_levels.Count == 0)
            {
                return null;
            }

            return _levels[Mathf.Clamp(level, 0, _levels.Count - 1)];
        }

        /// <summary>Pour les tests et la génération des données.</summary>
        public void Configure(GearCategory category, string displayName, IEnumerable<GearLevel> levels, Sprite icon = null)
        {
            _category = category;
            _displayName = displayName;
            _icon = icon;
            _levels = new List<GearLevel>(levels);
        }
    }
}
