using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Player;
using FishAndChill.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.World
{
    /// <summary>
    /// Un site de plongée : la bouée posée dans l'eau d'une zone et le fond marin qui lui correspond, hors de la carte
    /// (comme l'intérieur des bâtiments). Au bord de l'eau, tourné vers la bouée, E fait plonger (il faut le masque et le
    /// tuba du marchand) : on arrive au pied de la corde, on nage (le pêcheur porte masque et palmes), on ramasse les
    /// décorations posées sur le sable en passant dessus, et on remonte en revenant à la corde. La jauge d'air descend :
    /// à court d'air, on remonte seul en lâchant la moitié de ce qu'on portait (<see cref="DiveMath"/>). Des poissons de
    /// la zone nagent autour, la lumière danse sur le fond, des bulles montent du plongeur.
    /// </summary>
    public sealed class DiveSite : MonoBehaviour
    {
        [SerializeField] private PlayerMovement _player;
        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private WorldMap _map;
        [SerializeField] private CameraFollow _camera;
        [SerializeField] private UiSkin _skin;
        [SerializeField, Tooltip("La zone : ses décorations et ses poissons.")] private FishingZone _zone;
        [SerializeField, Tooltip("Coin bas-gauche du fond marin, dans le monde.")] private Vector2 _origin;
        [SerializeField, Tooltip("La bouée, dans l'eau de la zone.")] private Vector2 _buoy;
        [SerializeField] private int _seed = 4242;
        [SerializeField] private Sprite[] _caustics = new Sprite[0];
        [SerializeField] private Sprite _bubble;
        [SerializeField] private Color _waterColor = new Color(0.10f, 0.42f, 0.52f);

        private const float BuoyReach = 2.6f;

        private sealed class Pickup
        {
            public Decoration Item;
            public SpriteRenderer Renderer;
            public Vector2 Position;
        }

        private sealed class Fish
        {
            public SpriteRenderer Renderer;
            public Sprite[] Frames;
            public Vector2 Position;
            public Vector2 Target;
            public float Clock;
        }

        private readonly List<Pickup> _pickups = new List<Pickup>();
        private readonly List<Decoration> _carried = new List<Decoration>();
        private readonly List<Fish> _fish = new List<Fish>();
        private readonly List<(SpriteRenderer Renderer, float Age)> _bubbles = new List<(SpriteRenderer, float)>();
        private readonly List<SpriteRenderer> _light = new List<SpriteRenderer>();
        private System.Random _random;
        private DivePlan _plan;
        private PlayerAnimator _animator;
        private Vector2 _shore;
        private Vector2 _shoreFacing;
        private Color _outsideBackground;
        private float _air;
        private bool _leftRope;
        private float _bubbleTimer;
        private float _messageTimer;

        private Text _prompt;
        private Text _message;
        private GameObject _gauge;
        private Image _airFill;
        private Text _airLabel;
        private Text _carriedLabel;

        /// <summary>Le site où l'on plonge en ce moment, ou celui devant lequel on se tient.</summary>
        public static DiveSite Current { get; private set; }

        /// <summary>E sert à plonger ou on est sous l'eau : le sac ne s'ouvre pas.</summary>
        public static bool BlocksInteract => Current != null && (Current.Diving || Current.PlayerAtBuoy);

        public bool Diving { get; private set; }
        public float Air => _air;
        public IReadOnlyList<Decoration> Carried => _carried;
        public Rect Area => new Rect(_origin, new Vector2(DivePlan.Width, DivePlan.Height));

        public bool PlayerAtBuoy
        {
            get
            {
                if (Diving || _player == null || !_player.CanMove) return false;
                Vector2 feet = _player.transform.position;
                Vector2 toBuoy = _buoy - feet;
                return toBuoy.magnitude < BuoyReach && Vector2.Dot(toBuoy.normalized, _player.Facing) > 0.3f;
            }
        }

        private Vector2 World(Vector2 cell) => _origin + cell;

        private void Start()
        {
            _random = new System.Random(_seed + System.Environment.TickCount);
            _plan = DivePlan.Build(_seed);
            if (_player != null) _animator = _player.GetComponent<PlayerAnimator>();
            if (_map != null)
            {
                _map.AddWalkableArea(Area);
                foreach ((Vector2 center, Vector2 half) in _plan.Rocks)
                {
                    Vector2 c = World(center);
                    _map.AddObstacle(new Rect(c.x - half.x * 0.8f, c.y - half.y * 0.75f, half.x * 1.6f, half.y * 1.5f));
                }
            }

            BuildLight();
            BuildUi();
        }

        private void BuildLight()
        {
            if (_caustics == null || _caustics.Length == 0) return;
            for (int x = 0; x < DivePlan.Width; x += 4)
            {
                for (int y = 0; y < DivePlan.Height; y += 4)
                {
                    var go = new GameObject("Lumière sur le fond");
                    go.transform.SetParent(transform, false);
                    go.transform.position = World(new Vector2(x + 2f, y + 2f));
                    var renderer = go.AddComponent<SpriteRenderer>();
                    renderer.sprite = _caustics[0];
                    renderer.sortingOrder = -7;
                    _light.Add(renderer);
                }
            }
        }

        private void Update()
        {
            if (_player == null || _progress == null) return;
            if (!Diving)
            {
                bool atBuoy = PlayerAtBuoy;
                if (atBuoy) Current = this;
                else if (Current == this) Current = null;
                _prompt.gameObject.SetActive(atBuoy && _messageTimer <= 0f);
                if (atBuoy)
                {
                    _prompt.text = _progress.Decorations.HasMask ? "E : plonger" : "Il te faut un masque et un tuba (chez le marchand)";
                    if (InteractInput.PressedThisFrame() && _progress.Decorations.HasMask) StartDive();
                }
            }
            else
            {
                UpdateDive(Time.deltaTime);
            }

            AnimateLight();
            if (_messageTimer > 0f)
            {
                _messageTimer -= Time.deltaTime;
                if (_messageTimer <= 0f) _message.gameObject.SetActive(false);
            }
        }

        /// <summary>Plonge depuis la bouée. Public pour les tests.</summary>
        public void StartDive()
        {
            if (Diving) return;
            Diving = true;
            Current = this;
            _shore = _player.transform.position;
            _shoreFacing = _player.Facing;
            _air = DiveMath.MaskAir;
            _leftRope = false;
            _carried.Clear();
            _player.SpeedFactor = 0.8f;
            if (_animator != null) _animator.Swimming = true;
            _player.Teleport(World(_plan.Rope + Vector2.down * 1.2f), Vector2.down);
            if (_camera != null)
            {
                _camera.SetBoundsOverride(Area);
                _camera.SnapToTarget();
                Camera cam = _camera.GetComponent<Camera>();
                _outsideBackground = cam.backgroundColor;
                cam.backgroundColor = _waterColor;
            }

            SpawnPickups();
            SpawnFish();
            _prompt.gameObject.SetActive(false);
            _gauge.SetActive(true);
            Say("Tu plonges ! Ramasse les décorations, puis reviens à la corde pour remonter.", 3f);
        }

        private void SpawnPickups()
        {
            foreach (Pickup p in _pickups) if (p.Renderer != null) Destroy(p.Renderer.gameObject);
            _pickups.Clear();
            List<Decoration> items = _progress.DecorationCatalog != null ? _progress.DecorationCatalog.OfZone(_zone) : new List<Decoration>();
            var rarities = new List<Rarity>();
            foreach (Decoration d in items) rarities.Add(d.Rarity);
            foreach ((Vector2 spot, int index) in DiveMath.Pick(_plan, rarities, _random))
            {
                Decoration item = items[index];
                var go = new GameObject($"Décoration - {item.DisplayName}");
                go.transform.SetParent(transform, false);
                go.transform.position = World(spot);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = item.Sprite;
                renderer.sortingOrder = 9;
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                _pickups.Add(new Pickup { Item = item, Renderer = renderer, Position = World(spot) });
            }
        }

        private void SpawnFish()
        {
            foreach (Fish f in _fish) if (f.Renderer != null) Destroy(f.Renderer.gameObject);
            _fish.Clear();
            if (_zone == null) return;
            var species = new List<FishSpecies>();
            foreach (FishingZone.SpeciesEntry e in _zone.Species)
            {
                if (e.Species != null && e.Species.Kind == CreatureKind.Fish && e.Species.Rarity <= Rarity.Uncommon && e.Species.MiniFrames != null && e.Species.MiniFrames.Length > 0) species.Add(e.Species);
            }

            for (int i = 0; i < 7 && species.Count > 0; i++)
            {
                FishSpecies s = species[_random.Next(species.Count)];
                var go = new GameObject($"Poisson - {s.CommonName}");
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = s.MiniFrames[0];
                renderer.sortingOrder = 12;
                renderer.color = new Color(0.86f, 0.96f, 1f, 0.9f);
                Vector2 at = World(new Vector2(2f + (float)_random.NextDouble() * (DivePlan.Width - 4f), 2f + (float)_random.NextDouble() * (DivePlan.Height - 4f)));
                _fish.Add(new Fish { Renderer = renderer, Frames = s.MiniFrames, Position = at, Target = at, Clock = (float)_random.NextDouble() * 3f });
            }
        }

        private void UpdateDive(float dt)
        {
            Vector2 feet = _player.transform.position;
            Vector2 rope = World(_plan.Rope);
            if (!_leftRope && Vector2.Distance(feet, rope) > 2.2f) _leftRope = true;

            // Ramasser en passant dessus.
            for (int i = _pickups.Count - 1; i >= 0; i--)
            {
                Pickup p = _pickups[i];
                if (Vector2.Distance(feet, p.Position) > 0.8f) continue;
                _carried.Add(p.Item);
                Destroy(p.Renderer.gameObject);
                _pickups.RemoveAt(i);
                Say($"+ {p.Item.DisplayName}{(p.Item.Rarity >= Rarity.Rare ? " (rare !)" : string.Empty)}", 1.6f);
            }

            _air -= dt;
            float ratio = Mathf.Clamp01(_air / DiveMath.MaskAir);
            _airFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            _airFill.color = ratio > 0.5f ? new Color(0.40f, 0.82f, 0.96f) : ratio > 0.2f ? new Color(0.98f, 0.80f, 0.30f) : new Color(0.96f, 0.36f, 0.30f);
            _airLabel.text = $"Air : {Mathf.CeilToInt(Mathf.Max(0f, _air))} s";
            _carriedLabel.text = _carried.Count == 0 ? "Rien dans le filet" : $"Dans le filet : {_carried.Count}";

            // Bulles qui montent du plongeur.
            _bubbleTimer -= dt;
            if (_bubbleTimer <= 0f && _bubble != null)
            {
                _bubbleTimer = 0.7f + (float)_random.NextDouble() * 0.6f;
                var go = new GameObject("Bulle");
                go.transform.SetParent(transform, false);
                go.transform.position = feet + new Vector2(0.2f, 1.3f);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = _bubble;
                renderer.sortingOrder = 30;
                _bubbles.Add((renderer, 0f));
            }

            for (int i = _bubbles.Count - 1; i >= 0; i--)
            {
                (SpriteRenderer r, float age) = _bubbles[i];
                age += dt;
                r.transform.position += new Vector3(Mathf.Sin(age * 6f) * 0.01f, dt * 1.1f, 0f);
                r.color = new Color(1f, 1f, 1f, 1f - age / 1.8f);
                if (age > 1.8f) { Destroy(r.gameObject); _bubbles.RemoveAt(i); }
                else _bubbles[i] = (r, age);
            }

            MoveFish(dt);

            if (_leftRope && Vector2.Distance(feet, rope) < 1.1f) Surface(false);
            else if (_air <= 0f) Surface(true);
        }

        private void MoveFish(float dt)
        {
            foreach (Fish f in _fish)
            {
                if (Vector2.Distance(f.Position, f.Target) < 0.3f)
                {
                    f.Target = World(new Vector2(2f + (float)_random.NextDouble() * (DivePlan.Width - 4f), 2f + (float)_random.NextDouble() * (DivePlan.Height - 4f)));
                }

                // Ils s'écartent du plongeur.
                Vector2 away = f.Position - (Vector2)_player.transform.position;
                if (away.magnitude < 1.6f) f.Target = f.Position + away.normalized * 4f;
                Vector2 step = Vector2.ClampMagnitude(f.Target - f.Position, 1.6f * dt);
                f.Position += step;
                f.Clock += dt;
                f.Renderer.transform.position = new Vector3(Mathf.Round(f.Position.x * 16f) / 16f, Mathf.Round(f.Position.y * 16f) / 16f, 0f);
                f.Renderer.sprite = f.Frames[Mathf.FloorToInt(f.Clock * 6f) % f.Frames.Length];
                if (Mathf.Abs(step.x) > 0.0001f) f.Renderer.flipX = step.x < 0f;
            }
        }

        /// <summary>Remonte à la surface (à court d'air : on lâche la moitié du filet). Public pour les tests.</summary>
        public void Surface(bool outOfAir)
        {
            if (!Diving) return;
            Diving = false;
            int kept = outOfAir ? DiveMath.KeptWhenOutOfAir(_carried.Count) : _carried.Count;
            var names = new List<string>();
            for (int i = 0; i < kept; i++)
            {
                _progress.Decorations.Add(_carried[i].Id);
                names.Add(_carried[i].DisplayName);
            }

            foreach (Pickup p in _pickups) if (p.Renderer != null) Destroy(p.Renderer.gameObject);
            _pickups.Clear();
            foreach (Fish f in _fish) if (f.Renderer != null) Destroy(f.Renderer.gameObject);
            _fish.Clear();
            foreach ((SpriteRenderer r, float _) in _bubbles) if (r != null) Destroy(r.gameObject);
            _bubbles.Clear();

            _player.SpeedFactor = 1f;
            if (_animator != null) _animator.Swimming = false;
            _player.Teleport(_shore, _shoreFacing);
            if (_camera != null)
            {
                _camera.SetBoundsOverride(null);
                _camera.SnapToTarget();
                _camera.GetComponent<Camera>().backgroundColor = _outsideBackground;
            }

            _gauge.SetActive(false);
            if (Current == this) Current = null;
            string found = names.Count == 0 ? "rien" : string.Join(", ", names);
            if (outOfAir) Say($"Plus d'air ! Tu lâches une partie du filet et remontes avec : {found}.", 4f);
            else Say(names.Count == 0 ? "Tu remontes les mains vides." : $"Tu remontes avec : {found} ! Pose-les à l'aquarium (R devant le bassin).", 4f);
            _carried.Clear();
        }

        private void AnimateLight()
        {
            if (_light.Count == 0 || _caustics.Length == 0) return;
            Sprite frame = _caustics[Mathf.FloorToInt(Time.time * 4f) % _caustics.Length];
            foreach (SpriteRenderer r in _light) r.sprite = frame;
        }

        private void Say(string text, float seconds)
        {
            _message.text = text;
            _message.gameObject.SetActive(true);
            _messageTimer = seconds;
        }

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Interface de la plongée", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 12;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();
            Color gold = new Color(1f, 0.84f, 0.30f);

            _prompt = UiFactory.Label(root, "Invitation", 30, TextAnchor.MiddleCenter, gold);
            UiFactory.Place(_prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(1000f, 44f));
            _prompt.gameObject.SetActive(false);

            _message = UiFactory.Label(root, "Message", 32, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Place(_message.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(1500f, 90f));
            _message.gameObject.SetActive(false);

            // La jauge d'air, en haut au centre, dans un cadre de la charte.
            Image frame = UiFactory.Sliced(root, "Jauge d'air", _skin != null ? _skin.Panel : null, _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            UiFactory.Place(frame.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(560f, 118f));
            _gauge = frame.gameObject;
            Image track = UiFactory.Sliced(frame.rectTransform, "Fond de la jauge", _skin != null ? _skin.Slot : null, new Color(0.10f, 0.16f, 0.22f));
            UiFactory.Center(track.rectTransform, new Vector2(0f, 10f), new Vector2(480f, 30f));
            _airFill = UiFactory.Plain(track.rectTransform, "Air", new Color(0.40f, 0.82f, 0.96f));
            _airFill.rectTransform.anchorMin = Vector2.zero;
            _airFill.rectTransform.anchorMax = Vector2.one;
            _airFill.rectTransform.offsetMin = new Vector2(4f, 4f);
            _airFill.rectTransform.offsetMax = new Vector2(-4f, -4f);
            _airLabel = UiFactory.Label(frame.rectTransform, "Air restant", 26, TextAnchor.MiddleLeft, Color.white);
            UiFactory.Center(_airLabel.rectTransform, new Vector2(-100f, -30f), new Vector2(260f, 36f));
            _carriedLabel = UiFactory.Label(frame.rectTransform, "Filet", 26, TextAnchor.MiddleRight, gold);
            UiFactory.Center(_carriedLabel.rectTransform, new Vector2(100f, -30f), new Vector2(260f, 36f));
            _gauge.SetActive(false);
        }
    }
}
