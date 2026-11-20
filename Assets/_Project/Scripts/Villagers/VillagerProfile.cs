using FishAndChill.Player;
using UnityEngine;

namespace FishAndChill.Villagers
{
    /// <summary>
    /// Un habitant de l'île : nom, apparence (celle d'un pêcheur plus ses accessoires), emploi du temps, façon de se
    /// balader et répliques. Généré par <c>Editor/VillagerSetup</c> dans <c>Data/Habitants/</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "FishAndChill/Habitant", fileName = "Habitant")]
    public sealed class VillagerProfile : ScriptableObject
    {
        [SerializeField] private string _id = "habitant";
        [SerializeField] private string _displayName = "Habitant";
        [SerializeField] private CharacterLook _look = new CharacterLook();
        [SerializeField] private NpcExtras _extras = new NpcExtras();

        [Header("Emploi du temps")]
        [SerializeField, Range(0f, 24f), Tooltip("Heure à laquelle il sort de chez lui.")] private float _leaveHour = 8f;
        [SerializeField, Range(0f, 24f), Tooltip("Heure à laquelle il rentre.")] private float _homeHour = 19f;

        [Header("Balade")]
        [SerializeField, Min(0.5f)] private float _wanderRadius = 5f;
        [SerializeField, Min(0.2f)] private float _speed = 1.6f;
        [SerializeField, Min(0f)] private float _minPause = 2f;
        [SerializeField, Min(0f)] private float _maxPause = 6f;

        [Header("Commandes")]
        [SerializeField, Tooltip("Zones dont il demande les poissons (communs, peu communs, rares).")] private World.FishingZone[] _zones = new World.FishingZone[0];

        [SerializeField] private VillagerLines _lines = new VillagerLines();

        public string Id => _id;
        public string DisplayName => _displayName;
        public CharacterLook Look => _look;
        public NpcExtras Extras => _extras;
        public float LeaveHour => _leaveHour;
        public float HomeHour => _homeHour;
        public float WanderRadius => _wanderRadius;
        public float Speed => _speed;
        public float MinPause => _minPause;
        public float MaxPause => _maxPause;
        public VillagerLines Lines => _lines;
        public System.Collections.Generic.IReadOnlyList<World.FishingZone> Zones => _zones;
    }
}
