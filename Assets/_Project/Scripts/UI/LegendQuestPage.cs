using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Rubrique « Légendes » du journal de quêtes : une carte par légende (3 par rangée) avec la silhouette de son
    /// artefact, son nom (ou « Relique inconnue »), sa zone, où on en est (fragments trouvés et posés, artefact complet,
    /// gardien réveillé, vaincu) et ce qu'il reste à faire.
    /// </summary>
    public sealed class LegendQuestPage
    {
        private const float CardWidth = 500f;
        private const float CardHeight = 170f;

        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.66f, 0.72f, 0.78f);
        private static readonly Color Text = new Color(0.96f, 0.94f, 0.88f);
        private static readonly Color Warm = new Color(1f, 0.62f, 0.32f);

        private readonly RectTransform _page;
        private readonly Sprite _card;
        private readonly PlayerProgress _progress;
        private readonly FishingController _fishing;
        private readonly Text _header;
        private readonly Text _empty;
        private readonly List<Card> _cards = new List<Card>();

        private sealed class Card
        {
            public GameObject Root;
            public Image Art;
            public Text Name;
            public Text Zone;
            public Text Status;
            public Text Hint;
        }

        public LegendQuestPage(RectTransform page, Sprite card, PlayerProgress progress, FishingController fishing)
        {
            _page = page;
            _card = card;
            _progress = progress;
            _fishing = fishing;
            _header = UiFactory.Label(page, "Titre légendes", 32, TextAnchor.MiddleLeft, Gold);
            UiFactory.Bold(_header);
            UiFactory.Center(_header.rectTransform, new Vector2(-10f, 278f), new Vector2(1600f, 40f));
            _empty = UiFactory.Label(page, "Aucune légende", 26, TextAnchor.MiddleCenter, Muted);
            _empty.text = "Aucune légende connue pour l'instant.";
            UiFactory.Center(_empty.rectTransform, Vector2.zero, new Vector2(1200f, 40f));
        }

        private Card CreateCard(int index)
        {
            int column = index % 3, row = index / 3;
            var card = new Card();
            Image back = UiFactory.Sliced(_page, $"Légende {index + 1}", _card, _card != null ? Color.white : new Color(0.10f, 0.16f, 0.24f));
            card.Root = back.gameObject;
            UiFactory.Center(back.rectTransform, new Vector2((column - 1) * (CardWidth + 20f), 180f - row * (CardHeight + 16f)), new Vector2(CardWidth, CardHeight));
            card.Art = UiFactory.Plain(back.rectTransform, "Artefact", Color.white);
            card.Art.preserveAspect = true;
            UiFactory.Center(card.Art.rectTransform, new Vector2(-180f, 0f), new Vector2(100f, 130f));
            card.Name = Label(back.rectTransform, "Nom", 26, Text, 52f);
            UiFactory.Bold(card.Name);
            card.Zone = Label(back.rectTransform, "Zone", 20, Muted, 22f);
            card.Status = Label(back.rectTransform, "Étape", 22, Gold, -8f);
            card.Hint = Label(back.rectTransform, "Suite", 18, Muted, -42f);
            return card;
        }

        private static Text Label(RectTransform parent, string name, int size, Color color, float y)
        {
            Text text = UiFactory.Label(parent, name, size, TextAnchor.MiddleLeft, color);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Center(text.rectTransform, new Vector2(60f, y), new Vector2(350f, 34f));
            return text;
        }

        public void Refresh()
        {
            if (_fishing == null || _progress == null) return;
            var legends = new List<LegendFish>();
            foreach (LegendFish legend in _fishing.Legends)
            {
                if (legend != null) legends.Add(legend);
            }

            _empty.gameObject.SetActive(legends.Count == 0);
            int beaten = 0;
            for (int i = 0; i < legends.Count; i++)
            {
                while (_cards.Count <= i) _cards.Add(CreateCard(_cards.Count));
                Card card = _cards[i];
                card.Root.SetActive(true);
                if (Fill(card, legends[i])) beaten++;
            }

            for (int i = legends.Count; i < _cards.Count; i++) _cards[i].Root.SetActive(false);
            _header.text = $"Légendes   {beaten} / {legends.Count} vaincue{(beaten > 1 ? "s" : string.Empty)}";
        }

        /// <summary>Remplit la carte d'une légende ; vrai si elle est vaincue.</summary>
        private bool Fill(Card card, LegendFish legend)
        {
            LegendProgress progress = _progress.Legends;
            LegendState state = progress.State(legend.Id);
            int total = legend.FragmentCount;
            int found = state == LegendState.Collecting ? progress.Fragments(legend.Id) : total;
            int placed = state == LegendState.Collecting ? progress.Placed(legend.Id) : total;
            bool known = found > 0;
            card.Art.sprite = legend.Silhouette;
            card.Art.color = state == LegendState.Beaten ? new Color(1f, 0.84f, 0.4f) : known ? new Color(0.78f, 0.82f, 0.88f) : new Color(0.46f, 0.53f, 0.64f);
            card.Name.text = known ? legend.ArtifactName : "Relique inconnue";
            card.Zone.text = $"Du côté de « {(legend.Zone != null ? legend.Zone.DisplayName : "?")} »";
            switch (state)
            {
                case LegendState.Beaten:
                    card.Status.text = $"{legend.Title} vaincu";
                    card.Status.color = Gold;
                    card.Hint.text = $"{legend.SizeCm:0} cm à ton palmarès.";
                    return true;
                case LegendState.Summoned:
                    card.Status.text = $"{legend.Title} est réveillé !";
                    card.Status.color = Warm;
                    card.Hint.text = "Il t'attend au remous légendaire : le point doré sur ta carte.";
                    return false;
                case LegendState.Complete:
                    card.Status.text = "Artefact complet";
                    card.Status.color = Gold;
                    card.Hint.text = "Réveille le gardien depuis le socle du laboratoire.";
                    return false;
                default:
                    card.Status.text = known ? $"{found} / {total} fragments, {placed} posé{(placed > 1 ? "s" : string.Empty)}" : "Aucun fragment trouvé";
                    card.Status.color = known ? Text : Muted;
                    card.Hint.text = !known ? "Ses fragments se pêchent dans cette zone."
                        : found > placed ? "Pose tes fragments sur le socle du laboratoire."
                        : "Continue de pêcher : il en manque encore.";
                    return false;
            }
        }
    }
}
