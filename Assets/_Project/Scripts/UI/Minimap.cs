using System;
using System.Collections.Generic;
using FishAndChill.World;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Carte de l'île : une petite carte toujours visible en bas à droite, et une grande carte au centre
    /// ouverte / fermée avec M. Les deux partagent la même image (1 pixel = 1 case, construite depuis les
    /// Tilemaps) et un point clignotant qui suit le joueur en direct ; le jeu continue pendant ce temps.
    /// </summary>
    public sealed class Minimap : MonoBehaviour
    {
        [Serializable]
        public sealed class TileColor
        {
            public TileBase Tile;
            public Color Color = Color.white;
        }

        /// <summary>Une vue de la carte : image + point du joueur.</summary>
        private sealed class MapView
        {
            public GameObject Root;
            public RawImage Image;
            public RectTransform Dot;
            public Image DotImage;
            public RectTransform Landmark;
        }

        [SerializeField] private WorldMap _map;
        [SerializeField] private Transform _player;

        [SerializeField, Tooltip("Tilemap de décor (palmiers…) affichée par-dessus la terre.")]
        private Tilemap _decor;

        [SerializeField, Tooltip("Couleur de chaque tuile sur la carte.")]
        private List<TileColor> _palette = new List<TileColor>();

        [SerializeField, Tooltip("Lieu à signaler sur la carte (l'aquarium).")]
        private Transform _landmark;

        [SerializeField] private Color _landmarkColor = new Color(1.00f, 0.60f, 0.15f);

        [SerializeField] private Color _waterColor = new Color(0.20f, 0.62f, 0.72f);
        [SerializeField] private Color _unknownLandColor = new Color(0.80f, 0.75f, 0.55f);
        [SerializeField] private Color _playerColor = new Color(0.95f, 0.20f, 0.20f);

        [SerializeField, Min(1), Tooltip("Grande carte : taille d'une case en pixels de référence (1920×1080).")]
        private int _pixelsPerCell = 6;

        [SerializeField, Min(1), Tooltip("Petite carte (coin) : taille d'une case en pixels de référence.")]
        private int _cornerPixelsPerCell = 3;

        [SerializeField] private string _title = "Île Mahana";

        private MapView _corner;
        private MapView _large;
        private Texture2D _texture;
        private Rect _worldBounds;

        /// <summary>La grande carte est-elle ouverte ?</summary>
        public bool IsOpen => _large != null && _large.Root.activeSelf;

        /// <summary>La petite carte du coin est-elle affichée ?</summary>
        public bool IsCornerVisible => _corner != null && _corner.Root.activeSelf;

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

            UpdateDot(IsOpen ? _large : _corner);
        }

        public void Toggle() => SetOpen(!IsOpen);

        /// <summary>Ouvre la grande carte (la petite se masque) ou revient à la petite carte du coin.</summary>
        public void SetOpen(bool open)
        {
            if (_large == null)
            {
                return;
            }

            _large.Root.SetActive(open);
            _corner.Root.SetActive(!open);
            UpdateDot(open ? _large : _corner);
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

                    TileBase water = _map.Water != null ? _map.Water.GetTile(cell) : null;
                    if (water != null && lookup.TryGetValue(water, out Color waterColor))
                    {
                        color = waterColor;
                    }

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
            var cellCount = new Vector2(cells.size.x, cells.size.y);
            ApplyTexture(_large, cellCount * _pixelsPerCell);
            ApplyTexture(_corner, cellCount * _cornerPixelsPerCell);
        }

        private void ApplyTexture(MapView view, Vector2 size)
        {
            view.Image.texture = _texture;
            view.Image.rectTransform.sizeDelta = size;

            // La petite carte : son cadre s'ajuste à l'image.
            if (view == _corner)
            {
                var frame = (RectTransform)view.Root.transform;
                frame.sizeDelta = size + new Vector2(12f, 44f);
            }
        }

        private void UpdateDot(MapView view)
        {
            if (view == null || !view.Root.activeSelf)
            {
                return;
            }

            Vector2 normalized = PlayerNormalizedPosition;
            Vector2 size = view.Image.rectTransform.sizeDelta;
            view.Dot.anchoredPosition = new Vector2((normalized.x - 0.5f) * size.x, (normalized.y - 0.5f) * size.y);

            view.Landmark.gameObject.SetActive(_landmark != null);
            if (_landmark != null)
            {
                Vector2 mark = MinimapMath.WorldToNormalized(_landmark.position, _worldBounds);
                view.Landmark.anchoredPosition = new Vector2((mark.x - 0.5f) * size.x, (mark.y - 0.5f) * size.y);
            }

            Color color = _playerColor;
            color.a = MinimapMath.BlinkAlpha(Time.unscaledTime);
            view.DotImage.color = color;
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

            _corner = BuildCornerView(root, font);
            _large = BuildLargeView(root, font);
        }

        /// <summary>Petite carte permanente, ancrée en bas à droite de l'écran.</summary>
        private MapView BuildCornerView(RectTransform root, Font font)
        {
            Image frame = AddImage(root, "Petite carte", new Color(0.07f, 0.10f, 0.14f, 1f));
            RectTransform rect = frame.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-24f, 24f);
            var border = frame.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(0.95f, 0.85f, 0.55f, 0.9f);
            border.effectDistance = new Vector2(3f, -3f);

            MapView view = CreateView(frame.gameObject, rect, 14f, new Vector2(0f, 16f));

            Text hint = AddText(rect, "Agrandir", font, 22, TextAnchor.MiddleCenter);
            hint.text = "M : agrandir";
            hint.rectTransform.anchorMin = new Vector2(0f, 0f);
            hint.rectTransform.anchorMax = new Vector2(1f, 0f);
            hint.rectTransform.pivot = new Vector2(0.5f, 0f);
            hint.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            hint.rectTransform.sizeDelta = new Vector2(0f, 28f);
            return view;
        }

        /// <summary>Grande carte au centre, ouverte avec M.</summary>
        private MapView BuildLargeView(RectTransform root, Font font)
        {
            Image frame = AddImage(root, "Grande carte", new Color(0.07f, 0.10f, 0.14f, 1f));
            RectTransform rect = frame.rectTransform;
            rect.anchoredPosition = new Vector2(0f, -45f);
            rect.sizeDelta = new Vector2(640f, 560f);
            var border = frame.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(0.95f, 0.85f, 0.55f);
            border.effectDistance = new Vector2(4f, -4f);

            Text title = AddText(rect, "Titre", font, 38, TextAnchor.MiddleCenter);
            title.text = _title;
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchoredPosition = new Vector2(0f, 248f);
            title.rectTransform.sizeDelta = new Vector2(600f, 50f);

            MapView view = CreateView(frame.gameObject, rect, 20f, new Vector2(0f, -10f));

            Text close = AddText(rect, "Fermer", font, 22, TextAnchor.MiddleCenter);
            close.text = "Point rouge : toi  •  Losange orange : aquarium  •  M ou Échap pour fermer";
            close.color = new Color(0.80f, 0.84f, 0.88f);
            close.rectTransform.anchoredPosition = new Vector2(0f, -256f);
            close.rectTransform.sizeDelta = new Vector2(600f, 34f);
            return view;
        }

        private MapView CreateView(GameObject rootObject, RectTransform parent, float dotSize, Vector2 imageOffset)
        {
            var imageObject = new GameObject("Île", typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<RawImage>();
            image.rectTransform.anchoredPosition = imageOffset;

            Image landmark = AddImage(image.rectTransform, "Aquarium", _landmarkColor);
            landmark.rectTransform.sizeDelta = new Vector2(dotSize * 0.9f, dotSize * 0.9f);
            landmark.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var landmarkBorder = landmark.gameObject.AddComponent<Outline>();
            landmarkBorder.effectColor = new Color(0.25f, 0.12f, 0.02f);
            landmarkBorder.effectDistance = new Vector2(2f, -2f);

            Image dot = AddImage(image.rectTransform, "Toi", _playerColor);
            dot.rectTransform.sizeDelta = new Vector2(dotSize, dotSize);
            var dotBorder = dot.gameObject.AddComponent<Outline>();
            dotBorder.effectColor = Color.white;
            dotBorder.effectDistance = new Vector2(2f, -2f);

            return new MapView { Root = rootObject, Image = image, Dot = dot.rectTransform, DotImage = dot, Landmark = landmark.rectTransform };
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
