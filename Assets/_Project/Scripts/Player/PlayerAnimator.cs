using UnityEngine;

namespace FishAndChill.Player
{
    /// <summary>
    /// Choisit l'image du pêcheur selon la direction du regard et s'il marche.
    /// Chaque tableau : [0] = à l'arrêt, [1..n] = images de marche jouées en boucle.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private Sprite[] _down;
        [SerializeField] private Sprite[] _up;
        [SerializeField] private Sprite[] _left;
        [SerializeField] private Sprite[] _right;

        [SerializeField, Min(1f), Tooltip("Vitesse de l'animation de marche (images par seconde).")]
        private float _walkFramesPerSecond = 8f;

        private SpriteRenderer _renderer;
        private float _walkTime;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_movement == null)
            {
                _movement = GetComponent<PlayerMovement>();
            }
        }

        private void LateUpdate()
        {
            if (_movement == null)
            {
                return;
            }

            Sprite[] frames = FramesFor(DirectionOf(_movement.Facing));
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            if (_movement.IsMoving && frames.Length > 1)
            {
                _walkTime += Time.deltaTime;
                _renderer.sprite = frames[WalkFrameIndex(_walkTime, _walkFramesPerSecond, frames.Length)];
            }
            else
            {
                _walkTime = 0f;
                _renderer.sprite = frames[0];
            }
        }

        /// <summary>Direction d'image la plus proche du regard (l'horizontal l'emporte à égalité).</summary>
        public static SpriteDirection DirectionOf(Vector2 facing)
        {
            if (Mathf.Abs(facing.x) >= Mathf.Abs(facing.y) && facing.x != 0f)
            {
                return facing.x > 0f ? SpriteDirection.Right : SpriteDirection.Left;
            }

            return facing.y < 0f ? SpriteDirection.Down : SpriteDirection.Up;
        }

        /// <summary>Index de l'image de marche (jamais 0, réservé à l'arrêt).</summary>
        public static int WalkFrameIndex(float walkTime, float framesPerSecond, int frameCount)
        {
            int walkFrames = Mathf.Max(1, frameCount - 1);
            return 1 + Mathf.FloorToInt(walkTime * framesPerSecond) % walkFrames;
        }

        private Sprite[] FramesFor(SpriteDirection direction)
        {
            switch (direction)
            {
                case SpriteDirection.Up: return _up;
                case SpriteDirection.Left: return _left;
                case SpriteDirection.Right: return _right;
                default: return _down;
            }
        }
    }
}
