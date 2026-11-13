using FishAndChill.Fishing;
using UnityEngine;

namespace FishAndChill.Player
{
    /// <summary>
    /// Déplacement du pêcheur sur la berge (vue de dessus).
    /// Bloqué tant que la ligne est à l'eau : on ne se promène pas pendant une touche ou un combat.
    /// </summary>
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Vitesse en unités par seconde.")]
        private float _speed = 4f;

        [SerializeField, Tooltip("Zone où le joueur peut marcher (coordonnées du monde).")]
        private Rect _walkableArea = Rect.MinMaxRect(-8f, -4.6f, 8f, -1.9f);

        [SerializeField, Tooltip("Optionnel : bloque le déplacement quand la ligne est à l'eau.")]
        private FishingController _fishing;

        public Rect WalkableArea => _walkableArea;

        public bool CanMove
        {
            get
            {
                if (_fishing == null)
                {
                    return true;
                }

                FishingState state = _fishing.State;
                return state == FishingState.Idle || state == FishingState.Result;
            }
        }

        private void Update()
        {
            if (CanMove)
            {
                Move(MoveInput.Read(), Time.deltaTime);
            }
        }

        /// <summary>Déplace le joueur. Public pour pouvoir le piloter hors clavier (tests).</summary>
        public void Move(Vector2 input, float deltaTime)
        {
            transform.position = MovementMath.Step(transform.position, input, _speed, deltaTime, _walkableArea);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_walkableArea.center, _walkableArea.size);
        }
    }
}
