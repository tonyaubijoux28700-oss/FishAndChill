using FishAndChill.Fishing;
using UnityEngine;

namespace FishAndChill.Collection
{
    /// <summary>Fiche d'une espèce de poisson. Une espèce = un asset, jamais du code.</summary>
    [CreateAssetMenu(menuName = "FishAndChill/Espèce de poisson", fileName = "NewFishSpecies")]
    public sealed class FishSpecies : ScriptableObject
    {
        [SerializeField, Tooltip("Identifiant unique et stable (ex. : perca-fluviatilis). Ne pas changer une fois publié.")]
        private string _id;

        [SerializeField] private string _commonName;
        [SerializeField] private string _scientificName;
        [SerializeField] private Rarity _rarity;
        [SerializeField] private QteDifficulty _difficulty = new QteDifficulty();

        [SerializeField, Tooltip("Taille minimale et maximale, en centimètres.")]
        private Vector2 _sizeRangeCm = new Vector2(10f, 30f);

        [SerializeField, Min(0)] private int _basePrice = 10;

        [SerializeField, Tooltip("Couleur provisoire tant qu'il n'y a pas de sprite.")]
        private Color _placeholderColor = Color.gray;

        [SerializeField] private Sprite _sprite;

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;
        public string CommonName => string.IsNullOrEmpty(_commonName) ? name : _commonName;
        public string ScientificName => _scientificName;
        public Rarity Rarity => _rarity;
        public QteDifficulty Difficulty => _difficulty;
        public Vector2 SizeRangeCm => _sizeRangeCm;
        public int BasePrice => _basePrice;
        public Color PlaceholderColor => _placeholderColor;
        public Sprite Sprite => _sprite;
    }
}
