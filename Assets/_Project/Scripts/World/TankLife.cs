using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// La vie d'un bassin de l'aquarium, en plus des pensionnaires (<see cref="AquariumTank"/>) : plantes qui ondulent
    /// (derrière et devant les poissons), colonnes de bulles, rayons de lumière qui glissent, petits bancs de poissons
    /// décoratifs (le bassin vit même vide), reflets d'eau qui dansent sur le sol devant la vitre, méduses qui pulsent dans
    /// leurs colonnes. Purement décoratif.
    /// Ordres d'affichage : sol et fond -6, plantes du fond et reflets -5, poissons -4, plantes de devant, bulles et
    /// rayons -3, vitre -2, mobilier et personnages 10, méduses 11.
    /// </summary>
    public sealed class TankLife : MonoBehaviour
    {
        [System.Serializable]
        public sealed class PlantSet
        {
            public Sprite[] Frames;
            [Min(0)] public int Count = 4;
            [Tooltip("Devant les poissons (sinon derrière).")] public bool Front;
        }

        [SerializeField] private AquariumBuilding _building;
        [SerializeField, Tooltip("Eau du bassin, relative au coin de la salle.")] private Rect _area;
        [SerializeField] private int _seed = 1;

        [SerializeField] private List<PlantSet> _plants = new List<PlantSet>();

        [SerializeField] private Sprite _bubble;
        [SerializeField, Min(0)] private int _bubbleSources = 3;

        [SerializeField, Tooltip("Rayon de lumière oblique qui traverse l'eau.")] private Sprite _ray;
        [SerializeField, Min(0)] private int _rays = 3;

        [SerializeField, Tooltip("Petit poisson d'un banc décoratif (2 images).")] private Sprite[] _school = new Sprite[0];
        [SerializeField, Min(0)] private int _schools = 2;
        [SerializeField, Min(1)] private int _schoolSize = 7;

        [SerializeField, Tooltip("Reflets d'eau sur le sol devant la vitre (images qui s'enchaînent).")] private Sprite[] _caustics = new Sprite[0];

        [SerializeField, Tooltip("Méduse (2 images : ouverte, contractée).")] private Sprite[] _jelly = new Sprite[0];
        [SerializeField, Tooltip("Pied des colonnes à méduses, relatif au coin de la salle.")] private Vector2[] _jellyColumns = new Vector2[0];

        private sealed class Animated
        {
            public SpriteRenderer Renderer;
            public Sprite[] Frames;
            public float Phase;
            public float Speed;
            public Vector2 Home;
        }

        private sealed class Bubble
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public float Speed;
            public float Phase;
        }

        private sealed class School
        {
            public Vector2 Center;
            public Vector2 Target;
            public float Speed;
            public readonly List<Animated> Fish = new List<Animated>();
        }

        private readonly List<Animated> _plantViews = new List<Animated>();
        private readonly List<Bubble> _bubbles = new List<Bubble>();
        private readonly List<float> _sourceX = new List<float>();
        private readonly List<float> _sourceTimer = new List<float>();
        private readonly List<Animated> _rayViews = new List<Animated>();
        private readonly List<School> _schoolViews = new List<School>();
        private readonly List<Animated> _causticViews = new List<Animated>();
        private readonly List<Animated> _jellyViews = new List<Animated>();
        private System.Random _random;

        private Rect Area => _building.WorldRect(_area);

        private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);

        private SpriteRenderer Make(string name, Sprite sprite, int order, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private void Start()
        {
            if (_building == null)
            {
                enabled = false;
                return;
            }

            _random = new System.Random(_seed);
            Rect area = Area;

            foreach (PlantSet set in _plants)
            {
                for (int i = 0; i < set.Count && set.Frames != null && set.Frames.Length > 0; i++)
                {
                    // Réparties sur toute la largeur, un peu au hasard ; posées sur le sable.
                    float x = Mathf.Lerp(area.xMin + 0.6f, area.xMax - 0.6f, (i + Range(0.15f, 0.85f)) / set.Count);
                    var home = new Vector2(x, area.yMin - (set.Front ? 0.35f : 0.1f));
                    SpriteRenderer r = Make(set.Front ? "Plante (devant)" : "Plante", set.Frames[0], set.Front ? -3 : -5, home);
                    _plantViews.Add(new Animated { Renderer = r, Frames = set.Frames, Phase = Range(0f, 10f), Speed = Range(1.6f, 2.6f), Home = home });
                }
            }

            for (int i = 0; i < _bubbleSources && _bubble != null; i++)
            {
                _sourceX.Add(Mathf.Lerp(area.xMin + 1f, area.xMax - 1f, (i + 0.5f) / _bubbleSources + Range(-0.08f, 0.08f)));
                _sourceTimer.Add(Range(0f, 1f));
            }

            for (int i = 0; i < _rays && _ray != null; i++)
            {
                var home = new Vector2(Mathf.Lerp(area.xMin, area.xMax, (i + 0.5f) / _rays), area.center.y + 0.3f);
                SpriteRenderer r = Make("Rayon de lumière", _ray, -3, home);
                _rayViews.Add(new Animated { Renderer = r, Phase = Range(0f, 10f), Speed = Range(0.15f, 0.3f), Home = home });
            }

            for (int s = 0; s < _schools && _school.Length > 0; s++)
            {
                var school = new School { Center = RandomPoint(area), Speed = Range(0.5f, 0.9f) };
                school.Target = RandomPoint(area);
                for (int k = 0; k < _schoolSize; k++)
                {
                    SpriteRenderer r = Make("Petit poisson", _school[0], -4, school.Center);
                    var offset = new Vector2(Range(-0.9f, 0.9f), Range(-0.45f, 0.45f));
                    school.Fish.Add(new Animated { Renderer = r, Frames = _school, Phase = Range(0f, 10f), Home = offset });
                }

                _schoolViews.Add(school);
            }

            if (_caustics.Length > 0)
            {
                // Bande de reflets sur le sol, juste devant la vitre, sur toute la largeur du bassin.
                float width = _caustics[0].bounds.size.x;
                int count = Mathf.CeilToInt(area.width / width);
                for (int i = 0; i < count; i++)
                {
                    var home = new Vector2(area.xMin + width * (i + 0.5f), _building.InteriorOrigin.y + AquariumBuilding.FloorTop - 0.9f);
                    SpriteRenderer r = Make("Reflets au sol", _caustics[0], -5, home);
                    _causticViews.Add(new Animated { Renderer = r, Frames = _caustics, Phase = i * 0.7f, Speed = 3f, Home = home });
                }
            }

            foreach (Vector2 column in _jellyColumns)
            {
                for (int k = 0; k < 2 && _jelly.Length > 0; k++)
                {
                    var home = _building.InteriorOrigin + column + new Vector2(Range(-0.35f, 0.35f), 2.2f + k * 1.4f);
                    SpriteRenderer r = Make("Méduse", _jelly[0], 11, home);
                    _jellyViews.Add(new Animated { Renderer = r, Frames = _jelly, Phase = Range(0f, 10f), Speed = Range(1.2f, 1.8f), Home = home });
                }
            }
        }

        private Vector2 RandomPoint(Rect area) => new Vector2(Range(area.xMin + 1f, area.xMax - 1f), Range(area.yMin + 0.8f, area.yMax - 0.5f));

        private static Vector3 Snap(Vector2 p) => new Vector3(Mathf.Round(p.x * 16f) / 16f, Mathf.Round(p.y * 16f) / 16f, 0f);

        private void Update()
        {
            float time = Time.time;
            float dt = Time.deltaTime;
            Rect area = Area;

            // Plantes : chaque tige ondule à son rythme (images qui s'enchaînent en aller-retour).
            foreach (Animated plant in _plantViews)
            {
                float wave = Mathf.Sin(time * plant.Speed + plant.Phase);
                int frame = Mathf.Clamp(Mathf.RoundToInt((wave + 1f) * 0.5f * (plant.Frames.Length - 1)), 0, plant.Frames.Length - 1);
                plant.Renderer.sprite = plant.Frames[frame];
            }

            // Bulles : chaque source lâche des bulles qui montent en zigzag et éclatent à la surface.
            for (int i = 0; i < _sourceX.Count; i++)
            {
                _sourceTimer[i] -= dt;
                if (_sourceTimer[i] <= 0f)
                {
                    _sourceTimer[i] = Range(0.25f, 0.9f);
                    SpawnBubble(new Vector2(_sourceX[i] + Range(-0.1f, 0.1f), area.yMin + 0.2f));
                }
            }

            foreach (Bubble bubble in _bubbles)
            {
                if (!bubble.Renderer.enabled)
                {
                    continue;
                }

                bubble.Position.y += bubble.Speed * dt;
                bubble.Phase += dt * 5f;
                Vector2 p = bubble.Position + new Vector2(Mathf.Sin(bubble.Phase) * 0.06f, 0f);
                bubble.Renderer.transform.position = Snap(p);
                if (bubble.Position.y > area.yMax + 0.2f)
                {
                    bubble.Renderer.enabled = false;
                }
            }

            // Rayons de lumière : glissent lentement et respirent.
            foreach (Animated ray in _rayViews)
            {
                float x = ray.Home.x + Mathf.Sin(time * ray.Speed + ray.Phase) * 1.6f;
                ray.Renderer.transform.position = Snap(new Vector2(x, ray.Home.y));
                ray.Renderer.color = new Color(1f, 1f, 1f, 0.35f + 0.3f * Mathf.Sin(time * 0.9f + ray.Phase));
            }

            // Bancs : le groupe file vers un point, chaque petit poisson garde sa place en ondulant.
            foreach (School school in _schoolViews)
            {
                Vector2 toTarget = school.Target - school.Center;
                if (toTarget.magnitude < 0.3f)
                {
                    school.Target = RandomPoint(area);
                }

                school.Center += toTarget.normalized * school.Speed * dt;
                bool left = toTarget.x < 0f;
                foreach (Animated fish in school.Fish)
                {
                    Vector2 p = school.Center + fish.Home + new Vector2(Mathf.Sin(time * 1.3f + fish.Phase) * 0.15f, Mathf.Sin(time * 2.1f + fish.Phase) * 0.08f);
                    p.x = Mathf.Clamp(p.x, area.xMin + 0.2f, area.xMax - 0.2f);
                    p.y = Mathf.Clamp(p.y, area.yMin + 0.4f, area.yMax - 0.2f);
                    fish.Renderer.transform.position = Snap(p);
                    fish.Renderer.flipX = left;
                    fish.Renderer.sprite = fish.Frames[Mathf.FloorToInt(time * 6f + fish.Phase) % fish.Frames.Length];
                }
            }

            foreach (Animated caustic in _causticViews)
            {
                caustic.Renderer.sprite = caustic.Frames[Mathf.FloorToInt(time * caustic.Speed + caustic.Phase) % caustic.Frames.Length];
            }

            // Méduses : se contractent pour monter, s'ouvrent en redescendant doucement.
            foreach (Animated jelly in _jellyViews)
            {
                float beat = Mathf.Sin(time * jelly.Speed + jelly.Phase);
                jelly.Renderer.sprite = jelly.Frames[beat > 0.3f ? Mathf.Min(1, jelly.Frames.Length - 1) : 0];
                jelly.Renderer.transform.position = Snap(jelly.Home + new Vector2(Mathf.Sin(time * 0.4f + jelly.Phase) * 0.15f, beat * 0.35f));
            }
        }

        private void SpawnBubble(Vector2 at)
        {
            Bubble bubble = null;
            foreach (Bubble b in _bubbles)
            {
                if (!b.Renderer.enabled)
                {
                    bubble = b;
                    break;
                }
            }

            if (bubble == null)
            {
                bubble = new Bubble { Renderer = Make("Bulle", _bubble, -3, at) };
                _bubbles.Add(bubble);
            }

            bubble.Renderer.enabled = true;
            bubble.Position = at;
            bubble.Speed = Range(0.7f, 1.2f);
            bubble.Phase = Range(0f, 6f);
            float size = Range(0.6f, 1.1f);
            bubble.Renderer.transform.localScale = new Vector3(size, size, 1f);
        }
    }
}
