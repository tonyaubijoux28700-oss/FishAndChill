using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Calculs du cycle jour/nuit (logique pure, testée) : avance de l'horloge, couleur de la lumière du ciel selon
    /// l'heure (nuit bleutée, aube rosée, plein jour blanc, crépuscule orangé), allumage des lampadaires et heures où
    /// mord chaque espèce.
    /// </summary>
    public static class DayCycleMath
    {
        /// <summary>Le jour (pour les espèces) va de 6 h à 20 h ; la nuit de 19 h à 7 h (les crépuscules comptent pour les deux).</summary>
        public const float DayStart = 6f;
        public const float DayEnd = 20f;
        public const float NightStart = 19f;
        public const float NightEnd = 7f;

        private static readonly (float hour, Color color)[] SkyKeys =
        {
            (0f, new Color(0.07f, 0.10f, 0.30f)),
            (4.5f, new Color(0.07f, 0.10f, 0.30f)),
            (6f, new Color(0.92f, 0.70f, 0.66f)),
            (7.5f, new Color(1.00f, 0.96f, 0.90f)),
            (9f, Color.white),
            (17f, Color.white),
            (18.5f, new Color(1.00f, 0.80f, 0.58f)),
            (19.5f, new Color(0.72f, 0.46f, 0.50f)),
            (21f, new Color(0.07f, 0.10f, 0.30f)),
            (24f, new Color(0.07f, 0.10f, 0.30f)),
        };

        /// <summary>Ramène une heure entre 0 et 24.</summary>
        public static float Wrap(float hour) => Mathf.Repeat(hour, 24f);

        /// <summary>Avance l'horloge : une journée entière dure <paramref name="secondsPerDay"/> secondes réelles.</summary>
        public static float Advance(float hour, float deltaSeconds, float secondsPerDay)
        {
            return secondsPerDay <= 0f ? Wrap(hour) : Wrap(hour + deltaSeconds * 24f / secondsPerDay);
        }

        /// <summary>Couleur de la lumière globale à cette heure (interpolée entre les teintes clés).</summary>
        public static Color SkyColor(float hour)
        {
            hour = Wrap(hour);
            for (int i = 1; i < SkyKeys.Length; i++)
            {
                if (hour <= SkyKeys[i].hour)
                {
                    (float h0, Color c0) = SkyKeys[i - 1];
                    (float h1, Color c1) = SkyKeys[i];
                    return Color.Lerp(c0, c1, Mathf.InverseLerp(h0, h1, hour));
                }
            }

            return SkyKeys[SkyKeys.Length - 1].color;
        }

        /// <summary>Lampadaires : 0 le jour, 1 la nuit, allumage progressif de 18 h à 19 h 30 et extinction de 5 h 30 à 7 h.</summary>
        public static float LampLevel(float hour)
        {
            hour = Wrap(hour);
            if (hour >= 18f)
            {
                return Mathf.Clamp01((hour - 18f) / 1.5f);
            }

            if (hour >= 7f)
            {
                return 0f;
            }

            return Mathf.Clamp01((7f - hour) / 1.5f);
        }

        /// <summary>Cette espèce mord-elle à cette heure ?</summary>
        public static bool IsActive(ActiveTime time, float hour)
        {
            hour = Wrap(hour);
            switch (time)
            {
                case ActiveTime.Day: return hour >= DayStart && hour < DayEnd;
                case ActiveTime.Night: return hour >= NightStart || hour < NightEnd;
                default: return true;
            }
        }

        /// <summary>Heure affichée : « 7h05 », « 21h30 ».</summary>
        public static string TimeText(float hour)
        {
            int minutes = Mathf.FloorToInt(Wrap(hour) * 60f);
            return $"{minutes / 60}h{minutes % 60:00}";
        }

        /// <summary>Texte de la fiche de l'index : vide pour une espèce qui mord à toute heure.</summary>
        public static string ActiveText(ActiveTime time)
        {
            switch (time)
            {
                case ActiveTime.Day: return "Mord le jour";
                case ActiveTime.Night: return "Mord la nuit";
                default: return string.Empty;
            }
        }
    }
}
