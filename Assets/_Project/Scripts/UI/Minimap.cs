using System;
using System.Collections.Generic;
using FishAndChill.World;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Carte de l'île, ouverte / fermée avec M. L'image est construite à partir des Tilemaps
    /// (1 pixel = 1 case) et un point clignotant suit le joueur en direct ; le jeu continue pendant ce temps.
    /// </summary>
    public sealed class Minimap : MonoBehaviour
    {
        [Serializable]
        public sealed class TileColor
        {
            public TileBase Tile;
            public Color Color = Color.white;
        }

        [SerializeField] private WorldMap _map;
        [SerializeField] private Transform _player;

        [SerializeField, Tooltip("Tilemap de décor (palmiers…) affichée par-dessus la terre.")]
        private Tilemap _decor;

        [SerializeField, Tooltip("Couleur de chaque tuile sur la carte.")]
        private List<TileColor> _palette = new List<TileColor>();

        [SerializeField] private Color _waterColor = new Color(0.20f, 0.62f, 0.72f);
        [SerializeField] private Color _unknownLandColor = new Color(0.80f, 0.75f, 0.55f);
        [SerializeField] private Color _playerColor = new Color(0.95f, 0.20f, 0.20f);

        [SerializeField, Min(1), Tooltip("Taille d'une case sur l'écran, en pixels de référence (1920×1080).")]
        private int _pixelsPerCell = 6;

        [SerializeField] private string _title = "Île Mahana";

        private GameObject _panel;
        private RawImage _mapImage;
        private RectTransform _dot;
        private Image _dotImage;
        private Texture2D _texture;
        private Rect _worldBounds;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        private void Start()
        {
            BuildUi();
            Rebuild();
            SetOpen(false);
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                Destroy(_texture);
            }
        }

        private void Update()
        {
            if (MapInput.TogglePressedThisFrame())
            {
                Toggle();
            }
            else if (IsOpen && MapInput.ClosePressedThisFrame())
            {
                SetOpen(false);
            }

            if (IsOpen)
            {
                UpdateDot();
            }
        }

        public void Toggle() => SetOpen(!IsOpen);

        public void SetOpen(bool open)
        {
            if (_panel == null)
            {
                return;
            }

            _panel.SetActive(open);
            if (open)
            {
                UpdateDot();
            }
        }

        /// <summary>Position actuelle du point sur la carte (0..1). Utile pour les tests.</summary>
        public Vector2 PlayerNormalizedPosition =>
            _player != null ? MinimapMath.WorldToNormalized(_player.position, _worldBounds) : new Vector2(0.5f, 0.5f);

        /// <summary>Reconstruit l'image de la carte (à rappeler si l'île est modifiée en jeu).</summary>
        public void Rebuild()
        {
            if (_map == null || _map.Land == null)
            {
                return;
            }

            _worldBounds = _map.WorldBounds;
            Tilemap reference = _map.Water != null ? _map.Water : _map.Land;
            reference.CompressBounds();
            BoundsInt cells = reference.cellBounds;

            var lookup = new Dictionary<TileBase, Color>();
            foreach (TileColor entry in _palette)
            {
                if (entry.Tile != null)
                {
                    lookup[entry.Tile] = entry.Color;
                }
            }

            if (_texture != null)
            {
                Destroy(_texture);
            }

            _texture = new Texture2D(cells.size.x, cells.size.y, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            for (int x = 0; x < cells.size.x; x++)
            {
                for (int y = 0; y < cells.size.y; y++)
                {
                    var cell = new Vector3Int(cells.xMin + x, cells.yMin + y, 0);
                    Color color = _waterColor;

                    TileBase land = _map.Land.GetTile(cell);
                    if (land != null)
                    {
                        color = lookup.TryGetValue(land, out Color landColor) ? landColor : _unknownLandColor;
                    }

                    TileBase decor = _decor != null ? _decor.GetTile(cell) : null;
                    if (decor != null && lookup.TryGetValue(decor, out Color decorColor))
                    {
                        color = decorColor;
                    }

                    _texture.SetPixel(x, y, color);
                }
            }

            _texture.Apply();
            _mapImage.texture = _texture;
            _mapImage.rectTransform.sizeDelta = new Vector2(cells.size.x, cells.size.y) * _pixelsPerCell;
        }

        private void UpdateDot()
        {
            Vector2 normalized = PlayerNormalizedPosition;
            Vector2 size = _mapImage.rectTransform.sizeDelta;
            _dot.anchoredPosition = new Vector2((normalized.x - 0.5f) * size.x, (normalized.y - 0.5f) * size.y);

            Color color = _playerColor;
            color.a = MinimapMath.BlinkAlpha(Time.unscaledTime);
            _dotImage.color = color;
        }

        // ---------- Interface ----------

        private void BuildUi()
        {
            var canvasObject = new GameObject("Carte (minimap)", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Rappel permanent en bas à droite.
            Text hint = AddText(root, "Rappel", font, 26, TextAnchor.LowerRight);
            hint.text = "M : carte";
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(1f, 0f);
            hint.rectTransform.pivot = new Vector2(1f, 0f);
            hint.rectTransform.anchoredPosition = new Vector2(-30f, 24f);
            hint.rectTransform.sizeDelta = new Vector2(300f, 40f);

            _panel = new GameObject("Carte ouverte", typeof(RectTransform));
            var panelRect = _panel.GetComponent<RectTransform>();
            panelRect.SetParent(root, false);
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image frame = AddImage(panelRect, "Cadre", new Color(0.07f, 0.10f, 0.14f, 0.92f));
            frame.rectTransform.anchoredPosition = new Vector2(0f, -45f);
            frame.rectTransform.sizeDelta = new Vector2(640f, 560f);
            var border = frame.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(0.95f, 0.85f, 0.55f);
            border.effectDistance = new Vector2(4f, -4f);

            Text title = AddText(frame.rectTransform, "Titre", font, 38, TextAnchor.MiddleCenter);
            title.text = _title;
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchoredPosition = new Vector2(0f, 248f);
            title.rectTransform.sizeDelta = new Vector2(600f, 50f);

            var imageObject = new GameObject("Île", typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(frame.rectTransform, false);
            _mapImage = imageObject.GetComponent<RawImage>();
            _mapImage.rectTransform.anchoredPosition = new Vector2(0f, -10f);

            Image dot = AddImage(_mapImage.rectTransform, "Toi", _playerColor);
            _dot = dot.rectTransform;
            _dot.sizeDelta = new Vector2(20f, 20f);
            _dotImage = dot;
            var dotBorder = dot.gameObject.AddComponent<Outline>();
            dotBorder.effectColor = Color.white;
            dotBorder.effectDistance = new Vector2(2f, -2f);

            Text close = AddText(frame.rectTransform, "Fermer", font, 22, TextAnchor.MiddleCenter);
            close.text = "Le point rouge, c'est toi  •  M ou Échap pour fermer";
            close.color = new Color(0.80f, 0.84f, 0.88f);
            close.rectTransform.anchoredPosition = new Vector2(0f, -256f);
            close.rectTransform.sizeDelta = new Vector2(600f, 34f);
        }

        private static Image AddImage(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text AddText(RectTransform parent, string name, Font font, int size, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            return text;
        }
    }
}
