using System.Collections.Generic;
using FishAndChill.Fishing;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Carte du monde : sur le ponton, E l'ouvre. ←/→ choisit une destination, Espace (ou E, A) part, Échap ferme.
    /// Le voyage passe par un fondu au noir, puis le nom de la zone s'affiche en grand.
    /// </summary>
    public sealed class TravelMenu : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.70f, 0.76f, 0.82f);
        private static readonly Color Ink = new Color(0.86f, 0.90f, 0.94f);

        [SerializeField] private ZoneTravel _travel;
        [SerializeField] private PlayerMovement _player;
        [SerializeField] private FishingController _fishing;

        [SerializeField, Tooltip("Charte pixel art de l'interface.")]
        private UiSkin _skin;

        [SerializeField, Min(0.05f)] private float _fadeSeconds = 0.4f;
        [SerializeField, Min(0.5f)] private float _bannerSeconds = 2.6f;

        private readonly List<RectTransform> _markers = new List<RectTransform>();
        private readonly List<Text> _markerLabels = new List<Text>();
        private GameObject _window;
        private RectTransform _mapArea;
        private RectTransform _here;
        private Text _name;
        private Text _region;
        private Text _description;
        private Text _species;
        private Text _prompt;
        private Image _fade;
        private Text _banner;
        private Text _bannerSub;
        private int _selected;
        private float _fadeTimer = -1f;
        private int _pendingDestination = -1;
        private float _bannerTimer;

        public bool IsOpen => _window != null && _window.activeSelf;

        /// <summary>Un voyage est en cours (fondu au noir).</summary>
        public bool Traveling => _fadeTimer >= 0f;

        private void Start()
        {
            BuildUi();
            _window.SetActive(false);
        }

        private void Update()
        {
            UpdateFade();
            UpdateBanner();
            _prompt.gameObject.SetActive(!IsOpen && !Traveling && CanOpenNow());

            if (!IsOpen)
            {
                if (!Traveling && InteractInput.PressedThisFrame() && CanOpenNow())
                {
                    Open();
                }

                return;
            }

            if (PauseInput.TogglePressedThisFrame() || PauseInput.BackPressedThisFrame())
            {
                Close();
                return;
            }

            (int dx, int dy) = IndexInput.NavigationThisFrame();
            int step = dx != 0 ? dx : -dy;
            if (step != 0 && _travel.Destinations.Count > 0)
            {
                _selected = PauseMenuLogic.Move(_selected, step, _travel.Destinations.Count);
                Refresh();
            }
            else if (PauseInput.ConfirmPressedThisFrame() || InteractInput.PressedThisFrame())
            {
                Depart(_selected);
            }
        }

        private bool CanOpenNow()
        {
            if (_travel == null || !_travel.PlayerAtDock)
            {
                return false;
            }

            if (_fishing != null && _fishing.State != FishingState.Idle && _fishing.State != FishingState.Result)
            {
                return false;
            }

            return _player == null || !_player.Locked;
        }

        public void Open()
        {
            _selected = _travel.Current;
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

        /// <summary>Part vers la destination (fondu au noir, puis arrivée sur son ponton).</summary>
        public void Depart(int index)
        {
            if (index == _travel.Current)
            {
                _description.text = "Tu es déjà ici ! Choisis une autre destination avec ← →.";
                return;
            }

            _window.SetActive(false);
            _pendingDestination = index;
            _fadeTimer = 0f;
            if (_player != null)
            {
                _player.Locked = true;
            }
        }

        // ---------- Voyage : fondu et bandeau ----------

        private void UpdateFade()
        {
            if (_fadeTimer < 0f)
            {
                return;
            }

            _fadeTimer += Time.unscaledDeltaTime;
            float half = _fadeSeconds;
            if (_pendingDestination >= 0 && _fadeTimer >= half)
            {
                // Écran noir : on change de zone à l'abri des regards.
                _travel.TravelTo(_pendingDestination);
                _pendingDestination = -1;
                ShowBanner();
            }

            float alpha = _fadeTimer < half ? _fadeTimer / half : 1f - (_fadeTimer - half - 0.15f) / half;
            SetAlpha(_fade, Mathf.Clamp01(alpha));
            if (_fadeTimer >= half * 2f + 0.15f)
            {
                _fadeTimer = -1f;
                SetAlpha(_fade, 0f);
                if (_player != null)
                {
                    _player.Locked = false;
                }
            }
        }

        private void ShowBanner()
        {
            FishingZone zone = _travel.CurrentDestination != null ? _travel.CurrentDestination.Zone : null;
            if (zone == null)
            {
                return;
            }

            _banner.text = zone.DisplayName;
            _bannerSub.text = zone.Region;
            _bannerTimer = _bannerSeconds;
        }

        private void UpdateBanner()
        {
            if (_bannerTimer <= 0f)
            {
                _banner.gameObject.SetActive(false);
                return;
            }

            _bannerTimer -= Time.unscaledDeltaTime;
            _banner.gameObject.SetActive(true);
            // Apparaît, reste, puis s'efface.
            float shown = _bannerSeconds - _bannerTimer;
            float alpha = Mathf.Min(Mathf.Clamp01(shown / 0.4f), Mathf.Clamp01(_bannerTimer / 0.6f));
            SetAlpha(_banner, alpha);
            SetAlpha(_bannerSub, alpha);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        // ---------- Affichage ----------

        private void Refresh()
        {
            IReadOnlyList<ZoneTravel.Destination> destinations = _travel.Destinations;
            Vector2 size = _mapArea.rect.size;
            for (int i = 0; i < _markers.Count; i++)
            {
                FishingZone zone = destinations[i].Zone;
                bool selected = i == _selected;
                _markers[i].localScale = selected ? Vector3.one * 1.5f : Vector3.one;
                _markerLabels[i].color = selected ? Gold : Ink;
                if (i == _travel.Current)
                {
                    _here.anchoredPosition = MarkerPosition(zone, size) + new Vector2(0f, 46f);
                }
            }

            FishingZone chosen = destinations.Count > 0 ? destinations[_selected].Zone : null;
            if (chosen == null)
            {
                return;
            }

            _name.text = chosen.DisplayName;
            _region.text = chosen.Region;
            _description.text = _selected == _travel.Current ? $"{chosen.Description}\nTu es ici." : chosen.Description;
            int discovered = 0;
            foreach (FishingZone.SpeciesEntry entry in chosen.Species)
            {
                if (entry.Species != null && _fishing != null && _fishing.CatchLog.IsDiscovered(entry.Species.Id))
                {
                    discovered++;
                }
            }

            _species.text = $"Espèces découvertes : {discovered} / {chosen.Species.Count}";
        }

        private static Vector2 MarkerPosition(FishingZone zone, Vector2 size)
        {
            Vector2 p = zone != null ? zone.MapPosition : new Vector2(0.5f, 0.5f);
            return new Vector2((p.x - 0.5f) * size.x, (p.y - 0.5f) * size.y);
        }

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Interface du voyage", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();

            // Invitation sur le ponton.
            _prompt = UiFactory.Label(root, "Invitation", 30, TextAnchor.MiddleCenter, Gold);
            UiFactory.Place(_prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(700f, 44f));
            _prompt.text = "E : prendre le bateau (carte du monde)";

            Image window = UiFactory.Sliced(root, "Carte du monde", _skin != null ? _skin.Panel : null, _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            _window = window.gameObject;
            RectTransform panel = window.rectTransform;
            UiFactory.Center(panel, new Vector2(0f, 10f), new Vector2(1240f, 860f));
            UiFactory.TitlePlaque(panel, _skin, "Carte du monde", 38, new Vector2(0f, 420f), new Vector2(500f, 80f), out _);

            // La mer, quadrillée comme une carte marine (méridiens et parallèles).
            Image sea = UiFactory.Sliced(panel, "Mer", _skin != null ? _skin.WaterBackdrop : null, _skin != null ? Color.white : new Color(0.16f, 0.42f, 0.55f));
            _mapArea = sea.rectTransform;
            UiFactory.Center(_mapArea, new Vector2(0f, 95f), new Vector2(1120f, 520f));
            for (int i = 1; i < 8; i++)
            {
                Image meridian = UiFactory.Plain(_mapArea, "Méridien", new Color(1f, 1f, 1f, 0.10f));
                UiFactory.Place(meridian.rectTransform, new Vector2(i / 8f, 0f), new Vector2(i / 8f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, -16f));
            }

            for (int i = 1; i < 4; i++)
            {
                Image parallel = UiFactory.Plain(_mapArea, "Parallèle", new Color(1f, 1f, 1f, i == 2 ? 0.18f : 0.10f));
                UiFactory.Place(parallel.rectTransform, new Vector2(0f, i / 4f), new Vector2(1f, i / 4f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, 2f));
            }

            Vector2 size = new Vector2(1120f, 520f);
            IReadOnlyList<ZoneTravel.Destination> destinations = _travel.Destinations;

            // Routes en pointillés entre les destinations.
            for (int i = 0; i + 1 < destinations.Count; i++)
            {
                Vector2 a = MarkerPosition(destinations[i].Zone, size);
                Vector2 b = MarkerPosition(destinations[i + 1].Zone, size);
                int dots = Mathf.Max(2, Mathf.RoundToInt(Vector2.Distance(a, b) / 22f));
                for (int d = 1; d < dots; d++)
                {
                    Image dot = UiFactory.Plain(_mapArea, "Route", new Color(1f, 0.95f, 0.80f, 0.55f));
                    UiFactory.Center(dot.rectTransform, Vector2.Lerp(a, b, d / (float)dots) + Vector2.up * Mathf.Sin(d / (float)dots * Mathf.PI) * 40f, new Vector2(6f, 6f));
                }
            }

            foreach (ZoneTravel.Destination destination in destinations)
            {
                Vector2 position = MarkerPosition(destination.Zone, size);
                Image marker = UiFactory.Sliced(_mapArea, "Repère", _skin != null ? _skin.Gem : null, _skin != null ? Gold : Gold);
                marker.type = Image.Type.Simple;
                UiFactory.Center(marker.rectTransform, position, new Vector2(28f, 28f));
                Text label = UiFactory.Label(_mapArea, "Nom", 26, TextAnchor.MiddleCenter, Ink);
                UiFactory.Center(label.rectTransform, position + new Vector2(0f, -36f), new Vector2(320f, 34f));
                label.text = destination.Zone != null ? destination.Zone.DisplayName : "?";
                _markers.Add(marker.rectTransform);
                _markerLabels.Add(label);
            }

            _here = UiFactory.Label(_mapArea, "Vous êtes ici", 22, TextAnchor.MiddleCenter, Gold).rectTransform;
            _here.GetComponent<Text>().text = "Tu es ici";
            _here.sizeDelta = new Vector2(220f, 30f);

            // Fiche de la destination choisie.
            _name = UiFactory.Label(panel, "Destination", 40, TextAnchor.MiddleLeft, Gold);
            UiFactory.Center(_name.rectTransform, new Vector2(-180f, -205f), new Vector2(760f, 50f));
            UiFactory.Bold(_name);
            _region = UiFactory.Label(panel, "Région", 24, TextAnchor.MiddleLeft, Muted);
            UiFactory.Center(_region.rectTransform, new Vector2(-180f, -243f), new Vector2(760f, 30f));
            _description = UiFactory.Label(panel, "Description", 24, TextAnchor.UpperLeft, Ink);
            UiFactory.Center(_description.rectTransform, new Vector2(-180f, -300f), new Vector2(760f, 70f));
            _species = UiFactory.Label(panel, "Espèces", 26, TextAnchor.MiddleRight, Ink);
            UiFactory.Center(_species.rectTransform, new Vector2(380f, -230f), new Vector2(380f, 40f));

            Text hint = UiFactory.Label(panel, "Aide", 22, TextAnchor.MiddleCenter, Muted);
            UiFactory.Center(hint.rectTransform, new Vector2(0f, -385f), new Vector2(1100f, 32f));
            hint.text = "← → choisir la destination      Espace partir      Échap fermer";

            // Fondu au noir du voyage, puis grand bandeau avec le nom de la zone.
            _fade = UiFactory.Plain(root, "Fondu", new Color(0f, 0f, 0f, 0f));
            UiFactory.Stretch(_fade.rectTransform);
            _banner = UiFactory.Label(root, "Arrivée", 72, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.82f, 0f));
            UiFactory.Center(_banner.rectTransform, new Vector2(0f, 250f), new Vector2(1400f, 100f));
            UiFactory.Bold(_banner);
            _bannerSub = UiFactory.Label(_banner.rectTransform, "Région", 32, TextAnchor.MiddleCenter, new Color(0.85f, 0.90f, 0.95f, 0f));
            UiFactory.Center(_bannerSub.rectTransform, new Vector2(0f, -66f), new Vector2(1200f, 44f));
            _banner.gameObject.SetActive(false);
        }
    }
}
