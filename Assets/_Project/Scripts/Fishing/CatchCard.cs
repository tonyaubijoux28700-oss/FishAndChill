using FishAndChill.Collection;
using FishAndChill.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.Fishing
{
    /// <summary>
    /// Carte affichée à chaque prise, habillée avec la charte pixel art : halo de la couleur de la rareté,
    /// bandeau doré, poisson sur fond d'eau, pastilles taille / rareté / valeur, consigne.
    /// </summary>
    public sealed class CatchCard
    {
        private static readonly Color Muted = new Color(0.70f, 0.76f, 0.82f);
        private static readonly Color Gold = new Color(1.00f, 0.85f, 0.32f);

        private const float PopDuration = 0.35f;
        private static readonly Vector2 PanelSize = new Vector2(720f, 660f);

        private readonly GameObject _root;
        private readonly RectTransform _panel;
        private readonly Image _glow;
        private float _sideScale = 1f;
        private readonly Text _title;
        private readonly UI.WigglingImage _fish;
        private bool _wiggles;
        private readonly UI.ShineSweep _shine;
        private readonly RectTransform _fishRect;
        private static readonly Vector2 FishBox = new Vector2(560f, 210f);
        private readonly Text _name;
        private readonly Text _scientificName;
        private readonly Text _size;
        private readonly Image _gem;
        private readonly Text _rarity;
        private readonly Text _valueCaption;
        private readonly Text _value;
        private readonly Text _footer;
        private readonly Text _hint;
        private Color _glowColor;
        private float _age;

        // « Photo » d'une nouvelle espèce : flash, bord de photo, tampon, confettis.
        private const int ConfettiCount = 70;
        private readonly RectTransform _photo;
        private readonly Image _photoFrame;
        private readonly RectTransform _stamp;
        private readonly Image _stampImage;
        private readonly Text _stampText;
        private readonly Image _flash;
        private readonly System.Collections.Generic.List<Image> _confetti = new System.Collections.Generic.List<Image>();
        private readonly Vector2[] _confettiVelocity = new Vector2[ConfettiCount];
        private readonly float[] _confettiSpin = new float[ConfettiCount];
        private readonly System.Random _random = new System.Random();
        private bool _celebrate;
        private bool _photoMode;
        private readonly UiSkin _skin;

        public CatchCard(RectTransform canvas, UiSkin skin)
        {
            _skin = skin;
            _root = new GameObject("Carte de prise", typeof(RectTransform));
            var rootRect = _root.GetComponent<RectTransform>();
            rootRect.SetParent(canvas, false);
            UiFactory.Stretch(rootRect);

            Image dim = UiFactory.Plain(rootRect, "Voile", new Color(0f, 0f, 0f, 0.45f));
            UiFactory.Stretch(dim.rectTransform);

            // Halo de rareté derrière la carte.
            _glow = UiFactory.Plain(rootRect, "Halo", Color.white);
            _glow.sprite = skin != null ? skin.Glow : null;
            UiFactory.Center(_glow.rectTransform, Vector2.zero, PanelSize * 1.55f);

            Image panel = UiFactory.Sliced(rootRect, "Cadre", skin != null ? skin.Panel : null, skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            _panel = panel.rectTransform;
            UiFactory.Center(_panel, Vector2.zero, PanelSize);

            // Fond d'eau et poisson.
            _photo = new GameObject("Photo", typeof(RectTransform)).GetComponent<RectTransform>();
            _photo.SetParent(_panel, false);
            UiFactory.Center(_photo, new Vector2(0f, 128f), new Vector2(620f, 262f));
            // Bord blanc de photo souvenir (nouvelle espèce seulement), un peu plus large en bas.
            _photoFrame = UiFactory.Plain(_photo, "Bord de la photo", new Color(0.97f, 0.95f, 0.90f));
            UiFactory.Center(_photoFrame.rectTransform, new Vector2(0f, -2f), new Vector2(648f, 286f));
            Image backdrop = UiFactory.Sliced(_photo, "Fond d'eau", skin != null ? skin.WaterBackdrop : null, skin != null ? Color.white : new Color(0.2f, 0.55f, 0.65f));
            UiFactory.Center(backdrop.rectTransform, Vector2.zero, new Vector2(620f, 262f));
            // Le poisson frétille : son dessin ondule, la queue bat (WigglingImage).
            var fishObject = new GameObject("Poisson", typeof(RectTransform), typeof(UI.WigglingImage));
            fishObject.transform.SetParent(backdrop.rectTransform, false);
            _fish = fishObject.GetComponent<UI.WigglingImage>();
            _fish.raycastTarget = false;
            _shine = UI.ShineSweep.Attach(_fish); // éclat sur les écailles des espèces rares
            _fishRect = _fish.rectTransform;
            UiFactory.Center(_fishRect, Vector2.zero, FishBox);

            // Bandeau du titre, à cheval sur le haut du cadre.
            _title = UiFactory.TitlePlaque(_panel, skin, string.Empty, 36, new Vector2(0f, 318f), new Vector2(460f, 72f), out _);

            // Tampon « ajoutée au carnet », qui s'abat sur le coin de la photo.
            _stampImage = UiFactory.Sliced(_panel, "Tampon", skin != null ? skin.Slot : null, new Color(0.80f, 0.16f, 0.12f, 0.95f));
            _stamp = _stampImage.rectTransform;
            UiFactory.Center(_stamp, new Vector2(196f, 228f), new Vector2(290f, 56f));
            _stampText = UiFactory.Label(_stamp, "Texte", 24, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Stretch(_stampText.rectTransform);
            UiFactory.Bold(_stampText);
            _stampText.text = "AJOUTÉE AU CARNET !";

            _name = UiFactory.Label(_panel, "Nom", 42, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Bold(_name);
            UiFactory.Center(_name.rectTransform, new Vector2(0f, -38f), new Vector2(660f, 54f));

            _scientificName = UiFactory.Label(_panel, "Nom scientifique", 25, TextAnchor.MiddleCenter, Muted);
            _scientificName.fontStyle = FontStyle.Italic;
            UiFactory.Center(_scientificName.rectTransform, new Vector2(0f, -80f), new Vector2(660f, 34f));

            // Trois pastilles : taille, rareté, valeur.
            _size = Chip(_panel, skin, new Vector2(-212f, -150f), "TAILLE", out _);
            _rarity = Chip(_panel, skin, new Vector2(0f, -150f), "RARETÉ", out RectTransform rarityChip);
            _gem = UiFactory.Plain(rarityChip, "Pierre", Color.white);
            _gem.sprite = skin != null ? skin.Gem : null;
            UiFactory.Place(_gem.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, -10f), new Vector2(26f, 26f));
            _value = Chip(_panel, skin, new Vector2(212f, -150f), "VALEUR", out RectTransform valueChip);
            _valueCaption = valueChip.Find("Légende").GetComponent<Text>();

            _footer = UiFactory.Label(_panel, "Pied", 27, TextAnchor.MiddleCenter, Gold);
            UiFactory.Bold(_footer);
            _footer.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Center(_footer.rectTransform, new Vector2(0f, -224f), new Vector2(640f, 40f));

            _hint = UiFactory.Label(_panel, "Aide", 21, TextAnchor.MiddleCenter, Muted);
            UiFactory.Center(_hint.rectTransform, new Vector2(0f, -272f), new Vector2(640f, 30f));

            // Confettis par-dessus la carte, puis le flash de l'appareil photo par-dessus tout.
            for (int i = 0; i < ConfettiCount; i++)
            {
                Image piece = UiFactory.Plain(rootRect, "Confetti", Color.white);
                piece.raycastTarget = false;
                piece.rectTransform.sizeDelta = i % 3 == 0 ? new Vector2(10f, 10f) : new Vector2(14f, 7f);
                _confetti.Add(piece);
            }

            _flash = UiFactory.Plain(rootRect, "Flash", new Color(1f, 1f, 1f, 0f));
            _flash.raycastTarget = false;
            UiFactory.Stretch(_flash.rectTransform);

            Hide();
        }

        public bool IsVisible => _root.activeSelf;

        /// <summary>Carte simple (sans sac) : utilisée pour prévisualiser une espèce.</summary>
        public void Show(FishSpecies species, float sizeCm, bool isNew)
        {
            Show(new CaughtFish(species, sizeCm), isNew, false, -1, -1);
        }

        /// <summary>Carte de prise avec l'état du sac (<paramref name="bagCount"/> &lt; 0 : pas de sac).</summary>
        /// <param name="beatenRecord">Ancien record de l'espèce que cette prise vient de battre (0 = pas de record).</param>
        public void Show(CaughtFish fish, bool isNew, bool bagFull, int bagCount, int bagCapacity, float beatenRecord = 0f)
        {
            FishSpecies species = fish.Species;
            Color rarityColor = species.Rarity.DisplayColor();

            bool record = !isNew && beatenRecord > 0f;
            _title.text = isNew ? "Nouvelle espèce !" : record ? "Nouveau record !" : "Belle prise !";
            _glowColor = rarityColor;
            _glowColor.a = GlowStrength(species.Rarity);

            _fish.sprite = species.Sprite;
            _wiggles = species.Kind == CreatureKind.Fish && species.Sprite != null;
            _shine.SetRarity(species.Rarity, species.Sprite != null);
            _fish.color = species.Sprite != null ? Color.white : species.PlaceholderColor;
            // Taille du dessin selon la vraie taille de la prise : une ablette paraît petite, une carangue remplit la carte.
            Vector2 pixels = species.Sprite != null ? species.Sprite.rect.size : new Vector2(2f, 1f);
            _fishRect.sizeDelta = FishDisplayScale.Fit(FishBox, pixels, fish.SizeCm);

            _name.text = species.CommonName;
            _scientificName.text = species.ScientificName;
            _size.text = record ? $"{fish.SizeCm:0} cm <color=#FFD64D>(+{Mathf.Max(1f, fish.SizeCm - beatenRecord):0})</color>" : $"{fish.SizeCm:0} cm";
            // Espaces : laissent la place à la pierre de rareté, à gauche dans la pastille.
            _rarity.text = $"     {species.Rarity.DisplayName()}";
            _rarity.color = rarityColor;
            _gem.color = rarityColor;

            if (isNew)
            {
                _valueCaption.text = "DESTINATION";
                _value.text = "Aquarium";
                _value.color = Gold;
            }
            else
            {
                _valueCaption.text = "VALEUR";
                _value.text = $"{FishShop.PriceOf(fish)} pièces";
                _value.color = Gold;
            }

            string bag = bagCount >= 0 ? $"  (sac {bagCount}/{bagCapacity})" : string.Empty;
            // Sac plein : la carte passe à gauche, la fenêtre du sac s'ouvre à droite pour choisir quoi relâcher.
            _sideScale = bagFull ? 0.82f : 1f;
            Vector2 place = bagFull ? new Vector2(-500f, 0f) : Vector2.zero;
            _panel.anchoredPosition = place;
            _glow.rectTransform.anchoredPosition = place;
            if (bagFull)
            {
                _footer.text = "Sac plein ! Relâche une prise pour la garder";
                _footer.color = Gold;
                _hint.text = $"1 à {bagCapacity} : relâcher cette prise du sac   •   Espace : relâcher le nouveau";
                _hint.color = Color.white;
            }
            else
            {
                _footer.text = isNew ? $"Garde-la pour l'aquarium !{bag}"
                    : record ? $"Ancien record : {beatenRecord:0} cm{bag}"
                    : $"Doublon : le marchand te l'achètera{bag}";
                _footer.color = isNew || record ? Gold : Muted;
                _hint.text = "Espace pour continuer";
                _hint.color = Muted;
            }

            // Nouvelle espèce : c'est une photo souvenir (flash, bord blanc, tampon, confettis).
            // Nouveau record : tampon doré et pluie de confettis dorés, sans le flash ni la photo.
            _celebrate = isNew || record;
            _photoMode = isNew;
            _photoFrame.gameObject.SetActive(isNew);
            _stamp.gameObject.SetActive(_celebrate);
            _stampText.text = isNew ? "AJOUTÉE AU CARNET !" : "NOUVEAU RECORD !";
            _stampText.color = isNew ? Color.white : new Color(0.30f, 0.16f, 0.04f);
            // Rouge sur la case creusée pour une nouvelle espèce, bandeau doré de la charte pour un record.
            _stampImage.sprite = _skin == null ? null : isNew ? _skin.Slot : _skin.Plaque;
            _stampImage.color = isNew ? new Color(0.80f, 0.16f, 0.12f, 0.95f) : Color.white;
            Color[] colors = isNew
                ? new[] { rarityColor, Gold, Color.white, new Color(0.96f, 0.46f, 0.64f), new Color(0.40f, 0.86f, 0.96f), new Color(0.56f, 0.90f, 0.46f) }
                : new[] { Gold, new Color(1f, 0.94f, 0.62f), new Color(0.90f, 0.62f, 0.16f), Color.white };
            for (int i = 0; i < _confetti.Count; i++)
            {
                Image piece = _confetti[i];
                piece.gameObject.SetActive(isNew);
                piece.color = colors[i % colors.Length];
                piece.rectTransform.anchoredPosition = new Vector2(Range(-940f, 940f), Range(560f, 900f));
                piece.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Range(0f, 360f));
                _confettiVelocity[i] = new Vector2(Range(-50f, 50f), Range(-260f, -120f));
                _confettiSpin[i] = Range(-400f, 400f);
            }

            _age = 0f;
            _root.SetActive(true);
            Animate(0f);
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

        /// <summary>Animation : la carte « saute » à l'apparition, le poisson nage sur place, le halo respire.</summary>
        public void Animate(float deltaTime)
        {
            if (!IsVisible)
            {
                return;
            }

            _age += deltaTime;
            float t = Mathf.Clamp01(_age / PopDuration);
            float overshoot = 1f + Mathf.Sin(t * Mathf.PI) * 0.10f;
            _panel.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, t) * overshoot * _sideScale;

            // Nage sur place ; de temps en temps, un sursaut : coup de queue, petit saut et bascule.
            float flop = _wiggles ? UI.WiggleMath.Flop(_age) : 0f;
            _fishRect.anchoredPosition = new Vector2(Mathf.Sin(_age * 1.3f) * 8f, Mathf.Sin(_age * 3f) * 6f + flop * 24f);
            _fishRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_age * 5f) * 2.5f + flop * Mathf.Sin(_age * 26f) * 7f);
            _fish.Phase = _age * (9f + flop * 10f);
            _fish.Amplitude = _wiggles ? 1.4f + flop * 3.5f : 0f;
            _fish.Refresh();

            AnimateCelebration(deltaTime);

            Color glow = _glowColor;
            glow.a *= 0.75f + 0.25f * Mathf.Sin(_age * 2.5f);
            _glow.color = glow;
            _glow.rectTransform.localScale = Vector3.one * (Mathf.Lerp(0.6f, 1f, t) + 0.03f * Mathf.Sin(_age * 2.5f)) * _sideScale;
        }

        private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);

        /// <summary>Flash qui s'efface, photo qui se redresse, tampon qui s'abat, confettis qui tombent en tournant.</summary>
        private void AnimateCelebration(float deltaTime)
        {
            if (!_celebrate)
            {
                SetAlpha(_flash, 0f);
                _photo.localRotation = Quaternion.identity;
                return;
            }

            SetAlpha(_flash, _photoMode ? Mathf.Clamp01(0.85f - _age / 0.4f * 0.85f) : 0f);
            float settle = Mathf.SmoothStep(0f, 1f, _age / 0.6f);
            _photo.localRotation = _photoMode ? Quaternion.Euler(0f, 0f, Mathf.Lerp(-7f, -1.5f, settle)) : Quaternion.identity;

            float slam = Mathf.Clamp01((_age - 0.45f) / 0.16f);
            _stamp.localScale = Vector3.one * (slam <= 0f ? 0f : Mathf.Lerp(2.4f, 1f, slam) + (slam >= 1f ? Mathf.Max(0f, 0.08f - (_age - 0.61f) * 0.4f) : 0f));
            _stamp.localRotation = Quaternion.Euler(0f, 0f, -12f);
            SetAlpha(_stampImage, Mathf.Clamp01(slam * 2f) * 0.95f);
            SetAlpha(_stampText, Mathf.Clamp01(slam * 2f));

            for (int i = 0; i < _confetti.Count; i++)
            {
                RectTransform piece = _confetti[i].rectTransform;
                _confettiVelocity[i].y -= 50f * deltaTime;
                Vector2 sway = new Vector2(Mathf.Sin(_age * 3f + i) * 40f * deltaTime, 0f);
                piece.anchoredPosition += _confettiVelocity[i] * deltaTime + sway;
                piece.localRotation = Quaternion.Euler(0f, 0f, piece.localEulerAngles.z + _confettiSpin[i] * deltaTime);
                // Effet de papier qui tourne : la largeur bat.
                piece.localScale = new Vector3(Mathf.Abs(Mathf.Cos(_age * 6f + i)) * 0.8f + 0.2f, 1f, 1f);
            }
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            Color c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }

        private static float GlowStrength(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Uncommon: return 0.35f;
                case Rarity.Rare: return 0.45f;
                case Rarity.Epic: return 0.55f;
                case Rarity.Legendary: return 0.70f;
                default: return 0.20f;
            }
        }

        /// <summary>Pastille creusée avec une petite légende en capitales et une valeur.</summary>
        private static Text Chip(RectTransform parent, UiSkin skin, Vector2 position, string caption, out RectTransform chip)
        {
            Image frame = UiFactory.Sliced(parent, $"Pastille {caption}", skin != null ? skin.Slot : null, skin != null ? Color.white : new Color(0.12f, 0.2f, 0.27f));
            chip = frame.rectTransform;
            UiFactory.Center(chip, position, new Vector2(200f, 76f));

            Text legend = UiFactory.Label(chip, "Légende", 16, TextAnchor.MiddleCenter, Muted, shadow: false);
            legend.name = "Légende";
            legend.text = caption;
            UiFactory.Place(legend.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(0f, 22f));

            Text value = UiFactory.Label(chip, "Valeur", 26, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Bold(value);
            UiFactory.Place(value.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(0f, 36f));
            return value;
        }
    }
}
