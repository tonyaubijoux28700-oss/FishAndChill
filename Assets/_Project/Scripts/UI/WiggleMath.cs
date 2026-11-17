using UnityEngine;

namespace FishAndChill.UI
{
    /// <summary>Calculs du frétillement des poissons sur la carte de prise (logique pure, testée).</summary>
    public static class WiggleMath
    {
        /// <summary>
        /// Décalage vertical (en pixels du dessin) d'une tranche du poisson : <paramref name="u"/> va de 0 (queue, à gauche)
        /// à 1 (museau). La tête bouge à peine, la queue bat fort ; la vague avance de la tête vers la queue.
        /// </summary>
        public static float Offset(float u, float phase, float amplitude)
        {
            float tail = 1f - Mathf.Clamp01(u);
            float weight = tail * tail;
            return Mathf.Sin(phase + u * 3.2f) * amplitude * weight;
        }

        /// <summary>
        /// Sursaut régulier : renvoie 0 la plupart du temps, puis monte à 1 et redescend pendant <paramref name="length"/>
        /// secondes, toutes les <paramref name="every"/> secondes (le premier juste après l'apparition de la carte).
        /// </summary>
        public static float Flop(float age, float every = 2.6f, float length = 0.5f, float first = 0.6f)
        {
            if (age < first) return 0f;
            float t = Mathf.Repeat(age - first, every);
            return t < length ? Mathf.Sin(t / length * Mathf.PI) : 0f;
        }
    }
}
