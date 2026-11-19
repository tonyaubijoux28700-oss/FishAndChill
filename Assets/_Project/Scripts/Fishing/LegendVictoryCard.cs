using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.Fishing
{
    /// <summary>
    /// Carte de victoire contre une légende : bandeau « Légende vaincue ! », la bête en grand sur un fond d'eau, rayons
    /// dorés qui tournent derrière elle, confettis dorés qui tombent, son nom, sa taille, les pièces gagnées et une phrase.
    /// Affichée à la place de la carte de prise.
    /// </summary>
    public sealed class LegendVictoryCard
    {
        private static readonly Color Gold = new Color(1.00f, 0.85f, 0.32f);
        private static readonly Color Ink = new Color(0.24f, 0.16f, 0.10f);

        private readonly GameObject _root;
        private readonly RectTransform _panel;
        private readonly RectTransform _rays;
        private readonly RectTransform _fish;
        private readonly Image _fishImage;
        private readonly Text _name;
        private readonly Text _size;
        private readonly Text _coins;
        private readonly Text _line;
        private readonly List<Confetto> _confetti = new List<Confetto>();
        private float _age;

        private sealed class Confetto
        {
            public RectTransform Rect;
            public Vector2 Start;
            public float Speed;
            public float Spin;
            public float Phase;
        }

        public LegendVictoryCard(RectTransform canvas, UiSkin skin)
        {
            _root = new GameObject("Légende vaincue", typeof(RectTransform));
            var rootRect = _root.GetComponent<RectTransform>();
            rootRect.SetParent(canvas, false);
            UiFactory.Stretch(rootRect);

            Image dim = UiFactory.Plain(rootRect, "Voile", new Color(0f, 0f, 0f, 0.55f));
            UiFactory.Stretch(dim.rectTransform);

            Image panel = UiFactory.Sliced(rootRect, "Cadre", skin != null ? skin.Panel : null, skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            _panel = panel.rectTransform;
            UiFactory.Center(_panel, Vector2.zero, new Vector2(1000f, 720f));
            UiFactory.TitlePlaque(_panel, skin, "Légende vaincue !", 40, new Vector2(0f, 350f), new Vector2(560f, 80f), out _);

            Image water = UiFactory.Sliced(_panel, "Fond d'eau", skin != null ? skin.WaterBackdrop : null, skin != null ? Color.white : new Color(0.2f, 0.55f, 0.65f));
            UiFactory.Center(water.rectTransform, new Vector2(0f, 120f), new Vector2(880f, 330f));
            var mask = water.gameObject.AddComponent<RectMask2D>();
            mask.padding = new Vector4(6f, 6f, 6f, 6f);

            // Rayons dorés qui tournent lentement derrière la bête.
            var rays = new GameObject("Rayons", typeof(RectTransform));
            _rays = rays.GetComponent<RectTransform>();
            _rays.SetParent(water.rectTransform, false);
            UiFactory.Center(_rays, Vector2.zero, Vector2.zero);
            for (int i = 0; i < 10; i++)
            {
                Image ray = UiFactory.Plain(_rays, "Rayon", new Color(Gold.r, Gold.g, Gold.b, 0.16f));
                ray.raycastTarget = false;
                ray.rectTransform.pivot = new Vector2(0.5f, 0f);
                ray.rectTransform.sizeDelta = new Vector2(46f, 520f);
                ray.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i * 36f);
            }

            Image halo = UiFactory.Plain(water.rectTransform, "Halo", new Color(Gold.r, Gold.g, Gold.b, 0.45f));
            halo.sprite = skin != null ? skin.Glow : null;
            UiFactory.Center(halo.rectTransform, Vector2.zero, new Vector2(700f, 300f));

            _fishImage = UiFactory.Plain(water.rectTransform, "La légende", Color.white);
            _fishImage.preserveAspect = true;
            _fish = _fishImage.rectTransform;
            UiFactory.Center(_fish, Vector2.zero, new Vector2(800f, 300f));

            _name = UiFactory.Label(_panel, "Nom", 46, TextAnchor.MiddleCenter, Gold);
            UiFactory.Bold(_name);
            UiFactory.Center(_name.rectTransform, new Vector2(0f, -88f), new Vector2(900f, 60f));
            _size = UiFactory.Label(_panel, "Taille", 28, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Center(_size.rectTransform, new Vector2(0f, -136f), new Vector2(900f, 40f));

            Image paper = UiFactory.Plain(_panel, "Papier", new Color(0.96f, 0.91f, 0.76f));
            UiFactory.Center(paper.rectTransform, new Vector2(0f, -210f), new Vector2(820f, 90f));
            _line = UiFactory.Label(paper.rectTransform, "Phrase", 25, TextAnchor.MiddleCenter, Ink, shadow: false);
            _line.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Center(_line.rectTransform, Vector2.zero, new Vector2(780f, 84f));

            _coins = UiFactory.Label(_panel, "Pièces", 34, TextAnchor.MiddleCenter, Gold);
            UiFactory.Bold(_coins);
            UiFactory.Center(_coins.rectTransform, new Vector2(0f, -284f), new Vector2(900f, 44f));

            Text hint = UiFactory.Label(_panel, "Aide", 21, TextAnchor.MiddleCenter, new Color(0.70f, 0.76f, 0.82f));
            hint.text = "Espace pour continuer";
            UiFactory.Center(hint.rectTransform, new Vector2(0f, -330f), new Vector2(640f, 30f));

            // Confettis dorés (et quelques turquoise) qui tombent devant la carte.
            var random = new System.Random(11);
            for (int i = 0; i < 60; i++)
            {
                Color c = i % 5 == 0 ? new Color(0.45f, 0.95f, 0.92f) : i % 3 == 0 ? new Color(1f, 0.96f, 0.80f) : Gold;
                Image confetto = UiFactory.Plain(rootRect, "Confetti", c);
                confetto.raycastTarget = false;
                confetto.rectTransform.sizeDelta = new Vector2(10f, 16f);
                _confetti.Add(new Confetto
                {
                    Rect = confetto.rectTransform,
                    Start = new Vector2((float)random.NextDouble() * 1800f - 900f, 560f + (float)random.NextDouble() * 700f),
                    Speed = 140f + (float)random.NextDouble() * 160f,
                    Spin = 120f + (float)random.NextDouble() * 360f,
                    Phase = (float)random.NextDouble() * 6.3f,
                });
            }

            Hide();
        }

        public bool IsVisible => _root.activeSelf;

        public void Show(LegendFish legend, int coins)
        {
            FishSpecies species = legend.Species;
            _fishImage.sprite = legend.Art;
            _name.text = legend.Title;
            _size.text = species != null
                ? $"{legend.KindName} — {legend.SizeCm:0} cm   ({species.CommonName.ToLowerInvariant()} : {species.SizeRangeCm.y:0} cm au plus d'habitude)"
                : $"{legend.KindName} — {legend.SizeCm:0} cm";
            _line.text = $"L'artefact « {legend.ArtifactName} » repose, apaisé. Le Dr Hina Teva n'en croira pas ses yeux !";
            _coins.text = coins > 0 ? $"+{coins} pièces" : string.Empty;
            _age = 0f;
            _root.SetActive(true);
            Animate(0f);
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

        public void Animate(float deltaTime)
        {
            if (!IsVisible)
            {
                return;
            }

            _age += deltaTime;
            float t = Mathf.Clamp01(_age / 0.4f);
            _panel.localScale = Vector3.one * Mathf.Lerp(0.5f, 1f, t) * (1f + Mathf.Sin(t * Mathf.PI) * 0.12f);
            _rays.localRotation = Quaternion.Euler(0f, 0f, _age * 12f);
            _fish.anchoredPosition = new Vector2(Mathf.Sin(_age * 0.8f) * 10f, Mathf.Sin(_age * 1.7f) * 8f);
            _fish.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_age * 1.3f) * 4f);

            foreach (Confetto c in _confetti)
            {
                float y = c.Start.y - c.Speed * _age;
                if (y < -620f) y += 1300f * Mathf.Ceil((-620f - y) / 1300f);
                c.Rect.anchoredPosition = new Vector2(c.Start.x + Mathf.Sin(_age * 2f + c.Phase) * 30f, y);
                c.Rect.localRotation = Quaternion.Euler(0f, 0f, c.Spin * _age + c.Phase * 57f);
                c.Rect.localScale = new Vector3(Mathf.Abs(Mathf.Cos(_age * 4f + c.Phase)), 1f, 1f);   // il tournoie
            }
        }
    }
}
