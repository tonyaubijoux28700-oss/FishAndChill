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
        public enum Tab { Sell, Gear }

        private static readonly Color PanelColor = new Color(0.07f, 0.10f, 0.14f, 1f);
        private static readonly Color RowColor = new Color(0.14f, 0.24f, 0.30f, 1f);
        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.62f, 0.68f, 0.74f);
        private static readonly Color Good = new Color(0.55f, 0.92f, 0.55f);
        private static readonly Color TooExpensive = new Color(0.90f, 0.50f, 0.45f);

        private const string Greeting = "« Bienvenue ! Je rachète tes doublons. Le plus gros de chaque nouvelle espèce, garde-le pour l'aquarium ! »";
        private const string GearGreeting = "« Du bon matériel, ça change tout ! Chaque achat remplace l'ancien. »";
        private const float RowsAreaHeight = 320f;

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
        private GameObject _panel;
        private RectTransform _sellRoot;
        private RectTransform _gearRoot;
        private Image _sellTab;
        private Image _gearTab;
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

            if (TabSwitchPressed())
            {
                ShowTab(CurrentTab == Tab.Sell ? Tab.Gear : Tab.Sell);
                return;
            }

            int slot = FishingInput.SlotPressedThisFrame();
            if (slot >= 0)
            {
                if (CurrentTab == Tab.Sell)
                {
                    SellSlot(slot);
                }
                else
                {
                    BuyGear(slot);
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
            _gearRoot.gameObject.SetActive(tab == Tab.Gear);
            _sellTab.color = tab == Tab.Sell ? Color.white : new Color(0.55f, 0.55f, 0.6f);
            _gearTab.color = tab == Tab.Gear ? Color.white : new Color(0.55f, 0.55f, 0.6f);
            _help.text = tab == Tab.Sell
                ? "1, 2, 3… : vendre ce poisson   •   V : tout vendre   •   Tab : matériel   •   Échap : sortir"
                : "1 à 4 : acheter   •   Tab : vendre   •   Échap : sortir";
            Say(tab == Tab.Sell ? Greeting : GearGreeting, Muted);
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
            int earned = FishShop.Sell(bag, slot, _progress.Wallet, _progress.Aquarium);
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
            int earned = FishShop.SellAll(_progress.Bag, _progress.Wallet, _progress.Aquarium);
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

        /// <summary>Achète le niveau suivant de la famille numéro <paramref name="index"/> du catalogue (0 = canne).</summary>
        public GearPurchase BuyGear(int index)
        {
            GearCatalog catalog = _progress.GearCatalog;
            if (catalog == null || index < 0 || index >= catalog.Lines.Count)
            {
                return GearPurchase.Unavailable;
            }

            GearLine line = catalog.Lines[index];
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
                    return $"zone de réussite +{level.Value * 100f:0} %, curseur plus lent";
                case GearCategory.Hook:
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
                row.Price.text = sellable ? $"{FishShop.PriceOf(fish)} pièces" : "Pour l'aquarium";
                row.Price.color = sellable ? Gold : Muted;
            }
        }

        private void RefreshGear()
        {
            GearCatalog catalog = _progress.GearCatalog;
            int count = catalog != null ? catalog.Lines.Count : 0;
            EnsureGearRows(count);
            for (int i = 0; i < _gearRows.Count; i++)
            {
                RowView row = _gearRows[i];
                GearLine line = catalog.Lines[i];
                int level = _progress.Gear.Level(line.Category);
                GearLevel current = line.At(level);
                GearLevel next = GearShop.NextLevel(line, _progress.Gear);

                row.Picture.sprite = line.Icon;
                row.Label.text = $"{i + 1}.   {current?.Name}   (niv. {level}/{line.MaxLevel})";
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

        private static bool TabSwitchPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.tabKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame))
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && (gamepad.leftShoulder.wasPressedThisFrame || gamepad.rightShoulder.wasPressedThisFrame);
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
            canvas.sortingOrder = 9;
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
            UiFactory.Center(panel.rectTransform, new Vector2(0f, 20f), new Vector2(1040f, 700f));

            UiFactory.TitlePlaque(panel.rectTransform, _skin, "Boutique de Mahana", 36, new Vector2(0f, 342f), new Vector2(520f, 76f), out _);

            // Onglets.
            UiFactory.TitlePlaque(panel.rectTransform, _skin, "Vendre", 24, new Vector2(-120f, 272f), new Vector2(200f, 44f), out RectTransform sellTab);
            UiFactory.TitlePlaque(panel.rectTransform, _skin, "Matériel", 24, new Vector2(120f, 272f), new Vector2(200f, 44f), out RectTransform gearTab);
            _sellTab = sellTab.GetComponent<Image>();
            _gearTab = gearTab.GetComponent<Image>();

            _feedback = AddText(panel.rectTransform, "Marchand", 24, TextAnchor.MiddleCenter);
            _feedback.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(_feedback.rectTransform, new Vector2(0f, 212f), new Vector2(940f, 64f));

            _sellRoot = AddArea(panel.rectTransform, "Poissons");
            _gearRoot = AddArea(panel.rectTransform, "Matériel");

            _coins = UiFactory.TitlePlaque(panel.rectTransform, _skin, "Tes pièces : 0", 26, new Vector2(0f, -232f), new Vector2(300f, 52f), out _);

            _help = AddText(panel.rectTransform, "Aide", 20, TextAnchor.MiddleCenter);
            _help.color = Muted;
            Place(_help.rectTransform, new Vector2(0f, -292f), new Vector2(980f, 34f));
        }

        private static RectTransform AddArea(RectTransform parent, string name)
        {
            var area = new GameObject(name, typeof(RectTransform));
            var rect = area.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, new Vector2(0f, 2f), new Vector2(940f, RowsAreaHeight));
            return rect;
        }

        private void EnsureSellRows(int count)
        {
            if (_sellRows.Count == count)
            {
                return;
            }

            ClearRows(_sellRows);
            float rowHeight = Mathf.Min(86f, RowsAreaHeight / Mathf.Max(1, count));
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
            float rowHeight = Mathf.Min(80f, RowsAreaHeight / Mathf.Max(1, count));
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
            Place(background.rectTransform, new Vector2(0f, top - index * rowHeight), new Vector2(920f, rowHeight - 6f));

            float pictureHeight = rowHeight - 18f;
            Image backdrop = UiFactory.Sliced(background.rectTransform, "Fond d'eau", _skin != null ? _skin.WaterBackdrop : null, _skin != null ? Color.white : new Color(0.2f, 0.55f, 0.65f));
            Place(backdrop.rectTransform, new Vector2(-380f, 0f), new Vector2(pictureHeight * 2.1f, pictureHeight));
            Image picture = AddImage(backdrop.rectTransform, "Image", Color.white);
            picture.preserveAspect = true;
            Place(picture.rectTransform, Vector2.zero, new Vector2(pictureHeight * 1.9f, pictureHeight - 8f));

            int labelSize = rowHeight >= 70f ? 23 : 19;
            Text label = AddText(background.rectTransform, "Nom", labelSize, TextAnchor.MiddleLeft);
            Text detail = null;
            if (withDetail)
            {
                Place(label.rectTransform, new Vector2(10f, rowHeight * 0.17f), new Vector2(560f, rowHeight * 0.45f));
                detail = AddText(background.rectTransform, "Détail", 17, TextAnchor.MiddleLeft);
                detail.horizontalOverflow = HorizontalWrapMode.Overflow;
                Place(detail.rectTransform, new Vector2(10f, -rowHeight * 0.2f), new Vector2(560f, rowHeight * 0.4f));
            }
            else
            {
                Place(label.rectTransform, new Vector2(10f, 0f), new Vector2(560f, rowHeight - 10f));
            }

            Text price = AddText(background.rectTransform, "Prix", labelSize - 1, TextAnchor.MiddleRight);
            Place(price.rectTransform, new Vector2(330f, 0f), new Vector2(220f, rowHeight - 10f));
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
