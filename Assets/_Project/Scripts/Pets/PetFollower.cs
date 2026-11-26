using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEngine;

namespace FishAndChill.Pets
{
    /// <summary>
    /// L'animal qui accompagne le joueur (<see cref="PlayerProgress.EquippedPet"/>) : il suit son chemin à une case
    /// derrière lui (<see cref="PetMath.FollowPoint"/>), marche quand il marche, s'arrête quand il s'arrête, et le
    /// rejoint d'un coup quand le joueur est loin (porte, voyage). Pendant la pêche, il vient s'asseoir à côté du joueur
    /// et regarde l'eau avec lui ; à chaque prise il saute de joie à sa façon (<see cref="PetMath.ReactionOffset"/>),
    /// avec un cœur pour une belle prise (rare et plus, nouvelle espèce, légende). Tourné vers lui, E le caresse
    /// (petit bond et cœur ; c'est l'inventaire qui transmet la touche, pour garder ses priorités). Le bouledogue trouve
    /// parfois des pièces en se promenant (bulle avec une pièce). Aucun animal : rien n'est affiché.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PetFollower : MonoBehaviour
    {
        private const int TrailLength = 80;
        private const float FollowDistance = 1.05f;
        private const float SnapDistance = 5f;
        private const float FramesPerSecond = 7f;
        private const float BubbleSeconds = 1.6f;
        /// <summary>Hauteur du haut du chapeau, où se pose l'oiseau (en cases au-dessus des pieds du joueur).</summary>
        private const float PerchHeight = 1.42f;

        [SerializeField] private PlayerMovement _player;
        [SerializeField] private PlayerProgress _progress;
        [SerializeField, Tooltip("La pêche : l'animal s'assoit pendant qu'on pêche et réagit aux prises.")] private FishingController _fishing;
        [SerializeField, Tooltip("Pour s'asseoir sur la terre ferme, pas dans l'eau.")] private WorldMap _map;
        [SerializeField, Tooltip("Bulle avec une pièce : le bouledogue a trouvé des pièces.")] private Sprite _coinBubble;
        [SerializeField, Tooltip("Bulle avec un cœur : une caresse, une belle prise.")] private Sprite _heartBubble;

        private readonly List<Vector2> _trail = new List<Vector2>();
        private readonly System.Random _random = new System.Random(7);
        private SpriteRenderer _renderer;
        private SpriteRenderer _bubble;
        private PetDefinition _pet;
        private Vector2 _ground;
        private Vector2 _facing = Vector2.down;
        private float _walkTime;
        private float _bubbleTimer;
        private float _reaction = -1f;
        private float _idleTime;

        /// <summary>Le joueur est tourné vers son animal, assez près pour le caresser (E).</summary>
        public bool PlayerCanPet => _pet != null && _player != null && !IsFishing && _reaction < 0f
                                    && PetMath.CanPet(_player.transform.position, _player.Facing, _ground);

        public string PetName => _pet != null ? _pet.DisplayName : string.Empty;

        private bool IsFishing => _fishing != null && _fishing.State != FishingState.Idle;

        private void Start()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _ground = transform.position;
            var bubbleObject = new GameObject("Bulle de l'animal");
            bubbleObject.transform.SetParent(transform, false);
            _bubble = bubbleObject.AddComponent<SpriteRenderer>();
            _bubble.sortingOrder = 100;
            _bubble.sprite = _coinBubble;
            bubbleObject.SetActive(false);
            if (_fishing != null)
            {
                _fishing.FishCaught += OnFishCaught;
                _fishing.LegendBeaten += OnLegendBeaten;
            }
        }

        private void OnDestroy()
        {
            if (_fishing != null)
            {
                _fishing.FishCaught -= OnFishCaught;
                _fishing.LegendBeaten -= OnLegendBeaten;
            }
        }

        private void OnFishCaught(CaughtFish fish, bool isNew, bool bagFull)
        {
            React(fish != null && fish.Species != null && PetMath.HeartFor(fish.Species.Rarity, isNew));
        }

        private void OnLegendBeaten(LegendFish legend, int coins) => React(true);

        /// <summary>E devant l'animal : il saute de joie et montre un cœur.</summary>
        public void Pet()
        {
            if (_pet == null) return;
            if (_player != null) _facing = (Vector2)_player.transform.position - _ground;
            React(true);
        }

        private void React(bool heart)
        {
            if (_pet == null) return;
            _reaction = 0f;
            if (heart) ShowBubble(_heartBubble);
        }

        private void ShowBubble(Sprite sprite)
        {
            if (sprite == null) return;
            _bubble.sprite = sprite;
            _bubble.gameObject.SetActive(true);
            _bubbleTimer = BubbleSeconds;
        }

        private void Update()
        {
            if (_player == null || _progress == null) return;
            PetDefinition pet = _progress.EquippedPet;
            if (pet != _pet)
            {
                _pet = pet;
                _trail.Clear();
                _reaction = -1f;
                if (pet != null) _ground = (Vector2)_player.transform.position + Vector2.left * 0.8f;
            }

            _renderer.enabled = pet != null;
            if (pet == null)
            {
                _bubble.gameObject.SetActive(false);
                transform.position = _ground;
                return;
            }

            Vector2 feet = _player.transform.position;
            if (_trail.Count == 0 || Vector2.Distance(_trail[0], feet) > 0.05f)
            {
                _trail.Insert(0, feet);
                if (_trail.Count > TrailLength) _trail.RemoveAt(_trail.Count - 1);
            }

            bool moving;
            bool perching = false;
            if (Vector2.Distance(_ground, feet) > SnapDistance)
            {
                // Le joueur a franchi une porte ou voyagé : l'animal le rejoint aussitôt.
                _trail.Clear();
                _trail.Add(feet);
                _ground = feet + Vector2.left * 0.8f;
                moving = false;
            }
            else if (IsFishing)
            {
                // Pendant la pêche : il vient s'asseoir à côté du joueur et regarde l'eau avec lui.
                Vector2 seat = PetMath.SitPoint(feet, _player.Facing, p => _map == null || _map.CanStandAt(p, 0.2f) || pet.Flies);
                moving = WalkTo(seat, 0.05f);
                if (!moving) _facing = _player.Facing;
            }
            else if (pet.Flies && IdleMath.PetIdle(_idleTime))
            {
                // L'oiseau en attente vient se poser sur la tête du pêcheur.
                Vector2 perch = feet + Vector2.up * PerchHeight;
                _ground = Vector2.MoveTowards(_ground, perch, 3.5f * Time.deltaTime);
                perching = true;
                moving = Vector2.Distance(_ground, perch) > 0.02f;
                _facing = Vector2.down;
            }
            else
            {
                Vector2 target = PetMath.FollowPoint(_trail, FollowDistance);
                moving = Vector2.Distance(_ground, feet) > 0.7f && WalkTo(target, 0.06f);
                if (!moving && _reaction < 0f && Vector2.Distance(_ground, feet) <= 1.4f) _facing = feet - _ground;
            }

            // L'attente : le joueur et l'animal immobiles depuis un moment (l'oiseau compte encore quand il vole vers la tête).
            bool calm = !IsFishing && _reaction < 0f && !_player.IsMoving && (!moving || perching);
            _idleTime = calm ? _idleTime + Time.deltaTime : 0f;

            if (moving) _walkTime += Time.deltaTime;
            else _walkTime = 0f;

            // La réaction de joie : bond, frétillement… par-dessus la place au sol.
            float tilt = 0f;
            Vector2 offset = Vector2.zero;
            if (_reaction >= 0f)
            {
                offset = PetMath.ReactionOffset(pet.Kind, _reaction, out tilt);
                _reaction += Time.deltaTime;
                if (_reaction >= PetMath.ReactionSeconds) _reaction = -1f;
            }

            transform.position = _ground + offset;
            transform.rotation = Quaternion.Euler(0f, 0f, _renderer.flipX ? -tilt : tilt);
            bool idlePose = calm && IdleMath.PetIdle(_idleTime) && !(perching && moving) && pet.Idle != null && pet.Idle.Length >= 2;
            if (idlePose)
            {
                _renderer.sprite = pet.Idle[Mathf.Clamp(IdleMath.PetFrame(_idleTime), 0, pet.Idle.Length - 1)];
                _renderer.flipX = false;
            }
            else
            {
                ApplySprite(pet, moving || _reaction >= 0f && pet.Flies);
            }

            // Perché sur la tête, l'oiseau passe devant le pêcheur.
            _renderer.sortingOrder = perching ? 11 : 10;
            UpdateBubble();
            FindCoins(pet, moving);
        }

        /// <summary>Avance vers <paramref name="target"/> ; vrai s'il marche encore.</summary>
        private bool WalkTo(Vector2 target, float stopDistance)
        {
            Vector2 toTarget = target - _ground;
            if (toTarget.magnitude <= stopDistance) return false;
            float speed = Mathf.Max(3.2f, toTarget.magnitude * 4f);
            Vector2 step = Vector2.ClampMagnitude(toTarget, speed * Time.deltaTime);
            _ground += step;
            _facing = step;
            return true;
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
            // Le perroquet bat des ailes même à l'arrêt ; pendant une réaction, tout le monde s'agite.
            bool animate = moving || pet.Flies || _reaction >= 0f;
            float clock = moving ? _walkTime : Time.time * (_reaction >= 0f ? 1.8f : 1f);
            int frame = animate ? PlayerAnimator.WalkFrameIndex(clock, FramesPerSecond, frames.Length) : 0;
            _renderer.sprite = frames[Mathf.Clamp(frame, 0, frames.Length - 1)];
            _renderer.flipX = flip;
        }

        private void UpdateBubble()
        {
            if (_bubbleTimer <= 0f) return;
            _bubbleTimer -= Time.deltaTime;
            _bubble.transform.localPosition = new Vector3(0f, 1.05f + Mathf.Round(Mathf.Sin(Time.time * 4f) * 1.5f) / 16f, 0f);
            _bubble.transform.rotation = Quaternion.identity;
            if (_bubbleTimer <= 0f) _bubble.gameObject.SetActive(false);
        }

        /// <summary>Le bouledogue (ou tout animal qui a cette chance) trouve parfois des pièces en marchant.</summary>
        private void FindCoins(PetDefinition pet, bool moving)
        {
            int coins = PetMath.FoundCoins(_random, Time.deltaTime, moving, pet.CoinChancePerSecond, pet.CoinMin, pet.CoinMax);
            if (coins <= 0) return;
            _progress.Wallet.Add(coins);
            ShowBubble(_coinBubble);
        }
    }
}
