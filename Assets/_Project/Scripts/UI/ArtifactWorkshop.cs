using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// L'atelier des artefacts, ouvert devant le socle du laboratoire : à gauche la liste des artefacts (un par poisson
    /// de légende, « ??? » tant qu'aucun fragment n'est trouvé) ; au centre l'artefact choisi, grisé, que l'on remplit
    /// morceau par morceau ; en bas les fragments en poche. Espace pose le prochain : il s'envole du plateau jusqu'à sa
    /// place (arc, rotation, rebond), puis éclair, secousse et étincelles. Le dernier posé illumine l'artefact entier.
    /// Haut / Bas changent d'artefact, Échap ou E ferme.
    /// </summary>
    public sealed class ArtifactWorkshop : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.66f, 0.72f, 0.78f);
        private static readonly Color Glow = new Color(0.55f, 1f, 0.95f);

        /// <summary>Taille de l'artefact entier au centre (même proportions que les dessins, 48×64).</summary>
        private static readonly Vector2 FrameSize = new Vector2(330f, 440f);
        private static readonly Vector2 FramePosition = new Vector2(110f, 95f);

        /// <summary>Échelle d'un fragment sur le plateau, par rapport à sa taille sur l'artefact.</summary>
        private const float TrayScale = 0.3f;
        private const float TrayY = -335f;
        private const float TraySpacing = 140f;
        private const float FlightSeconds = 0.75f;

        private UiSkin _skin;
        private PlayerProgress _progress;
        private IReadOnlyList<LegendFish> _legends;
        private PlayerMovement _player;

        private GameObject _window;
        private RectTransform _panel;
        private RectTransform _frame;
        private Image _silhouette;
        private Image _sketch;
        private Image _halo;
        private Image _flash;
        private Text _artifactName;
        private Text _status;
        private Text _trayTitle;
        private Text _help;
        private readonly List<Image> _pieces = new List<Image>();
        private readonly List<RowView> _rows = new List<RowView>();
        private readonly List<SlotView> _slots = new List<SlotView>();
        private readonly List<Spark> _sparks = new List<Spark>();

        private int _selected;
        private int _openedFrame = -1;
        private float _age;

        // Fragment en vol vers sa place.
        private Image _flying;
        private int _flyingIndex = -1;
        private float _flightTime;
        private Vector2 _flightFrom;
        private Vector2 _flightTo;
        private float _shake;
        private float _completeAge = -1f;

        // Réveil du gardien : l'artefact tremble de plus en plus, le halo enfle, puis un éclair blanc remplit l'écran.
        private const float SummonSeconds = 2.4f;
        private const float SummonFlashAt = 1.6f;
        private float _summonAge = -1f;
        private Image _whiteout;
        private Text _banner;
        private float _bannerAge = -1f;

        /// <summary>La légende vient d'être réveillée depuis l'atelier.</summary>
        public event System.Action<LegendFish> Summoned;

        private sealed class RowView
        {
            public Image Frame;
            public Image Icon;
            public Text Name;
            public Text Detail;
        }

        private sealed class SlotView
        {
            public Image Frame;
            public Image Piece;
        }

        private sealed class Spark
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Velocity;
            public float Life;
        }

        public bool IsOpen => _window != null && _window.activeSelf;

        /// <summary>Image où l'atelier a été fermé (l'appui sur E qui ferme ne doit rien rouvrir).</summary>
        public int ClosedFrame { get; private set; } = -1;

        /// <summary>Le décor de l'atelier (dessins de WorkshopArt) ; sans lui, la charte d'interface sert de fond.</summary>
        [System.Serializable]
        public sealed class Look
        {
            public Sprite Bench;
            public Sprite Slab;
            public Sprite Compartment;
            public Sprite Card;
        }

        private static readonly Color CardInk = new Color(0.26f, 0.17f, 0.10f);
        private static readonly Color CardSoft = new Color(0.46f, 0.34f, 0.24f);
        private Look _look = new Look();

        public void Init(UiSkin skin, PlayerProgress progress, IReadOnlyList<LegendFish> legends, PlayerMovement player, Look look = null)
        {
            _look = look ?? new Look();
            _skin = skin;
            _progress = progress;
            _legends = legends ?? new LegendFish[0];
            _player = player;
            BuildUi();
            _window.SetActive(false);
        }

        public void Open()
        {
            if (_window == null || _progress == null)
            {
                return;
            }

            _progress.EnsureCreated();
            _openedFrame = Time.frameCount;
            _selected = Mathf.Clamp(FirstStarted(), 0, Mathf.Max(0, _legends.Count - 1));
            _flyingIndex = -1;
            _completeAge = -1f;
            _summonAge = -1f;
            _bannerAge = -1f;
            _banner.color = Color.clear;
            _whiteout.color = Color.clear;
            if (_flying != null) _flying.gameObject.SetActive(false);
            if (_player != null) _player.Locked = true;
            _window.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            FinishFlight();
            _window.SetActive(false);
            ClosedFrame = Time.frameCount;
            if (_player != null) _player.Locked = false;
        }

        /// <summary>Pose le prochain fragment en poche de l'artefact choisi (animation). Public pour les essais par script.</summary>
        public bool PlaceNext()
        {
            LegendFish legend = Selected;
            if (legend == null || _flyingIndex >= 0)
            {
                return false;
            }

            Vector2 from = SlotPosition(0);
            int index = _progress.Legends.PlaceFragment(legend.Id, legend.FragmentCount);
            if (index < 0)
            {
                return false;
            }

            // Le fragment quitte le plateau et s'envole vers sa place sur l'artefact.
            _flyingIndex = index;
            _flightTime = 0f;
            Vector2 center = legend.FragmentCenter(index);
            _flying.sprite = legend.Fragment(index);
            _flying.rectTransform.pivot = center;
            _flying.rectTransform.sizeDelta = FrameSize;
            _flightFrom = from;
            _flightTo = FramePosition + new Vector2((center.x - 0.5f) * FrameSize.x, (center.y - 0.5f) * FrameSize.y);
            _flying.gameObject.SetActive(true);
            Refresh();
            return true;
        }

        /// <summary>
        /// Réveille le gardien de l'artefact choisi (s'il est complet et pas encore réveillé) : animation, puis la légende
        /// est invoquée dans sa zone. Public pour les essais par script.
        /// </summary>
        public bool Summon()
        {
            LegendFish legend = Selected;
            if (legend == null || _summonAge >= 0f || _flyingIndex >= 0 || _progress.Legends.State(legend.Id) != LegendState.Complete)
            {
                return false;
            }

            _summonAge = 0f;
            _help.text = string.Empty;
            _status.text = "L'artefact s'éveille…";
            return true;
        }

        private void AnimateSummon(float dt)
        {
            if (_summonAge >= 0f)
            {
                float before = _summonAge;
                _summonAge += dt;
                float build = Mathf.Clamp01(_summonAge / SummonFlashAt);
                _shake = Mathf.Max(_shake, 0.05f + 0.25f * build);
                if (Random.value < dt * (6f + 30f * build))
                {
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    Burst(FramePosition + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 60f, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(200f, 380f), Random.value < 0.5f ? Gold : Glow);
                }

                if (before < SummonFlashAt && _summonAge >= SummonFlashAt)
                {
                    LegendFish legend = Selected;
                    if (legend != null && _progress.Legends.Summon(legend.Id))
                    {
                        string zone = legend.Zone != null ? legend.Zone.DisplayName : "sa zone";
                        _banner.text = $"{legend.Title} s'est réveillé !";
                        _bannerAge = 0f;
                        Summoned?.Invoke(legend);
                        Refresh();
                        _status.text = $"Il t'attend au remous de « {zone} ».";
                    }
                }

                if (_summonAge >= SummonSeconds)
                {
                    _summonAge = -1f;
                    Refresh();
                }
            }

            // Éclair blanc qui remplit l'écran au moment du réveil, puis s'efface.
            float white = _summonAge < 0f ? 0f
                : _summonAge < SummonFlashAt ? Mathf.Pow(Mathf.Clamp01((_summonAge - SummonFlashAt + 0.35f) / 0.35f), 2f)
                : Mathf.Clamp01(1f - (_summonAge - SummonFlashAt) / 0.8f);
            _whiteout.color = new Color(1f, 0.98f, 0.92f, white);

            if (_bannerAge >= 0f)
            {
                _bannerAge += dt;
                float appear = Mathf.Clamp01(_bannerAge / 0.3f);
                float fade = Mathf.Clamp01((4f - _bannerAge) / 0.6f);
                _banner.color = new Color(Gold.r, Gold.g, Gold.b, appear * fade);
                _banner.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, appear);
                if (_bannerAge > 4f) _bannerAge = -1f;
            }
        }

        private LegendFish Selected => _legends.Count > 0 ? _legends[Mathf.Clamp(_selected, 0, _legends.Count - 1)] : null;

        private int FirstStarted()
        {
            for (int i = 0; i < _legends.Count; i++)
            {
                if (_legends[i] != null && _progress.Legends.InHand(_legends[i].Id) > 0) return i;
            }

            return 0;
        }

        private bool Discovered(LegendFish legend) => legend != null
            && (_progress.Legends.Fragments(legend.Id) > 0 || _progress.Legends.State(legend.Id) != LegendState.Collecting);

        private int PlacedOf(LegendFish legend) => _progress.Legends.State(legend.Id) == LegendState.Collecting
            ? _progress.Legends.Placed(legend.Id) : legend.FragmentCount;

        private static Vector2 SlotPosition(int slot) => new Vector2(-150f + slot * TraySpacing, TrayY);

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            _age += dt;
            AnimateFlight(dt);
            AnimateSummon(dt);
            AnimateEffects(dt);

            // L'appui qui a ouvert l'atelier ne doit pas déjà le refermer.
            if (Time.frameCount == _openedFrame)
            {
                return;
            }

            if (_summonAge < 0f && (PauseInput.TogglePressedThisFrame() || PauseInput.BackPressedThisFrame() || InteractInput.PressedThisFrame()))
            {
                Close();
                return;
            }

            if (_summonAge >= 0f)
            {
                return;   // pendant le réveil, on regarde
            }

            if (PauseInput.ConfirmPressedThisFrame() && _flyingIndex < 0 && !PlaceNext())
            {
                Summon();
            }

            int move = VerticalPressed();
            if (move != 0 && _flyingIndex < 0 && _legends.Count > 1)
            {
                _selected = (_selected + move + _legends.Count) % _legends.Count;
                _completeAge = -1f;
                Refresh();
            }
        }

        private static int VerticalPressed()
        {
            Keyboard k = Keyboard.current;
            Gamepad g = Gamepad.current;
            bool up = (k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)) || (g != null && g.dpad.up.wasPressedThisFrame);
            bool down = (k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)) || (g != null && g.dpad.down.wasPressedThisFrame);
            return up ? -1 : down ? 1 : 0;
        }

        // ---------- Animations ----------

        private void AnimateFlight(float dt)
        {
            if (_flyingIndex < 0)
            {
                return;
            }

            _flightTime += dt;
            float t = Mathf.Clamp01(_flightTime / FlightSeconds);
            float ease = 1f - (1f - t) * (1f - t) * (1f - t);
            Vector2 p = Vector2.Lerp(_flightFrom, _flightTo, ease) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 110f;   // arc
            RectTransform r = _flying.rectTransform;
            r.anchoredPosition = p;
            r.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(35f, 0f, ease) + Mathf.Sin(t * 18f) * 6f * (1f - t));
            float bounce = t > 0.82f ? 1f + Mathf.Sin((t - 0.82f) / 0.18f * Mathf.PI) * 0.1f : 1f;
            r.localScale = Vector3.one * Mathf.Lerp(TrayScale, 1f, ease) * bounce;
            if (t >= 1f)
            {
                Land();
            }
        }

        /// <summary>Le fragment s'emboîte : il rejoint l'artefact, éclair, secousse, gerbe d'étincelles.</summary>
        private void Land()
        {
            LegendFish legend = Selected;
            int index = _flyingIndex;
            FinishFlight();
            _shake = 0.28f;
            _flash.color = new Color(1f, 1f, 1f, 0.7f);
            Vector2 at = _flightTo;
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                Burst(at, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(160f, 320f), i % 3 == 0 ? Glow : Gold);
            }

            if (legend != null && _progress.Legends.State(legend.Id) != LegendState.Collecting && index == legend.FragmentCount - 1)
            {
                // Le dernier morceau : l'artefact entier s'illumine.
                _completeAge = 0f;
                for (int i = 0; i < 26; i++)
                {
                    float a = i / 26f * Mathf.PI * 2f;
                    Burst(FramePosition, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(220f, 420f), i % 2 == 0 ? Gold : Glow);
                }
            }

            Refresh();
        }

        private void FinishFlight()
        {
            _flyingIndex = -1;
            if (_flying != null)
            {
                _flying.gameObject.SetActive(false);
            }
        }

        private void Burst(Vector2 at, Vector2 velocity, Color color)
        {
            Spark spark = _sparks.Find(s => s.Life <= 0f);
            if (spark == null)
            {
                Image image = UiFactory.Plain(_panel, "Étincelle", color);
                image.raycastTarget = false;
                spark = new Spark { Rect = image.rectTransform, Image = image };
                UiFactory.Center(spark.Rect, Vector2.zero, new Vector2(10f, 10f));
                _sparks.Add(spark);
            }

            spark.Rect.anchoredPosition = at;
            spark.Image.color = color;
            spark.Velocity = velocity;
            spark.Life = Random.Range(0.45f, 0.8f);
            spark.Rect.gameObject.SetActive(true);
            spark.Rect.SetAsLastSibling();
        }

        private void AnimateEffects(float dt)
        {
            // Secousse du cadre, éclair qui s'éteint.
            _shake = Mathf.Max(0f, _shake - dt);
            float shake = _shake > 0f ? Mathf.Sin(_age * 80f) * 9f * (_shake / 0.28f) : 0f;
            _frame.anchoredPosition = FramePosition + new Vector2(Mathf.Round(shake / 3f) * 3f, 0f);
            Color flash = _flash.color;
            flash.a = Mathf.Max(0f, flash.a - dt * 2.6f);
            _flash.color = flash;

            foreach (Spark spark in _sparks)
            {
                if (spark.Life <= 0f)
                {
                    continue;
                }

                spark.Life -= dt;
                spark.Velocity *= 1f - dt * 2.2f;
                spark.Velocity += Vector2.down * 260f * dt;
                spark.Rect.anchoredPosition += spark.Velocity * dt;
                spark.Rect.localScale = Vector3.one * Mathf.Clamp01(spark.Life / 0.4f) * 1.2f;
                if (spark.Life <= 0f) spark.Rect.gameObject.SetActive(false);
            }

            // Halo : turquoise discret pendant l'assemblage, or éclatant puis qui respire une fois l'artefact complet.
            LegendFish legend = Selected;
            bool complete = legend != null && PlacedOf(legend) >= legend.FragmentCount;
            float pulse = 0.5f + 0.5f * Mathf.Sin(_age * 2.2f);
            if (complete)
            {
                float burst = _completeAge >= 0f ? Mathf.Clamp01(1f - _completeAge / 1.2f) : 0f;
                if (_completeAge >= 0f) _completeAge += dt;
                _halo.color = new Color(Gold.r, Gold.g, Gold.b, 0.35f + 0.2f * pulse + 0.4f * burst);
                _halo.rectTransform.localScale = Vector3.one * (1f + 0.08f * pulse + 0.5f * burst);
            }
            else
            {
                _halo.color = new Color(Glow.r, Glow.g, Glow.b, 0.10f + 0.08f * pulse);
                _halo.rectTransform.localScale = Vector3.one;
            }

            // Les fragments du plateau flottent doucement.
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Piece.gameObject.activeSelf)
                {
                    _slots[i].Piece.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Sin(_age * 2f + i) * 4f);
                }
            }
        }

        // ---------- Affichage ----------

        private void Refresh()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                RowView row = _rows[i];
                LegendFish legend = i < _legends.Count ? _legends[i] : null;
                bool known = Discovered(legend);
                row.Frame.color = i == _selected ? (_look.Card != null ? Color.white : new Color(1f, 0.92f, 0.62f))
                    : (_look.Card != null ? new Color(0.78f, 0.74f, 0.68f) : Color.white);
                row.Icon.sprite = legend == null ? null : known && PlacedOf(legend) >= legend.FragmentCount ? legend.Artifact : legend.Silhouette;
                row.Icon.color = known ? Color.white : new Color(1f, 1f, 1f, 0.5f);
                row.Name.text = known ? legend.ArtifactName : "???";
                string zone = legend != null && legend.Zone != null ? legend.Zone.DisplayName : string.Empty;
                LegendState rowState = legend != null ? _progress.Legends.State(legend.Id) : LegendState.Collecting;
                row.Detail.text = !known ? zone
                    : rowState == LegendState.Summoned ? "Gardien réveillé"
                    : rowState == LegendState.Beaten ? "Légende vaincue"
                    : $"{PlacedOf(legend)} / {legend.FragmentCount} posés";
            }

            LegendFish selected = Selected;
            if (selected == null)
            {
                return;
            }

            bool discovered = Discovered(selected);
            int placed = PlacedOf(selected);
            int found = _progress.Legends.State(selected.Id) == LegendState.Collecting ? _progress.Legends.Fragments(selected.Id) : selected.FragmentCount;
            int inHand = Mathf.Max(0, found - placed);
            bool complete = placed >= selected.FragmentCount;

            _artifactName.text = discovered ? selected.ArtifactName : "Artefact inconnu";
            _silhouette.sprite = selected.Silhouette;
            if (_sketch != null)
            {
                _sketch.sprite = discovered ? selected.Silhouette : null;
                _sketch.enabled = _sketch.sprite != null;
            }

            while (_pieces.Count < selected.FragmentCount)
            {
                Image piece = UiFactory.Plain(_silhouette.rectTransform, $"Morceau {_pieces.Count + 1}", Color.white);
                piece.raycastTarget = false;
                UiFactory.Stretch(piece.rectTransform);
                _pieces.Add(piece);
            }

            for (int i = 0; i < _pieces.Count; i++)
            {
                // Le fragment en vol n'apparaît à sa place qu'une fois posé.
                bool shown = i < selected.FragmentCount && i < placed && i != _flyingIndex;
                _pieces[i].sprite = selected.Fragment(i);
                _pieces[i].gameObject.SetActive(shown && selected.Fragment(i) != null);
            }

            string zoneName = selected.Zone != null ? selected.Zone.DisplayName : "sa zone";
            _status.color = complete ? Gold : Color.white;
            LegendState state = _progress.Legends.State(selected.Id);
            _status.text = state == LegendState.Summoned ? $"{selected.Title} est réveillé ! Il t'attend au remous de « {zoneName} »."
                : state == LegendState.Beaten ? $"{selected.Title} a été vaincu. L'artefact repose, apaisé."
                : complete ? "Artefact complet ! Espace : réveiller le gardien"
                : !discovered ? $"Aucun fragment trouvé. Ils se pêchent du côté de « {zoneName} »."
                : inHand > 0 ? $"{placed} / {selected.FragmentCount} posés — Espace : poser le fragment suivant"
                : $"{placed} / {selected.FragmentCount} posés — pêche {(selected.FragmentCount - found == 1 ? "le dernier" : $"les {selected.FragmentCount - found} autres")} du côté de « {zoneName} »";

            _trayTitle.text = complete ? "Tous les fragments sont posés" : inHand > 0 ? $"Fragments en poche : {inHand}" : "Aucun fragment en poche";
            for (int i = 0; i < _slots.Count; i++)
            {
                SlotView slot = _slots[i];
                int index = placed + i;   // le plateau garde l'ordre : le prochain à poser est à gauche
                bool filled = i < inHand && index < selected.FragmentCount;
                // Une case par fragment qui reste à poser (vide tant qu'il n'est pas pêché).
                slot.Frame.gameObject.SetActive(i < selected.FragmentCount - placed);
                slot.Frame.color = filled && i == 0 ? new Color(1f, 0.92f, 0.62f) : Color.white;
                slot.Piece.gameObject.SetActive(filled && selected.Fragment(index) != null);
                if (filled)
                {
                    slot.Piece.sprite = selected.Fragment(index);
                    slot.Piece.rectTransform.pivot = selected.FragmentCenter(index);
                }
            }

            _help.text = (_legends.Count > 1 ? "Haut / Bas : artefact   —   " : string.Empty)
                + (inHand > 0 && !complete ? "Espace : poser   —   " : string.Empty)
                + (state == LegendState.Complete ? "Espace : réveiller le gardien   —   " : string.Empty) + "E ou Échap : fermer";
        }

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Atelier des artefacts", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();
            _window = canvasObject;

            Image dim = UiFactory.Plain(root, "Voile", new Color(0f, 0f, 0f, 0.55f));
            UiFactory.Stretch(dim.rectTransform);

            Image panel = UiFactory.Sliced(root, "Fenêtre", _skin != null ? _skin.Panel : null, _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            _panel = panel.rectTransform;
            UiFactory.Center(_panel, Vector2.zero, new Vector2(1500f, 900f));

            // L'établi du laboratoire, dans le cadre doré : bois sombre, tapis de feutre, carnet, loupe, bocal.
            if (_look.Bench != null)
            {
                Image bench = UiFactory.Plain(_panel, "Établi", Color.white);
                bench.sprite = _look.Bench;
                bench.raycastTarget = false;
                UiFactory.Stretch(bench.rectTransform, 14f);

                // Sur la page de gauche du carnet, le croquis à l'encre de l'artefact choisi.
                _sketch = UiFactory.Plain(_panel, "Croquis du carnet", new Color(0.50f, 0.30f, 0.15f, 1f));
                _sketch.raycastTarget = false;
                _sketch.preserveAspect = true;
                UiFactory.Center(_sketch.rectTransform, new Vector2(531f, 215f), new Vector2(92f, 122f));
            }

            UiFactory.TitlePlaque(_panel, _skin, "Atelier des artefacts", 36, new Vector2(0f, 450f), new Vector2(560f, 72f), out _);
            bool cards = _look.Card != null;

            // Liste des artefacts, à gauche.
            Text listTitle = UiFactory.Label(_panel, "Titre liste", 26, TextAnchor.MiddleLeft, Gold);
            listTitle.text = "Légendes";
            UiFactory.Center(listTitle.rectTransform, new Vector2(-560f, 360f), new Vector2(320f, 36f));
            for (int i = 0; i < Mathf.Max(1, _legends.Count); i++)
            {
                // Une fiche de papier épinglée sur l'établi (ou une case de la charte, sans décor).
                Image frame = cards
                    ? UiFactory.Plain(_panel, $"Légende {i + 1}", Color.white)
                    : UiFactory.Sliced(_panel, $"Légende {i + 1}", _skin != null ? _skin.Slot : null, _skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
                if (cards) frame.sprite = _look.Card;
                // Jusqu'à 5 fiches à pleine taille ; au-delà, elles rapetissent pour tenir dans la colonne.
                float step = Mathf.Min(128f, 640f / Mathf.Max(1, _legends.Count));
                UiFactory.Center(frame.rectTransform, new Vector2(-560f, 270f - i * step - (128f - step) * 0.5f), new Vector2(332f, 116f));
                frame.rectTransform.localScale = Vector3.one * (step / 128f);
                Image icon = UiFactory.Plain(frame.rectTransform, "Icône", Color.white);
                icon.preserveAspect = true;
                UiFactory.Center(icon.rectTransform, new Vector2(-112f, -4f), new Vector2(66f, 88f));
                Text name = UiFactory.Label(frame.rectTransform, "Nom", 24, TextAnchor.MiddleLeft, cards ? CardInk : Color.white, shadow: !cards);
                UiFactory.Center(name.rectTransform, new Vector2(40f, 18f), new Vector2(230f, 34f));
                Text detail = UiFactory.Label(frame.rectTransform, "Détail", 19, TextAnchor.MiddleLeft, cards ? CardSoft : Muted, shadow: !cards);
                UiFactory.Center(detail.rectTransform, new Vector2(40f, -18f), new Vector2(230f, 30f));
                _rows.Add(new RowView { Frame = frame, Icon = icon, Name = name, Detail = detail });
            }

            // L'artefact au centre : fond de pierre, halo, silhouette grisée, morceaux posés, éclair.
            // La dalle de pierre gravée sur laquelle on reconstitue l'artefact.
            Image stone = _look.Slab != null
                ? UiFactory.Plain(_panel, "Dalle de pierre", Color.white)
                : UiFactory.Sliced(_panel, "Fond de l'artefact", _skin != null ? _skin.Slot : null, _skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
            if (_look.Slab != null) stone.sprite = _look.Slab;
            UiFactory.Center(stone.rectTransform, FramePosition, new Vector2(560f, 540f));
            _artifactName = UiFactory.Label(_panel, "Nom de l'artefact", 32, TextAnchor.MiddleCenter, Gold);
            UiFactory.Bold(_artifactName);
            UiFactory.Center(_artifactName.rectTransform, FramePosition + new Vector2(0f, 300f), new Vector2(560f, 44f));

            _halo = UiFactory.Plain(_panel, "Halo", new Color(Glow.r, Glow.g, Glow.b, 0.15f));
            _halo.sprite = _skin != null ? _skin.Glow : null;
            _halo.raycastTarget = false;
            UiFactory.Center(_halo.rectTransform, FramePosition, new Vector2(520f, 560f));

            _silhouette = UiFactory.Plain(_panel, "Artefact grisé", Color.white);
            _silhouette.raycastTarget = false;
            _frame = _silhouette.rectTransform;
            UiFactory.Center(_frame, FramePosition, FrameSize);

            _flash = UiFactory.Plain(stone.rectTransform, "Éclair", new Color(1f, 1f, 1f, 0f));
            _flash.raycastTarget = false;
            UiFactory.Stretch(_flash.rectTransform, 10f);

            _status = UiFactory.Label(_panel, "État", 25, TextAnchor.MiddleCenter, Color.white);
            _status.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Center(_status.rectTransform, new Vector2(110f, -205f), new Vector2(900f, 40f));
            // Sous la dalle : un texte trop long rapetisse au lieu de déborder sur le cadre.
            _status.resizeTextForBestFit = true;
            _status.resizeTextMaxSize = _status.fontSize;
            _status.resizeTextMinSize = Mathf.Max(10, _status.fontSize * 2 / 3);

            // Plateau des fragments en poche, en bas.
            _trayTitle = UiFactory.Label(_panel, "Titre plateau", 22, TextAnchor.MiddleLeft, Muted);
            UiFactory.Center(_trayTitle.rectTransform, new Vector2(-560f, TrayY + 4f), new Vector2(330f, 34f));
            for (int i = 0; i < 6; i++)
            {
                // Une case de velours dans le plateau de bois.
                Image frame = _look.Compartment != null
                    ? UiFactory.Plain(_panel, $"Plateau {i + 1}", Color.white)
                    : UiFactory.Sliced(_panel, $"Plateau {i + 1}", _skin != null ? _skin.Slot : null, _skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
                if (_look.Compartment != null) frame.sprite = _look.Compartment;
                UiFactory.Center(frame.rectTransform, SlotPosition(i), new Vector2(120f, 120f));
                Image piece = UiFactory.Plain(frame.rectTransform, "Fragment", Color.white);
                piece.raycastTarget = false;
                UiFactory.Center(piece.rectTransform, Vector2.zero, FrameSize);
                piece.rectTransform.localScale = Vector3.one * TrayScale;
                _slots.Add(new SlotView { Frame = frame, Piece = piece });
            }

            _flying = UiFactory.Plain(_panel, "Fragment en vol", Color.white);
            _flying.raycastTarget = false;
            UiFactory.Center(_flying.rectTransform, Vector2.zero, FrameSize);
            _flying.gameObject.SetActive(false);

            _help = UiFactory.Label(_panel, "Aide", 22, TextAnchor.MiddleCenter, Muted);
            UiFactory.Center(_help.rectTransform, new Vector2(0f, -420f), new Vector2(1300f, 30f));

            _whiteout = UiFactory.Plain(root, "Éclair du réveil", new Color(1f, 1f, 1f, 0f));
            _whiteout.raycastTarget = false;
            UiFactory.Stretch(_whiteout.rectTransform);

            _banner = UiFactory.Label(root, "Bandeau du réveil", 64, TextAnchor.MiddleCenter, new Color(Gold.r, Gold.g, Gold.b, 0f));
            UiFactory.Bold(_banner);
            UiFactory.Center(_banner.rectTransform, new Vector2(0f, 300f), new Vector2(1600f, 90f));
        }
    }
}
