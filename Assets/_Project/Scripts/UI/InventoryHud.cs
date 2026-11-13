using System.Collections.Generic;
using System.Text;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.World;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Interface du sac (en bas à gauche, une case par place), des pièces (en haut à droite),
    /// de l'invite de dépôt devant l'aquarium et du résumé de dépôt.
    /// </summary>
    public sealed class InventoryHud : MonoBehaviour
    {
        // Opaques : avec une bordure (Outline), un fond translucide laisse transparaître la couleur du bord.
        private static readonly Color PanelColor = new Color(0.07f, 0.10f, 0.14f, 1f);
        private static readonly Color SlotColor = new Color(0.16f, 0.36f, 0.44f, 1f);
        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.78f, 0.82f, 0.86f);

        private const float SlotWidth = 132f;
        private const float SlotHeight = 84f;
        private const float SlotGap = 10f;

        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private FishingController _fishing;
        [SerializeField] private AquariumStation _station;

        [SerializeField, Min(1f)] private float _toastSeconds = 5f;

        private sealed class SlotView
        {
            public Image Frame;
            public Outline Border;
            public Image Fish;
            public Text Size;
            public Text Number;
        }

        private readonly List<SlotView> _slots = new List<SlotView>();
        private Font _font;
        private RectTransform _bagPanel;
        private Text _bagTitle;
        private Text _coins;
        private Text _prompt;
        private GameObject _toast;
        private Text _toastText;
        private float _toastTimer;
        private int _builtCapacity = -1;

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
            bool choosing = _fishing != null && _fishing.State == FishingState.BagFull;
            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 6f);
            foreach (SlotView slot in _slots)
            {
                slot.Border.effectColor = choosing ? new Color(Gold.r, Gold.g, Gold.b, pulse) : new Color(1f, 1f, 1f, 0.25f);
                slot.Number.color = choosing ? Gold : Muted;
            }

            if (_station != null && _station.PlayerInRange)
            {
                _prompt.gameObject.SetActive(true);
                _prompt.text = _progress.Bag.Count > 0
                    ? "E : déposer tes poissons à l'aquarium"
                    : "Aquarium — ton sac est vide";
            }
            else
            {
                _prompt.gameObject.SetActive(false);
            }

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

            _bagTitle.text = $"Sac {bag.Count}/{bag.Capacity}";
            _bagTitle.color = bag.IsFull ? Gold : Color.white;

            for (int i = 0; i < _slots.Count; i++)
            {
                SlotView slot = _slots[i];
                bool filled = i < bag.Count;
                slot.Fish.gameObject.SetActive(filled);
                slot.Size.gameObject.SetActive(filled);
                if (filled)
                {
                    CaughtFish fish = bag.Fish[i];
                    slot.Fish.sprite = fish.Species.Sprite;
                    slot.Fish.color = fish.Species.Sprite != null ? Color.white : fish.Species.PlaceholderColor;
                    slot.Size.text = $"{fish.SizeCm:0} cm";
                    slot.Size.color = fish.Species.Rarity.DisplayColor();
                }
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
                    text.Append($"<color=#FFD54D>+{result.NewSpecies.Count} nouvelle{(result.NewSpecies.Count > 1 ? "s" : string.Empty)} espèce{(result.NewSpecies.Count > 1 ? "s" : string.Empty)} dans l'aquarium</color>\n");
                    var names = new List<string>();
                    foreach (CaughtFish fish in result.NewSpecies)
                    {
                        names.Add(fish.Species.CommonName);
                    }

                    text.Append(string.Join(", ", names)).Append('\n');
                }

                if (result.Sold.Count > 0)
                {
                    text.Append($"{result.Sold.Count} doublon{(result.Sold.Count > 1 ? "s" : string.Empty)} vendu{(result.Sold.Count > 1 ? "s" : string.Empty)} : <color=#FFD54D>+{result.CoinsEarned} pièces</color>\n");
                }

                text.Append($"Aquarium : {_progress.Aquarium.SpeciesCount} espèce{(_progress.Aquarium.SpeciesCount > 1 ? "s" : string.Empty)}");
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

            // Sac : panneau en bas à gauche.
            Image bag = AddImage(root, "Sac", PanelColor);
            _bagPanel = bag.rectTransform;
            _bagPanel.anchorMin = _bagPanel.anchorMax = Vector2.zero;
            _bagPanel.pivot = Vector2.zero;
            _bagPanel.anchoredPosition = new Vector2(24f, 24f);
            Outline bagBorder = bag.gameObject.AddComponent<Outline>();
            bagBorder.effectColor = new Color(0.95f, 0.85f, 0.55f, 0.9f);
            bagBorder.effectDistance = new Vector2(3f, -3f);

            _bagTitle = AddText(_bagPanel, "Titre", 26, TextAnchor.UpperLeft);
            _bagTitle.fontStyle = FontStyle.Bold;
            _bagTitle.rectTransform.anchorMin = _bagTitle.rectTransform.anchorMax = new Vector2(0f, 1f);
            _bagTitle.rectTransform.pivot = new Vector2(0f, 1f);
            _bagTitle.rectTransform.anchoredPosition = new Vector2(12f, -6f);
            _bagTitle.rectTransform.sizeDelta = new Vector2(300f, 34f);

            // Pièces : en haut à droite.
            _coins = AddText(root, "Pièces", 32, TextAnchor.UpperRight);
            _coins.fontStyle = FontStyle.Bold;
            _coins.color = Gold;
            _coins.rectTransform.anchorMin = _coins.rectTransform.anchorMax = new Vector2(1f, 1f);
            _coins.rectTransform.pivot = new Vector2(1f, 1f);
            _coins.rectTransform.anchoredPosition = new Vector2(-32f, -28f);
            _coins.rectTransform.sizeDelta = new Vector2(400f, 44f);

            // Invite de dépôt : en bas au centre.
            _prompt = AddText(root, "Invite", 32, TextAnchor.MiddleCenter);
            _prompt.fontStyle = FontStyle.Bold;
            _prompt.rectTransform.anchorMin = _prompt.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _prompt.rectTransform.anchoredPosition = new Vector2(0f, 150f);
            _prompt.rectTransform.sizeDelta = new Vector2(900f, 50f);
            _prompt.gameObject.SetActive(false);

            // Résumé de dépôt.
            Image toast = AddImage(root, "Résumé", PanelColor);
            _toast = toast.gameObject;
            toast.rectTransform.anchorMin = toast.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            toast.rectTransform.pivot = new Vector2(0.5f, 1f);
            toast.rectTransform.anchoredPosition = new Vector2(0f, -280f);
            toast.rectTransform.sizeDelta = new Vector2(820f, 170f);
            Outline toastBorder = _toast.AddComponent<Outline>();
            toastBorder.effectColor = Gold;
            toastBorder.effectDistance = new Vector2(3f, -3f);
            _toastText = AddText(toast.rectTransform, "Texte", 28, TextAnchor.MiddleCenter);
            _toastText.supportRichText = true;
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
            _bagPanel.sizeDelta = new Vector2(SlotGap + capacity * (SlotWidth + SlotGap), SlotHeight + 56f);

            for (int i = 0; i < capacity; i++)
            {
                Image frame = AddImage(_bagPanel, $"Place {i + 1}", SlotColor);
                RectTransform rect = frame.rectTransform;
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.anchoredPosition = new Vector2(SlotGap + i * (SlotWidth + SlotGap), SlotGap);
                rect.sizeDelta = new Vector2(SlotWidth, SlotHeight);
                Outline border = frame.gameObject.AddComponent<Outline>();
                border.effectDistance = new Vector2(2f, -2f);

                Image fish = AddImage(rect, "Poisson", Color.white);
                fish.preserveAspect = true;
                fish.rectTransform.anchoredPosition = new Vector2(0f, 8f);
                fish.rectTransform.sizeDelta = new Vector2(SlotWidth - 16f, SlotHeight - 30f);

                Text size = AddText(rect, "Taille", 20, TextAnchor.LowerCenter);
                size.rectTransform.anchorMin = new Vector2(0f, 0f);
                size.rectTransform.anchorMax = new Vector2(1f, 0f);
                size.rectTransform.pivot = new Vector2(0.5f, 0f);
                size.rectTransform.anchoredPosition = new Vector2(0f, 2f);
                size.rectTransform.sizeDelta = new Vector2(0f, 24f);

                Text number = AddText(rect, "Numéro", 20, TextAnchor.UpperLeft);
                number.text = (i + 1).ToString();
                number.fontStyle = FontStyle.Bold;
                number.rectTransform.anchorMin = number.rectTransform.anchorMax = new Vector2(0f, 1f);
                number.rectTransform.pivot = new Vector2(0f, 1f);
                number.rectTransform.anchoredPosition = new Vector2(6f, -2f);
                number.rectTransform.sizeDelta = new Vector2(30f, 24f);

                _slots.Add(new SlotView { Frame = frame, Border = border, Fish = fish, Size = size, Number = number });
            }
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
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            return text;
        }
    }
}
