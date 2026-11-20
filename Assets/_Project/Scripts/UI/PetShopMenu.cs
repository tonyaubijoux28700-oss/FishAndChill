using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Pets;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// La boutique du vendeur de l'animalerie : devant le comptoir, « E : parler au vendeur » ouvre une fenêtre avec une
    /// carte par animal (portrait, nom, bonus, prix ou « Avec toi ») et une carte « Aucun animal ». Gauche / Droite pour
    /// choisir, E ou Espace : acheter (il accompagne aussitôt le joueur) ou prendre avec soi un animal déjà acheté,
    /// Échap : fermer. Le sac ne s'ouvre pas pendant ce temps (<see cref="Busy"/>).
    /// </summary>
    public sealed class PetShopMenu : MonoBehaviour
    {
        private const float CardWidth = 330f;
        private const float CardHeight = 400f;

        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.66f, 0.72f, 0.78f);
        private static readonly Color Good = new Color(0.55f, 0.95f, 0.55f);

        [SerializeField] private UiSkin _skin;
        [SerializeField] private PlayerMovement _player;
        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private PetShopBuilding _shop;

        private Text _prompt;
        private GameObject _window;
        private Text _message;
        private readonly List<Card> _cards = new List<Card>();
        private int _selected;
        private int _openedFrame = -1;

        private sealed class Card
        {
            public Image Frame;
            public Image Portrait;
            public Text Name;
            public Text Bonus;
            public Text Price;
        }

        /// <summary>La fenêtre est ouverte, ou le joueur est devant le comptoir (E y est réservé).</summary>
        public static bool Busy { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Busy = false;

        public bool IsOpen => _window != null && _window.activeSelf;

        private IReadOnlyList<PetDefinition> Pets => _progress != null && _progress.PetCatalog != null ? _progress.PetCatalog.Pets : new PetDefinition[0];

        private void Start() => BuildUi();

        private void OnDisable() => Busy = false;

        private void Update()
        {
            if (IsOpen)
            {
                Busy = true;
                _prompt.gameObject.SetActive(false);
                if (Time.frameCount == _openedFrame) return;
                if (PauseInput.TogglePressedThisFrame() || PauseInput.BackPressedThisFrame())
                {
                    Close();
                    return;
                }

                (int dx, int dy) = IndexInput.NavigationThisFrame();
                int step = dx != 0 ? dx : -dy;
                if (step != 0)
                {
                    _selected = Mathf.Clamp(_selected + step, 0, Pets.Count);
                    Refresh();
                }
                else if (InteractInput.PressedThisFrame() || PauseInput.ConfirmPressedThisFrame())
                {
                    Choose();
                }

                return;
            }

            bool atCounter = AtCounter();
            Busy = atCounter;
            _prompt.gameObject.SetActive(atCounter);
            if (atCounter && InteractInput.PressedThisFrame()) Open();
        }

        private bool AtCounter()
        {
            if (_shop == null || _player == null || _player.Locked || !_shop.IsPlayerInside) return false;
            Vector2 front = _shop.CounterSpot + Vector2.down * 0.5f;
            return Vector2.Distance(_player.transform.position, front) < 1.4f;
        }

        /// <summary>Ouvre la boutique (public pour les tests).</summary>
        public void Open()
        {
            _progress.EnsureCreated();
            _selected = 0;
            _openedFrame = Time.frameCount;
            _window.SetActive(true);
            if (_player != null) _player.Locked = true;
            Busy = true;
            _message.text = "Bienvenue ! Un compagnon pour tes parties de pêche ?";
            _message.color = Muted;
            Refresh();
        }

        public void Close()
        {
            _window.SetActive(false);
            if (_player != null) _player.Locked = false;
        }

        /// <summary>Achète ou prend avec soi l'animal choisi ; la dernière carte laisse l'animal à la maison.</summary>
        public void Choose()
        {
            PetCollection pets = _progress.Pets;
            if (_selected >= Pets.Count)
            {
                pets.Equip(null);
                Say("Ton compagnon t'attendra à la maison.", Muted);
            }
            else
            {
                PetDefinition pet = Pets[_selected];
                if (pets.Owns(pet.Id))
                {
                    pets.Equip(pet.Id);
                    Say($"{pet.DisplayName} t'accompagne !", Good);
                }
                else
                {
                    switch (pets.TryBuy(pet.Id, pet.Price, _progress.Wallet))
                    {
                        case PetPurchase.Bought: Say($"Prends bien soin de {pet.DisplayName} ! Il te suit déjà.", Good); break;
                        case PetPurchase.NotEnoughCoins: Say($"Il te manque {pet.Price - _progress.Wallet.Coins} pièces.", new Color(1f, 0.55f, 0.45f)); break;
                    }
                }
            }

            Refresh();
        }

        private void Say(string text, Color color)
        {
            _message.text = text;
            _message.color = color;
        }

        private void Refresh()
        {
            int count = Pets.Count + 1;
            while (_cards.Count < count) _cards.Add(CreateCard(_cards.Count, count));
            PetCollection owned = _progress.Pets;
            for (int i = 0; i < _cards.Count; i++)
            {
                Card card = _cards[i];
                card.Frame.gameObject.SetActive(i < count);
                card.Frame.color = i == _selected ? Gold : new Color(0.62f, 0.66f, 0.72f);
                if (i >= Pets.Count)
                {
                    card.Portrait.enabled = false;
                    card.Name.text = "Aucun animal";
                    card.Bonus.text = "Ton compagnon reste à la maison.";
                    card.Price.text = owned.Equipped == null ? "Choisi" : "Gratuit";
                    card.Price.color = owned.Equipped == null ? Good : Muted;
                    continue;
                }

                PetDefinition pet = Pets[i];
                card.Portrait.enabled = pet.Portrait != null;
                card.Portrait.sprite = pet.Portrait;
                card.Name.text = pet.DisplayName;
                card.Bonus.text = pet.BonusText;
                bool has = owned.Owns(pet.Id);
                card.Price.text = owned.Equipped == pet.Id ? "Avec toi" : has ? "À toi : le prendre" : $"{pet.Price} pièces";
                card.Price.color = owned.Equipped == pet.Id ? Good : has ? Color.white : Gold;
            }
        }

        private Card CreateCard(int index, int count)
        {
            float x = (index - (count - 1) * 0.5f) * (CardWidth + 24f);
            var card = new Card();
            card.Frame = UiFactory.Sliced(_window.GetComponent<RectTransform>(), $"Animal {index + 1}", _skin != null ? _skin.Slot : null, Color.white);
            RectTransform rect = card.Frame.rectTransform;
            UiFactory.Center(rect, new Vector2(x, 10f), new Vector2(CardWidth, CardHeight));
            Image backdrop = UiFactory.Sliced(rect, "Fond", _skin != null ? _skin.WaterBackdrop : null, new Color(1f, 1f, 1f, 0.4f));
            UiFactory.Center(backdrop.rectTransform, new Vector2(0f, 80f), new Vector2(220f, 180f));
            card.Portrait = UiFactory.Plain(rect, "Portrait", Color.white);
            card.Portrait.preserveAspect = true;
            UiFactory.Center(card.Portrait.rectTransform, new Vector2(0f, 76f), new Vector2(200f, 180f));
            card.Name = UiFactory.Label(rect, "Nom", 30, TextAnchor.MiddleCenter, Gold);
            UiFactory.Bold(card.Name);
            UiFactory.Center(card.Name.rectTransform, new Vector2(0f, -40f), new Vector2(300f, 40f));
            card.Bonus = UiFactory.Label(rect, "Bonus", 21, TextAnchor.UpperCenter, Color.white);
            card.Bonus.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Center(card.Bonus.rectTransform, new Vector2(0f, -96f), new Vector2(290f, 80f));
            card.Price = UiFactory.Label(rect, "Prix", 26, TextAnchor.MiddleCenter, Gold);
            UiFactory.Bold(card.Price);
            UiFactory.Center(card.Price.rectTransform, new Vector2(0f, -172f), new Vector2(300f, 36f));
            return card;
        }

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Animalerie - Interface", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 13;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();

            _prompt = UiFactory.Label(root, "Invite", 30, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.60f));
            _prompt.text = "E : parler au vendeur";
            UiFactory.Center(_prompt.rectTransform, new Vector2(0f, -395f), new Vector2(1200f, 50f));
            _prompt.gameObject.SetActive(false);

            Image window = UiFactory.Sliced(root, "Fenêtre de l'animalerie", _skin != null ? _skin.Panel : null, _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            _window = window.gameObject;
            UiFactory.Center(window.rectTransform, new Vector2(0f, 10f), new Vector2(1500f, 640f));
            UiFactory.TitlePlaque(window.rectTransform, _skin, "Animalerie", 30, new Vector2(0f, 320f), new Vector2(420f, 64f), out _);
            _message = UiFactory.Label(window.rectTransform, "Message", 24, TextAnchor.MiddleCenter, Muted);
            UiFactory.Center(_message.rectTransform, new Vector2(0f, 250f), new Vector2(1400f, 34f));
            Text help = UiFactory.Label(window.rectTransform, "Aide", 22, TextAnchor.MiddleCenter, Muted);
            help.text = "Gauche / Droite : choisir   •   E ou Espace : acheter ou prendre avec toi   •   Échap : fermer";
            UiFactory.Center(help.rectTransform, new Vector2(0f, -285f), new Vector2(1400f, 30f));
            _window.SetActive(false);
        }
    }
}
