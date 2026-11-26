using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Player;
using FishAndChill.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FishAndChill.World
{
    /// <summary>
    /// Les décorations d'un bassin de l'aquarium : des emplacements fixes au fond, sur trois rangs (le fond, plus sombre
    /// et derrière les poissons ; le milieu ; le devant, devant les poissons). Devant le bassin, R ouvre le mode
    /// décoration : Gauche / Droite choisissent la case, Haut / Bas le rang, E fait défiler les décorations qu'on a
    /// encore en réserve (puis la case vide), Échap ou R terminent. Les poses sont gardées dans
    /// <see cref="DecorationCollection"/> (et la sauvegarde).
    /// </summary>
    public sealed class TankDecorations : MonoBehaviour
    {
        public const int Columns = 6;
        public const int Rows = 3;

        [SerializeField, Tooltip("Nom du bassin dans la sauvegarde.")] private string _tankKey = "hall";
        [SerializeField] private AquariumBuilding _building;
        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private PlayerMovement _player;
        [SerializeField] private UiSkin _skin;
        [SerializeField, Tooltip("Eau du bassin (relative à l'intérieur), comme pour ses poissons.")] private Rect _area = new Rect(2.4f, 12.4f, 25.2f, 4.4f);
        [SerializeField] private Color _water = new Color(0.10f, 0.30f, 0.44f);

        private readonly SpriteRenderer[] _slots = new SpriteRenderer[Columns * Rows];
        private SpriteRenderer _cursor;
        private int _column;
        private int _row = 1;
        private Text _prompt;
        private GameObject _panel;
        private Text _slotLabel;
        private Text _help;

        /// <summary>Le mode décoration est ouvert (le joueur est immobilisé, E sert à changer de décoration).</summary>
        public static bool Busy { get; private set; }

        public bool Editing { get; private set; }

        private Rect WorldArea => _building != null ? _building.WorldRect(_area) : _area;

        /// <summary>Le pied d'une case, dans le monde. Rang 0 au fond, 2 devant.</summary>
        public Vector2 SlotPosition(int column, int row)
        {
            Rect a = WorldArea;
            float shift = row == 1 ? 0.5f : row == 0 ? 0.15f : 0.3f;
            float width = a.width / (Columns + 0.5f);
            float x = a.xMin + (column + 0.5f + shift * 0.5f) * width;
            float y = a.yMin + (row == 0 ? 0.75f : row == 1 ? 0.4f : 0.08f);
            return new Vector2(x, y);
        }

        private bool PlayerAtThisTank
        {
            get
            {
                if (_building == null || _player == null || !_building.IsPlayerInside || !_building.PlayerAtTank) return false;
                Rect a = WorldArea;
                float x = _player.transform.position.x;
                return x >= a.xMin && x <= a.xMax;
            }
        }

        private void Start()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                int row = i / Columns;
                var go = new GameObject($"Décoration du bassin {i + 1}");
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = row == 2 ? -3 : -5;
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                renderer.color = AquariumMath.PlaneTint(row, _water);
                _slots[i] = renderer;
            }

            var cursorObject = new GameObject("Case choisie");
            cursorObject.transform.SetParent(transform, false);
            _cursor = cursorObject.AddComponent<SpriteRenderer>();
            _cursor.sprite = FrameSprite();
            _cursor.sortingOrder = -1;
            cursorObject.SetActive(false);
            BuildUi();
            if (_progress != null) _progress.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_progress != null) _progress.Changed -= Refresh;
            if (Editing) Busy = false;
        }

        /// <summary>Remet chaque case à jour d'après les poses sauvegardées.</summary>
        public void Refresh()
        {
            if (_progress == null || _progress.Decorations == null) return;
            DecorationCatalog catalog = _progress.DecorationCatalog;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null) continue;
                Decoration d = catalog != null ? catalog.Find(_progress.Decorations.PlacedAt(_tankKey, i)) : null;
                _slots[i].sprite = d != null ? d.Sprite : null;
                _slots[i].transform.position = SlotPosition(i % Columns, i / Columns);
            }
        }

        private void Update()
        {
            if (_progress == null || _player == null) return;
            Keyboard k = Keyboard.current;
            Gamepad g = Gamepad.current;
            bool toggle = (k != null && k.rKey.wasPressedThisFrame) || (g != null && g.buttonNorth.wasPressedThisFrame);
            if (!Editing)
            {
                bool here = PlayerAtThisTank && !_player.Locked;
                _prompt.gameObject.SetActive(here);
                if (here && toggle) Open();
                return;
            }

            if (toggle || (k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.buttonEast.wasPressedThisFrame))
            {
                Close();
                return;
            }

            if (Pressed(k, g, Key.LeftArrow, Key.Q, g?.dpad.left)) _column = (_column + Columns - 1) % Columns;
            if (Pressed(k, g, Key.RightArrow, Key.D, g?.dpad.right)) _column = (_column + 1) % Columns;
            if (Pressed(k, g, Key.UpArrow, Key.Z, g?.dpad.up)) _row = Mathf.Max(0, _row - 1);
            if (Pressed(k, g, Key.DownArrow, Key.S, g?.dpad.down)) _row = Mathf.Min(Rows - 1, _row + 1);
            if (InteractInput.PressedThisFrame()) Cycle();
            ShowCursor();
        }

        private static bool Pressed(Keyboard k, Gamepad g, Key a, Key b, UnityEngine.InputSystem.Controls.ButtonControl pad)
        {
            return (k != null && (k[a].wasPressedThisFrame || k[b].wasPressedThisFrame)) || (pad != null && pad.wasPressedThisFrame);
        }

        /// <summary>Ouvre le mode décoration (public pour les tests).</summary>
        public void Open()
        {
            Editing = true;
            Busy = true;
            _player.Locked = true;
            _prompt.gameObject.SetActive(false);
            _panel.SetActive(true);
            _cursor.gameObject.SetActive(true);
            ShowCursor();
        }

        public void Close()
        {
            Editing = false;
            Busy = false;
            _player.Locked = false;
            _panel.SetActive(false);
            _cursor.gameObject.SetActive(false);
        }

        /// <summary>Passe à la décoration suivante qu'on a en réserve pour la case choisie, puis à la case vide.</summary>
        public void Cycle()
        {
            int slot = _row * Columns + _column;
            DecorationCatalog catalog = _progress.DecorationCatalog;
            if (catalog == null) return;
            var choices = new List<string> { null };
            foreach (Decoration d in catalog.All)
            {
                if (d != null && _progress.Decorations.Owned(d.Id) > 0) choices.Add(d.Id);
            }

            string current = _progress.Decorations.PlacedAt(_tankKey, slot);
            int start = Mathf.Max(0, choices.IndexOf(current));
            for (int step = 1; step <= choices.Count; step++)
            {
                string next = choices[(start + step) % choices.Count];
                if (next == null || next == current || _progress.Decorations.Available(next) > 0)
                {
                    _progress.Decorations.Place(_tankKey, slot, next);
                    break;
                }
            }

            Refresh();
            ShowCursor();
        }

        private void ShowCursor()
        {
            int slot = _row * Columns + _column;
            _cursor.transform.position = SlotPosition(_column, _row) + new Vector2(0f, 0.5f);
            DecorationCatalog catalog = _progress.DecorationCatalog;
            Decoration d = catalog != null ? catalog.Find(_progress.Decorations.PlacedAt(_tankKey, slot)) : null;
            string rowName = _row == 0 ? "au fond" : _row == 1 ? "au milieu" : "devant";
            int owned = 0;
            if (catalog != null) foreach (Decoration c in catalog.All) if (c != null) owned += _progress.Decorations.Owned(c.Id);
            _slotLabel.text = d != null
                ? $"Case {_column + 1}, {rowName} : {d.DisplayName} ({_progress.Decorations.Available(d.Id)} en réserve)"
                : owned == 0 ? $"Case {_column + 1}, {rowName} : vide. Plonge pour trouver des décorations !" : $"Case {_column + 1}, {rowName} : vide";
        }

        private static Sprite FrameSprite()
        {
            const int w = 32, h = 32;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color[w * h];
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    bool corner = (x < 6 || x >= w - 6) && (y < 6 || y >= h - 6);
                    bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                    pixels[y * w + x] = edge && corner ? new Color(1f, 0.86f, 0.30f, 1f) : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16);
        }

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Interface des décorations", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 14;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();
            Color gold = new Color(1f, 0.84f, 0.30f);

            _prompt = UiFactory.Label(root, "Invitation", 28, TextAnchor.MiddleCenter, gold);
            UiFactory.Place(_prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(800f, 40f));
            _prompt.text = "R : décorer le bassin";
            _prompt.gameObject.SetActive(false);

            Image panel = UiFactory.Sliced(root, "Décorer le bassin", _skin != null ? _skin.Panel : null, _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1240f, 150f));
            _panel = panel.gameObject;
            _slotLabel = UiFactory.Label(panel.rectTransform, "Case", 30, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Center(_slotLabel.rectTransform, new Vector2(0f, 22f), new Vector2(1180f, 44f));
            _help = UiFactory.Label(panel.rectTransform, "Aide", 22, TextAnchor.MiddleCenter, new Color(0.70f, 0.76f, 0.82f));
            UiFactory.Center(_help.rectTransform, new Vector2(0f, -28f), new Vector2(1180f, 36f));
            _help.text = "Gauche / Droite : case  —  Haut / Bas : rang  —  E : changer de décoration  —  Échap ou R : terminer";
            _panel.SetActive(false);
        }
    }
}
