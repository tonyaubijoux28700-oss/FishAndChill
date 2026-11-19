using FishAndChill.Collection;
using FishAndChill.Fishing;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Le remous légendaire d'une zone : une fois la légende invoquée (artefact réveillé au laboratoire), un grand remous
    /// sombre pulse en eau libre, une ombre géante y tourne lentement et l'eau éclate de temps en temps. C'est là qu'on
    /// lance pour l'affronter. Toujours au même endroit (graine fixe), à portée de lancer depuis la rive. Invisible tant
    /// que la légende n'est pas réveillée, et après sa défaite.
    /// </summary>
    public sealed class LegendSpot : MonoBehaviour
    {
        /// <summary>Rayon dans lequel le bouchon doit tomber.</summary>
        public const float Radius = 1.9f;

        [SerializeField] private WorldMap _map;
        [SerializeField] private int _region;
        [SerializeField, Tooltip("Zone à éviter (ponton).")] private Rect _avoid;
        [SerializeField] private LegendFish _legend;
        [SerializeField] private PlayerProgress _progress;
        [SerializeField, Tooltip("Rond dans l'eau qui s'élargit (3 images).")] private Sprite[] _ring = new Sprite[0];

        private SpriteRenderer _eddy;
        private SpriteRenderer _shadow;
        private readonly SpriteRenderer[] _waves = new SpriteRenderer[3];
        private readonly float[] _waveAge = new float[3];
        private FishShadowSprites _shadows;
        private FishingController _fishing;
        private System.Random _random;
        private float _angle;
        private bool _visible;

        public LegendFish Legend => _legend;
        public int Region => _region;
        public Vector2 Center { get; private set; }

        /// <summary>La légende attend ici (réveillée, pas encore vaincue) ?</summary>
        public bool IsAwake => _legend != null && _progress != null && _progress.Legends.State(_legend.Id) == LegendState.Summoned;

        /// <summary>Le remous est-il là, dans la région où se trouve le joueur ?</summary>
        public bool Active => IsAwake && _map != null && _map.CurrentRegion == _region;

        public bool Contains(Vector2 point) => Vector2.Distance(point, Center) <= Radius;

        private void Start()
        {
            if (_map == null || _legend == null || _progress == null)
            {
                enabled = false;
                return;
            }

            _fishing = FindAnyObjectByType<FishingController>();
            _random = new System.Random(_region * 131 + 7);
            Center = PickCenter();
            transform.position = Center;
            _shadows = new FishShadowSprites(72);

            _eddy = Make("Remous légendaire", -8, _ring.Length > 1 ? _ring[1] : null);
            _eddy.transform.localScale = new Vector3(3.2f, 3.2f, 1f);
            _shadow = Make("Ombre géante", -17, null);
            for (int i = 0; i < _waves.Length; i++)
            {
                _waves[i] = Make("Vague", -7, _ring.Length > 0 ? _ring[0] : null);
                _waveAge[i] = i * 0.9f;
            }

            SetVisible(false);
        }

        private SpriteRenderer Make(string name, int order, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;
            return r;
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            _eddy.gameObject.SetActive(visible);
            _shadow.gameObject.SetActive(visible);
            foreach (SpriteRenderer wave in _waves) wave.gameObject.SetActive(visible);
        }

        private void Update()
        {
            bool show = Active && InteriorBuilding.Occupied == null;
            if (show != _visible)
            {
                SetVisible(show);
            }

            if (!show)
            {
                return;
            }

            float t = Time.time;
            float dt = Time.deltaTime;

            // Remous sombre qui respire.
            float beat = 0.5f + 0.5f * Mathf.Sin(t * 1.4f);
            _eddy.color = new Color(0.08f, 0.20f, 0.34f, 0.55f + 0.3f * beat);
            _eddy.transform.localScale = Vector3.one * (3f + 0.35f * beat);

            // L'ombre géante tourne lentement, la queue qui ondule ; elle disparaît quand c'est la ligne qu'elle suit
            // (la pêche dessine alors sa propre ombre sous le bouchon).
            bool hooked = _fishing != null && _fishing.HookedLegend == _legend && _fishing.State != FishingState.Idle
                          && _fishing.State != FishingState.Result;
            _shadow.enabled = !hooked;
            _angle += dt * 0.45f;
            var offset = new Vector2(Mathf.Cos(_angle), Mathf.Sin(_angle) * 0.75f) * 1.05f;
            var heading = new Vector2(-Mathf.Sin(_angle), Mathf.Cos(_angle) * 0.75f);
            _shadow.sprite = _shadows.Get(BodyShape.Monster, 0, heading, (int)(t * 4f));
            _shadow.transform.position = Center + offset;

            // Ondes qui s'élargissent depuis le centre.
            for (int i = 0; i < _waves.Length; i++)
            {
                _waveAge[i] += dt;
                if (_waveAge[i] > 2.7f) _waveAge[i] -= 2.7f;
                float a = _waveAge[i] / 2.7f;
                _waves[i].transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 4.6f, a);
                _waves[i].color = new Color(0.92f, 0.98f, 1f, (1f - a) * 0.8f);
            }
        }

        /// <summary>Eau libre, loin du ponton, avec une rive en face où se tenir à portée de lancer.</summary>
        private Vector2 PickCenter()
        {
            Rect bounds = _map.RegionBounds(_region);
            for (int attempt = 0; attempt < 2000; attempt++)
            {
                var p = new Vector2(Range(bounds.xMin + 3f, bounds.xMax - 3f), Range(bounds.yMin + 3f, bounds.yMax - 3f));
                if (!_avoid.Contains(p) && OpenWater(p, 2.2f) && ReachableFromShore(p))
                {
                    return p;
                }
            }

            return bounds.center;
        }

        private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);

        private bool OpenWater(Vector2 p, float radius)
        {
            if (!_map.IsWater(p)) return false;
            for (int k = 0; k < 12; k++)
            {
                float a = k * Mathf.PI / 6f;
                if (!_map.IsWater(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius)) return false;
            }

            return true;
        }

        private bool ReachableFromShore(Vector2 p)
        {
            Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            foreach (Vector2 d in directions)
            {
                for (float k = 2.6f; k <= 4.2f; k += 0.2f)
                {
                    if (_map.CanStandAt(p - d * k, 0.3f)) return true;
                }
            }

            return false;
        }
    }
}
