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
            public RectTransform Rect;
            public Text Name;
            public Text Effect;
            public readonly List<Image> Levels = new List<Image>();
        }

        private int _hoverSlot = -1;
        private bool _hoverIcon;
        private int _closedByClickFrame = -1;

        private Image _rodImage;
        private Image _hookImage;
        private Image _baitImage;
        private Image _bagImage;
        private GearLabel _rodLabel;
        private GearLabel _hookLabel;
        private GearLabel _baitLabel;
        private GearLabel _bagLabel;
        private Text _petName;
        private Image _petImage;
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
        [SerializeField, Tooltip("Vue de près des bassins : l'aide devant la vitre la mentionne.")] private AquariumCloseUp _closeUp;
        [SerializeField, Tooltip("L'animal de compagnie : tourné vers lui, E le caresse (au lieu d'ouvrir le sac).")] private Pets.PetFollower _pet;
        [SerializeField, Tooltip("Empreinte de patte de l'emplacement de l'animal de compagnie.")] private Sprite _petPaw;
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
            ShowPage(InventoryPage.Bag);
            _progress.Changed += Refresh;
            if (_aquarium != null)
            {
                _aquarium.Deposited += OnDeposited;
            }

            Refresh();
            Close();
            FishingInput.PointerOnInterface = PointerOnInterface;
        }

        private void OnDestroy()
        {
            if (FishingInput.PointerOnInterface == PointerOnInterface)
            {
                FishingInput.PointerOnInterface = null;
            }

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

                // R : canne suivante, F : hameçon suivant (touches physiques ; gâchettes hautes à la manette).
                bool rodKey = (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                    || (Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame);
                bool hookKey = (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                    || (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame);
                if (IsOpen && rodKey)
                {
                    _progress.CycleGear(GearCategory.Rod);
                }

                if (IsOpen && hookKey)
                {
                    _progress.CycleGear(GearCategory.Hook);
                }

                if (IsOpen && (closeKey || InteractInput.PressedThisFrame()))
                {
                    Close();
                }
                else if (!IsOpen && InteractInput.PressedThisFrame() && CanOpenNow())
                {
                    // Tourné vers son animal, E le caresse ; sinon E ouvre le sac.
                    if (_pet != null && _pet.PlayerCanPet) _pet.Pet();
                    else Open();
                }
                else if (IsOpen)
                {
                    int tab = TabPressed();
                    if (tab != 0)
                    {
                        ShowPage((InventoryPage)(((int)_page + tab + 3) % 3));
                    }
                }
            }

            HandleMouse();
            AnimateSlots();
            UpdatePrompt();
            UpdateToast();
        }

        // ---------- Souris ----------

        private static bool Over(RectTransform rect, Vector2 screen) =>
            rect != null && rect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null);

        /// <summary>La souris est sur l'icône du sac ou sur la fenêtre ouverte : le clic ne lance pas la ligne.</summary>
        private bool PointerOnInterface()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || _window == null) return false;
            if (_closedByClickFrame == Time.frameCount) return true; // le clic qui a fermé le sac ne lance pas la ligne
            Vector2 p = mouse.position.ReadValue();
            return IsOpen ? Over(_windowRect, p) : Over(_iconRect, p);
        }

        /// <summary>Case du sac sous la souris (places occupées seulement), ou -1.</summary>
        private int SlotAt(Vector2 screen)
        {
            for (int i = 0; i < _slots.Count && i < _progress.Bag.Count; i++)
            {
                if (Over(_slots[i].Frame.rectTransform, screen)) return i;
            }

            return -1;
        }

        /// <summary>
        /// Clics : l'icône ouvre le sac ; fenêtre ouverte, les onglets changent de page, la canne, l'hameçon et l'appât
        /// (dessin ou étiquette) passent au suivant, un clic à côté ferme. Sac plein, un clic sur une case relâche ce poisson.
        /// Survol : l'élément grossit un peu.
        /// </summary>
        private void HandleMouse()
        {
            _hoverSlot = -1;
            _hoverIcon = false;
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 p = mouse.position.ReadValue();
            bool click = mouse.leftButton.wasPressedThisFrame;

            if (Choosing)
            {
                _hoverSlot = SlotAt(p);
                if (click && _hoverSlot >= 0) _fishing.ChooseReplace(_hoverSlot);
                return;
            }

            if (!IsOpen)
            {
                _hoverIcon = Over(_iconRect, p) && CanOpenNow();
                if (click && _hoverIcon) Open();
                return;
            }

            bool onBag = _page == InventoryPage.Bag;
            bool rod = onBag && (Over(_rodLabel.Rect, p) || Over(_rodImage.rectTransform, p));
            bool hook = onBag && !rod && (Over(_hookLabel.Rect, p) || Over(_hookImage.rectTransform, p));
            bool bait = onBag && !rod && !hook && (Over(_baitLabel.Rect, p) || Over(_baitImage.rectTransform, p));
            Grow(_rodLabel.Rect, rod && _progress.OwnedCount(GearCategory.Rod) > 1);
            Grow(_hookLabel.Rect, hook && _progress.OwnedCount(GearCategory.Hook) > 1);
            Grow(_baitLabel.Rect, bait && _progress.Baits.Owned.Count > 1);
            Grow(_bagTab.rectTransform, Over(_bagTab.rectTransform, p));
            Grow(_relicTab.rectTransform, Over(_relicTab.rectTransform, p));
            Grow(_questTab.rectTransform, Over(_questTab.rectTransform, p));
            if (!click) return;

            if (Over(_bagTab.rectTransform, p)) ShowPage(InventoryPage.Bag);
            else if (Over(_relicTab.rectTransform, p)) ShowPage(InventoryPage.Relics);
            else if (Over(_questTab.rectTransform, p)) ShowPage(InventoryPage.Quests);
            else if (rod) _progress.CycleGear(GearCategory.Rod);
            else if (hook) _progress.CycleGear(GearCategory.Hook);
            else if (bait) _progress.Baits.Cycle(1);
            else if (!Over(_windowRect, p))
            {
                _closedByClickFrame = Time.frameCount;
                Close();
            }
        }

        private static void Grow(RectTransform rect, bool hovered) => rect.localScale = Vector3.one * (hovered ? 1.05f : 1f);

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
            ShowPage(InventoryPage.Bag);
        }

        /// <summary>Ouvre directement la page des quêtes (utilisable par script pour les tests).</summary>
        public void OpenQuests()
        {
            Open();
            ShowPage(InventoryPage.Quests);
        }

        /// <summary>Ouvre directement la page des reliques (utilisable par script pour les tests).</summary>
        public void OpenRelics()
        {
            Open();
            ShowPage(InventoryPage.Relics);
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

            // Devant le lit, E fait dormir.
            if (FishAndChill.World.HouseBuilding.PlayerAtBed)
            {
                return false;
            }

            // Devant le bureau du laboratoire, E fait parler la scientifique.
            if (FishAndChill.World.LabBuilding.PlayerAtDesk || FishAndChill.World.LabBuilding.DialogOpen
                || FishAndChill.World.LabBuilding.PlayerAtPedestal)
            {
                return false;
            }

            // Devant l'ascenseur de l'aquarium, E ouvre le menu des étages (et pendant le trajet).
            if (FishAndChill.UI.ElevatorMenu.Busy || DialogueBox.Busy || PetShopMenu.Busy || (_aquarium != null && _aquarium.PlayerAtElevator >= 0))
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
                ShowPage(InventoryPage.Bag);
            }
            else if (!Choosing && _openedForChoice)
            {
                _window.SetActive(false);
                _openedForChoice = false;
            }

            // Sac plein : on montre toujours le sac (et pas d'onglets).
            if (Choosing && (_page != InventoryPage.Bag || _bagTab.gameObject.activeSelf))
            {
                ShowPage(InventoryPage.Bag);
            }
            else if (!Choosing && IsOpen && !_bagTab.gameObject.activeSelf)
            {
                ShowPage(_page);
            }

            _windowTitle.text = Choosing ? "Quel poisson relâcher ?" : "Mon inventaire";
            // Pendant le choix, la carte de la prise est à gauche : la fenêtre se fait plus petite, à droite.
            _windowRect.anchoredPosition = Choosing ? new Vector2(440f, 10f) : new Vector2(0f, 10f);
            _windowRect.localScale = Vector3.one * (Choosing ? 0.55f : 1.08f);
            _windowHelp.text = Choosing
                ? $"1 à {_progress.Bag.Capacity} : relâcher   •   Espace : relâcher le nouveau"
                : (_page == InventoryPage.Bag ? ChangeHelp() : string.Empty) + "Tab : onglet   •   E ou Échap : fermer";
            _windowHelp.color = Choosing ? Gold : Muted;
        }

        /// <summary>Les touches pour changer de canne, d'hameçon ou d'appât, seulement pour ce qu'on a en plusieurs.</summary>
        private string ChangeHelp()
        {
            var keyboard = Keyboard.current;
            string Key(UnityEngine.InputSystem.Controls.KeyControl key, string fallback) => key != null ? key.displayName.ToUpperInvariant() : fallback;
            string help = string.Empty;
            if (_progress.OwnedCount(GearCategory.Rod) > 1) help += $"{Key(keyboard?.rKey, "R")} : canne   •   ";
            if (_progress.OwnedCount(GearCategory.Hook) > 1) help += $"{Key(keyboard?.fKey, "F")} : hameçon   •   ";
            if (_progress.Baits.Owned.Count > 1) help += $"{Key(keyboard?.qKey, "Q")} : appât   •   ";
            return help;
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
                slot.Frame.rectTransform.localScale = Vector3.one * (i == _hoverSlot ? 1.05f : 1f);
            }

            // Petit rebond de l'icône quand le sac est plein ; elle grossit sous la souris.
            float scale = (bag.IsFull ? 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 4f) : 1f) * (_hoverIcon ? 1.08f : 1f);
            _iconRect.localScale = Vector3.one * scale;
        }

        private void UpdatePrompt()
        {
            if (_aquarium != null && _aquarium.CanDeposit && !IsOpen && _toastTimer <= 0f)
            {
                _prompt.gameObject.SetActive(true);
                _prompt.text = (_progress.Bag.Count > 0 ? "E : déposer tes poissons dans le bassin" : "Ton sac est vide : va pêcher !")
                    + (_closeUp != null && _closeUp.CanOpen ? "    Haut : regarder de près" : string.Empty);
            }
            else if (_pet != null && _pet.PlayerCanPet && !IsOpen && _toastTimer <= 0f && CanOpenNow())
            {
                _prompt.gameObject.SetActive(true);
                _prompt.text = "E : caresser ton compagnon";
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
            RefreshRelics();

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
            FillLabel(_rodLabel, rod?.Name, rod == null ? string.Empty : ShopMenu.EffectText(GearCategory.Rod, rod), -1, -1); // objets à choisir, pas des niveaux

            // L'hameçon pend au bout du fil, accroché par son œillet ; l'appât est piqué dans sa courbure.
            GearLevel hook = Item(GearCategory.Hook, out int hookLevel, out int hookMax);
            _hookImage.sprite = hook?.Icon;
            _hookImage.enabled = _hookImage.sprite != null;
            Vector2 lineEnd = RodCenter - RodSize * 0.5f + RodLineEnd * ShowcaseScale;
            float hookSize = _hookImage.rectTransform.sizeDelta.x;
            float eyeX = hookLevel == 1 ? 16f : 11f;
            Vector2 hookCenter = lineEnd + new Vector2((16f - eyeX) / 32f * hookSize, -(28.5f - 16f) / 32f * hookSize);
            _hookImage.rectTransform.anchoredPosition = hookCenter;
            _baitImage.rectTransform.anchoredPosition = hookCenter + new Vector2((hookLevel == 2 ? 0f : 13.5f - 16f) / 32f * hookSize, -9f / 32f * hookSize);
            FillLabel(_hookLabel, hook?.Name, hook == null ? string.Empty : ShopMenu.EffectText(GearCategory.Hook, hook), -1, -1);

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

            // L'animal qui accompagne le joueur : son portrait et son nom, sinon l'empreinte en creux et « Aucun ».
            FishAndChill.Pets.PetDefinition pet = _progress.EquippedPet;
            if (_petImage != null)
            {
                _petImage.sprite = pet != null && pet.Portrait != null ? pet.Portrait : _petPaw;
                _petImage.color = pet != null ? Color.white : new Color(0.62f, 0.48f, 0.34f, 0.75f);
                _petImage.enabled = _petImage.sprite != null;
                UiFactory.Center(_petImage.rectTransform, new Vector2(0f, pet != null ? -2f : -4f), pet != null ? new Vector2(70f, 63f) : new Vector2(44f, 44f));
            }

            if (_petName != null) _petName.text = pet != null ? pet.DisplayName : "Aucun";
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

            // Trois onglets : le sac et le matériel, les reliques des légendes, les quêtes des habitants.
            _bagPage = Page("Page du sac");
            _relicPage = Page("Page des reliques");
            _questPage = Page("Page des quêtes");
            Image Tab(string title, float x)
            {
                UiFactory.TitlePlaque(_windowRect, _skin, title, 24, new Vector2(x, 345f), new Vector2(230f, 48f), out RectTransform tab);
                return tab.GetComponent<Image>();
            }

            _bagTab = Tab("Sac et matériel", -640f);
            _relicTab = Tab("Reliques", -395f);
            _questTab = Tab("Quêtes", -150f);

            BuildGearPanel();
            BuildRelicPage();
            _quests = new QuestPage(_questPage, _skin, _roundCard);

            _fishTitle = UiFactory.Label(_bagPage, "Titre poissons", 32, TextAnchor.MiddleLeft, Gold);
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

        // ---------- Onglets ----------

        private enum InventoryPage { Bag, Relics, Quests }

        private InventoryPage _page = InventoryPage.Bag;
        private RectTransform _bagPage;
        private RectTransform _relicPage;
        private Image _bagTab;
        private Image _relicTab;
        private RectTransform _questPage;
        private Image _questTab;
        private QuestPage _quests;

        private RectTransform Page(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_windowRect, false);
            UiFactory.Stretch(rect);
            return rect;
        }

        /// <summary>Onglet suivant (Tab, →, croix droite) ou précédent (←, croix gauche) ; 0 sinon.</summary>
        private static int TabPressed()
        {
            Keyboard k = Keyboard.current;
            Gamepad g = Gamepad.current;
            if ((k != null && (k.tabKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame)) || (g != null && g.dpad.right.wasPressedThisFrame)) return 1;
            if ((k != null && k.leftArrowKey.wasPressedThisFrame) || (g != null && g.dpad.left.wasPressedThisFrame)) return -1;
            return 0;
        }

        /// <summary>Affiche une page de l'inventaire (le sac pendant le choix « sac plein »).</summary>
        private void ShowPage(InventoryPage page)
        {
            _page = Choosing ? InventoryPage.Bag : page;
            _bagPage.gameObject.SetActive(_page == InventoryPage.Bag);
            _relicPage.gameObject.SetActive(_page == InventoryPage.Relics);
            _questPage.gameObject.SetActive(_page == InventoryPage.Quests);
            Color dim = new Color(0.55f, 0.55f, 0.6f);
            _bagTab.color = _page == InventoryPage.Bag ? Color.white : dim;
            _relicTab.color = _page == InventoryPage.Relics ? Color.white : dim;
            _questTab.color = _page == InventoryPage.Quests ? Color.white : dim;
            _bagTab.gameObject.SetActive(!Choosing);
            _relicTab.gameObject.SetActive(!Choosing);
            _questTab.gameObject.SetActive(!Choosing);
            if (_page == InventoryPage.Quests)
            {
                _quests.Refresh();
            }

            if (_page == InventoryPage.Relics)
            {
                RefreshRelics();
            }
        }

        // ---------- Page des reliques (artefacts des poissons de légende) ----------

        private const int RelicColumns = 3;
        private const float RelicWidth = 520f;
        private const float RelicHeight = 290f;

        private Text _relicHeader;
        private Text _relicEmpty;
        private readonly List<RelicView> _relics = new List<RelicView>();

        private sealed class RelicView
        {
            public Image Frame;
            public Image Silhouette;
            public readonly List<Image> Pieces = new List<Image>();
            public readonly List<Image> Pips = new List<Image>();
            public RectTransform PipRow;
            public Text Name;
            public Text Zone;
            public Text Status;
            public Text Hint;
        }

        /// <summary>
        /// Page « Reliques » : une carte par poisson de légende (3 par rangée) avec son artefact (fragments posés en
        /// couleur, en poche à demi transparents, manquants en silhouette), des pierres par fragment, l'étape et la suite.
        /// Les fragments ne prennent pas de place dans le sac.
        /// </summary>
        private void BuildRelicPage()
        {
            _relicHeader = UiFactory.Label(_relicPage, "Titre reliques", 32, TextAnchor.MiddleLeft, Gold);
            UiFactory.Bold(_relicHeader);
            UiFactory.Center(_relicHeader.rectTransform, new Vector2(-10f, 278f), new Vector2(1600f, 40f));

            _relicEmpty = UiFactory.Label(_relicPage, "Aucune légende", 26, TextAnchor.MiddleCenter, Muted);
            _relicEmpty.text = "Aucune légende connue pour l'instant.";
            UiFactory.Center(_relicEmpty.rectTransform, Vector2.zero, new Vector2(1200f, 40f));
        }

        private RelicView CreateRelicCard(int index)
        {
            int column = index % RelicColumns;
            int row = index / RelicColumns;
            var position = new Vector2((column - 1) * (RelicWidth + 24f), 110f - row * (RelicHeight + 24f));
            var view = new RelicView();
            view.Frame = UiFactory.Sliced(_relicPage, $"Relique {index + 1}", _roundCard != null ? _roundCard : SkinSprite(s => s.Slot), _skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
            RectTransform card = view.Frame.rectTransform;
            UiFactory.Center(card, position, new Vector2(RelicWidth, RelicHeight));

            Image backdrop = UiFactory.Sliced(card, "Fond", SkinSprite(s => s.WaterBackdrop), _skin != null ? new Color(1f, 1f, 1f, 0.55f) : new Color(0.2f, 0.4f, 0.5f));
            UiFactory.Center(backdrop.rectTransform, new Vector2(-165f, 0f), new Vector2(170f, 250f));
            view.Silhouette = UiFactory.Plain(card, "Artefact", Color.white);
            view.Silhouette.preserveAspect = true;
            UiFactory.Center(view.Silhouette.rectTransform, new Vector2(-165f, 0f), new Vector2(165f, 220f));

            view.Name = UiFactory.Label(card, "Nom", 30, TextAnchor.MiddleLeft, Gold);
            UiFactory.Bold(view.Name);
            UiFactory.Center(view.Name.rectTransform, new Vector2(95f, 102f), new Vector2(330f, 40f));
            view.Zone = UiFactory.Label(card, "Zone", 20, TextAnchor.MiddleLeft, Muted);
            UiFactory.Center(view.Zone.rectTransform, new Vector2(95f, 70f), new Vector2(330f, 28f));

            var pipRow = new GameObject("Fragments", typeof(RectTransform));
            view.PipRow = pipRow.GetComponent<RectTransform>();
            view.PipRow.SetParent(card, false);
            UiFactory.Center(view.PipRow, new Vector2(95f, 30f), new Vector2(330f, 34f));

            view.Status = UiFactory.Label(card, "Étape", 23, TextAnchor.UpperLeft, Color.white);
            view.Status.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Center(view.Status.rectTransform, new Vector2(95f, -22f), new Vector2(330f, 60f));
            view.Hint = UiFactory.Label(card, "Suite", 19, TextAnchor.UpperLeft, Muted);
            view.Hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Center(view.Hint.rectTransform, new Vector2(95f, -92f), new Vector2(330f, 70f));
            return view;
        }

        private void RefreshRelics()
        {
            if (_relicPage == null || _fishing == null)
            {
                return;
            }

            IReadOnlyList<LegendFish> legends = _fishing.Legends;
            int beaten = 0;
            for (int i = 0; i < legends.Count; i++)
            {
                LegendFish legend = legends[i];
                if (legend == null) continue;
                while (_relics.Count <= i) _relics.Add(CreateRelicCard(_relics.Count));
                if (_progress.Legends.State(legend.Id) == LegendState.Beaten) beaten++;
                FillRelic(_relics[i], legend);
            }

            _relicEmpty.gameObject.SetActive(legends.Count == 0);
            _relicHeader.text = $"Reliques des légendes   {beaten} / {legends.Count} vaincue{(beaten > 1 ? "s" : string.Empty)}";
        }

        private void FillRelic(RelicView view, LegendFish legend)
        {
            LegendProgress progress = _progress.Legends;
            LegendState state = progress.State(legend.Id);
            int total = legend.FragmentCount;
            int found = state == LegendState.Collecting ? progress.Fragments(legend.Id) : total;
            int placed = state == LegendState.Collecting ? progress.Placed(legend.Id) : total;
            bool known = found > 0;
            string zone = legend.Zone != null ? legend.Zone.DisplayName : "?";

            view.Silhouette.sprite = legend.Silhouette;
            view.Silhouette.color = known ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            while (view.Pieces.Count < total)
            {
                Image piece = UiFactory.Plain(view.Silhouette.rectTransform, "Fragment", Color.white);
                piece.preserveAspect = true;
                UiFactory.Stretch(piece.rectTransform);
                view.Pieces.Add(piece);
            }

            for (int i = 0; i < view.Pieces.Count; i++)
            {
                // Posé : en couleur ; en poche : à demi transparent, pas encore à sa place ; manquant : silhouette seule.
                view.Pieces[i].sprite = legend.Fragment(i);
                view.Pieces[i].gameObject.SetActive(i < total && i < found && legend.Fragment(i) != null);
                view.Pieces[i].color = i < placed ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            }

            while (view.Pips.Count < total)
            {
                Image pip = UiFactory.Plain(view.PipRow, "Pierre", Color.white);
                pip.sprite = SkinSprite(s => s.Gem);
                UiFactory.Place(pip.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(view.Pips.Count * 40f, 0f), new Vector2(30f, 30f));
                view.Pips.Add(pip);
            }

            for (int i = 0; i < view.Pips.Count; i++)
            {
                view.Pips[i].color = i < placed ? Gold : i < found ? new Color(0.45f, 0.95f, 0.92f) : new Color(0.3f, 0.34f, 0.4f);
            }

            view.Name.text = known ? legend.ArtifactName : "???";
            view.Zone.text = $"Du côté de « {zone} »";
            switch (state)
            {
                case LegendState.Beaten:
                    view.Status.text = $"{legend.Title} vaincu — {legend.SizeCm:0} cm";
                    view.Status.color = Gold;
                    view.Hint.text = "Une légende de plus à ton palmarès !";
                    break;
                case LegendState.Summoned:
                    view.Status.text = $"{legend.Title} est réveillé !";
                    view.Status.color = new Color(1f, 0.55f, 0.35f);
                    view.Hint.text = "Il t'attend au remous légendaire : le point doré sur ta carte.";
                    break;
                case LegendState.Complete:
                    view.Status.text = "Artefact complet";
                    view.Status.color = Gold;
                    view.Hint.text = "Réveille le gardien depuis le socle du laboratoire.";
                    break;
                default:
                    view.Status.color = Color.white;
                    view.Status.text = known ? $"{found} / {total} trouvés   •   {placed} posé{(placed > 1 ? "s" : string.Empty)}" : "Aucun fragment trouvé";
                    view.Hint.text = !known ? "Ses fragments se pêchent ici, plus souvent dans les coins de pêche."
                        : found > placed ? "Pose tes fragments sur le socle du laboratoire."
                        : "Continue de pêcher : il en manque encore.";
                    break;
            }
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
            Image panel = UiFactory.Sliced(_bagPage, "Matériel", SkinSprite(s => s.Slot), _skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
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
            _bagLabel = BuildLabel(rect, "SAC", new Vector2(-8f, -254f), true, 300f, 114f);
            BuildPetSlot(rect);
        }

        /// <summary>
        /// L'emplacement de l'animal de compagnie, sur l'étagère du bas à côté du sac : une étiquette avec une empreinte
        /// de patte. Vide pour l'instant (« Aucun ») : les animaux viendront de l'animalerie.
        /// </summary>
        private void BuildPetSlot(RectTransform parent)
        {
            Image tag = UiFactory.Sliced(parent, "Étiquette ANIMAL", _roundTag != null ? _roundTag : _tag, _tag != null ? Color.white : new Color(0.92f, 0.86f, 0.70f));
            RectTransform rect = tag.rectTransform;
            UiFactory.Center(rect, new Vector2(222f, -254f), new Vector2(128f, 114f));

            Text family = UiFactory.Label(rect, "Famille", 22, TextAnchor.MiddleCenter, TagFamily, shadow: false);
            family.text = "ANIMAL";
            UiFactory.Bold(family);
            UiFactory.Place(family.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-12f, 26f));

            Image paw = UiFactory.Plain(rect, "Patte", new Color(0.62f, 0.48f, 0.34f, 0.75f));   // empreinte en creux : emplacement vide
            _petImage = paw;
            paw.sprite = _petPaw;
            paw.enabled = _petPaw != null;
            paw.preserveAspect = true;
            UiFactory.Center(paw.rectTransform, new Vector2(0f, -4f), new Vector2(44f, 44f));

            _petName = UiFactory.Label(rect, "Nom", 20, TextAnchor.MiddleCenter, TagSoft, shadow: false);
            _petName.text = "Aucun";
            UiFactory.Place(_petName.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(-12f, 24f));
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

            var gearLabel = new GearLabel { Rect = rect, Name = name, Effect = effect };
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
                Image frame = UiFactory.Sliced(_bagPage, $"Place {i + 1}", _roundCard != null ? _roundCard : SkinSprite(s => s.Slot), _skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
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