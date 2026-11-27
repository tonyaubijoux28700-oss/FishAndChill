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
    /// Le marchand de la boutique : devant le comptoir, E ouvre son menu, en cinq rayons (Tab / Maj + Tab ou gâchettes).
    /// « Vendre » : la liste du sac, 1, 2, 3… vend un poisson, V vend tous les doublons.
    /// « Cannes », « Hameçons », « Appâts », « Sacs » : une vitrine (<see cref="ShopShowcase"/>) montre un
    /// objet à la fois, en grand sur son présentoir ; Q / D (ou les flèches, la croix, les flèches et vignettes à la
    /// souris) font défiler, Espace (ou bouton A, ou clic sur l'objet) l'achète ou le prend en main.
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
        private const string BagGreeting = "« Plus de place dans ton sac, moins d'allers-retours ! Et une épuisette pour fouiller les bulles ? »";
        private const string BaitGreeting = "« Les poissons rares ne mordent qu'à leur leurre : le carnet te dit lequel ! »";
        private const float RowsAreaHeight = 560f;

        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private ShopBuilding _shop;
        [SerializeField] private PlayerMovement _player;

        [SerializeField, Min(0.5f), Tooltip("Distance au comptoir pour parler au marchand.")]
        private float _talkRadius = 1.3f;

        [SerializeField, Tooltip("Charte pixel art de l'interface.")]
        private UiSkin _skin;

        [Header("Vitrine")]
        [SerializeField, Tooltip("Fond de la vitrine (mur, lambris, plancher, rond de lumière).")] private Sprite _showcaseBack;
        [SerializeField, Tooltip("Socle à coussin de velours.")] private Sprite _pedestal;
        [SerializeField, Tooltip("Râtelier où les cannes se tiennent debout.")] private Sprite _rodStand;
        [SerializeField, Tooltip("Flèche pointée à droite (retournée pour la gauche).")] private Sprite _arrow;
        [SerializeField, Tooltip("Icône de l'épuisette (vendue au rayon des sacs).")] private Sprite _netIcon;

        private sealed class RowView
        {
            public Image Background;
            public Image Picture;
            public Text Label;
            public Text Price;
        }

        private readonly List<RowView> _sellRows = new List<RowView>();
        private readonly List<ShowcaseEntry> _entries = new List<ShowcaseEntry>();
        private readonly int[] _selected = new int[TabCount];
        private ShopShowcase _showcase;
        private GameObject _panel;
        private RectTransform _sellRoot;
        private Image[] _tabs;
        private RectTransform _panelRect;
        private Image _sellAll;

        // Souris : ce que le pointeur survole (-1 / faux : rien).
        private int _hoverTab = -1;
        private int _hoverRow = -1;
        private bool _hoverSellAll;
        private Text _prompt;
        private Text _coins;
        private Text _feedback;
        private Text _help;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public Tab CurrentTab { get; private set; }

        /// <summary>Numéro de l'objet montré dans la vitrine du rayon actuel.</summary>
        public int Selected => _selected[(int)CurrentTab];

        /// <summary>Les objets du rayon actuel (vide pour « Vendre »).</summary>
        public IReadOnlyList<ShowcaseEntry> Entries => _entries;

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

            _showcase.Animate(Time.unscaledTime);
            int tabStep = TabStepPressed();
            if (tabStep != 0)
            {
                ShowTab((Tab)(((int)CurrentTab + tabStep + TabCount) % TabCount));
                return;
            }

            if (InteractInput.PressedThisFrame() || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                Close();
                return;
            }

            if (!HandleMouse())
            {
                return;
            }

            if (CurrentTab == Tab.Sell)
            {
                int slot = FishingInput.SlotPressedThisFrame();
                if (slot >= 0) SellSlot(slot);
                else if (LetterPressed("v")) SellAll();
                return;
            }

            int number = FishingInput.SlotPressedThisFrame();
            if (number >= 0 && number < _entries.Count)
            {
                Select(number);
                return;
            }

            int step = ItemStepPressed();
            if (step != 0)
            {
                Select(ShowcaseMath.Step(Selected, step, _entries.Count));
                return;
            }

            if (ConfirmPressed())
            {
                BuySelected();
            }
        }

        // ---------- Souris ----------

        private static bool Over(RectTransform rect, Vector2 screen) =>
            rect != null && rect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null);

        /// <summary>
        /// Souris, menu ouvert : survol mis en valeur ; clic sur un rayon, sur un poisson à vendre, sur « Tout vendre »,
        /// sur une flèche, une vignette ou l'objet à acheter ; molette pour passer d'un objet à l'autre ; clic à côté
        /// du menu pour sortir. Renvoie faux si le menu vient d'être fermé ou si le clic a déjà servi.
        /// </summary>
        private bool HandleMouse()
        {
            _hoverTab = -1;
            _hoverRow = -1;
            _hoverSellAll = false;
            Mouse mouse = Mouse.current;
            Vector2 at = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            if (mouse != null)
            {
                for (int i = 0; i < _tabs.Length; i++)
                {
                    if (Over(_tabs[i].rectTransform, at)) _hoverTab = i;
                }

                if (CurrentTab == Tab.Sell)
                {
                    for (int i = 0; i < _sellRows.Count && i < _progress.Bag.Count; i++)
                    {
                        if (Over(_sellRows[i].Background.rectTransform, at)) _hoverRow = i;
                    }

                    _hoverSellAll = Over(_sellAll.rectTransform, at);
                }
            }

            ApplyHover();
            if (CurrentTab != Tab.Sell)
            {
                _showcase.Hover(at, mouse != null);
            }

            if (mouse == null)
            {
                return true;
            }

            float wheel = mouse.scroll.ReadValue().y;
            if (wheel != 0f && CurrentTab != Tab.Sell && _entries.Count > 1)
            {
                Select(ShowcaseMath.Step(Selected, wheel > 0f ? -1 : 1, _entries.Count));
            }

            if (!mouse.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            if (!Over(_panelRect, at))
            {
                Close();
                return false;
            }

            if (_hoverTab >= 0)
            {
                if (_hoverTab != (int)CurrentTab) ShowTab((Tab)_hoverTab);
                return false;
            }

            if (CurrentTab == Tab.Sell)
            {
                if (_hoverRow >= 0) SellSlot(_hoverRow);
                else if (_hoverSellAll) SellAll();
                return false;
            }

            int arrow = _showcase.ArrowAt(at);
            int thumb = _showcase.ThumbnailAt(at);
            if (arrow != 0) Select(ShowcaseMath.Step(Selected, arrow, _entries.Count));
            else if (thumb >= 0) Select(thumb);
            else if (_showcase.ActionAt(at)) BuySelected();
            return false;
        }

        /// <summary>Rayon, poisson ou bouton survolé : plus clair et doré.</summary>
        private void ApplyHover()
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                _tabs[i].color = i == (int)CurrentTab ? Color.white : i == _hoverTab ? new Color(1f, 0.95f, 0.8f, 0.85f) : new Color(1f, 1f, 1f, 0.45f);
            }

            Color rowBase = _skin != null ? Color.white : RowColor;
            for (int i = 0; i < _sellRows.Count; i++)
            {
                _sellRows[i].Background.color = i == _hoverRow ? Color.Lerp(rowBase, Gold, 0.45f) : rowBase;
            }

            _sellAll.color = _hoverSellAll ? Color.Lerp(Color.white, Gold, 0.55f) : Color.white;
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

        /// <summary>Affiche le rayon demandé : la vente, ou la vitrine d'une famille d'objets (sur l'objet en main).</summary>
        public void ShowTab(Tab tab)
        {
            CurrentTab = tab;
            _sellRoot.gameObject.SetActive(tab == Tab.Sell);
            _showcase.Visible = tab != Tab.Sell;
            _sellAll.gameObject.SetActive(tab == Tab.Sell);
            ApplyHover();

            const string end = "   •   Tab ou clic : rayon   •   Échap : sortir";
            string browse = $"{KeyNames.Of(Key.A, "Q")} / {KeyNames.Of(Key.D, "D")} ou molette : objet précédent / suivant   •   Espace ou clic : acheter";
            switch (tab)
            {
                case Tab.Sell: _help.text = "Clic ou 1, 2, 3… : vendre ce poisson   •   V : tout vendre" + end; Say(Greeting, Muted); break;
                case Tab.Rods: _help.text = browse + end; Say(GearGreeting, Muted); break;
                case Tab.Hooks: _help.text = browse + end; Say(HookGreeting, Muted); break;
                case Tab.Baits: _help.text = browse + end; Say(BaitGreeting, Muted); break;
                default: _help.text = browse + end; Say(BagGreeting, Muted); break;
            }

            BuildEntries();
            _selected[(int)tab] = Mathf.Clamp(DefaultSelection(tab), 0, Mathf.Max(0, _entries.Count - 1));
            Refresh();
        }

        /// <summary>Montre l'objet <paramref name="index"/> du rayon actuel.</summary>
        public void Select(int index)
        {
            if (CurrentTab == Tab.Sell || _entries.Count == 0)
            {
                return;
            }

            _selected[(int)CurrentTab] = Mathf.Clamp(index, 0, _entries.Count - 1);
            Refresh();
        }

        /// <summary>Achète (ou prend en main) l'objet montré dans la vitrine.</summary>
        public void BuySelected()
        {
            if (CurrentTab == Tab.Sell || Selected >= _entries.Count)
            {
                return;
            }

            _entries[Selected].Buy?.Invoke();
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
                Say($"« Marché conclu ! » {fish.Species.CommonName} {SpeciesGrammar.Agree("vendu", fish.Species.Feminine)} : +{earned} pièces", Good);
            }
            else
            {
                Say($"« {UpperFirst(fish.Species.Your())}, garde-{SpeciesGrammar.Pronoun(fish.Species.Feminine)} pour l'aquarium ! »", Gold);
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
                Say($"« Tu l'as déjà, je te l'accroche ! » À ta ligne : {bait.DisplayName}", Gold);
                return false;
            }

            if (_progress.BuyBait(bait))
            {
                Say($"« Bonne pêche ! » À ta ligne : {bait.DisplayName}", Good);
                return true;
            }

            Say($"« Il te manque {bait.Price - _progress.Wallet.Coins} pièces pour ça. »", TooExpensive);
            return false;
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

        // ---------- Les objets de chaque rayon ----------

        private void BuildEntries()
        {
            _entries.Clear();
            switch (CurrentTab)
            {
                case Tab.Rods: AddItems(GearCategory.Rod); break;
                case Tab.Hooks: AddItems(GearCategory.Hook); break;
                case Tab.Baits: AddBaits(); break;
                case Tab.Bag: AddBags(); AddNet(); break;
            }
        }

        /// <summary>Le rayon s'ouvre sur l'objet en main (canne, hameçon, appât) ou sur le prochain sac à acheter.</summary>
        private int DefaultSelection(Tab tab)
        {
            switch (tab)
            {
                case Tab.Rods: return _progress.Gear.Level(GearCategory.Rod);
                case Tab.Hooks: return _progress.Gear.Level(GearCategory.Hook);
                case Tab.Baits:
                    BaitCatalog catalog = _progress.BaitCatalog;
                    for (int i = 0; catalog != null && i < catalog.Baits.Count; i++)
                    {
                        if (catalog.Baits[i].Id == _progress.Baits.Equipped) return i;
                    }

                    return 0;
                case Tab.Bag:
                    GearLine bags = _progress.GearCatalog != null ? _progress.GearCatalog.Line(GearCategory.Bag) : null;
                    int level = _progress.Gear.Level(GearCategory.Bag);
                    return bags != null && level < bags.MaxLevel ? level + 1 : _entries.Count - 1;
                default:
                    return 0;
            }
        }

        /// <summary>Cannes et hameçons : chacun à acheter, ou à prendre en main s'il est déjà à toi.</summary>
        private void AddItems(GearCategory category)
        {
            GearLine line = _progress.GearCatalog != null ? _progress.GearCatalog.Line(category) : null;
            if (line == null) return;
            int equipped = _progress.Gear.Level(category);
            for (int i = 0; i < line.Levels.Count; i++)
            {
                GearLevel item = line.Levels[i];
                bool owned = _progress.Gear.Owns(category, i);
                int index = i;
                var entry = new ShowcaseEntry
                {
                    Picture = category == GearCategory.Rod ? item.Illustration : item.Icon != null ? item.Icon : line.Icon,
                    Tall = category == GearCategory.Rod,
                    Name = item.Name,
                    Detail = EffectText(category, item),
                    Buy = () => BuyItem(category, index),
                };

                if (i == equipped)
                {
                    entry.Status = "En main";
                    entry.StatusColor = Gold;
                }
                else if (owned)
                {
                    entry.Status = "À toi";
                    entry.StatusColor = Good;
                    entry.Action = "prendre en main";
                }
                else
                {
                    Price(entry, item.Price);
                }

                _entries.Add(entry);
            }
        }

        private static string UpperFirst(string text) => string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);

        private void AddBaits()
        {
            BaitCatalog catalog = _progress.BaitCatalog;
            for (int i = 0; catalog != null && i < catalog.Baits.Count; i++)
            {
                Bait bait = catalog.Baits[i];
                int owned = _progress.Baits.Count(bait.Id);
                bool equipped = _progress.Baits.Equipped == bait.Id;
                int index = i;
                var entry = new ShowcaseEntry
                {
                    Picture = bait.Icon,
                    Name = bait.DisplayName,
                    Detail = $"{bait.Description}\n{UpperFirst(LureRules.Name(bait.LureType))}, {(bait.Unlimited ? "réutilisable" : $"paquet de {bait.Uses}")}. {bait.Model}",
                    Buy = () => BuyBait(index),
                };

                string have = owned == BaitBag.Unlimited ? "À toi" : owned > 0 ? $"Tu en as {owned}" : "Tu n'en as pas";
                entry.Status = equipped ? $"{have} — accroché" : have;
                entry.StatusColor = equipped ? Gold : owned != 0 ? Good : Muted;
                if (bait.Unlimited && owned != 0)
                {
                    entry.Action = equipped ? null : "l'accrocher";
                }
                else
                {
                    Price(entry, bait.Price);
                    entry.Action = bait.Price <= 0 ? "prendre" : bait.Unlimited ? "acheter" : "acheter un paquet";
                }

                _entries.Add(entry);
            }
        }

        /// <summary>Tous les sacs, dans l'ordre : les anciens, le tien, le suivant à acheter, et ceux d'après.</summary>
        private void AddBags()
        {
            GearLine line = _progress.GearCatalog != null ? _progress.GearCatalog.Line(GearCategory.Bag) : null;
            if (line == null) return;
            int level = _progress.Gear.Level(GearCategory.Bag);
            for (int i = 0; i < line.Levels.Count; i++)
            {
                GearLevel bag = line.Levels[i];
                var entry = new ShowcaseEntry
                {
                    Picture = bag.Icon != null ? bag.Icon : line.Icon,
                    Name = bag.Name,
                    Detail = $"Sac de {EffectText(GearCategory.Bag, bag)}.",
                };

                if (i < level)
                {
                    entry.Status = "Ton ancien sac";
                    entry.StatusColor = Muted;
                }
                else if (i == level)
                {
                    entry.Status = "Ton sac";
                    entry.StatusColor = Gold;
                }
                else if (i == level + 1)
                {
                    Price(entry, bag.Price);
                    entry.Buy = () => BuyGear(GearCategory.Bag);
                }
                else
                {
                    entry.Status = $"D'abord : {line.Levels[i - 1].Name}";
                    entry.StatusColor = Muted;
                    entry.Price = $"{bag.Price} pièces";
                    entry.PriceColor = Muted;
                }

                _entries.Add(entry);
            }
        }

        /// <summary>L'épuisette, à la suite des sacs : achetée une fois pour toutes.</summary>
        private void AddNet()
        {
            var entry = new ShowcaseEntry
            {
                Picture = _netIcon,
                Name = "Épuisette",
                Detail = "Pour la pêche à pied : face aux bulles, ramasse crabes, coquillages, oursins et autres bêtes du fond.",
            };

            if (_progress.HasNet)
            {
                entry.Status = "Ton épuisette";
                entry.StatusColor = Gold;
            }
            else
            {
                Price(entry, PlayerProgress.NetPrice);
                entry.Buy = () => BuyNet();
            }

            _entries.Add(entry);
        }

        /// <summary>Achète l'épuisette.</summary>
        public bool BuyNet()
        {
            if (_progress.HasNet)
            {
                Say("« Tu en as déjà une, elle est solide ! »", Gold);
                return false;
            }

            if (!_progress.BuyNet())
            {
                Say($"« Il te manque {PlayerProgress.NetPrice - _progress.Wallet.Coins} pièces pour l'épuisette. »", TooExpensive);
                return false;
            }

            Say("« Bonne pêche à pied ! Cherche les bulles près de la rive, il y a toujours quelque chose qui fouille le fond. »", Good);
            return true;
        }

        private void Price(ShowcaseEntry entry, int price)
        {
            entry.Price = price > 0 ? $"{price} pièces" : "Gratuit";
            entry.PriceColor = _progress.Wallet.Coins >= price ? UiSkin.PlaqueText : TooExpensive;
            entry.Action ??= "acheter";
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
                BuildEntries();
                Refresh();
            }
        }

        private void Refresh()
        {
            RefreshSell();
            if (CurrentTab != Tab.Sell)
            {
                _showcase.Show(_entries, Selected);
            }

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

        // ---------- Touches ----------

        /// <summary>Rayon suivant (Tab, gâchette droite) ou précédent (Maj + Tab, gâchette gauche) ; 0 sinon.</summary>
        private static int TabStepPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                return keyboard.shiftKey.isPressed ? -1 : 1;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.rightShoulder.wasPressedThisFrame) return 1;
                if (gamepad.leftShoulder.wasPressedThisFrame) return -1;
            }

            return 0;
        }

        /// <summary>Objet suivant (D, flèche droite, croix droite) ou précédent (Q, flèche gauche, croix gauche).</summary>
        private static int ItemStepPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                // Touches physiques (Q / D en AZERTY, A / D en QWERTY), comme le déplacement.
                if (keyboard[Key.D].wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) return 1;
                if (keyboard[Key.A].wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) return -1;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.dpad.right.wasPressedThisFrame) return 1;
                if (gamepad.dpad.left.wasPressedThisFrame) return -1;
            }

            return 0;
        }

        private static bool ConfirmPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
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
            _panelRect = panel.rectTransform;
            UiFactory.Center(panel.rectTransform, new Vector2(0f, 0f), new Vector2(1520f, 940f));

            UiFactory.TitlePlaque(panel.rectTransform, _skin, "Boutique de Mahana", 42, new Vector2(0f, 462f), new Vector2(620f, 88f), out _);

            // Les rayons.
            string[] titles = { "Vendre", "Cannes", "Hameçons", "Leurres", "Sacs" };
            _tabs = new Image[titles.Length];
            for (int i = 0; i < titles.Length; i++)
            {
                UiFactory.TitlePlaque(panel.rectTransform, _skin, titles[i], 30, new Vector2(-540f + i * 270f, 384f), new Vector2(240f, 56f), out RectTransform tab);
                _tabs[i] = tab.GetComponent<Image>();
            }

            _feedback = AddText(panel.rectTransform, "Marchand", 28, TextAnchor.MiddleCenter);
            _feedback.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(_feedback.rectTransform, new Vector2(0f, 312f), new Vector2(1380f, 70f));

            var sellArea = new GameObject("Poissons", typeof(RectTransform));
            _sellRoot = sellArea.GetComponent<RectTransform>();
            _sellRoot.SetParent(panel.rectTransform, false);
            Place(_sellRoot, new Vector2(0f, -16f), new Vector2(1380f, RowsAreaHeight));

            _showcase = new ShopShowcase(panel.rectTransform, _skin, _showcaseBack, _pedestal, _rodStand, _arrow);

            _coins = UiFactory.TitlePlaque(panel.rectTransform, _skin, "Tes pièces : 0", 30, new Vector2(0f, -352f), new Vector2(360f, 60f), out _);

            // Bouton « Tout vendre » (rayon de la vente), à côté des pièces.
            UiFactory.TitlePlaque(panel.rectTransform, _skin, "Tout vendre (V)", 30, new Vector2(470f, -352f), new Vector2(320f, 60f), out RectTransform sellAll);
            _sellAll = sellAll.GetComponent<Image>();

            _help = AddText(panel.rectTransform, "Aide", 24, TextAnchor.MiddleCenter);
            _help.color = Muted;
            Place(_help.rectTransform, new Vector2(0f, -418f), new Vector2(1400f, 40f));
        }

        private void EnsureSellRows(int count)
        {
            if (_sellRows.Count == count)
            {
                return;
            }

            foreach (RowView row in _sellRows)
            {
                Destroy(row.Background.gameObject);
            }

            _sellRows.Clear();
            float rowHeight = Mathf.Min(120f, RowsAreaHeight / Mathf.Max(1, count));
            for (int i = 0; i < count; i++)
            {
                _sellRows.Add(BuildRow(_sellRoot, $"Place {i + 1}", i, count, rowHeight));
            }
        }

        /// <summary>Une ligne de la vente : le poisson sur un fond d'eau, son nom, son prix à droite.</summary>
        private RowView BuildRow(RectTransform parent, string name, int index, int count, float rowHeight)
        {
            float top = (count - 1) * rowHeight * 0.5f;
            Image background = UiFactory.Sliced(parent, name, _skin != null ? _skin.Slot : null, _skin != null ? Color.white : RowColor);
            Place(background.rectTransform, new Vector2(0f, top - index * rowHeight), new Vector2(1360f, rowHeight - 8f));

            float pictureHeight = rowHeight - 18f;
            Image backdrop = UiFactory.Sliced(background.rectTransform, "Fond d'eau", _skin != null ? _skin.WaterBackdrop : null, _skin != null ? Color.white : new Color(0.2f, 0.55f, 0.65f));
            Place(backdrop.rectTransform, new Vector2(-570f, 0f), new Vector2(pictureHeight * 2.1f, pictureHeight));
            Image picture = UiFactory.Plain(backdrop.rectTransform, "Image", Color.white);
            picture.preserveAspect = true;
            Place(picture.rectTransform, Vector2.zero, new Vector2(pictureHeight * 1.9f, pictureHeight - 8f));

            int labelSize = rowHeight >= 100f ? 30 : 27;
            Text label = AddText(background.rectTransform, "Nom", labelSize, TextAnchor.MiddleLeft);
            Place(label.rectTransform, new Vector2(20f, 0f), new Vector2(860f, rowHeight - 10f));
            Text price = AddText(background.rectTransform, "Prix", labelSize - 1, TextAnchor.MiddleRight);
            Place(price.rectTransform, new Vector2(520f, 0f), new Vector2(260f, rowHeight - 10f));
            return new RowView { Background = background, Picture = picture, Label = label, Price = price };
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private Text AddText(RectTransform parent, string name, int size, TextAnchor alignment)
        {
            return UiFactory.Label(parent, name, size, alignment, Color.white);
        }
    }
}
