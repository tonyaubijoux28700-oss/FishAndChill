using System.Collections.Generic;
using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.Gear
{
    /// <summary>
    /// Un appât (ScriptableObject) : vendu par paquet de quelques utilisations (ou illimité), il attire surtout ses
    /// espèces favorites, et selon sa nature les gros poissons rares, les espèces de nuit ou seulement les chasseurs.
    /// Une utilisation part à chaque lancer.
    /// </summary>
    [CreateAssetMenu(menuName = "FishAndChill/Appât", fileName = "Appat")]
    public sealed class Bait : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField, TextArea] private string _description;
        [SerializeField, Min(0), Tooltip("Prix d'un paquet (0 = gratuit).")] private int _price;
        [SerializeField, Min(0), Tooltip("Utilisations par paquet (0 = illimité).")] private int _uses;

        [SerializeField, Tooltip("Espèces que cet appât attire particulièrement.")]
        private List<FishSpecies> _favorites = new List<FishSpecies>();
        [SerializeField, Min(1f)] private float _favoriteBonus = 3f;

        [SerializeField, Min(0f), Tooltip("Multiplicateur des espèces rares, épiques et légendaires.")] private float _rareBonus = 1f;
        [SerializeField, Min(0f), Tooltip("Multiplicateur des espèces de nuit, quand il fait nuit (appât lumineux).")] private float _nightBonus = 1f;
        [SerializeField, Tooltip("Leurre : les espèces qui ne sont pas ses favorites mordent deux fois moins.")] private bool _favoritesOnly;
        [SerializeField, Range(0.3f, 1.5f), Tooltip("Attente de la touche (fraction de l'attente normale).")] private float _biteWaitFactor = 1f;

        public string Id => name;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;
        public int Price => _price;
        public int Uses => _uses;
        public bool Unlimited => _uses <= 0;
        public float BiteWaitFactor => _biteWaitFactor;
        public IReadOnlyList<FishSpecies> Favorites => _favorites;

        public bool IsFavorite(FishSpecies species) => species != null && _favorites.Contains(species);

        /// <summary>Multiplicateur de fréquence de cette espèce avec cet appât (de nuit ou de jour).</summary>
        public float Multiplier(FishSpecies species, bool isNight)
        {
            if (species == null)
            {
                return 1f;
            }

            float multiplier = IsFavorite(species) ? _favoriteBonus : _favoritesOnly ? 0.5f : 1f;
            if (species.Rarity >= Rarity.Rare)
            {
                multiplier *= _rareBonus;
            }

            if (isNight && species.ActiveTime == ActiveTime.Night)
            {
                multiplier *= _nightBonus;
            }

            return multiplier;
        }

        /// <summary>Réglage complet (génération des données et tests).</summary>
        public void Configure(string displayName, string description, int price, int uses, IEnumerable<FishSpecies> favorites,
            float rareBonus = 1f, float nightBonus = 1f, bool favoritesOnly = false, float biteWaitFactor = 1f, Sprite icon = null)
        {
            _displayName = displayName;
            _description = description;
            _price = price;
            _uses = uses;
            _favorites = new List<FishSpecies>(favorites ?? new FishSpecies[0]);
            _rareBonus = rareBonus;
            _nightBonus = nightBonus;
            _favoritesOnly = favoritesOnly;
            _biteWaitFactor = biteWaitFactor;
            _icon = icon;
        }
    }
}
