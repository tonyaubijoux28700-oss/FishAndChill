using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>Calculs de l'ambiance (logique pure, testée) : saut d'un poisson, nuages qui reviennent de l'autre côté.</summary>
    public static class AmbientMath
    {
        /// <summary>Position d'un poisson qui saute à l'instant t (0..1) : arc de <paramref name="height"/> au-dessus de la ligne de départ.</summary>
        public static Vector2 JumpArc(Vector2 from, Vector2 to, float height, float t)
        {
            t = Mathf.Clamp01(t);
            return Vector2.Lerp(from, to, t) + Vector2.up * (4f * height * t * (1f - t));
        }

        /// <summary>
        /// Un nuage sorti de la région (marge comprise) revient par le côté opposé.
        /// </summary>
        public static Vector2 Wrap(Vector2 position, Rect bounds, Vector2 margin)
        {
            Rect outer = Rect.MinMaxRect(bounds.xMin - margin.x, bounds.yMin - margin.y, bounds.xMax + margin.x, bounds.yMax + margin.y);
            float x = position.x;
            float y = position.y;
            if (x > outer.xMax) x = outer.xMin + (x - outer.xMax);
            else if (x < outer.xMin) x = outer.xMax - (outer.xMin - x);
            if (y > outer.yMax) y = outer.yMin + (y - outer.yMax);
            else if (y < outer.yMin) y = outer.yMax - (outer.yMin - y);
            return new Vector2(x, y);
        }
    }
}
