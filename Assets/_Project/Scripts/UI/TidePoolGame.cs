using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Pêche à pied à l'épuisette. Face à un coin à bulles à portée, l'action (Espace, clic, A) sort l'épuisette au lieu de
    /// lancer : la flaque s'ouvre en grand, vue de dessus, avec quelques bestioles de la zone (une seule forme, plus grande
    /// quand elle est rare). L'épuisette suit la souris (ou le stick, ZQSD) avec le retard de l'eau ; bougée vite, elle
    /// inquiète les bêtes proches. Un coup (clic, Espace, A) rafle ce qui est calme sous le cercle (<see cref="NetRules"/>).
    /// Chaque bête réagit selon sa nature : se cacher sous un rocher, filer, s'enfouir, ou lâcher de l'encre. Les prises
    /// sont notées dans le journal (<see cref="CatchLog"/>) puis relâchées. Échap ou B pour revenir.
    /// </summary>
    public sealed class TidePoolGame : MonoBehaviour
    {
        private const float Zoom = 3f;
        private const float PoolWidth = 320f;
        private const float PoolHeight = 192f;
        private const float Reach = 4.8f;

        [SerializeField] private FishingController _fishing;
        [SerializeField] private PlayerMovement _player;
        [SerializeField] private WorldMap _map;
        [SerializeField] private DayNightCycle _cycle;
        [SerializeField] private UiSkin _skin;
        [SerializeField, Tooltip("La progression : il faut avoir acheté l'épuisette.")] private PlayerProgress _progress;

        [Header("Dessins")]
        [SerializeField, Tooltip("La flaque vue de dessus (320×192).")] private Sprite _pool;
        [SerializeField, Tooltip("La bestiole sous l'eau (3 images : posée, puis deux en marche).")] private Sprite[] _critter = new Sprite[0];
        [SerializeField, Tooltip("L'épuisette dans l'eau (40×40, cercle centré au pixel 16, 24 depuis le bas).")] private Sprite _net;

        [SerializeField, Tooltip("Rochers de la flaque où les bêtes se cachent (pixels de la flaque, y depuis le bas).")]
        private Vector2[] _rocks =
        {
            new Vector2(70f, 137f), new Vector2(240f, 52f), new Vector2(258f, 67f), new Vector2(140f, 152f), new Vector2(110f, 32f),
        };

        private sealed class Critter
        {
            public FishSpecies Species;
            public CritterBehaviour Behaviour;
            public Image Image;
            public Vector2 Position;
            public Vector2 Velocity;
            public Vector2 Target;
            public float Scale;
            public float Sensitivity;
            public float Alarm;
            public float Hidden;
            public bool Running;
            public bool Caught;
            public float Wander;
            public float Anim;
        }

        private readonly List<Critter> _critters = new List<Critter>();
        private readonly List<(Image image, float age)> _inks = new List<(Image, float)>();
        private System.Random _random;
        private FishingSpots[] _spots = new FishingSpots[0];
        private GameObject _window;
        private RectTransform _poolRect;
        private RectTransform _netRect;
        private Image _netImage;
        private Text _tries;
        private Text _message;
        private Text _prompt;
        private Sprite _inkSprite;
        private Vector2 _netPos;
        private Vector2 _netTarget;
        private Vector2 _lastNet;
        private float _netSpeed;
        private float _scoop = -1f;
        private int _triesLeft;
        private int _caught;
        private Vector2 _spotCenter;
        private FishingSpots _spotSource;
        private Vector2 _lastMouse;
        private int _openedFrame = -1;
        private int _closedFrame = -1;

        /// <summary>La flaque est-elle ouverte ?</summary>
        public bool IsOpen => _window != null && _window.activeSelf;

        private void Start()
        {
            _random = new System.Random();
            _spots = FindObjectsByType<FishingSpots>();
            BuildUi();
            _window.SetActive(false);
            if (_fishing != null) _fishing.BeforeCast = TryOpen;
        }

        // ---------- Ouverture depuis la rive ----------

        /// <summary>Le coin à bulles visé (face au pêcheur, à portée de l'épuisette), s'il y en a un.</summary>
        private bool TryFindSpot(out FishingSpots source, out Vector2 center)
        {
            source = null;
            center = Vector2.zero;
            if (_player == null || _map == null || InteriorBuilding.Occupied != null) return false;
            Vector2 origin = _player.transform.position;
            foreach (FishingSpots spots in _spots)
            {
                if (spots == null || spots.Region != _map.CurrentRegion) continue;
                IList<Vector2> centers = spots.Centers;
                int aimed = FishingSpotRules.Assist(origin, _player.Facing, centers, 0.3f, Reach);
                if (aimed < 0) continue;
                source = spots;
                center = centers[aimed];
                return true;
            }

            return false;
        }

        private bool HasNet => _progress == null || _progress.HasNet;

        private bool CanStart => !IsOpen && _fishing != null && _fishing.State == FishingState.Idle && _player != null && !_player.Locked
            && _fishing.Zone != null && _fishing.Zone.NetSpecies.Count > 0;

        /// <summary>Appelé par la pêche quand on appuie sur l'action : vrai si l'épuisette sort (pas de lancer).</summary>
        public bool TryOpen()
        {
            if (Time.frameCount == _closedFrame) return true;   // l'appui qui a refermé la flaque ne lance pas la ligne
            if (!CanStart || !HasNet || !TryFindSpot(out FishingSpots source, out Vector2 center)) return false;
            _spotSource = source;
            _spotCenter = center;
            Open();
            return true;
        }

        private void Open()
        {
            _openedFrame = Time.frameCount;
            _window.SetActive(true);
            _player.Locked = true;
            _triesLeft = NetRules.Tries;
            _caught = 0;
            _scoop = -1f;
            _netPos = _netTarget = _lastNet = new Vector2(PoolWidth * 0.5f, PoolHeight * 0.5f);
            _lastMouse = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            foreach (Critter c in _critters) Destroy(c.Image.gameObject);
            _critters.Clear();
            foreach (var ink in _inks) Destroy(ink.image.gameObject);
            _inks.Clear();
            SpawnCritters();
            Say("Approche doucement, puis donne un coup d'épuisette.");
            RefreshTries();
        }

        private void Close()
        {
            _closedFrame = Time.frameCount;
            _window.SetActive(false);
            if (_player != null) _player.Locked = false;
            _spotSource?.Consume(_spotCenter); // le coin s'épuise et finira par partir ailleurs
        }

        private void SpawnCritters()
        {
            FishingZone zone = _fishing.Zone;
            float hour = _cycle != null ? _cycle.Hour : 12f;
            var pool = new List<FishingZone.SpeciesEntry>();
            float total = 0f;
            foreach (FishingZone.SpeciesEntry entry in zone.NetSpecies)
            {
                if (entry.Species == null || !DayCycleMath.IsActive(entry.Species.ActiveTime, hour)) continue;
                pool.Add(entry);
                total += entry.Weight;
            }

            for (int i = 0; i < NetRules.Critters && total > 0f; i++)
            {
                double roll = _random.NextDouble() * total;
                FishSpecies species = pool[pool.Count - 1].Species;
                foreach (FishingZone.SpeciesEntry entry in pool)
                {
                    roll -= entry.Weight;
                    if (roll <= 0) { species = entry.Species; break; }
                }

                var critter = new Critter
                {
                    Species = species,
                    Behaviour = NetRules.Behaviour(species.Kind),
                    Scale = NetRules.Scale(species.Rarity),
                    Sensitivity = NetRules.Sensitivity(species.Rarity),
                    Position = RandomSpot(),
                    Wander = (float)_random.NextDouble() * 3f,
                };
                critter.Image = UiFactory.Plain(_poolRect, "Bestiole", Color.white);
                critter.Image.sprite = _critter.Length > 0 ? _critter[0] : null;
                critter.Image.rectTransform.anchorMin = critter.Image.rectTransform.anchorMax = Vector2.zero;
                critter.Image.rectTransform.sizeDelta = Vector2.one * 24f * Zoom * critter.Scale;
                critter.Image.transform.SetSiblingIndex(1);
                _critters.Add(critter);
            }
        }

        private Vector2 RandomSpot() => new Vector2(20f + (float)_random.NextDouble() * (PoolWidth - 40f), 20f + (float)_random.NextDouble() * (PoolHeight - 40f));

        // ---------- Chaque image ----------

        private void Update()
        {
            if (!IsOpen)
            {
                bool aimed = CanStart && TryFindSpot(out _, out _);
                _prompt.gameObject.SetActive(aimed);
                if (aimed) _prompt.text = HasNet ? "Espace : sortir l'épuisette" : "Des bulles… avec une épuisette du marchand, tu pourrais y fouiller";
                return;
            }

            _prompt.gameObject.SetActive(false);
            float dt = Time.deltaTime;
            Keyboard k = Keyboard.current;
            Gamepad g = Gamepad.current;
            if ((k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.buttonEast.wasPressedThisFrame))
            {
                Close();
                return;
            }

            MoveTarget(dt);
            _netPos = NetRules.FollowPointer(_netPos, _netTarget, dt);
            float speed = dt > 0f ? Vector2.Distance(_netPos, _lastNet) / dt : 0f;
            _netSpeed = Mathf.Lerp(_netSpeed, speed, 0.2f);
            _lastNet = _netPos;

            bool finished = _triesLeft <= 0 || _critters.TrueForAll(c => c.Caught);
            bool justOpened = Time.frameCount == _openedFrame;   // l'appui qui a ouvert la flaque ne compte pas
            bool scoopPressed = !justOpened && ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                || (k != null && k.spaceKey.wasPressedThisFrame) || (g != null && g.buttonSouth.wasPressedThisFrame));
            if (scoopPressed && finished && _scoop < 0f)
            {
                Close();
                return;
            }

            if (scoopPressed && _scoop < 0f && _triesLeft > 0)
            {
                StartScoop();
            }

            if (_scoop >= 0f)
            {
                _scoop += dt;
                if (_scoop >= NetRules.ScoopSeconds) FinishScoop();
            }

            foreach (Critter c in _critters) UpdateCritter(c, dt);
            UpdateInks(dt);
            DrawNet();
        }

        /// <summary>La souris pointe la cible ; le stick et ZQSD (touches physiques WASD) la déplacent.</summary>
        private void MoveTarget(float dt)
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 screen = mouse.position.ReadValue();
                if ((screen - _lastMouse).sqrMagnitude > 0.5f
                    && RectTransformUtility.ScreenPointToLocalPointInRectangle(_poolRect, screen, null, out Vector2 local))
                {
                    _netTarget = local / Zoom;
                }

                _lastMouse = screen;
            }

            Vector2 move = Vector2.zero;
            Keyboard k = Keyboard.current;
            if (k != null)
            {
                if (k.wKey.isPressed || k.upArrowKey.isPressed) move.y += 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) move.y -= 1f;
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) move.x -= 1f;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) move.x += 1f;
            }

            if (Gamepad.current != null) move += Gamepad.current.leftStick.ReadValue();
            _netTarget += Vector2.ClampMagnitude(move, 1f) * 90f * dt;   // lent au clavier : on approche sans troubler l'eau
            _netTarget = new Vector2(Mathf.Clamp(_netTarget.x, 0f, PoolWidth), Mathf.Clamp(_netTarget.y, 0f, PoolHeight));
        }

        private void StartScoop()
        {
            _triesLeft--;
            _scoop = 0f;
            foreach (Critter c in _critters)
            {
                if (c.Caught || c.Hidden > 0f) continue;
                c.Alarm = NetRules.SplashAlarm(c.Alarm, Vector2.Distance(c.Position, _netPos));
            }

            RefreshTries();
        }

        private void FinishScoop()
        {
            _scoop = -1f;
            var names = new List<string>();
            foreach (Critter c in _critters)
            {
                if (c.Caught || c.Hidden > 0f || !NetRules.Caught(Vector2.Distance(c.Position, _netPos), c.Alarm, c.Scale)) continue;
                c.Caught = true;
                c.Image.gameObject.SetActive(false);
                _caught++;
                bool first = !_fishing.CatchLog.IsDiscovered(c.Species.Id);
                Vector2 range = c.Species.SizeRangeCm;
                _fishing.CatchLog.Register(c.Species.Id, Mathf.Lerp(range.x, range.y, (float)_random.NextDouble()));
                string color = ColorUtility.ToHtmlStringRGB(c.Species.Rarity.DisplayColor());
                names.Add($"<color=#{color}>{c.Species.CommonName}</color>{(first ? " (nouvelle espèce !)" : string.Empty)}");
            }

            bool finished = _triesLeft <= 0 || _critters.TrueForAll(c => c.Caught);
            string end = finished ? $"\n{_caught} prise(s), relâchée(s) après les avoir notées. Clic ou Espace : revenir." : string.Empty;
            Say((names.Count > 0 ? "Dans l'épuisette : " + string.Join(", ", names) : "Raté… l'eau s'est troublée.") + end);
        }

        private void UpdateCritter(Critter c, float dt)
        {
            if (c.Caught) return;
            RectTransform rect = c.Image.rectTransform;
            if (c.Hidden > 0f)
            {
                c.Hidden -= dt;
                c.Image.enabled = false;
                if (c.Hidden <= 0f)
                {
                    c.Alarm = 0f;
                    c.Running = false;
                    c.Image.enabled = true;
                    if (c.Behaviour == CritterBehaviour.Hide) c.Position += new Vector2(Range(-14f, 14f), Range(-16f, -8f));
                }

                return;
            }

            float distance = Vector2.Distance(c.Position, _netPos);
            c.Alarm = Mathf.Clamp(c.Alarm + NetRules.AlarmDelta(_netSpeed, distance, c.Sensitivity, c.Behaviour, dt), 0f, 1.2f);
            bool moving = false;
            if (c.Alarm >= 1f && !c.Running)
            {
                React(c);
            }

            if (c.Running)
            {
                Vector2 to = c.Target - c.Position;
                if (to.magnitude < 3f)
                {
                    c.Running = false;
                    c.Hidden = Range(3.5f, 6f);   // à l'abri sous le rocher
                }
                else
                {
                    c.Position += to.normalized * 110f * dt;
                    moving = true;
                }
            }
            else if (c.Velocity.sqrMagnitude > 4f)
            {
                c.Position += c.Velocity * dt;
                c.Velocity *= Mathf.Exp(-4f * dt);
                moving = true;
            }
            else if (c.Behaviour != CritterBehaviour.Burrow)
            {
                // Promenade tranquille.
                c.Wander -= dt;
                if (c.Wander <= 0f)
                {
                    c.Wander = Range(1.5f, 4f);
                    c.Velocity = new Vector2(Range(-1f, 1f), Range(-1f, 1f)).normalized * Range(8f, 16f);
                }

                c.Position += c.Velocity * dt;
                moving = c.Velocity.sqrMagnitude > 1f;
            }

            c.Position = new Vector2(Mathf.Clamp(c.Position.x, 10f, PoolWidth - 10f), Mathf.Clamp(c.Position.y, 10f, PoolHeight - 10f));
            rect.anchoredPosition = c.Position * Zoom;

            // Pattes qui bougent quand elle marche ; les céphalopodes calmes se fondent dans le sable.
            c.Anim += dt * (moving ? 10f : 2f);
            if (_critter.Length >= 3) c.Image.sprite = moving ? _critter[1 + (int)c.Anim % 2] : _critter[(int)c.Anim % 2 == 0 ? 0 : 0];
            float alpha = c.Behaviour == CritterBehaviour.Ink ? Mathf.Clamp01(0.3f + c.Alarm * 0.8f) : 1f;
            c.Image.color = new Color(1f, 1f, 1f, alpha);
        }

        /// <summary>Elle a eu peur : chacune sa façon de s'en tirer.</summary>
        private void React(Critter c)
        {
            switch (c.Behaviour)
            {
                case CritterBehaviour.Hide:
                    c.Running = true;
                    c.Target = NearestRock(c.Position);
                    break;
                case CritterBehaviour.Dart:
                    c.Velocity = (c.Position - _netPos).normalized * 220f;
                    c.Alarm = 0.4f;
                    break;
                case CritterBehaviour.Ink:
                    AddInk(c.Position, c.Scale);
                    c.Position = RandomSpot();
                    c.Alarm = 0.3f;
                    break;
                default:
                    c.Hidden = Range(2.5f, 4.5f);   // enfouie dans le sable
                    break;
            }
        }

        private Vector2 NearestRock(Vector2 from)
        {
            Vector2 best = from;
            float d = float.MaxValue;
            foreach (Vector2 rock in _rocks)
            {
                float e = Vector2.Distance(rock, from);
                if (e < d) { d = e; best = rock; }
            }

            return best;
        }

        private float Range(float a, float b) => a + (float)_random.NextDouble() * (b - a);

        // ---------- Encre, épuisette ----------

        private void AddInk(Vector2 at, float scale)
        {
            Image ink = UiFactory.Plain(_poolRect, "Encre", Color.white);
            ink.sprite = _inkSprite;
            ink.rectTransform.anchorMin = ink.rectTransform.anchorMax = Vector2.zero;
            ink.rectTransform.anchoredPosition = at * Zoom;
            ink.rectTransform.sizeDelta = Vector2.one * 30f * Zoom * scale;
            _inks.Add((ink, 0f));
        }

        private void UpdateInks(float dt)
        {
            for (int i = _inks.Count - 1; i >= 0; i--)
            {
                (Image image, float age) = _inks[i];
                age += dt;
                if (age > 2.5f)
                {
                    Destroy(image.gameObject);
                    _inks.RemoveAt(i);
                    continue;
                }

                image.rectTransform.localScale = Vector3.one * (1f + age * 0.6f);
                image.color = new Color(1f, 1f, 1f, 1f - age / 2.5f);
                _inks[i] = (image, age);
            }
        }

        private void DrawNet()
        {
            float squeeze = _scoop >= 0f ? 1f - _scoop / NetRules.ScoopSeconds * 0.4f : 1f;
            _netRect.anchoredPosition = _netPos * Zoom;
            _netRect.localScale = Vector3.one * squeeze;
            _netRect.SetAsLastSibling();
        }

        private void Say(string text) => _message.text = text;

        private void RefreshTries() => _tries.text = $"Coups : {_triesLeft}";

        // ---------- Construction ----------

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Interface de l'épuisette", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 14;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();

            _prompt = UiFactory.Label(root, "Invite épuisette", 32, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Bold(_prompt);
            _prompt.text = "Espace : sortir l'épuisette";
            UiFactory.Place(_prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(900f, 50f));
            _prompt.gameObject.SetActive(false);

            Image window = UiFactory.Sliced(root, "Flaque", _skin != null ? _skin.Panel : null, _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            _window = window.gameObject;
            UiFactory.Center(window.rectTransform, new Vector2(0f, 10f), new Vector2(PoolWidth * Zoom + 60f, PoolHeight * Zoom + 230f));
            UiFactory.TitlePlaque(window.rectTransform, _skin, "Pêche à l'épuisette", 30, new Vector2(0f, PoolHeight * Zoom * 0.5f + 90f), new Vector2(460f, 64f), out _);

            Image pool = UiFactory.Plain(window.rectTransform, "Eau", Color.white);
            pool.sprite = _pool;
            _poolRect = pool.rectTransform;
            UiFactory.Center(_poolRect, new Vector2(0f, 40f), new Vector2(PoolWidth * Zoom, PoolHeight * Zoom));
            _poolRect.pivot = Vector2.zero;   // coordonnées des bêtes et de l'épuisette depuis le coin bas gauche
            _poolRect.anchoredPosition = new Vector2(-PoolWidth * Zoom * 0.5f, 40f - PoolHeight * Zoom * 0.5f);
            pool.gameObject.AddComponent<RectMask2D>();

            _netImage = UiFactory.Plain(_poolRect, "Épuisette", Color.white);
            _netImage.sprite = _net;
            _netRect = _netImage.rectTransform;
            _netRect.anchorMin = _netRect.anchorMax = Vector2.zero;
            _netRect.pivot = new Vector2(16f / 40f, 24f / 40f);
            _netRect.sizeDelta = Vector2.one * 40f * Zoom;

            _tries = UiFactory.Label(window.rectTransform, "Coups", 26, TextAnchor.MiddleLeft, new Color(1f, 0.84f, 0.3f));
            UiFactory.Bold(_tries);
            UiFactory.Place(_tries.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(34f, 78f), new Vector2(200f, 40f));
            _message = UiFactory.Label(window.rectTransform, "Message", 24, TextAnchor.MiddleCenter, Color.white);
            _message.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Place(_message.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(60f, 76f), new Vector2(-320f, 64f));
            Text help = UiFactory.Label(window.rectTransform, "Aide", 20, TextAnchor.MiddleRight, new Color(0.66f, 0.72f, 0.78f));
            help.text = "Souris ou ZQSD : bouger   •   Clic ou Espace : coup   •   Échap : revenir";
            UiFactory.Place(help.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(-60f, 28f));

            _inkSprite = MakeInk();
        }

        /// <summary>Nuage d'encre : rond sombre aux bords doux (fait au démarrage).</summary>
        private static Sprite MakeInk()
        {
            const int size = 32;
            var tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
            for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f)) / 16f;
                float noise = Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.35f;
                float a = d + noise < 0.95f ? Mathf.Clamp01(0.95f - d - noise) * 1.6f : 0f;
                tex.SetPixel(x, y, new Color(0.10f, 0.08f, 0.14f, Mathf.Min(0.9f, a)));
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
