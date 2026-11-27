using System.Collections.Generic;
using FishAndChill.Collection;
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
        private readonly List<Text> _cardTitles = new List<Text>();
        private CharacterCreator _creator;
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

        // Écran titre vivant : décor de la dernière partie, poissons qui sautent, bulles.
        private sealed class Jumper
        {
            public Image Image;
            public float Age = -1f;
            public float Duration;
            public Vector2 From;
            public Vector2 To;
            public float Height;
        }

        private readonly List<Jumper> _jumpers = new List<Jumper>();
        private readonly List<RectTransform> _bubbles = new List<RectTransform>();
        private readonly List<float> _bubbleSpeed = new List<float>();
        private readonly System.Random _random = new System.Random();
        private Text _lastStop;
        private FishingZone _lastZone;
        private bool _backdropDone;
        private float _nextJump = 0.8f;

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
            if (!_backdropDone)
            {
                // Après les Start de tout le monde : on se place dans la zone de la dernière partie jouée.
                _backdropDone = true;
                SetBackdrop();
            }

            AnimateLife(Time.unscaledDeltaTime);
            if (_creator != null && _creator.IsOpen)
            {
                _creator.Update(Time.unscaledDeltaTime);
                return;
            }

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

            if (_slots[_selected] == null && _creator != null)
            {
                // Nouvelle partie : on crée d'abord son pêcheur.
                SetTitleVisible(false);
                _creator.Open(_selected);
                return;
            }

            Play(_selected);
        }

        /// <summary>Ouvre la création du pêcheur sur cet emplacement (utilisable par script pour les tests).</summary>
        public CharacterCreator OpenCreator(int slot)
        {
            _selected = Mathf.Clamp(slot, 0, SaveSlots.Count - 1);
            SetTitleVisible(false);
            _creator.Open(_selected);
            return _creator;
        }

        private void SetTitleVisible(bool visible)
        {
            _logo.gameObject.SetActive(visible);
            _quitFrame.gameObject.SetActive(visible);
            _hint.gameObject.SetActive(visible);
            foreach (Image card in _cards)
            {
                card.gameObject.SetActive(visible);
            }
        }

        private void OnCreatorConfirmed(int slot, CharacterLook look)
        {
            SaveManager.PendingLook = look;
            Play(slot);
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

        // ---------- Écran titre vivant ----------

        private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);

        /// <summary>Le décor derrière le titre : la zone et l'heure de la partie jouée le plus récemment.</summary>
        private void SetBackdrop()
        {
            SaveData latest = null;
            foreach (SaveData data in _slots)
            {
                if (data != null && (latest == null || data.SavedAtTicks > latest.SavedAtTicks)) latest = data;
            }

            var travel = FindAnyObjectByType<ZoneTravel>();
            if (latest != null && travel != null)
            {
                int index = travel.IndexOf(latest.Zone);
                if (index >= 0 && index != travel.Current) travel.TravelTo(index);
                var cycle = FindAnyObjectByType<DayNightCycle>();
                if (cycle != null) cycle.SetHour(latest.Hour);
            }

            _lastZone = travel != null && travel.CurrentDestination != null ? travel.CurrentDestination.Zone : null;
            _lastStop.text = latest != null && _lastZone != null ? $"Dernière escale : {_lastZone.DisplayName}" : string.Empty;
        }

        private void AnimateLife(float dt)
        {
            float time = Time.unscaledTime;

            // Bulles qui montent en ondulant, et repartent d'en bas.
            for (int i = 0; i < _bubbles.Count; i++)
            {
                RectTransform bubble = _bubbles[i];
                Vector2 p = bubble.anchoredPosition;
                p.y += _bubbleSpeed[i] * dt;
                p.x += Mathf.Sin(time * 1.5f + i) * 12f * dt;
                if (p.y > 580f) p = new Vector2(Range(-940f, 940f), -580f);
                bubble.anchoredPosition = p;
            }

            // Un poisson de la zone saute de temps en temps, de derrière les emplacements, devant le logo.
            _nextJump -= dt;
            if (_nextJump <= 0f)
            {
                _nextJump = Range(1.1f, 2.4f);
                Jump();
            }

            foreach (Jumper jumper in _jumpers)
            {
                if (jumper.Age < 0f) continue;
                jumper.Age += dt;
                float t = jumper.Age / jumper.Duration;
                if (t >= 1f)
                {
                    jumper.Age = -1f;
                    jumper.Image.gameObject.SetActive(false);
                    continue;
                }

                Vector2 p = Vector2.Lerp(jumper.From, jumper.To, t) + Vector2.up * (4f * jumper.Height * t * (1f - t));
                Vector2 v = (jumper.To - jumper.From) + Vector2.up * (4f * jumper.Height * (1f - 2f * t));
                RectTransform rect = jumper.Image.rectTransform;
                rect.anchoredPosition = p;
                bool left = v.x < 0f;
                rect.localScale = new Vector3(left ? -1f : 1f, 1f, 1f);
                float angle = Mathf.Atan2(v.y, Mathf.Abs(v.x)) * Mathf.Rad2Deg;
                rect.localRotation = Quaternion.Euler(0f, 0f, left ? -angle : angle);
            }
        }

        private void Jump()
        {
            if (_lastZone == null || _lastZone.Species.Count == 0) return;
            Jumper free = _jumpers.Find(j => j.Age < 0f);
            if (free == null) return;
            FishSpecies species = null;
            for (int attempt = 0; attempt < 8 && species == null; attempt++)
            {
                FishSpecies candidate = _lastZone.Species[_random.Next(_lastZone.Species.Count)].Species;
                if (candidate != null && candidate.Sprite != null && candidate.Kind == CreatureKind.Fish) species = candidate;
            }

            if (species == null) return;
            float direction = _random.NextDouble() < 0.5 ? -1f : 1f;
            free.From = new Vector2(Range(-760f, 760f), 40f);
            free.To = free.From + new Vector2(direction * Range(240f, 380f), 0f);
            free.Height = Range(280f, 380f);
            free.Duration = Range(1.1f, 1.5f);
            free.Age = 0f;
            free.Image.sprite = species.Sprite;
            Vector2 size = species.Sprite.rect.size * 2.2f;
            if (size.x > 240f) size *= 240f / size.x;
            free.Image.rectTransform.sizeDelta = size;
            free.Image.gameObject.SetActive(true);
        }

        private static Sprite Circle(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f));
                    float r = size * 0.5f - 0.5f;
                    Color c = d > r ? Color.clear : d > r - 1.2f ? new Color(1f, 1f, 1f, 0.75f) : new Color(1f, 1f, 1f, 0.18f);
                    if (d < r && x < size * 0.4f && y > size * 0.6f && d > r - 2.5f) c = Color.white; // reflet
                    texture.SetPixel(x, y, c);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
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
                string name = data != null && data.Look != null ? CharacterOptions.CleanName(data.Look.Name) : string.Empty;
                _cardTitles[i].text = name.Length > 0 ? name : $"Partie {i + 1}";
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
                    _cardFooters[i].text = "Suppr encore : effacer";
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
                ? "Haut : revenir aux parties   •   Espace : quitter le jeu"
                : _slots[_selected] != null
                    ? "Gauche / Droite : choisir   •   Espace : jouer   •   Suppr : effacer   •   Bas : quitter"
                    : "Gauche / Droite : choisir   •   Espace : jouer   •   Bas : quitter";
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

            // Bulles qui montent, derrière tout le reste.
            Sprite bubbleSprite = Circle(12);
            for (int i = 0; i < 26; i++)
            {
                Image bubble = UiFactory.Plain(root, "Bulle", Color.white);
                bubble.sprite = bubbleSprite;
                bubble.raycastTarget = false;
                float size = Range(8f, 22f);
                UiFactory.Center(bubble.rectTransform, new Vector2(Range(-940f, 940f), Range(-560f, 560f)), new Vector2(size, size));
                _bubbles.Add(bubble.rectTransform);
                _bubbleSpeed.Add(Range(30f, 80f));
            }

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
            _lastStop = UiFactory.Label(_logo, "Dernière escale", 26, TextAnchor.MiddleCenter, Gold);
            UiFactory.Center(_lastStop.rectTransform, new Vector2(0f, -108f), new Vector2(1200f, 36f));

            // Poissons qui sautent : devant le logo, derrière les emplacements (qui font la surface de l'eau).
            for (int i = 0; i < 4; i++)
            {
                Image fish = UiFactory.Plain(root, "Poisson qui saute", Color.white);
                fish.raycastTarget = false;
                fish.preserveAspect = true;
                fish.gameObject.SetActive(false);
                _jumpers.Add(new Jumper { Image = fish });
            }

            for (int i = 0; i < SaveSlots.Count; i++)
            {
                Image card = UiFactory.Sliced(root, $"Emplacement {i + 1}", _skin != null ? _skin.Panel : null,
                    _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
                RectTransform rect = card.rectTransform;
                UiFactory.Center(rect, new Vector2((i - 1) * 500f, -40f), new Vector2(440f, 330f));
                _cardTitles.Add(UiFactory.TitlePlaque(rect, _skin, $"Partie {i + 1}", 30, new Vector2(0f, 160f), new Vector2(240f, 62f), out _));

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

            _creator = new CharacterCreator(root, _skin);
            _creator.Confirmed = OnCreatorConfirmed;
            _creator.Cancelled = () => SetTitleVisible(true);
        }
    }
}
