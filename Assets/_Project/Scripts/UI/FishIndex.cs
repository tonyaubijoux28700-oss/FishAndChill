using System.Collections.Generic;
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
    /// L'index des espèces, en carnet de pêcheur : une icône de livre en bas à gauche (compteur « 12/29 ») et, avec Tab
    /// (ou Y à la manette), un carnet ouvert sur deux pages.
    /// Sur le côté gauche, une colonne des zones qui défile (Haut / Bas quand elle a la main, Droite pour passer à la
    /// grille, Gauche pour y revenir ; Q/E ou gâchettes hautes partout). Page de gauche : la progression de la zone avec ses paliers
    /// de récompense (25 %, 50 %, 100 % : des pièces, et un tampon « Complet »), et la grille des espèces (dessins à
    /// l'échelle de leur taille, silhouettes « ??? » pour les inconnues).
    /// À droite : la fiche de l'espèce choisie (grand dessin, rareté, heure, zone, description réelle, prises, record),
    /// ou un indice pour une espèce pas encore pêchée. Les paliers sont encaissés dès qu'ils sont atteints.
    /// </summary>
    public sealed class FishIndex : MonoBehaviour
    {
        private static readonly Color Ink = new Color(0.26f, 0.16f, 0.08f);
        private static readonly Color InkSoft = new Color(0.50f, 0.38f, 0.24f);
        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color CellTint = new Color(0.86f, 0.80f, 0.66f);
        // Presque noir : la teinte multiplie les couleurs du dessin, il ne doit plus rien en rester (ni rayures, ni taches).
        private static readonly Color Silhouette = new Color(0.10f, 0.08f, 0.06f, 0.85f);

        private static readonly Color[] ZoneColors =
        {
            new Color(0.22f, 0.70f, 0.70f),
            new Color(0.36f, 0.52f, 0.86f),
            new Color(0.86f, 0.52f, 0.30f),
            new Color(0.62f, 0.40f, 0.78f),
        };

        private const int Columns = 4;
        private const int Rows = 3;
        private const int PerPage = Columns * Rows;
        private const float CellWidth = 160f;
        private const float CellHeight = 150f;
        private const float CellGap = 12f;
        private const float IconSize = 112f;
        private const float PageWidth = 740f;
        private const float PageHeight = 810f;
        private const float FlipSeconds = 0.45f;

        // Page qui tourne : une feuille qui pivote autour du pli quand on change de zone ou de page.
        private RectTransform _flip;
        private Image _flipImage;
        private Image _flipShadow;
        private float _flipT = -1f;
        private int _flipDirection = 1;
        private bool _refreshAtEnd;
        private const float BarWidth = 500f;

        // Le carnet, un peu réduit et décalé à droite pour laisser la place à la colonne des zones.
        private const float BookScale = 0.86f;
        private static readonly Vector2 BookCenter = new Vector2(165f, 0f);

        // Colonne des zones, à gauche du carnet : une liste qui défile, la zone choisie toujours visible.
        private static readonly Vector2 ColumnCenter = new Vector2(-695f, 0f);
        private static readonly Vector2 ColumnSize = new Vector2(290f, 760f);
        private const float ColumnBand = 80f;        // en haut (titre, « Plus haut ») et en bas (« Plus bas », compteur)
        private const float ZoneRowHeight = 72f;
        private const float ZoneRowGap = 10f;
        private const float ZoneScrollMargin = 30f;
        private const float ZonePadding = 8f;        // pour que le halo de la zone choisie ne soit pas rogné
        private RectTransform _zoneContent;
        private float _zoneViewHeight;
        private float _zoneScroll;
        private float _zoneScrollTarget;
        private Text _moreAbove;
        private Text _moreBelow;
        private Text _zoneCounter;
        private bool _zoneListFocused = true;

        // Souris : zones cliquables (colonne, carnet, icône) et ce que le pointeur survole (-1 : rien).
        private RectTransform _iconRect;
        private RectTransform _columnRect;
        private RectTransform _zoneViewRect;
        private RectTransform _bookRect;
        private int _hoverZone = -1;
        private int _hoverCell = -1;

        [SerializeField, Tooltip("Zones dont les espèces figurent dans l'index, dans l'ordre de la colonne des zones.")]
        private FishingZone[] _zones = new FishingZone[0];

        [SerializeField] private FishingController _fishing;

        [SerializeField, Tooltip("Aquarium (espèces exposées) et porte-monnaie (récompenses).")]
        private PlayerProgress _progress;
        [SerializeField] private PlayerMovement _player;

        [SerializeField, Tooltip("Icône du livre (pixel art).")]
        private Sprite _bookIcon;

        [SerializeField, Tooltip("Charte pixel art de l'interface.")]
        private UiSkin _skin;

        [Header("Carnet")]
        [SerializeField] private Sprite _cover;
        [SerializeField] private Sprite _page;
        [SerializeField] private Sprite _cell;
        [SerializeField] private Sprite _ribbon;
        [SerializeField] private Sprite _spine;
        [SerializeField] private Sprite _stamp;
        [SerializeField] private Sprite _sun;
        [SerializeField] private Sprite _moon;
        [SerializeField] private Sprite _coin;

        private sealed class CellView
        {
            public Image Frame;
            public Image Fish;
            public ShineSweep Shine;
            public Image Gem;
            public Text Number;
            public Text Name;
        }

        private sealed class ZoneEntryView
        {
            public RectTransform Rect;
            public Image Halo;
            public Image Ribbon;
            public Text Name;
            public Text Count;
        }

        private sealed class MilestoneView
        {
            public Image Coin;
            public Text Label;
        }

        private readonly List<CellView> _cells = new List<CellView>();
        private readonly List<ZoneEntryView> _zoneEntries = new List<ZoneEntryView>();
        private readonly List<MilestoneView> _milestones = new List<MilestoneView>();
        private readonly List<List<FishSpecies>> _zoneSpecies = new List<List<FishSpecies>>();
        private SpeciesIndex _index;
        private Text _badge;
        private GameObject _window;
        private Text _zoneName;
        private Text _zoneRegion;
        private Image _barFill;
        private Text _barText;
        private RectTransform _stampRect;
        private Text _pageText;
        private Image _detailFish;
        private ShineSweep _detailShine;
        private Text _detailNumber;
        private Text _detailName;
        private Text _detailScientific;
        private Image _rarityGem;
        private Text _rarityText;
        private Image _timeIcon;
        private Text _timeText;
        private Text _zoneText;
        private Text _description;
        private readonly Text[] _stats = new Text[3];
        private Text _aquarium;
        private Text _help;
        private RectTransform _bannerRect;
        private Text _bannerText;
        private float _bannerTime;
        private int _zone;
        private int _selected;

        public bool IsOpen => _window != null && _window.activeSelf;

        /// <summary>Espèce sélectionnée (numéro dans la zone affichée).</summary>
        public int Selected => _selected;

        /// <summary>Zone affichée (choisie dans la colonne de gauche).</summary>
        public int Zone => _zone;

        /// <summary>La colonne des zones a la main (Haut / Bas changent de zone) ; sinon, c'est la grille des espèces.</summary>
        public bool ZoneListFocused => _zoneListFocused;

        public SpeciesIndex Index => _index;

        private CatchLog Log => _fishing != null ? _fishing.CatchLog : null;

        private List<FishSpecies> Current => _zoneSpecies.Count > 0 ? _zoneSpecies[_zone] : new List<FishSpecies>();

        private void Start()
        {
            var all = new List<FishSpecies>();
            foreach (FishingZone zone in _zones)
            {
                var list = new List<FishSpecies>();
                var seen = new HashSet<string>();
                if (zone != null)
                {
                    foreach (FishingZone.SpeciesEntry entry in zone.Species)
                    {
                        if (entry.Species != null && seen.Add(entry.Species.Id))
                        {
                            list.Add(entry.Species);
                            all.Add(entry.Species);
                        }
                    }
                }

                _zoneSpecies.Add(list);
            }

            _index = new SpeciesIndex(all);
            BuildUi();
            if (Log != null)
            {
                Log.Changed += OnLogChanged;
            }

            _window.SetActive(false);
            ClaimRewards();
            Refresh();
        }

        private void OnDestroy()
        {
            if (Log != null)
            {
                Log.Changed -= OnLogChanged;
            }
        }

        private void OnLogChanged()
        {
            ClaimRewards();
            Refresh();
        }

        private void Update()
        {
            UpdateBanner();
            if (IsOpen)
            {
                if (IndexInput.TogglePressedThisFrame() || IndexInput.ClosePressedThisFrame())
                {
                    Close();
                    return;
                }

                int zoneStep = ZoneStepThisFrame();
                (int dx, int dy) = IndexInput.NavigationThisFrame();
                if (_zoneListFocused)
                {
                    // Dans la colonne : Haut / Bas changent de zone, Droite ou Entrée passent à la grille des espèces.
                    if (dy != 0) zoneStep = -dy;
                    if ((dx > 0 || IndexInput.ConfirmPressedThisFrame()) && Current.Count > 0) FocusZoneList(false);
                }
                else if (dx < 0 && _selected % Columns == 0)
                {
                    // Gauche depuis la première colonne de la grille : retour à la liste des zones.
                    FocusZoneList(true);
                }
                else if (dx != 0 || dy != 0)
                {
                    Select(SpeciesIndex.Move(_selected, dx, -dy, Columns, Current.Count));
                }

                if (zoneStep != 0 && _zoneSpecies.Count > 1)
                {
                    TurnPage(zoneStep);
                    ShowZone((_zone + zoneStep + _zoneSpecies.Count) % _zoneSpecies.Count);
                }

                if (!HandleMouse())
                {
                    return;
                }

                Animate();
                AnimateZoneList(Time.unscaledDeltaTime);
                AnimateFlip(Time.unscaledDeltaTime);
            }
            else if ((IndexInput.TogglePressedThisFrame() || IconClickedThisFrame()) && CanOpenNow())
            {
                Open();
            }
        }

        // ---------- Souris ----------

        private static bool Over(RectTransform rect, Vector2 screen) =>
            rect != null && rect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null);

        /// <summary>Clic sur l'icône du livre (carnet fermé).</summary>
        private bool IconClickedThisFrame()
        {
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame && Over(_iconRect, mouse.position.ReadValue());
        }

        /// <summary>
        /// Souris, carnet ouvert : survol (zone ou case éclairée), clic sur une zone ou une espèce, molette pour faire
        /// défiler la colonne des zones ou tourner les pages d'espèces, clic à côté du carnet pour le fermer.
        /// Renvoie faux si le carnet vient d'être fermé.
        /// </summary>
        private bool HandleMouse()
        {
            _hoverZone = -1;
            _hoverCell = -1;
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return true;
            }

            Vector2 screen = mouse.position.ReadValue();
            bool overColumn = Over(_columnRect, screen);
            if (Over(_zoneViewRect, screen))
            {
                for (int i = 0; i < _zoneEntries.Count; i++)
                {
                    if (Over(_zoneEntries[i].Rect, screen))
                    {
                        _hoverZone = i;
                        break;
                    }
                }
            }

            for (int i = 0; i < _cells.Count; i++)
            {
                if (Over(_cells[i].Frame.rectTransform, screen))
                {
                    _hoverCell = i;
                    break;
                }
            }

            float wheel = mouse.scroll.ReadValue().y;
            if (wheel != 0f)
            {
                int step = wheel > 0f ? -1 : 1;
                if (overColumn)
                {
                    // Fait défiler la liste sans changer de zone (elle revient sur la zone choisie au prochain changement).
                    float max = ZoneListScroll.MaxOffset(_zoneEntries.Count, ZoneRowHeight, ZoneRowGap, _zoneViewHeight);
                    _zoneScrollTarget = Mathf.Clamp(_zoneScrollTarget + step * (ZoneRowHeight + ZoneRowGap), 0f, max);
                }
                else if (Over(_bookRect, screen) && Current.Count > PerPage)
                {
                    // Page d'espèces suivante / précédente, à la même place dans la grille.
                    int page = SpeciesIndex.PageOf(_selected, PerPage) + step;
                    int pages = (Current.Count + PerPage - 1) / PerPage;
                    if (page >= 0 && page < pages)
                    {
                        Select(Mathf.Min(page * PerPage + _selected % PerPage, Current.Count - 1));
                    }
                }
            }

            if (!mouse.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            if (_hoverZone >= 0)
            {
                if (_hoverZone != _zone)
                {
                    TurnPage(_hoverZone > _zone ? 1 : -1);
                    ShowZone(_hoverZone);
                }

                FocusZoneList(true);
            }
            else if (_hoverCell >= 0)
            {
                int number = SpeciesIndex.PageOf(_selected, PerPage) * PerPage + _hoverCell;
                if (number < Current.Count)
                {
                    if (_zoneListFocused) FocusZoneList(false);
                    Select(number);
                }
            }
            else if (!overColumn && !Over(_bookRect, screen))
            {
                Close();
                return false;
            }

            return true;
        }

        private static int ZoneStepThisFrame()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                // Touches physiques Q et E (A et E sur un clavier AZERTY), juste au-dessus de ZQSD.
                if (keyboard.qKey.wasPressedThisFrame) return -1;
                if (keyboard.eKey.wasPressedThisFrame) return 1;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.leftShoulder.wasPressedThisFrame) return -1;
                if (gamepad.rightShoulder.wasPressedThisFrame) return 1;
            }

            return 0;
        }

        public void Open()
        {
            _window.SetActive(true);
            if (_player != null)
            {
                _player.Locked = true;
            }

            _zoneListFocused = true;
            ClaimRewards();
            Refresh();
            // À l'ouverture, la liste est déjà en place (pas de glissement depuis le haut).
            _zoneScroll = _zoneScrollTarget;
            AnimateZoneList(0f);
        }

        /// <summary>Donne la main à la colonne des zones (vrai) ou à la grille des espèces (faux).</summary>
        public void FocusZoneList(bool focused)
        {
            _zoneListFocused = focused;
            RefreshZoneList();
            RefreshHelp();
        }

        public void Close()
        {
            _window.SetActive(false);
            if (_player != null)
            {
                _player.Locked = false;
            }
        }

        /// <summary>Affiche une zone (comme si on la choisissait dans la colonne).</summary>
        public void ShowZone(int zone)
        {
            _zone = Mathf.Clamp(zone, 0, Mathf.Max(0, _zoneSpecies.Count - 1));
            _selected = 0;
            RefreshOrWait();
        }

        /// <summary>Sélectionne une espèce de la zone affichée (et tourne la page si besoin).</summary>
        public void Select(int index)
        {
            int before = SpeciesIndex.PageOf(_selected, PerPage);
            _selected = Mathf.Clamp(index, 0, Mathf.Max(0, Current.Count - 1));
            int after = SpeciesIndex.PageOf(_selected, PerPage);
            if (after != before)
            {
                TurnPage(after > before ? 1 : -1);
            }

            RefreshOrWait();
        }

        /// <summary>
        /// Pendant qu'une page tourne, comme dans un vrai livre : la page que la feuille découvre en se soulevant se met à
        /// jour tout de suite, celle sur laquelle elle se rabat seulement à la fin, une fois recouverte.
        /// </summary>
        private void RefreshOrWait()
        {
            if (_flipT >= 0f)
            {
                Refresh(left: _flipDirection < 0, right: _flipDirection > 0);
                _refreshAtEnd = true;
                return;
            }

            Refresh();
        }

        /// <summary>
        /// Fait tourner une page : vers l'avant (1), la feuille de droite se soulève et se rabat à gauche ; vers l'arrière
        /// (-1), l'inverse. Le nouveau contenu est déjà dessous, la feuille le découvre en tournant.
        /// </summary>
        public void TurnPage(int direction)
        {
            if (_flip == null)
            {
                return;
            }

            if (_refreshAtEnd)
            {
                // Une page tournait déjà : on finit d'afficher son contenu avant d'en tourner une autre.
                _refreshAtEnd = false;
                Refresh();
            }

            _flipDirection = direction >= 0 ? 1 : -1;
            _flipT = 0f;
            // Pivot sur le pli : bord gauche de la page de droite, ou bord droit de la page de gauche.
            _flip.pivot = new Vector2(_flipDirection > 0 ? 0f : 1f, 0.5f);
            _flip.anchoredPosition = new Vector2(_flipDirection > 0 ? 382f - PageWidth * 0.5f : -382f + PageWidth * 0.5f, 0f);
            _flip.gameObject.SetActive(true);
            _flipShadow.gameObject.SetActive(true);
            AnimateFlip(0f);
        }

        private void AnimateFlip(float deltaTime)
        {
            if (_flipT < 0f)
            {
                return;
            }

            _flipT += deltaTime / FlipSeconds;
            if (_flipT >= 1f)
            {
                if (_refreshAtEnd)
                {
                    _refreshAtEnd = false;
                    Refresh();
                }

                _flipT = -1f;
                _flip.gameObject.SetActive(false);
                _flipShadow.gameObject.SetActive(false);
                return;
            }

            // Largeur apparente : 1 → 0 (feuille vue par la tranche) → -1 (rabattue de l'autre côté).
            float eased = Mathf.SmoothStep(0f, 1f, _flipT);
            float width = Mathf.Cos(eased * Mathf.PI);
            _flip.localScale = new Vector3(width, 1f, 1f);
            // Un peu plus sombre par la tranche, et ombre portée sur la page d'en dessous.
            float edge = 1f - Mathf.Abs(width);
            _flipImage.color = Color.Lerp(new Color(0.96f, 0.91f, 0.78f), new Color(0.70f, 0.62f, 0.48f), edge * 0.8f);
            RectTransform shadow = _flipShadow.rectTransform;
            bool firstHalf = width > 0f;
            float side = firstHalf ? _flipDirection : -_flipDirection;
            shadow.pivot = new Vector2(side > 0 ? 0f : 1f, 0.5f);
            shadow.anchoredPosition = new Vector2(side * 12f, 0f);
            shadow.sizeDelta = new Vector2(PageWidth * (1f - Mathf.Abs(width)) * 0.5f + 20f, PageHeight);
            _flipShadow.color = new Color(0.2f, 0.12f, 0.06f, 0.25f * edge + 0.05f);
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

        // ---------- Récompenses ----------

        /// <summary>Encaisse les paliers atteints (25, 50, 100 % d'une zone) et l'annonce par un bandeau.</summary>
        private void ClaimRewards()
        {
            if (_progress == null || Log == null || !Save.SaveManager.InGame)
            {
                return;
            }

            for (int z = 0; z < _zones.Length && z < _zoneSpecies.Count; z++)
            {
                if (_zones[z] == null)
                {
                    continue;
                }

                List<FishSpecies> species = _zoneSpecies[z];
                int discovered = CollectionRewards.Discovered(species, Log);
                foreach (int percent in CollectionRewards.Pending(_zones[z].Id, discovered, species.Count, _progress.ClaimedRewards))
                {
                    int coins = CollectionRewards.CoinsFor(percent);
                    if (_progress.ClaimReward(CollectionRewards.Key(_zones[z].Id, percent), coins))
                    {
                        string what = percent == 100 ? "Zone complète" : $"{percent} % découverts";
                        ShowBanner($"Carnet — {_zones[z].DisplayName} : {what} !   +{coins} pièces");
                    }
                }
            }
        }

        private void ShowBanner(string text)
        {
            _bannerText.text = text;
            _bannerTime = 4.5f;
            _bannerRect.gameObject.SetActive(true);
        }

        private void UpdateBanner()
        {
            if (_bannerTime <= 0f)
            {
                return;
            }

            _bannerTime -= Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(Mathf.Min(4.5f - _bannerTime, _bannerTime) / 0.3f);
            _bannerRect.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, t);
            _bannerRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(-20f, 0f, t) - 482f);
            if (_bannerTime <= 0f)
            {
                _bannerRect.gameObject.SetActive(false);
            }
        }

        // ---------- Rafraîchissement ----------

        /// <summary>Met à jour la colonne des zones, la page de gauche (zone, grille) et / ou celle de droite (fiche de l'espèce).</summary>
        private void Refresh(bool left = true, bool right = true)
        {
            if (_index == null)
            {
                return;
            }

            CatchLog log = Log;
            // La colonne des zones est hors des pages : elle suit tout de suite, même pendant qu'une page tourne.
            RefreshZoneList();
            RefreshHelp();
            if (!left)
            {
                if (right) RefreshDetail(log, _zones.Length > _zone ? _zones[_zone] : null);
                return;
            }

            _badge.text = $"{_index.DiscoveredCount(log)}/{_index.Count}";

            List<FishSpecies> species = Current;
            FishingZone zone = _zones.Length > _zone ? _zones[_zone] : null;
            int discovered = CollectionRewards.Discovered(species, log);
            _zoneName.text = zone != null ? zone.DisplayName : "Index";
            _zoneRegion.text = zone != null ? zone.Region : string.Empty;
            float ratio = species.Count > 0 ? (float)discovered / species.Count : 0f;
            _barFill.rectTransform.sizeDelta = new Vector2(Mathf.Max(0f, (BarWidth - 12f) * ratio), 14f);
            _barText.text = $"{discovered} / {species.Count}";
            for (int m = 0; m < _milestones.Count; m++)
            {
                int percent = CollectionRewards.Percents[m];
                bool claimed = zone != null && _progress != null && _progress.ClaimedRewards.Contains(CollectionRewards.Key(zone.Id, percent));
                _milestones[m].Coin.color = claimed ? Color.white : new Color(0.55f, 0.50f, 0.42f, 0.55f);
                _milestones[m].Label.text = claimed ? "Reçu" : $"+{CollectionRewards.CoinsFor(percent)}";
                _milestones[m].Label.color = claimed ? new Color(0.22f, 0.50f, 0.18f) : InkSoft;
            }

            _stampRect.gameObject.SetActive(species.Count > 0 && discovered == species.Count);

            int page = SpeciesIndex.PageOf(_selected, PerPage);
            int pages = species.Count > 0 ? (species.Count + PerPage - 1) / PerPage : 1;
            _pageText.text = pages > 1 ? $"Page {page + 1} / {pages}" : string.Empty;

            for (int i = 0; i < _cells.Count; i++)
            {
                CellView cell = _cells[i];
                int number = page * PerPage + i;
                bool exists = number < species.Count;
                cell.Frame.gameObject.SetActive(exists);
                if (!exists)
                {
                    continue;
                }

                FishSpecies s = species[number];
                bool known = log != null && log.IsDiscovered(s.Id);
                cell.Number.text = $"{number + 1}";
                cell.Fish.sprite = s.Sprite;
                // À l'échelle de la taille adulte maximale de l'espèce : une ablette paraît petite, une carangue remplit la case.
                cell.Fish.rectTransform.sizeDelta = FishDisplayScale.Fit(new Vector2(CellWidth - 30f, 66f), SpritePixels(s), s.SizeRangeCm.y);
                cell.Fish.color = known ? (s.Sprite != null ? Color.white : s.PlaceholderColor) : Silhouette;
                cell.Shine.SetRarity(s.Rarity, known && s.Sprite != null);
                cell.Name.text = known ? s.CommonName : "???";
                cell.Name.color = known ? Ink : InkSoft;
                cell.Gem.color = known ? s.Rarity.DisplayColor() : new Color(0.55f, 0.50f, 0.42f);
            }

            if (right) RefreshDetail(log, zone);
        }

        private void RefreshHelp()
        {
            if (_help == null)
            {
                return;
            }

            if (_zoneListFocused)
            {
                _help.text = _zoneSpecies.Count > 1
                    ? "Haut / Bas : changer de zone      Droite : voir les espèces      Tab : fermer"
                    : "Droite : voir les espèces      Tab : fermer";
                return;
            }

            _help.text = _zoneSpecies.Count > 1
                ? $"Flèches : choisir      Gauche : liste des zones      {ZoneKeys()} : zone      Tab : fermer"
                : "Flèches : choisir      Tab : fermer";
        }

        /// <summary>Colonne des zones : la zone affichée en couleur, sortie vers le carnet, et toujours visible.</summary>
        private void RefreshZoneList()
        {
            if (_zoneContent == null)
            {
                return;
            }

            CatchLog log = Log;
            for (int i = 0; i < _zoneEntries.Count; i++)
            {
                ZoneEntryView entry = _zoneEntries[i];
                bool active = i == _zone;
                Color color = ZoneColors[i % ZoneColors.Length];
                entry.Ribbon.color = active ? color : Color.Lerp(color, new Color(0.4f, 0.36f, 0.3f), 0.55f);
                entry.Rect.sizeDelta = new Vector2(active ? 236f : 214f, ZoneRowHeight);
                entry.Rect.anchoredPosition = new Vector2(active ? 6f : -6f, -ZonePadding - i * (ZoneRowHeight + ZoneRowGap));
                entry.Halo.gameObject.SetActive(active);
                entry.Name.color = active ? Color.white : new Color(0.86f, 0.84f, 0.8f);

                List<FishSpecies> species = i < _zoneSpecies.Count ? _zoneSpecies[i] : new List<FishSpecies>();
                int discovered = CollectionRewards.Discovered(species, log);
                bool complete = species.Count > 0 && discovered == species.Count;
                entry.Count.text = complete ? $"Complet  {discovered} / {species.Count}" : $"{discovered} / {species.Count}";
                entry.Count.color = complete ? Gold : (active ? new Color(0.96f, 0.94f, 0.88f) : new Color(0.80f, 0.78f, 0.72f));
            }

            _zoneScrollTarget = ZoneListScroll.Offset(_zoneScrollTarget, _zone, _zoneEntries.Count, ZoneRowHeight, ZoneRowGap, _zoneViewHeight, ZoneScrollMargin);
            _zoneCounter.text = _zoneEntries.Count > 0 ? $"Zone {_zone + 1} / {_zoneEntries.Count}" : string.Empty;
        }

        /// <summary>Glissement doux de la liste vers la zone choisie, repères « Plus haut » / « Plus bas » et halo qui pulse.</summary>
        private void AnimateZoneList(float deltaTime)
        {
            if (_zoneContent == null)
            {
                return;
            }

            _zoneScroll = deltaTime <= 0f ? _zoneScrollTarget : Mathf.Lerp(_zoneScroll, _zoneScrollTarget, 1f - Mathf.Exp(-14f * deltaTime));
            if (Mathf.Abs(_zoneScroll - _zoneScrollTarget) < 0.5f) _zoneScroll = _zoneScrollTarget;
            _zoneContent.anchoredPosition = new Vector2(0f, Mathf.Round(_zoneScroll));
            _moreAbove.gameObject.SetActive(ZoneListScroll.MoreAbove(_zoneScroll));
            _moreBelow.gameObject.SetActive(ZoneListScroll.MoreBelow(_zoneScroll, _zoneEntries.Count, ZoneRowHeight, ZoneRowGap, _zoneViewHeight));

            // Zone survolée à la souris : son nom passe en or.
            for (int i = 0; i < _zoneEntries.Count; i++)
            {
                _zoneEntries[i].Name.color = i == _zone ? Color.white : i == _hoverZone ? Gold : new Color(0.86f, 0.84f, 0.8f);
            }

            if (_zone < _zoneEntries.Count)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                Image halo = _zoneEntries[_zone].Halo;
                halo.color = _zoneListFocused ? Color.Lerp(Gold, Color.white, 0.5f * pulse) : new Color(Gold.r, Gold.g, Gold.b, 0.55f);
            }
        }

        private static string ZoneKeys()
        {
            var keyboard = Keyboard.current;
            return keyboard != null ? $"{keyboard.qKey.displayName} / {keyboard.eKey.displayName}" : "Q / E";
        }

        private static Vector2 SpritePixels(FishSpecies species) => species.Sprite != null ? species.Sprite.rect.size : new Vector2(2f, 1f);

        private void RefreshDetail(CatchLog log, FishingZone zone)
        {
            List<FishSpecies> species = Current;
            bool any = species.Count > 0;
            _detailFish.gameObject.SetActive(any);
            if (!any)
            {
                _detailName.text = "Aucune espèce";
                return;
            }

            FishSpecies s = species[Mathf.Clamp(_selected, 0, species.Count - 1)];
            bool known = log != null && log.IsDiscovered(s.Id);
            _detailFish.sprite = s.Sprite;
            _detailFish.rectTransform.sizeDelta = FishDisplayScale.Fit(new Vector2(560f, 200f), SpritePixels(s), s.SizeRangeCm.y);
            _detailFish.color = known ? Color.white : Silhouette;
            _detailShine.SetRarity(s.Rarity, known && s.Sprite != null);
            _detailNumber.text = $"N° {_selected + 1}";

            // Rareté, heure et zone : toujours visibles, ce sont aussi les indices d'une espèce inconnue.
            _rarityGem.color = s.Rarity.DisplayColor();
            _rarityText.text = s.Rarity.DisplayName();
            _timeIcon.sprite = s.ActiveTime == ActiveTime.Night ? _moon : _sun;
            _timeIcon.color = Color.white;
            _timeText.text = TimeAndWeatherText(s);
            _zoneText.text = zone != null ? zone.DisplayName : string.Empty;

            if (!known)
            {
                _detailName.text = "???";
                bool trapped = s.Kind.CaughtInTrap();
                _detailScientific.text = trapped ? "Espèce pas encore attrapée" : "Espèce pas encore pêchée";
                _description.text = trapped
                    ? $"Indice : {s.Rarity.DisplayName().ToLowerInvariant()}, de {s.SizeRangeCm.x:0} à {s.SizeRangeCm.y:0} cm.{FavoriteBaitText(s)}\n" +
                      "Pose un casier dans sa zone (il s'achète à la boutique) !"
                    : $"Indice : {LowerFirst(_timeText.text)}, {s.Rarity.DisplayName().ToLowerInvariant()}, " +
                      $"de {s.SizeRangeCm.x:0} à {s.SizeRangeCm.y:0} cm.{FavoriteBaitText(s)}\nPêche-la pour en savoir plus !";
                _stats[0].text = "Prises\n—";
                _stats[1].text = "Record\n—";
                _stats[2].text = "Taille\n?";
                _aquarium.text = string.Empty;
                return;
            }

            Vector2 range = s.SizeRangeCm;
            _detailName.text = s.CommonName;
            _detailScientific.text = $"{s.ScientificName}  •  {s.Kind.DisplayName()}";
            _description.text = (string.IsNullOrEmpty(s.Description) ? "—" : s.Description) + FavoriteBaitText(s, true);
            _stats[0].text = $"Prises\n{log.CountOf(s.Id)}";
            _stats[1].text = $"Record\n{log.BestSizeOf(s.Id):0} cm";
            _stats[2].text = $"Taille\n{range.x:0} à {range.y:0} cm";
            CaughtFish specimen = _progress != null && _progress.Aquarium != null ? _progress.Aquarium.SpecimenOf(s.Id) : null;
            _aquarium.text = specimen != null ? $"Exposé dans l'aquarium ({specimen.SizeCm:0} cm)" : "Pas encore dans l'aquarium";
            _aquarium.color = specimen != null ? new Color(0.22f, 0.50f, 0.18f) : InkSoft;
        }

        /// <summary>« Appât préféré : crevette » (en couleur), vide si l'espèce n'a pas d'appât préféré.</summary>
        private string FavoriteBaitText(FishSpecies species, bool newLine = false)
        {
            if (species.Kind.CaughtInTrap())
            {
                // Les crustacés ne mordent pas à la canne : l'appât ne compte pas, seul le casier les attrape.
                const string trap = "<color=#2E6B2A>S'attrape au casier (pas à la canne)</color>";
                return newLine ? "\n" + trap : " " + trap + ".";
            }

            Bait bait = _progress != null && _progress.BaitCatalog != null ? _progress.BaitCatalog.FavoriteOf(species) : null;
            if (bait == null)
            {
                return string.Empty;
            }

            string text = $"<color=#2E6B2A>Appât préféré : {bait.DisplayName.ToLowerInvariant()}</color>";
            return newLine ? "\n" + text : " " + text + ".";
        }

        /// <summary>« Toute la journée », « Mord la nuit », « Sous la pluie », « Le jour, par temps d'orage »…</summary>
        private static string TimeAndWeatherText(FishSpecies s)
        {
            string weather = WeatherMath.NeedText(s.Weather);
            if (weather.Length == 0)
            {
                return s.ActiveTime == ActiveTime.Always ? "Toute la journée" : DayCycleMath.ActiveText(s.ActiveTime);
            }

            string time = s.ActiveTime == ActiveTime.Day ? "Le jour, " : s.ActiveTime == ActiveTime.Night ? "La nuit, " : string.Empty;
            return time.Length > 0 ? time + weather : char.ToUpperInvariant(weather[0]) + weather.Substring(1);
        }

        private static string LowerFirst(string text) => string.IsNullOrEmpty(text) ? text : char.ToLowerInvariant(text[0]) + text.Substring(1);

        private void Animate()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
            int first = SpeciesIndex.PageOf(_selected, PerPage) * PerPage;
            for (int i = 0; i < _cells.Count; i++)
            {
                bool selected = first + i == _selected;
                bool hovered = i == _hoverCell;   // survolée à la souris : case éclaircie, un peu plus grande
                _cells[i].Frame.color = selected ? Color.Lerp(Color.white, Gold, 0.35f + 0.4f * pulse) : hovered ? Color.Lerp(CellTint, Color.white, 0.6f) : CellTint;
                _cells[i].Frame.rectTransform.localScale = Vector3.one * (selected ? 1.05f : hovered ? 1.03f : 1f);
            }

            _detailFish.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 1.3f) * 6f, Mathf.Sin(Time.unscaledTime * 2.6f) * 4f);
        }

        // ---------- Construction ----------

        private Sprite SkinSprite(System.Func<UiSkin, Sprite> pick) => _skin != null ? pick(_skin) : null;

        private Color SkinTint(Color fallback) => _skin != null ? Color.white : fallback;

        private Text InkLabel(RectTransform parent, string name, int size, TextAnchor anchor, Color color)
        {
            Text text = UiFactory.Label(parent, name, size, anchor, color, shadow: false);
            text.supportRichText = true;
            return text;
        }

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Interface de l'index", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 12; // au-dessus de l'en-tête du jeu (10), sous la pause (20)
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();

            BuildIcon(root);

            // Voile, colonne des zones à gauche, puis le carnet : couverture, deux pages et le pli.
            Image veil = UiFactory.Plain(root, "Voile", new Color(0.02f, 0.04f, 0.06f, 0.55f));
            UiFactory.Stretch(veil.rectTransform);
            _window = veil.gameObject;

            BuildZoneColumn(veil.rectTransform);

            var bookObject = new GameObject("Carnet", typeof(RectTransform));
            bookObject.transform.SetParent(veil.rectTransform, false);
            var book = bookObject.GetComponent<RectTransform>();
            UiFactory.Center(book, BookCenter, new Vector2(1580f, 880f));
            book.localScale = Vector3.one * BookScale;
            _bookRect = book;

            Image cover = UiFactory.Sliced(book, "Couverture", _cover, new Color(0.40f, 0.22f, 0.12f));
            UiFactory.Center(cover.rectTransform, new Vector2(0f, 0f), new Vector2(1580f, 880f));
            RectTransform left = BuildPage(book, "Page de gauche", -382f);
            RectTransform right = BuildPage(book, "Page de droite", 382f);
            Image spine = UiFactory.Plain(book, "Pli", Color.white);
            spine.sprite = _spine;
            UiFactory.Center(spine.rectTransform, Vector2.zero, new Vector2(70f, PageHeight));

            UiFactory.TitlePlaque(book, _skin, "Carnet du pêcheur", 32, new Vector2(382f, 440f), new Vector2(440f, 70f), out _);

            BuildLeftPage(left);
            BuildRightPage(right);

            // Ombre et feuille qui tourne, par-dessus les deux pages.
            _flipShadow = UiFactory.Plain(book, "Ombre de la page", new Color(0f, 0f, 0f, 0f));
            _flipShadow.raycastTarget = false;
            UiFactory.Center(_flipShadow.rectTransform, Vector2.zero, new Vector2(20f, PageHeight));
            _flipShadow.gameObject.SetActive(false);
            _flipImage = UiFactory.Sliced(book, "Page qui tourne", _page, new Color(0.96f, 0.91f, 0.78f));
            _flipImage.raycastTarget = false;
            _flip = _flipImage.rectTransform;
            UiFactory.Center(_flip, Vector2.zero, new Vector2(PageWidth, PageHeight));
            _flip.gameObject.SetActive(false);

            // Bandeau des récompenses (visible même carnet fermé).
            _bannerText = UiFactory.TitlePlaque(root, _skin, string.Empty, 28, new Vector2(0f, -482f), new Vector2(900f, 72f), out _bannerRect);
            _bannerRect.gameObject.SetActive(false);
        }

        private RectTransform BuildPage(RectTransform book, string name, float x)
        {
            Image page = UiFactory.Sliced(book, name, _page, new Color(0.96f, 0.91f, 0.78f));
            UiFactory.Center(page.rectTransform, new Vector2(x, 0f), new Vector2(PageWidth, PageHeight));
            return page.rectTransform;
        }

        /// <summary>
        /// Colonne des zones : un étui de cuir assorti au carnet, une feuille de papier et, dessus, un marque-page par zone
        /// dans une vue masquée qui défile (autant de zones qu'on veut).
        /// </summary>
        private void BuildZoneColumn(RectTransform parent)
        {
            Image frame = UiFactory.Sliced(parent, "Colonne des zones", _cover, new Color(0.40f, 0.22f, 0.12f));
            RectTransform column = frame.rectTransform;
            _columnRect = column;
            UiFactory.Center(column, ColumnCenter, ColumnSize);

            Image paper = UiFactory.Sliced(column, "Feuille", _page, new Color(0.96f, 0.91f, 0.78f));
            UiFactory.Stretch(paper.rectTransform, 14f);

            UiFactory.TitlePlaque(column, _skin, "Zones", 30, new Vector2(0f, ColumnSize.y * 0.5f - 8f), new Vector2(200f, 60f), out _);

            var viewObject = new GameObject("Vue des zones", typeof(RectTransform), typeof(RectMask2D));
            viewObject.transform.SetParent(column, false);
            var view = viewObject.GetComponent<RectTransform>();
            _zoneViewRect = view;
            view.anchorMin = Vector2.zero;
            view.anchorMax = Vector2.one;
            view.offsetMin = new Vector2(16f, ColumnBand);
            view.offsetMax = new Vector2(-16f, -ColumnBand);
            _zoneViewHeight = ColumnSize.y - 2f * ColumnBand - 2f * ZonePadding;

            var contentObject = new GameObject("Liste", typeof(RectTransform));
            contentObject.transform.SetParent(view, false);
            _zoneContent = contentObject.GetComponent<RectTransform>();
            UiFactory.Place(_zoneContent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero,
                new Vector2(0f, ZoneListScroll.ContentHeight(_zones.Length, ZoneRowHeight, ZoneRowGap) + 2f * ZonePadding));

            for (int i = 0; i < _zones.Length; i++)
            {
                _zoneEntries.Add(BuildZoneEntry(_zoneContent, i));
            }

            float bandCenter = ColumnSize.y * 0.5f - ColumnBand + 24f;
            _moreAbove = InkLabel(column, "Plus haut", 19, TextAnchor.MiddleCenter, InkSoft);
            _moreAbove.text = "Plus haut";
            UiFactory.Center(_moreAbove.rectTransform, new Vector2(0f, bandCenter - 2f), new Vector2(240f, 24f));
            _moreBelow = InkLabel(column, "Plus bas", 19, TextAnchor.MiddleCenter, InkSoft);
            _moreBelow.text = "Plus bas";
            UiFactory.Center(_moreBelow.rectTransform, new Vector2(0f, -bandCenter + 2f), new Vector2(240f, 24f));
            _zoneCounter = InkLabel(column, "Compteur des zones", 20, TextAnchor.MiddleCenter, Ink);
            UiFactory.Center(_zoneCounter.rectTransform, new Vector2(0f, -ColumnSize.y * 0.5f + 36f), new Vector2(240f, 26f));
        }

        private ZoneEntryView BuildZoneEntry(RectTransform content, int i)
        {
            var holder = new GameObject($"Zone {i + 1}", typeof(RectTransform));
            holder.transform.SetParent(content, false);
            var rect = holder.GetComponent<RectTransform>();
            UiFactory.Place(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -ZonePadding - i * (ZoneRowHeight + ZoneRowGap)), new Vector2(214f, ZoneRowHeight));

            // Halo doré derrière le marque-page de la zone choisie (il pulse quand la colonne a la main).
            Image halo = UiFactory.Sliced(rect, "Halo", _cell, Gold);
            UiFactory.Stretch(halo.rectTransform, -5f);
            halo.gameObject.SetActive(false);

            Image ribbon = UiFactory.Sliced(rect, "Marque-page", _ribbon, ZoneColors[i % ZoneColors.Length]);
            UiFactory.Stretch(ribbon.rectTransform);

            Text name = UiFactory.Label(ribbon.rectTransform, "Nom", 22, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Bold(name);
            name.text = _zones[i] != null ? _zones[i].DisplayName : "?";
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.resizeTextForBestFit = true;   // un nom long rapetisse au lieu de déborder
            name.resizeTextMaxSize = name.fontSize;
            name.resizeTextMinSize = Mathf.Max(10, name.fontSize * 2 / 3);
            UiFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(-16f, 34f));

            Text count = UiFactory.Label(ribbon.rectTransform, "Découvertes", 18, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Place(count.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(-16f, 24f));

            return new ZoneEntryView { Rect = rect, Halo = halo, Ribbon = ribbon, Name = name, Count = count };
        }

        private void BuildLeftPage(RectTransform page)
        {
            _zoneName = InkLabel(page, "Zone", 38, TextAnchor.MiddleLeft, Ink);
            UiFactory.Bold(_zoneName);
            UiFactory.Center(_zoneName.rectTransform, new Vector2(-40f, 352f), new Vector2(580f, 50f));
            _zoneRegion = InkLabel(page, "Pays", 21, TextAnchor.MiddleLeft, InkSoft);
            UiFactory.Center(_zoneRegion.rectTransform, new Vector2(-40f, 316f), new Vector2(580f, 30f));

            // Barre de progression de la zone, avec les paliers de récompense.
            Image bar = UiFactory.Sliced(page, "Progression", _cell, CellTint);
            UiFactory.Center(bar.rectTransform, new Vector2(-70f, 270f), new Vector2(BarWidth, 26f));
            _barFill = UiFactory.Plain(bar.rectTransform, "Remplissage", new Color(0.36f, 0.64f, 0.30f));
            UiFactory.Place(_barFill.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(0f, 14f));
            _barText = InkLabel(page, "Compteur", 26, TextAnchor.MiddleLeft, Ink);
            UiFactory.Bold(_barText);
            UiFactory.Center(_barText.rectTransform, new Vector2(270f, 270f), new Vector2(150f, 34f));
            foreach (int percent in CollectionRewards.Percents)
            {
                float x = -70f - BarWidth * 0.5f + BarWidth * percent / 100f;
                Image coin = UiFactory.Plain(page, $"Palier {percent}", Color.white);
                coin.sprite = _coin;
                UiFactory.Center(coin.rectTransform, new Vector2(Mathf.Min(x, -70f + BarWidth * 0.5f - 6f), 270f), new Vector2(30f, 30f));
                Text label = InkLabel(page, $"Gain {percent}", 18, TextAnchor.MiddleCenter, InkSoft);
                UiFactory.Center(label.rectTransform, new Vector2(Mathf.Min(x, -70f + BarWidth * 0.5f - 6f), 240f), new Vector2(90f, 24f));
                _milestones.Add(new MilestoneView { Coin = coin, Label = label });
            }

            // Grille des espèces de la zone.
            float gridWidth = Columns * CellWidth + (Columns - 1) * CellGap;
            for (int i = 0; i < PerPage; i++)
            {
                int column = i % Columns;
                int row = i / Columns;
                var position = new Vector2(-gridWidth * 0.5f + CellWidth * 0.5f + column * (CellWidth + CellGap), 140f - row * (CellHeight + CellGap));
                _cells.Add(BuildCell(page, i, position));
            }

            _pageText = InkLabel(page, "Page", 21, TextAnchor.MiddleRight, InkSoft);
            UiFactory.Center(_pageText.rectTransform, new Vector2(230f, -362f), new Vector2(220f, 30f));

            // Tampon « Complet », posé de travers quand toute la zone est découverte.
            Image stamp = UiFactory.Plain(page, "Tampon", Color.white);
            stamp.sprite = _stamp;
            _stampRect = stamp.rectTransform;
            UiFactory.Center(_stampRect, new Vector2(262f, 362f), new Vector2(128f, 128f));
            _stampRect.localRotation = Quaternion.Euler(0f, 0f, 14f);
            Text stampText = InkLabel(_stampRect, "Complet", 26, TextAnchor.MiddleCenter, new Color(0.80f, 0.16f, 0.12f, 0.9f));
            UiFactory.Bold(stampText);
            stampText.text = "COMPLET";
            UiFactory.Stretch(stampText.rectTransform);
        }

        private CellView BuildCell(RectTransform parent, int i, Vector2 position)
        {
            Image frame = UiFactory.Sliced(parent, $"Espèce {i + 1}", _cell, CellTint);
            UiFactory.Center(frame.rectTransform, position, new Vector2(CellWidth, CellHeight));
            RectTransform rect = frame.rectTransform;

            Image backdrop = UiFactory.Sliced(rect, "Fond d'eau", SkinSprite(s => s.WaterBackdrop), SkinTint(new Color(0.2f, 0.55f, 0.65f)));
            UiFactory.Place(backdrop.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(CellWidth - 18f, 86f));
            Image fish = UiFactory.Plain(backdrop.rectTransform, "Poisson", Color.white);
            fish.preserveAspect = true;
            UiFactory.Center(fish.rectTransform, Vector2.zero, new Vector2(CellWidth - 30f, 66f));

            Text number = InkLabel(rect, "Numéro", 16, TextAnchor.UpperLeft, Color.white);
            UiFactory.Place(number.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -12f), new Vector2(40f, 22f));

            Image gem = UiFactory.Plain(rect, "Pierre", Color.white);
            gem.sprite = SkinSprite(s => s.Gem);
            UiFactory.Place(gem.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-20f, -22f), new Vector2(20f, 20f));

            Text name = InkLabel(rect, "Nom", 17, TextAnchor.MiddleCenter, Ink);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Place(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(CellWidth - 12f, 46f));

            return new CellView { Frame = frame, Fish = fish, Gem = gem, Number = number, Name = name, Shine = ShineSweep.Attach(fish) };
        }

        private void BuildRightPage(RectTransform page)
        {
            _detailNumber = InkLabel(page, "Numéro", 22, TextAnchor.MiddleLeft, InkSoft);
            UiFactory.Center(_detailNumber.rectTransform, new Vector2(-250f, 352f), new Vector2(160f, 30f));
            _detailName = InkLabel(page, "Nom", 42, TextAnchor.MiddleCenter, Ink);
            UiFactory.Bold(_detailName);
            UiFactory.Center(_detailName.rectTransform, new Vector2(0f, 312f), new Vector2(680f, 54f));
            _detailScientific = InkLabel(page, "Nom scientifique", 22, TextAnchor.MiddleCenter, InkSoft);
            _detailScientific.fontStyle = FontStyle.Italic;
            UiFactory.Center(_detailScientific.rectTransform, new Vector2(0f, 272f), new Vector2(680f, 30f));

            Image backdrop = UiFactory.Sliced(page, "Fond d'eau", SkinSprite(s => s.WaterBackdrop), SkinTint(new Color(0.2f, 0.55f, 0.65f)));
            UiFactory.Center(backdrop.rectTransform, new Vector2(0f, 120f), new Vector2(620f, 240f));
            _detailFish = UiFactory.Plain(backdrop.rectTransform, "Poisson", Color.white);
            _detailFish.preserveAspect = true;
            _detailShine = ShineSweep.Attach(_detailFish);
            UiFactory.Center(_detailFish.rectTransform, Vector2.zero, new Vector2(560f, 200f));

            // Ligne d'icônes : rareté, heure, zone.
            _rarityGem = UiFactory.Plain(page, "Rareté", Color.white);
            _rarityGem.sprite = SkinSprite(s => s.Gem);
            UiFactory.Center(_rarityGem.rectTransform, new Vector2(-290f, -28f), new Vector2(26f, 26f));
            _rarityText = InkLabel(page, "Rareté (texte)", 22, TextAnchor.MiddleLeft, Ink);
            UiFactory.Center(_rarityText.rectTransform, new Vector2(-200f, -28f), new Vector2(150f, 30f));
            _timeIcon = UiFactory.Plain(page, "Heure", Color.white);
            UiFactory.Center(_timeIcon.rectTransform, new Vector2(-95f, -28f), new Vector2(30f, 30f));
            _timeText = InkLabel(page, "Heure (texte)", 22, TextAnchor.MiddleLeft, Ink);
            UiFactory.Center(_timeText.rectTransform, new Vector2(10f, -28f), new Vector2(180f, 30f));
            _timeText.resizeTextForBestFit = true;   // « Le jour, sous la pluie » : rapetisse au lieu de déborder
            _timeText.resizeTextMaxSize = _timeText.fontSize;
            _timeText.resizeTextMinSize = Mathf.Max(10, _timeText.fontSize * 2 / 3);
            _zoneText = InkLabel(page, "Zone", 22, TextAnchor.MiddleRight, InkSoft);
            UiFactory.Center(_zoneText.rectTransform, new Vector2(220f, -28f), new Vector2(200f, 30f));

            _description = InkLabel(page, "Description", 23, TextAnchor.UpperCenter, Ink);
            _description.horizontalOverflow = HorizontalWrapMode.Wrap;
            _description.lineSpacing = 1.05f;
            UiFactory.Center(_description.rectTransform, new Vector2(0f, -110f), new Vector2(640f, 100f));

            for (int i = 0; i < _stats.Length; i++)
            {
                Image box = UiFactory.Sliced(page, $"Chiffre {i + 1}", _cell, CellTint);
                UiFactory.Center(box.rectTransform, new Vector2(-215f + i * 215f, -228f), new Vector2(200f, 78f));
                _stats[i] = InkLabel(box.rectTransform, "Texte", 22, TextAnchor.MiddleCenter, Ink);
                UiFactory.Stretch(_stats[i].rectTransform);
            }

            _aquarium = InkLabel(page, "Aquarium", 21, TextAnchor.MiddleCenter, InkSoft);
            UiFactory.Center(_aquarium.rectTransform, new Vector2(0f, -298f), new Vector2(660f, 30f));

            _help = InkLabel(page, "Aide", 19, TextAnchor.MiddleCenter, InkSoft);
            UiFactory.Center(_help.rectTransform, new Vector2(0f, -362f), new Vector2(700f, 28f));
            _help.horizontalOverflow = HorizontalWrapMode.Wrap;
            _help.resizeTextForBestFit = true;   // l'aide la plus longue rapetisse au lieu de déborder de la page
            _help.resizeTextMaxSize = _help.fontSize;
            _help.resizeTextMinSize = Mathf.Max(10, _help.fontSize * 3 / 4);
        }

        /// <summary>Icône du livre, à droite de celle du sac, avec le compteur et la touche.</summary>
        private void BuildIcon(RectTransform root)
        {
            Image iconBase = UiFactory.Sliced(root, "Index", SkinSprite(s => s.Slot), SkinTint(new Color(0.12f, 0.2f, 0.27f)));
            RectTransform iconRect = iconBase.rectTransform;
            _iconRect = iconRect;
            float x = 24f + IconSize + 22f + IconSize * 0.5f;
            UiFactory.Place(iconRect, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(x, 24f + IconSize * 0.5f), new Vector2(IconSize, IconSize));

            Image icon = UiFactory.Plain(iconRect, "Icône", Color.white);
            icon.sprite = _bookIcon;
            icon.preserveAspect = true;
            UiFactory.Center(icon.rectTransform, new Vector2(0f, 6f), new Vector2(IconSize - 26f, IconSize - 26f));

            _badge = UiFactory.TitlePlaque(iconRect, _skin, "0/0", 22, new Vector2(30f, -46f), new Vector2(64f, 34f), out _);

            Text key = UiFactory.Label(iconRect, "Touche", 20, TextAnchor.UpperLeft, Gold);
            key.text = "Tab";
            UiFactory.Bold(key);
            UiFactory.Place(key.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -6f), new Vector2(50f, 28f));
        }
    }
}
