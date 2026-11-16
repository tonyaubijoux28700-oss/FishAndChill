using System.Collections.Generic;
using System.Text;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Gear;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Le sac : une icône en bas à gauche (avec le compteur « 2/3 »), et une grande fenêtre d'inventaire ouverte avec E :
    /// à gauche le matériel utilisé (canne, hameçon, appât, sac : icône, nom, effet, niveau), à droite les poissons du sac
    /// (une case par place : dessin à l'échelle, nom, taille, rareté ; les places pas encore achetées sont verrouillées).
    /// Devant le comptoir, E parle au marchand à la place. Quand le sac est plein après une prise, la fenêtre
    /// s'ouvre seule pour montrer les places 1, 2, 3. Affiche aussi les pièces en haut à droite.
    /// </summary>
    public sealed class InventoryHud : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.66f, 0.72f, 0.78f);
        private static readonly Color EmptyTint = new Color(0.62f, 0.66f, 0.72f);

        private const float SlotWidth = 270f;
        private const float SlotHeight = 250f;
        private const float SlotGap = 16f;
        private const float IconSize = 112f;
        private const int SlotColumns = 3;
        private const int ShownSlots = 6;
        private const float WindowWidth = 1460f;
        private const float WindowHeight = 760f;
        private static readonly Color Ink = new Color(0.90f, 0.93f, 0.96f);

        private static readonly GearCategory[] GearOrder = { GearCategory.Rod, GearCategory.Hook, GearCategory.Bait, GearCategory.Bag };
        private static readonly string[] GearLabels = { "CANNE", "HAMEÇON", "APPÂT", "SAC" };

        private sealed class GearView
        {
            public Image Icon;
            public Text Name;
            public Text Effect;
            public readonly List<Image> Levels = new List<Image>();
        }

        private readonly List<GearView> _gear = new List<GearView>();
        private Text _fishTitle;

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

        [SerializeField, Tooltip("Voyage : sur le ponton, E ouvre la carte du monde au lieu du sac.")]
        private ZoneTravel _travel;

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
            public GameObject Lock;
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

            // Sur le ponton, E ouvre la carte du monde.
            if (_travel != null && _travel.PlayerAtDock)
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

            _windowTitle.text = Choosing ? "Quel poisson relâcher ?" : "Mon inventaire";
            // Pendant le choix, la carte de la prise est à gauche : la fenêtre se fait plus petite, à droite.
            _windowRect.anchoredPosition = Choosing ? new Vector2(470f, 10f) : new Vector2(0f, 10f);
            _windowRect.localScale = Vector3.one * (Choosing ? 0.6f : 1f);
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
            int shown = Mathf.Max(ShownSlots, bag.Capacity);
            if (_builtCapacity != shown)
            {
                BuildSlots(shown);
            }

            _badge.text = $"{bag.Count}/{bag.Capacity}";
            _fishTitle.text = $"Mes poissons   {bag.Count} / {bag.Capacity}";
            RefreshGear();

            for (int i = 0; i < _slots.Count; i++)
            {
                SlotView slot = _slots[i];
                bool locked = i >= bag.Capacity;
                bool filled = i < bag.Count;
                slot.Lock.SetActive(locked);
                slot.NumberPlaque.gameObject.SetActive(!locked);
                slot.Backdrop.gameObject.SetActive(filled);
                slot.RarityStrip.gameObject.SetActive(filled);
                if (!filled)
                {
                    slot.Name.text = locked ? string.Empty : "Place libre";
                    slot.Name.color = Muted;
                    slot.Details.text = string.Empty;
                    continue;
                }

                CaughtFish fish = bag.Fish[i];
                slot.Fish.sprite = fish.Species.Sprite;
                slot.Fish.color = fish.Species.Sprite != null ? Color.white : fish.Species.PlaceholderColor;
                Vector2 pixels = fish.Species.Sprite != null ? fish.Species.Sprite.rect.size : new Vector2(2f, 1f);
                slot.Fish.rectTransform.sizeDelta = FishDisplayScale.Fit(new Vector2(SlotWidth - 40f, 100f), pixels, fish.SizeCm);
                slot.Name.text = fish.Species.CommonName;
                slot.Name.color = Color.white;
                Color rarity = fish.Species.Rarity.DisplayColor();
                slot.RarityStrip.color = rarity;
                slot.Details.text = $"{fish.SizeCm:0} cm  •  <color=#{ColorUtility.ToHtmlStringRGB(rarity)}>{fish.Species.Rarity.DisplayName()}</color>";
            }

            _coins.text = $"{_progress.Wallet.Coins} pièces";
        }

        /// <summary>Le matériel utilisé : icône, nom, effet et niveau (pierres allumées) de chaque famille.</summary>
        private void RefreshGear()
        {
            GearCatalog catalog = _progress.GearCatalog;
            for (int i = 0; i < _gear.Count; i++)
            {
                GearView view = _gear[i];
                GearCategory category = GearOrder[i];
                GearLine line = catalog != null ? catalog.Line(category) : null;
                int level = _progress.Gear.Level(category);
                GearLevel item = line != null ? line.At(level) : null;
                view.Icon.sprite = item != null && item.Icon != null ? item.Icon : line != null ? line.Icon : null;
                view.Icon.enabled = view.Icon.sprite != null;
                view.Name.text = item != null ? item.Name : "—";
                view.Effect.text = item == null ? string.Empty
                    : level == 0 && category != GearCategory.Bag ? "Matériel de départ" : ShopMenu.EffectText(category, item);
                int max = line != null ? line.MaxLevel : 0;
                for (int k = 0; k < view.Levels.Count; k++)
                {
                    view.Levels[k].gameObject.SetActive(k <= max);
                    view.Levels[k].color = k <= level ? Gold : new Color(0.32f, 0.36f, 0.42f);
                }
            }
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
            canvas.sortingOrder = 11; // au-dessus de la carte de prise et de son voile (10), sous le carnet (12)
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

            // Grande fenêtre d'inventaire au centre : le matériel à gauche, les poissons à droite.
            Image window = UiFactory.Sliced(root, "Fenêtre du sac", SkinSprite(s => s.Panel), _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            _window = window.gameObject;
            _windowRect = window.rectTransform;
            UiFactory.Center(_windowRect, new Vector2(0f, 10f), new Vector2(WindowWidth, WindowHeight));

            _windowTitle = UiFactory.TitlePlaque(_windowRect, _skin, string.Empty, 30, Vector2.zero, new Vector2(460f, 64f), out RectTransform titlePlaque);
            UiFactory.Place(titlePlaque, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(460f, 64f));

            _windowHelp = UiFactory.Label(_windowRect, "Aide", 21, TextAnchor.MiddleCenter, Muted);
            UiFactory.Place(_windowHelp.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(-30f, 30f));

            BuildGearPanel();

            _fishTitle = UiFactory.Label(_windowRect, "Titre poissons", 28, TextAnchor.MiddleLeft, Gold);
            UiFactory.Bold(_fishTitle);
            UiFactory.Center(_fishTitle.rectTransform, new Vector2(245f, 300f), new Vector2(860f, 40f));

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

        /// <summary>Panneau « Mon matériel » à gauche : une rangée par famille (icône, nom, effet, niveau).</summary>
        private void BuildGearPanel()
        {
            Image panel = UiFactory.Sliced(_windowRect, "Matériel", SkinSprite(s => s.Slot), _skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
            UiFactory.Center(panel.rectTransform, new Vector2(-480f, -10f), new Vector2(440f, 640f));
            RectTransform rect = panel.rectTransform;

            Text title = UiFactory.Label(rect, "Titre", 28, TextAnchor.MiddleCenter, Gold);
            UiFactory.Bold(title);
            title.text = "Mon matériel";
            UiFactory.Center(title.rectTransform, new Vector2(0f, 284f), new Vector2(400f, 40f));

            for (int i = 0; i < GearOrder.Length; i++)
            {
                float y = 175f - i * 132f;
                Image row = UiFactory.Sliced(rect, $"Matériel {i + 1}", SkinSprite(s => s.Panel), _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
                UiFactory.Center(row.rectTransform, new Vector2(0f, y), new Vector2(404f, 120f));
                RectTransform r = row.rectTransform;

                Image iconBox = UiFactory.Sliced(r, "Case", SkinSprite(s => s.WaterBackdrop), _skin != null ? Color.white : new Color(0.2f, 0.55f, 0.65f));
                UiFactory.Center(iconBox.rectTransform, new Vector2(-142f, 0f), new Vector2(92f, 92f));
                Image icon = UiFactory.Plain(iconBox.rectTransform, "Icône", Color.white);
                icon.preserveAspect = true;
                UiFactory.Center(icon.rectTransform, Vector2.zero, new Vector2(76f, 76f));

                Text label = UiFactory.Label(r, "Famille", 17, TextAnchor.MiddleLeft, Gold);
                label.text = GearLabels[i];
                UiFactory.Center(label.rectTransform, new Vector2(52f, 35f), new Vector2(270f, 22f));

                Text name = UiFactory.Label(r, "Nom", 23, TextAnchor.MiddleLeft, Ink);
                UiFactory.Bold(name);
                UiFactory.Center(name.rectTransform, new Vector2(52f, 12f), new Vector2(270f, 30f));

                Text effect = UiFactory.Label(r, "Effet", 15, TextAnchor.UpperLeft, Muted);
                effect.horizontalOverflow = HorizontalWrapMode.Wrap;
                UiFactory.Center(effect.rectTransform, new Vector2(52f, -22f), new Vector2(270f, 40f));

                var view = new GearView { Icon = icon, Name = name, Effect = effect };
                for (int k = 0; k < 4; k++)
                {
                    Image gem = UiFactory.Plain(r, $"Niveau {k}", Color.white);
                    gem.sprite = SkinSprite(s => s.Gem);
                    UiFactory.Center(gem.rectTransform, new Vector2(104f + k * 20f, 35f), new Vector2(16f, 16f));
                    view.Levels.Add(gem);
                }

                _gear.Add(view);
            }

            Text hint = UiFactory.Label(rect, "Conseil", 17, TextAnchor.MiddleCenter, Muted);
            hint.text = "Améliore ton matériel chez le marchand";
            UiFactory.Center(hint.rectTransform, new Vector2(0f, -296f), new Vector2(400f, 26f));
        }

        /// <summary>Cases des poissons (3 colonnes) : les places achetées, puis les places encore verrouillées.</summary>
        private void BuildSlots(int count)
        {
            foreach (SlotView slot in _slots)
            {
                Destroy(slot.Frame.gameObject);
            }

            _slots.Clear();
            _builtCapacity = count;
            float gridLeft = 245f - (SlotColumns * SlotWidth + (SlotColumns - 1) * SlotGap) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                int column = i % SlotColumns;
                int row = i / SlotColumns;
                var position = new Vector2(gridLeft + SlotWidth * 0.5f + column * (SlotWidth + SlotGap), 135f - row * (SlotHeight + SlotGap));
                Image frame = UiFactory.Sliced(_windowRect, $"Place {i + 1}", SkinSprite(s => s.Slot), _skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
                UiFactory.Center(frame.rectTransform, position, new Vector2(SlotWidth, SlotHeight));

                // Fond d'eau avec le poisson (à l'échelle de sa taille).
                Image backdrop = UiFactory.Sliced(frame.rectTransform, "Fond d'eau", SkinSprite(s => s.WaterBackdrop), _skin != null ? Color.white : new Color(0.2f, 0.55f, 0.65f));
                UiFactory.Place(backdrop.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(SlotWidth - 22f, 130f));
                Image fish = UiFactory.Plain(backdrop.rectTransform, "Poisson", Color.white);
                fish.preserveAspect = true;
                UiFactory.Center(fish.rectTransform, Vector2.zero, new Vector2(SlotWidth - 40f, 100f));

                Text name = UiFactory.Label(frame.rectTransform, "Nom", 22, TextAnchor.MiddleCenter, Color.white);
                UiFactory.Bold(name);
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                UiFactory.Place(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(SlotWidth - 16f, 52f));

                Text details = UiFactory.Label(frame.rectTransform, "Détails", 17, TextAnchor.MiddleCenter, Color.white);
                details.horizontalOverflow = HorizontalWrapMode.Overflow;
                UiFactory.Place(details.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(SlotWidth - 10f, 30f));

                // Liseré de rareté en bas de la case.
                Image strip = UiFactory.Plain(frame.rectTransform, "Rareté", Color.white);
                UiFactory.Place(strip.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(-24f, 6f));

                // Numéro de la place sur une petite plaque dorée.
                Text number = UiFactory.TitlePlaque(frame.rectTransform, _skin, (i + 1).ToString(), 22, Vector2.zero, new Vector2(38f, 34f), out RectTransform plaque);
                UiFactory.Place(plaque, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(10f, -8f), new Vector2(38f, 34f));
                number.fontSize = UiFactory.Size(22);

                // Place pas encore achetée : voile sombre et explication.
                Image lockVeil = UiFactory.Plain(frame.rectTransform, "Verrou", new Color(0.03f, 0.05f, 0.08f, 0.72f));
                UiFactory.Stretch(lockVeil.rectTransform, 8f);
                Text lockText = UiFactory.Label(lockVeil.rectTransform, "Texte", 19, TextAnchor.MiddleCenter, Muted);
                lockText.horizontalOverflow = HorizontalWrapMode.Wrap;
                lockText.text = "Place verrouillée\n\nUn sac plus grand\nchez le marchand";
                UiFactory.Stretch(lockText.rectTransform, 12f);

                _slots.Add(new SlotView
                {
                    Frame = frame,
                    Backdrop = backdrop,
                    Fish = fish,
                    RarityStrip = strip,
                    NumberPlaque = plaque.GetComponent<Image>(),
                    Name = name,
                    Details = details,
                    Lock = lockVeil.gameObject,
                });
            }
        }
    }
}