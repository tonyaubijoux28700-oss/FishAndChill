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
    /// (une case par place : dessin, nom, taille, rareté), habillées avec la charte pixel art.
    /// Devant le comptoir, E parle au marchand à la place. Quand le sac est plein après une prise, la fenêtre
    /// s'ouvre seule pour montrer les places 1, 2, 3. Affiche aussi les pièces en haut à droite.
    /// </summary>
    public sealed class InventoryHud : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.66f, 0.72f, 0.78f);
        private static readonly Color EmptyTint = new Color(0.62f, 0.66f, 0.72f);

        private const float SlotWidth = 168f;
        private const float SlotHeight = 214f;
        private const float SlotGap = 12f;
        private const float WindowPadding = 22f;
        private const float IconSize = 112f;

        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private FishingController _fishing;
        [SerializeField] private PlayerMovement _player;

        [SerializeField, Tooltip("Pour laisser E au marchand quand on est devant son comptoir.")]
        private ShopMenu _shopMenu;

        [SerializeField, Tooltip("Icône du sac (pixel art).")]
        private Sprite _bagIcon;

        [SerializeField, Tooltip("Charte pixel art de l'interface.")]
        private UiSkin _skin;

        [SerializeField, Tooltip("L'aquarium : devant le bassin, E dépose le sac (au lieu d'ouvrir la fenêtre).")]
        private AquariumBuilding _aquarium;

        [SerializeField, Min(1f)] private float _toastSeconds = 5f;

        private sealed class SlotView
        {
            public Image Frame;
            public Image Backdrop;
            public Image Fish;
            public Image RarityStrip;
            public Image NumberPlaque;
            public Text Name;
            public Text Details;
        }

        private readonly List<SlotView> _slots = new List<SlotView>();
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
            if (_aquarium != null)
            {
                _aquarium.Deposited += OnDeposited;
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

            if (_aquarium != null)
            {
                _aquarium.Deposited -= OnDeposited;
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

            // Devant le bassin, E dépose les poissons.
            if (_aquarium != null && _aquarium.PlayerAtTank)
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

            _windowTitle.text = Choosing ? "Quel poisson relâcher ?" : $"Mon sac  {_progress.Bag.Count}/{_progress.Bag.Capacity}";
            _windowHelp.text = Choosing ? $"1 à {_progress.Bag.Capacity} : relâcher   •   Espace : relâcher le nouveau" : "E ou Échap : fermer";
            _windowHelp.color = Choosing ? Gold : Muted;
        }

        private void AnimateSlots()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
            Inventory bag = _progress.Bag;
            for (int i = 0; i < _slots.Count; i++)
            {
                SlotView slot = _slots[i];
                Color baseTint = i < bag.Count ? Color.white : EmptyTint;
                // En mode choix, les cases pleines « clignotent » en doré pour attirer l'œil sur leur numéro.
                slot.Frame.color = Choosing && i < bag.Count ? Color.Lerp(Color.white, Gold, pulse * 0.8f) : baseTint;
                slot.NumberPlaque.rectTransform.localScale = Vector3.one * (Choosing ? 1f + 0.12f * pulse : 1f);
            }

            // Petit rebond de l'icône quand le sac est plein.
            float scale = bag.IsFull ? 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 4f) : 1f;
            _iconRect.localScale = Vector3.one * scale;
        }

        private void UpdatePrompt()
        {
            if (_aquarium != null && _aquarium.CanDeposit && !IsOpen && _toastTimer <= 0f)
            {
                _prompt.gameObject.SetActive(true);
                _prompt.text = _progress.Bag.Count > 0 ? "E : déposer tes poissons dans le bassin" : "Ton sac est vide : va pêcher !";
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

            for (int i = 0; i < _slots.Count; i++)
            {
                SlotView slot = _slots[i];
                bool filled = i < bag.Count;
                slot.Backdrop.gameObject.SetActive(filled);
                slot.RarityStrip.gameObject.SetActive(filled);
                if (!filled)
                {
                    slot.Name.text = "Place libre";
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
                slot.RarityStrip.color = rarity;
                slot.Details.text = $"{fish.SizeCm:0} cm  •  <color=#{ColorUtility.ToHtmlStringRGB(rarity)}>{fish.Species.Rarity.DisplayName()}</color>";
            }

            _coins.text = $"{_progress.Wallet.Coins} pièces";
        }

        private void OnDeposited(DepositResult result)
        {
            var text = new StringBuilder();
            if (result.IsEmpty)
            {
                text.Append(_progress.Bag.Count == 0
                    ? "Ton sac est vide : va pêcher !"
                    : "Rien de nouveau pour le bassin :\nvends tes doublons au marchand.");
            }
            else
            {
                foreach (CaughtFish fish in result.NewSpecies)
                {
                    text.Append($"<color=#FFD54D>Nouvelle espèce : {fish.Species.CommonName}</color>\n");
                }

                foreach (CaughtFish fish in result.Upgraded)
                {
                    text.Append($"Record ! {fish.Species.CommonName} de {fish.SizeCm:0} cm exposé\n");
                }

                if (result.LeftInBag > 0)
                {
                    text.Append($"{result.LeftInBag} poisson(s) restent dans le sac pour le marchand\n");
                }
            }

            _toastText.text = text.ToString().TrimEnd('\n');
            _toastTimer = _toastSeconds;
            _toast.SetActive(true);
        }

        // ---------- Construction ----------

        private Sprite SkinSprite(System.Func<UiSkin, Sprite> pick) => _skin != null ? pick(_skin) : null;

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
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

            // Icône du sac posée sur une case creusée, compteur sur une plaque dorée, touche E.
            Image iconBase = UiFactory.Sliced(root, "Sac", SkinSprite(s => s.Slot), _skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
            _iconRect = iconBase.rectTransform;
            UiFactory.Place(_iconRect, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(24f + IconSize * 0.5f, 24f + IconSize * 0.5f), new Vector2(IconSize, IconSize));

            Image icon = UiFactory.Plain(_iconRect, "Icône", Color.white);
            icon.sprite = _bagIcon;
            icon.preserveAspect = true;
            UiFactory.Center(icon.rectTransform, new Vector2(0f, 6f), new Vector2(IconSize - 26f, IconSize - 26f));

            _badge = UiFactory.TitlePlaque(_iconRect, _skin, "0/3", 22, new Vector2(30f, -46f), new Vector2(64f, 34f), out _);

            Text key = UiFactory.Label(_iconRect, "Touche", 24, TextAnchor.UpperLeft, Gold);
            key.text = "E";
            UiFactory.Bold(key);
            UiFactory.Place(key.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -6f), new Vector2(30f, 28f));

            // Fenêtre d'inventaire, juste au-dessus de l'icône (sans recouvrir le centre de l'écran).
            Image window = UiFactory.Sliced(root, "Fenêtre du sac", SkinSprite(s => s.Panel), _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            _window = window.gameObject;
            _windowRect = window.rectTransform;
            UiFactory.Place(_windowRect, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(24f, 24f + IconSize + 30f), Vector2.one * 100f);

            _windowTitle = UiFactory.TitlePlaque(_windowRect, _skin, string.Empty, 26, Vector2.zero, new Vector2(360f, 54f), out RectTransform titlePlaque);
            UiFactory.Place(titlePlaque, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(360f, 54f));

            _windowHelp = UiFactory.Label(_windowRect, "Aide", 20, TextAnchor.MiddleCenter, Muted);
            UiFactory.Place(_windowHelp.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(-30f, 28f));

            // Pièces : en haut à droite, sur une plaque.
            _coins = UiFactory.TitlePlaque(root, _skin, "0 pièces", 26, Vector2.zero, new Vector2(220f, 52f), out RectTransform coinsPlaque);
            UiFactory.Place(coinsPlaque, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(220f, 52f));

            // Invite de dépôt (futur aquarium).
            _prompt = UiFactory.Label(root, "Invite", 32, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Bold(_prompt);
            UiFactory.Place(_prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(900f, 50f));
            _prompt.gameObject.SetActive(false);

            Image toast = UiFactory.Sliced(root, "Résumé", SkinSprite(s => s.Panel), _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            _toast = toast.gameObject;
            // En bas de l'écran : dans l'aquarium, le bassin (en haut) reste visible pendant le résumé.
            UiFactory.Place(toast.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(860f, 230f));
            _toastText = UiFactory.Label(toast.rectTransform, "Texte", 24, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Stretch(_toastText.rectTransform, 24f);
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
            _windowRect.sizeDelta = new Vector2(WindowPadding * 2f + capacity * SlotWidth + (capacity - 1) * SlotGap, SlotHeight + 110f);

            for (int i = 0; i < capacity; i++)
            {
                Image frame = UiFactory.Sliced(_windowRect, $"Place {i + 1}", SkinSprite(s => s.Slot), _skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
                UiFactory.Place(frame.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(WindowPadding + i * (SlotWidth + SlotGap), 52f), new Vector2(SlotWidth, SlotHeight));

                // Fond d'eau avec le poisson.
                Image backdrop = UiFactory.Sliced(frame.rectTransform, "Fond d'eau", SkinSprite(s => s.WaterBackdrop), _skin != null ? Color.white : new Color(0.2f, 0.55f, 0.65f));
                UiFactory.Place(backdrop.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(SlotWidth - 20f, 96f));
                Image fish = UiFactory.Plain(backdrop.rectTransform, "Poisson", Color.white);
                fish.preserveAspect = true;
                UiFactory.Center(fish.rectTransform, Vector2.zero, new Vector2(SlotWidth - 34f, 74f));

                Text name = UiFactory.Label(frame.rectTransform, "Nom", 20, TextAnchor.MiddleCenter, Color.white);
                UiFactory.Bold(name);
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                UiFactory.Place(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(SlotWidth - 16f, 50f));

                Text details = UiFactory.Label(frame.rectTransform, "Détails", 15, TextAnchor.MiddleCenter, Color.white);
                details.horizontalOverflow = HorizontalWrapMode.Overflow;
                UiFactory.Place(details.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(SlotWidth - 10f, 28f));

                // Liseré de rareté en bas de la case.
                Image strip = UiFactory.Plain(frame.rectTransform, "Rareté", Color.white);
                UiFactory.Place(strip.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(-24f, 6f));

                // Numéro de la place sur une petite plaque dorée.
                Text number = UiFactory.TitlePlaque(frame.rectTransform, _skin, (i + 1).ToString(), 22, Vector2.zero, new Vector2(38f, 34f), out RectTransform plaque);
                UiFactory.Place(plaque, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(10f, -8f), new Vector2(38f, 34f));
                number.fontSize = UiFactory.Size(22);

                _slots.Add(new SlotView
                {
                    Frame = frame,
                    Backdrop = backdrop,
                    Fish = fish,
                    RarityStrip = strip,
                    NumberPlaque = plaque.GetComponent<Image>(),
                    Name = name,
                    Details = details,
                });
            }
        }
    }
}
