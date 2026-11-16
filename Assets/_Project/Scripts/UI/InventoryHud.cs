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
        private const float WindowWidth = 1700f;
        private const float WindowHeight = 760f;
        private static readonly Color Ink = new Color(0.90f, 0.93f, 0.96f);

        // Vitrine du matériel : la grande canne (dessin 96×160 affiché ×3), le fil finit au pixel (86, 62) du dessin.
        private const float ShowcaseScale = 2.6f;
        private static readonly Vector2 RodSize = new Vector2(96f, 160f) * ShowcaseScale;
        private static readonly Vector2 RodCenter = new Vector2(-152f, 30f);
        private static readonly Vector2 RodLineEnd = new Vector2(84f, 60f);

        private sealed class GearLabel
        {
            public Text Name;
            public Text Effect;
            public readonly List<Image> Levels = new List<Image>();
        }

        private Image _rodImage;
        private Image _hookImage;
        private Image _baitImage;
        private Image _bagImage;
        private GearLabel _rodLabel;
        private GearLabel _hookLabel;
        private GearLabel _baitLabel;
        private GearLabel _bagLabel;
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

        [Header("Vitrine du matériel")]
        [SerializeField, Tooltip("Fond en bois de la vitrine.")] private Sprite _showcaseBack;
        [SerializeField, Tooltip("Support en laiton sous la canne.")] private Sprite _peg;
        [SerializeField, Tooltip("Étiquette en papier (étirable).")] private Sprite _tag;
        [SerializeField, Tooltip("Étiquette en papier à coins arrondis.")] private Sprite _roundTag;
        [SerializeField, Tooltip("Case sombre à coins arrondis (poissons, vitrine).")] private Sprite _roundCard;
        [SerializeField, Tooltip("Liseré doré à coins arrondis, posé sur la vitrine.")] private Sprite _roundRim;

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
                bool baitKey = (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
                    || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
                if (IsOpen && baitKey)
                {
                    _progress.Baits.Cycle(1); // appât suivant parmi ceux qu'on a
                }

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
            _windowRect.anchoredPosition = Choosing ? new Vector2(440f, 10f) : new Vector2(0f, 10f);
            _windowRect.localScale = Vector3.one * (Choosing ? 0.55f : 1.08f);
            string baitKey = Keyboard.current != null ? Keyboard.current.qKey.displayName : "Q";
            _windowHelp.text = Choosing
                ? $"1 à {_progress.Bag.Capacity} : relâcher   •   Espace : relâcher le nouveau"
                : _progress.Baits.Owned.Count > 1 ? $"{baitKey} : changer d'appât   •   E ou Échap : fermer" : "E ou Échap : fermer";
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

        /// <summary>
        /// La vitrine : grande canne du niveau actuel, hameçon pendu au bout du fil, appât accroché dessus, sac ;
        /// avec leurs noms, effets, niveaux (pierres allumées) et ce qu'il reste d'appât.
        /// </summary>
        private void RefreshGear()
        {
            GearCatalog catalog = _progress.GearCatalog;
            GearLevel Item(GearCategory category, out int level, out int max)
            {
                GearLine line = catalog != null ? catalog.Line(category) : null;
                level = _progress.Gear.Level(category);
                max = line != null ? line.MaxLevel : 0;
                return line != null ? line.At(level) : null;
            }

            GearLevel rod = Item(GearCategory.Rod, out int rodLevel, out int rodMax);
            _rodImage.sprite = rod != null ? rod.Illustration : null;
            _rodImage.enabled = _rodImage.sprite != null;
            FillLabel(_rodLabel, rod?.Name, rod == null ? string.Empty : rodLevel == 0 ? "Matériel de départ" : ShopMenu.EffectText(GearCategory.Rod, rod), rodLevel, rodMax);

            // L'hameçon pend au bout du fil, accroché par son œillet ; l'appât est piqué dans sa courbure.
            GearLevel hook = Item(GearCategory.Hook, out int hookLevel, out int hookMax);
            _hookImage.sprite = hook?.Icon;
            _hookImage.enabled = _hookImage.sprite != null;
            Vector2 lineEnd = RodCenter - RodSize * 0.5f + RodLineEnd * ShowcaseScale;
            float hookSize = _hookImage.rectTransform.sizeDelta.x;
            float eyeX = hookLevel == 2 ? 16f : 11f;
            Vector2 hookCenter = lineEnd + new Vector2((16f - eyeX) / 32f * hookSize, -(28.5f - 16f) / 32f * hookSize);
            _hookImage.rectTransform.anchoredPosition = hookCenter;
            _baitImage.rectTransform.anchoredPosition = hookCenter + new Vector2((hookLevel == 2 ? 0f : 13.5f - 16f) / 32f * hookSize, -9f / 32f * hookSize);
            FillLabel(_hookLabel, hook?.Name, hook == null ? string.Empty : hookLevel == 0 ? "Matériel de départ" : ShopMenu.EffectText(GearCategory.Hook, hook), hookLevel, hookMax);

            Bait bait = _progress.EquippedBait;
            _baitImage.sprite = bait != null ? bait.Icon : null;
            _baitImage.enabled = _baitImage.sprite != null;
            int left = bait != null ? _progress.Baits.Count(bait.Id) : 0;
            string baitName = bait == null ? "—" : left == BaitBag.Unlimited ? bait.DisplayName : $"{bait.DisplayName}  ×{left}";
            FillLabel(_baitLabel, baitName, bait != null ? bait.Description : string.Empty, -1, -1);

            GearLevel bag = Item(GearCategory.Bag, out int bagLevel, out int bagMax);
            _bagImage.sprite = bag?.Icon;
            _bagImage.enabled = _bagImage.sprite != null;
            FillLabel(_bagLabel, bag?.Name, bag != null ? ShopMenu.EffectText(GearCategory.Bag, bag) : string.Empty, bagLevel, bagMax);
        }

        private static void FillLabel(GearLabel label, string name, string effect, int level, int max)
        {
            label.Name.text = string.IsNullOrEmpty(name) ? "—" : name;
            label.Effect.text = effect;
            for (int k = 0; k < label.Levels.Count; k++)
            {
                label.Levels[k].gameObject.SetActive(k <= max);
                label.Levels[k].color = k <= level ? Gold : new Color(0.32f, 0.36f, 0.42f);
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

            _windowHelp = UiFactory.Label(_windowRect, "Aide", 25, TextAnchor.MiddleCenter, Muted);
            UiFactory.Place(_windowHelp.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(-30f, 30f));

            BuildGearPanel();

            _fishTitle = UiFactory.Label(_windowRect, "Titre poissons", 32, TextAnchor.MiddleLeft, Gold);
            UiFactory.Bold(_fishTitle);
            UiFactory.Center(_fishTitle.rectTransform, new Vector2(330f, 300f), new Vector2(860f, 40f));

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

        private static readonly Color TagFamily = new Color(0.62f, 0.22f, 0.12f);
        private static readonly Color TagInk = new Color(0.22f, 0.13f, 0.07f);
        private static readonly Color TagSoft = new Color(0.42f, 0.32f, 0.22f);

        /// <summary>Même courbe que le dessin de la grande canne (GearArt.RodPoint), en pixels du dessin.</summary>
        private static Vector2 RodPoint(float t) => new Vector2(6f + t * 56f + t * t * t * 28f, 6f + t * 150f - t * t * t * 12f);

        private static Vector2 RodToPanel(Vector2 pixel) => RodCenter - RodSize * 0.5f + pixel * ShowcaseScale;

        /// <summary>
        /// Vitrine « Mon matériel » à gauche : présentoir en bois éclairé par le haut, la canne cintrée posée sur deux
        /// supports en laiton, son fil jusqu'à l'hameçon et l'appât, le sac sur l'étagère ; une étiquette en papier à
        /// côté de chacun.
        /// </summary>
        private void BuildGearPanel()
        {
            Image panel = UiFactory.Sliced(_windowRect, "Matériel", SkinSprite(s => s.Slot), _skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
            UiFactory.Center(panel.rectTransform, new Vector2(-530f, -10f), new Vector2(600f, 640f));
            RectTransform rect = panel.rectTransform;

            // Le fond en bois est découpé aux coins arrondis du cadre, puis un liseré doré arrondi passe par-dessus.
            if (_roundCard != null)
            {
                panel.sprite = _roundCard;
                panel.type = Image.Type.Sliced;
                panel.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            }

            Image back = UiFactory.Plain(rect, "Présentoir", _showcaseBack != null ? Color.white : new Color(0.40f, 0.26f, 0.16f));
            back.sprite = _showcaseBack;
            UiFactory.Stretch(back.rectTransform, 4f);
            Image rim = UiFactory.Sliced(rect, "Liseré", _roundRim, Color.white);
            UiFactory.Stretch(rim.rectTransform);
            rim.enabled = _roundRim != null;

            // Titre sur un bandeau doré, vissé en haut du présentoir.
            UiFactory.TitlePlaque(rect, _skin, "Mon matériel", 30, new Vector2(0f, 284f), new Vector2(320f, 56f), out _);

            _rodImage = UiFactory.Plain(rect, "Canne", Color.white);
            UiFactory.Center(_rodImage.rectTransform, RodCenter, RodSize);

            // Deux supports en laiton sous le brin : la canne repose dessus.
            foreach (float t in new[] { 0.3f, 0.66f })
            {
                Image peg = UiFactory.Plain(rect, "Support", Color.white);
                peg.sprite = _peg;
                peg.enabled = _peg != null;
                UiFactory.Center(peg.rectTransform, RodToPanel(RodPoint(t)) + new Vector2(2f, -9f), new Vector2(26f, 21f));
            }

            _hookImage = UiFactory.Plain(rect, "Hameçon", Color.white);
            _hookImage.preserveAspect = true;
            UiFactory.Center(_hookImage.rectTransform, Vector2.zero, new Vector2(100f, 100f));
            _baitImage = UiFactory.Plain(rect, "Appât", Color.white);
            _baitImage.preserveAspect = true;
            UiFactory.Center(_baitImage.rectTransform, Vector2.zero, new Vector2(70f, 70f));

            _rodLabel = BuildLabel(rect, "CANNE", new Vector2(150f, 188f), true);
            _hookLabel = BuildLabel(rect, "HAMEÇON", new Vector2(150f, 30f), true);
            _baitLabel = BuildLabel(rect, "APPÂT", new Vector2(150f, -124f), false);

            // Le sac, posé sur l'étagère du bas.
            _bagImage = UiFactory.Plain(rect, "Sac", Color.white);
            _bagImage.preserveAspect = true;
            UiFactory.Center(_bagImage.rectTransform, new Vector2(-220f, -250f), new Vector2(90f, 90f));
            _bagLabel = BuildLabel(rect, "SAC", new Vector2(10f, -254f), true, 330f, 114f);
        }

        /// <summary>
        /// Étiquette en papier : famille, nom (sa taille s'adapte pour tenir sur deux lignes au plus), effet, niveau.
        /// </summary>
        private GearLabel BuildLabel(RectTransform parent, string family, Vector2 position, bool levels, float width = 286f, float height = 150f)
        {
            Image tag = UiFactory.Sliced(parent, $"Étiquette {family}", _roundTag != null ? _roundTag : _tag, _tag != null ? Color.white : new Color(0.92f, 0.86f, 0.70f));
            RectTransform rect = tag.rectTransform;
            UiFactory.Center(rect, position, new Vector2(width, height));

            Text label = UiFactory.Label(rect, "Famille", 22, TextAnchor.MiddleLeft, TagFamily, shadow: false);
            label.text = family;
            UiFactory.Bold(label);
            UiFactory.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-24f, 28f));

            Text name = UiFactory.Label(rect, "Nom", 30, TextAnchor.UpperLeft, TagInk, shadow: false);
            UiFactory.Bold(name);
            FitText(name, 22);
            UiFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(-24f, 40f));

            Text effect = UiFactory.Label(rect, "Effet", 22, TextAnchor.UpperLeft, TagSoft, shadow: false);
            FitText(effect, 18);
            const float effectTop = -80f;
            UiFactory.Place(effect.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, effectTop), new Vector2(-24f, height + effectTop - 8f));

            var gearLabel = new GearLabel { Name = name, Effect = effect };
            if (levels)
            {
                for (int k = 0; k < 4; k++)
                {
                    Image gem = UiFactory.Plain(rect, $"Niveau {k}", Color.white);
                    gem.sprite = SkinSprite(s => s.Gem);
                    UiFactory.Place(gem.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f - (3 - k) * 23f, -10f), new Vector2(21f, 21f));
                    gearLabel.Levels.Add(gem);
                }
            }

            return gearLabel;
        }

        /// <summary>Le texte rétrécit tout seul (jusqu'à <paramref name="minSize"/>) pour tenir dans sa case, sur plusieurs lignes.</summary>
        private static void FitText(Text text, int minSize)
        {
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMaxSize = text.fontSize;
            text.resizeTextMinSize = UiFactory.Size(minSize);
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
            float gridLeft = 330f - (SlotColumns * SlotWidth + (SlotColumns - 1) * SlotGap) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                int column = i % SlotColumns;
                int row = i / SlotColumns;
                var position = new Vector2(gridLeft + SlotWidth * 0.5f + column * (SlotWidth + SlotGap), 135f - row * (SlotHeight + SlotGap));
                Image frame = UiFactory.Sliced(_windowRect, $"Place {i + 1}", _roundCard != null ? _roundCard : SkinSprite(s => s.Slot), _skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
                UiFactory.Center(frame.rectTransform, position, new Vector2(SlotWidth, SlotHeight));

                // Fond d'eau avec le poisson (à l'échelle de sa taille).
                Image backdrop = UiFactory.Sliced(frame.rectTransform, "Fond d'eau", SkinSprite(s => s.WaterBackdrop), _skin != null ? Color.white : new Color(0.2f, 0.55f, 0.65f));
                UiFactory.Place(backdrop.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(SlotWidth - 22f, 130f));
                Image fish = UiFactory.Plain(backdrop.rectTransform, "Poisson", Color.white);
                fish.preserveAspect = true;
                UiFactory.Center(fish.rectTransform, Vector2.zero, new Vector2(SlotWidth - 40f, 100f));

                Text name = UiFactory.Label(frame.rectTransform, "Nom", 26, TextAnchor.MiddleCenter, Color.white);
                UiFactory.Bold(name);
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                UiFactory.Place(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(SlotWidth - 16f, 52f));

                Text details = UiFactory.Label(frame.rectTransform, "Détails", 21, TextAnchor.MiddleCenter, Color.white);
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
                Image lockVeil = UiFactory.Sliced(frame.rectTransform, "Verrou", _roundCard, new Color(0.25f, 0.28f, 0.34f, 0.85f));
                UiFactory.Stretch(lockVeil.rectTransform, 6f);
                Text lockText = UiFactory.Label(lockVeil.rectTransform, "Texte", 22, TextAnchor.MiddleCenter, Muted);
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