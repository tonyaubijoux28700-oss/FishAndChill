using System.Collections.Generic;
using System.Text;
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
    /// Le sac : une icône en bas à gauche (avec le compteur « 2/3 »), et une fenêtre d'inventaire ouverte avec E
    /// (une case par place : dessin, nom, taille, rareté). Devant le comptoir, E parle au marchand à la place.
    /// Quand le sac est plein après une prise, la fenêtre s'ouvre seule pour montrer les places 1, 2, 3.
    /// Affiche aussi les pièces en haut à droite.
    /// </summary>
    public sealed class InventoryHud : MonoBehaviour
    {
        // Opaques : avec une bordure (Outline), un fond translucide laisse transparaître la couleur du bord.
        private static readonly Color PanelColor = new Color(0.07f, 0.10f, 0.14f, 1f);
        private static readonly Color SlotColor = new Color(0.14f, 0.26f, 0.33f, 1f);
        private static readonly Color EmptySlotColor = new Color(0.10f, 0.16f, 0.21f, 1f);
        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.62f, 0.68f, 0.74f);

        private const float SlotWidth = 172f;
        private const float SlotHeight = 196f;
        private const float SlotGap = 12f;
        private const float IconSize = 96f;

        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private FishingController _fishing;
        [SerializeField] private PlayerMovement _player;

        [SerializeField, Tooltip("Pour laisser E au marchand quand on est devant son comptoir.")]
        private ShopMenu _shopMenu;

        [SerializeField, Tooltip("Icône du sac (pixel art).")]
        private Sprite _bagIcon;

        [SerializeField, Tooltip("Optionnel : bâtiment de dépôt (futur aquarium).")]
        private AquariumStation _station;

        [SerializeField, Min(1f)] private float _toastSeconds = 5f;

        private sealed class SlotView
        {
            public Image Frame;
            public Outline Border;
            public Image Fish;
            public Text Name;
            public Text Details;
            public Text Number;
        }

        private readonly List<SlotView> _slots = new List<SlotView>();
        private Font _font;
        private RectTransform _iconRect;
        private Text _badge;
        private GameObject _window;
        private RectTransform _windowRect;
        private Text _windowTitle;
        private Text _windowHelp;
        private Text _coins;
        private Text _prompt;
        private GameObject _toast;
        private Text _toastText;
        private float _toastTimer;
        private int _builtCapacity = -1;
        private bool _openedForChoice;

        /// <summary>La fenêtre d'inventaire est-elle ouverte ?</summary>
        public bool IsOpen => _window != null && _window.activeSelf;

        private bool Choosing => _fishing != null && _fishing.State == FishingState.BagFull;

        private void Start()
        {
            if (_progress == null)
            {
                enabled = false;
                return;
            }

            _progress.EnsureCreated();
            BuildUi();
            _progress.Changed += Refresh;
            if (_station != null)
            {
                _station.Deposited += OnDeposited;
            }

            Refresh();
            Close();
        }

        private void OnDestroy()
        {
            if (_progress != null)
            {
                _progress.Changed -= Refresh;
            }

            if (_station != null)
            {
                _station.Deposited -= OnDeposited;
            }
        }

        private void Update()
        {
            UpdateChoiceMode();

            if (!Choosing)
            {
                bool closeKey = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
                if (IsOpen && (closeKey || InteractInput.PressedThisFrame()))
                {
                    Close();
                }
                else if (!IsOpen && InteractInput.PressedThisFrame() && CanOpenNow())
                {
                    Open();
                }
            }

            AnimateSlots();
            UpdatePrompt();
            UpdateToast();
        }

        /// <summary>Ouvre la fenêtre du sac (le pêcheur s'arrête).</summary>
        public void Open()
        {
            _window.SetActive(true);
            _openedForChoice = false;
            if (_player != null)
            {
                _player.Locked = true;
            }

            Refresh();
        }

        public void Close()
        {
            _window.SetActive(false);
            if (_player != null && !_openedForChoice)
            {
                _player.Locked = false;
            }

            _openedForChoice = false;
        }

        /// <summary>E ouvre le sac seulement hors pêche, hors menu du marchand et loin de son comptoir.</summary>
        private bool CanOpenNow()
        {
            if (_shopMenu != null && (_shopMenu.IsOpen || _shopMenu.PlayerAtCounter))
            {
                return false;
            }

            if (_fishing != null && _fishing.State != FishingState.Idle && _fishing.State != FishingState.Result)
            {
                return false;
            }

            return _player == null || !_player.Locked;
        }

        /// <summary>Sac plein après une prise : la fenêtre s'ouvre seule pour montrer les numéros des places.</summary>
        private void UpdateChoiceMode()
        {
            if (Choosing && !IsOpen)
            {
                _window.SetActive(true);
                _openedForChoice = true;
                Refresh();
            }
            else if (!Choosing && _openedForChoice)
            {
                _window.SetActive(false);
                _openedForChoice = false;
            }

            _windowTitle.text = Choosing ? "Sac plein : quel poisson relâcher ?" : $"Mon sac  {_progress.Bag.Count}/{_progress.Bag.Capacity}";
            _windowHelp.text = Choosing ? "1, 2, 3 : relâcher   •   Espace : relâcher le nouveau" : "E ou Échap : fermer";
            _windowHelp.color = Choosing ? Gold : Muted;
        }

        private void AnimateSlots()
        {
            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 6f);
            foreach (SlotView slot in _slots)
            {
                slot.Border.effectColor = Choosing ? new Color(Gold.r, Gold.g, Gold.b, pulse) : new Color(1f, 1f, 1f, 0.2f);
                slot.Number.color = Choosing ? Gold : Muted;
            }

            // Petit rebond de l'icône quand le sac est plein.
            float scale = _progress.Bag.IsFull ? 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 4f) : 1f;
            _iconRect.localScale = Vector3.one * scale;
        }

        private void UpdatePrompt()
        {
            if (_station != null && _station.PlayerInRange)
            {
                _prompt.gameObject.SetActive(true);
                _prompt.text = _progress.Bag.Count > 0 ? "E : déposer tes poissons à l'aquarium" : "Aquarium — ton sac est vide";
            }
            else
            {
                _prompt.gameObject.SetActive(false);
            }
        }

        private void UpdateToast()
        {
            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.unscaledDeltaTime;
                _toast.SetActive(_toastTimer > 0f);
            }
        }

        // ---------- Rafraîchissement ----------

        private void Refresh()
        {
            Inventory bag = _progress.Bag;
            if (_builtCapacity != bag.Capacity)
            {
                BuildSlots(bag.Capacity);
            }

            _badge.text = $"{bag.Count}/{bag.Capacity}";
            _badge.color = bag.IsFull ? Gold : Color.white;

            for (int i = 0; i < _slots.Count; i++)
            {
                SlotView slot = _slots[i];
                bool filled = i < bag.Count;
                slot.Frame.color = filled ? SlotColor : EmptySlotColor;
                slot.Fish.gameObject.SetActive(filled);
                if (!filled)
                {
                    slot.Name.text = "(vide)";
                    slot.Name.color = Muted;
                    slot.Details.text = string.Empty;
                    continue;
                }

                CaughtFish fish = bag.Fish[i];
                slot.Fish.sprite = fish.Species.Sprite;
                slot.Fish.color = fish.Species.Sprite != null ? Color.white : fish.Species.PlaceholderColor;
                slot.Name.text = fish.Species.CommonName;
                slot.Name.color = Color.white;
                Color rarity = fish.Species.Rarity.DisplayColor();
                slot.Details.text = $"{fish.SizeCm:0} cm\n<color=#{ColorUtility.ToHtmlStringRGB(rarity)}>{fish.Species.Rarity.DisplayName()}</color>";
            }

            _coins.text = $"Pièces : {_progress.Wallet.Coins}";
        }

        private void OnDeposited(DepositResult result)
        {
            var text = new StringBuilder();
            if (result.IsEmpty)
            {
                text.Append("Ton sac est vide : va pêcher !");
            }
            else
            {
                if (result.NewSpecies.Count > 0)
                {
                    text.Append($"<color=#FFD54D>+{result.NewSpecies.Count} nouvelle(s) espèce(s) dans l'aquarium</color>\n");
                }

                if (result.Sold.Count > 0)
                {
                    text.Append($"{result.Sold.Count} doublon(s) vendu(s) : <color=#FFD54D>+{result.CoinsEarned} pièces</color>\n");
                }

                text.Append($"Aquarium : {_progress.Aquarium.SpeciesCount} espèce(s)");
            }

            _toastText.text = text.ToString().TrimEnd('\n');
            _toastTimer = _toastSeconds;
            _toast.SetActive(true);
        }

        // ---------- Construction ----------

        private void BuildUi()
        {
            var canvasObject = new GameObject("Interface du sac", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 8;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Icône du sac en bas à gauche, avec le compteur et la touche.
            Image icon = AddImage(root, "Icône du sac", Color.white);
            icon.sprite = _bagIcon;
            icon.preserveAspect = true;
            _iconRect = icon.rectTransform;
            _iconRect.anchorMin = _iconRect.anchorMax = Vector2.zero;
            _iconRect.pivot = new Vector2(0.5f, 0.5f);
            _iconRect.anchoredPosition = new Vector2(24f + IconSize * 0.5f, 24f + IconSize * 0.5f);
            _iconRect.sizeDelta = new Vector2(IconSize, IconSize);
            if (_bagIcon == null)
            {
                icon.color = new Color(0.62f, 0.40f, 0.22f);
            }

            // Compteur dans le coin bas-droit de l'icône, touche E dans le coin haut-gauche.
            _badge = AddText(_iconRect, "Compteur", 24, TextAnchor.LowerRight);
            _badge.fontStyle = FontStyle.Bold;
            Place(_badge.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-2f, 0f), new Vector2(80f, 30f), new Vector2(1f, 0f));

            Text key = AddText(_iconRect, "Touche", 22, TextAnchor.UpperLeft);
            key.text = "E";
            key.fontStyle = FontStyle.Bold;
            key.color = Gold;
            Place(key.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(4f, -2f), new Vector2(30f, 26f), new Vector2(0f, 1f));

            // Fenêtre d'inventaire, juste au-dessus de l'icône (sans recouvrir le centre de l'écran).
            Image window = AddImage(root, "Fenêtre du sac", PanelColor);
            _window = window.gameObject;
            _windowRect = window.rectTransform;
            _windowRect.anchorMin = _windowRect.anchorMax = Vector2.zero;
            _windowRect.pivot = Vector2.zero;
            _windowRect.anchoredPosition = new Vector2(24f, 24f + IconSize + 14f);
            Outline windowBorder = _window.AddComponent<Outline>();
            windowBorder.effectColor = Gold;
            windowBorder.effectDistance = new Vector2(3f, -3f);

            _windowTitle = AddText(_windowRect, "Titre", 30, TextAnchor.UpperLeft);
            _windowTitle.fontStyle = FontStyle.Bold;
            _windowTitle.color = Gold;
            Place(_windowTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -10f), new Vector2(-32f, 40f), new Vector2(0.5f, 1f));

            _windowHelp = AddText(_windowRect, "Aide", 20, TextAnchor.LowerCenter);
            Place(_windowHelp.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f), new Vector2(-24f, 28f), new Vector2(0.5f, 0f));

            // Pièces : en haut à droite.
            _coins = AddText(root, "Pièces", 32, TextAnchor.UpperRight);
            _coins.fontStyle = FontStyle.Bold;
            _coins.color = Gold;
            Place(_coins.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -28f), new Vector2(400f, 44f), new Vector2(1f, 1f));

            // Invite de dépôt (futur aquarium).
            _prompt = AddText(root, "Invite", 32, TextAnchor.MiddleCenter);
            _prompt.fontStyle = FontStyle.Bold;
            Place(_prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(900f, 50f));
            _prompt.gameObject.SetActive(false);

            Image toast = AddImage(root, "Résumé", PanelColor);
            _toast = toast.gameObject;
            Place(toast.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(820f, 170f), new Vector2(0.5f, 1f));
            Outline toastBorder = _toast.AddComponent<Outline>();
            toastBorder.effectColor = Gold;
            toastBorder.effectDistance = new Vector2(3f, -3f);
            _toastText = AddText(toast.rectTransform, "Texte", 28, TextAnchor.MiddleCenter);
            _toastText.rectTransform.anchorMin = Vector2.zero;
            _toastText.rectTransform.anchorMax = Vector2.one;
            _toastText.rectTransform.offsetMin = new Vector2(16f, 8f);
            _toastText.rectTransform.offsetMax = new Vector2(-16f, -8f);
            _toast.SetActive(false);
        }

        private void BuildSlots(int capacity)
        {
            foreach (SlotView slot in _slots)
            {
                Destroy(slot.Frame.gameObject);
            }

            _slots.Clear();
            _builtCapacity = capacity;
            _windowRect.sizeDelta = new Vector2(SlotGap + capacity * (SlotWidth + SlotGap), SlotHeight + 100f);

            for (int i = 0; i < capacity; i++)
            {
                Image frame = AddImage(_windowRect, $"Place {i + 1}", SlotColor);
                Place(frame.rectTransform, Vector2.zero, Vector2.zero, new Vector2(SlotGap + i * (SlotWidth + SlotGap), 42f), new Vector2(SlotWidth, SlotHeight), Vector2.zero);
                Outline border = frame.gameObject.AddComponent<Outline>();
                border.effectDistance = new Vector2(2f, -2f);

                Image fish = AddImage(frame.rectTransform, "Poisson", Color.white);
                fish.preserveAspect = true;
                Place(fish.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(SlotWidth - 20f, 86f), new Vector2(0.5f, 1f));

                Text name = AddText(frame.rectTransform, "Nom", 20, TextAnchor.MiddleCenter);
                name.fontStyle = FontStyle.Bold;
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                Place(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(SlotWidth - 12f, 48f), new Vector2(0.5f, 0f));

                // Taille et rareté sur deux lignes : rien ne déborde sur la case voisine.
                Text details = AddText(frame.rectTransform, "Détails", 18, TextAnchor.MiddleCenter);
                details.supportRichText = true;
                details.horizontalOverflow = HorizontalWrapMode.Wrap;
                details.lineSpacing = 0.9f;
                Place(details.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(SlotWidth - 8f, 44f), new Vector2(0.5f, 0f));

                Text number = AddText(frame.rectTransform, "Numéro", 24, TextAnchor.UpperLeft);
                number.text = (i + 1).ToString();
                number.fontStyle = FontStyle.Bold;
                Place(number.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -4f), new Vector2(30f, 28f), new Vector2(0f, 1f));

                _slots.Add(new SlotView { Frame = frame, Border = border, Fish = fish, Name = name, Details = details, Number = number });
            }
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            if (pivot.HasValue)
            {
                rect.pivot = pivot.Value;
            }

            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Image AddImage(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private Text AddText(RectTransform parent, string name, int size, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            return text;
        }
    }
}
