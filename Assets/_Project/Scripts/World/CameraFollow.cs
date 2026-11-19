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
        private Rect? _boundsOverride;

        /// <summary>Limites temporaires (ex. l'intérieur d'un bâtiment) ; null pour revenir à la carte.</summary>
        public void SetBoundsOverride(Rect? bounds)
        {
            _boundsOverride = bounds;
        }

        /// <summary>Décalage vers ce qu'il faut regarder (ex. le bouchon pendant la pêche) ; la caméra y glisse en douceur.</summary>
        public Vector2 LookAhead { get; set; }

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

        private Vector3 _lastShake;
        private float _shakeStrength;
        private float _shakeTime;
        private float _shakeDuration;

        /// <summary>Secoue la caméra (coup de foudre…) : décalage de <paramref name="strength"/> cases qui s'éteint en <paramref name="seconds"/> s.</summary>
        public void Shake(float strength, float seconds)
        {
            if (strength < _shakeStrength * (1f - _shakeTime / Mathf.Max(0.01f, _shakeDuration)))
            {
                return;   // une secousse plus forte est déjà en cours
            }

            _shakeStrength = strength;
            _shakeDuration = Mathf.Max(0.01f, seconds);
            _shakeTime = 0f;
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            // La secousse s'ajoute à la position suivie sans la perturber (on la retire avant de suivre).
            Vector3 basePosition = transform.position - _lastShake;
            Vector3 desired = DesiredPosition();
            basePosition = _smoothTime > 0f
                ? Vector3.SmoothDamp(basePosition, desired, ref _velocity, _smoothTime)
                : desired;
            Vector3 shake = Vector3.zero;
            if (_shakeTime < _shakeDuration)
            {
                _shakeTime += Time.unscaledDeltaTime;
                float s = _shakeStrength * (1f - _shakeTime / _shakeDuration);
                shake = new Vector3(Random.Range(-s, s), Random.Range(-s, s), 0f);
            }

            _lastShake = shake;
            transform.position = basePosition + shake;
        }

        /// <summary>Place la caméra directement sur la cible, sans glissement.</summary>
        public void SnapToTarget()
        {
            if (_target != null)
            {
                transform.position = DesiredPosition();
                _lastShake = Vector3.zero;
                _velocity = Vector3.zero;
            }
        }

        private Vector3 DesiredPosition()
        {
            Vector2 center = (Vector2)_target.position + (_boundsOverride.HasValue ? Vector2.zero : LookAhead);
            if (_camera != null && _camera.orthographic && (_boundsOverride.HasValue || _map != null))
            {
                var halfExtents = new Vector2(_camera.orthographicSize * _camera.aspect, _camera.orthographicSize);
                Rect bounds = _boundsOverride ?? _map.WorldBounds;
                center = CameraMath.ClampToBounds(center, halfExtents, bounds);
            }

            return new Vector3(center.x, center.y, transform.position.z);
        }
    }
}
