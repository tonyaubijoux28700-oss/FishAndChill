using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Le panneau de l'ascenseur de l'aquarium : devant les portes, E ouvre la liste des étages (rez-de-chaussée = hall,
    /// puis une salle par zone) avec l'avancée de chaque bassin (« 12 / 14 espèces »). Haut / Bas pour choisir, E ou Espace
    /// pour y aller, Échap pour fermer. Le trajet : fondu au noir, arrivée devant les portes de l'étage, bandeau
    /// « Étage 2 — Lac d'Annecy ». La liste défile quand il y a beaucoup d'étages.
    /// </summary>
    public sealed class ElevatorMenu : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.70f, 0.76f, 0.82f);
        private static readonly Color Ink = new Color(0.90f, 0.93f, 0.96f);
        private const int VisibleRows = 7;
        private const float RowHeight = 76f;

        [SerializeField] private AquariumBuilding _aquarium;
        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private PlayerMovement _player;
        [SerializeField] private UiSkin _skin;
        [SerializeField, Min(0.05f)] private float _fadeSeconds = 0.35f;
        [SerializeField, Min(0.5f)] private float _bannerSeconds = 2f;

        private sealed class Row
        {
            public Image Frame;
            public Text Floor;
            public Text Name;
            public Text Count;
        }

        private readonly List<Row> _rows = new List<Row>();
        private GameObject _window;
        private Text _prompt;
        private Text _help;
        private Image _fade;
        private Text _banner;
        private int _selected;
        private int _scroll;
        private int _pending = -1;
        private float _fadeTimer = -1f;
        private float _bannerTimer;
        private int _openedFrame;
        private bool _armed = true;   // marcher vers le bas sur le tapis appelle l'ascenseur, une fois par appui

        /// <summary>Le menu est ouvert ou l'ascenseur roule : E ne doit rien faire d'autre.</summary>
        public static bool Busy { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Busy = false;

        public bool IsOpen => _window != null && _window.activeSelf;
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
            Busy = IsOpen || Traveling;
            bool canCall = _aquarium != null && _aquarium.PlayerAtElevator >= 0 && !IsOpen && !Traveling && _player != null && !_player.Locked;
            _prompt.gameObject.SetActive(canCall);
            if (!IsOpen)
            {
                // Comme le paillasson : marcher vers le bas sur le tapis (le mur de l'ascenseur) l'appelle.
                bool pushingDown = _player != null && _player.LastInput.y < -0.5f;
                if (!pushingDown) _armed = true;
                if (canCall && ((pushingDown && _armed) || InteractInput.PressedThisFrame()))
                {
                    _armed = false;
                    Open();
                }

                return;
            }

            if (Traveling || Time.frameCount == _openedFrame)
            {
                return;
            }

            if (PauseInput.TogglePressedThisFrame() || PauseInput.BackPressedThisFrame())
            {
                Close();
                return;
            }

            (int dx, int dy) = IndexInput.NavigationThisFrame();
            int step = dy != 0 ? dy : dx;   // Haut = étage au-dessus
            if (step != 0)
            {
                _selected = Mathf.Clamp(_selected + step, 0, _aquarium.FloorCount - 1);
                Refresh();
            }
            else if (PauseInput.ConfirmPressedThisFrame() || InteractInput.PressedThisFrame())
            {
                Go(_selected);
            }
        }

        /// <summary>Ouvre le panneau (public pour les tests).</summary>
        public void Open()
        {
            _selected = _aquarium.CurrentFloor;
            _openedFrame = Time.frameCount;
            _window.SetActive(true);
            if (_player != null) _player.Locked = true;
            Busy = true;
            Refresh();
        }

        public void Close()
        {
            _window.SetActive(false);
            if (_player != null) _player.Locked = false;
        }

        /// <summary>Part vers un étage : fondu au noir, changement d'étage, bandeau (public pour les tests).</summary>
        public void Go(int floor)
        {
            if (floor == _aquarium.CurrentFloor)
            {
                _help.text = "Tu es déjà à cet étage !";
                return;
            }

            _pending = floor;
            _fadeTimer = 0f;
            if (_player != null) _player.Locked = true;
        }

        private void UpdateFade()
        {
            if (_fadeTimer < 0f)
            {
                return;
            }

            _fadeTimer += Time.unscaledDeltaTime;
            float half = _fadeSeconds;
            if (_pending >= 0 && _fadeTimer >= half)
            {
                // Écran noir : la cabine arrive à l'étage.
                _window.SetActive(false);
                _aquarium.GoToFloor(_pending);
                _banner.text = FloorTitle(_pending);
                _bannerTimer = _bannerSeconds;
                _pending = -1;
            }

            float hold = 0.25f;
            float alpha = _fadeTimer < half ? _fadeTimer / half : 1f - (_fadeTimer - half - hold) / half;
            SetAlpha(_fade, Mathf.Clamp01(alpha));
            if (_fadeTimer >= half * 2f + hold)
            {
                _fadeTimer = -1f;
                SetAlpha(_fade, 0f);
                if (_player != null) _player.Locked = false;
            }
        }

        private void UpdateBanner()
        {
            if (_bannerTimer <= 0f)
            {
                SetAlpha(_banner, 0f);
                return;
            }

            _bannerTimer -= Time.unscaledDeltaTime;
            float t = _bannerSeconds - _bannerTimer;
            float alpha = Mathf.Clamp01(Mathf.Min(t / 0.3f, _bannerTimer / 0.5f));
            SetAlpha(_banner, alpha);
        }

        private string FloorLabel(int floor) => floor == 0 ? "RDC" : $"Étage {floor}";

        private string FloorTitle(int floor)
        {
            FishingZone zone = _aquarium.FloorZone(floor);
            string name = zone != null ? zone.DisplayName : "Bassins";
            return floor == 0 ? $"Rez-de-chaussée — {name}" : $"Étage {floor} — {name}";
        }

        /// <summary>Espèces de la zone exposées dans l'aquarium, sur le total de la zone.</summary>
        private (int shown, int total) Progress(FishingZone zone)
        {
            if (zone == null)
            {
                return (0, 0);
            }

            int shown = 0, total = 0;
            foreach (FishingZone.SpeciesEntry entry in zone.Species)
            {
                if (entry.Species == null) continue;
                total++;
                if (_progress != null && _progress.Aquarium != null && _progress.Aquarium.Contains(entry.Species.Id)) shown++;
            }

            return (shown, total);
        }

        private void Refresh()
        {
            int count = _aquarium.FloorCount;
            int shownAt = count - 1 - _selected;   // position du choix dans la liste (le haut de la tour en premier)
            _scroll = Mathf.Clamp(Mathf.Clamp(_scroll, shownAt - VisibleRows + 1, shownAt), 0, Mathf.Max(0, count - VisibleRows));
            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                int floor = _scroll + i;
                bool used = floor < count;
                row.Frame.gameObject.SetActive(used);
                if (!used) continue;

                // Le haut de la tour en haut de la liste : les étages s'empilent comme dans un vrai ascenseur.
                floor = count - 1 - floor;
                FishingZone zone = _aquarium.FloorZone(floor);
                (int shown, int total) = Progress(zone);
                bool selected = floor == _selected;
                bool here = floor == _aquarium.CurrentFloor;
                row.Floor.text = FloorLabel(floor);
                row.Name.text = (zone != null ? zone.DisplayName : "Bassins") + (here ? "   (tu es ici)" : string.Empty);
                row.Count.text = total > 0 ? $"{shown} / {total}" : string.Empty;
                row.Count.color = total > 0 && shown >= total ? Gold : Ink;
                row.Frame.color = selected ? new Color(1f, 0.86f, 0.40f) : new Color(1f, 1f, 1f, 0.55f);
                row.Floor.color = selected ? Gold : Muted;
                row.Name.color = selected ? Color.white : Ink;
            }

            _help.text = "Haut / Bas : étage  —  E ou Espace : y aller  —  Échap : fermer";
        }

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Interface de l'ascenseur", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();

            _prompt = UiFactory.Label(root, "Invitation", 30, TextAnchor.MiddleCenter, Gold);
            UiFactory.Place(_prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(700f, 44f));
            _prompt.text = "Bas ou E : prendre l'ascenseur";

            Image window = UiFactory.Sliced(root, "Ascenseur", _skin != null ? _skin.Panel : null, _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            _window = window.gameObject;
            RectTransform panel = window.rectTransform;
            float height = 200f + VisibleRows * RowHeight;
            UiFactory.Center(panel, Vector2.zero, new Vector2(860f, height));
            UiFactory.TitlePlaque(panel, _skin, "Ascenseur", 38, new Vector2(0f, height * 0.5f - 10f), new Vector2(420f, 76f), out _);

            float top = height * 0.5f - 100f;
            for (int i = 0; i < VisibleRows; i++)
            {
                var row = new Row();
                row.Frame = UiFactory.Sliced(panel, $"Étage {i}", _skin != null ? _skin.Slot : null, Color.white);
                UiFactory.Center(row.Frame.rectTransform, new Vector2(0f, top - i * RowHeight - RowHeight * 0.5f), new Vector2(760f, RowHeight - 10f));
                RectTransform r = row.Frame.rectTransform;
                row.Floor = UiFactory.Label(r, "Numéro", 26, TextAnchor.MiddleLeft, Muted);
                UiFactory.Center(row.Floor.rectTransform, new Vector2(-280f, 0f), new Vector2(160f, 50f));
                row.Name = UiFactory.Label(r, "Zone", 28, TextAnchor.MiddleLeft, Ink);
                UiFactory.Center(row.Name.rectTransform, new Vector2(30f, 0f), new Vector2(460f, 50f));
                row.Count = UiFactory.Label(r, "Avancée", 26, TextAnchor.MiddleRight, Ink);
                UiFactory.Center(row.Count.rectTransform, new Vector2(300f, 0f), new Vector2(140f, 50f));
                _rows.Add(row);
            }

            _help = UiFactory.Label(panel, "Aide", 22, TextAnchor.MiddleCenter, Muted);
            UiFactory.Center(_help.rectTransform, new Vector2(0f, -height * 0.5f + 45f), new Vector2(800f, 40f));

            _fade = UiFactory.Plain(root, "Fondu", new Color(0f, 0f, 0f, 0f));
            _fade.raycastTarget = false;
            UiFactory.Stretch(_fade.rectTransform);

            _banner = UiFactory.Label(root, "Bandeau de l'étage", 52, TextAnchor.MiddleCenter, Gold);
            UiFactory.Place(_banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1400f, 90f));
            SetAlpha(_banner, 0f);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            Color c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }
    }
}
