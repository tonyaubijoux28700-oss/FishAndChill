using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEngine;

namespace FishAndChill.Villagers
{
    /// <summary>
    /// Un habitant qui vit sa journée : la nuit chez lui (debout dans sa maison, <see cref="_indoor"/>), le jour dehors.
    /// À l'heure de sortie il apparaît devant sa porte et marche jusqu'à son coin préféré (<see cref="_anchor"/>), s'y
    /// balade (un point au hasard, une pause, un autre point), puis rentre à pied le soir. Un enfant suit son
    /// <see cref="_leader"/> au lieu de choisir ses propres buts. Il s'arrête et se tourne vers le joueur quand celui-ci
    /// est tout près ; on lui parle avec E (<see cref="UI.DialogueBox"/>). Images : celles du pêcheur, avec ses
    /// accessoires (<see cref="CharacterSprites.Build(CharacterLook, NpcExtras)"/>).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Villager : MonoBehaviour
    {
        private enum State { Home, Leaving, Out, Returning }

        private const float BodyRadius = 0.2f;
        private const float TalkDistance = 1.3f;
        private const float FramesPerSecond = 6f;

        [SerializeField] private VillagerProfile _profile;
        [SerializeField, Tooltip("Sa maison : il en sort le matin et y rentre le soir.")] private VillagerHouseBuilding _home;
        [SerializeField, Tooltip("L'habitant debout dans sa maison (affiché quand il est chez lui).")] private SpriteRenderer _indoor;
        [SerializeField, Tooltip("Centre de son coin préféré, sur l'île.")] private Vector2 _anchor;
        [SerializeField, Tooltip("Optionnel : un parent qu'il suit au lieu de se balader seul.")] private Villager _leader;
        [SerializeField] private Vector2 _leaderOffset = new Vector2(1.1f, -0.5f);
        [SerializeField] private WorldMap _map;
        [SerializeField] private PlayerMovement _player;
        [SerializeField] private DayNightCycle _clock;
        [SerializeField] private WeatherSystem _weather;
        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private int _seed = 1;

        private static readonly List<Villager> All = new List<Villager>();

        private SpriteRenderer _renderer;
        private CharacterSprites _sprites;
        private System.Random _random;
        private State _state;
        private Vector2 _target;
        private Vector2 _facing = Vector2.down;
        private float _pause;
        private float _walkTime;
        private float _stuck;
        private bool _moving;
        private bool _talking;

        /// <summary>Tous les habitants présents (pour trouver celui à qui parler).</summary>
        public static IReadOnlyList<Villager> Everyone => All;

        public string DisplayName => _profile != null ? _profile.DisplayName : name;

        public bool IsHome => _state == State.Home;

        private Vector2 DoorStep => _home != null ? (Vector2)_home.transform.position + Vector2.down * 0.7f : _anchor;

        private float Hour => _clock != null ? _clock.Hour : 12f;

        private void OnEnable() => All.Add(this);

        private void OnDisable() => All.Remove(this);

        private void Start()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _random = new System.Random(_seed);
            if (_profile != null)
            {
                _sprites = CharacterSprites.Build(_profile.Look, _profile.Extras);
                if (_indoor != null) _indoor.sprite = _sprites.Down[0];
            }

            // Au lancement, chacun est déjà là où il doit être : dans son coin le jour, chez lui la nuit.
            if (IsOutsideTime())
            {
                _state = State.Out;
                transform.position = SpawnPoint();
                _pause = VillagerSchedule.PauseSeconds(_random, 0.5f, 3f);
                _target = NextWanderTarget();
            }
            else
            {
                _state = State.Home;
            }

            ApplyVisibility();
            ApplySprite();
        }

        private void Update()
        {
            if (_sprites == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            bool outsideTime = IsOutsideTime();
            _moving = false;
            switch (_state)
            {
                case State.Home:
                    if (outsideTime)
                    {
                        _state = State.Leaving;
                        transform.position = DoorStep;
                        _target = _leader != null ? (Vector2)_leader.transform.position + _leaderOffset : _anchor;
                        _facing = Vector2.down;
                    }

                    break;

                case State.Leaving:
                    if (!outsideTime) BeginReturn();
                    else if (!Frozen() && Walk(_target, dt, forceArrive: true)) { _state = State.Out; _pause = VillagerSchedule.PauseSeconds(_random, _profile.MinPause, _profile.MaxPause); }
                    break;

                case State.Out:
                    if (!outsideTime)
                    {
                        BeginReturn();
                        break;
                    }

                    if (Frozen()) break;
                    if (_leader != null) _target = (Vector2)_leader.transform.position + _leaderOffset;
                    if (_pause > 0f)
                    {
                        _pause -= dt;
                        break;
                    }

                    if (Walk(_target, dt, forceArrive: false))
                    {
                        _pause = _leader != null ? 0.4f : VillagerSchedule.PauseSeconds(_random, _profile.MinPause, _profile.MaxPause);
                        _target = NextWanderTarget();
                    }

                    break;

                case State.Returning:
                    if (outsideTime) { _state = State.Out; _target = NextWanderTarget(); }
                    else if (!Frozen() && Walk(DoorStep, dt, forceArrive: true)) _state = State.Home;
                    break;
            }

            if (_moving) _walkTime += dt;
            else _walkTime = 0f;
            ApplyVisibility();
            ApplySprite();
        }

        private bool IsOutsideTime() => _profile == null || VillagerSchedule.IsOutside(Hour, _profile.LeaveHour, _profile.HomeHour);

        private void BeginReturn()
        {
            _state = State.Returning;
            _pause = 0f;
        }

        /// <summary>Arrêté pendant qu'on lui parle, ou quand le joueur est tout près (il se tourne vers lui).</summary>
        private bool Frozen()
        {
            if (_talking) return true;
            if (_player == null || InteriorBuilding.Occupied != null) return false;
            Vector2 toPlayer = (Vector2)_player.transform.position - (Vector2)transform.position;
            if (toPlayer.sqrMagnitude > 0.9f * 0.9f) return false;
            _facing = toPlayer;
            return true;
        }

        private Vector2 NextWanderTarget()
        {
            if (_leader != null) return (Vector2)_leader.transform.position + _leaderOffset;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector2 candidate = VillagerSchedule.WanderTarget(_random, _anchor, _profile != null ? _profile.WanderRadius : 4f);
                if (_map == null || _map.CanStandAt(candidate, BodyRadius)) return candidate;
            }

            return _anchor;
        }

        private Vector3 SpawnPoint()
        {
            Vector2 spot = _leader != null ? _anchor + _leaderOffset : _anchor;
            return _map == null || _map.CanStandAt(spot, BodyRadius) ? (Vector3)spot : (Vector3)DoorStep;
        }

        /// <summary>
        /// Un pas vers <paramref name="target"/> (d'abord tout droit, sinon en glissant le long de l'obstacle). Vrai une fois
        /// arrivé. Bloqué trop longtemps : un trajet obligé (sortie, retour) saute au but, une balade change de but.
        /// </summary>
        private bool Walk(Vector2 target, float dt, bool forceArrive)
        {
            Vector2 position = transform.position;
            Vector2 toTarget = target - position;
            float step = (_profile != null ? _profile.Speed : 1.6f) * dt;
            if (toTarget.magnitude <= Mathf.Max(step, 0.05f))
            {
                transform.position = target;
                _stuck = 0f;
                return true;
            }

            Vector2 move = toTarget.normalized * step;
            foreach (Vector2 attempt in new[] { move, new Vector2(move.x, 0f), new Vector2(0f, move.y) })
            {
                if (attempt.sqrMagnitude < 1e-6f) continue;
                Vector2 next = position + attempt;
                if (_map == null || _map.CanStandAt(next, BodyRadius))
                {
                    transform.position = next;
                    _facing = attempt;
                    _moving = true;
                    _stuck = 0f;
                    return false;
                }
            }

            _stuck += dt;
            if (_stuck < 1.5f) return false;
            _stuck = 0f;
            if (forceArrive)
            {
                transform.position = target;
                return true;
            }

            _target = NextWanderTarget();
            return false;
        }

        private void ApplyVisibility()
        {
            bool home = _state == State.Home;
            _renderer.enabled = !home;
            if (_indoor != null) _indoor.enabled = home;
        }

        private void ApplySprite()
        {
            Sprite[] frames;
            switch (PlayerAnimator.DirectionOf(_facing))
            {
                case SpriteDirection.Up: frames = _sprites.Up; break;
                case SpriteDirection.Left: frames = _sprites.Left; break;
                case SpriteDirection.Right: frames = _sprites.Right; break;
                default: frames = _sprites.Down; break;
            }

            _renderer.sprite = _moving ? frames[PlayerAnimator.WalkFrameIndex(_walkTime, FramesPerSecond, frames.Length)] : frames[0];
        }

        /// <summary>Le joueur peut-il lui parler : dehors tout près de lui, ou chez lui quand le joueur est dans sa maison.</summary>
        public bool CanTalk(Vector2 playerFeet, out float distance)
        {
            distance = float.MaxValue;
            if (_state == State.Home)
            {
                if (_indoor == null || _home == null || !_home.IsPlayerInside) return false;
                distance = Vector2.Distance(playerFeet, _indoor.transform.position);
            }
            else
            {
                if (InteriorBuilding.Occupied != null) return false;
                distance = Vector2.Distance(playerFeet, transform.position);
            }

            return distance <= TalkDistance;
        }

        /// <summary>Commence une conversation : se tourne vers le joueur et choisit ses répliques.</summary>
        public string[] Talk(Vector2 playerFeet)
        {
            _talking = true;
            if (_state != State.Home)
            {
                _facing = playerFeet - (Vector2)transform.position;
                ApplySprite();
            }

            string id = _profile != null ? _profile.Id : name;
            var context = new DialogueContext
            {
                FirstMeeting = !VillagerMemory.HasMet(id),
                Raining = _weather != null && _weather.Rain > 0.3f,
                Night = Hour >= 19f || Hour < 6f,
                AquariumSpecies = _progress != null && _progress.Aquarium != null ? _progress.Aquarium.SpeciesCount : 0,
            };
            VillagerMemory.MarkMet(id);
            return VillagerDialogue.Pick(_profile != null ? _profile.Lines : null, context, _random.Next());
        }

        public void EndTalk() => _talking = false;
    }
}
