using System.Collections.Generic;
using FishAndChill.Fishing;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Les coins à bulles d'une zone : quelques endroits d'eau, près d'une rive qu'on rejoint à pied, où de petites bulles
    /// remontent du fond (quelque chose y fouille le sable). La canne n'y gagne rien : ils servent à l'épuisette, et n'apparaissent qu'une fois celle-ci reçue. Chaque
    /// coin dure un moment puis réapparaît ailleurs. Ne tourne que dans sa région, hors des bâtiments.
    /// </summary>
    public sealed class FishingSpots : MonoBehaviour
    {
        [SerializeField] private WorldMap _map;
        [SerializeField, Tooltip("Région de la carte de ces coins.")] private int _region;
        [SerializeField, Tooltip("Zone à éviter (ponton).")] private Rect _avoid;
        [SerializeField, Min(0)] private int _count = 3;
        [SerializeField, Tooltip("Durée de vie d'un coin (secondes).")] private Vector2 _lifetime = new Vector2(70f, 140f);
        [SerializeField, Min(1), Tooltip("Prises possibles avant qu'un coin s'épuise et parte ailleurs.")] private int _uses = 2;

        [SerializeField, Tooltip("Rond dans l'eau qui s'élargit (3 images).")] private Sprite[] _ring = new Sprite[0];
        [SerializeField] private Sprite _bubble;
        [SerializeField, Tooltip("Grappe de bulles bien visible au-dessus du coin (3 images), à la place du rond.")] private Sprite[] _cluster = new Sprite[0];

        private sealed class Spot
        {
            public Vector2 Center;
            public float Life;
            public float Age;
            public int UsesLeft;
            public SpriteRenderer Marker;
            public readonly List<SpriteRenderer> Parts = new List<SpriteRenderer>();
            public readonly List<float> Timers = new List<float>();
            public readonly List<Vector2> Anchors = new List<Vector2>();
        }

        private readonly List<Spot> _spots = new List<Spot>();
        private readonly List<Vector2> _centers = new List<Vector2>();
        private System.Random _random;
        private bool _active = true;
        private Collection.PlayerProgress _progress;

        /// <summary>Les bulles ne se montrent qu'une fois l'épuisette reçue (Chiyo, à Izu).</summary>
        private bool Revealed => _progress == null || _progress.HasNet;

        public int Region => _region;

        private Rect Bounds => _map.RegionBounds(_region);

        private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);

        /// <summary>Centres des coins actuels (pour la visée du lancer).</summary>
        public IList<Vector2> Centers
        {
            get
            {
                _centers.Clear();
                if (!Revealed) return _centers;
                foreach (Spot spot in _spots)
                {
                    if (spot.Age > 0.5f) _centers.Add(spot.Center);
                }

                return _centers;
            }
        }

        /// <summary>Ce point est-il dans un coin à bulles ?</summary>
        public bool Contains(Vector2 point)
        {
            if (!Revealed) return false;
            foreach (Spot spot in _spots)
            {
                if (spot.Age > 0.5f && Vector2.Distance(point, spot.Center) <= FishingSpotRules.Radius)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Une bête a été prise ici : le coin se disperse tout de suite (une seule prise par flaque).</summary>
        public void Deplete(Vector2 point)
        {
            foreach (Spot spot in _spots)
            {
                if (Vector2.Distance(point, spot.Center) <= FishingSpotRules.Radius)
                {
                    spot.UsesLeft = 0;
                    spot.Life = Mathf.Min(spot.Life, spot.Age + 1.5f);
                    return;
                }
            }
        }

        /// <summary>Une prise a été faite dans le coin : il s'épuise et finit par partir ailleurs.</summary>
        public void Consume(Vector2 point)
        {
            foreach (Spot spot in _spots)
            {
                if (Vector2.Distance(point, spot.Center) <= FishingSpotRules.Radius)
                {
                    spot.UsesLeft--;
                    if (spot.UsesLeft <= 0)
                    {
                        spot.Life = Mathf.Min(spot.Life, spot.Age + 1.5f); // il se disperse
                    }

                    return;
                }
            }
        }

        private void Start()
        {
            if (_map == null)
            {
                enabled = false;
                return;
            }

            _progress = FindFirstObjectByType<Collection.PlayerProgress>();
            _random = new System.Random(_region * 977 + 31);
            ComputeReachable();
            for (int i = 0; i < _count; i++)
            {
                Spot spot = CreateSpot();
                spot.Age = Range(1f, 20f); // déjà là au début
                _spots.Add(spot);
            }
        }

        private Spot CreateSpot()
        {
            var spot = new Spot { Life = Range(_lifetime.x, _lifetime.y), UsesLeft = _uses };
            spot.Center = PickCenter();
            // Remous permanent qui pulse : on repère le coin même entre deux éclaboussures.
            var marker = new GameObject("Remous du coin à bulles");
            marker.transform.SetParent(transform, false);
            spot.Marker = marker.AddComponent<SpriteRenderer>();
            spot.Marker.sortingOrder = -8;
            spot.Marker.sprite = _cluster.Length > 0 ? _cluster[0] : _ring.Length > 0 ? _ring[Mathf.Min(1, _ring.Length - 1)] : null;
            spot.Marker.transform.position = spot.Center;
            spot.Marker.transform.localScale = _cluster.Length > 0 ? Vector3.one : new Vector3(1.4f, 1.4f, 1f);

            for (int i = 0; i < 8; i++)
            {
                var go = new GameObject("Bulle du coin");
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = -7;
                renderer.enabled = false;
                spot.Parts.Add(renderer);
                spot.Timers.Add(Range(0f, 2f));
                spot.Anchors.Add(Vector2.zero);
            }

            return spot;
        }

        /// <summary>Eau libre, assez loin des autres coins, mais à portée de lancer depuis une rive.</summary>
        private Vector2 PickCenter()
        {
            Rect bounds = Bounds;
            for (int attempt = 0; attempt < 400; attempt++)
            {
                var p = new Vector2(Range(bounds.xMin + 2f, bounds.xMax - 2f), Range(bounds.yMin + 2f, bounds.yMax - 2f));
                if (_avoid.Contains(p) || !OpenWater(p, 1.6f) || !ReachableFromShore(p))
                {
                    continue;
                }

                bool crowded = false;
                foreach (Spot other in _spots)
                {
                    crowded |= Vector2.Distance(other.Center, p) < 7f;
                }

                if (!crowded)
                {
                    return p;
                }
            }

            return bounds.center;
        }

        private bool OpenWater(Vector2 p, float radius)
        {
            if (!_map.IsWater(p)) return false;
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f;
                if (!_map.IsWater(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius)) return false;
            }

            return true;
        }

        /// <summary>Cases de la région qu'on rejoint à pied depuis le ponton (calculées une fois au début).</summary>
        private ShoreReach _reach;

        private void ComputeReachable() => _reach = new ShoreReach(_map, Bounds, _avoid);

        /// <summary>
        /// Le pêcheur peut se tenir sur une rive qu'il rejoint à pied depuis le ponton, droit en face du coin (haut, bas,
        /// gauche ou droite), à portée de lancer.
        /// </summary>
        private bool ReachableFromShore(Vector2 p) => _reach.FindShore(p, 2.2f, 4.2f, out _, out _);

        private void Update()
        {
            bool here = _map.CurrentRegion == _region && InteriorBuilding.Occupied == null && Revealed;
            if (here != _active)
            {
                _active = here;
                foreach (Spot spot in _spots)
                {
                    foreach (SpriteRenderer part in spot.Parts) part.gameObject.SetActive(here);
                    spot.Marker.gameObject.SetActive(here);
                }
            }

            if (!here)
            {
                return;
            }

            float dt = Time.deltaTime;
            for (int i = 0; i < _spots.Count; i++)
            {
                Spot spot = _spots[i];
                spot.Age += dt;
                if (spot.Age >= spot.Life)
                {
                    // Le coin se disperse et réapparaît ailleurs.
                    foreach (SpriteRenderer part in spot.Parts) Destroy(part.gameObject);
                    Destroy(spot.Marker.gameObject);
                    _spots[i] = CreateSpot();
                    continue;
                }

                // Apparition et disparition en douceur.
                float fade = Mathf.Clamp01(Mathf.Min(spot.Age, spot.Life - spot.Age) / 1.5f);
                if (_cluster.Length > 0)
                {
                    spot.Marker.sprite = _cluster[(int)(spot.Age * 4f) % _cluster.Length];
                    spot.Marker.color = new Color(1f, 1f, 1f, fade);
                }
                else
                {
                    spot.Marker.color = new Color(1f, 1f, 1f, fade * (0.35f + 0.2f * Mathf.Sin(spot.Age * 2.2f)));
                }
                UpdateBubbles(spot, fade, dt);
            }
        }

        private static Vector3 Snap(Vector2 p) => new Vector3(Mathf.Round(p.x * 16f) / 16f, Mathf.Round(p.y * 16f) / 16f, 0f);

        /// <summary>Bulles : de petites bulles éclatent à la surface, ici et là autour du coin.</summary>
        private void UpdateBubbles(Spot spot, float fade, float dt)
        {
            for (int k = 0; k < spot.Parts.Count; k++)
            {
                SpriteRenderer part = spot.Parts[k];
                spot.Timers[k] -= dt;
                if (spot.Timers[k] <= 0f)
                {
                    spot.Timers[k] = Range(0.4f, 1.2f);
                    spot.Anchors[k] = spot.Center + new Vector2(Range(-0.7f, 0.7f), Range(-0.4f, 0.4f));
                }

                float t = 1f - Mathf.Clamp01(spot.Timers[k] / 0.4f);
                part.enabled = _bubble != null && spot.Timers[k] < 0.4f;
                part.sprite = _bubble;
                part.transform.position = Snap(spot.Anchors[k] + new Vector2(0f, t * 0.15f));
                part.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.4f, t);
                part.color = new Color(1f, 1f, 1f, fade * (1f - t * 0.6f));
            }
        }
    }
}
