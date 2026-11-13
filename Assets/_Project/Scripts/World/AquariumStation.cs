using System;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Player;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Le bâtiment de l'aquarium : infranchissable ; devant sa porte, la touche E dépose tout le sac
    /// (nouvelles espèces dans l'aquarium, doublons vendus).
    /// Le pivot du bâtiment est au milieu de sa façade, au sol.
    /// </summary>
    public sealed class AquariumStation : MonoBehaviour
    {
        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private Transform _player;
        [SerializeField] private WorldMap _map;

        [SerializeField, Tooltip("Optionnel : pas de dépôt pendant une partie de pêche.")]
        private FishingController _fishing;

        [SerializeField, Tooltip("Emprise au sol du bâtiment (largeur, profondeur), à partir de la façade.")]
        private Vector2 _footprint = new Vector2(3f, 1.7f);

        [SerializeField, Min(0.5f), Tooltip("Distance à la porte pour pouvoir déposer.")]
        private float _interactRadius = 1.4f;

        public event Action<DepositResult> Deposited;

        /// <summary>Le joueur est devant la porte et peut déposer.</summary>
        public bool PlayerInRange { get; private set; }

        public Vector2 DoorPosition => (Vector2)transform.position + Vector2.down * 0.35f;

        private void Start()
        {
            if (_map != null)
            {
                Vector2 basePosition = transform.position;
                _map.AddObstacle(new Rect(basePosition.x - _footprint.x * 0.5f, basePosition.y, _footprint.x, _footprint.y));
            }
        }

        private void Update()
        {
            PlayerInRange = _player != null && Vector2.Distance(_player.position, DoorPosition) <= _interactRadius && FishingAllowsIt();
            if (PlayerInRange && InteractInput.PressedThisFrame())
            {
                Deposit();
            }
        }

        /// <summary>Dépose tout le sac. Public pour pouvoir le déclencher hors clavier (tests).</summary>
        public DepositResult Deposit()
        {
            if (_progress == null)
            {
                return new DepositResult();
            }

            _progress.EnsureCreated();
            DepositResult result = _progress.DepositBag();
            Deposited?.Invoke(result);
            return result;
        }

        private bool FishingAllowsIt()
        {
            return _fishing == null || _fishing.State == FishingState.Idle || _fishing.State == FishingState.Result;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector2 basePosition = transform.position;
            Gizmos.DrawWireCube(basePosition + new Vector2(0f, _footprint.y * 0.5f), _footprint);
            Gizmos.DrawWireSphere(DoorPosition, _interactRadius);
        }
    }
}
