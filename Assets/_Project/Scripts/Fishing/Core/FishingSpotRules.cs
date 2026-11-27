using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.Fishing
{
    /// <summary>
    /// Règles des coins à bulles et des remous (logique pure, testée) : leur rayon, la visée qui envoie le bouchon dans le
    /// remous d'une légende quand on lance face à lui, et les rives qu'on rejoint à pied.
    /// </summary>
    public static class FishingSpotRules
    {
        /// <summary>Rayon d'un coin : un bouchon posé à moins de cette distance du centre y pêche.</summary>
        public const float Radius = 1.3f;

        /// <summary>Écart maximal (en degrés) entre le regard et le coin pour que le lancer y aille tout seul.</summary>
        public const float AssistAngle = 32f;

        /// <summary>
        /// Coin vers lequel le lancer part tout seul : à portée (entre <paramref name="minDistance"/> et
        /// <paramref name="maxDistance"/>) et dans le regard (à moins de <see cref="AssistAngle"/>°). Le plus droit devant.
        /// Renvoie l'indice du coin, ou -1.
        /// </summary>
        public static int Assist(Vector2 origin, Vector2 facing, IList<Vector2> spots, float minDistance, float maxDistance)
        {
            int best = -1;
            float bestAngle = AssistAngle;
            if (spots == null || facing.sqrMagnitude < 0.0001f)
            {
                return best;
            }

            for (int i = 0; i < spots.Count; i++)
            {
                Vector2 to = spots[i] - origin;
                float distance = to.magnitude;
                if (distance < minDistance || distance > maxDistance + Radius * 0.5f)
                {
                    continue;
                }

                float angle = Vector2.Angle(facing, to);
                if (angle <= bestAngle)
                {
                    bestAngle = angle;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>
        /// Cases qu'on peut rejoindre à pied depuis <paramref name="start"/> (le ponton d'arrivée), en passant d'une case
        /// praticable à sa voisine (haut, bas, gauche, droite) : un coin de pêche ne doit faire face qu'à une de ces rives,
        /// jamais à un îlot qu'on ne peut pas atteindre.
        /// </summary>
        public static bool[,] Reachable(int width, int height, Vector2Int start, System.Func<int, int, bool> canStand)
        {
            var seen = new bool[width, height];
            if (start.x < 0 || start.y < 0 || start.x >= width || start.y >= height || !canStand(start.x, start.y)) return seen;
            var queue = new Queue<Vector2Int>();
            seen[start.x, start.y] = true;
            queue.Enqueue(start);
            Vector2Int[] steps = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                foreach (Vector2Int d in steps)
                {
                    Vector2Int n = c + d;
                    if (n.x < 0 || n.y < 0 || n.x >= width || n.y >= height || seen[n.x, n.y] || !canStand(n.x, n.y)) continue;
                    seen[n.x, n.y] = true;
                    queue.Enqueue(n);
                }
            }

            return seen;
        }
    }
}
