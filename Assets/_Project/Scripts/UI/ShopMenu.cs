using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Le marchand de la boutique : devant le comptoir, E ouvre le menu de vente.
    /// 1/2/3… vend un poisson, V vend tous les doublons, Échap (ou E) referme. Le joueur est immobile pendant ce temps.
    /// </summary>
    public sealed class ShopMenu : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.07f, 0.10f, 0.14f, 1f);
        private static readonly Color RowColor = new Color(0.14f, 0.24f, 0.30f, 1f);
        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.62f, 0.68f, 0.74f);
        private static readonly Color Good = new Color(0.55f, 0.92f, 0.55f);

        private const string Greeting = "« Bienvenue ! Je rachète tes doublons. Le plus gros de chaque nouvelle espèce, garde-le pour l'aquarium ! »";

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
            public Image Fish;
            public Text Label;
            public Text Price;
        }

        private readonly List<RowView> _rows = new List<RowView>();
        private GameObject _panel;
        private RectTransform _rowsRoot;
        private Text _prompt;
        private Text _coins;
        private Text _feedback;

        public bool IsOpen => _panel != null && _panel.activeSelf;

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
                if (PlayerAtCounter && InteractInput.PressedThisFrame())
                {
                    Open();
                }

                return;
            }

            int slot = FishingInput.SlotPressedThisFrame();
            if (slot >= 0)
            {
                SellSlot(slot);
            }
            else if (LetterPressed("v"))
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

            _feedback.text = Greeting;
            _feedback.color = Muted;
            Refresh();
        }

        public void Close()
        {
            _panel.SetActive(false);
            if (_player != null)
            {
                _player.Locked = false;
            }
        }

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
                Say($"« Ton {fish.Species.CommonName}, garde-le pour l'aquarium : c'est ta seule prise de cette espèce ! »", Gold);
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
            Inventory bag = _progress.Bag;
            EnsureRows(bag.Capacity);
            for (int i = 0; i < _rows.Count; i++)
            {
                RowView row = _rows[i];
                if (i >= bag.Count)
                {
                    row.Fish.gameObject.SetActive(false);
                    row.Label.text = $"{i + 1}.   (vide)";
                    row.Label.color = Muted;
                    row.Price.text = string.Empty;
                    continue;
                }

                CaughtFish fish = bag.Fish[i];
                row.Fish.gameObject.SetActive(true);
                row.Fish.sprite = fish.Species.Sprite;
                row.Label.text = $"{i + 1}.   {fish.Species.CommonName}  —  {fish.SizeCm:0} cm";
                row.Label.color = Color.white;

                bool sellable = FishShop.IsSellable(bag.Fish, i, _progress.Aquarium);
                row.Price.text = sellable ? $"{FishShop.PriceOf(fish)} pièces" : "Pour l'aquarium";
                row.Price.color = sellable ? Gold : Muted;
            }

            _coins.text = $"Tes pièces : {_progress.Wallet.Coins}";
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

            Image panel = UiFactory.Sliced(root, "Menu de vente", _skin != null ? _skin.Panel : null, _skin != null ? Color.white : PanelColor);
            _panel = panel.gameObject;
            UiFactory.Center(panel.rectTransform, new Vector2(0f, 30f), new Vector2(980f, 640f));

            UiFactory.TitlePlaque(panel.rectTransform, _skin, "Boutique de Mahana", 36, new Vector2(0f, 312f), new Vector2(520f, 76f), out _);

            _feedback = AddText(panel.rectTransform, "Marchand", 26, TextAnchor.MiddleCenter);
            _feedback.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(_feedback.rectTransform, new Vector2(0f, 205f), new Vector2(880f, 80f));

            var rows = new GameObject("Poissons", typeof(RectTransform));
            _rowsRoot = rows.GetComponent<RectTransform>();
            _rowsRoot.SetParent(panel.rectTransform, false);
            Place(_rowsRoot, new Vector2(0f, 0f), new Vector2(900f, 300f));

            _coins = UiFactory.TitlePlaque(panel.rectTransform, _skin, "Tes pièces : 0", 26, new Vector2(0f, -212f), new Vector2(300f, 52f), out _);

            Text help = AddText(panel.rectTransform, "Aide", 22, TextAnchor.MiddleCenter);
            help.text = "1, 2, 3 : vendre ce poisson   •   V : tout vendre   •   Échap : sortir";
            help.color = Muted;
            Place(help.rectTransform, new Vector2(0f, -270f), new Vector2(900f, 36f));
        }

        private void EnsureRows(int count)
        {
            if (_rows.Count == count)
            {
                return;
            }

            foreach (RowView row in _rows)
            {
                Destroy(row.Background.gameObject);
            }

            _rows.Clear();
            const float rowHeight = 86f;
            float top = (count - 1) * rowHeight * 0.5f;
            for (int i = 0; i < count; i++)
            {
                Image background = UiFactory.Sliced(_rowsRoot, $"Place {i + 1}", _skin != null ? _skin.Slot : null, _skin != null ? Color.white : RowColor);
                Place(background.rectTransform, new Vector2(0f, top - i * rowHeight), new Vector2(880f, rowHeight - 8f));

                // Le poisson sur un petit fond d'eau.
                Image backdrop = UiFactory.Sliced(background.rectTransform, "Fond d'eau", _skin != null ? _skin.WaterBackdrop : null, _skin != null ? Color.white : new Color(0.2f, 0.55f, 0.65f));
                Place(backdrop.rectTransform, new Vector2(-362f, 0f), new Vector2(136f, 64f));
                Image fish = AddImage(backdrop.rectTransform, "Poisson", Color.white);
                fish.preserveAspect = true;
                Place(fish.rectTransform, Vector2.zero, new Vector2(118f, 52f));

                Text label = AddText(background.rectTransform, "Nom", 24, TextAnchor.MiddleLeft);
                Place(label.rectTransform, new Vector2(-20f, 0f), new Vector2(520f, 60f));

                Text price = AddText(background.rectTransform, "Prix", 22, TextAnchor.MiddleRight);
                Place(price.rectTransform, new Vector2(282f, 0f), new Vector2(260f, 60f));

                _rows.Add(new RowView { Background = background, Fish = fish, Label = label, Price = price });
            }
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
