using System.Collections.Generic;
using FishAndChill.Fishing;
using FishAndChill.Player;
using FishAndChill.Save;
using FishAndChill.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Écran titre, posé sur l'île vivante tant qu'aucun emplacement de sauvegarde n'est choisi
    /// (<see cref="SaveManager.ActiveSlot"/>) : logo et 3 emplacements (zone, espèces, pièces, temps de jeu, date).
    /// ←/→ choisir, Espace / A jouer (reprise, ou nouvelle partie sur un emplacement vide), Suppr / X effacer (à valider
    /// deux fois), ↓ Quitter. Le jeu est bloqué derrière ; choisir un emplacement recharge la scène sur cette partie.
    /// </summary>
    public sealed class TitleScreen : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Normal = new Color(0.90f, 0.93f, 0.96f);
        private static readonly Color Muted = new Color(0.64f, 0.70f, 0.76f);
        private static readonly Color Danger = new Color(1.00f, 0.55f, 0.45f);

        [SerializeField] private SaveManager _save;
        [SerializeField] private PlayerMovement _player;
        [SerializeField] private FishingController _fishing;
        [SerializeField] private Minimap _minimap;

        [SerializeField, Tooltip("Zones du jeu : nombre total d'espèces et nom de la zone de chaque partie.")]
        private FishingZone[] _zones = new FishingZone[0];

        [SerializeField, Tooltip("Charte pixel art de l'interface.")]
        private UiSkin _skin;

        private readonly List<Image> _cards = new List<Image>();
        private readonly List<Text> _cardTexts = new List<Text>();
        private readonly List<Text> _cardFooters = new List<Text>();
        private readonly SaveData[] _slots = new SaveData[SaveSlots.Count];
        private Canvas _canvas;
        private RectTransform _logo;
        private Image _quitFrame;
        private Text _quitLabel;
        private Text _hint;
        private int _selected;
        private int _lastSlot;
        private int _confirmDelete = -1;
        private int _hideFrames = 3;

        public bool IsOpen => _canvas != null;

        private void Start()
        {
            if (SaveManager.InGame)
            {
                enabled = false;
                return;
            }

            BuildUi();
            ReadSlots();
            SetGameplayEnabled(false);
            Refresh();
        }

        private void Update()
        {
            if (_canvas == null)
            {
                return;
            }

            if (_hideFrames > 0)
            {
                // Les autres interfaces (sac, livre, minimap…) se construisent à leur Start : on les masque
                // pendant les premières images.
                _hideFrames--;
                HideOtherCanvases();
            }

            if (_player != null)
            {
                _player.Locked = true;
            }

            float bob = Mathf.Sin(Time.unscaledTime * 1.6f) * 8f;
            _logo.anchoredPosition = new Vector2(0f, 330f + bob);

            (int dx, int dy) = IndexInput.NavigationThisFrame();
            if (dx != 0 || dy != 0)
            {
                _selected = TitleScreenLogic.Navigate(_selected, _lastSlot, dx, dy, SaveSlots.Count);
                if (_selected < SaveSlots.Count)
                {
                    _lastSlot = _selected;
                }

                _confirmDelete = -1;
            }

            if (DeletePressedThisFrame() && _selected < SaveSlots.Count && _slots[_selected] != null)
            {
                if (_confirmDelete == _selected)
                {
                    SaveManager.DeleteSlot(_selected);
                    _confirmDelete = -1;
                    ReadSlots();
                }
                else
                {
                    _confirmDelete = _selected;
                }
            }
            else if (IndexInput.ClosePressedThisFrame())
            {
                _confirmDelete = -1;
            }
            else if (PauseInput.ConfirmPressedThisFrame())
            {
                Activate();
            }

            Refresh();
        }

        /// <summary>Joue l'emplacement choisi (utilisable par script pour les tests).</summary>
        public void Play(int slot)
        {
            if (_save != null)
            {
                _save.PlaySlot(slot);
            }
        }

        private void Activate()
        {
            if (_selected == TitleScreenLogic.Quit)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                return;
            }

            Play(_selected);
        }

        private static bool DeletePressedThisFrame()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.deleteKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame))
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonWest.wasPressedThisFrame;
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

        private void HideOtherCanvases()
        {
            foreach (Canvas canvas in FindObjectsByType<Canvas>())
            {
                if (canvas != _canvas && canvas.isRootCanvas)
                {
                    canvas.enabled = false;
                }
            }
        }

        private void ReadSlots()
        {
            for (int i = 0; i < SaveSlots.Count; i++)
            {
                _slots[i] = SaveManager.Peek(i);
            }
        }

        // ---------- Affichage ----------

        private int TotalSpecies()
        {
            var ids = new HashSet<string>();
            foreach (FishingZone zone in _zones)
            {
                if (zone == null)
                {
                    continue;
                }

                foreach (FishingZone.SpeciesEntry entry in zone.Species)
                {
                    if (entry.Species != null)
                    {
                        ids.Add(entry.Species.Id);
                    }
                }
            }

            return ids.Count;
        }

        private string ZoneName(string id)
        {
            foreach (FishingZone zone in _zones)
            {
                if (zone != null && (zone.Id == id || string.IsNullOrEmpty(id)))
                {
                    return zone.DisplayName;
                }
            }

            return _zones.Length > 0 && _zones[0] != null ? _zones[0].DisplayName : string.Empty;
        }

        private void Refresh()
        {
            int total = TotalSpecies();
            for (int i = 0; i < SaveSlots.Count; i++)
            {
                bool selected = i == _selected;
                SaveData data = _slots[i];
                _cards[i].color = selected ? Color.white : new Color(0.62f, 0.67f, 0.74f);
                _cards[i].rectTransform.localScale = selected ? new Vector3(1.05f, 1.05f, 1f) : Vector3.one;
                if (data == null)
                {
                    _cardTexts[i].text = "Vide";
                    _cardTexts[i].color = Muted;
                    _cardFooters[i].text = selected ? "Espace : nouvelle partie" : "Nouvelle partie";
                    _cardFooters[i].color = selected ? Gold : Muted;
                    continue;
                }

                _cardTexts[i].color = Normal;
                _cardTexts[i].text =
                    $"<color=#FFD64D>{ZoneName(data.Zone)}</color>\n" +
                    $"Espèces : {SaveSlots.SpeciesDiscovered(data)} / {total}\n" +
                    $"Pièces : {data.Coins}\n" +
                    $"Temps de jeu : {SaveSlots.PlayTimeText(data.PlaySeconds)}";
                if (_confirmDelete == i)
                {
                    _cardFooters[i].text = "Effacer ? Suppr encore";
                    _cardFooters[i].color = Danger;
                }
                else
                {
                    _cardFooters[i].text = selected ? "Espace : continuer" : SaveSlots.SavedAtText(data.SavedAtTicks);
                    _cardFooters[i].color = selected ? Gold : Muted;
                }
            }

            bool quit = _selected == TitleScreenLogic.Quit;
            _quitFrame.color = quit ? Color.white : new Color(0.62f, 0.67f, 0.74f);
            _quitFrame.rectTransform.localScale = quit ? new Vector3(1.05f, 1.05f, 1f) : Vector3.one;
            _quitLabel.color = quit ? Gold : Normal;
            _hint.text = quit
                ? "Haut : revenir aux parties      Espace : quitter le jeu"
                : _slots[_selected] != null
                    ? "← → choisir      Espace : jouer      Suppr : effacer      Bas : quitter"
                    : "← → choisir      Espace : jouer      Bas : quitter";
        }

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Écran titre", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 30;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();

            // Voile léger : l'île reste visible et vivante derrière.
            Image veil = UiFactory.Plain(root, "Voile", new Color(0.01f, 0.05f, 0.09f, 0.42f));
            UiFactory.Stretch(veil.rectTransform);

            // Logo qui flotte doucement.
            var logoObject = new GameObject("Logo", typeof(RectTransform));
            _logo = logoObject.GetComponent<RectTransform>();
            _logo.SetParent(root, false);
            UiFactory.Center(_logo, new Vector2(0f, 330f), new Vector2(1200f, 220f));
            Text title = UiFactory.Label(_logo, "Titre", 150, TextAnchor.MiddleCenter, Gold);
            UiFactory.Bold(title);
            title.text = "Fish & Chill";
            UiFactory.Center(title.rectTransform, new Vector2(0f, 30f), new Vector2(1200f, 170f));
            Text subtitle = UiFactory.Label(_logo, "Sous-titre", 34, TextAnchor.MiddleCenter, Normal);
            subtitle.text = "Pêche, collectionne, remplis ton aquarium";
            UiFactory.Center(subtitle.rectTransform, new Vector2(0f, -70f), new Vector2(1200f, 50f));

            for (int i = 0; i < SaveSlots.Count; i++)
            {
                Image card = UiFactory.Sliced(root, $"Emplacement {i + 1}", _skin != null ? _skin.Panel : null,
                    _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
                RectTransform rect = card.rectTransform;
                UiFactory.Center(rect, new Vector2((i - 1) * 500f, -40f), new Vector2(440f, 330f));
                UiFactory.TitlePlaque(rect, _skin, $"Partie {i + 1}", 30, new Vector2(0f, 160f), new Vector2(240f, 62f), out _);

                Text text = UiFactory.Label(rect, "Résumé", 30, TextAnchor.MiddleCenter, Normal);
                text.supportRichText = true;
                text.lineSpacing = 1.1f;
                UiFactory.Center(text.rectTransform, new Vector2(0f, 10f), new Vector2(400f, 200f));
                Text footer = UiFactory.Label(rect, "Pied", 24, TextAnchor.MiddleCenter, Muted);
                UiFactory.Center(footer.rectTransform, new Vector2(0f, -128f), new Vector2(400f, 40f));
                _cards.Add(card);
                _cardTexts.Add(text);
                _cardFooters.Add(footer);
            }

            _quitFrame = UiFactory.Sliced(root, "Quitter", _skin != null ? _skin.Slot : null, Color.white);
            UiFactory.Center(_quitFrame.rectTransform, new Vector2(0f, -310f), new Vector2(300f, 66f));
            _quitLabel = UiFactory.Label(_quitFrame.rectTransform, "Texte", 30, TextAnchor.MiddleCenter, Normal);
            _quitLabel.text = "Quitter le jeu";
            UiFactory.Stretch(_quitLabel.rectTransform);

            _hint = UiFactory.Label(root, "Aide", 26, TextAnchor.MiddleCenter, Normal);
            UiFactory.Center(_hint.rectTransform, new Vector2(0f, -420f), new Vector2(1400f, 36f));
        }
    }
}
