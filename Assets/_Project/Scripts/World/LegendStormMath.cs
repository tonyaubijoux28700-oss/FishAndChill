using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Règles de la tempête des boss (logique pure, testée) : plus on s'approche du remous d'un boss réveillé, plus la
    /// tempête monte ; pendant le combat elle est à son comble. Teinte du ciel, fréquence des éclairs, vent.
    /// </summary>
    public static class LegendStormMath
    {
        /// <summary>À cette distance du remous (et plus près), la tempête est à son comble.</summary>
        public const float NearDistance = 6f;

        /// <summary>Au-delà de cette distance, plus de tempête.</summary>
        public const float FarDistance = 30f;

        /// <summary>Ciel de tempête : presque nuit, violacé.</summary>
        public static readonly Color Tint = new Color(0.30f, 0.29f, 0.44f);

        /// <summary>Intensité visée (0 à 1) selon la distance au remous du boss réveillé, et au comble pendant le combat.</summary>
        public static float Target(bool bossAwake, float distance, bool fighting)
        {
            if (fighting) return 1f;
            if (!bossAwake) return 0f;
            return Mathf.Clamp01((FarDistance - distance) / (FarDistance - NearDistance));
        }

        /// <summary>La tempête monte en quelques secondes et retombe plus lentement.</summary>
        public static float Approach(float current, float target, float deltaTime)
            => Mathf.MoveTowards(current, target, deltaTime * (target > current ? 0.35f : 0.2f));

        /// <summary>Secondes avant le prochain éclair : de 6-16 s (orage ordinaire) à 1,2-3 s (tempête au comble).</summary>
        public static float LightningInterval(float intensity, float roll)
        {
            float calm = Mathf.Lerp(6f, 16f, roll);
            float wild = Mathf.Lerp(1.2f, 3f, roll);
            return Mathf.Lerp(calm, wild, Mathf.Clamp01(intensity));
        }

        /// <summary>Assez forte pour que la foudre tombe vraiment (éclair dessiné, secousse) ?</summary>
        public static bool StrikesBolt(float intensity) => intensity > 0.35f;
    }
}
