namespace FishAndChill.World
{
    /// <summary>
    /// Le lit (logique pure, testée) : la nuit (de 18 h à 7 h) on dort jusqu'à 7 h du matin ; le jour on fait une sieste
    /// jusqu'à 18 h, pour aller pêcher les espèces de nuit.
    /// </summary>
    public static class SleepRules
    {
        /// <summary>La nuit commence à cette heure…</summary>
        public const float BedTime = 18f;

        /// <summary>… et finit à l'heure du réveil.</summary>
        public const float WakeHour = 7f;

        /// <summary>Est-ce la nuit (dormir jusqu'au matin) plutôt que le jour (sieste jusqu'au soir) ?</summary>
        public static bool IsNight(float hour) => hour >= BedTime || hour < WakeHour;

        /// <summary>Heure à laquelle on se réveille si l'on se couche à <paramref name="hour"/>.</summary>
        public static float WakeFor(float hour) => IsNight(hour) ? WakeHour : BedTime;

        /// <summary>Heures passées au lit (de l'heure du coucher jusqu'au réveil, le lendemain si besoin).</summary>
        public static float HoursSlept(float hour)
        {
            float wake = WakeFor(hour);
            return wake > hour ? wake - hour : wake + 24f - hour;
        }

        /// <summary>Heure affichée pendant le sommeil, après <paramref name="progress"/> (0 à 1) du temps passé au lit.</summary>
        public static float HourDuring(float bedHour, float progress)
        {
            float hour = bedHour + HoursSlept(bedHour) * (progress < 0f ? 0f : progress > 1f ? 1f : progress);
            return hour >= 24f ? hour - 24f : hour;
        }
    }
}
