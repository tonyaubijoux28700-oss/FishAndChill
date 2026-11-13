using FishAndChill.Player;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// La boutique : un bâtiment infranchissable dans lequel on entre en marchant vers sa porte.
    /// L'intérieur est une pièce placée hors de l'île (<see cref="InteriorOrigin"/>) ; on en sort en
    /// redescendant vers le paillasson. La caméra se cale sur la pièce tant qu'on est dedans.
    /// Le pivot du bâtiment est au sol, au milieu de la façade (la porte).
    /// </summary>
    public sealed class ShopBuilding : MonoBehaviour
    {
        public const float InteriorWidth = 10f;
        public const float InteriorHeight = 7f;

        [SerializeField] private PlayerMovement _player;
        [SerializeField] private WorldMap _map;
        [SerializeField] private CameraFollow _camera;

        [SerializeField, Tooltip("Coin bas-gauche de la pièce intérieure, en coordonnées du monde (hors de l'île).")]
        private Vector2 _interiorOrigin = new Vector2(200f, 0f);

        [SerializeField, Tooltip("Emprise extérieure (largeur, profondeur) à partir de la façade.")]
        private Vector2 _footprint = new Vector2(5f, 2.4f);

        [SerializeField] private Color _interiorBackground = new Color(0.08f, 0.06f, 0.05f);

        private Color _outsideBackground;

        public bool IsPlayerInside { get; private set; }
        public Vector2 InteriorOrigin => _interiorOrigin;
        public Rect InteriorRect => new Rect(_interiorOrigin, new Vector2(InteriorWidth, InteriorHeight));

        /// <summary>Devant le comptoir, là où l'on parle au marchand.</summary>
        public Vector2 CounterFront => _interiorOrigin + new Vector2(InteriorWidth * 0.5f, 2.55f);

        /// <summary>Emplacement (pieds) du marchand, derrière le comptoir.</summary>
        public Vector2 MerchantSpot => _interiorOrigin + new Vector2(InteriorWidth * 0.5f, 4.05f);

        /// <summary>Pied du comptoir (pivot de son sprite).</summary>
        public Vector2 CounterSpot => _interiorOrigin + new Vector2(InteriorWidth * 0.5f, 3.1f);

        private Vector2 Door => transform.position;
        private Vector2 OutsideSpot => Door + new Vector2(0f, -0.9f);
        private Vector2 InsideSpot => _interiorOrigin + new Vector2(InteriorWidth * 0.5f, 0.9f);

        private void Start()
        {
            if (_map == null)
            {
                return;
            }

            // Le bâtiment vu de dehors.
            _map.AddObstacle(new Rect(Door.x - _footprint.x * 0.5f, Door.y, _footprint.x, _footprint.y));

            // Le sol de la boutique (sous le mur du fond) et le comptoir.
            _map.AddWalkableArea(new Rect(_interiorOrigin.x + 0.6f, _interiorOrigin.y + 0.15f, InteriorWidth - 1.2f, 4.35f));
            _map.AddObstacle(new Rect(CounterSpot.x - 2.5f, CounterSpot.y - 0.1f, 5f, 0.9f));
        }

        private void Update()
        {
            if (_player == null || _player.Locked)
            {
                return;
            }

            Vector2 feet = _player.transform.position;
            // Direction demandée, même si le mur empêche d'avancer (on « pousse » la porte).
            Vector2 input = _player.LastInput;

            if (!IsPlayerInside)
            {
                bool atDoor = Mathf.Abs(feet.x - Door.x) < 0.7f && feet.y < Door.y && feet.y > Door.y - 0.75f;
                if (atDoor && input.y > 0.5f)
                {
                    Enter();
                }
            }
            else
            {
                bool onDoormat = Mathf.Abs(feet.x - InsideSpot.x) < 0.9f && feet.y < _interiorOrigin.y + 0.6f;
                if (onDoormat && input.y < -0.5f)
                {
                    Exit();
                }
            }
        }

        /// <summary>Entre dans la boutique. Public pour pouvoir le déclencher hors clavier (tests).</summary>
        public void Enter()
        {
            IsPlayerInside = true;
            _player.Teleport(InsideSpot, Vector2.up);
            if (_camera != null)
            {
                _camera.SetBoundsOverride(InteriorRect);
                _camera.SnapToTarget();
                Camera cam = _camera.GetComponent<Camera>();
                _outsideBackground = cam.backgroundColor;
                cam.backgroundColor = _interiorBackground;
            }
        }

        /// <summary>Ressort devant la porte.</summary>
        public void Exit()
        {
            IsPlayerInside = false;
            _player.Teleport(OutsideSpot, Vector2.down);
            if (_camera != null)
            {
                _camera.SetBoundsOverride(null);
                _camera.SnapToTarget();
                _camera.GetComponent<Camera>().backgroundColor = _outsideBackground;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Door + new Vector2(0f, _footprint.y * 0.5f), _footprint);
            Gizmos.DrawWireCube(InteriorRect.center, InteriorRect.size);
        }
    }
}
