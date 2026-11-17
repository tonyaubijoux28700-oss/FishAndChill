using FishAndChill.Collection;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Éclat sur les écailles : une bande de lumière en biais traverse le dessin d'un poisson rare, découpée à sa forme
    /// (le dessin sert de masque). Doré et plus fréquent pour une légendaire, avec quelques étincelles. Calculs dans
    /// <see cref="ShineMath"/> (testé).
    /// </summary>
    public sealed class ShineSweep : MonoBehaviour
    {
        private static Sprite _bandSprite;
        private static Sprite _sparkSprite;

        private RectTransform _band;
        private Image _bandImage;
        private readonly Image[] _sparks = new Image[3];
        private RectTransform _rect;
        private Rarity _rarity;
        private bool _active;
        private float _offset;

        /// <summary>Ajoute l'éclat sur cette image (elle devient le masque de la bande de lumière).</summary>
        public static ShineSweep Attach(Image image)
        {
            var mask = image.gameObject.GetComponent<Mask>();
            if (mask == null)
            {
                mask = image.gameObject.AddComponent<Mask>();
            }

            mask.showMaskGraphic = true;
            var shine = image.gameObject.AddComponent<ShineSweep>();
            shine.Build(image.rectTransform);
            return shine;
        }

        /// <summary>Brille (ou non) selon la rareté ; jamais pour une espèce pas encore découverte.</summary>
        public void SetRarity(Rarity rarity, bool discovered)
        {
            _rarity = rarity;
            _active = discovered && ShineMath.Shines(rarity);
            _bandImage.color = ShineMath.Tint(rarity);
            _band.gameObject.SetActive(false);
            foreach (Image spark in _sparks) spark.gameObject.SetActive(false);
        }

        private void Build(RectTransform rect)
        {
            _rect = rect;
            _offset = Random.value * 2f;
            _bandImage = UiFactory.Plain(rect, "Éclat", Color.white);
            _bandImage.sprite = BandSprite();
            _bandImage.raycastTarget = false;
            _band = _bandImage.rectTransform;
            _band.localRotation = Quaternion.Euler(0f, 0f, -22f);
            _band.gameObject.SetActive(false);
            for (int i = 0; i < _sparks.Length; i++)
            {
                Image spark = UiFactory.Plain(rect, "Étincelle", new Color(1f, 0.96f, 0.70f));
                spark.sprite = SparkSprite();
                spark.raycastTarget = false;
                spark.gameObject.SetActive(false);
                _sparks[i] = spark;
            }
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            Vector2 size = _rect.rect.size;
            float sweep = ShineMath.Sweep(Time.unscaledTime, ShineMath.Period(_rarity), _offset);
            _band.gameObject.SetActive(sweep >= 0f);
            if (sweep >= 0f)
            {
                _band.sizeDelta = new Vector2(Mathf.Max(10f, size.x * 0.16f), size.y * 2.4f);
                _band.anchoredPosition = new Vector2(Mathf.Lerp(-0.75f, 0.75f, sweep) * size.x, 0f);
            }

            // Étincelles qui scintillent sur une légendaire, juste après le passage de l'éclat.
            bool legendary = _rarity == Rarity.Legendary;
            for (int i = 0; i < _sparks.Length; i++)
            {
                Image spark = _sparks[i];
                float phase = Mathf.Repeat(Time.unscaledTime * 1.3f + i * 0.37f + _offset, 1f);
                bool on = legendary && phase < 0.35f;
                spark.gameObject.SetActive(on);
                if (!on) continue;
                float s = Mathf.Sin(phase / 0.35f * Mathf.PI);
                float k = Mathf.Max(10f, size.y * 0.16f) * s;
                spark.rectTransform.sizeDelta = new Vector2(k, k);
                spark.rectTransform.anchoredPosition = new Vector2((0.25f - i * 0.25f + Mathf.Sin(i * 7.1f) * 0.12f) * size.x, (Mathf.Cos(i * 3.3f) * 0.22f) * size.y);
            }
        }

        /// <summary>Bande de lumière douce : plus forte au milieu, qui s'efface sur les bords.</summary>
        private static Sprite BandSprite()
        {
            if (_bandSprite != null) return _bandSprite;
            var texture = new Texture2D(16, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < 16; x++)
            {
                float d = Mathf.Abs(x + 0.5f - 8f) / 8f;
                float a = Mathf.Round((1f - d * d) * 3f) / 3f; // paliers nets, façon pixel art
                for (int y = 0; y < 4; y++) texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }

            texture.Apply();
            _bandSprite = Sprite.Create(texture, new Rect(0, 0, 16, 4), new Vector2(0.5f, 0.5f), 16);
            return _bandSprite;
        }

        /// <summary>Petite étincelle en croix.</summary>
        private static Sprite SparkSprite()
        {
            if (_sparkSprite != null) return _sparkSprite;
            var texture = new Texture2D(7, 7, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int x = 0; x < 7; x++)
            {
                for (int y = 0; y < 7; y++)
                {
                    bool cross = x == 3 || y == 3;
                    bool center = Mathf.Abs(x - 3) <= 1 && Mathf.Abs(y - 3) <= 1;
                    texture.SetPixel(x, y, center ? Color.white : cross ? new Color(1f, 1f, 1f, 0.75f) : Color.clear);
                }
            }

            texture.Apply();
            _sparkSprite = Sprite.Create(texture, new Rect(0, 0, 7, 7), new Vector2(0.5f, 0.5f), 7);
            return _sparkSprite;
        }
    }
}
