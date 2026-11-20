using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Player;
using UnityEngine;

namespace FishAndChill.Pets
{
    /// <summary>
    /// L'animal qui accompagne le joueur (<see cref="PlayerProgress.EquippedPet"/>) : il suit son chemin à une case
    /// derrière lui (<see cref="PetMath.FollowPoint"/>), marche quand il marche, s'arrête quand il s'arrête, et le
    /// rejoint d'un coup quand le joueur est loin (porte, voyage). Le bouledogue trouve parfois des pièces en se
    /// promenant (bulle avec une pièce au-dessus de lui). Aucun animal : rien n'est affiché.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PetFollower : MonoBehaviour
    {
        private const int TrailLength = 80;
        private const float FollowDistance = 1.05f;
        private const float SnapDistance = 5f;
        private const float FramesPerSecond = 7f;

        [SerializeField] private PlayerMovement _player;
        [SerializeField] private PlayerProgress _progress;
        [SerializeField, Tooltip("Bulle avec une pièce : le bouledogue a trouvé des pièces.")] private Sprite _coinBubble;

        private readonly List<Vector2> _trail = new List<Vector2>();
        private readonly System.Random _random = new System.Random(7);
        private SpriteRenderer _renderer;
        private SpriteRenderer _bubble;
        private PetDefinition _pet;
        private Vector2 _facing = Vector2.down;
        private float _walkTime;
        private float _bubbleTimer;

        private void Start()
        {
            _renderer = GetComponent<SpriteRenderer>();
            var bubbleObject = new GameObject("Bulle de l'animal");
            bubbleObject.transform.SetParent(transform, false);
            _bubble = bubbleObject.AddComponent<SpriteRenderer>();
            _bubble.sortingOrder = 100;
            _bubble.sprite = _coinBubble;
            bubbleObject.SetActive(false);
        }

        private void Update()
        {
            if (_player == null || _progress == null) return;
            PetDefinition pet = _progress.EquippedPet;
            if (pet != _pet)
            {
                _pet = pet;
                _trail.Clear();
                if (pet != null) transform.position = (Vector2)_player.transform.position + Vector2.left * 0.8f;
            }

            _renderer.enabled = pet != null;
            if (pet == null)
            {
                _bubble.gameObject.SetActive(false);
                return;
            }

            Vector2 feet = _player.transform.position;
            if (_trail.Count == 0 || Vector2.Distance(_trail[0], feet) > 0.05f)
            {
                _trail.Insert(0, feet);
                if (_trail.Count > TrailLength) _trail.RemoveAt(_trail.Count - 1);
            }

            Vector2 position = transform.position;
            if (Vector2.Distance(position, feet) > SnapDistance)
            {
                // Le joueur a franchi une porte ou voyagé : l'animal le rejoint aussitôt.
                _trail.Clear();
                _trail.Add(feet);
                transform.position = feet + Vector2.left * 0.8f;
                return;
            }

            Vector2 target = PetMath.FollowPoint(_trail, FollowDistance);
            Vector2 toTarget = target - position;
            bool moving = toTarget.magnitude > 0.06f && Vector2.Distance(position, feet) > 0.7f;
            if (moving)
            {
                float speed = Mathf.Max(3.2f, toTarget.magnitude * 4f);
                Vector2 step = Vector2.ClampMagnitude(toTarget, speed * Time.deltaTime);
                transform.position = position + step;
                _facing = step;
                _walkTime += Time.deltaTime;
            }
            else
            {
                _walkTime = 0f;
                if (Vector2.Distance(position, feet) <= 1.4f) _facing = feet - position;
            }

            ApplySprite(pet, moving);
            FindCoins(pet, moving);
        }

        private void ApplySprite(PetDefinition pet, bool moving)
        {
            Sprite[] frames;
            bool flip = false;
            if (Mathf.Abs(_facing.x) >= Mathf.Abs(_facing.y) && _facing.x != 0f)
            {
                frames = pet.Side;
                flip = _facing.x < 0f;
            }
            else
            {
                frames = _facing.y > 0f ? pet.Up : pet.Down;
            }

            if (frames == null || frames.Length == 0) return;
            // Le perroquet bat des ailes même à l'arrêt.
            bool animate = moving || pet.Flies;
            int frame = animate ? PlayerAnimator.WalkFrameIndex(moving ? _walkTime : Time.time, FramesPerSecond, frames.Length) : 0;
            _renderer.sprite = frames[Mathf.Clamp(frame, 0, frames.Length - 1)];
            _renderer.flipX = flip;
        }

        /// <summary>Le bouledogue (ou tout animal qui a cette chance) trouve parfois des pièces en marchant.</summary>
        private void FindCoins(PetDefinition pet, bool moving)
        {
            if (_bubbleTimer > 0f)
            {
                _bubbleTimer -= Time.deltaTime;
                _bubble.transform.localPosition = new Vector3(0f, 1.05f + Mathf.Round(Mathf.Sin(Time.time * 4f) * 1.5f) / 16f, 0f);
                if (_bubbleTimer <= 0f) _bubble.gameObject.SetActive(false);
            }

            int coins = PetMath.FoundCoins(_random, Time.deltaTime, moving, pet.CoinChancePerSecond, pet.CoinMin, pet.CoinMax);
            if (coins <= 0) return;
            _progress.Wallet.Add(coins);
            _bubble.gameObject.SetActive(_coinBubble != null);
            _bubbleTimer = 1.6f;
        }
    }
}
