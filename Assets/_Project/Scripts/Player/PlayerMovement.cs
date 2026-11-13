using FishAndChill.Fishing;
using FishAndChill.World;
using UnityEngine;

namespace FishAndChill.Player
{
    /// <summary>
    /// Déplacement du pêcheur (vue de dessus). Avec une carte, il ne marche que sur la terre ;
    /// sans carte, il reste dans un rectangle. Bloqué tant que la ligne est à l'eau.
    /// </summary>
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Vitesse en unités (cases) par seconde.")]
        private float _speed = 5f;

        [SerializeField, Tooltip("Carte du monde : le joueur ne marche que sur la terre.")]
        private WorldMap _map;

        [SerializeField, Min(0.05f), Tooltip("Demi-largeur du personnage pour les collisions.")]
        private float _radius = 0.25f;

        [SerializeField, Tooltip("Zone de marche utilisée seulement s'il n'y a pas de carte.")]
        private Rect _walkableArea = Rect.MinMaxRect(-8f, -4.6f, 8f, -1.9f);

        [SerializeField, Tooltip("Optionnel : bloque le déplacement quand la ligne est à l'eau.")]
        private FishingController _fishing;

        public Rect WalkableArea => _walkableArea;

        /// <summary>Direction dans laquelle regarde le pêcheur (le lancer part par là).</summary>
        public Vector2 Facing { get; private set; } = Vector2.down;

        /// <summary>Le joueur s'est-il déplacé à la dernière image ?</summary>
        public bool IsMoving { get; private set; }

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
            else
            {
                IsMoving = false;
            }
        }

        /// <summary>Déplace le joueur. Public pour pouvoir le piloter hors clavier (tests).</summary>
        public void Move(Vector2 input, float deltaTime)
        {
            Facing = MovementMath.UpdateFacing(Facing, input);

            Vector2 position = transform.position;
            Vector2 next = _map != null
                ? MovementMath.StepWithCollision(position, input, _speed, deltaTime, p => _map.CanStandAt(p, _radius))
                : MovementMath.Step(position, input, _speed, deltaTime, _walkableArea);

            IsMoving = (next - position).sqrMagnitude > 1e-8f;
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            if (_map == null)
            {
                Gizmos.DrawWireCube(_walkableArea.center, _walkableArea.size);
            }

            Gizmos.DrawWireCube(transform.position, Vector3.one * _radius * 2f);
        }
    }
}
