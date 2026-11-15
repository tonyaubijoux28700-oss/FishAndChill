using UnityEngine;

namespace FishAndChill.UI
{
    /// <summary>Calculs du menu pause (logique pure, testée) : déplacement du curseur et réglage du volume.</summary>
    public static class PauseMenuLogic
    {
        public const float VolumeStep = 0.1f;

        /// <summary>Ligne suivante ou précédente, en boucle (du bas on revient en haut).</summary>
        public static int Move(int index, int delta, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            return ((index + delta) % count + count) % count;
        }

        /// <summary>Monte ou baisse le volume d'un cran (10 %), entre 0 et 100 %, arrondi au cran.</summary>
        public static float StepVolume(float volume, int direction)
        {
            float steps = Mathf.Round(Mathf.Clamp01(volume) / VolumeStep) + Mathf.Clamp(direction, -1, 1);
            return Mathf.Clamp01(steps * VolumeStep);
        }

        /// <summary>Le volume tel qu'affiché : « 80 % ».</summary>
        public static string VolumeText(float volume) => $"{Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f)} %";
    }
}
