using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>Petits outils pour construire l'interface par code avec la charte (<see cref="UiSkin"/>).</summary>
    public static class UiFactory
    {
        private static Font _font;

        /// <summary>Police de l'interface : celle de la charte si elle en a une, sinon la police par défaut de Unity.</summary>
        public static Font Font => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        private static float _fontScale = 1f;

        /// <summary>Taille de texte corrigée selon la police de la charte.</summary>
        public static int Size(int size) => Mathf.Max(1, Mathf.RoundToInt(size * _fontScale));

        /// <summary>Vrai quand la police de la charte (pixel art) est utilisée.</summary>
        public static bool UsesSkinFont { get; private set; }

        /// <summary>
        /// Met un texte en gras… sauf avec la police pixel art : le gras artificiel de Unity l'empâte
        /// (un « 2 » devient un « 8 »). La police pixel art est déjà épaisse et lisible.
        /// </summary>
        public static void Bold(Text text)
        {
            text.fontStyle = UsesSkinFont ? FontStyle.Normal : FontStyle.Bold;
        }

        /// <summary>Adopte la police de la charte (à appeler avant de construire une interface).</summary>
        public static void UseSkin(UiSkin skin)
        {
            if (skin != null && skin.Font != null)
            {
                _font = skin.Font;
                UsesSkinFont = true;
                _fontScale = skin.FontScale;
            }
        }

        /// <summary>Image étirable en 9 parties (coins nets) ; sans sprite, simple rectangle de la couleur donnée.</summary>
        public static Image Sliced(RectTransform parent, string name, Sprite sprite, Color color)
        {
            Image image = Plain(parent, name, color);
            image.sprite = sprite;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                image.type = Image.Type.Sliced;
                image.fillCenter = true;
            }

            return image;
        }

        public static Image Plain(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Texte avec une ombre portée (plus lisible et plus « fini » qu'un contour).</summary>
        public static Text Label(RectTransform parent, string name, int size, TextAnchor alignment, Color color, bool shadow = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(FrenchText));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Font;
            text.fontSize = Size(size);
            text.alignment = alignment;
            text.color = color;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            if (shadow)
            {
                var drop = go.AddComponent<Shadow>();
                drop.effectColor = new Color(0f, 0f, 0f, 0.65f);
                drop.effectDistance = new Vector2(2f, -2f);
            }

            return text;
        }

        public static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Placement centré dans le parent (ancres et pivot au milieu).</summary>
        public static void Center(RectTransform rect, Vector2 position, Vector2 size)
        {
            Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        }

        public static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>Bandeau de titre doré avec son texte foncé.</summary>
        public static Text TitlePlaque(RectTransform parent, UiSkin skin, string text, int size, Vector2 position, Vector2 boxSize, out RectTransform plaque)
        {
            Image image = Sliced(parent, "Bandeau", skin != null ? skin.Plaque : null, skin != null ? Color.white : new Color(0.93f, 0.72f, 0.28f));
            plaque = image.rectTransform;
            Center(plaque, position, boxSize);
            Text label = Label(plaque, "Titre", size, TextAnchor.MiddleCenter, UiSkin.PlaqueText, shadow: false);
            UiFactory.Bold(label);
            label.text = text;
            Stretch(label.rectTransform);
            return label;
        }
    }
}
