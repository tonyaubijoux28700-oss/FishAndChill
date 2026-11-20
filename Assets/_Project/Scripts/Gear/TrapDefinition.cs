using UnityEngine;

namespace FishAndChill.Gear
{
    /// <summary>Le casier à crustacés vendu par le marchand : prix, nombre maximal, attente et dessins.</summary>
    [CreateAssetMenu(menuName = "FishAndChill/Casier à crustacés", fileName = "Casier")]
    public sealed class TrapDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Casier à crustacés";
        [SerializeField, TextArea] private string _description = "Pose-le dans l'eau près de la rive et reviens plus tard.";
        [SerializeField, Min(0)] private int _price = 150;
        [SerializeField, Min(1)] private int _maxOwned = 3;
        [SerializeField, Min(1f), Tooltip("Heures de jeu pour que le casier soit plein (meilleures chances).")]
        private float _hoursToFull = 12f;
        [SerializeField, Min(1)] private int _maxCatch = 3;
        [SerializeField, Tooltip("Image de la boutique.")] private Sprite _icon;
        [SerializeField, Tooltip("La bouée qui flotte au-dessus du casier posé (images de l'animation).")]
        private Sprite[] _buoyFrames = new Sprite[0];

        public string DisplayName => _displayName;
        public string Description => _description;
        public int Price => _price;
        public int MaxOwned => _maxOwned;
        public float HoursToFull => _hoursToFull;
        public int MaxCatch => _maxCatch;
        public Sprite Icon => _icon;
        public Sprite[] BuoyFrames => _buoyFrames;

        /// <summary>Règle les dessins (génération des données, éditeur).</summary>
        public void Configure(Sprite icon, Sprite[] buoyFrames)
        {
            _icon = icon;
            _buoyFrames = buoyFrames ?? new Sprite[0];
        }
    }
}
