using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Sélecteur de matériel de l'inventaire, posé par-dessus la grille des poissons : une carte par canne, hameçon ou
    /// appât (dessin, nom, atouts en pierres ou réserve), celle en main bordée d'or, celles pas encore achetées voilées
    /// (« Chez le marchand »). Au survol, le bandeau du bas dit ce que fait l'objet ; un clic sur un objet à soi le prend
    /// en main. 8 cartes par page, molette ou plaques « Précédent / Suivant » pour les autres.
    /// </summary>
    public sealed class GearPicker
    {
        public sealed class Entry
        {
            public Sprite Icon;
            public string Name;
            public string Line;      // sous le nom : réserve d'appât, prix chez le marchand…
            public string Detail;    // bandeau du bas au survol
            public string[] StatNames;
            public int[] Stats;      // pierres allumées (sur GearStats.Max), ou null
            public bool Owned;
            public bool Equipped;
            public Action Pick;
        }

        private sealed class Card
        {
            public Image Frame;
            public Image Icon;
            public Text Name;
            public Text Line;
            public GameObject Badge;
            public GameObject Veil;
            public readonly List<Text> StatNames = new List<Text>();
            public readonly List<List<Image>> StatPips = new List<List<Image>>();
        }

        private const int Columns = 4;
        private const int PerPage = 8;
        private const float CardWidth = 205f;
        private const float CardHeight = 200f;
        private const float Gap = 12f;
        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.66f, 0.72f, 0.78f);
        private static readonly Color PipOff = new Color(0.32f, 0.36f, 0.42f);

        private readonly RectTransform _root;
        private readonly UiSkin _skin;
        private readonly Text _title;
        private readonly Text _detail;
        private readonly Text _pageText;
        private readonly RectTransform _previous;
        private readonly RectTransform _next;
        private readonly List<Card> _cards = new List<Card>();
        private List<Entry> _entries = new List<Entry>();
        private int _page;
        private int _hover = -1;

        public bool IsOpen => _root.gameObject.activeSelf;

        /// <summary>Ce qui est ouvert (pour rafraîchir après un changement au clavier).</summary>
        public object Subject { get; private set; }

        public GearPicker(RectTransform parent, UiSkin skin, Sprite card, Vector2 center, Vector2 size)
        {
            _skin = skin;
            Image panel = UiFactory.Sliced(parent, "Choix du matériel", skin != null ? skin.Panel : null, skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            _root = panel.rectTransform;
            UiFactory.Center(_root, center, size);

            _title = UiFactory.Label(_root, "Titre", 32, TextAnchor.MiddleLeft, Gold);
            UiFactory.Bold(_title);
            UiFactory.Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(-60f, 40f));

            Text close = UiFactory.Label(_root, "Fermer", 20, TextAnchor.MiddleRight, Muted);
            close.text = "Échap : retour";
            UiFactory.Place(close.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(-60f, 32f));

            float gridWidth = Columns * CardWidth + (Columns - 1) * Gap;
            for (int i = 0; i < PerPage; i++)
            {
                int column = i % Columns;
                int row = i / Columns;
                var position = new Vector2(-gridWidth * 0.5f + CardWidth * 0.5f + column * (CardWidth + Gap), 135f - row * (CardHeight + Gap));
                _cards.Add(BuildCard(card, i, position));
            }

            Image detailBack = UiFactory.Sliced(_root, "Bandeau", skin != null ? skin.Slot : null, skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
            UiFactory.Place(detailBack.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(-50f, 70f));
            _detail = UiFactory.Label(detailBack.rectTransform, "Effet", 23, TextAnchor.MiddleCenter, Color.white);
            _detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Stretch(_detail.rectTransform, 14f);

            _previous = Plaque("Précédent", new Vector2(-300f, -214f));
            _next = Plaque("Suivant", new Vector2(300f, -214f));
            _pageText = UiFactory.Label(_root, "Page", 22, TextAnchor.MiddleCenter, Muted);
            UiFactory.Center(_pageText.rectTransform, new Vector2(0f, -214f), new Vector2(300f, 34f));

            _root.gameObject.SetActive(false);
        }

        private RectTransform Plaque(string text, Vector2 position)
        {
            UiFactory.TitlePlaque(_root, _skin, text, 22, position, new Vector2(170f, 38f), out RectTransform plaque);
            return plaque;
        }

        private Card BuildCard(Sprite sprite, int index, Vector2 position)
        {
            var card = new Card();
            card.Frame = UiFactory.Sliced(_root, $"Carte {index + 1}", sprite != null ? sprite : _skin != null ? _skin.Slot : null, _skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
            RectTransform rect = card.Frame.rectTransform;
            UiFactory.Center(rect, position, new Vector2(CardWidth, CardHeight));

            card.Icon = UiFactory.Plain(rect, "Dessin", Color.white);
            card.Icon.preserveAspect = true;
            UiFactory.Center(card.Icon.rectTransform, new Vector2(-24f, 58f), new Vector2(64f, 64f));

            card.Name = UiFactory.Label(rect, "Nom", 22, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Bold(card.Name);
            card.Name.horizontalOverflow = HorizontalWrapMode.Wrap;
            card.Name.verticalOverflow = VerticalWrapMode.Truncate; // une seule ligne : le nom rétrécit au lieu de passer à la ligne
            card.Name.resizeTextForBestFit = true;
            card.Name.resizeTextMaxSize = card.Name.fontSize;
            card.Name.resizeTextMinSize = UiFactory.Size(14);
            UiFactory.Center(card.Name.rectTransform, new Vector2(0f, 16f), new Vector2(CardWidth - 16f, 30f));

            card.Line = UiFactory.Label(rect, "Ligne", 19, TextAnchor.MiddleCenter, Muted);
            card.Line.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Center(card.Line.rectTransform, new Vector2(0f, -32f), new Vector2(CardWidth - 16f, 50f));

            for (int s = 0; s < 4; s++)
            {
                float y = -6f - s * 20f;
                Text name = UiFactory.Label(rect, "Atout", 16, TextAnchor.MiddleLeft, Muted);
                UiFactory.Center(name.rectTransform, new Vector2(-50f, y), new Vector2(84f, 20f));
                card.StatNames.Add(name);
                var pips = new List<Image>();
                for (int k = 0; k < Gear.GearStats.Max; k++)
                {
                    Image pip = UiFactory.Plain(rect, "Pierre", Color.white);
                    pip.sprite = _skin != null ? _skin.Gem : null;
                    UiFactory.Center(pip.rectTransform, new Vector2(8f + k * 16f, y), new Vector2(14f, 14f));
                    pips.Add(pip);
                }

                card.StatPips.Add(pips);
            }

            Text badge = UiFactory.TitlePlaque(rect, _skin, "En main", 18, Vector2.zero, new Vector2(92f, 30f), out RectTransform badgePlaque);
            UiFactory.Place(badgePlaque, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(92f, 30f));
            badge.fontSize = UiFactory.Size(18);
            card.Badge = badgePlaque.gameObject;

            Image veil = UiFactory.Sliced(rect, "Voile", sprite, new Color(0.18f, 0.2f, 0.26f, 0.62f));
            UiFactory.Stretch(veil.rectTransform, 5f);
            veil.raycastTarget = false;
            card.Veil = veil.gameObject;
            return card;
        }

        public void Open(object subject, string title, List<Entry> entries)
        {
            bool same = IsOpen && Equals(subject, Subject);
            Subject = subject;
            _title.text = title;
            _entries = entries ?? new List<Entry>();
            if (!same)
            {
                int equipped = _entries.FindIndex(e => e.Equipped);
                _page = equipped >= 0 ? equipped / PerPage : 0;
            }

            _page = Mathf.Clamp(_page, 0, PageCount - 1);
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            Fill();
        }

        public void Close()
        {
            _root.gameObject.SetActive(false);
            Subject = null;
        }

        private int PageCount => Mathf.Max(1, (_entries.Count + PerPage - 1) / PerPage);

        private void Fill()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                Card card = _cards[i];
                int index = _page * PerPage + i;
                bool shown = index < _entries.Count;
                card.Frame.gameObject.SetActive(shown);
                if (!shown) continue;

                Entry entry = _entries[index];
                card.Icon.sprite = entry.Icon;
                card.Icon.enabled = entry.Icon != null;
                card.Name.text = entry.Name;
                card.Name.color = entry.Equipped ? Gold : Color.white;
                card.Line.text = entry.Stats != null ? string.Empty : entry.Line;
                card.Badge.SetActive(entry.Equipped);
                card.Veil.SetActive(!entry.Owned);
                card.Frame.color = entry.Equipped ? Gold : Color.white;
                for (int s = 0; s < card.StatNames.Count; s++)
                {
                    bool has = entry.Stats != null && s < entry.Stats.Length;
                    card.StatNames[s].gameObject.SetActive(has);
                    card.StatNames[s].text = has && entry.StatNames != null && s < entry.StatNames.Length ? entry.StatNames[s] : string.Empty;
                    for (int k = 0; k < card.StatPips[s].Count; k++)
                    {
                        card.StatPips[s][k].gameObject.SetActive(has);
                        if (has) card.StatPips[s][k].color = k < entry.Stats[s] ? Gold : PipOff;
                    }
                }

                // Pas encore à soi : le prix chez le marchand prend la place des atouts sous le nom.
                if (!entry.Owned && entry.Stats != null) card.Line.text = string.Empty;
            }

            bool pages = PageCount > 1;
            _previous.gameObject.SetActive(pages && _page > 0);
            _next.gameObject.SetActive(pages && _page < PageCount - 1);
            _pageText.text = pages ? $"Page {_page + 1} / {PageCount}" : string.Empty;
            ShowDetail();
        }

        private void ShowDetail()
        {
            int index = _hover >= 0 ? _page * PerPage + _hover : _entries.FindIndex(e => e.Equipped);
            if (index < 0 || index >= _entries.Count)
            {
                _detail.text = "Clique sur un objet pour le prendre en main.";
                return;
            }

            Entry entry = _entries[index];
            _detail.text = entry.Owned ? $"<color=#FFD54D>{entry.Name}</color> : {entry.Detail}" : $"<color=#FFD54D>{entry.Name}</color> : {entry.Detail}  ({entry.Line})";
        }

        private static bool Over(RectTransform rect, Vector2 screen) =>
            rect != null && rect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null);

        /// <summary>La souris est-elle sur le sélecteur ?</summary>
        public bool Contains(Vector2 screen) => IsOpen && Over(_root, screen);

        /// <summary>Survol, clic et molette ; renvoie vrai si un objet vient d'être pris en main.</summary>
        public bool HandleMouse(Vector2 screen, bool click, float scroll)
        {
            int hover = -1;
            for (int i = 0; i < _cards.Count; i++)
            {
                bool over = Over(_cards[i].Frame.rectTransform, screen);
                _cards[i].Frame.rectTransform.localScale = Vector3.one * (over ? 1.05f : 1f);
                if (over) hover = i;
            }

            if (hover != _hover)
            {
                _hover = hover;
                ShowDetail();
            }

            int turn = scroll < 0f || (click && Over(_next, screen)) ? 1 : scroll > 0f || (click && Over(_previous, screen)) ? -1 : 0;
            if (turn != 0 && PageCount > 1)
            {
                _page = Mathf.Clamp(_page + turn, 0, PageCount - 1);
                Fill();
                return false;
            }

            if (!click || _hover < 0) return false;
            Entry entry = _entries[_page * PerPage + _hover];
            if (!entry.Owned) return false;
            entry.Pick?.Invoke();
            return true;
        }
    }
}
