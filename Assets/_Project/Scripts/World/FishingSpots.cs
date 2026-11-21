using System.Collections.Generic;
using FishAndChill.Fishing;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Les coins de pêche d'une zone : quelques endroits d'eau libre, à portée de lancer depuis la rive, qu'on repère à
    /// l'œil (voir <see cref="SpotKind"/>) : un banc (ombres qui tournent, ronds), des bulles qui remontent du fond, une
    /// chasse (éclaboussures, petits poissons qui sautent). Chaque coin dure un moment (ou quelques prises) puis
    /// réapparaît ailleurs. Le <see cref="FishingController"/> vise un coin quand on lance face à lui et en applique les
    /// effets (<see cref="FishingSpotRules"/>). Ne tourne que dans sa région, hors des bâtiments.
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
        [SerializeField, Tooltip("Ombre de poisson vue de dessus (banc).")] private Sprite _shadow;
        [SerializeField] private Sprite _bubble;
        [SerializeField, Tooltip("Petit poisson qui saute (3 images : sortie, sommet, plongeon).")] private Sprite[] _jumper = new Sprite[0];

        private sealed class Spot
        {
            public SpotKind Kind;
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

        public int Region => _region;

        private Rect Bounds => _map.RegionBounds(_region);

        private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);

        /// <summary>Centres des coins actuels (pour la visée du lancer).</summary>
        public IList<Vector2> Centers
        {
            get
            {
                _centers.Clear();
                foreach (Spot spot in _spots)
                {
                    if (spot.Age > 0.5f) _centers.Add(spot.Center);
                }

                return _centers;
            }
        }

        /// <summary>Le bouchon posé ici est-il dans un coin ? Lequel ?</summary>
        public bool TryGetSpot(Vector2 point, out SpotKind kind)
        {
            foreach (Spot spot in _spots)
            {
                if (spot.Age > 0.5f && Vector2.Distance(point, spot.Center) <= FishingSpotRules.Radius)
                {
                    kind = spot.Kind;
                    return true;
                }
            }

            kind = SpotKind.School;
            return false;
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

            _random = new System.Random(_region * 977 + 31);
            ComputeReachable();
            for (int i = 0; i < _count; i++)
            {
                Spot spot = CreateSpot((SpotKind)(i % 3));
                spot.Age = Range(1f, 20f); // déjà là au début
                _spots.Add(spot);
            }
        }

        private Spot CreateSpot(SpotKind kind)
        {
            var spot = new Spot { Kind = kind, Life = Range(_lifetime.x, _lifetime.y), UsesLeft = _uses };
            spot.Center = PickCenter();
            // Remous permanent qui pulse : on repère le coin même entre deux éclaboussures.
            var marker = new GameObject("Remous du coin de pêche");
            marker.transform.SetParent(transform, false);
            spot.Marker = marker.AddComponent<SpriteRenderer>();
            spot.Marker.sortingOrder = -8;
            spot.Marker.sprite = _ring.Length > 0 ? _ring[Mathf.Min(1, _ring.Length - 1)] : null;
            spot.Marker.transform.position = spot.Center;
            spot.Marker.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

            int parts = kind == SpotKind.School ? 7 : kind == SpotKind.Bubbles ? 8 : 6;
            for (int i = 0; i < parts; i++)
            {
                var go = new GameObject($"Coin de pêche ({FishingSpotRules.Name(kind)})");
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = kind == SpotKind.School ? -17 : -7; // les ombres sous l'eau, le reste en surface
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
        private bool[,] _reachable;

        private void ComputeReachable()
        {
            Rect bounds = Bounds;
            int width = Mathf.CeilToInt(bounds.width), height = Mathf.CeilToInt(bounds.height);
            bool Stand(int x, int y) => _map.CanStandAt(new Vector2(bounds.xMin + x + 0.5f, bounds.yMin + y + 0.5f), 0.3f);
            var start = new Vector2Int(Mathf.FloorToInt(_avoid.center.x - bounds.xMin), Mathf.FloorToInt(_avoid.center.y - bounds.yMin));
            for (int r = 0; r <= 3 && !(start.x >= 0 && start.y >= 0 && start.x < width && start.y < height && Stand(start.x, start.y)); r++)
            {
                start += Vector2Int.up;   // le milieu du ponton tombe parfois entre deux planches : on cherche juste à côté
            }

            _reachable = FishingSpotRules.Reachable(width, height, start, Stand);
            if (!_reachable[Mathf.Clamp(start.x, 0, width - 1), Mathf.Clamp(start.y, 0, height - 1)]) _reachable = null;   // ponton introuvable : on garde l'ancienne règle
        }

        /// <summary>Peut-on rejoindre à pied l'endroit <paramref name="q"/> (sa case ou une case voisine) ?</summary>
        private bool Reached(Vector2 q)
        {
            if (_reachable == null) return true;
            Rect bounds = Bounds;
            int cx = Mathf.FloorToInt(q.x - bounds.xMin), cy = Mathf.FloorToInt(q.y - bounds.yMin);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x >= 0 && y >= 0 && x < _reachable.GetLength(0) && y < _reachable.GetLength(1) && _reachable[x, y]) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Le pêcheur peut se tenir sur une rive qu'il rejoint à pied depuis le ponton, droit en face du coin (haut, bas,
        /// gauche ou droite), à portée de lancer.
        /// </summary>
        private bool ReachableFromShore(Vector2 p)
        {
            Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            foreach (Vector2 d in directions)
            {
                for (float k = 2.2f; k <= 4.2f; k += 0.25f)
                {
                    Vector2 q = p - d * k;
                    if (_map.CanStandAt(q, 0.3f) && Reached(q)) return true;
                }
            }

            return false;
        }

        private void Update()
        {
            bool here = _map.CurrentRegion == _region && InteriorBuilding.Occupied == null;
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
                    // Le coin se disperse et réapparaît ailleurs, avec une autre sorte.
                    foreach (SpriteRenderer part in spot.Parts) Destroy(part.gameObject);
                    Destroy(spot.Marker.gameObject);
                    _spots[i] = CreateSpot((SpotKind)_random.Next(0, 3));
                    continue;
                }

                // Apparition et disparition en douceur.
                float fade = Mathf.Clamp01(Mathf.Min(spot.Age, spot.Life - spot.Age) / 1.5f);
                spot.Marker.color = new Color(1f, 1f, 1f, fade * (0.35f + 0.2f * Mathf.Sin(spot.Age * 2.2f)));
                switch (spot.Kind)
                {
                    case SpotKind.School: UpdateSchool(spot, fade, dt); break;
                    case SpotKind.Bubbles: UpdateBubbles(spot, fade, dt); break;
                    default: UpdateFrenzy(spot, fade, dt); break;
                }
            }
        }

        private static Vector3 Snap(Vector2 p) => new Vector3(Mathf.Round(p.x * 16f) / 16f, Mathf.Round(p.y * 16f) / 16f, 0f);

        /// <summary>Banc : ombres qui tournent en rond sous la surface, et de temps en temps un rond dans l'eau.</summary>
        private void UpdateSchool(Spot spot, float fade, float dt)
        {
            int shadows = spot.Parts.Count - 1;
            for (int k = 0; k < shadows; k++)
            {
                SpriteRenderer part = spot.Parts[k];
                part.enabled = _shadow != null;
                part.sprite = _shadow;
                float angle = spot.Age * 0.9f + k * Mathf.PI * 2f / shadows;
                float radius = 0.55f + 0.2f * Mathf.Sin(spot.Age * 0.7f + k);
                part.transform.position = Snap(spot.Center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.6f));
                part.flipX = Mathf.Sin(angle) > 0f; // ils tournent : la tête suit le cercle
                part.color = new Color(1f, 1f, 1f, fade);
            }

            AnimateRing(spot, spot.Parts.Count - 1, fade, dt, 1.2f, 0.4f);
        }

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

        /// <summary>Chasse : éclaboussures fréquentes, et de petits poissons qui sautent hors de l'eau.</summary>
        private void UpdateFrenzy(Spot spot, float fade, float dt)
        {
            for (int k = 0; k < spot.Parts.Count; k++)
            {
                if (k < 2)
                {
                    // Petits poissons qui sautent en arc.
                    SpriteRenderer fish = spot.Parts[k];
                    spot.Timers[k] -= dt;
                    if (spot.Timers[k] <= -0.5f)
                    {
                        spot.Timers[k] = Range(0.1f, 0.6f);
                        spot.Anchors[k] = spot.Center + new Vector2(Range(-0.8f, 0.8f), Range(-0.4f, 0.4f));
                    }

                    bool jumping = spot.Timers[k] < 0f && _jumper.Length > 0;
                    fish.enabled = jumping;
                    if (jumping)
                    {
                        float t = Mathf.Clamp01(-spot.Timers[k] / 0.5f);
                        int frame = Mathf.Min(_jumper.Length - 1, Mathf.FloorToInt(t * _jumper.Length));
                        fish.sprite = _jumper[frame];
                        fish.sortingOrder = -6;
                        fish.transform.position = Snap(spot.Anchors[k] + new Vector2(t * 0.6f - 0.3f, Mathf.Sin(t * Mathf.PI) * 0.45f));
                        fish.color = new Color(1f, 1f, 1f, fade);
                    }
                }
                else
                {
                    AnimateRing(spot, k, fade, dt, 0.3f, 0.7f);
                }
            }
        }

        /// <summary>Un rond qui s'élargit au même endroit du coin, puis repart ailleurs.</summary>
        private void AnimateRing(Spot spot, int index, float fade, float dt, float interval, float spread)
        {
            SpriteRenderer ring = spot.Parts[index];
            if (_ring.Length == 0)
            {
                ring.enabled = false;
                return;
            }

            spot.Timers[index] -= dt;
            if (spot.Timers[index] <= -0.6f)
            {
                spot.Timers[index] = Range(interval * 0.5f, interval * 1.5f);
                spot.Anchors[index] = spot.Center + new Vector2(Range(-spread, spread), Range(-spread * 0.5f, spread * 0.5f));
            }

            bool showing = spot.Timers[index] < 0f;
            ring.enabled = showing;
            if (showing)
            {
                float t = Mathf.Clamp01(-spot.Timers[index] / 0.6f);
                ring.sprite = _ring[Mathf.Min(_ring.Length - 1, Mathf.FloorToInt(t * _ring.Length))];
                ring.sortingOrder = -7;
                ring.transform.position = Snap(spot.Anchors[index]);
                ring.color = new Color(1f, 1f, 1f, fade);
            }
        }
    }
}
