using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.Pets
{
    /// <summary>Effets des animaux sur la pêche et la balade (logique pure, testée).</summary>
    public static class PetMath
    {
        /// <summary>Multiplicateur de chance d'une espèce : le bonus ne touche que les rares et plus.</summary>
        public static float RarityFactor(Rarity rarity, float rareMultiplier)
        {
            return rarity >= Rarity.Rare ? Mathf.Max(1f, rareMultiplier) : 1f;
        }

        /// <summary>
        /// Pièces trouvées pendant <paramref name="seconds"/> de marche (0 la plupart du temps) : une chance par seconde,
        /// puis un nombre entre <paramref name="min"/> et <paramref name="max"/>. Rien à l'arrêt.
        /// </summary>
        public static int FoundCoins(System.Random random, float seconds, bool walking, float chancePerSecond, int min, int max)
        {
            if (!walking || seconds <= 0f || chancePerSecond <= 0f) return 0;
            if (random.NextDouble() >= chancePerSecond * seconds) return 0;
            return random.Next(Mathf.Min(min, max), Mathf.Max(min, max) + 1);
        }

        /// <summary>
        /// Point de suivi : derrière le joueur, à <paramref name="distance"/> le long de son chemin récent
        /// (<paramref name="trail"/>, du plus récent au plus ancien). Pas assez de chemin : le dernier point connu.
        /// </summary>
        public static Vector2 FollowPoint(System.Collections.Generic.IReadOnlyList<Vector2> trail, float distance)
        {
            if (trail == null || trail.Count == 0) return Vector2.zero;
            float left = distance;
            for (int i = 1; i < trail.Count; i++)
            {
                float step = Vector2.Distance(trail[i - 1], trail[i]);
                if (step >= left && step > 0f) return Vector2.Lerp(trail[i - 1], trail[i], left / step);
                left -= step;
            }

            return trail[trail.Count - 1];
        }
    }
}
