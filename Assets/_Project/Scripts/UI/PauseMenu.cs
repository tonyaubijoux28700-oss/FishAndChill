using System.Collections.Generic;
using FishAndChill.Fishing;
using FishAndChill.Player;
using FishAndChill.Save;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Menu pause : Échap (ou Start) quand aucun autre menu n'est ouvert. Le jeu se fige (<c>Time.timeScale = 0</c>).
    /// Lignes : Reprendre, Volume (←/→), Plein écran, Retour au titre (sauvegarde puis écran titre), Quitter le jeu.
    /// ↑/↓ (ou ZQSD) pour choisir, Espace / Entrée / A pour valider, Échap / B pour reprendre.
    /// Volume et plein écran sont retenus d'une partie à l'autre (<see cref="PlayerPrefs"/>).
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        private const string VolumeKey = "options.volume";
        private const string FullscreenKey = "options.pleinEcran";

        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Normal = new Color(0.86f, 0.90f, 0.94f);

        private enum Row
        {
            Resume,
            Volume,
            Fullscreen,
            Title,
            Quit,
        }

        [SerializeField] private PlayerMovement _player;
        [SerializeField] private FishingController _fishing;
        [SerializeField] private Minimap _minimap;
        [SerializeField] private SaveManager _save;

        [SerializeField, Tooltip("Charte pixel art de l'interface.")]
        private UiSkin _skin;

        private readonly List<Image> _rowFrames = new List<Image>();
        private readonly List<Text> _rowLabels = new List<Text>();
        private GameObject _window;
        private Text _hint;
        private int _selected;
        private bool _blockedLastFrame;
        private bool _resumeNextFrame;
        private float _volume = 0.8f;

        public bool IsOpen => _window != null && _window.activeSelf;

        private static int RowCount => System.Enum.GetValues(typeof(Row)).Length;

        private void Start()
        {
            _volume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            AudioListener.volume = _volume;
            if (PlayerPrefs.HasKey(FullscreenKey))
            {
                Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey) == 1;
            }

            BuildUi();
            _window.SetActive(false);
        }

        private void OnDestroy()
        {
            if (IsOpen)
            {
                Time.timeScale = 1f;
            }
        }

        private void Update()
        {
            if (_resumeNextFrame)
            {
                // Une image plus tard : la touche qui a fermé le menu ne doit pas aussi lancer la ligne.
                _resumeNextFrame = false;
                SetGameplayEnabled(true);
                return;
            }

            if (!IsOpen)
            {
                if (PauseInput.TogglePressedThisFrame() && !_blockedLastFrame && !IsBlocked())
                {
                    Open();
                }

                return;
            }

            if (PauseInput.TogglePressedThisFrame() || PauseInput.BackPressedThisFrame())
            {
                Resume();
                return;
            }

            (int dx, int dy) = IndexInput.NavigationThisFrame();
            if (dy != 0)
            {
                _selected = PauseMenuLogic.Move(_selected, -dy, RowCount);
                }

            if (dx != 0 && (Row)_selected == Row.Volume)
            {
                SetVolume(PauseMenuLogic.StepVolume(_volume, dx));
            }
            else if (dx != 0 && (Row)_selected == Row.Fullscreen)
            {
                ToggleFullscreen();
            }
            else if (PauseInput.ConfirmPressedThisFrame())
            {
                Activate((Row)_selected);
            }

            Refresh();
        }

        private void LateUpdate()
        {
            // Échap ferme aussi le sac, le livre, le marchand et la grande carte : si l'un d'eux était ouvert
            // à l'image précédente, cette pression lui revient et n'ouvre pas la pause.
            _blockedLastFrame = !IsOpen && IsBlocked();
        }

        private bool IsBlocked()
        {
            return (_player != null && _player.Locked) || (_minimap != null && _minimap.IsOpen);
        }

        public void Open()
        {
            _selected = 0;
            _window.SetActive(true);
            Time.timeScale = 0f;
            SetGameplayEnabled(false);
            Refresh();
        }

        public void Resume()
        {
            _window.SetActive(false);
            Time.timeScale = 1f;
            _resumeNextFrame = true;
        }

        private void SetGameplayEnabled(bool on)
        {
            if (_player != null)
            {
                _player.Locked = !on;
            }

            if (_fishing != null)
            {
                _fishing.enabled = on;
            }

            if (_minimap != null)
            {
                _minimap.enabled = on;
            }
        }

        private void Activate(Row row)
        {
            switch (row)
            {
                case Row.Resume:
                    Resume();
                    break;
                case Row.Volume:
                    SetVolume(_volume >= 1f ? 0f : PauseMenuLogic.StepVolume(_volume, 1));
                    break;
                case Row.Fullscreen:
                    ToggleFullscreen();
                    break;
                case Row.Title:
                    if (_save != null)
                    {
                        _save.ReturnToTitle();
                    }

                    break;
                case Row.Quit:
                    Quit();
                    break;
            }
        }

        private void SetVolume(float volume)
        {
            _volume = volume;
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat(VolumeKey, volume);
            PlayerPrefs.Save();
        }

        private void ToggleFullscreen()
        {
            bool full = !Screen.fullScreen;
            Screen.fullScreen = full;
            PlayerPrefs.SetInt(FullscreenKey, full ? 1 : 0);
            PlayerPrefs.Save();
            _fullscreenShown = full;
        }

        private bool? _fullscreenShown;

        private static void Quit()
        {
            Time.timeScale = 1f;
            // La sauvegarde s'écrit d'elle-même en quittant (SaveManager.OnApplicationQuit).
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------- Affichage ----------

        private string RowText(Row row)
        {
            switch (row)
            {
                case Row.Resume: return "Reprendre";
                case Row.Volume: return $"Volume :  <  {PauseMenuLogic.VolumeText(_volume)}  >";
                case Row.Fullscreen: return $"Plein écran :  {((_fullscreenShown ?? Screen.fullScreen) ? "Oui" : "Non")}";
                case Row.Title: return "Retour au titre";
                default: return "Quitter le jeu";
            }
        }

        private void Refresh()
        {
            for (int i = 0; i < _rowLabels.Count; i++)
            {
                bool selected = i == _selected;
                Row row = (Row)i;
                _rowLabels[i].text = RowText(row);
                _rowLabels[i].color = selected ? Gold : Normal;
                _rowFrames[i].color = selected ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                _rowFrames[i].rectTransform.localScale = selected ? new Vector3(1.04f, 1.04f, 1f) : Vector3.one;
            }

            _hint.text = (Row)_selected == Row.Volume
                ? "← → régler le volume      Échap reprendre"
                : "Haut / Bas : choisir      Espace : valider      Échap : reprendre";
        }

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Interface de la pause", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();

            // Voile sombre sur tout l'écran, puis la fenêtre au centre.
            Image veil = UiFactory.Plain(root, "Voile", new Color(0.01f, 0.03f, 0.06f, 0.62f));
            UiFactory.Stretch(veil.rectTransform);
            _window = veil.gameObject;

            Image window = UiFactory.Sliced(veil.rectTransform, "Pause", _skin != null ? _skin.Panel : null,
                _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            RectTransform panel = window.rectTransform;
            UiFactory.Center(panel, new Vector2(0f, 0f), new Vector2(620f, 620f));
            UiFactory.TitlePlaque(panel, _skin, "Pause", 40, new Vector2(0f, 300f), new Vector2(300f, 80f), out _);

            for (int i = 0; i < RowCount; i++)
            {
                Image frame = UiFactory.Sliced(panel, $"Ligne {i + 1}", _skin != null ? _skin.Slot : null, Color.white);
                UiFactory.Center(frame.rectTransform, new Vector2(0f, 175f - i * 86f), new Vector2(500f, 70f));
                Text label = UiFactory.Label(frame.rectTransform, "Texte", 30, TextAnchor.MiddleCenter, Normal);
                UiFactory.Stretch(label.rectTransform);
                _rowFrames.Add(frame);
                _rowLabels.Add(label);
            }

            _hint = UiFactory.Label(panel, "Aide", 20, TextAnchor.MiddleCenter, new Color(0.66f, 0.72f, 0.78f));
            UiFactory.Center(_hint.rectTransform, new Vector2(0f, -268f), new Vector2(580f, 32f));
        }
    }
}
