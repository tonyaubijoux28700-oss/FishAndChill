using FishAndChill.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.Fishing
{
    /// <summary>
    /// Carte de la bouteille à la mer : bandeau « Bouteille à la mer ! », bouteille de verre qui flotte en se balançant,
    /// message sur un bout de papier et pièces trouvées dedans. Affichée à la place de la carte de prise.
    /// </summary>
    public sealed class BottleCard
    {
        private static readonly Color Ink = new Color(0.24f, 0.16f, 0.10f);
        private static readonly Color Gold = new Color(1.00f, 0.85f, 0.32f);

        private readonly GameObject _root;
        private readonly RectTransform _panel;
        private readonly RectTransform _bottle;
        private readonly Text _message;
        private readonly Text _coins;
        private float _age;

        public BottleCard(RectTransform canvas, UiSkin skin)
        {
            _root = new GameObject("Bouteille à la mer", typeof(RectTransform));
            var rootRect = _root.GetComponent<RectTransform>();
            rootRect.SetParent(canvas, false);
            UiFactory.Stretch(rootRect);

            Image dim = UiFactory.Plain(rootRect, "Voile", new Color(0f, 0f, 0f, 0.45f));
            UiFactory.Stretch(dim.rectTransform);

            Image panel = UiFactory.Sliced(rootRect, "Cadre", skin != null ? skin.Panel : null, skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            _panel = panel.rectTransform;
            UiFactory.Center(_panel, Vector2.zero, new Vector2(760f, 600f));
            UiFactory.TitlePlaque(_panel, skin, "Bouteille à la mer !", 36, new Vector2(0f, 288f), new Vector2(480f, 72f), out _);

            // La bouteille, sur un fond d'eau.
            Image water = UiFactory.Sliced(_panel, "Fond d'eau", skin != null ? skin.WaterBackdrop : null, skin != null ? Color.white : new Color(0.2f, 0.55f, 0.65f));
            UiFactory.Center(water.rectTransform, new Vector2(0f, 150f), new Vector2(640f, 170f));
            Image bottle = UiFactory.Plain(water.rectTransform, "Bouteille", Color.white);
            bottle.sprite = BottleSprite();
            _bottle = bottle.rectTransform;
            UiFactory.Center(_bottle, Vector2.zero, new Vector2(240f, 100f));

            // Le message, sur un papier crème.
            Image paper = UiFactory.Plain(_panel, "Papier", new Color(0.96f, 0.91f, 0.76f));
            UiFactory.Center(paper.rectTransform, new Vector2(0f, -70f), new Vector2(640f, 220f));
            Image paperEdge = UiFactory.Plain(paper.rectTransform, "Bord", new Color(0.80f, 0.70f, 0.50f));
            UiFactory.Place(paperEdge.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 6f));
            _message = UiFactory.Label(paper.rectTransform, "Message", 26, TextAnchor.MiddleCenter, Ink, shadow: false);
            _message.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Center(_message.rectTransform, Vector2.zero, new Vector2(590f, 200f));

            _coins = UiFactory.Label(_panel, "Pièces", 30, TextAnchor.MiddleCenter, Gold);
            UiFactory.Bold(_coins);
            UiFactory.Center(_coins.rectTransform, new Vector2(0f, -218f), new Vector2(640f, 40f));

            Text hint = UiFactory.Label(_panel, "Aide", 21, TextAnchor.MiddleCenter, new Color(0.70f, 0.76f, 0.82f));
            hint.text = "Espace pour continuer";
            UiFactory.Center(hint.rectTransform, new Vector2(0f, -262f), new Vector2(640f, 30f));
            Hide();
        }

        public bool IsVisible => _root.activeSelf;

        public void Show(string message, int coins)
        {
            _message.text = message;
            _coins.text = coins > 0 ? $"Il y avait aussi {coins} pièces dans la bouteille !" : string.Empty;
            _age = 0f;
            _root.SetActive(true);
            Animate(0f);
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

        /// <summary>La carte saute à l'apparition, la bouteille flotte en se balançant sur l'eau.</summary>
        public void Animate(float deltaTime)
        {
            if (!IsVisible)
            {
                return;
            }

            _age += deltaTime;
            float t = Mathf.Clamp01(_age / 0.35f);
            _panel.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, t) * (1f + Mathf.Sin(t * Mathf.PI) * 0.1f);
            _bottle.anchoredPosition = new Vector2(Mathf.Sin(_age * 0.9f) * 14f, Mathf.Sin(_age * 2.2f) * 7f);
            _bottle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_age * 1.7f) * 9f - 8f);
        }

        /// <summary>Bouteille de verre vert couchée, 48×20 : bouchon de liège, message roulé à l'intérieur, reflet.</summary>
        private static Sprite BottleSprite()
        {
            const int w = 48;
            const int h = 20;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var glass = new Color(0.28f, 0.62f, 0.42f, 0.88f);
            var dark = new Color(0.10f, 0.30f, 0.20f);
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    Color c = Color.clear;
                    float half = x < 30 ? 7.5f : x < 36 ? Mathf.Lerp(7.5f, 3f, (x - 30) / 6f) : 3f; // corps, épaule, goulot
                    float dy = Mathf.Abs(y + 0.5f - 10f);
                    if (x >= 2 && x <= 42 && dy <= half)
                    {
                        c = dy > half - 1.2f || x == 2 ? dark : glass;
                        if (y > 12 && y < 15 && x > 5 && x < 30) c = new Color(0.80f, 0.96f, 0.86f, 0.95f); // reflet
                        if (x >= 8 && x <= 26 && dy <= 3f && !(y > 12 && y < 15)) c = (x - 8) % 6 == 0 ? new Color(0.70f, 0.60f, 0.40f) : new Color(0.94f, 0.88f, 0.70f); // papier roulé
                    }

                    if (x >= 43 && x <= 46 && dy <= 2.6f) c = x == 46 || dy > 1.8f ? new Color(0.44f, 0.30f, 0.18f) : new Color(0.72f, 0.54f, 0.34f); // bouchon
                    texture.SetPixel(x, y, c);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        }
    }
}
