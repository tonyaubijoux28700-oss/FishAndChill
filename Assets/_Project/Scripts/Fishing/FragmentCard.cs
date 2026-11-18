using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.Fishing
{
    /// <summary>
    /// Carte du fragment d'artefact, à la place de la carte de prise : bandeau « Fragment d'artefact ! », le fragment qui
    /// flotte et tourne lentement dans un halo, entouré d'étincelles ; à droite, l'artefact en silhouette avec les
    /// fragments déjà trouvés à leur place ; la phrase du fragment sur un papier et le décompte.
    /// </summary>
    public sealed class FragmentCard
    {
        private static readonly Color Ink = new Color(0.24f, 0.16f, 0.10f);
        private static readonly Color Gold = new Color(1.00f, 0.85f, 0.32f);
        private static readonly Color Glow = new Color(0.55f, 1f, 0.95f);

        private readonly GameObject _root;
        private readonly RectTransform _panel;
        private readonly RectTransform _fragment;
        private readonly Image _fragmentImage;
        private readonly Image _halo;
        private readonly Image _silhouette;
        private readonly List<Image> _pieces = new List<Image>();
        private readonly List<RectTransform> _sparkles = new List<RectTransform>();
        private readonly Text _line;
        private readonly Text _count;
        private readonly Text _title;
        private float _age;
        private int _newPiece = -1;

        /// <summary>Taille du cadre de l'artefact entier sur la carte : le fragment y est agrandi puis recentré sur lui-même.</summary>
        private static readonly Vector2 FragmentSize = new Vector2(330f, 440f);

        public FragmentCard(RectTransform canvas, UiSkin skin)
        {
            _root = new GameObject("Fragment d'artefact", typeof(RectTransform));
            var rootRect = _root.GetComponent<RectTransform>();
            rootRect.SetParent(canvas, false);
            UiFactory.Stretch(rootRect);

            Image dim = UiFactory.Plain(rootRect, "Voile", new Color(0f, 0f, 0f, 0.5f));
            UiFactory.Stretch(dim.rectTransform);

            Image panel = UiFactory.Sliced(rootRect, "Cadre", skin != null ? skin.Panel : null, skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            _panel = panel.rectTransform;
            UiFactory.Center(_panel, Vector2.zero, new Vector2(820f, 660f));
            _title = UiFactory.TitlePlaque(_panel, skin, "Fragment d'artefact !", 36, new Vector2(0f, 318f), new Vector2(520f, 72f), out _);

            // Le fragment, sur un fond d'eau, dans un halo turquoise.
            Image water = UiFactory.Sliced(_panel, "Fond d'eau", skin != null ? skin.WaterBackdrop : null, skin != null ? Color.white : new Color(0.2f, 0.55f, 0.65f));
            UiFactory.Center(water.rectTransform, new Vector2(-150f, 100f), new Vector2(420f, 330f));
            _halo = UiFactory.Plain(water.rectTransform, "Halo", new Color(Glow.r, Glow.g, Glow.b, 0.35f));
            _halo.sprite = skin != null ? skin.Glow : null;
            UiFactory.Center(_halo.rectTransform, Vector2.zero, new Vector2(300f, 300f));
            _fragmentImage = UiFactory.Plain(water.rectTransform, "Fragment", Color.white);
            _fragmentImage.preserveAspect = true;
            _fragment = _fragmentImage.rectTransform;
            UiFactory.Center(_fragment, Vector2.zero, FragmentSize);
            for (int i = 0; i < 8; i++)
            {
                Image sparkle = UiFactory.Plain(water.rectTransform, "Étincelle", Gold);
                UiFactory.Center(sparkle.rectTransform, Vector2.zero, new Vector2(10f, 10f));
                _sparkles.Add(sparkle.rectTransform);
            }

            // L'artefact à reconstituer : silhouette, fragments trouvés par-dessus.
            Image slot = UiFactory.Sliced(_panel, "Artefact", skin != null ? skin.Slot : null, skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
            UiFactory.Center(slot.rectTransform, new Vector2(230f, 100f), new Vector2(250f, 330f));
            _silhouette = UiFactory.Plain(slot.rectTransform, "Silhouette", Color.white);
            _silhouette.preserveAspect = true;
            UiFactory.Center(_silhouette.rectTransform, new Vector2(0f, 20f), new Vector2(168f, 224f));
            _count = UiFactory.Label(slot.rectTransform, "Décompte", 30, TextAnchor.MiddleCenter, Gold);
            UiFactory.Bold(_count);
            UiFactory.Place(_count.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(-16f, 40f));

            // La phrase du fragment, sur un papier crème.
            Image paper = UiFactory.Plain(_panel, "Papier", new Color(0.96f, 0.91f, 0.76f));
            UiFactory.Center(paper.rectTransform, new Vector2(0f, -170f), new Vector2(720f, 160f));
            _line = UiFactory.Label(paper.rectTransform, "Phrase", 26, TextAnchor.MiddleCenter, Ink, shadow: false);
            _line.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Center(_line.rectTransform, Vector2.zero, new Vector2(670f, 140f));

            Text hint = UiFactory.Label(_panel, "Aide", 21, TextAnchor.MiddleCenter, new Color(0.70f, 0.76f, 0.82f));
            hint.text = "Espace pour continuer";
            UiFactory.Center(hint.rectTransform, new Vector2(0f, -290f), new Vector2(640f, 30f));
            Hide();
        }

        public bool IsVisible => _root.activeSelf;

        /// <summary>Montre le fragment numéro <paramref name="count"/> (à partir de 1) de l'artefact de cette légende.</summary>
        public void Show(LegendFish legend, int count)
        {
            int index = Mathf.Clamp(count - 1, 0, legend.FragmentCount - 1);
            bool complete = count >= legend.FragmentCount;
            _title.text = complete ? "Dernier fragment !" : "Fragment d'artefact !";
            _fragmentImage.sprite = legend.Fragment(index);
            // Le pivot sur le fragment lui-même : il est centré dans le halo et tourne autour de lui-même.
            _fragment.pivot = legend.FragmentCenter(index);
            _silhouette.sprite = legend.Silhouette;
            while (_pieces.Count < legend.FragmentCount)
            {
                Image piece = UiFactory.Plain(_silhouette.rectTransform, "Fragment trouvé", Color.white);
                piece.preserveAspect = true;
                UiFactory.Stretch(piece.rectTransform);
                _pieces.Add(piece);
            }

            for (int i = 0; i < _pieces.Count; i++)
            {
                _pieces[i].sprite = legend.Fragment(i);
                _pieces[i].gameObject.SetActive(i < count && legend.Fragment(i) != null);
            }

            _newPiece = index;
            _line.text = legend.FragmentLine(index)
                + (complete ? $"\nTu as tous les morceaux : assemble l'artefact sur le socle du laboratoire !" : string.Empty);
            _count.text = $"{Mathf.Min(count, legend.FragmentCount)} / {legend.FragmentCount}";
            _age = 0f;
            _root.SetActive(true);
            Animate(0f);
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

        /// <summary>La carte saute, le fragment flotte et tourne, le halo respire, les étincelles tournent autour ; le nouveau fragment clignote sur l'artefact.</summary>
        public void Animate(float deltaTime)
        {
            if (!IsVisible)
            {
                return;
            }

            _age += deltaTime;
            float t = Mathf.Clamp01(_age / 0.35f);
            _panel.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, t) * (1f + Mathf.Sin(t * Mathf.PI) * 0.1f);
            _fragment.anchoredPosition = new Vector2(0f, Mathf.Sin(_age * 1.6f) * 10f);
            _fragment.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_age * 0.9f) * 12f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(_age * 2.4f);
            _halo.color = new Color(Glow.r, Glow.g, Glow.b, 0.22f + 0.2f * pulse);
            _halo.rectTransform.localScale = Vector3.one * (0.9f + 0.15f * pulse);

            for (int i = 0; i < _sparkles.Count; i++)
            {
                float angle = _age * 0.8f + i * Mathf.PI * 2f / _sparkles.Count;
                float radius = 130f + Mathf.Sin(_age * 2f + i) * 14f;
                _sparkles[i].anchoredPosition = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.8f);
                float twinkle = Mathf.Abs(Mathf.Sin(_age * 3f + i * 1.7f));
                _sparkles[i].localScale = Vector3.one * (0.4f + twinkle);
            }

            if (_newPiece >= 0 && _newPiece < _pieces.Count)
            {
                float blink = _age < 2.4f ? 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(_age * 5f)) : 1f;
                _pieces[_newPiece].color = new Color(1f, 1f, 1f, blink);
            }
        }
    }
}
