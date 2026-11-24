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
        [SerializeField, Tooltip("Le remous qui tourne (images à la taille du jeu, jamais agrandies).")] private Sprite[] _eddyFrames = new Sprite[0];
        [SerializeField, Tooltip("Une onde, de la plus petite à la plus grande (une image par taille).")] private Sprite[] _waveFrames = new Sprite[0];

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

        /// <summary>Là où se tenir pour lancer sur le remous : une rive qu'on rejoint à pied depuis le ponton.</summary>
        public Vector2 Shore { get; private set; }

        /// <summary>Direction du regard depuis <see cref="Shore"/> vers le remous.</summary>
        public Vector2 ShoreFacing { get; private set; } = Vector2.up;

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

            _eddy = Make("Remous légendaire", -8, _eddyFrames.Length > 0 ? _eddyFrames[0] : null);
            _shadow = Make("Ombre géante", -17, null);
            for (int i = 0; i < _waves.Length; i++)
            {
                _waves[i] = Make("Vague", -7, null);
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

            // Remous sombre qui tourne et respire (seulement l'opacité : le dessin reste net, jamais agrandi).
            float beat = 0.5f + 0.5f * Mathf.Sin(t * 1.4f);
            if (_eddyFrames.Length > 0)
            {
                _eddy.sprite = _eddyFrames[(int)(t * 7f) % _eddyFrames.Length];
            }

            _eddy.color = new Color(1f, 1f, 1f, 0.75f + 0.25f * beat);

            // L'ombre géante tourne lentement, la queue qui ondule ; elle disparaît quand c'est la ligne qu'elle suit
            // (la pêche dessine alors sa propre ombre sous le bouchon).
            bool hooked = _fishing != null && _fishing.HookedLegend == _legend && _fishing.State != FishingState.Idle
                          && _fishing.State != FishingState.Result;
            _shadow.enabled = !hooked;
            _angle += dt * 0.45f;
            var offset = new Vector2(Mathf.Cos(_angle), Mathf.Sin(_angle) * 0.75f) * 1.05f;
            var heading = new Vector2(-Mathf.Sin(_angle), Mathf.Cos(_angle) * 0.75f);
            _shadow.sprite = _shadows.Get(_legend.ShadowShape, 0, heading, (int)(t * 4f));
            _shadow.transform.position = Center + offset;

            // Ondes qui s'élargissent depuis le centre.
            for (int i = 0; i < _waves.Length; i++)
            {
                _waveAge[i] += dt;
                if (_waveAge[i] > 2.7f) _waveAge[i] -= 2.7f;
                float a = _waveAge[i] / 2.7f;
                if (_waveFrames.Length > 0)
                {
                    _waves[i].sprite = _waveFrames[Mathf.Min(_waveFrames.Length - 1, (int)(a * _waveFrames.Length))];
                }

                _waves[i].color = new Color(0.92f, 0.98f, 1f, (1f - a) * 0.8f);
            }
        }

        /// <summary>
        /// Eau libre, loin du ponton, avec en face une rive qu'on rejoint à pied depuis le ponton, à portée de lancer
        /// (jamais au large d'un îlot inaccessible).
        /// </summary>
        private Vector2 PickCenter()
        {
            Rect bounds = _map.RegionBounds(_region);
            var reach = new ShoreReach(_map, bounds, _avoid);
            for (int attempt = 0; attempt < 2000; attempt++)
            {
                var p = new Vector2(Range(bounds.xMin + 3f, bounds.xMax - 3f), Range(bounds.yMin + 3f, bounds.yMax - 3f));
                if (!_avoid.Contains(p) && OpenWater(p, 2.2f) && reach.FindShore(p, 2.6f, 4.2f, out Vector2 stand, out Vector2 facing))
                {
                    Shore = stand;
                    ShoreFacing = facing;
                    return p;
                }
            }

            Shore = bounds.center;
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
    }
}
