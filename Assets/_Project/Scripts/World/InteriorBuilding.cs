using FishAndChill.Player;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Bâtiment dans lequel on entre (boutique, aquarium) : infranchissable de dehors, on entre en marchant vers sa
    /// porte. L'intérieur est une pièce placée hors de l'île (<see cref="InteriorOrigin"/>) ; on en sort en
    /// redescendant vers le paillasson. La caméra se cale sur la pièce tant qu'on est dedans.
    /// Le pivot du bâtiment est au sol, au milieu de la façade (la porte).
    /// </summary>
    public abstract class InteriorBuilding : MonoBehaviour
    {
        [SerializeField] private PlayerMovement _player;
        [SerializeField] private WorldMap _map;
        [SerializeField] private CameraFollow _camera;

        [SerializeField, Tooltip("Coin bas-gauche de la pièce intérieure, en coordonnées du monde (hors de l'île).")]
        private Vector2 _interiorOrigin = new Vector2(200f, 0f);

        [SerializeField, Tooltip("Emprise extérieure (largeur, profondeur) à partir de la façade.")]
        private Vector2 _footprint = new Vector2(5f, 2.4f);

        [SerializeField, Tooltip("Décalage de l'emprise vers la droite par rapport à la porte (bâtiment à aile, comme le laboratoire).")]
        private float _footprintShift;

        [SerializeField] private Color _interiorBackground = new Color(0.08f, 0.06f, 0.05f);

        private Color _outsideBackground;

        /// <summary>Le bâtiment où se trouve le joueur (null dehors). Pas de pêche à l'intérieur.</summary>
        public static InteriorBuilding Occupied { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Occupied = null;
        }

        protected PlayerMovement Player => _player;

        public bool IsPlayerInside { get; private set; }
        public Vector2 InteriorOrigin => _interiorOrigin;

        /// <summary>Taille de la pièce intérieure, en cases.</summary>
        public abstract Vector2 InteriorSize { get; }

        public Rect InteriorRect => new Rect(_interiorOrigin, InteriorSize);

        /// <summary>Ce que la caméra peut montrer à l'intérieur (toute la pièce ; l'aquarium : l'étage où l'on est).</summary>
        protected virtual Rect CameraRect => InteriorRect;

        protected CameraFollow FollowCamera => _camera;

        /// <summary>Sol où l'on peut marcher, relatif au coin bas-gauche de la pièce (sous le mur du fond).</summary>
        protected abstract Rect FloorArea { get; }

        protected Vector2 Door => transform.position;
        private Vector2 OutsideSpot => Door + new Vector2(0f, -0.9f);
        /// <summary>Position de la porte dans la pièce (par défaut au milieu du mur du bas).</summary>
        protected virtual float EntranceX => InteriorSize.x * 0.5f;

        private Vector2 InsideSpot => _interiorOrigin + new Vector2(EntranceX, 0.9f);

        protected virtual void Start()
        {
            if (_map == null)
            {
                return;
            }

            // Le bâtiment vu de dehors.
            _map.AddObstacle(new Rect(Door.x + _footprintShift - _footprint.x * 0.5f, Door.y, _footprint.x, _footprint.y));

            Rect floor = FloorArea;
            _map.AddWalkableArea(new Rect(_interiorOrigin + floor.position, floor.size));
            SetupInterior(_map);
        }

        /// <summary>Obstacles propres à la pièce (comptoir…).</summary>
        protected virtual void SetupInterior(WorldMap map)
        {
        }

        protected virtual void Update()
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

        /// <summary>Entre dans le bâtiment. Public pour pouvoir le déclencher hors clavier (tests).</summary>
        public virtual void Enter()
        {
            IsPlayerInside = true;
            Occupied = this;
            _player.Teleport(InsideSpot, Vector2.up);
            if (_camera != null)
            {
                _camera.SetBoundsOverride(CameraRect);
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
            if (Occupied == this)
            {
                Occupied = null;
            }

            _player.Teleport(OutsideSpot, Vector2.down);
            if (_camera != null)
            {
                _camera.SetBoundsOverride(null);
                _camera.SnapToTarget();
                _camera.GetComponent<Camera>().backgroundColor = _outsideBackground;
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Door + new Vector2(_footprintShift, _footprint.y * 0.5f), _footprint);
            Gizmos.DrawWireCube(InteriorRect.center, InteriorRect.size);
        }
    }
}
