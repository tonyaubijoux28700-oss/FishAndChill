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

        [SerializeField, Tooltip("Position sur la carte du monde (0..1, 0 = bas gauche).")]
        private Vector2 _mapPosition = new Vector2(0.5f, 0.5f);

        [SerializeField, Tooltip("Pays ou région réelle, affiché sur la carte du monde.")]
        private string _region;

        [SerializeField, Tooltip("Climat : fréquence relative de chaque temps.")]
        private WeatherClimate _climate = new WeatherClimate();

        [SerializeField, Tooltip("Lumière de la zone, multipliée à celle du ciel : blanc = normale, gris = zone sombre et inquiétante.")]
        private Color _ambient = Color.white;

        [SerializeField, Range(0f, 1f), Tooltip("Brume toujours présente, même par beau temps (0 = aucune).")]
        private float _minimumFog;

        /// <summary>Lumière de la zone (blanc = normale), multipliée à la teinte du ciel.</summary>
        public Color Ambient => _ambient;

        /// <summary>Brume minimale de la zone, quel que soit le temps.</summary>
        public float MinimumFog => _minimumFog;

        /// <summary>À quel point la zone est sombre (0 = normale, 1 = noire) : les lanternes s'y allument plus tôt.</summary>
        public float Gloom => Mathf.Clamp01(1f - _ambient.grayscale);

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public Vector2 MapPosition => _mapPosition;
        public string Region => _region;

        /// <summary>Le climat de la zone : la fréquence de chaque temps (beau, nuageux, pluie, orage, brouillard).</summary>
        public WeatherClimate Climate => _climate;

        /// <summary>Identifiant stable de la zone (nom de l'asset), pour la sauvegarde.</summary>
        public string Id => name;
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
