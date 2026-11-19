using System.Collections.Generic;
using FishAndChill.Save;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Ce qu'on voit du temps qu'il fait, autour de la caméra : gouttes de pluie qui tombent en biais et éclatent en petits
    /// ronds, nappes de brume qui dérivent avec le vent. Quantités en fondu (<see cref="WeatherSystem.Rain"/>,
    /// <see cref="WeatherSystem.Fog"/>). Rien dans les bâtiments ni sur l'écran titre. Sprites éclairés : la nuit les
    /// assombrit comme le reste du monde. Tempête des boss (<see cref="WeatherSystem.Epic"/>) : déluge couché par le vent,
    /// vraie foudre dessinée qui frappe souvent le remous, lueur à l'impact et secousse de la caméra.
    /// </summary>
    public sealed class WeatherEffects : MonoBehaviour
    {
        [SerializeField] private WeatherSystem _weather;
        [SerializeField] private Sprite _drop;
        [SerializeField, Tooltip("Éclaboussure d'une goutte (images dans l'ordre).")] private Sprite[] _splash = new Sprite[0];
        [SerializeField] private Sprite _fog;
        [SerializeField, Tooltip("Éclairs de la tempête des boss (pied de l'éclair = point d'impact).")] private Sprite[] _bolts = new Sprite[0];
        [SerializeField, Tooltip("Lueur au point d'impact de la foudre (images dans l'ordre).")] private Sprite[] _impact = new Sprite[0];
        [SerializeField, Tooltip("Matériau non éclairé : la foudre brille même dans le noir.")] private Material _glowMaterial;
        [SerializeField] private CameraFollow _cameraFollow;
        [SerializeField, Min(0), Tooltip("Gouttes en plus au comble de la tempête d'un boss.")] private int _stormDrops = 170;
        [SerializeField, Min(0)] private int _maxDrops = 170;
        [SerializeField, Min(0)] private int _fogPuffs = 10;
        [SerializeField, Tooltip("Au-dessus de tout le monde du jeu.")] private int _sortingOrder = 400;
        [SerializeField] private Vector2 _rainVelocity = new Vector2(-3.2f, -17f);
        [SerializeField] private Vector2 _wind = new Vector2(0.35f, 0.05f);

        private sealed class Drop
        {
            public SpriteRenderer Renderer;
            public float Life;
        }

        private sealed class Splash
        {
            public SpriteRenderer Renderer;
            public float Age = 99f;
        }

        private sealed class Puff
        {
            public SpriteRenderer Renderer;
            public Vector2 Offset;
            public float Phase;
        }

        private readonly List<Drop> _drops = new List<Drop>();
        private readonly List<Splash> _splashes = new List<Splash>();
        private readonly List<Puff> _puffs = new List<Puff>();
        private System.Random _random;
        private Camera _camera;
        private int _nextSplash;
        private bool _shown = true;
        private SpriteRenderer _bolt;
        private SpriteRenderer _boltImpact;
        private float _boltAge = 99f;
        private float _boltStrength;

        private const float BoltSeconds = 0.45f;
        private static readonly Vector2 StormVelocity = new Vector2(-10f, -23f);
        private Vector2 _velocity = new Vector2(-3.2f, -17f);

        private const float SplashSeconds = 0.24f;

        private void Start()
        {
            _random = new System.Random(4242);
            _camera = Camera.main;
            for (int i = 0; i < _maxDrops + _stormDrops; i++)
            {
                _drops.Add(new Drop { Renderer = Make("Goutte", _drop, _sortingOrder) });
            }

            for (int i = 0; i < 70; i++)
            {
                _splashes.Add(new Splash { Renderer = Make("Éclaboussure", null, _sortingOrder - 1) });
            }

            for (int i = 0; i < _fogPuffs; i++)
            {
                _puffs.Add(new Puff
                {
                    Renderer = Make("Brume", _fog, _sortingOrder + 1),
                    Offset = new Vector2(Range(-1f, 1f), Range(-1f, 1f)),
                    Phase = Range(0f, 10f),
                });
            }

            _bolt = Make("Foudre", null, _sortingOrder + 3);
            _boltImpact = Make("Impact de la foudre", null, _sortingOrder + 2);
            if (_glowMaterial != null)
            {
                _bolt.sharedMaterial = _glowMaterial;
                _boltImpact.sharedMaterial = _glowMaterial;
            }

            if (_weather != null) _weather.Lightning += OnLightning;
            Show(false);
        }

        private SpriteRenderer Make(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;
            r.enabled = false;
            return r;
        }

        private void OnDestroy()
        {
            if (_weather != null) _weather.Lightning -= OnLightning;
        }

        private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);

        private void Show(bool on)
        {
            if (_shown == on) return;
            _shown = on;
            if (on) return;
            foreach (Drop d in _drops) d.Renderer.enabled = false;
            foreach (Splash s in _splashes) s.Renderer.enabled = false;
            foreach (Puff p in _puffs) p.Renderer.enabled = false;
            if (_bolt != null) _bolt.enabled = false;
            if (_boltImpact != null) _boltImpact.enabled = false;
        }

        private void Update()
        {
            if (_camera == null) _camera = Camera.main;
            bool visible = _weather != null && _camera != null && SaveManager.InGame && InteriorBuilding.Occupied == null;
            Show(visible);
            if (!visible)
            {
                return;
            }

            float dt = Time.deltaTime;
            Vector2 center = _camera.transform.position;
            float halfH = _camera.orthographicSize + 1f;
            float halfW = halfH * _camera.aspect + 1f;
            UpdateRain(dt, center, halfW, halfH);
            UpdateSplashes(dt);
            UpdateFog(dt, center, halfW, halfH);
            UpdateBolt(dt);
        }

        private void UpdateRain(float dt, Vector2 center, float halfW, float halfH)
        {
            float epic = _weather.Epic;
            int wanted = Mathf.RoundToInt(_maxDrops * Mathf.Clamp01(_weather.Rain) + _stormDrops * epic);
            // Le vent de la tempête couche la pluie.
            _velocity = Vector2.Lerp(_rainVelocity, StormVelocity, epic);
            // Le dessin de la goutte est déjà un peu en biais (pluie ordinaire) : on ne tourne que du surplus.
            Quaternion slant = Quaternion.Euler(0f, 0f, (Mathf.Atan2(_velocity.x, -_velocity.y) - Mathf.Atan2(_rainVelocity.x, -_rainVelocity.y)) * Mathf.Rad2Deg);
            Color color = new Color(0.82f, 0.90f, 1f, _weather.Current == Weather.Storm || epic > 0.3f ? 0.75f : 0.6f);
            for (int i = 0; i < _drops.Count; i++)
            {
                Drop d = _drops[i];
                SpriteRenderer r = d.Renderer;
                if (i >= wanted)
                {
                    // La goutte finit sa chute avant de disparaître : la pluie s'arrête doucement.
                    if (!r.enabled) continue;
                }
                else if (!r.enabled)
                {
                    Respawn(d, center, halfW, halfH);
                }

                r.color = color;
                r.transform.rotation = slant;
                r.transform.position += (Vector3)(_velocity * dt);
                d.Life -= dt;
                if (d.Life <= 0f)
                {
                    SpawnSplash(r.transform.position);
                    if (i < wanted) Respawn(d, center, halfW, halfH);
                    else r.enabled = false;
                }
            }
        }

        private void Respawn(Drop d, Vector2 center, float halfW, float halfH)
        {
            d.Life = Range(0.25f, 0.8f);
            // Elle part assez haut pour finir sa chute dans le champ de la caméra.
            Vector2 end = center + new Vector2(Range(-halfW, halfW), Range(-halfH, halfH));
            d.Renderer.transform.position = end - _velocity * d.Life;
            d.Renderer.enabled = true;
        }

        private void SpawnSplash(Vector2 position)
        {
            if (_splash.Length == 0 || _splashes.Count == 0) return;
            Splash s = _splashes[_nextSplash];
            _nextSplash = (_nextSplash + 1) % _splashes.Count;
            s.Age = 0f;
            s.Renderer.transform.position = position;
            s.Renderer.enabled = true;
        }

        private void UpdateSplashes(float dt)
        {
            foreach (Splash s in _splashes)
            {
                if (!s.Renderer.enabled) continue;
                s.Age += dt;
                if (s.Age >= SplashSeconds)
                {
                    s.Renderer.enabled = false;
                    continue;
                }

                int frame = Mathf.Min(_splash.Length - 1, (int)(s.Age / SplashSeconds * _splash.Length));
                s.Renderer.sprite = _splash[frame];
                s.Renderer.color = new Color(0.86f, 0.93f, 1f, 0.7f * (1f - s.Age / SplashSeconds));
            }
        }

        /// <summary>La foudre tombe pour de bon (tempête d'un boss) : souvent sur le remous, sinon ailleurs dans le champ.</summary>
        private void OnLightning(float epic)
        {
            if (!_shown || _camera == null || _bolts.Length == 0 || !LegendStormMath.StrikesBolt(epic))
            {
                return;
            }

            Vector2 center = _camera.transform.position;
            float halfH = _camera.orthographicSize;
            float halfW = halfH * _camera.aspect;
            Vector2 heart = _weather.StormCenter;
            bool heartVisible = Mathf.Abs(heart.x - center.x) < halfW - 1f && Mathf.Abs(heart.y - center.y) < halfH - 1f;
            Vector2 strike = heartVisible && _random.NextDouble() < 0.6
                ? heart + new Vector2(Range(-2.5f, 2.5f), Range(-1.5f, 1.5f))
                : center + new Vector2(Range(-halfW * 0.85f, halfW * 0.85f), Range(-halfH * 0.8f, halfH * 0.3f));

            _bolt.sprite = _bolts[_random.Next(_bolts.Length)];
            _bolt.flipX = _random.NextDouble() < 0.5;
            _bolt.transform.position = strike;
            _boltImpact.transform.position = strike;
            _boltAge = 0f;
            _boltStrength = epic;
            if (_cameraFollow != null)
            {
                _cameraFollow.Shake(0.10f + 0.18f * epic, 0.5f);
            }
        }

        private void UpdateBolt(float dt)
        {
            _boltAge += dt;
            bool on = _boltAge < BoltSeconds;
            _bolt.enabled = on;
            _boltImpact.enabled = on && _impact.Length > 0;
            if (!on)
            {
                return;
            }

            // L'éclair claque, s'éteint, revient, puis s'efface.
            float a = _boltAge < 0.07f ? 1f : _boltAge < 0.13f ? 0.25f : _boltAge < 0.22f ? 0.95f : 1f - (_boltAge - 0.22f) / (BoltSeconds - 0.22f);
            _bolt.color = new Color(0.92f, 0.96f, 1f, a * Mathf.Lerp(0.7f, 1f, _boltStrength));
            if (_impact.Length > 0)
            {
                int frame = Mathf.Min(_impact.Length - 1, (int)(_boltAge / BoltSeconds * _impact.Length));
                _boltImpact.sprite = _impact[frame];
                _boltImpact.color = new Color(0.85f, 0.95f, 1f, Mathf.Clamp01(a * 1.2f));
            }
        }

        private void UpdateFog(float dt, Vector2 center, float halfW, float halfH)
        {
            float amount = Mathf.Clamp01(_weather.Fog);
            foreach (Puff p in _puffs)
            {
                p.Renderer.enabled = amount > 0.01f && _fog != null;
                if (!p.Renderer.enabled) continue;

                // Les nappes dérivent avec le vent et reviennent de l'autre côté de l'écran.
                p.Offset += _wind * dt / new Vector2(halfW, halfH);
                p.Offset = new Vector2(Mathf.Repeat(p.Offset.x + 1.3f, 2.6f) - 1.3f, Mathf.Repeat(p.Offset.y + 1.3f, 2.6f) - 1.3f);
                p.Renderer.transform.position = center + new Vector2(p.Offset.x * halfW, p.Offset.y * halfH);
                float breathe = 0.8f + 0.2f * Mathf.Sin(Time.time * 0.3f + p.Phase);
                p.Renderer.color = new Color(0.92f, 0.94f, 0.96f, 0.42f * amount * breathe);
            }
        }
    }
}
