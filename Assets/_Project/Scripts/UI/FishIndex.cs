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
    /// À gauche : un onglet par zone (Q/E ou gâchettes hautes pour changer), la progression de la zone avec ses paliers
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

        [SerializeField, Tooltip("Zones dont les espèces figurent dans l'index, dans l'ordre des onglets.")]
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

        private sealed class TabView
        {
            public RectTransform Rect;
            public Image Ribbon;
            public Text Label;
        }

        private sealed class MilestoneView
        {
            public Image Coin;
            public Text Label;
        }

        private readonly List<CellView> _cells = new List<CellView>();
        private readonly List<TabView> _tabs = new List<TabView>();
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

        /// <summary>Zone affichée (onglet).</summary>
        public int Zone => _zone;

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
                if (zoneStep != 0 && _zoneSpecies.Count > 1)
                {
                    TurnPage(zoneStep);
                    ShowZone((_zone + zoneStep + _zoneSpecies.Count) % _zoneSpecies.Count);
                }

                (int dx, int dy) = IndexInput.NavigationThisFrame();
                if (dx != 0 || dy != 0)
                {
                    Select(SpeciesIndex.Move(_selected, dx, -dy, Columns, Current.Count));
                }

                Animate();
                AnimateFlip(Time.unscaledDeltaTime);
            }
            else if (IndexInput.TogglePressedThisFrame() && CanOpenNow())
            {
                Open();
            }
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

            ClaimRewards();
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

        /// <summary>Ouvre l'onglet d'une zone.</summary>
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

        /// <summary>Met à jour la page de gauche (onglets, zone, grille) et / ou celle de droite (fiche de l'espèce).</summary>
        private void Refresh(bool left = true, bool right = true)
        {
            if (_index == null)
            {
                return;
            }

            CatchLog log = Log;
            if (!left)
            {
                if (right) RefreshDetail(log, _zones.Length > _zone ? _zones[_zone] : null);
                return;
            }

            _badge.text = $"{_index.DiscoveredCount(log)}/{_index.Count}";

            for (int i = 0; i < _tabs.Count; i++)
            {
                bool active = i == _zone;
                _tabs[i].Rect.anchoredPosition = new Vector2(_tabs[i].Rect.anchoredPosition.x, active ? 462f : 452f);
                _tabs[i].Ribbon.color = active ? ZoneColors[i % ZoneColors.Length] : Color.Lerp(ZoneColors[i % ZoneColors.Length], new Color(0.4f, 0.36f, 0.3f), 0.55f);
                _tabs[i].Label.color = active ? Color.white : new Color(0.86f, 0.84f, 0.8f);
            }

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
            _help.text = _zoneSpecies.Count > 1
                ? $"{ZoneKeys()} : changer de zone      Flèches : choisir      Tab : fermer"
                : "Flèches : choisir      Tab : fermer";
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
            _timeText.text = s.ActiveTime == ActiveTime.Always ? "Toute la journée" : DayCycleMath.ActiveText(s.ActiveTime);
            _zoneText.text = zone != null ? zone.DisplayName : string.Empty;

            if (!known)
            {
                _detailName.text = "???";
                _detailScientific.text = "Espèce pas encore pêchée";
                _description.text = $"Indice : {LowerFirst(_timeText.text)}, {s.Rarity.DisplayName().ToLowerInvariant()}, " +
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
            Bait bait = _progress != null && _progress.BaitCatalog != null ? _progress.BaitCatalog.FavoriteOf(species) : null;
            if (bait == null)
            {
                return string.Empty;
            }

            string text = $"<color=#2E6B2A>Appât préféré : {bait.DisplayName.ToLowerInvariant()}</color>";
            return newLine ? "\n" + text : " " + text + ".";
        }

        private static string LowerFirst(string text) => string.IsNullOrEmpty(text) ? text : char.ToLowerInvariant(text[0]) + text.Substring(1);

        private void Animate()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
            int first = SpeciesIndex.PageOf(_selected, PerPage) * PerPage;
            for (int i = 0; i < _cells.Count; i++)
            {
                bool selected = first + i == _selected;
                _cells[i].Frame.color = selected ? Color.Lerp(Color.white, Gold, 0.35f + 0.4f * pulse) : CellTint;
                _cells[i].Frame.rectTransform.localScale = Vector3.one * (selected ? 1.05f : 1f);
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

            // Le carnet : voile, onglets (derrière la couverture), couverture, deux pages et le pli.
            Image veil = UiFactory.Plain(root, "Voile", new Color(0.02f, 0.04f, 0.06f, 0.55f));
            UiFactory.Stretch(veil.rectTransform);
            _window = veil.gameObject;
            RectTransform book = veil.rectTransform;

            for (int i = 0; i < _zones.Length; i++)
            {
                _tabs.Add(BuildTab(book, i));
            }

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

        private TabView BuildTab(RectTransform book, int i)
        {
            Image ribbon = UiFactory.Sliced(book, $"Onglet {i + 1}", _ribbon, ZoneColors[i % ZoneColors.Length]);
            RectTransform rect = ribbon.rectTransform;
            UiFactory.Center(rect, new Vector2(-640f + i * 250f, 452f), new Vector2(230f, 84f));
            Text label = UiFactory.Label(rect, "Zone", 24, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Bold(label);
            label.text = _zones[i] != null ? _zones[i].DisplayName : "?";
            UiFactory.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(-12f, 40f));
            return new TabView { Rect = rect, Ribbon = ribbon, Label = label };
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
        }

        /// <summary>Icône du livre, à droite de celle du sac, avec le compteur et la touche.</summary>
        private void BuildIcon(RectTransform root)
        {
            Image iconBase = UiFactory.Sliced(root, "Index", SkinSprite(s => s.Slot), SkinTint(new Color(0.12f, 0.2f, 0.27f)));
            RectTransform iconRect = iconBase.rectTransform;
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
