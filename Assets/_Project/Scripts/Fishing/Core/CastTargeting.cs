using System;
using UnityEngine;

namespace FishAndChill.Fishing
{
    /// <summary>Où tombe le bouchon : devant le pêcheur, et forcément dans l'eau (logique pure, testée).</summary>
    public static class CastTargeting
    {
        private const float SearchStep = 0.25f;

        /// <summary>
        /// Cherche un point d'eau dans la direction <paramref name="facing"/>, en partant de
        /// <paramref name="desiredDistance"/> et en s'éloignant jusqu'à <paramref name="maxDistance"/>.
        /// <paramref name="sideOffset"/> décale le lancer sur le côté (imprécision).
        /// Renvoie <c>null</c> si aucun point d'eau n'est à portée (le joueur regarde la terre).
        /// </summary>
        public static Vector2? FindCastPoint(
            Vector2 origin,
            Vector2 facing,
            float desiredDistance,
            float maxDistance,
            float sideOffset,
            Func<Vector2, bool> isWater)
        {
            if (isWater == null)
            {
                throw new ArgumentNullException(nameof(isWater));
            }

            Vector2 forward = facing.sqrMagnitude > 0f ? facing.normalized : Vector2.up;
            var side = new Vector2(-forward.y, forward.x);

            for (float distance = desiredDistance; distance <= maxDistance + 0.001f; distance += SearchStep)
            {
                Vector2 point = origin + forward * distance + side * sideOffset;
                if (isWater(point))
                {
                    return point;
                }
            }

            return null;
        }
    }
}
