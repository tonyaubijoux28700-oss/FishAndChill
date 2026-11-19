using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Règles de la météo (logique pure, testée) : tirage du temps selon le climat de la zone, durée d'un temps, teinte
    /// du ciel, effet sur les touches et espèces qui ne mordent que par un certain temps.
    /// </summary>
    public static class WeatherMath
    {
        public static readonly Weather[] All = { Weather.Clear, Weather.Cloudy, Weather.Rain, Weather.Storm, Weather.Fog };

        /// <summary>Durée d'un temps, en heures de jeu (une heure de jeu = 35 s).</summary>
        public const float MinHours = 3f;
        public const float MaxHours = 7f;

        /// <summary>Tire un temps selon le climat (beau temps si le climat est vide).</summary>
        public static Weather Pick(System.Random random, WeatherClimate climate)
        {
            if (climate == null)
            {
                return Weather.Clear;
            }

            float total = 0f;
            foreach (Weather w in All) total += Mathf.Max(0f, climate.Weight(w));
            if (total <= 0f)
            {
                return Weather.Clear;
            }

            float roll = (float)random.NextDouble() * total;
            foreach (Weather w in All)
            {
                roll -= Mathf.Max(0f, climate.Weight(w));
                if (roll < 0f) return w;
            }

            return Weather.Clear;
        }

        public static float Duration(System.Random random) => Mathf.Lerp(MinHours, MaxHours, (float)random.NextDouble());

        /// <summary>Teinte multipliée à la lumière du ciel : grise et plus sombre quand le temps se gâte.</summary>
        public static Color SkyTint(Weather weather)
        {
            switch (weather)
            {
                case Weather.Cloudy: return new Color(0.86f, 0.88f, 0.92f);
                case Weather.Rain: return new Color(0.70f, 0.74f, 0.82f);
                case Weather.Storm: return new Color(0.52f, 0.55f, 0.68f);
                case Weather.Fog: return new Color(0.88f, 0.90f, 0.93f);
                default: return Color.white;
            }
        }

        /// <summary>Facteur sur l'attente avant la touche : les poissons mordent plus vite quand l'eau est agitée.</summary>
        public static float BiteWaitFactor(Weather weather)
        {
            switch (weather)
            {
                case Weather.Cloudy: return 0.92f;
                case Weather.Rain: return 0.82f;
                case Weather.Storm: return 0.75f;
                case Weather.Fog: return 0.95f;
                default: return 1f;
            }
        }

        /// <summary>Intensité de la pluie (0 à 1) pour ce temps.</summary>
        public static float RainAmount(Weather weather) => weather == Weather.Storm ? 1f : weather == Weather.Rain ? 0.55f : 0f;

        /// <summary>Épaisseur du brouillard (0 à 1) pour ce temps.</summary>
        public static float FogAmount(Weather weather) => weather == Weather.Fog ? 1f : weather == Weather.Rain ? 0.2f : weather == Weather.Storm ? 0.25f : 0f;

        /// <summary>Une espèce qui demande ce temps peut-elle mordre maintenant ?</summary>
        public static bool Allows(WeatherNeed need, Weather weather)
        {
            switch (need)
            {
                case WeatherNeed.Rain: return weather == Weather.Rain || weather == Weather.Storm;
                case WeatherNeed.Storm: return weather == Weather.Storm;
                case WeatherNeed.Fog: return weather == Weather.Fog;
                case WeatherNeed.Clear: return weather == Weather.Clear;
                default: return true;
            }
        }

        public static string Name(Weather weather)
        {
            switch (weather)
            {
                case Weather.Cloudy: return "Nuageux";
                case Weather.Rain: return "Pluie";
                case Weather.Storm: return "Orage";
                case Weather.Fog: return "Brouillard";
                default: return "Beau temps";
            }
        }

        /// <summary>Pour la fiche d'une espèce : « sous la pluie », « par temps d'orage »… (vide par tous les temps).</summary>
        public static string NeedText(WeatherNeed need)
        {
            switch (need)
            {
                case WeatherNeed.Rain: return "sous la pluie";
                case WeatherNeed.Storm: return "par temps d'orage";
                case WeatherNeed.Fog: return "dans le brouillard";
                case WeatherNeed.Clear: return "par beau temps";
                default: return string.Empty;
            }
        }
    }
}
