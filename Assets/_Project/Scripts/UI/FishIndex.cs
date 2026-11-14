using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// L'index des poissons : une icône de livre en bas à gauche (à côté du sac, compteur « 3/8 »),
    /// et un grand livre ouvert avec Tab (ou Y à la manette) qui montre toutes les espèces des zones :
    /// les espèces découvertes avec leur dessin, les autres en silhouette « ??? ».
    /// Flèches / ZQSD pour choisir une espèce et voir sa fiche ; Tab ou Échap pour fermer.
    /// </summary>
    public sealed class FishIndex : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.66f, 0.72f, 0.78f);
        private static readonly Color UnknownTint = new Color(0.70f, 0.74f, 0.80f);
        // Presque noir : la teinte multiplie les couleurs du dessin, il ne doit plus rien en rester (ni rayures, ni taches).
        private static readonly Color Silhouette = new Color(0.02f, 0.06f, 0.09f, 0.88f);

        private const int Columns = 4;
        private const int Rows = 2;
        private const int PerPage = Columns * Rows;
        private const float CardWidth = 250f;
        private const float CardHeight = 200f;
        private const float CardGap = 18f;
        private const float IconSize = 112f;

        [SerializeField, Tooltip("Zones dont les espèces figurent dans l'index, dans l'ordre d'affichage.")]
        private FishingZone[] _zones = new FishingZone[0];

        [SerializeField] private FishingController _fishing;
        [SerializeField] private PlayerMovement _player;

        [SerializeField, Tooltip("Icône du livre (pixel art).")]
        private Sprite _bookIcon;

        [SerializeField, Tooltip("Charte pixel art de l'interface.")]
        private UiSkin _skin;

        private sealed class CardView
        {
            public Image Frame;
            public Image Fish;
            public Image Gem;
            public Text Number;
            public Text Name;
        }

        private readonly List<CardView> _cards = new List<CardView>();
        private SpeciesIndex _index;
        private RectTransform _iconRect;
        private Text _badge;
        private GameObject _window;
        private Text _subtitle;
        private Image _detailFish;
        private Text _detailName;
        private Text _detailScientific;
        private Text _detailStats;
        private Text _page;
        private int _selected;

        public bool IsOpen => _window != null && _window.activeSelf;

        /// <summary>Espèce sélectionnée (numéro dans l'index).</summary>
        public int Selected => _selected;

        public SpeciesIndex Index => _index;

        private CatchLog Log => _fishing != null ? _fishing.CatchLog : null;

        private void Start()
        {
            var species = new List<FishSpecies>();
            foreach (FishingZone zone in _zones)
            {
                if (zone == null)
                {
                    continue;
                }

                foreach (FishingZone.SpeciesEntry entry in zone.Species)
                {
                    species.Add(entry.Species);
                }
            }

            _index = new SpeciesIndex(species);
            BuildUi();
            if (Log != null)
            {
                Log.Changed += Refresh;
            }

            _window.SetActive(false);
            Refresh();
        }

        private void OnDestroy()
        {
            if (Log != null)
            {
                Log.Changed -= Refresh;
            }
        }

        private void Update()
        {
            if (IsOpen)
            {
                if (IndexInput.TogglePressedThisFrame() || IndexInput.ClosePressedThisFrame())
                {
                    Close();
                    return;
                }

                (int dx, int dy) = IndexInput.NavigationThisFrame();
                if (dx != 0 || dy != 0)
                {
                    Select(SpeciesIndex.Move(_selected, dx, -dy, Columns, _index.Count));
                }

                AnimateCards();
            }
            else if (IndexInput.TogglePressedThisFrame() && CanOpenNow())
            {
                Open();
            }
        }

        public void Open()
        {
            _window.SetActive(true);
            if (_player != null)
            {
                _player.Locked = true;
            }

            Refresh();
        }

        public void Close()
        {
            _window.SetActive(false);
            if (_player != null)
            {
                _player.Locked = false;
            }
        }

        /// <summary>Sélectionne une espèce (et tourne la page si besoin).</summary>
        public void Select(int index)
        {
            _selected = Mathf.Clamp(index, 0, Mathf.Max(0, _index.Count - 1));
            Refresh();
        }

        /// <summary>Hors pêche, hors menus (sac, marchand) : le joueur doit être libre de ses mouvements.</summary>
        private bool CanOpenNow()
        {
            if (_fishing != null && _fishing.State != FishingState.Idle && _fishing.State != FishingState.Result)
            {
                return false;
            }

            return _player == null || !_player.Locked;
        }

        // ---------- Rafraîchissement ----------

        private void Refresh()
        {
            if (_index == null)
            {
                return;
            }

            CatchLog log = Log;
            int discovered = _index.DiscoveredCount(log);
            _badge.text = $"{discovered}/{_index.Count}";
            _subtitle.text = $"{ZoneNames()}   —   {discovered} / {_index.Count} espèces découvertes";

            int page = SpeciesIndex.PageOf(_selected, PerPage);
            int pages = _index.PageCount(PerPage);
            _page.text = pages > 1 ? $"Page {page + 1} / {pages}" : string.Empty;

            for (int i = 0; i < _cards.Count; i++)
            {
                CardView card = _cards[i];
                int number = page * PerPage + i;
                bool exists = number < _index.Count;
                card.Frame.gameObject.SetActive(exists);
                if (!exists)
                {
                    continue;
                }

                FishSpecies species = _index.Species[number];
                bool known = log != null && log.IsDiscovered(species.Id);
                card.Number.text = $"{number + 1}";
                card.Fish.sprite = species.Sprite;
                card.Fish.color = known ? (species.Sprite != null ? Color.white : species.PlaceholderColor) : Silhouette;
                card.Name.text = known ? species.CommonName : "???";
                card.Name.color = known ? Color.white : Muted;
                card.Gem.color = known ? species.Rarity.DisplayColor() : new Color(0.3f, 0.34f, 0.4f);
            }

            RefreshDetail(log);
        }

        private void RefreshDetail(CatchLog log)
        {
            if (_index.Count == 0)
            {
                _detailFish.gameObject.SetActive(false);
                _detailName.text = "Aucune espèce";
                _detailScientific.text = string.Empty;
                _detailStats.text = string.Empty;
                return;
            }

            FishSpecies species = _index.Species[_selected];
            bool known = log != null && log.IsDiscovered(species.Id);
            _detailFish.gameObject.SetActive(true);
            _detailFish.sprite = species.Sprite;
            _detailFish.color = known ? Color.white : Silhouette;

            if (!known)
            {
                _detailName.text = $"N° {_selected + 1}   ???";
                _detailScientific.text = "Espèce inconnue";
                _detailStats.text = "Pêche-la pour la découvrir !";
                _detailStats.color = Muted;
                return;
            }

            Color rarity = species.Rarity.DisplayColor();
            Vector2 range = species.SizeRangeCm;
            _detailName.text = $"N° {_selected + 1}   {species.CommonName}";
            _detailScientific.text = species.ScientificName;
            _detailStats.text =
                $"<color=#{ColorUtility.ToHtmlStringRGB(rarity)}>{species.Rarity.DisplayName()}</color>" +
                $"   •   Prises : {log.CountOf(species.Id)}" +
                $"   •   Record : {log.BestSizeOf(species.Id):0} cm" +
                $"   •   Taille : {range.x:0} à {range.y:0} cm";
            _detailStats.color = Color.white;
        }

        private string ZoneNames()
        {
            var names = new List<string>();
            foreach (FishingZone zone in _zones)
            {
                if (zone != null)
                {
                    names.Add(zone.DisplayName);
                }
            }

            return names.Count > 0 ? string.Join(", ", names) : "Index";
        }

        private void AnimateCards()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
            int first = SpeciesIndex.PageOf(_selected, PerPage) * PerPage;
            CatchLog log = Log;
            for (int i = 0; i < _cards.Count; i++)
            {
                int number = first + i;
                if (number >= _index.Count)
                {
                    continue;
                }

                bool known = log != null && log.IsDiscovered(_index.Species[number].Id);
                Color baseTint = known ? Color.white : UnknownTint;
                bool selected = number == _selected;
                _cards[i].Frame.color = selected ? Color.Lerp(Color.white, Gold, 0.5f + 0.5f * pulse) : baseTint;
                _cards[i].Frame.rectTransform.localScale = Vector3.one * (selected ? 1.04f : 1f);
            }
        }

        // ---------- Construction ----------

        private Sprite SkinSprite(System.Func<UiSkin, Sprite> pick) => _skin != null ? pick(_skin) : null;

        private Color SkinTint(Color fallback) => _skin != null ? Color.white : fallback;

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Interface de l'index", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 8;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();

            BuildIcon(root);

            // Le livre ouvert, au centre de l'écran.
            Image window = UiFactory.Sliced(root, "Index des poissons", SkinSprite(s => s.Panel), SkinTint(new Color(0.08f, 0.12f, 0.19f)));
            _window = window.gameObject;
            RectTransform panel = window.rectTransform;
            UiFactory.Center(panel, new Vector2(0f, 20f), new Vector2(1200f, 790f));

            UiFactory.TitlePlaque(panel, _skin, "Index des poissons", 36, new Vector2(0f, 387f), new Vector2(520f, 76f), out _);

            _subtitle = UiFactory.Label(panel, "Zones", 24, TextAnchor.MiddleCenter, Muted);
            UiFactory.Center(_subtitle.rectTransform, new Vector2(0f, 318f), new Vector2(1100f, 36f));

            float gridWidth = Columns * CardWidth + (Columns - 1) * CardGap;
            for (int i = 0; i < PerPage; i++)
            {
                int column = i % Columns;
                int row = i / Columns;
                var position = new Vector2(
                    -gridWidth * 0.5f + CardWidth * 0.5f + column * (CardWidth + CardGap),
                    188f - row * (CardHeight + CardGap));
                _cards.Add(BuildCard(panel, i, position));
            }

            // Fiche de l'espèce sélectionnée.
            Image detail = UiFactory.Sliced(panel, "Fiche", SkinSprite(s => s.Slot), SkinTint(new Color(0.12f, 0.2f, 0.27f)));
            UiFactory.Center(detail.rectTransform, new Vector2(0f, -205f), new Vector2(1090f, 140f));
            RectTransform detailRect = detail.rectTransform;

            Image backdrop = UiFactory.Sliced(detailRect, "Fond d'eau", SkinSprite(s => s.WaterBackdrop), SkinTint(new Color(0.2f, 0.55f, 0.65f)));
            UiFactory.Place(backdrop.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(230f, 112f));
            _detailFish = UiFactory.Plain(backdrop.rectTransform, "Poisson", Color.white);
            _detailFish.preserveAspect = true;
            UiFactory.Center(_detailFish.rectTransform, Vector2.zero, new Vector2(206f, 92f));

            _detailName = UiFactory.Label(detailRect, "Nom", 32, TextAnchor.MiddleLeft, Color.white);
            UiFactory.Bold(_detailName);
            UiFactory.Place(_detailName.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(268f, -10f), new Vector2(-290f, 42f));

            _detailScientific = UiFactory.Label(detailRect, "Nom scientifique", 22, TextAnchor.MiddleLeft, Muted);
            _detailScientific.fontStyle = FontStyle.Italic;
            UiFactory.Place(_detailScientific.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(268f, -52f), new Vector2(-290f, 30f));

            _detailStats = UiFactory.Label(detailRect, "Stats", 22, TextAnchor.MiddleLeft, Color.white);
            _detailStats.supportRichText = true;
            UiFactory.Place(_detailStats.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(268f, -88f), new Vector2(-290f, 32f));

            _page = UiFactory.Label(panel, "Page", 22, TextAnchor.MiddleRight, Gold);
            UiFactory.Center(_page.rectTransform, new Vector2(420f, -325f), new Vector2(240f, 32f));

            Text help = UiFactory.Label(panel, "Aide", 21, TextAnchor.MiddleCenter, Muted);
            help.text = "Flèches ou ZQSD : choisir   •   Tab ou Échap : fermer";
            UiFactory.Center(help.rectTransform, new Vector2(0f, -325f), new Vector2(700f, 32f));
        }

        /// <summary>Icône du livre, à droite de celle du sac, avec le compteur et la touche.</summary>
        private void BuildIcon(RectTransform root)
        {
            Image iconBase = UiFactory.Sliced(root, "Index", SkinSprite(s => s.Slot), SkinTint(new Color(0.12f, 0.2f, 0.27f)));
            _iconRect = iconBase.rectTransform;
            float x = 24f + IconSize + 22f + IconSize * 0.5f;
            UiFactory.Place(_iconRect, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(x, 24f + IconSize * 0.5f), new Vector2(IconSize, IconSize));

            Image icon = UiFactory.Plain(_iconRect, "Icône", Color.white);
            icon.sprite = _bookIcon;
            icon.preserveAspect = true;
            UiFactory.Center(icon.rectTransform, new Vector2(0f, 6f), new Vector2(IconSize - 26f, IconSize - 26f));

            _badge = UiFactory.TitlePlaque(_iconRect, _skin, "0/0", 22, new Vector2(30f, -46f), new Vector2(64f, 34f), out _);

            Text key = UiFactory.Label(_iconRect, "Touche", 20, TextAnchor.UpperLeft, Gold);
            key.text = "Tab";
            UiFactory.Bold(key);
            UiFactory.Place(key.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -6f), new Vector2(50f, 28f));
        }

        private CardView BuildCard(RectTransform parent, int i, Vector2 position)
        {
            Image frame = UiFactory.Sliced(parent, $"Espèce {i + 1}", SkinSprite(s => s.Slot), SkinTint(new Color(0.12f, 0.2f, 0.27f)));
            UiFactory.Center(frame.rectTransform, position, new Vector2(CardWidth, CardHeight));
            RectTransform rect = frame.rectTransform;

            Image backdrop = UiFactory.Sliced(rect, "Fond d'eau", SkinSprite(s => s.WaterBackdrop), SkinTint(new Color(0.2f, 0.55f, 0.65f)));
            UiFactory.Place(backdrop.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(CardWidth - 24f, 124f));
            Image fish = UiFactory.Plain(backdrop.rectTransform, "Poisson", Color.white);
            fish.preserveAspect = true;
            UiFactory.Center(fish.rectTransform, new Vector2(0f, -4f), new Vector2(CardWidth - 50f, 92f));

            Text number = UiFactory.TitlePlaque(rect, _skin, (i + 1).ToString(), 20, Vector2.zero, new Vector2(40f, 32f), out RectTransform plaque);
            UiFactory.Place(plaque, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(12f, -10f), new Vector2(40f, 32f));

            Image gem = UiFactory.Plain(rect, "Pierre", Color.white);
            gem.sprite = SkinSprite(s => s.Gem);
            UiFactory.Place(gem.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-24f, -26f), new Vector2(26f, 26f));

            Text name = UiFactory.Label(rect, "Nom", 21, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Bold(name);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Place(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(CardWidth - 16f, 56f));

            return new CardView { Frame = frame, Fish = fish, Gem = gem, Number = number, Name = name };
        }
    }
}
