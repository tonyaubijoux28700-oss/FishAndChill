using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.Fishing
{
    /// <summary>
    /// Quand une espèce rare mord, avant même le combat : des étincelles de la couleur de la rareté jaillissent du
    /// bouchon (rare : quelques-unes ; épique : une gerbe ; légendaire : deux gerbes dorées). Pour une épique ou une
    /// légendaire, un bandeau s'affiche en haut de l'écran (et un flash doré pour une légendaire). On ne sait pas encore
    /// quelle espèce c'est : seulement que c'est quelque chose de spécial.
    /// </summary>
    [RequireComponent(typeof(FishingController))]
    public sealed class RareBiteEffect : MonoBehaviour
    {
        [SerializeField, Tooltip("Charte pixel art de l'interface (bandeau).")]
        private UiSkin _skin;

        [SerializeField, Min(0.2f)] private float _bannerSeconds = 2.4f;

        private sealed class Sparkle
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Delay;
            public float Age;
            public float Life;
        }

        private readonly List<Sparkle> _sparkles = new List<Sparkle>();
        private FishingController _controller;
        private Sprite _star;
        private RectTransform _banner;
        private Text _bannerText;
        private Image _flash;
        private float _bannerTime;
        private float _flashTime;

        private void Awake()
        {
            _controller = GetComponent<FishingController>();
            _star = CreateStarSprite();
            BuildUi();
        }

        private void OnEnable()
        {
            if (_controller != null)
            {
                _controller.StateChanged += OnStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_controller != null)
            {
                _controller.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged()
        {
            if (_controller.State != FishingState.Bite || _controller.HookedSpecies == null)
            {
                return;
            }

            Rarity rarity = _controller.HookedSpecies.Rarity;
            if (rarity < Rarity.Rare)
            {
                return;
            }

            Color color = rarity.DisplayColor();
            Vector2 at = _controller.CastPoint;
            Burst(at, color, rarity == Rarity.Rare ? 7 : rarity == Rarity.Epic ? 14 : 18, 0f);
            if (rarity == Rarity.Legendary)
            {
                Burst(at, new Color(1f, 0.86f, 0.36f), 16, 0.3f);
                _flashTime = 0.45f;
            }

            if (rarity >= Rarity.Epic)
            {
                _bannerText.text = rarity == Rarity.Legendary ? "Une prise LÉGENDAIRE a mordu !" : "Quelque chose de gros a mordu !";
                _bannerText.color = UiSkin.PlaqueText;
                _banner.GetComponent<Image>().color = Color.Lerp(Color.white, color, 0.35f);
                _bannerTime = _bannerSeconds;
                _banner.gameObject.SetActive(true);
            }
        }

        private void Burst(Vector2 at, Color color, int count, float delay)
        {
            for (int i = 0; i < count; i++)
            {
                Sparkle sparkle = Take();
                float angle = Random.value * Mathf.PI * 2f;
                float speed = Random.Range(1.4f, 3.2f);
                sparkle.Position = at;
                sparkle.Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.75f + 0.35f) * speed;
                sparkle.Delay = delay + Random.Range(0f, 0.12f);
                sparkle.Age = 0f;
                sparkle.Life = Random.Range(0.7f, 1.1f);
                sparkle.Renderer.color = Color.Lerp(color, Color.white, Random.Range(0f, 0.4f));
                sparkle.Renderer.enabled = false;
            }
        }

        private Sparkle Take()
        {
            foreach (Sparkle s in _sparkles)
            {
                if (s.Age >= s.Life)
                {
                    return s;
                }
            }

            var go = new GameObject("Étincelle");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _star;
            renderer.sortingOrder = 30;
            var sparkle = new Sparkle { Renderer = renderer };
            _sparkles.Add(sparkle);
            return sparkle;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (Sparkle s in _sparkles)
            {
                if (s.Age >= s.Life)
                {
                    s.Renderer.enabled = false;
                    continue;
                }

                if (s.Delay > 0f)
                {
                    s.Delay -= dt;
                    continue;
                }

                s.Age += dt;
                s.Velocity *= Mathf.Exp(-3f * dt);   // ralentissent en s'éloignant
                s.Velocity.y -= 1.2f * dt;           // et retombent un peu
                s.Position += s.Velocity * dt;
                float t = s.Age / s.Life;
                s.Renderer.enabled = true;
                s.Renderer.transform.position = new Vector3(Mathf.Round(s.Position.x * 16f) / 16f, Mathf.Round(s.Position.y * 16f) / 16f, 0f);
                float twinkle = 0.8f + 0.4f * Mathf.Abs(Mathf.Sin(s.Age * 18f + s.Life * 10f));
                s.Renderer.transform.localScale = Vector3.one * twinkle * Mathf.Lerp(1.1f, 0.5f, t);
                Color c = s.Renderer.color;
                c.a = 1f - t * t;
                s.Renderer.color = c;
            }

            // Bandeau : apparaît en rebondissant, puis s'efface (ou dès que la touche est passée).
            if (_bannerTime > 0f)
            {
                _bannerTime -= Time.unscaledDeltaTime;
                // Il s'efface dès que le combat commence (la jauge peut s'afficher au même endroit).
                bool still = _controller.State == FishingState.Bite;
                if (!still)
                {
                    _bannerTime = Mathf.Min(_bannerTime, 0.25f);
                }

                float shown = _bannerSeconds - _bannerTime;
                float pop = Mathf.Clamp01(shown / 0.18f);
                _banner.localScale = Vector3.one * (Mathf.Lerp(0.6f, 1f, pop) + Mathf.Sin(pop * Mathf.PI) * 0.12f);
                _banner.gameObject.SetActive(_bannerTime > 0f);
            }

            if (_flashTime > 0f)
            {
                _flashTime -= Time.unscaledDeltaTime;
                _flash.color = new Color(1f, 0.86f, 0.36f, Mathf.Clamp01(_flashTime / 0.45f) * 0.28f);
            }
            else if (_flash.color.a > 0f)
            {
                _flash.color = Color.clear;
            }
        }

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Interface des touches rares", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();

            _flash = UiFactory.Plain(root, "Flash doré", Color.clear);
            UiFactory.Stretch(_flash.rectTransform);

            _bannerText = UiFactory.TitlePlaque(root, _skin, string.Empty, 34, new Vector2(0f, 430f), new Vector2(700f, 80f), out _banner);
            _banner.gameObject.SetActive(false);
        }

        /// <summary>Étoile 7×7 à quatre branches, cœur blanc : teintée de la couleur de la rareté.</summary>
        private static Sprite CreateStarSprite()
        {
            var texture = new Texture2D(7, 7, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color[49];
            for (int y = 0; y < 7; y++)
            {
                for (int x = 0; x < 7; x++)
                {
                    int dx = Mathf.Abs(x - 3);
                    int dy = Mathf.Abs(y - 3);
                    bool arm = (dx == 0 && dy <= 3) || (dy == 0 && dx <= 3);
                    bool core = dx <= 1 && dy <= 1;
                    pixels[y * 7 + x] = core ? Color.white : arm ? new Color(1f, 1f, 1f, dx + dy == 3 ? 0.55f : 0.9f) : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 7, 7), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
