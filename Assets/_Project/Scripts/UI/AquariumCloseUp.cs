using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Vue de près d'un bassin de l'aquarium : devant la vitre, Haut fait zoomer la caméra sur le bassin (il remplit
    /// l'écran en hauteur), Gauche / Droite longent la vitre, la souris ou Espace (poisson suivant) choisissent un
    /// pensionnaire : il est encadré et sa fiche s'affiche en bas (nom, nom savant, rareté, taille, description).
    /// Échap, Bas ou E reviennent. Le pêcheur ne bouge pas pendant ce temps.
    /// </summary>
    public sealed class AquariumCloseUp : MonoBehaviour
    {
        [SerializeField] private AquariumBuilding _building;
        [SerializeField] private CameraFollow _camera;
        [SerializeField] private UiSkin _skin;
        [SerializeField, Tooltip("Les bassins de l'aquarium.")] private AquariumTank[] _tanks = new AquariumTank[0];

        private const float ZoomSeconds = 0.45f;
        private const float PanSpeed = 7f;
        private const float HoldToOpen = 0.2f;

        private static readonly Color Gold = new Color(1f, 0.86f, 0.42f);
        private static readonly Color Muted = new Color(0.82f, 0.86f, 0.90f);

        private Camera _view;
        private PlayerMovement _player;
        private AquariumTank _tank;
        private Transform _pivot;
        private Transform _savedTarget;
        private float _savedSize;
        private float _closeSize;
        private float _zoom;          // 0 vue normale, 1 vue de près
        private bool _leaving;
        private float _upHeld;
        private CaughtFish _selected;
        private int _cycle = -1;
        private Vector2 _lastMouse;

        private GameObject _root;
        private Text _title;
        private Text _hint;
        private GameObject _card;
        private Text _name;
        private Text _latin;
        private Text _details;
        private Text _description;
        private Image[] _corners;

        /// <summary>La vue de près est ouverte (ou en train de s'ouvrir / se fermer).</summary>
        public bool IsOpen => _tank != null;

        /// <summary>Un bassin est devant le joueur : la vue de près peut s'ouvrir (pour l'aide affichée).</summary>
        public bool CanOpen => _tank == null && _building != null && _building.PlayerAtTank && _building.Visitor != null && !_building.Visitor.Locked
                               && TankInFront() != null;

        private void Start()
        {
            if (_camera != null) _view = _camera.GetComponent<Camera>();
            BuildUi();
        }

        private AquariumTank TankInFront()
        {
            if (_building == null || _building.Visitor == null) return null;
            float x = _building.Visitor.transform.position.x;
            foreach (AquariumTank tank in _tanks)
            {
                if (tank == null) continue;
                Rect area = tank.WorldArea;
                if (x >= area.xMin - 0.5f && x <= area.xMax + 0.5f) return tank;
            }

            return null;
        }

        private void Update()
        {
            if (_tank == null)
            {
                // Devant la vitre, on pousse vers le haut un court instant : on regarde de près.
                bool up = Held(Key.UpArrow, "z", "w") || (Gamepad.current != null && (Gamepad.current.dpad.up.isPressed || Gamepad.current.leftStick.y.ReadValue() > 0.6f));
                _upHeld = up && CanOpen ? _upHeld + Time.deltaTime : 0f;
                if (_upHeld >= HoldToOpen) Open(TankInFront());
                return;
            }

            float dt = Time.unscaledDeltaTime;
            _zoom = Mathf.MoveTowards(_zoom, _leaving ? 0f : 1f, dt / ZoomSeconds);
            float eased = Mathf.SmoothStep(0f, 1f, _zoom);
            if (_view != null) _view.orthographicSize = Mathf.Lerp(_savedSize, _closeSize, eased);
            if (_leaving)
            {
                if (_zoom <= 0f) Close();
                return;
            }

            _root.SetActive(_zoom >= 1f);
            if (_zoom < 1f) return;

            if (Pressed(Key.Escape) || Pressed(Key.DownArrow, "s") || InteractInput.PressedThisFrame()
                || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame))
            {
                Leave();
                return;
            }

            // Longer la vitre.
            float pan = (Held(Key.RightArrow, "d") ? 1f : 0f) - (Held(Key.LeftArrow, "q", "a") ? 1f : 0f);
            if (Gamepad.current != null) pan += Gamepad.current.leftStick.x.ReadValue() + (Gamepad.current.dpad.right.isPressed ? 1f : 0f) - (Gamepad.current.dpad.left.isPressed ? 1f : 0f);
            Rect area = _tank.WorldArea;
            float halfView = _view != null ? _view.orthographicSize * _view.aspect : 4f;
            float minX = area.xMin + halfView - 0.4f, maxX = area.xMax - halfView + 0.4f;
            Vector3 p = _pivot.position;
            if (Mathf.Abs(pan) > 0.1f)
            {
                p.x += Mathf.Clamp(pan, -1f, 1f) * PanSpeed * dt;
                _cycle = -1;
            }
            else if (_cycle >= 0 && _selected != null && _tank.BoundsOf(_selected, out Bounds followed))
            {
                // Poisson choisi au clavier : la vue le suit en douceur.
                p.x = Mathf.Lerp(p.x, followed.center.x, 1f - Mathf.Exp(-dt * 3f));
            }

            p.x = minX <= maxX ? Mathf.Clamp(p.x, minX, maxX) : area.center.x;
            _pivot.position = p;

            // Poisson suivant (Espace), ou celui sous la souris.
            if (Pressed(Key.Space) || Pressed(Key.Enter) || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame))
            {
                IReadOnlyList<CaughtFish> residents = _tank.Residents;
                if (residents.Count > 0)
                {
                    _cycle = (_cycle + 1) % residents.Count;
                    _selected = residents[_cycle];
                }
            }
            else if (Mouse.current != null && _view != null)
            {
                Vector2 mouse = Mouse.current.position.ReadValue();
                if ((mouse - _lastMouse).sqrMagnitude > 1f || Mouse.current.leftButton.wasPressedThisFrame)
                {
                    _lastMouse = mouse;
                    CaughtFish under = _tank.SpecimenAt(_view.ScreenToWorldPoint(mouse));
                    if (under != null)
                    {
                        _selected = under;
                        _cycle = IndexOf(under);
                    }
                }
            }

            ShowSelection();
        }

        private int IndexOf(CaughtFish specimen)
        {
            IReadOnlyList<CaughtFish> residents = _tank.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                if (residents[i] == specimen) return i;
            }

            return -1;
        }

        /// <summary>Ouvre la vue de près sur ce bassin (public pour les essais par script).</summary>
        public void Open(AquariumTank tank)
        {
            if (tank == null || _camera == null || _view == null || _tank != null) return;
            _tank = tank;
            _player = _building.Visitor;
            _player.Locked = true;
            _leaving = false;
            _zoom = 0f;
            _upHeld = 0f;
            _selected = null;
            _cycle = -1;
            _lastMouse = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

            Rect area = tank.WorldArea;
            _savedSize = _view.orthographicSize;
            _closeSize = AquariumMath.CloseUpSize(new Rect(area.x, area.y - 0.6f, area.width, area.height + 1.2f), 100f, 0.5f);
            if (_pivot == null) _pivot = new GameObject("Vue de près du bassin").transform;
            _pivot.position = new Vector3(Mathf.Clamp(_player.transform.position.x, area.xMin, area.xMax), area.center.y, 0f);
            _savedTarget = _camera.Target;
            _camera.Target = _pivot;
            _camera.SetBoundsOverride(new Rect(area.xMin - 0.6f, area.yMin - 1.4f, area.width + 1.2f, area.height + 2.8f));

            // Le reste de l'interface (heure, pièces, sac, minimap) se retire : rien ne cache le bassin.
            _hidden.Clear();
            foreach (Canvas canvas in FindObjectsByType<Canvas>())
            {
                if (canvas.isRootCanvas && canvas.enabled && canvas.gameObject != _root && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas.enabled = false;
                    _hidden.Add(canvas);
                }
            }

            FishingZone zone = tank.Zone;
            _title.text = zone != null ? $"Bassin - {zone.DisplayName}" : "Bassin";
            _card.SetActive(false);
            foreach (Image corner in _corners) corner.gameObject.SetActive(false);
        }

        private void Leave()
        {
            _leaving = true;
            _root.SetActive(false);
        }

        private void Close()
        {
            if (_view != null) _view.orthographicSize = _savedSize;
            _camera.Target = _savedTarget != null ? _savedTarget : _player.transform;
            _camera.SetBoundsOverride(_building.WorldRect(AquariumBuilding.Floors[Mathf.Clamp(_building.CurrentFloor, 0, AquariumBuilding.Floors.Length - 1)]));
            _camera.SnapToTarget();
            _player.Locked = false;
            _tank = null;
            _root.SetActive(false);
            foreach (Canvas canvas in _hidden)
            {
                if (canvas != null) canvas.enabled = true;
            }

            _hidden.Clear();
        }

        private readonly List<Canvas> _hidden = new List<Canvas>();

        private void ShowSelection()
        {
            bool shown = _selected != null && _selected.Species != null && _tank.BoundsOf(_selected, out _);
            _card.SetActive(shown);
            if (!shown)
            {
                foreach (Image corner in _corners) corner.gameObject.SetActive(false);
                _hint.text = "Gauche / Droite : longer la vitre   •   Souris ou Espace : choisir un poisson   •   Échap : revenir";
                return;
            }

            FishSpecies species = _selected.Species;
            _name.text = species.CommonName;
            _name.color = species.Rarity.DisplayColor();
            _latin.text = $"{species.ScientificName}  •  {species.Kind.DisplayName()}";
            _details.text = $"{species.Rarity.DisplayName()}   •   {_selected.SizeCm:0} cm (de {species.SizeRangeCm.x:0} à {species.SizeRangeCm.y:0} cm)";
            _description.text = species.Description;
            _hint.text = "Gauche / Droite : longer la vitre   •   Espace : poisson suivant   •   Échap : revenir";

            // Les coins dorés autour du poisson choisi.
            _tank.BoundsOf(_selected, out Bounds b);
            var root = (RectTransform)_root.transform;
            Vector2 Local(Vector3 world)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, _view.WorldToScreenPoint(world), null, out Vector2 local);
                return local;
            }

            Vector2 min = Local(b.min - new Vector3(0.15f, 0.15f, 0f)), max = Local(b.max + new Vector3(0.15f, 0.15f, 0f));

            // La fiche se met du côté opposé au poisson, pour ne pas le cacher.
            var cardRect = (RectTransform)_card.transform;
            bool fishOnLeft = (min.x + max.x) * 0.5f < 0f;
            Vector2 cardAnchor = fishOnLeft ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
            cardRect.anchorMin = cardAnchor;
            cardRect.anchorMax = cardAnchor;
            cardRect.pivot = cardAnchor;
            cardRect.anchoredPosition = new Vector2(fishOnLeft ? -36f : 36f, 110f);
            Vector2[] at = { new Vector2(min.x, max.y), new Vector2(max.x, max.y), new Vector2(min.x, min.y), new Vector2(max.x, min.y) };
            for (int c = 0; c < 4; c++)
            {
                float sx = c % 2 == 0 ? 1f : -1f, sy = c < 2 ? -1f : 1f;
                Image h = _corners[c * 2], v = _corners[c * 2 + 1];
                h.gameObject.SetActive(true);
                v.gameObject.SetActive(true);
                h.rectTransform.anchoredPosition = at[c] + new Vector2(sx * 12f, 0f);
                v.rectTransform.anchoredPosition = at[c] + new Vector2(0f, sy * 12f);
            }
        }

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Aquarium - Vue de près", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 16;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();
            _root = canvasObject;

            // Un voile sombre en haut et en bas : le bassin reste au milieu de l'écran, l'interface sur le voile.
            Image top = UiFactory.Plain(root, "Voile du haut", new Color(0.02f, 0.04f, 0.07f, 0.75f));
            UiFactory.Place(top.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 110f));
            Image bottom = UiFactory.Plain(root, "Voile du bas", new Color(0.02f, 0.04f, 0.07f, 0.75f));
            UiFactory.Place(bottom.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 90f));

            _title = UiFactory.TitlePlaque(root, _skin, "Bassin", 30, new Vector2(0f, 480f), new Vector2(620f, 64f), out _);
            _hint = UiFactory.Label(root, "Aide", 24, TextAnchor.MiddleCenter, Muted);
            UiFactory.Center(_hint.rectTransform, new Vector2(0f, -495f), new Vector2(1800f, 40f));

            // La fiche du poisson choisi.
            Image card = UiFactory.Sliced(root, "Fiche du poisson", _skin != null ? _skin.Panel : null, _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f, 0.95f));
            _card = card.gameObject;
            UiFactory.Place(card.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(36f, 110f), new Vector2(760f, 230f));
            _name = UiFactory.Label(card.rectTransform, "Nom", 36, TextAnchor.UpperLeft, Gold);
            UiFactory.Bold(_name);
            UiFactory.Place(_name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -24f), new Vector2(700f, 44f));
            _latin = UiFactory.Label(card.rectTransform, "Nom savant", 22, TextAnchor.UpperLeft, Muted);
            UiFactory.Place(_latin.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -70f), new Vector2(700f, 30f));
            _details = UiFactory.Label(card.rectTransform, "Rareté et taille", 24, TextAnchor.UpperLeft, Color.white);
            UiFactory.Place(_details.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -104f), new Vector2(700f, 32f));
            _description = UiFactory.Label(card.rectTransform, "Description", 20, TextAnchor.UpperLeft, Muted);
            _description.horizontalOverflow = HorizontalWrapMode.Wrap;
            _description.verticalOverflow = VerticalWrapMode.Truncate;
            UiFactory.Place(_description.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -142f), new Vector2(696f, 74f));

            // Coins dorés (deux traits par coin) autour du poisson choisi.
            _corners = new Image[8];
            for (int i = 0; i < 8; i++)
            {
                _corners[i] = UiFactory.Plain(root, $"Coin {i / 2 + 1}", Gold);
                UiFactory.Center(_corners[i].rectTransform, Vector2.zero, i % 2 == 0 ? new Vector2(24f, 4f) : new Vector2(4f, 24f));
                _corners[i].gameObject.SetActive(false);
            }

            _root.SetActive(false);
        }

        private static bool Held(Key arrow, params string[] letters) => KeyState(arrow, letters, false);

        private static bool Pressed(Key arrow, params string[] letters) => KeyState(arrow, letters, true);

        private static bool KeyState(Key arrow, string[] letters, bool pressed)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;
            var control = keyboard[arrow];
            if (pressed ? control.wasPressedThisFrame : control.isPressed) return true;
            foreach (string letter in letters)
            {
                var key = keyboard.FindKeyOnCurrentKeyboardLayout(letter);
                if (key != null && (pressed ? key.wasPressedThisFrame : key.isPressed)) return true;
            }

            return false;
        }
    }
}
