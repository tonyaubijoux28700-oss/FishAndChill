using FishAndChill.World;
using UnityEngine;

namespace FishAndChill.Collection
{
    /// <summary>
    /// Une décoration d'aquarium qu'on ramasse en plongée (corail, coquillage, rocher, épave…) : sa zone, sa rareté (les
    /// plus rares sont loin de la corde de la bouée) et son dessin, le même sur le fond marin et dans le bassin.
    /// </summary>
    [CreateAssetMenu(menuName = "FishAndChill/Décoration", fileName = "Decoration")]
    public sealed class Decoration : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private FishingZone _zone;
        [SerializeField] private Rarity _rarity;
        [SerializeField, Tooltip("Pivot au pied, 16 px par case.")] private Sprite _sprite;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public FishingZone Zone => _zone;
        public Rarity Rarity => _rarity;
        public Sprite Sprite => _sprite;

        public void Configure(string id, string displayName, string description, FishingZone zone, Rarity rarity, Sprite sprite)
        {
            _id = id;
            _displayName = displayName;
            _description = description;
            _zone = zone;
            _rarity = rarity;
            _sprite = sprite;
        }
    }
}
