using System.Collections.Generic;
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
    /// ou stick gauche enfoncé à la manette). Trois onglets : quêtes principales (les légendes), secondaires (Chiyo…),
    /// quotidiennes (les commandes des habitants) — chacun liste à gauche les quêtes récupérées
    /// (<see cref="QuestCatalog"/>), et à droite le détail de celle choisie : image, étapes cochées, note. Gauche / Droite
    /// ou clic : onglet ; Haut / Bas ou clic : quête ; J ou Échap : fermer.
    /// </summary>
    public sealed class QuestJournal : MonoBehaviour
    {
        private const float IconSize = 112f;
        private const float WindowWidth = 1700f;
        private const float WindowHeight = 760f;
        private const int MaxRows = 8;

        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.66f, 0.72f, 0.78f);
        private static readonly Color Text = new Color(0.96f, 0.94f, 0.88f);
        private static readonly Color Blue = new Color(0.36f, 0.70f, 0.86f);
        private static readonly Color Grey = new Color(0.40f, 0.42f, 0.46f);

        [SerializeField] private UiSkin _skin;
        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private FishingController _fishing;
        [SerializeField] private PlayerMovement _player;
        [SerializeField, Tooltip("Icône du journal (parchemin, pixel art).")] private Sprite _icon;
        [SerializeField, Tooltip("Case sombre à coins arrondis (lignes et fiche des quêtes).")] private Sprite _card;

        private static readonly string[] TabNames = { "Principales", "Secondaires", "Quotidiennes" };
        private static readonly string[] Empty =
        {
            "Aucune quête principale pour l'instant. Les légendes se réveillent quand on trouve leurs fragments.",
            "Aucune quête secondaire pour l'instant. Parle aux gens que tu croises.",
            "Aucune commande en cours. Les habitants en ont une nouvelle chaque matin.",
        };

        private sealed class Row
        {
            public Image Back;
            public Image Gem;
            public Text Title;
        }

        private sealed class StepView
        {
            public Image Gem;
            public Text Label;
        }

        private RectTransform _iconRect;
        private GameObject _window;
        private RectTransform _windowRect;
        private readonly Image[] _tabs = new Image[3];
        private readonly List<Row> _rows = new List<Row>();
        private readonly List<StepView> _steps = new List<StepView>();
        private readonly List<Image> _pearls = new List<Image>();
        private GameObject _detail;
        private Image _picture;
        private Text _title;
        private Text _where;
        private Text _note;
        private Text _empty;
        private List<QuestEntry> _quests = new List<QuestEntry>();
        private int _tab;
        private int _selected;
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
            if (!Save.SaveManager.InGame || DialogueBox.Busy) return false;
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

            int tab = 0, row = 0;
            if ((k != null && (k.rightArrowKey.wasPressedThisFrame || k.tabKey.wasPressedThisFrame)) || (g != null && g.dpad.right.wasPressedThisFrame)) tab = 1;
            if ((k != null && k.leftArrowKey.wasPressedThisFrame) || (g != null && g.dpad.left.wasPressedThisFrame)) tab = -1;
            if ((k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)) || (g != null && g.dpad.down.wasPressedThisFrame)) row = 1;
            if ((k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)) || (g != null && g.dpad.up.wasPressedThisFrame)) row = -1;
            if (tab != 0) ShowTab((_tab + tab + 3) % 3);
            else if (row != 0 && _quests.Count > 0) Select((_selected + row + _quests.Count) % _quests.Count);

            for (int i = 0; i < _tabs.Length; i++)
            {
                bool over = Over(_tabs[i].rectTransform, p);
                _tabs[i].rectTransform.localScale = Vector3.one * (over ? 1.05f : 1f);
                if (over && click) ShowTab(i);
            }

            for (int i = 0; i < _rows.Count && i < _quests.Count; i++)
            {
                if (click && Over(_rows[i].Back.rectTransform, p)) Select(i);
            }
        }

        /// <summary>Ouvre le journal sur son dernier onglet (public pour les tests).</summary>
        public void Open()
        {
            _window.SetActive(true);
            _openedFrame = Time.frameCount;
            if (_player != null) _player.Locked = true;
            ShowTab(_tab);
        }

        /// <summary>Ouvre le journal sur un onglet : 0 principales, 1 secondaires, 2 quotidiennes (tests).</summary>
        public void Open(int tab)
        {
            _tab = Mathf.Clamp(tab, 0, 2);
            Open();
        }

        public void Close()
        {
            _window.SetActive(false);
            if (_player != null) _player.Locked = false;
        }

        private void ShowTab(int tab)
        {
            if (tab != _tab) _selected = 0;
            _tab = tab;
            for (int i = 0; i < 3; i++) _tabs[i].color = i == tab ? Color.white : new Color(0.55f, 0.55f, 0.6f);
            _quests = tab == 0 ? QuestCatalog.Main(_progress, _fishing) : tab == 1 ? QuestCatalog.Side(_progress, _fishing) : QuestCatalog.Daily();
            // Les quêtes en cours d'abord, les terminées ensuite.
            _quests.Sort((a, b) => a.Done.CompareTo(b.Done));
            _empty.text = Empty[tab];
            _empty.gameObject.SetActive(_quests.Count == 0);
            Select(Mathf.Clamp(_selected, 0, Mathf.Max(0, _quests.Count - 1)));
        }

        private void Select(int index)
        {
            _selected = index;
            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                bool shown = i < _quests.Count;
                row.Back.gameObject.SetActive(shown);
                if (!shown) continue;
                QuestEntry quest = _quests[i];
                row.Title.text = quest.Title;
                row.Title.color = i == index ? Gold : quest.Done ? Muted : Text;
                row.Gem.color = quest.Done ? Gold : Blue;
                row.Back.color = i == index ? Color.white : new Color(0.75f, 0.78f, 0.82f);
                row.Back.rectTransform.localScale = Vector3.one * (i == index ? 1.03f : 1f);
            }

            _detail.SetActive(_quests.Count > 0);
            if (_quests.Count == 0) return;
            QuestEntry q = _quests[Mathf.Clamp(index, 0, _quests.Count - 1)];
            _title.text = q.Done ? $"{q.Title}   •   Terminée" : q.Title;
            _title.color = q.Done ? Gold : Text;
            _where.text = q.Where;
            _picture.sprite = q.Picture;
            _picture.color = q.PictureTint;
            _picture.enabled = q.Picture != null;
            for (int i = 0; i < _steps.Count; i++)
            {
                StepView view = _steps[i];
                bool shown = i < q.Steps.Count;
                view.Gem.gameObject.SetActive(shown);
                view.Label.gameObject.SetActive(shown);
                if (!shown) continue;
                QuestStep step = q.Steps[i];
                view.Gem.color = step.Done ? Gold : step.Reached ? Blue : Grey;
                view.Label.text = step.Reached ? step.Text : "???";
                view.Label.color = step.Done ? Gold : step.Reached ? Text : Muted;
            }

            _note.text = q.Note ?? string.Empty;
            for (int i = 0; i < _pearls.Count; i++)
            {
                _pearls[i].gameObject.SetActive(q.Pearls);
                if (!q.Pearls || _progress == null) continue;
                string zone = PearlLore.Zones[i];
                PearlState state = _progress.Pearls.StateOf(zone);
                Color color = PearlLore.ColorOf(zone);
                _pearls[i].color = state == PearlState.Threaded ? color
                    : state == PearlState.Found ? new Color(color.r, color.g, color.b, 0.55f)
                    : new Color(0.12f, 0.14f, 0.30f, 0.6f);
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
            Sprite gem = _skin != null ? _skin.Gem : null;
            Color cardTint = _card != null ? Color.white : new Color(0.10f, 0.16f, 0.24f);

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

            // La fenêtre, comme celle du sac : titre, onglets, liste à gauche, fiche à droite, aide.
            Image window = UiFactory.Sliced(root, "Fenêtre du journal", _skin != null ? _skin.Panel : null, _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            _window = window.gameObject;
            _windowRect = window.rectTransform;
            UiFactory.Center(_windowRect, new Vector2(0f, 10f), new Vector2(WindowWidth, WindowHeight));
            _windowRect.localScale = Vector3.one * 1.08f;
            UiFactory.TitlePlaque(_windowRect, _skin, "Journal de quêtes", 30, Vector2.zero, new Vector2(460f, 64f), out RectTransform titlePlaque);
            UiFactory.Place(titlePlaque, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 64f));
            for (int i = 0; i < 3; i++)
            {
                UiFactory.TitlePlaque(_windowRect, _skin, TabNames[i], 24, new Vector2(-736f + i * 202f, 345f), new Vector2(196f, 48f), out RectTransform tab);
                _tabs[i] = tab.GetComponent<Image>();
            }

            // La liste des quêtes de l'onglet.
            for (int i = 0; i < MaxRows; i++)
            {
                var row = new Row();
                row.Back = UiFactory.Sliced(_windowRect, $"Quête {i + 1}", _card, cardTint);
                UiFactory.Center(row.Back.rectTransform, new Vector2(-560f, 260f - i * 72f), new Vector2(500f, 64f));
                row.Gem = UiFactory.Plain(row.Back.rectTransform, "Pierre", Color.white);
                row.Gem.sprite = gem;
                UiFactory.Center(row.Gem.rectTransform, new Vector2(-212f, 0f), new Vector2(30f, 30f));
                row.Title = UiFactory.Label(row.Back.rectTransform, "Titre", 24, TextAnchor.MiddleLeft, Text);
                UiFactory.Bold(row.Title);
                UiFactory.Center(row.Title.rectTransform, new Vector2(26f, 0f), new Vector2(420f, 40f));
                _rows.Add(row);
            }

            _empty = UiFactory.Label(_windowRect, "Aucune quête", 26, TextAnchor.MiddleCenter, Muted);
            _empty.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Center(_empty.rectTransform, new Vector2(0f, 0f), new Vector2(1200f, 80f));

            // La fiche de la quête choisie.
            Image detail = UiFactory.Sliced(_windowRect, "Fiche de la quête", _card, cardTint);
            _detail = detail.gameObject;
            RectTransform d = detail.rectTransform;
            UiFactory.Center(d, new Vector2(270f, -10f), new Vector2(1060f, 610f));
            _picture = UiFactory.Plain(d, "Image", Color.white);
            _picture.preserveAspect = true;
            UiFactory.Center(_picture.rectTransform, new Vector2(-400f, 170f), new Vector2(170f, 170f));
            _title = UiFactory.Label(d, "Titre", 34, TextAnchor.MiddleLeft, Text);
            UiFactory.Bold(_title);
            UiFactory.Center(_title.rectTransform, new Vector2(100f, 220f), new Vector2(760f, 46f));
            _where = UiFactory.Label(d, "Où", 22, TextAnchor.MiddleLeft, Muted);
            UiFactory.Center(_where.rectTransform, new Vector2(100f, 178f), new Vector2(760f, 30f));
            for (int i = 0; i < 6; i++)
            {
                var view = new StepView();
                float y = 70f - i * 62f;
                view.Gem = UiFactory.Plain(d, $"Étape {i + 1}", Color.white);
                view.Gem.sprite = gem;
                UiFactory.Center(view.Gem.rectTransform, new Vector2(-440f, y), new Vector2(34f, 34f));
                view.Label = UiFactory.Label(d, $"Texte de l'étape {i + 1}", 26, TextAnchor.MiddleLeft, Text);
                UiFactory.Center(view.Label.rectTransform, new Vector2(-40f, y), new Vector2(760f, 40f));
                _steps.Add(view);
            }

            // La rangée des perles de Chiyo, au bout de l'étape des perles.
            for (int i = 0; i < PearlLore.Count; i++)
            {
                Image pearl = UiFactory.Plain(d, $"Perle {i + 1}", Color.white);
                pearl.sprite = PearlSprite.Get();
                UiFactory.Center(pearl.rectTransform, new Vector2(150f + i * 40f, 70f - 62f), new Vector2(34f, 34f));
                _pearls.Add(pearl);
            }

            _note = UiFactory.Label(d, "Note", 23, TextAnchor.MiddleLeft, Gold);
            _note.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Center(_note.rectTransform, new Vector2(0f, -250f), new Vector2(940f, 60f));

            Text help = UiFactory.Label(_windowRect, "Aide", 25, TextAnchor.MiddleCenter, Muted);
            help.text = $"Gauche / Droite : onglet   •   Haut / Bas ou clic : quête   •   {KeyNames.Of(Key.J, "J")} ou Échap : fermer";
            UiFactory.Place(help.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(-30f, 30f));
            _window.SetActive(false);
        }
    }
}
