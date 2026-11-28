using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Journal de quêtes, à part du sac : une icône (parchemin) à droite de celle du carnet, touche J (clic sur l'icône,
    /// ou clic sur le stick gauche de la manette). Trois rubriques : les commandes des habitants (<see cref="QuestPage"/>),
    /// la quête de Chiyo (<see cref="ChiyoQuestPage"/>) et les légendes (<see cref="LegendQuestPage"/>). Gauche / Droite ou
    /// clic sur un onglet : changer de rubrique ; J ou Échap : fermer. Le pêcheur ne bouge pas tant qu'il est ouvert.
    /// </summary>
    public sealed class QuestJournal : MonoBehaviour
    {
        private const float IconSize = 112f;
        private const float WindowWidth = 1700f;
        private const float WindowHeight = 760f;

        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.66f, 0.72f, 0.78f);

        [SerializeField] private UiSkin _skin;
        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private FishingController _fishing;
        [SerializeField] private PlayerMovement _player;
        [SerializeField, Tooltip("Icône du journal (parchemin, pixel art).")] private Sprite _icon;
        [SerializeField, Tooltip("Case sombre à coins arrondis (cartes des quêtes).")] private Sprite _card;

        private enum Section { Orders, Chiyo, Legends }

        private RectTransform _iconRect;
        private GameObject _window;
        private RectTransform _windowRect;
        private readonly RectTransform[] _pages = new RectTransform[3];
        private readonly Image[] _tabs = new Image[3];
        private QuestPage _orders;
        private ChiyoQuestPage _chiyo;
        private LegendQuestPage _legends;
        private Section _section = Section.Orders;
        private int _openedFrame = -1;

        public bool IsOpen => _window != null && _window.activeSelf;

        /// <summary>Le journal est ouvert (le sac ne s'ouvre pas par-dessus).</summary>
        public static bool Busy { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Busy = false;

        private void OnDisable() => Busy = false;

        private void Start() => BuildUi();

        private static bool TogglePressed()
        {
            Keyboard k = Keyboard.current;
            Gamepad g = Gamepad.current;
            return (k != null && k.jKey.wasPressedThisFrame) || (g != null && g.leftStickButton.wasPressedThisFrame);
        }

        private static bool Over(RectTransform rect, Vector2 screen) =>
            rect != null && rect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null);

        /// <summary>Hors pêche, hors menus et conversations : le pêcheur doit être libre de ses mouvements.</summary>
        private bool CanOpenNow()
        {
            if (!Save.SaveManager.InGame || DialogueBox.Busy && !IsOpen) return false;
            if (_fishing != null && _fishing.State != FishingState.Idle && _fishing.State != FishingState.Result) return false;
            return _player == null || !_player.Locked;
        }

        private void Update()
        {
            if (_window == null) return;
            Busy = IsOpen;
            _iconRect.gameObject.SetActive(Save.SaveManager.InGame);
            Mouse mouse = Mouse.current;
            Vector2 p = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            bool click = mouse != null && mouse.leftButton.wasPressedThisFrame;
            _iconRect.localScale = Vector3.one * (Over(_iconRect, p) ? 1.05f : 1f);

            if (!IsOpen)
            {
                if ((TogglePressed() || (click && Over(_iconRect, p))) && CanOpenNow()) Open();
                return;
            }

            if (Time.frameCount == _openedFrame) return;
            Keyboard k = Keyboard.current;
            Gamepad g = Gamepad.current;
            if (TogglePressed() || (k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.buttonEast.wasPressedThisFrame))
            {
                Close();
                return;
            }

            int step = 0;
            if ((k != null && (k.rightArrowKey.wasPressedThisFrame || k.tabKey.wasPressedThisFrame)) || (g != null && g.dpad.right.wasPressedThisFrame)) step = 1;
            if ((k != null && k.leftArrowKey.wasPressedThisFrame) || (g != null && g.dpad.left.wasPressedThisFrame)) step = -1;
            if (step != 0) Show((Section)(((int)_section + step + 3) % 3));

            for (int i = 0; i < _tabs.Length; i++)
            {
                bool over = Over(_tabs[i].rectTransform, p);
                _tabs[i].rectTransform.localScale = Vector3.one * (over ? 1.05f : 1f);
                if (over && click) Show((Section)i);
            }
        }

        /// <summary>Ouvre le journal sur sa dernière rubrique (public pour les tests).</summary>
        public void Open()
        {
            _window.SetActive(true);
            _openedFrame = Time.frameCount;
            if (_player != null) _player.Locked = true;
            Show(_section);
        }

        /// <summary>Ouvre le journal sur une rubrique : 0 commandes, 1 Chiyo, 2 légendes (tests).</summary>
        public void Open(int section)
        {
            _section = (Section)Mathf.Clamp(section, 0, 2);
            Open();
        }

        public void Close()
        {
            _window.SetActive(false);
            if (_player != null) _player.Locked = false;
        }

        private void Show(Section section)
        {
            _section = section;
            for (int i = 0; i < 3; i++)
            {
                _pages[i].gameObject.SetActive(i == (int)section);
                _tabs[i].color = i == (int)section ? Color.white : new Color(0.55f, 0.55f, 0.6f);
            }

            switch (section)
            {
                case Section.Orders: _orders.Refresh(); break;
                case Section.Chiyo: _chiyo.Refresh(); break;
                default: _legends.Refresh(); break;
            }
        }

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Journal de quêtes - Interface", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 11;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();

            // Icône du parchemin, à droite de celles du sac et du carnet, touche J.
            Image iconBase = UiFactory.Sliced(root, "Journal", _skin != null ? _skin.Slot : null, _skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
            _iconRect = iconBase.rectTransform;
            float x = 24f + (IconSize + 22f) * 2f + IconSize * 0.5f;
            UiFactory.Place(_iconRect, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(x, 24f + IconSize * 0.5f), new Vector2(IconSize, IconSize));
            Image icon = UiFactory.Plain(_iconRect, "Icône", Color.white);
            icon.sprite = _icon;
            icon.preserveAspect = true;
            UiFactory.Center(icon.rectTransform, new Vector2(0f, 6f), new Vector2(IconSize - 26f, IconSize - 26f));
            Text key = UiFactory.Label(_iconRect, "Touche", 24, TextAnchor.UpperLeft, Gold);
            key.text = KeyNames.Of(Key.J, "J");
            UiFactory.Bold(key);
            UiFactory.Place(key.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -6f), new Vector2(30f, 28f));

            // La fenêtre, comme celle du sac : titre, onglets, rubriques, aide.
            Image window = UiFactory.Sliced(root, "Fenêtre du journal", _skin != null ? _skin.Panel : null, _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            _window = window.gameObject;
            _windowRect = window.rectTransform;
            UiFactory.Center(_windowRect, new Vector2(0f, 10f), new Vector2(WindowWidth, WindowHeight));
            _windowRect.localScale = Vector3.one * 1.08f;
            UiFactory.TitlePlaque(_windowRect, _skin, "Journal de quêtes", 30, Vector2.zero, new Vector2(460f, 64f), out RectTransform titlePlaque);
            UiFactory.Place(titlePlaque, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 64f));

            string[] names = { "Commandes", "Chiyo", "Légendes" };
            for (int i = 0; i < 3; i++)
            {
                var page = new GameObject($"Rubrique {names[i]}", typeof(RectTransform));
                _pages[i] = page.GetComponent<RectTransform>();
                _pages[i].SetParent(_windowRect, false);
                UiFactory.Stretch(_pages[i]);
                UiFactory.TitlePlaque(_windowRect, _skin, names[i], 24, new Vector2(-736f + i * 202f, 345f), new Vector2(196f, 48f), out RectTransform tab);
                _tabs[i] = tab.GetComponent<Image>();
            }

            _orders = new QuestPage(_pages[0], _skin, _card);
            _chiyo = new ChiyoQuestPage(_pages[1], _skin, _card, _progress, _fishing);
            _legends = new LegendQuestPage(_pages[2], _card, _progress, _fishing);

            Text help = UiFactory.Label(_windowRect, "Aide", 25, TextAnchor.MiddleCenter, Muted);
            help.text = $"Gauche / Droite ou clic : rubrique   •   {KeyNames.Of(Key.J, "J")} ou Échap : fermer";
            UiFactory.Place(help.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(-30f, 30f));
            _window.SetActive(false);
        }
    }
}
