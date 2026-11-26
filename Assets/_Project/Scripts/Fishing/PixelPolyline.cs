using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.Fishing
{
    /// <summary>
    /// Trait en pixel art dans le monde : une courbe dessinée par des points d'un pixel (16 par case), calés sur la
    /// grille des pixels. Sert au fil de pêche et à la canne (pas de rectangle étiré, pas de trait flou).
    /// </summary>
    public sealed class PixelPolyline
    {
        private const float PixelsPerUnit = 16f;

        private readonly Transform _parent;
        private readonly Sprite _pixel;
        private readonly int _sortingOrder;
        private readonly List<SpriteRenderer> _dots = new List<SpriteRenderer>();
        private readonly HashSet<Vector2Int> _used = new HashSet<Vector2Int>();
        private int _active;

        public PixelPolyline(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            _parent = go.transform;
            _sortingOrder = sortingOrder;
            _pixel = CreatePixelSprite();
        }

        /// <summary>Efface le trait (avant de redessiner, ou pour le cacher).</summary>
        public void Clear()
        {
            for (int i = 0; i < _active; i++)
            {
                _dots[i].enabled = false;
            }

            _active = 0;
            _used.Clear();
        }

        /// <summary>
        /// Dessine une courbe de Bézier quadratique de <paramref name="from"/> à <paramref name="to"/> qui pend
        /// de <paramref name="sag"/> unités vers le bas au milieu. <paramref name="colorAt"/> donne la couleur selon la
        /// position le long du trait (0 = début, 1 = fin), <paramref name="thickness"/> l'épaisseur en pixels.
        /// </summary>
        public void DrawCurve(Vector2 from, Vector2 to, float sag, System.Func<float, Color> colorAt, int thickness = 1)
        {
            Vector2 control = (from + to) * 0.5f + Vector2.down * sag;
            float length = Vector2.Distance(from, control) + Vector2.Distance(control, to);
            int steps = Mathf.Max(2, Mathf.CeilToInt(length * PixelsPerUnit * 1.5f));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 point = (1 - t) * (1 - t) * from + 2 * (1 - t) * t * control + t * t * to;
                Color color = colorAt(t);
                Plot(point, color);
                if (thickness > 1)
                {
                    Plot(point + new Vector2(0f, 1f / PixelsPerUnit), color);
                }

                if (thickness > 2)
                {
                    Plot(point - new Vector2(0f, 1f / PixelsPerUnit), color);
                }
            }
        }

        private void Plot(Vector2 world, Color color)
        {
            var cell = new Vector2Int(Mathf.FloorToInt(world.x * PixelsPerUnit), Mathf.FloorToInt(world.y * PixelsPerUnit));
            if (!_used.Add(cell))
            {
                return;
            }

            if (_active == _dots.Count)
            {
                var go = new GameObject("Pixel");
                go.transform.SetParent(_parent, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = _pixel;
                renderer.sortingOrder = _sortingOrder;
                _dots.Add(renderer);
            }

            SpriteRenderer dot = _dots[_active++];
            dot.enabled = true;
            dot.color = color;
            dot.transform.position = new Vector3((cell.x + 0.5f) / PixelsPerUnit, (cell.y + 0.5f) / PixelsPerUnit, 0f);
        }

        private static Sprite CreatePixelSprite()
        {
            var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }
    }
}
