using System;
using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Common;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>Lieu de pêche (inspiré d'un lieu réel) avec ses espèces et leurs fréquences.</summary>
    [CreateAssetMenu(menuName = "FishAndChill/Zone de pêche", fileName = "NewFishingZone")]
    public sealed class FishingZone : ScriptableObject
    {
        [Serializable]
        public sealed class SpeciesEntry
        {
            [SerializeField] private FishSpecies _species;

            [SerializeField, Min(0f), Tooltip("Fréquence relative : 40 sort deux fois plus souvent que 20.")]
            private float _weight = 10f;

            public FishSpecies Species => _species;
            public float Weight => _weight;
        }

        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Color _waterColor = new Color(0.16f, 0.42f, 0.55f);
        [SerializeField] private List<SpeciesEntry> _species = new List<SpeciesEntry>();

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public string Description => _description;
        public Color WaterColor => _waterColor;
        public IReadOnlyList<SpeciesEntry> Species => _species;

        /// <summary>Tire au sort l'espèce qui va mordre, selon les fréquences. Null si la zone est vide.</summary>
        public FishSpecies PickSpecies(System.Random random) => PickSpecies(random, null);

        /// <summary>
        /// Tirage avec un multiplicateur de fréquence par espèce (l'appât rend les espèces rares plus fréquentes).
        /// </summary>
        public FishSpecies PickSpecies(System.Random random, System.Func<FishSpecies, float> weightMultiplier)
        {
            var weights = new float[_species.Count];
            for (int i = 0; i < _species.Count; i++)
            {
                FishSpecies species = _species[i].Species;
                float multiplier = species != null && weightMultiplier != null ? weightMultiplier(species) : 1f;
                weights[i] = species != null ? _species[i].Weight * multiplier : 0f;
            }

            int index = WeightedPicker.Pick(weights, random.NextDouble());
            return index >= 0 ? _species[index].Species : null;
        }
    }
}
