using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Gear;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Le marchand de la boutique : devant le comptoir, E ouvre son menu, en deux onglets (Tab ou ←/→ pour changer).
    /// « Vendre » : 1, 2, 3… vend un poisson, V vend tous les doublons.
    /// « Matériel » : 1 à 4 achète le niveau suivant de la canne, de l'hameçon, de l'appât ou du sac.
    /// Échap (ou E) referme. Le joueur est immobile pendant ce temps.
    /// </summary>
    public sealed class ShopMenu : MonoBehaviour
    {
        public enum Tab { Sell, Rods, Hooks, Baits, Bag }
        private const int TabCount = 5;

        private static readonly Color PanelColor = new Color(0.07f, 0.10f, 0.14f, 1f);
        private static readonly Color RowColor = new Color(0.14f, 0.24f, 0.30f, 1f);
        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.62f, 0.68f, 0.74f);
        private static readonly Color Good = new Color(0.55f, 0.92f, 0.55f);
        private static readonly Color TooExpensive = new Color(0.90f, 0.50f, 0.45f);

        private const string Greeting = "« Bienvenue ! Je rachète tes doublons. Le plus gros de chaque nouvelle espèce, garde-le pour l'aquarium ! »";
        private const string GearGreeting = "« Chaque canne a son caractère : achète celle qui te plaît, tu pourras changer quand tu veux ! »";
        private const string HookGreeting = "« Un bon hameçon, c'est un poisson qui ne se décroche pas ! »";
        private const string BagGreeting = "« Plus de place dans ton sac, moins d'allers-retours ! »";
        private const string BaitGreeting = "« Chaque appât a ses amateurs : regarde le carnet pour savoir qui aime quoi ! »";
        private const float RowsAreaHeight = 560f;

        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private ShopBuilding _shop;
        [SerializeField] private PlayerMovement _player;

        [SerializeField, Min(0.5f), Tooltip("Distance au comptoir pour parler au marchand.")]
        private float _talkRadius = 1.3f;

        [SerializeField, Tooltip("Charte pixel art de l'interface.")]
        private UiSkin _skin;

        private sealed class RowView
        {
            public Image Background;
            public Image Picture;
            public Text Label;
            public Text Detail;
            public Text Price;
        }

        private readonly List<RowView> _sellRows = new List<RowView>();
        private readonly List<RowView> _gearRows = new List<RowView>();
        private readonly List<RowView> _baitRows = new List<RowView>();
        private RectTransform _baitRoot;
        private Image _baitTab;
        private GameObject _panel;
        private RectTransform _sellRoot;
        private RectTransform _gearRoot;
        private Image _sellTab;
        private Image _rodTab;
        private Image _hookTab;
        private Image _bagTab;
        private Text _prompt;
        private Text _coins;
        private Text _feedback;
        private Text _help;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public Tab CurrentTab { get; private set; }

        /// <summary>Le joueur est dans la boutique, devant le comptoir.</summary>
        public bool PlayerAtCounter =>
            _shop != null && _player != null && _shop.IsPlayerInside
            && Vector2.Distance(_player.transform.position, _shop.CounterFront) <= _talkRadius;

        private void Start()
        {
            if (_progress == null)
            {
                enabled = false;
                return;
            }

            _progress.EnsureCreated();
            BuildUi();
            _progress.Changed += RefreshIfOpen;
            Close();
        }

        private void OnDestroy()
        {
            if (_progress != null)
            {
                _progress.Changed -= RefreshIfOpen;
            }
        }

        private void Update()
        {
            if (!IsOpen)
            {
                _prompt.gameObject.SetActive(PlayerAtCounter);
                // Pas d'ouverture si une autre fenêtre (index, sac) bloque déjà le joueur.
                if (PlayerAtCounter && !_player.Locked && InteractInput.PressedThisFrame())
                {
                    Open();
                }

                return;
            }

            int tabStep = TabStepPressed();
            if (tabStep != 0)
            {
                ShowTab((Tab)(((int)CurrentTab + tabStep + TabCount) % TabCount));
                return;
            }

            int slot = FishingInput.SlotPressedThisFrame();
            if (slot >= 0)
            {
                if (CurrentTab == Tab.Sell)
                {
                    SellSlot(slot);
                }
                else if (CurrentTab == Tab.Rods)
                {
                    BuyItem(GearCategory.Rod, slot);
                }
                else if (CurrentTab == Tab.Hooks)
                {
                    BuyItem(GearCategory.Hook, slot);
                }
                else if (CurrentTab == Tab.Bag)
                {
                    if (slot == 0) BuyGear(GearCategory.Bag);
                    else if (slot == 1) BuyTrap();
                }
                else
                {
                    BuyBait(slot);
                }
            }
            else if (CurrentTab == Tab.Sell && LetterPressed("v"))
            {
                SellAll();
            }
            else if (InteractInput.PressedThisFrame() || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                Close();
            }
        }

        public void Open()
        {
            _panel.SetActive(true);
            _prompt.gameObject.SetActive(false);
            if (_player != null)
            {
                _player.Locked = true;
            }

            ShowTab(Tab.Sell);
        }

        public void Close()
        {
            _panel.SetActive(false);
            if (_player != null)
            {
                _player.Locked = false;
            }
        }

        /// <summary>Affiche l'onglet demandé (vente des poissons ou achat de matériel).</summary>
        public void ShowTab(Tab tab)
        {
            CurrentTab = tab;
            _sellRoot.gameObject.SetActive(tab == Tab.Sell);
            _gearRoot.gameObject.SetActive(tab == Tab.Rods || tab == Tab.Hooks || tab == Tab.Bag);
            _baitRoot.gameObject.SetActive(tab == Tab.Baits);
            Color dim = new Color(0.55f, 0.55f, 0.6f);
            _sellTab.color = tab == Tab.Sell ? Color.white : dim;
            _rodTab.color = tab == Tab.Rods ? Color.white : dim;
            _hookTab.color = tab == Tab.Hooks ? Color.white : dim;
            _baitTab.color = tab == Tab.Baits ? Color.white : dim;
            _bagTab.color = tab == Tab.Bag ? Color.white : dim;
            const string end = "   •   Tab : onglet suivant   •   Échap : sortir";
            switch (tab)
            {
                case Tab.Sell: _help.text = "1, 2, 3… : vendre ce poisson   •   V : tout vendre" + end; Say(Greeting, Muted); break;
                case Tab.Rods: _help.text = "1 à 5 : acheter, ou prendre une canne déjà à toi" + end; Say(GearGreeting, Muted); break;
                case Tab.Hooks: _help.text = "1 à 4 : acheter, ou prendre un hameçon déjà à toi" + end; Say(HookGreeting, Muted); break;
                case Tab.Baits: _help.text = "1 à 6 : acheter un paquet" + end; Say(BaitGreeting, Muted); break;
                default: _help.text = "1 : acheter le sac suivant   •   2 : acheter un casier" + end; Say(BagGreeting, Muted); break;
            }
            Refresh();
        }

        // ---------- Vente ----------

        /// <summary>Vend le poisson de la place <paramref name="slot"/> (0 = première place).</summary>
        public int SellSlot(int slot)
        {
            Inventory bag = _progress.Bag;
            if (slot >= bag.Count)
            {
                Say($"Ta place {slot + 1} est vide.", Muted);
                return 0;
            }

            CaughtFish fish = bag.Fish[slot];
            int earned = FishShop.Sell(bag, slot, _progress.Wallet, _progress.Aquarium, _progress.SellFactor);
            if (earned > 0)
            {
                Say($"« Marché conclu ! » {fish.Species.CommonName} vendu : +{earned} pièces", Good);
            }
            else
            {
                Say($"« Ton {fish.Species.CommonName}, garde-le pour l'aquarium ! »", Gold);
            }

            return earned;
        }

        /// <summary>Vend tous les doublons du sac.</summary>
        public int SellAll()
        {
            int earned = FishShop.SellAll(_progress.Bag, _progress.Wallet, _progress.Aquarium, _progress.SellFactor);
            if (earned > 0)
            {
                Say($"« Je prends tout ça ! » +{earned} pièces", Good);
            }
            else
            {
                Say("« Rien à vendre pour l'instant : pêche encore quelques doublons ! »", Muted);
            }

            return earned;
        }

        // ---------- Matériel ----------

        /// <summary>Achète (ou équipe, s'il est déjà à toi) l'objet numéro <paramref name="index"/> de cette famille.</summary>
        public GearPurchase BuyItem(GearCategory category, int index)
        {
            GearLine line = _progress.GearCatalog != null ? _progress.GearCatalog.Line(category) : null;
            GearLevel item = line != null ? line.At(index) : null;
            if (item == null || index >= line.Levels.Count)
            {
                return GearPurchase.Unavailable;
            }

            bool wasEquipped = _progress.Gear.Level(category) == index;
            GearPurchase result = _progress.BuyGearItem(category, index);
            switch (result)
            {
                case GearPurchase.Bought:
                    Say($"« Excellent choix ! » {item.Name} : {EffectText(category, item)}", Good);
                    break;
                case GearPurchase.AlreadyOwned:
                    Say(wasEquipped ? $"« Tu l'as déjà en main ! » ({item.Name})" : $"« C'est déjà à toi, tiens ! » {item.Name} en main.", Gold);
                    break;
                case GearPurchase.NotEnoughCoins:
                    Say($"« Il te manque {item.Price - _progress.Wallet.Coins} pièces pour ça. Reviens avec des doublons ! »", TooExpensive);
                    break;
            }

            return result;
        }

        /// <summary>Achète le niveau suivant de cette famille (le sac).</summary>
        public GearPurchase BuyGear(GearCategory category)
        {
            GearLine line = _progress.GearCatalog != null ? _progress.GearCatalog.Line(category) : null;
            if (line == null)
            {
                return GearPurchase.Unavailable;
            }

            GearLevel next = GearShop.NextLevel(line, _progress.Gear);
            GearPurchase result = _progress.BuyGear(line.Category);
            switch (result)
            {
                case GearPurchase.Bought:
                    Say($"« Excellent choix ! » {next.Name} : {EffectText(line.Category, next)}", Good);
                    break;
                case GearPurchase.NotEnoughCoins:
                    Say($"« Il te manque {next.Price - _progress.Wallet.Coins} pièces pour ça. Reviens avec des doublons ! »", TooExpensive);
                    break;
                case GearPurchase.AlreadyMax:
                    Say($"« Tu as déjà ce qui se fait de mieux en {line.DisplayName.ToLowerInvariant()} ! »", Gold);
                    break;
            }

            return result;
        }

        /// <summary>Ce que change ce niveau de matériel, en clair.</summary>
        public static string EffectText(GearCategory category, GearLevel level)
        {
            switch (category)
            {
                case GearCategory.Rod:
                    if (!string.IsNullOrEmpty(level.Specialty)) return level.Specialty;
                    return $"zone de réussite +{level.Value * 100f:0} %, curseur plus lent";
                case GearCategory.Hook:
                    if (!string.IsNullOrEmpty(level.Specialty)) return level.Specialty;
                    int misses = Mathf.RoundToInt(level.Value);
                    return $"+{misses} raté{(misses > 1 ? "s" : "")} toléré{(misses > 1 ? "s" : "")}";
                case GearCategory.Bait:
                    return $"rares ×{1f + level.Value:0.#}, touches {level.Value2 * 100f:0} % plus vite";
                case GearCategory.Bag:
                    return $"{Mathf.RoundToInt(level.Value)} places";
                default:
                    return string.Empty;
            }
        }

        // ---------- Rafraîchissement ----------

        private void Say(string text, Color color)
        {
            _feedback.text = text;
            _feedback.color = color;
        }

        private void RefreshIfOpen()
        {
            if (IsOpen)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            RefreshSell();
            RefreshGear();
            RefreshBaits();
            _coins.text = $"Tes pièces : {_progress.Wallet.Coins}";
        }

        private void RefreshSell()
        {
            Inventory bag = _progress.Bag;
            EnsureSellRows(bag.Capacity);
            for (int i = 0; i < _sellRows.Count; i++)
            {
                RowView row = _sellRows[i];
                if (i >= bag.Count)
                {
                    row.Picture.gameObject.SetActive(false);
                    row.Label.text = $"{i + 1}.   (vide)";
                    row.Label.color = Muted;
                    row.Price.text = string.Empty;
                    continue;
                }

                CaughtFish fish = bag.Fish[i];
                row.Picture.gameObject.SetActive(true);
                row.Picture.sprite = fish.Species.Sprite;
                row.Label.text = $"{i + 1}.   {fish.Species.CommonName}  —  {fish.SizeCm:0} cm";
                row.Label.color = Color.white;

                bool sellable = FishShop.IsSellable(bag.Fish, i, _progress.Aquarium);
                row.Price.text = sellable ? $"{FishShop.PriceOf(fish, _progress.SellFactor)} pièces" : "Pour l'aquarium";
                row.Price.color = sellable ? Gold : Muted;
            }
        }

        private void RefreshGear()
        {
            GearCatalog catalog = _progress.GearCatalog;
            GearCategory category = CurrentTab == Tab.Hooks ? GearCategory.Hook : CurrentTab == Tab.Bag ? GearCategory.Bag : GearCategory.Rod;
            GearLine shown = catalog != null ? catalog.Line(category) : null;
            if (shown == null)
            {
                EnsureGearRows(0);
                return;
            }

            if (category != GearCategory.Bag)
            {
                RefreshItems(shown);
                return;
            }

            TrapDefinition trap = _progress.Trap;
            EnsureGearRows(trap != null ? 2 : 1);
            if (trap != null)
            {
                RefreshTrapRow(_gearRows[1], trap);
            }

            for (int i = 0; i < 1; i++)
            {
                RowView row = _gearRows[i];
                GearLine line = shown;
                int level = _progress.Gear.Level(line.Category);
                GearLevel current = line.At(level);
                GearLevel next = GearShop.NextLevel(line, _progress.Gear);

                row.Picture.sprite = line.Icon;
                row.Label.text = $"1.   {current?.Name}   (niv. {level}/{line.MaxLevel})";
                if (next == null)
                {
                    row.Detail.text = $"Le meilleur ! {EffectText(line.Category, current)}";
                    row.Detail.color = Gold;
                    row.Price.text = "MAX";
                    row.Price.color = Gold;
                    continue;
                }

                row.Detail.text = $"Suivant : {next.Name} — {EffectText(line.Category, next)}";
                row.Detail.color = Muted;
                row.Price.text = $"{next.Price} pièces";
                row.Price.color = _progress.Wallet.Coins >= next.Price ? Gold : TooExpensive;
            }
        }

        /// <summary>La ligne du casier à crustacés, sous le sac : combien on en a, combien sont dans l'eau, son prix.</summary>
        private void RefreshTrapRow(RowView row, TrapDefinition trap)
        {
            TrapSet traps = _progress.Traps;
            row.Picture.sprite = trap.Icon;
            row.Label.text = $"2.   {trap.DisplayName}   ({traps.Owned}/{trap.MaxOwned})";
            row.Label.color = Color.white;
            string placed = traps.Placed.Count > 0 ? $"   ({traps.Placed.Count} dans l'eau)" : string.Empty;
            row.Detail.text = "Les crustacés ne s'attrapent qu'au casier : C devant l'eau pour le poser." + placed;
            row.Detail.color = Muted;
            if (traps.Owned >= trap.MaxOwned)
            {
                row.Price.text = "MAX";
                row.Price.color = Gold;
            }
            else
            {
                row.Price.text = $"{trap.Price} pièces";
                row.Price.color = _progress.Wallet.Coins >= trap.Price ? Gold : TooExpensive;
            }
        }

        /// <summary>Achète un casier à crustacés.</summary>
        public TrapPurchase BuyTrap()
        {
            TrapDefinition trap = _progress.Trap;
            TrapPurchase result = _progress.BuyTrap();
            switch (result)
            {
                case TrapPurchase.Bought:
                    Say("« Un bon casier ! Pose-le près de la rive et reviens dans une demi-journée. »", Good);
                    break;
                case TrapPurchase.NotEnoughCoins:
                    Say($"« Il te manque {trap.Price - _progress.Wallet.Coins} pièces pour un casier. »", TooExpensive);
                    break;
                case TrapPurchase.AtMax:
                    Say($"« Tu as déjà {trap.MaxOwned} casiers, c'est bien assez ! »", Gold);
                    break;
            }

            return result;
        }

        /// <summary>Une ligne par objet de la famille : possédé, en main, ou son prix.</summary>
        private void RefreshItems(GearLine line)
        {
            EnsureGearRows(line.Levels.Count);
            int equipped = _progress.Gear.Level(line.Category);
            for (int i = 0; i < _gearRows.Count; i++)
            {
                RowView row = _gearRows[i];
                GearLevel item = line.Levels[i];
                bool owned = _progress.Gear.Owns(line.Category, i);
                row.Picture.sprite = item.Icon != null ? item.Icon : line.Icon;
                row.Label.text = $"{i + 1}.   {item.Name}{(i == equipped ? "   — en main" : string.Empty)}";
                row.Label.color = i == equipped ? Gold : Color.white;
                row.Detail.text = EffectText(line.Category, item);
                row.Detail.color = Muted;
                if (owned)
                {
                    row.Price.text = i == equipped ? "En main" : "À toi";
                    row.Price.color = i == equipped ? Gold : Good;
                }
                else
                {
                    row.Price.text = $"{item.Price} pièces";
                    row.Price.color = _progress.Wallet.Coins >= item.Price ? Gold : TooExpensive;
                }
            }
        }

        /// <summary>Onglet suivant (Tab, →, gâchette droite) ou précédent (←, gâchette gauche) ; 0 sinon.</summary>
        private static int TabStepPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.tabKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) return 1;
                if (keyboard.leftArrowKey.wasPressedThisFrame) return -1;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.rightShoulder.wasPressedThisFrame) return 1;
                if (gamepad.leftShoulder.wasPressedThisFrame) return -1;
            }

            return 0;
        }

        // ---------- Appâts ----------

        /// <summary>Achète un paquet de l'appât numéro <paramref name="index"/> (il est accroché tout de suite).</summary>
        public bool BuyBait(int index)
        {
            BaitCatalog catalog = _progress.BaitCatalog;
            if (catalog == null || index < 0 || index >= catalog.Baits.Count)
            {
                return false;
            }

            Bait bait = catalog.Baits[index];
            if (bait.Unlimited && _progress.Baits.Owns(bait.Id))
            {
                _progress.Baits.Equip(bait.Id);
                Say($"« Ton {bait.DisplayName.ToLowerInvariant()} est déjà à toi : je te l'accroche ! »", Gold);
                return false;
            }

            if (_progress.BuyBait(bait))
            {
                Say($"« Bonne pêche ! » {bait.DisplayName} accroché à ta ligne.", Good);
                return true;
            }

            Say($"« Il te manque {bait.Price - _progress.Wallet.Coins} pièces pour ça. »", TooExpensive);
            return false;
        }

        private void RefreshBaits()
        {
            BaitCatalog catalog = _progress.BaitCatalog;
            int count = catalog != null ? catalog.Baits.Count : 0;
            if (_baitRows.Count != count)
            {
                ClearRows(_baitRows);
                float rowHeight = Mathf.Min(120f, RowsAreaHeight / Mathf.Max(1, count));
                for (int i = 0; i < count; i++)
                {
                    _baitRows.Add(BuildRow(_baitRoot, $"Appât {i + 1}", i, count, rowHeight, withDetail: true));
                }
            }

            for (int i = 0; i < _baitRows.Count; i++)
            {
                RowView row = _baitRows[i];
                Bait bait = catalog.Baits[i];
                int owned = _progress.Baits.Count(bait.Id);
                bool equipped = _progress.Baits.Equipped == bait.Id;
                row.Picture.sprite = bait.Icon;
                string pack = bait.Unlimited ? "réutilisable" : $"paquet de {bait.Uses}";
                row.Label.text = $"{i + 1}.   {bait.DisplayName}   ({pack}){(equipped ? "   — accroché" : string.Empty)}";
                row.Label.color = equipped ? Gold : Color.white;
                string have = owned == BaitBag.Unlimited ? "à toi" : owned > 0 ? $"tu en as {owned}" : "aucun";
                row.Detail.text = $"{bait.Description}   ({have})";
                row.Detail.color = Muted;
                if (bait.Unlimited && owned != 0)
                {
                    row.Price.text = "Possédé";
                    row.Price.color = Muted;
                }
                else
                {
                    row.Price.text = bait.Price > 0 ? $"{bait.Price} pièces" : "Gratuit";
                    row.Price.color = _progress.Wallet.Coins >= bait.Price ? Gold : TooExpensive;
                }
            }
        }

        private static bool LetterPressed(string letter)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            KeyControl key = keyboard.FindKeyOnCurrentKeyboardLayout(letter);
            return key != null && key.wasPressedThisFrame;
        }

        // ---------- Construction ----------

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Interface de la boutique", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 13; // devant l'en-tête du jeu, la minimap et le carnet ; sous la pause (20)
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();

            _prompt = AddText(root, "Invite", 32, TextAnchor.MiddleCenter);
            _prompt.text = "E : parler au marchand";
            UiFactory.Bold(_prompt);
            _prompt.rectTransform.anchorMin = _prompt.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _prompt.rectTransform.anchoredPosition = new Vector2(0f, 200f);
            _prompt.rectTransform.sizeDelta = new Vector2(900f, 50f);
            _prompt.gameObject.SetActive(false);

            Image panel = UiFactory.Sliced(root, "Menu du marchand", _skin != null ? _skin.Panel : null, _skin != null ? Color.white : PanelColor);
            _panel = panel.gameObject;
            UiFactory.Center(panel.rectTransform, new Vector2(0f, 0f), new Vector2(1520f, 940f));

            UiFactory.TitlePlaque(panel.rectTransform, _skin, "Boutique de Mahana", 42, new Vector2(0f, 462f), new Vector2(620f, 88f), out _);

            // Onglets.
            Image TabPlaque(string title, float x)
            {
                UiFactory.TitlePlaque(panel.rectTransform, _skin, title, 30, new Vector2(x, 384f), new Vector2(240f, 56f), out RectTransform tab);
                return tab.GetComponent<Image>();
            }

            _sellTab = TabPlaque("Vendre", -540f);
            _rodTab = TabPlaque("Cannes", -270f);
            _hookTab = TabPlaque("Hameçons", 0f);
            _baitTab = TabPlaque("Appâts", 270f);
            _bagTab = TabPlaque("Sacs, casiers", 540f);

            _feedback = AddText(panel.rectTransform, "Marchand", 28, TextAnchor.MiddleCenter);
            _feedback.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(_feedback.rectTransform, new Vector2(0f, 312f), new Vector2(1380f, 70f));

            _sellRoot = AddArea(panel.rectTransform, "Poissons");
            _gearRoot = AddArea(panel.rectTransform, "Matériel");
            _baitRoot = AddArea(panel.rectTransform, "Appâts");

            _coins = UiFactory.TitlePlaque(panel.rectTransform, _skin, "Tes pièces : 0", 30, new Vector2(0f, -352f), new Vector2(360f, 60f), out _);

            _help = AddText(panel.rectTransform, "Aide", 24, TextAnchor.MiddleCenter);
            _help.color = Muted;
            Place(_help.rectTransform, new Vector2(0f, -418f), new Vector2(1400f, 40f));
        }

        private static RectTransform AddArea(RectTransform parent, string name)
        {
            var area = new GameObject(name, typeof(RectTransform));
            var rect = area.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, new Vector2(0f, -16f), new Vector2(1380f, RowsAreaHeight));
            return rect;
        }

        private void EnsureSellRows(int count)
        {
            if (_sellRows.Count == count)
            {
                return;
            }

            ClearRows(_sellRows);
            float rowHeight = Mathf.Min(120f, RowsAreaHeight / Mathf.Max(1, count));
            for (int i = 0; i < count; i++)
            {
                _sellRows.Add(BuildRow(_sellRoot, $"Place {i + 1}", i, count, rowHeight, withDetail: false));
            }
        }

        private void EnsureGearRows(int count)
        {
            if (_gearRows.Count == count)
            {
                return;
            }

            ClearRows(_gearRows);
            float rowHeight = Mathf.Min(120f, RowsAreaHeight / Mathf.Max(1, count));
            for (int i = 0; i < count; i++)
            {
                _gearRows.Add(BuildRow(_gearRoot, $"Matériel {i + 1}", i, count, rowHeight, withDetail: true));
            }
        }

        private static void ClearRows(List<RowView> rows)
        {
            foreach (RowView row in rows)
            {
                Destroy(row.Background.gameObject);
            }

            rows.Clear();
        }

        /// <summary>Une ligne : image sur un fond d'eau, intitulé (et détail), prix à droite.</summary>
        private RowView BuildRow(RectTransform parent, string name, int index, int count, float rowHeight, bool withDetail)
        {
            float top = (count - 1) * rowHeight * 0.5f;
            Image background = UiFactory.Sliced(parent, name, _skin != null ? _skin.Slot : null, _skin != null ? Color.white : RowColor);
            Place(background.rectTransform, new Vector2(0f, top - index * rowHeight), new Vector2(1360f, rowHeight - 8f));

            float pictureHeight = rowHeight - 18f;
            Image backdrop = UiFactory.Sliced(background.rectTransform, "Fond d'eau", _skin != null ? _skin.WaterBackdrop : null, _skin != null ? Color.white : new Color(0.2f, 0.55f, 0.65f));
            Place(backdrop.rectTransform, new Vector2(-570f, 0f), new Vector2(pictureHeight * 2.1f, pictureHeight));
            Image picture = AddImage(backdrop.rectTransform, "Image", Color.white);
            picture.preserveAspect = true;
            Place(picture.rectTransform, Vector2.zero, new Vector2(pictureHeight * 1.9f, pictureHeight - 8f));

            int labelSize = rowHeight >= 100f ? 30 : 27;
            Text label = AddText(background.rectTransform, "Nom", labelSize, TextAnchor.MiddleLeft);
            Text detail = null;
            if (withDetail)
            {
                Place(label.rectTransform, new Vector2(20f, rowHeight * 0.18f), new Vector2(860f, rowHeight * 0.45f));
                detail = AddText(background.rectTransform, "Détail", 22, TextAnchor.MiddleLeft);
                detail.horizontalOverflow = HorizontalWrapMode.Overflow;
                Place(detail.rectTransform, new Vector2(20f, -rowHeight * 0.2f), new Vector2(860f, rowHeight * 0.4f));
            }
            else
            {
                Place(label.rectTransform, new Vector2(20f, 0f), new Vector2(860f, rowHeight - 10f));
            }

            Text price = AddText(background.rectTransform, "Prix", labelSize - 1, TextAnchor.MiddleRight);
            Place(price.rectTransform, new Vector2(520f, 0f), new Vector2(260f, rowHeight - 10f));
            return new RowView { Background = background, Picture = picture, Label = label, Detail = detail, Price = price };
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
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
            return UiFactory.Label(parent, name, size, alignment, Color.white);
        }
    }
}
