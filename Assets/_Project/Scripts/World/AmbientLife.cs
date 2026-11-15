using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// La vie d'une zone, pour qu'elle ne soit pas « morte » : oiseaux qui nagent (cygnes, canards), libellules qui
    /// virevoltent près du joueur, poissons qui sautent au loin, ombres de nuages qui passent. Tout reste dans la
    /// région de la zone et ne tourne que quand le joueur s'y trouve. Purement décoratif : rien ne bloque.
    /// </summary>
    public sealed class AmbientLife : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Swimmer
        {
            [Tooltip("Images de l'animation (tête vers la droite).")]
            public Sprite[] Frames;
            [Min(0)] public int Count = 3;
            [Min(0.05f)] public float Speed = 0.5f;
        }

        [SerializeField] private WorldMap _map;
        [SerializeField, Tooltip("Région de la carte où vit cette faune.")] private int _region;
        [SerializeField] private Transform _player;

        [SerializeField, Tooltip("Zone à éviter (ponton) pour les oiseaux.")] private Rect _avoid;

        [SerializeField] private List<Swimmer> _swimmers = new List<Swimmer>();

        [SerializeField] private Sprite[] _dragonfly = new Sprite[0];
        [SerializeField, Min(0)] private int _dragonflies = 3;

        [SerializeField, Tooltip("Poisson qui saute : sortie, sommet, plongeon.")] private Sprite[] _jumpingFish = new Sprite[0];
        [SerializeField, Tooltip("Rond dans l'eau qui s'élargit.")] private Sprite[] _ring = new Sprite[0];
        [SerializeField] private Vector2 _jumpInterval = new Vector2(2.5f, 6f);

        [SerializeField] private Sprite _cloud;
        [SerializeField, Min(0)] private int _clouds = 4;
        [SerializeField] private Vector2 _wind = new Vector2(0.55f, 0.12f);

        private sealed class Critter
        {
            public SpriteRenderer Renderer;
            public Sprite[] Frames;
            public Vector2 Target;
            public float Speed;
            public float Wait;
            public float FrameTime;
            public int Frame;
        }

        private readonly List<Critter> _birds = new List<Critter>();
        private readonly List<Critter> _flies = new List<Critter>();
        private readonly List<Transform> _cloudShadows = new List<Transform>();
        private System.Random _random;
        private SpriteRenderer _jumper;
        private SpriteRenderer _splash;
        private float _jumpTimer;
        private float _jumpTime = -1f;
        private Vector2 _jumpFrom;
        private Vector2 _jumpTo;
        private float _splashTime = -1f;
        private bool _active = true;

        private Rect Region => _map.RegionBounds(_region);

        private void Start()
        {
            if (_map == null)
            {
                enabled = false;
                return;
            }

            _random = new System.Random(_region * 7919 + 17);
            foreach (Swimmer swimmer in _swimmers)
            {
                for (int i = 0; i < swimmer.Count && swimmer.Frames.Length > 0; i++)
                {
                    if (TryPickWater(Region.center, Mathf.Max(Region.width, Region.height), out Vector2 start))
                    {
                        Critter bird = Spawn("Oiseau", swimmer.Frames, start, -7);
                        bird.Speed = swimmer.Speed * Range(0.75f, 1.25f);
                        bird.Target = start;
                        _birds.Add(bird);
                    }
                }
            }

            for (int i = 0; i < _dragonflies && _dragonfly.Length > 0; i++)
            {
                Critter fly = Spawn("Libellule", _dragonfly, Region.center, 30);
                fly.Speed = 3f;
                _flies.Add(fly);
            }

            for (int i = 0; i < _clouds && _cloud != null; i++)
            {
                var shadow = new GameObject("Ombre de nuage");
                shadow.transform.SetParent(transform, false);
                shadow.transform.position = new Vector2(Range(Region.xMin, Region.xMax), Range(Region.yMin, Region.yMax));
                var renderer = shadow.AddComponent<SpriteRenderer>();
                renderer.sprite = _cloud;
                renderer.sortingOrder = 40;
                _cloudShadows.Add(shadow.transform);
            }

            _jumper = Spawn("Poisson qui saute", _jumpingFish, Region.center, -6).Renderer;
            _splash = Spawn("Rond dans l'eau", _ring, Region.center, -7).Renderer;
            _jumper.enabled = false;
            _splash.enabled = false;
            _jumpTimer = Range(_jumpInterval.x, _jumpInterval.y);
        }

        private void Update()
        {
            bool here = _map.CurrentRegion == _region;
            if (here != _active)
            {
                _active = here;
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(here);
                }
            }

            if (!here)
            {
                return;
            }

            float dt = Time.deltaTime;
            foreach (Critter bird in _birds)
            {
                UpdateBird(bird, dt);
            }

            foreach (Critter fly in _flies)
            {
                UpdateFly(fly, dt);
            }

            foreach (Transform cloud in _cloudShadows)
            {
                cloud.position = AmbientMath.Wrap((Vector2)cloud.position + _wind * dt, Region, new Vector2(4f, 3f));
            }

            UpdateJump(dt);
        }

        // ---------- Oiseaux qui nagent ----------

        private void UpdateBird(Critter bird, float dt)
        {
            Animate(bird, dt, 0.5f);
            if (bird.Wait > 0f)
            {
                bird.Wait -= dt;
                return;
            }

            Vector2 position = bird.Renderer.transform.position;
            Vector2 toTarget = bird.Target - position;
            if (toTarget.magnitude < 0.1f)
            {
                // Arrivé : petite pause, puis un nouveau coin de lac pas trop loin.
                bird.Wait = Range(0.5f, 3f);
                if (TryPickWater(position, 8f, out Vector2 next))
                {
                    bird.Target = next;
                }

                return;
            }

            Vector2 step = toTarget.normalized * Mathf.Min(bird.Speed * dt, toTarget.magnitude);
            Vector2 ahead = position + toTarget.normalized * 0.6f;
            if (!IsOpenWater(ahead))
            {
                bird.Target = position; // la rive : on s'arrête et on choisit un autre cap
                return;
            }

            bird.Renderer.transform.position = position + step;
            if (Mathf.Abs(step.x) > 0.0001f)
            {
                bird.Renderer.flipX = step.x < 0f;
            }
        }

        // ---------- Libellules ----------

        private void UpdateFly(Critter fly, float dt)
        {
            Animate(fly, dt, 0.06f);
            if (_player == null)
            {
                return;
            }

            Vector2 position = fly.Renderer.transform.position;
            Vector2 player = _player.position;
            if ((position - player).sqrMagnitude > 100f)
            {
                // Trop loin du joueur : elle réapparaît près de lui, hors de l'écran ou presque.
                position = player + Random2(5f);
                fly.Renderer.transform.position = position;
                fly.Target = position;
            }

            if (fly.Wait > 0f)
            {
                fly.Wait -= dt;
                // Vol sur place : petit tremblement.
                fly.Renderer.transform.position = fly.Target + new Vector2(Mathf.Sin(Time.time * 23f), Mathf.Cos(Time.time * 17f)) * 0.03f;
                return;
            }

            Vector2 toTarget = fly.Target - position;
            if (toTarget.magnitude < 0.05f)
            {
                // Un brusque changement de cap, à quelques cases du joueur (au-dessus de l'eau ou de la rive).
                fly.Wait = Range(0.3f, 1.6f);
                fly.Target = player + Random2(4f);
                return;
            }

            Vector2 step = toTarget.normalized * Mathf.Min(fly.Speed * dt, toTarget.magnitude);
            fly.Renderer.transform.position = position + step;
            fly.Renderer.flipX = step.x < 0f;
        }

        // ---------- Poisson qui saute ----------

        private void UpdateJump(float dt)
        {
            if (_splashTime >= 0f)
            {
                _splashTime += dt;
                int frame = Mathf.Min(_ring.Length - 1, Mathf.FloorToInt(_splashTime / 0.18f));
                _splash.sprite = _ring[frame];
                if (_splashTime > _ring.Length * 0.18f)
                {
                    _splash.enabled = false;
                    _splashTime = -1f;
                }
            }

            if (_jumpTime >= 0f)
            {
                _jumpTime += dt;
                float t = _jumpTime / 0.6f;
                _jumper.transform.position = AmbientMath.JumpArc(_jumpFrom, _jumpTo, 0.7f, t);
                _jumper.sprite = _jumpingFish[Mathf.Clamp(Mathf.FloorToInt(t * 3f), 0, _jumpingFish.Length - 1)];
                if (t >= 1f)
                {
                    _jumper.enabled = false;
                    _jumpTime = -1f;
                    StartSplash(_jumpTo);
                }

                return;
            }

            _jumpTimer -= dt;
            if (_jumpTimer > 0f || _player == null || _jumpingFish.Length == 0 || _ring.Length == 0)
            {
                return;
            }

            _jumpTimer = Range(_jumpInterval.x, _jumpInterval.y);
            // Dans le champ de la caméra : à quelques cases du joueur, en pleine eau.
            if (TryPickWater(_player.position, 7f, out Vector2 from) && IsOpenWater(from + Vector2.right * 1.2f))
            {
                bool right = _random.NextDouble() < 0.5;
                _jumpFrom = from;
                _jumpTo = from + (right ? Vector2.right : Vector2.left) * 1.2f;
                _jumper.flipX = !right;
                _jumper.enabled = true;
                _jumpTime = 0f;
                StartSplash(from);
            }
        }

        private void StartSplash(Vector2 at)
        {
            _splash.transform.position = at;
            _splash.enabled = true;
            _splashTime = 0f;
        }

        // ---------- Outils ----------

        private Critter Spawn(string name, Sprite[] frames, Vector2 position, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            renderer.sprite = frames.Length > 0 ? frames[0] : null;
            return new Critter { Renderer = renderer, Frames = frames, Target = position, FrameTime = Range(0f, 0.5f) };
        }

        private static void Animate(Critter critter, float dt, float frameSeconds)
        {
            if (critter.Frames.Length < 2)
            {
                return;
            }

            critter.FrameTime += dt;
            if (critter.FrameTime >= frameSeconds)
            {
                critter.FrameTime = 0f;
                critter.Frame = (critter.Frame + 1) % critter.Frames.Length;
                critter.Renderer.sprite = critter.Frames[critter.Frame];
            }
        }

        /// <summary>Eau libre : de l'eau, dans la région, loin de la rive (marge d'une demi-case) et hors du ponton.</summary>
        private bool IsOpenWater(Vector2 p)
        {
            if (!Region.Contains(p) || _avoid.Contains(p))
            {
                return false;
            }

            const float m = 0.6f;
            return _map.IsWater(p) && _map.IsWater(p + new Vector2(m, 0f)) && _map.IsWater(p - new Vector2(m, 0f))
                && _map.IsWater(p + new Vector2(0f, m)) && _map.IsWater(p - new Vector2(0f, m));
        }

        private bool TryPickWater(Vector2 around, float radius, out Vector2 point)
        {
            for (int attempt = 0; attempt < 40; attempt++)
            {
                point = around + Random2(radius);
                if (IsOpenWater(point))
                {
                    return true;
                }
            }

            point = around;
            return false;
        }

        private Vector2 Random2(float radius) => new Vector2(Range(-radius, radius), Range(-radius, radius));

        private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);
    }
}
