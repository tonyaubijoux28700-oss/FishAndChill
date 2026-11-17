namespace FishAndChill.World
{
    /// <summary>Quand peut-on dormir dans son lit, et à quelle heure se réveille-t-on (logique pure, testée).</summary>
    public static class SleepRules
    {
        /// <summary>On peut se coucher à partir de cette heure le soir…</summary>
        public const float BedTime = 18f;

        /// <summary>… et jusqu'à cette heure du matin.</summary>
        public const float LateNight = 5f;

        /// <summary>Heure du réveil.</summary>
        public const float WakeHour = 7f;

        public static bool CanSleep(float hour) => hour >= BedTime || hour < LateNight;

        /// <summary>Heures passées au lit (de l'heure du coucher jusqu'au réveil).</summary>
        public static float HoursSlept(float hour)
        {
            float wake = hour < WakeHour ? WakeHour : WakeHour + 24f;
            return wake - hour;
        }
    }
}
