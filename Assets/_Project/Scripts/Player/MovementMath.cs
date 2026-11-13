using UnityEngine;

namespace FishAndChill.Player
{
    /// <summary>Calcul du déplacement vue de dessus (logique pure, testée).</summary>
    public static class MovementMath
    {
        /// <summary>
        /// Nouvelle position après un pas de déplacement. L'entrée est limitée à une longueur de 1
        /// (pas de déplacement plus rapide en diagonale), puis la position est gardée dans <paramref name="bounds"/>.
        /// </summary>
        public static Vector2 Step(Vector2 position, Vector2 input, float speed, float deltaTime, Rect bounds)
        {
            Vector2 direction = Vector2.ClampMagnitude(input, 1f);
            Vector2 next = position + direction * (speed * deltaTime);
            return new Vector2(
                Mathf.Clamp(next.x, bounds.xMin, bounds.xMax),
                Mathf.Clamp(next.y, bounds.yMin, bounds.yMax));
        }
    }
}
