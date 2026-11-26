using UnityEngine;

namespace FishAndChill.Player
{
    /// <summary>
    /// Petites animations d'attente quand on ne bouge plus (logique pure, testée). Le pêcheur, après
    /// <see cref="PlayerDelay"/> secondes, regarde à gauche puis à droite, tape du pied en attendant, puis s'étire en
    /// bâillant, et recommence toutes les <see cref="PlayerCycle"/> secondes. L'animal, après <see cref="PetDelay"/>
    /// secondes, fait son geste à lui (le chat s'assoit et se lèche la patte, l'oiseau se pose sur la tête du pêcheur…).
    /// </summary>
    public static class IdleMath
    {
        public const float PlayerDelay = 6f;
        public const float PlayerCycle = 12f;
        public const float PetDelay = 4f;

        /// <summary>
        /// Pose d'attente du pêcheur après <paramref name="idleSeconds"/> sans bouger : un numéro de
        /// <see cref="CharacterPainter.IdleLookLeft"/> à <see cref="CharacterPainter.IdleTap"/>, ou -1 (pose normale).
        /// </summary>
        public static int PlayerPose(float idleSeconds)
        {
            if (idleSeconds < PlayerDelay) return -1;
            float t = Mathf.Repeat(idleSeconds - PlayerDelay, PlayerCycle);
            if (t < 0.8f) return CharacterPainter.IdleLookLeft;
            if (t < 1.6f) return CharacterPainter.IdleLookRight;
            if (t < 2.2f) return -1;
            if (t < 4f) return Mathf.FloorToInt((t - 2.2f) / 0.3f) % 2 == 0 ? CharacterPainter.IdleTap : -1;   // tape du pied
            if (t < 6.5f) return -1;
            if (t < 8.3f) return CharacterPainter.IdleStretch;                                                 // s'étire en bâillant
            return -1;
        }

        /// <summary>L'animal est-il dans son attente (assez longtemps immobile) ?</summary>
        public static bool PetIdle(float idleSeconds) => idleSeconds >= PetDelay;

        /// <summary>Image de l'attente de l'animal (0 ou 1) : la pose, et de temps en temps le geste (lécher, se gratter…).</summary>
        public static int PetFrame(float idleSeconds)
        {
            if (idleSeconds < PetDelay) return 0;
            float t = Mathf.Repeat(idleSeconds - PetDelay, 4f);
            // La première seconde il prend la pose ; ensuite le geste revient trois fois par cycle, en saccades.
            return t > 1f && Mathf.FloorToInt(t * 3f) % 2 == 0 ? 1 : 0;
        }
    }
}
