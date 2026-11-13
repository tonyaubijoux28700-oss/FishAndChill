using FishAndChill.Collection;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.Fishing
{
    /// <summary>
    /// Carte affichée à chaque prise : dessin du poisson, nom, taille, rareté, nouvelle espèce ou doublon.
    /// Interface provisoire construite par le code (comme le reste du prototype).
    /// </summary>
    public sealed class CatchCard
    {
        private static readonly Color PanelColor = new Color(0.07f, 0.10f, 0.14f, 0.94f);
        private static readonly Color FishBackdrop = new Color(0.20f, 0.55f, 0.65f, 0.55f);
        private static readonly Color MutedText = new Color(0.75f, 0.80f, 0.85f);
        private static readonly Color NewSpeciesColor = new Color(1.00f, 0.85f, 0.30f);

        private const float PopDuration = 0.35f;

        private readonly GameObject _root;
        private readonly RectTransform _panel;
        private readonly Outline _border;
        private readonly Text _title;
        private readonly Image _fish;
        private readonly RectTransform _fishRect;
        private readonly Text _name;
        private readonly Text _scientificName;
        private readonly Text _details;
        private readonly Text _footer;
        private readonly Text _hint;
        private float _age;

        public CatchCard(RectTransform canvas, Font font)
        {
            _root = new GameObject("Carte de prise", typeof(RectTransform));
            var rootRect = _root.GetComponent<RectTransform>();
            rootRect.SetParent(canvas, false);
            Stretch(rootRect);

            Image dim = AddImage(rootRect, "Voile", new Color(0f, 0f, 0f, 0.35f));
            Stretch(dim.rectTransform);

            Image panel = AddImage(rootRect, "Cadre", PanelColor);
            _panel = panel.rectTransform;
            _panel.sizeDelta = new Vector2(720f, 600f);
            _border = panel.gameObject.AddComponent<Outline>();
            _border.effectDistance = new Vector2(6f, -6f);

            _title = AddText(_panel, "Titre", font, 46, FontStyle.Bold, new Vector2(0f, 245f), new Vector2(680f, 70f));

            Image backdrop = AddImage(_panel, "Fond du poisson", FishBackdrop);
            backdrop.rectTransform.anchoredPosition = new Vector2(0f, 75f);
            backdrop.rectTransform.sizeDelta = new Vector2(600f, 250f);

            _fish = AddImage(backdrop.rectTransform, "Poisson", Color.white);
            _fish.preserveAspect = true;
            _fishRect = _fish.rectTransform;
            _fishRect.sizeDelta = new Vector2(540f, 230f);

            _name = AddText(_panel, "Nom", font, 42, FontStyle.Bold, new Vector2(0f, -95f), new Vector2(680f, 56f));
            _scientificName = AddText(_panel, "Nom scientifique", font, 28, FontStyle.Italic, new Vector2(0f, -140f), new Vector2(680f, 40f));
            _scientificName.color = MutedText;
            _details = AddText(_panel, "Détails", font, 30, FontStyle.Normal, new Vector2(0f, -185f), new Vector2(680f, 44f));
            _footer = AddText(_panel, "Pied", font, 30, FontStyle.Bold, new Vector2(0f, -228f), new Vector2(680f, 44f));

            _hint = AddText(_panel, "Aide", font, 22, FontStyle.Normal, new Vector2(0f, -272f), new Vector2(680f, 34f));
            _hint.color = MutedText;

            Hide();
        }

        public bool IsVisible => _root.activeSelf;

        /// <summary>Carte simple (sans sac) : utilisée pour prévisualiser une espèce.</summary>
        public void Show(FishSpecies species, float sizeCm, bool isNew)
        {
            Show(new CaughtFish(species, sizeCm), isNew, false, -1, -1);
        }

        /// <summary>Carte de prise avec l'état du sac (<paramref name="bagCount"/> &lt; 0 : pas de sac).</summary>
        public void Show(CaughtFish fish, bool isNew, bool bagFull, int bagCount, int bagCapacity)
        {
            FishSpecies species = fish.Species;
            float sizeCm = fish.SizeCm;
            Color rarityColor = species.Rarity.DisplayColor();

            _title.text = isNew ? "Nouvelle espèce !" : "Belle prise !";
            _title.color = isNew ? NewSpeciesColor : Color.white;
            _border.effectColor = rarityColor;

            _fish.sprite = species.Sprite;
            _fish.color = species.Sprite != null ? Color.white : species.PlaceholderColor;

            _name.text = species.CommonName;
            _scientificName.text = species.ScientificName;
            _details.text = $"{sizeCm:0} cm   •   <color=#{ColorUtility.ToHtmlStringRGB(rarityColor)}>{species.Rarity.DisplayName()}</color>";
            string bag = bagCount >= 0 ? $" (sac {bagCount}/{bagCapacity})" : string.Empty;
            if (bagFull)
            {
                _footer.text = "Sac plein ! Relâche un poisson pour garder celui-ci";
                _footer.color = NewSpeciesColor;
                _hint.text = "1, 2, 3 : relâcher ce poisson du sac   •   Espace : relâcher le nouveau";
                _hint.color = Color.white;
            }
            else
            {
                _footer.text = isNew
                    ? $"Garde-la pour l'aquarium !{bag}"
                    : $"Doublon : le marchand te l'achètera {FishShop.PriceOf(fish)} pièces{bag}";
                _footer.color = isNew ? NewSpeciesColor : MutedText;
                _hint.text = "Espace pour continuer";
                _hint.color = MutedText;
            }

            _age = 0f;
            _root.SetActive(true);
            Animate(0f);
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

        /// <summary>Petite animation : la carte « saute » à l'apparition, le poisson frétille.</summary>
        public void Animate(float deltaTime)
        {
            if (!IsVisible)
            {
                return;
            }

            _age += deltaTime;
            float t = Mathf.Clamp01(_age / PopDuration);
            float overshoot = 1f + Mathf.Sin(t * Mathf.PI) * 0.12f;
            _panel.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, t) * overshoot;

            _fishRect.anchoredPosition = new Vector2(0f, Mathf.Sin(_age * 3f) * 6f);
            _fishRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_age * 5f) * 3f);
        }

        private static Image AddImage(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text AddText(RectTransform parent, string name, Font font, int size, FontStyle style, Vector2 position, Vector2 boxSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = boxSize;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
