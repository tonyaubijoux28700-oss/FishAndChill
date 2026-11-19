using System;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>Le climat d'une zone : la fréquence relative de chaque temps (40 sort deux fois plus souvent que 20).</summary>
    [Serializable]
    public sealed class WeatherClimate
    {
        [SerializeField, Min(0f)] private float _clear = 50f;
        [SerializeField, Min(0f)] private float _cloudy = 25f;
        [SerializeField, Min(0f)] private float _rain = 15f;
        [SerializeField, Min(0f)] private float _storm = 5f;
        [SerializeField, Min(0f)] private float _fog = 5f;

        public WeatherClimate()
        {
        }

        public WeatherClimate(float clear, float cloudy, float rain, float storm, float fog)
        {
            _clear = clear;
            _cloudy = cloudy;
            _rain = rain;
            _storm = storm;
            _fog = fog;
        }

        public float Weight(Weather weather)
        {
            switch (weather)
            {
                case Weather.Cloudy: return _cloudy;
                case Weather.Rain: return _rain;
                case Weather.Storm: return _storm;
                case Weather.Fog: return _fog;
                default: return _clear;
            }
        }
    }
}
