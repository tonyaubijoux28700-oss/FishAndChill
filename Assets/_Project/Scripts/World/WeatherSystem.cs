using System;
using FishAndChill.Fishing;
using FishAndChill.Save;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// La météo de la zone où se trouve le joueur : un temps tiré selon le climat de la zone (<see cref="WeatherClimate"/>),
    /// qui dure quelques heures de jeu puis change ; un voyage tire le temps de la nouvelle zone. Donne au ciel sa teinte
    /// (fondu doux d'un temps à l'autre) et, pendant un orage, des éclairs. Les effets (pluie, brume) sont dans
    /// <see cref="WeatherEffects"/>, les règles dans <see cref="WeatherMath"/>. Une nouvelle partie commence au beau temps.
    /// </summary>
    public sealed class WeatherSystem : MonoBehaviour
    {
        [SerializeField] private DayNightCycle _cycle;
        [SerializeField] private FishingController _fishing;

        [SerializeField, Tooltip("Tempête des boss : ciel noir, déluge et foudre près du remous d'un boss réveillé.")]
        private LegendStorm _legendStorm;

        [SerializeField, Min(0.1f), Tooltip("Durée du fondu d'un temps à l'autre, en secondes.")]
        private float _fadeSeconds = 6f;

        private System.Random _random;
        private float _lastHour = -1f;
        private Color _tint = Color.white;
        private float _lightningTimer;
        private float _flashAge = 10f;
        private bool _running;

        /// <summary>Le temps qu'il fait.</summary>
        public Weather Current { get; private set; } = Weather.Clear;

        /// <summary>Heures de jeu avant le prochain changement de temps.</summary>
        public float HoursLeft { get; private set; } = WeatherMath.MaxHours;

        /// <summary>Teinte du ciel (multipliée à celle de l'heure), en fondu d'un temps à l'autre.</summary>
        public Color Tint => _tint;

        /// <summary>Éclair d'orage, de 0 (rien) à 1 (plein flash).</summary>
        public float Flash { get; private set; }

        /// <summary>Part de pluie (0 à 1) et de brouillard (0 à 1), en fondu.</summary>
        public float Rain { get; private set; }
        public float Fog { get; private set; }

        /// <summary>Le temps vient de changer.</summary>
        public event Action<Weather> Changed;

        /// <summary>Intensité de la tempête des boss (0 à 1).</summary>
        public float Epic => _legendStorm != null ? _legendStorm.Intensity : 0f;

        /// <summary>Cœur de la tempête des boss (le remous).</summary>
        public Vector2 StormCenter => _legendStorm != null ? _legendStorm.Center : Vector2.zero;

        /// <summary>Nom affiché : « Tempête » quand la tempête d'un boss gronde, sinon le temps qu'il fait.</summary>
        public string DisplayName => Epic > 0.45f ? "Tempête" : WeatherMath.Name(Current);

        /// <summary>Un éclair vient de frapper ; donne l'intensité de la tempête des boss à ce moment (0 pour un orage ordinaire).</summary>
        public event Action<float> Lightning;

        private void Awake()
        {
            _random = new System.Random();
            _lightningTimer = NextLightning();
        }

        private void OnEnable()
        {
            if (_fishing != null) _fishing.ZoneChanged += OnZoneChanged;
        }

        private void OnDisable()
        {
            if (_fishing != null) _fishing.ZoneChanged -= OnZoneChanged;
        }

        /// <summary>Remet le temps d'une sauvegarde (ou d'un test).</summary>
        public void SetWeather(Weather weather, float hoursLeft)
        {
            Current = weather;
            HoursLeft = Mathf.Max(0.5f, hoursLeft);
            SnapVisuals();
            Changed?.Invoke(Current);
        }

        /// <summary>Tire un nouveau temps pour la zone actuelle.</summary>
        public void Reroll()
        {
            FishingZone zone = _fishing != null ? _fishing.Zone : null;
            Weather next = WeatherMath.Pick(_random, zone != null ? zone.Climate : null);
            Current = next;
            HoursLeft = WeatherMath.Duration(_random);
            Changed?.Invoke(Current);
        }

        private void OnZoneChanged()
        {
            // Au chargement, ZoneTravel remet la zone sauvegardée après la lecture de la sauvegarde : on garde alors le
            // temps sauvegardé. Seuls les vrais voyages, une fois la partie lancée, changent le temps.
            if (SaveManager.InGame && _running)
            {
                Reroll();
                SnapVisuals();
            }
        }

        private void SnapVisuals()
        {
            _tint = WeatherMath.SkyTint(Current) * ZoneAmbient;
            Rain = WeatherMath.RainAmount(Current);
            Fog = Mathf.Max(WeatherMath.FogAmount(Current), ZoneFog);
        }

        private FishingZone CurrentZone => _fishing != null ? _fishing.Zone : null;

        /// <summary>Lumière propre à la zone (blanc ailleurs).</summary>
        private Color ZoneAmbient => CurrentZone != null ? CurrentZone.Ambient : Color.white;

        /// <summary>Brume toujours présente dans la zone.</summary>
        private float ZoneFog => CurrentZone != null ? CurrentZone.MinimumFog : 0f;

        /// <summary>À quel point la zone actuelle est sombre (0 = normale) : les lanternes s'y allument plus tôt.</summary>
        public float Gloom => CurrentZone != null ? CurrentZone.Gloom : 0f;

        private void Update()
        {
            if (!SaveManager.InGame)
            {
                return;
            }

            _running = true;

            // Le temps passe avec l'horloge du jeu (il s'arrête en pause comme elle).
            if (_cycle != null)
            {
                float hour = _cycle.Hour;
                if (_lastHour >= 0f)
                {
                    float elapsed = Mathf.Repeat(hour - _lastHour, 24f);
                    if (elapsed < 2f)
                    {
                        HoursLeft -= elapsed;
                    }
                }

                _lastHour = hour;
                if (HoursLeft <= 0f)
                {
                    Reroll();
                }
            }

            float dt = Time.deltaTime;
            float k = dt <= 0f ? 0f : 1f - Mathf.Exp(-dt * 3f / _fadeSeconds);
            float epic = Epic;
            Color targetTint = Color.Lerp(WeatherMath.SkyTint(Current) * ZoneAmbient, LegendStormMath.Tint, epic);
            _tint = epic > 0.01f ? targetTint : Color.Lerp(_tint, targetTint, k);   // la tempête a déjà son propre fondu
            Rain = Mathf.Max(Mathf.Lerp(Rain, WeatherMath.RainAmount(Current), k), epic);
            Fog = Mathf.Max(Mathf.Lerp(Fog, Mathf.Max(WeatherMath.FogAmount(Current), ZoneFog), k), epic * 0.3f);

            // Éclairs d'orage : un double flash de temps en temps.
            _flashAge += dt;
            if ((Current == Weather.Storm || epic > 0.15f) && InteriorBuilding.Occupied == null)
            {
                _lightningTimer = Mathf.Min(_lightningTimer, LegendStormMath.LightningInterval(epic, 1f)) - dt;
                if (_lightningTimer <= 0f)
                {
                    _lightningTimer = LegendStormMath.LightningInterval(epic, (float)_random.NextDouble());
                    _flashAge = 0f;
                    Lightning?.Invoke(epic);
                }
            }

            Flash = FlashAt(_flashAge);
        }

        private float NextLightning() => LegendStormMath.LightningInterval(0f, (float)(_random?.NextDouble() ?? 0.5));

        /// <summary>Deux éclats rapprochés qui s'éteignent vite.</summary>
        private static float FlashAt(float age)
        {
            if (age < 0.12f) return 1f - age / 0.12f * 0.6f;
            if (age < 0.2f) return 0.15f;
            if (age < 0.55f) return 0.8f * (1f - (age - 0.2f) / 0.35f);
            return 0f;
        }
    }
}
