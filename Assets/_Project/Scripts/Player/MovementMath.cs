using System;
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

        /// <summary>
        /// Pas de déplacement avec obstacles : si la destination est interdite, on essaie de glisser
        /// le long de l'obstacle (axe X seul, puis axe Y seul), sinon on reste sur place.
        /// </summary>
        public static Vector2 StepWithCollision(Vector2 position, Vector2 input, float speed, float deltaTime, Func<Vector2, bool> canStandAt)
        {
            if (canStandAt == null)
            {
                throw new ArgumentNullException(nameof(canStandAt));
            }

            Vector2 delta = Vector2.ClampMagnitude(input, 1f) * (speed * deltaTime);
            if (delta == Vector2.zero)
            {
                return position;
            }

            Vector2 target = position + delta;
            if (canStandAt(target))
            {
                return target;
            }

            var slideX = new Vector2(target.x, position.y);
            if (delta.x != 0f && canStandAt(slideX))
            {
                return slideX;
            }

            var slideY = new Vector2(position.x, target.y);
            if (delta.y != 0f && canStandAt(slideY))
            {
                return slideY;
            }

            return position;
        }

        /// <summary>Direction de regard mise à jour par l'entrée ; inchangée quand le joueur ne bouge pas.</summary>
        public static Vector2 UpdateFacing(Vector2 currentFacing, Vector2 input, float deadZone = 0.2f)
        {
            return input.magnitude > deadZone ? input.normalized : currentFacing;
        }
    }
}
