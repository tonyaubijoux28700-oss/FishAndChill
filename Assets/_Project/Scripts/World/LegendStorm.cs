using FishAndChill.Fishing;
using FishAndChill.Save;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// La tempête des boss : quand un boss est réveillé, la tempête monte à mesure que le joueur s'approche de son remous
    /// et atteint son comble pendant le combat ; elle retombe quand on s'éloigne, et disparaît avec le remous une fois le
    /// boss vaincu. Pour chaque boss (tous les <see cref="LegendSpot"/>). Donne l'intensité (0 à 1) que
    /// <see cref="WeatherSystem"/> et <see cref="WeatherEffects"/> transforment en ciel noir, déluge, vent et foudre.
    /// Règles dans <see cref="LegendStormMath"/>.
    /// </summary>
    public sealed class LegendStorm : MonoBehaviour
    {
        [SerializeField] private FishingController _fishing;
        [SerializeField] private Transform _player;

        private LegendSpot[] _spots = new LegendSpot[0];

        /// <summary>Intensité de la tempête, de 0 (rien) à 1 (au comble).</summary>
        public float Intensity { get; private set; }

        /// <summary>Le cœur de la tempête : le remous du boss (ou le joueur s'il n'y en a pas).</summary>
        public Vector2 Center { get; private set; }

        private void Start()
        {
            _spots = FindObjectsByType<LegendSpot>();
        }

        private void Update()
        {
            if (!SaveManager.InGame || _player == null)
            {
                Intensity = 0f;
                return;
            }

            LegendSpot active = null;
            foreach (LegendSpot spot in _spots)
            {
                if (spot != null && spot.Active) active = spot;
            }

            Vector2 player = _player.position;
            bool fighting = _fishing != null && _fishing.HookedLegend != null && _fishing.LegendFight != null;
            float distance = active != null ? Vector2.Distance(player, active.Center) : float.MaxValue;
            Center = active != null ? active.Center : player;
            float target = InteriorBuilding.Occupied != null ? 0f : LegendStormMath.Target(active != null, distance, fighting);
            Intensity = LegendStormMath.Approach(Intensity, target, Time.deltaTime);
        }
    }
}
