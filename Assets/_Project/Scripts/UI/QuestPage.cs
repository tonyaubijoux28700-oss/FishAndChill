using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Villagers;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Onglet « Quêtes » de l'inventaire (<see cref="InventoryHud"/>) : une carte par habitant (2 par rangée) avec son
    /// portrait, où il est en ce moment, le poisson qu'il demande (petite image, nom, zone où le pêcher), la récompense et
    /// où en est la commande (à écouter, à pêcher, dans le sac, à garder pour l'aquarium, servie aujourd'hui).
    /// </summary>
    public sealed class QuestPage
    {
        private const float CardWidth = 780f;
        private const float CardHeight = 250f;

        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.66f, 0.72f, 0.78f);
        private static readonly Color Good = new Color(0.55f, 0.95f, 0.55f);
        private static readonly Color Warm = new Color(1f, 0.62f, 0.32f);

        private readonly RectTransform _page;
        private readonly UiSkin _skin;
        private readonly Sprite _card;
        private readonly Text _header;
        private readonly Text _empty;
        private readonly List<Card> _cards = new List<Card>();

        private sealed class Card
        {
            public GameObject Root;
            public Image Portrait;
            public Text Name;
            public Text Where;
            public Image Fish;
            public Text FishName;
            public Text Zone;
            public Text Reward;
            public Text Status;
        }

        public QuestPage(RectTransform page, UiSkin skin, Sprite card)
        {
            _page = page;
            _skin = skin;
            _card = card;
            _header = UiFactory.Label(page, "Titre quêtes", 32, TextAnchor.MiddleLeft, Gold);
            UiFactory.Bold(_header);
            UiFactory.Center(_header.rectTransform, new Vector2(-10f, 278f), new Vector2(1600f, 40f));
            _empty = UiFactory.Label(page, "Aucune quête", 26, TextAnchor.MiddleCenter, Muted);
            _empty.text = "Personne n'a rien à te demander pour l'instant.";
            UiFactory.Center(_empty.rectTransform, Vector2.zero, new Vector2(1200f, 40f));
        }

        public void Refresh()
        {
            var villagers = new List<Villager>(Villager.Everyone);
            villagers.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
            int open = 0;
            for (int i = 0; i < villagers.Count; i++)
            {
                while (_cards.Count <= i) _cards.Add(CreateCard(_cards.Count));
                if (Fill(_cards[i], villagers[i])) open++;
            }

            for (int i = villagers.Count; i < _cards.Count; i++) _cards[i].Root.SetActive(false);
            _empty.gameObject.SetActive(villagers.Count == 0);
            _header.text = $"Commandes des habitants   {open} en cours";
        }

        private Card CreateCard(int index)
        {
            int column = index % 2;
            int row = index / 2;
            var position = new Vector2(column == 0 ? -(CardWidth * 0.5f + 12f) : CardWidth * 0.5f + 12f, 115f - row * (CardHeight + 22f));
            var card = new Card();
            Image frame = UiFactory.Sliced(_page, $"Quête {index + 1}", _card != null ? _card : _skin != null ? _skin.Slot : null,
                _skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
            card.Root = frame.gameObject;
            RectTransform rect = frame.rectTransform;
            UiFactory.Center(rect, position, new Vector2(CardWidth, CardHeight));

            Image backdrop = UiFactory.Sliced(rect, "Fond", _skin != null ? _skin.WaterBackdrop : null, _skin != null ? new Color(1f, 1f, 1f, 0.45f) : new Color(0.2f, 0.4f, 0.5f));
            UiFactory.Center(backdrop.rectTransform, new Vector2(-300f, 0f), new Vector2(150f, 210f));
            card.Portrait = UiFactory.Plain(rect, "Portrait", Color.white);
            card.Portrait.preserveAspect = true;
            UiFactory.Center(card.Portrait.rectTransform, new Vector2(-300f, -6f), new Vector2(112f, 168f));

            card.Name = Text(rect, "Nom", 30, Gold, new Vector2(80f, 90f));
            UiFactory.Bold(card.Name);
            card.Where = Text(rect, "Où", 19, Muted, new Vector2(80f, 60f));

            card.Fish = UiFactory.Plain(rect, "Poisson", Color.white);
            card.Fish.preserveAspect = true;
            UiFactory.Center(card.Fish.rectTransform, new Vector2(-145f, -2f), new Vector2(100f, 64f));
            card.FishName = Text(rect, "Poisson demandé", 25, Color.white, new Vector2(150f, 16f), 440f);
            UiFactory.Bold(card.FishName);
            card.Zone = Text(rect, "Zone", 19, Muted, new Vector2(150f, -16f), 440f);
            card.Reward = Text(rect, "Récompense", 19, Gold, new Vector2(150f, -44f), 440f);
            card.Status = Text(rect, "État", 21, Color.white, new Vector2(80f, -88f));
            return card;
        }

        private static Text Text(RectTransform parent, string name, int size, Color color, Vector2 position, float width = 580f)
        {
            Text text = UiFactory.Label(parent, name, size, TextAnchor.MiddleLeft, color);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiFactory.Center(text.rectTransform, position, new Vector2(width, size + 8f));
            return text;
        }

        /// <summary>Remplit la carte ; vrai si une commande est en cours.</summary>
        private static bool Fill(Card card, Villager villager)
        {
            card.Root.SetActive(true);
            card.Portrait.sprite = villager.Portrait;
            card.Portrait.enabled = villager.Portrait != null;
            card.Name.text = villager.DisplayName;
            card.Where.text = villager.IsHome ? "En ce moment : à la maison" : "En ce moment : en balade sur l'île";

            QuestStatus status = villager.Quest;
            FishSpecies species = villager.RequestedSpecies;
            bool known = species != null && status != QuestStatus.NotHeard;
            card.Fish.sprite = known ? species.MiniSprite : null;
            card.Fish.enabled = known && species.MiniSprite != null;
            card.FishName.text = known ? species.CommonName : status == QuestStatus.NotHeard ? "???" : string.Empty;
            string zone = villager.RequestZoneName;
            card.Zone.text = known && !string.IsNullOrEmpty(zone) ? $"Se pêche : {zone}" : string.Empty;
            card.Reward.text = known ? $"Récompense : {RequestRules.Reward(species.BasePrice, species.Rarity)} pièces" : string.Empty;

            switch (status)
            {
                case QuestStatus.NotHeard:
                    card.Status.text = "Nouvelle commande ! Va lui parler pour la découvrir.";
                    card.Status.color = Warm;
                    break;
                case QuestStatus.Ready:
                    card.Status.text = "Tu l'as dans ton sac : va le lui apporter !";
                    card.Status.color = Good;
                    break;
                case QuestStatus.KeepForAquarium:
                    card.Status.text = "Ton seul exemplaire : mets-le d'abord à l'aquarium.";
                    card.Status.color = Warm;
                    break;
                case QuestStatus.Fishing:
                    card.Status.text = "À pêcher.";
                    card.Status.color = Color.white;
                    break;
                case QuestStatus.ServedToday:
                    card.Status.text = "Servi aujourd'hui. Nouvelle commande demain matin.";
                    card.Status.color = Muted;
                    break;
                default:
                    card.Status.text = "Rien à te demander pour l'instant.";
                    card.Status.color = Muted;
                    break;
            }

            return species != null;
        }
    }
}
