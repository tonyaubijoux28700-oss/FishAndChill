using UnityEngine;

namespace FishAndChill.Villagers
{
    /// <summary>Emploi du temps et balade des habitants (logique pure, testée).</summary>
    public static class VillagerSchedule
    {
        /// <summary>Dehors entre l'heure de sortie (comprise) et l'heure du retour (exclue), sur une journée de 24 h.</summary>
        public static bool IsOutside(float hour, float leaveHour, float homeHour)
        {
            hour = Mathf.Repeat(hour, 24f);
            return leaveHour <= homeHour
                ? hour >= leaveHour && hour < homeHour
                : hour >= leaveHour || hour < homeHour;   // sortie le soir, retour le lendemain
        }

        /// <summary>Prochain but de balade : un point au hasard dans le disque autour du coin préféré.</summary>
        public static Vector2 WanderTarget(System.Random random, Vector2 anchor, float radius)
        {
            double angle = random.NextDouble() * System.Math.PI * 2.0;
            double distance = System.Math.Sqrt(random.NextDouble()) * radius;
            return anchor + new Vector2((float)(System.Math.Cos(angle) * distance), (float)(System.Math.Sin(angle) * distance));
        }

        /// <summary>Temps d'arrêt entre deux balades, en secondes (entre <paramref name="min"/> et <paramref name="max"/>).</summary>
        public static float PauseSeconds(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * Mathf.Max(0f, max - min);
        }
    }
}
