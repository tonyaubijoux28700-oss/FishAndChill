using System.Collections.Generic;
using FishAndChill.Save;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace FishAndChill.World
{
    /// <summary>
    /// Cycle jour/nuit : l'horloge avance pendant la partie (pas sur l'écran titre ni en pause), la lumière globale 2D
    /// prend la teinte de l'heure (<see cref="DayCycleMath.SkyColor"/>) et, la nuit, les lampadaires, les façades des
    /// bâtiments et les lanternes des pontons s'allument (lumières 2D ponctuelles qui vacillent un peu).
    /// Dans un intérieur, la lumière reste celle d'une pièce éclairée.
    /// </summary>
    public sealed class DayNightCycle : MonoBehaviour
    {
        [SerializeField, Tooltip("Lumière globale 2D de la scène.")]
        private Light2D _globalLight;

        [SerializeField, Tooltip("Tilemap du décor : chaque tuile dont le nom contient « Lampadaire » reçoit une lumière.")]
        private Tilemap _decor;

        [SerializeField] private string _lampTileName = "Lampadaire";

        [SerializeField, Tooltip("Hauteur de l'ampoule au-dessus du centre de la case du lampadaire.")]
        private float _lampHeight = 0.9f;

        [SerializeField, Tooltip("Lanternes en plus (bout des pontons), en coordonnées du monde.")]
        private List<Vector2> _lanterns = new List<Vector2>();

        [SerializeField, Min(10f), Tooltip("Durée d'une journée entière en secondes réelles.")]
        private float _secondsPerDay = 840f;

        [SerializeField, Range(0f, 24f), Tooltip("Heure d'une nouvelle partie.")]
        private float _startHour = 8f;

        [SerializeField] private Color _lampColor = new Color(1f, 0.78f, 0.42f);
        [SerializeField] private Color _indoorColor = new Color(1f, 0.97f, 0.92f);

        [SerializeField, Tooltip("Météo : assombrit et grise le ciel, éclairs d'orage.")]
        private WeatherSystem _weather;

        private float _baseGlobalIntensity = -1f;

        private readonly List<Light2D> _lights = new List<Light2D>();
        private readonly List<float> _baseIntensity = new List<float>();
        private float _hour = -1f;

        /// <summary>Heure actuelle, de 0 à 24.</summary>
        public float Hour => _hour;

        private double _totalHours = -1.0;

        /// <summary>Heures de jeu écoulées depuis le début de la partie (sommeil compris) : l'attente des casiers.</summary>
        public double TotalHours => _totalHours >= 0.0 ? _totalHours : Mathf.Max(0f, _hour);

        /// <summary>Reprend le compteur d'heures de la sauvegarde.</summary>
        public void RestoreTotalHours(double totalHours) => _totalHours = System.Math.Max(0.0, totalHours);

        private void Awake()
        {
            if (_globalLight == null)
            {
                foreach (Light2D light in FindObjectsByType<Light2D>())
                {
                    if (light.lightType == Light2D.LightType.Global)
                    {
                        _globalLight = light;
                        break;
                    }
                }
            }

            if (_hour < 0f)
            {
                _hour = _startHour; // pas encore réglée par la sauvegarde
            }
        }

        /// <summary>Règle l'heure (reprise d'une sauvegarde, tests).</summary>
        public void SetHour(float hour)
        {
            float wrapped = DayCycleMath.Wrap(hour);
            if (_hour >= 0f)
            {
                _totalHours = TotalHours + Mathf.Repeat(wrapped - _hour, 24f); // le temps avance toujours (sommeil)
            }

            _hour = wrapped;
            Apply();
        }

        private void Start()
        {
            if (_decor != null)
            {
                BoundsInt bounds = _decor.cellBounds;
                foreach (Vector3Int cell in bounds.allPositionsWithin)
                {
                    TileBase tile = _decor.GetTile(cell);
                    if (tile != null && tile.name.Contains(_lampTileName))
                    {
                        Vector3 center = _decor.GetCellCenterWorld(cell);
                        AddLight("Lumière du lampadaire", (Vector2)center + new Vector2(0f, _lampHeight), 4.8f, 1.4f);
                    }
                }
            }

            // Fenêtres des bâtiments : une lueur chaude devant la façade.
            foreach (InteriorBuilding building in FindObjectsByType<InteriorBuilding>())
            {
                AddLight("Lueur des fenêtres", (Vector2)building.transform.position + new Vector2(0f, 1.5f), 6f, 0.8f);
            }

            foreach (Vector2 lantern in _lanterns)
            {
                AddLight("Lanterne du ponton", lantern, 3.5f, 1.1f);
            }

            Apply();
        }

        private void AddLight(string name, Vector2 position, float radius, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = _lampColor;
            light.pointLightInnerRadius = radius * 0.15f;
            light.pointLightOuterRadius = radius;
            light.falloffIntensity = 0.6f;
            light.intensity = 0f;
            _lights.Add(light);
            _baseIntensity.Add(intensity);
        }

        private void Update()
        {
            if (!SaveManager.InGame)
            {
                return;
            }

            // Time.deltaTime : l'heure s'arrête pendant la pause (timeScale = 0).
            float before = _hour;
            _hour = DayCycleMath.Advance(_hour, Time.deltaTime, _secondsPerDay);
            _totalHours = TotalHours + Mathf.Repeat(_hour - before, 24f);
            Apply();
        }

        private void Apply()
        {
            bool indoors = InteriorBuilding.Occupied != null;
            if (_globalLight != null)
            {
                if (_baseGlobalIntensity < 0f) _baseGlobalIntensity = _globalLight.intensity;
                Color sky = DayCycleMath.SkyColor(_hour);
                float flash = 0f;
                if (_weather != null && !indoors)
                {
                    sky *= _weather.Tint;
                    flash = _weather.Flash;
                }

                _globalLight.color = indoors ? _indoorColor : Color.Lerp(sky, new Color(0.92f, 0.95f, 1f), flash * 0.85f);
                _globalLight.intensity = _baseGlobalIntensity * (1f + flash * 0.9f);
            }

            // La tempête d'un boss plonge le monde dans le noir : les lanternes s'allument même en plein jour.
            float level = indoors ? 0f : Mathf.Max(DayCycleMath.LampLevel(_hour), _weather != null ? _weather.Epic * 0.8f : 0f);
            for (int i = 0; i < _lights.Count; i++)
            {
                // Léger vacillement, décalé d'une lumière à l'autre.
                float flicker = 1f + Mathf.Sin(Time.time * 7f + i * 1.7f) * 0.04f + Mathf.Sin(Time.time * 13f + i) * 0.03f;
                _lights[i].intensity = _baseIntensity[i] * level * flicker;
                _lights[i].enabled = level > 0.001f;
            }
        }
    }
}
