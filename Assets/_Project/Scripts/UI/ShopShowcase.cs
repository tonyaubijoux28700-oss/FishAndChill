using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// La vitrine du marchand : à gauche, la scène éclairée avec l'objet en grand sur son présentoir (coussin de velours,
    /// ou râtelier pour une canne) et une flèche de chaque côté ; à droite, la fiche de l'objet (nom, effet, état, prix,
    /// ce que fait Espace) ; dessous, une vignette par objet du rayon, celle de l'objet montré en doré.
    /// Les coordonnées de la scène sont celles du dessin du fond agrandi ×3 (720×405, origine en bas à gauche).
    /// </summary>
    public sealed class ShopShowcase
    {
        private const float Zoom = 3f;
        private const float StageWidth = 240f * Zoom;
        private const float StageHeight = 135f * Zoom;
        private const float Floor = 40f;               // pied des présentoirs dans la scène
        private const float RodButt = 0.14f;            // où se trouve le talon de la canne dans son dessin (part de la largeur)
        private const int MaxThumbnails = 8;

        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.62f, 0.68f, 0.74f);

        private readonly RectTransform _root;
        private readonly RectTransform _stage;
        private readonly Image _pedestal;
        private readonly Image _rodStand;
        private readonly Image _item;
        private readonly RectTransform _previous;
        private readonly RectTransform _next;
        private readonly Text _name;
        private readonly Text _detail;
        private readonly Text _status;
        private readonly Text _price;
        private readonly Image _pricePlaque;
        private readonly Text _action;
        private readonly RectTransform _actionPlaque;
        private readonly Text _position;
        private readonly List<Image> _thumbFrames = new List<Image>();
        private readonly List<Image> _thumbPictures = new List<Image>();
        private readonly Vector2 _pedestalSize;
        private readonly Vector2 _rodStandSize;

        private float _itemBottom;
        private int _shownFirst;
        private int _shownSelected;
        private int _shownCount;

        public ShopShowcase(RectTransform panel, UiSkin skin, Sprite back, Sprite pedestal, Sprite rodStand, Sprite arrow)
        {
            var rootObject = new GameObject("Vitrine", typeof(RectTransform));
            _root = rootObject.GetComponent<RectTransform>();
            _root.SetParent(panel, false);
            UiFactory.Center(_root, Vector2.zero, new Vector2(1440f, 640f));

            // La scène, dans un cadre de la charte.
            Image frame = UiFactory.Sliced(_root, "Cadre de la vitrine", skin != null ? skin.Slot : null, skin != null ? Color.white : new Color(0.14f, 0.24f, 0.30f));
            UiFactory.Center(frame.rectTransform, new Vector2(-330f, 55f), new Vector2(StageWidth + 28f, StageHeight + 28f));
            Image stage = UiFactory.Plain(frame.rectTransform, "Scène", back != null ? Color.white : new Color(0.18f, 0.27f, 0.20f));
            stage.sprite = back;
            _stage = stage.rectTransform;
            UiFactory.Center(_stage, Vector2.zero, new Vector2(StageWidth, StageHeight));

            _pedestalSize = Size(pedestal, new Vector2(72f, 60f)) * Zoom;
            _rodStandSize = Size(rodStand, new Vector2(72f, 40f)) * Zoom;
            _pedestal = Place(AddImage(_stage, "Socle", pedestal), new Vector2(StageWidth * 0.5f - _pedestalSize.x * 0.5f, Floor), _pedestalSize);
            _rodStand = Place(AddImage(_stage, "Râtelier", rodStand), new Vector2(StageWidth * 0.5f - _rodStandSize.x * 0.5f, Floor), _rodStandSize);
            _item = AddImage(_stage, "Objet", null);

            Vector2 arrowSize = Size(arrow, new Vector2(22f, 30f)) * Zoom;
            _next = Place(AddImage(_stage, "Suivant", arrow), new Vector2(StageWidth - 30f - arrowSize.x, 170f), arrowSize).rectTransform;
            _previous = Place(AddImage(_stage, "Précédent", arrow), new Vector2(30f, 170f), arrowSize).rectTransform;
            _previous.localScale = new Vector3(-1f, 1f, 1f); // la même flèche, retournée

            // La fiche, à droite.
            _name = UiFactory.TitlePlaque(_root, skin, string.Empty, 36, new Vector2(380f, 250f), new Vector2(600f, 84f), out _);
            _detail = UiFactory.Label(_root, "Effet", 27, TextAnchor.UpperLeft, Color.white);
            _detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Center(_detail.rectTransform, new Vector2(380f, 110f), new Vector2(580f, 170f));
            _status = UiFactory.Label(_root, "État", 28, TextAnchor.MiddleLeft, Color.white);
            UiFactory.Bold(_status);
            UiFactory.Center(_status.rectTransform, new Vector2(380f, -10f), new Vector2(580f, 50f));
            _price = UiFactory.TitlePlaque(_root, skin, string.Empty, 34, new Vector2(380f, -90f), new Vector2(320f, 72f), out RectTransform pricePlaque);
            _pricePlaque = pricePlaque.GetComponent<Image>();
            _action = UiFactory.TitlePlaque(_root, skin, string.Empty, 28, new Vector2(380f, -180f), new Vector2(440f, 64f), out _actionPlaque);

            // Les vignettes du rayon, sous la scène.
            _position = UiFactory.Label(_root, "Position", 24, TextAnchor.MiddleCenter, Muted);
            UiFactory.Center(_position.rectTransform, new Vector2(-330f, -185f), new Vector2(400f, 34f));
            for (int i = 0; i < MaxThumbnails; i++)
            {
                Image thumb = UiFactory.Sliced(_root, $"Vignette {i + 1}", skin != null ? skin.Slot : null, skin != null ? Color.white : new Color(0.14f, 0.24f, 0.30f));
                Image picture = UiFactory.Plain(thumb.rectTransform, "Image", Color.white);
                picture.preserveAspect = true;
                UiFactory.Stretch(picture.rectTransform, 10f);
                _thumbFrames.Add(thumb);
                _thumbPictures.Add(picture);
            }
        }

        public bool Visible
        {
            get => _root.gameObject.activeSelf;
            set => _root.gameObject.SetActive(value);
        }

        /// <summary>Montre l'objet <paramref name="selected"/> du rayon.</summary>
        public void Show(IReadOnlyList<ShowcaseEntry> entries, int selected)
        {
            int count = entries != null ? entries.Count : 0;
            ShowcaseEntry entry = count > 0 ? entries[Mathf.Clamp(selected, 0, count - 1)] : null;
            _item.gameObject.SetActive(entry != null && entry.Picture != null);
            _previous.gameObject.SetActive(count > 1);
            _next.gameObject.SetActive(count > 1);
            _pedestal.gameObject.SetActive(entry == null || !entry.Tall);
            _rodStand.gameObject.SetActive(entry != null && entry.Tall);
            if (entry != null && entry.Picture != null)
            {
                _item.sprite = entry.Picture;
                Vector2 size = entry.Picture.rect.size;
                if (entry.Tall)
                {
                    // Debout dans le râtelier : le talon dans la bague, la canne aussi haute que la scène le permet.
                    float scale = (StageHeight - Floor - _rodStandSize.y - 10f) / size.y;
                    size *= scale;
                    _itemBottom = Floor + _rodStandSize.y - 34f;
                    Place(_item, new Vector2(StageWidth * 0.5f - size.x * RodButt, _itemBottom), size);
                }
                else
                {
                    // Sur le coussin, agrandi d'un nombre entier de fois pour garder des pixels nets.
                    size *= ShowcaseMath.FitScale(size.x, size.y, 200f, 170f);
                    _itemBottom = Floor + _pedestalSize.y - 34f;
                    Place(_item, new Vector2(StageWidth * 0.5f - size.x * 0.5f, _itemBottom), size);
                }
            }

            _name.text = entry != null ? entry.Name : "Rien à vendre ici";
            _detail.text = entry != null ? entry.Detail : string.Empty;
            _status.text = entry != null ? entry.Status : string.Empty;
            _status.color = entry != null ? entry.StatusColor : Muted;
            _pricePlaque.gameObject.SetActive(entry != null && !string.IsNullOrEmpty(entry.Price));
            _price.text = entry != null ? entry.Price : string.Empty;
            _price.color = entry != null ? entry.PriceColor : Muted;
            bool canAct = entry != null && !string.IsNullOrEmpty(entry.Action);
            _actionPlaque.gameObject.SetActive(canAct);
            _action.text = canAct ? $"Clic ou Espace : {entry.Action}" : string.Empty;
            _position.text = count > 0 ? $"{Mathf.Clamp(selected, 0, count - 1) + 1} / {count}" : string.Empty;

            int shown = Mathf.Min(count, MaxThumbnails);
            int first = ShowcaseMath.WindowStart(selected, count, MaxThumbnails);
            _shownFirst = first;
            _shownSelected = selected - first;
            _shownCount = shown;
            const float step = 92f;
            for (int i = 0; i < MaxThumbnails; i++)
            {
                bool visible = i < shown;
                _thumbFrames[i].gameObject.SetActive(visible);
                if (!visible) continue;
                bool current = first + i == selected;
                float size = current ? 92f : 76f;
                UiFactory.Center(_thumbFrames[i].rectTransform, new Vector2(-330f + (i - (shown - 1) * 0.5f) * step, -250f), new Vector2(size, size));
                _thumbFrames[i].color = current ? Gold : new Color(1f, 1f, 1f, 0.75f);
                _thumbPictures[i].sprite = entries[first + i].Picture;
                _thumbPictures[i].color = current ? Color.white : new Color(1f, 1f, 1f, 0.7f);
            }
        }

        /// <summary>L'objet posé flotte doucement au-dessus de son présentoir (au pixel près).</summary>
        public void Animate(float time)
        {
            if (!_item.gameObject.activeSelf) return;
            Vector2 at = _item.rectTransform.anchoredPosition;
            at.y = _itemBottom + Mathf.Round(Mathf.Sin(time * 2.2f) * 1.5f) * Zoom;
            _item.rectTransform.anchoredPosition = at;
        }

        /// <summary>
        /// Survol de la souris (à appeler à chaque image) : flèche, vignette et bandeau d'achat sous le pointeur
        /// passent en lumière dorée.
        /// </summary>
        public void Hover(Vector2 screen, bool hasMouse)
        {
            int arrow = hasMouse ? ArrowAt(screen) : 0;
            int thumb = hasMouse ? ThumbnailAt(screen) : -1;
            bool action = hasMouse && ActionAt(screen);
            _previous.GetComponent<Image>().color = arrow == -1 ? Gold : Color.white;
            _next.GetComponent<Image>().color = arrow == 1 ? Gold : Color.white;
            _actionPlaque.GetComponent<Image>().color = action ? Color.Lerp(Color.white, Gold, 0.55f) : Color.white;
            for (int i = 0; i < _shownCount; i++)
            {
                bool current = i == _shownSelected;
                bool hovered = _shownFirst + i == thumb;
                _thumbFrames[i].color = current ? Gold : hovered ? Color.white : new Color(1f, 1f, 1f, 0.75f);
                _thumbPictures[i].color = current || hovered ? Color.white : new Color(1f, 1f, 1f, 0.7f);
            }
        }

        /// <summary>-1 sur la flèche de gauche, +1 sur celle de droite, 0 ailleurs (clic de souris).</summary>
        public int ArrowAt(Vector2 screen)
        {
            if (_previous.gameObject.activeInHierarchy && Over(_previous, screen)) return -1;
            if (_next.gameObject.activeInHierarchy && Over(_next, screen)) return 1;
            return 0;
        }

        /// <summary>Numéro de la vignette sous la souris (numéro de l'objet), ou -1.</summary>
        public int ThumbnailAt(Vector2 screen)
        {
            for (int i = 0; i < _thumbFrames.Count; i++)
            {
                if (_thumbFrames[i].gameObject.activeInHierarchy && Over(_thumbFrames[i].rectTransform, screen)) return _shownFirst + i;
            }

            return -1;
        }

        /// <summary>La souris est-elle sur le bandeau « Espace : … » (ou sur l'objet lui-même) ?</summary>
        public bool ActionAt(Vector2 screen)
        {
            return (_actionPlaque.gameObject.activeInHierarchy && Over(_actionPlaque, screen))
                || (_item.gameObject.activeInHierarchy && Over(_item.rectTransform, screen));
        }

        private static bool Over(RectTransform rect, Vector2 screen) => RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null);

        private static Vector2 Size(Sprite sprite, Vector2 fallback) => sprite != null ? sprite.rect.size : fallback;

        private static Image AddImage(RectTransform parent, string name, Sprite sprite)
        {
            Image image = UiFactory.Plain(parent, name, Color.white);
            image.sprite = sprite;
            return image;
        }

        /// <summary>Pose une image dans la scène (origine en bas à gauche, en pixels de la scène).</summary>
        private static Image Place(Image image, Vector2 bottomLeft, Vector2 size)
        {
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(bottomLeft.x + size.x * 0.5f, bottomLeft.y);
            return image;
        }
    }
}
