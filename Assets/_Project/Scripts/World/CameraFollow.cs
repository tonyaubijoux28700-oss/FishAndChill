using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Caméra qui suit le joueur en douceur, sans jamais montrer ce qu'il y a au-delà de la carte.
    /// À placer sur la caméra (orthographique).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;

        [SerializeField, Tooltip("Optionnel : la caméra reste dans les limites de cette carte.")]
        private WorldMap _map;

        [SerializeField, Min(0f), Tooltip("Temps de rattrapage en secondes (0 = collée au joueur).")]
        private float _smoothTime = 0.15f;

        private Camera _camera;
        private Vector3 _velocity;

        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 desired = DesiredPosition();
            transform.position = _smoothTime > 0f
                ? Vector3.SmoothDamp(transform.position, desired, ref _velocity, _smoothTime)
                : desired;
        }

        /// <summary>Place la caméra directement sur la cible, sans glissement.</summary>
        public void SnapToTarget()
        {
            if (_target != null)
            {
                transform.position = DesiredPosition();
                _velocity = Vector3.zero;
            }
        }

        private Vector3 DesiredPosition()
        {
            Vector2 center = _target.position;
            if (_map != null && _camera != null && _camera.orthographic)
            {
                var halfExtents = new Vector2(_camera.orthographicSize * _camera.aspect, _camera.orthographicSize);
                center = CameraMath.ClampToBounds(center, halfExtents, _map.WorldBounds);
            }

            return new Vector3(center.x, center.y, transform.position.z);
        }
    }
}
