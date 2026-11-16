using FishAndChill.Fishing;
using UnityEngine;

namespace FishAndChill.Collection
{
    /// <summary>
    /// Fiche d'une espèce pêchable : poisson, crustacé, reptile ou amphibien (le nom de la classe est resté
    /// « FishSpecies »). Une espèce = un asset, jamais du code.
    /// </summary>
    [CreateAssetMenu(menuName = "FishAndChill/Espèce de poisson", fileName = "NewFishSpecies")]
    public sealed class FishSpecies : ScriptableObject
    {
        [SerializeField, Tooltip("Identifiant unique et stable (ex. : perca-fluviatilis). Ne pas changer une fois publié.")]
        private string _id;

        [SerializeField] private string _commonName;
        [SerializeField] private string _scientificName;
        [SerializeField] private Rarity _rarity;

        [SerializeField, Tooltip("Poisson, crustacé, reptile ou amphibien : seuls les poissons nagent dans l'aquarium.")]
        private CreatureKind _kind = CreatureKind.Fish;

        [SerializeField, Tooltip("Silhouette sous l'eau pendant la pêche.")]
        private BodyShape _bodyShape = BodyShape.Fish;

        [SerializeField] private QteDifficulty _difficulty = new QteDifficulty();

        [SerializeField, Tooltip("Façon de se battre au QTE : calme, nerveux, fuyant, rushs, lourd (tension).")]
        private FightStyle _fightStyle;

        [SerializeField, Tooltip("Taille minimale et maximale, en centimètres.")]
        private Vector2 _sizeRangeCm = new Vector2(10f, 30f);

        [SerializeField, Min(0)] private int _basePrice = 10;

        [SerializeField, Tooltip("Couleur provisoire tant qu'il n'y a pas de sprite.")]
        private Color _placeholderColor = Color.gray;

        [SerializeField, TextArea, Tooltip("Une ou deux phrases vraies sur l'espèce, pour la fiche de l'index.")]
        private string _description;

        [SerializeField, Tooltip("Moment où l'espèce mord : à toute heure, le jour ou la nuit.")]
        private ActiveTime _activeTime;

        [SerializeField] private Sprite _sprite;

        [SerializeField, Tooltip("Petite version (≈ 32 px) qui nage dans l'aquarium.")]
        private Sprite _miniSprite;

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;
        public string CommonName => string.IsNullOrEmpty(_commonName) ? name : _commonName;
        public string ScientificName => _scientificName;
        public Rarity Rarity => _rarity;
        public CreatureKind Kind => _kind;
        public BodyShape BodyShape => _bodyShape;
        public QteDifficulty Difficulty => _difficulty;
        public FightStyle FightStyle => _fightStyle;
        public Vector2 SizeRangeCm => _sizeRangeCm;
        public int BasePrice => _basePrice;
        public Color PlaceholderColor => _placeholderColor;
        public Sprite Sprite => _sprite;
        public ActiveTime ActiveTime => _activeTime;
        public string Description => _description;

        /// <summary>Sprite de l'aquarium (le grand dessin s'il n'y a pas de petite version).</summary>
        public Sprite MiniSprite => _miniSprite != null ? _miniSprite : _sprite;
    }
}
